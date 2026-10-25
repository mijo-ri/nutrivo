using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.CreateFood;

public sealed record CreateFoodCommand(
    string? Name,
    string? Brand,
    decimal ReferenceQuantity,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams,
    IReadOnlyList<CreateServingSizeInput?>? ServingSizes = null
);

public sealed record CreateServingSizeInput(
    decimal Quantity,
    string? Unit,
    string? Label,
    decimal EquivalentQuantity
);
