using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VAuction.Resources;

internal class Secrets
{
    [JsonPropertyName("NEW_SHARED_KEY")]
    public string NewSharedKey { get; set; }
}

internal static class SecretManager
{
    static Secrets _secrets;

    static SecretManager()
    {
        LoadSecrets();
    }

    static void LoadSecrets()
    {
        // This file is intentionally not committed. It must exist locally for Eclipse sync.
        const string resourceName = "VAuction.Resources.secrets.json";
        Assembly assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' not found in assembly.");
        using var reader = new StreamReader(stream);

        string jsonContent = reader.ReadToEnd();
        _secrets = JsonSerializer.Deserialize<Secrets>(jsonContent)
            ?? throw new InvalidOperationException("Failed to deserialize secrets.json.");
    }

    public static string GetNewSharedKey()
    {
        return _secrets.NewSharedKey;
    }
}

