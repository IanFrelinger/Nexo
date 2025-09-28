namespace Nexo.Core.Domain.Values;

public abstract class BaseTypeValue : ITypeValue
{
    public string Value { get; }
    public string Display { get; }

    protected BaseTypeValue(string value, string display)
    {
        Value = value;
        Display = display;
    }
}
