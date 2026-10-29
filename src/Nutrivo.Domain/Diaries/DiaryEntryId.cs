using Nutrivo.Domain.Common;

namespace Nutrivo.Domain.Diaries;

public sealed class DiaryEntryId : ValueObject
{
    private DiaryEntryId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static DiaryEntryId New()
    {
        return new DiaryEntryId(Guid.NewGuid());
    }

    public static Result<DiaryEntryId> Create(Guid value)
    {
        return value == Guid.Empty
            ? Result<DiaryEntryId>.Failure(DiaryEntryErrors.EmptyId)
            : Result<DiaryEntryId>.Success(new DiaryEntryId(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
