// Пространство имён System.Text.Json: JsonSerializer и JsonSerializerOptions. Используется: только в Export (выгрузка notes.json).
using System.Text.Json;
// Пространство имён Lab6.Data: тут лежит интерфейс INoteRepository. Используется: поле _repository и конструктор.
using Lab6.Data;
// Пространство имён Lab6.Models: классы Note и Category. Используется: во всех действиях и в параметрах.
using Lab6.Models;
// Пространство имён MVC: Controller, IActionResult, [HttpGet], [HttpPost], [ValidateAntiForgeryToken], File(), View().
using Microsoft.AspNetCore.Mvc;

// Все контроллеры проекта лежат в этом пространстве имён (папка Controllers).
namespace Lab6.Controllers;

// [MVC: C = Controller] Контроллер заметок. Имя "NoteController" -> в URL это сегмент "Note" (суффикс Controller отбрасывается).
// Используется: адреса /Note/All, /Note/Add, /Note/Edit/5, /Note/Remove/5, /Note/Details/5, /Note/Stat, /Note/Export, /Note/Filter.
// Кто вызывает: [Route] маршрутизатор из Program.cs (MapControllerRoute) по шаблону {controller}/{action}/{id?}.
public class NoteController : Controller
{
    // Ссылка на репозиторий (доступ к данным). Тип - ИНТЕРФЕЙС, а не EFNoteRepository: принцип D из SOLID (зависим от абстракции).
    // readonly: после конструктора поле не меняется. Используется: во всех действиях ниже (_repository.GetAllNotes() и т.д.).
    private readonly INoteRepository _repository;

    // [DI] Конструктор. Его вызывает не программист, а DI-контейнер ASP.NET Core на каждый HTTP-запрос:
    // он видит параметр INoteRepository, находит регистрацию AddScoped<INoteRepository, EFNoteRepository> (Program.cs) и подставляет готовый объект.
    public NoteController(INoteRepository repository)
    {
        // Сохраняем полученный репозиторий в поле, чтобы использовать его в действиях этого запроса.
        _repository = repository;
    } // конец конструктора NoteController

    // GET: /Note/All?n=0&sort=Title
    // [Route] Действие All: вызывается при переходе на /Note/All (кнопка «Заметки» на главной и в меню _Layout.cshtml).
    // Параметры n и sort model binding берёт из строки запроса (?n=2&sort=Title); если их нет, n=0, sort=null.
    // Используется: Views/Note/All.cshtml (форма «Сортировка/Показать» отправляет GET сюда же).
    public IActionResult All(int n, string sort)
    {
        // Берём все заметки из репозитория (с категориями, см. Include в EFNoteRepository.GetAllNotes) и сортируем/ограничиваем.
        var notes = SortAndLimit(_repository.GetAllNotes(), n, sort);

        // ViewData - словарь «контроллер -> представление» на один запрос. Кладём выбранную сортировку, чтобы форма в All.cshtml её запомнила.
        ViewData["Sort"] = sort;
        // Кладём число n, чтобы поле «Показать» в All.cshtml осталось заполненным.
        ViewData["N"] = n;
        // Передаём список заметок как МОДЕЛЬ в Views/Note/All.cshtml (там @model List<Note>). ToList() выполняет запрос окончательно.
        return View(notes.ToList());
    } // конец All

    // Атрибут: этот метод Add отвечает только на GET, т.е. когда пользователь открывает страницу (кнопка «Добавить заметку»).
    [HttpGet]
    // Действие Add (GET): показать пустую форму. URL: /Note/Add. Представление: Views/Note/Add.cshtml.
    public IActionResult Add()
    {
        // ViewBag - динамическая обёртка над ViewData. Кладём список категорий, из него Add.cshtml нарисует чекбоксы.
        ViewBag.Categories = _repository.GetCategories().ToList();
        // Передаём в форму новую пустую заметку с сегодняшней датой (Add.cshtml: @model Note, поле Date).
        return View(new Note { Date = DateTime.Now });
    } // конец Add (GET)

