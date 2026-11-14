# Spec Delta

## ADDED Requirements

### Requirement: Confirm catalogue deletion from food details

The app SHALL offer deletion from an available food's details on Web, iOS and Android. Before sending a deletion request, it SHALL show a dialog naming the food and explaining permanent deletion and preservation of existing diary entries, with distinct cancel and destructive confirmation actions.

#### Scenario: Open confirmation without deleting
- **WHEN** a user selects "Löschen" on an available food's details
- **THEN** the dialog identifies that food and explains the deletion effects
- **AND** the actions are "Abbrechen" and "Dauerhaft löschen"
- **AND** no deletion request has been sent

#### Scenario: Cancel confirmation
- **WHEN** the user cancels the dialog before submitting, including Escape on Web or hardware Back on Android
- **THEN** the dialog closes and the food remains unchanged
- **AND** no deletion request is sent

### Requirement: Make deletion confirmation accessible

The confirmation dialog SHALL have an accessible name and message, readable content and reachable actions at narrow widths and increased text sizes. On Web, focus SHALL start on cancel, remain within the open dialog, and return to the delete trigger after cancellation. Assistive technology SHALL identify the dialog and its actions on native platforms.

#### Scenario: Operate the dialog with a keyboard
- **WHEN** a Web user opens the dialog and navigates with Tab and Shift+Tab
- **THEN** focus starts on "Abbrechen" and remains within the dialog
- **AND** Escape closes the unsubmitted dialog and restores focus to "Löschen"

#### Scenario: Read and operate on a small screen
- **WHEN** the dialog is opened at 320-point width or with increased text size
- **THEN** the food name, explanation and both actions remain readable and reachable without horizontal overflow
- **AND** screen readers can identify the dialog and operate its actions

### Requirement: Submit confirmed deletion once

After explicit confirmation, the app SHALL send one bodyless `DELETE /foods/{id}` for the displayed food. While it is pending, it SHALL display progress and block duplicate submission, editing and in-app back navigation. It SHALL NOT automatically retry a deletion request.

#### Scenario: Rapid confirmation taps
- **WHEN** the user confirms deletion repeatedly while the request is pending
- **THEN** exactly one deletion request is sent
- **AND** progress is visible and editing, confirmation, cancellation and in-app back actions cannot initiate competing actions

#### Scenario: Attempt Android back during submission
- **WHEN** the user presses Android hardware Back while deletion is pending
- **THEN** the app keeps the pending deletion view open and sends no additional request

### Requirement: Complete deletion on an empty success response

The app SHALL accept `204 No Content` as successful deletion without requiring a JSON response body. It SHALL return to the food catalogue after success. A subsequent catalogue refresh failure SHALL NOT be reported as a failed deletion or cause another deletion request.

#### Scenario: Delete a food successfully
- **WHEN** the confirmed deletion returns `204` with an empty body
- **THEN** the app returns to the food catalogue without a response-parsing error
- **AND** existing diary entries, nutrition snapshots and displayed diary totals remain unchanged

#### Scenario: Catalogue refresh fails after deletion succeeds
- **WHEN** deletion returns `204` but the subsequent catalogue read fails
- **THEN** the app shows a catalogue loading error with a read-only retry
- **AND** retrying does not repeat the deletion

### Requirement: Reconcile visible catalogue data after deletion

After successful deletion or a deletion response confirming the food is missing, the app SHALL discard stale details and refresh catalogue searches and counts, including previously cached searches used by diary selection. Late reads SHALL NOT restore the removed food. Later catalogue reads and pagination SHALL use the server's post-deletion ordering and counts.

#### Scenario: Revisit cached catalogue and diary searches
- **GIVEN** the food appears in multiple cached searches, including a paginated result
- **WHEN** deletion succeeds and the user revisits those searches
- **THEN** the deleted food is absent and cannot be selected from stale results
- **AND** refreshed counts and subsequent pages reflect the server's remaining catalogue without skipped foods caused by obsolete page boundaries

#### Scenario: A read was pending before deletion completed
- **WHEN** an earlier detail or catalogue request completes after deletion
- **THEN** its stale result cannot restore the deleted food as an available detail or selectable search result

#### Scenario: Reopen a deleted food's detail link
- **WHEN** the user directly opens or revisits details for a successfully deleted food
- **THEN** the app shows that the food is unavailable and offers a route to the catalogue
- **AND** editing and deletion are unavailable

### Requirement: Present already missing foods truthfully

For `404` with `DeleteFood.NotFound`, the app SHALL explain that the food is no longer available, hide or disable its edit and delete actions, reconcile stale catalogue data, and offer navigation to the catalogue. It SHALL NOT claim this request successfully deleted the food. A detail read returning `404` SHALL likewise prevent actions on stale food data.

#### Scenario: Another client already deleted the food
- **WHEN** the confirmed deletion returns `404` with `DeleteFood.NotFound`
- **THEN** the app displays an unavailable-food state with a route to the catalogue
- **AND** the stale food cannot be edited, deleted again or selected from cached searches

#### Scenario: Cached details are disproved by a refresh
- **GIVEN** a previously loaded food is still cached
- **WHEN** refreshing its details returns `404`
- **THEN** the app does not present edit or delete actions for that cached food

### Requirement: Recover from rejected or uncertain deletion

For rejected deletion requests or server failures, the app SHALL show a German error without presenting deletion as successful. For a timeout or connection loss, it SHALL explain that deletion may have completed and offer checking current catalogue state before another explicit attempt. Recovery SHALL NOT automatically repeat DELETE or remove diary data.

#### Scenario: Backend rejects deletion or returns an error
- **WHEN** deletion returns `400` or `500`
- **THEN** the details view retains a useful error and a route back to the catalogue
- **AND** no success navigation or automatic deletion retry occurs
- **AND** any retry requires explicit user confirmation

#### Scenario: Response is lost after possible server commit
- **WHEN** the deletion connection is interrupted or times out without a definitive response
- **THEN** the app explains the uncertain outcome and offers checking availability through a read request
- **AND** a missing food leads to an unavailable state, while a food confirmed to exist can be deleted again only after a new confirmation
- **AND** no automatic deletion retry occurs

### Requirement: Permit development Web deletion for configured origins

In Development, the API SHALL allow cross-origin `DELETE /foods/{id}` from configured Web origins, alongside its existing allowed methods and headers. Unknown origins SHALL receive no CORS permission. Production SHALL NOT acquire the development CORS policy.

#### Scenario: Configured Web origin preflights deletion
- **WHEN** a configured development Web origin sends an OPTIONS preflight requesting DELETE on a food URL
- **THEN** the response permits that origin and DELETE
- **AND** the bodyless delete can proceed without requiring a Content-Type header

#### Scenario: Reject an unconfigured origin
- **WHEN** an unconfigured origin preflights DELETE in Development
- **THEN** the response does not grant that origin access

#### Scenario: Keep development CORS out of Production
- **WHEN** a client requests deletion preflight in Production
- **THEN** the API does not grant access through the development CORS policy
