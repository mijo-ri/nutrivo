# Design

## Context

See `proposal.md` for motivation and `specs/food-catalogue/spec.md` for observable behavior. This design is warranted because the change crosses food UI, the shared HTTP client, cached catalogue reads and API development access.

Observed implementation:

- `FoodDetailsScreen` already has a destructive `FoodButton`, currently disabled, and uses `useFood` for detail reads.
- `foodsApi.ts` maps API DTOs; `foodsQueries.ts` owns detail keys and paginated list keys shared by catalogue search and diary food selection.
- `FoodsProvider` supplies one TanStack Query client, 30-second query freshness and disabled mutation retries. Screen-focus gating disables inactive food reads, so invalidating an inactive query alone can leave cached rows visible briefly on re-entry.
- `apiClient.ts` accepts GET/POST/PUT only, always parses successful bodies as JSON, preserves Problem Details status/code in `ApiError`, and times out after 15 seconds.
- `DELETE /foods/{id}` already returns empty 204 or existing Problem Details. ADR 0011 and backend integration tests cover permanent deletion, serving cleanup and historical diary preservation.
- Development CORS in `Program.cs` allows GET/POST/PUT. `DevelopmentAccessTests` already tests configured origins and write preflight without needing PostgreSQL.
- `DiaryDatePickerDialog` demonstrates the existing themed React Native Modal layout. There is no general confirmation-dialog component. Earlier local deletion acceptance notes in `docs/mobile.md` are historical rather than current functionality.

## Goals / Non-Goals

**Goals:**

- Reuse existing food API, query, theme and navigation patterns with a small feature-local dialog and mutation.
- Make server-confirmed absence authoritative across details and cached searches, including page boundaries, without mutating diary state.
- Separate deletion outcome from subsequent read failures and from uncertain network outcomes.

**Non-Goals:**

- No application-wide dialog framework, global notification store, offline mutation queue or persistent cache.
- No changes to the delete handler, domain rules, persistence mappings, authentication or production hosting policy.
- No attempt to prevent operating-system termination, browser closure or all browser-history navigation during a request.

## Decisions

### 1. Use a feature-local confirmation dialog with an explicit lifecycle

Add a `FoodDeleteDialog` alongside the food screens, following the existing Modal/theme approach. Keep orchestration in `FoodDetailsScreen`; bind the submitted ID to the displayed food rather than reading mutable route parameters after awaiting a request.

Use the German message `„<Name>“ dauerhaft löschen? Bereits erfasste Tagebucheinträge bleiben erhalten.` with `Abbrechen` and destructive `Dauerhaft löschen`. While submitting, keep the dialog visible, show `Wird gelöscht …`, disable both dialog actions and underlying edit/delete/back controls, and consume Android hardware Back. Use an immediate submission guard as well as mutation pending state so same-frame taps cannot submit twice. Guard any completion navigation against a different route or unmounted screen.

On Web, focus cancel when opened, contain Tab/Shift+Tab, support Escape before submission, and restore trigger focus on cancellation. Keep browser-specific focus work isolated from native behavior; on native use modal accessibility semantics and reachable labelled actions. Long names and large text must wrap/scroll using existing sizing conventions. Do not refactor the diary picker to share a speculative abstraction.

Alternative: native alerts plus a separate browser confirmation. This would create divergent layout and keyboard behavior; a small shared visual dialog fits the confirmed cross-platform experience without a dependency.

### 2. Extend the existing HTTP client for empty success responses

Allow DELETE in `apiRequest` and return without JSON parsing on status 204; retain JSON parsing and existing error normalization for all other responses. `deleteFood(id): Promise<void>` calls the encoded food URL with DELETE and no body or Content-Type header. Keep the current timeout and preserve HTTP status and Problem Details code.

Give DELETE connection/timeout failures a German message explaining that deletion might already have completed and advising a read check. Do not reuse the POST/PUT saved-food wording. Keep `retry: false` explicit in the deletion mutation.

Alternative: bypass `apiRequest` with a delete-specific fetch. That would duplicate URL validation, timeout, cancellation and error handling. Treating 204 as JSON would incorrectly report committed deletion as failure.

### 3. Reconcile catalogue caches only after confirmed absence

