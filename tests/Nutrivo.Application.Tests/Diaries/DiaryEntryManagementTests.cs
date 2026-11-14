using System.Collections.Immutable;
using Nutrivo.Application.Diaries;
using Nutrivo.Application.Diaries.GetDiaryDay;
using Nutrivo.Application.Diaries.GetDiaryEntryById;
using Nutrivo.Application.Diaries.UpdateDiaryEntry;
using Nutrivo.Application.Diaries.RemoveDiaryEntry;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Application.Tests.Diaries;

public sealed class DiaryEntryManagementTests
{
    private static DiaryEntry Entry(bool serving = false)
    {
        var food = Food.Create(
                "Egg",
                null,
                100m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(200m, 10m, 20m, 5m).Value,
                [ServingSize.Create(1m, "piece", null, 50m).Value]
            )
            .Value;
        return serving
            ? DiaryEntry.CreateFromServing(
                    food,
                    new DateOnly(2026, 11, 14),
                    MealType.Lunch,
                    food.ServingSizes.Single().Id,
                    1m
                )
                .Value
            : DiaryEntry.Create(
                    food,
                    new DateOnly(2026, 11, 14),
                    MealType.Lunch,
                    50m,
                    ReferenceUnit.Gram
                )
                .Value;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_DelegatesToDomainAndCommits(bool serving)
    {
        var entry = Entry(serving);
        var repo = new Entries(entry);
        using var cancel = new CancellationTokenSource();
        var result = await new UpdateDiaryEntryCommandHandler(repo).HandleAsync(
            new(entry.Id.Value, serving ? null : 25m, serving ? 0.5m : null),
            cancel.Token
        );
        Assert.True(result.IsSuccess);
        Assert.Equal(25m, entry.Quantity);
        Assert.Equal(50m, entry.NutritionalValues.Kilocalories);
        Assert.True(repo.Saved);
        Assert.Equal(cancel.Token, repo.Token);
    }

    [Fact]
    public async Task Update_RejectsInvalidInputAndDoesNotSave()
    {
        var entry = Entry();
        foreach (var command in new[]
                 {
                     new UpdateDiaryEntryCommand(entry.Id.Value),
                     new(entry.Id.Value, 1m, 1m), new(entry.Id.Value, 0m),
                     new(entry.Id.Value, -1m), new(entry.Id.Value, null, 1m),
                     new(Guid.Empty, 1m)
                 })
        {
            var repo = new Entries(entry);
            var result =
                await new UpdateDiaryEntryCommandHandler(repo).HandleAsync(
                    command,
                    default
                );
            Assert.True(result.IsFailure);
            Assert.False(repo.Saved);
            Assert.Equal(50m, entry.Quantity);
        }
    }

    [Fact]
    public async Task Update_MissingAndConcurrentlyDeletedEntriesReturnNotFound()
    {
        var entry = Entry();
        foreach (var repo in new[]
                 {
                     new Entries(null), new Entries(entry)
                     {
                         SaveResult = false
                     }
                 })
        {
            var result = await new UpdateDiaryEntryCommandHandler(repo).HandleAsync(
                new(entry.Id.Value, 25m),
                default
            );
            Assert.Equal(UpdateDiaryEntryErrors.NotFound, result.Error);
        }
    }

    [Fact]
    public async Task Remove_ValidatesIdAndReportsMissingTarget()
    {
        var entry = Entry();
        var repo = new Entries(entry);
        var handler = new RemoveDiaryEntryCommandHandler(repo);
        Assert.Equal(
            DiaryEntryErrors.EmptyId,
            (await handler.HandleAsync(new(Guid.Empty), default)).Error
        );
        Assert.False(repo.Deleted);
        Assert.Equal(
            RemoveDiaryEntryErrors.NotFound,
            (await handler.HandleAsync(new(Guid.NewGuid()), default)).Error
        );
        Assert.True((await handler.HandleAsync(new(entry.Id.Value), default)).IsSuccess);
        Assert.True(repo.Deleted);
    }

    [Fact]
    public async Task Detail_ReturnsExactReadModelAndValidatesIdentity()
    {
        var entry = Entry();
        var model = new DiaryEntryDetail(
            entry.Id.Value,
            entry.Date,
            entry.MealType,
            50m,
            ReferenceUnit.Gram,
            100m,
            5m,
            10m,
            2.5m,
            null,
            null,
            new(
                entry.FoodSnapshot.FoodId.Value,
                "Egg",
                null,
                100m,
                ReferenceUnit.Gram,
                200m,
                10m,
                20m,
                5m
            )
        );
        var queries = new Queries(model);
        var handler = new GetDiaryEntryByIdQueryHandler(queries);
        using var cancel = new CancellationTokenSource();
        Assert.Same(
            model,
            (await handler.HandleAsync(new(model.Id), cancel.Token)).Value
        );
        Assert.Equal(cancel.Token, queries.Token);
        Assert.Equal(
            DiaryEntryErrors.EmptyId,
            (await handler.HandleAsync(new(Guid.Empty), default)).Error
        );
        Assert.Equal(
            GetDiaryEntryByIdErrors.NotFound,
            (await handler.HandleAsync(new(Guid.NewGuid()), default)).Error
        );
    }

    [Fact]
    public async Task Handlers_PropagateCancellationAndStorageFailures()
    {
        var entry = Entry();
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GetDiaryEntryByIdQueryHandler(new Queries(null))
                .HandleAsync(new(entry.Id.Value), cancel.Token)
        );
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new UpdateDiaryEntryCommandHandler(new Entries(entry))
                .HandleAsync(new(entry.Id.Value, 1m), cancel.Token)
        );
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new RemoveDiaryEntryCommandHandler(new Entries(entry))
                .HandleAsync(new(entry.Id.Value), cancel.Token)
        );
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GetDiaryEntryByIdQueryHandler(
                    new Queries(null)
                    {
                        Fail = true
                    }
                )
                .HandleAsync(new(entry.Id.Value), default)
        );
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new UpdateDiaryEntryCommandHandler(
                    new Entries(entry)
                    {
                        Fail = true
                    }
                )
                .HandleAsync(new(entry.Id.Value, 1m), default)
        );
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RemoveDiaryEntryCommandHandler(
                    new Entries(entry)
                    {
                        Fail = true
                    }
                )
                .HandleAsync(new(entry.Id.Value), default)
        );
    }

    private sealed class Entries(DiaryEntry? entry) : IDiaryEntryRepository
    {
        public bool Saved { get; private set; }
        public bool Deleted { get; private set; }
        public bool SaveResult { get; init; } = true;
        public bool Fail { get; init; }
        public CancellationToken Token { get; private set; }

        private void Check(CancellationToken token)
        {
            Token = token;
            token.ThrowIfCancellationRequested();
            if (Fail)
                throw new InvalidOperationException("Storage unavailable");
        }

        public Task<DiaryEntry?> GetForUpdateAsync(
            DiaryEntryId id,
            CancellationToken token
        )
        {
            Check(token);
            return Task.FromResult(entry?.Id == id ? entry : null);
        }

        public Task<bool> SaveAsync(DiaryEntry value, CancellationToken token)
        {
            Check(token);
            Saved = true;
            return Task.FromResult(SaveResult);
        }

        public Task<bool> DeleteAsync(DiaryEntryId id, CancellationToken token)
        {
            Check(token);
            Deleted = entry?.Id == id;
            return Task.FromResult(Deleted);
        }

        public Task AddAsync(DiaryEntry value, CancellationToken token) =>
            throw new NotSupportedException();
    }

    private sealed class Queries(DiaryEntryDetail? entry) : IDiaryQueries
    {
        public bool Fail { get; init; }
        public CancellationToken Token { get; private set; }

        public Task<DiaryEntryDetail?> GetByIdAsync(
            DiaryEntryId id,
            CancellationToken token
        )
        {
            Token = token;
            token.ThrowIfCancellationRequested();
            if (Fail)
                throw new InvalidOperationException("Storage unavailable");
            return Task.FromResult(entry?.Id == id.Value ? entry : null);
        }

        public Task<ImmutableArray<DiaryDayEntry>> GetDayAsync(
            DateOnly date,
            CancellationToken token
        ) => throw new NotSupportedException();
    }
}
