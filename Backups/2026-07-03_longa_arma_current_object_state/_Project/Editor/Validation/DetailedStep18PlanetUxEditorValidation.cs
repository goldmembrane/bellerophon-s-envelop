using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep18PlanetUxEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 18 planet UX editor validation passed.");
            Debug.Log("Detailed step 18 planet UX validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var prompt = NewGameStartFlowState.CreateNewGame();
            var auto = prompt.TickAssociationContractScroll(60f);
            var fast = prompt.TickAssociationContractDownArrowFastMove(3f);
            if (!auto.CanAcceptAssociationContract || !fast.CanAcceptAssociationContract)
            {
                throw new InvalidOperationException("Association contract scroll must reach the bottom by 60 second auto-scroll or 3 second Down fast-move.");
            }

            var no = fast.RejectAssociationContract();
            if (!no.Blocked || no.Summary != "이미 잠정적으로 동의한 상태입니다")
            {
                throw new InvalidOperationException("Association No button must be blocked after tentative consent.");
            }

            var privateRoute = prompt
                .TickAssociationContractScroll(5f)
                .StopAssociationContractScroll()
                .StartPrivateBusinessRouteFromStoppedContract();
            if (!privateRoute.Succeeded ||
                privateRoute.State.Phase != NewGameStartFlowPhase.PrivateBusinessPlanet ||
                privateRoute.State.Session.IsAssociationMember)
            {
                throw new InvalidOperationException("Hidden private business route must require Ctrl+C stop before Ctrl+X cancel.");
            }

            var skipped = NewGameStartFlowState.CreateReturningPlayerNewGame()
                .MoveAssociationContractToBottom()
                .AcceptAssociationContract()
                .SkipTutorialForReturningPlayer();
            if (!skipped.TutorialSkipped ||
                skipped.Session.Phase != GameSessionPhase.Completed ||
                skipped.Session.Wallet.Credits != NewGameStartFlowState.TutorialSkipRewardCredits ||
                skipped.AvailableContractCount < 2)
            {
                throw new InvalidOperationException("Returning player tutorial skip must grant $1100 and expose post-tutorial contracts.");
            }

            var hub = PlanetStayRules.CreateHubState(skipped.Session);
            if (hub.MapMarkers.Length != 4 ||
                !hub.CanOpenRepairShop ||
                !hub.CanOpenContractOffice ||
                !hub.CanOpenShop ||
                !hub.CanOpenPersonalCargoDepot ||
                !hub.CanOpenShip ||
                hub.ContractBoard.AssociationContractCount <= 0 ||
                hub.ContractBoard.PrivateContractCount <= 0 ||
                !hub.ContractBoard.BuyTabAvailable ||
                !hub.ContractBoard.SellTabAvailable)
            {
                throw new InvalidOperationException("Planet stay hub must expose map facilities, contract categories, and shop buy/sell tabs.");
            }

            var specialSession = CreatePresenceDetectorOfferSession();
            var specialHub = PlanetStayRules.CreateHubState(specialSession);
            var specialAccepted = specialSession.AcceptSpecialContract(SpecialContractKind.PresenceDetectorUnlock);
            if (specialHub.ContractBoard.SpecialContractCount != 1 ||
                !specialAccepted.Accepted ||
                specialAccepted.State.SpecialContracts.ActiveContractKind != SpecialContractKind.PresenceDetectorUnlock)
            {
                throw new InvalidOperationException("Planet stay special contract offer must connect to the session active special contract state.");
            }

            return "MapMarkers=" + hub.MapMarkers.Length +
                   "; AssociationContracts=" + hub.ContractBoard.AssociationContractCount +
                   "; PrivateContracts=" + hub.ContractBoard.PrivateContractCount +
                   "; SpecialOffers=" + specialHub.ContractBoard.SpecialContractCount +
                   "; SkipCredits=" + skipped.Session.Wallet.Credits +
                   "; PrivateRoute=" + privateRoute.State.Phase;
        }

        private static GameSessionState CreatePresenceDetectorOfferSession()
        {
            var contract = new TransportContractDefinition(
                "detailed-step18-organic-arrival",
                "Detailed Step 18 Organic Arrival",
                "Organic Rich Planet",
                ContractType.Association,
                ContractDifficulty.VeryEasy,
                60,
                0,
                new CargoState(CargoGrade.Common, 1, 0, 1f, false),
                false,
                destinationTrait: PlanetTrait.OrganicRich);
            var started = GameSessionState.StartAssociationSession().StartTransport(contract);
            return started
                .CompleteTransport(new SettlementInput(
                    contract.ContractType,
                    contract.Difficulty,
                    contract.Cargo,
                    started.Ship,
                    new CrewState(1, 0),
                    started.Wallet,
                    contractBasePay: contract.RewardCredits))
                .WithReputation(new ReputationState(SpecialContractRules.PresenceDetectorRequiredFame, 0, false));
        }
    }
}
