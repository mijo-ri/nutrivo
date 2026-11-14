# Design

## Context

See `proposal.md` for motivation and `specs/nutrition-diary/spec.md` for observable behavior. This cross-layer change warrants a design because historical editing affects domain behavior, persistence, transport and mobile state.

`DiaryProvider` currently seeds today's entries from `diaryExample.ts` and assigns local IDs when adding. `DiaryScreen` and `DiaryMealCard` sum client entry values. `DiaryEntryCreateScreen` embeds the existing direct/serving amount form and reads a catalogue food. The installed TanStack Query and shared `apiRequest` already support catalogue requests, but the latter's failure wording currently refers specifically to foods.

The backend already persists immutable food/serving snapshots and unrounded consumed nutrition. `GET /diary-entries?date=...` returns rounded display entries and independently calculated daily/meal totals; it does not expose the complete historical reference basis needed for editing. `DiaryEntry` currently only has creation behavior, and `IDiaryEntryRepository` only adds entries. Existing ADRs 0006-0009 and the `food-catalogue` spec require history to remain independent of catalogue changes/deletion.

## Goals / Non-Goals

**Goals:** Use existing storage and request patterns for a complete diary slice, keep invariants in the domain and retain one reusable form with explicit create/edit inputs.

**Non-Goals:** No second local entry store, offline mutation queue, generic repository, new meal aggregate, catalogue lookup during historical editing, new package, or target-management backend. Existing illustrative targets are a documented scope assumption; extract production nutrition types/helpers from `diaryExample.ts` rather than retaining example entry dependencies.

## Decisions

### 1. Use snapshot-based amount updates within the existing aggregate

Add domain operations for updating a direct quantity or the count of an existing serving entry. The entry's original mode stays fixed: a direct entry edits grams/ml; a serving entry edits its historical serving count. Validate positive amounts and calculate quantity/nutrition completely before assigning mutable consumed fields. Reuse `NutritionalValues.Scale` and existing range-error conventions. No failed operation may partly change the aggregate. Preserve food/serving snapshots, ID, date, meal and `CreatedAt`.

This deliberately restricts edit-mode controls in the shared form; creation still offers all current catalogue servings or a direct quantity. Alternative: rebuild the entry from today's catalogue data. That would fail for deleted foods and silently alter historical reference values. Alternative: offer switches among current servings while editing. That expands the confirmed amount-only scope and requires a new historical-serving policy.

### 2. Add a focused detail API instead of expanding daily reads

Add `GetDiaryEntryById`, `UpdateDiaryEntry` and `RemoveDiaryEntry` application slices with thin endpoints:

| Operation | Input / success | Failure contract |
| --- | --- | --- |
| `GET /diary-entries/{id}` | `200`: ID, date, meal, exact consumed amount, consumed nutrition, historical food ID/name/brand/reference quantity/unit/nutrients, optional historical serving and count | `400`, `404`, `500` |
| `PUT /diary-entries/{id}` | `{ quantity }` for a direct entry or `{ servingCount }` for a serving entry; `204` after commit | `400`, `404`, `500` |
| `DELETE /diary-entries/{id}` | no body; empty `204` after commit | `400`, `404`, `500` |

The detail response exposes historical reference nutrients and quantities at full calculation precision; consumed nutrition remains output-rounded with `NutritionRounding`. The update DTO contains no writable food/date/meal/reference/serving-definition fields. Mixed or missing amounts use `UpdateDiaryEntry.Amount.Invalid`; the domain rejects mismatched mode with `DiaryEntry.Amount.ModeMismatch`. Use `DiaryEntry.Id.Empty` for empty IDs and operation-specific `*.NotFound` codes. Domain validation/range errors pass through. Malformed IDs/JSON use the existing Problem Details conventions. Document operations and schemas in generated OpenAPI.

Alternative: include full reference snapshots on every daily entry. A detail query avoids increasing the daily payload and supports direct edit links without requiring a date or cached day. Do not reconstruct a reference basis from rounded consumed values.

### 3. Keep persistence operations focused and commit before returning

Extend diary read/repository abstractions with focused ID lookup, save-updated-entry and delete operations, matching current repository commit semantics. Read the aggregate with snapshots for updates; save only its changed amount/nutrition fields. Use normal EF tracking with private setters/backing fields as needed. Do not replace the row or call the creation factory for an update, as either could change capture order or identity.

Delete with a filtered statement and interpret affected rows as success/not-found. If deletion races with a tracked update, map the missing-row concurrency failure to update-not-found rather than reinserting. Concurrent amount saves use last committed write wins; no version column is introduced in this local slice. All I/O receives `CancellationToken`. Existing unconstrained numeric columns and snapshot columns suffice; no migration is expected. Alternative: a generic repository/unit of work adds indirection without a current cross-aggregate transaction need.

