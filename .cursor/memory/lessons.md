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

## L-014 — Do not resolve Server World in static initializers (plugin load order)

### Status
- Active

### Tags
- [Server] [Init] [Reliability]

### Introduced
- 2026-01-14

### Symptom
- Server fails to load plugin with `System.TypeInitializationException` and `There is no Server world!`.

### Root cause
- Plugin type initializer accessed `World.s_AllWorlds` too early (static property initializer), before the Server world existed.

### Wrong approach (DO NOT REPEAT)
- `public static World Server { get; } = GetServerWorld() ?? throw ...;` in a type initializer for server mods.

### Correct approach
- Use lazy resolution and/or delayed initialization:
  - Poll until the Server world exists, then initialize services and register commands.

### Rule
> Any initialization that depends on Unity/ProjectM world availability must be deferred (lazy or delayed), not done in static initializers.

### References
- Files:
  - `VAuction/Core.cs`
  - `VAuction/Plugin.cs`

---

## L-015 — IL2CPP: AddComponent<T> on custom managed MonoBehaviours can crash during plugin load

### Status
- Active

### Tags
- [IL2CPP] [Unity] [Init] [Reliability]

### Introduced
- 2026-01-14

### Symptom
- Server plugin fails to load with a `System.TypeInitializationException` for `MethodInfoStoreGeneric_AddComponent_Public_T_0`1`.

### Root cause
- `GameObject.AddComponent<T>()` was called with a custom managed `MonoBehaviour` type that was not injected/registered for IL2CPP.

### Wrong approach (DO NOT REPEAT)
- Using `new GameObject(...).AddComponent<MyManagedMonoBehaviour>()` as a coroutine runner in IL2CPP without type injection.

### Correct approach
- Use a known IL2CPP component type as the coroutine runner (pattern used in this repo):
  - `new GameObject(...).AddComponent<IgnorePhysicsDebugSystem>()`
- Or, if you truly need a custom managed MonoBehaviour, register it via the established IL2CPP type injection mechanism before use.

### Rule
> In IL2CPP mods, do not `AddComponent<T>` for custom managed `MonoBehaviour` types unless they are explicitly injected/registered.

### References
- Files:
  - `VAuction/Core.cs`

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

## L-018 — Layout drag/hover must use canvas-local coordinates and root UI camera

### Status
- Active

### Tags
- [UI] [Layout] [Input]

### Introduced
- 2026-01-15

### Symptom
- Layout mode hover fails on progress bars and dragging moves elements away from the mouse.

### Root cause
- Screen-space mouse deltas and `Camera.main` were used for UI elements that live on scaled canvases and screen-space camera roots.

### Wrong approach (DO NOT REPEAT)
- Applying `Input.mousePosition` deltas directly to `anchoredPosition` or using `Camera.main` for screen-to-UI conversions.

### Correct approach
- Convert screen points to parent-local coordinates with `RectTransformUtility` using the root canvas camera, then apply deltas in `anchoredPosition` space.
- For hover hit-tests/outlines, compute canvas-relative bounds via `RectTransformUtility.CalculateRelativeRectTransformBounds` and compare against the mouse converted with `ScreenPointToLocalPointInRectangle` (CanvasScaler-safe).
- Ownership: `Services/LayoutService.cs`

### Rule
> UI drag/hover math must use the correct canvas camera and parent-local coordinates; never mix screen pixels with `anchoredPosition`.

### References
- Files:
  - `Services/LayoutService.cs`
- Related journal entry:
  - `journal/2026-01.md#2026-01-15`

---

## L-016 — BepInEx.PluginInfoProps: `MyPluginInfo` is generated under `RootNamespace`

### Status
- Active

### Tags
- [Build] [DX]

### Introduced
- 2026-01-14

### Symptom
- A new Tools/aux plugin fails to compile with `The name 'MyPluginInfo' does not exist in the current context`.

### Root cause
- `BepInEx.PluginInfoProps` writes `MyPluginInfo` into `namespace $(RootNamespace)`, and the default `RootNamespace` is the `.csproj` filename (not the plugin’s code namespace or `<AssemblyName>`).

