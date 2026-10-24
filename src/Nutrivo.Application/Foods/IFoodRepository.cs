using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Foods;

public interface IFoodRepository
{
    /// <summary>
    /// Adds a food and commits it to persistent storage before returning.
    /// </summary>
    Task AddAsync(Food food, CancellationToken cancellationToken);
}