Introduce `useDeleteFood` next to existing mutations. On 204, or on `404` with `DeleteFood.NotFound`, stop rendering the old detail as actionable and pause its detail query during reconciliation. Cancel in-flight reads for that exact detail and for all `foodKeys.lists` before discarding stale data, preventing pre-deletion responses from repopulating the cache.

Reset all cached list variants, including inactive ones, so old pages and counts are unavailable and searches reload from page one when enabled. Evict the exact food detail without removing unrelated food details. Query hooks must not refetch or render the evicted detail as a usable food during the terminal UI transition. Use the existing list-key prefix so catalogue and diary selection participate automatically.

After 204, navigate with the existing `router.dismissTo('/foods')` pattern. Catalogue refresh is a read concern: do not let an awaited refetch failure turn a committed DELETE into a mutation error or keep the user on a submitting dialog. The overview can show its existing loading/error/retry state. Keep the terminal unavailable state for a deletion 404 until the user chooses the overview.

Do not optimistically remove foods before a definitive result. For ordinary 400/500 failures keep the food and error available. A failed detail refresh returning 404 takes precedence over cached `query.data`: hide edit/delete actions, reconcile catalogue state and show a route to the overview.

Alternative: invalidate lists alone or splice one row out of each page. Invalidation alone briefly exposes inactive cached rows; splicing does not repair shifted server page boundaries. Resetting the small in-memory catalogue caches gives correct ordering and counts at the cost of refetching.

### 4. Treat uncertain outcomes separately from rejected requests

For a known 400/500 error, leave the submitting state and show the normalized German error on details. A fresh attempt opens confirmation again. For connection loss/timeout, explain uncertainty and expose `Verfügbarkeit prüfen`, which only refetches the detail. Until that check confirms the food still exists, do not offer a second DELETE. Checking a missing food enters the unavailable state and reconciles catalogue caches; checking an existing food permits a new explicit confirmation. A failed availability check stays recoverable without any mutation retry.

A deletion 404 remains an error in the HTTP contract and is presented as `Dieses Lebensmittel ist nicht mehr verfügbar.` with an overview action; do not claim that this client deleted it. Historical diary data is never cleared or recalculated by this feature.

Alternative: automatically retry DELETE or silently turn every 404 into success. Both obscure what happened, and automatic retries are unnecessary for this permanent operation.

### 5. Extend development access within its existing boundary

Add DELETE to the explicit development method list. Preserve configured origins, allowed headers and the Development environment guard. Extend `DevelopmentAccessTests` with food-item DELETE preflights for both default origins and an additional configured origin, including the bodyless request without requested Content-Type; retain existing POST/PUT coverage and verify disallowed origins and Production receive no development CORS permission.

Alternative: `AllowAnyMethod` or enabling CORS globally. Neither is needed for this slice and both broaden access beyond the known requirements.

## Risks / Trade-offs

- A server commit can precede a lost response -> expose the uncertain outcome and read-only availability check; never retry DELETE automatically.
- A successful mutation can be followed by an unavailable API -> clear obsolete list pages immediately and show independent catalogue read recovery.
- Resetting all searches loses previously loaded pages -> accept the extra reads to avoid stale selection and skipped rows after server pagination shifts.
- Native Modal behavior does not establish Web focus containment by itself -> verify keyboard focus, Escape and focus restoration explicitly; keep focus helpers scoped to this dialog.
- Browser closure cannot be blocked reliably -> do not interpret unmount or timeout as proof of rollback; future reads stay authoritative.
- Native accessibility requires a device/runtime -> record any unavailable native checks honestly during implementation rather than marking them passed.

## Migration Plan

No schema migration or data backfill is needed. Ship the development CORS addition with the mobile changes; existing backend clients and response bodies remain compatible. Update the current CORS and food-management guidance plus deletion acceptance steps in `docs/mobile.md`, preserving dated historical validation records.

Before final implementation validation, format only changed mobile source files with the local Prettier scripts and changed C# files with JetBrains CleanupCode. Run mobile lint/typecheck, backend build and the affected CORS and existing delete integration tests. Manually cover the spec scenarios with the real API on Web and available Expo Go platforms; introduce no automated frontend tests.

Rolling back the UI/client/CORS changes disables this entry point but cannot restore catalogue foods already deleted. Existing diary history remains governed by ADR 0011.
