namespace ServiceDesk.Application.Common;

public class PagedQuery
{
    public string? Search { get; set; }
    public bool IncludeInactive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public static class Paging
{
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
        => (Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize));
}
