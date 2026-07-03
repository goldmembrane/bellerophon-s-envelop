using System;
using System.Collections.Generic;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ModelingInspectionModeBootstrap
    {
        public const string FreeCameraRootName = "Model Cam";

        private static readonly string[] TutorialUiRootNames =
        {
            Phase7NewGameStartBootstrap.Phase7RootName,
            Phase9SettlementGameOverBootstrap.Phase9RootName,
            Phase9SettlementGameOverBootstrap.SettlementRootName,
            Phase9SettlementGameOverBootstrap.GameOverRootName,
            Phase10PlanetMaintenanceBootstrap.Phase10RootName,
            Phase10PlanetMaintenanceBootstrap.PlanetStayRootName,
            Phase10PlanetMaintenanceBootstrap.MaintenanceRootName,
            Phase10PlanetMaintenanceBootstrap.ContractBoardRootName,
            Phase10PlanetMaintenanceBootstrap.PersonalCargoRootName,
            Phase10PlanetMaintenanceBootstrap.ShipUpgradeRootName
        };

        [MenuItem("Bellerophon/Bootstrap/Disable Tutorial Logic For Modeling Inspection")]
        public static void DisableTutorialLogicForModeling()
        {
            var scene = OpenCargoRunScene();

            var deactivatedUiRoots = DisableTutorialUiRoots();
            var disabledControllers = DisableTutorialControllers();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log(
                "Tutorial logic disabled for modeling inspection. DeactivatedUiRoots=" +
                deactivatedUiRoots +
                "; DisabledControllers=" +
                disabledControllers);
        }

        [MenuItem("Bellerophon/Bootstrap/Enable Modeling Inspection Free Camera")]
        public static void EnableFreeCameraForModeling()
        {
            var scene = OpenCargoRunScene();
            var deactivatedUiRoots = DisableTutorialUiRoots();
            var disabledControllers = DisableTutorialControllers();
            var details = ApplyFreeCameraForModeling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            ValidateFreeCamera();
            Debug.Log(
                "Modeling inspection free camera enabled. DeactivatedUiRoots=" +
                deactivatedUiRoots +
                "; DisabledControllers=" +
                disabledControllers +
                "; " +
                details);
        }

        [MenuItem("Bellerophon/Bootstrap/Restore Gameplay Mode After Modeling Inspection")]
        public static void RestoreGameplayModeAfterInspection()
        {
            var scene = OpenCargoRunScene();

            var disabledInspectionCamera = DisableInspectionCamera();
            var enabledPlayerObjects = EnablePlayerObjects();
            var enabledPlayerCamera = EnablePlayerCamera();
            var restoredHudRoots = ShowFirstPersonHudRoots();
            var bakedAreaLights = BakeUnsupportedAreaLightsForUrp();
            var disabledPunctualShadows = DisableAdditionalPunctualLightShadows();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateGameplayModeRestored();
            Debug.Log(
                "Gameplay mode restored after modeling inspection. DisabledInspectionCamera=" +
                disabledInspectionCamera +
                "; EnabledPlayerObjects=" +
                enabledPlayerObjects +
                "; EnabledPlayerCamera=" +
                enabledPlayerCamera +
                "; RestoredHudRoots=" +
                restoredHudRoots +
                "; BakedAreaLights=" +
                bakedAreaLights +
                "; DisabledPunctualShadows=" +
                disabledPunctualShadows);
        }

        public static string ApplyFreeCameraForModeling()
        {
            var root = FindOrCreateRoot(FreeCameraRootName);
            root.SetActive(true);

            var camera = root.GetComponent<Camera>();
            if (camera == null)
            {
                camera = root.AddComponent<Camera>();
            }

            var audioListener = root.GetComponent<AudioListener>();
            if (audioListener == null)
            {
                audioListener = root.AddComponent<AudioListener>();
            }

            var controller = root.GetComponent<ModelingInspectionFreeCamera>();
            if (controller == null)
            {
                controller = root.AddComponent<ModelingInspectionFreeCamera>();
            }

            camera.enabled = true;
            camera.fieldOfView = 70f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.007f, 0.008f, 1f);
            root.tag = "MainCamera";

            var view = ResolveInspectionView();
            controller.Configure(camera, 6f, 3f, 0.25f, 0.08f, 0.5f, 40f, true);
            controller.ResetView(view.Position, view.LookAt);

            var disabledOtherCameras = DisableOtherCameras(camera);
            var disabledOtherListeners = DisableOtherAudioListeners(audioListener);
            var hiddenEquipmentVisuals = HideEquipmentVisuals();
            var disabledPlayerViewComponents = DisablePlayerViewComponents();
            var hiddenHudRoots = HideFirstPersonHudRoots();

            return "MainCamera=" +
                   FreeCameraRootName +
                   "; DisabledOtherCameras=" +
                   disabledOtherCameras +
                   "; DisabledOtherAudioListeners=" +
                   disabledOtherListeners +
                   "; DisabledPlayerViewComponents=" +
                   disabledPlayerViewComponents +
                   "; HiddenHudRoots=" +
                   hiddenHudRoots +
                   "; HiddenEquipmentVisuals=" +
                   hiddenEquipmentVisuals;
        }

        public static void ValidateScene()
        {
            OpenCargoRunScene();

            var activeUiRoots = 0;
            for (var i = 0; i < TutorialUiRootNames.Length; i++)
            {
                activeUiRoots += CountActiveNamedObjects(TutorialUiRootNames[i]);
            }

            if (activeUiRoots != 0)
            {
                throw new InvalidOperationException("Tutorial UI roots must be inactive for modeling inspection. ActiveUiRoots=" + activeUiRoots);
            }

            var enabledControllers =
                CountEnabledComponents<NewGameStartFlowController>() +
                CountEnabledComponents<TransportSettlementController>() +
                CountEnabledComponents<PlanetStayController>() +
                CountEnabledComponents<PlanetMaintenanceController>() +
                CountEnabledComponents<ContractBoardController>() +
                CountEnabledComponents<EquipmentShopController>() +
                CountEnabledComponents<PersonalCargoController>() +
                CountEnabledComponents<ShipUpgradeController>();

            if (enabledControllers != 0)
            {
                throw new InvalidOperationException("Tutorial/session UI controllers must be disabled for modeling inspection. EnabledControllers=" + enabledControllers);
            }

            Debug.Log("Modeling inspection mode validation passed. ActiveTutorialUiRoots=0; EnabledTutorialControllers=0");
        }

        public static void ValidateFreeCamera()
        {
            OpenCargoRunScene();

            var controller = FindActiveFreeCameraController();
            if (controller == null)
            {
                throw new InvalidOperationException("Modeling inspection free camera controller is missing or inactive.");
            }

            var camera = controller.TargetCamera != null
                ? controller.TargetCamera
                : controller.GetComponent<Camera>();
            if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
            {
                throw new InvalidOperationException("Modeling inspection camera must be active and enabled.");
            }

            if (!camera.CompareTag("MainCamera"))
            {
                throw new InvalidOperationException("Modeling inspection camera must be tagged MainCamera.");
            }

            if (Camera.main != camera)
            {
                throw new InvalidOperationException("Camera.main must resolve to the modeling inspection camera.");
            }

            var otherEnabledCameras = CountOtherEnabledCameras(camera);
            if (otherEnabledCameras != 0)
            {
                throw new InvalidOperationException("Only the modeling inspection camera may render during inspection. OtherEnabledCameras=" + otherEnabledCameras);
            }

            var activePlayerViewComponents = CountActivePlayerViewComponents();
            if (activePlayerViewComponents != 0)
            {
                throw new InvalidOperationException("Player view/input components must be disabled during modeling inspection. ActivePlayerViewComponents=" + activePlayerViewComponents);
            }

            var activeHudRoots = CountActiveFirstPersonHudRoots();
            if (activeHudRoots != 0)
            {
                throw new InvalidOperationException("First-person HUD roots must be hidden during modeling inspection. ActiveHudRoots=" + activeHudRoots);
            }

            Debug.Log(
                "Modeling inspection free camera validation passed. MainCamera=" +
                camera.gameObject.name +
                "; OtherEnabledCameras=0; ActivePlayerViewComponents=0; ActiveHudRoots=0");
        }

        private static void ValidateGameplayModeRestored()
        {
            var playerMotor = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var interaction = UnityEngine.Object.FindFirstObjectByType<FirstPersonInteractionController>();
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var camera = Camera.main;
            if (playerMotor == null ||
                playerInput == null ||
                interaction == null ||
                hud == null ||
                camera == null ||
                !camera.isActiveAndEnabled)
            {
                throw new InvalidOperationException("Gameplay mode restore failed to reactivate player, HUD, interaction, and player camera.");
            }

            var inspectionCamera = FindNamedObject(FreeCameraRootName);
            if (inspectionCamera != null && inspectionCamera.activeInHierarchy)
            {
                throw new InvalidOperationException("Modeling inspection camera must be inactive after gameplay restore.");
            }
        }

        private static Scene OpenCargoRunScene()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene: " + Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            }

            if (SceneManager.GetActiveScene().path != Phase4CargoShipGrayboxBootstrap.CargoRunScenePath)
            {
                return EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            return SceneManager.GetActiveScene();
        }

        private static int DisableTutorialUiRoots()
        {
            var deactivatedUiRoots = 0;
            for (var i = 0; i < TutorialUiRootNames.Length; i++)
            {
                deactivatedUiRoots += SetNamedObjectsActive(TutorialUiRootNames[i], false);
            }

            return deactivatedUiRoots;
        }

        private static int DisableTutorialControllers()
        {
            return DisableComponents<NewGameStartFlowController>() +
                   DisableComponents<TransportSettlementController>() +
                   DisableComponents<PlanetStayController>() +
                   DisableComponents<PlanetMaintenanceController>() +
                   DisableComponents<ContractBoardController>() +
                   DisableComponents<EquipmentShopController>() +
                   DisableComponents<PersonalCargoController>() +
                   DisableComponents<ShipUpgradeController>();
        }

        private static GameObject FindOrCreateRoot(string objectName)
        {
            var existing = FindNamedObject(objectName);
            if (existing != null)
            {
                existing.transform.SetParent(null);
                return existing;
            }

            return new GameObject(objectName);
        }

        private static GameObject FindNamedObject(string objectName)
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == objectName)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static InspectionView ResolveInspectionView()
        {
            var cockpit = FindNamedObject(ApprovedCockpitStructureBootstrap.RootName);
            if (cockpit == null || !TryCollectRendererBounds(cockpit.transform, out var bounds))
            {
                var fallbackLookAt = new Vector3(0f, 1.25f, 18f);
                return new InspectionView(new Vector3(0f, 2.2f, 10.5f), fallbackLookAt);
            }

            var lookAt = bounds.center + new Vector3(0f, 0.15f, 0f);
            var rearDistance = Mathf.Max(6f, bounds.extents.z * 1.75f);
            var height = Mathf.Max(bounds.min.y + 2.1f, bounds.center.y + 0.45f);
            var position = new Vector3(bounds.center.x, height, bounds.center.z - rearDistance);
            return new InspectionView(position, lookAt);
        }

        private static bool TryCollectRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = new Bounds(root.position, Vector3.zero);
            var hasBounds = false;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static int DisableOtherCameras(Camera inspectionCamera)
        {
            var disabled = 0;
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || camera == inspectionCamera)
                {
                    continue;
                }

                if (camera.CompareTag("MainCamera"))
                {
                    camera.gameObject.tag = "Untagged";
                }

                if (camera.enabled)
                {
                    camera.enabled = false;
                    disabled++;
                }
            }

            return disabled;
        }

        private static int DisableOtherAudioListeners(AudioListener inspectionListener)
        {
            var disabled = 0;
            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null || listener == inspectionListener)
                {
                    continue;
                }

                if (listener.enabled)
                {
                    listener.enabled = false;
                    disabled++;
                }
            }

            inspectionListener.enabled = true;
            return disabled;
        }

        private static int DisablePlayerViewComponents()
        {
            return DisableActiveComponents<FirstPersonPlayerInput>() +
                   DisableActiveComponents<FirstPersonPlayerMotor>() +
                   DisableActiveComponents<FirstPersonInteractionController>() +
                   DisableActiveComponents<FirstPersonHandInventory>() +
                   DisableActiveComponents<PlayerEquipmentController>() +
                   DisableActiveComponents<FirstPersonEquipmentVisualController>();
        }

        private static int DisableInspectionCamera()
        {
            var changed = 0;
            var root = FindNamedObject(FreeCameraRootName);
            if (root == null)
            {
                return changed;
            }

            if (root.CompareTag("MainCamera"))
            {
                root.tag = "Untagged";
                changed++;
            }

            changed += DisableComponentsOnObject<Camera>(root);
            changed += DisableComponentsOnObject<AudioListener>(root);
            changed += DisableComponentsOnObject<ModelingInspectionFreeCamera>(root);
            if (root.activeSelf)
            {
                root.SetActive(false);
                changed++;
            }

            return changed;
        }

        private static int EnablePlayerObjects()
        {
            return EnableComponentsWithOwners<FirstPersonPlayerInput>() +
                   EnableComponentsWithOwners<FirstPersonPlayerMotor>() +
                   EnableComponentsWithOwners<FirstPersonInteractionController>() +
                   EnableComponentsWithOwners<FirstPersonHandInventory>() +
                   EnableComponentsWithOwners<PlayerEquipmentController>() +
                   EnableComponentsWithOwners<FirstPersonEquipmentVisualController>();
        }

        private static int EnablePlayerCamera()
        {
            var changed = 0;
            var motors = UnityEngine.Object.FindObjectsByType<FirstPersonPlayerMotor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < motors.Length; i++)
            {
                var motor = motors[i];
                if (motor == null)
                {
                    continue;
                }

                var cameras = motor.GetComponentsInChildren<Camera>(true);
                for (var j = 0; j < cameras.Length; j++)
                {
                    var camera = cameras[j];
                    if (camera == null)
                    {
                        continue;
                    }

                    SetObjectAndParentsActive(camera.gameObject);
                    if (!camera.enabled)
                    {
                        camera.enabled = true;
                        changed++;
                    }

                    if (!camera.CompareTag("MainCamera"))
                    {
                        camera.gameObject.tag = "MainCamera";
                        changed++;
                    }

                    var listener = camera.GetComponent<AudioListener>();
                    if (listener != null && !listener.enabled)
                    {
                        listener.enabled = true;
                        changed++;
                    }

                    return changed;
                }
            }

            return changed;
        }

        private static int HideEquipmentVisuals()
        {
            var changed = 0;
            var controllers = UnityEngine.Object.FindObjectsByType<FirstPersonEquipmentVisualController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                changed += SetObjectInactive(controller.StickVisual);
                changed += SetObjectInactive(controller.MusketVisual);
                changed += SetObjectInactive(controller.ProtectiveSuitReadout);
            }

            return changed;
        }

        private static int HideFirstPersonHudRoots()
        {
            var changed = 0;
            var huds = UnityEngine.Object.FindObjectsByType<FirstPersonHud>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < huds.Length; i++)
            {
                var hud = huds[i];
                if (hud == null)
                {
                    continue;
                }

                if (hud.gameObject.activeSelf)
                {
                    hud.gameObject.SetActive(false);
                    changed++;
                }
            }

            return changed;
        }

        private static int ShowFirstPersonHudRoots()
        {
            var changed = 0;
            var huds = UnityEngine.Object.FindObjectsByType<FirstPersonHud>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < huds.Length; i++)
            {
                var hud = huds[i];
                if (hud == null)
                {
                    continue;
                }

                SetObjectAndParentsActive(hud.gameObject);
                if (!hud.enabled)
                {
                    hud.enabled = true;
                    changed++;
                }

                var behaviours = hud.GetComponentsInChildren<Behaviour>(true);
                for (var j = 0; j < behaviours.Length; j++)
                {
                    var behaviour = behaviours[j];
                    if (behaviour != null && !behaviour.enabled)
                    {
                        behaviour.enabled = true;
                        changed++;
                    }
                }
            }

            return changed;
        }

        private static int BakeUnsupportedAreaLightsForUrp()
        {
            var changed = 0;
            var lights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null ||
                    light.type != LightType.Rectangle ||
                    light.lightmapBakeType == LightmapBakeType.Baked)
                {
                    continue;
                }

                light.lightmapBakeType = LightmapBakeType.Baked;
                EditorUtility.SetDirty(light);
                changed++;
            }

            return changed;
        }

        private static int DisableAdditionalPunctualLightShadows()
        {
            var changed = 0;
            var lights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null ||
                    light.type == LightType.Directional ||
                    light.shadows == LightShadows.None)
                {
                    continue;
                }

                light.shadows = LightShadows.None;
                EditorUtility.SetDirty(light);
                changed++;
            }

            return changed;
        }

        private static int SetObjectInactive(GameObject target)
        {
            if (target == null || !target.activeSelf)
            {
                return 0;
            }

            target.SetActive(false);
            return 1;
        }

        private static int DisableActiveComponents<T>()
            where T : Behaviour
        {
            var changed = 0;
            var components = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || !component.enabled || !component.gameObject.activeInHierarchy)
                {
                    continue;
                }

                component.enabled = false;
                changed++;
            }

            return changed;
        }

        private static int DisableComponentsOnObject<T>(GameObject root)
            where T : Behaviour
        {
            var changed = 0;
            var components = root.GetComponents<T>();
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].enabled)
                {
                    components[i].enabled = false;
                    changed++;
                }
            }

            return changed;
        }

        private static int EnableComponentsWithOwners<T>()
            where T : Behaviour
        {
            var changed = 0;
            var components = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null)
                {
                    continue;
                }

                SetObjectAndParentsActive(component.gameObject);
                if (!component.enabled)
                {
                    component.enabled = true;
                    changed++;
                }
            }

            return changed;
        }

        private static void SetObjectAndParentsActive(GameObject target)
        {
            var parents = new List<Transform>();
            var current = target.transform;
            while (current != null)
            {
                parents.Add(current);
                current = current.parent;
            }

            for (var i = parents.Count - 1; i >= 0; i--)
            {
                if (!parents[i].gameObject.activeSelf)
                {
                    parents[i].gameObject.SetActive(true);
                }
            }
        }

        private static ModelingInspectionFreeCamera FindActiveFreeCameraController()
        {
            var controllers = UnityEngine.Object.FindObjectsByType<ModelingInspectionFreeCamera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < controllers.Length; i++)
            {
                var controller = controllers[i];
                if (controller != null &&
                    controller.enabled &&
                    controller.gameObject.activeInHierarchy &&
                    controller.gameObject.name == FreeCameraRootName)
                {
                    return controller;
                }
            }

            return null;
        }

        private static int CountOtherEnabledCameras(Camera inspectionCamera)
        {
            var count = 0;
            var cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera != null &&
                    camera != inspectionCamera &&
                    camera.enabled &&
                    camera.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActivePlayerViewComponents()
        {
            return CountActiveEnabledComponents<FirstPersonPlayerInput>() +
                   CountActiveEnabledComponents<FirstPersonPlayerMotor>() +
                   CountActiveEnabledComponents<FirstPersonInteractionController>() +
                   CountActiveEnabledComponents<FirstPersonHandInventory>() +
                   CountActiveEnabledComponents<PlayerEquipmentController>() +
                   CountActiveEnabledComponents<FirstPersonEquipmentVisualController>();
        }

        private static int CountActiveFirstPersonHudRoots()
        {
            var count = 0;
            var huds = UnityEngine.Object.FindObjectsByType<FirstPersonHud>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < huds.Length; i++)
            {
                if (huds[i] != null && huds[i].gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveEnabledComponents<T>()
            where T : Behaviour
        {
            var count = 0;
            var components = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component != null && component.enabled && component.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int SetNamedObjectsActive(string objectName, bool active)
        {
            var changed = 0;
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform.gameObject.name != objectName)
                {
                    continue;
                }

                if (transform.gameObject.activeSelf != active)
                {
                    transform.gameObject.SetActive(active);
                    changed++;
                }
            }

            return changed;
        }

        private static int DisableComponents<T>()
            where T : Behaviour
        {
            var changed = 0;
            var components = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                if (!components[i].enabled)
                {
                    continue;
                }

                components[i].enabled = false;
                changed++;
            }

            return changed;
        }

        private static int CountEnabledComponents<T>()
            where T : Behaviour
        {
            var count = 0;
            var components = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveNamedObjects(string objectName)
        {
            var count = 0;
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform != null &&
                    transform.gameObject.name == objectName &&
                    transform.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private readonly struct InspectionView
        {
            public InspectionView(Vector3 position, Vector3 lookAt)
            {
                Position = position;
                LookAt = lookAt;
            }

            public Vector3 Position { get; }

            public Vector3 LookAt { get; }
        }
    }
}