### 4. Use the existing query cache as the mobile source of persisted state

Add diary API mapping and query/mutation hooks patterned after `foodsApi.ts`/`foodsQueries.ts`, with day keys by ISO calendar date and detail keys by entry ID. Map backend meal/unit enums explicitly. Use returned day/meal totals directly and preserve server entry order. Keep the selected date in the diary screen and retain it through edit navigation. Prevent stale responses for another date from appearing as the current day.

Remove example entry initialization and local generated IDs. `DiaryProvider` may remain for transient add-confirmation coordination used by `FoodSelectionScreen`; it must not own a second entry collection. Extract nutrition types/formatters into a production module and remove seeded meals/unused sample-only fields.

On committed writes invalidate/refetch the affected day; edits also invalidate their detail, and removals discard missing detail data. Refresh on screen focus using existing patterns. Use non-optimistic mutations with duplicate-submission guards and `retry: false`. Keep committed mutation success independent from later read-refresh failures. Handle missing targets by refreshing the affected day and showing a diary-specific message. During background-read failure, cached data may remain visible with a stale/error notice; an initial read failure is not an empty day.

Alternative: keep local state and copy server responses into it. That duplicates cache state and complicates reload/focus consistency.

### 5. Share the amount form while separating its data sources

Extract the form from `DiaryEntryCreateScreen` into a diary amount form with initial draft, display/reference data, allowed amount choices, pending/error state and submit/cancel callbacks. Creation gets current catalogue food data and submits `LogFood`. A new edit route loads entry detail by ID, adapts its historical snapshot to the same form, locks amount mode and submits the amount update. Read-only food/date/meal context remains visible. Cancel writes nothing; successful edit returns to the selected diary day.

Client calculations remain previews. Backend decimals determine persisted nutrition; display values never become reference calculation inputs. Preserve draft text on mutation failure. At the diary boundary translate generic food-specific API errors into entry-specific wording, including uncertain outcomes. After an ambiguous write offer a read-only reconciliation path before another submission; do not automatically retry. Alternative: duplicate the form for editing, which would allow validation, accessibility and preview behavior to drift.

### 6. Reveal a delete action using existing React Native primitives

Implement a focused row component around each diary entry. Use `PanResponder` and `Animated` for a left-swipe reveal, with a horizontal threshold/direction check so vertical diary scrolling remains available. A completed swipe reveals a red action; tapping it sends DELETE. Suppress row edit activation following a drag, close a revealed row predictably, and disable repeated pending deletion.

Provide an explicit keyboard/assistive-technology path to the same delete action; support pointer dragging on web. Keep the row until a confirmed deletion; reconcile `RemoveDiaryEntry.NotFound` by refreshing the day. A lost response reports an uncertain outcome rather than claiming the server retained the entry. Alternative: add a gesture package solely for these rows. Native primitives avoid a new dependency; browser and native interaction require manual verification.

## Risks / Trade-offs

- Rounded daily DTOs cannot support accurate historical previews -> use full reference precision on the detail DTO and test tiny/fractional nutrient values.
- A serving definition could disappear from the catalogue -> edit only the captured serving snapshot, with no catalogue dependency.
- Updating immutable-looking EF properties can accidentally rewrite snapshots/order -> use narrowly mutable consumed fields and persistence tests asserting snapshots and `CreatedAt` remain unchanged.
- A timeout can occur after commit -> disable mutation retries, retain draft input, explain the uncertain result and provide a diary reload path. Durable creation idempotency remains outside this slice.
- Last committed write wins can overwrite an earlier edit -> accepted for the current local app; test concurrent deletion does not resurrect entries. Future multi-user editing may need version checks.
- Web/native gesture behavior can differ -> manually verify swipe versus tap/scroll plus an accessible delete alternative. Use the existing localhost environment; report unavailable platforms rather than configuring a replacement.
- Nutrition targets remain illustrative -> document this boundary clearly; target persistence is a later change.

## Migration Plan

1. Implement and verify domain/application/persistence changes and additive endpoints; retain existing daily-read and logging contracts.
2. Integrate mobile queries, shared editing and row deletion. Existing database entries immediately become visible; local-only/example IDs are not migrated or saved.
3. Run backend tests, required scoped formatters and mobile checks, then manual verification through `http://localhost:8081` if reachable. If unavailable, document the manual checks for the user without creating another environment.
4. Rollback application code if necessary. No schema rollback is expected; already saved amount changes and deletions are real user data and are not undone by a code rollback.
