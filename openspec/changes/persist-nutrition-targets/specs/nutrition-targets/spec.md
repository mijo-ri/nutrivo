# Spec Delta

## Purpose

Let users configure persistent daily calorie and macronutrient targets and compare the nutrition consumed on any diary date against their current settings.

## ADDED Requirements

### Requirement: Start without configured targets

The system SHALL initially have no target set. It SHALL NOT seed, prefill or substitute example targets. An absent target set SHALL be distinct from explicitly entered numeric values and from a failed read.

#### Scenario: First use
- **WHEN** a user opens Ziele before any target set has been saved
- **THEN** all four input fields are empty
- **AND** opening or reading the page does not create a database record

#### Scenario: Read fails on first use
- **WHEN** the initial target read fails
- **THEN** the page shows an error and a read-retry action
- **AND** it does not present the failure as a successfully loaded empty target set or show example values

### Requirement: Read the current target set

`GET /nutrition-targets` SHALL return `200` with `{ "targets": null }` when unset, or a `targets` object containing `kilocalories`, `proteinGrams`, `carbohydratesGrams` and `fatGrams` when set. Saved target values SHALL be returned without display rounding. Reads SHALL NOT mutate storage.

#### Scenario: Read saved decimal values
- **GIVEN** a target set containing fractional values has been saved
- **WHEN** the client reads the current targets after an application restart
- **THEN** the API returns the persisted values in kcal and grams without rounding them for display

#### Scenario: Read an unset target set
- **WHEN** a successful read finds no saved target set
- **THEN** the response is `200` with `targets` equal to null
- **AND** absence is not reported as an error or represented by four zero values

### Requirement: Save one complete current target set

`PUT /nutrition-targets` SHALL accept the four independent target values and atomically create or replace the single current target set. It SHALL return `200` with the saved `targets` object only after commit. Repeating the same request SHALL NOT create additional target sets. Concurrent successful saves SHALL leave one complete submitted set, with the last committed save winning.

#### Scenario: Save initial targets and replace them
- **WHEN** a valid complete target set is saved for the first time
- **THEN** subsequent reads return that set after reload or restart
- **WHEN** another valid complete set is saved
- **THEN** subsequent reads return the replacement rather than an additional set or historical version

#### Scenario: Repeated or concurrent saves
- **WHEN** identical requests are repeated or two valid different sets are saved concurrently
- **THEN** storage contains exactly one current set
- **AND** its four values come from one complete committed request rather than a mixture of requests

### Requirement: Keep calorie and macro targets independent

The system SHALL accept and store calorie and macronutrient targets independently. It SHALL NOT derive calories from macros, derive macros from calories, enforce an energy-equivalence equation, or silently change another field when one target changes.

#### Scenario: Save independently chosen targets
- **WHEN** a user submits 2400 kcal, 180 g protein, 260 g carbohydrates and 70 g fat
- **THEN** the system retains those four values even though their macro-derived calorie total differs

### Requirement: Reject invalid target submissions atomically

All four numeric fields SHALL be required. Missing, null, negative, nonnumeric or out-of-decimal-range input SHALL return `400` Problem Details and SHALL leave the existing target set unchanged. Invalid first submissions SHALL leave targets unset. Domain validation errors SHALL use stable field-specific codes.

#### Scenario: Invalid initial or replacement submission
- **WHEN** a request omits a field or supplies null, a negative number, invalid numeric JSON or a value outside the supported decimal range
- **THEN** the request fails with `400` and no partial target set is stored
- **AND** previously saved targets, if present, remain unchanged

### Requirement: Accept explicit zero targets safely

Each target SHALL accept zero as a configured value. A zero target SHALL NOT mean unset. The diary SHALL omit percentage progress for each individual zero target while still showing consumption and its configured zero target. For positive targets, progress fills SHALL be capped at 100% while actual consumption remains visible. Remaining calories SHALL be the calorie target minus consumed calories, with negative results labelled as over target.

#### Scenario: Configure all targets as zero
- **WHEN** a user explicitly enters and saves zero in all four fields
- **THEN** the API persists and returns a configured zero-valued target set
- **AND** reopening Ziele shows four zero values rather than empty fields
- **AND** the diary displays numeric comparisons without percentage calculations, division by zero or a target-setup prompt

#### Scenario: Mix positive and zero targets
- **GIVEN** the calorie and protein targets are positive while the carbohydrate and fat targets are zero
- **WHEN** the diary displays consumed nutrition
- **THEN** only the positive targets have percentage progress rails
- **AND** consuming more calories than the target shows the excess amount labelled as over target without concealing the consumed value

