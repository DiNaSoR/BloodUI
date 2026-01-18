# VRiseUI - Universal V Rising Mod UI Framework

## Project Vision

**VRiseUI** is a universal client-side UI framework for V Rising that supports multiple server mods through a manifest-based adapter system. It includes a web-based visual designer for pixel-perfect UI creation.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         VRiseUI Ecosystem                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌──────────────────┐    ┌──────────────────┐    ┌────────────────┐ │
│  │  VRiseUI.dll     │    │  Web Designer    │    │  Adapter Packs │ │
│  │  (BepInEx Plugin)│    │  (vriseui.com)   │    │  (Community)   │ │
│  └────────┬─────────┘    └────────┬─────────┘    └───────┬────────┘ │
│           │                       │                      │          │
│           ▼                       ▼                      ▼          │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │                    Manifest System                              ││
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ ││
│  │  │ BloodCraft  │ │ BloodyBoss  │ │ BloodyShop  │ │  Custom   │ ││
│  │  │ .manifest   │ │ .manifest   │ │ .manifest   │ │ .manifest │ ││
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ ││
│  └─────────────────────────────────────────────────────────────────┘│
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Component 1: VRiseUI Core Plugin

### File Structure

```
BepInEx/plugins/VRiseUI/
├── VRiseUI.dll                    # Main plugin
├── config.json                    # Global settings
└── Adapters/
    ├── BloodCraft/
    │   ├── manifest.json          # UI layout definition
    │   └── assets/
    │       ├── xp_icon.png
    │       ├── expertise_bar.png
    │       └── legacy_icon.png
    ├── BloodyBoss/
    │   ├── manifest.json
    │   └── assets/
    │       └── boss_icon.png
    └── Custom/                    # User's custom layouts
        └── ...
```

### Core Classes

```csharp
namespace VRiseUI
{
    // Plugin entry point
    public class VRiseUIPlugin : BasePlugin
    {
        public override void Load()
        {
            // 1. Load global config
            // 2. Discover adapters in Adapters/ folder
            // 3. Initialize UI renderer
            // 4. Hook into game UI
        }
    }
    
    // Manifest loader
    public class AdapterManager
    {
        public List<ModAdapter> LoadedAdapters { get; }
        public void DiscoverAdapters(string path);
        public void LoadManifest(string manifestPath);
        public void UnloadAdapter(string modId);
    }
    
    // UI Renderer
    public class UIRenderer
    {
        public void RenderHUD(List<HUDElement> elements);
        public void RenderMenu(MenuDefinition menu);
        public Texture2D LoadAsset(string path);
    }
    
    // Command Bridge
    public class CommandBridge
    {
        public void SendCommand(string command);
        public event Action<string> OnServerResponse;
        public void ParseResponse(string response, string pattern);
    }
}
```

### Key Features

| Feature | Description |
|---------|-------------|
| **Hot-reload** | Change manifest → UI updates live |
| **Asset loading** | Load PNG/JPG from adapter folders |
| **Drag & drop** | Users can reposition elements in-game |
| **Persistence** | Save user positions per profile |
| **Multi-adapter** | Load multiple mod adapters simultaneously |

---

## Component 2: Manifest Format Specification

### manifest.json Structure

```json
{
  "version": "1.0",
  "modId": "BloodCraft",
  "modVersion": "1.12.x",
  "author": "DiNaSoR",
  "displayName": "BloodCraft UI",
  "description": "HUD and menus for BloodCraft RPG mod",
  
  "detection": {
    "method": "command",
    "command": ".bc",
    "expectedResponse": "BloodCraft"
  },
  
  "hud": {
    "elements": [
      {
        "id": "experience_bar",
        "type": "progressBar",
        "label": "Experience",
        "position": { "x": 100, "y": 50, "anchor": "topLeft" },
        "size": { "width": 200, "height": 20 },
        "style": {
          "backgroundColor": "#1a1a1a",
          "foregroundColor": "#4a9eff",
          "borderColor": "#333333",
          "borderWidth": 1,
          "cornerRadius": 4
        },
        "icon": "assets/xp_icon.png",
        "dataSource": {
          "command": ".xp",
          "pattern": "Level (\\d+) \\((\\d+)/(\\d+)\\)",
          "mapping": {
            "current": "$2",
            "max": "$3",
            "label": "Lv.$1"
          },
          "refreshInterval": 5000
        }
      },
      {
        "id": "expertise_bar",
        "type": "progressBar",
        "label": "Expertise",
        "position": { "x": 100, "y": 75, "anchor": "topLeft" },
        "size": { "width": 200, "height": 20 },
        "style": {
          "backgroundColor": "#1a1a1a",
          "foregroundColor": "#ff9f43",
          "borderColor": "#333333"
        },
        "icon": "assets/expertise_icon.png",
        "dataSource": {
          "command": ".exp",
          "pattern": "(\\w+) \\[(\\d+)%\\]",
          "mapping": {
            "current": "$2",
            "max": "100",
            "label": "$1"
          }
        }
      }
    ]
  },
  
  "menus": [
    {
      "id": "familiars_menu",
      "type": "panel",
      "title": "Familiars",
      "hotkey": "F5",
      "position": { "x": "center", "y": "center" },
      "size": { "width": 600, "height": 400 },
      "tabs": [
        {
          "id": "list",
          "label": "My Familiars",
          "content": {
            "type": "list",
            "dataSource": {
              "command": ".fam l",
              "pattern": "(\\d+)\\. (.+)",
              "perItem": {
                "index": "$1",
                "name": "$2"
              }
            },
            "actions": [
              {
                "label": "Bind",
                "command": ".fam b {index}"
              },
              {
                "label": "Toggle",
                "command": ".fam t"
              }
            ]
          }
        },
        {
          "id": "talents",
          "label": "Talents",
          "content": {
            "type": "custom",
            "component": "TalentTree"
          }
        }
      ]
    }
  ],
  
  "notifications": {
    "patterns": [
      {
        "match": "You have leveled up!",
        "style": "celebration",
        "sound": "levelup.wav"
      },
      {
        "match": "Familiar .+ has leveled up",
        "style": "info"
      }
    ]
  }
}
```

