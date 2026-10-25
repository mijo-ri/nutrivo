namespace Nutrivo.Domain.Foods;

public static class NutritionRounding
{
    public static decimal ForDisplay(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
