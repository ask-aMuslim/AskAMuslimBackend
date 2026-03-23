using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Commands.UpdateLearnIslamRequestLabel;

public class UpdateLearnIslamRequestLabelCommandHandler : IRequestHandler<UpdateLearnIslamRequestLabelCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateLearnIslamRequestLabelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateLearnIslamRequestLabelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.LearnIslamRequests
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            return Result<bool>.Failure(new[] { $"LearnIslamRequest with ID {request.Id} not found." });
        }

        entity.Label = request.Label;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}