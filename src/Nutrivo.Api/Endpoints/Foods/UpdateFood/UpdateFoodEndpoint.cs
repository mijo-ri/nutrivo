using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Foods.UpdateFood;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods.UpdateFood;

public sealed class UpdateFoodEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPut("/foods/{id}", HandleAsync)
            .WithName("UpdateFood")
            .WithTags(Tags.Foods)
            .WithSummary("Updates a food with its nutrition basis and serving sizes.")
            .WithDescription(
                """
                Replaces all editable food fields. Reference quantity must be positive,
                in grams (1) or milliliters (2). No automatic unit conversion is performed.
                Nutrition and serving equivalent quantities refer to the submitted basis.
                A serving with an ID updates that food's existing serving; without an ID,
                a new serving is created. Omitted servings are removed. Missing, null or
                empty servingSizes removes all servings. Servings retain request order.
                Empty, duplicate and foreign serving IDs are invalid.
                Historical diary snapshots remain unchanged.
                """
            )
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<Ok<UpdateFoodResponse>, ProblemHttpResult>>
        HandleAsync(
            Guid id,
            UpdateFoodRequest request,
            ICommandHandler<UpdateFoodCommand, FoodId> handler,
            CancellationToken cancellationToken
        )
    {
        var command = new UpdateFoodCommand(
            id,
            request.Name,
            request.Brand,
            request.ReferenceQuantity,
            request.ReferenceUnit,
            request.Kilocalories,
            request.ProteinGrams,
            request.CarbohydratesGrams,
            request.FatGrams,
            request
                .ServingSizes?.Select(serving => serving is null
                    ? null
                    : new UpdateServingSizeInput(
                        serving.Id,
                        serving.Quantity,
                        serving.Unit,
                        serving.Label,
                        serving.EquivalentQuantity
                    )
                )
                .ToArray()
        );
        var result = await handler.HandleAsync(command, cancellationToken);
        if (!result.IsFailure)
        {
            return TypedResults.Ok(new UpdateFoodResponse(result.Value.Value));
        }

        var notFound = result.Error == UpdateFoodErrors.NotFound;
        return TypedResults.Problem(
            statusCode: notFound
                ? StatusCodes.Status404NotFound
                : StatusCodes.Status400BadRequest,
            title: notFound ? "Food not found." : "Invalid food.",
            detail: result.Error!.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = result.Error.Code
            }
        );
    }
}
