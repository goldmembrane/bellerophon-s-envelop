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
    public static class ApprovedControlRoomDirectionLabelsBootstrap
    {
        public const string RootName = "Approved Control Room 17 Direction Labels";

        private const string MaterialFolder = "Assets/_Project/Art/Ship/ControlRoom";

        private static readonly string[] CockpitRootNames =
        {
            ApprovedCockpitStructureBootstrap.RootName,
            ApprovedCockpitWindowBootstrap.RootName,
            ApprovedCockpitConsoleBootstrap.RootName,
            ApprovedCockpitWarningBootstrap.RootName,
            ApprovedCockpitDirectionBootstrap.RootName
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Control Room 17 Direction Labels")]
        public static void EnsureApprovedControlRoomDirectionLabels()
        {
            var scene = OpenOrUseCargoRunScene();
            if (!scene.IsValid())
            {
                throw new InvalidOperationException("CargoRunMvp scene could not be opened.");
            }

            var controlRoot = RequireObject(ApprovedControlRoomShellBootstrap.RootName);
            var engineRoot = RequireObject(ApprovedEngineRoomShellBootstrap.RootName);
            var cockpitRoots = FindExistingObjects(CockpitRootNames);
            if (cockpitRoots.Count == 0)
            {
                throw new InvalidOperationException("No approved cockpit roots were found.");
            }

            var existingRoot = FindNamedObject(RootName);
            var protectedSnapshots = CaptureExistingObjectSnapshots(existingRoot == null ? null : existingRoot.transform);
            var engineBounds = GetRendererBounds(engineRoot.transform);
            var cockpitBounds = GetCombinedRendererBounds(cockpitRoots);

            GameObject root = null;
            try
            {
                var materials = CreateMaterials();
                root = new GameObject(RootName + " Rebuild Pending");
                root.transform.position = controlRoot.transform.position;
                root.transform.rotation = controlRoot.transform.rotation;
                root.transform.localScale = controlRoot.transform.localScale;

                var textFitRecords = new List<TextFitRecord>();
                BuildDirectionLabels(root.transform, materials, textFitRecords);
                DisableAllColliders(root.transform);

                var labelBounds = GetRendererBounds(root.transform);
                var visibleTextMeshes = EnsureVisibleTextMeshes(root.transform);
                var fittedTextMeshes = EnsureTextFitsLabelPanels(textFitRecords);
                EnsureNoOverlap(labelBounds, engineBounds, "engine room");
                EnsureNoOverlap(labelBounds, cockpitBounds, "cockpit");
                EnsureExistingObjectsUntouched(protectedSnapshots);

                if (existingRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(existingRoot);
                }

                root.name = RootName;
                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);

                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "Approved CR-17 control room direction labels applied. Root=" +
                    RootName +
                    "; Center=" +
                    FormatVector(labelBounds.center) +
                    "; Bounds=" +
                    FormatBounds(labelBounds) +
                    "; Parts=" +
                    root.GetComponentsInChildren<Renderer>(true).Length.ToString(CultureInfo.InvariantCulture) +
                    "; LabelCount=4" +
                    "; VisibleTextMeshes=" +
                    visibleTextMeshes.ToString(CultureInfo.InvariantCulture) +
                    "; TextFitChecked=" +
                    fittedTextMeshes.ToString(CultureInfo.InvariantCulture) +
                    "; TextFitsLabelPanels=True" +
                    "; EnglishMainLabels=True" +
                    "; ExistingCr17Rebuilt=" +
                    (existingRoot != null ? "True" : "False") +
                    "; ExistingObjectsUntouched=True" +
                    "; ControlRoomUntouched=True" +
                    "; CockpitUntouched=True" +
                    "; EngineRoomUntouched=True" +
                    "; DirectionLabelsOverlapsEngineRoom=False" +
                    "; DirectionLabelsOverlapsCockpit=False");
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

        private static Scene OpenOrUseCargoRunScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            var activePath = activeScene.path.Replace('\\', '/');
            var cargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\', '/');
            if (activeScene.IsValid() &&
                string.Equals(activePath, cargoPath, StringComparison.OrdinalIgnoreCase))
            {
                return activeScene;
            }

            return EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
        }

        private static void BuildDirectionLabels(
            Transform root,
            DirectionLabelMaterials materials,
            List<TextFitRecord> textFitRecords)
        {
            var labels = AddGroup(root, "CR-17 Corridor Direction Labels - individually editable");
            var arrows = AddGroup(root, "CR-17 Floor Direction Arrows - individually editable");
            var relation = AddGroup(root, "CR-17 Layout Relation Markers - individually editable");

            AddLabelPanel(
                labels,
                "CR-17 cockpit direction",
                "COCKPIT",
                "조종실",
                new Vector3(-3.70f, 1.36f, -0.92f),
                1.15f,
                140f,
                materials.CockpitBlue,
                materials,
                textFitRecords);
            AddLabelPanel(
                labels,
                "CR-17 engine room direction",
                "ENGINE ROOM",
                "동력실",
                new Vector3(-3.70f, 1.36f, -3.00f),
                1.15f,
                180f,
                materials.EngineAmber,
                materials,
                textFitRecords);
            AddLabelPanel(
                labels,
                "CR-17 cargo hold direction",
                "CARGO HOLD",
                "이송창고",
                new Vector3(-0.72f, 1.30f, -4.46f),
                1.20f,
                270f,
                materials.CargoGreen,
                materials,
                textFitRecords);
            AddLabelPanel(
                labels,
                "CR-17 armory direction",
                "ARMORY",
                "무기실",
                new Vector3(0.72f, 1.30f, -4.46f),
                1.20f,
                270f,
                materials.ArmoryRed,
                materials,
                textFitRecords);

            AddFloorArrow(arrows, "CR-17 cockpit floor arrow", new Vector3(-3.70f, 0.05f, -1.54f), 140f, materials.CockpitBlue);
            AddFloorArrow(arrows, "CR-17 engine room floor arrow", new Vector3(-3.70f, 0.05f, -3.62f), 180f, materials.EngineAmber);
            AddFloorArrow(arrows, "CR-17 cargo hold floor arrow", new Vector3(-0.72f, 0.05f, -5.08f), 270f, materials.CargoGreen);
            AddFloorArrow(arrows, "CR-17 armory floor arrow", new Vector3(0.72f, 0.05f, -5.08f), 270f, materials.ArmoryRed);

            AddBox(
                "CR-17 left corridor separated relation marker",
                relation,
                new Vector3(-4.05f, 0.04f, -1.96f),
                new Vector3(0.06f, 0.035f, 1.24f),
                materials.RelationMarker,
                Quaternion.identity);
            AddBox(
                "CR-17 south corridor adjacent relation marker",
                relation,
                new Vector3(-0.03f, 0.04f, -4.55f),
                new Vector3(1.62f, 0.035f, 0.055f),
                materials.RelationMarker,
                Quaternion.identity);
        }

        private static void AddLabelPanel(
            Transform parent,
            string prefix,
            string mainText,
            string subText,
            Vector3 center,
            float panelWidth,
            float arrowAngleDegrees,
            Material accentMaterial,
            DirectionLabelMaterials materials,
            List<TextFitRecord> textFitRecords)
        {
            AddBox(
                prefix + " wall label panel",
                parent,
                center,
                new Vector3(panelWidth, 0.44f, 0.08f),
                materials.DarkPanel,
                Quaternion.identity);
            AddBox(
                prefix + " top accent trim",
                parent,
                center + new Vector3(0f, 0.245f, -0.047f),
                new Vector3(panelWidth + 0.08f, 0.035f, 0.025f),
                accentMaterial,
                Quaternion.identity);
            AddBox(
                prefix + " bottom accent trim",
                parent,
                center + new Vector3(0f, -0.245f, -0.047f),
                new Vector3(panelWidth + 0.08f, 0.035f, 0.025f),
                accentMaterial,
                Quaternion.identity);
            AddBox(
                prefix + " left bracket",
                parent,
                center + new Vector3(-panelWidth * 0.5f - 0.065f, 0f, -0.045f),
                new Vector3(0.05f, 0.38f, 0.035f),
                materials.Bracket,
                Quaternion.identity);
            AddBox(
                prefix + " right bracket",
                parent,
                center + new Vector3(panelWidth * 0.5f + 0.065f, 0f, -0.045f),
                new Vector3(0.05f, 0.38f, 0.035f),
                materials.Bracket,
                Quaternion.identity);

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    AddCylinder(
                        prefix + " bolt " + FormatSigned(x) + " " + FormatSigned(y),
                        parent,
                        center + new Vector3(x * (panelWidth * 0.5f - 0.08f), y * 0.16f, -0.091f),
                        0.022f,
                        0.018f,
                        materials.Bolt,
                        Quaternion.Euler(90f, 0f, 0f));
                }
            }

            var mainSize = mainText.Length <= 7 ? 0.13f : 0.095f;
            AddTextLabel(
                prefix + " main english label",
                parent,
                mainText,
                center + new Vector3(0f, 0.075f, -0.098f),
                mainSize,
                materials.MainText,
                panelWidth - 0.14f,
                0.150f,
                textFitRecords);
            AddTextLabel(
                prefix + " korean support label",
                parent,
                subText,
                center + new Vector3(0f, -0.112f, -0.101f),
                0.064f,
                materials.SubText,
                panelWidth - 0.20f,
                0.130f,
                textFitRecords);
            AddBox(
                prefix + " direction color chip",
                parent,
                center + new Vector3(-panelWidth * 0.5f + 0.085f, 0f, -0.103f),
                new Vector3(0.055f, 0.23f, 0.014f),
                accentMaterial,
                Quaternion.identity);
            AddFloorArrow(parent, prefix + " mini direction cue", center + new Vector3(panelWidth * 0.5f - 0.12f, -0.145f, -0.108f), arrowAngleDegrees, accentMaterial, 0.23f, 0.035f);
        }

        private static void AddFloorArrow(
            Transform parent,
            string prefix,
            Vector3 center,
            float angleDegrees,
            Material material,
            float length = 0.72f,
            float width = 0.18f)
        {
            var angle = angleDegrees * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var tip = center + direction * (length * 0.34f);
            AddBox(
                prefix + " shaft",
                parent,
                center - direction * (length * 0.12f),
                new Vector3(length * 0.58f, 0.028f, width),
                material,
                Quaternion.Euler(0f, -angleDegrees, 0f));
            AddBox(
                prefix + " left chevron",
                parent,
                tip - direction * (width * 0.48f),
                new Vector3(width * 1.85f, 0.030f, width * 0.34f),
                material,
                Quaternion.Euler(0f, -(angleDegrees + 35f), 0f));
            AddBox(
                prefix + " right chevron",
                parent,
                tip - direction * (width * 0.48f),
                new Vector3(width * 1.85f, 0.030f, width * 0.34f),
                material,
                Quaternion.Euler(0f, -(angleDegrees - 35f), 0f));
        }

        private static DirectionLabelMaterials CreateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                throw new InvalidOperationException("Missing control room material folder: " + MaterialFolder);
            }

            return new DirectionLabelMaterials(
                CreateMaterialAsset("M_Cr17_DarkPanel", new Color(0.035f, 0.041f, 0.047f, 1f), 0.32f, 0.48f, false),
                CreateMaterialAsset("M_Cr17_Bracket", new Color(0.16f, 0.17f, 0.17f, 1f), 0.55f, 0.38f, false),
                CreateMaterialAsset("M_Cr17_Bolt", new Color(0.035f, 0.037f, 0.038f, 1f), 0.80f, 0.30f, false),
                CreateFontMaterialAsset("M_Cr17_MainText", new Color(0.92f, 0.97f, 1.00f, 1f)),
                CreateFontMaterialAsset("M_Cr17_SubText", new Color(0.62f, 0.68f, 0.72f, 1f)),
                CreateMaterialAsset("M_Cr17_CockpitBlue", new Color(0.12f, 0.45f, 0.95f, 1f), 0.18f, 0.36f, true),
                CreateMaterialAsset("M_Cr17_EngineAmber", new Color(0.95f, 0.47f, 0.08f, 1f), 0.18f, 0.36f, true),
                CreateMaterialAsset("M_Cr17_CargoGreen", new Color(0.12f, 0.65f, 0.38f, 1f), 0.18f, 0.36f, true),
                CreateMaterialAsset("M_Cr17_ArmoryRed", new Color(0.82f, 0.12f, 0.12f, 1f), 0.18f, 0.36f, true),
                CreateMaterialAsset("M_Cr17_RelationMarker", new Color(0.92f, 0.78f, 0.36f, 1f), 0.05f, 0.46f, true));
        }

        private static Material CreateMaterialAsset(string name, Color color, float metallic, float smoothness, bool emissive)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
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

        private static Material CreateFontMaterialAsset(string name, Color color)
        {
            var font = GetRuntimeFont();
            var sourceMaterial = font.material;
            if (sourceMaterial == null || sourceMaterial.mainTexture == null)
            {
                throw new InvalidOperationException("Runtime font material is missing its atlas texture.");
            }

            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(sourceMaterial)
                {
                    name = name
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = sourceMaterial.shader;
            material.mainTexture = sourceMaterial.mainTexture;
            material.color = color;
            SetColor(material, "_Color", color);
            SetColor(material, "_BaseColor", color);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Font GetRuntimeFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (font == null)
            {
                throw new InvalidOperationException("Unity runtime font was not found for CR-17 direction labels.");
            }

            return font;
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
            Quaternion rotation)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = center;
            obj.transform.localRotation = rotation;
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
            obj.transform.localPosition = center;
            obj.transform.localRotation = rotation;
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
            Material material,
            float maxWidth,
            float maxHeight,
            List<TextFitRecord> textFitRecords)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = center;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            var mesh = obj.AddComponent<TextMesh>();
            mesh.font = GetRuntimeFont();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = characterSize;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.color = material.color;
            var renderer = mesh.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 20;
            FitTextMeshToBox(mesh, maxWidth, maxHeight);
            textFitRecords.Add(new TextFitRecord(mesh, maxWidth, maxHeight));
        }

        private static void FitTextMeshToBox(TextMesh mesh, float maxWidth, float maxHeight)
        {
            const float minCharacterSize = 0.018f;
            var renderer = mesh.GetComponent<Renderer>();
            mesh.font.RequestCharactersInTexture(mesh.text, mesh.fontSize, mesh.fontStyle);

            for (var i = 0; i < 8; i++)
            {
                var width = GetLocalRendererWidth(renderer);
                var height = GetLocalRendererHeight(renderer);
                if (width <= 0f || height <= 0f)
                {
                    return;
                }

                var widthRatio = maxWidth / width;
                var heightRatio = maxHeight / height;
                var ratio = Mathf.Min(widthRatio, heightRatio);
                if (ratio >= 0.995f)
                {
                    return;
                }

                mesh.characterSize = Mathf.Max(minCharacterSize, mesh.characterSize * ratio * 0.96f);
            }
        }

        private static int EnsureVisibleTextMeshes(Transform root)
        {
            var textMeshes = root.GetComponentsInChildren<TextMesh>(true);
            if (textMeshes.Length < 8)
            {
                throw new InvalidOperationException(
                    "CR-17 direction labels have too few text meshes. Count=" +
                    textMeshes.Length.ToString(CultureInfo.InvariantCulture));
            }

            for (var i = 0; i < textMeshes.Length; i++)
            {
                var textMesh = textMeshes[i];
                if (textMesh == null || string.IsNullOrWhiteSpace(textMesh.text))
                {
                    throw new InvalidOperationException("CR-17 direction label text mesh is empty.");
                }

                var renderer = textMesh.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled)
                {
                    throw new InvalidOperationException("CR-17 direction label renderer is missing or disabled: " + textMesh.name);
                }

                var material = renderer.sharedMaterial;
                if (material == null || material.mainTexture == null)
                {
                    throw new InvalidOperationException("CR-17 direction label font material is missing atlas texture: " + textMesh.name);
                }
            }

            return textMeshes.Length;
        }

        private static int EnsureTextFitsLabelPanels(IReadOnlyList<TextFitRecord> records)
        {
            if (records.Count < 8)
            {
                throw new InvalidOperationException(
                    "CR-17 direction label text fit records are incomplete. Count=" +
                    records.Count.ToString(CultureInfo.InvariantCulture));
            }

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record.TextMesh == null)
                {
                    throw new InvalidOperationException("CR-17 direction label text fit record has no TextMesh.");
                }

                var renderer = record.TextMesh.GetComponent<Renderer>();
                var width = GetLocalRendererWidth(renderer);
                var height = GetLocalRendererHeight(renderer);
                if (width > record.MaxWidth + 0.006f ||
                    height > record.MaxHeight + 0.006f)
                {
                    throw new InvalidOperationException(
                        "CR-17 direction label text does not fit its panel: " +
                        record.TextMesh.name +
                        "; Text=" +
                        record.TextMesh.text +
                        "; Width=" +
                        width.ToString("0.000", CultureInfo.InvariantCulture) +
                        "; MaxWidth=" +
                        record.MaxWidth.ToString("0.000", CultureInfo.InvariantCulture) +
                        "; Height=" +
                        height.ToString("0.000", CultureInfo.InvariantCulture) +
                        "; MaxHeight=" +
                        record.MaxHeight.ToString("0.000", CultureInfo.InvariantCulture));
                }
            }

            return records.Count;
        }

        private static float GetLocalRendererWidth(Renderer renderer)
        {
            if (renderer == null)
            {
                return 0f;
            }

            var scale = Mathf.Max(0.0001f, Mathf.Abs(renderer.transform.lossyScale.x));
            return renderer.bounds.size.x / scale;
        }

        private static float GetLocalRendererHeight(Renderer renderer)
        {
            if (renderer == null)
            {
                return 0f;
            }

            var scale = Mathf.Max(0.0001f, Mathf.Abs(renderer.transform.lossyScale.y));
            return renderer.bounds.size.y / scale;
        }

        private static void EnsureNoOverlap(Bounds newBounds, Bounds protectedBounds, string protectedName)
        {
            if (newBounds.Intersects(protectedBounds))
            {
                throw new InvalidOperationException(
                    "Approved CR-17 direction labels overlap existing " +
                    protectedName +
                    ". LabelBounds=" +
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

        private static List<ProtectedObjectSnapshot> CaptureExistingObjectSnapshots(Transform excludedRoot)
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

                if (excludedRoot != null &&
                    (transform == excludedRoot || transform.IsChildOf(excludedRoot)))
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

        private readonly struct TextFitRecord
        {
            public TextFitRecord(TextMesh textMesh, float maxWidth, float maxHeight)
            {
                TextMesh = textMesh;
                MaxWidth = maxWidth;
                MaxHeight = maxHeight;
            }

            public TextMesh TextMesh { get; }
            public float MaxWidth { get; }
            public float MaxHeight { get; }
        }

        private readonly struct DirectionLabelMaterials
        {
            public DirectionLabelMaterials(
                Material darkPanel,
                Material bracket,
                Material bolt,
                Material mainText,
                Material subText,
                Material cockpitBlue,
                Material engineAmber,
                Material cargoGreen,
                Material armoryRed,
                Material relationMarker)
            {
                DarkPanel = darkPanel;
                Bracket = bracket;
                Bolt = bolt;
                MainText = mainText;
                SubText = subText;
                CockpitBlue = cockpitBlue;
                EngineAmber = engineAmber;
                CargoGreen = cargoGreen;
                ArmoryRed = armoryRed;
                RelationMarker = relationMarker;
            }

            public Material DarkPanel { get; }
            public Material Bracket { get; }
            public Material Bolt { get; }
            public Material MainText { get; }
            public Material SubText { get; }
            public Material CockpitBlue { get; }
            public Material EngineAmber { get; }
            public Material CargoGreen { get; }
            public Material ArmoryRed { get; }
            public Material RelationMarker { get; }
        }
    }
}
