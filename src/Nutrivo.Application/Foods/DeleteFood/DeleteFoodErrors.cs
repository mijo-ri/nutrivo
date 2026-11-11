using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Foods.DeleteFood;

public static class DeleteFoodErrors
{
    public static readonly DomainError NotFound = new(
        "DeleteFood.NotFound",
        "The food was not found."
    );
}
