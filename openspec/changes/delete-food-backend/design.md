# Design

## Context

See `proposal.md` for motivation and `specs/food-catalogue/spec.md` for the behavior contract. The slice spans API, Application and Infrastructure and benefits from explicit persistence and concurrency decisions before implementation.

- `IFoodRepository` has untracked lookup, locked update lookup, update commit and creation operations. Each write owns its persistence boundary.
- `FoodRepository.GetForUpdateAsync` locks the catalogue row before loading servings. `SaveAsync` handles serving reordering within that transaction.
- `FoodConfiguration` already cascades deletion from `foods` to `food_serving_sizes`.
- `DiaryEntryConfiguration` stores food and serving IDs as snapshot scalars without catalogue foreign keys. ADR 0007 records this boundary; ADR 0008 keeps diary reads independent of catalogue joins.
- Existing persistence and diary-query tests delete foods directly and verify snapshot and total preservation. They do not exercise a public delete endpoint.
- Endpoint and handler assembly scanning already discovers new slices. Several repository test doubles must be extended when the interface changes, and endpoint registration currently asserts six endpoints.

## Goals / Non-Goals

**Goals:**
- Implement the behavior through the existing CQRS and repository boundaries with a single atomic catalogue write.
- Preserve exact stored diary data and existing query behavior without modifying diary aggregates or mappings.
- Keep expected missing-food outcomes separate from technical failures.

**Non-Goals:**
- No domain deletion flag, generic repository, unit-of-work abstraction, version token, new dependency or schema migration.
- No extension of the existing logging transaction boundary to coordinate in-flight logging with catalogue deletion.
- No frontend code, client wiring or CORS expansion; direct API access and same-origin Swagger access are sufficient for this backend slice.

## Decisions

### 1. Permanent catalogue deletion with existing snapshot independence

Delete the food and rely on the existing serving foreign-key cascade. Do not query diary usage, remove diary rows, clear snapshot IDs or recalculate nutrients. IDs in historical snapshots remain valid historical identifiers even when their catalogue record is absent.

Alternatives: soft deletion would add query filters and restoration semantics the user did not request; blocking deletion of used foods contradicts the confirmed behavior. No `Food.Delete` method is needed because the aggregate has no new state transition or conditional deletion invariant. Record this decision in a lightweight ADR during implementation, referencing ADRs 0007, 0008 and 0010.

### 2. Focused atomic repository operation

Add `Task<bool> DeleteAsync(FoodId id, CancellationToken cancellationToken)` to `IFoodRepository`: `true` means the food was deleted and committed, and `false` means no food matched. Implement it as a filtered `ExecuteDeleteAsync` on `Foods`, comparing the affected row count to one. The repository is scoped; this operation runs in the fresh request context and does not call `GetForUpdateAsync` or `SaveAsync`.

A single PostgreSQL delete statement atomically removes the food and cascaded servings. It bypasses EF tracking, so it needs no aggregate load or `SaveChangesAsync`. Its database row lock coordinates with existing catalogue update locks. Concurrent deletes yield one affected row for the winner and zero for the other request. An update completing first may be followed by deletion; a delete completing first leaves the existing update lookup with no food. Neither path recreates the food.

Alternative: loading a tracked aggregate, checking existence and then deleting adds reads and requires additional handling when another request deletes between those steps. Reusing `SaveAsync` would unnecessarily couple deletion to serving-position rewrites. The direct affected-row result avoids these complications without a new persistence abstraction.

### 3. Application command and thin endpoint

Add `DeleteFoodCommand(Guid Id)`, `DeleteFoodCommandHandler` and `DeleteFoodErrors.NotFound` in `Application/Foods/DeleteFood`. The handler validates with `FoodId.Create`, invokes `DeleteAsync`, returns `DeleteFood.NotFound` for `false`, and returns the deleted `FoodId` on success through the existing `Result<FoodId>` contract. Cancellation and unexpected storage exceptions propagate to the existing infrastructure; they are not expected business errors.

Add `DeleteFoodEndpoint` in `Api/Endpoints/Foods/DeleteFood`. Bind `Guid id` on `/foods/{id}` using the existing unconstrained route style so malformed UUIDs receive the existing binding `400`. No request DTO or successful response DTO is necessary. Return typed `NoContent` on success and Problem Details with stable codes for application failures, following `GetFoodByIdEndpoint`. Document operation ID `DeleteFood`, the Foods tag and statuses `204`, `400`, `404`, `500`. Reuse the global exception handler for generic technical errors.

Alternative: a new non-generic result or Unit type would broaden shared abstractions solely to represent an empty HTTP response. Returning the internal ID and discarding it at the endpoint fits the current contracts.

### 4. Preserve existing logging concurrency semantics

After committed deletion, new food lookups return missing, so existing detail, search, update and logging paths already provide the required behavior. A logging request that read the food before deletion may still finish saving its snapshot afterward. This is consistent with independent historical snapshots; this slice does not promise serialization between logging and deletion. Sequential tests must start their post-delete logging request after awaiting DELETE completion.

Alternative: lock food rows across logging and diary persistence to forbid in-flight completions. That changes an existing use case and transaction model without being necessary for catalogue removal or historical accuracy.

### 5. Verification through the actual deletion path

- Application tests cover valid deletion, missing ID, empty UUID without persistence access, cancellation token forwarding, cancellation and storage exception propagation.
- PostgreSQL/API tests use the existing Testcontainers fixture and `FoodApiFactory`. Delete foods with and without servings; verify persisted cleanup, unrelated data, and committed absence from a fresh context.
- Capture direct and serving-based entries across multiple days, then delete through HTTP and compare persisted values, day responses, daily totals and meal totals before and after.
- Verify subsequent search/detail/update/log behavior, invalid IDs, repeated and concurrent deletion, generic `500` responses in Development and Production, and OpenAPI metadata.
- Prove atomic rollback using a test-local PostgreSQL trigger that rejects a serving delete: the food, all servings and diary entries must survive the failed statement. Keep the trigger confined to the test's isolated database.
- Verify serialization against an existing locked food update using separate scopes and explicit transaction ordering. Avoid sleep-based assumptions about race timing.
- Extend repository test doubles and discovery assertions without changing production registration mechanisms. Existing domain tests remain regression coverage; no new domain rule requires new domain tests.

## Risks / Trade-offs

- [Permanent deletion has no application undo] -> This is the confirmed product behavior; do not add restoration infrastructure.
- [Future foreign keys could affect history] -> Retain snapshot mappings and add endpoint-driven persistence and diary regression checks.
- [Direct deletes bypass tracked state] -> Execute deletion in its request scope without first loading the aggregate; verify committed results using fresh scopes.
- [A log already in progress can finish after deletion] -> Preserve the documented existing lookup semantics and test requests begun after deletion commits separately.
- [Interface extension breaks existing test doubles] -> Update every `IFoodRepository` implementation found under `tests/` and run the full backend suite.

## Migration Plan

No database migration or data backfill is required: the existing cascade and snapshot columns already support deletion. Validate the implementation, deploy the backend, and exercise the endpoint through Swagger or an HTTP client. Reverting the code removes the endpoint without altering the schema; it does not restore foods already deleted. The mobile application remains outside this rollout.
