using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class Phase5ShipStateModelsEditorValidation
    {
        public static void Run()
        {
            var ship = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(50, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(25, 100));
            RequireApproximately(475f / 600f, ship.AverageDurabilityPercent, "average six-room durability");

            var repairShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(90, 100))
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(50, 100))
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(0, 100));
            RequireEqual(800, ShipStateRules.CalculateRepairCost(repairShip), "repair cost");

            var cargo = new CargoState(CargoGrade.Rare, 100, 1000, 0.8f, false)
                .WithDamagePercent(0.35f);
            RequireApproximately(0.45f, cargo.DurabilityPercent, "cargo durability");
            RequireApproximately(0.55f, cargo.LossPercent, "cargo loss");

            var criticalCargoHold = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(25, 100));
            var readiness = ShipStateRules.EvaluateStartReadiness(criticalCargoHold);
            RequireApproximately(
                0.2f,
                ShipStateRules.CalculateCargoLossPercentFromCargoHold(criticalCargoHold),
                "critical cargo hold loss");
            if (readiness.CanStartTransport || !readiness.IsCargoHoldBlocked)
            {
                throw new InvalidOperationException("Phase 5 cargo hold critical state must block transport start.");
            }

            Debug.Log("Phase 5 ship state models editor validation passed.");
            Debug.Log(
                "Phase 5 ship state details: Rooms=6; RepairCost=800; CargoLoss=55%; CargoHoldCritical=Blocked");
        }

        private static void RequireEqual<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Phase 5 expected " + label + " to be " + expected + ", got " + actual + ".");
            }
        }

        private static void RequireApproximately(float expected, float actual, string label)
        {
            if (Mathf.Abs(expected - actual) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Phase 5 expected " + label + " to be " + expected + ", got " + actual + ".");
            }
        }
    }
}
