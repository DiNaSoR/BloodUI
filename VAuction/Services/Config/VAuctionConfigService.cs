using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace VAuction.Services.Config;

internal static class VAuctionConfigService
{
    // General
    public static bool Enabled { get; private set; }
    public static int MaxListingsPerPlayer { get; private set; }
    public static int MinAuctionDurationHours { get; private set; }
    public static int MaxAuctionDurationHours { get; private set; }
    public static int HistoryRetentionDays { get; private set; }

    // Economy
    public static int PrimaryCurrencyPrefab { get; private set; }
    public static int ListingFeePercent { get; private set; }
    public static int SaleTaxPercent { get; private set; }
    public static int CancelRefundPercent { get; private set; }
    public static int MinimumPrice { get; private set; }
    public static int MaximumPrice { get; private set; }

    // Bidding
    public static int MinBidIncrementPercent { get; private set; }
    public static int AntiSnipeWindowMinutes { get; private set; }
    public static int AntiSnipeExtensionMinutes { get; private set; }

    // UI sync
    public static bool EnableEclipseSync { get; private set; }
    public static int PageSize { get; private set; }

    public static class ConfigInitialization
    {
        static readonly Regex _regexSection = new(@"^\[(.+)\]$");

        public class ConfigEntryDefinition(string section, string key, object defaultValue, string description)
        {
            public string Section { get; } = section;
            public string Key { get; } = key;
            public object DefaultValue { get; } = defaultValue;
            public string Description { get; } = description;
        }

        public static readonly List<string> SectionOrder =
        [
            "General",
            "Economy",
            "Bidding",
            "UI"
        ];

        public static readonly List<ConfigEntryDefinition> ConfigEntries =
        [
            new("General", "Enabled", true, "Enable/disable VAuction."),
            new("General", "MaxListingsPerPlayer", 10, "Maximum active listings per player."),
            new("General", "MinAuctionDurationHours", 1, "Minimum auction duration in hours."),
            new("General", "MaxAuctionDurationHours", 168, "Maximum auction duration in hours."),
            new("General", "HistoryRetentionDays", 30, "Days to retain completed auctions in history."),

            new("Economy", "PrimaryCurrencyPrefab", 576389135, "Primary currency PrefabGUID hash used for bids and buy-now."),
            new("Economy", "ListingFeePercent", 5, "Listing fee percent charged upfront."),
            new("Economy", "SaleTaxPercent", 10, "Sale tax percent charged on successful sale."),
            new("Economy", "CancelRefundPercent", 50, "Percent of listing fee refunded on cancel."),
            new("Economy", "MinimumPrice", 10, "Minimum allowed starting bid/buy-now."),
            new("Economy", "MaximumPrice", 100000, "Maximum allowed starting bid/buy-now."),

            new("Bidding", "MinBidIncrementPercent", 5, "Minimum bid increment percent."),
            new("Bidding", "AntiSnipeWindowMinutes", 5, "If bid placed within this window, extend expiry."),
            new("Bidding", "AntiSnipeExtensionMinutes", 5, "Extension duration for anti-snipe."),

            new("UI", "EnableEclipseSync", true, "Enable/disable sending VAuction payloads to EclipsePlus."),
            new("UI", "PageSize", 10, "Listings per page for Eclipse sync (keep small due to chat payload size).")
        ];

        const string DEFAULT_VALUE_LINE = "# Default value: ";

        public static void InitializeConfig()
        {
            // Load old (flat) cfg values if present.
            string configPath = Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");
            Dictionary<string, string> existingValues = [];

            if (File.Exists(configPath))
            {
                foreach (string line in File.ReadAllLines(configPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                        continue;

                    string[] keyValue = line.Split('=', 2);
                    if (keyValue.Length == 2)
                    {
                        existingValues[keyValue[0].Trim()] = keyValue[1].Trim();
                    }
                }
            }

            foreach (ConfigEntryDefinition entry in ConfigEntries)
            {
                Type entryType = entry.DefaultValue.GetType();
                Type nested = typeof(VAuctionConfigService).GetNestedType(nameof(ConfigInitialization), BindingFlags.Static | BindingFlags.Public);
                MethodInfo initMethod = nested?.GetMethod(nameof(InitConfigEntry), BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo generic = initMethod?.MakeGenericMethod(entryType);

                object valueToUse = entry.DefaultValue;
                if (existingValues.TryGetValue(entry.Key, out string existing))
                {
                    try
                    {
                        valueToUse = ConvertString(existing, entryType);
                    }
                    catch
                    {
                        valueToUse = entry.DefaultValue;
                    }
                }

                object configEntry = generic?.Invoke(null, [entry.Section, entry.Key, valueToUse, entry.Description]);
                UpdateConfigProperty(entry.Key, configEntry);
            }

            if (File.Exists(configPath))
            {
                try
                {
                    OrganizeConfig(configPath);
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"[VAuction] Failed to organize config: {ex.Message}");
                }
            }
        }

        static object ConvertString(string value, Type t)
        {
            if (t == typeof(float)) return float.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(decimal)) return decimal.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(int)) return int.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(uint)) return uint.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(long)) return long.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(ulong)) return ulong.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(short)) return short.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(ushort)) return ushort.Parse(value, CultureInfo.InvariantCulture);
            if (t == typeof(bool)) return bool.Parse(value);
            if (t == typeof(string)) return value;
            throw new NotSupportedException($"Type {t} is not supported");
        }

        static void UpdateConfigProperty(string key, object configEntry)
        {
            PropertyInfo prop = typeof(VAuctionConfigService).GetProperty(key, BindingFlags.Static | BindingFlags.Public);
            if (prop == null || !prop.CanWrite) return;

            object value = configEntry?.GetType().GetProperty("Value")?.GetValue(configEntry);
            if (value != null)
            {
                prop.SetValue(null, Convert.ChangeType(value, prop.PropertyType));
            }
        }

        static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
        {
            return Plugin.Instance.Config.Bind(section, key, defaultValue, description);
        }

        static void OrganizeConfig(string configFile)
        {
            Dictionary<string, List<string>> orderedSections = [];
            string currentSection = string.Empty;

            string[] lines = File.ReadAllLines(configFile);
            string[] fileHeader = lines.Length >= 3 ? lines[0..3] : lines;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                Match match = _regexSection.Match(trimmed);

                if (match.Success)
                {
                    currentSection = match.Groups[1].Value;
                    if (!orderedSections.ContainsKey(currentSection))
                        orderedSections[currentSection] = [];
                }
                else if (SectionOrder.Contains(currentSection))
                {
                    orderedSections[currentSection].Add(trimmed);
                }
            }

            using StreamWriter writer = new(configFile, false);

            foreach (string header in fileHeader)
                writer.WriteLine(header);

            foreach (string section in SectionOrder)
            {
                if (!orderedSections.TryGetValue(section, out List<string> sectionLines))
                    continue;

                writer.WriteLine($"[{section}]");

                foreach (string line in sectionLines)
                {
                    if (line.Contains(DEFAULT_VALUE_LINE, StringComparison.Ordinal))
                    {
                        // Leave as-is (BepInEx writes default lines).
                        writer.WriteLine(line);
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }
    }

    public static void InitializeConfig()
    {
        ConfigInitialization.InitializeConfig();
    }
}

