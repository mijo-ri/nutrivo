## Project Overview

This repository contains a full-stack calorie and nutrition tracking application.

The main purpose of the project is to practise and deepen skills in:

* .NET 10
* Domain-Driven Design
* Clean Architecture
* CQRS
* Entity Framework Core
* API design
* React Native
* automated testing
* Docker
* GitHub Actions
* cloud deployment
* AI-assisted software development

The application is inspired by products such as MyFitnessPal, but the goal is not to clone an existing product. The focus is on a clean domain model, maintainable architecture, good developer experience, and iterative product development.

## Product Goal

Users should be able to track what they eat with as little friction as possible and understand their daily calorie and macronutrient intake.

The application should eventually support:

* calorie and macronutrient tracking
* daily nutrition summaries
* reusable foods and meals
* barcode-based food lookup
* voice-based food entry
* personal calorie and macro targets
* food history and favourites
* editing and correcting logged food
* external nutrition data providers

Example voice input:

> "100 grams of oats, 4 eggs and 2 tablespoons of olive oil"

The system should convert this input into structured food entries. The user must be able to review and correct the parsed result before it is saved.

## Development Strategy

Build the product incrementally.

Do not implement the complete product at once.

Preferred delivery phases:

### Phase 1 — Core Tracking

Implement the smallest useful version:

* create or select a food
* define nutritional values per reference quantity
* log a consumed amount
* group entries by day
* calculate daily calories and macros
* edit and delete logged entries
* configure daily calorie and macro targets

This phase should work without barcode scanning, voice input, authentication providers, or external nutrition APIs.

### Phase 2 — Food Catalogue

Add:

* food search
* recent foods
* favourites
* reusable meals or recipes
* serving sizes and common units

### Phase 3 — Barcode Tracking

Add:

* barcode scanning in the mobile app
* product lookup through an external nutrition API
* provider abstraction so the external API can be replaced
* local persistence of imported product data where appropriate

### Phase 4 — Voice Tracking

Add:

* speech-to-text on the client or through a provider
* interpretation of natural-language food descriptions
* mapping parsed items to known foods
* confidence/ambiguity handling
* mandatory confirmation before saving uncertain entries

The AI/LLM component must not write directly to the nutrition diary.

### Phase 5 — Production Concerns

Add only when needed:

* user accounts
* synchronisation across devices
* observability
* rate limiting
* caching
* background jobs
* production deployment

## Architecture Principles

Use Clean Architecture with the dependency direction pointing towards the domain.

Recommended solution structure:

```text
src/
  Nutrivo.Api/
  Nutrivo.Application/
  Nutrivo.Domain/
  Nutrivo.Infrastructure/

tests/
  Nutrivo.Domain.Tests/
  Nutrivo.Application.Tests/
  Nutrivo.IntegrationTests/

mobile/
  nutrivo-app/
```

### Domain

The Domain project contains:

* aggregates
* entities
* value objects
* domain services
* domain events
* domain exceptions or domain errors
* business rules

The Domain project must not depend on:

* Entity Framework Core
* ASP.NET Core
* Azure SDKs
* external APIs
* UI frameworks
* infrastructure implementations

Prefer behaviour-rich domain objects over anemic entities containing only getters and setters.

### Application

The Application project coordinates use cases.

It may contain:

* commands
* queries
* handlers
* DTOs
* validation
* application interfaces
* authorization policies
* orchestration logic

Business invariants that belong to the domain must not be moved into handlers merely because handlers are easier to implement.

### Infrastructure

Infrastructure contains technical implementations such as:

* Entity Framework Core
* database repositories
* external nutrition providers
* barcode product providers
* AI/LLM providers
* authentication implementations
* file or blob storage
* telemetry adapters

External systems must be accessed through abstractions owned by the inner layers where appropriate.

### API

The API project is responsible for:

* HTTP transport
* endpoint definitions
* authentication middleware
* request/response mapping
* dependency injection composition
* API-specific error responses

Endpoints should be thin.

Do not place domain logic directly inside controllers or endpoints.

## Domain Modelling Guidelines

Do not create aggregates based on database tables.

Start with business behaviour and invariants.

Potential domain concepts include:

* `Food`
* `NutritionalValues`
* `ServingSize`
* `NutritionDiary`
* `DiaryEntry`
* `DailyNutritionTarget`
* `Meal`
* `Recipe`
* `Ingredient`
* `Quantity`
* `Weight`
* `Energy`
* `Macronutrients`

These are starting points, not mandatory final names.

Continuously refine the model when new domain knowledge is discovered.

### Suggested Aggregate Boundaries

A possible initial model is:

#### Food

Represents a trackable food or product.

Possible responsibilities:

* nutritional values for a reference quantity
* supported serving sizes
* barcode identity
* data source
* user-created versus imported food

#### NutritionDiary

Represents food consumed by one user over time.

Possible responsibilities:

* add an entry
* change consumed quantity
* remove an entry
* associate an entry with a date and optional meal category

Avoid designing one ever-growing aggregate containing an entire user's lifetime diary.

