# ADR 0005: Food nutrition basis and derived serving sizes

- Status: Accepted
- Date: 2026-10-25
- Amends: ADR 0001, ADR 0002, ADR 0003 and ADR 0004

## Context

A food such as an egg needs multiple selectable sizes within one catalogue entry.
The existing model only accepts nutrition per exactly 100 grams or milliliters.
Users want free-text serving units and optional size labels, with serving nutrition
calculated from a common food basis rather than entered independently.

## Decision

Keep Food as the aggregate root. Its reference quantity becomes an explicit,
positive decimal, while ReferenceUnit remains Gram or Milliliter. The four basis
nutrients retain their existing nonnegative decimal rules and exact storage.

Food owns an ordered, read-only collection of ServingSize entities with typed
ServingSizeId identities. Each serving has a positive quantity, a required free-text
unit, an optional label and a positive equivalent quantity in the food's reference
unit. Unit and label are trimmed and limited to 200 characters; blank labels become
null. The equivalent describes the entire serving quantity, so two medium eggs
require their combined weight. No predefined egg sizes or weights are inferred.

A food may have no servings. Alternatively, creating separate foods for each size
would keep a single reference per food, but would not support the requested size
selection within one food. Independently entering each variant's nutrients would
allow different compositions, but users chose a shared basis and weight/volume
conversion instead. Food variants with different compositions must remain separate
foods. There is no implicit gram/milliliter conversion or density model.

Each nutrient is scaled by equivalentQuantity / referenceQuantity. Domain arithmetic
uses decimal, and creation validates all serving calculations before persistence.
Arithmetic overflow becomes a stable DomainError through Result, with no partial
write. Calculation tries the other operation order when an intermediate result
would overflow; fractional bases divide first to reduce product underflow.
Input quantities and basis nutrients are never rounded. Only calculated nutrients
at the HTTP response boundary use the central NutritionRounding policy: two decimal
places and MidpointRounding.AwayFromZero. The application read model stays unrounded.

POST /foods requires referenceQuantity and accepts an optional servingSizes list.
Missing, null or empty lists mean no servings; null entries are invalid. Serving IDs
are generated server-side. GET /foods returns the common basis plus every serving's
inputs, ID and calculated nutrients in input order. Search remains a name/brand
substring match and pagination counts foods rather than servings. API DTOs stay
separate from domain entities, and existing Result/Problem Details contracts remain.

Map servings to public.food_serving_sizes with a Food foreign key, cascading delete
and a unique position per Food. Quantities use unconstrained PostgreSQL numeric,
as do nutrients. The aggregate is committed in one SaveChangesAsync call. Read
queries project only the requested page and calculate serving nutrition in memory
through the Domain, avoiding PostgreSQL/.NET arithmetic differences.

The migration adds reference_quantity with 100 for existing foods, then removes the
migration-only column default. Existing identities, units and nutrient values stay
unchanged; existing foods initially have no servings. Down locks the affected tables
and rejects rollback if any serving or non-100 basis exists. Otherwise, removing the
new schema preserves the previous nutritional interpretation. Migration application
remains explicit.

## Consequences

The CreateFood request changes intentionally: clients must provide referenceQuantity.
There is no legacy request adapter, mobile UI, edit endpoint, diary logging or new
package dependency in this slice. Future diary entries must snapshot the chosen
serving and nutritional data before catalogue edits become available. Rounded API
values must not become the source for authoritative diary calculations.

Domain and application tests cover inputs, arithmetic, ordering and atomic creation.
PostgreSQL/API integration tests cover exact round-trips, serving IDs and order,
paginated reads, rounding, legacy migration and rollback guards.
