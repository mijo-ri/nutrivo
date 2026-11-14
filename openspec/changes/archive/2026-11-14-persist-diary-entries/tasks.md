# Tasks

## 1. Historical amount-update behavior

- [x] 1.1 Add direct-quantity and existing-serving-count update behavior to `DiaryEntry`, preserving original amount mode, immutable snapshots, identity, date and meal; add domain tests for g/ml, fractional counts and recalculation from the original reference basis after catalogue changes.
- [x] 1.2 Add domain tests proving unsupported mode changes, invalid/nonpositive amounts, quantity underflow/overflow and nutrition overflow cannot partly mutate an entry; verify repeated edits use original reference precision rather than rounded consumed values.
- [x] 1.3 Record the amount-only historical-edit decision and alternatives in a new `docs/adr/` entry; run JetBrains CleanupCode in file mode against explicit changed C# paths before running the domain tests, and verify both the tests and reviewed diff.

## 2. Entry detail read

- [x] 2.1 Add a focused `GetDiaryEntryById` query, historical detail read model and EF projection independent of catalogue joins; verify application tests cover success, empty/missing IDs, cancellation and storage failure propagation.
- [x] 2.2 Add `GET /diary-entries/{id}` with DTOs, full-precision historical reference values, rounded consumed nutrition and stable Problem Details; add PostgreSQL/API tests for direct and serving entries, deleted/changed catalogue foods, unknown IDs and precision retention.
- [x] 2.3 Document detail reads in `docs/api.md` and generated OpenAPI metadata; format explicit changed C# files with CleanupCode before running the relevant tests, and verify documented schemas/statuses plus unchanged daily-read behavior.

## 3. Entry amount update and deletion

- [x] 3.1 Extend focused diary repository operations and add `UpdateDiaryEntry` command handling, delegating amount-mode and numerical invariants to the domain; verify application tests cover valid modes, mixed/missing input, invalid IDs, not-found and unchanged entries after rejection.
- [x] 3.2 Add `PUT /diary-entries/{id}` with amount-only DTO and committed `204` success; verify API tests cover quantity/count updates, invalid/mismatched amounts, historical values after catalogue deletion, and generic storage-failure responses.
- [x] 3.3 Implement `RemoveDiaryEntry` and `DELETE /diary-entries/{id}` using a filtered delete with affected-row not-found detection; verify application and PostgreSQL/API tests cover repeated deletion, selected-entry isolation, unchanged catalogue data and commit failures.
- [x] 3.4 Add persistence/API tests asserting updates preserve snapshots, `CreatedAt`, ID and daily entry order, daily/meal totals change correctly, and a concurrent delete cannot be undone by an update; verify concurrent deletes have one success and one not-found response.
- [x] 3.5 Document PUT/DELETE payloads, historical mode restrictions, error codes and OpenAPI responses in `docs/api.md`; run CleanupCode on explicit changed C# files before relevant application/integration tests, and verify existing LogFood/daily-read/catalogue-history tests still pass.

## 4. Persistent diary reads and food additions

- [x] 4.1 Add diary API DTO mapping and TanStack Query hooks keyed by date and entry ID using the existing API client; verify enum/unit mapping against documented backend JSON and run scoped Prettier, lint and TypeScript checks on the mobile changes.
- [x] 4.2 Replace seeded/local entries in `DiaryProvider` and `DiaryScreen` with daily queries, preserving transient add confirmation where needed; extract nutrition helpers/types from sample data and use backend daily/meal totals. Verify empty/existing days, date isolation, focus refresh, loading and retry states manually, plus lint/typecheck.
- [x] 4.3 Connect the existing create form to persisted LogFood with pending guards, no mutation retries and affected-day invalidation; verify a direct quantity and fractional serving survive page reload, separate additions stay separate and failed submissions retain input.
- [x] 4.4 Handle diary-specific errors and uncertain write outcomes without showing food-catalogue wording; provide a read reconciliation path and distinguish committed writes from failed refreshes. Verify these states manually and confirm cancelled/stale date reads cannot overwrite the selected day's data.
- [x] 4.5 Update `docs/mobile.md` to replace local/example persistence expectations with real diary behavior and clearly state that existing target values remain illustrative; format only explicitly modified mobile files, run format:check, lint and typecheck, and review the diff for unrelated formatting.

## 5. Shared entry amount editing

- [x] 5.1 Extract the existing amount form into a reusable component with initial draft, reference data, allowed controls and async submit callbacks; verify the create flow retains all direct/serving choices, preview behavior, keyboard support and cancellation.
- [x] 5.2 Add an entry-edit route and make diary rows open it by entry ID; prefill from historical detail, lock original amount mode and food/date/meal, and invalidate the day/detail after save. Verify both entry modes, direct edit links and editing after catalogue deletion manually.
- [x] 5.3 Preserve the selected diary date through edit/cancel/back navigation and show missing-entry/read/write failures with retry or return actions; verify save updates the existing row without changing its position or creating an additional entry, and cancel persists nothing.
- [x] 5.4 Document amount editing and manual checks in `docs/mobile.md`; run Prettier and format:check with explicit changed mobile paths, then lint/typecheck, and manually compare create/edit forms at narrow width and with keyboard navigation.

## 6. Swipe-to-delete diary rows

- [x] 6.1 Add an entry row with a left-swipe reveal using existing React Native primitives, a red delete action and an accessible gesture-free equivalent; verify horizontal drag versus tap and vertical scrolling, pointer behavior on web and keyboard/assistive-technology reachability manually.
- [x] 6.2 Connect the action to entry deletion with pending guards, confirmed-success cache updates and missing/failed/uncertain outcome handling; verify only the chosen row disappears, other occurrences/catalogue foods remain, and meal/daily totals refresh correctly after reload.
- [x] 6.3 Document swipe/delete behavior and its manual checks in `docs/mobile.md`; run scoped Prettier/format:check, lint and typecheck, then verify no new dependencies or automated frontend tests were introduced.

## 7. Complete-slice verification

- [x] 7.1 Run the backend build and full backend tests after required scoped CleanupCode, including PostgreSQL/Testcontainers integration coverage; verify all results and report any unavailable tooling or runtime rather than marking blocked checks as passed.
- [x] 7.2 Run final mobile lint/typecheck and scoped format:check after the last mobile edit; verify a clean task diff without unrelated formatting or environment changes.
- [x] 7.3 Use the existing `http://localhost:8081` environment for an end-to-end check: empty/existing days, add direct/serving entry, reload, edit amount, cancel, delete by left swipe, authoritative totals and date preservation. Include failure/retry and accessible deletion checks where possible; if unreachable, report the unperformed checks for the user without creating another environment.
- [x] 7.4 Validate the OpenSpec change with `openspec validate persist-diary-entries --strict` and review proposal/spec/design/task consistency; verify no food/date/meal changes, new catalogue serving selection or target persistence slipped into the implementation scope.