Consider modelling diary data around a day if aggregate size becomes problematic.

#### DailyNutritionTarget

Represents the user's target values for:

* calories
* protein
* carbohydrates
* fat

Targets may later support effective date ranges rather than overwriting historical targets.

## Important Domain Rules

Make units explicit.

Avoid primitive obsession for nutrition-related values where a value object improves correctness.

Examples:

* grams
* millilitres
* kilocalories
* protein grams
* carbohydrate grams
* fat grams

Do not use floating-point arithmetic for values where rounding behaviour matters.

Prefer `decimal` in .NET for nutritional calculations unless there is a documented reason not to.

A food should define nutritional values against a clear reference quantity, for example:

```text
100 g oats
372 kcal
13.5 g protein
58.7 g carbohydrates
7.0 g fat
```

Consumed nutrition must be calculated from the consumed quantity and the food's reference quantity.

The rounding policy must be defined centrally and covered by tests.

Historical diary entries should remain understandable if a food's catalogue information changes later.

Before implementing food updates, explicitly decide whether diary entries:

1. reference the current food values, or
2. store a nutritional snapshot at logging time.

Prefer a snapshot when historical accuracy is required.

## CQRS Guidelines

Use CQRS to make use cases explicit, not to create unnecessary layers.

Commands change state.

Examples:

* `LogFood`
* `UpdateDiaryEntry`
* `RemoveDiaryEntry`
* `CreateFood`
* `SetDailyNutritionTarget`

Queries return information.

Examples:

* `GetDiaryDay`
* `SearchFoods`
* `GetRecentFoods`
* `GetNutritionSummary`

Do not introduce separate databases, event sourcing, or distributed messaging merely because the project uses CQRS.

A single relational database is the default.

## Persistence

Use Entity Framework Core.

Initial database recommendation:

* PostgreSQL for local and hosted environments

Use EF Core configuration outside the domain model.

Prefer explicit entity type configurations.

Database migrations belong to Infrastructure or a dedicated migrations project.

Repositories should be introduced when they provide a meaningful domain/application abstraction.

Do not create generic repositories such as:

```text
IRepository<T>
```

unless a concrete need justifies them.

For read-heavy queries, direct EF Core projections in query handlers are acceptable.

## API Design

Prefer REST-style HTTP APIs for the initial version.

Use resource-oriented URLs and explicit application use cases.

Return stable error contracts.

Use appropriate HTTP status codes.

Do not expose EF entities directly.

Use DTOs at the API boundary.

Generate OpenAPI documentation.

## Mobile App

Use React Native with TypeScript.

Prefer Expo unless a required native capability makes the Expo workflow unsuitable.

The mobile application should contain:

* screens
* navigation
* API client
* client-side state
* form handling
* camera/barcode integration
* speech input integration

Do not duplicate backend business rules in the client.

Simple presentation validation may exist on the client for user experience.

The backend remains authoritative for domain invariants.

## State Management

Start simple.

Use local React state and server-state tooling where sufficient.

Do not add Redux or another global state library unless application complexity creates a concrete need.

If server-state caching is required, TanStack Query is preferred.

## Barcode Integration

Treat barcode lookup as an external capability.

Define an application-level abstraction such as:

```csharp
public interface IProductLookupService
{
    Task<ProductLookupResult?> FindByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken);
}
```

Do not let external provider DTOs leak into the domain.

Potential providers may include Open Food Facts or another nutrition database.

Provider selection should be replaceable.

When data originates from an external provider, preserve source/provenance information where useful.

## Voice Tracking

Voice tracking is an input mechanism, not a separate source of truth.

Conceptual pipeline:

```text
Speech
  -> Speech-to-text
  -> Structured food interpretation
  -> Food matching
  -> User confirmation
  -> LogFood command
```

Represent the interpreted result as structured data.

Example:

```json
{
  "items": [
    {
      "food": "oats",
      "quantity": 100,
      "unit": "g"
    },
    {
      "food": "egg",
      "quantity": 4,
      "unit": "piece"
    },
    {
      "food": "olive oil",
      "quantity": 2,
      "unit": "tablespoon"
    }
  ]
}
```

Never trust LLM-generated calorie or nutrient values when authoritative food data is available.

The AI component should interpret language. Nutritional values should come from the application's food catalogue or a trusted nutrition provider.

Ambiguous matches must be surfaced to the user.

## Testing Strategy

Testing is a core learning goal.

### Domain Tests

Write focused unit tests for:

* nutritional calculations
* quantity conversion
* diary rules
* target calculations
* rounding
* recipe calculations
* domain invariants

Domain tests should not require a database.

### Application Tests

Test command and query behaviour.

Mock only external boundaries where useful.

Avoid over-mocking internal application code.

### Integration Tests

Use integration tests for:

* EF Core mappings
* database queries
* migrations where appropriate
* HTTP API behaviour
* authentication/authorization flows when introduced

Prefer real PostgreSQL through Testcontainers for important persistence tests.

### End-to-End Tests

Add only for a small number of critical user journeys.

Do not rely on E2E tests for logic that can be tested more cheaply at lower levels.

