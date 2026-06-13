using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class AssetStoreShipDressingEditorValidation
    {
        private static readonly (string From, string To)[] RequiredCorridors =
        {
            ("Cargo Hold", "Cockpit"),
            ("Cargo Hold", "Engine Room"),
            ("Cargo Hold", "Control Room"),
            ("Cargo Hold", "Armory"),
            ("Cargo Hold", "Supply Room"),
            ("Control Room", "Armory"),
            ("Supply Room", "Armory"),
            ("Cockpit", "Engine Room"),
            ("Cockpit", "Control Room"),
            ("Engine Room", "Control Room")
        };

        private static readonly string[] RequiredRooms =
        {
            "Cargo Hold",
            "Cockpit",
            "Engine Room",
            "Control Room",
            "Armory",
            "Supply Room"
        };

        [MenuItem("Bellerophon/Validation/Run Asset Store Ship Dressing Step 1 Validation")]
        public static void Run()
        {
            AssetStoreShipDressingBootstrap.EnsureStep1RootsWithoutValidation();
            ValidateScene();
        }

        [MenuItem("Bellerophon/Validation/Run Asset Store Ship Dressing Step 2 Corridor Validation")]
        public static void RunStep2()
        {
            AssetStoreShipDressingBootstrap.EnsureStep2CorridorDressingWithoutValidation();
            ValidateStep2Scene();
        }

        public static void ValidateScene()
        {
            RequireSceneAndBaseShip();

            var root = RequireRootObject(AssetStoreShipDressingBootstrap.RootName);

            for (var i = 0; i < AssetStoreShipDressingBootstrap.TopLevelRoots.Length; i++)
            {
                RequireDirectChild(root.transform, AssetStoreShipDressingBootstrap.TopLevelRoots[i]);
            }

            var corridorRoot = RequireDirectChild(root.transform, AssetStoreShipDressingBootstrap.CorridorRootName);
            for (var i = 0; i < RequiredCorridors.Length; i++)
            {
                RequireDirectChild(
                    corridorRoot.transform,
                    AssetStoreShipDressingBootstrap.CorridorDressingRootName(RequiredCorridors[i].From, RequiredCorridors[i].To));
            }

            for (var i = 0; i < AssetStoreShipDressingBootstrap.ImportedAssetPaths.Length; i++)
            {
                RequireAssetFolderWithPrefabs(AssetStoreShipDressingBootstrap.ImportedAssetPaths[i]);
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true).Length;
            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing roots must not introduce enabled colliders before traversal-specific validation. EnabledColliders=" +
                    enabledColliders);
            }

            Debug.Log("Asset Store ship dressing step 1 validation passed.");
            Debug.Log(
                "Asset Store ship dressing step 1 details: Root=True; TopRoots=" +
                AssetStoreShipDressingBootstrap.TopLevelRoots.Length +
                "; CorridorRoots=" +
                RequiredCorridors.Length +
                "; ImportedPacks=" +
                AssetStoreShipDressingBootstrap.ImportedAssetPaths.Length +
                "; Renderers=" +
                renderers +
                "; EnabledColliders=0");
        }

        public static void ValidateStep2Scene()
        {
            RequireSceneAndBaseShip();

            var root = RequireRootObject(AssetStoreShipDressingBootstrap.RootName);
            var corridorRoot = RequireDirectChild(root.transform, AssetStoreShipDressingBootstrap.CorridorRootName);
            var totalRenderers = 0;
            var enabledColliders = 0;
            var errorMaterialRenderers = 0;
            var corridorRootsWithDressing = 0;
            var solidWallBackers = 0;
            var opaqueWallBackings = 0;
            var thresholdSidePosts = 0;
            var thresholdTopLintels = 0;
            var thresholdCenterBlockers = 0;

            for (var i = 0; i < RequiredCorridors.Length; i++)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasCorridor(RequiredCorridors[i].From, RequiredCorridors[i].To))
                {
                    throw new InvalidOperationException("Asset Store ship dressing step 2 must preserve corridor route: " + RequiredCorridors[i].From + " to " + RequiredCorridors[i].To);
                }

                var routeRoot = RequireDirectChild(
                    corridorRoot.transform,
                    AssetStoreShipDressingBootstrap.CorridorDressingRootName(RequiredCorridors[i].From, RequiredCorridors[i].To));
                var generated = RequireDirectChild(routeRoot.transform, AssetStoreShipDressingBootstrap.CorridorGeneratedRootName);
                var renderers = generated.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length < 10)
                {
                    throw new InvalidOperationException(
                        "Asset Store ship dressing step 2 corridor root has too few visual renderers: " +
                        routeRoot.name +
                        ", Renderers=" +
                        renderers.Length);
                }

                totalRenderers += renderers.Length;
                errorMaterialRenderers += CountErrorMaterialRenderers(renderers);
                solidWallBackers += CountObjectsContaining(generated.transform, "SMP Solid Wall Backer");
                opaqueWallBackings += CountObjectsContaining(generated.transform, "Project Opaque Wall Backing");
                thresholdSidePosts += CountObjectsContaining(generated.transform, "HSK Threshold Side Post");
                thresholdTopLintels += CountObjectsContaining(generated.transform, "HSK Threshold Top Lintel");
                thresholdCenterBlockers += CountObjectsContaining(generated.transform, "HSK Threshold Arch");
                thresholdCenterBlockers += CountObjectsContaining(generated.transform, "SMP Threshold Side Cap");
                corridorRootsWithDressing++;

                var colliders = generated.GetComponentsInChildren<Collider>(true);
                for (var colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    if (colliders[colliderIndex].enabled)
                    {
                        enabledColliders++;
                    }
                }
            }

            if (enabledColliders > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 must keep imported dressing colliders disabled. EnabledColliders=" + enabledColliders);
            }

            if (errorMaterialRenderers > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 has magenta/error-shader corridor renderers. ErrorMaterialRenderers=" + errorMaterialRenderers);
            }

            if (solidWallBackers < RequiredCorridors.Length * 2 || opaqueWallBackings < RequiredCorridors.Length * 2)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must close hollow corridor wall gaps with solid backing panels. SolidWallBackers=" +
                    solidWallBackers +
                    "; OpaqueWallBackings=" +
                    opaqueWallBackings);
            }

            if (thresholdCenterBlockers > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 must not leave pass-through visual walls in doorway centers. ThresholdCenterBlockers=" + thresholdCenterBlockers);
            }

            if (thresholdSidePosts < RequiredCorridors.Length * 4 || thresholdTopLintels < RequiredCorridors.Length * 2)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must use open doorway frames instead of centered threshold blockers. ThresholdSidePosts=" +
                    thresholdSidePosts +
                    "; ThresholdTopLintels=" +
                    thresholdTopLintels);
            }

            var hskObjects = CountObjectsWithPrefix(root.transform, "HSK ");
            var smpObjects = CountObjectsWithPrefix(root.transform, "SMP ");
            if (hskObjects < RequiredCorridors.Length * 4 || smpObjects < RequiredCorridors.Length * 2)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must use both Heavy Station Kit and Sci-Fi Styled Modular Pack assets. HSK=" +
                    hskObjects +
                    ", SMP=" +
                    smpObjects);
            }

            var enabledLegacyCorridorRenderers = CountEnabledLegacyCorridorRenderers(root.transform);
            if (enabledLegacyCorridorRenderers > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 must hide legacy graybox corridor renderers after adding the dressing layer. EnabledLegacyCorridorRenderers=" + enabledLegacyCorridorRenderers);
            }

            if (totalRenderers < 120)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 created too little corridor visual coverage. Renderers=" + totalRenderers);
            }

            Debug.Log("Asset Store ship dressing step 2 corridor validation passed.");
            Debug.Log(
                "Asset Store ship dressing step 2 details: CorridorRoots=" +
                corridorRootsWithDressing +
                "; Renderers=" +
                totalRenderers +
                "; EnabledColliders=" +
                enabledColliders +
                "; ErrorMaterialRenderers=" +
                errorMaterialRenderers +
                "; SolidWallBackers=" +
                solidWallBackers +
                "; OpaqueWallBackings=" +
                opaqueWallBackings +
                "; ThresholdCenterBlockers=" +
                thresholdCenterBlockers +
                "; ThresholdSidePosts=" +
                thresholdSidePosts +
                "; ThresholdTopLintels=" +
                thresholdTopLintels +
                "; HSK=" +
                hskObjects +
                "; SMP=" +
                smpObjects +
                "; EnabledLegacyCorridorRenderers=" +
                enabledLegacyCorridorRenderers);
        }

        private static void RequireSceneAndBaseShip()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetStoreShipDressingBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Asset Store ship dressing validation.");
            }

            if (SceneManager.GetActiveScene().path != AssetStoreShipDressingBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(AssetStoreShipDressingBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            RequireRootObject(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            for (var i = 0; i < RequiredRooms.Length; i++)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasProductionRoomShell(RequiredRooms[i]))
                {
                    throw new InvalidOperationException("Asset Store ship dressing requires the existing production room shell for: " + RequiredRooms[i]);
                }
            }
        }

        private static GameObject RequireRootObject(string objectName)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    return roots[i];
                }
            }

            throw new InvalidOperationException("Missing root object: " + objectName);
        }

        private static GameObject RequireDirectChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException("Missing direct child '" + childName + "' under '" + parent.name + "'.");
            }

            return child.gameObject;
        }

        private static void RequireAssetFolderWithPrefabs(string assetFolder)
        {
            var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetFolder);
            if (folderAsset == null)
            {
                throw new InvalidOperationException("Missing imported Asset Store folder: " + assetFolder);
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetFolder });
            if (prefabGuids.Length == 0)
            {
                throw new InvalidOperationException("Imported Asset Store folder has no prefabs: " + assetFolder);
            }
        }

        private static int CountErrorMaterialRenderers(Renderer[] renderers)
        {
            var count = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (RendererHasErrorMaterial(renderers[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool RendererHasErrorMaterial(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || material.shader == null)
                {
                    return true;
                }

                if (material.shader.name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                    material.HasProperty("_Color") ? material.GetColor("_Color") : Color.black;
                if (color.r > 0.85f && color.b > 0.85f && color.g < 0.25f)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountObjectsWithPrefix(Transform root, string prefix)
        {
            var count = 0;
            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountObjectsContaining(Transform root, string fragment)
        {
            var count = 0;
            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name.IndexOf(fragment, StringComparison.Ordinal) >= 0)
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

        private static int CountEnabledLegacyCorridorRenderers(Transform dressingRoot)
        {
            var count = 0;
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || IsChildOf(renderer.transform, dressingRoot))
                {
                    continue;
                }

                if (renderer.enabled && renderer.gameObject.name.StartsWith("Corridor - ", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsChildOf(Transform candidate, Transform ancestor)
        {
            var current = candidate;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
