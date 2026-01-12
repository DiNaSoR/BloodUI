## L-001 — Name-based binding is ambiguous; always bind by slot ID

### Status
- Active

### Tags
- [UI] [Data] [Reliability]

### Introduced
- 2025-12-31

### Symptom
- Clicking a familiar row binds/summons the wrong familiar when multiple familiars share similar names
  (e.g., “Skeleton” vs “Skeleton Crossbow”).

### Root cause
- Name-based selection is not unique and can collide with localized/variant names or overlapping prefixes.

### Wrong approach (DO NOT REPEAT)
- Binding/smartbinding by displayed name or fuzzy matching.

### Correct approach
- Bind by stable slot identity:
  - Use `.fam b #` (slot-based binding)
  - When rebinding, apply a delayed unbind→bind routine to ensure the final state is correct.

### Rule
> Any bind/summon action must be keyed by slot/ID, not by display name.

### References
- Files:
  - `Services/CharacterMenuService.cs`

---

## L-002 — Missing sprite icons must be hidden/fallback (no white placeholders)

### Status
- Active

### Tags
- [UI] [Assets]

### Introduced
- 2025-12-31

### Symptom
- Header/action icons appear as white placeholder boxes when sprite names fail to resolve.

### Root cause
- Sprite names can be missing, incorrect, or not allowlisted for HUD usage.

### Wrong approach (DO NOT REPEAT)
- Assuming sprite names always resolve and always rendering the icon element.

### Correct approach
- Prefer manifest-backed sprite names.
- If sprite lookup fails, hide the icon element instead of rendering a placeholder.
- Ensure required sprites are present in the HUD sprite allowlist when relevant.

### Rule
> Any UI icon must either resolve to a valid sprite or be hidden (no placeholders).

### References
- Files:
  - `Services/CharacterMenuService.cs`
  - `Services/HUD/Shared/HudData.cs`
---

## L-003 — Visual dividers must participate in layout (avoid overlap/drift)

### Status
- Active

### Tags
- [UI] [Layout]

### Introduced
- 2025-12-31

### Symptom
- Columns overlap or ignore the divider, causing spacing issues and drift as content changes.

### Root cause
- Divider rendered as a purely visual element not included in layout calculations, so columns don't reserve space for it.

### Wrong approach (DO NOT REPEAT)
- Adding a divider that does not affect layout sizing/constraints.

### Correct approach
- Make the divider a real layout participant so left/right columns respect it.
- Re-check spacing with smaller typography and dynamic content updates.

### Rule
> Any divider separating layout regions must be part of layout constraints, not just a visual overlay.

### References
- Files:
  - `Services/CharacterMenuService.cs`
---

## L-004 — Il2Cpp: avoid RectOffset 4-arg constructor

### Status
- Active

### Tags
- [Build] [Compat]

### Introduced
- 2025-12-28

### Symptom
- Build/runtime failure when using `new RectOffset(left, right, top, bottom)`.

### Root cause
- Il2Cpp environment does not support the 4-argument `RectOffset` constructor reliably.

### Wrong approach (DO NOT REPEAT)
- Instantiating padding via `new RectOffset(left, right, top, bottom)`.

### Correct approach
- Create the object with a default constructor and assign properties explicitly:
  - `RectOffset padding = new(); padding.left = ...; padding.right = ...; padding.top = ...; padding.bottom = ...;`

### Rule
> In Il2Cpp targets, construct `RectOffset` with `new()` and set fields explicitly; do not use the 4-arg ctor.

### References
- Files:
  - `Services/CharacterMenu/Shared/UIFactory.cs`

---

## L-009 — MSBuild post-build targets must not break builds

### Status
- Active

### Tags
- [Build] [DX] [Reliability]

### Introduced
- 2026-01-12

### Symptom
- `dotnet build` fails on machines without a .NET 6 runtime due to a post-build `Exec` that runs the built `net6.0` assembly.
- Deploy copy steps can fail or copy the wrong DLL when `<AssemblyName>` differs from the project filename.

### Root cause
- Post-build steps were unconditional and depended on local machine state (installed runtimes / writable Steam install paths).
- Copy tasks used `$(ProjectName).dll` instead of the actual build output path.

### Wrong approach (DO NOT REPEAT)
- Hardcoding post-build `Exec`/deploy steps as always-on.
- Copying `$(TargetDir)$(ProjectName).dll` when `AssemblyName` is not the same as `ProjectName`.

