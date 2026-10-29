using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Diaries.LogFood;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Api.Endpoints.Diaries.LogFood;

public sealed class LogFoodEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/diary-entries", HandleAsync)
            .WithName("LogFood")
            .WithTags(Tags.Diaries)
            .WithSummary(
                "Logs a food by quantity or serving size for a calendar day and meal."
            )
            .WithDescription(
                """
                Specify exactly one complete amount: quantity and referenceUnit, or
                servingSizeId and servingCount. Units are grams (1) and milliliters (2)
                and must match the food. Meals are breakfast (1), lunch (2), dinner (3)
                and snacks (4). Past and future calendar dates are allowed.
                Serving count multiplies the entire defined serving quantity.
                Food, serving and nutrition are snapshotted when the entry is saved.

                Example request using grams:
                ```json
                {
                  "foodId": "11111111-1111-1111-1111-111111111111",
                  "date": "2026-10-29",
                  "mealType": 1,
                  "quantity": 75,
                  "referenceUnit": 1
                }
                ```

                Example request using a serving:
                ```json
                {
                  "foodId": "11111111-1111-1111-1111-111111111111",
                  "date": "2026-10-29",
                  "mealType": 1,
                  "servingSizeId": "22222222-2222-2222-2222-222222222222",
                  "servingCount": 0.5
                }
                ```
                """
            )
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<Created<LogFoodResponse>, ProblemHttpResult>>
        HandleAsync(
            LogFoodRequest request,
            ICommandHandler<LogFoodCommand, DiaryEntryId> handler,
            CancellationToken cancellationToken
        )
    {
        var result = await handler.HandleAsync(
            new LogFoodCommand(
                request.FoodId,
                request.Date,
                request.MealType,
                request.Quantity,
                request.ReferenceUnit,
                request.ServingSizeId,
                request.ServingCount
            ),
            cancellationToken
        );

        if (result.IsFailure)
        {
            var notFound = result.Error == LogFoodErrors.FoodNotFound;
            return TypedResults.Problem(
                statusCode: notFound
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest,
                title: notFound ? "Food not found." : "Invalid diary entry.",
                detail: result.Error!.Description,
                extensions: new Dictionary<string, object?>
                    { ["code"] = result.Error.Code }
            );
        }

        return TypedResults.Created(
            (string?)null,
            new LogFoodResponse(result.Value.Value)
        );
    }
}
