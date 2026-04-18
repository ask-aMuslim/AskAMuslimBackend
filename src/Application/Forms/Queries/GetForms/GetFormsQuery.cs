using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Forms.Queries.GetForms;

public record GetFormsQuery : IRequest<Result<List<FormDto>>>;

public class GetFormsQueryHandler : IRequestHandler<GetFormsQuery, Result<List<FormDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFormsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FormDto>>> Handle(GetFormsQuery request, CancellationToken cancellationToken)
    {
        var forms = await _context.Forms
            .Include(f => f.Fields)
                .ThenInclude(f => f.Options)
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
            .ToListAsync(cancellationToken);

        return Result<List<FormDto>>.Success(forms);
    }
}