using Eclipse.Services.CharacterMenu.Tabs;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.CharacterMenu;

/// <summary>
/// Integration layer between the legacy CharacterMenuService and the new modular tab components.
/// Provides factory methods to create and register tabs with the orchestrator.
/// </summary>
internal static class CharacterMenuIntegration
{
    private static CharacterMenuOrchestrator _orchestrator;

    /// <summary>
    /// Gets or creates the CharacterMenu orchestrator.
    /// </summary>
    public static CharacterMenuOrchestrator GetOrCreateOrchestrator()
    {
        if (_orchestrator == null)
        {
            _orchestrator = new CharacterMenuOrchestrator();
            RegisterDefaultTabs();
        }
        return _orchestrator;
    }

    /// <summary>
    /// Registers all default tabs with the orchestrator.
    /// </summary>
    private static void RegisterDefaultTabs()
    {
        if (_orchestrator == null) return;

        _orchestrator.RegisterTab(new PrestigeTab());
        _orchestrator.RegisterTab(new ExoformTab());
        _orchestrator.RegisterTab(new StatBonusesTab());
        _orchestrator.RegisterTab(new ProgressionTab());
        _orchestrator.RegisterTab(new FamiliarsTab());
    }

    /// <summary>
    /// Gets the Prestige tab.
    /// </summary>
    public static PrestigeTab GetPrestigeTab()
    {
        return _orchestrator?.GetTab<PrestigeTab>(BloodcraftTab.Prestige);
    }

    /// <summary>
    /// Gets the Exoform tab.
    /// </summary>
    public static ExoformTab GetExoformTab()
    {
        return _orchestrator?.GetTab<ExoformTab>(BloodcraftTab.Exoform);
    }

    /// <summary>
    /// Gets the StatBonuses tab.
    /// </summary>
    public static StatBonusesTab GetStatBonusesTab()
    {
        return _orchestrator?.GetTab<StatBonusesTab>(BloodcraftTab.StatBonuses);
    }

    /// <summary>
    /// Gets the Professions tab.
    /// </summary>
    public static ProfessionsTab GetProfessionsTab()
    {
        ProgressionTab progressionTab = _orchestrator?.GetTab<ProgressionTab>(BloodcraftTab.Progression);
        return progressionTab?.ProfessionsTabInstance;
    }

    /// <summary>
    /// Gets the Familiars tab.
    /// </summary>
    public static FamiliarsTab GetFamiliarsTab()
    {
        return _orchestrator?.GetTab<FamiliarsTab>(BloodcraftTab.Familiars);
    }

    /// <summary>
    /// Resets the integration layer and orchestrator.
    /// </summary>
    public static void Reset()
    {
        _orchestrator?.Reset();
        _orchestrator = null;
    }

    /// <summary>
    /// Gets the currently active tab type.
    /// </summary>
    public static BloodcraftTab GetActiveTab()
    {
        return _orchestrator?.ActiveTab ?? BloodcraftTab.Prestige;
    }

    /// <summary>
    /// Sets the active tab type.
    /// </summary>
    public static void SetActiveTab(BloodcraftTab tabType)
    {
        _orchestrator?.SetActiveTab(tabType);
    }
}
