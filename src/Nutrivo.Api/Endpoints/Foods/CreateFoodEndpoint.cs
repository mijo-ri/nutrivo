using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Foods.CreateFood;

namespace Nutrivo.Api.Endpoints.Foods;

public static class CreateFoodEndpoint
{
    public static IEndpointRouteBuilder MapCreateFood(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/foods", HandleAsync)
            .WithName("CreateFood")
            .WithSummary("Creates a food with nutrition per 100 grams or milliliters.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<Results<Created<CreateFoodResponse>, ProblemHttpResult>>
        HandleAsync(
            CreateFoodRequest request,
            CreateFoodCommandHandler handler,
            CancellationToken cancellationToken
        )
    {
        var command = new CreateFoodCommand(
            request.Name,
            request.Brand,
            request.ReferenceUnit,
            request.Kilocalories,
            request.ProteinGrams,
            request.CarbohydratesGrams,
            request.FatGrams
        );
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid food.",
                detail: result.Error!.Description,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error.Code
                }
            );
        }

        return TypedResults.Created(
            (string?)null,
            new CreateFoodResponse(result.Value.Value)
        );
    }
}
