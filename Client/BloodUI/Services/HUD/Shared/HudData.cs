using ProjectM.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using static BloodUI.Services.DataService;

namespace BloodUI.Services.HUD.Shared;

/// <summary>
/// Central data storage for HUD components.
/// Contains enums, constants, sprites, and shared state.
/// </summary>
internal static class HudData
{
    #region Enums

    /// <summary>
    /// Types of UI elements that can be configured and toggled.
    /// </summary>
    public enum UIElement
    {
        Experience,
        Legacy,
        Expertise,
        Familiars,
        Professions,
        Daily,
        Weekly,
        ShiftSlot
    }

    /// <summary>
    /// Types of tabs in the Eclipse tabs UI.
    /// </summary>
    public enum TabType
    {
        Prestige,
        Exoform,
        Battles
    }

    #endregion

    #region Constants

    public const string V1_3 = "1.3";
    public const string ABILITY_ICON = "Stunlock_Icon_Ability_Spell_";
    public const string NPC_ABILITY = "Ashka_M1_64";
    public const string FISHING = "Go Fish!";

    public const float BAR_HEIGHT_SPACING = 0.075f;
    public const float BAR_WIDTH_SPACING = 0.065f;
    public const float MAX_PROFESSION_LEVEL = 100f;
    public const float EQUIPMENT_BONUS = 0.1f;
    public const float COOLDOWN_FACTOR = 8f;

    public static readonly Color BrightGold = new(1f, 0.8f, 0f, 1f);

    public static readonly Vector2 TabsNavAnchor = new(0f, 0f);
    public static readonly Vector2 TabsNavPivot = new(0f, 0f);
    public static readonly Vector2 TabsNavPosition = new(480f, 560f);
    public static readonly Vector2 TabsContentAnchor = new(0f, 0f);
    public static readonly Vector2 TabsContentPivot = new(0f, 0f);
    public static readonly Vector2 TabsContentPosition = new(480f, 320f);

    #endregion

    #region Static Dictionaries

    public static readonly Dictionary<int, string> RomanNumerals = new()
    {
        { 100, "C" }, { 90, "XC" }, { 50, "L" }, { 40, "XL" },
        { 10, "X" }, { 9, "IX" }, { 5, "V" }, { 4, "IV" },
        { 1, "I" }
    };

