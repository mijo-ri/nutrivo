using System.Collections.Immutable;
using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.GetDiaryDay;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Diaries;

public sealed class GetDiaryDayQueryHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_ForwardsDateAndTokenAndPreservesEntries(
        bool withEntry
    )
    {
        var date = new DateOnly(2020, 2, 29);
        var queries = new RecordingDiaryQueries
        {
            Response = withEntry
                ?
                [
                    new DiaryDayEntry(
                        Guid.NewGuid(),
                        MealType.Lunch,
                        Guid.NewGuid(),
                        "Oats",
                        null,
                        25.123m,
                        ReferenceUnit.Gram,
                        75.005m,
                        1.005m,
                        2.005m,
                        3.005m,
                        null,
                        null
                    )
                ]
                : []
        };
        var handler = new GetDiaryDayQueryHandler(queries);
        using var cancellation = new CancellationTokenSource();

        var result = await handler.HandleAsync(
            new GetDiaryDayQuery(date),
            cancellation.Token
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(date, result.Value.Date);
        Assert.Equal(queries.Response, result.Value.Entries);
        Assert.Equal((date, cancellation.Token), Assert.Single(queries.Calls));
    }

    [Fact]
    public async Task HandleAsync_WithNoEntries_ReturnsZeroTotalsForDayAndAllMeals()
    {
        var result = await HandleAsync([]);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Entries);
        Assert.Equal(new DiaryNutritionTotals(0m, 0m, 0m, 0m), result.Value.Totals);
        AssertMealOrder(result.Value);
        Assert.All(
            result.Value.MealTotals,
            meal =>
                Assert.Equal(result.Value.Totals, meal.Totals)
        );
    }

    [Theory]
    [InlineData(MealType.Breakfast)]
    [InlineData(MealType.Lunch)]
    [InlineData(MealType.Dinner)]
    [InlineData(MealType.Snacks)]
    public async Task HandleAsync_WithOneEntry_PreservesExactNutritionAndZerosOtherMeals(
        MealType mealType
    )
    {
        var result = await HandleAsync([CreateEntry(mealType)]);

        Assert.True(result.IsSuccess);
        var expected = new DiaryNutritionTotals(75.005m, 1.005m, 2.005m, 3.005m);
        Assert.Equal(expected, result.Value.Totals);
        AssertMealOrder(result.Value);
        foreach (var meal in result.Value.MealTotals)
        {
            Assert.Equal(
                meal.MealType == mealType
                    ? expected
                    : new DiaryNutritionTotals(0m, 0m, 0m, 0m),
                meal.Totals
            );
        }
    }

    [Fact]
    public async Task HandleAsync_SumsAllMealsAndCountsRepeatedFoodWithoutRounding()
    {
        var breakfast = CreateEntry(MealType.Breakfast, 0.005m);
        ImmutableArray<DiaryDayEntry> entries =
        [
            CreateEntry(MealType.Snacks, 1000m, 2000m, 3000m, 4000m),
            breakfast,
            CreateEntry(MealType.Dinner, 100m, 200m, 300m, 400m),
            breakfast with
            {
                Id = Guid.NewGuid()
            },
            CreateEntry(MealType.Lunch, 10m, 20m, 30m, 40m)
        ];

        var result = await HandleAsync(entries);

        Assert.True(result.IsSuccess);
        Assert.Equal(entries, result.Value.Entries);
        Assert.Equal(
            new DiaryNutritionTotals(1110.010m, 2222.010m, 3334.010m, 4446.010m),
            result.Value.Totals
        );
        AssertMealOrder(result.Value);
        Assert.Equal(
            new[]
            {
                new DiaryNutritionTotals(0.010m, 2.010m, 4.010m, 6.010m),
                new DiaryNutritionTotals(10m, 20m, 30m, 40m),
                new DiaryNutritionTotals(100m, 200m, 300m, 400m),
                new DiaryNutritionTotals(1000m, 2000m, 3000m, 4000m)
            },
            result.Value.MealTotals.Select(meal => meal.Totals)
        );
    }

    [Fact]
    public async Task HandleAsync_WithMaximumRepresentableTotals_Succeeds()
    {
        var result = await HandleAsync(
            [
                CreateEntry(
                    MealType.Lunch,
                    decimal.MaxValue,
                    decimal.MaxValue,
                    decimal.MaxValue,
                    decimal.MaxValue
                )
            ]
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new DiaryNutritionTotals(
                decimal.MaxValue,
                decimal.MaxValue,
                decimal.MaxValue,
                decimal.MaxValue
            ),
            result.Value.Totals
        );
        Assert.Equal(result.Value.Totals, result.Value.MealTotals[1].Totals);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public async Task HandleAsync_WhenAnyTotalOverflows_ReturnsFailure(
        int nutrient,
        bool differentMeals
    )
    {
        decimal[] first = [0m, 0m, 0m, 0m];
        decimal[] second = [0m, 0m, 0m, 0m];
        first[nutrient] = decimal.MaxValue;
        second[nutrient] = 1m;

        var result = await HandleAsync(
            [
                CreateEntry(MealType.Breakfast, first[0], first[1], first[2], first[3]),
                CreateEntry(
                    differentMeals ? MealType.Lunch : MealType.Breakfast,
                    second[0],
                    second[1],
                    second[2],
                    second[3]
                )
            ]
        );

        Assert.True(result.IsFailure);
        Assert.Same(GetDiaryDayErrors.TotalsOutOfRange, result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenStorageFails_PropagatesTechnicalFailure()
    {
        var failure = new InvalidOperationException("Storage unavailable.");
        var handler = new GetDiaryDayQueryHandler(
            new RecordingDiaryQueries
            {
                Failure = failure
            }
        );

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new GetDiaryDayQuery(new DateOnly(2030, 1, 1)),
                CancellationToken.None
            )
        );

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_PropagatesCancellation()
    {
        var handler = new GetDiaryDayQueryHandler(new RecordingDiaryQueries());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(
                new GetDiaryDayQuery(new DateOnly(2030, 1, 1)),
                cancellation.Token
            )
        );
    }

    private static DiaryDayEntry CreateEntry(
        MealType mealType,
        decimal kilocalories = 75.005m,
        decimal proteinGrams = 1.005m,
        decimal carbohydratesGrams = 2.005m,
        decimal fatGrams = 3.005m
    )
    {
        return new DiaryDayEntry(
            Guid.NewGuid(),
            mealType,
            Guid.NewGuid(),
            "Oats",
            null,
            25.123m,
            ReferenceUnit.Gram,
            kilocalories,
            proteinGrams,
            carbohydratesGrams,
            fatGrams,
            null,
            null
        );
    }

    private static void AssertMealOrder(GetDiaryDayResult result)
    {
        Assert.Equal(
            new[]
            {
                MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snacks
            },
            result.MealTotals.Select(meal => meal.MealType)
        );
    }

    private static Task<Result<GetDiaryDayResult>> HandleAsync(
        ImmutableArray<DiaryDayEntry> entries
    )
    {
        var handler = new GetDiaryDayQueryHandler(
            new RecordingDiaryQueries
            {
                Response = entries
            }
        );
        return handler.HandleAsync(
            new GetDiaryDayQuery(new DateOnly(2026, 11, 2)),
            CancellationToken.None
        );
    }

    private sealed class RecordingDiaryQueries : IDiaryQueries
    {
        public List<(DateOnly Date, CancellationToken Token)> Calls { get; } = [];

        public ImmutableArray<DiaryDayEntry> Response { get; init; } = [];

        public Exception? Failure { get; init; }

        public Task<ImmutableArray<DiaryDayEntry>> GetDayAsync(
            DateOnly date,
            CancellationToken cancellationToken
        )
        {
            Calls.Add((date, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null
                ? Task.FromResult(Response)
                : Task.FromException<ImmutableArray<DiaryDayEntry>>(Failure);
        }
    }
}