    // Атрибут: этот метод Add срабатывает только на POST - когда форма в Add.cshtml отправлена кнопкой «Сохранить».
    [HttpPost]
    // Защита от CSRF: проверяет скрытый токен, который tag helper <form asp-action> вставляет в форму автоматически. Без токена - 400.
    [ValidateAntiForgeryToken]
    // Действие Add (POST). Model binding: поля формы (Title, Description, Date) складываются в obj, чекбоксы name="categoryIds" - в массив categoryIds.
    // int[]? - массив может быть null, если не отмечен ни один чекбокс.
    public IActionResult Add(Note obj, int[]? categoryIds)
    {
        // [ModelState] Проверка DataAnnotations из Models/Note.cs ([Required], [StringLength]): если есть ошибки - форма показывается снова.
        if (!ModelState.IsValid)
        {
            // Список категорий нужно положить заново: ViewBag живёт один запрос, а форма перерисовывается.
            ViewBag.Categories = _repository.GetCategories().ToList();
            // Возвращаем ту же форму с введёнными значениями и сообщениями об ошибках (asp-validation-for в Add.cshtml).
            return View(obj);
        }

        // Превращаем отмеченные Id в «заглушки» Category с одним заполненным Id (см. BuildCategoryStubs ниже).
        obj.Categories = BuildCategoryStubs(categoryIds);
        // Просим репозиторий добавить заметку; false означает «такая уже есть» (дедупликация в EFNoteRepository.AddNote).
        bool added = _repository.AddNote(obj);

        // Если добавить не вышло (дубликат)...
        if (!added)
        {
            // ...добавляем общую ошибку (пустой ключ = не к полю). Её покажет <div asp-validation-summary> в Add.cshtml.
            ModelState.AddModelError(string.Empty, "Такая заметка уже существует (совпадает Id либо все остальные поля).");
            // Снова кладём категории для чекбоксов.
            ViewBag.Categories = _repository.GetCategories().ToList();
            // Показываем форму с ошибкой, введённые данные сохраняются.
            return View(obj);
        }

        // После успешного добавления показываем страницу со статистикой.
        // Считаем статистику и кладём её в ViewData (Count, Date, Titles) для Stat.cshtml.
        FillStat();
        // Рендерим представление Stat.cshtml напрямую (без редиректа) - по заданию после добавления видна сводка.
        return View("Stat");
    } // конец Add (POST)

    // Атрибут: только GET - открытие страницы редактирования по ссылке «Изменить» (All.cshtml, Details.cshtml).
    [HttpGet]
    // Действие Edit (GET). URL: /Note/Edit/5 - число 5 из маршрута {id?} попадает в параметр id.
    public IActionResult Edit(int id)
    {
        // Ищем заметку с нужным Id среди всех (LINQ FirstOrDefault; лямбда n => n.Id == id - условие поиска). Категории уже подгружены Include.
        var note = _repository.GetAllNotes().FirstOrDefault(n => n.Id == id);
        // Нет такой заметки - ответ 404 (страница не найдена).
        if (note is null) return NotFound();

        // Все категории для списка чекбоксов в Edit.cshtml.
        ViewBag.Categories = _repository.GetCategories().ToList();
        // Id уже выбранных категорий - Edit.cshtml по ним ставит галочки (checked="@selected.Contains(...)").
        ViewBag.SelectedCategoryIds = note.Categories.Select(c => c.Id).ToList();
        // Передаём заметку как модель в Views/Note/Edit.cshtml.
        return View(note);
    } // конец Edit (GET)

    // Атрибут: только POST - отправка формы редактирования кнопкой «Сохранить изменения».
    [HttpPost]
    // Защита от подделки запроса (токен вставлен в форму Edit.cshtml тегом <form asp-action="Edit">).
    [ValidateAntiForgeryToken]
    // Действие Edit (POST). Id приходит из скрытого поля <input type="hidden" asp-for="Id"> формы; остальное - как в Add.
    public IActionResult Edit(Note obj, int[]? categoryIds)
    {
        // Валидация DataAnnotations; при ошибке возвращаем форму.
        if (!ModelState.IsValid)
        {
            // Категории для чекбоксов заново.
            ViewBag.Categories = _repository.GetCategories().ToList();
            // Оставляем отмеченным то, что пользователь уже выбрал (?? [] - если ничего не выбрано, пустой список).
            ViewBag.SelectedCategoryIds = categoryIds?.ToList() ?? [];
            // Показываем форму с ошибками.
            return View(obj);
        }

        // Заглушки категорий из отмеченных чекбоксов.
        obj.Categories = BuildCategoryStubs(categoryIds);
        // Репозиторий обновляет заметку и её категории.
        bool updated = _repository.UpdateNote(obj);
        // Если заметки с таким Id нет - 404.
        if (!updated) return NotFound();

        // [PRG] После сохранения делаем редирект на список: браузер выполнит GET /Note/All (nameof(All) = "All"), F5 не повторит POST.
        return RedirectToAction(nameof(All));
    } // конец Edit (POST)

