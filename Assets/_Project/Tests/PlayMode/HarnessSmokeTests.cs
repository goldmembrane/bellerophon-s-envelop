using System.Collections;
using Bellerophon.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Bellerophon.Tests.PlayMode
{
    public sealed class HarnessSmokeTests
    {
        [UnityTest]
        public IEnumerator RuntimeObject_CanExistForOneFrame()
        {
            var gameObject = new GameObject("Harness Smoke Object");
            gameObject.AddComponent<HarnessSmokeMarker>();

            yield return null;

            Assert.That(Object.FindFirstObjectByType<HarnessSmokeMarker>(), Is.Not.Null);
            Object.Destroy(gameObject);
        }
    }
}

