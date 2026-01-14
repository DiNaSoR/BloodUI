# 🔧 VDebug

<div align="center">

[![BepInEx](https://img.shields.io/badge/BepInEx-IL2CPP-6b2d5b?style=for-the-badge&logo=unity)](https://github.com/BepInEx/BepInEx)
[![Client Only](https://img.shields.io/badge/Client-Only-1a1a2e?style=for-the-badge&logo=windows)](.)
[![Optional](https://img.shields.io/badge/Optional-Plugin-2563eb?style=for-the-badge)](.)

**Debug toolkit for V Rising client mods**

*Quiet in production. Powerful in development.*

---

[Installation](#-installation) • [API Reference](#-api-reference) • [Integration Guide](#-integration-guide)

</div>

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 📝 **Centralized Logging** | Route all plugin logs through a single VDebug log source |
| 🖼️ **Asset Dumping** | Extract UI sprites, fonts, and layout data to disk |
| 🔇 **Silent Fallback** | Zero errors when VDebug isn't installed |

```
┌─────────────────┐          ┌─────────────┐          ┌──────────────────┐
│  Your Plugin    │ ──────▶  │   VDebug    │ ──────▶  │  BepInEx Logger  │
│                 │ reflect  │             │          │  + Asset Dumps   │
└─────────────────┘          └─────────────┘          └──────────────────┘
```

---

## 📦 Installation

> [!NOTE]
> VDebug is **client-only** and will not load on dedicated servers.

1. Build `Tools/VDebug/VDebug.csproj`
2. Copy `VDebug.dll` from `bin/Release/net6.0/` to `BepInEx/plugins/`
3. Launch the V Rising client

---

## 📖 API Reference

| Property | Value |
|----------|-------|
| **Assembly** | `VDebug` |
| **Namespace** | `VDebug` |
| **Type** | `VDebugApi` |
| **GUID** | `com.dinasor.vdebug` |

### Methods

| Method | Description |
|--------|-------------|
| `LogInfo(string)` | Log informational message |
| `LogWarning(string)` | Log warning message |
| `LogError(string)` | Log error message |
| `DumpMenuAssets()` | Dump UI assets to `BepInEx/VDebug/DebugDumps/` |

---

## 🔌 Integration Guide

### Option 1: Soft Dependency (Recommended)

Use reflection to call VDebug—your plugin works whether VDebug is installed or not.

```csharp
using System;
using System.Reflection;

static class VDebugBridge
{
    const string AssemblyName = "VDebug";
    const string ApiTypeName  = "VDebug.VDebugApi";

    public static void LogInfo(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm?.GetName().Name != AssemblyName) continue;

            Type apiType = asm.GetType(ApiTypeName, false);
            apiType?.GetMethod("LogInfo", BindingFlags.Public | BindingFlags.Static)
                   ?.Invoke(null, [message]);
            return;
        }
    }
}
```

> [!TIP]
> See [`Services/DebugToolsBridge.cs`](../../Services/DebugToolsBridge.cs) for a complete implementation with caching.

### Option 2: Hard Dependency

Reference `VDebug.dll` directly and call the API:

```csharp
VDebug.VDebugApi.LogInfo("Hello from my plugin!");
VDebug.VDebugApi.LogWarning("Something might be wrong...");
VDebug.VDebugApi.LogError("Critical failure!");
```

---

## 🔇 When VDebug is Missing

| Scenario | Behavior |
|----------|----------|
| VDebug installed | ✅ Logs appear in BepInEx console |
| VDebug not installed | ✅ All calls silently no-op |
| Error thrown? | ❌ Never—reflection fails gracefully |

This keeps production builds **clean** while enabling deep debugging during development.

---

<div align="center">

**Made for The V Rising modding community**

</div>
