using BepInEx;
using System.IO;
using TMPro;
using UnityEngine;

namespace VDebug.Services;

public static class FontService
{
    private static TMP_FontAsset _customFont;
    private static bool _triedLoading;

    public static TMP_FontAsset GetFont()
    {
        if (_customFont != null) return _customFont;
        if (_triedLoading) return null; // Fallback to default if already tried

        _triedLoading = true;
        
        try
        {
            // 1. Try to load from "vdebug.bundle" in plugin folder (Best for custom/distributed fonts)
            // This bypasses OS font stripping issues by using Unity's native AssetBundle format
            string bundlePath = Path.Combine(Paths.PluginPath, "vdebug.bundle");
            if (!File.Exists(bundlePath))
                bundlePath = Path.Combine(Paths.PluginPath, "VDebug", "vdebug.bundle");

            if (File.Exists(bundlePath))
            {
                 try 
                 {
                     VDebugLog.Log.LogInfo($"[VDebug] Loading AssetBundle from: {bundlePath}");
                     AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
                     if (bundle != null)
                     {
                         // Try loading TMP_FontAsset first (pre-cooked)
                         var assets = bundle.LoadAllAssets<TMP_FontAsset>();
                         if (assets != null && assets.Length > 0)
                         {
                             _customFont = assets[0];
                             VDebugLog.Log.LogInfo($"[VDebug] Loaded FontAsset from bundle: {_customFont.name}");
                             return _customFont;
                         }

                         // Try loading raw Font and converting
                         var fonts = bundle.LoadAllAssets<Font>();
                         if (fonts != null && fonts.Length > 0)
                         {
                             _customFont = TMP_FontAsset.CreateFontAsset(fonts[0]);
                             VDebugLog.Log.LogInfo($"[VDebug] Created FontAsset from bundle font: {fonts[0].name}");
                             return _customFont;
                         }
                         
                         bundle.Unload(false); // Unload metadata but keep assets? Actually better keep loaded or manage lifecycle.
                         // For a singleton service, keeping it loaded is fine.
                     }
                 }
                 catch (Exception ex)
                 {
                     VDebugLog.Log.LogWarning($"[VDebug] Failed to load bundle: {ex.Message}");
                 }
            }

            // 2. Try to load system fonts via Unity API
            // Note: CreateDynamicFontFromOSFont might be stripped in IL2CPP builds
            string[] fontNames = { "Segoe UI Emoji", "Arial", "Consolas" };
            if (Plugin.CustomFontName != null && !string.IsNullOrEmpty(Plugin.CustomFontName.Value))
            {
                // Prepend custom font
                string[] newFonts = new string[fontNames.Length + 1];
                newFonts[0] = Plugin.CustomFontName.Value;
                Array.Copy(fontNames, 0, newFonts, 1, fontNames.Length);
                fontNames = newFonts;
            }

            foreach (var fontName in fontNames)
            {
                try 
                {
                    // Check if OS has it
                    string[] osFonts = Font.GetOSInstalledFontNames();
                    bool osHasIt = false;
                    foreach(var f in osFonts) if (f.Equals(fontName, StringComparison.OrdinalIgnoreCase)) osHasIt = true;

                    if (osHasIt)
                    {
                        VDebugLog.Log.LogInfo($"[VDebug] Attempting to load OS font: {fontName}");
                        Font osFont = Font.CreateDynamicFontFromOSFont(fontName, 16);
                        if (osFont != null)
                        {
                            _customFont = TMP_FontAsset.CreateFontAsset(osFont);
                            _customFont.name = $"VDebug_{fontName}";
                            return _customFont;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue to next candidate
                    VDebugLog.Log.LogWarning($"[VDebug] Failed to load OS font '{fontName}': {ex.Message}");
                }
            }

            // Fallback: Try to find existing fonts in the game
            VDebugLog.Log.LogInfo("[VDebug] Fallback: Searching for in-game fonts...");
            var allFonts = Resources.FindObjectsOfTypeAll<Font>();
            foreach (var f in allFonts)
            {
                // Try to find a good fallback like Arial
                if (f.name.Contains("Arial") || f.name.Contains("Liberation"))
                {
                    VDebugLog.Log.LogInfo($"[VDebug] Found in-game font: {f.name}");
                    _customFont = TMP_FontAsset.CreateFontAsset(f);
                    return _customFont;
                }
            }
            
            // If we found ANY font, use it?
            if (allFonts.Length > 0)
            {
                 VDebugLog.Log.LogInfo($"[VDebug] Using first available in-game font: {allFonts[0].name}");
                 _customFont = TMP_FontAsset.CreateFontAsset(allFonts[0]);
                 return _customFont;
            }

            // Try built-in Arial explicitly
            Font builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (builtin != null)
            {
                VDebugLog.Log.LogInfo("[VDebug] Using built-in Unity Arial font.");
                _customFont = TMP_FontAsset.CreateFontAsset(builtin);
                return _customFont;
            }


        }
        catch (System.Exception ex)
        {
            VDebugLog.Log.LogWarning($"[VDebug] Failed to load custom font: {ex.Message}");
        }

        return null;
    }
}
