using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nutrivo.Api.Endpoints.Foods.SearchFoods;
using Nutrivo.Api.Endpoints.Foods.UpdateFood;
using Nutrivo.Application.Foods;
using Nutrivo.Domain.Diaries;
using Nutrivo.Domain.Foods;
using Nutrivo.Infrastructure.Persistence;
using Nutrivo.IntegrationTests.Persistence;

namespace Nutrivo.IntegrationTests.Foods;

[Collection("PostgreSQL")]
public sealed class UpdateFoodEndpointTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task
        Put_ReordersAddsRemovesAndRepeatedlyUpdatesWithStableIdsAndExactDecimals()
    {
        var (options, original) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var request = Request() with
        {
            ServingSizes =
            [
                Serving(original.ServingSizes[2].Id.Value, 10m),
                Serving(null, 12.123456789012345678901234567m),
                Serving(original.ServingSizes[0].Id.Value, 20m)
            ]
        };
        using var response = await client.PutAsJsonAsync(
            $"/foods/{original.Id.Value}",
            request
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(original.Id.Value, body.GetProperty("id").GetGuid());
        Assert.Single(body.EnumerateObject());

        var updated = await ReadAsync(options, original.Id);
        Assert.Equal("Updated", updated.Name);
        Assert.Null(updated.Brand);
        Assert.Equal(ReferenceUnit.Milliliter, updated.ReferenceUnit);
        Assert.Equal(request.ReferenceQuantity, updated.ReferenceQuantity);
        Assert.Equal(request.Kilocalories, updated.NutritionalValues.Kilocalories);
        Assert.Equal(request.ProteinGrams, updated.NutritionalValues.ProteinGrams);
        Assert.Equal(
            request.CarbohydratesGrams,
            updated.NutritionalValues.CarbohydratesGrams
        );
        Assert.Equal(request.FatGrams, updated.NutritionalValues.FatGrams);
        Assert.Equal(original.ServingSizes[2].Id, updated.ServingSizes[0].Id);
        Assert.Equal(original.ServingSizes[0].Id, updated.ServingSizes[2].Id);
        Assert.DoesNotContain(
            updated.ServingSizes,
            size => size.Id == original.ServingSizes[1].Id
        );
        Assert.Equal(
            request.ServingSizes[1]!.EquivalentQuantity,
            updated.ServingSizes[1].EquivalentQuantity
        );
        Assert.Equal(
            [
                0, 1, 2
            ],
            updated.ServingSizes.Select(size => size.Position)
        );

        // Swap occupied positions, then remove a middle serving and append a new one.
        request = request with
        {
            ServingSizes =
            [
                Serving(updated.ServingSizes[2].Id.Value, 30m),
                Serving(updated.ServingSizes[1].Id.Value, 40m),
                Serving(updated.ServingSizes[0].Id.Value, 50m)
            ]
        };
        using var swapped = await client.PutAsJsonAsync(
            $"/foods/{original.Id.Value}",
            request
        );
        Assert.Equal(HttpStatusCode.OK, swapped.StatusCode);
        var swappedFood = await ReadAsync(options, original.Id);
        Assert.Equal(
            request.ServingSizes.Select(size => size!.Id),
            swappedFood.ServingSizes.Select(size => (Guid?)size.Id.Value)
        );
        request = request with
        {
            ServingSizes =
            [request.ServingSizes[0], request.ServingSizes[2], Serving(null, 15m)]
        };
        using var removed = await client.PutAsJsonAsync(
            $"/foods/{original.Id.Value}",
            request
        );
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        var final = await ReadAsync(options, original.Id);
        request = request with
        {
            ServingSizes =
            [
                .. final
                    .ServingSizes.Select(size => Serving(
                            size.Id.Value,
                            size.EquivalentQuantity
                        )
                    )
            ]
        };
        using var repeated = await client.PutAsJsonAsync(
            $"/foods/{original.Id.Value}",
            request
        );
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(State(final), State(await ReadAsync(options, original.Id)));

        var detail =
            (await client.GetFromJsonAsync<SearchFoodResponse>(
                $"/foods/{original.Id.Value}"
            ))!;
        var search =
            (await client.GetFromJsonAsync<SearchFoodsResponse>(
                "/foods?search=Updated"
            ))!;
        Assert.Equal(
            JsonSerializer.Serialize(detail),
            JsonSerializer.Serialize(Assert.Single(search.Items))
        );
        Assert.Equal(
            final.ServingSizes.Select(size => size.Id.Value),
            detail.ServingSizes.Select(size => size.Id)
        );
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("empty")]
    public async Task Put_WithoutServings_RemovesAllServings(string mode)
    {
        var (options, food) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var json = JsonSerializer.SerializeToNode(
            Request(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        )!;
        if (mode == "missing")
        {
            json.AsObject().Remove("servingSizes");
        }

        if (mode == "empty")
        {
            json["servingSizes"] = new System.Text.Json.Nodes.JsonArray();
        }

        using var response = await client.PutAsync(
            $"/foods/{food.Id.Value}",
            new StringContent(json.ToJsonString(), Encoding.UTF8, "application/json")
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await ReadAsync(options, food.Id)).ServingSizes);
        await using var context = new NutrivoDbContext(options);
        Assert.Equal(0, await context.Set<ServingSize>().CountAsync());
    }

    [Theory]
    [InlineData("name", "Food.Name.Required")]
    [InlineData("negative", "NutritionalValues.Kilocalories.Negative")]
    [InlineData("nullServing", "Food.ServingSize.Required")]
    [InlineData("emptyId", "ServingSize.Id.Empty")]
    [InlineData("foreignId", "Food.ServingSize.NotFound")]
    [InlineData("duplicate", "Food.ServingSize.Id.Duplicate")]
    [InlineData("overflow", "Food.ServingSize.Nutrition.OutOfRange")]
    public async Task
        Put_WithInvalidInput_ReturnsStableProblemAndPreservesStoredAggregate(
            string scenario,
            string code
        )
    {
        var (options, food) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var serving = Serving(food.ServingSizes[0].Id.Value, 50m);
        var request = scenario switch
        {
            "name" => Request() with
            {
                Name = " "
            },
            "negative" => Request() with
            {
                Kilocalories = -1m
            },
            "nullServing" => Request() with
            {
                ServingSizes = [null]
            },
            "emptyId" => Request() with
            {
                ServingSizes =
                [
                    serving with
                    {
                        Id = Guid.Empty
                    }
                ]
            },
            "foreignId" => Request() with
            {
                ServingSizes =
                [
                    serving with
                    {
                        Id = Guid.NewGuid()
                    }
                ]
            },
            "duplicate" => Request() with
            {
                ServingSizes = [serving, serving]
            },
            _ => Request() with
            {
                ReferenceQuantity = 1m,
                Kilocalories = decimal.MaxValue,
                ServingSizes = [serving]
            }
        };
        using var response =
            await client.PutAsJsonAsync($"/foods/{food.Id.Value}", request);
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, code);
        Assert.Equal(State(food), State(await ReadAsync(options, food.Id)));
    }

    [Theory]
    [InlineData("not-a-guid", 400, null)]
    [InlineData("00000000-0000-0000-0000-000000000000", 400, "Food.Id.Empty")]
    [InlineData("11111111-1111-1111-1111-111111111111", 404, "UpdateFood.NotFound")]
    public async Task Put_WithInvalidOrMissingFoodId_ReturnsProblem(
        string id,
        int status,
        string? code
    )
    {
        var (options, _) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync($"/foods/{id}", Request());
        await AssertProblemAsync(response, (HttpStatusCode)status, code);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("referenceQuantity")]
    [InlineData("referenceUnit")]
    [InlineData("kilocalories")]
    [InlineData("proteinGrams")]
    [InlineData("carbohydratesGrams")]
    [InlineData("fatGrams")]
    public async Task Put_WithMissingRequiredField_IsRejectedBeforeWriting(string field)
    {
        var (options, food) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var json = JsonSerializer.SerializeToNode(
            Request(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        )!.AsObject();
        json.Remove(field);
        using var response = await client.PutAsync(
            $"/foods/{food.Id.Value}",
            new StringContent(json.ToJsonString(), Encoding.UTF8, "application/json")
        );
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, null);
        Assert.Equal(State(food), State(await ReadAsync(options, food.Id)));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task
        Put_WhenStorageFails_RollsBackFoodAndTemporaryPositionsAndHidesDetails(
            string environment
        )
    {
        var (options, food) = await SeedAsync();
        await using (var context = new NutrivoDbContext(options))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
                CREATE FUNCTION reject_food_update() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'secret-storage-details'; END; $$;
                CREATE TRIGGER reject_food_update BEFORE UPDATE ON public.foods
                FOR EACH ROW EXECUTE FUNCTION reject_food_update();
                """
            );
        }

        await using var factory = new FoodApiFactory(options, environment);
        using var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            }
        );
        using var response = await client.PutAsJsonAsync(
            $"/foods/{food.Id.Value}",
            Request() with
            {
                ServingSizes = [Serving(food.ServingSizes[2].Id.Value, 999m)]
            }
        );
        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, null);
        Assert.DoesNotContain(
            "secret-storage-details",
            await response.Content.ReadAsStringAsync()
        );
        Assert.Equal(State(food), State(await ReadAsync(options, food.Id)));
    }

    [Fact]
    public async Task Put_PreservesDiarySnapshotsAndNewEntriesUseUpdatedFood()
    {
        var (options, food) = await SeedAsync();
        var date = new DateOnly(2026, 11, 8);
        var direct = DiaryEntry.Create(
                food,
                date,
                MealType.Breakfast,
                25m,
                ReferenceUnit.Gram
            )
            .Value;
        var serving = DiaryEntry.CreateFromServing(
                food,
                date,
                MealType.Lunch,
                food.ServingSizes[0].Id,
                2m
            )
            .Value;
        await using (var context = new NutrivoDbContext(options))
        {
            context.DiaryEntries.AddRange(direct, serving);
            await context.SaveChangesAsync();
        }

        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        using var response = await client.PutAsJsonAsync(
            $"/foods/{food.Id.Value}",
            Request() with
            {
                ServingSizes = [Serving(food.ServingSizes[0].Id.Value, 999m)]
            }
        );
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var readContext = new NutrivoDbContext(options);
        var entries = await readContext.DiaryEntries.AsNoTracking().ToListAsync();
        foreach (var expected in new[]
                 {
                     direct, serving
                 })
        {
            var actual = entries.Single(entry => entry.Id == expected.Id);
            Assert.Equal(expected.FoodSnapshot, actual.FoodSnapshot);
            Assert.Equal(expected.ServingSizeSnapshot, actual.ServingSizeSnapshot);
            Assert.Equal(expected.NutritionalValues, actual.NutritionalValues);
        }

        using var logged = await client.PostAsJsonAsync(
            "/diary-entries",
            new
            {
                foodId = food.Id.Value,
                date,
                mealType = MealType.Dinner,
                servingSizeId = food.ServingSizes[0].Id.Value,
                servingCount = 1m
            }
        );
        Assert.Equal(HttpStatusCode.Created, logged.StatusCode);
        var id = (await logged.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id")
            .GetGuid();
        var newEntry = await readContext
            .DiaryEntries.AsNoTracking()
            .SingleAsync(entry => entry.Id == DiaryEntryId.Create(id).Value);
        Assert.Equal("Updated", newEntry.FoodSnapshot.Name);
        Assert.Equal(ReferenceUnit.Milliliter, newEntry.ReferenceUnit);
        Assert.Equal(999m, newEntry.ServingSizeSnapshot!.EquivalentQuantity);
    }

    [Fact]
    public async Task Repository_LoadsTrackedGraphAndSupportsCancellation()
    {
        var (options, food) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var context = scope.ServiceProvider.GetRequiredService<NutrivoDbContext>();
        var loaded = await repository.GetForUpdateAsync(food.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(4, context.ChangeTracker.Entries().Count());
        Assert.Null(
            await repository.GetForUpdateAsync(FoodId.New(), CancellationToken.None)
        );
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.SaveAsync(loaded, cancellation.Token)
        );
        Assert.Equal(State(food), State(await ReadAsync(options, food.Id)));
    }

    [Fact]
    public async Task Repository_SerializesWritersAndLoadsServingsAfterPreviousCommit()
    {
        var (options, food) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var firstScope = factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var loaded = (await first.GetForUpdateAsync(food.Id, CancellationToken.None))!;

        using (var blockedScope = factory.Services.CreateScope())
        {
            var blocked =
                blockedScope.ServiceProvider.GetRequiredService<IFoodRepository>();
            using var cancellation =
                new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                blocked.GetForUpdateAsync(food.Id, cancellation.Token)
            );
        }

        Assert.True(
            loaded.Update(
                    "First write",
                    null,
                    100m,
                    ReferenceUnit.Gram,
                    loaded.NutritionalValues,
                    [new(null, 1m, "New serving", null, 50m)]
                )
                .IsSuccess
        );
        await first.SaveAsync(loaded, CancellationToken.None);

        using var secondScope = factory.Services.CreateScope();
        var second = secondScope.ServiceProvider.GetRequiredService<IFoodRepository>();
        var latest = (await second.GetForUpdateAsync(food.Id, CancellationToken.None))!;
        Assert.Equal("First write", latest.Name);
        Assert.Equal(loaded.ServingSizes[0].Id, Assert.Single(latest.ServingSizes).Id);
        Assert.True(
            latest.Update(
                    "Last write",
                    null,
                    100m,
                    ReferenceUnit.Gram,
                    latest.NutritionalValues
                )
                .IsSuccess
        );
        await second.SaveAsync(latest, CancellationToken.None);
        var stored = await ReadAsync(options, food.Id);
        Assert.Equal("Last write", stored.Name);
        Assert.Empty(stored.ServingSizes);
    }

    [Fact]
    public async Task OpenApi_DescribesUpdateEndpointAndResponses()
    {
        var (options, _) = await SeedAsync();
        await using var factory = new FoodApiFactory(options);
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var operation = document
            .GetProperty("paths")
            .GetProperty("/foods/{id}")
            .GetProperty("put");
        Assert.Equal("UpdateFood", operation.GetProperty("operationId").GetString());
        Assert.True(
            operation.GetProperty("parameters")[0].GetProperty("required").GetBoolean()
        );
        foreach (var status in new[]
                 {
                     "200", "400", "404", "500"
                 })
        {
            Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
        }
    }

    private async Task<(DbContextOptions<NutrivoDbContext>, Food)> SeedAsync()
    {
        var options = await fixture.CreateDatabaseAsync();
        await using var context = new NutrivoDbContext(options);
        await context.Database.MigrateAsync();
        var food = Food.Create(
                "Original",
                "Brand",
                100m,
                ReferenceUnit.Gram,
                NutritionalValues.Create(100m, 1m, 2m, 3m).Value,
                [
                    ServingSize.Create(1m, "First", null, 10m).Value,
                    ServingSize.Create(1m, "Second", null, 20m).Value,
                    ServingSize.Create(1m, "Third", null, 30m).Value
                ]
            )
            .Value;
        context.Foods.Add(food);
        await context.SaveChangesAsync();
        return (options, food);
    }

    private static UpdateFoodRequest Request()
    {
        return new UpdateFoodRequest
        {
            Name = " Updated ",
            Brand = " ",
            ReferenceQuantity = 50.123456789012345678901234567m,
            ReferenceUnit = ReferenceUnit.Milliliter,
            Kilocalories = 123.12345678901234567890123456m,
            ProteinGrams = 1.2345678901234567890123456789m,
            CarbohydratesGrams = 5m,
            FatGrams = 6m
        };
    }

    private static UpdateServingSizeRequest Serving(Guid? id, decimal equivalent)
    {
        return new UpdateServingSizeRequest
        {
            Id = id,
            Quantity = 2.123456789012345678901234567m,
            Unit = " Cup ",
            Label = " Large ",
            EquivalentQuantity = equivalent
        };
    }

    private static async Task<Food> ReadAsync(
        DbContextOptions<NutrivoDbContext> options,
        FoodId id
    )
    {
        await using var context = new NutrivoDbContext(options);
        return await context.Foods.AsNoTracking().SingleAsync(food => food.Id == id);
    }

    private static string State(Food food)
    {
        return JsonSerializer.Serialize(food);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string? code
    )
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        if (code is not null)
        {
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(code, problem.GetProperty("code").GetString());
        }
    }
}
