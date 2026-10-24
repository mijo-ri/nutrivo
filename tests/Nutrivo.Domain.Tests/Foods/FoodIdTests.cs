using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Foods;

public class FoodIdTests
{
    [Fact]
    public void New_ProducesDistinctNonEmptyIds()
    {
        var first = FoodId.New();
        var second = FoodId.New();

        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(Guid.Empty, second.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_WithEmptyGuid_ReturnsError()
    {
        var result = FoodId.Create(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("Food.Id.Empty", result.Error?.Code);
    }

    [Fact]
    public void Create_WithExistingGuid_PreservesIdentityAndValueEquality()
    {
        var guid = Guid.NewGuid();
        var first = FoodId.Create(guid).Value;
        var second = FoodId.Create(guid).Value;

        Assert.Equal(guid, first.Value);
        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<FoodId> { first, second });
    }
}
