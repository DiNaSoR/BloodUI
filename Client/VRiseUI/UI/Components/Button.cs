using VRiseUI.Adapters.Models;
using UnityEngine;
using UnityEngine.UI;

namespace VRiseUI.UI.Components;

/// <summary>
/// Button component - clickable element that sends a command.
/// </summary>
public class Button : UIComponentBase
{
    private UnityEngine.UI.Button _button;
    private Image _background;
    private TMPro.TextMeshProUGUI _text;
    private string _command;

    public Button(Adapter adapter, HudElementDef definition) : base(adapter, definition) { }

    protected override void CreateUI()
    {
        // Background image
        _background = _gameObject.AddComponent<Image>();
        _background.color = ParseColor(_definition.Style?.BackgroundColor, new Color(0.2f, 0.2f, 0.2f, 0.9f));

        // Button component
        _button = _gameObject.AddComponent<UnityEngine.UI.Button>();
        _button.targetGraphic = _background;

        // Set up button colors
        var colors = _button.colors;
        colors.normalColor = _background.color;
        colors.highlightedColor = _background.color * 1.2f;
        colors.pressedColor = _background.color * 0.8f;
        _button.colors = colors;

        // Label text
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(_gameObject.transform, false);
        _text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        _text.text = _definition.Label ?? "Button";
        _text.fontSize = _definition.Style?.FontSize ?? 14f;
        _text.alignment = TMPro.TextAlignmentOptions.Center;
        _text.color = ParseColor(_definition.Style?.ForegroundColor, Color.white);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // Store command if defined
        _command = _definition.DataSource?.Command;

        // Add click listener (IL2CPP compatible)
        _button.onClick.AddListener((UnityEngine.Events.UnityAction)OnClickHandler);
    }

    private void OnClickHandler()
    {
        if (!string.IsNullOrEmpty(_command))
        {
            // Send command via CommandBridge
            Data.CommandBridge.SendCommand(_command);
        }
    }

    /// <summary>
    /// Set the button label.
    /// </summary>
    public void SetLabel(string text)
    {
        if (_text != null)
            _text.text = text;
    }

    public override void Destroy()
    {
        if (_button != null)
            _button.onClick.RemoveAllListeners();
        base.Destroy();
    }
}
