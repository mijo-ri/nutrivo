using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Diaries.GetDiaryEntryById;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Api.Endpoints.Diaries.GetDiaryEntryById;

public sealed class GetDiaryEntryByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/diary-entries/{id}", HandleAsync)
            .WithName("GetDiaryEntryById")
            .WithTags(Tags.Diaries)
            .WithSummary("Reads an entry with its historical nutrition basis.")
            .WithDescription(
                "Independent of the food catalogue. Reference nutrients and quantities retain exact precision; consumed nutrition is rounded for display."
            )
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(500);
    }

    private static async Task<Results<Ok<DiaryEntryDetailResponse>, ProblemHttpResult>>
        HandleAsync(
            Guid id,
            IQueryHandler<GetDiaryEntryByIdQuery, DiaryEntryDetail> handler,
            CancellationToken cancellationToken
        )
    {
        var result = await handler.HandleAsync(
            new GetDiaryEntryByIdQuery(id),
            cancellationToken
        );
        if (result.IsSuccess)
        {
            return TypedResults.Ok(DiaryEntryDetailResponse.From(result.Value));
        }

        var missing = result.Error == GetDiaryEntryByIdErrors.NotFound;
        return TypedResults.Problem(
            statusCode: missing ? 404 : 400,
            title: missing ? "Diary entry not found." : "Invalid diary entry request.",
            detail: result.Error!.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = result.Error.Code
            }
        );
    }
}