    public static readonly List<string> SpriteNames =
    [
        "Attribute_TierIndicator_Fixed",
        "BloodTypeFrame",
        "BloodTypeIcon_Tiny_Warrior",
        "BloodIcon_Cursed",
        "BloodIcon_Small_Cursed",
        "BloodIcon_Small_Holy",
        "BloodIcon_Warrior",
        "BloodIcon_Small_Warrior",
        // Blood type icons (used by Stat Bonuses "Blood Legacies" header)
        "BloodType_Worker_Big",
        "BloodType_Worker_Small",
        "BloodType_Warrior_Big",
        "BloodType_Warrior_Small",
        "BloodType_Scholar_Big",
        "BloodType_Scholar_Small",
        "BloodType_Rogue_Big",
        "BloodType_Rogue_Small",
        "BloodType_Mutant_Big",
        "BloodType_Mutant_Small",
        "BloodType_Draculin_Big",
        "BloodType_Draculin_Small",
        "BloodType_Creature_Big",
        "BloodType_Creature_Small",
        "BloodType_Brute_Big",
        "BloodType_Brute_Small",
        "BloodType_Corruption_Big",
        "BloodType_Corruption_Small",
        "Poneti_Icon_Hammer_30",
        "Poneti_Icon_Bag",
        "Poneti_Icon_Res_93",
        "Stunlock_Icon_Item_Jewel_Collection4",
        "Stunlock_Icon_Bag_Background_Alchemy",
        "Poneti_Icon_Alchemy_02_mortar",
        "Stunlock_Icon_Bag_Background_Jewel",
        "Poneti_Icon_runic_tablet_12",
        "Stunlock_Icon_Bag_Background_Woodworking",
        "Stunlock_Icon_Bag_Background_Herbs",
        "Poneti_Icon_Herbalism_35_fellherb",
        "Stunlock_Icon_Bag_Background_Fish",
        "Poneti_Icon_Cooking_28_fish",
        "Poneti_Icon_Cooking_60_oceanfish",
        "Stunlock_Icon_Bag_Background_Armor",
        "Poneti_Icon_Tailoring_38_fiercloth",
        "FantasyIcon_ResourceAndCraftAddon (56)",
        "Stunlock_Icon_Bag_Background_Weapon",
        "Poneti_Icon_Sword_v2_48",
        "Poneti_Icon_Hammer_30",
        "Poneti_Icon_Spear_v2_01",
        "Poneti_Icon_Crossbow_v2_01",
        "Poneti_Icon_Bow_v2_01",
        "Poneti_Icon_Greatsword_v2_01",
        "Poneti_Icon_Dagger_v2_01",
        "Poneti_Icon_Pistol_v2_01",
        "Poneti_Icon_Scythe_v2_01",
        "Poneti_Icon_Whip_v2_01",
        "Stunlock_Icon_BoneSword01",
        "Stunlock_Icon_BoneAxe01",
        "Stunlock_Icon_BoneMace01",
        "Stunlock_Icon_Bag_Background_Consumable",
        "Poneti_Icon_Quest_131",
        "FantasyIcon_Wood_Hallow",
        "Poneti_Icon_Engineering_59_mega_fishingrod",
        "Poneti_Icon_Axe_v2_04",
        "Poneti_Icon_Blacksmith_21_big_grindstone",
        "FantasyIcon_Flowers (11)",
        "FantasyIcon_MagicItem (105)",
        "Item_MagicSource_General_T05_Relic",
        "Stunlock_Icon_BloodRose",
        "Poneti_Icon_Blacksmith_24_bigrune_grindstone",
        "Item_MagicSource_General_T04_FrozenEye",
        "Stunlock_Icon_SpellPoint_Blood1",
        "Stunlock_Icon_SpellPoint_Unholy1",
        "Stunlock_Icon_SpellPoint_Frost1",
        "Stunlock_Icon_SpellPoint_Chaos1",
        "Stunlock_Icon_SpellPoint_Frost1",
        "Stunlock_Icon_SpellPoint_Storm1",
        "Stunlock_Icon_SpellPoint_Illusion1",
        "spell_level_icon",
        "strength_level_icon",
        "Stunlock_Icon_BannerDoubleSmall",
        "Stunlock_Icon_NewStar",
        "Act_BG",
        "ActionSlotDivider",
        "ActionWheel_InnerCircle_Gradient",
        "Arrow",
        "Box_InventoryExtraBagBG",
        "ContainerSlot_Default",
        "Divider_Horizontal",
        "FoldoutButton_Arrow",
        "IconBackground",
        "Icon_DepositItems",
        "Icon_DropItems",
        "Icon_SortItems",
        "Icon_TakeItems",
        "MobLevel_Skull",
        "Portrait_Small_Smoke_AlphaWolf",
        "Portrait_Small_Smoke_Dracula_Grey",
        "Portrait_Small_Smoke_Unknown",
        "SimpleBox_Normal",
        "SimpleProgressBar_Empty_Default",
        "SimpleProgressBar_Fill",
        "SimpleProgressBar_Mask",
        "ChargeRefill",
        "SlotFrame_Smaller",
        "Slot_Normal",
        "Stunlock_Icons_spellbook_blood",
        "spell_icon",
        "TabGradient",
        "Window_Box",
        "Window_Box_Background",
        "Window_Divider_Horizontal_V_Red",
        "Glow03",
        "Glow04"
    ];