### Manifest Element Types

| Type | Description | Properties |
|------|-------------|------------|
| `progressBar` | Horizontal/vertical progress | current, max, color |
| `label` | Text display | text, fontSize, color |
| `button` | Clickable action | command, label, icon |
| `panel` | Container | children, tabs |
| `list` | Scrollable list | items, itemTemplate |
| `grid` | Grid layout | columns, items |
| `image` | Static image | src, size |
| `custom` | Special component | component name |

### Data Source Patterns

```json
{
  "dataSource": {
    "command": ".xp",                    // Chat command to send
    "pattern": "Level (\\d+) \\((\\d+)/(\\d+)\\)",  // Regex to parse response
    "mapping": {
      "level": "$1",
      "current": "$2",
      "max": "$3"
    },
    "refreshInterval": 5000,             // ms between updates
    "cacheTime": 1000                    // ms to cache response
  }
}
```

---

## Component 3: Web-Based Designer

### URL: designer.vriseui.com

### Features

| Feature | Description |
|---------|-------------|
| **Visual Canvas** | 1920x1080 game screen preview |
| **Drag & Drop** | Place components visually |
| **Property Editor** | Edit position, size, colors |
| **Image Upload** | Upload custom icons/backgrounds |
| **Command Tester** | Test regex patterns |
| **Live Preview** | See changes in real-time |
| **Export** | Download as .zip with manifest + assets |
| **Templates** | Pre-made layouts to start from |
| **Sharing** | Share layouts with community |

### Tech Stack

```
Frontend:
├── React 18
├── TypeScript
├── TailwindCSS
├── React DnD (drag & drop)
├── Monaco Editor (JSON editing)
└── Konva.js (canvas rendering)

Backend (optional):
├── Node.js / Express
├── MongoDB (for sharing)
└── Cloudinary (image hosting)
```

### UI Mockup

```
┌─────────────────────────────────────────────────────────────────────┐
│ VRiseUI Designer                          [Import] [Export] [Share] │
├──────────────────┬──────────────────────────────────────────────────┤
│ COMPONENTS       │                                                  │
│ ─────────────    │     ┌─────────────────────────────────────┐      │
│ [+ Progress Bar] │     │        Game Screen Preview          │      │
│ [+ Label       ] │     │                                     │      │
│ [+ Button      ] │     │  ┌──────────────────┐               │      │
│ [+ Panel       ] │     │  │ ▓▓▓▓▓▓▓░░ 75%   │ ← dragging    │      │
│ [+ Image       ] │     │  └──────────────────┘               │      │
│                  │     │                                     │      │
│ ASSETS           │     │                                     │      │
│ ─────────────    │     │                                     │      │
│ 📁 Upload Image  │     │                                     │      │
│ ├── xp.png       │     └─────────────────────────────────────┘      │
│ └── sword.png    │                                                  │
│                  ├──────────────────────────────────────────────────┤
│ LAYERS           │ PROPERTIES                                       │
│ ─────────────    │ ────────────                                     │
│ ○ experience_bar │ ID: experience_bar                               │
│ ○ expertise_bar  │ Type: progressBar                                │
│ ○ legacy_bar     │ Position: X[100] Y[50]                           │
│                  │ Size: W[200] H[20]                                │
│                  │ Command: [.xp           ]                        │
│                  │ Pattern: [Level (\d+)...]                        │
└──────────────────┴──────────────────────────────────────────────────┘
```

---

## Component 4: Built-in Adapters

### Priority 1 - Official Adapters (You Create)

| Mod | Priority | Complexity |
|-----|----------|------------|
| BloodCraft | 🔴 High | Complex - many features |
| BloodyBoss | 🟡 Medium | Boss timers, loot |
| BloodyShop | 🟡 Medium | Store UI |
| BloodyWallet | 🟢 Low | Currency display |

### Priority 2 - Community Adapters

| Mod | Notes |
|-----|-------|
| LeaderBoard | Stats display |
| BloodyNotify | Notifications |
| KindredCommands | Admin panels |
| etc. | Community contributes |

---

## Implementation Phases

### Phase 1: Core Plugin (2-3 weeks)

```
Week 1:
├── [ ] Project setup (BepInEx plugin)
├── [ ] Manifest loader
├── [ ] Asset loader (PNG/JPG)
└── [ ] Basic UI renderer

Week 2:
├── [ ] HUD element types (bar, label, image)
├── [ ] Command bridge (send/receive chat)
├── [ ] Response parsing (regex)
└── [ ] Position persistence

Week 3:
├── [ ] In-game drag & drop
├── [ ] Hot-reload manifests
├── [ ] Config system
└── [ ] Testing with BloodCraft
```

### Phase 2: BloodCraft Adapter (1 week)

```
├── [ ] Experience bar
├── [ ] Expertise bar
├── [ ] Legacy bar
├── [ ] Familiar bar
├── [ ] Quest tracker
├── [ ] Character menu
│   ├── [ ] Classes tab
│   ├── [ ] Familiars tab
│   ├── [ ] Talents panel
│   └── [ ] Prestige tab
└── [ ] Testing & polish
```

