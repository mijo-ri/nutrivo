# ADR 0010: Food updates and stable serving identities

- Status: Accepted
- Date: 2026-11-08
- Amends: ADR 0001, ADR 0005 and ADR 0007

## Decision

PUT /foods/{id} replaces the name, optional brand, reference quantity/unit,
nutrition and ordered serving list. It follows CreateFood's validation and
Problem Details conventions, returning 200 with the food ID, 400 for invalid
input and 404 for an unknown food. All fields remain editable; changing the
reference unit performs no automatic conversion. Submitted nutrition and serving
equivalents describe the new basis. Concurrent edits use last successful write,
without a version token.

An optional serving ID selects an existing serving of that food. Missing or null
IDs create servings with server-generated identities; omitted servings are removed.
Missing, null and empty lists all remove every serving. Empty, duplicate and
foreign IDs are rejected. Replacing all serving identities would simplify the
write contract but unnecessarily invalidate existing references and selections.

Food validates the full candidate before mutating itself or its existing serving
entities. Creation and updates share validation, normalization and nutrition
calculations. Identity remains stable. Diary entries retain their independent
food, serving and nutrition snapshots; updates do not rewrite diary history.

IFoodRepository retains its untracked lookup for logging and adds a tracked lookup
and an explicit commit for updates. The tracked lookup begins a transaction and
locks the food row before loading its current servings, serializing concurrent
writers without introducing version tokens. Rejected requests release the lock
when their DbContext is disposed. The commit uses that transaction: existing
serving positions move temporarily to negative values, EF's original position
values are aligned with that intermediate state, and final food/serving changes
are saved before commit. This avoids collisions and EF dependency cycles on the
existing unique (food_id, position) index during swaps, removals and additions.
Failures roll back the entire transaction. No schema migration or new dependency
is required.

## Consequences

The shared mobile form carries optional serving IDs only for updates; creation
continues to omit IDs. Successful updates invalidate detail and catalogue queries.
Form state survives background reads and failed saves. Deletion remains unavailable.
Domain, application and PostgreSQL/API tests cover validation, stable identities,
ordering, exact decimals, rollback and snapshot preservation.
