using ProjectM;
using Stunlock.Core;
using Unity.Collections;

namespace VAuction.Services;

internal static class PrefabNameService
{
    static readonly Dictionary<PrefabGUID, string> _prefabNames = [];
    static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;

        try
        {
            var lookup = Core.SystemService.PrefabCollectionSystem._PrefabDataLookup;
            var keys = lookup.GetKeyArray(Allocator.Temp);
            var values = lookup.GetValueArray(Allocator.Temp);

            try
            {
                for (int i = 0; i < keys.Length; i++)
                {
                    PrefabGUID key = keys[i];
                    var data = values[i];
                    _prefabNames[key] = data.AssetName.Value;
                }
            }
            finally
            {
                keys.Dispose();
                values.Dispose();
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[VAuction] PrefabNameService init failed: {ex.Message}");
        }

        _initialized = true;
    }

    public static string GetPrefabName(PrefabGUID prefabGuid)
    {
        if (_prefabNames.TryGetValue(prefabGuid, out string name) && !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return "LocalizationKey.Empty";
    }
}

