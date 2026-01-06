namespace Nexo.Core.Domain.Execution;

/// <summary>
/// Input data for a brick execution.
/// </summary>
public class BrickInput
{
    private readonly Dictionary<string, object> _data = new();
    
    public BrickInput()
    {
    }
    
    public BrickInput(IReadOnlyDictionary<string, object> data)
    {
        foreach (var kvp in data)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }
    
    public void Set(string key, object value)
    {
        _data[key] = value;
    }
    
    public T Get<T>(string key)
    {
        if (!_data.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Input key '{key}' not found");
        
        if (value is T typedValue)
            return typedValue;
        
        throw new InvalidCastException($"Input key '{key}' is not of type {typeof(T).Name}");
    }
    
    public T? Get<T>(string key, T? defaultValue)
    {
        if (!_data.TryGetValue(key, out var value))
            return defaultValue;
        
        if (value is T typedValue)
            return typedValue;
        
        return defaultValue;
    }
    
    public IReadOnlyDictionary<string, object> ToDictionary() => _data;
}

