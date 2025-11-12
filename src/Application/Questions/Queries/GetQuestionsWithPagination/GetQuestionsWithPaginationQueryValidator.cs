using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Questions.Queries.GetQuestionsWithPagination;

public class GetQuestionsWithPaginationQueryValidator : PaginationQueryValidator<GetQuestionsWithPaginationQuery>
{
    public GetQuestionsWithPaginationQueryValidator()
    {
        RuleFor(x => x.QuizId).NotEmpty();
    }
}
