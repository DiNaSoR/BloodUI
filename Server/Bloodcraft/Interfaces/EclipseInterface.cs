using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Expertise;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Globalization;
using System.Text;
using Unity.Entities;
using static Bloodcraft.Interfaces.EclipseInterface;
using static Bloodcraft.Services.EclipseService;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using FamiliarBattleGroupsManager = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBattleGroupsManager;
using FamiliarPrestigeManager = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager;
using LevelingPrestigeManager = Bloodcraft.Systems.Leveling.PrestigeManager;
using Shapeshift = Bloodcraft.Interfaces.Shapeshift;

namespace Bloodcraft.Interfaces;
internal static class EclipseInterface // terrible name but do later
{
    public static readonly bool ExtraRecipes = ConfigService.ExtraRecipes;
    public static readonly int PrimalCost = ConfigService.PrimalJewelCost;
    public static readonly int MaxLevel = ConfigService.MaxLevel;
    public static readonly int MaxLegacyLevel = ConfigService.MaxBloodLevel;
    public static readonly int MaxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    public static readonly int MaxFamiliarLevel = ConfigService.MaxFamiliarLevel;
    public static readonly float PrestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
    public static readonly float ClassStatMultiplier = ConfigService.SynergyMultiplier;

    public const int MAX_PROFESSION_LEVEL = 100;
}
internal interface IVersionHandler<TProgressData>
{
    /// <summary>
    /// Sends configuration data to the client.
    /// </summary>
    /// <param name="user">The target user.</param>
    void SendClientConfig(User user);
    /// <summary>
    /// Sends periodic progress data to the client.
    /// </summary>
    /// <param name="character">The player character entity.</param>
    /// <param name="steamId">The user's Steam ID.</param>
    void SendClientProgress(Entity character, ulong steamId);
    /// <summary>
    /// Sends class system data to the client.
    /// </summary>
    /// <param name="user">The target user.</param>
    void SendClientClassData(User user);
    /// <summary>
    /// Sends prestige leaderboard data to the client.
    /// </summary>
    /// <param name="user">The target user.</param>
    void SendClientPrestigeLeaderboard(User user);
    /// <summary>
    /// Sends exoform and shapeshift data to the client.
    /// </summary>
    /// <param name="character">The player character entity.</param>
    /// <param name="user">The target user.</param>
    /// <param name="steamId">The user's Steam ID.</param>
    void SendClientExoFormData(Entity character, User user, ulong steamId);
    /// <summary>
    /// Sends familiar battle data to the client.
    /// </summary>
    /// <param name="user">The target user.</param>
    /// <param name="steamId">The user's Steam ID.</param>
    void SendClientFamiliarBattleData(User user, ulong steamId);
    /// <summary>
    /// Sends weapon stat bonus data to the client.
    /// </summary>
    /// <param name="character">The player character entity.</param>
    /// <param name="user">The target user.</param>
    /// <param name="steamId">The user's Steam ID.</param>
    void SendClientWeaponStatBonusData(Entity character, User user, ulong steamId);
    /// <summary>
    /// Builds the configuration payload.
    /// </summary>
    /// <returns>A formatted config message string.</returns>
    string BuildConfigMessage();
    /// <summary>
    /// Builds the progress payload.
    /// </summary>
    /// <param name="data">The progress snapshot.</param>
    /// <returns>A formatted progress message string.</returns>
    string BuildProgressMessage(TProgressData data);
    /// <summary>
    /// Builds the class system payload.
    /// </summary>
    /// <returns>A formatted class data message string.</returns>
    string BuildClassDataMessage();
    /// <summary>
    /// Builds the prestige leaderboard payload.
    /// </summary>
    /// <returns>A formatted leaderboard message string.</returns>
    string BuildPrestigeLeaderboardMessage();
    /// <summary>
    /// Builds the exoform and shapeshift payload.
    /// </summary>
    /// <param name="character">The player character entity.</param>
    /// <param name="steamId">The user's Steam ID.</param>
    /// <returns>A formatted exoform message string.</returns>
    string BuildExoFormDataMessage(Entity character, ulong steamId);
    /// <summary>
    /// Builds the familiar battle payload.
    /// </summary>
    /// <param name="steamId">The user's Steam ID.</param>
    /// <returns>A formatted familiar battle message string.</returns>
    string BuildFamiliarBattleDataMessage(ulong steamId);
    /// <summary>
    /// Builds the weapon stat bonus payload.
    /// </summary>
    /// <param name="character">The player character entity.</param>
    /// <param name="steamId">The user's Steam ID.</param>
    /// <returns>A formatted weapon stat bonus message string.</returns>
    string BuildWeaponStatBonusDataMessage(Entity character, ulong steamId);
}
internal static class VersionHandler
{
    const string VERSION_1_3 = "1.3";

