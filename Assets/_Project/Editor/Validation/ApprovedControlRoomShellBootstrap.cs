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
    public static class ApprovedControlRoomShellBootstrap
    {
        public const string RootName = "Approved Control Room 01 Shell";

        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/ControlRoom";
        private const float RoomWidth = 8.8f;
        private const float RoomNorthY = 3.4f;
        private const float RoomSouthY = -5.35f;
        private const float RoomDepth = RoomNorthY - RoomSouthY;
        private const float RoomCenterY = (RoomNorthY + RoomSouthY) * 0.5f;
        private const float RoomHeight = 3.2f;
        private const float FloorThickness = 0.18f;
        private const float WallThickness = 0.34f;
        private const float DoorWidth = 1.55f;
        private const float DoorHeight = 2.12f;
        private const float CockpitDoorSampleY = -1.30f;
        private const float CockpitGap = 0.20f;
        private const float EngineGap = 0.45f;

        private static readonly Vector3 UserEditedControlRoomCenter = new Vector3(13.20795f, 0f, 19.265f);
        private static readonly TransformOverride[] UserEditedTransformOverrides =
        {
            new TransformOverride("Floor - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Floor - individually editable/CR-01 sealed control room deck floor", new Vector3(0f, 0f, -0.975f), Quaternion.Euler(0f, 0f, 0f), new Vector3(8.8f, 0.18f, 8.75f), true),
            new TransformOverride("Walls - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Walls - individually editable/CR-01 north solid future screen wall shell", new Vector3(0f, 1.6f, 3.4f), Quaternion.Euler(0f, 0f, 0f), new Vector3(9.14f, 3.2f, 0.34f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon sealed wall segment 1", new Vector3(-3.0175f, 1.6f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(2.765f, 3.2f, 0.34f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon sealed wall segment 2", new Vector3(0f, 1.6f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.17f, 3.2f, 0.34f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon sealed final wall segment", new Vector3(3.0175f, 1.6f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(2.765f, 3.2f, 0.34f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon cargo bay doorway upper header", new Vector3(-0.86f, 2.66f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1.55f, 1.08f, 0.34f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon cargo bay doorway left frame", new Vector3(-1.635f, 1.06f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.18f, 2.12f, 0.44f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon cargo bay doorway right frame", new Vector3(-0.085f, 1.06f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.18f, 2.12f, 0.44f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon weapon room doorway upper header", new Vector3(0.86f, 2.66f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1.55f, 1.08f, 0.34f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon weapon room doorway left frame", new Vector3(0.085f, 1.06f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.18f, 2.12f, 0.44f), true),
            new TransformOverride("Walls - individually editable/CR-01 south attached cargo and weapon weapon room doorway right frame", new Vector3(1.635f, 1.06f, -5.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.18f, 2.12f, 0.44f), true),
            new TransformOverride("Walls - individually editable/CR-01 west wall separated sealed segment 1", new Vector3(-4.4f, 1.6f, -4.7875f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.34f, 3.2f, 1.125f), true),
            new TransformOverride("Walls - individually editable/CR-01 west wall separated sealed segment 2", new Vector3(-4.4f, 1.6f, -2.375f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.34f, 3.2f, 0.6f), true),
            new TransformOverride("Walls - individually editable/CR-01 west wall separated sealed final segment", new Vector3(-4.4f, 1.6f, 1.4375f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.34f, 3.2f, 3.925f), true),
            new TransformOverride("Walls - individually editable/CR-01 west cockpit angled doorway upper header", new Vector3(-4.4f, 2.66f, -1.3f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.34f, 1.08f, 1.55f), true),
            new TransformOverride("Walls - individually editable/CR-01 west cockpit angled doorway lower frame", new Vector3(-4.4f, 1.06f, -2.075f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.44f, 2.12f, 0.18f), true),
            new TransformOverride("Walls - individually editable/CR-01 west cockpit angled doorway upper frame", new Vector3(-4.4f, 1.06f, -0.525f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.44f, 2.12f, 0.18f), true),
            new TransformOverride("Walls - individually editable/CR-01 west engine room doorway upper header", new Vector3(-4.4f, 2.66f, -3.45f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.34f, 1.08f, 1.55f), true),
            new TransformOverride("Walls - individually editable/CR-01 east solid control room wall with no corridor", new Vector3(4.4f, 1.6f, -0.975f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.34f, 3.2f, 9.09f), true),
            new TransformOverride("Corridors - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Corridors - individually editable/CR-01 cockpit 40 degree outside only corridor floor continuation", new Vector3(-5.23f, 0f, -1.295f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.15f, 0.18f, 1.91f), true),
            new TransformOverride("Corridors - individually editable/CR-01 cockpit 40 degree outside only corridor side wall -0.96", new Vector3(-5.2f, 1.05f, -0.398f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.15f, 2.1f, 0.2f), true),
            new TransformOverride("Corridors - individually editable/CR-01 cockpit 40 degree outside only corridor side wall +0.96", new Vector3(-5.446f, 1.031f, -2.192f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.15f, 2.1f, 0.2f), true),
            new TransformOverride("Corridors - individually editable/CR-01 engine room left separated corridor floor continuation", new Vector3(-5.485f, 0f, -3.45f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.05f, 0.18f, 1.97f), true),
            new TransformOverride("Corridors - individually editable/CR-01 engine room left separated corridor side wall -0.99", new Vector3(-5.485f, 1.05f, -2.465f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.05f, 2.1f, 0.2f), true),
            new TransformOverride("Corridors - individually editable/CR-01 engine room left separated corridor side wall +0.99", new Vector3(-5.485f, 1.05f, -4.435f), Quaternion.Euler(0f, 180f, 0f), new Vector3(2.05f, 2.1f, 0.2f), true),
            new TransformOverride("Corridors - individually editable/CR-01 cargo bay south attached corridor floor continuation", new Vector3(-0.86f, 0f, -6.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2f, 0.18f, 1.83f), true),
            new TransformOverride("Corridors - individually editable/CR-01 cargo bay south attached corridor side wall -0.92", new Vector3(-1.775f, 1.05f, -6.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2f, 2.1f, 0.2f), true),
            new TransformOverride("Corridors - individually editable/CR-01 cargo bay south attached corridor side wall +0.92", new Vector3(0.055f, 1.05f, -6.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2f, 2.1f, 0.2f), true),
            new TransformOverride("Corridors - individually editable/CR-01 weapon room south attached corridor floor continuation", new Vector3(0.86f, 0f, -6.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2f, 0.18f, 1.83f), true),
            new TransformOverride("Corridors - individually editable/CR-01 weapon room south attached corridor side wall -0.92", new Vector3(-0.055f, 1.05f, -6.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2f, 2.1f, 0.2f), true),
            new TransformOverride("Corridors - individually editable/CR-01 weapon room south attached corridor side wall +0.92", new Vector3(1.775f, 1.05f, -6.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(2f, 2.1f, 0.2f), true),
            new TransformOverride("Internal Partition - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Internal Partition - individually editable/CR-01 internal partition left wall between entry and screen side", new Vector3(-2.54f, 1.275f, 0.56f), Quaternion.Euler(0f, 0f, 0f), new Vector3(3.72f, 2.55f, 0.18f), true),
            new TransformOverride("Internal Partition - individually editable/CR-01 internal partition right wall between entry and screen side", new Vector3(2.54f, 1.275f, 0.56f), Quaternion.Euler(0f, 0f, 0f), new Vector3(3.72f, 2.55f, 0.18f), true),
            new TransformOverride("Internal Partition - individually editable/CR-01 internal partition doorway header", new Vector3(0f, 2.285f, 0.56f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1.36f, 0.53f, 0.22f), true),
            new TransformOverride("Internal Partition - individually editable/CR-01 internal partition doorway left jamb", new Vector3(-0.68f, 1.01f, 0.56f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.14f, 2.02f, 0.26f), true),
            new TransformOverride("Internal Partition - individually editable/CR-01 internal partition doorway right jamb", new Vector3(0.68f, 1.01f, 0.56f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.14f, 2.02f, 0.26f), true),
            new TransformOverride("Direction Markers - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cargo bay large direction color plate", new Vector3(-0.86f, 1.5f, -5.38f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1.4f, 0.44f, 0.07f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 weapon room large direction color plate", new Vector3(0.86f, 1.5f, -5.38f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1.4f, 0.44f, 0.07f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 engine room large direction color plate", new Vector3(-3.809f, 1.55f, -3.45f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.07f, 0.44f, 1.46f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cockpit 40 degree large direction color plate", new Vector3(-3.656f, 1.55f, -1.25f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.58f, 0.44f, 0.07f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cockpit angled colored doorway threshold", new Vector3(-4.69f, 0.072f, -1.392f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.72f, 0.06f, 2.13f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cockpit angled floor guide stripe", new Vector3(-3.576f, 0.082f, -1.37f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.52f, 0.045f, 0.22f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 engine room left colored doorway threshold", new Vector3(-4.58f, 0.215f, -3.45f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.72f, 0.06f, 2.13f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 engine room left colored doorway upper banner", new Vector3(-4.46f, 2.38f, -3.45f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.16f, 0.34f, 2.33f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 engine room left colored doorway jamb -0.93", new Vector3(-4.46f, 1.03f, -2.515f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.16f, 1.78f, 0.14f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 engine room left colored doorway jamb +0.93", new Vector3(-4.46f, 1.03f, -4.385f), Quaternion.Euler(0f, 180f, 0f), new Vector3(0.16f, 1.78f, 0.14f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 engine room left floor guide stripe", new Vector3(-3.44f, 0.19f, -3.45f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.52f, 0.045f, 0.22f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cargo south colored doorway threshold", new Vector3(-0.86f, 0.215f, -5.53f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.72f, 0.06f, 2.13f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cargo south colored doorway upper banner", new Vector3(-0.86f, 2.38f, -5.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.16f, 0.34f, 2.33f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cargo south colored doorway jamb -0.93", new Vector3(-1.795f, 1.03f, -5.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.16f, 1.78f, 0.14f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cargo south colored doorway jamb +0.93", new Vector3(0.075f, 1.03f, -5.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.16f, 1.78f, 0.14f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 cargo south floor guide stripe", new Vector3(-0.86f, 0.19f, -4.39f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.52f, 0.045f, 0.22f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 weapon south colored doorway threshold", new Vector3(0.86f, 0.215f, -5.53f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.72f, 0.06f, 2.13f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 weapon south colored doorway upper banner", new Vector3(0.86f, 2.38f, -5.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.16f, 0.34f, 2.33f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 weapon south colored doorway jamb -0.93", new Vector3(-0.075f, 1.03f, -5.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.16f, 1.78f, 0.14f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 weapon south colored doorway jamb +0.93", new Vector3(1.795f, 1.03f, -5.41f), Quaternion.Euler(0f, 90f, 0f), new Vector3(0.16f, 1.78f, 0.14f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 weapon south floor guide stripe", new Vector3(0.86f, 0.19f, -4.39f), Quaternion.Euler(0f, 90f, 0f), new Vector3(1.52f, 0.045f, 0.22f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 left side cockpit engine separation wall pier", new Vector3(-4.38f, 1.35f, -2.38f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.2f, 2.7f, 0.48f), true),
            new TransformOverride("Direction Markers - individually editable/CR-01 south cargo weapon shared divider", new Vector3(0f, 1.3f, -5.33f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.18f, 2.6f, 0.28f), true),
            new TransformOverride("Dressing - individually editable", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Dressing - individually editable/CR-01 blank future main screen recessed wall bay", new Vector3(0f, 1.82f, 3.178f), Quaternion.Euler(0f, 0f, 0f), new Vector3(4.7f, 1.18f, 0.08f), true),
            new TransformOverride("Dressing - individually editable/CR-01 main screen bay upper structural lintel", new Vector3(0f, 2.52f, 3.138f), Quaternion.Euler(0f, 0f, 0f), new Vector3(5.05f, 0.18f, 0.14f), true),
            new TransformOverride("Dressing - individually editable/CR-01 main screen bay lower service sill", new Vector3(0f, 1.1f, 3.138f), Quaternion.Euler(0f, 0f, 0f), new Vector3(5.05f, 0.18f, 0.14f), true),
            new TransformOverride("Dressing - individually editable/CR-01 side blank vertical monitor recess left", new Vector3(-2.75f, 1.72f, 3.181f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.72f, 1.45f, 0.075f), true),
            new TransformOverride("Dressing - individually editable/CR-01 side blank vertical monitor recess right", new Vector3(2.75f, 1.72f, 3.181f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.72f, 1.45f, 0.075f), true),
            new TransformOverride("Dressing - individually editable/CR-01 control room deck rib +0.85", new Vector3(0f, 0.145f, 0.85f), Quaternion.Euler(0f, 0f, 0f), new Vector3(8.05f, 0.045f, 0.035f), true),
            new TransformOverride("Dressing - individually editable/CR-01 control room deck rib +1.70", new Vector3(0f, 0.145f, 1.7f), Quaternion.Euler(0f, 0f, 0f), new Vector3(8.05f, 0.045f, 0.035f), true),
            new TransformOverride("Dressing - individually editable/CR-01 side wall utility conduit west -2.60", new Vector3(-4.27f, 2.62f, -2.18f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.052f, 0.42f, 0.052f), true),
            new TransformOverride("Dressing - individually editable/CR-01 side wall utility conduit east -2.60", new Vector3(4.27f, 2.62f, -2.18f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.052f, 0.42f, 0.052f), true),
            new TransformOverride("Dressing - individually editable/CR-01 side wall utility conduit west -0.35", new Vector3(-4.27f, 2.62f, 0.07f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.052f, 0.42f, 0.052f), true),
            new TransformOverride("Dressing - individually editable/CR-01 side wall utility conduit east -0.35", new Vector3(4.27f, 2.62f, 0.07f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.052f, 0.42f, 0.052f), true),
            new TransformOverride("Dressing - individually editable/CR-01 side wall utility conduit west +2.60", new Vector3(-4.27f, 2.62f, 3.02f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.052f, 0.42f, 0.052f), true),
            new TransformOverride("Inspection Lights", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Inspection Lights/CR-01 large overhead control room inspection softbox", new Vector3(0f, 5.8f, -0.975f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
            new TransformOverride("Inspection Lights/CR-01 cool cockpit corridor fill", new Vector3(-5.9f, 2.742f, -2.35f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f), true),
        };

        private static readonly string[] CockpitRootNames =
        {
            ApprovedCockpitStructureBootstrap.RootName,
            ApprovedCockpitWindowBootstrap.RootName,
            ApprovedCockpitConsoleBootstrap.RootName,
            ApprovedCockpitWarningBootstrap.RootName,
            ApprovedCockpitDirectionBootstrap.RootName
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Control Room 01 Shell")]
        public static void EnsureApprovedControlRoomShell()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            var engineRoot = RequireObject(ApprovedEngineRoomShellBootstrap.RootName);
            var cockpitRoots = FindExistingObjects(CockpitRootNames);
            if (cockpitRoots.Count == 0)
            {
                throw new InvalidOperationException("No approved cockpit roots were found for CR-01 placement.");
            }

            var protectedRoots = new List<GameObject>(cockpitRoots.Count + 1) { engineRoot };
            protectedRoots.AddRange(cockpitRoots);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var engineBounds = GetRendererBounds(engineRoot.transform);
            var cockpitBounds = GetCombinedRendererBounds(cockpitRoots);

            DeleteGeneratedObject(RootName);
            Directory.CreateDirectory(UnityAssetDirectory);

            var materials = EnsureMaterials();
            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            BuildControlRoom(root.transform, materials);
            DisableAllColliders(root.transform);
            if (UserEditedTransformOverrides.Length > 0)
            {
                root.transform.position = UserEditedControlRoomCenter;
                ApplyUserEditedTransformOverrides(root.transform);
            }
            else
            {
                var localControlBounds = GetRendererBounds(root.transform);
                root.transform.position = DeterminePlacement(localControlBounds, cockpitBounds, engineBounds);
            }

            var controlBounds = GetRendererBounds(root.transform);
            EnsureNoOverlap(controlBounds, engineBounds, "engine room");
            EnsureNoOverlap(controlBounds, cockpitBounds, "cockpit");
            if (UserEditedTransformOverrides.Length == 0)
            {
                EnsurePlacedNextToCockpit(controlBounds, cockpitBounds);
            }
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved CR-01 control room shell applied. Root=" +
                RootName +
                "; Center=" +
                FormatVector(root.transform.position) +
                "; Bounds=" +
                FormatBounds(controlBounds) +
                "; Parts=" +
                root.GetComponentsInChildren<Renderer>(true).Length +
                "; CockpitUntouched=True" +
                "; EngineRoomUntouched=True" +
                "; ControlRoomPlacedNextToCockpit=True" +
                "; ControlRoomOverlapsEngineRoom=False" +
                "; ControlRoomOverlapsCockpit=False");
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Control Room 01 Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for control room current object capture.");
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
                throw new InvalidOperationException("Could not resolve project root for control room current object capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, "artSample", "control_room_shell", "editor_current");
            Directory.CreateDirectory(outputRoot);

            var builder = new StringBuilder();
            builder.AppendLine("# CR-01 Current Editor Objects");
            builder.AppendLine();
            builder.AppendLine("Captured from the currently open CargoRunMvp scene without regenerating CR-01.");
            builder.AppendLine("Use these values to reflect user-edited control room placement in ApprovedControlRoomShellBootstrap.");
            builder.AppendLine();
            builder.Append("private static readonly Vector3 UserEditedControlRoomCenter = ")
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
                    .Append(", ")
                    .Append(transform.gameObject.activeSelf ? "true" : "false")
                    .AppendLine("),");
            }

            builder.AppendLine("};");

            var outputPath = Path.Combine(outputRoot, "cr01_current_objects.md");
            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Approved CR-01 current object capture saved: " + outputPath);
        }

        private static Vector3 DeterminePlacement(Bounds localControlBounds, Bounds cockpitBounds, Bounds engineBounds)
        {
            var rootX = cockpitBounds.max.x - localControlBounds.min.x + CockpitGap;
            rootX = Mathf.Max(rootX, engineBounds.max.x - localControlBounds.min.x + EngineGap);
            var rootZ = cockpitBounds.center.z - CockpitDoorSampleY;
            return new Vector3(rootX, 0f, rootZ);
        }

        private static void BuildControlRoom(Transform root, ControlRoomMaterials materials)
        {
            var floorGroup = AddGroup(root, "Floor - individually editable");
            var wallGroup = AddGroup(root, "Walls - individually editable");
            var corridorGroup = AddGroup(root, "Corridors - individually editable");
            var partitionGroup = AddGroup(root, "Internal Partition - individually editable");
            var markerGroup = AddGroup(root, "Direction Markers - individually editable");
            var dressingGroup = AddGroup(root, "Dressing - individually editable");
            var lightGroup = AddGroup(root, "Inspection Lights");

            AddBox(
                "CR-01 sealed control room deck floor",
                floorGroup,
                new Vector3(0f, RoomCenterY, 0f),
                new Vector3(RoomWidth, RoomDepth, FloorThickness),
                materials.Floor,
                0f);
            AddBox(
                "CR-01 north solid future screen wall shell",
                wallGroup,
                new Vector3(0f, RoomNorthY, RoomHeight * 0.5f),
                new Vector3(RoomWidth + WallThickness, WallThickness, RoomHeight),
                materials.Wall,
                0f);

            AddDoubleDoorWallY(
                wallGroup,
                "CR-01 south attached cargo and weapon",
                RoomSouthY,
                new[]
                {
                    new DoorCenter("cargo bay", -0.86f),
                    new DoorCenter("weapon room", 0.86f)
                },
                materials);
            AddWestDoorWall(wallGroup, -RoomWidth * 0.5f, materials);
            AddBox(
                "CR-01 east solid control room wall with no corridor",
                wallGroup,
                new Vector3(RoomWidth * 0.5f, RoomCenterY, RoomHeight * 0.5f),
                new Vector3(WallThickness, RoomDepth + WallThickness, RoomHeight),
                materials.Wall,
                0f);

            AddOrientedCorridor(
                corridorGroup,
                "CR-01 cockpit 40 degree outside only",
                new Vector2(-RoomWidth * 0.5f - 0.64f, -1.30f),
                140f,
                2.15f,
                DoorWidth + 0.36f,
                materials);
            AddOrientedCorridor(
                corridorGroup,
                "CR-01 engine room left separated",
                new Vector2(-RoomWidth * 0.5f - 0.06f, -3.45f),
                180f,
                2.05f,
                DoorWidth + 0.42f,
                materials);
            AddOrientedCorridor(
                corridorGroup,
                "CR-01 cargo bay south attached",
                new Vector2(-0.86f, RoomSouthY - 0.06f),
                -90f,
                2.00f,
                DoorWidth + 0.28f,
                materials);
            AddOrientedCorridor(
                corridorGroup,
                "CR-01 weapon room south attached",
                new Vector2(0.86f, RoomSouthY - 0.06f),
                -90f,
                2.00f,
                DoorWidth + 0.28f,
                materials);

            AddInternalPartition(partitionGroup, materials);
            AddWallPanelGrid(dressingGroup, materials);
            AddDirectionMarkers(markerGroup, materials);
            AddEntryHighlights(markerGroup, materials);
            AddUtilityConduits(dressingGroup, materials);
            AddInspectionLights(lightGroup);
        }

        private static void AddDoubleDoorWallY(
            Transform parent,
            string name,
            float y,
            IReadOnlyList<DoorCenter> doorCenters,
            ControlRoomMaterials materials)
        {
            var intervals = new List<DoorInterval>();
            for (var i = 0; i < doorCenters.Count; i++)
            {
                var center = doorCenters[i].Center;
                intervals.Add(new DoorInterval(center - DoorWidth * 0.5f, center + DoorWidth * 0.5f, doorCenters[i].Label));
            }

            intervals.Sort((left, right) => left.Start.CompareTo(right.Start));

            var cursor = -RoomWidth * 0.5f;
            var zMid = RoomHeight * 0.5f;
            for (var i = 0; i < intervals.Count; i++)
            {
                if (intervals[i].Start > cursor)
                {
                    var width = intervals[i].Start - cursor;
                    AddBox(
                        name + " sealed wall segment " + (i + 1).ToString(CultureInfo.InvariantCulture),
                        parent,
                        new Vector3(cursor + width * 0.5f, y, zMid),
                        new Vector3(width, WallThickness, RoomHeight),
                        materials.Wall,
                        0f);
                }

                cursor = intervals[i].End;
            }

            if (cursor < RoomWidth * 0.5f)
            {
                var width = RoomWidth * 0.5f - cursor;
                AddBox(
                    name + " sealed final wall segment",
                    parent,
                    new Vector3(cursor + width * 0.5f, y, zMid),
                    new Vector3(width, WallThickness, RoomHeight),
                    materials.Wall,
                    0f);
            }

            var headerHeight = RoomHeight - DoorHeight;
            for (var i = 0; i < doorCenters.Count; i++)
            {
                var center = doorCenters[i].Center;
                var label = doorCenters[i].Label;
                AddBox(
                    name + " " + label + " doorway upper header",
                    parent,
                    new Vector3(center, y, DoorHeight + headerHeight * 0.5f),
                    new Vector3(DoorWidth, WallThickness, headerHeight),
                    materials.Wall,
                    0f);
                AddBox(
                    name + " " + label + " doorway left frame",
                    parent,
                    new Vector3(center - DoorWidth * 0.5f, y, DoorHeight * 0.5f),
                    new Vector3(0.18f, WallThickness + 0.10f, DoorHeight),
                    materials.DoorFrame,
                    0f);
                AddBox(
                    name + " " + label + " doorway right frame",
                    parent,
                    new Vector3(center + DoorWidth * 0.5f, y, DoorHeight * 0.5f),
                    new Vector3(0.18f, WallThickness + 0.10f, DoorHeight),
                    materials.DoorFrame,
                    0f);
            }
        }

        private static void AddWestDoorWall(Transform parent, float x, ControlRoomMaterials materials)
        {
            var doorSpecs = new[]
            {
                new DoorCenter("cockpit angled", -1.30f),
                new DoorCenter("engine room", -3.45f)
            };
            var intervals = new List<DoorInterval>();
            for (var i = 0; i < doorSpecs.Length; i++)
            {
                var center = doorSpecs[i].Center;
                intervals.Add(new DoorInterval(center - DoorWidth * 0.5f, center + DoorWidth * 0.5f, doorSpecs[i].Label));
            }

            intervals.Sort((left, right) => left.Start.CompareTo(right.Start));

            var cursor = RoomSouthY;
            var zMid = RoomHeight * 0.5f;
            for (var i = 0; i < intervals.Count; i++)
            {
                if (intervals[i].Start > cursor)
                {
                    var depth = intervals[i].Start - cursor;
                    AddBox(
                        "CR-01 west wall separated sealed segment " + (i + 1).ToString(CultureInfo.InvariantCulture),
                        parent,
                        new Vector3(x, cursor + depth * 0.5f, zMid),
                        new Vector3(WallThickness, depth, RoomHeight),
                        materials.Wall,
                        0f);
                }

                cursor = intervals[i].End;
            }

            if (cursor < RoomNorthY)
            {
                var depth = RoomNorthY - cursor;
                AddBox(
                    "CR-01 west wall separated sealed final segment",
                    parent,
                    new Vector3(x, cursor + depth * 0.5f, zMid),
                    new Vector3(WallThickness, depth, RoomHeight),
                    materials.Wall,
                    0f);
            }

            var headerHeight = RoomHeight - DoorHeight;
            for (var i = 0; i < doorSpecs.Length; i++)
            {
                var center = doorSpecs[i].Center;
                var label = doorSpecs[i].Label;
                AddBox(
                    "CR-01 west " + label + " doorway upper header",
                    parent,
                    new Vector3(x, center, DoorHeight + headerHeight * 0.5f),
                    new Vector3(WallThickness, DoorWidth, headerHeight),
                    materials.Wall,
                    0f);
                AddBox(
                    "CR-01 west " + label + " doorway lower frame",
                    parent,
                    new Vector3(x, center - DoorWidth * 0.5f, DoorHeight * 0.5f),
                    new Vector3(WallThickness + 0.10f, 0.18f, DoorHeight),
                    materials.DoorFrame,
                    0f);
                AddBox(
                    "CR-01 west " + label + " doorway upper frame",
                    parent,
                    new Vector3(x, center + DoorWidth * 0.5f, DoorHeight * 0.5f),
                    new Vector3(WallThickness + 0.10f, 0.18f, DoorHeight),
                    materials.DoorFrame,
                    0f);
            }
        }

        private static void AddInternalPartition(Transform parent, ControlRoomMaterials materials)
        {
            const float partitionY = 0.56f;
            const float partitionHeight = 2.55f;
            const float doorWidth = 1.36f;
            const float doorHeight = 2.02f;
            var leftWidth = RoomWidth * 0.5f - doorWidth * 0.5f;
            var rightWidth = RoomWidth * 0.5f - doorWidth * 0.5f;

            AddBox(
                "CR-01 internal partition left wall between entry and screen side",
                parent,
                new Vector3(-RoomWidth * 0.25f - doorWidth * 0.25f, partitionY, partitionHeight * 0.5f),
                new Vector3(leftWidth, 0.18f, partitionHeight),
                materials.Partition,
                0f);
            AddBox(
                "CR-01 internal partition right wall between entry and screen side",
                parent,
                new Vector3(RoomWidth * 0.25f + doorWidth * 0.25f, partitionY, partitionHeight * 0.5f),
                new Vector3(rightWidth, 0.18f, partitionHeight),
                materials.Partition,
                0f);
            AddBox(
                "CR-01 internal partition doorway header",
                parent,
                new Vector3(0f, partitionY, doorHeight + (partitionHeight - doorHeight) * 0.5f),
                new Vector3(doorWidth, 0.22f, partitionHeight - doorHeight),
                materials.Partition,
                0f);
            AddBox(
                "CR-01 internal partition doorway left jamb",
                parent,
                new Vector3(-doorWidth * 0.5f, partitionY, doorHeight * 0.5f),
                new Vector3(0.14f, 0.26f, doorHeight),
                materials.DoorFrame,
                0f);
            AddBox(
                "CR-01 internal partition doorway right jamb",
                parent,
                new Vector3(doorWidth * 0.5f, partitionY, doorHeight * 0.5f),
                new Vector3(0.14f, 0.26f, doorHeight),
                materials.DoorFrame,
                0f);
            AddWallText(
                "CR-01 internal partition passage label",
                parent,
                "가벽 출입문",
                new Vector3(0f, partitionY - 0.135f, 1.54f),
                0f,
                0.14f,
                new Color(0.78f, 0.88f, 0.84f, 1f));
        }

        private static void AddOrientedCorridor(
            Transform parent,
            string name,
            Vector2 center,
            float angleDegrees,
            float length,
            float width,
            ControlRoomMaterials materials)
        {
            var floorCenter = LocalXY(center, angleDegrees, length * 0.5f, 0f);
            AddBox(
                name + " corridor floor continuation",
                parent,
                new Vector3(floorCenter.x, floorCenter.y, 0f),
                new Vector3(length, width, FloorThickness),
                materials.CorridorFloor,
                angleDegrees);

            for (var sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                var side = sideIndex == 0 ? -width * 0.5f : width * 0.5f;
                var wallCenter = LocalXY(center, angleDegrees, length * 0.5f, side);
                AddBox(
                    name + " corridor side wall " + FormatSigned(side),
                    parent,
                    new Vector3(wallCenter.x, wallCenter.y, 1.05f),
                    new Vector3(length, 0.20f, 2.10f),
                    materials.WallDark,
                    angleDegrees);
            }
        }

        private static void AddWallPanelGrid(Transform parent, ControlRoomMaterials materials)
        {
            var northY = RoomNorthY - WallThickness * 0.55f;
            AddBox(
                "CR-01 blank future main screen recessed wall bay",
                parent,
                new Vector3(0f, northY - 0.035f, 1.82f),
                new Vector3(4.7f, 0.08f, 1.18f),
                materials.BlankPanel,
                0f);
            AddBox(
                "CR-01 main screen bay upper structural lintel",
                parent,
                new Vector3(0f, northY - 0.075f, 2.52f),
                new Vector3(5.05f, 0.14f, 0.18f),
                materials.DoorFrame,
                0f);
            AddBox(
                "CR-01 main screen bay lower service sill",
                parent,
                new Vector3(0f, northY - 0.075f, 1.10f),
                new Vector3(5.05f, 0.14f, 0.18f),
                materials.DoorFrame,
                0f);

            AddBox(
                "CR-01 side blank vertical monitor recess left",
                parent,
                new Vector3(-2.75f, northY - 0.032f, 1.72f),
                new Vector3(0.72f, 0.075f, 1.45f),
                materials.BlankPanel,
                0f);
            AddBox(
                "CR-01 side blank vertical monitor recess right",
                parent,
                new Vector3(2.75f, northY - 0.032f, 1.72f),
                new Vector3(0.72f, 0.075f, 1.45f),
                materials.BlankPanel,
                0f);

            foreach (var x in new[] { -3.4f, -1.7f, 0f, 1.7f, 3.4f })
            {
                AddBox(
                    "CR-01 floor access plate " + FormatSigned(x),
                    parent,
                    new Vector3(x, -0.15f, 0.105f),
                    new Vector3(1.25f, 1.72f, 0.045f),
                    materials.FloorPanel,
                    0f);
            }

            foreach (var y in new[] { -4.60f, -3.70f, -2.80f, -1.90f, -1.00f, -0.10f, 0.85f, 1.70f, 2.55f })
            {
                AddBox(
                    "CR-01 control room deck rib " + FormatSigned(y),
                    parent,
                    new Vector3(0f, y, 0.145f),
                    new Vector3(RoomWidth - 0.75f, 0.035f, 0.045f),
                    materials.DeckRib,
                    0f);
            }

            foreach (var x in new[] { -3.85f, -2.55f, -1.25f, 1.25f, 2.55f, 3.85f })
            {
                AddBox(
                    "CR-01 north wall armored rib " + FormatSigned(x),
                    parent,
                    new Vector3(x, RoomDepth * 0.5f - 0.19f, 1.62f),
                    new Vector3(0.10f, 0.12f, 2.35f),
                    materials.Beam,
                    0f);
            }
        }

        private static void AddDirectionMarkers(Transform parent, ControlRoomMaterials materials)
        {
            AddMarker(
                parent,
                "CR-01 cargo bay",
                "운송창고",
                new Vector3(-0.86f, RoomSouthY - 0.03f, 1.50f),
                0f,
                new Vector3(1.40f, 0.07f, 0.44f),
                materials.CargoMarker);
            AddMarker(
                parent,
                "CR-01 weapon room",
                "무기실",
                new Vector3(0.86f, RoomSouthY - 0.03f, 1.50f),
                0f,
                new Vector3(1.40f, 0.07f, 0.44f),
                materials.WeaponMarker);
            AddMarker(
                parent,
                "CR-01 engine room",
                "동력실",
                new Vector3(-RoomWidth * 0.5f - 0.03f, -3.45f, 1.55f),
                -90f,
                new Vector3(0.07f, 1.46f, 0.44f),
                materials.EngineMarker);
            AddMarker(
                parent,
                "CR-01 cockpit 40 degree",
                "조종실 40도",
                new Vector3(-RoomWidth * 0.5f - 0.58f, -1.30f, 1.55f),
                40f,
                new Vector3(1.58f, 0.07f, 0.44f),
                materials.CockpitMarker);
        }

        private static void AddMarker(
            Transform parent,
            string key,
            string text,
            Vector3 sampleLocation,
            float angleDegrees,
            Vector3 sampleScale,
            Material material)
        {
            AddBox(key + " large direction color plate", parent, sampleLocation, sampleScale, material, angleDegrees);
            AddWallText(
                key + " large direction text",
                parent,
                text,
                sampleLocation + new Vector3(0f, -0.045f, 0f),
                angleDegrees,
                0.195f,
                new Color(0.78f, 0.88f, 0.84f, 1f));
        }

        private static void AddEntryHighlights(Transform parent, ControlRoomMaterials materials)
        {
            AddPortal(
                parent,
                "CR-01 cockpit angled",
                "조종실 40도",
                new Vector2(-RoomWidth * 0.5f - 0.58f, -1.30f),
                140f,
                materials.CockpitMarker,
                true);
            AddPortal(
                parent,
                "CR-01 engine room left",
                "동력실",
                new Vector2(-RoomWidth * 0.5f - 0.06f, -3.45f),
                180f,
                materials.EngineMarker,
                false);
            AddPortal(
                parent,
                "CR-01 cargo south",
                "운송창고",
                new Vector2(-0.86f, RoomSouthY - 0.06f),
                -90f,
                materials.CargoMarker,
                false);
            AddPortal(
                parent,
                "CR-01 weapon south",
                "무기실",
                new Vector2(0.86f, RoomSouthY - 0.06f),
                -90f,
                materials.WeaponMarker,
                false);

            AddBox(
                "CR-01 left side cockpit engine separation wall pier",
                parent,
                new Vector3(-RoomWidth * 0.5f + 0.02f, -2.38f, 1.35f),
                new Vector3(0.20f, 0.48f, 2.70f),
                materials.DoorFrame,
                0f);
            AddBox(
                "CR-01 south cargo weapon shared divider",
                parent,
                new Vector3(0f, RoomSouthY + 0.02f, 1.30f),
                new Vector3(0.18f, 0.28f, 2.60f),
                materials.DoorFrame,
                0f);
        }

        private static void AddPortal(
            Transform parent,
            string key,
            string text,
            Vector2 center,
            float angleDegrees,
            Material material,
            bool angledBanner)
        {
            var threshold = LocalXY(center, angleDegrees, 0.12f, 0f);
            AddBox(
                key + " colored doorway threshold",
                parent,
                new Vector3(threshold.x, threshold.y, 0.215f),
                new Vector3(0.72f, DoorWidth + 0.58f, 0.060f),
                material,
                angleDegrees);
            AddBox(
                key + " colored doorway upper banner",
                parent,
                new Vector3(center.x, center.y, DoorHeight + 0.26f),
                new Vector3(0.16f, DoorWidth + 0.78f, 0.34f),
                material,
                angleDegrees);

            foreach (var side in new[] { -DoorWidth * 0.5f - 0.16f, DoorWidth * 0.5f + 0.16f })
            {
                var jamb = LocalXY(center, angleDegrees, 0f, side);
                AddBox(
                    key + " colored doorway jamb " + FormatSigned(side),
                    parent,
                    new Vector3(jamb.x, jamb.y, 1.03f),
                    new Vector3(0.16f, 0.14f, 1.78f),
                    material,
                    angleDegrees);
            }

            var guide = LocalXY(center, angleDegrees, -1.02f, 0f);
            AddBox(
                key + " floor guide stripe",
                parent,
                new Vector3(guide.x, guide.y, 0.190f),
                new Vector3(1.52f, 0.22f, 0.045f),
                material,
                angleDegrees);

            var textPosition = LocalXY(center, angleDegrees, -1.32f, 0f);
            AddFloorText(
                key + " floor guide label",
                parent,
                text,
                new Vector3(textPosition.x, textPosition.y, 0.255f),
                angledBanner ? angleDegrees : angleDegrees + 90f,
                0.165f,
                new Color(0.78f, 0.88f, 0.84f, 1f));
        }

        private static void AddUtilityConduits(Transform parent, ControlRoomMaterials materials)
        {
            foreach (var y in new[] { -2.6f, -0.35f, 2.6f })
            {
                AddCylinderBetween(
                    "CR-01 side wall utility conduit west " + FormatSigned(y),
                    parent,
                    new Vector3(-RoomWidth * 0.5f + 0.13f, y, 2.62f),
                    new Vector3(-RoomWidth * 0.5f + 0.13f, y + 0.84f, 2.62f),
                    0.026f,
                    materials.Conduit);
                AddCylinderBetween(
                    "CR-01 side wall utility conduit east " + FormatSigned(y),
                    parent,
                    new Vector3(RoomWidth * 0.5f - 0.13f, y, 2.62f),
                    new Vector3(RoomWidth * 0.5f - 0.13f, y + 0.84f, 2.62f),
                    0.026f,
                    materials.Conduit);
            }
        }

        private static void AddInspectionLights(Transform parent)
        {
            var topObject = new GameObject("CR-01 large overhead control room inspection softbox");
            topObject.transform.SetParent(parent, false);
            topObject.transform.localPosition = new Vector3(0f, 5.8f, RoomCenterY);
            var top = topObject.AddComponent<Light>();
            top.type = LightType.Rectangle;
            top.intensity = 430f;
            top.range = 12f;
            top.color = new Color(0.74f, 0.86f, 1f, 1f);

            var coolObject = new GameObject("CR-01 cool cockpit corridor fill");
            coolObject.transform.SetParent(parent, false);
            coolObject.transform.localPosition = ToUnityPosition(new Vector3(-6.2f, -1.35f, 2.25f));
            var cool = coolObject.AddComponent<Light>();
            cool.type = LightType.Point;
            cool.intensity = 185f;
            cool.range = 6f;
            cool.color = new Color(0.62f, 0.82f, 1f, 1f);
        }

        private static Transform AddGroup(Transform parent, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static GameObject AddBox(
            string name,
            Transform parent,
            Vector3 sampleLocation,
            Vector3 sampleScale,
            Material material,
            float angleDegrees)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = ToUnityPosition(sampleLocation);
            obj.transform.localRotation = Quaternion.Euler(0f, -angleDegrees, 0f);
            obj.transform.localScale = ToUnityScale(sampleScale);

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DisableCollider(obj);
            return obj;
        }

        private static GameObject AddCylinderBetween(
            string name,
            Transform parent,
            Vector3 sampleStart,
            Vector3 sampleEnd,
            float radius,
            Material material)
        {
            var start = ToUnityPosition(sampleStart);
            var end = ToUnityPosition(sampleEnd);
            var direction = end - start;
            var length = direction.magnitude;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = (start + end) * 0.5f;
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            obj.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DisableCollider(obj);
            return obj;
        }

        private static void AddWallText(
            string name,
            Transform parent,
            string text,
            Vector3 sampleLocation,
            float angleDegrees,
            float characterSize,
            Color color)
        {
            AddText(name, parent, text, sampleLocation, Quaternion.Euler(0f, -angleDegrees, 0f), characterSize, color);
        }

        private static void AddFloorText(
            string name,
            Transform parent,
            string text,
            Vector3 sampleLocation,
            float angleDegrees,
            float characterSize,
            Color color)
        {
            AddText(name, parent, text, sampleLocation, Quaternion.Euler(-90f, -angleDegrees, 0f), characterSize, color);
        }

        private static void AddText(
            string name,
            Transform parent,
            string text,
            Vector3 sampleLocation,
            Quaternion rotation,
            float characterSize,
            Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = ToUnityPosition(sampleLocation);
            obj.transform.localRotation = rotation;
            obj.transform.localScale = Vector3.one;

            var mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = characterSize;
            mesh.fontSize = 72;
            mesh.color = color;
        }

        private static Vector2 LocalXY(Vector2 center, float angleDegrees, float forward, float side)
        {
            var radians = angleDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(
                center.x + cos * forward - sin * side,
                center.y + sin * forward + cos * side);
        }

        private static Vector3 ToUnityPosition(Vector3 sample)
        {
            return new Vector3(sample.x, sample.z, sample.y);
        }

        private static Vector3 ToUnityScale(Vector3 sample)
        {
            return new Vector3(sample.x, sample.z, sample.y);
        }

        private static ControlRoomMaterials EnsureMaterials()
        {
            return new ControlRoomMaterials(
                EnsureMaterial("M_Cr01_Floor", new Color(0.14f, 0.17f, 0.17f, 1f), 0.18f, 0.20f, false),
                EnsureMaterial("M_Cr01_FloorPanel", new Color(0.19f, 0.22f, 0.21f, 1f), 0.18f, 0.24f, false),
                EnsureMaterial("M_Cr01_DeckRib", new Color(0.07f, 0.09f, 0.09f, 1f), 0.20f, 0.16f, false),
                EnsureMaterial("M_Cr01_CorridorFloor", new Color(0.16f, 0.19f, 0.19f, 1f), 0.18f, 0.22f, false),
                EnsureMaterial("M_Cr01_Wall", new Color(0.20f, 0.25f, 0.25f, 1f), 0.20f, 0.20f, false),
                EnsureMaterial("M_Cr01_WallDark", new Color(0.11f, 0.14f, 0.15f, 1f), 0.20f, 0.18f, false),
                EnsureMaterial("M_Cr01_DoorFrame", new Color(0.32f, 0.34f, 0.31f, 1f), 0.28f, 0.26f, false),
                EnsureMaterial("M_Cr01_Partition", new Color(0.16f, 0.20f, 0.20f, 1f), 0.20f, 0.19f, false),
                EnsureMaterial("M_Cr01_BlankPanel", new Color(0.025f, 0.055f, 0.060f, 1f), 0.0f, 0.35f, true),
                EnsureMaterial("M_Cr01_Beam", new Color(0.27f, 0.30f, 0.28f, 1f), 0.26f, 0.24f, false),
                EnsureMaterial("M_Cr01_Conduit", new Color(0.045f, 0.055f, 0.055f, 1f), 0.28f, 0.20f, false),
                EnsureMaterial("M_Cr01_CargoMarker", new Color(0.18f, 0.42f, 0.30f, 1f), 0.0f, 0.30f, true),
                EnsureMaterial("M_Cr01_CockpitMarker", new Color(0.12f, 0.28f, 0.58f, 1f), 0.0f, 0.30f, true),
                EnsureMaterial("M_Cr01_EngineMarker", new Color(0.72f, 0.42f, 0.12f, 1f), 0.0f, 0.28f, true),
                EnsureMaterial("M_Cr01_WeaponMarker", new Color(0.58f, 0.14f, 0.13f, 1f), 0.0f, 0.28f, true));
        }

        private static Material EnsureMaterial(string name, Color color, float metallic, float smoothness, bool emissive)
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
            SetFloat(material, "_Surface", 0f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                var emission = color * 1.45f;
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

        private static List<GameObject> FindExistingObjects(IEnumerable<string> names)
        {
            var found = new List<GameObject>();
            foreach (var name in names)
            {
                var obj = FindNamedObject(name);
                if (obj != null)
                {
                    found.Add(obj);
                }
            }

            return found;
        }

        private static List<ProtectedTransformSnapshot> CaptureProtectedSnapshots(IEnumerable<GameObject> roots)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            foreach (var root in roots)
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    if (transform == null)
                    {
                        continue;
                    }

                    snapshots.Add(new ProtectedTransformSnapshot(
                        root.name + "/" + GetRelativePath(root.transform, transform),
                        transform,
                        transform.localPosition,
                        transform.localRotation,
                        transform.localScale,
                        transform.gameObject.activeSelf));
                }
            }

            return snapshots;
        }

        private static void EnsureProtectedObjectsUntouched(IReadOnlyList<ProtectedTransformSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Protected object was removed: " + snapshot.Path);
                }

                if (snapshot.Transform.gameObject.activeSelf != snapshot.ActiveSelf)
                {
                    throw new InvalidOperationException("Protected object active state changed: " + snapshot.Path);
                }

                if (Vector3.Distance(snapshot.Transform.localPosition, snapshot.LocalPosition) > 0.0001f ||
                    Quaternion.Angle(snapshot.Transform.localRotation, snapshot.LocalRotation) > 0.001f ||
                    Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                {
                    throw new InvalidOperationException("Protected object transform changed: " + snapshot.Path);
                }
            }
        }

        private static Bounds GetCombinedRendererBounds(IEnumerable<GameObject> roots)
        {
            var hasBounds = false;
            var combined = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var root in roots)
            {
                if (root == null || !TryGetRendererBounds(root.transform, out var bounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combined = bounds;
                    hasBounds = true;
                    continue;
                }

                combined.Encapsulate(bounds);
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("No renderer bounds were found for the requested roots.");
            }

            return combined;
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

        private static void EnsureNoOverlap(Bounds controlBounds, Bounds protectedBounds, string protectedName)
        {
            if (controlBounds.Intersects(protectedBounds))
            {
                throw new InvalidOperationException(
                    "Approved CR-01 control room shell overlaps existing " +
                    protectedName +
                    ". ControlBounds=" +
                    FormatBounds(controlBounds) +
                    "; ProtectedBounds=" +
                    FormatBounds(protectedBounds));
            }
        }

        private static void EnsurePlacedNextToCockpit(Bounds controlBounds, Bounds cockpitBounds)
        {
            var gap = controlBounds.min.x - cockpitBounds.max.x;
            if (gap < -0.01f || gap > 1.20f)
            {
                throw new InvalidOperationException(
                    "Approved CR-01 control room shell is not placed next to the cockpit. GapX=" +
                    gap.ToString("0.00", CultureInfo.InvariantCulture) +
                    "; ControlBounds=" +
                    FormatBounds(controlBounds) +
                    "; CockpitBounds=" +
                    FormatBounds(cockpitBounds));
            }
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
            RemoveGeneratedObjectsOutsideUserSnapshot(root);

            for (var i = 0; i < UserEditedTransformOverrides.Length; i++)
            {
                var transform = FindRelativeTransform(root, UserEditedTransformOverrides[i].Path);
                if (transform == null)
                {
                    Debug.LogWarning("Missing CR-01 user edited transform override target: " + UserEditedTransformOverrides[i].Path);
                    continue;
                }

                transform.localPosition = UserEditedTransformOverrides[i].LocalPosition;
                transform.localRotation = UserEditedTransformOverrides[i].LocalRotation;
                transform.localScale = UserEditedTransformOverrides[i].LocalScale;
                transform.gameObject.SetActive(UserEditedTransformOverrides[i].ActiveSelf);
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

        private static string GetRelativePath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return ".";
            }

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

        private static string FormatSigned(float value)
        {
            return value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatBounds(Bounds bounds)
        {
            return "center=" + FormatVector(bounds.center) + ",size=" + FormatVector(bounds.size);
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00", CultureInfo.InvariantCulture) +
                   "," +
                   value.y.ToString("0.00", CultureInfo.InvariantCulture) +
                   "," +
                   value.z.ToString("0.00", CultureInfo.InvariantCulture);
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

        private readonly struct DoorCenter
        {
            public DoorCenter(string label, float center)
            {
                Label = label;
                Center = center;
            }

            public string Label { get; }
            public float Center { get; }
        }

        private readonly struct DoorInterval
        {
            public DoorInterval(float start, float end, string label)
            {
                Start = start;
                End = end;
                Label = label;
            }

            public float Start { get; }
            public float End { get; }
            public string Label { get; }
        }

        private readonly struct TransformOverride
        {
            public TransformOverride(string path, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, bool activeSelf)
            {
                Path = path;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ActiveSelf = activeSelf;
            }

            public string Path { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public bool ActiveSelf { get; }
        }

        private readonly struct ProtectedTransformSnapshot
        {
            public ProtectedTransformSnapshot(
                string path,
                Transform transform,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                bool activeSelf)
            {
                Path = path;
                Transform = transform;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ActiveSelf = activeSelf;
            }

            public string Path { get; }
            public Transform Transform { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public bool ActiveSelf { get; }
        }

        private readonly struct ControlRoomMaterials
        {
            public ControlRoomMaterials(
                Material floor,
                Material floorPanel,
                Material deckRib,
                Material corridorFloor,
                Material wall,
                Material wallDark,
                Material doorFrame,
                Material partition,
                Material blankPanel,
                Material beam,
                Material conduit,
                Material cargoMarker,
                Material cockpitMarker,
                Material engineMarker,
                Material weaponMarker)
            {
                Floor = floor;
                FloorPanel = floorPanel;
                DeckRib = deckRib;
                CorridorFloor = corridorFloor;
                Wall = wall;
                WallDark = wallDark;
                DoorFrame = doorFrame;
                Partition = partition;
                BlankPanel = blankPanel;
                Beam = beam;
                Conduit = conduit;
                CargoMarker = cargoMarker;
                CockpitMarker = cockpitMarker;
                EngineMarker = engineMarker;
                WeaponMarker = weaponMarker;
            }

            public Material Floor { get; }
            public Material FloorPanel { get; }
            public Material DeckRib { get; }
            public Material CorridorFloor { get; }
            public Material Wall { get; }
            public Material WallDark { get; }
            public Material DoorFrame { get; }
            public Material Partition { get; }
            public Material BlankPanel { get; }
            public Material Beam { get; }
            public Material Conduit { get; }
            public Material CargoMarker { get; }
            public Material CockpitMarker { get; }
            public Material EngineMarker { get; }
            public Material WeaponMarker { get; }
        }
    }
}
