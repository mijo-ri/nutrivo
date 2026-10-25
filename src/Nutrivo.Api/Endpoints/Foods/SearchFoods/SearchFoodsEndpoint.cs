using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Foods.SearchFoods;

namespace Nutrivo.Api.Endpoints.Foods.SearchFoods;

public sealed class SearchFoodsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/foods", HandleAsync)
            .WithName("SearchFoods")
            .WithTags(Tags.Foods)
            .WithSummary(
                "Searches foods by name or brand with paginated nutrition results."
            )
            .WithDescription(
                "Matches a literal, case-insensitive substring of name or brand. "
                + "Omitted or blank search lists all foods. Results are sorted by name, "
                + "brand and ID. Page starts at 1; page size defaults to 20 and permits "
                + "1 to 100 items. Nutrition is per 100 grams or milliliters."
            )
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Results<Ok<SearchFoodsResponse>, ProblemHttpResult>>
        HandleAsync(
            IQueryHandler<SearchFoodsQuery, SearchFoodsResult> handler,
            CancellationToken cancellationToken,
            [Description("Optional literal substring of name or brand.")]
            string? search = null,
            [Description("Page number, starting at 1.")]
            int page = 1,
            [Description("Items per page, between 1 and 100.")]
            int pageSize = 20
        )
    {
        var result = await handler.HandleAsync(
            new SearchFoodsQuery(search, page, pageSize),
            cancellationToken
        );

        if (result.IsFailure)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid food search.",
                detail: result.Error!.Description,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = result.Error.Code
                }
            );
        }

        var value = result.Value;
        var items = value
            .Items.Select(item => new SearchFoodResponse(
                    item.Id,
                    item.Name,
                    item.Brand,
                    item.ReferenceQuantity,
                    item.ReferenceUnit,
                    item.Kilocalories,
                    item.ProteinGrams,
                    item.CarbohydratesGrams,
                    item.FatGrams
                )
            )
            .ToImmutableArray();

        return TypedResults.Ok(
            new SearchFoodsResponse(items, value.Page, value.PageSize, value.TotalCount)
        );
    }
}
