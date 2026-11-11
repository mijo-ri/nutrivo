using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Foods.DeleteFood;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods.DeleteFood;

public sealed class DeleteFoodEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapDelete("/foods/{id}", HandleAsync)
            .WithName("DeleteFood")
            .WithTags(Tags.Foods)
            .WithSummary("Permanently deletes a food and its catalogue serving sizes.")
            .WithDescription(
                "Deletes the food even when previously logged. Historical diary entries, "
                + "food and serving snapshots, consumed nutrition and totals remain unchanged. "
                + "Unknown or already deleted food IDs return 404."
            )
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
        Guid id,
        ICommandHandler<DeleteFoodCommand, FoodId> handler,
        CancellationToken cancellationToken
    )
    {
        var result = await handler.HandleAsync(
            new DeleteFoodCommand(id),
            cancellationToken
        );
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var notFound = result.Error == DeleteFoodErrors.NotFound;
        return TypedResults.Problem(
            statusCode: notFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest,
            title: notFound ? "Food not found." : "Invalid food ID.",
            detail: result.Error!.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = result.Error.Code
            }
        );
    }
}
