using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Forms.Queries.GetForms;

public record GetFormByIdQuery(Guid Id) : IRequest<Result<FormDto>>;

public class GetFormByIdQueryHandler : IRequestHandler<GetFormByIdQuery, Result<FormDto>>
{
    private readonly IApplicationDbContext _context;

    public GetFormByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<FormDto>> Handle(GetFormByIdQuery request, CancellationToken cancellationToken)
    {
        var form = await _context.Forms
            .Include(f => f.Fields)
                .ThenInclude(f => f.Options)
            .Where(f => f.Id == request.Id)
            .Select(f => new FormDto
            {
                Id = f.Id,
                Title = f.Title,
                Description = f.Description,
                IsPublished = f.IsPublished,
                Fields = f.Fields.Select(field => new FormFieldDto
                {
                    Id = field.Id,
                    Label = field.Label,
                    Type = field.Type,
                    IsRequired = field.IsRequired,
                    Order = field.Order,
                    Options = field.Options.Select(opt => new FormFieldOptionDto
                    {
                        Id = opt.Id,
                        Label = opt.Label,
                        Value = opt.Value,
                        Order = opt.Order
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (form == null)
            return Result<FormDto>.Failure(new[] { "Form not found" });

        return Result<FormDto>.Success(form);
    }
}