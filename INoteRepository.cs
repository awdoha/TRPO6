// Пространство имён моделей: Note и Category. Используется: в сигнатурах методов.
using Lab6.Models;

// Пространство имён слоя данных (папка Data).
namespace Lab6.Data;

// Интерфейс репозитория (сигнатура задана заданием). Контроллеры знают только его, а не EF Core: принципы D (зависимость от абстракции)
// и O (можно подменить реализацию, не меняя контроллеры) из SOLID. Единственная реализация - EFNoteRepository.
// [DI] Регистрируется в Program.cs: AddScoped<INoteRepository, EFNoteRepository>. Используется: конструкторы NoteController и CategoryController.
public interface INoteRepository
{
    // Все заметки (с категориями). Используется: NoteController.All/Edit/Remove/Details/Export/Filter/FillStat.
    IEnumerable<Note> GetAllNotes();
    // Добавить заметку; false - дубликат. Используется: NoteController.Add (POST).
    bool AddNote(Note obj);
    // Удалить заметку; false - не найдена. Используется: NoteController.Remove.
    bool RemoveNote(Note obj);
    // Изменить заметку и её категории. Используется: NoteController.Edit (POST).
    bool UpdateNote(Note obj);

    // Все категории (с заметками). Используется: CategoryController (все действия) и NoteController (Add/Edit - для чекбоксов).
    IEnumerable<Category> GetCategories();
    // Добавить категорию; false - дубликат. Используется: CategoryController.Add (POST).
    bool AddCategory(Category category);
    // Удалить категорию. Используется: CategoryController.Remove.
    bool RemoveCategory(Category category);
    // Изменить категорию. Используется: CategoryController.Edit (POST).
    bool UpdateCategory(Category category);
} // конец интерфейса INoteRepository
