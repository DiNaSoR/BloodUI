using System.Text.RegularExpressions;

namespace VRiseUI.Data;

/// <summary>
/// Bridge for sending chat commands and receiving responses from mods.
/// </summary>
public static class CommandBridge
{
    private static readonly Queue<PendingCommand> _pendingCommands = new();
    private static readonly Dictionary<string, Action<string>> _responseHandlers = new();

    /// <summary>
    /// Event raised when a chat message is received.
    /// </summary>
    public static event Action<string> OnChatMessage;

    /// <summary>
    /// Send a chat command.
    /// </summary>
    /// <param name="command">The command to send (e.g., ".xp")</param>
    /// <param name="onResponse">Optional callback for the response</param>
    public static void SendCommand(string command, Action<string> onResponse = null)
    {
        if (string.IsNullOrEmpty(command)) return;

        Plugin.Log.LogInfo($"Sending command: {command}");

        // Store pending command with response handler
        if (onResponse != null)
        {
            var pending = new PendingCommand
            {
                Command = command,
                Callback = onResponse,
                Timestamp = DateTime.UtcNow
            };
            _pendingCommands.Enqueue(pending);
        }

        // Actually send the command via game chat
        // This will be hooked into the game's chat system
        SendChatMessage(command);
    }

    /// <summary>
    /// Register a handler for responses matching a pattern.
    /// </summary>
    public static void RegisterHandler(string pattern, Action<string> handler)
    {
        _responseHandlers[pattern] = handler;
    }

    /// <summary>
    /// Unregister a response handler.
    /// </summary>
    public static void UnregisterHandler(string pattern)
    {
        _responseHandlers.Remove(pattern);
    }

    /// <summary>
    /// Process an incoming chat message.
    /// Called from chat hook patch.
    /// </summary>
    public static void ProcessMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        // Notify subscribers
        OnChatMessage?.Invoke(message);

        // Check pending commands
        while (_pendingCommands.Count > 0)
        {
            var pending = _pendingCommands.Peek();

            // Timeout old commands (5 seconds)
            if ((DateTime.UtcNow - pending.Timestamp).TotalSeconds > 5)
            {
                _pendingCommands.Dequeue();
                continue;
            }

            // Invoke callback and remove
            pending.Callback?.Invoke(message);
            _pendingCommands.Dequeue();
            break;
        }

        // Check registered handlers
        foreach (var (pattern, handler) in _responseHandlers)
        {
            try
            {
                if (Regex.IsMatch(message, pattern))
                {
                    handler(message);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Handler error for pattern '{pattern}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Send a message through the game's chat system.
    /// </summary>
    private static void SendChatMessage(string message)
    {
        // TODO: Hook into game's chat input
        // This will use ProjectM.UI chat system
        // For now, log the intent
        Plugin.Log.LogInfo($"[CHAT] Would send: {message}");
    }

    private class PendingCommand
    {
        public string Command { get; set; }
        public Action<string> Callback { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
