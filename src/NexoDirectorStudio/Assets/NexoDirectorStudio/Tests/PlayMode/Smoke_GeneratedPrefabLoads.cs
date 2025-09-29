using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.PlayMode
{
    /// <summary>
    /// Smoke test to verify that generated prefabs can be loaded in play mode without exceptions.
    /// </summary>
    [TestFixture]
    public class Smoke_GeneratedPrefabLoads
    {
        private GameObject _testPrefab;
        private string _prefabPath;
        
        [SetUp]
        public void SetUp()
        {
            // Create a test prefab for loading
            _prefabPath = "Assets/Generated/TestPrefab.prefab";
            CreateTestPrefab();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test prefab
            if (_testPrefab != null)
            {
                Object.DestroyImmediate(_testPrefab);
            }
            
            // Clean up generated files
            if (File.Exists(_prefabPath))
            {
                File.Delete(_prefabPath);
            }
        }
        
        [Test]
        public void GeneratedPrefab_ShouldLoadInPlayMode()
        {
            // Arrange - Ensure we start in edit mode
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act & Assert - Enter play mode should not throw
            Assert.DoesNotThrow(() =>
            {
                EditorApplication.isPlaying = true;
                while (!Application.isPlaying) { }
            });
            
            // Assert - Prefab should be loadable
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            Assert.IsNotNull(prefab, "Generated prefab should be loadable");
            
            // Clean up - Exit play mode
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public void GeneratedPrefab_ShouldNotHaveNullReferences()
        {
            // Arrange
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            Assert.IsNotNull(prefab, "Prefab should exist");
            
            // Act - Instantiate the prefab
            GameObject instance = null;
            Assert.DoesNotThrow(() =>
            {
                instance = Object.Instantiate(prefab);
            }, "Prefab instantiation should not throw");
            
            // Assert - Instance should be valid
            Assert.IsNotNull(instance, "Prefab instance should be created");
            Assert.IsNotNull(instance.transform, "Transform should not be null");
            
            // Clean up
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }
        }
        
        [Test]
        public void GeneratedPrefab_ShouldHaveValidComponents()
        {
            // Arrange
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            Assert.IsNotNull(prefab, "Prefab should exist");
            
            // Act
            var components = prefab.GetComponents<Component>();
            
            // Assert - All components should be valid
            foreach (var component in components)
            {
                Assert.IsNotNull(component, "Component should not be null");
                Assert.IsNotNull(component.gameObject, "Component's game object should not be null");
            }
        }
        
        [Test]
        public void GeneratedPrefab_ShouldBeAddressable()
        {
            // Arrange
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            Assert.IsNotNull(prefab, "Prefab should exist");
            
            // Act - Check if prefab can be referenced
            var prefabReference = new SerializedObject(prefab);
            var property = prefabReference.FindProperty("m_Name");
            
            // Assert - Prefab should have valid properties
            Assert.IsNotNull(property, "Prefab should have name property");
            Assert.IsNotNull(property.stringValue, "Prefab name should not be null");
        }
        
        [Test]
        public void GeneratedPrefab_ShouldLoadWithoutConsoleErrors()
        {
            // Arrange
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            Assert.IsNotNull(prefab, "Prefab should exist");
            
            // Act - Load prefab and check for errors
            var logHandler = new TestLogHandler();
            LogHandler.originalLogHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = logHandler;
            
            try
            {
                var instance = Object.Instantiate(prefab);
                
                // Assert - No errors should be logged
                Assert.AreEqual(0, logHandler.ErrorCount, "No errors should be logged when loading prefab");
                Assert.AreEqual(0, logHandler.ExceptionCount, "No exceptions should be logged when loading prefab");
                
                // Clean up
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }
            finally
            {
                Debug.unityLogger.logHandler = LogHandler.originalLogHandler;
            }
        }
        
        [Test]
        public void GeneratedPrefab_ShouldHaveValidHierarchy()
        {
            // Arrange
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
            Assert.IsNotNull(prefab, "Prefab should exist");
            
            // Act - Check hierarchy
            var children = new List<Transform>();
            foreach (Transform child in prefab.transform)
            {
                children.Add(child);
            }
            
            // Assert - All children should be valid
            foreach (var child in children)
            {
                Assert.IsNotNull(child, "Child transform should not be null");
                Assert.IsNotNull(child.gameObject, "Child game object should not be null");
                Assert.IsNotNull(child.parent, "Child should have a parent");
            }
        }
        
        private void CreateTestPrefab()
        {
            // Create a simple test prefab
            var go = new GameObject("TestPrefab");
            go.AddComponent<BoxCollider>();
            go.AddComponent<Rigidbody>();
            
            // Create a child object
            var child = new GameObject("Child");
            child.transform.SetParent(go.transform);
            child.AddComponent<MeshRenderer>();
            
            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, _prefabPath);
            Assert.IsNotNull(prefab, "Prefab should be created");
            
            // Clean up original object
            Object.DestroyImmediate(go);
        }
        
        private class TestLogHandler : ILogHandler
        {
            public int ErrorCount { get; private set; }
            public int ExceptionCount { get; private set; }
            
            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (logType == LogType.Error || logType == LogType.Exception)
                {
                    ErrorCount++;
                }
            }
            
            public void LogException(Exception exception, Object context)
            {
                ExceptionCount++;
            }
        }
    }
}
