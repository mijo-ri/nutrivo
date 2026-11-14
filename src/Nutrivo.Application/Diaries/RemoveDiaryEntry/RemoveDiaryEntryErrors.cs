using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Diaries.RemoveDiaryEntry;

public static class RemoveDiaryEntryErrors
{
    public static readonly DomainError NotFound = new(
        "RemoveDiaryEntry.NotFound",
        "The diary entry was not found."
    );
}
