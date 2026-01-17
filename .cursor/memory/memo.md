# Project Memo

Last updated: 2026-01-17 (Docs site added)

## Project Structure (current truth)

This is a **monorepo** with three main projects:

```
BloodCraftPlus/
├── Client/EclipsePlus/    # Client mod (UI overlay)
│   ├── EclipsePlus.csproj
│   ├── Services/CharacterMenu/
│   ├── Services/HUD/
│   └── Patches/
├── Server/Bloodcraftplus/     # Server mod (RPG systems)
│   ├── Bloodcraftplus.csproj
│   ├── Systems/
│   ├── Interfaces/
│   └── Commands/
├── Tools/VDebug/          # Optional debug plugin
│   └── VDebug.csproj
└── Docs/                  # Documentation
```

---

## V Rising Mod – Familiars (current truth)

- Ownership:
  - UI: `Client/EclipsePlus/Services/CharacterMenu/Tabs/FamiliarsTab.cs`
  - Sub-panels: Battles, Talents, Overflow, Settings
  - Box/chat updates: `Client/EclipsePlus/Services/DataService.cs` + `Client/EclipsePlus/Patches/ClientChatSystemPatch.cs`
  - Sprite allowlist: `Client/EclipsePlus/Services/HUD/Shared/HudData.cs`

- Familiar Talents:
  - Client UI: `Client/EclipsePlus/Services/CharacterMenu/Tabs/FamiliarsTab.Talents.cs`
  - Server logic: `Server/Bloodcraftplus/Systems/Familiars/FamiliarTalentSystem.cs`
  - Three paths: Speed, Power, Vitality with keystones

- Familiar catch-up speed:
  - Service: `Server/Bloodcraftplus/Services/FamiliarService.cs`
  - 2x speed boost at 15+ units, returns to normal at 8 units

---

## V Rising Mod – Gear Level Mirage (current truth)

- Display Level = Player Level + Weapon Expertise
- Armor level hidden (set to 0)
- Implementation: `Server/Bloodcraftplus/Systems/Leveling/LevelingSystem.cs` → `SetLevel()`

---

## Eclipse modular architecture (current truth)

- Ownership:
  - HUD subsystem: `Client/EclipsePlus/Services/HUD/*`
  - Character menu subsystem: `Client/EclipsePlus/Services/CharacterMenu/*`
  - Shared UI factory: `Client/EclipsePlus/Services/CharacterMenu/Shared/UIFactory.cs`

- Character Menu Tabs:
  - Class, Exoform, Familiars, Prestige, Professions, Progression, StatBonuses

- HUD Components:
  - Experience Bar, Expertise Bar, Familiar Bar, Legacy Bar, Quest Tracker

- Optional debug tooling lives in a separate plugin:
  - Debug plugin: `Tools/VDebug` (GUID: `com.dinasor.vdebug`)
  - EclipsePlus calls it via reflection: `Client/EclipsePlus/Services/DebugToolsBridge.cs`
  - All EclipsePlus logs route to VDebug (silent without it)

---

## Build & CI/CD (current truth)

- GitHub Actions:
  - `.github/workflows/build.yml` — Build + release mods
  - `.github/workflows/docs.yml` — Build + deploy docs to GitHub Pages
- Thunderstore configs:
  - `Client/EclipsePlus/thunderstore.toml`
  - `Server/Bloodcraftplus/thunderstore.toml`

---

## Documentation Site (current truth)

- Location: `Docs/`
- Tech: Vite + React + TypeScript + MDX
- Theme: Dracula-vibes (dark, vampiric red accents, gothic typography)
- Deployment: GitHub Pages via Actions (auto-deploy on push to main)
- URL: `https://dinasor.github.io/BloodCraftPlus/#/`
- Router: HashRouter (URLs use `#/path` to survive refresh on GitHub Pages)
- Navigation config: `Docs/src/docs.config.ts`
- Content: `Docs/src/content/**/*.mdx`

### Key Components
- `Docs/src/components/Layout.tsx` — Sidebar + header (accordion nav)
- `Docs/src/components/Callout.tsx` — Info/warning/tip boxes
- `Docs/src/components/CodeBlock.tsx` — Syntax highlighting
- `Docs/src/components/CommandCard.tsx` — Command reference cards
- `Docs/src/components/ConfigTable.tsx` — Searchable config table
- `Docs/src/components/ChangelogSection.tsx` — Expandable changelog entries
- `Docs/src/components/PortraitShotPlaceholder.tsx` — 9:16 screenshot placeholders

### Documentation Workflow (MANDATORY)
1. **After ANY feature/fix**: Update the relevant content page in `Docs/src/content/`.
2. **After ANY release-worthy change**: Add `<ChangelogSection>` entry to `Docs/src/content/reference/changelog.mdx`.
3. **New commands**: Update `Docs/src/content/reference/commands.mdx`.
4. **New config options**: Update `Docs/src/content/reference/config.mdx`.
5. **Client UI changes**: Update `Docs/src/content/client/*.mdx` with screenshots.
6. **Server system changes**: Update `Docs/src/content/server/*.mdx`.

### Content Structure
```
Docs/src/content/
├── index.mdx                  # Home page
├── getting-started/
│   ├── index.mdx
│   ├── installation.mdx
│   └── troubleshooting.mdx
├── server/
│   ├── index.mdx
│   ├── leveling.mdx
│   ├── expertise.mdx
│   ├── legacies.mdx
│   ├── classes.mdx
│   ├── familiars.mdx
│   ├── quests.mdx
│   ├── professions.mdx
│   └── prestige.mdx
├── client/
│   ├── index.mdx
│   ├── hud.mdx
│   ├── character-menu.mdx
│   └── data-flow.mdx
├── reference/
│   ├── commands.mdx
│   ├── config.mdx
│   └── changelog.mdx          # ← Add entries here with <ChangelogSection>
├── tools/
│   ├── vdebug.mdx
│   └── design-mock.mdx
└── contributing/
    └── index.mdx
```

---

## Il2Cpp constraints to remember

- Avoid ambiguous `Object.Destroy()` → prefer `UnityEngine.Object.Destroy()`. 
- Avoid `new RectOffset(left,right,top,bottom)`; set properties explicitly.
