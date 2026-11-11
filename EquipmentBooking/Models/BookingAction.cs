using System.ComponentModel.DataAnnotations;

namespace EquipmentBooking.Models;

/// <summary>Действие с бронью для журнала операций.</summary>
public enum BookingAction
{
    [Display(Name = "Создано")]
    Created = 1,

    [Display(Name = "Отменено")]
    Cancelled = 2,

    [Display(Name = "Завершено")]
    Completed = 3
}
