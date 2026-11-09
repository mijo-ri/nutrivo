# Development guide

For first-time installation and Rider debugging, follow the [root README](../README.md).
Run all commands below from the repository root.

## Local configuration and database lifecycle

Docker Compose runs PostgreSQL 18.6, exposes it only on `127.0.0.1` and waits for
its healthcheck. Data persists in the `postgres_data` named volume, mounted at
`/var/lib/postgresql` as required by the PostgreSQL 18 image.

Compose reads `.env`; ASP.NET Core does not. The API reads its connection string
from User Secrets in Development, or from `ConnectionStrings__Nutrivo` in the
environment. The environment variable overrides User Secrets. User Secrets live
outside the repository; they are a development convenience, not an encrypted vault.

PostgreSQL initialization variables are applied only when its data directory is
empty. Changing `.env` does not change credentials or databases in an existing
volume, and does not update the API connection string.

```sh
docker compose down
docker compose up -d --wait
```

Stopping and restarting preserves data. `docker compose down --volumes` deletes
the local database data; use it only for an intentional reset and apply migrations
again afterwards.

The local tool manifest pins `dotnet-ef` to the EF Core package version. Migrations
and the model snapshot live in Infrastructure. EF uses the API as its startup
project and reads the same connection configuration as the application. A missing
connection string produces an explicit configuration error. Migrations are never
applied automatically at API startup.

## Working with migrations

After changing the model or its mappings, generate and review a migration:

```sh
dotnet ef migrations add DescribeChange \
  --project src/Nutrivo.Infrastructure \
  --startup-project src/Nutrivo.Api \
  --output-dir Persistence/Migrations \
  -- --environment Development
```

Apply it with the `database update` command below:

```sh
dotnet ef database update \
  --project src/Nutrivo.Infrastructure \
  --startup-project src/Nutrivo.Api \
  -- --environment Development
```

Commit the generated migration, designer and model snapshot together. Check that
mappings match the snapshot:

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

The `DiaryEntryCaptureTime` migration is included in the `database update` above.
PostgreSQL generates `created_at` on INSERT; this is
internal metadata and is not returned by the API. All existing entries receive
the migration timestamp, so their original capture order cannot be reconstructed;
within a meal they are ordered by ID. Rollback removes only this metadata and
preserves diary content, but discards the recorded capture order.

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

Daily diary tests cover exact date selection, duplicate bookings, all meal groups,
capture-time ordering with ID ties, direct and serving inputs, historical snapshots
after catalogue changes/deletion, output rounding, cancellation and read-only
projections. Totals tests cover daily and meal aggregation, empty meals and days,
mixed reference units, rounding only after summation, maximum representable totals
and nutrient overflow with a stable 422 error. Migration tests cover legacy
timestamp backfill, generated timestamps, rollback and reapplication without
changing diary content.

## Architecture decisions

- [Food aggregate and reference basis](adr/0001-food-aggregate.md)
- [PostgreSQL and Food persistence](adr/0002-postgresql-food-persistence.md)
- [CreateFood command and HTTP endpoint](adr/0003-create-food.md)
- [Food search and read access](adr/0004-food-search.md)
- [Daily diary read access and capture order](adr/0008-daily-diary-query.md)
- [Daily and meal nutrition totals](adr/0009-diary-nutrition-totals.md)
- [Food updates and stable serving identities](adr/0010-update-food.md)
