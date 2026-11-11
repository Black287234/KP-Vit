using EquipmentBooking.Forms;
using EquipmentBooking.Models;
using EquipmentBooking.Services;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBooking.Tests;

/// <summary>
/// Создание и отмена броней через сервис. Часы: 12.11.2026 09:00.
/// Во всех тестах бронируется завтрашний день (13.11.2026).
/// </summary>
public class BookingServiceTests : IDisposable
{
    private readonly TestEnv _env = new();
    private readonly Equipment _projector;
    private readonly Employee _teacher;
    private static readonly DateTime Tomorrow = TestEnv.Today.AddDays(1);

    public BookingServiceTests()
    {
        _projector = _env.AddEquipment();
        _teacher = _env.AddEmployee();
    }

    public void Dispose() => _env.Dispose();

    private OperationResult Book(int fromHour, int toHour, int? equipmentId = null, string purpose = "Лекция",
        int fromMinute = 0, int? employeeId = null, DateTime? day = null)
    {
        var d = day ?? Tomorrow;
        using var db = _env.Db();
        return new BookingService(db, _env.Clock).Create(new BookingForm
        {
            EquipmentId = equipmentId ?? _projector.Id,
            EmployeeId = employeeId ?? _teacher.Id,
            StartAt = d.AddHours(fromHour).AddMinutes(fromMinute),
            EndAt = d.AddHours(toHour),
            Purpose = purpose
        });
    }

    private OperationResult Cancel(int id, string? reason)
    {
        using var db = _env.Db();
        return new BookingService(db, _env.Clock).Cancel(id, reason);
    }

    [Fact]
    public void СоздатьБронь_СвободныйПериод_СохраняетсяСЗаписьюВЖурнале()
    {
        var result = Book(10, 12);

        Assert.True(result.Success);
        using var db = _env.Db();
        var b = db.Bookings.Include(x => x.History).Single();
        Assert.Equal(BookingStatus.Active, b.Status);
        Assert.Equal(BookingAction.Created, Assert.Single(b.History).Action);
    }

    [Fact]
    public void СоздатьБронь_ПериодПересекается_Отклоняется()
    {
        Book(11, 13);

        var result = Book(10, 12);   // пример из задания: 10:00–12:00 при брони 11:00–13:00

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("уже забронировано", result.Error);
    }

    [Fact]
    public void СоздатьБронь_НачинаетсяВМоментОкончанияДругой_Допустимо()
    {
        Book(10, 12);

        var result = Book(12, 14);

        Assert.True(result.Success);
    }

