using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Unity.Entities;

namespace VAuction.Services;

/// <summary>
/// Minimal server-side system accessor (mirrors Bloodcraft's pattern).
/// </summary>
internal sealed class SystemService
{
    readonly World _world;

    public SystemService(World world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    ServerScriptMapper _serverScriptMapper;
    public ServerScriptMapper ServerScriptMapper => _serverScriptMapper ??= GetSystem<ServerScriptMapper>();

    PrefabCollectionSystem _prefabCollectionSystem;
    public PrefabCollectionSystem PrefabCollectionSystem => _prefabCollectionSystem ??= GetSystem<PrefabCollectionSystem>();

    T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>()
            ?? throw new InvalidOperationException($"[{_world.Name}] - failed to get ({Il2CppType.Of<T>().FullName})");
    }
}