    public static readonly Dictionary<Profession, string> ProfessionIcons = new()
    {
        { Profession.Enchanting, "Item_MagicSource_General_T04_FrozenEye" },
        { Profession.Alchemy, "FantasyIcon_MagicItem (105)" },
        { Profession.Harvesting, "Stunlock_Icon_BloodRose" },
        { Profession.Blacksmithing, "Poneti_Icon_Blacksmith_24_bigrune_grindstone" },
        { Profession.Tailoring, "FantasyIcon_ResourceAndCraftAddon (56)" },
        { Profession.Woodcutting, "Poneti_Icon_Axe_v2_04" },
        { Profession.Mining, "Poneti_Icon_Hammer_30" },
        { Profession.Fishing, "Poneti_Icon_Engineering_59_mega_fishingrod" }
    };

    public static readonly Dictionary<string, Sprite> Sprites = [];

    public static readonly Dictionary<PlayerClass, Color> ClassColorHexMap = new()
    {
        { PlayerClass.ShadowBlade, new Color(0.6f, 0.1f, 0.9f) },
        { PlayerClass.DemonHunter, new Color(1f, 0.8f, 0f) },
        { PlayerClass.BloodKnight, new Color(1f, 0f, 0f) },
        { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f) },
        { PlayerClass.VampireLord, new Color(0f, 1f, 1f) },
        { PlayerClass.DeathMage, new Color(0f, 1f, 0f) }
    };

    public static readonly List<TabType> TabOrder =
    [
        TabType.Prestige,
        TabType.Exoform,
        TabType.Battles
    ];

    public static readonly Dictionary<TabType, string> TabLabels = new()
    {
        { TabType.Prestige, "Prestige" },
        { TabType.Exoform, "Exoform" },
        { TabType.Battles, "Familiar Battles" }
    };

    public static readonly Dictionary<UIElement, string> AbilitySlotNamePaths = new()
    {
        { UIElement.Experience, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Primary/" },
        { UIElement.Legacy, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill1/" },
        { UIElement.Expertise, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_WeaponSkill2/" },
        { UIElement.Familiars, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Travel/" },
        { UIElement.Professions, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell1/" },
        { UIElement.Weekly, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Spell2/" },
        { UIElement.Daily, "HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/Background/AbilityBar/AbilityBarEntry_Ultimate/" },
    };

    public static readonly Dictionary<UIElement, GameObject> GameObjects = [];
    public static readonly Dictionary<GameObject, bool> ObjectStates = [];
    public static readonly List<GameObject> ProfessionObjects = [];

    #endregion

    #region Regex Patterns

    public static readonly Regex ClassNameRegex = new("(?<!^)([A-Z])");
    public static readonly Regex AbilitySpellRegex = new("(?<=AB_).*(?=_Group)");

    #endregion

    #region Sprites

    public static Sprite QuestKillStandardUnit;
    public static Sprite QuestKillVBloodUnit;

    #endregion

    #region Canvas References

    public static UICanvasBase CanvasBase;

    #endregion

    #region Global State

    public static string Version = string.Empty;

    public static bool IsReady = false;
    public static bool IsActive = false;
    public static bool ShiftActive = false;
    public static bool KillSwitch = false;

    public static TabType ActiveTab = TabType.Prestige;

    #endregion

    #region Configuration State

    /// <summary>
    /// Gets the current configuration state for each UI element type.
    /// </summary>
    public static Dictionary<UIElement, bool> GetUiElementsConfigured()
    {
        return new Dictionary<UIElement, bool>
        {
            { UIElement.Experience, HudConfiguration.ExperienceBarEnabled },
            { UIElement.Legacy, HudConfiguration.LegacyBarEnabled },
            { UIElement.Expertise, HudConfiguration.ExpertiseBarEnabled },
            { UIElement.Familiars, HudConfiguration.FamiliarBarEnabled },
            { UIElement.Professions, HudConfiguration.ProfessionBarsEnabled },
            { UIElement.Daily, HudConfiguration.QuestTrackerEnabled },
            { UIElement.Weekly, HudConfiguration.QuestTrackerEnabled },
            { UIElement.ShiftSlot, HudConfiguration.ShiftSlotEnabled }
        };
    }

    #endregion
}
