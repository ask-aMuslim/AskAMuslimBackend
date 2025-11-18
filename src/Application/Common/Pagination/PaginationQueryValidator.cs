using FluentValidation;

namespace AskAMuslimBackend.Application.Common.Pagination;

public class PaginationQueryValidator<T> : AbstractValidator<T> where T : IPaginationQuery
{
    public PaginationQueryValidator(int? maxPageSize = null)
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("PageSize at least greater than or equal to 1.");

        if (maxPageSize is { } max && max > 0)
        {
            RuleFor(x => x.PageSize)
                .LessThanOrEqualTo(max)
                .WithMessage($"PageSize must not exceed {max}.");
        }
    }
}
