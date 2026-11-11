using EquipmentBooking.Data;
using EquipmentBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBooking.Services;

/// <summary>Каталог оборудования: поиск, добавление, изменение, смена состояния.</summary>
public class CatalogService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public CatalogService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    private DateTime Now => _clock.GetLocalNow().DateTime;

    /// <summary>
    /// Поиск по названию, инвентарному номеру или категории (без учёта регистра)
    /// и фильтр по состоянию. Поиск идёт в памяти: в SQLite сравнение без учёта
    /// регистра работает только для латиницы.
    /// </summary>
    public List<Equipment> Search(string? text, EquipmentState? state = null, string? category = null)
    {
        IEnumerable<Equipment> items = _db.Equipment.AsNoTracking().OrderBy(e => e.InventoryNumber).ToList();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var t = text.Trim();
            items = items.Where(e => e.Name.Contains(t, StringComparison.OrdinalIgnoreCase)
                                  || e.InventoryNumber.Contains(t, StringComparison.OrdinalIgnoreCase)
                                  || e.Category.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        if (state != null)
            items = items.Where(e => e.State == state);
        if (!string.IsNullOrWhiteSpace(category))
            items = items.Where(e => e.Category == category);
        return items.ToList();
    }

    public List<string> Categories() =>
        _db.Equipment.Select(e => e.Category).Distinct().OrderBy(c => c).ToList();

    public Equipment? GetById(int id) => _db.Equipment.AsNoTracking().FirstOrDefault(e => e.Id == id);

    public OperationResult Create(Equipment item)
    {
        Normalize(item);
        var error = Validate(item);
        if (error != null) return OperationResult.Invalid(error);
        if (_db.Equipment.Any(e => e.InventoryNumber == item.InventoryNumber))
            return OperationResult.Conflict($"Оборудование с инвентарным номером {item.InventoryNumber} уже есть в каталоге");

        item.Id = 0;
        item.State = EquipmentState.Available;
        _db.Equipment.Add(item);
        _db.SaveChanges();
        return OperationResult.Ok(item.Id);
    }

    /// <summary>Изменение описательных полей (состояние меняется отдельным действием).</summary>
    public OperationResult Update(Equipment item)
    {
        var existing = _db.Equipment.FirstOrDefault(e => e.Id == item.Id);
        if (existing == null) return OperationResult.NotFound("Оборудование не найдено");
        Normalize(item);
        var error = Validate(item);
        if (error != null) return OperationResult.Invalid(error);
        if (_db.Equipment.Any(e => e.InventoryNumber == item.InventoryNumber && e.Id != item.Id))
            return OperationResult.Conflict($"Оборудование с инвентарным номером {item.InventoryNumber} уже есть в каталоге");

        existing.InventoryNumber = item.InventoryNumber;
        existing.Name = item.Name;
        existing.Category = item.Category;
        existing.Description = item.Description;
        _db.SaveChanges();
        return OperationResult.Ok();
    }

    /// <summary>
    /// Смена состояния. Вывести на обслуживание или списать нельзя, пока есть текущие брони,
    /// а списанное оборудование не возвращается (это архив).
    /// </summary>
    public OperationResult ChangeState(int id, EquipmentState newState)
    {
        var item = _db.Equipment.FirstOrDefault(e => e.Id == id);
        if (item == null) return OperationResult.NotFound("Оборудование не найдено");
        if (item.State == newState) return OperationResult.Invalid("Оборудование уже в этом состоянии");
        if (item.State == EquipmentState.WrittenOff)
            return OperationResult.Conflict("Списанное оборудование нельзя вернуть в работу");

        if (newState != EquipmentState.Available)
        {
            var now = Now;
            int current = _db.Bookings.Count(b => b.EquipmentId == id && b.Status == BookingStatus.Active && b.EndAt > now);
            if (current > 0)
                return OperationResult.Conflict($"У оборудования есть текущие брони ({current}). Сначала отмените их");
        }

        item.State = newState;
        _db.SaveChanges();
        return OperationResult.Ok();
    }

    private static void Normalize(Equipment e)
    {
        e.InventoryNumber = (e.InventoryNumber ?? "").Trim().ToUpperInvariant();
        e.Name = (e.Name ?? "").Trim();
        e.Category = (e.Category ?? "").Trim();
        e.Description = string.IsNullOrWhiteSpace(e.Description) ? null : e.Description.Trim();
    }

    private static string? Validate(Equipment e)
    {
        if (e.InventoryNumber.Length == 0) return "Укажите инвентарный номер";
        if (e.InventoryNumber.Length > 20) return "Инвентарный номер — не длиннее 20 символов";
        if (e.Name.Length == 0) return "Укажите название";
        if (e.Name.Length > 100) return "Название — не длиннее 100 символов";
        if (e.Category.Length == 0) return "Укажите категорию";
        return null;
    }
}
