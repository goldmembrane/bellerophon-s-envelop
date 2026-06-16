using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class CockpitConsoleDamageVisualSwitcherTests
    {
        [Test]
        public void Refresh_TogglesDestroyedConsoleOnlyAtZeroCockpitDurability()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var switcherObject = new GameObject("Cockpit Console Damage Visual Switcher Test");
            var normalConsole = new GameObject("Normal Console");
            var destroyedConsole = new GameObject("Destroyed Console");

            try
            {
                var interactionState = stateObject.AddComponent<ShipDeviceInteractionState>();
                var switcher = switcherObject.AddComponent<CockpitConsoleDamageVisualSwitcher>();
                switcher.Configure(interactionState, normalConsole, destroyedConsole);

                Assert.That(normalConsole.activeSelf, Is.True);
                Assert.That(destroyedConsole.activeSelf, Is.False);
                Assert.That(switcher.IsDestroyedVisualActive, Is.False);

                interactionState.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100)));
                switcher.Refresh();

                Assert.That(normalConsole.activeSelf, Is.False);
                Assert.That(destroyedConsole.activeSelf, Is.True);
                Assert.That(switcher.IsDestroyedVisualActive, Is.True);

                interactionState.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(100, 100)));
                switcher.Refresh();

                Assert.That(normalConsole.activeSelf, Is.True);
                Assert.That(destroyedConsole.activeSelf, Is.False);
                Assert.That(switcher.IsDestroyedVisualActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(destroyedConsole);
                Object.DestroyImmediate(normalConsole);
                Object.DestroyImmediate(switcherObject);
                Object.DestroyImmediate(stateObject);
            }
        }
    }
}
