using Eclipse.Services.CharacterMenu.Shared;
using Eclipse.Utilities;
using ProjectM.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static Eclipse.Services.CanvasService.DataHUD;
using static Eclipse.Services.DataService;
using Eclipse.Services;

namespace Eclipse.Services.CharacterMenu.Tabs;

/// <summary>
/// Partial class for FamiliarsTab containing Talents panel functionality.
/// Implements a Path of Exile-style talent tree for familiars.
/// </summary>
internal partial class FamiliarsTab
{
    #region Talent Constants

    private const float TalentNodeSize = 40f;
    private const float TalentKeystoneSize = 52f;
    private const float TalentNodeSpacing = 8f;
    private const float TalentConnectionWidth = 3f;
    private const float TalentTreePadding = 12f;

    // Brighter, more saturated colors for better visibility
    private static readonly Color TalentNodeAllocatedColor = new(0.2f, 1f, 0.3f, 1f);  // Bright green
    private static readonly Color TalentNodeAvailableColor = new(1f, 0.85f, 0.2f, 1f); // Bright gold
    private static readonly Color TalentNodeLockedColor = new(0.5f, 0.5f, 0.55f, 0.9f); // Gray
    private static readonly Color TalentNodeKeystoneColor = new(1f, 0.3f, 0.3f, 1f);   // Bright red
    private static readonly Color TalentConnectionAllocatedColor = new(0.3f, 1f, 0.4f, 0.9f);
    private static readonly Color TalentConnectionLockedColor = new(0.4f, 0.4f, 0.45f, 0.6f);
    
    // Glow colors for allocated talents
    private static readonly Color TalentAllocatedGlowColor = new(0.4f, 1f, 0.5f, 0.6f);
    private static readonly Color TalentKeystoneAllocatedGlowColor = new(1f, 0.5f, 0.3f, 0.7f);

    // Background frames for nodes
    private static readonly string[] TalentNodeFrameSprites = ["AbilitySlot02_64x64_Normal", "Stunlock_Icon_AbilityBackground", "IconBackground"];
    private static readonly string[] TalentKeystoneFrameSprites = ["AbilityFrame02_Ulti_Normal", "Stunlock_Icon_UltimateBackground", "IconBackground"];

    #endregion

    #region Talent UI Classes

    private class TalentNodeUI
    {
        public int TalentId { get; set; }
        public RectTransform Root { get; set; }
        public Image Frame { get; set; }      // Background frame
        public Image Icon { get; set; }       // Actual talent icon
        public Image Glow { get; set; }       // Glow effect for allocated
        public TextMeshProUGUI Label { get; set; }
        public SimpleStunButton Button { get; set; }
        public bool IsAllocated { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsKeystone { get; set; }
    }

    private class TalentDef
    {
        public string Name;
        public string Description;
        public string Effect;
        public string Icon;
    }

    private TextMeshProUGUI _infoTitleText;
    private TextMeshProUGUI _infoDescText;
    private TextMeshProUGUI _infoEffectText;

    private readonly HashSet<int> _cachedAllocatedTalents = new();
    private bool _talentsSyncedFromServer = false;
    private float _lastTalentRequestTime = -1000f;
    private const float TalentRequestCooldown = 2f; // Only request every 2 seconds

    private readonly Dictionary<int, TalentDef> _talentDefs = new()
    {
        { 1, new() { Name = "Speed I", Description = "+5% Movement Speed", Icon = "AbilityFrame02_Travel_Normal" } },
        { 2, new() { Name = "Speed II", Description = "+10% Movement Speed", Icon = "AbilityFrame02_Travel_Normal" } },
        { 3, new() { Name = "Swift Strike", Description = "+25% Attack Speed", Icon = "Stunlock_Icon_BoneSword01" } },
        { 4, new() { Name = "Berserker", Description = "+50% Attack Speed, +30% Move Speed", Effect = "Effect: Dark Red Aura, -15% Defense", Icon = "Stunlock_Icon_NewStar" } },
        
        { 10, new() { Name = "Power I", Description = "+5% Physical & Spell Power", Icon = "Stunlock_Icon_BoneAxe01" } },
        { 11, new() { Name = "Power II", Description = "+10% Physical & Spell Power", Icon = "Stunlock_Icon_BoneAxe01" } },
        { 12, new() { Name = "Brutal Force", Description = "+25% All Damage", Icon = "Stunlock_Icon_BoneMace01" } },
        { 13, new() { Name = "Enrage", Description = "+100% Damage below 50% HP", Effect = "Effect: Enrage Aura, -25% Max Health", Icon = "MobLevel_Skull" } },

        { 20, new() { Name = "Vitality I", Description = "+5% Max Health", Icon = "Stunlock_Icon_BloodRose" } },
        { 21, new() { Name = "Vitality II", Description = "+10% Max Health", Icon = "Stunlock_Icon_BloodRose" } },
        { 22, new() { Name = "Fortitude", Description = "+25% Damage Reduction", Icon = "Stunlock_Icon_Chest_01_Boneguard" } },
        { 23, new() { Name = "Juggernaut", Description = "+100% Health, Immune to Stuns", Effect = "Effect: Iron Skin, -30% Speed", Icon = "Stunlock_Icon_Item_TaintedHeart02" } }
    };

