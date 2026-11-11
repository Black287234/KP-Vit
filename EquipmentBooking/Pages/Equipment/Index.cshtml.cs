using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Equipment;

/// <summary>Модель страницы каталога: получает список у сервиса.</summary>
public class IndexModel : PageModel
{
    private readonly CatalogService _catalog;

    public IndexModel(CatalogService catalog) => _catalog = catalog;

    public List<Models.Equipment> Items { get; private set; } = new();

    public void OnGet()
    {
        Items = _catalog.GetAll();
    }
}
