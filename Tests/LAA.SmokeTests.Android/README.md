# LAA Android Smoke Tests

PowerShell script that exercises the main user flows of the LAA MAUI Android
app on a connected device or emulator using **adb + UIAutomator**.
No additional tools required beyond Android SDK platform-tools.

---

## Quick start (when the phone is connected)

```powershell
# 1. Plug in the phone and confirm it's visible
adb devices

# 2. Run all 12 smoke tests
.\Tests\LAA.SmokeTests.Android\SmokeTest-Android.ps1
```

That's it.  The script auto-detects the device and the installed LAA package.

---

## Prerequisites

### 1. Android SDK platform-tools (`adb`)

Install via Android Studio SDK Manager, or download platform-tools standalone:
<https://developer.android.com/studio/releases/platform-tools>

Verify: `adb version`

### 2. USB debugging enabled on the device

On the phone: **Settings → About phone → tap "Build number" 7 times**
→ Developer options appear → enable **USB debugging**.

Connect the phone via USB and accept the "Allow USB debugging?" prompt.

Verify: `adb devices` should show the device in **device** state (not "unauthorized").

### 3. LAA app installed

The app must be installed on the device.  To install from the repo:

```powershell
# Build for the phone's ABI (typically arm64-v8a for modern Android phones)
dotnet publish LocalAIAssistant.Ui.Maui.csproj -f net9.0-android -c Debug
adb install -r bin\Debug\net9.0-android\com.snikpoh.localaiassistant.debug.apk
```

Verify the package is installed:
```powershell
adb shell pm list packages | Select-String "localai"
```

---

## Running the tests

### Default (auto-detect everything)

```powershell
.\Tests\LAA.SmokeTests.Android\SmokeTest-Android.ps1
```

### With explicit device and package

```powershell
.\Tests\LAA.SmokeTests.Android\SmokeTest-Android.ps1 `
    -Device    "R5CR8123456"              `   # from 'adb devices'
    -PackageName "com.snikpoh.localaiassistant.debug" `
    -MaxWaitSeconds 60
```

### Leave app open after tests

```powershell
.\Tests\LAA.SmokeTests.Android\SmokeTest-Android.ps1 -KeepAppOpen
```

---

## Parameters

| Parameter | Default | Description |
|---|---|---|
| `-Device` | (first connected) | adb device serial from `adb devices` |
| `-PackageName` | (auto-detected) | Android package name (first match for "localaiassistant") |
| `-MaxWaitSeconds` | `30` | How long to wait for the app to be ready |
| `-KeepAppOpen` | false | Leave the app running after all tests |

---

## Smoke test coverage (12 tests)

| # | Test | What it guards |
|---|------|----------------|
| 1 | App launches and Chat page is visible | Core launch |
| 2 | Chat editor is focusable and clickable | Editor accessibility regression (PRs #23-#28) |
| 3 | Chat editor is enabled (no overlay blocking) | Permission dialog / overlay interception |
| 4 | Can type a message and Send button is present | Basic chat interaction |
| 5 | Navigation to Chats tab works | Tab routing |
| 6 | Chats page shows list or empty state | Conversations page loads |
| 7 | New Chat button present on Chats page | New conversation action |
| 8 | Navigation to Inbox tab works | Inbox tab routing |
| 9 | Inbox page loads without crashing | Inbox stability |
| 10 | Navigate back to Chat tab from Inbox | Return navigation |
| 11 | Rapid tab cycling does not crash | State leak / crash on fast nav |
| 12 | Back navigation from Chats does not crash | Back-gesture stability |

---

## How it works

The script uses UIAutomator's `uiautomator dump` command to capture the
current UI hierarchy as XML, then queries it via XPath.  Taps are performed
with `adb shell input tap x y` using bounds extracted from the dump.

On Android, MAUI maps `AutomationId` (set in XAML) to the view's
`contentDescription`, which UIAutomator exposes as the `content-desc`
attribute.  The script searches by both `content-desc` (AutomationId) and
`text` (button/label text) for maximum compatibility.

### AutomationId map (Android `content-desc`)

| MAUI AutomationId | Control | Fallback (text search) |
|---|---|---|
| `ChatEditor` | Message input | `class="android.widget.EditText"` |
| `SendButton` | Send Button | `text="Send"` |
| `ClearButton` | Clear Button | `text="Clear"` |
| `NewChatButton` | New Chat Button | `text="New Chat"` |
| `ConversationsList` | Conversations list | — |
| `InboxList` | Inbox list | — |

Tab navigation uses `text` matching against the Shell tab bar items:
`"Chat"`, `"Chats"`, `"Inbox"`, `"Memory"`, `"Logs"`, `"Settings"`.

---

## Failure output

When a test fails the script prints:
1. The failure reason
2. The path to a saved UIAutomator XML dump:
   `%TEMP%\laa_failure_<TestName>.xml`

Open that file to see the full UI hierarchy at the time of failure.

---

## Known issues / gotchas

**Emulator crashes on launch (SIGBUS / signal 7)**

This is an ABI mismatch — the installed APK was built for arm64-v8a but the
emulator is x86_64 (or vice versa).  Rebuild for the target architecture:

```powershell
# For x86_64 emulator
dotnet build LocalAIAssistant.Ui.Maui.csproj -f net9.0-android

# Then deploy via Android Studio or adb install
```

**`adb devices` shows "unauthorized"**

Accept the "Allow USB debugging?" prompt on the phone.  If the prompt doesn't
appear, revoke USB debugging authorizations (Developer options) and reconnect.

**App not found (`LAA package not found on device`)**

The package is not installed.  See the [Prerequisites](#prerequisites) section
for how to build and install.

**Tests time out waiting for app to be ready**

Increase `-MaxWaitSeconds` (default 30).  First launch after install may be
slower because of AOT compilation on the device.
