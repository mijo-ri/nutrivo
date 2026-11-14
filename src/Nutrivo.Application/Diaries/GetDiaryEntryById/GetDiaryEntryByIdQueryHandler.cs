using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Application.Diaries.GetDiaryEntryById;

public sealed class GetDiaryEntryByIdQueryHandler(IDiaryQueries queries)
    : IQueryHandler<GetDiaryEntryByIdQuery, DiaryEntryDetail>
{
    public async Task<Result<DiaryEntryDetail>> HandleAsync(
        GetDiaryEntryByIdQuery query,
        CancellationToken cancellationToken
    )
    {
        var id = DiaryEntryId.Create(query.Id);
        if (id.IsFailure)
        {
            return Result<DiaryEntryDetail>.Failure(id.Error!);
        }

        var entry = await queries.GetByIdAsync(id.Value, cancellationToken);
        return entry is null
            ? Result<DiaryEntryDetail>.Failure(GetDiaryEntryByIdErrors.NotFound)
            : Result<DiaryEntryDetail>.Success(entry);
    }
}
