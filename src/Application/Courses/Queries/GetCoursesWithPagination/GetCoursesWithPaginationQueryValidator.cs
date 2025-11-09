namespace AskAMuslimBackend.Application.Courses.Queries.GetCoursesWithPagination;

public class GetCoursesWithPaginationQueryValidator : AbstractValidator<GetCoursesWithPaginationQuery>
{
    public GetCoursesWithPaginationQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber at least greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize at least greater than or equal to 1.");
    }
}
