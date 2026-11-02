using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Diaries.GetDiaryDay;

public sealed class GetDiaryDayQueryHandler(IDiaryQueries diaryQueries)
    : IQueryHandler<GetDiaryDayQuery, GetDiaryDayResult>
{
    public async Task<Result<GetDiaryDayResult>> HandleAsync(
        GetDiaryDayQuery query,
        CancellationToken cancellationToken
    )
    {
        var result = await diaryQueries.GetDayAsync(query.Date, cancellationToken);
        return Result<GetDiaryDayResult>.Success(result);
    }
}
