using System.ComponentModel.DataAnnotations;

namespace EquipmentBooking.Models;

/// <summary>
/// Бронирование оборудования на период. Брони не удаляются — только отменяются,
/// чтобы сохранялась история использования оборудования.
/// </summary>
public class Booking
{
    public int Id { get; set; }

    [Display(Name = "Оборудование")]
    public int EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    [Display(Name = "Сотрудник")]
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    [Display(Name = "Начало")]
    public DateTime StartAt { get; set; }

    [Display(Name = "Окончание")]
    public DateTime EndAt { get; set; }

    [Display(Name = "Цель использования")]
    [StringLength(200)]
    public string Purpose { get; set; } = string.Empty;

    [Display(Name = "Статус")]
    public BookingStatus Status { get; set; } = BookingStatus.Active;

    public DateTime CreatedAt { get; set; }

    [Display(Name = "Дата отмены")]
    public DateTime? CancelledAt { get; set; }

    [Display(Name = "Причина отмены")]
    [StringLength(200)]
    public string? CancelReason { get; set; }

    public List<BookingHistory> History { get; set; } = new();
}
