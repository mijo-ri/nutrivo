# Nutrivo mobile

React Native with TypeScript, Expo SDK 58 and Expo Router. The first screen is a
static diary introduction with the shared Nutrivo theme. It does not load diary
data or require the API or PostgreSQL to be running.

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
  app/                  Expo Router stack and diary entry route
  features/diary/       Diary screen
  shared/
    theme/              Typed colors, spacing, radii and typography
    ui/                 Screen, AppText and Card
assets/fonts/geist/     Bundled static fonts, source information and OFL license
```

The [Stitch screenshot](../../docs/design/home/screen.png) and
[HTML export](../../docs/design/home/code.html) take precedence over conflicting
values in `DESIGN.md`. The app uses a dark theme, lavender accents and the
export's macro colors. Geist is loaded from bundled files with `expo-font`,
without runtime requests to Google Fonts. Text retains native font scaling.
Numeric text styles use tabular figures.

The root stack hides its native header so the diary screen owns the visible
heading. Shared screens provide safe-area insets and scrolling; the status bar
uses light symbols. No tab bar, backend client, nutrition calculations or
demo nutrition data is included. The next slice can replace the introduction
with the actual diary day without changing the routing shell.

## Validation

```sh
npm ci
npm run lint
npm run typecheck
npx expo install --check
npx expo-doctor
npx expo export --platform ios --output-dir dist/ios
npx expo export --platform android --output-dir dist/android
```

Exports verify bundling and bundled assets; they do not produce installable
native apps. Generated exports, Metro state and dependencies are ignored by Git.
Additional test libraries and CI are deferred until the app gains behavior.

### Dependency audit

On 2026-11-03, the clean SDK 58 installation reports 18 moderate findings
and no high or critical findings. No forced audit fixes or peer-dependency
exceptions have been applied. Run `npm audit` to inspect the current findings
when reviewing dependency updates; passing Expo Doctor checks does not mean
the dependency audit is clean.

Manual acceptance in Expo Go:

1. Open the QR code and check that Nutrivo starts without a runtime error.
2. Check the dark background, lavender Nutrivo title, Tagebuch heading and card.
3. Confirm that the screen clears the notch, status bar and bottom gesture area.
4. Increase the phone's text size and check wrapping and scrolling, also in
   landscape. No content should be hidden or overlap.
5. Edit the introduction text and verify Fast Refresh updates the screen.
6. Reload the app and confirm the locally bundled Geist font loads again.

Visual checks in a web preview complement these steps; physical-device checks
must be performed on the phone.
