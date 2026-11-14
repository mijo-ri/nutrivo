using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Nutrivo.Api.Endpoints.Diaries.GetDiaryEntryById;
using Nutrivo.Api.Endpoints.Diaries.GetDiaryDay;
using Nutrivo.Application.Diaries;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Foods;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Diaries;

[Collection("PostgreSQL")]
public sealed class DiaryEntryManagementEndpointTests(PostgreSqlFixture fixture)
{
    private static readonly DateOnly Day = new(2026, 11, 14);

    private static HttpClient Client(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            }
        );

    private async
        Task<(DbContextOptions<NutrivoDbContext> Options, Food Food, DiaryEntry Entry,
            DiaryEntry Other)> Seed(bool serving = false)
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var db = new NutrivoDbContext(options);
        await db.Database.MigrateAsync();
        var food = Food.Create(
                "Original",
                "Brand",
                100m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(200.005m, 10.005m, 20m, 5m).Value,
                [ServingSize.Create(2m, "pieces", "Original", 116m).Value]
            )
            .Value;
        var entry = serving
            ? DiaryEntry.CreateFromServing(
                    food,
                    Day,
                    MealType.Lunch,
                    food.ServingSizes.Single().Id,
                    1m
                )
                .Value
            : DiaryEntry.Create(food, Day, MealType.Lunch, 50m, ReferenceUnit.Gram).Value;
        var other = DiaryEntry.Create(food, Day, MealType.Lunch, 20m, ReferenceUnit.Gram)
            .Value;
        db.AddRange(food, entry, other);
        db.Add(
            DiaryEntry.Create(
                    food,
                    Day.AddDays(-1),
                    MealType.Breakfast,
                    30m,
                    ReferenceUnit.Gram
                )
                .Value
        );
        await db.SaveChangesAsync();
        return (options, food, entry, other);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetailAndUpdate_PreserveSnapshotsAndOrderAfterCatalogueDeletion(
        bool serving
    )
    {
        var (options, food, entry, other) = await Seed(serving);
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        var before =
            await client.GetFromJsonAsync<GetDiaryDayResponse>(
                $"/diary-entries?date={Day:yyyy-MM-dd}"
            );
        DateTimeOffset captured;
        await using (var db = new NutrivoDbContext(options))
        {
            captured = await db
                .DiaryEntries.Where(x => x.Id == entry.Id)
                .Select(x => EF.Property<DateTimeOffset>(x, "CreatedAt"))
                .SingleAsync();
        }

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/foods/{food.Id.Value}")).StatusCode
        );
        var detail =
            await client.GetFromJsonAsync<DiaryEntryDetailResponse>(
                $"/diary-entries/{entry.Id.Value}"
            );
        Assert.Equal("Original", detail!.Food.Name);
        Assert.Equal(200.005m, detail.Food.Kilocalories);
        Assert.Equal(100m, detail.Food.ReferenceQuantity);
        Assert.Equal(serving ? 1m : null, detail.ServingCount);
        Assert.Equal(serving ? 116m : 50m, detail.Quantity);
        using var changed = await client.PutAsJsonAsync(
            $"/diary-entries/{entry.Id.Value}",
            serving
                ? new
                {
                    quantity = (decimal?)null,
                    servingCount = (decimal?)0.5m
                }
                : new
                {
                    quantity = (decimal?)75m,
                    servingCount = (decimal?)null
                }
        );
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Empty(await changed.Content.ReadAsByteArrayAsync());
        await using var verify = new NutrivoDbContext(options);
        var stored = await verify.DiaryEntries.SingleAsync(x => x.Id == entry.Id);
        Assert.Equal(serving ? 58m : 75m, stored.Quantity);
        Assert.Equal(
            serving ? 116.0029m : 150.00375m,
            stored.NutritionalValues.Kilocalories
        );
        Assert.Equal(entry.FoodSnapshot, stored.FoodSnapshot);
        Assert.Equal(entry.ServingSizeSnapshot, stored.ServingSizeSnapshot);
        Assert.Equal(Day, stored.Date);
        Assert.Equal(MealType.Lunch, stored.MealType);
        Assert.Equal(
            captured,
            verify.Entry(stored).Property<DateTimeOffset>("CreatedAt").CurrentValue
        );
        var after =
            await client.GetFromJsonAsync<GetDiaryDayResponse>(
                $"/diary-entries?date={Day:yyyy-MM-dd}"
            );
        Assert.Equal(before!.Entries.Select(x => x.Id), after!.Entries.Select(x => x.Id));
        Assert.Equal(serving ? 156m : 190m, after.Totals.Kilocalories);
        Assert.Equal(
            after.Totals,
            after.MealTotals.Single(x => x.MealType == MealType.Lunch).Totals
        );
        Assert.Equal(
            other.Quantity,
            (await verify.DiaryEntries.SingleAsync(x => x.Id == other.Id)).Quantity
        );
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"quantity\":1,\"servingCount\":1}")]
    [InlineData("{\"quantity\":0}")]
    [InlineData("{\"quantity\":-1}")]
    [InlineData("{\"servingCount\":1}")]
    public async Task InvalidUpdate_DoesNotChangeEntry(string json)
    {
        var (options, _, entry, _) = await Seed();
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        using var response = await client.PutAsync(
            $"/diary-entries/{entry.Id.Value}",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        );
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType!.MediaType
        );
        await using var db = new NutrivoDbContext(options);
        Assert.Equal(
            50m,
            (await db.DiaryEntries.SingleAsync(x => x.Id == entry.Id)).Quantity
        );
    }

    [Theory]
    [InlineData("not-a-guid", 400)]
    [InlineData("00000000-0000-0000-0000-000000000000", 400)]
    [InlineData("0d116f7f-5f46-47a8-9574-a1c5b6f37476", 404)]
    public async Task MissingAndInvalidIds_UseProblemDetails(string id, int status)
    {
        var (options, _, _, _) = await Seed();
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        foreach (var method in new[]
                 {
                     HttpMethod.Get, HttpMethod.Put, HttpMethod.Delete
                 })
        {
            using var request = new HttpRequestMessage(method, $"/diary-entries/{id}");
            if (method == HttpMethod.Put)
                request.Content = JsonContent.Create(
                    new
                    {
                        quantity = 1m
                    }
                );
            using var response = await client.SendAsync(request);
            Assert.Equal(status, (int)response.StatusCode);
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType!.MediaType
            );
            if (status == 404)
            {
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
                var name = method == HttpMethod.Get ? "GetDiaryEntryById" :
                    method == HttpMethod.Put ? "UpdateDiaryEntry" : "RemoveDiaryEntry";
                Assert.Equal(name + ".NotFound", problem.GetProperty("code").GetString());
            }
        }
    }

    [Fact]
    public async Task Delete_OnlyRemovesSelectedEntryAndConcurrentRequestsHaveOneWinner()
    {
        var (options, food, entry, other) = await Seed();
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        var responses = await Task.WhenAll(
            client.DeleteAsync($"/diary-entries/{entry.Id.Value}"),
            client.DeleteAsync($"/diary-entries/{entry.Id.Value}")
        );
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.NotFound);
        foreach (var response in responses)
            response.Dispose();
        await using var db = new NutrivoDbContext(options);
        Assert.Equal(2, await db.DiaryEntries.CountAsync());
        Assert.Equal(food.Id, (await db.Foods.SingleAsync()).Id);
        Assert.Single((await db.Foods.SingleAsync()).ServingSizes);
        var day =
            await client.GetFromJsonAsync<GetDiaryDayResponse>(
                $"/diary-entries?date={Day:yyyy-MM-dd}"
            );
        Assert.Equal(other.Id.Value, Assert.Single(day!.Entries).Id);
        Assert.Equal(40m, day.Totals.Kilocalories);
    }

    [Fact]
    public async Task TrackedUpdate_AfterConcurrentDelete_DoesNotRecreateEntry()
    {
        var (options, _, entry, _) = await Seed();
        await using var factory = new FoodApiFactory(options);
        using var scope = factory.Services.CreateScope();
        var repository =
            scope.ServiceProvider.GetRequiredService<IDiaryEntryRepository>();
        var tracked = await repository.GetForUpdateAsync(entry.Id, default);
        using var client = Client(factory);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/diary-entries/{entry.Id.Value}")).StatusCode
        );
        tracked!.UpdateQuantity(75m);
        Assert.False(await repository.SaveAsync(tracked, default));
        await using var db = new NutrivoDbContext(options);
        Assert.False(await db.DiaryEntries.AnyAsync(x => x.Id == entry.Id));
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task DatabaseWriteFailure_IsGenericAndAtomic(string method)
    {
        var (options, _, entry, _) = await Seed();
        await using (var db = new NutrivoDbContext(options))
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE FUNCTION reject_diary_write() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'secret-storage-details'; END; $$"
            );
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER reject_diary_write BEFORE UPDATE OR DELETE ON public.diary_entries FOR EACH ROW EXECUTE FUNCTION reject_diary_write()"
            );
        }

        await using var factory = new FoodApiFactory(options, "Production");
        using var client = Client(factory);
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            $"/diary-entries/{entry.Id.Value}"
        );
        if (method == "PUT")
            request.Content = JsonContent.Create(
                new
                {
                    quantity = 75m
                }
            );
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain(
            "secret-storage-details",
            await response.Content.ReadAsStringAsync()
        );
        await using var verify = new NutrivoDbContext(options);
        Assert.Equal(
            50m,
            (await verify.DiaryEntries.SingleAsync(x => x.Id == entry.Id)).Quantity
        );
        Assert.Equal(3, await verify.DiaryEntries.CountAsync());
    }

    [Fact]
    public async Task OpenApi_DocumentsEntryOperationsAndAmounts()
    {
        var (options, _, _, _) = await Seed();
        await using var factory = new FoodApiFactory(options);
        using var client = Client(factory);
        var json = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var path = json.GetProperty("paths").GetProperty("/diary-entries/{id}");
        foreach (var operation in new[]
                 {
                     "get", "put", "delete"
                 })
        {
            var responses = path.GetProperty(operation).GetProperty("responses");
            foreach (var code in new[]
                     {
                         operation == "get" ? "200" : "204", "400", "404", "500"
                     })
                Assert.True(responses.TryGetProperty(code, out _));
        }
    }
}
