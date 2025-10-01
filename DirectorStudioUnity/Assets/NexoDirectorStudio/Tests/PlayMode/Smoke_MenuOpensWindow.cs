using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.Editor;

namespace NexoDirectorStudio.Tests.PlayMode
{
    /// <summary>
    /// Smoke test to verify that the Director Studio menu opens the window without exceptions.
    /// </summary>
    [TestFixture]
    public class Smoke_MenuOpensWindow
    {
        [Test]
        public void MenuOpensWindow_ShouldNotThrowException()
        {
            // Arrange & Act
            DirectorStudioWindow window = null;
            
            // Assert no exception is thrown
            Assert.DoesNotThrow(() =>
            {
                DirectorStudioWindow.ShowWindow();
                window = EditorWindow.GetWindow<DirectorStudioWindow>();
            });
            
            // Assert window is created
            Assert.IsNotNull(window, "Director Studio window should be created");
            
            // Clean up
            if (window != null)
            {
                window.Close();
            }
        }
        
        [Test]
        public void MenuOpensWindow_ShouldCreateValidWindow()
        {
            // Arrange & Act
            DirectorStudioWindow.ShowWindow();
            var window = EditorWindow.GetWindow<DirectorStudioWindow>();
            
            // Assert
            Assert.IsNotNull(window, "Window should not be null");
            Assert.IsTrue(window is DirectorStudioWindow, "Window should be of correct type");
            
            // Clean up
            window.Close();
        }
    }
}
