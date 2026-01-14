using System.Collections;
using UnityEngine;

namespace VAuction.Systems;

internal static class AuctionTimerSystem
{
    static Coroutine _routine;

    public static void Start()
    {
        if (_routine != null) return;
        _routine = Core.StartCoroutine(Loop());
    }

    public static void Stop()
    {
        if (_routine == null) return;
        Core.StopCoroutine(_routine);
        _routine = null;
    }

    static IEnumerator Loop()
    {
        while (true)
        {
            try
            {
                Core.AuctionService?.ProcessExpirations();
            }
            catch (System.Exception ex)
            {
                Core.Log.LogWarning($"[VAuction] AuctionTimerSystem error: {ex.Message}");
            }

            yield return new WaitForSeconds(1.0f);
        }
    }
}

