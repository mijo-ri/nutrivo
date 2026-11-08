using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Foods.GetFoodById;

public static class GetFoodByIdErrors
{
    public static readonly DomainError NotFound = new(
        "GetFoodById.NotFound",
        "The food was not found."
    );
}