### Wrong approach (DO NOT REPEAT)
- Assuming `MyPluginInfo` is generated in the global namespace or matches `<AssemblyName>`.

### Correct approach
- Set `<RootNamespace>` to the namespace where `MyPluginInfo` is referenced, or fully qualify the generated namespace.

### Rule
> Any project using `MyPluginInfo` must ensure `<RootNamespace>` matches the code namespace that references it.

### References
- Files:
  - `Tools/VDebug/VDebug.csproj`

---

## L-017 — Optional plugin integration: don’t depend on `Chainloader` (BepInEx 6 IL2CPP)

### Status
- Active

### Tags
- [Integration] [IL2CPP] [Reliability]

### Introduced
- 2026-01-14

### Symptom
- Client plugin code fails to compile when referencing `BepInEx.Bootstrap.Chainloader` (e.g. `Chainloader.PluginInfos`).

### Root cause
- In this repo’s build setup (BepInEx 6 IL2CPP), plugin projects do not have a public `Chainloader` API available at compile time.

### Wrong approach (DO NOT REPEAT)
- Using `Chainloader.PluginInfos` to detect optional plugins.

### Correct approach
- Discover optional plugins via reflection on loaded assemblies (stable API type/method names), and treat missing plugins as safe no-ops.

### Rule
> Optional plugin calls must be discovered via reflection and must never hard-fail when the optional plugin isn’t installed.

### References
- Files:
  - `Services/DebugToolsBridge.cs`

---

## L-013 — UI command indices must match server semantics

### Status
- Active

### Tags
- [UI] [Commands] [Reliability]

### Introduced
- 2026-01-13

### Symptom
- Clicking the first Class spell row sends `.class csp 0` and equips the default spell (e.g., “Veil of Shadow”) instead of the intended class spell.

### Root cause
- Server semantics reserve `.class csp 0` for the default class spell and use `1..N` for class spells, but the UI used zero-based row indices for command parameters (and unlock level indexing).

### Wrong approach (DO NOT REPEAT)
- Reusing zero-based UI list indices as chat command parameters when the server uses 1-based indexing (or reserves values like `0`).

### Correct approach
- Translate UI row indices to server command indices (e.g., `rowIndex + 1`), and keep any related arrays (like unlock levels) aligned to the same indexing scheme.

### Rule
> Any UI that sends server commands must match the server’s indexing/parameter contract; do not assume UI list order/zero-based indices map 1:1.

### References
- Files:
  - `Services/CharacterMenu/Tabs/ClassTab.cs`
  - `Server/Bloodcraftplus/Commands/ClassCommands.cs`
  - `Server/Bloodcraftplus/Utilities/Classes.cs`

---

## L-010 — CI: `dotnet build -t:Compile` does not create `bin/` outputs

### Status
- Active

### Tags
- [CI] [Build] [DX]

### Introduced
- 2026-01-12

### Symptom
- GitHub Actions release steps fail with unmatched files when expecting `bin/Release/.../*.dll` after running `dotnet build -t:Compile`.

### Root cause
- The `Compile` target emits assemblies to `obj/...` (intermediate output); `Build` is responsible for copying outputs to `bin/...`.

### Wrong approach (DO NOT REPEAT)
- Using `-t:Compile` and then uploading/releasing from `bin/...` paths.

### Correct approach
- Use `dotnet build` (default `Build` target) and disable optional post-build steps via properties.
- If compile-only is required, release from `obj/...` (intermediate) paths intentionally.

### Rule
> If CI expects outputs under `bin/...`, do not use `-t:Compile` (or adjust artifact paths accordingly).

### References
- Files:
  - `.github/workflows/build.yml`
  - `.github/workflows/release.yml`

---

## L-012 — GitHub Releases: uploaded assets must have unique names

### Status
- Active

### Tags
- [CI] [Release] [DX]

### Introduced
- 2026-01-12

### Symptom
- `softprops/action-gh-release` fails during asset upload when multiple files share the same basename (e.g., `CHANGELOG.md`), resulting in errors like `404 Not Found` / unmatched uploads.

