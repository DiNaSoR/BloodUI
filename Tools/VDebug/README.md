# VDebug

<div align="center">

[![BepInEx](https://img.shields.io/badge/BepInEx-IL2CPP-6b2d5b?style=for-the-badge&logo=unity)](https://github.com/BepInEx/BepInEx)
[![Client + Server](https://img.shields.io/badge/Client%20%2B%20Server-1a1a2e?style=for-the-badge&logo=windows)](.)
[![Optional](https://img.shields.io/badge/Optional-Plugin-2563eb?style=for-the-badge)](.)

**Debug toolkit for V Rising mods**

*Structured logging. ANSI colors. Repeat suppression. Zero noise in production.*

---

[Installation](#installation) • [API Reference](#api-reference) • [Structured Logging](#structured-logging) • [Integration Guide](#integration-guide) • [Configuration](#configuration)

</div>

---

## Features

| Feature | Description |
|---------|-------------|
| **Structured Logging** | Machine-parseable format: `timestamp [level] [source] [category] message key=value` |
| **ANSI Colors** | Colored console output for quick visual scanning (Client=blue, Server=orange) |
| **Repeat Suppression** | Identical messages are collapsed and summarized |
| **Source + Category** | Distinguish Client/Server and subsystems (EclipseSync, Layout, etc.) |
| **Asset Dumping** | Extract UI sprites, fonts, and layout data to disk (client only) |
| **Silent Fallback** | Zero errors when VDebug isn't installed |

```
┌─────────────────┐          ┌─────────────┐          ┌──────────────────┐
│  Your Plugin    │ ──────▶  │   VDebug    │ ──────▶  │  BepInEx Logger  │
│                 │ reflect  │  (format +  │          │  (console/file)  │
│                 │          │  suppress)  │          │                  │
└─────────────────┘          └─────────────┘          └──────────────────┘
```

---

## Installation

VDebug works on both **client** and **server**. On server, UI/panel features are disabled but logging is fully functional.

1. Build `Tools/VDebug/VDebug.csproj`
2. Copy `VDebug.dll` from `bin/Release/net6.0/` to `BepInEx/plugins/`
3. Launch V Rising (client or server)

---

## API Reference

| Property | Value |
|----------|-------|
| **Assembly** | `VDebug` |
| **Namespace** | `VDebug` |
| **Type** | `VDebugApi` |
| **API Version** | `3` |
| **GUID** | `com.dinasor.vdebug` |

### Logging Methods

| Method | Description |
|--------|-------------|
| `LogInfo(string message)` | Log info (no source/category) |
| `LogInfo(string source, string message)` | Log info with source |
| `LogInfo(string source, string category, string message)` | Log info with source + category |
| `LogWarning(...)` | Same overloads for warnings |
| `LogError(...)` | Same overloads for errors |
| `Log(source, category, level, message, context)` | Full structured log with key-value context |

### Asset Dumping (client only)

| Method | Description |
|--------|-------------|
| `DumpMenuAssets()` | Dump UI assets to `BepInEx/VDebug/DebugDumps/` |
| `DumpCharacterMenu()` | Dump character menu hierarchy |
| `DumpHudMenu()` | Dump HUD hierarchy |
| `DumpMainMenu()` | Dump main menu hierarchy |

### Debug Panel (client only)

| Method | Description |
|--------|-------------|
| `ShowDebugPanel()` | Show the debug panel UI |
| `HideDebugPanel()` | Hide the debug panel UI |
| `ToggleDebugPanel()` | Toggle debug panel visibility |

---

## Structured Logging

VDebug outputs logs in a consistent, machine-parseable format:

```
2026-01-17T14:22:03.123Z [I] [Client] [Layout] Loaded layout path=D:\...
2026-01-17T14:22:04.456Z [W] [Server] [EclipseSync] Client version mismatch version=1.3
2026-01-17T14:22:05.789Z [E] [Client] [Network] Connection failed reason=timeout
```

### Format breakdown

```
TIMESTAMP [LEVEL] [SOURCE] [CATEGORY] MESSAGE key=value ...
```

- **TIMESTAMP**: ISO 8601 UTC (`2026-01-17T14:22:03.123Z`)
- **LEVEL**: `I` (Info), `W` (Warning), `E` (Error)
- **SOURCE**: `Client` or `Server`
- **CATEGORY**: Subsystem identifier (e.g., `Layout`, `EclipseSync`, `Network`)
- **MESSAGE**: Human-readable description
- **key=value**: Optional structured context

### Best practices

```csharp
// Good: source + category for easy filtering
VDebugApi.LogInfo("Client", "Layout", "Loaded layout");
VDebugApi.LogWarning("Server", "EclipseSync", $"Version mismatch: {version}");

// Good: full structured with context
VDebugApi.Log("Client", "Network", LogLevel.Error, 
    "Connection failed", ("reason", "timeout"), ("retries", "3"));

// Avoid: embedding tags in message (harder to parse)
VDebugApi.LogInfo("[Client] [Layout] Loaded layout");  // Don't do this
```

---

## Integration Guide

### Option 1: Soft Dependency via DebugToolsBridge (Recommended)

Use the bridge pattern for optional VDebug integration. Your plugin works whether VDebug is installed or not.

```csharp
// In your plugin:
DebugToolsBridge.TryLogInfo("Layout", "Loaded configuration");
DebugToolsBridge.TryLogWarning("Network", $"Retry {count}/3");
DebugToolsBridge.TryLogError("Fatal error occurred");
```

See the reference implementations:
- Client: [`Client/EclipsePlus/Services/DebugToolsBridge.cs`](../../Client/EclipsePlus/Services/DebugToolsBridge.cs)
- Server: [`Server/Bloodcraftplus/Services/DebugToolsBridge.cs`](../../Server/Bloodcraftplus/Services/DebugToolsBridge.cs)

### Option 2: Hard Dependency

Reference `VDebug.dll` directly:

```csharp
using VDebug;
using VDebug.Services;

// Simple
VDebugApi.LogInfo("Hello from my plugin!");

// With source
VDebugApi.LogInfo("Client", "Initialization complete");

// With source + category (recommended)
VDebugApi.LogInfo("Client", "MySystem", "Initialization complete");

// Full structured
VDebugApi.Log("Client", "MySystem", LogLevel.Info,
    "Operation complete", ("duration", "150ms"), ("items", "42"));
```

---

## Configuration

VDebug settings are in `BepInEx/config/com.dinasor.vdebug.cfg`:

### General

| Setting | Default | Description |
|---------|---------|-------------|
| `CustomFontName` | `NotoSansMono-Regular` | Font for debug panel |

### Logging

| Setting | Default | Description |
|---------|---------|-------------|
| `EnableAnsiColors` | `true` | Enable ANSI color codes in console output |
| `RepeatSuppressionEnabled` | `true` | Collapse repeated identical messages |
| `RepeatWindowSeconds` | `2` | Time window for detecting repeats |
| `RepeatFlushSeconds` | `10` | After this duration, emit repeat summary |

### Repeat suppression example

Without suppression:
```
[Info :VDebug] 2026-01-17T14:22:03.123Z [I] [Client] [Network] Heartbeat sent
[Info :VDebug] 2026-01-17T14:22:03.223Z [I] [Client] [Network] Heartbeat sent
[Info :VDebug] 2026-01-17T14:22:03.323Z [I] [Client] [Network] Heartbeat sent
... (47 more times)
```

With suppression (default):
```
[Info :VDebug] 2026-01-17T14:22:03.123Z [I] [Client] [Network] Heartbeat sent
[Info :VDebug] [repeated x47 in last 10s] 2026-01-17T14:22:03.123Z [I] [Client] [Network] Heartbeat sent
```

---

## When VDebug is Missing

| Scenario | Behavior |
|----------|----------|
| VDebug installed | Logs appear in BepInEx console with colors + formatting |
| VDebug not installed | All bridge calls silently no-op |
| Error thrown? | Never—reflection fails gracefully |

This keeps production builds **clean** while enabling deep debugging during development.

---

## Server Mode

VDebug automatically detects server mode (`VRisingServer`) and disables UI/panel features while keeping full logging functionality. Server logs use `[Server]` source tag (orange in ANSI-enabled consoles).

---

<div align="center">

**Made for the V Rising modding community**

</div>