### Phase 3: Web Designer (2-3 weeks)

```
Week 1:
├── [ ] React project setup
├── [ ] Canvas component
├── [ ] Drag & drop system
└── [ ] Property editor

Week 2:
├── [ ] Component palette
├── [ ] Image upload
├── [ ] Export to JSON/ZIP
└── [ ] Import existing manifests

Week 3:
├── [ ] Template system
├── [ ] Command tester
├── [ ] Documentation
└── [ ] Deploy to vriseui.com
```

### Phase 4: Community & Growth

```
├── [ ] GitHub organization setup
├── [ ] Documentation site
├── [ ] Discord community
├── [ ] Adapter submission system
├── [ ] Featured layouts gallery
└── [ ] Thunderstore package
```

---

## File Locations

### Client Plugin

```
BloodCraftPlus/
└── Client/
    └── VRiseUI/
        ├── VRiseUI.csproj
        ├── VRiseUI.sln
        ├── Plugin.cs
        ├── Core/
        │   ├── AdapterManager.cs
        │   ├── ManifestLoader.cs
        │   ├── AssetLoader.cs
        │   └── CommandBridge.cs
        ├── UI/
        │   ├── UIRenderer.cs
        │   ├── HUDController.cs
        │   ├── MenuController.cs
        │   └── Elements/
        │       ├── ProgressBar.cs
        │       ├── Label.cs
        │       ├── Button.cs
        │       └── Panel.cs
        ├── Models/
        │   ├── Manifest.cs
        │   ├── HUDElement.cs
        │   └── DataSource.cs
        └── Resources/
            └── default.css
```

### Web Designer

```
vriseui-designer/
├── package.json
├── src/
│   ├── App.tsx
│   ├── components/
│   │   ├── Canvas.tsx
│   │   ├── Palette.tsx
│   │   ├── PropertyEditor.tsx
│   │   ├── LayerPanel.tsx
│   │   └── ExportModal.tsx
│   ├── models/
│   │   └── manifest.ts
│   └── utils/
│       ├── export.ts
│       └── patterns.ts
└── public/
    └── templates/
        └── bloodcraft-default.json
```

---

## Success Metrics

| Metric | Target |
|--------|--------|
| Thunderstore downloads | 1000+ first month |
| Community adapters | 5+ in 3 months |
| Discord members | 100+ |
| Web designer users | 500+ unique |

---

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Game updates break UI | Abstract game hooks, quick patches |
| Mods change command format | Version detection in manifest |
| Performance issues | Lazy loading, caching |
| Community adoption | Great documentation, templates |

---

## Next Steps

1. [ ] Create `Client/VRiseUI/` folder structure
2. [ ] Initialize BepInEx plugin project
3. [ ] Define manifest JSON schema
4. [ ] Build minimal working prototype
5. [ ] Test with BloodCraft commands

---

*This plan is a living document. Update as implementation progresses.*

---

# APPENDIX A: Super Detailed Plugin Architecture

## Design Principles

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      VRiseUI Design Principles                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  🎯 MODULAR         - Every feature is a pluggable module               │
│  🧠 SMART           - Self-configuring, auto-detecting                  │
│  🧩 COMPONENTS      - Reusable, composable UI building blocks           │
│  ⚡ DYNAMIC          - Runtime loading, hot-reload, lazy evaluation      │
│  🚫 NO DUPLICATION  - Single source of truth, DRY everywhere            │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Complete Folder Structure

