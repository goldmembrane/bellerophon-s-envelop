using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class AssetStoreShipDressingBootstrap
    {
        public const string CargoRunScenePath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath;
        public const string RootName = "Asset Store Ship Dressing";
        public const string SafetyRootName = "00 Safety Baseline";
        public const string CorridorRootName = "01 Corridors And Thresholds";
        public const string CargoHoldRootName = "02 Cargo Hold Dressing";
        public const string CockpitRootName = "03 Cockpit Dressing";
        public const string ControlRoomRootName = "04 Control Room Dressing";
        public const string EngineRoomRootName = "05 Engine Room Dressing";
        public const string SupplyRoomRootName = "06 Supply Room Dressing";
        public const string ArmoryRootName = "07 Armory Dressing";
        public const string MaterialLightingRootName = "08 Material And Lighting Cohesion";
        public const string RevisionRootName = "09 Revision Capture";
        public const string CorridorGeneratedRootName = "Generated Corridor Visual Dressing";

        public const string HeavyStationKitPath = "Assets/Heavy Station Kit";
        public const string ScifiStyledModularPackPath = "Assets/Sci-Fi Styled Modular Pack";
        public const string ScifiOfficeLitePath = "Assets/ScifiOfficeLite";
        public const string GoldenFrameTerminalPath = "Assets/GoldenFrame_Terminal_FREE";
        public const string HeavyStationFloorPrefabPath = HeavyStationKitPath + "/BASE/Prefabs/Floors/Floor Base 3.prefab";
        public const string HeavyStationWallPrefabPath = HeavyStationKitPath + "/BASE/Prefabs/Walls/1 Wall/W1_D0.prefab";
        public const string HeavyStationArchPrefabPath = HeavyStationKitPath + "/BASE/Prefabs/Arches/G1.prefab";
        public const string HeavyStationLightPrefabPath = HeavyStationKitPath + "/BASE/Prefabs/Walls/Wall Lights/Wall Lights On.prefab";
        public const string HeavyStationRailingPrefabPath = HeavyStationKitPath + "/BASE/Prefabs/Floors Fill/_Handrails/St_Base2_Railing.prefab";
        public const string StyledFloorPrefabPath = ScifiStyledModularPackPath + "/Prefabs/Floors/floor_2.prefab";
        public const string StyledSolidWallPrefabPath = ScifiStyledModularPackPath + "/Prefabs/Walls/Simple/blank_wall_A.prefab";
        public const string StyledCeilingLightPrefabPath = ScifiStyledModularPackPath + "/Prefabs/Lights/light_celing_1.prefab";
        public const string StyledJointPrefabPath = ScifiStyledModularPackPath + "/Prefabs/Joints/Joint_X_6.prefab";
        private const string CorridorFloorMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorCorridorFloor_Rough.mat";
        private const string CorridorWallMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorWall_Rough.mat";
        private const string CorridorFrameMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorDoorFrame_Worn.mat";
        private const string CorridorRailMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorCableTray_Dark.mat";
        private const string CorridorLightMaterialPath = "Assets/_Project/Art/Ship/Materials/Stage3Light_Cyan.mat";

        public static readonly string[] TopLevelRoots =
        {
            SafetyRootName,
            CorridorRootName,
            CargoHoldRootName,
            CockpitRootName,
            ControlRoomRootName,
            EngineRoomRootName,
            SupplyRoomRootName,
            ArmoryRootName,
            MaterialLightingRootName,
            RevisionRootName
        };

        public static readonly string[] ImportedAssetPaths =
        {
            HeavyStationKitPath,
            ScifiStyledModularPackPath,
            ScifiOfficeLitePath,
            GoldenFrameTerminalPath
        };

        private static readonly RoomDressingRoot[] RoomRoots =
        {
            new RoomDressingRoot(CargoHoldRootName, new Vector3(0f, -3f, 0f)),
            new RoomDressingRoot(CockpitRootName, new Vector3(0f, 0f, 18f)),
            new RoomDressingRoot(ControlRoomRootName, new Vector3(14f, 0f, 18f)),
            new RoomDressingRoot(EngineRoomRootName, new Vector3(-14f, 0f, 18f)),
            new RoomDressingRoot(SupplyRoomRootName, new Vector3(14f, 0f, -14f)),
            new RoomDressingRoot(ArmoryRootName, new Vector3(-14f, 0f, -14f))
        };

        private static readonly (string From, string To)[] CorridorRoots =
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

        [MenuItem("Bellerophon/Bootstrap/Ensure Asset Store Ship Dressing Step 1")]
        public static void EnsureStep1Roots()
        {
            EnsureStep1Roots(validateAfterCreate: true);
        }

        internal static void EnsureStep1RootsWithoutValidation()
        {
            EnsureStep1Roots(validateAfterCreate: false);
        }

        [MenuItem("Bellerophon/Bootstrap/Ensure Asset Store Ship Dressing Step 2 Corridors")]
        public static void EnsureStep2CorridorDressing()
        {
            EnsureStep2CorridorDressing(validateAfterCreate: true);
        }

        internal static void EnsureStep2CorridorDressingWithoutValidation()
        {
            EnsureStep2CorridorDressing(validateAfterCreate: false);
        }

        private static void EnsureStep1Roots(bool validateAfterCreate)
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var root = EnsureRoot(RootName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            EnsureChildRoot(root.transform, SafetyRootName, Vector3.zero);
            var corridorRoot = EnsureChildRoot(root.transform, CorridorRootName, Vector3.zero);
            EnsureCorridorRoots(corridorRoot.transform);
            EnsureRoomRoots(root.transform);
            EnsureChildRoot(root.transform, MaterialLightingRootName, Vector3.zero);
            EnsureChildRoot(root.transform, RevisionRootName, Vector3.zero);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);

            if (validateAfterCreate)
            {
                AssetStoreShipDressingEditorValidation.ValidateScene();
            }

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Asset Store ship dressing step 1 roots are ready.");
        }

        private static void EnsureStep2CorridorDressing(bool validateAfterCreate)
        {
            EnsureStep1RootsWithoutValidation();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var root = EnsureRoot(RootName);
            var corridorRoot = EnsureChildRoot(root.transform, CorridorRootName, Vector3.zero);
            var assets = CorridorAssetSet.Load();
            var materials = CorridorDressingMaterials.Load();
            var hiddenLegacyCorridorRenderers = HideLegacyCorridorRenderers(root.transform);

            for (var i = 0; i < CorridorRoots.Length; i++)
            {
                var corridor = CorridorRoots[i];
                var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute(corridor.From, corridor.To);
                var routeRoot = EnsureChildRoot(
                    corridorRoot.transform,
                    CorridorDressingRootName(corridor.From, corridor.To),
                    RouteCenter(route));
                ClearChildren(routeRoot.transform);

                var generated = EnsureChildRoot(routeRoot.transform, CorridorGeneratedRootName, Vector3.zero);
                CreateCorridorVisualDressing(generated.transform, assets, materials, corridor.From, corridor.To, route);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);

            if (validateAfterCreate)
            {
                AssetStoreShipDressingEditorValidation.ValidateStep2Scene();
            }

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Asset Store ship dressing step 2 corridors are ready. HiddenLegacyCorridorRenderers=" + hiddenLegacyCorridorRenderers);
        }

        private static GameObject EnsureRoot(string name)
        {
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i].name == name)
                {
                    return rootObjects[i];
                }
            }

            return new GameObject(name);
        }

        private static GameObject EnsureChildRoot(Transform parent, string name, Vector3 localPosition)
        {
            var child = parent.Find(name);
            GameObject target;
            if (child == null)
            {
                target = new GameObject(name);
                target.transform.SetParent(parent, false);
            }
            else
            {
                target = child.gameObject;
            }

            target.transform.localPosition = localPosition;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
            return target;
        }

        private static void EnsureRoomRoots(Transform root)
        {
            for (var i = 0; i < RoomRoots.Length; i++)
            {
                EnsureChildRoot(root, RoomRoots[i].Name, RoomRoots[i].Position);
            }
        }

        private static void EnsureCorridorRoots(Transform root)
        {
            for (var i = 0; i < CorridorRoots.Length; i++)
            {
                var name = CorridorDressingRootName(CorridorRoots[i].From, CorridorRoots[i].To);
                var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute(CorridorRoots[i].From, CorridorRoots[i].To);
                EnsureChildRoot(root, name, RouteCenter(route));
            }
        }

        public static string CorridorDressingRootName(string from, string to)
        {
            return "Dressing - " + from + " to " + to;
        }

        private static Vector3 RouteCenter(IReadOnlyList<Vector3> route)
        {
            if (route == null || route.Count == 0)
            {
                return Vector3.zero;
            }

            var sum = Vector3.zero;
            for (var i = 0; i < route.Count; i++)
            {
                sum += route[i];
            }

            return sum / route.Count;
        }

        private static void CreateCorridorVisualDressing(
            Transform parent,
            CorridorAssetSet assets,
            CorridorDressingMaterials materials,
            string from,
            string to,
            IReadOnlyList<Vector3> route)
        {
            if (route == null || route.Count < 2)
            {
                throw new InvalidOperationException("Cannot dress corridor with fewer than two route points: " + from + " to " + to);
            }

            for (var i = 0; i < route.Count - 1; i++)
            {
                CreateSegmentDressing(parent, assets, materials, from, to, i + 1, route[i], route[i + 1]);
            }

            for (var i = 1; i < route.Count - 1; i++)
            {
                CreateJointCleanupDressing(parent, assets, materials, from, to, i, route[i - 1], route[i], route[i + 1]);
            }

            CreateThresholdDressing(parent, assets, materials, from, to, "Start", route[0], route[1]);
            CreateThresholdDressing(parent, assets, materials, from, to, "End", route[route.Count - 1], route[route.Count - 2]);
        }

        private static void CreateSegmentDressing(
            Transform parent,
            CorridorAssetSet assets,
            CorridorDressingMaterials materials,
            string from,
            string to,
            int segmentIndex,
            Vector3 start,
            Vector3 end)
        {
            var delta = end - start;
            var length = delta.magnitude;
            if (length < 0.1f)
            {
                return;
            }

            var flatDirection = FlatDirection(delta);
            var yawRotation = Quaternion.LookRotation(flatDirection, Vector3.up);
            var floorRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            var right = Vector3.Cross(Vector3.up, flatDirection).normalized;
            var panelCount = Mathf.Max(1, Mathf.CeilToInt(length / 2.2f));
            var panelLength = length / panelCount;

            for (var i = 0; i < panelCount; i++)
            {
                var t = (i + 0.5f) / panelCount;
                var center = Vector3.Lerp(start, end, t);
                var floorPrefab = i % 2 == 0 ? assets.HeavyFloor : assets.StyledFloor;
                InstantiateFittedPrefab(
                    floorPrefab,
                    "HSK Floor Plate - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1),
                    parent,
                    center + new Vector3(0f, 0.035f, 0f),
                    floorRotation,
                    new Vector3(2.42f, 0.16f, Mathf.Max(0.8f, panelLength * 0.92f)),
                    materials.Floor);

                for (var side = -1; side <= 1; side += 2)
                {
                    var sideOffset = right * (side * 1.52f);

                    CreateSolidPanel(
                        "Project Opaque Wall Backing - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center + (right * (side * 1.62f)) + new Vector3(0f, 1.12f, 0f),
                        yawRotation,
                        new Vector3(0.18f, 2.24f, Mathf.Max(0.82f, panelLength * 0.96f)),
                        materials.Wall);

                    InstantiateFittedPrefab(
                        assets.StyledWall,
                        "SMP Solid Wall Backer - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center + sideOffset + new Vector3(0f, 1.12f, 0f),
                        yawRotation,
                        new Vector3(0.3f, 2.18f, Mathf.Max(0.8f, panelLength * 0.9f)),
                        materials.Wall);

                    InstantiateFittedPrefab(
                        assets.HeavyWall,
                        "HSK Wall Rib Overlay - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center + (right * (side * 1.4f)) + new Vector3(0f, 1.1f, 0f),
                        yawRotation,
                        new Vector3(0.12f, 1.9f, Mathf.Max(0.68f, panelLength * 0.72f)),
                        materials.Frame);

                    if (i % 2 == 0)
                    {
                        InstantiateFittedPrefab(
                            assets.HeavyRailing,
                            "HSK Low Rail - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                            parent,
                            center + (right * (side * 1.18f)) + new Vector3(0f, 0.52f, 0f),
                            yawRotation,
                            new Vector3(0.16f, 0.42f, Mathf.Max(0.75f, panelLength * 0.82f)),
                            materials.Rail);
                    }

                    if (i % 3 == 1)
                    {
                        InstantiateFittedPrefab(
                            assets.HeavyWallLight,
                            "HSK Wall Light - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                            parent,
                            center + (right * (side * 1.42f)) + new Vector3(0f, 1.62f, 0f),
                            yawRotation,
                            new Vector3(0.34f, 0.34f, 0.34f),
                            materials.Light);
                    }
                }

                InstantiateFittedPrefab(
                    assets.StyledCeilingLight,
                    "SMP Ceiling Fixture - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1),
                    parent,
                    center + new Vector3(0f, 2.36f, 0f),
                    yawRotation,
                    new Vector3(2.25f, 0.18f, Mathf.Max(0.7f, panelLength * 0.75f)),
                    materials.Frame);

                CreateSolidPanel(
                    "Project Opaque Ceiling Backing - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1),
                    parent,
                    center + new Vector3(0f, 2.46f, 0f),
                    yawRotation,
                    new Vector3(2.34f, 0.12f, Mathf.Max(0.82f, panelLength * 0.94f)),
                    materials.Frame);
            }
        }

        private static void CreateJointCleanupDressing(
            Transform parent,
            CorridorAssetSet assets,
            CorridorDressingMaterials materials,
            string from,
            string to,
            int jointIndex,
            Vector3 previous,
            Vector3 joint,
            Vector3 next)
        {
            var direction = FlatDirection((joint - previous) + (next - joint));
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            var right = Vector3.Cross(Vector3.up, direction).normalized;

            InstantiateFittedPrefab(
                assets.StyledJoint,
                "SMP Cleanup Joint Floor Mask - " + from + " to " + to + " J" + jointIndex,
                parent,
                joint + new Vector3(0f, 0.07f, 0f),
                rotation,
                new Vector3(3.1f, 0.16f, 3.1f),
                materials.Floor);

            for (var side = -1; side <= 1; side += 2)
            {
                CreateSolidPanel(
                    "Project Opaque Joint Backing - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint + (right * (side * 1.66f)) + new Vector3(0f, 1.18f, 0f),
                    rotation,
                    new Vector3(0.18f, 2.24f, 1.28f),
                    materials.Wall);

                InstantiateFittedPrefab(
                    assets.StyledWall,
                    "SMP Solid Joint Side Cover - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint + (right * (side * 1.56f)) + new Vector3(0f, 1.16f, 0f),
                    rotation,
                    new Vector3(0.32f, 2.1f, 1.15f),
                    materials.Wall);

                InstantiateFittedPrefab(
                    assets.HeavyWall,
                    "HSK Joint Rib Overlay - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint + (right * (side * 1.38f)) + new Vector3(0f, 1.08f, 0f),
                    rotation,
                    new Vector3(0.12f, 1.82f, 0.92f),
                    materials.Frame);
            }
        }

        private static void CreateThresholdDressing(
            Transform parent,
            CorridorAssetSet assets,
            CorridorDressingMaterials materials,
            string from,
            string to,
            string label,
            Vector3 endpoint,
            Vector3 adjacentPoint)
        {
            var direction = FlatDirection(adjacentPoint - endpoint);
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            var basePosition = endpoint + (direction * 0.24f);

            InstantiateFittedPrefab(
                assets.HeavyFloor,
                "HSK Threshold Floor Lip - " + from + " to " + to + " " + label,
                parent,
                basePosition + new Vector3(0f, 0.08f, 0f),
                rotation,
                new Vector3(3.45f, 0.12f, 0.72f),
                materials.Floor);

            for (var side = -1; side <= 1; side += 2)
            {
                CreateSolidPanel(
                    "Project Opaque Threshold Post Backing - " + from + " to " + to + " " + label + " Side " + side,
                    parent,
                    basePosition + (right * (side * 1.96f)) + new Vector3(0f, 1.26f, 0f),
                    rotation,
                    new Vector3(0.3f, 2.46f, 0.64f),
                    materials.Wall);

                InstantiateFittedPrefab(
                    assets.StyledWall,
                    "SMP Threshold Side Backer - " + from + " to " + to + " " + label + " Side " + side,
                    parent,
                    basePosition + (right * (side * 1.9f)) + new Vector3(0f, 1.22f, 0f),
                    rotation,
                    new Vector3(0.26f, 2.24f, 0.62f),
                    materials.Wall);

                InstantiateFittedPrefab(
                    assets.HeavyWall,
                    "HSK Threshold Side Post - " + from + " to " + to + " " + label + " Side " + side,
                    parent,
                    basePosition + (right * (side * 1.68f)) + new Vector3(0f, 1.22f, 0f),
                    rotation,
                    new Vector3(0.18f, 2.14f, 0.48f),
                    materials.Frame);
            }

            CreateSolidPanel(
                "Project Opaque Threshold Top Backing - " + from + " to " + to + " " + label,
                parent,
                basePosition + new Vector3(0f, 2.46f, 0f),
                rotation,
                new Vector3(3.55f, 0.24f, 0.64f),
                materials.Frame);

            InstantiateFittedPrefab(
                assets.HeavyArch,
                "HSK Threshold Top Lintel - " + from + " to " + to + " " + label,
                parent,
                basePosition + new Vector3(0f, 2.38f, 0f),
                rotation,
                new Vector3(3.5f, 0.26f, 0.5f),
                materials.Frame);
        }

        private static GameObject CreateSolidPanel(
            string name,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 localScale,
            Material material)
        {
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = name;
            panel.transform.SetParent(parent, false);
            panel.transform.position = worldPosition;
            panel.transform.rotation = worldRotation;
            panel.transform.localScale = localScale;

            var collider = panel.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var renderer = panel.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            return panel;
        }

        private static GameObject InstantiateFittedPrefab(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 targetLocalBounds,
            Material overrideMaterial)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = worldPosition;
            anchor.transform.rotation = worldRotation;
            anchor.transform.localScale = Vector3.one;

            var instance = PrefabUtility.InstantiatePrefab(prefab, anchor.transform) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, anchor.transform);
            }

            instance.name = name + " Source";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            ConfigureVisualOnly(instance);
            ApplyMaterialOverride(instance, overrideMaterial);
            FitChildToLocalBounds(anchor.transform, instance.transform, targetLocalBounds);
            return anchor;
        }

        private static void FitChildToLocalBounds(Transform anchor, Transform child, Vector3 targetLocalBounds)
        {
            var initialBounds = CalculateLocalRenderBounds(anchor);
            var size = initialBounds.size;
            var scale = new Vector3(
                AxisScale(targetLocalBounds.x, size.x),
                AxisScale(targetLocalBounds.y, size.y),
                AxisScale(targetLocalBounds.z, size.z));
            child.localScale = Vector3.Scale(child.localScale, scale);

            var fittedBounds = CalculateLocalRenderBounds(anchor);
            child.localPosition -= fittedBounds.center;
        }

        private static float AxisScale(float target, float current)
        {
            if (current <= 0.001f)
            {
                return 1f;
            }

            return Mathf.Clamp(target / current, 0.025f, 12f);
        }

        private static Bounds CalculateLocalRenderBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                var rendererBounds = renderers[i].bounds;
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, rendererBounds.min);
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, rendererBounds.max);
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.min.x, rendererBounds.min.y, rendererBounds.max.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.min.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.min.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.max.x, rendererBounds.max.y, rendererBounds.min.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.max.x, rendererBounds.min.y, rendererBounds.max.z));
                EncapsulateLocalPoint(root, ref bounds, ref hasBounds, new Vector3(rendererBounds.min.x, rendererBounds.max.y, rendererBounds.max.z));
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        private static void EncapsulateLocalPoint(Transform root, ref Bounds bounds, ref bool hasBounds, Vector3 worldPoint)
        {
            var localPoint = root.InverseTransformPoint(worldPoint);
            if (!hasBounds)
            {
                bounds = new Bounds(localPoint, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(localPoint);
            }
        }

        private static void ConfigureVisualOnly(GameObject instance)
        {
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            var rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rigidbodies.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(rigidbodies[i]);
            }

            var behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = false;
                }
            }

            var lights = instance.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                lights[i].intensity = Mathf.Min(lights[i].intensity, 0.65f);
                lights[i].range = Mathf.Min(lights[i].range, 4.5f);
            }
        }

        private static void ApplyMaterialOverride(GameObject instance, Material material)
        {
            if (material == null)
            {
                return;
            }

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var materials = renderers[i].sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderers[i].sharedMaterial = material;
                    continue;
                }

                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    materials[materialIndex] = material;
                }

                renderers[i].sharedMaterials = materials;
            }
        }

        private static Vector3 FlatDirection(Vector3 direction)
        {
            var flat = new Vector3(direction.x, 0f, direction.z);
            return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
        }

        private static int HideLegacyCorridorRenderers(Transform dressingRoot)
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

                if (!renderer.gameObject.name.StartsWith("Corridor - ", StringComparison.Ordinal))
                {
                    continue;
                }

                if (renderer.enabled)
                {
                    renderer.enabled = false;
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

        private sealed class CorridorDressingMaterials
        {
            private CorridorDressingMaterials(
                Material floor,
                Material wall,
                Material frame,
                Material rail,
                Material light)
            {
                Floor = floor;
                Wall = wall;
                Frame = frame;
                Rail = rail;
                Light = light;
            }

            public Material Floor { get; }

            public Material Wall { get; }

            public Material Frame { get; }

            public Material Rail { get; }

            public Material Light { get; }

            public static CorridorDressingMaterials Load()
            {
                return new CorridorDressingMaterials(
                    LoadMaterial(CorridorFloorMaterialPath),
                    LoadMaterial(CorridorWallMaterialPath),
                    LoadMaterial(CorridorFrameMaterialPath),
                    LoadMaterial(CorridorRailMaterialPath),
                    LoadMaterial(CorridorLightMaterialPath));
            }

            private static Material LoadMaterial(string path)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    throw new InvalidOperationException("Missing corridor dressing material: " + path);
                }

                return material;
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private sealed class CorridorAssetSet
        {
            private CorridorAssetSet(
                GameObject heavyFloor,
                GameObject heavyWall,
                GameObject heavyArch,
                GameObject heavyWallLight,
                GameObject heavyRailing,
                GameObject styledFloor,
                GameObject styledWall,
                GameObject styledCeilingLight,
                GameObject styledJoint)
            {
                HeavyFloor = heavyFloor;
                HeavyWall = heavyWall;
                HeavyArch = heavyArch;
                HeavyWallLight = heavyWallLight;
                HeavyRailing = heavyRailing;
                StyledFloor = styledFloor;
                StyledWall = styledWall;
                StyledCeilingLight = styledCeilingLight;
                StyledJoint = styledJoint;
            }

            public GameObject HeavyFloor { get; }

            public GameObject HeavyWall { get; }

            public GameObject HeavyArch { get; }

            public GameObject HeavyWallLight { get; }

            public GameObject HeavyRailing { get; }

            public GameObject StyledFloor { get; }

            public GameObject StyledWall { get; }

            public GameObject StyledCeilingLight { get; }

            public GameObject StyledJoint { get; }

            public static CorridorAssetSet Load()
            {
                return new CorridorAssetSet(
                    LoadPrefab(HeavyStationFloorPrefabPath),
                    LoadPrefab(HeavyStationWallPrefabPath),
                    LoadPrefab(HeavyStationArchPrefabPath),
                    LoadPrefab(HeavyStationLightPrefabPath),
                    LoadPrefab(HeavyStationRailingPrefabPath),
                    LoadPrefab(StyledFloorPrefabPath),
                    LoadPrefab(StyledSolidWallPrefabPath),
                    LoadPrefab(StyledCeilingLightPrefabPath),
                    LoadPrefab(StyledJointPrefabPath));
            }

            private static GameObject LoadPrefab(string path)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing corridor dressing prefab: " + path);
                }

                return prefab;
            }
        }

        private readonly struct RoomDressingRoot
        {
            public RoomDressingRoot(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }

            public string Name { get; }

            public Vector3 Position { get; }
        }
    }
}
