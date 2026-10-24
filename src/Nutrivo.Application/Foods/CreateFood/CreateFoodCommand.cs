using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods.CreateFood;

public sealed record CreateFoodCommand(
    string? Name,
    string? Brand,
    ReferenceUnit ReferenceUnit,
    decimal Kilocalories,
    decimal ProteinGrams,
    decimal CarbohydratesGrams,
    decimal FatGrams
);
