namespace EquipmentBooking.Services;

/// <summary>Вид ошибки: от него зависит, что увидит пользователь (и код ответа API).</summary>
public enum ErrorKind
{
    None,
    Invalid,   // неверные данные — 400
    NotFound,  // записи нет — 404
    Conflict   // нарушено правило предметной области — 409
}

/// <summary>
/// Результат действия сервиса. Ошибки пользователя не бросают исключений,
/// а возвращаются как понятный текст на русском языке.
/// </summary>
public class OperationResult
{
    public bool Success => Kind == ErrorKind.None;
    public ErrorKind Kind { get; private init; }
    public string? Error { get; private init; }

    /// <summary>Id созданной записи (для перехода на её страницу).</summary>
    public int? CreatedId { get; private init; }

    public static OperationResult Ok(int? createdId = null) => new() { Kind = ErrorKind.None, CreatedId = createdId };
    public static OperationResult Invalid(string error) => new() { Kind = ErrorKind.Invalid, Error = error };
    public static OperationResult NotFound(string error) => new() { Kind = ErrorKind.NotFound, Error = error };
    public static OperationResult Conflict(string error) => new() { Kind = ErrorKind.Conflict, Error = error };
}
