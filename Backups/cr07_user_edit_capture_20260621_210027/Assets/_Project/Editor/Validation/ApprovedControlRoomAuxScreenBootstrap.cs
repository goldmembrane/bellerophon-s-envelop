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
    public static class ApprovedControlRoomAuxScreenBootstrap
    {
        public const string RootName = "Approved Control Room 07 Aux Screen";

        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/ControlRoom";
        private const string DisplayTexturePath = "Assets/Heavy Station Kit/_common/Textures/GUI/C2_ElC2Disp.png";
        private const string MainScreenObjectName = "CR-01 blank future main screen recessed wall bay";

        private const float AuxDisplayWidth = 1.42f;
        private const float AuxDisplayHeight = 0.44f;
        private const float FrameWidth = AuxDisplayWidth + 0.28f;
        private const float FrameHeight = AuxDisplayHeight + 0.22f;
        private const float MainToAuxCenterX = 3.76f;
        private const float AuxCenterAboveMainTop = 0.55f;

        private static readonly string[] CockpitRootNames =
        {
            ApprovedCockpitStructureBootstrap.RootName,
            ApprovedCockpitWindowBootstrap.RootName,
            ApprovedCockpitConsoleBootstrap.RootName,
            ApprovedCockpitWarningBootstrap.RootName,
            ApprovedCockpitDirectionBootstrap.RootName
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Control Room 07 Aux Screen")]
        public static void EnsureApprovedControlRoomAuxScreen()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be opened.");
            }

            var engineRoot = RequireObject(ApprovedEngineRoomShellBootstrap.RootName);
            var cockpitRoots = FindExistingObjects(CockpitRootNames);
            if (cockpitRoots.Count == 0)
            {
                throw new InvalidOperationException("No approved cockpit roots were found.");
            }

            var controlRoot = RequireObject(ApprovedControlRoomShellBootstrap.RootName);
            var protectedRoots = new List<GameObject>();
            protectedRoots.Add(engineRoot);
            protectedRoots.Add(controlRoot);
            protectedRoots.AddRange(cockpitRoots);
            protectedRoots.AddRange(FindApprovedControlRoomRootsExcept(RootName, controlRoot));
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            var mainScreen = FindChildByName(controlRoot.transform, MainScreenObjectName);
            if (mainScreen == null)
            {
                throw new InvalidOperationException("Could not find CR-01 main screen reference object: " + MainScreenObjectName);
            }

            var mainScreenBounds = GetRendererBounds(mainScreen.transform);
            var engineBounds = GetRendererBounds(engineRoot.transform);
            var cockpitBounds = GetCombinedRendererBounds(cockpitRoots);

            DeleteGeneratedObject(RootName);
            Directory.CreateDirectory(UnityAssetDirectory);

            var materials = EnsureMaterials();
            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var placement = CalculatePlacement(mainScreenBounds);
            BuildAuxScreen(root.transform, placement, materials);
            DisableAllColliders(root.transform);

            var auxBounds = GetRendererBounds(root.transform);
            var displayBounds = GetRendererBounds(RequireChild(root.transform, "CR-07 C2_ElC2Disp full display texture"));
            EnsureAboveMainScreen(displayBounds, mainScreenBounds);
            EnsureNoOverlap(auxBounds, engineBounds, "engine room");
            EnsureNoOverlap(auxBounds, cockpitBounds, "cockpit");
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved CR-07 control room auxiliary screen applied. Root=" +
                RootName +
                "; Center=" +
                FormatVector(auxBounds.center) +
                "; Bounds=" +
                FormatBounds(auxBounds) +
                "; Parts=" +
                root.GetComponentsInChildren<Renderer>(true).Length +
                "; DisplayTexture=C2_ElC2Disp.png" +
                "; DisplayTextureApplied=True" +
                "; AuxScreenAboveMainScreen=True" +
                "; ControlRoomUntouched=True" +
                "; CockpitUntouched=True" +
                "; EngineRoomUntouched=True" +
                "; AuxScreenOverlapsEngineRoom=False" +
                "; AuxScreenOverlapsCockpit=False");
        }

        private static AuxScreenPlacement CalculatePlacement(Bounds mainScreenBounds)
        {
            var displayCenter = new Vector3(
                mainScreenBounds.center.x + MainToAuxCenterX,
                mainScreenBounds.max.y + AuxCenterAboveMainTop,
                mainScreenBounds.min.z - 0.30f);

            return new AuxScreenPlacement(displayCenter, mainScreenBounds.min.z);
        }

        private static void BuildAuxScreen(Transform root, AuxScreenPlacement placement, AuxScreenMaterials materials)
        {
            var group = AddGroup(root, "CR-07 Auxiliary Screen - individually editable");
            var displayCenter = placement.DisplayCenter;
            var wallZ = placement.MainScreenFrontZ;

            AddBox(
                "CR-07 amber reserved upper right mounting surround",
                group,
                new Vector3(displayCenter.x, displayCenter.y, wallZ - 0.06f),
                new Vector3(FrameWidth + 0.46f, FrameHeight + 0.34f, 0.08f),
                materials.Zone,
                0f);
            AddBox(
                "CR-07 wall-side mounting pad",
                group,
                new Vector3(displayCenter.x, displayCenter.y, wallZ - 0.11f),
                new Vector3(FrameWidth + 0.30f, FrameHeight + 0.22f, 0.105f),
                materials.Mount,
                0f);
            AddBox(
                "CR-07 black vibration gasket",
                group,
                new Vector3(displayCenter.x, displayCenter.y, wallZ - 0.16f),
                new Vector3(FrameWidth + 0.13f, FrameHeight + 0.10f, 0.065f),
                materials.Rubber,
                0f);
            AddBox(
                "CR-07 horizontal armored frame",
                group,
                new Vector3(displayCenter.x, displayCenter.y, wallZ - 0.22f),
                new Vector3(FrameWidth, FrameHeight, 0.13f),
                materials.Frame,
                0f);
            AddBox(
                "CR-07 smoked glass bevel lip",
                group,
                new Vector3(displayCenter.x, displayCenter.y, wallZ - 0.285f),
                new Vector3(AuxDisplayWidth + 0.08f, AuxDisplayHeight + 0.07f, 0.025f),
                materials.Glass,
                0f);
            AddBox(
                "CR-07 inactive horizontal auxiliary display backing",
                group,
                new Vector3(displayCenter.x, displayCenter.y, wallZ - 0.315f),
                new Vector3(AuxDisplayWidth, AuxDisplayHeight, 0.018f),
                materials.ScreenBacking,
                0f);
            AddDisplayPlane(
                "CR-07 C2_ElC2Disp full display texture",
                group,
                new Vector3(displayCenter.x, displayCenter.y, wallZ - 0.333f),
                AuxDisplayWidth,
                AuxDisplayHeight,
                materials.DisplayTexture);

            AddCornerBolts(group, materials.Bolt, displayCenter, wallZ - 0.345f);

            AddBox(
                "CR-07 left bracket bolted to main screen bay",
                group,
                new Vector3(displayCenter.x - FrameWidth * 0.5f - 0.13f, displayCenter.y, wallZ - 0.18f),
                new Vector3(0.13f, FrameHeight * 0.78f, 0.15f),
                materials.Bracket,
                0f);
            AddBox(
                "CR-07 upper right anti-vibration clamp",
                group,
                new Vector3(displayCenter.x + FrameWidth * 0.34f, displayCenter.y + FrameHeight * 0.5f + 0.08f, wallZ - 0.165f),
                new Vector3(0.42f, 0.075f, 0.12f),
                materials.Bracket,
                0f);
            AddBox(
                "CR-07 lower right anti-vibration clamp",
                group,
                new Vector3(displayCenter.x + FrameWidth * 0.34f, displayCenter.y - FrameHeight * 0.5f - 0.08f, wallZ - 0.165f),
                new Vector3(0.42f, 0.075f, 0.12f),
                materials.Bracket,
                0f);

            var cableX = displayCenter.x + FrameWidth * 0.5f + 0.10f;
            AddBox(
                "CR-07 right cable socket",
                group,
                new Vector3(cableX, displayCenter.y, wallZ - 0.22f),
                new Vector3(0.10f, 0.32f, 0.15f),
                materials.Socket,
                0f);
            AddCylinder(
                "CR-07 round side cable gland",
                group,
                new Vector3(cableX + 0.08f, displayCenter.y, wallZ - 0.22f),
                0.034f,
                0.10f,
                materials.Conduit,
                Quaternion.Euler(0f, 0f, 90f));
            AddBox(
                "CR-07 short cable run into upper wall conduit",
                group,
                new Vector3(cableX + 0.12f, displayCenter.y + 0.28f, wallZ - 0.11f),
                new Vector3(0.05f, 0.58f, 0.055f),
                materials.Conduit,
                0f);
            AddBox(
                "CR-07 small service latch",
                group,
                new Vector3(displayCenter.x + 0.18f, displayCenter.y - FrameHeight * 0.48f, wallZ - 0.352f),
                new Vector3(0.34f, 0.055f, 0.012f),
                materials.Latch,
                0f);
            AddBox(
                "CR-07 upper control room wall conduit continuing behind screen",
                group,
                new Vector3(displayCenter.x - 1.85f, displayCenter.y + 0.34f, wallZ - 0.02f),
                new Vector3(3.70f, 0.070f, 0.052f),
                materials.Conduit,
                0f);
            AddTextLabel(
                "CR-07 placement label",
                group,
                "CR-07 보조 스크린",
                new Vector3(displayCenter.x, displayCenter.y + 0.36f, wallZ - 0.365f),
                materials.Label);
        }

        private static AuxScreenMaterials EnsureMaterials()
        {
            var displayTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DisplayTexturePath);
            if (displayTexture == null)
            {
                throw new InvalidOperationException("Missing CR-07 display texture: " + DisplayTexturePath);
            }

            return new AuxScreenMaterials(
                EnsureMaterial("M_Cr07_Mount", new Color(0.21f, 0.24f, 0.22f, 1f), 0.22f, 0.16f, false),
                EnsureMaterial("M_Cr07_Rubber", new Color(0.010f, 0.012f, 0.012f, 1f), 0.0f, 0.06f, false),
                EnsureMaterial("M_Cr07_Frame", new Color(0.29f, 0.32f, 0.29f, 1f), 0.24f, 0.18f, false),
                EnsureMaterial("M_Cr07_GlassLip", new Color(0.010f, 0.020f, 0.024f, 1f), 0.0f, 0.55f, false),
                EnsureMaterial("M_Cr07_ScreenBacking", new Color(0.010f, 0.055f, 0.060f, 1f), 0.0f, 0.52f, true),
                EnsureDisplayMaterial("M_Cr07_Display_C2_ElC2Disp", displayTexture),
                EnsureMaterial("M_Cr07_Bracket", new Color(0.12f, 0.13f, 0.12f, 1f), 0.26f, 0.20f, false),
                EnsureMaterial("M_Cr07_Socket", new Color(0.08f, 0.09f, 0.09f, 1f), 0.28f, 0.22f, false),
                EnsureMaterial("M_Cr07_Conduit", new Color(0.045f, 0.055f, 0.055f, 1f), 0.28f, 0.20f, false),
                EnsureMaterial("M_Cr07_Bolt", new Color(0.36f, 0.36f, 0.31f, 1f), 0.30f, 0.25f, false),
                EnsureMaterial("M_Cr07_Latch", new Color(0.38f, 0.40f, 0.34f, 1f), 0.24f, 0.24f, false),
                EnsureMaterial("M_Cr07_Zone", new Color(0.95f, 0.50f, 0.10f, 1f), 0.05f, 0.28f, true),
                EnsureMaterial("M_Cr07_Label", new Color(0.78f, 0.88f, 0.84f, 1f), 0.0f, 0.30f, true));
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

        private static Material EnsureDisplayMaterial(string name, Texture2D texture)
        {
            var path = UnityAssetDirectory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Universal Render Pipeline/Lit") ??
                             Shader.Find("Unlit/Texture") ??
                             Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            SetTexture(material, "_BaseMap", texture);
            SetTexture(material, "_MainTex", texture);
            SetColor(material, "_BaseColor", Color.white);
            SetColor(material, "_Color", Color.white);
            SetColor(material, "_EmissionColor", Color.white * 0.35f);
            SetFloat(material, "_Surface", 0f);
            SetFloat(material, "_Metallic", 0f);
            SetFloat(material, "_Smoothness", 0.35f);
            material.EnableKeyword("_EMISSION");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform AddGroup(Transform parent, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;
            return group.transform;
        }

        private static GameObject AddBox(
            string name,
            Transform parent,
            Vector3 center,
            Vector3 size,
            Material material,
            float rotationY)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = center;
            obj.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private static GameObject AddCylinder(
            string name,
            Transform parent,
            Vector3 center,
            float radius,
            float depth,
            Material material,
            Quaternion rotation)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = center;
            obj.transform.rotation = rotation;
            obj.transform.localScale = new Vector3(radius * 2f, depth * 0.5f, radius * 2f);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private static GameObject AddDisplayPlane(
            string name,
            Transform parent,
            Vector3 center,
            float width,
            float height,
            Material material)
        {
            var mesh = new Mesh();
            mesh.name = name + " Mesh";
            mesh.vertices = new[]
            {
                new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, height * 0.5f, 0f),
                new Vector3(-width * 0.5f, height * 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.RecalculateBounds();

            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = center;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            return obj;
        }

        private static void AddCornerBolts(Transform parent, Material material, Vector3 center, float z)
        {
            for (var xIndex = -1; xIndex <= 1; xIndex += 2)
            {
                for (var yIndex = -1; yIndex <= 1; yIndex += 2)
                {
                    AddCylinder(
                        "CR-07 compact frame bolt " + FormatSigned(xIndex) + " " + FormatSigned(yIndex),
                        parent,
                        new Vector3(center.x + xIndex * FrameWidth * 0.46f, center.y + yIndex * FrameHeight * 0.40f, z),
                        0.027f,
                        0.018f,
                        material,
                        Quaternion.Euler(90f, 0f, 0f));
                }
            }
        }

        private static void AddTextLabel(string name, Transform parent, string text, Vector3 center, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = center;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            var mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.09f;
            mesh.fontSize = 64;
            mesh.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void EnsureAboveMainScreen(Bounds displayBounds, Bounds mainScreenBounds)
        {
            if (displayBounds.min.y <= mainScreenBounds.max.y + 0.01f)
            {
                throw new InvalidOperationException(
                    "Approved CR-07 auxiliary screen is not above the main screen. DisplayBounds=" +
                    FormatBounds(displayBounds) +
                    "; MainScreenBounds=" +
                    FormatBounds(mainScreenBounds));
            }
        }

        private static void EnsureNoOverlap(Bounds auxBounds, Bounds protectedBounds, string protectedName)
        {
            if (auxBounds.Intersects(protectedBounds))
            {
                throw new InvalidOperationException(
                    "Approved CR-07 auxiliary screen overlaps existing " +
                    protectedName +
                    ". AuxBounds=" +
                    FormatBounds(auxBounds) +
                    "; ProtectedBounds=" +
                    FormatBounds(protectedBounds));
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

        private static List<GameObject> FindApprovedControlRoomRootsExcept(string excludedName, GameObject knownControlRoot)
        {
            var roots = new List<GameObject>();
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null ||
                    transform.parent != null ||
                    !transform.name.StartsWith("Approved Control Room ", StringComparison.Ordinal) ||
                    string.Equals(transform.name, excludedName, StringComparison.Ordinal) ||
                    transform.gameObject == knownControlRoot)
                {
                    continue;
                }

                roots.Add(transform.gameObject);
            }

            return roots;
        }

        private static List<ProtectedTransformSnapshot> CaptureProtectedSnapshots(IEnumerable<GameObject> roots)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var seen = new HashSet<Transform>();
            foreach (var root in roots)
            {
                if (root == null || !seen.Add(root.transform))
                {
                    continue;
                }

                var transforms = root.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    if (transform == null)
                    {
                        continue;
                    }

                    var renderer = transform.GetComponent<Renderer>();
                    snapshots.Add(new ProtectedTransformSnapshot(
                        root.name + "/" + GetRelativePath(root.transform, transform),
                        transform,
                        transform.localPosition,
                        transform.localRotation,
                        transform.localScale,
                        transform.gameObject.activeSelf,
                        renderer != null,
                        renderer != null && renderer.enabled,
                        renderer != null ? GetMaterialSignature(renderer) : string.Empty));
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

                var renderer = snapshot.Transform.GetComponent<Renderer>();
                if ((renderer != null) != snapshot.HadRenderer)
                {
                    throw new InvalidOperationException("Protected object renderer presence changed: " + snapshot.Path);
                }

                if (renderer == null)
                {
                    continue;
                }

                if (renderer.enabled != snapshot.RendererEnabled)
                {
                    throw new InvalidOperationException("Protected object renderer enabled state changed: " + snapshot.Path);
                }

                if (!string.Equals(GetMaterialSignature(renderer), snapshot.MaterialSignature, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Protected object material changed: " + snapshot.Path);
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

        private static Transform FindChildByName(Transform root, string objectName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == objectName)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static Transform RequireChild(Transform root, string objectName)
        {
            var found = FindChildByName(root, objectName);
            if (found == null)
            {
                throw new InvalidOperationException("Missing child object: " + objectName);
            }

            return found;
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

        private static string GetMaterialSignature(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            var builder = new StringBuilder();
            for (var i = 0; i < materials.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append("|");
                }

                if (materials[i] == null)
                {
                    builder.Append("<null>");
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(materials[i]);
                builder.Append(string.IsNullOrWhiteSpace(path) ? materials[i].GetInstanceID().ToString(CultureInfo.InvariantCulture) : path);
            }

            return builder.ToString();
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

        private static string FormatSigned(int value)
        {
            return value.ToString("+0;-0;0", CultureInfo.InvariantCulture);
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

        private readonly struct AuxScreenPlacement
        {
            public AuxScreenPlacement(Vector3 displayCenter, float mainScreenFrontZ)
            {
                DisplayCenter = displayCenter;
                MainScreenFrontZ = mainScreenFrontZ;
            }

            public Vector3 DisplayCenter { get; }
            public float MainScreenFrontZ { get; }
        }

        private readonly struct ProtectedTransformSnapshot
        {
            public ProtectedTransformSnapshot(
                string path,
                Transform transform,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                bool activeSelf,
                bool hadRenderer,
                bool rendererEnabled,
                string materialSignature)
            {
                Path = path;
                Transform = transform;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ActiveSelf = activeSelf;
                HadRenderer = hadRenderer;
                RendererEnabled = rendererEnabled;
                MaterialSignature = materialSignature;
            }

            public string Path { get; }
            public Transform Transform { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public bool ActiveSelf { get; }
            public bool HadRenderer { get; }
            public bool RendererEnabled { get; }
            public string MaterialSignature { get; }
        }

        private readonly struct AuxScreenMaterials
        {
            public AuxScreenMaterials(
                Material mount,
                Material rubber,
                Material frame,
                Material glass,
                Material screenBacking,
                Material displayTexture,
                Material bracket,
                Material socket,
                Material conduit,
                Material bolt,
                Material latch,
                Material zone,
                Material label)
            {
                Mount = mount;
                Rubber = rubber;
                Frame = frame;
                Glass = glass;
                ScreenBacking = screenBacking;
                DisplayTexture = displayTexture;
                Bracket = bracket;
                Socket = socket;
                Conduit = conduit;
                Bolt = bolt;
                Latch = latch;
                Zone = zone;
                Label = label;
            }

            public Material Mount { get; }
            public Material Rubber { get; }
            public Material Frame { get; }
            public Material Glass { get; }
            public Material ScreenBacking { get; }
            public Material DisplayTexture { get; }
            public Material Bracket { get; }
            public Material Socket { get; }
            public Material Conduit { get; }
            public Material Bolt { get; }
            public Material Latch { get; }
            public Material Zone { get; }
            public Material Label { get; }
        }
    }
}
