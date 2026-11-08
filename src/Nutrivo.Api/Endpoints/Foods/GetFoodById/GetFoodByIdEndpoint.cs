using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Api.Endpoints.Foods.SearchFoods;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.GetFoodById;

namespace Nutrivo.Api.Endpoints.Foods.GetFoodById;

public sealed class GetFoodByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/foods/{id}", HandleAsync)
            .WithName("GetFoodById")
            .WithTags(Tags.Foods)
            .WithSummary("Gets a food with its nutrition basis and serving sizes.")
            .WithDescription(
                "Returns the same food representation as food search. "
                + "Basis nutrition is exact; serving nutrition is rounded to two decimal "
                + "places with midpoint ties away from zero. Servings retain input order."
            )
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<Ok<SearchFoodResponse>, ProblemHttpResult>>
        HandleAsync(
            Guid id,
            IQueryHandler<GetFoodByIdQuery, FoodReadModel> handler,
            CancellationToken cancellationToken
        )
    {
        var result = await handler.HandleAsync(
            new GetFoodByIdQuery(id),
            cancellationToken
        );
        if (result.IsFailure)
        {
            var notFound = result.Error == GetFoodByIdErrors.NotFound;
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

        return TypedResults.Ok(FoodResponseMapping.Map(result.Value));
    }
}
