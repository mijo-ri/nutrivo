using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.DeleteFood;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Foods;

public sealed class DeleteFoodCommandHandlerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_DeletesWithTokenAndReportsResult(bool found)
    {
        var id = Guid.NewGuid();
        var repository = new RecordingRepository
        {
            Found = found
        };
        using var cancellation = new CancellationTokenSource();
        var result =
            await new DeleteFoodCommandHandler(repository).HandleAsync(
                new(id),
                cancellation.Token
            );
        Assert.Equal(id, repository.Id!.Value);
        Assert.Equal(cancellation.Token, repository.Token);
        if (found)
        {
            Assert.Equal(id, result.Value.Value);
        }
        else
        {
            Assert.Equal(DeleteFoodErrors.NotFound, result.Error);
        }
    }

    [Fact]
    public async Task Handle_EmptyIdDoesNotAccessRepository()
    {
        var repository = new RecordingRepository();
        var result = await new DeleteFoodCommandHandler(repository).HandleAsync(
            new(Guid.Empty),
            CancellationToken.None
        );
        Assert.Equal(FoodErrors.EmptyId, result.Error);
        Assert.Null(repository.Id);
    }

    [Fact]
    public async Task Handle_PropagatesCancellation()
    {
        var repository = new RecordingRepository();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new DeleteFoodCommandHandler(repository).HandleAsync(
                new(Guid.NewGuid()),
                cancellation.Token
            )
        );
    }

    [Fact]
    public async Task Handle_PropagatesStorageFailure()
    {
        var failure = new InvalidOperationException("storage failed");
        var repository = new RecordingRepository
        {
            Failure = failure
        };
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new DeleteFoodCommandHandler(repository).HandleAsync(
                new(Guid.NewGuid()),
                CancellationToken.None
            )
        );
        Assert.Same(failure, actual);
    }

    private sealed class RecordingRepository : IFoodRepository
    {
        public bool Found { get; init; }
        public Exception? Failure { get; init; }
        public FoodId? Id { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<bool> DeleteAsync(FoodId id, CancellationToken cancellationToken)
        {
            Id = id;
            Token = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null)
            {
                throw Failure;
            }

            return Task.FromResult(Found);
        }

        public Task<Food?> GetByIdAsync(FoodId id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Food?> GetForUpdateAsync(
            FoodId id,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task SaveAsync(Food food, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddAsync(Food food, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
