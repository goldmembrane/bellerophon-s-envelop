using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class ShipInteriorHudRulesTests
    {
        [Test]
        public void ShipInteriorMapRules_ResolveCurrentRoomFromGrayboxPosition()
        {
            Assert.That(
                ShipInteriorMapRules.FindCurrentRoom(new Vector3(0f, -3f, -5f)),
                Is.EqualTo(ShipRoomId.CargoHold));
            Assert.That(
                ShipInteriorMapRules.FindCurrentRoom(new Vector3(0f, 0f, 18f)),
                Is.EqualTo(ShipRoomId.Cockpit));
            Assert.That(
                ShipInteriorMapRules.FindCurrentRoom(new Vector3(-14f, 0f, -14f)),
                Is.EqualTo(ShipRoomId.Armory));
            Assert.That(ShipInteriorMapRules.ShipInteriorMapScale, Is.EqualTo(0.8f));
        }

        [Test]
        public void ShipSignalAudioHooks_RecordDamageDangerAndIntruderSignals()
        {
            var hookObject = new GameObject("Signal Hooks Test");
            try
            {
                var hooks = hookObject.AddComponent<ShipSignalAudioHooks>();

                hooks.TriggerShipDamageSignal();
                hooks.TriggerExternalDangerSignal();
                hooks.TriggerIntruderSignal();

                Assert.That(hooks.ShipDamageSignalCount, Is.EqualTo(1));
                Assert.That(hooks.ExternalDangerSignalCount, Is.EqualTo(1));
                Assert.That(hooks.IntruderSignalCount, Is.EqualTo(1));
                Assert.That(hooks.LastCue, Is.EqualTo(ShipSignalAudioCue.IntruderSignal));
            }
            finally
            {
                Object.DestroyImmediate(hookObject);
            }
        }
    }
}
