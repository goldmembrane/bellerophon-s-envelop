using System;
using System.IO;
using System.Linq;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase4CargoShipGrayboxBootstrap
    {
        public const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        public const string GrayboxRootName = "Phase 4 Cargo Ship Graybox";

        private const string SettingsDirectory = "Assets/_Project/Settings/Ship";
        private const string ArtShipDirectory = "Assets/_Project/Art/Ship";
        private const string ArtShipMaterialsDirectory = ArtShipDirectory + "/Materials";
        private const string FloorMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorFloor_Rough.mat";
        private const string CorridorMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorCorridorFloor_Rough.mat";
        private const string WallMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorWall_Rough.mat";
        private const string CeilingMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorCeiling_Rough.mat";
        private const string DoorFrameMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorDoorFrame_Worn.mat";
        private const string CableMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorCableTray_Dark.mat";
        private const string DamageMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorDamageState_Warning.mat";
        private const string GlassMaterialPath = ArtShipMaterialsDirectory + "/CockpitGlass_Dirty.mat";
        private const string ConsoleMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorConsole_Aged.mat";
        private const string CargoMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorCargo_Worn.mat";
        private const string InteractableMaterialPath = ArtShipMaterialsDirectory + "/ShipInteriorInteractable_WornYellow.mat";
        private const float UpperDeckY = 0f;
        private const float CargoHoldDeckY = -3.0f;
        public const float ProductionCorridorWidth = 2.6f;
        public const float ProductionDoorWidth = 3.4f;
        public const float ProductionDoorHeight = 2.25f;
        public const float ProductionWallHeight = 2.75f;
        private const float WallThickness = 0.28f;
        private const float CeilingThickness = 0.12f;
        private const float CorridorFloorOverlap = 1.0f;
        private const float CorridorWallEndOverlap = 0.35f;
        private const float CorridorWallJointOverlap = -1.15f;
        private const float CorridorLandingExtraWidth = 1.6f;
        private const float CorridorThresholdSealDepth = 1.35f;
        public const float ProductionSlopedEndpointSealDepth = 3.2f;
        public const float ProductionThresholdMouthWidth = 4.4f;
        public const float ProductionJointSealSpan = ProductionCorridorWidth + (WallThickness * 2f);

        private static readonly RoomSpec[] Rooms =
        {
            new RoomSpec("Cargo Hold", new Vector3(0f, CargoHoldDeckY, 0f), new Vector2(12f, 12f)),
            new RoomSpec("Cockpit", new Vector3(0f, UpperDeckY, 18f), new Vector2(10f, 8f)),
            new RoomSpec("Engine Room", new Vector3(-14f, UpperDeckY, 18f), new Vector2(8f, 8f)),
            new RoomSpec("Control Room", new Vector3(14f, UpperDeckY, 18f), new Vector2(8f, 8f)),
            new RoomSpec("Armory", new Vector3(-14f, UpperDeckY, -14f), new Vector2(8f, 8f)),
            new RoomSpec("Supply Room", new Vector3(14f, UpperDeckY, -14f), new Vector2(8f, 8f))
        };

        private static readonly CorridorSpec[] Corridors =
        {
            new CorridorSpec("Cargo Hold", "Cockpit"),
            new CorridorSpec("Cargo Hold", "Engine Room"),
            new CorridorSpec("Cargo Hold", "Control Room"),
            new CorridorSpec("Cargo Hold", "Armory"),
            new CorridorSpec("Cargo Hold", "Supply Room"),
            new CorridorSpec("Supply Room", "Armory"),
            new CorridorSpec("Cockpit", "Engine Room"),
            new CorridorSpec("Cockpit", "Control Room"),
            new CorridorSpec("Engine Room", "Control Room")
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 4 Cargo Ship Graybox")]
        public static void EnsurePhase4Assets()
        {
            Directory.CreateDirectory(SettingsDirectory);
            Directory.CreateDirectory(ArtShipDirectory);
            Directory.CreateDirectory(ArtShipMaterialsDirectory);

            Phase2PlayerMvpBootstrap.EnsurePhase2Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(GrayboxRootName);
            DeleteGeneratedObject("Cargo Bay Test Floor");
            DeleteGeneratedObject("Cargo Bay Back Wall");
            DeleteGeneratedObject("Phase 2 Interaction Target");

            var floorMaterial = EnsureMaterial(FloorMaterialPath, new Color(0.16f, 0.17f, 0.15f, 1f));
            var corridorMaterial = EnsureMaterial(CorridorMaterialPath, new Color(0.11f, 0.12f, 0.11f, 1f));
            var wallMaterial = EnsureMaterial(WallMaterialPath, new Color(0.25f, 0.27f, 0.25f, 1f));
            var ceilingMaterial = EnsureMaterial(CeilingMaterialPath, new Color(0.12f, 0.13f, 0.12f, 1f));
            var doorFrameMaterial = EnsureMaterial(DoorFrameMaterialPath, new Color(0.34f, 0.33f, 0.28f, 1f));
            var cableMaterial = EnsureMaterial(CableMaterialPath, new Color(0.055f, 0.058f, 0.052f, 1f));
            var damageMaterial = EnsureMaterial(DamageMaterialPath, new Color(0.72f, 0.28f, 0.08f, 1f));
            var glassMaterial = EnsureMaterial(GlassMaterialPath, new Color(0.12f, 0.24f, 0.27f, 0.55f));
            var consoleMaterial = EnsureMaterial(ConsoleMaterialPath, new Color(0.06f, 0.075f, 0.07f, 1f));
            var cargoMaterial = EnsureMaterial(CargoMaterialPath, new Color(0.38f, 0.29f, 0.19f, 1f));
            var interactableMaterial = EnsureMaterial(InteractableMaterialPath, new Color(0.72f, 0.58f, 0.25f, 1f));

            var root = new GameObject(GrayboxRootName);
            CreateRooms(root.transform, floorMaterial, wallMaterial, ceilingMaterial, doorFrameMaterial, cableMaterial, damageMaterial);
            CreateCorridors(root.transform, corridorMaterial, wallMaterial, ceilingMaterial, doorFrameMaterial, cableMaterial);
            CreateRoomFeatures(root.transform, wallMaterial, glassMaterial, consoleMaterial, cargoMaterial, interactableMaterial, damageMaterial);
            CreateDirectionSigns(root.transform);
            ConfigurePlayerStart();
            ConfigureLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase4CargoShipGrayboxEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 4 cargo ship graybox assets are ready.");
        }

        private static void CreateRooms(
            Transform root,
            Material floorMaterial,
            Material wallMaterial,
            Material ceilingMaterial,
            Material doorFrameMaterial,
            Material cableMaterial,
            Material damageMaterial)
        {
            foreach (var room in Rooms)
            {
                var roomRoot = new GameObject("Room - " + room.Name);
                roomRoot.transform.SetParent(root, false);
                roomRoot.transform.position = room.Center;

                CreateRoomShell(roomRoot.transform, room, floorMaterial, wallMaterial, ceilingMaterial, doorFrameMaterial, cableMaterial, damageMaterial);
                CreateLabel("Label - " + room.Name, room.Name, roomRoot.transform, new Vector3(0f, 1.8f, -room.Size.y * 0.36f), 0f);
            }
        }

        private static void CreateRoomShell(
            Transform roomRoot,
            RoomSpec room,
            Material floorMaterial,
            Material wallMaterial,
            Material ceilingMaterial,
            Material doorFrameMaterial,
            Material cableMaterial,
            Material damageMaterial)
        {
            CreateBox(
                "Floor - " + room.Name,
                roomRoot,
                Vector3.down * 0.05f,
                new Vector3(room.Size.x, 0.1f, room.Size.y),
                Quaternion.identity,
                floorMaterial,
                true);

            CreateBox(
                "Ceiling - " + room.Name,
                roomRoot,
                new Vector3(0f, ProductionWallHeight + (CeilingThickness * 0.5f), 0f),
                new Vector3(room.Size.x, CeilingThickness, room.Size.y),
                Quaternion.identity,
                ceilingMaterial,
                false);

            var openings = GetDoorOpenings(room.Name);
            CreateRoomWall(roomRoot, room, WallSide.North, openings, wallMaterial, doorFrameMaterial);
            CreateRoomWall(roomRoot, room, WallSide.South, openings, wallMaterial, doorFrameMaterial);
            CreateRoomWall(roomRoot, room, WallSide.East, openings, wallMaterial, doorFrameMaterial);
            CreateRoomWall(roomRoot, room, WallSide.West, openings, wallMaterial, doorFrameMaterial);
            CreateRoomCableTrays(roomRoot, room, cableMaterial);
            CreateRoomWearPatches(roomRoot, room, damageMaterial);
        }

        private static void CreateCorridors(
            Transform root,
            Material floorMaterial,
            Material wallMaterial,
            Material ceilingMaterial,
            Material doorFrameMaterial,
            Material cableMaterial)
        {
            var corridorRoot = new GameObject("Corridors");
            corridorRoot.transform.SetParent(root, false);

            foreach (var corridor in Corridors)
            {
                var corridorName = "Corridor - " + corridor.From + " to " + corridor.To;
                CreateSegmentedCorridor(
                    corridorName,
                    corridor.From,
                    corridor.To,
                    corridorRoot.transform,
                    GetCorridorRoute(corridor.From, corridor.To),
                    ProductionCorridorWidth,
                    floorMaterial,
                    wallMaterial,
                    ceilingMaterial,
                    doorFrameMaterial,
                    cableMaterial);
            }
        }

        private static void CreateSegmentedCorridor(
            string name,
            string fromRoom,
            string toRoom,
            Transform parent,
            Vector3[] points,
            float width,
            Material floorMaterial,
            Material wallMaterial,
            Material ceilingMaterial,
            Material doorFrameMaterial,
            Material cableMaterial)
        {
            var corridorRoot = new GameObject(name);
            corridorRoot.transform.SetParent(parent, false);

            for (var i = 0; i < points.Length - 1; i++)
            {
                CreateCorridorSegment(
                    name + " Segment " + (i + 1),
                    corridorRoot.transform,
                    points[i],
                    points[i + 1],
                    width,
                    floorMaterial,
                    wallMaterial,
                    ceilingMaterial,
                    cableMaterial,
                    i == 0,
                    i == points.Length - 2);
            }

            for (var i = 0; i < points.Length; i++)
            {
                var isIntermediateLanding = i > 0 && i < points.Length - 1;
                CreateCorridorLanding(
                    name + " Landing " + (i + 1),
                    corridorRoot.transform,
                    points[i],
                    width,
                    floorMaterial,
                    isIntermediateLanding);
            }

            for (var i = 1; i < points.Length - 1; i++)
            {
                CreateCorridorJointSeal(
                    name + " Joint " + i,
                    corridorRoot.transform,
                    points[i],
                    points[i - 1],
                    points[i + 1],
                    width,
                    wallMaterial,
                    ceilingMaterial);
            }

            CreateThresholdFrame(name + " Start Threshold", corridorRoot.transform, points[0], points[1], width, doorFrameMaterial);
            CreateThresholdSeal(
                name + " Start Threshold Seal",
                corridorRoot.transform,
                points[0],
                points[1],
                width,
                floorMaterial,
                wallMaterial,
                ceilingMaterial);
            CreateThresholdFrame(
                name + " End Threshold",
                corridorRoot.transform,
                points[points.Length - 1],
                points[points.Length - 2],
                width,
                doorFrameMaterial);
            CreateThresholdSeal(
                name + " End Threshold Seal",
                corridorRoot.transform,
                points[points.Length - 1],
                points[points.Length - 2],
                width,
                floorMaterial,
                wallMaterial,
                ceilingMaterial);

            if (IsSlopedRoute(points))
            {
                CreateSlopedEndpointSeal(
                    name + " Start Sloped Endpoint Seal",
                    corridorRoot.transform,
                    fromRoom,
                    points[0],
                    points[1],
                    width,
                    wallMaterial,
                    ceilingMaterial);
                CreateSlopedEndpointSeal(
                    name + " End Sloped Endpoint Seal",
                    corridorRoot.transform,
                    toRoom,
                    points[points.Length - 1],
                    points[points.Length - 2],
                    width,
                    wallMaterial,
                    ceilingMaterial);
            }
        }

        private static void CreateCorridorSegment(
            string name,
            Transform parent,
            Vector3 from,
            Vector3 to,
            float width,
            Material floorMaterial,
            Material wallMaterial,
            Material ceilingMaterial,
            Material cableMaterial,
            bool isFirstSegment,
            bool isLastSegment)
        {
            var delta = to - from;
            var spatialLength = delta.magnitude;
            var planarDelta = new Vector3(delta.x, 0f, delta.z);
            var planarLength = planarDelta.magnitude;
            if (spatialLength < 0.01f || planarLength < 0.01f)
            {
                throw new InvalidOperationException("Corridor segment requires distinct route points: " + name);
            }

            var planarForward = planarDelta.normalized;
            var floorRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            var wallRotation = Quaternion.LookRotation(planarForward, Vector3.up);
            var floorCenter = (from + to) * 0.5f;
            var side = wallRotation * Vector3.right;
            var segmentRoot = new GameObject(name);
            segmentRoot.transform.SetParent(parent, false);

            var floorLength = Mathf.Max(0.2f, spatialLength + CorridorFloorOverlap);
            CreateBox(
                name + " Floor",
                segmentRoot.transform,
                new Vector3(floorCenter.x, floorCenter.y - 0.06f, floorCenter.z),
                new Vector3(width, 0.08f, floorLength),
                floorRotation,
                floorMaterial,
                true);

            var startOverlap = isFirstSegment ? CorridorWallEndOverlap : CorridorWallJointOverlap;
            var endOverlap = isLastSegment ? CorridorWallEndOverlap : CorridorWallJointOverlap;
            var wallLength = Mathf.Max(0.4f, planarLength + startOverlap + endOverlap);
            var wallCenter = ((from + to) * 0.5f) + (planarForward * ((endOverlap - startOverlap) * 0.5f));
            var lowerY = Mathf.Min(from.y, to.y);
            var upperY = Mathf.Max(from.y, to.y);
            var wallHeight = ProductionWallHeight + (upperY - lowerY);
            var wallCenterY = lowerY + (wallHeight * 0.5f);
            var wallTopY = lowerY + wallHeight;
            var leftPosition = new Vector3(wallCenter.x, wallCenterY, wallCenter.z) - (side * ((width * 0.5f) + (WallThickness * 0.5f)));
            var rightPosition = new Vector3(wallCenter.x, wallCenterY, wallCenter.z) + (side * ((width * 0.5f) + (WallThickness * 0.5f)));

            CreateBox(name + " Left Wall", segmentRoot.transform, leftPosition, new Vector3(WallThickness, wallHeight, wallLength), wallRotation, wallMaterial, true);
            CreateBox(name + " Right Wall", segmentRoot.transform, rightPosition, new Vector3(WallThickness, wallHeight, wallLength), wallRotation, wallMaterial, true);
            CreateBox(
                name + " Ceiling",
                segmentRoot.transform,
                new Vector3(floorCenter.x, wallTopY + (CeilingThickness * 0.5f), floorCenter.z),
                new Vector3(width + WallThickness + WallThickness, CeilingThickness, wallLength),
                wallRotation,
                ceilingMaterial,
                false);

            CreateBox(
                name + " Overhead Cable Tray",
                segmentRoot.transform,
                new Vector3(wallCenter.x, upperY + ProductionWallHeight - 0.18f, wallCenter.z) + (side * ((width * 0.5f) - 0.24f)),
                new Vector3(0.18f, 0.12f, wallLength),
                wallRotation,
                cableMaterial,
                false);

            if (isFirstSegment || isLastSegment)
            {
                CreateBox(
                    name + " Worn Threshold Plate",
                    segmentRoot.transform,
                    from + (Vector3.up * 0.01f),
                    new Vector3(width + 0.35f, 0.05f, 0.32f),
                    wallRotation,
                    floorMaterial,
                    false);
            }
        }

        private static void CreateCorridorLanding(
            string name,
            Transform parent,
            Vector3 point,
            float width,
            Material floorMaterial,
            bool keepCollider)
        {
            CreateBox(
                name,
                parent,
                new Vector3(point.x, point.y - 0.035f, point.z),
                new Vector3(width + CorridorLandingExtraWidth, 0.08f, width + CorridorLandingExtraWidth),
                Quaternion.identity,
                floorMaterial,
                keepCollider);
        }

        private static void CreateCorridorJointSeal(
            string name,
            Transform parent,
            Vector3 point,
            Vector3 previous,
            Vector3 next,
            float width,
            Material wallMaterial,
            Material ceilingMaterial)
        {
            var previousDirection = CardinalDirection(previous - point);
            var nextDirection = CardinalDirection(next - point);
            if (previousDirection == Vector3.zero || nextDirection == Vector3.zero)
            {
                return;
            }

            var lowerY = Mathf.Min(point.y, Mathf.Min(previous.y, next.y));
            var upperY = Mathf.Max(point.y, Mathf.Max(previous.y, next.y));
            var wallHeight = ProductionWallHeight + (upperY - lowerY);
            var wallCenterY = lowerY + (wallHeight * 0.5f);
            var wallTopY = lowerY + wallHeight;
            var halfWidth = width * 0.5f;
            var span = ProductionJointSealSpan + WallThickness;

            CreateBox(
                name + " Ceiling Cap",
                parent,
                new Vector3(point.x, wallTopY + (CeilingThickness * 0.5f), point.z),
                new Vector3(span, CeilingThickness, span),
                Quaternion.identity,
                ceilingMaterial,
                false);

            var directions = new[]
            {
                Vector3.forward,
                Vector3.back,
                Vector3.right,
                Vector3.left
            };

            for (var i = 0; i < directions.Length; i++)
            {
                var direction = directions[i];
                if (IsSameCardinalDirection(direction, previousDirection) ||
                    IsSameCardinalDirection(direction, nextDirection))
                {
                    continue;
                }

                CreateCorridorJointWall(
                    name + " " + CardinalName(direction) + " Closure Wall",
                    parent,
                    point,
                    direction,
                    halfWidth,
                    wallCenterY,
                    wallHeight,
                    span,
                    wallMaterial);
            }
        }

        private static void CreateCorridorJointWall(
            string name,
            Transform parent,
            Vector3 point,
            Vector3 direction,
            float halfWidth,
            float wallCenterY,
            float wallHeight,
            float span,
            Material wallMaterial)
        {
            var wallCenter = new Vector3(point.x, wallCenterY, point.z) + (direction * (halfWidth + (WallThickness * 0.5f)));
            var scale = Mathf.Abs(direction.z) > 0.5f
                ? new Vector3(span, wallHeight, WallThickness)
                : new Vector3(WallThickness, wallHeight, span);

            CreateBox(
                name,
                parent,
                wallCenter,
                scale,
                Quaternion.identity,
                wallMaterial,
                true);
        }

        private static Vector3 CardinalDirection(Vector3 delta)
        {
            var planar = new Vector3(delta.x, 0f, delta.z);
            if (planar.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            if (Mathf.Abs(planar.x) >= Mathf.Abs(planar.z))
            {
                return planar.x >= 0f ? Vector3.right : Vector3.left;
            }

            return planar.z >= 0f ? Vector3.forward : Vector3.back;
        }

        private static bool IsSameCardinalDirection(Vector3 first, Vector3 second)
        {
            return Vector3.Dot(first, second) > 0.95f;
        }

        private static string CardinalName(Vector3 direction)
        {
            if (direction == Vector3.forward)
            {
                return "North";
            }

            if (direction == Vector3.back)
            {
                return "South";
            }

            if (direction == Vector3.right)
            {
                return "East";
            }

            return "West";
        }

        private static void CreateThresholdSeal(
            string name,
            Transform parent,
            Vector3 from,
            Vector3 toward,
            float width,
            Material floorMaterial,
            Material wallMaterial,
            Material ceilingMaterial)
        {
            var delta = toward - from;
            var planarDelta = new Vector3(delta.x, 0f, delta.z);
            if (planarDelta.sqrMagnitude < 0.001f)
            {
                return;
            }

            var forward = planarDelta.normalized;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var right = rotation * Vector3.right;
            var sealCenter = from + (forward * (CorridorThresholdSealDepth * 0.5f));
            var sealWidth = ProductionThresholdMouthWidth;
            var lowerY = Mathf.Min(from.y, toward.y);
            var upperY = Mathf.Max(from.y, toward.y);
            var sealHeight = ProductionWallHeight + (upperY - lowerY);
            var sealCenterY = lowerY + (sealHeight * 0.5f);
            var sealTopY = lowerY + sealHeight;
            var centralClearWidth = width;
            var sideClosureWidth = (sealWidth - centralClearWidth) * 0.5f;
            var sideClosureOffset = (centralClearWidth * 0.5f) + (sideClosureWidth * 0.5f);
            var upperBulkheadBottomY = from.y + ProductionDoorHeight;
            var upperBulkheadHeight = Mathf.Max(0.1f, sealTopY - upperBulkheadBottomY);

            CreateBox(
                name + " Floor Lip",
                parent,
                from + (forward * (CorridorThresholdSealDepth * 0.5f)) + (Vector3.up * 0.005f),
                new Vector3(sealWidth, 0.04f, CorridorThresholdSealDepth),
                rotation,
                floorMaterial,
                false);

            CreateBox(
                name + " Left Reveal Wall",
                parent,
                new Vector3(sealCenter.x, sealCenterY, sealCenter.z) - (right * ((width * 0.5f) + (WallThickness * 0.5f))),
                new Vector3(WallThickness, sealHeight, CorridorThresholdSealDepth),
                rotation,
                wallMaterial,
                false);

            CreateBox(
                name + " Right Reveal Wall",
                parent,
                new Vector3(sealCenter.x, sealCenterY, sealCenter.z) + (right * ((width * 0.5f) + (WallThickness * 0.5f))),
                new Vector3(WallThickness, sealHeight, CorridorThresholdSealDepth),
                rotation,
                wallMaterial,
                false);

            CreateBox(
                name + " Left Mouth Closure Wall",
                parent,
                new Vector3(sealCenter.x, sealCenterY, sealCenter.z) - (right * sideClosureOffset),
                new Vector3(sideClosureWidth, sealHeight, CorridorThresholdSealDepth),
                rotation,
                wallMaterial,
                true);

            CreateBox(
                name + " Right Mouth Closure Wall",
                parent,
                new Vector3(sealCenter.x, sealCenterY, sealCenter.z) + (right * sideClosureOffset),
                new Vector3(sideClosureWidth, sealHeight, CorridorThresholdSealDepth),
                rotation,
                wallMaterial,
                true);

            CreateBox(
                name + " Upper Bulkhead Wall",
                parent,
                new Vector3(from.x, upperBulkheadBottomY + (upperBulkheadHeight * 0.5f), from.z) + (forward * (WallThickness * 0.5f)),
                new Vector3(sealWidth, upperBulkheadHeight, WallThickness),
                rotation,
                wallMaterial,
                true);

            CreateBox(
                name + " Ceiling Cap",
                parent,
                new Vector3(sealCenter.x, sealTopY + (CeilingThickness * 0.5f), sealCenter.z),
                new Vector3(sealWidth, CeilingThickness, CorridorThresholdSealDepth),
                rotation,
                ceilingMaterial,
                false);
        }

        private static void CreateSlopedEndpointSeal(
            string name,
            Transform parent,
            string roomName,
            Vector3 from,
            Vector3 toward,
            float width,
            Material wallMaterial,
            Material ceilingMaterial)
        {
            var delta = toward - from;
            var planarDelta = new Vector3(delta.x, 0f, delta.z);
            if (planarDelta.sqrMagnitude < 0.001f)
            {
                return;
            }

            var forward = planarDelta.normalized;
            var corridorRotation = Quaternion.LookRotation(forward, Vector3.up);
            var right = corridorRotation * Vector3.right;
            var lowerY = Mathf.Min(from.y, toward.y);
            var upperY = Mathf.Max(from.y, toward.y);
            var sealHeight = ProductionWallHeight + (upperY - lowerY);
            var sealCenterY = lowerY + (sealHeight * 0.5f);
            var sealTopY = lowerY + sealHeight;
            var sealWidth = ProductionThresholdMouthWidth;
            var centralClearWidth = width;
            var sideClosureWidth = (sealWidth - centralClearWidth) * 0.5f;
            var sideClosureOffset = (centralClearWidth * 0.5f) + (sideClosureWidth * 0.5f);
            var innerHalfWidth = centralClearWidth * 0.5f;
            var outerHalfWidth = sealWidth * 0.5f;
            var sleeveCenter = from + (forward * (ProductionSlopedEndpointSealDepth * 0.5f));

            CreateBox(
                name + " Sleeve Left Closure Wall",
                parent,
                new Vector3(sleeveCenter.x, sealCenterY, sleeveCenter.z) - (right * sideClosureOffset),
                new Vector3(sideClosureWidth, sealHeight, ProductionSlopedEndpointSealDepth),
                corridorRotation,
                wallMaterial,
                true);

            CreateBox(
                name + " Sleeve Right Closure Wall",
                parent,
                new Vector3(sleeveCenter.x, sealCenterY, sleeveCenter.z) + (right * sideClosureOffset),
                new Vector3(sideClosureWidth, sealHeight, ProductionSlopedEndpointSealDepth),
                corridorRotation,
                wallMaterial,
                true);

            CreateBox(
                name + " Sleeve Ceiling Cap",
                parent,
                new Vector3(sleeveCenter.x, sealTopY + (CeilingThickness * 0.5f), sleeveCenter.z),
                new Vector3(sealWidth, CeilingThickness, ProductionSlopedEndpointSealDepth),
                corridorRotation,
                ceilingMaterial,
                false);

            CreateRoomPlaneEndpointSeal(
                name,
                parent,
                roomName,
                from,
                sealWidth,
                sideClosureWidth,
                sideClosureOffset,
                sealCenterY,
                sealHeight,
                sealTopY,
                wallMaterial,
                ceilingMaterial);

            var wallSide = GetEndpointWallSide(roomName, from);
            var roomTangent = WallTangent(wallSide);
            CreateSlopedEndpointSideWedge(
                name + " Left Side Wedge Fill",
                parent,
                from,
                forward,
                right,
                roomTangent,
                -1f,
                innerHalfWidth,
                outerHalfWidth,
                lowerY,
                sealHeight,
                wallMaterial);
            CreateSlopedEndpointSideWedge(
                name + " Right Side Wedge Fill",
                parent,
                from,
                forward,
                right,
                roomTangent,
                1f,
                innerHalfWidth,
                outerHalfWidth,
                lowerY,
                sealHeight,
                wallMaterial);
        }

        private static void CreateRoomPlaneEndpointSeal(
            string name,
            Transform parent,
            string roomName,
            Vector3 from,
            float sealWidth,
            float sideClosureWidth,
            float sideClosureOffset,
            float sealCenterY,
            float sealHeight,
            float sealTopY,
            Material wallMaterial,
            Material ceilingMaterial)
        {
            var wallSide = GetEndpointWallSide(roomName, from);
            var tangent = WallTangent(wallSide);
            var sideWallScale = WallAlignedScale(wallSide, sideClosureWidth, sealHeight, WallThickness);
            var upperBulkheadBottomY = from.y + ProductionDoorHeight;
            var upperBulkheadHeight = Mathf.Max(0.1f, sealTopY - upperBulkheadBottomY);
            var upperBulkheadScale = WallAlignedScale(wallSide, sealWidth, upperBulkheadHeight, WallThickness);
            var ceilingScale = WallAlignedScale(wallSide, sealWidth, CeilingThickness, WallThickness + 0.4f);

            CreateBox(
                name + " Room Plane Left Closure Wall",
                parent,
                new Vector3(from.x, sealCenterY, from.z) - (tangent * sideClosureOffset),
                sideWallScale,
                Quaternion.identity,
                wallMaterial,
                true);

            CreateBox(
                name + " Room Plane Right Closure Wall",
                parent,
                new Vector3(from.x, sealCenterY, from.z) + (tangent * sideClosureOffset),
                sideWallScale,
                Quaternion.identity,
                wallMaterial,
                true);

            CreateBox(
                name + " Room Plane Upper Bulkhead Wall",
                parent,
                new Vector3(from.x, upperBulkheadBottomY + (upperBulkheadHeight * 0.5f), from.z),
                upperBulkheadScale,
                Quaternion.identity,
                wallMaterial,
                true);

            CreateBox(
                name + " Room Plane Ceiling Cap",
                parent,
                new Vector3(from.x, sealTopY + (CeilingThickness * 0.5f), from.z),
                ceilingScale,
                Quaternion.identity,
                ceilingMaterial,
                false);
        }

        private static bool IsSlopedRoute(Vector3[] points)
        {
            return points.Length > 1 && Mathf.Abs(points[points.Length - 1].y - points[0].y) > 0.5f;
        }

        private static void CreateSlopedEndpointSideWedge(
            string name,
            Transform parent,
            Vector3 from,
            Vector3 forward,
            Vector3 corridorRight,
            Vector3 roomTangent,
            float corridorSideSign,
            float innerHalfWidth,
            float outerHalfWidth,
            float bottomY,
            float height,
            Material material)
        {
            var tangentSideSign = Vector3.Dot(roomTangent, corridorRight) >= 0f
                ? corridorSideSign
                : -corridorSideSign;
            var sleeveEnd = from + (forward * ProductionSlopedEndpointSealDepth);
            var floorPolygon = new[]
            {
                new Vector3(from.x, bottomY, from.z) + (roomTangent * (tangentSideSign * innerHalfWidth)),
                new Vector3(from.x, bottomY, from.z) + (roomTangent * (tangentSideSign * outerHalfWidth)),
                new Vector3(sleeveEnd.x, bottomY, sleeveEnd.z) + (corridorRight * (corridorSideSign * outerHalfWidth)),
                new Vector3(sleeveEnd.x, bottomY, sleeveEnd.z) + (corridorRight * (corridorSideSign * innerHalfWidth))
            };

            CreateVerticalPrism(name, parent, floorPolygon, height, material, true);
        }

        private static GameObject CreateVerticalPrism(
            string name,
            Transform parent,
            Vector3[] floorPolygon,
            float height,
            Material material,
            bool keepCollider)
        {
            if (floorPolygon.Length < 3)
            {
                throw new InvalidOperationException("Vertical prism requires at least three floor points: " + name);
            }

            var prism = new GameObject(name);
            prism.transform.SetParent(parent, false);

            var vertexCount = floorPolygon.Length;
            var vertices = new Vector3[vertexCount * 2];
            for (var i = 0; i < vertexCount; i++)
            {
                vertices[i] = floorPolygon[i];
                vertices[i + vertexCount] = floorPolygon[i] + (Vector3.up * height);
            }

            var triangles = new System.Collections.Generic.List<int>();
            for (var i = 1; i < vertexCount - 1; i++)
            {
                AddDoubleSidedTriangle(triangles, 0, i, i + 1);
                AddDoubleSidedTriangle(triangles, vertexCount, vertexCount + i + 1, vertexCount + i);
            }

            for (var i = 0; i < vertexCount; i++)
            {
                var next = (i + 1) % vertexCount;
                AddDoubleSidedTriangle(triangles, i, next, vertexCount + next);
                AddDoubleSidedTriangle(triangles, i, vertexCount + next, vertexCount + i);
            }

            var mesh = new Mesh
            {
                name = name + " Mesh",
                vertices = vertices,
                triangles = triangles.ToArray()
            };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            prism.AddComponent<MeshFilter>().sharedMesh = mesh;
            prism.AddComponent<MeshRenderer>().sharedMaterial = material;

            if (keepCollider)
            {
                var collider = prism.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            return prism;
        }

        private static void AddDoubleSidedTriangle(System.Collections.Generic.ICollection<int> triangles, int a, int b, int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(a);
        }

        private static WallSide GetEndpointWallSide(string roomName, Vector3 worldPoint)
        {
            var room = FindRoom(roomName);
            var localPoint = worldPoint - room.Center;
            var halfX = room.Size.x * 0.5f;
            var halfZ = room.Size.y * 0.5f;
            var north = Mathf.Abs(localPoint.z - halfZ);
            var south = Mathf.Abs(localPoint.z + halfZ);
            var east = Mathf.Abs(localPoint.x - halfX);
            var west = Mathf.Abs(localPoint.x + halfX);

            if (north <= south && north <= east && north <= west)
            {
                return WallSide.North;
            }

            if (south <= east && south <= west)
            {
                return WallSide.South;
            }

            if (east <= west)
            {
                return WallSide.East;
            }

            return WallSide.West;
        }

        private static Vector3 WallTangent(WallSide side)
        {
            return side == WallSide.North || side == WallSide.South ? Vector3.right : Vector3.forward;
        }

        private static Vector3 WallAlignedScale(WallSide side, float tangentWidth, float height, float depth)
        {
            return side == WallSide.North || side == WallSide.South
                ? new Vector3(tangentWidth, height, depth)
                : new Vector3(depth, height, tangentWidth);
        }

        private static void CreateRoomWall(
            Transform roomRoot,
            RoomSpec room,
            WallSide side,
            DoorOpening[] openings,
            Material wallMaterial,
            Material doorFrameMaterial)
        {
            var halfLength = side == WallSide.North || side == WallSide.South
                ? room.Size.x * 0.5f
                : room.Size.y * 0.5f;
            var sideOpenings = openings
                .Where(opening => opening.Side == side)
                .OrderBy(opening => opening.Offset)
                .ToArray();

            var cursor = -halfLength;
            var segmentIndex = 1;
            for (var i = 0; i < sideOpenings.Length; i++)
            {
                var opening = sideOpenings[i];
                var openingStart = Mathf.Clamp(opening.Offset - (opening.Width * 0.5f), -halfLength, halfLength);
                var openingEnd = Mathf.Clamp(opening.Offset + (opening.Width * 0.5f), -halfLength, halfLength);

                if (openingStart - cursor > 0.1f)
                {
                    CreateWallSegment(roomRoot, room, side, cursor, openingStart, segmentIndex++, wallMaterial);
                }

                CreateDoorHeaderWall(roomRoot, room, side, opening, wallMaterial);
                CreateDoorFrame(roomRoot, room, side, opening, doorFrameMaterial);
                cursor = Mathf.Max(cursor, openingEnd);
            }

            if (halfLength - cursor > 0.1f)
            {
                CreateWallSegment(roomRoot, room, side, cursor, halfLength, segmentIndex, wallMaterial);
            }
        }

        private static void CreateWallSegment(
            Transform roomRoot,
            RoomSpec room,
            WallSide side,
            float start,
            float end,
            int index,
            Material wallMaterial)
        {
            var length = end - start;
            if (length <= 0.1f)
            {
                return;
            }

            var center = (start + end) * 0.5f;
            var halfX = room.Size.x * 0.5f;
            var halfZ = room.Size.y * 0.5f;
            var wallCenterY = ProductionWallHeight * 0.5f;
            var name = "Wall - " + room.Name + " - " + side + " " + index;

            switch (side)
            {
                case WallSide.North:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(center, wallCenterY, halfZ),
                        new Vector3(length, ProductionWallHeight, WallThickness),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                case WallSide.South:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(center, wallCenterY, -halfZ),
                        new Vector3(length, ProductionWallHeight, WallThickness),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                case WallSide.East:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(halfX, wallCenterY, center),
                        new Vector3(WallThickness, ProductionWallHeight, length),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                case WallSide.West:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(-halfX, wallCenterY, center),
                        new Vector3(WallThickness, ProductionWallHeight, length),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private static void CreateDoorHeaderWall(
            Transform roomRoot,
            RoomSpec room,
            WallSide side,
            DoorOpening opening,
            Material wallMaterial)
        {
            var headerHeight = ProductionWallHeight - ProductionDoorHeight;
            if (headerHeight <= 0.05f)
            {
                return;
            }

            var halfX = room.Size.x * 0.5f;
            var halfZ = room.Size.y * 0.5f;
            var centerY = ProductionDoorHeight + (headerHeight * 0.5f);
            var name = "Door Header Wall - " + room.Name + " - " + side + " " + opening.Label;

            switch (side)
            {
                case WallSide.North:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(opening.Offset, centerY, halfZ),
                        new Vector3(opening.Width, headerHeight, WallThickness),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                case WallSide.South:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(opening.Offset, centerY, -halfZ),
                        new Vector3(opening.Width, headerHeight, WallThickness),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                case WallSide.East:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(halfX, centerY, opening.Offset),
                        new Vector3(WallThickness, headerHeight, opening.Width),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                case WallSide.West:
                    CreateBox(
                        name,
                        roomRoot,
                        new Vector3(-halfX, centerY, opening.Offset),
                        new Vector3(WallThickness, headerHeight, opening.Width),
                        Quaternion.identity,
                        wallMaterial,
                        true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private static void CreateDoorFrame(
            Transform roomRoot,
            RoomSpec room,
            WallSide side,
            DoorOpening opening,
            Material material)
        {
            const float postThickness = 0.22f;
            const float frameDepth = 0.42f;
            const float lintelHeight = 0.24f;

            var halfX = room.Size.x * 0.5f;
            var halfZ = room.Size.y * 0.5f;
            var halfWidth = opening.Width * 0.5f;
            var postCenterY = ProductionDoorHeight * 0.5f;
            var lintelCenterY = ProductionDoorHeight + (lintelHeight * 0.5f);
            var name = "Door Frame - " + room.Name + " - " + side + " " + opening.Label;

            switch (side)
            {
                case WallSide.North:
                case WallSide.South:
                {
                    var z = side == WallSide.North ? halfZ : -halfZ;
                    CreateBox(name + " Left Post", roomRoot, new Vector3(opening.Offset - halfWidth, postCenterY, z), new Vector3(postThickness, ProductionDoorHeight, frameDepth), Quaternion.identity, material, false);
                    CreateBox(name + " Right Post", roomRoot, new Vector3(opening.Offset + halfWidth, postCenterY, z), new Vector3(postThickness, ProductionDoorHeight, frameDepth), Quaternion.identity, material, false);
                    CreateBox(name + " Lintel", roomRoot, new Vector3(opening.Offset, lintelCenterY, z), new Vector3(opening.Width + postThickness + postThickness, lintelHeight, frameDepth), Quaternion.identity, material, false);
                    break;
                }

                case WallSide.East:
                case WallSide.West:
                {
                    var x = side == WallSide.East ? halfX : -halfX;
                    CreateBox(name + " Left Post", roomRoot, new Vector3(x, postCenterY, opening.Offset - halfWidth), new Vector3(frameDepth, ProductionDoorHeight, postThickness), Quaternion.identity, material, false);
                    CreateBox(name + " Right Post", roomRoot, new Vector3(x, postCenterY, opening.Offset + halfWidth), new Vector3(frameDepth, ProductionDoorHeight, postThickness), Quaternion.identity, material, false);
                    CreateBox(name + " Lintel", roomRoot, new Vector3(x, lintelCenterY, opening.Offset), new Vector3(frameDepth, lintelHeight, opening.Width + postThickness + postThickness), Quaternion.identity, material, false);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private static void CreateRoomCableTrays(Transform roomRoot, RoomSpec room, Material material)
        {
            var halfX = room.Size.x * 0.5f;
            var halfZ = room.Size.y * 0.5f;
            CreateBox(
                "Cable Tray - " + room.Name + " North",
                roomRoot,
                new Vector3(0f, ProductionWallHeight - 0.24f, halfZ - 0.24f),
                new Vector3(room.Size.x - 0.8f, 0.14f, 0.12f),
                Quaternion.identity,
                material,
                false);
            CreateBox(
                "Cable Tray - " + room.Name + " West",
                roomRoot,
                new Vector3(-halfX + 0.24f, ProductionWallHeight - 0.42f, 0f),
                new Vector3(0.12f, 0.14f, room.Size.y - 0.9f),
                Quaternion.identity,
                material,
                false);
        }

        private static void CreateRoomWearPatches(Transform roomRoot, RoomSpec room, Material material)
        {
            var halfX = room.Size.x * 0.5f;
            var halfZ = room.Size.y * 0.5f;
            CreateBox(
                "Wear Patch - " + room.Name + " Floor Plate",
                roomRoot,
                new Vector3(-room.Size.x * 0.22f, 0.01f, room.Size.y * 0.12f),
                new Vector3(room.Size.x * 0.24f, 0.025f, room.Size.y * 0.08f),
                Quaternion.Euler(0f, 7f, 0f),
                material,
                false);
            CreateBox(
                "Wear Patch - " + room.Name + " Wall Repair",
                roomRoot,
                new Vector3(halfX - 0.015f, 1.1f, -halfZ * 0.35f),
                new Vector3(0.03f, 0.65f, room.Size.y * 0.18f),
                Quaternion.identity,
                material,
                false);
        }

        private static void CreateThresholdFrame(
            string name,
            Transform parent,
            Vector3 from,
            Vector3 toward,
            float width,
            Material material)
        {
            var delta = toward - from;
            var planarDelta = new Vector3(delta.x, 0f, delta.z);
            if (planarDelta.sqrMagnitude < 0.001f)
            {
                return;
            }

            var rotation = Quaternion.LookRotation(planarDelta.normalized, Vector3.up);
            var right = rotation * Vector3.right;
            const float postThickness = 0.16f;
            const float frameDepth = 0.24f;
            var postOffset = (width * 0.5f) + (postThickness * 0.5f);

            CreateBox(
                name + " Left Post",
                parent,
                from - (right * postOffset) + (Vector3.up * (ProductionDoorHeight * 0.5f)),
                new Vector3(postThickness, ProductionDoorHeight, frameDepth),
                rotation,
                material,
                false);
            CreateBox(
                name + " Right Post",
                parent,
                from + (right * postOffset) + (Vector3.up * (ProductionDoorHeight * 0.5f)),
                new Vector3(postThickness, ProductionDoorHeight, frameDepth),
                rotation,
                material,
                false);
            CreateBox(
                name + " Lintel",
                parent,
                from + (Vector3.up * (ProductionDoorHeight + 0.1f)),
                new Vector3(width + postThickness + postThickness, 0.2f, frameDepth),
                rotation,
                material,
                false);
        }

        private static DoorOpening[] GetDoorOpenings(string roomName)
        {
            switch (roomName)
            {
                case "Cargo Hold":
                    return new[]
                    {
                        new DoorOpening(WallSide.North, -4.25f, 2.6f, "Engine"),
                        new DoorOpening(WallSide.North, 0f, 3.6f, "Cockpit"),
                        new DoorOpening(WallSide.North, 4.25f, 3.4f, "Control"),
                        new DoorOpening(WallSide.South, -4.2f, 4.2f, "Armory"),
                        new DoorOpening(WallSide.South, 4.2f, 4.2f, "Supply")
                    };
                case "Cockpit":
                    return new[]
                    {
                        new DoorOpening(WallSide.South, 0f, ProductionDoorWidth, "Cargo"),
                        new DoorOpening(WallSide.West, 0f, ProductionDoorWidth, "Engine"),
                        new DoorOpening(WallSide.East, 0f, ProductionDoorWidth, "Control")
                    };
                case "Engine Room":
                    return new[]
                    {
                        new DoorOpening(WallSide.South, 2.8f, ProductionDoorWidth, "Cargo"),
                        new DoorOpening(WallSide.East, 0f, 2.5f, "Cockpit"),
                        new DoorOpening(WallSide.North, 2.0f, ProductionDoorWidth, "Control")
                    };
                case "Control Room":
                    return new[]
                    {
                        new DoorOpening(WallSide.South, -2.8f, 3.4f, "Cargo"),
                        new DoorOpening(WallSide.West, 0f, 2.5f, "Cockpit"),
                        new DoorOpening(WallSide.North, -2.0f, ProductionDoorWidth, "Engine")
                    };
                case "Armory":
                    return new[]
                    {
                        new DoorOpening(WallSide.North, 2.8f, ProductionDoorWidth, "Cargo"),
                        new DoorOpening(WallSide.East, 0f, ProductionDoorWidth, "Supply")
                    };
                case "Supply Room":
                    return new[]
                    {
                        new DoorOpening(WallSide.North, -2.8f, ProductionDoorWidth, "Cargo"),
                        new DoorOpening(WallSide.West, 0f, ProductionDoorWidth, "Armory")
                    };
                default:
                    return Array.Empty<DoorOpening>();
            }
        }

        private static void CreateRoomFeatures(
            Transform root,
            Material wallMaterial,
            Material glassMaterial,
            Material consoleMaterial,
            Material cargoMaterial,
            Material interactableMaterial,
            Material damageMaterial)
        {
            var featureRoot = new GameObject("Room Feature Placeholders");
            featureRoot.transform.SetParent(root, false);

            CreateCargoHoldFeatures(featureRoot.transform, cargoMaterial, interactableMaterial, damageMaterial);
            CreateCockpitFeatures(featureRoot.transform, wallMaterial, glassMaterial, consoleMaterial, interactableMaterial, damageMaterial);
            CreateEngineRoomFeatures(featureRoot.transform, consoleMaterial, interactableMaterial);
            CreateControlRoomFeatures(featureRoot.transform, consoleMaterial, interactableMaterial);
            CreateArmoryFeatures(featureRoot.transform, consoleMaterial, interactableMaterial, damageMaterial);
            CreateSupplyRoomFeatures(featureRoot.transform, consoleMaterial, interactableMaterial);
        }

        private static void CreateCargoHoldFeatures(
            Transform parent,
            Material cargoMaterial,
            Material interactableMaterial,
            Material damageMaterial)
        {
            CreateBox("Cargo Hold Central Cargo", parent, RoomPoint("Cargo Hold", 0f, 0.7f, 0f), new Vector3(2.4f, 1.4f, 3f), Quaternion.identity, cargoMaterial, true);
            CreateBox("Cargo Hold Securing Frame Left", parent, RoomPoint("Cargo Hold", -1.45f, 0.92f, 0f), new Vector3(0.12f, 1.75f, 3.35f), Quaternion.identity, damageMaterial, false);
            CreateBox("Cargo Hold Securing Frame Right", parent, RoomPoint("Cargo Hold", 1.45f, 0.92f, 0f), new Vector3(0.12f, 1.75f, 3.35f), Quaternion.identity, damageMaterial, false);
            CreateBox("Cargo Hold Tie Down Strap A", parent, RoomPoint("Cargo Hold", 0f, 1.48f, -0.8f), new Vector3(2.9f, 0.12f, 0.16f), Quaternion.identity, damageMaterial, false);
            CreateBox("Cargo Hold Tie Down Strap B", parent, RoomPoint("Cargo Hold", 0f, 1.48f, 0.8f), new Vector3(2.9f, 0.12f, 0.16f), Quaternion.identity, damageMaterial, false);
            CreateInteractableBox(
                "Interactable - Cargo Hold Cargo Status",
                "Cargo Hold Cargo Status",
                "Inspect",
                parent,
                RoomPoint("Cargo Hold", 0f, 1.45f, -2.6f),
                new Vector3(1.8f, 1.2f, 0.35f),
                Quaternion.identity,
                interactableMaterial);
        }

        private static void CreateCockpitFeatures(
            Transform parent,
            Material wallMaterial,
            Material glassMaterial,
            Material consoleMaterial,
            Material interactableMaterial,
            Material damageMaterial)
        {
            CreateBox("Cockpit Front Glass", parent, RoomPoint("Cockpit", 0f, 1.4f, 4.1f), new Vector3(8.8f, 2.4f, 0.18f), Quaternion.identity, glassMaterial, false);
            CreateBox("Cockpit Forward Frame Top", parent, RoomPoint("Cockpit", 0f, 2.58f, 4.02f), new Vector3(9.2f, 0.18f, 0.34f), Quaternion.identity, wallMaterial, false);
            CreateBox("Cockpit Forward Frame Left", parent, RoomPoint("Cockpit", -4.5f, 1.38f, 4.02f), new Vector3(0.2f, 2.35f, 0.34f), Quaternion.identity, wallMaterial, false);
            CreateBox("Cockpit Forward Frame Right", parent, RoomPoint("Cockpit", 4.5f, 1.38f, 4.02f), new Vector3(0.2f, 2.35f, 0.34f), Quaternion.identity, wallMaterial, false);
            CreateBox("Cockpit Rear Service Ramp Cover", parent, RoomPoint("Cockpit", 0f, 0.15f, -3.55f), new Vector3(3.5f, 0.3f, 1.4f), Quaternion.identity, wallMaterial, false);
            CreateInteractableBox(
                "Interactable - Cockpit Helm",
                "Cockpit Helm",
                "Use",
                parent,
                RoomPoint("Cockpit", 0f, 0.8f, 1.6f),
                new Vector3(2.8f, 1f, 1f),
                Quaternion.identity,
                interactableMaterial);
            CreateBox("Cockpit Console Base", parent, RoomPoint("Cockpit", 0f, 0.45f, 2.2f), new Vector3(3.2f, 0.9f, 1f), Quaternion.identity, consoleMaterial, true);
            CreateBox("Cockpit Worn Button Strip", parent, RoomPoint("Cockpit", 0f, 1.02f, 1.66f), new Vector3(2.4f, 0.08f, 0.12f), Quaternion.identity, damageMaterial, false);
        }

        private static void CreateEngineRoomFeatures(Transform parent, Material consoleMaterial, Material interactableMaterial)
        {
            var engine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            engine.name = "Engine Room Central Power Cylinder";
            engine.transform.SetParent(parent, false);
            engine.transform.localPosition = RoomPoint("Engine Room", 0f, 1.1f, 0f);
            engine.transform.localScale = new Vector3(1.6f, 1.1f, 1.6f);
            engine.GetComponent<MeshRenderer>().sharedMaterial = consoleMaterial;

            CreateInteractableBox(
                "Interactable - Engine Room Power Screen",
                "Engine Room Power Screen",
                "Overclock",
                parent,
                RoomPoint("Engine Room", 2.1f, 1f, 0f),
                new Vector3(0.25f, 1.2f, 2.4f),
                Quaternion.identity,
                interactableMaterial);
        }

        private static void CreateControlRoomFeatures(Transform parent, Material consoleMaterial, Material interactableMaterial)
        {
            CreateInteractableBox(
                "Interactable - Control Room Main Screen",
                "Control Room Main Screen",
                "Inspect",
                parent,
                RoomPoint("Control Room", 0f, 1.3f, 3.4f),
                new Vector3(4.8f, 1.8f, 0.25f),
                Quaternion.identity,
                interactableMaterial);
            CreateBox("Control Room Horizontal Screen Placeholder", parent, RoomPoint("Control Room", -1.9f, 1.8f, 3.1f), new Vector3(1.8f, 0.6f, 0.2f), Quaternion.identity, consoleMaterial, false);
            CreateBox("Control Room Vertical Screen Placeholder", parent, RoomPoint("Control Room", 2.3f, 1.3f, 2.9f), new Vector3(0.8f, 1.6f, 0.2f), Quaternion.identity, consoleMaterial, false);
            CreateBox("Control Room Screen Partition", parent, RoomPoint("Control Room", 0f, 1.1f, -1f), new Vector3(7.6f, 2.2f, 0.22f), Quaternion.identity, consoleMaterial, false);
        }

        private static void CreateArmoryFeatures(
            Transform parent,
            Material consoleMaterial,
            Material interactableMaterial,
            Material damageMaterial)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Armory Central Pillar";
            pillar.transform.SetParent(parent, false);
            pillar.transform.localPosition = RoomPoint("Armory", 0f, 1f, 0f);
            pillar.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = consoleMaterial;

            CreateInteractableBox(
                "Interactable - Armory Turret Handle",
                "Armory Turret Handle",
                "Use",
                parent,
                RoomPoint("Armory", 0f, 2.2f, 2f),
                new Vector3(1.4f, 0.35f, 0.35f),
                Quaternion.identity,
                interactableMaterial);
            CreateBox("Armory Turret Station Support Frame", parent, RoomPoint("Armory", 0f, 1.65f, 2f), new Vector3(2.4f, 1.1f, 0.18f), Quaternion.identity, damageMaterial, false);
            CreateBox("Armory Turret Warning Rail", parent, RoomPoint("Armory", 0f, 0.72f, 1.15f), new Vector3(3.2f, 0.14f, 0.16f), Quaternion.identity, damageMaterial, false);
            CreateBox("Armory Forward Screen Placeholder", parent, RoomPoint("Armory", 0f, 1.5f, 3.6f), new Vector3(5.2f, 1.6f, 0.25f), Quaternion.identity, consoleMaterial, false);
        }

        private static void CreateSupplyRoomFeatures(Transform parent, Material consoleMaterial, Material interactableMaterial)
        {
            CreateInteractableBox(
                "Interactable - Supply Room Storage Cabinet",
                "Supply Room Storage Cabinet",
                "Inspect",
                parent,
                RoomPoint("Supply Room", 2.6f, 1.1f, 0f),
                new Vector3(0.5f, 2f, 3.5f),
                Quaternion.identity,
                interactableMaterial);
            CreateBox("Supply Room Ejection Pad Placeholder", parent, RoomPoint("Supply Room", -1.9f, 0.12f, 0f), new Vector3(2.2f, 0.24f, 3f), Quaternion.identity, consoleMaterial, true);
            CreateBox("Supply Room Ejection Terminal Placeholder", parent, RoomPoint("Supply Room", -1.9f, 1f, 1.8f), new Vector3(0.7f, 1f, 0.35f), Quaternion.identity, consoleMaterial, true);
        }

        private static void CreateDirectionSigns(Transform root)
        {
            var signRoot = new GameObject("Direction Signs");
            signRoot.transform.SetParent(root, false);

            CreateLabel("Sign - To Cockpit", "-> Cockpit", signRoot.transform, new Vector3(0f, 1.2f, 5.2f), 180f);
            CreateLabel("Sign - To Engine Room", "-> Engine Room", signRoot.transform, new Vector3(-5.2f, 1.2f, 4.2f), 140f);
            CreateLabel("Sign - To Control Room", "-> Control Room", signRoot.transform, new Vector3(5.2f, 1.2f, 4.2f), -140f);
            CreateLabel("Sign - To Armory", "-> Armory", signRoot.transform, new Vector3(-5.2f, 1.2f, -4.2f), 40f);
            CreateLabel("Sign - To Supply Room", "-> Supply Room", signRoot.transform, new Vector3(5.2f, 1.2f, -4.2f), -40f);
        }

        private static void ConfigurePlayerStart()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>()?.gameObject;
            if (player == null)
            {
                throw new InvalidOperationException("Phase 4 graybox requires the Phase 2 Player prefab in CargoRunMvp.");
            }

            player.transform.SetPositionAndRotation(RoomPoint("Cargo Hold", 0f, 0f, -5f), Quaternion.identity);
        }

        private static void ConfigureLighting()
        {
            var existingLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in existingLights)
            {
                if (light.name == "Cargo Bay Directional Light")
                {
                    light.intensity = 0.55f;
                    light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                }
            }

            RenderSettings.ambientLight = new Color(0.05f, 0.055f, 0.06f);
        }

        private static GameObject CreateInteractableBox(
            string name,
            string displayName,
            string prompt,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material)
        {
            var box = CreateBox(name, parent, position, scale, rotation, material, true);
            box.AddComponent<DebugInteractable>().Configure(displayName, prompt, true);
            return box;
        }

        private static GameObject CreateBox(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Quaternion rotation,
            Material material,
            bool keepCollider)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localRotation = rotation;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (!keepCollider)
            {
                var collider = box.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            return box;
        }

        private static void CreateLabel(string name, string text, Transform parent, Vector3 position, float yaw)
        {
            var label = new GameObject(name);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.18f;
            textMesh.fontSize = 64;
            textMesh.color = new Color(0.82f, 0.92f, 0.88f, 1f);
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

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var target = roots.FirstOrDefault(root => root.name == objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static RoomSpec FindRoom(string name)
        {
            foreach (var room in Rooms)
            {
                if (room.Name == name)
                {
                    return room;
                }
            }

            throw new InvalidOperationException("Unknown graybox room: " + name);
        }

        public static bool HasRoom(string roomName)
        {
            return GameObject.Find("Room - " + roomName) != null;
        }

        public static bool HasCorridor(string from, string to)
        {
            return GameObject.Find("Corridor - " + from + " to " + to) != null;
        }

        public static float RoomDeckY(string roomName)
        {
            return FindRoom(roomName).Center.y;
        }

        public static Vector3 CorridorEndpoint(string roomName, string otherRoomName)
        {
            return GetCorridorEndpoint(roomName, otherRoomName);
        }

        public static Vector3[] ArmoryCargoCorridorRoute()
        {
            return GetCorridorRoute("Cargo Hold", "Armory");
        }

        public static Vector3[] CorridorRoute(string from, string to)
        {
            return GetCorridorRoute(from, to);
        }

        public static int CorridorSegmentCount(string from, string to)
        {
            return GetCorridorRoute(from, to).Length - 1;
        }

        public static bool HasProductionRoomShell(string roomName)
        {
            return GameObject.Find("Floor - " + roomName) != null &&
                   GameObject.Find("Ceiling - " + roomName) != null &&
                   GameObject.Find("Wall - " + roomName + " - North 1") != null &&
                   GameObject.Find("Wall - " + roomName + " - South 1") != null;
        }

        public static string[] ProductionMaterialPaths()
        {
            return new[]
            {
                FloorMaterialPath,
                CorridorMaterialPath,
                WallMaterialPath,
                CeilingMaterialPath,
                DoorFrameMaterialPath,
                CableMaterialPath,
                DamageMaterialPath,
                GlassMaterialPath,
                ConsoleMaterialPath,
                CargoMaterialPath,
                InteractableMaterialPath
            };
        }

        private static Vector3 GetCorridorEndpoint(string roomName, string otherRoomName)
        {
            var room = FindRoom(roomName);
            var halfX = room.Size.x * 0.5f;
            var halfZ = room.Size.y * 0.5f;

            if (roomName == "Cargo Hold")
            {
                switch (otherRoomName)
                {
                    case "Cockpit":
                        return RoomPoint(roomName, 0f, 0f, halfZ);
                    case "Engine Room":
                        return RoomPoint(roomName, -4.25f, 0f, halfZ - 0.15f);
                    case "Control Room":
                        return RoomPoint(roomName, 4.25f, 0f, halfZ - 0.15f);
                    case "Armory":
                        return RoomPoint(roomName, -4.2f, 0f, -halfZ + 0.15f);
                    case "Supply Room":
                        return RoomPoint(roomName, 4.2f, 0f, -halfZ + 0.15f);
                }
            }

            if (roomName == "Cockpit")
            {
                switch (otherRoomName)
                {
                    case "Cargo Hold":
                        return RoomPoint(roomName, 0f, 0f, -halfZ);
                    case "Engine Room":
                        return RoomPoint(roomName, -halfX, 0f, 0f);
                    case "Control Room":
                        return RoomPoint(roomName, halfX, 0f, 0f);
                }
            }

            if (roomName == "Engine Room")
            {
                switch (otherRoomName)
                {
                    case "Cargo Hold":
                        return RoomPoint(roomName, 2.8f, 0f, -halfZ);
                    case "Cockpit":
                        return RoomPoint(roomName, halfX, 0f, 0f);
                    case "Control Room":
                        return RoomPoint(roomName, 2.0f, 0f, halfZ);
                }
            }

            if (roomName == "Control Room")
            {
                switch (otherRoomName)
                {
                    case "Cargo Hold":
                        return RoomPoint(roomName, -2.8f, 0f, -halfZ);
                    case "Cockpit":
                        return RoomPoint(roomName, -halfX, 0f, 0f);
                    case "Engine Room":
                        return RoomPoint(roomName, -2.0f, 0f, halfZ);
                }
            }

            if (roomName == "Armory")
            {
                switch (otherRoomName)
                {
                    case "Cargo Hold":
                        return RoomPoint(roomName, 2.8f, 0f, halfZ);
                    case "Supply Room":
                        return RoomPoint(roomName, halfX, 0f, 0f);
                }
            }

            if (roomName == "Supply Room")
            {
                switch (otherRoomName)
                {
                    case "Cargo Hold":
                        return RoomPoint(roomName, -2.8f, 0f, halfZ);
                    case "Armory":
                        return RoomPoint(roomName, -halfX, 0f, 0f);
                }
            }

            return room.Center;
        }

        private static Vector3[] GetCorridorRoute(string from, string to)
        {
            if (Connects(from, to, "Cargo Hold", "Engine Room"))
            {
                return OrientRoute(from, to, "Cargo Hold", "Engine Room", new[]
                {
                    GetCorridorEndpoint("Cargo Hold", "Engine Room"),
                    GetCorridorEndpoint("Engine Room", "Cargo Hold")
                });
            }

            if (Connects(from, to, "Cargo Hold", "Control Room"))
            {
                return OrientRoute(from, to, "Cargo Hold", "Control Room", new[]
                {
                    GetCorridorEndpoint("Cargo Hold", "Control Room"),
                    GetCorridorEndpoint("Control Room", "Cargo Hold")
                });
            }

            if (Connects(from, to, "Cargo Hold", "Armory"))
            {
                return OrientRoute(from, to, "Cargo Hold", "Armory", new[]
                {
                    GetCorridorEndpoint("Cargo Hold", "Armory"),
                    GetCorridorEndpoint("Armory", "Cargo Hold")
                });
            }

            if (Connects(from, to, "Cargo Hold", "Supply Room"))
            {
                return OrientRoute(from, to, "Cargo Hold", "Supply Room", new[]
                {
                    GetCorridorEndpoint("Cargo Hold", "Supply Room"),
                    GetCorridorEndpoint("Supply Room", "Cargo Hold")
                });
            }

            if (Connects(from, to, "Engine Room", "Control Room"))
            {
                var engineEndpoint = GetCorridorEndpoint("Engine Room", "Control Room");
                var controlEndpoint = GetCorridorEndpoint("Control Room", "Engine Room");
                return OrientRoute(from, to, "Engine Room", "Control Room", new[]
                {
                    engineEndpoint,
                    new Vector3(engineEndpoint.x, UpperDeckY, 24.2f),
                    new Vector3(controlEndpoint.x, UpperDeckY, 24.2f),
                    controlEndpoint
                });
            }

            return new[]
            {
                GetCorridorEndpoint(from, to),
                GetCorridorEndpoint(to, from)
            };
        }

        private static Vector3 RoomPoint(string roomName, float localX, float localY, float localZ)
        {
            var room = FindRoom(roomName);
            return room.Center + new Vector3(localX, localY, localZ);
        }

        private static bool Connects(string from, string to, string first, string second)
        {
            return (from == first && to == second) || (from == second && to == first);
        }

        private static Vector3[] OrientRoute(string from, string to, string routeFrom, string routeTo, Vector3[] route)
        {
            if (from == routeFrom && to == routeTo)
            {
                return route;
            }

            var reversed = new Vector3[route.Length];
            for (var i = 0; i < route.Length; i++)
            {
                reversed[i] = route[route.Length - i - 1];
            }

            return reversed;
        }

        private enum WallSide
        {
            North,
            South,
            East,
            West
        }

        private readonly struct DoorOpening
        {
            public DoorOpening(WallSide side, float offset, float width, string label)
            {
                Side = side;
                Offset = offset;
                Width = width;
                Label = label;
            }

            public WallSide Side { get; }

            public float Offset { get; }

            public float Width { get; }

            public string Label { get; }
        }

        private readonly struct RoomSpec
        {
            public RoomSpec(string name, Vector3 center, Vector2 size)
            {
                Name = name;
                Center = center;
                Size = size;
            }

            public string Name { get; }

            public Vector3 Center { get; }

            public Vector2 Size { get; }

        }

        private readonly struct CorridorSpec
        {
            public CorridorSpec(string from, string to)
            {
                From = from;
                To = to;
            }

            public string From { get; }

            public string To { get; }

            public bool Connects(string from, string to)
            {
                return (From == from && To == to) || (From == to && To == from);
            }
        }

    }
}
