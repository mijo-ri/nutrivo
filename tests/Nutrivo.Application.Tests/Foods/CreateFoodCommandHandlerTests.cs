using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.CreateFood;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Foods;

public sealed class CreateFoodCommandHandlerTests
{
    private static readonly CreateFoodCommand ValidCommand = new(
        " \tRolled  oats\n", " Example  brand ", ReferenceUnit.Gram,
        372.123456789m, 13.5m, 58.7m, 7m
    );

    [Theory]
    [InlineData(ReferenceUnit.Gram)]
    [InlineData(ReferenceUnit.Milliliter)]
    public async Task HandleAsync_WithValidData_PersistsFoodAndReturnsItsId(
        ReferenceUnit unit
    )
    {
        var repository = new RecordingFoodRepository();
        var handler = new CreateFoodCommandHandler(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await handler.HandleAsync(
            ValidCommand with { ReferenceUnit = unit }, cancellation.Token
        );

        Assert.True(result.IsSuccess);
        var food = Assert.Single(repository.Foods);
        Assert.Equal(food.Id, result.Value);
        Assert.NotEqual(Guid.Empty, result.Value.Value);
        Assert.Equal("Rolled  oats", food.Name);
        Assert.Equal("Example  brand", food.Brand);
        Assert.Equal(unit, food.ReferenceUnit);
        Assert.Equal(100m, food.ReferenceQuantity);
        Assert.Equal(372.123456789m, food.NutritionalValues.Kilocalories);
        Assert.Equal(13.5m, food.NutritionalValues.ProteinGrams);
        Assert.Equal(58.7m, food.NutritionalValues.CarbohydratesGrams);
        Assert.Equal(7m, food.NutritionalValues.FatGrams);
        Assert.Equal(cancellation.Token, repository.CancellationToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\n")]
    public async Task HandleAsync_WithEmptyBrandAndZeroNutrition_PersistsKnownZeros(
        string? brand
    )
    {
        var repository = new RecordingFoodRepository();
        var handler = new CreateFoodCommandHandler(repository);
        var command = ValidCommand with
        {
            Brand = brand,
            Kilocalories = 0m,
            ProteinGrams = 0m,
            CarbohydratesGrams = 0m,
            FatGrams = 0m
        };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var food = Assert.Single(repository.Foods);
        Assert.Null(food.Brand);
        Assert.Equal(
            NutritionalValues.Create(0m, 0m, 0m, 0m).Value, food.NutritionalValues
        );
    }

    public static TheoryData<CreateFoodCommand, DomainError> InvalidCommands => new()
    {
        { ValidCommand with { Name = null }, FoodErrors.NameRequired },
        { ValidCommand with { Name = " \t" }, FoodErrors.NameRequired },
        {
            ValidCommand with { Name = new string('a', 201) },
            FoodErrors.NameTooLong
        },
        {
            ValidCommand with { Brand = new string('b', 201) },
            FoodErrors.BrandTooLong
        },
        {
            ValidCommand with { ReferenceUnit = (ReferenceUnit)0 },
            FoodErrors.InvalidReferenceUnit
        },
        { ValidCommand with { Kilocalories = -1m }, FoodErrors.NegativeKilocalories },
        { ValidCommand with { ProteinGrams = -1m }, FoodErrors.NegativeProtein },
        {
            ValidCommand with { CarbohydratesGrams = -1m },
            FoodErrors.NegativeCarbohydrates
        },
        { ValidCommand with { FatGrams = -1m }, FoodErrors.NegativeFat },
        {
            ValidCommand with { Name = null, Kilocalories = -1m, ProteinGrams = -1m },
            FoodErrors.NegativeKilocalories
        },
        {
            ValidCommand with { Name = null, ReferenceUnit = (ReferenceUnit)0 },
            FoodErrors.NameRequired
        }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task HandleAsync_WithInvalidData_ReturnsDomainErrorWithoutPersisting(
        CreateFoodCommand command,
        DomainError expectedError
    )
    {
        var repository = new RecordingFoodRepository();
        var handler = new CreateFoodCommandHandler(repository);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Same(expectedError, result.Error);
        Assert.Empty(repository.Foods);
    }

    [Fact]
    public async Task HandleAsync_WhenSavingFails_PropagatesTechnicalFailure()
    {
        var failure = new InvalidOperationException("Storage unavailable.");
        var repository = new RecordingFoodRepository { Failure = failure };
        var handler = new CreateFoodCommandHandler(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(ValidCommand, CancellationToken.None)
        );

        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_PropagatesCancellation()
    {
        var repository = new RecordingFoodRepository();
        var handler = new CreateFoodCommandHandler(repository);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(ValidCommand, cancellation.Token)
        );

        Assert.Empty(repository.Foods);
    }

    private sealed class RecordingFoodRepository : IFoodRepository
    {
        public List<Food> Foods { get; } = [];

        public CancellationToken CancellationToken { get; private set; }

        public Exception? Failure { get; init; }

        public Task AddAsync(Food food, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            if (Failure is not null)
            {
                return Task.FromException(Failure);
            }

            Foods.Add(food);
            return Task.CompletedTask;
        }
    }
}
