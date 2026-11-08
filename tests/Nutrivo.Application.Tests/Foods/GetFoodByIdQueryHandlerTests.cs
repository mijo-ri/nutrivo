using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.GetFoodById;
using Nutrivo.Application.Foods.SearchFoods;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Foods;

public sealed class GetFoodByIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsFoodAndForwardsIdAndToken()
    {
        var food = new FoodReadModel(
            Guid.NewGuid(),
            "Oats",
            null,
            100m,
            ReferenceUnit.Gram,
            372m,
            13m,
            58m,
            7m,
            []
        );
        var queries = new RecordingFoodQueries
        {
            Food = food
        };
        using var cancellation = new CancellationTokenSource();

        var result = await new GetFoodByIdQueryHandler(queries)
            .HandleAsync(new GetFoodByIdQuery(food.Id), cancellation.Token);

        Assert.True(result.IsSuccess);
        Assert.Same(food, result.Value);
        var call = Assert.Single(queries.Calls);
        Assert.Equal(food.Id, call.Id.Value);
        Assert.Equal(cancellation.Token, call.Token);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyId_DoesNotReadStorage()
    {
        var queries = new RecordingFoodQueries();
        var result = await new GetFoodByIdQueryHandler(queries)
            .HandleAsync(new GetFoodByIdQuery(Guid.Empty), CancellationToken.None);

        Assert.Equal(FoodErrors.EmptyId, result.Error);
        Assert.Empty(queries.Calls);
    }

    [Fact]
    public async Task HandleAsync_WithMissingFood_ReturnsNotFound()
    {
        var result = await new GetFoodByIdQueryHandler(new RecordingFoodQueries())
            .HandleAsync(new GetFoodByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(GetFoodByIdErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var handler = new GetFoodByIdQueryHandler(new RecordingFoodQueries());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
                new GetFoodByIdQuery(Guid.NewGuid()),
                cancellation.Token
            )
        );
    }

    [Fact]
    public async Task HandleAsync_PropagatesStorageFailure()
    {
        var failure = new InvalidOperationException("Storage unavailable.");
        var handler = new GetFoodByIdQueryHandler(
            new RecordingFoodQueries
            {
                Failure = failure
            }
        );

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new GetFoodByIdQuery(Guid.NewGuid()),
                CancellationToken.None
            )
        );

        Assert.Same(failure, actual);
    }

    private sealed class RecordingFoodQueries : IFoodQueries
    {
        public FoodReadModel? Food { get; init; }
        public Exception? Failure { get; init; }
        public List<(FoodId Id, CancellationToken Token)> Calls { get; } = [];

        public Task<FoodReadModel?> GetByIdAsync(
            FoodId id,
            CancellationToken cancellationToken
        )
        {
            Calls.Add((id, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null
                ? Task.FromResult(Food)
                : Task.FromException<FoodReadModel?>(Failure);
        }

        public Task<SearchFoodsResult> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
