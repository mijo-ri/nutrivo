namespace Nutrivo.Domain.Foods;

public sealed record ServingSizeUpdate(
    ServingSizeId? Id,
    decimal Quantity,
    string? Unit,
    string? Label,
    decimal EquivalentQuantity
);
