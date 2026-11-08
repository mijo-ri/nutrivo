using System.Collections.Immutable;
using Nutrivo.Api.Endpoints.Foods.SearchFoods;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods;

internal static class FoodResponseMapping
{
    public static SearchFoodResponse Map(FoodReadModel food)
    {
        return new SearchFoodResponse(
            food.Id,
            food.Name,
            food.Brand,
            food.ReferenceQuantity,
            food.ReferenceUnit,
            food.Kilocalories,
            food.ProteinGrams,
            food.CarbohydratesGrams,
            food.FatGrams,
            food
                .ServingSizes.Select(serving => new SearchServingSizeResponse(
                        serving.Id,
                        serving.Quantity,
                        serving.Unit,
                        serving.Label,
                        serving.EquivalentQuantity,
                        NutritionRounding.ForDisplay(serving.Kilocalories),
                        NutritionRounding.ForDisplay(serving.ProteinGrams),
                        NutritionRounding.ForDisplay(serving.CarbohydratesGrams),
                        NutritionRounding.ForDisplay(serving.FatGrams)
                    )
                )
                .ToImmutableArray()
        );
    }
}
