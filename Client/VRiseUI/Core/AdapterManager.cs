using System.IO;
using System.Text.Json;
using VRiseUI.Adapters.Models;

namespace VRiseUI.Core;

/// <summary>
/// Discovers and manages mod adapters from the Adapters folder.
/// </summary>
public static class AdapterManager
{
    private static readonly Dictionary<string, Adapter> _adapters = new();

    /// <summary>
    /// All loaded adapters.
    /// </summary>
    public static IReadOnlyDictionary<string, Adapter> Adapters => _adapters;

    /// <summary>
    /// Initialize the adapter manager and discover adapters.
    /// </summary>
    public static void Initialize()
    {
        Plugin.Log.LogInfo("Initializing Adapter Manager...");
        DiscoverAdapters();
    }

    /// <summary>
    /// Shutdown and unload all adapters.
    /// </summary>
    public static void Shutdown()
    {
        foreach (var adapter in _adapters.Values)
        {
            adapter.Unload();
        }
        _adapters.Clear();
    }

    /// <summary>
    /// Discover adapters from the Adapters folder.
    /// </summary>
    private static void DiscoverAdapters()
    {
        var adaptersPath = AssetLoader.AdaptersPath;
        if (!Directory.Exists(adaptersPath))
        {
            Plugin.Log.LogWarning($"Adapters folder not found: {adaptersPath}");
            return;
        }

        foreach (var adapterDir in Directory.GetDirectories(adaptersPath))
        {
            var manifestPath = Path.Combine(adapterDir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Plugin.Log.LogWarning($"No manifest.json in adapter folder: {adapterDir}");
                continue;
            }

            try
            {
                LoadAdapter(adapterDir, manifestPath);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load adapter from {adapterDir}: {ex.Message}");
            }
        }

        Plugin.Log.LogInfo($"Discovered {_adapters.Count} adapter(s).");
    }

    /// <summary>
    /// Load a single adapter from its folder.
    /// </summary>
    private static void LoadAdapter(string adapterDir, string manifestPath)
    {
        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<Manifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (manifest == null)
        {
            Plugin.Log.LogError($"Failed to parse manifest: {manifestPath}");
            return;
        }

        var adapterId = Path.GetFileName(adapterDir);
        var adapter = new Adapter(adapterId, adapterDir, manifest);

        _adapters[adapterId] = adapter;
        Plugin.Log.LogInfo($"Loaded adapter: {manifest.DisplayName} v{manifest.ModVersion} for {manifest.ModId}");
    }

    /// <summary>
    /// Get an adapter by ID.
    /// </summary>
    public static Adapter GetAdapter(string adapterId)
    {
        return _adapters.TryGetValue(adapterId, out var adapter) ? adapter : null;
    }
}
