using EquipmentBooking.Forms;
using EquipmentBooking.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EquipmentBooking.Pages;

/// <summary>Недельный график занятости: строки — оборудование, столбцы — дни.</summary>
public class ScheduleModel : PageModel
{
    private readonly BookingService _bookings;
    public ScheduleModel(BookingService bookings) => _bookings = bookings;

    public DateTime Start { get; private set; }
    public DateTime Today { get; private set; }
    public List<WeekRow> Rows { get; private set; } = new();

    public void OnGet(DateTime? start)
    {
        Today = _bookings.Now.Date;
        var anchor = (start ?? Today).Date;
        // Неделя начинается с понедельника
        int shift = ((int)anchor.DayOfWeek + 6) % 7;
        Start = anchor.AddDays(-shift);
        Rows = _bookings.WeekLoad(Start);
    }
}
