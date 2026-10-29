# ADR 0007: LogFood command, endpoint and diary persistence

- Status: Accepted
- Date: 2026-10-29

## Context

DiaryEntry already validates direct quantities and serving counts and captures
historical snapshots. Users need to persist these entries through an application
command and an HTTP endpoint, following the CreateFood conventions.

## Decision

LogFoodCommand accepts a food UUID, an explicit DateOnly and MealType, and exactly
one complete amount: quantity with reference unit, or serving size UUID with count.
Incomplete and mixed amounts return LogFood.Amount.Invalid. Empty IDs use the
existing domain errors. The handler loads the food including its servings through
IFoodRepository.GetByIdAsync and delegates to DiaryEntry.Create or CreateFromServing.
Missing food returns LogFood.Food.NotFound; domain errors pass through unchanged.
No calculation or domain invariant is duplicated in the handler.

IDiaryEntryRepository.AddAsync commits before returning, matching IFoodRepository's
existing write contract. Both repositories are scoped and pass CancellationToken
to EF Core. Food lookup is untracked. No unit of work or new dependency is needed.

POST /diary-entries has separate request and response DTOs. Food ID, calendar date
and meal type are required JSON properties. Amount fields are nullable. Success
returns 201 with only the generated entry UUID and no Location until a GET exists.
Validation returns 400, missing food returns 404, and both use Problem Details with
a stable code and descriptive detail. Unexpected storage failures return generic
500 Problem Details. Existing handler and endpoint scanning discovers this slice.

The diary_entries table stores scalars, consumed nutrition and complete food and
optional serving snapshots in the same row. Snapshot IDs have no catalogue foreign
keys, so catalogue deletion cannot remove entries. Snapshot objects use OwnsOne;
food snapshot nutrition is nested owned data, while consumed nutrition uses the
existing complex-property approach. Each food snapshot copies reference nutrition
into its own value instance so EF ownership remains independent across entries.
All decimal columns use unconstrained PostgreSQL numeric, preserving domain values
without output rounding. Private scalar constructors support materialization while
public properties remain get-only. The migration is applied explicitly.

## Consequences

The date, meal, duplicate-entry and calculation rules from ADR 0006 remain in force.
Application tests verify orchestration and errors; PostgreSQL/API tests cover exact
roundtrips, snapshot independence after catalogue changes and deletion, binding,
HTTP contracts, and migrations. Daily queries, totals, editing, identity and mobile
UI remain separate slices.
