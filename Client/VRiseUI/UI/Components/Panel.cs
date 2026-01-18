using VRiseUI.Adapters.Models;
using UnityEngine;
using UnityEngine.UI;

namespace VRiseUI.UI.Components;

/// <summary>
/// Panel component - container for other elements.
/// </summary>
public class Panel : UIComponentBase
{
    private Image _background;

    public Panel(Adapter adapter, HudElementDef definition) : base(adapter, definition) { }

    protected override void CreateUI()
    {
        // Background
        _background = _gameObject.AddComponent<Image>();
        _background.color = ParseColor(_definition.Style?.BackgroundColor, new Color(0.1f, 0.1f, 0.1f, 0.85f));

        // Add layout group for child elements
        var layout = _gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding.left = 10;
        layout.padding.right = 10;
        layout.padding.top = 10;
        layout.padding.bottom = 10;
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    /// <summary>
    /// Add a child component to this panel.
    /// </summary>
    public void AddChild(UIComponentBase child)
    {
        var childGo = child.GetGameObject();
        if (childGo != null && _gameObject != null)
        {
            childGo.transform.SetParent(_gameObject.transform, false);
        }
    }
}
