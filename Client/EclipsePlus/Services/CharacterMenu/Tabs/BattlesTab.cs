using Eclipse.Services.CharacterMenu.Base;
using Eclipse.Services.CharacterMenu.Interfaces;
using System;
using System.Collections.Generic;
using TMPro;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.CharacterMenu.Tabs;

/// <summary>
/// Character menu tab for displaying Familiar Battle groups and configurations.
/// </summary>
internal class BattlesTab : CharacterMenuTabBase, ICharacterMenuTabWithEntries
{
    #region Properties

    public override string TabId => "Battles";
    public override string TabLabel => "Familiar Battles";
    public override string SectionTitle => "Familiar Battles";
    public override BloodcraftTab TabType => BloodcraftTab.Battles;

    #endregion

    #region ICharacterMenuTabWithEntries

    public List<BloodcraftEntry> BuildEntries()
    {
        var list = new List<BloodcraftEntry>();

        if (!_familiarBattleDataReady)
        {
            list.Add(new BloodcraftEntry("Awaiting familiar battle data...", FontStyles.Normal));
            return list;
        }

        if (!_familiarSystemEnabled)
        {
            list.Add(new BloodcraftEntry("Familiars are disabled.", FontStyles.Normal));
            return list;
        }

        if (!_familiarBattlesEnabled)
        {
            list.Add(new BloodcraftEntry("Familiar battles disabled.", FontStyles.Normal));
            return list;
        }

        string activeGroupName = string.IsNullOrWhiteSpace(_familiarActiveBattleGroup)
            ? "None"
            : _familiarActiveBattleGroup;

        list.Add(new BloodcraftEntry($"Active group: {activeGroupName}", FontStyles.Normal));

        if (_familiarBattleGroups == null || _familiarBattleGroups.Count == 0)
        {
            list.Add(new BloodcraftEntry("No battle groups available.", FontStyles.Normal));
            return list;
        }

        list.Add(new BloodcraftEntry("Groups", FontStyles.Bold));

        for (int i = 0; i < _familiarBattleGroups.Count; i++)
        {
            FamiliarBattleGroupData group = _familiarBattleGroups[i];
            bool isActive = group.Name.Equals(_familiarActiveBattleGroup, StringComparison.OrdinalIgnoreCase);
            FontStyles style = isActive ? FontStyles.Bold : FontStyles.Normal;
            string suffix = isActive ? " (Active)" : string.Empty;
            list.Add(new BloodcraftEntry($"{i + 1} | {group.Name}{suffix}", style,
                command: $".fam cbg {group.Name}", enabled: true));
        }

        FamiliarBattleGroupData activeGroup = FindBattleGroup(_familiarActiveBattleGroup);
        if (activeGroup == null && _familiarBattleGroups.Count > 0)
        {
            activeGroup = _familiarBattleGroups[0];
        }

        if (activeGroup != null)
        {
            list.Add(new BloodcraftEntry("Slots", FontStyles.Bold));

            for (int i = 0; i < activeGroup.Slots.Count; i++)
            {
                FamiliarBattleSlotData slot = activeGroup.Slots[i];
                string slotText = FormatFamiliarSlot(slot, i + 1);
                list.Add(new BloodcraftEntry(slotText, FontStyles.Normal, command: $".fam sbg {i + 1}", enabled: true));
            }
        }

        return list;
    }

    #endregion

    #region Lifecycle

    public override void Update()
    {
        // Update handled by orchestrator calling BuildEntries
    }

    #endregion

    #region Private Methods

    private static FamiliarBattleGroupData FindBattleGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || _familiarBattleGroups == null)
            return null;

        foreach (var group in _familiarBattleGroups)
        {
            if (group.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                return group;
        }

        return null;
    }

    private static string FormatFamiliarSlot(FamiliarBattleSlotData slot, int slotNumber)
    {
        if (string.IsNullOrEmpty(slot.Name))
            return $"  {slotNumber}. Empty";

        return $"  {slotNumber}. {slot.Name} (Lv.{slot.Level})";
    }

    #endregion
}
