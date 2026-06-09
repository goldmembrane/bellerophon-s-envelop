using System;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase4CargoShipGrayboxEditorValidation
    {
        private static readonly string[] RequiredRooms =
        {
            "Cargo Hold",
            "Cockpit",
            "Engine Room",
            "Control Room",
            "Armory",
            "Supply Room"
        };

        private static readonly (string From, string To)[] RequiredCorridors =
        {
            ("Cargo Hold", "Cockpit"),
            ("Cargo Hold", "Engine Room"),
            ("Cargo Hold", "Control Room"),
            ("Cargo Hold", "Armory"),
            ("Cargo Hold", "Supply Room"),
            ("Supply Room", "Armory"),
            ("Cockpit", "Engine Room"),
            ("Cockpit", "Control Room"),
            ("Engine Room", "Control Room")
        };

        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene.");
            }

            if (SceneManager.GetActiveScene().path != Phase4CargoShipGrayboxBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (root == null)
            {
                throw new InvalidOperationException("Missing Phase 4 cargo ship graybox root.");
            }

            foreach (var room in RequiredRooms)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasRoom(room))
                {
                    throw new InvalidOperationException("Missing ship room: " + room);
                }

                if (!Phase4CargoShipGrayboxBootstrap.HasProductionRoomShell(room))
                {
                    throw new InvalidOperationException("Missing production wall/ceiling shell for room: " + room);
                }
            }

            foreach (var corridor in RequiredCorridors)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasCorridor(corridor.From, corridor.To))
                {
                    throw new InvalidOperationException($"Missing graybox corridor: {corridor.From} to {corridor.To}");
                }
            }

            if (Phase4CargoShipGrayboxBootstrap.RoomDeckY("Cargo Hold") >= Phase4CargoShipGrayboxBootstrap.RoomDeckY("Cockpit") - 2.0f)
            {
                throw new InvalidOperationException("Cargo Hold must be below the other ship rooms for the sloped corridor layout.");
            }

            var cargoCockpitStart = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint("Cargo Hold", "Cockpit");
            var cargoCockpitEnd = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint("Cockpit", "Cargo Hold");
            if (Mathf.Abs(cargoCockpitEnd.y - cargoCockpitStart.y) < 2f)
            {
                throw new InvalidOperationException("Cargo Hold to Cockpit corridor must be sloped.");
            }

            RequireSeparatedCargoEntrance("Control Room", "Cargo Hold");
            RequireSeparatedCargoEntrance("Armory", "Cargo Hold");
            RequireAngledArmoryCargoCorridor();
            RequireNoUndefinedControlSupplyCorridor();
            RequireCorridorGeometryClearance(root.transform);
            RequireCorridorJointSeals(root.transform);
            RequireSlopedCorridorEndpointSeals();
            RequireCorridorCeilingsSitOnWalls(root.transform);
            RequireProductionMaterials();

            var interactables = UnityEngine.Object.FindObjectsByType<DebugInteractable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            if (interactables.Length < 6)
            {
                throw new InvalidOperationException($"Phase 4 graybox must have at least 6 interaction points. Found: {interactables.Length}");
            }

            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if (player == null)
            {
                throw new InvalidOperationException("CargoRunMvp must contain the first-person player.");
            }

            var camera = Camera.main;
            if (camera == null || !camera.isActiveAndEnabled)
            {
                throw new InvalidOperationException("CargoRunMvp must contain an active MainCamera.");
            }

            var visibleRenderers = CountVisibleRenderers(camera);
            if (visibleRenderers < 5)
            {
                throw new InvalidOperationException($"Phase 4 graybox must be visible from player start. VisibleRenderers={visibleRenderers}");
            }

            Debug.Log($"Phase 4 cargo ship graybox editor validation passed. Rooms={RequiredRooms.Length}, Corridors={RequiredCorridors.Length}, Interactables={interactables.Length}, VisibleRenderers={visibleRenderers}, CorridorWidth={Phase4CargoShipGrayboxBootstrap.ProductionCorridorWidth:0.0}");
        }

        private static int CountVisibleRenderers(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var visibleRendererCount = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                {
                    visibleRendererCount++;
                }
            }

            return visibleRendererCount;
        }

        private static void RequireSeparatedCargoEntrance(string roomName, string cargoRoomName)
        {
            var roomCenter = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint(roomName, roomName);
            var cargoEntrance = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint(roomName, cargoRoomName);
            var distance = Vector3.Distance(
                new Vector3(roomCenter.x, 0f, roomCenter.z),
                new Vector3(cargoEntrance.x, 0f, cargoEntrance.z));

            if (distance < 2.5f)
            {
                throw new InvalidOperationException(
                    $"{roomName} cargo corridor entrance must be separated from the room center to avoid overlapping other corridor starts. Distance={distance:0.00}");
            }
        }

        private static void RequireAngledArmoryCargoCorridor()
        {
            var segmentCount = Phase4CargoShipGrayboxBootstrap.CorridorSegmentCount("Cargo Hold", "Armory");
            if (segmentCount != 1)
            {
                throw new InvalidOperationException($"Cargo Hold to Armory corridor must use a direct smooth ramp without a blocking mid-corner. SegmentCount={segmentCount}");
            }

            var armoryCenter = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint("Armory", "Armory");
            var armoryCargoEntrance = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint("Armory", "Cargo Hold");
            if (armoryCargoEntrance.x <= armoryCenter.x || armoryCargoEntrance.z <= armoryCenter.z)
            {
                throw new InvalidOperationException(
                    $"Armory cargo corridor entrance must be on the north-east side of the Armory, close to Cargo Hold. Center={armoryCenter}, Entrance={armoryCargoEntrance}");
            }
        }

        private static void RequireNoUndefinedControlSupplyCorridor()
        {
            if (GameObject.Find("Corridor - Control Room to Supply Room") != null ||
                GameObject.Find("Corridor - Supply Room to Control Room") != null ||
                GameObject.Find("Corridor - Control Room to Armory") != null)
            {
                throw new InvalidOperationException("Production ship interior must not create undefined control-supply or control-armory bypass corridors.");
            }
        }

        private static void RequireCorridorGeometryClearance(Transform root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!IsCorridorOverheadOrWall(renderer.name))
                {
                    continue;
                }

                var euler = renderer.transform.rotation.eulerAngles;
                var pitch = Mathf.Abs(NormalizeEulerAngle(euler.x));
                var roll = Mathf.Abs(NormalizeEulerAngle(euler.z));
                if (pitch > 0.25f || roll > 0.25f)
                {
                    throw new InvalidOperationException(
                        $"Corridor wall/ceiling geometry must stay upright instead of pitching into the walkway. Object={renderer.name}, Pitch={pitch:0.00}, Roll={roll:0.00}");
                }
            }

            for (var i = 0; i < RequiredCorridors.Length; i++)
            {
                var corridor = RequiredCorridors[i];
                var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute(corridor.From, corridor.To);
                var corridorObjectName = "Corridor - " + corridor.From + " to " + corridor.To;
                for (var segment = 0; segment < route.Length - 1; segment++)
                {
                    RequireSegmentCenterlineClear(
                        renderers,
                        route[segment],
                        route[segment + 1],
                        corridorObjectName,
                        segment + 1,
                        corridor.From + " to " + corridor.To);
                }
            }
        }

        private static void RequireSegmentCenterlineClear(
            MeshRenderer[] renderers,
            Vector3 from,
            Vector3 to,
            string corridorObjectName,
            int segmentIndex,
            string routeName)
        {
            var segmentPrefix = corridorObjectName + " Segment " + segmentIndex;
            for (var sampleIndex = 1; sampleIndex <= 5; sampleIndex++)
            {
                var t = sampleIndex / 6f;
                var floorPoint = Vector3.Lerp(from, to, t);
                var headPoint = floorPoint + (Vector3.up * 1.45f);

                for (var i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (!renderer.name.StartsWith(segmentPrefix, StringComparison.Ordinal) ||
                        !IsCorridorOverheadOrWall(renderer.name))
                    {
                        continue;
                    }

                    if (ContainsPointInsideCubeRenderer(renderer.transform, headPoint, 0.02f))
                    {
                        throw new InvalidOperationException(
                            $"Corridor centerline must stay visually clear at player head height. Route={routeName}, Object={renderer.name}, Point={headPoint}");
                    }
                }
            }
        }

        private static void RequireCorridorJointSeals(Transform root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < RequiredCorridors.Length; i++)
            {
                var corridor = RequiredCorridors[i];
                var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute(corridor.From, corridor.To);
                if (route.Length <= 2)
                {
                    continue;
                }

                var corridorObjectName = "Corridor - " + corridor.From + " to " + corridor.To;
                for (var jointIndex = 1; jointIndex < route.Length - 1; jointIndex++)
                {
                    var jointPrefix = corridorObjectName + " Joint " + jointIndex;
                    var ceilingCap = GameObject.Find(jointPrefix + " Ceiling Cap");
                    if (ceilingCap == null)
                    {
                        throw new InvalidOperationException("Corridor joint must have a ceiling cap to hide exterior voids: " + jointPrefix);
                    }

                    if (ceilingCap.transform.localScale.x < Phase4CargoShipGrayboxBootstrap.ProductionJointSealSpan - 0.05f ||
                        ceilingCap.transform.localScale.z < Phase4CargoShipGrayboxBootstrap.ProductionJointSealSpan - 0.05f)
                    {
                        throw new InvalidOperationException("Corridor joint ceiling cap is too small to seal the turn: " + ceilingCap.name);
                    }

                    var closureWallCount = 0;
                    for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    {
                        var rendererName = renderers[rendererIndex].name;
                        if (rendererName.StartsWith(jointPrefix, StringComparison.Ordinal) &&
                            rendererName.EndsWith("Closure Wall", StringComparison.Ordinal))
                        {
                            closureWallCount++;
                        }
                    }

                    if (closureWallCount < 2)
                    {
                        throw new InvalidOperationException(
                            "Corridor joint must have at least two closure walls around the outside corner. Joint=" +
                            jointPrefix +
                            ", ClosureWalls=" +
                            closureWallCount);
                    }
                }
            }
        }

        private static void RequireSlopedCorridorEndpointSeals()
        {
            for (var i = 0; i < RequiredCorridors.Length; i++)
            {
                var corridor = RequiredCorridors[i];
                var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute(corridor.From, corridor.To);
                if (route.Length < 2 ||
                    Mathf.Abs(route[route.Length - 1].y - route[0].y) <= 0.5f)
                {
                    continue;
                }

                var corridorName = "Corridor - " + corridor.From + " to " + corridor.To;
                RequireSlopedEndpointSeal(corridorName, "Start");
                RequireSlopedEndpointSeal(corridorName, "End");
            }
        }

        private static void RequireSlopedEndpointSeal(string corridorName, string endName)
        {
            var prefix = corridorName + " " + endName + " Sloped Endpoint Seal";
            var sleeveLeft = RequireObject(prefix + " Sleeve Left Closure Wall");
            var sleeveRight = RequireObject(prefix + " Sleeve Right Closure Wall");
            var sleeveCap = RequireObject(prefix + " Sleeve Ceiling Cap");
            var roomPlaneLeft = RequireObject(prefix + " Room Plane Left Closure Wall");
            var roomPlaneRight = RequireObject(prefix + " Room Plane Right Closure Wall");
            var roomPlaneUpper = RequireObject(prefix + " Room Plane Upper Bulkhead Wall");
            var roomPlaneCap = RequireObject(prefix + " Room Plane Ceiling Cap");
            var leftWedge = RequireObject(prefix + " Left Side Wedge Fill");
            var rightWedge = RequireObject(prefix + " Right Side Wedge Fill");
            var expectedHeight = Phase4CargoShipGrayboxBootstrap.ProductionWallHeight + 2.5f;

            if (sleeveLeft.transform.localScale.y < expectedHeight ||
                sleeveRight.transform.localScale.y < expectedHeight ||
                roomPlaneLeft.transform.localScale.y < expectedHeight ||
                roomPlaneRight.transform.localScale.y < expectedHeight)
            {
                throw new InvalidOperationException("Sloped corridor endpoint side seals must span the low-to-high deck gap: " + prefix);
            }

            if (sleeveCap.transform.localScale.x < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f ||
                sleeveCap.transform.localScale.z < Phase4CargoShipGrayboxBootstrap.ProductionSlopedEndpointSealDepth - 0.05f)
            {
                throw new InvalidOperationException("Sloped corridor endpoint sleeve ceiling cap is too small to cover the ramp mouth: " + sleeveCap.name);
            }

            if (Mathf.Max(roomPlaneUpper.transform.localScale.x, roomPlaneUpper.transform.localScale.z) < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f ||
                roomPlaneUpper.transform.localScale.y < 0.45f)
            {
                throw new InvalidOperationException("Sloped corridor endpoint room-plane bulkhead must seal the doorway top: " + roomPlaneUpper.name);
            }

            if (Mathf.Max(roomPlaneCap.transform.localScale.x, roomPlaneCap.transform.localScale.z) < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f)
            {
                throw new InvalidOperationException("Sloped corridor endpoint room-plane ceiling cap must cover the full doorway mouth: " + roomPlaneCap.name);
            }

            RequireWedgeMesh(leftWedge);
            RequireWedgeMesh(rightWedge);
        }

        private static void RequireWedgeMesh(GameObject target)
        {
            var meshFilter = target.GetComponent<MeshFilter>();
            var meshCollider = target.GetComponent<MeshCollider>();
            if (meshFilter == null || meshFilter.sharedMesh == null || meshCollider == null)
            {
                throw new InvalidOperationException("Sloped endpoint side wedge must be a closed visual/collision mesh: " + target.name);
            }

            if (meshFilter.sharedMesh.vertexCount < 8)
            {
                throw new InvalidOperationException("Sloped endpoint side wedge mesh is too small to seal the side transition: " + target.name);
            }
        }

        private static GameObject RequireObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing required cargo ship graybox object: " + objectName);
            }

            return target;
        }

        private static bool IsCorridorOverheadOrWall(string objectName)
        {
            return objectName.StartsWith("Corridor - ", StringComparison.Ordinal) &&
                   (objectName.EndsWith(" Wall", StringComparison.Ordinal) ||
                    objectName.EndsWith(" Ceiling", StringComparison.Ordinal) ||
                    objectName.EndsWith(" Ceiling Cap", StringComparison.Ordinal) ||
                    objectName.EndsWith(" Cable Tray", StringComparison.Ordinal) ||
                    objectName.EndsWith(" Post", StringComparison.Ordinal) ||
                    objectName.EndsWith(" Lintel", StringComparison.Ordinal));
        }

        private static void RequireCorridorCeilingsSitOnWalls(Transform root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var ceiling = renderers[i];
                if (!ceiling.name.StartsWith("Corridor - ", StringComparison.Ordinal) ||
                    !ceiling.name.EndsWith(" Ceiling", StringComparison.Ordinal))
                {
                    continue;
                }

                var parent = ceiling.transform.parent;
                if (parent == null)
                {
                    throw new InvalidOperationException("Corridor ceiling must be parented with its wall segment: " + ceiling.name);
                }

                var leftWall = parent.Find(ceiling.name.Replace(" Ceiling", " Left Wall"));
                var rightWall = parent.Find(ceiling.name.Replace(" Ceiling", " Right Wall"));
                if (leftWall == null || rightWall == null)
                {
                    throw new InvalidOperationException("Corridor ceiling must have matching left/right walls: " + ceiling.name);
                }

                var leftRenderer = leftWall.GetComponent<MeshRenderer>();
                var rightRenderer = rightWall.GetComponent<MeshRenderer>();
                if (leftRenderer == null || rightRenderer == null)
                {
                    throw new InvalidOperationException("Corridor wall renderers are missing for ceiling: " + ceiling.name);
                }

                var wallTopY = Mathf.Min(leftRenderer.bounds.max.y, rightRenderer.bounds.max.y);
                var ceilingBottomY = ceiling.bounds.min.y;
                if (ceilingBottomY - wallTopY > 0.03f)
                {
                    throw new InvalidOperationException(
                        $"Corridor ceiling must sit directly on top of corridor walls. Object={ceiling.name}, Gap={ceilingBottomY - wallTopY:0.000}");
                }
            }
        }

        private static bool ContainsPointInsideCubeRenderer(Transform transform, Vector3 point, float margin)
        {
            var localPoint = transform.InverseTransformPoint(point);
            return Mathf.Abs(localPoint.x) <= 0.5f + margin &&
                   Mathf.Abs(localPoint.y) <= 0.5f + margin &&
                   Mathf.Abs(localPoint.z) <= 0.5f + margin;
        }

        private static float NormalizeEulerAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private static void RequireProductionMaterials()
        {
            var materialPaths = Phase4CargoShipGrayboxBootstrap.ProductionMaterialPaths();
            for (var i = 0; i < materialPaths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<Material>(materialPaths[i]) == null)
                {
                    throw new InvalidOperationException("Missing production ship interior material: " + materialPaths[i]);
                }
            }

            if (Phase4CargoShipGrayboxBootstrap.ProductionCorridorWidth < 2.4f ||
                Phase4CargoShipGrayboxBootstrap.ProductionCorridorWidth > 2.8f)
            {
                throw new InvalidOperationException(
                    "Production corridor width must stay inside the approved 2-person target range.");
            }
        }

    }
}
