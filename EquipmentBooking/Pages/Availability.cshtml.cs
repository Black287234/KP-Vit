using EquipmentBooking.Forms;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages;

/// <summary>Страница «Свободно ли оборудование с … по …» со списком свободных единиц.</summary>
public class AvailabilityModel : PageModel
{
    private readonly BookingService _bookings;
    public AvailabilityModel(BookingService bookings) => _bookings = bookings;

    public DateTime From { get; private set; }
    public DateTime To { get; private set; }
    public string? Error { get; private set; }
    public List<AvailabilityRow> Rows { get; private set; } = new();

    public void OnGet(DateTime? from, DateTime? to)
    {
        // По умолчанию — завтра с 10:00 до 12:00
        From = from ?? _bookings.Now.Date.AddDays(1).AddHours(10);
        To = to ?? From.AddHours(2);
        (Error, Rows) = _bookings.CheckAvailability(From, To);
    }
}
