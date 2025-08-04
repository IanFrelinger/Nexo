#!/bin/bash

echo "Setting up Unity test environment..."

# Create Unity project structure
mkdir -p unity-test-project/Assets/Scripts
mkdir -p unity-test-project/Assets/Plugins
mkdir -p unity-test-project/Assets/Tests
mkdir -p unity-test-project/Packages

# Build core assemblies for Unity compatibility
dotnet build src/Nexo.Core.Domain/Nexo.Core.Domain.csproj --configuration Release --framework netstandard2.0
dotnet build src/Nexo.Shared/Nexo.Shared.csproj --configuration Release --framework netstandard2.0

# Copy assemblies to Unity project
cp -r src/Nexo.Core.Domain/bin/Release/netstandard2.0/* unity-test-project/Assets/Plugins/
cp -r src/Nexo.Shared/bin/Release/netstandard2.0/* unity-test-project/Assets/Plugins/

# Create Unity project manifest
cat > unity-test-project/Packages/manifest.json << 'MANIFEST_EOF'
{
  "dependencies": {
    "com.unity.test-framework": "1.1.33",
    "com.unity.modules.core": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0"
  }
}
MANIFEST_EOF

# Create Unity test script
cat > unity-test-project/Assets/Tests/NexoUnityTest.cs << 'TEST_EOF'
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Nexo.Core.Domain.Enums;

namespace Nexo.Unity.Tests
{
    public class NexoUnityTest
    {
        [Test]
        public void TestCommandPriorityEnum()
        {
            Assert.AreEqual(0, (int)CommandPriority.Critical);
            Assert.AreEqual(1, (int)CommandPriority.High);
            Assert.AreEqual(2, (int)CommandPriority.Normal);
        }

        [UnityTest]
        public IEnumerator TestAsyncOperation()
        {
            yield return null;
            Assert.Pass("Unity test environment is working");
        }
    }
}
TEST_EOF

# Create assembly definition
cat > unity-test-project/Assets/Tests/Nexo.Unity.Tests.asmdef << 'ASMDEF_EOF'
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
ASMDEF_EOF

echo "Unity test environment setup complete" 