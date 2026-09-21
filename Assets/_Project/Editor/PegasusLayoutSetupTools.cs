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
        private const string PlayerSettingsPath =
            "Assets/_Project/Settings/Player/DefaultFirstPersonPlayerSettings.asset";
        private const float HorizontalCorridorTravelSeconds = 5f;
        private const string SourceReportName = "SourceInspection.txt";
        private const string InspectionReportName = "Inspection.txt";
        private const string ReviewCaptureName = "LayoutReview.png";
        private const string FinalCaptureName = "Final.png";
        private const float CommonCeilingTopY = 6f;
        private const float CargoCeilingDrop = 0.5f;
        private const float DetachedEndClearance = 2f;
        private const float TriRoomUniformConnectorGap = 1f;
        private const float TriRoomUniformConnectorGapTolerance = 0.01f;
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
                180f),
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
                return BuildWallPairEntranceAnchor(
                    "EngineRoomToCockpit",
                    rooms[roomId],
                    RequireRenderer(scene, "ER-01 1시 Cockpit corridor side wall 1"),
                    RequireRenderer(scene, "ER-01 1시 Cockpit corridor side wall 2"));
            }

            if (roomId == "EngineRoom" && otherRoomId == "ControlRoom")
            {
                return BuildWallPairEntranceAnchor(
                    "EngineRoomToControlRoom",
                    rooms[roomId],
                    RequireRenderer(scene, "ER-01 3시 Control corridor side wall 1"),
                    RequireRenderer(scene, "ER-01 3시 Control corridor side wall 2"));
            }

            if (roomId == "Cockpit" && otherRoomId == "EngineRoom")
            {
                return BuildSingleRendererEntranceAnchor(
                    "CockpitToEngineRoom",
                    rooms[roomId],
                    RequireRenderer(scene, "left future opening floor edge only"),
                    true);
            }

            if (roomId == "Cockpit" && otherRoomId == "ControlRoom")
            {
                return BuildSingleRendererEntranceAnchor(
                    "CockpitToControlRoom",
                    rooms[roomId],
                    RequireRenderer(scene, "right future opening floor edge only"),
                    true);
            }

            if (roomId == "ControlRoom" && otherRoomId == "Cockpit")
            {
                return BuildSingleRendererEntranceAnchor(
                    "ControlRoomToCockpit",
                    rooms[roomId],
                    RequireRenderer(
                        scene,
                        "CR-01 cockpit 40 degree outside only corridor floor continuation"),
                    true);
            }

            if (roomId == "ControlRoom" && otherRoomId == "EngineRoom")
            {
                return BuildSingleRendererEntranceAnchor(
                    "ControlRoomToEngineRoom",
                    rooms[roomId],
                    RequireRenderer(
                        scene,
                        "CR-01 engine room left separated corridor floor continuation"),
                    true);
            }

            throw new InvalidOperationException(
                "Unsupported Pegasus tri-room entrance pair: " + roomId + " -> " + otherRoomId);
        }

        private static EntranceConnectionAnchor BuildWallPairEntranceAnchor(
            string label,
            GameObject room,
            Renderer firstWall,
            Renderer secondWall)
        {
            var averageCenter = (firstWall.bounds.center + secondWall.bounds.center) * 0.5f;
            var outward = HorizontalDirection(averageCenter - room.transform.position);
            var outerProjection = Mathf.Max(
                GetRendererProjectionMaximum(firstWall, outward),
                GetRendererProjectionMaximum(secondWall, outward));
            var point = averageCenter +
                (outward * (outerProjection - Vector3.Dot(averageCenter, outward)));
            var floorY = Mathf.Min(firstWall.bounds.min.y, secondWall.bounds.min.y);
            point.y = floorY;
            return new EntranceConnectionAnchor(label, point, outward, floorY);
        }

        private static EntranceConnectionAnchor BuildSingleRendererEntranceAnchor(
            string label,
            GameObject room,
            Renderer renderer,
            bool useRendererTopAsFloor)
        {
            var outward = HorizontalDirection(renderer.bounds.center - room.transform.position);
            var outerProjection = GetRendererProjectionMaximum(renderer, outward);
            var point = renderer.bounds.center +
                (outward * (outerProjection - Vector3.Dot(renderer.bounds.center, outward)));
            var floorY = useRendererTopAsFloor ? renderer.bounds.max.y : renderer.bounds.min.y;
            point.y = floorY;
            return new EntranceConnectionAnchor(label, point, outward, floorY);
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
            var direction = HorizontalDirection(to.Point - from.Point);
            var center = (from.Point + to.Point) * 0.5f;
            var connectionLength = HorizontalDistance(from.Point, to.Point);
            var localBounds = CalculateVisibleBoundsInLocalSpace(module.gameObject, module);
            var worldWidth = localBounds.size.z * module.TransformVector(Vector3.forward).magnitude;
            var aspect = width / (float)height;
            var viewLength = connectionLength + 10f;
            var viewWidth = Mathf.Max(14f, worldWidth + 10f);
            camera.orthographicSize = Mathf.Max(
                viewWidth * 0.5f,
                (viewLength / aspect) * 0.55f);
            camera.transform.position = new Vector3(
                center.x,
                Mathf.Max(from.Point.y, to.Point.y) + 80f,
                center.z);
            camera.transform.rotation = Quaternion.LookRotation(
                Vector3.down,
                Vector3.Cross(direction, Vector3.up));
            return RenderCamera(camera, width, height);
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
    }
}
