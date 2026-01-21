using BloodUI.Patches;
using BloodUI.Resources;
using BloodUI.Utilities;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MonoMod.Utils;
using ProjectM;
using ProjectM.UI;
using Stunlock.Core;
using StunShared.UI;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static BloodUI.Patches.InitializationPatches;
using static BloodUI.Services.CanvasService.ConfigureHUD;
using static BloodUI.Services.CanvasService.DataHUD;
using static BloodUI.Services.CanvasService.InitializeHUD;
using static BloodUI.Services.CanvasService.InputHUD;
using static BloodUI.Services.CanvasService.UpdateHUD;
using static BloodUI.Services.CanvasService.UtilitiesHUD;
using static BloodUI.Services.DataService;
using static BloodUI.Utilities.GameObjects;
using BloodUI.Services.HUD;
using BloodUI.Services.HUD.Configurators;
using BloodUI.Services.HUD.Shared;
using Image = UnityEngine.UI.Image;
using InputAdaptiveElement = BloodUI.Services.HUD.InputAdaptiveManager.InputAdaptiveElement;
using BloodUI.Services;

namespace BloodUI.Services;
internal class CanvasService
{
    static EntityManager EntityManager
        => Core.EntityManager;
    static SystemService SystemService
        => Core.SystemService;
    static ManagedDataRegistry ManagedDataRegistry
        => SystemService.ManagedDataSystem.ManagedDataRegistry;
    static UIDataSystem UIDataSystem
        => SystemService.UIDataSystem;
    static Entity LocalCharacter
        => Core.LocalCharacter;
    static BufferLookup<ModifyUnitStatBuff_DOTS> ModifyUnitStatBuffLookup
        => ClientChatSystemPatch.ModifyUnitStatBuffLookup;
    static bool Eclipsed { get; } = Plugin.Eclipsed;
    static bool AttributeBuffs => Plugin.AttributeBuffsEnabled;
    public static WaitForSeconds WaitForSeconds { get; } = Eclipsed
        ? new WaitForSeconds(0.1f)
        : new WaitForSeconds(1f);

    public static Coroutine _canvasRoutine;
    public static Coroutine _shiftRoutine;
    public CanvasService(UICanvasBase canvas)
    {
        _canvasBase = canvas;

        _bottomBarCanvas = canvas.BottomBarParent.gameObject.GetComponent<Canvas>();
        _targetInfoPanelCanvas = canvas.TargetInfoPanelParent.gameObject.GetComponent<Canvas>();

        _layer = _bottomBarCanvas.gameObject.layer;
        _barNumber = 0;
        _graphBarNumber = 0;
        _windowOffset = 0f;

        // Initialize the modular HUD system
        HudIntegration.GetOrCreateOrchestrator(canvas);

        FindSprites();
        InitializeBloodButton();

        try
        {
            InitializeUI();
            InitializeAbilitySlotButtons();
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogError($"Failed to initialize UI elements: {ex}");
        }
    }

