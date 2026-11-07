namespace Encuestas.Data;

/// <summary>Una página de resultados junto con la información necesaria para paginar en la vista.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>Valores de paginación comunes a los listados.</summary>
public static class Paging
{
    public const int DefaultPageSize = 10;

    /// <summary>Corrige valores fuera de rango que lleguen desde la URL.</summary>
    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(page, 1), pageSize < 1 ? DefaultPageSize : pageSize);
}