    #endregion

    #region Talent Panel Creation

    private void CreateFamiliarTalentsPanel(Transform parent, TextMeshProUGUI reference)
    {
        if (parent == null) return;

        RectTransform card = CreateFamiliarCard(parent, "FamiliarTalentsCard", stretchHeight: false);
        if (card == null) return;

        // Ensure the card has a VerticalLayoutGroup to stack Header, Tree, Info, and Footer
        VerticalLayoutGroup cardLayout = card.GetComponent<VerticalLayoutGroup>();
        if (cardLayout == null) cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlHeight = true;
        cardLayout.childControlWidth = true;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childForceExpandWidth = true;
        cardLayout.spacing = 8f;
        cardLayout.padding = CreatePadding(16, 16, 16, 16);

        _ = CreateFamiliarSectionLabel(card, reference, "Familiar Talent Tree", FamiliarHeaderDefaultIconSpriteNames);

        // Talent points header
        _talentPointsText = CreateFamiliarText(card, reference, "Talent Points: 0 / 0",
            FamiliarMetaFontScale, FontStyles.Normal, TextAlignmentOptions.Left, FamiliarMetaColor);

        // Create the talent tree container with horizontal layout for 3 paths
        RectTransform treeContainer = CreateRectTransformObject("TalentTreeContainer", card);
        if (treeContainer != null)
        {
            // Background for the tree area
            Image bg = treeContainer.gameObject.AddComponent<Image>();
            ApplySprite(bg, FamiliarCardSpriteNames);
            bg.color = new Color(0f, 0f, 0f, 0.3f);
            bg.raycastTarget = false;

            // Horizontal layout for the 3 paths (Speed, Power, Vitality)
            HorizontalLayoutGroup hLayout = treeContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.UpperCenter;
            hLayout.spacing = 20f; 
            hLayout.padding = CreatePadding(20, 20, 20, 20); 
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // Height for the tree container - reduced to make room for info panel
            LayoutElement treeLayout = treeContainer.gameObject.AddComponent<LayoutElement>();
            treeLayout.minHeight = 320f;     
            treeLayout.preferredHeight = 320f;
            treeLayout.flexibleHeight = 0f;

            _talentTreeRoot = treeContainer;

            // Create the 3 paths
            CreateTalentPath(treeContainer, reference, "Speed", new[] {
                (1, "Speed I", false),
                (2, "Speed II", false),
                (3, "Swift Strike", false),
                (4, "Berserker", true)
            });

            CreateTalentPath(treeContainer, reference, "Power", new[] {
                (10, "Power I", false),
                (11, "Power II", false),
                (12, "Brutal Force", false),
                (13, "Enrage", true)
            });

            CreateTalentPath(treeContainer, reference, "Vitality", new[] {
                (20, "Vitality I", false),
                (21, "Vitality II", false),
                (22, "Fortitude", false),
                (23, "Juggernaut", true)
            });
        }

        CreateSpacer(card, 5f);

        // --- Talent Info Panel (between tree and Reset button) ---
        // --- Talent Info Panel (Card style) --
        RectTransform infoPanel = CreateFamiliarCard(card, "TalentInfoPanel", stretchHeight: false, enforceMinimumHeight: false);
        
        // Adjust the card's layout settings
        if (infoPanel != null)
        {
            LayoutElement infoLayout = infoPanel.GetComponent<LayoutElement>();
            infoLayout.minHeight = 70f;
            infoLayout.preferredHeight = 80f;
            
            // Allow clicking through the card background
            Image infoBg = infoPanel.GetComponent<Image>();
            if (infoBg != null) infoBg.raycastTarget = false;
        }

        _infoTitleText = CreateFamiliarText(infoPanel, reference, "Hover a Talent", FamiliarMetaFontScale, FontStyles.Bold, TextAlignmentOptions.Left, Color.white);
        _infoDescText = CreateFamiliarText(infoPanel, reference, "See details here.", FamiliarMetaFontScale * 0.9f, FontStyles.Normal, TextAlignmentOptions.Left, new Color(0.8f, 0.8f, 0.8f));
        _infoEffectText = CreateFamiliarText(infoPanel, reference, "", FamiliarMetaFontScale * 0.8f, FontStyles.Italic, TextAlignmentOptions.Left, new Color(0.6f, 0.6f, 1f));

        CreateSpacer(card, 5f);

        // Users might want the Reset button at the bottom
        RectTransform resetRow = CreateRectTransformObject("ResetTalentsRow", card);
        if (resetRow != null)
        {
            LayoutElement resetLayout = resetRow.gameObject.AddComponent<LayoutElement>();
            resetLayout.minHeight = 36f;
            resetLayout.preferredHeight = 36f;
            resetLayout.flexibleWidth = 1f;

            Image resetBg = resetRow.gameObject.AddComponent<Image>();
            ApplySprite(resetBg, FamiliarRowSpriteNames);
            resetBg.color = new Color(0.6f, 0.1f, 0.1f, 0.8f);
            resetBg.raycastTarget = true;

            HorizontalLayoutGroup resetHLayout = resetRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            resetHLayout.childAlignment = TextAnchor.MiddleCenter;
            resetHLayout.padding = CreatePadding(10, 10, 4, 4);
            resetHLayout.childControlWidth = true;
            resetHLayout.childControlHeight = true;

            CreateFamiliarText(resetRow, reference, "Reset Talents",
                FamiliarActionFontScale, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            SimpleStunButton resetButton = resetRow.gameObject.AddComponent<SimpleStunButton>();
            ConfigureCommandButton(resetButton, null, false);
            resetButton.onClick.AddListener((UnityAction)(() => 
            {
                Quips.SendCommand(".fam talent reset");
                _cachedAllocatedTalents.Clear();
                UpdateFamiliarTalentsPanel();
            }));
        }
    }

    private void CreateTalentPath(Transform parent, TextMeshProUGUI reference, string pathName,
        (int id, string name, bool isKeystone)[] talents)
    {
        RectTransform pathContainer = CreateRectTransformObject($"TalentPath_{pathName}", parent);
        if (pathContainer == null) return;

        // Vertical layout for nodes in this path
        VerticalLayoutGroup vLayout = pathContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.spacing = 0f; // Spacing handled by connection lines
        vLayout.padding = CreatePadding(0, 0, 10, 10);
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;

        // Path label at top
        TextMeshProUGUI pathLabel = CreateFamiliarText(pathContainer, reference, pathName,
            FamiliarMetaFontScale, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.9f, 0.8f, 0.6f));
        
        if (pathLabel != null)
        {
            LayoutElement labelLayout = pathLabel.gameObject.AddComponent<LayoutElement>();
            labelLayout.minHeight = 24f;
            labelLayout.preferredHeight = 24f;
            
            // Spacer after label
            CreateSpacer(pathContainer, 10f);
        }

        // Create talent nodes with connections
        for (int i = 0; i < talents.Length; i++)
        {
            var talent = talents[i];
            
            // Create connection line BEFORE the node (except the first one)
            if (i > 0)
            {
                CreateConnectionLine(pathContainer, 25f);
            }

            TalentNodeUI nodeUI = CreateTalentNode(pathContainer, reference, talent.id, talent.name,
                Vector2.zero, talent.isKeystone ? TalentKeystoneSize : TalentNodeSize, talent.isKeystone);

            if (nodeUI != null)
            {
                _talentNodes.Add(nodeUI);
            }
        }
    }

