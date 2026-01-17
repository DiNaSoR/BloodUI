# 📋 Changelog

> All notable changes to **BloodCraftPlus** are documented here.  
> This project is a V Rising modding monorepo containing both **EclipsePlus** (client) and **Bloodcraftplus** (server).

---

## 🎮 Current Versions

| Mod | Version | Type |
|-----|---------|------|
| **EclipsePlus** | `1.4.0` | 🖥️ Client |
| **Bloodcraftplus** | `1.12.21` | 🗄️ Server |

---

## 📅 January 2026 Updates

### 🗓️ January 17, 2026 - v1.4.0 / v1.12.21 — Gear Level UI & Rift/Familiar Fixes

#### 🖥️ EclipsePlus (Client)

**Changed**
- **Protocol**: bumped Eclipse sync to `1.4.x` (update client + server together).

**Fixed**
- **Familiar stats display**: HP/PP/SP no longer mis-parses when values exceed old fixed-width limits.

#### 🗄️ Bloodcraftplus (Server)

**Changed**
- **Equipment Tab Gear Level**: now shows **Exp level only** (no longer adds weapon expertise and no hidden +1).

**Fixed**
- **Primal rifts**: `RiftFrequency <= 0` now disables rift scheduling (prevents 0-interval spam).
- **Familiar stats sync**: Familiar HP/PP/SP now sent as `hp|pp|sp` to avoid overflow/misalignment.
- **Documentation**: clarified that some config options (e.g., `ElitePrimalRifts`) are cached at startup and require a server restart.

---

### 🗓️ January 15, 2026 - v1.3.15 / v1.12.20

## ✨ EclipsePlus (Client) Changes

### 🆕 Added

#### 🎮 Character Menu System
- **Class Tab** - View and select player classes with stat previews
- **Exoform Tab** - Manage exoform abilities and transformations
- **Familiars Tab** - Complete familiar management with sub-panels:
  - **Battles Panel** - Familiar combat and battle management
  - **Talents Panel** - Visual talent tree with Speed/Power/Vitality paths
  - **Overflow Panel** - Manage excess familiars
  - **Settings Panel** - Per-familiar configuration options
- **Prestige Tab** - Track and purchase prestige upgrades
- **Professions Tab** - View profession levels and bonuses
- **Progression Tab** - Combined class and profession overview
- **Stat Bonuses Tab** - Detailed stat breakdown and bonuses

#### 📊 HUD Components
- **Experience Bar** - Real-time XP progress display
- **Expertise Bar** - Weapon mastery progress tracking
- **Familiar Bar** - Familiar health and level display
- **Legacy Bar** - Blood legacy progress indicator
- **Quest Tracker** - Active quest objectives overlay

#### 🛠️ VDebug Toolkit
- **New VDebug plugin** with asset dumping and UI inspection
- **UI Inspector Service** with layout, sibling, and style info
- **Export functionality** for capturing UI component details
- **Font Loading Service** supporting asset bundles and in-game resources

#### 📐 Layout Service
- **LayoutService** for managing UI element positions with save/load
- **In-game layout editing mode** for real-time UI adjustments
- **Canvas-relative bounds calculations** for hit-testing
- **Enhanced drag/hover functionality** with zero-size rect handling


### 🔧 Improved

#### 📊 Progression System
- **Renamed Professions tab to Progression** for consistency
- **Progression tab functionality** with new styles and JavaScript for class and profession management
- Fixed command index mapping to align with server semantics

#### 🎨 UI Enhancements
- **Modular CSS structure** split into separate files for maintainability
- **Updated icons** for better representation
- **New banner image** added to documentation

#### 📝 Documentation
- **Enhanced README** with features and installation instructions
- **Updated documentation** reflecting file structure changes

### 🗑️ Removed

- Removed unnecessary Class UI and Tabs UI functionality
- Removed deprecated services from the codebase
- Streamlined project by removing unused UI elements

### 🐛 Fixed

- Fixed compilation issues with Professions → Progression references
- Fixed missing line breaks and build configurations
- Improved logging framework across multiple services and patches

---

## 🗄️ Bloodcraftplus (Server) Changes

### 🆕 Added

#### 🌳 Familiar Talent System
- **Full Talent Tree UI** with three paths: Speed, Power, and Vitality
- **Talent allocation** with prerequisites and keystone abilities
- **Stat bonuses** including Physical Power, Spell Power, Attack Speed, Movement Speed, Max Health, and Damage Reduction
- **Visual buff effects** for allocated keystones
- **Talent reset functionality** for respeccing familiar builds
- **Server-side persistence** with per-familiar talent data storage

