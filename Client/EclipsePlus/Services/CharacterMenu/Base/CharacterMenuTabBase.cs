using Eclipse.Services.CharacterMenu.Interfaces;
using TMPro;
using UnityEngine;
using static Eclipse.Services.DataService;

namespace Eclipse.Services.CharacterMenu.Base;

/// <summary>
/// Abstract base class for all character menu tabs.
/// Provides common functionality and lifecycle management.
/// </summary>
internal abstract class CharacterMenuTabBase : ICharacterMenuTab
{
    protected Transform ContentRoot { get; private set; }
    protected TextMeshProUGUI ReferenceText { get; private set; }

    public abstract string TabId { get; }
    public abstract string TabLabel { get; }
    public abstract string SectionTitle { get; }
    public abstract BloodcraftTab TabType { get; }
    public virtual int SortOrder => (int)TabType;
    public bool IsInitialized { get; protected set; }

    public virtual void Initialize(Transform contentRoot, TextMeshProUGUI referenceText)
    {
        ContentRoot = contentRoot;
        ReferenceText = referenceText;
        IsInitialized = true;
    }

    public abstract void Update();

    public virtual void Reset()
    {
        IsInitialized = false;
        ContentRoot = null;
        ReferenceText = null;
    }

    public virtual void OnActivated()
    {
        // Override in derived classes if needed
    }

    public virtual void OnDeactivated()
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Safely destroys a GameObject if it exists.
    /// </summary>
    protected static void SafeDestroy(GameObject obj)
    {
        if (obj != null)
        {
            UnityEngine.Object.Destroy(obj);
        }
    }
}
