using EquipmentBooking.Models;

namespace EquipmentBooking.Forms;

/// <summary>Фильтр списка броней. Пустое поле — условие не применяется.</summary>
public class BookingFilter
{
    public int? EquipmentId { get; set; }
    public int? EmployeeId { get; set; }

    /// <summary>Показать брони, которые захватывают этот день.</summary>
    public DateTime? Date { get; set; }

    public BookingStatus? Status { get; set; }

    /// <summary>"current" — текущие, "finished" — завершённые и отменённые.</summary>
    public string? Tab { get; set; }
}
