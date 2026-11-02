using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.GetDiaryDay;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Diaries;

public sealed class GetDiaryDayQueryHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsync_ForwardsDateAndTokenAndReturnsUnchangedResult(
        bool withEntry
    )
    {
        var date = new DateOnly(2020, 2, 29);
        var queries = new RecordingDiaryQueries
        {
            Response = new GetDiaryDayResult(
                date,
                withEntry
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
            )
        };
        var handler = new GetDiaryDayQueryHandler(queries);
        using var cancellation = new CancellationTokenSource();

        var result = await handler.HandleAsync(
            new GetDiaryDayQuery(date),
            cancellation.Token
        );

        Assert.True(result.IsSuccess);
        Assert.Same(queries.Response, result.Value);
        Assert.Equal((date, cancellation.Token), Assert.Single(queries.Calls));
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

    private sealed class RecordingDiaryQueries : IDiaryQueries
    {
        public List<(DateOnly Date, CancellationToken Token)> Calls { get; } = [];

        public GetDiaryDayResult Response { get; init; } = new(
            new DateOnly(2030, 1, 1),
            []
        );

        public Exception? Failure { get; init; }

        public Task<GetDiaryDayResult> GetDayAsync(
            DateOnly date,
            CancellationToken cancellationToken
        )
        {
            Calls.Add((date, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null
                ? Task.FromResult(Response)
                : Task.FromException<GetDiaryDayResult>(Failure);
        }
    }
}
