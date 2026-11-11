using EquipmentBooking.Forms;
using EquipmentBooking.Models;
using EquipmentBooking.Services;

namespace EquipmentBooking.Tests;

/// <summary>Текущие и завершённые брони, фильтры, доступность на период, каталог, сохранение.</summary>
public class SearchAndCatalogTests : IDisposable
{
    private readonly TestEnv _env = new();
    private readonly Equipment _projector;
    private readonly Equipment _laptop;
    private readonly Employee _sokolova;
    private readonly Employee _gromov;
    private static readonly DateTime Tomorrow = TestEnv.Today.AddDays(1);

    public SearchAndCatalogTests()
    {
        _projector = _env.AddEquipment("INV-0001", "Проектор Epson EB-X51");
        _laptop = _env.AddEquipment("INV-0101", "Ноутбук Lenovo ThinkPad", category: "Ноутбуки");
        _env.AddEquipment("INV-0302", "Колонка JBL EON", EquipmentState.Maintenance, "Звук");
        _sokolova = _env.AddEmployee("Соколова Ирина Павловна");
        _gromov = _env.AddEmployee("Громов Андрей Николаевич");

        // проектор: завтра 10–12 (Соколова); ноутбук: завтра 14–16 (Громов), отменённая бронь проектора 15–16
        Create(_projector.Id, _sokolova.Id, 10, 12);
        Create(_laptop.Id, _gromov.Id, 14, 16);
        var cancelled = Create(_projector.Id, _gromov.Id, 15, 16);
        using var db = _env.Db();
        new BookingService(db, _env.Clock).Cancel(cancelled, "Перенос");
    }

    public void Dispose() => _env.Dispose();

    private int Create(int equipmentId, int employeeId, int from, int to)
    {
        using var db = _env.Db();
        return new BookingService(db, _env.Clock).Create(new BookingForm
        {
            EquipmentId = equipmentId, EmployeeId = employeeId, Purpose = "Занятие",
            StartAt = Tomorrow.AddHours(from), EndAt = Tomorrow.AddHours(to)
        }).CreatedId!.Value;
    }

    private List<Booking> Search(BookingFilter f)
    {
        using var db = _env.Db();
        return new BookingService(db, _env.Clock).Search(f);
    }

    [Fact]
    public void ТекущиеБрони_ТолькоАктивныеСНеНаступившимОкончанием()
    {
        Assert.Equal(2, Search(new BookingFilter { Tab = "current" }).Count);
    }

    [Fact]
    public void ЗавершённыеБрони_ОтменённыеИЗавершённыеПослеОкончания()
    {
        Assert.Single(Search(new BookingFilter { Tab = "finished" }));   // пока только отменённая

        _env.Clock.Advance(TimeSpan.FromDays(2));   // обе активные брони закончились

        Assert.Equal(3, Search(new BookingFilter { Tab = "finished" }).Count);
        Assert.Empty(Search(new BookingFilter { Tab = "current" }));
    }

    [Fact]
    public void ФильтрПоСотрудникуИОборудованию_НаходитНужныеБрони()
    {
        Assert.Equal(2, Search(new BookingFilter { EmployeeId = _gromov.Id }).Count);
        Assert.Single(Search(new BookingFilter { EmployeeId = _gromov.Id, EquipmentId = _laptop.Id }));
    }

    [Fact]
    public void ФильтрПоДатеИСтатусу_НаходитНужныеБрони()
    {
        Assert.Equal(3, Search(new BookingFilter { Date = Tomorrow }).Count);
        Assert.Empty(Search(new BookingFilter { Date = Tomorrow.AddDays(1) }));
        Assert.Single(Search(new BookingFilter { Status = BookingStatus.Cancelled }));
    }

    [Fact]
    public void Доступность_ЗанятоеИНаОбслуживании_НеВходятВСвободные()
    {
        using var db = _env.Db();
        var (error, rows) = new BookingService(db, _env.Clock).CheckAvailability(Tomorrow.AddHours(11), Tomorrow.AddHours(13));

        Assert.Null(error);
        var free = rows.Where(r => r.IsFree).Select(r => r.Equipment.Id).ToList();
        Assert.Equal(new[] { _laptop.Id }, free);                     // проектор занят 10–12, колонка на обслуживании
        Assert.Contains("занято", rows.Single(r => r.Equipment.Id == _projector.Id).Reason);
    }

