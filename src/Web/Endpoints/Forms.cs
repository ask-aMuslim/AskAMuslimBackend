using AskAMuslimBackend.Application.Forms.Commands.CreateForm;
using AskAMuslimBackend.Application.Forms.Commands.CreateFormSubmission;
using AskAMuslimBackend.Application.Forms.Queries.GetForms;
using AskAMuslimBackend.Application.Forms.Queries.GetFormSubmissions;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Forms : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetForms).WithName("GetForms").WithSummary("Get all forms");
        groupBuilder.MapGet(GetFormById, "{id}").WithName("GetForm").WithSummary("Get form by ID");
        groupBuilder.MapPost(CreateForm)
            .RequireAuthorization(Policies.ManageForms)
            .WithName("CreateForm");

        // Form submissions
        groupBuilder.MapGet(GetFormSubmissions, "{formId}/submissions")
            .RequireAuthorization(Policies.ViewForms)
            .WithName("GetFormSubmissions");
        
        groupBuilder.MapPost(CreateFormSubmission, "{formId}/submissions")
            .WithName("SubmitForm")
            .WithSummary("Submit form answers");
    }

    public async Task<IResult> GetForms(ISender sender)
    {
        var result = await sender.Send(new GetFormsQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetFormById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetFormByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateForm(ISender sender, CreateFormCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Forms)}/{result.Data}");
    }

    public async Task<IResult> GetFormSubmissions(ISender sender, Guid formId)
    {
        var result = await sender.Send(new GetFormSubmissionsQuery(formId));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateFormSubmission(ISender sender, Guid formId, CreateFormSubmissionCommand command)
    {
        if (formId != command.FormId) return Results.BadRequest();

        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Forms)}/{formId}/submissions/{result.Data}");
    }
}