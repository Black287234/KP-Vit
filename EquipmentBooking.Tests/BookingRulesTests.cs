using EquipmentBooking.Models;
using EquipmentBooking.Services;

namespace EquipmentBooking.Tests;

/// <summary>Правила без базы данных: пересечение периодов, проверка периода, возможность отмены.</summary>
public class BookingRulesTests
{
    private static readonly DateTime D = TestEnv.Today;
    private static DateTime T(int h, int m = 0) => D.AddHours(h).AddMinutes(m);

    [Theory]
    [InlineData(10, 0, 12, 0, true)]   // 10:00–12:00 при брони 11:00–13:00 — пересекается
    [InlineData(12, 0, 14, 0, true)]   // захватывает конец существующей
    [InlineData(13, 0, 15, 0, false)]  // начинается ровно в момент окончания — допустимо
    [InlineData(9, 0, 11, 0, false)]   // заканчивается ровно в момент начала — допустимо
    [InlineData(12, 59, 15, 0, true)]  // пересечение на 1 минуту (12:59–13:00)
    [InlineData(11, 30, 12, 30, true)] // целиком внутри
    [InlineData(10, 0, 14, 0, true)]   // целиком накрывает
    public void Пересечение_СБронью11_13_ВерныйРезультат(int sh, int sm, int eh, int em, bool expected)
    {
        Assert.Equal(expected, BookingRules.Overlaps(T(sh, sm), T(eh, em), T(11), T(13)));
    }

    [Fact]
    public void Период_ОкончаниеРаньшеНачала_Ошибка()
    {
        Assert.Equal("Окончание брони должно быть позже начала", BookingRules.ValidatePeriod(T(12), T(10), T(9)));
    }

    [Fact]
    public void Период_ОкончаниеРавноНачалу_Ошибка()
    {
        Assert.NotNull(BookingRules.ValidatePeriod(T(12), T(12), T(9)));
    }

    [Fact]
    public void Период_НачалоВПрошлом_Ошибка()
    {
        Assert.Equal("Нельзя бронировать на время в прошлом", BookingRules.ValidatePeriod(T(8), T(10), T(9)));
    }

    [Fact]
    public void Период_Ровно14Дней_Допустим()
    {
        Assert.Null(BookingRules.ValidatePeriod(T(10), T(10).AddDays(14), T(9)));
    }

    [Fact]
    public void Период_14ДнейИОднаМинута_Ошибка()
    {
        Assert.NotNull(BookingRules.ValidatePeriod(T(10), T(10).AddDays(14).AddMinutes(1), T(9)));
    }

    [Theory]
    [InlineData(BookingStatus.Active, 12, null)]
    [InlineData(BookingStatus.Cancelled, 12, "Бронь уже отменена")]
    [InlineData(BookingStatus.Completed, 8, "Нельзя отменить завершённую бронь")]
    [InlineData(BookingStatus.Active, 8, "Нельзя отменить завершённую бронь")]   // окончание уже прошло
    public void Отмена_РазныеСостояния_ВерныйОтвет(BookingStatus status, int endHour, string? expected)
    {
        var b = new Booking { Status = status, StartAt = T(7), EndAt = T(endHour) };

        Assert.Equal(expected, BookingRules.CanCancel(b, T(9)));
    }
}