    // Действие Remove: URL /Note/Remove/5 (кнопка «Удалить» в All.cshtml/Details.cshtml после confirm(...) в браузере).
    public IActionResult Remove(int id)
    {
        // Находим заметку по Id.
        var note = _repository.GetAllNotes().FirstOrDefault(n => n.Id == id);
        // Если нашли - удаляем через репозиторий (is not null - проверка на null).
        if (note is not null) _repository.RemoveNote(note);
        // Возвращаемся на список заметок (редирект на /Note/All).
        return RedirectToAction(nameof(All));
    } // конец Remove

    // Действие Details: URL /Note/Details/5 (кнопка «Просмотр» в All.cshtml и Filter.cshtml, ссылка в Category/Details.cshtml).
    public IActionResult Details(int id)
    {
        // Ищем заметку (с категориями).
        var note = _repository.GetAllNotes().FirstOrDefault(n => n.Id == id);
        // Не нашли - 404.
        if (note is null) return NotFound();
        // Передаём заметку как модель в Views/Note/Details.cshtml.
        return View(note);
    } // конец Details

    // Действие Stat: URL /Note/Stat (кнопка «Статистика» на All.cshtml). Показывает сводку по заметкам.
    public IActionResult Stat()
    {
        // Заполняем ViewData: Count, Date, Titles.
        FillStat();
        // Показываем Views/Note/Stat.cshtml (модели нет, данные берутся из ViewData).
        return View();
    } // конец Stat

    // GET: /Note/Export?n=0&sort=Title — выгрузка того же набора, что и All, в JSON.
    // Действие Export: кнопка «Экспорт в JSON» в All.cshtml (asp-route-n и asp-route-sort передают текущие n и sort).
    public IActionResult Export(int n, string sort)
    {
        // Та же выборка, что и в All (сортировка + ограничение); ToList() сразу выполняет запрос.
        var notes = SortAndLimit(_repository.GetAllNotes(), n, sort).ToList();

        // Настройки сериализатора JSON.
        var options = new JsonSerializerOptions
        {
            // Красивый JSON с отступами (удобно читать).
            WriteIndented = true,
            // [ReferenceHandler.IgnoreCycles] Note -> Categories -> Category -> Notes -> Note ... образует цикл; эта настройка обрывает его, иначе будет исключение.
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
        };
        // Превращаем список заметок в строку JSON.
        string json = JsonSerializer.Serialize(notes, options);
        // Строку -> байты в кодировке UTF-8 (File принимает byte[]).
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        // [File(...)] Возвращаем не страницу, а файл: MIME-тип application/json, имя для скачивания notes.json (браузер предложит сохранить).
        return File(bytes, "application/json", "notes.json");
    } // конец Export

