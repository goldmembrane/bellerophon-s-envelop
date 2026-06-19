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
        private const string OverclockControlGroupName = "ER-10 approved overclock lever switch - right of health screen";
        private const string OverclockControlPreservePathPrefix =
            "ER-09 wall screen set - 9 o'clock placement/" + OverclockControlGroupName;
        private const string FlashlightChargingDockGroupName =
            "ER-15 approved flashlight charging dock - left of auxiliary screen";
        private const string FlashlightChargingDockPreservePathPrefix =
            "ER-09 wall screen set - 9 o'clock placement/" + FlashlightChargingDockGroupName;
        private const string CantabileWarningLightGroupName =
            "ER-20 approved cantabile resonance warning ceiling light - between screen and core";
        private const string CantabileWarningLightPreservePathPrefix =
            "ER-09 wall screen set - 9 o'clock placement/" + CantabileWarningLightGroupName;
        private const float FlashlightChargingDockInternalWallInset = 0.90f;
        private const float OverclockSamplePanelCenterZ = 1.26f;
        private const string MainDisplayObjectName = "ER-09 B2_Eq41_E single display tile surface";
        private const string LeftAuxiliaryDisplayObjectName =
            "ER-09 left decorative auxiliary wall screen decorative B2_Eq41_E display tile";
        private const string RightAuxiliaryDisplayObjectName =
            "ER-09 right decorative auxiliary wall screen decorative B2_Eq41_E display tile";

        private static readonly Vector2 MainDisplayUvMin = new Vector2(0.0f, 0.75f);
        private static readonly Vector2 MainDisplayUvMax = new Vector2(0.5f, 1.0f);
        private static readonly Vector2 LeftAuxDisplayUvMin = new Vector2(0.0f, 2.0f / 3.0f);
        private static readonly Vector2 LeftAuxDisplayUvMax = new Vector2(0.25f, 1.0f);
        private static readonly Vector2 RightAuxDisplayUvMin = new Vector2(0.0f, 0.5f);
        private static readonly Vector2 RightAuxDisplayUvMax = new Vector2(0.5f, 1.0f);
        private static readonly string[] DisplayObjectNames =
        {
            MainDisplayObjectName,
            LeftAuxiliaryDisplayObjectName,
            RightAuxiliaryDisplayObjectName
        };

        private static readonly Vector3 RadialOutward = Vector3.left;
        private static readonly Vector3 Tangent = Vector3.back;
        private static readonly Quaternion SampleRotation = Quaternion.LookRotation(Vector3.up, RadialOutward);
        private static readonly Quaternion SampleXRotation = Quaternion.LookRotation(Vector3.up, Tangent);
        private static readonly TransformOverride UserEditedRootTransformOverride = new TransformOverride(string.Empty, new Vector3(-13.7f, 0f, 18f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f));

        private static readonly TransformOverride[] UserEditedTransformOverrides =
        {
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 scaled big screen prefab footprint backplate", new Vector3(-4.068f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.86f, 0.135f, 2.2f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 dark vibration pad behind asset screen", new Vector3(-3.995f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.66f, 0.07f, 2.03f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 worn asset screen armored frame", new Vector3(-3.946f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.5f, 0.155f, 1.86f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 slightly recessed glass bevel lip", new Vector3(-3.868f, 1.48f, 0f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(2.15f, 0.02f, 1.4f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-09 B2_Eq41_E single display tile surface", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
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
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 armored flashlight charging dock backplate", new Vector3(-2.687f, 1.05f, -3.412f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.36f, 0.033f, 0.7f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 deep vertical flashlight sized recess", new Vector3(-2.697f, 1.047f, -3.389f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.19f, 0.017f, 0.56f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 left raised cradle rail", new Vector3(-2.757f, 1.048f, -3.335f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.034f, 0.039f, 0.58f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 right raised cradle rail", new Vector3(-2.613f, 1.049f, -3.481f), Quaternion.Euler(-90f, 127.4626f, 0f), new Vector3(0.034f, 0.039f, 0.58f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 lower receiving cup block", new Vector3(-2.682f, 0.767f, -3.415f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.25f, 0.046f, 0.075f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 upper passive retaining collar", new Vector3(-2.68f, 1.327f, -3.402f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.25f, 0.028f, 0.026f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 lower passive retaining collar", new Vector3(-2.686f, 0.888f, -3.413f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.25f, 0.028f, 0.026f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 rear copper contact strip left", new Vector3(-2.708f, 0.793f, -3.371f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.03f, 0.008f, 0.08f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 rear copper contact strip right", new Vector3(-2.649f, 0.792f, -3.43f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.03f, 0.008f, 0.08f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 spring loaded lower contact pin right", new Vector3(-2.674f, 0.75f, -3.404f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.02f, 0.005f, 0.02f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt#0", new Vector3(-2.8f, 0.74f, -3.3f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.024f, 0.0065f, 0.024f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt slot#0", new Vector3(-2.794f, 0.74f, -3.306f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.017f, 0.005f, 0.0026f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt#1", new Vector3(-2.8f, 1.36f, -3.3f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.024f, 0.0065f, 0.024f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt slot#1", new Vector3(-2.794f, 1.36f, -3.306f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.017f, 0.005f, 0.0026f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt#2", new Vector3(-2.57f, 0.74f, -3.52f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.024f, 0.0065f, 0.024f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt slot#2", new Vector3(-2.564f, 0.74f, -3.526f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.017f, 0.005f, 0.0026f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt#3", new Vector3(-2.57f, 1.36f, -3.52f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.024f, 0.0065f, 0.024f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 charging dock corner bolt slot#3", new Vector3(-2.564f, 1.36f, -3.526f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.017f, 0.005f, 0.0026f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 dock chipped edge 1", new Vector3(-2.767f, 1.32f, -3.328f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.052f, 0.005f, 0.008f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 dock chipped edge 2", new Vector3(-2.626f, 1.13f, -3.466f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.06f, 0.005f, 0.008f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-15 approved flashlight charging dock - left of auxiliary screen/ER-15 dock chipped edge 3", new Vector3(-2.632f, 0.85f, -3.458f), Quaternion.Euler(-90f, 45f, 0f), new Vector3(0.046f, 0.005f, 0.007f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 overclock control wall placement proxy", new Vector3(-2.901f, 1.468f, 2.965f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(1.2056f, 0.066f, 1.2496f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 armored overclock control backplate", new Vector3(-2.888f, 1.468f, 2.968f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(1.0384f, 0.0528f, 1.1088f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 raised lever face plate", new Vector3(-2.874f, 1.424f, 2.964f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.902f, 0.044f, 0.924f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 protective left lever rail", new Vector3(-2.69f, 1.4416f, 3.131f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.02464f, 0.0352f, 0.7788f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 protective right lever rail", new Vector3(-3.021f, 1.4416f, 2.804f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.02464f, 0.0352f, 0.7788f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 protective upper lever rail", new Vector3(-2.853f, 1.842f, 2.97f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.4708f, 0.0352f, 0.02552f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 protective lower lever rail", new Vector3(-2.855f, 1.043f, 2.961f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.4708f, 0.0352f, 0.02552f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 lever pivot axle cap", new Vector3(-2.839f, 1.4416f, 2.943f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.1276f, 0.0132f, 0.1276f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 lever pivot inner bolt", new Vector3(-2.828f, 1.4416f, 2.935f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.0484f, 0.00572f, 0.0484f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 spring return lever black steel arm", new Vector3(-2.769f, 1.603f, 3.007f), Quaternion.Euler(27f, 9.999999f, -15f), new Vector3(0.0396f, 0.176223f, 0.0396f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 rubberized red lever hand grip", new Vector3(-2.725f, 1.754f, 3.084f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.10384f, 0.10384f, 0.10384f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 red beacon recessed collar", new Vector3(-2.5886f, 1.6748f, 3.225999f), Quaternion.Euler(-90f, 135f, 0f), new Vector3(0.1584f, 0.011f, 0.1584f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 single red overclock active beacon", new Vector3(-2.571f, 1.6748f, 3.226f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.11f, 0.11f, 0.11f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 overclock panel corner bolt#0", new Vector3(-3.924f, 2.5228f, -1.7758f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.02816f, 0.013f, 0.02816f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 overclock panel corner bolt slot#0", new Vector3(-3.908f, 1.5372f, -1.7758f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.019994f, 0.01f, 0.003098f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 overclock panel corner bolt#1", new Vector3(-3.924f, 2.5228f, -2.7042f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.02816f, 0.013f, 0.02816f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 overclock panel corner bolt slot#1", new Vector3(-3.908f, 2.5228f, -1.7758f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.019994f, 0.01f, 0.003098f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 overclock panel corner bolt slot#2", new Vector3(-3.908f, 1.5372f, -2.7042f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.019994f, 0.01f, 0.003098f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-10 approved overclock lever switch - right of health screen/ER-10 overclock panel corner bolt slot#3", new Vector3(-3.908f, 2.5228f, -2.7042f), Quaternion.Euler(-90f, 90f, 0f), new Vector3(0.019994f, 0.01f, 0.003098f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core", new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 black ceiling backing disc", new Vector3(-2.34f, 2.806f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.68f, 0.015f, 0.68f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 black cylindrical warning light base", new Vector3(-2.34f, 2.72f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.49f, 0.0775f, 0.49f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 pale separation ring below base", new Vector3(-2.34f, 2.63f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.52f, 0.016f, 0.52f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 black lens socket lip", new Vector3(-2.34f, 2.595f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.45f, 0.02f, 0.45f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 red rounded dome lens end", new Vector3(-2.34f, 2.581f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.37f, 0.2294f, 0.37f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 black base rib upper", new Vector3(-2.34f, 2.768f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.504f, 0.005f, 0.504f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 black base rib lower", new Vector3(-2.34f, 2.675f, 0f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.504f, 0.005f, 0.504f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 visible ceiling mount bolt#0", new Vector3(-2.15f, 2.826f, 0.255f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.036f, 0.008f, 0.036f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 visible ceiling mount bolt#1", new Vector3(-2.53f, 2.826f, 0.255f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.036f, 0.008f, 0.036f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 visible ceiling mount bolt#2", new Vector3(-2.15f, 2.826f, -0.255f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.036f, 0.008f, 0.036f)),
            new TransformOverride("ER-09 wall screen set - 9 o'clock placement/ER-20 approved cantabile resonance warning ceiling light - between screen and core/ER-20 visible ceiling mount bolt#3", new Vector3(-2.53f, 2.826f, -0.255f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.036f, 0.008f, 0.036f)),
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

        [MenuItem("Bellerophon/Bootstrap/Flip Approved Engine Room 09 Display UVs")]
        public static void FlipApprovedEngineRoomHealthScreenDisplayUvs()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for ER-09 display UV flip.");
            }

            var normalizedActivePath = activeScene.path.Replace('\\', '/');
            var normalizedCargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\', '/');
            if (!string.Equals(normalizedActivePath, normalizedCargoPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Current active scene is not CargoRunMvp. ActiveScene=" + activeScene.path);
            }

            var changedCount = 0;
            if (SetDisplayMeshUvs(RequireObject(MainDisplayObjectName), MainDisplayUvMin, MainDisplayUvMax))
            {
                changedCount++;
            }

            if (SetDisplayMeshUvs(RequireObject(LeftAuxiliaryDisplayObjectName), RightAuxDisplayUvMin, RightAuxDisplayUvMax))
            {
                changedCount++;
            }

            if (SetDisplayMeshUvs(RequireObject(RightAuxiliaryDisplayObjectName), LeftAuxDisplayUvMin, LeftAuxDisplayUvMax))
            {
                changedCount++;
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("Approved ER-09 display UVs flipped horizontally: " + changedCount + "/" + DisplayObjectNames.Length);
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
            AddApprovedFlashlightChargingDock(screen, materials);
            AddApprovedOverclockLeverSwitch(screen, materials);
            AddApprovedCantabileWarningLight(screen, materials);
        }

        private static void AddApprovedFlashlightChargingDock(Transform parent, ScreenMaterials materials)
        {
            var group = AddGroup(parent, FlashlightChargingDockGroupName);
            const float centerX = -2.95f;
            const float centerZ = 1.25f;

            AddCurvedWallBox("ER-15 armored flashlight charging dock backplate", group, centerX, -0.050f, centerZ, 0.36f, 0.033f, 0.70f, materials.OverclockBackplate, 0f, 0.010f);
            AddCurvedWallBox("ER-15 deep vertical flashlight sized recess", group, centerX, -0.104f, centerZ, 0.190f, 0.017f, 0.560f, materials.Rubber, 0f, 0.014f);
            AddCurvedWallBox("ER-15 left raised cradle rail", group, centerX - 0.115f, -0.150f, centerZ, 0.034f, 0.039f, 0.580f, materials.Rail, 0f, 0.007f);
            AddCurvedWallBox("ER-15 right raised cradle rail", group, centerX + 0.115f, -0.150f, centerZ, 0.034f, 0.039f, 0.580f, materials.Rail, 0f, 0.007f);
            AddCurvedWallBox("ER-15 lower receiving cup block", group, centerX, -0.164f, centerZ - 0.330f, 0.250f, 0.046f, 0.075f, materials.Rail, 0f, 0.010f);
            AddCurvedWallBox("ER-15 upper passive retaining collar", group, centerX, -0.192f, centerZ + 0.260f, 0.250f, 0.028f, 0.026f, materials.OverclockGuard, 0f, 0.006f);
            AddCurvedWallBox("ER-15 lower passive retaining collar", group, centerX, -0.192f, centerZ - 0.170f, 0.250f, 0.028f, 0.026f, materials.OverclockGuard, 0f, 0.006f);

            AddCurvedWallBox("ER-15 rear copper contact strip left", group, centerX - 0.035f, -0.198f, centerZ - 0.320f, 0.030f, 0.008f, 0.080f, materials.OverclockStopContact, 0f, 0.003f);
            AddCurvedWallBox("ER-15 rear copper contact strip right", group, centerX + 0.035f, -0.198f, centerZ - 0.320f, 0.030f, 0.008f, 0.080f, materials.OverclockStopContact, 0f, 0.003f);
            AddCurvedWallCylinder("ER-15 spring loaded lower contact pin left", group, centerX - 0.035f, -0.222f, centerZ - 0.375f, 0.010f, 0.010f, materials.OverclockStopContact, CylinderAxis.SampleY);
            AddCurvedWallCylinder("ER-15 spring loaded lower contact pin right", group, centerX + 0.035f, -0.222f, centerZ - 0.375f, 0.010f, 0.010f, materials.OverclockStopContact, CylinderAxis.SampleY);

            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddCurvedWallBolt(group, "ER-15 charging dock corner bolt", centerX + sx * 0.150f, centerZ + sz * 0.335f, materials.Bolt, 0.012f);
                }
            }

            AddCurvedWallBox("ER-15 dock chipped edge 1", group, centerX - 0.125f, -0.224f, centerZ + 0.285f, 0.052f, 0.005f, 0.008f, materials.Wear, -8f, 0.001f);
            AddCurvedWallBox("ER-15 dock chipped edge 2", group, centerX + 0.108f, -0.224f, centerZ + 0.075f, 0.060f, 0.005f, 0.008f, materials.Wear, 11f, 0.001f);
            AddCurvedWallBox("ER-15 dock chipped edge 3", group, centerX + 0.098f, -0.224f, centerZ - 0.235f, 0.046f, 0.005f, 0.007f, materials.Wear, -14f, 0.001f);
        }

        private static void AddApprovedCantabileWarningLight(Transform parent, ScreenMaterials materials)
        {
            var group = AddGroup(parent, CantabileWarningLightGroupName);
            const float centerX = 0.0f;
            const float centerY = -1.82f;
            const float ceilingZ = 2.82f;

            AddCeilingCylinder("ER-20 black ceiling backing disc", group, centerX, centerY, ceilingZ - 0.014f, 0.34f, 0.030f, materials.Rubber);
            AddCeilingCylinder("ER-20 black cylindrical warning light base", group, centerX, centerY, ceilingZ - 0.100f, 0.245f, 0.155f, materials.Rubber);
            AddCeilingCylinder("ER-20 pale separation ring below base", group, centerX, centerY, ceilingZ - 0.190f, 0.260f, 0.032f, materials.OverclockBeaconCollar);
            AddCeilingCylinder("ER-20 black lens socket lip", group, centerX, centerY, ceilingZ - 0.225f, 0.225f, 0.040f, materials.Rubber);
            AddCeilingCylinder("ER-20 red transparent cylindrical lens body", group, centerX, centerY, ceilingZ - 0.330f, 0.185f, 0.180f, materials.OverclockBeaconOn);
            AddCeilingSphere("ER-20 red rounded dome lens end", group, centerX, centerY, ceilingZ - 0.435f, 0.185f, new Vector3(1f, 0.62f, 1f), materials.OverclockBeaconOn);
            AddCeilingCylinder("ER-20 faint downward red warning glow volume", group, centerX, centerY, ceilingZ - 0.535f, 0.220f, 0.240f, materials.OverclockBeaconOn);

            AddCeilingCylinder("ER-20 black base rib upper", group, centerX, centerY, ceilingZ - 0.052f, 0.252f, 0.010f, materials.Frame);
            AddCeilingCylinder("ER-20 black base rib lower", group, centerX, centerY, ceilingZ - 0.145f, 0.252f, 0.010f, materials.Frame);

            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sy = -1; sy <= 1; sy += 2)
                {
                    AddCeilingCylinder(
                        "ER-20 visible ceiling mount bolt",
                        group,
                        centerX + sx * 0.255f,
                        centerY + sy * 0.190f,
                        ceilingZ + 0.006f,
                        0.018f,
                        0.016f,
                        materials.Bolt);
                }
            }
        }

        private static void AddApprovedOverclockLeverSwitch(Transform parent, ScreenMaterials materials)
        {
            var group = AddGroup(parent, OverclockControlGroupName);
            const float centerX = 2.24f;
            const float panelCenterZ = 2.03f;
            const float sampleScale = 0.44f;
            const float panelWidth = 2.36f * sampleScale;
            const float panelHeight = 2.52f * sampleScale;

            AddBox("ER-10 overclock control wall placement proxy", group, centerX, ScaleOverclockY(0.062f, sampleScale), panelCenterZ, 2.74f * sampleScale, 0.150f * sampleScale, 2.84f * sampleScale, materials.Wall, 0f, 0.010f * sampleScale);
            AddBox("ER-10 armored overclock control backplate", group, centerX, ScaleOverclockY(-0.065f, sampleScale), panelCenterZ, panelWidth, 0.120f * sampleScale, panelHeight, materials.OverclockBackplate, 0f, 0.022f * sampleScale);
            AddBox("ER-10 dark recessed service gasket", group, centerX, ScaleOverclockY(-0.145f, sampleScale), panelCenterZ, panelWidth - (0.17f * sampleScale), 0.050f * sampleScale, panelHeight - (0.20f * sampleScale), materials.Rubber, 0f, 0.014f * sampleScale);
            AddBox("ER-10 raised lever face plate", group, centerX, ScaleOverclockY(-0.206f, sampleScale), ScaleOverclockZ(panelCenterZ, 1.160f, sampleScale), panelWidth - (0.31f * sampleScale), 0.100f * sampleScale, panelHeight - (0.42f * sampleScale), materials.OverclockFace, 0f, 0.018f * sampleScale);

            AddBox("ER-10 upper lever hard stop block", group, ScaleOverclockX(centerX, 0.035f, sampleScale), ScaleOverclockY(-0.315f, sampleScale), ScaleOverclockZ(panelCenterZ, 1.950f, sampleScale), 0.560f * sampleScale, 0.040f * sampleScale, 0.090f * sampleScale, materials.OverclockStop, 0f, 0.008f * sampleScale);
            AddBox("ER-10 lower lever hard stop block", group, ScaleOverclockX(centerX, 0.035f, sampleScale), ScaleOverclockY(-0.315f, sampleScale), ScaleOverclockZ(panelCenterZ, 0.450f, sampleScale), 0.560f * sampleScale, 0.040f * sampleScale, 0.090f * sampleScale, materials.OverclockStop, 0f, 0.008f * sampleScale);

            AddBox("ER-10 protective left lever rail", group, ScaleOverclockX(centerX, -0.515f, sampleScale), ScaleOverclockY(-0.308f, sampleScale), ScaleOverclockZ(panelCenterZ, 1.200f, sampleScale), 0.056f * sampleScale, 0.080f * sampleScale, 1.770f * sampleScale, materials.OverclockGuard, 0f, 0.009f * sampleScale);
            AddBox("ER-10 protective right lever rail", group, ScaleOverclockX(centerX, 0.500f, sampleScale), ScaleOverclockY(-0.308f, sampleScale), ScaleOverclockZ(panelCenterZ, 1.200f, sampleScale), 0.056f * sampleScale, 0.080f * sampleScale, 1.770f * sampleScale, materials.OverclockGuard, 0f, 0.009f * sampleScale);
            AddBox("ER-10 protective upper lever rail", group, ScaleOverclockX(centerX, -0.005f, sampleScale), ScaleOverclockY(-0.307f, sampleScale), ScaleOverclockZ(panelCenterZ, 2.110f, sampleScale), 1.070f * sampleScale, 0.080f * sampleScale, 0.058f * sampleScale, materials.OverclockGuard, 0f, 0.009f * sampleScale);
            AddBox("ER-10 protective lower lever rail", group, ScaleOverclockX(centerX, -0.005f, sampleScale), ScaleOverclockY(-0.307f, sampleScale), ScaleOverclockZ(panelCenterZ, 0.285f, sampleScale), 1.070f * sampleScale, 0.080f * sampleScale, 0.058f * sampleScale, materials.OverclockGuard, 0f, 0.009f * sampleScale);

            AddOverclockLeverUp(group, materials, centerX, panelCenterZ, sampleScale);
            AddOverclockStatusLamp(group, materials, ScaleOverclockX(centerX, 0.920f, sampleScale), ScaleOverclockZ(panelCenterZ, 1.730f, sampleScale), sampleScale);

            AddBox("ER-10 lower cable trunk", group, centerX, ScaleOverclockY(-0.103f, sampleScale), ScaleOverclockZ(panelCenterZ, -0.150f, sampleScale), 0.150f * sampleScale, 0.125f * sampleScale, 0.780f * sampleScale, materials.Conduit, 0f, 0.013f * sampleScale);
            AddCylinder("ER-10 left cable gland", group, ScaleOverclockX(centerX, -0.680f, sampleScale), ScaleOverclockY(-0.160f, sampleScale), ScaleOverclockZ(panelCenterZ, -0.455f, sampleScale), 0.050f * sampleScale, 0.240f * sampleScale, materials.Conduit, CylinderAxis.SampleX);
            AddCylinder("ER-10 right cable gland", group, ScaleOverclockX(centerX, 0.680f, sampleScale), ScaleOverclockY(-0.160f, sampleScale), ScaleOverclockZ(panelCenterZ, -0.455f, sampleScale), 0.050f * sampleScale, 0.240f * sampleScale, materials.Conduit, CylinderAxis.SampleX);

            for (var sx = -1; sx <= 1; sx += 2)
            {
                for (var sz = -1; sz <= 1; sz += 2)
                {
                    AddBolt(group, "ER-10 overclock panel corner bolt", centerX + sx * 1.055f * sampleScale, panelCenterZ + sz * 1.120f * sampleScale, materials.Bolt, 0.032f * sampleScale);
                }
            }

            AddBox("ER-10 chipped exposed metal 1", group, ScaleOverclockX(centerX, -0.520f, sampleScale), ScaleOverclockY(-0.322f, sampleScale), ScaleOverclockZ(panelCenterZ, 2.170f, sampleScale), 0.135f * sampleScale, 0.010f * sampleScale, 0.020f * sampleScale, materials.Wear, -8f, 0.001f * sampleScale);
            AddBox("ER-10 chipped exposed metal 2", group, ScaleOverclockX(centerX, 0.310f, sampleScale), ScaleOverclockY(-0.322f, sampleScale), ScaleOverclockZ(panelCenterZ, 0.760f, sampleScale), 0.160f * sampleScale, 0.010f * sampleScale, 0.020f * sampleScale, materials.Wear, 10f, 0.001f * sampleScale);
            AddBox("ER-10 chipped exposed metal 3", group, ScaleOverclockX(centerX, 0.610f, sampleScale), ScaleOverclockY(-0.322f, sampleScale), ScaleOverclockZ(panelCenterZ, 0.345f, sampleScale), 0.115f * sampleScale, 0.010f * sampleScale, 0.018f * sampleScale, materials.Wear, -14f, 0.001f * sampleScale);
        }

        private static void AddOverclockLeverUp(Transform parent, ScreenMaterials materials, float centerX, float panelCenterZ, float sampleScale)
        {
            var pivotSample = new Vector3(
                ScaleOverclockX(centerX, -0.160f, sampleScale),
                ScaleOverclockY(-0.340f, sampleScale),
                ScaleOverclockZ(panelCenterZ, 1.200f, sampleScale));
            var endSample = new Vector3(
                ScaleOverclockX(centerX, 0.210f, sampleScale),
                ScaleOverclockY(-0.365f, sampleScale),
                ScaleOverclockZ(panelCenterZ, 1.910f, sampleScale));

            AddCylinder("ER-10 lever pivot axle cap", parent, pivotSample.x, pivotSample.y, pivotSample.z, 0.145f * sampleScale, 0.060f * sampleScale, materials.OverclockPivot, CylinderAxis.SampleY);
            AddCylinder("ER-10 lever pivot inner bolt", parent, pivotSample.x, ScaleOverclockY(-0.384f, sampleScale), pivotSample.z, 0.055f * sampleScale, 0.026f * sampleScale, materials.Bolt, CylinderAxis.SampleY);
            AddCylinderBetweenSample("ER-10 spring return lever black steel arm", parent, pivotSample, endSample, 0.045f * sampleScale, materials.OverclockLeverArm);
            AddSphere("ER-10 rubberized red lever hand grip", parent, endSample.x, endSample.y, endSample.z, 0.118f * sampleScale, materials.OverclockGrip);
            AddBox("ER-10 contacted upper lever hard stop wear", parent, ScaleOverclockX(centerX, 0.030f, sampleScale), ScaleOverclockY(-0.372f, sampleScale), ScaleOverclockZ(panelCenterZ, 1.940f, sampleScale), 0.520f * sampleScale, 0.035f * sampleScale, 0.070f * sampleScale, materials.OverclockStopContact, 0f, 0.008f * sampleScale);
        }

        private static void AddOverclockStatusLamp(Transform parent, ScreenMaterials materials, float centerX, float centerZ, float sampleScale)
        {
            AddCylinder("ER-10 red beacon recessed collar", parent, centerX, ScaleOverclockY(-0.286f, sampleScale), centerZ, 0.180f * sampleScale, 0.050f * sampleScale, materials.OverclockBeaconCollar, CylinderAxis.SampleY);
            AddSphere("ER-10 single red overclock active beacon", parent, centerX, ScaleOverclockY(-0.326f, sampleScale), centerZ, 0.125f * sampleScale, materials.OverclockBeaconOn);
        }

        private static float ScaleOverclockX(float centerX, float sourceOffset, float sampleScale)
        {
            return centerX + (sourceOffset * sampleScale);
        }

        private static float ScaleOverclockY(float sourceY, float sampleScale)
        {
            return sourceY * sampleScale;
        }

        private static float ScaleOverclockZ(float panelCenterZ, float sourceZ, float sampleScale)
        {
            return panelCenterZ + ((sourceZ - OverclockSamplePanelCenterZ) * sampleScale);
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

        private static void AddCurvedWallBolt(Transform parent, string name, float x, float z, Material material, float radius)
        {
            AddCurvedWallCylinder(name, parent, x, -0.236f, z, radius, 0.026f, material, CylinderAxis.SampleY);
            AddCurvedWallBox(name + " slot", parent, x, -0.252f, z, radius * 1.42f, 0.010f, radius * 0.22f, material, 0f, 0.001f);
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

        private static GameObject AddCurvedWallBox(
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
            obj.transform.localPosition = ToCurvedWallLocal(sampleX, sampleY, sampleZ);
            obj.transform.localRotation = CurvedWallSampleRotation(sampleX) * Quaternion.Euler(0f, 0f, sampleZRotationDegrees);
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

        private static GameObject AddCurvedWallCylinder(
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
            obj.transform.localPosition = ToCurvedWallLocal(sampleX, sampleY, sampleZ);
            obj.transform.localRotation = axis == CylinderAxis.SampleX
                ? CurvedWallSampleXRotation(sampleX)
                : CurvedWallSampleRotation(sampleX);
            obj.transform.localScale = new Vector3(radius * 2f, depth * 0.5f, radius * 2f);

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DisableCollider(obj);
            return obj;
        }

        private static GameObject AddCylinderBetweenSample(
            string name,
            Transform parent,
            Vector3 startSample,
            Vector3 endSample,
            float radius,
            Material material)
        {
            var start = ToLocal(startSample.x, startSample.y, startSample.z);
            var end = ToLocal(endSample.x, endSample.y, endSample.z);
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

        private static GameObject AddSphere(
            string name,
            Transform parent,
            float sampleX,
            float sampleY,
            float sampleZ,
            float radius,
            Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = ToLocal(sampleX, sampleY, sampleZ);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one * radius * 2f;

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
            mesh.uv = CreateFlippedDisplayUvs(uvMin, uvMax);
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

        private static bool SetDisplayMeshUvs(GameObject display, Vector2 uvMin, Vector2 uvMax)
        {
            var filter = display.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                return false;
            }

            filter.sharedMesh.uv = CreateFlippedDisplayUvs(uvMin, uvMax);
            EditorUtility.SetDirty(filter.sharedMesh);
            EditorUtility.SetDirty(display);
            return true;
        }

        private static Vector2[] CreateFlippedDisplayUvs(Vector2 uvMin, Vector2 uvMax)
        {
            return new[]
            {
                new Vector2(uvMax.x, uvMin.y),
                new Vector2(uvMin.x, uvMin.y),
                new Vector2(uvMin.x, uvMax.y),
                new Vector2(uvMax.x, uvMax.y)
            };
        }

        private static Vector3 ToLocal(float sampleX, float sampleY, float sampleZ)
        {
            return (RadialOutward * (WallAnchorRadius + sampleY)) +
                   (Tangent * sampleX) +
                   (Vector3.up * sampleZ);
        }

        private static Vector3 ToCurvedWallLocal(float sampleX, float sampleY, float sampleZ)
        {
            return (CurvedWallOutward(sampleX) * (WallAnchorRadius - FlashlightChargingDockInternalWallInset + sampleY)) +
                   (Vector3.up * sampleZ);
        }

        private static Quaternion CurvedWallSampleRotation(float sampleX)
        {
            return Quaternion.LookRotation(Vector3.up, CurvedWallOutward(sampleX));
        }

        private static Quaternion CurvedWallSampleXRotation(float sampleX)
        {
            return Quaternion.LookRotation(Vector3.up, CurvedWallTangent(sampleX));
        }

        private static Vector3 CurvedWallOutward(float sampleX)
        {
            var degrees = -sampleX / WallAnchorRadius * Mathf.Rad2Deg;
            return Quaternion.AngleAxis(degrees, Vector3.up) * RadialOutward;
        }

        private static Vector3 CurvedWallTangent(float sampleX)
        {
            var degrees = -sampleX / WallAnchorRadius * Mathf.Rad2Deg;
            return Quaternion.AngleAxis(degrees, Vector3.up) * Tangent;
        }

        private static GameObject AddCeilingCylinder(
            string name,
            Transform parent,
            float sampleX,
            float sampleY,
            float sampleZ,
            float radius,
            float depth,
            Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = ToLocal(sampleX, sampleY, sampleZ);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector3(radius * 2f, depth * 0.5f, radius * 2f);

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DisableCollider(obj);
            return obj;
        }

        private static GameObject AddCeilingSphere(
            string name,
            Transform parent,
            float sampleX,
            float sampleY,
            float sampleZ,
            float radius,
            Vector3 axisScale,
            Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = ToLocal(sampleX, sampleY, sampleZ);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector3(
                radius * 2f * axisScale.x,
                radius * 2f * axisScale.y,
                radius * 2f * axisScale.z);

            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            DisableCollider(obj);
            return obj;
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
                EnsureMaterial("M_Er09_InactiveOverclockConnectorCover", new Color(0.11f, 0.12f, 0.11f, 1f), 0.30f, 0.12f, false, false),
                EnsureMaterial("M_Er10_DarkArmoredOverclockBackplate", new Color(0.13f, 0.15f, 0.14f, 1f), 0.34f, 0.14f, false, false),
                EnsureMaterial("M_Er10_WornRaisedLeverFacePlate", new Color(0.23f, 0.25f, 0.22f, 1f), 0.34f, 0.12f, false, false),
                EnsureMaterial("M_Er10_LeverProtectiveGuardRail", new Color(0.07f, 0.08f, 0.075f, 1f), 0.34f, 0.12f, false, false),
                EnsureMaterial("M_Er10_LeverHardStopBlock", new Color(0.34f, 0.33f, 0.25f, 1f), 0.34f, 0.18f, false, false),
                EnsureMaterial("M_Er10_FreshWearOnContactedHardStop", new Color(0.72f, 0.63f, 0.34f, 1f), 0.24f, 0.50f, false, false),
                EnsureMaterial("M_Er10_LeverPivotAxleMetal", new Color(0.28f, 0.28f, 0.24f, 1f), 0.34f, 0.18f, false, false),
                EnsureMaterial("M_Er10_SpringReturnLeverBlackSteelArm", new Color(0.035f, 0.039f, 0.037f, 1f), 0.36f, 0.12f, false, false),
                EnsureMaterial("M_Er10_RubberizedWornRedLeverGrip", new Color(0.72f, 0.050f, 0.032f, 1f), 0.0f, 0.38f, false, false),
                EnsureMaterial("M_Er10_RedOverclockActiveBeacon", new Color(1.0f, 0.030f, 0.012f, 1f), 0.0f, 0.78f, false, true),
                EnsureMaterial("M_Er10_BlackBeaconCollarRing", new Color(0.030f, 0.033f, 0.031f, 1f), 0.30f, 0.16f, false, false));
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

                var relativePath = GetRelativePath(root, transform);
                if (!keptPaths.Contains(relativePath) && !ShouldPreserveGeneratedObjectOutsideUserSnapshot(relativePath))
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

        private static bool ShouldPreserveGeneratedObjectOutsideUserSnapshot(string relativePath)
        {
            if (relativePath.StartsWith(OverclockControlPreservePathPrefix, StringComparison.Ordinal))
            {
                return !HasUserEditedTransformUnderPrefix(OverclockControlPreservePathPrefix);
            }

            if (relativePath.StartsWith(FlashlightChargingDockPreservePathPrefix, StringComparison.Ordinal))
            {
                return !HasUserEditedTransformUnderPrefix(FlashlightChargingDockPreservePathPrefix);
            }

            if (relativePath.StartsWith(CantabileWarningLightPreservePathPrefix, StringComparison.Ordinal))
            {
                return !HasUserEditedTransformUnderPrefix(CantabileWarningLightPreservePathPrefix);
            }

            return false;
        }

        private static bool HasUserEditedTransformUnderPrefix(string prefix)
        {
            for (var i = 0; i < UserEditedTransformOverrides.Length; i++)
            {
                if (UserEditedTransformOverrides[i].Path.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
                Material reserve,
                Material overclockBackplate,
                Material overclockFace,
                Material overclockGuard,
                Material overclockStop,
                Material overclockStopContact,
                Material overclockPivot,
                Material overclockLeverArm,
                Material overclockGrip,
                Material overclockBeaconOn,
                Material overclockBeaconCollar)
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
                OverclockBackplate = overclockBackplate;
                OverclockFace = overclockFace;
                OverclockGuard = overclockGuard;
                OverclockStop = overclockStop;
                OverclockStopContact = overclockStopContact;
                OverclockPivot = overclockPivot;
                OverclockLeverArm = overclockLeverArm;
                OverclockGrip = overclockGrip;
                OverclockBeaconOn = overclockBeaconOn;
                OverclockBeaconCollar = overclockBeaconCollar;
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
            public Material OverclockBackplate { get; }
            public Material OverclockFace { get; }
            public Material OverclockGuard { get; }
            public Material OverclockStop { get; }
            public Material OverclockStopContact { get; }
            public Material OverclockPivot { get; }
            public Material OverclockLeverArm { get; }
            public Material OverclockGrip { get; }
            public Material OverclockBeaconOn { get; }
            public Material OverclockBeaconCollar { get; }
        }
    }
}