    public static readonly Dictionary<string, object> VersionHandlers = new()
    {
        // { "1.1.2", new VersionHandler_1_1_2() },
        // { "1.2.2", new VersionHandler_1_2_2() },
        { VERSION_1_3, new VersionHandler_1_3() }
    };

#nullable enable
    public static IVersionHandler<TProgressData>? GetHandler<TProgressData>(string version)
    {
        if (VersionHandlers.TryGetValue(version, out var handler) && handler is IVersionHandler<TProgressData> typedHandler)
        {
            return typedHandler;
        }

        return null;
    }

#nullable disable
}
internal class VersionHandler_1_3 : IVersionHandler<ProgressDataV1_3>
{
    public void SendClientConfig(User user)
    {
        string message = BuildConfigMessage();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientProgress(Entity playerCharacter, ulong steamId)
    {
        Entity userEntity = playerCharacter.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        ProgressDataV1_3 data = new()
        {
            ExperienceData = GetExperienceData(steamId),
            LegacyData = GetLegacyData(playerCharacter, steamId),
            ExpertiseData = GetExpertiseData(playerCharacter, steamId),
            FamiliarData = GetFamiliarData(playerCharacter, steamId),
            ProfessionData = GetProfessionData(steamId),
            DailyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Daily),
            WeeklyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Weekly),
            ShiftSpellData = GetShiftSpellData(playerCharacter)
        };