### Root cause
- GitHub release assets are keyed by asset name; `action-gh-release` uses the file basename as the asset name, so two different paths with the same basename collide.

### Wrong approach (DO NOT REPEAT)
- Listing both `CHANGELOG.md` and `Server/Bloodcraftplus/CHANGELOG.md` under `files:` (same asset name).

### Correct approach
- Ensure each uploaded asset has a unique filename (copy/rename before uploading).

### Rule
> Never upload multiple release assets with the same basename; rename/copy first so asset names are unique.

### References
- Files:
  - `.github/workflows/build.yml`

---

## L-011 — Character menu tabs: keep `BloodcraftTab` enum and tab wiring consistent

### Status
- Active

### Tags
- [UI] [Build] [DX]

### Introduced
- 2026-01-12

### Symptom
- Build fails with `CS0117`/`CS0103` after tab refactors (e.g., references to `BloodcraftTab.Professions` or `professionsRoot` remain after renaming the tab to `Progression`).

### Root cause
- Refactor renamed the top-level tab concept, but legacy wiring/integration code still referenced the old enum member and UI root variable.

### Wrong approach (DO NOT REPEAT)
- Renaming tabs without updating:
  - `DataService.BloodcraftTab`
  - Legacy UI roots/visibility logic
  - Orchestrator registration and accessors

### Correct approach
- Treat `BloodcraftTab` as the single source of truth for top-level tabs.
- Ensure all wiring uses the same tab key (e.g., `Progression`) and registers the correct owning tab (e.g., `ProgressionTab`, not its embedded sub-tabs).

### Rule
> Any tab rename/refactor must update the enum + UI roots + orchestrator registration/accessors together; no stale tab identifiers may remain.

### References
- Files:
  - `Services/DataService.cs`
  - `Services/CharacterMenuService.cs`
  - `Services/CharacterMenu/CharacterMenuIntegration.cs`

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
  - `Server/Bloodcraftplus/Bloodcraftplus.csproj`
  - `Server/Bloodcraftplus/.codex/install.sh`
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
---

## L-019 — Layout hover/outline must handle zero-size root rects

### Status
- Active

### Tags
- [UI] [Layout]

### Introduced
- 2026-01-15

### Symptom
- Layout mode cannot hover or drag HUD bars; outlines never appear.

### Root cause
- Many HUD elements register a root `RectTransform` with a zero-size rect, and hover detection only used child `Graphic` bounds.

### Wrong approach (DO NOT REPEAT)
- Assuming the registered `RectTransform` has non-zero size or that `Graphic` components always exist.

### Correct approach
- Fall back to child `RectTransform` bounds when `Graphic` bounds are empty.
- Ownership: `Services/LayoutService.cs`

### Rule
> Layout hit-tests must fall back to child `RectTransform` bounds when the root rect has no size or no `Graphic` components.

### References
- Files:
  - `Services/LayoutService.cs`
- Related journal entry:
  - `journal/2026-01.md#2026-01-15`

---

## L-020 — Documentation is part of the deliverable (update docs + changelog)

### Status
- Active

### Tags
- [Docs] [DX] [Process]

### Introduced
- 2026-01-17

### Symptom
- Features ship without documentation; users don't know how to use new features; changelog is outdated.

### Root cause
- Documentation updates were not treated as part of the implementation task.

### Wrong approach (DO NOT REPEAT)
- Implementing features without updating the relevant docs pages or changelog.

### Correct approach
- After ANY feature/fix:
  1. Update the relevant content page in `Docs/src/content/`.
  2. Add a `<ChangelogSection>` entry to `Docs/src/content/reference/changelog.mdx`.
  3. If new commands: update `Docs/src/content/reference/commands.mdx`.
  4. If new config: update `Docs/src/content/reference/config.mdx`.
  5. If client UI changes: update `Docs/src/content/client/*.mdx` with screenshots.
  6. If server system changes: update `Docs/src/content/server/*.mdx`.

### Rule
> Never ship a feature without documenting it. The docs site IS part of the deliverable.

### References
- Files:
  - `Docs/src/content/**/*.mdx`
  - `.cursor/memory/hot-rules.md`
  - `.cursor/memory/memo.md`