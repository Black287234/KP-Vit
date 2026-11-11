using EquipmentBooking.Models;

namespace EquipmentBooking.Services;

/// <summary>
/// Правила бронирования в виде «чистых» функций без базы данных — их удобно проверять тестами.
/// </summary>
public static class BookingRules
{
    /// <summary>Максимальный срок одной брони.</summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(14);

    /// <summary>
    /// ГЛАВНОЕ ПРАВИЛО. Два периода пересекаются, если новоеНачало &lt; существующийКонец
    /// и новыйКонец &gt; существующееНачало. Касание концами (12:00–14:00 после 10:00–12:00)
    /// пересечением не считается.
    /// </summary>
    public static bool Overlaps(DateTime newStart, DateTime newEnd, DateTime existingStart, DateTime existingEnd)
    {
        return newStart < existingEnd && newEnd > existingStart;
    }

    /// <summary>Проверка самого периода. Возвращает текст ошибки или null, если период допустим.</summary>
    public static string? ValidatePeriod(DateTime start, DateTime end, DateTime now)
    {
        if (end <= start)
            return "Окончание брони должно быть позже начала";
        // Минута запаса: форма отправляется не мгновенно
        if (start < now.AddMinutes(-1))
            return "Нельзя бронировать на время в прошлом";
        if (end - start > MaxDuration)
            return $"Бронь не может быть длиннее {MaxDuration.TotalDays:0} дней";
        return null;
    }

    /// <summary>Бронь «текущая», если она активна и её окончание ещё не наступило.</summary>
    public static bool IsCurrent(Booking b, DateTime now) => b.Status == BookingStatus.Active && b.EndAt > now;

    /// <summary>Отменить можно только активную бронь, которая ещё не закончилась.</summary>
    public static string? CanCancel(Booking b, DateTime now)
    {
        if (b.Status == BookingStatus.Cancelled)
            return "Бронь уже отменена";
        if (b.Status == BookingStatus.Completed || b.EndAt <= now)
            return "Нельзя отменить завершённую бронь";
        return null;
    }
}
