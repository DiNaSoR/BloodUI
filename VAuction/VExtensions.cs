using Stunlock.Core;
using VAuction.Services;

namespace VAuction;

internal static class VExtensions
{
    const string EMPTY_KEY = "LocalizationKey.Empty";

    public static string GetPrefabName(this PrefabGUID prefabGuid)
    {
        return PrefabNameService.GetPrefabName(prefabGuid);
    }

    public static string GetLocalizedName(this PrefabGUID prefabGuid)
    {
        // VAuction doesn't ship its own localization table; we use prefab asset name as display name.
        string name = PrefabNameService.GetPrefabName(prefabGuid);
        return string.IsNullOrWhiteSpace(name) ? EMPTY_KEY : name;
    }
}

