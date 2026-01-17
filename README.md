<p align="center">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/banner.png" alt="BloodCraft PLUS" width="900">
</p>
<div align="center">

# 🧛 BloodCraftPlus

### *The Ultimate V Rising RPG Experience*

[![V Rising](https://img.shields.io/badge/V%20Rising-1.1-darkred?style=for-the-badge&logo=steam&logoColor=white)](https://store.steampowered.com/app/1604030/V_Rising/)
[![BepInEx](https://img.shields.io/badge/BepInEx-6.x-blueviolet?style=for-the-badge)](https://github.com/BepInEx/BepInEx)
[![License](https://img.shields.io/badge/License-CC%20BY--NC%204.0-lightgrey?style=for-the-badge)](LICENSE.md)

*Transform your V Rising server into a full-fledged RPG with experience levels, weapon expertise, blood legacies, classes, familiars, and much more!*

</div>

---

## 📦 Repository Structure

This is a **monorepo** containing three projects:

| Project | Type | Location | Description |
|---------|------|----------|-------------|
| **EclipsePlus** | 🖥️ Client | `Client/EclipsePlus/` | Beautiful UI overlay for players |
| **Bloodcraftplus** | 🖧 Server | `Server/Bloodcraft/` | Core RPG systems and game logic |
| **VDebug** | 🛠️ Tool | `Tools/VDebug/` | Optional debug & asset inspection |

> ⚡ **EclipsePlus + Bloodcraftplus are required** - The server mod provides the features, and the client mod displays them!

---

## ✨ Features

<table>
<tr>
<td width="50%">

### ⚔️ Combat & Progression
- **Experience Leveling** - Level up by slaying enemies
- **Weapon Expertise** - Master each weapon type
- **Blood Legacies** - Gain power from feeding
- **Prestige System** - Reset and grow stronger
- **Gear Level Mirage** - Display = Level + Expertise

</td>
<td width="50%">

### 🎭 Classes & Abilities
- **6 Unique Classes** - Each with special abilities
- **Stat Synergies** - Class-specific bonuses
- **On-Hit Effects** - Proc debuffs on enemies
- **Shift Spells** - Unlock powerful abilities

</td>
</tr>
<tr>
<td width="50%">

### 🐺 Familiars
- **Unlock 100+ Familiars** - Capture defeated foes
- **Talent Trees** - Speed, Power, Vitality paths
- **Familiar Battles** - PvP battles with familiars
- **Shiny Variants** - Rare cosmetic effects

</td>
<td width="50%">

### 📜 Quests & Professions
- **Daily & Weekly Quests** - Earn rewards
- **8 Professions** - Mining, Fishing, Alchemy...
- **Bonus Resources** - Scale with profession level
- **Crafting Bonuses** - Enhanced gear stats

</td>
</tr>
</table>

---

## 🎮 Classes

| Class | Theme | On-Hit Effect | 
|:---:|:---|:---|
| 🔴 **Blood Knight** | Tank / Life Steal | Leech → Blood Curse |
| 🟡 **Demon Hunter** | Agile / Crits | Static → Lightning |
| 🔵 **Vampire Lord** | Balanced / Frost | Chill → Freeze |
| 🟣 **Shadow Blade** | Assassin / Illusion | Weaken → Phantasm |
| 🟢 **Arcane Sorcerer** | Burst / Chaos | Ignite → Burn |
| 🟩 **Death Mage** | Necro / Unholy | Condemn → Curse |

---

## 🖼️ UI Preview (EclipsePlus)

<p align="center">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Progression_tab_01.png" alt="Professions Tab" width="400">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Progression_tab_02.png" alt="Class Tab" width="400">
</p>
<p align="center">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Familiars_tab_01.png" alt="Familiars Tab" width="400">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Familiars_tab_02.png" alt="Familiars Management" width="400">
</p>
<p align="center">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Stat_bonuses_tab_01.png" alt="Weapon Expertise" width="400">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Stat_bonuses_tab_02.png" alt="Blood Legacies" width="400">
</p>
<p align="center">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Exoform_tab.png" alt="Exoform Tab" width="400">
  <img src="https://dinasor.github.io/BloodCraftPlus/images/Prestige_tab.png" alt="Prestige Tab" width="400">
</p>

---

## 📥 Installation

### Client Setup (EclipsePlus)
```
📁 V Rising/BepInEx/plugins/
   └── EclipsePlus.dll
```

### Server Setup (Bloodcraftplus)
```
📁 VRisingServer/BepInEx/plugins/
   └── Bloodcraftplus.dll
```

### Optional: VDebug (Development)
```
📁 V Rising/BepInEx/plugins/
   └── VDebug.dll
```

---

## 🛠️ Quick Start

After installation, use these chat commands to get started:

```
.lvl get          → Check your level
.class l          → List available classes
.class s BloodKnight  → Select a class
.fam l            → List your familiars
.quest d          → View daily quest
```

---

## 🛠️ VDebug (Developer Tool)

Optional debug plugin for modders and developers:

- **Asset Dumping** - Export game assets and sprites
- **UI Inspector** - Explore Unity UI hierarchy
- **Layout Editor** - Adjust UI positions in real-time
- **Debug Logging** - Centralized log routing

See [Tools/VDebug/README.md](Tools/VDebug/README.md) for details.

---

## 💫 Credits

<div align="center">

| Role | Credit |
|------|--------|
| **Original Creator** | [mfoltz](https://github.com/mfoltz) |
| **Fork Maintainer** | [DiNaSoR](https://github.com/DiNaSoR) |

</div>

---

## 🔗 Downloads

<div align="center">

[![Thunderstore EclipsePlus](https://img.shields.io/badge/Thunderstore-EclipsePlus%20(Client)-blue?style=for-the-badge)](https://new.thunderstore.io/c/v-rising/p/DiNaSoR/EclipsePlus/)
[![Thunderstore BloodCraftPlus](https://img.shields.io/badge/Thunderstore-Bloodcraftplus%20(Server)-green?style=for-the-badge)](https://new.thunderstore.io/c/v-rising/p/DiNaSoR/Bloodcraftplus/)

[![GitHub Releases](https://img.shields.io/badge/GitHub-Releases-black?style=for-the-badge&logo=github)](https://github.com/DiNaSoR/BloodCraftPlus/releases)

</div>

---

## 📄 License

This project is licensed under the **Creative Commons Attribution-NonCommercial 4.0 International License (CC BY-NC 4.0)** - see the [LICENSE.md](LICENSE.md) file for details.

---

<div align="center">

**Made with 🩸 for the V Rising modding community**

*If you enjoy this mod, consider starring the repository!* ⭐

</div>