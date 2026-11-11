# EquipmentBooking — модуль бронирования оборудования

Курсовой проект, тема №13: «Проектирование и разработка модуля бронирования оборудования».
ЧПОУ «Сургутский колледж предпринимательства», 2026. Версия **v1.0**.

Веб-приложение **ASP.NET Core Razor Pages** (.NET 10): каталог оборудования, проверка доступности на период,
создание и отмена броней (с причиной, без удаления), автоматическое завершение, текущие и завершённые брони,
фильтры, недельный график загрузки.

**Главное правило:** нельзя забронировать оборудование на период, пересекающийся с другой *активной* бронью
этого оборудования: `новоеНачало < существующийКонец && новыйКонец > существующееНачало`.
Бронь, начинающаяся ровно в момент окончания другой, допустима.

## Что нужно

| Программа | Версия |
|---|---|
| .NET SDK | 10.0 (`dotnet --version` → `10.x`) |
| Браузер | любой; интернет — для стилей Bootstrap (CDN) и пакетов NuGet при первой сборке |

## Запуск

```powershell
git clone https://github.com/Black287234/KP-Vit.git
cd KP-Vit
dotnet run --project EquipmentBooking
```
Открыть **http://localhost:5090**. В Visual Studio 2026 — `EquipmentBooking.sln`, F5.

## База данных

- SQLite через EF Core 10.0.15. При запуске `Database.Migrate()` создаёт `EquipmentBooking/booking.db` по миграции `InitialCreate`.
- `DbSeeder` добавляет 11 единиц оборудования, 8 сотрудников и 14 броней; даты броней считаются от дня первого запуска.
- Сброс к учебным данным: остановить приложение, удалить `booking.db`, запустить снова.
- Альтернатива: `dotnet tool install --global dotnet-ef`, затем `dotnet ef database update --project EquipmentBooking`.

## Тесты

```powershell
dotnet test
```
Ожидается: `Passed: 52, Failed: 0`. Тесты используют «остановленные часы» (`TimeProvider`) и SQLite в памяти.

## Основной сценарий

1. «Свободно ли?» — выбрать завтра 10:00–12:00 → список свободного оборудования.
2. «Забронировать на этот период» у документ-камеры → бронь создана.
3. Забронировать проектор INV-0001 на завтра 11:00–13:00 → отказ «уже забронировано с … по …».
4. Карточка брони → отмена с причиной → статус «Отменено».
5. «Брони» → вкладки «Текущие» и «Завершённые и отменённые»; «График» — загрузка на неделю.

Ручные проверки API — `EquipmentBooking/EquipmentBooking.http`.

## Структура

```
EquipmentBooking/
  Models/     Equipment, Employee, Booking, BookingHistory, перечисления состояний
  Data/       AppDbContext, DbSeeder, Migrations/
  Services/   BookingService, BookingRules, CatalogService, EmployeeService, OperationResult
  Forms/      формы и фильтры (BookingForm, BookingFilter, AvailabilityRow, WeekRow)
  Pages/      Razor Pages: Equipment/, Bookings/, Employees/, Availability, Schedule
EquipmentBooking.Tests/   xUnit
docs/                     пояснительная записка, презентация, таблица проверок, материалы к защите
tools/screenshots.py      прогон ручных проверок в браузере (GitHub Actions → workflow checks)
```

## Архив для сдачи

```powershell
git archive -o stage5.zip HEAD
```
