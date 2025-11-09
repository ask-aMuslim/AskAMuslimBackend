using AskAMuslimBackend.Application.Certificates.Commands.CreateCertificate;
using AskAMuslimBackend.Application.Certificates.Commands.DeleteCertificate;
using AskAMuslimBackend.Application.Certificates.Queries;
using AskAMuslimBackend.Application.Certificates.Queries.GetCertificateById;
using AskAMuslimBackend.Application.Certificates.Queries.GetCertificatesByStudent;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class Certificates : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetCertificate, "{id}").WithName("GetCertificate").WithSummary("Get a certificate by id");
        groupBuilder.MapGet(GetByStudent, "by-student/{studentId}").RequireAuthorization().WithName("GetCertificatesByStudent").WithSummary("Get certificates for a student");
        groupBuilder.MapPost(CreateCertificate).RequireAuthorization().WithName("CreateCertificate").WithSummary("Issue a new certificate");
        groupBuilder.MapDelete(DeleteCertificate, "{id}").RequireAuthorization().WithName("DeleteCertificate").WithSummary("Delete a certificate");
    }

    public async Task<Results<Ok<CertificateDto>, NotFound>> GetCertificate(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetCertificateByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<CertificateDto>>> GetByStudent(ISender sender, Guid studentId)
    {
        var result = await sender.Send(new GetCertificatesByStudentQuery(studentId));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> CreateCertificate(ISender sender, CreateCertificateCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(Certificates)}/{id}", id);
    }

    public async Task<NoContent> DeleteCertificate(ISender sender, Guid id)
    {
        await sender.Send(new DeleteCertificateCommand(id));
        return TypedResults.NoContent();
    }
}
