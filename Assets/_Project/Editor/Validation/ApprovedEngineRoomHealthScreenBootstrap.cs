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
    public static class ApprovedEngineRoomHealthScreenBootstrap
    {
        public const string RootName = "Approved Engine Room 09 Health Screen";

        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/EngineRoom";
        private const string MainDisplayTexturePath = "Assets/Heavy Station Kit/BASE/Textures/Displays/B2_Eq41_E.png";
        private const string LeftAuxDisplayTexturePath = "Assets/Heavy Station Kit/BASE/Textures/Displays/B2_Eq52_E.png";
        private const string RightAuxDisplayTexturePath = "Assets/Heavy Station Kit/BASE/Textures/Displays/B2_Eq_23c.png";
        private const float WallAnchorRadius = 4.16f;
        private const string PlacementClockLabel = "9시";

        private static readonly Vector2 MainDisplayUvMin = new Vector2(0.0f, 0.75f);
        private static readonly Vector2 MainDisplayUvMax = new Vector2(0.5f, 1.0f);
        private static readonly Vector2 LeftAuxDisplayUvMin = new Vector2(0.0f, 2.0f / 3.0f);
        private static readonly Vector2 LeftAuxDisplayUvMax = new Vector2(0.25f, 1.0f);
        private static readonly Vector2 RightAuxDisplayUvMin = new Vector2(0.0f, 0.5f);
        private static readonly Vector2 RightAuxDisplayUvMax = new Vector2(0.5f, 1.0f);

        private static readonly Vector3 RadialOutward = Vector3.left;
        private static readonly Vector3 Tangent = Vector3.back;
        private static readonly Quaternion SampleRotation = Quaternion.LookRotation(Vector3.up, RadialOutward);
        private static readonly Quaternion SampleXRotation = Quaternion.LookRotation(Vector3.up, Tangent);
        private static readonly TransformOverride UserEditedRootTransformOverride =
            new TransformOverride(string.Empty, new Vector3(-13.7f, 0f, 18f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f));
        private static readonly TransformOverride[] UserEditedTransformOverrides =
        {
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 scaled big screen prefab footprint backplate", new Vector3(-4.068f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.86f, 0.135f, 2.2f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 dark vibration pad behind asset screen", new Vector3(-3.995f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.66f, 0.07f, 2.03f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 worn asset screen armored frame", new Vector3(-3.946f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.5f, 0.155f, 1.86f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 slightly recessed glass bevel lip", new Vector3(-3.868f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.15f, 0.02f, 1.4f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 B2_Eq41_E single display tile surface", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 runtime UI corner registration tab 1", new Vector3(-3.831f, 1.94f, 0.96f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.07f, 0.01f, 0.07f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 runtime UI corner registration tab 2", new Vector3(-3.831f, 1.94f, -0.96f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.07f, 0.01f, 0.07f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 runtime UI corner registration tab 3", new Vector3(-3.831f, 0.86f, 0.96f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.07f, 0.01f, 0.07f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 runtime UI corner registration tab 4", new Vector3(-3.831f, 0.86f, -0.96f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.07f, 0.01f, 0.07f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 ER-10 lower reserved connector cover", new Vector3(-3.94f, 0.38f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(1.08f, 0.15f, 0.3f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 lower cover inactive access seam", new Vector3(-3.852f, 0.38f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.88f, 0.014f, 0.052f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 left reserved connector screw", new Vector3(-3.846f, 0.38f, 0.42f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.048f, 0.005f, 0.048f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 right reserved connector screw", new Vector3(-3.846f, 0.38f, -0.42f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.048f, 0.005f, 0.048f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt#0", new Vector3(-3.924f, 0.6988f, 1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.084f, 0.013f, 0.084f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt slot#0", new Vector3(-3.908f, 0.6988f, 1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.05964f, 0.01f, 0.00924f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt#1", new Vector3(-3.924f, 2.2612f, 1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.084f, 0.013f, 0.084f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt slot#1", new Vector3(-3.908f, 2.2612f, 1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.05964f, 0.01f, 0.00924f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt#2", new Vector3(-3.924f, 0.6988f, -1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.084f, 0.013f, 0.084f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt slot#2", new Vector3(-3.908f, 0.6988f, -1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.05964f, 0.01f, 0.00924f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt#3", new Vector3(-3.924f, 2.2612f, -1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.084f, 0.013f, 0.084f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 asset screen corner bolt slot#3", new Vector3(-3.908f, 2.2612f, -1.125f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.05964f, 0.01f, 0.00924f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 worn exposed metal chip 3", new Vector3(-3.896f, 2.49f, -0.76f), Quaternion.Euler(-90f, 99f, 0f), new Vector3(0.16f, 0.01f, 0.02f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 worn exposed metal chip 6", new Vector3(-3.896f, 0.54f, -0.12f), Quaternion.Euler(-90f, 82.99999f, 0f), new Vector3(0.18f, 0.01f, 0.016f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 left decorative auxiliary wall screen", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 left decorative auxiliary wall screen/ER-09 left decorative auxiliary wall screen dark vibration gasket", new Vector3(-3.748f, 1.78f, 1.95f), Quaternion.Euler(-90f, 120f, 0f), new Vector3(1.06f, 0.05f, 1.3f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 left decorative auxiliary wall screen/ER-09 left decorative auxiliary wall screen compact worn frame", new Vector3(-3.742f, 1.78f, 1.95f), Quaternion.Euler(-90f, 120f, 0f), new Vector3(0.98f, 0.105f, 1.22f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 left decorative auxiliary wall screen/ER-09 left decorative auxiliary wall screen inner smoked lip", new Vector3(-3.691f, 1.78f, 1.95f), Quaternion.Euler(-90f, 120f, 0f), new Vector3(0.76f, 0.018f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 left decorative auxiliary wall screen/ER-09 left decorative auxiliary wall screen decorative B2_Eq41_E display tile", new Vector3(-1.314f, 0f, -1.66f), Quaternion.Euler(0f, 30.00001f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 right decorative auxiliary wall screen", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 right decorative auxiliary wall screen/ER-09 right decorative auxiliary wall screen recessed mount pad", new Vector3(-3.638f, 0.88f, -2.179f), Quaternion.Euler(-90f, 60f, 0f), new Vector3(1.4f, 0.08f, 1.06f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 right decorative auxiliary wall screen/ER-09 right decorative auxiliary wall screen dark vibration gasket", new Vector3(-3.608f, 0.88f, -2.179f), Quaternion.Euler(-90f, 60f, 0f), new Vector3(1.3f, 0.05f, 0.96f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 right decorative auxiliary wall screen/ER-09 right decorative auxiliary wall screen inner smoked lip", new Vector3(-3.561f, 0.88f, -2.179f), Quaternion.Euler(-90f, 60f, 0f), new Vector3(1f, 0.018f, 0.66f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 right decorative auxiliary wall screen/ER-09 right decorative auxiliary wall screen decorative B2_Eq41_E display tile", new Vector3(-1.199f, 0f, 1.431f), Quaternion.Euler(0f, -30f, 0f), new Vector3(1f, 1f, 1f)),
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Engine Room 09 Health Screen")]
        public static void EnsureApprovedEngineRoomHealthScreen()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var engineRoomRoot = RequireObject(ApprovedEngineRoomShellBootstrap.RootName);

            DeleteGeneratedObject(RootName);
            Directory.CreateDirectory(UnityAssetDirectory);

            var materials = EnsureMaterials();
            var root = new GameObject(RootName);
            root.transform.position = engineRoomRoot.transform.position;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            BuildScreenSet(root.transform, materials);
            ApplyUserEditedTransformOverrides(root.transform);
            DisableAllColliders(root.transform);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var comparisonPath = CaptureUnityComparison(root.transform);

            Debug.Log(
                "Approved ER-09 engine room health screen applied. Root=" +
                RootName +
                "; PlacementClock=" +
                PlacementClockLabel +
                "; EngineRoomRootFound=True" +
                "; Parts=" +
                root.GetComponentsInChildren<Renderer>(true).Length +
                "; UnityComparisonSaved=True" +
                "; Comparison=" +
                comparisonPath);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Engine Room 09 Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for ER-09 current object capture.");
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
                throw new InvalidOperationException("Could not resolve project root for ER-09 current object capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, "artSample", "engine_room_health_screen", "editor_current");
            Directory.CreateDirectory(outputRoot);

            var builder = new StringBuilder();
            builder.AppendLine("# ER-09 Current Editor Objects");
            builder.AppendLine();
            builder.AppendLine("Captured from the currently open CargoRunMvp scene without regenerating ER-09.");
            builder.AppendLine("Use these values to reflect user-edited ER-09 screen placement in ApprovedEngineRoomHealthScreenBootstrap.");
            builder.AppendLine();
            builder.Append("private static readonly TransformOverride UserEditedRootTransformOverride = new TransformOverride(string.Empty, ")
                .Append(FormatSourceVector(root.transform.position))
                .Append(", ")
                .Append(FormatSourceQuaternion(root.transform.rotation))
                .Append(", ")
                .Append(FormatSourceVector(root.transform.localScale))
                .AppendLine(");");
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

            var outputPath = Path.Combine(outputRoot, "er09_current_objects.md");
            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Approved ER-09 current object capture saved: " + outputPath);
        }

        private static void BuildScreenSet(Transform root, ScreenMaterials materials)
        {
            var screen = AddGroup(root, "ER-09 wall screen set - 9 o'clock placement");

            AddBox("ER-09 engine room side wall placement proxy", screen, 0f, 0.075f, 1.45f, 5.60f, 0.18f, 2.95f, materials.Wall, 0f, 0.014f);
            AddBox("ER-09 screen installation height rail", screen, 0f, -0.032f, 0.42f, 5.36f, 0.034f, 0.070f, materials.Rail, 0f, 0.004f);
            AddBox("ER-09 upper conduit rail continuing through wall", screen, 0f, -0.034f, 2.86f, 5.18f, 0.050f, 0.085f, materials.Conduit, 0f, 0.006f);
            AddBox("ER-09 wall vertical rib framing screen bay left", screen, -2.44f, -0.040f, 1.52f, 0.105f, 0.075f, 2.55f, materials.Rib, 0f, 0.006f);
            AddBox("ER-09 wall vertical rib framing screen bay right", screen, 2.44f, -0.040f, 1.52f, 0.105f, 0.075f, 2.55f, materials.Rib, 0f, 0.006f);

            AddBox("ER-09 scaled big screen prefab footprint backplate", screen, 0f, -0.092f, 1.48f, 2.86f, 0.135f, 2.20f, materials.Mount, 0f, 0.024f);
            AddBox("ER-09 dark vibration pad behind asset screen", screen, 0f, -0.165f, 1.48f, 2.66f, 0.070f, 2.03f, materials.Rubber, 0f, 0.018f);
            AddBox("ER-09 worn asset screen armored frame", screen, 0f, -0.214f, 1.48f, 2.50f, 0.155f, 1.86f, materials.Frame, 0f, 0.030f);
            AddBox("ER-09 slightly recessed glass bevel lip", screen, 0f, -0.292f, 1.48f, 2.15f, 0.020f, 1.40f, materials.GlassLip, 0f, 0.012f);
            AddTexturedPanel(
                "ER-09 B2_Eq41_E single display tile surface",
                screen,
                0f,
                -0.326f,
                1.48f,
                2.02f,
                1.27f,
                materials.ComputerScreen,
                MainDisplayUvMin,
                MainDisplayUvMax);
            AddRuntimeUiAnchorMarkers(screen, materials);

            AddBox("ER-09 left side hinge lug from asset mount", screen, -1.45f, -0.190f, 1.48f, 0.140f, 0.190f, 0.76f, materials.Hinge, 0f, 0.012f);
            AddBox("ER-09 right side cable socket block", screen, 1.45f, -0.190f, 1.48f, 0.190f, 0.205f, 0.62f, materials.Hinge, 0f, 0.012f);
            AddCylinder("ER-09 right screen conduit socket", screen, 1.66f, -0.190f, 1.48f, 0.060f, 0.24f, materials.Conduit, CylinderAxis.SampleX);
            AddCylinder("ER-09 upper cable coupler", screen, 1.20f, -0.064f, 2.70f, 0.040f, 0.75f, materials.Conduit, CylinderAxis.SampleX);
            AddBox("ER-09 short cable drop from conduit to screen", screen, 1.36f, -0.096f, 2.44f, 0.070f, 0.070f, 0.50f, materials.Conduit, 0f, 0.012f);

            AddBox("ER-09 ER-10 lower reserved connector cover", screen, 0f, -0.220f, 0.38f, 1.08f, 0.150f, 0.30f, materials.Reserve, 0f, 0.018f);
            AddBox("ER-09 lower cover inactive access seam", screen, 0f, -0.308f, 0.38f, 0.88f, 0.014f, 0.052f, materials.BlankSurface, 0f, 0.003f);
            AddCylinder("ER-09 left reserved connector screw", screen, -0.42f, -0.314f, 0.38f, 0.024f, 0.010f, materials.Bolt, CylinderAxis.SampleY);
            AddCylinder("ER-09 right reserved connector screw", screen, 0.42f, -0.314f, 0.38f, 0.024f, 0.010f, materials.Bolt, CylinderAxis.SampleY);

            AddCornerBolts(screen, materials, 2.50f, 1.86f, 1.48f);
            AddWear(screen, materials);
            AddDecorativeAuxiliaryScreen(
                screen,
                materials,
                "ER-09 left decorative auxiliary wall screen",
                -1.95f,
                1.78f,
                0.68f,
                0.92f,
                RightAuxDisplayUvMin,
                RightAuxDisplayUvMax,
                materials.RightAuxScreen,
                -1f);
            AddDecorativeAuxiliaryScreen(
                screen,
                materials,
                "ER-09 right decorative auxiliary wall screen",
                1.94f,
                0.88f,
                0.92f,
                0.58f,
                LeftAuxDisplayUvMin,
                LeftAuxDisplayUvMax,
                materials.LeftAuxScreen,
                1f);
        }

        private static void AddRuntimeUiAnchorMarkers(Transform parent, ScreenMaterials materials)
        {
            AddBox("ER-09 runtime UI corner registration tab 1", parent, -0.96f, -0.329f, 1.94f, 0.070f, 0.010f, 0.070f, materials.Marker, 0f, 0.004f);
            AddBox("ER-09 runtime UI corner registration tab 2", parent, 0.96f, -0.329f, 1.94f, 0.070f, 0.010f, 0.070f, materials.Marker, 0f, 0.004f);
            AddBox("ER-09 runtime UI corner registration tab 3", parent, -0.96f, -0.329f, 0.86f, 0.070f, 0.010f, 0.070f, materials.Marker, 0f, 0.004f);
            AddBox("ER-09 runtime UI corner registration tab 4", parent, 0.96f, -0.329f, 0.86f, 0.070f, 0.010f, 0.070f, materials.Marker, 0f, 0.004f);
        }

        private static void AddCornerBolts(Transform parent, ScreenMaterials materials, float width, float height, float zCenter)
        {
            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddBolt(parent, "ER-09 asset screen corner bolt", sx * width * 0.45f, zCenter + sz * height * 0.42f, materials.Bolt, 0.042f);
                }
            }
        }

        private static void AddWear(Transform parent, ScreenMaterials materials)
        {
            AddBox("ER-09 worn exposed metal chip 1", parent, -1.10f, -0.264f, 2.52f, 0.13f, 0.010f, 0.020f, materials.Wear, 6f, 0.001f);
            AddBox("ER-09 worn exposed metal chip 2", parent, -0.62f, -0.264f, 2.54f, 0.19f, 0.010f, 0.018f, materials.Wear, -4f, 0.001f);
            AddBox("ER-09 worn exposed metal chip 3", parent, 0.76f, -0.264f, 2.49f, 0.16f, 0.010f, 0.020f, materials.Wear, 9f, 0.001f);
            AddBox("ER-09 worn exposed metal chip 4", parent, 1.22f, -0.264f, 1.27f, 0.13f, 0.010f, 0.018f, materials.Wear, -11f, 0.001f);
            AddBox("ER-09 worn exposed metal chip 5", parent, -1.33f, -0.264f, 1.52f, 0.11f, 0.010f, 0.014f, materials.Wear, 14f, 0.001f);
            AddBox("ER-09 worn exposed metal chip 6", parent, 0.12f, -0.264f, 0.54f, 0.18f, 0.010f, 0.016f, materials.Wear, -7f, 0.001f);
        }

        private static void AddDecorativeAuxiliaryScreen(
            Transform parent,
            ScreenMaterials materials,
            string name,
            float centerX,
            float centerZ,
            float screenWidth,
            float screenHeight,
            Vector2 uvMin,
            Vector2 uvMax,
            Material displayMaterial,
            float cableSide)
        {
            var group = AddGroup(parent, name);
            var frameWidth = screenWidth + 0.30f;
            var frameHeight = screenHeight + 0.30f;
            var mountWidth = frameWidth + 0.18f;
            var mountHeight = frameHeight + 0.18f;

            AddBox(name + " recessed mount pad", group, centerX, -0.118f, centerZ, mountWidth, 0.080f, mountHeight, materials.Mount, 0f, 0.014f);
            AddBox(name + " dark vibration gasket", group, centerX, -0.178f, centerZ, frameWidth + 0.08f, 0.050f, frameHeight + 0.08f, materials.Rubber, 0f, 0.012f);
            AddBox(name + " compact worn frame", group, centerX, -0.230f, centerZ, frameWidth, 0.105f, frameHeight, materials.Frame, 0f, 0.020f);
            AddBox(name + " inner smoked lip", group, centerX, -0.292f, centerZ, screenWidth + 0.08f, 0.018f, screenHeight + 0.08f, materials.GlassLip, 0f, 0.006f);
            AddTexturedPanel(name + " decorative B2_Eq41_E display tile", group, centerX, -0.322f, centerZ, screenWidth, screenHeight, displayMaterial, uvMin, uvMax);

            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddCylinder(
                        name + " compact corner bolt",
                        group,
                        centerX + sx * frameWidth * 0.42f,
                        -0.316f,
                        centerZ + sz * frameHeight * 0.40f,
                        0.026f,
                        0.012f,
                        materials.Bolt,
                        CylinderAxis.SampleY);
                }
            }

            var cableX = centerX + cableSide * (mountWidth * 0.50f + 0.10f);
            AddBox(name + " side cable socket", group, cableX, -0.220f, centerZ, 0.085f, 0.120f, screenHeight * 0.58f, materials.Hinge, 0f, 0.009f);
            AddCylinder(name + " round cable gland", group, cableX + cableSide * 0.075f, -0.218f, centerZ, 0.032f, 0.090f, materials.Conduit, CylinderAxis.SampleX);
            AddBox(
                name + " short decorative cable run",
                group,
                cableX + cableSide * 0.055f,
                -0.105f,
                centerZ + frameHeight * 0.38f,
                0.050f,
                0.050f,
                frameHeight * 0.42f,
                materials.Conduit,
                0f,
                0.010f);
        }

        private static void AddBolt(Transform parent, string name, float x, float z, Material material, float radius)
        {
            AddCylinder(name, parent, x, -0.236f, z, radius, 0.026f, material, CylinderAxis.SampleY);
            AddBox(name + " slot", parent, x, -0.252f, z, radius * 1.42f, 0.010f, radius * 0.22f, material, 0f, 0.001f);
        }

        private static Transform AddGroup(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }

        private static GameObject AddBox(
            string name,
            Transform parent,
            float sampleX,
            float sampleY,
            float sampleZ,
            float sizeX,
            float sizeY,
            float sizeZ,
            Material material,
            float sampleZRotationDegrees,
            float bevelWidth)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = ToLocal(sampleX, sampleY, sampleZ);
            obj.transform.localRotation = SampleRotation * Quaternion.Euler(0f, 0f, sampleZRotationDegrees);
            obj.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DisableCollider(obj);
            return obj;
        }

        private static GameObject AddCylinder(
            string name,
            Transform parent,
            float sampleX,
            float sampleY,
            float sampleZ,
            float radius,
            float depth,
            Material material,
            CylinderAxis axis)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = ToLocal(sampleX, sampleY, sampleZ);
            obj.transform.localRotation = axis == CylinderAxis.SampleX ? SampleXRotation : SampleRotation;
            obj.transform.localScale = new Vector3(radius * 2f, depth * 0.5f, radius * 2f);

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DisableCollider(obj);
            return obj;
        }

        private static GameObject AddTexturedPanel(
            string name,
            Transform parent,
            float sampleX,
            float sampleY,
            float sampleZ,
            float width,
            float height,
            Material material,
            Vector2 uvMin,
            Vector2 uvMax)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var mesh = new Mesh
            {
                name = name + " Mesh"
            };
            mesh.vertices = new[]
            {
                ToLocal(sampleX - halfWidth, sampleY, sampleZ - halfHeight),
                ToLocal(sampleX + halfWidth, sampleY, sampleZ - halfHeight),
                ToLocal(sampleX + halfWidth, sampleY, sampleZ + halfHeight),
                ToLocal(sampleX - halfWidth, sampleY, sampleZ + halfHeight)
            };
            mesh.uv = new[]
            {
                new Vector2(uvMin.x, uvMin.y),
                new Vector2(uvMax.x, uvMin.y),
                new Vector2(uvMax.x, uvMax.y),
                new Vector2(uvMin.x, uvMax.y)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var filter = obj.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return obj;
        }

        private static Vector3 ToLocal(float sampleX, float sampleY, float sampleZ)
        {
            return (RadialOutward * (WallAnchorRadius + sampleY)) +
                   (Tangent * sampleX) +
                   (Vector3.up * sampleZ);
        }

        private static ScreenMaterials EnsureMaterials()
        {
            var mainDisplayTexture = LoadRequiredTexture(MainDisplayTexturePath);
            var leftAuxDisplayTexture = LoadRequiredTexture(LeftAuxDisplayTexturePath);
            var rightAuxDisplayTexture = LoadRequiredTexture(RightAuxDisplayTexturePath);

            return new ScreenMaterials(
                EnsureMaterial("M_Er09_WallComputerTextureProxy", new Color(0.18f, 0.22f, 0.20f, 1f), 0.18f, 0.14f, false, false),
                EnsureMaterial("M_Er09_ScreenInstallationRail", new Color(0.34f, 0.34f, 0.28f, 1f), 0.34f, 0.18f, false, false),
                EnsureMaterial("M_Er09_WallVerticalRib", new Color(0.12f, 0.14f, 0.13f, 1f), 0.28f, 0.14f, false, false),
                EnsureMaterial("M_Er09_ScreenMount", new Color(0.18f, 0.20f, 0.18f, 1f), 0.30f, 0.16f, false, false),
                EnsureMaterial("M_Er09_BlackRubberPad", new Color(0.012f, 0.014f, 0.013f, 1f), 0.0f, 0.08f, false, false),
                EnsureMaterial("M_Er09_WornArmoredFrame", new Color(0.23f, 0.26f, 0.23f, 1f), 0.32f, 0.12f, false, false),
                EnsureMaterial("M_Er09_SmokedGlassLip", new Color(0.012f, 0.018f, 0.017f, 1f), 0.0f, 0.74f, false, false),
                EnsureTexturedMaterial("M_Er09_B2_Eq41_DisplayTile", mainDisplayTexture, new Color(0.35f, 0.82f, 0.74f, 1f), 0.0f, 0.58f),
                EnsureTexturedMaterial("M_Er09_LeftAux_B2_Eq52_DisplayTile", leftAuxDisplayTexture, new Color(0.35f, 0.82f, 0.74f, 1f), 0.0f, 0.58f),
                EnsureTexturedMaterial("M_Er09_RightAux_B2_Eq23c_DisplayTile", rightAuxDisplayTexture, new Color(0.35f, 0.82f, 0.74f, 1f), 0.0f, 0.58f),
                EnsureMaterial("M_Er09_BlankInactiveSurface", new Color(0.004f, 0.007f, 0.007f, 1f), 0.0f, 0.30f, false, true),
                EnsureMaterial("M_Er09_RuntimeUiCornerTab", new Color(0.10f, 0.22f, 0.22f, 1f), 0.0f, 0.45f, false, true),
                EnsureMaterial("M_Er09_HingeAndSocket", new Color(0.10f, 0.11f, 0.10f, 1f), 0.32f, 0.14f, false, false),
                EnsureMaterial("M_Er09_ScreenConduit", new Color(0.045f, 0.050f, 0.047f, 1f), 0.34f, 0.12f, false, false),
                EnsureMaterial("M_Er09_BoltHeads", new Color(0.34f, 0.34f, 0.30f, 1f), 0.38f, 0.22f, false, false),
                EnsureMaterial("M_Er09_ScrapedExposedMetal", new Color(0.68f, 0.66f, 0.56f, 1f), 0.42f, 0.45f, false, false),
                EnsureMaterial("M_Er09_InactiveOverclockConnectorCover", new Color(0.11f, 0.12f, 0.11f, 1f), 0.30f, 0.12f, false, false));
        }

        private static Texture2D LoadRequiredTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException("ER-09 display texture was not found: " + path);
            }

            return texture;
        }

        private static Material EnsureTexturedMaterial(string name, Texture2D texture, Color emissionColor, float metallic, float smoothness)
        {
            var material = EnsureMaterial(name, Color.white, metallic, smoothness, false, true);
            SetTexture(material, "_BaseMap", texture);
            SetTexture(material, "_MainTex", texture);
            SetTexture(material, "_EmissionMap", texture);
            SetColor(material, "_EmissionColor", emissionColor * 1.15f);
            EditorUtility.SetDirty(material);
            return material;
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

        private static string CaptureUnityComparison(Transform screenRoot)
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for ER-09 comparison capture.");
            }

            var comparisonRoot = Path.Combine(projectRoot.FullName, "artSample", "engine_room_health_screen", "unity_applied_comparison");
            Directory.CreateDirectory(comparisonRoot);

            var unityRenderPath = Path.Combine(comparisonRoot, "unity_er09_9_oclock_front.png");
            var sideBySidePath = Path.Combine(comparisonRoot, "side_by_side_01_front.png");
            var notesPath = Path.Combine(comparisonRoot, "comparison_notes.md");

            var cameraObject = new GameObject("Temporary ER-09 comparison camera");
            var lightObject = new GameObject("Temporary ER-09 comparison light");
            Camera camera = null;
            RenderTexture renderTexture = null;
            try
            {
                var target = screenRoot.position + ToLocal(0f, -0.12f, 1.44f);
                var cameraPosition = screenRoot.position + ToLocal(0f, -2.20f, 1.58f);

                camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(target, Vector3.up);
                camera.orthographic = true;
                camera.orthographicSize = 3.58f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.015f, 0.017f, 0.016f, 1f);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Rectangle;
                light.color = new Color(0.94f, 0.98f, 0.92f, 1f);
                light.intensity = 420f;
                light.range = 6f;
                light.transform.position = cameraPosition + new Vector3(0.3f, 1.2f, 0.1f);
                light.transform.LookAt(target, Vector3.up);

                renderTexture = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;

                File.WriteAllBytes(unityRenderPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                if (RenderTexture.active == renderTexture)
                {
                    RenderTexture.active = null;
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            TryCreateSideBySideComparison(projectRoot.FullName, unityRenderPath, sideBySidePath);
            WriteComparisonNotes(notesPath, unityRenderPath, sideBySidePath);
            AssetDatabase.Refresh();
            return sideBySidePath;
        }

        private static void TryCreateSideBySideComparison(string projectRoot, string unityRenderPath, string sideBySidePath)
        {
            var samplePath = Path.Combine(projectRoot, "artSample", "engine_room_health_screen", "renders", "01_front_all_states.png");
            if (!File.Exists(samplePath) || !File.Exists(unityRenderPath))
            {
                return;
            }

            var sample = LoadPng(samplePath);
            var unity = LoadPng(unityRenderPath);
            var height = Mathf.Min(sample.height, unity.height);
            var width = sample.width + unity.width + 24;
            var combined = new Texture2D(width, height, TextureFormat.RGB24, false);
            var gutterColor = new Color32(18, 20, 19, 255);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    combined.SetPixel(x, y, gutterColor);
                }
            }

            CopyTexture(sample, combined, 0, 0, sample.width, height);
            CopyTexture(unity, combined, sample.width + 24, 0, unity.width, height);
            combined.Apply();

            File.WriteAllBytes(sideBySidePath, combined.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sample);
            UnityEngine.Object.DestroyImmediate(unity);
            UnityEngine.Object.DestroyImmediate(combined);
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                throw new InvalidOperationException("Could not load PNG for ER-09 comparison: " + path);
            }

            return texture;
        }

        private static void CopyTexture(Texture2D source, Texture2D target, int offsetX, int offsetY, int width, int height)
        {
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    target.SetPixel(offsetX + x, offsetY + y, source.GetPixel(x, y));
                }
            }
        }

        private static void WriteComparisonNotes(string notesPath, string unityRenderPath, string sideBySidePath)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# ER-09 Unity Applied Comparison");
            builder.AppendLine();
            builder.AppendLine("- 기준 샘플: `artSample/engine_room_health_screen/renders/01_front_all_states.png`");
            builder.AppendLine("- Unity 캡처: `" + ToProjectRelative(unityRenderPath) + "`");
            builder.AppendLine("- 좌우 비교: `" + ToProjectRelative(sideBySidePath) + "`");
            builder.AppendLine("- 배치 기준: 동력실을 위에서 내려다본 기준 9시 방향 벽.");
            builder.AppendLine("- 보조 스크린은 기능 없는 장식용 스크린으로 유지했다.");
            File.WriteAllText(notesPath, builder.ToString(), new UTF8Encoding(false));
        }

        private static string ToProjectRelative(string path)
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                return path;
            }

            return path.Replace(projectRoot.FullName + Path.DirectorySeparatorChar, string.Empty).Replace('\\', '/');
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

        private static void SetTexture(Material material, string property, Texture texture)
        {
            if (material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static void ApplyUserEditedTransformOverrides(Transform root)
        {
            if (UserEditedTransformOverrides.Length == 0)
            {
                return;
            }

            root.position = UserEditedRootTransformOverride.LocalPosition;
            root.rotation = UserEditedRootTransformOverride.LocalRotation;
            root.localScale = UserEditedRootTransformOverride.LocalScale;
            RemoveGeneratedObjectsOutsideUserSnapshot(root);

            for (var i = 0; i < UserEditedTransformOverrides.Length; i++)
            {
                var transform = FindRelativeTransform(root, UserEditedTransformOverrides[i].Path);
                if (transform == null)
                {
                    Debug.LogWarning("Missing ER-09 user edited transform override target: " + UserEditedTransformOverrides[i].Path);
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
                segments.Add(GetUniquePathSegment(current));
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static string GetUniquePathSegment(Transform transform)
        {
            var parent = transform.parent;
            if (parent == null)
            {
                return transform.name;
            }

            var matchingSiblings = 0;
            var indexAmongMatches = 0;
            for (var i = 0; i < parent.childCount; i++)
            {
                var sibling = parent.GetChild(i);
                if (!string.Equals(sibling.name, transform.name, StringComparison.Ordinal))
                {
                    continue;
                }

                if (sibling == transform)
                {
                    indexAmongMatches = matchingSiblings;
                }

                matchingSiblings++;
            }

            if (matchingSiblings <= 1)
            {
                return transform.name;
            }

            return transform.name + "#" + indexAmongMatches.ToString(CultureInfo.InvariantCulture);
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

        private enum CylinderAxis
        {
            SampleY,
            SampleX
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

        private readonly struct ScreenMaterials
        {
            public ScreenMaterials(
                Material wall,
                Material rail,
                Material rib,
                Material mount,
                Material rubber,
                Material frame,
                Material glassLip,
                Material computerScreen,
                Material leftAuxScreen,
                Material rightAuxScreen,
                Material blankSurface,
                Material marker,
                Material hinge,
                Material conduit,
                Material bolt,
                Material wear,
                Material reserve)
            {
                Wall = wall;
                Rail = rail;
                Rib = rib;
                Mount = mount;
                Rubber = rubber;
                Frame = frame;
                GlassLip = glassLip;
                ComputerScreen = computerScreen;
                LeftAuxScreen = leftAuxScreen;
                RightAuxScreen = rightAuxScreen;
                BlankSurface = blankSurface;
                Marker = marker;
                Hinge = hinge;
                Conduit = conduit;
                Bolt = bolt;
                Wear = wear;
                Reserve = reserve;
            }

            public Material Wall { get; }
            public Material Rail { get; }
            public Material Rib { get; }
            public Material Mount { get; }
            public Material Rubber { get; }
            public Material Frame { get; }
            public Material GlassLip { get; }
            public Material ComputerScreen { get; }
            public Material LeftAuxScreen { get; }
            public Material RightAuxScreen { get; }
            public Material BlankSurface { get; }
            public Material Marker { get; }
            public Material Hinge { get; }
            public Material Conduit { get; }
            public Material Bolt { get; }
            public Material Wear { get; }
            public Material Reserve { get; }
        }
    }
}
