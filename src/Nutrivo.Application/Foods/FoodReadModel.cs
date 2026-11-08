using System.Collections.Immutable;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods;

public sealed record FoodReadModel(
    Guid Id,
    string Name,
    string? Brand,
    decimal ReferenceQuantity,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams,
    ImmutableArray<ServingSizeReadModel> ServingSizes
);

public sealed record ServingSizeReadModel(
    Guid Id,
    decimal Quantity,
    string Unit,
    string? Label,
    decimal EquivalentQuantity,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams
);
