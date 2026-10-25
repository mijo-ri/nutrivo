namespace Nutrivo.Api.Endpoints;

/// <summary>
/// Maps routes at startup. Implementations are stateless singletons;
/// inject request dependencies into HTTP handler parameters.
/// </summary>
public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
