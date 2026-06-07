using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class Phase1SessionModelsEditorValidation
    {
        public static void Run()
        {
            var wallet = new WalletState(50, false);
            var ready = GameSessionState.StartSession(wallet);
            RequireEqual(GameSessionPhase.Ready, ready.Phase, "new session phase");
            RequireEqual(ShipRunState.Docked, ready.Ship.RunState, "new session ship run state");

            var transporting = ready.StartTransport();
            RequireEqual(GameSessionPhase.Transporting, transporting.Phase, "transporting phase");
            RequireEqual(ShipRunState.InTransit, transporting.Ship.RunState, "in-transit ship state");

            var run = TransportRunState.Start(60, ShipState.CreateDefault()).Tick(15f);
            RequireApproximately(0.25f, run.ProgressPercent, "transport progress after 15 seconds");
            RequireApproximately(45f, run.RemainingSeconds, "transport remaining seconds after 15 seconds");

            var completed = transporting.CompleteTransport(CreateSettlementInput(wallet, ShipState.CreateDefault()));
            RequireEqual(GameSessionPhase.Completed, completed.Phase, "completed phase");
            RequireEqual(ShipRunState.Completed, completed.Ship.RunState, "completed ship state");
            RequireEqual(1, completed.CompletedTransportCount, "completed transport count");
            RequireEqual(150, completed.Wallet.Credits, "wallet after completed transport");

            var failed = GameSessionState.StartSession(wallet)
                .StartTransport()
                .FailTransport(CreateSettlementInput(wallet, ShipState.CreateDefault(), cargoLossPenalty: 25));
            RequireEqual(GameSessionPhase.Failed, failed.Phase, "failed phase");
            RequireEqual(ShipRunState.Failed, failed.Ship.RunState, "failed ship state");

            Debug.Log("Phase 1 session models editor validation passed.");
            Debug.Log(
                "Phase 1 session model details: Ready=OK; Transport=OK; Complete=OK; Fail=OK; Progress=25%");
        }

        private static SettlementInput CreateSettlementInput(
            WalletState wallet,
            ShipState ship,
            int cargoLossPenalty = 0)
        {
            return new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Normal,
                new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                ship,
                new CrewState(1, 0),
                wallet,
                cargoLossPenalty: cargoLossPenalty);
        }

        private static void RequireEqual<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    "Phase 1 expected " + label + " to be " + expected + ", got " + actual + ".");
            }
        }

        private static void RequireApproximately(float expected, float actual, string label)
        {
            if (Mathf.Abs(expected - actual) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Phase 1 expected " + label + " to be " + expected + ", got " + actual + ".");
            }
        }
    }
}