```
Client/VRiseUI/
│
├── VRiseUI.sln
├── VRiseUI.csproj
│
├── 📁 Core/                          # Core framework (ZERO mod-specific code)
│   ├── 📁 Bootstrap/
│   │   ├── Plugin.cs                 # BepInEx entry point (minimal)
│   │   ├── ServiceContainer.cs       # Dependency injection container
│   │   └── Lifecycle.cs              # Init/Shutdown/Update hooks
│   │
│   ├── 📁 Config/
│   │   ├── IConfigProvider.cs        # Config abstraction
│   │   ├── JsonConfigProvider.cs     # JSON config implementation
│   │   └── GlobalConfig.cs           # Global settings model
│   │
│   ├── 📁 Events/
│   │   ├── EventBus.cs               # Pub/sub event system
│   │   ├── IEventHandler.cs          # Event handler interface
│   │   └── Events/                   # Built-in event types
│   │       ├── UIReadyEvent.cs
│   │       ├── AdapterLoadedEvent.cs
│   │       ├── CommandResponseEvent.cs
│   │       └── HotkeyPressedEvent.cs
│   │
│   └── 📁 Logging/
│       ├── ILogger.cs                # Logger interface
│       ├── BepInExLogger.cs          # BepInEx adapter
│       └── FileLogger.cs             # File output (debug)
│
├── 📁 Data/                          # Data layer (parsing, caching)
│   ├── 📁 Sources/
│   │   ├── IDataSource.cs            # Data source interface
│   │   ├── CommandDataSource.cs      # Chat command → parse response
│   │   ├── PollingDataSource.cs      # Periodic refresh wrapper
│   │   └── CachedDataSource.cs       # Caching decorator
│   │
│   ├── 📁 Parsing/
│   │   ├── IResponseParser.cs        # Parser interface
│   │   ├── RegexParser.cs            # Regex-based parsing
│   │   ├── JsonParser.cs             # JSON response parsing
│   │   └── PatternMapping.cs         # $1, $2 → field mapping
│   │
│   └── 📁 Cache/
│       ├── ICache.cs                 # Cache interface
│       ├── MemoryCache.cs            # In-memory cache
│       └── CachePolicy.cs            # TTL, invalidation rules
│
├── 📁 Commands/                      # Chat command system
│   ├── ICommandBridge.cs             # Command abstraction
│   ├── ChatCommandBridge.cs          # Send via game chat
│   ├── CommandQueue.cs               # Rate limiting, queue
│   └── ResponseMatcher.cs            # Match response to command
│
├── 📁 Assets/                        # Asset loading system
│   ├── IAssetLoader.cs               # Loader interface
│   ├── TextureLoader.cs              # PNG/JPG → Texture2D
│   ├── SpriteFactory.cs              # Texture2D → Sprite
│   ├── FontLoader.cs                 # Custom fonts
│   └── AssetCache.cs                 # Loaded asset cache
│
├── 📁 Adapters/                      # Mod adapter system
│   ├── 📁 Core/
│   │   ├── IAdapter.cs               # Adapter interface
│   │   ├── AdapterManager.cs         # Load/unload adapters
│   │   ├── AdapterDiscovery.cs       # Scan Adapters/ folder
│   │   └── AdapterContext.cs         # Per-adapter runtime context
│   │
│   ├── 📁 Manifest/
│   │   ├── ManifestSchema.cs         # JSON schema definition
│   │   ├── ManifestLoader.cs         # Load & validate JSON
│   │   ├── ManifestValidator.cs      # Schema validation
│   │   └── 📁 Models/                # Manifest C# models
│   │       ├── Manifest.cs
│   │       ├── HUDElementDef.cs
│   │       ├── MenuDef.cs
│   │       ├── DataSourceDef.cs
│   │       ├── StyleDef.cs
│   │       └── ActionDef.cs
│   │
│   └── 📁 Detection/
│       ├── IModDetector.cs           # Detection interface
│       ├── CommandDetector.cs        # Detect via command response
│       └── AssemblyDetector.cs       # Detect loaded DLLs
│
├── 📁 UI/                            # UI rendering system
│   ├── 📁 Core/
│   │   ├── UIManager.cs              # Central UI orchestrator
│   │   ├── UIFactory.cs              # Create elements from defs
│   │   ├── UIUpdater.cs              # Update loop for data binding
│   │   └── UITheme.cs                # Global theme/colors
│   │
│   ├── 📁 Layout/
│   │   ├── ILayoutEngine.cs          # Layout abstraction
│   │   ├── AbsoluteLayout.cs         # Fixed position
│   │   ├── AnchoredLayout.cs         # Relative to screen edges
│   │   ├── FlexLayout.cs             # Flexbox-style
│   │   └── GridLayout.cs             # CSS Grid-style
│   │
│   ├── 📁 Components/                # REUSABLE UI COMPONENTS
│   │   ├── 📁 Base/
│   │   │   ├── UIComponent.cs        # Base class for ALL components
│   │   │   ├── IBindable.cs          # Data binding interface
│   │   │   ├── IDraggable.cs         # Drag & drop interface
│   │   │   └── IHoverable.cs         # Hover events interface
│   │   │
│   │   ├── 📁 Display/               # Read-only display components
│   │   │   ├── ProgressBar.cs        # Horizontal/vertical bar
│   │   │   ├── CircularProgress.cs   # Circular gauge
│   │   │   ├── Label.cs              # Text display
│   │   │   ├── RichLabel.cs          # Formatted text
│   │   │   ├── Image.cs              # Static image
│   │   │   ├── Icon.cs               # Small icon with tooltip
│   │   │   └── Badge.cs              # Notification badge
│   │   │
│   │   ├── 📁 Interactive/           # Clickable/input components
│   │   │   ├── Button.cs             # Click action
│   │   │   ├── IconButton.cs         # Icon-only button
│   │   │   ├── Toggle.cs             # On/off switch
│   │   │   ├── Dropdown.cs           # Select from list
│   │   │   ├── Slider.cs             # Value slider
│   │   │   ├── TextField.cs          # Text input
│   │   │   └── Hotkey.cs             # Key binding
│   │   │
│   │   ├── 📁 Containers/            # Layout containers
│   │   │   ├── Panel.cs              # Basic container
│   │   │   ├── Window.cs             # Draggable window
│   │   │   ├── TabPanel.cs           # Tabbed content
│   │   │   ├── Accordion.cs          # Collapsible sections
│   │   │   ├── ScrollView.cs         # Scrollable area
│   │   │   ├── SplitPane.cs          # Resizable split
│   │   │   └── Modal.cs              # Popup dialog
│   │   │
│   │   ├── 📁 Lists/                 # Collection displays
│   │   │   ├── ListView.cs           # Vertical list
│   │   │   ├── GridView.cs           # Grid of items
│   │   │   ├── TreeView.cs           # Hierarchical
│   │   │   └── VirtualList.cs        # Large list (virtualized)
│   │   │
│   │   └── 📁 Specialized/           # Complex pre-built components
│   │       ├── TalentTree.cs         # Talent allocation
│   │       ├── InventoryGrid.cs      # Item slots
│   │       ├── Minimap.cs            # Map display
│   │       ├── Tooltip.cs            # Hover tooltip
│   │       └── Toast.cs              # Notification popup
│   │
│   ├── 📁 Styles/
│   │   ├── StyleSheet.cs             # CSS-like style system
│   │   ├── StyleParser.cs            # Parse style strings
│   │   ├── StyleMerger.cs            # Cascade/inherit styles
│   │   └── 📁 Themes/
│   │       ├── DarkTheme.cs
│   │       ├── LightTheme.cs
│   │       └── VampireTheme.cs       # Default V Rising theme
│   │
│   └── 📁 Animation/
│       ├── IAnimatable.cs            # Animation interface
│       ├── Tweener.cs                # Tween engine
│       ├── EasingFunctions.cs        # Ease in/out/etc
│       └── 📁 Presets/
│           ├── FadeIn.cs
│           ├── SlideIn.cs
│           └── Pulse.cs
│
├── 📁 Persistence/                   # Save/load user settings
│   ├── IUserSettings.cs              # Settings interface
│   ├── JsonUserSettings.cs           # JSON storage
│   ├── LayoutPersistence.cs          # Save element positions
│   └── ProfileManager.cs             # Multiple profiles
│
├── 📁 Input/                         # Input handling
│   ├── IInputHandler.cs              # Input abstraction
│   ├── HotkeyManager.cs              # Keyboard shortcuts
│   ├── DragDropManager.cs            # Drag & drop
│   └── FocusManager.cs               # UI focus tracking
│
├── 📁 Hooks/                         # Game integration
│   ├── ChatHook.cs                   # Intercept chat messages
│   ├── UIHook.cs                     # Hook into game UI
│   └── UpdateHook.cs                 # Game update loop
│
└── 📁 Utils/
    ├── Extensions/
    │   ├── StringExtensions.cs
    │   ├── RectExtensions.cs
    │   └── ColorExtensions.cs
    ├── Pool/
    │   └── ObjectPool.cs             # Reuse UI objects
    └── Math/
        └── UIRect.cs                 # Position/size helpers
```

