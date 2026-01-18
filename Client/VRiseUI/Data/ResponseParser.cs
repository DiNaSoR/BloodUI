using System.Text.RegularExpressions;
using VRiseUI.Adapters.Models;

namespace VRiseUI.Data;

/// <summary>
/// Parses mod responses using regex patterns from manifest.
/// </summary>
public class ResponseParser
{
    private readonly DataSourceDef _dataSource;
    private Regex _regex;
    private Dictionary<string, object> _lastData;
    private DateTime _lastUpdate;

    /// <summary>
    /// Event raised when data is updated.
    /// </summary>
    public event Action<Dictionary<string, object>> OnDataUpdated;

    public ResponseParser(DataSourceDef dataSource)
    {
        _dataSource = dataSource;

        if (!string.IsNullOrEmpty(dataSource.Pattern))
        {
            _regex = new Regex(dataSource.Pattern, RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Parse a response string and extract data.
    /// </summary>
    public Dictionary<string, object> Parse(string response)
    {
        if (string.IsNullOrEmpty(response) || _regex == null)
            return null;

        var match = _regex.Match(response);
        if (!match.Success)
            return null;

        var data = new Dictionary<string, object>();

        // Map regex groups to named fields
        if (_dataSource.Mapping != null)
        {
            foreach (var (fieldName, groupPattern) in _dataSource.Mapping)
            {
                var value = ResolveGroupPattern(match, groupPattern);
                data[fieldName] = value;
            }
        }
        else
        {
            // Default: use group indices
            for (int i = 1; i < match.Groups.Count; i++)
            {
                data[$"group{i}"] = match.Groups[i].Value;
            }
        }

        _lastData = data;
        _lastUpdate = DateTime.UtcNow;

        OnDataUpdated?.Invoke(data);

        return data;
    }

    /// <summary>
    /// Resolve a group pattern like "$1" or "$2" to the actual value.
    /// </summary>
    private string ResolveGroupPattern(Match match, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return "";

        // Replace $1, $2, etc. with group values
        var result = pattern;
        for (int i = 1; i < match.Groups.Count; i++)
        {
            result = result.Replace($"${i}", match.Groups[i].Value);
        }

        return result;
    }

    /// <summary>
    /// Get the last parsed data.
    /// </summary>
    public Dictionary<string, object> GetLastData() => _lastData;

    /// <summary>
    /// Check if cached data is still valid.
    /// </summary>
    public bool IsCacheValid()
    {
        if (_lastData == null) return false;

        var cacheTime = _dataSource.CacheTime > 0 ? _dataSource.CacheTime : 1000;
        return (DateTime.UtcNow - _lastUpdate).TotalMilliseconds < cacheTime;
    }

    /// <summary>
    /// Request a refresh of data by sending the command.
    /// </summary>
    public void RequestRefresh()
    {
        if (string.IsNullOrEmpty(_dataSource.Command))
            return;

        CommandBridge.SendCommand(_dataSource.Command, response =>
        {
            Parse(response);
        });
    }
}
