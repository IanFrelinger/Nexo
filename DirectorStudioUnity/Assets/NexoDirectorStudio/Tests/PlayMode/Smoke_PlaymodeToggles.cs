using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace NexoDirectorStudio.Tests.PlayMode
{
    /// <summary>
    /// Smoke test to verify that entering/exiting play mode doesn't cause issues
    /// with Director Studio components.
    /// </summary>
    [TestFixture]
    public class Smoke_PlaymodeToggles
    {
        [Test]
        public void EnterPlayMode_ShouldNotThrowException()
        {
            // Arrange - Ensure we start in edit mode
            if (Application.isPlaying)
            {
                EditorApplication.isPlaying = false;
                // Wait for play mode to exit
                while (Application.isPlaying) { }
            }
            
            // Act & Assert - Enter play mode should not throw
            Assert.DoesNotThrow(() =>
            {
                EditorApplication.isPlaying = true;
            });
            
            // Clean up - Exit play mode
            EditorApplication.isPlaying = false;
        }
        
        [Test]
        public void ExitPlayMode_ShouldNotThrowException()
        {
            // Arrange - Enter play mode first
            EditorApplication.isPlaying = true;
            // Wait for play mode to start
            while (!Application.isPlaying) { }
            
            // Act & Assert - Exit play mode should not throw
            Assert.DoesNotThrow(() =>
            {
                EditorApplication.isPlaying = false;
            });
        }
        
        [Test]
        public void PlayModeToggle_ShouldNotLeaveLingeringState()
        {
            // Arrange - Start in edit mode
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
            
            // Act - Toggle play mode
            EditorApplication.isPlaying = true;
            while (!Application.isPlaying) { }
            
            EditorApplication.isPlaying = false;
            while (Application.isPlaying) { }
            
            // Assert - Should be back in edit mode
            Assert.IsFalse(Application.isPlaying, "Should be back in edit mode");
        }
    }
}
