# Nutrivo

A calorie and nutrition tracking app built with .NET 10, DDD and Clean Architecture.
The current backend includes the Food domain, PostgreSQL persistence and a
CreateFood command exposed through `POST /foods` and a paginated Food search at
`GET /foods`.

## Local development

Prerequisites: .NET 10 SDK and Docker with Docker Compose. Run the following
commands from the repository root. The API runs locally; PostgreSQL runs in Docker.

### 1. Configure PostgreSQL

```sh
cp .env.example .env
```

Edit `.env` and replace `POSTGRES_PASSWORD` with a local password. The default
database and username are `nutrivo`. Change `POSTGRES_PORT` if port 5432 is already
in use. `.env` is ignored by Git; never commit real credentials.

```sh
docker compose up -d --wait
```

Compose runs PostgreSQL 18.6, waits for its healthcheck and exposes it only on
`127.0.0.1`. Database files persist in the `postgres_data` named volume, mounted
at `/var/lib/postgresql` as required by the PostgreSQL 18 image.

### 2. Configure the local API connection

Set the connection string with the same database, username, password and host port
as `.env`:

```sh
dotnet user-secrets set 'ConnectionStrings:Nutrivo' \
  'Host=localhost;Port=5432;Database=nutrivo;Username=nutrivo;Password=YOUR_LOCAL_PASSWORD' \
  --project src/Nutrivo.Api
```

User Secrets are stored outside the repository and loaded in the `Development`
environment. They are a local development convenience, not an encrypted vault.
Alternatively, provide the `ConnectionStrings__Nutrivo` environment variable.
It overrides the corresponding configuration value, including User Secrets.

Docker Compose loads `.env`; **ASP.NET Core does not automatically read `.env`**.
Updating `.env` alone does not update the API connection string. PostgreSQL's
initialization variables are applied only when its data directory is empty;
changing them does not change credentials or databases in an existing volume.

### 3. Restore dependencies and apply migrations

```sh
dotnet restore
dotnet tool restore
dotnet ef database update \
  --project src/Nutrivo.Infrastructure \
  --startup-project src/Nutrivo.Api \
  -- --environment Development
```

The local tool manifest pins `dotnet-ef` to the EF Core package version. Migrations
and the model snapshot live in Infrastructure. EF uses the API as its startup
project and reads the same connection configuration as the application. A missing
connection string produces an explicit configuration error.

### 4. Start the API

```sh
dotnet run --project src/Nutrivo.Api
```

