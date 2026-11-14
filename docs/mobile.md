# Mobile development guide

For installation, API connectivity and debugging, start with the
[mobile README](../mobile/nutrivo-app/README.md). Source paths below are relative
to `mobile/nutrivo-app`.

The catalogue supports backend search, creation, details and editing. Deletion
remains disabled. Diary entries and targets are local examples/session state;
reloading resets the diary while catalogue data persists in PostgreSQL.

## Connection troubleshooting

- Keep the phone and computer on the same Wi-Fi, with local-network access
  enabled for Expo Go. Check VPN, firewall and guest-network isolation if either
  the QR code or API connection fails.
- Metro normally uses port 8081; the API uses port 5097. A Metro tunnel does not
  expose the API. The phone must be able to reach both services.
- The phone uses the computer's LAN address for `EXPO_PUBLIC_API_URL`.
  A browser or iOS simulator on the computer uses `http://localhost:5097`;
  the standard Android emulator uses `http://10.0.2.2:5097`.
- After changing `.env.local`, fully reload the app and restart Metro if necessary.
  Missing or invalid API configuration is shown in the foods UI; there is no
  fallback to example foods or an offline catalogue.
- Test `http://<computer-LAN-IP>:5097/swagger` in the phone's browser. If it fails,
  check the API's LAN binding and firewall before investigating the mobile app.
  PostgreSQL stays bound to localhost; only the API needs to be reachable.

### SDK compatibility and Metro cache

After an Expo SDK upgrade, stop the old Metro server, close the project in
Expo Go and run these commands from `mobile/nutrivo-app`:

```sh
npm ci
npm start -- --clear
```

