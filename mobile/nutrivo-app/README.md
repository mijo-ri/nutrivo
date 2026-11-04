# Nutrivo mobile

React Native with TypeScript, Expo SDK 58 and Expo Router. The app has a shared
header and bottom tabs for the diary, foods and nutrition targets. Each tab
currently shows a static introduction using the shared Nutrivo theme. The app
does not load nutrition data or require the API or PostgreSQL to be running.

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
  app/                  Expo Router tabs: /, /foods and /targets
  features/diary/       Diary screen
  features/foods/       Foods placeholder screen
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

The root layout uses Expo Router's JavaScript tabs with a shared `AppHeader`.
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
Direct URLs work in the web preview. No backend client, nutrition calculations,
authentication or demo nutrition data is included.

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
3. Switch between Tagebuch, Lebensmittel and Ziele. Each tab should display its
   matching introduction and header title; exactly one tab is highlighted.
   On Android, Back from another tab should return to Tagebuch.
4. Confirm that the header clears the notch and status bar and the tab bar
   clears the bottom gesture area. Both bars must stay visible when scrolling.
5. Increase the phone's text size and check wrapping and scrolling, also in
   landscape. No content should be hidden or overlap. Check VoiceOver/TalkBack:
   tabs have names and a selected state, and icon glyphs are not read aloud.
6. Edit the introduction text and verify Fast Refresh updates the screen.
7. Reload the app and confirm the locally bundled Geist and icon fonts load.

In the web preview, also open `/`, `/foods` and `/targets` directly and reload
each page. Check a narrow viewport (320 points), a typical phone viewport
(390 × 844), landscape and increased browser zoom. Visual checks in a web
preview complement the Expo Go steps; physical-device safe areas, native text
scaling, screen readers and Android Back must be checked on the phone.

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
