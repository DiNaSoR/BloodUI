using VRiseUI.Adapters.Models;
using VRiseUI.Core;

namespace VRiseUI.Adapters.Models;

/// <summary>
/// Represents a loaded mod adapter with its manifest and resources.
/// </summary>
public class Adapter
{
    /// <summary>
    /// Unique identifier (folder name).
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Path to the adapter folder.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The parsed manifest.
    /// </summary>
    public Manifest Manifest { get; }

    /// <summary>
    /// Whether this adapter is active (mod detected).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this adapter's UI has been initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

    public Adapter(string id, string path, Manifest manifest)
    {
        Id = id;
        Path = path;
        Manifest = manifest;
    }

    /// <summary>
    /// Load and activate this adapter's UI.
    /// </summary>
    public void Load()
    {
        if (IsInitialized) return;

        Plugin.Log.LogInfo($"Loading adapter: {Manifest.DisplayName}");

        // Create UI components from manifest
        if (Manifest.Hud?.Elements != null)
        {
            foreach (var elementDef in Manifest.Hud.Elements)
            {
                CreateComponent(elementDef);
            }
        }

        IsInitialized = true;
        IsActive = true;
    }

    /// <summary>
    /// Unload this adapter's UI.
    /// </summary>
    public void Unload()
    {
        if (!IsInitialized) return;

        Plugin.Log.LogInfo($"Unloading adapter: {Manifest.DisplayName}");

        // Unregister all components for this adapter
        // Components are identified by adapterId prefix
        // UIOrchestrator handles cleanup

        IsInitialized = false;
        IsActive = false;
    }

    /// <summary>
    /// Create a UI component from its definition.
    /// </summary>
    private void CreateComponent(HudElementDef elementDef)
    {
        // Component factory will handle creation based on type
        var component = UIComponentFactory.Create(this, elementDef);
        if (component != null)
        {
            UIOrchestrator.RegisterComponent(component);
        }
    }
}
