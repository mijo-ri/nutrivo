# Nutrivo mobile

React Native with TypeScript, Expo SDK 58 and Expo Router. The food catalogue
supports backend search, details, creation and editing; deletion remains disabled.
Diary entries and nutrition targets currently use local examples/session state.
Reloading resets the diary; foods persist in PostgreSQL.

## Prerequisites

- Complete the [backend setup](../../README.md#local-setup), including migrations.
- A supported Node.js LTS release and npm. The installed React Native version
  requires Node.js 22.13+, 24.3+ or 26+; use an even-numbered LTS release.
- Expo Go supporting **SDK 58** on an iOS or Android phone.
- The phone and computer on the same Wi-Fi network.

Expo Go on a physical phone needs no Expo account, cloud build, Xcode or Android
Studio. Dependencies are pinned in `package-lock.json`.

## 1. Install and configure

From the repository root:

```sh
cd mobile/nutrivo-app
npm ci
cp .env.example .env.local
```

In `.env.local`, set the computer's LAN address, for example:

```dotenv
EXPO_PUBLIC_API_URL=http://192.168.1.10:5097
```

On the phone, `localhost` refers to the phone itself. This variable is public
configuration bundled with the app; never put secrets in it.

## 2. Make the API reachable

In Rider, open **Run → Edit Configurations**, select the API's `http` launch
configuration and add `--urls http://0.0.0.0:5097` to **Program arguments**.
Keep the `http` launch profile selected so the environment remains Development.
Apply this to your local configuration, then start or restart it with **Debug**.
See [Rider launch-profile settings](https://www.jetbrains.com/help/rider/Running_LaunchSettings.html).

Alternatively, from a separate terminal at the repository root:

```sh
dotnet run --project src/Nutrivo.Api --launch-profile http --urls http://0.0.0.0:5097
```

Run one API instance at a time. Allow API port 5097 through the computer's local
firewall, and check `http://<computer-LAN-IP>:5097/swagger` in the phone's browser.
Development permits HTTP without a local HTTPS certificate. PostgreSQL stays
bound to localhost.

## 3. Start and debug the app

From `mobile/nutrivo-app`:

```sh
npm start
```

Scan Metro's QR code with the iPhone Camera app or Expo Go on Android. Allow local
network access when prompted. Open **Lebensmittel**; an empty database shows an
empty catalogue. Create a food and check that it remains after reloading the app.

With the app open, press **j** in the Metro terminal to open
[React Native DevTools](https://docs.expo.dev/debugging/tools/#debugging-with-react-native-devtools).
Use **Console** for logs and **Sources** to set a breakpoint, then repeat the
relevant action in the app. Backend C# breakpoints remain in Rider. Fast Refresh
updates the app after source changes; stop Metro with Ctrl+C.

After changing `.env.local`, fully reload the app; restart Metro if necessary.
For connection issues or an Expo Go SDK mismatch, see
[troubleshooting](../../docs/mobile.md#connection-troubleshooting).

## Web preview

Set `EXPO_PUBLIC_API_URL=http://localhost:5097` in `.env.local`, then run from
`mobile/nutrivo-app` (stop an existing Metro server first):

```sh
npm run web -- --port 8081
```

Open [localhost:8081](http://localhost:8081) and use the browser's developer tools
for Web debugging. Development CORS allows `http://localhost:8081` and
`http://127.0.0.1:8081`. For other origins, see
[Web CORS configuration](../../docs/mobile.md#web-cors-configuration).
Switch back to the computer's LAN address when returning to the phone.

## Common commands

Run from `mobile/nutrivo-app`:

| Command | Purpose |
| --- | --- |
| `npm start` | Start Metro in Expo Go mode. |
| `npm run web -- --port 8081` | Start the Web preview. |
| `npm run android` | Open Expo Go on a configured Android emulator or connected device. |
| `npm run ios` | Open Expo Go in an iOS simulator; requires full Xcode. |
| `npm run lint` | Run ESLint. |
| `npm run typecheck` | Check TypeScript without emitting files. |

## Documentation

- [Backend setup and debugging](../../README.md)
- [Structure, design and current behaviour](../../docs/mobile.md#structure-and-design)
- [Validation commands and manual checks](../../docs/mobile.md#validation)
- [Historical validation records](../../docs/mobile.md#historical-validation-records)
- [API examples and contracts](../../docs/api.md)
