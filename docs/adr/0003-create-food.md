# ADR 0003: CreateFood command and HTTP endpoint

- Status: Accepted
- Date: 2026-10-24

## Context

Food and its EF Core mappings already exist. The first application use case must
create a validated Food and persist it through a thin HTTP endpoint without
introducing EF Core into Application or Domain.

## Decision

Application owns CreateFoodCommand, CreateFoodCommandHandler and IFoodRepository.
The handler first invokes NutritionalValues.Create, then Food.Create, returning
the first DomainError unchanged through Result<FoodId>. Only valid Foods reach
the repository. Nutrition retains the existing fixed reference basis and exact
decimal values.

IFoodRepository.AddAsync explicitly commits the Food before returning. Its scoped
Infrastructure implementation adds the aggregate to NutrivoDbContext and awaits
SaveChangesAsync. CancellationToken flows from HTTP through the handler to EF.
This single-aggregate use case needs no separate unit of work or generic
repository. A future use case that requires several writes in one transaction
must revisit this persistence contract.

POST /foods maps its own request DTO to the command and calls the scoped handler
directly. CQRS is expressed through explicit commands and handlers; a mediator or
dispatch framework would add dependencies without a current need. The response
contains only the generated UUID, never the domain aggregate. It returns 201 only
after the commit and omits Location until a corresponding GET endpoint exists.

Required JSON properties distinguish missing nutrients from known zero values.
Domain failures produce 400 Problem Details with a stable code and descriptive
detail. Request-binding failures also produce 400 Problem Details in all
environments. Unexpected storage failures remain exceptions and produce generic
500 Problem Details without exposing internal information.

## Consequences

Application and Domain remain free of persistence and web-framework dependencies.
No schema change, automatic migration, authentication or duplicate detection is
introduced. Application tests use a small recording repository; HTTP integration
tests use WebApplicationFactory and the existing PostgreSQL Testcontainers
fixture to verify real persistence in independent databases.
