using EquipmentBooking.Forms;
using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Bookings;

/// <summary>Текущие и завершённые брони с фильтрами по оборудованию, сотруднику, дате и статусу.</summary>
public class IndexModel : PageModel
{
    private readonly BookingService _bookings;
    private readonly CatalogService _catalog;
    private readonly EmployeeService _employees;

    public IndexModel(BookingService bookings, CatalogService catalog, EmployeeService employees)
    {
        _bookings = bookings;
        _catalog = catalog;
        _employees = employees;
    }

    public BookingFilter Filter { get; private set; } = new();

    public List<Booking> Items { get; private set; } = new();
    public List<Models.Equipment> EquipmentList { get; private set; } = new();
    public List<Employee> Employees { get; private set; } = new();

    // Параметры фильтра приходят из строки адреса: ?Tab=current&EmployeeId=2&Date=2026-11-12
    public void OnGet(BookingFilter filter)
    {
        Filter = filter;
        EquipmentList = _catalog.Search(null);
        Employees = _employees.GetAll();
        Items = _bookings.Search(Filter);
    }
}
