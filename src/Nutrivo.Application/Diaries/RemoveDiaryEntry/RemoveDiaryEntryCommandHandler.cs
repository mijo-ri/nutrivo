using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Application.Diaries.RemoveDiaryEntry;

public sealed class RemoveDiaryEntryCommandHandler(IDiaryEntryRepository repository)
    : ICommandHandler<RemoveDiaryEntryCommand, DiaryEntryId>
{
    public async Task<Result<DiaryEntryId>> HandleAsync(
        RemoveDiaryEntryCommand command,
        CancellationToken cancellationToken
    )
    {
        var id = DiaryEntryId.Create(command.Id);
        if (id.IsFailure)
        {
            return Result<DiaryEntryId>.Failure(id.Error!);
        }

        return await repository.DeleteAsync(id.Value, cancellationToken)
            ? Result<DiaryEntryId>.Success(id.Value)
            : Result<DiaryEntryId>.Failure(RemoveDiaryEntryErrors.NotFound);
    }
}