    [Fact]
    public void Доступность_ПериодСразуПослеБрони_ПроекторСвободен()
    {
        using var db = _env.Db();
        var (_, rows) = new BookingService(db, _env.Clock).CheckAvailability(Tomorrow.AddHours(12), Tomorrow.AddHours(13));

        Assert.True(rows.Single(r => r.Equipment.Id == _projector.Id).IsFree);   // 12:00 — граница, отменённая 15–16 не мешает
    }

    [Fact]
    public void Доступность_КонецРаньшеНачала_Ошибка()
    {
        using var db = _env.Db();
        var (error, _) = new BookingService(db, _env.Clock).CheckAvailability(Tomorrow.AddHours(13), Tomorrow.AddHours(11));

        Assert.NotNull(error);
    }

    [Fact]
    public void ПоискВКаталоге_ПоНазваниюБезУчётаРегистра()
    {
        using var db = _env.Db();
        var found = new CatalogService(db, _env.Clock).Search("ПРОЕКТОР");

        Assert.Equal(_projector.Id, Assert.Single(found).Id);
    }

    [Fact]
    public void ПоискВКаталоге_ПоИнвентарномуНомеруИСостоянию()
    {
        using var db = _env.Db();
        var catalog = new CatalogService(db, _env.Clock);

        Assert.Single(catalog.Search("inv-01"));
        Assert.Single(catalog.Search(null, EquipmentState.Maintenance));
    }

    [Fact]
    public void ДобавитьОборудование_ПовторИнвентарногоНомера_Отклоняется()
    {
        using var db = _env.Db();
        var result = new CatalogService(db, _env.Clock).Create(new Equipment { InventoryNumber = "inv-0001", Name = "Ещё проектор", Category = "Проекторы" });

        Assert.Equal(ErrorKind.Conflict, result.Kind);
    }

    [Fact]
    public void ДобавитьОборудование_ПустоеНазвание_Отклоняется()
    {
        using var db = _env.Db();
        var result = new CatalogService(db, _env.Clock).Create(new Equipment { InventoryNumber = "INV-0900", Name = " ", Category = "Проекторы" });

        Assert.Equal(ErrorKind.Invalid, result.Kind);
    }

    [Fact]
    public void СписатьОборудование_ЕстьТекущиеБрони_Отклоняется()
    {
        using var db = _env.Db();
        var result = new CatalogService(db, _env.Clock).ChangeState(_projector.Id, EquipmentState.WrittenOff);

        Assert.Equal(ErrorKind.Conflict, result.Kind);
        Assert.Contains("текущие брони", result.Error);
    }

    [Fact]
    public void СписанноеОборудование_ВернутьВРаботу_Отклоняется()
    {
        var old = _env.AddEquipment("INV-0501", "Планшет", EquipmentState.WrittenOff, "Планшеты");
        using var db = _env.Db();

        var result = new CatalogService(db, _env.Clock).ChangeState(old.Id, EquipmentState.Available);

        Assert.Equal("Списанное оборудование нельзя вернуть в работу", result.Error);
    }

    [Fact]
    public void ИзменитьОборудование_НеизвестныйId_ЗаписьНеНайдена()
    {
        using var db = _env.Db();
        var result = new CatalogService(db, _env.Clock).Update(new Equipment { Id = 999, InventoryNumber = "INV-1", Name = "X", Category = "Y" });

        Assert.Equal(ErrorKind.NotFound, result.Kind);
    }

    [Fact]
    public void Сохранение_БроньЧитаетсяНовымКонтекстом()
    {
        using var db = _env.Db();
        var b = new BookingService(db, _env.Clock).GetById(1);

        Assert.NotNull(b);
        Assert.Equal("Проектор Epson EB-X51", b!.Equipment!.Name);
        Assert.Equal("Соколова Ирина Павловна", b.Employee!.FullName);
    }
}
