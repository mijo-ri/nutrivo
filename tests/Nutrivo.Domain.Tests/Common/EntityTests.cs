using Nutrivo.Domain.Common;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Common;

public class EntityTests
{
    [Fact]
    public void Equality_WithSameConcreteTypeAndEqualIds_IgnoresOtherState()
    {
        var guid = Guid.NewGuid();
        var first = new TestEntity(FoodId.Create(guid).Value, "First");
        var second = new TestEntity(FoodId.Create(guid).Value, "Second");

        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<Entity<FoodId>> { first, second });
    }

    [Fact]
    public void Equality_WithDifferentIds_ReturnsFalse()
    {
        var first = new TestEntity(FoodId.New(), "Same");
        var second = new TestEntity(FoodId.New(), "Same");

        Assert.NotEqual(first, second);
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equality_WithDifferentConcreteTypesAndSameId_ReturnsFalseSymmetrically()
    {
        var id = FoodId.New();
        Entity<FoodId> first = new TestEntity(id, "Name");
        Entity<FoodId> second = new OtherEntity(id);

        Assert.False(first.Equals(second));
        Assert.False(second.Equals(first));
        Assert.False(first == second);
        Assert.True(first != second);
    }

    [Fact]
    public void Equality_HandlesSameReferenceNullAndUnrelatedObjects()
    {
        Entity<FoodId> entity = new TestEntity(FoodId.New(), "Name");
        var sameReference = entity;
        Entity<FoodId>? missing = null;
        Entity<FoodId>? alsoMissing = null;

        Assert.True(entity == sameReference);
        Assert.True(missing == alsoMissing);
        Assert.False(entity == missing);
        Assert.False(missing == entity);
        Assert.True(entity != missing);
        Assert.False(entity.Equals(missing));
        Assert.False(entity.Equals((object?)null));
        Assert.False(entity.Equals("unrelated"));
    }

    [Fact]
    public void Constructor_WithNullId_RejectsProgrammingError()
    {
        Assert.Throws<ArgumentNullException>(() => new TestEntity(null!, "Name"));
    }

    private sealed class TestEntity(FoodId id, string name) : Entity<FoodId>(id)
    {
        public string Name { get; } = name;
    }

    private sealed class OtherEntity(FoodId id) : Entity<FoodId>(id);
}
