# ADR 0002: PostgreSQL and Food persistence

- Status: Accepted
- Date: 2026-10-24

## Context

The existing Food aggregate must be persisted without introducing EF Core into
the Domain or changing the nutritional rules recorded in ADR 0001. Local
development needs a reproducible database and explicit schema evolution.

## Decision

Use PostgreSQL 18 in Docker Compose and EF Core 10 with the Npgsql 10 provider.
The API remains local. Compose binds the database to localhost and persists data
in a named volume at `/var/lib/postgresql`. Credentials are configured through an
ignored `.env`; the API uses User Secrets or environment variables separately.

Infrastructure owns `NutrivoDbContext`, fluent mappings and migrations. The API
registers the context through `AddInfrastructure`. EF tooling uses the API startup
configuration and a repository-local tool manifest. Migrations are committed and
applied explicitly with `dotnet ef database update`, never automatically at startup.

Map Food to `public.foods`. Its domain-generated FoodId converts to a UUID without
database value generation. Names and optional brands use `varchar(200)`; the
reference unit stores the existing enum's integer values. The constant reference
quantity of 100 is not stored.

Map NutritionalValues as a required EF complex type with four required columns
in the Food table. Unlike an owned entity, a complex type has value semantics
without a hidden identity and permits sharing an immutable instance across foods.
A separate persistence model would avoid any materialization constructor in the
Domain but add a duplicate model and mapping code without a current use case.

Nutrient columns use unconstrained PostgreSQL `numeric`, not `numeric(p,s)`.
The domain accepts all nonnegative .NET decimal values without rounding or an
upper bound below `decimal.MaxValue`; a fixed scale or precision would silently
round or reject valid values. PostgreSQL numeric supports decimal persistence
without imposing such a restriction.

Food gains a private scalar constructor for materialization. EF sets the required
complex value through its backing field after constructing Food. Public creation
still uses the validating factory; get-only properties and framework independence
are preserved. NutritionalValues uses its existing private scalar constructor.

## Consequences

Application and Domain have no EF dependencies. There is no repository interface
or application persistence abstraction until an actual use case needs one.
Integration tests use isolated PostgreSQL Testcontainers, execute the committed
migrations and verify decimal round-trips and the migration lifecycle.

The database enforces column types, lengths, nullability and identity uniqueness.
Domain factories remain authoritative for business validation. Direct SQL writes
are not an alternate supported path for creating domain data. No seed data, Food
endpoints or automatic migrations are added in this slice.
