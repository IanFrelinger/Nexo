using Xunit;

namespace Nexo.Tests.Contracts;

/// <summary>
/// Test category attribute for organizing tests
/// </summary>
public sealed class CategoryAttribute : Attribute
{
    public string Name { get; }
    public CategoryAttribute(string name) => Name = name;
}

/// <summary>
/// Architecture test attribute
/// </summary>
public sealed class ArchitectureFactAttribute : FactAttribute
{
    public ArchitectureFactAttribute() => DisplayName = "Architecture";
}

/// <summary>
/// Contract test attribute
/// </summary>
public sealed class ContractFactAttribute : FactAttribute
{
    public ContractFactAttribute() => DisplayName = "Contract";
}

/// <summary>
/// Integration test attribute
/// </summary>
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute() => DisplayName = "Integration";
}

/// <summary>
/// End-to-end test attribute
/// </summary>
public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute() => DisplayName = "E2E";
}

/// <summary>
/// Test trait constants
/// </summary>
public static class Traits
{
    public const string Category = "Category";
    public const string Architecture = "Architecture";
    public const string Contract = "Contract";
    public const string Integration = "Integration";
    public const string E2E = "E2E";
}
