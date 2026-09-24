// Точка входа приложения (top-level statements: метода Main не видно, компилятор создаёт его сам). Выполняется один раз при `dotnet run`.
// Пространство имён Lab6.Data: NoteContext, INoteRepository, EFNoteRepository. Используется: регистрации DI ниже и SeedData.
using Lab6.Data;
// Пространство имён Lab6.Models: Note и Category. Используется: в SeedData (создание демо-данных).
using Lab6.Models;
// Пространство имён EF Core: методы UseSqlite и Database.Migrate.
using Microsoft.EntityFrameworkCore;

// Создаём «строителя» приложения: читает appsettings*.json, launchSettings (порт), настраивает Kestrel и DI-контейнер. args - аргументы командной строки.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// [DI] Регистрируем в контейнере сервисы MVC: контроллеры + представления Razor. Без этого MapControllerRoute не заработает.
builder.Services.AddControllersWithViews();

// Провайдер БД — SQLite вместо MS SQL Server из ТЗ: локального экземпляра SQL Server
// нет, а разворачивать его ради учебной сдачи нецелесообразно. SQLite даёт полноценный
// EF Core поверх реального файла БД (notes.db) и не требует установки сервера — решение
// подробно обосновано в О_РАБОТЕ.md.
// Читаем строку подключения "NoteContext" из appsettings.json (секция ConnectionStrings); если её нет (?? - «или»), берём запасной вариант "Data Source=notes.db".
// Файл notes.db создаётся рядом с проектом при первом запуске.
string connectionString = builder.Configuration.GetConnectionString("NoteContext")
    ?? "Data Source=notes.db";
// [DI][EF] Регистрируем контекст БД NoteContext (Data/NoteContext.cs) и говорим использовать SQLite. Время жизни по умолчанию - Scoped (один на HTTP-запрос).
// Для SQL Server здесь было бы options.UseSqlServer(connectionString) и другой NuGet-пакет.
builder.Services.AddDbContext<NoteContext>(options => options.UseSqlite(connectionString));
// [DI] Регистрируем: «когда просят INoteRepository - создай EFNoteRepository». AddScoped = один объект на один HTTP-запрос
// (тот же срок жизни, что у DbContext, который репозиторий получает в конструкторе). Используется: конструкторы NoteController и CategoryController.
builder.Services.AddScoped<INoteRepository, EFNoteRepository>();

// Готовим приложение: с этого момента список сервисов закрыт (после Build() добавлять сервисы нельзя).
var app = builder.Build();

// Применяем миграции и засеваем демонстрационные данные при пустой БД.
// [DI] Создаём отдельную «область» (scope): вне HTTP-запроса нет своего scope, а Scoped-сервисы (DbContext) можно получить только внутри него. using - по выходу область освободится, DbContext закроется (IDisposable).
using (var scope = app.Services.CreateScope())
{
    // Просим у контейнера объект NoteContext (GetRequiredService - если не зарегистрирован, будет исключение).
    var context = scope.ServiceProvider.GetRequiredService<NoteContext>();
    // [EF] Применяем к БД все миграции из папки Migrations (InitialCreate): если notes.db нет - создаёт файл и таблицы Notes, Categories, NoteCategory. Это не EnsureCreated: миграции учитывают историю изменений схемы.
    context.Database.Migrate();
    // Заполняем пустую БД демонстрационными данными (метод SeedData ниже).
    SeedData(context);
} // конец блока using: область и контекст освобождены

// Configure the HTTP request pipeline.
// Ниже строим конвейер обработки запроса (middleware): каждый запрос проходит их по очереди сверху вниз.
// Если приложение НЕ в режиме Development (например, Production)...
if (!app.Environment.IsDevelopment())
{
    // ...при необработанном исключении перенаправляем на /Home/Error (HomeController.Error, Views/Shared/Error.cshtml).
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // HSTS: браузер запоминает, что сайт нужно открывать только по HTTPS.
    app.UseHsts();
}

