using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Forms.Queries.GetForms;

public record GetFormsQuery : IRequest<Result<PaginatedList<FormDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsPublished { get; init; }
}

public class GetFormsQueryHandler : IRequestHandler<GetFormsQuery, Result<PaginatedList<FormDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetFormsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<FormDto>>> Handle(GetFormsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Forms
            .AsNoTracking()
            .Include(f => f.Fields)
                .ThenInclude(f => f.Options)
            .AsQueryable();

        if (request.IsPublished.HasValue)
        {
            query = query.Where(f => f.IsPublished == request.IsPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(f => f.Title.ToLower().Contains(searchLower) || (f.Description != null && f.Description.ToLower().Contains(searchLower)));
        }

        var forms = await query
            .OrderByDescending(f => f.Created)
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
                    IsMultiSelect = field.IsMultiSelect,
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
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<FormDto>>.Success(forms);
    }
}
