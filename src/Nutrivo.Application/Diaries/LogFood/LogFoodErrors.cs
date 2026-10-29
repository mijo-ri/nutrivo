using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Diaries.LogFood;

public static class LogFoodErrors
{
    public static readonly DomainError InvalidAmount = new(
        "LogFood.Amount.Invalid",
        "Specify either quantity and reference unit or serving size ID and serving count."
    );

    public static readonly DomainError FoodNotFound = new(
        "LogFood.Food.NotFound",
        "The selected food does not exist."
    );
}
