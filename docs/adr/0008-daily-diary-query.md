# ADR 0008: Daily diary read access and capture order

- Status: Accepted
- Date: 2026-11-02

## Context

Users can log foods by quantity or serving and need to retrieve all entries for a
selected calendar day. Entries already preserve historical food, serving and
consumed nutrition snapshots. Within each meal, users want capture order rather
than alphabetical order; no capture timestamp was previously stored.

## Decision

Application owns GetDiaryDayQuery, its handler, immutable result records and
IDiaryQueries.GetDayAsync. Infrastructure implements the focused read interface
with an untracked EF projection filtered by DateOnly. This follows the existing
FoodQueries boundary and adds no persistence dependency to Application.

GET /diary-entries requires a date query parameter, documented as yyyy-MM-dd. It
returns the requested date and a flat entries array without pagination or totals.
Every entry carries its meal, original food ID/name/brand, consumed quantity and
unit, consumed nutrients, optional serving count and snapshotted serving details.
Empty days return 200 with an empty array. Past and future dates are accepted;
there is no clock-based date default or time zone conversion. Missing or invalid
dates use the existing 400 Problem Details binding behavior; technical errors use
the existing generic 500 response. The API maps separate response DTOs and rounds
only consumed nutrients through NutritionRounding.ForDisplay. Stored and
Application values remain exact. Repeated bookings are never merged.

All display data comes from diary snapshots, with no catalogue joins or nutrient
recalculation. Food and serving IDs may therefore refer to deleted catalogue data.

Infrastructure adds the EF shadow property CreatedAt (DateTimeOffset), mapped to
non-null created_at of PostgreSQL type timestamp with time zone and generated on
INSERT by CURRENT_TIMESTAMP. This is persistence metadata, not a domain rule or a
new API field. Compared with adding a domain timestamp and passing an application
clock into entry creation, this keeps domain creation independent of capture
metadata and uses one database clock for writes.

Ordering is MealType (breakfast, lunch, dinner, snacks), CreatedAt ascending and
entry ID ascending. PostgreSQL CURRENT_TIMESTAMP is the transaction timestamp;
entries created in the same transaction may tie and then use ID order.

The DiaryEntryCaptureTime migration backfills existing entries with the same
migration transaction timestamp. Their original order is unknowable; ID order
within each meal is the explicit fallback. The migration preserves every other
column. Rollback drops only the timestamp, losing capture-order metadata but
retaining diary content. Migrations remain explicitly applied, never on startup.

## Consequences

No Domain model, new package, authentication, additional index or write-endpoint
change is required. Daily totals and mobile UI remain separate slices. Query and
HTTP tests cover exact dates, ordering, snapshots, decimal precision, rounding,
empty results, errors and cancellation. Real PostgreSQL migration tests cover
backfill, generated timestamps and data preservation across rollback/reapplication.
