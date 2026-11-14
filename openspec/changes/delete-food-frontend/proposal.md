# Proposal

## Why

The backend already supports permanent food catalogue deletion, but the food details screen still disables its delete action. Users need to remove catalogue foods from the app with explicit confirmation while retaining all previously recorded diary data.

## What Changes

- Activate deletion on the food details screen with a confirmation dialog naming the food, explaining permanent deletion and preservation of existing diary entries, and offering cancel and confirm actions on Web, iOS and Android.
- Connect a non-retrying delete mutation to the existing `DELETE /foods/{id}` endpoint, including correct handling of its empty `204 No Content` response.
- Prevent duplicate submissions, display progress and actionable failures, and handle foods already deleted elsewhere without leaving usable stale details.
- On confirmed deletion, return to the food catalogue and reconcile detail and paginated search caches so deleted foods cannot remain selectable.
- Allow `DELETE` through the existing development CORS policy for configured Web origins and extend its backend integration coverage.
- Document manual validation; keep frontend validation to formatting, linting, TypeScript and manual checks.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `food-catalogue`: Add the user-facing confirmation, request lifecycle, catalogue cache consistency and development Web access requirements for the existing deletion API.

## Impact

- Mobile: `FoodDetailsScreen.tsx`, a focused food deletion dialog, `foodsApi.ts`, `foodsQueries.ts`, and the shared `apiClient.ts`; nearby catalogue UI only if necessary for stale data handling.
- API: development CORS methods in `src/Nutrivo.Api/Program.cs`; integration cases in `tests/Nutrivo.IntegrationTests/Endpoints/DevelopmentAccessTests.cs`.
- Documentation: mobile API/CORS guidance and manual deletion checks in `docs/mobile.md`.
- Existing backend deletion contract, domain behavior, database schema and diary snapshots remain as specified. No new dependencies, automated frontend test infrastructure, undo/restore, diary-entry deletion or additional deletion entry points are included.
