# Design

## Context

See `proposal.md` for motivation and `specs/nutrition-targets/spec.md` for behavior. This design is warranted because the slice introduces a persisted domain concept, schema evolution, two API operations and shared state between Ziele and the diary.

`TargetsScreen.tsx` currently renders an introduction. `DiaryScreen.tsx` imports `exampleTargets` from `diaryExample.ts` and passes them to `DiarySummary`. The summary requires a target set, calculates remaining calories and renders calorie/macro rails; one accessible label still says "Beispielziel". Actual diary entries and daily/meal totals already come from the backend.

The backend uses behavior-rich domain models, `Result<T>`/`DomainError`, explicit command/query handlers, focused repositories, fluent EF mappings, PostgreSQL `numeric` columns and explicit migrations. There is no authentication or user ownership model. Mobile already has TanStack Query, shared API transport, themed form controls and focus-based refresh patterns. `apiRequest` still has food-specific error wording, so target operations need a scoped error translation as the diary does.

## Goals / Non-Goals

**Goals:** Add a coherent vertical slice using the existing architecture, enforce one current local target set, preserve exact backend decimal values, and keep target availability independent of diary availability.

**Non-Goals:** No diary aggregate redesign, reference quantities for daily targets, shared nutrition-type relocation across the domain, generalized settings framework, additional client state provider or replacement of existing numeric transport conventions. The remaining product exclusions are in `proposal.md`.

## Decisions

### 1. Model one current daily target aggregate

Introduce a focused `DailyNutritionTarget` in a Targets domain namespace, using the existing aggregate base and a stable local singleton identity. A small fixed integer identity is sufficient; it is not a user identifier. A validating factory creates a complete candidate set before any persistence operation. Store get-only decimal properties named `Kilocalories`, `ProteinGrams`, `CarbohydratesGrams` and `FatGrams` with no reference quantity or unit conversion: these are daily absolute totals.

All four values are required and nonnegative. The user explicitly confirmed zero for every field; zero is configured, whereas an absent aggregate is unset. Use stable domain errors `DailyNutritionTarget.Kilocalories.Negative`, `DailyNutritionTarget.ProteinGrams.Negative`, `DailyNutritionTarget.CarbohydratesGrams.Negative` and `DailyNutritionTarget.FatGrams.Negative`. Do not enforce a macro-energy equation, introduce physiological minimums, or add an arbitrary precision/scale restriction. Missing/null/invalid JSON numbers are transport validation failures, not silently defaulted zeros. Expected domain failures return results rather than exceptions.

Alternative: reuse `Foods.NutritionalValues`. Its food-specific errors and scaling behavior describe food nutrition, while targets have their own lifecycle and semantics. Four scalar values in a validating aggregate avoid coupling or a broad refactor. Alternative: use a generic settings dictionary. That loses typed units and makes validation less explicit.

### 2. Persist a database-enforced singleton with an atomic set operation

Map to an additive `public.daily_nutrition_targets` table with fixed primary key `id = 1`, a check constraint restricting the key to 1, and four required unconstrained `numeric` nutrient columns, consistent with existing decimal storage. The migration creates no row and no defaults for target values. Domain code contains no EF/Npgsql dependency. Infrastructure owns the mapping/materialization details and singleton database constraints.

Add one focused application-owned `IDailyNutritionTargetRepository` with `GetCurrentAsync` and `SetCurrentAsync`, both accepting cancellation tokens. A query reads the optional current aggregate; a command validates a whole candidate before calling the set operation. Use a single PostgreSQL upsert on the fixed key for replacement, committing all four fields together before returning. Infrastructure can use the existing parameterized EF SQL support; no new package or generic repository is necessary. Do not use an existence check followed by an unprotected insert. Last committed write wins; duplicate identical PUTs do not add rows. Save failure before commit leaves the previous set unchanged.

Alternative: an EF tracked read-then-create/update needs explicit handling for concurrent first inserts. A fixed-key atomic upsert directly expresses this tiny singleton operation. Alternative: dated or per-user rows require ownership/effective-date rules that are outside the confirmed scope. Introducing authentication later will require an explicit ownership migration; this slice does not manufacture a user.

### 3. Expose an explicit nullable read envelope and complete PUT

Add `GetDailyNutritionTarget` and `SetDailyNutritionTarget` handlers through existing CQRS/DI patterns. Use a non-null result DTO containing a nullable `Targets` property so absence fits the existing non-null `Result<T>` contract without exceptions. The domain aggregate and storage key are not API entities.

| Operation | Success | Failure |
| --- | --- | --- |
| `GET /nutrition-targets` | `200` with `{ targets: null }` or `{ targets: { kilocalories, proteinGrams, carbohydratesGrams, fatGrams } }` | Generic `500` Problem Details |
| `PUT /nutrition-targets` | Required request fields `kilocalories`, `proteinGrams`, `carbohydratesGrams`, `fatGrams`; `200` with the same non-null target envelope after commit | `400` for invalid input/domain validation; generic `500` for unexpected storage failure |

Return saved target decimals without calling `NutritionRounding.ForDisplay`; edit inputs must not inherit rounded diary display values. Use required decimal request properties and existing malformed-body Problem Details handling. Pass field-specific domain codes through the error `code` extension; document framework request-binding errors consistently with existing API behavior. Generated OpenAPI describes nullable GET targets, required PUT fields, units, zero validity, independent values, atomic replacement and response/error schemas. Existing food and diary API contracts are unchanged.

