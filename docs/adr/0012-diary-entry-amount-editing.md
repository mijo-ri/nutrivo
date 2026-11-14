# ADR 0012: Edit historical diary amounts

- Status: Accepted
- Date: 2026-11-14

## Decision

An existing DiaryEntry changes only its direct quantity or original serving count.
Its identity, date, meal, capture time and immutable food/serving snapshots remain
unchanged. Direct and serving modes cannot be switched during editing.
The domain validates and calculates all new consumed values before mutating them.
Calculations always scale the historical reference nutrients with decimal precision;
they never scale rounded display values or consult the current catalogue.

A dedicated detail read exposes the original reference basis for the shared mobile
amount form. Updates and deletions commit before returning success. Updates use EF
tracking; missing rows after concurrent deletion return not-found rather than being
recreated. Concurrent edits use last committed write wins for this local slice.

## Alternatives

Rebuilding from the catalogue would silently replace historical nutrition and fail
after food deletion. Replacing an entry with a newly created row would change its
identity and capture order. Allowing other servings while editing would require a
separate policy for reconciling historical and current serving definitions.

## Consequences

Deleted catalogue foods remain editable in the diary. No schema migration, new
dependency or aggregate is required. The mobile form shares layout and previews
between creation and editing, with editing limited to the original amount mode.
