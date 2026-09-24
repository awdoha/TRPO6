// Пространство имён с атрибутами валидации ([Required], [StringLength], [Display]). Используется: над свойствами ниже.
using System.ComponentModel.DataAnnotations;

// Все модели проекта в пространстве имён Lab6.Models (папка Models).
namespace Lab6.Models;

// Основная сущность варианта В3.1 — заметка.
// [MVC: M = Model][EF] Класс = таблица Notes в БД (DbSet<Note> в NoteContext). Используется: NoteContext, EFNoteRepository, NoteController, все Views/Note/*.cshtml (@model).
public class Note
{
    // Первичный ключ: EF по имени "Id" сам делает его ключом с автонумерацией. Используется: ссылки asp-route-id, скрытое поле Edit.cshtml, поиск в контроллере.
    public int Id { get; set; }

    // [DataAnnotations] Поле обязательно; при пустом значении ModelState.IsValid = false, сообщение показывает asp-validation-for="Title".
    [Required(ErrorMessage = "Введите заголовок заметки")]
    // Максимум 200 символов (в БД - столбец TEXT с maxLength 200, см. миграцию). Ошибка отображается в форме Add/Edit.
    [StringLength(200, ErrorMessage = "Заголовок не должен превышать 200 символов")]
    // Заголовок. Авто-свойство (get; set;) = поле + аксессоры генерирует компилятор. = string.Empty - значение по умолчанию, чтобы не было null (Nullable). Используется: All, Details, Filter (поиск), Stat.
    public string Title { get; set; } = string.Empty;

    // Текст заметки обязателен (проверяется при POST на Add/Edit).
    [Required(ErrorMessage = "Введите текст заметки")]
    // Описание (текст) заметки. Используется: All (первые 80 символов), Details, Filter (второе поле поиска).
    public string Description { get; set; } = string.Empty;

    // Подпись для <label asp-for="Date"> в формах: вместо "Date" будет "Дата".
    [Display(Name = "Дата")]
    // Дата заметки; по умолчанию текущая (DateTime - значимый тип, struct). Используется: поле «Дата» в Add/Edit, колонка в All, диапазон в Stat, сортировка ?sort=Date.
    public DateTime Date { get; set; } = DateTime.Now;

    // Многие-ко-многим: у заметки может быть несколько категорий.
    // [EF] Навигационное свойство: EF по нему строит связь и промежуточную таблицу NoteCategory (см. NoteContext.OnModelCreating).
    // Заполняется только через Include(n => n.Categories) в EFNoteRepository. Используется: колонка «Категории» в All/Filter, список в Details, галочки в Edit.
    public List<Category> Categories { get; set; } = [];
} // конец класса Note
