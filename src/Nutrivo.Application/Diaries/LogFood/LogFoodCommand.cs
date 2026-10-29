using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Diaries.LogFood;

public sealed record LogFoodCommand(
    Guid FoodId,
    DateOnly Date,
    MealType MealType,
    decimal? Quantity = null,
    ReferenceUnit? ReferenceUnit = null,
    Guid? ServingSizeId = null,
    decimal? ServingCount = null
);
