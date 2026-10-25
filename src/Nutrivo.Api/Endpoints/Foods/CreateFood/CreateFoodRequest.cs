using Nutrivo.Domain.Foods;

namespace Nutrivo.Api.Endpoints.Foods.CreateFood;

public sealed record CreateFoodRequest
{
    public required string? Name { get; init; }

    public string? Brand { get; init; }

    public required decimal ReferenceQuantity { get; init; }

    public required ReferenceUnit ReferenceUnit { get; init; }

    public required decimal Kilocalories { get; init; }

    public required decimal ProteinGrams { get; init; }

    public required decimal CarbohydratesGrams { get; init; }

    public required decimal FatGrams { get; init; }

    public IReadOnlyList<CreateServingSizeRequest?>? ServingSizes { get; init; }
}

public sealed record CreateServingSizeRequest
{
    public required decimal Quantity { get; init; }
    public required string? Unit { get; init; }
    public string? Label { get; init; }
    public required decimal EquivalentQuantity { get; init; }
}
