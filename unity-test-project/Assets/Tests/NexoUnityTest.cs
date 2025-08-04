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
