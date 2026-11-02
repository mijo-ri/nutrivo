# ADR 0009: Daily and meal nutrition totals

- Status: Accepted
- Date: 2026-11-02

## Context

Users need daily calorie and macronutrient totals as well as totals for breakfast,
lunch, dinner and snacks. ADR 0008 introduced a daily read with exact consumed
nutrition snapshots but explicitly deferred totals. Summaries must preserve that
historical accuracy and agree with the entries read in the same request.

## Decision

Extend GET /diary-entries?date=yyyy-MM-dd with totals and mealTotals. Both contain
kilocalories, proteinGrams, carbohydratesGrams and fatGrams. Each mealTotals item
contains mealType and a totals object. Always return all four meals in enum order,
including zero totals for meals without entries. An empty day returns an empty
entries array, zero daily totals and four zero meal totals.

IDiaryQueries.GetDayAsync now returns immutable projected entries rather than the
final GetDiaryDayResult. Infrastructure retains the single untracked, date-filtered
and ordered EF query from ADR 0008. Application's GetDiaryDayQueryHandler builds
the result, summing the stored consumed nutrient fields across all entries and
within each meal. Repeated foods count separately. No catalogue joins, quantity
recalculation, extra database queries or persisted summaries are needed. This is
read-model aggregation and introduces no aggregate or domain invariant.

Application uses immutable DiaryNutritionTotals and DiaryMealTotals records and
unrounded decimal arithmetic. Separate API response DTOs round each entry, daily
total and meal total through NutritionRounding.ForDisplay: two decimal places,
midpoint ties away from zero. A daily total can therefore differ from the sum of
displayed entries or meals. For example, two 0.005 kcal entries have a daily total
of 0.01 kcal even when each entry or its separate meal displays 0.01 kcal.

Existing entries permit any nonnegative decimal nutrient value. If summation
overflows decimal, the handler returns a failed Result with stable code
GetDiaryDay.Totals.OutOfRange. The API returns 422 Problem Details for the entire
request, with no partial or clamped totals. Stored entries are unchanged. Storage
failures remain generic 500 responses; cancellation continues to propagate.

## Consequences

This supersedes ADR 0008's response without totals and its read-interface return
type. Date selection, ordering, historical snapshots, separate response DTOs and
output-only rounding remain in effect. No migration or new dependency is required.
Clients accepting additional response fields can continue consuming date/entries.

Application tests cover exact aggregation, meal assignment, repeated foods, empty
results, decimal limits, cancellation and storage failures. PostgreSQL/API tests
cover date isolation, direct and serving inputs, mixed units, historical accuracy,
rounding differences, zero totals, the 422 contract and OpenAPI schemas. Targets
and mobile presentation remain later slices.
