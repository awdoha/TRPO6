// Пространство имён Lab6.Data: интерфейс INoteRepository. Используется: поле _repository и конструктор.
using Lab6.Data;
// Пространство имён Lab6.Models: класс Category. Используется: параметры действий и вспомогательные методы.
using Lab6.Models;
// Пространство имён MVC: Controller, IActionResult, [HttpGet], [HttpPost], [ValidateAntiForgeryToken].
using Microsoft.AspNetCore.Mvc;

// Пространство имён всех контроллеров проекта (папка Controllers).
namespace Lab6.Controllers;

// [MVC: C = Controller] Контроллер категорий. Имя "CategoryController" -> сегмент URL "Category".
// Используется: /Category/All, /Category/Add, /Category/Edit/3, /Category/Remove/3, /Category/Details/3, /Category/Stat.
// Вызывается маршрутизатором [Route] из Program.cs (MapControllerRoute). Кнопка «Категории» есть на главной и в меню _Layout.cshtml.
public class CategoryController : Controller
{
    // Репозиторий как ИНТЕРФЕЙС (принцип D из SOLID). Используется: во всех действиях этого контроллера.
    private readonly INoteRepository _repository;

    // [DI] Конструктор: DI-контейнер сам передаёт сюда EFNoteRepository (регистрация AddScoped в Program.cs).
    public CategoryController(INoteRepository repository)
    {
        // Запоминаем репозиторий в поле.
        _repository = repository;
    } // конец конструктора CategoryController

    // Действие All: /Category/All?n=..&sort=... Model binding берёт n и sort из строки запроса. Представление: Views/Category/All.cshtml.
    public IActionResult All(int n, string sort)
    {
        // Все категории (с заметками, Include в репозитории) -> сортировка и ограничение по n.
        var categories = SortAndLimit(_repository.GetCategories(), n, sort);

        // ViewData: запоминаем выбранную сортировку для выпадающего списка в All.cshtml.
        ViewData["Sort"] = sort;
        // ViewData: запоминаем n для поля «Показать» в All.cshtml.
        ViewData["N"] = n;
        // Список категорий - модель для All.cshtml (@model List<Category>).
        return View(categories.ToList());
    } // конец All

    // Атрибут: только GET - открытие страницы (кнопка «Добавить категорию» в All.cshtml).
    [HttpGet]
    // Действие Add (GET): показывает пустую форму Views/Category/Add.cshtml. Выражение-тело: => View(...) вместо { return ...; }.
    public IActionResult Add() => View(new Category());

    // Атрибут: только POST - форма Add.cshtml отправлена кнопкой «Сохранить».
    [HttpPost]
    // Проверка защитного токена CSRF (его добавляет <form asp-action="Add">).
    [ValidateAntiForgeryToken]
    // Действие Add (POST). Model binding заполняет obj из полей формы (Title, Description).
    public IActionResult Add(Category obj)
    {
        // [ModelState] Проверка DataAnnotations из Models/Category.cs ([Required], [StringLength]); при ошибках возвращаем форму.
        if (!ModelState.IsValid) return View(obj);

        // Добавляем через репозиторий; false = дубликат (совпал Id или Title+Description).
        bool added = _repository.AddCategory(obj);
        // Если не добавлено...
        if (!added)
        {
            // ...добавляем общую ошибку, её покажет asp-validation-summary в Add.cshtml.
            ModelState.AddModelError(string.Empty, "Такая категория уже существует (совпадает Id либо все остальные поля).");
            // Показываем форму снова.
            return View(obj);
        }

        // Считаем статистику для Category/Stat.cshtml.
        FillStat();
        // После добавления сразу показываем страницу статистики категорий (без редиректа).
        return View("Stat");
    } // конец Add (POST)

