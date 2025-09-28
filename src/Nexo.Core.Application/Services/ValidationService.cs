using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Commands.Project;
using Nexo.Shared.Values;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// Service for validating commands and data
    /// </summary>
    public class ValidationService
    {
        /// <summary>
        /// Validates a command input
        /// </summary>
        public CommandValidationResult ValidateCommandInput<T>(T input)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (input == null)
            {
                errors.Add("Input cannot be null");
                return new CommandValidationResult
                {
                    IsValid = false,
                    Errors = errors.ToArray(),
                    Warnings = warnings.ToArray()
                };
            }

            // Basic validation - in a real implementation, you'd have more sophisticated validation
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                var value = property.GetValue(input);
                if (value == null && !IsNullable(property.PropertyType))
                {
                    errors.Add($"Property '{property.Name}' cannot be null");
                }
            }

            return new CommandValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        /// <summary>
        /// Validates an agent request
        /// </summary>
        public CommandValidationResult ValidateAgentRequest(CreateAgentRequest request)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (request.Id == null)
            {
                errors.Add("Agent ID cannot be null");
            }

            if (request.Name == null)
            {
                errors.Add("Agent name cannot be null");
            }

            if (string.IsNullOrWhiteSpace(request.Name?.Value))
            {
                errors.Add("Agent name cannot be empty");
            }

            return new CommandValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        /// <summary>
        /// Validates a project creation request
        /// </summary>
        public CommandValidationResult ValidateProjectRequest(CreateProjectInput input)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(input.Name))
            {
                errors.Add("Project name cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(input.Path))
            {
                errors.Add("Project path cannot be empty");
            }

            return new CommandValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors.ToArray(),
                Warnings = warnings.ToArray()
            };
        }

        private static bool IsNullable(Type type)
        {
            return !type.IsValueType || (Nullable.GetUnderlyingType(type) != null);
        }
    }
}
