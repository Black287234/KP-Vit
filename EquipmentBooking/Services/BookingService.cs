using EquipmentBooking.Data;
using EquipmentBooking.Forms;
using EquipmentBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBooking.Services;

/// <summary>
/// Бронирование: проверка доступности, создание, отмена, автоматическое завершение,
/// текущие и завершённые брони, фильтры, недельный график загрузки.
/// </summary>
public class BookingService
{
    // Две брони одного оборудования, пришедшие одновременно, проверяются по очереди:
    // иначе обе увидели бы свободный период и обе сохранились бы.
    private static readonly object CreateLock = new();

    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public BookingService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <summary>Текущее время берём у TimeProvider — в тестах его можно «остановить».</summary>
    public DateTime Now => _clock.GetLocalNow().DateTime;

    /// <summary>
    /// Активные брони, у которых время окончания уже прошло, получают статус «Завершено».
    /// Вызывается перед выводом списков, поэтому статус всегда актуален.
    /// </summary>
    public int CompleteExpired()
    {
        var now = Now;
        var expired = _db.Bookings.Where(b => b.Status == BookingStatus.Active && b.EndAt <= now).ToList();
        foreach (var b in expired)
        {
            b.Status = BookingStatus.Completed;
            _db.BookingHistory.Add(new BookingHistory
            {
                BookingId = b.Id, Action = BookingAction.Completed, Date = now, Comment = "Завершено автоматически по окончании периода"
            });
        }
        if (expired.Count > 0) _db.SaveChanges();
        return expired.Count;
    }

    /// <summary>Создать бронь, если оборудование доступно и свободно на весь период.</summary>
    public OperationResult Create(BookingForm form)
    {
        var now = Now;
        var purpose = (form.Purpose ?? "").Trim();
        if (purpose.Length < 3)
            return OperationResult.Invalid("Укажите цель использования (не короче 3 символов)");

        var periodError = BookingRules.ValidatePeriod(form.StartAt, form.EndAt, now);
        if (periodError != null)
            return OperationResult.Invalid(periodError);

        var equipment = _db.Equipment.FirstOrDefault(e => e.Id == form.EquipmentId);
        if (equipment == null)
            return OperationResult.NotFound("Оборудование не найдено");
        if (equipment.State != EquipmentState.Available)
            return OperationResult.Conflict(
                $"«{equipment.Name}» нельзя забронировать: состояние «{StateName(equipment.State)}»");

        if (!_db.Employees.Any(e => e.Id == form.EmployeeId))
            return OperationResult.NotFound("Сотрудник не найден");

        lock (CreateLock)
        {
            // ГЛАВНОЕ ПРАВИЛО: период не должен пересекаться с другой АКТИВНОЙ бронью этого оборудования.
            // Отменённые и завершённые брони не мешают.
            var conflict = _db.Bookings
                .Include(b => b.Employee)
                .Where(b => b.EquipmentId == form.EquipmentId && b.Status == BookingStatus.Active)
                .Where(b => form.StartAt < b.EndAt && form.EndAt > b.StartAt)
                .OrderBy(b => b.StartAt)
                .FirstOrDefault();
            if (conflict != null)
                return OperationResult.Conflict(
                    $"«{equipment.Name}» уже забронировано с {conflict.StartAt:dd.MM.yyyy HH:mm} по {conflict.EndAt:dd.MM.yyyy HH:mm} " +
                    $"({conflict.Employee?.FullName}). Выберите другое время или другое оборудование");

            var booking = new Booking
            {
                EquipmentId = form.EquipmentId,
                EmployeeId = form.EmployeeId,
                StartAt = form.StartAt,
                EndAt = form.EndAt,
                Purpose = purpose,
                Status = BookingStatus.Active,
                CreatedAt = now
            };
            booking.History.Add(new BookingHistory { Action = BookingAction.Created, Date = now, Comment = purpose });
            _db.Bookings.Add(booking);
            _db.SaveChanges();
            return OperationResult.Ok(booking.Id);
        }
    }

    /// <summary>Отменить бронь с указанием причины (вместо удаления).</summary>
    public OperationResult Cancel(int bookingId, string? reason)
    {
        var now = Now;
        var booking = _db.Bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking == null)
            return OperationResult.NotFound("Бронь не найдена");

        var error = BookingRules.CanCancel(booking, now);
        if (error != null)
            return OperationResult.Conflict(error);

