# ADR 0001: Food aggregate and nutritional reference basis

- Status: Accepted
- Date: 2026-10-21

## Context

The first catalogue model must represent foods and branded products with valid,
explicit nutritional information. Recipes, serving sizes, ownership, imports and
diary entries are later use cases. The domain must remain independent of persistence
and transport frameworks.

## Decision

`Food` is an aggregate root with a typed `FoodId` wrapping a non-empty GUID.
Names and brands are descriptive, not unique identifiers. Generic entity and
aggregate-root bases support typed identities, while value objects compare their
components and concrete types. A shared GUID identity would require fewer types;
typed IDs are chosen to prevent accidental interchange of different entity IDs.

A food requires a trimmed name of 1–200 characters and allows an optional trimmed
brand of up to 200 characters. An empty brand becomes null. Internal whitespace
and casing are preserved.

Every food has immutable nutritional values per exactly 100 grams or 100 milliliters.
Kilocalories, protein grams, carbohydrate grams and fat grams are required,
nonnegative decimals. Zero is a known value. Values are preserved without rounding,
upper bounds, macro-sum validation or reconciliation of calories with macros.
Grams and milliliters are separate bases and cannot be converted automatically.
An arbitrary reference quantity would support label-specific amounts but add
validation and calculation concerns that are unnecessary for the first slice.

Factories return `Result<T>` with either a valid value or the first `DomainError`,
identified by a stable code and description. Food validation checks name, brand,
unit and presence of nutrition in that order. Nutrition validation checks
kilocalories, protein, carbohydrates and fat in that order. Constructors and
get-only properties prevent callers from bypassing those checks.

## Consequences

Only creation and reading are supported. No domain events, persistence mappings,
application handlers or APIs are introduced. The domain has no package dependencies.
Expected validation failures do not throw; accessing the value of a failed result
or passing null to a result factory is a programming error and does throw.

Portions and recipes can be introduced with concrete use cases. Rounding will be
defined centrally when nutritional calculations are added. Historical diary accuracy
and snapshots must be addressed before catalogue updates and diary logging are
implemented together.
