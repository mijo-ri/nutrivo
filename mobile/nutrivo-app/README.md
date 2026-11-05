# Nutrivo mobile

React Native with TypeScript, Expo SDK 58 and Expo Router. The app has a shared
header and bottom tabs for the diary, foods and nutrition targets. The diary
shows a design preview with example meals and daily nutrition totals. Its add
buttons open a searchable example food catalogue; foods and targets tabs still
show introductions using the shared Nutrivo theme. The app does not fetch
nutrition data or require the API or PostgreSQL to be running.

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
  app/                  Root stack with /food-selection
    (tabs)/             Tabs: /, /foods and /targets
  features/diary/       Diary screen
  features/foods/       Food selection, example catalogue and foods tab placeholder
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
tabs with a shared `AppHeader`; `/food-selection` opens above them without a tab bar.
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
Foods (`/foods`) and targets (`/targets`) are placeholders for later slices.
Direct URLs work in the web preview. No backend client or authentication is
included.

### Diary design preview

The diary uses local, typed example snapshots in `src/features/diary/`. These
illustrative values already describe the consumed portions; they are not a
food database or production nutrition calculations. It runs without the API or database.

The date shows today in the device's local timezone. Date selection and day arrows
are visibly disabled and labelled as disabled for assistive technology. Add-entry
buttons open food selection for their respective meal. Food rows are read-only.
There is no persistence or local editing, barcode scanning or voice input.

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

Each meal's add button opens `/food-selection?meal=breakfast|lunch|dinner|snacks`
using one of those four IDs. The full-screen header shows “Lebensmittel auswählen”
and the meal name. Missing, repeated or unknown meal parameters show a general
header. The back button returns to the previous screen, retaining diary scroll
position; without navigation history it falls back to `/`. The root stack anchors
direct links to the tabs so native Android Back can return there as well.

The search field stays above a separately scrolling result list. It starts empty
on each opening, with “Suche nach Lebensmitteln oder Marken” and no food rows.
The keyboard opens only when the input is tapped. Local search trims outer
whitespace and matches a contiguous substring of either name or brand,
case-insensitively. Results sort by name and then brand using `de-CH` ordering.
Clearing the field restores the initial hint; unmatched queries show
“Keine Lebensmittel gefunden”.

The separate, typed catalogue in `src/features/foods/foodExamples.ts` contains
illustrative values per 100 g or 100 ml, including example brands and unbranded
foods. These are not inferred from consumed diary portions. Each row shows its
name, optional brand and whole-number `de-CH` kcal with the explicit reference
unit. Rows stack their nutrition below the name on narrow screens or with large
native text. They are read-only: no quantity entry, selection action, saving,
network requests or changes to diary totals are included.

For static web exports, `useIsClient` defers device-dependent diary content and
the URL-dependent meal subtitle until hydration. The static HTML and initial
browser render therefore agree; native rendering and later client navigation
show this content immediately.

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
Additional test libraries and CI are deferred; validate this navigation slice
with linting, type checks, exports and the manual checks below.

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
   its example day; the other tabs show their matching introduction and header
   title. Exactly one tab is highlighted.
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
5. Check that date controls cannot change data and are announced as disabled.
   Add-entry controls open food selection; read-only rows offer no edit action.

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
6. Check VoiceOver/TalkBack labels, focus indication on the input and read-only
   food announcements with kcal and reference unit.
7. Load and reload `/food-selection`, a valid meal link and
   `/food-selection?meal=invalid` directly. Verify the generic or meal header and
   back navigation. Confirm `/`, `/foods` and `/targets` still work directly.

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
