using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Tests.Common;

public class ValueObjectTests
{
    [Fact]
    public void Equality_WithEqualComponentsIncludingNull_SupportsHashCollections()
    {
        var first = new TestValue("Example", null);
        var second = new TestValue("Example", null);

        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<ValueObject> { first, second });
    }

    [Fact]
    public void Equality_WithDifferentComponentOrder_ReturnsFalse()
    {
        var first = new TestValue("First", "Second");
        var second = new TestValue("Second", "First");

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equality_WithDifferentTypesAndSameComponents_ReturnsFalseSymmetrically()
    {
        ValueObject first = new TestValue("Example", null);
        ValueObject second = new OtherValue("Example", null);

        Assert.False(first.Equals(second));
        Assert.False(second.Equals(first));
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equality_HandlesSameReferenceNullAndUnrelatedObjects()
    {
        ValueObject value = new TestValue("Example", null);
        var sameReference = value;
        ValueObject? missing = null;
        ValueObject? alsoMissing = null;

        Assert.True(value == sameReference);
        Assert.True(missing == alsoMissing);
        Assert.False(value == missing);
        Assert.False(missing == value);
        Assert.True(value != missing);
        Assert.False(value.Equals(missing));
        Assert.False(value.Equals((object?)null));
        Assert.False(value.Equals("unrelated"));
    }

    private sealed class TestValue(string? first, string? second) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return first;
            yield return second;
        }
    }

    private sealed class OtherValue(string? first, string? second) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return first;
            yield return second;
        }
    }
}
