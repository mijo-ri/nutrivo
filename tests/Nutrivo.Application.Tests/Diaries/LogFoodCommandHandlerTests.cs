using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.LogFood;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Diaries;

public sealed class LogFoodCommandHandlerTests
{
    private static Food CreateFood(ReferenceUnit unit = ReferenceUnit.Gram)
    {
        return Food.Create(
                "Egg",
                "Brand",
                50m,
                unit,
                NutritionalValues.Create(150.005m, 10.005m, 1.005m, 5.005m).Value,
                [ServingSize.Create(2m, "pieces", "Medium", 116m).Value]
            )
            .Value;
    }

    private static LogFoodCommand Direct(Food food)
    {
        return new LogFoodCommand(
            food.Id.Value,
            new DateOnly(2026, 10, 29),
            MealType.Breakfast,
            25m,
            food.ReferenceUnit
        );
    }

    private static LogFoodCommand Serving(Food food)
    {
        return Direct(food) with
        {
            Quantity = null,
            ReferenceUnit = null,
            ServingSizeId = food.ServingSizes.Single().Id.Value,
            ServingCount = 0.5m
        };
    }

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public async Task HandleAsync_WithQuantity_PersistsSnapshotAndExactNutrition(
        ReferenceUnit unit
    )
    {
        var food = CreateFood(unit);
        var foods = new RecordingFoodRepository(food);
        var entries = new RecordingDiaryEntryRepository();
        using var cancellation = new CancellationTokenSource();
        var command = Direct(food);

        var result =
            await new LogFoodCommandHandler(foods, entries).HandleAsync(
                command,
                cancellation.Token
            );

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(entries.Entries);
        Assert.Equal(entry.Id, result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Value);
        Assert.Equal(command.Date, entry.Date);
        Assert.Equal(command.MealType, entry.MealType);
        Assert.Equal(25m, entry.Quantity);
        Assert.Equal(unit, entry.ReferenceUnit);
        Assert.Equal(food.Id, entry.FoodSnapshot.FoodId);
        Assert.Equal(food.NutritionalValues, entry.FoodSnapshot.NutritionalValues);
        Assert.Equal(75.0025m, entry.NutritionalValues.Kilocalories);
        Assert.Equal(5.0025m, entry.NutritionalValues.ProteinGrams);
        Assert.Equal(0.5025m, entry.NutritionalValues.CarbohydratesGrams);
        Assert.Equal(2.5025m, entry.NutritionalValues.FatGrams);
        Assert.Null(entry.ServingSizeSnapshot);
        Assert.Null(entry.ServingCount);
        Assert.Equal(food.Id, foods.RequestedId);
        Assert.Equal(cancellation.Token, foods.Token);
        Assert.Equal(cancellation.Token, entries.Token);
    }

    [Fact]
    public async Task HandleAsync_WithFractionalServing_UsesEntireServingAndSnapshotsIt()
    {
        var food = CreateFood();
        var foods = new RecordingFoodRepository(food);
        var entries = new RecordingDiaryEntryRepository();
        using var cancellation = new CancellationTokenSource();

        var result = await new LogFoodCommandHandler(foods, entries).HandleAsync(
            Serving(food),
            cancellation.Token
        );

        Assert.True(result.IsSuccess);
        var entry = Assert.Single(entries.Entries);
        Assert.Equal(entry.Id, result.Value);
        Assert.Equal(58m, entry.Quantity);
        Assert.Equal(0.5m, entry.ServingCount);
        Assert.Equal(
            food.ServingSizes.Single().Id,
            entry.ServingSizeSnapshot!.ServingSizeId
        );
        Assert.Equal(2m, entry.ServingSizeSnapshot.Quantity);
        Assert.Equal(116m, entry.ServingSizeSnapshot.EquivalentQuantity);
        Assert.Equal(174.0058m, entry.NutritionalValues.Kilocalories);
        Assert.Equal(cancellation.Token, foods.Token);
        Assert.Equal(cancellation.Token, entries.Token);
    }

