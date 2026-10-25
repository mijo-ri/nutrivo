# ADR 0004: Food search and read access

- Status: Accepted
- Date: 2026-10-25

## Context

Users need to find and select stored foods with their nutrition. The existing
write repository commits newly created Food aggregates. Application currently has
no EF Core dependency, and query handlers already have a discovery mechanism.

## Decision

Application owns SearchFoodsQuery, its handler, immutable result records and the
IFoodQueries interface for querying Food read models. Infrastructure implements
that interface as FoodQueries with EF Core and registers it as scoped. SearchAsync
is its first query; further methods are added only when a use case needs them.
The API maps its own response DTOs.
The handler validates pagination, trims search text and forwards cancellation.
Validation uses the existing Result and error contracts; search parameter errors
are defined in Application because they are not Food aggregate invariants.

Extending IFoodRepository would combine aggregate writes with a DTO read model.
Putting EF directly in the Application handler is viable, but would introduce a
persistence dependency solely for this query. A focused read interface preserves
the current boundaries without a generic repository or query framework.

Infrastructure uses parameterized PostgreSQL ILIKE for a literal, case-insensitive
substring of name or brand, escaping backslash, percent and underscore. Missing
or blank search lists all foods. Multiple words remain a contiguous substring.
The database filters, counts, sorts and paginates. Ordering uses lowercased name,
lowercased brand (null as empty), and ID under the existing database collation.
AsNoTracking and direct DTO projection avoid returning tracked aggregates.

GET /foods defaults to page 1 and page size 20; size is limited to 1 through 100.
Pages must be positive and the offset must fit in Int32, calculated with Int64
before conversion. Responses contain items, page, pageSize and totalCount.
An out-of-range page returns an empty array with the matching total. Each item
includes exact nutrition per the existing 100 gram/milliliter reference basis.

## Consequences

No Domain, schema, package or index changes are needed for the initial catalogue.
Substring scans are acceptable for the current local use case; indexing can be
revisited with measured data volumes. Count and items are separate reads without
a shared snapshot; concurrent writes can change results between reads or pages.
There is no fuzzy search, transliteration, tokenization, external provider,
authentication or Food detail endpoint in this slice.

Application tests use a recording read service. Integration tests use real
PostgreSQL migrations and the existing Testcontainers fixture, including escaped
matching, deterministic pages, DTO nutrition, cancellation and HTTP contracts.
