// Пространство имён моделей: Note и Category для DbSet.
using Lab6.Models;
// Пространство имён EF Core: DbContext, DbSet, ModelBuilder, DbContextOptions.
using Microsoft.EntityFrameworkCore;

// Пространство имён слоя данных.
namespace Lab6.Data;

// [EF] DbContext - «мост» между C#-классами и БД: одна «сессия» работы с базой, отслеживает изменения и превращает LINQ в SQL.
// DbContext реализует IDisposable: его создаёт и освобождает DI-контейнер в конце HTTP-запроса (Scoped, регистрация AddDbContext в Program.cs).
// Используется: EFNoteRepository (получает его в конструкторе), Program.cs (Migrate и SeedData), dotnet ef (миграции).
public class NoteContext : DbContext
{
    // [DI] Конструктор принимает настройки (провайдер SQLite и строка подключения), которые заданы в Program.cs через AddDbContext; base(options) передаёт их в DbContext.
    public NoteContext(DbContextOptions<NoteContext> options) : base(options)
    {
    } // конец конструктора NoteContext

    // [EF] DbSet<Note> = таблица Notes; по нему пишут LINQ-запросы (_context.Notes...). Set<Note>() - получить набор из контекста. Используется: EFNoteRepository, SeedData.
    public DbSet<Note> Notes => Set<Note>();
    // [EF] DbSet<Category> = таблица Categories. Используется: EFNoteRepository (GetCategories, AttachCategories и т.д.), SeedData.
    public DbSet<Category> Categories => Set<Category>();

    // Метод EF вызывается один раз при построении модели (при первом обращении к контексту): здесь настраиваем связи Fluent API.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Сохраняем стандартную настройку модели EF (соглашения по умолчанию).
        base.OnModelCreating(modelBuilder);

        // Связь many-to-many между Note и Category. Обе стороны уже содержат
        // навигационные коллекции (Note.Categories и Category.Notes), поэтому
        // EF Core 8+ способен настроить skip-навигацию неявно, но здесь связь
        // задаётся явно через Fluent API — так яснее видна промежуточная таблица.
        // [EF] Читается так: «у Note есть много Categories; у каждой Category есть много Notes; связь хранится в отдельной таблице NoteCategory».
        // UsingEntity создаёт промежуточную (join) таблицу с двумя внешними ключами (NotesId, CategoriesId) - она видна в миграции InitialCreate.
        modelBuilder.Entity<Note>()
            .HasMany(n => n.Categories)
            .WithMany(c => c.Notes)
            .UsingEntity(j => j.ToTable("NoteCategory"));
    } // конец OnModelCreating
} // конец класса NoteContext
