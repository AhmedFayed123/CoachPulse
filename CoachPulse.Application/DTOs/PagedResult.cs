namespace CoachPulse.Application.DTOs;

public sealed record PagedResult<T>([property: System.ComponentModel.DataAnnotations.Required] IReadOnlyList<T> Items,
    [property: System.ComponentModel.DataAnnotations.Required] int Page,
    [property: System.ComponentModel.DataAnnotations.Required] int PageSize,
    [property: System.ComponentModel.DataAnnotations.Required] int TotalCount)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount) =>
        new(items, page, pageSize, totalCount);
}
