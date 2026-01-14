using BepInEx.Unity.IL2CPP.Utils.Collections;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using VAuction.Resources;
using VAuction.Services;

namespace VAuction;

internal static class Core
{
    static World _server;
    static SystemService _systemService;
    static ServerGameManager _serverGameManager;

    public static World Server => _server ?? throw new Exception("There is no Server world!");
    public static EntityManager EntityManager => Server.EntityManager;
    public static SystemService SystemService => _systemService ??= new SystemService(Server);
    public static ServerGameManager ServerGameManager
    {
        get
        {
            if (_serverGameManager.Equals(default(ServerGameManager)))
            {
                _serverGameManager = SystemService.ServerScriptMapper.GetServerGameManager();
            }

            return _serverGameManager;
        }
    }

    public static BepInEx.Logging.ManualLogSource Log => Plugin.LogInstance;

    public static byte[] NEW_SHARED_KEY { get; private set; }
    public static Services.AuctionService AuctionService { get; private set; }
    public static Services.EclipseSyncService EclipseSyncService { get; private set; }

    static bool _initialized;
    static MonoBehaviour _monoBehaviour;
    static Coroutine _initRoutine;

    /// <summary>
    /// Attempts to initialize VAuction. Returns false if the Server world is not available yet.
    /// </summary>
    public static bool TryInitialize()
    {
        if (_initialized) return true;

        if (!TryResolveServerWorld(out World serverWorld))
        {
            return false;
        }

        _server = serverWorld;

        // Shared key is optional (depends on local secrets.json which is not committed).
        try
        {
            string base64 = SecretManager.GetNewSharedKey();
            NEW_SHARED_KEY = string.IsNullOrWhiteSpace(base64) ? null : Convert.FromBase64String(base64);
        }
        catch (Exception ex)
        {
            NEW_SHARED_KEY = null;
            Log.LogWarning($"[VAuction] Failed to load NEW_SHARED_KEY (Eclipse sync disabled): {ex.Message}");
        }

        Services.Config.VAuctionConfigService.InitializeConfig();
        Persistence.AuctionRepository.Initialize();
        Persistence.EscrowRepository.Initialize();

        Services.PrefabNameService.Initialize();

        AuctionService = new Services.AuctionService();
        EclipseSyncService = new Services.EclipseSyncService();

        _initialized = true;
        return true;
    }

    static bool TryResolveServerWorld(out World serverWorld)
    {
        serverWorld = null;

        try
        {
            serverWorld = World.s_AllWorlds.ToArray().FirstOrDefault(world => world != null && world.Name == "Server");
            return serverWorld != null;
        }
        catch
        {
            return false;
        }
    }

    static MonoBehaviour GetOrCreateMonoBehaviour()
    {
        return _monoBehaviour ??= CreateMonoBehaviour();
    }

    static MonoBehaviour CreateMonoBehaviour()
    {
        // NOTE: In IL2CPP, adding a custom managed MonoBehaviour via AddComponent<T> can fail unless the type is injected.
        // Use a known IL2CPP component type (same pattern as Bloodcraft) as a safe coroutine runner.
        MonoBehaviour monoBehaviour = new GameObject(MyPluginInfo.PLUGIN_NAME).AddComponent<IgnorePhysicsDebugSystem>();
        UnityEngine.Object.DontDestroyOnLoad(monoBehaviour.gameObject);
        return monoBehaviour;
    }

    public static Coroutine StartCoroutine(IEnumerator routine)
    {
        return GetOrCreateMonoBehaviour().StartCoroutine(routine.WrapToIl2Cpp());
    }

    public static void StopCoroutine(Coroutine routine)
    {
        GetOrCreateMonoBehaviour().StopCoroutine(routine);
    }

    public static void BeginInitializeWithRetry(Action onInitialized)
    {
        if (_initialized)
        {
            onInitialized?.Invoke();
            return;
        }

        if (_initRoutine != null)
        {
            return;
        }

        _initRoutine = StartCoroutine(InitializeRetryRoutine(onInitialized));
    }

    static IEnumerator InitializeRetryRoutine(Action onInitialized)
    {
        // Poll a few times until the Server world is ready.
        var wait = new WaitForSeconds(0.5f);

        while (!_initialized)
        {
            if (TryInitialize())
            {
                break;
            }

            yield return wait;
        }

        onInitialized?.Invoke();
        _initRoutine = null;
    }
}

