using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Diaries;

public sealed class DiaryEntry : AggregateRoot<DiaryEntryId>
{
    private DiaryEntry(
        DateOnly date,
        MealType mealType,
        FoodSnapshot foodSnapshot,
        decimal quantity,
        NutritionalValues nutritionalValues,
        ServingSizeSnapshot? servingSizeSnapshot,
        decimal? servingCount
    ) : base(DiaryEntryId.New())
    {
        Date = date;
        MealType = mealType;
        FoodSnapshot = foodSnapshot;
        Quantity = quantity;
        ReferenceUnit = foodSnapshot.ReferenceUnit;
        NutritionalValues = nutritionalValues;
        ServingSizeSnapshot = servingSizeSnapshot;
        ServingCount = servingCount;
    }

    public DateOnly Date { get; }
    public MealType MealType { get; }
    public FoodSnapshot FoodSnapshot { get; }
    public decimal Quantity { get; }
    public ReferenceUnit ReferenceUnit { get; }
    public NutritionalValues NutritionalValues { get; }
    public ServingSizeSnapshot? ServingSizeSnapshot { get; }
    public decimal? ServingCount { get; }

    public static Result<DiaryEntry> Create(
        Food? food,
        DateOnly date,
        MealType mealType,
        decimal quantity,
        ReferenceUnit referenceUnit
    )
    {
        if (food is null)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.FoodRequired);
        }

        if (!IsValidMealType(mealType))
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.InvalidMealType);
        }

        if (quantity <= 0)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.InvalidQuantity);
        }

        if (referenceUnit is not (ReferenceUnit.Gram or ReferenceUnit.Milliliter))
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.InvalidReferenceUnit);
        }

        if (referenceUnit != food.ReferenceUnit)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.ReferenceUnitMismatch);
        }

        return CreateEntry(food, date, mealType, quantity, null, null);
    }

    public static Result<DiaryEntry> CreateFromServing(
        Food? food,
        DateOnly date,
        MealType mealType,
        ServingSizeId? servingSizeId,
        decimal servingCount
    )
    {
        if (food is null)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.FoodRequired);
        }

        if (!IsValidMealType(mealType))
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.InvalidMealType);
        }

        if (servingSizeId is null)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.ServingSizeIdRequired);
        }

        if (servingCount <= 0)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.InvalidServingCount);
        }

        var servingSize = food.ServingSizes.SingleOrDefault(serving =>
            serving.Id == servingSizeId
        );

        if (servingSize is null)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.ServingSizeNotFound);
        }

        decimal quantity;
        try
        {
            quantity = servingCount * servingSize.EquivalentQuantity;
        }
        catch (OverflowException)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.QuantityOutOfRange);
        }

        if (quantity <= 0)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.QuantityOutOfRange);
        }

        return CreateEntry(
            food,
            date,
            mealType,
            quantity,
            new ServingSizeSnapshot(servingSize),
            servingCount
        );
    }

    private static bool IsValidMealType(MealType mealType)
    {
        return mealType is MealType.Breakfast
            or MealType.Lunch
            or MealType.Dinner
            or MealType.Snacks;
    }

    private static Result<DiaryEntry> CreateEntry(
        Food food,
        DateOnly date,
        MealType mealType,
        decimal quantity,
        ServingSizeSnapshot? servingSizeSnapshot,
        decimal? servingCount
    )
    {
        var nutrition = food.NutritionalValues.Scale(quantity, food.ReferenceQuantity);
        if (nutrition.IsFailure)
        {
            return Result<DiaryEntry>.Failure(DiaryEntryErrors.NutritionOutOfRange);
        }

        return Result<DiaryEntry>.Success(
            new DiaryEntry(
                date,
                mealType,
                new FoodSnapshot(food),
                quantity,
                nutrition.Value,
                servingSizeSnapshot,
                servingCount
            )
        );
    }
}
