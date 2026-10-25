using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Foods.CreateFood;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods;

public sealed class CreateFoodEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/foods", HandleAsync)
            .WithName("CreateFood")
            .WithSummary("Creates a food with nutrition per 100 grams or milliliters.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<Created<CreateFoodResponse>, ProblemHttpResult>>
        HandleAsync(
            CreateFoodRequest request,
            ICommandHandler<CreateFoodCommand, FoodId> handler,
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
