<div align="center">

<img src="Assets/app.png" alt="Everyview Logo" width="128" height="128" />

# Everyview

**Ultra-fast, modern, and visually stunning Windows 11 desktop search suite powered by Voidtools Everything.**

[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%2010-0078D4?logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/Design-WPF--UI%20Fluent%20Mica-238636)](https://github.com/lepoco/wpfui)
[![Engine](https://img.shields.io/badge/Powered%20By-Voidtools%20Everything-FFB900)](https://www.voidtools.com)
[![Development](https://img.shields.io/badge/Development-Human--Guided%20AI-8A2BE2?logo=sparkles&logoColor=white)](https://github.com)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Release](https://img.shields.io/badge/Release-v1.1.0-0ea5e9)](https://github.com)

</div>

---

> [!NOTE]
> **Crafted by Human Vision, Powered by AI**  
> Everyview was envisioned, architected, and steered by the repository creator, with the implementation built collaboratively alongside AI under direct human guidance.

---

## ⚡ Overview

**Everyview** transforms the raw, unmatched speed of [Voidtools Everything](https://www.voidtools.com) into a state-of-the-art Windows 11 power search suite. It marries sub-millisecond NTFS/ReFS indexing with a modern Fluent 2 Mica glass design, interactive drive scope switcher pills, native search modifiers, live disk analytics, scale capacity up to 100,000+ items, multi-selection shell file drops, Quick Look spacebar previews, and comprehensive developer workflows.

---

## ✨ Key Features

### 💾 Interactive Drive Scope Bar (Instant Full-Drive Scans)
- **1-Click Drive Scope Pills**: Dedicated interactive pills for `[ 🌐 All Drives ]`, `[ 💾 C: ]`, `[ 💾 D: ]`, displaying live free space badges (e.g. `60.3 GB free`).
- **Complete Drive Scanning**: Clicking any drive pill instantly triggers Voidtools native drive query syntax (`c:`), immediately scanning and populating the **entire drive** across all subdirectories with zero delays.
- **Instant Drive Metrics**: Real-time drive metadata badge displayed in the analytics strip: volume label, file system (NTFS/ReFS), free space, total capacity, and usage percentage (`Local Disk (C:) • NTFS • 60.3 GB Free of 510.7 GB (88% used)`).
- **Open in Explorer**: Instant one-click action to reveal the active drive in Windows File Explorer.

### 🔍 Advanced Power Search Omnibar & Modifiers
- **Zero-Lag Responsiveness**: Clicks on drive pills, category filters, modifiers, and sort columns execute **instantaneously (0ms debounce)**. Search typing is debounced at an ultra-crisp 18ms for fluid as-you-type responsiveness.
- **Active Scanning Pulse**: Sleek neon cyan animated glow progress line under the omnibar indicating background query activity.
- **Native Search Modifiers**:
  - `Aa` **Match Case** (`Alt + C`)
  - `" "` **Match Whole Word** (`Alt + W`)
  - `📁` **Match Path** (`Alt + P`)
  - `.*` **Regular Expressions** (`Alt + R`)
- **Interactive Syntax Cheatsheet (`F1`)**: Floating assistant featuring 1-click filter chips (`size:>1gb`, `dm:today`, `folder:`, `file:`, `dupe:name`, `ext:...`).
- **Smart Filter Pills & Presets**: One-click filters for `⚡ All`, `📁 Folders`, `📄 Documents`, `🖼️ Pictures`, `🎬 Videos`, `🎵 Audio`, `💻 Code`, `📦 Archives`, `⚙️ Apps`, plus presets like `🔥 Large (>100MB)` and `🕒 Modified Today`.

### 📊 Real-Time Disk Analytics & Insights (`Alt + A`)
- **Live Capacity Bar**: Real-time aggregate byte size calculations across active search results (e.g. `14.2 GB across 5,000 items`).
- **File vs. Folder Breakdown**: Instant breakdown of matched file and folder counts.
- **Heaviest File Identifier**: Highlights the largest file in the current query with its exact size.
- **Top Extension Badges**: Visual distribution pills showing the most common extensions found in your results.

### 🚀 Scale & High-Throughput Performance
- **Bulk Collection Pipeline (`ObservableRangeCollection`)**: Replaced individual item insertion with unified range notifications, loading 25,000 to 100,000 results in milliseconds with zero UI stutter.
- **Adjustable Scale Limits**: Effortlessly toggle between `5k`, `25k`, `50k`, and `100k` maximum items.
- **Virtualized Pixel Scrolling**: WPF UI virtualization configured for smooth pixel-by-pixel scrolling even with tens of thousands of items loaded.
- **Zero-Freeze Microsecond Latency**: Query execution times typically range between `1 ms` to `8 ms`.

### 📋 Multi-Selection & Shell Clipboard Integration
- **Extended Selection**: Select single or multiple items using `Shift + Click` or `Ctrl + Click`.
- **Live Selection Badge**: Status bar indicates selected count and combined size in real-time (`12 items selected • 1.4 GB`).
- **Native Windows Explorer FileDrop**: Pressing `Ctrl + C` copies both plain-text paths and native Windows `FileDrop` objects—allowing you to paste files directly into Windows Explorer, VS Code, Discord, Slack, or email clients.
- **Batch File Operations**: Context menu actions to copy paths, copy quoted paths (`"C:\..."`), delete to Recycle Bin, or inspect properties for all selected items at once.

### 🖱️ Native Drag-and-Drop File Export
- **Drag Anywhere**: Click and drag any selected file(s) or folder(s) directly out of Everyview into Windows Explorer, Desktop, VS Code, Discord, Slack, Outlook, or browser upload dialogs.
- **Multi-File Dropping**: Dragging multiple selected items packages all files simultaneously using standard Windows `CF_HDROP` (`DataFormats.FileDrop`).

### ✏️ In-Place File & Folder Rename (`F2`)
- **Instant Modal Dialog**: Press `F2` or click Rename to open a sleek Fluent modal overlay with pre-populated name and extension.
- **Safety Validation**: Validates reserved characters (`\ / : * ? " < > |`) and existing collisions.
- **Live UI Sync**: Immediately updates the result row, full path, and inspector without requiring a full re-index.

### 🕒 Search History & ⭐️ Bookmarks
- **Search History (`Ctrl + H`)**: Automatically records queries with thread-safe persistence in `%LOCALAPPDATA%\Everyview\user_settings.json`. Click any past search chip to re-run instantly, or clear history in 1 click.
- **Saved Searches & Bookmarks (`Ctrl + B` / `Ctrl + D`)**: Save frequent complex queries (e.g. `ext:png;jpg size:>10mb`, `dupe:name;size`) with custom titles and icons. Run with 1 click from the omnibar or bookmarks popover.

### 🔍 Duplicate & Empty Folder Finders
- **Find Duplicates (`dupe:name;size`)**: 1-click preset identifies duplicate files matching both filename and byte-for-byte size.
- **Find Empty Folders (`empty: folder:`)**: 1-click preset spots zero-byte empty directories to help clean up messy disk volumes.

### 👁️ Supercharged Quick Look (`Spacebar`) & Inspector
- **Smart Text Sniffing**: Inspects code files, configuration files (`.rs`, `.go`, `.ts`, `.vue`, `.yaml`, `.toml`, `.env`, etc.), and extensionless files (`LICENSE`, `Dockerfile`, `Makefile`) with Cascadia Code monospace font and formatted line numbers (`line | text`).
- **Audio File Inspector**: Dedicated card for `.mp3`, `.wav`, `.flac`, `.ogg`, `.m4a`, `.aac` with playback prompt.
- **16-Column Binary Hex Dump**: Formatted binary hex inspector for unparsed binaries with byte offsets, hex values, and ASCII side-column.
- **Folder Content Breakdown**: Shows subfolder and subfile counts plus total item statistics.
- **MD5 & SHA-256 Checksums**: Asynchronously compute both MD5 and SHA-256 cryptographic hashes on demand.
- **Instant File Attributes**: View NTFS attributes (`R`, `H`, `S`, `A`, `C`, `E`) directly from Everything's memory cache with zero disk I/O.

### 🌐 Global Hotkey & System Tray
- **Global Summon (`Ctrl + Alt + F` / `Alt + Space`)**: Press from anywhere in Windows to bring Everyview to the foreground and focus the search box.
- **Pure Win32 System Tray**: Minimize to Windows Notification Area with click-to-summon, double-click restore, and native tray menu without heavy WinForms dependencies.
- **Density Switch**: Toggle between Compact (26px) for 40% more visible results and Comfortable (34px) viewing mode.

### 📋 Clipboard Power Actions
- **Copy File as URL**: Copies `file:///C:/...` formatted URL for web applications and browser address bars.
- **Copy Directory Path**: Copies the parent folder path directly.
- **Permanent Delete (`Shift + Delete`)**: Bypass Recycle Bin with a confirmation safety prompt.

---

## ⌨️ Complete Keyboard Shortcuts Reference

| Shortcut | Action |
| :--- | :--- |
| **`Ctrl + Alt + F`** | Global Summon: Bring Everyview to front from anywhere |
| **`Space`** | Open / Close Quick Look Floating Preview (Text, Audio, Hex, Images) |
| **`F2`** | In-place Rename selected file or folder |
| **`Enter`** | Open selected file with default handler |
| **`Shift + Enter`** / **`Ctrl + Enter`** | Reveal selected item in Windows File Explorer |
| **`Ctrl + F`** | Focus search omnibar and select text |
| **`Ctrl + H`** | Open Search History popover |
| **`Ctrl + B`** | Open Saved Searches & Bookmarks popover |
| **`Ctrl + D`** | Bookmark current search query |
| **`Ctrl + C`** | Copy selected files (`FileDrop` for Explorer paste + Text Paths) |
| **`Ctrl + Shift + C`** | Copy selected files as quoted paths (`"C:\..."`) |
| **`Delete`** | Safely move selected items to Windows Recycle Bin |
| **`Shift + Delete`** | Permanently delete selected items (bypass Recycle Bin) |
| **`Alt + C`** | Toggle Match Case search modifier |
| **`Alt + W`** | Toggle Match Whole Word search modifier |
| **`Alt + P`** | Toggle Match Path search modifier |
| **`Alt + R`** | Toggle Regular Expression search modifier |
| **`Alt + A`** | Toggle Live Disk Analytics & Insights strip |
| **`F1`** | Toggle Interactive Syntax Cheatsheet popup |
| **`Ctrl + P`** | Toggle Details Inspector side panel |
| **`F5`** | Force refresh current search query |
| **`Esc`** | Dismiss Modals / Quick Look / History / Bookmarks / Clear search |
| **`Alt + Enter`** | Open native Windows File Properties dialog |

---

## 🛠️ Architecture & Tech Stack

- **Target Framework**: `.NET 10.0 (Windows Desktop)`
- **UI Architecture**: WPF with **WPF-UI 4.x** (Fluent 2 Design System, Mica glass, Segoe Fluent Icons)
- **MVVM Framework**: `CommunityToolkit.Mvvm 8.4.0` (Source Generators)
- **Search Engine**: Voidtools Everything IPC API (`Everything64.dll` & `WM_COPYDATA` inter-process protocol)
- **Shell Interop**:
  - `SHFileOperation` for shell Recycle Bin deletion with undo support
  - `ShellExecuteEx` for native Properties sheet display
  - `SHGetFileInfo` for native high-resolution system icons
  - `IDataObject` multi-format clipboard packaging (`FileDrop` + `UnicodeText`)

---

## 🚀 Getting Started

### Prerequisites
* Windows 10 (version 1809+) or Windows 11 (recommended for Mica backdrop)
* [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (or .NET 10 SDK)
* [Voidtools Everything](https://www.voidtools.com) installed (Standard or Portable)

### Launching the Application
Double-click **`run.bat`** in the repository root, or launch from PowerShell:
```powershell
.\bin\Release\net10.0-windows\Everyview.exe
```

### Building from Source
```powershell
# Clone the repository
git clone https://github.com/<your-username>/everyview.git
cd everyview

# Build Release binary
dotnet build -c Release
```

The compiled standalone binary is placed in:
`bin\Release\net10.0-windows\Everyview.exe`

---

## 📦 GitHub Releases (v1.1)

| Asset | Size | Type | SHA-256 Checksum |
| :--- | :--- | :--- | :--- |
| **`Everyview-v1.1-Windows-x64.zip`** | ~61 MB | Standalone Portable (Zero Dependencies, Self-Contained) | `CADE37B4A6E893BE1544FBCD3445F0E352DAFAA124F4B632A2C8F9AE4ECD9D07` |

*Contains single-file `Everyview.exe`, `Everything64.dll`, `run.bat`, `README.md`, and `LICENSE`.*

---

## 🔧 Voidtools Everything Engine Architecture & Multi-Instance Handling

Voidtools Everything utilizes a two-tier architecture:
1. **Everything Service** (`Session 0`, `NT AUTHORITY\SYSTEM`): Runs as a lightweight Windows service to read low-level NTFS USN change journals without requiring user elevation.
2. **Everything Desktop Client** (`Session 1+`, active user session): Holds the in-memory filename database and hosts the `EVERYTHING_TASKBAR_NOTIFICATION` Win32 message window for ultra-low latency IPC queries.

### Why Everyview v1.1 handles this seamlessly:
- **Session-Aware Launch Guard (`run.bat`)**: Checks for active Everything processes in the current user session (`SESSION ne 0`) before launching. Never spawns redundant background processes if Everything is already running.
- **Automatic Duplicate Instance Cleanup (`CleanupDuplicateInstances`)**: If orphan desktop clients exist in the user's session, Everyview automatically terminates the duplicates, preserving the primary process holding the active memory index.
- **Fast-Path IPC & Parallel Splash Warmup**: Everyview warms up and binds to the Everything engine during the splash screen, verifying `Everything_IsDBLoaded()` so the search interface opens connected with 0 ms query response time.

---

## 📜 License & Credits

- **Conception & Architectural Direction**: Guided, prompted, and steered by the repository author.
- **Search Engine**: Powered by the legendary [Voidtools Everything](https://www.voidtools.com) SDK.
- **UI Framework**: Powered by [WPF-UI](https://github.com/lepoco/wpfui) & Microsoft CommunityToolkit.MVVM.
- **License**: MIT License. Free for personal and commercial use. Built with ❤️ for Windows power users.

