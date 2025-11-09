using FluentValidation;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelsWithPagination;

public class GetLevelsWithPaginationQueryValidator : AbstractValidator<GetLevelsWithPaginationQuery>
{
    public GetLevelsWithPaginationQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}
