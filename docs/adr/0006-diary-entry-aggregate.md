# ADR 0006: Diary entry aggregate and historical snapshots

- Status: Accepted
- Date: 2026-10-25

## Context

Users need to record foods for breakfast, lunch, dinner and snacks on a selected
calendar day. Foods already define nutrition per an arbitrary gram or milliliter
basis and may own named serving sizes. Diary history must remain accurate when
catalogue data changes. This slice models entries without persistence or transport.

## Decision

Each DiaryEntry is an aggregate root with a typed, non-empty DiaryEntryId. A daily
NutritionDiary aggregate would centralize rules spanning several entries, but would
also couple otherwise independent changes. There are no such cross-entry rules yet,
so a future daily query will group independent entries by date and meal instead.
Neither aggregate represents a user's entire diary history.

An entry has an explicit DateOnly and one of Breakfast, Lunch, Dinner or Snacks.
Past and future dates are allowed; the domain does not consult a clock or infer
dates from time zones. Repeated foods within the same meal remain separate entries.
There is no user identity, meal entity, custom meal category or meal time yet.

Create accepts a positive decimal quantity and an explicit ReferenceUnit matching
the food. Grams and milliliters cannot be converted automatically. CreateFromServing
resolves a ServingSizeId within the selected food and accepts a positive decimal
serving count. Count multiplies the entire defined serving: three servings of
"2 eggs = 116 g" mean six eggs and 348 g; half a serving means one egg and 58 g.
The entry retains the serving count as well as the equivalent consumed base quantity.

Every entry captures an immutable FoodSnapshot containing food ID, name, optional
brand, reference quantity, reference unit and reference nutritional values. A serving
entry also captures an immutable ServingSizeSnapshot containing serving ID, quantity,
unit, optional label and equivalent quantity. Snapshots hold no Food or ServingSize
entity references. Existing immutable nutritional values and typed IDs can be shared
safely. Serving position is catalogue presentation data and is not snapshotted.

Consumed nutrition is calculated from the original reference values through
NutritionalValues.Scale(consumedQuantity, referenceQuantity) and retained on the
entry. Decimal precision is preserved; NutritionRounding remains an output concern.
Rounded API serving nutrition must never be used as input to diary calculations.

Factories return Result<DiaryEntry> with stable DiaryEntryErrors for missing inputs,
invalid enum values, nonpositive quantities/counts, unit mismatch, foreign servings
and arithmetic failures. Serving quantity overflow or underflow to zero fails with
DiaryEntry.Quantity.OutOfRange. Nutrition overflow maps to DiaryEntry.Nutrition.OutOfRange
rather than exposing the existing food-serving error code. No failed factory produces
an entry. Entry and snapshot properties are get-only and constructors are restricted.

## Consequences

The domain gains no package dependencies. Domain tests exercise direct and serving
input, fractional amounts, calendar dates, snapshots, typed identity, independent
repeated entries and arithmetic/validation failures without a database.

Persistence mappings, migrations, LogFood handlers, APIs, daily queries/totals,
editing/deletion, targets, identity and mobile UI remain separate slices. Future
persistence must store snapshot values and consumed nutrition rather than rebuilding
historical entries from current food or serving data.
