// Пространство имён моделей: Note и Category.
using Lab6.Models;
// Пространство имён EF Core: методы Include, AsNoTracking и др.
using Microsoft.EntityFrameworkCore;

// Пространство имён слоя данных.
namespace Lab6.Data;

// Репозиторий работает через EF Core и локальную SQLite-базу (см. NoteContext).
// Реализация интерфейса INoteRepository (полиморфизм: контроллер видит интерфейс, а работает этот класс).
// [DI] Создаётся контейнером по регистрации AddScoped<INoteRepository, EFNoteRepository> (Program.cs) - один объект на HTTP-запрос.
public class EFNoteRepository : INoteRepository
{
    // Контекст БД для запросов. Получен из DI. readonly - присваивается только в конструкторе. Используется: во всех методах ниже.
    private readonly NoteContext _context;

    // [DI] Конструктор: контейнер видит параметр NoteContext (зарегистрирован AddDbContext) и передаёт его. Один и тот же контекст на запрос у репозитория и контроллера.
    public EFNoteRepository(NoteContext context)
    {
        // Запоминаем контекст в поле.
        _context = context;
    } // конец конструктора EFNoteRepository

    // Все заметки со списком категорий. Вызывается: NoteController.All, Edit (GET), Remove, Details, Export, Filter, FillStat.
    // Выражение-тело метода (=>): весь метод - одно выражение-запрос.
    public IEnumerable<Note> GetAllNotes()
        // Начало запроса к таблице Notes (запрос ещё не выполнен - IQueryable, отложенное выполнение).
        => _context.Notes
            // [EF: eager loading] Include - жадная загрузка: тем же SQL-запросом (JOIN через NoteCategory) подтягиваются Categories каждой заметки. Без Include список Categories был бы пуст.
            .Include(n => n.Categories)
            // Только чтение: EF не отслеживает объекты (быстрее). Изменения через них не сохранятся - для этого в Update/Remove объекты берутся заново.
            .AsNoTracking()
            // ToList() выполняет SQL СЕЙЧАС и возвращает готовый список в памяти. Дальше OrderBy/Where контроллера - уже LINQ to Objects (IEnumerable), а не SQL.
            .ToList();

    // Добавление заметки. Вызывается: NoteController.Add (POST). Возвращает false, если такая заметка уже есть.
    public bool AddNote(Note obj)
    {
        // Исключаем дублирование: совпадение Id либо совпадение всех остальных полей.
        // [EF] Any(...) - SQL "EXISTS": есть ли хоть одна запись, удовлетворяющая условию. Условие: тот же Id ИЛИ одновременно совпали Title, Description и Date.
        bool duplicate = _context.Notes.Any(n =>
            n.Id == obj.Id ||
            (n.Title == obj.Title && n.Description == obj.Description && n.Date == obj.Date));
        // Дубликат найден - ничего не добавляем; контроллер покажет ошибку в форме.
        if (duplicate) return false;

        // Создаём НОВЫЙ объект Note (а не берём obj из формы), чтобы не унаследовать чужой Id и не подсунуть EF «заглушки» категорий.
        var note = new Note
        {
            // Копируем заголовок из формы.
            Title = obj.Title,
            // Копируем описание из формы.
            Description = obj.Description,
            // Копируем дату из формы.
            Date = obj.Date
        };

        // Привязываем к заметке выбранные категории (настоящие объекты из БД по Id из чекбоксов) - см. AttachCategories ниже.
        AttachCategories(note, obj.Categories);

        // [EF] Помечаем заметку как «добавить» (Add) - пока только в памяти.
        _context.Notes.Add(note);
        // [EF] Записываем в БД: INSERT в Notes и строки в NoteCategory для каждой выбранной категории.
        _context.SaveChanges();
        // Успех.
        return true;
    } // конец AddNote

    // Удаление заметки. Вызывается: NoteController.Remove (кнопка «Удалить»).
    public bool RemoveNote(Note obj)
    {
        // Ищем отслеживаемую запись по Id вместе с категориями (Include нужен, чтобы EF корректно удалил строки связи в NoteCategory).
        var existing = _context.Notes
            // Жадная загрузка категорий заметки.
            .Include(n => n.Categories)
            // FirstOrDefault - первая подходящая запись или null; выполняет запрос.
            .FirstOrDefault(n => n.Id == obj.Id);
        // Не найдено - возвращаем false (контроллер всё равно сделает редирект).
        if (existing is null) return false;

        // [EF] Помечаем на удаление.
        _context.Notes.Remove(existing);
        // [EF] DELETE из Notes (и каскадно из NoteCategory).
        _context.SaveChanges();
        // Успех.
        return true;
    } // конец RemoveNote

