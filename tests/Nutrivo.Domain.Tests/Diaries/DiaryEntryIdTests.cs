using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;

namespace Nutrivo.Domain.Tests.Diaries;

public sealed class DiaryEntryIdTests
{
    [Fact]
    public void New_ProducesDistinctNonEmptyIds()
    {
        var first = DiaryEntryId.New();
        var second = DiaryEntryId.New();

        Assert.NotEqual(Guid.Empty, first.Value);
        Assert.NotEqual(Guid.Empty, second.Value);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_WithEmptyGuid_ReturnsDomainError()
    {
        var result = DiaryEntryId.Create(Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Equal("DiaryEntry.Id.Empty", result.Error?.Code);
    }

    [Fact]
    public void Create_WithExistingGuid_PreservesTypedValueEquality()
    {
        var guid = Guid.NewGuid();
        var first = DiaryEntryId.Create(guid).Value;
        var second = DiaryEntryId.Create(guid).Value;

        Assert.Equal(guid, first.Value);
        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.False(first != second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Single(new HashSet<DiaryEntryId> { first, second });
        Assert.False(first.Equals(FoodId.Create(guid).Value));
        Assert.False(first.Equals(ServingSizeId.Create(guid).Value));
    }
}
