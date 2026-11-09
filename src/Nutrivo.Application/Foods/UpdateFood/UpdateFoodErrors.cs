using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Foods.UpdateFood;

public static class UpdateFoodErrors
{
    public static readonly DomainError NotFound = new(
        "UpdateFood.NotFound",
        "The food was not found."
    );
}
