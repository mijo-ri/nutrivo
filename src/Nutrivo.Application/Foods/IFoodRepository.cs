using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods;

public interface IFoodRepository
{
    /// <summary>
    /// Permanently deletes a food and its servings, committing before returning.
    /// Returns false when no food exists. Call in a fresh scope without tracked foods
    /// or an existing transaction.
    /// </summary>
    Task<bool> DeleteAsync(FoodId id, CancellationToken cancellationToken);

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
