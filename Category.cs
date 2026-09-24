// Пространство имён с атрибутами валидации. Используется: [Required] и [StringLength] ниже.
using System.ComponentModel.DataAnnotations;

// Пространство имён моделей (папка Models).
namespace Lab6.Models;

// Связанная сущность варианта В3.1 — категория заметок.
// В тексте задания у преподавателя здесь опечатка ("List<Card> Cards", скопировано
// из раздела FlashCards) — для Notes корректно List<Note> Notes.
// [MVC: M = Model][EF] Класс = таблица Categories. Используется: NoteContext (DbSet<Category>), EFNoteRepository, CategoryController, Views/Category/*.cshtml, чекбоксы в Note/Add и Note/Edit.
public class Category
{
    // Первичный ключ (EF находит его по имени Id). Используется: asp-route-id в ссылках, value чекбоксов categoryIds, скрытое поле Category/Edit.cshtml.
    public int Id { get; set; }

    // [DataAnnotations] Обязательное поле: без названия форма вернётся с ошибкой (ModelState.IsValid = false).
    [Required(ErrorMessage = "Введите название категории")]
    // Не длиннее 100 символов (в БД - maxLength 100 в миграции).
    [StringLength(100, ErrorMessage = "Название не должно превышать 100 символов")]
    // Название категории. Используется: Category/All, Details, Stat; подпись чекбокса в Note/Add и Note/Edit; колонка «Категории» в Note/All.
    public string Title { get; set; } = string.Empty;

    // Описание обязательно.
    [Required(ErrorMessage = "Введите описание категории")]
    // Описание категории. Используется: Category/All, Category/Details, Note/Details (после тире).
    public string Description { get; set; } = string.Empty;

    // Многие-ко-многим: у категории может быть несколько заметок.
    // [EF] Вторая сторона связи many-to-many (парное свойство - Note.Categories). Заполняется через Include(c => c.Notes) в GetCategories.
    // Используется: колонка «Заметок» (Notes.Count) в Category/All, список заметок в Category/Details.
    public List<Note> Notes { get; set; } = [];
} // конец класса Category
