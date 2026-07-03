using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellerophon.Editor.Validation
{
    public static class AssetDressingStep02SampleRenderer
    {
        private const string OutputRootRelativePath = "artSample/asset_dressing_samples/step02_corridors_thresholds_2026-06-13";
        private const string WornOutputRootRelativePath = "artSample/asset_dressing_samples/step02_corridors_thresholds_worn_2026-06-13";
        private const string SteelPlateOutputRootRelativePath = "artSample/asset_dressing_samples/step02_corridors_thresholds_steel_plate_2026-06-14";
        private const string BumpyWornPlateOutputRootRelativePath = "artSample/asset_dressing_samples/step02_corridors_thresholds_bumpy_worn_plate_2026-06-14";

        private const string HeavyFloorPrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Floors/Floor Base 3.prefab";
        private const string HeavySteelPlatePrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Floors/Floor_5_base_Plate.prefab";
        private const string HeavyWallRibPrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Walls/1 Wall/W1_D0.prefab";
        private const string HeavyWideWallRibPrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Walls/2 Walls/W2_D0.prefab";
        private const string HeavyWallLightPrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Walls/Wall Lights/Wall Lights On.prefab";
        private const string HeavyRailingPrefabPath = "Assets/Heavy Station Kit/BASE/Prefabs/Floors Fill/_Handrails/St_Base2_Railing.prefab";
        private const string StyledStraightCorridorPrefabPath = "Assets/Sci-Fi Styled Modular Pack/Prefabs/Corridors/Corridor_I.prefab";
        private const string StyledBlankWallPrefabPath = "Assets/Sci-Fi Styled Modular Pack/Prefabs/Walls/Simple/blank_wall_A.prefab";
        private const string StyledCeilingLightPrefabPath = "Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_celing_1.prefab";
        private const string StyledJointPrefabPath = "Assets/Sci-Fi Styled Modular Pack/Prefabs/Joints/Joint_X_6.prefab";

        private const string CorridorFloorMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorCorridorFloor_Rough.mat";
        private const string CorridorWallMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorWall_Rough.mat";
        private const string CorridorCeilingMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorCeiling_Rough.mat";
        private const string CorridorFrameMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorDoorFrame_Worn.mat";
        private const string CorridorRailMaterialPath = "Assets/_Project/Art/Ship/Materials/ShipInteriorCableTray_Dark.mat";
        private const string CorridorLightMaterialPath = "Assets/_Project/Art/Ship/Materials/Stage3Light_Cyan.mat";
        private const string HeavyCommonPlateAlbedoTexturePath = "Assets/Heavy Station Kit/_common/Textures/plate_D.png";
        private const string HeavyCommonPlateNormalTexturePath = "Assets/Heavy Station Kit/_common/Textures/plate_N.png";
        private const string HeavyCommonPlateSpecTexturePath = "Assets/Heavy Station Kit/_common/Textures/plate_S.png";

        private const int PreviewLayer = 31;
        private const int CornerPreviewLayer = 30;

        private enum SampleStyle
        {
            Standard,
            Worn,
            SteelPlate,
            BumpyWornPlate
        }

        [MenuItem("Bellerophon/Validation/Capture Asset Dressing Step 02 Sample")]
        public static void Capture()
        {
            CaptureTo(OutputRootRelativePath, SampleStyle.Standard, "Asset dressing step 02 sample renders saved:");
        }

        [MenuItem("Bellerophon/Validation/Capture Asset Dressing Step 02 Worn Sample")]
        public static void CaptureWorn()
        {
            CaptureTo(WornOutputRootRelativePath, SampleStyle.Worn, "Asset dressing step 02 worn sample renders saved:");
        }

        [MenuItem("Bellerophon/Validation/Capture Asset Dressing Step 02 Steel Plate Sample")]
        public static void CaptureSteelPlate()
        {
            CaptureTo(SteelPlateOutputRootRelativePath, SampleStyle.SteelPlate, "Asset dressing step 02 steel plate sample renders saved:");
        }

        [MenuItem("Bellerophon/Validation/Capture Asset Dressing Step 02 Bumpy Worn Plate Sample")]
        public static void CaptureBumpyWornPlate()
        {
            CaptureTo(BumpyWornPlateOutputRootRelativePath, SampleStyle.BumpyWornPlate, "Asset dressing step 02 bumpy worn plate sample renders saved:");
        }

        private static void CaptureTo(string outputRootRelativePath, SampleStyle style, string successMarker)
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for asset dressing sample output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, outputRootRelativePath);
            Directory.CreateDirectory(outputRoot);

            var previewRoot = BuildPreviewScene(style);
            try
            {
                CaptureView(
                    Path.Combine(outputRoot, "view_01_player_entry.png"),
                    new Vector3(0f, 1.38f, -6.1f),
                    new Vector3(0f, 1.12f, 3.7f),
                    48f,
                    false,
                    6f,
                    1 << PreviewLayer);
                CaptureView(
                    Path.Combine(outputRoot, "view_02_threshold_diagonal.png"),
                    new Vector3(-3.25f, 1.68f, -5.35f),
                    new Vector3(0f, 1.08f, 0.65f),
                    57f,
                    false,
                    6f,
                    1 << PreviewLayer);
                CaptureView(
                    Path.Combine(outputRoot, "view_03_layout_topdown.png"),
                    new Vector3(3.4f, 13.2f, 4.2f),
                    new Vector3(3.4f, 0f, 4.2f),
                    45f,
                    true,
                    9.8f,
                    (1 << PreviewLayer) | (1 << CornerPreviewLayer));
                CaptureView(
                    Path.Combine(outputRoot, "view_04_module_stack.png"),
                    new Vector3(0.55f, 1.28f, 1.15f),
                    new Vector3(0f, 1.12f, 6.5f),
                    72f,
                    false,
                    6f,
                    1 << PreviewLayer);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewRoot);
            }

            AssetDatabase.Refresh();
            Debug.Log(successMarker + " " + outputRoot);
        }

        private static GameObject BuildPreviewScene(SampleStyle style)
        {
            var worn = style == SampleStyle.Worn;
            var steelPlate = style == SampleStyle.SteelPlate;
            var bumpyWornPlate = style == SampleStyle.BumpyWornPlate;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = bumpyWornPlate ? new Color(0.31f, 0.315f, 0.305f, 1f) : steelPlate ? new Color(0.38f, 0.4f, 0.39f, 1f) : worn ? new Color(0.25f, 0.26f, 0.255f, 1f) : new Color(0.36f, 0.39f, 0.4f, 1f);
            RenderSettings.fog = false;

            var root = new GameObject(GetRootName(style));
            root.layer = PreviewLayer;
            root.hideFlags = HideFlags.HideAndDontSave;

            var assets = PreviewAssets.Load();
            var materials = PreviewMaterials.Load(style);

            CreateStraightCorridor(root.transform, assets, materials, Vector3.zero, Quaternion.identity);
            CreateCornerCandidate(root.transform, assets, materials, new Vector3(9f, 0f, 4.35f), Quaternion.Euler(0f, 90f, 0f));
            if (steelPlate)
            {
                AddSteelPlateInterior(root.transform, assets, materials, Vector3.zero, Quaternion.identity);
                AddSteelPlateCornerInterior(root.transform, assets, materials, new Vector3(9f, 0f, 4.35f), Quaternion.Euler(0f, 90f, 0f));
            }
            else if (bumpyWornPlate)
            {
                AddBumpyWornPlateInterior(root.transform, assets, materials, Vector3.zero, Quaternion.identity);
                AddBumpyWornPlateCornerInterior(root.transform, assets, materials, new Vector3(9f, 0f, 4.35f), Quaternion.Euler(0f, 90f, 0f));
                AddHeavyWornAgingDetails(root.transform, materials);
            }

            if (worn)
            {
                AddWornAgingDetails(root.transform, materials);
            }
            CreateLighting(root.transform, style);
            return root;
        }

        private static string GetRootName(SampleStyle style)
        {
            switch (style)
            {
                case SampleStyle.Worn:
                    return "Step02 Worn Corridor Asset Sample";
                case SampleStyle.SteelPlate:
                    return "Step02 Steel Plate Corridor Asset Sample";
                case SampleStyle.BumpyWornPlate:
                    return "Step02 Bumpy Worn Plate Corridor Asset Sample";
                default:
                    return "Step02 Corridor Asset Sample";
            }
        }

        private static void CreateStraightCorridor(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation)
        {
            var corridorRotation = rotation * Quaternion.Euler(0f, 90f, 0f);
            for (var i = 0; i < 4; i++)
            {
                var z = i * 2.32f;
                var moduleCenter = origin + (rotation * new Vector3(0f, 1.08f, z));

                InstantiateFittedPrefab(
                    assets.StyledStraightCorridor,
                    "SMP Straight Corridor " + i,
                    root,
                    moduleCenter,
                    corridorRotation,
                    new Vector3(3.25f, 2.38f, 2.35f),
                    null,
                    materials);

                InstantiateFittedPrefab(
                    assets.HeavyFloor,
                    "HSK Floor Plate " + i,
                    root,
                    origin + (rotation * new Vector3(0f, 0.08f, z)),
                    rotation,
                    new Vector3(2.72f, 0.13f, 1.5f),
                    materials.Floor,
                    materials);

                if (i == 1 || i == 3)
                {
                    AddWallLightPair(root, assets, materials, origin, rotation, z);
                }

                if (i == 1)
                {
                    AddLowRailPair(root, assets, materials, origin, rotation, z);
                }

                InstantiateFittedPrefab(
                    assets.StyledCeilingLight,
                    "SMP Ceiling Light " + i,
                    root,
                    origin + (rotation * new Vector3(0f, 2.32f, z)),
                    rotation,
                    new Vector3(2.1f, 0.14f, 1.16f),
                    materials.Light,
                    materials);
            }

            AddOpaqueBackers(root, assets, materials, origin, rotation);
            CreateThreshold(root, assets, materials, origin + (rotation * new Vector3(0f, 0f, -1.65f)), rotation);
            CreateThreshold(root, assets, materials, origin + (rotation * new Vector3(0f, 0f, 7.15f)), rotation * Quaternion.Euler(0f, 180f, 0f));
        }

        private static void AddOpaqueBackers(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation)
        {
            for (var i = 0; i < 4; i++)
            {
                var z = i * 2.32f;
                InstantiateFittedPrefab(
                    assets.StyledBlankWall,
                    "SMP Left Solid Backer " + i,
                    root,
                    origin + (rotation * new Vector3(-1.88f, 1.08f, z)),
                    rotation,
                    new Vector3(0.12f, 1.9f, 1.55f),
                    materials.Wall,
                    materials);
                InstantiateFittedPrefab(
                    assets.StyledBlankWall,
                    "SMP Right Solid Backer " + i,
                    root,
                    origin + (rotation * new Vector3(1.88f, 1.08f, z)),
                    rotation,
                    new Vector3(0.12f, 1.9f, 1.55f),
                    materials.Wall,
                    materials);
            }
        }

        private static void CreateThreshold(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 position, Quaternion rotation)
        {
            InstantiateFittedPrefab(
                assets.HeavyWallRib,
                "HSK Threshold Left Post",
                root,
                position + (rotation * new Vector3(-1.52f, 1.08f, 0f)),
                rotation,
                new Vector3(0.18f, 2.12f, 0.48f),
                materials.Frame,
                materials);
            InstantiateFittedPrefab(
                assets.HeavyWallRib,
                "HSK Threshold Right Post",
                root,
                position + (rotation * new Vector3(1.52f, 1.08f, 0f)),
                rotation,
                new Vector3(0.18f, 2.12f, 0.48f),
                materials.Frame,
                materials);
            InstantiateFittedPrefab(
                assets.HeavyWideWallRib,
                "HSK Threshold Top Lintel",
                root,
                position + (rotation * new Vector3(0f, 2.18f, 0f)),
                rotation,
                new Vector3(3.1f, 0.22f, 0.44f),
                materials.Frame,
                materials);
            InstantiateFittedPrefab(
                assets.HeavyFloor,
                "HSK Threshold Floor Plate",
                root,
                position + (rotation * new Vector3(0f, 0.08f, 0f)),
                rotation,
                new Vector3(3f, 0.12f, 0.55f),
                materials.Floor,
                materials);
        }

        private static void CreateCornerCandidate(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation)
        {
            var corridorAlongZ = rotation * Quaternion.Euler(0f, 90f, 0f);
            var corridorAlongX = rotation;

            InstantiateFittedPrefab(
                assets.StyledStraightCorridor,
                "SMP Corner Incoming Corridor",
                root,
                origin + new Vector3(0f, 1.08f, -1.16f),
                corridorAlongZ,
                new Vector3(3.2f, 2.38f, 2.25f),
                null,
                materials,
                CornerPreviewLayer);
            InstantiateFittedPrefab(
                assets.StyledStraightCorridor,
                "SMP Corner Outgoing Corridor",
                root,
                origin + new Vector3(-1.16f, 1.08f, 0f),
                corridorAlongX,
                new Vector3(2.25f, 2.38f, 3.2f),
                null,
                materials,
                CornerPreviewLayer);
            InstantiateFittedPrefab(
                assets.StyledJoint,
                "SMP Joint Floor Mask",
                root,
                origin + new Vector3(0f, 0.09f, 0f),
                rotation,
                new Vector3(3.35f, 0.16f, 3.35f),
                materials.Floor,
                materials,
                CornerPreviewLayer);
            InstantiateFittedPrefab(
                assets.HeavyFloor,
                "HSK Corner Floor Plate A",
                root,
                origin + new Vector3(0f, 0.12f, 0.9f),
                rotation,
                new Vector3(2.4f, 0.12f, 1.25f),
                materials.Floor,
                materials,
                CornerPreviewLayer);
            InstantiateFittedPrefab(
                assets.HeavyFloor,
                "HSK Corner Floor Plate B",
                root,
                origin + new Vector3(-0.9f, 0.12f, 0f),
                rotation * Quaternion.Euler(0f, 90f, 0f),
                new Vector3(2.4f, 0.12f, 1.25f),
                materials.Floor,
                materials,
                CornerPreviewLayer);
            InstantiateFittedPrefab(
                assets.HeavyWideWallRib,
                "HSK Corner Inner Rib",
                root,
                origin + new Vector3(1.36f, 1.1f, -0.55f),
                rotation,
                new Vector3(0.16f, 1.95f, 1.75f),
                materials.Frame,
                materials,
                CornerPreviewLayer);
            InstantiateFittedPrefab(
                assets.HeavyWallLight,
                "HSK Corner Wall Light",
                root,
                origin + new Vector3(1.2f, 1.55f, -0.95f),
                rotation,
                new Vector3(0.28f, 0.28f, 0.28f),
                materials.Light,
                materials,
                CornerPreviewLayer);
        }

        private static void AddWallLightPair(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation, float z)
        {
            InstantiateFittedPrefab(
                assets.HeavyWallLight,
                "HSK Left Wall Light " + z,
                root,
                origin + (rotation * new Vector3(-1.36f, 1.48f, z)),
                rotation,
                new Vector3(0.3f, 0.3f, 0.3f),
                materials.Light,
                materials);
            InstantiateFittedPrefab(
                assets.HeavyWallLight,
                "HSK Right Wall Light " + z,
                root,
                origin + (rotation * new Vector3(1.36f, 1.48f, z)),
                rotation,
                new Vector3(0.3f, 0.3f, 0.3f),
                materials.Light,
                materials);
        }

        private static void AddLowRailPair(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation, float z)
        {
            InstantiateFittedPrefab(
                assets.HeavyRailing,
                "HSK Left Low Rail",
                root,
                origin + (rotation * new Vector3(-1.18f, 0.48f, z)),
                rotation,
                new Vector3(0.13f, 0.36f, 1.3f),
                materials.Rail,
                materials);
            InstantiateFittedPrefab(
                assets.HeavyRailing,
                "HSK Right Low Rail",
                root,
                origin + (rotation * new Vector3(1.18f, 0.48f, z)),
                rotation,
                new Vector3(0.13f, 0.36f, 1.3f),
                materials.Rail,
                materials);
        }

        private static void AddSteelPlateInterior(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation)
        {
            for (var i = 0; i < 9; i++)
            {
                var z = -0.45f + (i * 0.98f);
                InstantiateFittedPrefab(
                    assets.HeavySteelPlate,
                    "HSK Interior Steel Floor Plate " + i,
                    root,
                    origin + (rotation * new Vector3(0f, 0.205f, z)),
                    rotation,
                    new Vector3(2.34f, 0.055f, 0.88f),
                    materials.Plate,
                    materials,
                    PreviewLayer);
                InstantiateFittedPrefab(
                    assets.HeavySteelPlate,
                    "HSK Interior Steel Ceiling Plate " + i,
                    root,
                    origin + (rotation * new Vector3(0f, 2.055f, z)),
                    rotation,
                    new Vector3(2.08f, 0.055f, 0.88f),
                    materials.Plate,
                    materials,
                    PreviewLayer);
                InstantiateFittedPrefab(
                    assets.HeavySteelPlate,
                    "HSK Interior Steel Left Wall Plate " + i,
                    root,
                    origin + (rotation * new Vector3(-1.53f, 1.16f, z)),
                    rotation * Quaternion.Euler(0f, 0f, 90f),
                    new Vector3(0.06f, 1.52f, 0.88f),
                    materials.Plate,
                    materials,
                    PreviewLayer);
                InstantiateFittedPrefab(
                    assets.HeavySteelPlate,
                    "HSK Interior Steel Right Wall Plate " + i,
                    root,
                    origin + (rotation * new Vector3(1.53f, 1.16f, z)),
                    rotation * Quaternion.Euler(0f, 0f, 90f),
                    new Vector3(0.06f, 1.52f, 0.88f),
                    materials.Plate,
                    materials,
                    PreviewLayer);

                AddSteelPlateSeams(root, materials, origin, rotation, z, i);
            }

            for (var i = 0; i < 5; i++)
            {
                var z = -1.06f + (i * 2.08f);
                InstantiateFittedPrefab(
                    assets.HeavyWideWallRib,
                    "HSK Steel Left Vertical Rib " + i,
                    root,
                    origin + (rotation * new Vector3(-1.48f, 1.16f, z)),
                    rotation,
                    new Vector3(0.14f, 1.78f, 0.18f),
                    materials.Frame,
                    materials);
                InstantiateFittedPrefab(
                    assets.HeavyWideWallRib,
                    "HSK Steel Right Vertical Rib " + i,
                    root,
                    origin + (rotation * new Vector3(1.48f, 1.16f, z)),
                    rotation,
                    new Vector3(0.14f, 1.78f, 0.18f),
                    materials.Frame,
                    materials);
            }
        }

        private static void AddSteelPlateSeams(Transform root, PreviewMaterials materials, Vector3 origin, Quaternion rotation, float z, int index)
        {
            CreateWornBox(root, "Steel Floor Rear Seam " + index, origin + (rotation * new Vector3(0f, 0.236f, z + 0.43f)), rotation, new Vector3(2.18f, 0.005f, 0.018f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Floor Center Seam " + index, origin + (rotation * new Vector3(0f, 0.238f, z)), rotation, new Vector3(0.018f, 0.005f, 0.74f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Left Wall Rear Seam " + index, origin + (rotation * new Vector3(-1.495f, 1.16f, z + 0.43f)), rotation, new Vector3(0.012f, 1.34f, 0.018f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Right Wall Rear Seam " + index, origin + (rotation * new Vector3(1.495f, 1.16f, z + 0.43f)), rotation, new Vector3(0.012f, 1.34f, 0.018f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Left Wall Lower Horizontal Seam " + index, origin + (rotation * new Vector3(-1.492f, 0.78f, z)), rotation, new Vector3(0.012f, 0.018f, 0.72f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Right Wall Lower Horizontal Seam " + index, origin + (rotation * new Vector3(1.492f, 0.78f, z)), rotation, new Vector3(0.012f, 0.018f, 0.72f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Left Wall Upper Horizontal Seam " + index, origin + (rotation * new Vector3(-1.492f, 1.54f, z)), rotation, new Vector3(0.012f, 0.018f, 0.72f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Right Wall Upper Horizontal Seam " + index, origin + (rotation * new Vector3(1.492f, 1.54f, z)), rotation, new Vector3(0.012f, 0.018f, 0.72f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Steel Ceiling Rear Seam " + index, origin + (rotation * new Vector3(0f, 2.017f, z + 0.43f)), rotation, new Vector3(1.94f, 0.012f, 0.018f), materials.DarkSeam, PreviewLayer);
            AddSteelPlateFasteners(root, materials, origin, rotation, z, index);
        }

        private static void AddSteelPlateFasteners(Transform root, PreviewMaterials materials, Vector3 origin, Quaternion rotation, float z, int index)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                var x = side * 1.486f;
                CreateWornBox(root, "Steel Wall Fastener " + side + " A " + index, origin + (rotation * new Vector3(x, 0.58f, z - 0.31f)), rotation, new Vector3(0.018f, 0.052f, 0.052f), materials.EdgeWear, PreviewLayer);
                CreateWornBox(root, "Steel Wall Fastener " + side + " B " + index, origin + (rotation * new Vector3(x, 0.58f, z + 0.31f)), rotation, new Vector3(0.018f, 0.052f, 0.052f), materials.EdgeWear, PreviewLayer);
                CreateWornBox(root, "Steel Wall Fastener " + side + " C " + index, origin + (rotation * new Vector3(x, 1.78f, z - 0.31f)), rotation, new Vector3(0.018f, 0.052f, 0.052f), materials.EdgeWear, PreviewLayer);
                CreateWornBox(root, "Steel Wall Fastener " + side + " D " + index, origin + (rotation * new Vector3(x, 1.78f, z + 0.31f)), rotation, new Vector3(0.018f, 0.052f, 0.052f), materials.EdgeWear, PreviewLayer);
            }
        }

        private static void AddSteelPlateCornerInterior(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation)
        {
            InstantiateFittedPrefab(
                assets.HeavySteelPlate,
                "HSK Corner Steel Center Plate",
                root,
                origin + new Vector3(-0.2f, 0.22f, 0.15f),
                rotation,
                new Vector3(2.35f, 0.055f, 2.35f),
                materials.Plate,
                materials,
                CornerPreviewLayer);

            for (var i = 0; i < 3; i++)
            {
                var offset = -0.9f + (i * 0.85f);
                InstantiateFittedPrefab(
                    assets.HeavySteelPlate,
                    "HSK Corner Incoming Steel Wall Plate " + i,
                    root,
                    origin + new Vector3(1.42f, 1.14f, offset),
                    rotation * Quaternion.Euler(0f, 0f, 90f),
                    new Vector3(0.06f, 1.42f, 0.76f),
                    materials.Plate,
                    materials,
                    CornerPreviewLayer);
                InstantiateFittedPrefab(
                    assets.HeavySteelPlate,
                    "HSK Corner Outgoing Steel Wall Plate " + i,
                    root,
                    origin + new Vector3(offset, 1.14f, 1.42f),
                    rotation * Quaternion.Euler(0f, 90f, 90f),
                    new Vector3(0.06f, 1.42f, 0.76f),
                    materials.Plate,
                    materials,
                    CornerPreviewLayer);
            }
        }

        private static void AddBumpyWornPlateInterior(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation)
        {
            for (var i = 0; i < 8; i++)
            {
                var z = -0.35f + (i * 1.08f);
                CreateWornBox(root, "HSK Bumpy Worn Floor Plate " + i, origin + (rotation * new Vector3(0f, 0.232f, z)), rotation, new Vector3(2.3f, 0.035f, 1.02f), materials.Plate, PreviewLayer);
                CreateWornBox(root, "HSK Bumpy Worn Ceiling Plate " + i, origin + (rotation * new Vector3(0f, 2.035f, z)), rotation, new Vector3(2.05f, 0.035f, 1.02f), materials.Plate, PreviewLayer);
                CreateWornBox(root, "HSK Bumpy Worn Left Wall Plate " + i, origin + (rotation * new Vector3(-1.515f, 1.18f, z)), rotation, new Vector3(0.035f, 1.56f, 1.02f), materials.Plate, PreviewLayer);
                CreateWornBox(root, "HSK Bumpy Worn Right Wall Plate " + i, origin + (rotation * new Vector3(1.515f, 1.18f, z)), rotation, new Vector3(0.035f, 1.56f, 1.02f), materials.Plate, PreviewLayer);

                AddBumpyPlateSeams(root, materials, origin, rotation, z, i);
            }

            for (var i = 0; i < 5; i++)
            {
                var z = -0.92f + (i * 2.02f);
                InstantiateFittedPrefab(
                    assets.HeavyWideWallRib,
                    "HSK Bumpy Worn Left Rib " + i,
                    root,
                    origin + (rotation * new Vector3(-1.46f, 1.17f, z)),
                    rotation,
                    new Vector3(0.16f, 1.84f, 0.18f),
                    materials.Frame,
                    materials);
                InstantiateFittedPrefab(
                    assets.HeavyWideWallRib,
                    "HSK Bumpy Worn Right Rib " + i,
                    root,
                    origin + (rotation * new Vector3(1.46f, 1.17f, z)),
                    rotation,
                    new Vector3(0.16f, 1.84f, 0.18f),
                    materials.Frame,
                    materials);
            }
        }

        private static void AddBumpyPlateSeams(Transform root, PreviewMaterials materials, Vector3 origin, Quaternion rotation, float z, int index)
        {
            CreateWornBox(root, "Bumpy Plate Floor Cross Seam " + index, origin + (rotation * new Vector3(0f, 0.264f, z + 0.51f)), rotation, new Vector3(2.16f, 0.006f, 0.028f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Bumpy Plate Floor Center Scratch Seam " + index, origin + (rotation * new Vector3(0f, 0.267f, z)), rotation, new Vector3(0.026f, 0.006f, 0.84f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Bumpy Plate Ceiling Cross Seam " + index, origin + (rotation * new Vector3(0f, 2.004f, z + 0.51f)), rotation, new Vector3(1.92f, 0.014f, 0.028f), materials.Scorch, PreviewLayer);
            CreateWornBox(root, "Bumpy Plate Left Wall Seam " + index, origin + (rotation * new Vector3(-1.485f, 1.18f, z + 0.51f)), rotation, new Vector3(0.014f, 1.42f, 0.026f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Bumpy Plate Right Wall Seam " + index, origin + (rotation * new Vector3(1.485f, 1.18f, z + 0.51f)), rotation, new Vector3(0.014f, 1.42f, 0.026f), materials.DarkSeam, PreviewLayer);
            CreateWornBox(root, "Bumpy Plate Left Wall Lower Rust Edge " + index, origin + (rotation * new Vector3(-1.482f, 0.54f, z - 0.12f)), rotation, new Vector3(0.018f, 0.075f, 0.72f), materials.Rust, PreviewLayer);
            CreateWornBox(root, "Bumpy Plate Right Wall Lower Rust Edge " + index, origin + (rotation * new Vector3(1.482f, 0.52f, z + 0.08f)), rotation, new Vector3(0.018f, 0.07f, 0.68f), materials.Rust, PreviewLayer);
        }

        private static void AddBumpyWornPlateCornerInterior(Transform root, PreviewAssets assets, PreviewMaterials materials, Vector3 origin, Quaternion rotation)
        {
            CreateWornBox(root, "HSK Bumpy Worn Corner Floor Plate", origin + new Vector3(-0.16f, 0.24f, 0.12f), rotation, new Vector3(2.35f, 0.035f, 2.35f), materials.Plate, CornerPreviewLayer);

            for (var i = 0; i < 3; i++)
            {
                var offset = -0.88f + (i * 0.86f);
                CreateWornBox(root, "HSK Bumpy Worn Corner Incoming Wall " + i, origin + new Vector3(1.43f, 1.16f, offset), rotation, new Vector3(0.035f, 1.42f, 0.78f), materials.Plate, CornerPreviewLayer);
                CreateWornBox(root, "HSK Bumpy Worn Corner Outgoing Wall " + i, origin + new Vector3(offset, 1.16f, 1.43f), rotation, new Vector3(0.78f, 1.42f, 0.035f), materials.Plate, CornerPreviewLayer);
            }

            CreateWornBox(root, "Bumpy Worn Corner Rust Pool", origin + new Vector3(-0.36f, 0.272f, 0.42f), Quaternion.Euler(0f, 18f, 0f), new Vector3(0.82f, 0.006f, 0.12f), materials.Rust, CornerPreviewLayer);
            CreateWornBox(root, "Bumpy Worn Corner Dirt Drag", origin + new Vector3(0.22f, 0.274f, -0.22f), Quaternion.Euler(0f, -12f, 0f), new Vector3(1.05f, 0.006f, 0.1f), materials.Dirt, CornerPreviewLayer);
        }

        private static void AddHeavyWornAgingDetails(Transform root, PreviewMaterials materials)
        {
            for (var i = 0; i < 8; i++)
            {
                var z = -0.35f + (i * 1.08f);
                CreateWornBox(root, "Heavy Worn Floor Grime Strip " + i, new Vector3(0.18f, 0.274f, z - 0.22f), Quaternion.Euler(0f, -8f, 0f), new Vector3(1.52f, 0.006f, 0.095f), materials.Dirt, PreviewLayer);
                CreateWornBox(root, "Heavy Worn Floor Rust Break " + i, new Vector3(-0.58f, 0.279f, z + 0.24f), Quaternion.Euler(0f, 20f, 0f), new Vector3(0.46f, 0.006f, 0.11f), materials.Rust, PreviewLayer);
                CreateWornBox(root, "Heavy Worn Floor Exposed Scratch " + i, new Vector3(0.42f, 0.284f, z + 0.09f), Quaternion.Euler(0f, 34f, 0f), new Vector3(0.72f, 0.005f, 0.018f), materials.EdgeWear, PreviewLayer);
                CreateWornBox(root, "Heavy Worn Floor Warning Remnant " + i, new Vector3(-0.05f, 0.287f, z + 0.42f), Quaternion.Euler(0f, -28f, 0f), new Vector3(0.54f, 0.005f, 0.034f), materials.FadedWarning, PreviewLayer);

                CreateWornBox(root, "Heavy Worn Left Wall Dirt Panel " + i, new Vector3(-1.472f, 1.12f, z + 0.08f), Quaternion.identity, new Vector3(0.022f, 0.64f, 0.48f), materials.Dirt, PreviewLayer);
                CreateWornBox(root, "Heavy Worn Right Wall Dirt Panel " + i, new Vector3(1.472f, 1.04f, z - 0.18f), Quaternion.identity, new Vector3(0.022f, 0.58f, 0.42f), materials.Dirt, PreviewLayer);
                CreateWornBox(root, "Heavy Worn Left Wall Rust Streak " + i, new Vector3(-1.468f, 1.54f, z - 0.34f), Quaternion.identity, new Vector3(0.026f, 0.62f, 0.055f), materials.Rust, PreviewLayer);
                CreateWornBox(root, "Heavy Worn Right Wall Rust Streak " + i, new Vector3(1.468f, 1.48f, z + 0.31f), Quaternion.identity, new Vector3(0.026f, 0.58f, 0.052f), materials.Rust, PreviewLayer);
                CreateWornBox(root, "Heavy Worn Ceiling Soot Band " + i, new Vector3(0.06f, 1.998f, z + 0.04f), Quaternion.identity, new Vector3(1.28f, 0.028f, 0.11f), materials.Scorch, PreviewLayer);
            }

            AddThresholdWear(root, materials, -1.65f);
            AddThresholdWear(root, materials, 7.15f);
            AddCornerWear(root, materials, new Vector3(9f, 0f, 4.35f));
        }

        private static void AddWornAgingDetails(Transform root, PreviewMaterials materials)
        {
            for (var i = 0; i < 4; i++)
            {
                var z = i * 2.32f;
                CreateWornBox(root, "Worn Floor Dirt Band " + i, new Vector3(0.05f, 0.18f, z + 0.34f), Quaternion.identity, new Vector3(1.75f, 0.006f, 0.09f), materials.Dirt, PreviewLayer);
                CreateWornBox(root, "Worn Floor Rust Patch Left " + i, new Vector3(-0.68f, 0.186f, z - 0.42f), Quaternion.Euler(0f, 6f, 0f), new Vector3(0.32f, 0.006f, 0.1f), materials.Rust, PreviewLayer);
                CreateWornBox(root, "Worn Floor Rust Patch Right " + i, new Vector3(0.72f, 0.187f, z + 0.62f), Quaternion.Euler(0f, -9f, 0f), new Vector3(0.28f, 0.006f, 0.085f), materials.Rust, PreviewLayer);
                CreateWornBox(root, "Worn Floor Bright Scratch " + i, new Vector3(0.16f, 0.191f, z - 0.08f), Quaternion.Euler(0f, 12f, 0f), new Vector3(0.62f, 0.004f, 0.018f), materials.EdgeWear, PreviewLayer);
                CreateWornBox(root, "Worn Faded Hazard Strip " + i, new Vector3(-0.42f, 0.193f, z + 0.78f), Quaternion.Euler(0f, 34f, 0f), new Vector3(0.42f, 0.004f, 0.036f), materials.FadedWarning, PreviewLayer);

                CreateWornBox(root, "Worn Left Wall Grime " + i, new Vector3(-1.46f, 1.16f, z + 0.16f), Quaternion.identity, new Vector3(0.026f, 0.78f, 0.42f), materials.Dirt, PreviewLayer);
                CreateWornBox(root, "Worn Right Wall Grime " + i, new Vector3(1.46f, 1.08f, z - 0.28f), Quaternion.identity, new Vector3(0.026f, 0.62f, 0.36f), materials.Dirt, PreviewLayer);
                CreateWornBox(root, "Worn Left Rust Streak " + i, new Vector3(-1.43f, 1.42f, z - 0.58f), Quaternion.identity, new Vector3(0.03f, 0.56f, 0.06f), materials.Rust, PreviewLayer);
                CreateWornBox(root, "Worn Right Rust Streak " + i, new Vector3(1.43f, 1.32f, z + 0.44f), Quaternion.identity, new Vector3(0.03f, 0.5f, 0.055f), materials.Rust, PreviewLayer);
                CreateWornBox(root, "Worn Ceiling Soot " + i, new Vector3(0.04f, 2.18f, z + 0.12f), Quaternion.identity, new Vector3(1.04f, 0.026f, 0.12f), materials.Scorch, PreviewLayer);
            }

            AddThresholdWear(root, materials, -1.65f);
            AddThresholdWear(root, materials, 7.15f);
            AddCornerWear(root, materials, new Vector3(9f, 0f, 4.35f));
        }

        private static void AddThresholdWear(Transform root, PreviewMaterials materials, float z)
        {
            CreateWornBox(root, "Worn Threshold Left Edge " + z, new Vector3(-1.42f, 1.45f, z - 0.02f), Quaternion.identity, new Vector3(0.028f, 0.58f, 0.035f), materials.EdgeWear, PreviewLayer);
            CreateWornBox(root, "Worn Threshold Right Edge " + z, new Vector3(1.42f, 1.45f, z - 0.02f), Quaternion.identity, new Vector3(0.028f, 0.58f, 0.035f), materials.EdgeWear, PreviewLayer);
            CreateWornBox(root, "Worn Threshold Left Base Rust " + z, new Vector3(-1.38f, 0.28f, z + 0.06f), Quaternion.identity, new Vector3(0.12f, 0.16f, 0.055f), materials.Rust, PreviewLayer);
            CreateWornBox(root, "Worn Threshold Right Base Rust " + z, new Vector3(1.38f, 0.28f, z + 0.06f), Quaternion.identity, new Vector3(0.12f, 0.16f, 0.055f), materials.Rust, PreviewLayer);
            CreateWornBox(root, "Worn Threshold Top Smoke " + z, new Vector3(0f, 2.06f, z), Quaternion.identity, new Vector3(1.7f, 0.045f, 0.07f), materials.Scorch, PreviewLayer);
        }

        private static void AddCornerWear(Transform root, PreviewMaterials materials, Vector3 origin)
        {
            CreateWornBox(root, "Worn Corner Floor Rust A", origin + new Vector3(-0.46f, 0.188f, 0.5f), Quaternion.Euler(0f, 24f, 0f), new Vector3(0.42f, 0.005f, 0.08f), materials.Rust, CornerPreviewLayer);
            CreateWornBox(root, "Worn Corner Floor Dirt A", origin + new Vector3(0.34f, 0.184f, -0.12f), Quaternion.identity, new Vector3(0.92f, 0.005f, 0.09f), materials.Dirt, CornerPreviewLayer);
            CreateWornBox(root, "Worn Corner Joint Scratch", origin + new Vector3(-0.04f, 0.192f, 0.18f), Quaternion.Euler(0f, -31f, 0f), new Vector3(0.58f, 0.004f, 0.018f), materials.EdgeWear, CornerPreviewLayer);
            CreateWornBox(root, "Worn Corner Faded Mark", origin + new Vector3(0.55f, 0.196f, 0.72f), Quaternion.Euler(0f, 38f, 0f), new Vector3(0.48f, 0.004f, 0.035f), materials.FadedWarning, CornerPreviewLayer);
        }

        private static void CreateWornBox(Transform root, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, int layer)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.hideFlags = HideFlags.HideAndDontSave;
            box.transform.SetParent(root, false);
            box.transform.position = position;
            box.transform.rotation = rotation;
            box.transform.localScale = scale;
            SetLayerRecursively(box, layer);

            var collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void CreateLighting(Transform root, SampleStyle style)
        {
            var steelPlate = style == SampleStyle.SteelPlate || style == SampleStyle.BumpyWornPlate;
            var key = new GameObject("Sample Key Light");
            key.transform.SetParent(root, false);
            key.hideFlags = HideFlags.HideAndDontSave;
            key.layer = PreviewLayer;
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = steelPlate ? 2.8f : 2.2f;
            keyLight.cullingMask = (1 << PreviewLayer) | (1 << CornerPreviewLayer);
            key.transform.rotation = Quaternion.Euler(54f, -34f, 0f);

            AddPointLight(root, "Sample Fill Light A", new Vector3(0f, 2.1f, -1.6f), steelPlate ? 3.2f : 2.4f, 8f);
            AddPointLight(root, "Sample Fill Light B", new Vector3(0f, 2.2f, 4.2f), steelPlate ? 3f : 2.1f, 8f);
            AddPointLight(root, "Sample Corner Fill Light", new Vector3(4.5f, 2.1f, 4.4f), steelPlate ? 2.8f : 2.1f, 7f);
        }

        private static void AddPointLight(Transform root, string name, Vector3 position, float intensity, float range)
        {
            var fill = new GameObject(name);
            fill.transform.SetParent(root, false);
            fill.hideFlags = HideFlags.HideAndDontSave;
            fill.layer = PreviewLayer;
            fill.transform.position = position;
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.intensity = intensity;
            fillLight.range = range;
            fillLight.cullingMask = (1 << PreviewLayer) | (1 << CornerPreviewLayer);
        }

        private static void CaptureView(string path, Vector3 position, Vector3 lookAt, float fieldOfView, bool orthographic, float orthographicSize, int cullingMask)
        {
            var cameraObject = new GameObject("Sample Capture Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.layer = PreviewLayer;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = position;
                camera.transform.rotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
                camera.fieldOfView = fieldOfView;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
                camera.aspect = 16f / 9f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 60f;
                camera.cullingMask = cullingMask;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.012f, 0.014f, 0.016f, 1f);
                CaptureCamera(camera, path, 1600, 900);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static GameObject InstantiateFittedPrefab(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector3 targetLocalBounds,
            Material materialOverride,
            PreviewMaterials materials,
            int layer = PreviewLayer,
            bool keepOriginalMaterials = false)
        {
            var anchor = new GameObject(name);
            anchor.hideFlags = HideFlags.HideAndDontSave;
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = worldPosition;
            anchor.transform.rotation = worldRotation;

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

            if (!keepOriginalMaterials && materialOverride == null)
            {
                ApplyProjectMaterialPalette(instance, materials);
            }
            else if (!keepOriginalMaterials)
            {
                ApplyMaterialOverride(instance, materialOverride);
            }

            FitChildToLocalBounds(anchor.transform, instance.transform, targetLocalBounds);
            SetLayerRecursively(anchor, layer);
            return anchor;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                children[i].gameObject.layer = layer;
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
                lights[i].enabled = false;
            }
        }

        private static void ApplyProjectMaterialPalette(GameObject instance, PreviewMaterials materials)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var rendererName = renderers[i].name.ToLowerInvariant();
                var material = materials.Frame;
                if (rendererName.Contains("floor"))
                {
                    material = materials.Floor;
                }
                else if (rendererName.Contains("ceiling"))
                {
                    material = materials.Ceiling;
                }
                else if (rendererName.Contains("light"))
                {
                    material = materials.Light;
                }
                else if (rendererName.Contains("window") || rendererName.Contains("wall") || rendererName.Contains("plug"))
                {
                    material = materials.Wall;
                }

                ApplyMaterialOverride(renderers[i], material);
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
                ApplyMaterialOverride(renderers[i], material);
            }
        }

        private static void ApplyMaterialOverride(Renderer renderer, Material material)
        {
            if (material == null)
            {
                return;
            }

            var sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                renderer.sharedMaterial = material;
                return;
            }

            for (var materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                sharedMaterials[materialIndex] = material;
            }

            renderer.sharedMaterials = sharedMaterials;
        }

        private static void FitChildToLocalBounds(Transform anchor, Transform child, Vector3 targetLocalBounds)
        {
            var bounds = CalculateLocalRenderBounds(anchor);
            var size = bounds.size;
            child.localScale = Vector3.Scale(
                child.localScale,
                new Vector3(
                    AxisScale(targetLocalBounds.x, size.x),
                    AxisScale(targetLocalBounds.y, size.y),
                    AxisScale(targetLocalBounds.z, size.z)));

            var fittedBounds = CalculateLocalRenderBounds(anchor);
            child.localPosition -= fittedBounds.center;
        }

        private static float AxisScale(float target, float current)
        {
            return current <= 0.001f ? 1f : Mathf.Clamp(target / current, 0.025f, 12f);
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

        private static void CaptureCamera(Camera camera, string path, int width, int height)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private sealed class PreviewAssets
        {
            public GameObject HeavyFloor { get; private set; }
            public GameObject HeavySteelPlate { get; private set; }
            public GameObject HeavyWallRib { get; private set; }
            public GameObject HeavyWideWallRib { get; private set; }
            public GameObject HeavyWallLight { get; private set; }
            public GameObject HeavyRailing { get; private set; }
            public GameObject StyledStraightCorridor { get; private set; }
            public GameObject StyledBlankWall { get; private set; }
            public GameObject StyledCeilingLight { get; private set; }
            public GameObject StyledJoint { get; private set; }

            public static PreviewAssets Load()
            {
                return new PreviewAssets
                {
                    HeavyFloor = LoadPrefab(HeavyFloorPrefabPath),
                    HeavySteelPlate = LoadPrefab(HeavySteelPlatePrefabPath),
                    HeavyWallRib = LoadPrefab(HeavyWallRibPrefabPath),
                    HeavyWideWallRib = LoadPrefab(HeavyWideWallRibPrefabPath),
                    HeavyWallLight = LoadPrefab(HeavyWallLightPrefabPath),
                    HeavyRailing = LoadPrefab(HeavyRailingPrefabPath),
                    StyledStraightCorridor = LoadPrefab(StyledStraightCorridorPrefabPath),
                    StyledBlankWall = LoadPrefab(StyledBlankWallPrefabPath),
                    StyledCeilingLight = LoadPrefab(StyledCeilingLightPrefabPath),
                    StyledJoint = LoadPrefab(StyledJointPrefabPath)
                };
            }

            private static GameObject LoadPrefab(string path)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing step 02 sample prefab: " + path);
                }

                return prefab;
            }
        }

        private sealed class PreviewMaterials
        {
            public Material Floor { get; private set; }
            public Material Wall { get; private set; }
            public Material Ceiling { get; private set; }
            public Material Frame { get; private set; }
            public Material Rail { get; private set; }
            public Material Light { get; private set; }
            public Material Plate { get; private set; }
            public Material DarkSeam { get; private set; }
            public Material Dirt { get; private set; }
            public Material Rust { get; private set; }
            public Material EdgeWear { get; private set; }
            public Material Scorch { get; private set; }
            public Material FadedWarning { get; private set; }

            public static PreviewMaterials Load(SampleStyle style)
            {
                var baseFloor = LoadMaterial(CorridorFloorMaterialPath);
                var baseWall = LoadMaterial(CorridorWallMaterialPath);
                var baseCeiling = LoadMaterial(CorridorCeilingMaterialPath);
                var baseFrame = LoadMaterial(CorridorFrameMaterialPath);
                var baseRail = LoadMaterial(CorridorRailMaterialPath);
                var baseLight = LoadMaterial(CorridorLightMaterialPath);

                if (style == SampleStyle.Standard)
                {
                    return new PreviewMaterials
                    {
                        Floor = baseFloor,
                        Wall = baseWall,
                        Ceiling = baseCeiling,
                        Frame = baseFrame,
                        Rail = baseRail,
                        Light = baseLight,
                        Plate = baseFloor,
                        DarkSeam = CreateSolidMaterial("Sample Dark Seam", new Color(0.018f, 0.019f, 0.018f, 1f)),
                        Dirt = CreateSolidMaterial("Sample Dirt", new Color(0.055f, 0.05f, 0.042f, 1f)),
                        Rust = CreateSolidMaterial("Sample Rust", new Color(0.25f, 0.085f, 0.028f, 1f)),
                        EdgeWear = CreateSolidMaterial("Sample Edge Wear", new Color(0.35f, 0.34f, 0.3f, 1f)),
                        Scorch = CreateSolidMaterial("Sample Scorch", new Color(0.018f, 0.017f, 0.015f, 1f)),
                        FadedWarning = CreateSolidMaterial("Sample Faded Warning", new Color(0.28f, 0.21f, 0.045f, 1f))
                    };
                }

                if (style == SampleStyle.SteelPlate)
                {
                    return new PreviewMaterials
                    {
                        Floor = CreateWornCopy(baseFloor, "Steel Base Floor", new Color(0.13f, 0.145f, 0.145f, 1f), 0.18f, 0.16f),
                        Wall = CreateWornCopy(baseWall, "Steel Back Wall", new Color(0.095f, 0.108f, 0.108f, 1f), 0.12f, 0.14f),
                        Ceiling = CreateWornCopy(baseCeiling, "Steel Back Ceiling", new Color(0.1f, 0.11f, 0.112f, 1f), 0.12f, 0.14f),
                        Frame = CreateWornCopy(baseFrame, "Steel Rib Frame", new Color(0.115f, 0.122f, 0.118f, 1f), 0.2f, 0.15f),
                        Rail = CreateWornCopy(baseRail, "Steel Dark Rail", new Color(0.05f, 0.056f, 0.055f, 1f), 0.15f, 0.1f),
                        Light = CreateWornCopy(baseLight, "Steel Cool Utility Light", new Color(0.1f, 0.42f, 0.35f, 1f), 0f, 0.08f),
                        Plate = CreateWornCopy(baseFloor, "Steel Plate Surface", new Color(0.18f, 0.195f, 0.19f, 1f), 0.42f, 0.2f),
                        DarkSeam = CreateSolidMaterial("Steel Plate Dark Seam", new Color(0.012f, 0.013f, 0.012f, 1f)),
                        Dirt = CreateSolidMaterial("Steel Plate Dirt", new Color(0.036f, 0.033f, 0.028f, 1f)),
                        Rust = CreateSolidMaterial("Steel Plate Rust", new Color(0.19f, 0.065f, 0.024f, 1f)),
                        EdgeWear = CreateSolidMaterial("Steel Plate Edge Wear", new Color(0.38f, 0.39f, 0.36f, 1f)),
                        Scorch = CreateSolidMaterial("Steel Plate Scorch", new Color(0.009f, 0.009f, 0.008f, 1f)),
                        FadedWarning = CreateSolidMaterial("Steel Plate Faded Warning", new Color(0.25f, 0.19f, 0.04f, 1f))
                    };
                }

                if (style == SampleStyle.BumpyWornPlate)
                {
                    return new PreviewMaterials
                    {
                        Floor = CreateWornCopy(baseFloor, "Bumpy Worn Base Floor", new Color(0.105f, 0.108f, 0.1f, 1f), 0.18f, 0.1f),
                        Wall = CreateWornCopy(baseWall, "Bumpy Worn Back Wall", new Color(0.075f, 0.083f, 0.078f, 1f), 0.1f, 0.09f),
                        Ceiling = CreateWornCopy(baseCeiling, "Bumpy Worn Back Ceiling", new Color(0.07f, 0.074f, 0.072f, 1f), 0.1f, 0.08f),
                        Frame = CreateWornCopy(baseFrame, "Bumpy Worn Frame", new Color(0.1f, 0.092f, 0.078f, 1f), 0.18f, 0.08f),
                        Rail = CreateWornCopy(baseRail, "Bumpy Worn Rail", new Color(0.04f, 0.038f, 0.034f, 1f), 0.12f, 0.06f),
                        Light = CreateWornCopy(baseLight, "Bumpy Worn Dim Utility Light", new Color(0.11f, 0.36f, 0.29f, 1f), 0f, 0.04f),
                        Plate = CreateBumpyPlateMaterial(),
                        DarkSeam = CreateSolidMaterial("Bumpy Worn Deep Seam", new Color(0.006f, 0.005f, 0.004f, 1f)),
                        Dirt = CreateSolidMaterial("Bumpy Worn Oil Dirt", new Color(0.028f, 0.023f, 0.017f, 1f)),
                        Rust = CreateSolidMaterial("Bumpy Worn Rust", new Color(0.31f, 0.085f, 0.022f, 1f)),
                        EdgeWear = CreateSolidMaterial("Bumpy Worn Exposed Metal", new Color(0.48f, 0.46f, 0.39f, 1f)),
                        Scorch = CreateSolidMaterial("Bumpy Worn Soot", new Color(0.004f, 0.004f, 0.003f, 1f)),
                        FadedWarning = CreateSolidMaterial("Bumpy Worn Faded Yellow Paint", new Color(0.33f, 0.245f, 0.045f, 1f))
                    };
                }

                return new PreviewMaterials
                {
                    Floor = CreateWornCopy(baseFloor, "Worn Floor", new Color(0.16f, 0.165f, 0.155f, 1f), 0.12f, 0.2f),
                    Wall = CreateWornCopy(baseWall, "Worn Wall", new Color(0.12f, 0.135f, 0.13f, 1f), 0.08f, 0.17f),
                    Ceiling = CreateWornCopy(baseCeiling, "Worn Ceiling", new Color(0.105f, 0.112f, 0.112f, 1f), 0.08f, 0.16f),
                    Frame = CreateWornCopy(baseFrame, "Worn Frame", new Color(0.145f, 0.14f, 0.128f, 1f), 0.16f, 0.18f),
                    Rail = CreateWornCopy(baseRail, "Worn Rail", new Color(0.075f, 0.078f, 0.075f, 1f), 0.15f, 0.12f),
                    Light = CreateWornCopy(baseLight, "Worn Dim Light", new Color(0.12f, 0.44f, 0.34f, 1f), 0f, 0.05f),
                    Plate = CreateWornCopy(baseFloor, "Worn Plate Surface", new Color(0.16f, 0.165f, 0.155f, 1f), 0.12f, 0.2f),
                    DarkSeam = CreateSolidMaterial("Worn Dark Seam", new Color(0.018f, 0.016f, 0.012f, 1f)),
                    Dirt = CreateSolidMaterial("Worn Dirt", new Color(0.047f, 0.039f, 0.028f, 1f)),
                    Rust = CreateSolidMaterial("Worn Rust", new Color(0.25f, 0.08f, 0.025f, 1f)),
                    EdgeWear = CreateSolidMaterial("Worn Exposed Edge", new Color(0.35f, 0.34f, 0.3f, 1f)),
                    Scorch = CreateSolidMaterial("Worn Scorch", new Color(0.012f, 0.011f, 0.01f, 1f)),
                    FadedWarning = CreateSolidMaterial("Worn Faded Warning", new Color(0.28f, 0.21f, 0.045f, 1f))
                };
            }

            private static Material LoadMaterial(string path)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    throw new InvalidOperationException("Missing step 02 sample material: " + path);
                }

                return material;
            }

            private static Texture2D LoadTexture(string path)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    throw new InvalidOperationException("Missing step 02 sample texture: " + path);
                }

                return texture;
            }

            private static Material CreateWornCopy(Material source, string name, Color color, float metallic, float smoothness)
            {
                var material = new Material(source)
                {
                    name = name,
                    hideFlags = HideFlags.HideAndDontSave
                };
                ApplyMaterialColor(material, color);
                ApplyScalar(material, "_Metallic", metallic);
                ApplyScalar(material, "_Smoothness", smoothness);
                ApplyScalar(material, "_Glossiness", smoothness);
                return material;
            }

            private static Material CreateBumpyPlateMaterial()
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Texture");
                }

                var material = new Material(shader)
                {
                    name = "Bumpy Worn Hex Plate",
                    hideFlags = HideFlags.HideAndDontSave
                };
                ApplyMaterialColor(material, new Color(0.55f, 0.54f, 0.49f, 1f));
                ApplyTexture(material, "_BaseMap", LoadTexture(HeavyCommonPlateAlbedoTexturePath));
                ApplyTexture(material, "_MainTex", LoadTexture(HeavyCommonPlateAlbedoTexturePath));
                ApplyTexture(material, "_BumpMap", LoadTexture(HeavyCommonPlateNormalTexturePath));
                ApplyTexture(material, "_MetallicGlossMap", LoadTexture(HeavyCommonPlateSpecTexturePath));
                ApplyTextureScale(material, "_BaseMap", new Vector2(1.35f, 1.35f));
                ApplyTextureScale(material, "_MainTex", new Vector2(1.35f, 1.35f));
                ApplyTextureScale(material, "_BumpMap", new Vector2(1.35f, 1.35f));
                ApplyScalar(material, "_BumpScale", 1.65f);
                ApplyScalar(material, "_Metallic", 0.35f);
                ApplyScalar(material, "_Smoothness", 0.12f);
                ApplyScalar(material, "_Glossiness", 0.12f);
                ApplyScalar(material, "_Cull", 0f);
                material.EnableKeyword("_NORMALMAP");
                return material;
            }

            private static Material CreateSolidMaterial(string name, Color color)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                var material = new Material(shader)
                {
                    name = name,
                    hideFlags = HideFlags.HideAndDontSave
                };
                ApplyMaterialColor(material, color);
                ApplyScalar(material, "_Metallic", 0f);
                ApplyScalar(material, "_Smoothness", 0.08f);
                ApplyScalar(material, "_Glossiness", 0.08f);
                return material;
            }

            private static void ApplyMaterialColor(Material material, Color color)
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

            private static void ApplyTexture(Material material, string propertyName, Texture texture)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                }
            }

            private static void ApplyTextureScale(Material material, string propertyName, Vector2 scale)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTextureScale(propertyName, scale);
                }
            }

            private static void ApplyScalar(Material material, string propertyName, float value)
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                }
            }
        }
    }
}
