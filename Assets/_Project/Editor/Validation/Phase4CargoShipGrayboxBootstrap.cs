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
        private const string FloorMaterialPath = SettingsDirectory + "/GrayboxFloorMaterial.mat";
        private const string CorridorMaterialPath = SettingsDirectory + "/GrayboxCorridorMaterial.mat";
        private const string WallMaterialPath = SettingsDirectory + "/GrayboxWallMaterial.mat";
        private const string GlassMaterialPath = SettingsDirectory + "/CockpitGlassMaterial.mat";
        private const string ConsoleMaterialPath = SettingsDirectory + "/GrayboxConsoleMaterial.mat";
        private const string CargoMaterialPath = SettingsDirectory + "/GrayboxCargoMaterial.mat";
        private const string InteractableMaterialPath = SettingsDirectory + "/GrayboxInteractableMaterial.mat";
        private const float UpperDeckY = 0f;
        private const float CargoHoldDeckY = -3.0f;
        private const float CorridorWidth = 3f;
        private const float SegmentedCorridorJointOverlap = 0.45f;
        private const int ArmoryCargoCurveSegmentCount = 20;

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
            new CorridorSpec("Engine Room", "Control Room"),
            new CorridorSpec("Control Room", "Armory")
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 4 Cargo Ship Graybox")]
        public static void EnsurePhase4Assets()
        {
            Directory.CreateDirectory(SettingsDirectory);

            Phase2PlayerMvpBootstrap.EnsurePhase2Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(GrayboxRootName);
            DeleteGeneratedObject("Cargo Bay Test Floor");
            DeleteGeneratedObject("Cargo Bay Back Wall");
            DeleteGeneratedObject("Phase 2 Interaction Target");

            var floorMaterial = EnsureMaterial(FloorMaterialPath, new Color(0.18f, 0.2f, 0.2f, 1f));
            var corridorMaterial = EnsureMaterial(CorridorMaterialPath, new Color(0.14f, 0.16f, 0.16f, 1f));
            var wallMaterial = EnsureMaterial(WallMaterialPath, new Color(0.31f, 0.34f, 0.35f, 1f));
            var glassMaterial = EnsureMaterial(GlassMaterialPath, new Color(0.18f, 0.42f, 0.62f, 0.55f));
            var consoleMaterial = EnsureMaterial(ConsoleMaterialPath, new Color(0.08f, 0.12f, 0.13f, 1f));
            var cargoMaterial = EnsureMaterial(CargoMaterialPath, new Color(0.48f, 0.31f, 0.15f, 1f));
            var interactableMaterial = EnsureMaterial(InteractableMaterialPath, new Color(0.94f, 0.68f, 0.22f, 1f));

            var root = new GameObject(GrayboxRootName);
            CreateRooms(root.transform, floorMaterial, wallMaterial);
            CreateCorridors(root.transform, corridorMaterial);
            CreateRoomFeatures(root.transform, wallMaterial, glassMaterial, consoleMaterial, cargoMaterial, interactableMaterial);
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

        private static void CreateRooms(Transform root, Material floorMaterial, Material wallMaterial)
        {
            foreach (var room in Rooms)
            {
                var roomRoot = new GameObject("Room - " + room.Name);
                roomRoot.transform.SetParent(root, false);
                roomRoot.transform.position = room.Center;

                CreateBox(
                    "Floor - " + room.Name,
                    roomRoot.transform,
                    Vector3.down * 0.05f,
                    new Vector3(room.Size.x, 0.1f, room.Size.y),
                    Quaternion.identity,
                    floorMaterial,
                    true);

                CreateLabel("Label - " + room.Name, room.Name, roomRoot.transform, new Vector3(0f, 1.8f, -room.Size.y * 0.42f), 0f);
            }
        }

        private static void CreateCorridors(Transform root, Material material)
        {
            var corridorRoot = new GameObject("Corridors");
            corridorRoot.transform.SetParent(root, false);

            foreach (var corridor in Corridors)
            {
                var corridorName = "Corridor - " + corridor.From + " to " + corridor.To;
                if (corridor.Connects("Cargo Hold", "Armory"))
                {
                    CreateSegmentedCorridor(
                        corridorName,
                        corridorRoot.transform,
                        GetArmoryCargoCorridorRoute(),
                        CorridorWidth,
                        material);
                    continue;
                }

                var from = GetCorridorEndpoint(corridor.From, corridor.To);
                var to = GetCorridorEndpoint(corridor.To, corridor.From);
                CreateCorridor(corridorName, corridorRoot.transform, from, to, CorridorWidth, material);
            }
        }

        private static void CreateSegmentedCorridor(string name, Transform parent, Vector3[] points, float width, Material material)
        {
            var corridorRoot = new GameObject(name);
            corridorRoot.transform.SetParent(parent, false);

            for (var i = 0; i < points.Length - 1; i++)
            {
                CreateCorridor(
                    name + " Segment " + (i + 1),
                    corridorRoot.transform,
                    points[i],
                    points[i + 1],
                    width,
                    material,
                    SegmentedCorridorJointOverlap);
            }
        }

        private static void CreateCorridor(
            string name,
            Transform parent,
            Vector3 from,
            Vector3 to,
            float width,
            Material material,
            float lengthOverlap = 0f)
        {
            var delta = to - from;
            var length = delta.magnitude;
            var center = (from + to) * 0.5f;
            var rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            CreateBox(name, parent, new Vector3(center.x, center.y - 0.06f, center.z), new Vector3(width, 0.08f, length + lengthOverlap), rotation, material, true);
        }

        private static void CreateRoomFeatures(
            Transform root,
            Material wallMaterial,
            Material glassMaterial,
            Material consoleMaterial,
            Material cargoMaterial,
            Material interactableMaterial)
        {
            var featureRoot = new GameObject("Room Feature Placeholders");
            featureRoot.transform.SetParent(root, false);

            CreateCargoHoldFeatures(featureRoot.transform, cargoMaterial, interactableMaterial);
            CreateCockpitFeatures(featureRoot.transform, wallMaterial, glassMaterial, consoleMaterial, interactableMaterial);
            CreateEngineRoomFeatures(featureRoot.transform, consoleMaterial, interactableMaterial);
            CreateControlRoomFeatures(featureRoot.transform, consoleMaterial, interactableMaterial);
            CreateArmoryFeatures(featureRoot.transform, consoleMaterial, interactableMaterial);
            CreateSupplyRoomFeatures(featureRoot.transform, consoleMaterial, interactableMaterial);
        }

        private static void CreateCargoHoldFeatures(Transform parent, Material cargoMaterial, Material interactableMaterial)
        {
            CreateBox("Cargo Hold Central Cargo", parent, RoomPoint("Cargo Hold", 0f, 0.7f, 0f), new Vector3(2.4f, 1.4f, 3f), Quaternion.identity, cargoMaterial, true);
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
            Material interactableMaterial)
        {
            CreateBox("Cockpit Front Glass", parent, RoomPoint("Cockpit", 0f, 1.4f, 4.1f), new Vector3(8.8f, 2.4f, 0.18f), Quaternion.identity, glassMaterial, false);
            CreateBox("Cockpit Rear Slope Placeholder", parent, RoomPoint("Cockpit", 0f, 0.15f, -4.6f), new Vector3(3.5f, 0.3f, 2.2f), Quaternion.identity, wallMaterial, false);
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

        private static void CreateArmoryFeatures(Transform parent, Material consoleMaterial, Material interactableMaterial)
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
            return GetArmoryCargoCorridorRoute();
        }

        public static int CorridorSegmentCount(string from, string to)
        {
            var corridor = GameObject.Find("Corridor - " + from + " to " + to);
            if (corridor == null)
            {
                return 0;
            }

            return corridor.transform.childCount > 0 ? corridor.transform.childCount : 1;
        }

        private static Vector3 GetCorridorEndpoint(string roomName, string otherRoomName)
        {
            if (roomName == "Cargo Hold" && otherRoomName == "Armory")
            {
                return RoomPoint("Cargo Hold", -5.2f, 0f, -5.2f);
            }

            if (roomName == "Control Room" && otherRoomName == "Cargo Hold")
            {
                return RoomPoint("Control Room", 3.2f, 0f, -3.2f);
            }

            if (roomName == "Armory" && otherRoomName == "Cargo Hold")
            {
                return RoomPoint("Armory", -4.2f, 0f, -1.6f);
            }

            return FindRoom(roomName).Center;
        }

        private static Vector3[] GetArmoryCargoCorridorRoute()
        {
            return SampleCubicBezier(
                GetCorridorEndpoint("Cargo Hold", "Armory"),
                new Vector3(-16.5f, CargoHoldDeckY + 0.2f, -5.2f),
                new Vector3(-25f, UpperDeckY, -14.9f),
                GetCorridorEndpoint("Armory", "Cargo Hold"),
                ArmoryCargoCurveSegmentCount);
        }

        private static Vector3 RoomPoint(string roomName, float localX, float localY, float localZ)
        {
            var room = FindRoom(roomName);
            return room.Center + new Vector3(localX, localY, localZ);
        }

        private static Vector3[] SampleCubicBezier(Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, int segmentCount)
        {
            var points = new Vector3[segmentCount + 1];
            for (var i = 0; i <= segmentCount; i++)
            {
                var t = i / (float)segmentCount;
                points[i] = CubicBezier(start, controlA, controlB, end, t);
            }

            return points;
        }

        private static Vector3 CubicBezier(Vector3 start, Vector3 controlA, Vector3 controlB, Vector3 end, float t)
        {
            var inverseT = 1f - t;
            return (inverseT * inverseT * inverseT * start)
                   + (3f * inverseT * inverseT * t * controlA)
                   + (3f * inverseT * t * t * controlB)
                   + (t * t * t * end);
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
