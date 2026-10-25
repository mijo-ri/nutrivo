using Nutrivo.Application.Foods;
using Nutrivo.Application.Foods.SearchFoods;

namespace Nutrivo.Application.Tests.Foods;

public sealed class SearchFoodsQueryHandlerTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData(" \t\n", null)]
    [InlineData("  Rolled  Oats \t", "Rolled  Oats")]
    [InlineData("a", "a")]
    public async Task HandleAsync_NormalizesSearchAndForwardsDefaultsAndToken(
        string? input,
        string? expected
    )
    {
        var search = new RecordingFoodQueries();
        var handler = new SearchFoodsQueryHandler(search);
        using var cancellation = new CancellationTokenSource();

        var result = await handler.HandleAsync(
            new SearchFoodsQuery(input),
            cancellation.Token
        );

        Assert.True(result.IsSuccess);
        Assert.Same(search.Response, result.Value);
        Assert.Equal((expected, 1, 20, cancellation.Token), Assert.Single(search.Calls));
    }

    [Theory]
    [InlineData(0, 20, "SearchFoods.Page.Invalid")]
    [InlineData(-1, 20, "SearchFoods.Page.Invalid")]
    [InlineData(int.MinValue, 20, "SearchFoods.Page.Invalid")]
    [InlineData(1, 0, "SearchFoods.PageSize.Invalid")]
    [InlineData(1, -1, "SearchFoods.PageSize.Invalid")]
    [InlineData(1, 101, "SearchFoods.PageSize.Invalid")]
    [InlineData(1, int.MaxValue, "SearchFoods.PageSize.Invalid")]
    [InlineData(int.MaxValue, 2, "SearchFoods.Page.Invalid")]
    [InlineData(1073741825, 2, "SearchFoods.Page.Invalid")]
    public async Task HandleAsync_WithInvalidPagination_DoesNotSearch(
        int page,
        int pageSize,
        string code
    )
    {
        var search = new RecordingFoodQueries();
        var handler = new SearchFoodsQueryHandler(search);

        var result = await handler.HandleAsync(
            new SearchFoodsQuery(Page: page, PageSize: pageSize),
            CancellationToken.None
        );

        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error!.Code);
        Assert.Empty(search.Calls);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 100)]
    [InlineData(int.MaxValue, 1)]
    [InlineData(1073741824, 2)]
    public async Task HandleAsync_WithValidPagination_ForwardsParameters(
        int page,
        int pageSize
    )
    {
        var search = new RecordingFoodQueries();
        var handler = new SearchFoodsQueryHandler(search);

        var result = await handler.HandleAsync(
            new SearchFoodsQuery("oats", page, pageSize),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ("oats", page, pageSize, CancellationToken.None),
            Assert.Single(search.Calls)
        );
    }

    [Fact]
    public async Task HandleAsync_WhenStorageFails_PropagatesTechnicalFailure()
    {
        var failure = new InvalidOperationException("Storage unavailable.");
        var handler =
            new SearchFoodsQueryHandler(new RecordingFoodQueries { Failure = failure });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new SearchFoodsQuery(), CancellationToken.None)
        );

        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task HandleAsync_WhenCancelled_PropagatesCancellation()
    {
        var handler = new SearchFoodsQueryHandler(new RecordingFoodQueries());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(new SearchFoodsQuery(), cancellation.Token)
        );
    }

    private sealed class RecordingFoodQueries : IFoodQueries
    {
        public List<(string? Search, int Page, int PageSize, CancellationToken Token)>
            Calls { get; } = [];

        public SearchFoodsResult Response { get; } = new([], 1, 20, 0);

        public Exception? Failure { get; init; }

        public Task<SearchFoodsResult> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            Calls.Add((search, page, pageSize, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            return Failure is null
                ? Task.FromResult(Response)
                : Task.FromException<SearchFoodsResult>(Failure);
        }
    }
}
