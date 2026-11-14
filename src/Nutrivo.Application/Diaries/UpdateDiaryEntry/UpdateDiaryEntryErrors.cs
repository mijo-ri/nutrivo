using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Diaries.UpdateDiaryEntry;

public static class UpdateDiaryEntryErrors
{
    public static readonly DomainError NotFound = new(
        "UpdateDiaryEntry.NotFound",
        "The diary entry was not found."
    );

    public static readonly DomainError InvalidAmount = new(
        "UpdateDiaryEntry.Amount.Invalid",
        "Specify exactly one quantity or serving count."
    );
}
