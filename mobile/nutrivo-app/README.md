# Nutrivo mobile

React Native with TypeScript, Expo SDK 58 and Expo Router. The app has a shared
header and bottom tabs for the diary, foods and nutrition targets. The diary
supports adding foods by direct quantity or serving count, with per-day entries
and live meal/day nutrition totals. Its add buttons open a searchable food catalogue. The foods tab supports searching, viewing
and creating foods with nutrition and serving sizes; targets remains a placeholder.
The app does not fetch nutrition data or require the API or PostgreSQL to be running.

## Run on your phone

Prerequisites: a supported Node.js LTS release, npm and Expo Go supporting
**SDK 58** on an iOS or Android phone. SDK 58 tooling supports Node.js 22.13+,
24.3+ or 26+, excluding odd-numbered releases.
Dependencies are pinned by `package-lock.json`.

From the repository root:

```sh
cd mobile/nutrivo-app
npm ci
npm start
```

Keep the computer and phone on the same Wi-Fi network. `npm start` starts Metro
in Expo Go mode and shows a QR code. Scan it with the iPhone Camera app or with
Expo Go on Android, then open Nutrivo in Expo Go. No Expo account, cloud build,
Xcode or Android Studio is needed for this workflow.

If the phone cannot connect, check that both devices can communicate on the
local network, grant Expo Go local-network access when prompted and check VPN
or firewall settings for the Metro port shown in the terminal (usually 8081).
Stop the development server with Ctrl+C.

After updating from SDK 57, stop the old Metro server, close the project in
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

Run the commands below from `mobile/nutrivo-app`:

| Command | Purpose |
| --- | --- |
| `npm start` | Start Metro and connect Expo Go using the QR code. |
| `npm run android` | Open Expo Go on a configured Android emulator or connected Android device. |
| `npm run ios` | Open Expo Go in an iOS simulator; requires full Xcode with a simulator. |
| `npm run web` | Open a browser preview. |
| `npm run lint` | Run Expo's ESLint configuration. |
| `npm run typecheck` | Check strict TypeScript without emitting files. |

The Android emulator requires Android Studio and an installed virtual device.
The primary development workflow is Expo Go on a physical phone.

## Structure and design

```text
src/
  app/                  Root stack with food selection, diary entry creation and food management
    (tabs)/             Tabs: /, /foods and /targets
  features/diary/       Diary screen, session state and entry creation
  features/foods/       Food catalogue, session state, search, creation and details
  features/targets/     Nutrition targets placeholder screen
  shared/
    theme/              Typed colors, spacing, radii and typography
    ui/                 AppHeader, Screen, AppText and Card
assets/fonts/geist/     Bundled static fonts, source information and OFL license
```

The [Stitch screenshot](../../docs/design/home/screen.png) and
[HTML export](../../docs/design/home/code.html) take precedence over conflicting
values in `DESIGN.md`. The app uses a dark theme, lavender accents and the
export's macro colors. Geist is loaded from bundled files with `expo-font`,
without runtime requests to Google Fonts. Text retains native font scaling.
Numeric text styles use tabular figures.

The root layout uses an Expo Router stack. Its `(tabs)` group contains JavaScript
tabs with a shared `AppHeader`; `/food-selection`, `/diary-entry-create`, `/food-create` and `/food-details`
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
Direct URLs work in the web preview. No backend client or authentication is
included.

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

### Food selection preview

Each meal's add button opens `/food-selection?meal=breakfast|lunch|dinner|snacks&date=YYYY-MM-DD`
using one of those four IDs and the selected local calendar date. The full-screen
header shows “Lebensmittel auswählen”, the meal name and date. Missing, repeated
or invalid meal/date parameters show an error and a return action to the diary. The back button returns to the previous screen, retaining diary scroll
position; without navigation history it falls back to `/`. The root stack anchors
direct links to the tabs so native Android Back can return there as well.

The search field stays above a separately scrolling result list. It starts empty
on each opening, with “Suche nach Lebensmitteln oder Marken” and no food rows.
The keyboard opens only when the input is tapped. Local search trims outer
whitespace and matches a contiguous substring of either name or brand,
case-insensitively. Results sort by name and then brand using `de-CH` ordering.
Clearing the field restores the initial hint; unmatched queries show
“Keine Lebensmittel gefunden”.

The shared, typed catalogue is seeded from `src/features/foods/foodExamples.ts`
with illustrative macros, brands and portions per 100 g or 100 ml. It also includes
foods created during this app session, with their own reference quantities. These
values are independent of consumed diary portions. Each row shows its name,
optional brand and `de-CH` kcal with up to two fractional digits and an explicit
reference quantity and unit. Rows stack their nutrition below the name on narrow
screens or with large native text. Tapping a result opens quantity entry; the
separate food catalogue continues to open read-only food details.

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
Reload/restart resets the catalogue, diary examples, user entries and selected date.
Backend rules remain authoritative when API integration is introduced.

Quantity entry acceptance checks:

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

### Food management preview

`/foods` shows all foods alphabetically by name, then brand. The search matches
name or brand substrings, ignoring surrounding whitespace and case. Clearing it
shows the complete catalogue again. Tapping a row opens `/food-details?id=…`,
showing the food, reference quantity, kcal, protein, carbohydrates, fat and each
additional serving with its equivalent in the reference unit.

“Lebensmittel hinzufügen” opens `/food-create`. Enter a name, an optional brand,
a positive reference quantity (initially 100 g; g/ml selectable), and all four
nonnegative nutrition values. Zero is a known value; empty nutrient fields are
invalid. Decimal commas and decimal points are accepted; incomplete decimals,
non-finite values and partially numeric text are rejected. Text fields accept up
to 200 trimmed characters. Inputs are stored without display rounding. Nutrition
displays use up to two fractional digits; quantity displays preserve small amounts
such as 0.001 g instead of rounding them to zero.

