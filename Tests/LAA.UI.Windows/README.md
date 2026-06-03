# LAA Windows UI Smoke Tests

xUnit tests for the Local AI Assistant MAUI app on Windows, driven via
**FlaUI** (Windows UI Automation 3 — no external server required).

> **Quick smoke run:** use the PowerShell script instead of `dotnet test`:
> `.\Tests\LAA.SmokeTests.Windows\SmokeTest-Windows.ps1`
> It produces the same 12 pass/fail results and needs no build step.

---

## How it works

FlaUI wraps the built-in Windows UIA3 API.  Tests launch the LAA exe
directly, discover controls by their **AutomationId** (set in XAML), and
interact with them via UIA patterns (InvokePattern, ValuePattern,
SelectionItemPattern).  No WinAppDriver server, no Appium — just .NET and
the Windows accessibility layer.

---

## Prerequisites

### 1. Build the LAA Windows app

From the repo root:

```powershell
dotnet build LocalAIAssistant.Ui.Maui.csproj -f net9.0-windows10.0.19041.0
```

The test runner auto-discovers the exe at:

```
bin\Debug\net9.0-windows10.0.19041.0\win10-x64\LocalAIAssistant.Ui.Maui.exe
```

To point to a different build, set the environment variable before running tests:

```powershell
$env:LAA_EXE_PATH = "C:\path\to\LocalAIAssistant.Ui.Maui.exe"
```

### 2. Windows Developer Mode (for UIA access to WinUI 3 apps)

UIA3 can inspect WinUI 3 apps without Developer Mode on most machines.
If tests fail to locate the app window, enable Developer Mode:

```
Settings → Privacy & Security → For developers → Developer Mode → On
```

---

## Running the tests

```powershell
dotnet test Tests\LAA.UI.Windows\LAA.UI.Windows.csproj
```

Run a single test by name:

```powershell
dotnet test --filter "FullyQualifiedName~Chat_Editor_Is_Enabled"
```

Run with verbose output:

```powershell
dotnet test -v normal
```

---

## How the tests work

| Mechanism | Detail |
|-----------|--------|
| **Session management** | `AppFixture` (IClassFixture) — one FlaUI session per test class; app launched once, torn down after the last test |
| **Element location** | `FindFirstDescendant(cf => cf.ByAutomationId(...))` — matches MAUI `AutomationId` attributes |
| **Tab navigation** | `FindFirstDescendant(cf => cf.ByName(tabTitle))` — matches Shell tab title text |
| **Wait strategy** | `AppFixture.FindById()` polls every 300 ms; `WaitForElement()` throws `TimeoutException` on failure |
| **Baseline reset** | Each test calls `TryNavigateTo("Chat")` in its constructor so tests are order-independent |

---

## Smoke test coverage (10 tests)

| # | Test | What it guards |
|---|------|----------------|
| 1 | App launches — Chat page visible | Core launch regression |
| 2 | ChatEditor enabled and not blocked | Overlay/permission interception regression |
| 3 | Editor accepts text input | Basic input pipeline |
| 4 | Send button present and enabled | Chat send action |
| 5 | Clear button present and enabled | Clear action |
| 6 | Navigate to Chats tab | Shell tab routing |
| 7 | Chats page shows list or empty state | Conversations page loads |
| 8 | Navigate to Inbox tab | Inbox tab routing |
| 9 | Inbox page loads without crashing | Inbox stability |
| 10 | Navigate back to Chat from Inbox | Return navigation |
| 11 | Rapid tab cycling does not crash | State leak / crash on fast nav |

---

## AutomationId map

The XAML controls targeted by these tests:

| AutomationId | Control | Page |
|---|---|---|
| `ChatEditor` | Message input Editor | MainPage |
| `SendButton` | Send Button | MainPage |
| `ClearButton` | Clear Button | MainPage |
| `MessagesView` | Messages CollectionView | MainPage |
| `NewChatButton` | New Chat Button | ConversationsPage |
| `ConversationsList` | Conversations CollectionView | ConversationsPage |
| `InboxList` | Inbox CollectionView | KnowledgeInboxPage |
| `AppTitle` | Title Label | AppShell |

---

## Troubleshooting

**`TimeoutException: Element 'ChatEditor' not found within 30s`**
- The app may still be on the splash screen.  Increase `MaxWaitSeconds` via
  the constructor or check that the exe path is correct.
- Ensure you built after the AutomationId XAML changes were made
  (`dotnet build -f net9.0-windows10.0.19041.0`).

**`FileNotFoundException: Could not find LocalAIAssistant.Ui.Maui.exe`**
- Build the Windows target first.
- Or set `$env:LAA_EXE_PATH` to the full exe path.

**FlaUI can't see controls inside the app**
- MAUI on Windows uses a `DesktopChildSiteBridge` (WinUI 3 island).
  FlaUI.UIA3 traverses through this boundary correctly.
- If controls are still not found, verify the MAUI AutomationId properties
  are set in XAML (see the table above) and the build is current.