    private void CreateConnectionLine(Transform parent, float height)
    {
        RectTransform line = CreateRectTransformObject("ConnectionLine", parent);
        if (line == null) return;

        LayoutElement layout = line.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.minWidth = TalentConnectionWidth;
        layout.preferredWidth = TalentConnectionWidth;
        
        Image img = line.gameObject.AddComponent<Image>();
        // Use a simple sprite for connection line
        ApplySprite(img, new[] { "IconBackground", "BlackSquare" }); 
        img.color = TalentConnectionLockedColor;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        RectTransform spacer = CreateRectTransformObject("Spacer", parent);
        LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = height;
        layout.preferredHeight = height;
    }

    private TalentNodeUI CreateTalentNode(Transform parent, TextMeshProUGUI reference, 
        int talentId, string name, Vector2 position, float size, bool isKeystone)
    {
        // Container for Node + Label
        RectTransform nodeContainer = CreateRectTransformObject($"TalentNodeContainer_{talentId}", parent);
        if (nodeContainer == null) return null;

        VerticalLayoutGroup containerVLayout = nodeContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        containerVLayout.childAlignment = TextAnchor.UpperCenter;
        containerVLayout.spacing = 4f;
        containerVLayout.childControlWidth = true; // Changed to TRUE to prevent stretching if force expand is false
        containerVLayout.childControlHeight = true;
        containerVLayout.childForceExpandWidth = false;
        containerVLayout.childForceExpandHeight = false;

        // Node Icon/Button
        RectTransform nodeRect = CreateRectTransformObject($"TalentNode_{talentId}", nodeContainer);
        LayoutElement nodeLayout = nodeRect.gameObject.AddComponent<LayoutElement>();
        nodeLayout.minWidth = size;
        nodeLayout.preferredWidth = size;
        nodeLayout.minHeight = size;
        nodeLayout.preferredHeight = size;
        nodeRect.sizeDelta = new Vector2(size, size);

        // === GLOW LAYER (behind everything, for allocated effect) ===
        RectTransform glowRect = CreateRectTransformObject($"TalentGlow_{talentId}", nodeRect);
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-6f, -6f);  // Extend beyond node
        glowRect.offsetMax = new Vector2(6f, 6f);
        Image glowImg = glowRect.gameObject.AddComponent<Image>();
        ApplySprite(glowImg, new[] { "Circle", "BloodOrb", "IconBackground" });
        glowImg.color = new Color(0, 0, 0, 0);  // Start invisible
        glowImg.raycastTarget = false;

