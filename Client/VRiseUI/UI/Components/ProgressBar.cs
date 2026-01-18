using VRiseUI.Adapters.Models;
using UnityEngine;
using UnityEngine.UI;

namespace VRiseUI.UI.Components;

/// <summary>
/// Progress bar component - displays a value between 0 and 100%.
/// Used for XP, HP, expertise, etc.
/// </summary>
public class ProgressBar : UIComponentBase
{
    private Image _background;
    private Image _fill;
    private TMPro.TextMeshProUGUI _label;
    private TMPro.TextMeshProUGUI _valueText;

    private float _currentValue;
    private float _maxValue = 100f;

    public ProgressBar(Adapter adapter, HudElementDef definition) : base(adapter, definition) { }

    protected override void CreateUI()
    {
        // Background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(_gameObject.transform, false);
        _background = bgGo.AddComponent<Image>();
        _background.color = ParseColor(_definition.Style?.BackgroundColor, new Color(0.1f, 0.1f, 0.1f, 0.8f));

        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(_gameObject.transform, false);
        _fill = fillGo.AddComponent<Image>();
        _fill.color = ParseColor(_definition.Style?.ForegroundColor, new Color(0.3f, 0.6f, 1f, 1f));
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = 0;
        _fill.fillAmount = 0f;

        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Label (optional)
        if (!string.IsNullOrEmpty(_definition.Label))
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_gameObject.transform, false);
            _label = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
            _label.text = _definition.Label;
            _label.fontSize = _definition.Style?.FontSize ?? 12f;
            _label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            _label.color = Color.white;

            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.5f, 1);
            labelRect.offsetMin = new Vector2(5, 0);
            labelRect.offsetMax = new Vector2(0, 0);
        }

        // Value text
        var valueGo = new GameObject("Value");
        valueGo.transform.SetParent(_gameObject.transform, false);
        _valueText = valueGo.AddComponent<TMPro.TextMeshProUGUI>();
        _valueText.text = "0%";
        _valueText.fontSize = _definition.Style?.FontSize ?? 12f;
        _valueText.alignment = TMPro.TextAlignmentOptions.MidlineRight;
        _valueText.color = Color.white;

        var valueRect = valueGo.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.5f, 0);
        valueRect.anchorMax = new Vector2(1, 1);
        valueRect.offsetMin = new Vector2(0, 0);
        valueRect.offsetMax = new Vector2(-5, 0);
    }

    /// <summary>
    /// Set the progress value.
    /// </summary>
    public void SetProgress(float current, float max)
    {
        _currentValue = current;
        _maxValue = max > 0 ? max : 1f;

        var normalized = Mathf.Clamp01(current / _maxValue);
        _fill.fillAmount = normalized;
        _valueText.text = $"{(normalized * 100):F0}%";
    }

    /// <summary>
    /// Set the label text.
    /// </summary>
    public void SetLabel(string text)
    {
        if (_label != null)
            _label.text = text;
    }

    /// <summary>
    /// Handle data updates from the data binding.
    /// </summary>
    protected override void OnDataUpdated(Dictionary<string, object> data)
    {
        if (data == null) return;

        // Try to parse progress data
        if (data.TryGetValue("current", out var current) && data.TryGetValue("max", out var max))
        {
            if (float.TryParse(current?.ToString(), out var currentVal) &&
                float.TryParse(max?.ToString(), out var maxVal))
            {
                SetProgress(currentVal, maxVal);
            }
        }
        else if (data.TryGetValue("percent", out var percent))
        {
            // Handle percentage-based progress
            if (float.TryParse(percent?.ToString(), out var percentVal))
            {
                SetProgress(percentVal, 100f);
            }
        }

        // Update label if level is present
        if (data.TryGetValue("level", out var level))
        {
            SetLabel($"{_definition.Label} Lv{level}");
        }
        else if (data.TryGetValue("weapon", out var weapon))
        {
            SetLabel($"{weapon}");
        }
        else if (data.TryGetValue("blood", out var blood))
        {
            SetLabel($"{blood}");
        }
    }
}
