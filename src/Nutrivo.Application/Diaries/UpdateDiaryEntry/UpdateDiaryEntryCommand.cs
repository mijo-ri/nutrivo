namespace Nutrivo.Application.Diaries.UpdateDiaryEntry;

public sealed record UpdateDiaryEntryCommand(
    Guid Id,
    decimal? Quantity = null,
    decimal? ServingCount = null
);
