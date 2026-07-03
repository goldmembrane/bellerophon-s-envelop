using System;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class CargoShipVisualModelingBootstrap
    {
        private static readonly string[] VisualDressingRootNames =
        {
            AssetStoreShipDressingBootstrap.RootName,
            Phase20PresentationBootstrap.Phase20RootName,
            PostDetailedStage3GameplayPropsBootstrap.Stage3RootName,
            PostDetailedStage3GameplayPropsBootstrap.FirstPersonPreviewRootName,
            PostDetailedStage3GameplayPropsBootstrap.SpecialEquipmentRootName,
            PostDetailedStage3GameplayPropsBootstrap.RoomDressingRootName,
            PostDetailedStage3GameplayPropsBootstrap.CockpitDressingName,
            PostDetailedStage3GameplayPropsBootstrap.ControlRoomDressingName,
            PostDetailedStage3GameplayPropsBootstrap.EngineRoomDressingName,
            PostDetailedStage3GameplayPropsBootstrap.SupplyRoomDressingName,
            PostDetailedStage3GameplayPropsBootstrap.CargoHoldDressingName,
            PostDetailedStage3GameplayPropsBootstrap.ArmoryDressingName,
            PostDetailedStage3GameplayPropsBootstrap.CargoStartCorridorDressingName
        };

        [MenuItem("Bellerophon/Bootstrap/Disable Cargo Ship Visual Modeling")]
        public static void DisableVisualModeling()
        {
            var scene = OpenCargoRunScene();
            var deactivatedVisualRoots = 0;
            for (var i = 0; i < VisualDressingRootNames.Length; i++)
            {
                deactivatedVisualRoots += SetNamedObjectsActive(VisualDressingRootNames[i], false);
            }

            var grayboxRoot = RequireRootObject(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            var disabledGrayboxRenderers = SetRenderersEnabled(grayboxRoot.transform, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log(
                "Cargo ship visual modeling disabled. DeactivatedVisualRoots=" +
                deactivatedVisualRoots +
                "; DisabledGrayboxRenderers=" +
                disabledGrayboxRenderers +
                "; EnabledGrayboxColliders=" +
                CountEnabledColliders(grayboxRoot.transform) +
                "; EnabledDebugInteractables=" +
                CountEnabledBehaviours<DebugInteractable>(grayboxRoot.transform));
        }

        public static void ValidateScene()
        {
            OpenCargoRunScene();

            var grayboxRoot = RequireRootObject(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (!grayboxRoot.activeSelf)
            {
                throw new InvalidOperationException("Cargo ship graybox root must remain active so gameplay colliders and interactables stay available.");
            }

            var activeVisualRoots = 0;
            for (var i = 0; i < VisualDressingRootNames.Length; i++)
            {
                activeVisualRoots += CountActiveNamedObjects(VisualDressingRootNames[i]);
            }

            if (activeVisualRoots != 0)
            {
                throw new InvalidOperationException("Cargo ship visual dressing roots must be inactive. ActiveVisualRoots=" + activeVisualRoots);
            }

            var enabledGrayboxRenderers = CountEnabledRenderers(grayboxRoot.transform);
            if (enabledGrayboxRenderers != 0)
            {
                throw new InvalidOperationException("Cargo ship graybox renderers must be disabled while preserving logic. EnabledGrayboxRenderers=" + enabledGrayboxRenderers);
            }

            var enabledGrayboxColliders = CountEnabledColliders(grayboxRoot.transform);
            if (enabledGrayboxColliders <= 0)
            {
                throw new InvalidOperationException("Cargo ship gameplay colliders must remain enabled after disabling visual modeling.");
            }

            var enabledDebugInteractables = CountEnabledBehaviours<DebugInteractable>(grayboxRoot.transform);
            if (enabledDebugInteractables <= 0)
            {
                throw new InvalidOperationException("Cargo ship interaction components must remain enabled after disabling visual modeling.");
            }

            Debug.Log("Cargo ship visual modeling disabled validation passed.");
            Debug.Log(
                "Cargo ship visual modeling disabled details: AssetStoreActiveInHierarchy=False; ActiveVisualRoots=0; EnabledGrayboxRenderers=0; EnabledGrayboxColliders=" +
                enabledGrayboxColliders +
                "; EnabledDebugInteractables=" +
                enabledDebugInteractables);
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

        private static GameObject RequireRootObject(string objectName)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    return roots[i];
                }
            }

            throw new InvalidOperationException("Missing root object: " + objectName);
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

        private static int SetRenderersEnabled(Transform root, bool enabled)
        {
            var changed = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled == enabled)
                {
                    continue;
                }

                renderers[i].enabled = enabled;
                changed++;
            }

            return changed;
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

        private static int CountEnabledRenderers(Transform root)
        {
            var count = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnabledColliders(Transform root)
        {
            var count = 0;
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnabledBehaviours<T>(Transform root)
            where T : Behaviour
        {
            var count = 0;
            var behaviours = root.GetComponentsInChildren<T>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
