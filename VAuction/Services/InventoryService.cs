using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace VAuction.Services;

internal static class InventoryService
{
    public static bool TryGetInventoryEntity(Entity character, out Entity inventoryEntity)
    {
        return InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, character, out inventoryEntity);
    }

    public static int GetCount(Entity inventoryEntity, PrefabGUID prefab)
    {
        return Core.ServerGameManager.GetInventoryItemCount(inventoryEntity, prefab);
    }

    public static bool TryRemove(Entity inventoryEntity, PrefabGUID prefab, int amount)
    {
        return Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, prefab, amount);
    }

    public static bool TryAdd(Entity character, PrefabGUID prefab, int amount)
    {
        var response = Core.ServerGameManager.TryAddInventoryItem(character, prefab, amount);
        return response.Success;
    }
}

