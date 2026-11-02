using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Diaries.GetDiaryDay;

public static class GetDiaryDayErrors
{
    public static readonly DomainError TotalsOutOfRange = new(
        "GetDiaryDay.Totals.OutOfRange",
        "The diary nutrition totals exceed the supported decimal range."
    );
}
