# ADR 0011: Permanent food catalogue deletion

- Status: Accepted
- Date: 2026-11-11
- References: ADR 0007, ADR 0008, ADR 0010

## Decision

DELETE /foods/{id} permanently deletes a catalogue food even when it has been
logged. Existing PostgreSQL cascade deletion removes its serving sizes. Diary
entries retain their food, serving and nutrition snapshots, including original
catalogue IDs, without joins or recalculation. No snapshot foreign keys or schema
changes are introduced.

A focused IFoodRepository.DeleteAsync operation executes one filtered delete
statement and reports whether a row was removed. PostgreSQL makes that statement
and its cascade atomic and coordinates it with existing catalogue update locks.
The operation runs in a fresh request scope without tracked foods or an existing
transaction. Success is committed before returning 204 with no body. A missing
or already deleted food returns 404 with DeleteFood.NotFound; invalid IDs return
400. Technical failures use the existing generic 500 Problem Details response.

Soft deletion would require filters and restoration behavior. Rejecting used foods
would prevent the confirmed use case. Neither is needed because diary snapshots
already own their historical data.

## Consequences

New food lookups after deletion cannot select, update or log that food. A logging
request that read the food before deletion may still save its independent snapshot
afterward; the existing logging transaction boundary is unchanged. There is no
application undo. Reverting the endpoint cannot restore deleted catalogue data.

Repository and API tests verify serving cleanup, snapshot and totals preservation,
concurrent writes and statement rollback. Frontend integration remains a later
slice; mobile code and development CORS stay unchanged.
