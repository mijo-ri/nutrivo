# API guide

Start PostgreSQL, configure the connection and apply migrations using the
[root setup guide](../README.md#local-setup). Examples use the HTTP launch profile
at `http://localhost:5097`. All commands below run from the repository root.
Swagger UI is available at [localhost:5097/swagger](http://localhost:5097/swagger),
with the OpenAPI document at `/openapi/v1.json`, in Development only.

For phone and Web connectivity, see the [mobile setup guide](../mobile/nutrivo-app/README.md).
Migration and capture-time details are in the [development guide](development.md#working-with-migrations).

## Create a food

With the API running under the default HTTP launch profile, send:

```sh
curl --fail-with-body http://localhost:5097/foods \
  --header 'Content-Type: application/json' \
  --data '{
    "name": "Rolled oats",
    "brand": "Example",
    "referenceQuantity": 100,
    "referenceUnit": 1,
    "kilocalories": 372,
    "proteinGrams": 13.5,
    "carbohydratesGrams": 58.7,
    "fatGrams": 7
  }'
```

The response is `201 Created` with `{ "id": "<uuid>" }` after the food has been
committed to PostgreSQL. The response body remains ID-only and no Location header is returned.
Retrieve saved details with `GET /foods/{id}`.

`referenceUnit` is `1` for grams or `2` for milliliters. `referenceQuantity` is
required and may be any positive decimal. The four nutritional values refer to
that entire quantity. All fields except `brand` and `servingSizes` must be present.
Nutrient values must be nonnegative numbers; explicit zeros are accepted, while
missing or null nutrient values are rejected. Names and brands are trimmed, and
an empty brand becomes null. Foods with identical data may be created independently.

A food can include multiple freely named serving sizes calculated from its common
nutrition basis. For example, create an egg with nutrition per 100 g and two sizes
(the nutrition and weights below are illustrative user inputs):

```json
{
  "name": "Ei",
  "referenceQuantity": 100,
  "referenceUnit": 1,
  "kilocalories": 150,
  "proteinGrams": 12,
  "carbohydratesGrams": 1,
  "fatGrams": 10,
  "servingSizes": [
    { "quantity": 1, "unit": "Stück", "label": "Klein", "equivalentQuantity": 45 },
    { "quantity": 2, "unit": "Stück", "label": "Mittel", "equivalentQuantity": 116 }
  ]
}
```

Each serving requires positive `quantity` and `equivalentQuantity` numbers and a
nonblank free-text `unit`. `label` is optional. Unit and label are trimmed and
limited to 200 characters; a blank label becomes null. `equivalentQuantity` uses
the food's reference unit and describes the **entire** serving quantity: the two
medium eggs above weigh 116 g together. The API does not infer weights or convert
grams to milliliters. Units such as `Scheibe` and `Esslöffel` are also free text.
Omitting `servingSizes` (or supplying null or an empty array) creates no servings;
null entries inside the array are rejected.

Calculated nutrition is `basis nutrient × equivalentQuantity / referenceQuantity`.
The two medium eggs above therefore contain 174 kcal, 13.92 g protein, 1.16 g
carbohydrates and 11.6 g fat together. Basis values and all input quantities are
stored exactly. Calculations use decimal arithmetic without display rounding;
serving nutrients in HTTP responses are rounded to two decimal places with
midpoint ties away from zero. A serving whose calculated nutrition exceeds the
decimal range is rejected during creation with
`Food.ServingSize.Nutrition.OutOfRange`; no part of the food is saved.

Invalid inputs return `400 Bad Request` as `application/problem+json`. Domain
validation responses include a stable `code` and a human-readable `detail`, for
example `Food.Name.Required`. Malformed or incomplete JSON also returns Problem
Details. Technical storage failures return `500` without internal error details.
The endpoint does not require authentication.

## Search foods

```sh
curl --fail-with-body --get http://localhost:5097/foods \
  --data-urlencode 'search=oats' \
  --data-urlencode 'page=1' \
  --data-urlencode 'pageSize=20'
```

`GET /foods` searches the stored name or optional brand for a case-insensitive,
literal substring. Leading and trailing whitespace is trimmed. A missing, empty
or whitespace-only `search` lists all foods. Single-character searches are valid;
multiple words remain one contiguous search text. `%`, `_` and backslashes are
ordinary characters. There is no fuzzy matching, accent transliteration or
external lookup.

Results are sorted case-insensitively by name, then brand (null treated as empty),
using the database collation, then by ID. `page` defaults to 1 and must be at least
1. `pageSize` defaults to 20 and must be between 1 and 100. The computed offset
must fit in a signed 32-bit integer. Ordering is stable while the data is unchanged;
the count and page are separate database reads without a shared snapshot.

The `200 OK` response contains `items`, `page`, `pageSize` and `totalCount` (the
number of all matching foods, before pagination). Each item includes `id`, `name`,
`brand`, `referenceQuantity`, `referenceUnit`, `kilocalories`, `proteinGrams`,
`carbohydratesGrams`, `fatGrams` and `servingSizes`. Basis nutrition retains its
exact decimal values and refers to `referenceQuantity` grams (`referenceUnit: 1`)
or milliliters (`referenceUnit: 2`). Each serving includes its generated `id`,
`quantity`, `unit`, `label`, `equivalentQuantity` and the same four nutrient fields
calculated for that serving, rounded as described above. Servings retain input
order. Foods without servings return `servingSizes: []`. Portions do not change
food pagination or name/brand matching.
An empty database, no matches or a page beyond the results returns an empty
`items` array. A page beyond the results still reports the matching `totalCount`.

Invalid page values or offset overflow return `400 Problem Details` with code
`SearchFoods.Page.Invalid`; invalid page sizes use `SearchFoods.PageSize.Invalid`.
Malformed numeric parameters also return `400 Problem Details` through request
binding. Technical storage failures return a generic `500` without internal
details. Search requires no authentication and does not change stored foods.

## Food details and updates

`GET /foods/{id}` returns a single food in the same shape as a search item,
including persisted serving IDs and input ordering. Basis nutrition is exact;
serving nutrition uses the same two-decimal rounding as search. Invalid GUIDs or
an empty GUID return `400 Problem Details`; an unknown ID returns `404` with code
`GetFoodById.NotFound`. Reads do not track or mutate entities.

`PUT /foods/{id}` replaces the editable food fields using the same fields as
creation. Serving inputs additionally accept an optional `id`: existing IDs are
preserved, missing IDs create new servings, and omitted servings are deleted.
Missing, null or empty serving lists remove all servings. The request defines
their order. Changing the reference basis performs no automatic conversion.
Success returns `200` with `{ "id": "..." }`; validation errors return `400`,
and an unknown food returns `404` with code `UpdateFood.NotFound`. Historical
diary snapshots remain unchanged. The mobile details screen opens a prefilled
edit form and refreshes details and catalogue queries after saving.

## Read a diary day

After logging food through `POST /diary-entries`, retrieve a selected calendar day:

```sh
curl --fail-with-body --get http://localhost:5097/diary-entries \
  --data-urlencode 'date=2026-11-02'
```

`date` is required and documented as `yyyy-MM-dd`. It selects the exact stored
calendar date without time zone conversion or a server-side "today" default.
Past and future days are supported. The response is `200 OK`, for example:

```json
{
  "date": "2026-11-02",
  "entries": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "mealType": 1,
      "foodId": "22222222-2222-2222-2222-222222222222",
      "name": "Rolled oats",
      "brand": "Example",
      "quantity": 75,
      "referenceUnit": 1,
      "kilocalories": 279,
      "proteinGrams": 10.13,
      "carbohydratesGrams": 44.03,
      "fatGrams": 5.25,
      "servingCount": null,
      "servingSize": null
    }
  ],
  "totals": {
    "kilocalories": 279,
    "proteinGrams": 10.13,
    "carbohydratesGrams": 44.03,
    "fatGrams": 5.25
  },
  "mealTotals": [
    {
      "mealType": 1,
      "totals": {
        "kilocalories": 279,
        "proteinGrams": 10.13,
        "carbohydratesGrams": 44.03,
        "fatGrams": 5.25
      }
    },
    {
      "mealType": 2,
      "totals": {
        "kilocalories": 0,
        "proteinGrams": 0,
        "carbohydratesGrams": 0,
        "fatGrams": 0
      }
    },
    {
      "mealType": 3,
      "totals": {
        "kilocalories": 0,
        "proteinGrams": 0,
        "carbohydratesGrams": 0,
        "fatGrams": 0
      }
    },
    {
      "mealType": 4,
      "totals": {
        "kilocalories": 0,
        "proteinGrams": 0,
        "carbohydratesGrams": 0,
        "fatGrams": 0
      }
    }
  ]
}
```

The flat list is sorted by meal (breakfast `1`, lunch `2`, dinner `3`, snacks `4`),
then by capture time oldest first, then by entry ID for ties. Repeated bookings
remain separate entries and each contributes to the totals. There is no pagination.
The `totals` object contains daily kilocalories, protein, carbohydrates and fat;
`mealTotals` always contains all four meals in the order above, each with its own
`totals` object. Meals without entries have zero totals. Days without entries return
the requested `date`, `entries: []`, zero daily totals and four zero meal totals.

Totals use the exact stored consumed nutrients from the same read as the entries,
without catalogue lookups or recalculating food quantities. Daily and meal values
are summed before display rounding. For example, two entries of 0.005 kcal each
display as 0.01 kcal each but have a total of 0.01 kcal. If they belong to different
meals, each meal displays 0.01 kcal while the day still displays 0.01 kcal. Thus
displayed daily totals may differ from the sum of displayed entries or meal totals.

Names, brands and serving details come from historical snapshots, so catalogue
changes or deletion do not alter the diary. Nutrition refers to the consumed
quantity, rounded only for the HTTP response to two decimal places with midpoint
ties away from zero. Quantities and stored nutrients retain their exact values.
`referenceUnit` is grams (`1`) or milliliters (`2`). A serving booking also returns
`servingCount` and a `servingSize` object with `id`, `quantity`, `unit`, optional
`label` and `equivalentQuantity`; the last field describes the entire defined
serving in the entry's reference unit. Direct bookings return null for both
serving fields. `foodId` and serving IDs identify the originally logged catalogue
items, which may no longer exist.

Missing or invalid dates return `400 Problem Details` through request binding.
If any daily or meal nutrient total exceeds the supported decimal range, the whole
request returns `422 Problem Details` with code `GetDiaryDay.Totals.OutOfRange`,
without partial results or changes to stored entries.
Technical failures return a generic `500 Problem Details`. The query requires no
authentication and does not modify entries.