        string message = BuildProgressMessage(data);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientClassData(User user)
    {
        string message = BuildClassDataMessage();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientPrestigeLeaderboard(User user)
    {
        string message = BuildPrestigeLeaderboardMessage();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientExoFormData(Entity playerCharacter, User user, ulong steamId)
    {
        string message = BuildExoFormDataMessage(playerCharacter, steamId);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientFamiliarBattleData(User user, ulong steamId)
    {
        string message = BuildFamiliarBattleDataMessage(steamId);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public void SendClientWeaponStatBonusData(Entity character, User user, ulong steamId)
    {
        string message = BuildWeaponStatBonusDataMessage(character, steamId);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(Core.EntityManager, user, messageWithMAC);
    }
    public string BuildConfigMessage()
    {
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatBaseCaps[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatBaseCaps[stat]).ToList();

        float prestigeStatMultiplier = PrestigeStatMultiplier;
        float statSynergyMultiplier = ClassStatMultiplier;

        int maxPlayerLevel = MaxLevel;
        int maxLegacyLevel = MaxLegacyLevel;
        int maxExpertiseLevel = MaxExpertiseLevel;
        int maxFamiliarLevel = MaxFamiliarLevel;
        int maxProfessionLevel = MAX_PROFESSION_LEVEL;
        bool extraRecipes = ExtraRecipes;
        int primalCost = PrimalCost;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},{5},{6},{7},{8},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel,
            maxExpertiseLevel, maxFamiliarLevel, maxProfessionLevel, extraRecipes, primalCost);

        sb.Append(string.Join(",", weaponStatValues.Select(val => val.ToString("F2", CultureInfo.InvariantCulture))))
            .Append(',');

        sb.Append(string.Join(",", bloodStatValues.Select(val => val.ToString("F2", CultureInfo.InvariantCulture))))
            .Append(',');

        foreach (var classEntry in Classes.ClassWeaponBloodEnumMap)
        {
            var playerClass = classEntry.Key;
            var (weaponSynergies, bloodSynergies) = classEntry.Value;

            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D2},", (int)playerClass + 1);
            sb.Append(string.Join("", weaponSynergies.Select(s => (s + 1).ToString("D2", CultureInfo.InvariantCulture))));
            sb.Append(',');

            sb.Append(string.Join("", bloodSynergies.Select(s => (s + 1).ToString("D2", CultureInfo.InvariantCulture))));
            sb.Append(',');
        }

        if (sb[^1] == ',')
            sb.Length--;

        return sb.ToString();
    }
    public string BuildProgressMessage(ProgressDataV1_3 data)
    {
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},", data.ExperienceData.Percent, data.ExperienceData.Level, data.ExperienceData.Prestige, data.ExperienceData.Class)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.LegacyData.Percent, data.LegacyData.Level, data.LegacyData.Prestige, data.LegacyData.Enum, data.LegacyData.LegacyBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", data.ExpertiseData.Percent, data.ExpertiseData.Level, data.ExpertiseData.Prestige, data.ExpertiseData.Enum, data.ExpertiseData.ExpertiseBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},{4},", data.FamiliarData.Percent, data.FamiliarData.Level, data.FamiliarData.Prestige, data.FamiliarData.Name, data.FamiliarData.FamiliarStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D2},{5:D2},{6:D2},{7:D2},{8:D2},{9:D2},{10:D2},{11:D2},{12:D2},{13:D2},{14:D2},{15:D2},",
                data.ProfessionData.EnchantingProgress, data.ProfessionData.EnchantingLevel, data.ProfessionData.AlchemyProgress, data.ProfessionData.AlchemyLevel,
                data.ProfessionData.HarvestingProgress, data.ProfessionData.HarvestingLevel, data.ProfessionData.BlacksmithingProgress, data.ProfessionData.BlacksmithingLevel,
                data.ProfessionData.TailoringProgress, data.ProfessionData.TailoringLevel, data.ProfessionData.WoodcuttingProgress, data.ProfessionData.WoodcuttingLevel,
                data.ProfessionData.MiningProgress, data.ProfessionData.MiningLevel, data.ProfessionData.FishingProgress, data.ProfessionData.FishingLevel)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", data.DailyQuestData.Type, data.DailyQuestData.Progress, data.DailyQuestData.Goal, data.DailyQuestData.Target, data.DailyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", data.WeeklyQuestData.Type, data.WeeklyQuestData.Progress, data.WeeklyQuestData.Goal, data.WeeklyQuestData.Target, data.WeeklyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2}", data.ShiftSpellData);

        return sb.ToString();
    }
    public string BuildClassDataMessage()
    {
        List<int> prestigeLevelsList = Configuration.ParseIntegersFromString(ConfigService.PrestigeLevelsToUnlockClassSpells);
        string prestigeLevels = prestigeLevelsList.Count > 0 ? string.Join(",", prestigeLevelsList) : "0";

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ClassDataToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5}",
                ConfigService.ClassSystem,
                ConfigService.ShiftSlot,
                ConfigService.DefaultClassSpell,
                ConfigService.ChangeClassItem,
                ConfigService.ChangeClassQuantity,
                prestigeLevels);

        foreach (var classEntry in Classes.ClassSpellsMap)
        {
            int classId = (int)classEntry.Key + 1;
            List<int> spells = Configuration.ParseIntegersFromString(classEntry.Value);

            sb.Append('|')
                .AppendFormat(CultureInfo.InvariantCulture, "{0:D2}", classId);

            foreach (int spell in spells)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ",{0}", spell);
            }
        }

        return sb.ToString();
    }
    public string BuildPrestigeLeaderboardMessage()
    {
        bool prestigeEnabled = ConfigService.PrestigeSystem;
        bool leaderboardEnabled = ConfigService.PrestigeLeaderboard;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:{1},{2}",
            (int)NetworkEventSubType.PrestigeLeaderboardToClient,
            prestigeEnabled,
            leaderboardEnabled);

        if (!prestigeEnabled || !leaderboardEnabled)
        {
            return sb.ToString();
        }

        foreach (PrestigeType prestigeType in Enum.GetValues(typeof(PrestigeType)))
        {
            if (prestigeType == PrestigeType.Exo && !ConfigService.ExoPrestiging)
            {
                continue;
            }

            var prestigeData = LevelingPrestigeManager.GetPrestigeForType(prestigeType)
                .Where(entry => entry.Value > 0)
                .OrderByDescending(entry => entry.Value)
                .Take(10)
                .ToList();

            sb.Append('|').Append(prestigeType);

            foreach (var entry in prestigeData)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, ",{0},{1}", entry.Key, entry.Value);
            }
        }

        return sb.ToString();
    }
    public string BuildExoFormDataMessage(Entity playerCharacter, ulong steamId)
    {
        bool exoEnabled = ConfigService.ExoPrestiging;
        int exoPrestiges = 0;
        float charge = 0f;
        float maxDuration = 0f;
        bool tauntEnabled = GetPlayerBool(steamId, SHAPESHIFT_KEY);
        string currentForm = string.Empty;

        if (exoEnabled && steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Exo, out int exoLevel))
        {
            exoPrestiges = exoLevel;
        }

        if (exoEnabled && exoPrestiges > 0)
        {
            Shapeshifts.UpdateExoFormChargeStored(steamId);
            if (steamId.TryGetPlayerExoFormData(out var exoFormData))
            {
                charge = exoFormData.Value;
            }

            maxDuration = Shapeshifts.CalculateFormDuration(exoPrestiges);

            if (steamId.TryGetPlayerShapeshift(out ShapeshiftType shapeshiftType))
            {
                currentForm = shapeshiftType.ToString();
            }
            else if (Shapeshifts.ShapeshiftCache.TryGetShapeshiftBuff(steamId, out PrefabGUID shapeshiftBuff))
            {
                foreach (var kvp in Shapeshifts.ShapeshiftBuffs)
                {
                    if (kvp.Value.Equals(shapeshiftBuff))
                    {
                        currentForm = kvp.Key.ToString();
                        break;
                    }
                }
            }
        }

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:{1},{2},{3:F2},{4:F2},{5},{6}",
            (int)NetworkEventSubType.ExoFormDataToClient,
            exoEnabled,
            exoPrestiges,
            charge,
            maxDuration,
            tauntEnabled,
            currentForm);

        if (!exoEnabled)
        {
            return sb.ToString();
        }

        Entity userEntity = playerCharacter.Read<PlayerCharacter>().UserEntity;
        bool hasDracula = Progression.ConsumedDracula(userEntity);
        bool hasMegara = Progression.ConsumedMegara(userEntity);

        foreach (IShapeshift form in Shapeshifts.ShapeshiftRegistry.All)
        {
            if (form is not Shapeshift shapeshift)
            {
                continue;
            }

            bool unlocked = exoPrestiges > 0
                && (!shapeshift.Type.Equals(ShapeshiftType.EvolvedVampire) || hasDracula)
                && (!shapeshift.Type.Equals(ShapeshiftType.CorruptedSerpent) || hasMegara);

            sb.Append('|').Append(shapeshift.Type).Append(',').Append(unlocked);

            foreach (var ability in shapeshift.Abilities.OrderBy(entry => entry.Key))
            {
                PrefabGUID abilityGuid = ability.Value;
                if (!shapeshift.TryGetCooldown(abilityGuid, out float cooldown))
                {
                    cooldown = 0f;
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, ",{0}:{1:F2}", abilityGuid.GuidHash, cooldown);
            }
        }

        return sb.ToString();
    }
    public string BuildFamiliarBattleDataMessage(ulong steamId)
    {
        bool familiarEnabled = ConfigService.FamiliarSystem;
        bool battlesEnabled = ConfigService.FamiliarBattles;
        string activeGroup = FamiliarBattleGroupsManager.GetActiveBattleGroupName(steamId);

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:{1},{2},{3}",
            (int)NetworkEventSubType.FamiliarBattleDataToClient,
            familiarEnabled,
            battlesEnabled,
            activeGroup);

        if (!familiarEnabled || !battlesEnabled)
        {
            return sb.ToString();
        }

        FamiliarBattleGroupsManager.FamiliarBattleGroupsData data = FamiliarBattleGroupsManager.LoadFamiliarBattleGroupsData(steamId);
        FamiliarPrestigeManager.FamiliarPrestigeData prestigeData = FamiliarPrestigeManager.LoadFamiliarPrestigeData(steamId);

        foreach (var group in data.BattleGroups)
        {
            sb.Append('|').Append(group.Name);

            foreach (int famId in group.Familiars)
            {
                if (famId == 0)
                {
                    sb.Append(",0:0:0:");
                    continue;
                }

                int level = FamiliarLevelingSystem.GetFamiliarExperience(steamId, famId).Key;
                int prestiges = prestigeData.FamiliarPrestige.TryGetValue(famId, out int familiarPrestige) ? familiarPrestige : 0;

                PrefabGUID famPrefab = new(famId);
                string famName = famPrefab.GetLocalizedName();
                if (string.IsNullOrEmpty(famName) || famName.Equals("LocalizationKey.Empty"))
                {
                    famName = famPrefab.GetPrefabName();
                }

                sb.AppendFormat(CultureInfo.InvariantCulture, ",{0}:{1}:{2}:{3}", famId, level, prestiges, famName);
            }
        }

        return sb.ToString();
    }

    public string BuildWeaponStatBonusDataMessage(Entity character, ulong steamId)
    {
        bool expertiseEnabled = ConfigService.ExpertiseSystem;
        if (!expertiseEnabled) return string.Empty;

        WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
        
        IWeaponExpertise expertiseHandler = WeaponExpertiseFactory.GetExpertise(weaponType);
        if (expertiseHandler == null) return string.Empty;

        float expertiseProgress = 0f;
        int expertiseLevel = 0;
        int maxStatChoices = ConfigService.ExpertiseStatChoices;

        expertiseProgress = WeaponSystem.GetLevelProgress(steamId, expertiseHandler) / 100f; // Client expects 0.0-1.0
        expertiseLevel = WeaponSystem.GetLevel(steamId, expertiseHandler);

        // Build base info
        // Format: [EventID]:[WeaponType],[ExpertiseLevel],[ExpertiseProgress],[MaxStatChoices]
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:{1},{2},{3:F2},{4}",
            (int)NetworkEventSubType.WeaponStatBonusDataToClient,
            weaponType.ToString(),
            expertiseLevel,
            expertiseProgress,
            maxStatChoices);

        // Get selected stats
        HashSet<WeaponStatType> selectedStats = new();
        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
        {
            selectedStats = new HashSet<WeaponStatType>(stats);
        }

        // Iterate all stats
        // Format: |[StatIndex],[IsSelected],[Value],[MaxValue]
        foreach (WeaponStatType stat in Enum.GetValues(typeof(WeaponStatType)))
        {
            
            bool isSelected = selectedStats.Contains(stat);
            float maxValue = WeaponStatBaseCaps[stat];
            
            // Calculate current value (for now assumption: if selected -> max value, else 0? 
            // Or server logic: usually it's static bonus if selected.
            // Based on WeaponCommands.cs FormatWeaponStatValue usage: it format the *value* passed to it.
            // TryGetScaledModifyUnitExpertiseStat calculates the value.
            // Let's rely on base cap for UI display "potential" and handle applied value logic.
            // For the UI "Value" field, let's send the potential value (Base Cap) so user knows what they get.
            // If we want "Current Value", it would be 0 if not selected. 
            // But the UI mock showed "+250" even if not selected (just greyed out).
            // So we send the Potential Value.
            
            // Calculate current value based on expertise level scaling
            float scaleFactor = MaxExpertiseLevel > 0 ? (float)expertiseLevel / MaxExpertiseLevel : 0f;
            float value = maxValue * scaleFactor; 
            
            sb.AppendFormat(CultureInfo.InvariantCulture, "|{0},{1},{2:F2},{3:F2}",
                (int)stat,
                isSelected ? 1 : 0,
                value,
                maxValue);
        }

        return sb.ToString();
    }
}
internal class ProgressDataV1_3
{
    public (int Percent, int Level, int Prestige, int Class) ExperienceData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) LegacyData { get; set; }
    public (int Percent, int Level, int Prestige, int Enum, int ExpertiseBonusStats) ExpertiseData { get; set; }
    public (int Percent, int Level, int Prestige, string Name, string FamiliarStats) FamiliarData { get; set; }

    public (int EnchantingProgress, int EnchantingLevel, int AlchemyProgress, int AlchemyLevel,
        int HarvestingProgress, int HarvestingLevel, int BlacksmithingProgress, int BlacksmithingLevel,
        int TailoringProgress, int TailoringLevel, int WoodcuttingProgress, int WoodcuttingLevel,
        int MiningProgress, int MiningLevel, int FishingProgress, int FishingLevel) ProfessionData
    { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) DailyQuestData { get; set; }
    public (int Type, int Progress, int Goal, string Target, string IsVBlood) WeeklyQuestData { get; set; }
    public int ShiftSpellData { get; set; }
}
