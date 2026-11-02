using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Diaries.GetDiaryDay;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Diaries.GetDiaryDay;

public sealed class GetDiaryDayEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/diary-entries", HandleAsync)
            .WithName("GetDiaryDay")
            .WithTags(Tags.Diaries)
            .WithSummary("Gets the diary entries for a calendar day.")
            .WithDescription(
                "An explicit date in yyyy-MM-dd format is required; past and future "
                + "dates are allowed without time zone conversion. Entries are sorted "
                + "by meal (breakfast, lunch, dinner, snacks), then capture time oldest "
                + "first, then ID. Entries predating capture-time tracking share the "
                + "migration timestamp and are ordered by ID within each meal. "
                + "Names and portions come from historical snapshots. Nutrition is for "
                + "the consumed amount, rounded to two decimal places with midpoint "
                + "ties away from zero. Quantities retain their exact values. "
                + "Direct quantities have null servingCount and servingSize. Empty "
                + "days return an empty entries array. No daily totals are included."
            )
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Ok<GetDiaryDayResponse>> HandleAsync(
        [Description("Required calendar date in yyyy-MM-dd format.")]
        DateOnly date,
        IQueryHandler<GetDiaryDayQuery, GetDiaryDayResult> handler,
        CancellationToken cancellationToken
    )
    {
        var result = await handler.HandleAsync(
            new GetDiaryDayQuery(date),
            cancellationToken
        );
        var value = result.Value;
        var entries = value
            .Entries.Select(entry => new DiaryDayEntryResponse(
                    entry.Id,
                    entry.MealType,
                    entry.FoodId,
                    entry.Name,
                    entry.Brand,
                    entry.Quantity,
                    entry.ReferenceUnit,
                    NutritionRounding.ForDisplay(entry.Kilocalories),
                    NutritionRounding.ForDisplay(entry.ProteinGrams),
                    NutritionRounding.ForDisplay(entry.CarbohydratesGrams),
                    NutritionRounding.ForDisplay(entry.FatGrams),
                    entry.ServingCount,
                    entry.ServingSize is { } serving
                        ? new DiaryDayServingSizeResponse(
                            serving.Id,
                            serving.Quantity,
                            serving.Unit,
                            serving.Label,
                            serving.EquivalentQuantity
                        )
                        : null
                )
            )
            .ToImmutableArray();

        return TypedResults.Ok(new GetDiaryDayResponse(value.Date, entries));
    }
}