    // Атрибут: только GET - ссылка «Изменить» из All.cshtml/Details.cshtml.
    [HttpGet]
    // Действие Edit (GET): /Category/Edit/3, число 3 из маршрута попадает в id.
    public IActionResult Edit(int id)
    {
        // Ищем категорию по Id среди всех (LINQ FirstOrDefault).
        var category = _repository.GetCategories().FirstOrDefault(c => c.Id == id);
        // Не нашли - 404.
        if (category is null) return NotFound();
        // Категория - модель для формы Views/Category/Edit.cshtml.
        return View(category);
    } // конец Edit (GET)

    // Атрибут: только POST - кнопка «Сохранить изменения» в Edit.cshtml.
    [HttpPost]
    // Защитный токен CSRF.
    [ValidateAntiForgeryToken]
    // Действие Edit (POST): Id приходит из скрытого поля формы, Title/Description - из полей.
    public IActionResult Edit(Category obj)
    {
        // Валидация DataAnnotations; при ошибке показываем форму снова.
        if (!ModelState.IsValid) return View(obj);

        // Репозиторий обновляет запись; false - если Id не найден.
        bool updated = _repository.UpdateCategory(obj);
        // Нет такой категории - 404.
        if (!updated) return NotFound();

        // [PRG] Редирект на GET /Category/All после сохранения (nameof(All) = "All").
        return RedirectToAction(nameof(All));
    } // конец Edit (POST)

    // Действие Remove: /Category/Remove/3 (кнопка «Удалить» в All.cshtml/Details.cshtml после confirm).
    public IActionResult Remove(int id)
    {
        // Находим категорию по Id.
        var category = _repository.GetCategories().FirstOrDefault(c => c.Id == id);
        // Если нашли - удаляем; строки связи в NoteCategory удалятся каскадно (ON DELETE CASCADE, см. миграцию).
        if (category is not null) _repository.RemoveCategory(category);
        // Возвращаемся к списку категорий (редирект).
        return RedirectToAction(nameof(All));
    } // конец Remove

    // Действие Details: /Category/Details/3 (кнопка «Просмотр» в All.cshtml, ссылка в Note/Details.cshtml).
    public IActionResult Details(int id)
    {
        // Ищем категорию вместе с её заметками (Include в репозитории).
        var category = _repository.GetCategories().FirstOrDefault(c => c.Id == id);
        // Не нашли - 404.
        if (category is null) return NotFound();
        // Категория - модель Views/Category/Details.cshtml.
        return View(category);
    } // конец Details

    // Действие Stat: /Category/Stat (кнопка «Статистика» в Category/All.cshtml).
    public IActionResult Stat()
    {
        // Кладём Count и Titles в ViewData.
        FillStat();
        // Показываем Views/Category/Stat.cshtml.
        return View();
    } // конец Stat

    // Вспомогательный метод (private - для него нет URL). Используется: All. Сортировка и ограничение количества.
    private static IEnumerable<Category> SortAndLimit(IEnumerable<Category> categories, int n, string? sort)
    {
        // switch-выражение: выбор сортировки по строке sort (?sort=Title).
        IEnumerable<Category> sorted = sort switch
        {
            // ?sort=Title -> по названию.
            nameof(Category.Title) => categories.OrderBy(x => x.Title),
            // ?sort=Description -> по описанию.
            nameof(Category.Description) => categories.OrderBy(x => x.Description),
            // По умолчанию (или ?sort=Id) -> по Id.
            _ => categories.OrderBy(x => x.Id)
        };

        // n > 0 -> первые n записей; иначе все.
        return n > 0 ? sorted.Take(n) : sorted;
    } // конец SortAndLimit

    // Вспомогательный метод. Используется: Stat() и Add (POST). Готовит данные для Category/Stat.cshtml.
    private void FillStat()
    {
        // Загружаем все категории в память.
        var categories = _repository.GetCategories().ToList();

        // Число категорий -> Stat.cshtml: «Всего категорий».
        ViewData["Count"] = categories.Count;
        // Уникальные названия по алфавиту -> Stat.cshtml: «Уникальные названия».
        ViewData["Titles"] = categories.Select(x => x.Title).Distinct().OrderBy(x => x).ToList();
    } // конец FillStat
} // конец класса CategoryController
