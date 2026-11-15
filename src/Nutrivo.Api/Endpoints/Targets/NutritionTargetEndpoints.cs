using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using Nutrivo.Application.Abstractions;
using Nutrivo.Application.Targets;
using Nutrivo.Application.Targets.GetDailyNutritionTarget;
using Nutrivo.Application.Targets.SetDailyNutritionTarget;

namespace Nutrivo.Api.Endpoints.Targets;

public sealed class NutritionTargetEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("/nutrition-targets", GetAsync)
            .WithName("GetDailyNutritionTarget")
            .WithTags("Nutrition targets")
            .WithSummary("Gets current daily targets, or null when not configured.")
            .WithDescription(
                "Returns exact saved decimals in kcal and grams without display rounding. Reads do not create defaults. Current targets apply to every diary date, including past dates."
            )
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints
            .MapPut("/nutrition-targets", SetAsync)
            .WithName("SetDailyNutritionTarget")
            .WithTags("Nutrition targets")
            .WithSummary(
                "Creates or replaces the current daily calorie and macro targets."
            )
            .WithDescription(
                "All four numeric fields are required, independent and nonnegative; explicit zero is valid. Values use kcal and grams. No macro-energy equation is enforced. The whole set is replaced atomically; the last committed save wins. Returns exact saved values after commit. Does not change diary entries or consumed totals."
            )
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<Ok<DailyNutritionTargetResult>> GetAsync(
        IQueryHandler<GetDailyNutritionTargetQuery, DailyNutritionTargetResult> handler,
        CancellationToken cancellationToken
    )
    {
        var result = await handler.HandleAsync(
            new GetDailyNutritionTargetQuery(),
            cancellationToken
        );
        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<DailyNutritionTargetResult>, ProblemHttpResult>>
        SetAsync(
            SetNutritionTargetRequest request,
            ICommandHandler<SetDailyNutritionTargetCommand, DailyNutritionTargetResult>
                handler,
            CancellationToken cancellationToken
        )
    {
        var result = await handler.HandleAsync(
            new SetDailyNutritionTargetCommand(
                request.Kilocalories,
                request.ProteinGrams,
                request.CarbohydratesGrams,
                request.FatGrams
            ),
            cancellationToken
        );
        if (result.IsSuccess)
        {
            return TypedResults.Ok(result.Value);
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid nutrition targets.",
            detail: result.Error!.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = result.Error.Code
            }
        );
    }
}

public sealed record SetNutritionTargetRequest
{
    [Description("Daily calorie target in kcal; zero is valid.")]
    public required decimal Kilocalories { get; init; }

    [Description("Daily protein target in grams; zero is valid.")]
    public required decimal ProteinGrams { get; init; }

    [Description("Daily carbohydrate target in grams; zero is valid.")]
    public required decimal CarbohydratesGrams { get; init; }

    [Description("Daily fat target in grams; zero is valid.")]
    public required decimal FatGrams { get; init; }
}
