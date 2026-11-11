# Proposal

## Why

Foods can be created, searched, retrieved and updated, but unwanted catalogue entries cannot yet be deleted through the API. Users need permanent deletion even for previously logged foods, while their diary history and nutrition totals remain accurate.

## What Changes

- Add `DELETE /foods/{id}` for permanent deletion of a food and its catalogue serving sizes.
- Allow deletion regardless of existing diary usage; preserve all diary entries, food and serving snapshots, consumed nutrition and totals.
- Return `204 No Content` after committed deletion, `404` for an unknown or already deleted food, and `400` for an invalid food ID, using existing Problem Details conventions for failures.
- Ensure deleted foods disappear from catalogue reads and cannot be used by logging requests that look them up after deletion commits.
- Add application and PostgreSQL/API coverage, including deletion through the public endpoint and preservation of history.
- Scope this change to the backend. Mobile UI, client API calls, cache invalidation, frontend integration and frontend tests are excluded. Archiving, restoration, diary-entry deletion and authentication are also excluded.

## Capabilities

### New Capabilities

- `food-catalogue`: Catalogue food deletion, dependent serving cleanup, subsequent catalogue availability, and preservation of historical diary data.

### Modified Capabilities

None. No existing capability specifications are registered in this project.

## Impact

- API: a thin `DeleteFood` endpoint with OpenAPI metadata and existing error handling.
- Application: `DeleteFoodCommand`, its handler and errors, plus a focused deletion operation on `IFoodRepository`.
- Infrastructure: an atomic PostgreSQL-backed delete using the existing serving cascade; no schema migration or new dependency is expected.
- Tests: new deletion coverage and small updates to repository test doubles and handler/endpoint registration checks.
- Documentation: a lightweight ADR recording permanent catalogue deletion with snapshot preservation.
- Domain models, diary query contracts, the mobile project and development CORS configuration remain unchanged.
