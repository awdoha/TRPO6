// Пространство имён System.Diagnostics: класс Activity. Используется: в Error() для идентификатора запроса.
using System.Diagnostics;
// Пространство имён MVC: Controller, IActionResult, [ResponseCache].
using Microsoft.AspNetCore.Mvc;
// Пространство имён Lab6.Models: ErrorViewModel. Используется: в Error().
using Lab6.Models;

// Пространство имён контроллеров проекта.
namespace Lab6.Controllers;

// Контроллер главной страницы. Сегмент URL "Home". Он же контроллер по умолчанию в шаблоне маршрута (Program.cs: {controller=Home}).
// Репозиторий не нужен - здесь только статические страницы.
public class HomeController : Controller
{
    // Действие Index: адрес "/" (корень сайта; по умолчанию Home/Index) и /Home/Index. Также ссылка «Главная» в меню и «На главную» на страницах.
    public IActionResult Index()
    {
        // Показываем Views/Home/Index.cshtml (три кнопки: Заметки, Категории, Фильтрация).
        return View();
    } // конец Index

    // Действие Privacy: /Home/Privacy - ссылка «Privacy» в подвале _Layout.cshtml (шаблонная страница).
    public IActionResult Privacy()
    {
        // Показываем Views/Home/Privacy.cshtml.
        return View();
    } // конец Privacy

    // Атрибут: запретить кэширование страницы ошибки (Duration=0, NoStore).
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    // Действие Error: /Home/Error. Его вызывает UseExceptionHandler("/Home/Error") из Program.cs при необработанном исключении (не в Development).
    public IActionResult Error()
    {
        // Передаём модели идентификатор запроса: Activity.Current?.Id, а если его нет - HttpContext.TraceIdentifier. Показывает Views/Shared/Error.cshtml.
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    } // конец Error
} // конец класса HomeController
