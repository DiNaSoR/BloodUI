using System.IO;
using UnityEngine;

namespace VRiseUI.Core;

/// <summary>
/// Loads images from adapter asset folders.
/// </summary>
public static class AssetLoader
{
    private static readonly Dictionary<string, Texture2D> _textureCache = new();
    private static readonly Dictionary<string, Sprite> _spriteCache = new();
    private static string _adapterBasePath;

    /// <summary>
    /// Initialize the asset loader.
    /// </summary>
    public static void Initialize()
    {
        _adapterBasePath = Path.Combine(
            Path.GetDirectoryName(typeof(Plugin).Assembly.Location),
            "Adapters"
        );

        // Create adapters folder if it doesn't exist
        if (!Directory.Exists(_adapterBasePath))
        {
            Directory.CreateDirectory(_adapterBasePath);
        }

        Plugin.Log.LogInfo($"Asset loader initialized. Adapters path: {_adapterBasePath}");
    }

    /// <summary>
    /// Shutdown and clear caches.
    /// </summary>
    public static void Shutdown()
    {
        _textureCache.Clear();
        _spriteCache.Clear();
    }

    /// <summary>
    /// Gets the base path for adapters.
    /// </summary>
    public static string AdaptersPath => _adapterBasePath;

    /// <summary>
    /// Load a texture from an adapter's assets folder.
    /// </summary>
    /// <param name="adapterId">The adapter ID (folder name)</param>
    /// <param name="relativePath">Relative path within the adapter folder</param>
    /// <returns>The loaded texture, or null if not found</returns>
    public static Texture2D LoadTexture(string adapterId, string relativePath)
    {
        var fullPath = Path.Combine(_adapterBasePath, adapterId, relativePath);
        var cacheKey = fullPath.ToLowerInvariant();

        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        if (!File.Exists(fullPath))
        {
            Plugin.Log.LogWarning($"Texture not found: {fullPath}");
            return null;
        }

        try
        {
            var bytes = File.ReadAllBytes(fullPath);
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(bytes))
            {
                _textureCache[cacheKey] = texture;
                return texture;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogError($"Failed to load texture {fullPath}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Load a sprite from an adapter's assets folder.
    /// </summary>
    public static Sprite LoadSprite(string adapterId, string relativePath)
    {
        var fullPath = Path.Combine(_adapterBasePath, adapterId, relativePath);
        var cacheKey = fullPath.ToLowerInvariant();

        if (_spriteCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var texture = LoadTexture(adapterId, relativePath);
        if (texture == null) return null;

        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        _spriteCache[cacheKey] = sprite;
        return sprite;
    }
}
