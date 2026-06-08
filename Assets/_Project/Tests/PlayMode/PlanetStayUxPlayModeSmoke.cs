using System.Collections;
using Bellerophon.Core.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Bellerophon.Tests.PlayMode
{
    public sealed class PlanetStayUxPlayModeSmoke
    {
        [UnityTest]
        public IEnumerator StartFlowButtons_BlockNoAndAllowReturningPlayerTutorialSkip()
        {
            var root = new GameObject("Planet Stay UX Smoke");
            try
            {
                var controller = root.AddComponent<NewGameStartFlowController>();
                var title = CreateText(root, "Title");
                var body = CreateText(root, "Body");
                var status = CreateText(root, "Status");
                var yes = CreateButton(root, "Yes");
                var no = CreateButton(root, "No");
                var tutorial = CreateButton(root, "Tutorial");
                var skip = CreateButton(root, "Skip");

                controller.Configure(title, body, status, yes, tutorial, null, null, no, skip);
                controller.SetTutorialCompletedBefore(true);
                yield return null;

                Assert.That(yes.interactable, Is.False);
                Assert.That(no.interactable, Is.False);

                controller.FastForwardAssociationContractForValidation();
                Assert.That(yes.interactable, Is.True);
                Assert.That(no.interactable, Is.True);

                controller.RejectAssociationContract();
                Assert.That(controller.FlowState.Phase, Is.EqualTo(NewGameStartFlowPhase.ContractPrompt));
                Assert.That(status.text, Does.Contain("이미 잠정적으로 동의한 상태입니다"));

                controller.AcceptAssociationContract();
                Assert.That(controller.FlowState.Phase, Is.EqualTo(NewGameStartFlowPhase.AssociationPlanet));
                Assert.That(skip.interactable, Is.True);

                controller.SkipTutorialForReturningPlayer();
                Assert.That(controller.CurrentSession.Phase, Is.EqualTo(GameSessionPhase.Completed));
                Assert.That(controller.CurrentSession.Wallet.Credits, Is.EqualTo(NewGameStartFlowState.TutorialSkipRewardCredits));
                Assert.That(controller.AvailableContractCount, Is.EqualTo(2));
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator ReturningPlayerTutorialSkip_ClosesStartUiAndOpensPlanetHub()
        {
            var startRoot = new GameObject("Planet Stay Skip Start Smoke");
            var planetHost = new GameObject("Planet Stay Skip Hub Smoke");
            try
            {
                var controller = startRoot.AddComponent<NewGameStartFlowController>();
                var title = CreateText(startRoot, "Title");
                var body = CreateText(startRoot, "Body");
                var status = CreateText(startRoot, "Status");
                var yes = CreateButton(startRoot, "Yes");
                var no = CreateButton(startRoot, "No");
                var tutorial = CreateButton(startRoot, "Tutorial");
                var skip = CreateButton(startRoot, "Skip");

                var planetRoot = new GameObject("Planet Root");
                planetRoot.transform.SetParent(planetHost.transform, false);
                var planetTitle = CreateText(planetRoot, "Planet Title");
                var planetBody = CreateText(planetRoot, "Planet Body");
                var planetStatus = CreateText(planetRoot, "Planet Status");
                var repair = CreateButton(planetRoot, "Repair");
                var contracts = CreateButton(planetRoot, "Contracts");
                var shop = CreateButton(planetRoot, "Shop");
                var cargo = CreateButton(planetRoot, "Cargo");
                var ship = CreateButton(planetRoot, "Ship");
                var planetController = planetHost.AddComponent<PlanetStayController>();

                controller.Configure(title, body, status, yes, tutorial, null, null, no, skip);
                planetController.Configure(
                    controller,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    planetRoot,
                    planetTitle,
                    planetBody,
                    planetStatus,
                    repair,
                    contracts,
                    shop,
                    cargo,
                    ship);
                controller.SetTutorialCompletedBefore(true);
                controller.FastForwardAssociationContractForValidation();
                controller.AcceptAssociationContract();
                yield return null;

                controller.SkipTutorialForReturningPlayer();
                yield return null;

                Assert.That(controller.CurrentSession.Phase, Is.EqualTo(GameSessionPhase.Completed));
                Assert.That(controller.CurrentSession.Wallet.Credits, Is.EqualTo(NewGameStartFlowState.TutorialSkipRewardCredits));
                Assert.That(startRoot.activeSelf, Is.False);
                Assert.That(planetController.IsPlanetVisible, Is.True);
                Assert.That(planetBody.text, Does.Contain("Surface map"));
                Assert.That(planetBody.text, Does.Contain("Repair Shop"));
                Assert.That(planetStatus.text, Does.Contain("Ready"));
            }
            finally
            {
                Object.Destroy(startRoot);
                Object.Destroy(planetHost);
            }
        }

        private static Text CreateText(GameObject root, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            var text = child.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static Button CreateButton(GameObject root, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<Button>();
        }
    }
}
