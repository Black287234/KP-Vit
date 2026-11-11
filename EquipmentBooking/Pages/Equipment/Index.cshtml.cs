using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Equipment;

/// <summary>Каталог с поиском и фильтрами по категории и состоянию.</summary>
public class IndexModel : PageModel
{
    private readonly CatalogService _catalog;

    public IndexModel(CatalogService catalog) => _catalog = catalog;

    // SupportsGet — значения берутся из строки адреса (?Search=…&State=…)
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public EquipmentState? State { get; set; }
    [BindProperty(SupportsGet = true)] public string? Category { get; set; }

    public List<Models.Equipment> Items { get; private set; } = new();
    public List<string> Categories { get; private set; } = new();

    public void OnGet()
    {
        Categories = _catalog.Categories();
        Items = _catalog.Search(Search, State, Category);
    }
}
