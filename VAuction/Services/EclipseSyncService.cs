using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using VAuction.EclipseBridge;

namespace VAuction.Services;

/// <summary>
/// Sends server→client VAuction payloads to EclipsePlus using the shared MAC key, when available.
/// </summary>
internal sealed class EclipseSyncService
{
    public bool IsEnabled => Services.Config.VAuctionConfigService.EnableEclipseSync && Core.NEW_SHARED_KEY != null && Core.NEW_SHARED_KEY.Length > 0;

    public void TrySend(User user, string messageWithoutMac)
    {
        if (!IsEnabled) return;
        if (user.Equals(null)) return;
        if (string.IsNullOrWhiteSpace(messageWithoutMac)) return;

        string messageWithMac = MacSigner.AppendMac(messageWithoutMac, Core.NEW_SHARED_KEY);
        if (string.IsNullOrEmpty(messageWithMac)) return;

        // Use the simplest working path shared by Bloodcraft: system message to user.
        FixedString512Bytes fixedMessage = new(messageWithMac);
        ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, ref fixedMessage);
    }
}