    [Fact]
    public void СоздатьБронь_ПересечениеНаОднуМинуту_Отклоняется()
    {
        Book(10, 12);

        var result = Book(11, 14, fromMinute: 59);   // 11:59–14:00

        Assert.Equal(ErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public void СоздатьБронь_ОтменённаяБроньНеМешает_Допустимо()
    {
        var first = Book(10, 12);
        Cancel(first.CreatedId!.Value, "Занятие перенесено");

        var result = Book(10, 12);

        Assert.True(result.Success);
    }

    [Fact]
    public void СоздатьБронь_ДругоеОборудованиеНаТоЖеВремя_Допустимо()
    {
        var laptop = _env.AddEquipment("INV-0101", "Ноутбук Lenovo", category: "Ноутбуки");
        Book(10, 12);

        var result = Book(10, 12, laptop.Id);

        Assert.True(result.Success);
    }

    [Theory]
    [InlineData(EquipmentState.Maintenance)]
    [InlineData(EquipmentState.WrittenOff)]
    public void СоздатьБронь_ОборудованиеНедоступно_Отклоняется(EquipmentState state)
    {
        var broken = _env.AddEquipment("INV-0302", "Колонка JBL", state, "Звук");

        var result = Book(10, 12, broken.Id);

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("нельзя забронировать", result.Error);
    }

    [Fact]
    public void СоздатьБронь_ПустаяЦель_Отклоняется()
    {
        var result = Book(10, 12, purpose: "  ");

        Assert.Equal(ErrorKind.Invalid, result.Kind);
    }

    [Fact]
    public void СоздатьБронь_КонецРаньшеНачала_Отклоняется()
    {
        var result = Book(12, 10);

        Assert.Equal("Окончание брони должно быть позже начала", result.Error);
    }

    [Fact]
    public void СоздатьБронь_ДатаВПрошлом_Отклоняется()
    {
        var result = Book(7, 8, day: TestEnv.Today);   // сегодня 07:00, а сейчас 09:00

        Assert.Equal("Нельзя бронировать на время в прошлом", result.Error);
    }

    [Fact]
    public void СоздатьБронь_ДлиннееЧетырнадцатиДней_Отклоняется()
    {
        using var db = _env.Db();
        var result = new BookingService(db, _env.Clock).Create(new BookingForm
        {
            EquipmentId = _projector.Id, EmployeeId = _teacher.Id, Purpose = "Командировка",
            StartAt = Tomorrow.AddHours(9), EndAt = Tomorrow.AddDays(15).AddHours(9)
        });

        Assert.Equal(ErrorKind.Invalid, result.Kind);
    }

    [Fact]
    public void СоздатьБронь_НеизвестноеОборудование_ЗаписьНеНайдена()
    {
        Assert.Equal(ErrorKind.NotFound, Book(10, 12, equipmentId: 999).Kind);
    }

    [Fact]
    public void СоздатьБронь_НеизвестныйСотрудник_ЗаписьНеНайдена()
    {
        Assert.Equal(ErrorKind.NotFound, Book(10, 12, employeeId: 999).Kind);
    }

    [Fact]
    public void ОтменитьБронь_СПричиной_СтатусОтменено()
    {
        var id = Book(10, 12).CreatedId!.Value;

        var result = Cancel(id, "Занятие перенесено");

        Assert.True(result.Success);
        using var db = _env.Db();
        var b = db.Bookings.Include(x => x.History).Single();
        Assert.Equal(BookingStatus.Cancelled, b.Status);
        Assert.Equal("Занятие перенесено", b.CancelReason);
        Assert.NotNull(b.CancelledAt);
        Assert.Contains(b.History, h => h.Action == BookingAction.Cancelled);
    }

    [Fact]
    public void ОтменитьБронь_Повторно_Отклоняется()
    {
        var id = Book(10, 12).CreatedId!.Value;
        Cancel(id, "Занятие перенесено");

        var result = Cancel(id, "Ещё раз");

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Equal("Бронь уже отменена", result.Error);
    }

    [Fact]
    public void ОтменитьБронь_БезПричины_Отклоняется()
    {
        var id = Book(10, 12).CreatedId!.Value;

        Assert.Equal(ErrorKind.Invalid, Cancel(id, "").Kind);
    }

    [Fact]
    public void ОтменитьБронь_НеизвестныйId_ЗаписьНеНайдена()
    {
        Assert.Equal(ErrorKind.NotFound, Cancel(999, "Причина").Kind);
    }

    [Fact]
    public void ОтменитьБронь_ПослеОкончания_Отклоняется()
    {
        var id = Book(10, 12).CreatedId!.Value;
        _env.Clock.Advance(TimeSpan.FromDays(2));   // бронь уже закончилась

        Assert.Equal(ErrorKind.Conflict, Cancel(id, "Поздно").Kind);
    }

    [Fact]
    public void Завершение_ПослеОкончанияПериода_СтатусСтавитсяАвтоматически()
    {
        Book(10, 12);
        _env.Clock.Advance(TimeSpan.FromDays(1).Add(TimeSpan.FromHours(3)));   // 13.11 12:00

        using var db = _env.Db();
        int completed = new BookingService(db, _env.Clock).CompleteExpired();

        Assert.Equal(1, completed);
        using var db2 = _env.Db();
        var b = db2.Bookings.Include(x => x.History).Single();
        Assert.Equal(BookingStatus.Completed, b.Status);
        Assert.Contains(b.History, h => h.Action == BookingAction.Completed);
    }

    [Fact]
    public void Завершение_ДоОкончанияПериода_БроньОстаётсяАктивной()
    {
        Book(10, 12);
        _env.Clock.Advance(TimeSpan.FromDays(1).Add(TimeSpan.FromHours(2)));   // 13.11 11:00 — бронь идёт

        using var db = _env.Db();
        Assert.Equal(0, new BookingService(db, _env.Clock).CompleteExpired());
    }
}
