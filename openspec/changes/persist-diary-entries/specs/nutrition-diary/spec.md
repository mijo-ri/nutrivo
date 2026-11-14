# Spec Delta

## Purpose

Let users maintain persistent food entries within daily meals and see accurate consumed nutrition while preserving the historical basis of every entry.

## ADDED Requirements

### Requirement: Display the persisted diary day

The diary SHALL load entries for the selected calendar date through `GET /diary-entries?date=yyyy-MM-dd`, group them into breakfast, lunch, dinner and snacks, and preserve server ordering. It SHALL NOT seed example entries or substitute examples when loading fails.

#### Scenario: Open an existing day
- **WHEN** a user opens or reloads a date containing persisted entries
- **THEN** the diary displays those entries with their stored food names, quantities, units, serving descriptions and consumed nutrition
- **AND** entries from other dates are not displayed

#### Scenario: Empty day and read failure
- **WHEN** a successful daily read returns no entries
- **THEN** all four meals show their empty state and consumed totals are zero
- **WHEN** the initial daily read fails
- **THEN** the diary shows an error with a retry action without presenting an empty day as a successful read

### Requirement: Use authoritative daily and meal totals

The diary SHALL display the daily `totals` and per-meal `mealTotals` returned by the backend. It SHALL NOT derive these totals from rounded entry values. Consumed totals SHALL be refreshed after successful additions, edits and deletions for that date.

#### Scenario: Rounding differs between entries and totals
- **GIVEN** rounded entry values whose sum differs from the backend total
- **WHEN** the diary displays the day
- **THEN** the summary and meal headers use the totals provided by the backend

### Requirement: Persist food additions in the selected meal

Confirming a valid amount SHALL save through `POST /diary-entries` with the selected food, date, meal and exactly one direct or serving-based amount. Success SHALL be shown only after `201` with a persisted entry ID. Repeated foods SHALL remain separate entries.

#### Scenario: Add direct and serving quantities
- **WHEN** a user confirms a positive direct amount or fractional serving count
- **THEN** the entry is saved to the selected date and meal
- **AND** revisiting or reloading the diary displays it with updated server totals

#### Scenario: Duplicate activation while saving
- **WHEN** a user activates save repeatedly while a request is pending
- **THEN** the form sends only one request for that submission
- **AND** an intentionally separate later addition creates its own entry

### Requirement: Read an entry independently of the catalogue

`GET /diary-entries/{id}` SHALL return `200` with entry identity, date, meal, exact quantity, consumed nutrition, complete historical food reference data, and optional historical serving/count data. The historical reference nutrients and quantities SHALL retain calculation precision. The read SHALL work after the catalogue food is changed or deleted.

#### Scenario: Open historical entry directly
- **GIVEN** an entry whose original catalogue food has been deleted
- **WHEN** its edit screen is opened directly using its entry ID
- **THEN** its stored food, quantity and optional serving count are available without a catalogue lookup
- **AND** the historical reference basis is available for the form's preview

### Requirement: Reuse a prefilled amount form for editing

Tapping an entry SHALL open the same amount form used for adding, prefilled from that entry. Food, date and meal SHALL be read-only. Direct entries SHALL edit their original-unit quantity; serving entries SHALL edit their original serving count. Selecting another food, serving definition or amount mode while editing SHALL NOT be offered.

#### Scenario: Edit or cancel a prefilled entry
- **WHEN** a user taps a direct or serving-based entry
- **THEN** the form shows its existing amount and a save-changes action
- **AND** the user can preview changes using the historical basis
- **WHEN** the user cancels
- **THEN** the stored entry and diary totals remain unchanged

### Requirement: Update only the consumed amount

`PUT /diary-entries/{id}` SHALL accept exactly one positive `quantity` or `servingCount`, matching the entry's original amount mode, and return `204` after commit. The update SHALL preserve entry ID, date, meal, capture order and all food/serving snapshots. Consumed quantity and nutrition SHALL be recalculated with decimal arithmetic from the original reference basis without storage rounding.

#### Scenario: Update a direct quantity using its original basis
- **GIVEN** an entry recorded as 50 g against 100 g containing 200 kcal
- **AND** the catalogue food is subsequently changed or deleted
- **WHEN** the entry quantity is updated to 75 g
- **THEN** its consumed calories become 150 kcal using the original snapshot
- **AND** its identity, date, meal, snapshots and relative capture order remain unchanged

