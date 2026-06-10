using System;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase20PresentationBootstrap
    {
        public const string CargoRunScenePath = Phase16HudMapAtmosphereBootstrap.CargoRunScenePath;
        public const string Phase20RootName = "Phase 20 Presentation Polish";
        public const string CockpitGlassFrameName = "Phase 20 Cockpit Glass Frame";
        public const string EngineDonutRootName = "Phase 20 Engine Donut Ring";
        public const string ControlScreenAccentName = "Phase 20 Control Screen Accent";
        public const string ArmoryTurretAccentName = "Phase 20 Armory Turret Accent";
        public const string SupplyEjectionWarningName = "Phase 20 Supply Ejection Warning";
        public const string CargoHoldStrapsName = "Phase 20 Cargo Hold Secured Straps";
        public const string CorridorBeaconRootName = "Phase 20 Low Visibility Corridor Beacons";
        public const int EngineDonutSegmentCount = 8;
        public const int CorridorBeaconCount = 6;

        private const string SettingsDirectory = "Assets/_Project/Settings/Ship";
        private const string AccentMaterialPath = SettingsDirectory + "/Phase20AccentMaterial.mat";
        private const string WarningMaterialPath = SettingsDirectory + "/Phase20WarningMaterial.mat";
        private const string ScreenMaterialPath = SettingsDirectory + "/Phase20ScreenMaterial.mat";
        private const string GlassFrameMaterialPath = SettingsDirectory + "/Phase20GlassFrameMaterial.mat";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 20 Presentation Polish")]
        public static void EnsurePhase20Assets()
        {
            Phase16HudMapAtmosphereBootstrap.EnsurePhase16Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase20RootName);

            var planetController = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var audioHooks = UnityEngine.Object.FindFirstObjectByType<ShipSignalAudioHooks>();
            if (planetController == null || settlementController == null || audioHooks == null)
            {
                throw new InvalidOperationException("Phase 20 requires Phase 16 scene assets plus planet stay, settlement, and audio hook controllers.");
            }

            if (settlementController.PlanetStayController != planetController)
            {
                settlementController.ConfigurePlanetContinuation(
                    planetController,
                    settlementController.ContinueToMaintenanceButton);
            }

            var root = new GameObject(Phase20RootName);
            var accentMaterial = EnsureMaterial(AccentMaterialPath, new Color(0.12f, 0.5f, 0.42f, 1f));
            var warningMaterial = EnsureMaterial(WarningMaterialPath, new Color(0.75f, 0.12f, 0.08f, 1f));
            var screenMaterial = EnsureMaterial(ScreenMaterialPath, new Color(0.04f, 0.34f, 0.28f, 1f));
            var glassFrameMaterial = EnsureMaterial(GlassFrameMaterialPath, new Color(0.18f, 0.36f, 0.42f, 1f));

            CreateCockpitGlassFrame(root.transform, glassFrameMaterial);
            CreateEngineDonutRing(root.transform, accentMaterial);
            CreateControlScreenAccents(root.transform, screenMaterial);
            CreateArmoryTurretAccent(root.transform, accentMaterial);
            CreateSupplyEjectionWarning(root.transform, warningMaterial);
            CreateCargoHoldStraps(root.transform, warningMaterial);
            CreateCorridorBeacons(root.transform, accentMaterial);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase20PresentationEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 20 presentation polish assets are ready.");
        }

        private static void CreateCockpitGlassFrame(Transform parent, Material material)
        {
            var root = new GameObject(CockpitGlassFrameName);
            root.transform.SetParent(parent, false);
            CreateBox(
                CockpitGlassFrameName + " Top",
                root.transform,
                new Vector3(0f, 2.66f, 22.12f),
                new Vector3(9.2f, 0.18f, 0.24f),
                material);
            CreateBox(
                CockpitGlassFrameName + " Bottom",
                root.transform,
                new Vector3(0f, 0.18f, 22.12f),
                new Vector3(9.2f, 0.18f, 0.24f),
                material);
            CreateBox(
                CockpitGlassFrameName + " Left",
                root.transform,
                new Vector3(-4.6f, 1.42f, 22.12f),
                new Vector3(0.18f, 2.45f, 0.24f),
                material);
            CreateBox(
                CockpitGlassFrameName + " Right",
                root.transform,
                new Vector3(4.6f, 1.42f, 22.12f),
                new Vector3(0.18f, 2.45f, 0.24f),
                material);
        }

        private static void CreateEngineDonutRing(Transform parent, Material material)
        {
            var root = new GameObject(EngineDonutRootName);
            root.transform.SetParent(parent, false);
            var center = new Vector3(-14f, 1.15f, 18f);
            for (var i = 0; i < EngineDonutSegmentCount; i++)
            {
                var angle = i * Mathf.PI * 2f / EngineDonutSegmentCount;
                var position = center + new Vector3(Mathf.Cos(angle) * 1.45f, 0f, Mathf.Sin(angle) * 1.45f);
                var segment = CreateBox(
                    EngineDonutRootName + " Segment " + (i + 1),
                    root.transform,
                    position,
                    new Vector3(0.72f, 0.2f, 0.24f),
                    material);
                segment.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }
        }

        private static void CreateControlScreenAccents(Transform parent, Material material)
        {
            var root = new GameObject(ControlScreenAccentName);
            root.transform.SetParent(parent, false);
            CreateBox(
                ControlScreenAccentName + " Horizontal Glow",
                root.transform,
                new Vector3(12.1f, 1.85f, 21.16f),
                new Vector3(1.9f, 0.08f, 0.08f),
                material);
            CreateBox(
                ControlScreenAccentName + " Vertical Glow",
                root.transform,
                new Vector3(16.3f, 1.35f, 20.98f),
                new Vector3(0.08f, 1.45f, 0.08f),
                material);
        }

        private static void CreateArmoryTurretAccent(Transform parent, Material material)
        {
            CreateBox(
                ArmoryTurretAccentName,
                parent,
                new Vector3(-14f, 2.8f, -12f),
                new Vector3(3.4f, 0.18f, 0.22f),
                material);
        }

        private static void CreateSupplyEjectionWarning(Transform parent, Material material)
        {
            var root = new GameObject(SupplyEjectionWarningName);
            root.transform.SetParent(parent, false);
            CreateBox(
                SupplyEjectionWarningName + " Stripe A",
                root.transform,
                new Vector3(11.6f, 0.08f, -14.9f),
                new Vector3(0.22f, 0.08f, 3.0f),
                material).transform.rotation = Quaternion.Euler(0f, 24f, 0f);
            CreateBox(
                SupplyEjectionWarningName + " Stripe B",
                root.transform,
                new Vector3(12.3f, 0.08f, -13.2f),
                new Vector3(0.22f, 0.08f, 3.0f),
                material).transform.rotation = Quaternion.Euler(0f, 24f, 0f);
        }

        private static void CreateCargoHoldStraps(Transform parent, Material material)
        {
            var root = new GameObject(CargoHoldStrapsName);
            root.transform.SetParent(parent, false);
            CreateBox(
                CargoHoldStrapsName + " Strap A",
                root.transform,
                new Vector3(0f, -2.15f, -0.7f),
                new Vector3(2.65f, 0.12f, 0.16f),
                material);
            CreateBox(
                CargoHoldStrapsName + " Strap B",
                root.transform,
                new Vector3(0f, -2.15f, 0.7f),
                new Vector3(2.65f, 0.12f, 0.16f),
                material);
        }

        private static void CreateCorridorBeacons(Transform parent, Material material)
        {
            var root = new GameObject(CorridorBeaconRootName);
            root.transform.SetParent(parent, false);
            var positions = new[]
            {
                new Vector3(0f, 0.18f, 7f),
                new Vector3(-6f, 0.18f, 9f),
                new Vector3(6f, 0.18f, 9f),
                new Vector3(-6f, 0.18f, -7f),
                new Vector3(6f, 0.18f, -7f),
                new Vector3(0f, -2.82f, -4f)
            };

            for (var i = 0; i < positions.Length; i++)
            {
                CreateBox(
                    CorridorBeaconRootName + " " + (i + 1),
                    root.transform,
                    positions[i],
                    new Vector3(0.34f, 0.1f, 0.34f),
                    material);
            }
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = scale;
            var renderer = box.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            var collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return box;
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
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null && material.shader != shader)
                {
                    material.shader = shader;
                }
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(child.gameObject);
                return;
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