        // === FRAME LAYER (background frame) ===
        RectTransform frameRect = CreateRectTransformObject($"TalentFrame_{talentId}", nodeRect);
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;
        Image frameImg = frameRect.gameObject.AddComponent<Image>();
        ApplySprite(frameImg, isKeystone ? TalentKeystoneFrameSprites : TalentNodeFrameSprites);
        frameImg.color = TalentNodeLockedColor;
        frameImg.raycastTarget = false;

        // === ICON LAYER (the actual talent icon on top) ===
        float iconPadding = size * 0.15f;
        RectTransform iconRect = CreateRectTransformObject($"TalentIcon_{talentId}", nodeRect);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(iconPadding, iconPadding);
        iconRect.offsetMax = new Vector2(-iconPadding, -iconPadding);
        Image iconImg = iconRect.gameObject.AddComponent<Image>();
        
        // Apply the specific talent icon
        if (_talentDefs.TryGetValue(talentId, out var def) && !string.IsNullOrEmpty(def.Icon))
        {
            ApplySprite(iconImg, new[] { def.Icon, "spell_icon", "IconBackground" });
        }
        else
        {
            ApplySprite(iconImg, new[] { "spell_icon", "IconBackground" });
        }
        iconImg.color = Color.white;  // Icon is always full color
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        // Button goes on the main node (captures clicks)
        SimpleStunButton button = nodeRect.gameObject.AddComponent<SimpleStunButton>();
        Image buttonBg = nodeRect.gameObject.AddComponent<Image>();
        buttonBg.color = new Color(0, 0, 0, 0);  // Invisible, just for raycasting
        buttonBg.raycastTarget = true;
        
        int capturedId = talentId;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener((UnityAction)(() => OnTalentNodeClicked(capturedId)));

