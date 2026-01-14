# Project Memo

Last updated: 2026-01-14

## V Rising Mod – Familiars (current truth)

- Ownership:
  - UI: `Services/CharacterMenu/Tabs/FamiliarsTab.cs` (created/updated via `Services/CharacterMenuService.cs`)
  - Box/chat updates: `Services/DataService.cs` + `Patches/ClientChatSystemPatch.cs`
  - Sprite allowlist: `Services/HUD/Shared/HudData.cs`

- Familiars UI is owned by `Services/CharacterMenu/Tabs/FamiliarsTab.cs` (single owner; avoid parallel UI implementations).

- Familiars box live updates are driven by chat parsing:
  - Parsing: `Services/DataService.cs`
  - Hook/wiring: `Patches/ClientChatSystemPatch.cs`

- The Current Box list is intentionally fixed to 10 slots:
  - Always render 10 rows
  - Empty rows are disabled placeholders (consistent layout)

- Icon/sprite stability rules:
  - Use manifest-backed sprite names where possible
  - If a sprite fails to resolve, hide the icon to avoid white placeholder boxes
  - HUD sprite allowlist must include required sprites for headers/dividers/icons:
    - `Services/HUD/Shared/HudData.cs`

- Binding must be slot-based (not name-based) to avoid ambiguity:
  - Prefer `.fam b #` with delayed unbind→bind routine when needed

---

## V Rising Mod – Stat Bonuses (current truth)

- Ownership:
  - UI: `Services/CharacterMenu/Tabs/StatBonusesTab.cs` (created/updated via `Services/CharacterMenuService.cs`)
  - Data payload + parsing:
    - Weapon Expertise stats: `Docs/Bloodcraft/Interfaces/EclipseInterface.cs` → `Patches/ClientChatSystemPatch.cs` → `Services/DataService.cs` (`ParseWeaponStatBonusData`)
    - Blood Legacies stats: `Docs/Bloodcraft/Interfaces/EclipseInterface.cs` (ProgressToClient / LegacyData) → `Patches/ClientChatSystemPatch.cs` → `Services/DataService.cs` (`ParsePlayerData`) → `Services/CanvasService.cs` (`DataHUD._legacy*`)
  - Sprite allowlist: `Services/HUD/Shared/HudData.cs`

- Stat Bonuses UI is owned by `Services/CharacterMenu/Tabs/StatBonusesTab.cs` (single owner; avoid parallel UI implementations).
- Stat Bonuses panel supports two modes:
  - Weapon Expertise (interactive via `.wep cst …`)
  - Blood Legacies (interactive via `.bl cst …`)
    - Requires Bloodcraft server behavior that supports toggling selections off (clicking an already-selected stat removes it). If the server is on older behavior (add-only), unselect will not work without `.bl rst`.

---

## V Rising Mod – Exoform (current truth)

- Ownership:
  - UI: `Services/CharacterMenu/Tabs/ExoformTab.cs` (panel-based; created/updated via `Services/CharacterMenuService.cs`)
  - Data payload + parsing: `Docs/Bloodcraft/Interfaces/EclipseInterface.cs` (`ExoFormDataToClient`) → `Patches/ClientChatSystemPatch.cs` → `Services/DataService.cs` (`ParseExoFormData`)
  - Sprite allowlist: `Services/HUD/Shared/HudData.cs`

- Notes:
  - Exoform UI no longer uses the legacy text-entry list (`BuildEntries()`).
  - UI is click-only:
    - Toggle Taunt-to-Exoform: `.prestige exoform`
    - Select active form: `.prestige sf <FormName>`
  - Ability names are resolved via prefab localization with safe fallback.

---

## V Rising Mod – Prestige (current truth)

- Ownership:
  - UI: `Services/CharacterMenu/Tabs/PrestigeTab.cs` (panel-based; created/updated via `Services/CharacterMenuService.cs`)
  - Data payload + parsing: `Docs/Bloodcraft/Interfaces/EclipseInterface.cs` (`PrestigeLeaderboardToClient`) → `Patches/ClientChatSystemPatch.cs` → `Services/DataService.cs` (`ParsePrestigeLeaderboardData`)

- Notes:
  - Prestige tab is now click-only and panel-based (no legacy text-entry rendering).
  - Type selection uses a dropdown list populated from `_prestigeLeaderboardOrder`.

---

## V Rising Mod – Professions (current truth)

- Ownership:
  - UI: `Services/CharacterMenu/Tabs/ProfessionsTab.cs` (panel-based; created/updated via `Services/CharacterMenuService.cs`)
  - Data source: `Services/DataService.cs` (`ParsePlayerData` → `_enchanting*`, `_alchemy*`, etc.)

- Notes:
  - When `Plugin.Professions` is disabled, the tab shows “Profession UI disabled.”

---

## Eclipse modular architecture (current truth)

- Ownership:
  - HUD subsystem: `Services/HUD/*`
  - Character menu subsystem: `Services/CharacterMenu/*`
  - Shared UI factory: `Services/CharacterMenu/Shared/UIFactory.cs`

- HUD subsystem lives under `Services/HUD/*` with clear ownership boundaries:
  - Interfaces (`Interfaces/*`), base classes (`Base/*`), managers/orchestrator/integration, shared utilities/config/data, and concrete components.

- Character menu subsystem lives under `Services/CharacterMenu/*`:
  - Tabs implement `ICharacterMenuTab` and typically derive from `CharacterMenuTabBase`.
  - Shared UI creation helpers live in `Services/CharacterMenu/Shared/UIFactory.cs`.
  - Orchestrator + integration manage lifecycle and wiring.

- Backwards compatibility policy:
  - Public APIs remain stable; legacy nested static classes delegate to extracted manager classes.

- Optional debug tooling lives in a separate plugin:
  - Debug plugin: `Tools/VDebug` (GUID: `com.dinasor.vdebug`)
  - EclipsePlus calls it via reflection: `Services/DebugToolsBridge.cs` (safe no-op when not installed)
  - Debug-only Info logs (registration payload, Bloodcraft stats, chat command echo) go to VDebug and are silent without it

- Il2Cpp constraints to remember:
  - Avoid ambiguous `Object.Destroy()` → prefer `UnityEngine.Object.Destroy()`. 
  - Avoid `new RectOffset(left,right,top,bottom)`; set properties explicitly.   

- Old monolithic sources are archived at:
  - `Docs/Archive/OldServices/*`
