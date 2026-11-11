using EquipmentBooking.Data;
using EquipmentBooking.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBooking.Tests;

/// <summary>
/// «Остановленные» часы: время не идёт само, его двигает тест.
/// Так проверки «в прошлом» и «уже завершилась» не зависят от момента запуска тестов.
/// </summary>
public class FixedClock : TimeProvider
{
    private DateTimeOffset _now;

    public FixedClock(DateTime now) => _now = new DateTimeOffset(now, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public void Advance(TimeSpan span) => _now = _now.Add(span);
}

/// <summary>Тестовое окружение: пустая база SQLite в памяти и часы на 12.11.2026 09:00.</summary>
public sealed class TestEnv : IDisposable
{
    public static readonly DateTime Today = new(2026, 11, 12);
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public FixedClock Clock { get; } = new(Today.AddHours(9));

    public TestEnv()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = Db();
        db.Database.EnsureCreated();
    }

    public AppDbContext Db() => new(_options);

    public Equipment AddEquipment(string inv = "INV-0001", string name = "Проектор Epson EB-X51",
        EquipmentState state = EquipmentState.Available, string category = "Проекторы")
    {
        using var db = Db();
        var e = new Equipment { InventoryNumber = inv, Name = name, Category = category, State = state };
        db.Equipment.Add(e);
        db.SaveChanges();
        return e;
    }

    public Employee AddEmployee(string name = "Соколова Ирина Павловна")
    {
        using var db = Db();
        var e = new Employee { FullName = name, Department = "Учебная часть" };
        db.Employees.Add(e);
        db.SaveChanges();
        return e;
    }

    public void Dispose() => _connection.Dispose();
}
