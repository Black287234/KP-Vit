using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages.Bookings;

/// <summary>Карточка брони, журнал операций и отмена с причиной.</summary>
public class DetailsModel : PageModel
{
    private readonly BookingService _bookings;
    public DetailsModel(BookingService bookings) => _bookings = bookings;

    public Booking Item { get; private set; } = null!;
    public bool CanCancel { get; private set; }
    public string? Reason { get; private set; }

    public IActionResult OnGet(int id) => Load(id) ? Page() : NotFound();

    public IActionResult OnPost(int id, string? reason)
    {
        var result = _bookings.Cancel(id, reason);
        if (result.Kind == ErrorKind.NotFound) return NotFound();
        if (result.Success)
        {
            TempData["Message"] = "Бронь отменена";
            return RedirectToPage(new { id });
        }
        ModelState.AddModelError(string.Empty, result.Error!);
        Reason = reason;
        Load(id);
        return Page();
    }

    private bool Load(int id)
    {
        var b = _bookings.GetById(id);
        if (b == null) return false;
        Item = b;
        CanCancel = BookingRules.CanCancel(b, _bookings.Now) == null;
        return true;
    }
}