#### Scenario: Update a fractional serving count
- **GIVEN** an entry whose historical serving equals 116 g
- **WHEN** its serving count is updated to 0.5
- **THEN** its consumed quantity becomes 58 g
- **AND** its nutrition is calculated from 58 g and the historical food reference values

### Requirement: Reject invalid amount updates without partial changes

Updates SHALL reject missing, mixed, nonpositive, mode-mismatched or arithmetically unrepresentable amounts with `400` Problem Details and a stable code. Rejected updates SHALL leave the entry and its consumed nutrition unchanged.

#### Scenario: Invalid amounts
- **WHEN** a request supplies neither amount, both amounts, zero, a negative amount, servingCount for a direct entry, quantity for a serving entry, or an overflowing calculation
- **THEN** it returns `400` and persists no change

### Requirement: Delete only the selected diary entry

`DELETE /diary-entries/{id}` SHALL permanently remove the selected entry and return empty `204` only after commit. It SHALL leave catalogue foods, catalogue servings and all other diary entries unchanged. Subsequent daily reads SHALL omit the entry and reflect its removal in daily and meal totals.

#### Scenario: Delete one of several repeated foods
- **GIVEN** multiple entries for the same food, including entries on another date
- **WHEN** one entry is deleted
- **THEN** only that entry is removed
- **AND** its catalogue food and all other entries remain available

### Requirement: Reveal deletion with a left swipe

A left swipe on an entry SHALL reveal a red delete action without opening the edit form. Activating that action SHALL request entry deletion, block repeated pending activations and refresh the affected date after success. An accessible equivalent SHALL support keyboard and assistive-technology use without requiring a gesture.

#### Scenario: Swipe and confirm the action
- **WHEN** a user swipes an entry left
- **THEN** its delete action is revealed and no deletion has yet occurred
- **WHEN** the user activates delete and the server confirms success
- **THEN** that entry disappears and the server totals are refreshed

#### Scenario: Delete without touch gestures
- **WHEN** a keyboard or assistive-technology user navigates to an entry's delete action
- **THEN** they can activate the same deletion behavior

### Requirement: Report missing targets and storage failures consistently

Detail, update and delete SHALL return `400` Problem Details for malformed/empty IDs and `404` for absent entries. Missing-target codes SHALL be `GetDiaryEntryById.NotFound`, `UpdateDiaryEntry.NotFound` and `RemoveDiaryEntry.NotFound`. Unexpected storage failures SHALL use generic `500` Problem Details without internal exception details or partial writes.

#### Scenario: Entry already removed
- **WHEN** a client reads, updates or deletes an already removed entry
- **THEN** the response is `404` with the operation's missing-target code
- **AND** no entry is recreated or other data changed

#### Scenario: Storage failure
- **WHEN** an update or deletion fails before commit
- **THEN** the response is generic `500` Problem Details and stored data remains unchanged

### Requirement: Preserve user input and reconcile write outcomes

Failed or uncertain writes SHALL NOT produce a success message or automatically retry a mutation. The form SHALL retain entered values on failure. If connectivity fails after submission, the UI SHALL explain that the write may have committed and provide a way to reload the affected diary before another submission. A failed refresh after a confirmed commit SHALL be reported as a refresh error.

#### Scenario: Response is lost after submission
- **WHEN** a write loses connectivity without a definitive response
- **THEN** the UI preserves user input, reports the uncertain outcome and offers diary reconciliation
- **AND** no automatic duplicate request is sent

#### Scenario: Day refresh fails after successful save
- **WHEN** the server confirms a write but the subsequent daily read fails
- **THEN** the UI treats the write as saved and offers a read retry
- **AND** it does not invite a duplicate addition by reporting the save as failed

### Requirement: Document the entry management contract

The generated OpenAPI document SHALL describe the new detail, update and delete operations, UUID parameters, response DTOs, amount restrictions, snapshot semantics and `200` or `204`, `400`, `404` and `500` responses as applicable. Existing daily-read and logging contracts SHALL remain compatible.

#### Scenario: Inspect API documentation
- **WHEN** a client retrieves the development OpenAPI document
- **THEN** the new operations and their request/response schemas and errors are documented
- **AND** the existing daily-read and logging operations retain their contracts