    // Обновление заметки. Вызывается: NoteController.Edit (POST, кнопка «Сохранить изменения»).
    public bool UpdateNote(Note obj)
    {
        // Находим в БД существующую заметку вместе с категориями (отслеживаемую EF, чтобы изменения были замечены).
        var existing = _context.Notes
            // Жадная загрузка текущих категорий.
            .Include(n => n.Categories)
            // Ищем по Id из скрытого поля формы.
            .FirstOrDefault(n => n.Id == obj.Id);
        // Такой заметки нет - false, контроллер вернёт 404.
        if (existing is null) return false;

        // Переносим новые значения из формы в найденный объект: заголовок...
        existing.Title = obj.Title;
        // ...описание...
        existing.Description = obj.Description;
        // ...и дату.
        existing.Date = obj.Date;

        // Очищаем старые связи с категориями: EF удалит лишние строки из NoteCategory...
        existing.Categories.Clear();
        // ...и добавляем отмеченные в форме (EF вставит нужные строки).
        AttachCategories(existing, obj.Categories);

        // [EF] EF сам вычисляет разницу (Title, Description, Date, связи) и выполняет UPDATE/INSERT/DELETE.
        _context.SaveChanges();
        // Успех.
        return true;
    } // конец UpdateNote

    // Все категории вместе с заметками. Вызывается: CategoryController (все действия), NoteController.Add/Edit (чекбоксы).
    public IEnumerable<Category> GetCategories()
        // Запрос к таблице Categories (пока не выполнен).
        => _context.Categories
            // [EF: eager loading] Загружаем заметки каждой категории (нужно для колонки «Заметок» в Category/All и списка в Category/Details).
            .Include(c => c.Notes)
            // Только чтение, без отслеживания.
            .AsNoTracking()
            // Выполняем запрос и возвращаем список.
            .ToList();

    // Добавление категории. Вызывается: CategoryController.Add (POST).
    public bool AddCategory(Category category)
    {
        // Проверка дубля: тот же Id ИЛИ одновременно совпали Title и Description (SQL EXISTS).
        bool duplicate = _context.Categories.Any(c =>
            c.Id == category.Id ||
            (c.Title == category.Title && c.Description == category.Description));
        // Дубликат - false, контроллер покажет сообщение об ошибке.
        if (duplicate) return false;

        // Добавляем НОВЫЙ объект с копией полей (Id назначит БД).
        _context.Categories.Add(new Category
        {
            // Название из формы.
            Title = category.Title,
            // Описание из формы.
            Description = category.Description
        });
        // [EF] INSERT в таблицу Categories.
        _context.SaveChanges();
        // Успех.
        return true;
    } // конец AddCategory

    // Удаление категории. Вызывается: CategoryController.Remove.
    public bool RemoveCategory(Category category)
    {
        // Находим категорию по Id (с отслеживанием).
        var existing = _context.Categories.FirstOrDefault(c => c.Id == category.Id);
        // Нет такой - false.
        if (existing is null) return false;

        // Помечаем на удаление.
        _context.Categories.Remove(existing);
        // [EF] DELETE; строки в NoteCategory удаляются каскадом (ON DELETE CASCADE в миграции), сами заметки остаются.
        _context.SaveChanges();
        // Успех.
        return true;
    } // конец RemoveCategory

    // Изменение категории. Вызывается: CategoryController.Edit (POST).
    public bool UpdateCategory(Category category)
    {
        // Находим категорию по Id (Id пришёл из скрытого поля формы).
        var existing = _context.Categories.FirstOrDefault(c => c.Id == category.Id);
        // Не нашли - false (контроллер вернёт 404).
        if (existing is null) return false;

        // Обновляем название...
        existing.Title = category.Title;
        // ...и описание.
        existing.Description = category.Description;
        // [EF] UPDATE Categories.
        _context.SaveChanges();
        // Успех.
        return true;
    } // конец UpdateCategory

    // Подтягивает уже существующие в базе категории по Id, чтобы EF не пытался
    // создать их заново (у переданных из формы категорий заполнен только Id).
    // Вспомогательный метод (private). Используется: AddNote и UpdateNote.
    private void AttachCategories(Note note, IEnumerable<Category> categories)
    {
        // Берём Id категорий из «заглушек» (Distinct - без повторов) в список.
        var ids = categories.Select(c => c.Id).Distinct().ToList();
        // Ничего не выбрано - выходим.
        if (ids.Count == 0) return;

        // [EF] Запрос "WHERE Id IN (...)": настоящие Category из БД, отслеживаемые контекстом. ids.Contains(c.Id) превращается в SQL IN.
        var tracked = _context.Categories.Where(c => ids.Contains(c.Id)).ToList();
        // Перебираем найденные категории...
        foreach (var category in tracked)
        {
            // ...и добавляем в коллекцию заметки: при SaveChanges EF создаст строки в NoteCategory.
            note.Categories.Add(category);
        }
    } // конец AttachCategories
} // конец класса EFNoteRepository
