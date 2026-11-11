using EquipmentBooking.Models;

namespace EquipmentBooking.Data;

/// <summary>
/// Учебные данные: каталог, сотрудники и брони. Даты броней считаются от «сегодня»,
/// поэтому при первом запуске в любой день есть и текущие, и завершённые брони.
/// </summary>
public static class DbSeeder
{
    public static void Seed(AppDbContext db, DateTime now)
    {
        if (db.Equipment.Any())
            return;   // база уже заполнена — данные пользователя не трогаем

        var eq = new List<Equipment>
        {
            new() { InventoryNumber = "INV-0001", Name = "Проектор Epson EB-X51", Category = "Проекторы", Description = "3800 лм, HDMI/VGA, в сумке с пультом" },
            new() { InventoryNumber = "INV-0002", Name = "Проектор BenQ MW560", Category = "Проекторы", Description = "4000 лм, WXGA" },
            new() { InventoryNumber = "INV-0101", Name = "Ноутбук Lenovo ThinkPad E14", Category = "Ноутбуки", Description = "Windows 11, Office, зарядное устройство" },
            new() { InventoryNumber = "INV-0102", Name = "Ноутбук Lenovo ThinkPad E14", Category = "Ноутбуки", Description = "Второй экземпляр" },
            new() { InventoryNumber = "INV-0103", Name = "Ноутбук HP ProBook 450", Category = "Ноутбуки" },
            new() { InventoryNumber = "INV-0201", Name = "Фотоаппарат Canon EOS 250D", Category = "Фото и видео", Description = "Объектив 18–55 мм, карта 64 ГБ" },
            new() { InventoryNumber = "INV-0202", Name = "Видеокамера Sony HDR-CX405", Category = "Фото и видео" },
            new() { InventoryNumber = "INV-0301", Name = "Радиосистема Shure BLX24 с микрофоном", Category = "Звук" },
            new() { InventoryNumber = "INV-0302", Name = "Акустическая система JBL EON 710", Category = "Звук", State = EquipmentState.Maintenance, Description = "Замена динамика" },
            new() { InventoryNumber = "INV-0401", Name = "Документ-камера AVerVision U50", Category = "Проекторы" },
            new() { InventoryNumber = "INV-0501", Name = "Планшет Samsung Galaxy Tab A7", Category = "Планшеты", State = EquipmentState.WrittenOff, Description = "Разбит экран, списан" }
        };
        db.Equipment.AddRange(eq);

        var emp = new List<Employee>
        {
            new() { FullName = "Соколова Ирина Павловна", Department = "Учебная часть", Email = "sokolova@college.local" },
            new() { FullName = "Громов Андрей Николаевич", Department = "Кафедра информатики", Email = "gromov@college.local" },
            new() { FullName = "Белова Татьяна Сергеевна", Department = "Кафедра экономики" },
            new() { FullName = "Захаров Максим Олегович", Department = "Отдел воспитательной работы" },
            new() { FullName = "Ким Елена Викторовна", Department = "Кафедра иностранных языков", Email = "kim@college.local" },
            new() { FullName = "Федоров Павел Игоревич", Department = "Пресс-служба" },
            new() { FullName = "Тарасова Ольга Анатольевна", Department = "Приёмная комиссия" },
            new() { FullName = "Мельников Артём Валерьевич", Department = "ИТ-отдел", Email = "melnikov@college.local" }
        };
        db.Employees.AddRange(emp);
        db.SaveChanges();

        var today = now.Date;
        Booking B(int e, int p, int dayOffset, int fromHour, int toHour, string purpose, BookingStatus status = BookingStatus.Active, string? cancel = null, int toDayOffset = -1)
        {
            var start = today.AddDays(dayOffset).AddHours(fromHour);
            var end = today.AddDays(toDayOffset < 0 ? dayOffset : toDayOffset).AddHours(toHour);
            var created = start.AddDays(-3);
            var b = new Booking
            {
                EquipmentId = eq[e].Id, EmployeeId = emp[p].Id, StartAt = start, EndAt = end, Purpose = purpose,
                Status = status, CreatedAt = created,
                CancelledAt = status == BookingStatus.Cancelled ? created.AddDays(1) : null, CancelReason = cancel
            };
            b.History.Add(new BookingHistory { Action = BookingAction.Created, Date = created, Comment = purpose });
            if (status == BookingStatus.Cancelled)
                b.History.Add(new BookingHistory { Action = BookingAction.Cancelled, Date = created.AddDays(1), Comment = cancel });
            if (status == BookingStatus.Completed)
                b.History.Add(new BookingHistory { Action = BookingAction.Completed, Date = end, Comment = "Завершено автоматически по окончании периода" });
            return b;
        }

        db.Bookings.AddRange(
            // прошедшие
            B(0, 0, -6, 9, 12, "Педсовет: презентация отчёта", BookingStatus.Completed),
            B(2, 1, -5, 10, 14, "Открытый урок по программированию", BookingStatus.Completed),
            B(5, 5, -4, 13, 18, "Фотосъёмка дня открытых дверей", BookingStatus.Completed),
            B(7, 3, -3, 15, 19, "Концерт ко дню учителя", BookingStatus.Completed),
            B(1, 2, -2, 9, 11, "Защита курсовых работ", BookingStatus.Cancelled, "Защита перенесена"),
            // будущие и текущие
            B(0, 1, 1, 10, 12, "Лекция по сетевым технологиям"),
            B(0, 4, 1, 12, 14, "Урок английского: видеоматериалы"),
            B(2, 1, 2, 8, 18, "Олимпиада по программированию"),
            B(3, 1, 2, 8, 18, "Олимпиада по программированию"),
            B(6, 5, 3, 9, 17, "Видеосъёмка интервью с выпускниками"),
            B(5, 5, 3, 9, 17, "Фото для сайта колледжа"),
            B(9, 2, 4, 14, 16, "Семинар по бухучёту: работа с документами"),
            B(4, 6, 5, 9, 9, "Выездная работа приёмной комиссии", toDayOffset: 7),
            B(1, 0, 6, 10, 13, "Совещание с работодателями", BookingStatus.Cancelled, "Совещание проведено онлайн")
        );
        db.SaveChanges();
    }
}
