# ADR 0013: Current daily nutrition targets

- Status: Accepted
- Date: 2026-11-14

## Context

The local app needs configurable daily calorie and macro targets in place of
illustrative frontend values. Authentication and target history are outside this
slice. Changing a comparison basis must not change diary consumption or snapshots.

## Decision

Use one `DailyNutritionTarget` aggregate for the local application. Its four
independent decimal values have explicit units: kcal and grams. Each is required
and nonnegative; explicit zero is valid. Absence of the aggregate means unset and
is distinct from an all-zero set. No macro-energy equation is enforced.

Persist the current set under the fixed key 1, with a primary key and a database
check constraint limiting the table to that key. Replace all values in one atomic
upsert; the last committed write wins. Do not seed examples. Infrastructure owns
EF mappings, PostgreSQL numeric storage and SQL; the Domain has no framework
dependency. Use focused read/set application use cases and persistence methods.

The same current set applies to every displayed diary date, including past days.
Changes affect only comparisons; entries, snapshots and consumed totals retain
their existing semantics. Zero targets have numeric comparisons without percent
rails. Unset targets show consumption and an action to configure targets.

## Alternatives and consequences

Dated targets would preserve previous comparison settings but require effective
date rules and UI that are not needed yet. Per-user settings require an explicit
ownership design when authentication is introduced. The local singleton is not
a synthetic user identity and will need an intentional ownership migration then.

A generic settings dictionary weakens typed validation. Reusing food nutritional
values couples targets to food-specific errors and reference/scaling semantics;
a focused target aggregate avoids that coupling without a shared-model refactor.

The additive migration leaves existing foods and entries unchanged. Explicit
migration application remains required. Downgrading removes targets, so production
rollback should retain the additive table; migration rollback tests use disposable
databases.
