using System.ComponentModel.DataAnnotations;

namespace EquipmentBooking.Forms;

/// <summary>Данные формы создания брони.</summary>
public class BookingForm
{
    [Display(Name = "Оборудование")]
    [Range(1, int.MaxValue, ErrorMessage = "Выберите оборудование")]
    public int EquipmentId { get; set; }

    [Display(Name = "Сотрудник")]
    [Range(1, int.MaxValue, ErrorMessage = "Выберите сотрудника")]
    public int EmployeeId { get; set; }

    [Display(Name = "Начало")]
    [DataType(DataType.DateTime)]
    public DateTime StartAt { get; set; }

    [Display(Name = "Окончание")]
    [DataType(DataType.DateTime)]
    public DateTime EndAt { get; set; }

    [Display(Name = "Цель использования")]
    [Required(ErrorMessage = "Укажите цель использования")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Цель — от 3 до 200 символов")]
    public string Purpose { get; set; } = string.Empty;
}
