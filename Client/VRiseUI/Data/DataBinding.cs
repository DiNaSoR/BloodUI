using VRiseUI.Adapters.Models;

namespace VRiseUI.Data;

/// <summary>
/// Binds a UI component to a data source, handling refresh and caching.
/// </summary>
public class DataBinding
{
    private readonly DataSourceDef _definition;
    private readonly ResponseParser _parser;
    private readonly Action<Dictionary<string, object>> _onDataChanged;
    private float _refreshTimer;
    private bool _isActive;

    public DataBinding(DataSourceDef definition, Action<Dictionary<string, object>> onDataChanged)
    {
        _definition = definition;
        _onDataChanged = onDataChanged;
        _parser = new ResponseParser(definition);

        _parser.OnDataUpdated += data =>
        {
            _onDataChanged?.Invoke(data);
        };
    }

    /// <summary>
    /// Start the data binding (begin polling if configured).
    /// </summary>
    public void Start()
    {
        _isActive = true;
        _refreshTimer = 0;

        // Initial fetch
        _parser.RequestRefresh();
    }

    /// <summary>
    /// Stop the data binding.
    /// </summary>
    public void Stop()
    {
        _isActive = false;
    }

    /// <summary>
    /// Update the binding (called each frame).
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        // Check if we need to refresh
        if (_definition.RefreshInterval > 0)
        {
            _refreshTimer += deltaTime * 1000; // Convert to ms

            if (_refreshTimer >= _definition.RefreshInterval)
            {
                _refreshTimer = 0;

                // Only refresh if cache is invalid
                if (!_parser.IsCacheValid())
                {
                    _parser.RequestRefresh();
                }
            }
        }
    }

    /// <summary>
    /// Force a refresh of data.
    /// </summary>
    public void ForceRefresh()
    {
        _parser.RequestRefresh();
    }

    /// <summary>
    /// Get the last known data.
    /// </summary>
    public Dictionary<string, object> GetData() => _parser.GetLastData();
}
