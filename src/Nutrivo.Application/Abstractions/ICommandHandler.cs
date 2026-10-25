using Nutrivo.Domain.Common;

namespace Nutrivo.Application.Abstractions;

public interface ICommandHandler<TCommand, TResponse>
    where TResponse : notnull
{
    Task<Result<TResponse>> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken
    );
}