### Correct approach
- Copy using `$(TargetPath)` (the actual primary build output).
- Guard optional post-build steps behind properties/conditions, and make deploy copies non-fatal (`ContinueOnError`) so a clean build is always possible.

### Rule
> Build must be able to succeed without local deploy paths or runtime prerequisites; optional post-build actions must be guarded and copy from `$(TargetPath)`.

### References
- Files:
  - `Eclipse.csproj`
  - `Docs/Bloodcraft/Bloodcraft.csproj`
  - `Docs/Bloodcraft/.codex/install.sh`
  - `Services/HUD/Shared/*`
---

## L-005 — Unity: qualify Object.Destroy to avoid ambiguity

### Status
- Active

### Tags
- [Build] [Compat]

### Introduced
- 2025-12-28

### Symptom
- Compilation errors due to ambiguous `Object` type resolution.

### Root cause
- Multiple `Object` types exist in scope; unqualified `Object.Destroy()` may bind incorrectly.

### Wrong approach (DO NOT REPEAT)
- Calling `Object.Destroy()` without qualification.

### Correct approach
- Call `UnityEngine.Object.Destroy()` explicitly.

### Rule
> Always call `UnityEngine.Object.Destroy()` (fully qualified) to avoid ambiguous Object resolution.

### References
- Files:
  - `Services/HUD/Base/HudComponentBase.cs`
  - `Services/CharacterMenu/Base/CharacterMenuTabBase.cs`
  - `Services/CharacterMenu/Shared/UIFactory.cs`
---

## L-006 — Large refactors must preserve public APIs via delegation

### Status
- Active

### Tags
- [Architecture] [DX]

### Introduced
- 2025-12-28

### Symptom
- Downstream code breaks after refactor even if behavior is correct.

### Root cause
- Public entry points change or disappear when monoliths are split into modules.

### Wrong approach (DO NOT REPEAT)
- Renaming/removing public APIs during modularization without a compatibility layer.

### Correct approach
- Keep existing public APIs stable and delegate implementation to extracted modules/managers.
- Move internals behind orchestrators/managers while preserving old call sites.

### Rule
> During modularization, keep public APIs stable and delegate to extracted modules; do not break external call sites.

### References
- Files:
  - `Services/HUD/*`
  - `Services/CharacterMenu/*`
  - `CanvasService.cs`
  - `CharacterMenuService.cs`

---

## L-007 — ProjectM/UI name collisions: avoid Unity EventSystems pointer handler interfaces

### Status
- Active

### Tags
- [UI] [Build] [Compat]

### Introduced
- 2026-01-02

### Symptom
- Build fails when adding hover handlers like `IPointerEnterHandler` / `IPointerExitHandler` to UI components.

### Root cause
- In this repo/runtime, `IPointerEnterHandler` / `IPointerExitHandler` resolve to ProjectM/UI types (not Unity’s `UnityEngine.EventSystems` interfaces), causing invalid inheritance (C# error: “cannot have multiple base classes”).

### Wrong approach (DO NOT REPEAT)
- Implementing Unity-style pointer handler interfaces on `MonoBehaviour` for hover effects.

### Correct approach
- Prefer existing interaction components (e.g. `SimpleStunButton`) and/or ProjectM UI hooks.
- If hover feedback is required, implement it using established ProjectM-compatible patterns (or omit hover styling).

### Rule
> Do not rely on Unity EventSystems pointer handler interfaces for UI hover in this project; use ProjectM-compatible interaction paths.

---

## L-008 — UI: Non-interactive TMP text must not block raycasts

### Status
- Active

### Tags
- [UI] [Layout]

### Introduced
- 2026-01-02

### Symptom
- Header/title text appears missing (invisible), and UI elements underneath become unclickable.

### Root cause
- `CopyTextStyle(...)` can inherit a fully-transparent reference color, and `TextMeshProUGUI` defaults `raycastTarget = true`, so an invisible text element can still block raycasts.

### Wrong approach (DO NOT REPEAT)
- Creating non-interactive TMP elements without explicitly disabling raycasts and ensuring the text color is visible.

### Correct approach
- For non-interactive TMP text, always set `raycastTarget = false`.
- For header/title text created from a style reference, force a visible alpha (do not inherit a potentially-transparent reference color).

### Rule
> Any non-interactive TMP text must set `raycastTarget = false` and must not rely on a potentially-transparent reference color.

### References
- Files:
  - `Services/CharacterMenu/Shared/UIFactory.cs`
