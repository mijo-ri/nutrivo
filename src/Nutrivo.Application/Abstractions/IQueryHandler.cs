using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Abstractions;

public interface IQueryHandler<TQuery, TResponse>
    where TResponse : notnull
{
    Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken
    );
}
