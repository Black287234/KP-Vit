using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Equipment;

/// <summary>Одна страница и для добавления (без id), и для изменения (с id).</summary>
public class EditModel : PageModel
{
    private readonly CatalogService _catalog;

    public EditModel(CatalogService catalog) => _catalog = catalog;

    [BindProperty] public Models.Equipment Item { get; set; } = new();
    public List<string> Categories { get; private set; } = new();

    public IActionResult OnGet(int? id)
    {
        Categories = _catalog.Categories();
        if (id == null) return Page();
        var item = _catalog.GetById(id.Value);
        if (item == null) return NotFound();
        Item = item;
        return Page();
    }

    public IActionResult OnPost(int? id)
    {
        Categories = _catalog.Categories();
        if (!ModelState.IsValid) return Page();

        var result = Item.Id == 0 ? _catalog.Create(Item) : _catalog.Update(Item);
        if (result.Kind == ErrorKind.NotFound) return NotFound();
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        TempData["Message"] = "Сохранено";
        return RedirectToPage("Details", new { id = result.CreatedId ?? Item.Id });
    }
}
