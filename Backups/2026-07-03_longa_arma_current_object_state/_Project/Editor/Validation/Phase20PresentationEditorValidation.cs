using System;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase20PresentationEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase20PresentationBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 20 presentation validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase20PresentationBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase20PresentationBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase20PresentationBootstrap.Phase20RootName);
            var planetController = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var shopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            var audioHooks = UnityEngine.Object.FindFirstObjectByType<ShipSignalAudioHooks>();
            if (root == null ||
                planetController == null ||
                settlementController == null ||
                shopController == null ||
                audioHooks == null)
            {
                throw new InvalidOperationException("Phase 20 requires presentation root, planet stay, settlement, shop, and audio hooks.");
            }

            if (settlementController.PlanetStayController != planetController)
            {
                throw new InvalidOperationException("Phase 20 settlement continuation must point at the planet stay controller.");
            }

            AssertPlanetStayScreen(planetController);
            AssertPresentationObject(Phase20PresentationBootstrap.CockpitGlassFrameName, minChildRenderers: 4);
            AssertPresentationObject(Phase20PresentationBootstrap.EngineDonutRootName, Phase20PresentationBootstrap.EngineDonutSegmentCount);
            AssertPresentationObject(Phase20PresentationBootstrap.ControlScreenAccentName, minChildRenderers: 2);
            AssertPresentationObject(Phase20PresentationBootstrap.ArmoryTurretAccentName, minChildRenderers: 1);
            AssertPresentationObject(Phase20PresentationBootstrap.SupplyEjectionWarningName, minChildRenderers: 2);
            AssertPresentationObject(Phase20PresentationBootstrap.CargoHoldStrapsName, minChildRenderers: 2);
            AssertPresentationObject(Phase20PresentationBootstrap.CorridorBeaconRootName, Phase20PresentationBootstrap.CorridorBeaconCount);

            audioHooks.TriggerShipDamageSignal();
            audioHooks.TriggerExternalDangerSignal();
            audioHooks.TriggerIntruderSignal();
            if (audioHooks.ShipDamageSignalCount < 1 ||
                audioHooks.ExternalDangerSignalCount < 1 ||
                audioHooks.IntruderSignalCount < 1 ||
                audioHooks.LastCue != ShipSignalAudioCue.IntruderSignal)
            {
                throw new InvalidOperationException("Phase 20 audio placeholder hooks must expose damage, external danger, and intruder signal cues.");
            }

            Debug.Log("Phase 20 presentation polish editor validation passed.");
            Debug.Log("Phase 20 presentation polish details: PlanetHub=True; RoomMarkers=6; CorridorBeacons=" +
                      Phase20PresentationBootstrap.CorridorBeaconCount +
                      "; AudioHooks=Damage/External/Intruder");
        }

        private static void AssertPlanetStayScreen(PlanetStayController controller)
        {
            if (controller.PlanetRoot == null ||
                controller.PlanetRoot.name != Phase10PlanetMaintenanceBootstrap.PlanetStayRootName ||
                controller.TitleText == null ||
                controller.BodyText == null ||
                controller.StatusText == null ||
                controller.RepairShopButton == null ||
                controller.ContractOfficeButton == null ||
                controller.ShopButton == null ||
                controller.CargoDepotButton == null ||
                controller.ShipButton == null)
            {
                throw new InvalidOperationException("Phase 20 planet stay screen is not fully wired.");
            }

            var rectTransform = controller.PlanetRoot.GetComponent<RectTransform>();
            if (rectTransform == null ||
                rectTransform.anchorMin != Vector2.zero ||
                rectTransform.anchorMax != Vector2.one ||
                rectTransform.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 20 planet stay screen must cover the full screen.");
            }

            var background = controller.PlanetRoot.GetComponent<Image>();
            if (background == null || background.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 20 planet stay screen background must be opaque.");
            }

            var hub = PlanetStayRules.CreateHubState(
                GameSessionState.StartAssociationSession().GrantTutorialSkipReward(NewGameStartFlowState.TutorialSkipRewardCredits));
            if (hub.MapMarkers.Length != 4 ||
                !hub.CanOpenRepairShop ||
                !hub.CanOpenContractOffice ||
                !hub.CanOpenShop ||
                !hub.CanOpenPersonalCargoDepot ||
                !hub.CanOpenShip)
            {
                throw new InvalidOperationException("Phase 20 planet stay rules must expose all five hub entry points and four map markers.");
            }
        }

        private static void AssertPresentationObject(string objectName, int minChildRenderers)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing Phase 20 presentation object: " + objectName);
            }

            var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length < minChildRenderers)
            {
                throw new InvalidOperationException(
                    objectName + " must contain at least " + minChildRenderers + " renderers.");
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sharedMaterial == null)
                {
                    throw new InvalidOperationException(objectName + " renderer is missing a material.");
                }
            }
        }
    }
}
