# Proposal

## Why

The diary currently starts with illustrative foods and keeps newly added entries only in React state, so reloading loses user input and ignores existing database entries. Users need a complete diary flow that loads, saves, adjusts and removes real entries within their meals.

## What Changes

- Replace seeded diary entries and local entry persistence with the existing daily-read and LogFood APIs, using backend daily and meal totals.
- Persist additions to the selected calendar date and meal and refresh the affected day after successful writes.
- Open a prefilled version of the same amount form when an entry is tapped. Editing changes only the consumed quantity or serving count; food, date, meal, entry identity and capture order remain unchanged.
- Calculate edits from historical food and serving snapshots, including when the catalogue food has changed or been deleted.
- Add diary-entry detail, amount-update and deletion APIs.
- Reveal an accessible delete action when an entry is swiped left; remove only that diary entry after confirmed server success.
- Handle loading, empty days, failed requests and uncertain write outcomes without substituting example entries or reporting unsaved changes as successful.
- Scope assumption: nutrition-target persistence is a separate feature. Existing illustrative targets remain unchanged by this slice; removing example data here concerns diary entries and consumed totals.

## Capabilities

### New Capabilities

- `nutrition-diary`: Persistent daily meal entries, authoritative totals, shared amount editing with historical snapshots, and entry removal.

### Modified Capabilities

None. The existing `food-catalogue` guarantees around historical diary preservation remain in force.

## Impact

- Backend: `DiaryEntry`, diary application handlers/read models/repository interfaces, EF Core persistence and thin REST endpoints. Reuse existing schema and snapshot storage; no new service or package is planned.
- API: retain `GET /diary-entries?date=...` and `POST /diary-entries`; add `GET`, `PUT` and `DELETE /diary-entries/{id}` with documented DTOs and Problem Details.
- Mobile: replace entry state in `DiaryProvider`, add diary API/query hooks using the installed TanStack Query, extract the amount form from `DiaryEntryCreateScreen`, add entry editing/navigation and swipe actions in `DiaryMealCard`.
- Verification: domain, application and PostgreSQL/API tests; scoped formatting, mobile lint/typecheck and manual checks through the existing `http://localhost:8081` environment when reachable. No frontend test infrastructure or separate manual-test environment.
- Out of scope: changing entry food/date/meal, selecting different catalogue servings while editing history, custom meals, nutrition-target management, authentication, offline write queues and undo.