---

## Dependency Injection System

### ServiceContainer

```csharp
namespace VRiseUI.Core.Bootstrap
{
    /// <summary>
    /// Lightweight DI container - NO external dependencies!
    /// </summary>
    public sealed class ServiceContainer
    {
        private static ServiceContainer _instance;
        public static ServiceContainer Instance => _instance ??= new ServiceContainer();
        
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly Dictionary<Type, Func<object>> _factories = new();
        
        // Register singleton
        public void RegisterSingleton<TInterface, TImplementation>() 
            where TImplementation : TInterface, new()
        {
            _singletons[typeof(TInterface)] = new TImplementation();
        }
        
        // Register singleton instance
        public void RegisterInstance<TInterface>(TInterface instance)
        {
            _singletons[typeof(TInterface)] = instance;
        }
        
        // Register factory (new instance each time)
        public void RegisterFactory<TInterface>(Func<TInterface> factory)
        {
            _factories[typeof(TInterface)] = () => factory();
        }
        
        // Resolve dependency
        public T Resolve<T>()
        {
            if (_singletons.TryGetValue(typeof(T), out var singleton))
                return (T)singleton;
            
            if (_factories.TryGetValue(typeof(T), out var factory))
                return (T)factory();
            
            throw new InvalidOperationException($"Service not registered: {typeof(T)}");
        }
        
        // Auto-wire services on startup
        public void AutoWire()
        {
            // Core services
            RegisterSingleton<ILogger, BepInExLogger>();
            RegisterSingleton<IConfigProvider, JsonConfigProvider>();
            RegisterSingleton<EventBus, EventBus>();
            
            // Data layer
            RegisterSingleton<ICache, MemoryCache>();
            RegisterSingleton<ICommandBridge, ChatCommandBridge>();
            
            // Assets
            RegisterSingleton<IAssetLoader, TextureLoader>();
            RegisterSingleton<AssetCache, AssetCache>();
            
            // Adapters
            RegisterSingleton<AdapterManager, AdapterManager>();
            RegisterSingleton<ManifestLoader, ManifestLoader>();
            
            // UI
            RegisterSingleton<UIManager, UIManager>();
            RegisterSingleton<UIFactory, UIFactory>();
            RegisterSingleton<UITheme, VampireTheme>();
            
            // Persistence
            RegisterSingleton<IUserSettings, JsonUserSettings>();
            RegisterSingleton<LayoutPersistence, LayoutPersistence>();
            
            // Input
            RegisterSingleton<HotkeyManager, HotkeyManager>();
            RegisterSingleton<DragDropManager, DragDropManager>();
        }
    }
}
```

---

## Component Base Class

### Zero Duplication Design

