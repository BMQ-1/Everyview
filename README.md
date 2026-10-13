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
[![Release](https://img.shields.io/badge/Release-v1.0.0-0ea5e9)](https://github.com)

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

### 👁️ Quick Look Preview (`Spacebar`) & Details Inspector
- **Instant Floating Preview**: Press `Spacebar` on any file to open a macOS-style Quick Look modal:
  - High-resolution image viewer with dimensions and aspect ratio.
  - Formatted text/code viewer with Cascadia Code monospace font.
  - Video and media metadata viewer.
  - Direct actions to open file, reveal in Explorer, or copy path.
- **Side Details Pane (`Ctrl + P`)**: Detailed file metadata sidebar with SHA-256 cryptographic checksum calculation.

### 🗂️ Dual View Modes
- **Detailed Table View**: Clean columns with sorting for `Name`, `Path`, `Size`, `Date Modified`, and `Type`.
- **Visual Card Grid View**: Modern media-centric card layout with large icons, category badges, and quick-action toolbars.

### 🔄 Auto-Managed Background Daemon & Watchdog
- **Seamless Startup**: Automatically detects and launches `Everything.exe -startup` in silent tray mode if the engine isn't already running.
- **Live Reconnect Watchdog**: Background timer automatically recovers connection as soon as the Everything engine starts up.
- **Elevation / UIPI Detection**: Detects User Interface Privilege Isolation mismatches and advises the user if Everything is running with elevated Administrator privileges.

### 💾 Data Export
- Export search results directly to `CSV`, `JSON`, or formatted `TXT` file lists.

---

## ⌨️ Complete Keyboard Shortcuts Reference

| Shortcut | Action |
| :--- | :--- |
| **`Space`** | Open / Close Quick Look Floating Preview |
| **`Enter`** | Open selected file with default handler |
| **`Shift + Enter`** / **`Ctrl + Enter`** | Reveal selected item in Windows File Explorer |
| **`Ctrl + F`** | Focus search omnibar and select text |
| **`Ctrl + C`** | Copy selected files (`FileDrop` for Explorer paste + Text Paths) |
| **`Ctrl + Shift + C`** | Copy selected files as quoted paths (`"C:\..."`) |
| **`Delete`** | Safely move selected items to Windows Recycle Bin |
| **`Alt + C`** | Toggle Match Case search modifier |
| **`Alt + W`** | Toggle Match Whole Word search modifier |
| **`Alt + P`** | Toggle Match Path search modifier |
| **`Alt + R`** | Toggle Regular Expression search modifier |
| **`Alt + A`** | Toggle Live Disk Analytics & Insights strip |
| **`F1`** | Toggle Interactive Syntax Cheatsheet popup |
| **`Ctrl + P`** | Toggle Details Inspector side panel |
| **`F5`** | Force refresh current search query |
| **`Esc`** | Dismiss Quick Look / Syntax Cheatsheet / Clear search |
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

## 📜 License & Credits

- **Conception & Architectural Direction**: Guided, prompted, and steered by the repository author.
- **Search Engine**: Powered by the legendary [Voidtools Everything](https://www.voidtools.com) SDK.
- **UI Framework**: Powered by [WPF-UI](https://github.com/lepoco/wpfui) & Microsoft CommunityToolkit.MVVM.
- **License**: MIT License. Free for personal and commercial use. Built with ❤️ for Windows power users.
