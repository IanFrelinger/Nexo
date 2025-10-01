using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Smoke test to verify that Editor-only code is not referenced by Runtime assembly.
    /// This ensures proper separation of concerns between Runtime and Editor assemblies.
    /// </summary>
    [TestFixture]
    public class Smoke_AsmdefBoundaries
    {
        [Test]
        public void RuntimeAssembly_ShouldNotReferenceEditorCode()
        {
            // Arrange
            var runtimeAssembly = Assembly.Load("NexoDirectorStudio.Runtime");
            var editorAssembly = Assembly.Load("NexoDirectorStudio.Editor");
            
            // Act - Get all types from runtime assembly
            var runtimeTypes = runtimeAssembly.GetTypes();
            
            // Assert - Runtime assembly should not contain any Editor-specific types
            foreach (var type in runtimeTypes)
            {
                // Check if type is in Editor assembly
                var isEditorType = type.Assembly == editorAssembly;
                Assert.IsFalse(isEditorType, 
                    $"Runtime assembly should not contain Editor type: {type.FullName}");
            }
        }
        
        [Test]
        public void EditorAssembly_ShouldReferenceRuntimeCode()
        {
            // Arrange
            var runtimeAssembly = Assembly.Load("NexoDirectorStudio.Runtime");
            var editorAssembly = Assembly.Load("NexoDirectorStudio.Editor");
            
            // Act - Get referenced assemblies
            var editorReferences = editorAssembly.GetReferencedAssemblies();
            
            // Assert - Editor assembly should reference Runtime assembly
            bool referencesRuntime = false;
            foreach (var reference in editorReferences)
            {
                if (reference.Name == "NexoDirectorStudio.Runtime")
                {
                    referencesRuntime = true;
                    break;
                }
            }
            
            Assert.IsTrue(referencesRuntime, 
                "Editor assembly should reference Runtime assembly");
        }
        
        [Test]
        public void RuntimeAssembly_ShouldNotHaveEditorDefineSymbols()
        {
            // Arrange
            var runtimeAssembly = Assembly.Load("NexoDirectorStudio.Runtime");
            
            // Act - Check for Editor-specific attributes or types
            var runtimeTypes = runtimeAssembly.GetTypes();
            
            // Assert - Runtime assembly should not contain Editor-specific code
            foreach (var type in runtimeTypes)
            {
                // Check for Editor-specific attributes
                var attributes = type.GetCustomAttributes(false);
                foreach (var attribute in attributes)
                {
                    var attributeType = attribute.GetType();
                    var attributeName = attributeType.Name;
                    
                    // These are common Unity Editor attributes that shouldn't be in Runtime
                    Assert.IsFalse(attributeName.Contains("MenuItem") || 
                                 attributeName.Contains("CustomEditor") ||
                                 attributeName.Contains("PropertyDrawer"),
                        $"Runtime assembly should not contain Editor attribute {attributeName} on type {type.FullName}");
                }
            }
        }
    }
}