Alternative: `404` for unset targets requires special error handling for a normal first-use state. An explicit null envelope makes successful absence distinguishable from connectivity/storage failure. Returning `204` after PUT would require an extra read to get the authoritative saved values; `200` can populate the shared cache immediately.

### 4. Share one target query between Ziele and the diary

Introduce feature-local target DTOs, API adapters and query/mutation hooks with one key such as `['nutrition-targets', 'current']`, independent of diary date. Reuse the installed TanStack Query client and existing client/focus guards, passing read cancellation signals. No Targets provider or copy of persisted target state is needed. Translate network/validation failures into target-specific wording at this boundary without changing unrelated food flows.

On confirmed PUT success, cancel in-flight target reads that could replace the saved response with an older value, then place the returned envelope into the shared cache. Mark the form clean only after confirmation, retaining the saved values for both consumers. Refresh on subsequent focus. Updating targets does not invalidate or recalculate diary entry/day queries because their consumed values did not change. A late response for a target read must not reset the selected diary date or an edited form draft.

Alternative: store targets in `DiaryProvider` or a second local store. That creates another persisted-state owner and complicates refresh/reload consistency.

### 5. Use an explicit-save form with a separate draft lifecycle

Replace the Ziele introduction within the existing route/layout and reuse the theme and suitable existing form controls. Provide labelled fields for kcal and grams, an explicit save action and in-page success/error feedback. After successful initial load, initialize a string-based draft exactly once from saved values or four empty strings. Distinguish empty strings from explicit `0`. Accept complete decimal input using comma or point, reject partial parses/grouping/nonfinite input, and map backend field errors to the corresponding inputs. Keep parsing local to targets if sharing would require changing unrelated food code.

Populate saved inputs from unrounded numeric values with the existing app's no-grouping numeric conventions, rather than the diary's whole-number formatter. The backend remains authoritative for decimal validation. The app uses JavaScript numbers and JSON numeric transport, as existing forms do; do not claim lossless 28-digit decimal round-trips through mobile or introduce a decimal library in this slice.

Disable duplicate submission and editing during a pending save. Ordinary typing or leaving the page does not persist changes. On focus/background refresh, an edited draft remains intact. After confirmed save, the returned values become the clean baseline. A failed save retains the draft. If a response is lost, report an uncertain outcome, do not automatically repeat PUT, and offer an explicit read-only check of saved targets; show its result separately from the current draft. Adopting server values requires an explicit action. A failed follow-up refresh must not turn a confirmed save into a reported failed save.

Alternative: autosave each field. That changes the confirmed explicit-save flow, creates partial-state concerns and adds unnecessary request coordination.

### 6. Render diary consumption independently of target state

Keep daily and meal consumed totals from the existing diary API. Adapt `DiarySummary` to accept optional targets and render its consumption values without denominators, rails or remaining calories when targets are unavailable. Keep target query feedback distinct from diary query feedback:

- Successful `targets: null`: show "Noch keine Ziele festgelegt" and an accessible action navigating to `/targets`.
- Initial target loading: show a loading indication; do not imply targets are unset.
- Initial target read error: show a target-specific error/retry, while successful diary entries and consumption remain visible.
- Refresh error with cached targets: keep comparisons only with a visible potentially-outdated/error notice and retry.
- Configured targets: use the same set for any selected diary date; changes affect only comparisons.

For each positive target retain the existing capped horizontal progress rail and actual consumed value. For an individual zero target show its numeric comparison and omit its percentage rail; do not divide by zero or substitute 1. Remaining calories is still `target - consumed`: at zero target and zero consumption show zero remaining; positive consumption against zero is over target. Use existing diary display formatting and replace "Beispielziel" accessibility wording with actual target wording. Remove `diaryExample.ts` when its sole remaining export is unused; do not retain fallback examples.

Alternative: wait for both target and diary requests before showing the diary. That makes an optional comparison feature block the main tracking use case.

## Risks / Trade-offs

- Global current targets also change comparisons on past days -> intentional and confirmed; test that food snapshots, entries and consumed totals do not change.
- A singleton without identity is local-application-wide -> document this boundary and defer user ownership until authentication is explicitly introduced.
- Concurrent initial saves could create duplicates or mixed values -> fixed-key constraint and one complete upsert, verified with separate contexts/concurrent requests.
- Zero and unset can be conflated -> represent unset with null/absence, require explicit numeric fields, and manually check all-zero and mixed-zero UI states.
- Refetches can replace drafts or newer cached values -> protect dirty drafts and cancel obsolete target reads on successful save.
- Numeric JavaScript transport has existing precision limits -> retain exact backend persistence/API values, avoid display rounding in edit paths, and document rather than promise arbitrary decimal precision in mobile.
- API transport error strings mention foods -> provide scoped target-specific messaging, including uncertain PUT outcomes.

## Migration Plan

1. Implement domain/application behavior and the additive mapping/migration, with no seed row. Add a lightweight ADR recording the local singleton boundary and all-date comparison policy during implementation.
2. Apply the committed migration explicitly using the existing development process; do not introduce startup migrations. Verify upgrading a database with existing diary data preserves all existing data and leaves targets unset.
3. Add the new endpoints and integrate targets/diary presentation. Existing entries need no backfill, and former example targets are never migrated into personal settings.
4. Validate backend behavior against PostgreSQL and manually verify the frontend through the existing `http://localhost:8081` environment if reachable, plus required scoped formatting/lint/type checks. Report unavailable runtimes/platforms accurately.
5. Application-code rollback can leave the additive table in place. A schema downgrade removes target settings; only exercise that destructive path on disposable test databases. Existing food/diary data must survive migration rollback and reapplication.
