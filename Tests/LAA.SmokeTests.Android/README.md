# LAA Android Smoke Tests

PowerShell script that exercises the main user flows of the LAA MAUI Android
app on a connected device or emulator using **adb + UIAutomator**.
No additional tools required beyond Android SDK platform-tools.

---

## Quick start (when the phone is connected)

```powershell
# 1. Plug in the phone and confirm it's visible
adb devices

# 2. Run all smoke tests
.\Tests\LAA.SmokeTests.Android\SmokeTest-Android.ps1
```

The script auto-detects the device and the installed LAA package.

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

The app must be installed on the device. To install from the repo:

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
| `-MaxWaitSeconds` | `180` | How long to wait for the app to be ready |
| `-KeepAppOpen` | false | Leave the app running after all tests |
| `-ForceFailure` | false | Trigger a test failure for screenshot validation |

---

## Smoke Test Catalog (26 tests)

| # | Test | What it guards |
|---|------|----------------|
| 1 | App launches and Chat page is visible | Core launch & activity initialization |
| 2 | Chat editor is focusable and clickable | Editor accessibility regression |
| 3 | Chat editor is enabled (no overlay blocking input) | Permission dialog / overlay interception |
| 4 | Can type a message and Send button is present | Basic chat interaction & text input pipeline |
| 5 | Navigation to Chats tab works | Shell tab routing to Conversations |
| 6 | Chats page shows conversation list or empty state | Conversations view rendering |
| 7 | New Chat button is present on Chats page | New conversation action accessibility |
| 8 | Navigation to Inbox tab works | Inbox/Knowledge tab routing |
| 9 | Inbox page loads without crashing | Inbox stability & view holder initialization |
| 10 | Navigate back to Chat tab from Inbox | Return navigation and view model reuse |
| 11 | App survives rapid tab cycling without crashing | State leak / crash on fast tab navigation |
| 12 | Back navigation from Chats page does not crash | Native Android back-gesture stability |
| 13 | Ask Coco toolbar toggle is absent on Android | WinUI-only feature isolation guard |
| 14 | Settings page loads cleanly on Android | Settings navigation & Coco section exclusion |
| 15 | Inbox filter chips are visible after navigating to Inbox | Knowledge category filtering (All/Journals/Tasks) |
| 16 | Chat Send button present after returning from Inbox and typing | Input state retention across cross-tab visits |
| 17 | Meal and nutrition command execution in Chat | Mode 2.94 fast-path command submission (`/meal list`) |
| 18 | Memory Management page loads and displays memory action controls | Memory tab load, column headers & clear/refresh actions |
| 19 | Conversation list item swipe reveals Rename and Delete actions | `SwipeView` gesture interaction & context action exposure |
| 20 | Settings page scrolling and Save configuration action | `ScrollView` traversal & Settings persistence pipeline |
| 21 | Soft keyboard layout adjustment and viewport restoration | Keyboard summon (`AdjustResize`), unclipped input & dismissal |
| 22 | Conversation Recorder page loads and displays recording controls | Record tab routing, `RecordToggleButton` & `RecordingsList` rendering |
| 23 | Diagnostic Logs page renders controls and executes test diagnostics | Logs tab routing, Refresh/Export/Clear buttons, diagnostics event generator |
| 24 | Knowledge Inbox search bar interaction | Full-text search bar presence, real-time query filtering in Knowledge Inbox |
| 25 | Settings page displays App Theme selector | Dynamic theme selection (System / Dark / Light) in Settings |
| 26 | Memory Management segmented tabs and layout | Responsive segmented view & provisional memory action controls |

---

## Test Coverage Matrix

| Feature / UI Area | Covered By Tests | Key Validations |
|---|---|---|
| **Chat Interaction & Engine** | 1, 2, 3, 4, 16, 17 | Editor focus, input dispatch, Send button readiness, FastPath command submission |
| **Conversations / History** | 5, 6, 7, 12, 19 | Conversations list load, New Chat button, Back navigation, horizontal `SwipeView` Rename/Delete |
| **Inbox & Knowledge** | 8, 9, 10, 15, 24 | Category filter chips (All/Journals/Tasks), CollectionView rendering, Search bar, Tab return |
| **Memory Management** | 18, 26 | Segmented tabs, Short/Long term cards, Clear & Refresh action buttons, Provisional review actions |
| **Conversation Recorder** | 22 | Record tab routing, Offline Conversation Recorder header, `RecordToggleButton` presence |
| **Settings & Configuration** | 14, 20, 25 | Android layout (Coco hidden), vertical scrolling across sections, Save persistence, App Theme selector |
| **Diagnostic Logging** | 23 | Modern toolbar controls, Level chips, Test Diagnostics runner, Log collection render |
| **Navigation & Stability** | 11, 12, 13 | Rapid Shell tab cycling, Back gestures, Platform conditional UI elements (Coco isolation) |
| **Input & Soft Keyboard** | 21 | Soft keyboard summon, editor viewport retention (`AdjustResize`), clean dismissal |

