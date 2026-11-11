using EquipmentBooking.Models;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBooking.Data;

/// <summary>Контекст базы данных: четыре таблицы.</summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingHistory> BookingHistory => Set<BookingHistory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Инвентарный номер уникален
        b.Entity<Equipment>().HasIndex(e => e.InventoryNumber).IsUnique();

        // Оборудование 1 — N броней, сотрудник 1 — N броней; удалять связанные записи нельзя
        b.Entity<Booking>().HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Booking>().HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);

        // Бронь 1 — N записей журнала
        b.Entity<BookingHistory>().HasOne<Booking>().WithMany(x => x.History).HasForeignKey(x => x.BookingId);

        // Поиск пересечений идёт по оборудованию и времени — индекс ускоряет проверку доступности
        b.Entity<Booking>().HasIndex(x => new { x.EquipmentId, x.StartAt, x.EndAt });
    }
}
