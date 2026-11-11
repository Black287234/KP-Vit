using EquipmentBooking.Forms;
using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Equipment;

/// <summary>Карточка оборудования: описание, смена состояния, история броней.</summary>
public class DetailsModel : PageModel
{
    private readonly CatalogService _catalog;
    private readonly BookingService _bookings;

    public DetailsModel(CatalogService catalog, BookingService bookings)
    {
        _catalog = catalog;
        _bookings = bookings;
    }

    public Models.Equipment Item { get; private set; } = null!;
    public List<Booking> Bookings { get; private set; } = new();

    public IActionResult OnGet(int id)
    {
        var item = _catalog.GetById(id);
        if (item == null) return NotFound();   // неизвестный Id → страница 404
        Item = item;
        Bookings = _bookings.Search(new BookingFilter { EquipmentId = id });
        return Page();
    }

    // Смена состояния: на обслуживание, списание, возврат в работу
    public IActionResult OnPost(int id, EquipmentState state)
    {
        var result = _catalog.ChangeState(id, state);
        if (result.Kind == ErrorKind.NotFound) return NotFound();
        TempData[result.Success ? "Message" : "Error"] = result.Success ? "Состояние изменено" : result.Error;
        return RedirectToPage(new { id });
    }
}
