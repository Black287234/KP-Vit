using EquipmentBooking.Models;

namespace EquipmentBooking.Forms;

/// <summary>Строка ответа «свободно ли оборудование в период»: единица и причина занятости.</summary>
public class AvailabilityRow
{
    public Equipment Equipment { get; set; } = null!;
    public bool IsFree { get; set; }

    /// <summary>Почему нельзя забронировать: состояние или пересекающаяся бронь.</summary>
    public string? Reason { get; set; }
}
