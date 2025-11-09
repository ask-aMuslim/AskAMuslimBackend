using AskAMuslimBackend.Application.Categories.Commands.CreateCategory;
using AskAMuslimBackend.Application.Categories.Commands.DeleteCategory;
using AskAMuslimBackend.Application.Categories.Commands.UpdateCategory;
using AskAMuslimBackend.Application.Categories.Queries;
using AskAMuslimBackend.Application.Categories.Queries.GetCategoriesWithPagination;
using AskAMuslimBackend.Application.Categories.Queries.GetCategoryById;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Categories : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCategoriesWithPagination).WithName("GetCategories").WithSummary("Get categories");
        groupBuilder.MapGet(GetCategoryById, "{id}").WithName("GetCategory").WithSummary("Get a category by id");
        groupBuilder.MapPost(CreateCategory).RequireAuthorization().WithName("CreateCategory").WithSummary("Create a new category");
        groupBuilder.MapPut(UpdateCategory, "{id}").RequireAuthorization().WithName("UpdateCategory").WithSummary("Update a category");
        groupBuilder.MapDelete(DeleteCategory, "{id}").RequireAuthorization().WithName("DeleteCategory").WithSummary("Delete a category");
    }

    public async Task<Ok<PaginatedList<CategoryBriefDto>>> GetCategoriesWithPagination(ISender sender, [AsParameters] GetCategoriesWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<CategoryBriefDto>, NotFound>> GetCategoryById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetCategoryByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Created<Guid>> CreateCategory(ISender sender, CreateCategoryCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Categories)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateCategory(ISender sender, Guid id, UpdateCategoryCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<NoContent> DeleteCategory(ISender sender, Guid id)
    {
        await sender.Send(new DeleteCategoryCommand(id));
        return TypedResults.NoContent();
    }
}
