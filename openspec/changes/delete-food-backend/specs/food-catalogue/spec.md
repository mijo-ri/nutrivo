# Spec Delta

## Purpose

Manage food availability in the nutrition catalogue while keeping previously recorded diary history independent of catalogue deletion.

## ADDED Requirements

### Requirement: Permanently delete a catalogue food

The system SHALL expose `DELETE /foods/{id}` without a required request body. For an existing food, it SHALL permanently remove that food and all its catalogue serving sizes atomically and return `204 No Content` with an empty response body only after deletion commits. Prior diary usage SHALL NOT prevent deletion.

#### Scenario: Delete an unused food without servings
- **GIVEN** an existing food without catalogue serving sizes or diary usage
- **WHEN** a client deletes that food
- **THEN** the response is `204 No Content` with no response body
- **AND** the food no longer exists in the catalogue

#### Scenario: Delete a previously logged food with servings
- **GIVEN** a food with multiple catalogue serving sizes and existing diary entries
- **WHEN** a client deletes that food
- **THEN** the response is `204 No Content`
- **AND** the food and all of its catalogue serving sizes are permanently removed
- **AND** unrelated catalogue foods and their serving sizes remain unchanged

### Requirement: Preserve historical diary data

Deleting a catalogue food SHALL preserve every existing diary entry referring to it, including entry identity, date, meal, quantities, food and serving snapshots, and consumed nutrition. Daily diary responses, daily totals and meal totals SHALL remain unchanged by catalogue deletion.

#### Scenario: Preserve direct and serving-based entries across days
- **GIVEN** entries for a food recorded by direct quantity and by serving size on multiple dates
- **WHEN** the food is deleted through `DELETE /foods/{id}`
- **THEN** all entries and their persisted snapshot values remain unchanged
- **AND** querying each affected diary day returns the same entries in the same order, daily totals and meal totals as before deletion
- **AND** other diary entries remain unchanged

### Requirement: Deleted foods are unavailable for subsequent catalogue use

After deletion commits, subsequent food search and detail reads SHALL NOT return the deleted food. Food updates and logging requests whose food lookup begins after deletion commits SHALL treat its ID as missing and SHALL NOT recreate the food or save a new diary entry for it.

#### Scenario: Search and detail after deletion
- **GIVEN** a successfully deleted food
- **WHEN** a client searches the catalogue and requests `GET /foods/{id}` for that food
- **THEN** search results exclude the food and the search count reflects its removal
- **AND** the detail request returns `404` with the existing `GetFoodById.NotFound` Problem Details code

#### Scenario: Logging or updating a deleted food
- **GIVEN** a successfully deleted food
- **WHEN** a new valid logging request or food update request looks up that food
- **THEN** logging returns `404` with `LogFood.Food.NotFound` or updating returns `404` with `UpdateFood.NotFound`, respectively
- **AND** no new diary entry or catalogue food is created

### Requirement: Report invalid and missing deletion targets consistently

The delete endpoint SHALL return `400` Problem Details for malformed or empty food IDs and `404` Problem Details with code `DeleteFood.NotFound` for unknown or already deleted IDs. Empty UUIDs SHALL use code `Food.Id.Empty`. Rejected requests SHALL leave catalogue and diary data unchanged.

#### Scenario: Malformed ID
- **WHEN** a client requests `DELETE /foods/not-a-guid`
- **THEN** the response is `400` with media type `application/problem+json`
- **AND** no catalogue or diary data is changed

#### Scenario: Empty UUID
- **WHEN** a client requests deletion with the all-zero UUID
- **THEN** the response is `400` Problem Details with code `Food.Id.Empty`
- **AND** no catalogue or diary data is changed

#### Scenario: Unknown or already deleted UUID
- **WHEN** a client requests deletion of a valid UUID that does not identify an existing food
- **THEN** the response is `404` Problem Details with code `DeleteFood.NotFound`
- **AND** no catalogue or diary data is changed

#### Scenario: Concurrent deletion requests
- **GIVEN** an existing food
- **WHEN** two deletion requests for that food complete successfully at the storage level
- **THEN** exactly one request returns `204` and the other returns `404` with code `DeleteFood.NotFound`
- **AND** the food and its catalogue serving sizes are absent while diary history is preserved

### Requirement: Handle deletion storage failures without partial changes

An unexpected deletion storage failure SHALL use the existing generic `500` Problem Details response without exposing storage exception details. A failed delete statement SHALL leave the food, its catalogue serving sizes and diary history intact.

#### Scenario: Storage rejects the deletion
- **GIVEN** storage fails before committing the delete statement
- **WHEN** a client requests deletion
- **THEN** the response is generic `500` Problem Details
- **AND** no internal exception details are exposed
- **AND** the food, serving sizes and diary entries retain their previous values

### Requirement: Document the deletion API

The generated OpenAPI document SHALL describe `DELETE /foods/{id}` with operation ID `DeleteFood`, the Foods tag, a required UUID path parameter, and response statuses `204`, `400`, `404` and `500`. Its description SHALL explain permanent deletion and preservation of diary snapshots.

#### Scenario: Inspect the deletion contract
- **WHEN** a client retrieves the development OpenAPI document
- **THEN** the deletion operation documents its parameter, all response statuses, and the effects on catalogue servings and diary history
- **AND** the successful response has no response-body schema