    // Фильтрация: заметка проходит, если в Title ИЛИ Description встречается
    // хотя бы одно из переданных слов (регистр не учитывается) — В3.1, OR-логика.
    // Атрибут: только GET (форма фильтра отправляется методом GET, слова видны в адресной строке).
    [HttpGet]
    // Действие Filter: URL /Note/Filter?searchWords=работа&searchWords=лекции. Model binding собирает одноимённые параметры в List<string>.
    // Используется: Views/Note/Filter.cshtml; ссылки «Фильтрация» в меню, на главной и на All.cshtml.
    public IActionResult Filter(List<string>? searchWords)
    {
        // ??= : если searchWords == null (страницу открыли без параметров), подставить пустой список.
        searchWords ??= [];
        // Очищаем введённые слова: отбрасываем пустые/пробельные и обрезаем пробелы по краям.
        var words = searchWords
            // Where - оставить только непустые слова.
            .Where(w => !string.IsNullOrWhiteSpace(w))
            // Select - для каждого слова обрезать пробелы.
            .Select(w => w.Trim())
            // ToList - выполнить запрос и получить список.
            .ToList();

        // Берём все заметки. Тип IEnumerable<Note> - дальше на него навешиваем фильтр.
        IEnumerable<Note> notes = _repository.GetAllNotes();
        // Если слов нет - показываем все заметки; фильтр применяем только когда слова введены.
        if (words.Count > 0)
        {
            // [Filter OR] words.Any(...) = «хотя бы одно слово подошло». Для заметки: слово есть в Title ИЛИ в Description.
            // Одно слово из двух совпало - заметка уже найдена (это OR). Для AND (В3.2) Any заменяют на All.
            notes = notes.Where(note => words.Any(w =>
                // Contains(w, OrdinalIgnoreCase) - подстрока без учёта регистра в заголовке...
                note.Title.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                // ...или в описании.
                note.Description.Contains(w, StringComparison.OrdinalIgnoreCase)));
        }

        // Кладём применённые слова в ViewData - Filter.cshtml покажет их в поле ввода и в строке «Применённые фильтры».
        ViewData["SearchWords"] = words;
        // Найденные заметки - модель для Views/Note/Filter.cshtml.
        return View(notes.ToList());
    } // конец Filter

    // Вспомогательный метод (не действие: private, поэтому URL для него нет). Используется: All и Export.
    // Сортирует по выбранному полю и (если n > 0) берёт первые n. Параметр sort приходит из ?sort=... и выпадающего списка в All.cshtml.
    private static IEnumerable<Note> SortAndLimit(IEnumerable<Note> notes, int n, string? sort)
    {
        // switch-выражение: выбираем сортировку по строке sort. nameof(Note.Title) даёт строку "Title" (защита от опечаток).
        IEnumerable<Note> sorted = sort switch
        {
            // ?sort=Title -> по заголовку.
            nameof(Note.Title) => notes.OrderBy(x => x.Title),
            // ?sort=Description -> по описанию.
            nameof(Note.Description) => notes.OrderBy(x => x.Description),
            // ?sort=Date -> по дате.
            nameof(Note.Date) => notes.OrderBy(x => x.Date),
            // Любое другое значение (или null) -> по Id.
            _ => notes.OrderBy(x => x.Id)
        };

        // n > 0 -> Take(n) берёт первые n записей (?n=2); n == 0 -> все.
        return n > 0 ? sorted.Take(n) : sorted;
    } // конец SortAndLimit

    // Вспомогательный метод. Используется: Add (POST) и Edit (POST).
    // Из отмеченных чекбоксов получаем Id, делаем «пустые» Category только с Id; настоящие объекты репозиторий подтянет из БД (AttachCategories).
    private static List<Category> BuildCategoryStubs(int[]? categoryIds)
        // (categoryIds ?? []) - если null, берём пустой массив; Select создаёт Category с этим Id; ToList - в список.
        => (categoryIds ?? []).Select(id => new Category { Id = id }).ToList();

    // Вспомогательный метод. Используется: Stat() и Add (POST). Готовит для Stat.cshtml три значения в ViewData.
    private void FillStat()
    {
        // Все заметки из БД в память.
        var notes = _repository.GetAllNotes().ToList();

        // Количество заметок -> Stat.cshtml: «Всего заметок».
        ViewData["Count"] = notes.Count;
        // Диапазон дат [самая ранняя, самая поздняя] -> Stat.cshtml: «Диапазон дат». Если заметок нет - MinValue.
        ViewData["Date"] = notes.Count > 0
            ? new[] { notes.Min(x => x.Date), notes.Max(x => x.Date) }
            : new[] { DateTime.MinValue, DateTime.MinValue };
        // Уникальные заголовки (Distinct), отсортированные по алфавиту -> Stat.cshtml: «Уникальные заголовки».
        ViewData["Titles"] = notes.Select(x => x.Title).Distinct().OrderBy(x => x).ToList();
    } // конец FillStat
} // конец класса NoteController
