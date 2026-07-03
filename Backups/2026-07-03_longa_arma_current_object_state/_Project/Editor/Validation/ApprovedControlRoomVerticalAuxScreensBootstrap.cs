using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedControlRoomVerticalAuxScreensBootstrap
    {
        public const string RootName = "Approved Control Room 08 Vertical Aux Screens";

        private const string MainScreenObjectName = "CR-01 blank future main screen recessed wall bay";

        private const int PanelCount = 3;
        private const float PanelDisplayWidth = 0.20f;
        private const float PanelDisplayHeight = 2.20f;
        private const float PanelGap = 0.12f;
        private const float BankCenterOffsetFromMainCenterX = -3.58f;
        private const float BankCenterOffsetFromMainCenterY = 0.00f;
        private const float FrameWidth = PanelDisplayWidth + 0.18f;
        private const float FrameHeight = PanelDisplayHeight + 0.20f;
        private const float BankWidth = PanelCount * PanelDisplayWidth + (PanelCount - 1) * PanelGap;

        private static readonly string[] CockpitRootNames =
        {
            ApprovedCockpitStructureBootstrap.RootName,
            ApprovedCockpitWindowBootstrap.RootName,
            ApprovedCockpitConsoleBootstrap.RootName,
            ApprovedCockpitWarningBootstrap.RootName,
            ApprovedCockpitDirectionBootstrap.RootName
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Control Room 08 Vertical Aux Screens")]
        public static void EnsureApprovedControlRoomVerticalAuxScreens()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be opened.");
            }

            if (FindNamedObject(RootName) != null)
            {
                throw new InvalidOperationException(
                    "Existing CR-08 root was found and was not modified: " + RootName);
            }

            var engineRoot = RequireObject(ApprovedEngineRoomShellBootstrap.RootName);
            var cockpitRoots = FindExistingObjects(CockpitRootNames);
            if (cockpitRoots.Count == 0)
            {
                throw new InvalidOperationException("No approved cockpit roots were found.");
            }

            var controlRoot = RequireObject(ApprovedControlRoomShellBootstrap.RootName);
            var mainScreen = FindChildByName(controlRoot.transform, MainScreenObjectName);
            if (mainScreen == null)
            {
                throw new InvalidOperationException("Could not find CR-01 main screen reference object: " + MainScreenObjectName);
            }

            var cr07Root = FindNamedObject(ApprovedControlRoomAuxScreenBootstrap.RootName);
            var protectedSnapshots = CaptureExistingObjectSnapshots();
            var mainScreenBounds = GetRendererBounds(mainScreen.transform);
            var engineBounds = GetRendererBounds(engineRoot.transform);
            var cockpitBounds = GetCombinedRendererBounds(cockpitRoots);
            var cr07Bounds = cr07Root == null ? default : GetRendererBounds(cr07Root.transform);

            GameObject root = null;
            try
            {
                var materials = CreateMaterials();
                root = new GameObject(RootName);
                root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;

                var placement = CalculatePlacement(mainScreenBounds);
                BuildVerticalAuxScreens(root.transform, placement, materials);
                DisableAllColliders(root.transform);

                var cr08Bounds = GetRendererBounds(root.transform);
                EnsureLeftOfMainScreen(cr08Bounds, mainScreenBounds);
                EnsureNoOverlap(cr08Bounds, engineBounds, "engine room");
                EnsureNoOverlap(cr08Bounds, cockpitBounds, "cockpit");
                if (cr07Root != null)
                {
                    EnsureNoOverlap(cr08Bounds, cr07Bounds, "CR-07 auxiliary screen");
                }

                EnsureExistingObjectsUntouched(protectedSnapshots);

                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
                AssetDatabase.Refresh();

                Debug.Log(
                    "Approved CR-08 control room vertical auxiliary screens applied. Root=" +
                    RootName +
                    "; Center=" +
                    FormatVector(cr08Bounds.center) +
                    "; Bounds=" +
                    FormatBounds(cr08Bounds) +
                    "; Parts=" +
                    root.GetComponentsInChildren<Renderer>(true).Length +
                    "; PanelCount=" +
                    PanelCount.ToString(CultureInfo.InvariantCulture) +
                    "; PanelWidth=0.20" +
                    "; PanelHeight=2.20" +
                    "; ExistingObjectsUntouched=True" +
                    "; ControlRoomUntouched=True" +
                    "; CockpitUntouched=True" +
                    "; EngineRoomUntouched=True" +
                    "; VerticalAuxScreensLeftOfMainScreen=True" +
                    "; VerticalAuxScreensOverlapsEngineRoom=False" +
                    "; VerticalAuxScreensOverlapsCockpit=False" +
                    "; VerticalAuxScreensOverlapsCr07=False");
            }
            catch
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                throw;
            }
        }

        private static VerticalAuxScreensPlacement CalculatePlacement(Bounds mainScreenBounds)
        {
            var bankCenter = new Vector3(
                mainScreenBounds.center.x + BankCenterOffsetFromMainCenterX,
                mainScreenBounds.center.y + BankCenterOffsetFromMainCenterY,
                mainScreenBounds.min.z - 0.329f);

            return new VerticalAuxScreensPlacement(bankCenter, mainScreenBounds.min.z);
        }

        private static void BuildVerticalAuxScreens(
            Transform root,
            VerticalAuxScreensPlacement placement,
            VerticalAuxScreensMaterials materials)
        {
            var group = AddGroup(root, "CR-08 Vertical Aux Screens - individually editable");
            var bankCenter = placement.BankCenter;
            var wallZ = placement.MainScreenFrontZ;

            AddBox(
                "CR-08 left side vertical screen reserved placement outline",
                group,
                new Vector3(bankCenter.x, bankCenter.y, wallZ - 0.185f),
                new Vector3(BankWidth + 0.42f, PanelDisplayHeight + 0.46f, 0.020f),
                materials.Zone,
                0f);

            var modes = new[] { "ZONE", "CCTV", "LOCK" };
            for (var i = 0; i < PanelCount; i++)
            {
                var offset = (i - (PanelCount - 1) * 0.5f) * (PanelDisplayWidth + PanelGap);
                AddVerticalPanel(
                    group,
                    materials,
                    i,
                    "panel " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    modes[i],
                    new Vector3(bankCenter.x + offset, bankCenter.y, bankCenter.z),
                    wallZ);
            }

            AddTextLabel(
                "CR-08 placement label",
                group,
                "CR-08 세로 보조 스크린",
                new Vector3(bankCenter.x, bankCenter.y - PanelDisplayHeight * 0.5f - 0.18f, wallZ - 0.382f),
                0.070f,
                materials.Label);

            AddWallDressing(group, materials, bankCenter, wallZ);
        }

        private static void AddVerticalPanel(
            Transform parent,
            VerticalAuxScreensMaterials materials,
            int panelIndex,
            string title,
            string mode,
            Vector3 center,
            float wallZ)
        {
            AddBox(
                "CR-08 " + title + " rear mounting pad",
                parent,
                new Vector3(center.x, center.y, wallZ - 0.094f),
                new Vector3(FrameWidth + 0.16f, FrameHeight + 0.16f, 0.105f),
                materials.Mount,
                0f);
            AddBox(
                "CR-08 " + title + " black gasket",
                parent,
                new Vector3(center.x, center.y, wallZ - 0.158f),
                new Vector3(FrameWidth + 0.060f, FrameHeight + 0.060f, 0.060f),
                materials.Rubber,
                0f);
            AddBox(
                "CR-08 " + title + " vertical armored frame",
                parent,
                new Vector3(center.x, center.y, wallZ - 0.222f),
                new Vector3(FrameWidth, FrameHeight, 0.130f),
                materials.Frame,
                0f);
            AddBox(
                "CR-08 " + title + " smoked vertical glass lip",
                parent,
                new Vector3(center.x, center.y, wallZ - 0.295f),
                new Vector3(PanelDisplayWidth + 0.040f, PanelDisplayHeight + 0.050f, 0.026f),
                materials.Glass,
                0f);
            AddBox(
                "CR-08 " + title + " inactive vertical display surface",
                parent,
                new Vector3(center.x, center.y, wallZ - 0.329f),
                new Vector3(PanelDisplayWidth, PanelDisplayHeight, 0.018f),
                materials.Screen,
                0f);
            AddBox(
                "CR-08 " + title + " top header strip",
                parent,
                new Vector3(center.x, center.y + PanelDisplayHeight * 0.5f - 0.055f, wallZ - 0.360f),
                new Vector3(PanelDisplayWidth - 0.060f, 0.060f, 0.012f),
                materials.Header,
                0f);
            AddTextLabel(
                "CR-08 " + title + " title",
                parent,
                mode,
                new Vector3(center.x, center.y + PanelDisplayHeight * 0.5f - 0.055f, wallZ - 0.376f),
                0.036f,
                materials.ScreenText);

            var labels = new[] { "BRG", "CARGO", "WPN", "STORE", "ENG", "CTRL" };
            var bandMaterials = new[]
            {
                materials.Green,
                materials.Amber,
                materials.Red,
                materials.Blue,
                materials.Amber,
                materials.Green
            };

            for (var i = 0; i < labels.Length; i++)
            {
                AddZoneBand(
                    parent,
                    materials,
                    center.x,
                    center.y,
                    wallZ,
                    i,
                    labels.Length,
                    bandMaterials[(i + panelIndex) % bandMaterials.Length],
                    labels[i]);
            }

            AddCornerBolts(parent, materials.Bolt, "CR-08 " + title + " compact frame", center, wallZ - 0.345f);
            AddBox(
                "CR-08 " + title + " lower service latch",
                parent,
                new Vector3(center.x, center.y - FrameHeight * 0.49f, wallZ - 0.355f),
                new Vector3(0.22f, 0.044f, 0.012f),
                materials.Latch,
                0f);
        }

        private static void AddZoneBand(
            Transform parent,
            VerticalAuxScreensMaterials materials,
            float panelX,
            float panelY,
            float wallZ,
            int index,
            int total,
            Material colorMaterial,
            string label)
        {
            var bandHeight = (PanelDisplayHeight - 0.22f) / total;
            var top = panelY + PanelDisplayHeight * 0.5f - 0.16f;
            var y = top - bandHeight * (index + 0.5f);
            AddBox(
                "CR-08 " + label + " status color band",
                parent,
                new Vector3(panelX, y, wallZ - 0.358f),
                new Vector3(PanelDisplayWidth - 0.090f, bandHeight * 0.72f, 0.012f),
                colorMaterial,
                0f);
            AddBox(
                "CR-08 " + label + " slim divider",
                parent,
                new Vector3(panelX, y - bandHeight * 0.45f, wallZ - 0.361f),
                new Vector3(PanelDisplayWidth - 0.070f, 0.008f, 0.010f),
                materials.ScreenLine,
                0f);
            AddTextLabel(
                "CR-08 " + label + " short label",
                parent,
                label,
                new Vector3(panelX, y, wallZ - 0.374f),
                label.Length > 4 ? 0.026f : 0.032f,
                materials.ScreenText);
        }

        private static void AddCornerBolts(
            Transform parent,
            Material material,
            string prefix,
            Vector3 center,
            float z)
        {
            for (var xIndex = -1; xIndex <= 1; xIndex += 2)
            {
                for (var yIndex = -1; yIndex <= 1; yIndex += 2)
                {
                    AddCylinder(
                        prefix + " bolt " + FormatSigned(xIndex) + " " + FormatSigned(yIndex),
                        parent,
                        new Vector3(center.x + xIndex * FrameWidth * 0.42f, center.y + yIndex * FrameHeight * 0.43f, z),
                        0.018f,
                        0.016f,
                        material,
                        Quaternion.Euler(90f, 0f, 0f));
                }
            }
        }

        private static void AddWallDressing(
            Transform parent,
            VerticalAuxScreensMaterials materials,
            Vector3 bankCenter,
            float wallZ)
        {
            AddBox(
                "CR-08 left vertical screen cable raceway",
                parent,
                new Vector3(bankCenter.x, bankCenter.y + 0.98f, wallZ - 0.028f),
                new Vector3(BankWidth + 0.35f, 0.070f, 0.060f),
                materials.Conduit,
                0f);
            AddBox(
                "CR-08 shared lower service trunk",
                parent,
                new Vector3(bankCenter.x, bankCenter.y - 0.96f, wallZ - 0.030f),
                new Vector3(BankWidth + 0.20f, 0.060f, 0.055f),
                materials.Conduit,
                0f);

            var cableOffsets = new[] { -0.36f, -0.12f, 0.12f, 0.36f };
            for (var i = 0; i < cableOffsets.Length; i++)
            {
                AddCylinder(
                    "CR-08 shared cable gland " + (i + 1).ToString(CultureInfo.InvariantCulture),
                    parent,
                    new Vector3(bankCenter.x + cableOffsets[i], bankCenter.y + 0.98f, wallZ - 0.065f),
                    0.024f,
                    0.060f,
                    materials.Bolt,
                    Quaternion.Euler(90f, 0f, 0f));
            }
        }

        private static VerticalAuxScreensMaterials CreateMaterials()
        {
            return new VerticalAuxScreensMaterials(
                CreateMaterial("M_Cr08_Mount", new Color(0.21f, 0.24f, 0.22f, 1f), 0.22f, 0.16f, false),
                CreateMaterial("M_Cr08_Rubber", new Color(0.010f, 0.012f, 0.012f, 1f), 0.0f, 0.06f, false),
                CreateMaterial("M_Cr08_Frame", new Color(0.29f, 0.32f, 0.29f, 1f), 0.24f, 0.18f, false),
                CreateMaterial("M_Cr08_GlassLip", new Color(0.010f, 0.020f, 0.024f, 1f), 0.0f, 0.55f, false),
                CreateMaterial("M_Cr08_Screen", new Color(0.010f, 0.048f, 0.052f, 1f), 0.0f, 0.52f, true),
                CreateMaterial("M_Cr08_ScreenLine", new Color(0.18f, 0.72f, 0.70f, 1f), 0.0f, 0.34f, true),
                CreateMaterial("M_Cr08_ScreenText", new Color(0.74f, 0.92f, 0.88f, 1f), 0.0f, 0.30f, true),
                CreateMaterial("M_Cr08_Header", new Color(0.25f, 0.45f, 0.38f, 1f), 0.0f, 0.34f, true),
                CreateMaterial("M_Cr08_Green", new Color(0.09f, 0.78f, 0.47f, 1f), 0.0f, 0.34f, true),
                CreateMaterial("M_Cr08_Amber", new Color(0.95f, 0.54f, 0.13f, 1f), 0.0f, 0.30f, true),
                CreateMaterial("M_Cr08_Red", new Color(0.90f, 0.18f, 0.14f, 1f), 0.0f, 0.30f, true),
                CreateMaterial("M_Cr08_Blue", new Color(0.12f, 0.42f, 0.90f, 1f), 0.0f, 0.30f, true),
                CreateMaterial("M_Cr08_Bolt", new Color(0.36f, 0.36f, 0.31f, 1f), 0.30f, 0.25f, false),
                CreateMaterial("M_Cr08_Latch", new Color(0.38f, 0.40f, 0.34f, 1f), 0.24f, 0.24f, false),
                CreateMaterial("M_Cr08_Zone", new Color(0.95f, 0.50f, 0.10f, 1f), 0.0f, 0.28f, true),
                CreateMaterial("M_Cr08_Conduit", new Color(0.045f, 0.055f, 0.055f, 1f), 0.28f, 0.20f, false),
                CreateMaterial("M_Cr08_Label", new Color(0.78f, 0.88f, 0.84f, 1f), 0.0f, 0.30f, true));
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, bool emissive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name,
                color = color
            };

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

        private static void AddTextLabel(
            string name,
            Transform parent,
            string text,
            Vector3 center,
            float characterSize,
            Material material)
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
            mesh.characterSize = characterSize;
            mesh.fontSize = 64;
            mesh.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void EnsureLeftOfMainScreen(Bounds cr08Bounds, Bounds mainScreenBounds)
        {
            if (cr08Bounds.center.x >= mainScreenBounds.center.x ||
                cr08Bounds.max.x >= mainScreenBounds.center.x)
            {
                throw new InvalidOperationException(
                    "Approved CR-08 vertical auxiliary screens are not on the left side of the main screen. CR08Bounds=" +
                    FormatBounds(cr08Bounds) +
                    "; MainScreenBounds=" +
                    FormatBounds(mainScreenBounds));
            }
        }

        private static void EnsureNoOverlap(Bounds newBounds, Bounds protectedBounds, string protectedName)
        {
            if (newBounds.Intersects(protectedBounds))
            {
                throw new InvalidOperationException(
                    "Approved CR-08 vertical auxiliary screens overlap existing " +
                    protectedName +
                    ". CR08Bounds=" +
                    FormatBounds(newBounds) +
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

        private static List<ProtectedObjectSnapshot> CaptureExistingObjectSnapshots()
        {
            var snapshots = new List<ProtectedObjectSnapshot>();
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                var renderer = transform.GetComponent<Renderer>();
                snapshots.Add(new ProtectedObjectSnapshot(
                    GetFullPath(transform),
                    transform,
                    transform.name,
                    transform.parent,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf,
                    GetComponentSignature(transform.gameObject),
                    renderer != null,
                    renderer != null && renderer.enabled,
                    renderer != null ? GetMaterialSignature(renderer) : string.Empty));
            }

            return snapshots;
        }

        private static void EnsureExistingObjectsUntouched(IReadOnlyList<ProtectedObjectSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Protected object was removed: " + snapshot.Path);
                }

                if (!string.Equals(snapshot.Transform.name, snapshot.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Protected object name changed: " + snapshot.Path);
                }

                if (snapshot.Transform.parent != snapshot.Parent)
                {
                    throw new InvalidOperationException("Protected object parent changed: " + snapshot.Path);
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

                if (!string.Equals(GetComponentSignature(snapshot.Transform.gameObject), snapshot.ComponentSignature, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Protected object component list changed: " + snapshot.Path);
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

        private static string GetFullPath(Transform transform)
        {
            var segments = new List<string>();
            var current = transform;
            while (current != null)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static string GetComponentSignature(GameObject obj)
        {
            var components = obj.GetComponents<Component>();
            var builder = new StringBuilder();
            for (var i = 0; i < components.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append("|");
                }

                builder.Append(components[i] == null ? "<missing>" : components[i].GetType().FullName);
            }

            return builder.ToString();
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

        private readonly struct VerticalAuxScreensPlacement
        {
            public VerticalAuxScreensPlacement(Vector3 bankCenter, float mainScreenFrontZ)
            {
                BankCenter = bankCenter;
                MainScreenFrontZ = mainScreenFrontZ;
            }

            public Vector3 BankCenter { get; }
            public float MainScreenFrontZ { get; }
        }

        private readonly struct ProtectedObjectSnapshot
        {
            public ProtectedObjectSnapshot(
                string path,
                Transform transform,
                string name,
                Transform parent,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                bool activeSelf,
                string componentSignature,
                bool hadRenderer,
                bool rendererEnabled,
                string materialSignature)
            {
                Path = path;
                Transform = transform;
                Name = name;
                Parent = parent;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ActiveSelf = activeSelf;
                ComponentSignature = componentSignature;
                HadRenderer = hadRenderer;
                RendererEnabled = rendererEnabled;
                MaterialSignature = materialSignature;
            }

            public string Path { get; }
            public Transform Transform { get; }
            public string Name { get; }
            public Transform Parent { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public bool ActiveSelf { get; }
            public string ComponentSignature { get; }
            public bool HadRenderer { get; }
            public bool RendererEnabled { get; }
            public string MaterialSignature { get; }
        }

        private readonly struct VerticalAuxScreensMaterials
        {
            public VerticalAuxScreensMaterials(
                Material mount,
                Material rubber,
                Material frame,
                Material glass,
                Material screen,
                Material screenLine,
                Material screenText,
                Material header,
                Material green,
                Material amber,
                Material red,
                Material blue,
                Material bolt,
                Material latch,
                Material zone,
                Material conduit,
                Material label)
            {
                Mount = mount;
                Rubber = rubber;
                Frame = frame;
                Glass = glass;
                Screen = screen;
                ScreenLine = screenLine;
                ScreenText = screenText;
                Header = header;
                Green = green;
                Amber = amber;
                Red = red;
                Blue = blue;
                Bolt = bolt;
                Latch = latch;
                Zone = zone;
                Conduit = conduit;
                Label = label;
            }

            public Material Mount { get; }
            public Material Rubber { get; }
            public Material Frame { get; }
            public Material Glass { get; }
            public Material Screen { get; }
            public Material ScreenLine { get; }
            public Material ScreenText { get; }
            public Material Header { get; }
            public Material Green { get; }
            public Material Amber { get; }
            public Material Red { get; }
            public Material Blue { get; }
            public Material Bolt { get; }
            public Material Latch { get; }
            public Material Zone { get; }
            public Material Conduit { get; }
            public Material Label { get; }
        }
    }
}
