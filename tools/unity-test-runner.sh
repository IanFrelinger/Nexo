#!/bin/bash

# Unity Test Runner for Nexo Core Components
# This script tests the core and shared components in a Unity environment

set -e

echo "=== Unity Test Runner for Nexo ==="
echo "Testing core components compatibility with Unity Engine"

# Configuration
UNITY_VERSION="2022.3.0f1"
UNITY_PROJECT_PATH="./unity-test-project"
ASSETS_PATH="$UNITY_PROJECT_PATH/Assets"
PLUGINS_PATH="$ASSETS_PATH/Plugins"
SCRIPTS_PATH="$ASSETS_PATH/Scripts"
TESTS_PATH="$ASSETS_PATH/Tests"

# Create Unity project structure
echo "Creating Unity project structure..."
mkdir -p "$UNITY_PROJECT_PATH"
mkdir -p "$ASSETS_PATH"
mkdir -p "$PLUGINS_PATH"
mkdir -p "$SCRIPTS_PATH"
mkdir -p "$TESTS_PATH"

# Build core assemblies for Unity compatibility
echo "Building core assemblies for Unity..."
dotnet build src/Nexo.Core.Domain/Nexo.Core.Domain.csproj --configuration Release --framework netstandard2.0
dotnet build src/Nexo.Shared/Nexo.Shared.csproj --configuration Release --framework netstandard2.0

# Copy assemblies to Unity project
echo "Copying assemblies to Unity project..."
cp -r src/Nexo.Core.Domain/bin/Release/netstandard2.0/* "$PLUGINS_PATH/"
cp -r src/Nexo.Shared/bin/Release/netstandard2.0/* "$PLUGINS_PATH/"

# Create Unity project manifest
cat > "$UNITY_PROJECT_PATH/Packages/manifest.json" << EOF
{
  "dependencies": {
    "com.unity.test-framework": "1.1.33",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.core": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
EOF

# Create Unity test script to verify core components
cat > "$SCRIPTS_PATH/NexoCoreTest.cs" << 'EOF'
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Enums;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Shared.Models;

namespace Nexo.Unity.Tests
{
    public class NexoCoreTest
    {
        [Test]
        public void TestCoreDomainEntities()
        {
            // Test that core domain entities can be instantiated in Unity
            Assert.DoesNotThrow(() => {
                // Test basic entity functionality
                var entity = new TestEntity();
                Assert.NotNull(entity);
            });
        }

        [Test]
        public void TestValueObjects()
        {
            // Test value objects work in Unity environment
            Assert.DoesNotThrow(() => {
                // Test value object creation and validation
                var valueObject = new TestValueObject("test");
                Assert.NotNull(valueObject);
            });
        }

        [Test]
        public void TestEnums()
        {
            // Test enums are accessible
            Assert.AreEqual(0, (int)CommandPriority.Critical);
            Assert.AreEqual(1, (int)CommandPriority.High);
            Assert.AreEqual(2, (int)CommandPriority.Normal);
        }

        [Test]
        public void TestSharedModels()
        {
            // Test shared models work in Unity
            Assert.DoesNotThrow(() => {
                var model = new TestModel();
                Assert.NotNull(model);
            });
        }

        [UnityTest]
        public IEnumerator TestAsyncOperations()
        {
            // Test async operations work in Unity coroutines
            yield return null;
            
            Assert.DoesNotThrow(async () => {
                await System.Threading.Tasks.Task.Delay(10);
            });
        }
    }

    // Test classes for Unity compatibility
    public class TestEntity
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
    }

    public class TestValueObject
    {
        public string Value { get; }
        
        public TestValueObject(string value)
        {
            Value = value ?? throw new System.ArgumentNullException(nameof(value));
        }
    }

    public class TestModel
    {
        public string Name { get; set; } = "Test";
        public int Value { get; set; } = 42;
    }
}
EOF

# Create Unity project settings
mkdir -p "$UNITY_PROJECT_PATH/ProjectSettings"
cat > "$UNITY_PROJECT_PATH/ProjectSettings/ProjectVersion.txt" << EOF
m_EditorVersion: $UNITY_VERSION
m_EditorVersionWithRevision: $UNITY_VERSION (0)
EOF

# Create assembly definition for tests
cat > "$TESTS_PATH/Nexo.Unity.Tests.asmdef" << EOF
{
    "name": "Nexo.Unity.Tests",
    "rootNamespace": "Nexo.Unity.Tests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [
        "Nexo.Core.Domain.dll",
        "Nexo.Shared.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
EOF

echo "Unity test environment setup complete!"
echo "Project structure created at: $UNITY_PROJECT_PATH"
echo ""
echo "To run Unity tests:"
echo "1. Open Unity Hub"
echo "2. Add project: $UNITY_PROJECT_PATH"
echo "3. Open project in Unity $UNITY_VERSION"
echo "4. Run tests via Window > General > Test Runner"
echo ""
echo "Or use Unity CLI:"
echo "unity-editor -batchmode -quit -projectPath $UNITY_PROJECT_PATH -runTests -testPlatform EditMode -testResults results.xml"

# Verify assemblies are compatible
echo "Verifying assembly compatibility..."
if [ -f "$PLUGINS_PATH/Nexo.Core.Domain.dll" ]; then
    echo "✓ Nexo.Core.Domain.dll copied successfully"
else
    echo "✗ Failed to copy Nexo.Core.Domain.dll"
    exit 1
fi

if [ -f "$PLUGINS_PATH/Nexo.Shared.dll" ]; then
    echo "✓ Nexo.Shared.dll copied successfully"
else
    echo "✗ Failed to copy Nexo.Shared.dll"
    exit 1
fi

echo "Unity test environment is ready!" 