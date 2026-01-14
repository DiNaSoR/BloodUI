using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VAuction.Persistence;

internal static class JsonStore
{
    static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T LoadOrDefault<T>(string path, T defaultValue)
    {
        try
        {
            if (!File.Exists(path))
            {
                return defaultValue;
            }

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return defaultValue;
            }

            return JsonSerializer.Deserialize<T>(json, Options) ?? defaultValue;
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[VAuction] Failed to load {path}: {ex.Message}");
            return defaultValue;
        }
    }

    public static void SaveAtomic<T>(string path, T value)
    {
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tmp = path + ".tmp";
            string json = JsonSerializer.Serialize(value, Options);

            File.WriteAllText(tmp, json);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tmp, path);
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[VAuction] Failed to save {path}: {ex.Message}");
        }
    }
}

