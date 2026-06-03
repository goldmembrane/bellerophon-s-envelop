using System;
using System.IO;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase14ParvumIntruderBootstrap
    {
        public const string CargoRunScenePath = Phase13IntruderFrameworkBootstrap.CargoRunScenePath;
        public const string Phase14RootName = "Phase 14 Parvum Intruder";

        private const string SettingsDirectory = "Assets/_Project/Settings/Ship";
        private const string ParvumBodyMaterialPath = SettingsDirectory + "/ParvumBodyMaterial.mat";
        private const string ParvumCoreMaterialPath = SettingsDirectory + "/ParvumCoreMaterial.mat";
        private const string ParvumMawMaterialPath = SettingsDirectory + "/ParvumMawMaterial.mat";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 14 Parvum Intruder")]
        public static void EnsurePhase14Assets()
        {
            Directory.CreateDirectory(SettingsDirectory);

            Phase13IntruderFrameworkBootstrap.EnsurePhase13Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase14RootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            if (hud == null)
            {
                throw new InvalidOperationException("Phase 14 requires the Phase 13 HUD hierarchy.");
            }

            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if (deviceState == null)
            {
                throw new InvalidOperationException("Phase 14 requires the ship device interaction state.");
            }

            var bodyMaterial = EnsureMaterial(ParvumBodyMaterialPath, new Color(0.16f, 0.38f, 0.24f, 1f));
            var coreMaterial = EnsureMaterial(ParvumCoreMaterialPath, new Color(0.94f, 0.24f, 0.12f, 1f));
            var mawMaterial = EnsureMaterial(ParvumMawMaterialPath, new Color(0.05f, 0.02f, 0.03f, 1f));

            CreateRoot(deviceState, bodyMaterial, coreMaterial, mawMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase14ParvumIntruderEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 14 parvum intruder assets are ready.");
        }

        private static void CreateRoot(
            ShipDeviceInteractionState deviceState,
            Material bodyMaterial,
            Material coreMaterial,
            Material mawMaterial)
        {
            var root = new GameObject(Phase14RootName);
            var anchorsRoot = new GameObject("Parvum Room Anchors");
            anchorsRoot.transform.SetParent(root.transform, false);

            var cockpit = CreateRoomAnchor(anchorsRoot.transform, ShipRoomId.Cockpit);
            var cargoHold = CreateRoomAnchor(anchorsRoot.transform, ShipRoomId.CargoHold);
            var engineRoom = CreateRoomAnchor(anchorsRoot.transform, ShipRoomId.EngineRoom);
            var controlRoom = CreateRoomAnchor(anchorsRoot.transform, ShipRoomId.ControlRoom);
            var armory = CreateRoomAnchor(anchorsRoot.transform, ShipRoomId.Armory);
            var supplyRoom = CreateRoomAnchor(anchorsRoot.transform, ShipRoomId.SupplyRoom);

            var parvumVisual = CreateParvumVisual(root.transform, bodyMaterial, coreMaterial, mawMaterial);
            var visualView = root.AddComponent<SeedIntruderVisualView>();
            visualView.Configure(deviceState, parvumVisual, cockpit, cargoHold, engineRoom, controlRoom, armory, supplyRoom);
        }

        private static Transform CreateRoomAnchor(Transform parent, ShipRoomId roomId)
        {
            var roomObject = GameObject.Find("Room - " + GetRoomDisplayName(roomId));
            if (roomObject == null)
            {
                throw new InvalidOperationException("Phase 14 requires graybox room object for " + roomId + ".");
            }

            var anchor = new GameObject("Parvum Anchor - " + GetRoomDisplayName(roomId));
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = roomObject.transform.position + new Vector3(0f, 0.85f, 0f);
            anchor.transform.rotation = Quaternion.identity;
            return anchor.transform;
        }

        private static GameObject CreateParvumVisual(
            Transform parent,
            Material bodyMaterial,
            Material coreMaterial,
            Material mawMaterial)
        {
            var visualRoot = new GameObject("Parvum Intruder Visual");
            visualRoot.transform.SetParent(parent, false);

            CreatePrimitivePart(
                "Parvum Body",
                PrimitiveType.Sphere,
                visualRoot.transform,
                new Vector3(0f, 0.36f, 0f),
                new Vector3(0.78f, 0.42f, 0.6f),
                Quaternion.identity,
                bodyMaterial);
            CreatePrimitivePart(
                "Parvum Maw",
                PrimitiveType.Cube,
                visualRoot.transform,
                new Vector3(0f, 0.27f, 0.48f),
                new Vector3(0.3f, 0.08f, 0.1f),
                Quaternion.identity,
                mawMaterial);
            CreatePrimitivePart(
                "Parvum Core",
                PrimitiveType.Sphere,
                visualRoot.transform,
                new Vector3(0f, 0.45f, 0.48f),
                new Vector3(0.18f, 0.18f, 0.18f),
                Quaternion.identity,
                coreMaterial);
            CreatePrimitivePart(
                "Parvum Left Fore Tendril",
                PrimitiveType.Cube,
                visualRoot.transform,
                new Vector3(-0.38f, 0.19f, 0.24f),
                new Vector3(0.12f, 0.08f, 0.62f),
                Quaternion.Euler(0f, 28f, 0f),
                bodyMaterial);
            CreatePrimitivePart(
                "Parvum Right Fore Tendril",
                PrimitiveType.Cube,
                visualRoot.transform,
                new Vector3(0.38f, 0.19f, 0.24f),
                new Vector3(0.12f, 0.08f, 0.62f),
                Quaternion.Euler(0f, -28f, 0f),
                bodyMaterial);
            CreatePrimitivePart(
                "Parvum Left Rear Tendril",
                PrimitiveType.Cube,
                visualRoot.transform,
                new Vector3(-0.36f, 0.18f, -0.28f),
                new Vector3(0.11f, 0.07f, 0.54f),
                Quaternion.Euler(0f, -24f, 0f),
                bodyMaterial);
            CreatePrimitivePart(
                "Parvum Right Rear Tendril",
                PrimitiveType.Cube,
                visualRoot.transform,
                new Vector3(0.36f, 0.18f, -0.28f),
                new Vector3(0.11f, 0.07f, 0.54f),
                Quaternion.Euler(0f, 24f, 0f),
                bodyMaterial);

            visualRoot.SetActive(false);
            return visualRoot;
        }

        private static void CreatePrimitivePart(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string GetRoomDisplayName(ShipRoomId roomId)
        {
            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return "Cockpit";
                case ShipRoomId.CargoHold:
                    return "Cargo Hold";
                case ShipRoomId.EngineRoom:
                    return "Engine Room";
                case ShipRoomId.ControlRoom:
                    return "Control Room";
                case ShipRoomId.Armory:
                    return "Armory";
                case ShipRoomId.SupplyRoom:
                    return "Supply Room";
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
            }
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
