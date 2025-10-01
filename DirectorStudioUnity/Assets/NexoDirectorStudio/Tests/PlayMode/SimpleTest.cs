using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace NexoDirectorStudio.Tests.PlayMode
{
    /// <summary>
    /// Simple test to verify PlayMode test framework is working
    /// </summary>
    public class SimpleTest
    {
        [UnityTest]
        public IEnumerator SimpleTest_Passes()
        {
            Debug.Log("✅ Simple test is running!");
            yield return null;
            Assert.IsTrue(true, "Simple test should pass");
        }
        
        [Test]
        public void SimpleTest_EditMode()
        {
            Debug.Log("✅ Simple EditMode test is running!");
            Assert.IsTrue(true, "Simple EditMode test should pass");
        }
    }
}
