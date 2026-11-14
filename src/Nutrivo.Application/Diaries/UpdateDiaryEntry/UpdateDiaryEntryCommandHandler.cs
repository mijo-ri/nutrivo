using Nutrivo.Application.Abstractions;
using Nutrivo.Domain.Common;
using Nutrivo.Domain.Diaries;

namespace Nutrivo.Application.Diaries.UpdateDiaryEntry;

public sealed class UpdateDiaryEntryCommandHandler(IDiaryEntryRepository repository)
    : ICommandHandler<UpdateDiaryEntryCommand, DiaryEntryId>
{
    public async Task<Result<DiaryEntryId>> HandleAsync(
        UpdateDiaryEntryCommand command,
        CancellationToken cancellationToken
    )
    {
        var id = DiaryEntryId.Create(command.Id);
        if (id.IsFailure)
        {
            return Result<DiaryEntryId>.Failure(id.Error!);
        }

        if (command.Quantity.HasValue == command.ServingCount.HasValue)
        {
            return Result<DiaryEntryId>.Failure(UpdateDiaryEntryErrors.InvalidAmount);
        }

        var entry = await repository.GetForUpdateAsync(id.Value, cancellationToken);
        if (entry is null)
        {
            return Result<DiaryEntryId>.Failure(UpdateDiaryEntryErrors.NotFound);
        }

        var result = command.Quantity.HasValue
            ? entry.UpdateQuantity(command.Quantity.Value)
            : entry.UpdateServingCount(command.ServingCount!.Value);
        if (result.IsFailure)
        {
            return result;
        }

        return await repository.SaveAsync(entry, cancellationToken)
            ? result
            : Result<DiaryEntryId>.Failure(UpdateDiaryEntryErrors.NotFound);
    }
}