        // ADD HOVER (PointerEnter/Exit)
        EventTrigger trigger = nodeRect.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry entryEnter = new() { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((UnityAction<BaseEventData>)delegate { UpdateTalentInfoPanel(capturedId); });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new() { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((UnityAction<BaseEventData>)delegate { ClearTalentInfoPanel(); });
        trigger.triggers.Add(entryExit);

        // ADD SELECT/DESELECT (Gamepad/Keyboard Navigation)
        EventTrigger.Entry entrySelect = new() { eventID = EventTriggerType.Select };
        entrySelect.callback.AddListener((UnityAction<BaseEventData>)delegate { UpdateTalentInfoPanel(capturedId); });
        trigger.triggers.Add(entrySelect);

        EventTrigger.Entry entryDeselect = new() { eventID = EventTriggerType.Deselect };
        entryDeselect.callback.AddListener((UnityAction<BaseEventData>)delegate { ClearTalentInfoPanel(); });
        trigger.triggers.Add(entryDeselect);

        // Label
        TextMeshProUGUI label = CreateFamiliarText(nodeContainer, reference, name,
            FamiliarMetaFontScale * 0.8f, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.9f));
        
        if (label != null)
        {
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.minHeight = 18f;
            labelLayout.preferredHeight = 18f;
            labelLayout.preferredWidth = 100f;
        }

        return new TalentNodeUI
        {
            TalentId = talentId,
            Root = nodeRect,
            Frame = frameImg,
            Icon = iconImg,
            Glow = glowImg,
            Label = label,
            Button = button,
            IsAllocated = false,
            IsAvailable = false,
            IsKeystone = isKeystone
        };
    }

    private void UpdateTalentInfoPanel(int talentId)
    {
        if (_talentDefs.TryGetValue(talentId, out var def))
        {
            if (_infoTitleText != null) _infoTitleText.text = def.Name;
            if (_infoDescText != null) _infoDescText.text = def.Description;
            if (_infoEffectText != null) _infoEffectText.text = def.Effect ?? "";
        }
    }

    private void ClearTalentInfoPanel()
    {
        if (_infoTitleText != null) _infoTitleText.text = "Hover a Talent";
        if (_infoDescText != null) _infoDescText.text = "See details here.";
        if (_infoEffectText != null) _infoEffectText.text = "";
    }

    #endregion

    #region Talent Panel Updates

    private void UpdateFamiliarTalentsPanel()
    {
        if (_talentTreeRoot == null) return;

        // Request talent sync from server (if not already synced, with rate limiting)
        if (!DataService._familiarTalentsDataReady)
        {
            float currentTime = UnityEngine.Time.realtimeSinceStartup;
            if (currentTime - _lastTalentRequestTime >= TalentRequestCooldown)
            {
                _lastTalentRequestTime = currentTime;
                Quips.SendCommand(".fam tl");
            }
        }
        else if (!_talentsSyncedFromServer)
        {
            // Only sync ONCE when data first becomes ready, preserving optimistic updates afterward
            SyncTalentsFromServer();
            _talentsSyncedFromServer = true;
        }

        // Check if we have an active familiar
        string displayName = ResolveFamiliarDisplayName();
        bool hasFamiliar = !displayName.Equals("None", StringComparison.OrdinalIgnoreCase);

        if (!hasFamiliar)
        {
            if (_talentPointsText != null)
            {
                _talentPointsText.text = "No familiar bound - bind a familiar to allocate talents";
            }

            foreach (var node in _talentNodes)
            {
                if (node?.Frame != null)
                {
                    node.Frame.color = TalentNodeLockedColor;
                }
            }
            return;
        }

        // Calculate available talent points (1 per 10 levels + 2 per prestige)
        int availablePoints = _familiarLevel / 10 + (_familiarPrestige * 2);
        int spentPoints = CalculateSpentTalentPoints();
        int remainingPoints = availablePoints - spentPoints;

        if (_talentPointsText != null)
        {
            _talentPointsText.text = $"<color=#FFD700>Talent Points: {remainingPoints}</color> <color=#888>({spentPoints}/{availablePoints} spent)</color> | {displayName} Lv.{_familiarLevel}";
        }

        // Update node visuals based on allocation state
        UpdateTalentNodeVisuals(remainingPoints);
    }

    private int CalculateSpentTalentPoints()
    {
        return _cachedAllocatedTalents.Count;
    }