#### ⚔️ Gear Level Mirage System
- **Dynamic gear level display** calculated as Player Level + Weapon Expertise
- **Armor level hidden** (set to 0) to emphasize weapon mastery
- **Real-time updates** when switching weapons or gaining expertise
- Creates an immersive "power fantasy" where weapon mastery affects perceived strength

### 🔧 Improved

#### 🏗️ Build System
- **Standardized naming convention** for Bloodcraftplus across all files
- **GitHub Actions workflows** standardized and updated
- **BuildToServer target** with SkipServerCopy variable for build flexibility
- **Post-build targets** refactored for builds without local dependencies

#### 📝 Documentation
- **README updates** with maintainer information
- **Client/server mod details** clarified
- **Quick start commands** added
- **Installation instructions** enhanced

### 🗑️ Removed

- Removed deprecated services including CanvasService

### 🐛 Fixed

#### 🐾 Familiar Catch-Up Speed
- **Dynamic speed boost** when familiar is far from player (15+ units away)
- **2x speed multiplier** to catch up quickly
- **Hysteresis system** prevents speed flickering (returns to normal at 8 units)
- Familiars now follow the player smoothly without getting left behind

- Fixed GitHub Actions to use bracket notation for secrets
- Fixed build output paths for release artifacts
- Fixed README generation conditions

---

## 🔄 Shared / Infrastructure Changes

### 🏗️ Build & CI/CD

| Change | Description |
|--------|-------------|
| 🔧 GitHub Actions | Updated to use `dotnet build` instead of `dotnet build -t:Compile` |
| 📦 Thunderstore | Added build configuration with proper output directories |
| 🔑 Secrets | Fixed bracket notation for accessing secrets |
| 📋 Versioning | Synchronized thunderstore.toml versions across projects |

### 📚 Documentation

- Updated README with proper naming conventions
- Added maintainer credits and contact information
- Enhanced installation and quick start guides
- Added banner images to documentation

### 🧹 Code Cleanup

- Refactored logging to use DebugToolsBridge consistently
- Removed unnecessary file compilations from Eclipse project
- Streamlined codebase by removing deprecated components

---

## 📊 Version History

| Date | Eclipse | Bloodcraft | Notes |
|------|---------|------------|-------|
| Jan 17, 2026 | 1.4.0 | 1.12.21 | Current release - Gear Level UI & Rift fixes |
| Jan 15, 2026 | 1.3.15 | 1.12.20 | Character Menu & Talents |
| Jan 14, 2026 | 1.3.14 | 1.12.19 | VDebug & Layout improvements |
| Jan 13, 2026 | 1.3.13 | 1.12.18 | Cleanup |

---

## 📖 Quick Reference

<details>
<summary>🖥️ EclipsePlus Features</summary>

### Client-Side Features
- **VDebug Toolkit** - Asset dumping and UI inspection
- **Layout Service** - Draggable UI elements with persistence
- **Progression Tab** - Class and profession management
- **Familiar Talent Tree** - Visual talent allocation UI
- **Debug Panel** - Runtime UI debugging (F9 to toggle)

</details>

<details>
<summary>🗄️ Bloodcraftplus Features</summary>

### Server-Side Features
- **Leveling System** - Player progression and XP
- **Expertise System** - Weapon mastery
- **Legacy System** - Blood type bonuses
- **Familiar System** - Pet companions with **Talent Trees**
- **Gear Level Mirage** - Display level = Player Level + Weapon Expertise
- **Profession System** - Crafting and gathering bonuses
- **Quest System** - Daily and weekly objectives

</details>

---

## 📝 Notes

> [!NOTE]
> This changelog covers commits from **December 22, 2025** to **January 17, 2026**.

> [!TIP]
> For detailed installation instructions, see the main [README.md](README.md).

> [!IMPORTANT]
> Make sure both client (EclipsePlus) and server (Bloodcraftplus) mods match compatible versions.

---

<div align="center">

**Made with ❤️ for the V Rising Community**

[🐛 Report Issues](https://github.com/DiNaSoR/BloodCraftPlus/issues) • [📖 Documentation](https://github.com/DiNaSoR/BloodCraftPlus)

</div>
