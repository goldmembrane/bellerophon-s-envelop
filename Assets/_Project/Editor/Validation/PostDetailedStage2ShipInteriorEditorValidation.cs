using System;
using Bellerophon.Core.Ship;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class PostDetailedStage2ShipInteriorEditorValidation
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

        public static void Run()
        {
            Phase20PresentationBootstrap.EnsurePhase20Assets();
            Phase20PresentationEditorValidation.Run();

            for (var i = 0; i < RequiredRooms.Length; i++)
            {
                var room = RequiredRooms[i];
                if (!Phase4CargoShipGrayboxBootstrap.HasProductionRoomShell(room))
                {
                    throw new InvalidOperationException("Post-detailed stage 2 requires a closed production shell for " + room + ".");
                }
            }

            RequireObject("Cargo Hold Central Cargo");
            RequireObject("Cargo Hold Securing Frame Left");
            RequireObject("Cockpit Forward Frame Top");
            RequireObject("Armory Turret Station Support Frame");
            RequireObject("Corridor - Cargo Hold to Armory Segment 1 Floor");
            RequireObject("Corridor - Cargo Hold to Supply Room Segment 1 Floor");
            RequireObject("Corridor - Supply Room to Armory Segment 1 Floor");
            RequireObject("Ceiling - Cargo Hold");
            RequireObject("Door Frame - Cargo Hold - North Cockpit Lintel");
            RequireObject("Door Frame - Armory - North Cargo Lintel");
            RequireObject("Door Frame - Supply Room - North Cargo Lintel");
            RequireDoorHeaderWalls();
            RequireThresholdSeals();
            RequireSlopedEndpointSeals();
            RequireCorridorJointSeals();
            RequireMissingObject("Corridor - Cargo Hold to Armory Segment 2 Floor");
            RequireMissingObject("Corridor - Cargo Hold to Supply Room Segment 2 Floor");
            RequireMissingObject("Corridor - Control Room to Supply Room");
            RequireMissingObject("Corridor - Supply Room to Control Room");
            RequireMissingObject("Corridor - Control Room to Armory");
            RequireMissingObject("Phase 16 Map Corridor - ControlRoom to SupplyRoom");
            RequireMissingObject("Phase 16 Map Corridor - SupplyRoom to ControlRoom");
            RequireMissingObject("Phase 16 Map Corridor - ControlRoom to Armory");

            var armoryCargoSegments = Phase4CargoShipGrayboxBootstrap.CorridorSegmentCount("Cargo Hold", "Armory");
            if (armoryCargoSegments != 1)
            {
                throw new InvalidOperationException(
                    "Post-detailed stage 2 must use a direct smooth armory-cargo ramp with no blocking mid-corner. Segments=" +
                    armoryCargoSegments);
            }

            var supplyCargoSegments = Phase4CargoShipGrayboxBootstrap.CorridorSegmentCount("Cargo Hold", "Supply Room");
            if (supplyCargoSegments != 1)
            {
                throw new InvalidOperationException("Post-detailed stage 2 requires a direct smooth cargo-supply ramp route that clears the supply-armory corridor. Segments=" + supplyCargoSegments);
            }

            var supplyArmorySegments = Phase4CargoShipGrayboxBootstrap.CorridorSegmentCount("Supply Room", "Armory");
            if (supplyArmorySegments != 1)
            {
                throw new InvalidOperationException("Post-detailed stage 2 requires only the defined one-segment supply-armory corridor, not a control-supply bypass. Segments=" + supplyArmorySegments);
            }

            RequireSeparatedRoutes("Cargo Hold", "Armory", "Cargo Hold", "Supply Room", 3.0f);
            RequireCorridorFloorsHaveColliders();

            if (Phase4CargoShipGrayboxBootstrap.ProductionCorridorWidth < 2.4f ||
                Phase4CargoShipGrayboxBootstrap.ProductionCorridorWidth > 2.8f)
            {
                throw new InvalidOperationException("Post-detailed stage 2 corridor width must stay inside the approved two-person target.");
            }

            var mapRoom = ShipInteriorMapRules.FindCurrentRoom(new Vector3(0f, -3f, -5f));
            if (mapRoom != Bellerophon.Core.Session.ShipRoomId.CargoHold)
            {
                throw new InvalidOperationException("Post-detailed stage 2 must preserve the player start in Cargo Hold.");
            }

            Debug.Log("Post-detailed stage 2 ship interior editor validation passed.");
            Debug.Log(
                "Post-detailed stage 2 ship interior details: Rooms=" +
                RequiredRooms.Length +
                "; CorridorWidth=" +
                Phase4CargoShipGrayboxBootstrap.ProductionCorridorWidth.ToString("0.0") +
                "; ArmoryCargoSegments=" +
                armoryCargoSegments +
                "; SupplyCargoSegments=" +
                supplyCargoSegments +
                "; SupplyArmorySegments=" +
                supplyArmorySegments +
                "; ClosedShell=True; RuntimeIntegration=True");
        }

        private static GameObject RequireObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing post-detailed stage 2 ship interior object: " + objectName);
            }

            return target;
        }

        private static void RequireMissingObject(string objectName)
        {
            if (GameObject.Find(objectName) != null)
            {
                throw new InvalidOperationException("Post-detailed stage 2 must not create undefined corridor object: " + objectName);
            }
        }

        private static void RequireDoorHeaderWalls()
        {
            var requiredHeaders = new[]
            {
                "Door Header Wall - Cargo Hold - North Engine",
                "Door Header Wall - Cargo Hold - North Cockpit",
                "Door Header Wall - Cargo Hold - North Control",
                "Door Header Wall - Cargo Hold - South Armory",
                "Door Header Wall - Cargo Hold - South Supply",
                "Door Header Wall - Cockpit - South Cargo",
                "Door Header Wall - Engine Room - South Cargo",
                "Door Header Wall - Engine Room - North Control",
                "Door Header Wall - Control Room - South Cargo",
                "Door Header Wall - Control Room - North Engine",
                "Door Header Wall - Armory - North Cargo",
                "Door Header Wall - Supply Room - North Cargo"
            };

            for (var i = 0; i < requiredHeaders.Length; i++)
            {
                RequireObject(requiredHeaders[i]);
            }
        }

        private static void RequireThresholdSeals()
        {
            var corridors = new[]
            {
                "Corridor - Cargo Hold to Cockpit",
                "Corridor - Cargo Hold to Engine Room",
                "Corridor - Cargo Hold to Control Room",
                "Corridor - Cargo Hold to Armory",
                "Corridor - Cargo Hold to Supply Room",
                "Corridor - Supply Room to Armory",
                "Corridor - Cockpit to Engine Room",
                "Corridor - Cockpit to Control Room",
                "Corridor - Engine Room to Control Room"
            };

            for (var i = 0; i < corridors.Length; i++)
            {
                RequireThresholdSeal(corridors[i], "Start");
                RequireThresholdSeal(corridors[i], "End");
            }
        }

        private static void RequireThresholdSeal(string corridorName, string endName)
        {
            var prefix = corridorName + " " + endName + " Threshold Seal";
            RequireObject(prefix + " Left Reveal Wall");
            RequireObject(prefix + " Right Reveal Wall");
            RequireObject(prefix + " Left Mouth Closure Wall");
            RequireObject(prefix + " Right Mouth Closure Wall");
            var upperBulkhead = RequireObject(prefix + " Upper Bulkhead Wall");
            var ceilingCap = RequireObject(prefix + " Ceiling Cap");

            if (upperBulkhead.transform.localScale.x < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f)
            {
                throw new InvalidOperationException("Threshold upper bulkhead is too narrow to seal the room-corridor mouth: " + upperBulkhead.name);
            }

            if (upperBulkhead.transform.localScale.y < 0.45f)
            {
                throw new InvalidOperationException("Threshold upper bulkhead must close the visible gap above the doorway: " + upperBulkhead.name);
            }

            if (ceilingCap.transform.localScale.x < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f)
            {
                throw new InvalidOperationException("Threshold ceiling cap is too narrow to cover the full room-corridor mouth: " + ceilingCap.name);
            }
        }

        private static void RequireSlopedEndpointSeals()
        {
            var corridors = new[]
            {
                "Corridor - Cargo Hold to Cockpit",
                "Corridor - Cargo Hold to Engine Room",
                "Corridor - Cargo Hold to Control Room",
                "Corridor - Cargo Hold to Armory",
                "Corridor - Cargo Hold to Supply Room"
            };

            for (var i = 0; i < corridors.Length; i++)
            {
                RequireSlopedEndpointSeal(corridors[i], "Start");
                RequireSlopedEndpointSeal(corridors[i], "End");
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
                throw new InvalidOperationException("Sloped endpoint side seals must span the cargo-to-upper-deck gap: " + prefix);
            }

            if (sleeveCap.transform.localScale.x < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f ||
                sleeveCap.transform.localScale.z < Phase4CargoShipGrayboxBootstrap.ProductionSlopedEndpointSealDepth - 0.05f)
            {
                throw new InvalidOperationException("Sloped endpoint sleeve ceiling cap is too small to seal the ramp mouth: " + sleeveCap.name);
            }

            if (Mathf.Max(roomPlaneUpper.transform.localScale.x, roomPlaneUpper.transform.localScale.z) < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f ||
                roomPlaneUpper.transform.localScale.y < 0.45f)
            {
                throw new InvalidOperationException("Sloped endpoint room-plane bulkhead must seal the doorway top: " + roomPlaneUpper.name);
            }

            if (Mathf.Max(roomPlaneCap.transform.localScale.x, roomPlaneCap.transform.localScale.z) < Phase4CargoShipGrayboxBootstrap.ProductionThresholdMouthWidth - 0.05f)
            {
                throw new InvalidOperationException("Sloped endpoint room-plane ceiling cap must cover the full doorway mouth: " + roomPlaneCap.name);
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

        private static void RequireCorridorJointSeals()
        {
            RequireCorridorJointSeal("Corridor - Engine Room to Control Room", 1);
            RequireCorridorJointSeal("Corridor - Engine Room to Control Room", 2);
        }

        private static void RequireCorridorJointSeal(string corridorName, int jointIndex)
        {
            var prefix = corridorName + " Joint " + jointIndex;
            var ceilingCap = RequireObject(prefix + " Ceiling Cap");
            if (ceilingCap.transform.localScale.x < Phase4CargoShipGrayboxBootstrap.ProductionJointSealSpan - 0.05f ||
                ceilingCap.transform.localScale.z < Phase4CargoShipGrayboxBootstrap.ProductionJointSealSpan - 0.05f)
            {
                throw new InvalidOperationException("Corridor joint ceiling cap is too small to seal the room-corridor exterior void: " + ceilingCap.name);
            }

            var closureWallCount = 0;
            var root = GameObject.Find(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (root == null)
            {
                throw new InvalidOperationException("Missing production ship interior root.");
            }

            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var rendererName = renderers[i].name;
                if (rendererName.StartsWith(prefix, StringComparison.Ordinal) &&
                    rendererName.EndsWith("Closure Wall", StringComparison.Ordinal))
                {
                    closureWallCount++;
                }
            }

            if (closureWallCount < 2)
            {
                throw new InvalidOperationException("Corridor joint must seal both outside sides of the turn: " + prefix);
            }
        }

        private static void RequireCorridorFloorsHaveColliders()
        {
            var root = GameObject.Find(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (root == null)
            {
                throw new InvalidOperationException("Missing production ship interior root.");
            }

            var floorRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < floorRenderers.Length; i++)
            {
                var floor = floorRenderers[i].gameObject;
                if (!floor.name.StartsWith("Corridor - ", StringComparison.Ordinal) ||
                    !floor.name.EndsWith(" Floor", StringComparison.Ordinal))
                {
                    continue;
                }

                if (floor.GetComponent<Collider>() == null)
                {
                    throw new InvalidOperationException("Corridor floor must have a collider to prevent fall-through gaps: " + floor.name);
                }
            }
        }

        private static void RequireSeparatedRoutes(
            string firstFrom,
            string firstTo,
            string secondFrom,
            string secondTo,
            float minimumPlanarDistance)
        {
            var first = Phase4CargoShipGrayboxBootstrap.CorridorRoute(firstFrom, firstTo);
            var second = Phase4CargoShipGrayboxBootstrap.CorridorRoute(secondFrom, secondTo);
            var distance = FindMinimumPlanarDistance(first, second);
            if (distance < minimumPlanarDistance)
            {
                throw new InvalidOperationException(
                    firstFrom +
                    " to " +
                    firstTo +
                    " corridor overlaps or crowds " +
                    secondFrom +
                    " to " +
                    secondTo +
                    ". MinimumDistance=" +
                    distance.ToString("0.00"));
            }
        }

        private static float FindMinimumPlanarDistance(Vector3[] first, Vector3[] second)
        {
            var minimum = float.MaxValue;
            for (var firstIndex = 0; firstIndex < first.Length - 1; firstIndex++)
            {
                for (var secondIndex = 0; secondIndex < second.Length - 1; secondIndex++)
                {
                    var segmentDistance = FindMinimumPlanarSegmentDistance(
                        first[firstIndex],
                        first[firstIndex + 1],
                        second[secondIndex],
                        second[secondIndex + 1]);
                    minimum = Mathf.Min(minimum, segmentDistance);
                }
            }

            return minimum;
        }

        private static float FindMinimumPlanarSegmentDistance(Vector3 firstStart, Vector3 firstEnd, Vector3 secondStart, Vector3 secondEnd)
        {
            const int samples = 20;
            var minimum = float.MaxValue;
            for (var firstSample = 0; firstSample <= samples; firstSample++)
            {
                var firstPoint = Vector3.Lerp(firstStart, firstEnd, firstSample / (float)samples);
                for (var secondSample = 0; secondSample <= samples; secondSample++)
                {
                    var secondPoint = Vector3.Lerp(secondStart, secondEnd, secondSample / (float)samples);
                    minimum = Mathf.Min(minimum, PlanarDistance(firstPoint, secondPoint));
                }
            }

            return minimum;
        }

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            var deltaX = first.x - second.x;
            var deltaZ = first.z - second.z;
            return Mathf.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
        }
    }
}
