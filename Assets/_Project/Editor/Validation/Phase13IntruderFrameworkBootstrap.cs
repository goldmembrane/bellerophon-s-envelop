using System;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase13IntruderFrameworkBootstrap
    {
        public const string CargoRunScenePath = Phase12ManualTurretBootstrap.CargoRunScenePath;
        public const string Phase13RootName = "Phase 13 Intruder Framework";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 13 Intruder Framework")]
        public static void EnsurePhase13Assets()
        {
            Phase12ManualTurretBootstrap.EnsurePhase12Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase13RootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            if (hud == null)
            {
                throw new InvalidOperationException("Phase 13 requires the Phase 12 HUD hierarchy.");
            }

            CreateRoot(hud.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase13IntruderFrameworkEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 13 intruder framework assets are ready.");
        }

        private static void CreateRoot(Transform parent)
        {
            var root = new GameObject(Phase13RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                    return;
                }

                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    return;
                }
            }
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
