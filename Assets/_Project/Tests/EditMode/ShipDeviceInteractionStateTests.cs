using Bellerophon.Core.Ship;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class ShipDeviceInteractionStateTests
    {
        [Test]
        public void EngineScreen_ActivatesOverclockOnlyOncePerRun()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();

                state.ActivateDevice(ShipDeviceType.EngineRoomPowerScreen);
                state.ActivateDevice(ShipDeviceType.EngineRoomPowerScreen);

                Assert.That(state.ActivePanelMode, Is.EqualTo(ShipDevicePanelMode.EngineStatus));
                Assert.That(state.EngineOverclockUsedThisRun, Is.True);
                Assert.That(state.EngineOverclockActivationCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomCctv_CyclesInOriginalOrderWithDirections()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();

                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.CycleCctv(1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.CargoHold));

                state.CycleCctv(1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.EngineRoom));

                state.CycleCctv(1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.Armory));

                state.CycleCctv(-1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.EngineRoom));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }
    }
}
