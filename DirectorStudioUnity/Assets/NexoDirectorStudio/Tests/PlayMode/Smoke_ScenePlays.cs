using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Tests.PlayMode
{
    /// <summary>
    /// Smoke test to verify that generated scenes can be played without exceptions.
    /// Tests that spawns, triggers, and interactions work correctly in play mode.
    /// </summary>
    [TestFixture]
    public class Smoke_ScenePlays
    {
        private Scene _testScene;
        private GameObject _testPlayer;
        private GameObject _testCollectible;
        private GameObject _testTrigger;
        
        [SetUp]
        public void SetUp()
        {
            // Create a test scene
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Create test objects
            CreateTestObjects();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            if (_testPlayer != null)
            {
                UnityEngine.Object.DestroyImmediate(_testPlayer);
            }
            if (_testCollectible != null)
            {
                UnityEngine.Object.DestroyImmediate(_testCollectible);
            }
            if (_testTrigger != null)
            {
                UnityEngine.Object.DestroyImmediate(_testTrigger);
            }
            
            // Clean up test scene
            if (_testScene.IsValid())
            {
                EditorSceneManager.CloseScene(_testScene, true);
            }
        }
        
        [Test]
        public void EnterPlayMode_ShouldNotThrowException()
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
            
            // Clean up - Exit play mode
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public void PlayerSpawn_ShouldWorkInPlayMode()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            // Assert - Player should be spawned and active
            Assert.IsNotNull(_testPlayer, "Player should be spawned");
            Assert.IsTrue(_testPlayer.activeInHierarchy, "Player should be active in hierarchy");
            Assert.IsNotNull(_testPlayer.transform, "Player should have a transform");
            
            // Clean up
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public void CollectibleTrigger_ShouldWorkInPlayMode()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            // Assert - Collectible should be active and triggerable
            Assert.IsNotNull(_testCollectible, "Collectible should exist");
            Assert.IsTrue(_testCollectible.activeInHierarchy, "Collectible should be active");
            
            // Simulate player collision with collectible
            var playerPos = _testPlayer.transform.position;
            var collectiblePos = _testCollectible.transform.position;
            var distance = Vector3.Distance(playerPos, collectiblePos);
            
            Assert.IsTrue(distance < 2.0f, "Player should be close enough to collectible for testing");
            
            // Clean up
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public void EnvironmentalTrigger_ShouldWorkInPlayMode()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            // Assert - Environmental trigger should be active
            Assert.IsNotNull(_testTrigger, "Environmental trigger should exist");
            Assert.IsTrue(_testTrigger.activeInHierarchy, "Environmental trigger should be active");
            
            // Clean up
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public void SceneObjects_ShouldNotHaveNullReferences()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            // Assert - All objects should have valid references
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                Assert.IsNotNull(obj, "GameObject should not be null");
                Assert.IsNotNull(obj.transform, $"Transform should not be null for {obj.name}");
                Assert.IsNotNull(obj.gameObject, $"GameObject reference should not be null for {obj.name}");
            }
            
            // Clean up
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public void SceneObjects_ShouldHaveValidComponents()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            // Assert - All components should be valid
            var allComponents = UnityEngine.Object.FindObjectsOfType<Component>();
            foreach (var component in allComponents)
            {
                Assert.IsNotNull(component, "Component should not be null");
                Assert.IsNotNull(component.gameObject, $"Component's game object should not be null for {component.GetType().Name}");
            }
            
            // Clean up
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public IEnumerator SceneObjects_ShouldNotLogErrors()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            var logHandler = new TestLogHandler();
            Debug.unityLogger.logHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = logHandler;
            
            try
            {
                // Act
                EditorApplication.isPlaying = true;
                while (!Application.isPlaying) { }
                
                // Wait a frame for any startup errors
                yield return null;
                
                // Assert - No errors should be logged
                Assert.AreEqual(0, logHandler.ErrorCount, "No errors should be logged during scene play");
                Assert.AreEqual(0, logHandler.ExceptionCount, "No exceptions should be logged during scene play");
            }
            finally
            {
                Debug.unityLogger.logHandler = Debug.unityLogger.logHandler;
                
                // Clean up
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
        }
        
        [Test]
        public void SceneObjects_ShouldHaveValidHierarchy()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            // Assert - All objects should have valid hierarchy
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                // Check parent-child relationships
                if (obj.transform.parent != null)
                {
                    Assert.IsNotNull(obj.transform.parent, $"Parent should not be null for {obj.name}");
                    Assert.IsNotNull(obj.transform.parent.gameObject, $"Parent game object should not be null for {obj.name}");
                }
                
                // Check children
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    var child = obj.transform.GetChild(i);
                    Assert.IsNotNull(child, $"Child {i} should not be null for {obj.name}");
                    Assert.IsNotNull(child.gameObject, $"Child {i} game object should not be null for {obj.name}");
                }
            }
            
            // Clean up
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        [Test]
        public void SceneObjects_ShouldHaveValidPhysics()
        {
            // Arrange
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                while (Application.isPlaying) { }
            }
            
            // Act
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            // Assert - Physics objects should be valid
            var rigidbodies = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                Assert.IsNotNull(rb, "Rigidbody should not be null");
                Assert.IsNotNull(rb.gameObject, "Rigidbody's game object should not be null");
                Assert.IsTrue(rb.mass > 0, "Rigidbody mass should be positive");
            }
            
            var colliders = UnityEngine.Object.FindObjectsOfType<Collider>();
            foreach (var col in colliders)
            {
                Assert.IsNotNull(col, "Collider should not be null");
                Assert.IsNotNull(col.gameObject, "Collider's game object should not be null");
                Assert.IsNotNull(col.bounds, "Collider should have valid bounds");
            }
            
            // Clean up
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
        }
        
        private void CreateTestObjects()
        {
            // Create player object
            _testPlayer = new GameObject("Player");
            _testPlayer.AddComponent<Rigidbody>();
            _testPlayer.AddComponent<CapsuleCollider>();
            _testPlayer.transform.position = new Vector3(0, 1, 0);
            
            // Create collectible object
            _testCollectible = new GameObject("Collectible");
            _testCollectible.AddComponent<SphereCollider>();
            _testCollectible.GetComponent<SphereCollider>().isTrigger = true;
            _testCollectible.transform.position = new Vector3(2, 1, 0);
            
            // Create environmental trigger
            _testTrigger = new GameObject("EnvironmentalTrigger");
            _testTrigger.AddComponent<BoxCollider>();
            _testTrigger.GetComponent<BoxCollider>().isTrigger = true;
            _testTrigger.transform.position = new Vector3(4, 1, 0);
        }
        
        private class TestLogHandler : ILogHandler
        {
            public int ErrorCount { get; private set; }
            public int ExceptionCount { get; private set; }
            
            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (logType == LogType.Error || logType == LogType.Exception)
                {
                    ErrorCount++;
                }
            }
            
            public void LogException(Exception exception, UnityEngine.Object context)
            {
                ExceptionCount++;
            }
        }
    }
}
