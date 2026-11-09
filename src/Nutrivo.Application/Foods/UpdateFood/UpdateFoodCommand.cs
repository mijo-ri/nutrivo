using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.UpdateFood;

public sealed record UpdateFoodCommand(
    Guid Id,
    string? Name,
    string? Brand,
    decimal ReferenceQuantity,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams,
    IReadOnlyList<UpdateServingSizeInput?>? ServingSizes = null
);

public sealed record UpdateServingSizeInput(
    Guid? Id,
    decimal Quantity,
    string? Unit,
    string? Label,
    decimal EquivalentQuantity
);
