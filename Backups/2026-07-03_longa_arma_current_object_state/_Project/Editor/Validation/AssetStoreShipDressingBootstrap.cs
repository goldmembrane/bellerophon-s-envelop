using System;
using System.Collections.Generic;
using System.IO;
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
        public const string ApprovedFloorBasePlateModelPath = HeavyStationKitPath + "/BASE/Meshes/Floors/Floor_5_base_Plate.fbx";
        public const string ApprovedFloorBase1FPrefabPath = HeavyStationKitPath + "/BASE/Prefabs/Floors/Floor Base 1 F.prefab";
        public const string ApprovedWall2ModelPath = ScifiOfficeLitePath + "/Meshes/Walls/Wall 2.FBX";
        public const string ApprovedWallPillarPrefabPath = ScifiOfficeLitePath + "/Prefabs/Wall/Wall Pillar.prefab";
        public const string ApprovedCeilingPrefabPath = HeavyStationKitPath + "/BASE/Prefabs/Top-Bottom/TB_2.prefab";
        public const string ApprovedCeilingDetailPrefabPath = ScifiOfficeLitePath + "/Prefabs/Wall/Wall Top Piece.prefab";

        private const string MaterialDirectory = "Assets/_Project/Art/Ship/Materials";
        private const string ApprovedBaseFloorMaterialPath = MaterialDirectory + "/ApprovedCorridor_Floor5BasePlate.mat";
        private const string ApprovedTopFloorMaterialPath = MaterialDirectory + "/ApprovedCorridor_FloorBase1F.mat";
        private const string ApprovedWallMaterialPath = MaterialDirectory + "/ApprovedCorridor_Wall2.mat";
        private const string ApprovedWallBackerMaterialPath = MaterialDirectory + "/ApprovedCorridor_Wall2HiddenBacker.mat";
        private const string ApprovedHorizontalWallBaseMaterialPath = MaterialDirectory + "/ApprovedCorridor_HorizontalWallBase.mat";
        private const string ApprovedHorizontalWallBandMaterialPath = MaterialDirectory + "/ApprovedCorridor_HorizontalWallDarkBand.mat";
        private const string ApprovedHorizontalWallTrimMaterialPath = MaterialDirectory + "/ApprovedCorridor_HorizontalWallTrim.mat";
        private const string ApprovedCeilingMaterialPath = MaterialDirectory + "/ApprovedCorridor_TB2Ceiling.mat";
        private const string ApprovedCeilingTrimMaterialPath = MaterialDirectory + "/ApprovedCorridor_CeilingTrim.mat";
        private const string ApprovedFrameMaterialPath = MaterialDirectory + "/ApprovedCorridor_DarkFrame.mat";
        private const string ApprovedDarkSeamMaterialPath = MaterialDirectory + "/ApprovedCorridor_DarkSeam.mat";
        private const string ApprovedEdgeWearMaterialPath = MaterialDirectory + "/ApprovedCorridor_EdgeWear.mat";
        private const string ApprovedAmberLightMaterialPath = MaterialDirectory + "/ApprovedCorridor_AmberLight.mat";

        private const string FloorAlbedoPath = HeavyStationKitPath + "/BASE/Textures/Floors/B2_Floors_A.png";
        private const string FloorNormalPath = HeavyStationKitPath + "/BASE/Textures/Floors/B2_Floors_N.png";
        private const string WallAlbedoPath = ScifiOfficeLitePath + "/Meshes/Textures/Environment/Wall texture/Wall set 2/Wall_Multiset_2_Diffuse.png";
        private const string WallNormalPath = ScifiOfficeLitePath + "/Meshes/Textures/Environment/Wall texture/Wall set 2/Wall_Multiset_2_Normal.png";
        private const string CeilingAlbedoPath = HeavyStationKitPath + "/BASE/Textures/Top-Bottom/B2_Top_Bottom_A.png";
        private const string CeilingNormalPath = HeavyStationKitPath + "/BASE/Textures/Top-Bottom/B2_Top_Bottom_N.png";

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
            var removedStage3GameplayPropRoots = DeleteNamedObjects(PostDetailedStage3GameplayPropsBootstrap.Stage3RootName);
            var hiddenStage3CargoStartRenderers = HideNamedObjectRenderers(PostDetailedStage3GameplayPropsBootstrap.CargoStartCorridorDressingName);
            var disabledLegacyClearanceColliders = DisableLegacyCorridorClearanceColliders();

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
            Debug.Log(
                "Asset Store ship dressing step 2 corridors are ready. HiddenLegacyCorridorRenderers=" +
                hiddenLegacyCorridorRenderers +
                "; RemovedStage3GameplayPropRoots=" +
                removedStage3GameplayPropRoots +
                "; HiddenStage3CargoStartRenderers=" +
                hiddenStage3CargoStartRenderers +
                "; DisabledLegacyClearanceColliders=" +
                disabledLegacyClearanceColliders);
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
                var startBranchSide = i > 0 ? SegmentBranchSide(route[i - 1] - route[i], route[i + 1] - route[i]) : 0;
                var endBranchSide = i < route.Count - 2 ? SegmentBranchSide(route[i + 2] - route[i + 1], route[i + 1] - route[i]) : 0;
                CreateSegmentDressing(parent, assets, materials, from, to, i + 1, route[i], route[i + 1], startBranchSide, endBranchSide);
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
            Vector3 end,
            int startBranchSide,
            int endBranchSide)
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
            var floorNormal = floorRotation * Vector3.up;
            var right = Vector3.Cross(Vector3.up, flatDirection).normalized;
            var panelCount = Mathf.Max(1, Mathf.CeilToInt(length / 1.18f));
            var panelLength = length / panelCount;

            for (var i = 0; i < panelCount; i++)
            {
                var t = (i + 0.5f) / panelCount;
                var center = Vector3.Lerp(start, end, t);
                var startBranchClearancePanel = startBranchSide != 0 && i < 2;
                var endBranchClearancePanel = endBranchSide != 0 && i >= panelCount - 2;
                InstantiateFittedPrefab(
                    assets.FloorBasePlate,
                    "HSK Floor_5_base_Plate - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1),
                    parent,
                    center + (floorNormal * 0.04f),
                    floorRotation,
                    new Vector3(3.18f, 0.075f, Mathf.Max(0.34f, panelLength * 0.92f)),
                    materials.BaseFloor);

                for (var side = -1; side <= 1; side += 2)
                {
                    var branchClearancePanel =
                        (startBranchClearancePanel && side == startBranchSide) ||
                        (endBranchClearancePanel && side == endBranchSide);
                    if (branchClearancePanel)
                    {
                        continue;
                    }

                    CreateSolidPanel(
                        "Project Opaque Wall Backing - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center + (right * (side * 1.69f)) + new Vector3(0f, 1.16f, 0f),
                        yawRotation,
                        new Vector3(0.04f, 1.78f, Mathf.Max(0.28f, panelLength * 0.86f)),
                        materials.WallBacker);

                    InstantiateFittedPrefab(
                        assets.Wall2,
                        "SOL Wall 2 Unified Panel - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center + (right * (side * 1.58f)) + new Vector3(0f, 1.16f, 0f),
                        yawRotation,
                        new Vector3(0.16f, 1.9f, Mathf.Max(0.3f, panelLength * 0.88f)),
                        materials.Wall);

                    CreateApprovedHorizontalWallBands(
                        "Project Approved Horizontal Wall Band - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center,
                        right,
                        side,
                        1.2f,
                        Mathf.Max(0.3f, panelLength * 0.88f),
                        yawRotation,
                        materials);
                }

                InstantiateFittedPrefab(
                    assets.Ceiling,
                    "HSK TB_2 Cargo Ceiling - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1),
                    parent,
                    center + new Vector3(0f, 2.42f, 0f),
                    floorRotation,
                    new Vector3(3.25f, 0.32f, Mathf.Max(0.34f, panelLength * 0.95f)),
                    materials.Ceiling);

                CreateSolidPanel(
                    "Project Opaque Ceiling Cap - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1),
                    parent,
                    center + new Vector3(0f, 2.62f, 0f),
                    floorRotation,
                    new Vector3(3.38f, 0.08f, Mathf.Max(0.36f, panelLength * 0.98f)),
                    materials.Ceiling);

                for (var side = -1; side <= 1; side += 2)
                {
                    CreateSolidPanel(
                        "Project Opaque Ceiling Side Skirt - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center + (right * (side * 1.64f)) + new Vector3(0f, 2.42f, 0f),
                        yawRotation,
                        new Vector3(0.075f, 0.5f, Mathf.Max(0.34f, panelLength * 0.94f)),
                        materials.Frame);

                    InstantiateFittedPrefab(
                        assets.CeilingDetail,
                        "SOL Wall Top Piece Ceiling Rail - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1) + " Side " + side,
                        parent,
                        center + (right * (side * 1.18f)) + new Vector3(0f, 2.18f, 0f),
                        yawRotation * Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f),
                        new Vector3(0.18f, 0.22f, Mathf.Max(0.3f, panelLength * 0.89f)),
                        materials.CeilingTrim);
                }
            }

            for (var i = 0; i <= panelCount; i++)
            {
                var t = i / (float)panelCount;
                var center = Vector3.Lerp(start, end, t);
                var skipSide = 0;
                if (startBranchSide != 0 && i <= 2)
                {
                    skipSide = startBranchSide;
                }
                else if (endBranchSide != 0 && i >= panelCount - 2)
                {
                    skipSide = endBranchSide;
                }

                CreateCorridorSeamDressing(parent, assets, materials, from, to, segmentIndex, i, center, right, yawRotation, floorRotation, floorNormal, skipSide);
            }

            CreateDenseFloorOverlay(parent, assets, materials, from, to, segmentIndex, start, end, floorRotation, floorNormal, right);
            CreateApprovedCorridorWallFillLights(parent, from, to, segmentIndex, start, end);
        }

        private static void CreateCorridorSeamDressing(
            Transform parent,
            CorridorAssetSet assets,
            CorridorDressingMaterials materials,
            string from,
            string to,
            int segmentIndex,
            int seamIndex,
            Vector3 center,
            Vector3 right,
            Quaternion yawRotation,
            Quaternion floorRotation,
            Vector3 floorNormal,
            int skipSide)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                if (side == skipSide)
                {
                    continue;
                }

                InstantiateFittedPrefab(
                    assets.WallPillar,
                    "SOL Wall Pillar Unified Seam - " + from + " to " + to + " S" + segmentIndex + "-" + seamIndex + " Side " + side,
                    parent,
                    center + (right * (side * 1.59f)) + new Vector3(0f, 1.15f, 0f),
                    side < 0 ? yawRotation : yawRotation * Quaternion.Euler(0f, 180f, 0f),
                    new Vector3(0.11f, 1.9f, 0.12f),
                    materials.Frame);
            }

            CreateSolidPanel(
                "Clean floor seam rib - " + from + " to " + to + " S" + segmentIndex + "-" + seamIndex,
                parent,
                center + (floorNormal * 0.2f),
                floorRotation,
                new Vector3(3.02f, 0.045f, 0.055f),
                materials.EdgeWear);
            CreateSolidPanel(
                "Clean ceiling seam rib - " + from + " to " + to + " S" + segmentIndex + "-" + seamIndex,
                parent,
                center + new Vector3(0f, 2.23f, 0f),
                yawRotation,
                new Vector3(3.02f, 0.075f, 0.055f),
                materials.DarkSeam);
            CreateSolidPanel(
                "Project Opaque Ceiling Seam Cap - " + from + " to " + to + " S" + segmentIndex + "-" + seamIndex,
                parent,
                center + new Vector3(0f, 2.62f, 0f),
                floorRotation,
                new Vector3(3.38f, 0.08f, 0.14f),
                materials.Ceiling);
        }

        private static void CreateDenseFloorOverlay(
            Transform parent,
            CorridorAssetSet assets,
            CorridorDressingMaterials materials,
            string from,
            string to,
            int segmentIndex,
            Vector3 start,
            Vector3 end,
            Quaternion floorRotation,
            Vector3 floorNormal,
            Vector3 right)
        {
            const int columnCount = 8;
            const float columnSpacing = 0.38f;
            const float rowSpacing = 0.16f;
            const float firstColumnOffset = -1.33f;
            const float targetWidth = 0.74f;
            const float targetDepth = 0.19f;
            const float topOffset = 0.19f;

            var delta = end - start;
            var length = delta.magnitude;
            var direction = delta.normalized;
            var rowCount = Mathf.Max(1, Mathf.CeilToInt(length / rowSpacing));
            for (var row = 0; row < rowCount; row++)
            {
                var distance = Mathf.Min(length, (row + 0.5f) * rowSpacing);
                var center = start + (direction * distance);
                for (var column = 0; column < columnCount; column++)
                {
                    var lateralOffset = firstColumnOffset + (column * columnSpacing);
                    InstantiateFittedPrefab(
                        assets.FloorBase1F,
                        "HSK Floor Base 1 F Dense Overlay - " + from + " to " + to + " S" + segmentIndex + " R" + row + " C" + column,
                        parent,
                        center + (right * lateralOffset) + (floorNormal * topOffset),
                        floorRotation,
                        new Vector3(targetWidth, 0.055f, targetDepth),
                        materials.TopFloor);
                }
            }
        }

        private static void CreateApprovedCorridorWallFillLights(
            Transform parent,
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

            var direction = delta.normalized;
            var lightCount = Mathf.Max(2, Mathf.CeilToInt(length / 5.5f));
            for (var i = 0; i < lightCount; i++)
            {
                var distance = ((i + 0.5f) / lightCount) * length;
                var lightObject = new GameObject(
                    "Approved Corridor Wall Fill Light - " + from + " to " + to + " S" + segmentIndex + "-" + (i + 1));
                lightObject.transform.SetParent(parent, false);
                lightObject.transform.position = start + (direction * distance) + new Vector3(0f, 1.72f, 0f);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.79f, 0.52f, 1f);
                light.intensity = 0.42f;
                light.range = 4.2f;
                light.shadows = LightShadows.None;
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
                assets.FloorBasePlate,
                "HSK Floor_5_base_Plate Joint Mask - " + from + " to " + to + " J" + jointIndex,
                parent,
                joint + new Vector3(0f, 0.07f, 0f),
                rotation,
                new Vector3(3.36f, 0.075f, 1.28f),
                materials.BaseFloor);

            for (var row = 0; row < 8; row++)
            {
                var forwardOffset = -0.62f + (row * 0.18f);
                for (var column = 0; column < 8; column++)
                {
                    var lateralOffset = -1.33f + (column * 0.38f);
                    InstantiateFittedPrefab(
                        assets.FloorBase1F,
                        "HSK Floor Base 1 F Dense Joint Overlay - " + from + " to " + to + " J" + jointIndex + " R" + row + " C" + column,
                        parent,
                        joint + (direction * forwardOffset) + (right * lateralOffset) + new Vector3(0f, 0.19f, 0f),
                        rotation,
                        new Vector3(0.74f, 0.055f, 0.19f),
                        materials.TopFloor);
                }
            }

            for (var side = -1; side <= 1; side += 2)
            {
                CreateSolidPanel(
                    "Project Opaque Joint Backing - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint + (right * (side * 1.69f)) + new Vector3(0f, 1.16f, 0f),
                    rotation,
                    new Vector3(0.04f, 1.78f, 1.22f),
                    materials.WallBacker);

                InstantiateFittedPrefab(
                    assets.Wall2,
                    "SOL Wall 2 Unified Panel Joint - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint + (right * (side * 1.58f)) + new Vector3(0f, 1.16f, 0f),
                    rotation,
                    new Vector3(0.16f, 1.9f, 1.08f),
                    materials.Wall);

                CreateApprovedHorizontalWallBands(
                    "Project Approved Horizontal Wall Band Joint - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint,
                    right,
                    side,
                    1.18f,
                    1.08f,
                    rotation,
                    materials);

                InstantiateFittedPrefab(
                    assets.WallPillar,
                    "SOL Wall Pillar Unified Joint Seam - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint + (right * (side * 1.59f)) + new Vector3(0f, 1.15f, 0f),
                    side < 0 ? rotation : rotation * Quaternion.Euler(0f, 180f, 0f),
                    new Vector3(0.11f, 1.9f, 0.12f),
                    materials.Frame);
            }

            InstantiateFittedPrefab(
                assets.Ceiling,
                "HSK TB_2 Joint Ceiling - " + from + " to " + to + " J" + jointIndex,
                parent,
                joint + new Vector3(0f, 2.42f, 0f),
                rotation,
                new Vector3(3.25f, 0.32f, 1.12f),
                materials.Ceiling);
            CreateSolidPanel(
                "Project Opaque Joint Ceiling Cap - " + from + " to " + to + " J" + jointIndex,
                parent,
                joint + new Vector3(0f, 2.62f, 0f),
                rotation,
                new Vector3(3.42f, 0.08f, 1.28f),
                materials.Ceiling);
            for (var side = -1; side <= 1; side += 2)
            {
                CreateSolidPanel(
                    "Project Opaque Joint Ceiling Side Skirt - " + from + " to " + to + " J" + jointIndex + " Side " + side,
                    parent,
                    joint + (right * (side * 1.64f)) + new Vector3(0f, 2.42f, 0f),
                    rotation,
                    new Vector3(0.075f, 0.5f, 1.18f),
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
                assets.FloorBasePlate,
                "HSK Floor_5_base_Plate Threshold Lip - " + from + " to " + to + " " + label,
                parent,
                basePosition + new Vector3(0f, 0.08f, 0f),
                rotation,
                new Vector3(3.35f, 0.075f, 0.72f),
                materials.BaseFloor);

            for (var side = -1; side <= 1; side += 2)
            {
                CreateSolidPanel(
                    "Project Opaque Threshold Post Backing - " + from + " to " + to + " " + label + " Side " + side,
                    parent,
                    basePosition + (right * (side * 1.78f)) + new Vector3(0f, 1.16f, 0f),
                    rotation,
                    new Vector3(0.14f, 2.18f, 0.34f),
                    materials.WallBacker);

                InstantiateFittedPrefab(
                    assets.WallPillar,
                    "SOL Threshold Side Post - " + from + " to " + to + " " + label + " Side " + side,
                    parent,
                    basePosition + (right * (side * 1.64f)) + new Vector3(0f, 1.16f, 0f),
                    side < 0 ? rotation : rotation * Quaternion.Euler(0f, 180f, 0f),
                    new Vector3(0.22f, 2.22f, 0.22f),
                    materials.Frame);

                CreateSolidPanel(
                    "Approved Threshold Amber Side Light - " + from + " to " + to + " " + label + " Side " + side,
                    parent,
                    basePosition + (right * (side * 1.4f)) + new Vector3(0f, 1.48f, -0.03f),
                    rotation,
                    new Vector3(0.045f, 0.42f, 0.045f),
                    materials.AmberLight);
            }

            CreateSolidPanel(
                "Project Opaque Threshold Top Backing - " + from + " to " + to + " " + label,
                parent,
                basePosition + new Vector3(0f, 2.22f, 0f),
                rotation,
                new Vector3(3.38f, 0.18f, 0.24f),
                materials.Frame);

            CreateSolidPanel(
                "Approved Threshold Top Lintel - " + from + " to " + to + " " + label,
                parent,
                basePosition + new Vector3(0f, 2.32f, 0f),
                rotation,
                new Vector3(3.42f, 0.14f, 0.22f),
                materials.Frame);

            CreateSolidPanel(
                "Project Opaque Threshold Ceiling Cap - " + from + " to " + to + " " + label,
                parent,
                basePosition + new Vector3(0f, 2.62f, 0f),
                rotation,
                new Vector3(3.46f, 0.08f, 0.5f),
                materials.Ceiling);
        }

        private static void CreateApprovedHorizontalWallBands(
            string name,
            Transform parent,
            Vector3 center,
            Vector3 right,
            int side,
            float centerY,
            float length,
            Quaternion rotation,
            CorridorDressingMaterials materials,
            float halfWidth = 1.59f)
        {
            var wallCenter = center + (right * (side * halfWidth));
            var inward = right * -side;
            CreateSolidPanel(
                name + " Upper Shadow Band",
                parent,
                wallCenter + (inward * 0.072f) + new Vector3(0f, centerY + 0.42f, 0f),
                rotation,
                new Vector3(0.035f, 0.34f, length + 0.018f),
                materials.HorizontalWallBand);
            CreateSolidPanel(
                name + " Center Light Trim",
                parent,
                wallCenter + (inward * 0.074f) + new Vector3(0f, centerY + 0.17f, 0f),
                rotation,
                new Vector3(0.032f, 0.055f, length + 0.024f),
                materials.HorizontalWallTrim);
            CreateSolidPanel(
                name + " Lower Dark Band",
                parent,
                wallCenter + (inward * 0.073f) + new Vector3(0f, centerY - 0.36f, 0f),
                rotation,
                new Vector3(0.034f, 0.3f, length + 0.02f),
                materials.HorizontalWallBand);
            CreateSolidPanel(
                name + " Lower Light Trim",
                parent,
                wallCenter + (inward * 0.074f) + new Vector3(0f, centerY - 0.58f, 0f),
                rotation,
                new Vector3(0.032f, 0.055f, length + 0.024f),
                materials.HorizontalWallTrim);
        }

        private static GameObject CreateSolidPanel(
            string name,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 localScale,
            Material material,
            bool enableCollider = false)
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
                collider.enabled = enableCollider;
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

        private static int SegmentBranchSide(Vector3 branchDirection, Vector3 segmentDirection)
        {
            var flatSegment = FlatDirection(segmentDirection);
            var flatBranch = FlatDirection(branchDirection);
            var right = Vector3.Cross(Vector3.up, flatSegment).normalized;
            var dot = Vector3.Dot(flatBranch, right);
            if (Mathf.Abs(dot) < 0.35f)
            {
                return 0;
            }

            return dot > 0f ? 1 : -1;
        }

        private static Quaternion Wall2RotationForRoute(string from, string to, Quaternion corridorRotation, int side)
        {
            return side < 0 ? corridorRotation : corridorRotation * Quaternion.Euler(0f, 180f, 0f);
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

        private static int HideNamedObjectRenderers(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                return 0;
            }

            var count = 0;
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    renderers[i].enabled = false;
                    count++;
                }
            }

            return count;
        }

        private static int DeleteNamedObjects(string objectName)
        {
            var matches = new List<GameObject>();
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == objectName)
                {
                    matches.Add(transforms[i].gameObject);
                }
            }

            for (var i = 0; i < matches.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(matches[i]);
            }

            return matches.Count;
        }

        private static int DisableLegacyCorridorClearanceColliders()
        {
            var grayboxRoot = GameObject.Find(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (grayboxRoot == null)
            {
                return 0;
            }

            var count = 0;
            var colliders = grayboxRoot.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                if (!IsLegacyCorridorClearanceCollider(collider.gameObject.name))
                {
                    continue;
                }

                collider.enabled = false;
                count++;
            }

            return count;
        }

        private static bool IsLegacyCorridorClearanceCollider(string objectName)
        {
            if (!objectName.StartsWith("Corridor - ", StringComparison.Ordinal))
            {
                return false;
            }

            return objectName.Contains(" Mouth Closure Wall", StringComparison.Ordinal) ||
                   objectName.Contains(" Upper Bulkhead Wall", StringComparison.Ordinal) ||
                   (objectName.Contains(" Joint ", StringComparison.Ordinal) &&
                    objectName.Contains(" Closure Wall", StringComparison.Ordinal));
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
                Material baseFloor,
                Material topFloor,
                Material wall,
                Material wallBacker,
                Material horizontalWallBase,
                Material horizontalWallBand,
                Material horizontalWallTrim,
                Material ceiling,
                Material ceilingTrim,
                Material frame,
                Material darkSeam,
                Material edgeWear,
                Material amberLight)
            {
                BaseFloor = baseFloor;
                TopFloor = topFloor;
                Wall = wall;
                WallBacker = wallBacker;
                HorizontalWallBase = horizontalWallBase;
                HorizontalWallBand = horizontalWallBand;
                HorizontalWallTrim = horizontalWallTrim;
                Ceiling = ceiling;
                CeilingTrim = ceilingTrim;
                Frame = frame;
                DarkSeam = darkSeam;
                EdgeWear = edgeWear;
                AmberLight = amberLight;
            }

            public Material BaseFloor { get; }

            public Material TopFloor { get; }

            public Material Wall { get; }

            public Material WallBacker { get; }

            public Material HorizontalWallBase { get; }

            public Material HorizontalWallBand { get; }

            public Material HorizontalWallTrim { get; }

            public Material Ceiling { get; }

            public Material CeilingTrim { get; }

            public Material Frame { get; }

            public Material DarkSeam { get; }

            public Material EdgeWear { get; }

            public Material AmberLight { get; }

            public static CorridorDressingMaterials Load()
            {
                Directory.CreateDirectory(MaterialDirectory);
                return new CorridorDressingMaterials(
                    EnsureTexturedMaterial(ApprovedBaseFloorMaterialPath, FloorAlbedoPath, FloorNormalPath, new Color(0.42f, 0.44f, 0.39f, 1f), 0.3f, 0.16f, false),
                    EnsureTexturedMaterial(ApprovedTopFloorMaterialPath, FloorAlbedoPath, FloorNormalPath, new Color(0.58f, 0.57f, 0.5f, 1f), 0.35f, 0.18f, false),
                    EnsureTexturedMaterial(ApprovedWallMaterialPath, WallAlbedoPath, WallNormalPath, new Color(0.68f, 0.68f, 0.62f, 1f), 0.12f, 0.22f, false),
                    EnsureSolidMaterial(ApprovedWallBackerMaterialPath, new Color(0.32f, 0.34f, 0.32f, 1f), 0.12f, 0.12f, false),
                    EnsureSolidMaterial(ApprovedHorizontalWallBaseMaterialPath, new Color(0.48f, 0.49f, 0.44f, 1f), 0.12f, 0.18f, false),
                    EnsureSolidMaterial(ApprovedHorizontalWallBandMaterialPath, new Color(0.075f, 0.078f, 0.072f, 1f), 0.18f, 0.12f, false),
                    EnsureSolidMaterial(ApprovedHorizontalWallTrimMaterialPath, new Color(0.72f, 0.7f, 0.62f, 1f), 0.08f, 0.2f, false),
                    EnsureTexturedMaterial(ApprovedCeilingMaterialPath, CeilingAlbedoPath, CeilingNormalPath, new Color(0.42f, 0.43f, 0.39f, 1f), 0.24f, 0.14f, false),
                    EnsureSolidMaterial(ApprovedCeilingTrimMaterialPath, new Color(0.09f, 0.095f, 0.085f, 1f), 0.2f, 0.12f, false),
                    EnsureSolidMaterial(ApprovedFrameMaterialPath, new Color(0.075f, 0.08f, 0.075f, 1f), 0.2f, 0.1f, false),
                    EnsureSolidMaterial(ApprovedDarkSeamMaterialPath, new Color(0.018f, 0.018f, 0.015f, 1f), 0f, 0.05f, false),
                    EnsureSolidMaterial(ApprovedEdgeWearMaterialPath, new Color(0.42f, 0.4f, 0.34f, 1f), 0.1f, 0.2f, false),
                    EnsureSolidMaterial(ApprovedAmberLightMaterialPath, new Color(1f, 0.53f, 0.16f, 1f), 0f, 0.25f, true));
            }

            private static Material EnsureTexturedMaterial(
                string materialPath,
                string albedoPath,
                string normalPath,
                Color tint,
                float metallic,
                float smoothness,
                bool emissive)
            {
                var material = EnsureSolidMaterial(materialPath, tint, metallic, smoothness, emissive);
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
                if (albedo != null)
                {
                    ApplyTexture(material, "_BaseMap", albedo);
                    ApplyTexture(material, "_MainTex", albedo);
                }

                var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal != null)
                {
                    ApplyTexture(material, "_BumpMap", normal);
                    ApplyFloat(material, "_BumpScale", 0.8f);
                    material.EnableKeyword("_NORMALMAP");
                }

                EditorUtility.SetDirty(material);
                return material;
            }

            private static Material EnsureSolidMaterial(
                string path,
                Color color,
                float metallic,
                float smoothness,
                bool emissive)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(FindLitShader());
                    AssetDatabase.CreateAsset(material, path);
                }
                else
                {
                    var shader = FindLitShader();
                    if (material.shader != shader)
                    {
                        material.shader = shader;
                    }
                }

                ApplyColor(material, color);
                ClearTexture(material, "_BaseMap");
                ClearTexture(material, "_MainTex");
                ClearTexture(material, "_BumpMap");
                material.DisableKeyword("_NORMALMAP");
                ApplyFloat(material, "_Metallic", metallic);
                ApplyFloat(material, "_Smoothness", smoothness);
                ApplyFloat(material, "_Glossiness", smoothness);
                if (emissive)
                {
                    material.EnableKeyword("_EMISSION");
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", color * 1.75f);
                    }
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                }

                EditorUtility.SetDirty(material);
                return material;
            }

            private static Shader FindLitShader()
            {
                return Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard") ??
                    Shader.Find("Unlit/Texture");
            }

            private static void ApplyColor(Material material, Color color)
            {
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }
            }

            private static void ClearTexture(Material material, string property)
            {
                if (material.HasProperty(property))
                {
                    material.SetTexture(property, null);
                }
            }

            private static void ApplyTexture(Material material, string propertyName, Texture texture)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                }
            }

            private static void ApplyFloat(Material material, string propertyName, float value)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                }
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
                GameObject floorBasePlate,
                GameObject floorBase1F,
                GameObject wall2,
                GameObject wallPillar,
                GameObject ceiling,
                GameObject ceilingDetail)
            {
                FloorBasePlate = floorBasePlate;
                FloorBase1F = floorBase1F;
                Wall2 = wall2;
                WallPillar = wallPillar;
                Ceiling = ceiling;
                CeilingDetail = ceilingDetail;
            }

            public GameObject FloorBasePlate { get; }

            public GameObject FloorBase1F { get; }

            public GameObject Wall2 { get; }

            public GameObject WallPillar { get; }

            public GameObject Ceiling { get; }

            public GameObject CeilingDetail { get; }

            public static CorridorAssetSet Load()
            {
                return new CorridorAssetSet(
                    LoadPrefab(ApprovedFloorBasePlateModelPath),
                    LoadPrefab(ApprovedFloorBase1FPrefabPath),
                    LoadPrefab(ApprovedWall2ModelPath),
                    LoadPrefab(ApprovedWallPillarPrefabPath),
                    LoadPrefab(ApprovedCeilingPrefabPath),
                    LoadPrefab(ApprovedCeilingDetailPrefabPath));
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
