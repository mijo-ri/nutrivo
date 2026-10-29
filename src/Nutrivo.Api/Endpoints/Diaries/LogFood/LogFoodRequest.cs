using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Diaries.LogFood;

public sealed record LogFoodRequest
{
    public required Guid FoodId { get; init; }
    public required DateOnly Date { get; init; }
    public required MealType MealType { get; init; }
    public decimal? Quantity { get; init; }
    public ReferenceUnit? ReferenceUnit { get; init; }
    public Guid? ServingSizeId { get; init; }
    public decimal? ServingCount { get; init; }
}
