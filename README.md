# Nutrivo

A calorie and nutrition tracking app built with .NET 10, DDD and Clean Architecture.
The current backend includes the Food domain, PostgreSQL persistence and a
CreateFood command exposed through `POST /foods`.

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
    "referenceUnit": 1,
    "kilocalories": 372,
    "proteinGrams": 13.5,
    "carbohydratesGrams": 58.7,
    "fatGrams": 7
  }'
```

The response is `201 Created` with `{ "id": "<uuid>" }` after the food has been
committed to PostgreSQL. No Location header is returned because a Food GET endpoint
is not available yet.

`referenceUnit` is `1` for grams or `2` for milliliters. All four nutritional values
refer to exactly 100 grams or 100 milliliters, respectively. Every field except
`brand` must be present. Nutrient values must be nonnegative numbers; explicit
zeros are accepted, while missing or null nutrient values are rejected. Names and
brands are trimmed, and an empty brand becomes null. The existing domain rules
govern validation; foods with identical data may be created independently.

Invalid inputs return `400 Bad Request` as `application/problem+json`. Domain
validation responses include a stable `code` and a human-readable `detail`, for
example `Food.Name.Required`. Malformed or incomplete JSON also returns Problem
Details. Technical storage failures return `500` without internal error details.
The endpoint does not require authentication.

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

To roll back all migrations on a disposable database:

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
Domain tests continue to run without a database.

## Architecture decisions

- [Food aggregate and reference basis](docs/adr/0001-food-aggregate.md)
- [PostgreSQL and Food persistence](docs/adr/0002-postgresql-food-persistence.md)
- [CreateFood command and HTTP endpoint](docs/adr/0003-create-food.md)
