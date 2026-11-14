using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Diaries.RemoveDiaryEntry;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Api.Endpoints.Diaries.RemoveDiaryEntry;

public sealed class RemoveDiaryEntryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapDelete("/diary-entries/{id}", HandleAsync)
            .WithName("RemoveDiaryEntry")
            .WithTags(Tags.Diaries)
            .WithSummary("Permanently removes only this diary entry.")
            .WithDescription(
                "Leaves catalogue foods, servings and other diary entries unchanged. Daily and meal totals reflect the committed removal. Missing or already deleted IDs return 404."
            )
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(500);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        ICommandHandler<RemoveDiaryEntryCommand, DiaryEntryId> handler,
        CancellationToken cancellationToken
    )
    {
        var result = await handler.HandleAsync(
            new RemoveDiaryEntryCommand(id),
            cancellationToken
        );
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var missing = result.Error == RemoveDiaryEntryErrors.NotFound;
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