Optional portions can be added and removed while drafting. Each requires a
positive quantity, a free-text unit and a positive equivalent quantity in the
food's g/ml reference unit; its label is optional. For example, “2 EL = 30 ml”
means that the entire two-spoon portion equals 30 ml. Incomplete portion rows
must be completed or removed before saving. Changing the reference unit changes
the unit of all equivalents; no automatic g/ml conversion takes place.

Saving validates every field, adds the food once and replaces the form with its
details. The header back button returns to foods, including on direct links;
“Abbrechen” or leaving the form discards the draft. Missing, repeated or unknown
detail IDs show a not-found message and a link to the catalogue.

`FoodsProvider` holds the catalogue above the router stack using React Context
and state. New foods are available immediately in the foods tab and diary search.
Switching screens retains them; restarting or reloading resets to the examples.
A direct link to a session-created food after reload therefore shows not found.
Diary search can log these foods immediately; saved diary entries hold independent
food and serving snapshots and unrounded consumed nutrients.
There is no backend integration, persistent storage, editing or deletion of saved
foods, and no new dependencies.

Food management acceptance checks:

1. Open foods: all 14 examples appear in name/brand order. Search
   ` HaFeRfLoCkEn `, `ALPEN`, `flocken`, an unmatched term and whitespace.
   Clear the query and verify the complete list returns.
2. Open an apple, water and oat drink: check absent brands, zero values, all four
   nutrients, g/ml and servings. Check the long tofu name wraps.
3. Create a food with reference 30 g, decimals in both formats, zero macros,
   optional brand and no portions. Save and check its details and catalogue row.
   Repeat with a brand, 250 ml and multiple portions, including “2 EL = 30 ml”.
4. Leave required fields empty; try negative/zero quantities, negative nutrients,
   `1,`, `1.`, `12abc`, non-finite numbers and text over 200 characters. Saving
   must show field errors and retain the draft without creating a food. Zero
   nutrients and decimal quantities must work. Correct errors and save once.
5. Add multiple portions, leave one incomplete and verify saving is blocked.
   Remove it and save. Check remaining portions retain their order and values.
   Change the reference unit and check all equivalent labels follow it.
6. Cancel a filled form and reopen; defaults must return without an added food.
   After saving, return to foods, switch to diary and find the new food by name
   and brand. Diary search must start empty; logging the new food must update only
   the chosen date and meal.
7. Load `/food-create`, `/food-details?id=apple`, `/food-details`, an unknown ID
   and repeated IDs directly; verify the appropriate screen and return to foods.
   Reload a local food's detail URL; verify not found and the reset catalogue.
8. Check 320- and 390-point widths, landscape, large text and browser zoom.
   Forms and long details must scroll without horizontal overflow; all actions
   must stay reachable with the native keyboard open. Check focus indication,
   field/error labels and unit selection with VoiceOver/TalkBack and keyboard.

## Validation

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

### Dependency audit

On 2026-11-03, the clean SDK 58 installation reports 18 moderate findings
and no high or critical findings. No forced audit fixes or peer-dependency
exceptions have been applied. Run `npm audit` to inspect the current findings
when reviewing dependency updates; passing Expo Doctor checks does not mean
the dependency audit is clean.

Manual acceptance in Expo Go:

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
6. Edit a diary example and verify Fast Refresh updates the screen and totals.
7. Reload the app and confirm the locally bundled Geist and icon fonts load.

In the web preview, also open `/`, `/foods` and `/targets` directly and reload
each page. Check a narrow viewport (320 points), a typical phone viewport
(390 × 844), landscape and increased browser zoom. Visual checks in a web
preview complement the Expo Go steps; physical-device safe areas, native text
scaling, screen readers and Android Back must be checked on the phone.

Diary acceptance checks:

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

Date picker validation on 2026-11-07:

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

Navigation slice validation on 2026-11-04:

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

Diary design validation on 2026-11-04:

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

Food selection acceptance checks:

1. Open selection from all four meal buttons and verify the matching meal title,
   hidden tabs and empty initial search. Return using the header button and
   Android Back; the diary must retain its previous scroll position and totals.
2. Search ` HaFeRfLoCkEn `: both oat examples must appear, sorted by brand. Search
   `ALPEN`: name/brand matches must appear. Search `flocken`: both oats match.
   A query containing only spaces must show the initial hint.
3. Search `xyznotfound`: verify the no-results message. Clear the search, then
   return to the diary and reopen: both paths must restore an empty search.
4. Search `Apfel`, `Milch` and `Wasser`: verify absent brands are omitted, gram
   and milliliter labels are correct, and zero kcal is displayed. Search
   `Räuchertofu` to check wrapping of the long name and brand.
5. At 320- and 390-point widths, landscape and larger font sizes, verify no
   horizontal overflow, accessible touch targets and readable text. With the
   native keyboard open, ensure results can scroll while the search stays fixed.
6. Check VoiceOver/TalkBack labels, focus indication on the input and selectable
   food announcements with kcal and reference unit.
7. Load and reload `/food-selection`, a valid meal/date link and
   `/food-selection?meal=invalid` directly. Invalid context must prevent logging
   and offer a return to the diary. Confirm `/`, `/foods` and `/targets` still work directly.

Food selection validation on 2026-11-05:

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

Food management validation on 2026-11-07:

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

Diary quantity-entry validation on 2026-11-08:

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
