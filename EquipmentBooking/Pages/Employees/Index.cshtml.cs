using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Employees;

public class IndexModel : PageModel
{
    private readonly EmployeeService _employees;
    public IndexModel(EmployeeService employees) => _employees = employees;

    public List<Employee> Items { get; private set; } = new();

    public void OnGet() => Items = _employees.GetAll();
}
