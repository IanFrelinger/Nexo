using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Shared.Interfaces.Types;
using Nexo.Shared.Values;

namespace Nexo.Core.Domain.Values
{
    /// <summary>
    /// Represents task types as a type value
    /// </summary>
    public sealed class TaskType : BaseTypeValue, IParsableTypeValue, ISerializableTypeValue
    {
        private TaskType(string name, string description, int value) : base(name, description, value)
        {
        }

        // Static instances for each task type
        public static readonly TaskType CodeGeneration = new("CodeGeneration", "Code generation task", 0);
        public static readonly TaskType CodeAnalysis = new("CodeAnalysis", "Code analysis task", 1);
        public static readonly TaskType Deployment = new("Deployment", "Deployment task", 2);
        public static readonly TaskType SecurityAnalysis = new("SecurityAnalysis", "Security analysis task", 3);
        public static readonly TaskType Communication = new("Communication", "Communication task", 4);
        public static readonly TaskType DataProcessing = new("DataProcessing", "Data processing task", 5);
        public static readonly TaskType UserInterfaceDesign = new("UserInterfaceDesign", "User interface design task", 6);
        public static readonly TaskType Infrastructure = new("Infrastructure", "Infrastructure task", 7);
        public static readonly TaskType Testing = new("Testing", "Testing task", 8);
        public static readonly TaskType Documentation = new("Documentation", "Documentation task", 9);
        public static readonly TaskType Custom = new("Custom", "Custom task", 10);

        // Collection of all task types
        public static readonly IReadOnlyList<TaskType> All = new[]
        {
            CodeGeneration, CodeAnalysis, Deployment, SecurityAnalysis, Communication, 
            DataProcessing, UserInterfaceDesign, Infrastructure, Testing, Documentation, Custom
        };

        // Factory methods
        public static TaskType FromName(string name) => 
            All.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Custom;

        public static TaskType FromValue(int value) => 
            All.FirstOrDefault(s => s.Value == value) ?? Custom;

        // IParsableTypeValue implementation
        public static bool TryParse(string value, out ITypeValue? result)
        {
            var taskType = FromName(value);
            result = taskType;
            return taskType != Custom || value.Equals("Custom", StringComparison.OrdinalIgnoreCase);
        }

        public static ITypeValue Parse(string value)
        {
            if (TryParse(value, out var result))
                return result;
            throw new ArgumentException($"Invalid task type: {value}", nameof(value));
        }

        // ISerializableTypeValue implementation
        public object ToSerializable() => new { Name, Description, Value, Type = "TaskType" };

        public static ITypeValue FromSerializable(object value)
        {
            if (value is { } obj)
            {
                var name = obj.GetType().GetProperty("Name")?.GetValue(obj)?.ToString();
                if (!string.IsNullOrEmpty(name))
                    return FromName(name);
            }
            return Custom;
        }
    }
}
