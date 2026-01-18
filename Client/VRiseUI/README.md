# VRiseUI

**Universal V Rising Mod UI Framework**

Created by DiNaSoR

---

## Overview

VRiseUI is a manifest-driven UI framework for V Rising mods. It allows mod authors (or community contributors) to define HUD elements and menus via JSON manifests, without touching the game code.

## Features

- 📦 **Manifest-based Adapters** - Define UI in JSON, not code
- 🎨 **Multiple Component Types** - Progress bars, labels, buttons, panels
- 💾 **Persistent Layouts** - User positions saved and restored
- 🔌 **Plugin Architecture** - Add new mods via adapter folders
- 📡 **Command Bridge** - Send chat commands, parse responses

## Installation

1. Install BepInEx 6 for V Rising
2. Copy `VRiseUI.dll` to `BepInEx/plugins/`
3. Copy adapter folders to `BepInEx/plugins/VRiseUI/Adapters/`

## Creating an Adapter

Create a folder in `Adapters/` with:

```
Adapters/
└── MyMod/
    ├── manifest.json    # UI definition
    └── assets/          # Icons, backgrounds
```

### Example manifest.json

```json
{
  "version": "1.0",
  "modId": "MyMod",
  "displayName": "My Mod UI",
  
  "hud": {
    "elements": [
      {
        "id": "health",
        "type": "progressBar",
        "label": "HP",
        "position": { "x": 100, "y": 50 },
        "size": { "width": 200, "height": 24 },
        "style": {
          "foregroundColor": "#ff4444"
        }
      }
    ]
  }
}
```

## Component Types

| Type | Description |
|------|-------------|
| `progressBar` | Horizontal fill bar with label |
| `label` | Text display |
| `button` | Clickable, sends commands |
| `panel` | Container for other elements |

## Building from Source

```bash
dotnet build -c Release
```

## License

MIT License - See LICENSE file

---

*Part of the BloodCraftPlus ecosystem*
