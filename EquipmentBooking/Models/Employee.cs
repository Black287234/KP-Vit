using System.ComponentModel.DataAnnotations;

namespace EquipmentBooking.Models;

/// <summary>Сотрудник, который бронирует оборудование.</summary>
public class Employee
{
    public int Id { get; set; }

    [Display(Name = "ФИО")]
    [Required(ErrorMessage = "Укажите ФИО")]
    [StringLength(100, ErrorMessage = "ФИО — не длиннее 100 символов")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Подразделение")]
    [Required(ErrorMessage = "Укажите подразделение")]
    [StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Электронная почта")]
    [EmailAddress(ErrorMessage = "Неверный формат электронной почты")]
    [StringLength(100)]
    public string? Email { get; set; }
}
