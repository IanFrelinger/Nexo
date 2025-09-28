using System;
using System.Collections.Generic;
using Nexo.Shared.Values;
using Nexo.Shared.Constants;
using Nexo.Shared.Interfaces.Types;

namespace Nexo.Shared.Examples;

/// <summary>
/// Example demonstrating how to use the new interface-based type system instead of enums
/// </summary>
public static class TypeValueExample
{
    public static void DemonstrateUsage()
    {
        Console.WriteLine("=== Interface-Based Type System Example ===\n");

        // 1. Using type values directly
        DemonstrateDirectUsage();
        
        // 2. Using centralized constants
        DemonstrateCentralizedUsage();
        
        // 3. Parsing and serialization
        DemonstrateParsingAndSerialization();
        
        // 4. Comparison and equality
        DemonstrateComparison();
        
        // 5. Factory methods
        DemonstrateFactoryMethods();
    }

    private static void DemonstrateDirectUsage()
    {
        Console.WriteLine("1. Direct Usage:");
        
        // Create type values directly
        var success = OperationStatus.Success;
        var error = ValidationSeverity.Error;
        var highPriority = Priority.High;
        
        Console.WriteLine($"Operation Status: {success.Name} (Value: {success.Value})");
        Console.WriteLine($"Validation Severity: {error.Name} (Value: {error.Value})");
        Console.WriteLine($"Priority: {highPriority.Name} (Value: {highPriority.Value})");
        Console.WriteLine();
    }

    private static void DemonstrateCentralizedUsage()
    {
        Console.WriteLine("2. Centralized Constants:");
        
        // Use centralized constants
        var status = CentralizedTypes.OperationStatuses.Success;
        var severity = CentralizedTypes.ValidationSeverities.Critical;
        var level = CentralizedTypes.LogLevels.Information;
        
        Console.WriteLine($"Status: {status.Name}");
        Console.WriteLine($"Severity: {severity.Name}");
        Console.WriteLine($"Log Level: {level.Name}");
        Console.WriteLine();
    }

    private static void DemonstrateParsingAndSerialization()
    {
        Console.WriteLine("3. Parsing and Serialization:");
        
        // Parse from string
        if (OperationStatus.TryParse("Success", out var parsedStatus))
        {
            Console.WriteLine($"Parsed Status: {parsedStatus?.Name}");
        }
        
        // Serialize to object
        var serializable = OperationStatus.Success.ToSerializable();
        Console.WriteLine($"Serialized: {serializable}");
        
        // Deserialize from object
        var deserialized = OperationStatus.FromSerializable(serializable);
        Console.WriteLine($"Deserialized: {deserialized?.Name}");
        Console.WriteLine();
    }

    private static void DemonstrateComparison()
    {
        Console.WriteLine("4. Comparison and Equality:");
        
        var status1 = OperationStatus.Success;
        var status2 = OperationStatus.Success;
        var status3 = OperationStatus.Failure;
        
        Console.WriteLine($"status1 == status2: {status1 == status2}");
        Console.WriteLine($"status1 == status3: {status1 == status3}");
        Console.WriteLine($"status1.Equals(status2): {status1.Equals(status2)}");
        Console.WriteLine($"status1.GetHashCode(): {status1.GetHashCode()}");
        Console.WriteLine();
    }

    private static void DemonstrateFactoryMethods()
    {
        Console.WriteLine("5. Factory Methods:");
        
        // From name
        var fromName = OperationStatus.FromName("Success");
        Console.WriteLine($"FromName('Success'): {fromName?.Name}");
        
        // From value
        var fromValue = OperationStatus.FromValue(0);
        Console.WriteLine($"FromValue(0): {fromValue?.Name}");
        
        // From invalid name (returns default)
        var invalid = OperationStatus.FromName("InvalidStatus");
        Console.WriteLine($"FromName('InvalidStatus'): {invalid?.Name}");
        Console.WriteLine();
    }

    public static void DemonstrateCustomTypeValue()
    {
        Console.WriteLine("=== Custom Type Value Example ===\n");
        
        // Example of how you could create your own type value
        var customStatus = new CustomStatus("Processing", "Currently processing", 5);
        Console.WriteLine($"Custom Status: {customStatus.Name} - {customStatus.Description}");
        Console.WriteLine($"Value: {customStatus.Value}");
        Console.WriteLine($"ToString: {customStatus}");
    }
}

/// <summary>
/// Example of a custom type value implementation
/// </summary>
public sealed class CustomStatus : BaseTypeValue
{
    public CustomStatus(string name, string description, int value) 
        : base(name, description, value)
    {
    }
    
    // You can add custom static instances
    public static readonly CustomStatus Processing = new("Processing", "Currently processing", 1);
    public static readonly CustomStatus Completed = new("Completed", "Processing completed", 2);
    public static readonly CustomStatus Failed = new("Failed", "Processing failed", 3);
    
    public static readonly IReadOnlyList<CustomStatus> All = new[]
    {
        Processing, Completed, Failed
    };
    
    public static CustomStatus FromName(string name) => 
        All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Failed;
}
