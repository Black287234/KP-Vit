using EquipmentBooking.Services;

var builder = WebApplication.CreateBuilder(args);

// Шаблон Razor Pages: каждая страница — файл .cshtml и класс модели страницы рядом
builder.Services.AddRazorPages();
builder.Services.AddScoped<CatalogService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

// GET-запросы API для проверки (JSON)
app.MapGet("/api/equipment", (CatalogService catalog) => catalog.GetAll());
app.MapGet("/api/equipment/{id:int}", (int id, CatalogService catalog) =>
    catalog.GetById(id) is { } item
        ? Results.Ok(item)
        : Results.NotFound(new { error = $"Оборудование с Id = {id} не найдено" }));

app.Run();
