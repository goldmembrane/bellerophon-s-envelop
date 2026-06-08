using System;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep20PresentationEditorValidation
    {
        public static void Run()
        {
            Phase20PresentationBootstrap.EnsurePhase20Assets();
            Phase20PresentationEditorValidation.Run();
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 20 presentation editor validation passed.");
            Debug.Log("Detailed step 20 presentation validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var planetController = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            var audioHooks = UnityEngine.Object.FindFirstObjectByType<ShipSignalAudioHooks>();
            if (planetController == null || audioHooks == null)
            {
                throw new InvalidOperationException("Detailed step 20 requires planet stay and audio hook controllers.");
            }

            var skipped = NewGameStartFlowState.CreateReturningPlayerNewGame()
                .MoveAssociationContractToBottom()
                .AcceptAssociationContract()
                .SkipTutorialForReturningPlayer();
            var hub = PlanetStayRules.CreateHubState(skipped.Session);
            if (hub.MapMarkers.Length != 4 ||
                hub.ContractBoard.AssociationContractCount <= 0 ||
                hub.ContractBoard.PrivateContractCount <= 0 ||
                !hub.ContractBoard.BuyTabAvailable ||
                !hub.ContractBoard.SellTabAvailable)
            {
                throw new InvalidOperationException("Detailed step 20 planet hub data must expose map markers, contracts, and shop tabs.");
            }

            return "PlanetRoot=" + planetController.PlanetRoot.name +
                   "; MapMarkers=" + hub.MapMarkers.Length +
                   "; Contracts=" + hub.ContractBoard.TotalContractCount +
                   "; EngineRingSegments=" + Phase20PresentationBootstrap.EngineDonutSegmentCount +
                   "; CorridorBeacons=" + Phase20PresentationBootstrap.CorridorBeaconCount +
                   "; LastAudioCue=" + audioHooks.LastCue;
        }
    }
}
