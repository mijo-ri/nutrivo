using Nutrivo.Application.Targets.GetDailyNutritionTarget;
using Nutrivo.Application.Targets.SetDailyNutritionTarget;

namespace Nutrivo.Application.Tests.Targets;

public sealed class DailyNutritionTargetHandlerTests
{
    [Fact]
    public async Task Read_ReturnsNullUntilSetAndForwardsCancellation()
    {
        var repository = new TargetRepositoryStub();
        using var source = new CancellationTokenSource();
        var query = new GetDailyNutritionTargetQueryHandler(repository);
        Assert.Null((await query.HandleAsync(new(), source.Token)).Value.Targets);
        Assert.Equal(source.Token, repository.LastToken);
        var command = new SetDailyNutritionTargetCommandHandler(repository);
        var saved = await command.HandleAsync(
            new(2400.123456789m, 180m, 260m, 70m),
            source.Token
        );
        Assert.Equal(source.Token, repository.LastToken);
        Assert.Equal(2400.123456789m, saved.Value.Targets!.Kilocalories);
        Assert.Equal(saved.Value, (await query.HandleAsync(new(), source.Token)).Value);
        await command.HandleAsync(new(0, 0, 0, 0), source.Token);
        Assert.Equal(
            0m,
            (await query.HandleAsync(new(), source.Token)).Value.Targets!.Kilocalories
        );
        Assert.Equal(2, repository.Saves);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(0, -1, 0, 0)]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 0, -1)]
    public async Task InvalidCommand_NeverPersists(
        decimal kcal,
        decimal protein,
        decimal carbs,
        decimal fat
    )
    {
        var repository = new TargetRepositoryStub();
        var handler = new SetDailyNutritionTargetCommandHandler(repository);
        Assert.True(
            (await handler.HandleAsync(new(kcal, protein, carbs, fat), default)).IsFailure
        );
        Assert.Equal(0, repository.Saves);
        Assert.Null(repository.Current);
    }
}
