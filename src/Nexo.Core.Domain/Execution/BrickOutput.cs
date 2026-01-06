namespace Nexo.Core.Domain.Execution;

/// <summary>
/// Output data from a brick execution.
/// </summary>
public class BrickOutput
{
    private readonly Dictionary<string, object> _data = new();
    
    public string? Summary { get; set; }
    
    public object this[string key]
    {
        get => _data[key];
        set => _data[key] = value;
    }
    
    public void Set(string key, object value)
    {
        _data[key] = value;
    }
    
    public T Get<T>(string key)
    {
        if (!_data.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Output key '{key}' not found");
        
        if (value is T typedValue)
            return typedValue;
        
        throw new InvalidCastException($"Output key '{key}' is not of type {typeof(T).Name}");
    }
    
    public IReadOnlyDictionary<string, object> ToDictionary() => _data;
}

