# Tasks

## 1. Atomic catalogue persistence

- [x] 1.1 Add the documented `Task<bool> DeleteAsync(FoodId, CancellationToken)` contract to `src/Nutrivo.Application/Foods/IFoodRepository.cs` and implement the filtered, affected-row-based delete in `FoodRepository`; update all repository test doubles under `tests/` so the solution compiles, verifying that `true` denotes committed deletion and `false` denotes absence without an aggregate load or `SaveAsync` call.
- [x] 1.2 Add PostgreSQL repository tests using the existing isolated-database fixture for foods with and without servings, missing/repeated deletion, token cancellation, committed absence in a fresh context and preservation of unrelated catalogue data; verify the cascade removes only the target food's servings.
- [x] 1.3 Add persistence tests for concurrent deletes and serialization with the existing locked update path using separate scopes and controlled transaction ordering; verify exactly one delete succeeds, an update is never resurrected after deletion, and no test relies on timing sleeps.
- [x] 1.4 Add a test-local PostgreSQL trigger that rejects serving deletion and verify the repository failure rolls back both food and serving deletion while preserving persisted diary entries and snapshots; keep the trigger inside the test's isolated database.
- [x] 1.5 Add a lightweight ADR under `docs/adr/` documenting permanent catalogue deletion, serving cleanup, preserved historical snapshot IDs and existing in-flight logging semantics; verify it agrees with ADRs 0007, 0008 and 0010 and the change specification.

## 2. DeleteFood command and HTTP API

- [x] 2.1 Add `DeleteFoodCommand`, `DeleteFoodCommandHandler` and `DeleteFoodErrors.NotFound` under `src/Nutrivo.Application/Foods/DeleteFood`; add application tests verifying `FoodId` validation, success, `DeleteFood.NotFound`, no persistence call for an empty UUID, token forwarding, cancellation and unexpected storage exception propagation.
- [x] 2.2 Add the thin `DeleteFoodEndpoint` under `src/Nutrivo.Api/Endpoints/Foods/DeleteFood` with no required body, empty `204` success, existing Problem Details mapping and complete OpenAPI metadata; extend handler discovery checks and update endpoint registration checks from six to seven endpoints, verifying automatic registration without manual production wiring.
- [x] 2.3 Add PostgreSQL/API tests that delete unused foods and previously used foods through HTTP, verify catalogue serving cleanup and unrelated data preservation, and compare persisted direct/serving snapshots and diary responses across multiple dates; verify entry order, consumed nutrition, daily totals and meal totals are unchanged.
- [x] 2.4 Add API tests for malformed/empty IDs, unknown and repeated targets, concurrent deletion, empty `204` response bodies, generic `500` errors in Development and Production, and OpenAPI parameter/status/description metadata; verify exact application error codes where specified and absence of storage exception details. Exercise the rollback trigger through HTTP to verify a real failed delete statement preserves food, servings and diary data.
- [x] 2.5 Add post-delete API checks for search exclusion and count reduction, detail `404`, update `404` and logging `404` for both direct and serving-based amounts; begin requests after DELETE completes and verify no catalogue recreation or new diary entries.

## 3. Formatting and integrated backend validation

- [x] 3.1 Restore local tools with `dotnet tool restore` if necessary, then run `dotnet jb cleanupcode` from the repository root with an explicit, correctly quoted argument for every C# file created or modified in this change, including test doubles; use file mode only, no solution/directory/wildcard arguments, review the diff and verify unrelated changes are preserved before the final build and tests.
- [x] 3.2 Run `dotnet build Nutrivo.slnx` and `dotnet test Nutrivo.slnx` with Docker available for the PostgreSQL Testcontainers suite; verify all backend tests pass, including existing diary and food-update regressions, and report any formatter or validation failure accurately.
- [x] 3.3 Run `openspec validate delete-food-backend --strict` and review the final diff; verify only the intended backend slice, its tests, ADR and change artifacts were changed, with no mobile code, frontend integration, CORS expansion, new dependencies or schema migrations.
