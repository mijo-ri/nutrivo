using Nutrivo.Api.Endpoints.Foods;
using Nutrivo.Application.Foods.CreateFood;
using Nutrivo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(options =>
{
    options.ThrowOnBadRequest = false;
});
builder.Services.AddScoped<CreateFoodCommandHandler>();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapCreateFood();

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching"
};

app.MapGet(
    "/weatherforecast",
    () =>
    {
        var forecast = Enumerable.Range(1, 5)
            .Select(
                index => new WeatherForecast(
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                )
            )
            .ToArray();
        return forecast;
    }
)
    .WithName("GetWeatherForecast");

app.Run();

public partial class Program;

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF
    {
        get
        {
            return 32 + (int)(TemperatureC / 0.5556);
        }
    }
}
