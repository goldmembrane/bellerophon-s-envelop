using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedEngineRoomShellBootstrap
    {
        public const string RootName = "Approved Engine Room 01 Shell";

        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/EngineRoom";
        private const float OuterRadius = 4.4f;
        private const float InnerRadius = 1.48f;
        private const float FloorThickness = 0.16f;
        private const float WallHeight = 2.55f;
        private const float WallThickness = 0.34f;
        private const float DoorOpeningHeight = 1.92f;
        private const float DoorOpeningHalfDegrees = 10f;

        private static readonly Vector3 EngineRoomCenter = new Vector3(-13.7f, 0f, 18f);
        private static readonly EntranceSpec[] Entrances =
        {
            new EntranceSpec("Cockpit", "1시", "조종실", 60f, false),
            new EntranceSpec("Control", "3시", "통제실", 0f, false),
            new EntranceSpec("Cargo", "5시", "운송창고", -60f, true)
        };
        private static readonly TransformOverride[] UserEditedTransformOverrides =
        {
            new TransformOverride("Floor - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Floor - individually editable/ER-01 sealed full circular floor deck", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Floor - individually editable/ER-01 raised circular walking route panel", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Floor - individually editable/ER-01 central sealed cylinder base gasket", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib -160", new Vector3(-2.762696f, 0.035f, -1.005539f), Quaternion.Euler(0f, -110f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib -143", new Vector3(-2.358242f, 0.035f, -1.755647f), Quaternion.Euler(0f, -126.6667f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib -127", new Vector3(-1.755646f, 0.035f, -2.358243f), Quaternion.Euler(0f, -143.3333f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib -110", new Vector3(-1.005539f, 0.035f, -2.762696f), Quaternion.Euler(0f, -160f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib -37", new Vector3(2.337655f, 0.035f, -1.782966f), Quaternion.Euler(0f, 127.3333f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib -24", new Vector3(2.685824f, 0.035f, -1.195806f), Quaternion.Euler(0f, 114f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib 24", new Vector3(2.685824f, 0.035f, 1.195806f), Quaternion.Euler(0f, 66f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib 37", new Vector3(2.337655f, 0.035f, 1.782966f), Quaternion.Euler(0f, 52.66667f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib 112", new Vector3(-1.101343f, 0.035f, 2.725921f), Quaternion.Euler(0f, -22f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib 124", new Vector3(-1.644027f, 0.035f, 2.437371f), Quaternion.Euler(0f, -34f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib 136", new Vector3(-2.114859f, 0.035f, 2.042296f), Quaternion.Euler(0f, -46f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib 148", new Vector3(-2.493261f, 0.035f, 1.557963f), Quaternion.Euler(0f, -58f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Floor - individually editable/ER-01 radial deck rib 160", new Vector3(-2.762696f, 0.035f, 1.005539f), Quaternion.Euler(0f, -70f, 0f), new Vector3(0.04f, 0.035f, 2.47f)),
            new TransformOverride("Walls - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 outer hull wall lower sealed section 1", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 outer hull wall lower sealed section 2", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 outer hull wall lower sealed section 3", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 outer hull wall lower sealed section 4", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 outer hull wall continuous upper doorway header", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 smooth interior pressure wall liner lower sealed section 1", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 smooth interior pressure wall liner lower sealed section 2", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 smooth interior pressure wall liner lower sealed section 3", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 smooth interior pressure wall liner lower sealed section 4", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 smooth interior pressure wall liner continuous upper doorway header", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 upper inspection rim around sealed power cylinder", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Walls - individually editable/ER-01 solid outer upper maintenance rim", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Entrances - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Entrances - individually editable/ER-01 1시 Cockpit corridor side wall 1", new Vector3(3.423f, 0.928f, 4.159f), Quaternion.Euler(0f, 30.00001f, 0f), new Vector3(0.26f, 1.97f, 2.55f)),
            new TransformOverride("Entrances - individually editable/ER-01 1시 Cockpit corridor side wall 2", new Vector3(1.911f, 0.928f, 5.074f), Quaternion.Euler(0f, 30.00001f, 0f), new Vector3(0.26f, 1.97f, 2.55f)),
            new TransformOverride("Entrances - individually editable/ER-01 3시 Control corridor side wall 1", new Vector3(5.353f, 0.928f, -0.872f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.26f, 1.97f, 2.55f)),
            new TransformOverride("Entrances - individually editable/ER-01 3시 Control corridor side wall 2", new Vector3(4.855f, 0.928f, 0.856f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.26f, 1.97f, 2.55f)),
            new TransformOverride("Entrances - individually editable/ER-01 5시 Cargo ramp side wall 1", new Vector3(1.925f, 0.928f, -5.055f), Quaternion.Euler(0f, 150f, 0f), new Vector3(0.26f, 1.97f, 2.346f)),
            new TransformOverride("Entrances - individually editable/ER-01 5시 Cargo ramp side wall 2", new Vector3(3.266f, 0.928f, -4.405f), Quaternion.Euler(0f, 150f, 0f), new Vector3(0.26f, 1.97f, 2.346f)),
            new TransformOverride("Center Chamber - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Center Chamber - individually editable/ER-01 sealed transparent cylindrical power chamber glass", new Vector3(0f, 1.34f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Center Chamber - individually editable/ER-01 sealed cylinder lower metal cap", new Vector3(0f, 0.3f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Center Chamber - individually editable/ER-01 sealed cylinder upper metal cap", new Vector3(0f, 2.46f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Center Chamber - individually editable/ER-01 visible blue white inner power core column", new Vector3(0f, 1.34f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Center Chamber - individually editable/ER-01 visible contained power plasma", new Vector3(0f, 1.34f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.88f, 1.1264f, 0.88f)),
            new TransformOverride("Labels - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Labels - individually editable/ER-01 1시 Cockpit wall label plate", new Vector3(2.245f, 1.42f, 1.58f), Quaternion.Euler(0f, 30f, 0f), new Vector3(1.16f, 0.34f, 0.045f)),
            new TransformOverride("Labels - individually editable/ER-01 1시 Cockpit direction text", new Vector3(2.263f, 1.4f, 3.919631f), Quaternion.Euler(0f, 30f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Labels - individually editable/ER-01 3시 Control wall label plate", new Vector3(3.152f, 1.42f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.16f, 0.34f, 0.045f)),
            new TransformOverride("Labels - individually editable/ER-01 3시 Control direction text", new Vector3(4.526f, 1.4f, 0f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Labels - individually editable/ER-01 5시 Cargo wall label plate", new Vector3(2.245f, 1.3f, -2.089f), Quaternion.Euler(0f, 150f, 0f), new Vector3(1.16f, 0.34f, 0.045f)),
            new TransformOverride("Labels - individually editable/ER-01 5시 Cargo direction text", new Vector3(2.263f, 1.28f, -3.919631f), Quaternion.Euler(0f, 150f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Dressing - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Inspection Lights", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Inspection Lights/ER-01 large overhead room inspection softbox", new Vector3(0f, 5.2f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Inspection Lights/ER-01 cool corridor entry fill", new Vector3(2.8f, 2.8f, 4.3f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("Inspection Lights/ER-01 warm cargo ramp low light", new Vector3(2.825f, 1.15f, -4.893044f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Engine Room 01 Shell")]
        public static void EnsureApprovedEngineRoomShell()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            DeleteGeneratedObject(RootName);
            Directory.CreateDirectory(UnityAssetDirectory);

            var materials = EnsureMaterials();
            var root = new GameObject(RootName);
            root.transform.position = EngineRoomCenter;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            BuildEngineRoom(root.transform, materials);
            ApplyUserEditedTransformOverrides(root.transform);
            DisableAllColliders(root.transform);
            EnsureNoCockpitOverlap(root);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved engine room 01 shell applied. Root=" +
                RootName +
                "; Center=" +
                FormatVector(EngineRoomCenter) +
                "; Parts=" +
                root.GetComponentsInChildren<Renderer>(true).Length +
                "; CockpitUntouched=True");
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Engine Room 01 Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for engine room current object capture.");
            }

            var normalizedActivePath = activeScene.path.Replace('\\', '/');
            var normalizedCargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\', '/');
            if (!string.Equals(normalizedActivePath, normalizedCargoPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Current active scene is not CargoRunMvp. ActiveScene=" + activeScene.path);
            }

            var root = RequireObject(RootName);
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for engine room current object capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, "artSample", "engine_room_shell", "editor_current");
            Directory.CreateDirectory(outputRoot);

            var builder = new StringBuilder();
            builder.AppendLine("# ER-01 Current Editor Objects");
            builder.AppendLine();
            builder.AppendLine("Captured from the currently open CargoRunMvp scene without regenerating ER-01.");
            builder.AppendLine("Use these values to reflect user-edited engine room placement in ApprovedEngineRoomShellBootstrap.");
            builder.AppendLine();
            builder.Append("private static readonly Vector3 EngineRoomCenter = ")
                .Append(FormatSourceVector(root.transform.position))
                .AppendLine(";");
            builder.AppendLine();
            builder.AppendLine("private static readonly TransformOverride[] UserEditedTransformOverrides =");
            builder.AppendLine("{");

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root.transform)
                {
                    continue;
                }

                builder.Append("    new TransformOverride(")
                    .Append(Quote(GetRelativePath(root.transform, transform)))
                    .Append(", ")
                    .Append(FormatSourceVector(transform.localPosition))
                    .Append(", ")
                    .Append(FormatSourceQuaternion(transform.localRotation))
                    .Append(", ")
                    .Append(FormatSourceVector(transform.localScale))
                    .AppendLine("),");
            }

            builder.AppendLine("};");

            var outputPath = Path.Combine(outputRoot, "er01_current_objects.md");
            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Approved engine room 01 current object capture saved: " + outputPath);
        }

        private static void BuildEngineRoom(Transform root, EngineRoomMaterials materials)
        {
            var floorGroup = AddGroup(root, "Floor - individually editable");
            var wallGroup = AddGroup(root, "Walls - individually editable");
            var entranceGroup = AddGroup(root, "Entrances - individually editable");
            var chamberGroup = AddGroup(root, "Center Chamber - individually editable");
            var labelGroup = AddGroup(root, "Labels - individually editable");
            var dressingGroup = AddGroup(root, "Dressing - individually editable");
            var lightGroup = AddGroup(root, "Inspection Lights");

            AddCylinder(
                "ER-01 sealed full circular floor deck",
                floorGroup,
                new Vector3(0f, 0f, 0f),
                OuterRadius,
                FloorThickness,
                materials.Floor,
                128);
            AddAnnularSector(
                "ER-01 raised circular walking route panel",
                floorGroup,
                1.38f,
                OuterRadius - 0.34f,
                -178f,
                178f,
                0.105f,
                0.045f,
                materials.FloorPanel,
                96);
            AddAnnularSector(
                "ER-01 central sealed cylinder base gasket",
                floorGroup,
                0.98f,
                1.34f,
                -178f,
                178f,
                0.155f,
                0.075f,
                materials.Rim,
                96);

            AddCorridorOpenedCylindricalShell(
                wallGroup,
                "ER-01 outer hull wall",
                OuterRadius - 0.02f,
                OuterRadius + WallThickness,
                0f,
                WallHeight,
                materials.OuterWall);
            AddCorridorOpenedCylindricalShell(
                wallGroup,
                "ER-01 smooth interior pressure wall liner",
                OuterRadius - 0.22f,
                OuterRadius - 0.04f,
                0.04f,
                WallHeight - 0.12f,
                materials.WallLiner);
            AddAnnularSector(
                "ER-01 upper inspection rim around sealed power cylinder",
                wallGroup,
                0.98f,
                1.34f,
                -178f,
                178f,
                2.52f,
                0.12f,
                materials.Rim,
                96);
            AddAnnularSector(
                "ER-01 solid outer upper maintenance rim",
                wallGroup,
                OuterRadius - 0.18f,
                OuterRadius + 0.30f,
                -178f,
                178f,
                WallHeight + 0.07f,
                0.14f,
                materials.Rim,
                96);

            for (var i = 0; i < Entrances.Length; i++)
            {
                AddEntryCorridor(entranceGroup, Entrances[i], materials);
            }

            AddDoorwaySideSeals(entranceGroup, materials);
            AddCenterChamber(chamberGroup, materials);
            AddFloorGrating(floorGroup, materials, -160f, -110f, 4);
            AddFloorGrating(floorGroup, materials, -64f, -24f, 4);
            AddFloorGrating(floorGroup, materials, 24f, 64f, 4);
            AddFloorGrating(floorGroup, materials, 112f, 160f, 5);
            AddRampHazardStripes(entranceGroup, materials);
            AddBolts(wallGroup, materials);

            for (var i = 0; i < Entrances.Length; i++)
            {
                AddDirectionLabel(labelGroup, Entrances[i], materials);
            }

            AddDressing(dressingGroup, materials);
            AddInspectionLights(lightGroup);
        }

        private static Transform AddGroup(Transform parent, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static void AddEntryCorridor(Transform parent, EntranceSpec entrance, EngineRoomMaterials materials)
        {
            if (entrance.IsRamp)
            {
                AddOrientedBox(
                    parent,
                    "ER-01 " + entrance.ClockLabel + " " + entrance.Key + " descending ramp slab",
                    entrance.Degree,
                    OuterRadius + 1.70f,
                    0f,
                    -0.20f,
                    new Vector3(1.78f, 1.70f, 0.18f),
                    materials.RampFloor,
                    -7f,
                    0.012f);

                for (var i = 0; i < 2; i++)
                {
                    var side = i == 0 ? -1.10f : 1.10f;
                    AddOrientedBox(
                        parent,
                        "ER-01 " + entrance.ClockLabel + " " + entrance.Key + " ramp side wall " + (i + 1),
                        entrance.Degree,
                        OuterRadius + 1.70f,
                        side,
                        WallHeight * 0.45f - 0.12f,
                        new Vector3(0.26f, 1.70f, WallHeight * 0.92f),
                        materials.OuterWall,
                        -7f,
                        0.014f);
                }

                return;
            }

            AddOrientedBox(
                parent,
                "ER-01 " + entrance.ClockLabel + " " + entrance.Key + " corridor floor stub",
                entrance.Degree,
                OuterRadius + 1.62f,
                0f,
                0f,
                new Vector3(1.55f, 1.50f, FloorThickness),
                materials.CorridorFloor,
                0f,
                0.015f);

            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? -0.96f : 0.96f;
                AddOrientedBox(
                    parent,
                    "ER-01 " + entrance.ClockLabel + " " + entrance.Key + " corridor side wall " + (i + 1),
                    entrance.Degree,
                    OuterRadius + 1.62f,
                    side,
                    WallHeight * 0.5f,
                    new Vector3(0.26f, 1.50f, WallHeight),
                    materials.OuterWall,
                    0f,
                    0.014f);
            }
        }

        private static void AddDoorwaySideSeals(Transform parent, EngineRoomMaterials materials)
        {
            const float sealDepth = 1.04f;
            const float sealOffset = 0.88f;
            var sealHeight = DoorOpeningHeight * 0.5f;
            var sealCenter = DoorOpeningHeight * 0.5f;
            var radialCenter = OuterRadius + 0.48f;

            for (var entryIndex = 0; entryIndex < Entrances.Length; entryIndex++)
            {
                var entrance = Entrances[entryIndex];
                for (var sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    var side = sideIndex == 0 ? -sealOffset : sealOffset;
                    AddOrientedBox(
                        parent,
                        "ER-01 " + entrance.ClockLabel + " " + entrance.Key + " doorway sealed side return wall " + (sideIndex + 1),
                        entrance.Degree,
                        radialCenter,
                        side,
                        sealCenter,
                        new Vector3(0.24f, sealDepth, sealHeight * 2f),
                        materials.OuterWall,
                        0f,
                        0.012f);
                }
            }
        }

        private static void AddCenterChamber(Transform parent, EngineRoomMaterials materials)
        {
            AddCylinder("ER-01 sealed transparent cylindrical power chamber glass", parent, new Vector3(0f, 1.34f, 0f), 1.06f, 2.28f, materials.Glass, 96);
            AddCylinder("ER-01 sealed cylinder lower metal cap", parent, new Vector3(0f, 0.30f, 0f), 1.12f, 0.18f, materials.Rim, 96);
            AddCylinder("ER-01 sealed cylinder upper metal cap", parent, new Vector3(0f, 2.46f, 0f), 1.12f, 0.18f, materials.Rim, 96);
            AddCylinder("ER-01 visible blue white inner power core column", parent, new Vector3(0f, 1.34f, 0f), 0.20f, 1.70f, materials.CoreGlow, 48);
            AddSphere("ER-01 visible contained power plasma", parent, new Vector3(0f, 1.34f, 0f), 0.44f, new Vector3(1f, 1.28f, 1f), materials.CorePlasma);

            for (var index = 0; index < 6; index++)
            {
                var degree = index * 60f;
                var start = AngleToLocalPosition(0.62f, degree, 0.60f);
                var end = AngleToLocalPosition(0.62f, degree, 2.06f);
                AddCylinderBetween("ER-01 visible insulated inner coil support " + (index + 1), parent, start, end, 0.018f, materials.CoreMetal, 12);
            }
        }

        private static void AddFloorGrating(Transform parent, EngineRoomMaterials materials, float startDegree, float endDegree, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0f : i / (float)(count - 1);
                var degree = Mathf.Lerp(startDegree, endDegree, t);
                AddBox(
                    parent,
                    "ER-01 radial deck rib " + Mathf.RoundToInt(degree),
                    AngleToLocalPosition((InnerRadius + OuterRadius) * 0.5f, degree, 0.035f),
                    new Vector3(0.040f, 0.035f, OuterRadius - InnerRadius - 0.45f),
                    Quaternion.Euler(0f, RadialYaw(degree), 0f),
                    materials.DeckRib,
                    0.002f);
            }
        }

        private static void AddRampHazardStripes(Transform parent, EngineRoomMaterials materials)
        {
            var cargo = GetEntrance("Cargo");
            var offsets = new[] { -0.54f, -0.18f, 0.18f, 0.54f };
            for (var i = 0; i < offsets.Length; i++)
            {
                AddOrientedBox(
                    parent,
                    "ER-01 5시 cargo ramp amber hazard stripe " + (i + 1),
                    cargo.Degree,
                    OuterRadius + 0.24f,
                    offsets[i],
                    0.025f,
                    new Vector3(0.12f, 0.82f, 0.032f),
                    materials.Hazard,
                    0f,
                    0.002f);
            }
        }

        private static void AddBolts(Transform parent, EngineRoomMaterials materials)
        {
            var degrees = new[] { -150f, -120f, -95f, -35f, 35f, 95f, 120f, 150f };
            for (var i = 0; i < degrees.Length; i++)
            {
                var radial = RadialDirection(degrees[i]);
                var rotation = Quaternion.FromToRotation(Vector3.up, radial);
                AddCylinder(
                    "ER-01 outer wall exposed structural bolt " + (i + 1),
                    parent,
                    AngleToLocalPosition(OuterRadius + 0.03f, degrees[i], 0.28f),
                    0.045f,
                    0.036f,
                    materials.Bolt,
                    16,
                    rotation);
            }
        }

        private static void AddDirectionLabel(Transform parent, EntranceSpec entrance, EngineRoomMaterials materials)
        {
            var z = entrance.Key == "Cargo" ? 1.30f : 1.42f;
            var position = LocalEntryPosition(entrance.Degree, OuterRadius + 0.09f, 0f, z);
            var rotation = Quaternion.Euler(0f, RadialYaw(entrance.Degree), 0f);

            AddBox(
                parent,
                "ER-01 " + entrance.ClockLabel + " " + entrance.Key + " wall label plate",
                position,
                new Vector3(1.16f, 0.34f, 0.045f),
                rotation,
                materials.LabelPlate,
                0.010f);

            var textObject = new GameObject("ER-01 " + entrance.ClockLabel + " " + entrance.Key + " direction text");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = position + RadialDirection(entrance.Degree) * 0.036f + new Vector3(0f, -0.02f, 0f);
            textObject.transform.localRotation = rotation;
            textObject.transform.localScale = Vector3.one;

            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = entrance.DisplayName;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.18f;
            mesh.fontSize = 72;
            mesh.color = new Color(0.75f, 0.86f, 0.82f, 1f);

            var renderer = textObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.LabelText;
        }

        private static void AddDressing(Transform parent, EngineRoomMaterials materials)
        {
            for (var i = 0; i < 2; i++)
            {
                var z = i == 0 ? -0.45f : 0.45f;
                AddCylinderBetween(
                    "ER-01 ceiling conduit across shell opening " + (i + 1),
                    parent,
                    new Vector3(-OuterRadius + 0.45f, WallHeight + 0.25f, z),
                    new Vector3(OuterRadius - 0.45f, WallHeight + 0.25f, z),
                    0.026f,
                    materials.Conduit,
                    14);
            }

            var cargo = GetEntrance("Cargo");
            AddCylinderBetween(
                "ER-01 cargo ramp side utility pipe left",
                parent,
                LocalEntryPosition(cargo.Degree, OuterRadius + 2.30f, -1.24f, 0.72f),
                LocalEntryPosition(cargo.Degree, OuterRadius + 0.28f, -1.24f, 0.98f),
                0.030f,
                materials.Conduit,
                14);
            AddCylinderBetween(
                "ER-01 cargo ramp side utility pipe right",
                parent,
                LocalEntryPosition(cargo.Degree, OuterRadius + 2.30f, 1.24f, 0.72f),
                LocalEntryPosition(cargo.Degree, OuterRadius + 0.28f, 1.24f, 0.98f),
                0.030f,
                materials.Conduit,
                14);
        }

        private static void AddInspectionLights(Transform parent)
        {
            AddLight(parent, "ER-01 large overhead room inspection softbox", LightType.Rectangle, new Vector3(0f, 5.2f, 0f), new Color(1f, 0.95f, 0.86f, 1f), 360f, 8f, new Vector2(6.8f, 6.8f));
            AddLight(parent, "ER-01 cool corridor entry fill", LightType.Point, new Vector3(2.8f, 2.8f, 4.3f), new Color(0.70f, 0.90f, 1f, 1f), 85f, 10f, Vector2.one);
            AddLight(parent, "ER-01 warm cargo ramp low light", LightType.Point, LocalEntryPosition(GetEntrance("Cargo").Degree, OuterRadius + 1.25f, 0f, 1.15f), new Color(1f, 0.62f, 0.32f, 1f), 80f, 8f, Vector2.one);
        }

        private static void AddLight(Transform parent, string name, LightType type, Vector3 position, Color color, float intensity, float range, Vector2 areaSize)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            var light = lightObject.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            if (type == LightType.Rectangle)
            {
                light.lightmapBakeType = LightmapBakeType.Baked;
                light.areaSize = areaSize;
            }
        }

        private static void AddCorridorOpenedCylindricalShell(Transform parent, string name, float innerRadius, float outerRadius, float yBase, float height, Material material)
        {
            var doorwayRanges = new List<AngularRange>();
            for (var i = 0; i < Entrances.Length; i++)
            {
                doorwayRanges.Add(new AngularRange(Entrances[i].Degree - DoorOpeningHalfDegrees, Entrances[i].Degree + DoorOpeningHalfDegrees));
            }

            doorwayRanges.Sort((left, right) => left.Start.CompareTo(right.Start));

            var solidRanges = new List<AngularRange>();
            var cursor = -180f;
            for (var i = 0; i < doorwayRanges.Count; i++)
            {
                if (doorwayRanges[i].Start > cursor)
                {
                    solidRanges.Add(new AngularRange(cursor, doorwayRanges[i].Start));
                }

                cursor = Mathf.Max(cursor, doorwayRanges[i].End);
            }

            if (cursor < 180f)
            {
                solidRanges.Add(new AngularRange(cursor, 180f));
            }

            var lowerHeight = Mathf.Min(DoorOpeningHeight, height);
            for (var i = 0; i < solidRanges.Count; i++)
            {
                var range = solidRanges[i];
                var arcDegrees = Mathf.Abs(range.End - range.Start);
                var segments = Mathf.Max(18, Mathf.RoundToInt(256f * arcDegrees / 360f));
                AddCylindricalShell(
                    name + " lower sealed section " + (i + 1),
                    parent,
                    innerRadius,
                    outerRadius,
                    range.Start,
                    range.End,
                    yBase,
                    lowerHeight,
                    material,
                    segments);
            }

            var upperHeight = Mathf.Max(height - lowerHeight, 0f);
            if (upperHeight > 0.01f)
            {
                AddCylindricalShell(
                    name + " continuous upper doorway header",
                    parent,
                    innerRadius,
                    outerRadius,
                    -180f,
                    180f,
                    yBase + lowerHeight,
                    upperHeight,
                    material,
                    256);
            }
        }

        private static GameObject AddOrientedBox(
            Transform parent,
            string name,
            float degree,
            float radialDistance,
            float tangentOffset,
            float height,
            Vector3 size,
            Material material,
            float tiltDegrees,
            float bevelWidth)
        {
            return AddBox(
                parent,
                name,
                LocalEntryPosition(degree, radialDistance, tangentOffset, height),
                size,
                Quaternion.Euler(tiltDegrees, RadialYaw(degree), 0f),
                material,
                bevelWidth);
        }

        private static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 size, Quaternion rotation, Material material, float bevelWidth)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            DisableCollider(obj);
            return obj;
        }

        private static GameObject AddCylinder(
            string name,
            Transform parent,
            Vector3 localPosition,
            float radius,
            float height,
            Material material,
            int segments,
            Quaternion? localRotation = null)
        {
            var mesh = CreateCylinderMesh(name + " Mesh", radius, height, segments);
            var obj = CreateMeshObject(name, parent, mesh, material);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation ?? Quaternion.identity;
            return obj;
        }

        private static void AddCylinderBetween(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material, int segments)
        {
            var direction = end - start;
            var midpoint = (start + end) * 0.5f;
            var rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            AddCylinder(name, parent, midpoint, radius, direction.magnitude, material, segments, rotation);
        }

        private static void AddSphere(string name, Transform parent, Vector3 localPosition, float radius, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = new Vector3(radius * 2f * scale.x, radius * 2f * scale.y, radius * 2f * scale.z);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            DisableCollider(obj);
        }

        private static void AddAnnularSector(
            string name,
            Transform parent,
            float innerRadius,
            float outerRadius,
            float startDegree,
            float endDegree,
            float y,
            float thickness,
            Material material,
            int segments)
        {
            var verts = new List<Vector3>();
            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(outerRadius, degree, y + thickness * 0.5f));
            }

            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(innerRadius, degree, y + thickness * 0.5f));
            }

            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(outerRadius, degree, y - thickness * 0.5f));
            }

            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(innerRadius, degree, y - thickness * 0.5f));
            }

            var tris = new List<int>();
            var n = segments + 1;
            for (var i = 0; i < segments; i++)
            {
                AddQuad(tris, i, i + 1, n + i + 1, n + i);
                AddQuad(tris, 2 * n + i + 1, 2 * n + i, 3 * n + i, 3 * n + i + 1);
                AddQuad(tris, i + 1, 2 * n + i + 1, 3 * n + i + 1, n + i + 1);
                AddQuad(tris, 2 * n + i, i, n + i, 3 * n + i);
            }

            AddQuad(tris, 0, n, 3 * n, 2 * n);
            AddQuad(tris, segments, 2 * n + segments, 3 * n + segments, n + segments);
            CreateMeshObject(name, parent, CreateMesh(name + " Mesh", verts, tris), material);
        }

        private static void AddCylindricalShell(
            string name,
            Transform parent,
            float innerRadius,
            float outerRadius,
            float startDegree,
            float endDegree,
            float yBase,
            float height,
            Material material,
            int segments)
        {
            var verts = new List<Vector3>();
            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(outerRadius, degree, yBase));
            }

            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(outerRadius, degree, yBase + height));
            }

            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(innerRadius, degree, yBase));
            }

            for (var i = 0; i <= segments; i++)
            {
                var degree = Mathf.Lerp(startDegree, endDegree, i / (float)segments);
                verts.Add(AngleToLocalPosition(innerRadius, degree, yBase + height));
            }

            var tris = new List<int>();
            var n = segments + 1;
            for (var i = 0; i < segments; i++)
            {
                var j = i + 1;
                AddQuad(tris, i, j, n + j, n + i);
                AddQuad(tris, 2 * n + j, 2 * n + i, 3 * n + i, 3 * n + j);
                AddQuad(tris, n + i, n + j, 3 * n + j, 3 * n + i);
                AddQuad(tris, j, i, 2 * n + i, 2 * n + j);
            }

            AddQuad(tris, 0, n, 3 * n, 2 * n);
            AddQuad(tris, segments, 2 * n + segments, 3 * n + segments, n + segments);
            CreateMeshObject(name, parent, CreateMesh(name + " Mesh", verts, tris), material);
        }

        private static Mesh CreateCylinderMesh(string name, float radius, float height, int segments)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var yMin = -height * 0.5f;
            var yMax = height * 0.5f;

            for (var i = 0; i < segments; i++)
            {
                var degree = i * 360f / segments;
                var point = AngleToLocalPosition(radius, degree, 0f);
                verts.Add(new Vector3(point.x, yMin, point.z));
                verts.Add(new Vector3(point.x, yMax, point.z));
            }

            var bottomCenter = verts.Count;
            verts.Add(new Vector3(0f, yMin, 0f));
            var topCenter = verts.Count;
            verts.Add(new Vector3(0f, yMax, 0f));

            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                var bottom = i * 2;
                var top = bottom + 1;
                var nextBottom = next * 2;
                var nextTop = nextBottom + 1;
                AddQuad(tris, bottom, nextBottom, nextTop, top);
                AddTriangle(tris, bottomCenter, nextBottom, bottom);
                AddTriangle(tris, topCenter, top, nextTop);
            }

            return CreateMesh(name, verts, tris);
        }

        private static Mesh CreateMesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh
            {
                name = name
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var filter = obj.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return obj;
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            AddTriangle(triangles, a, b, c);
            AddTriangle(triangles, a, c, d);
        }

        private static void AddTriangle(List<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private static Vector3 AngleToLocalPosition(float radius, float degree, float height)
        {
            var radians = degree * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians) * radius, height, Mathf.Sin(radians) * radius);
        }

        private static Vector3 LocalEntryPosition(float degree, float radialDistance, float tangentOffset, float height)
        {
            var radial = RadialDirection(degree);
            var tangent = TangentDirection(degree);
            return radial * radialDistance + tangent * tangentOffset + Vector3.up * height;
        }

        private static Vector3 RadialDirection(float degree)
        {
            var radians = degree * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        }

        private static Vector3 TangentDirection(float degree)
        {
            var radians = degree * Mathf.Deg2Rad;
            return new Vector3(-Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        private static float RadialYaw(float degree)
        {
            return 90f - degree;
        }

        private static EntranceSpec GetEntrance(string key)
        {
            for (var i = 0; i < Entrances.Length; i++)
            {
                if (Entrances[i].Key == key)
                {
                    return Entrances[i];
                }
            }

            throw new InvalidOperationException("Missing engine room entrance: " + key);
        }

        private static EngineRoomMaterials EnsureMaterials()
        {
            return new EngineRoomMaterials(
                EnsureMaterial("M_Er01_Floor", new Color(0.17f, 0.20f, 0.18f, 1f), 0.22f, 0.20f, false, false),
                EnsureMaterial("M_Er01_FloorPanel", new Color(0.13f, 0.16f, 0.15f, 1f), 0.18f, 0.22f, false, false),
                EnsureMaterial("M_Er01_CorridorFloor", new Color(0.15f, 0.18f, 0.17f, 1f), 0.20f, 0.22f, false, false),
                EnsureMaterial("M_Er01_RampFloor", new Color(0.18f, 0.20f, 0.19f, 1f), 0.20f, 0.24f, false, false),
                EnsureMaterial("M_Er01_OuterWall", new Color(0.22f, 0.27f, 0.24f, 1f), 0.22f, 0.18f, false, false),
                EnsureMaterial("M_Er01_WallLiner", new Color(0.20f, 0.25f, 0.23f, 1f), 0.20f, 0.20f, false, false),
                EnsureMaterial("M_Er01_Rim", new Color(0.30f, 0.32f, 0.28f, 1f), 0.26f, 0.26f, false, false),
                EnsureMaterial("M_Er01_DeckRib", new Color(0.095f, 0.105f, 0.10f, 1f), 0.20f, 0.16f, false, false),
                EnsureMaterial("M_Er01_Conduit", new Color(0.045f, 0.050f, 0.047f, 1f), 0.25f, 0.15f, false, false),
                EnsureMaterial("M_Er01_Bolt", new Color(0.34f, 0.34f, 0.30f, 1f), 0.30f, 0.26f, false, false),
                EnsureMaterial("M_Er01_HazardAmber", new Color(0.86f, 0.50f, 0.12f, 1f), 0.0f, 0.20f, false, false),
                EnsureMaterial("M_Er01_Glass", new Color(0.38f, 0.86f, 0.92f, 0.28f), 0.0f, 0.72f, true, true),
                EnsureMaterial("M_Er01_CoreGlow", new Color(0.42f, 0.95f, 1.0f, 1f), 0.0f, 0.50f, false, true),
                EnsureMaterial("M_Er01_CorePlasma", new Color(0.16f, 0.72f, 0.90f, 0.48f), 0.0f, 0.38f, true, true),
                EnsureMaterial("M_Er01_CoreMetal", new Color(0.42f, 0.43f, 0.38f, 1f), 0.28f, 0.24f, false, false),
                EnsureMaterial("M_Er01_LabelPlate", new Color(0.030f, 0.034f, 0.032f, 1f), 0.20f, 0.18f, false, false),
                EnsureMaterial("M_Er01_LabelText", new Color(0.75f, 0.86f, 0.82f, 1f), 0.0f, 0.42f, false, true));
        }

        private static Material EnsureMaterial(string name, Color color, float metallic, float smoothness, bool transparent, bool emissive)
        {
            var path = UnityAssetDirectory + "/" + name + ".mat";
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
            SetColor(material, "_BaseColor", color);
            SetColor(material, "_Color", color);
            SetFloat(material, "_Metallic", Mathf.Clamp01(metallic));
            SetFloat(material, "_Smoothness", Mathf.Clamp01(smoothness));

            if (transparent)
            {
                SetFloat(material, "_Surface", 1f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                SetFloat(material, "_Surface", 0f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = -1;
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                var emission = color * 1.8f;
                emission.a = 1f;
                SetColor(material, "_EmissionColor", emission);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetColor(Material material, string property, Color color)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, color);
            }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void EnsureNoCockpitOverlap(GameObject engineRoot)
        {
            var engineBounds = GetRendererBounds(engineRoot.transform);
            var cockpitRoots = new[]
            {
                ApprovedCockpitStructureBootstrap.RootName,
                ApprovedCockpitWindowBootstrap.RootName,
                ApprovedCockpitConsoleBootstrap.RootName,
                ApprovedCockpitWarningBootstrap.RootName,
                ApprovedCockpitDirectionBootstrap.RootName
            };

            for (var i = 0; i < cockpitRoots.Length; i++)
            {
                var cockpit = FindNamedObject(cockpitRoots[i]);
                if (cockpit == null || !TryGetRendererBounds(cockpit.transform, out var cockpitBounds))
                {
                    continue;
                }

                if (engineBounds.Intersects(cockpitBounds))
                {
                    throw new InvalidOperationException(
                        "Approved engine room shell overlaps existing cockpit object " +
                        cockpitRoots[i] +
                        ". EngineBounds=" +
                        FormatBounds(engineBounds) +
                        "; CockpitBounds=" +
                        FormatBounds(cockpitBounds));
                }
            }
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            if (TryGetRendererBounds(root, out var bounds))
            {
                return bounds;
            }

            throw new InvalidOperationException("No renderers found under " + root.name);
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            bounds = new Bounds(root.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            return hasBounds;
        }

        private static void DisableAllColliders(Transform root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void DisableCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private static void ApplyUserEditedTransformOverrides(Transform root)
        {
            if (UserEditedTransformOverrides.Length > 0)
            {
                RemoveGeneratedObjectsOutsideUserSnapshot(root);
            }

            for (var i = 0; i < UserEditedTransformOverrides.Length; i++)
            {
                var transform = FindRelativeTransform(root, UserEditedTransformOverrides[i].Path);
                if (transform == null)
                {
                    Debug.LogWarning("Missing ER-01 user edited transform override target: " + UserEditedTransformOverrides[i].Path);
                    continue;
                }

                transform.localPosition = UserEditedTransformOverrides[i].LocalPosition;
                transform.localRotation = UserEditedTransformOverrides[i].LocalRotation;
                transform.localScale = UserEditedTransformOverrides[i].LocalScale;
            }
        }

        private static void RemoveGeneratedObjectsOutsideUserSnapshot(Transform root)
        {
            var keptPaths = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < UserEditedTransformOverrides.Length; i++)
            {
                keptPaths.Add(UserEditedTransformOverrides[i].Path);
            }

            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (!keptPaths.Contains(GetRelativePath(root, transform)))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static int GetDepth(Transform transform)
        {
            var depth = 0;
            var current = transform;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static Transform FindRelativeTransform(Transform root, string relativePath)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null || transforms[i] == root)
                {
                    continue;
                }

                if (string.Equals(GetRelativePath(root, transforms[i]), relativePath, StringComparison.Ordinal))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform transform)
        {
            var segments = new List<string>();
            var current = transform;
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static GameObject RequireObject(string objectName)
        {
            var found = FindNamedObject(objectName);
            if (found == null)
            {
                throw new InvalidOperationException("Missing object: " + objectName);
            }

            return found;
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

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = FindNamedObject(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static string FormatBounds(Bounds bounds)
        {
            return "center=" + FormatVector(bounds.center) + ",size=" + FormatVector(bounds.size);
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00") + "," + value.y.ToString("0.00") + "," + value.z.ToString("0.00");
        }

        private static string FormatSourceVector(Vector3 value)
        {
            return "new Vector3(" +
                FormatSourceFloat(value.x) +
                "f, " +
                FormatSourceFloat(value.y) +
                "f, " +
                FormatSourceFloat(value.z) +
                "f)";
        }

        private static string FormatSourceQuaternion(Quaternion rotation)
        {
            return "Quaternion.Euler(" +
                FormatSourceFloat(NormalizeEuler(rotation.eulerAngles.x)) +
                "f, " +
                FormatSourceFloat(NormalizeEuler(rotation.eulerAngles.y)) +
                "f, " +
                FormatSourceFloat(NormalizeEuler(rotation.eulerAngles.z)) +
                "f)";
        }

        private static float NormalizeEuler(float value)
        {
            while (value > 180f)
            {
                value -= 360f;
            }

            while (value < -180f)
            {
                value += 360f;
            }

            return value;
        }

        private static string FormatSourceFloat(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private readonly struct EntranceSpec
        {
            public EntranceSpec(string key, string clockLabel, string displayName, float degree, bool isRamp)
            {
                Key = key;
                ClockLabel = clockLabel;
                DisplayName = displayName;
                Degree = degree;
                IsRamp = isRamp;
            }

            public string Key { get; }
            public string ClockLabel { get; }
            public string DisplayName { get; }
            public float Degree { get; }
            public bool IsRamp { get; }
        }

        private readonly struct AngularRange
        {
            public AngularRange(float start, float end)
            {
                Start = start;
                End = end;
            }

            public float Start { get; }
            public float End { get; }
        }

        private readonly struct TransformOverride
        {
            public TransformOverride(string path, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                Path = path;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public string Path { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct EngineRoomMaterials
        {
            public EngineRoomMaterials(
                Material floor,
                Material floorPanel,
                Material corridorFloor,
                Material rampFloor,
                Material outerWall,
                Material wallLiner,
                Material rim,
                Material deckRib,
                Material conduit,
                Material bolt,
                Material hazard,
                Material glass,
                Material coreGlow,
                Material corePlasma,
                Material coreMetal,
                Material labelPlate,
                Material labelText)
            {
                Floor = floor;
                FloorPanel = floorPanel;
                CorridorFloor = corridorFloor;
                RampFloor = rampFloor;
                OuterWall = outerWall;
                WallLiner = wallLiner;
                Rim = rim;
                DeckRib = deckRib;
                Conduit = conduit;
                Bolt = bolt;
                Hazard = hazard;
                Glass = glass;
                CoreGlow = coreGlow;
                CorePlasma = corePlasma;
                CoreMetal = coreMetal;
                LabelPlate = labelPlate;
                LabelText = labelText;
            }

            public Material Floor { get; }
            public Material FloorPanel { get; }
            public Material CorridorFloor { get; }
            public Material RampFloor { get; }
            public Material OuterWall { get; }
            public Material WallLiner { get; }
            public Material Rim { get; }
            public Material DeckRib { get; }
            public Material Conduit { get; }
            public Material Bolt { get; }
            public Material Hazard { get; }
            public Material Glass { get; }
            public Material CoreGlow { get; }
            public Material CorePlasma { get; }
            public Material CoreMetal { get; }
            public Material LabelPlate { get; }
            public Material LabelText { get; }
        }
    }
}
