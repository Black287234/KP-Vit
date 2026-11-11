using EquipmentBooking.Forms;
using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Bookings;

/// <summary>Создание брони. Все проверки — в BookingService.Create.</summary>
public class CreateModel : PageModel
{
    private readonly BookingService _bookings;
    private readonly CatalogService _catalog;
    private readonly EmployeeService _employees;

    public CreateModel(BookingService bookings, CatalogService catalog, EmployeeService employees)
    {
        _bookings = bookings;
        _catalog = catalog;
        _employees = employees;
    }

    [BindProperty] public BookingForm Form { get; set; } = new();
    public List<Models.Equipment> EquipmentList { get; private set; } = new();
    public List<Employee> Employees { get; private set; } = new();

    // Можно прийти из каталога (?equipmentId=3) или со страницы доступности (?equipmentId=3&from=…&to=…)
    public void OnGet(int? equipmentId, DateTime? from, DateTime? to)
    {
        var start = from ?? _bookings.Now.Date.AddDays(1).AddHours(10);
        Form = new BookingForm { EquipmentId = equipmentId ?? 0, StartAt = start, EndAt = to ?? start.AddHours(2) };
        LoadLists();
    }

    public IActionResult OnPost()
    {
        if (ModelState.IsValid)
        {
            var result = _bookings.Create(Form);
            if (result.Success)
            {
                TempData["Message"] = "Бронь создана";
                return RedirectToPage("Details", new { id = result.CreatedId });
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        LoadLists();
        return Page();
    }

    private void LoadLists()
    {
        EquipmentList = _catalog.Search(null, EquipmentState.Available);
        Employees = _employees.GetAll();
    }
}
