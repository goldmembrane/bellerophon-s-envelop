using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class FugaLongaArmaPlacementRecovery
    {
        private const string CurrentScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string RecoveryScenePath = "Assets/_Recovery/0 (1).unity";
        private const string FugaRootName = "Approved Fuga Enemy Placement";
        private const string LongaArmaRootName = "Approved Longa Arma Enemy Placement";

        private static readonly string[] RequiredFugaChildren =
        {
            "Fuga_00_Static",
            "Fuga_01_Idle",
            "Fuga_02_Move",
            "Fuga_03_Attack",
            "Fuga_04_Hit",
            "Fuga_05_Death",
            "Fuga_06_Consume"
        };

        private static readonly string[] RequiredLongaArmaChildren =
        {
            "LongaArma_00_Static_Review",
            "LongaArma_01_Idle",
            "LongaArma_02_Move_Crawl",
            "LongaArma_03_Attack_SlamDrag",
            "LongaArma_04_Hit_Recoil",
            "LongaArma_05_Consume_Peck",
            "LongaArma_06_Death_MeltPuddle"
        };

        [MenuItem("Bellerophon/Recovery/Restore Fuga And Longa Arma Placements From Recovery Scene")]
        public static void RestoreFugaAndLongaArmaPlacementsFromRecoveryScene()
        {
            RequireSceneAsset(CurrentScenePath);
            RequireSceneAsset(RecoveryScenePath);
            EnsureNotPlaying();

            var targetScene = EditorSceneManager.OpenScene(CurrentScenePath, OpenSceneMode.Single);
            var recoveryScene = EditorSceneManager.OpenScene(RecoveryScenePath, OpenSceneMode.Additive);

            try
            {
                RemoveRootIfPresent(targetScene, FugaRootName);
                RemoveRootIfPresent(targetScene, LongaArmaRootName);
                RemoveRootIfPresent(targetScene, "Model Cam");
                RemoveRootIfPresent(targetScene, "LongaArmaLowPolyFromOriginal");
                RemoveRootsByPrefix(targetScene, "LongaArma_V2");

                var fugaRoot = CopyRootFromRecovery(recoveryScene, targetScene, FugaRootName);
                var longaArmaRoot = CopyRootFromRecovery(recoveryScene, targetScene, LongaArmaRootName);

                RemoveDisallowedDescendants(fugaRoot);
                RemoveDisallowedDescendants(longaArmaRoot);

                EditorSceneManager.SetActiveScene(targetScene);
                ValidateRestoredScene(targetScene);

                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
                AssetDatabase.SaveAssets();

                Debug.Log("Fuga and Longa Arma placement roots restored from recovery scene.");
            }
            finally
            {
                if (recoveryScene.IsValid() && recoveryScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(recoveryScene, true);
                }
            }
        }

        private static GameObject CopyRootFromRecovery(Scene recoveryScene, Scene targetScene, string rootName)
        {
            var sourceRoot = FindRoot(recoveryScene, rootName);
            if (sourceRoot == null)
            {
                throw new InvalidOperationException($"Recovery scene is missing root: {rootName}");
            }

            var copy = UnityEngine.Object.Instantiate(sourceRoot);
            copy.name = rootName;
            SceneManager.MoveGameObjectToScene(copy, targetScene);
            EditorUtility.SetDirty(copy);
            return copy;
        }

        private static void RemoveRootIfPresent(Scene scene, string rootName)
        {
            var existing = FindRoot(scene, rootName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void RemoveRootsByPrefix(Scene scene, string prefix)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static void RemoveDisallowedDescendants(GameObject restoredRoot)
        {
            var toRemove = new List<GameObject>();
            foreach (var transform in restoredRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform == restoredRoot.transform)
                {
                    continue;
                }

                if (transform.name.StartsWith("LongaArma_V2", StringComparison.Ordinal) ||
                    string.Equals(transform.name, "Model Cam", StringComparison.Ordinal) ||
                    string.Equals(transform.name, "LongaArmaLowPolyFromOriginal", StringComparison.Ordinal))
                {
                    toRemove.Add(transform.gameObject);
                }
            }

            foreach (var gameObject in toRemove)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static void ValidateRestoredScene(Scene targetScene)
        {
            var fugaRoot = RequireRoot(targetScene, FugaRootName);
            var longaArmaRoot = RequireRoot(targetScene, LongaArmaRootName);

            RequireChildren(fugaRoot.transform, RequiredFugaChildren);
            RequireChildren(longaArmaRoot.transform, RequiredLongaArmaChildren);
            RequireNoName(targetScene, "Model Cam");
            RequireNoName(targetScene, "LongaArmaLowPolyFromOriginal");
            RequireNoNamePrefix(targetScene, "LongaArma_V2");

            Debug.Log(
                "Recovered placement roots verified. " +
                $"{FugaRootName} children={fugaRoot.transform.childCount}, " +
                $"{LongaArmaRootName} children={longaArmaRoot.transform.childCount}.");
        }

        private static void RequireChildren(Transform root, IEnumerable<string> childNames)
        {
            foreach (var childName in childNames)
            {
                if (root.Find(childName) == null)
                {
                    throw new InvalidOperationException($"{root.name} is missing required child: {childName}");
                }
            }
        }

        private static GameObject RequireRoot(Scene scene, string rootName)
        {
            var root = FindRoot(scene, rootName);
            if (root == null)
            {
                throw new InvalidOperationException($"Scene is missing root: {rootName}");
            }

            return root;
        }

        private static void RequireNoName(Scene scene, string disallowedName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (string.Equals(transform.name, disallowedName, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Disallowed object was restored: {disallowedName}");
                    }
                }
            }
        }

        private static void RequireNoNamePrefix(Scene scene, string disallowedPrefix)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name.StartsWith(disallowedPrefix, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Disallowed object was restored: {transform.name}");
                    }
                }
            }
        }

        private static GameObject FindRoot(Scene scene, string rootName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, rootName, StringComparison.Ordinal))
                {
                    return root;
                }
            }

            return null;
        }

        private static void RequireSceneAsset(string scenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException($"Scene asset was not found: {scenePath}");
            }
        }

        private static void EnsureNotPlaying()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            throw new InvalidOperationException("Cannot restore placements while Unity is entering or leaving Play Mode.");
        }
    }
}
