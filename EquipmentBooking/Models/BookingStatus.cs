using System.ComponentModel.DataAnnotations;

namespace EquipmentBooking.Models;

/// <summary>Статус брони.</summary>
public enum BookingStatus
{
    [Display(Name = "Активно")]
    Active = 1,

    [Display(Name = "Отменено")]
    Cancelled = 2,

    // Ставится автоматически, когда время окончания брони прошло
    [Display(Name = "Завершено")]
    Completed = 3
}