---

## Device Compatibility Matrix

| Device / Target Profile | Android Version (API) | Architecture | Screen Density / Resolution | Status |
|---|---|---|---|---|
| Physical Flagship (e.g. Galaxy S23/S24, Pixel 8) | Android 14–15 (API 34–35) | `arm64-v8a` | FHD+ / QHD+ (1080x2340 ~ 1440x3120, 420-480 dpi) | Verified |
| Physical Mid-Range / Budget | Android 12–13 (API 31–33) | `arm64-v8a` | HD+ / FHD+ (720x1600 ~ 1080x2400, 280-400 dpi) | Verified |
| Google Pixel Emulator | Android 14 (API 34) | `x86_64` | 1080x2400 (420 dpi) | Verified |
| Android Tablet / Foldable | Android 13–14 (API 33–34) | `arm64-v8a` / `x86_64` | 1600x2560 (280-320 dpi) | Verified |

---

## How it works

The script uses UIAutomator's `uiautomator dump` command to capture the
current UI hierarchy as XML, then queries it via XPath. Taps and gestures are performed
with `adb shell input tap x y` and `adb shell input swipe x1 y1 x2 y2 duration` using bounds extracted from the dump.

On Android, MAUI maps `AutomationId` (set in XAML) to the view's
`contentDescription`, which UIAutomator exposes as the `content-desc`
attribute. The script searches by both `content-desc` (AutomationId) and
`text` (button/label text) for maximum compatibility.

### AutomationId map (Android `content-desc`)

| MAUI AutomationId | Control | Fallback (text search) |
|---|---|---|
| `ChatEditor` | Message input | `class="android.widget.EditText"` |
| `SendButton` | Send Button | `text="Send"` |
| `ClearButton` | Clear Button | `text="Clear"` |
| `NewChatButton` | New Chat Button | `text="New Chat"` |
| `ConversationsList` | Conversations list | `class="android.view.ViewGroup"` |
| `InboxList` | Inbox list | `class="androidx.recyclerview.widget.RecyclerView"` |
| `ClearShorTermButton` | Clear Short Term Memory | `text="Clear Short Term"` |
| `ClearLongTermButton` | Clear Long Term Memory | `text="Clear Long Term"` |
| `RefreshButton` | Refresh Memory Management | `text="Refresh"` |

Tab navigation uses `text` matching against the Shell tab bar items:
`"Chat"`, `"Chats"`, `"Inbox"`, `"Memory"`, `"Logs"`, `"Settings"`.

---

## Failure output

When a test fails the script prints:
1. The failure reason
2. The path to a saved UIAutomator XML dump:
   `%TEMP%\laa_failure_<TestName>.xml`
3. A captured device screenshot saved to the artifacts directory.

Open that file to see the full UI hierarchy at the time of failure.

---

## Known issues / gotchas

**Emulator crashes on launch (SIGBUS / signal 7)**

This is an ABI mismatch — the installed APK was built for arm64-v8a but the
emulator is x86_64 (or vice versa). Rebuild for the target architecture:

```powershell
# For x86_64 emulator
dotnet build LocalAIAssistant.Ui.Maui.csproj -f net9.0-android

# Then deploy via Android Studio or adb install
```

**`adb devices` shows "unauthorized"**

Accept the "Allow USB debugging?" prompt on the phone. If the prompt doesn't
appear, revoke USB debugging authorizations (Developer options) and reconnect.

**App not found (`LAA package not found on device`)**

The package is not installed. See the [Prerequisites](#prerequisites) section
for how to build and install.

**Tests time out waiting for app to be ready**

Increase `-MaxWaitSeconds` (default 180). First launch after install may be
slower because of AOT compilation on the device.
