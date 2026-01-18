using VRiseUI.Adapters.Models;
using UnityEngine;

namespace VRiseUI.UI.Components;

/// <summary>
/// Label component - displays text.
/// </summary>
public class Label : UIComponentBase
{
    private TMPro.TextMeshProUGUI _text;

    public Label(Adapter adapter, HudElementDef definition) : base(adapter, definition) { }

    protected override void CreateUI()
    {
        _text = _gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        _text.text = _definition.Label ?? "";
        _text.fontSize = _definition.Style?.FontSize ?? 14f;
        _text.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        _text.color = ParseColor(_definition.Style?.ForegroundColor, Color.white);
    }

    /// <summary>
    /// Set the label text.
    /// </summary>
    public void SetText(string text)
    {
        if (_text != null)
            _text.text = text;
    }
}