```csharp
namespace VRiseUI.UI.Components.Base
{
    /// <summary>
    /// BASE CLASS FOR ALL UI COMPONENTS
    /// Every component inherits this - ZERO duplication of common features
    /// </summary>
    public abstract class UIComponent : MonoBehaviour, IBindable, IDraggable, IHoverable
    {
        // ═══════════════════════════════════════════════════════════════
        //  IDENTITY
        // ═══════════════════════════════════════════════════════════════
        
        public string Id { get; set; }
        public string AdapterId { get; set; }  // Which adapter owns this
        
        // ═══════════════════════════════════════════════════════════════
        //  LIFECYCLE (Template Method Pattern)
        // ═══════════════════════════════════════════════════════════════
        
        protected virtual void Awake()
        {
            InitializeComponent();
            SetupBindings();
        }
        
        protected virtual void OnEnable() => SubscribeToEvents();
        protected virtual void OnDisable() => UnsubscribeFromEvents();
        protected virtual void OnDestroy() => Cleanup();
        
        // Subclasses override these
        protected abstract void InitializeComponent();
        protected virtual void SetupBindings() { }
        protected virtual void SubscribeToEvents() { }
        protected virtual void UnsubscribeFromEvents() { }
        protected virtual void Cleanup() { }
        
        // ═══════════════════════════════════════════════════════════════
        //  POSITIONING (Shared by ALL components)
        // ═══════════════════════════════════════════════════════════════
        
        [SerializeField] protected RectTransform _rectTransform;
        
        public Vector2 Position
        {
            get => _rectTransform.anchoredPosition;
            set => _rectTransform.anchoredPosition = value;
        }
        
        public Vector2 Size
        {
            get => _rectTransform.sizeDelta;
            set => _rectTransform.sizeDelta = value;
        }
        
        public AnchorPreset Anchor { get; set; } = AnchorPreset.TopLeft;
        
        public void SetAnchor(AnchorPreset preset)
        {
            Anchor = preset;
            AnchorHelper.Apply(_rectTransform, preset);
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  STYLING (Shared by ALL components)
        // ═══════════════════════════════════════════════════════════════
        
        protected StyleSheet _style = new StyleSheet();
        
        public void ApplyStyle(StyleDef styleDef)
        {
            _style.Merge(styleDef);
            OnStyleChanged();
        }
        
        protected virtual void OnStyleChanged() { }
        
        public Color BackgroundColor
        {
            get => _style.GetColor("backgroundColor", Color.clear);
            set => _style.SetColor("backgroundColor", value);
        }
        
        public Color ForegroundColor
        {
            get => _style.GetColor("foregroundColor", Color.white);
            set => _style.SetColor("foregroundColor", value);
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  DATA BINDING (IBindable)
        // ═══════════════════════════════════════════════════════════════
        
        protected IDataSource _dataSource;
        protected Dictionary<string, object> _boundData = new();
        
        public void BindTo(IDataSource source)
        {
            _dataSource?.Unbind();
            _dataSource = source;
            _dataSource.OnDataChanged += HandleDataChanged;
            _dataSource.Bind();
        }
        
        private void HandleDataChanged(Dictionary<string, object> data)
        {
            _boundData = data;
            OnDataUpdated(data);
        }
        
        protected abstract void OnDataUpdated(Dictionary<string, object> data);
        
        public T GetBoundValue<T>(string key, T defaultValue = default)
        {
            if (_boundData.TryGetValue(key, out var value))
                return (T)Convert.ChangeType(value, typeof(T));
            return defaultValue;
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  DRAG & DROP (IDraggable)
        // ═══════════════════════════════════════════════════════════════
        
        public bool IsDraggable { get; set; } = false;
        public bool IsDragging { get; private set; }
        
        private Vector2 _dragOffset;
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsDraggable) return;
            IsDragging = true;
            _dragOffset = Position - (Vector2)eventData.position;
            OnDragStart();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            Position = (Vector2)eventData.position + _dragOffset;
            OnDragMove();
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            IsDragging = false;
            OnDragEnd();
            
            // Auto-save position
            ServiceContainer.Instance.Resolve<LayoutPersistence>()
                .SavePosition(AdapterId, Id, Position);
        }
        
        protected virtual void OnDragStart() { }
        protected virtual void OnDragMove() { }
        protected virtual void OnDragEnd() { }
        
        // ═══════════════════════════════════════════════════════════════
        //  HOVER (IHoverable)
        // ═══════════════════════════════════════════════════════════════
        
        public bool IsHovered { get; private set; }
        public string TooltipText { get; set; }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHovered = true;
            OnHoverEnter();
            
            if (!string.IsNullOrEmpty(TooltipText))
                ShowTooltip(TooltipText);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            IsHovered = false;
            OnHoverExit();
            HideTooltip();
        }
        
        protected virtual void OnHoverEnter() { }
        protected virtual void OnHoverExit() { }
        
        // ═══════════════════════════════════════════════════════════════
        //  VISIBILITY (Shared by ALL)
        // ═══════════════════════════════════════════════════════════════
        
        public bool IsVisible
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }
        
        public void Show() => IsVisible = true;
        public void Hide() => IsVisible = false;
        public void Toggle() => IsVisible = !IsVisible;
        
        public float Alpha
        {
            get => _canvasGroup?.alpha ?? 1f;
            set { if (_canvasGroup) _canvasGroup.alpha = value; }
        }
        
        protected CanvasGroup _canvasGroup;
        
        // ═══════════════════════════════════════════════════════════════
        //  ANIMATION (Shared helpers)
        // ═══════════════════════════════════════════════════════════════
        
        protected Tweener _tweener;
        
        public void FadeIn(float duration = 0.3f)
        {
            Alpha = 0;
            Show();
            _tweener?.Kill();
            _tweener = DOTween.To(() => Alpha, x => Alpha = x, 1f, duration);
        }
        
        public void FadeOut(float duration = 0.3f, Action onComplete = null)
        {
            _tweener?.Kill();
            _tweener = DOTween.To(() => Alpha, x => Alpha = x, 0f, duration)
                .OnComplete(() => { Hide(); onComplete?.Invoke(); });
        }
    }
}
```

---

## Smart Component Examples

### ProgressBar (Display Component)