### Requirement: Edit targets through Ziele

Ziele SHALL provide labelled inputs for calories in kcal and protein, carbohydrates and fat in grams, plus an explicit save action. Existing values SHALL prefill editing without applying diary display rounding. Unsaved changes SHALL remain drafts. The page SHALL prevent repeated submissions while saving, preserve input on failure, and never overwrite an edited draft with a background read.

#### Scenario: Edit and save
- **WHEN** a user opens Ziele with saved targets
- **THEN** the four fields show the saved values with their units
- **WHEN** the user edits fields without saving
- **THEN** persisted targets and diary comparisons remain unchanged
- **WHEN** a successful save completes
- **THEN** the page confirms the save and the diary uses the saved values

#### Scenario: Decimal input and invalid drafts
- **WHEN** the user enters decimal values with a comma or a point
- **THEN** complete valid decimals can be saved
- **WHEN** a required field is empty or contains invalid numeric text
- **THEN** the page identifies the field and sends no save request

#### Scenario: Background refresh during editing
- **GIVEN** a user has unsaved target edits
- **WHEN** a background read or screen-focus refresh completes
- **THEN** the current draft is preserved

### Requirement: Compare every diary date with current targets

The diary SHALL use the same persisted current targets for every selected date, including past dates. A successful target change SHALL update target comparisons without changing the selected date, diary entries, snapshots, meal totals or daily consumed totals. Consumed totals SHALL continue to come from the existing diary API.

#### Scenario: Change targets and revisit a past date
- **GIVEN** a past diary date has existing entries and consumed totals
- **WHEN** a user saves new targets and displays that date
- **THEN** its progress and remaining calories use the new targets
- **AND** its entries, snapshots and consumed totals are unchanged

### Requirement: Display consumption when targets are unset

When targets are unset, the diary SHALL display consumed kcal and all three consumed macros, a "Noch keine Ziele festgelegt" hint and an accessible link to Ziele. It SHALL omit target denominators, progress rails and remaining-calorie comparisons until targets are available.

#### Scenario: Existing diary day without targets
- **WHEN** a diary day loads successfully and the target read confirms no targets are set
- **THEN** the consumed values and meal entries remain visible
- **AND** the summary offers target setup without displaying example targets or progress against zero

### Requirement: Keep target read failures separate from diary data

Loading or failing to read targets SHALL NOT prevent a successfully loaded diary day from displaying its consumed nutrition and entries. Initial target loading SHALL NOT appear as an unset state. A target read failure SHALL show a retry action; cached targets displayed during a failed refresh SHALL be identified as potentially outdated.

#### Scenario: Target API fails while diary loads
- **WHEN** the diary read succeeds but no target data can be loaded
- **THEN** consumption and entries are displayed with a target-specific error and retry action
- **AND** the diary does not display example comparisons or claim that targets are unset

#### Scenario: Refresh fails with cached targets
- **WHEN** a target refresh fails after targets have already been loaded
- **THEN** any retained target comparisons carry a visible stale/error notice

### Requirement: Report target save outcomes accurately

Failed or uncertain saves SHALL preserve the draft and SHALL NOT show success or automatically retry the mutation. An uncertain outcome SHALL offer a read-only check of persisted targets. A confirmed commit SHALL remain a successful save even if a later refresh fails. Unexpected storage failures SHALL return generic `500` Problem Details without exposing internal details.

#### Scenario: Connection is lost after submission
- **WHEN** the client cannot determine whether a target save committed
- **THEN** it explains that the targets may have been saved and offers to check persisted targets
- **AND** checking does not silently replace the user's draft or trigger another save

#### Scenario: Refresh fails after a confirmed save
- **WHEN** the API confirms the save and a later read fails
- **THEN** the UI reports a refresh failure while retaining the confirmed saved outcome

#### Scenario: Storage failure before commit
- **WHEN** persistence fails before committing a replacement
- **THEN** the API returns generic `500` Problem Details
- **AND** the previously saved set remains unchanged

### Requirement: Document the nutrition target contract

Generated OpenAPI documentation SHALL describe both target operations, the nullable read envelope, required independent numeric values and their units, validation rules, atomic replacement semantics and applicable `200`, `400` and `500` responses. Existing diary and food API contracts SHALL remain compatible.

#### Scenario: Inspect OpenAPI
- **WHEN** a client reads the development OpenAPI document
- **THEN** both target endpoints and their request, response and error schemas are documented
- **AND** the existing diary and food operations retain their contracts
