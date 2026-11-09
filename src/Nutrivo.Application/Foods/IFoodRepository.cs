using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods;

public interface IFoodRepository
{
    Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a tracked food and holds its write lock until saving or scope disposal.
    /// </summary>
    Task<Food?> GetForUpdateAsync(FoodId id, CancellationToken cancellationToken);

    /// <summary>
    /// Commits changes to a food loaded for update before returning.
    /// </summary>
    Task SaveAsync(Food food, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a food and commits it to persistent storage before returning.
    /// </summary>
    Task AddAsync(Food food, CancellationToken cancellationToken);
}
