using System.ComponentModel.DataAnnotations;

namespace EquipmentBooking.Models;

/// <summary>Состояние единицы оборудования.</summary>
public enum EquipmentState
{
    [Display(Name = "Доступно для бронирования")]
    Available = 1,

    [Display(Name = "На обслуживании")]
    Maintenance = 2,

    // Вместо удаления: списанное оборудование остаётся в базе ради истории броней
    [Display(Name = "Списано")]
    WrittenOff = 3
}
