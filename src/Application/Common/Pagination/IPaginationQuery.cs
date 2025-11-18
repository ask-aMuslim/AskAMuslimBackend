namespace AskAMuslimBackend.Application.Common.Pagination;

public interface IPaginationQuery
{
    int PageNumber { get; }
    int PageSize { get; }
}
