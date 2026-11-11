using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Employees;

public class EditModel : PageModel
{
    private readonly EmployeeService _employees;
    public EditModel(EmployeeService employees) => _employees = employees;

    [BindProperty] public Employee Item { get; set; } = new();

    public IActionResult OnGet(int? id)
    {
        if (id == null) return Page();
        var e = _employees.GetById(id.Value);
        if (e == null) return NotFound();
        Item = e;
        return Page();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();
        var result = _employees.Save(Item);
        if (result.Kind == ErrorKind.NotFound) return NotFound();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        TempData["Message"] = $"Сотрудник «{Item.FullName}» сохранён";
        return RedirectToPage("Index");
    }
}
