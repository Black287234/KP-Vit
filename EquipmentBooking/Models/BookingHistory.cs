namespace EquipmentBooking.Models;

/// <summary>Запись журнала: что и когда произошло с бронью.</summary>
public class BookingHistory
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public BookingAction Action { get; set; }
    public DateTime Date { get; set; }
    public string? Comment { get; set; }
}
