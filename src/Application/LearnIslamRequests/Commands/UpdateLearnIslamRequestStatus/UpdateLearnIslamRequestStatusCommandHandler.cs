using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Commands.UpdateLearnIslamRequestStatus;

public class UpdateLearnIslamRequestStatusCommandHandler : IRequestHandler<UpdateLearnIslamRequestStatusCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;

    public UpdateLearnIslamRequestStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(UpdateLearnIslamRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.LearnIslamRequests
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
        {
            return Result<bool>.Failure(new[] { $"LearnIslamRequest with ID {request.Id} not found." });
        }

        entity.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}