Scan the newly displayed QR code. `--clear` resets Metro's bundler cache.
The project's SDK major version must match the one supported by Expo Go.
The App Store version of Expo Go cannot run older SDK projects on a physical
iPhone; updating only the Expo Go app does not upgrade the project. See the
[Expo SDK upgrade guide](https://docs.expo.dev/workflow/upgrading-expo-sdk-walkthrough/).

The Android emulator requires Android Studio and an installed virtual device;
the iOS simulator requires full Xcode with a simulator. Neither is needed for
Expo Go on a physical phone.

### Web CORS configuration

Development CORS permits `http://localhost:8081` and `http://127.0.0.1:8081`, with
GET/POST/PUT/DELETE and Content-Type preflight support. Food deletion sends a
bodyless `DELETE /foods/{id}` without Content-Type and accepts `204 No Content`
without parsing JSON. A timeout or lost response may happen after deletion commits;
check availability through GET before confirming another attempt. If using another port or a LAN-hosted
web origin, configure the exact origin through API User Secrets or environment
configuration, for example from the repository root:

```sh
dotnet user-secrets set 'Cors:AllowedOrigins:2' 'http://192.168.1.10:8081' --project src/Nutrivo.Api
```

Restart the API afterwards. Origins contain the scheme, host and port, without a
trailing slash. Native Expo Go requests do not require a CORS origin. Development
CORS is not enabled in Production.

## Structure and design

```text
src/
  app/                  Root stack with food selection, diary entry creation and food management
    (tabs)/             Tabs: /, /foods and /targets
  features/diary/       Diary screen, session state and entry creation
  features/foods/       Backend food catalogue, query cache, search and create/edit form
  features/targets/     Nutrition targets placeholder screen
  shared/
    theme/              Typed colors, spacing, radii and typography
    ui/                 AppHeader, Screen, AppText and Card
assets/fonts/geist/     Bundled static fonts, source information and OFL license
```

The [Stitch screenshot](design/home/screen.png) and
[HTML export](design/home/code.html) take precedence over conflicting
values in [`DESIGN.md`](design/home/DESIGN.md). The app uses a dark theme, lavender
accents and the export's macro colors. Geist is loaded from bundled files with `expo-font`,
without runtime requests to Google Fonts. Text retains native font scaling.
Numeric text styles use tabular figures.

The root layout uses an Expo Router stack. Its `(tabs)` group contains JavaScript
tabs with a shared `AppHeader`; `/food-selection`, `/diary-entry-create`, `/food-create`, `/food-edit` and `/food-details`
open above them without a tab bar.
The header shows Nutrivo above the current page title and a circular profile
icon. The profile is labelled for screen readers and has no action yet. The
bottom bar keeps icons above the labels in portrait and landscape, highlights
the selected tab in lavender and provides at least 44-point touch targets.
Its height grows with the native font scale to make room for larger labels.

The header owns the top safe-area inset and the tab bar owns the bottom inset;
shared screens provide side insets and scrolling between the two bars. The
status bar uses light symbols. Material Design Icons from
`@react-native-vector-icons/material-design-icons` are bundled locally and
preloaded alongside Geist with `expo-font`, without remote font requests or a
custom native build. Decorative icons are hidden from screen readers.

The diary (`/`) is the initial tab and the Android back-navigation anchor.
Foods (`/foods`) opens the catalogue; targets (`/targets`) is a placeholder.
Direct URLs work in the web preview. Foods use a shared API client; authentication
is not included.

### Local diary

The diary uses local, typed example snapshots in `src/features/diary/`. These
illustrative values already describe consumed quantities and seed only the local
calendar date when the app starts. Other dates start empty. New entries and totals
are held by `DiaryProvider` above the router stack. It runs without the API or database.

The selected date starts at today in the device's local timezone. The day arrows
move one local calendar day backward or forward, including future days. Today is
labelled “Heute”; other dates show the short weekday, day and month in `de-CH`.
Entries and totals belong to the selected date; example targets are shared across days.
Reloading resets the selected date to today. Tap the central date button to
select a date directly: Android opens its system date dialog; iOS shows a native
monthly calendar with “Abbrechen” and “Übernehmen”. The web preview uses a labelled
browser date field in a modal with the same actions. Only confirmation changes
the diary date; cancelling keeps the previous selection. Past and future dates
are available. Native pickers follow the device language, the browser field follows
the browser locale, and the diary label stays in `de-CH`. Dates are interpreted
in the device's local timezone, without UTC conversion.

The native picker uses `@react-native-community/datetimepicker`, installed through
`npx expo install` to match the Expo SDK and work in Expo Go. Its native imports
are excluded from web through a platform-specific component. No custom native
build is required. Add-entry buttons open food selection for their respective
meal and selected date. Food rows open quantity entry. There is no persistent
storage, editing/deletion of saved entries, barcode scanning or voice input.

The four expanded meal cards contain breakfast (six entries), lunch (four),
dinner labelled “Nachtessen” (five), and an empty snacks state. Rice and quinoa
use 80 g dry portions. Meal and day totals are summed from the same snapshots;
the example day contains 2’157 kcal, 157 g protein, 205 g carbohydrates and 75 g
fat. Targets are illustrative: 2’400 kcal, 180 g protein, 260 g carbohydrates
and 70 g fat, leaving 243 kcal. The fat total deliberately exceeds its example
target; all progress fills are capped at 100%, with the actual totals still
shown. Display values share a whole-number `de-CH` formatter.

The preview reuses the existing theme, Geist fonts, header, scrolling screen and
bottom tabs. Macro rails stack on narrow displays or with larger native text;
meal names and quantities can wrap. The add-entry actions use compact chips with
a minimum visual size of 28 points and an 8-point touch-area extension on each
side. On narrow displays they use an icon while retaining their full accessible
names.

### Food selection

Each meal's add button opens `/food-selection?meal=breakfast|lunch|dinner|snacks&date=YYYY-MM-DD`
using one of those four IDs and the selected local calendar date. The full-screen
header shows “Lebensmittel auswählen”, the meal name and date. Missing, repeated
or invalid meal/date parameters show an error and a return action to the diary. The back button returns to the previous screen, retaining diary scroll
position; without navigation history it falls back to `/`. The root stack anchors
direct links to the tabs so native Android Back can return there as well.

The search field stays above a separately scrolling result list. It starts empty
on each opening, with “Suche nach Lebensmitteln oder Marken” and no API request.
The keyboard opens only when tapped. Searches trim outer whitespace and wait
300 ms after typing. The backend matches a case-insensitive literal substring
of name or brand and determines ordering. Each page contains up to 20 foods;
scrolling loads further pages. The displayed count is the backend total, not the
number of loaded rows. Clearing the query restores the initial hint.

Loading, no matches and request failures have distinct states. Failures offer
“Erneut versuchen”; loaded rows remain visible when a later page fails.
Old results are hidden as soon as the search changes. Each row shows its name,
optional brand, kcal and explicit reference quantity/unit. Tapping a result opens
quantity entry, which fetches the food by ID, including on direct links.

For static web exports, `useIsClient` defers device-dependent diary content and
the URL-dependent meal subtitle and food detail content until hydration. The
static HTML and initial browser render therefore agree; native rendering and later client navigation
show this content immediately.

### Diary quantity entry

Selecting a search result opens `/diary-entry-create?foodId=…&meal=…&date=YYYY-MM-DD`.
The screen shows the food, optional brand, destination meal and date. The first
serving is selected with count 1; foods without servings start with their reference
quantity in g/ml. Direct entry remains available for every food, using only its
reference unit. There is no automatic conversion between grams and milliliters.
Switching to a different option resets its amount to 1 serving or the reference quantity.

Portion counts multiply the **entire** defined portion: 1.5 × (2 EL = 30 ml)
means 45 ml. Fractional counts and amounts accept decimal commas or points.
The preview and save path share validation and calculate each nutrient as
`reference nutrient × consumed quantity / reference quantity`. Values are stored
without display rounding; the preview shows up to two decimals and the diary
retains its whole-number nutrition display. Quantity labels preserve small amounts.
Empty, non-positive, malformed and non-finite quantities/results prevent saving.

“Hinzufügen” adds one snapshot entry and returns to the existing search, retaining
its query and scroll position. A brief status message names the added food and
is announced to assistive technology. Rapid repeated taps do not duplicate a save;
consciously logging a food again creates a separate entry. Back/“Abbrechen” discards
the draft. Missing, repeated or invalid route parameters and unknown foods block
logging and offer a return to the diary. Selection from valid direct links works
without prior navigation, returning to food selection after save.

Only the selected date and meal receive the entry; their totals update immediately.
`DiaryProvider` owns session entries; no API request or persistent storage is used.
Reload/restart resets diary examples, user entries and the selected date. The
backend catalogue persists independently. Backend domain rules remain authoritative;
the diary calculation is still a local preview until diary integration is implemented.

#### Quantity entry acceptance checks

1. Add foods to all four meals on today, a past day and a future day. Verify date
   and meal in both screens and ensure entries never appear on another day.
2. Select oats, change serving size, log 1.5 portions, then log 75 g directly.
   Check both entries remain separate and totals use unrounded nutrients.
3. Select water (no servings): only direct ml entry is offered and zero nutrients
   can be saved. Check fractional amounts and decimal commas/points.
4. Create a food with reference 30 g or 250 ml and several portions, including
   “2 EL = 30 ml”. Log 1.5 of that portion: 45 ml; with 40 kcal/100 ml, expect 18 kcal.
5. Try empty, zero, negative, incomplete, non-numeric and overflowing amounts.
   Verify inline errors, retained input, unavailable preview and disabled save.
6. Save with a double tap: only one entry is added. Verify search/scroll retention,
   the brief confirmation, cancel/back without saving and date navigation.
7. Open the creation URL with missing, repeated, invalid or unknown food/meal/date
   parameters. Check the error and diary return. Reload to verify session reset.
8. Check keyboard-only operation, 320/390-point widths, landscape, increased text
   and VoiceOver/TalkBack, including selection state and success announcement.

### Food management

`/foods` loads all backend foods, ordered by name/brand/ID. Search and pagination
behave as described above, with an empty query listing all foods. Tapping a row
opens `/food-details?id=<uuid>`, displaying the persisted reference quantity,
exact basis nutrition formatted for display, and ordered serving sizes.
Details are loaded through `GET /foods/{id}`, including after browser reload.

“Lebensmittel hinzufügen” opens `/food-create`. Enter a name, optional brand,
positive reference quantity (initially 100 g; g/ml selectable), and all four
nonnegative nutrients. Zero is valid; missing nutrients are not. Inputs accept
decimal commas or points and are not rounded before submission. Text fields are
limited to 200 trimmed characters. Optional servings need a positive quantity,
free-text unit and positive equivalent in the reference unit; labels are optional.
“2 EL = 30 ml” describes the entire two-spoon serving. No g/ml conversion occurs.

Saving sends `POST /foods`. Inputs and form actions are disabled while pending;
rapid taps do not create repeated requests. POST is never automatically retried.
Errors retain the draft and show German messages; known domain codes are translated.
A network failure may occur after the server commits, so check the catalogue
before manually repeating an uncertain save. After a successful POST, list caches
are invalidated and navigation opens the new food's details. If detail loading fails,
retrying only repeats GET, never POST. Backend food and serving IDs are authoritative.

“Bearbeiten” opens `/food-edit?id=<uuid>` with a prefilled shared form. Saving
sends `PUT /foods/{id}`, invalidates detail and catalogue queries, and returns to
the food details. Cancelling returns without saving. Existing serving IDs are
preserved, new servings receive IDs from the backend and removed servings are
omitted from the replacement list. Existing diary snapshots remain unchanged.
Direct edit links load the food from the API; invalid or unknown IDs show an error.
“Löschen” opens a confirmation dialog naming the food and explaining permanent
catalogue deletion and preservation of diary entries. “Abbrechen”, Escape (Web)
or hardware Back (Android) cancels without a request. “Dauerhaft löschen” sends
one bodyless DELETE; both dialog actions, editing and in-app back navigation are
blocked while pending. DELETE is never automatically retried. The dialog uses
the existing Modal's Web focus containment/restoration and initially focuses cancel.

After 204, the app discards that food's details and resets all catalogue search
pages/counts, including inactive diary searches, before returning to `/foods`.
Earlier reads are cancelled so they cannot restore stale data. Searches restart
at page one to avoid gaps when deletion shifts the server's pagination. A later
catalogue GET failure shows read-only recovery and does not repeat DELETE.

`DeleteFood.NotFound` or a refreshed detail returning 404 shows an unavailable
food state with a route to the catalogue and no edit/delete actions, even when
details were cached. Other HTTP failures retain a German error and require a new
confirmation to retry. Connection loss or timeout explains that deletion might
already have committed; “Verfügbarkeit prüfen” checks GET before enabling a new
confirmed attempt. A failed check leaves the uncertain state recoverable. Local
diary state is never cleared by deletion; backend diary snapshots remain governed
by the existing delete endpoint contract. There is no undo or restoration action.

There are no local catalogue mutations. `FoodsProvider` owns a shared TanStack Query
client; search and detail hooks use an in-memory cache with 30-second freshness.
Screen re-entry, app foregrounding and browser focus refresh stale active queries.
Read requests support cancellation and a 15-second timeout; transient read failures
retry once, while HTTP 4xx responses do not retry automatically.

#### Food integration acceptance checks (Web and Expo Go)

1. With an empty database, verify an empty catalogue. Create foods through the UI
   or Swagger, including more than 20 matches; check total count and scroll pagination.
2. Search a name/brand with mixed case, surrounding whitespace, `%`, `_`, and a
   nonexistent term. Change the search quickly and ensure old rows never replace
   current results. Clear the search in the catalogue and diary selection.
3. Create a gram food without a brand/servings and a milliliter food with zero
   nutrients, decimal comma/point inputs and multiple servings. Check ordering,
   generated IDs, catalogue search and direct detail links after a full app reload.
4. Check required/invalid fields and incomplete servings. Trigger a backend
   validation failure (for example a number beyond decimal range); retain all inputs.
   Repeated taps during a slow save must send only one POST.
5. Stop the API during first load, pagination and saving. Check error/retry states,
   retained rows/drafts and recovery. Fail detail loading after a successful POST;
   retry must not create another food. Verify missing API URL and unknown UUID states.
6. Find a created food in diary selection and log direct amounts and fractional
   servings. Only the selected local date/meal changes; reload resets diary entries
   while the food remains stored. Open quantity entry directly with a valid food UUID.
7. Edit a food, change its reference values and add/remove servings. Verify the
   prefilled form, saved details, refreshed search results and persistence after
   reload. Check direct edit links, invalid/unknown IDs and cancellation. Existing
   diary entries must retain their snapshots; new entries use the updated food.
   Confirm diary snapshots still use their recorded values.
8. Check 320/390-point widths, landscape, large text, keyboard operation and
   VoiceOver/TalkBack. Forms/details must scroll without horizontal overflow.

#### Food deletion acceptance checks (Web and available Expo Go platforms)

Use disposable foods in a separate test database. Observe network requests to
distinguish DELETE from read-only GET recovery; do not delete real catalogue data.

1. Create a food with servings and record direct/serving diary entries. Open its
   details, choose “Löschen”, and check the name and diary-preservation explanation.
   Cancel with the button, Escape or Android Back: verify zero DELETE requests.
2. Check initial Web focus on cancel, Tab/Shift+Tab containment and restoration
   to “Löschen” after cancellation. Check 320/390-point widths, landscape, long
   names, large text, and VoiceOver/TalkBack labels and reachable actions.
3. Confirm repeatedly under a delayed connection. Verify exactly one bodyless
   DELETE, visible progress and disabled edit/back/cancel/confirm actions. Accept
   the empty 204 and return to the catalogue without a JSON parsing error.
4. Prime multiple catalogue and diary search terms, including more than 20 rows.
   After deletion, revisit them: no stale selectable row, refreshed counts, page
   one reloaded and no missing/duplicated remaining foods across later pages.
   Delay a pre-deletion list/detail GET and verify its response cannot restore it.
5. Confirm existing local diary entries/totals are unchanged in the current app
   session. Independently compare backend diary responses before/after deletion;
   the diary UI still uses local session state and does not prove backend persistence.
   Reload catalogue/details: the food stays absent and its old link is unavailable.
6. Delete the displayed food elsewhere before confirming; verify the deletion 404
   explains unavailability, clears stale searches and offers the overview without
   claiming this request deleted it. A cached detail refresh returning 404 must
   also remove edit/delete actions.
7. Exercise 400/500 errors and loss/timeout before and after a possible commit.
   Verify no automatic DELETE retry. For uncertainty, fail and then recover
   “Verfügbarkeit prüfen”: missing leads to unavailable; still existing permits a
   fresh confirmation. Check request counts throughout.
8. Fail catalogue GET after a successful 204. Verify overview read retry sends
   only GET and does not present a deletion failure or restore old pages. Record
   any native hardware-Back/accessibility checks unavailable in the environment.

## Validation

Run from `mobile/nutrivo-app`:

```sh
npm ci
npm run lint
npm run typecheck
npx expo install --check
npx expo-doctor
npx expo export --platform ios --output-dir dist/ios
npx expo export --platform android --output-dir dist/android
npx expo export --platform web --output-dir dist/web
```

Exports verify bundling and bundled assets; they do not produce installable
native apps. Generated exports, Metro state and dependencies are ignored by Git.
Automated frontend tests and CI are deferred; validate with linting, type checks,
exports and the manual checks below.

### Manual acceptance in Expo Go

1. Open the QR code and check that Nutrivo starts without a runtime error.
2. Check the dark background, lavender Nutrivo title, uppercase page title and
   circular profile icon. The profile icon must not trigger an action.
3. Switch between Tagebuch, Lebensmittel and Ziele. The diary should display
   its example day; foods shows the catalogue and targets its introduction.
   Check the matching header title. Exactly one tab is highlighted.
   On Android, Back from another tab should return to Tagebuch.
4. Confirm that the header clears the notch and status bar and the tab bar
   clears the bottom gesture area. Both bars must stay visible when scrolling.
5. Increase the phone's text size and check wrapping and scrolling, also in
   landscape. No content should be hidden or overlap. Check VoiceOver/TalkBack:
   tabs have names and a selected state, and icon glyphs are not read aloud.
6. Edit a diary example in the source and verify Fast Refresh updates the screen
   and totals.
7. Reload the app and confirm the locally bundled Geist and icon fonts load.

In the web preview, also open `/`, `/foods` and `/targets` directly and reload
each page. Check a narrow viewport (320 points), a typical phone viewport
(390 × 844), landscape and increased browser zoom. Visual checks in a web
preview complement the Expo Go steps; physical-device safe areas, native text
scaling, screen readers and Android Back must be checked on the phone.

### Diary acceptance checks

1. Verify all 15 food entries and quantities, the four meal headings and the
   empty snacks state. Meal totals must be 655, 703, 799 and 0 kcal.
2. Verify the day totals and 243 kcal remaining against the values above.
   The fat rail must stop at the end of its track despite showing 75/70 g.
3. Check 320- and 390-point widths, landscape and increased text size. Long food
   names, quantities and numbers must stay readable without horizontal overflow.
4. Scroll to snacks; the header and bottom tabs must stay visible and the last
   add-entry button must be reachable above the tabs.
5. Navigate backward, forward beyond today, and back to today. Verify the “Heute”
   label only appears for today, with short weekdays for other dates. Tap rapidly
   and confirm each tap moves one calendar day. Only today has seeded examples;
   other days start empty. Reload and confirm the date resets to today.
6. Check month and year boundaries, February 29 in a leap year, and local daylight
   saving transitions. Each tap must move exactly one local calendar day.
7. Verify both arrows are enabled, named “Vorheriger Tag” and “Nächster Tag”, and
   retain touch targets of at least 44 × 44 points. The central date button is
   enabled, has visible press feedback, and its accessible name includes the year.
   Add-entry controls open food selection; read-only rows offer no edit action.
8. Open the date picker on iOS and Android in Expo Go and in the browser. Verify
   it starts at the selected diary day. Select a different past or future day and
   confirm; verify the label, then navigate with both arrows. Repeat for a month
   boundary, year boundary, February 29, and the local daylight saving changes.
   Entries and totals must match the confirmed date; example targets stay fixed.
9. Change the draft selection and cancel. Reopen and verify the original diary
   date is still selected. On Android, also cancel with the system Back button.
   Rapid taps and repeated opening/closing must not stack dialogs. Navigate away
   from the diary while Android's dialog is open and verify it is dismissed.
10. In the browser, clear the date field or enter an incomplete/invalid date:
    “Übernehmen” must be disabled. Tab and Shift+Tab must stay inside the modal;
    Escape cancels and focus returns to the date button after cancelling or
    confirming. Verify date entry and confirmation using only the keyboard.
11. Repeat the picker checks at small widths/heights, in landscape, and with
    increased text size. The iOS calendar and all actions must remain reachable
    by scrolling vertically; the native calendar also scrolls horizontally if
    its intrinsic width exceeds a narrow window. Use VoiceOver/TalkBack to check
    the dialog and action labels.

### Food selection acceptance checks

1. Open selection from all four meal buttons and verify the matching meal title,
   hidden tabs and empty initial search. Return using the header button and
   Android Back; the diary must retain its previous scroll position and totals.
2. Create suitable foods through the API/UI, then search their names and brands
   with mixed case and surrounding whitespace. Check backend ordering and pagination.
   A query containing only spaces must show the initial hint.
3. Search `xyznotfound`: verify the no-results message. Clear the search, then
   return to the diary and reopen: both paths must restore an empty search.
4. Search stored gram/milliliter foods, including zero nutrients, absent brands
   and a long name; verify units, omitted brands and wrapping.
5. At 320- and 390-point widths, landscape and larger font sizes, verify no
   horizontal overflow, accessible touch targets and readable text. With the
   native keyboard open, ensure results can scroll while the search stays fixed.
6. Check VoiceOver/TalkBack labels, focus indication on the input and selectable
   food announcements with kcal and reference unit.
7. Load and reload `/food-selection`, a valid meal/date link and
   `/food-selection?meal=invalid` directly. Invalid context must prevent logging
   and offer a return to the diary. Confirm `/`, `/foods` and `/targets` still work directly.

## Historical validation records

These are reports from earlier checks, not validation performed for the current
checkout or current acceptance criteria. Audit counts, test counts and feature
availability describe the implementation at the time of each report. In
particular, the original food API integration disabled editing; editing is now
available, while deletion remains disabled.

### Dependency audit on 2026-11-03

On 2026-11-03, the clean SDK 58 installation reports 18 moderate findings
and no high or critical findings. No forced audit fixes or peer-dependency
exceptions have been applied. Run `npm audit` to inspect the current findings
when reviewing dependency updates; passing Expo Doctor checks does not mean
the dependency audit is clean.

### Date picker validation on 2026-11-07

- Lint, strict TypeScript, Expo dependency validation and Expo Doctor (20/20)
  passed. Exports for iOS, Android and Web succeeded.
- Browser checks covered confirmation, cancellation, empty/incomplete input,
  Escape, Tab/Shift+Tab focus containment, focus restoration and resetting to
  today on reload. Calendar checks covered February 29, month/year boundaries
  and dates of local daylight saving transitions, including subsequent arrows.
- The web dialog remained usable at 390 × 844 and 320 × 220. Browser warning/error
  logs were empty. Direct checks of the web date parser also passed in
  Europe/Zurich, America/Los_Angeles and Pacific/Auckland, including invalid dates.
- Native Expo Go, VoiceOver/TalkBack and native large-text checks remain manual:
  no simulator or connected physical device was available for this validation.

### Food deletion integration validation on 2026-11-13

- Scoped JetBrains CleanupCode completed for `Program.cs` and
  `DevelopmentAccessTests.cs`. The backend build passed without warnings;
  all 14 development-access tests and 16 existing delete endpoint/persistence
  integration tests passed, including the PostgreSQL snapshot-preservation cases.
- Scoped Prettier formatting/checks, mobile lint and strict TypeScript passed.
  No frontend automated tests, test framework or runtime dependencies were added.
- Manual Web checks used a disposable PostgreSQL container, the real API and a
  temporary local fault proxy. A separate preview copy provided its own public
  API URL because Expo's virtual environment kept the project's `.env.local`
  value despite a shell override. The project's local configuration was preserved.
- Button/Escape cancellation sent zero DELETE requests. Cancel received initial
  focus; Tab/Shift+Tab stayed in the modal and cancellation restored trigger focus.
  A double confirmation under delayed response sent exactly one bodyless DELETE;
  progress and disabled actions were visible, and empty 204 returned to the catalogue.
- Multiple catalogue/diary search terms refreshed after deletion. With more than
  20 matches, the remaining 24 foods appeared without duplicates/gaps across both
  pages. A pre-deletion page-two response delayed until after DELETE did not restore
  the removed food. Old detail links and reloads showed the unavailable state.
- A food deleted elsewhere produced `DeleteFood.NotFound` and the unavailable
  state. Injected 400/500 responses remained recoverable without automatic DELETE
  retries. Connection loss before and after commit, plus a real 15-second client
  timeout against a 17-second delayed response, offered GET-only availability
  checking; failed checks stayed recoverable, existing foods required a fresh
  confirmation, and missing foods became unavailable. A catalogue read failure
  after 204 stayed on the overview; retries sent GET without repeating DELETE.
- Direct and serving-based entries remained visible in the local diary session,
  with the same 2'311 kcal total after deletion. Separately, the real backend diary
  JSON for two persisted entries matched its pre-deletion response exactly. This
  does not imply the mobile diary is backend-persistent; it still uses local state.
- Dialog layout was checked with a long food name at 320-point width and in
  landscape; narrow page width matched the viewport. At 320 × 220, scrolling and
  keyboard operation kept the actions reachable. A separate disposable preview
  doubled font sizes and line heights: at 320 points the modal's client and scroll
  widths both remained 296 points, and wrapped actions were reachable by scrolling.
  This was a modal layout stress check, not a native font-scaling or browser-zoom
  check; the existing background details page overflowed under those artificial
  font metrics and was not changed as part of this deletion slice.
- Native Expo Go, VoiceOver/TalkBack, native large text and Android hardware Back
  remain unverified here (`xcrun simctl` is unavailable). Chrome access was not
  granted by Computer Use, so an actual Chrome browser-zoom check was not performed.

### Food API integration validation on 2026-11-08

- JetBrains CleanupCode completed for the changed C# files. The backend build
  passed without warnings and all 409 tests passed (137 domain, 96 application,
  176 integration), including the detail endpoint and development access tests.
- Mobile lint, strict TypeScript and production exports for iOS, Android and Web
  passed. No automated frontend test infrastructure was added.
- Browser checks used a separate disposable PostgreSQL database and the real API.
  They covered creating g/ml foods with and without brand/servings, decimal comma
  and point inputs, zero nutrients, backend validation failures with retained drafts,
  double-click save (exactly one persisted food), and details after full reload.
- Searches covered case/whitespace, no matches and quick changes. A catalogue with
  more than 20 foods loaded further pages by scrolling. Stopping the API before
  page two preserved existing rows and showed a footer retry; restarting the API
  and retrying loaded the missing results.
- Diary selection found the newly created food and logged 1.5 servings locally.
  Empty search showed its hint. Unknown detail IDs and direct edit links showed
  their respective not-found/unavailable states; edit/delete buttons were disabled.
- Detail layout was checked at 320 and 390 pixels without horizontal page overflow.
  The browser reported no console warnings. Native Expo Go, hardware Back, keyboard
  avoidance, large text and screen-reader checks require a physical device and
  remain manual acceptance steps.

### Before food API integration

The dated records below describe earlier implementations, including the former
local catalogue and edit/delete previews; they are not current acceptance criteria.

#### Navigation slice validation on 2026-11-04

- Lint, strict TypeScript, Expo dependency validation and Expo Doctor (20/20)
  passed. Exports for iOS, Android and Web succeeded.
- Web checks passed for all three tabs, direct route loads and reloads at
  320 × 568, 390 × 844 and 844 × 390. Exactly one tab was selected, with equal
  tab widths, no horizontal overflow and at least 44-point touch targets.
- At 320 × 220, the diary content scrolled while the header and tab bar kept
  their positions. Decorative icon glyphs were absent from the accessibility
  tree after being hidden explicitly.
- Physical-device checks, native font scaling and increased browser zoom have
  not been verified in this environment and remain manual acceptance steps.

#### Diary design validation on 2026-11-04

- Lint and strict TypeScript passed. `npx expo export --platform all
  --output-dir dist/diary` successfully bundled iOS, Android and Web.
- A direct data check verified all 15 names and quantities, meal and day totals,
  the empty meal and whole-number display rounding.
- Web checks passed at 320 × 568, 390 × 844 and 844 × 390 with no horizontal
  overflow. At 320 points, the macro rails stack without overlapping, and snacks
  remain fully reachable while the header and tabs stay fixed.
- All seven date/add-entry controls exposed a disabled state and measured at
  least 44 × 44 points. The fat fill measured exactly the track width; the calorie
  fill matched 2’157/2’400. The web preview reported no runtime warnings or errors.
- Physical-device safe areas, native text scaling, screen-reader announcements
  and increased browser zoom remain manual checks.

#### Food selection validation on 2026-11-05

- Lint and strict TypeScript checks passed. Production exports for iOS, Android
  and Web succeeded, including a final combined `expo export --platform all`.
- Local assertions passed for name/brand substring matching, whitespace, case,
  sorting, missing brands, reference units, zero kcal, unique example IDs and
  valid/invalid meal parameters.
- Browser checks covered all four meal contexts, empty/no-result/clear states,
  repeated opening, existing routes and direct search links with reloads. Diary
  scroll position was retained (952 points before and after), and the four add
  buttons each measured at least 44 points in both dimensions.
- Layout checks passed at 320 × 568, 390 × 844 and 844 × 390, including long food
  names and an independently scrolling result list below the fixed search.
  Final direct-route checks reported no browser warnings or errors after the
  static hydration adjustment.
- Physical-device safe areas, keyboard avoidance, native font scaling,
  VoiceOver/TalkBack, Android hardware Back and browser zoom remain manual checks.

#### Food management validation on 2026-11-07

- Lint, strict TypeScript and production exports for iOS, Android and Web passed.
- Local assertions covered numeric parsing, required fields, zero/negative values,
  non-finite inputs, text lengths, portion validation/order, input precision,
  name/brand search, sorting, display formatting and unique example IDs.
- Browser checks covered create/detail/list navigation, optional brand, a 30.25 g
  reference, a 250 ml reference, both decimal formats, zero macros, multiple
  servings, incomplete-serving errors, removing a serving and cancelling drafts.
  Created foods appeared in both catalogue and read-only diary search; diary
  totals remained unchanged. Reload reset local foods and showed not found for
  their old detail links. Known, missing, unknown and repeated detail IDs worked
  on direct loads without browser runtime warnings or errors.
- Layout checks covered 320 × 568, 390 × 844 and 844 × 390, including long names
  and scrolling forms, with no horizontal page overflow. Unit selection exposes
  its checked state; validation errors are associated with their input fields.
- Physical-device safe areas, native keyboard avoidance, native large text,
  VoiceOver/TalkBack, Android hardware Back and browser zoom remain manual checks.

#### Diary quantity-entry validation on 2026-11-08

- Strict TypeScript, lint and production exports for iOS, Android and Web passed.
- Browser checks covered all four meals, today/past/future date isolation,
  fractional servings, direct g/ml, zero nutrients, multiple portion options,
  invalid input, cancellation, repeated bookings and a double-click save.
  Search text survived saving and cancelling; meal/day totals updated correctly.
- A session-created food with reference 250 ml and 100 kcal, with a portion
  “2 EL = 30 ml”, yielded 45 ml and 18 kcal for 1.5 portions.
- Quantity entry was checked at 320 × 568, 390 × 844 and 844 × 390, with wrapping,
  scrolling and reachable actions. Measured page width matched the viewport at
  320 and 844 pixels. Keyboard checks covered Tab, Space, arrows and Enter.
- Unknown foods and repeated meal parameters blocked logging. A valid direct
  creation link saved successfully and showed its confirmation in food selection.
  A fresh app start produced no browser warnings/errors; reload reset entries
  and totals to the example day.
- Native runtime/device tests were unavailable (`xcrun simctl` is not installed).
  Physical-device safe areas, keyboard avoidance, native large text, browser zoom,
  VoiceOver/TalkBack and Android hardware Back remain manual checks.

#### Food editing/deletion validation on 2026-11-08

- Strict TypeScript, lint and production exports for iOS, Android and Web passed.
- Browser checks covered creation through the shared form, edit prefilling,
  changes to every field, optional brand, g/ml, zero macros, adding/removing
  portions, required-field errors, cancelling drafts and repeated save clicks.
  Reopening preserved 60.12345678901234 kcal and 0.0000001 kcal without rounding.
- After logging an apple as 1 Stück = 150 g (78 kcal), editing it to 2 EL = 30 ml
  with new reference values left the original entry unchanged. A new booking
  used the updated values (59.63 kcal preview). Deletion removed it from both
  searches while retaining both snapshots and the 2'295 kcal daily total.
- Delete checks covered confirmation, button/Escape cancellation, initial focus
  on cancel, Tab/Shift+Tab containment and focus restoration to the trigger.
  Direct edit links returned to details on both save and cancel. Missing,
  unknown and repeated IDs showed not found; a new session could not resolve
  an earlier session-created food.
- Forms/dialogs were checked at 320 × 568, 390 × 844 and 844 × 390. Page widths
  matched the viewport at 320 and 844 pixels. Unit selection supported Space
  and arrow keys, form actions remained reachable, and final browser logs
  contained no runtime warnings or errors.
- No native simulator was available (`xcrun simctl` is not installed). Native
  safe areas, keyboard avoidance, large text, VoiceOver/TalkBack, Android
  hardware Back and increased browser zoom remain manual acceptance checks.
