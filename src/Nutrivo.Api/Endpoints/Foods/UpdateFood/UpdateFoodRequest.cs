using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods.UpdateFood;

public sealed record UpdateFoodRequest
{
    public required string? Name { get; init; }
    public string? Brand { get; init; }
    public required decimal ReferenceQuantity { get; init; }
    public required ReferenceUnit ReferenceUnit { get; init; }
    public required decimal Kilocalories { get; init; }
    public required decimal ProteinGrams { get; init; }
    public required decimal CarbohydratesGrams { get; init; }
    public required decimal FatGrams { get; init; }
    public IReadOnlyList<UpdateServingSizeRequest?>? ServingSizes { get; init; }
}

public sealed record UpdateServingSizeRequest
{
    public Guid? Id { get; init; }
    public required decimal Quantity { get; init; }
    public required string? Unit { get; init; }
    public string? Label { get; init; }
    public required decimal EquivalentQuantity { get; init; }
}
