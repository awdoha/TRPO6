// Пространство имён моделей проекта.
namespace Lab6.Models;

// Модель страницы ошибки (шаблонная). Создаётся в HomeController.Error() и передаётся в Views/Shared/Error.cshtml (@model ErrorViewModel).
public class ErrorViewModel
{
    // Идентификатор запроса для поиска в логах. string? - может быть null (Nullable reference types). Заполняется в HomeController.Error.
    public string? RequestId { get; set; }

    // Вычисляемое свойство только для чтения (=> выражение): true, если RequestId не пуст. Используется: Error.cshtml (@if (Model.ShowRequestId)).
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
} // конец класса ErrorViewModel
