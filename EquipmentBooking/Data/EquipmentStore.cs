using EquipmentBooking.Models;

namespace EquipmentBooking.Data;

/// <summary>
/// Временное хранилище каталога в памяти (этап 1). Сбрасывается при перезапуске.
/// На этапе 2 заменяется базой SQLite.
/// </summary>
public static class EquipmentStore
{
    public static readonly List<Equipment> Items = new()
    {
        new() { Id = 1, InventoryNumber = "INV-0001", Name = "Проектор Epson EB-X51", Category = "Проекторы" },
        new() { Id = 2, InventoryNumber = "INV-0002", Name = "Проектор BenQ MW560", Category = "Проекторы" },
        new() { Id = 3, InventoryNumber = "INV-0101", Name = "Ноутбук Lenovo ThinkPad E14", Category = "Ноутбуки" },
        new() { Id = 4, InventoryNumber = "INV-0102", Name = "Ноутбук HP ProBook 450", Category = "Ноутбуки" },
        new() { Id = 5, InventoryNumber = "INV-0201", Name = "Фотоаппарат Canon EOS 250D", Category = "Фото и видео" },
        new() { Id = 6, InventoryNumber = "INV-0301", Name = "Акустическая система JBL EON 710", Category = "Звук", State = EquipmentState.Maintenance }
    };
}