```csharp
namespace VRiseUI.UI.Components.Display
{
    /// <summary>
    /// Reusable progress bar - works for XP, HP, expertise, ANYTHING
    /// </summary>
    public class ProgressBar : UIComponent
    {
        // ═══════════════════════════════════════════════════════════════
        //  UI ELEMENTS (Created once, reused)
        // ═══════════════════════════════════════════════════════════════
        
        [SerializeField] private Image _background;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private TextMeshProUGUI _valueText;
        
        // ═══════════════════════════════════════════════════════════════
        //  CONFIGURATION (Set from manifest)
        // ═══════════════════════════════════════════════════════════════
        
        public ProgressBarOrientation Orientation { get; set; } = ProgressBarOrientation.Horizontal;
        public bool ShowLabel { get; set; } = true;
        public bool ShowValue { get; set; } = true;
        public bool ShowIcon { get; set; } = true;
        public string ValueFormat { get; set; } = "{current}/{max}";
        
        // ═══════════════════════════════════════════════════════════════
        //  LIFECYCLE
        // ═══════════════════════════════════════════════════════════════
        
        protected override void InitializeComponent()
        {
            // Find or create child elements
            _background = GetOrCreateChild<Image>("Background");
            _fill = GetOrCreateChild<Image>("Fill");
            _icon = GetOrCreateChild<Image>("Icon");
            _label = GetOrCreateChild<TextMeshProUGUI>("Label");
            _valueText = GetOrCreateChild<TextMeshProUGUI>("Value");
        }
        
        protected override void OnStyleChanged()
        {
            _background.color = BackgroundColor;
            _fill.color = ForegroundColor;
            
            if (_style.TryGetColor("borderColor", out var borderColor))
                ApplyBorder(borderColor, _style.GetFloat("borderWidth", 1));
            
            if (_style.TryGetFloat("cornerRadius", out var radius))
                ApplyCornerRadius(radius);
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  DATA BINDING (From base class)
        // ═══════════════════════════════════════════════════════════════
        
        protected override void OnDataUpdated(Dictionary<string, object> data)
        {
            var current = GetBoundValue<float>("current", 0);
            var max = GetBoundValue<float>("max", 100);
            var label = GetBoundValue<string>("label", "");
            
            SetProgress(current / max);
            SetLabel(label);
            SetValueText(current, max);
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═══════════════════════════════════════════════════════════════
        
        public void SetProgress(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            
            if (Orientation == ProgressBarOrientation.Horizontal)
                _fill.fillAmount = normalized;
            else
                _fill.fillAmount = normalized;
            
            // Optional: animate the change
            // _tweener?.Kill();
            // _tweener = DOTween.To(() => _fill.fillAmount, x => _fill.fillAmount = x, normalized, 0.2f);
        }
        
        public void SetLabel(string text)
        {
            if (_label && ShowLabel)
                _label.text = text;
        }
        
        public void SetValueText(float current, float max)
        {
            if (_valueText && ShowValue)
            {
                _valueText.text = ValueFormat
                    .Replace("{current}", current.ToString("F0"))
                    .Replace("{max}", max.ToString("F0"))
                    .Replace("{percent}", (current / max * 100).ToString("F0"));
            }
        }
        
        public void SetIcon(Sprite sprite)
        {
            if (_icon && ShowIcon)
            {
                _icon.sprite = sprite;
                _icon.gameObject.SetActive(sprite != null);
            }
        }
    }
}
```

---

## Dynamic Component Factory

### UIFactory (Creates components from manifest definitions)

```csharp
namespace VRiseUI.UI.Core
{
    /// <summary>
    /// Factory that creates UI components from manifest definitions
    /// NO switch statements - uses registration pattern
    /// </summary>
    public class UIFactory
    {
        // ═══════════════════════════════════════════════════════════════
        //  COMPONENT REGISTRY (Add once, use everywhere)
        // ═══════════════════════════════════════════════════════════════
        
        private readonly Dictionary<string, ComponentRegistration> _registry = new();
        
        public UIFactory()
        {
            // Register all component types ONCE
            Register<ProgressBar>("progressBar");
            Register<CircularProgress>("circularProgress");
            Register<Label>("label");
            Register<RichLabel>("richLabel");
            Register<Button>("button");
            Register<IconButton>("iconButton");
            Register<Toggle>("toggle");
            Register<Image>("image");
            Register<Icon>("icon");
            Register<Panel>("panel");
            Register<Window>("window");
            Register<TabPanel>("tabPanel");
            Register<ListView>("list");
            Register<GridView>("grid");
            Register<ScrollView>("scroll");
            Register<TalentTree>("talentTree");
            Register<Tooltip>("tooltip");
            Register<Toast>("toast");
        }
        
        private void Register<T>(string type) where T : UIComponent
        {
            _registry[type] = new ComponentRegistration
            {
                Type = typeof(T),
                Create = (parent) => CreateComponent<T>(parent)
            };
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  CREATE FROM MANIFEST
        // ═══════════════════════════════════════════════════════════════
        
        public UIComponent CreateFromDefinition(HUDElementDef def, Transform parent, AdapterContext context)
        {
            if (!_registry.TryGetValue(def.Type, out var registration))
            {
                Log.Warning($"Unknown component type: {def.Type}");
                return null;
            }
            
            // Create the component
            var component = registration.Create(parent);
            
            // Apply common properties
            component.Id = def.Id;
            component.AdapterId = context.AdapterId;
            component.Position = new Vector2(def.Position.X, def.Position.Y);
            component.Size = new Vector2(def.Size.Width, def.Size.Height);
            component.SetAnchor(ParseAnchor(def.Position.Anchor));
            
            // Apply style
            if (def.Style != null)
                component.ApplyStyle(def.Style);
            
            // Apply icon
            if (!string.IsNullOrEmpty(def.Icon))
            {
                var sprite = context.LoadAsset<Sprite>(def.Icon);
                if (component is ISupportsIcon iconComponent)
                    iconComponent.SetIcon(sprite);
            }
            
            // Bind data source
            if (def.DataSource != null)
            {
                var dataSource = CreateDataSource(def.DataSource, context);
                component.BindTo(dataSource);
            }
            
            // Restore saved position (user may have moved it)
            var persistence = ServiceContainer.Instance.Resolve<LayoutPersistence>();
            if (persistence.TryGetPosition(context.AdapterId, def.Id, out var savedPos))
                component.Position = savedPos;
            
            // Make draggable if in edit mode
            component.IsDraggable = GlobalConfig.Instance.EditMode;
            
            return component;
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  DATA SOURCE FACTORY
        // ═══════════════════════════════════════════════════════════════
        
        private IDataSource CreateDataSource(DataSourceDef def, AdapterContext context)
        {
            // Create parser
            IResponseParser parser = def.ResponseType switch
            {
                "json" => new JsonParser(),
                _ => new RegexParser(def.Pattern, def.Mapping)
            };
            
            // Create command source
            var commandSource = new CommandDataSource(
                ServiceContainer.Instance.Resolve<ICommandBridge>(),
                def.Command,
                parser
            );
            
            // Wrap with caching
            var cached = new CachedDataSource(
                commandSource,
                ServiceContainer.Instance.Resolve<ICache>(),
                TimeSpan.FromMilliseconds(def.CacheTime ?? 1000)
            );
            
            // Wrap with polling if refresh interval specified
            if (def.RefreshInterval > 0)
            {
                return new PollingDataSource(cached, def.RefreshInterval);
            }
            
            return cached;
        }
        
        // ═══════════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════════
        
        private T CreateComponent<T>(Transform parent) where T : UIComponent
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(parent, false);
            
            // Add required Unity components
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasGroup>();
            
            // Add our component
            return go.AddComponent<T>();
        }
        
        private class ComponentRegistration
        {
            public Type Type { get; set; }
            public Func<Transform, UIComponent> Create { get; set; }
        }
    }
}
```