// Перенаправляет http:// на https:// (если настроен https-порт; профиль "http" в launchSettings.json его не задаёт, поэтому по http сайт работает).
app.UseHttpsRedirection();
// [Route] Включает маршрутизацию: определяет по URL, какой контроллер/действие вызвать.
app.UseRouting();

// Включает проверку прав доступа (атрибутов [Authorize]); в проекте авторизации нет, строка оставлена из шаблона.
app.UseAuthorization();

// Раздаёт статические файлы из wwwroot (css, js, bootstrap, favicon) по адресам вроде /css/site.css с версионированием.
app.MapStaticAssets();

// [Route] Единственный маршрут проекта. Шаблон {controller}/{action}/{id?}:
// /Note/Edit/5 -> NoteController.Edit(id=5); "/" -> Home/Index (значения по умолчанию после =); ? означает, что id необязателен.
app.MapControllerRoute(
    // Имя маршрута (используется, если ссылку нужно построить по имени).
    name: "default",
    // Сам шаблон URL: сегмент 1 - контроллер (по умолчанию Home), сегмент 2 - действие (по умолчанию Index), сегмент 3 - необязательный id.
    pattern: "{controller=Home}/{action=Index}/{id?}")
    // Подключает к маршруту статические ресурсы (нужно для asp-append-version в _Layout.cshtml).
    .WithStaticAssets();


// Запускаем веб-сервер Kestrel и ждём запросов; блокирует поток до Ctrl+C. Адрес покажет консоль: "Now listening on: http://localhost:5189".
app.Run();

// Наполняет базу демонстрационными данными, если она пуста — для удобства проверки.
// Локальная static-функция; вызывается выше из блока using после Migrate(). Параметр - контекст БД.
static void SeedData(NoteContext context)
{
    // Если в БД уже есть хоть одна заметка или категория (Any() = «есть ли хоть одна запись»), выходим: повторно не засеиваем.
    if (context.Notes.Any() || context.Categories.Any()) return;

    // Создаём три категории (Id проставит БД). Их увидит пользователь на /Category/All.
    var work = new Category { Title = "Работа", Description = "Рабочие задачи и встречи" };
    // Категория «Учёба».
    var study = new Category { Title = "Учёба", Description = "Заметки по учебным дисциплинам" };
    // Категория «Личное».
    var personal = new Category { Title = "Личное", Description = "Личные напоминания" };

    // [EF] Помечаем категории как «добавить» (пока только в памяти контекста).
    context.Categories.AddRange(work, study, personal);

    // Добавляем четыре заметки; Categories = [...] - связь many-to-many: EF сам заполнит таблицу NoteCategory.
    context.Notes.AddRange(
        // Заметка 1: категория «Учёба». Видна на /Note/All.
        new Note
        {
            Title = "Сдать практику №6",
            Description = "Подготовить ASP.NET Core MVC приложение Notes и защитить его",
            Date = new DateTime(2026, 9, 20),
            Categories = [study]
        },
        // Заметка 2: категория «Работа».
        new Note
        {
            Title = "Созвон с командой",
            Description = "Обсудить план спринта и распределить задачи",
            Date = new DateTime(2026, 9, 22),
            Categories = [work]
        },
        // Заметка 3: категория «Личное».
        new Note
        {
            Title = "Купить продукты",
            Description = "Молоко, хлеб, овощи на неделю",
            Date = new DateTime(2026, 9, 23),
            Categories = [personal]
        },
        // Заметка 4: сразу две категории («Учёба» и «Работа») - удобна для показа many-to-many и OR-фильтра.
        new Note
        {
            Title = "Повторить лекции по БД",
            Description = "Повторить материал по нормализации и индексам перед экзаменом",
            Date = new DateTime(2026, 9, 25),
            Categories = [study, work]
        });

    // [EF] Отправляем в БД все накопленные изменения одной транзакцией (INSERT в Categories, Notes, NoteCategory).
    context.SaveChanges();
} // конец SeedData