    public void RebuildLayout()
    {
        _ready = false;
        LayoutService.Reset();
        ResetState();

        // Reset and reinitialize the modular HUD system
        HudIntegration.Reset();
        HudIntegration.GetOrCreateOrchestrator(_canvasBase);

        _barNumber = 0;
        _graphBarNumber = 0;
        _windowOffset = 0f;

        FindSprites();

        try
        {
            InitializeUI();
            InitializeAbilitySlotButtons();
        }
        catch (Exception ex)
        {
            DebugToolsBridge.TryLogError($"Failed to rebuild UI elements: {ex}");
        }

        _ready = true;
    }
    public static IEnumerator CanvasUpdateLoop()
    {
        while (true)
        {
            if (_killSwitch)
            {
                yield break;
            }
            else if (!_ready || !_active)
            {
                yield return WaitForSeconds;
                continue;
            }

            if (_experienceBar)
            {
                try
                {
                    UpdateBar(_experienceProgress, _experienceLevel, _experienceMaxLevel, _experiencePrestige, _experienceText, _experienceHeader, _experienceFill, UIElement.Experience);
                    UpdateClass(_classType, _experienceClassText);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating experience bar: {e}");
                }
            }

            var buffer = AttributeBuffs ? TryGetSourceBuffer() : default;

            if (_legacyBar)
            {
                try
                {
                    UpdateBar(_legacyProgress, _legacyLevel, _legacyMaxLevel, _legacyPrestige, _legacyText, _legacyHeader, _legacyFill, UIElement.Legacy, _legacyType);
                    UpdateBloodStats(_legacyBonusStats, [_firstLegacyStat, _secondLegacyStat, _thirdLegacyStat], ref buffer, GetBloodStatInfo);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating legacy bar: {e}");
                }
            }

            if (_expertiseBar)
            {
                try
                {
                    UpdateBar(_expertiseProgress, _expertiseLevel, _expertiseMaxLevel, _expertisePrestige, _expertiseText, _expertiseHeader, _expertiseFill, UIElement.Expertise, _expertiseType);
                    UpdateWeaponStats(_expertiseBonusStats, [_firstExpertiseStat, _secondExpertiseStat, _thirdExpertiseStat], ref buffer, GetWeaponStatInfo);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating expertise bar: {e}");
                }
            }

            if (StatBuffActive && AttributeBuffs)
            {
                try
                {
                    UpdateTargetBuffer(ref buffer);
                    UpdateAttributes(ref buffer);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating stats buff: {e}");
                }
            }

            if (_familiarBar)
            {
                try
                {
                    UpdateBar(_familiarProgress, _familiarLevel, _familiarMaxLevel, _familiarPrestige, _familiarText, _familiarHeader, _familiarFill, UIElement.Familiars, _familiarName);
                    UpdateFamiliarStats(_familiarStats, [_familiarMaxHealth, _familiarPhysicalPower, _familiarSpellPower]);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating familiar bar: {e}");
                }
            }
            if (_questTracker)
            {
                try
                {
                    UpdateQuests(_dailyQuestObject, _dailyQuestSubHeader, _dailyQuestIcon, _dailyTargetType, _dailyTarget, _dailyProgress, _dailyGoal, _dailyVBlood);
                    UpdateQuests(_weeklyQuestObject, _weeklyQuestSubHeader, _weeklyQuestIcon, _weeklyTargetType, _weeklyTarget, _weeklyProgress, _weeklyGoal, _weeklyVBlood);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating quest tracker: {e}");
                }
            }
            if (_professionBars)
            {
                try
                {
                    UpdateProfessions(_alchemyProgress, _alchemyLevel, _alchemyLevelText, _alchemyProgressFill, _alchemyFill, Profession.Alchemy);
                    UpdateProfessions(_blacksmithingProgress, _blacksmithingLevel, _blacksmithingLevelText, _blacksmithingProgressFill, _blacksmithingFill, Profession.Blacksmithing);
                    UpdateProfessions(_enchantingProgress, _enchantingLevel, _enchantingLevelText, _enchantingProgressFill, _enchantingFill, Profession.Enchanting);
                    UpdateProfessions(_tailoringProgress, _tailoringLevel, _tailoringLevelText, _tailoringProgressFill, _tailoringFill, Profession.Tailoring);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating professions(1): {e}");
                }

                try
                {
                    UpdateProfessions(_fishingProgress, _fishingLevel, _fishingLevelText, _fishingProgressFill, _fishingFill, Profession.Fishing);
                    UpdateProfessions(_harvestingProgress, _harvestingLevel, _harvestingLevelText, _harvestingProgressFill, _harvestingFill, Profession.Harvesting);
                    UpdateProfessions(_miningProgress, _miningLevel, _miningLevelText, _miningProgressFill, _miningFill, Profession.Mining);
                    UpdateProfessions(_woodcuttingProgress, _woodcuttingLevel, _woodcuttingLevelText, _woodcuttingProgressFill, _woodcuttingFill, Profession.Woodcutting);
                }
                catch (Exception e)
                {
                    DebugToolsBridge.TryLogError($"Error updating professions(2): {e}");
                }
            }


            try
            {
                CharacterMenuService.Update();
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Error updating Bloodcraft tab: {e}");       
            }
            // if (_killSwitch) yield break;

            try
            {
                if (!_shiftActive && LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
                {
                    Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();

                    if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState)
                        && abilityGroupState.SlotIndex == 3 // shift "slot" index
                        && _shiftRoutine == null) // if ability found on slot 3, activate shift loop
                    {
                        _shiftRoutine = ShiftUpdateLoop().Start();
                        _shiftActive = true;
                    }
                }
            }
            catch (Exception e)
            {
                DebugToolsBridge.TryLogError($"Error updating ability bar: {e}");
            }

            SyncInputHUD();
            yield return WaitForSeconds;
        }
    }
    public static IEnumerator ShiftUpdateLoop()
    {
        while (true)
        {
            if (_killSwitch)
            {
                yield break;
            }
            else if (!_ready)
            {
                yield return WaitForSeconds;
                continue;
            }
            else if (!_shiftActive)
            {
                yield return WaitForSeconds;
                continue;
            }

            if (LocalCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared))
            {
                Entity abilityGroupEntity = abilityBar_Shared.CastGroup.GetEntityOnServer();
                Entity abilityCastEntity = abilityBar_Shared.CastAbility.GetEntityOnServer();

                if (abilityGroupEntity.TryGetComponent(out AbilityGroupState abilityGroupState) && abilityGroupState.SlotIndex == 3)
                {
                    PrefabGUID currentPrefabGUID = abilityGroupEntity.GetPrefabGUID();

                    if (UpdateTooltipData(abilityGroupEntity, currentPrefabGUID))
                    {
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                    else if (_abilityTooltipData != null)
                    {
                        UpdateAbilityData(_abilityTooltipData, abilityGroupEntity, abilityCastEntity, currentPrefabGUID);
                    }
                }

                if (_abilityTooltipData != null)
                {
                    UpdateAbilityState(abilityGroupEntity, abilityCastEntity);
                }
            }

            yield return WaitForSeconds;
        }
    }
    public static void ResetState()
    {
        // Reset the modular HUD system
        HudIntegration.Reset();

        foreach (GameObject gameObject in ObjectStates.Keys)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in ProfessionObjects)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in DataHUD.GameObjects.Values)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (GameObject gameObject in AttributeObjects)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        foreach (InputAdaptiveElement adaptiveElement in AdaptiveElements)
        {
            if (adaptiveElement.AdaptiveObject != null)
            {
                UnityEngine.Object.Destroy(adaptiveElement.AdaptiveObject);
            }
        }

        ObjectStates.Clear();
        ProfessionObjects.Clear();
        DataHUD.GameObjects.Clear();
        AttributeObjects.Clear();

        _attributeObjectPrefab = null;

        BloodAttributeTexts.Clear();
        WeaponAttributeTexts.Clear();
        AdaptiveElements.Clear();

        Sprites.Clear();


    }
    public static class InitializeHUD
    {
        /*
        static HashSet<UnitStatType> UnitStatAttributes { get; } =
        [
            // Weapon Stats
            UnitStatType.MaxHealth,
            UnitStatType.PhysicalPower,
            UnitStatType.SpellPower,
            UnitStatType.MovementSpeed,
            UnitStatType.PrimaryAttackSpeed,
            UnitStatType.PhysicalCriticalStrikeChance,
            UnitStatType.PhysicalCriticalStrikeDamage,
            UnitStatType.SpellCriticalStrikeChance,
            UnitStatType.SpellCriticalStrikeDamage,
            UnitStatType.PrimaryLifeLeech,
            UnitStatType.PhysicalLifeLeech,
            UnitStatType.SpellLifeLeech,
            // Blood Stats
            UnitStatType.MinionDamage,
            UnitStatType.DamageReduction,
            UnitStatType.HealingReceived,
            UnitStatType.ReducedBloodDrain,
            UnitStatType.ResourceYield,
            UnitStatType.WeaponCooldownRecoveryRate,
            UnitStatType.SpellCooldownRecoveryRate,
            UnitStatType.UltimateCooldownRecoveryRate
        ];

        static readonly HashSet<UnitStatType> RegisteredAttributes = [];
        */

        public static void InitializeUI()
        {
            if (_experienceBar)
            {
                ConfigureHorizontalProgressBar(ref _experienceBarGameObject, ref _experienceInformationPanel,
                ref _experienceFill, ref _experienceText, ref _experienceHeader, UIElement.Experience, Color.green,
                ref _experienceFirstText, ref _experienceClassText, ref _experienceSecondText);
            }

            if (_legacyBar)
            {
                ConfigureHorizontalProgressBar(ref _legacyBarGameObject, ref _legacyInformationPanel,
                ref _legacyFill, ref _legacyText, ref _legacyHeader, UIElement.Legacy, Color.red,
                ref _firstLegacyStat, ref _secondLegacyStat, ref _thirdLegacyStat);
            }

            if (_expertiseBar)
            {
                ConfigureHorizontalProgressBar(ref _expertiseBarGameObject, ref _expertiseInformationPanel,
                ref _expertiseFill, ref _expertiseText, ref _expertiseHeader, UIElement.Expertise, Color.grey,
                ref _firstExpertiseStat, ref _secondExpertiseStat, ref _thirdExpertiseStat);
            }

            if (_familiarBar)
            {
                ConfigureHorizontalProgressBar(ref _familiarBarGameObject, ref _familiarInformationPanel,
                ref _familiarFill, ref _familiarText, ref _familiarHeader, UIElement.Familiars, Color.yellow,
                ref _familiarMaxHealth, ref _familiarPhysicalPower, ref _familiarSpellPower);
            }

            if (_questTracker)
            {
                ConfigureQuestWindow(ref _dailyQuestObject, UIElement.Daily, Color.green, ref _dailyQuestHeader, ref _dailyQuestSubHeader, ref _dailyQuestIcon);
                ConfigureQuestWindow(ref _weeklyQuestObject, UIElement.Weekly, Color.magenta, ref _weeklyQuestHeader, ref _weeklyQuestSubHeader, ref _weeklyQuestIcon);
            }

            if (_professionBars)
            {
                ConfigureVerticalProgressBar(ref _alchemyBarGameObject, ref _alchemyProgressFill, ref _alchemyFill, ref _alchemyLevelText, Profession.Alchemy);
                ConfigureVerticalProgressBar(ref _blacksmithingBarGameObject, ref _blacksmithingProgressFill, ref _blacksmithingFill, ref _blacksmithingLevelText, Profession.Blacksmithing);
                ConfigureVerticalProgressBar(ref _enchantingBarGameObject, ref _enchantingProgressFill, ref _enchantingFill, ref _enchantingLevelText, Profession.Enchanting);
                ConfigureVerticalProgressBar(ref _tailoringBarGameObject, ref _tailoringProgressFill, ref _tailoringFill, ref _tailoringLevelText, Profession.Tailoring);
                ConfigureVerticalProgressBar(ref _fishingBarGameObject, ref _fishingProgressFill, ref _fishingFill, ref _fishingLevelText, Profession.Fishing);
                ConfigureVerticalProgressBar(ref _harvestingGameObject, ref _harvestingProgressFill, ref _harvestingFill, ref _harvestingLevelText, Profession.Harvesting);
                ConfigureVerticalProgressBar(ref _miningBarGameObject, ref _miningProgressFill, ref _miningFill, ref _miningLevelText, Profession.Mining);
                ConfigureVerticalProgressBar(ref _woodcuttingBarGameObject, ref _woodcuttingProgressFill, ref _woodcuttingFill, ref _woodcuttingLevelText, Profession.Woodcutting);
            }



            if (_shiftSlot)
            {
                ConfigureShiftSlot(ref _abilityDummyObject, ref _abilityBarEntry, ref _uiState, ref _cooldownParentObject, ref _cooldownText,
                    ref _chargesTextObject, ref _cooldownFillImage, ref _chargesText, ref _chargeCooldownFillImage, ref _chargeCooldownImageObject,
                    ref _abilityEmptyIcon, ref _abilityIcon, ref _keybindObject);
            }

            _ready = true;
        }
        public static void InitializeAbilitySlotButtons()
        {
            foreach (var keyValuePair in UiElementsConfigured)
            {
                if (keyValuePair.Value && AbilitySlotNamePaths.ContainsKey(keyValuePair.Key))
                {
                    GameObject abilitySlotObject = GameObject.Find(AbilitySlotNamePaths[keyValuePair.Key]);
                    if (abilitySlotObject == null)
                    {
                        continue;
                    }

                    if (abilitySlotObject.GetComponent<SimpleStunButton>() != null)
                    {
                        continue;
                    }

                    SimpleStunButton stunButton = abilitySlotObject.AddComponent<SimpleStunButton>();

                    if (keyValuePair.Key.Equals(UIElement.Professions))
                    {
                        GameObject[] capturedObjects = [.. ProfessionObjects];
                        stunButton.onClick.AddListener((UnityAction)(() => ToggleHUD.ToggleGameObjects(capturedObjects)));
                    }
                    else if (DataHUD.GameObjects.TryGetValue(keyValuePair.Key, out GameObject gameObject))
                    {
                        GameObject[] capturedObjects = [gameObject];
                        stunButton.onClick.AddListener((UnityAction)(() => ToggleHUD.ToggleGameObjects(capturedObjects)));
                    }
                }
            }
        }
        public static void InitializeBloodButton()
        {
            GameObject bloodObject = GameObject.Find("HUDCanvas(Clone)/BottomBarCanvas/BottomBar(Clone)/Content/BloodOrbParent/BloodOrb/BlackBackground/Blood");

            if (bloodObject != null)
            {
                SimpleStunButton stunButton = bloodObject.AddComponent<SimpleStunButton>();
                stunButton.onClick.AddListener(new Action(ToggleHUD.ToggleAllObjects));
            }
        }
        public static bool InitializeAttributeValues(InventorySubMenu inventorySubMenu)
        {
            if (inventorySubMenu == null)
            {
                // inventorySubMenu.PreInstantiatedAttributeEntries
                // inventorySubMenu.AttributeSections
                // inventorySubMenu.AttributeSettings
                DebugToolsBridge.TryLogError("InventorySubMenu is null!");
                return false;
            }

            /*
            try
            {
                // var attributePrefab = inventorySubMenu.AttributeEntryPrefab;
                // var attributeKeys = inventorySubMenu.AttributeKeys;
                var preInstantiated = inventorySubMenu.PreInstantiatedAttributeEntries;
                var attributeSettings = inventorySubMenu.AttributeSettings;
                // var parentConsole = inventorySubMenu.AttributesParentConsole;
                // var selectionGroup = inventorySubMenu.AttributesSelectionGroup;

                foreach (var attributeEntry in preInstantiated)
                {
                    DebugToolsBridge.TryLogWarning($"{attributeEntry.CurrentData.AttributeUIType}, {attributeEntry.CurrentData.TextKey.ToString}, {attributeEntry.CurrentData.TooltipDesc}");
                }

                foreach (var attributeSetting in attributeSettings.Settings)
                {
                    DebugToolsBridge.TryLogWarning($"{attributeSetting.Header.ToString}, {attributeSetting.Tooltip.ToString}, {attributeSetting.UnitStatType}");
                }

                // UIDataSystem._BottomBar_Keyboard, UIDataSystem._BottomBar_Gamepad

                UIDataSystem.UI.BottomBar.InventorySlotsFillImage = _alchemyFill;
                // UIDataSystem.UI.BottomBar.InventorySlotsInverted
                UIDataSystem.UI.BottomBar.InventorySlotsText.ForceSet("Test");
            }
            catch (Exception ex)
            {
                DebugToolsBridge.TryLogError($"Failed to dump attributes: {ex}");
            }
            */

            Transform attributeSectionsParent = inventorySubMenu.AttributesParentConsole.transform.parent.parent.GetChild(4).GetChild(0).GetChild(2).GetChild(0);
            var attributeSections = attributeSectionsParent?
                .GetComponentsInChildren<CharacterAttributeSection>(false).Take(1)
                .Concat(attributeSectionsParent.GetComponentsInChildren<CharacterAttributeSection>(false).Skip(2));

            // DebugToolsBridge.TryLogWarning($"Found {attributeSections.Count()} attribute sections!");

            foreach (CharacterAttributeSection section in attributeSections)
            {
                GameObject attributesContainer = section.transform.FindChild("AttributesContainer").gameObject;
                var attributeEntries = attributesContainer.transform.GetComponentsInChildren<CharacterAttributeEntry>(false);
                int index = 0;

                // DebugToolsBridge.TryLogWarning($"Found {attributeEntries.Length} attribute entries!");
                if (!attributeEntries.Any())
                    return false;

                try
                {
                    foreach (CharacterAttributeEntry characterAttributeEntry in attributeEntries)
                    {
                        string name = characterAttributeEntry.gameObject.name;

                        if (!name.EndsWith("(Clone)"))
                        {
                            continue;
                        }

                        GameObject attributeObject = characterAttributeEntry.transform.GetChild(0).gameObject;
                        SimpleStunButton simpleStunButton = attributeObject.GetComponent<SimpleStunButton>();

                        if (simpleStunButton == null)
                            continue;

                        GameObject gameObject = attributeObject.gameObject.transform.GetChild(0).gameObject;
                        GameObject attributeValue = gameObject.transform.GetChild(1).gameObject;

                        // if (_attributeObjectPrefab ??= null)
                            // _attributeObjectPrefab = attributeValue;
                        _attributeObjectPrefab ??= attributeValue;

                        UnitStatType unitStatType = section.Attributes[index++].Type;
                        GameObject attributeValueClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);
                        // GameObject attributeTypeClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);
                        // GameObject attibuteSynergyClone = UIHelper.InstantiateGameObjectUnderAnchor(_attributeObjectPrefab, gameObject.transform);

                        ConfigureAttributeObjects(simpleStunButton, attributeObject, gameObject,
                            attributeValue, attributeValueClone,
                            // attributeTypeClone, attibuteSynergyClone,
                            unitStatType);
                    }
                }
                catch (Exception ex)
                {
                    DebugToolsBridge.TryLogError($"Failed to initialize attribute values for section {section.name}: {ex}");
                }
            }

            return true;
        }
    }
    public static class DataHUD
    {
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

        public static readonly Dictionary<int, string> RomanNumerals = new()
        {
            {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
            {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"},
            {1, "I"}
        };

        public static readonly List<string> SpriteNames =
        [
            "Attribute_TierIndicator_Fixed", // class stat synergy?
            "BloodTypeFrame",                // bl
            "BloodTypeIcon_Tiny_Warrior",    // wep
            // sprites for attribute page ^
            "BloodIcon_Cursed",
            "BloodIcon_Small_Cursed",
            "BloodIcon_Small_Holy",
            "BloodIcon_Warrior",
            "BloodIcon_Small_Warrior",
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
            "spell_level_icon"
        ];

        public const string ABILITY_ICON = "Stunlock_Icon_Ability_Spell_";
        public const string NPC_ABILITY = "Ashka_M1_64";

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



        public static readonly Regex ClassNameRegex = new("(?<!^)([A-Z])");
        public static readonly Regex AbilitySpellRegex = new("(?<=AB_).*(?=_Group)");

        public static readonly Dictionary<PlayerClass, Color> ClassColorHexMap = new()
        {
            { PlayerClass.ShadowBlade, new Color(0.6f, 0.1f, 0.9f) },  // ignite purple
            { PlayerClass.DemonHunter, new Color(1f, 0.8f, 0f) },      // static yellow
            { PlayerClass.BloodKnight, new Color(1f, 0f, 0f) },        // leech red
            { PlayerClass.ArcaneSorcerer, new Color(0f, 0.5f, 0.5f) }, // weaken teal
            { PlayerClass.VampireLord, new Color(0f, 1f, 1f) },        // chill cyan
            { PlayerClass.DeathMage, new Color(0f, 1f, 0f) }           // condemn green
        };

        public const string V1_3 = "1.3";

        public static UICanvasBase _canvasBase;
        public static Canvas _bottomBarCanvas;
        public static Canvas _targetInfoPanelCanvas;
        public static string _version = string.Empty;
        // Eclipse sync status (server ↔ client). Shown in Equipment tab summary.
        public static string _eclipseSyncStatus = string.Empty;
        public static bool _eclipseSyncDisabled = false;

        public static GameObject _experienceBarGameObject;
        public static GameObject _experienceInformationPanel;
        public static LocalizedText _experienceHeader;
        public static LocalizedText _experienceText;
        public static LocalizedText _experienceFirstText;
        public static LocalizedText _experienceClassText;
        public static LocalizedText _experienceSecondText;
        public static Image _experienceFill;
        public static float _experienceProgress = 0f;
        public static int _experienceLevel = 0;
        public static int _experiencePrestige = 0;
        public static int _experienceMaxLevel = 90;
        public static PlayerClass _classType = PlayerClass.None;

        public static GameObject _legacyBarGameObject;
        public static GameObject _legacyInformationPanel;
        public static LocalizedText _firstLegacyStat;
        public static LocalizedText _secondLegacyStat;
        public static LocalizedText _thirdLegacyStat;
        public static LocalizedText _legacyHeader;
        public static LocalizedText _legacyText;
        public static Image _legacyFill;
        public static string _legacyType;
        public static float _legacyProgress = 0f;
        public static int _legacyLevel = 0;
        public static int _legacyPrestige = 0;
        public static int _legacyMaxLevel = 100;
        public static List<string> _legacyBonusStats = ["", "", ""];

        public static GameObject _expertiseBarGameObject;
        public static GameObject _expertiseInformationPanel;
        public static LocalizedText _firstExpertiseStat;
        public static LocalizedText _secondExpertiseStat;
        public static LocalizedText _thirdExpertiseStat;
        public static LocalizedText _expertiseHeader;
        public static LocalizedText _expertiseText;
        public static Image _expertiseFill;
        public static string _expertiseType;
        public static float _expertiseProgress = 0f;
        public static int _expertiseLevel = 0;
        public static int _expertisePrestige = 0;
        public static int _expertiseMaxLevel = 100;
        public static List<string> _expertiseBonusStats = ["", "", ""];

        public static GameObject _familiarBarGameObject;
        public static GameObject _familiarInformationPanel;
        public static LocalizedText _familiarMaxHealth;
        public static LocalizedText _familiarPhysicalPower;
        public static LocalizedText _familiarSpellPower;
        public static LocalizedText _familiarHeader;
        public static LocalizedText _familiarText;
        public static Image _familiarFill;
        public static float _familiarProgress = 0f;
        public static int _familiarLevel = 1;
        public static int _familiarPrestige = 0;
        public static int _familiarMaxLevel = 90;
        public static string _familiarName = "";
        public static List<string> _familiarStats = ["", "", ""];

        public static bool _equipmentBonus = false;
        public const float MAX_PROFESSION_LEVEL = 100f;
        public const float EQUIPMENT_BONUS = 0.1f;

        public static GameObject _enchantingBarGameObject;
        public static LocalizedText _enchantingLevelText;
        public static Image _enchantingProgressFill;
        public static Image _enchantingFill;
        public static float _enchantingProgress = 0f;
        public static int _enchantingLevel = 0;

        public static GameObject _alchemyBarGameObject;
        public static LocalizedText _alchemyLevelText;
        public static Image _alchemyProgressFill;
        public static Image _alchemyFill;
        public static float _alchemyProgress = 0f;
        public static int _alchemyLevel = 0;

        public static GameObject _harvestingGameObject;
        public static LocalizedText _harvestingLevelText;
        public static Image _harvestingProgressFill;
        public static Image _harvestingFill;
        public static float _harvestingProgress = 0f;
        public static int _harvestingLevel = 0;

        public static GameObject _blacksmithingBarGameObject;
        public static LocalizedText _blacksmithingLevelText;
        public static Image _blacksmithingProgressFill;
        public static Image _blacksmithingFill;
        public static float _blacksmithingProgress = 0f;
        public static int _blacksmithingLevel = 0;

        public static GameObject _tailoringBarGameObject;
        public static LocalizedText _tailoringLevelText;
        public static Image _tailoringProgressFill;
        public static Image _tailoringFill;
        public static float _tailoringProgress = 0f;
        public static int _tailoringLevel = 0;

        public static GameObject _woodcuttingBarGameObject;
        public static LocalizedText _woodcuttingLevelText;
        public static Image _woodcuttingProgressFill;
        public static Image _woodcuttingFill;
        public static float _woodcuttingProgress = 0f;
        public static int _woodcuttingLevel = 0;

        public static GameObject _miningBarGameObject;
        public static LocalizedText _miningLevelText;
        public static Image _miningProgressFill;
        public static Image _miningFill;
        public static float _miningProgress = 0f;
        public static int _miningLevel = 0;

        public static GameObject _fishingBarGameObject;
        public static LocalizedText _fishingLevelText;
        public static Image _fishingProgressFill;
        public static Image _fishingFill;
        public static float _fishingProgress = 0f;
        public static int _fishingLevel = 0;

        public static GameObject _dailyQuestObject;
        public static LocalizedText _dailyQuestHeader;
        public static LocalizedText _dailyQuestSubHeader;
        public static Image _dailyQuestIcon;
        public static TargetType _dailyTargetType = TargetType.Kill;
        public static int _dailyProgress = 0;
        public static int _dailyGoal = 0;
        public static string _dailyTarget = "";
        public static bool _dailyVBlood = false;

        public static GameObject _weeklyQuestObject;
        public static LocalizedText _weeklyQuestHeader;
        public static LocalizedText _weeklyQuestSubHeader;
        public static Image _weeklyQuestIcon;
        public static TargetType _weeklyTargetType = TargetType.Kill;
        public static int _weeklyProgress = 0;
        public static int _weeklyGoal = 0;
        public static string _weeklyTarget = "";
        public static bool _weeklyVBlood = false;


        public static PrefabGUID _abilityGroupPrefabGUID;

        public static AbilityTooltipData _abilityTooltipData;
        public static readonly ComponentType AbilityTooltipDataComponent = ComponentType.ReadOnly(Il2CppType.Of<AbilityTooltipData>());

        public static GameObject _abilityDummyObject;
        public static AbilityBarEntry _abilityBarEntry;
        public static AbilityBarEntry.UIState _uiState;

        public static GameObject _cooldownParentObject;
        public static TextMeshProUGUI _cooldownText;
        public static GameObject _chargeCooldownImageObject;
        public static GameObject _chargesTextObject;
        public static TextMeshProUGUI _chargesText;
        public static Image _cooldownFillImage;
        public static Image _chargeCooldownFillImage;

        public static GameObject _abilityEmptyIcon;
        public static GameObject _abilityIcon;

        public static GameObject _keybindObject;

        public static int _shiftSpellIndex = -1;
        public const float COOLDOWN_FACTOR = 8f;

        public static double _cooldownEndTime = 0;
        public static float _cooldownRemaining = 0f;
        public static float _cooldownTime = 0f;
        public static int _currentCharges = 0;
        public static int _maxCharges = 0;
        public static double _chargeUpEndTime = 0;
        public static float _chargeUpTime = 0f;
        public static float _chargeUpTimeRemaining = 0f;
        public static float _chargeCooldownTime = 0f;

        public static int _layer;
        public static int _barNumber;
        public static int _graphBarNumber;
        public static float _horizontalBarHeaderFontSize;
        public static float _windowOffset;
        public static readonly Color BrightGold = new(1f, 0.8f, 0f, 1f);

        public static readonly Vector2 TabsNavAnchor = new(0f, 0f);
        public static readonly Vector2 TabsNavPivot = new(0f, 0f);
        public static readonly Vector2 TabsNavPosition = new(480f, 560f);
        public static readonly Vector2 TabsContentAnchor = new(0f, 0f);
        public static readonly Vector2 TabsContentPivot = new(0f, 0f);
        public static readonly Vector2 TabsContentPosition = new(480f, 320f);

        public const float BAR_HEIGHT_SPACING = 0.075f;
        public const float BAR_WIDTH_SPACING = 0.065f;

        public static readonly Dictionary<UIElement, GameObject> GameObjects = [];
        public static readonly Dictionary<GameObject, bool> ObjectStates = [];
        public static readonly List<GameObject> ProfessionObjects = [];

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

        public static readonly Dictionary<UIElement, bool> UiElementsConfigured = new()
        {
            { UIElement.Experience, _experienceBar },
            { UIElement.Legacy, _legacyBar },
            { UIElement.Expertise, _expertiseBar },
            { UIElement.Familiars, _familiarBar },
            { UIElement.Professions, _professionBars },
            { UIElement.Daily, _questTracker },
            { UIElement.Weekly, _questTracker },
            { UIElement.ShiftSlot, _shiftSlot }
        };

        public const string FISHING = "Go Fish!";

        public static bool _ready = false;
        public static bool _active = false;
        public static bool _shiftActive = false;
        public static bool _killSwitch = false;
    }
    /// <summary>
    /// Delegates to HudUpdateManager. Kept for backward compatibility.
    /// </summary>
    public static class UpdateHUD
    {
        public static PrefabGUID StatBuff => HudUpdateManager.StatBuff;
        public static bool StatBuffActive => HudUpdateManager.StatBuffActive;

        public static HashSet<GameObject> AttributeObjects => HudUpdateManager.AttributeObjects;
        public static GameObject _attributeObjectPrefab
        {
            get => HudUpdateManager._attributeObjectPrefab;
            set => HudUpdateManager._attributeObjectPrefab = value;
        }

        public static HashSet<LocalizedText> CombinedAttributeTexts => HudUpdateManager.CombinedAttributeTexts;
        public static Dictionary<UnitStatType, LocalizedText> BloodAttributeTexts => HudUpdateManager.BloodAttributeTexts;
        public static Dictionary<UnitStatType, LocalizedText> WeaponAttributeTexts => HudUpdateManager.WeaponAttributeTexts;
        public static List<ModifyUnitStatBuff_DOTS> BloodStatBuffs => HudUpdateManager.BloodStatBuffs;
        public static List<ModifyUnitStatBuff_DOTS> WeaponStatBuffs => HudUpdateManager.WeaponStatBuffs;

        public static void UpdateAttributeType(UnitStatType unitStatType, Sprite sprite)
            => HudUpdateManager.UpdateAttributeType(unitStatType, sprite);
        public static DynamicBuffer<ModifyUnitStatBuff_DOTS> TryGetSourceBuffer()
            => HudUpdateManager.TryGetSourceBuffer();
        public static void UpdateTargetBuffer(ref DynamicBuffer<ModifyUnitStatBuff_DOTS> sourceBuffer)
            => HudUpdateManager.UpdateTargetBuffer(ref sourceBuffer);
        public static void UpdateAttributes(ref DynamicBuffer<ModifyUnitStatBuff_DOTS> sourceBuffer)
            => HudUpdateManager.UpdateAttributes(ref sourceBuffer);
        public static string GetWeaponStatInfo(int i, string statType)
            => HudUpdateManager.GetWeaponStatInfo(i, statType);
        public static string GetBloodStatInfo(int i, string statType)
            => HudUpdateManager.GetBloodStatInfo(i, statType);
        public static void UpdateAbilityData(AbilityTooltipData abilityTooltipData, Entity abilityGroupEntity,
            Entity abilityCastEntity, PrefabGUID abilityGroupPrefabGUID)
            => HudUpdateManager.UpdateAbilityData(abilityTooltipData, abilityGroupEntity, abilityCastEntity, abilityGroupPrefabGUID);
        public static void UpdateAbilityState(Entity abilityGroupEntity, Entity abilityCastEntity)
            => HudUpdateManager.UpdateAbilityState(abilityGroupEntity, abilityCastEntity);
        public static bool UpdateTooltipData(Entity abilityGroupEntity, PrefabGUID abilityGroupPrefabGUID)
            => HudUpdateManager.UpdateTooltipData(abilityGroupEntity, abilityGroupPrefabGUID);
        public static void UpdateProfessions(float progress, int level, LocalizedText levelText,
            Image progressFill, Image fill, Profession profession)
            => HudUpdateManager.UpdateProfessions(progress, level, levelText, progressFill, fill, profession);
        public static void UpdateBar(float progress, int level, int maxLevel,
            int prestiges, LocalizedText levelText, LocalizedText barHeader,
            Image fill, UIElement element, string type = "")
            => HudUpdateManager.UpdateBar(progress, level, maxLevel, prestiges, levelText, barHeader, fill, element, type);
        public static void UpdateClass(PlayerClass classType, LocalizedText classText)
            => HudUpdateManager.UpdateClass(classType, classText);
        public static void UpdateBloodStats(List<string> bonusStats, List<LocalizedText> statTexts,
            ref DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer, Func<int, string, string> getStatInfo)
            => HudUpdateManager.UpdateBloodStats(bonusStats, statTexts, ref buffer, getStatInfo);
        public static void UpdateWeaponStats(List<string> bonusStats, List<LocalizedText> statTexts,
            ref DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer, Func<int, string, string> getStatInfo)
            => HudUpdateManager.UpdateWeaponStats(bonusStats, statTexts, ref buffer, getStatInfo);
        public static void UpdateFamiliarStats(List<string> familiarStats, List<LocalizedText> statTexts)
            => HudUpdateManager.UpdateFamiliarStats(familiarStats, statTexts);
        public static void UpdateQuests(GameObject questObject, LocalizedText questSubHeader, Image questIcon,
            TargetType targetType, string target, int progress, int goal, bool isVBlood)
            => HudUpdateManager.UpdateQuests(questObject, questSubHeader, questIcon, targetType, target, progress, goal, isVBlood);
    }
    /// <summary>
    /// Delegates to HudToggleManager. Kept for backward compatibility.
    /// </summary>
    public static class ToggleHUD
    {
        public static Dictionary<int, Action> ActionToggles => HudToggleManager.ActionToggles;
        public static void ToggleAllObjects() => HudToggleManager.ToggleAllObjects();
        public static void ToggleGameObjects(params GameObject[] gameObjects) => HudToggleManager.ToggleGameObjects(gameObjects);
    }
    /// <summary>
    /// Delegates to InputAdaptiveManager. Kept for backward compatibility.
    /// </summary>
    public static class InputHUD
    {
        public static bool IsGamepad => InputAdaptiveManager.IsGamepad;
        public static List<InputAdaptiveManager.InputAdaptiveElement> AdaptiveElements => InputAdaptiveManager.AdaptiveElements;
        public static void SyncInputHUD() => InputAdaptiveManager.SyncInputHUD();
        public static void RegisterAdaptiveElement(GameObject adaptiveObject, Vector2 keyboardMousePos, Vector2 keyboardMouseAnchorMin,
            Vector2 keyboardMouseAnchorMax, Vector2 keyboardMousePivot, Vector3 keyboardMouseScale, Vector2 controllerPos,
            Vector2 controllerAnchorMin, Vector2 controllerAnchorMax, Vector2 controllerPivot, Vector3 controllerScale)
            => InputAdaptiveManager.RegisterAdaptiveElement(adaptiveObject, keyboardMousePos, keyboardMouseAnchorMin,
                keyboardMouseAnchorMax, keyboardMousePivot, keyboardMouseScale, controllerPos,
                controllerAnchorMin, controllerAnchorMax, controllerPivot, controllerScale);
        public static void SyncAdaptiveElements(bool isGamepad) => InputAdaptiveManager.SyncAdaptiveElements(isGamepad);
    }
    public static class ConfigureHUD
    {
        public static readonly bool _experienceBar = Plugin.Leveling;
        public static readonly bool _showPrestige = Plugin.Prestige;
        public static readonly bool _legacyBar = Plugin.Legacies;
        public static readonly bool _expertiseBar = Plugin.Expertise;
        public static readonly bool _familiarBar = Plugin.Familiars;
        public static readonly bool _professionBars = Plugin.Professions;
        public static readonly bool _questTracker = Plugin.Quests;
        public static readonly bool _shiftSlot = Plugin.ShiftSlot;
        /// <summary>
        /// Configures the shift slot ability bar entry. Delegates to ShiftSlotConfigurator.
        /// </summary>
        public static void ConfigureShiftSlot(ref GameObject shiftSlotObject, ref AbilityBarEntry shiftSlotEntry, ref AbilityBarEntry.UIState uiState, ref GameObject cooldownObject,
            ref TextMeshProUGUI cooldownText, ref GameObject chargeCooldownTextObject, ref Image cooldownFill,
            ref TextMeshProUGUI chargeCooldownText, ref Image chargeCooldownFillImage, ref GameObject chargeCooldownFillObject,
            ref GameObject abilityEmptyIcon, ref GameObject abilityIcon, ref GameObject keybindObject)
            => ShiftSlotConfigurator.Configure(ref shiftSlotObject, ref shiftSlotEntry, ref uiState, ref cooldownObject,
                ref cooldownText, ref chargeCooldownTextObject, ref cooldownFill,
                ref chargeCooldownText, ref chargeCooldownFillImage, ref chargeCooldownFillObject,
                ref abilityEmptyIcon, ref abilityIcon, ref keybindObject);
        /// <summary>
        /// Configures a quest window. Delegates to QuestWindowConfigurator.
        /// </summary>
        public static void ConfigureQuestWindow(ref GameObject questObject, UIElement questType, Color headerColor,
            ref LocalizedText header, ref LocalizedText subHeader, ref Image questIcon)
            => QuestWindowConfigurator.Configure(_canvasBase, _bottomBarCanvas, _layer, ref _windowOffset,
                ref questObject, (HudData.UIElement)questType, headerColor, ref header, ref subHeader, ref questIcon);
        /// <summary>
        /// Configures a class window. Delegates to ClassWindowConfigurator.
        /// </summary>
        public static void ConfigureClassWindow(ref GameObject classObject, string layoutKey, string title, Color headerColor,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition,
            ref LocalizedText header, ref LocalizedText subHeader, ref Transform entriesRoot, ref GameObject entryTemplate)
            => ClassWindowConfigurator.Configure(_canvasBase, _bottomBarCanvas, _layer,
                ref classObject, layoutKey, title, headerColor, anchor, pivot, anchoredPosition,
                ref header, ref subHeader, ref entriesRoot, ref entryTemplate);

        /// <summary>
        /// Ensures a LocalizedText has a bound text component. Delegates to ClassWindowConfigurator.
        /// </summary>
        internal static bool TryBindLocalizedText(LocalizedText localizedText, string context)
            => ClassWindowConfigurator.TryBindLocalizedText(localizedText, context);

        /// <summary>
        /// Applies a transparent graphic for raycasting. Delegates to ClassWindowConfigurator.
        /// </summary>
        internal static void ApplyTransparentGraphic(GameObject target, string context)
            => ClassWindowConfigurator.ApplyTransparentGraphic(target, context);
        /// <summary>
        /// Configures a horizontal progress bar. Delegates to ProgressBarConfigurator.
        /// </summary>
        public static void ConfigureHorizontalProgressBar(ref GameObject barGameObject, ref GameObject informationPanelObject, ref Image fill,
            ref LocalizedText level, ref LocalizedText header, UIElement element, Color fillColor,
            ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
            => ProgressBarConfigurator.ConfigureHorizontal(_canvasBase, _targetInfoPanelCanvas, _layer,
                ref _barNumber, ref _horizontalBarHeaderFontSize,
                ref barGameObject, ref informationPanelObject, ref fill, ref level, ref header,
                (HudData.UIElement)element, fillColor, ref firstText, ref secondText, ref thirdText);

        /// <summary>
        /// Configures a vertical progress bar for professions. Delegates to ProgressBarConfigurator.
        /// </summary>
        public static void ConfigureVerticalProgressBar(ref GameObject barGameObject, ref Image progressFill, ref Image maxFill,
            ref LocalizedText level, Profession profession)
            => ProgressBarConfigurator.ConfigureVertical(_canvasBase, _targetInfoPanelCanvas, _layer,
                ref _graphBarNumber, ref barGameObject, ref progressFill, ref maxFill, ref level, profession);

        /// <summary>
        /// Configures an information panel. Delegates to ProgressBarConfigurator.
        /// </summary>
        public static void ConfigureInformationPanel(ref GameObject informationPanelObject, ref LocalizedText firstText, ref LocalizedText secondText,
            ref LocalizedText thirdText, UIElement element)
            => ProgressBarConfigurator.ConfigureInformationPanel(ref informationPanelObject, ref firstText, ref secondText, ref thirdText, (HudData.UIElement)element);

        /// <summary>
        /// Configures the experience panel. Delegates to ProgressBarConfigurator.
        /// </summary>
        public static void ConfigureExperiencePanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
            => ProgressBarConfigurator.ConfigureExperiencePanel(ref panel, ref firstText, ref secondText, ref thirdText);

        /// <summary>
        /// Configures the default panel. Delegates to ProgressBarConfigurator.
        /// </summary>
        public static void ConfigureDefaultPanel(ref GameObject panel, ref LocalizedText firstText, ref LocalizedText secondText, ref LocalizedText thirdText)
            => ProgressBarConfigurator.ConfigureDefaultPanel(ref panel, ref firstText, ref secondText, ref thirdText);

        public static void ConfigureAttributeType(GameObject attributeSpriteClone, Sprite sprite)
        {
            TextMeshProUGUI textMeshPro = attributeSpriteClone.GetComponent<TextMeshProUGUI>();
            textMeshPro.spriteAsset = Utilities.GameObjects.CreateSpriteAsset(sprite);
            textMeshPro.m_spriteColor = Color.white;

            LayoutElement layoutElement = attributeSpriteClone.GetComponent<LayoutElement>();
            LocalizedText localizedText = attributeSpriteClone.GetComponent<LocalizedText>();
            
            layoutElement.flexibleWidth = 1f;
            attributeSpriteClone.transform.SetSiblingIndex(1);

            textMeshPro.autoSizeTextContainer = true;
            textMeshPro.enableWordWrapping = false;

            localizedText.ForceSet(string.Empty);
            attributeSpriteClone.SetActive(true);
        }
        public static void ConfigureAttributeSynergy(GameObject attributeSynergyClone, Sprite sprite)
        {
            TextMeshProUGUI textMeshPro = attributeSynergyClone.GetComponent<TextMeshProUGUI>();
            textMeshPro.spriteAsset = Utilities.GameObjects.CreateSpriteAsset(sprite);
            textMeshPro.m_spriteColor = Color.white;

            LayoutElement layoutElement = attributeSynergyClone.GetComponent<LayoutElement>();
            LocalizedText localizedText = attributeSynergyClone.GetComponent<LocalizedText>();

            layoutElement.flexibleWidth = 1f;
            localizedText.ForceSet(string.Empty);

            attributeSynergyClone.transform.SetSiblingIndex(1);
            textMeshPro.autoSizeTextContainer = true;
            textMeshPro.enableWordWrapping = false;

            attributeSynergyClone.SetActive(true);
        }
        public static void ConfigureAttributeButton(SimpleStunButton button, string command)
            => button.onClick.AddListener((UnityAction)(() => Quips.SendCommand(command)));
        public static void ConfigureAttributeObjects(SimpleStunButton simpleStunButton, GameObject attributeEntryObject,
            GameObject gameObject, GameObject attributeValue, GameObject attributeValueClone,
            // GameObject attributeTypeClone, GameObject attibuteSynergyClone,
            UnitStatType unitStatType)
        {
            HorizontalLayoutGroup horizontalLayoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>();
            TextMeshProUGUI textMeshPro = attributeValue.GetComponent<TextMeshProUGUI>();
            Image image = attributeEntryObject.GetComponent<Image>();

            LayoutElement layoutElement = attributeValueClone.GetComponent<LayoutElement>();
            LocalizedText localizedText = attributeValueClone.GetComponent<LocalizedText>();

            bool isValidStat = false;

            if (Enum.TryParse(unitStatType.ToString(), true, out BloodStatType bloodStatType)
                && _bloodStatValues.ContainsKey(bloodStatType))
            {
                ConfigureAttributeButton(simpleStunButton, $".bl cst {(int)bloodStatType}");
                image.color = Color.red;

                // ConfigureAttributeType(attributeTypeClone, _sprites["BloodTypeFrame"]);
                // ConfigureAttributeSynergy(attibuteSynergyClone, _sprites["Attribute_TierIndicator_Fixed"]);

                BloodAttributeTexts[unitStatType] = localizedText;
                // CombinedAttributeTexts[unitStatType] = localizedText;

                AttributeObjects.Add(attributeValueClone);
                // _attributeObjects.Add(attributeTypeClone);

                DebugToolsBridge.TryLogWarning($"Registered Blood Attribute: {unitStatType}");
                isValidStat = true;
            }

            if (Enum.TryParse(unitStatType.ToString(), true, out WeaponStatType weaponStatType)
                && _weaponStatValues.ContainsKey(weaponStatType))
            {
                ConfigureAttributeButton(simpleStunButton, $".wep cst {(int)weaponStatType}");
                image.color = Color.grey;

                // ConfigureAttributeType(attributeTypeClone, _sprites["BloodTypeIcon_Tiny_Warrior"]);
                // ConfigureAttributeSynergy(attibuteSynergyClone, _sprites["Attribute_TierIndicator_Fixed"]);

                WeaponAttributeTexts[unitStatType] = localizedText;
                // CombinedAttributeTexts[unitStatType] = localizedText;

                AttributeObjects.Add(attributeValueClone);
                // _attributeObjects.Add(attributeTypeClone);

                DebugToolsBridge.TryLogWarning($"Registered Weapon Attribute: {unitStatType}");
                isValidStat = true;
            }

            if (!isValidStat)
            {
                AttributeObjects.Add(attributeValueClone);
            }

            horizontalLayoutGroup.childForceExpandWidth = false;
            layoutElement.flexibleWidth = 1f;
            attributeValueClone.transform.SetSiblingIndex(1);
            textMeshPro.autoSizeTextContainer = true;
            textMeshPro.enableWordWrapping = false;

            localizedText.ForceSet(string.Empty);
            attributeValueClone.SetActive(true);
        }
    }
    /// <summary>
    /// Delegates to HudUtilities. Kept for backward compatibility.
    /// </summary>
    public static class UtilitiesHUD
    {
        public static float ClassSynergy<T>(T statType, PlayerClass classType, Dictionary<PlayerClass, (List<WeaponStatType> WeaponStatTypes, List<BloodStatType> BloodStatTypes)> classStatSynergy)
            => HudUtilities.ClassSynergy(statType, classType, classStatSynergy);
        public static string FormatWeaponStatBar(WeaponStatType weaponStat, float statValue)
            => HudUtilities.FormatWeaponStatBar(weaponStat, statValue);
        public static string FormatAttributeValue(UnitStatType unitStatType, float statValue)
            => HudUtilities.FormatAttributeValue(unitStatType, statValue);
        public static string FormatWeaponAttribute(WeaponStatType weaponStat, float statValue)
            => HudUtilities.FormatWeaponAttribute(weaponStat, statValue);
        public static string IntegerToRoman(int num)
            => HudUtilities.ToRoman(num);
        public static string FormatClassName(PlayerClass classType)
            => HudUtilities.FormatClassName(classType);
        public static string TrimToFirstWord(string input)
            => HudUtilities.TrimToFirstWord(input);
        public static string SplitPascalCase(string input)
            => HudUtilities.SplitPascalCase(input);
        public static void FindSprites()
            => HudUtilities.FindSprites();
    }
}