---

## Event System (Decoupled Communication)

```csharp
namespace VRiseUI.Core.Events
{
    /// <summary>
    /// Pub/sub event bus - components communicate without knowing each other
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        
        // Subscribe to event type
        public void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            var type = typeof(T);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new List<Delegate>();
            
            _handlers[type].Add(handler);
        }
        
        // Unsubscribe
        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }
        
        // Publish event
        public void Publish<T>(T evt) where T : IEvent
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                foreach (var handler in list.ToList()) // ToList to allow modification
                {
                    try
                    {
                        ((Action<T>)handler)(evt);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Event handler error: {ex}");
                    }
                }
            }
        }
    }
    
    // Event types
    public interface IEvent { }
    
    public class AdapterLoadedEvent : IEvent
    {
        public string AdapterId { get; set; }
        public Manifest Manifest { get; set; }
    }
    
    public class CommandResponseEvent : IEvent
    {
        public string Command { get; set; }
        public string Response { get; set; }
    }
    
    public class HotkeyPressedEvent : IEvent
    {
        public KeyCode Key { get; set; }
        public bool Ctrl { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
    }
    
    public class UIRefreshEvent : IEvent
    {
        public string AdapterId { get; set; }
    }
}
```

---

## Plugin Entry Point (Minimal)

```csharp
namespace VRiseUI.Core.Bootstrap
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BasePlugin
    {
        public const string GUID = "com.vriseui.core";
        public const string NAME = "VRiseUI";
        public const string VERSION = "1.0.0";
        
        public override void Load()
        {
            Log.Info($"{NAME} v{VERSION} loading...");
            
            // 1. Wire up dependency injection
            ServiceContainer.Instance.AutoWire();
            
            // 2. Load global config
            ServiceContainer.Instance.Resolve<IConfigProvider>().Load();
            
            // 3. Initialize UI system
            ServiceContainer.Instance.Resolve<UIManager>().Initialize();
            
            // 4. Discover and load adapters
            ServiceContainer.Instance.Resolve<AdapterManager>().DiscoverAndLoad();
            
            // 5. Hook into game
            Harmony.CreateAndPatchAll(typeof(ChatHook));
            Harmony.CreateAndPatchAll(typeof(UIHook));
            
            Log.Info($"{NAME} loaded successfully!");
        }
        
        public override bool Unload()
        {
            ServiceContainer.Instance.Resolve<UIManager>().Shutdown();
            return true;
        }
    }
}
```

---

## Summary: Zero Duplication Achieved

| Concern | Where It Lives | Duplication? |
|---------|----------------|--------------|
| Position/Size | `UIComponent` base class | ❌ None |
| Styling | `UIComponent` + `StyleSheet` | ❌ None |
| Data binding | `UIComponent.BindTo()` | ❌ None |
| Drag & drop | `UIComponent` (IDraggable) | ❌ None |
| Hover/tooltip | `UIComponent` (IHoverable) | ❌ None |
| Animation | `UIComponent` + `Tweener` | ❌ None |
| Visibility | `UIComponent.Show/Hide()` | ❌ None |
| Component creation | `UIFactory` registry | ❌ None |
| Data sources | Decorator pattern chain | ❌ None |
| Events | `EventBus` pub/sub | ❌ None |
| Config | `ServiceContainer` DI | ❌ None |

---

**This architecture ensures:**
- ✅ **Modular** - Every feature is a separate service/component
- ✅ **Smart** - Auto-discovery, auto-wiring, auto-binding
- ✅ **Dynamic** - Runtime loading, hot-reload, lazy creation
- ✅ **No Duplication** - Base classes, DI, factory pattern
- ✅ **Testable** - Interfaces everywhere, easy to mock
- ✅ **Extensible** - Add new components without modifying core

