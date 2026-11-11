using EquipmentBooking.Data;
using EquipmentBooking.Forms;
using EquipmentBooking.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Шаблон Razor Pages: каждая страница — файл .cshtml и класс модели страницы рядом
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Часы приложения. В тестах вместо системных подставляются «остановленные» часы
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

// База создаётся и обновляется миграциями при запуске; при пустой базе добавляются учебные данные
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db, TimeProvider.System.GetLocalNow().DateTime);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
// Неизвестный адрес → своя страница 404
app.UseStatusCodePagesWithReExecute("/NotFound");

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// ---------- API для проверки HTTP-запросами (файл EquipmentBooking.http) ----------
var api = app.MapGroup("/api");

api.MapGet("/equipment", (string? search, CatalogService catalog) => catalog.Search(search));
api.MapGet("/equipment/{id:int}", (int id, CatalogService catalog) =>
    catalog.GetById(id) is { } item ? Results.Ok(item) : Results.NotFound(new { error = $"Оборудование с Id = {id} не найдено" }));

api.MapGet("/availability", (DateTime from, DateTime to, BookingService bookings) =>
{
    var (error, rows) = bookings.CheckAvailability(from, to);
    return error != null
        ? Results.BadRequest(new { error })
        : Results.Ok(rows.Select(r => new { r.Equipment.Id, r.Equipment.InventoryNumber, r.Equipment.Name, r.IsFree, r.Reason }));
});

api.MapGet("/bookings", ([AsParameters] BookingFilter filter, BookingService bookings) =>
    bookings.Search(filter).Select(b => new
    {
        b.Id, Equipment = b.Equipment!.Name, Employee = b.Employee!.FullName, b.StartAt, b.EndAt,
        b.Purpose, Status = b.Status.ToString(), b.CancelReason
    }));

api.MapPost("/bookings", (BookingForm form, BookingService bookings) => ToHttp(bookings.Create(form)));
api.MapPost("/bookings/{id:int}/cancel", (int id, CancelRequest body, BookingService bookings) => ToHttp(bookings.Cancel(id, body.Reason)));

app.Run();

// Результат сервиса → HTTP-ответ: 200/201, 400, 404 или 409
static IResult ToHttp(OperationResult r) => r.Kind switch
{
    ErrorKind.None => r.CreatedId != null ? Results.Created($"/api/bookings/{r.CreatedId}", new { id = r.CreatedId }) : Results.Ok(new { message = "Выполнено" }),
    ErrorKind.NotFound => Results.NotFound(new { error = r.Error }),
    ErrorKind.Conflict => Results.Conflict(new { error = r.Error }),
    _ => Results.BadRequest(new { error = r.Error })
};

/// <summary>Тело запроса на отмену брони.</summary>
public record CancelRequest(string? Reason);

public partial class Program { }
