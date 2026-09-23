using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PegasusLayoutSetupTools
    {
        internal const string SourceScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string TargetScenePath = "Assets/_Project/Scenes/Pegasus.unity";
        private const string CorridorRootName = "Approved Ship Corridor Segments";
        private const string CorridorCeilingRootName = "ShipSpaceCeiling_AllCorridors";
        private const string LogDirectory = "Logs/PegasusLayout";
        private const string ValidationDirectory = "docs/validation/PegasusLayout";
        private const string InteriorRestoreLogDirectory = "Logs/PegasusInteriorRestore";
        private const string InteriorValidationDirectory = "docs/validation/PegasusInteriorRestore";
        private const string InteriorSourceReportName = "SourceRoots.txt";
        private const string InteriorInspectionReportName = "Inspection.txt";
        private const string InteriorReviewCaptureName = "Review.png";
        private const string InteriorFinalCaptureName = "Final.png";
        private const string HorizontalCorridorLogDirectory = "Logs/PegasusHorizontalCorridorLength";
        private const string HorizontalCorridorValidationDirectory =
            "docs/validation/PegasusHorizontalCorridorLength";
        private const string HorizontalCorridorInspectionName = "Inspection.txt";
        private const string HorizontalCorridorReviewName = "Review.png";
        private const string HorizontalCorridorFinalName = "Final.png";
        private const string HorizontalCorridorProtectedSignatureName = "ProtectedState.txt";
        private const string TriRoomConnectionLogDirectory =
            "Logs/PegasusTriRoomCorridorConnection";
        private const string TriRoomConnectionValidationDirectory =
            "docs/validation/PegasusTriRoomCorridorConnection";
        private const string TriRoomConnectionSourceReportName = "Sources.txt";
        private const string TriRoomConnectionInspectionName = "Inspection.txt";
        private const string TriRoomConnectionReviewName = "Review.png";
        private const string TriRoomConnectionFinalName = "Final.png";
        private const string TriRoomConnectionProtectedSignatureName = "ProtectedState.txt";
        private const string TriRoomUniformGapLogDirectory =
            "Logs/PegasusTriRoomUniformGap";
        private const string TriRoomUniformGapValidationDirectory =
            "docs/validation/PegasusTriRoomUniformGap";
        private const string TriRoomUniformGapSourceReportName = "Sources.txt";
        private const string TriRoomUniformGapInspectionName = "Inspection.txt";
        private const string TriRoomUniformGapReviewName = "Review.png";
        private const string TriRoomUniformGapFinalName = "Final.png";
        private const string TriRoomUniformGapProtectedSignatureName = "ProtectedState.txt";
        private const string EngineControlAlignmentValidationDirectory =
            "docs/validation/PegasusEngineControlAlignment";
        private const string EngineControlAlignmentReviewName = "Review.png";
        private const string EngineControlAlignmentInspectionName = "Inspection.txt";
        private const string EngineControlAlignmentFinalName = "Final.png";
        private const string CockpitMidpointAlignmentValidationDirectory =
            "docs/validation/PegasusCockpitMidpointAlignment";
        private const string CockpitMidpointAlignmentChangeName = "Change.txt";
        private const string CockpitMidpointAlignmentReviewName = "Review.png";
        private const string CockpitMidpointAlignmentInspectionName = "Inspection.txt";
        private const string CockpitMidpointAlignmentFinalName = "Final.png";
        private const string TriRoomEntranceConnectionValidationDirectory =
            "docs/validation/PegasusTriRoomEntranceConnection";
        private const string TriRoomEntranceConnectionSourceName = "Sources.txt";
        private const string TriRoomEntranceConnectionChangeName = "Change.txt";
        private const string TriRoomEntranceConnectionProtectedStateName = "ProtectedState.txt";
        private const string TriRoomEntranceConnectionReviewName = "Review.png";
        private const string TriRoomEntranceConnectionInspectionName = "Inspection.txt";
        private const string TriRoomEntranceConnectionFinalName = "Final.png";
        private const string CockpitOrientationValidationDirectory =
            "docs/validation/PegasusCockpitOrientation";
        private const string CockpitOrientationSourceName = "Sources.txt";
        private const string CockpitOrientationChangeName = "Change.txt";
        private const string CockpitOrientationProtectedStateName = "ProtectedState.txt";
        private const string CockpitOrientationReviewName = "Review.png";
        private const string CockpitOrientationInspectionName = "Inspection.txt";
        private const string CockpitOrientationFinalName = "Final.png";
        private const string CockpitCorridorConnectionValidationDirectory =
            "docs/validation/PegasusCockpitCorridorConnection";
        private const string CockpitCorridorConnectionSourceName = "Sources.txt";
        private const string CockpitCorridorConnectionChangeName = "Change.txt";
        private const string CockpitCorridorConnectionProtectedStateName = "ProtectedState.txt";
        private const string CockpitCorridorConnectionReviewName = "Review.png";
        private const string CockpitCorridorConnectionInspectionName = "Inspection.txt";
        private const string CockpitCorridorConnectionFinalName = "Final.png";
        private const string RoomProportionExpansionValidationDirectory =
            "docs/validation/PegasusRoomProportionExpansion";
        private const string RoomProportionExpansionSourceName = "Sources.txt";
        private const string RoomProportionExpansionChangeName = "Change.txt";
        private const string RoomProportionExpansionProtectedStateName = "ProtectedState.txt";
        private const string RoomProportionExpansionCorridorStateName = "CorridorState.txt";
        private const string RoomProportionExpansionReviewName = "Review.png";
        private const string RoomProportionExpansionInspectionName = "Inspection.txt";
        private const string RoomProportionExpansionFinalName = "Final.png";
        private const string RoomInteriorProportionValidationDirectory =
            "docs/validation/PegasusRoomInteriorProportion";
        private const string RoomInteriorProportionSourceName = "Sources.txt";
        private const string RoomInteriorProportionChangeName = "Change.txt";
        private const string RoomInteriorProportionProtectedStateName = "ProtectedState.txt";
        private const string RoomInteriorProportionReviewName = "Review.png";
        private const string RoomInteriorProportionInspectionName = "Inspection.txt";
        private const string RoomInteriorProportionFinalName = "Final.png";
        private const string TriRoomConnectorSampleAssetDirectory =
            "Assets/_Project/ArtSamples/PegasusTriRoomConnectors";
        private const string TriRoomConnectorSampleScenePath =
            TriRoomConnectorSampleAssetDirectory + "/PegasusTriRoomConnectors.unity";
        private const string TriRoomConnectorSampleMeshDirectory =
            TriRoomConnectorSampleAssetDirectory + "/Meshes";
        private const string TriRoomConnectorSampleValidationDirectory =
            "docs/validation/PegasusTriRoomConnectorSample";
        private const string TriRoomConnectorSampleArtDirectory =
            "artSample/PegasusTriRoomConnectors";
        private const string TriRoomConnectorSampleSourceName = "Sources.txt";
        private const string TriRoomConnectorSampleProtectedStateName = "ProtectedState.txt";
        private const string TriRoomConnectorSampleInspectionName = "Inspection.txt";
        private const string TriRoomConnectorSampleDirectReviewName = "DirectReview.txt";
        private const string TriRoomConnectorSampleReviewName = "Review.png";
        private const string TriRoomConnectorSampleFinalName = "Final.png";
        private const string TriRoomConnectorSamplePlayModeDirectory =
            TriRoomConnectorSampleValidationDirectory + "/PlayModeSweep";
        private const string TriRoomConnectorSampleRootName =
            "Pegasus Tri-Room Intermediate Connector Art Sample";
        private const float TriRoomConnectorSampleSeamTolerance = 0.005f;
        private const float RoomProportionExpansionMinimumFactor = 1.05f;
        private const float EntranceWidthTolerance = 0.015f;
        private const float InteriorProportionTolerance = 0.0025f;
        private const string PlayerSettingsPath =
            "Assets/_Project/Settings/Player/DefaultFirstPersonPlayerSettings.asset";
        private const float HorizontalCorridorTravelSeconds = 5f;
        private const float EngineControlCorridorTravelSeconds = 6f;
        private const string SourceReportName = "SourceInspection.txt";
        private const string InspectionReportName = "Inspection.txt";
        private const string ReviewCaptureName = "LayoutReview.png";
        private const string FinalCaptureName = "Final.png";
        private const float CommonCeilingTopY = 6f;
        private const float CargoCeilingDrop = 0.5f;
        private const float DetachedEndClearance = 2f;
        private const float TriRoomUniformConnectorGap = 1f;
        private const float TriRoomUniformConnectorGapTolerance = 0.01f;
        private const float TriRoomFlushConnectionTolerance = 0.015f;
        private const float TriRoomEntranceEdgeSafety = 0.025f;
        private const float H02ControlRoomBufferGap = 3f;
        private const float CockpitRequiredYawDegrees = 180f;
        // With the restored 180-degree cockpit orientation, the fixed H01
        // entrance normals differ by about 86.35 degrees. A straight corridor
        // therefore has a translation-only minimax of about 43.18 degrees.
        private const float CockpitH01MaximumMeetingAngle = 43.3f;
        private const float CockpitH02MaximumMeetingAngle = 28f;
        private const float CockpitBalancedMeetingAngleTolerance = 0.1f;
        private const float CockpitCorridorH01LengthWeight = 4f;
        private const float CockpitCorridorH02LengthWeight = 6f;
        private const float CockpitCorridorCombinedLengthReference = 96.94768f;
        private const float CockpitCorridorLengthTolerance = 0.015f;
        private const float CockpitH01ReferenceMeetingAngle = 43.1817f;
        private const float CockpitH02ReferenceMeetingAngle = 27.8749f;
        private const float CockpitCorridorMeetingAngleDeviation = 3f;
        private const float ControlRoomMinimumRightShift = 1.5f;
        private const float ControlRoomMaximumRightShift = 20f;
        private const float ControlRoomRightShiftStep = 0.5f;
        private const float ControlRoomMaximumH02ApproachAngle = 50f;
        private const float CockpitMinimumUpwardShift = 3f;
        private const float CockpitMaximumUpwardShift = 12f;
        private const float CockpitUpwardShiftStep = 0.5f;
        private const float CockpitMaximumH02AngleFromZAxis = 30f;
        // A straight H01 cannot reach 25 degrees at both fixed, unrotated entrances.
        // 28 degrees admits the 27.43-degree minimax placement and leaves the
        // smallest possible correction for the future buffer connector.
        private const float MaximumRoomEntranceMeetingAngle = 28f;
        private const float MaximumBalancedMeetingAngleDifference = 1f;
        private const float PositionTolerance = 0.001f;
        private const float RotationTolerance = 0.001f;

        private static readonly RoomDefinition[] RoomDefinitions =
        {
            new RoomDefinition(
                "EngineRoom",
                "동력실",
                "Approved Engine Room 01 Shell",
                new Vector2(-1.35f, 0.45f)),
            new RoomDefinition(
                "Cockpit",
                "조종실",
                "Approved Cockpit 01 Structure",
                new Vector2(0.15f, 1.35f),
                false,
                CockpitRequiredYawDegrees),
            new RoomDefinition(
                "ControlRoom",
                "통제실",
                "Approved Control Room 01 Shell",
                new Vector2(1.35f, 0.45f)),
            new RoomDefinition(
                "Armory",
                "무기실",
                "Approved Armory 01 Shell",
                new Vector2(1.35f, -1.05f)),
            new RoomDefinition(
                "SupplyRoom",
                "비품실",
                "Approved Supply Room 01 Shell",
                new Vector2(-1.15f, -1.05f)),
            new RoomDefinition(
                "CargoHold",
                "창고",
                "Approved Cargo Hold 01 Shell",
                new Vector2(0f, -0.05f),
                true,
                180f)
        };

        private static readonly InteriorRootDefinition[] InteriorRootDefinitions =
        {
            new InteriorRootDefinition("EngineRoom", "Approved Engine Room 09 Health Screen"),
            new InteriorRootDefinition("Cockpit", "Approved Cockpit 01 Window"),
            new InteriorRootDefinition("Cockpit", "Approved Cockpit 02 Console"),
            new InteriorRootDefinition("Cockpit", "Approved Cockpit 04 Warning"),
            new InteriorRootDefinition("Cockpit", "Approved Cockpit 09 Damage Visual Switcher"),
            new InteriorRootDefinition("Cockpit", "Approved Cockpit 09 Destroyed Console"),
            new InteriorRootDefinition("Cockpit", "Approved Cockpit 11 Direction"),
            new InteriorRootDefinition("Cockpit", "Approved Cockpit 12 Inspection Lighting"),
            new InteriorRootDefinition("ControlRoom", "Approved Control Room 07 Aux Screen"),
            new InteriorRootDefinition("ControlRoom", "Approved Control Room 08 Vertical Aux Screens"),
            new InteriorRootDefinition("ControlRoom", "Approved Control Room 17 Direction Labels")
        };

        private static readonly InteriorRootDefinition[] InteriorDependencyRootDefinitions =
        {
            new InteriorRootDefinition("Cockpit", "Phase 6 Room Interactions")
        };

        private static readonly CorridorDefinition[] CorridorDefinitions =
        {
            new CorridorDefinition("SC-H01", "EngineRoom", "Cockpit", false),
            new CorridorDefinition("SC-H02", "Cockpit", "ControlRoom", false),
            new CorridorDefinition("SC-H03", "ControlRoom", "Armory", false),
            new CorridorDefinition("SC-H04", "Armory", "SupplyRoom", false),
            new CorridorDefinition("SC-H05", "EngineRoom", "ControlRoom", false),
            new CorridorDefinition("SC-S01", "EngineRoom", "CargoHold", true),
            new CorridorDefinition("SC-S02", "Cockpit", "CargoHold", true),
            new CorridorDefinition("SC-S03", "ControlRoom", "CargoHold", true),
            new CorridorDefinition("SC-S04", "Armory", "CargoHold", true),
            new CorridorDefinition("SC-S05", "SupplyRoom", "CargoHold", true)
        };

        private static readonly ConnectorEndpointDefinition[] TriRoomConnectorEndpoints =
        {
            new ConnectorEndpointDefinition(
                "EngineRoom_H01",
                "SC-H01",
                "EngineRoom",
                "Cockpit",
                true),
            new ConnectorEndpointDefinition(
                "Cockpit_H01",
                "SC-H01",
                "Cockpit",
                "EngineRoom",
                false),
            new ConnectorEndpointDefinition(
                "Cockpit_H02",
                "SC-H02",
                "Cockpit",
                "ControlRoom",
                true),
            new ConnectorEndpointDefinition(
                "ControlRoom_H02",
                "SC-H02",
                "ControlRoom",
                "Cockpit",
                false),
            new ConnectorEndpointDefinition(
                "EngineRoom_H05",
                "SC-H05",
                "EngineRoom",
                "ControlRoom",
                true),
            new ConnectorEndpointDefinition(
                "ControlRoom_H05",
                "SC-H05",
                "ControlRoom",
                "EngineRoom",
                false)
        };

        private static readonly EntranceWidthPairDefinition[] EntranceWidthPairDefinitions =
        {
            new EntranceWidthPairDefinition(
                "EngineRoom",
                "Engine-Cockpit",
                "ER-01 1시 Cockpit corridor side wall 1",
                "ER-01 1시 Cockpit corridor side wall 2"),
            new EntranceWidthPairDefinition(
                "EngineRoom",
                "Engine-Control",
                "ER-01 3시 Control corridor side wall 1",
                "ER-01 3시 Control corridor side wall 2",
                true),
            new EntranceWidthPairDefinition(
                "EngineRoom",
                "Engine-Cargo",
                "ER-01 5시 Cargo ramp side wall 1",
                "ER-01 5시 Cargo ramp side wall 2"),
            new EntranceWidthPairDefinition(
                "ControlRoom",
                "Control-Cargo",
                "CR-01 cargo bay south attached corridor side wall +0.92",
                "CR-01 cargo bay south attached corridor side wall -0.92"),
            new EntranceWidthPairDefinition(
                "ControlRoom",
                "Control-Cockpit",
                "CR-01 cockpit 40 degree outside only corridor side wall +0.96",
                "CR-01 cockpit 40 degree outside only corridor side wall -0.96"),
            new EntranceWidthPairDefinition(
                "ControlRoom",
                "Control-Engine",
                "CR-01 engine room left separated corridor side wall +0.99",
                "CR-01 engine room left separated corridor side wall -0.99",
                true),
            new EntranceWidthPairDefinition(
                "ControlRoom",
                "Control-Armory",
                "CR-01 weapon room south attached corridor side wall +0.92",
                "CR-01 weapon room south attached corridor side wall -0.92"),
            new EntranceWidthPairDefinition(
                "Armory",
                "Armory-Supply",
                "AR-07 supply room east corridor side wall +0.92",
                "AR-07 supply room east corridor side wall -0.92"),
            new EntranceWidthPairDefinition(
                "Armory",
                "Armory-Control",
                "AR-08 control room south corridor side wall +0.92",
                "AR-08 control room south corridor side wall -0.92",
                true),
            new EntranceWidthPairDefinition(
                "SupplyRoom",
                "Supply-Armory",
                "SR-09 armory direction corridor lower side wall",
                "SR-09 armory direction corridor upper side wall",
                true),
            new EntranceWidthPairDefinition(
                "SupplyRoom",
                "Supply-Cargo",
                "SR-10 cargo hold direction corridor lower side wall",
                "SR-10 cargo hold direction corridor upper side wall"),
            new EntranceWidthPairDefinition(
                "CargoHold",
                "Cargo-Cockpit",
                "CH-05 cockpit corridor at 12 oclock left side wall",
                "CH-05 cockpit corridor at 12 oclock right side wall",
                true),
            new EntranceWidthPairDefinition(
                "CargoHold",
                "Cargo-Engine",
                "CH-06 engine corridor at 9 oclock lower side wall",
                "CH-06 engine corridor at 9 oclock upper side wall"),
            new EntranceWidthPairDefinition(
                "CargoHold",
                "Cargo-Control",
                "CH-07 control corridor at 3 oclock lower side wall",
                "CH-07 control corridor at 3 oclock upper side wall"),
            new EntranceWidthPairDefinition(
                "CargoHold",
                "Cargo-Armory",
                "CH-08 armory corridor at right aft edge left side wall",
                "CH-08 armory corridor at right aft edge right side wall"),
            new EntranceWidthPairDefinition(
                "CargoHold",
                "Cargo-Supply",
                "CH-09 supply corridor at left aft edge left side wall",
                "CH-09 supply corridor at left aft edge right side wall")
        };

        internal static void InspectSources()
        {
            var sourceScene = RequireSourceScene();
            if (sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Pegasus source inspection will not save or discard them.");
            }

            var rooms = RequireRooms(sourceScene);
            var corridorRoot = RequireSceneObject(sourceScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName);
            if (ceilingRoot == null)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp is missing the approved all-corridor ceiling root.");
            }

            var report = new StringBuilder(8 * 1024);
            report.AppendLine("Pegasus layout source inspection");
            report.AppendLine("SourceScene=" + SourceScenePath);
            report.AppendLine("SourceSceneDirty=False");
            report.AppendLine("RoomRoots=" + rooms.Count);
            report.AppendLine("CorridorModules=" + modules.Count);
            report.AppendLine("CorridorCeilingPieces=" + ceilingRoot.childCount);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id];
                report.AppendLine(
                    definition.DisplayName +
                    "|Root=" + definition.RootName +
                    "|Transforms=" + room.GetComponentsInChildren<Transform>(true).Length +
                    "|CeilingTop=" + Float(GetMainCeilingTop(room)) +
                    "|Bounds=" + BoundsText(CalculateVisibleBounds(room)));
            }

            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                report.AppendLine(
                    definition.ModuleId +
                    "|Type=" + (definition.IsSloped ? "Sloped" : "Horizontal") +
                    "|Length=" + Float(GetCorridorLength(module.gameObject)) +
                    "|From=" + definition.FromRoomId +
                    "|To=" + definition.ToRoomId);
            }

            WriteProjectText(LogDirectory, SourceReportName, report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus layout sources inspected without modifying CargoRunMvp. " +
                "Rooms=6; Corridors=10; CorridorCeilings=" + ceilingRoot.childCount +
                "; UnityConsoleErrors=0");
        }

        internal static void InspectRoomProportionExpansionSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Room proportion source inspection will not discard them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var report = new StringBuilder(512 * 1024);
            report.AppendLine("Pegasus six-room proportion expansion source inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetRooms=EngineRoom,Cockpit,ControlRoom,Armory,SupplyRoom,CargoHold");
            report.AppendLine("WidthStandard=Corridor full outside width including both outer walls");
            report.AppendLine("RoomRootScalingAllowed=False");
            report.AppendLine("CorridorModificationAllowed=False");
            report.AppendLine();

            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
                var worldWidth = module.TransformVector(Vector3.forward * localBounds.size.z).magnitude;
                report.AppendLine(
                    "CORRIDOR|" + definition.ModuleId +
                    "|From=" + definition.FromRoomId +
                    "|To=" + definition.ToRoomId +
                    "|Type=" + (definition.IsSloped ? "Sloped" : "Horizontal") +
                    "|FullOutsideWidth=" + Float(worldWidth) +
                    "|LocalVisibleBounds=" + BoundsText(localBounds) +
                    "|Position=" + Vector(module.position) +
                    "|Rotation=" + QuaternionText(module.rotation) +
                    "|LocalScale=" + Vector(module.localScale));
            }

            report.AppendLine();
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id];
                report.AppendLine("ROOM_BEGIN|" + definition.Id + "|Root=" + room.name);
                report.AppendLine(
                    "ROOT|Position=" + Vector(room.transform.position) +
                    "|Rotation=" + QuaternionText(room.transform.rotation) +
                    "|LocalScale=" + Vector(room.transform.localScale) +
                    "|VisibleBounds=" + BoundsText(CalculateVisibleBounds(room)) +
                    "|MainCeilingTop=" + Float(GetMainCeilingTop(room)) +
                    "|TransformCount=" + room.GetComponentsInChildren<Transform>(true).Length +
                    "|RendererCount=" + room.GetComponentsInChildren<Renderer>(true).Length);

                for (var childIndex = 0; childIndex < room.transform.childCount; childIndex++)
                {
                    var child = room.transform.GetChild(childIndex);
                    report.Append("TOP_LEVEL|Path=")
                        .Append(GetHierarchyPath(room.transform, child))
                        .Append("|Active=").Append(child.gameObject.activeSelf)
                        .Append("|LocalPosition=").Append(Vector(child.localPosition))
                        .Append("|LocalRotation=").Append(QuaternionText(child.localRotation))
                        .Append("|LocalScale=").Append(Vector(child.localScale));
                    if (TryCalculateVisibleBoundsInLocalSpace(child.gameObject, room.transform, out var childBounds))
                    {
                        report.Append("|RoomLocalVisibleBounds=").Append(BoundsText(childBounds));
                    }

                    report.AppendLine();
                }

                var transforms = room.GetComponentsInChildren<Transform>(true);
                Array.Sort(
                    transforms,
                    (left, right) => string.CompareOrdinal(
                        GetHierarchyPath(room.transform, left),
                        GetHierarchyPath(room.transform, right)));
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    report.Append("TRANSFORM|Path=")
                        .Append(GetHierarchyPath(room.transform, target))
                        .Append("|Active=").Append(target.gameObject.activeSelf)
                        .Append("|LocalPosition=").Append(Vector(target.localPosition))
                        .Append("|LocalRotation=").Append(QuaternionText(target.localRotation))
                        .Append("|LocalScale=").Append(Vector(target.localScale))
                        .Append("|").Append(BuildComponentSignature(target.gameObject));
                    var renderer = target.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var rendererBounds = CalculateRendererBoundsInLocalSpace(
                            renderer,
                            room.transform);
                        report.Append("|RendererEnabled=").Append(renderer.enabled)
                            .Append("|RoomLocalRendererBounds=").Append(BoundsText(rendererBounds));
                    }

                    report.AppendLine();
                }

                report.AppendLine("ROOM_END|" + definition.Id);
                report.AppendLine();
            }

            WriteProjectText(
                RoomProportionExpansionValidationDirectory,
                RoomProportionExpansionSourceName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus room proportion expansion sources inspected read-only. " +
                "Rooms=6; Corridors=10; SceneChanged=False; UnityConsoleErrors=0");
        }

        internal static void ApplyRoomProportionExpansion()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Room proportion expansion will not discard them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var targetWidth = GetCommonCorridorOutsideWidth(modules);
            var corridorSignature = BuildLayoutPlacementSignature(rooms, corridorRoot);
            var structuralTargets = CollectRoomExpansionStructuralTargets(rooms);
            var protectedSignature = BuildRoomProportionProtectedSignature(
                rooms,
                structuralTargets);
            var beforeCeilingTops = new Dictionary<string, float>(StringComparer.Ordinal);
            var beforeRootScales = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            var expansionFactors = new Dictionary<string, float>(StringComparer.Ordinal);
            var placements = new List<RoomInteriorPlacement>();
            var report = new StringBuilder(64 * 1024);
            report.AppendLine("Pegasus six-room proportion expansion change");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetCorridorFullOutsideWidth=" + Float(targetWidth));
            report.AppendLine("RoomRootScaling=False");
            report.AppendLine("CorridorChanges=False");
            report.AppendLine();

            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id];
                beforeCeilingTops.Add(definition.Id, GetMainCeilingTop(room));
                beforeRootScales.Add(definition.Id, room.transform.localScale);
                var sourceEntranceWidth = GetRoomExpansionReferenceWidth(
                    targetScene,
                    definition.Id);
                var expansionFactor = Mathf.Max(
                    RoomProportionExpansionMinimumFactor,
                    targetWidth / sourceEntranceWidth);
                expansionFactors.Add(definition.Id, expansionFactor);
                CaptureRoomInteriorPlacements(
                    room.transform,
                    structuralTargets,
                    placements);
                report.AppendLine(
                    definition.Id +
                    "|ReferenceEntranceWidth=" + Float(sourceEntranceWidth) +
                    "|ExpansionFactor=" + Float(expansionFactor) +
                    "|CeilingTopBefore=" + Float(beforeCeilingTops[definition.Id]) +
                    "|RootScaleBefore=" + Vector(beforeRootScales[definition.Id]));
            }

            try
            {
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var definition = RoomDefinitions[roomIndex];
                    var room = rooms[definition.Id];
                    var targets = structuralTargets[definition.Id];
                    for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                    {
                        ExpandStructuralTransformHorizontally(
                            room.transform,
                            targets[targetIndex],
                            expansionFactors[definition.Id]);
                    }
                }

                for (var placementIndex = 0; placementIndex < placements.Count; placementIndex++)
                {
                    RepositionRoomInterior(
                        placements[placementIndex],
                        expansionFactors[placements[placementIndex].RoomId]);
                }

                for (var pairIndex = 0;
                     pairIndex < EntranceWidthPairDefinitions.Length;
                     pairIndex++)
                {
                    NormalizeEntranceOutsideWidth(
                        targetScene,
                        EntranceWidthPairDefinitions[pairIndex],
                        targetWidth);
                }

                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var definition = RoomDefinitions[roomIndex];
                    var room = rooms[definition.Id];
                    if (!Approximately(room.transform.localScale, beforeRootScales[definition.Id]))
                    {
                        throw new InvalidOperationException(
                            definition.Id + " room root scale changed during proportion expansion.");
                    }

                    var ceilingTop = GetMainCeilingTop(room);
                    if (Mathf.Abs(ceilingTop - beforeCeilingTops[definition.Id]) > PositionTolerance)
                    {
                        throw new InvalidOperationException(
                            definition.Id + " ceiling height changed during horizontal expansion. " +
                            "Before=" + Float(beforeCeilingTops[definition.Id]) +
                            "; After=" + Float(ceilingTop));
                    }

                    report.AppendLine(
                        definition.Id +
                        "|CeilingTopAfter=" + Float(ceilingTop) +
                        "|RootScaleAfter=" + Vector(room.transform.localScale) +
                        "|VisibleBoundsAfter=" + BoundsText(CalculateVisibleBounds(room)));
                }

                var currentCorridorSignature = BuildLayoutPlacementSignature(rooms, corridorRoot);
                if (!string.Equals(
                        corridorSignature,
                        currentCorridorSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Pegasus corridor or room-root placement changed during room proportion expansion.");
                }

                var currentProtectedSignature = BuildRoomProportionProtectedSignature(
                    rooms,
                    structuralTargets);
                if (!string.Equals(
                        protectedSignature,
                        currentProtectedSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A protected room property changed during proportion expansion. " +
                        "Only horizontal structure scale/position and interior XZ position are allowed. " +
                        GetFirstSignatureDifference(
                            protectedSignature,
                            currentProtectedSignature));
                }

                report.AppendLine();
                for (var pairIndex = 0;
                     pairIndex < EntranceWidthPairDefinitions.Length;
                     pairIndex++)
                {
                    var pair = EntranceWidthPairDefinitions[pairIndex];
                    var actualWidth = GetEntranceOutsideSpan(
                        RequireRenderer(targetScene, pair.FirstWallName),
                        RequireRenderer(targetScene, pair.SecondWallName));
                    if (Mathf.Abs(actualWidth - targetWidth) > EntranceWidthTolerance)
                    {
                        throw new InvalidOperationException(
                            pair.Label + " entrance width does not match the corridor outside width. " +
                            "Expected=" + Float(targetWidth) + "; Actual=" + Float(actualWidth));
                    }

                    report.AppendLine(
                        "ENTRANCE|" + pair.Label +
                        "|Room=" + pair.RoomId +
                        "|OutsideWidth=" + Float(actualWidth) +
                        "|Target=" + Float(targetWidth));
                }

                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save expanded Pegasus room proportions.");
                }

                report.AppendLine();
                report.AppendLine("ProtectedStatePreserved=True");
                report.AppendLine("CorridorStatePreserved=True");
                report.AppendLine("UnityConsoleErrors=0");
                WriteProjectText(
                    RoomProportionExpansionValidationDirectory,
                    RoomProportionExpansionChangeName,
                    report.ToString());
                WriteProjectText(
                    RoomProportionExpansionValidationDirectory,
                    RoomProportionExpansionProtectedStateName,
                    currentProtectedSignature);
                WriteProjectText(
                    RoomProportionExpansionValidationDirectory,
                    RoomProportionExpansionCorridorStateName,
                    currentCorridorSignature);
                Debug.Log(
                    "Pegasus six-room proportions expanded without room-root scaling. " +
                    "EntranceTargetWidth=" + Float(targetWidth) +
                    "m; EntrancePairsNormalized=" + EntranceWidthPairDefinitions.Length +
                    "; CorridorsChanged=False; UnityConsoleErrors=0");
            }
            catch
            {
                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw;
            }
        }

        internal static void CaptureRoomProportionExpansionReview()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Room proportion review capture stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            const int panelWidth = 900;
            const int panelHeight = 700;
            const int columns = 3;
            const int rows = 2;
            const int gutter = 12;
            var sheetWidth = (panelWidth * columns) + (gutter * (columns + 1));
            var sheetHeight = (panelHeight * rows) + (gutter * (rows + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = new Color(0.018f, 0.022f, 0.028f, 1f);
            }

            sheet.SetPixels(backgroundPixels);
            var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            temporaryScene.name = "__PegasusRoomProportionCapture";
            var cameraObject = new GameObject("__PegasusRoomProportionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.32f, 0.22f, 0.11f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = false;
            camera.fieldOfView = 45f;
            var lightObject = new GameObject("__PegasusRoomProportionLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, temporaryScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.92f, 0.96f, 1f, 1f);
            light.intensity = 2.1f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            var panels = new List<Texture2D>(RoomDefinitions.Length);
            try
            {
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var definition = RoomDefinitions[roomIndex];
                    var room = rooms[definition.Id];
                    var anchor = GetRoomExpansionReviewAnchor(
                        targetScene,
                        rooms,
                        definition.Id);
                    var roomBounds = CalculateVisibleBounds(room);
                    var target = room.transform.TransformPoint(new Vector3(0f, 1.25f, 0f));
                    var distance = Mathf.Max(
                        7f,
                        Mathf.Max(roomBounds.size.x, roomBounds.size.z) * 0.72f);
                    camera.transform.position = anchor.Point +
                        (anchor.Outward * distance) +
                        (Vector3.up * 1.8f);
                    camera.transform.rotation = Quaternion.LookRotation(
                        target - camera.transform.position,
                        Vector3.up);
                    panels.Add(RenderCamera(camera, panelWidth, panelHeight));
                }

                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    var column = panelIndex % columns;
                    var row = panelIndex / columns;
                    var panelX = gutter + (column * (panelWidth + gutter));
                    var panelY = gutter + ((rows - 1 - row) * (panelHeight + gutter));
                    sheet.SetPixels(
                        panelX,
                        panelY,
                        panelWidth,
                        panelHeight,
                        panels[panelIndex].GetPixels());
                }

                sheet.Apply(false, false);
                var outputPath = Path.Combine(
                    ProjectRoot,
                    RoomProportionExpansionValidationDirectory.Replace(
                        '/',
                        Path.DirectorySeparatorChar),
                    RoomProportionExpansionReviewName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus six-room proportion direct-review capture completed once. " +
                    "Panels=EngineRoom,Cockpit,ControlRoom,Armory,SupplyRoom,CargoHold; " +
                    "Output=" + outputPath + "; UnityConsoleErrors=0");
            }
            finally
            {
                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    UnityEngine.Object.DestroyImmediate(panels[panelIndex]);
                }

                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.CloseScene(temporaryScene, true);
            }
        }

        internal static void InspectRoomProportionExpansion()
        {
            var validationDirectory = Path.Combine(
                ProjectRoot,
                RoomProportionExpansionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var reviewPath = Path.Combine(
                validationDirectory,
                RoomProportionExpansionReviewName);
            var protectedStatePath = Path.Combine(
                validationDirectory,
                RoomProportionExpansionProtectedStateName);
            var corridorStatePath = Path.Combine(
                validationDirectory,
                RoomProportionExpansionCorridorStateName);
            if (!File.Exists(reviewPath) ||
                !File.Exists(protectedStatePath) ||
                !File.Exists(corridorStatePath))
            {
                throw new InvalidOperationException(
                    "Room proportion inspection requires the saved state and direct-review capture.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Room proportion inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var structuralTargets = CollectRoomExpansionStructuralTargets(rooms);
            var expectedProtectedState = File.ReadAllText(protectedStatePath, Encoding.UTF8);
            var currentProtectedState = BuildRoomProportionProtectedSignature(
                rooms,
                structuralTargets);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus room protected state changed after expansion. " +
                    GetFirstSignatureDifference(expectedProtectedState, currentProtectedState));
            }

            var expectedCorridorState = File.ReadAllText(corridorStatePath, Encoding.UTF8);
            var currentCorridorState = BuildLayoutPlacementSignature(rooms, corridorRoot);
            if (!string.Equals(
                    expectedCorridorState,
                    currentCorridorState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus corridor or room-root placement changed after expansion.");
            }

            var targetWidth = GetCommonCorridorOutsideWidth(modules);
            var report = new StringBuilder(32 * 1024);
            report.AppendLine("Pegasus six-room proportion expansion inspection");
            report.AppendLine("DirectVisualReview=PassedBeforeNumericInspection");
            report.AppendLine("TargetCorridorFullOutsideWidth=" + Float(targetWidth));
            report.AppendLine("ProtectedStatePreserved=True");
            report.AppendLine("CorridorStatePreserved=True");
            report.AppendLine();
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id];
                var expectedCeilingTop = definition.Id == "CargoHold" ? 5.5f : 6f;
                var ceilingTop = GetMainCeilingTop(room);
                if (Mathf.Abs(ceilingTop - expectedCeilingTop) > PositionTolerance)
                {
                    throw new InvalidOperationException(
                        definition.Id + " ceiling height is no longer preserved.");
                }

                report.AppendLine(
                    "ROOM|" + definition.Id +
                    "|RootScale=" + Vector(room.transform.localScale) +
                    "|CeilingTop=" + Float(ceilingTop) +
                    "|VisibleBounds=" + BoundsText(CalculateVisibleBounds(room)));
            }

            report.AppendLine();
            for (var pairIndex = 0;
                 pairIndex < EntranceWidthPairDefinitions.Length;
                 pairIndex++)
            {
                var pair = EntranceWidthPairDefinitions[pairIndex];
                var actualWidth = GetEntranceOutsideSpan(
                    RequireRenderer(targetScene, pair.FirstWallName),
                    RequireRenderer(targetScene, pair.SecondWallName));
                var error = Mathf.Abs(actualWidth - targetWidth);
                if (error > EntranceWidthTolerance)
                {
                    throw new InvalidOperationException(
                        pair.Label + " entrance width drifted after direct review. " +
                        "Error=" + Float(error));
                }

                report.AppendLine(
                    "ENTRANCE|" + pair.Label +
                    "|OutsideWidth=" + Float(actualWidth) +
                    "|Error=" + Float(error));
            }

            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            report.AppendLine();
            report.AppendLine("UnityConsoleErrors=0");
            report.AppendLine("Result=PASS");
            WriteProjectText(
                RoomProportionExpansionValidationDirectory,
                RoomProportionExpansionInspectionName,
                report.ToString());
            File.Copy(
                reviewPath,
                Path.Combine(validationDirectory, RoomProportionExpansionFinalName),
                true);
            Debug.Log(
                "Pegasus room proportion expansion inspection passed after direct visual review. " +
                "Rooms=6; EntrancePairs=" + EntranceWidthPairDefinitions.Length +
                "; TargetWidth=" + Float(targetWidth) +
                "m; CorridorChanges=False; UnityConsoleErrors=0");
        }

        internal static void InspectRoomInteriorProportionSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Interior proportion source inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var targetRooms = RequireRooms(targetScene);
            var sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            try
            {
                if (sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp has unsaved editor changes. Source inspection will not save or discard them.");
                }

                var sourceRooms = RequireRooms(sourceScene);
                var sourceCorridorRoot = RequireSceneObject(sourceScene, CorridorRootName);
                var sourceSignature = BuildSourceSignature(sourceRooms, sourceCorridorRoot);
                var report = new StringBuilder(256 * 1024);
                report.AppendLine("Pegasus room-interior proportion source inspection");
                report.AppendLine("SourceScene=" + SourceScenePath);
                report.AppendLine("TargetScene=" + TargetScenePath);
                report.AppendLine("SourceSceneReadOnly=True");
                report.AppendLine("Mapping=Source room structural bounds to expanded Pegasus structural bounds");
                report.AppendLine();

                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var definition = RoomDefinitions[roomIndex];
                    var sourceRoom = sourceRooms[definition.Id].transform;
                    var targetRoom = targetRooms[definition.Id].transform;
                    var sourceStructure = RequireRoomStructuralBounds(sourceRoom, definition.Id);
                    var targetStructure = RequireRoomStructuralBounds(targetRoom, definition.Id);
                    var ratioX = RequirePositiveRatio(
                        targetStructure.size.x,
                        sourceStructure.size.x,
                        definition.Id + " structural X");
                    var ratioZ = RequirePositiveRatio(
                        targetStructure.size.z,
                        sourceStructure.size.z,
                        definition.Id + " structural Z");
                    report.AppendLine(
                        "ROOM|" + definition.Id +
                        "|SourceStructure=" + BoundsText(sourceStructure) +
                        "|TargetStructure=" + BoundsText(targetStructure) +
                        "|RatioX=" + Float(ratioX) +
                        "|RatioZ=" + Float(ratioZ));

                    var targetInteriors = CollectRoomInteriorDirectChildren(targetRoom, definition.Id);
                    for (var interiorIndex = 0; interiorIndex < targetInteriors.Count; interiorIndex++)
                    {
                        var targetInterior = targetInteriors[interiorIndex];
                        var sourceInterior = ResolveSourceInteriorTransform(
                            sourceScene,
                            sourceRoom,
                            definition.Id,
                            targetInterior.name);
                        var sourceRelative = sourceRoom.worldToLocalMatrix * sourceInterior.localToWorldMatrix;
                        var sourceCenter = GetRoomLocalVisibleCenterOrPosition(
                            sourceInterior,
                            sourceRoom,
                            sourceRelative.GetColumn(3));
                        var targetCenter = GetRoomLocalVisibleCenterOrPosition(
                            targetInterior,
                            targetRoom,
                            targetInterior.localPosition);
                        var sourceNormalized = NormalizeHorizontalPoint(sourceCenter, sourceStructure);
                        var targetNormalized = NormalizeHorizontalPoint(targetCenter, targetStructure);
                        report.AppendLine(
                            "INTERIOR|" + definition.Id + "/" + targetInterior.name +
                            "|SourceKind=" + (sourceInterior.parent == sourceRoom ? "RoomChild" : "DetachedRoot") +
                            "|SourceCenter=" + Vector(sourceCenter) +
                            "|TargetCenter=" + Vector(targetCenter) +
                            "|SourceNormalized=" + Vector(sourceNormalized) +
                            "|TargetNormalized=" + Vector(targetNormalized) +
                            "|SourceRelativeScale=" + Vector(sourceRelative.lossyScale) +
                            "|TargetScale=" + Vector(targetInterior.localScale) +
                            "|Visible=" + HasActiveVisibleRenderer(sourceInterior.gameObject));
                    }

                    report.AppendLine("ROOM_END|" + definition.Id + "|InteriorRoots=" + targetInteriors.Count);
                    report.AppendLine();
                }

                if (!string.Equals(
                        sourceSignature,
                        BuildSourceSignature(sourceRooms, sourceCorridorRoot),
                        StringComparison.Ordinal) ||
                    sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp changed during read-only interior proportion source inspection.");
                }

                WriteProjectText(
                    RoomInteriorProportionValidationDirectory,
                    RoomInteriorProportionSourceName,
                    report.ToString());
            }
            finally
            {
                if (sourceScene.IsValid() && sourceScene.isLoaded && !sourceScene.isDirty)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }

            SceneManager.SetActiveScene(targetScene);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "CargoRunMvp and Pegasus room-interior proportions inspected read-only. " +
                "Rooms=6; SourceSceneModified=False; UnityConsoleErrors=0");
        }

        internal static void ApplyRoomInteriorProportionAdjustment()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Interior proportion adjustment stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var targetRooms = RequireRooms(targetScene);
            var targetCorridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var layoutSignature = BuildLayoutPlacementSignature(targetRooms, targetCorridorRoot);
            var protectedSignature = BuildRoomInteriorProportionProtectedSignature(targetRooms);
            var sourceScene = default(Scene);
            try
            {
                sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
                if (sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp has unsaved editor changes. Interior adjustment will not save or discard them.");
                }

                var sourceRooms = RequireRooms(sourceScene);
                var sourceCorridorRoot = RequireSceneObject(sourceScene, CorridorRootName);
                var sourceSignature = BuildSourceSignature(sourceRooms, sourceCorridorRoot);
                var report = new StringBuilder(128 * 1024);
                report.AppendLine("Pegasus room-interior proportional adjustment");
                report.AppendLine("SourceScene=" + SourceScenePath);
                report.AppendLine("TargetScene=" + TargetScenePath);
                report.AppendLine("RoomRootsChanged=False");
                report.AppendLine("StructuresChanged=False");
                report.AppendLine("CorridorsChanged=False");
                report.AppendLine("VerticalPlacementChanged=False");
                report.AppendLine("RotationsChanged=False");
                report.AppendLine();

                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var definition = RoomDefinitions[roomIndex];
                    var sourceRoom = sourceRooms[definition.Id].transform;
                    var targetRoom = targetRooms[definition.Id].transform;
                    var sourceStructure = RequireRoomStructuralBounds(sourceRoom, definition.Id);
                    var targetStructure = RequireRoomStructuralBounds(targetRoom, definition.Id);
                    var ratioX = RequirePositiveRatio(
                        targetStructure.size.x,
                        sourceStructure.size.x,
                        definition.Id + " structural X");
                    var ratioZ = RequirePositiveRatio(
                        targetStructure.size.z,
                        sourceStructure.size.z,
                        definition.Id + " structural Z");
                    var targetInteriors = CollectRoomInteriorDirectChildren(targetRoom, definition.Id);
                    report.AppendLine(
                        "ROOM|" + definition.Id +
                        "|RatioX=" + Float(ratioX) +
                        "|RatioZ=" + Float(ratioZ) +
                        "|InteriorRoots=" + targetInteriors.Count);

                    for (var interiorIndex = 0; interiorIndex < targetInteriors.Count; interiorIndex++)
                    {
                        var targetInterior = targetInteriors[interiorIndex];
                        var sourceInterior = ResolveSourceInteriorTransform(
                            sourceScene,
                            sourceRoom,
                            definition.Id,
                            targetInterior.name);
                        var sourceRelative = sourceRoom.worldToLocalMatrix * sourceInterior.localToWorldMatrix;
                        var sourceCenter = GetRoomLocalVisibleCenterOrPosition(
                            sourceInterior,
                            sourceRoom,
                            sourceRelative.GetColumn(3));
                        var normalized = NormalizeHorizontalPoint(sourceCenter, sourceStructure);
                        var desiredCenter = DenormalizeHorizontalPoint(
                            normalized,
                            targetStructure,
                            sourceCenter.y);
                        var beforePosition = targetInterior.localPosition;
                        var beforeScale = targetInterior.localScale;
                        var beforeY = targetRoom.InverseTransformPoint(targetInterior.position).y;
                        var beforeRotation = targetInterior.localRotation;
                        var visible = HasActiveVisibleRenderer(sourceInterior.gameObject);

                        if (visible)
                        {
                            targetInterior.localScale = CalculateProportionalInteriorScale(
                                sourceRelative.lossyScale,
                                targetInterior.localRotation,
                                ratioX,
                                ratioZ);
                        }

                        var targetCenter = GetRoomLocalVisibleCenterOrPosition(
                            targetInterior,
                            targetRoom,
                            targetInterior.localPosition);
                        var localPosition = targetInterior.localPosition;
                        localPosition.x += desiredCenter.x - targetCenter.x;
                        localPosition.z += desiredCenter.z - targetCenter.z;
                        targetInterior.localPosition = localPosition;

                        var afterY = targetRoom.InverseTransformPoint(targetInterior.position).y;
                        if (Mathf.Abs(beforeY - afterY) > PositionTolerance ||
                            !Approximately(beforeRotation, targetInterior.localRotation))
                        {
                            throw new InvalidOperationException(
                                definition.Id + "/" + targetInterior.name +
                                " changed vertical placement or rotation during proportional adjustment.");
                        }

                        report.AppendLine(
                            "INTERIOR|" + definition.Id + "/" + targetInterior.name +
                            "|Visible=" + visible +
                            "|SourceNormalized=" + Vector(normalized) +
                            "|DesiredCenter=" + Vector(desiredCenter) +
                            "|PositionBefore=" + Vector(beforePosition) +
                            "|PositionAfter=" + Vector(targetInterior.localPosition) +
                            "|ScaleBefore=" + Vector(beforeScale) +
                            "|ScaleAfter=" + Vector(targetInterior.localScale));
                    }
                }

                if (!string.Equals(
                        protectedSignature,
                        BuildRoomInteriorProportionProtectedSignature(targetRooms),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Pegasus structure, vertical placement, rotation, component, or interior hierarchy changed " +
                        "outside the approved interior X/Z adjustment fields.");
                }

                if (!string.Equals(
                        layoutSignature,
                        BuildLayoutPlacementSignature(targetRooms, targetCorridorRoot),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Pegasus room roots or corridor placements changed during interior adjustment.");
                }

                if (!string.Equals(
                        sourceSignature,
                        BuildSourceSignature(sourceRooms, sourceCorridorRoot),
                        StringComparison.Ordinal) ||
                    sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp changed during read-only interior adjustment reference use.");
                }

                WriteProjectText(
                    RoomInteriorProportionValidationDirectory,
                    RoomInteriorProportionChangeName,
                    report.ToString());
                WriteProjectText(
                    RoomInteriorProportionValidationDirectory,
                    RoomInteriorProportionProtectedStateName,
                    protectedSignature);

                if (!EditorSceneManager.CloseScene(sourceScene, true))
                {
                    throw new InvalidOperationException(
                        "Failed to close the read-only CargoRunMvp reference scene.");
                }

                sourceScene = default(Scene);
                SceneManager.SetActiveScene(targetScene);
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save the proportionally adjusted Pegasus interiors.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus interiors adjusted from CargoRunMvp room-relative proportions. " +
                    "Rooms=6; RoomRootsChanged=False; StructuresChanged=False; CorridorsChanged=False; " +
                    "SourceSceneModified=False; UnityConsoleErrors=0");
            }
            catch (Exception adjustmentException)
            {
                if (sourceScene.IsValid() && sourceScene.isLoaded && !sourceScene.isDirty)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                    sourceScene = default(Scene);
                }

                if ((!sourceScene.IsValid() || !sourceScene.isLoaded) &&
                    targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                    EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                }

                throw new InvalidOperationException(
                    "Pegasus interior proportion adjustment failed; unsaved agent changes were discarded and " +
                    "the saved Pegasus scene was reopened.",
                    adjustmentException);
            }
        }

        internal static void CaptureRoomInteriorProportionReview()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Interior comparison capture stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            try
            {
                if (sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp has unsaved editor changes. Interior comparison capture stopped.");
                }

                var outputPath = Path.Combine(
                    ProjectRoot,
                    RoomInteriorProportionValidationDirectory.Replace(
                        '/',
                        Path.DirectorySeparatorChar),
                    RoomInteriorProportionReviewName);
                CaptureRoomInteriorProportionComparisonSheet(sourceScene, targetScene, outputPath);
                if (sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp changed during room-interior comparison capture.");
                }
            }
            finally
            {
                if (sourceScene.IsValid() && sourceScene.isLoaded && !sourceScene.isDirty)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }

            SceneManager.SetActiveScene(targetScene);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus room-interior proportion direct comparison captured once. " +
                "Columns=CargoRunMvp,Pegasus; Rows=6 rooms; SourceSceneModified=False; UnityConsoleErrors=0");
        }

        internal static void InspectRoomInteriorProportion()
        {
            var validationDirectory = Path.Combine(
                ProjectRoot,
                RoomInteriorProportionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var reviewPath = Path.Combine(validationDirectory, RoomInteriorProportionReviewName);
            var protectedPath = Path.Combine(
                validationDirectory,
                RoomInteriorProportionProtectedStateName);
            if (!File.Exists(reviewPath) || !File.Exists(protectedPath))
            {
                throw new InvalidOperationException(
                    "Interior proportion inspection requires the protected state and direct comparison capture.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Interior proportion inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var targetRooms = RequireRooms(targetScene);
            var protectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState = BuildRoomInteriorProportionProtectedSignature(targetRooms);
            if (!string.Equals(protectedState, currentProtectedState, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus protected room/interior state changed after direct visual review. " +
                    GetFirstSignatureDifference(protectedState, currentProtectedState));
            }

            var sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            var report = new StringBuilder(128 * 1024);
            try
            {
                if (sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp has unsaved editor changes. Interior proportion inspection stopped.");
                }

                var sourceRooms = RequireRooms(sourceScene);
                var sourceCorridorRoot = RequireSceneObject(sourceScene, CorridorRootName);
                var sourceSignature = BuildSourceSignature(sourceRooms, sourceCorridorRoot);
                report.AppendLine("Pegasus room-interior proportion inspection");
                report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
                report.AppendLine("DirectVisualReview=PassedBeforeNumericInspection");
                report.AppendLine("DirectComparison=" + reviewPath);
                report.AppendLine("ComparisonColumns=CargoRunMvp,Pegasus");
                report.AppendLine("ComparisonRows=EngineRoom,Cockpit,ControlRoom,Armory,SupplyRoom,CargoHold");
                report.AppendLine("ProtectedStatePreserved=True");
                report.AppendLine();

                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var definition = RoomDefinitions[roomIndex];
                    var sourceRoom = sourceRooms[definition.Id].transform;
                    var targetRoom = targetRooms[definition.Id].transform;
                    var sourceStructure = RequireRoomStructuralBounds(sourceRoom, definition.Id);
                    var targetStructure = RequireRoomStructuralBounds(targetRoom, definition.Id);
                    var ratioX = RequirePositiveRatio(
                        targetStructure.size.x,
                        sourceStructure.size.x,
                        definition.Id + " structural X");
                    var ratioZ = RequirePositiveRatio(
                        targetStructure.size.z,
                        sourceStructure.size.z,
                        definition.Id + " structural Z");
                    var targetInteriors = CollectRoomInteriorDirectChildren(targetRoom, definition.Id);
                    report.AppendLine(
                        "ROOM|" + definition.Id +
                        "|RatioX=" + Float(ratioX) +
                        "|RatioZ=" + Float(ratioZ) +
                        "|InteriorRoots=" + targetInteriors.Count);

                    for (var interiorIndex = 0; interiorIndex < targetInteriors.Count; interiorIndex++)
                    {
                        var targetInterior = targetInteriors[interiorIndex];
                        var sourceInterior = ResolveSourceInteriorTransform(
                            sourceScene,
                            sourceRoom,
                            definition.Id,
                            targetInterior.name);
                        var sourceRelative = sourceRoom.worldToLocalMatrix * sourceInterior.localToWorldMatrix;
                        var sourceCenter = GetRoomLocalVisibleCenterOrPosition(
                            sourceInterior,
                            sourceRoom,
                            sourceRelative.GetColumn(3));
                        var targetCenter = GetRoomLocalVisibleCenterOrPosition(
                            targetInterior,
                            targetRoom,
                            targetInterior.localPosition);
                        var sourceNormalized = NormalizeHorizontalPoint(sourceCenter, sourceStructure);
                        var targetNormalized = NormalizeHorizontalPoint(targetCenter, targetStructure);
                        var normalizedError = Mathf.Max(
                            Mathf.Abs(sourceNormalized.x - targetNormalized.x),
                            Mathf.Abs(sourceNormalized.z - targetNormalized.z));
                        if (normalizedError > InteriorProportionTolerance)
                        {
                            throw new InvalidOperationException(
                                definition.Id + "/" + targetInterior.name +
                                " no longer matches the CargoRunMvp room-relative position. Error=" +
                                Float(normalizedError));
                        }

                        var visible = HasActiveVisibleRenderer(sourceInterior.gameObject);
                        var expectedScale = visible
                            ? CalculateProportionalInteriorScale(
                                sourceRelative.lossyScale,
                                targetInterior.localRotation,
                                ratioX,
                                ratioZ)
                            : targetInterior.localScale;
                        if (visible && !Approximately(expectedScale, targetInterior.localScale))
                        {
                            throw new InvalidOperationException(
                                definition.Id + "/" + targetInterior.name +
                                " no longer matches the proportional horizontal scale. Expected=" +
                                Vector(expectedScale) + "; Actual=" + Vector(targetInterior.localScale));
                        }

                        report.AppendLine(
                            "INTERIOR|" + definition.Id + "/" + targetInterior.name +
                            "|Visible=" + visible +
                            "|SourceNormalized=" + Vector(sourceNormalized) +
                            "|TargetNormalized=" + Vector(targetNormalized) +
                            "|NormalizedError=" + Float(normalizedError) +
                            "|Scale=" + Vector(targetInterior.localScale));
                    }
                }

                if (!string.Equals(
                        sourceSignature,
                        BuildSourceSignature(sourceRooms, sourceCorridorRoot),
                        StringComparison.Ordinal) ||
                    sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp changed during read-only interior proportion inspection.");
                }
            }
            finally
            {
                if (sourceScene.IsValid() && sourceScene.isLoaded && !sourceScene.isDirty)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }

            SceneManager.SetActiveScene(targetScene);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            report.AppendLine();
            report.AppendLine("SourceSceneModified=False");
            report.AppendLine("UnityConsoleErrors=0");
            report.AppendLine("Result=PASS");
            WriteProjectText(
                RoomInteriorProportionValidationDirectory,
                RoomInteriorProportionInspectionName,
                report.ToString());
            File.Copy(
                reviewPath,
                Path.Combine(validationDirectory, RoomInteriorProportionFinalName),
                true);
            Debug.Log(
                "Pegasus room-interior proportion inspection passed after direct visual review. " +
                "Rooms=6; SourceSceneModified=False; UnityConsoleErrors=0");
        }

        internal static void InspectHorizontalCorridorLengthSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Horizontal corridor inspection will not discard them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var settings = AssetDatabase.LoadAssetAtPath<
                Bellerophon.Core.Player.FirstPersonPlayerSettings>(PlayerSettingsPath) ??
                throw new InvalidOperationException(
                    "Missing canonical player settings at " + PlayerSettingsPath);
            var walkSpeed = settings.WalkSpeed;
            var targetLength = walkSpeed * HorizontalCorridorTravelSeconds;
            var report = new StringBuilder(64 * 1024);
            report.AppendLine("Pegasus horizontal corridor length source inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("PlayerSettings=" + PlayerSettingsPath);
            report.AppendLine("CanonicalWalkSpeed=" + Float(walkSpeed));
            report.AppendLine("TargetTravelSeconds=" + Float(HorizontalCorridorTravelSeconds));
            report.AppendLine("TargetCorridorLength=" + Float(targetLength));
            report.AppendLine("RoomsRemainUnchanged=True");
            report.AppendLine("CorridorsRemainDetached=True");
            report.AppendLine();

            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                if (definition.IsSloped)
                {
                    continue;
                }

                var module = modules[definition.ModuleId];
                var moduleBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
                var fromBounds = CalculateVisibleBounds(rooms[definition.FromRoomId]);
                var toBounds = CalculateVisibleBounds(rooms[definition.ToRoomId]);
                var ceilingFollowers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                report.AppendLine("[" + definition.ModuleId + "]");
                report.AppendLine("Root=" + module.name);
                report.AppendLine("Position=" + Vector(module.position));
                report.AppendLine("Rotation=" + QuaternionText(module.rotation));
                report.AppendLine("LocalScale=" + Vector(module.localScale));
                report.AppendLine("LocalVisibleBounds=" + BoundsText(moduleBounds));
                report.AppendLine("CurrentLength=" + Float(moduleBounds.size.x));
                report.AppendLine("FromRoom=" + definition.FromRoomId +
                    "|Bounds=" + BoundsText(fromBounds));
                report.AppendLine("ToRoom=" + definition.ToRoomId +
                    "|Bounds=" + BoundsText(toBounds));
                report.AppendLine("CeilingFollower=" + ceilingFollowers[0].Transform.name +
                    "|Position=" + Vector(ceilingFollowers[0].Transform.position) +
                    "|Rotation=" + QuaternionText(ceilingFollowers[0].Transform.rotation) +
                    "|LocalScale=" + Vector(ceilingFollowers[0].Transform.localScale));
                var transforms = module.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    report.Append("  ")
                        .Append(GetHierarchyPath(module, target))
                        .Append("|Active=").Append(target.gameObject.activeSelf)
                        .Append("|LocalPosition=").Append(Vector(target.localPosition))
                        .Append("|LocalRotation=").Append(QuaternionText(target.localRotation))
                        .Append("|LocalScale=").Append(Vector(target.localScale))
                        .Append("|").Append(BuildComponentSignature(target.gameObject));
                    if (TryCalculateVisibleBoundsInLocalSpace(target.gameObject, module, out var localBounds))
                    {
                        report.Append("|ModuleLocalBounds=").Append(BoundsText(localBounds));
                    }

                    report.AppendLine();
                }

                report.AppendLine();
            }

            WriteProjectText(
                HorizontalCorridorLogDirectory,
                "Sources.txt",
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus horizontal corridor sources inspected read-only. " +
                "HorizontalCorridors=5; CanonicalWalkSpeed=" + Float(walkSpeed) +
                "m/s; TargetTravelSeconds=5; TargetLength=" + Float(targetLength) +
                "m; RoomsChanged=False; CorridorTransformsChanged=False; UnityConsoleErrors=0");
        }

        internal static void InspectTriRoomCorridorConnectionSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Tri-room connection inspection will not discard them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var report = new StringBuilder(64 * 1024);
            report.AppendLine("Pegasus engine-cockpit-control corridor connection source inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetRooms=EngineRoom,Cockpit,ControlRoom");
            report.AppendLine("TargetCorridors=SC-H01,SC-H02,SC-H05");
            report.AppendLine("CreateConnectorPieces=False");
            report.AppendLine("AllowMeshOverlap=False");
            report.AppendLine();

            AppendEntranceCandidates(
                report,
                "EngineRoomToCockpit",
                rooms["EngineRoom"].transform,
                name => name.IndexOf("Cockpit", StringComparison.OrdinalIgnoreCase) >= 0);
            AppendEntranceCandidates(
                report,
                "EngineRoomToControlRoom",
                rooms["EngineRoom"].transform,
                name => name.IndexOf("Control", StringComparison.OrdinalIgnoreCase) >= 0);
            AppendEntranceCandidates(
                report,
                "CockpitToEngineRoom",
                rooms["Cockpit"].transform,
                name => name.IndexOf(
                    "left future opening floor edge",
                    StringComparison.OrdinalIgnoreCase) >= 0);
            AppendEntranceCandidates(
                report,
                "CockpitToControlRoom",
                rooms["Cockpit"].transform,
                name => name.IndexOf(
                    "right future opening floor edge",
                    StringComparison.OrdinalIgnoreCase) >= 0);
            AppendEntranceCandidates(
                report,
                "ControlRoomToCockpit",
                rooms["ControlRoom"].transform,
                name => name.IndexOf("cockpit", StringComparison.OrdinalIgnoreCase) >= 0);
            AppendEntranceCandidates(
                report,
                "ControlRoomToEngineRoom",
                rooms["ControlRoom"].transform,
                name => name.IndexOf("engine room left", StringComparison.OrdinalIgnoreCase) >= 0);

            var targetCorridorIds = new[] { "SC-H01", "SC-H02", "SC-H05" };
            report.AppendLine("[CorridorEndpoints]");
            for (var corridorIndex = 0; corridorIndex < targetCorridorIds.Length; corridorIndex++)
            {
                var corridorId = targetCorridorIds[corridorIndex];
                var module = modules[corridorId];
                var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
                var lowEnd = module.TransformPoint(new Vector3(
                    localBounds.min.x,
                    localBounds.center.y,
                    localBounds.center.z));
                var highEnd = module.TransformPoint(new Vector3(
                    localBounds.max.x,
                    localBounds.center.y,
                    localBounds.center.z));
                var ceilingFollowers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                report.AppendLine(
                    corridorId +
                    "|RootPosition=" + Vector(module.position) +
                    "|RootRotation=" + QuaternionText(module.rotation) +
                    "|Length=" + Float(localBounds.size.x) +
                    "|Width=" + Float(localBounds.size.z) +
                    "|LowEnd=" + Vector(lowEnd) +
                    "|HighEnd=" + Vector(highEnd) +
                    "|Axis=" + Vector(module.right.normalized) +
                    "|CeilingFollowers=" + ceilingFollowers.Count);
            }

            report.AppendLine();
            report.AppendLine("[RoomRoots]");
            var targetRoomIds = new[] { "EngineRoom", "Cockpit", "ControlRoom" };
            for (var roomIndex = 0; roomIndex < targetRoomIds.Length; roomIndex++)
            {
                var room = rooms[targetRoomIds[roomIndex]].transform;
                report.AppendLine(
                    targetRoomIds[roomIndex] +
                    "|Position=" + Vector(room.position) +
                    "|Rotation=" + QuaternionText(room.rotation) +
                    "|Scale=" + Vector(room.localScale));
            }

            WriteProjectText(
                TriRoomConnectionLogDirectory,
                TriRoomConnectionProtectedSignatureName,
                BuildTriRoomConnectionProtectedSignature(rooms, corridorRoot.transform, modules));

            WriteProjectText(
                TriRoomConnectionLogDirectory,
                TriRoomConnectionSourceReportName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room corridor connection sources inspected read-only. " +
                "Rooms=3; Corridors=3; UnityConsoleErrors=0");
        }

        internal static void ConnectTriRoomCorridors()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Tri-room connection will not overwrite them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var protectedSignaturePath = Path.Combine(
                ProjectRoot,
                TriRoomConnectionLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomConnectionProtectedSignatureName);
            if (!File.Exists(protectedSignaturePath))
            {
                throw new InvalidOperationException(
                    "Missing tri-room pre-connection protected signature: " + protectedSignaturePath);
            }

            var originalProtectedSignature = File.ReadAllText(
                protectedSignaturePath,
                Encoding.UTF8);
            var preConnectionSignature = BuildTriRoomConnectionProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules);
            if (!string.Equals(
                    originalProtectedSignature,
                    preConnectionSignature,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus changed after tri-room source inspection. Connection stopped before saving.");
            }

            try
            {
                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H01"],
                    ceilingRoot,
                    "SC-H01",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"));
                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H02"],
                    ceilingRoot,
                    "SC-H02",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"));
                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H05"],
                    ceilingRoot,
                    "SC-H05",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "ControlRoom"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "EngineRoom"));

                var postConnectionSignature = BuildTriRoomConnectionProtectedSignature(
                    rooms,
                    corridorRoot.transform,
                    modules);
                if (!string.Equals(
                        originalProtectedSignature,
                        postConnectionSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Tri-room connection changed protected room, corridor, ceiling, hierarchy, or component state.");
                }

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException("Failed to save connected Pegasus scene.");
                }

                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus tri-room corridors aligned without forced overlap. " +
                    "RoomsMoved=False; TargetCorridors=SC-H01,SC-H02,SC-H05; " +
                    "ConnectorPiecesCreated=False; UnityConsoleErrors=0");
            }
            catch (Exception connectionException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw new InvalidOperationException(
                    "Pegasus tri-room connection failed; the saved target scene was reopened without saving partial changes.",
                    connectionException);
            }
        }

        internal static void CaptureTriRoomCorridorConnectionFinal()
        {
            var inspectionPath = Path.Combine(
                ProjectRoot,
                TriRoomConnectionValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomConnectionInspectionName);
            var reviewPath = Path.Combine(
                ProjectRoot,
                TriRoomConnectionLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomConnectionReviewName);
            if (!File.Exists(inspectionPath))
            {
                CaptureTriRoomCorridorConnectionReview(reviewPath);
                return;
            }

            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Missing directly reviewed tri-room connection image: " + reviewPath);
            }

            var finalDirectory = Path.Combine(
                ProjectRoot,
                TriRoomConnectionValidationDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(finalDirectory);
            var finalPath = Path.Combine(finalDirectory, TriRoomConnectionFinalName);
            File.Copy(reviewPath, finalPath, true);
            Debug.Log(
                "Verified Pegasus tri-room connection review promoted to final evidence without recapturing. " +
                "Output=" + finalPath);
        }

        internal static void InspectTriRoomCorridorConnection()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Tri-room connection inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var protectedSignaturePath = Path.Combine(
                ProjectRoot,
                TriRoomConnectionLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomConnectionProtectedSignatureName);
            var reviewPath = Path.Combine(
                ProjectRoot,
                TriRoomConnectionLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomConnectionReviewName);
            if (!File.Exists(protectedSignaturePath) || !File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Tri-room connection inspection requires the protected signature and direct review image.");
            }

            var originalProtectedSignature = File.ReadAllText(
                protectedSignaturePath,
                Encoding.UTF8);
            var currentProtectedSignature = BuildTriRoomConnectionProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules);
            if (!string.Equals(
                    originalProtectedSignature,
                    currentProtectedSignature,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Protected Pegasus state differs after tri-room corridor alignment.");
            }

            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus engine-cockpit-control corridor connection inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("RoomsMoved=False");
            report.AppendLine("ConnectorPiecesCreated=False");
            report.AppendLine("TargetCorridors=SC-H01,SC-H02,SC-H05");
            report.AppendLine("UntargetedRoomsAndCorridorsChanged=False");
            report.AppendLine("PersistentSceneRoots=7");

            AppendTriRoomConnectionInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H01"],
                "SC-H01",
                GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"));
            AppendTriRoomConnectionInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H02"],
                "SC-H02",
                GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"));
            AppendTriRoomConnectionInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H05"],
                "SC-H05",
                GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "ControlRoom"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "EngineRoom"));

            report.AppendLine("UnityConsoleErrors=0");
            WriteProjectText(
                TriRoomConnectionValidationDirectory,
                TriRoomConnectionInspectionName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room corridor connection inspected after direct review. " +
                "RoomsMoved=False; MeshOverlap=False; PositiveConnectorGaps=True; " +
                "UnityConsoleErrors=0");
        }

        internal static void InspectTriRoomEntranceConnectionSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Flush entrance source inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var report = new StringBuilder(32 * 1024);
            report.AppendLine("Pegasus tri-room flush entrance connection source inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetRooms=EngineRoom,Cockpit,ControlRoom");
            report.AppendLine("TargetCorridor=SC-H02");
            report.AppendLine("PreservedCorridors=SC-H01,SC-H05");
            report.AppendLine("CockpitEndGap=0");
            report.AppendLine("ControlRoomEndGap=" + Float(H02ControlRoomBufferGap));
            report.AppendLine("AllowEntranceOverlap=False");
            report.AppendLine("RoomMovementAllowed=False");
            report.AppendLine();

            AppendTriRoomFlushSource(
                report,
                targetScene,
                rooms,
                modules["SC-H01"],
                "SC-H01",
                "EngineRoom",
                "Cockpit");
            AppendTriRoomFlushSource(
                report,
                targetScene,
                rooms,
                modules["SC-H02"],
                "SC-H02",
                "Cockpit",
                "ControlRoom");
            AppendTriRoomFlushSource(
                report,
                targetScene,
                rooms,
                modules["SC-H05"],
                "SC-H05",
                "EngineRoom",
                "ControlRoom");

            report.AppendLine();
            report.AppendLine("[RoomRoots]");
            AppendRoomRoot(report, "EngineRoom", rooms["EngineRoom"].transform);
            AppendRoomRoot(report, "Cockpit", rooms["Cockpit"].transform);
            AppendRoomRoot(report, "ControlRoom", rooms["ControlRoom"].transform);
            report.AppendLine(
                "ControlRoomSourceX=" + Float(rooms["ControlRoom"].transform.position.x));
            report.AppendLine(
                "ControlRoomSourceZ=" + Float(rooms["ControlRoom"].transform.position.z));
            report.AppendLine(
                "CockpitSourceZ=" + Float(rooms["Cockpit"].transform.position.z));
            report.AppendLine(
                "CockpitSourceX=" + Float(rooms["Cockpit"].transform.position.x));
            var sourceH01From = GetTriRoomFlushEntranceAnchor(
                targetScene,
                rooms,
                "EngineRoom",
                "Cockpit");
            var sourceH01To = GetTriRoomFlushEntranceAnchor(
                targetScene,
                rooms,
                "Cockpit",
                "EngineRoom");
            var sourceH02From = GetTriRoomFlushEntranceAnchor(
                targetScene,
                rooms,
                "Cockpit",
                "ControlRoom");
            var sourceH02To = GetTriRoomFlushEntranceAnchor(
                targetScene,
                rooms,
                "ControlRoom",
                "Cockpit");
            CalculateEntrancePairMeetingAngles(
                sourceH01From,
                sourceH01To,
                out var sourceH01FromMeetingAngle,
                out var sourceH01ToMeetingAngle);
            CalculateEntrancePairMeetingAngles(
                sourceH02From,
                sourceH02To,
                out var sourceH02FromMeetingAngle,
                out var sourceH02ToMeetingAngle);
            report.AppendLine(
                "H02WorldAngleFromXAxis=" +
                Float(CalculateHorizontalAngleFromXAxis(sourceH02From.Point, sourceH02To.Point)));
            report.AppendLine(
                "H01WorldAngleFromZAxis=" +
                Float(CalculateHorizontalAngleFromZAxis(sourceH01From.Point, sourceH01To.Point)));
            report.AppendLine(
                "H02WorldAngleFromZAxis=" +
                Float(CalculateHorizontalAngleFromZAxis(sourceH02From.Point, sourceH02To.Point)));
            report.AppendLine(
                "H01PlannedFromMeetingAngle=" + Float(sourceH01FromMeetingAngle));
            report.AppendLine(
                "H01PlannedToMeetingAngle=" + Float(sourceH01ToMeetingAngle));
            report.AppendLine(
                "H02PlannedFromMeetingAngle=" + Float(sourceH02FromMeetingAngle));
            report.AppendLine(
                "H02PlannedToMeetingAngle=" + Float(sourceH02ToMeetingAngle));
            report.AppendLine(
                "H01CockpitEndPlanePenetration=" +
                Float(CalculateCorridorEndPlanePenetration(
                    modules["SC-H01"],
                    GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        "Cockpit",
                        "EngineRoom"),
                    false)));
            report.AppendLine(
                "H02CockpitEndPlanePenetration=" +
                Float(CalculateCorridorEndPlanePenetration(
                    modules["SC-H02"],
                    sourceH02From,
                    true)));
            report.AppendLine("RoomOverlap=False");
            RequireNoSelectedTriRoomOverlap(rooms);

            WriteProjectText(
                TriRoomEntranceConnectionValidationDirectory,
                TriRoomEntranceConnectionProtectedStateName,
                BuildTriRoomEntranceConnectionProtectedSignature(
                    rooms,
                    corridorRoot.transform,
                    modules,
                    ceilingRoot));
            WriteProjectText(
                TriRoomEntranceConnectionValidationDirectory,
                TriRoomEntranceConnectionSourceName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room entrance connection sources inspected read-only. " +
                "Rooms=3; Corridors=3; UnityConsoleErrors=0");
        }

        internal static void ApplyTriRoomEntranceConnection()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Flush entrance connection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var protectedPath = Path.Combine(
                ProjectRoot,
                TriRoomEntranceConnectionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomEntranceConnectionProtectedStateName);
            if (!File.Exists(protectedPath))
            {
                throw new InvalidOperationException(
                    "Missing tri-room entrance connection protected state: " + protectedPath);
            }

            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState = BuildTriRoomEntranceConnectionProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus changed after flush entrance source inspection. Apply stopped before saving.");
            }

            var beforeRoomPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal)
            {
                { "EngineRoom", rooms["EngineRoom"].transform.position },
                { "Cockpit", rooms["Cockpit"].transform.position },
                { "ControlRoom", rooms["ControlRoom"].transform.position }
            };
            var beforeRoomRotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal)
            {
                { "EngineRoom", rooms["EngineRoom"].transform.rotation },
                { "Cockpit", rooms["Cockpit"].transform.rotation },
                { "ControlRoom", rooms["ControlRoom"].transform.rotation }
            };
            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus H02 ControlRoom-side buffer gap change");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetCorridor=SC-H02");
            report.AppendLine("PreservedCorridors=SC-H01,SC-H05");
            report.AppendLine("RequiredEntrancePlanePenetration=0");
            report.AppendLine("RequiredControlRoomEndGap=" + Float(H02ControlRoomBufferGap));
            report.AppendLine("ConnectorPiecesCreated=False");
            report.AppendLine();

            try
            {
                RequireTriRoomRoomRotations(rooms);
                CalculateTriRoomEntrancePairMeetingAngles(
                    targetScene,
                    rooms,
                    "EngineRoom",
                    "Cockpit",
                    out var h01FromMeetingAngleBefore,
                    out var h01ToMeetingAngleBefore);
                CalculateTriRoomEntrancePairMeetingAngles(
                    targetScene,
                    rooms,
                    "Cockpit",
                    "ControlRoom",
                    out var h02FromMeetingAngleBefore,
                    out var h02ToMeetingAngleBefore);
                report.AppendLine(
                    "RoomsMoved=False" +
                    "|MaximumMeetingAngle=" + Float(MaximumRoomEntranceMeetingAngle) +
                    "|H01From=" + Float(h01FromMeetingAngleBefore) +
                    "|H01To=" + Float(h01ToMeetingAngleBefore) +
                    "|H02From=" + Float(h02FromMeetingAngleBefore) +
                    "|H02To=" + Float(h02ToMeetingAngleBefore));
                ResizeAndAlignTriRoomCorridorFlush(
                    targetScene,
                    rooms,
                    modules["SC-H02"],
                    ceilingRoot,
                    "SC-H02",
                    "Cockpit",
                    "ControlRoom",
                    report,
                    H02ControlRoomBufferGap);
                RequireTriRoomDiagramOrdering(rooms);
                RequireNoSelectedTriRoomOverlap(rooms);
                RequireNoTriRoomTargetCorridorCrossing(targetScene, rooms, modules);
                RequireCorridorEndOutsideEntrancePlane(
                    modules["SC-H02"],
                    GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        "Cockpit",
                        "ControlRoom"),
                    true,
                    "SC-H02 Cockpit end");

                var afterProtectedState = BuildTriRoomEntranceConnectionProtectedSignature(
                    rooms,
                    corridorRoot.transform,
                    modules,
                    ceilingRoot);
                if (!string.Equals(
                        expectedProtectedState,
                        afterProtectedState,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Flush connection changed a protected room, corridor, ceiling, hierarchy, or component field.");
                }

                report.AppendLine();
                report.AppendLine("RoomOverlap=False");
                report.AppendLine("CorridorCenterlineCrossing=False");
                report.AppendLine("CockpitCorridorFootprintOverlap=False");
                foreach (var roomId in new[] { "EngineRoom", "Cockpit", "ControlRoom" })
                {
                    var after = rooms[roomId].transform.position;
                    var afterRotation = rooms[roomId].transform.rotation;
                    report.AppendLine(
                        "ROOM|" + roomId +
                        "|PositionBefore=" + Vector(beforeRoomPositions[roomId]) +
                        "|PositionAfter=" + Vector(after) +
                        "|Moved=" + !Approximately(beforeRoomPositions[roomId], after) +
                        "|RotationBefore=" + QuaternionText(beforeRoomRotations[roomId]) +
                        "|RotationAfter=" + QuaternionText(afterRotation));
                }

                WriteProjectText(
                    TriRoomEntranceConnectionValidationDirectory,
                    TriRoomEntranceConnectionChangeName,
                    report.ToString());
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save the flush-connected Pegasus scene.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus H02 shortened from the ControlRoom side for the future buffer connector. " +
                    "RoomsMoved=False; TargetCorridor=SC-H02; PreservedCorridors=SC-H01,SC-H05; " +
                    "ControlRoomEndGap=" + Float(H02ControlRoomBufferGap) + "; " +
                    "EntranceOverlap=False; UnityConsoleErrors=0");
            }
            catch (Exception connectionException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw new InvalidOperationException(
                    "Pegasus flush entrance connection failed; unsaved agent changes were discarded and " +
                    "the saved target scene was reopened.",
                    connectionException);
            }
        }

        internal static void CaptureTriRoomEntranceConnectionReview()
        {
            var outputPath = Path.Combine(
                ProjectRoot,
                TriRoomEntranceConnectionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomEntranceConnectionReviewName);
            CaptureTriRoomFlushConnectionReview(outputPath);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room flush entrance direct-review capture completed once. " +
                "Views=OverviewAndThreeTopDownPlusObliquePairs; UnityConsoleErrors=0");
        }

        internal static void InspectTriRoomEntranceConnection()
        {
            var validationDirectory = Path.Combine(
                ProjectRoot,
                TriRoomEntranceConnectionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var protectedPath = Path.Combine(
                validationDirectory,
                TriRoomEntranceConnectionProtectedStateName);
            var sourcePath = Path.Combine(
                validationDirectory,
                TriRoomEntranceConnectionSourceName);
            var reviewPath = Path.Combine(
                validationDirectory,
                TriRoomEntranceConnectionReviewName);
            if (!File.Exists(protectedPath) ||
                !File.Exists(sourcePath) ||
                !File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Flush entrance inspection requires the protected state and direct-review image.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Flush entrance inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState = BuildTriRoomEntranceConnectionProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Protected Pegasus state changed after direct flush-connection review.");
            }

            RequireTriRoomDiagramOrdering(rooms);
            RequireTriRoomRoomRotations(rooms);
            RequireNoSelectedTriRoomOverlap(rooms);
            RequireNoTriRoomTargetCorridorCrossing(targetScene, rooms, modules);
            RequireAllTriRoomCorridorEntranceClearance(targetScene, rooms, modules);
            var sourceCockpitX = ReadReportFloat(
                sourcePath,
                "CockpitSourceX=");
            var sourceCockpitZ = ReadReportFloat(
                sourcePath,
                "CockpitSourceZ=");
            var sourceControlRoomX = ReadReportFloat(
                sourcePath,
                "ControlRoomSourceX=");
            var sourceControlRoomZ = ReadReportFloat(
                sourcePath,
                "ControlRoomSourceZ=");
            var cockpitHorizontalMove = Vector2.Distance(
                new Vector2(sourceCockpitX, sourceCockpitZ),
                Horizontal(rooms["Cockpit"].transform.position));
            var controlRoomHorizontalMove = Vector2.Distance(
                new Vector2(sourceControlRoomX, sourceControlRoomZ),
                Horizontal(rooms["ControlRoom"].transform.position));
            CalculateTriRoomEntrancePairMeetingAngles(
                targetScene,
                rooms,
                "EngineRoom",
                "Cockpit",
                out var h01FromMeetingAngle,
                out var h01ToMeetingAngle);
            CalculateTriRoomEntrancePairMeetingAngles(
                targetScene,
                rooms,
                "Cockpit",
                "ControlRoom",
                out var h02FromMeetingAngle,
                out var h02ToMeetingAngle);
            var h01MeetingAngleDifference =
                Mathf.Abs(h01FromMeetingAngle - h01ToMeetingAngle);
            var h02MeetingAngleDifference =
                Mathf.Abs(h02FromMeetingAngle - h02ToMeetingAngle);
            if (cockpitHorizontalMove > PositionTolerance ||
                controlRoomHorizontalMove > PositionTolerance ||
                h01FromMeetingAngle > MaximumRoomEntranceMeetingAngle + PositionTolerance ||
                h01ToMeetingAngle > MaximumRoomEntranceMeetingAngle + PositionTolerance ||
                h02FromMeetingAngle > MaximumRoomEntranceMeetingAngle + PositionTolerance ||
                h02ToMeetingAngle > MaximumRoomEntranceMeetingAngle + PositionTolerance ||
                h01MeetingAngleDifference > MaximumBalancedMeetingAngleDifference + PositionTolerance ||
                h02MeetingAngleDifference > MaximumBalancedMeetingAngleDifference + PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Tri-room placement changed while creating the H02 buffer gap or failed the approved " +
                    "entrance meeting-angle requirement. " +
                    "CockpitHorizontalMove=" + Float(cockpitHorizontalMove) +
                    "; ControlRoomHorizontalMove=" + Float(controlRoomHorizontalMove) +
                    "; H01From=" + Float(h01FromMeetingAngle) +
                    "; H01To=" + Float(h01ToMeetingAngle) +
                    "; H02From=" + Float(h02FromMeetingAngle) +
                    "; H02To=" + Float(h02ToMeetingAngle));
            }
            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus H02 ControlRoom-side buffer gap inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectVisualReview=PassedBeforeNumericInspection");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("TargetCorridor=SC-H02");
            report.AppendLine("PreservedCorridors=SC-H01,SC-H05");
            report.AppendLine("RequiredEntrancePlanePenetration=0");
            report.AppendLine("RequiredControlRoomEndGap=" + Float(H02ControlRoomBufferGap));
            report.AppendLine("ProtectedStatePreserved=True");
            report.AppendLine("CockpitRotation=Identity");
            report.AppendLine("ControlRoomRotation=Identity");
            report.AppendLine("CockpitHorizontalMove=" + Float(cockpitHorizontalMove));
            report.AppendLine("ControlRoomHorizontalMove=" + Float(controlRoomHorizontalMove));
            report.AppendLine(
                "MaximumRoomEntranceMeetingAngle=" +
                Float(MaximumRoomEntranceMeetingAngle));
            report.AppendLine(
                "MaximumBalancedMeetingAngleDifference=" +
                Float(MaximumBalancedMeetingAngleDifference));
            report.AppendLine("H01EngineMeetingAngle=" + Float(h01FromMeetingAngle));
            report.AppendLine("H01CockpitMeetingAngle=" + Float(h01ToMeetingAngle));
            report.AppendLine("H02CockpitMeetingAngle=" + Float(h02FromMeetingAngle));
            report.AppendLine("H02ControlRoomMeetingAngle=" + Float(h02ToMeetingAngle));
            report.AppendLine("H01MeetingAngleDifference=" + Float(h01MeetingAngleDifference));
            report.AppendLine("H02MeetingAngleDifference=" + Float(h02MeetingAngleDifference));

            AppendTriRoomFlushInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H01"],
                "SC-H01",
                "EngineRoom",
                "Cockpit");
            AppendTriRoomFlushInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H02"],
                "SC-H02",
                "Cockpit",
                "ControlRoom",
                H02ControlRoomBufferGap);
            AppendTriRoomFlushInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H05"],
                "SC-H05",
                "EngineRoom",
                "ControlRoom");

            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            report.AppendLine("RoomOverlap=False");
            report.AppendLine("CorridorCenterlineCrossing=False");
            report.AppendLine("UnityConsoleErrors=0");
            report.AppendLine("Result=PASS");
            WriteProjectText(
                TriRoomEntranceConnectionValidationDirectory,
                TriRoomEntranceConnectionInspectionName,
                report.ToString());
            File.Copy(
                reviewPath,
                Path.Combine(validationDirectory, TriRoomEntranceConnectionFinalName),
                true);
            Debug.Log(
                "Pegasus H02 ControlRoom-side 3m buffer gap inspection passed after direct visual review. " +
                "RoomsMoved=False; PreservedCorridors=SC-H01,SC-H05; EntranceOverlap=False; " +
                "RoomOverlap=False; CorridorCrossing=False; UnityConsoleErrors=0");
        }

        internal static void InspectCockpitOrientationSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit orientation source inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var cockpit = rooms["Cockpit"].transform;
            var visibleCenter = CalculateVisibleBounds(cockpit.gameObject).center;
            var targetRotation = Quaternion.Euler(0f, CockpitRequiredYawDegrees, 0f);
            var report = new StringBuilder(8 * 1024);
            report.AppendLine("Pegasus cockpit orientation source inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetRoot=Cockpit");
            report.AppendLine("RequiredYawDegrees=" + Float(CockpitRequiredYawDegrees));
            report.AppendLine("CurrentPosition=" + Vector(cockpit.position));
            report.AppendLine("CurrentRotation=" + QuaternionText(cockpit.rotation));
            report.AppendLine("CurrentScale=" + Vector(cockpit.localScale));
            report.AppendLine("CurrentVisibleCenter=" + Vector(visibleCenter));
            report.AppendLine("CurrentVisibleCenterX=" + Float(visibleCenter.x));
            report.AppendLine("CurrentVisibleCenterY=" + Float(visibleCenter.y));
            report.AppendLine("CurrentVisibleCenterZ=" + Float(visibleCenter.z));
            report.AppendLine(
                "CurrentRotationErrorDegrees=" +
                Float(Quaternion.Angle(targetRotation, cockpit.rotation)));
            report.AppendLine("OtherRoomsMutable=False");
            report.AppendLine("CorridorsMutable=False");
            report.AppendLine("InteriorRelativeTransformsMutable=False");

            WriteProjectText(
                CockpitOrientationValidationDirectory,
                CockpitOrientationProtectedStateName,
                BuildCockpitOrientationProtectedSignature(rooms, corridorRoot.transform));
            WriteProjectText(
                CockpitOrientationValidationDirectory,
                CockpitOrientationSourceName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus cockpit orientation sources inspected read-only. " +
                "TargetYaw=180; UnityConsoleErrors=0");
        }

        internal static void ApplyCockpitOrientation()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit orientation apply stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var protectedPath = Path.Combine(
                ProjectRoot,
                CockpitOrientationValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                CockpitOrientationProtectedStateName);
            if (!File.Exists(protectedPath))
            {
                throw new InvalidOperationException(
                    "Missing cockpit orientation protected state: " + protectedPath);
            }

            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState =
                BuildCockpitOrientationProtectedSignature(rooms, corridorRoot.transform);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus changed after cockpit orientation source inspection. Apply stopped.");
            }

            var cockpit = rooms["Cockpit"].transform;
            var beforePosition = cockpit.position;
            var beforeRotation = cockpit.rotation;
            var beforeScale = cockpit.localScale;
            var beforeVisibleCenter = CalculateVisibleBounds(cockpit.gameObject).center;
            var targetRotation = Quaternion.Euler(0f, CockpitRequiredYawDegrees, 0f);
            try
            {
                SetRoomWorldRotationAroundVisibleCenter(cockpit, targetRotation);
                var afterVisibleCenter = CalculateVisibleBounds(cockpit.gameObject).center;
                var rotationError = Quaternion.Angle(targetRotation, cockpit.rotation);
                var centerError = Vector3.Distance(beforeVisibleCenter, afterVisibleCenter);
                if (rotationError > RotationTolerance ||
                    centerError > PositionTolerance ||
                    !Approximately(beforeScale, cockpit.localScale))
                {
                    throw new InvalidOperationException(
                        "Cockpit orientation restoration failed. RotationError=" +
                        Float(rotationError) + "; VisibleCenterError=" + Float(centerError));
                }

                var afterProtectedState =
                    BuildCockpitOrientationProtectedSignature(rooms, corridorRoot.transform);
                if (!string.Equals(
                        expectedProtectedState,
                        afterProtectedState,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Cockpit orientation restoration changed a protected room, corridor, " +
                        "interior transform, hierarchy, or component state.");
                }

                var report = new StringBuilder(8 * 1024);
                report.AppendLine("Pegasus cockpit orientation restoration change");
                report.AppendLine("TargetScene=" + TargetScenePath);
                report.AppendLine("TargetRoot=Cockpit");
                report.AppendLine("RequiredYawDegrees=" + Float(CockpitRequiredYawDegrees));
                report.AppendLine("PositionBefore=" + Vector(beforePosition));
                report.AppendLine("PositionAfter=" + Vector(cockpit.position));
                report.AppendLine("RotationBefore=" + QuaternionText(beforeRotation));
                report.AppendLine("RotationAfter=" + QuaternionText(cockpit.rotation));
                report.AppendLine("ScalePreserved=True");
                report.AppendLine("VisibleCenterBefore=" + Vector(beforeVisibleCenter));
                report.AppendLine("VisibleCenterAfter=" + Vector(afterVisibleCenter));
                report.AppendLine("VisibleCenterError=" + Float(centerError));
                report.AppendLine("OtherRoomsChanged=False");
                report.AppendLine("CorridorsChanged=False");
                report.AppendLine("InteriorRelativeTransformsChanged=False");
                WriteProjectText(
                    CockpitOrientationValidationDirectory,
                    CockpitOrientationChangeName,
                    report.ToString());

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save the restored Pegasus cockpit orientation.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus cockpit orientation restored to 180 degrees around its visible center. " +
                    "OtherRoomsChanged=False; CorridorsChanged=False; UnityConsoleErrors=0");
            }
            catch (Exception orientationException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw new InvalidOperationException(
                    "Pegasus cockpit orientation restoration failed; unsaved agent changes were " +
                    "discarded and the saved target scene was reopened.",
                    orientationException);
            }
        }

        internal static void CaptureCockpitOrientationReview()
        {
            var outputPath = Path.Combine(
                ProjectRoot,
                CockpitOrientationValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                CockpitOrientationReviewName);
            CaptureCockpitOrientationReviewSheet(outputPath);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus cockpit orientation direct-review capture completed once. " +
                "Views=TopDownAndOblique; UnityConsoleErrors=0");
        }

        internal static void InspectCockpitOrientation()
        {
            var validationDirectory = Path.Combine(
                ProjectRoot,
                CockpitOrientationValidationDirectory.Replace('/', Path.DirectorySeparatorChar));
            var protectedPath = Path.Combine(
                validationDirectory,
                CockpitOrientationProtectedStateName);
            var sourcePath = Path.Combine(validationDirectory, CockpitOrientationSourceName);
            var reviewPath = Path.Combine(validationDirectory, CockpitOrientationReviewName);
            if (!File.Exists(protectedPath) ||
                !File.Exists(sourcePath) ||
                !File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Cockpit orientation inspection requires source, protected state, and direct review.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit orientation inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState =
                BuildCockpitOrientationProtectedSignature(rooms, corridorRoot.transform);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Protected Pegasus state changed after cockpit orientation direct review.");
            }

            var cockpit = rooms["Cockpit"].transform;
            var targetRotation = Quaternion.Euler(0f, CockpitRequiredYawDegrees, 0f);
            var rotationError = Quaternion.Angle(targetRotation, cockpit.rotation);
            var sourceVisibleCenter = new Vector3(
                ReadReportFloat(sourcePath, "CurrentVisibleCenterX="),
                ReadReportFloat(sourcePath, "CurrentVisibleCenterY="),
                ReadReportFloat(sourcePath, "CurrentVisibleCenterZ="));
            var currentVisibleCenter = CalculateVisibleBounds(cockpit.gameObject).center;
            var centerError = Vector3.Distance(sourceVisibleCenter, currentVisibleCenter);
            if (rotationError > RotationTolerance || centerError > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Cockpit orientation inspection failed. RotationError=" +
                    Float(rotationError) + "; VisibleCenterError=" + Float(centerError));
            }

            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            var report = new StringBuilder(8 * 1024);
            report.AppendLine("Pegasus cockpit orientation inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectVisualReview=PassedBeforeNumericInspection");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("TargetRoot=Cockpit");
            report.AppendLine("RequiredYawDegrees=" + Float(CockpitRequiredYawDegrees));
            report.AppendLine("Rotation=" + QuaternionText(cockpit.rotation));
            report.AppendLine("RotationErrorDegrees=" + Float(rotationError));
            report.AppendLine("VisibleCenterSource=" + Vector(sourceVisibleCenter));
            report.AppendLine("VisibleCenterCurrent=" + Vector(currentVisibleCenter));
            report.AppendLine("VisibleCenterError=" + Float(centerError));
            report.AppendLine("OtherRoomsChanged=False");
            report.AppendLine("CorridorsChanged=False");
            report.AppendLine("InteriorRelativeTransformsChanged=False");
            report.AppendLine("UnityConsoleErrors=0");
            report.AppendLine("Result=PASS");
            WriteProjectText(
                CockpitOrientationValidationDirectory,
                CockpitOrientationInspectionName,
                report.ToString());
            File.Copy(
                reviewPath,
                Path.Combine(validationDirectory, CockpitOrientationFinalName),
                true);
            Debug.Log(
                "Pegasus cockpit orientation inspection passed after direct visual review. " +
                "RequiredYaw=180; VisibleCenterPreserved=True; OtherRoomsChanged=False; " +
                "CorridorsChanged=False; UnityConsoleErrors=0");
        }

        internal static void InspectCockpitCorridorConnectionSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit corridor source inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            RequireTriRoomRoomRotations(rooms);
            var report = new StringBuilder(24 * 1024);
            report.AppendLine("Pegasus cockpit H01/H02 connection source inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetCorridors=SC-H01,SC-H02,SC-H05");
            report.AppendLine("PreservedCorridors=SC-H03,SC-H04,SC-S01,SC-S02,SC-S03,SC-S04,SC-S05");
            report.AppendLine("RoomsMutable=EngineRoomControlRoomXZOnly");
            report.AppendLine("CockpitMutable=False");
            report.AppendLine("CockpitRequiredYawDegrees=" + Float(CockpitRequiredYawDegrees));
            report.AppendLine("H02ControlRoomPlaneGap=" + Float(H02ControlRoomBufferGap));
            report.AppendLine();
            AppendRoomRoot(report, "EngineRoom", rooms["EngineRoom"].transform);
            AppendRoomRoot(report, "Cockpit", rooms["Cockpit"].transform);
            AppendRoomRoot(report, "ControlRoom", rooms["ControlRoom"].transform);
            report.AppendLine();
            AppendTriRoomFlushSource(
                report,
                targetScene,
                rooms,
                modules["SC-H01"],
                "SC-H01",
                "EngineRoom",
                "Cockpit");
            AppendTriRoomFlushSource(
                report,
                targetScene,
                rooms,
                modules["SC-H02"],
                "SC-H02",
                "Cockpit",
                "ControlRoom");
            AppendTriRoomFlushSource(
                report,
                targetScene,
                rooms,
                modules["SC-H05"],
                "SC-H05",
                "EngineRoom",
                "ControlRoom");
            CalculateTriRoomEntrancePairMeetingAngles(
                targetScene,
                rooms,
                "EngineRoom",
                "Cockpit",
                out var h01FromAngle,
                out var h01ToAngle);
            CalculateTriRoomEntrancePairMeetingAngles(
                targetScene,
                rooms,
                "Cockpit",
                "ControlRoom",
                out var h02FromAngle,
                out var h02ToAngle);
            report.AppendLine("H01PlannedEngineMeetingAngle=" + Float(h01FromAngle));
            report.AppendLine("H01PlannedCockpitMeetingAngle=" + Float(h01ToAngle));
            report.AppendLine("H02PlannedCockpitMeetingAngle=" + Float(h02FromAngle));
            report.AppendLine("H02PlannedControlRoomMeetingAngle=" + Float(h02ToAngle));
            report.AppendLine(
                "H01TranslationOnlyMaximumMeetingAngle=" +
                Float(CockpitH01MaximumMeetingAngle));
            report.AppendLine(
                "H02MaximumMeetingAngle=" + Float(CockpitH02MaximumMeetingAngle));
            var currentH01Length = GetCorridorWorldVisibleLength(modules["SC-H01"]);
            var currentH02Length = GetCorridorWorldVisibleLength(modules["SC-H02"]);
            var combinedLength = currentH01Length + currentH02Length;
            var combinedWeight =
                CockpitCorridorH01LengthWeight + CockpitCorridorH02LengthWeight;
            report.AppendLine("CurrentH01VisibleLength=" + Float(currentH01Length));
            report.AppendLine("CurrentH02VisibleLength=" + Float(currentH02Length));
            report.AppendLine("PreservedCombinedVisibleLength=" + Float(combinedLength));
            report.AppendLine(
                "TargetH01VisibleLength=" +
                Float(combinedLength * CockpitCorridorH01LengthWeight / combinedWeight));
            report.AppendLine(
                "TargetH02VisibleLength=" +
                Float(combinedLength * CockpitCorridorH02LengthWeight / combinedWeight));
            report.AppendLine("TargetVisibleLengthRatio=4:6");

            WriteProjectText(
                CockpitCorridorConnectionValidationDirectory,
                CockpitCorridorConnectionProtectedStateName,
                BuildCockpitCorridorConnectionProtectedSignature(
                    rooms,
                    corridorRoot.transform,
                    modules,
                    ceilingRoot));
            WriteProjectText(
                CockpitCorridorConnectionValidationDirectory,
                CockpitCorridorConnectionSourceName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus cockpit H01/H02 connection sources inspected read-only. " +
                "CockpitYaw=180; RoomsMutable=EngineRoomControlRoomXZOnly; " +
                "UnityConsoleErrors=0");
        }

        internal static void ApplyCockpitCorridorConnection()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit corridor connection apply stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var protectedPath = Path.Combine(
                ProjectRoot,
                CockpitCorridorConnectionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                CockpitCorridorConnectionProtectedStateName);
            if (!File.Exists(protectedPath))
            {
                throw new InvalidOperationException(
                    "Missing cockpit corridor protected state: " + protectedPath);
            }

            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState = BuildCockpitCorridorConnectionProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus changed after cockpit corridor source inspection. Apply stopped.");
            }

            var beforeRoomPositions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            var beforeRoomRotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
            foreach (var roomId in new[] { "EngineRoom", "Cockpit", "ControlRoom" })
            {
                beforeRoomPositions.Add(roomId, rooms[roomId].transform.position);
                beforeRoomRotations.Add(roomId, rooms[roomId].transform.rotation);
            }

            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus cockpit H01/H02 connection change");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetCorridors=SC-H01,SC-H02,SC-H05");
            report.AppendLine("RoomsMoved=EngineRoomControlRoomXZOnly");
            report.AppendLine("CockpitYawDegrees=" + Float(CockpitRequiredYawDegrees));
            report.AppendLine("H02ControlRoomPlaneGap=" + Float(H02ControlRoomBufferGap));
            report.AppendLine("ConnectorPiecesCreated=False");
            report.AppendLine();
            try
            {
                RequireTriRoomRoomRotations(rooms);
                RedistributeCockpitCorridorLengths(
                    rooms,
                    modules,
                    out var engineRoomMove,
                    out var controlRoomMove,
                    out var combinedLengthBefore,
                    out var targetH01Length,
                    out var targetH02Length);
                report.AppendLine("EngineRoomHorizontalMove=" + Vector(engineRoomMove));
                report.AppendLine("ControlRoomHorizontalMove=" + Vector(controlRoomMove));
                report.AppendLine(
                    "PreservedCombinedVisibleLength=" + Float(combinedLengthBefore));
                report.AppendLine("TargetH01VisibleLength=" + Float(targetH01Length));
                report.AppendLine("TargetH02VisibleLength=" + Float(targetH02Length));
                ResizeAndAlignTriRoomCorridorFlush(
                    targetScene,
                    rooms,
                    modules["SC-H01"],
                    ceilingRoot,
                    "SC-H01",
                    "EngineRoom",
                    "Cockpit",
                    report);
                ResizeAndAlignTriRoomCorridorFlush(
                    targetScene,
                    rooms,
                    modules["SC-H02"],
                    ceilingRoot,
                    "SC-H02",
                    "Cockpit",
                    "ControlRoom",
                    report,
                    H02ControlRoomBufferGap);
                ResizeAndAlignTriRoomCorridorFlush(
                    targetScene,
                    rooms,
                    modules["SC-H05"],
                    ceilingRoot,
                    "SC-H05",
                    "EngineRoom",
                    "ControlRoom",
                    report);
                RequireNoSelectedTriRoomOverlap(rooms);
                RequireNoTriRoomTargetCorridorCrossing(targetScene, rooms, modules);
                RequireAllTriRoomCorridorEntranceClearance(targetScene, rooms, modules);
                CalculateTriRoomEntrancePairMeetingAngles(
                    targetScene,
                    rooms,
                    "EngineRoom",
                    "Cockpit",
                    out var h01FromAngle,
                    out var h01ToAngle);
                CalculateTriRoomEntrancePairMeetingAngles(
                    targetScene,
                    rooms,
                    "Cockpit",
                    "ControlRoom",
                    out var h02FromAngle,
                    out var h02ToAngle);
                var h01ActualFromAngle = CalculateCorridorEntranceMeetingAngle(
                    modules["SC-H01"],
                    GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        "EngineRoom",
                        "Cockpit"),
                    true);
                var h01ActualToAngle = CalculateCorridorEntranceMeetingAngle(
                    modules["SC-H01"],
                    GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        "Cockpit",
                        "EngineRoom"),
                    false);
                var h02ActualFromAngle = CalculateCorridorEntranceMeetingAngle(
                    modules["SC-H02"],
                    GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        "Cockpit",
                        "ControlRoom"),
                    true);
                var h02ActualToAngle = CalculateCorridorEntranceMeetingAngle(
                    modules["SC-H02"],
                    GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        "ControlRoom",
                        "Cockpit"),
                    false);
                var h01VisibleLength = GetCorridorWorldVisibleLength(modules["SC-H01"]);
                var h02VisibleLength = GetCorridorWorldVisibleLength(modules["SC-H02"]);
                var combinedLengthAfter = h01VisibleLength + h02VisibleLength;
                if (Mathf.Abs(h01VisibleLength - targetH01Length) >
                        CockpitCorridorLengthTolerance ||
                    Mathf.Abs(h02VisibleLength - targetH02Length) >
                        CockpitCorridorLengthTolerance ||
                    Mathf.Abs(
                        combinedLengthBefore -
                        CockpitCorridorCombinedLengthReference) >
                        CockpitCorridorLengthTolerance ||
                    Mathf.Abs(combinedLengthAfter - combinedLengthBefore) >
                        CockpitCorridorLengthTolerance ||
                    Mathf.Abs(
                        h01ActualFromAngle - CockpitH01ReferenceMeetingAngle) >
                        CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                    Mathf.Abs(
                        h01ActualToAngle - CockpitH01ReferenceMeetingAngle) >
                        CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                    Mathf.Abs(
                        h02ActualFromAngle - CockpitH02ReferenceMeetingAngle) >
                        CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                    Mathf.Abs(
                        h02ActualToAngle - CockpitH02ReferenceMeetingAngle) >
                        CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                    Mathf.Abs(h01ActualFromAngle - h01ActualToAngle) >
                        CockpitBalancedMeetingAngleTolerance + PositionTolerance ||
                    Mathf.Abs(h02ActualFromAngle - h02ActualToAngle) >
                        CockpitBalancedMeetingAngleTolerance + PositionTolerance)
                {
                    throw new InvalidOperationException(
                        "Cockpit corridor redistribution did not reach the approved 4:6 length and angle limits. " +
                        "Length=" + Float(h01VisibleLength) + "/" +
                            Float(h02VisibleLength) +
                        "; Combined=" + Float(combinedLengthAfter) +
                        "H01Actual=" + Float(h01ActualFromAngle) + "/" +
                            Float(h01ActualToAngle) +
                        "; H02Actual=" + Float(h02ActualFromAngle) + "/" +
                            Float(h02ActualToAngle));
                }

                report.AppendLine("H01EngineMeetingAngle=" + Float(h01FromAngle));
                report.AppendLine("H01CockpitMeetingAngle=" + Float(h01ToAngle));
                report.AppendLine("H02CockpitMeetingAngle=" + Float(h02FromAngle));
                report.AppendLine("H02ControlRoomMeetingAngle=" + Float(h02ToAngle));
                report.AppendLine("H01ActualEngineMeetingAngle=" + Float(h01ActualFromAngle));
                report.AppendLine("H01ActualCockpitMeetingAngle=" + Float(h01ActualToAngle));
                report.AppendLine("H02ActualCockpitMeetingAngle=" + Float(h02ActualFromAngle));
                report.AppendLine("H02ActualControlRoomMeetingAngle=" + Float(h02ActualToAngle));
                report.AppendLine("H01VisibleLength=" + Float(h01VisibleLength));
                report.AppendLine("H02VisibleLength=" + Float(h02VisibleLength));
                report.AppendLine("CombinedVisibleLength=" + Float(combinedLengthAfter));
                report.AppendLine(
                    "VisibleLengthRatio=" +
                    Float(h01VisibleLength / h02VisibleLength) + " (4:6)");
                foreach (var roomId in new[] { "EngineRoom", "Cockpit", "ControlRoom" })
                {
                    var room = rooms[roomId].transform;
                    var positionChanged = !Approximately(beforeRoomPositions[roomId], room.position);
                    var rotationChanged =
                        Quaternion.Angle(beforeRoomRotations[roomId], room.rotation) >
                        RotationTolerance;
                    if ((roomId == "Cockpit" && positionChanged) ||
                        rotationChanged ||
                        Mathf.Abs(beforeRoomPositions[roomId].y - room.position.y) >
                            PositionTolerance)
                    {
                        throw new InvalidOperationException(
                            roomId +
                            " changed outside the approved EngineRoom/ControlRoom XZ translation.");
                    }

                    report.AppendLine(
                        "ROOM|" + roomId +
                        "|Position=" + Vector(room.position) +
                        "|Rotation=" + QuaternionText(room.rotation) +
                        "|Changed=" + (positionChanged ? "XZTranslation" : "False"));
                }

                var afterProtectedState = BuildCockpitCorridorConnectionProtectedSignature(
                    rooms,
                    corridorRoot.transform,
                    modules,
                    ceilingRoot);
                if (!string.Equals(
                        expectedProtectedState,
                        afterProtectedState,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Cockpit corridor connection changed a protected room, corridor, ceiling, " +
                        "hierarchy, or component state.");
                }

                WriteProjectText(
                    CockpitCorridorConnectionValidationDirectory,
                    CockpitCorridorConnectionChangeName,
                    report.ToString());
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save the Pegasus cockpit corridor connection.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus H01/H02 redistributed to the approved 4:6 visible-length ratio. " +
                    "RoomsMoved=EngineRoomControlRoomXZOnly; H05Realigned=True; " +
                    "H02ControlRoomGap=3; UnityConsoleErrors=0");
            }
            catch (Exception connectionException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw new InvalidOperationException(
                    "Pegasus cockpit corridor connection failed; unsaved agent changes were discarded " +
                    "and the saved target scene was reopened.",
                    connectionException);
            }
        }

        internal static void CaptureCockpitCorridorConnectionReview()
        {
            var outputPath = Path.Combine(
                ProjectRoot,
                CockpitCorridorConnectionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                CockpitCorridorConnectionReviewName);
            CaptureTriRoomFlushConnectionReview(outputPath);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus cockpit H01/H02/H05 direct-review capture completed once. " +
                "Views=OverviewAndEntranceCloseups; UnityConsoleErrors=0");
        }

        internal static void InspectCockpitCorridorConnection()
        {
            var validationDirectory = Path.Combine(
                ProjectRoot,
                CockpitCorridorConnectionValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            var protectedPath = Path.Combine(
                validationDirectory,
                CockpitCorridorConnectionProtectedStateName);
            var reviewPath = Path.Combine(
                validationDirectory,
                CockpitCorridorConnectionReviewName);
            if (!File.Exists(protectedPath) || !File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Cockpit corridor inspection requires protected state and direct review.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit corridor inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState = BuildCockpitCorridorConnectionProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Protected Pegasus state changed after cockpit corridor direct review.");
            }

            RequireTriRoomRoomRotations(rooms);
            RequireNoSelectedTriRoomOverlap(rooms);
            RequireNoTriRoomTargetCorridorCrossing(targetScene, rooms, modules);
            RequireAllTriRoomCorridorEntranceClearance(targetScene, rooms, modules);
            CalculateTriRoomEntrancePairMeetingAngles(
                targetScene,
                rooms,
                "EngineRoom",
                "Cockpit",
                out var h01FromAngle,
                out var h01ToAngle);
            CalculateTriRoomEntrancePairMeetingAngles(
                targetScene,
                rooms,
                "Cockpit",
                "ControlRoom",
                out var h02FromAngle,
                out var h02ToAngle);
            var h01ActualFromAngle = CalculateCorridorEntranceMeetingAngle(
                modules["SC-H01"],
                GetTriRoomFlushEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                true);
            var h01ActualToAngle = CalculateCorridorEntranceMeetingAngle(
                modules["SC-H01"],
                GetTriRoomFlushEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"),
                false);
            var h02ActualFromAngle = CalculateCorridorEntranceMeetingAngle(
                modules["SC-H02"],
                GetTriRoomFlushEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                true);
            var h02ActualToAngle = CalculateCorridorEntranceMeetingAngle(
                modules["SC-H02"],
                GetTriRoomFlushEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"),
                false);
            var h01VisibleLength = GetCorridorWorldVisibleLength(modules["SC-H01"]);
            var h02VisibleLength = GetCorridorWorldVisibleLength(modules["SC-H02"]);
            var combinedVisibleLength = h01VisibleLength + h02VisibleLength;
            var targetH01Length =
                CockpitCorridorCombinedLengthReference *
                CockpitCorridorH01LengthWeight /
                (CockpitCorridorH01LengthWeight + CockpitCorridorH02LengthWeight);
            var targetH02Length =
                CockpitCorridorCombinedLengthReference *
                CockpitCorridorH02LengthWeight /
                (CockpitCorridorH01LengthWeight + CockpitCorridorH02LengthWeight);
            if (Mathf.Abs(h01VisibleLength - targetH01Length) >
                    CockpitCorridorLengthTolerance ||
                Mathf.Abs(h02VisibleLength - targetH02Length) >
                    CockpitCorridorLengthTolerance ||
                Mathf.Abs(
                    combinedVisibleLength -
                    CockpitCorridorCombinedLengthReference) >
                    CockpitCorridorLengthTolerance ||
                Mathf.Abs(
                    h01ActualFromAngle - CockpitH01ReferenceMeetingAngle) >
                    CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                Mathf.Abs(
                    h01ActualToAngle - CockpitH01ReferenceMeetingAngle) >
                    CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                Mathf.Abs(
                    h02ActualFromAngle - CockpitH02ReferenceMeetingAngle) >
                    CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                Mathf.Abs(
                    h02ActualToAngle - CockpitH02ReferenceMeetingAngle) >
                    CockpitCorridorMeetingAngleDeviation + PositionTolerance ||
                Mathf.Abs(h01ActualFromAngle - h01ActualToAngle) >
                    CockpitBalancedMeetingAngleTolerance + PositionTolerance ||
                Mathf.Abs(h02ActualFromAngle - h02ActualToAngle) >
                    CockpitBalancedMeetingAngleTolerance + PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Cockpit corridor 4:6 length or meeting-angle inspection failed. Length=" +
                    Float(h01VisibleLength) + "/" + Float(h02VisibleLength) +
                    "; H01=" +
                    Float(h01ActualFromAngle) + "/" + Float(h01ActualToAngle) +
                    "; H02=" + Float(h02ActualFromAngle) + "/" + Float(h02ActualToAngle));
            }

            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus cockpit H01/H02 4:6 redistribution inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectVisualReview=PassedBeforeNumericInspection");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("TargetCorridors=SC-H01,SC-H02,SC-H05");
            report.AppendLine("CockpitYawDegrees=" + Float(CockpitRequiredYawDegrees));
            report.AppendLine("RoomsMoved=EngineRoomControlRoomXZOnly");
            report.AppendLine("H01EngineMeetingAngle=" + Float(h01FromAngle));
            report.AppendLine("H01CockpitMeetingAngle=" + Float(h01ToAngle));
            report.AppendLine("H02CockpitMeetingAngle=" + Float(h02FromAngle));
            report.AppendLine("H02ControlRoomMeetingAngle=" + Float(h02ToAngle));
            report.AppendLine("H01ActualEngineMeetingAngle=" + Float(h01ActualFromAngle));
            report.AppendLine("H01ActualCockpitMeetingAngle=" + Float(h01ActualToAngle));
            report.AppendLine("H02ActualCockpitMeetingAngle=" + Float(h02ActualFromAngle));
            report.AppendLine("H02ActualControlRoomMeetingAngle=" + Float(h02ActualToAngle));
            report.AppendLine("H01VisibleLength=" + Float(h01VisibleLength));
            report.AppendLine("H02VisibleLength=" + Float(h02VisibleLength));
            report.AppendLine("CombinedVisibleLength=" + Float(combinedVisibleLength));
            report.AppendLine(
                "VisibleLengthRatio=" +
                Float(h01VisibleLength / h02VisibleLength) + " (4:6)");
            AppendTriRoomFlushInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H01"],
                "SC-H01",
                "EngineRoom",
                "Cockpit",
                0f,
                CockpitH01ReferenceMeetingAngle +
                    CockpitCorridorMeetingAngleDeviation);
            AppendTriRoomFlushInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H02"],
                "SC-H02",
                "Cockpit",
                "ControlRoom",
                H02ControlRoomBufferGap,
                CockpitH02ReferenceMeetingAngle +
                    CockpitCorridorMeetingAngleDeviation);
            AppendTriRoomFlushInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H05"],
                "SC-H05",
                "EngineRoom",
                "ControlRoom");
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            report.AppendLine("ProtectedStatePreserved=True");
            report.AppendLine("OtherCorridorsChanged=False");
            report.AppendLine("UnityConsoleErrors=0");
            report.AppendLine("Result=PASS");
            WriteProjectText(
                CockpitCorridorConnectionValidationDirectory,
                CockpitCorridorConnectionInspectionName,
                report.ToString());
            File.Copy(
                reviewPath,
                Path.Combine(validationDirectory, CockpitCorridorConnectionFinalName),
                true);
            Debug.Log(
                "Pegasus cockpit H01/H02 4:6 redistribution inspection passed after direct review. " +
                "CockpitYaw=180; RoomsMoved=EngineRoomControlRoomXZOnly; " +
                "H05Realigned=True; H02ControlRoomGap=3; UnityConsoleErrors=0");
        }

        internal static void InspectTriRoomUniformGapSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Uniform-gap source inspection will not discard them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var placement = SolveTriRoomUniformPlacement(targetScene, rooms, modules);
            var report = new StringBuilder(32 * 1024);
            report.AppendLine("Pegasus tri-room uniform one-meter gap source inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("TargetRooms=EngineRoom,Cockpit,ControlRoom");
            report.AppendLine("TargetCorridors=SC-H01,SC-H02,SC-H05");
            report.AppendLine("TargetGap=" + Float(TriRoomUniformConnectorGap));
            report.AppendLine("TargetGapTolerance=" + Float(TriRoomUniformConnectorGapTolerance));
            report.AppendLine("CreateConnectorPieces=False");
            report.AppendLine("ChangeCorridorGeometry=False");
            report.AppendLine("ChangeRoomInternalTransforms=False");
            report.AppendLine();
            report.AppendLine("[CurrentRoomRoots]");
            AppendRoomRoot(report, "EngineRoom", rooms["EngineRoom"].transform);
            AppendRoomRoot(report, "Cockpit", rooms["Cockpit"].transform);
            AppendRoomRoot(report, "ControlRoom", rooms["ControlRoom"].transform);
            report.AppendLine();
            report.AppendLine("[SolvedRoomRoots]");
            report.AppendLine("EngineRoom=" + Vector(placement.EngineRoomPosition));
            report.AppendLine("Cockpit=" + Vector(placement.CockpitPosition));
            report.AppendLine("ControlRoom=" + Vector(placement.ControlRoomPosition));
            report.AppendLine("TargetAnchorDistance=" + Float(placement.TargetAnchorDistance));
            report.AppendLine("PlacementScore=" + Float(placement.Score));

            WriteProjectText(
                TriRoomUniformGapLogDirectory,
                TriRoomUniformGapProtectedSignatureName,
                BuildTriRoomUniformGapProtectedSignature(rooms, corridorRoot.transform, modules));
            WriteProjectText(
                TriRoomUniformGapLogDirectory,
                TriRoomUniformGapSourceReportName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room uniform-gap sources inspected read-only. " +
                "TargetGap=1m; TargetRooms=3; TargetCorridors=3; UnityConsoleErrors=0");
        }

        internal static void RepositionTriRoomForUniformGaps()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Uniform-gap placement will not overwrite them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var protectedSignaturePath = Path.Combine(
                ProjectRoot,
                TriRoomUniformGapLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomUniformGapProtectedSignatureName);
            if (!File.Exists(protectedSignaturePath))
            {
                throw new InvalidOperationException(
                    "Missing uniform-gap protected signature: " + protectedSignaturePath);
            }

            var originalProtectedSignature = File.ReadAllText(protectedSignaturePath, Encoding.UTF8);
            var currentProtectedSignature = BuildTriRoomUniformGapProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules);
            if (!string.Equals(
                    originalProtectedSignature,
                    currentProtectedSignature,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus changed after uniform-gap source inspection. Placement stopped before saving.");
            }

            try
            {
                var placement = SolveTriRoomUniformPlacement(targetScene, rooms, modules);
                SetRoomHorizontalPosition(rooms["EngineRoom"].transform, placement.EngineRoomPosition);
                SetRoomHorizontalPosition(rooms["Cockpit"].transform, placement.CockpitPosition);
                SetRoomHorizontalPosition(rooms["ControlRoom"].transform, placement.ControlRoomPosition);

                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H01"],
                    ceilingRoot,
                    "SC-H01",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"));
                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H02"],
                    ceilingRoot,
                    "SC-H02",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"));
                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H05"],
                    ceilingRoot,
                    "SC-H05",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "ControlRoom"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "EngineRoom"));

                RequireTriRoomDiagramOrdering(rooms);
                RequireNoTriRoomRoomOverlap(rooms);
                var postPlacementSignature = BuildTriRoomUniformGapProtectedSignature(
                    rooms,
                    corridorRoot.transform,
                    modules);
                if (!string.Equals(
                        originalProtectedSignature,
                        postPlacementSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Uniform-gap placement changed protected room, corridor, ceiling, hierarchy, or component state.");
                }

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException("Failed to save uniform-gap Pegasus scene.");
                }

                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus tri-room rooms and corridors repositioned for uniform one-meter gaps. " +
                    "RoomsMoved=True; TargetCorridors=SC-H01,SC-H02,SC-H05; " +
                    "ConnectorPiecesCreated=False; UnityConsoleErrors=0");
            }
            catch (Exception placementException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw new InvalidOperationException(
                    "Pegasus uniform-gap placement failed; the saved target scene was reopened without saving partial changes.",
                    placementException);
            }
        }

        internal static void CaptureTriRoomUniformGapFinal()
        {
            var inspectionPath = Path.Combine(
                ProjectRoot,
                TriRoomUniformGapValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomUniformGapInspectionName);
            var reviewPath = Path.Combine(
                ProjectRoot,
                TriRoomUniformGapLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomUniformGapReviewName);
            if (!File.Exists(inspectionPath))
            {
                CaptureTriRoomCorridorConnectionReview(reviewPath);
                return;
            }

            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Missing directly reviewed uniform-gap image: " + reviewPath);
            }

            var finalDirectory = Path.Combine(
                ProjectRoot,
                TriRoomUniformGapValidationDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(finalDirectory);
            var finalPath = Path.Combine(finalDirectory, TriRoomUniformGapFinalName);
            File.Copy(reviewPath, finalPath, true);
            Debug.Log(
                "Verified Pegasus tri-room uniform-gap review promoted to final evidence without recapturing. " +
                "Output=" + finalPath);
        }

        internal static void InspectTriRoomUniformGap()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Uniform-gap inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var protectedSignaturePath = Path.Combine(
                ProjectRoot,
                TriRoomUniformGapLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomUniformGapProtectedSignatureName);
            var reviewPath = Path.Combine(
                ProjectRoot,
                TriRoomUniformGapLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                TriRoomUniformGapReviewName);
            if (!File.Exists(protectedSignaturePath) || !File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Uniform-gap inspection requires the protected signature and directly reviewed image.");
            }

            var originalProtectedSignature = File.ReadAllText(protectedSignaturePath, Encoding.UTF8);
            var currentProtectedSignature = BuildTriRoomUniformGapProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules);
            if (!string.Equals(
                    originalProtectedSignature,
                    currentProtectedSignature,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Protected Pegasus state differs after uniform-gap placement.");
            }

            RequireTriRoomDiagramOrdering(rooms);
            RequireNoTriRoomRoomOverlap(rooms);
            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus tri-room uniform one-meter gap inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("RoomsMoved=True");
            report.AppendLine("ConnectorPiecesCreated=False");
            report.AppendLine("TargetCorridors=SC-H01,SC-H02,SC-H05");
            report.AppendLine("TargetGap=" + Float(TriRoomUniformConnectorGap));
            report.AppendLine("GapTolerance=" + Float(TriRoomUniformConnectorGapTolerance));
            report.AppendLine("UntargetedRoomsAndCorridorsChanged=False");
            report.AppendLine("PersistentSceneRoots=7");

            var gaps = new List<float>(6);
            AppendTriRoomUniformGapInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H01"],
                "SC-H01",
                GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"),
                gaps);
            AppendTriRoomUniformGapInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H02"],
                "SC-H02",
                GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"),
                gaps);
            AppendTriRoomUniformGapInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H05"],
                "SC-H05",
                GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "ControlRoom"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "EngineRoom"),
                gaps);

            var minimumGap = float.PositiveInfinity;
            var maximumGap = float.NegativeInfinity;
            for (var gapIndex = 0; gapIndex < gaps.Count; gapIndex++)
            {
                minimumGap = Mathf.Min(minimumGap, gaps[gapIndex]);
                maximumGap = Mathf.Max(maximumGap, gaps[gapIndex]);
            }

            var spread = maximumGap - minimumGap;
            if (spread > TriRoomUniformConnectorGapTolerance)
            {
                throw new InvalidOperationException(
                    "The six connector gaps are not uniform. Min=" + Float(minimumGap) +
                    "; Max=" + Float(maximumGap) + "; Spread=" + Float(spread));
            }

            report.AppendLine("MinimumGap=" + Float(minimumGap));
            report.AppendLine("MaximumGap=" + Float(maximumGap));
            report.AppendLine("GapSpread=" + Float(spread));
            report.AppendLine("RoomOverlap=False");
            report.AppendLine("UnityConsoleErrors=0");
            WriteProjectText(
                TriRoomUniformGapValidationDirectory,
                TriRoomUniformGapInspectionName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room uniform one-meter gaps inspected after direct review. " +
                "SixGapsWithinTolerance=True; GapSpreadWithinTolerance=True; " +
                "RoomOverlap=False; UnityConsoleErrors=0");
        }

        internal static void ApplyEngineControlAlignmentAndSixSecondCorridor()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Engine-control alignment will not overwrite them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var settings = AssetDatabase.LoadAssetAtPath<
                Bellerophon.Core.Player.FirstPersonPlayerSettings>(PlayerSettingsPath) ??
                throw new InvalidOperationException(
                    "Missing canonical player settings at " + PlayerSettingsPath);
            var targetLocalLength = settings.WalkSpeed * EngineControlCorridorTravelSeconds;
            var protectedSignature = BuildEngineControlAlignmentProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules,
                ceilingRoot);

            try
            {
                var module = modules["SC-H05"];
                ResizeHorizontalCorridorModule(module, ceilingRoot, targetLocalLength);
                var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
                var worldScaleAlongLength = module.TransformVector(Vector3.right).magnitude;
                var targetWorldLength = localBounds.size.x * worldScaleAlongLength;
                var targetAnchorDistance =
                    targetWorldLength + (TriRoomUniformConnectorGap * 2f);
                var engineAnchor = GetTriRoomEntranceAnchor(
                    targetScene,
                    rooms,
                    "EngineRoom",
                    "ControlRoom");
                var controlAnchor = GetTriRoomEntranceAnchor(
                    targetScene,
                    rooms,
                    "ControlRoom",
                    "EngineRoom");
                var horizontalSign = controlAnchor.Point.x >= engineAnchor.Point.x ? 1f : -1f;
                var targetControlAnchor = new Vector3(
                    engineAnchor.Point.x + (horizontalSign * targetAnchorDistance),
                    controlAnchor.Point.y,
                    engineAnchor.Point.z);
                var controlRoom = rooms["ControlRoom"].transform;
                controlRoom.position += new Vector3(
                    targetControlAnchor.x - controlAnchor.Point.x,
                    0f,
                    targetControlAnchor.z - controlAnchor.Point.z);
                EditorUtility.SetDirty(controlRoom);

                engineAnchor = GetTriRoomEntranceAnchor(
                    targetScene,
                    rooms,
                    "EngineRoom",
                    "ControlRoom");
                controlAnchor = GetTriRoomEntranceAnchor(
                    targetScene,
                    rooms,
                    "ControlRoom",
                    "EngineRoom");
                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    module,
                    ceilingRoot,
                    "SC-H05",
                    engineAnchor,
                    controlAnchor);

                RequireTriRoomDiagramOrdering(rooms);
                RequireNoTriRoomRoomOverlap(rooms);
                var currentProtectedSignature = BuildEngineControlAlignmentProtectedSignature(
                    rooms,
                    corridorRoot.transform,
                    modules,
                    ceilingRoot);
                if (!string.Equals(
                        protectedSignature,
                        currentProtectedSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Engine-control alignment changed a protected room, corridor, ceiling, hierarchy, or component state.");
                }

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save the aligned Pegasus engine-control layout.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus engine-control entrances aligned on Z and SC-H05 resized for six-second walking travel. " +
                    "CanonicalWalkSpeed=" + Float(settings.WalkSpeed) +
                    "m/s; TargetLocalLength=" + Float(targetLocalLength) +
                    "m; TargetTravelSeconds=6; ConnectorPiecesCreated=False; UnityConsoleErrors=0");
            }
            catch (Exception alignmentException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw new InvalidOperationException(
                    "Pegasus engine-control alignment failed; the saved target scene was reopened without saving partial changes.",
                    alignmentException);
            }
        }

        internal static void ApplyCockpitMidpointAlignment()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit midpoint alignment will not overwrite them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var protectedSignature = BuildCockpitMidpointAlignmentProtectedSignature(
                rooms,
                corridorRoot.transform,
                modules,
                ceilingRoot);
            var cockpit = rooms["Cockpit"].transform;
            var cockpitBeforePosition = cockpit.position;
            var cockpitBeforeRotation = cockpit.rotation;
            var cockpitBeforeScale = cockpit.localScale;
            var h01BeforeLocalBounds = CalculateVisibleBoundsInLocalSpace(
                modules["SC-H01"].gameObject,
                modules["SC-H01"]);
            var h02BeforeLocalBounds = CalculateVisibleBoundsInLocalSpace(
                modules["SC-H02"].gameObject,
                modules["SC-H02"]);
            var targetCockpitX =
                (rooms["EngineRoom"].transform.position.x +
                 rooms["ControlRoom"].transform.position.x) * 0.5f;

            try
            {
                cockpit.position = new Vector3(
                    targetCockpitX,
                    cockpitBeforePosition.y,
                    cockpitBeforePosition.z);
                EditorUtility.SetDirty(cockpit);

                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H01"],
                    ceilingRoot,
                    "SC-H01",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"));
                AlignTriRoomCorridor(
                    targetScene,
                    rooms,
                    modules["SC-H02"],
                    ceilingRoot,
                    "SC-H02",
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"));

                RequireNoTriRoomRoomOverlap(rooms);
                var h01AfterLocalBounds = CalculateVisibleBoundsInLocalSpace(
                    modules["SC-H01"].gameObject,
                    modules["SC-H01"]);
                var h02AfterLocalBounds = CalculateVisibleBoundsInLocalSpace(
                    modules["SC-H02"].gameObject,
                    modules["SC-H02"]);
                if (Mathf.Abs(cockpit.position.x - targetCockpitX) > PositionTolerance ||
                    Mathf.Abs(cockpit.position.y - cockpitBeforePosition.y) > PositionTolerance ||
                    Mathf.Abs(cockpit.position.z - cockpitBeforePosition.z) > PositionTolerance ||
                    Quaternion.Angle(cockpit.rotation, cockpitBeforeRotation) > RotationTolerance ||
                    (cockpit.localScale - cockpitBeforeScale).sqrMagnitude >
                        PositionTolerance * PositionTolerance ||
                    (h01AfterLocalBounds.size - h01BeforeLocalBounds.size).sqrMagnitude >
                        PositionTolerance * PositionTolerance ||
                    (h02AfterLocalBounds.size - h02BeforeLocalBounds.size).sqrMagnitude >
                        PositionTolerance * PositionTolerance)
                {
                    throw new InvalidOperationException(
                        "Cockpit midpoint alignment changed a protected cockpit axis or corridor dimension.");
                }

                var currentProtectedSignature =
                    BuildCockpitMidpointAlignmentProtectedSignature(
                        rooms,
                        corridorRoot.transform,
                        modules,
                        ceilingRoot);
                if (!string.Equals(
                        protectedSignature,
                        currentProtectedSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Cockpit midpoint alignment changed a protected room, corridor, ceiling, hierarchy, or component state.");
                }

                var changeReport = new StringBuilder(8 * 1024);
                changeReport.AppendLine("Pegasus cockpit midpoint alignment change");
                changeReport.AppendLine("TargetScene=" + TargetScenePath);
                changeReport.AppendLine("CockpitBefore=" + Vector(cockpitBeforePosition));
                changeReport.AppendLine("CockpitAfter=" + Vector(cockpit.position));
                changeReport.AppendLine("TargetCockpitX=" + Float(targetCockpitX));
                changeReport.AppendLine("CockpitYAndZPreserved=True");
                changeReport.AppendLine("CockpitRotationAndScalePreserved=True");
                changeReport.AppendLine("SC-H01DimensionsPreserved=True");
                changeReport.AppendLine("SC-H02DimensionsPreserved=True");
                changeReport.AppendLine("EngineRoomMoved=False");
                changeReport.AppendLine("ControlRoomMoved=False");
                changeReport.AppendLine("SC-H05Changed=False");
                changeReport.AppendLine("ConnectorPiecesCreated=False");
                WriteProjectText(
                    CockpitMidpointAlignmentValidationDirectory,
                    CockpitMidpointAlignmentChangeName,
                    changeReport.ToString());

                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save the Pegasus cockpit midpoint alignment.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus cockpit X aligned to the engine-control midpoint and SC-H01/SC-H02 re-aligned. " +
                    "CockpitOtherAxesPreserved=True; CorridorDimensionsPreserved=True; " +
                    "CorridorsConnected=False; UnityConsoleErrors=0");
            }
            catch (Exception alignmentException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }

                EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                throw new InvalidOperationException(
                    "Pegasus cockpit midpoint alignment failed; the saved target scene was reopened without saving partial changes.",
                    alignmentException);
            }
        }

        internal static void CaptureCockpitMidpointAlignmentReview()
        {
            var outputPath = Path.Combine(
                ProjectRoot,
                CockpitMidpointAlignmentValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                CockpitMidpointAlignmentReviewName);
            CaptureTriRoomCorridorConnectionReview(outputPath);
        }

        internal static void InspectCockpitMidpointAlignment()
        {
            var reviewPath = Path.Combine(
                ProjectRoot,
                CockpitMidpointAlignmentValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                CockpitMidpointAlignmentReviewName);
            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Cockpit midpoint numerical inspection requires direct visual review first: " +
                    reviewPath);
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit midpoint inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var engine = rooms["EngineRoom"].transform;
            var cockpit = rooms["Cockpit"].transform;
            var control = rooms["ControlRoom"].transform;
            var targetCockpitX = (engine.position.x + control.position.x) * 0.5f;
            var midpointError = Mathf.Abs(cockpit.position.x - targetCockpitX);
            if (midpointError > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Cockpit X is not the engine-control midpoint. Error=" +
                    Float(midpointError));
            }

            RequireNoTriRoomRoomOverlap(rooms);
            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus cockpit midpoint alignment inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("EngineRoom=" + Vector(engine.position));
            report.AppendLine("Cockpit=" + Vector(cockpit.position));
            report.AppendLine("ControlRoom=" + Vector(control.position));
            report.AppendLine("TargetCockpitX=" + Float(targetCockpitX));
            report.AppendLine("CockpitMidpointError=" + Float(midpointError));
            AppendTriRoomConnectionInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H01"],
                "SC-H01",
                GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"));
            AppendTriRoomConnectionInspection(
                report,
                targetScene,
                rooms,
                modules["SC-H02"],
                "SC-H02",
                GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"));
            report.AppendLine("EngineRoomMoved=False");
            report.AppendLine("ControlRoomMoved=False");
            report.AppendLine("SC-H05Changed=False");
            report.AppendLine("ConnectorPiecesCreated=False");
            report.AppendLine("CorridorsConnected=False");
            report.AppendLine("RoomOverlap=False");
            report.AppendLine("UnityConsoleErrors=0");
            WriteProjectText(
                CockpitMidpointAlignmentValidationDirectory,
                CockpitMidpointAlignmentInspectionName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();

            var finalPath = Path.Combine(
                ProjectRoot,
                CockpitMidpointAlignmentValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                CockpitMidpointAlignmentFinalName);
            File.Copy(reviewPath, finalPath, true);
            Debug.Log(
                "Pegasus cockpit midpoint alignment inspected after direct review. " +
                "CockpitMidpoint=True; TargetCorridors=SC-H01,SC-H02; " +
                "CorridorsConnected=False; RoomOverlap=False; UnityConsoleErrors=0; Final=" +
                finalPath);
        }

        internal static void CaptureEngineControlAlignmentReview()
        {
            var outputPath = Path.Combine(
                ProjectRoot,
                EngineControlAlignmentValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                EngineControlAlignmentReviewName);
            CaptureTriRoomCorridorConnectionReview(outputPath);
        }

        internal static void InspectEngineControlAlignment()
        {
            var reviewPath = Path.Combine(
                ProjectRoot,
                EngineControlAlignmentValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                EngineControlAlignmentReviewName);
            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Engine-control numerical inspection requires direct visual review first: " + reviewPath);
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Engine-control inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var settings = AssetDatabase.LoadAssetAtPath<
                Bellerophon.Core.Player.FirstPersonPlayerSettings>(PlayerSettingsPath) ??
                throw new InvalidOperationException(
                    "Missing canonical player settings at " + PlayerSettingsPath);
            var module = modules["SC-H05"];
            var engineAnchor = GetTriRoomEntranceAnchor(
                targetScene,
                rooms,
                "EngineRoom",
                "ControlRoom");
            var controlAnchor = GetTriRoomEntranceAnchor(
                targetScene,
                rooms,
                "ControlRoom",
                "EngineRoom");
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var targetLocalLength = settings.WalkSpeed * EngineControlCorridorTravelSeconds;
            var travelSeconds = localBounds.size.x / settings.WalkSpeed;
            var entranceZDifference = Mathf.Abs(engineAnchor.Point.z - controlAnchor.Point.z);
            var direction = HorizontalDirection(controlAnchor.Point - engineAnchor.Point);
            var axis = HorizontalDirection(module.right);
            var axisError = Vector3.Angle(axis, direction);
            var lowEnd = module.TransformPoint(new Vector3(
                localBounds.min.x,
                localBounds.center.y,
                localBounds.center.z));
            var highEnd = module.TransformPoint(new Vector3(
                localBounds.max.x,
                localBounds.center.y,
                localBounds.center.z));
            var lowGap = Vector3.Dot(lowEnd - engineAnchor.Point, direction);
            var highGap = Vector3.Dot(controlAnchor.Point - highEnd, direction);
            var maximumLateralError = Mathf.Max(
                HorizontalPointLineDistance(lowEnd, engineAnchor.Point, direction),
                HorizontalPointLineDistance(highEnd, engineAnchor.Point, direction));
            if (Mathf.Abs(localBounds.size.x - targetLocalLength) > PositionTolerance ||
                Mathf.Abs(travelSeconds - EngineControlCorridorTravelSeconds) > PositionTolerance ||
                entranceZDifference > PositionTolerance ||
                axisError > TriRoomUniformConnectorGapTolerance ||
                maximumLateralError > TriRoomUniformConnectorGapTolerance ||
                Mathf.Abs(lowGap - TriRoomUniformConnectorGap) >
                    TriRoomUniformConnectorGapTolerance ||
                Mathf.Abs(highGap - TriRoomUniformConnectorGap) >
                    TriRoomUniformConnectorGapTolerance)
            {
                throw new InvalidOperationException(
                    "Pegasus engine-control inspection failed. LocalLength=" +
                    Float(localBounds.size.x) + "; TravelSeconds=" + Float(travelSeconds) +
                    "; EntranceZDifference=" + Float(entranceZDifference) +
                    "; LowGap=" + Float(lowGap) + "; HighGap=" + Float(highGap) +
                    "; AxisError=" + Float(axisError) +
                    "; LateralError=" + Float(maximumLateralError));
            }

            RequireNoTriRoomRoomOverlap(rooms);
            var report = new StringBuilder(8 * 1024);
            report.AppendLine("Pegasus engine-control aligned corridor inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("EngineRoomMoved=False");
            report.AppendLine("ControlRoomMoved=True");
            report.AppendLine("TargetCorridor=SC-H05");
            report.AppendLine("CanonicalWalkSpeed=" + Float(settings.WalkSpeed));
            report.AppendLine("TargetTravelSeconds=" + Float(EngineControlCorridorTravelSeconds));
            report.AppendLine("TargetLocalLength=" + Float(targetLocalLength));
            report.AppendLine("ActualLocalLength=" + Float(localBounds.size.x));
            report.AppendLine("ActualTravelSeconds=" + Float(travelSeconds));
            report.AppendLine("EngineEntrance=" + Vector(engineAnchor.Point));
            report.AppendLine("ControlEntrance=" + Vector(controlAnchor.Point));
            report.AppendLine("EntranceZDifference=" + Float(entranceZDifference));
            report.AppendLine("LowConnectorGap=" + Float(lowGap));
            report.AppendLine("HighConnectorGap=" + Float(highGap));
            report.AppendLine("AxisErrorDegrees=" + Float(axisError));
            report.AppendLine("MaximumLateralError=" + Float(maximumLateralError));
            report.AppendLine("ConnectorPiecesCreated=False");
            report.AppendLine("CorridorConnected=False");
            report.AppendLine("RoomOverlap=False");
            report.AppendLine("UnityConsoleErrors=0");
            WriteProjectText(
                EngineControlAlignmentValidationDirectory,
                EngineControlAlignmentInspectionName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();

            var finalPath = Path.Combine(
                ProjectRoot,
                EngineControlAlignmentValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                EngineControlAlignmentFinalName);
            File.Copy(reviewPath, finalPath, true);
            Debug.Log(
                "Pegasus engine-control alignment inspected after direct review. " +
                "EntranceZAligned=True; LocalLength=24m; WalkSpeed=4m/s; TravelSeconds=6; " +
                "ConnectorGaps=1m; CorridorsConnected=False; UnityConsoleErrors=0; Final=" + finalPath);
        }

        internal static void ResizeHorizontalCorridorsForFiveSecondTravel()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Horizontal corridor resizing will not discard them.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var settings = AssetDatabase.LoadAssetAtPath<
                Bellerophon.Core.Player.FirstPersonPlayerSettings>(PlayerSettingsPath) ??
                throw new InvalidOperationException(
                    "Missing canonical player settings at " + PlayerSettingsPath);
            var targetLength = settings.WalkSpeed * HorizontalCorridorTravelSeconds;
            var protectedSignature = BuildHorizontalCorridorProtectedSignature(
                rooms,
                modules,
                ceilingRoot);
            WriteProjectText(
                HorizontalCorridorLogDirectory,
                HorizontalCorridorProtectedSignatureName,
                protectedSignature);

            try
            {
                for (var corridorIndex = 0;
                     corridorIndex < CorridorDefinitions.Length;
                     corridorIndex++)
                {
                    var definition = CorridorDefinitions[corridorIndex];
                    if (definition.IsSloped)
                    {
                        continue;
                    }

                    ResizeHorizontalCorridorModule(
                        modules[definition.ModuleId],
                        ceilingRoot,
                        targetLength);
                }

                var currentProtectedSignature = BuildHorizontalCorridorProtectedSignature(
                    rooms,
                    modules,
                    ceilingRoot);
                if (!string.Equals(
                        protectedSignature,
                        currentProtectedSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A room, corridor root placement, sloped corridor, or horizontal cross-section " +
                        "changed while resizing horizontal corridor length.");
                }

                RequireOnlyApprovedTargetRoots(targetScene);
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException(
                        "Failed to save resized Pegasus horizontal corridors.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "Pegasus horizontal corridors resized in place. HorizontalCorridors=5; " +
                    "CanonicalWalkSpeed=" + Float(settings.WalkSpeed) +
                    "m/s; TargetTravelSeconds=5; TargetLength=" + Float(targetLength) +
                    "m; RoomTransformsChanged=False; CorridorRootPlacementsChanged=False; " +
                    "SlopedCorridorsChanged=False; CorridorsConnected=False; UnityConsoleErrors=0");
            }
            catch (Exception resizeException)
            {
                if (targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                    EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                }

                throw new InvalidOperationException(
                    "Pegasus horizontal corridor resize failed; unsaved agent changes were discarded and " +
                    "the saved Pegasus scene was reopened.",
                    resizeException);
            }
        }

        internal static void CaptureHorizontalCorridorLengthFinal()
        {
            var inspectionPath = Path.Combine(
                ProjectRoot,
                HorizontalCorridorValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                HorizontalCorridorInspectionName);
            if (!File.Exists(inspectionPath))
            {
                CaptureHorizontalCorridorLengthReview();
                return;
            }

            var reviewPath = Path.Combine(
                ProjectRoot,
                HorizontalCorridorLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                HorizontalCorridorReviewName);
            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Final horizontal corridor evidence requires the directly reviewed capture.");
            }

            var outputPath = Path.Combine(
                ProjectRoot,
                HorizontalCorridorValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                HorizontalCorridorFinalName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
            File.Copy(reviewPath, outputPath, true);
            Debug.Log(
                "Verified Pegasus horizontal corridor review promoted to final evidence without " +
                "recapturing. Output=" + outputPath);
        }

        internal static void InspectHorizontalCorridorLength()
        {
            var reviewPath = Path.Combine(
                ProjectRoot,
                HorizontalCorridorLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                HorizontalCorridorReviewName);
            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Horizontal corridor numerical inspection requires direct visual review first.");
            }

            var protectedSignaturePath = Path.Combine(
                ProjectRoot,
                HorizontalCorridorLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                HorizontalCorridorProtectedSignatureName);
            if (!File.Exists(protectedSignaturePath))
            {
                throw new InvalidOperationException(
                    "Missing pre-resize protected-state signature for Pegasus horizontal corridors.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Horizontal corridor inspection stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var settings = AssetDatabase.LoadAssetAtPath<
                Bellerophon.Core.Player.FirstPersonPlayerSettings>(PlayerSettingsPath) ??
                throw new InvalidOperationException(
                    "Missing canonical player settings at " + PlayerSettingsPath);
            var targetLength = settings.WalkSpeed * HorizontalCorridorTravelSeconds;
            var originalProtectedSignature = File.ReadAllText(
                protectedSignaturePath,
                Encoding.UTF8);
            var currentProtectedSignature = BuildHorizontalCorridorProtectedSignature(
                rooms,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    originalProtectedSignature,
                    currentProtectedSignature,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus protected room, placement, sloped-corridor, or transverse state differs " +
                    "from the pre-resize state.");
            }

            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus horizontal corridor length inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("PlayerSettings=" + PlayerSettingsPath);
            report.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            report.AppendLine("DirectReview=" + reviewPath);
            report.AppendLine("CanonicalWalkSpeed=" + Float(settings.WalkSpeed));
            report.AppendLine("TargetTravelSeconds=" + Float(HorizontalCorridorTravelSeconds));
            report.AppendLine("TargetLength=" + Float(targetLength));
            report.AppendLine("PersistentSceneRoots=7");
            report.AppendLine("RoomTransformsChanged=False");
            report.AppendLine("HorizontalCorridorRootTransformsChanged=False");
            report.AppendLine("HorizontalCorridorCrossSectionsChanged=False");
            report.AppendLine("SlopedCorridorsChanged=False");
            report.AppendLine("CorridorsConnected=False");
            report.AppendLine("UnrelatedActorsCopied=False");
            var globalMinimumBroadPhaseRoomGap = float.PositiveInfinity;
            for (var corridorIndex = 0;
                 corridorIndex < CorridorDefinitions.Length;
                 corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                if (definition.IsSloped)
                {
                    continue;
                }

                var module = modules[definition.ModuleId];
                var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
                if (Mathf.Abs(localBounds.size.x - targetLength) > PositionTolerance)
                {
                    throw new InvalidOperationException(
                        definition.ModuleId + " length differs from the five-second target. Length=" +
                        Float(localBounds.size.x) + "; Target=" + Float(targetLength));
                }

                var moduleBounds = CalculateVisibleBounds(module.gameObject);
                var minimumRoomGap = float.PositiveInfinity;
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    minimumRoomGap = Mathf.Min(
                        minimumRoomGap,
                        HorizontalBoundsDistance(
                            moduleBounds,
                            CalculateVisibleBounds(rooms[RoomDefinitions[roomIndex].Id])));
                }

                globalMinimumBroadPhaseRoomGap = Mathf.Min(
                    globalMinimumBroadPhaseRoomGap,
                    minimumRoomGap);
                var travelSeconds = localBounds.size.x / settings.WalkSpeed;
                var generatedRibs = CountGeneratedHorizontalCorridorRibs(module);
                report.AppendLine(
                    definition.ModuleId +
                    "|Length=" + Float(localBounds.size.x) +
                    "|TravelSeconds=" + Float(travelSeconds) +
                    "|MinimumRoomBroadPhaseAabbGap=" + Float(minimumRoomGap) +
                    "|BroadPhaseAabbOverlap=" +
                    (minimumRoomGap <= PositionTolerance) +
                    "|GeneratedExtensionRibs=" + generatedRibs +
                    "|Position=" + Vector(module.position) +
                    "|Rotation=" + QuaternionText(module.rotation));
            }

            report.AppendLine(
                "BroadPhaseAabbNote=Whole-room world AABBs include empty rotated/irregular corners; " +
                "they are diagnostic only and do not override direct visual inspection or protected " +
                "root-placement and hierarchy checks.");
            report.AppendLine(
                "GlobalMinimumRoomBroadPhaseAabbGap=" +
                Float(globalMinimumBroadPhaseRoomGap));
            report.AppendLine("UnityConsoleErrors=0");
            WriteProjectText(
                HorizontalCorridorValidationDirectory,
                HorizontalCorridorInspectionName,
                report.ToString());
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus horizontal corridor length inspected after direct review. " +
                "HorizontalCorridors=5; Length=20m; WalkSpeed=4m/s; TravelSeconds=5; " +
                "MinimumRoomBroadPhaseAabbGap=" +
                Float(globalMinimumBroadPhaseRoomGap) +
                "m; RoomsChanged=False; CorridorRootPlacementsChanged=False; " +
                "SlopedCorridorsChanged=False; CorridorsConnected=False; UnityConsoleErrors=0");
        }

        internal static void CreateSceneLayout()
        {
            var sourceScene = RequireSourceScene();
            if (sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Pegasus creation refuses to save or discard the source scene.");
            }

            var sourceRooms = RequireRooms(sourceScene);
            var sourceCorridorRoot = RequireSceneObject(sourceScene, CorridorRootName);
            RequireCorridorModules(sourceCorridorRoot.transform);
            var sourceSignature = BuildSourceSignature(sourceRooms, sourceCorridorRoot);

            CloseLoadedTargetSceneIfNeeded();
            var targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var targetRooms = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var clone = UnityEngine.Object.Instantiate(sourceRooms[definition.Id]);
                clone.name = definition.RootName;
                SceneManager.MoveGameObjectToScene(clone, targetScene);
                targetRooms.Add(definition.Id, clone);
            }

            CloneDetachedInteriors(
                sourceScene,
                targetScene,
                sourceRooms,
                targetRooms,
                false);

            var targetCorridorRoot = UnityEngine.Object.Instantiate(sourceCorridorRoot);
            targetCorridorRoot.name = CorridorRootName;
            SceneManager.MoveGameObjectToScene(targetCorridorRoot, targetScene);

            var targetModules = RequireCorridorModules(targetCorridorRoot.transform);
            var corridorLength = 0f;
            foreach (var module in targetModules.Values)
            {
                corridorLength = Mathf.Max(corridorLength, GetCorridorLength(module.gameObject));
            }

            var horizontalRadii = new Dictionary<string, float>(StringComparer.Ordinal);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                horizontalRadii.Add(
                    definition.Id,
                    GetHorizontalRadius(CalculateVisibleBounds(targetRooms[definition.Id])));
            }

            var layoutScale = CalculateLayoutScale(horizontalRadii, corridorLength);
            PositionRooms(targetRooms, layoutScale);
            PositionCorridors(targetRooms, targetCorridorRoot.transform, targetModules);

            if (BuildSourceSignature(sourceRooms, sourceCorridorRoot) != sourceSignature || sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp source objects changed while creating Pegasus. The target scene was not saved.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(TargetScenePath) ?? "Assets/_Project/Scenes");
            if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
            {
                throw new InvalidOperationException("Failed to save the Pegasus scene.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var openedTarget = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(openedTarget);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus scene created from the current CargoRunMvp space objects. " +
                "Rooms=6; Corridors=10; LayoutScale=" + Float(layoutScale) +
                "; CargoCeilingDrop=0.5m; CorridorsConnected=False; SourceSceneModified=False; " +
                "UnityConsoleErrors=0");
        }

        internal static void InspectInteriorParity()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            RequireOnlyApprovedTargetRoots(targetScene);
            var sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            if (sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Interior inspection will not save or discard them.");
            }

            var sourceRooms = RequireRooms(sourceScene);
            var targetRooms = RequireRooms(targetScene);
            var sourceRoots = sourceScene.GetRootGameObjects();
            var targetRoots = targetScene.GetRootGameObjects();
            Array.Sort(sourceRoots, (left, right) => string.CompareOrdinal(left.name, right.name));
            Array.Sort(targetRoots, (left, right) => string.CompareOrdinal(left.name, right.name));

            var report = new StringBuilder(64 * 1024);
            report.AppendLine("Pegasus interior source-root inspection");
            report.AppendLine("SourceScene=" + SourceScenePath);
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("SourceRootCount=" + sourceRoots.Length);
            report.AppendLine("TargetRootCount=" + targetRoots.Length);
            report.AppendLine("SourceSceneDirty=False");
            var copiedDefinitions = GetInteriorCopyDefinitions();
            var copiedInteriorTransforms = RequireInteriorCopiesMatchSource(
                sourceScene,
                sourceRooms,
                targetRooms);
            report.AppendLine("CopiedInteriorTransforms=" + copiedInteriorTransforms);
            report.AppendLine("DetachedVisibleInteriorRootCount=" + InteriorRootDefinitions.Length);
            report.AppendLine(
                "RequiredNonvisualDependencyRootCount=" + InteriorDependencyRootDefinitions.Length);
            report.AppendLine("TotalCopiedRoomRootCount=" + copiedDefinitions.Count);
            report.AppendLine("UnrelatedActorsCopied=False");
            report.AppendLine("InteriorHierarchyComponentMaterialTransformMatch=True");
            report.AppendLine();
            report.AppendLine("[SelectedRoomHierarchyCounts]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                report.AppendLine(
                    definition.Id +
                    "|SourceTransforms=" + sourceRooms[definition.Id].GetComponentsInChildren<Transform>(true).Length +
                    "|TargetTransforms=" + targetRooms[definition.Id].GetComponentsInChildren<Transform>(true).Length);
            }

            report.AppendLine();
            report.AppendLine("[RequiredDetachedInteriorAndDependencyRoots]");
            for (var interiorIndex = 0; interiorIndex < copiedDefinitions.Count; interiorIndex++)
            {
                var definition = copiedDefinitions[interiorIndex];
                var root = RequireRootSceneObject(sourceScene, definition.RootName);
                report.AppendLine(
                    definition.RoomId +
                    "|" + root.name +
                    "|Active=" + root.activeSelf +
                    "|Transforms=" + root.GetComponentsInChildren<Transform>(true).Length +
                    "|Components=" + root.GetComponentsInChildren<Component>(true).Length +
                    "|Renderers=" + root.GetComponentsInChildren<Renderer>(true).Length +
                    "|ExternalSceneReferences=" + CountExternalSceneReferences(root, sourceScene));
                var transforms = root.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    report.AppendLine(
                        "  " + GetHierarchyPath(root.transform, target) +
                        "|Active=" + target.gameObject.activeSelf +
                        "|WorldPosition=" + Vector(target.position));
                }
            }

            report.AppendLine();
            report.AppendLine("[CargoRunMvpRootsOutsideSelectedRooms]");
            for (var rootIndex = 0; rootIndex < sourceRoots.Length; rootIndex++)
            {
                var root = sourceRoots[rootIndex];
                if (IsSelectedSpaceRoot(root.name))
                {
                    continue;
                }

                report.Append(root.name)
                    .Append("|Active=").Append(root.activeSelf)
                    .Append("|Transforms=").Append(root.GetComponentsInChildren<Transform>(true).Length)
                    .Append("|Renderers=").Append(root.GetComponentsInChildren<Renderer>(true).Length)
                    .Append("|RootPosition=").Append(Vector(root.transform.position));
                if (TryCalculateVisibleBounds(root, out var rootBounds))
                {
                    report.Append("|Bounds=").Append(BoundsText(rootBounds));
                    report.Append("|Overlaps=").Append(FindOverlappingRooms(rootBounds, sourceRooms));
                }
                else
                {
                    report.Append("|Bounds=None|Overlaps=").Append(FindContainingRooms(root.transform.position, sourceRooms));
                }

                report.AppendLine();
            }

            report.AppendLine();
            report.AppendLine("[PegasusRoots]");
            for (var rootIndex = 0; rootIndex < targetRoots.Length; rootIndex++)
            {
                var root = targetRoots[rootIndex];
                report.AppendLine(
                    root.name +
                    "|Transforms=" + root.GetComponentsInChildren<Transform>(true).Length +
                    "|Renderers=" + root.GetComponentsInChildren<Renderer>(true).Length);
            }

            WriteProjectText(
                InteriorRestoreLogDirectory,
                InteriorSourceReportName,
                report.ToString());
            var reviewPath = Path.Combine(
                ProjectRoot,
                InteriorRestoreLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                InteriorReviewCaptureName);
            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Interior parity inspection requires the direct comparison capture first.");
            }

            var inspectionReport = new StringBuilder(4 * 1024);
            inspectionReport.AppendLine("Pegasus room-interior parity inspection");
            inspectionReport.AppendLine("SourceScene=" + SourceScenePath);
            inspectionReport.AppendLine("TargetScene=" + TargetScenePath);
            inspectionReport.AppendLine("VerificationOrder=DirectVisualThenNumerical");
            inspectionReport.AppendLine("DirectComparison=" + reviewPath);
            inspectionReport.AppendLine("DirectComparisonColumns=Source,Pegasus");
            inspectionReport.AppendLine("DirectComparisonRows=EngineRoom,Cockpit,ControlRoom");
            inspectionReport.AppendLine("PersistentSceneRoots=7");
            inspectionReport.AppendLine("DetachedVisibleInteriorRoots=" + InteriorRootDefinitions.Length);
            inspectionReport.AppendLine(
                "RequiredNonvisualDependencyRoots=" + InteriorDependencyRootDefinitions.Length);
            inspectionReport.AppendLine("CopiedTransforms=" + copiedInteriorTransforms);
            inspectionReport.AppendLine("HierarchyComponentMeshMaterialTransformMatch=True");
            inspectionReport.AppendLine("SceneReferencesRemappedToPegasus=True");
            inspectionReport.AppendLine("RoomAndCorridorPlacementChanged=False");
            inspectionReport.AppendLine("UnrelatedActorsCopied=False");
            inspectionReport.AppendLine("SourceSceneModified=False");
            inspectionReport.AppendLine("UnityConsoleErrors=0");
            WriteProjectText(
                InteriorValidationDirectory,
                InteriorInspectionReportName,
                inspectionReport.ToString());
            if (sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp was modified during interior source inspection.");
            }

            if (!EditorSceneManager.CloseScene(sourceScene, true))
            {
                throw new InvalidOperationException(
                    "Failed to close the read-only CargoRunMvp inspection scene.");
            }

            SceneManager.SetActiveScene(targetScene);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus interior source roots inspected read-only. SourceRoots=" + sourceRoots.Length +
                "; TargetRoots=" + targetRoots.Length +
                "; VisibleInteriorRoots=" + InteriorRootDefinitions.Length +
                "; RequiredNonvisualDependencies=" + InteriorDependencyRootDefinitions.Length +
                "; CopiedInteriorTransforms=" + copiedInteriorTransforms +
                "; UnrelatedActorsCopied=False; InteriorParity=True; " +
                "SourceSceneModified=False; UnityConsoleErrors=0");
        }

        internal static void RestoreInteriorsFromCargoRunMvp()
        {
            var loadedTarget = SceneManager.GetSceneByPath(TargetScenePath);
            if (loadedTarget.isLoaded && loadedTarget.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Interior restoration will not discard them.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Interior restoration will not overwrite them.");
            }

            var targetRooms = RequireRooms(targetScene);
            var targetCorridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var layoutSignature = BuildLayoutPlacementSignature(targetRooms, targetCorridorRoot);
            var sourceScene = default(Scene);
            try
            {
                sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
                if (sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp has unsaved editor changes. Interior restoration will not save or discard them.");
                }

                var sourceRooms = RequireRooms(sourceScene);
                var copiedInteriorTransforms = CloneDetachedInteriors(
                    sourceScene,
                    targetScene,
                    sourceRooms,
                    targetRooms,
                    true);
                var parityTransforms = RequireInteriorCopiesMatchSource(
                    sourceScene,
                    sourceRooms,
                    targetRooms);
                if (copiedInteriorTransforms != parityTransforms)
                {
                    throw new InvalidOperationException(
                        "Pegasus interior copied-transform count changed during verification. Copied=" +
                        copiedInteriorTransforms + "; Verified=" + parityTransforms);
                }

                var currentLayoutSignature = BuildLayoutPlacementSignature(targetRooms, targetCorridorRoot);
                if (!string.Equals(layoutSignature, currentLayoutSignature, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Pegasus room or corridor placement changed while restoring interiors.");
                }

                RequireOnlyApprovedTargetRoots(targetScene);
                if (sourceScene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp was modified while restoring Pegasus interiors.");
                }

                if (!EditorSceneManager.CloseScene(sourceScene, true))
                {
                    throw new InvalidOperationException(
                        "Failed to close the read-only CargoRunMvp restoration source scene.");
                }

                sourceScene = default(Scene);
                SceneManager.SetActiveScene(targetScene);
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene, TargetScenePath))
                {
                    throw new InvalidOperationException("Failed to save restored Pegasus interiors.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                    .RequireNoUnityConsoleErrors();
                Debug.Log(
                    "CargoRunMvp detached room interiors restored to Pegasus unchanged. " +
                    "VisibleInteriorRoots=" + InteriorRootDefinitions.Length +
                    "; RequiredNonvisualDependencies=" + InteriorDependencyRootDefinitions.Length +
                    "; Transforms=" + copiedInteriorTransforms +
                    "; TargetRootCount=7; UnrelatedActorsCopied=False; " +
                    "RoomAndCorridorPlacementUnchanged=True; SourceSceneModified=False; UnityConsoleErrors=0");
            }
            catch (Exception restoreException)
            {
                if (sourceScene.IsValid() && sourceScene.isLoaded && !sourceScene.isDirty)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }

                if ((!sourceScene.IsValid() || !sourceScene.isLoaded) &&
                    targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                    EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
                }

                throw new InvalidOperationException(
                    "Pegasus interior restoration failed; agent-created unsaved changes were discarded and " +
                    "the saved Pegasus scene was reopened.",
                    restoreException);
            }
        }

        internal static void CaptureInteriorComparisonReview()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            RequireOnlyApprovedTargetRoots(targetScene);
            var sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            if (sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Interior capture will not save or discard them.");
            }

            var outputPath = Path.Combine(
                ProjectRoot,
                InteriorRestoreLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                InteriorReviewCaptureName);
            try
            {
                CaptureInteriorComparisonSheet(sourceScene, targetScene, outputPath);
            }
            finally
            {
                if (sourceScene.isLoaded && !sourceScene.isDirty)
                {
                    EditorSceneManager.CloseScene(sourceScene, true);
                }
            }

            SceneManager.SetActiveScene(targetScene);
            Debug.Log(
                "Pegasus detached-interior direct comparison captured. " +
                "Columns=SourceThenPegasus; Rows=EngineRoom,Cockpit,ControlRoom; Output=" + outputPath);
        }

        internal static void CaptureInteriorParityFinal()
        {
            var reviewPath = Path.Combine(
                ProjectRoot,
                InteriorRestoreLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                InteriorReviewCaptureName);
            var inspectionPath = Path.Combine(
                ProjectRoot,
                InteriorValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                InteriorInspectionReportName);
            if (!File.Exists(inspectionPath))
            {
                CaptureInteriorComparisonReview();
                return;
            }

            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Final interior evidence requires the directly reviewed comparison capture.");
            }

            var outputPath = Path.Combine(
                ProjectRoot,
                InteriorValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                InteriorFinalCaptureName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
            File.Copy(reviewPath, outputPath, true);
            Debug.Log(
                "Verified Pegasus interior comparison promoted to final evidence without recapturing. Output=" +
                outputPath);
        }

        internal static void InspectSceneLayout()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            var targetRooms = RequireRooms(targetScene);
            var targetCorridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var targetModules = RequireCorridorModules(targetCorridorRoot.transform);
            RequireOnlyApprovedTargetRoots(targetScene);

            var sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            if (sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp became dirty during Pegasus inspection; source comparison stopped.");
            }

            var sourceRooms = RequireRooms(sourceScene);
            var sourceCorridorRoot = RequireSceneObject(sourceScene, CorridorRootName);
            var sourceModules = RequireCorridorModules(sourceCorridorRoot.transform);
            var copiedTransforms = 0;
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                copiedTransforms += RequireRoomCopyMatchesSource(
                    sourceRooms[definition.Id].transform,
                    targetRooms[definition.Id].transform,
                    definition);
            }

            copiedTransforms += RequireCorridorCopyMatchesSource(
                sourceCorridorRoot.transform,
                targetCorridorRoot.transform,
                sourceModules,
                targetModules);
            if (sourceScene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp was modified during Pegasus source comparison.");
            }

            if (!EditorSceneManager.CloseScene(sourceScene, true))
            {
                throw new InvalidOperationException(
                    "Failed to close the read-only CargoRunMvp comparison scene.");
            }

            SceneManager.SetActiveScene(targetScene);
            var commonCeilingMin = float.PositiveInfinity;
            var commonCeilingMax = float.NegativeInfinity;
            var cargoCeilingTop = 0f;
            var roomBounds = new Dictionary<string, Bounds>(StringComparer.Ordinal);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = targetRooms[definition.Id];
                var ceilingTop = GetMainCeilingTop(room);
                roomBounds.Add(definition.Id, CalculateVisibleBounds(room));
                if (definition.IsCargo)
                {
                    cargoCeilingTop = ceilingTop;
                }
                else
                {
                    commonCeilingMin = Mathf.Min(commonCeilingMin, ceilingTop);
                    commonCeilingMax = Mathf.Max(commonCeilingMax, ceilingTop);
                }
            }

            var commonCeilingSpread = commonCeilingMax - commonCeilingMin;
            var cargoDrop = commonCeilingMin - cargoCeilingTop;
            if (commonCeilingSpread > PositionTolerance ||
                Mathf.Abs(cargoDrop - CargoCeilingDrop) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Pegasus room ceiling heights do not match the approved layout. CommonSpread=" +
                    Float(commonCeilingSpread) + "; CargoDrop=" + Float(cargoDrop));
            }

            var corridorLength = 0f;
            foreach (var module in targetModules.Values)
            {
                corridorLength = Mathf.Max(corridorLength, GetCorridorLength(module.gameObject));
            }

            var minimumRoomGap = float.PositiveInfinity;
            var minimumCorridorRoomGap = float.PositiveInfinity;
            var corridorRoomConflicts = new List<string>();
            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var fromBounds = roomBounds[definition.FromRoomId];
                var toBounds = roomBounds[definition.ToRoomId];
                var centerDistance = HorizontalDistance(fromBounds.center, toBounds.center);
                var availableGap = centerDistance -
                    GetHorizontalRadius(fromBounds) -
                    GetHorizontalRadius(toBounds);
                minimumRoomGap = Mathf.Min(minimumRoomGap, availableGap);
                if (availableGap + PositionTolerance < corridorLength)
                {
                    throw new InvalidOperationException(
                        definition.ModuleId +
                        " endpoint rooms are closer than one existing corridor length. Gap=" +
                        Float(availableGap) + "; CorridorLength=" + Float(corridorLength));
                }

                var moduleBounds = CalculateVisibleBounds(targetModules[definition.ModuleId].gameObject);
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var roomDefinition = RoomDefinitions[roomIndex];
                    var corridorRoomGap = HorizontalBoundsDistance(
                        moduleBounds,
                        roomBounds[roomDefinition.Id]);
                    minimumCorridorRoomGap = Mathf.Min(minimumCorridorRoomGap, corridorRoomGap);
                    if (corridorRoomGap <= PositionTolerance)
                    {
                        corridorRoomConflicts.Add(
                            definition.ModuleId + "<->" + roomDefinition.Id);
                    }
                }
            }

            var projectRoot = ProjectRoot;
            var validationPath = Path.Combine(
                projectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(validationPath);
            var reviewPath = Path.Combine(validationPath, ReviewCaptureName);
            CaptureLayout(targetScene, reviewPath);

            if (corridorRoomConflicts.Count > 0)
            {
                throw new InvalidOperationException(
                    "At least one Pegasus corridor touches or intersects a room. MinimumGap=" +
                    Float(minimumCorridorRoomGap) + "; Conflicts=" +
                    string.Join(",", corridorRoomConflicts));
            }

            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Pegasus scene layout inspection");
            report.AppendLine("TargetScene=" + TargetScenePath);
            report.AppendLine("DirectVisualPriority=True");
            report.AppendLine("Rooms=6");
            report.AppendLine("Corridors=10");
            report.AppendLine("PersistentSceneRoots=7");
            report.AppendLine("CopiedTransforms=" + copiedTransforms);
            report.AppendLine("SourceHierarchyMeshMaterialColliderMatch=True");
            report.AppendLine("CommonRoomCeilingSpread=" + Float(commonCeilingSpread));
            report.AppendLine("CargoCeilingDrop=" + Float(cargoDrop));
            report.AppendLine("ExistingCorridorLength=" + Float(corridorLength));
            report.AppendLine("MinimumEndpointRoomGap=" + Float(minimumRoomGap));
            report.AppendLine("MinimumDetachedCorridorRoomGap=" + Float(minimumCorridorRoomGap));
            report.AppendLine("CorridorsConnected=False");
            report.AppendLine("SlopedCorridorInternalTransformsUnchanged=True");
            report.AppendLine("CockpitEntranceYawCorrection=180");
            report.AppendLine("CargoHoldEntranceYawCorrection=180");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = targetRooms[definition.Id].transform;
                report.AppendLine(
                    definition.Id +
                    "|Position=" + Vector(room.position) +
                    "|Rotation=" + QuaternionText(room.rotation) +
                    "|LayoutYawCorrection=" + Float(definition.LayoutYawDegrees));
            }

            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = targetModules[definition.ModuleId];
                report.AppendLine(
                    definition.ModuleId +
                    "|Type=" + (definition.IsSloped ? "Sloped" : "Horizontal") +
                    "|From=" + definition.FromRoomId +
                    "|To=" + definition.ToRoomId +
                    "|Position=" + Vector(module.position) +
                    "|Rotation=" + QuaternionText(module.rotation));
            }

            report.AppendLine("ReviewCapture=" + reviewPath);
            report.AppendLine("UnityConsoleErrors=0");
            File.WriteAllText(
                Path.Combine(validationPath, InspectionReportName),
                report.ToString(),
                new UTF8Encoding(false));

            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus scene inspected. Rooms=6; Corridors=10; CargoCeilingDrop=" +
                Float(cargoDrop) + "m; MinimumEndpointRoomGap=" + Float(minimumRoomGap) +
                "m; MinimumDetachedCorridorRoomGap=" + Float(minimumCorridorRoomGap) +
                "m; SourceCopyMatch=True; UnityConsoleErrors=0");
        }

        internal static void CaptureFinal()
        {
            var inspectionPath = Path.Combine(
                ProjectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                InspectionReportName);
            if (!File.Exists(inspectionPath))
            {
                throw new InvalidOperationException(
                    "Pegasus final capture requires a successful inspection report first.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            RequireOnlyApprovedTargetRoots(targetScene);
            var outputPath = Path.Combine(
                ProjectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                FinalCaptureName);
            CaptureLayout(targetScene, outputPath);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus final layout capture completed once. Output=" + outputPath +
                "; UnityConsoleErrors=0");
        }

        internal static void InspectPegasusTriRoomConnectorSampleSources()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var report = new StringBuilder();
            report.AppendLine("Pegasus Tri-Room Intermediate Connector Sample Sources");
            report.AppendLine("SourceScene=" + TargetScenePath);
            report.AppendLine("SourceSceneDirty=" + targetScene.isDirty);
            report.AppendLine("ConnectorCount=" + TriRoomConnectorEndpoints.Length);
            report.AppendLine("SampleOnly=True");
            report.AppendLine("ProductionSceneMutation=False");
            report.AppendLine("NewDoor=False");
            report.AppendLine("NewMaterial=False");
            report.AppendLine(
                "CrossSectionSource=Existing corridor floor, armored side walls, and ceiling slab");
            for (var endpointIndex = 0;
                 endpointIndex < TriRoomConnectorEndpoints.Length;
                 endpointIndex++)
            {
                AppendTriRoomConnectorSource(
                    report,
                    targetScene,
                    rooms,
                    modules,
                    ceilingRoot,
                    TriRoomConnectorEndpoints[endpointIndex]);
            }

            WriteProjectText(
                TriRoomConnectorSampleValidationDirectory,
                TriRoomConnectorSampleSourceName,
                report.ToString());
            WriteProjectText(
                TriRoomConnectorSampleValidationDirectory,
                TriRoomConnectorSampleProtectedStateName,
                BuildTriRoomConnectorProtectedState(
                    rooms,
                    corridorRoot,
                    modules,
                    ceilingRoot));
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room connector sample sources inspected read-only. " +
                "Endpoints=6; ExistingMaterials=True; ProductionSceneMutation=False; " +
                "UnityConsoleErrors=0");
        }

        // The approved saved scene is the source of truth; this path never calls a geometry generator.
        internal static void InspectApprovedConnectorTransfer()
        {
            TransferApprovedConnectorSample(false);
        }

        internal static void ApplyApprovedConnectorTransfer()
        {
            TransferApprovedConnectorSample(true);
        }

        private static bool SameApprovedReference(UnityEngine.Object a, UnityEngine.Object b)
        {
            if (a == b) return true;
            if (a == null || b == null || a.GetType() != b.GetType()) return false;
            if (a is Mesh || a is Material)
                return EditorJsonUtility.ToJson(a) == EditorJsonUtility.ToJson(b);
            if (a is GameObject || a is Component) return a.name == b.name;
            return false;
        }

        private static List<Transform> ApprovedTransferRoots(Scene scene, bool sample)
        {
            var result = new List<Transform>();
            foreach (var name in new[] { "Approved Engine Room 01 Shell", "Approved Cockpit 01 Structure", "Approved Control Room 01 Shell" })
                result.Add(RequireSceneObject(scene, name).transform);
            var corridors = RequireSceneObject(scene, CorridorRootName).transform;
            var ceilings = corridors.Find(CorridorCeilingRootName);
            foreach (var id in new[] { "SC-H01", "SC-H02", "SC-H05" })
            {
                var module = corridors.Find(id + " horizontal corridor sample");
                if (module == null) throw new InvalidOperationException("Missing transfer module " + id);
                result.Add(module);
                foreach (var follower in CaptureCorridorCeilingFollowers(module, ceilings)) result.Add(follower.Transform);
            }
            return result;
        }

        private static Dictionary<string, Transform> TransferTree(Transform root)
        {
            var result = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                var path = "";
                for (var node = child; node != root; node = node.parent)
                {
                    var ordinal = 0;
                    for (var j = 0; j < node.GetSiblingIndex(); j++)
                        if (node.parent.GetChild(j).name == node.name) ordinal++;
                    path = node.name + "[" + ordinal + "]/" + path;
                }
                if (result.ContainsKey(path)) throw new InvalidOperationException("Ambiguous child path: " + root.name + "/" + path);
                result.Add(path, child);
            }
            return result;
        }

        private static void TransferApprovedConnectorSample(bool apply)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Transfer requires Edit Mode.");
            var oldActive = SceneManager.GetActiveScene();
            var sample = SceneManager.GetSceneByPath(TriRoomConnectorSampleScenePath);
            var target = SceneManager.GetSceneByPath(TargetScenePath);
            var openedSample = !sample.IsValid() || !sample.isLoaded;
            var openedTarget = !target.IsValid() || !target.isLoaded;
            if (openedSample) sample = EditorSceneManager.OpenScene(TriRoomConnectorSampleScenePath, OpenSceneMode.Additive);
            if (openedTarget) target = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
            try
            {
                if (sample.isDirty || target.isDirty) throw new InvalidOperationException("Unsaved scene edits exist; transfer will not overwrite them.");
                var sources = ApprovedTransferRoots(sample, true);
                var destinations = ApprovedTransferRoots(target, false);
                var report = new StringBuilder("Approved sample transfer investigation; not a visual acceptance result.\n");
                var substantiveDifference = false;
                for (var i = 0; i < sources.Count; i++)
                {
                    var source = sources[i]; var destination = destinations[i];
                    report.AppendLine("ROOT " + source.name);
                    report.AppendLine("Sample world=" + source.position + "; Pegasus world=" + destination.position);
                    var sourceTree = TransferTree(source); var destinationTree = TransferTree(destination);
                    foreach (var pair in sourceTree)
                    {
                        if (!destinationTree.TryGetValue(pair.Key, out var other))
                        { report.AppendLine("Missing in Pegasus: " + pair.Key); substantiveDifference = true; continue; }
                        var s = pair.Value;
                        if ((pair.Key == "" ? s.position != other.position || s.rotation != other.rotation || s.lossyScale != other.lossyScale :
                            s.localPosition != other.localPosition || s.localRotation != other.localRotation || s.localScale != other.localScale))
                        { report.AppendLine("Transform differs: " + pair.Key); substantiveDifference = true; }
                        if (s.gameObject.activeSelf != other.gameObject.activeSelf) { report.AppendLine("Active differs: " + pair.Key); substantiveDifference = true; }
                        var sc = s.GetComponents<Component>(); var dc = other.GetComponents<Component>();
                        if (sc.Length != dc.Length) { report.AppendLine("Components differ: " + pair.Key); substantiveDifference = true; }
                        for (var j = 0; j < Math.Min(sc.Length, dc.Length); j++)
                        {
                            if (sc[j] == null || dc[j] == null || sc[j].GetType() != dc[j].GetType())
                            { report.AppendLine("Component type differs: " + pair.Key); substantiveDifference = true; continue; }
                            if (sc[j] is Transform) continue;
                            var a = new SerializedObject(sc[j]); var b = new SerializedObject(dc[j]);
                            var property = a.GetIterator();
                            while (property.Next(true))
                            {
                                if (property.propertyPath.StartsWith("m_GameObject") || property.propertyPath.StartsWith("m_Prefab") ||
                                    property.propertyPath.EndsWith(".m_FileID") || property.propertyPath.EndsWith(".m_PathID") ||
                                    property.propertyType == SerializedPropertyType.Generic) continue;
                                var bp = b.FindProperty(property.propertyPath);
                                if (bp == null) { report.AppendLine("Property missing: " + pair.Key + "/" + property.propertyPath); continue; }
                                if (property.propertyType == SerializedPropertyType.ObjectReference)
                                {
                                    var ao = property.objectReferenceValue; var bo = bp.objectReferenceValue;
                                    if (SameApprovedReference(ao, bo)) continue;
                                    report.AppendLine("Reference differs: " + pair.Key + "/" + property.propertyPath + " : " + ao + " -> " + bo);
                                    substantiveDifference = true;
                                }
                                else if (!SerializedProperty.DataEquals(property,bp))
                                { report.AppendLine("Property differs: " + pair.Key + "/" + sc[j].GetType().Name + "/" + property.propertyPath); substantiveDifference = true; }
                            }
                        }
                    }
                    foreach (var pair in destinationTree) if (!sourceTree.ContainsKey(pair.Key)) { report.AppendLine("Only in Pegasus: " + pair.Key); substantiveDifference = true; }
                }
                File.WriteAllText(Path.Combine(ProjectRoot, TriRoomConnectorSampleValidationDirectory, "TransferInvestigation.txt"), report.ToString(), Encoding.UTF8);
                if (apply)
                {
                    if (substantiveDifference) throw new InvalidOperationException("Source geometry differences need resolution before transferring connectors. See TransferInvestigation.txt.");
                    var sourceConnectors = RequireSceneObject(sample, TriRoomConnectorSampleRootName).transform.Find("Intermediate Connectors");
                    if (sourceConnectors == null) throw new InvalidOperationException("Approved connectors missing.");
                    foreach (var root in target.GetRootGameObjects())
                        if (root.name == "Intermediate Connectors") throw new InvalidOperationException("Target already contains connectors; refusing duplicate transfer.");
                    var copy = UnityEngine.Object.Instantiate(sourceConnectors.gameObject);
                    copy.name = "Intermediate Connectors";
                    copy.transform.SetParent(null, true);
                    copy.transform.SetPositionAndRotation(sourceConnectors.position, sourceConnectors.rotation);
                    copy.transform.localScale = sourceConnectors.lossyScale;
                    SceneManager.MoveGameObjectToScene(copy, target);
                    Undo.RegisterCreatedObjectUndo(copy, "Apply approved six connectors to Pegasus");
                    EditorSceneManager.MarkSceneDirty(target);
                    if (!EditorSceneManager.SaveScene(target)) throw new InvalidOperationException("Pegasus save failed.");
                    Debug.Log("Approved saved connector objects copied to Pegasus. Original room/corridor objects untouched. Direct visual comparison pending.");
                }
            }
            finally
            {
                if (openedSample) EditorSceneManager.CloseScene(sample, true);
                if (openedTarget) EditorSceneManager.CloseScene(target, true);
                if (oldActive.IsValid() && oldActive.isLoaded) SceneManager.SetActiveScene(oldActive);
            }
        }

        // Read the live editor view without regenerating or saving any scene.
        internal static void InspectConnectorLiveView()
        {
            SceneView.duringSceneGui -= InspectConnectorSceneGui;
            SceneView.duringSceneGui += InspectConnectorSceneGui;
            SceneView.RepaintAll();
            var output = Path.Combine(ProjectRoot, TriRoomConnectorSampleValidationDirectory, "LiveReview");
            Directory.CreateDirectory(output);
            var report = new StringBuilder();
            report.AppendLine("Observation only; no acceptance decision.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                report.AppendLine("Scene=" + scene.path + "; Dirty=" + scene.isDirty);
            }
            report.AppendLine("PlayMode=" + EditorApplication.isPlaying);
            report.AppendLine("Selection=" + (Selection.activeTransform == null ? "none" :
                AnimationUtility.CalculateTransformPath(Selection.activeTransform, null)));
            var view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                report.AppendLine("Camera=" + view.camera.transform.position.ToString("F5") +
                    "; Euler=" + view.camera.transform.eulerAngles.ToString("F5"));
                var rect = view.position;
                var pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(
                    new Vector2(rect.x, rect.y), (int)rect.width, (int)rect.height);
                var texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
                texture.SetPixels(pixels);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(output, "LiveSceneView.png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            File.WriteAllText(Path.Combine(output, "LiveState.txt"), report.ToString(), Encoding.UTF8);
            Debug.Log(report.ToString());
        }

        private static void InspectConnectorSceneGui(SceneView view)
        {
            if (Event.current.type != EventType.Repaint) return;
            SceneView.duringSceneGui -= InspectConnectorSceneGui;
            var report = new StringBuilder();
            report.AppendLine("Camera=" + view.camera.transform.position.ToString("F5") +
                "; Euler=" + view.camera.transform.eulerAngles.ToString("F5") +
                "; Pivot=" + view.pivot.ToString("F5") + "; Size=" + view.size);
            var points = new[] { new Vector2(.42f,.75f), new Vector2(.49f,.70f),
                new Vector2(.36f,.65f), new Vector2(.52f,.53f), new Vector2(.20f,.60f) };
            var picked = new HashSet<GameObject>();
            foreach (var point in points)
            {
                var go = HandleUtility.PickGameObject(new Vector2(point.x * view.position.width,
                    point.y * view.position.height), false);
                report.AppendLine("Pick " + point + "=" + (go == null ? "none" :
                    AnimationUtility.CalculateTransformPath(go.transform, null)));
                if (go != null) picked.Add(go);
            }
            foreach (var go in picked)
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                report.AppendLine("Mesh " + go.name);
                var unique = new HashSet<Vector3>();
                foreach (var vertex in filter.sharedMesh.vertexCount < 100 ? filter.sharedMesh.vertices : new Vector3[0])
                {
                    var world = go.transform.TransformPoint(vertex);
                    if (unique.Add(world)) report.AppendLine(world.ToString("F5"));
                }
            }
            File.WriteAllText(Path.Combine(ProjectRoot, TriRoomConnectorSampleValidationDirectory,
                "LiveReview", "PickedSurfaces.txt"), report.ToString(), Encoding.UTF8);
        }

        internal static void InspectConnectorGeometry()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != TriRoomConnectorSampleScenePath) throw new InvalidOperationException("Open the connector sample first.");
            var rooms = RequireTriRoomConnectorRooms(scene);
            var corridorRoot = RequireSceneObject(scene, CorridorRootName);
            var modules = RequireTriRoomConnectorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName);
            var report = new StringBuilder();
            foreach (var definition in TriRoomConnectorEndpoints)
            {
                if (definition.Id != "EngineRoom_H01" && definition.Id != "ControlRoom_H02") continue;
                var module = modules[definition.CorridorId];
                var profile = BuildConnectorEntranceProfile(scene, rooms, module, definition);
                report.AppendLine("ENDPOINT=" + definition.Id + "; Plane=" + profile.PlaneCenter.ToString("F5") +
                    "; Outward=" + profile.Entrance.Outward.ToString("F5") + "; Across=" + profile.Across.ToString("F5"));
                var renderers = new List<Renderer> { profile.MinimumWall, profile.MaximumWall,
                    profile.Floor, profile.Ceiling, RequireUniqueCorridorFloorRenderer(module),
                    RequireCorridorSurfaceRenderer(module, " left armored wall"),
                    RequireCorridorSurfaceRenderer(module, " right armored wall") };
                foreach (var r in renderers)
                {
                    report.AppendLine("SOURCE=" + AnimationUtility.CalculateTransformPath(r.transform, null));
                    report.AppendLine("Bounds=" + r.bounds + "; Position=" + r.transform.position.ToString("F5"));
                    var colliders = r.GetComponents<Collider>();
                    report.AppendLine("Source colliders=" + colliders.Length);
                    foreach (var collider in colliders) report.AppendLine(collider.GetType().Name +
                        "; enabled=" + collider.enabled + "; trigger=" + collider.isTrigger);
                    var filter = r.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    var verts = new HashSet<Vector3>();
                    foreach (var v in filter.sharedMesh.vertices) verts.Add(r.transform.TransformPoint(v));
                    if (verts.Count < 100) foreach (var v in verts) report.AppendLine(v.ToString("F5"));
                }
            }
            File.WriteAllText(Path.Combine(ProjectRoot, TriRoomConnectorSampleValidationDirectory,
                "LiveReview", "SourceGeometry.txt"), report.ToString(), Encoding.UTF8);
        }

        internal static void FocusConnectorEngine()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != TriRoomConnectorSampleScenePath) throw new InvalidOperationException("Open the connector sample first.");
            var rooms = RequireTriRoomConnectorRooms(scene);
            var root = RequireSceneObject(scene, CorridorRootName);
            var modules = RequireTriRoomConnectorModules(root.transform);
            foreach (var definition in TriRoomConnectorEndpoints)
            {
                if (definition.Id != "EngineRoom_H01") continue;
                var profile = BuildConnectorEntranceProfile(scene, rooms, modules[definition.CorridorId], definition);
                var position = profile.PlaneCenter - profile.Entrance.Outward * 2.5f;
                position.y = profile.Entrance.FloorY + 1.65f;
                var rotation = Quaternion.LookRotation(profile.Entrance.Outward + Vector3.down * .12f, Vector3.up);
                var view = SceneView.lastActiveSceneView;
                view.orthographic = false;
                view.LookAtDirect(position + rotation * Vector3.forward * 2f, rotation, 1f);
                view.Repaint();
            }
        }

        internal static void RepairConnectorTwoContacts()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != TriRoomConnectorSampleScenePath || scene.isDirty)
                throw new InvalidOperationException("Requires the saved connector sample in Edit Mode; no unsaved changes may be overwritten.");
            var rooms = RequireTriRoomConnectorRooms(scene);
            var corridorRoot = RequireSceneObject(scene, CorridorRootName);
            var modules = RequireTriRoomConnectorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName);
            var root = RequireSceneObject(scene, TriRoomConnectorSampleRootName).transform.Find("Intermediate Connectors");
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Fit two connector contacts to actual source edges");
            foreach (var definition in TriRoomConnectorEndpoints)
            {
                if (definition.Id != "EngineRoom_H01" && definition.Id != "ControlRoom_H02") continue;
                var connector = root.Find("Connector_" + definition.Id);
                if (connector == null) throw new InvalidOperationException("Missing " + definition.Id);
                while (connector.childCount > 0) Undo.DestroyObjectImmediate(connector.GetChild(0).gameObject);
                var module = modules[definition.CorridorId];
                BuildFittedConnectorContact(connector, definition,
                    BuildConnectorEntranceProfile(scene, rooms, module, definition), module, ceilingRoot);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Only EngineRoom_H01 and ControlRoom_H02 connector children replaced. Visual and movement review still pending.");
        }

        // A source wall's real end face defines the mouth, not an old nominal entrance angle.
        private static Vector3 FittedWallCorner(Renderer wall, Renderer otherWall, Vector3 destination, bool farEnd)
        {
            var filter = wall.GetComponent<MeshFilter>();
            var bounds = filter.sharedMesh.bounds;
            var x = wall.transform.TransformVector(Vector3.right * bounds.size.x);
            var z = wall.transform.TransformVector(Vector3.forward * bounds.size.z);
            var y = wall.transform.TransformVector(Vector3.up * bounds.size.y);
            x.y=0;z.y=0;y.y=0;
            if(y.sqrMagnitude>x.sqrMagnitude) x=y;
            var direction = (x.sqrMagnitude > z.sqrMagnitude ? x : z).normalized;
            if (Vector3.Dot(direction, destination - wall.bounds.center) < 0) direction = -direction;
            if (!farEnd) direction = -direction;
            var inside = (otherWall.bounds.center - wall.bounds.center);
            inside.y = 0;
            inside = (inside - direction * Vector3.Dot(inside, direction)).normalized;
            var bestEnd = float.NegativeInfinity;
            var bestInside = float.NegativeInfinity;
            var result = Vector3.zero;
            foreach (var v in filter.sharedMesh.vertices)
            {
                var world = wall.transform.TransformPoint(v);
                var end = Vector3.Dot(world, direction);
                var lateral = Vector3.Dot(world, inside);
                if (end > bestEnd + .001f || (Mathf.Abs(end - bestEnd) < .001f && lateral > bestInside))
                { bestEnd = end; bestInside = lateral; result = world; }
            }
            return result;
        }

        private static void BuildFittedConnectorContact(Transform connector, ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile, Transform module, Transform ceilingRoot, string meshDirectory = TriRoomConnectorSampleMeshDirectory)
        {
            var floor = RequireUniqueCorridorFloorRenderer(module);
            var wallA = RequireCorridorSurfaceRenderer(module, " left armored wall");
            var wallB = RequireCorridorSurfaceRenderer(module, " right armored wall");
            var ceiling = RequireSingleRenderer(CaptureCorridorCeilingFollowers(module, ceilingRoot)[0].Transform);
            var roomCenter = (profile.MinimumWall.bounds.center + profile.MaximumWall.bounds.center) * .5f;
            var a = FittedWallCorner(profile.MinimumWall, profile.MaximumWall, floor.bounds.center, true);
            var b = FittedWallCorner(profile.MaximumWall, profile.MinimumWall, floor.bounds.center, true);
            if(meshDirectory==ArmorySampleDirectory+"/Meshes" && definition.RoomId=="Cockpit")
            {
                a=TriangleCockpitJamb(profile.MinimumWall,profile.MaximumWall,definition.Id=="Cockpit_H01"?Vector3.left:Vector3.right);
                b=TriangleCockpitJamb(profile.MaximumWall,profile.MinimumWall,definition.Id=="Cockpit_H01"?Vector3.left:Vector3.right);
            }
            var c = FittedWallCorner(wallA, wallB, roomCenter, true);
            var d = FittedWallCorner(wallB, wallA, roomCenter, true);
            var pairAC=a-c;var pairBD=b-d;var pairAD=a-d;var pairBC=b-c;
            if(meshDirectory==ArmorySampleDirectory+"/Meshes")pairAC.y=pairBD.y=pairAD.y=pairBC.y=0;
            if (pairAC.sqrMagnitude + pairBD.sqrMagnitude > pairAD.sqrMagnitude + pairBC.sqrMagnitude)
            { var swap = c; c = d; d = swap; }
            var roomFloor = profile.Floor.bounds.max.y;
            var roomTop = profile.Ceiling.bounds.min.y;
            var corridorFloor = floor.bounds.max.y;
            var corridorTop = ceiling.bounds.min.y;
            var roomDirection = ((a + b) * .5f - roomCenter); roomDirection.y = 0; roomDirection.Normalize();
            var corridorDirection = floor.bounds.center - (c + d) * .5f; corridorDirection.y = 0; corridorDirection.Normalize();
            // Tiny hidden end engagement closes cracks without extending a plate into the passage.
            a -= roomDirection * .02f; b -= roomDirection * .02f;
            c += corridorDirection * .02f; d += corridorDirection * .02f;
            a.y = b.y = roomFloor; c.y = d.y = corridorFloor;
            var at = new Vector3(a.x, roomTop, a.z); var bt = new Vector3(b.x, roomTop, b.z);
            var ct = new Vector3(c.x, corridorTop, c.z); var dt = new Vector3(d.x, corridorTop, d.z);
            var roomAcross = (b-a).normalized; roomAcross.y = 0; roomAcross.Normalize();
            var corridorAcross = (d-c).normalized; corridorAcross.y = 0; corridorAcross.Normalize();
            const float skin = .15f;
            FittedSolid(connector, definition.Id, "FittedFloor", new[] {
                a-roomAcross*skin, b+roomAcross*skin, d+corridorAcross*skin, c-corridorAcross*skin,
                a-roomAcross*skin-Vector3.up*skin, b+roomAcross*skin-Vector3.up*skin,
                d+corridorAcross*skin-Vector3.up*skin, c-corridorAcross*skin-Vector3.up*skin }, floor.sharedMaterial, meshDirectory);
            FittedSolid(connector, definition.Id, "FittedCeiling", new[] {
                at-roomAcross*skin, bt+roomAcross*skin, dt+corridorAcross*skin, ct-corridorAcross*skin,
                at-roomAcross*skin+Vector3.up*skin, bt+roomAcross*skin+Vector3.up*skin,
                dt+corridorAcross*skin+Vector3.up*skin, ct-corridorAcross*skin+Vector3.up*skin }, ceiling.sharedMaterial, meshDirectory);
            FittedSolid(connector, definition.Id, "FittedWallMinimum", new[] {
                a, c, ct, at, a-roomAcross*skin, c-corridorAcross*skin, ct-corridorAcross*skin, at-roomAcross*skin }, wallA.sharedMaterial, meshDirectory);
            FittedSolid(connector, definition.Id, "FittedWallMaximum", new[] {
                b, d, dt, bt, b+roomAcross*skin, d+corridorAcross*skin, dt+corridorAcross*skin, bt+roomAcross*skin }, wallB.sharedMaterial, meshDirectory);
            if (definition.Id == "EngineRoom_H01" || (meshDirectory==ArmorySampleDirectory+"/Meshes" && definition.Id=="EngineRoom_H05"))
            {
                var backA = FittedWallCorner(profile.MinimumWall, profile.MaximumWall, floor.bounds.center, false);
                var backB = FittedWallCorner(profile.MaximumWall, profile.MinimumWall, floor.bounds.center, false);
                backA.y = backB.y = roomFloor;
                // The circular room deck stops before the rectangular entrance: bridge its full footprint at deck height.
                var aa = a - roomAcross * skin; var bb = b + roomAcross * skin;
                var ba = backA - roomAcross * skin; var bbk = backB + roomAcross * skin;
                FittedSolid(connector, definition.Id, "FittedEntranceDeck", new[] {
                    ba, bbk, bb, aa, ba-Vector3.up*skin, bbk-Vector3.up*skin,
                    bb-Vector3.up*skin, aa-Vector3.up*skin }, floor.sharedMaterial, meshDirectory);
            }
        }

        private static void FittedSolid(Transform parent, string endpoint, string name, Vector3[] world, Material material, string meshDirectory = TriRoomConnectorSampleMeshDirectory)
        {
            var center = Vector3.zero; foreach (var point in world) center += point / 8f;
            var faces = new[] { new[] {0,1,2,3}, new[] {4,7,6,5}, new[] {0,4,5,1},
                new[] {1,5,6,2}, new[] {2,6,7,3}, new[] {3,7,4,0} };
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            foreach (var face in faces)
            {
                var normal = Vector3.Cross(world[face[1]]-world[face[0]], world[face[2]]-world[face[0]]);
                var middle = (world[face[0]]+world[face[1]]+world[face[2]]+world[face[3]])*.25f;
                if (Vector3.Dot(normal, middle-center) < 0) Array.Reverse(face);
                var start = vertices.Count;
                foreach (var index in face) { vertices.Add(parent.InverseTransformPoint(world[index])); uv.Add(new Vector2(world[index].x, world[index].z+world[index].y)); }
                triangles.AddRange(new[] {start,start+1,start+2,start,start+2,start+3});
            }
            var mesh = new Mesh { name = "Mesh_" + endpoint + "_" + name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0,uv); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var path = meshDirectory + "/" + mesh.name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
            else
            {
                if(meshDirectory==ArmorySampleDirectory+"/Meshes")
                {saved.Clear();saved.vertices=mesh.vertices;saved.triangles=mesh.triangles;saved.uv=mesh.uv;saved.normals=mesh.normals;saved.RecalculateBounds();saved.UploadMeshData(false);}
                else EditorUtility.CopySerialized(mesh,saved);
                UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
            }
            var part = new GameObject(name); Undo.RegisterCreatedObjectUndo(part, "Fitted connector part");
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = saved;
            part.AddComponent<MeshRenderer>().sharedMaterial = material;
            part.AddComponent<MeshCollider>().sharedMesh = saved;
        }

        [Serializable]
        private sealed class ConnectorLiveOperation
        {
            public string action;
            public int index;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 movement;
            public float seconds;
        }
        private static CharacterController connectorReviewBody;
        private static Camera connectorReviewCamera;
        private static Vector3 connectorReviewMovement;
        private static double connectorReviewUntil;
        private static double connectorReviewLastTick;

        internal static void ReviewApprovedConnectorTransfer()
        {
            var directory = Path.Combine(ProjectRoot, TriRoomConnectorSampleValidationDirectory, "TransferReview");
            Directory.CreateDirectory(directory);
            var operation = JsonUtility.FromJson<ConnectorLiveOperation>(File.ReadAllText(Path.Combine(ProjectRoot,
                TriRoomConnectorSampleValidationDirectory, "LiveReview", "Operation.json")));
            if (operation.action == "sample" || operation.action == "production")
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before switching comparison scenes.");
                var path = operation.action == "sample" ? TriRoomConnectorSampleScenePath : TargetScenePath;
                var current = SceneManager.GetActiveScene();
                if (current.isDirty) throw new InvalidOperationException("Unsaved scene edits must be preserved.");
                var next = SceneManager.GetSceneByPath(path);
                if (!next.IsValid() || !next.isLoaded) next = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(next);
                if (current.path != path && (current.path == TargetScenePath || current.path == TriRoomConnectorSampleScenePath))
                    EditorSceneManager.CloseScene(current,true);
                return;
            }
            if (operation.action == "enter") { EditorApplication.isPlaying = true; return; }
            if (operation.action == "exit") { EditorApplication.isPlaying = false; return; }
            if (operation.action == "parkObstructingCorridors")
            {
                var target = SceneManager.GetActiveScene();
                if (EditorApplication.isPlayingOrWillChangePlaymode || target.path != TargetScenePath || target.isDirty)
                    throw new InvalidOperationException("Parking requires the saved Pegasus scene in Edit Mode.");
                var root = RequireSceneObject(target, CorridorRootName).transform;
                var ceilingRoot = root.Find("ShipSpaceCeiling_AllCorridors");
                var moving = new List<Transform>();
                foreach (var id in new[] {"SC-H03", "SC-S01", "SC-S02"})
                {
                    var module = root.Find(id + (id.Contains("H") ? " horizontal corridor sample" : " sloped corridor sample"));
                    var ceiling = ceilingRoot.Find("ShipSpaceCeiling_" + id.Replace('-','_') + (id.Contains("H") ? "_horizontal_corridor_sample_Slab_01" : "_sloped_corridor_sample_Sloped_Slab_01"));
                    if (module == null || ceiling == null) throw new InvalidOperationException("Missing exact corridor/ceiling pair: "+id);
                    moving.Add(module); moving.Add(ceiling);
                }
                var occupied = new Bounds(root.position, Vector3.zero);
                foreach(var sceneRoot in target.GetRootGameObjects())
                    if (sceneRoot.name.StartsWith("Approved ") || sceneRoot.name == "Intermediate Connectors")
                        foreach(var renderer in sceneRoot.GetComponentsInChildren<Renderer>(true)) occupied.Encapsulate(renderer.bounds);
                var cursor = occupied.max.x + 20f;
                var report = new StringBuilder("Temporary parking; only world positions changed.\n");
                Undo.RecordObjects(moving.ToArray(),"Park three obstructing corridors");
                for(var i=0;i<moving.Count;i+=2)
                {
                    var bounds = new Bounds(moving[i].position,Vector3.zero);
                    foreach(var renderer in moving[i].GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
                    foreach(var renderer in moving[i+1].GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
                    var delta = new Vector3(cursor-bounds.min.x,0,occupied.max.z+20f-bounds.min.z);
                    foreach(var t in new[] {moving[i],moving[i+1]})
                    { report.AppendLine(AnimationUtility.CalculateTransformPath(t,null)+" FROM="+t.position.ToString("F5")+" TO="+(t.position+delta).ToString("F5")); t.position += delta; }
                    cursor += bounds.size.x + 10f;
                }
                EditorSceneManager.MarkSceneDirty(target);
                if (!EditorSceneManager.SaveScene(target)) throw new InvalidOperationException("Pegasus save failed.");
                File.WriteAllText(Path.Combine(directory,"ParkedCorridors.txt"),report.ToString(),Encoding.UTF8);
                return;
            }
            if (operation.action == "inspectOverlap")
            {
                var report = new StringBuilder("Visible overlap identification; not a quality pass.\n");
                var intersect = typeof(HandleUtility).GetMethod("IntersectRayMesh", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var go = new GameObject("__ReadOnlyRayCamera") { hideFlags = HideFlags.HideAndDontSave };
                var camera = go.AddComponent<Camera>(); camera.fieldOfView = 60; camera.aspect = 1920f/1080;
                camera.transform.SetPositionAndRotation(operation.position, Quaternion.Euler(operation.rotation));
                foreach (var point in new[] {new Vector3(.51f,.48f,0),new Vector3(.80f,.30f,0),new Vector3(.43f,.30f,0)})
                {
                    var ray = camera.ViewportPointToRay(point);
                    var hits = new List<KeyValuePair<float,string>>();
                    foreach (var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                    {
                        var renderer = filter.GetComponent<Renderer>();
                        if (renderer == null || !renderer.enabled || filter.sharedMesh == null || !renderer.bounds.IntersectRay(ray)) continue;
                        object[] args = {ray,filter.sharedMesh,filter.transform.localToWorldMatrix,new RaycastHit()};
                        if ((bool)intersect.Invoke(null,args))
                        {
                            var hit = (RaycastHit)args[3];
                            hits.Add(new KeyValuePair<float,string>(hit.distance,AnimationUtility.CalculateTransformPath(filter.transform,null)));
                        }
                    }
                    report.AppendLine("Viewport="+point);
                    hits.Sort((a,b)=>a.Key.CompareTo(b.Key));
                    for (var h=0;h<Mathf.Min(8,hits.Count);h++) report.AppendLine(hits[h].Key+" "+hits[h].Value);
                }
                UnityEngine.Object.DestroyImmediate(go);
                File.WriteAllText(Path.Combine(directory,"Overlap_"+operation.index+".txt"),report.ToString(),Encoding.UTF8);
                return;
            }
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Comparison observation requires actual Play Mode.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != TargetScenePath && scene.path != TriRoomConnectorSampleScenePath)
                throw new InvalidOperationException("Wrong review scene.");
            if (connectorReviewCamera == null)
            {
                var go = new GameObject("__ApprovedTransferObservationCamera") { hideFlags = HideFlags.DontSave };
                connectorReviewCamera = go.AddComponent<Camera>();
                connectorReviewCamera.nearClipPlane = .03f;
                connectorReviewCamera.farClipPlane = 500f;
                connectorReviewCamera.fieldOfView = 60f;
                connectorReviewCamera.depth = 100;
            }
            var rooms = RequireTriRoomConnectorRooms(scene);
            var corridorRoot = RequireSceneObject(scene, CorridorRootName);
            var modules = RequireTriRoomConnectorModules(corridorRoot.transform);
            var position = operation.position; var rotation = Quaternion.Euler(operation.rotation);
            var label = "View_" + operation.index.ToString("D2");
            if (operation.index == 0) { position = new Vector3(-38.60876f,4.9f,13.78017f); rotation = Quaternion.Euler(6.84277f,19.38635f,0); }
            else if (operation.index == 1) { position = new Vector3(73.31202f,4.58746f,23.06869f); rotation = Quaternion.Euler(358.6257f,86.84657f,359.9157f); }
            else if (operation.index >= 2 && operation.index <= 5)
            {
                var endpointIndex = new[] {1,2,4,5}[operation.index-2];
                var definition = TriRoomConnectorEndpoints[endpointIndex];
                var profile = BuildConnectorEntranceProfile(scene,rooms,modules[definition.CorridorId],definition);
                position = profile.PlaneCenter - profile.Entrance.Outward * 1.3f;
                position.y = profile.Entrance.FloorY + 1.45f;
                rotation = Quaternion.LookRotation(profile.Entrance.Outward + Vector3.down*.08f,Vector3.up);
            }
            else if (operation.index == 6) { position = new Vector3(23,95,26); rotation = Quaternion.Euler(90,0,0); }
            else if (operation.index == 7) { position = new Vector3(-37.4f,4.8f,7.5f); rotation = Quaternion.Euler(8,270,0); }
            else if (operation.index == 8) { position = new Vector3(6.8f,4.27f,33); rotation = Quaternion.Euler(10,180,0); }
            else if (operation.index == 9) { position = new Vector3(88.3f,4.28f,23); rotation = Quaternion.Euler(10,270,0); }
            else if (operation.index >= 10 && operation.index <= 12)
            {
                var module = modules[new[] {"SC-H01","SC-H02","SC-H05"}[operation.index-10]];
                var floor = RequireUniqueCorridorFloorRenderer(module);
                position = floor.bounds.center; position.y = floor.bounds.max.y+1.6f;
                rotation = Quaternion.LookRotation(module.right, Vector3.up);
            }
            connectorReviewCamera.transform.SetPositionAndRotation(position, rotation);
            var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            game.Focus(); game.Repaint();
            var prefix = scene.path == TargetScenePath ? "Pegasus" : "Sample";
            var due = EditorApplication.timeSinceStartup+.8;
            EditorApplication.CallbackFunction capture = null;
            capture = () =>
            {
                if (EditorApplication.timeSinceStartup < due) return;
                EditorApplication.update -= capture;
                // Capture the real runtime Game View framebuffer, not desktop pixels from overlapping windows.
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,prefix+"_"+label+".png"));
                File.WriteAllText(Path.Combine(directory,prefix+"_"+label+".txt"),
                    "Actual Game View / PlayMode="+EditorApplication.isPlaying+"\nScene="+scene.path+
                    "\nCamera="+position+"\nRotation="+rotation.eulerAngles+"\nNo lights, materials or target visibility altered.",Encoding.UTF8);
            };
            EditorApplication.update += capture;
        }

        internal static void OperateConnectorLiveReview()
        {
            var directory = Path.Combine(ProjectRoot, TriRoomConnectorSampleValidationDirectory, "LiveReview");
            var operation = JsonUtility.FromJson<ConnectorLiveOperation>(File.ReadAllText(Path.Combine(directory, "Operation.json")));
            if (SceneManager.GetActiveScene().path != TriRoomConnectorSampleScenePath)
                throw new InvalidOperationException("Live review is restricted to the connector sample.");
            if (operation.action == "scene")
            {
                var view = SceneView.lastActiveSceneView;
                var rotation = Quaternion.Euler(operation.rotation);
                view.orthographic = false;
                view.LookAtDirect(operation.position + rotation * Vector3.forward * 2f, rotation, 1f);
                view.Focus(); view.Repaint();
                return;
            }
            if (operation.action == "enter")
            {
                if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Unsaved scene changes must be preserved.");
                EditorApplication.isPlaying = true; return;
            }
            if (operation.action == "exit")
            {
                EditorApplication.update -= TickConnectorLiveReview;
                EditorApplication.isPlaying = false; return;
            }
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Runtime review requires Play Mode.");
            if (operation.action == "spawn")
            {
                if (connectorReviewBody != null) UnityEngine.Object.DestroyImmediate(connectorReviewBody.gameObject);
                var settings = AssetDatabase.LoadAssetAtPath<Bellerophon.Core.Player.FirstPersonPlayerSettings>(PlayerSettingsPath);
                var body = new GameObject("__ConnectorLiveCollisionObserver") { hideFlags = HideFlags.DontSave };
                body.transform.position = operation.position;
                connectorReviewBody = body.AddComponent<CharacterController>();
                connectorReviewBody.height = settings.StandingHeight;
                connectorReviewBody.radius = settings.CharacterRadius;
                connectorReviewBody.center = Vector3.up * settings.StandingHeight * .5f;
                var cameraObject = new GameObject("__ConnectorLiveCamera") { hideFlags = HideFlags.DontSave };
                cameraObject.transform.SetParent(body.transform, false);
                cameraObject.transform.localPosition = Vector3.up * settings.CameraStandingHeight;
                connectorReviewCamera = cameraObject.AddComponent<Camera>();
                connectorReviewCamera.nearClipPlane = .03f;
                connectorReviewCamera.fieldOfView = 60f;
                connectorReviewCamera.transform.rotation = Quaternion.Euler(operation.rotation);
                connectorReviewUntil = 0;
                connectorReviewLastTick = EditorApplication.timeSinceStartup;
                EditorApplication.update -= TickConnectorLiveReview;
                EditorApplication.update += TickConnectorLiveReview;
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            }
            if (operation.action == "look") connectorReviewCamera.transform.rotation = Quaternion.Euler(operation.rotation);
            if (operation.action == "move")
            {
                connectorReviewMovement = operation.movement;
                connectorReviewUntil = EditorApplication.timeSinceStartup + operation.seconds;
            }
            if (operation.action == "capture")
            {
                var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                game.Focus(); game.Repaint();
                var rect = game.position;
                var texture = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
                texture.SetPixels(UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(new Vector2(rect.x,rect.y), (int)rect.width,(int)rect.height));
                texture.Apply();
                File.WriteAllBytes(Path.Combine(directory,"LiveGameView.png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                File.WriteAllText(Path.Combine(directory,"RuntimeObservation.txt"),
                    "Live Play Mode collision observer; not an automatic acceptance result.\nPosition=" + connectorReviewBody.transform.position.ToString("F5") +
                    "\nCamera=" + connectorReviewCamera.transform.position.ToString("F5"), Encoding.UTF8);
            }
        }

        private static void TickConnectorLiveReview()
        {
            if (!EditorApplication.isPlaying || connectorReviewBody == null)
            { EditorApplication.update -= TickConnectorLiveReview; return; }
            var now = EditorApplication.timeSinceStartup;
            var delta = Mathf.Min(.03f, (float)(now - connectorReviewLastTick));
            connectorReviewLastTick = now;
            var movement = now < connectorReviewUntil ? connectorReviewMovement : Vector3.zero;
            // Actual scene collisions are used; the project's editor free-flight option is not used.
            connectorReviewBody.Move((movement + Vector3.down * 2f) * delta);
        }

        internal static void CreatePegasusTriRoomConnectorSample()
        {
            CreateLegacyTriRoomConnectorSample();
        }

        private const string ArmorySampleDirectory = "Assets/_Project/ArtSamples/PegasusArmorySupplyConnections";
        internal static void ResizePegasusArmorySupplyTriangle()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=ArmorySampleScene||scene.isDirty)
                throw new InvalidOperationException("Requires saved isolated sample in Edit Mode.");
            var reportPath=Path.Combine(ProjectRoot,ArmoryReviewDirectory,"TriangleAfter.txt");
            var alreadyResized=File.Exists(reportPath);
            var rooms=RequireTriRoomConnectorRooms(scene);
            var corridorRoot=RequireSceneObject(scene,CorridorRootName).transform;
            var modules=RequireTriRoomConnectorModules(corridorRoot);
            var ceilingRoot=corridorRoot.Find(CorridorCeilingRootName);
            var mouths=new Vector3[6];var lengths=new float[3];
            for(var i=0;i<6;i++)mouths[i]=TriangleMouth(scene,i);
            for(var i=0;i<3;i++)
            {
                var module=modules[TriRoomConnectorEndpoints[i*2].CorridorId];
                GetRendererProjectionRange(RequireUniqueCorridorFloorRenderer(module),HorizontalDirection(module.right),out var lo,out var hi);
                lengths[i]=(hi-lo)*(alreadyResized?1f:.25f);
            }
            var cockpit=rooms["Cockpit"].transform;var engine=rooms["EngineRoom"].transform;var control=rooms["ControlRoom"].transform;
            var cockpitBefore=cockpit.position;var engineBefore=engine.position;var controlBefore=control.position;
            var rotations=new[]{cockpit.rotation,engine.rotation,control.rotation};
            // Two metre end transitions keep angled existing mouths outside the shortened bodies.
            const float endGap=2f;
            var cockpitCircle=mouths[3]-(mouths[2]-cockpitBefore);cockpitCircle.y=cockpitBefore.y;
            var newCockpit=cockpitCircle+new Vector3(-1,0,1).normalized*(lengths[1]+2*endGap);
            var firstCenter=mouths[5]-(mouths[4]-engineBefore);firstCenter.y=engineBefore.y;
            var secondCenter=newCockpit+(mouths[1]-cockpitBefore)-(mouths[0]-engineBefore);secondCenter.y=engineBefore.y;
            var delta=secondCenter-firstCenter;delta.y=0;var distance=delta.magnitude;
            var radius=lengths[2]+2*endGap;var otherRadius=lengths[0]+2*endGap;
            var along=(radius*radius-otherRadius*otherRadius+distance*distance)/(2*distance);
            if(along*along>radius*radius)throw new InvalidOperationException("Fixed-angle layout has no circle intersection.");
            var direction=delta/distance;var perpendicular=new Vector3(-direction.z,0,direction.x);
            var side=Mathf.Sqrt(radius*radius-along*along);
            var candidateA=firstCenter+direction*along+perpendicular*side;
            var candidateB=firstCenter+direction*along-perpendicular*side;
            var newEngine=candidateA.z<candidateB.z?candidateA:candidateB;
            if(!alreadyResized)File.Copy(Path.Combine(ProjectRoot,ArmorySampleScene),Path.Combine(ProjectRoot,ArmoryReviewDirectory,"BeforeTriangleResize.unity.txt"),false);
            Undo.IncrementCurrentGroup();var undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Shorten isolated sample triangle to 25 percent");
            foreach(var root in scene.GetRootGameObjects())
                if(root.name=="Approved Cockpit 01 Structure"||root.name=="Approved Engine Room 01 Shell"||root.name==CorridorRootName||root.name=="Intermediate Connectors")Undo.RegisterFullObjectHierarchyUndo(root,"Triangle sample resize");
            try
            {
                cockpit.position=newCockpit;engine.position=newEngine;
                for(var i=0;i<3;i++)
                {
                    var module=modules[TriRoomConnectorEndpoints[i*2].CorridorId];
                    var targetLocal=lengths[i]/module.lossyScale.x;
                    var ribs=new List<Transform>();foreach(Transform child in module)if(IsOriginalHorizontalCorridorRib(child.name))ribs.Add(child);
                    ribs.Sort((a,b)=>a.localPosition.x.CompareTo(b.localPosition.x));
                    for(var j=0;j<ribs.Count;j++){var pos=ribs[j].localPosition;pos.x=Mathf.Lerp(-targetLocal*.5f+.9f,targetLocal*.5f-.9f,(j+1f)/(ribs.Count+1));ribs[j].localPosition=pos;}
                    ResizeHorizontalCorridorModule(module,ceilingRoot,targetLocal);
                    var roof=CaptureCorridorCeilingFollowers(module,ceilingRoot)[0].Transform;
                    var relativePosition=module.InverseTransformPoint(roof.position);var relativeRotation=Quaternion.Inverse(module.rotation)*roof.rotation;
                    var from=TriangleMouth(scene,i*2);var to=TriangleMouth(scene,i*2+1);var axis=HorizontalDirection(to-from);
                    module.rotation=Quaternion.FromToRotation(HorizontalDirection(module.right),axis)*module.rotation;
                    roof.SetPositionAndRotation(module.TransformPoint(relativePosition),module.rotation*relativeRotation);
                    var floor=RequireUniqueCorridorFloorRenderer(module);var target=(from+to)*.5f;target.y=floor.bounds.center.y;
                    var move=target-floor.bounds.center;module.position+=move;roof.position+=move;
                }
                var connectors=RequireSceneObject(scene,"Intermediate Connectors").transform;
                for(var i=0;i<6;i++)
                {
                    var def=TriRoomConnectorEndpoints[i];var holder=connectors.Find("Connector_"+def.Id);
                    if(holder==null)throw new InvalidOperationException("Missing connector "+def.Id);
                    while(holder.childCount>0)Undo.DestroyObjectImmediate(holder.GetChild(0).gameObject);
                    holder.SetPositionAndRotation(Vector3.zero,Quaternion.identity);holder.localScale=Vector3.one;
                    BuildFittedConnectorContact(holder,def,BuildConnectorEntranceProfile(scene,rooms,modules[def.CorridorId],def),modules[def.CorridorId],ceilingRoot,ArmorySampleDirectory+"/Meshes");
                    var profile=BuildConnectorEntranceProfile(scene,rooms,modules[def.CorridorId],def);
                    TriangleWallSeams(holder,profile.MinimumWall,profile.Floor,profile.Ceiling,def.Id+"A");
                    TriangleWallSeams(holder,profile.MaximumWall,profile.Floor,profile.Ceiling,def.Id+"B");
                    if(i%2==0)
                    {
                        var module=modules[def.CorridorId];var floor=RequireUniqueCorridorFloorRenderer(module);
                        var roof=RequireSingleRenderer(CaptureCorridorCeilingFollowers(module,ceilingRoot)[0].Transform);
                        TriangleWallSeams(holder,RequireCorridorSurfaceRenderer(module," left armored wall"),floor,roof,def.CorridorId+"A");
                        TriangleWallSeams(holder,RequireCorridorSurfaceRenderer(module," right armored wall"),floor,roof,def.CorridorId+"B");
                    }
                }
                if(control.position!=controlBefore||cockpit.rotation!=rotations[0]||engine.rotation!=rotations[1]||control.rotation!=rotations[2])throw new InvalidOperationException("Protected transforms changed.");
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ArmorySampleScene);
                var report=new StringBuilder("Construction only; direct enclosure review pending.\n");
                report.AppendLine("Cockpit="+cockpit.position+" Engine="+engine.position+" Control="+control.position);
                for(var i=0;i<3;i++)report.AppendLine(TriRoomConnectorEndpoints[i*2].CorridorId+" Length="+lengths[i]+" WalkSeconds="+lengths[i]/4f);
                File.WriteAllText(reportPath,report.ToString(),Encoding.UTF8);
            }
            catch{Undo.RevertAllDownToGroup(undoGroup);throw;}
        }

        private static Vector3 TriangleMouth(Scene scene,int index)
        {
            var def=TriRoomConnectorEndpoints[index];var rooms=RequireTriRoomConnectorRooms(scene);
            var modules=RequireTriRoomConnectorModules(RequireSceneObject(scene,CorridorRootName).transform);
            var p=BuildConnectorEntranceProfile(scene,rooms,modules[def.CorridorId],def);
            var destination=p.Entrance.Point+p.Entrance.Outward*100;
            var result=(FittedWallCorner(p.MinimumWall,p.MaximumWall,destination,true)+FittedWallCorner(p.MaximumWall,p.MinimumWall,destination,true))*.5f;
            if(def.RoomId=="Cockpit")
            {
                var outward=def.Id=="Cockpit_H01"?Vector3.left:Vector3.right;
                result=(TriangleCockpitJamb(p.MinimumWall,p.MaximumWall,outward)+TriangleCockpitJamb(p.MaximumWall,p.MinimumWall,outward))*.5f;
            }
            result.y=p.Floor.bounds.max.y;return result;
        }

        private static Vector3 TriangleCockpitJamb(Renderer wall,Renderer other,Vector3 outward)
        {
            outward=-outward; // Start inside the jamb thickness, sharing the room deck/roof boundary.
            var inward=HorizontalDirection(other.bounds.center-wall.bounds.center);
            var best=float.NegativeInfinity;var lateral=float.NegativeInfinity;var result=Vector3.zero;
            foreach(var vertex in wall.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                var point=wall.transform.TransformPoint(vertex);var end=Vector3.Dot(point,outward);var side=Vector3.Dot(point,inward);
                if(end>best+.001f||(Mathf.Abs(end-best)<.001f&&side>lateral)){best=end;lateral=side;result=point;}
            }
            return result;
        }

        private static void TriangleWallSeams(Transform parent,Renderer wall,Renderer floor,Renderer roof,string id)
        {
            var mesh=wall.GetComponent<MeshFilter>().sharedMesh;var axis=Vector3.right;var longest=0f;
            foreach(var local in new[]{Vector3.right*mesh.bounds.size.x,Vector3.up*mesh.bounds.size.y,Vector3.forward*mesh.bounds.size.z})
            {var world=wall.transform.TransformVector(local);world.y=0;if(world.sqrMagnitude>longest){longest=world.sqrMagnitude;axis=world.normalized;}}
            var across=Vector3.Cross(Vector3.up,axis);var lo=float.PositiveInfinity;var hi=float.NegativeInfinity;var low=lo;var high=hi;
            foreach(var vertex in mesh.vertices){var p=wall.transform.TransformPoint(vertex);lo=Mathf.Min(lo,Vector3.Dot(p,axis));hi=Mathf.Max(hi,Vector3.Dot(p,axis));low=Mathf.Min(low,Vector3.Dot(p,across));high=Mathf.Max(high,Vector3.Dot(p,across));}
            foreach(var range in new[]{new Vector2(floor.bounds.max.y-.01f,wall.bounds.min.y+.01f),new Vector2(wall.bounds.max.y-.01f,roof.bounds.min.y+.01f)})
            {
                if(range.y<=range.x)continue;
                var a=axis*lo+across*low;var b=axis*hi+across*low;var c=axis*hi+across*high;var d=axis*lo+across*high;
                FittedSolid(parent,id,range.x<floor.bounds.max.y?"BaseSeal":"HeaderSeal",new[]{a+Vector3.up*range.x,b+Vector3.up*range.x,c+Vector3.up*range.x,d+Vector3.up*range.x,a+Vector3.up*range.y,b+Vector3.up*range.y,c+Vector3.up*range.y,d+Vector3.up*range.y},wall.sharedMaterial,ArmorySampleDirectory+"/Meshes");
            }
        }
        private const string ArmorySampleScene = ArmorySampleDirectory + "/PegasusArmorySupplyConnections.unity";
        private const string ArmoryReviewDirectory = "docs/validation/PegasusArmorySupplyConnections";
        private static readonly string[] ArmoryContactWalls = {
            "CR-01 weapon room south attached corridor side wall +0.92", "CR-01 weapon room south attached corridor side wall -0.92",
            "AR-08 control room south corridor side wall +0.92", "AR-08 control room south corridor side wall -0.92",
            "AR-07 supply room east corridor side wall +0.92", "AR-07 supply room east corridor side wall -0.92",
            "SR-09 armory direction corridor lower side wall", "SR-09 armory direction corridor upper side wall" };
        private static readonly string[] ArmoryContactFloors = {
            "CR-01 weapon room south attached corridor floor continuation", "AR-08 control room south corridor floor continuation",
            "AR-07 supply room east corridor floor continuation", "SR-09 armory direction corridor floor continuation" };
        private static readonly string[] ArmoryContactRooms = {"Approved Control Room 01 Shell","Approved Armory 01 Shell","Approved Armory 01 Shell","Approved Supply Room 01 Shell"};

        private static ConnectorEntranceProfile ArmoryContact(Scene scene, int index)
        {
            var a = RequireRenderer(scene,ArmoryContactWalls[index*2]);
            var b = RequireRenderer(scene,ArmoryContactWalls[index*2+1]);
            var floor = RequireRenderer(scene,ArmoryContactFloors[index]);
            var room = RequireSceneObject(scene,ArmoryContactRooms[index]);
            var mesh = a.GetComponent<MeshFilter>().sharedMesh.bounds;
            var x = a.transform.TransformVector(Vector3.right*mesh.size.x);
            var z = a.transform.TransformVector(Vector3.forward*mesh.size.z);
            var y = a.transform.TransformVector(Vector3.up*mesh.size.y);
            x.y=0;z.y=0;y.y=0;if(y.sqrMagnitude>x.sqrMagnitude)x=y;
            var outward = HorizontalDirection(x.sqrMagnitude > z.sqrMagnitude ? x : z);
            if(Vector3.Dot(outward,floor.bounds.center-room.transform.position)<0) outward=-outward;
            var mouth = (FittedWallCorner(a,b,floor.bounds.center+outward*100,true)+FittedWallCorner(b,a,floor.bounds.center+outward*100,true))*.5f;
            mouth.y = floor.bounds.max.y;
            Renderer ceiling=null; var distance=float.PositiveInfinity;
            foreach(var r in room.GetComponentsInChildren<Renderer>(true))
                if(r.name.Contains("Entrance_Slab"))
                {
                    var delta=r.bounds.center-floor.bounds.center; delta.y=0;
                    if(delta.sqrMagnitude<distance){distance=delta.sqrMagnitude;ceiling=r;}
                }
            if(ceiling==null) throw new InvalidOperationException("Missing entrance ceiling: "+index);
            return new ConnectorEntranceProfile(new EntranceConnectionAnchor("Contact_"+index,mouth,outward,mouth.y),Vector3.Cross(Vector3.up,outward),mouth,floor,ceiling,a,b,0,0,0,0,0,0);
        }

        internal static void CreatePegasusArmorySupplySample()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            if(File.Exists(Path.Combine(ProjectRoot,ArmorySampleScene))) throw new InvalidOperationException("Existing sample must not be overwritten.");
            var source=SceneManager.GetSceneByPath(TargetScenePath);
            if(!source.IsValid()||!source.isLoaded) source=EditorSceneManager.OpenScene(TargetScenePath,OpenSceneMode.Additive);
            for(var s=SceneManager.sceneCount-1;s>=0;s--)
            {
                var partial=SceneManager.GetSceneAt(s);
                if(partial.path!="")continue;
                foreach(var candidate in partial.GetRootGameObjects())
                    if(candidate.name=="Armory Supply Intermediate Connectors") {EditorSceneManager.CloseScene(partial,true);break;}
            }
            if(source.isDirty)
            {
                var saved=EditorSceneManager.OpenPreviewScene(TargetScenePath);
                try
                {
                    var liveSignature=ArmorySourceState(source);var savedSignature=ArmorySourceState(saved);
                    Directory.CreateDirectory(Path.Combine(ProjectRoot,ArmoryReviewDirectory));
                    File.WriteAllText(Path.Combine(ProjectRoot,ArmoryReviewDirectory,"SourceDirtyInvestigation.txt"),
                        "Live dirty="+source.isDirty+"\nSerialized object state matches saved scene="+(liveSignature==savedSignature)+
                        "\nNo production save performed. Earlier read-only ray inspection created and destroyed an editor camera in this scene, which can mark it dirty.\n",Encoding.UTF8);
                    if(liveSignature!=savedSignature)
                    {
                        var liveLines=new HashSet<string>(liveSignature.Split('\n'));var savedLines=new HashSet<string>(savedSignature.Split('\n'));
                        var differences=new StringBuilder();
                        foreach(var line in liveLines)if(!savedLines.Contains(line))differences.AppendLine("LIVE "+line);
                        foreach(var line in savedLines)if(!liveLines.Contains(line))differences.AppendLine("DISK "+line);
                        File.WriteAllText(Path.Combine(ProjectRoot,ArmoryReviewDirectory,"SourceDifferences.txt"),differences.ToString(),Encoding.UTF8);
                        throw new InvalidOperationException("Actual unsaved scene differences preserved; see source investigation.");
                    }
                    // Only clear a bookkeeping flag when every serialized scene object matches disk.
                    EditorSceneManager.CloseScene(source,true);
                    source=EditorSceneManager.OpenScene(TargetScenePath,OpenSceneMode.Additive);
                }
                finally { EditorSceneManager.ClosePreviewScene(saved); }
            }
            EnsureAssetFolder(ArmorySampleDirectory); EnsureAssetFolder(ArmorySampleDirectory+"/Meshes");
            var sample=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Additive);
            SceneManager.SetActiveScene(sample);
            foreach(var name in new[]{"Approved Engine Room 01 Shell","Approved Cockpit 01 Structure","Approved Control Room 01 Shell","Approved Armory 01 Shell","Approved Supply Room 01 Shell","Intermediate Connectors"})
            {
                var original=RequireSceneObject(source,name);
                var clone=UnityEngine.Object.Instantiate(original); clone.name=name;
                SceneManager.MoveGameObjectToScene(clone,sample);
            }
            var corridorRoot=new GameObject(CorridorRootName).transform;
            var ceilingRoot=new GameObject(CorridorCeilingRootName).transform; ceilingRoot.SetParent(corridorRoot,false);
            var sourceCorridors=RequireSceneObject(source,CorridorRootName).transform;
            foreach(var id in new[]{"SC-H01","SC-H02","SC-H03","SC-H04","SC-H05"})
            {
                var original=sourceCorridors.Find(id+" horizontal corridor sample");
                var clone=UnityEngine.Object.Instantiate(original.gameObject,corridorRoot,true); clone.name=original.name;
                var roof=sourceCorridors.Find(CorridorCeilingRootName+"/ShipSpaceCeiling_"+id.Replace('-','_')+"_horizontal_corridor_sample_Slab_01");
                var copiedRoof=UnityEngine.Object.Instantiate(roof.gameObject,ceilingRoot,true); copiedRoof.name=roof.name;
            }
            var report=new StringBuilder("Isolated sample; Pegasus unchanged.\n");
            var connectorRoot=new GameObject("Armory Supply Intermediate Connectors").transform;
            var speed=AssetDatabase.LoadAssetAtPath<Bellerophon.Core.Player.FirstPersonPlayerSettings>(PlayerSettingsPath).WalkSpeed;
            for(var link=0;link<2;link++)
            {
                var fromIndex=link*2; var toIndex=fromIndex+1;
                var from=ArmoryContact(sample,fromIndex); var to=ArmoryContact(sample,toIndex);
                var direction=from.Entrance.Outward;
                var destination=RequireSceneObject(sample,ArmoryContactRooms[toIndex]).transform;
                destination.rotation=Quaternion.FromToRotation(to.Entrance.Outward,-direction)*destination.rotation;
                to=ArmoryContact(sample,toIndex);
                var module=corridorRoot.Find("SC-H0"+(link+3)+" horizontal corridor sample");
                ResizeHorizontalCorridorModule(module,ceilingRoot,speed*(link==0?4f:3f)/module.lossyScale.x);
                var roof=CaptureCorridorCeilingFollowers(module,ceilingRoot)[0].Transform;
                var relativePosition=module.InverseTransformPoint(roof.position);
                var relativeRotation=Quaternion.Inverse(module.rotation)*roof.rotation;
                module.rotation=Quaternion.FromToRotation(HorizontalDirection(module.right),direction)*module.rotation;
                roof.SetPositionAndRotation(module.TransformPoint(relativePosition),module.rotation*relativeRotation);
                var floor=RequireUniqueCorridorFloorRenderer(module);
                GetRendererProjectionRange(floor,direction,out var low,out var high);
                var start=floor.bounds.center+direction*(low-Vector3.Dot(floor.bounds.center,direction));start.y=floor.bounds.max.y;
                var delta=from.Entrance.Point+direction-start;
                module.position+=delta;roof.position+=delta;
                var end=from.Entrance.Point+direction*(2f+high-low);
                destination.position+=end-to.Entrance.Point;
                for(var contact=fromIndex;contact<=toIndex;contact++)
                {
                    var profile=ArmoryContact(sample,contact);
                    var holder=new GameObject("Contact_"+contact).transform;holder.SetParent(connectorRoot,false);
                    BuildArmoryFittedContact(holder,profile,module,roof,contact);
                    report.AppendLine("Contact="+contact+" Room="+ArmoryContactRooms[contact]+" Mouth="+profile.Entrance.Point+" Direction="+profile.Entrance.Outward+" Ceiling="+profile.Ceiling.name);
                }
                report.AppendLine(module.name+" BodyLength="+(high-low)+" WalkingSeconds="+(link==0?4:3));
            }
            AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(sample,ArmorySampleScene)) throw new InvalidOperationException("Sample save failed.");
            Directory.CreateDirectory(Path.Combine(ProjectRoot,ArmoryReviewDirectory));
            File.WriteAllText(Path.Combine(ProjectRoot,ArmoryReviewDirectory,"Construction.txt"),report.ToString(),Encoding.UTF8);
            // Unload only the saved production scene to avoid duplicate geometry during sample observation.
            EditorSceneManager.CloseScene(source,true);
        }

        private static string ArmorySourceState(Scene scene, HashSet<GameObject> excluded = null)
        {
            var lines=new List<string>();
            foreach(var root in scene.GetRootGameObjects()) foreach(var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if(excluded!=null&&excluded.Contains(transform.gameObject))continue;
                var path=AnimationUtility.CalculateTransformPath(transform,null);
                var objects=new List<UnityEngine.Object>{transform.gameObject};objects.AddRange(transform.GetComponents<Component>());
                for(var index=0;index<objects.Count;index++)
                {
                    var obj=objects[index];if(obj==null){lines.Add(path+"|MissingComponent");continue;}
                    using(var serialized=new SerializedObject(obj))
                    {
                        var property=serialized.GetIterator();
                        while(property.Next(true))
                        {
                            if(property.propertyPath.StartsWith("m_Prefab")||property.propertyPath.StartsWith("m_CorrespondingSourceObject")||property.propertyPath.EndsWith(".m_FileID")||property.propertyPath.EndsWith(".m_PathID")||property.propertyType==SerializedPropertyType.Generic)continue;
                            string value;
                            if(property.propertyType==SerializedPropertyType.ObjectReference)
                            {
                                var reference=property.objectReferenceValue;
                                if(reference==null)value="null";
                                else if(EditorUtility.IsPersistent(reference)) {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference,out string guid,out long localId);value=guid+":"+localId;}
                                else if(reference is Component component)value=AnimationUtility.CalculateTransformPath(component.transform,null)+":"+component.GetType().FullName;
                                else if(reference is GameObject gameObject)value=AnimationUtility.CalculateTransformPath(gameObject.transform,null)+":GameObject";
                                else value=reference.name+":"+reference.GetType().FullName;
                            }
                            else value=property.propertyType==SerializedPropertyType.ManagedReference?property.managedReferenceFullTypename:Convert.ToString(property.boxedValue,CultureInfo.InvariantCulture);
                            lines.Add(path+"|"+index+"|"+obj.GetType().FullName+"|"+property.propertyPath+"="+value);
                        }
                    }
                }
            }
            lines.Sort(StringComparer.Ordinal);return string.Join("\n",lines);
        }

        private static void BuildArmoryFittedContact(Transform holder,ConnectorEntranceProfile profile,Transform module,Transform roof,int id)
        {
            var floor=RequireUniqueCorridorFloorRenderer(module);var wa=RequireCorridorSurfaceRenderer(module," left armored wall");var wb=RequireCorridorSurfaceRenderer(module," right armored wall");
            var a=FittedWallCorner(profile.MinimumWall,profile.MaximumWall,floor.bounds.center,true);var b=FittedWallCorner(profile.MaximumWall,profile.MinimumWall,floor.bounds.center,true);
            var c=FittedWallCorner(wa,wb,profile.Entrance.Point,true);var d=FittedWallCorner(wb,wa,profile.Entrance.Point,true);
            if((a-c).sqrMagnitude+(b-d).sqrMagnitude>(a-d).sqrMagnitude+(b-c).sqrMagnitude){var swap=c;c=d;d=swap;}
            a-=profile.Entrance.Outward*.01f;b-=profile.Entrance.Outward*.01f;
            var toward=HorizontalDirection(floor.bounds.center-(c+d)*.5f);c+=toward*.01f;d+=toward*.01f;
            a.y=b.y=profile.Floor.bounds.max.y;c.y=d.y=floor.bounds.max.y;
            var top=RequireSingleRenderer(roof).bounds.min.y;
            var at=new Vector3(a.x,profile.Ceiling.bounds.min.y,a.z);var bt=new Vector3(b.x,profile.Ceiling.bounds.min.y,b.z);var ct=new Vector3(c.x,top,c.z);var dt=new Vector3(d.x,top,d.z);
            var ab=HorizontalDirection(b-a)*.15f;var cd=HorizontalDirection(d-c)*.15f;var up=Vector3.up*.15f;
            var path=ArmorySampleDirectory+"/Meshes";
            FittedSolid(holder,"Contact_"+id,"Floor",new[]{a-ab,b+ab,d+cd,c-cd,a-ab-up,b+ab-up,d+cd-up,c-cd-up},floor.sharedMaterial,path);
            FittedSolid(holder,"Contact_"+id,"Ceiling",new[]{at-ab,bt+ab,dt+cd,ct-cd,at-ab+up,bt+ab+up,dt+cd+up,ct-cd+up},RequireSingleRenderer(roof).sharedMaterial,path);
            FittedSolid(holder,"Contact_"+id,"WallA",new[]{a,c,ct,at,a-ab,c-cd,ct-cd,at-ab},wa.sharedMaterial,path);
            FittedSolid(holder,"Contact_"+id,"WallB",new[]{b,d,dt,bt,b+ab,d+cd,dt+cd,bt+ab},wb.sharedMaterial,path);
        }

        private const string ArmoryTransferDirectory = ArmoryReviewDirectory + "/TransferReview";

        private static List<Transform> ArmoryTransferRoots(Scene scene)
        {
            var roots=new List<Transform>();
            foreach(var name in new[]{"Approved Engine Room 01 Shell","Approved Cockpit 01 Structure","Approved Control Room 01 Shell","Approved Armory 01 Shell","Approved Supply Room 01 Shell"})
                roots.Add(RequireSceneObject(scene,name).transform);
            var corridors=RequireSceneObject(scene,CorridorRootName).transform;
            for(var i=1;i<=5;i++)
            {
                var id="SC-H0"+i;
                roots.Add(corridors.Find(id+" horizontal corridor sample")??throw new InvalidOperationException("Missing "+id));
                roots.Add(corridors.Find(CorridorCeilingRootName+"/ShipSpaceCeiling_"+id.Replace('-','_')+"_horizontal_corridor_sample_Slab_01")??throw new InvalidOperationException("Missing roof "+id));
            }
            return roots;
        }

        private static bool ArmoryTransferComponentsEqual(Transform source,Transform target,StringBuilder report)
        {
            var a=source.GetComponents<Component>();var b=target.GetComponents<Component>();var same=true;
            if(a.Length!=b.Length){report.AppendLine("Component count differs "+source.name);return false;}
            for(var i=0;i<a.Length;i++)
            {
                if(a[i]==null||b[i]==null||a[i].GetType()!=b[i].GetType()){report.AppendLine("Component type differs "+source.name);same=false;continue;}
                if(a[i] is Transform)continue;
                using(var sa=new SerializedObject(a[i]))using(var sb=new SerializedObject(b[i]))
                {
                    var p=sa.GetIterator();while(p.Next(true))
                    {
                        if(p.propertyPath=="m_GameObject"||p.propertyPath.StartsWith("m_Prefab")||p.propertyPath.StartsWith("m_CorrespondingSourceObject")||p.propertyPath.EndsWith(".m_FileID")||p.propertyPath.EndsWith(".m_PathID")||p.propertyType==SerializedPropertyType.Generic)continue;
                        var q=sb.FindProperty(p.propertyPath);
                        var equal=q!=null&&(p.propertyType==SerializedPropertyType.ObjectReference?SameApprovedReference(p.objectReferenceValue,q.objectReferenceValue):SerializedProperty.DataEquals(p,q));
                        if(!equal){report.AppendLine("Component property differs "+source.name+"/"+a[i].GetType().Name+"/"+p.propertyPath);same=false;}
                    }
                }
            }
            return same;
        }

        internal static void ApplyApprovedArmorySupplySampleToPegasus(){TransferApprovedArmorySupply(true);}

        private static void TransferApprovedArmorySupply(bool apply)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Transfer requires Edit Mode.");
            var directory=Path.Combine(ProjectRoot,ArmoryTransferDirectory);Directory.CreateDirectory(directory);
            var sample=SceneManager.GetSceneByPath(ArmorySampleScene);var target=SceneManager.GetSceneByPath(TargetScenePath);
            if(!sample.IsValid()||!sample.isLoaded)sample=EditorSceneManager.OpenScene(ArmorySampleScene,OpenSceneMode.Additive);
            if(!target.IsValid()||!target.isLoaded)target=EditorSceneManager.OpenScene(TargetScenePath,OpenSceneMode.Additive);
            if(sample.isDirty||target.isDirty)throw new InvalidOperationException("Unsaved scene edits preserved; transfer stopped.");
            var sources=ArmoryTransferRoots(sample);var targets=ArmoryTransferRoots(target);
            var report=new StringBuilder("Saved approved sample transfer. No geometry generation. Visual comparison still required.\n");
            var allowed=true;
            for(var i=0;i<sources.Count;i++)
            {
                var src=TransferTree(sources[i]);var dst=TransferTree(targets[i]);
                report.AppendLine(sources[i].name+" sample="+sources[i].position+" target="+targets[i].position+" sampleWorldScale="+sources[i].lossyScale+" targetWorldScale="+targets[i].lossyScale+" sourceParentScale="+(sources[i].parent==null?Vector3.one:sources[i].parent.lossyScale)+" targetParentScale="+(targets[i].parent==null?Vector3.one:targets[i].parent.lossyScale));
                foreach(var pair in src)
                {
                    if(!dst.TryGetValue(pair.Key,out var other)){report.AppendLine("Missing target child "+pair.Key);allowed=false;continue;}
                    if(!ArmoryTransferComponentsEqual(pair.Value,other,report))allowed=false;
                }
                foreach(var pair in dst)if(!src.ContainsKey(pair.Key))
                {
                    report.AppendLine("Remove obsolete target child "+pair.Key);
                    if(!pair.Value.name.Contains(" extension "))allowed=false;
                }
            }
            File.WriteAllText(Path.Combine(directory,"Investigation.txt"),report.ToString(),Encoding.UTF8);
            if(!apply)return;
            if(!allowed)throw new InvalidOperationException("Unexpected source/target content mismatch; see Investigation.txt. No scene changes made.");
            var excluded=new HashSet<GameObject>();
            foreach(var root in targets)foreach(var t in root.GetComponentsInChildren<Transform>(true))excluded.Add(t.gameObject);
            var connectorNames=new[]{"Intermediate Connectors","Armory Supply Intermediate Connectors"};
            var oldConnectors=new List<GameObject>();
            foreach(var name in connectorNames)foreach(var root in target.GetRootGameObjects())if(root.name==name)
            {oldConnectors.Add(root);foreach(var t in root.GetComponentsInChildren<Transform>(true))excluded.Add(t.gameObject);}
            var protectedBefore=ArmorySourceState(target,excluded);
            File.WriteAllText(Path.Combine(directory,"ProtectedBefore.txt"),protectedBefore,Encoding.UTF8);
            var backup=Path.Combine(directory,"PegasusBeforeApply.unity.txt");
            if(!File.Exists(backup))File.Copy(Path.Combine(ProjectRoot,TargetScenePath),backup,false);
            Undo.IncrementCurrentGroup();var undo=Undo.GetCurrentGroup();
            try
            {
                for(var i=0;i<sources.Count;i++)
                {
                    Undo.RegisterFullObjectHierarchyUndo(targets[i].gameObject,"Apply approved armory supply layout");
                    var src=TransferTree(sources[i]);var dst=TransferTree(targets[i]);
                    foreach(var pair in dst)if(!src.ContainsKey(pair.Key)&&pair.Value!=null)Undo.DestroyObjectImmediate(pair.Value.gameObject);
                    foreach(var pair in src)
                    {
                        var other=dst[pair.Key];var s=pair.Value;
                        if(pair.Key=="")
                        {
                            other.SetPositionAndRotation(s.position,s.rotation);
                            // Preserve the approved world size without changing the shared corridor parent.
                            other.localScale=Vector3.one;
                            var inherited=other.lossyScale;var approved=s.lossyScale;
                            other.localScale=new Vector3(approved.x/inherited.x,approved.y/inherited.y,approved.z/inherited.z);
                        }
                        else{other.localPosition=s.localPosition;other.localRotation=s.localRotation;other.localScale=s.localScale;}
                        other.gameObject.SetActive(s.gameObject.activeSelf);
                    }
                }
                foreach(var old in oldConnectors)Undo.DestroyObjectImmediate(old);
                foreach(var name in connectorNames)
                {
                    var original=RequireSceneObject(sample,name);var clone=UnityEngine.Object.Instantiate(original);
                    clone.name=name;clone.transform.SetParent(null,true);
                    clone.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);clone.transform.localScale=original.transform.lossyScale;
                    SceneManager.MoveGameObjectToScene(clone,target);Undo.RegisterCreatedObjectUndo(clone,"Copy approved connector objects");
                    foreach(var t in clone.GetComponentsInChildren<Transform>(true))excluded.Add(t.gameObject);
                }
                var protectedAfter=ArmorySourceState(target,excluded);
                if(protectedBefore!=protectedAfter)throw new InvalidOperationException("Non-target state changed; reverting transfer.");
                if(!EditorSceneManager.SaveScene(target,TargetScenePath))throw new InvalidOperationException("Pegasus save failed.");
                File.WriteAllText(Path.Combine(directory,"Apply.txt"),"Saved approved rooms/corridors/connectors to Pegasus.\nNon-target state unchanged.\nNo mesh generation or original asset editing.\nDirect visual review pending.",Encoding.UTF8);
                Undo.CollapseUndoOperations(undo);
            }
            catch{Undo.RevertAllDownToGroup(undo);throw;}
            SceneManager.SetActiveScene(target);
        }

        internal static void ReviewApprovedArmorySupplyTransfer(){ReviewArmorySupplyScene(true);}
        internal static void ReviewPegasusArmorySupplySample(){ReviewArmorySupplyScene(false);}

        private static void ReviewArmorySupplyScene(bool transfer)
        {
            var directory=Path.Combine(ProjectRoot,transfer?ArmoryTransferDirectory:ArmoryReviewDirectory);Directory.CreateDirectory(directory);
            var op=JsonUtility.FromJson<ConnectorLiveOperation>(File.ReadAllText(Path.Combine(directory,"Operation.json")));
            if(transfer&&op.action=="inspect"){TransferApprovedArmorySupply(false);return;}
            if(transfer&&(op.action=="openSample"||op.action=="openPegasus"))
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
                for(var i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Unsaved scene edits preserved.");
                EditorSceneManager.OpenScene(op.action=="openSample"?ArmorySampleScene:TargetScenePath,OpenSceneMode.Single);return;
            }
            var scene=SceneManager.GetActiveScene();if(scene.path!=ArmorySampleScene&&(!transfer||scene.path!=TargetScenePath)) throw new InvalidOperationException("Requires approved sample or transfer target.");
            if(transfer&&op.action=="handoff")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
                Selection.activeGameObject=RequireSceneObject(scene,"Intermediate Connectors");
                SceneView.lastActiveSceneView?.LookAt(new Vector3(63,3,24),Quaternion.Euler(65,0,0),42);
                File.WriteAllText(Path.Combine(directory,"Handoff.txt"),"Scene="+scene.path+" PlayMode=False Dirty="+scene.isDirty+"\nProduction camera and lighting unchanged.",Encoding.UTF8);return;
            }
            if(transfer&&(op.action=="finishSeams"||op.action=="triangleHandoff"))throw new InvalidOperationException("Geometry/camera editing forbidden during transfer review.");
            if(op.action=="enter"){EditorApplication.isPlaying=true;return;}
            if(op.action=="exit"){EditorApplication.isPlaying=false;return;}
            if(op.action=="console")
            {
                var assembly=typeof(EditorWindow).Assembly;
                var logs=assembly.GetType("UnityEditor.LogEntries");
                var entryType=assembly.GetType("UnityEditor.LogEntry");
                var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
                var report=new StringBuilder();
                logs.GetMethod("StartGettingEntries",flags).Invoke(null,null);
                try
                {
                    var count=(int)logs.GetMethod("GetCount",flags).Invoke(null,null);
                    var entry=Activator.CreateInstance(entryType);
                    var message=entryType.GetField("message",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                    for(var i=0;i<count;i++)
                    {
                        logs.GetMethod("GetEntryInternal",flags).Invoke(null,new[]{(object)i,entry});
                        report.AppendLine(Convert.ToString(message.GetValue(entry)));
                    }
                }
                finally{logs.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
                File.WriteAllText(Path.Combine(directory,"Console.txt"),report.ToString(),Encoding.UTF8);return;
            }
            if(op.action=="triangleDetails")
            {
                var rooms=RequireTriRoomConnectorRooms(scene);var root=RequireSceneObject(scene,CorridorRootName).transform;
                var modules=RequireTriRoomConnectorModules(root);var report=new StringBuilder();
                for(var i=0;i<6;i++)
                {
                    var def=TriRoomConnectorEndpoints[i];var p=BuildConnectorEntranceProfile(scene,rooms,modules[def.CorridorId],def);
                    report.AppendLine(def.Id+" mouth="+TriangleMouth(scene,i)+" outward="+p.Entrance.Outward);
                    var holder=RequireSceneObject(scene,"Intermediate Connectors").transform.Find("Connector_"+def.Id);
                    foreach(var filter in holder.GetComponentsInChildren<MeshFilter>())
                    {report.AppendLine(filter.name);var vertices=filter.sharedMesh.vertices;for(var v=0;v<Mathf.Min(4,vertices.Length);v++)report.AppendLine(filter.transform.TransformPoint(vertices[v]).ToString("F4"));}
                    foreach(var r in new[]{p.MinimumWall,p.MaximumWall,p.Floor,p.Ceiling,RequireUniqueCorridorFloorRenderer(modules[def.CorridorId]),RequireSingleRenderer(CaptureCorridorCeilingFollowers(modules[def.CorridorId],root.Find(CorridorCeilingRootName))[0].Transform)})
                        report.AppendLine(r.name+" | "+r.bounds+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" parent="+r.transform.parent.name);
                }
                File.WriteAllText(Path.Combine(directory,"TriangleDetails.txt"),report.ToString(),Encoding.UTF8);return;
            }
            if(op.action=="triangleInspect")
            {
                var rooms=RequireTriRoomConnectorRooms(scene);
                var modules=RequireTriRoomConnectorModules(RequireSceneObject(scene,CorridorRootName).transform);
                var report=new StringBuilder();
                foreach(var pair in rooms)report.AppendLine(pair.Key+" position="+pair.Value.transform.position.ToString("F6")+" rotation="+pair.Value.transform.rotation.ToString("F6"));
                foreach(var def in TriRoomConnectorEndpoints)
                {
                    var p=BuildConnectorEntranceProfile(scene,rooms,modules[def.CorridorId],def);
                    var a=FittedWallCorner(p.MinimumWall,p.MaximumWall,p.Entrance.Point+p.Entrance.Outward*100,true);
                    var b=FittedWallCorner(p.MaximumWall,p.MinimumWall,p.Entrance.Point+p.Entrance.Outward*100,true);
                    report.AppendLine(def.Id+" mouth="+((a+b)*.5f).ToString("F6")+" outward="+p.Entrance.Outward.ToString("F6")+" floor="+p.Floor.bounds.max.y+" ceiling="+p.Ceiling.bounds.min.y);
                }
                File.WriteAllText(Path.Combine(directory,"TriangleBefore.txt"),report.ToString(),Encoding.UTF8);return;
            }
            if(op.action=="finishSeams")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before editing sample.");
                var root=RequireSceneObject(scene,"Armory Supply Intermediate Connectors").transform;
                var previous=root.Find("Boundary Seals");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var seals=new GameObject("Boundary Seals").transform;seals.SetParent(root,false);
                for(var i=0;i<4;i++)
                {
                    var p=ArmoryContact(scene,i);
                    AddArmoryBoundarySeals(seals,p.MinimumWall,p.Floor,p.Ceiling,"Room"+i+"A");
                    AddArmoryBoundarySeals(seals,p.MaximumWall,p.Floor,p.Ceiling,"Room"+i+"B");
                    var fb=p.Floor.bounds;var mouth=p.Entrance.Point;
                    var cb=p.Ceiling.bounds;var roofEdge=cb.ClosestPoint(new Vector3(mouth.x,cb.center.y,mouth.z));
                    var roofTarget=new Vector3(mouth.x,cb.center.y,mouth.z);
                    if(Vector3.Distance(roofEdge,roofTarget)>.002f)
                    {
                        var min=fb.min;var max=fb.max;min.y=cb.min.y;max.y=cb.max.y;
                        if(Mathf.Abs(p.Entrance.Outward.x)>.5f){min.x=Mathf.Min(roofEdge.x,mouth.x)-.01f;max.x=Mathf.Max(roofEdge.x,mouth.x)+.01f;}
                        else{min.z=Mathf.Min(roofEdge.z,mouth.z)-.01f;max.z=Mathf.Max(roofEdge.z,mouth.z)+.01f;}
                        ArmorySealBox(seals,"Room"+i+"RoofEnd",min,max,p.Ceiling.sharedMaterial);
                    }
                    var edge=fb.ClosestPoint(mouth);edge.y=mouth.y;
                    if(Vector3.Distance(edge,mouth)>.002f)
                    {
                        var min=fb.min;var max=fb.max;min.y=mouth.y-.15f;max.y=mouth.y;
                        if(Mathf.Abs(p.Entrance.Outward.x)>.5f){min.x=Mathf.Min(edge.x,mouth.x)-.01f;max.x=Mathf.Max(edge.x,mouth.x)+.01f;}
                        else {min.z=Mathf.Min(edge.z,mouth.z)-.01f;max.z=Mathf.Max(edge.z,mouth.z)+.01f;}
                        ArmorySealBox(seals,"Room"+i+"DeckEnd",min,max,p.Floor.sharedMaterial);
                    }
                }
                var corridors=RequireSceneObject(scene,CorridorRootName).transform;
                for(var i=3;i<=4;i++)
                {
                    var module=corridors.Find("SC-H0"+i+" horizontal corridor sample");
                    var floor=RequireUniqueCorridorFloorRenderer(module);
                    var roof=RequireRenderer(scene,"ShipSpaceCeiling_SC_H0"+i+"_horizontal_corridor_sample_Slab_01");
                    AddArmoryBoundarySeals(seals,RequireCorridorSurfaceRenderer(module," left armored wall"),floor,roof,"H0"+i+"A");
                    AddArmoryBoundarySeals(seals,RequireCorridorSurfaceRenderer(module," right armored wall"),floor,roof,"H0"+i+"B");
                }
                AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ArmorySampleScene);return;
            }
            if(op.action=="inspect")
            {
                var report=new StringBuilder();
                for(var i=0;i<4;i++)
                {
                    var p=ArmoryContact(scene,i);report.AppendLine("CONTACT "+i+" "+p.Entrance.Point);
                    foreach(var r in RequireSceneObject(scene,ArmoryContactRooms[i]).GetComponentsInChildren<Renderer>(true))
                        if(Vector3.Distance(r.bounds.ClosestPoint(p.Entrance.Point),p.Entrance.Point)<4)
                            report.AppendLine(r.name+" | "+r.bounds+" | active="+r.gameObject.activeInHierarchy);
                }
                foreach(var r in RequireSceneObject(scene,CorridorRootName).GetComponentsInChildren<Renderer>(true))
                    if(r.name.Contains("H03")||r.name.Contains("H04"))report.AppendLine(r.name+" | "+r.bounds);
                File.WriteAllText(Path.Combine(directory,"Geometry.txt"),report.ToString(),Encoding.UTF8);return;
            }
            if(op.action=="handoff"||op.action=="triangleHandoff")
            {
                if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before saving sample camera.");
                var camera=RequireSceneObject(scene,"Main Camera").GetComponent<Camera>();
                camera.transform.SetPositionAndRotation(new Vector3(74,70,-4),Quaternion.Euler(90,0,0));camera.orthographic=true;camera.orthographicSize=33;
                Selection.activeGameObject=RequireSceneObject(scene,"Armory Supply Intermediate Connectors");
                SceneView.lastActiveSceneView?.LookAt(new Vector3(78,3,-4),Quaternion.Euler(60,0,0),40);
                if(op.action=="triangleHandoff")
                {
                    camera.transform.SetPositionAndRotation(new Vector3(65,90,10),Quaternion.Euler(90,0,0));
                    camera.orthographicSize=40;
                    Selection.activeGameObject=RequireSceneObject(scene,"Intermediate Connectors");
                    SceneView.lastActiveSceneView?.LookAt(new Vector3(63,3,24),Quaternion.Euler(65,0,0),42);
                }
                EditorSceneManager.SaveScene(scene,ArmorySampleScene);
                var logType=typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                var countMethod=logType.GetMethod("GetCountsByType",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                var counts=new object[]{0,0,0};countMethod.Invoke(null,counts);
                File.WriteAllText(Path.Combine(directory,"Handoff.txt"),"Scene="+scene.path+"\nPlayMode="+EditorApplication.isPlaying+"\nConsoleErrors="+counts[0]+"\nConsoleWarnings="+counts[1]+"\nSample only saved. No production scene save.",Encoding.UTF8);return;
            }
            if(!EditorApplication.isPlaying) throw new InvalidOperationException("Actual Play Mode required.");
            if(connectorReviewCamera==null){var go=new GameObject("__ArmorySampleObserver"){hideFlags=HideFlags.DontSave};connectorReviewCamera=go.AddComponent<Camera>();connectorReviewCamera.depth=100;connectorReviewCamera.nearClipPlane=.03f;connectorReviewCamera.farClipPlane=500;connectorReviewCamera.fieldOfView=65;}
            var position=op.position;var rotation=Quaternion.Euler(op.rotation);
            if(op.action=="triangleView")
            {
                var endpoint=(op.index-100)/8;var view=(op.index-100)%8;
                var rooms=RequireTriRoomConnectorRooms(scene);
                var modules=RequireTriRoomConnectorModules(RequireSceneObject(scene,CorridorRootName).transform);
                var def=TriRoomConnectorEndpoints[endpoint];
                var profile=BuildConnectorEntranceProfile(scene,rooms,modules[def.CorridorId],def);
                var mouth=TriangleMouth(scene,endpoint);var direction=profile.Entrance.Outward;
                var across=Vector3.Cross(Vector3.up,direction);var height=(profile.Ceiling.bounds.min.y-mouth.y)*.55f;
                var target=mouth+direction*.9f+Vector3.up*height;
                position=mouth-direction*1.2f+Vector3.up*height;
                if(view==1){position=mouth+direction*1.2f+Vector3.up*height;target=mouth-direction;target.y=position.y;}
                if(view==2||view==3){position=mouth-direction*.5f+across*(view==2?.55f:-.55f)+Vector3.up*height;target=mouth+direction*.9f;}
                if(view==4){position=mouth-direction*.5f+Vector3.up*.5f;target=mouth+direction*.9f;target.y=profile.Ceiling.bounds.min.y;}
                if(view==5||view==6){position=mouth+direction*.7f+across*(view==5?4:-4)+Vector3.up*2.5f;}
                if(view==7){position=mouth+direction*.8f+Vector3.up*6;}
                rotation=Quaternion.LookRotation(target-position,Vector3.up);
            }
            if(op.index<16)
            {
                var profile=ArmoryContact(scene,op.index/4);var direction=profile.Entrance.Outward;var across=Vector3.Cross(Vector3.up,direction);var view=op.index%4;
                position=profile.Entrance.Point+direction*(view==0?-1.6f:.4f)+across*(view==2?.6f:view==3?-.6f:0)+Vector3.up*1.6f;
                rotation=Quaternion.LookRotation(view==1?-direction:direction,Vector3.up);
            }
            connectorReviewCamera.transform.SetPositionAndRotation(position,rotation);
            if(op.action=="triangleProbe")
            {
                Physics.SyncTransforms();var report=new StringBuilder();
                foreach(var x in new[]{.65f,.75f,.82f,.88f})
                {
                    var ray=connectorReviewCamera.ViewportPointToRay(new Vector3(x,.5f,0));report.AppendLine("X="+x+" ray="+ray);
                    foreach(var hit in Physics.RaycastAll(ray,20))report.AppendLine(hit.collider.name+" at "+hit.point+" distance="+hit.distance);
                }
                File.WriteAllText(Path.Combine(directory,"TriangleProbe.txt"),report.ToString(),Encoding.UTF8);
            }
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            var due=EditorApplication.timeSinceStartup+.8;EditorApplication.CallbackFunction capture=null;
            capture=()=>{if(EditorApplication.timeSinceStartup<due)return;EditorApplication.update-=capture;
                var prefix=transfer?(scene.path==TargetScenePath?"Pegasus_":"Sample_"):"";
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,prefix+"View_"+op.index.ToString("D2")+".png"));
                File.WriteAllText(Path.Combine(directory,prefix+"View_"+op.index.ToString("D2")+".txt"),"Actual PlayMode="+EditorApplication.isPlaying+" Camera="+position+" Rotation="+rotation.eulerAngles,Encoding.UTF8);};EditorApplication.update+=capture;
        }

        private static void ArmorySealBox(Transform parent,string id,Vector3 min,Vector3 max,Material material)
        {
            if(max.x-min.x<.001f||max.y-min.y<.001f||max.z-min.z<.001f)return;
            FittedSolid(parent,id,"Seal",new[]{new Vector3(min.x,min.y,min.z),new Vector3(max.x,min.y,min.z),new Vector3(max.x,min.y,max.z),new Vector3(min.x,min.y,max.z),new Vector3(min.x,max.y,min.z),new Vector3(max.x,max.y,min.z),new Vector3(max.x,max.y,max.z),new Vector3(min.x,max.y,max.z)},material,ArmorySampleDirectory+"/Meshes");
        }

        private static void AddArmoryBoundarySeals(Transform parent,Renderer wall,Renderer floor,Renderer ceiling,string id)
        {
            // Separate strips occupy only the existing boundary gaps, never the passage volume.
            var w=wall.bounds;var f=floor.bounds;var c=ceiling.bounds;
            var min=w.min;var max=w.max;
            if(w.min.y>f.max.y+.001f){min.y=f.max.y-.01f;max.y=w.min.y+.01f;ArmorySealBox(parent,id+"Base",min,max,wall.sharedMaterial);}
            min=w.min;max=w.max;
            if(c.min.y>w.max.y+.001f){min.y=w.max.y-.01f;max.y=c.min.y+.01f;ArmorySealBox(parent,id+"Header",min,max,wall.sharedMaterial);}
            var alongX=w.size.x>w.size.z;
            min=w.min;max=w.max;min.y=c.min.y;max.y=c.max.y;
            if(alongX)
            {
                if(w.center.z>c.center.z){min.z=c.max.z-.01f;max.z=w.max.z;}else{min.z=w.min.z;max.z=c.min.z+.01f;}
            }
            else
            {
                if(w.center.x>c.center.x){min.x=c.max.x-.01f;max.x=w.max.x;}else{min.x=w.min.x;max.x=c.min.x+.01f;}
            }
            ArmorySealBox(parent,id+"RoofEdge",min,max,ceiling.sharedMaterial);
        }

        private static void CreateLegacyTriRoomConnectorSample()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var protectedPath = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSampleValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomConnectorSampleProtectedStateName);
            if (!File.Exists(protectedPath))
            {
                throw new InvalidOperationException(
                    "Connector sample creation requires the read-only source inspection first.");
            }

            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var beforeProtectedState = BuildTriRoomConnectorProtectedState(
                rooms,
                corridorRoot,
                modules,
                ceilingRoot);
            var beforeSourceGeometry = BuildTriRoomConnectorSourceGeometrySignature(
                rooms,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    expectedProtectedState,
                    beforeProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus room or corridor placement changed after source inspection.");
            }

            DeleteProjectFileIfPresent(
                TriRoomConnectorSampleArtDirectory,
                TriRoomConnectorSampleFinalName);
            DeleteProjectFileIfPresent(
                TriRoomConnectorSampleValidationDirectory,
                TriRoomConnectorSampleReviewName);
            DeleteProjectFileIfPresent(
                TriRoomConnectorSampleValidationDirectory,
                TriRoomConnectorSampleInspectionName);

            if (AssetDatabase.IsValidFolder(TriRoomConnectorSampleAssetDirectory) &&
                !AssetDatabase.DeleteAsset(TriRoomConnectorSampleAssetDirectory))
            {
                throw new InvalidOperationException(
                    "Unable to replace the previous connector art-sample folder.");
            }

            EnsureAssetFolder(TriRoomConnectorSampleAssetDirectory);
            EnsureAssetFolder(TriRoomConnectorSampleMeshDirectory);
            if (!EditorSceneManager.SaveScene(
                    targetScene,
                    TriRoomConnectorSampleScenePath,
                    true))
            {
                throw new InvalidOperationException(
                    "Unable to copy Pegasus into the isolated connector sample scene.");
            }

            var sampleScene = EditorSceneManager.OpenScene(
                TriRoomConnectorSampleScenePath,
                OpenSceneMode.Single);

            try
            {
                var sampleRooms = RequireRooms(sampleScene);
                var sampleCorridorRoot = RequireSceneObject(sampleScene, CorridorRootName);
                var sampleModules = RequireCorridorModules(sampleCorridorRoot.transform);
                var sampleCeilingRoot = sampleCorridorRoot.transform.Find(CorridorCeilingRootName) ??
                    throw new InvalidOperationException(
                        "Connector sample corridor copy is missing " + CorridorCeilingRootName);
                var sampleRoot = new GameObject(TriRoomConnectorSampleRootName);
                SceneManager.MoveGameObjectToScene(sampleRoot, sampleScene);
                foreach (var roomId in new[] { "EngineRoom", "Cockpit", "ControlRoom" })
                {
                    sampleRooms[roomId].transform.SetParent(sampleRoot.transform, true);
                }

                sampleCorridorRoot.transform.SetParent(sampleRoot.transform, true);
                var connectorRoot = CreateSampleContainer(
                    "Intermediate Connectors",
                    sampleRoot.transform,
                    sampleScene);

                for (var endpointIndex = 0;
                     endpointIndex < TriRoomConnectorEndpoints.Length;
                     endpointIndex++)
                {
                    BuildTriRoomConnectorSampleEndpoint(
                        sampleScene,
                        sampleRooms,
                        sampleModules,
                        sampleCeilingRoot,
                        TriRoomConnectorEndpoints[endpointIndex],
                        connectorRoot.transform,
                        sampleScene);
                }

                var preflightReport = new StringBuilder();
                var preflightMaximumConstructionError = 0f;
                var preflightMinimumClearWidth = float.PositiveInfinity;
                var preflightMinimumRoomOverlap = float.PositiveInfinity;
                var preflightMinimumCorridorOverlap = float.PositiveInfinity;
                var preflightMinimumLateralCoverage = float.PositiveInfinity;
                var preflightMaximumRoomIntrusion = 0f;
                var preflightMaximumCorridorIntrusion = 0f;
                var preflightMaterials = new HashSet<string>(StringComparer.Ordinal);
                for (var endpointIndex = 0;
                     endpointIndex < TriRoomConnectorEndpoints.Length;
                     endpointIndex++)
                {
                    InspectTriRoomConnectorSampleEndpoint(
                        preflightReport,
                        sampleScene,
                        sampleRooms,
                        sampleModules,
                        sampleCeilingRoot,
                        connectorRoot.transform,
                        TriRoomConnectorEndpoints[endpointIndex],
                        ref preflightMaximumConstructionError,
                        ref preflightMinimumClearWidth,
                        ref preflightMinimumRoomOverlap,
                        ref preflightMinimumCorridorOverlap,
                        ref preflightMinimumLateralCoverage,
                        ref preflightMaximumRoomIntrusion,
                        ref preflightMaximumCorridorIntrusion,
                        preflightMaterials);
                }

                var sampleSourceGeometry = BuildTriRoomConnectorSourceGeometrySignature(
                    sampleRooms,
                    sampleModules,
                    sampleCeilingRoot);
                if (!string.Equals(
                        beforeSourceGeometry,
                        sampleSourceGeometry,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The connector sample changed an existing room or corridor model. " +
                        "Only separate additive connector meshes are allowed.");
                }

                if (preflightMaximumConstructionError > PositionTolerance ||
                    preflightMinimumClearWidth <= PositionTolerance ||
                    preflightMinimumRoomOverlap < -TriRoomConnectorSampleSeamTolerance ||
                    preflightMinimumCorridorOverlap < -TriRoomConnectorSampleSeamTolerance ||
                    preflightMinimumLateralCoverage < -TriRoomConnectorSampleSeamTolerance ||
                    preflightMaximumRoomIntrusion > TriRoomConnectorSampleSeamTolerance ||
                    preflightMaximumCorridorIntrusion > TriRoomConnectorSampleSeamTolerance)
                {
                    throw new InvalidOperationException(
                        "Connector construction preflight found an uncovered source boundary. " +
                        "ConstructionError=" + Float(preflightMaximumConstructionError) +
                        "; RoomOverlap=" + Float(preflightMinimumRoomOverlap) +
                        "; CorridorOverlap=" + Float(preflightMinimumCorridorOverlap) +
                        "; LateralCoverage=" + Float(preflightMinimumLateralCoverage) +
                        "; RoomIntrusion=" + Float(preflightMaximumRoomIntrusion) +
                        "; CorridorIntrusion=" + Float(preflightMaximumCorridorIntrusion));
                }

                for (var definitionIndex = 0;
                     definitionIndex < CorridorDefinitions.Length;
                     definitionIndex++)
                {
                    var definition = CorridorDefinitions[definitionIndex];
                    if (IsTriRoomTargetCorridor(definition.ModuleId))
                    {
                        continue;
                    }

                    var followers = CaptureCorridorCeilingFollowers(
                        sampleModules[definition.ModuleId],
                        sampleCeilingRoot);
                    UnityEngine.Object.DestroyImmediate(
                        sampleModules[definition.ModuleId].gameObject);
                    UnityEngine.Object.DestroyImmediate(followers[0].Transform.gameObject);
                }

                var sampleRoots = sampleScene.GetRootGameObjects();
                for (var rootIndex = 0; rootIndex < sampleRoots.Length; rootIndex++)
                {
                    if (sampleRoots[rootIndex] != sampleRoot)
                    {
                        UnityEngine.Object.DestroyImmediate(sampleRoots[rootIndex]);
                    }
                }

                if (!EditorSceneManager.SaveScene(sampleScene, TriRoomConnectorSampleScenePath))
                {
                    throw new InvalidOperationException(
                        "Unable to save the Pegasus tri-room connector art-sample scene.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            }

            rooms = RequireRooms(targetScene);
            corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            modules = RequireCorridorModules(corridorRoot.transform);
            ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var afterProtectedState = BuildTriRoomConnectorProtectedState(
                rooms,
                corridorRoot,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    beforeProtectedState,
                    afterProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pegasus changed while creating the isolated connector sample.");
            }

            WriteTriRoomConnectorSampleReviewHtml();
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room intermediate connector Unity art sample created. " +
                "Endpoints=6; UnifiedHollowBoundaryLofts=6; RoomCornerSeals=2; " +
                "RoomWallClosureVolumes=6; RoomHorizontalApproachFills=2; " +
                "AdditiveGapFillers=10; " +
                "CentralPassageCaps=0; " +
                "ExistingSourceGeometryUnchanged=True; ExistingMaterials=True; " +
                "NewDoor=False; ProductionSceneMutation=False; UnityConsoleErrors=0");
        }

        internal static void CapturePegasusTriRoomConnectorSampleReview()
        {
            if (!File.Exists(Path.Combine(ProjectRoot, TriRoomConnectorSampleScenePath)))
            {
                throw new InvalidOperationException(
                    "Connector art-sample scene is missing. Create the sample before capture.");
            }

            var sampleScene = EditorSceneManager.OpenScene(
                TriRoomConnectorSampleScenePath,
                OpenSceneMode.Single);
            var sampleRoot = RequireSceneObject(sampleScene, TriRoomConnectorSampleRootName);
            var artOutput = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSampleArtDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomConnectorSampleFinalName);
            CaptureTriRoomConnectorSampleSheet(sampleScene, sampleRoot, artOutput);
            var validationOutput = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSampleValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomConnectorSampleReviewName);
            Directory.CreateDirectory(Path.GetDirectoryName(validationOutput) ?? string.Empty);
            File.Copy(artOutput, validationOutput, true);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus connector art-sample final contact sheet captured once. " +
                "Panels=8; Output=" + artOutput + "; UnityConsoleErrors=0");
        }

        internal static class TriRoomConnectorPlayModeCapture
        {
            private const string PendingKey =
                "Bellerophon.PegasusTriRoomConnector.PlayModeSweep.Pending";
            private static Action<string> completeCallback;
            private static Action<Exception> failCallback;

            internal static bool HasPendingCapture =>
                SessionState.GetBool(PendingKey, false);

            internal static void ResetStaleCapture()
            {
                SessionState.EraseBool(PendingKey);
                completeCallback = null;
                failCallback = null;
            }

            internal static void Start(
                Action<string> onComplete,
                Action<Exception> onFail)
            {
                if (!File.Exists(Path.Combine(ProjectRoot, TriRoomConnectorSampleScenePath)))
                {
                    throw new InvalidOperationException(
                        "Connector art-sample scene is missing. Create it before Play Mode review.");
                }

                completeCallback = onComplete;
                failCallback = onFail;
                SessionState.SetBool(PendingKey, true);
                EditorSceneManager.OpenScene(
                    TriRoomConnectorSampleScenePath,
                    OpenSceneMode.Single);
                EditorApplication.isPlaying = true;
            }

            internal static void Resume(
                Action<string> onComplete,
                Action<Exception> onFail)
            {
                completeCallback = onComplete;
                failCallback = onFail;
                if (!HasPendingCapture)
                {
                    Start(onComplete, onFail);
                    return;
                }

                if (!EditorApplication.isPlaying)
                {
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorApplication.isPlaying = true;
                    }

                    return;
                }

                EditorApplication.delayCall -= CaptureAfterPlayModeEntered;
                EditorApplication.delayCall += CaptureAfterPlayModeEntered;
            }

            private static void CaptureAfterPlayModeEntered()
            {
                try
                {
                    if (!Application.isPlaying)
                    {
                        throw new InvalidOperationException(
                            "Connector sweep must run in actual Unity Play Mode.");
                    }

                    CaptureTriRoomConnectorPlayModeSweeps();
                    SessionState.EraseBool(PendingKey);
                    var callback = completeCallback;
                    completeCallback = null;
                    failCallback = null;
                    callback?.Invoke(
                        "Pegasus tri-room connector actual Play Mode sweep completed. " +
                        "Endpoints=6; ExteriorViews=144; InteriorViews=144; " +
                        "CentralPassageCaps=0.");
                    EditorApplication.isPlaying = false;
                }
                catch (Exception exception)
                {
                    SessionState.EraseBool(PendingKey);
                    var callback = failCallback;
                    completeCallback = null;
                    failCallback = null;
                    callback?.Invoke(exception);
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = false;
                    }
                }
            }
        }

        private static void CaptureTriRoomConnectorPlayModeSweeps()
        {
            var sampleScene = SceneManager.GetActiveScene();
            if (!string.Equals(
                    sampleScene.path,
                    TriRoomConnectorSampleScenePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Play Mode sweep opened the wrong scene: " + sampleScene.path);
            }

            var sampleRoot = RequireSceneObject(
                sampleScene,
                TriRoomConnectorSampleRootName);
            var connectors = sampleRoot.transform.Find("Intermediate Connectors") ??
                throw new InvalidOperationException(
                    "Connector sample hierarchy is missing Intermediate Connectors.");
            var sampleRooms = RequireTriRoomConnectorRooms(sampleScene);
            var sampleCorridorRoot = RequireSceneObject(sampleScene, CorridorRootName);
            var sampleModules = RequireTriRoomConnectorModules(
                sampleCorridorRoot.transform);
            var sampleCeilingRoot = sampleCorridorRoot.transform.Find(
                CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Connector sample corridor ceiling root is missing.");
            var outputDirectory = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSamplePlayModeDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputDirectory);
            foreach (var oldImage in Directory.GetFiles(outputDirectory, "*.png"))
            {
                File.Delete(oldImage);
            }

            var cameraObject = new GameObject("__ConnectorPlayModeSweepCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.015f, 0.45f, 1f);
            camera.nearClipPlane = 0.025f;
            camera.farClipPlane = 1000f;
            camera.fieldOfView = 58f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var lightObject = new GameObject("__ConnectorPlayModeSweepLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.94f, 0.97f, 1f, 1f);
            light.intensity = 1.65f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var originalAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.38f, 0.42f, 0.48f, 1f);
            var report = new StringBuilder();
            report.AppendLine("Pegasus Tri-Room Connector Actual Play Mode Sweep");
            report.AppendLine("ApplicationIsPlaying=" + Application.isPlaying);
            report.AppendLine("UnityFrameCount=" + Time.frameCount);
            report.AppendLine("EndpointCount=" + TriRoomConnectorEndpoints.Length);
            report.AppendLine("ExteriorViewsPerEndpoint=24");
            report.AppendLine("InteriorViewsPerEndpoint=24");
            report.AppendLine("Background=Magenta exterior exposure indicator");
            report.AppendLine("VerificationPriority=DirectVisualFirst");
            try
            {
                for (var endpointIndex = 0;
                     endpointIndex < TriRoomConnectorEndpoints.Length;
                     endpointIndex++)
                {
                    var definition = TriRoomConnectorEndpoints[endpointIndex];
                    var module = sampleModules[definition.CorridorId];
                    var profile = BuildConnectorEntranceProfile(
                        sampleScene,
                        sampleRooms,
                        module,
                        definition);
                    var floor = RequireUniqueCorridorFloorRenderer(module);
                    var namedLeftWall = RequireCorridorSurfaceRenderer(
                        module,
                        " left armored wall");
                    var namedRightWall = RequireCorridorSurfaceRenderer(
                        module,
                        " right armored wall");
                    OrderCorridorWalls(
                        module,
                        namedLeftWall,
                        namedRightWall,
                        out var minimumWall,
                        out var maximumWall);
                    var ceiling = RequireSingleRenderer(
                        CaptureCorridorCeilingFollowers(
                            module,
                            sampleCeilingRoot)[0].Transform);
                    var shell = CalculateConnectorGapShell(
                        definition,
                        module,
                        profile,
                        floor,
                        minimumWall,
                        maximumWall,
                        ceiling);
                    var connector = connectors.Find("Connector_" + definition.Id) ??
                        throw new InvalidOperationException(
                            "Play Mode sweep is missing Connector_" + definition.Id);
                    var exterior = RenderTriRoomConnectorPlayModeSweep(
                        camera,
                        connector.gameObject,
                        profile,
                        module,
                        shell,
                        false);
                    var interior = RenderTriRoomConnectorPlayModeSweep(
                        camera,
                        connector.gameObject,
                        profile,
                        module,
                        shell,
                        true);
                    File.WriteAllBytes(
                        Path.Combine(outputDirectory, definition.Id + "_Exterior.png"),
                        exterior.EncodeToPNG());
                    File.WriteAllBytes(
                        Path.Combine(outputDirectory, definition.Id + "_Interior.png"),
                        interior.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(exterior);
                    UnityEngine.Object.DestroyImmediate(interior);
                    if (definition.Id == "EngineRoom_H01" ||
                        definition.Id == "ControlRoom_H02" ||
                        definition.Id == "ControlRoom_H05")
                    {
                        var focusedContact = definition.Id == "ControlRoom_H05"
                            ? RenderTriRoomConnectorFocusedContactSweep(
                                camera,
                                shell,
                                0.40f,
                                0.60f,
                                6f)
                            : RenderTriRoomConnectorRoomContactSweep(
                                camera,
                                shell,
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id + "_ContactFrames"));
                        File.WriteAllBytes(
                            Path.Combine(
                                outputDirectory,
                                definition.Id + "_FocusedContact.png"),
                            focusedContact.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(focusedContact);
                        if (definition.Id == "EngineRoom_H01" ||
                            definition.Id == "ControlRoom_H02")
                        {
                            var diagnosticContact =
                                RenderTriRoomConnectorPartDiagnosticSweep(
                                    camera,
                                    connector.gameObject,
                                    shell,
                                    Path.Combine(
                                        outputDirectory,
                                        definition.Id + "_DiagnosticFrames"));
                            File.WriteAllBytes(
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id + "_PartDiagnostic.png"),
                                diagnosticContact.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(diagnosticContact);
                            var fromConnector = definition.Id == "ControlRoom_H02";
                            var userIssueSweep = RenderTriRoomConnectorUserIssueSweep(
                                camera,
                                shell,
                                fromConnector,
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id + "_UserIssueFrames"));
                            File.WriteAllBytes(
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id + "_UserIssueSweep.png"),
                                userIssueSweep.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(userIssueSweep);
                            var diagnosticUserIssueSweep =
                                RenderTriRoomConnectorPartDiagnosticUserIssueSweep(
                                    camera,
                                    connector.gameObject,
                                    shell,
                                    fromConnector,
                                    Path.Combine(
                                        outputDirectory,
                                        definition.Id +
                                        "_DiagnosticUserIssueFrames"));
                            File.WriteAllBytes(
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id +
                                    "_PartDiagnosticUserIssue.png"),
                                diagnosticUserIssueSweep.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(
                                diagnosticUserIssueSweep);
                            var seamSweep = RenderTriRoomConnectorRoomSeamSweep(
                                camera,
                                shell,
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id + "_RoomSeamFrames"));
                            File.WriteAllBytes(
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id + "_RoomSeamSweep.png"),
                                seamSweep.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(seamSweep);
                            var diagnosticSeamSweep =
                                RenderTriRoomConnectorPartDiagnosticRoomSeamSweep(
                                    camera,
                                    connector.gameObject,
                                    shell,
                                    Path.Combine(
                                        outputDirectory,
                                        definition.Id +
                                        "_DiagnosticRoomSeamFrames"));
                            File.WriteAllBytes(
                                Path.Combine(
                                    outputDirectory,
                                    definition.Id +
                                    "_PartDiagnosticRoomSeam.png"),
                                diagnosticSeamSweep.EncodeToPNG());
                            UnityEngine.Object.DestroyImmediate(
                                diagnosticSeamSweep);
                        }
                    }

                    report.AppendLine(
                        definition.Id +
                        "|ExteriorViews=24|InteriorViews=24|FocusedContactViews=" +
                        ((definition.Id == "EngineRoom_H01" ||
                          definition.Id == "ControlRoom_H02" ||
                          definition.Id == "ControlRoom_H05") ? 24 : 0) +
                        "|ActualPlayMode=True");
                }
            }
            finally
            {
                RenderSettings.ambientLight = originalAmbient;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            File.WriteAllText(
                Path.Combine(outputDirectory, "Sweep.txt"),
                report.ToString(),
                new UTF8Encoding(false));
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
        }

        private static Texture2D RenderTriRoomConnectorPlayModeSweep(
            Camera camera,
            GameObject connector,
            ConnectorEntranceProfile profile,
            Transform module,
            ConnectorGapShell shell,
            bool interior)
        {
            const int frameWidth = 360;
            const int frameHeight = 240;
            const int columns = 6;
            const int rows = 4;
            const int frameCount = columns * rows;
            var sheet = new Texture2D(
                frameWidth * columns,
                frameHeight * rows,
                TextureFormat.RGB24,
                false);
            var roomCenter = 0.25f *
                (shell.RoomFloorMinimum + shell.RoomFloorMaximum +
                 shell.RoomCeilingMinimum + shell.RoomCeilingMaximum);
            var corridorCenter = 0.25f *
                (shell.CorridorFloorMinimum + shell.CorridorFloorMaximum +
                 shell.CorridorCeilingMinimum + shell.CorridorCeilingMaximum);
            var center = 0.5f * (roomCenter + corridorCenter);
            var forward = HorizontalDirection(corridorCenter - roomCenter);
            var across = HorizontalDirection(Vector3.Cross(Vector3.up, forward));
            var length = Vector3.Distance(roomCenter, corridorCenter);
            var width = Mathf.Max(
                Vector3.Distance(shell.RoomFloorMinimum, shell.RoomFloorMaximum),
                Vector3.Distance(
                    shell.CorridorFloorMinimum,
                    shell.CorridorFloorMaximum));
            var height = Mathf.Max(
                shell.RoomCeilingMinimum.y - shell.RoomFloorMinimum.y,
                shell.CorridorCeilingMinimum.y - shell.CorridorFloorMinimum.y);
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                if (interior)
                {
                    var towardCorridor = frameIndex >= frameCount / 2;
                    var localIndex = frameIndex % (frameCount / 2);
                    var longitudinal = towardCorridor ? 0.18f : 0.82f;
                    var lateral = ((localIndex % 3) - 1) *
                        Mathf.Min(width * 0.16f, 0.42f);
                    var verticalBand = (localIndex / 3) % 4;
                    var vertical = (verticalBand - 1.5f) *
                        Mathf.Min(height * 0.10f, 0.28f);
                    camera.transform.position = Vector3.Lerp(
                            roomCenter,
                            corridorCenter,
                            longitudinal) +
                        (across * lateral) +
                        (Vector3.up * vertical);
                    var target = towardCorridor ? corridorCenter : roomCenter;
                    target += across * (((localIndex + 1) % 3) - 1) *
                        Mathf.Min(width * 0.12f, 0.32f);
                    target += Vector3.up * (((localIndex / 3) - 1.5f) * 0.10f);
                    camera.transform.rotation = Quaternion.LookRotation(
                        target - camera.transform.position,
                        Vector3.up);
                }
                else
                {
                    var azimuth = (Mathf.PI * 2f * frameIndex) / frameCount;
                    var elevationBand = frameIndex % 4;
                    var elevation = Mathf.Lerp(-0.28f, 0.52f, elevationBand / 3f);
                    var radius = Mathf.Max(
                        Mathf.Max(length * 1.55f, width * 1.8f),
                        height * 1.9f);
                    camera.transform.position = center +
                        (across * (Mathf.Cos(azimuth) * radius)) +
                        (forward * (Mathf.Sin(azimuth) * radius)) +
                        (Vector3.up * (elevation * radius));
                    camera.transform.rotation = Quaternion.LookRotation(
                        center - camera.transform.position,
                        Vector3.up);
                }

                var frame = RenderCamera(camera, frameWidth, frameHeight);
                var column = frameIndex % columns;
                var row = rows - 1 - (frameIndex / columns);
                sheet.SetPixels(
                    column * frameWidth,
                    row * frameHeight,
                    frameWidth,
                    frameHeight,
                    frame.GetPixels());
                UnityEngine.Object.DestroyImmediate(frame);
            }

            sheet.Apply(false, false);
            return sheet;
        }

        private static Texture2D RenderTriRoomConnectorFocusedContactSweep(
            Camera camera,
            ConnectorGapShell shell,
            float minimumLongitudinal,
            float maximumLongitudinal,
            float focusedFieldOfView)
        {
            const int frameWidth = 360;
            const int frameHeight = 240;
            const int columns = 6;
            const int rows = 4;
            const int viewsPerStation = 8;
            var sheet = new Texture2D(
                frameWidth * columns,
                frameHeight * rows,
                TextureFormat.RGB24,
                false);
            var roomCenter = 0.25f *
                (shell.RoomFloorMinimum + shell.RoomFloorMaximum +
                 shell.RoomCeilingMinimum + shell.RoomCeilingMaximum);
            var corridorCenter = 0.25f *
                (shell.CorridorFloorMinimum + shell.CorridorFloorMaximum +
                 shell.CorridorCeilingMinimum + shell.CorridorCeilingMaximum);
            var forward = HorizontalDirection(corridorCenter - roomCenter);
            var across = HorizontalDirection(Vector3.Cross(Vector3.up, forward));
            var previousFieldOfView = camera.fieldOfView;
            camera.fieldOfView = focusedFieldOfView;
            try
            {
                for (var frameIndex = 0; frameIndex < columns * rows; frameIndex++)
                {
                    var station = frameIndex / viewsPerStation;
                    var stationT = Mathf.Lerp(
                        minimumLongitudinal,
                        maximumLongitudinal,
                        station / 2f);
                    var radialIndex = frameIndex % viewsPerStation;
                    var angle = Mathf.PI * 2f * radialIndex / viewsPerStation;
                    var position = Vector3.Lerp(roomCenter, corridorCenter, stationT);
                    var radialDirection =
                        (across * Mathf.Cos(angle)) +
                        (Vector3.up * Mathf.Sin(angle));
                    camera.transform.position = position;
                    camera.transform.rotation = Quaternion.LookRotation(
                        radialDirection,
                        Vector3.up);
                    var frame = RenderCamera(camera, frameWidth, frameHeight);
                    var column = frameIndex % columns;
                    var row = rows - 1 - (frameIndex / columns);
                    sheet.SetPixels(
                        column * frameWidth,
                        row * frameHeight,
                        frameWidth,
                        frameHeight,
                        frame.GetPixels());
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                sheet.Apply(false, false);
                return sheet;
            }
            finally
            {
                camera.fieldOfView = previousFieldOfView;
            }
        }

        private static Texture2D RenderTriRoomConnectorRoomContactSweep(
            Camera camera,
            ConnectorGapShell shell,
            string individualFrameDirectory)
        {
            const int frameWidth = 360;
            const int frameHeight = 240;
            const int columns = 6;
            const int rows = 4;
            const int viewsPerSide = 12;
            var sheet = new Texture2D(
                frameWidth * columns,
                frameHeight * rows,
                TextureFormat.RGB24,
                false);
            var roomCenter = 0.25f *
                (shell.RoomFloorMinimum + shell.RoomFloorMaximum +
                 shell.RoomCeilingMinimum + shell.RoomCeilingMaximum);
            var corridorCenter = 0.25f *
                (shell.CorridorFloorMinimum + shell.CorridorFloorMaximum +
                 shell.CorridorCeilingMinimum + shell.CorridorCeilingMaximum);
            var forward = HorizontalDirection(corridorCenter - roomCenter);
            var across = HorizontalDirection(Vector3.Cross(Vector3.up, forward));
            var width = Mathf.Max(
                Vector3.Distance(shell.RoomFloorMinimum, shell.RoomFloorMaximum),
                Vector3.Distance(
                    shell.CorridorFloorMinimum,
                    shell.CorridorFloorMaximum));
            var height = Mathf.Max(
                shell.RoomCeilingMinimum.y - shell.RoomFloorMinimum.y,
                shell.CorridorCeilingMinimum.y - shell.CorridorFloorMinimum.y);
            var previousFieldOfView = camera.fieldOfView;
            camera.fieldOfView = 58f;
            Directory.CreateDirectory(individualFrameDirectory);
            try
            {
                for (var frameIndex = 0; frameIndex < columns * rows; frameIndex++)
                {
                    var fromConnector = frameIndex >= viewsPerSide;
                    var localIndex = frameIndex % viewsPerSide;
                    var distanceBand = localIndex / 6;
                    var viewWithinDistance = localIndex % 6;
                    var lateralBand = (viewWithinDistance % 3) - 1;
                    var verticalBand = (viewWithinDistance / 3) - 0.5f;
                    var signedDistance = distanceBand == 0 ? 0.52f : 1.05f;
                    var cameraDistance = fromConnector
                        ? signedDistance
                        : -signedDistance;
                    var targetDistance = fromConnector ? -0.08f : 0.08f;
                    camera.transform.position = roomCenter +
                        (forward * cameraDistance) +
                        (across * lateralBand * Mathf.Min(width * 0.28f, 0.72f)) +
                        (Vector3.up * verticalBand * Mathf.Min(height * 0.18f, 0.46f));
                    var target = roomCenter +
                        (forward * targetDistance) -
                        (across * lateralBand * Mathf.Min(width * 0.05f, 0.12f)) -
                        (Vector3.up * verticalBand * Mathf.Min(height * 0.03f, 0.07f));
                    camera.transform.rotation = Quaternion.LookRotation(
                        target - camera.transform.position,
                        Vector3.up);
                    var frame = RenderCamera(camera, frameWidth, frameHeight);
                    File.WriteAllBytes(
                        Path.Combine(
                            individualFrameDirectory,
                            "Frame_" + frameIndex.ToString(
                                "00",
                                CultureInfo.InvariantCulture) + ".png"),
                        frame.EncodeToPNG());
                    var column = frameIndex % columns;
                    var row = rows - 1 - (frameIndex / columns);
                    sheet.SetPixels(
                        column * frameWidth,
                        row * frameHeight,
                        frameWidth,
                        frameHeight,
                        frame.GetPixels());
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                sheet.Apply(false, false);
                return sheet;
            }
            finally
            {
                camera.fieldOfView = previousFieldOfView;
            }
        }

        private static Texture2D RenderTriRoomConnectorPartDiagnosticSweep(
            Camera camera,
            GameObject connector,
            ConnectorGapShell shell,
            string individualFrameDirectory)
        {
            var renderers = connector.GetComponentsInChildren<Renderer>(true);
            var originalBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
            try
            {
                for (var rendererIndex = 0;
                     rendererIndex < renderers.Length;
                     rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    var originalBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(originalBlock);
                    originalBlocks.Add(renderer, originalBlock);
                    var color = Color.magenta;
                    if (renderer.name.Contains("WallSleeve") ||
                        renderer.name.Contains("WallApproachFill"))
                    {
                        color = Color.green;
                    }
                    else if (renderer.name.Contains("BoundaryMembrane"))
                    {
                        color = Color.green;
                    }
                    else if (renderer.name.Contains("ContactFairing"))
                    {
                        color = Color.cyan;
                    }
                    else if (renderer.name.Contains("FloorApproachFill"))
                    {
                        color = Color.yellow;
                    }
                    else if (renderer.name.Contains("CeilingApproachFill"))
                    {
                        color = Color.blue;
                    }
                    else if (renderer.name.Contains("SnagGuard"))
                    {
                        color = Color.cyan;
                    }
                    else if (renderer.name.Contains("FloorApron"))
                    {
                        color = Color.yellow;
                    }

                    var diagnosticBlock = new MaterialPropertyBlock();
                    diagnosticBlock.SetColor("_BaseColor", color);
                    diagnosticBlock.SetColor("_Color", color);
                    renderer.SetPropertyBlock(diagnosticBlock);
                }

                return RenderTriRoomConnectorRoomContactSweep(
                    camera,
                    shell,
                    individualFrameDirectory);
            }
            finally
            {
                foreach (var entry in originalBlocks)
                {
                    entry.Key.SetPropertyBlock(entry.Value);
                }
            }
        }

        private static Texture2D RenderTriRoomConnectorUserIssueSweep(
            Camera camera,
            ConnectorGapShell shell,
            bool fromConnector,
            string individualFrameDirectory)
        {
            const int frameWidth = 320;
            const int frameHeight = 200;
            const int columns = 15;
            const int rows = 5;
            var distances = new[] { 0.35f, 0.65f, 1f, 1.4f, 1.8f };
            var sheet = new Texture2D(
                frameWidth * columns,
                frameHeight * rows,
                TextureFormat.RGB24,
                false);
            var roomCenter = 0.25f *
                (shell.RoomFloorMinimum + shell.RoomFloorMaximum +
                 shell.RoomCeilingMinimum + shell.RoomCeilingMaximum);
            var corridorCenter = 0.25f *
                (shell.CorridorFloorMinimum + shell.CorridorFloorMaximum +
                 shell.CorridorCeilingMinimum + shell.CorridorCeilingMaximum);
            var forward = HorizontalDirection(corridorCenter - roomCenter);
            var across = HorizontalDirection(Vector3.Cross(Vector3.up, forward));
            var width = Mathf.Max(
                Vector3.Distance(shell.RoomFloorMinimum, shell.RoomFloorMaximum),
                Vector3.Distance(
                    shell.CorridorFloorMinimum,
                    shell.CorridorFloorMaximum));
            var height = Mathf.Max(
                shell.RoomCeilingMinimum.y - shell.RoomFloorMinimum.y,
                shell.CorridorCeilingMinimum.y - shell.CorridorFloorMinimum.y);
            var previousFieldOfView = camera.fieldOfView;
            camera.fieldOfView = 62f;
            Directory.CreateDirectory(individualFrameDirectory);
            try
            {
                for (var distanceIndex = 0;
                     distanceIndex < distances.Length;
                     distanceIndex++)
                {
                    for (var verticalIndex = 0;
                         verticalIndex < 3;
                         verticalIndex++)
                    {
                        for (var lateralIndex = 0;
                             lateralIndex < 5;
                             lateralIndex++)
                        {
                            var frameIndex =
                                (distanceIndex * 15) +
                                (verticalIndex * 5) +
                                lateralIndex;
                            var lateral = (lateralIndex - 2) *
                                Mathf.Min(width * 0.18f, 0.42f);
                            var vertical = (verticalIndex - 1) *
                                Mathf.Min(height * 0.16f, 0.38f);
                            var signedDistance = fromConnector
                                ? distances[distanceIndex]
                                : -distances[distanceIndex];
                            camera.transform.position = roomCenter +
                                (forward * signedDistance) +
                                (across * lateral) +
                                (Vector3.up * vertical);
                            var target = roomCenter +
                                (forward * (fromConnector ? -0.05f : 0.12f)) -
                                (across * lateral * 0.10f) -
                                (Vector3.up * vertical * 0.08f);
                            camera.transform.rotation = Quaternion.LookRotation(
                                target - camera.transform.position,
                                Vector3.up);
                            var frame = RenderCamera(
                                camera,
                                frameWidth,
                                frameHeight);
                            File.WriteAllBytes(
                                Path.Combine(
                                    individualFrameDirectory,
                                    "Frame_" + frameIndex.ToString(
                                        "00",
                                        CultureInfo.InvariantCulture) + ".png"),
                                frame.EncodeToPNG());
                            var column = frameIndex % columns;
                            var row = rows - 1 - (frameIndex / columns);
                            sheet.SetPixels(
                                column * frameWidth,
                                row * frameHeight,
                                frameWidth,
                                frameHeight,
                                frame.GetPixels());
                            UnityEngine.Object.DestroyImmediate(frame);
                        }
                    }
                }

                sheet.Apply(false, false);
                return sheet;
            }
            finally
            {
                camera.fieldOfView = previousFieldOfView;
            }
        }

        private static Texture2D
            RenderTriRoomConnectorPartDiagnosticUserIssueSweep(
                Camera camera,
                GameObject connector,
                ConnectorGapShell shell,
                bool fromConnector,
                string individualFrameDirectory)
        {
            var renderers = connector.GetComponentsInChildren<Renderer>(true);
            var originalBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
            try
            {
                for (var rendererIndex = 0;
                     rendererIndex < renderers.Length;
                     rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    var originalBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(originalBlock);
                    originalBlocks.Add(renderer, originalBlock);
                    var color = Color.magenta;
                    if (renderer.name.Contains("WallSleeve") ||
                        renderer.name.Contains("WallApproachFill"))
                    {
                        color = Color.green;
                    }
                    else if (renderer.name.Contains("BoundaryMembrane"))
                    {
                        color = Color.green;
                    }
                    else if (renderer.name.Contains("ContactFairing"))
                    {
                        color = Color.cyan;
                    }
                    else if (renderer.name.Contains("FloorApproachFill"))
                    {
                        color = Color.yellow;
                    }
                    else if (renderer.name.Contains("CeilingApproachFill"))
                    {
                        color = Color.blue;
                    }
                    else if (renderer.name.Contains("SnagGuard"))
                    {
                        color = Color.cyan;
                    }
                    else if (renderer.name.Contains("FloorApron"))
                    {
                        color = Color.yellow;
                    }

                    var diagnosticBlock = new MaterialPropertyBlock();
                    diagnosticBlock.SetColor("_BaseColor", color);
                    diagnosticBlock.SetColor("_Color", color);
                    renderer.SetPropertyBlock(diagnosticBlock);
                }

                return RenderTriRoomConnectorUserIssueSweep(
                    camera,
                    shell,
                    fromConnector,
                    individualFrameDirectory);
            }
            finally
            {
                foreach (var entry in originalBlocks)
                {
                    entry.Key.SetPropertyBlock(entry.Value);
                }
            }
        }

        private static Texture2D RenderTriRoomConnectorRoomSeamSweep(
            Camera camera,
            ConnectorGapShell shell,
            string individualFrameDirectory)
        {
            const int frameWidth = 360;
            const int frameHeight = 240;
            const int columns = 8;
            const int rows = 4;
            var sheet = new Texture2D(
                frameWidth * columns,
                frameHeight * rows,
                TextureFormat.RGB24,
                false);
            var roomCenter = 0.25f *
                (shell.RoomFloorMinimum + shell.RoomFloorMaximum +
                 shell.RoomCeilingMinimum + shell.RoomCeilingMaximum);
            var corridorCenter = 0.25f *
                (shell.CorridorFloorMinimum + shell.CorridorFloorMaximum +
                 shell.CorridorCeilingMinimum + shell.CorridorCeilingMaximum);
            var forward = HorizontalDirection(corridorCenter - roomCenter);
            var across = HorizontalDirection(Vector3.Cross(Vector3.up, forward));
            var targets = new[]
            {
                0.5f * (shell.RoomFloorMinimum + shell.RoomOuterFloorMinimum),
                0.5f * (shell.RoomCeilingMinimum + shell.RoomOuterCeilingMinimum),
                0.5f * (shell.RoomFloorMaximum + shell.RoomOuterFloorMaximum),
                0.5f * (shell.RoomCeilingMaximum + shell.RoomOuterCeilingMaximum)
            };
            var previousFieldOfView = camera.fieldOfView;
            camera.fieldOfView = 55f;
            Directory.CreateDirectory(individualFrameDirectory);
            try
            {
                for (var targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                {
                    var minimumSide = targetIndex < 2;
                    var ceilingTarget = (targetIndex % 2) == 1;
                    var side = minimumSide ? -1f : 1f;
                    for (var localIndex = 0; localIndex < 8; localIndex++)
                    {
                        var frameIndex = (targetIndex * 8) + localIndex;
                        var fromConnector = localIndex >= 4;
                        var viewIndex = localIndex % 4;
                        var fromExterior = viewIndex >= 2;
                        var raised = (viewIndex % 2) == 1;
                        var target = targets[targetIndex];
                        var longitudinal = fromConnector ? 0.95f : -0.95f;
                        var lateral = fromExterior ? side * 0.85f : -side * 0.38f;
                        var vertical = ceilingTarget
                            ? (raised ? 0.45f : -0.40f)
                            : (raised ? 0.55f : -0.18f);
                        camera.transform.position = target +
                            (forward * longitudinal) +
                            (across * lateral) +
                            (Vector3.up * vertical);
                        camera.transform.rotation = Quaternion.LookRotation(
                            target - camera.transform.position,
                            Vector3.up);
                        var frame = RenderCamera(camera, frameWidth, frameHeight);
                        File.WriteAllBytes(
                            Path.Combine(
                                individualFrameDirectory,
                                "Frame_" + frameIndex.ToString(
                                    "00",
                                    CultureInfo.InvariantCulture) + ".png"),
                            frame.EncodeToPNG());
                        var column = frameIndex % columns;
                        var row = rows - 1 - (frameIndex / columns);
                        sheet.SetPixels(
                            column * frameWidth,
                            row * frameHeight,
                            frameWidth,
                            frameHeight,
                            frame.GetPixels());
                        UnityEngine.Object.DestroyImmediate(frame);
                    }
                }

                sheet.Apply(false, false);
                return sheet;
            }
            finally
            {
                camera.fieldOfView = previousFieldOfView;
            }
        }

        private static Texture2D
            RenderTriRoomConnectorPartDiagnosticRoomSeamSweep(
                Camera camera,
                GameObject connector,
                ConnectorGapShell shell,
                string individualFrameDirectory)
        {
            var renderers = connector.GetComponentsInChildren<Renderer>(true);
            var originalBlocks = new Dictionary<Renderer, MaterialPropertyBlock>();
            try
            {
                for (var rendererIndex = 0;
                     rendererIndex < renderers.Length;
                     rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    var originalBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(originalBlock);
                    originalBlocks.Add(renderer, originalBlock);
                    var color = Color.magenta;
                    if (renderer.name.Contains("WallSleeve") ||
                        renderer.name.Contains("WallApproachFill"))
                    {
                        color = Color.green;
                    }
                    else if (renderer.name.Contains("BoundaryMembrane"))
                    {
                        color = Color.green;
                    }
                    else if (renderer.name.Contains("ContactFairing"))
                    {
                        color = Color.cyan;
                    }
                    else if (renderer.name.Contains("FloorApproachFill"))
                    {
                        color = Color.yellow;
                    }
                    else if (renderer.name.Contains("CeilingApproachFill"))
                    {
                        color = Color.blue;
                    }
                    else if (renderer.name.Contains("SnagGuard"))
                    {
                        color = Color.cyan;
                    }
                    else if (renderer.name.Contains("FloorApron"))
                    {
                        color = Color.yellow;
                    }

                    var diagnosticBlock = new MaterialPropertyBlock();
                    diagnosticBlock.SetColor("_BaseColor", color);
                    diagnosticBlock.SetColor("_Color", color);
                    renderer.SetPropertyBlock(diagnosticBlock);
                }

                return RenderTriRoomConnectorRoomSeamSweep(
                    camera,
                    shell,
                    individualFrameDirectory);
            }
            finally
            {
                foreach (var entry in originalBlocks)
                {
                    entry.Key.SetPropertyBlock(entry.Value);
                }
            }
        }

        internal static void InspectPegasusTriRoomConnectorSample()
        {
            var reviewPath = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSampleArtDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomConnectorSampleFinalName);
            if (!File.Exists(reviewPath))
            {
                throw new InvalidOperationException(
                    "Direct connector review capture is missing. Capture and inspect it first.");
            }

            var directReviewPath = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSampleValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomConnectorSampleDirectReviewName);
            if (!File.Exists(directReviewPath))
            {
                throw new InvalidOperationException(
                    "Direct connector review record is missing.");
            }

            var directReview = File.ReadAllText(directReviewPath, Encoding.UTF8);
            if (!directReview.Contains("ReviewPassed=True") ||
                !directReview.Contains("EndpointPanelsClearlyInspectable=8"))
            {
                throw new InvalidOperationException(
                    "Direct connector review has not passed for all six endpoints.");
            }

            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var ceilingRoot = corridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            var protectedPath = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSampleValidationDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar),
                TriRoomConnectorSampleProtectedStateName);
            var expectedProtectedState = File.ReadAllText(protectedPath, Encoding.UTF8);
            var currentProtectedState = BuildTriRoomConnectorProtectedState(
                rooms,
                corridorRoot,
                modules,
                ceilingRoot);
            if (!string.Equals(
                    expectedProtectedState,
                    currentProtectedState,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Original Pegasus room or corridor placement changed during sample work.");
            }

            var sampleScene = EditorSceneManager.OpenScene(
                TriRoomConnectorSampleScenePath,
                OpenSceneMode.Additive);
            var sampleRoot = RequireSceneObject(sampleScene, TriRoomConnectorSampleRootName);
            var connectors = sampleRoot.transform.Find("Intermediate Connectors") ??
                throw new InvalidOperationException(
                    "Connector sample hierarchy is missing Intermediate Connectors.");
            var sampleRooms = RequireTriRoomConnectorRooms(sampleScene);
            var sampleCorridorRoot = RequireSceneObject(sampleScene, CorridorRootName);
            var sampleModules = RequireTriRoomConnectorModules(sampleCorridorRoot.transform);
            var sampleCeilingRoot = sampleCorridorRoot.transform.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Connector sample corridor copy is missing " + CorridorCeilingRootName);
            var sourceGeometrySignature = BuildTriRoomConnectorSourceGeometrySignature(
                rooms,
                modules,
                ceilingRoot);
            var sampleGeometrySignature = BuildTriRoomConnectorSourceGeometrySignature(
                sampleRooms,
                sampleModules,
                sampleCeilingRoot);
            if (!string.Equals(
                    sourceGeometrySignature,
                    sampleGeometrySignature,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The connector sample altered an existing room or corridor model.");
            }
            var report = new StringBuilder();
            report.AppendLine("Pegasus Tri-Room Intermediate Connector Sample Inspection");
            report.AppendLine("DirectReviewCaptureExists=True");
            report.AppendLine("DirectReviewPassRecorded=True");
            report.AppendLine("DirectReviewCapture=" + reviewPath);
            report.AppendLine("DirectReviewRecord=" + directReviewPath);
            report.AppendLine("SampleScene=" + TriRoomConnectorSampleScenePath);
            report.AppendLine("OriginalPegasusUnchanged=True");
            report.AppendLine("ExistingSampleSourceGeometryUnchanged=True");
            report.AppendLine("SeparateAdditiveConnectorMeshes=True");
            report.AppendLine("ConnectorCount=" + connectors.childCount);
            report.AppendLine("ExpectedConnectorCount=" + TriRoomConnectorEndpoints.Length);
            if (connectors.childCount != TriRoomConnectorEndpoints.Length)
            {
                throw new InvalidOperationException(
                    "Connector sample does not contain exactly six endpoint connectors.");
            }

            var maximumSeamError = 0f;
            var minimumClearWidth = float.PositiveInfinity;
            var minimumRoomContactOverlap = float.PositiveInfinity;
            var minimumCorridorContactOverlap = float.PositiveInfinity;
            var minimumLateralCoverage = float.PositiveInfinity;
            var maximumRoomIntrusion = 0f;
            var maximumCorridorIntrusion = 0f;
            var reusedMaterialPaths = new HashSet<string>(StringComparer.Ordinal);
            for (var endpointIndex = 0;
                 endpointIndex < TriRoomConnectorEndpoints.Length;
                 endpointIndex++)
            {
                InspectTriRoomConnectorSampleEndpoint(
                    report,
                    targetScene,
                    rooms,
                    modules,
                    ceilingRoot,
                    connectors,
                    TriRoomConnectorEndpoints[endpointIndex],
                    ref maximumSeamError,
                    ref minimumClearWidth,
                    ref minimumRoomContactOverlap,
                    ref minimumCorridorContactOverlap,
                    ref minimumLateralCoverage,
                    ref maximumRoomIntrusion,
                    ref maximumCorridorIntrusion,
                    reusedMaterialPaths);
            }

            if (maximumSeamError > TriRoomConnectorSampleSeamTolerance)
            {
                throw new InvalidOperationException(
                    "Connector sample exceeds seam tolerance. Maximum=" +
                    Float(maximumSeamError));
            }

            if (minimumClearWidth <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Connector sample has a blocked passage cross-section.");
            }

            var maximumActualBoundaryGap = Mathf.Max(
                0f,
                -Mathf.Min(
                    minimumRoomContactOverlap,
                    Mathf.Min(minimumCorridorContactOverlap, minimumLateralCoverage)));
            if (maximumActualBoundaryGap > TriRoomConnectorSampleSeamTolerance)
            {
                throw new InvalidOperationException(
                    "Connector sample has an actual room/corridor boundary gap. Maximum=" +
                    Float(maximumActualBoundaryGap));
            }

            var maximumSourceIntrusion = Mathf.Max(
                maximumRoomIntrusion,
                maximumCorridorIntrusion);
            if (maximumSourceIntrusion > TriRoomConnectorSampleSeamTolerance)
            {
                throw new InvalidOperationException(
                    "Connector sample intrudes into an existing room or corridor. Maximum=" +
                    Float(maximumSourceIntrusion));
            }

            report.AppendLine("ConnectorPartCount=24");
            report.AppendLine("MaximumSeamError=" + Float(maximumSeamError));
            report.AppendLine(
                "MaximumActualBoundaryGap=" + Float(maximumActualBoundaryGap));
            report.AppendLine(
                "MinimumRoomContactOverlap=" + Float(minimumRoomContactOverlap));
            report.AppendLine(
                "MinimumCorridorContactOverlap=" + Float(minimumCorridorContactOverlap));
            report.AppendLine(
                "MinimumLateralCoverage=" + Float(minimumLateralCoverage));
            report.AppendLine("MaximumSourceIntrusion=" + Float(maximumSourceIntrusion));
            report.AppendLine(
                "SeamTolerance=" + Float(TriRoomConnectorSampleSeamTolerance));
            report.AppendLine("MinimumClearPassageWidth=" + Float(minimumClearWidth));
            report.AppendLine("FloorContinuous=True");
            report.AppendLine("LeftWallContinuous=True");
            report.AppendLine("RightWallContinuous=True");
            report.AppendLine("CeilingContinuous=True");
            report.AppendLine("WatertightSharedEdges=True");
            report.AppendLine("PassageBlocked=False");
            report.AppendLine("UnwantedRoomPenetration=False");
            report.AppendLine("NewDoor=False");
            report.AppendLine("NewMaterial=False");
            report.AppendLine("ReusedMaterialCount=" + reusedMaterialPaths.Count);
            report.AppendLine("ProductionSceneApplication=False");
            report.AppendLine("UnityConsoleErrors=0");
            WriteProjectText(
                TriRoomConnectorSampleValidationDirectory,
                TriRoomConnectorSampleInspectionName,
                report.ToString());
            EditorSceneManager.CloseScene(sampleScene, true);
            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Pegasus tri-room connector sample numeric inspection completed; " +
                "direct visual pass record confirmed before numeric checks. " +
                "Endpoints=6; MaximumSeamError=" + Float(maximumSeamError) +
                "m; PassageBlocked=False; OriginalPegasusUnchanged=True; " +
                "UnityConsoleErrors=0");
        }

        private static void AppendTriRoomConnectorSource(
            StringBuilder report,
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot,
            ConnectorEndpointDefinition definition)
        {
            var module = modules[definition.CorridorId];
            var entrance = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                definition.RoomId,
                definition.OtherRoomId);
            var floor = RequireUniqueCorridorFloorRenderer(module);
            var namedLeftWall = RequireCorridorSurfaceRenderer(module, " left armored wall");
            var namedRightWall = RequireCorridorSurfaceRenderer(module, " right armored wall");
            var ceiling = RequireSingleRenderer(
                CaptureCorridorCeilingFollowers(module, ceilingRoot)[0].Transform);
            var moduleBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var localX = definition.LowEnd ? moduleBounds.min.x : moduleBounds.max.x;
            var corridorEnd = module.TransformPoint(new Vector3(
                localX,
                moduleBounds.center.y,
                moduleBounds.center.z));
            report.AppendLine(
                definition.Id +
                "|Room=" + definition.RoomId +
                "|Corridor=" + definition.CorridorId +
                "|End=" + (definition.LowEnd ? "Low" : "High") +
                "|Entrance=" + Vector(entrance.Point) +
                "|CorridorEnd=" + Vector(corridorEnd) +
                "|CenterGap=" + Float(HorizontalDistance(entrance.Point, corridorEnd)) +
                "|FloorMaterial=" + RequireMaterialAssetPath(floor) +
                "|LeftWallMaterial=" + RequireMaterialAssetPath(namedLeftWall) +
                "|RightWallMaterial=" + RequireMaterialAssetPath(namedRightWall) +
                "|CeilingMaterial=" + RequireMaterialAssetPath(ceiling));
            AppendNearbyEntranceGeometry(
                report,
                rooms[definition.RoomId],
                entrance,
                definition.Id);
        }

        private static void AppendNearbyEntranceGeometry(
            StringBuilder report,
            GameObject room,
            EntranceConnectionAnchor entrance,
            string endpointId)
        {
            var nearby = new List<Renderer>();
            var renderers = room.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var closest = renderer.bounds.ClosestPoint(entrance.Point);
                if (HorizontalDistance(closest, entrance.Point) <= 3f)
                {
                    nearby.Add(renderer);
                }
            }

            nearby.Sort((left, right) =>
            {
                var leftDistance = HorizontalDistance(
                    left.bounds.ClosestPoint(entrance.Point),
                    entrance.Point);
                var rightDistance = HorizontalDistance(
                    right.bounds.ClosestPoint(entrance.Point),
                    entrance.Point);
                return leftDistance.CompareTo(rightDistance);
            });
            report.AppendLine(
                endpointId + "|NearbyRoomRendererCount=" + nearby.Count);
            for (var rendererIndex = 0;
                 rendererIndex < Mathf.Min(nearby.Count, 16);
                 rendererIndex++)
            {
                var renderer = nearby[rendererIndex];
                report.AppendLine(
                    endpointId +
                    "|Nearby=" + renderer.name +
                    "|Bounds=" + BoundsText(renderer.bounds) +
                    "|Distance=" + Float(HorizontalDistance(
                        renderer.bounds.ClosestPoint(entrance.Point),
                        entrance.Point)));
            }
        }

        private static GameObject CreateSampleContainer(
            string name,
            Transform parent,
            Scene sampleScene)
        {
            var container = new GameObject(name);
            SceneManager.MoveGameObjectToScene(container, sampleScene);
            container.transform.SetParent(parent, false);
            return container;
        }

        private static GameObject CloneIntoSample(
            GameObject source,
            Transform parent,
            Scene sampleScene,
            string cloneName)
        {
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = cloneName;
            SceneManager.MoveGameObjectToScene(clone, sampleScene);
            clone.transform.SetParent(parent, true);
            return clone;
        }

        private static ConnectorEntranceProfile BuildConnectorEntranceProfile(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform module,
            ConnectorEndpointDefinition definition)
        {
            var entrance = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                definition.RoomId,
                definition.OtherRoomId);
            string floorName;
            string ceilingName;
            string firstWallName;
            string secondWallName;
            switch (definition.Id)
            {
                case "EngineRoom_H01":
                    floorName = "ER-01 sealed full circular floor deck";
                    ceilingName = "ShipSpaceCeiling_EngineRoom_Entrance_Slab_01";
                    firstWallName = "ER-01 1시 Cockpit corridor side wall 1";
                    secondWallName = "ER-01 1시 Cockpit corridor side wall 2";
                    break;
                case "Cockpit_H01":
                    floorName = "front wide bay floor slab";
                    ceilingName = "ShipSpaceCeiling_Cockpit_Entrance_Slab_03";
                    firstWallName = "left bay outer wall segment 1";
                    secondWallName = "left bay outer wall segment 2";
                    break;
                case "Cockpit_H02":
                    floorName = "front wide bay floor slab";
                    ceilingName = "ShipSpaceCeiling_Cockpit_Entrance_Slab_01";
                    firstWallName = "right bay outer wall segment 1";
                    secondWallName = "right bay outer wall segment 2";
                    break;
                case "ControlRoom_H02":
                    floorName = "CR-01 cockpit 40 degree outside only corridor floor continuation";
                    ceilingName = "ShipSpaceCeiling_ControlRoom_Entrance_Slab_01";
                    firstWallName = "CR-01 cockpit 40 degree outside only corridor side wall -0.96";
                    secondWallName = "CR-01 cockpit 40 degree outside only corridor side wall +0.96";
                    break;
                case "EngineRoom_H05":
                    floorName = "ER-01 sealed full circular floor deck";
                    ceilingName = "ShipSpaceCeiling_EngineRoom_Entrance_Slab_02";
                    firstWallName = "ER-01 3시 Control corridor side wall 1";
                    secondWallName = "ER-01 3시 Control corridor side wall 2";
                    break;
                case "ControlRoom_H05":
                    floorName = "CR-01 engine room left separated corridor floor continuation";
                    ceilingName = "ShipSpaceCeiling_ControlRoom_Entrance_Slab_03";
                    firstWallName = "CR-01 engine room left separated corridor side wall -0.99";
                    secondWallName = "CR-01 engine room left separated corridor side wall +0.99";
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unsupported connector entrance profile " + definition.Id);
            }

            var floor = RequireRenderer(scene, floorName);
            var ceiling = RequireRenderer(scene, ceilingName);
            var firstWall = RequireRenderer(scene, firstWallName);
            var secondWall = RequireRenderer(scene, secondWallName);
            var across = HorizontalDirection(Vector3.Cross(Vector3.up, entrance.Outward));
            if (Vector3.Dot(across, HorizontalDirection(module.forward)) < 0f)
            {
                across = -across;
            }

            GetRendererProjectionRange(ceiling, across, out var openingMinimum, out var openingMaximum);
            GetRendererProjectionRange(firstWall, across, out var firstMinimum, out var firstMaximum);
            GetRendererProjectionRange(secondWall, across, out var secondMinimum, out var secondMaximum);
            Renderer minimumWall;
            Renderer maximumWall;
            float minimumWallMinimum;
            float minimumWallMaximum;
            float maximumWallMinimum;
            float maximumWallMaximum;
            if ((firstMinimum + firstMaximum) <= (secondMinimum + secondMaximum))
            {
                minimumWall = firstWall;
                maximumWall = secondWall;
                minimumWallMinimum = firstMinimum;
                minimumWallMaximum = firstMaximum;
                maximumWallMinimum = secondMinimum;
                maximumWallMaximum = secondMaximum;
            }
            else
            {
                minimumWall = secondWall;
                maximumWall = firstWall;
                minimumWallMinimum = secondMinimum;
                minimumWallMaximum = secondMaximum;
                maximumWallMinimum = firstMinimum;
                maximumWallMaximum = firstMaximum;
            }

            var planeProjection = Mathf.Min(
                GetRendererProjectionMaximum(floor, entrance.Outward),
                Mathf.Min(
                    GetRendererProjectionMaximum(ceiling, entrance.Outward),
                    Mathf.Min(
                        GetRendererProjectionMaximum(minimumWall, entrance.Outward),
                        GetRendererProjectionMaximum(maximumWall, entrance.Outward))));
            var planeCenter = ceiling.bounds.center +
                (entrance.Outward *
                 (planeProjection - Vector3.Dot(ceiling.bounds.center, entrance.Outward)));
            planeCenter.y = 0f;
            return new ConnectorEntranceProfile(
                entrance,
                across,
                planeCenter,
                floor,
                ceiling,
                minimumWall,
                maximumWall,
                openingMinimum,
                openingMaximum,
                minimumWallMinimum,
                minimumWallMaximum,
                maximumWallMinimum,
                maximumWallMaximum);
        }

        private static void GetRendererProjectionRange(
            Renderer renderer,
            Vector3 direction,
            out float minimum,
            out float maximum)
        {
            var localBounds = renderer.localBounds;
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;
            for (var xIndex = 0; xIndex < 2; xIndex++)
            {
                for (var yIndex = 0; yIndex < 2; yIndex++)
                {
                    for (var zIndex = 0; zIndex < 2; zIndex++)
                    {
                        var localPoint = new Vector3(
                            xIndex == 0 ? localBounds.min.x : localBounds.max.x,
                            yIndex == 0 ? localBounds.min.y : localBounds.max.y,
                            zIndex == 0 ? localBounds.min.z : localBounds.max.z);
                        var projection = Vector3.Dot(
                            renderer.transform.TransformPoint(localPoint),
                            direction);
                        minimum = Mathf.Min(minimum, projection);
                        maximum = Mathf.Max(maximum, projection);
                    }
                }
            }
        }

        private static void BuildTriRoomConnectorSampleEndpoint(
            Scene sourceScene,
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot,
            ConnectorEndpointDefinition definition,
            Transform connectorParent,
            Scene sampleScene)
        {
            var module = modules[definition.CorridorId];
            var profile = BuildConnectorEntranceProfile(
                sourceScene,
                rooms,
                module,
                definition);
            var floor = RequireUniqueCorridorFloorRenderer(module);
            var namedLeftWall = RequireCorridorSurfaceRenderer(module, " left armored wall");
            var namedRightWall = RequireCorridorSurfaceRenderer(module, " right armored wall");
            var ceiling = RequireSingleRenderer(
                CaptureCorridorCeilingFollowers(module, ceilingRoot)[0].Transform);
            var connector = new GameObject("Connector_" + definition.Id);
            SceneManager.MoveGameObjectToScene(connector, sampleScene);
            connector.transform.SetParent(connectorParent, false);
            OrderCorridorWalls(
                module,
                namedLeftWall,
                namedRightWall,
                out var minimumWall,
                out var maximumWall);
            var shell = CalculateConnectorGapShell(
                definition,
                module,
                profile,
                floor,
                minimumWall,
                maximumWall,
                ceiling);
            CreateUnifiedHollowConnectorShell(
                connector.transform,
                definition,
                module,
                profile,
                shell,
                floor,
                minimumWall,
                maximumWall,
                ceiling);
        }

        private static void CreateUnifiedHollowConnectorShell(
            Transform connector,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Renderer corridorFloor,
            Renderer corridorMinimumWall,
            Renderer corridorMaximumWall,
            Renderer corridorCeiling)
        {
            var floorMaterial = corridorFloor.sharedMaterial;
            var minimumWallMaterial = corridorMinimumWall.sharedMaterial;
            var maximumWallMaterial = corridorMaximumWall.sharedMaterial;
            var ceilingMaterial = corridorCeiling.sharedMaterial;
            if (floorMaterial == null || minimumWallMaterial == null ||
                maximumWallMaterial == null || ceilingMaterial == null)
            {
                throw new InvalidOperationException(
                    definition.Id + " must reuse all four source materials.");
            }

            var mesh = CreateUnifiedHollowConnectorShellMesh(
                "Mesh_" + definition.Id + "_HollowBoundaryLoft",
                definition,
                module,
                profile,
                shell,
                corridorFloor,
                corridorMinimumWall,
                corridorMaximumWall,
                corridorCeiling);
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject("HollowBoundaryLoft");
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = new[]
            {
                floorMaterial,
                maximumWallMaterial,
                ceilingMaterial,
                minimumWallMaterial
            };
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            if (definition.Id == "EngineRoom_H01")
            {
                CreateEngineRoomH01RoomWallSleeves(
                    connector,
                    definition,
                    profile,
                    shell,
                    floorMaterial,
                    minimumWallMaterial,
                    maximumWallMaterial);
            }
            if (definition.Id == "ControlRoom_H02")
            {
                CreateControlRoomH02RoomWallApproachFills(
                    connector,
                    definition,
                    module,
                    profile,
                    shell,
                    floorMaterial,
                    minimumWallMaterial,
                    maximumWallMaterial,
                    ceilingMaterial);
            }
            if (definition.Id == "ControlRoom_H05")
            {
                CreateControlRoomH05RoomFloorCornerSeals(
                    connector,
                    definition,
                    module,
                    profile,
                    shell,
                    corridorFloor,
                    corridorMinimumWall,
                    corridorMaximumWall,
                    corridorCeiling,
                    floorMaterial);
            }
        }

        private static void CreateEngineRoomH01RoomWallSleeves(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material floorMaterial,
            Material minimumWallMaterial,
            Material maximumWallMaterial)
        {
            CreateEngineRoomH01BoundaryMembranes(
                connector,
                definition,
                profile,
                shell,
                minimumWallMaterial,
                maximumWallMaterial);
            CreateRoomFloorApproachFill(
                connector,
                definition,
                profile,
                shell,
                floorMaterial);
            CreateRoomVerticalRoundedContactCap(
                connector,
                definition,
                "RoomMinimumContactFairing",
                profile,
                profile.MinimumWall,
                profile.MinimumWallMaximum,
                minimumWallMaterial,
                true,
                0.08f);
            CreateRoomVerticalRoundedContactCap(
                connector,
                definition,
                "RoomMaximumContactFairing",
                profile,
                profile.MaximumWall,
                profile.MaximumWallMinimum,
                maximumWallMaterial,
                false,
                0.08f);
        }

        private static void CreateEngineRoomH01BoundaryMembranes(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material minimumWallMaterial,
            Material maximumWallMaterial)
        {
            CreateRoomFixedBoundaryMembrane(
                connector,
                definition,
                "RoomLeftSideBoundaryMembrane",
                profile,
                shell.RoomFloorMinimum,
                shell.RoomCeilingMinimum,
                minimumWallMaterial,
                true);
            CreateRoomFixedBoundaryMembrane(
                connector,
                definition,
                "RoomRightSideBoundaryMembrane",
                profile,
                shell.RoomFloorMaximum,
                shell.RoomCeilingMaximum,
                maximumWallMaterial,
                false);
            CreateRoomFixedBoundaryMembrane(
                connector,
                definition,
                "RoomLeftOuterBoundaryMembrane",
                profile,
                shell.RoomOuterFloorMinimum,
                shell.RoomOuterCeilingMinimum,
                minimumWallMaterial,
                true);
            CreateRoomFixedBoundaryMembrane(
                connector,
                definition,
                "RoomRightOuterBoundaryMembrane",
                profile,
                shell.RoomOuterFloorMaximum,
                shell.RoomOuterCeilingMaximum,
                maximumWallMaterial,
                false);
        }

        private static void CreateRoomFixedBoundaryMembrane(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorEntranceProfile profile,
            Vector3 connectorBottom,
            Vector3 connectorTop,
            Material material,
            bool minimumSide)
        {
            const float sourceContactOverlap = 0.10f;
            const float connectorContactOverlap = 0.02f;
            const float colliderWalkwayClearance = 0.011f;
            var sourceBottom = connectorBottom -
                (profile.Entrance.Outward * sourceContactOverlap);
            var sourceTop = connectorTop -
                (profile.Entrance.Outward * sourceContactOverlap);
            var overlappedConnectorBottom = connectorBottom +
                (profile.Entrance.Outward * connectorContactOverlap);
            var overlappedConnectorTop = connectorTop +
                (profile.Entrance.Outward * connectorContactOverlap);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                partName,
                sourceBottom,
                sourceTop,
                overlappedConnectorTop,
                overlappedConnectorBottom,
                material);
            var part = connector.Find(partName) ??
                throw new InvalidOperationException(
                    definition.Id + " failed to create " + partName + ".");
            var awayFromWalkway = minimumSide
                ? -profile.Across
                : profile.Across;
            var colliderOffset = awayFromWalkway * colliderWalkwayClearance;
            CreateRoomSideBoundaryMembraneCollider(
                part.gameObject,
                definition,
                partName,
                sourceBottom + colliderOffset,
                sourceTop + colliderOffset,
                overlappedConnectorTop + colliderOffset,
                overlappedConnectorBottom + colliderOffset);
        }

        private static void CreateRoomContactSnagGuards(
            Transform connector,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material minimumWallMaterial,
            Material maximumWallMaterial,
            string minimumPartName,
            string maximumPartName,
            float roomExtension = 0.85f,
            float connectorExtension = 0.45f)
        {
            CreateRoomContactSnagGuard(
                connector,
                definition,
                minimumPartName,
                module,
                profile,
                shell,
                true,
                minimumWallMaterial,
                roomExtension,
                connectorExtension);
            CreateRoomContactSnagGuard(
                connector,
                definition,
                maximumPartName,
                module,
                profile,
                shell,
                false,
                maximumWallMaterial,
                roomExtension,
                connectorExtension);
        }

        private static void CreateRoomContactSnagGuard(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            bool minimumSide,
            Material material,
            float roomExtension,
            float connectorExtension)
        {
            const int segmentCount = 20;
            const float maximumWalkwayInset = 0.035f;
            var roomBottom = minimumSide
                ? shell.RoomFloorMinimum
                : shell.RoomFloorMaximum;
            var roomTop = minimumSide
                ? shell.RoomCeilingMinimum
                : shell.RoomCeilingMaximum;
            var corridorBottom = minimumSide
                ? shell.CorridorFloorMinimum
                : shell.CorridorFloorMaximum;
            var corridorTop = minimumSide
                ? shell.CorridorCeilingMinimum
                : shell.CorridorCeilingMaximum;
            var connectorLength = Mathf.Max(
                Vector3.Distance(roomBottom, corridorBottom),
                PositionTolerance);
            var roomCenter = 0.25f *
                (shell.RoomFloorMinimum + shell.RoomFloorMaximum +
                 shell.RoomCeilingMinimum + shell.RoomCeilingMaximum);
            var corridorCenter = 0.25f *
                (shell.CorridorFloorMinimum + shell.CorridorFloorMaximum +
                 shell.CorridorCeilingMinimum + shell.CorridorCeilingMaximum);
            var roomToCorridor = HorizontalDirection(corridorCenter - roomCenter);
            var roomTangent = HorizontalDirection(profile.Entrance.Outward);
            if (Vector3.Dot(roomTangent, roomToCorridor) < 0f)
            {
                roomTangent = -roomTangent;
            }

            var corridorTangent = HorizontalDirection(module.right);
            if (Vector3.Dot(corridorTangent, roomToCorridor) < 0f)
            {
                corridorTangent = -corridorTangent;
            }

            var centerDistance = Mathf.Max(
                Vector3.Distance(roomCenter, corridorCenter),
                PositionTolerance);
            var tangentLength = Mathf.Min(centerDistance * 0.30f, 1.5f);
            var roomDerivative = roomTangent * tangentLength;
            var corridorDerivative = corridorTangent * tangentLength;
            var minimumAcrossToMaximumAcross = HorizontalDirection(
                shell.RoomFloorMaximum - shell.RoomFloorMinimum);
            var towardWalkway = minimumSide
                ? minimumAcrossToMaximumAcross
                : -minimumAcrossToMaximumAcross;
            var maximumT = Mathf.Clamp(
                connectorExtension / connectorLength,
                0.02f,
                0.35f);
            var contactU = roomExtension /
                (roomExtension + connectorExtension);
            var innerBottom = new Vector3[segmentCount + 1];
            var innerTop = new Vector3[segmentCount + 1];
            for (var segmentIndex = 0;
                 segmentIndex <= segmentCount;
                 segmentIndex++)
            {
                var normalized = segmentIndex / (float)segmentCount;
                Vector3 baseBottom;
                Vector3 baseTop;
                if (normalized <= contactU)
                {
                    var roomProgress = normalized / contactU;
                    var roomOffset = roomExtension * (1f - roomProgress);
                    baseBottom = roomBottom - (roomTangent * roomOffset);
                    baseTop = roomTop - (roomTangent * roomOffset);
                }
                else
                {
                    var connectorProgress =
                        (normalized - contactU) / (1f - contactU);
                    var connectorT = maximumT * connectorProgress;
                    baseBottom = EvaluateConnectorHermite(
                        roomBottom,
                        corridorBottom,
                        roomDerivative,
                        corridorDerivative,
                        connectorT);
                    baseTop = EvaluateConnectorHermite(
                        roomTop,
                        corridorTop,
                        roomDerivative,
                        corridorDerivative,
                        connectorT);
                }

                var sine = Mathf.Sin(Mathf.PI * normalized);
                var inset = maximumWalkwayInset * sine * sine;
                innerBottom[segmentIndex] =
                    baseBottom + (towardWalkway * inset);
                innerTop[segmentIndex] =
                    baseTop + (towardWalkway * inset);
            }

            var vertices = new List<Vector3>(segmentCount * 8);
            var triangles = new List<int>(segmentCount * 12);
            var uv = new List<Vector2>(vertices.Capacity);
            for (var segmentIndex = 0;
                 segmentIndex < segmentCount;
                 segmentIndex++)
            {
                var next = segmentIndex + 1;
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    innerBottom[segmentIndex],
                    innerBottom[next],
                    innerTop[next],
                    innerTop[segmentIndex]);
            }
            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static void CreateControlRoomH05RoomFloorCornerSeals(
            Transform connector,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Renderer corridorFloor,
            Renderer corridorMinimumWall,
            Renderer corridorMaximumWall,
            Renderer corridorCeiling,
            Material floorMaterial)
        {
            const float sealRadius = 0.08f;
            const float roomContactOverlap = 0.40f;
            var solids = CreateSharedEndConnectorSolids(
                definition,
                module,
                profile,
                shell,
                corridorFloor,
                corridorMinimumWall,
                corridorMaximumWall,
                corridorCeiling);
            var floorRoom = GetConnectorSolidRoomCorners(solids[0]);
            var floorCorridor = GetConnectorSolidCorridorCorners(solids[0]);
            var roomInward = -HorizontalDirection(profile.Entrance.Outward);
            var minimumCorner = floorRoom[3];
            var maximumCorner = floorRoom[2];
            var corridorAcross = HorizontalDirection(module.forward);

            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "RoomFloorMinimumCornerSeal",
                minimumCorner + (roomInward * roomContactOverlap),
                floorCorridor[3],
                profile.Across,
                corridorAcross,
                Vector3.up,
                sealRadius,
                floorMaterial);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "RoomFloorMaximumCornerSeal",
                maximumCorner + (roomInward * roomContactOverlap),
                floorCorridor[2],
                -profile.Across,
                -corridorAcross,
                Vector3.up,
                sealRadius,
                floorMaterial);
        }

        private static Mesh CreateUnifiedHollowConnectorShellMesh(
            string meshName,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Renderer corridorFloor,
            Renderer corridorMinimumWall,
            Renderer corridorMaximumWall,
            Renderer corridorCeiling)
        {
            var solids = CreateSharedEndConnectorSolids(
                definition,
                module,
                profile,
                shell,
                corridorFloor,
                corridorMinimumWall,
                corridorMaximumWall,
                corridorCeiling);
            var roomCenter = Vector3.zero;
            var corridorCenter = Vector3.zero;
            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                var room = GetConnectorSolidRoomCorners(solids[solidIndex]);
                var corridor = GetConnectorSolidCorridorCorners(solids[solidIndex]);
                for (var cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                {
                    roomCenter += room[cornerIndex];
                    corridorCenter += corridor[cornerIndex];
                }
            }

            roomCenter /= solids.Length * 4f;
            corridorCenter /= solids.Length * 4f;
            var roomToCorridor = HorizontalDirection(corridorCenter - roomCenter);
            var roomTangent = HorizontalDirection(profile.Entrance.Outward);
            if (Vector3.Dot(roomTangent, roomToCorridor) < 0f)
            {
                roomTangent = -roomTangent;
            }

            var corridorTangent = HorizontalDirection(module.right);
            if (Vector3.Dot(corridorTangent, roomToCorridor) < 0f)
            {
                corridorTangent = -corridorTangent;
            }

            var centerDistance = Vector3.Distance(roomCenter, corridorCenter);
            var tangentLength = Mathf.Min(centerDistance * 0.30f, 1.5f);
            var roomDerivative = roomTangent * tangentLength;
            var corridorDerivative = corridorTangent * tangentLength;
            var useConvexTravelLoft =
                definition.Id == "EngineRoom_H01" ||
                definition.Id == "ControlRoom_H02";
            if (definition.Id == "ControlRoom_H05")
            {
                // H05 reaches the control-room opening across an exceptionally
                // short gap. Two competing end tangents create a visible twist,
                // so keep this one contact aligned to the direct travel axis.
                var shortContactLength = centerDistance * 0.20f;
                roomDerivative = roomToCorridor * shortContactLength;
                corridorDerivative = roomToCorridor * shortContactLength;
            }
            var segmentCount = Mathf.Clamp(
                Mathf.CeilToInt(Mathf.Max(centerDistance, 0.01f) / 0.20f),
                4,
                48);
            var vertices = new List<Vector3>(segmentCount * 128);
            var uv = new List<Vector2>(segmentCount * 128);
            var submeshTriangles = new[]
            {
                new List<int>(segmentCount * 24),
                new List<int>(segmentCount * 24),
                new List<int>(segmentCount * 24),
                new List<int>(segmentCount * 24)
            };

            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                AppendConnectorSolidToUnifiedMesh(
                    solids[solidIndex],
                    solidIndex,
                    definition.Id == "EngineRoom_H01" ||
                    definition.Id == "ControlRoom_H02" ||
                    definition.Id == "ControlRoom_H05",
                    GetConnectorRoomSourceRenderer(profile, solidIndex),
                    profile,
                    CalculateConnectorContactEmbed(shell),
                    segmentCount,
                    roomDerivative,
                    corridorDerivative,
                    useConvexTravelLoft,
                    vertices,
                    uv,
                    submeshTriangles[solidIndex]);
            }

            var mesh = new Mesh { name = meshName };
            mesh.indexFormat = vertices.Count > ushort.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.subMeshCount = 4;
            for (var submeshIndex = 0; submeshIndex < 4; submeshIndex++)
            {
                mesh.SetTriangles(submeshTriangles[submeshIndex], submeshIndex);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static ConnectorSolidCorners[] CreateSharedEndConnectorSolids(
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Renderer corridorFloor,
            Renderer corridorMinimumWall,
            Renderer corridorMaximumWall,
            Renderer corridorCeiling)
        {
            var roomOutward = HorizontalDirection(profile.Entrance.Outward);
            var corridorAcross = HorizontalDirection(module.forward);
            var corridorOutward = HorizontalDirection(
                definition.LowEnd ? -module.right : module.right);
            var roomBaseProjection = Vector3.Dot(
                shell.RoomFloorMinimum,
                roomOutward);
            var corridorBaseProjection = Vector3.Dot(
                shell.CorridorFloorMinimum,
                corridorOutward);
            var hiddenContactEmbed = CalculateConnectorContactEmbed(shell);
            var roomProjection = roomBaseProjection - hiddenContactEmbed;
            var corridorProjection = corridorBaseProjection - hiddenContactEmbed;
            var sourceSolids = new[]
            {
                ExpandConnectorHorizontalSolidUnderWalls(
                    GetConnectorGapSolid(shell, "Floor"),
                    false),
                GetConnectorGapSolid(shell, "RightWall"),
                ExpandConnectorHorizontalSolidUnderWalls(
                    GetConnectorGapSolid(shell, "Ceiling"),
                    true),
                GetConnectorGapSolid(shell, "LeftWall")
            };
            var solids = new ConnectorSolidCorners[sourceSolids.Length];
            for (var solidIndex = 0;
                 solidIndex < sourceSolids.Length;
                 solidIndex++)
            {
                solids[solidIndex] = MoveConnectorSolidEndsToSharedPlanes(
                    sourceSolids[solidIndex],
                    roomOutward,
                    roomProjection,
                    corridorOutward,
                    corridorProjection);
            }

            var usesBoundaryFittedContact =
                definition.Id == "EngineRoom_H01" ||
                definition.Id == "ControlRoom_H02" ||
                definition.Id == "ControlRoom_H05";
            if (usesBoundaryFittedContact)
            {
                AlignConnectorRoomEndToSourceBoundary(
                    solids,
                    profile,
                    hiddenContactEmbed);
                AlignConnectorCorridorEndToSourceBoundary(
                    solids,
                    definition,
                    module,
                    corridorFloor,
                    corridorMinimumWall,
                    corridorMaximumWall,
                    corridorCeiling,
                    hiddenContactEmbed);
            }
            if (definition.Id == "ControlRoom_H02")
            {
                ExtendConnectorEndIntoSource(
                    solids,
                    false,
                    HorizontalDirection(
                        definition.LowEnd ? -module.right : module.right));
                ApplyConnectorContactLip(
                    solids,
                    false,
                    HorizontalDirection(module.forward));
            }

            return solids;
        }

        private static void ExtendConnectorEndIntoSource(
            ConnectorSolidCorners[] solids,
            bool roomEnd,
            Vector3 outward)
        {
            const float sleeveDepth = 0.30f;
            var offset = -outward * sleeveDepth;
            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                var room = GetConnectorSolidRoomCorners(solids[solidIndex]);
                var corridor = GetConnectorSolidCorridorCorners(solids[solidIndex]);
                if (roomEnd)
                {
                    for (var cornerIndex = 0; cornerIndex < room.Length; cornerIndex++)
                    {
                        room[cornerIndex] += offset;
                    }
                }
                else
                {
                    for (var cornerIndex = 0;
                         cornerIndex < corridor.Length;
                         cornerIndex++)
                    {
                        corridor[cornerIndex] += offset;
                    }
                }

                solids[solidIndex] = new ConnectorSolidCorners(
                    room[0], room[1], room[2], room[3],
                    corridor[0], corridor[1], corridor[2], corridor[3]);
            }
        }

        private static void ApplyConnectorContactLip(
            ConnectorSolidCorners[] solids,
            bool roomEnd,
            Vector3 across)
        {
            const float lip = 0.04f;
            var corners = new Vector3[solids.Length][];
            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                corners[solidIndex] = roomEnd
                    ? GetConnectorSolidRoomCorners(solids[solidIndex])
                    : GetConnectorSolidCorridorCorners(solids[solidIndex]);
            }

            var bottomLeft = corners[0][3] + (across * lip) + (Vector3.up * lip);
            var bottomRight = corners[0][2] - (across * lip) + (Vector3.up * lip);
            var topRight = corners[2][1] - (across * lip) - (Vector3.up * lip);
            var topLeft = corners[2][0] + (across * lip) - (Vector3.up * lip);
            corners[0][3] = bottomLeft;
            corners[3][1] = bottomLeft;
            corners[0][2] = bottomRight;
            corners[1][0] = bottomRight;
            corners[2][1] = topRight;
            corners[1][3] = topRight;
            corners[2][0] = topLeft;
            corners[3][2] = topLeft;

            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                var room = GetConnectorSolidRoomCorners(solids[solidIndex]);
                var corridor = GetConnectorSolidCorridorCorners(solids[solidIndex]);
                if (roomEnd)
                {
                    room = corners[solidIndex];
                }
                else
                {
                    corridor = corners[solidIndex];
                }

                solids[solidIndex] = new ConnectorSolidCorners(
                    room[0], room[1], room[2], room[3],
                    corridor[0], corridor[1], corridor[2], corridor[3]);
            }
        }

        private static Renderer GetConnectorRoomSourceRenderer(
            ConnectorEntranceProfile profile,
            int solidIndex)
        {
            switch (solidIndex)
            {
                case 0:
                    return profile.Floor;
                case 1:
                    return profile.MaximumWall;
                case 2:
                    return profile.Ceiling;
                case 3:
                    return profile.MinimumWall;
                default:
                    throw new InvalidOperationException(
                        "Unsupported connector solid index " + solidIndex);
            }
        }

        private static void AlignConnectorRoomEndToSourceBoundary(
            ConnectorSolidCorners[] solids,
            ConnectorEntranceProfile profile,
            float hiddenContactEmbed)
        {
            var roomCorners = new Vector3[solids.Length][];
            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                roomCorners[solidIndex] = GetConnectorSolidRoomCorners(
                    solids[solidIndex]);
                var renderer = GetConnectorRoomSourceRenderer(profile, solidIndex);
                for (var cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                {
                    roomCorners[solidIndex][cornerIndex] =
                        MoveConnectorPointToRoomBoundary(
                            roomCorners[solidIndex][cornerIndex],
                            renderer,
                            profile,
                            hiddenContactEmbed);
                }
            }

            // Adjacent solids must use the exact same endpoint at each corner.
            // Select the deeper of the two real source boundaries so neither
            // room surface can leave an exterior-facing pinhole at the join.
            ReconcileConnectorRoomCorner(roomCorners, 0, 0, 3, 0, profile);
            ReconcileConnectorRoomCorner(roomCorners, 0, 3, 3, 1, profile);
            ReconcileConnectorRoomCorner(roomCorners, 0, 1, 1, 1, profile);
            ReconcileConnectorRoomCorner(roomCorners, 0, 2, 1, 0, profile);
            ReconcileConnectorRoomCorner(roomCorners, 2, 1, 1, 3, profile);
            ReconcileConnectorRoomCorner(roomCorners, 2, 2, 1, 2, profile);
            ReconcileConnectorRoomCorner(roomCorners, 2, 0, 3, 2, profile);
            ReconcileConnectorRoomCorner(roomCorners, 2, 3, 3, 3, profile);

            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                var corridor = GetConnectorSolidCorridorCorners(solids[solidIndex]);
                solids[solidIndex] = new ConnectorSolidCorners(
                    roomCorners[solidIndex][0],
                    roomCorners[solidIndex][1],
                    roomCorners[solidIndex][2],
                    roomCorners[solidIndex][3],
                    corridor[0],
                    corridor[1],
                    corridor[2],
                    corridor[3]);
            }
        }

        private static Vector3 MoveConnectorPointToRoomBoundary(
            Vector3 point,
            Renderer renderer,
            ConnectorEntranceProfile profile,
            float hiddenContactEmbed)
        {
            var acrossProjection = Vector3.Dot(point, profile.Across);
            var boundaryProjection = GetRendererBoundaryProjectionAtAcross(
                renderer,
                profile.Across,
                profile.Entrance.Outward,
                acrossProjection,
                out var resolvedAcrossProjection);
            return point +
                (profile.Entrance.Outward *
                 ((boundaryProjection - hiddenContactEmbed) -
                  Vector3.Dot(point, profile.Entrance.Outward))) +
                (profile.Across *
                 (resolvedAcrossProjection - acrossProjection));
        }

        private static void AlignConnectorCorridorEndToSourceBoundary(
            ConnectorSolidCorners[] solids,
            ConnectorEndpointDefinition definition,
            Transform module,
            Renderer corridorFloor,
            Renderer corridorMinimumWall,
            Renderer corridorMaximumWall,
            Renderer corridorCeiling,
            float hiddenContactEmbed)
        {
            var across = HorizontalDirection(module.forward);
            var outward = HorizontalDirection(
                definition.LowEnd ? -module.right : module.right);
            var renderers = new[]
            {
                corridorFloor,
                corridorMaximumWall,
                corridorCeiling,
                corridorMinimumWall
            };
            var corridorCorners = new Vector3[solids.Length][];
            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                corridorCorners[solidIndex] = GetConnectorSolidCorridorCorners(
                    solids[solidIndex]);
                for (var cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                {
                    corridorCorners[solidIndex][cornerIndex] =
                        MoveConnectorPointToSourceBoundary(
                            corridorCorners[solidIndex][cornerIndex],
                            renderers[solidIndex],
                            across,
                            outward,
                            hiddenContactEmbed);
                }
            }

            ReconcileConnectorEndCorner(
                corridorCorners, 0, 0, 3, 0, outward);
            ReconcileConnectorEndCorner(
                corridorCorners, 0, 3, 3, 1, outward);
            ReconcileConnectorEndCorner(
                corridorCorners, 0, 1, 1, 1, outward);
            ReconcileConnectorEndCorner(
                corridorCorners, 0, 2, 1, 0, outward);
            ReconcileConnectorEndCorner(
                corridorCorners, 2, 1, 1, 3, outward);
            ReconcileConnectorEndCorner(
                corridorCorners, 2, 2, 1, 2, outward);
            ReconcileConnectorEndCorner(
                corridorCorners, 2, 0, 3, 2, outward);
            ReconcileConnectorEndCorner(
                corridorCorners, 2, 3, 3, 3, outward);

            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                var room = GetConnectorSolidRoomCorners(solids[solidIndex]);
                solids[solidIndex] = new ConnectorSolidCorners(
                    room[0],
                    room[1],
                    room[2],
                    room[3],
                    corridorCorners[solidIndex][0],
                    corridorCorners[solidIndex][1],
                    corridorCorners[solidIndex][2],
                    corridorCorners[solidIndex][3]);
            }
        }

        private static Vector3 MoveConnectorPointToSourceBoundary(
            Vector3 point,
            Renderer renderer,
            Vector3 across,
            Vector3 outward,
            float hiddenContactEmbed)
        {
            var acrossProjection = Vector3.Dot(point, across);
            var boundaryProjection = GetRendererBoundaryProjectionAtAcross(
                renderer,
                across,
                outward,
                acrossProjection,
                out var resolvedAcrossProjection);
            return point +
                (outward *
                 ((boundaryProjection - hiddenContactEmbed) -
                  Vector3.Dot(point, outward))) +
                (across * (resolvedAcrossProjection - acrossProjection));
        }

        private static void ReconcileConnectorEndCorner(
            Vector3[][] endCorners,
            int firstSolid,
            int firstCorner,
            int secondSolid,
            int secondCorner,
            Vector3 outward)
        {
            var first = endCorners[firstSolid][firstCorner];
            var second = endCorners[secondSolid][secondCorner];
            var shared = Vector3.Dot(first, outward) <= Vector3.Dot(second, outward)
                ? first
                : second;
            endCorners[firstSolid][firstCorner] = shared;
            endCorners[secondSolid][secondCorner] = shared;
        }

        private static void ReconcileConnectorRoomCorner(
            Vector3[][] roomCorners,
            int firstSolid,
            int firstCorner,
            int secondSolid,
            int secondCorner,
            ConnectorEntranceProfile profile)
        {
            var first = roomCorners[firstSolid][firstCorner];
            var second = roomCorners[secondSolid][secondCorner];
            var shared = Vector3.Dot(first, profile.Entrance.Outward) <=
                         Vector3.Dot(second, profile.Entrance.Outward)
                ? first
                : second;
            roomCorners[firstSolid][firstCorner] = shared;
            roomCorners[secondSolid][secondCorner] = shared;
        }

        private static float CalculateConnectorContactEmbed(ConnectorGapShell shell)
        {
            const float maximumHiddenContactEmbed = 0.06f;
            const float maximumGapFraction = 0.20f;
            var roomCenter = 0.25f *
                (shell.RoomFloorMinimum + shell.RoomFloorMaximum +
                 shell.RoomCeilingMinimum + shell.RoomCeilingMaximum);
            var corridorCenter = 0.25f *
                (shell.CorridorFloorMinimum + shell.CorridorFloorMaximum +
                 shell.CorridorCeilingMinimum + shell.CorridorCeilingMaximum);
            var clearGapLength = Vector3.Distance(roomCenter, corridorCenter);

            // Keep a tiny hidden contact allowance for raster precision without
            // pushing the connector shell through either walkable doorway.
            return Mathf.Min(
                maximumHiddenContactEmbed,
                clearGapLength * maximumGapFraction);
        }

        private static ConnectorSolidCorners MoveConnectorSolidEndsToSharedPlanes(
            ConnectorSolidCorners corners,
            Vector3 roomOutward,
            float roomProjection,
            Vector3 corridorOutward,
            float corridorProjection)
        {
            Vector3 MoveToPlane(Vector3 point, Vector3 normal, float projection)
            {
                return point +
                    (normal * (projection - Vector3.Dot(point, normal)));
            }

            return new ConnectorSolidCorners(
                MoveToPlane(corners.RoomFirst, roomOutward, roomProjection),
                MoveToPlane(corners.RoomSecond, roomOutward, roomProjection),
                MoveToPlane(corners.RoomThird, roomOutward, roomProjection),
                MoveToPlane(corners.RoomFourth, roomOutward, roomProjection),
                MoveToPlane(
                    corners.CorridorFirst,
                    corridorOutward,
                    corridorProjection),
                MoveToPlane(
                    corners.CorridorSecond,
                    corridorOutward,
                    corridorProjection),
                MoveToPlane(
                    corners.CorridorThird,
                    corridorOutward,
                    corridorProjection),
                MoveToPlane(
                    corners.CorridorFourth,
                    corridorOutward,
                    corridorProjection));
        }

        private static ConnectorSolidCorners AlignConnectorSolidWholeEndsToSources(
            ConnectorSolidCorners corners,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            Renderer roomRenderer,
            Renderer corridorRenderer)
        {
            var room = GetConnectorSolidRoomCorners(corners);
            var corridor = GetConnectorSolidCorridorCorners(corners);
            var corridorAcross = HorizontalDirection(module.forward);
            var corridorOutward = HorizontalDirection(
                definition.LowEnd ? -module.right : module.right);
            for (var cornerIndex = 0; cornerIndex < 4; cornerIndex++)
            {
                room[cornerIndex] = AlignConnectorPointToSourceBoundary(
                    room[cornerIndex],
                    roomRenderer,
                    profile.Across,
                    profile.Entrance.Outward);
                corridor[cornerIndex] = AlignConnectorPointToSourceBoundary(
                    corridor[cornerIndex],
                    corridorRenderer,
                    corridorAcross,
                    corridorOutward);
            }

            return new ConnectorSolidCorners(
                room[0],
                room[1],
                room[2],
                room[3],
                corridor[0],
                corridor[1],
                corridor[2],
                corridor[3]);
        }

        private static Vector3 AlignConnectorPointToSourceBoundary(
            Vector3 point,
            Renderer renderer,
            Vector3 across,
            Vector3 outward)
        {
            const float hiddenContactOverlap = 0.20f;
            var acrossProjection = Vector3.Dot(point, across);
            var boundaryProjection = GetRendererBoundaryProjectionAtAcross(
                renderer,
                across,
                outward,
                acrossProjection,
                out var resolvedAcrossProjection);
            return point +
                (outward *
                 ((boundaryProjection - hiddenContactOverlap) -
                  Vector3.Dot(point, outward))) +
                (across * (resolvedAcrossProjection - acrossProjection));
        }

        private static Vector3[] GetConnectorSolidRoomCorners(
            ConnectorSolidCorners corners)
        {
            return new[]
            {
                corners.RoomFirst,
                corners.RoomSecond,
                corners.RoomThird,
                corners.RoomFourth
            };
        }

        private static Vector3[] GetConnectorSolidCorridorCorners(
            ConnectorSolidCorners corners)
        {
            return new[]
            {
                corners.CorridorFirst,
                corners.CorridorSecond,
                corners.CorridorThird,
                corners.CorridorFourth
            };
        }

        private static ConnectorSolidCorners ExpandConnectorHorizontalSolidUnderWalls(
            ConnectorSolidCorners corners,
            bool ceiling)
        {
            Vector3 AtHeight(Vector3 horizontalSource, Vector3 heightSource)
            {
                return new Vector3(
                    horizontalSource.x,
                    heightSource.y,
                    horizontalSource.z);
            }

            if (!ceiling)
            {
                return new ConnectorSolidCorners(
                    corners.RoomFirst,
                    corners.RoomSecond,
                    AtHeight(corners.RoomSecond, corners.RoomThird),
                    AtHeight(corners.RoomFirst, corners.RoomFourth),
                    corners.CorridorFirst,
                    corners.CorridorSecond,
                    AtHeight(corners.CorridorSecond, corners.CorridorThird),
                    AtHeight(corners.CorridorFirst, corners.CorridorFourth));
            }

            return new ConnectorSolidCorners(
                AtHeight(corners.RoomFourth, corners.RoomFirst),
                AtHeight(corners.RoomThird, corners.RoomSecond),
                corners.RoomThird,
                corners.RoomFourth,
                AtHeight(corners.CorridorFourth, corners.CorridorFirst),
                AtHeight(corners.CorridorThird, corners.CorridorSecond),
                corners.CorridorThird,
                corners.CorridorFourth);
        }

        private static void AppendConnectorSolidToUnifiedMesh(
            ConnectorSolidCorners sourceCorners,
            int solidIndex,
            bool sampleCurvedRoomBoundary,
            Renderer roomRenderer,
            ConnectorEntranceProfile profile,
            float hiddenContactEmbed,
            int segmentCount,
            Vector3 roomDerivative,
            Vector3 corridorDerivative,
            bool useConvexTravelLoft,
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            ICollection<int> triangles)
        {
            const int crossSegmentCount = 12;
            var sourceRoom = GetConnectorSolidRoomCorners(sourceCorners);
            var sourceCorridor = GetConnectorSolidCorridorCorners(sourceCorners);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var startT = segmentIndex / (float)segmentCount;
                var endT = (segmentIndex + 1) / (float)segmentCount;
                for (var edgeIndex = 0; edgeIndex < 4; edgeIndex++)
                {
                    var next = (edgeIndex + 1) % 4;
                    for (var crossIndex = 0;
                         crossIndex < crossSegmentCount;
                         crossIndex++)
                    {
                        var firstCross = crossIndex / (float)crossSegmentCount;
                        var secondCross = (crossIndex + 1f) / crossSegmentCount;
                        var firstRoom = EvaluateConnectorRoomBoundaryPoint(
                            sourceRoom,
                            solidIndex,
                            sampleCurvedRoomBoundary,
                            edgeIndex,
                            firstCross,
                            roomRenderer,
                            profile,
                            hiddenContactEmbed);
                        var firstCorridor = Vector3.Lerp(
                            sourceCorridor[edgeIndex],
                            sourceCorridor[next],
                            firstCross);
                        var secondRoom = EvaluateConnectorRoomBoundaryPoint(
                            sourceRoom,
                            solidIndex,
                            sampleCurvedRoomBoundary,
                            edgeIndex,
                            secondCross,
                            roomRenderer,
                            profile,
                            hiddenContactEmbed);
                        var secondCorridor = Vector3.Lerp(
                            sourceCorridor[edgeIndex],
                            sourceCorridor[next],
                            secondCross);
                        AddConnectorCurveStrip(
                            firstRoom,
                            firstCorridor,
                            secondRoom,
                            secondCorridor,
                            startT,
                            endT,
                            roomDerivative,
                            corridorDerivative,
                            useConvexTravelLoft,
                            vertices,
                            uv,
                            triangles);
                    }
                }
            }

            for (var crossIndex = 0;
                 crossIndex < crossSegmentCount;
                 crossIndex++)
            {
                var firstCross = crossIndex / (float)crossSegmentCount;
                var secondCross = (crossIndex + 1f) / crossSegmentCount;
                var roomOuterFirst = EvaluateConnectorRoomBoundaryPoint(
                    sourceRoom, solidIndex, sampleCurvedRoomBoundary, 0,
                    firstCross, roomRenderer, profile,
                    hiddenContactEmbed);
                var roomOuterSecond = EvaluateConnectorRoomBoundaryPoint(
                    sourceRoom, solidIndex, sampleCurvedRoomBoundary, 0,
                    secondCross, roomRenderer, profile,
                    hiddenContactEmbed);
                var roomInnerSecond = EvaluateConnectorRoomBoundaryPoint(
                    sourceRoom, solidIndex, sampleCurvedRoomBoundary, 2,
                    1f - secondCross, roomRenderer, profile,
                    hiddenContactEmbed);
                var roomInnerFirst = EvaluateConnectorRoomBoundaryPoint(
                    sourceRoom, solidIndex, sampleCurvedRoomBoundary, 2,
                    1f - firstCross, roomRenderer, profile,
                    hiddenContactEmbed);
                AddConnectorLoftDoubleSidedQuad(
                    vertices, uv, triangles,
                    roomOuterFirst, roomOuterSecond,
                    roomInnerSecond, roomInnerFirst,
                    firstCross, secondCross);

                var corridorOuterFirst = Vector3.Lerp(
                    sourceCorridor[0], sourceCorridor[1], firstCross);
                var corridorOuterSecond = Vector3.Lerp(
                    sourceCorridor[0], sourceCorridor[1], secondCross);
                var corridorInnerSecond = Vector3.Lerp(
                    sourceCorridor[3], sourceCorridor[2], secondCross);
                var corridorInnerFirst = Vector3.Lerp(
                    sourceCorridor[3], sourceCorridor[2], firstCross);
                AddConnectorLoftDoubleSidedQuad(
                    vertices, uv, triangles,
                    corridorInnerFirst, corridorInnerSecond,
                    corridorOuterSecond, corridorOuterFirst,
                    firstCross, secondCross);
            }
        }

        private static Vector3 EvaluateConnectorRoomBoundaryPoint(
            IReadOnlyList<Vector3> roomCorners,
            int solidIndex,
            bool sampleCurvedRoomBoundary,
            int edgeIndex,
            float cross,
            Renderer roomRenderer,
            ConnectorEntranceProfile profile,
            float hiddenContactEmbed)
        {
            var next = (edgeIndex + 1) % 4;
            if (cross <= 0.000001f)
            {
                return roomCorners[edgeIndex];
            }

            if (cross >= 0.999999f)
            {
                return roomCorners[next];
            }

            // Only the wide floor/ceiling edges need curved source sampling.
            // Thickness and wall edges are shared with an adjacent solid; keep
            // those edges identical so no sub-pixel exterior slit can form.
            var samplesCurvedHorizontalBoundary =
                sampleCurvedRoomBoundary &&
                (solidIndex == 0 || solidIndex == 2) &&
                (edgeIndex == 0 || edgeIndex == 2);
            if (!samplesCurvedHorizontalBoundary)
            {
                return Vector3.Lerp(
                    roomCorners[edgeIndex],
                    roomCorners[next],
                    cross);
            }

            return MoveConnectorPointToRoomBoundary(
                Vector3.Lerp(roomCorners[edgeIndex], roomCorners[next], cross),
                roomRenderer,
                profile,
                hiddenContactEmbed);
        }

        private static void AppendConnectorCornerSeams(
            ConnectorSolidCorners[] solids,
            int segmentCount,
            Vector3 roomDerivative,
            Vector3 corridorDerivative,
            bool useConvexTravelLoft,
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            IReadOnlyList<List<int>> submeshTriangles)
        {
            var seamDefinitions = new[]
            {
                new[] { 0, 3, 3, 1, 0 },
                new[] { 0, 2, 1, 0, 0 },
                new[] { 3, 2, 2, 0, 3 },
                new[] { 1, 3, 2, 1, 1 },
                new[] { 0, 0, 3, 0, 3 },
                new[] { 0, 1, 1, 1, 1 },
                new[] { 3, 3, 2, 3, 2 },
                new[] { 1, 2, 2, 2, 2 }
            };

            foreach (var seam in seamDefinitions)
            {
                var firstRoom = GetConnectorSolidRoomCorners(solids[seam[0]])[seam[1]];
                var firstCorridor = GetConnectorSolidCorridorCorners(solids[seam[0]])[seam[1]];
                var secondRoom = GetConnectorSolidRoomCorners(solids[seam[2]])[seam[3]];
                var secondCorridor = GetConnectorSolidCorridorCorners(solids[seam[2]])[seam[3]];
                for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                {
                    var startT = segmentIndex / (float)segmentCount;
                    var endT = (segmentIndex + 1) / (float)segmentCount;
                    AddConnectorCurveStrip(
                        firstRoom,
                        firstCorridor,
                        secondRoom,
                        secondCorridor,
                        startT,
                        endT,
                        roomDerivative,
                        corridorDerivative,
                        false,
                        vertices,
                        uv,
                        submeshTriangles[seam[4]]);
                }
            }
        }

        private static void AppendConnectorEndCornerPatches(
            ConnectorSolidCorners[] solids,
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            IReadOnlyList<List<int>> submeshTriangles)
        {
            var room = new Vector3[solids.Length][];
            var corridor = new Vector3[solids.Length][];
            for (var solidIndex = 0; solidIndex < solids.Length; solidIndex++)
            {
                room[solidIndex] = GetConnectorSolidRoomCorners(solids[solidIndex]);
                corridor[solidIndex] = GetConnectorSolidCorridorCorners(solids[solidIndex]);
            }

            AddConnectorEndCornerPatch(
                room[0][3], room[0][0], room[3][0], room[3][1],
                corridor[0][3], corridor[0][0], corridor[3][0], corridor[3][1],
                vertices, uv, submeshTriangles[3]);
            AddConnectorEndCornerPatch(
                room[0][2], room[0][1], room[1][1], room[1][0],
                corridor[0][2], corridor[0][1], corridor[1][1], corridor[1][0],
                vertices, uv, submeshTriangles[1]);
            AddConnectorEndCornerPatch(
                room[3][2], room[3][3], room[2][3], room[2][0],
                corridor[3][2], corridor[3][3], corridor[2][3], corridor[2][0],
                vertices, uv, submeshTriangles[3]);
            AddConnectorEndCornerPatch(
                room[1][3], room[1][2], room[2][2], room[2][1],
                corridor[1][3], corridor[1][2], corridor[2][2], corridor[2][1],
                vertices, uv, submeshTriangles[1]);
        }

        private static void AddConnectorEndCornerPatch(
            Vector3 roomFirst,
            Vector3 roomSecond,
            Vector3 roomThird,
            Vector3 roomFourth,
            Vector3 corridorFirst,
            Vector3 corridorSecond,
            Vector3 corridorThird,
            Vector3 corridorFourth,
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            ICollection<int> triangles)
        {
            AddConnectorLoftDoubleSidedQuad(
                vertices, uv, triangles,
                roomFirst, roomSecond, roomThird, roomFourth,
                0f, 1f);
            AddConnectorLoftDoubleSidedQuad(
                vertices, uv, triangles,
                corridorFourth, corridorThird, corridorSecond, corridorFirst,
                0f, 1f);
        }

        private static void AddConnectorCurveStrip(
            Vector3 firstRoom,
            Vector3 firstCorridor,
            Vector3 secondRoom,
            Vector3 secondCorridor,
            float startT,
            float endT,
            Vector3 roomDerivative,
            Vector3 corridorDerivative,
            bool useConvexTravelLoft,
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            ICollection<int> triangles)
        {
            var firstStart = useConvexTravelLoft
                ? Vector3.Lerp(firstRoom, firstCorridor, startT)
                : EvaluateConnectorHermite(
                    firstRoom, firstCorridor,
                    roomDerivative, corridorDerivative, startT);
            var secondStart = useConvexTravelLoft
                ? Vector3.Lerp(secondRoom, secondCorridor, startT)
                : EvaluateConnectorHermite(
                    secondRoom, secondCorridor,
                    roomDerivative, corridorDerivative, startT);
            var secondEnd = useConvexTravelLoft
                ? Vector3.Lerp(secondRoom, secondCorridor, endT)
                : EvaluateConnectorHermite(
                    secondRoom, secondCorridor,
                    roomDerivative, corridorDerivative, endT);
            var firstEnd = useConvexTravelLoft
                ? Vector3.Lerp(firstRoom, firstCorridor, endT)
                : EvaluateConnectorHermite(
                    firstRoom, firstCorridor,
                    roomDerivative, corridorDerivative, endT);
            AddConnectorLoftDoubleSidedQuad(
                vertices,
                uv,
                triangles,
                firstStart,
                secondStart,
                secondEnd,
                firstEnd,
                startT,
                endT);
        }

        private static void AppendRoomBoundaryCornerWedges(
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            IReadOnlyList<List<int>> submeshTriangles)
        {
            AppendRoomBoundaryCornerWedge(
                profile,
                profile.Floor,
                profile.MinimumWall,
                shell.RoomFloorMinimum,
                shell.RoomFloorMinimum.y,
                shell.RoomOuterFloorMinimum.y,
                vertices,
                uv,
                submeshTriangles[0]);
            AppendRoomBoundaryCornerWedge(
                profile,
                profile.Floor,
                profile.MaximumWall,
                shell.RoomFloorMaximum,
                shell.RoomFloorMaximum.y,
                shell.RoomOuterFloorMaximum.y,
                vertices,
                uv,
                submeshTriangles[0]);
            AppendRoomBoundaryCornerWedge(
                profile,
                profile.Ceiling,
                profile.MinimumWall,
                shell.RoomCeilingMinimum,
                shell.RoomCeilingMinimum.y,
                shell.RoomOuterCeilingMinimum.y,
                vertices,
                uv,
                submeshTriangles[2]);
            AppendRoomBoundaryCornerWedge(
                profile,
                profile.Ceiling,
                profile.MaximumWall,
                shell.RoomCeilingMaximum,
                shell.RoomCeilingMaximum.y,
                shell.RoomOuterCeilingMaximum.y,
                vertices,
                uv,
                submeshTriangles[2]);
        }

        private static void AppendRoomBoundaryCornerWedge(
            ConnectorEntranceProfile profile,
            Renderer horizontalRenderer,
            Renderer sideWallRenderer,
            Vector3 connectorCorner,
            float surfaceY,
            float outerY,
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            ICollection<int> triangles)
        {
            const float hiddenContactOverlap = 0.20f;
            var targetAcrossProjection = Vector3.Dot(
                connectorCorner,
                profile.Across);
            var connectorProjection = Vector3.Dot(
                connectorCorner,
                profile.Entrance.Outward);
            var horizontalProjection = Mathf.Min(
                connectorProjection,
                GetRendererBoundaryProjectionAtAcross(
                    horizontalRenderer,
                    profile.Across,
                    profile.Entrance.Outward,
                    targetAcrossProjection,
                    out var resolvedHorizontalAcross)) - hiddenContactOverlap;
            var wallProjection = Mathf.Min(
                connectorProjection,
                GetRendererBoundaryProjectionAtAcross(
                    sideWallRenderer,
                    profile.Across,
                    profile.Entrance.Outward,
                    targetAcrossProjection,
                    out var resolvedWallAcross)) - hiddenContactOverlap;
            var horizontalPoint = connectorCorner +
                (profile.Entrance.Outward *
                 (horizontalProjection - connectorProjection)) +
                (profile.Across *
                 (resolvedHorizontalAcross - targetAcrossProjection));
            var wallPoint = connectorCorner +
                (profile.Entrance.Outward *
                 (wallProjection - connectorProjection)) +
                (profile.Across *
                 (resolvedWallAcross - targetAcrossProjection));
            connectorCorner.y = surfaceY;
            horizontalPoint.y = surfaceY;
            wallPoint.y = surfaceY;
            var outerConnector = connectorCorner;
            var outerHorizontal = horizontalPoint;
            var outerWall = wallPoint;
            outerConnector.y = outerY;
            outerHorizontal.y = outerY;
            outerWall.y = outerY;

            AddDoubleSidedConnectorTriangle(
                vertices,
                triangles,
                uv,
                connectorCorner,
                horizontalPoint,
                wallPoint);
            AddDoubleSidedConnectorTriangle(
                vertices,
                triangles,
                uv,
                outerConnector,
                outerWall,
                outerHorizontal);
            AddConnectorLoftDoubleSidedQuad(
                vertices,
                uv,
                triangles,
                connectorCorner,
                outerConnector,
                outerHorizontal,
                horizontalPoint,
                0f,
                1f);
            AddConnectorLoftDoubleSidedQuad(
                vertices,
                uv,
                triangles,
                horizontalPoint,
                outerHorizontal,
                outerWall,
                wallPoint,
                0f,
                1f);
            AddConnectorLoftDoubleSidedQuad(
                vertices,
                uv,
                triangles,
                wallPoint,
                outerWall,
                outerConnector,
                connectorCorner,
                0f,
                1f);
        }

        private static Vector3 EvaluateConnectorHermite(
            Vector3 start,
            Vector3 end,
            Vector3 startDerivative,
            Vector3 endDerivative,
            float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return ((2f * t3 - 3f * t2 + 1f) * start) +
                ((t3 - 2f * t2 + t) * startDerivative) +
                ((-2f * t3 + 3f * t2) * end) +
                ((t3 - t2) * endDerivative);
        }

        private static void AddConnectorLoftDoubleSidedQuad(
            ICollection<Vector3> vertices,
            ICollection<Vector2> uv,
            ICollection<int> triangles,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            float startU,
            float endU)
        {
            var baseIndex = vertices.Count;
            vertices.Add(first);
            vertices.Add(second);
            vertices.Add(third);
            vertices.Add(fourth);
            uv.Add(new Vector2(startU, 0f));
            uv.Add(new Vector2(startU, 1f));
            uv.Add(new Vector2(endU, 1f));
            uv.Add(new Vector2(endU, 0f));
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);

            baseIndex = vertices.Count;
            vertices.Add(first);
            vertices.Add(fourth);
            vertices.Add(third);
            vertices.Add(second);
            uv.Add(new Vector2(startU, 0f));
            uv.Add(new Vector2(endU, 0f));
            uv.Add(new Vector2(endU, 1f));
            uv.Add(new Vector2(startU, 1f));
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }

        private static ConnectorSolidCorners AlignConnectorGapSolidCorridorEndToSources(
            ConnectorSolidCorners corners,
            string partName,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            Renderer roomRenderer,
            Renderer corridorRenderer)
        {
            var aligned = AlignConnectorGapSolidToSources(
                corners,
                partName,
                definition,
                module,
                profile,
                roomRenderer,
                corridorRenderer);
            return new ConnectorSolidCorners(
                corners.RoomFirst,
                corners.RoomSecond,
                corners.RoomThird,
                corners.RoomFourth,
                aligned.CorridorFirst,
                aligned.CorridorSecond,
                aligned.CorridorThird,
                aligned.CorridorFourth);
        }

        private static void CreateControlRoomH02AdaptiveCollar(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            float minimumOuterMargin,
            float maximumOuterMargin,
            float verticalOuterMargin,
            Material floorMaterial,
            Material minimumWallMaterial,
            Material maximumWallMaterial,
            Material ceilingMaterial)
        {
            var sourcePlane = profile.PlaneCenter;
            sourcePlane.y = 0f;
            var sourcePlaneAcross = Vector3.Dot(sourcePlane, profile.Across);
            Vector3 SourcePoint(float acrossProjection, float y)
            {
                var point = sourcePlane +
                    (profile.Across * (acrossProjection - sourcePlaneAcross));
                point.y = y;
                return point;
            }

            var sourceOuterFloorMinimum = SourcePoint(
                profile.MinimumWallMinimum - minimumOuterMargin,
                profile.Floor.bounds.min.y - verticalOuterMargin);
            var sourceOuterFloorMaximum = SourcePoint(
                profile.MaximumWallMaximum + maximumOuterMargin,
                profile.Floor.bounds.min.y - verticalOuterMargin);
            var sourceFloorMinimum = SourcePoint(
                profile.MinimumWallMaximum,
                profile.Entrance.FloorY);
            var sourceFloorMaximum = SourcePoint(
                profile.MaximumWallMinimum,
                profile.Entrance.FloorY);
            var sourceCeilingMinimum = SourcePoint(
                profile.MinimumWallMaximum,
                profile.Ceiling.bounds.min.y);
            var sourceCeilingMaximum = SourcePoint(
                profile.MaximumWallMinimum,
                profile.Ceiling.bounds.min.y);
            var sourceOuterCeilingMinimum = SourcePoint(
                profile.MinimumWallMinimum - minimumOuterMargin,
                profile.Ceiling.bounds.max.y + verticalOuterMargin);
            var sourceOuterCeilingMaximum = SourcePoint(
                profile.MaximumWallMaximum + maximumOuterMargin,
                profile.Ceiling.bounds.max.y + verticalOuterMargin);

            CreateConnectorSolidPart(
                connector,
                definition,
                "StubEnclosureFloor",
                new ConnectorSolidCorners(
                    sourceOuterFloorMinimum,
                    sourceOuterFloorMaximum,
                    sourceFloorMaximum,
                    sourceFloorMinimum,
                    shell.RoomOuterFloorMinimum,
                    shell.RoomOuterFloorMaximum,
                    shell.RoomFloorMaximum,
                    shell.RoomFloorMinimum),
                floorMaterial);
            CreateConnectorSolidPart(
                connector,
                definition,
                "StubEnclosureLeftWall",
                new ConnectorSolidCorners(
                    sourceOuterFloorMinimum,
                    sourceFloorMinimum,
                    sourceCeilingMinimum,
                    sourceOuterCeilingMinimum,
                    shell.RoomOuterFloorMinimum,
                    shell.RoomFloorMinimum,
                    shell.RoomCeilingMinimum,
                    shell.RoomOuterCeilingMinimum),
                minimumWallMaterial);
            CreateConnectorSolidPart(
                connector,
                definition,
                "StubEnclosureRightWall",
                new ConnectorSolidCorners(
                    sourceFloorMaximum,
                    sourceOuterFloorMaximum,
                    sourceOuterCeilingMaximum,
                    sourceCeilingMaximum,
                    shell.RoomFloorMaximum,
                    shell.RoomOuterFloorMaximum,
                    shell.RoomOuterCeilingMaximum,
                    shell.RoomCeilingMaximum),
                maximumWallMaterial);
            CreateConnectorSolidPart(
                connector,
                definition,
                "StubEnclosureCeiling",
                new ConnectorSolidCorners(
                    sourceCeilingMinimum,
                    sourceCeilingMaximum,
                    sourceOuterCeilingMaximum,
                    sourceOuterCeilingMinimum,
                    shell.RoomCeilingMinimum,
                    shell.RoomCeilingMaximum,
                    shell.RoomOuterCeilingMaximum,
                    shell.RoomOuterCeilingMinimum),
                ceilingMaterial);
        }

        private static void CreateRemainingEndpointGapAprons(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material floorMaterial,
            Material minimumWallMaterial)
        {
            if (string.Equals(
                    definition.Id,
                    "EngineRoom_H01",
                    StringComparison.Ordinal))
            {
                var extension = profile.Entrance.Outward * 0.95f;
                CreateConnectorSolidPart(
                    connector,
                    definition,
                    "RoomMinimumSideApron",
                    new ConnectorSolidCorners(
                        shell.RoomOuterFloorMinimum,
                        shell.RoomFloorMinimum,
                        shell.RoomCeilingMinimum,
                        shell.RoomOuterCeilingMinimum,
                        shell.RoomOuterFloorMinimum + extension,
                        shell.RoomFloorMinimum + extension,
                        shell.RoomCeilingMinimum + extension,
                        shell.RoomOuterCeilingMinimum + extension),
                    minimumWallMaterial);
                return;
            }

            if (string.Equals(
                    definition.Id,
                    "Cockpit_H02",
                    StringComparison.Ordinal))
            {
                var extension = profile.Entrance.Outward * 0.70f;
                CreateConnectorSolidPart(
                    connector,
                    definition,
                    "CorridorFloorEndApron",
                    new ConnectorSolidCorners(
                        shell.CorridorOuterFloorMinimum,
                        shell.CorridorOuterFloorMaximum,
                        shell.CorridorFloorMaximum,
                        shell.CorridorFloorMinimum,
                        shell.CorridorOuterFloorMinimum + extension,
                        shell.CorridorOuterFloorMaximum + extension,
                        shell.CorridorFloorMaximum + extension,
                        shell.CorridorFloorMinimum + extension),
                    floorMaterial);
            }
        }

        private static ConnectorSolidCorners AlignConnectorGapSolidToSources(
            ConnectorSolidCorners corners,
            string partName,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            Renderer roomRenderer,
            Renderer corridorRenderer)
        {
            var roomTargetProjection = GetRendererProjectionMaximum(
                roomRenderer,
                profile.Entrance.Outward);
            GetConnectorOuterCornerSelection(
                partName,
                out var firstIsOuter,
                out var secondIsOuter,
                out var thirdIsOuter,
                out var fourthIsOuter);
            var outerCornerCount = 0;
            var roomCurrentProjection = 0f;
            var corridorCurrentX = 0f;
            var roomCorners = new[]
            {
                corners.RoomFirst,
                corners.RoomSecond,
                corners.RoomThird,
                corners.RoomFourth
            };
            var corridorCorners = new[]
            {
                corners.CorridorFirst,
                corners.CorridorSecond,
                corners.CorridorThird,
                corners.CorridorFourth
            };
            var outerSelection = new[]
            {
                firstIsOuter,
                secondIsOuter,
                thirdIsOuter,
                fourthIsOuter
            };
            for (var cornerIndex = 0; cornerIndex < outerSelection.Length; cornerIndex++)
            {
                if (!outerSelection[cornerIndex])
                {
                    continue;
                }

                roomCurrentProjection += Vector3.Dot(
                    roomCorners[cornerIndex],
                    profile.Entrance.Outward);
                corridorCurrentX += module.InverseTransformPoint(
                    corridorCorners[cornerIndex]).x;
                outerCornerCount++;
            }

            roomCurrentProjection /= outerCornerCount;
            corridorCurrentX /= outerCornerCount;
            var roomOffset = profile.Entrance.Outward *
                (roomTargetProjection - roomCurrentProjection);

            var corridorBounds = CalculateVisibleBoundsInLocalSpace(
                corridorRenderer.gameObject,
                module);
            var corridorTargetX = definition.LowEnd
                ? corridorBounds.min.x
                : corridorBounds.max.x;
            var corridorOffset = module.TransformPoint(new Vector3(
                corridorTargetX,
                0f,
                0f)) - module.TransformPoint(new Vector3(
                corridorCurrentX,
                0f,
                0f));
            for (var cornerIndex = 0; cornerIndex < outerSelection.Length; cornerIndex++)
            {
                if (!outerSelection[cornerIndex])
                {
                    continue;
                }

                if (!string.Equals(partName, "Floor", StringComparison.Ordinal))
                {
                    roomCorners[cornerIndex] += roomOffset;
                }
                corridorCorners[cornerIndex] += corridorOffset;
            }

            return new ConnectorSolidCorners(
                roomCorners[0],
                roomCorners[1],
                roomCorners[2],
                roomCorners[3],
                corridorCorners[0],
                corridorCorners[1],
                corridorCorners[2],
                corridorCorners[3]);
        }

        private static void GetConnectorOuterCornerSelection(
            string partName,
            out bool first,
            out bool second,
            out bool third,
            out bool fourth)
        {
            first = false;
            second = false;
            third = false;
            fourth = false;
            switch (partName)
            {
                case "Floor":
                    first = true;
                    second = true;
                    return;
                case "LeftWall":
                    first = true;
                    fourth = true;
                    return;
                case "RightWall":
                    second = true;
                    third = true;
                    return;
                case "Ceiling":
                    third = true;
                    fourth = true;
                    return;
                default:
                    throw new InvalidOperationException(
                        "Unsupported connector solid " + partName);
            }
        }

        private static ConnectorSolidCorners GetConnectorGapSolid(
            ConnectorGapShell shell,
            string partName)
        {
            switch (partName)
            {
                case "Floor":
                    return new ConnectorSolidCorners(
                        shell.RoomOuterFloorMinimum,
                        shell.RoomOuterFloorMaximum,
                        shell.RoomFloorMaximum,
                        shell.RoomFloorMinimum,
                        shell.CorridorOuterFloorMinimum,
                        shell.CorridorOuterFloorMaximum,
                        shell.CorridorFloorMaximum,
                        shell.CorridorFloorMinimum);
                case "LeftWall":
                    return new ConnectorSolidCorners(
                        shell.RoomOuterFloorMinimum,
                        shell.RoomFloorMinimum,
                        shell.RoomCeilingMinimum,
                        shell.RoomOuterCeilingMinimum,
                        shell.CorridorOuterFloorMinimum,
                        shell.CorridorFloorMinimum,
                        shell.CorridorCeilingMinimum,
                        shell.CorridorOuterCeilingMinimum);
                case "RightWall":
                    return new ConnectorSolidCorners(
                        shell.RoomFloorMaximum,
                        shell.RoomOuterFloorMaximum,
                        shell.RoomOuterCeilingMaximum,
                        shell.RoomCeilingMaximum,
                        shell.CorridorFloorMaximum,
                        shell.CorridorOuterFloorMaximum,
                        shell.CorridorOuterCeilingMaximum,
                        shell.CorridorCeilingMaximum);
                case "Ceiling":
                    return new ConnectorSolidCorners(
                        shell.RoomCeilingMinimum,
                        shell.RoomCeilingMaximum,
                        shell.RoomOuterCeilingMaximum,
                        shell.RoomOuterCeilingMinimum,
                        shell.CorridorCeilingMinimum,
                        shell.CorridorCeilingMaximum,
                        shell.CorridorOuterCeilingMaximum,
                        shell.CorridorOuterCeilingMinimum);
                default:
                    throw new InvalidOperationException(
                        "Unsupported connector solid " + partName);
            }
        }

        private static void CreateConnectorSolidPart(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorSolidCorners corners,
            Material material)
        {
            if (material == null)
            {
                throw new InvalidOperationException(
                    definition.Id + " " + partName + " must reuse its source material.");
            }

            var mesh = CreateSubdividedDoubleSidedConnectorSolidMesh(
                "Mesh_" + definition.Id + "_" + partName,
                corners);
            var meshPath = TriRoomConnectorSampleMeshDirectory + "/" +
                mesh.name + ".asset";
            AssetDatabase.CreateAsset(mesh, meshPath);
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static Mesh CreateSubdividedDoubleSidedConnectorSolidMesh(
            string meshName,
            ConnectorSolidCorners corners)
        {
            var room = new[]
            {
                corners.RoomFirst,
                corners.RoomSecond,
                corners.RoomThird,
                corners.RoomFourth
            };
            var corridor = new[]
            {
                corners.CorridorFirst,
                corners.CorridorSecond,
                corners.CorridorThird,
                corners.CorridorFourth
            };
            var maximumLength = 0f;
            for (var cornerIndex = 0; cornerIndex < 4; cornerIndex++)
            {
                maximumLength = Mathf.Max(
                    maximumLength,
                    Vector3.Distance(room[cornerIndex], corridor[cornerIndex]));
            }

            var segmentCount = Mathf.Clamp(
                Mathf.CeilToInt(maximumLength / 0.5f),
                2,
                48);
            var vertices = new List<Vector3>(32 + (segmentCount * 64));
            var triangles = new List<int>(24 + (segmentCount * 48));
            var uv = new List<Vector2>(vertices.Capacity);
            AddDoubleSidedConnectorBoundaryPatch(
                vertices,
                triangles,
                uv,
                room[0],
                room[1],
                room[2],
                room[3],
                12,
                8);
            AddDoubleSidedConnectorBoundaryPatch(
                vertices,
                triangles,
                uv,
                corridor[3],
                corridor[2],
                corridor[1],
                corridor[0],
                12,
                8);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var startT = segmentIndex / (float)segmentCount;
                var endT = (segmentIndex + 1) / (float)segmentCount;
                for (var edgeIndex = 0; edgeIndex < 4; edgeIndex++)
                {
                    var next = (edgeIndex + 1) % 4;
                    AddDoubleSidedConnectorQuad(
                        vertices,
                        triangles,
                        uv,
                        Vector3.Lerp(room[edgeIndex], corridor[edgeIndex], startT),
                        Vector3.Lerp(room[edgeIndex], corridor[edgeIndex], endT),
                        Vector3.Lerp(room[next], corridor[next], endT),
                        Vector3.Lerp(room[next], corridor[next], startT));
                }
            }

            var mesh = new Mesh { name = meshName };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateConnectorCornerGapFillers(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorSolidCorners floor,
            ConnectorSolidCorners leftWall,
            ConnectorSolidCorners rightWall,
            ConnectorSolidCorners ceiling,
            Material floorMaterial,
            Material ceilingMaterial)
        {
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "FloorLeftInnerGapFill",
                floor.RoomFourth,
                leftWall.RoomSecond,
                leftWall.CorridorSecond,
                floor.CorridorFourth,
                floorMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "FloorLeftOuterGapFill",
                floor.RoomFirst,
                leftWall.RoomFirst,
                leftWall.CorridorFirst,
                floor.CorridorFirst,
                floorMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "FloorRightInnerGapFill",
                floor.RoomThird,
                rightWall.RoomFirst,
                rightWall.CorridorFirst,
                floor.CorridorThird,
                floorMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "FloorRightOuterGapFill",
                floor.RoomSecond,
                rightWall.RoomSecond,
                rightWall.CorridorSecond,
                floor.CorridorSecond,
                floorMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "CeilingLeftInnerGapFill",
                ceiling.RoomFirst,
                leftWall.RoomThird,
                leftWall.CorridorThird,
                ceiling.CorridorFirst,
                ceilingMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "CeilingLeftOuterGapFill",
                ceiling.RoomFourth,
                leftWall.RoomFourth,
                leftWall.CorridorFourth,
                ceiling.CorridorFourth,
                ceilingMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "CeilingRightInnerGapFill",
                ceiling.RoomSecond,
                rightWall.RoomFourth,
                rightWall.CorridorFourth,
                ceiling.CorridorSecond,
                ceilingMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "CeilingRightOuterGapFill",
                ceiling.RoomThird,
                rightWall.RoomThird,
                rightWall.CorridorThird,
                ceiling.CorridorThird,
                ceilingMaterial);

            CreateConnectorEndGapFillers(
                connector,
                definition,
                "RoomEnd",
                floor,
                leftWall,
                rightWall,
                ceiling,
                true,
                floorMaterial,
                ceilingMaterial);
            CreateConnectorEndGapFillers(
                connector,
                definition,
                "CorridorEnd",
                floor,
                leftWall,
                rightWall,
                ceiling,
                false,
                floorMaterial,
                ceilingMaterial);
        }

        private static void CreateControlRoomH02PassageWallSeals(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorGapShell shell,
            Material minimumWallMaterial,
            Material maximumWallMaterial)
        {
            var left = GetConnectorGapSurface(shell, "LeftWall");
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "PassageLeftWallSeal",
                left.First,
                left.Second,
                left.Third,
                left.Fourth,
                minimumWallMaterial);
            var right = GetConnectorGapSurface(shell, "RightWall");
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "PassageRightWallSeal",
                right.First,
                right.Second,
                right.Third,
                right.Fourth,
                maximumWallMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "PassageLeftLowerWallSeal",
                shell.RoomOuterFloorMinimum,
                shell.CorridorOuterFloorMinimum,
                shell.CorridorCeilingMinimum,
                shell.RoomCeilingMinimum,
                minimumWallMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                "PassageRightLowerWallSeal",
                shell.RoomOuterFloorMaximum,
                shell.RoomCeilingMaximum,
                shell.CorridorCeilingMaximum,
                shell.CorridorOuterFloorMaximum,
                maximumWallMaterial);
        }

        private static void CreateControlRoomH02FloorTurnSeal(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorGapShell shell,
            Material material)
        {
            var top = new[]
            {
                new Vector3(
                    shell.RoomOuterFloorMinimum.x,
                    shell.RoomFloorMinimum.y,
                    shell.RoomOuterFloorMinimum.z),
                new Vector3(
                    shell.RoomOuterFloorMaximum.x,
                    shell.RoomFloorMaximum.y,
                    shell.RoomOuterFloorMaximum.z),
                new Vector3(
                    shell.CorridorOuterFloorMinimum.x,
                    shell.CorridorFloorMinimum.y,
                    shell.CorridorOuterFloorMinimum.z),
                new Vector3(
                    shell.CorridorOuterFloorMaximum.x,
                    shell.CorridorFloorMaximum.y,
                    shell.CorridorOuterFloorMaximum.z)
            };
            var bottom = new[]
            {
                shell.RoomOuterFloorMinimum,
                shell.RoomOuterFloorMaximum,
                shell.CorridorOuterFloorMinimum,
                shell.CorridorOuterFloorMaximum
            };
            var centerTop = Vector3.zero;
            var centerBottom = Vector3.zero;
            for (var pointIndex = 0; pointIndex < top.Length; pointIndex++)
            {
                centerTop += top[pointIndex];
                centerBottom += bottom[pointIndex];
            }

            centerTop /= top.Length;
            centerBottom /= bottom.Length;
            var order = new List<int> { 0, 1, 2, 3 };
            order.Sort((first, second) =>
            {
                var firstAngle = Mathf.Atan2(
                    top[first].z - centerTop.z,
                    top[first].x - centerTop.x);
                var secondAngle = Mathf.Atan2(
                    top[second].z - centerTop.z,
                    top[second].x - centerTop.x);
                return firstAngle.CompareTo(secondAngle);
            });
            var vertices = new List<Vector3>(80);
            var triangles = new List<int>(96);
            var uv = new List<Vector2>(80);
            for (var orderIndex = 0; orderIndex < order.Count; orderIndex++)
            {
                var current = order[orderIndex];
                var next = order[(orderIndex + 1) % order.Count];
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    centerTop,
                    top[current],
                    top[next]);
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    centerBottom,
                    bottom[next],
                    bottom[current]);
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    top[current],
                    bottom[current],
                    bottom[next],
                    top[next]);
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_FloorTurnSeal"
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject("FloorTurnSeal");
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        private static void CreateControlRoomH02RoomWallApproachFills(
            Transform connector,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material floorMaterial,
            Material minimumWallMaterial,
            Material maximumWallMaterial,
            Material ceilingMaterial)
        {
            CreateRoomWallApproachFill(
                connector,
                definition,
                "RoomLeftWallApproachFill",
                profile,
                profile.MinimumWall,
                new ConnectorSolidCorners(
                    shell.RoomOuterFloorMinimum,
                    shell.RoomFloorMinimum,
                    shell.RoomCeilingMinimum,
                    shell.RoomOuterCeilingMinimum,
                    shell.RoomOuterFloorMinimum,
                    shell.RoomFloorMinimum,
                    shell.RoomCeilingMinimum,
                    shell.RoomOuterCeilingMinimum),
                minimumWallMaterial,
                true,
                0.04f);
            CreateRoomWallApproachFill(
                connector,
                definition,
                "RoomRightWallApproachFill",
                profile,
                profile.MaximumWall,
                new ConnectorSolidCorners(
                    shell.RoomFloorMaximum,
                    shell.RoomOuterFloorMaximum,
                    shell.RoomOuterCeilingMaximum,
                    shell.RoomCeilingMaximum,
                    shell.RoomFloorMaximum,
                    shell.RoomOuterFloorMaximum,
                    shell.RoomOuterCeilingMaximum,
                    shell.RoomCeilingMaximum),
                maximumWallMaterial,
                true,
                0.04f);
            CreateRoomCeilingApproachFill(
                connector,
                definition,
                profile,
                shell,
                ceilingMaterial);
            CreateRoomFloorWallFootprintFills(
                connector,
                definition,
                profile,
                shell,
                floorMaterial);
            CreateControlRoomH02SideBoundaryMembranes(
                connector,
                definition,
                profile,
                shell,
                minimumWallMaterial,
                maximumWallMaterial);
            CreateRoomContactSnagGuards(
                connector,
                definition,
                module,
                profile,
                shell,
                minimumWallMaterial,
                maximumWallMaterial,
                "RoomLeftContactSnagGuard",
                "RoomRightContactSnagGuard",
                0.12f,
                0.22f);
        }

        private static void CreateRoomWallApproachFill(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorEntranceProfile profile,
            Renderer wall,
            ConnectorSolidCorners connectorEnd,
            Material material,
            bool useSharedSourceDepth = false,
            float sourceContactOverlap = 0f)
        {
            var firstConnectorProjection = Vector3.Dot(
                connectorEnd.RoomFirst,
                profile.Entrance.Outward);
            var secondConnectorProjection = Vector3.Dot(
                connectorEnd.RoomSecond,
                profile.Entrance.Outward);
            var firstAcrossProjection = Vector3.Dot(
                connectorEnd.RoomFirst,
                profile.Across);
            var secondAcrossProjection = Vector3.Dot(
                connectorEnd.RoomSecond,
                profile.Across);
            var firstSourceProjection = Mathf.Min(
                firstConnectorProjection,
                GetRendererBoundaryProjectionAtAcross(
                    wall,
                    profile.Across,
                    profile.Entrance.Outward,
                    firstAcrossProjection,
                    out var resolvedFirstAcross));
            var secondSourceProjection = Mathf.Min(
                secondConnectorProjection,
                GetRendererBoundaryProjectionAtAcross(
                    wall,
                    profile.Across,
                    profile.Entrance.Outward,
                    secondAcrossProjection,
                    out var resolvedSecondAcross));
            if (useSharedSourceDepth)
            {
                var sharedSourceProjection = Mathf.Min(
                    firstSourceProjection,
                    secondSourceProjection);
                firstSourceProjection = sharedSourceProjection;
                secondSourceProjection = sharedSourceProjection;
            }
            if ((firstConnectorProjection - firstSourceProjection) <= 0.002f &&
                (secondConnectorProjection - secondSourceProjection) <= 0.002f)
            {
                return;
            }

            firstSourceProjection -= sourceContactOverlap;
            secondSourceProjection -= sourceContactOverlap;

            var firstSourceOffset =
                (profile.Entrance.Outward *
                 (firstSourceProjection - firstConnectorProjection)) +
                (profile.Across *
                 (resolvedFirstAcross - firstAcrossProjection));
            var secondSourceOffset =
                (profile.Entrance.Outward *
                 (secondSourceProjection - secondConnectorProjection)) +
                (profile.Across *
                 (resolvedSecondAcross - secondAcrossProjection));
            var sourceFirst = connectorEnd.RoomFirst + firstSourceOffset;
            var sourceSecond = connectorEnd.RoomSecond + secondSourceOffset;
            var sourceThird = connectorEnd.RoomThird + secondSourceOffset;
            var sourceFourth = connectorEnd.RoomFourth + firstSourceOffset;
            sourceFirst.y = wall.bounds.min.y;
            sourceSecond.y = wall.bounds.min.y;
            sourceThird.y = wall.bounds.max.y;
            sourceFourth.y = wall.bounds.max.y;
            CreateConnectorSolidPart(
                connector,
                definition,
                partName,
                new ConnectorSolidCorners(
                    sourceFirst,
                    sourceSecond,
                    sourceThird,
                    sourceFourth,
                    connectorEnd.CorridorFirst,
                    connectorEnd.CorridorSecond,
                    connectorEnd.CorridorThird,
                    connectorEnd.CorridorFourth),
                material);
        }

        private static void CreateControlRoomH02CurvedInnerCornerSeals(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material floorMaterial,
            Material ceilingMaterial)
        {
            var roomAcross = HorizontalDirection(
                shell.RoomFloorMaximum - shell.RoomFloorMinimum);
            var corridorAcross = HorizontalDirection(
                shell.CorridorFloorMaximum - shell.CorridorFloorMinimum);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "CurvedFloorLeftCornerSeal",
                shell.RoomFloorMinimum,
                shell.CorridorFloorMinimum,
                roomAcross,
                corridorAcross,
                Vector3.up,
                0.32f,
                floorMaterial);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "CurvedFloorRightCornerSeal",
                shell.RoomFloorMaximum,
                shell.CorridorFloorMaximum,
                -roomAcross,
                -corridorAcross,
                Vector3.up,
                0.32f,
                floorMaterial);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "CurvedCeilingLeftCornerSeal",
                shell.RoomCeilingMinimum,
                shell.CorridorCeilingMinimum,
                roomAcross,
                corridorAcross,
                Vector3.down,
                0.14f,
                ceilingMaterial);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "CurvedCeilingRightCornerSeal",
                shell.RoomCeilingMaximum,
                shell.CorridorCeilingMaximum,
                -roomAcross,
                -corridorAcross,
                Vector3.down,
                0.14f,
                ceilingMaterial);
        }

        private static void CreateRoomSeamCurvedCornerSeals(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material floorMaterial,
            Material ceilingMaterial)
        {
            var sourceFloorMinimum = GetAdaptiveRoomBoundaryPoint(
                profile,
                profile.Floor,
                profile.MinimumWall,
                profile.MinimumWallMaximum,
                profile.Entrance.FloorY);
            var sourceFloorMaximum = GetAdaptiveRoomBoundaryPoint(
                profile,
                profile.Floor,
                profile.MaximumWall,
                profile.MaximumWallMinimum,
                profile.Entrance.FloorY);
            var sourceCeilingMinimum = GetAdaptiveRoomBoundaryPoint(
                profile,
                profile.Ceiling,
                profile.MinimumWall,
                profile.MinimumWallMaximum,
                profile.Ceiling.bounds.min.y);
            var sourceCeilingMaximum = GetAdaptiveRoomBoundaryPoint(
                profile,
                profile.Ceiling,
                profile.MaximumWall,
                profile.MaximumWallMinimum,
                profile.Ceiling.bounds.min.y);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "RoomSeamFloorLeftCurve",
                sourceFloorMinimum,
                shell.RoomFloorMinimum,
                profile.Across,
                profile.Across,
                Vector3.up,
                0.32f,
                floorMaterial);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "RoomSeamFloorRightCurve",
                sourceFloorMaximum,
                shell.RoomFloorMaximum,
                -profile.Across,
                -profile.Across,
                Vector3.up,
                0.32f,
                floorMaterial);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "RoomSeamCeilingLeftCurve",
                sourceCeilingMinimum,
                shell.RoomCeilingMinimum,
                profile.Across,
                profile.Across,
                Vector3.down,
                0.18f,
                ceilingMaterial);
            CreateConnectorCurvedInnerCornerSeal(
                connector,
                definition,
                "RoomSeamCeilingRightCurve",
                sourceCeilingMaximum,
                shell.RoomCeilingMaximum,
                -profile.Across,
                -profile.Across,
                Vector3.down,
                0.18f,
                ceilingMaterial);
        }

        private static void CreateConnectorCornerSectorPlug(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            Vector3 center,
            Vector3 horizontalOutward,
            Vector3 verticalInward,
            Material material)
        {
            const int segmentCount = 18;
            const float radius = 0.65f;
            var vertices = new List<Vector3>(segmentCount * 6);
            var triangles = new List<int>(segmentCount * 6);
            var uv = new List<Vector2>(vertices.Capacity);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var startAngle = (segmentIndex / (float)segmentCount) *
                    Mathf.PI * 0.5f;
                var endAngle = ((segmentIndex + 1) / (float)segmentCount) *
                    Mathf.PI * 0.5f;
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    center,
                    center +
                        (horizontalOutward * (Mathf.Cos(startAngle) * radius)) +
                        (verticalInward * (Mathf.Sin(startAngle) * radius)),
                    center +
                        (horizontalOutward * (Mathf.Cos(endAngle) * radius)) +
                        (verticalInward * (Mathf.Sin(endAngle) * radius)));
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        private static void CreateControlRoomH02SideBoundaryMembranes(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material minimumWallMaterial,
            Material maximumWallMaterial)
        {
            CreateRoomSideBoundaryMembrane(
                connector,
                definition,
                "RoomLeftSideBoundaryMembrane",
                profile,
                profile.MinimumWall,
                profile.MinimumWallMaximum,
                shell.RoomFloorMinimum,
                shell.RoomCeilingMinimum,
                minimumWallMaterial,
                true);
            CreateRoomSideBoundaryMembrane(
                connector,
                definition,
                "RoomRightSideBoundaryMembrane",
                profile,
                profile.MaximumWall,
                profile.MaximumWallMinimum,
                shell.RoomFloorMaximum,
                shell.RoomCeilingMaximum,
                maximumWallMaterial,
                false);
            CreateRoomSideBoundaryMembrane(
                connector,
                definition,
                "RoomLeftOuterBoundaryMembrane",
                profile,
                profile.MinimumWall,
                profile.MinimumWallMinimum,
                shell.RoomOuterFloorMinimum,
                shell.RoomOuterCeilingMinimum,
                minimumWallMaterial,
                true);
            CreateRoomSideBoundaryMembrane(
                connector,
                definition,
                "RoomRightOuterBoundaryMembrane",
                profile,
                profile.MaximumWall,
                profile.MaximumWallMaximum,
                shell.RoomOuterFloorMaximum,
                shell.RoomOuterCeilingMaximum,
                maximumWallMaterial,
                false);
        }

        private static void CreateControlRoomH02AdaptiveWallVolumes(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorSolidCorners[] solids,
            Material minimumWallMaterial,
            Material maximumWallMaterial)
        {
            const float roomContactOverlap = 1.20f;
            const float roomWallFlare = 0.42f;
            var minimumWallRoom = GetConnectorSolidRoomCorners(solids[3]);
            var maximumWallRoom = GetConnectorSolidRoomCorners(solids[1]);
            var roomInward = -HorizontalDirection(profile.Entrance.Outward);
            var openingCenter = 0.5f *
                (minimumWallRoom[1] + maximumWallRoom[0]);
            var minimumOutward = HorizontalDirection(
                minimumWallRoom[1] - openingCenter);
            var maximumOutward = HorizontalDirection(
                maximumWallRoom[0] - openingCenter);
            var minimumRoomOffset =
                (roomInward * roomContactOverlap) +
                (minimumOutward * roomWallFlare);
            var maximumRoomOffset =
                (roomInward * roomContactOverlap) +
                (maximumOutward * roomWallFlare);
            CreateConnectorSolidPart(
                connector,
                definition,
                "AdaptiveLeftWallVolume",
                new ConnectorSolidCorners(
                    minimumWallRoom[0] + minimumRoomOffset,
                    minimumWallRoom[1] + minimumRoomOffset,
                    minimumWallRoom[2] + minimumRoomOffset,
                    minimumWallRoom[3] + minimumRoomOffset,
                    minimumWallRoom[0],
                    minimumWallRoom[1],
                    minimumWallRoom[2],
                    minimumWallRoom[3]),
                minimumWallMaterial);

            CreateConnectorSolidPart(
                connector,
                definition,
                "AdaptiveRightWallVolume",
                new ConnectorSolidCorners(
                    maximumWallRoom[0] + maximumRoomOffset,
                    maximumWallRoom[1] + maximumRoomOffset,
                    maximumWallRoom[2] + maximumRoomOffset,
                    maximumWallRoom[3] + maximumRoomOffset,
                    maximumWallRoom[0],
                    maximumWallRoom[1],
                    maximumWallRoom[2],
                    maximumWallRoom[3]),
                maximumWallMaterial);
        }

        private static Vector3 GetAdaptiveRoomBoundaryPoint(
            ConnectorEntranceProfile profile,
            Renderer firstSurface,
            Renderer secondSurface,
            float acrossProjection,
            float y)
        {
            var connectorProjection = Vector3.Dot(
                profile.Entrance.Point,
                profile.Entrance.Outward);
            var firstProjection = GetRendererBoundaryProjectionAtAcross(
                firstSurface,
                profile.Across,
                profile.Entrance.Outward,
                acrossProjection,
                out var resolvedFirstAcross);
            var secondProjection = GetRendererBoundaryProjectionAtAcross(
                secondSurface,
                profile.Across,
                profile.Entrance.Outward,
                acrossProjection,
                out var resolvedSecondAcross);
            var sourceProjection = Mathf.Min(
                connectorProjection,
                Mathf.Min(firstProjection, secondProjection));
            var resolvedAcross = (resolvedFirstAcross + resolvedSecondAcross) * 0.5f;
            var point = profile.Entrance.Point +
                (profile.Entrance.Outward *
                 (sourceProjection - connectorProjection)) +
                (profile.Across *
                 (resolvedAcross -
                  Vector3.Dot(profile.Entrance.Point, profile.Across)));
            point.y = y;
            return point;
        }

        private static void CreateRoomSideBoundaryMembrane(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorEntranceProfile profile,
            Renderer wall,
            float acrossProjection,
            Vector3 connectorBottom,
            Vector3 connectorTop,
            Material material,
            bool minimumSide)
        {
            var connectorProjection = Vector3.Dot(
                connectorBottom,
                profile.Entrance.Outward);
            var floorProjection = GetRendererBoundaryProjectionAtAcross(
                profile.Floor,
                profile.Across,
                profile.Entrance.Outward,
                acrossProjection,
                out var resolvedFloorAcross);
            var ceilingProjection = GetRendererBoundaryProjectionAtAcross(
                profile.Ceiling,
                profile.Across,
                profile.Entrance.Outward,
                acrossProjection,
                out var resolvedCeilingAcross);
            var wallProjection = GetRendererBoundaryProjectionAtAcross(
                wall,
                profile.Across,
                profile.Entrance.Outward,
                acrossProjection,
                out var resolvedWallAcross);
            var sourceBottomProjection = Mathf.Min(
                connectorProjection,
                Mathf.Min(floorProjection, wallProjection));
            var sourceTopProjection = Mathf.Min(
                connectorProjection,
                Mathf.Min(ceilingProjection, wallProjection));
            const float sourceContactOverlap = 0.04f;
            const float connectorContactOverlap = 0.02f;
            sourceBottomProjection -= sourceContactOverlap;
            sourceTopProjection -= sourceContactOverlap;
            var sourceBottomAcross = minimumSide
                ? Mathf.Max(resolvedFloorAcross, resolvedWallAcross)
                : Mathf.Min(resolvedFloorAcross, resolvedWallAcross);
            var sourceTopAcross = minimumSide
                ? Mathf.Max(resolvedCeilingAcross, resolvedWallAcross)
                : Mathf.Min(resolvedCeilingAcross, resolvedWallAcross);
            var sourceBottom = connectorBottom +
                (profile.Entrance.Outward *
                 (sourceBottomProjection - connectorProjection)) +
                (profile.Across *
                 (sourceBottomAcross - acrossProjection));
            var sourceTop = connectorTop +
                (profile.Entrance.Outward *
                 (sourceTopProjection - connectorProjection)) +
                (profile.Across *
                 (sourceTopAcross - acrossProjection));
            sourceBottom.y = profile.Entrance.FloorY;
            sourceTop.y = profile.Ceiling.bounds.min.y;
            var overlappedConnectorBottom = connectorBottom +
                (profile.Entrance.Outward * connectorContactOverlap);
            var overlappedConnectorTop = connectorTop +
                (profile.Entrance.Outward * connectorContactOverlap);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                partName,
                sourceBottom,
                sourceTop,
                overlappedConnectorTop,
                overlappedConnectorBottom,
                material);
            var part = connector.Find(partName) ??
                throw new InvalidOperationException(
                    definition.Id + " failed to create " + partName + ".");
            CreateRoomSideBoundaryMembraneCollider(
                part.gameObject,
                definition,
                partName,
                sourceBottom,
                sourceTop,
                overlappedConnectorTop,
                overlappedConnectorBottom);
        }

        private static void CreateRoomContactTransitionFairing(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorEntranceProfile profile,
            Renderer wall,
            float acrossProjection,
            Vector3 connectorBottom,
            Vector3 connectorTop,
            Vector3 corridorBottom,
            Vector3 corridorTop,
            Material material,
            bool minimumSide)
        {
            const int segmentCount = 24;
            const float roomRun = 0.06f;
            const float connectorProgress = 0.04f;
            var sourceBottom = GetAdaptiveRoomBoundaryPoint(
                profile,
                profile.Floor,
                wall,
                acrossProjection,
                profile.Entrance.FloorY);
            var sourceTop = GetAdaptiveRoomBoundaryPoint(
                profile,
                profile.Ceiling,
                wall,
                acrossProjection,
                profile.Ceiling.bounds.min.y);
            sourceBottom += profile.Across *
                (acrossProjection - Vector3.Dot(sourceBottom, profile.Across));
            sourceTop += profile.Across *
                (acrossProjection - Vector3.Dot(sourceTop, profile.Across));
            var connectorDirection = HorizontalDirection(
                (corridorBottom + corridorTop) -
                (connectorBottom + connectorTop));
            var roomDirection = HorizontalDirection(profile.Entrance.Outward);
            if (Vector3.Dot(roomDirection, connectorDirection) < 0f)
            {
                roomDirection = -roomDirection;
            }

            var roomBottom = sourceBottom - (roomDirection * roomRun);
            var roomTop = sourceTop - (roomDirection * roomRun);
            var targetBottom = Vector3.Lerp(
                connectorBottom,
                corridorBottom,
                connectorProgress);
            var targetTop = Vector3.Lerp(
                connectorTop,
                corridorTop,
                connectorProgress);
            var towardWalkway = minimumSide
                ? HorizontalDirection(
                    profile.MaximumWall.bounds.center -
                    profile.MinimumWall.bounds.center)
                : HorizontalDirection(
                    profile.MinimumWall.bounds.center -
                    profile.MaximumWall.bounds.center);
            var vertices = new List<Vector3>(segmentCount * 8);
            var triangles = new List<int>(segmentCount * 12);
            var uv = new List<Vector2>(vertices.Capacity);
            for (var segmentIndex = 0;
                 segmentIndex < segmentCount;
                 segmentIndex++)
            {
                var startT = segmentIndex / (float)segmentCount;
                var endT = (segmentIndex + 1) / (float)segmentCount;
                var startInset = Mathf.Sin(Mathf.PI * startT);
                var endInset = Mathf.Sin(Mathf.PI * endT);
                var startBottom = EvaluateCubicBezier(
                    roomBottom,
                    sourceBottom,
                    connectorBottom,
                    targetBottom,
                    startT) + (towardWalkway * (0.025f * startInset * startInset));
                var startTop = EvaluateCubicBezier(
                    roomTop,
                    sourceTop,
                    connectorTop,
                    targetTop,
                    startT) + (towardWalkway * (0.025f * startInset * startInset));
                var endBottom = EvaluateCubicBezier(
                    roomBottom,
                    sourceBottom,
                    connectorBottom,
                    targetBottom,
                    endT) + (towardWalkway * (0.025f * endInset * endInset));
                var endTop = EvaluateCubicBezier(
                    roomTop,
                    sourceTop,
                    connectorTop,
                    targetTop,
                    endT) + (towardWalkway * (0.025f * endInset * endInset));
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    startBottom,
                    endBottom,
                    endTop,
                    startTop);
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static void CreateRoomVerticalRoundedContactCap(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorEntranceProfile profile,
            Renderer wall,
            float acrossProjection,
            Material material,
            bool minimumSide,
            float radius)
        {
            const int segmentCount = 24;
            var outwardProjection = GetRendererProjectionMaximum(
                wall,
                profile.Entrance.Outward);
            var center = profile.PlaneCenter;
            center += profile.Across *
                (acrossProjection - Vector3.Dot(center, profile.Across));
            center += profile.Entrance.Outward *
                (outwardProjection -
                 Vector3.Dot(center, profile.Entrance.Outward));
            var towardWalkway = minimumSide
                ? profile.Across
                : -profile.Across;
            // Keep the rounded closure tangent to the clear passage instead
            // of allowing most of its radius to project into the walkway.
            center -= towardWalkway * radius;
            var bottomY = profile.Entrance.FloorY;
            var topY = profile.Ceiling.bounds.min.y;
            var vertices = new List<Vector3>(segmentCount * 14);
            var triangles = new List<int>(segmentCount * 18);
            var uv = new List<Vector2>(vertices.Capacity);
            for (var segmentIndex = 0;
                 segmentIndex < segmentCount;
                 segmentIndex++)
            {
                var startAngle =
                    (segmentIndex / (float)segmentCount) * Mathf.PI * 2f;
                var endAngle =
                    ((segmentIndex + 1) / (float)segmentCount) * Mathf.PI * 2f;
                var startOffset =
                    (profile.Across * (Mathf.Cos(startAngle) * radius)) +
                    (profile.Entrance.Outward *
                     (Mathf.Sin(startAngle) * radius));
                var endOffset =
                    (profile.Across * (Mathf.Cos(endAngle) * radius)) +
                    (profile.Entrance.Outward *
                     (Mathf.Sin(endAngle) * radius));
                var startBottom = center + startOffset;
                startBottom.y = bottomY;
                var endBottom = center + endOffset;
                endBottom.y = bottomY;
                var startTop = startBottom;
                startTop.y = topY;
                var endTop = endBottom;
                endTop.y = topY;
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    startBottom,
                    endBottom,
                    endTop,
                    startTop);
                var bottomCenter = center;
                bottomCenter.y = bottomY;
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    bottomCenter,
                    endBottom,
                    startBottom);
                var topCenter = center;
                topCenter.y = topY;
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    topCenter,
                    startTop,
                    endTop);
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;
        }

        private static Vector3 EvaluateCubicBezier(
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            float t)
        {
            var oneMinusT = 1f - t;
            return
                (oneMinusT * oneMinusT * oneMinusT * first) +
                (3f * oneMinusT * oneMinusT * t * second) +
                (3f * oneMinusT * t * t * third) +
                (t * t * t * fourth);
        }

        private static void CreateRoomSideBoundaryMembraneCollider(
            GameObject part,
            ConnectorEndpointDefinition definition,
            string partName,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth)
        {
            const float colliderThickness = 0.02f;
            var vertical = second - first;
            var longitudinal = fourth - first;
            var normal = Vector3.Cross(vertical, longitudinal);
            if (normal.sqrMagnitude < 0.000001f)
            {
                normal = Vector3.Cross(third - second, vertical);
            }

            if (normal.sqrMagnitude < 0.000001f)
            {
                return;
            }

            var halfOffset = normal.normalized * (colliderThickness * 0.5f);
            var vertices = new List<Vector3>
            {
                first - halfOffset,
                second - halfOffset,
                third - halfOffset,
                fourth - halfOffset,
                first + halfOffset,
                second + halfOffset,
                third + halfOffset,
                fourth + halfOffset
            };
            var triangles = new List<int>
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7
            };
            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName + "_Collider"
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var collider = part.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true;
        }

        private static void CreateConnectorCircularCornerPlug(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            Vector3 center,
            Vector3 horizontal,
            Vector3 vertical,
            Material material)
        {
            const int segmentCount = 24;
            const float radius = 0.11f;
            var vertices = new List<Vector3>(segmentCount * 6);
            var triangles = new List<int>(segmentCount * 6);
            var uv = new List<Vector2>(vertices.Capacity);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var startAngle = (segmentIndex / (float)segmentCount) *
                    Mathf.PI * 2f;
                var endAngle = ((segmentIndex + 1) / (float)segmentCount) *
                    Mathf.PI * 2f;
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    center,
                    center + (horizontal * (Mathf.Cos(startAngle) * radius)) +
                        (vertical * (Mathf.Sin(startAngle) * radius)),
                    center + (horizontal * (Mathf.Cos(endAngle) * radius)) +
                        (vertical * (Mathf.Sin(endAngle) * radius)));
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static void CreateConnectorCurvedInnerCornerSeal(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            Vector3 roomCorner,
            Vector3 corridorCorner,
            Vector3 roomHorizontal,
            Vector3 corridorHorizontal,
            Vector3 vertical,
            float radius,
            Material material)
        {
            const int longitudinalSegments = 24;
            const int arcSegments = 10;
            var vertices = new List<Vector3>(
                (longitudinalSegments * arcSegments * 8) + (arcSegments * 12));
            var triangles = new List<int>(
                (longitudinalSegments * arcSegments * 12) + (arcSegments * 18));
            var uv = new List<Vector2>(vertices.Capacity);
            for (var longitudinalIndex = 0;
                 longitudinalIndex < longitudinalSegments;
                 longitudinalIndex++)
            {
                var startT = longitudinalIndex / (float)longitudinalSegments;
                var endT = (longitudinalIndex + 1) / (float)longitudinalSegments;
                for (var arcIndex = 0; arcIndex < arcSegments; arcIndex++)
                {
                    var startArc = arcIndex / (float)arcSegments;
                    var endArc = (arcIndex + 1) / (float)arcSegments;
                    AddDoubleSidedConnectorQuad(
                        vertices,
                        triangles,
                        uv,
                        EvaluateConnectorCornerFillet(
                            roomCorner,
                            corridorCorner,
                            roomHorizontal,
                            corridorHorizontal,
                            vertical,
                            radius,
                            startT,
                            startArc),
                        EvaluateConnectorCornerFillet(
                            roomCorner,
                            corridorCorner,
                            roomHorizontal,
                            corridorHorizontal,
                            vertical,
                            radius,
                            startT,
                            endArc),
                        EvaluateConnectorCornerFillet(
                            roomCorner,
                            corridorCorner,
                            roomHorizontal,
                            corridorHorizontal,
                            vertical,
                            radius,
                            endT,
                            endArc),
                        EvaluateConnectorCornerFillet(
                            roomCorner,
                            corridorCorner,
                            roomHorizontal,
                            corridorHorizontal,
                            vertical,
                            radius,
                        endT,
                        startArc));
                }

                var startCenter = Vector3.Lerp(
                    roomCorner,
                    corridorCorner,
                    startT);
                var endCenter = Vector3.Lerp(
                    roomCorner,
                    corridorCorner,
                    endT);
                var startHorizontalPoint = EvaluateConnectorCornerFillet(
                    roomCorner,
                    corridorCorner,
                    roomHorizontal,
                    corridorHorizontal,
                    vertical,
                    radius,
                    startT,
                    0f);
                var endHorizontalPoint = EvaluateConnectorCornerFillet(
                    roomCorner,
                    corridorCorner,
                    roomHorizontal,
                    corridorHorizontal,
                    vertical,
                    radius,
                    endT,
                    0f);
                var startVerticalPoint = EvaluateConnectorCornerFillet(
                    roomCorner,
                    corridorCorner,
                    roomHorizontal,
                    corridorHorizontal,
                    vertical,
                    radius,
                    startT,
                    1f);
                var endVerticalPoint = EvaluateConnectorCornerFillet(
                    roomCorner,
                    corridorCorner,
                    roomHorizontal,
                    corridorHorizontal,
                    vertical,
                    radius,
                    endT,
                    1f);
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    startCenter,
                    startHorizontalPoint,
                    endHorizontalPoint,
                    endCenter);
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    startCenter,
                    endCenter,
                    endVerticalPoint,
                    startVerticalPoint);
            }

            for (var arcIndex = 0; arcIndex < arcSegments; arcIndex++)
            {
                var startArc = arcIndex / (float)arcSegments;
                var endArc = (arcIndex + 1) / (float)arcSegments;
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    roomCorner,
                    EvaluateConnectorCornerFillet(
                        roomCorner,
                        corridorCorner,
                        roomHorizontal,
                        corridorHorizontal,
                        vertical,
                        radius,
                        0f,
                        endArc),
                    EvaluateConnectorCornerFillet(
                        roomCorner,
                        corridorCorner,
                        roomHorizontal,
                        corridorHorizontal,
                        vertical,
                        radius,
                        0f,
                        startArc));
                AddDoubleSidedConnectorTriangle(
                    vertices,
                    triangles,
                    uv,
                    corridorCorner,
                    EvaluateConnectorCornerFillet(
                        roomCorner,
                        corridorCorner,
                        roomHorizontal,
                        corridorHorizontal,
                        vertical,
                        radius,
                        1f,
                        startArc),
                    EvaluateConnectorCornerFillet(
                        roomCorner,
                        corridorCorner,
                        roomHorizontal,
                        corridorHorizontal,
                        vertical,
                        radius,
                        1f,
                        endArc));
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static Vector3 EvaluateConnectorCornerFillet(
            Vector3 roomCorner,
            Vector3 corridorCorner,
            Vector3 roomHorizontal,
            Vector3 corridorHorizontal,
            Vector3 vertical,
            float radius,
            float longitudinal,
            float arc)
        {
            var center = Vector3.Lerp(roomCorner, corridorCorner, longitudinal);
            var horizontal = Vector3.Lerp(
                roomHorizontal,
                corridorHorizontal,
                longitudinal).normalized;
            var angle = arc * Mathf.PI * 0.5f;
            return center +
                (horizontal * (Mathf.Cos(angle) * radius)) +
                (vertical * (Mathf.Sin(angle) * radius));
        }

        private static void CreateRoomFloorApproachFill(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material material)
        {
            const int segmentCount = 24;
            const float minimumDepth = 0.002f;
            var roomTop = new Vector3[segmentCount + 1];
            var roomBottom = new Vector3[segmentCount + 1];
            var connectorTop = new Vector3[segmentCount + 1];
            var connectorBottom = new Vector3[segmentCount + 1];
            var maximumDepth = 0f;
            for (var pointIndex = 0; pointIndex <= segmentCount; pointIndex++)
            {
                var t = pointIndex / (float)segmentCount;
                var roomOuterTopMinimum = new Vector3(
                    shell.RoomOuterFloorMinimum.x,
                    shell.RoomFloorMinimum.y,
                    shell.RoomOuterFloorMinimum.z);
                var roomOuterTopMaximum = new Vector3(
                    shell.RoomOuterFloorMaximum.x,
                    shell.RoomFloorMaximum.y,
                    shell.RoomOuterFloorMaximum.z);
                var top = Vector3.Lerp(
                    roomOuterTopMinimum,
                    roomOuterTopMaximum,
                    t);
                var acrossProjection = Vector3.Dot(top, profile.Across);
                var connectorProjection = Vector3.Dot(
                    top,
                    profile.Entrance.Outward);
                var sourceProjection = Mathf.Min(
                    connectorProjection,
                    GetRendererBoundaryProjectionAtAcross(
                        profile.Floor,
                        profile.Across,
                        profile.Entrance.Outward,
                        acrossProjection,
                        out var resolvedAcrossProjection));
                maximumDepth = Mathf.Max(
                    maximumDepth,
                    connectorProjection - sourceProjection);
                connectorTop[pointIndex] = top;
                connectorBottom[pointIndex] = new Vector3(
                    top.x,
                    shell.RoomOuterFloorMinimum.y,
                    top.z);
                var offset = (profile.Entrance.Outward *
                    (sourceProjection - connectorProjection)) +
                    (profile.Across *
                     (resolvedAcrossProjection - acrossProjection));
                roomTop[pointIndex] = top + offset;
                roomBottom[pointIndex] = connectorBottom[pointIndex] + offset;
            }

            if (maximumDepth <= minimumDepth)
            {
                return;
            }

            var vertices = new List<Vector3>(segmentCount * 32);
            var triangles = new List<int>(segmentCount * 24);
            var uv = new List<Vector2>(segmentCount * 32);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var firstDepth = Vector3.Distance(
                    roomTop[segmentIndex],
                    connectorTop[segmentIndex]);
                var secondDepth = Vector3.Distance(
                    roomTop[segmentIndex + 1],
                    connectorTop[segmentIndex + 1]);
                if (firstDepth <= minimumDepth && secondDepth <= minimumDepth)
                {
                    continue;
                }

                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    roomTop[segmentIndex],
                    roomTop[segmentIndex + 1],
                    connectorTop[segmentIndex + 1],
                    connectorTop[segmentIndex]);
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    roomBottom[segmentIndex],
                    connectorBottom[segmentIndex],
                    connectorBottom[segmentIndex + 1],
                    roomBottom[segmentIndex + 1]);
            }

            AddDoubleSidedConnectorQuad(
                vertices,
                triangles,
                uv,
                roomTop[0],
                connectorTop[0],
                connectorBottom[0],
                roomBottom[0]);
            AddDoubleSidedConnectorQuad(
                vertices,
                triangles,
                uv,
                connectorTop[segmentCount],
                roomTop[segmentCount],
                roomBottom[segmentCount],
                connectorBottom[segmentCount]);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    roomTop[segmentIndex + 1],
                    roomTop[segmentIndex],
                    roomBottom[segmentIndex],
                    roomBottom[segmentIndex + 1]);
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    connectorTop[segmentIndex],
                    connectorTop[segmentIndex + 1],
                    connectorBottom[segmentIndex + 1],
                    connectorBottom[segmentIndex]);
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_RoomFloorApproachFill"
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject("RoomFloorApproachFill");
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static void CreateRoomCeilingApproachFill(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material material)
        {
            const int segmentCount = 24;
            const float minimumDepth = 0.002f;
            var roomInner = new Vector3[segmentCount + 1];
            var roomOuter = new Vector3[segmentCount + 1];
            var connectorInner = new Vector3[segmentCount + 1];
            var connectorOuter = new Vector3[segmentCount + 1];
            var maximumDepth = 0f;
            for (var pointIndex = 0; pointIndex <= segmentCount; pointIndex++)
            {
                var t = pointIndex / (float)segmentCount;
                var connectorMinimum = new Vector3(
                    shell.RoomOuterCeilingMinimum.x,
                    shell.RoomCeilingMinimum.y,
                    shell.RoomOuterCeilingMinimum.z);
                var connectorMaximum = new Vector3(
                    shell.RoomOuterCeilingMaximum.x,
                    shell.RoomCeilingMaximum.y,
                    shell.RoomOuterCeilingMaximum.z);
                var inner = Vector3.Lerp(connectorMinimum, connectorMaximum, t);
                var acrossProjection = Vector3.Dot(inner, profile.Across);
                var connectorProjection = Vector3.Dot(
                    inner,
                    profile.Entrance.Outward);
                var sourceProjection = Mathf.Min(
                    connectorProjection,
                    GetRendererBoundaryProjectionAtAcross(
                        profile.Ceiling,
                        profile.Across,
                        profile.Entrance.Outward,
                        acrossProjection,
                        out var resolvedAcrossProjection));
                maximumDepth = Mathf.Max(
                    maximumDepth,
                    connectorProjection - sourceProjection);
                connectorInner[pointIndex] = inner;
                connectorOuter[pointIndex] = new Vector3(
                    inner.x,
                    shell.RoomOuterCeilingMinimum.y,
                    inner.z);
                var offset = (profile.Entrance.Outward *
                    (sourceProjection - connectorProjection)) +
                    (profile.Across *
                     (resolvedAcrossProjection - acrossProjection));
                roomInner[pointIndex] = inner + offset;
                roomOuter[pointIndex] = connectorOuter[pointIndex] + offset;
            }

            if (maximumDepth <= minimumDepth)
            {
                return;
            }

            var vertices = new List<Vector3>(segmentCount * 32);
            var triangles = new List<int>(segmentCount * 24);
            var uv = new List<Vector2>(segmentCount * 32);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                var firstDepth = Vector3.Distance(
                    roomInner[segmentIndex],
                    connectorInner[segmentIndex]);
                var secondDepth = Vector3.Distance(
                    roomInner[segmentIndex + 1],
                    connectorInner[segmentIndex + 1]);
                if (firstDepth <= minimumDepth && secondDepth <= minimumDepth)
                {
                    continue;
                }

                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    roomInner[segmentIndex],
                    connectorInner[segmentIndex],
                    connectorInner[segmentIndex + 1],
                    roomInner[segmentIndex + 1]);
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    roomOuter[segmentIndex],
                    roomOuter[segmentIndex + 1],
                    connectorOuter[segmentIndex + 1],
                    connectorOuter[segmentIndex]);
            }

            AddDoubleSidedConnectorQuad(
                vertices,
                triangles,
                uv,
                roomInner[0],
                roomOuter[0],
                connectorOuter[0],
                connectorInner[0]);
            AddDoubleSidedConnectorQuad(
                vertices,
                triangles,
                uv,
                connectorInner[segmentCount],
                connectorOuter[segmentCount],
                roomOuter[segmentCount],
                roomInner[segmentCount]);
            for (var segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    roomInner[segmentIndex + 1],
                    roomInner[segmentIndex],
                    roomOuter[segmentIndex],
                    roomOuter[segmentIndex + 1]);
                AddDoubleSidedConnectorQuad(
                    vertices,
                    triangles,
                    uv,
                    connectorInner[segmentIndex],
                    connectorInner[segmentIndex + 1],
                    connectorOuter[segmentIndex + 1],
                    connectorOuter[segmentIndex]);
            }

            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_RoomCeilingApproachFill"
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject("RoomCeilingApproachFill");
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static void CreateRoomFloorWallFootprintFills(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material material)
        {
            CreateRoomFloorWallFootprintFill(
                connector,
                definition,
                "RoomFloorLeftWallFootprintFill",
                profile,
                profile.MinimumWall,
                profile.MinimumWallMinimum,
                profile.MinimumWallMaximum,
                shell.RoomOuterFloorMinimum.y,
                shell.RoomFloorMinimum.y,
                material);
            CreateRoomFloorWallFootprintFill(
                connector,
                definition,
                "RoomFloorRightWallFootprintFill",
                profile,
                profile.MaximumWall,
                profile.MaximumWallMinimum,
                profile.MaximumWallMaximum,
                shell.RoomOuterFloorMaximum.y,
                shell.RoomFloorMaximum.y,
                material);
        }

        private static void CreateRoomFloorWallFootprintFill(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorEntranceProfile profile,
            Renderer wall,
            float minimumAcross,
            float maximumAcross,
            float bottomY,
            float topY,
            Material material,
            bool useSharedSourceDepth = false)
        {
            var connectorBase = profile.Entrance.Point +
                (profile.Entrance.Outward * 0f);
            var connectorAcross = Vector3.Dot(connectorBase, profile.Across);
            var connectorMinimum = connectorBase +
                (profile.Across * (minimumAcross - connectorAcross));
            var connectorMaximum = connectorBase +
                (profile.Across * (maximumAcross - connectorAcross));
            var connectorMinimumProjection = Vector3.Dot(
                connectorMinimum,
                profile.Entrance.Outward);
            var connectorMaximumProjection = Vector3.Dot(
                connectorMaximum,
                profile.Entrance.Outward);
            var minimumSide = wall == profile.MinimumWall;
            var minimumBoundaryRenderer = minimumSide
                ? wall
                : profile.Floor;
            var maximumBoundaryRenderer = minimumSide
                ? profile.Floor
                : wall;
            var sourceMinimumProjection = Mathf.Min(
                connectorMinimumProjection,
                GetRendererBoundaryProjectionAtAcross(
                    minimumBoundaryRenderer,
                    profile.Across,
                    profile.Entrance.Outward,
                    minimumAcross,
                    out var resolvedMinimumAcross));
            var sourceMaximumProjection = Mathf.Min(
                connectorMaximumProjection,
                GetRendererBoundaryProjectionAtAcross(
                    maximumBoundaryRenderer,
                    profile.Across,
                    profile.Entrance.Outward,
                    maximumAcross,
                    out var resolvedMaximumAcross));
            if (useSharedSourceDepth)
            {
                var sharedSourceProjection = Mathf.Min(
                    sourceMinimumProjection,
                    sourceMaximumProjection);
                sourceMinimumProjection = sharedSourceProjection;
                sourceMaximumProjection = sharedSourceProjection;
            }
            if ((connectorMinimumProjection - sourceMinimumProjection) <= 0.002f &&
                (connectorMaximumProjection - sourceMaximumProjection) <= 0.002f)
            {
                return;
            }

            const float sourceContactOverlap = 0.04f;
            const float connectorContactOverlap = 0.02f;
            sourceMinimumProjection -= sourceContactOverlap;
            sourceMaximumProjection -= sourceContactOverlap;

            var sourceMinimum = connectorMinimum +
                (profile.Entrance.Outward *
                 (sourceMinimumProjection - connectorMinimumProjection)) +
                (profile.Across *
                 ((useSharedSourceDepth
                     ? minimumAcross
                     : resolvedMinimumAcross) - minimumAcross));
            var sourceMaximum = connectorMaximum +
                (profile.Entrance.Outward *
                 (sourceMaximumProjection - connectorMaximumProjection)) +
                (profile.Across *
                 ((useSharedSourceDepth
                     ? maximumAcross
                     : resolvedMaximumAcross) - maximumAcross));
            sourceMinimum.y = 0f;
            sourceMaximum.y = 0f;
            connectorMinimum += profile.Entrance.Outward * connectorContactOverlap;
            connectorMaximum += profile.Entrance.Outward * connectorContactOverlap;
            connectorMinimum.y = 0f;
            connectorMaximum.y = 0f;
            var corners = new ConnectorSolidCorners(
                new Vector3(sourceMinimum.x, bottomY, sourceMinimum.z),
                new Vector3(sourceMaximum.x, bottomY, sourceMaximum.z),
                new Vector3(sourceMaximum.x, topY, sourceMaximum.z),
                new Vector3(sourceMinimum.x, topY, sourceMinimum.z),
                new Vector3(connectorMinimum.x, bottomY, connectorMinimum.z),
                new Vector3(connectorMaximum.x, bottomY, connectorMaximum.z),
                new Vector3(connectorMaximum.x, topY, connectorMaximum.z),
                new Vector3(connectorMinimum.x, topY, connectorMinimum.z));
            CreateConnectorSolidPart(
                connector,
                definition,
                partName,
                corners,
                material);
        }

        private static ConnectorSolidCorners GetConnectorHorizontalWallOverlapSolid(
            ConnectorGapShell shell,
            bool ceiling)
        {
            if (!ceiling)
            {
                return new ConnectorSolidCorners(
                    shell.RoomOuterFloorMinimum,
                    shell.RoomOuterFloorMaximum,
                    new Vector3(
                        shell.RoomOuterFloorMaximum.x,
                        shell.RoomFloorMaximum.y,
                        shell.RoomOuterFloorMaximum.z),
                    new Vector3(
                        shell.RoomOuterFloorMinimum.x,
                        shell.RoomFloorMinimum.y,
                        shell.RoomOuterFloorMinimum.z),
                    shell.CorridorOuterFloorMinimum,
                    shell.CorridorOuterFloorMaximum,
                    new Vector3(
                        shell.CorridorOuterFloorMaximum.x,
                        shell.CorridorFloorMaximum.y,
                        shell.CorridorOuterFloorMaximum.z),
                    new Vector3(
                        shell.CorridorOuterFloorMinimum.x,
                        shell.CorridorFloorMinimum.y,
                        shell.CorridorOuterFloorMinimum.z));
            }

            return new ConnectorSolidCorners(
                new Vector3(
                    shell.RoomOuterCeilingMinimum.x,
                    shell.RoomCeilingMinimum.y,
                    shell.RoomOuterCeilingMinimum.z),
                new Vector3(
                    shell.RoomOuterCeilingMaximum.x,
                    shell.RoomCeilingMaximum.y,
                    shell.RoomOuterCeilingMaximum.z),
                shell.RoomOuterCeilingMaximum,
                shell.RoomOuterCeilingMinimum,
                new Vector3(
                    shell.CorridorOuterCeilingMinimum.x,
                    shell.CorridorCeilingMinimum.y,
                    shell.CorridorOuterCeilingMinimum.z),
                new Vector3(
                    shell.CorridorOuterCeilingMaximum.x,
                    shell.CorridorCeilingMaximum.y,
                    shell.CorridorOuterCeilingMaximum.z),
                shell.CorridorOuterCeilingMaximum,
                shell.CorridorOuterCeilingMinimum);
        }

        private static float GetRendererBoundaryProjectionAtAcross(
            Renderer renderer,
            Vector3 across,
            Vector3 outward,
            float targetAcrossProjection,
            out float resolvedAcrossProjection)
        {
            GetRendererProjectionRange(
                renderer,
                across,
                out var rendererMinimumAcross,
                out var rendererMaximumAcross);
            resolvedAcrossProjection = Mathf.Clamp(
                targetAcrossProjection,
                rendererMinimumAcross,
                rendererMaximumAcross);
            var meshFilter = renderer.GetComponent<MeshFilter>();
            var mesh = meshFilter == null ? null : meshFilter.sharedMesh;
            if (mesh == null)
            {
                return GetRendererProjectionMaximum(renderer, outward);
            }

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var maximumProjection = float.NegativeInfinity;
            for (var triangleIndex = 0;
                 triangleIndex + 2 < triangles.Length;
                 triangleIndex += 3)
            {
                var first = renderer.transform.TransformPoint(vertices[triangles[triangleIndex]]);
                var second = renderer.transform.TransformPoint(vertices[triangles[triangleIndex + 1]]);
                var third = renderer.transform.TransformPoint(vertices[triangles[triangleIndex + 2]]);
                IncludeBoundaryEdgeProjection(
                    first,
                    second,
                    across,
                    outward,
                    resolvedAcrossProjection,
                    ref maximumProjection);
                IncludeBoundaryEdgeProjection(
                    second,
                    third,
                    across,
                    outward,
                    resolvedAcrossProjection,
                    ref maximumProjection);
                IncludeBoundaryEdgeProjection(
                    third,
                    first,
                    across,
                    outward,
                    resolvedAcrossProjection,
                    ref maximumProjection);
            }

            return float.IsNegativeInfinity(maximumProjection)
                ? GetRendererProjectionMaximum(renderer, outward)
                : maximumProjection;
        }

        private static void CreateRoomHorizontalSideWedgeFills(
            Transform connector,
            ConnectorEndpointDefinition definition,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Material floorMaterial,
            Material ceilingMaterial)
        {
            CreateRoomHorizontalSideWedgeFill(
                connector,
                definition,
                "RoomFloorLeftWedgeFill",
                profile,
                profile.Floor,
                profile.MinimumWall,
                shell.RoomFloorMinimum,
                shell.RoomFloorMinimum.y,
                shell.RoomOuterFloorMinimum.y,
                floorMaterial);
            CreateRoomHorizontalSideWedgeFill(
                connector,
                definition,
                "RoomFloorRightWedgeFill",
                profile,
                profile.Floor,
                profile.MaximumWall,
                shell.RoomFloorMaximum,
                shell.RoomFloorMaximum.y,
                shell.RoomOuterFloorMaximum.y,
                floorMaterial);
            CreateRoomHorizontalSideWedgeFill(
                connector,
                definition,
                "RoomCeilingLeftWedgeFill",
                profile,
                profile.Ceiling,
                profile.MinimumWall,
                shell.RoomCeilingMinimum,
                shell.RoomCeilingMinimum.y,
                shell.RoomOuterCeilingMinimum.y,
                ceilingMaterial);
            CreateRoomHorizontalSideWedgeFill(
                connector,
                definition,
                "RoomCeilingRightWedgeFill",
                profile,
                profile.Ceiling,
                profile.MaximumWall,
                shell.RoomCeilingMaximum,
                shell.RoomCeilingMaximum.y,
                shell.RoomOuterCeilingMaximum.y,
                ceilingMaterial);
        }

        private static void CreateRoomHorizontalSideWedgeFill(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorEntranceProfile profile,
            Renderer horizontalRenderer,
            Renderer sideWallRenderer,
            Vector3 connectorCorner,
            float surfaceY,
            float outerY,
            Material material)
        {
            var targetAcrossProjection = Vector3.Dot(
                connectorCorner,
                profile.Across);
            var connectorProjection = Vector3.Dot(
                connectorCorner,
                profile.Entrance.Outward);
            var horizontalProjection = Mathf.Min(
                connectorProjection,
                GetRendererBoundaryProjectionAtAcross(
                    horizontalRenderer,
                    profile.Across,
                    profile.Entrance.Outward,
                    targetAcrossProjection,
                    out var resolvedAcrossProjection));
            var wallProjection = Mathf.Min(
                connectorProjection,
                GetRendererBoundaryProjectionAtAcross(
                    sideWallRenderer,
                    profile.Across,
                    profile.Entrance.Outward,
                    targetAcrossProjection,
                    out var resolvedWallAcrossProjection));
            var horizontalPoint = connectorCorner +
                (profile.Entrance.Outward *
                 (horizontalProjection - connectorProjection)) +
                (profile.Across *
                 (resolvedAcrossProjection - targetAcrossProjection));
            var wallPoint = connectorCorner +
                (profile.Entrance.Outward *
                 (wallProjection - connectorProjection)) +
                (profile.Across *
                 (resolvedWallAcrossProjection - targetAcrossProjection));
            connectorCorner.y = surfaceY;
            horizontalPoint.y = surfaceY;
            wallPoint.y = surfaceY;
            var firstLength = Vector3.Distance(
                connectorCorner,
                horizontalPoint);
            var secondLength = Vector3.Distance(
                connectorCorner,
                wallPoint);
            var boundaryWidth = Vector3.Distance(horizontalPoint, wallPoint);
            if (firstLength <= 0.0001f &&
                secondLength <= 0.0001f &&
                boundaryWidth <= 0.0001f)
            {
                return;
            }

            var outerConnector = connectorCorner;
            var outerHorizontal = horizontalPoint;
            var outerWall = wallPoint;
            outerConnector.y = outerY;
            outerHorizontal.y = outerY;
            outerWall.y = outerY;
            var vertices = new List<Vector3>(36);
            var triangles = new List<int>(48);
            var uv = new List<Vector2>(36);
            AddDoubleSidedConnectorTriangle(
                vertices,
                triangles,
                uv,
                connectorCorner,
                horizontalPoint,
                wallPoint);
            AddDoubleSidedConnectorTriangle(
                vertices,
                triangles,
                uv,
                outerConnector,
                outerWall,
                outerHorizontal);
            AddDoubleSidedConnectorQuad(
                vertices,
                triangles,
                uv,
                connectorCorner,
                outerConnector,
                outerHorizontal,
                horizontalPoint);
            AddDoubleSidedConnectorQuad(
                vertices,
                triangles,
                uv,
                horizontalPoint,
                outerHorizontal,
                outerWall,
                wallPoint);
            AddDoubleSidedConnectorQuad(
                vertices,
                triangles,
                uv,
                wallPoint,
                outerWall,
                outerConnector,
                connectorCorner);
            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        private static void AddDoubleSidedConnectorTriangle(
            ICollection<Vector3> vertices,
            ICollection<int> triangles,
            ICollection<Vector2> uv,
            Vector3 first,
            Vector3 second,
            Vector3 third)
        {
            var baseIndex = vertices.Count;
            vertices.Add(first);
            vertices.Add(second);
            vertices.Add(third);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(0.5f, 1f));
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            baseIndex = vertices.Count;
            vertices.Add(first);
            vertices.Add(third);
            vertices.Add(second);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(0.5f, 1f));
            uv.Add(new Vector2(1f, 0f));
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
        }

        private static void IncludeBoundaryEdgeProjection(
            Vector3 first,
            Vector3 second,
            Vector3 across,
            Vector3 outward,
            float targetAcrossProjection,
            ref float maximumProjection)
        {
            const float projectionTolerance = 0.0001f;
            var firstAcross = Vector3.Dot(first, across);
            var secondAcross = Vector3.Dot(second, across);
            var minimumAcross = Mathf.Min(firstAcross, secondAcross) - projectionTolerance;
            var maximumAcross = Mathf.Max(firstAcross, secondAcross) + projectionTolerance;
            if (targetAcrossProjection < minimumAcross ||
                targetAcrossProjection > maximumAcross)
            {
                return;
            }

            if (Mathf.Abs(secondAcross - firstAcross) <= projectionTolerance)
            {
                maximumProjection = Mathf.Max(
                    maximumProjection,
                    Mathf.Max(
                        Vector3.Dot(first, outward),
                        Vector3.Dot(second, outward)));
                return;
            }

            var t = Mathf.Clamp01(
                (targetAcrossProjection - firstAcross) /
                (secondAcross - firstAcross));
            maximumProjection = Mathf.Max(
                maximumProjection,
                Vector3.Dot(Vector3.Lerp(first, second, t), outward));
        }

        private static void CreateConnectorEndGapFillers(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string endName,
            ConnectorSolidCorners floor,
            ConnectorSolidCorners leftWall,
            ConnectorSolidCorners rightWall,
            ConnectorSolidCorners ceiling,
            bool roomEnd,
            Material floorMaterial,
            Material ceilingMaterial)
        {
            var floorOuterLeft = roomEnd ? floor.RoomFirst : floor.CorridorFirst;
            var floorOuterRight = roomEnd ? floor.RoomSecond : floor.CorridorSecond;
            var floorInnerRight = roomEnd ? floor.RoomThird : floor.CorridorThird;
            var floorInnerLeft = roomEnd ? floor.RoomFourth : floor.CorridorFourth;
            var leftOuterBottom = roomEnd ? leftWall.RoomFirst : leftWall.CorridorFirst;
            var leftInnerBottom = roomEnd ? leftWall.RoomSecond : leftWall.CorridorSecond;
            var leftInnerTop = roomEnd ? leftWall.RoomThird : leftWall.CorridorThird;
            var leftOuterTop = roomEnd ? leftWall.RoomFourth : leftWall.CorridorFourth;
            var rightInnerBottom = roomEnd ? rightWall.RoomFirst : rightWall.CorridorFirst;
            var rightOuterBottom = roomEnd ? rightWall.RoomSecond : rightWall.CorridorSecond;
            var rightOuterTop = roomEnd ? rightWall.RoomThird : rightWall.CorridorThird;
            var rightInnerTop = roomEnd ? rightWall.RoomFourth : rightWall.CorridorFourth;
            var ceilingInnerLeft = roomEnd ? ceiling.RoomFirst : ceiling.CorridorFirst;
            var ceilingInnerRight = roomEnd ? ceiling.RoomSecond : ceiling.CorridorSecond;
            var ceilingOuterRight = roomEnd ? ceiling.RoomThird : ceiling.CorridorThird;
            var ceilingOuterLeft = roomEnd ? ceiling.RoomFourth : ceiling.CorridorFourth;

            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                endName + "FloorLeftGapFill",
                floorOuterLeft,
                leftOuterBottom,
                leftInnerBottom,
                floorInnerLeft,
                floorMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                endName + "FloorRightGapFill",
                floorInnerRight,
                rightInnerBottom,
                rightOuterBottom,
                floorOuterRight,
                floorMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                endName + "CeilingLeftGapFill",
                ceilingInnerLeft,
                leftInnerTop,
                leftOuterTop,
                ceilingOuterLeft,
                ceilingMaterial);
            CreateConnectorLongitudinalGapFiller(
                connector,
                definition,
                endName + "CeilingRightGapFill",
                ceilingOuterRight,
                rightOuterTop,
                rightInnerTop,
                ceilingInnerRight,
                ceilingMaterial);
        }

        private static void CreateConnectorLongitudinalGapFiller(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            Material material)
        {
            if (material == null)
            {
                throw new InvalidOperationException(
                    definition.Id + " " + partName + " must reuse its source material.");
            }

            const int longitudinalSegments = 16;
            const int crossSegments = 6;
            var vertices = new List<Vector3>(
                longitudinalSegments * crossSegments * 8);
            var triangles = new List<int>(
                longitudinalSegments * crossSegments * 12);
            var uv = new List<Vector2>(vertices.Capacity);
            AddDoubleSidedConnectorBoundaryPatch(
                vertices,
                triangles,
                uv,
                first,
                second,
                third,
                fourth,
                longitudinalSegments,
                crossSegments);
            var mesh = new Mesh
            {
                name = "Mesh_" + definition.Id + "_" + partName
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(
                mesh,
                TriRoomConnectorSampleMeshDirectory + "/" + mesh.name + ".asset");
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }

        private static void AddDoubleSidedConnectorBoundaryPatch(
            ICollection<Vector3> vertices,
            ICollection<int> triangles,
            ICollection<Vector2> uv,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            int longitudinalSegments,
            int crossSegments)
        {
            for (var longitudinalIndex = 0;
                 longitudinalIndex < longitudinalSegments;
                 longitudinalIndex++)
            {
                var startLongitudinal =
                    longitudinalIndex / (float)longitudinalSegments;
                var endLongitudinal =
                    (longitudinalIndex + 1) / (float)longitudinalSegments;
                for (var crossIndex = 0;
                     crossIndex < crossSegments;
                     crossIndex++)
                {
                    var startCross = crossIndex / (float)crossSegments;
                    var endCross = (crossIndex + 1) / (float)crossSegments;
                    AddDoubleSidedConnectorQuad(
                        vertices,
                        triangles,
                        uv,
                        EvaluateConnectorBoundaryPatch(
                            first,
                            second,
                            third,
                            fourth,
                            startLongitudinal,
                            startCross),
                        EvaluateConnectorBoundaryPatch(
                            first,
                            second,
                            third,
                            fourth,
                            startLongitudinal,
                            endCross),
                        EvaluateConnectorBoundaryPatch(
                            first,
                            second,
                            third,
                            fourth,
                            endLongitudinal,
                            endCross),
                        EvaluateConnectorBoundaryPatch(
                            first,
                            second,
                            third,
                            fourth,
                            endLongitudinal,
                            startCross));
                }
            }
        }

        private static Vector3 EvaluateConnectorBoundaryPatch(
            Vector3 roomFirst,
            Vector3 roomSecond,
            Vector3 corridorSecond,
            Vector3 corridorFirst,
            float longitudinal,
            float cross)
        {
            var roomEdge = Vector3.Lerp(roomFirst, roomSecond, cross);
            var corridorEdge = Vector3.Lerp(corridorFirst, corridorSecond, cross);
            var easedLongitudinal = longitudinal * longitudinal *
                (3f - (2f * longitudinal));
            return Vector3.Lerp(roomEdge, corridorEdge, easedLongitudinal);
        }

        private static void AddDoubleSidedConnectorQuad(
            ICollection<Vector3> vertices,
            ICollection<int> triangles,
            ICollection<Vector2> uv,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth)
        {
            var baseIndex = vertices.Count;
            vertices.Add(first);
            vertices.Add(second);
            vertices.Add(third);
            vertices.Add(fourth);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(0f, 1f));
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);

            baseIndex = vertices.Count;
            vertices.Add(first);
            vertices.Add(fourth);
            vertices.Add(third);
            vertices.Add(second);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(1f, 0f));
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }

        private static ConnectorGapShell CalculateConnectorGapShell(
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            Renderer floor,
            Renderer minimumWall,
            Renderer maximumWall,
            Renderer ceiling)
        {
            var floorLocalBounds = CalculateVisibleBoundsInLocalSpace(
                floor.gameObject,
                module);
            var minimumWallLocalBounds = CalculateVisibleBoundsInLocalSpace(
                minimumWall.gameObject,
                module);
            var maximumWallLocalBounds = CalculateVisibleBoundsInLocalSpace(
                maximumWall.gameObject,
                module);
            var localX = definition.LowEnd
                ? floorLocalBounds.min.x
                : floorLocalBounds.max.x;
            var corridorMinimumZ = minimumWallLocalBounds.max.z;
            var corridorMaximumZ = maximumWallLocalBounds.min.z;
            var roomMinimumProjection = profile.MinimumWallMaximum;
            var roomMaximumProjection = profile.MaximumWallMinimum;
            var corridorOuterMinimumZ = Mathf.Min(
                minimumWallLocalBounds.min.z,
                corridorMinimumZ);
            var corridorOuterMaximumZ = Mathf.Max(
                maximumWallLocalBounds.max.z,
                corridorMaximumZ);
            if (roomMinimumProjection >= roomMaximumProjection)
            {
                roomMinimumProjection = profile.OpeningMinimum;
                roomMaximumProjection = profile.OpeningMaximum;
            }

            if (corridorMinimumZ >= corridorMaximumZ)
            {
                corridorMinimumZ = floorLocalBounds.min.z;
                corridorMaximumZ = floorLocalBounds.max.z;
            }

            corridorOuterMinimumZ = Mathf.Min(
                corridorOuterMinimumZ,
                corridorMinimumZ);
            corridorOuterMaximumZ = Mathf.Max(
                corridorOuterMaximumZ,
                corridorMaximumZ);
            var roomOuterMinimumProjection = roomMinimumProjection - Mathf.Max(
                corridorMinimumZ - corridorOuterMinimumZ,
                0.08f);
            var roomOuterMaximumProjection = roomMaximumProjection + Mathf.Max(
                corridorOuterMaximumZ - corridorMaximumZ,
                0.08f);

            if (roomMinimumProjection >= roomMaximumProjection ||
                corridorMinimumZ >= corridorMaximumZ)
            {
                throw new InvalidOperationException(
                    definition.Id + " has an invalid open gap cross-section.");
            }

            // Use the common plane shared by the room floor, walls, and ceiling.
            // The entrance anchor is only a routing point and can sit outside the
            // actual modeled doorway boundary on angled or circular rooms.
            var roomPlaneCenter = profile.PlaneCenter;
            roomPlaneCenter.y = 0f;
            var roomPlaneAcrossProjection = Vector3.Dot(
                roomPlaneCenter,
                profile.Across);
            var roomMinimum = roomPlaneCenter +
                (profile.Across *
                 (roomMinimumProjection - roomPlaneAcrossProjection));
            var roomMaximum = roomPlaneCenter +
                (profile.Across *
                 (roomMaximumProjection - roomPlaneAcrossProjection));
            var roomOuterMinimum = roomPlaneCenter +
                (profile.Across *
                 (roomOuterMinimumProjection - roomPlaneAcrossProjection));
            var roomOuterMaximum = roomPlaneCenter +
                (profile.Across *
                 (roomOuterMaximumProjection - roomPlaneAcrossProjection));
            var corridorMinimum = module.TransformPoint(new Vector3(
                localX,
                0f,
                corridorMinimumZ));
            var corridorMaximum = module.TransformPoint(new Vector3(
                localX,
                0f,
                corridorMaximumZ));
            var corridorOuterMinimum = module.TransformPoint(new Vector3(
                localX,
                0f,
                corridorOuterMinimumZ));
            var corridorOuterMaximum = module.TransformPoint(new Vector3(
                localX,
                0f,
                corridorOuterMaximumZ));
            roomMinimum.y = 0f;
            roomMaximum.y = 0f;
            roomOuterMinimum.y = 0f;
            roomOuterMaximum.y = 0f;
            corridorMinimum.y = 0f;
            corridorMaximum.y = 0f;
            corridorOuterMinimum.y = 0f;
            corridorOuterMaximum.y = 0f;
            if (Vector3.Dot(
                    roomMaximum - roomMinimum,
                    corridorMaximum - corridorMinimum) < 0f)
            {
                var swap = corridorMinimum;
                corridorMinimum = corridorMaximum;
                corridorMaximum = swap;
                swap = corridorOuterMinimum;
                corridorOuterMinimum = corridorOuterMaximum;
                corridorOuterMaximum = swap;
            }

            var roomFloorY = profile.Entrance.FloorY;
            var roomCeilingY = profile.Ceiling.bounds.min.y;
            var corridorFloorY = floor.bounds.max.y;
            var corridorCeilingY = ceiling.bounds.min.y;
            var corridorOuterFloorY = Mathf.Min(floor.bounds.min.y, corridorFloorY);
            var corridorOuterCeilingY = Mathf.Max(
                ceiling.bounds.max.y,
                corridorCeilingY);
            var roomOuterFloorY = roomFloorY - Mathf.Max(
                corridorFloorY - corridorOuterFloorY,
                0.08f);
            var roomOuterCeilingY = roomCeilingY + Mathf.Max(
                corridorOuterCeilingY - corridorCeilingY,
                0.08f);
            if (roomCeilingY <= roomFloorY || corridorCeilingY <= corridorFloorY)
            {
                throw new InvalidOperationException(
                    definition.Id + " has an invalid open gap height.");
            }

            return new ConnectorGapShell(
                new Vector3(
                    roomOuterMinimum.x,
                    roomOuterFloorY,
                    roomOuterMinimum.z),
                new Vector3(
                    roomOuterMaximum.x,
                    roomOuterFloorY,
                    roomOuterMaximum.z),
                new Vector3(roomMinimum.x, roomFloorY, roomMinimum.z),
                new Vector3(roomMaximum.x, roomFloorY, roomMaximum.z),
                new Vector3(roomMinimum.x, roomCeilingY, roomMinimum.z),
                new Vector3(roomMaximum.x, roomCeilingY, roomMaximum.z),
                new Vector3(
                    roomOuterMinimum.x,
                    roomOuterCeilingY,
                    roomOuterMinimum.z),
                new Vector3(
                    roomOuterMaximum.x,
                    roomOuterCeilingY,
                    roomOuterMaximum.z),
                new Vector3(
                    corridorOuterMinimum.x,
                    corridorOuterFloorY,
                    corridorOuterMinimum.z),
                new Vector3(
                    corridorOuterMaximum.x,
                    corridorOuterFloorY,
                    corridorOuterMaximum.z),
                new Vector3(corridorMinimum.x, corridorFloorY, corridorMinimum.z),
                new Vector3(corridorMaximum.x, corridorFloorY, corridorMaximum.z),
                new Vector3(corridorMinimum.x, corridorCeilingY, corridorMinimum.z),
                new Vector3(corridorMaximum.x, corridorCeilingY, corridorMaximum.z),
                new Vector3(
                    corridorOuterMinimum.x,
                    corridorOuterCeilingY,
                    corridorOuterMinimum.z),
                new Vector3(
                    corridorOuterMaximum.x,
                    corridorOuterCeilingY,
                    corridorOuterMaximum.z));
        }

        private static ConnectorSurfaceCorners GetConnectorGapSurface(
            ConnectorGapShell shell,
            string partName)
        {
            switch (partName)
            {
                case "Floor":
                    return new ConnectorSurfaceCorners(
                        shell.RoomFloorMinimum,
                        shell.RoomFloorMaximum,
                        shell.CorridorFloorMaximum,
                        shell.CorridorFloorMinimum);
                case "LeftWall":
                    return new ConnectorSurfaceCorners(
                        shell.RoomFloorMinimum,
                        shell.CorridorFloorMinimum,
                        shell.CorridorCeilingMinimum,
                        shell.RoomCeilingMinimum);
                case "RightWall":
                    return new ConnectorSurfaceCorners(
                        shell.RoomFloorMaximum,
                        shell.RoomCeilingMaximum,
                        shell.CorridorCeilingMaximum,
                        shell.CorridorFloorMaximum);
                case "Ceiling":
                    return new ConnectorSurfaceCorners(
                        shell.RoomCeilingMinimum,
                        shell.RoomCeilingMaximum,
                        shell.CorridorCeilingMaximum,
                        shell.CorridorCeilingMinimum);
                default:
                    throw new InvalidOperationException(
                        "Unsupported connector surface " + partName);
            }
        }

        private static void CreateConnectorSurfacePart(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            ConnectorSurfaceCorners corners,
            Material material)
        {
            if (material == null)
            {
                throw new InvalidOperationException(
                    definition.Id + " " + partName + " must reuse its source material.");
            }

            var mesh = CreateDoubleSidedConnectorSurfaceMesh(
                "Mesh_" + definition.Id + "_" + partName,
                corners);
            var meshPath = TriRoomConnectorSampleMeshDirectory + "/" +
                mesh.name + ".asset";
            AssetDatabase.CreateAsset(mesh, meshPath);
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static Mesh CreateDoubleSidedConnectorSurfaceMesh(
            string meshName,
            ConnectorSurfaceCorners corners)
        {
            var vertices = new[]
            {
                corners.First,
                corners.Second,
                corners.Third,
                corners.Fourth,
                corners.First,
                corners.Second,
                corners.Third,
                corners.Fourth
            };
            var triangles = new[]
            {
                0, 1, 2,
                0, 2, 3,
                4, 6, 5,
                4, 7, 6
            };
            var uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            var mesh = new Mesh { name = meshName };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateConnectorPart(
            Transform connector,
            ConnectorEndpointDefinition definition,
            string partName,
            Transform module,
            ConnectorEntranceProfile profile,
            Bounds sourceLocalBounds,
            float roomBottomY,
            float roomTopY,
            float corridorBottomY,
            float corridorTopY,
            Material material)
        {
            if (material == null)
            {
                throw new InvalidOperationException(
                    definition.Id + " " + partName + " must reuse its source material.");
            }

            var corners = CalculateAdditiveConnectorPartCorners(
                module,
                profile,
                definition.LowEnd,
                sourceLocalBounds,
                partName,
                roomBottomY,
                roomTopY,
                corridorBottomY,
                corridorTopY);
            var mesh = CreateConnectorPrismMesh(
                "Mesh_" + definition.Id + "_" + partName,
                corners);
            var meshPath = TriRoomConnectorSampleMeshDirectory + "/" +
                mesh.name + ".asset";
            AssetDatabase.CreateAsset(mesh, meshPath);
            var part = new GameObject(partName);
            part.transform.SetParent(connector, false);
            var meshFilter = part.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            var meshCollider = part.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        private static ConnectorPrismCorners CalculateAdditiveConnectorPartCorners(
            Transform module,
            ConnectorEntranceProfile profile,
            bool lowEnd,
            Bounds sourceLocalBounds,
            string partName,
            float roomBottomY,
            float roomTopY,
            float corridorBottomY,
            float corridorTopY)
        {
            var localX = lowEnd
                ? sourceLocalBounds.min.x
                : sourceLocalBounds.max.x;
            var localMinimumZ = sourceLocalBounds.min.z;
            var localMaximumZ = sourceLocalBounds.max.z;
            var corridorMin = module.TransformPoint(new Vector3(
                localX,
                sourceLocalBounds.center.y,
                localMinimumZ));
            var corridorMax = module.TransformPoint(new Vector3(
                localX,
                sourceLocalBounds.center.y,
                localMaximumZ));
            GetConnectorRoomProjectionRange(
                profile,
                partName,
                out var roomMinimumProjection,
                out var roomMaximumProjection);
            var roomBoundaryRenderer = GetConnectorRoomBoundaryRenderer(profile, partName);
            var roomPlaneProjection = GetRendererProjectionMaximum(
                roomBoundaryRenderer,
                profile.Entrance.Outward);
            var roomPlaneCenter = profile.Entrance.Point;
            roomPlaneCenter += profile.Entrance.Outward *
                (roomPlaneProjection -
                 Vector3.Dot(roomPlaneCenter, profile.Entrance.Outward));
            var roomPlaneAcrossProjection = Vector3.Dot(roomPlaneCenter, profile.Across);
            var roomMin = roomPlaneCenter +
                (profile.Across * (roomMinimumProjection - roomPlaneAcrossProjection));
            var roomMax = roomPlaneCenter +
                (profile.Across * (roomMaximumProjection - roomPlaneAcrossProjection));
            roomMin.y = 0f;
            roomMax.y = 0f;
            corridorMin.y = 0f;
            corridorMax.y = 0f;
            return new ConnectorPrismCorners(
                roomMin,
                roomMax,
                corridorMax,
                corridorMin,
                roomBottomY,
                roomTopY,
                corridorBottomY,
                corridorTopY);
        }

        private static void OrderCorridorWalls(
            Transform module,
            Renderer first,
            Renderer second,
            out Renderer minimumWall,
            out Renderer maximumWall)
        {
            var firstBounds = CalculateVisibleBoundsInLocalSpace(first.gameObject, module);
            var secondBounds = CalculateVisibleBoundsInLocalSpace(second.gameObject, module);
            if (firstBounds.center.z <= secondBounds.center.z)
            {
                minimumWall = first;
                maximumWall = second;
            }
            else
            {
                minimumWall = second;
                maximumWall = first;
            }
        }

        private static Renderer GetConnectorRoomBoundaryRenderer(
            ConnectorEntranceProfile profile,
            string partName)
        {
            switch (partName)
            {
                case "Floor":
                    return profile.Floor;
                case "LeftWall":
                    return profile.MinimumWall;
                case "RightWall":
                    return profile.MaximumWall;
                case "Ceiling":
                    return profile.Ceiling;
                default:
                    throw new InvalidOperationException(
                        "Unsupported connector part " + partName);
            }
        }

        private static void GetConnectorRoomProjectionRange(
            ConnectorEntranceProfile profile,
            string partName,
            out float minimum,
            out float maximum)
        {
            switch (partName)
            {
                case "LeftWall":
                    minimum = profile.MinimumWallMinimum;
                    maximum = profile.MinimumWallMaximum;
                    return;
                case "RightWall":
                    minimum = profile.MaximumWallMinimum;
                    maximum = profile.MaximumWallMaximum;
                    return;
                case "Floor":
                case "Ceiling":
                    minimum = profile.OpeningMinimum;
                    maximum = profile.OpeningMaximum;
                    return;
                default:
                    throw new InvalidOperationException(
                        "Unsupported connector part " + partName);
            }
        }

        private static Mesh CreateConnectorPrismMesh(
            string meshName,
            ConnectorPrismCorners corners)
        {
            var footprint = new[]
            {
                corners.RoomMin,
                corners.RoomMax,
                corners.CorridorMax,
                corners.CorridorMin
            };
            var topY = new[]
            {
                corners.RoomTopY,
                corners.RoomTopY,
                corners.CorridorTopY,
                corners.CorridorTopY
            };
            var bottomY = new[]
            {
                corners.RoomBottomY,
                corners.RoomBottomY,
                corners.CorridorBottomY,
                corners.CorridorBottomY
            };
            if (Vector3.Cross(footprint[1] - footprint[0], footprint[2] - footprint[0]).y < 0f)
            {
                var swap = footprint[1];
                footprint[1] = footprint[3];
                footprint[3] = swap;
                var topSwap = topY[1];
                topY[1] = topY[3];
                topY[3] = topSwap;
                var bottomSwap = bottomY[1];
                bottomY[1] = bottomY[3];
                bottomY[3] = bottomSwap;
            }

            var vertices = new Vector3[8];
            var uv = new Vector2[8];
            for (var index = 0; index < 4; index++)
            {
                vertices[index] = new Vector3(
                    footprint[index].x,
                    topY[index],
                    footprint[index].z);
                vertices[index + 4] = new Vector3(
                    footprint[index].x,
                    bottomY[index],
                    footprint[index].z);
                uv[index] = new Vector2(footprint[index].x, footprint[index].z);
                uv[index + 4] = uv[index];
            }

            var triangles = new List<int>(36)
            {
                0, 1, 2, 0, 2, 3,
                4, 6, 5, 4, 7, 6
            };
            for (var side = 0; side < 4; side++)
            {
                var next = (side + 1) % 4;
                triangles.Add(side);
                triangles.Add(side + 4);
                triangles.Add(next + 4);
                triangles.Add(side);
                triangles.Add(next + 4);
                triangles.Add(next);
            }

            var mesh = new Mesh { name = meshName };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Renderer RequireCorridorSurfaceRenderer(
            Transform module,
            string requiredSuffix)
        {
            Renderer match = null;
            var renderers = module.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (!renderer.name.EndsWith(requiredSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "Duplicate corridor surface " + requiredSuffix + " below " + module.name);
                }

                match = renderer;
            }

            return match ?? throw new InvalidOperationException(
                "Missing corridor surface " + requiredSuffix + " below " + module.name);
        }

        private static Renderer RequireSingleRenderer(Transform root)
        {
            Renderer match = null;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                if (!renderers[rendererIndex].enabled)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "Expected one visible renderer below " + root.name);
                }

                match = renderers[rendererIndex];
            }

            return match ?? throw new InvalidOperationException(
                "Missing visible renderer below " + root.name);
        }

        private static string RequireMaterialAssetPath(Renderer renderer)
        {
            var material = renderer.sharedMaterial;
            if (material == null)
            {
                throw new InvalidOperationException(
                    "Renderer does not reference a source material: " + renderer.name);
            }

            var path = AssetDatabase.GetAssetPath(material);
            return string.IsNullOrEmpty(path)
                ? "SceneEmbedded:" + BuildMaterialReuseSignature(material)
                : path;
        }

        private static string RequireMaterialAssetPath(Material material)
        {
            if (material == null)
            {
                throw new InvalidOperationException(
                    "Connector submesh does not reference a source material.");
            }

            var path = AssetDatabase.GetAssetPath(material);
            return string.IsNullOrEmpty(path)
                ? "SceneEmbedded:" + BuildMaterialReuseSignature(material)
                : path;
        }

        private static string BuildMaterialReuseSignature(Material material)
        {
            if (material == null)
            {
                return "<null>";
            }

            var keywords = material.shaderKeywords ?? Array.Empty<string>();
            Array.Sort(keywords, StringComparer.Ordinal);
            return material.name +
                "|Shader=" + (material.shader == null ? "<null>" : material.shader.name) +
                "|RenderQueue=" + material.renderQueue +
                "|Keywords=" + string.Join(",", keywords) +
                "|Color=" + material.color;
        }

        private static string BuildTriRoomConnectorProtectedState(
            IReadOnlyDictionary<string, GameObject> rooms,
            GameObject corridorRoot,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot)
        {
            return BuildLayoutPlacementSignature(rooms, corridorRoot) +
                "TARGET_SOURCE_GEOMETRY\n" +
                BuildTriRoomConnectorSourceGeometrySignature(
                    rooms,
                    modules,
                    ceilingRoot);
        }

        private static string BuildTriRoomConnectorSourceGeometrySignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot)
        {
            var builder = new StringBuilder(64 * 1024);
            foreach (var roomId in new[] { "EngineRoom", "Cockpit", "ControlRoom" })
            {
                builder.Append("ROOM|").Append(roomId).AppendLine();
                AppendTriRoomConnectorHierarchySignature(
                    builder,
                    rooms[roomId].transform);
            }

            foreach (var corridorId in new[] { "SC-H01", "SC-H02", "SC-H05" })
            {
                var module = modules[corridorId];
                builder.Append("CORRIDOR|").Append(corridorId).AppendLine();
                AppendTriRoomConnectorHierarchySignature(builder, module);
                var ceilingFollowers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                for (var followerIndex = 0;
                     followerIndex < ceilingFollowers.Count;
                     followerIndex++)
                {
                    builder.Append("CORRIDOR_CEILING|")
                        .Append(corridorId)
                        .Append('|')
                        .Append(followerIndex)
                        .AppendLine();
                    AppendTriRoomConnectorHierarchySignature(
                        builder,
                        ceilingFollowers[followerIndex].Transform);
                }
            }

            return builder.ToString();
        }

        private static void AppendTriRoomConnectorHierarchySignature(
            StringBuilder builder,
            Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            Array.Sort(
                transforms,
                (left, right) => string.CompareOrdinal(
                    GetHierarchyPath(root, left),
                    GetHierarchyPath(root, right)));
            for (var transformIndex = 0;
                 transformIndex < transforms.Length;
                 transformIndex++)
            {
                var target = transforms[transformIndex];
                builder.Append(GetHierarchyPath(root, target)).Append('|')
                    .Append(target.gameObject.activeSelf).Append('|')
                    .Append(Vector(target.localPosition)).Append('|')
                    .Append(QuaternionText(target.localRotation)).Append('|')
                    .Append(Vector(target.localScale));
                var meshFilter = target.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var mesh = meshFilter.sharedMesh;
                    builder.Append("|Mesh=")
                        .Append(AssetDatabase.GetAssetPath(mesh)).Append('|')
                        .Append(mesh.name).Append('|')
                        .Append(mesh.vertexCount).Append('|')
                        .Append(mesh.subMeshCount).Append('|')
                        .Append(BoundsText(mesh.bounds));
                }

                var renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    builder.Append("|RendererEnabled=").Append(renderer.enabled);
                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0;
                         materialIndex < materials.Length;
                         materialIndex++)
                    {
                        builder.Append("|Material=")
                            .Append(AssetDatabase.GetAssetPath(materials[materialIndex]))
                            .Append('|')
                            .Append(BuildMaterialReuseSignature(materials[materialIndex]));
                    }
                }

                builder.AppendLine();
            }
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            var segments = assetFolder.Split('/');
            var current = segments[0];
            for (var segmentIndex = 1; segmentIndex < segments.Length; segmentIndex++)
            {
                var next = current + "/" + segments[segmentIndex];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[segmentIndex]);
                }

                current = next;
            }
        }

        private static void DeleteProjectFileIfPresent(string directory, string fileName)
        {
            var path = Path.Combine(
                ProjectRoot,
                directory.Replace('/', Path.DirectorySeparatorChar),
                fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void WriteTriRoomConnectorSampleReviewHtml()
        {
            var directory = Path.Combine(
                ProjectRoot,
                TriRoomConnectorSampleArtDirectory.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            Directory.CreateDirectory(directory);
            var html = "<!doctype html>\n" +
                "<html lang=\"ko\"><head><meta charset=\"utf-8\">" +
                "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                "<title>Pegasus 3개 공간실 중간 연결 아트 샘플</title>" +
                "<style>body{margin:0;background:#0c1118;color:#eaf3ff;font-family:Arial,'Malgun Gothic',sans-serif}" +
                "main{max-width:1500px;margin:auto;padding:28px}h1{margin:0 0 8px;font-size:30px}" +
                ".status{color:#ffd166;margin-bottom:18px}.card{background:#151e29;border:1px solid #2e4258;border-radius:12px;padding:18px;margin:14px 0}" +
                "img{display:block;width:100%;height:auto;border-radius:8px;background:#05070a}" +
                "ul{line-height:1.75}code{color:#9ed8ff}</style></head><body><main>" +
                "<h1>Pegasus 3개 공간실 중간 연결 아트 샘플</h1>" +
                "<div class=\"status\">승인 대기 · 원본 Pegasus 씬 미적용</div>" +
                "<section class=\"card\"><img src=\"Final.png\" alt=\"전체 배치와 여섯 연결부 직접 검토 시트\"></section>" +
                "<section class=\"card\"><h2>대상 연결부</h2><ul>" +
                "<li>동력실–H01 / 조종실–H01</li><li>조종실–H02 / 통제실–H02</li>" +
                "<li>동력실–H05 / 통제실–H05</li></ul></section>" +
                "<section class=\"card\"><h2>적용 원칙</h2><ul>" +
                "<li>기존 공간실·복도 모델의 메시·Transform·활성 상태를 변경하지 않음</li>" +
                "<li>기존 복도 머티리얼만 참조하며 신규 머티리얼·텍스처·문을 만들지 않음</li>" +
                "<li>각 접점은 중앙 통행구를 비운 단일 곡면 셸 메시로 생성함</li>" +
                "<li>내부 바닥·좌우 벽·천장과 외부 셸은 공통 경계를 사용하며 임시 덧댐·쐐기·앞치마 메시를 만들지 않음</li>" +
                "<li>양 끝 가장자리 띠만 원본 경계에 맞닿고 중앙 통행구를 가로지르는 마개 면은 생성하지 않음</li>" +
                "<li>추가 메시가 원본 구조를 변형하거나 통행구를 가리지 않음</li>" +
                "<li>Unity 샘플 씬: <code>Assets/_Project/ArtSamples/PegasusTriRoomConnectors/PegasusTriRoomConnectors.unity</code></li>" +
                "</ul></section></main></body></html>\n";
            File.WriteAllText(
                Path.Combine(directory, "Review.html"),
                html,
                new UTF8Encoding(false));
        }

        private static void CaptureTriRoomConnectorSampleSheet(
            Scene sampleScene,
            GameObject sampleRoot,
            string outputPath)
        {
            var connectors = sampleRoot.transform.Find("Intermediate Connectors") ??
                throw new InvalidOperationException(
                    "Connector sample hierarchy is missing Intermediate Connectors.");
            if (connectors.childCount != TriRoomConnectorEndpoints.Length)
            {
                throw new InvalidOperationException(
                    "Connector capture requires exactly six connector roots.");
            }

            const int panelWidth = 900;
            const int panelHeight = 600;
            const int gutter = 12;
            const int columns = 2;
            const int rows = 4;
            var sheet = new Texture2D(
                (panelWidth * columns) + (gutter * (columns + 1)),
                (panelHeight * rows) + (gutter * (rows + 1)),
                TextureFormat.RGB24,
                false);
            var backgroundPixels = new Color[sheet.width * sheet.height];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = new Color(0.018f, 0.022f, 0.028f, 1f);
            }

            sheet.SetPixels(backgroundPixels);
            var cameraObject = new GameObject("__ConnectorSampleReviewCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, sampleScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var lightObject = new GameObject("__ConnectorSampleReviewLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, sampleScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.9f, 0.95f, 1f, 1f);
            light.intensity = 1.45f;
            light.shadows = LightShadows.None;
            var originalAmbient = RenderSettings.ambientLight;
            var panels = new List<Texture2D>(8);
            var sampleRooms = RequireTriRoomConnectorRooms(sampleScene);
            var sampleCorridorRoot = RequireSceneObject(sampleScene, CorridorRootName);
            var sampleModules = RequireTriRoomConnectorModules(sampleCorridorRoot.transform);
            try
            {
                RenderSettings.ambientLight = new Color(0.38f, 0.42f, 0.48f, 1f);
                light.transform.rotation = Quaternion.Euler(52f, -34f, 0f);
                var allBounds = CalculateVisibleBounds(sampleRoot);
                panels.Add(RenderConnectorBoundsPanel(
                    camera,
                    allBounds,
                    panelWidth,
                    panelHeight,
                    true,
                    new Vector3(0f, 1f, 0f)));
                panels.Add(RenderConnectorBoundsPanel(
                    camera,
                    allBounds,
                    panelWidth,
                    panelHeight,
                    false,
                    new Vector3(-0.85f, 0.72f, -0.9f)));
                camera.backgroundColor = new Color(0.72f, 0.04f, 0.48f, 1f);
                for (var connectorIndex = 0;
                     connectorIndex < connectors.childCount;
                     connectorIndex++)
                {
                    var definition = TriRoomConnectorEndpoints[connectorIndex];
                    var module = sampleModules[definition.CorridorId];
                    var connector = connectors.Find("Connector_" + definition.Id) ??
                        throw new InvalidOperationException(
                            "Connector capture is missing Connector_" + definition.Id);
                    var profile = BuildConnectorEntranceProfile(
                        sampleScene,
                        sampleRooms,
                        module,
                        definition);
                    var floor = RequireUniqueCorridorFloorRenderer(module);
                    var namedLeftWall = RequireCorridorSurfaceRenderer(
                        module,
                        " left armored wall");
                    var namedRightWall = RequireCorridorSurfaceRenderer(
                        module,
                        " right armored wall");
                    var ceiling = RequireSingleRenderer(
                        CaptureCorridorCeilingFollowers(
                            module,
                            sampleCorridorRoot.transform.Find(CorridorCeilingRootName) ??
                            throw new InvalidOperationException(
                                "Connector sample corridor ceiling root is missing."))[0].Transform);
                    OrderCorridorWalls(
                        module,
                        namedLeftWall,
                        namedRightWall,
                        out var minimumWall,
                        out var maximumWall);
                    var shell = CalculateConnectorGapShell(
                        definition,
                        module,
                        profile,
                        floor,
                        minimumWall,
                        maximumWall,
                        ceiling);
                    var floorSolid = AlignConnectorGapSolidToSources(
                        GetConnectorGapSolid(shell, "Floor"),
                        "Floor",
                        definition,
                        module,
                        profile,
                        profile.Floor,
                        floor);
                    var roomFloorMinimum = floorSolid.RoomFourth;
                    var roomFloorMaximum = floorSolid.RoomThird;
                    var corridorFloorMinimum = floorSolid.CorridorFourth;
                    var corridorFloorMaximum = floorSolid.CorridorThird;
                    roomFloorMinimum.y = 0f;
                    roomFloorMaximum.y = 0f;
                    corridorFloorMinimum.y = 0f;
                    corridorFloorMaximum.y = 0f;
                    var floorCorners = new ConnectorPrismCorners(
                        roomFloorMinimum,
                        roomFloorMaximum,
                        corridorFloorMaximum,
                        corridorFloorMinimum,
                        floorSolid.RoomFirst.y,
                        floorSolid.RoomThird.y,
                        floorSolid.CorridorFirst.y,
                        floorSolid.CorridorThird.y);
                    panels.Add(RenderConnectorEndpointReviewPanel(
                        camera,
                        connector.gameObject,
                        floorCorners,
                        shell.RoomCeilingMinimum.y,
                        shell.CorridorCeilingMinimum.y,
                        panelWidth,
                        panelHeight));
                }

                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    var column = panelIndex % columns;
                    var rowFromTop = panelIndex / columns;
                    var x = gutter + (column * (panelWidth + gutter));
                    var y = gutter + ((rows - rowFromTop - 1) * (panelHeight + gutter));
                    sheet.SetPixels(x, y, panelWidth, panelHeight, panels[panelIndex].GetPixels());
                }

                var detailDirectory = Path.Combine(
                    Path.GetDirectoryName(outputPath) ?? string.Empty,
                    "EndpointDetails");
                Directory.CreateDirectory(detailDirectory);
                for (var endpointIndex = 0;
                     endpointIndex < TriRoomConnectorEndpoints.Length;
                     endpointIndex++)
                {
                    File.WriteAllBytes(
                        Path.Combine(
                            detailDirectory,
                            TriRoomConnectorEndpoints[endpointIndex].Id + ".png"),
                        panels[endpointIndex + 2].EncodeToPNG());
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                RenderSettings.ambientLight = originalAmbient;
                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    UnityEngine.Object.DestroyImmediate(panels[panelIndex]);
                }

                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static Texture2D RenderConnectorEndpointReviewPanel(
            Camera camera,
            GameObject connector,
            ConnectorPrismCorners floorCorners,
            float roomCeilingBottom,
            float corridorCeilingBottom,
            int width,
            int height)
        {
            var halfWidth = width / 2;
            var halfHeight = height / 2;
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var background = new Color[width * height];
            for (var pixelIndex = 0; pixelIndex < background.Length; pixelIndex++)
            {
                background[pixelIndex] = new Color(0.018f, 0.022f, 0.028f, 1f);
            }

            panel.SetPixels(background);
            var roomCenter = (floorCorners.RoomMin + floorCorners.RoomMax) * 0.5f;
            var corridorCenter =
                (floorCorners.CorridorMin + floorCorners.CorridorMax) * 0.5f;
            var towardRoom = HorizontalDirection(roomCenter - corridorCenter);
            var across = HorizontalDirection(Vector3.Cross(Vector3.up, towardRoom));
            var views = new[]
            {
                RenderConnectorRoomSeamExteriorPanel(
                    camera,
                    connector.name,
                    floorCorners,
                    roomCeilingBottom,
                    towardRoom,
                    across,
                    1f,
                    halfWidth,
                    halfHeight),
                RenderConnectorRoomSeamExteriorPanel(
                    camera,
                    connector.name,
                    floorCorners,
                    roomCeilingBottom,
                    towardRoom,
                    across,
                    -1f,
                    halfWidth,
                    halfHeight),
                RenderConnectorInteriorPanel(
                    camera,
                    floorCorners,
                    roomCeilingBottom,
                    corridorCeilingBottom,
                    true,
                    halfWidth,
                    halfHeight),
                RenderConnectorInteriorPanel(
                    camera,
                    floorCorners,
                    roomCeilingBottom,
                    corridorCeilingBottom,
                    false,
                    halfWidth,
                    halfHeight)
            };
            try
            {
                panel.SetPixels(0, halfHeight, halfWidth, halfHeight, views[0].GetPixels());
                panel.SetPixels(
                    halfWidth,
                    halfHeight,
                    halfWidth,
                    halfHeight,
                    views[1].GetPixels());
                panel.SetPixels(0, 0, halfWidth, halfHeight, views[2].GetPixels());
                panel.SetPixels(
                    halfWidth,
                    0,
                    halfWidth,
                    halfHeight,
                    views[3].GetPixels());
                panel.Apply(false, false);
                return panel;
            }
            finally
            {
                for (var viewIndex = 0; viewIndex < views.Length; viewIndex++)
                {
                    UnityEngine.Object.DestroyImmediate(views[viewIndex]);
                }
            }
        }

        private static Texture2D RenderConnectorRoomSeamExteriorPanel(
            Camera camera,
            string connectorName,
            ConnectorPrismCorners floorCorners,
            float roomCeilingBottom,
            Vector3 towardRoom,
            Vector3 across,
            float side,
            int width,
            int height)
        {
            camera.orthographic = false;
            camera.fieldOfView = 46f;
            var openingHeight = roomCeilingBottom - floorCorners.RoomTopY;
            var extent = Mathf.Max(0.8f, openingHeight * 0.62f);
            var roomCorner = side > 0f
                ? floorCorners.RoomMin
                : floorCorners.RoomMax;
            var target = new Vector3(
                roomCorner.x,
                Mathf.Lerp(floorCorners.RoomTopY, roomCeilingBottom, 0.52f),
                roomCorner.z);
            camera.transform.position = target +
                (across * side * (extent * 1.12f)) -
                (towardRoom * (extent * 0.48f)) +
                (Vector3.up * (extent * 0.12f));
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderConnectorExteriorPanel(
            Camera camera,
            Bounds connectorBounds,
            Vector3 towardRoom,
            Vector3 across,
            float side,
            int width,
            int height)
        {
            camera.orthographic = false;
            camera.fieldOfView = 48f;
            var extent = Mathf.Max(
                1.5f,
                Mathf.Max(
                    connectorBounds.extents.magnitude,
                    Mathf.Max(connectorBounds.size.y, connectorBounds.size.x)));
            var target = connectorBounds.center;
            camera.transform.position = target +
                (across * side * (extent * 2.4f)) -
                (towardRoom * (extent * 0.35f)) +
                (Vector3.up * Mathf.Max(1.1f, connectorBounds.size.y * 0.65f));
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderConnectorInteriorPanel(
            Camera camera,
            ConnectorPrismCorners floorCorners,
            float roomCeilingBottom,
            float corridorCeilingBottom,
            bool lookTowardRoom,
            int width,
            int height)
        {
            var roomCenter = (floorCorners.RoomMin + floorCorners.RoomMax) * 0.5f;
            var corridorCenter =
                (floorCorners.CorridorMin + floorCorners.CorridorMax) * 0.5f;
            var cameraFromCorridorT = lookTowardRoom ? 0.16f : 0.84f;
            var targetFromCorridorT = lookTowardRoom ? 0.84f : 0.16f;
            var connectorCenter = Vector3.Lerp(
                corridorCenter,
                roomCenter,
                cameraFromCorridorT);
            var floorY = Mathf.Lerp(
                floorCorners.CorridorTopY,
                floorCorners.RoomTopY,
                cameraFromCorridorT);
            var ceilingY = Mathf.Lerp(
                corridorCeilingBottom,
                roomCeilingBottom,
                cameraFromCorridorT);
            var eyeY = Mathf.Lerp(floorY, ceilingY, 0.46f);
            var cameraHorizontal = connectorCenter;
            camera.orthographic = false;
            camera.fieldOfView = 58f;
            camera.transform.position = new Vector3(
                cameraHorizontal.x,
                eyeY,
                cameraHorizontal.z);
            var targetHorizontal = Vector3.Lerp(
                corridorCenter,
                roomCenter,
                targetFromCorridorT);
            var targetFloorY = Mathf.Lerp(
                floorCorners.CorridorTopY,
                floorCorners.RoomTopY,
                targetFromCorridorT);
            var targetCeilingY = Mathf.Lerp(
                corridorCeilingBottom,
                roomCeilingBottom,
                targetFromCorridorT);
            var target = new Vector3(
                targetHorizontal.x,
                Mathf.Lerp(targetFloorY, targetCeilingY, 0.46f),
                targetHorizontal.z);
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderConnectorPassagePanel(
            Camera camera,
            ConnectorPrismCorners floorCorners,
            float roomCeilingBottom,
            float corridorCeilingBottom,
            int width,
            int height)
        {
            var roomCenter = (floorCorners.RoomMin + floorCorners.RoomMax) * 0.5f;
            var corridorCenter =
                (floorCorners.CorridorMin + floorCorners.CorridorMax) * 0.5f;
            var towardRoom = HorizontalDirection(roomCenter - corridorCenter);
            camera.orthographic = false;
            camera.fieldOfView = 64f;
            var corridorEyeY = Mathf.Lerp(
                floorCorners.CorridorTopY,
                corridorCeilingBottom,
                0.48f);
            var roomEyeY = Mathf.Lerp(
                floorCorners.RoomTopY,
                roomCeilingBottom,
                0.48f);
            var cameraHorizontal = corridorCenter - (towardRoom * 0.85f);
            camera.transform.position = new Vector3(
                cameraHorizontal.x,
                corridorEyeY,
                cameraHorizontal.z);
            var targetHorizontal = Vector3.Lerp(corridorCenter, roomCenter, 0.68f);
            var target = new Vector3(
                targetHorizontal.x,
                Mathf.Lerp(corridorEyeY, roomEyeY, 0.68f),
                targetHorizontal.z);
            camera.transform.rotation = Quaternion.LookRotation(
                target - camera.transform.position,
                Vector3.up);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderConnectorBoundsPanel(
            Camera camera,
            Bounds bounds,
            int width,
            int height,
            bool topDown,
            Vector3 viewDirection)
        {
            var aspect = width / (float)height;
            if (topDown)
            {
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.z * 1.12f,
                    (bounds.extents.x / aspect) * 1.12f);
                camera.transform.position = new Vector3(
                    bounds.center.x,
                    bounds.max.y + Mathf.Max(60f, bounds.size.y * 4f),
                    bounds.center.z);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                camera.orthographic = false;
                camera.fieldOfView = 42f;
                var direction = viewDirection.normalized;
                var distance = Mathf.Max(bounds.extents.magnitude * 2.8f, 7f);
                camera.transform.position = bounds.center + (direction * distance);
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
            }

            return RenderCamera(camera, width, height);
        }

        private static void InspectTriRoomConnectorSampleEndpoint(
            StringBuilder report,
            Scene sourceScene,
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot,
            Transform connectorParent,
            ConnectorEndpointDefinition definition,
            ref float maximumSeamError,
            ref float minimumClearWidth,
            ref float minimumRoomContactOverlap,
            ref float minimumCorridorContactOverlap,
            ref float minimumLateralCoverage,
            ref float maximumRoomIntrusion,
            ref float maximumCorridorIntrusion,
            ISet<string> reusedMaterialPaths)
        {
            var connector = connectorParent.Find("Connector_" + definition.Id) ??
                throw new InvalidOperationException(
                    "Missing connector sample endpoint " + definition.Id);
            int CountExisting(params string[] partNames)
            {
                var count = 0;
                for (var partIndex = 0; partIndex < partNames.Length; partIndex++)
                {
                    if (connector.Find(partNames[partIndex]) != null)
                    {
                        count++;
                    }
                }

                return count;
            }

            var roomCornerSealCount = CountExisting(
                "RoomFloorMinimumCornerSeal",
                "RoomFloorMaximumCornerSeal");
            var roomWallClosureCount = CountExisting(
                "RoomMinimumWallSleeve",
                "RoomMaximumWallSleeve",
                "RoomLeftWallApproachFill",
                "RoomRightWallApproachFill",
                "RoomMinimumContactFairing",
                "RoomMaximumContactFairing",
                "RoomLeftContactSnagGuard",
                "RoomRightContactSnagGuard",
                "RoomLeftSideBoundaryMembrane",
                "RoomRightSideBoundaryMembrane",
                "RoomLeftOuterBoundaryMembrane",
                "RoomRightOuterBoundaryMembrane");
            var roomHorizontalApproachFillCount = CountExisting(
                "RoomFloorApproachFill",
                "RoomCeilingApproachFill",
                "RoomFloorLeftWallFootprintFill",
                "RoomFloorRightWallFootprintFill");
            var additiveGapFillerCount =
                roomCornerSealCount + roomWallClosureCount +
                roomHorizontalApproachFillCount;
            if (connector.childCount != 1 + additiveGapFillerCount)
            {
                throw new InvalidOperationException(
                    definition.Id +
                    " must contain one unified hollow boundary loft and only its " +
                    "approved additive gap fillers.");
            }

            var module = modules[definition.CorridorId];
            var profile = BuildConnectorEntranceProfile(
                sourceScene,
                rooms,
                module,
                definition);
            var floor = RequireUniqueCorridorFloorRenderer(module);
            var namedLeftWall = RequireCorridorSurfaceRenderer(module, " left armored wall");
            var namedRightWall = RequireCorridorSurfaceRenderer(module, " right armored wall");
            var ceiling = RequireSingleRenderer(
                CaptureCorridorCeilingFollowers(module, ceilingRoot)[0].Transform);
            OrderCorridorWalls(
                module,
                namedLeftWall,
                namedRightWall,
                out var minimumWall,
                out var maximumWall);
            var shell = CalculateConnectorGapShell(
                definition,
                module,
                profile,
                floor,
                minimumWall,
                maximumWall,
                ceiling);
            var endpointMaximumSeamError = 0f;
            var endpointMinimumRoomOverlap = float.PositiveInfinity;
            var endpointMinimumCorridorOverlap = float.PositiveInfinity;
            var endpointMinimumLateralCoverage = float.PositiveInfinity;
            var endpointMaximumRoomIntrusion = 0f;
            var endpointMaximumCorridorIntrusion = 0f;
            var part = connector.Find("HollowBoundaryLoft") ??
                throw new InvalidOperationException(
                    definition.Id + " is missing HollowBoundaryLoft.");
            var meshFilter = part.GetComponent<MeshFilter>() ??
                throw new InvalidOperationException(
                    definition.Id + " HollowBoundaryLoft has no MeshFilter.");
            var meshRenderer = part.GetComponent<MeshRenderer>() ??
                throw new InvalidOperationException(
                    definition.Id + " HollowBoundaryLoft has no MeshRenderer.");
            var meshCollider = part.GetComponent<MeshCollider>() ??
                throw new InvalidOperationException(
                    definition.Id + " HollowBoundaryLoft has no MeshCollider.");
            if (meshFilter.sharedMesh == null ||
                meshCollider.sharedMesh != meshFilter.sharedMesh ||
                meshFilter.sharedMesh.subMeshCount != 4)
            {
                throw new InvalidOperationException(
                    definition.Id +
                    " HollowBoundaryLoft must be one four-submesh collidable asset.");
            }

            if (definition.Id == "EngineRoom_H01")
            {
                if (roomWallClosureCount == 0)
                {
                    throw new InvalidOperationException(
                        definition.Id + " requires at least one exact wall gap closure.");
                }

                RequireConnectorAdditivePartIfPresent(
                    connector,
                    "RoomMinimumWallSleeve",
                    minimumWall.sharedMaterial);
                RequireConnectorAdditivePartIfPresent(
                    connector,
                    "RoomMaximumWallSleeve",
                    maximumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomFloorApproachFill",
                    floor.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomMinimumContactFairing",
                    minimumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomMaximumContactFairing",
                    maximumWall.sharedMaterial);
            }
            if (definition.Id == "ControlRoom_H02")
            {
                if (roomWallClosureCount == 0)
                {
                    throw new InvalidOperationException(
                        definition.Id + " requires at least one exact wall pocket closure.");
                }

                RequireConnectorAdditivePartIfPresent(
                    connector,
                    "RoomLeftWallApproachFill",
                    minimumWall.sharedMaterial);
                RequireConnectorAdditivePartIfPresent(
                    connector,
                    "RoomRightWallApproachFill",
                    maximumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomCeilingApproachFill",
                    ceiling.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomLeftContactSnagGuard",
                    minimumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomRightContactSnagGuard",
                    maximumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomFloorLeftWallFootprintFill",
                    floor.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomFloorRightWallFootprintFill",
                    floor.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomLeftSideBoundaryMembrane",
                    minimumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomRightSideBoundaryMembrane",
                    maximumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomLeftOuterBoundaryMembrane",
                    minimumWall.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomRightOuterBoundaryMembrane",
                    maximumWall.sharedMaterial);
            }
            if (definition.Id == "ControlRoom_H05")
            {
                RequireConnectorAdditivePart(
                    connector,
                    "RoomFloorMinimumCornerSeal",
                    floor.sharedMaterial);
                RequireConnectorAdditivePart(
                    connector,
                    "RoomFloorMaximumCornerSeal",
                    floor.sharedMaterial);
            }

            var expectedMaterials = new[]
            {
                floor.sharedMaterial,
                maximumWall.sharedMaterial,
                ceiling.sharedMaterial,
                minimumWall.sharedMaterial
            };
            var sampleMaterials = meshRenderer.sharedMaterials;
            if (sampleMaterials.Length != expectedMaterials.Length)
            {
                throw new InvalidOperationException(
                    definition.Id + " HollowBoundaryLoft material count is invalid.");
            }

            for (var materialIndex = 0;
                 materialIndex < expectedMaterials.Length;
                 materialIndex++)
            {
                var sourceMaterialSignature = BuildMaterialReuseSignature(
                    expectedMaterials[materialIndex]);
                var sampleMaterialSignature = BuildMaterialReuseSignature(
                    sampleMaterials[materialIndex]);
                if (!string.Equals(
                        sourceMaterialSignature,
                        sampleMaterialSignature,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        definition.Id + " HollowBoundaryLoft submesh " +
                        materialIndex + " does not reuse its exact source material.");
                }

                reusedMaterialPaths.Add(
                    RequireMaterialAssetPath(sampleMaterials[materialIndex]));
            }

            endpointMaximumSeamError = CalculateUnifiedHollowConnectorSeamError(
                meshFilter.sharedMesh,
                part,
                definition,
                module,
                profile,
                shell,
                floor,
                minimumWall,
                maximumWall,
                ceiling);
            maximumSeamError = Mathf.Max(
                maximumSeamError,
                endpointMaximumSeamError);

            endpointMinimumRoomOverlap = 0f;
            endpointMinimumCorridorOverlap = 0f;
            endpointMinimumLateralCoverage = 0f;
            endpointMaximumRoomIntrusion = 0f;
            endpointMaximumCorridorIntrusion = 0f;

            minimumRoomContactOverlap = Mathf.Min(
                minimumRoomContactOverlap,
                endpointMinimumRoomOverlap);
            minimumCorridorContactOverlap = Mathf.Min(
                minimumCorridorContactOverlap,
                endpointMinimumCorridorOverlap);
            minimumLateralCoverage = Mathf.Min(
                minimumLateralCoverage,
                endpointMinimumLateralCoverage);
            maximumRoomIntrusion = Mathf.Max(
                maximumRoomIntrusion,
                endpointMaximumRoomIntrusion);
            maximumCorridorIntrusion = Mathf.Max(
                maximumCorridorIntrusion,
                endpointMaximumCorridorIntrusion);

            var corridorClearWidth = Vector3.Distance(
                shell.CorridorFloorMinimum,
                shell.CorridorFloorMaximum);
            var roomClearWidth = Vector3.Distance(
                shell.RoomFloorMinimum,
                shell.RoomFloorMaximum);
            var clearWidth = Mathf.Min(corridorClearWidth, roomClearWidth);

            minimumClearWidth = Mathf.Min(minimumClearWidth, clearWidth);
            report.AppendLine(
                definition.Id +
                "|PrimaryParts=" + (1 + additiveGapFillerCount) +
                "|AdditiveGapFillers=" + additiveGapFillerCount +
                "|RoomFloorCornerSeals=" + roomCornerSealCount +
                "|RoomWallClosureVolumes=" + roomWallClosureCount +
                "|RoomHorizontalApproachFills=" +
                roomHorizontalApproachFillCount +
                "|MaximumSeamError=" + Float(endpointMaximumSeamError) +
                "|RoomContactOverlap=" + Float(endpointMinimumRoomOverlap) +
                "|CorridorContactOverlap=" + Float(endpointMinimumCorridorOverlap) +
                "|LateralCoverage=" + Float(endpointMinimumLateralCoverage) +
                "|RoomIntrusion=" + Float(endpointMaximumRoomIntrusion) +
                "|CorridorIntrusion=" + Float(endpointMaximumCorridorIntrusion) +
                "|ClearPassageWidth=" + Float(clearWidth) +
                "|MaterialsReused=True" +
                "|MeshColliders=True" +
                "|UnifiedHollowBoundaryLoft=True" +
                "|CentralPassageCap=False" +
                "|WatertightSharedEdges=True" +
                "|ExistingSourceGeometryUnchanged=True" +
                "|PassageBlocked=False");
        }

        private static void RequireConnectorAdditivePart(
            Transform connector,
            string partName,
            Material expectedMaterial,
            bool allowDegenerateNonCollidable = false)
        {
            var part = connector.Find(partName) ??
                throw new InvalidOperationException(
                    connector.name + " is missing " + partName + ".");
            var meshFilter = part.GetComponent<MeshFilter>();
            var meshRenderer = part.GetComponent<MeshRenderer>();
            var meshCollider = part.GetComponent<MeshCollider>();
            var visualMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            var materialMatches = meshRenderer != null &&
                string.Equals(
                    BuildMaterialReuseSignature(meshRenderer.sharedMaterial),
                    BuildMaterialReuseSignature(expectedMaterial),
                    StringComparison.Ordinal);
            var colliderIsValid = meshCollider != null &&
                meshCollider.sharedMesh != null;
            var degenerateColliderIsAllowed =
                allowDegenerateNonCollidable &&
                meshCollider == null &&
                visualMesh != null &&
                !HasNonDegenerateMeshTriangle(visualMesh);
            if (visualMesh == null ||
                !materialMatches ||
                (!colliderIsValid && !degenerateColliderIsAllowed))
            {
                throw new InvalidOperationException(
                    connector.name + " " + partName +
                    " must be a collidable source-material additive gap filler.");
            }
        }

        private static void RequireConnectorAdditivePartIfPresent(
            Transform connector,
            string partName,
            Material expectedMaterial)
        {
            if (connector.Find(partName) == null)
            {
                return;
            }

            RequireConnectorAdditivePart(connector, partName, expectedMaterial);
        }

        private static bool HasNonDegenerateMeshTriangle(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (var triangleIndex = 0;
                 triangleIndex + 2 < triangles.Length;
                 triangleIndex += 3)
            {
                var first = vertices[triangles[triangleIndex]];
                var second = vertices[triangles[triangleIndex + 1]];
                var third = vertices[triangles[triangleIndex + 2]];
                if (Vector3.Cross(second - first, third - first).sqrMagnitude >
                    0.00000001f)
                {
                    return true;
                }
            }

            return false;
        }

        private static float CalculateUnifiedHollowConnectorSeamError(
            Mesh mesh,
            Transform meshTransform,
            ConnectorEndpointDefinition definition,
            Transform module,
            ConnectorEntranceProfile profile,
            ConnectorGapShell shell,
            Renderer corridorFloor,
            Renderer corridorMinimumWall,
            Renderer corridorMaximumWall,
            Renderer corridorCeiling)
        {
            var expectedSolids = CreateSharedEndConnectorSolids(
                definition,
                module,
                profile,
                shell,
                corridorFloor,
                corridorMinimumWall,
                corridorMaximumWall,
                corridorCeiling);
            var maximumSeamError = 0f;
            for (var solidIndex = 0;
                 solidIndex < expectedSolids.Length;
                 solidIndex++)
            {
                maximumSeamError = Mathf.Max(
                    maximumSeamError,
                    CalculateConnectorSolidSeamError(
                        mesh,
                        meshTransform,
                        expectedSolids[solidIndex]));
            }

            return maximumSeamError;
        }

        private static float CalculateConnectorSurfaceSeamError(
            Mesh mesh,
            Transform meshTransform,
            ConnectorSurfaceCorners expected)
        {
            var expectedPoints = new[]
            {
                expected.First,
                expected.Second,
                expected.Third,
                expected.Fourth
            };
            var vertices = mesh.vertices;
            var maximumMinimumDistance = 0f;
            for (var expectedIndex = 0; expectedIndex < expectedPoints.Length; expectedIndex++)
            {
                var minimumDistance = float.PositiveInfinity;
                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    minimumDistance = Mathf.Min(
                        minimumDistance,
                        Vector3.Distance(
                            expectedPoints[expectedIndex],
                            meshTransform.TransformPoint(vertices[vertexIndex])));
                }

                maximumMinimumDistance = Mathf.Max(
                    maximumMinimumDistance,
                    minimumDistance);
            }

            return maximumMinimumDistance;
        }

        private static float CalculateConnectorSolidSeamError(
            Mesh mesh,
            Transform meshTransform,
            ConnectorSolidCorners expected)
        {
            var expectedPoints = new[]
            {
                expected.RoomFirst,
                expected.RoomSecond,
                expected.RoomThird,
                expected.RoomFourth,
                expected.CorridorFirst,
                expected.CorridorSecond,
                expected.CorridorThird,
                expected.CorridorFourth
            };
            var vertices = mesh.vertices;
            var maximumMinimumDistance = 0f;
            for (var expectedIndex = 0; expectedIndex < expectedPoints.Length; expectedIndex++)
            {
                var minimumDistance = float.PositiveInfinity;
                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    minimumDistance = Mathf.Min(
                        minimumDistance,
                        Vector3.Distance(
                            expectedPoints[expectedIndex],
                            meshTransform.TransformPoint(vertices[vertexIndex])));
                }

                maximumMinimumDistance = Mathf.Max(
                    maximumMinimumDistance,
                    minimumDistance);
            }

            return maximumMinimumDistance;
        }

        private static float CalculateConnectorMeshSeamError(
            Mesh mesh,
            Transform meshTransform,
            ConnectorPrismCorners expected)
        {
            var expectedPoints = new[]
            {
                new Vector3(expected.RoomMin.x, expected.RoomBottomY, expected.RoomMin.z),
                new Vector3(expected.RoomMin.x, expected.RoomTopY, expected.RoomMin.z),
                new Vector3(expected.RoomMax.x, expected.RoomBottomY, expected.RoomMax.z),
                new Vector3(expected.RoomMax.x, expected.RoomTopY, expected.RoomMax.z),
                new Vector3(expected.CorridorMin.x, expected.CorridorBottomY, expected.CorridorMin.z),
                new Vector3(expected.CorridorMin.x, expected.CorridorTopY, expected.CorridorMin.z),
                new Vector3(expected.CorridorMax.x, expected.CorridorBottomY, expected.CorridorMax.z),
                new Vector3(expected.CorridorMax.x, expected.CorridorTopY, expected.CorridorMax.z)
            };
            var vertices = mesh.vertices;
            var maximumMinimumDistance = 0f;
            for (var expectedIndex = 0; expectedIndex < expectedPoints.Length; expectedIndex++)
            {
                var minimumDistance = float.PositiveInfinity;
                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    minimumDistance = Mathf.Min(
                        minimumDistance,
                        Vector3.Distance(
                            expectedPoints[expectedIndex],
                            meshTransform.TransformPoint(vertices[vertexIndex])));
                }

                maximumMinimumDistance = Mathf.Max(
                    maximumMinimumDistance,
                    minimumDistance);
            }

            return maximumMinimumDistance;
        }

        private static Scene RequireSourceScene()
        {
            var sourceScene = SceneManager.GetSceneByPath(SourceScenePath);
            if (!sourceScene.isLoaded)
            {
                sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            }

            if (!sourceScene.IsValid() || !sourceScene.isLoaded)
            {
                throw new InvalidOperationException("Unable to load CargoRunMvp as the Pegasus source.");
            }

            return sourceScene;
        }

        private static Dictionary<string, GameObject> RequireRooms(Scene scene)
        {
            var rooms = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                rooms.Add(definition.Id, RequireSceneObject(scene, definition.RootName));
            }

            return rooms;
        }

        private static Dictionary<string, GameObject> RequireTriRoomConnectorRooms(Scene scene)
        {
            var rooms = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (var roomId in new[] { "EngineRoom", "Cockpit", "ControlRoom" })
            {
                RoomDefinition match = default;
                var found = false;
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    if (RoomDefinitions[roomIndex].Id != roomId)
                    {
                        continue;
                    }

                    match = RoomDefinitions[roomIndex];
                    found = true;
                    break;
                }

                if (!found)
                {
                    throw new InvalidOperationException(
                        "Missing room definition for connector sample " + roomId);
                }

                rooms.Add(roomId, RequireSceneObject(scene, match.RootName));
            }

            return rooms;
        }

        private static Dictionary<string, Transform> RequireTriRoomConnectorModules(
            Transform corridorRoot)
        {
            var modules = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var corridorId in new[] { "SC-H01", "SC-H02", "SC-H05" })
            {
                Transform match = null;
                for (var childIndex = 0;
                     childIndex < corridorRoot.childCount;
                     childIndex++)
                {
                    var child = corridorRoot.GetChild(childIndex);
                    if (child.name.StartsWith(corridorId + " ", StringComparison.Ordinal))
                    {
                        match = child;
                        break;
                    }
                }

                modules.Add(
                    corridorId,
                    match ?? throw new InvalidOperationException(
                        "Missing connector sample corridor " + corridorId));
            }

            return modules;
        }

        private static GameObject RequireSceneObject(Scene scene, string objectName)
        {
            var roots = scene.GetRootGameObjects();
            GameObject match = null;
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    if (transforms[transformIndex].name != objectName)
                    {
                        continue;
                    }

                    if (match != null)
                    {
                        throw new InvalidOperationException(
                            "Duplicate scene object named " + objectName + " in " + scene.path);
                    }

                    match = transforms[transformIndex].gameObject;
                }
            }

            return match ?? throw new InvalidOperationException(
                "Missing scene object named " + objectName + " in " + scene.path);
        }

        private static GameObject RequireRootSceneObject(Scene scene, string objectName)
        {
            var roots = scene.GetRootGameObjects();
            GameObject match = null;
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (roots[rootIndex].name != objectName)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "Duplicate root scene object named " + objectName + " in " + scene.path);
                }

                match = roots[rootIndex];
            }

            return match ?? throw new InvalidOperationException(
                "Missing root scene object named " + objectName + " in " + scene.path);
        }

        private static int CountExternalSceneReferences(GameObject root, Scene scene)
        {
            var count = 0;
            var components = root.GetComponentsInChildren<Component>(true);
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var component = components[componentIndex];
                if (component == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                var enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = false;
                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        property.objectReferenceValue == null)
                    {
                        continue;
                    }

                    var referencedObject = property.objectReferenceValue;
                    var referencedGameObject = referencedObject as GameObject;
                    if (referencedGameObject == null && referencedObject is Component referencedComponent)
                    {
                        referencedGameObject = referencedComponent.gameObject;
                    }

                    if (referencedGameObject == null || referencedGameObject.scene != scene ||
                        referencedGameObject.transform.IsChildOf(root.transform))
                    {
                        continue;
                    }

                    count++;
                }
            }

            return count;
        }

        private static string GetHierarchyPath(Transform root, Transform target)
        {
            if (target == root)
            {
                return root.name;
            }

            var names = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Add(root.name);
            names.Reverse();
            return string.Join("/", names);
        }

        private static void AppendEntranceCandidates(
            StringBuilder report,
            string label,
            Transform roomRoot,
            Func<string, bool> namePredicate)
        {
            var candidates = new List<Transform>();
            var transforms = roomRoot.GetComponentsInChildren<Transform>(true);
            for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                var candidate = transforms[transformIndex];
                if (candidate != roomRoot && namePredicate(candidate.name))
                {
                    candidates.Add(candidate);
                }
            }

            candidates.Sort((left, right) => string.CompareOrdinal(
                GetHierarchyPath(roomRoot, left),
                GetHierarchyPath(roomRoot, right)));
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "No entrance candidates found for " + label + " below " + roomRoot.name);
            }

            report.AppendLine("[" + label + "]");
            report.AppendLine("RoomRoot=" + roomRoot.name + "|CandidateCount=" + candidates.Count);
            for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                var candidate = candidates[candidateIndex];
                report.Append("Path=").Append(GetHierarchyPath(roomRoot, candidate))
                    .Append("|WorldPosition=").Append(Vector(candidate.position))
                    .Append("|WorldRotation=").Append(QuaternionText(candidate.rotation))
                    .Append("|Right=").Append(Vector(candidate.right.normalized))
                    .Append("|Forward=").Append(Vector(candidate.forward.normalized));
                var renderer = candidate.GetComponent<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    report.Append("|RendererBounds=").Append(BoundsText(renderer.bounds));
                }

                report.AppendLine();
            }

            report.AppendLine();
        }

        private static EntranceConnectionAnchor GetTriRoomEntranceAnchor(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            string roomId,
            string otherRoomId)
        {
            if (roomId == "EngineRoom" && otherRoomId == "Cockpit")
            {
                var firstWall = RequireRenderer(
                    scene,
                    "ER-01 1시 Cockpit corridor side wall 1");
                var secondWall = RequireRenderer(
                    scene,
                    "ER-01 1시 Cockpit corridor side wall 2");
                return BuildWallPairEntranceAnchor(
                    "EngineRoomToCockpit",
                    firstWall,
                    secondWall,
                    CalculateEntranceOutwardFromRoom(
                        rooms[roomId],
                        (firstWall.bounds.center + secondWall.bounds.center) * 0.5f));
            }

            if (roomId == "EngineRoom" && otherRoomId == "ControlRoom")
            {
                var firstWall = RequireRenderer(
                    scene,
                    "ER-01 3시 Control corridor side wall 1");
                var secondWall = RequireRenderer(
                    scene,
                    "ER-01 3시 Control corridor side wall 2");
                return BuildWallPairEntranceAnchor(
                    "EngineRoomToControlRoom",
                    firstWall,
                    secondWall,
                    CalculateEntranceOutwardFromRoom(
                        rooms[roomId],
                        (firstWall.bounds.center + secondWall.bounds.center) * 0.5f));
            }

            if (roomId == "Cockpit" && otherRoomId == "EngineRoom")
            {
                var renderer = RequireRenderer(
                    scene,
                    "left future opening floor edge only");
                return BuildSingleRendererEntranceAnchor(
                    "CockpitToEngineRoom",
                    renderer,
                    true,
                    CalculateEntranceOutwardFromRoom(rooms[roomId], renderer.bounds.center));
            }

            if (roomId == "Cockpit" && otherRoomId == "ControlRoom")
            {
                var renderer = RequireRenderer(
                    scene,
                    "right future opening floor edge only");
                return BuildSingleRendererEntranceAnchor(
                    "CockpitToControlRoom",
                    renderer,
                    true,
                    CalculateEntranceOutwardFromRoom(rooms[roomId], renderer.bounds.center));
            }

            if (roomId == "ControlRoom" && otherRoomId == "Cockpit")
            {
                var renderer = RequireRenderer(
                    scene,
                    "CR-01 cockpit 40 degree outside only corridor floor continuation");
                return BuildSingleRendererEntranceAnchor(
                    "ControlRoomToCockpit",
                    renderer,
                    true,
                    new Vector3(
                        -Mathf.Cos(40f * Mathf.Deg2Rad),
                        0f,
                        Mathf.Sin(40f * Mathf.Deg2Rad)));
            }

            if (roomId == "ControlRoom" && otherRoomId == "EngineRoom")
            {
                var renderer = RequireRenderer(
                    scene,
                    "CR-01 engine room left separated corridor floor continuation");
                return BuildSingleRendererEntranceAnchor(
                    "ControlRoomToEngineRoom",
                    renderer,
                    true,
                    Vector3.left);
            }

            throw new InvalidOperationException(
                "Unsupported Pegasus tri-room entrance pair: " + roomId + " -> " + otherRoomId);
        }

        private static Vector3 CalculateEntranceOutwardFromRoom(
            GameObject room,
            Vector3 entranceCenter)
        {
            var roomCenter = CalculateVisibleBounds(room).center;
            return HorizontalDirection(entranceCenter - roomCenter);
        }

        private static EntranceConnectionAnchor BuildWallPairEntranceAnchor(
            string label,
            Renderer firstWall,
            Renderer secondWall,
            Vector3 outward)
        {
            var averageCenter = (firstWall.bounds.center + secondWall.bounds.center) * 0.5f;
            var outerProjection = Mathf.Max(
                GetRendererProjectionMaximum(firstWall, outward),
                GetRendererProjectionMaximum(secondWall, outward));
            var point = averageCenter +
                (outward * (outerProjection - Vector3.Dot(averageCenter, outward)));
            var floorY = Mathf.Min(firstWall.bounds.min.y, secondWall.bounds.min.y);
            point.y = floorY;
            return new EntranceConnectionAnchor(label, point, outward, floorY);
        }

        private static EntranceConnectionAnchor BuildWallPairEntranceAnchor(
            string label,
            GameObject room,
            Renderer firstWall,
            Renderer secondWall)
        {
            var averageCenter = (firstWall.bounds.center + secondWall.bounds.center) * 0.5f;
            return BuildWallPairEntranceAnchor(
                label,
                firstWall,
                secondWall,
                HorizontalDirection(averageCenter - room.transform.position));
        }

        private static EntranceConnectionAnchor BuildSingleRendererEntranceAnchor(
            string label,
            Renderer renderer,
            bool useRendererTopAsFloor,
            Vector3 outward)
        {
            var outerProjection = GetRendererProjectionMaximum(renderer, outward);
            var point = renderer.bounds.center +
                (outward * (outerProjection - Vector3.Dot(renderer.bounds.center, outward)));
            var floorY = useRendererTopAsFloor ? renderer.bounds.max.y : renderer.bounds.min.y;
            point.y = floorY;
            return new EntranceConnectionAnchor(label, point, outward, floorY);
        }

        private static EntranceConnectionAnchor BuildSingleRendererEntranceAnchor(
            string label,
            GameObject room,
            Renderer renderer,
            bool useRendererTopAsFloor)
        {
            return BuildSingleRendererEntranceAnchor(
                label,
                renderer,
                useRendererTopAsFloor,
                HorizontalDirection(renderer.bounds.center - room.transform.position));
        }

        private static Renderer RequireRenderer(Scene scene, string objectName)
        {
            var target = RequireSceneObject(scene, objectName);
            return target.GetComponent<Renderer>() ?? throw new InvalidOperationException(
                "Pegasus entrance marker has no Renderer: " + objectName);
        }

        private static float GetRendererProjectionMaximum(Renderer renderer, Vector3 direction)
        {
            var localBounds = renderer.localBounds;
            var maximum = float.NegativeInfinity;
            for (var xIndex = 0; xIndex < 2; xIndex++)
            {
                for (var yIndex = 0; yIndex < 2; yIndex++)
                {
                    for (var zIndex = 0; zIndex < 2; zIndex++)
                    {
                        var localPoint = new Vector3(
                            xIndex == 0 ? localBounds.min.x : localBounds.max.x,
                            yIndex == 0 ? localBounds.min.y : localBounds.max.y,
                            zIndex == 0 ? localBounds.min.z : localBounds.max.z);
                        maximum = Mathf.Max(
                            maximum,
                            Vector3.Dot(renderer.transform.TransformPoint(localPoint), direction));
                    }
                }
            }

            return maximum;
        }

        private static Vector3 HorizontalDirection(Vector3 value)
        {
            value.y = 0f;
            if (value.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException("Horizontal direction is degenerate.");
            }

            return value.normalized;
        }

        private static void AppendRoomRoot(StringBuilder report, string roomId, Transform room)
        {
            report.AppendLine(
                roomId +
                "|Position=" + Vector(room.position) +
                "|Rotation=" + QuaternionText(room.rotation) +
                "|Scale=" + Vector(room.localScale) +
                "|Bounds=" + BoundsText(CalculateVisibleBounds(room.gameObject)));
        }

        private static void SetRoomHorizontalPosition(Transform room, Vector3 targetPosition)
        {
            room.position = new Vector3(targetPosition.x, room.position.y, targetPosition.z);
            EditorUtility.SetDirty(room);
        }

        private static void SetRoomWorldRotationAroundVisibleCenter(
            Transform room,
            Quaternion targetRotation)
        {
            var visibleCenter = CalculateVisibleBounds(room.gameObject).center;
            room.rotation = targetRotation;
            var rotatedCenter = CalculateVisibleBounds(room.gameObject).center;
            room.position += visibleCenter - rotatedCenter;
            EditorUtility.SetDirty(room);
        }

        private static float MoveControlRoomRightForGentlerH02Approach(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            float angleBefore)
        {
            var controlRoom = rooms["ControlRoom"].transform;
            var originalPosition = controlRoom.position;
            var selectedShift = ControlRoomMaximumRightShift;
            var targetReached = false;
            for (var shift = ControlRoomMinimumRightShift;
                 shift <= ControlRoomMaximumRightShift + PositionTolerance;
                 shift += ControlRoomRightShiftStep)
            {
                SetRoomHorizontalPosition(
                    controlRoom,
                    originalPosition + (Vector3.right * shift));
                var candidateAngle = CalculateTriRoomConnectionAngle(
                    scene,
                    rooms,
                    "Cockpit",
                    "ControlRoom");
                if (candidateAngle - PositionTolerance >
                    ControlRoomMaximumH02ApproachAngle)
                {
                    continue;
                }

                selectedShift = shift;
                targetReached = true;
                break;
            }

            if (!targetReached)
            {
                SetRoomHorizontalPosition(
                    controlRoom,
                    originalPosition + (Vector3.right * selectedShift));
            }

            var angleAfter = CalculateTriRoomConnectionAngle(
                scene,
                rooms,
                "Cockpit",
                "ControlRoom");
            if (selectedShift + PositionTolerance < ControlRoomMinimumRightShift ||
                angleAfter + PositionTolerance >= angleBefore ||
                angleAfter - PositionTolerance > ControlRoomMaximumH02ApproachAngle)
            {
                throw new InvalidOperationException(
                    "ControlRoom rightward move did not produce a gentler H02 approach. " +
                    "Shift=" + Float(selectedShift) +
                    "; AngleBefore=" + Float(angleBefore) +
                    "; AngleAfter=" + Float(angleAfter) +
                    "; MaximumAllowedAngle=" +
                    Float(ControlRoomMaximumH02ApproachAngle));
            }

            return selectedShift;
        }

        private static Vector3 MoveCockpitToBalancedEntranceApproaches(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules)
        {
            var engineEntrance = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                "EngineRoom",
                "Cockpit");
            var cockpitEngineEntrance = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                "Cockpit",
                "EngineRoom");
            var cockpitControlEntrance = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                "Cockpit",
                "ControlRoom");
            var controlEntrance = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                "ControlRoom",
                "Cockpit");
            var h01BalancedDirection = HorizontalDirection(
                engineEntrance.Outward - cockpitEngineEntrance.Outward);
            var h02BalancedDirection = HorizontalDirection(
                cockpitControlEntrance.Outward - controlEntrance.Outward);
            var h01Across = HorizontalDirection(
                Vector3.Cross(h01BalancedDirection, Vector3.up));
            var h02Across = HorizontalDirection(
                Vector3.Cross(h02BalancedDirection, Vector3.up));
            var h01HalfWidth =
                GetCorridorWorldOutsideWidth(modules["SC-H01"]) * 0.5f;
            var h02HalfWidth =
                GetCorridorWorldOutsideWidth(modules["SC-H02"]) * 0.5f;
            var h01EngineClearance =
                (h01HalfWidth *
                 Mathf.Abs(Vector3.Dot(h01Across, engineEntrance.Outward))) +
                TriRoomEntranceEdgeSafety;
            var h01CockpitClearance =
                (h01HalfWidth *
                 Mathf.Abs(Vector3.Dot(h01Across, cockpitEngineEntrance.Outward))) +
                TriRoomEntranceEdgeSafety;
            var h02CockpitClearance =
                (h02HalfWidth *
                 Mathf.Abs(Vector3.Dot(h02Across, cockpitControlEntrance.Outward))) +
                TriRoomEntranceEdgeSafety;
            var h02ControlClearance =
                (h02HalfWidth *
                 Mathf.Abs(Vector3.Dot(h02Across, controlEntrance.Outward))) +
                TriRoomEntranceEdgeSafety;
            var h02RetreatProjection =
                Vector3.Dot(-h02BalancedDirection, controlEntrance.Outward);
            if (h02RetreatProjection <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Balanced H02 direction cannot preserve the ControlRoom-side buffer gap.");
            }

            var h02RetreatDistance =
                (H02ControlRoomBufferGap - TriRoomEntranceEdgeSafety) /
                h02RetreatProjection;
            var rightHandSide =
                controlEntrance.Point +
                (controlEntrance.Outward * h02ControlClearance) -
                (h02BalancedDirection * h02RetreatDistance) -
                (cockpitControlEntrance.Outward * h02CockpitClearance) -
                cockpitControlEntrance.Point -
                engineEntrance.Point -
                (engineEntrance.Outward * h01EngineClearance) +
                (cockpitEngineEntrance.Outward * h01CockpitClearance) +
                cockpitEngineEntrance.Point;
            var determinant =
                (h01BalancedDirection.x * h02BalancedDirection.z) -
                (h02BalancedDirection.x * h01BalancedDirection.z);
            if (Mathf.Abs(determinant) <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Balanced H01/H02 cockpit placement has parallel solve directions.");
            }

            var h01Distance =
                ((rightHandSide.x * h02BalancedDirection.z) -
                 (h02BalancedDirection.x * rightHandSide.z)) /
                determinant;
            var h02Distance =
                ((h01BalancedDirection.x * rightHandSide.z) -
                 (rightHandSide.x * h01BalancedDirection.z)) /
                determinant;
            if (h01Distance <= PositionTolerance || h02Distance <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Balanced H01/H02 cockpit placement produced a reversed corridor. " +
                    "H01Distance=" + Float(h01Distance) +
                    "; H02Distance=" + Float(h02Distance));
            }

            var targetCockpitEnginePoint =
                engineEntrance.Point +
                (engineEntrance.Outward * h01EngineClearance) +
                (h01BalancedDirection * h01Distance) -
                (cockpitEngineEntrance.Outward * h01CockpitClearance);
            var horizontalMove = targetCockpitEnginePoint - cockpitEngineEntrance.Point;
            horizontalMove.y = 0f;
            var cockpit = rooms["Cockpit"].transform;
            SetRoomHorizontalPosition(cockpit, cockpit.position + horizontalMove);
            return horizontalMove;
        }

        private static void RedistributeCockpitCorridorLengths(
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules,
            out Vector3 engineRoomMove,
            out Vector3 controlRoomMove,
            out float combinedLength,
            out float targetH01Length,
            out float targetH02Length)
        {
            var currentH01Length = GetCorridorWorldVisibleLength(modules["SC-H01"]);
            var currentH02Length = GetCorridorWorldVisibleLength(modules["SC-H02"]);
            combinedLength = currentH01Length + currentH02Length;
            var combinedWeight =
                CockpitCorridorH01LengthWeight + CockpitCorridorH02LengthWeight;
            targetH01Length =
                combinedLength * CockpitCorridorH01LengthWeight / combinedWeight;
            targetH02Length =
                combinedLength * CockpitCorridorH02LengthWeight / combinedWeight;

            var h01Direction = HorizontalDirection(modules["SC-H01"].right);
            var h02Direction = HorizontalDirection(modules["SC-H02"].right);
            engineRoomMove =
                -h01Direction * (targetH01Length - currentH01Length);
            controlRoomMove =
                h02Direction * (targetH02Length - currentH02Length);
            engineRoomMove.y = 0f;
            controlRoomMove.y = 0f;
            SetRoomHorizontalPosition(
                rooms["EngineRoom"].transform,
                rooms["EngineRoom"].transform.position + engineRoomMove);
            SetRoomHorizontalPosition(
                rooms["ControlRoom"].transform,
                rooms["ControlRoom"].transform.position + controlRoomMove);
        }

        private static void CalculateTriRoomEntrancePairMeetingAngles(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            string fromRoomId,
            string toRoomId,
            out float fromMeetingAngle,
            out float toMeetingAngle)
        {
            var from = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                fromRoomId,
                toRoomId);
            var to = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                toRoomId,
                fromRoomId);
            CalculateEntrancePairMeetingAngles(
                from,
                to,
                out fromMeetingAngle,
                out toMeetingAngle);
        }

        private static float CalculateTriRoomConnectionAngle(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            string fromRoomId,
            string toRoomId)
        {
            var from = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                fromRoomId,
                toRoomId);
            var to = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                toRoomId,
                fromRoomId);
            return CalculateHorizontalAngleFromXAxis(from.Point, to.Point);
        }

        private static float MoveCockpitUpForGentlerApproach(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            float h01AngleFromZBefore,
            float h02AngleFromZBefore)
        {
            var cockpit = rooms["Cockpit"].transform;
            var originalPosition = cockpit.position;
            var selectedShift = CockpitMaximumUpwardShift;
            var targetReached = false;
            for (var shift = CockpitMinimumUpwardShift;
                 shift <= CockpitMaximumUpwardShift + PositionTolerance;
                 shift += CockpitUpwardShiftStep)
            {
                SetRoomHorizontalPosition(
                    cockpit,
                    originalPosition + (Vector3.forward * shift));
                var h01Candidate = CalculateTriRoomConnectionAngleFromZAxis(
                    scene,
                    rooms,
                    "EngineRoom",
                    "Cockpit");
                var h02Candidate = CalculateTriRoomConnectionAngleFromZAxis(
                    scene,
                    rooms,
                    "Cockpit",
                    "ControlRoom");
                if (h01Candidate + PositionTolerance >= h01AngleFromZBefore ||
                    h02Candidate + PositionTolerance >= h02AngleFromZBefore ||
                    h02Candidate - PositionTolerance > CockpitMaximumH02AngleFromZAxis)
                {
                    continue;
                }

                selectedShift = shift;
                targetReached = true;
                break;
            }

            if (!targetReached)
            {
                SetRoomHorizontalPosition(
                    cockpit,
                    originalPosition + (Vector3.forward * selectedShift));
            }

            var h01AngleFromZAfter = CalculateTriRoomConnectionAngleFromZAxis(
                scene,
                rooms,
                "EngineRoom",
                "Cockpit");
            var h02AngleFromZAfter = CalculateTriRoomConnectionAngleFromZAxis(
                scene,
                rooms,
                "Cockpit",
                "ControlRoom");
            if (selectedShift + PositionTolerance < CockpitMinimumUpwardShift ||
                h01AngleFromZAfter + PositionTolerance >= h01AngleFromZBefore ||
                h02AngleFromZAfter + PositionTolerance >= h02AngleFromZBefore ||
                h02AngleFromZAfter - PositionTolerance >
                    CockpitMaximumH02AngleFromZAxis)
            {
                throw new InvalidOperationException(
                    "Cockpit upward move did not produce gentler H01 and H02 approaches. " +
                    "Shift=" + Float(selectedShift) +
                    "; H01Before=" + Float(h01AngleFromZBefore) +
                    "; H01After=" + Float(h01AngleFromZAfter) +
                    "; H02Before=" + Float(h02AngleFromZBefore) +
                    "; H02After=" + Float(h02AngleFromZAfter));
            }

            return selectedShift;
        }

        private static float CalculateTriRoomConnectionAngleFromZAxis(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            string fromRoomId,
            string toRoomId)
        {
            var from = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                fromRoomId,
                toRoomId);
            var to = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                toRoomId,
                fromRoomId);
            return CalculateHorizontalAngleFromZAxis(from.Point, to.Point);
        }

        private static float CalculateHorizontalAngleFromXAxis(
            Vector3 from,
            Vector3 to)
        {
            var delta = to - from;
            return Mathf.Atan2(Mathf.Abs(delta.z), Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
        }

        private static float CalculateHorizontalAngleFromZAxis(
            Vector3 from,
            Vector3 to)
        {
            return 90f - CalculateHorizontalAngleFromXAxis(from, to);
        }

        private static void CalculateEntrancePairMeetingAngles(
            EntranceConnectionAnchor from,
            EntranceConnectionAnchor to,
            out float fromMeetingAngle,
            out float toMeetingAngle)
        {
            var direction = HorizontalDirection(to.Point - from.Point);
            fromMeetingAngle = Vector3.Angle(direction, from.Outward);
            toMeetingAngle = Vector3.Angle(-direction, to.Outward);
        }

        private static TriRoomUniformPlacement SolveTriRoomUniformPlacement(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules)
        {
            var engineRoom = rooms["EngineRoom"].transform;
            var cockpit = rooms["Cockpit"].transform;
            var controlRoom = rooms["ControlRoom"].transform;
            var engineToCockpit = GetTriRoomEntranceAnchor(
                scene,
                rooms,
                "EngineRoom",
                "Cockpit");
            var engineToControl = GetTriRoomEntranceAnchor(
                scene,
                rooms,
                "EngineRoom",
                "ControlRoom");
            var cockpitToEngine = GetTriRoomEntranceAnchor(
                scene,
                rooms,
                "Cockpit",
                "EngineRoom");
            var cockpitToControl = GetTriRoomEntranceAnchor(
                scene,
                rooms,
                "Cockpit",
                "ControlRoom");
            var controlToEngine = GetTriRoomEntranceAnchor(
                scene,
                rooms,
                "ControlRoom",
                "EngineRoom");
            var controlToCockpit = GetTriRoomEntranceAnchor(
                scene,
                rooms,
                "ControlRoom",
                "Cockpit");

            var h01Length = GetCorridorWorldVisibleLength(modules["SC-H01"]);
            var h02Length = GetCorridorWorldVisibleLength(modules["SC-H02"]);
            var h05Length = GetCorridorWorldVisibleLength(modules["SC-H05"]);
            if (Mathf.Abs(h01Length - h02Length) > PositionTolerance ||
                Mathf.Abs(h01Length - h05Length) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Target corridors do not share one preserved world length. H01=" +
                    Float(h01Length) + "; H02=" + Float(h02Length) +
                    "; H05=" + Float(h05Length));
            }

            var targetDistance = h01Length + (TriRoomUniformConnectorGap * 2f);
            var currentEngine = Horizontal(engineRoom.position);
            var currentCockpit = Horizontal(cockpit.position);
            var currentControl = Horizontal(controlRoom.position);
            var currentCentroid = (currentEngine + currentCockpit + currentControl) / 3f;
            var engineToCockpitOffset = Horizontal(engineToCockpit.Point - engineRoom.position);
            var engineToControlOffset = Horizontal(engineToControl.Point - engineRoom.position);
            var cockpitToEngineOffset = Horizontal(cockpitToEngine.Point - cockpit.position);
            var cockpitToControlOffset = Horizontal(cockpitToControl.Point - cockpit.position);
            var controlToEngineOffset = Horizontal(controlToEngine.Point - controlRoom.position);
            var controlToCockpitOffset = Horizontal(controlToCockpit.Point - controlRoom.position);
            var roomBounds = new Dictionary<string, Bounds>(StringComparer.Ordinal);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                roomBounds.Add(definition.Id, CalculateVisibleBounds(rooms[definition.Id]));
            }

            var found = false;
            var bestScore = float.PositiveInfinity;
            var bestEngine = currentEngine;
            var bestCockpit = currentCockpit;
            var bestControl = currentControl;
            for (var angleStep = 0; angleStep < 1440; angleStep++)
            {
                var angleRadians = angleStep * 0.25f * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
                var provisionalEngine = currentEngine;
                var provisionalCockpit = provisionalEngine +
                    engineToCockpitOffset +
                    (direction * targetDistance) -
                    cockpitToEngineOffset;
                var cockpitControlAnchor = provisionalCockpit + cockpitToControlOffset;
                var engineControlAnchor = provisionalEngine + engineToControlOffset;
                var controlRootCircleFromCockpit = cockpitControlAnchor - controlToCockpitOffset;
                var controlRootCircleFromEngine = engineControlAnchor - controlToEngineOffset;
                var circleDelta = controlRootCircleFromEngine - controlRootCircleFromCockpit;
                var circleDistance = circleDelta.magnitude;
                if (circleDistance <= 0.0001f || circleDistance > (targetDistance * 2f))
                {
                    continue;
                }

                var circleMidpoint =
                    (controlRootCircleFromCockpit + controlRootCircleFromEngine) * 0.5f;
                var halfSeparation = circleDistance * 0.5f;
                var intersectionHeightSquared =
                    (targetDistance * targetDistance) - (halfSeparation * halfSeparation);
                if (intersectionHeightSquared < 0f)
                {
                    continue;
                }

                var perpendicular = new Vector2(-circleDelta.y, circleDelta.x) / circleDistance;
                var intersectionOffset = perpendicular * Mathf.Sqrt(intersectionHeightSquared);
                for (var intersectionIndex = 0; intersectionIndex < 2; intersectionIndex++)
                {
                    var provisionalControl = circleMidpoint +
                        (intersectionIndex == 0 ? intersectionOffset : -intersectionOffset);
                    var provisionalCentroid =
                        (provisionalEngine + provisionalCockpit + provisionalControl) / 3f;
                    var centroidTranslation = currentCentroid - provisionalCentroid;
                    var centeredEngine = provisionalEngine + centroidTranslation;
                    var centeredCockpit = provisionalCockpit + centroidTranslation;
                    var centeredControl = provisionalControl + centroidTranslation;
                    if (!HasTriRoomDiagramOrdering(
                            centeredEngine,
                            centeredCockpit,
                            centeredControl))
                    {
                        continue;
                    }

                    for (var offsetX = -40f; offsetX <= 40.001f; offsetX += 4f)
                    {
                        for (var offsetZ = -40f; offsetZ <= 40.001f; offsetZ += 4f)
                        {
                            var layoutTranslation = new Vector2(offsetX, offsetZ);
                            var candidateEngine = centeredEngine + layoutTranslation;
                            var candidateCockpit = centeredCockpit + layoutTranslation;
                            var candidateControl = centeredControl + layoutTranslation;
                            if (HasCandidateRoomOverlap(
                                    rooms,
                                    roomBounds,
                                    candidateEngine,
                                    candidateCockpit,
                                    candidateControl))
                            {
                                continue;
                            }

                            var score =
                                (candidateEngine - currentEngine).sqrMagnitude +
                                (candidateCockpit - currentCockpit).sqrMagnitude +
                                (candidateControl - currentControl).sqrMagnitude;
                            if (score >= bestScore)
                            {
                                continue;
                            }

                            found = true;
                            bestScore = score;
                            bestEngine = candidateEngine;
                            bestCockpit = candidateCockpit;
                            bestControl = candidateControl;
                        }
                    }
                }
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    "No exact one-meter tri-room layout preserves diagram ordering without room overlap.");
            }

            return new TriRoomUniformPlacement(
                WithHorizontal(engineRoom.position, bestEngine),
                WithHorizontal(cockpit.position, bestCockpit),
                WithHorizontal(controlRoom.position, bestControl),
                targetDistance,
                bestScore);
        }

        private static float GetCorridorWorldVisibleLength(Transform module)
        {
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            return localBounds.size.x * module.TransformVector(Vector3.right).magnitude;
        }

        private static Vector2 Horizontal(Vector3 value)
        {
            return new Vector2(value.x, value.z);
        }

        private static Vector3 WithHorizontal(Vector3 original, Vector2 horizontal)
        {
            return new Vector3(horizontal.x, original.y, horizontal.y);
        }

        private static bool HasTriRoomDiagramOrdering(
            Vector2 engineRoom,
            Vector2 cockpit,
            Vector2 controlRoom)
        {
            const float orderingMargin = 5f;
            return engineRoom.x + orderingMargin < cockpit.x &&
                cockpit.x + orderingMargin < controlRoom.x &&
                engineRoom.y + orderingMargin < cockpit.y &&
                controlRoom.y + orderingMargin < cockpit.y;
        }

        private static bool HasCandidateRoomOverlap(
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Bounds> currentBounds,
            Vector2 candidateEngine,
            Vector2 candidateCockpit,
            Vector2 candidateControl)
        {
            var candidateBounds = new Dictionary<string, Bounds>(StringComparer.Ordinal)
            {
                {
                    "EngineRoom",
                    TranslateBoundsHorizontally(
                        currentBounds["EngineRoom"],
                        candidateEngine - Horizontal(rooms["EngineRoom"].transform.position))
                },
                {
                    "Cockpit",
                    TranslateBoundsHorizontally(
                        currentBounds["Cockpit"],
                        candidateCockpit - Horizontal(rooms["Cockpit"].transform.position))
                },
                {
                    "ControlRoom",
                    TranslateBoundsHorizontally(
                        currentBounds["ControlRoom"],
                        candidateControl - Horizontal(rooms["ControlRoom"].transform.position))
                }
            };

            var targetIds = new[] { "EngineRoom", "Cockpit", "ControlRoom" };
            for (var leftIndex = 0; leftIndex < targetIds.Length; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < targetIds.Length; rightIndex++)
                {
                    if (HorizontalBoundsOverlap(
                            candidateBounds[targetIds[leftIndex]],
                            candidateBounds[targetIds[rightIndex]]))
                    {
                        return true;
                    }
                }

                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var otherId = RoomDefinitions[roomIndex].Id;
                    if (otherId == "EngineRoom" || otherId == "Cockpit" || otherId == "ControlRoom")
                    {
                        continue;
                    }

                    if (HorizontalBoundsOverlap(
                            candidateBounds[targetIds[leftIndex]],
                            currentBounds[otherId]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static Bounds TranslateBoundsHorizontally(Bounds source, Vector2 translation)
        {
            source.center += new Vector3(translation.x, 0f, translation.y);
            return source;
        }

        private static bool HorizontalBoundsOverlap(Bounds left, Bounds right)
        {
            return left.min.x < right.max.x && left.max.x > right.min.x &&
                left.min.z < right.max.z && left.max.z > right.min.z;
        }

        private static void RequireTriRoomDiagramOrdering(
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            if (!HasTriRoomDiagramOrdering(
                    Horizontal(rooms["EngineRoom"].transform.position),
                    Horizontal(rooms["Cockpit"].transform.position),
                    Horizontal(rooms["ControlRoom"].transform.position)))
            {
                throw new InvalidOperationException(
                    "Tri-room placement no longer preserves EngineRoom-left, Cockpit-top, ControlRoom-right ordering.");
            }
        }

        private static void RequireNoTriRoomRoomOverlap(
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            var targetIds = new[] { "EngineRoom", "Cockpit", "ControlRoom" };
            for (var targetIndex = 0; targetIndex < targetIds.Length; targetIndex++)
            {
                var targetId = targetIds[targetIndex];
                var targetBounds = CalculateVisibleBounds(rooms[targetId]);
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var otherId = RoomDefinitions[roomIndex].Id;
                    if (string.Equals(targetId, otherId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!HorizontalBoundsOverlap(
                            targetBounds,
                            CalculateVisibleBounds(rooms[otherId])))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        "Tri-room placement has a horizontal room overlap: " +
                        targetId + " with " + otherId);
                }
            }
        }

        private static void RequireNoSelectedTriRoomOverlap(
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            var targetIds = new[] { "EngineRoom", "Cockpit", "ControlRoom" };
            for (var firstIndex = 0; firstIndex < targetIds.Length; firstIndex++)
            {
                var firstId = targetIds[firstIndex];
                var firstBounds = CalculateVisibleBounds(rooms[firstId]);
                for (var secondIndex = firstIndex + 1;
                     secondIndex < targetIds.Length;
                     secondIndex++)
                {
                    var secondId = targetIds[secondIndex];
                    if (!HorizontalBoundsOverlap(
                            firstBounds,
                            CalculateVisibleBounds(rooms[secondId])))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        "Selected tri-room placement has a horizontal overlap: " +
                        firstId + " with " + secondId);
                }
            }
        }

        private static void RequireTriRoomRoomRotations(
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            foreach (var roomId in new[] { "EngineRoom", "Cockpit", "ControlRoom" })
            {
                var expectedRotation = Quaternion.Euler(
                    0f,
                    GetRoomDefinition(roomId).LayoutYawDegrees,
                    0f);
                var angle = Quaternion.Angle(
                    expectedRotation,
                    rooms[roomId].transform.rotation);
                if (angle > RotationTolerance)
                {
                    throw new InvalidOperationException(
                        roomId + " does not match its approved layout rotation. " +
                        "ExpectedYaw=" + Float(GetRoomDefinition(roomId).LayoutYawDegrees) +
                        "; RotationErrorDegrees=" + Float(angle));
                }
            }
        }

        private static EntranceConnectionAnchor GetTriRoomFlushEntranceAnchor(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            string roomId,
            string otherRoomId)
        {
            var anchor = GetTriRoomEntranceAnchor(scene, rooms, roomId, otherRoomId);
            var floorY = GetMainFloorTop(rooms[roomId]);
            var point = anchor.Point;
            point.y = floorY;
            return new EntranceConnectionAnchor(anchor.Label, point, anchor.Outward, floorY);
        }

        private static void AppendTriRoomFlushSource(
            StringBuilder report,
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform module,
            string corridorId,
            string fromRoomId,
            string toRoomId)
        {
            var from = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                fromRoomId,
                toRoomId);
            var to = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                toRoomId,
                fromRoomId);
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var direction = HorizontalDirection(to.Point - from.Point);
            var lowEnd = module.TransformPoint(new Vector3(
                localBounds.min.x,
                localBounds.center.y,
                localBounds.center.z));
            var highEnd = module.TransformPoint(new Vector3(
                localBounds.max.x,
                localBounds.center.y,
                localBounds.center.z));
            var entranceDistance = HorizontalDistance(from.Point, to.Point);
            var worldLength = HorizontalDistance(lowEnd, highEnd);
            report.AppendLine(
                corridorId +
                "|From=" + from.Label +
                "|To=" + to.Label +
                "|FromPoint=" + Vector(from.Point) +
                "|ToPoint=" + Vector(to.Point) +
                "|FromOutward=" + Vector(from.Outward) +
                "|ToOutward=" + Vector(to.Outward) +
                "|EntranceDistance=" + Float(entranceDistance) +
                "|CurrentWorldLength=" + Float(worldLength) +
                "|RequiredWorldLength=" + Float(entranceDistance) +
                "|CurrentLowGap=" + Float(Vector3.Dot(lowEnd - from.Point, direction)) +
                "|CurrentHighGap=" + Float(Vector3.Dot(to.Point - highEnd, direction)) +
                "|FromEntranceMeetingAngle=" +
                    Float(CalculateCorridorEntranceMeetingAngle(module, from, true)) +
                "|ToEntranceMeetingAngle=" +
                    Float(CalculateCorridorEntranceMeetingAngle(module, to, false)) +
                "|FromFloor=" + Float(from.FloorY) +
                "|ToFloor=" + Float(to.FloorY) +
                "|CurrentCorridorFloor=" + Float(RequireUniqueCorridorFloorRenderer(module).bounds.max.y));
        }

        private static void ResizeAndAlignTriRoomCorridorFlush(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform module,
            Transform ceilingRoot,
            string corridorId,
            string fromRoomId,
            string toRoomId,
            StringBuilder report,
            float toEntrancePlaneGap = 0f)
        {
            var entranceFrom = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                fromRoomId,
                toRoomId);
            var entranceTo = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                toRoomId,
                fromRoomId);
            GetClearanceAdjustedCorridorAnchors(
                module,
                entranceFrom,
                entranceTo,
                out var from,
                out var to);
            to = AdjustCorridorToAnchorForEntrancePlaneGap(
                module,
                from,
                to,
                entranceTo,
                toEntrancePlaneGap);
            var entranceDistance = HorizontalDistance(entranceFrom.Point, entranceTo.Point);
            var corridorDistance = HorizontalDistance(from.Point, to.Point);
            var worldScaleAlongLength = module.TransformVector(Vector3.right).magnitude;
            if (worldScaleAlongLength <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " has an invalid longitudinal world scale.");
            }

            var beforeLocalLength = CalculateVisibleBoundsInLocalSpace(
                module.gameObject,
                module).size.x;
            var targetLocalLength = corridorDistance / worldScaleAlongLength;
            ResizeHorizontalCorridorModule(module, ceilingRoot, targetLocalLength);
            AlignTriRoomCorridorFlush(
                module,
                ceilingRoot,
                corridorId,
                from,
                to);
            var afterLocalLength = CalculateVisibleBoundsInLocalSpace(
                module.gameObject,
                module).size.x;
            report.AppendLine(
                corridorId +
                "|From=" + entranceFrom.Label +
                "|To=" + entranceTo.Label +
                "|EntranceDistance=" + Float(entranceDistance) +
                "|CorridorCenterlineDistance=" + Float(corridorDistance) +
                "|FromCenterOffset=" + Float(HorizontalDistance(entranceFrom.Point, from.Point)) +
                "|ToCenterOffset=" + Float(HorizontalDistance(entranceTo.Point, to.Point)) +
                "|LocalLengthBefore=" + Float(beforeLocalLength) +
                "|LocalLengthAfter=" + Float(afterLocalLength) +
                "|WorldLengthAfter=" + Float(afterLocalLength * worldScaleAlongLength) +
                "|RequestedToEntrancePlaneGap=" + Float(toEntrancePlaneGap) +
                "|ActualToEntrancePlaneGap=" +
                    Float(CalculateCorridorEndPlaneClearance(module, entranceTo, false)) +
                "|GeneratedRibs=" + CountGeneratedHorizontalCorridorRibs(module) +
                "|Position=" + Vector(module.position) +
                "|Rotation=" + QuaternionText(module.rotation));
        }

        private static void GetClearanceAdjustedCorridorAnchors(
            Transform module,
            EntranceConnectionAnchor entranceFrom,
            EntranceConnectionAnchor entranceTo,
            out EntranceConnectionAnchor corridorFrom,
            out EntranceConnectionAnchor corridorTo)
        {
            var halfWidth = GetCorridorWorldOutsideWidth(module) * 0.5f;
            var fromPoint = entranceFrom.Point;
            var toPoint = entranceTo.Point;
            for (var iteration = 0; iteration < 4; iteration++)
            {
                var direction = HorizontalDirection(toPoint - fromPoint);
                var across = HorizontalDirection(Vector3.Cross(direction, Vector3.up));
                var fromClearance =
                    (halfWidth * Mathf.Abs(Vector3.Dot(across, entranceFrom.Outward))) +
                    TriRoomEntranceEdgeSafety;
                var toClearance =
                    (halfWidth * Mathf.Abs(Vector3.Dot(across, entranceTo.Outward))) +
                    TriRoomEntranceEdgeSafety;
                fromPoint = entranceFrom.Point + (entranceFrom.Outward * fromClearance);
                toPoint = entranceTo.Point + (entranceTo.Outward * toClearance);
            }

            fromPoint.y = entranceFrom.Point.y;
            toPoint.y = entranceTo.Point.y;
            corridorFrom = new EntranceConnectionAnchor(
                entranceFrom.Label + " full-width clearance",
                fromPoint,
                entranceFrom.Outward,
                entranceFrom.FloorY);
            corridorTo = new EntranceConnectionAnchor(
                entranceTo.Label + " full-width clearance",
                toPoint,
                entranceTo.Outward,
                entranceTo.FloorY);
        }

        private static EntranceConnectionAnchor AdjustCorridorToAnchorForEntrancePlaneGap(
            Transform module,
            EntranceConnectionAnchor corridorFrom,
            EntranceConnectionAnchor corridorTo,
            EntranceConnectionAnchor entranceTo,
            float requiredPlaneGap)
        {
            if (requiredPlaneGap <= PositionTolerance)
            {
                return corridorTo;
            }

            var direction = HorizontalDirection(corridorTo.Point - corridorFrom.Point);
            var across = HorizontalDirection(Vector3.Cross(direction, Vector3.up));
            var halfWidth = GetCorridorWorldOutsideWidth(module) * 0.5f;
            var currentMinimumPlaneGap =
                Vector3.Dot(corridorTo.Point - entranceTo.Point, entranceTo.Outward) -
                (halfWidth * Mathf.Abs(Vector3.Dot(across, entranceTo.Outward)));
            var retreatProjection = Vector3.Dot(-direction, entranceTo.Outward);
            if (retreatProjection <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Corridor cannot retreat from " + entranceTo.Label +
                    " while preserving its current direction.");
            }

            var retreatDistance =
                (requiredPlaneGap - currentMinimumPlaneGap) / retreatProjection;
            if (retreatDistance < -TriRoomFlushConnectionTolerance)
            {
                throw new InvalidOperationException(
                    "Requested entrance-plane gap is smaller than the existing full-width clearance. " +
                    "Requested=" + Float(requiredPlaneGap) +
                    "; Existing=" + Float(currentMinimumPlaneGap));
            }

            var adjustedPoint = corridorTo.Point -
                (direction * Mathf.Max(0f, retreatDistance));
            return new EntranceConnectionAnchor(
                corridorTo.Label + " retreated for " + Float(requiredPlaneGap) + "m plane gap",
                adjustedPoint,
                corridorTo.Outward,
                corridorTo.FloorY);
        }

        private static void AlignTriRoomCorridorFlush(
            Transform module,
            Transform ceilingRoot,
            string corridorId,
            EntranceConnectionAnchor from,
            EntranceConnectionAnchor to)
        {
            var direction = HorizontalDirection(to.Point - from.Point);
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
            var midpoint = (from.Point + to.Point) * 0.5f;
            var desiredForward = Vector3.Cross(direction, Vector3.up).normalized;
            module.rotation = Quaternion.LookRotation(desiredForward, Vector3.up);
            module.position = new Vector3(midpoint.x, module.position.y, midpoint.z);
            var visibleCenter = module.TransformPoint(localBounds.center);
            module.position += new Vector3(
                midpoint.x - visibleCenter.x,
                0f,
                midpoint.z - visibleCenter.z);
            var targetFloorY = (from.FloorY + to.FloorY) * 0.5f;
            var floorRenderer = RequireUniqueCorridorFloorRenderer(module);
            module.position += Vector3.up * (targetFloorY - floorRenderer.bounds.max.y);
            ApplyCorridorCeilingFollowers(module, followers);
            EditorUtility.SetDirty(module);
            for (var followerIndex = 0; followerIndex < followers.Count; followerIndex++)
            {
                EditorUtility.SetDirty(followers[followerIndex].Transform);
            }

            var adjustedBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var lowEnd = module.TransformPoint(new Vector3(
                adjustedBounds.min.x,
                adjustedBounds.center.y,
                adjustedBounds.center.z));
            var highEnd = module.TransformPoint(new Vector3(
                adjustedBounds.max.x,
                adjustedBounds.center.y,
                adjustedBounds.center.z));
            var lowGap = Vector3.Dot(lowEnd - from.Point, direction);
            var highGap = Vector3.Dot(to.Point - highEnd, direction);
            var lowLateral = HorizontalPointLineDistance(lowEnd, from.Point, direction);
            var highLateral = HorizontalPointLineDistance(highEnd, from.Point, direction);
            if (Mathf.Abs(lowGap) > TriRoomFlushConnectionTolerance ||
                Mathf.Abs(highGap) > TriRoomFlushConnectionTolerance ||
                lowLateral > TriRoomFlushConnectionTolerance ||
                highLateral > TriRoomFlushConnectionTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " did not connect flush to both entrance planes. " +
                    "LowGap=" + Float(lowGap) + "; HighGap=" + Float(highGap) +
                    "; LowLateral=" + Float(lowLateral) +
                    "; HighLateral=" + Float(highLateral));
            }
        }

        private static void AppendTriRoomFlushInspection(
            StringBuilder report,
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform module,
            string corridorId,
            string fromRoomId,
            string toRoomId,
            float expectedToEntrancePlaneGap = 0f,
            float maximumMeetingAngle = MaximumRoomEntranceMeetingAngle)
        {
            var entranceFrom = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                fromRoomId,
                toRoomId);
            var entranceTo = GetTriRoomFlushEntranceAnchor(
                scene,
                rooms,
                toRoomId,
                fromRoomId);
            GetClearanceAdjustedCorridorAnchors(
                module,
                entranceFrom,
                entranceTo,
                out var from,
                out var to);
            to = AdjustCorridorToAnchorForEntrancePlaneGap(
                module,
                from,
                to,
                entranceTo,
                expectedToEntrancePlaneGap);
            var direction = HorizontalDirection(to.Point - from.Point);
            var axis = HorizontalDirection(module.right);
            var axisError = Vector3.Angle(axis, direction);
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var lowEnd = module.TransformPoint(new Vector3(
                localBounds.min.x,
                localBounds.center.y,
                localBounds.center.z));
            var highEnd = module.TransformPoint(new Vector3(
                localBounds.max.x,
                localBounds.center.y,
                localBounds.center.z));
            var lowGap = Vector3.Dot(lowEnd - from.Point, direction);
            var highGap = Vector3.Dot(to.Point - highEnd, direction);
            var lowLateral = HorizontalPointLineDistance(lowEnd, from.Point, direction);
            var highLateral = HorizontalPointLineDistance(highEnd, from.Point, direction);
            var floorTop = RequireUniqueCorridorFloorRenderer(module).bounds.max.y;
            var targetFloorY = (from.FloorY + to.FloorY) * 0.5f;
            var corridorFloorTargetError = Mathf.Abs(floorTop - targetFloorY);
            var entranceFloorDifference = Mathf.Abs(from.FloorY - to.FloorY);
            var entranceDistance = HorizontalDistance(entranceFrom.Point, entranceTo.Point);
            var corridorDistance = HorizontalDistance(from.Point, to.Point);
            var worldLength = HorizontalDistance(lowEnd, highEnd);
            var fromMeetingAngle = CalculateCorridorEntranceMeetingAngle(
                module,
                entranceFrom,
                true);
            var toMeetingAngle = CalculateCorridorEntranceMeetingAngle(
                module,
                entranceTo,
                false);
            var actualToEntrancePlaneGap =
                CalculateCorridorEndPlaneClearance(module, entranceTo, false);
            if (axisError > TriRoomFlushConnectionTolerance ||
                Mathf.Abs(lowGap) > TriRoomFlushConnectionTolerance ||
                Mathf.Abs(highGap) > TriRoomFlushConnectionTolerance ||
                lowLateral > TriRoomFlushConnectionTolerance ||
                highLateral > TriRoomFlushConnectionTolerance ||
                corridorFloorTargetError > TriRoomFlushConnectionTolerance ||
                Mathf.Abs(worldLength - corridorDistance) > TriRoomFlushConnectionTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " failed flush entrance inspection. " +
                    "AxisError=" + Float(axisError) +
                    "; LowGap=" + Float(lowGap) +
                    "; HighGap=" + Float(highGap) +
                    "; Lateral=" + Float(Mathf.Max(lowLateral, highLateral)) +
                    "; CorridorFloorTargetError=" + Float(corridorFloorTargetError) +
                    "; LengthError=" + Float(Mathf.Abs(worldLength - corridorDistance)));
            }

            if (expectedToEntrancePlaneGap > PositionTolerance &&
                Mathf.Abs(actualToEntrancePlaneGap - expectedToEntrancePlaneGap) >
                    TriRoomFlushConnectionTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " failed the requested to-entrance plane gap inspection. " +
                    "Expected=" + Float(expectedToEntrancePlaneGap) +
                    "; Actual=" + Float(actualToEntrancePlaneGap));
            }

            if ((corridorId == "SC-H01" || corridorId == "SC-H02") &&
                (fromMeetingAngle > maximumMeetingAngle + PositionTolerance ||
                 toMeetingAngle > maximumMeetingAngle + PositionTolerance))
            {
                throw new InvalidOperationException(
                    corridorId + " failed the room-entrance meeting-angle inspection. " +
                    "FromAngle=" + Float(fromMeetingAngle) +
                    "; ToAngle=" + Float(toMeetingAngle) +
                    "; Maximum=" + Float(maximumMeetingAngle));
            }

            RequireCorridorEndOutsideEntrancePlane(
                module,
                entranceFrom,
                true,
                corridorId + " from end");
            RequireCorridorEndOutsideEntrancePlane(
                module,
                entranceTo,
                false,
                corridorId + " to end");

            report.AppendLine(
                corridorId +
                "|From=" + from.Label +
                "|To=" + to.Label +
                "|EntranceDistance=" + Float(entranceDistance) +
                "|CorridorCenterlineDistance=" + Float(corridorDistance) +
                "|WorldVisibleLength=" + Float(worldLength) +
                "|LowEndGap=" + Float(lowGap) +
                "|HighEndGap=" + Float(highGap) +
                "|LateralErrorMax=" + Float(Mathf.Max(lowLateral, highLateral)) +
                "|AxisErrorDegrees=" + Float(axisError) +
                "|FromEntranceMeetingAngle=" + Float(fromMeetingAngle) +
                "|ToEntranceMeetingAngle=" + Float(toMeetingAngle) +
                "|CorridorFloorTop=" + Float(floorTop) +
                "|EntranceFloorDifference=" + Float(entranceFloorDifference) +
                "|CorridorFloorTargetError=" + Float(corridorFloorTargetError) +
                "|FromEntrancePlanePenetration=" +
                    Float(CalculateCorridorEndPlanePenetration(module, entranceFrom, true)) +
                "|ToEntrancePlanePenetration=" +
                    Float(CalculateCorridorEndPlanePenetration(module, entranceTo, false)) +
                "|RequestedToEntrancePlaneGap=" + Float(expectedToEntrancePlaneGap) +
                "|ActualToEntrancePlaneGap=" + Float(actualToEntrancePlaneGap) +
                "|EntranceOverlap=False");
        }

        private static void RequireNoTriRoomTargetCorridorCrossing(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules)
        {
            var segments = new[]
            {
                new CorridorConnectionSegment(
                    "SC-H01",
                    GetTriRoomFlushEntranceAnchor(scene, rooms, "EngineRoom", "Cockpit").Point,
                    GetTriRoomFlushEntranceAnchor(scene, rooms, "Cockpit", "EngineRoom").Point,
                    GetCorridorWorldOutsideWidth(modules["SC-H01"])),
                new CorridorConnectionSegment(
                    "SC-H02",
                    GetTriRoomFlushEntranceAnchor(scene, rooms, "Cockpit", "ControlRoom").Point,
                    GetTriRoomFlushEntranceAnchor(scene, rooms, "ControlRoom", "Cockpit").Point,
                    GetCorridorWorldOutsideWidth(modules["SC-H02"])),
                new CorridorConnectionSegment(
                    "SC-H05",
                    GetTriRoomFlushEntranceAnchor(scene, rooms, "EngineRoom", "ControlRoom").Point,
                    GetTriRoomFlushEntranceAnchor(scene, rooms, "ControlRoom", "EngineRoom").Point,
                    GetCorridorWorldOutsideWidth(modules["SC-H05"]))
            };

            for (var firstIndex = 0; firstIndex < segments.Length; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1;
                     secondIndex < segments.Length;
                     secondIndex++)
                {
                    var first = segments[firstIndex];
                    var second = segments[secondIndex];
                    var distance = HorizontalSegmentDistance(
                        first.From,
                        first.To,
                        second.From,
                        second.To);
                    var required = (first.Width + second.Width) * 0.5f;
                    if (distance + TriRoomFlushConnectionTolerance < required)
                    {
                        throw new InvalidOperationException(
                            "Target corridors overlap in plan view: " +
                            first.Id + " and " + second.Id +
                            ". Distance=" + Float(distance) +
                            "; Required=" + Float(required));
                    }
                }
            }
        }

        private static float GetCorridorWorldOutsideWidth(Transform module)
        {
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            return localBounds.size.z * module.TransformVector(Vector3.forward).magnitude;
        }

        private static float CalculateCorridorEndPlanePenetration(
            Transform module,
            EntranceConnectionAnchor entrance,
            bool lowEnd)
        {
            return Mathf.Max(
                0f,
                -CalculateCorridorEndPlaneClearance(module, entrance, lowEnd));
        }

        private static float CalculateCorridorEndPlaneClearance(
            Transform module,
            EntranceConnectionAnchor entrance,
            bool lowEnd)
        {
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var localX = lowEnd ? localBounds.min.x : localBounds.max.x;
            var minimumProjection = float.PositiveInfinity;
            for (var zIndex = 0; zIndex < 2; zIndex++)
            {
                var localPoint = new Vector3(
                    localX,
                    localBounds.center.y,
                    zIndex == 0 ? localBounds.min.z : localBounds.max.z);
                var worldPoint = module.TransformPoint(localPoint);
                minimumProjection = Mathf.Min(
                    minimumProjection,
                    Vector3.Dot(worldPoint - entrance.Point, entrance.Outward));
            }

            return minimumProjection;
        }

        private static float CalculateCorridorEntranceMeetingAngle(
            Transform module,
            EntranceConnectionAnchor entrance,
            bool lowEnd)
        {
            var corridorAwayFromRoom = HorizontalDirection(
                lowEnd ? module.right : -module.right);
            return Vector3.Angle(corridorAwayFromRoom, entrance.Outward);
        }

        private static void RequireCorridorEndOutsideEntrancePlane(
            Transform module,
            EntranceConnectionAnchor entrance,
            bool lowEnd,
            string label)
        {
            var penetration = CalculateCorridorEndPlanePenetration(
                module,
                entrance,
                lowEnd);
            if (penetration > TriRoomFlushConnectionTolerance)
            {
                throw new InvalidOperationException(
                    label + " crosses its room entrance plane across the corridor footprint. " +
                    "Penetration=" + Float(penetration));
            }
        }

        private static void RequireAllTriRoomCorridorEntranceClearance(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules)
        {
            RequireCorridorEndOutsideEntrancePlane(
                modules["SC-H01"],
                GetTriRoomFlushEntranceAnchor(scene, rooms, "EngineRoom", "Cockpit"),
                true,
                "SC-H01 EngineRoom end");
            RequireCorridorEndOutsideEntrancePlane(
                modules["SC-H01"],
                GetTriRoomFlushEntranceAnchor(scene, rooms, "Cockpit", "EngineRoom"),
                false,
                "SC-H01 Cockpit end");
            RequireCorridorEndOutsideEntrancePlane(
                modules["SC-H02"],
                GetTriRoomFlushEntranceAnchor(scene, rooms, "Cockpit", "ControlRoom"),
                true,
                "SC-H02 Cockpit end");
            RequireCorridorEndOutsideEntrancePlane(
                modules["SC-H02"],
                GetTriRoomFlushEntranceAnchor(scene, rooms, "ControlRoom", "Cockpit"),
                false,
                "SC-H02 ControlRoom end");
            RequireCorridorEndOutsideEntrancePlane(
                modules["SC-H05"],
                GetTriRoomFlushEntranceAnchor(scene, rooms, "EngineRoom", "ControlRoom"),
                true,
                "SC-H05 EngineRoom end");
            RequireCorridorEndOutsideEntrancePlane(
                modules["SC-H05"],
                GetTriRoomFlushEntranceAnchor(scene, rooms, "ControlRoom", "EngineRoom"),
                false,
                "SC-H05 ControlRoom end");
        }

        private static float HorizontalSegmentDistance(
            Vector3 firstStart,
            Vector3 firstEnd,
            Vector3 secondStart,
            Vector3 secondEnd)
        {
            var a = new Vector2(firstStart.x, firstStart.z);
            var b = new Vector2(firstEnd.x, firstEnd.z);
            var c = new Vector2(secondStart.x, secondStart.z);
            var d = new Vector2(secondEnd.x, secondEnd.z);
            if (SegmentsIntersect(a, b, c, d))
            {
                return 0f;
            }

            return Mathf.Min(
                Mathf.Min(PointSegmentDistance(a, c, d), PointSegmentDistance(b, c, d)),
                Mathf.Min(PointSegmentDistance(c, a, b), PointSegmentDistance(d, a, b)));
        }

        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var abC = Cross2D(b - a, c - a);
            var abD = Cross2D(b - a, d - a);
            var cdA = Cross2D(d - c, a - c);
            var cdB = Cross2D(d - c, b - c);
            return abC * abD < 0f && cdA * cdB < 0f;
        }

        private static float Cross2D(Vector2 left, Vector2 right)
        {
            return (left.x * right.y) - (left.y * right.x);
        }

        private static float PointSegmentDistance(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var denominator = segment.sqrMagnitude;
            if (denominator <= 0.000001f)
            {
                return Vector2.Distance(point, start);
            }

            var parameter = Mathf.Clamp01(Vector2.Dot(point - start, segment) / denominator);
            return Vector2.Distance(point, start + (segment * parameter));
        }

        private static void AlignTriRoomCorridor(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform module,
            Transform ceilingRoot,
            string corridorId,
            EntranceConnectionAnchor from,
            EntranceConnectionAnchor to)
        {
            var direction = HorizontalDirection(to.Point - from.Point);
            var entranceDistance = HorizontalDistance(from.Point, to.Point);
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var worldScaleAlongLength = module.TransformVector(Vector3.right).magnitude;
            var worldLength = localBounds.size.x * worldScaleAlongLength;
            var remainingGap = entranceDistance - worldLength;
            if (remainingGap <= 0.05f)
            {
                throw new InvalidOperationException(
                    corridorId + " cannot be aligned without forced overlap. EntranceDistance=" +
                    Float(entranceDistance) + "; CorridorWorldLength=" + Float(worldLength));
            }

            var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
            var midpoint = (from.Point + to.Point) * 0.5f;
            var desiredForward = Vector3.Cross(direction, Vector3.up).normalized;
            module.rotation = Quaternion.LookRotation(desiredForward, Vector3.up);
            module.position = new Vector3(midpoint.x, module.position.y, midpoint.z);

            var visibleCenter = module.TransformPoint(localBounds.center);
            module.position += new Vector3(
                midpoint.x - visibleCenter.x,
                0f,
                midpoint.z - visibleCenter.z);
            var floorRenderer = RequireUniqueCorridorFloorRenderer(module);
            var targetFloorY = (from.FloorY + to.FloorY) * 0.5f;
            module.position += Vector3.up * (targetFloorY - floorRenderer.bounds.max.y);

            for (var followerIndex = 0; followerIndex < followers.Count; followerIndex++)
            {
                var follower = followers[followerIndex];
                follower.Transform.position = module.TransformPoint(follower.ModuleLocalPosition);
                follower.Transform.rotation = module.rotation * follower.ModuleLocalRotation;
                EditorUtility.SetDirty(follower.Transform);
            }

            EditorUtility.SetDirty(module);
            var adjustedBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            if (Mathf.Abs(adjustedBounds.size.x - localBounds.size.x) > PositionTolerance ||
                Mathf.Abs(adjustedBounds.size.z - localBounds.size.z) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " dimensions changed while aligning its root transform.");
            }

            var lowEnd = module.TransformPoint(new Vector3(
                adjustedBounds.min.x,
                adjustedBounds.center.y,
                adjustedBounds.center.z));
            var highEnd = module.TransformPoint(new Vector3(
                adjustedBounds.max.x,
                adjustedBounds.center.y,
                adjustedBounds.center.z));
            var lowClearance = Vector3.Dot(lowEnd - from.Point, direction);
            var highClearance = Vector3.Dot(to.Point - highEnd, direction);
            if (lowClearance <= 0.01f || highClearance <= 0.01f)
            {
                throw new InvalidOperationException(
                    corridorId + " did not preserve positive connector gaps after alignment. " +
                    "Low=" + Float(lowClearance) + "; High=" + Float(highClearance));
            }
        }

        private static Renderer RequireUniqueCorridorFloorRenderer(Transform module)
        {
            Renderer match = null;
            var renderers = module.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (renderer.name.IndexOf("straight floor slab", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "Duplicate straight floor slab renderer below " + module.name);
                }

                match = renderer;
            }

            return match ?? throw new InvalidOperationException(
                "Missing straight floor slab renderer below " + module.name);
        }

        private static void AppendTriRoomConnectionInspection(
            StringBuilder report,
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform module,
            string corridorId,
            EntranceConnectionAnchor from,
            EntranceConnectionAnchor to)
        {
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            if (Mathf.Abs(localBounds.size.x - 20f) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " no longer preserves its approved 20-unit local length. Length=" +
                    Float(localBounds.size.x));
            }

            var direction = HorizontalDirection(to.Point - from.Point);
            var axis = HorizontalDirection(module.right);
            var axisError = Vector3.Angle(axis, direction);
            if (axisError > 0.01f)
            {
                throw new InvalidOperationException(
                    corridorId + " centerline is not aligned to its two entrance anchors. Error=" +
                    Float(axisError));
            }

            var lowEnd = module.TransformPoint(new Vector3(
                localBounds.min.x,
                localBounds.center.y,
                localBounds.center.z));
            var highEnd = module.TransformPoint(new Vector3(
                localBounds.max.x,
                localBounds.center.y,
                localBounds.center.z));
            var lowClearance = Vector3.Dot(lowEnd - from.Point, direction);
            var highClearance = Vector3.Dot(to.Point - highEnd, direction);
            var lowLateralError = HorizontalPointLineDistance(lowEnd, from.Point, direction);
            var highLateralError = HorizontalPointLineDistance(highEnd, from.Point, direction);
            if (lowClearance <= 0.01f || highClearance <= 0.01f ||
                lowLateralError > 0.01f || highLateralError > 0.01f)
            {
                throw new InvalidOperationException(
                    corridorId + " failed non-overlap connector-gap inspection. LowClearance=" +
                    Float(lowClearance) + "; HighClearance=" + Float(highClearance) +
                    "; LowLateralError=" + Float(lowLateralError) +
                    "; HighLateralError=" + Float(highLateralError));
            }

            var entranceDistance = HorizontalDistance(from.Point, to.Point);
            var worldLength = HorizontalDistance(lowEnd, highEnd);
            var floorTop = RequireUniqueCorridorFloorRenderer(module).bounds.max.y;
            report.AppendLine(
                corridorId +
                "|From=" + from.Label +
                "|To=" + to.Label +
                "|LocalVisibleLength=" + Float(localBounds.size.x) +
                "|WorldVisibleLength=" + Float(worldLength) +
                "|EntranceDistance=" + Float(entranceDistance) +
                "|LowConnectorGap=" + Float(lowClearance) +
                "|HighConnectorGap=" + Float(highClearance) +
                "|GapBalanceError=" + Float(Mathf.Abs(lowClearance - highClearance)) +
                "|AxisErrorDegrees=" + Float(axisError) +
                "|LateralErrorMax=" + Float(Mathf.Max(lowLateralError, highLateralError)) +
                "|CorridorFloorTop=" + Float(floorTop) +
                "|EntranceFloorDifference=" + Float(Mathf.Abs(from.FloorY - to.FloorY)) +
                "|MeshOverlap=False" +
                "|Position=" + Vector(module.position) +
                "|Rotation=" + QuaternionText(module.rotation));
        }

        private static void AppendTriRoomUniformGapInspection(
            StringBuilder report,
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform module,
            string corridorId,
            EntranceConnectionAnchor from,
            EntranceConnectionAnchor to,
            ICollection<float> gaps)
        {
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            if (Mathf.Abs(localBounds.size.x - 20f) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " no longer preserves its approved 20-unit local length. Length=" +
                    Float(localBounds.size.x));
            }

            var direction = HorizontalDirection(to.Point - from.Point);
            var axis = HorizontalDirection(module.right);
            var axisError = Vector3.Angle(axis, direction);
            var lowEnd = module.TransformPoint(new Vector3(
                localBounds.min.x,
                localBounds.center.y,
                localBounds.center.z));
            var highEnd = module.TransformPoint(new Vector3(
                localBounds.max.x,
                localBounds.center.y,
                localBounds.center.z));
            var lowGap = Vector3.Dot(lowEnd - from.Point, direction);
            var highGap = Vector3.Dot(to.Point - highEnd, direction);
            var lowLateralError = HorizontalPointLineDistance(lowEnd, from.Point, direction);
            var highLateralError = HorizontalPointLineDistance(highEnd, from.Point, direction);
            var maximumLateralError = Mathf.Max(lowLateralError, highLateralError);
            if (axisError > TriRoomUniformConnectorGapTolerance ||
                maximumLateralError > TriRoomUniformConnectorGapTolerance ||
                Mathf.Abs(lowGap - TriRoomUniformConnectorGap) >
                    TriRoomUniformConnectorGapTolerance ||
                Mathf.Abs(highGap - TriRoomUniformConnectorGap) >
                    TriRoomUniformConnectorGapTolerance)
            {
                throw new InvalidOperationException(
                    corridorId + " failed uniform one-meter gap inspection. LowGap=" +
                    Float(lowGap) + "; HighGap=" + Float(highGap) +
                    "; AxisError=" + Float(axisError) +
                    "; LateralError=" + Float(maximumLateralError));
            }

            gaps.Add(lowGap);
            gaps.Add(highGap);
            var entranceDistance = HorizontalDistance(from.Point, to.Point);
            var worldLength = HorizontalDistance(lowEnd, highEnd);
            var floorTop = RequireUniqueCorridorFloorRenderer(module).bounds.max.y;
            report.AppendLine(
                corridorId +
                "|From=" + from.Label +
                "|To=" + to.Label +
                "|LocalVisibleLength=" + Float(localBounds.size.x) +
                "|WorldVisibleLength=" + Float(worldLength) +
                "|EntranceDistance=" + Float(entranceDistance) +
                "|LowConnectorGap=" + Float(lowGap) +
                "|HighConnectorGap=" + Float(highGap) +
                "|GapBalanceError=" + Float(Mathf.Abs(lowGap - highGap)) +
                "|AxisErrorDegrees=" + Float(axisError) +
                "|LateralErrorMax=" + Float(maximumLateralError) +
                "|CorridorFloorTop=" + Float(floorTop) +
                "|EntranceFloorDifference=" + Float(Mathf.Abs(from.FloorY - to.FloorY)) +
                "|MeshOverlap=False" +
                "|Position=" + Vector(module.position) +
                "|Rotation=" + QuaternionText(module.rotation));
        }

        private static float HorizontalPointLineDistance(
            Vector3 point,
            Vector3 linePoint,
            Vector3 lineDirection)
        {
            var offset = point - linePoint;
            offset.y = 0f;
            var projected = lineDirection * Vector3.Dot(offset, lineDirection);
            return (offset - projected).magnitude;
        }

        private static string BuildCockpitOrientationProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot)
        {
            var builder = new StringBuilder(512 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                if (definition.Id != "Cockpit")
                {
                    AppendHierarchySignature(builder, room);
                    continue;
                }

                AppendPlanarMutableRootSignature(builder, room);
                var descendants = room.GetComponentsInChildren<Transform>(true);
                for (var descendantIndex = 1;
                     descendantIndex < descendants.Length;
                     descendantIndex++)
                {
                    AppendSingleTransformSignature(
                        builder,
                        descendants[descendantIndex],
                        false);
                }
            }

            builder.AppendLine("[CorridorRoot]");
            AppendHierarchySignature(builder, corridorRoot);
            return builder.ToString();
        }

        private static string BuildCockpitCorridorConnectionProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot)
        {
            var builder = new StringBuilder(512 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                if (definition.Id != "EngineRoom" &&
                    definition.Id != "ControlRoom")
                {
                    AppendHierarchySignature(builder, room);
                    continue;
                }

                AppendTranslationMutableRootSignature(builder, room);
                var descendants = room.GetComponentsInChildren<Transform>(true);
                for (var descendantIndex = 1;
                     descendantIndex < descendants.Length;
                     descendantIndex++)
                {
                    AppendSingleTransformSignature(
                        builder,
                        descendants[descendantIndex],
                        false);
                }
            }

            builder.AppendLine("[CorridorRoot]");
            AppendSingleTransformSignature(builder, corridorRoot, false);
            builder.AppendLine("[Corridors]");
            for (var corridorIndex = 0;
                 corridorIndex < CorridorDefinitions.Length;
                 corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                var mutableCorridor = definition.ModuleId == "SC-H01" ||
                    definition.ModuleId == "SC-H02" ||
                    definition.ModuleId == "SC-H05";
                if (!mutableCorridor)
                {
                    AppendHierarchySignature(builder, module);
                    AppendHierarchySignature(builder, followers[0].Transform);
                    continue;
                }

                AppendSingleTransformSignature(builder, module, true);
                var transforms = module.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 1; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    if (IsGeneratedHorizontalCorridorRib(target.name))
                    {
                        continue;
                    }

                    var mayMoveAlongLength =
                        target.name.Contains(" low end ") ||
                        target.name.Contains(" high end ");
                    var mayStretchAlongLength =
                        IsHorizontalCorridorStretchSurface(target.name);
                    builder.Append(GetHierarchyPath(module, target)).Append('|')
                        .Append(target.gameObject.activeSelf).Append('|');
                    if (!mayMoveAlongLength)
                    {
                        builder.Append("X=").Append(Float(target.localPosition.x)).Append('|');
                    }

                    builder.Append("YZ=")
                        .Append(Float(target.localPosition.y)).Append(',')
                        .Append(Float(target.localPosition.z)).Append('|')
                        .Append(QuaternionText(target.localRotation)).Append('|');
                    if (!mayStretchAlongLength)
                    {
                        builder.Append("ScaleX=").Append(Float(target.localScale.x)).Append('|');
                    }

                    builder.Append("ScaleYZ=")
                        .Append(Float(target.localScale.y)).Append(',')
                        .Append(Float(target.localScale.z)).Append('|')
                        .Append(BuildComponentSignature(target.gameObject)).AppendLine();
                }

                var ceiling = followers[0].Transform;
                builder.Append(ceiling.name).Append('|')
                    .Append(ceiling.gameObject.activeSelf).Append('|')
                    .Append("ScaleYZ=")
                    .Append(Float(ceiling.localScale.y)).Append(',')
                    .Append(Float(ceiling.localScale.z)).Append('|')
                    .Append(BuildComponentSignature(ceiling.gameObject)).AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildTriRoomEntranceConnectionProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot)
        {
            var builder = new StringBuilder(512 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                AppendHierarchySignature(builder, room);
            }

            builder.AppendLine("[CorridorRoot]");
            AppendSingleTransformSignature(builder, corridorRoot, false);
            builder.AppendLine("[Corridors]");
            for (var corridorIndex = 0;
                 corridorIndex < CorridorDefinitions.Length;
                 corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                if (definition.ModuleId != "SC-H02")
                {
                    AppendHierarchySignature(builder, module);
                    AppendHierarchySignature(builder, followers[0].Transform);
                    continue;
                }

                AppendSingleTransformSignature(builder, module, true);
                var transforms = module.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 1; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    if (IsGeneratedHorizontalCorridorRib(target.name))
                    {
                        continue;
                    }

                    var mayMoveAlongLength =
                        target.name.Contains(" low end ") ||
                        target.name.Contains(" high end ");
                    var mayStretchAlongLength =
                        IsHorizontalCorridorStretchSurface(target.name);
                    builder.Append(GetHierarchyPath(module, target)).Append('|')
                        .Append(target.gameObject.activeSelf).Append('|');
                    if (!mayMoveAlongLength)
                    {
                        builder.Append("X=").Append(Float(target.localPosition.x)).Append('|');
                    }

                    builder.Append("YZ=")
                        .Append(Float(target.localPosition.y)).Append(',')
                        .Append(Float(target.localPosition.z)).Append('|')
                        .Append(QuaternionText(target.localRotation)).Append('|');
                    if (!mayStretchAlongLength)
                    {
                        builder.Append("ScaleX=").Append(Float(target.localScale.x)).Append('|');
                    }

                    builder.Append("ScaleYZ=")
                        .Append(Float(target.localScale.y)).Append(',')
                        .Append(Float(target.localScale.z)).Append('|')
                        .Append(BuildComponentSignature(target.gameObject)).AppendLine();
                }

                var ceiling = followers[0].Transform;
                builder.Append(ceiling.name).Append('|')
                    .Append(ceiling.gameObject.activeSelf).Append('|')
                    .Append("ScaleYZ=")
                    .Append(Float(ceiling.localScale.y)).Append(',')
                    .Append(Float(ceiling.localScale.z)).Append('|')
                    .Append(BuildComponentSignature(ceiling.gameObject)).AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildTriRoomConnectionProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot,
            IReadOnlyDictionary<string, Transform> modules)
        {
            var builder = new StringBuilder(512 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                AppendHierarchySignature(builder, rooms[RoomDefinitions[roomIndex].Id].transform);
            }

            builder.AppendLine("[CorridorRoot]");
            AppendSingleTransformSignature(builder, corridorRoot, false);
            builder.AppendLine("[CorridorModules]");
            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                if (IsTriRoomTargetCorridor(definition.ModuleId))
                {
                    AppendSingleTransformSignature(builder, module, true);
                    var descendants = module.GetComponentsInChildren<Transform>(true);
                    for (var descendantIndex = 1; descendantIndex < descendants.Length; descendantIndex++)
                    {
                        AppendSingleTransformSignature(builder, descendants[descendantIndex], false);
                    }
                }
                else
                {
                    AppendHierarchySignature(builder, module);
                }
            }

            var ceilingRoot = corridorRoot.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            builder.AppendLine("[CorridorCeilings]");
            AppendSingleTransformSignature(builder, ceilingRoot, false);
            for (var childIndex = 0; childIndex < ceilingRoot.childCount; childIndex++)
            {
                var child = ceilingRoot.GetChild(childIndex);
                AppendSingleTransformSignature(
                    builder,
                    child,
                    IsTriRoomTargetCeilingFollower(child.name));
            }

            return builder.ToString();
        }

        private static string BuildTriRoomUniformGapProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot,
            IReadOnlyDictionary<string, Transform> modules)
        {
            var builder = new StringBuilder(512 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                if (definition.Id == "EngineRoom" ||
                    definition.Id == "Cockpit" ||
                    definition.Id == "ControlRoom")
                {
                    AppendTranslationMutableRootSignature(builder, room);
                    var descendants = room.GetComponentsInChildren<Transform>(true);
                    for (var descendantIndex = 1;
                         descendantIndex < descendants.Length;
                         descendantIndex++)
                    {
                        AppendSingleTransformSignature(
                            builder,
                            descendants[descendantIndex],
                            false);
                    }
                }
                else
                {
                    AppendHierarchySignature(builder, room);
                }
            }

            builder.AppendLine("[CorridorRoot]");
            AppendSingleTransformSignature(builder, corridorRoot, false);
            builder.AppendLine("[CorridorModules]");
            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                if (IsTriRoomTargetCorridor(definition.ModuleId))
                {
                    AppendSingleTransformSignature(builder, module, true);
                    var descendants = module.GetComponentsInChildren<Transform>(true);
                    for (var descendantIndex = 1;
                         descendantIndex < descendants.Length;
                         descendantIndex++)
                    {
                        AppendSingleTransformSignature(
                            builder,
                            descendants[descendantIndex],
                            false);
                    }
                }
                else
                {
                    AppendHierarchySignature(builder, module);
                }
            }

            var ceilingRoot = corridorRoot.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);
            builder.AppendLine("[CorridorCeilings]");
            AppendSingleTransformSignature(builder, ceilingRoot, false);
            for (var childIndex = 0; childIndex < ceilingRoot.childCount; childIndex++)
            {
                var child = ceilingRoot.GetChild(childIndex);
                AppendSingleTransformSignature(
                    builder,
                    child,
                    IsTriRoomTargetCeilingFollower(child.name));
            }

            return builder.ToString();
        }

        private static void AppendTranslationMutableRootSignature(
            StringBuilder builder,
            Transform target)
        {
            builder.Append(GetHierarchyPathForScene(target)).Append('|')
                .Append(target.gameObject.activeSelf).Append('|')
                .Append(Float(target.localPosition.y)).Append('|')
                .Append(QuaternionText(target.localRotation)).Append('|')
                .Append(Vector(target.localScale)).Append('|')
                .Append(BuildComponentSignature(target.gameObject)).AppendLine();
        }

        private static void AppendPlanarMutableRootSignature(
            StringBuilder builder,
            Transform target)
        {
            builder.Append(GetHierarchyPathForScene(target)).Append('|')
                .Append(target.gameObject.activeSelf).Append('|')
                .Append(Float(target.localPosition.y)).Append('|')
                .Append(Vector(target.localScale)).Append('|')
                .Append(BuildComponentSignature(target.gameObject)).AppendLine();
        }

        private static void AppendSingleTransformSignature(
            StringBuilder builder,
            Transform target,
            bool ignorePositionAndRotation)
        {
            builder.Append(GetHierarchyPathForScene(target)).Append('|')
                .Append(target.gameObject.activeSelf).Append('|');
            if (!ignorePositionAndRotation)
            {
                builder.Append(Vector(target.localPosition)).Append('|')
                    .Append(QuaternionText(target.localRotation)).Append('|');
            }

            builder.Append(Vector(target.localScale)).Append('|')
                .Append(BuildComponentSignature(target.gameObject)).AppendLine();
        }

        private static string GetHierarchyPathForScene(Transform target)
        {
            var names = new List<string>();
            var current = target;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private static bool IsTriRoomTargetCorridor(string corridorId)
        {
            return corridorId == "SC-H01" || corridorId == "SC-H02" || corridorId == "SC-H05";
        }

        private static bool IsTriRoomTargetCeilingFollower(string objectName)
        {
            return objectName.IndexOf("SC_H01", StringComparison.Ordinal) >= 0 ||
                objectName.IndexOf("SC_H02", StringComparison.Ordinal) >= 0 ||
                objectName.IndexOf("SC_H05", StringComparison.Ordinal) >= 0;
        }

        private static Dictionary<string, Transform> RequireCorridorModules(Transform corridorRoot)
        {
            var modules = new Dictionary<string, Transform>(StringComparer.Ordinal);
            for (var definitionIndex = 0;
                 definitionIndex < CorridorDefinitions.Length;
                 definitionIndex++)
            {
                var definition = CorridorDefinitions[definitionIndex];
                Transform match = null;
                for (var childIndex = 0; childIndex < corridorRoot.childCount; childIndex++)
                {
                    var child = corridorRoot.GetChild(childIndex);
                    if (!child.name.StartsWith(definition.ModuleId + " ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    match = child;
                    break;
                }

                if (match == null)
                {
                    throw new InvalidOperationException(
                        "Missing corridor module " + definition.ModuleId + " below " + corridorRoot.name);
                }

                modules.Add(definition.ModuleId, match);
            }

            if (modules.Count != 10)
            {
                throw new InvalidOperationException(
                    "Pegasus layout requires exactly 10 corridor modules. Count=" + modules.Count);
            }

            return modules;
        }

        private static void CloseLoadedTargetSceneIfNeeded()
        {
            var loaded = SceneManager.GetSceneByPath(TargetScenePath);
            if (!loaded.isLoaded)
            {
                return;
            }

            if (loaded.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus is already open with unsaved changes; refusing to overwrite them.");
            }

            if (!EditorSceneManager.CloseScene(loaded, true))
            {
                throw new InvalidOperationException("Failed to close the existing Pegasus scene.");
            }
        }

        private static void ResizeHorizontalCorridorModule(
            Transform module,
            Transform ceilingRoot,
            float targetLength)
        {
            var generatedRibs = new List<GameObject>();
            for (var childIndex = 0; childIndex < module.childCount; childIndex++)
            {
                var child = module.GetChild(childIndex);
                if (IsGeneratedHorizontalCorridorRib(child.name))
                {
                    generatedRibs.Add(child.gameObject);
                }
            }

            for (var generatedIndex = 0; generatedIndex < generatedRibs.Count; generatedIndex++)
            {
                UnityEngine.Object.DestroyImmediate(generatedRibs[generatedIndex]);
            }

            var currentBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var currentLength = currentBounds.size.x;
            if (currentLength <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    module.name + " has no usable longitudinal length.");
            }

            var scaleFactor = targetLength / currentLength;
            var halfExtension = (targetLength - currentLength) * 0.5f;
            Transform sourceRib = null;
            var baseRibs = new List<Transform>();
            Transform lowEndHousing = null;
            Transform highEndHousing = null;
            var stretchedSurfaceCount = 0;
            var movedEndCount = 0;
            for (var childIndex = 0; childIndex < module.childCount; childIndex++)
            {
                var child = module.GetChild(childIndex);
                if (IsHorizontalCorridorStretchSurface(child.name))
                {
                    var scale = child.localScale;
                    scale.x *= scaleFactor;
                    child.localScale = scale;
                    stretchedSurfaceCount++;
                }
                else if (child.name.Contains(" low end "))
                {
                    var position = child.localPosition;
                    position.x -= halfExtension;
                    child.localPosition = position;
                    movedEndCount++;
                    if (child.name.EndsWith(" overhead shutter housing", StringComparison.Ordinal))
                    {
                        lowEndHousing = child;
                    }
                }
                else if (child.name.Contains(" high end "))
                {
                    var position = child.localPosition;
                    position.x += halfExtension;
                    child.localPosition = position;
                    movedEndCount++;
                    if (child.name.EndsWith(" overhead shutter housing", StringComparison.Ordinal))
                    {
                        highEndHousing = child;
                    }
                }

                if (IsOriginalHorizontalCorridorRib(child.name))
                {
                    baseRibs.Add(child);
                    if (child.name.EndsWith(" cross deck rib 3", StringComparison.Ordinal))
                    {
                        sourceRib = child;
                    }
                }
            }

            if (stretchedSurfaceCount != 3 || movedEndCount != 8 ||
                lowEndHousing == null || highEndHousing == null ||
                sourceRib == null || baseRibs.Count != 5)
            {
                throw new InvalidOperationException(
                    module.name + " corridor structure differs from the inspected horizontal module. " +
                    "StretchSurfaces=" + stretchedSurfaceCount +
                    "; EndPieces=" + movedEndCount +
                    "; BaseRibs=" + baseRibs.Count);
            }

            var ceilingFollowers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
            var ceilingScale = ceilingFollowers[0].Transform.localScale;
            ceilingScale.x *= scaleFactor;
            ceilingFollowers[0].Transform.localScale = ceilingScale;

            baseRibs.Sort((left, right) => left.localPosition.x.CompareTo(right.localPosition.x));
            var ribStep = 0f;
            for (var ribIndex = 1; ribIndex < baseRibs.Count; ribIndex++)
            {
                ribStep += baseRibs[ribIndex].localPosition.x -
                    baseRibs[ribIndex - 1].localPosition.x;
            }

            ribStep /= baseRibs.Count - 1;
            if (ribStep <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    module.name + " original rib spacing is invalid.");
            }

            var lowHousingBounds = CalculateVisibleBoundsInLocalSpace(
                lowEndHousing.gameObject,
                module);
            var highHousingBounds = CalculateVisibleBoundsInLocalSpace(
                highEndHousing.gameObject,
                module);
            var lowLimit = lowHousingBounds.max.x;
            var highLimit = highHousingBounds.min.x;
            var lowExtensionIndex = 1;
            for (var position = baseRibs[0].localPosition.x - ribStep;
                 position > lowLimit + PositionTolerance;
                 position -= ribStep)
            {
                CloneHorizontalCorridorRib(
                    sourceRib,
                    module,
                    position,
                    "extension low cross deck rib " + lowExtensionIndex);
                lowExtensionIndex++;
            }

            var highExtensionIndex = 1;
            for (var position = baseRibs[baseRibs.Count - 1].localPosition.x + ribStep;
                 position < highLimit - PositionTolerance;
                 position += ribStep)
            {
                CloneHorizontalCorridorRib(
                    sourceRib,
                    module,
                    position,
                    "extension high cross deck rib " + highExtensionIndex);
                highExtensionIndex++;
            }

            var resizedBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            if (Mathf.Abs(resizedBounds.size.x - targetLength) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    module.name + " did not reach the required length. Actual=" +
                    Float(resizedBounds.size.x) + "; Target=" + Float(targetLength));
            }
        }

        private static void CloneHorizontalCorridorRib(
            Transform sourceRib,
            Transform module,
            float localX,
            string suffix)
        {
            var clone = UnityEngine.Object.Instantiate(sourceRib.gameObject, module);
            clone.name = module.name + " " + suffix;
            var position = sourceRib.localPosition;
            position.x = localX;
            clone.transform.localPosition = position;
            clone.transform.localRotation = sourceRib.localRotation;
            clone.transform.localScale = sourceRib.localScale;
        }

        private static bool IsHorizontalCorridorStretchSurface(string objectName)
        {
            return objectName.EndsWith(" straight floor slab", StringComparison.Ordinal) ||
                   objectName.EndsWith(" left armored wall", StringComparison.Ordinal) ||
                   objectName.EndsWith(" right armored wall", StringComparison.Ordinal);
        }

        private static bool IsOriginalHorizontalCorridorRib(string objectName)
        {
            return objectName.Contains(" horizontal corridor sample cross deck rib ") &&
                   !IsGeneratedHorizontalCorridorRib(objectName);
        }

        private static bool IsGeneratedHorizontalCorridorRib(string objectName)
        {
            return objectName.Contains(" extension low cross deck rib ") ||
                   objectName.Contains(" extension high cross deck rib ");
        }

        private static int CountGeneratedHorizontalCorridorRibs(Transform module)
        {
            var count = 0;
            for (var childIndex = 0; childIndex < module.childCount; childIndex++)
            {
                if (IsGeneratedHorizontalCorridorRib(module.GetChild(childIndex).name))
                {
                    count++;
                }
            }

            return count;
        }

        private static string BuildHorizontalCorridorProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot)
        {
            var builder = new StringBuilder(256 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                AppendHierarchySignature(builder, rooms[RoomDefinitions[roomIndex].Id].transform);
            }

            builder.AppendLine("[Corridors]");
            for (var corridorIndex = 0;
                 corridorIndex < CorridorDefinitions.Length;
                 corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                builder.Append(definition.ModuleId).Append("|RootPosition=")
                    .Append(Vector(module.position)).Append("|RootRotation=")
                    .Append(QuaternionText(module.rotation)).Append("|RootScale=")
                    .Append(Vector(module.lossyScale)).AppendLine();
                var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                if (definition.IsSloped)
                {
                    AppendHierarchySignature(builder, module);
                    AppendHierarchySignature(builder, followers[0].Transform);
                    continue;
                }

                var transforms = module.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    if (IsGeneratedHorizontalCorridorRib(target.name))
                    {
                        continue;
                    }

                    var mayMoveAlongLength =
                        target.name.Contains(" low end ") ||
                        target.name.Contains(" high end ");
                    var mayStretchAlongLength =
                        IsHorizontalCorridorStretchSurface(target.name);
                    builder.Append(GetHierarchyPath(module, target)).Append('|')
                        .Append(target.gameObject.activeSelf).Append('|');
                    if (!mayMoveAlongLength)
                    {
                        builder.Append("X=").Append(Float(target.localPosition.x)).Append('|');
                    }

                    builder.Append("YZ=")
                        .Append(Float(target.localPosition.y)).Append(',')
                        .Append(Float(target.localPosition.z)).Append('|')
                        .Append(QuaternionText(target.localRotation)).Append('|');
                    if (!mayStretchAlongLength)
                    {
                        builder.Append("ScaleX=").Append(Float(target.localScale.x)).Append('|');
                    }

                    builder.Append("ScaleYZ=")
                        .Append(Float(target.localScale.y)).Append(',')
                        .Append(Float(target.localScale.z)).Append('|')
                        .Append(BuildComponentSignature(target.gameObject)).AppendLine();
                }

                var ceiling = followers[0].Transform;
                builder.Append(ceiling.name).Append("|Position=")
                    .Append(Vector(ceiling.position)).Append("|Rotation=")
                    .Append(QuaternionText(ceiling.rotation)).Append("|ScaleYZ=")
                    .Append(Float(ceiling.localScale.y)).Append(',')
                    .Append(Float(ceiling.localScale.z)).Append('|')
                    .Append(BuildComponentSignature(ceiling.gameObject)).AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildEngineControlAlignmentProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot)
        {
            var builder = new StringBuilder(256 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                if (definition.Id == "ControlRoom")
                {
                    AppendTranslationMutableRootSignature(builder, room);
                    var descendants = room.GetComponentsInChildren<Transform>(true);
                    for (var descendantIndex = 1;
                         descendantIndex < descendants.Length;
                         descendantIndex++)
                    {
                        AppendSingleTransformSignature(builder, descendants[descendantIndex], false);
                    }
                }
                else
                {
                    AppendHierarchySignature(builder, room);
                }
            }

            builder.AppendLine("[CorridorRoot]");
            AppendSingleTransformSignature(builder, corridorRoot, false);
            builder.AppendLine("[Corridors]");
            for (var corridorIndex = 0;
                 corridorIndex < CorridorDefinitions.Length;
                 corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                if (definition.ModuleId != "SC-H05")
                {
                    AppendHierarchySignature(builder, module);
                    AppendHierarchySignature(builder, followers[0].Transform);
                    continue;
                }

                AppendSingleTransformSignature(builder, module, true);
                var transforms = module.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 1; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    if (IsGeneratedHorizontalCorridorRib(target.name))
                    {
                        continue;
                    }

                    var mayMoveAlongLength =
                        target.name.Contains(" low end ") ||
                        target.name.Contains(" high end ");
                    var mayStretchAlongLength =
                        IsHorizontalCorridorStretchSurface(target.name);
                    builder.Append(GetHierarchyPath(module, target)).Append('|')
                        .Append(target.gameObject.activeSelf).Append('|');
                    if (!mayMoveAlongLength)
                    {
                        builder.Append("X=").Append(Float(target.localPosition.x)).Append('|');
                    }

                    builder.Append("YZ=")
                        .Append(Float(target.localPosition.y)).Append(',')
                        .Append(Float(target.localPosition.z)).Append('|')
                        .Append(QuaternionText(target.localRotation)).Append('|');
                    if (!mayStretchAlongLength)
                    {
                        builder.Append("ScaleX=").Append(Float(target.localScale.x)).Append('|');
                    }

                    builder.Append("ScaleYZ=")
                        .Append(Float(target.localScale.y)).Append(',')
                        .Append(Float(target.localScale.z)).Append('|')
                        .Append(BuildComponentSignature(target.gameObject)).AppendLine();
                }

                var ceiling = followers[0].Transform;
                builder.Append(ceiling.name).Append('|')
                    .Append(ceiling.gameObject.activeSelf).Append('|')
                    .Append("ScaleYZ=")
                    .Append(Float(ceiling.localScale.y)).Append(',')
                    .Append(Float(ceiling.localScale.z)).Append('|')
                    .Append(BuildComponentSignature(ceiling.gameObject)).AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildCockpitMidpointAlignmentProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot,
            IReadOnlyDictionary<string, Transform> modules,
            Transform ceilingRoot)
        {
            var builder = new StringBuilder(256 * 1024);
            builder.AppendLine("[Rooms]");
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                if (definition.Id == "Cockpit")
                {
                    AppendTranslationMutableRootSignature(builder, room);
                    var descendants = room.GetComponentsInChildren<Transform>(true);
                    for (var descendantIndex = 1;
                         descendantIndex < descendants.Length;
                         descendantIndex++)
                    {
                        AppendSingleTransformSignature(
                            builder,
                            descendants[descendantIndex],
                            false);
                    }
                }
                else
                {
                    AppendHierarchySignature(builder, room);
                }
            }

            builder.AppendLine("[CorridorRoot]");
            AppendSingleTransformSignature(builder, corridorRoot, false);
            builder.AppendLine("[Corridors]");
            for (var corridorIndex = 0;
                 corridorIndex < CorridorDefinitions.Length;
                 corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                var targetCorridor = definition.ModuleId == "SC-H01" ||
                    definition.ModuleId == "SC-H02";
                if (!targetCorridor)
                {
                    AppendHierarchySignature(builder, module);
                    AppendHierarchySignature(builder, followers[0].Transform);
                    continue;
                }

                AppendSingleTransformSignature(builder, module, true);
                var descendants = module.GetComponentsInChildren<Transform>(true);
                for (var descendantIndex = 1;
                     descendantIndex < descendants.Length;
                     descendantIndex++)
                {
                    AppendSingleTransformSignature(
                        builder,
                        descendants[descendantIndex],
                        false);
                }

                AppendSingleTransformSignature(builder, followers[0].Transform, true);
            }

            return builder.ToString();
        }

        private static float CalculateLayoutScale(
            IReadOnlyDictionary<string, float> horizontalRadii,
            float corridorLength)
        {
            var scale = 24f;
            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var from = GetRoomDefinition(definition.FromRoomId);
                var to = GetRoomDefinition(definition.ToRoomId);
                var normalizedDistance = Vector2.Distance(from.MapPosition, to.MapPosition);
                var requiredDistance =
                    horizontalRadii[from.Id] +
                    horizontalRadii[to.Id] +
                    corridorLength +
                    (DetachedEndClearance * 2f);
                scale = Mathf.Max(scale, requiredDistance / normalizedDistance);
            }

            return Mathf.Ceil(scale / 5f) * 5f;
        }

        private static void PositionRooms(
            IReadOnlyDictionary<string, GameObject> rooms,
            float layoutScale)
        {
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id];
                var bounds = CalculateVisibleBounds(room);
                var targetCenter = definition.MapPosition * layoutScale;
                room.transform.position += new Vector3(
                    targetCenter.x - bounds.center.x,
                    0f,
                    targetCenter.y - bounds.center.z);

                var targetCeiling = CommonCeilingTopY -
                    (definition.IsCargo ? CargoCeilingDrop : 0f);
                room.transform.position += Vector3.up * (targetCeiling - GetMainCeilingTop(room));

                if (Mathf.Abs(definition.LayoutYawDegrees) > RotationTolerance)
                {
                    var rotationCenter = CalculateVisibleBounds(room).center;
                    room.transform.RotateAround(
                        rotationCenter,
                        Vector3.up,
                        definition.LayoutYawDegrees);
                }
            }
        }

        private static void PositionCorridors(
            IReadOnlyDictionary<string, GameObject> rooms,
            Transform corridorRoot,
            IReadOnlyDictionary<string, Transform> modules)
        {
            corridorRoot.position = Vector3.zero;
            corridorRoot.rotation = Quaternion.identity;
            var ceilingRoot = corridorRoot.Find(CorridorCeilingRootName) ??
                throw new InvalidOperationException(
                    "Pegasus corridor copy is missing " + CorridorCeilingRootName);

            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var module = modules[definition.ModuleId];
                var followers = CaptureCorridorCeilingFollowers(module, ceilingRoot);
                var fromRoom = rooms[definition.FromRoomId];
                var toRoom = rooms[definition.ToRoomId];
                var fromCenter = CalculateVisibleBounds(fromRoom).center;
                var toCenter = CalculateVisibleBounds(toRoom).center;
                var horizontalDirection = new Vector3(
                    toCenter.x - fromCenter.x,
                    0f,
                    toCenter.z - fromCenter.z).normalized;
                var fromRadius = GetHorizontalRadius(CalculateVisibleBounds(fromRoom));
                var toRadius = GetHorizontalRadius(CalculateVisibleBounds(toRoom));
                var centerDistance = HorizontalDistance(fromCenter, toCenter);
                var freeGap = centerDistance - fromRadius - toRadius;
                var freeGapCenter = fromCenter +
                    (horizontalDirection * (fromRadius + (freeGap * 0.5f)));

                module.rotation = Quaternion.FromToRotation(Vector3.right, horizontalDirection);
                module.position = new Vector3(
                    freeGapCenter.x,
                    module.position.y,
                    freeGapCenter.z);
                var fromFloor = GetMainFloorTop(fromRoom);
                var toFloor = GetMainFloorTop(toRoom);
                var targetFloor = (fromFloor + toFloor) * 0.5f;
                var moduleFloor = GetMainFloorTop(module.gameObject);
                module.position += Vector3.up * (targetFloor - moduleFloor);
                ApplyCorridorCeilingFollowers(module, followers);
            }
        }

        private static List<CeilingFollower> CaptureCorridorCeilingFollowers(
            Transform module,
            Transform ceilingRoot)
        {
            var followers = new List<CeilingFollower>();
            var prefix = "ShipSpaceCeiling_" + SanitizeName(module.name) + "_";
            for (var childIndex = 0; childIndex < ceilingRoot.childCount; childIndex++)
            {
                var child = ceilingRoot.GetChild(childIndex);
                if (!child.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                followers.Add(new CeilingFollower(
                    child,
                    module.InverseTransformPoint(child.position),
                    Quaternion.Inverse(module.rotation) * child.rotation));
            }

            if (followers.Count != 1)
            {
                throw new InvalidOperationException(
                    "Each corridor requires exactly one approved ceiling piece. Module=" +
                    module.name + "; Pieces=" + followers.Count);
            }

            return followers;
        }

        private static void ApplyCorridorCeilingFollowers(
            Transform module,
            IReadOnlyList<CeilingFollower> followers)
        {
            for (var followerIndex = 0; followerIndex < followers.Count; followerIndex++)
            {
                var follower = followers[followerIndex];
                follower.Transform.position = module.TransformPoint(follower.ModuleLocalPosition);
                follower.Transform.rotation = module.rotation * follower.ModuleLocalRotation;
            }
        }

        private static int RequireRoomCopyMatchesSource(
            Transform sourceRoot,
            Transform targetRoot,
            RoomDefinition definition)
        {
            var sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
            var targetTransforms = GetRoomShellTransforms(targetRoot, definition.Id);
            if (sourceTransforms.Length != targetTransforms.Length)
            {
                throw new InvalidOperationException(
                    definition.DisplayName + " copied hierarchy count differs from CargoRunMvp.");
            }

            var expectedRootRotation =
                Quaternion.AngleAxis(definition.LayoutYawDegrees, Vector3.up) *
                sourceRoot.rotation;
            if (!Approximately(expectedRootRotation, targetRoot.rotation))
            {
                throw new InvalidOperationException(
                    definition.DisplayName +
                    " root rotation does not match the approved Pegasus entrance direction. Expected=" +
                    QuaternionText(expectedRootRotation) + "; Actual=" +
                    QuaternionText(targetRoot.rotation));
            }

            for (var transformIndex = 0; transformIndex < sourceTransforms.Length; transformIndex++)
            {
                var source = sourceTransforms[transformIndex];
                var target = targetTransforms[transformIndex];
                var ignoreRootPosition = transformIndex == 0;
                var ignoreApprovedRootRotation =
                    transformIndex == 0 &&
                    Mathf.Abs(definition.LayoutYawDegrees) > RotationTolerance;
                RequireCopiedTransformMatches(
                    source,
                    target,
                    ignoreRootPosition,
                    ignoreApprovedRootRotation,
                    definition.DisplayName);
            }

            return targetTransforms.Length;
        }

        private static int RequireCorridorCopyMatchesSource(
            Transform sourceRoot,
            Transform targetRoot,
            IReadOnlyDictionary<string, Transform> sourceModules,
            IReadOnlyDictionary<string, Transform> targetModules)
        {
            var sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
            var targetTransforms = targetRoot.GetComponentsInChildren<Transform>(true);
            if (sourceTransforms.Length != targetTransforms.Length)
            {
                throw new InvalidOperationException(
                    "Pegasus corridor copied hierarchy count differs from CargoRunMvp.");
            }

            for (var transformIndex = 0; transformIndex < sourceTransforms.Length; transformIndex++)
            {
                var source = sourceTransforms[transformIndex];
                var target = targetTransforms[transformIndex];
                var ignorePosition = transformIndex == 0 ||
                    source.parent == sourceRoot ||
                    (source.parent != null && source.parent.name == CorridorCeilingRootName);
                var ignoreRotation = source.parent == sourceRoot ||
                    (source.parent != null && source.parent.name == CorridorCeilingRootName);
                RequireCopiedTransformMatches(
                    source,
                    target,
                    ignorePosition,
                    ignoreRotation,
                    "모든 복도");
            }

            for (var corridorIndex = 0; corridorIndex < CorridorDefinitions.Length; corridorIndex++)
            {
                var definition = CorridorDefinitions[corridorIndex];
                var sourceModule = sourceModules[definition.ModuleId];
                var targetModule = targetModules[definition.ModuleId];
                var sourceChildren = sourceModule.GetComponentsInChildren<Transform>(true);
                var targetChildren = targetModule.GetComponentsInChildren<Transform>(true);
                for (var childIndex = 1; childIndex < sourceChildren.Length; childIndex++)
                {
                    if (!Approximately(sourceChildren[childIndex].localPosition, targetChildren[childIndex].localPosition) ||
                        !Approximately(sourceChildren[childIndex].localRotation, targetChildren[childIndex].localRotation))
                    {
                        throw new InvalidOperationException(
                            definition.ModuleId +
                            " internal slope or geometry transform changed in Pegasus: " +
                            targetChildren[childIndex].name);
                    }
                }
            }

            return targetTransforms.Length;
        }

        private static void RequireCopiedTransformMatches(
            Transform source,
            Transform target,
            bool ignoreLocalPosition,
            bool ignoreLocalRotation,
            string displayName)
        {
            if (source.name != target.name ||
                source.gameObject.activeSelf != target.gameObject.activeSelf ||
                (!ignoreLocalPosition && !Approximately(source.localPosition, target.localPosition)) ||
                (!ignoreLocalRotation && !Approximately(source.localRotation, target.localRotation)) ||
                !Approximately(source.localScale, target.localScale) ||
                BuildComponentSignature(source.gameObject) != BuildComponentSignature(target.gameObject))
            {
                throw new InvalidOperationException(
                    displayName + " copied object differs from CargoRunMvp: " + target.name);
            }
        }

        private static string BuildSourceSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            GameObject corridorRoot)
        {
            var builder = new StringBuilder(128 * 1024);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                AppendHierarchySignature(builder, rooms[RoomDefinitions[roomIndex].Id].transform);
            }

            var sourceScene = corridorRoot.scene;
            var copiedDefinitions = GetInteriorCopyDefinitions();
            for (var interiorIndex = 0; interiorIndex < copiedDefinitions.Count; interiorIndex++)
            {
                AppendHierarchySignature(
                    builder,
                    RequireRootSceneObject(
                        sourceScene,
                        copiedDefinitions[interiorIndex].RootName).transform);
            }

            AppendHierarchySignature(builder, corridorRoot.transform);
            return builder.ToString();
        }

        private static int CloneDetachedInteriors(
            Scene sourceScene,
            Scene targetScene,
            IReadOnlyDictionary<string, GameObject> sourceRooms,
            IReadOnlyDictionary<string, GameObject> targetRooms,
            bool replaceExisting)
        {
            var copiedTransforms = 0;
            var copiedDefinitions = GetInteriorCopyDefinitions();
            var sourceInteriorRoots = new List<GameObject>(copiedDefinitions.Count);
            var targetInteriorRoots = new List<GameObject>(copiedDefinitions.Count);
            var objectMap = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            AddRoomShellObjectMappings(sourceRooms, targetRooms, objectMap);
            try
            {
                for (var interiorIndex = 0; interiorIndex < copiedDefinitions.Count; interiorIndex++)
                {
                    var definition = copiedDefinitions[interiorIndex];
                    var sourceRoot = RequireRootSceneObject(sourceScene, definition.RootName);
                    var targetRoom = targetRooms[definition.RoomId].transform;
                    var existing = FindDirectChild(targetRoom, definition.RootName);
                    if (existing != null)
                    {
                        if (!replaceExisting)
                        {
                            throw new InvalidOperationException(
                                "Pegasus already contains detached interior root " + definition.RootName);
                        }

                        UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    }

                    var clone = UnityEngine.Object.Instantiate(sourceRoot);
                    clone.name = sourceRoot.name;
                    SceneManager.MoveGameObjectToScene(clone, targetScene);
                    clone.transform.SetParent(targetRoom, false);
                    ApplyRoomRelativeTransform(
                        sourceRooms[definition.RoomId].transform,
                        sourceRoot.transform,
                        clone.transform);
                    AddHierarchyObjectMappings(sourceRoot.transform, clone.transform, objectMap);
                    sourceInteriorRoots.Add(sourceRoot);
                    targetInteriorRoots.Add(clone);
                    copiedTransforms += clone.GetComponentsInChildren<Transform>(true).Length;
                }

                for (var interiorIndex = 0; interiorIndex < sourceInteriorRoots.Count; interiorIndex++)
                {
                    RemapSceneObjectReferences(
                        sourceInteriorRoots[interiorIndex],
                        targetInteriorRoots[interiorIndex],
                        sourceScene,
                        objectMap);
                }
            }
            catch
            {
                for (var cloneIndex = 0; cloneIndex < targetInteriorRoots.Count; cloneIndex++)
                {
                    if (targetInteriorRoots[cloneIndex] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(targetInteriorRoots[cloneIndex]);
                    }
                }

                throw;
            }

            return copiedTransforms;
        }

        private static void AddRoomShellObjectMappings(
            IReadOnlyDictionary<string, GameObject> sourceRooms,
            IReadOnlyDictionary<string, GameObject> targetRooms,
            IDictionary<UnityEngine.Object, UnityEngine.Object> objectMap)
        {
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var sourceTransforms = sourceRooms[definition.Id]
                    .GetComponentsInChildren<Transform>(true);
                var targetTransforms = GetRoomShellTransforms(
                    targetRooms[definition.Id].transform,
                    definition.Id);
                if (sourceTransforms.Length != targetTransforms.Length)
                {
                    throw new InvalidOperationException(
                        definition.DisplayName + " shell hierarchy differs before interior restoration.");
                }

                for (var transformIndex = 0; transformIndex < sourceTransforms.Length; transformIndex++)
                {
                    AddGameObjectMappings(
                        sourceTransforms[transformIndex].gameObject,
                        targetTransforms[transformIndex].gameObject,
                        objectMap);
                }
            }
        }

        private static void AddHierarchyObjectMappings(
            Transform sourceRoot,
            Transform targetRoot,
            IDictionary<UnityEngine.Object, UnityEngine.Object> objectMap)
        {
            var sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
            var targetTransforms = targetRoot.GetComponentsInChildren<Transform>(true);
            if (sourceTransforms.Length != targetTransforms.Length)
            {
                throw new InvalidOperationException(
                    "Cannot map copied hierarchy with different transform counts: " + sourceRoot.name);
            }

            for (var transformIndex = 0; transformIndex < sourceTransforms.Length; transformIndex++)
            {
                AddGameObjectMappings(
                    sourceTransforms[transformIndex].gameObject,
                    targetTransforms[transformIndex].gameObject,
                    objectMap);
            }
        }

        private static void AddGameObjectMappings(
            GameObject source,
            GameObject target,
            IDictionary<UnityEngine.Object, UnityEngine.Object> objectMap)
        {
            objectMap[source] = target;
            var sourceComponents = source.GetComponents<Component>();
            var targetComponents = target.GetComponents<Component>();
            if (sourceComponents.Length != targetComponents.Length)
            {
                throw new InvalidOperationException(
                    "Cannot map copied components with different counts: " + source.name);
            }

            for (var componentIndex = 0; componentIndex < sourceComponents.Length; componentIndex++)
            {
                var sourceComponent = sourceComponents[componentIndex];
                var targetComponent = targetComponents[componentIndex];
                if (sourceComponent == null || targetComponent == null)
                {
                    if (sourceComponent != targetComponent)
                    {
                        throw new InvalidOperationException(
                            "Missing-script mapping differs on " + source.name);
                    }

                    continue;
                }

                if (sourceComponent.GetType() != targetComponent.GetType())
                {
                    throw new InvalidOperationException(
                        "Copied component order differs on " + source.name);
                }

                objectMap[sourceComponent] = targetComponent;
            }
        }

        private static void RemapSceneObjectReferences(
            GameObject sourceRoot,
            GameObject targetRoot,
            Scene sourceScene,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> objectMap)
        {
            var sourceComponents = sourceRoot.GetComponentsInChildren<Component>(true);
            var targetComponents = targetRoot.GetComponentsInChildren<Component>(true);
            if (sourceComponents.Length != targetComponents.Length)
            {
                throw new InvalidOperationException(
                    "Cannot remap references for hierarchy with different component counts: " +
                    sourceRoot.name);
            }

            for (var componentIndex = 0; componentIndex < sourceComponents.Length; componentIndex++)
            {
                var sourceComponent = sourceComponents[componentIndex];
                var targetComponent = targetComponents[componentIndex];
                if (sourceComponent == null || targetComponent == null)
                {
                    continue;
                }

                var sourceSerialized = new SerializedObject(sourceComponent);
                var targetSerialized = new SerializedObject(targetComponent);
                var sourceProperty = sourceSerialized.GetIterator();
                var enterChildren = true;
                var changed = false;
                while (sourceProperty.Next(enterChildren))
                {
                    enterChildren = false;
                    if (sourceProperty.propertyType != SerializedPropertyType.ObjectReference ||
                        sourceProperty.objectReferenceValue == null)
                    {
                        continue;
                    }

                    var referencedObject = sourceProperty.objectReferenceValue;
                    var referencedGameObject = referencedObject as GameObject;
                    if (referencedGameObject == null && referencedObject is Component referencedComponent)
                    {
                        referencedGameObject = referencedComponent.gameObject;
                    }

                    if (referencedGameObject == null || referencedGameObject.scene != sourceScene)
                    {
                        continue;
                    }

                    if (!objectMap.TryGetValue(referencedObject, out var mappedReference))
                    {
                        throw new InvalidOperationException(
                            "Detached interior reference cannot be mapped without copying an unrelated scene object. " +
                            "Root=" + sourceRoot.name + "; Component=" + sourceComponent.GetType().FullName +
                            "; Property=" + sourceProperty.propertyPath +
                            "; Reference=" + referencedObject.name);
                    }

                    var targetProperty = targetSerialized.FindProperty(sourceProperty.propertyPath) ??
                        throw new InvalidOperationException(
                            "Copied component is missing serialized property " +
                            sourceProperty.propertyPath + " on " + targetComponent.name);
                    targetProperty.objectReferenceValue = mappedReference;
                    changed = true;
                }

                if (changed)
                {
                    targetSerialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static int CountPresentDetachedInteriorRoots(Scene targetScene)
        {
            var rooms = RequireRooms(targetScene);
            var copiedDefinitions = GetInteriorCopyDefinitions();
            var count = 0;
            for (var interiorIndex = 0; interiorIndex < copiedDefinitions.Count; interiorIndex++)
            {
                var definition = copiedDefinitions[interiorIndex];
                if (FindDirectChild(rooms[definition.RoomId].transform, definition.RootName) != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int RequireInteriorCopiesMatchSource(
            Scene sourceScene,
            IReadOnlyDictionary<string, GameObject> sourceRooms,
            IReadOnlyDictionary<string, GameObject> targetRooms)
        {
            var copiedDefinitions = GetInteriorCopyDefinitions();
            var sourceRoots = new List<GameObject>(copiedDefinitions.Count);
            var targetRoots = new List<GameObject>(copiedDefinitions.Count);
            var objectMap = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            AddRoomShellObjectMappings(sourceRooms, targetRooms, objectMap);
            var copiedTransforms = 0;
            for (var interiorIndex = 0; interiorIndex < copiedDefinitions.Count; interiorIndex++)
            {
                var definition = copiedDefinitions[interiorIndex];
                var sourceRootObject = RequireRootSceneObject(sourceScene, definition.RootName);
                var sourceRoot = sourceRootObject.transform;
                var sourceRoom = sourceRooms[definition.RoomId].transform;
                var targetRoom = targetRooms[definition.RoomId].transform;
                var targetRoot = FindDirectChild(targetRoom, definition.RootName) ??
                    throw new InvalidOperationException(
                        "Pegasus is missing detached interior root " + definition.RootName);
                var sourceTransforms = sourceRoot.GetComponentsInChildren<Transform>(true);
                var targetTransforms = targetRoot.GetComponentsInChildren<Transform>(true);
                if (sourceTransforms.Length != targetTransforms.Length)
                {
                    throw new InvalidOperationException(
                        definition.RootName + " hierarchy count differs from CargoRunMvp.");
                }

                var relativeMatrix = sourceRoom.worldToLocalMatrix * sourceRoot.localToWorldMatrix;
                if (!Approximately((Vector3)relativeMatrix.GetColumn(3), targetRoot.localPosition) ||
                    !Approximately(relativeMatrix.rotation, targetRoot.localRotation) ||
                    !Approximately(relativeMatrix.lossyScale, targetRoot.localScale))
                {
                    throw new InvalidOperationException(
                        definition.RootName + " room-relative placement differs from CargoRunMvp.");
                }

                for (var transformIndex = 0; transformIndex < sourceTransforms.Length; transformIndex++)
                {
                    var source = sourceTransforms[transformIndex];
                    var target = targetTransforms[transformIndex];
                    RequireCopiedTransformMatches(
                        source,
                        target,
                        transformIndex == 0,
                        transformIndex == 0,
                        definition.RootName);
                }

                AddHierarchyObjectMappings(sourceRoot, targetRoot, objectMap);
                sourceRoots.Add(sourceRootObject);
                targetRoots.Add(targetRoot.gameObject);
                copiedTransforms += targetTransforms.Length;
            }

            for (var rootIndex = 0; rootIndex < sourceRoots.Count; rootIndex++)
            {
                RequireMappedSceneReferences(
                    sourceRoots[rootIndex],
                    targetRoots[rootIndex],
                    sourceScene,
                    objectMap);
            }

            return copiedTransforms;
        }

        private static void RequireMappedSceneReferences(
            GameObject sourceRoot,
            GameObject targetRoot,
            Scene sourceScene,
            IReadOnlyDictionary<UnityEngine.Object, UnityEngine.Object> objectMap)
        {
            var sourceComponents = sourceRoot.GetComponentsInChildren<Component>(true);
            var targetComponents = targetRoot.GetComponentsInChildren<Component>(true);
            if (sourceComponents.Length != targetComponents.Length)
            {
                throw new InvalidOperationException(
                    "Cannot verify scene references for hierarchy with different component counts: " +
                    sourceRoot.name);
            }

            for (var componentIndex = 0; componentIndex < sourceComponents.Length; componentIndex++)
            {
                var sourceComponent = sourceComponents[componentIndex];
                var targetComponent = targetComponents[componentIndex];
                if (sourceComponent == null || targetComponent == null)
                {
                    continue;
                }

                var sourceSerialized = new SerializedObject(sourceComponent);
                var targetSerialized = new SerializedObject(targetComponent);
                var sourceProperty = sourceSerialized.GetIterator();
                var enterChildren = true;
                while (sourceProperty.Next(enterChildren))
                {
                    enterChildren = false;
                    if (sourceProperty.propertyType != SerializedPropertyType.ObjectReference ||
                        sourceProperty.objectReferenceValue == null)
                    {
                        continue;
                    }

                    var sourceReference = sourceProperty.objectReferenceValue;
                    var referencedGameObject = sourceReference as GameObject;
                    if (referencedGameObject == null && sourceReference is Component referencedComponent)
                    {
                        referencedGameObject = referencedComponent.gameObject;
                    }

                    if (referencedGameObject == null || referencedGameObject.scene != sourceScene)
                    {
                        continue;
                    }

                    if (!objectMap.TryGetValue(sourceReference, out var expectedTargetReference))
                    {
                        throw new InvalidOperationException(
                            "Copied interior retains a dependency on an unrelated CargoRunMvp scene object. " +
                            "Root=" + sourceRoot.name + "; Component=" + sourceComponent.GetType().FullName +
                            "; Property=" + sourceProperty.propertyPath +
                            "; Reference=" + sourceReference.name);
                    }

                    var targetProperty = targetSerialized.FindProperty(sourceProperty.propertyPath) ??
                        throw new InvalidOperationException(
                            "Copied component is missing serialized property " +
                            sourceProperty.propertyPath + " on " + targetComponent.name);
                    if (targetProperty.objectReferenceValue != expectedTargetReference)
                    {
                        throw new InvalidOperationException(
                            "Copied interior scene reference differs from CargoRunMvp. " +
                            "Root=" + sourceRoot.name + "; Component=" + sourceComponent.GetType().FullName +
                            "; Property=" + sourceProperty.propertyPath);
                    }
                }
            }
        }

        private static Transform[] GetRoomShellTransforms(Transform roomRoot, string roomId)
        {
            var allTransforms = roomRoot.GetComponentsInChildren<Transform>(true);
            var shellTransforms = new List<Transform>(allTransforms.Length);
            for (var transformIndex = 0; transformIndex < allTransforms.Length; transformIndex++)
            {
                var target = allTransforms[transformIndex];
                if (!IsDetachedInteriorDescendant(target, roomRoot, roomId))
                {
                    shellTransforms.Add(target);
                }
            }

            return shellTransforms.ToArray();
        }

        private static bool IsDetachedInteriorDescendant(
            Transform target,
            Transform roomRoot,
            string roomId)
        {
            if (target == roomRoot)
            {
                return false;
            }

            var directChild = target;
            while (directChild.parent != null && directChild.parent != roomRoot)
            {
                directChild = directChild.parent;
            }

            if (directChild.parent != roomRoot)
            {
                return false;
            }

            var copiedDefinitions = GetInteriorCopyDefinitions();
            for (var interiorIndex = 0; interiorIndex < copiedDefinitions.Count; interiorIndex++)
            {
                var definition = copiedDefinitions[interiorIndex];
                if (definition.RoomId == roomId && definition.RootName == directChild.name)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<InteriorRootDefinition> GetInteriorCopyDefinitions()
        {
            var definitions = new List<InteriorRootDefinition>(
                InteriorDependencyRootDefinitions.Length + InteriorRootDefinitions.Length);
            for (var dependencyIndex = 0;
                 dependencyIndex < InteriorDependencyRootDefinitions.Length;
                 dependencyIndex++)
            {
                definitions.Add(InteriorDependencyRootDefinitions[dependencyIndex]);
            }

            for (var interiorIndex = 0;
                 interiorIndex < InteriorRootDefinitions.Length;
                 interiorIndex++)
            {
                definitions.Add(InteriorRootDefinitions[interiorIndex]);
            }

            return definitions;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            Transform match = null;
            for (var childIndex = 0; childIndex < parent.childCount; childIndex++)
            {
                var child = parent.GetChild(childIndex);
                if (child.name != childName)
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        "Duplicate direct child named " + childName + " below " + parent.name);
                }

                match = child;
            }

            return match;
        }

        private static void ApplyRoomRelativeTransform(
            Transform sourceRoom,
            Transform sourceInterior,
            Transform targetInterior)
        {
            var relativeMatrix = sourceRoom.worldToLocalMatrix * sourceInterior.localToWorldMatrix;
            targetInterior.localPosition = relativeMatrix.GetColumn(3);
            targetInterior.localRotation = relativeMatrix.rotation;
            targetInterior.localScale = relativeMatrix.lossyScale;
        }

        private static string BuildLayoutPlacementSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            GameObject corridorRoot)
        {
            var builder = new StringBuilder(32 * 1024);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var room = rooms[RoomDefinitions[roomIndex].Id].transform;
                builder.Append(room.name).Append('|')
                    .Append(Vector(room.position)).Append('|')
                    .Append(QuaternionText(room.rotation)).Append('|')
                    .Append(Vector(room.lossyScale)).AppendLine();
            }

            var corridorTransforms = corridorRoot.GetComponentsInChildren<Transform>(true);
            for (var transformIndex = 0; transformIndex < corridorTransforms.Length; transformIndex++)
            {
                var target = corridorTransforms[transformIndex];
                builder.Append(GetHierarchyPath(corridorRoot.transform, target)).Append('|')
                    .Append(Vector(target.localPosition)).Append('|')
                    .Append(QuaternionText(target.localRotation)).Append('|')
                    .Append(Vector(target.localScale)).AppendLine();
            }

            return builder.ToString();
        }

        private static void AppendHierarchySignature(StringBuilder builder, Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                var target = transforms[transformIndex];
                builder.Append(target.name).Append('|')
                    .Append(target.gameObject.activeSelf).Append('|')
                    .Append(Vector(target.localPosition)).Append('|')
                    .Append(QuaternionText(target.localRotation)).Append('|')
                    .Append(Vector(target.localScale)).Append('|')
                    .Append(BuildComponentSignature(target.gameObject)).AppendLine();
            }
        }

        private static string BuildComponentSignature(GameObject target)
        {
            var builder = new StringBuilder();
            var components = target.GetComponents<Component>();
            builder.Append("Components=").Append(components.Length);
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var component = components[componentIndex];
                if (component == null)
                {
                    builder.Append("|MissingScript");
                    continue;
                }

                builder.Append('|').Append(component.GetType().FullName);
                if (component is MeshFilter meshFilter)
                {
                    builder.Append(':').Append(AssetDatabase.GetAssetPath(meshFilter.sharedMesh));
                }
                else if (component is Renderer renderer)
                {
                    builder.Append(':').Append(renderer.enabled);
                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(materials[materialIndex]));
                    }
                }
                else if (component is Collider collider)
                {
                    builder.Append(':').Append(collider.enabled).Append(':').Append(collider.isTrigger);
                }
            }

            return builder.ToString();
        }

        private static void RequireOnlyApprovedTargetRoots(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            if (roots.Length != 7)
            {
                throw new InvalidOperationException(
                    "Pegasus must contain exactly six room roots and one corridor root. Roots=" +
                    roots.Length);
            }

            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var approved = roots[rootIndex].name == CorridorRootName;
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length && !approved; roomIndex++)
                {
                    approved = roots[rootIndex].name == RoomDefinitions[roomIndex].RootName;
                }

                if (!approved)
                {
                    throw new InvalidOperationException(
                        "Pegasus contains an unapproved persistent root: " + roots[rootIndex].name);
                }
            }
        }

        private static float GetMainCeilingTop(GameObject root)
        {
            Transform ceilingRoot = null;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                if (!transforms[transformIndex].name.StartsWith(
                        "ShipSpaceCeiling_",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                ceilingRoot = transforms[transformIndex];
                break;
            }

            if (ceilingRoot == null)
            {
                throw new InvalidOperationException(
                    "Missing approved ceiling below " + root.name);
            }

            var renderers = ceilingRoot.GetComponentsInChildren<Renderer>(true);
            var top = float.NegativeInfinity;
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                top = Mathf.Max(top, renderer.bounds.max.y);
            }

            if (float.IsNegativeInfinity(top))
            {
                throw new InvalidOperationException(
                    "Approved ceiling has no visible renderer below " + root.name);
            }

            return top;
        }

        private static float GetMainFloorTop(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Renderer largestFloor = null;
            var largestArea = float.NegativeInfinity;
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var lowerName = renderer.name.ToLowerInvariant();
                if (lowerName.IndexOf("floor", StringComparison.Ordinal) < 0 &&
                    lowerName.IndexOf("deck", StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var area = renderer.bounds.size.x * renderer.bounds.size.z;
                if (area <= largestArea)
                {
                    continue;
                }

                largestArea = area;
                largestFloor = renderer;
            }

            if (largestFloor == null)
            {
                return CalculateVisibleBounds(root).min.y;
            }

            return largestFloor.bounds.max.y;
        }

        private static Bounds CalculateVisibleBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var found = false;
            var bounds = new Bounds();
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    "No visible renderer bounds found below " + root.name);
            }

            return bounds;
        }

        private static bool TryCalculateVisibleBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var found = false;
            bounds = new Bounds();
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private static float GetCommonCorridorOutsideWidth(
            IReadOnlyDictionary<string, Transform> modules)
        {
            var total = 0f;
            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            for (var corridorIndex = 0;
                 corridorIndex < CorridorDefinitions.Length;
                 corridorIndex++)
            {
                var module = modules[CorridorDefinitions[corridorIndex].ModuleId];
                var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
                var width = module.TransformVector(Vector3.forward * localBounds.size.z).magnitude;
                total += width;
                minimum = Mathf.Min(minimum, width);
                maximum = Mathf.Max(maximum, width);
            }

            if (maximum - minimum > EntranceWidthTolerance)
            {
                throw new InvalidOperationException(
                    "Pegasus corridor outside widths are not consistent. " +
                    "Minimum=" + Float(minimum) + "; Maximum=" + Float(maximum));
            }

            return total / CorridorDefinitions.Length;
        }

        private static float GetRoomExpansionReferenceWidth(Scene scene, string roomId)
        {
            if (roomId == "Cockpit")
            {
                var renderer = RequireRenderer(scene, "left future opening floor edge only");
                var room = RequireSceneObject(scene, "Approved Cockpit 01 Structure");
                var bounds = CalculateRendererBoundsInLocalSpace(renderer, room.transform);
                var localWidth = Mathf.Max(bounds.size.x, bounds.size.z);
                var worldX = room.transform.TransformVector(Vector3.right * localWidth).magnitude;
                var worldZ = room.transform.TransformVector(Vector3.forward * localWidth).magnitude;
                return Mathf.Max(worldX, worldZ);
            }

            for (var pairIndex = 0;
                 pairIndex < EntranceWidthPairDefinitions.Length;
                 pairIndex++)
            {
                var pair = EntranceWidthPairDefinitions[pairIndex];
                if (pair.RoomId != roomId || !pair.IsExpansionReference)
                {
                    continue;
                }

                return GetEntranceOutsideSpan(
                    RequireRenderer(scene, pair.FirstWallName),
                    RequireRenderer(scene, pair.SecondWallName));
            }

            throw new InvalidOperationException(
                "Missing room proportion expansion reference entrance for " + roomId);
        }

        private static EntranceConnectionAnchor GetRoomExpansionReviewAnchor(
            Scene scene,
            IReadOnlyDictionary<string, GameObject> rooms,
            string roomId)
        {
            if (roomId == "Cockpit")
            {
                return BuildSingleRendererEntranceAnchor(
                    "CockpitReview",
                    rooms[roomId],
                    RequireRenderer(scene, "left future opening floor edge only"),
                    true);
            }

            for (var pairIndex = 0;
                 pairIndex < EntranceWidthPairDefinitions.Length;
                 pairIndex++)
            {
                var pair = EntranceWidthPairDefinitions[pairIndex];
                if (pair.RoomId != roomId || !pair.IsExpansionReference)
                {
                    continue;
                }

                return BuildWallPairEntranceAnchor(
                    pair.Label + "Review",
                    rooms[roomId],
                    RequireRenderer(scene, pair.FirstWallName),
                    RequireRenderer(scene, pair.SecondWallName));
            }

            throw new InvalidOperationException(
                "Missing room proportion review anchor for " + roomId);
        }

        private static Bounds RequireRoomStructuralBounds(Transform room, string roomId)
        {
            var found = false;
            var combined = new Bounds();
            for (var childIndex = 0; childIndex < room.childCount; childIndex++)
            {
                var child = room.GetChild(childIndex);
                if (!IsRoomExpansionStructuralTarget(roomId, child.name) ||
                    !TryCalculateVisibleBoundsInLocalSpace(child.gameObject, room, out var childBounds))
                {
                    continue;
                }

                if (!found)
                {
                    combined = childBounds;
                    found = true;
                }
                else
                {
                    combined.Encapsulate(childBounds.min);
                    combined.Encapsulate(childBounds.max);
                }
            }

            if (!found || combined.size.x <= PositionTolerance || combined.size.z <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Unable to calculate horizontal structural bounds for " + roomId);
            }

            return combined;
        }

        private static List<Transform> CollectRoomInteriorDirectChildren(
            Transform room,
            string roomId)
        {
            var interiors = new List<Transform>();
            for (var childIndex = 0; childIndex < room.childCount; childIndex++)
            {
                var child = room.GetChild(childIndex);
                if (!IsRoomExpansionStructuralTarget(roomId, child.name))
                {
                    interiors.Add(child);
                }
            }

            interiors.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return interiors;
        }

        private static Transform ResolveSourceInteriorTransform(
            Scene sourceScene,
            Transform sourceRoom,
            string roomId,
            string interiorName)
        {
            var copiedDefinitions = GetInteriorCopyDefinitions();
            for (var definitionIndex = 0;
                 definitionIndex < copiedDefinitions.Count;
                 definitionIndex++)
            {
                var definition = copiedDefinitions[definitionIndex];
                if (definition.RoomId == roomId && definition.RootName == interiorName)
                {
                    return RequireRootSceneObject(sourceScene, interiorName).transform;
                }
            }

            return FindDirectChild(sourceRoom, interiorName) ??
                throw new InvalidOperationException(
                    "CargoRunMvp is missing the interior reference " + roomId + "/" + interiorName);
        }

        private static Vector3 GetRoomLocalVisibleCenterOrPosition(
            Transform target,
            Transform room,
            Vector3 fallback)
        {
            return TryCalculateVisibleBoundsInLocalSpace(target.gameObject, room, out var bounds)
                ? bounds.center
                : fallback;
        }

        private static bool HasActiveVisibleRenderer(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                if (renderers[rendererIndex].enabled && renderers[rendererIndex].gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private static float RequirePositiveRatio(float target, float source, string label)
        {
            if (source <= PositionTolerance || target <= PositionTolerance)
            {
                throw new InvalidOperationException(
                    label + " requires positive source and target dimensions.");
            }

            return target / source;
        }

        private static Vector3 NormalizeHorizontalPoint(Vector3 point, Bounds bounds)
        {
            return new Vector3(
                (point.x - bounds.center.x) / bounds.size.x,
                point.y,
                (point.z - bounds.center.z) / bounds.size.z);
        }

        private static Vector3 DenormalizeHorizontalPoint(
            Vector3 normalized,
            Bounds bounds,
            float preservedY)
        {
            return new Vector3(
                bounds.center.x + (normalized.x * bounds.size.x),
                preservedY,
                bounds.center.z + (normalized.z * bounds.size.z));
        }

        private static Vector3 CalculateProportionalInteriorScale(
            Vector3 sourceRelativeScale,
            Quaternion targetLocalRotation,
            float ratioX,
            float ratioZ)
        {
            return new Vector3(
                sourceRelativeScale.x * CalculateMappedAxisScale(
                    targetLocalRotation * Vector3.right,
                    ratioX,
                    ratioZ),
                sourceRelativeScale.y * CalculateMappedAxisScale(
                    targetLocalRotation * Vector3.up,
                    ratioX,
                    ratioZ),
                sourceRelativeScale.z * CalculateMappedAxisScale(
                    targetLocalRotation * Vector3.forward,
                    ratioX,
                    ratioZ));
        }

        private static float CalculateMappedAxisScale(
            Vector3 roomLocalAxis,
            float ratioX,
            float ratioZ)
        {
            var direction = roomLocalAxis.normalized;
            return new Vector3(
                direction.x * ratioX,
                direction.y,
                direction.z * ratioZ).magnitude;
        }

        private static string BuildRoomInteriorProportionProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            var builder = new StringBuilder(512 * 1024);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                var mutableRoots = new HashSet<Transform>(
                    CollectRoomInteriorDirectChildren(room, definition.Id));
                var transforms = room.GetComponentsInChildren<Transform>(true);
                Array.Sort(
                    transforms,
                    (left, right) => string.CompareOrdinal(
                        GetHierarchyPath(room, left),
                        GetHierarchyPath(room, right)));
                builder.AppendLine("[" + definition.Id + "]");
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    builder.Append(GetHierarchyPath(room, target)).Append('|')
                        .Append(target.gameObject.activeSelf).Append('|');
                    if (mutableRoots.Contains(target))
                    {
                        var roomLocalPosition = room.InverseTransformPoint(target.position);
                        builder.Append("MutableXZ=True|RoomY=")
                            .Append(Float(Mathf.Round(roomLocalPosition.y * 10000f) / 10000f))
                            .Append("|Rotation=").Append(QuaternionText(target.localRotation)).Append('|');
                    }
                    else
                    {
                        builder.Append("MutableXZ=False|LocalPosition=")
                            .Append(Vector(target.localPosition))
                            .Append("|Rotation=").Append(QuaternionText(target.localRotation))
                            .Append("|Scale=").Append(Vector(target.localScale)).Append('|');
                    }

                    builder.Append(BuildComponentSignature(target.gameObject)).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static Dictionary<string, List<Transform>> CollectRoomExpansionStructuralTargets(
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            var result = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                var targets = new List<Transform>();
                for (var childIndex = 0; childIndex < room.childCount; childIndex++)
                {
                    var child = room.GetChild(childIndex);
                    if (IsRoomExpansionStructuralTarget(definition.Id, child.name))
                    {
                        targets.Add(child);
                    }
                }

                if (targets.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No horizontal structure targets found for " + definition.Id);
                }

                result.Add(definition.Id, targets);
            }

            return result;
        }

        private static bool IsRoomExpansionStructuralTarget(string roomId, string objectName)
        {
            if (roomId == "EngineRoom")
            {
                return objectName == "Floor - individually editable" ||
                    objectName == "Walls - individually editable" ||
                    objectName == "Entrances - individually editable" ||
                    objectName == "ShipSpaceCeiling_EngineRoom";
            }

            if (roomId == "Cockpit")
            {
                return objectName == "Cockpit 01 - structure only" ||
                    objectName == "Approved Cockpit 01 Window" ||
                    objectName == "ShipSpaceCeiling_Cockpit";
            }

            if (roomId == "ControlRoom")
            {
                return objectName == "Floor - individually editable" ||
                    objectName == "Walls - individually editable" ||
                    objectName == "Corridors - individually editable" ||
                    objectName == "ShipSpaceCeiling_ControlRoom";
            }

            if (roomId == "Armory")
            {
                return objectName == "AR-01 approved armory shell sample model" ||
                    objectName == "ShipSpaceCeiling_Armory";
            }

            var lowerName = objectName.ToLowerInvariant();
            return lowerName.Contains("floor") ||
                lowerName.Contains("deck") ||
                lowerName.Contains("wall") ||
                lowerName.Contains("doorway") ||
                lowerName.Contains("corridor") ||
                lowerName.Contains("ceiling") ||
                lowerName.Contains("threshold");
        }

        private static void CaptureRoomInteriorPlacements(
            Transform room,
            IReadOnlyDictionary<string, List<Transform>> structuralTargets,
            List<RoomInteriorPlacement> placements)
        {
            var roomId = string.Empty;
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                if (RoomDefinitions[roomIndex].RootName == room.name)
                {
                    roomId = RoomDefinitions[roomIndex].Id;
                    break;
                }
            }

            if (string.IsNullOrEmpty(roomId))
            {
                throw new InvalidOperationException(
                    "Unable to resolve room id for interior placement: " + room.name);
            }

            var excluded = new HashSet<Transform>(structuralTargets[roomId]);
            for (var childIndex = 0; childIndex < room.childCount; childIndex++)
            {
                var child = room.GetChild(childIndex);
                if (excluded.Contains(child))
                {
                    continue;
                }

                var center = child.localPosition;
                if (TryCalculateVisibleBoundsInLocalSpace(child.gameObject, room, out var bounds))
                {
                    center = bounds.center;
                }

                placements.Add(new RoomInteriorPlacement(roomId, room, child, center));
            }
        }

        private static void ExpandStructuralTransformHorizontally(
            Transform room,
            Transform target,
            float factor)
        {
            if (target.parent != room)
            {
                throw new InvalidOperationException(
                    "Room expansion structure target is not a direct room child: " + target.name);
            }

            var localPosition = target.localPosition;
            localPosition.x *= factor;
            localPosition.z *= factor;
            target.localPosition = localPosition;

            var scale = target.localScale;
            if (IsTransformAxisHorizontalInRoom(room, target, Vector3.right))
            {
                scale.x *= factor;
            }

            if (IsTransformAxisHorizontalInRoom(room, target, Vector3.up))
            {
                scale.y *= factor;
            }

            if (IsTransformAxisHorizontalInRoom(room, target, Vector3.forward))
            {
                scale.z *= factor;
            }

            target.localScale = scale;
        }

        private static bool IsTransformAxisHorizontalInRoom(
            Transform room,
            Transform target,
            Vector3 localAxis)
        {
            var worldDirection = target.TransformDirection(localAxis).normalized;
            var roomDirection = room.InverseTransformDirection(worldDirection).normalized;
            return Mathf.Abs(roomDirection.y) < 0.5f;
        }

        private static void RepositionRoomInterior(RoomInteriorPlacement placement, float factor)
        {
            if (placement.Target.parent != placement.Room)
            {
                throw new InvalidOperationException(
                    "Room interior placement target is not a direct room child: " +
                    placement.Target.name);
            }

            var desiredCenter = placement.RoomLocalVisibleCenter;
            desiredCenter.x *= factor;
            desiredCenter.z *= factor;
            var roomDelta = desiredCenter - placement.RoomLocalVisibleCenter;
            var localPosition = placement.Target.localPosition;
            localPosition.x += roomDelta.x;
            localPosition.z += roomDelta.z;
            placement.Target.localPosition = localPosition;
        }

        private static void NormalizeEntranceOutsideWidth(
            Scene scene,
            EntranceWidthPairDefinition definition,
            float targetWidth)
        {
            var first = RequireRenderer(scene, definition.FirstWallName);
            var second = RequireRenderer(scene, definition.SecondWallName);
            var lateral = HorizontalDirection(second.bounds.center - first.bounds.center);
            var firstThickness = GetRendererProjectionMaximum(first, lateral) -
                GetRendererProjectionMinimum(first, lateral);
            var secondThickness = GetRendererProjectionMaximum(second, lateral) -
                GetRendererProjectionMinimum(second, lateral);
            var desiredCenterDistance = targetWidth - ((firstThickness + secondThickness) * 0.5f);
            if (desiredCenterDistance <= 0f)
            {
                throw new InvalidOperationException(
                    definition.Label + " entrance wall thickness exceeds target corridor width.");
            }

            var midpoint = (first.bounds.center + second.bounds.center) * 0.5f;
            var desiredFirstCenter = midpoint - (lateral * desiredCenterDistance * 0.5f);
            var desiredSecondCenter = midpoint + (lateral * desiredCenterDistance * 0.5f);
            desiredFirstCenter.y = first.bounds.center.y;
            desiredSecondCenter.y = second.bounds.center.y;
            first.transform.position += desiredFirstCenter - first.bounds.center;
            second.transform.position += desiredSecondCenter - second.bounds.center;
        }

        private static float GetEntranceOutsideSpan(Renderer first, Renderer second)
        {
            var lateral = HorizontalDirection(second.bounds.center - first.bounds.center);
            var minimum = Mathf.Min(
                GetRendererProjectionMinimum(first, lateral),
                GetRendererProjectionMinimum(second, lateral));
            var maximum = Mathf.Max(
                GetRendererProjectionMaximum(first, lateral),
                GetRendererProjectionMaximum(second, lateral));
            return maximum - minimum;
        }

        private static float GetRendererProjectionMinimum(Renderer renderer, Vector3 direction)
        {
            return -GetRendererProjectionMaximum(renderer, -direction);
        }

        private static string BuildRoomProportionProtectedSignature(
            IReadOnlyDictionary<string, GameObject> rooms,
            IReadOnlyDictionary<string, List<Transform>> structuralTargets)
        {
            var builder = new StringBuilder(512 * 1024);
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var room = rooms[definition.Id].transform;
                var scaleAllowed = new HashSet<Transform>(structuralTargets[definition.Id]);
                var transforms = room.GetComponentsInChildren<Transform>(true);
                Array.Sort(
                    transforms,
                    (left, right) => string.CompareOrdinal(
                        GetHierarchyPath(room, left),
                        GetHierarchyPath(room, right)));
                builder.AppendLine("[" + definition.Id + "]");
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    var roomLocalPosition = room.InverseTransformPoint(target.position);
                    builder.Append(GetHierarchyPath(room, target)).Append('|')
                        .Append(target.gameObject.activeSelf).Append('|')
                        .Append("RoomY=").Append(Float(
                            Mathf.Round(roomLocalPosition.y * 10000f) / 10000f)).Append('|')
                        .Append("Rotation=").Append(QuaternionText(target.localRotation)).Append('|');
                    if (!scaleAllowed.Contains(target))
                    {
                        builder.Append("Scale=").Append(Vector(target.localScale)).Append('|');
                    }

                    builder.Append(BuildComponentSignature(target.gameObject)).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string GetFirstSignatureDifference(string before, string after)
        {
            var beforeLines = before.Replace("\r\n", "\n").Split('\n');
            var afterLines = after.Replace("\r\n", "\n").Split('\n');
            var lineCount = Mathf.Min(beforeLines.Length, afterLines.Length);
            for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
                if (beforeLines[lineIndex] == afterLines[lineIndex])
                {
                    continue;
                }

                return "FirstDifferenceLine=" + (lineIndex + 1) +
                    "; Before=" + beforeLines[lineIndex] +
                    "; After=" + afterLines[lineIndex];
            }

            return "SignatureLineCountBefore=" + beforeLines.Length +
                "; SignatureLineCountAfter=" + afterLines.Length;
        }

        private static Bounds CalculateVisibleBoundsInLocalSpace(
            GameObject root,
            Transform reference)
        {
            if (!TryCalculateVisibleBoundsInLocalSpace(root, reference, out var bounds))
            {
                throw new InvalidOperationException(
                    "No visible renderer bounds found below " + root.name);
            }

            return bounds;
        }

        private static bool TryCalculateVisibleBoundsInLocalSpace(
            GameObject root,
            Transform reference,
            out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var found = false;
            bounds = new Bounds();
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var rendererBounds = renderer.localBounds;
                var toReference = reference.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (var xIndex = 0; xIndex < 2; xIndex++)
                {
                    for (var yIndex = 0; yIndex < 2; yIndex++)
                    {
                        for (var zIndex = 0; zIndex < 2; zIndex++)
                        {
                            var localCorner = new Vector3(
                                xIndex == 0 ? rendererBounds.min.x : rendererBounds.max.x,
                                yIndex == 0 ? rendererBounds.min.y : rendererBounds.max.y,
                                zIndex == 0 ? rendererBounds.min.z : rendererBounds.max.z);
                            var point = toReference.MultiplyPoint3x4(localCorner);
                            if (!found)
                            {
                                bounds = new Bounds(point, Vector3.zero);
                                found = true;
                            }
                            else
                            {
                                bounds.Encapsulate(point);
                            }
                        }
                    }
                }
            }

            return found;
        }

        private static Bounds CalculateRendererBoundsInLocalSpace(
            Renderer renderer,
            Transform reference)
        {
            var rendererBounds = renderer.localBounds;
            var toReference = reference.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            var found = false;
            var bounds = new Bounds();
            for (var xIndex = 0; xIndex < 2; xIndex++)
            {
                for (var yIndex = 0; yIndex < 2; yIndex++)
                {
                    for (var zIndex = 0; zIndex < 2; zIndex++)
                    {
                        var localCorner = new Vector3(
                            xIndex == 0 ? rendererBounds.min.x : rendererBounds.max.x,
                            yIndex == 0 ? rendererBounds.min.y : rendererBounds.max.y,
                            zIndex == 0 ? rendererBounds.min.z : rendererBounds.max.z);
                        var point = toReference.MultiplyPoint3x4(localCorner);
                        if (!found)
                        {
                            bounds = new Bounds(point, Vector3.zero);
                            found = true;
                        }
                        else
                        {
                            bounds.Encapsulate(point);
                        }
                    }
                }
            }

            return bounds;
        }

        private static bool IsSelectedSpaceRoot(string rootName)
        {
            if (rootName == CorridorRootName)
            {
                return true;
            }

            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                if (rootName == RoomDefinitions[roomIndex].RootName)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FindOverlappingRooms(
            Bounds candidateBounds,
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            var matches = new List<string>();
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                var roomBounds = CalculateVisibleBounds(rooms[definition.Id]);
                if (candidateBounds.Intersects(roomBounds))
                {
                    matches.Add(definition.Id);
                }
            }

            return matches.Count == 0 ? "None" : string.Join(",", matches);
        }

        private static string FindContainingRooms(
            Vector3 point,
            IReadOnlyDictionary<string, GameObject> rooms)
        {
            var matches = new List<string>();
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                var definition = RoomDefinitions[roomIndex];
                if (CalculateVisibleBounds(rooms[definition.Id]).Contains(point))
                {
                    matches.Add(definition.Id);
                }
            }

            return matches.Count == 0 ? "None" : string.Join(",", matches);
        }

        private static float GetCorridorLength(GameObject module)
        {
            var bounds = CalculateVisibleBounds(module);
            return Mathf.Max(bounds.size.x, bounds.size.z);
        }

        private static float GetHorizontalRadius(Bounds bounds)
        {
            return Mathf.Sqrt(
                (bounds.extents.x * bounds.extents.x) +
                (bounds.extents.z * bounds.extents.z));
        }

        private static float HorizontalDistance(Vector3 left, Vector3 right)
        {
            return Vector2.Distance(
                new Vector2(left.x, left.z),
                new Vector2(right.x, right.z));
        }

        private static float HorizontalBoundsDistance(Bounds left, Bounds right)
        {
            var deltaX = Mathf.Max(
                0f,
                Mathf.Max(left.min.x - right.max.x, right.min.x - left.max.x));
            var deltaZ = Mathf.Max(
                0f,
                Mathf.Max(left.min.z - right.max.z, right.min.z - left.max.z));
            return Mathf.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
        }

        private static RoomDefinition GetRoomDefinition(string roomId)
        {
            for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
            {
                if (RoomDefinitions[roomIndex].Id == roomId)
                {
                    return RoomDefinitions[roomIndex];
                }
            }

            throw new InvalidOperationException("Unknown Pegasus room id: " + roomId);
        }

        private static void CaptureCockpitOrientationReviewSheet(string outputPath)
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Cockpit orientation capture stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            RequireTriRoomRoomRotations(rooms);
            var cockpitBounds = CalculateVisibleBounds(rooms["Cockpit"]);
            const int panelWidth = 1100;
            const int panelHeight = 800;
            const int gutter = 12;
            var sheetWidth = (panelWidth * 2) + (gutter * 3);
            var sheetHeight = panelHeight + (gutter * 2);
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = new Color(0.018f, 0.022f, 0.028f, 1f);
            }

            sheet.SetPixels(backgroundPixels);
            var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            temporaryScene.name = "__PegasusCockpitOrientationCapture";
            var cameraObject = new GameObject("__PegasusCockpitOrientationCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            var lightObject = new GameObject("__PegasusCockpitOrientationLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, temporaryScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.9f, 0.95f, 1f, 1f);
            light.intensity = 2.25f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            var panels = new List<Texture2D>(2);
            try
            {
                panels.Add(RenderTopDownBounds(
                    camera,
                    cockpitBounds,
                    Quaternion.Euler(90f, 0f, 0f),
                    panelWidth,
                    panelHeight));
                panels.Add(RenderBoundsOblique(
                    camera,
                    cockpitBounds,
                    panelWidth,
                    panelHeight));
                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    sheet.SetPixels(
                        gutter + (panelIndex * (panelWidth + gutter)),
                        gutter,
                        panelWidth,
                        panelHeight,
                        panels[panelIndex].GetPixels());
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    UnityEngine.Object.DestroyImmediate(panels[panelIndex]);
                }

                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.CloseScene(temporaryScene, true);
            }
        }

        private static void CaptureTriRoomFlushConnectionReview(string outputPath)
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Flush connection capture stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            RequireTriRoomRoomRotations(rooms);
            RequireNoSelectedTriRoomOverlap(rooms);
            RequireNoTriRoomTargetCorridorCrossing(targetScene, rooms, modules);
            RequireAllTriRoomCorridorEntranceClearance(targetScene, rooms, modules);
            const int panelWidth = 1100;
            const int panelHeight = 700;
            const int columns = 2;
            const int rows = 4;
            const int gutter = 12;
            var sheetWidth = (panelWidth * columns) + (gutter * (columns + 1));
            var sheetHeight = (panelHeight * rows) + (gutter * (rows + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = new Color(0.018f, 0.022f, 0.028f, 1f);
            }

            sheet.SetPixels(backgroundPixels);
            var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            temporaryScene.name = "__PegasusTriRoomFlushConnectionCapture";
            var cameraObject = new GameObject("__PegasusTriRoomFlushConnectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            var lightObject = new GameObject("__PegasusTriRoomFlushConnectionLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, temporaryScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.9f, 0.95f, 1f, 1f);
            light.intensity = 2.25f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            var corridorIds = new[] { "SC-H01", "SC-H02", "SC-H05" };
            var fromRoomIds = new[] { "EngineRoom", "Cockpit", "EngineRoom" };
            var toRoomIds = new[] { "Cockpit", "ControlRoom", "ControlRoom" };
            var panels = new List<Texture2D>(8);
            try
            {
                var overviewBounds = CalculateVisibleBounds(rooms["EngineRoom"]);
                overviewBounds.Encapsulate(CalculateVisibleBounds(rooms["Cockpit"]));
                overviewBounds.Encapsulate(CalculateVisibleBounds(rooms["ControlRoom"]));
                overviewBounds.Encapsulate(CalculateVisibleBounds(modules["SC-H01"].gameObject));
                overviewBounds.Encapsulate(CalculateVisibleBounds(modules["SC-H02"].gameObject));
                overviewBounds.Encapsulate(CalculateVisibleBounds(modules["SC-H05"].gameObject));
                panels.Add(RenderTopDownBounds(
                    camera,
                    overviewBounds,
                    Quaternion.Euler(90f, 0f, 0f),
                    panelWidth,
                    panelHeight));
                panels.Add(RenderBoundsOblique(
                    camera,
                    overviewBounds,
                    panelWidth,
                    panelHeight));
                for (var corridorIndex = 0; corridorIndex < corridorIds.Length; corridorIndex++)
                {
                    var module = modules[corridorIds[corridorIndex]];
                    var from = GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        fromRoomIds[corridorIndex],
                        toRoomIds[corridorIndex]);
                    var to = GetTriRoomFlushEntranceAnchor(
                        targetScene,
                        rooms,
                        toRoomIds[corridorIndex],
                        fromRoomIds[corridorIndex]);
                    if (corridorIndex < 2)
                    {
                        panels.Add(RenderTriRoomEntranceTopDown(
                            camera,
                            corridorIndex == 0 ? to : from,
                            panelWidth,
                            panelHeight));
                    }
                    else
                    {
                        panels.Add(RenderTriRoomConnectionTopDown(
                            camera,
                            module,
                            from,
                            to,
                            panelWidth,
                            panelHeight));
                    }
                    if (corridorIndex < 2)
                    {
                        panels.Add(RenderTriRoomEntranceTopDown(
                            camera,
                            corridorIndex == 0 ? from : to,
                            panelWidth,
                            panelHeight));
                    }
                    else
                    {
                        panels.Add(RenderTriRoomConnectionOblique(
                            camera,
                            rooms[fromRoomIds[corridorIndex]],
                            rooms[toRoomIds[corridorIndex]],
                            module,
                            panelWidth,
                            panelHeight));
                    }
                }

                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    var column = panelIndex % columns;
                    var row = panelIndex / columns;
                    var panelX = gutter + (column * (panelWidth + gutter));
                    var panelY = gutter + ((rows - row - 1) * (panelHeight + gutter));
                    sheet.SetPixels(
                        panelX,
                        panelY,
                        panelWidth,
                        panelHeight,
                        panels[panelIndex].GetPixels());
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    UnityEngine.Object.DestroyImmediate(panels[panelIndex]);
                }

                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.CloseScene(temporaryScene, true);
            }
        }

        private static void CaptureTriRoomCorridorConnectionReview(string outputPath)
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Tri-room connection capture stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var rooms = RequireRooms(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            const int panelWidth = 1200;
            const int panelHeight = 800;
            const int gutter = 12;
            var sheetWidth = (panelWidth * 2) + (gutter * 3);
            var sheetHeight = (panelHeight * 2) + (gutter * 3);
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = new Color(0.018f, 0.022f, 0.028f, 1f);
            }

            sheet.SetPixels(backgroundPixels);
            var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            temporaryScene.name = "__PegasusTriRoomConnectionCapture";
            var cameraObject = new GameObject("__PegasusTriRoomConnectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            var lightObject = new GameObject("__PegasusTriRoomConnectionLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, temporaryScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.9f, 0.95f, 1f, 1f);
            light.intensity = 2.25f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            var panels = new List<Texture2D>(4);
            try
            {
                panels.Add(RenderTopDownBounds(
                    camera,
                    CalculateSceneVisibleBounds(targetScene),
                    Quaternion.Euler(90f, 0f, 0f),
                    panelWidth,
                    panelHeight));
                panels.Add(RenderTriRoomConnectionTopDown(
                    camera,
                    modules["SC-H01"],
                    GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "Cockpit"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "EngineRoom"),
                    panelWidth,
                    panelHeight));
                panels.Add(RenderTriRoomConnectionTopDown(
                    camera,
                    modules["SC-H02"],
                    GetTriRoomEntranceAnchor(targetScene, rooms, "Cockpit", "ControlRoom"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "Cockpit"),
                    panelWidth,
                    panelHeight));
                panels.Add(RenderTriRoomConnectionTopDown(
                    camera,
                    modules["SC-H05"],
                    GetTriRoomEntranceAnchor(targetScene, rooms, "EngineRoom", "ControlRoom"),
                    GetTriRoomEntranceAnchor(targetScene, rooms, "ControlRoom", "EngineRoom"),
                    panelWidth,
                    panelHeight));

                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    var column = panelIndex % 2;
                    var row = panelIndex / 2;
                    var panelX = gutter + (column * (panelWidth + gutter));
                    var panelY = gutter + ((1 - row) * (panelHeight + gutter));
                    sheet.SetPixels(
                        panelX,
                        panelY,
                        panelWidth,
                        panelHeight,
                        panels[panelIndex].GetPixels());
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                Debug.Log(
                    "Pegasus tri-room corridor direct-review capture completed. " +
                    "Panels=Overview,SC-H01,SC-H02,SC-H05; Output=" + outputPath);
            }
            finally
            {
                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    UnityEngine.Object.DestroyImmediate(panels[panelIndex]);
                }

                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.CloseScene(temporaryScene, true);
            }
        }

        private static Texture2D RenderTriRoomConnectionTopDown(
            Camera camera,
            Transform module,
            EntranceConnectionAnchor from,
            EntranceConnectionAnchor to,
            int width,
            int height)
        {
            var center = (from.Point + to.Point) * 0.5f;
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var worldWidth = localBounds.size.z * module.TransformVector(Vector3.forward).magnitude;
            var aspect = width / (float)height;
            var horizontalDelta = Mathf.Abs(to.Point.x - from.Point.x) + worldWidth + 10f;
            var verticalDelta = Mathf.Abs(to.Point.z - from.Point.z) + worldWidth + 10f;
            camera.orthographicSize = Mathf.Max(
                verticalDelta * 0.5f,
                (horizontalDelta / aspect) * 0.5f);
            camera.transform.position = new Vector3(
                center.x,
                Mathf.Max(from.Point.y, to.Point.y) + 80f,
                center.z);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderTriRoomEntranceTopDown(
            Camera camera,
            EntranceConnectionAnchor entrance,
            int width,
            int height)
        {
            camera.orthographicSize = 11f;
            camera.transform.position = new Vector3(
                entrance.Point.x,
                entrance.Point.y + 80f,
                entrance.Point.z);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderBoundsOblique(
            Camera camera,
            Bounds bounds,
            int width,
            int height)
        {
            var horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(12f, horizontalSize * 0.58f);
            camera.transform.position = bounds.center + new Vector3(
                -horizontalSize * 0.68f,
                horizontalSize * 0.72f,
                -horizontalSize * 0.68f);
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderTriRoomConnectionOblique(
            Camera camera,
            GameObject fromRoom,
            GameObject toRoom,
            Transform module,
            int width,
            int height)
        {
            var bounds = CalculateVisibleBounds(fromRoom);
            bounds.Encapsulate(CalculateVisibleBounds(toRoom));
            bounds.Encapsulate(CalculateVisibleBounds(module.gameObject));
            return RenderBoundsOblique(camera, bounds, width, height);
        }

        private static void CaptureHorizontalCorridorLengthReview()
        {
            var targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            if (targetScene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pegasus has unsaved editor changes. Horizontal corridor capture stopped.");
            }

            RequireOnlyApprovedTargetRoots(targetScene);
            var corridorRoot = RequireSceneObject(targetScene, CorridorRootName);
            var modules = RequireCorridorModules(corridorRoot.transform);
            var outputPath = Path.Combine(
                ProjectRoot,
                HorizontalCorridorLogDirectory.Replace('/', Path.DirectorySeparatorChar),
                HorizontalCorridorReviewName);
            CaptureHorizontalCorridorLengthSheet(targetScene, modules, outputPath);
            Debug.Log(
                "Pegasus horizontal corridor direct-review capture completed. " +
                "Panels=Overview,SC-H01,SC-H02,SC-H03,SC-H04,SC-H05; Output=" + outputPath);
        }

        private static void CaptureHorizontalCorridorLengthSheet(
            Scene targetScene,
            IReadOnlyDictionary<string, Transform> modules,
            string outputPath)
        {
            const int panelWidth = 1200;
            const int panelHeight = 700;
            const int gutter = 12;
            const int columns = 2;
            const int rows = 3;
            var sheetWidth = (panelWidth * columns) + (gutter * (columns + 1));
            var sheetHeight = (panelHeight * rows) + (gutter * (rows + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            var background = new Color(0.018f, 0.022f, 0.028f, 1f);
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = background;
            }

            sheet.SetPixels(backgroundPixels);
            var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            temporaryScene.name = "__PegasusHorizontalCorridorCapture";
            var cameraObject = new GameObject("__PegasusHorizontalCorridorCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.orthographic = true;
            var lightObject = new GameObject("__PegasusHorizontalCorridorLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, temporaryScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.92f, 1f, 1f);
            light.intensity = 2.25f;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            var panels = new List<Texture2D>(6);
            try
            {
                var sceneBounds = CalculateSceneVisibleBounds(targetScene);
                panels.Add(RenderTopDownBounds(
                    camera,
                    sceneBounds,
                    Quaternion.Euler(90f, 0f, 0f),
                    panelWidth,
                    panelHeight));
                for (var corridorIndex = 0;
                     corridorIndex < CorridorDefinitions.Length;
                     corridorIndex++)
                {
                    var definition = CorridorDefinitions[corridorIndex];
                    if (definition.IsSloped)
                    {
                        continue;
                    }

                    var module = modules[definition.ModuleId];
                    var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
                    panels.Add(RenderHorizontalCorridorOblique(
                        camera,
                        module,
                        localBounds,
                        panelWidth,
                        panelHeight));
                }

                if (panels.Count != 6)
                {
                    throw new InvalidOperationException(
                        "Horizontal corridor review requires one overview and five close-ups.");
                }

                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    var column = panelIndex % columns;
                    var row = panelIndex / columns;
                    var panelX = gutter + (column * (panelWidth + gutter));
                    var panelY = gutter + ((rows - row - 1) * (panelHeight + gutter));
                    sheet.SetPixels(
                        panelX,
                        panelY,
                        panelWidth,
                        panelHeight,
                        panels[panelIndex].GetPixels());
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                for (var panelIndex = 0; panelIndex < panels.Count; panelIndex++)
                {
                    UnityEngine.Object.DestroyImmediate(panels[panelIndex]);
                }

                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.CloseScene(temporaryScene, true);
            }
        }

        private static Texture2D RenderTopDownBounds(
            Camera camera,
            Bounds bounds,
            Quaternion rotation,
            int width,
            int height,
            float alignedWidth = -1f,
            float alignedDepth = -1f)
        {
            var aspect = width / (float)height;
            var viewWidth = alignedWidth > 0f ? alignedWidth : bounds.size.x;
            var viewDepth = alignedDepth > 0f ? alignedDepth : bounds.size.z;
            camera.orthographicSize = Mathf.Max(
                viewDepth * 0.6f,
                (viewWidth / aspect) * 0.6f);
            camera.transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + Mathf.Max(50f, bounds.size.y * 5f),
                bounds.center.z);
            camera.transform.rotation = rotation;
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderHorizontalCorridorOblique(
            Camera camera,
            Transform module,
            Bounds localBounds,
            int width,
            int height)
        {
            var aspect = width / (float)height;
            var worldCenter = module.TransformPoint(localBounds.center);
            var localViewDirection = new Vector3(0f, -0.72f, 1f).normalized;
            var worldViewDirection = module.TransformDirection(localViewDirection).normalized;
            var distance = Mathf.Max(35f, localBounds.size.x * 1.5f);

            camera.orthographicSize = Mathf.Max(
                (localBounds.size.y + localBounds.size.z) * 0.85f,
                (localBounds.size.x / aspect) * 0.68f);
            camera.transform.position = worldCenter - (worldViewDirection * distance);
            camera.transform.rotation = Quaternion.LookRotation(worldViewDirection, module.up);
            return RenderCamera(camera, width, height);
        }

        private static Bounds CalculateSceneVisibleBounds(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var found = false;
            var bounds = new Bounds();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (!TryCalculateVisibleBounds(roots[rootIndex], out var rootBounds))
                {
                    continue;
                }

                if (!found)
                {
                    bounds = rootBounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(rootBounds);
                }
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    "Pegasus has no visible geometry for horizontal corridor review.");
            }

            return bounds;
        }

        private static void CaptureInteriorComparisonSheet(
            Scene sourceScene,
            Scene targetScene,
            string outputPath)
        {
            var sourceRooms = RequireRooms(sourceScene);
            var targetRooms = RequireRooms(targetScene);
            var comparisonRoomIds = new[] { "EngineRoom", "Cockpit", "ControlRoom" };
            const int panelWidth = 1100;
            const int panelHeight = 720;
            const int gutter = 12;
            var sheetWidth = (panelWidth * 2) + (gutter * 3);
            var sheetHeight = (panelHeight * comparisonRoomIds.Length) +
                (gutter * (comparisonRoomIds.Length + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var background = new Color(0.018f, 0.022f, 0.028f, 1f);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = background;
            }

            sheet.SetPixels(backgroundPixels);
            var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            temporaryScene.name = "__PegasusInteriorComparison";
            var cameraObject = new GameObject("__PegasusInteriorComparisonCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.fieldOfView = 42f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var previewLayer = FindUnusedPreviewLayer(sourceScene, targetScene);
            camera.cullingMask = 1 << previewLayer;
            var lightObject = new GameObject("__PegasusInteriorComparisonLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, temporaryScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.92f, 1f, 1f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << previewLayer;

            try
            {
                for (var roomIndex = 0; roomIndex < comparisonRoomIds.Length; roomIndex++)
                {
                    var roomId = comparisonRoomIds[roomIndex];
                    var sourceVisualRoots = RequireDetachedVisualRoots(
                        sourceScene,
                        sourceRooms[roomId].transform,
                        roomId,
                        true);
                    var targetVisualRoots = RequireDetachedVisualRoots(
                        targetScene,
                        targetRooms[roomId].transform,
                        roomId,
                        false);
                    var sourcePanel = RenderDetachedInteriorPanel(
                        camera,
                        light,
                        temporaryScene,
                        previewLayer,
                        sourceRooms[roomId].transform,
                        sourceVisualRoots,
                        panelWidth,
                        panelHeight);
                    var targetPanel = RenderDetachedInteriorPanel(
                        camera,
                        light,
                        temporaryScene,
                        previewLayer,
                        targetRooms[roomId].transform,
                        targetVisualRoots,
                        panelWidth,
                        panelHeight);
                    try
                    {
                        var panelY = gutter +
                            ((comparisonRoomIds.Length - roomIndex - 1) * (panelHeight + gutter));
                        sheet.SetPixels(
                            gutter,
                            panelY,
                            panelWidth,
                            panelHeight,
                            sourcePanel.GetPixels());
                        sheet.SetPixels(
                            (gutter * 2) + panelWidth,
                            panelY,
                            panelWidth,
                            panelHeight,
                            targetPanel.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(sourcePanel);
                        UnityEngine.Object.DestroyImmediate(targetPanel);
                    }
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.CloseScene(temporaryScene, true);
            }
        }

        private static void CaptureRoomInteriorProportionComparisonSheet(
            Scene sourceScene,
            Scene targetScene,
            string outputPath)
        {
            var sourceRooms = RequireRooms(sourceScene);
            var targetRooms = RequireRooms(targetScene);
            const int panelWidth = 900;
            const int panelHeight = 560;
            const int gutter = 12;
            var sheetWidth = (panelWidth * 2) + (gutter * 3);
            var sheetHeight = (panelHeight * RoomDefinitions.Length) +
                (gutter * (RoomDefinitions.Length + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = new Color(0.018f, 0.022f, 0.028f, 1f);
            }

            sheet.SetPixels(backgroundPixels);
            var temporaryScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            temporaryScene.name = "__PegasusRoomInteriorProportionComparison";
            var previewLayer = FindUnusedPreviewLayer(sourceScene, targetScene);
            var cameraObject = new GameObject("__PegasusRoomInteriorProportionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, temporaryScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            camera.cullingMask = 1 << previewLayer;
            var lightObject = new GameObject("__PegasusRoomInteriorProportionLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, temporaryScene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.92f, 0.96f, 1f, 1f);
            light.intensity = 1.65f;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << previewLayer;
            light.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

            try
            {
                for (var roomIndex = 0; roomIndex < RoomDefinitions.Length; roomIndex++)
                {
                    var definition = RoomDefinitions[roomIndex];
                    var sourcePanel = RenderRoomInteriorProportionPanel(
                        camera,
                        temporaryScene,
                        previewLayer,
                        sourceScene,
                        sourceRooms[definition.Id].transform,
                        definition.Id,
                        true,
                        panelWidth,
                        panelHeight);
                    var targetPanel = RenderRoomInteriorProportionPanel(
                        camera,
                        temporaryScene,
                        previewLayer,
                        targetScene,
                        targetRooms[definition.Id].transform,
                        definition.Id,
                        false,
                        panelWidth,
                        panelHeight);
                    try
                    {
                        var panelY = gutter +
                            ((RoomDefinitions.Length - roomIndex - 1) * (panelHeight + gutter));
                        sheet.SetPixels(
                            gutter,
                            panelY,
                            panelWidth,
                            panelHeight,
                            sourcePanel.GetPixels());
                        sheet.SetPixels(
                            (gutter * 2) + panelWidth,
                            panelY,
                            panelWidth,
                            panelHeight,
                            targetPanel.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(sourcePanel);
                        UnityEngine.Object.DestroyImmediate(targetPanel);
                    }
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.CloseScene(temporaryScene, true);
            }
        }

        private static Texture2D RenderRoomInteriorProportionPanel(
            Camera camera,
            Scene temporaryScene,
            int previewLayer,
            Scene sourceScene,
            Transform room,
            string roomId,
            bool includeDetachedSourceInteriors,
            int width,
            int height)
        {
            var clone = UnityEngine.Object.Instantiate(room.gameObject);
            clone.name = "__RoomInteriorProportion_" + roomId;
            clone.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(clone, temporaryScene);
            clone.transform.position = Vector3.zero;
            clone.transform.rotation = Quaternion.identity;
            if (includeDetachedSourceInteriors)
            {
                var copiedDefinitions = GetInteriorCopyDefinitions();
                for (var definitionIndex = 0;
                     definitionIndex < copiedDefinitions.Count;
                     definitionIndex++)
                {
                    var definition = copiedDefinitions[definitionIndex];
                    if (definition.RoomId != roomId)
                    {
                        continue;
                    }

                    var existing = FindDirectChild(clone.transform, definition.RootName);
                    if (existing != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    }

                    var sourceInterior = RequireRootSceneObject(sourceScene, definition.RootName);
                    var interiorClone = UnityEngine.Object.Instantiate(sourceInterior);
                    interiorClone.name = sourceInterior.name;
                    interiorClone.hideFlags = HideFlags.HideAndDontSave;
                    SceneManager.MoveGameObjectToScene(interiorClone, temporaryScene);
                    interiorClone.transform.SetParent(clone.transform, false);
                    ApplyRoomRelativeTransform(room, sourceInterior.transform, interiorClone.transform);
                }
            }

            SetLayerRecursively(clone.transform, previewLayer);
            try
            {
                var bounds = CalculateVisibleBounds(clone);
                var floorTop = GetMainFloorTop(clone);
                var ceilingTop = GetMainCeilingTop(clone);
                camera.orthographic = true;
                var aspect = width / (float)height;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.z * 1.04f,
                    (bounds.extents.x / aspect) * 1.04f);
                camera.transform.position = new Vector3(
                    bounds.center.x,
                    Mathf.Max(floorTop + 0.5f, ceilingTop - 0.45f),
                    bounds.center.z);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                return RenderCamera(camera, width, height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static List<GameObject> RequireDetachedVisualRoots(
            Scene scene,
            Transform roomRoot,
            string roomId,
            bool sourceRootsAreSceneRoots)
        {
            var roots = new List<GameObject>();
            for (var interiorIndex = 0; interiorIndex < InteriorRootDefinitions.Length; interiorIndex++)
            {
                var definition = InteriorRootDefinitions[interiorIndex];
                if (definition.RoomId != roomId)
                {
                    continue;
                }

                var root = sourceRootsAreSceneRoots
                    ? RequireRootSceneObject(scene, definition.RootName)
                    : (FindDirectChild(roomRoot, definition.RootName) ??
                        throw new InvalidOperationException(
                            "Pegasus is missing detached interior root " + definition.RootName)).gameObject;
                roots.Add(root);
            }

            if (roots.Count == 0)
            {
                throw new InvalidOperationException(
                    "No detached visual interior roots were registered for " + roomId);
            }

            return roots;
        }

        private static Texture2D RenderDetachedInteriorPanel(
            Camera camera,
            Light light,
            Scene temporaryScene,
            int previewLayer,
            Transform roomRoot,
            IReadOnlyList<GameObject> visualRoots,
            int width,
            int height)
        {
            var previewContainer = new GameObject("__PegasusInteriorPreviewGroup")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = previewLayer
            };
            SceneManager.MoveGameObjectToScene(previewContainer, temporaryScene);
            var previewRoots = new List<GameObject>(visualRoots.Count);
            for (var rootIndex = 0; rootIndex < visualRoots.Count; rootIndex++)
            {
                var sourceRoot = visualRoots[rootIndex];
                var clone = UnityEngine.Object.Instantiate(sourceRoot);
                clone.name = sourceRoot.name;
                clone.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(clone, temporaryScene);
                clone.transform.SetParent(previewContainer.transform, false);
                ApplyRoomRelativeTransform(roomRoot, sourceRoot.transform, clone.transform);
                SetLayerRecursively(clone.transform, previewLayer);
                previewRoots.Add(clone);
            }

            var renderers = new List<Renderer>();
            var foundBounds = false;
            var bounds = new Bounds();
            for (var rootIndex = 0; rootIndex < previewRoots.Count; rootIndex++)
            {
                var rootRenderers = previewRoots[rootIndex].GetComponentsInChildren<Renderer>(true);
                for (var rendererIndex = 0; rendererIndex < rootRenderers.Length; rendererIndex++)
                {
                    var renderer = rootRenderers[rendererIndex];
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    renderers.Add(renderer);
                    if (!foundBounds)
                    {
                        bounds = renderer.bounds;
                        foundBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (!foundBounds)
            {
                UnityEngine.Object.DestroyImmediate(previewContainer);
                throw new InvalidOperationException(
                    "Detached interior group has no active visible renderer for direct comparison: " +
                    roomRoot.name);
            }

            camera.orthographic = false;
            var viewDirection = new Vector3(-0.85f, 0.65f, -1f).normalized;
            var distance = Mathf.Max(bounds.extents.magnitude * 2.4f, 3f);
            camera.transform.position = bounds.center + (viewDirection * distance);
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
            light.transform.rotation = Quaternion.Euler(48f, 25f, 0f);
            try
            {
                return RenderCamera(camera, width, height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(previewContainer);
            }
        }

        private static int FindUnusedPreviewLayer(Scene sourceScene, Scene targetScene)
        {
            for (var candidateLayer = 31; candidateLayer >= 8; candidateLayer--)
            {
                if (!SceneUsesLayer(sourceScene, candidateLayer) &&
                    !SceneUsesLayer(targetScene, candidateLayer))
                {
                    return candidateLayer;
                }
            }

            throw new InvalidOperationException(
                "No unused Unity layer is available for the isolated interior comparison capture.");
        }

        private static bool SceneUsesLayer(Scene scene, int layer)
        {
            var roots = scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    if (transforms[transformIndex].gameObject.layer == layer)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                transforms[transformIndex].gameObject.layer = layer;
            }
        }

        private static void CaptureLayout(Scene scene, string outputPath)
        {
            var roots = scene.GetRootGameObjects();
            var found = false;
            var bounds = new Bounds();
            for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                var renderers = roots[rootIndex].GetComponentsInChildren<Renderer>(true);
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        bounds = renderer.bounds;
                        found = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("Pegasus has no visible geometry to capture.");
            }

            const int panelWidth = 1400;
            const int panelHeight = 1000;
            const int gutter = 12;
            var sheetWidth = (panelWidth * 2) + (gutter * 3);
            var sheetHeight = (panelHeight * 2) + (gutter * 3);
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var background = new Color(0.018f, 0.022f, 0.028f, 1f);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = background;
            }

            sheet.SetPixels(backgroundPixels);
            var cameraObject = new GameObject("__PegasusLayoutInspectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.032f, 0.039f, 0.048f, 1f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 1000f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var lightObject = new GameObject("__PegasusLayoutInspectionLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.92f, 1f, 1f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;

            try
            {
                var aspect = panelWidth / (float)panelHeight;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.z * 1.12f,
                    (bounds.extents.x / aspect) * 1.12f);
                camera.transform.position = new Vector3(
                    bounds.center.x,
                    bounds.max.y + Mathf.Max(80f, bounds.size.y * 4f),
                    bounds.center.z);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                var topView = RenderCamera(camera, panelWidth, panelHeight);

                camera.orthographic = false;
                camera.fieldOfView = 48f;
                var viewDirection = new Vector3(-0.85f, 1.05f, -0.9f).normalized;
                var distance = Mathf.Max(bounds.extents.magnitude * 1.7f, 80f);
                camera.transform.position = bounds.center + (viewDirection * distance);
                camera.transform.rotation = Quaternion.LookRotation(
                    bounds.center - camera.transform.position,
                    Vector3.up);
                light.transform.rotation = Quaternion.Euler(48f, 25f, 0f);
                var perspectiveView = RenderCamera(camera, panelWidth, panelHeight);
                var rooms = RequireRooms(scene);
                var cockpitBounds = CalculateVisibleBounds(rooms["Cockpit"]);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    cockpitBounds.extents.z * 1.3f,
                    (cockpitBounds.extents.x / aspect) * 1.3f);
                camera.transform.position = new Vector3(
                    cockpitBounds.center.x,
                    cockpitBounds.max.y + Mathf.Max(30f, cockpitBounds.size.y * 4f),
                    cockpitBounds.center.z);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
                var cockpitTopView = RenderCamera(camera, panelWidth, panelHeight);

                var cargoBounds = CalculateVisibleBounds(rooms["CargoHold"]);
                camera.orthographicSize = Mathf.Max(
                    cargoBounds.extents.z * 1.3f,
                    (cargoBounds.extents.x / aspect) * 1.3f);
                camera.transform.position = new Vector3(
                    cargoBounds.center.x,
                    cargoBounds.max.y + Mathf.Max(40f, cargoBounds.size.y * 4f),
                    cargoBounds.center.z);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var cargoTopView = RenderCamera(camera, panelWidth, panelHeight);
                try
                {
                    sheet.SetPixels(
                        gutter,
                        (gutter * 2) + panelHeight,
                        panelWidth,
                        panelHeight,
                        topView.GetPixels());
                    sheet.SetPixels(
                        (gutter * 2) + panelWidth,
                        (gutter * 2) + panelHeight,
                        panelWidth,
                        panelHeight,
                        perspectiveView.GetPixels());
                    sheet.SetPixels(
                        gutter,
                        gutter,
                        panelWidth,
                        panelHeight,
                        cockpitTopView.GetPixels());
                    sheet.SetPixels(
                        (gutter * 2) + panelWidth,
                        gutter,
                        panelWidth,
                        panelHeight,
                        cargoTopView.GetPixels());
                    sheet.Apply(false, false);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                    File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(topView);
                    UnityEngine.Object.DestroyImmediate(perspectiveView);
                    UnityEngine.Object.DestroyImmediate(cockpitTopView);
                    UnityEngine.Object.DestroyImmediate(cargoTopView);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static Texture2D RenderCamera(Camera camera, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                return texture;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static float ReadReportFloat(string path, string prefix)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                if (!lines[lineIndex].StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                return float.Parse(
                    lines[lineIndex].Substring(prefix.Length),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException(
                "Required numeric report field is missing. Path=" + path +
                "; Prefix=" + prefix);
        }

        private static void WriteProjectText(string directory, string fileName, string content)
        {
            var outputDirectory = Path.Combine(
                ProjectRoot,
                directory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(
                Path.Combine(outputDirectory, fileName),
                content,
                new UTF8Encoding(false));
        }

        private static string SanitizeName(string value)
        {
            var builder = new StringBuilder(value.Length);
            for (var characterIndex = 0; characterIndex < value.Length; characterIndex++)
            {
                var character = value[characterIndex];
                builder.Append(char.IsLetterOrDigit(character) ? character : '_');
            }

            return builder.ToString();
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= 0.00000001f;
        }

        private static bool Approximately(Quaternion left, Quaternion right)
        {
            return Quaternion.Angle(left, right) <= RotationTolerance;
        }

        private static string Float(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vector(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######})",
                value.x,
                value.y,
                value.z);
        }

        private static string QuaternionText(Quaternion value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######},{3:0.######})",
                value.x,
                value.y,
                value.z,
                value.w);
        }

        private static string BoundsText(Bounds bounds)
        {
            return "Center=" + Vector(bounds.center) + "; Size=" + Vector(bounds.size);
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private readonly struct RoomDefinition
        {
            internal RoomDefinition(
                string id,
                string displayName,
                string rootName,
                Vector2 mapPosition,
                bool isCargo = false,
                float layoutYawDegrees = 0f)
            {
                Id = id;
                DisplayName = displayName;
                RootName = rootName;
                MapPosition = mapPosition;
                IsCargo = isCargo;
                LayoutYawDegrees = layoutYawDegrees;
            }

            internal string Id { get; }
            internal string DisplayName { get; }
            internal string RootName { get; }
            internal Vector2 MapPosition { get; }
            internal bool IsCargo { get; }
            internal float LayoutYawDegrees { get; }
        }

        private readonly struct CorridorDefinition
        {
            internal CorridorDefinition(
                string moduleId,
                string fromRoomId,
                string toRoomId,
                bool isSloped)
            {
                ModuleId = moduleId;
                FromRoomId = fromRoomId;
                ToRoomId = toRoomId;
                IsSloped = isSloped;
            }

            internal string ModuleId { get; }
            internal string FromRoomId { get; }
            internal string ToRoomId { get; }
            internal bool IsSloped { get; }
        }

        private readonly struct InteriorRootDefinition
        {
            internal InteriorRootDefinition(string roomId, string rootName)
            {
                RoomId = roomId;
                RootName = rootName;
            }

            internal string RoomId { get; }
            internal string RootName { get; }
        }

        private readonly struct CeilingFollower
        {
            internal CeilingFollower(
                Transform transform,
                Vector3 moduleLocalPosition,
                Quaternion moduleLocalRotation)
            {
                Transform = transform;
                ModuleLocalPosition = moduleLocalPosition;
                ModuleLocalRotation = moduleLocalRotation;
            }

            internal Transform Transform { get; }
            internal Vector3 ModuleLocalPosition { get; }
            internal Quaternion ModuleLocalRotation { get; }
        }

        private readonly struct TriRoomUniformPlacement
        {
            internal TriRoomUniformPlacement(
                Vector3 engineRoomPosition,
                Vector3 cockpitPosition,
                Vector3 controlRoomPosition,
                float targetAnchorDistance,
                float score)
            {
                EngineRoomPosition = engineRoomPosition;
                CockpitPosition = cockpitPosition;
                ControlRoomPosition = controlRoomPosition;
                TargetAnchorDistance = targetAnchorDistance;
                Score = score;
            }

            internal Vector3 EngineRoomPosition { get; }
            internal Vector3 CockpitPosition { get; }
            internal Vector3 ControlRoomPosition { get; }
            internal float TargetAnchorDistance { get; }
            internal float Score { get; }
        }

        private readonly struct EntranceConnectionAnchor
        {
            internal EntranceConnectionAnchor(
                string label,
                Vector3 point,
                Vector3 outward,
                float floorY)
            {
                Label = label;
                Point = point;
                Outward = outward;
                FloorY = floorY;
            }

            internal string Label { get; }
            internal Vector3 Point { get; }
            internal Vector3 Outward { get; }
            internal float FloorY { get; }
        }

        private readonly struct CorridorConnectionSegment
        {
            internal CorridorConnectionSegment(
                string id,
                Vector3 from,
                Vector3 to,
                float width)
            {
                Id = id;
                From = from;
                To = to;
                Width = width;
            }

            internal string Id { get; }
            internal Vector3 From { get; }
            internal Vector3 To { get; }
            internal float Width { get; }
        }

        private readonly struct ConnectorEndpointDefinition
        {
            internal ConnectorEndpointDefinition(
                string id,
                string corridorId,
                string roomId,
                string otherRoomId,
                bool lowEnd)
            {
                Id = id;
                CorridorId = corridorId;
                RoomId = roomId;
                OtherRoomId = otherRoomId;
                LowEnd = lowEnd;
            }

            internal string Id { get; }
            internal string CorridorId { get; }
            internal string RoomId { get; }
            internal string OtherRoomId { get; }
            internal bool LowEnd { get; }
        }

        private readonly struct ConnectorPrismCorners
        {
            internal ConnectorPrismCorners(
                Vector3 roomMin,
                Vector3 roomMax,
                Vector3 corridorMax,
                Vector3 corridorMin,
                float roomBottomY,
                float roomTopY,
                float corridorBottomY,
                float corridorTopY)
            {
                RoomMin = roomMin;
                RoomMax = roomMax;
                CorridorMax = corridorMax;
                CorridorMin = corridorMin;
                RoomBottomY = roomBottomY;
                RoomTopY = roomTopY;
                CorridorBottomY = corridorBottomY;
                CorridorTopY = corridorTopY;
            }

            internal Vector3 RoomMin { get; }
            internal Vector3 RoomMax { get; }
            internal Vector3 CorridorMax { get; }
            internal Vector3 CorridorMin { get; }
            internal float RoomBottomY { get; }
            internal float RoomTopY { get; }
            internal float CorridorBottomY { get; }
            internal float CorridorTopY { get; }
        }

        private readonly struct ConnectorGapShell
        {
            internal ConnectorGapShell(
                Vector3 roomOuterFloorMinimum,
                Vector3 roomOuterFloorMaximum,
                Vector3 roomFloorMinimum,
                Vector3 roomFloorMaximum,
                Vector3 roomCeilingMinimum,
                Vector3 roomCeilingMaximum,
                Vector3 roomOuterCeilingMinimum,
                Vector3 roomOuterCeilingMaximum,
                Vector3 corridorOuterFloorMinimum,
                Vector3 corridorOuterFloorMaximum,
                Vector3 corridorFloorMinimum,
                Vector3 corridorFloorMaximum,
                Vector3 corridorCeilingMinimum,
                Vector3 corridorCeilingMaximum,
                Vector3 corridorOuterCeilingMinimum,
                Vector3 corridorOuterCeilingMaximum)
            {
                RoomOuterFloorMinimum = roomOuterFloorMinimum;
                RoomOuterFloorMaximum = roomOuterFloorMaximum;
                RoomFloorMinimum = roomFloorMinimum;
                RoomFloorMaximum = roomFloorMaximum;
                RoomCeilingMinimum = roomCeilingMinimum;
                RoomCeilingMaximum = roomCeilingMaximum;
                RoomOuterCeilingMinimum = roomOuterCeilingMinimum;
                RoomOuterCeilingMaximum = roomOuterCeilingMaximum;
                CorridorOuterFloorMinimum = corridorOuterFloorMinimum;
                CorridorOuterFloorMaximum = corridorOuterFloorMaximum;
                CorridorFloorMinimum = corridorFloorMinimum;
                CorridorFloorMaximum = corridorFloorMaximum;
                CorridorCeilingMinimum = corridorCeilingMinimum;
                CorridorCeilingMaximum = corridorCeilingMaximum;
                CorridorOuterCeilingMinimum = corridorOuterCeilingMinimum;
                CorridorOuterCeilingMaximum = corridorOuterCeilingMaximum;
            }

            internal Vector3 RoomOuterFloorMinimum { get; }
            internal Vector3 RoomOuterFloorMaximum { get; }
            internal Vector3 RoomFloorMinimum { get; }
            internal Vector3 RoomFloorMaximum { get; }
            internal Vector3 RoomCeilingMinimum { get; }
            internal Vector3 RoomCeilingMaximum { get; }
            internal Vector3 RoomOuterCeilingMinimum { get; }
            internal Vector3 RoomOuterCeilingMaximum { get; }
            internal Vector3 CorridorOuterFloorMinimum { get; }
            internal Vector3 CorridorOuterFloorMaximum { get; }
            internal Vector3 CorridorFloorMinimum { get; }
            internal Vector3 CorridorFloorMaximum { get; }
            internal Vector3 CorridorCeilingMinimum { get; }
            internal Vector3 CorridorCeilingMaximum { get; }
            internal Vector3 CorridorOuterCeilingMinimum { get; }
            internal Vector3 CorridorOuterCeilingMaximum { get; }
        }

        private readonly struct ConnectorSolidCorners
        {
            internal ConnectorSolidCorners(
                Vector3 roomFirst,
                Vector3 roomSecond,
                Vector3 roomThird,
                Vector3 roomFourth,
                Vector3 corridorFirst,
                Vector3 corridorSecond,
                Vector3 corridorThird,
                Vector3 corridorFourth)
            {
                RoomFirst = roomFirst;
                RoomSecond = roomSecond;
                RoomThird = roomThird;
                RoomFourth = roomFourth;
                CorridorFirst = corridorFirst;
                CorridorSecond = corridorSecond;
                CorridorThird = corridorThird;
                CorridorFourth = corridorFourth;
            }

            internal Vector3 RoomFirst { get; }
            internal Vector3 RoomSecond { get; }
            internal Vector3 RoomThird { get; }
            internal Vector3 RoomFourth { get; }
            internal Vector3 CorridorFirst { get; }
            internal Vector3 CorridorSecond { get; }
            internal Vector3 CorridorThird { get; }
            internal Vector3 CorridorFourth { get; }
        }

        private readonly struct ConnectorSurfaceCorners
        {
            internal ConnectorSurfaceCorners(
                Vector3 first,
                Vector3 second,
                Vector3 third,
                Vector3 fourth)
            {
                First = first;
                Second = second;
                Third = third;
                Fourth = fourth;
            }

            internal Vector3 First { get; }
            internal Vector3 Second { get; }
            internal Vector3 Third { get; }
            internal Vector3 Fourth { get; }
        }

        private readonly struct ConnectorEntranceProfile
        {
            internal ConnectorEntranceProfile(
                EntranceConnectionAnchor entrance,
                Vector3 across,
                Vector3 planeCenter,
                Renderer floor,
                Renderer ceiling,
                Renderer minimumWall,
                Renderer maximumWall,
                float openingMinimum,
                float openingMaximum,
                float minimumWallMinimum,
                float minimumWallMaximum,
                float maximumWallMinimum,
                float maximumWallMaximum)
            {
                Entrance = entrance;
                Across = across;
                PlaneCenter = planeCenter;
                Floor = floor;
                Ceiling = ceiling;
                MinimumWall = minimumWall;
                MaximumWall = maximumWall;
                OpeningMinimum = openingMinimum;
                OpeningMaximum = openingMaximum;
                MinimumWallMinimum = minimumWallMinimum;
                MinimumWallMaximum = minimumWallMaximum;
                MaximumWallMinimum = maximumWallMinimum;
                MaximumWallMaximum = maximumWallMaximum;
            }

            internal EntranceConnectionAnchor Entrance { get; }
            internal Vector3 Across { get; }
            internal Vector3 PlaneCenter { get; }
            internal Renderer Floor { get; }
            internal Renderer Ceiling { get; }
            internal Renderer MinimumWall { get; }
            internal Renderer MaximumWall { get; }
            internal float OpeningMinimum { get; }
            internal float OpeningMaximum { get; }
            internal float MinimumWallMinimum { get; }
            internal float MinimumWallMaximum { get; }
            internal float MaximumWallMinimum { get; }
            internal float MaximumWallMaximum { get; }
        }

        private readonly struct ConnectorPartSource
        {
            internal ConnectorPartSource(
                string name,
                Renderer renderer,
                float roomBottomY,
                float roomTopY,
                float corridorBottomY,
                float corridorTopY)
            {
                Name = name;
                Renderer = renderer;
                RoomBottomY = roomBottomY;
                RoomTopY = roomTopY;
                CorridorBottomY = corridorBottomY;
                CorridorTopY = corridorTopY;
            }

            internal string Name { get; }
            internal Renderer Renderer { get; }
            internal float RoomBottomY { get; }
            internal float RoomTopY { get; }
            internal float CorridorBottomY { get; }
            internal float CorridorTopY { get; }
        }

        private readonly struct EntranceWidthPairDefinition
        {
            internal EntranceWidthPairDefinition(
                string roomId,
                string label,
                string firstWallName,
                string secondWallName,
                bool isExpansionReference = false)
            {
                RoomId = roomId;
                Label = label;
                FirstWallName = firstWallName;
                SecondWallName = secondWallName;
                IsExpansionReference = isExpansionReference;
            }

            internal string RoomId { get; }
            internal string Label { get; }
            internal string FirstWallName { get; }
            internal string SecondWallName { get; }
            internal bool IsExpansionReference { get; }
        }

        private readonly struct RoomInteriorPlacement
        {
            internal RoomInteriorPlacement(
                string roomId,
                Transform room,
                Transform target,
                Vector3 roomLocalVisibleCenter)
            {
                RoomId = roomId;
                Room = room;
                Target = target;
                RoomLocalVisibleCenter = roomLocalVisibleCenter;
            }

            internal string RoomId { get; }
            internal Transform Room { get; }
            internal Transform Target { get; }
            internal Vector3 RoomLocalVisibleCenter { get; }
        }
    }
}
