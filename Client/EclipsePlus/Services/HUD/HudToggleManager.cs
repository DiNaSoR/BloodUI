using System;
using System.Collections.Generic;
using UnityEngine;
using static Eclipse.Services.CanvasService.DataHUD;

namespace Eclipse.Services.HUD;

/// <summary>
/// Manages visibility toggling of HUD elements.
/// Extracted from CanvasService.ToggleHUD.
/// </summary>
internal static class HudToggleManager
{
    public static readonly Dictionary<int, Action> ActionToggles = new()
    {
        { 0, ExperienceToggle },
        { 1, LegacyToggle },
        { 2, ExpertiseToggle },
        { 3, FamiliarToggle },
        { 4, ProfessionToggle },
        { 5, DailyQuestToggle },
        { 6, WeeklyQuestToggle },
        { 7, ShiftSlotToggle }
    };

    static void DailyQuestToggle()
    {
        if (_dailyQuestObject == null) return;
        bool active = !_dailyQuestObject.activeSelf;
        _dailyQuestObject.SetActive(active);
        ObjectStates[_dailyQuestObject] = active;
    }

    static void ExperienceToggle()
    {
        if (_experienceBarGameObject == null) return;
        bool active = !_experienceBarGameObject.activeSelf;
        _experienceBarGameObject.SetActive(active);
        ObjectStates[_experienceBarGameObject] = active;
    }

    static void ExpertiseToggle()
    {
        if (_expertiseBarGameObject == null) return;
        bool active = !_expertiseBarGameObject.activeSelf;
        _expertiseBarGameObject.SetActive(active);
        ObjectStates[_expertiseBarGameObject] = active;
    }

    static void FamiliarToggle()
    {
        if (_familiarBarGameObject == null) return;
        bool active = !_familiarBarGameObject.activeSelf;
        _familiarBarGameObject.SetActive(active);
        ObjectStates[_familiarBarGameObject] = active;
    }

    static void LegacyToggle()
    {
        if (_legacyBarGameObject == null) return;
        bool active = !_legacyBarGameObject.activeSelf;
        _legacyBarGameObject.SetActive(active);
        ObjectStates[_legacyBarGameObject] = active;
    }

    static void ProfessionToggle()
    {
        foreach (GameObject professionObject in ProfessionObjects)
        {
            if (professionObject == null) continue;
            bool active = !professionObject.activeSelf;
            professionObject.SetActive(active);
            ObjectStates[professionObject] = active;
        }
    }

    static void ShiftSlotToggle()
    {
        if (_abilityDummyObject == null) return;
        bool active = !_abilityDummyObject.activeSelf;
        _abilityDummyObject.SetActive(active);
        ObjectStates[_abilityDummyObject] = active;
    }

    public static void ToggleAllObjects()
    {
        _active = !_active;

        foreach (GameObject gameObject in ObjectStates.Keys)
        {
            if (gameObject == null) continue;
            gameObject.SetActive(_active);
            ObjectStates[gameObject] = _active;
        }
    }

    public static void ToggleGameObjects(params GameObject[] gameObjects)
    {
        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject == null) continue;
            bool newState = !gameObject.activeSelf;
            gameObject.SetActive(newState);
            ObjectStates[gameObject] = newState;
        }
    }

    static void WeeklyQuestToggle()
    {
        if (_weeklyQuestObject == null) return;
        bool active = !_weeklyQuestObject.activeSelf;
        _weeklyQuestObject.SetActive(active);
        ObjectStates[_weeklyQuestObject] = active;
    }
}
