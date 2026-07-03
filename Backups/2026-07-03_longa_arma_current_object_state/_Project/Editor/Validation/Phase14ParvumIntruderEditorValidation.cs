using System;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase14ParvumIntruderEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase14ParvumIntruderBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 14 parvum intruder validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase14ParvumIntruderBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase14ParvumIntruderBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase14ParvumIntruderBootstrap.Phase14RootName);
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var intruderView = UnityEngine.Object.FindFirstObjectByType<SeedIntruderVisualView>();
            if (root == null || deviceState == null || deviceHud == null)
            {
                throw new InvalidOperationException("Phase 14 parvum intruder scene wiring is incomplete.");
            }

            if (intruderView == null ||
                intruderView.ParvumVisualRoot == null ||
                !intruderView.HasAllRoomAnchorsForValidation)
            {
                throw new InvalidOperationException("Phase 14 parvum visual view must be wired to a world placeholder and every room anchor.");
            }

            intruderView.RefreshView();
            if (intruderView.IsViewActive)
            {
                throw new InvalidOperationException("Phase 14 parvum visual placeholder must stay hidden until an active Parvum exists.");
            }

            var summary = BuildValidationSummary();
            Debug.Log("Phase 14 parvum intruder validation passed.");
            Debug.Log("Phase 14 parvum intruder validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var tutorialSession = GameSessionState.StartAssociationSession()
                .StartTransport(TransportContractDefinition.CreateTutorial());
            var followUpSession = CreateFollowUpTransportSession();
            if (SeedIntruderRules.CanCheckSeedIntruder(tutorialSession) ||
                !SeedIntruderRules.CanCheckSeedIntruder(followUpSession))
            {
                throw new InvalidOperationException("Phase 14 seed intruder checks must exclude tutorial and allow post-tutorial transports.");
            }

            var checkIndex = FindTriggeringCheck(followUpSession);
            var state = SeedIntruderRules.CreateParvumIntrusion(followUpSession, checkIndex);
            var firstTick = SeedIntruderRules.TickParvum(
                state,
                ShipState.CreateDefault(),
                followUpSession.ActiveCargo.Value,
                SeedIntruderRules.ParvumAttackDelaySeconds);

            if (state.Kind != SeedIntruderKind.Parvum ||
                state.Definition.MaxHealth != SeedIntruderRules.ParvumHealth ||
                state.Definition.MovementSpeed != SeedIntruderRules.ParvumMovementSpeed ||
                state.Definition.AttackRange != SeedIntruderRules.ParvumAttackRange ||
                state.Definition.AttackDelaySeconds != SeedIntruderRules.ParvumAttackDelaySeconds ||
                firstTick.RoomDamageApplied != SeedIntruderRules.ParvumShipFacilityDamage ||
                ShipStateRules.CalculateRepairCost(firstTick.Ship) <= 0)
            {
                throw new InvalidOperationException("Phase 14 parvum stats or ship damage rules are not configured correctly.");
            }

            var neutralized = SeedIntruderRules.ApplyDamage(state, SeedIntruderRules.ParvumHealth);
            if (!neutralized.IsResolved || neutralized.Intruder.Resolution != IntruderResolution.Neutralized)
            {
                throw new InvalidOperationException("Phase 14 parvum must support neutralized resolution.");
            }

            return $"CheckInterval={SeedIntruderRules.OccurrenceCheckIntervalSeconds:0.0}; Chance={SeedIntruderRules.OccurrencePercent}%; TriggerCheck={checkIndex}; Target={state.TargetRoom}; Damage={firstTick.RoomDamageApplied}; RepairCost={ShipStateRules.CalculateRepairCost(firstTick.Ship)}; Visual=World";
        }

        private static int FindTriggeringCheck(GameSessionState session)
        {
            for (var checkIndex = 1; checkIndex <= 200; checkIndex++)
            {
                if (SeedIntruderRules.ShouldStartSeedIntruder(session, checkIndex))
                {
                    return checkIndex;
                }
            }

            throw new InvalidOperationException("Phase 14 could not find a deterministic seed intruder trigger check.");
        }

        private static GameSessionState CreateFollowUpTransportSession()
        {
            var tutorialContract = TransportContractDefinition.CreateTutorial();
            var tutorialSession = GameSessionState.StartAssociationSession().StartTransport(tutorialContract);
            var completedSession = tutorialSession.CompleteTransport(new SettlementInput(
                tutorialContract.ContractType,
                tutorialContract.Difficulty,
                tutorialContract.Cargo,
                tutorialSession.Ship,
                new CrewState(1, 0),
                tutorialSession.Wallet,
                contractBasePay: tutorialContract.RewardCredits,
                repairSupportAmount: 100));

            return completedSession.StartTransport(TransportContractDefinition.CreateAssociationFollowUp());
        }
    }
}
