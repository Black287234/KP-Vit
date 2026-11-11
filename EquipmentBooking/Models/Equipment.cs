using System.ComponentModel.DataAnnotations;

namespace EquipmentBooking.Models;

/// <summary>Единица оборудования из каталога (проектор, ноутбук, камера…).</summary>
public class Equipment
{
    public int Id { get; set; }

    [Display(Name = "Инвентарный номер")]
    [Required(ErrorMessage = "Укажите инвентарный номер")]
    [StringLength(20, ErrorMessage = "Инвентарный номер — не длиннее 20 символов")]
    public string InventoryNumber { get; set; } = string.Empty;

    [Display(Name = "Название")]
    [Required(ErrorMessage = "Укажите название")]
    [StringLength(100, ErrorMessage = "Название — не длиннее 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Категория")]
    [Required(ErrorMessage = "Укажите категорию")]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Описание")]
    [StringLength(300)]
    public string? Description { get; set; }

    [Display(Name = "Состояние")]
    public EquipmentState State { get; set; } = EquipmentState.Available;
}
