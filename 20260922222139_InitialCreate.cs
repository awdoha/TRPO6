// ВНИМАНИЕ: файл АВТОГЕНЕРИРОВАН командой `dotnet ef migrations add InitialCreate` и вручную не правится
// (изменилась модель - создаётся новая миграция). Комментарии добавлены только для защиты.
// Миграция = «инструкция» по созданию/изменению схемы БД. Применяется в Program.cs строкой context.Database.Migrate() при старте.
// Пространство имён System: тип DateTime используется в описании столбца Date.
using System;
// Пространство имён миграций EF Core: Migration, MigrationBuilder, ReferentialAction.
using Microsoft.EntityFrameworkCore.Migrations;

// Включает проверку nullable-ссылочных типов в режиме «отключено» для сгенерированного кода.
#nullable disable

// Пространство имён миграций проекта (папка Migrations).
namespace Lab6.Migrations
{
    /// <inheritdoc />
    // Класс миграции InitialCreate (первая миграция). partial - вторая часть лежит в файле .Designer.cs. Имя с меткой времени 20260922222139 задаёт порядок применения.
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        // Up = «применить»: создаёт таблицы. Вызывается при Database.Migrate(), если миграция ещё не отмечена в служебной таблице __EFMigrationsHistory.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Создаём таблицу Categories (сущность Category).
            migrationBuilder.CreateTable(
                // Имя таблицы = имя DbSet<Category> Categories в NoteContext.
                name: "Categories",
                // Описание столбцов.
                columns: table => new
                {
                    // Столбец Id: целое, не null; ниже автонумерация (Category.Id).
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        // Автоинкремент SQLite: Id назначает БД при INSERT.
                        .Annotation("Sqlite:Autoincrement", true),
                    // Столбец Title: текст до 100 символов (из [StringLength(100)]), обязательный (из [Required]).
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    // Столбец Description: текст, обязательный (из [Required]).
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                // Ограничения таблицы.
                constraints: table =>
                {
                    // Первичный ключ по Id.
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            // Создаём таблицу Notes (сущность Note).
            migrationBuilder.CreateTable(
                // Имя таблицы = DbSet<Note> Notes.
                name: "Notes",
                // Столбцы таблицы.
                columns: table => new
                {
                    // Id заметки с автонумерацией.
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        // Автоинкремент SQLite.
                        .Annotation("Sqlite:Autoincrement", true),
                    // Заголовок до 200 символов, обязательный.
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    // Текст заметки, обязательный.
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    // Дата: SQLite хранит DateTime как текст.
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                // Ограничения таблицы.
                constraints: table =>
                {
                    // Первичный ключ по Id.
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });

            // [EF] Создаём ПРОМЕЖУТОЧНУЮ таблицу связи many-to-many (Fluent API UsingEntity в NoteContext): одна строка = «заметка N принадлежит категории C».
            migrationBuilder.CreateTable(
                // Имя таблицы задано в NoteContext: .ToTable("NoteCategory").
                name: "NoteCategory",
                // Два столбца-ссылки.
                columns: table => new
                {
                    // Id категории (внешний ключ на Categories).
                    CategoriesId = table.Column<int>(type: "INTEGER", nullable: false),
                    // Id заметки (внешний ключ на Notes).
                    NotesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                // Ограничения таблицы.
                constraints: table =>
                {
                    // Составной первичный ключ (пара не может повторяться: одна категория у заметки только один раз).
                    table.PrimaryKey("PK_NoteCategory", x => new { x.CategoriesId, x.NotesId });
                    // Внешний ключ на Categories...
                    table.ForeignKey(
                        // Имя внешнего ключа.
                        name: "FK_NoteCategory_Categories_CategoriesId",
                        // Столбец этой таблицы.
                        column: x => x.CategoriesId,
                        // Родительская таблица.
                        principalTable: "Categories",
                        // Родительский столбец.
                        principalColumn: "Id",
                        // При удалении категории её строки в NoteCategory удаляются автоматически.
                        onDelete: ReferentialAction.Cascade);
                    // Внешний ключ на Notes...
                    table.ForeignKey(
                        // Имя внешнего ключа.
                        name: "FK_NoteCategory_Notes_NotesId",
                        // Столбец этой таблицы.
                        column: x => x.NotesId,
                        // Родительская таблица.
                        principalTable: "Notes",
                        // Родительский столбец.
                        principalColumn: "Id",
                        // При удалении заметки её строки в NoteCategory удаляются автоматически (это использует NoteController.Remove).
                        onDelete: ReferentialAction.Cascade);
                });

            // Индекс для быстрого поиска категорий по заметке (по NotesId; по CategoriesId индексом служит составной PK).
            migrationBuilder.CreateIndex(
                // Имя индекса.
                name: "IX_NoteCategory_NotesId",
                // Таблица.
                table: "NoteCategory",
                // Индексируемый столбец.
                column: "NotesId");
        } // конец Up

        /// <inheritdoc />
        // Down = «откатить»: удаляет всё, что создал Up (запускается при `dotnet ef database update 0`).
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Сначала удаляем таблицу связей (она ссылается на две остальные).
            migrationBuilder.DropTable(
                name: "NoteCategory");

            // Затем таблицу категорий.
            migrationBuilder.DropTable(
                name: "Categories");

            // И таблицу заметок.
            migrationBuilder.DropTable(
                name: "Notes");
        } // конец Down
    } // конец класса InitialCreate
} // конец пространства имён Lab6.Migrations