        var text = (reason ?? "").Trim();
        if (text.Length < 3)
            return OperationResult.Invalid("Укажите причину отмены (не короче 3 символов)");

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.CancelReason = text;
        _db.BookingHistory.Add(new BookingHistory { BookingId = booking.Id, Action = BookingAction.Cancelled, Date = now, Comment = text });
        _db.SaveChanges();
        return OperationResult.Ok();
    }

    public Booking? GetById(int id)
    {
        CompleteExpired();
        return _db.Bookings.AsNoTracking()
            .Include(b => b.Equipment).Include(b => b.Employee).Include(b => b.History)
            .FirstOrDefault(b => b.Id == id);
    }

    /// <summary>
    /// Список броней с фильтрами. Вкладка «current» — активные с ещё не наступившим окончанием,
    /// «finished» — завершённые и отменённые.
    /// </summary>
    public List<Booking> Search(BookingFilter f)
    {
        CompleteExpired();
        var now = Now;
        IQueryable<Booking> q = _db.Bookings.AsNoTracking().Include(b => b.Equipment).Include(b => b.Employee);

        if (f.Tab == "current")
            q = q.Where(b => b.Status == BookingStatus.Active && b.EndAt > now);
        else if (f.Tab == "finished")
            q = q.Where(b => b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled);

        if (f.EquipmentId != null) q = q.Where(b => b.EquipmentId == f.EquipmentId);
        if (f.EmployeeId != null) q = q.Where(b => b.EmployeeId == f.EmployeeId);
        if (f.Status != null) q = q.Where(b => b.Status == f.Status);
        if (f.Date != null)
        {
            // бронь «захватывает» день, если пересекается с отрезком [день 00:00; следующий день 00:00)
            var dayStart = f.Date.Value.Date;
            var dayEnd = dayStart.AddDays(1);
            q = q.Where(b => b.StartAt < dayEnd && b.EndAt > dayStart);
        }

        // Текущие — ближайшие сверху; завершённые — последние сверху
        return f.Tab == "current"
            ? q.OrderBy(b => b.StartAt).ToList()
            : q.OrderByDescending(b => b.StartAt).ToList();
    }

    /// <summary>
    /// «Свободно ли оборудование с … по …»: для каждой единицы каталога — свободна ли она
    /// и, если нет, почему (состояние или пересекающаяся бронь).
    /// </summary>
    public (string? Error, List<AvailabilityRow> Rows) CheckAvailability(DateTime from, DateTime to)
    {
        if (to <= from)
            return ("Окончание периода должно быть позже начала", new());

        CompleteExpired();
        var equipment = _db.Equipment.AsNoTracking().OrderBy(e => e.Category).ThenBy(e => e.Name).ToList();
        var busy = _db.Bookings.AsNoTracking().Include(b => b.Employee)
            .Where(b => b.Status == BookingStatus.Active && from < b.EndAt && to > b.StartAt)
            .ToList();

        var rows = equipment.Select(e =>
        {
            if (e.State != EquipmentState.Available)
                return new AvailabilityRow { Equipment = e, IsFree = false, Reason = StateName(e.State) };
            var b = busy.Where(x => x.EquipmentId == e.Id).OrderBy(x => x.StartAt).FirstOrDefault();
            return b == null
                ? new AvailabilityRow { Equipment = e, IsFree = true }
                : new AvailabilityRow { Equipment = e, IsFree = false,
                    Reason = $"занято {b.StartAt:dd.MM HH:mm}–{b.EndAt:dd.MM HH:mm} ({b.Employee?.FullName})" };
        }).ToList();
        return (null, rows);
    }

    /// <summary>Недельный график: какие часы каждого дня заняты у каждой единицы оборудования.</summary>
    public List<WeekRow> WeekLoad(DateTime weekStart)
    {
        CompleteExpired();
        var start = weekStart.Date;
        var end = start.AddDays(7);
        var equipment = _db.Equipment.AsNoTracking().Where(e => e.State != EquipmentState.WrittenOff)
            .OrderBy(e => e.Category).ThenBy(e => e.Name).ToList();
        var bookings = _db.Bookings.AsNoTracking().Include(b => b.Employee)
            .Where(b => b.Status != BookingStatus.Cancelled && b.StartAt < end && b.EndAt > start)
            .OrderBy(b => b.StartAt).ToList();

        var rows = new List<WeekRow>();
        foreach (var e in equipment)
        {
            var row = new WeekRow { EquipmentId = e.Id, Title = $"{e.Name} ({e.InventoryNumber})" };
            foreach (var b in bookings.Where(b => b.EquipmentId == e.Id))
            {
                for (int d = 0; d < 7; d++)
                {
                    var dayStart = start.AddDays(d);
                    var dayEnd = dayStart.AddDays(1);
                    if (b.StartAt < dayEnd && b.EndAt > dayStart)
                    {
                        var s = b.StartAt < dayStart ? dayStart : b.StartAt;
                        var f = b.EndAt > dayEnd ? dayEnd : b.EndAt;
                        var surname = b.Employee?.FullName.Split(' ')[0];
                        row.Days[d].Add($"{s:HH:mm}–{(f == dayEnd ? "24:00" : f.ToString("HH:mm"))} {surname}");
                    }
                }
            }
            rows.Add(row);
        }
        return rows;
    }

    public static string StateName(EquipmentState s) => s switch
    {
        EquipmentState.Available => "Доступно для бронирования",
        EquipmentState.Maintenance => "На обслуживании",
        _ => "Списано"
    };

    public static string StatusName(BookingStatus s) => s switch
    {
        BookingStatus.Active => "Активно",
        BookingStatus.Cancelled => "Отменено",
        _ => "Завершено"
    };
}
