using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.UpdateFood;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Foods;

public sealed class UpdateFoodCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_UpdatesTrackedFoodAndCommitsWithCancellationToken()
    {
        var food = CreateFood();
        var servingId = food.ServingSizes[0].Id;
        var repository = new RecordingFoodRepository(food);
        using var cancellation = new CancellationTokenSource();
        var command = Command(food.Id.Value) with
        {
            ServingSizes =
            [
                new(servingId.Value, 2m, "Cup", "Large", 200m),
                new(null, 1m, "Spoon", null, 10m)
            ]
        };

        var result =
            await new UpdateFoodCommandHandler(repository).HandleAsync(
                command,
                cancellation.Token
            );

        Assert.Equal(food.Id, result.Value);
        Assert.Equal(food.Id, repository.LoadedId);
        Assert.Equal(cancellation.Token, repository.LoadToken);
        Assert.Equal(cancellation.Token, repository.SaveToken);
        Assert.Same(food, repository.Saved);
        Assert.Equal("Updated", food.Name);
        Assert.Equal(ReferenceUnit.Milliliter, food.ReferenceUnit);
        Assert.Equal(servingId, food.ServingSizes[0].Id);
        Assert.Equal(2, food.ServingSizes.Count);
    }

    [Fact]
    public async Task HandleAsync_WithUnknownFood_ReturnsNotFoundWithoutSaving()
    {
        var repository = new RecordingFoodRepository(null);
        var result = await new UpdateFoodCommandHandler(repository).HandleAsync(
            Command(Guid.NewGuid()),
            CancellationToken.None
        );
        Assert.Equal(UpdateFoodErrors.NotFound, result.Error);
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyId_DoesNotLoadOrSave()
    {
        var repository = new RecordingFoodRepository(null);
        var result = await new UpdateFoodCommandHandler(repository).HandleAsync(
            Command(Guid.Empty),
            CancellationToken.None
        );
        Assert.Equal(FoodErrors.EmptyId, result.Error);
        Assert.Null(repository.LoadedId);
        Assert.Null(repository.Saved);
    }

    [Theory]
    [InlineData("name", "Food.Name.Required")]
    [InlineData("calories", "NutritionalValues.Kilocalories.Negative")]
    [InlineData("protein", "NutritionalValues.ProteinGrams.Negative")]
    [InlineData("carbohydrates", "NutritionalValues.CarbohydratesGrams.Negative")]
    [InlineData("fat", "NutritionalValues.FatGrams.Negative")]
    [InlineData("nullServing", "Food.ServingSize.Required")]
    [InlineData("emptyServingId", "ServingSize.Id.Empty")]
    [InlineData("foreignServingId", "Food.ServingSize.NotFound")]
    [InlineData("duplicate", "Food.ServingSize.Id.Duplicate")]
    public async Task HandleAsync_WithInvalidInput_DoesNotSaveOrMutate(
        string scenario,
        string code
    )
    {
        var food = CreateFood();
        var repository = new RecordingFoodRepository(food);
        var command = Command(food.Id.Value);
        var serving = new UpdateServingSizeInput(
            food.ServingSizes[0].Id.Value,
            1m,
            "Cup",
            null,
            50m
        );
        command = scenario switch
        {
            "name" => command with
            {
                Name = " "
            },
            "calories" => command with
            {
                Kilocalories = -1m
            },
            "protein" => command with
            {
                ProteinGrams = -1m
            },
            "carbohydrates" => command with
            {
                CarbohydratesGrams = -1m
            },
            "fat" => command with
            {
                FatGrams = -1m
            },
            "nullServing" => command with
            {
                ServingSizes = [null]
            },
            "emptyServingId" => command with
            {
                ServingSizes =
                [
                    serving with
                    {
                        Id = Guid.Empty
                    }
                ]
            },
            "foreignServingId" => command with
            {
                ServingSizes =
                [
                    serving with
                    {
                        Id = Guid.NewGuid()
                    }
                ]
            },
            _ => command with
            {
                ServingSizes = [serving, serving]
            }
        };

        var result =
            await new UpdateFoodCommandHandler(repository).HandleAsync(
                command,
                CancellationToken.None
            );
        Assert.Equal(code, result.Error?.Code);
        Assert.Null(repository.Saved);
        Assert.Equal("Original", food.Name);
        Assert.Equal(100m, food.ReferenceQuantity);
        Assert.Equal("Spoon", food.ServingSizes[0].Unit);
    }

    [Fact]
    public async Task HandleAsync_WithCancellation_PropagatesAndDoesNotSave()
    {
        var food = CreateFood();
        var repository = new RecordingFoodRepository(food);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UpdateFoodCommandHandler(repository).HandleAsync(
                Command(food.Id.Value),
                cancellation.Token
            )
        );
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task HandleAsync_WithStorageFailure_PropagatesException()
    {
        var food = CreateFood();
        var failure = new InvalidOperationException("storage failed");
        var repository = new RecordingFoodRepository(food)
        {
            Failure = failure
        };
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdateFoodCommandHandler(repository).HandleAsync(
                Command(food.Id.Value),
                CancellationToken.None
            )
        );
        Assert.Same(failure, thrown);
    }

    private static Food CreateFood()
    {
        return Food.Create(
                "Original",
                null,
                100m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(100m, 1m, 2m, 3m).Value,
                [ServingSize.Create(1m, "Spoon", null, 10m).Value]
            )
            .Value;
    }

    private static UpdateFoodCommand Command(Guid id)
    {
        return new UpdateFoodCommand(
            id,
            " Updated ",
            null,
            50m,
            ReferenceUnit.Milliliter,
            200m,
            2m,
            3m,
            4m
        );
    }

    private sealed class RecordingFoodRepository(Food? food) : IFoodRepository
    {
        public FoodId? LoadedId { get; private set; }
        public CancellationToken LoadToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Food? Saved { get; private set; }
        public Exception? Failure { get; init; }

        public Task<Food?> GetForUpdateAsync(
            FoodId id,
            CancellationToken cancellationToken
        )
        {
            LoadedId = id;
            LoadToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(food?.Id == id ? food : null);
        }

        public Task SaveAsync(Food updated, CancellationToken cancellationToken)
        {
            SaveToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null)
            {
                return Task.FromException(Failure);
            }

            Saved = updated;
            return Task.CompletedTask;
        }

        public Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(Food value, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
