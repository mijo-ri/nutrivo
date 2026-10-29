# AGENTS.md
## Project
Build a full-stack calorie and nutrition tracking app to practise .NET 10, DDD, Clean Architecture, CQRS, EF Core, React Native, testing, Docker, GitHub Actions, cloud deployment and AI-assisted development.
The app is inspired by MyFitnessPal but should evolve from its own domain model.

## Product Goal
Users should track food with minimal friction and see daily calories and macronutrients.
Planned features: food tracking, daily nutrition overview, personal targets, reusable foods/meals, recent foods, favourites, barcode scanning, voice input and external nutrition providers.
Example voice input: "100 grams of oats, 4 eggs and 2 tablespoons of olive oil".
Voice input must become structured entries and be confirmed before saving.

## Development Strategy
Build incrementally using vertical slices.
Phase 1: create/select food, define nutrition per reference quantity, log consumed amount, show daily diary/totals, edit/delete entries, manage daily targets.
Phase 2: search, recent foods, favourites, meals/recipes and serving sizes.
Phase 3: barcode scanning, external food lookup, voice tracking, authentication and deployment.
Do not start with barcode, voice, authentication or external providers.

## Solution Structure
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
Dependencies must point towards the Domain.

## Domain
The Domain may contain aggregates, entities, value objects, domain services, domain events, domain errors and business rules.
It must not depend on EF Core, ASP.NET Core, Azure SDKs, external APIs or UI frameworks.
Prefer behaviour-rich models over anemic entities.
Potential concepts: `Food`, `NutritionalValues`, `ServingSize`, `NutritionDiary`, `DiaryEntry`, `DailyNutritionTarget`, `Meal`, `Recipe`, `Quantity`, `Weight`, `Energy`, `Macronutrients`.
These are starting points, not fixed requirements.

### Domain Rules
- make units explicit
- use value objects where they improve correctness
- prefer `decimal` for nutritional calculations
- define nutrients against a clear reference quantity
- define rounding centrally and test it
- avoid one aggregate containing a user's full diary history
- preserve historical diary accuracy when food data changes
Prefer a nutrition snapshot in `DiaryEntry` when historical accuracy is required.
Consumed nutrients: `consumed quantity / reference quantity * nutritional values`.

## Application
The Application layer coordinates use cases and may contain commands, queries, handlers, DTOs, validation and interfaces.
Domain invariants belong in the Domain, not in handlers.
Typical commands: `CreateFood`, `LogFood`, `UpdateDiaryEntry`, `RemoveDiaryEntry`, `SetDailyNutritionTarget`.
Typical queries: `GetDiaryDay`, `SearchFoods`, `GetRecentFoods`, `GetNutritionSummary`.
Use CQRS to make use cases explicit.
Do not add separate read/write databases, event sourcing or messaging without a concrete need.

## Infrastructure
Infrastructure may contain EF Core, PostgreSQL, repositories, external food providers, barcode providers, AI/LLM providers, authentication and telemetry.
External provider DTOs must not leak into the Domain.
Use abstractions owned by inner layers where appropriate.
Avoid generic repositories such as `IRepository<T>` unless clearly justified.
Direct EF Core projections in query handlers are acceptable.

## API
- prefer REST initially
- keep endpoints thin
- use DTOs and stable error contracts
- do not expose EF entities
- generate OpenAPI documentation

## React Native
Use React Native with TypeScript.
Prefer Expo unless a required native capability prevents it.
Start with local state; use TanStack Query when server-state caching becomes useful.
Do not introduce Redux without a concrete need.
The backend remains authoritative for domain rules.

## Barcode Tracking
Treat barcode lookup as an external capability.
Flow: `scan barcode -> lookup product -> map provider data -> confirm -> log food`.
Keep providers replaceable and preserve source information where useful.

## Voice Tracking
Voice is an input mechanism, not a source of nutritional truth.
Flow: `speech -> speech-to-text -> structured interpretation -> food matching -> confirmation -> LogFood`.
LLMs may interpret language but should not invent nutritional values when trusted data exists.
Ambiguous matches must be shown to the user.

## Testing
Domain tests cover nutritional calculations, quantity conversion, rounding, diary rules, targets and invariants without requiring a database.
Application tests cover command/query behaviour and avoid unnecessary internal mocking.
Integration tests cover EF Core mappings, PostgreSQL queries, API behaviour and authentication when introduced.
Prefer Testcontainers for PostgreSQL integration tests.

## Code Quality
- enable nullable reference types
- use async APIs for I/O
- pass `CancellationToken`
- prefer immutable value objects
- avoid speculative abstractions
- prefer readable code over clever code
- do not use exceptions for expected business failures
- keep secrets outside source control

## Docker and CI/CD
Use Docker Compose for local API + PostgreSQL when useful and multi-stage Docker builds.
GitHub Actions should restore dependencies, build, run backend tests, lint/type-check mobile code, run mobile tests and build artifacts.
Deploy only after successful validation.

## Cloud
Prefer a simple, inexpensive setup.
Azure is preferred when practising Microsoft cloud skills.
Possible services: Azure App Service or Container Apps, Azure Database for PostgreSQL and Application Insights.
Do not introduce Kubernetes unless it becomes an explicit learning goal.

## Authentication
Authentication is not required for the first local vertical slice.
When introduced:
- use OpenID Connect / OAuth 2.0
- do not implement password security manually
- keep identity separate from nutrition domain concepts
- use stable user identifiers

## Architecture Decisions
Store lightweight ADRs in `docs/adr/`.
Use them for meaningful decisions such as aggregate boundaries, database choice, nutrition snapshots, external providers, authentication and voice/LLM integration.

## Rules for Coding Agents
Before implementing a feature:
1. understand the user-facing use case
2. identify affected domain concepts
3. inspect existing repository patterns
4. propose the smallest coherent change
5. avoid new dependencies without justification
For major architectural changes, present at least two viable approaches with trade-offs first.
Do not silently introduce microservices, event sourcing, message brokers, Kubernetes, generic repository frameworks, large state-management libraries, new cloud services or new AI providers.
Prefer the simplest architecture supporting current requirements.
When a domain rule is unclear, state the ambiguity instead of inventing behaviour.
Do not modify unrelated code or perform broad refactorings without necessity.

## Formatting
- After creating or modifying C# code, run JetBrains CleanupCode before the final build and tests required for the task.
- Run `dotnet tool restore` from the repository root if the local tools need restoring.
- From the repository root, run `dotnet jb cleanupcode <File1.cs> <File2.cs>` with explicit, correctly quoted paths to only the C# files created or modified for the task. Do not pass the solution, directories or wildcards.
- Use file mode, which applies formatting only, according to the repository's `.editorconfig`.
- Review the diff after formatting and preserve unrelated changes already present in the working tree.
- If the formatter is unavailable or fails, explicitly report the failure in the final response; do not claim formatting succeeded.

## Initial Backlog
- [x] create solution structure
- [x] configure PostgreSQL and EF Core
- [x] model `Food` and nutritional values
- [x] implement `CreateFood`
- [x] implement food search
- [x] model diary entries
- [x] implement `LogFood`
- [ ] implement daily diary query
- [ ] calculate daily totals
- [ ] build first React Native diary screen
- [ ] edit/delete entries
- [ ] add nutrition targets
- [ ] add recent foods and favourites
- [ ] add barcode integration
- [ ] add external food provider
- [ ] add voice tracking
- [ ] add authentication
- [ ] deploy a production-like environment
