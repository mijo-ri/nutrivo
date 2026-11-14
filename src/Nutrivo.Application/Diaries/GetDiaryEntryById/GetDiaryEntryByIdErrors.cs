using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Diaries.GetDiaryEntryById;

public static class GetDiaryEntryByIdErrors
{
    public static readonly DomainError NotFound = new(
        "GetDiaryEntryById.NotFound",
        "The diary entry was not found."
    );
}
