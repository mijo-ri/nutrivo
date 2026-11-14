# Tasks

## 1. Target domain model

- [ ] 1.1 Add the focused `DailyNutritionTarget` aggregate with a stable local singleton identity, four explicit-unit decimal values and a validating factory; verify it has no EF/API dependencies and accepts independently chosen fractional values without rounding or macro-energy derivation.
- [ ] 1.2 Add domain tests for each negative-field error, explicit zeros, mixed zero/positive values and decimal boundary values; verify failures return stable field-specific errors and no invalid candidate can be persisted.
- [ ] 1.3 Add a lightweight ADR in `docs/adr/` documenting the local singleton, all-date comparison policy, zero-versus-unset distinction and alternatives; review it against the confirmed decisions in this change.
- [ ] 1.4 Run required JetBrains CleanupCode in file mode with explicit changed C# paths before building and running domain tests; verify the tests pass and formatting does not alter unrelated code, or report unavailable tooling.

## 2. PostgreSQL persistence and migration

- [ ] 2.1 Add the focused application-owned repository abstraction, EF mapping and additive migration for the fixed-key target table with required unconstrained numeric columns and no seed row; verify the generated SQL and model snapshot enforce at most one current row without changing existing food/diary mappings.
- [ ] 2.2 Implement cancellation-aware current-target reads and an atomic complete-set upsert, registering the repository through existing DI; add PostgreSQL/Testcontainers coverage for absent reads, exact decimal round-trips, first save, replacement, repeated identical saves and concurrent first/replacement saves, verifying one complete submitted set survives.
- [ ] 2.3 Extend migration coverage, including existing hard-coded migration-count/latest-migration assertions, for fresh creation, upgrade with existing food/diary data, reapplication and downgrade/re-upgrade in disposable databases; verify target defaults are absent, existing data survives and no model changes remain pending.
- [ ] 2.4 Document explicit migration application and local singleton storage in `docs/development.md`, then run scoped CleanupCode before building and executing the affected persistence/integration tests; verify the documented workflow preserves the existing no-startup-migration policy and report any unavailable Docker runtime.

## 3. Target application use cases and API

- [ ] 3.1 Add `GetDailyNutritionTarget` and `SetDailyNutritionTarget` handlers, DTOs and DI using the existing command/query patterns; add application tests for nullable absence, complete replacement, invalid candidates never reaching persistence, independent/zero values and cancellation-token forwarding.
- [ ] 3.2 Add thin `GET /nutrition-targets` and `PUT /nutrition-targets` endpoints with the nullable read envelope, required four-field request and post-commit `200` response containing unrounded saved values; verify endpoints return DTOs rather than aggregates and introduce no change to existing diary/food contracts.
- [ ] 3.3 Add API integration tests for unset/read-after-restart, exact fractional values, independent and zero values, missing/null/negative/nonnumeric/out-of-range fields, unchanged storage after invalid writes, concurrent saves and generic storage failures; verify the required `200`/`400`/`500` statuses and stable domain error codes.
- [ ] 3.4 Document requests, envelopes, units, validation and all-date semantics in `docs/api.md` and generated OpenAPI; extend OpenAPI/endpoint registration tests to verify nullable targets and required fields, then run scoped CleanupCode before the affected build and application/API tests.

## 4. Ziele form and shared target state

- [ ] 4.1 Add target API adapters and TanStack Query hooks with one date-independent cache key, cancellation/focus handling, scoped target-specific error messages and non-retrying mutations; manually verify successful saves cancel obsolete reads and update the shared cache from the confirmed response without touching diary data queries.
- [ ] 4.2 Replace the Ziele placeholder with a labelled four-field form, blank initial draft, saved-value hydration, comma/point decimal entry and explicit save feedback; manually verify required-field errors, independent edits, fractional-value reloads, explicit zeros, pending duplicate guards and no save from typing or leaving the page.
- [ ] 4.3 Implement separate initial-loading/unset/error states, dirty-draft protection and failed/uncertain-save handling with an explicit read-only saved-value check; manually simulate failures and background/focus refreshes, verifying drafts are retained, stale reads cannot undo a confirmed save, and reconciliation does not silently adopt values or resend PUT.
- [ ] 4.4 Update the Ziele flow and manual checks in `docs/mobile.md`; from `mobile/nutrivo-app`, run `npm run format -- <explicit changed paths>` and `npm run format:check -- <the same explicit paths>`, followed by `npm run lint` and `npm run typecheck`, verifying no unrelated formatting, new dependencies or frontend test infrastructure.

## 5. Diary target comparisons

- [ ] 5.1 Connect `DiaryScreen` and `DiarySummary` to the shared current targets, remove `exampleTargets`/its unused module and replace example-specific accessibility labels; manually verify current targets apply to today and past dates while entry snapshots, authoritative consumed totals and selected-date navigation remain unchanged.
- [ ] 5.2 Render consumed kcal/macros independently of targets, with distinct loading/error feedback and the unset-state setup link; manually verify null targets omit denominators/rails/remaining values, target read failure does not block the diary, and cached comparisons show a visible warning when refresh fails.
- [ ] 5.3 Handle zero and over-target comparisons, retaining numeric consumption, omitting only zero-target percentage rails and capping positive-target fills; manually verify all-zero and mixed-zero sets, zero remaining at zero consumption, excess calories labelled as over target, and no invalid percentage output.
- [ ] 5.4 Update `docs/mobile.md` and applicable stale target statements in `docs/PROJECT.md` for real target persistence and the accepted current-set policy; format/check only explicitly changed mobile files, then run lint/typecheck and manually check narrow layouts, larger text, field labels, keyboard navigation and the setup link's accessibility.

## 6. Complete-slice verification

- [ ] 6.1 After the last C# edit, run scoped CleanupCode on all C# files created/modified for this slice, then the backend build and full backend tests including PostgreSQL/Testcontainers integration tests; verify results, migration lifecycle and existing food/diary behavior, and report unavailable checks rather than marking them passed.
- [ ] 6.2 After the last mobile edit, run scoped Prettier/format:check and mobile lint/typecheck, and verify the complete flow in the existing `http://localhost:8081` environment if reachable: initially unset, setup, save, diary update, past date, edit, reload, independent/zero values, invalid input and read/write failure recovery; report unperformed platform/manual checks without setting up another environment.
- [ ] 6.3 Run `openspec validate persist-nutrition-targets --strict` and review proposal/spec/design/task consistency and the final implementation diff; verify only the target slice changed, no seeds/history/authentication/new packages or frontend tests were introduced, and all performed checks are accurately recorded.