    [Fact]
    public async Task HandleAsync_WithIncompleteOrMixedAmounts_DoesNotReadOrWrite()
    {
        var food = CreateFood();
        foreach (var mask in Enumerable.Range(0, 16).Except([3, 12]))
        {
            var foods = new RecordingFoodRepository(food);
            var entries = new RecordingDiaryEntryRepository();
            var command = Direct(food) with
            {
                Quantity = (mask & 1) != 0 ? 25m : null,
                ReferenceUnit = (mask & 2) != 0 ? ReferenceUnit.Gram : null,
                ServingSizeId =
                (mask & 4) != 0 ? food.ServingSizes.Single().Id.Value : null,
                ServingCount = (mask & 8) != 0 ? 1m : null
            };

            var result = await new LogFoodCommandHandler(foods, entries).HandleAsync(
                command,
                CancellationToken.None
            );

            Assert.Equal(LogFoodErrors.InvalidAmount, result.Error);
            Assert.Null(foods.RequestedId);
            Assert.Empty(entries.Entries);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_WithEmptyId_DoesNotReadOrWrite(bool serving)
    {
        var food = CreateFood();
        var foods = new RecordingFoodRepository(food);
        var entries = new RecordingDiaryEntryRepository();
        var command = serving
            ? Serving(food) with
            {
                ServingSizeId = Guid.Empty
            }
            : Direct(food) with
            {
                FoodId = Guid.Empty
            };

        var result =
            await new LogFoodCommandHandler(foods, entries).HandleAsync(
                command,
                CancellationToken.None
            );

        Assert.Equal(
            serving ? FoodErrors.EmptyServingSizeId : FoodErrors.EmptyId,
            result.Error
        );
        Assert.Null(foods.RequestedId);
        Assert.Empty(entries.Entries);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownFood_ReturnsNotFoundAndSavesNothing()
    {
        var foods = new RecordingFoodRepository(null);
        var entries = new RecordingDiaryEntryRepository();

        var result = await new LogFoodCommandHandler(foods, entries).HandleAsync(
            Direct(CreateFood()),
            CancellationToken.None
        );

        Assert.Equal(LogFoodErrors.FoodNotFound, result.Error);
        Assert.Empty(entries.Entries);
    }

    public static IEnumerable<object[]> InvalidEntries()
    {
        var food = CreateFood();
        var direct = Direct(food);
        var serving = Serving(food);
        yield return
        [
            food, direct with
            {
                Quantity = 0m
            },
            DiaryEntryErrors.InvalidQuantity
        ];
        yield return
        [
            food, direct with
            {
                Quantity = -1m
            },
            DiaryEntryErrors.InvalidQuantity
        ];
        yield return
        [
            food, direct with
            {
                ReferenceUnit = (ReferenceUnit)99
            },
            DiaryEntryErrors.InvalidReferenceUnit
        ];
        yield return
        [
            food, direct with
            {
                ReferenceUnit = ReferenceUnit.Milliliter
            },
            DiaryEntryErrors.ReferenceUnitMismatch
        ];
        yield return
        [
            food, direct with
            {
                MealType = (MealType)99
            },
            DiaryEntryErrors.InvalidMealType
        ];
        yield return
        [
            food, serving with
            {
                MealType = (MealType)99
            },
            DiaryEntryErrors.InvalidMealType
        ];
        yield return
        [
            food, serving with
            {
                ServingCount = 0m
            },
            DiaryEntryErrors.InvalidServingCount
        ];
        yield return
        [
            food, serving with
            {
                ServingCount = -0.5m
            },
            DiaryEntryErrors.InvalidServingCount
        ];
        yield return
        [
            food, serving with
            {
                ServingSizeId = Guid.NewGuid()
            },
            DiaryEntryErrors.ServingSizeNotFound
        ];
        yield return
        [
            food, serving with
            {
                ServingCount = decimal.MaxValue
            },
            DiaryEntryErrors.QuantityOutOfRange
        ];
        yield return
        [
            food, direct with
            {
                Quantity = decimal.MaxValue
            },
            DiaryEntryErrors.NutritionOutOfRange
        ];
    }

    [Theory]
    [MemberData(nameof(InvalidEntries))]
    public async Task HandleAsync_WithDomainFailure_ReturnsUnchangedErrorAndSavesNothing(
        Food food,
        LogFoodCommand command,
        DomainError error
    )
    {
        var entries = new RecordingDiaryEntryRepository();

        var result =
            await new LogFoodCommandHandler(new RecordingFoodRepository(food), entries)
                .HandleAsync(command, CancellationToken.None);

        Assert.Equal(error, result.Error);
        Assert.Empty(entries.Entries);
    }

    [Fact]
    public async Task HandleAsync_WithRepeatedFood_CreatesIndependentEntries()
    {
        var food = CreateFood();
        var entries = new RecordingDiaryEntryRepository();
        var handler = new LogFoodCommandHandler(
            new RecordingFoodRepository(food),
            entries
        );

        var first = await handler.HandleAsync(Direct(food), CancellationToken.None);
        var second = await handler.HandleAsync(Direct(food), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(2, entries.Entries.Count);
    }

    [Fact]
    public async Task HandleAsync_WhenCommitFails_PropagatesFailure()
    {
        var food = CreateFood();
        var failure = new InvalidOperationException("storage failure");
        var entries = new RecordingDiaryEntryRepository
        {
            Failure = failure
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LogFoodCommandHandler(new RecordingFoodRepository(food), entries)
                .HandleAsync(Direct(food), CancellationToken.None)
        );

        Assert.Same(failure, exception);
        Assert.Empty(entries.Entries);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_DoesNotSave()
    {
        var food = CreateFood();
        var entries = new RecordingDiaryEntryRepository();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new LogFoodCommandHandler(new RecordingFoodRepository(food), entries)
                .HandleAsync(Direct(food), cancellation.Token)
        );

        Assert.Empty(entries.Entries);
    }

    private sealed class RecordingFoodRepository(Food? food) : IFoodRepository
    {
        public FoodId? RequestedId { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken)
        {
            RequestedId = id;
            Token = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(food?.Id == id ? food : null);
        }

        public Task AddAsync(Food value, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingDiaryEntryRepository : IDiaryEntryRepository
    {
        public List<DiaryEntry> Entries { get; } = [];
        public CancellationToken Token { get; private set; }
        public Exception? Failure { get; init; }

        public Task AddAsync(DiaryEntry entry, CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null)
            {
                return Task.FromException(Failure);
            }

            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
