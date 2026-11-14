using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Diaries.UpdateDiaryEntry;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Api.Endpoints.Diaries.UpdateDiaryEntry;

public sealed class UpdateDiaryEntryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPut("/diary-entries/{id}", HandleAsync)
            .WithName("UpdateDiaryEntry")
            .WithTags(Tags.Diaries)
            .WithSummary("Updates only the original quantity or serving count.")
            .WithDescription(
                "Exactly one positive quantity or servingCount matching the original amount mode is required. Preserves food, date, meal, capture order and snapshots. Recalculates from historical reference nutrition. Success commits before returning."
            )
            .ProducesProblem(400)
            .ProducesProblem(404)
            .ProducesProblem(500);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        UpdateDiaryEntryRequest request,
        ICommandHandler<UpdateDiaryEntryCommand, DiaryEntryId> handler,
        CancellationToken cancellationToken
    )
    {
        var result = await handler.HandleAsync(
            new UpdateDiaryEntryCommand(id, request.Quantity, request.ServingCount),
            cancellationToken
        );
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var missing = result.Error == UpdateDiaryEntryErrors.NotFound;
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
