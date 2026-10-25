using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Foods.CreateFood;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods.CreateFood;

public sealed class CreateFoodEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPost("/foods", HandleAsync)
            .WithName("CreateFood")
            .WithTags(Tags.Foods)
            .WithSummary(
                "Creates a food with a nutrition basis and optional serving sizes."
            )
            .WithDescription(
                """
                Reference quantity is a positive amount in grams (1) or milliliters (2).
                Each serving specifies a positive quantity, a free-text unit, an
                optional label and an equivalent quantity in the food reference unit.
                The equivalent quantity describes the entire serving quantity.
                Serving nutrition is calculated from the basis, never entered separately.

                Example request (illustrative nutrition and weights):
                ```json
                {
                  "name": "Ei",
                  "referenceQuantity": 100,
                  "referenceUnit": 1,
                  "kilocalories": 150,
                  "proteinGrams": 12,
                  "carbohydratesGrams": 1,
                  "fatGrams": 10,
                  "servingSizes": [
                    {
                      "quantity": 1,
                      "unit": "Stück",
                      "label": "Mittel",
                      "equivalentQuantity": 58
                    }
                  ]
                }
                ```
                """
            )
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
            request.ReferenceQuantity,
            request.ReferenceUnit,
            request.Kilocalories,
            request.ProteinGrams,
            request.CarbohydratesGrams,
            request.FatGrams,
            request
                .ServingSizes?.Select(serving => serving is null
                    ? null
                    : new CreateServingSizeInput(
                        serving.Quantity,
                        serving.Unit,
                        serving.Label,
                        serving.EquivalentQuantity
                    )
                )
                .ToArray()
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
