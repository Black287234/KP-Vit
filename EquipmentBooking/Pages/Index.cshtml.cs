using EquipmentBooking.Forms;
using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages;

/// <summary>Главная: короткая сводка по каталогу и броням.</summary>
public class IndexModel : PageModel
{
    private readonly CatalogService _catalog;
    private readonly BookingService _bookings;

    public IndexModel(CatalogService catalog, BookingService bookings)
    {
        _catalog = catalog;
        _bookings = bookings;
    }

    public int Available { get; private set; }
    public int Maintenance { get; private set; }
    public int Current { get; private set; }
    public int Today { get; private set; }

    public void OnGet()
    {
        var all = _catalog.Search(null);
        Available = all.Count(e => e.State == EquipmentState.Available);
        Maintenance = all.Count(e => e.State == EquipmentState.Maintenance);
        Current = _bookings.Search(new BookingFilter { Tab = "current" }).Count;
        Today = _bookings.Search(new BookingFilter { Tab = "current", Date = _bookings.Now.Date }).Count;
    }
}
