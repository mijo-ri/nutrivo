# Proposal

## Why

The diary compares actual consumption against hard-coded example targets, while the Ziele tab is only a placeholder. Users need to set their own daily calorie and macro targets and retain them across reloads and application restarts.

## What Changes

- Persist one current target set for the local application in PostgreSQL, with independent kilocalories, protein grams, carbohydrate grams and fat grams.
- Apply the current targets to every displayed diary date, including past dates. Updating targets changes comparison values without modifying food entries, snapshots or consumed totals.
- Start without a target set: do not seed the database, prefill example values or substitute examples on read failure.
- Replace the Ziele placeholder with a form that loads, explicitly saves and subsequently edits all four values, with validation and loading, empty, pending, success and error states.
- Show consumed nutrition without progress or remaining calories while targets are unset, and offer a link to Ziele. Once saved, use the shared persisted targets for diary comparisons.
- Add focused read/set use cases and REST endpoints, using existing persistence, error-contract and query-cache patterns.
- Require all four nonnegative values; explicit zero is valid and distinct from unset targets. Omit percentage progress for individual zero targets.

## Capabilities

### New Capabilities

- `nutrition-targets`: Maintain a persistent current daily calorie/macro target set, configure it through Ziele, and use it as the diary's comparison basis across all dates.

### Modified Capabilities

None. Existing `nutrition-diary` requirements concern persisted entries and authoritative consumed totals; they do not specify target management. Target-dependent presentation belongs to the new capability and preserves those guarantees.

## Impact

- Domain/Application: a focused daily-target model, domain validation, `GetDailyNutritionTarget` and `SetDailyNutritionTarget`, DTOs and narrowly scoped persistence abstractions.
- Infrastructure: an additive EF Core mapping/migration with no seed data, singleton-safe persistence and PostgreSQL integration coverage.
- API: additive `GET /nutrition-targets` and `PUT /nutrition-targets`, stable Problem Details and OpenAPI documentation.
- Mobile: the existing Ziele route, a shared TanStack Query target cache, target-aware diary summary states, and removal of `exampleTargets` and example-specific wording.
- Documentation/validation: API/mobile documentation, a lightweight ADR during implementation, backend tests and manual frontend verification plus scoped formatting, lint and TypeScript checks.
- No new dependencies, authentication, per-user identity, target history, effective dates, automatic calorie derivation, percentage-based macros, deletion/reset endpoint or offline write queue.