    private void UpdateTalentNodeVisuals(int remainingPoints)
    {
        List<int> allocatedTalents = GetAllocatedTalents();

        foreach (var node in _talentNodes)
        {
            if (node?.Frame == null) continue;

            bool isAllocated = allocatedTalents.Contains(node.TalentId);
            bool isAvailable = remainingPoints > 0 && CanAllocateTalent(node.TalentId, allocatedTalents);

            node.IsAllocated = isAllocated;
            node.IsAvailable = isAvailable;

            // Update Frame color based on state
            if (isAllocated)
            {
                // Bright allocated color
                node.Frame.color = node.IsKeystone ? TalentNodeKeystoneColor : TalentNodeAllocatedColor;
                
                // Show glow effect
                if (node.Glow != null)
                {
                    node.Glow.color = node.IsKeystone ? TalentKeystoneAllocatedGlowColor : TalentAllocatedGlowColor;
                }
                
                // Make icon fully bright
                if (node.Icon != null)
                {
                    node.Icon.color = Color.white;
                }
            }
            else if (isAvailable)
            {
                // Gold color for available
                node.Frame.color = TalentNodeAvailableColor;
                
                // Hide glow
                if (node.Glow != null)
                {
                    node.Glow.color = new Color(0, 0, 0, 0);
                }
                
                // Icon slightly dimmed
                if (node.Icon != null)
                {
                    node.Icon.color = new Color(1f, 1f, 1f, 0.9f);
                }
            }
            else
            {
                // Locked/gray
                node.Frame.color = TalentNodeLockedColor;
                
                // Hide glow
                if (node.Glow != null)
                {
                    node.Glow.color = new Color(0, 0, 0, 0);
                }
                
                // Icon desaturated/dimmed
                if (node.Icon != null)
                {
                    node.Icon.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                }
            }
            
            // Update connection lines if we track them
        }
    }

    private List<int> GetAllocatedTalents()
    {
        // Merge server-synced talents with local optimistic cache
        HashSet<int> combined = new(_cachedAllocatedTalents);
        foreach (int talentId in DataService._familiarAllocatedTalents)
        {
            combined.Add(talentId);
        }
        return combined.ToList();
    }

    /// <summary>
    /// Syncs local cache from server data when server data becomes available.
    /// </summary>
    private void SyncTalentsFromServer()
    {
        if (DataService._familiarTalentsDataReady)
        {
            // Clear local cache and populate from server
            _cachedAllocatedTalents.Clear();
            foreach (int talentId in DataService._familiarAllocatedTalents)
            {
                _cachedAllocatedTalents.Add(talentId);
            }
        }
    }

    private bool CanAllocateTalent(int talentId, List<int> allocatedTalents)
    {
        // Define prerequisites (matching server-side FamiliarTalentSystem)
        var prerequisites = new Dictionary<int, List<int>>
        {
            { 1, [] }, { 2, [1] }, { 3, [2] }, { 4, [3] },
            { 10, [] }, { 11, [10] }, { 12, [11] }, { 13, [12] },
            { 20, [] }, { 21, [20] }, { 22, [21] }, { 23, [22] },
        };

        if (!prerequisites.TryGetValue(talentId, out var prereqs))
            return false;

        if (allocatedTalents.Contains(talentId))
            return false;

        return prereqs.All(p => allocatedTalents.Contains(p));
    }

    private void OnTalentNodeClicked(int talentId)
    {
        // Calculate remaining points first
        int availablePoints = _familiarLevel / 10 + (_familiarPrestige * 2);
        int spentPoints = CalculateSpentTalentPoints();
        int remainingPoints = availablePoints - spentPoints;

        // Check if we have points AND meet prerequisites
        if (remainingPoints <= 0)
        {
            DebugToolsBridge.TryLogInfo($"[FamiliarsTab] No talent points available. Spent: {spentPoints}/{availablePoints}");
            return; // Don't send command or update UI
        }

        if (!CanAllocateTalent(talentId, GetAllocatedTalents()))
        {
            DebugToolsBridge.TryLogInfo($"[FamiliarsTab] Cannot allocate talent {talentId} - prerequisites not met or already allocated");
            return;
        }

        // Send command to server to allocate talent
        string command = $".fam talent allocate {talentId}";
        DebugToolsBridge.TryLogInfo($"[FamiliarsTab] Talent node clicked: {talentId}, sending command: {command}");

        // Optimistic update - only if validation passed
        _cachedAllocatedTalents.Add(talentId);
        UpdateFamiliarTalentsPanel();

        // Send command using the same pattern as other familiar actions
        if (!string.IsNullOrEmpty(command))
        {
            Quips.SendCommand(command);
        }
    }

    #endregion
}