## Code Quality

Use nullable reference types.

Use async APIs for I/O.

Pass `CancellationToken` through asynchronous application and infrastructure operations.

Prefer immutable value objects.

Prefer small explicit types over dictionaries and loosely typed structures.

Avoid speculative abstractions.

Do not introduce patterns without a concrete problem they solve.

Keep methods small and intention-revealing.

Prefer readable code over clever code.

## Error Handling

Expected business failures should be represented explicitly.

Examples:

* food not found
* invalid quantity
* unsupported unit conversion
* diary entry not found
* duplicate barcode where uniqueness is required

Do not use exceptions for ordinary expected control flow.

Unexpected technical failures may use exceptions and should be logged at the application boundary.

## Observability

When production deployment is introduced, support:

* structured logging
* request correlation
* health checks
* application metrics where useful

Do not log sensitive user data or raw voice recordings unless explicitly required.

## Security and Privacy

Treat nutrition data as personal user data.

Collect only data needed by the product.

Do not store raw voice recordings by default.

Use HTTPS in hosted environments.

Store secrets outside source control.

Use environment variables, development secrets, or managed secret stores.

Never commit:

* API keys
* database passwords
* cloud credentials
* signing secrets

## Docker

Provide a local development environment using Docker Compose when useful.

Typical services:

```text
API
PostgreSQL
```

Do not containerize the mobile development environment unless there is a concrete benefit.

The API Dockerfile should use multi-stage builds.

## CI/CD

Use GitHub Actions.

At minimum, CI should:

1. restore dependencies
2. build the backend
3. run backend tests
4. lint/type-check the mobile app
5. run mobile tests
6. build required artifacts

Deployment should only happen after successful validation.

Keep deployment workflows separate from pull-request validation where practical.

## Cloud Deployment

Prefer a setup that is inexpensive and easy to replace.

Azure is appropriate for practising Microsoft cloud skills.

Possible Azure components:

* Azure App Service or Azure Container Apps
* Azure Database for PostgreSQL
* Application Insights

Do not introduce Kubernetes for this project unless Kubernetes itself becomes an explicit learning goal.

A simpler alternative such as Render, Railway, Fly.io, or another low-cost host is acceptable if cloud-specific Azure learning is not currently the priority.

## Authentication

Authentication is not required for the first vertical slice if the app is used by one developer locally.

When authentication is introduced:

* use OpenID Connect / OAuth 2.0
* avoid implementing password security yourself
* keep identity separate from nutrition domain concepts
* use stable user identifiers in the application

## Vertical Slice Workflow

Implement features end-to-end.

For example, "Log food" should preferably include:

1. domain modelling
2. application command
3. persistence
4. API endpoint
5. tests
6. mobile UI

Avoid building the entire backend before validating workflows in the mobile client.

## Decision Records

For meaningful architectural decisions, create lightweight Architecture Decision Records under:

```text
docs/adr/
```

Examples:

* database choice
* diary aggregate boundary
* nutritional snapshot strategy
* external food provider
* authentication approach
* speech/LLM provider

Keep ADRs short.

## Documentation

Maintain:

```text
README.md
AGENTS.md
docs/
  adr/
```

README should explain:

* product purpose
* architecture
* local setup
* commands
* environment configuration
* testing
* deployment

AGENTS.md defines implementation guidance for coding agents and contributors.

## Rules for Coding Agents

Before implementing a feature:

1. understand the user-facing use case
2. identify affected domain concepts
3. inspect existing patterns in the repository
4. propose the smallest coherent change
5. avoid introducing new dependencies without justification

When architecture is affected, present at least two viable approaches with trade-offs before making a major structural change.

Do not silently introduce:

* event sourcing
* microservices
* message brokers
* Kubernetes
* generic repository frameworks
* large state-management libraries
* new cloud services
* new AI providers

Prefer the simplest architecture that supports the current requirements.

When uncertain about a domain rule, state the ambiguity instead of inventing business behaviour.

Do not modify unrelated code.

Do not perform broad refactorings as part of a feature unless they are necessary.

Keep commits and changes conceptually focused.

## Definition of Done

A feature is complete when applicable:

* domain behaviour is implemented
* business rules are covered by tests
* application use case is implemented
* persistence changes include migrations
* API contract is implemented
* mobile UI supports the workflow
* errors are handled
* relevant documentation is updated
* CI passes
* no secrets or temporary debug code are committed

## Initial Backlog

Recommended first implementation sequence:

1. create solution and project structure
2. configure PostgreSQL and EF Core
3. model `Food` and nutritional values
4. implement create-food use case
5. implement food search
6. model diary entries
7. implement `LogFood`
8. implement daily diary query
9. calculate daily calorie and macro totals
10. create the first React Native diary screen
11. support editing and deleting entries
12. add daily nutrition targets
13. add recent foods and favourites
14. add barcode scanning
15. integrate an external food provider
16. add voice tracking
17. add authentication
18. deploy a production-like environment

The backlog is intentionally ordered so that the core domain is validated before external integrations and infrastructure complexity are introduced.