The launch profile selects `Development`; the console shows the API URL. Open
[Swagger UI](http://localhost:5097/swagger) manually in your browser to explore
the endpoints and execute requests with **Try it out**. With the `https` launch
profile, use [Swagger UI over HTTPS](https://localhost:7179/swagger).

Swagger UI and the OpenAPI document at `/openapi/v1.json` are available only in
`Development`; both are unavailable in other environments. Starting the API does
not apply migrations or create the database.

### 5. Create a food

With the API running under the default HTTP launch profile, send:

```sh
curl --fail-with-body http://localhost:5097/foods \
  --header 'Content-Type: application/json' \
  --data '{
    "name": "Rolled oats",
    "brand": "Example",
    "referenceQuantity": 100,
    "referenceUnit": 1,
    "kilocalories": 372,
    "proteinGrams": 13.5,
    "carbohydratesGrams": 58.7,
    "fatGrams": 7
  }'
```

The response is `201 Created` with `{ "id": "<uuid>" }` after the food has been
committed to PostgreSQL. No Location header is returned because a Food detail
endpoint is not available yet.

`referenceUnit` is `1` for grams or `2` for milliliters. `referenceQuantity` is
required and may be any positive decimal. The four nutritional values refer to
that entire quantity. All fields except `brand` and `servingSizes` must be present.
Nutrient values must be nonnegative numbers; explicit zeros are accepted, while
missing or null nutrient values are rejected. Names and brands are trimmed, and
an empty brand becomes null. Foods with identical data may be created independently.

A food can include multiple freely named serving sizes calculated from its common
nutrition basis. For example, create an egg with nutrition per 100 g and two sizes
(the nutrition and weights below are illustrative user inputs):

```json
{
  "name": "Ei",
  "referenceQuantity": 100,
  "referenceUnit": 1,
  "kilocalories": 150,
  "proteinGrams": 12,
  "carbohydratesGrams": 1,
  "fatGrams": 10,
  "servingSizes": [
    { "quantity": 1, "unit": "Stück", "label": "Klein", "equivalentQuantity": 45 },
    { "quantity": 2, "unit": "Stück", "label": "Mittel", "equivalentQuantity": 116 }
  ]
}
```

Each serving requires positive `quantity` and `equivalentQuantity` numbers and a
nonblank free-text `unit`. `label` is optional. Unit and label are trimmed and
limited to 200 characters; a blank label becomes null. `equivalentQuantity` uses
the food's reference unit and describes the **entire** serving quantity: the two
medium eggs above weigh 116 g together. The API does not infer weights or convert
grams to milliliters. Units such as `Scheibe` and `Esslöffel` are also free text.
Omitting `servingSizes` (or supplying null or an empty array) creates no servings;
null entries inside the array are rejected.

Calculated nutrition is `basis nutrient × equivalentQuantity / referenceQuantity`.
The two medium eggs above therefore contain 174 kcal, 13.92 g protein, 1.16 g
carbohydrates and 11.6 g fat together. Basis values and all input quantities are
stored exactly. Calculations use decimal arithmetic without display rounding;
serving nutrients in HTTP responses are rounded to two decimal places with
midpoint ties away from zero. A serving whose calculated nutrition exceeds the
decimal range is rejected during creation with
`Food.ServingSize.Nutrition.OutOfRange`; no part of the food is saved.

Invalid inputs return `400 Bad Request` as `application/problem+json`. Domain
validation responses include a stable `code` and a human-readable `detail`, for
example `Food.Name.Required`. Malformed or incomplete JSON also returns Problem
Details. Technical storage failures return `500` without internal error details.
The endpoint does not require authentication.

### 6. Search foods

```sh
curl --fail-with-body --get http://localhost:5097/foods \
  --data-urlencode 'search=oats' \
  --data-urlencode 'page=1' \
  --data-urlencode 'pageSize=20'
```

`GET /foods` searches the stored name or optional brand for a case-insensitive,
literal substring. Leading and trailing whitespace is trimmed. A missing, empty
or whitespace-only `search` lists all foods. Single-character searches are valid;
multiple words remain one contiguous search text. `%`, `_` and backslashes are
ordinary characters. There is no fuzzy matching, accent transliteration or
external lookup.

Results are sorted case-insensitively by name, then brand (null treated as empty),
using the database collation, then by ID. `page` defaults to 1 and must be at least
1. `pageSize` defaults to 20 and must be between 1 and 100. The computed offset
must fit in a signed 32-bit integer. Ordering is stable while the data is unchanged;
the count and page are separate database reads without a shared snapshot.

The `200 OK` response contains `items`, `page`, `pageSize` and `totalCount` (the
number of all matching foods, before pagination). Each item includes `id`, `name`,
`brand`, `referenceQuantity`, `referenceUnit`, `kilocalories`, `proteinGrams`,
`carbohydratesGrams`, `fatGrams` and `servingSizes`. Basis nutrition retains its
exact decimal values and refers to `referenceQuantity` grams (`referenceUnit: 1`)
or milliliters (`referenceUnit: 2`). Each serving includes its generated `id`,
`quantity`, `unit`, `label`, `equivalentQuantity` and the same four nutrient fields
calculated for that serving, rounded as described above. Servings retain input
order. Foods without servings return `servingSizes: []`. Portions do not change
food pagination or name/brand matching.
An empty database, no matches or a page beyond the results returns an empty
`items` array. A page beyond the results still reports the matching `totalCount`.

Invalid page values or offset overflow return `400 Problem Details` with code
`SearchFoods.Page.Invalid`; invalid page sizes use `SearchFoods.PageSize.Invalid`.
Malformed numeric parameters also return `400 Problem Details` through request
binding. Technical storage failures return a generic `500` without internal
details. Search requires no authentication and does not change stored foods.

### Stop PostgreSQL

```sh
docker compose down
```

This preserves database data. To restart, run `docker compose up -d --wait` again.
`docker compose down --volumes` deletes the local database data and should only be
used when intentionally resetting it; apply migrations again afterwards.

## Working with migrations

After changing the model or its mappings, generate and review a migration:

```sh
dotnet ef migrations add DescribeChange \
  --project src/Nutrivo.Infrastructure \
  --startup-project src/Nutrivo.Api \
  --output-dir Persistence/Migrations \
  -- --environment Development
```

Apply it with the `database update` command above. Commit the generated migration,
designer and model snapshot together. Check that mappings match the snapshot:

```sh
dotnet ef migrations has-pending-model-changes \
  --project src/Nutrivo.Infrastructure \
  --startup-project src/Nutrivo.Api \
  -- --environment Development
```

`FoodServingSizes` upgrades existing foods with `referenceQuantity = 100` and no
servings, preserving IDs, units and exact nutrition. The API now requires callers
to supply `referenceQuantity`; old request bodies must be updated. Migrations are
still applied explicitly, never automatically at API startup.

A rollback of `FoodServingSizes` is rejected if any servings or reference
quantities other than 100 exist, preventing data loss or changed interpretation
of nutrition. Unchanged legacy foods can safely return to the previous schema.

To roll back all migrations on a compatible disposable database:

```sh
dotnet ef database update 0 \
  --project src/Nutrivo.Infrastructure \
  --startup-project src/Nutrivo.Api \
  -- --environment Development
```

Rolling back `InitialCreate` drops `foods` and its contents. This command is for
disposable local databases, not a data-preserving rollback strategy.

## Formatting

Use the pinned local JetBrains CleanupCode tool to apply the repository's
`.editorconfig` rules, including the 90-character line limit and one argument or
parameter per line for long lists. Run from the repository root:

```sh
dotnet tool restore
dotnet jb cleanupcode "tests/Nutrivo.Application.Tests/Foods/CreateFoodCommandHandlerTests.cs"
```

Pass explicit paths to only the C# files created or modified for the task; multiple
files can be passed in one invocation. File mode applies formatting only and does
not require a solution argument or cleanup profile. Review the diff afterwards,
then run the build and tests required for the change. Rider does not need to be
running. The tool version is pinned in the local manifest with runtime roll-forward
enabled so it can run with the project's .NET 10 installation.

## Validation

```sh
dotnet build
dotnet test
```

Integration tests require a running Docker engine. Testcontainers starts its own
PostgreSQL 18.6 container on a dynamically assigned port, creates a separate
database for each test and removes the container afterwards. Tests do not use
`.env`, API User Secrets or the Compose development database. They apply real EF
migrations rather than `EnsureCreated` or an in-memory database.

Tests cover Food materialization, typed IDs, both reference units, optional brand,
shared immutable nutrition, exact decimal persistence and migration apply,
reapply, rollback and rebuild. CreateFood application tests cover domain failures,
normalization, cancellation and persistence failures without a database. HTTP
integration tests exercise the real application and PostgreSQL, including stored
values, required JSON fields, Problem Details responses and OpenAPI metadata.
Domain tests continue to run without a database. Additional tests cover arbitrary
reference amounts, freely named portions, whole-serving calculations, rounding,
overflows, stable serving order and IDs, exact quantity persistence, migration of
legacy data and rejection of unsafe rollbacks.

Search tests cover normalization, pagination validation, overflow, cancellation
and technical failures. PostgreSQL/API tests exercise literal name/brand matching,
special characters, deterministic pagination, exact nutrition responses, foods
created through POST, read-only projections, Problem Details and OpenAPI defaults.

## Architecture decisions

- [Food aggregate and reference basis](docs/adr/0001-food-aggregate.md)
- [PostgreSQL and Food persistence](docs/adr/0002-postgresql-food-persistence.md)
- [CreateFood command and HTTP endpoint](docs/adr/0003-create-food.md)
- [Food search and read access](docs/adr/0004-food-search.md)
