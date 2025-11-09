using FluentValidation;

namespace AskAMuslimBackend.Application.Questions.Queries.GetQuestionsWithPagination;

public class GetQuestionsWithPaginationQueryValidator : AbstractValidator<GetQuestionsWithPaginationQuery>
{
    public GetQuestionsWithPaginationQueryValidator()
    {
        RuleFor(x => x.QuizId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1);
    }
}
