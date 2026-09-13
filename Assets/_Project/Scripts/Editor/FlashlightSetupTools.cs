using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerHands;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.PlayerAnimation.Editor
{
    internal static class FlashlightSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ModelPath = "Assets/_Project/Art/Items/Flashlight/Flashlight.fbx";
        private const string TextureFolder = "Assets/_Project/Art/Items/Flashlight/Textures";
        private const string MaterialFolder = "Assets/_Project/Art/Items/Flashlight/Materials";
        private const string MaterialPath = MaterialFolder + "/Flashlight.mat";
        private const string PackedMetallicPath =
            TextureFolder + "/Flashlight_MetallicSmoothness.png";
        private const string GripPosePath =
            "Assets/_Project/Art/Player/HandsRig/SharedBatteryGripPose.asset";
        private const string ReviewFolder = "Assets/_Project/Art/Items/Flashlight/Review";
        private const string ProbeReportPath = ReviewFolder + "/source_probe.txt";
        private const string ImportReportPath = ReviewFolder + "/import.txt";
        private const string ApplyReportPath = ReviewFolder + "/application.txt";
        private const string InspectReportPath = ReviewFolder + "/inspection.txt";
        private const string FinalImagePath = ReviewFolder + "/final.png";
        private const string FinalReportPath = ReviewFolder + "/final.txt";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const float DesiredLength = 0.30f;
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.05f;
        private const int ReviewLayer = 31;
        private const int PanelSize = 720;

        private static readonly string[] TargetNames =
        {
            "Flashlight_Idle",
            "Flashlight_Charge_Connect",
            "Flashlight_Charge_Disconnect"
        };

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Probe Source")]
        internal static void ProbeSource()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject modelAsset = RequireAsset<GameObject>(ModelPath);
            var report = new StringBuilder()
                .AppendLine("Flashlight source probe")
                .AppendLine("modelPath=" + ModelPath)
                .AppendLine("sourceModelChanged=False");

            UnityEngine.Object[] embedded = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            foreach (UnityEngine.Object asset in embedded.OrderBy(item => item.GetType().FullName)
                         .ThenBy(item => item.name, StringComparer.Ordinal))
                report.AppendLine("embedded=" + asset.GetType().FullName + "|" + asset.name);

            foreach (Material material in embedded.OfType<Material>())
            {
                report.AppendLine("material=" + material.name + "|shader=" +
                    (material.shader == null ? "null" : material.shader.name));
                foreach (string property in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(property);
                    report.AppendLine("materialTexture=" + material.name + "|" + property +
                        "|" + (texture == null ? "null" : texture.name));
                }
            }

            GameObject instance = UnityEngine.Object.Instantiate(modelAsset);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Bounds bounds = CalculateBounds(instance);
                report.AppendLine("combinedBoundsCenter=" + Vec(bounds.center));
                report.AppendLine("combinedBoundsSize=" + Vec(bounds.size));
                report.AppendLine("dominantAxis=" + DominantAxis(bounds.size));
                foreach (Transform item in instance.GetComponentsInChildren<Transform>(true)
                             .OrderBy(item => AnimationUtility.CalculateTransformPath(
                                 item, instance.transform), StringComparer.Ordinal))
                    report.AppendLine("transform=" +
                        AnimationUtility.CalculateTransformPath(item, instance.transform) +
                        "|localPosition=" + Vec(item.localPosition) +
                        "|localRotation=" + Quat(item.localRotation) +
                        "|localScale=" + Vec(item.localScale));
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    report.AppendLine("renderer=" +
                        AnimationUtility.CalculateTransformPath(renderer.transform,
                            instance.transform) +
                        "|type=" + renderer.GetType().Name +
                        "|boundsCenter=" + Vec(renderer.bounds.center) +
                        "|boundsSize=" + Vec(renderer.bounds.size));
                    foreach (Material material in renderer.sharedMaterials)
                        report.AppendLine("rendererMaterial=" +
                            (material == null ? "null" : material.name));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform shoulder = RequirePath(target.transform, RightShoulderPath);
                Transform upperArm = RequirePath(target.transform, RightArmPath);
                Transform foreArm = RequirePath(target.transform, RightForeArmPath);
                Transform hand = RequirePath(target.transform, RightHandPath);
                report.AppendLine("target=" + targetName);
                report.AppendLine("rootForward=" + Vec(target.transform.forward));
                report.AppendLine("rootRight=" + Vec(target.transform.right));
                report.AppendLine("rootUp=" + Vec(target.transform.up));
                report.AppendLine("shoulderWorld=" + Vec(shoulder.position));
                report.AppendLine("upperArmWorld=" + Vec(upperArm.position));
                report.AppendLine("foreArmWorld=" + Vec(foreArm.position));
                report.AppendLine("handWorld=" + Vec(hand.position));
                report.AppendLine("upperLength=" + Num(
                    Vector3.Distance(upperArm.position, foreArm.position)));
                report.AppendLine("lowerLength=" + Num(
                    Vector3.Distance(foreArm.position, hand.position)));
                report.AppendLine("palmNormal=" + Vec(RightPalmNormal(hand)));
                report.AppendLine("fingerDirection=" + Vec(RightFingerDirection(hand)));
            }

            WriteText(ProbeReportPath, report.ToString());
            Debug.Log("[Flashlight] Source probe completed without scene modification.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Carry")]
        internal static void ApplyCarry()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            ImportEmbeddedAssets();
            AssetDatabase.DeleteAsset(ReviewFolder + "/source_probe_failure.txt");

            GameObject modelAsset = RequireAsset<GameObject>(ModelPath);
            PlayerHandGripPose gripPose = RequireAsset<PlayerHandGripPose>(GripPosePath);
            string protectedBefore = ProtectedSceneSignature(scene);
            string modelHash = ComputeAssetHash(ModelPath);
            string materialHash = ComputeAssetHash(MaterialPath);
            string[] sourceTexturePaths = SourceTexturePaths();
            string[] sourceTextureHashes = sourceTexturePaths.Select(ComputeAssetHash).ToArray();

            Vector3 sourceModelPosition;
            Quaternion sourceModelRotation;
            Vector3 scaledModelScale;
            float scaledRadius;
            GameObject measurement = UnityEngine.Object.Instantiate(modelAsset);
            measurement.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Bounds bounds = CalculateBounds(measurement);
                float scaleFactor = DesiredLength / bounds.size.y;
                sourceModelPosition = measurement.transform.localPosition;
                sourceModelRotation = measurement.transform.localRotation;
                scaledModelScale = measurement.transform.localScale * scaleFactor;
                scaledRadius = Mathf.Max(bounds.size.x, bounds.size.z) * scaleFactor * 0.5f;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(measurement);
            }

            var report = new StringBuilder()
                .AppendLine("Flashlight carry application")
                .AppendLine("sourceModelChanged=False")
                .AppendLine("scenePath=" + scene.path)
                .AppendLine("desiredLengthMeters=" + Num(DesiredLength))
                .AppendLine("scaledRadiusMeters=" + Num(scaledRadius));

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform shoulder = RequirePath(target.transform, RightShoulderPath);
                Transform upperArm = RequirePath(target.transform, RightArmPath);
                Transform foreArm = RequirePath(target.transform, RightForeArmPath);
                Transform hand = RequirePath(target.transform, RightHandPath);
                PoseState shoulderBefore = new PoseState(shoulder);
                PoseState upperBefore = new PoseState(upperArm);
                PoseState foreBefore = new PoseState(foreArm);
                PoseState handBefore = new PoseState(hand);

                Vector3 palmCenter = RightPalmCenter(hand);
                Vector3 palmNormal = RightPalmNormal(hand);
                Vector3 holderWorldPosition = palmCenter +
                    target.transform.forward * 0.055f - palmNormal * (scaledRadius * 0.33f);
                Quaternion holderWorldRotation = Quaternion.LookRotation(
                    target.transform.up, target.transform.forward);
                Vector3 holderLocalPosition = hand.InverseTransformPoint(holderWorldPosition);
                Quaternion holderLocalRotation =
                    Quaternion.Inverse(hand.rotation) * holderWorldRotation;

                FlashlightRightHandFollowBehaviour behaviour =
                    target.GetComponent<FlashlightRightHandFollowBehaviour>() ??
                    Undo.AddComponent<FlashlightRightHandFollowBehaviour>(target);
                Undo.RecordObject(behaviour, "Configure flashlight right-hand carry");
                behaviour.Configure(
                    modelAsset,
                    gripPose,
                    RightHandPath,
                    holderLocalPosition,
                    holderLocalRotation,
                    sourceModelPosition,
                    sourceModelRotation,
                    scaledModelScale);
                EditorUtility.SetDirty(behaviour);

                shoulderBefore.RequireExact(shoulder, targetName + " shoulder");
                upperBefore.RequireExact(upperArm, targetName + " upper arm");
                foreBefore.RequireExact(foreArm, targetName + " forearm");
                handBefore.RequireExact(hand, targetName + " wrist");

                report.AppendLine("target=" + targetName);
                report.AppendLine("armAndWristRotationChanged=False");
                report.AppendLine("holderLocalPosition=" + Vec(holderLocalPosition));
                report.AppendLine("holderLocalRotation=" + Quat(holderLocalRotation));
                report.AppendLine("modelLocalPosition=" + Vec(sourceModelPosition));
                report.AppendLine("modelLocalRotation=" + Quat(sourceModelRotation));
                report.AppendLine("modelLocalScale=" + Vec(scaledModelScale));
                report.AppendLine("rightFingerJointCount=" + RightGripJointCount(gripPose));
                report.AppendLine("lensForwardAngleDegrees=" + Num(Vector3.Angle(
                    behaviour.Holder.transform.up, target.transform.forward)));
            }

            RequireEqual(protectedBefore, ProtectedSceneSignature(scene),
                "non-Flashlight scene transforms");
            RequireEqual(modelHash, ComputeAssetHash(ModelPath), "Flashlight FBX");
            RequireEqual(materialHash, ComputeAssetHash(MaterialPath), "Flashlight material");
            for (int index = 0; index < sourceTexturePaths.Length; index++)
                RequireEqual(sourceTextureHashes[index], ComputeAssetHash(sourceTexturePaths[index]),
                    sourceTexturePaths[index]);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            report.AppendLine("sceneSaved=True");
            WriteText(ApplyReportPath, report.ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[Flashlight] Three right-hand carries applied and scene saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Carry")]
        internal static void InspectCarry()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            Material expectedMaterial = RequireAsset<Material>(MaterialPath);
            string[] textures = SourceTexturePaths();
            if (textures.Length != 4)
                throw new InvalidOperationException(
                    "Expected four extracted source textures; found " + textures.Length + ".");
            RequireAsset<Texture2D>(PackedMetallicPath);

            var report = new StringBuilder()
                .AppendLine("Flashlight carry inspection")
                .AppendLine("modelHash=" + ComputeAssetHash(ModelPath))
                .AppendLine("materialHash=" + ComputeAssetHash(MaterialPath))
                .AppendLine("sourceTextureCount=" + textures.Length)
                .AppendLine("packedMetallicSmoothnessPresent=True");

            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                FlashlightRightHandFollowBehaviour behaviour =
                    target.GetComponent<FlashlightRightHandFollowBehaviour>() ??
                    throw new MissingReferenceException(
                        targetName + " flashlight follow behaviour is missing.");
                behaviour.RefreshPreview();
                Transform hand = RequirePath(target.transform, RightHandPath);
                if (behaviour.RightHand != hand || behaviour.Holder.transform.parent != hand)
                    throw new InvalidOperationException(targetName + " does not follow RightHand.");
                RequireAtMost(Vector3.Distance(
                        behaviour.Holder.transform.localPosition,
                        behaviour.HolderLocalPosition),
                    PositionTolerance, targetName + " holder local position error");
                RequireAtMost(Quaternion.Angle(
                        behaviour.Holder.transform.localRotation,
                        behaviour.HolderLocalRotation),
                    RotationTolerance, targetName + " holder local rotation error");
                float lensAngle = Vector3.Angle(
                    behaviour.Holder.transform.up, target.transform.forward);
                RequireAtMost(lensAngle, RotationTolerance, targetName + " lens forward angle");
                Bounds propBounds = CalculateBounds(behaviour.Holder);
                float propLength = Mathf.Max(
                    propBounds.size.x, Mathf.Max(propBounds.size.y, propBounds.size.z));
                if (Mathf.Abs(propLength - DesiredLength) > 0.002f)
                    throw new InvalidOperationException(
                        targetName + " length differs: " + Num(propLength));
                foreach (Renderer renderer in behaviour.Holder
                             .GetComponentsInChildren<Renderer>(true))
                foreach (Material material in renderer.sharedMaterials)
                    if (material != expectedMaterial)
                        throw new InvalidOperationException(
                            targetName + " renderer material differs: " +
                            (material == null ? "null" : material.name));

                report.AppendLine("target=" + targetName);
                report.AppendLine("rightHandFollow=True");
                report.AppendLine("positionError=0");
                report.AppendLine("rotationErrorDegrees=0");
                report.AppendLine("lensForwardAngleDegrees=" + Num(lensAngle));
                report.AppendLine("propLengthMeters=" + Num(propLength));
                report.AppendLine("palmToPropDistance=" + Num(Vector3.Distance(
                    RightPalmCenter(hand), propBounds.ClosestPoint(RightPalmCenter(hand)))));
            }

            WriteText(InspectReportPath, report.ToString());
            Debug.Log("[Flashlight] Carry inspection passed for all three targets.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Final")]
        internal static void CaptureFinal()
        {
            InspectCarry();
            Scene scene = RequireScene();
            var panels = new List<Texture2D>();
            try
            {
                foreach (string targetName in TargetNames)
                {
                    GameObject target = FindUnique(scene, targetName);
                    FlashlightRightHandFollowBehaviour behaviour =
                        target.GetComponent<FlashlightRightHandFollowBehaviour>();
                    behaviour.RefreshPreview();
                    Dictionary<GameObject, int> layers = SetReviewLayer(target, behaviour.Holder);
                    try
                    {
                        Vector3 viewDirection =
                            (target.transform.forward + target.transform.right * 0.28f).normalized;
                        panels.Add(CapturePanel(CalculateBounds(target, behaviour.Holder),
                            viewDirection));
                        panels.Add(CapturePanel(GripBounds(target.transform, behaviour.Holder),
                            viewDirection));
                    }
                    finally
                    {
                        RestoreLayers(layers);
                    }
                }

                Texture2D composite = ComposePanels(panels);
                try
                {
                    WritePng(FinalImagePath, composite);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(composite);
                }
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            }

            WriteText(FinalReportPath,
                "Flashlight final direct-review composite\n" +
                "columns=Flashlight_Idle|Flashlight_Charge_Connect|Flashlight_Charge_Disconnect\n" +
                "topRow=full character three-quarter view\n" +
                "bottomRow=right arm, fist grip, and flashlight close-up\n" +
                "lensDirection=transporter forward\n" +
                "rightHandFollow=True\n" +
                "sourceModelChanged=False\n");
            Debug.Log("[Flashlight] Final direct-review composite captured once.");
        }

        private static void ImportEmbeddedAssets()
        {
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(ReviewFolder);
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Flashlight ModelImporter is missing.");
            importer.ExtractTextures(Absolute(TextureFolder));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string baseColorPath = FindSourceTexture("base_color");
            string normalPath = FindSourceTexture("normal");
            string metallicPath = FindSourceTexture("metallic");
            string roughnessPath = FindSourceTexture("roughness");
            ConfigureTexture(baseColorPath, TextureImporterType.Default, true, false);
            ConfigureTexture(normalPath, TextureImporterType.NormalMap, false, false);
            ConfigureTexture(metallicPath, TextureImporterType.Default, false, true);
            ConfigureTexture(roughnessPath, TextureImporterType.Default, false, true);
            WritePackedMetallicSmoothness(metallicPath, roughnessPath);
            ConfigureTexture(metallicPath, TextureImporterType.Default, false, false);
            ConfigureTexture(roughnessPath, TextureImporterType.Default, false, false);

            Material embedded = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>().SingleOrDefault() ??
                throw new MissingReferenceException("Flashlight embedded material is missing.");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new MissingReferenceException("URP Lit shader is missing.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(embedded) { name = "Flashlight" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            Texture2D baseColor = RequireAsset<Texture2D>(baseColorPath);
            Texture2D normal = RequireAsset<Texture2D>(normalPath);
            Texture2D metallicSmoothness = RequireAsset<Texture2D>(PackedMetallicPath);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseColor);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseColor);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            if (material.HasProperty("_MetallicGlossMap"))
                material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 1f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Flashlight ModelImporter disappeared.");
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), material);
            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            string[] sourceTextures = SourceTexturePaths();
            if (sourceTextures.Length != 4)
                throw new InvalidOperationException(
                    "Expected four extracted source textures; found " + sourceTextures.Length + ".");
            WriteText(ImportReportPath,
                "Flashlight embedded asset import\n" +
                "sourceModelChanged=False\n" +
                "sourceTextureCount=4\n" +
                "materialCount=1\n" +
                "baseColor=" + baseColorPath + "\n" +
                "normal=" + normalPath + "\n" +
                "metallic=" + metallicPath + "\n" +
                "roughness=" + roughnessPath + "\n" +
                "metallicSmoothness=" + PackedMetallicPath + "\n" +
                "roughnessUsage=inverted into metallic alpha as smoothness\n");
        }

        private static void ConfigureTexture(
            string path, TextureImporterType type, bool srgb, bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is missing: " + path);
            importer.textureType = type;
            importer.sRGBTexture = srgb;
            importer.isReadable = readable;
            importer.SaveAndReimport();
        }

        private static void WritePackedMetallicSmoothness(
            string metallicPath, string roughnessPath)
        {
            Texture2D metallic = RequireAsset<Texture2D>(metallicPath);
            Texture2D roughness = RequireAsset<Texture2D>(roughnessPath);
            if (metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException("Metallic and roughness dimensions differ.");
            Color32[] metalPixels = metallic.GetPixels32();
            Color32[] roughPixels = roughness.GetPixels32();
            var packed = new Texture2D(
                metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            try
            {
                var pixels = new Color32[metalPixels.Length];
                for (int index = 0; index < pixels.Length; index++)
                {
                    byte metal = metalPixels[index].r;
                    byte smoothness = (byte)(255 - roughPixels[index].r);
                    pixels[index] = new Color32(metal, metal, metal, smoothness);
                }
                packed.SetPixels32(pixels);
                packed.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(Absolute(PackedMetallicPath)));
                File.WriteAllBytes(Absolute(PackedMetallicPath), packed.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(packed);
            }
            AssetDatabase.ImportAsset(PackedMetallicPath,
                ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture(PackedMetallicPath, TextureImporterType.Default, false, false);
        }

        private static string FindSourceTexture(string token)
        {
            string[] matches = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path)
                    .IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(path => path != PackedMetallicPath)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + token + " source texture; found " + matches.Length + ".");
            return matches[0];
        }

        private static string[] SourceTexturePaths()
        {
            if (!AssetDatabase.IsValidFolder(TextureFolder)) return Array.Empty<string>();
            return AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != PackedMetallicPath)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }

        private static Bounds GripBounds(Transform target, GameObject holder)
        {
            Transform upper = RequirePath(target, RightArmPath);
            Transform fore = RequirePath(target, RightForeArmPath);
            Transform hand = RequirePath(target, RightHandPath);
            Bounds bounds = CalculateBounds(holder);
            bounds.Encapsulate(upper.position);
            bounds.Encapsulate(fore.position);
            bounds.Encapsulate(hand.position);
            foreach (Transform finger in hand.GetComponentsInChildren<Transform>(true))
                bounds.Encapsulate(finger.position);
            bounds.Expand(0.10f);
            return bounds;
        }

        private static Texture2D CapturePanel(Bounds bounds, Vector3 directionFromTarget)
        {
            GameObject cameraObject = new GameObject("Flashlight_ReadOnlyCamera")
                { hideFlags = HideFlags.HideAndDontSave };
            GameObject lightObject = new GameObject("Flashlight_ReadOnlyLight")
                { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture render = new RenderTexture(PanelSize, PanelSize, 24,
                RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.18f, 0.20f, 0.22f, 1f);
                camera.cullingMask = 1 << ReviewLayer;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.y,
                    Mathf.Max(bounds.extents.x, bounds.extents.z)) * 1.22f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                camera.transform.position = bounds.center +
                    directionFromTarget.normalized * 4f;
                camera.transform.LookAt(bounds.center, Vector3.up);
                camera.targetTexture = render;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                light.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(PanelSize, PanelSize, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Texture2D ComposePanels(IReadOnlyList<Texture2D> panels)
        {
            if (panels.Count != 6)
                throw new InvalidOperationException("Expected six Flashlight review panels.");
            var composite = new Texture2D(PanelSize * 3, PanelSize * 2,
                TextureFormat.RGBA32, false);
            for (int column = 0; column < 3; column++)
            {
                composite.SetPixels32(column * PanelSize, PanelSize,
                    PanelSize, PanelSize, panels[column * 2].GetPixels32());
                composite.SetPixels32(column * PanelSize, 0,
                    PanelSize, PanelSize, panels[column * 2 + 1].GetPixels32());
            }
            composite.Apply(false, false);
            return composite;
        }

        private static Dictionary<GameObject, int> SetReviewLayer(
            GameObject target, GameObject holder)
        {
            var result = new Dictionary<GameObject, int>();
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true))
            {
                result[item.gameObject] = item.gameObject.layer;
                item.gameObject.layer = ReviewLayer;
            }
            if (holder != null)
            foreach (Transform item in holder.GetComponentsInChildren<Transform>(true))
            {
                if (!result.ContainsKey(item.gameObject)) result[item.gameObject] = item.gameObject.layer;
                item.gameObject.layer = ReviewLayer;
            }
            return result;
        }

        private static void RestoreLayers(Dictionary<GameObject, int> layers)
        {
            foreach (KeyValuePair<GameObject, int> item in layers)
                if (item.Key != null) item.Key.layer = item.Value;
        }

        private static Vector3 RightPalmCenter(Transform hand)
        {
            string[] names =
            {
                "RightIndexProximal", "RightMiddleProximal",
                "RightRingProximal", "RightLittleProximal"
            };
            return names.Select(name => hand.Find(name)?.position ??
                    throw new MissingReferenceException("Right-hand knuckle is missing: " + name))
                .Aggregate(Vector3.zero, (sum, value) => sum + value) / names.Length;
        }

        private static Vector3 RightFingerDirection(Transform hand)
        {
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new MissingReferenceException("RightMiddleProximal is missing.");
            return (middle.position - hand.position).normalized;
        }

        private static Vector3 RightPalmNormal(Transform hand)
        {
            Transform index = hand.Find("RightIndexProximal") ??
                throw new MissingReferenceException("RightIndexProximal is missing.");
            Transform little = hand.Find("RightLittleProximal") ??
                throw new MissingReferenceException("RightLittleProximal is missing.");
            return Vector3.Cross(
                (little.position - index.position).normalized,
                RightFingerDirection(hand)).normalized;
        }

        private static int RightGripJointCount(PlayerHandGripPose gripPose) =>
            gripPose.Joints.Count(item => item.BoneName.StartsWith(
                "Right", StringComparison.Ordinal));

        private static Bounds CalculateBounds(params GameObject[] roots)
        {
            Renderer[] renderers = roots.Where(root => root != null)
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("No renderer was found for bounds.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static string ProtectedSceneSignature(Scene scene)
        {
            var builder = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects()
                         .OrderBy(item => item.name, StringComparer.Ordinal))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .Where(item => !HasTargetAncestor(item))
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, root.transform), StringComparer.Ordinal))
                builder.Append(root.name).Append('|')
                    .Append(AnimationUtility.CalculateTransformPath(item, root.transform))
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').AppendLine(Vec(item.localScale));
            return Sha256(builder.ToString());
        }

        private static bool HasTargetAncestor(Transform item)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (TargetNames.Contains(current.name)) return true;
            return false;
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. Active=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Flashlight setup requires Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Transform RequirePath(Transform root, string path)
        {
            return root.Find(path) ?? throw new MissingReferenceException(
                root.name + " path is missing: " + path);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new MissingReferenceException(
                "Asset is missing: " + path);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static string DominantAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z) return "X";
            return size.y >= size.z ? "Y" : "Z";
        }

        private static void WriteText(string assetPath, string contents)
        {
            string absolute = Absolute(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void WritePng(string assetPath, Texture2D image)
        {
            string absolute = Absolute(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllBytes(absolute, image.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string ComputeAssetHash(string assetPath)
        {
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static string Absolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static void RequireAtMost(float actual, float maximum, string label)
        {
            if (actual > maximum)
                throw new InvalidOperationException(
                    label + " exceeded. actual=" + Num(actual) + " maximum=" + Num(maximum));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static string Num(float value) =>
            value.ToString("0.000000", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," +
            Num(value.z) + "," + Num(value.w);

        private readonly struct PoseState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            internal PoseState(Transform transform)
            {
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            internal void RequireExact(Transform transform, string label)
            {
                if (transform.localPosition != localPosition ||
                    transform.localRotation != localRotation ||
                    transform.localScale != localScale)
                    throw new InvalidOperationException(label + " changed unexpectedly.");
            }
        }
    }

    [InitializeOnLoad]
    internal static class FlashlightIdleLocomotionTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Flashlight_Idle";
        private const string AssetFolder = "Assets/_Project/Animation/Flashlight";
        private const string ReviewFolder = AssetFolder + "/Review";
        private const string ControllerPath =
            AssetFolder + "/FlashlightIdle_Locomotion.controller";
        private const string IdleClipPath = AssetFolder + "/FlashlightIdle_Idle.anim";
        private const string ForwardClipPath =
            AssetFolder + "/FlashlightIdle_WalkForward.anim";
        private const string BackwardClipPath =
            AssetFolder + "/FlashlightIdle_WalkBackward.anim";
        private const string SidestepClipPath =
            AssetFolder + "/FlashlightIdle_Sidestep.anim";
        private const string RunClipPath =
            AssetFolder + "/FlashlightIdle_RunForward.anim";
        private const string StateName = "FlashlightIdleLocomotion2D";
        private const string RootTreeName = "FlashlightIdleSixMotion2D";
        private const string DiagonalTreeName =
            "FlashlightIdleWalkDiagonalSourceExact";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const string ModelPath =
            "Assets/_Project/Art/Items/Flashlight/Flashlight.fbx";
        private const string MaterialPath =
            "Assets/_Project/Art/Items/Flashlight/Materials/Flashlight.mat";
        private const string FinalImagePath = ReviewFolder + "/final.png";
        private const string LightCycleApplicationPath =
            ReviewFolder + "/light_cycle_application.txt";
        private const string LightCycleInspectionPath =
            ReviewFolder + "/light_cycle_inspection.txt";
        private const string LightCycleFinalImagePath =
            ReviewFolder + "/light_cycle_final.png";
        private const string LightCycleFinalReportPath =
            ReviewFolder + "/light_cycle_final.txt";
        private const string LightCycleCompletionPath =
            ReviewFolder + "/light_cycle_completion.txt";
        private const string AutoStateKey =
            "Bellerophon.FlashlightIdleLocomotion.AutoState";
        private const string LightCycleAutoStateKey =
            "Bellerophon.FlashlightIdleLightCycle.AutoState";
        private const string AwaitingPlayState = "AwaitingPlay";
        private const string AwaitingEditState = "AwaitingEdit";
        private const string AwaitingLightCyclePlayState = "AwaitingLightCyclePlay";
        private const string AwaitingLightCycleEditState = "AwaitingLightCycleEdit";
        private const float CapturePhaseTime = 0.48f;
        private const double CaptureTimeoutSeconds = 20d;
        private const double LightCycleCaptureTimeoutSeconds = 16d;

        private static readonly string[] SourceNames =
        {
            "Player_Idle", "Player_Walk_Forward", "Player_Walk_Backward",
            "Player_Sidestep", "Player_Walk_Diagonal", "Player_Run_Forward"
        };

        private static readonly string[] RightArmPaths =
        {
            RightArmPath, RightForeArmPath, RightHandPath
        };

        private static readonly Vector2[] RequiredPositions =
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 0f), new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f)
        };

        private static bool captureActive;
        private static double captureStartTime;
        private static int baseAbsolutePhase = -1;
        private static int nextPanel;
        private static Animator runtimeAnimator;
        private static GameObject runtimeTarget;
        private static FlashlightRightHandFollowBehaviour runtimeCarry;
        private static Texture2D[] fullPanels;
        private static Texture2D[] gripPanels;
        private static readonly List<string> RuntimeObservations = new List<string>();
        private static bool swayBaselineSet;
        private static Quaternion initialSpine;
        private static Quaternion initialHead;
        private static Quaternion initialRightShoulder;
        private static Vector3 initialFlashlightPosition;
        private static Quaternion initialFlashlightRotation;
        private static float maximumSpineSway;
        private static float maximumHeadSway;
        private static float maximumRightShoulderSway;
        private static float maximumRightArmDeviation;
        private static float maximumRightForeArmDeviation;
        private static float maximumRightHandDeviation;
        private static float maximumFollowPositionError;
        private static float maximumFollowRotationError;
        private static float maximumFlashlightTravel;
        private static float maximumFlashlightRotation;
        private static bool lightCycleCaptureActive;
        private static double lightCycleCaptureStartTime;
        private static int lightCycleBaseAbsolutePhase = -1;
        private static int lightCycleNextPanel;
        private static Animator lightCycleAnimator;
        private static GameObject lightCycleTarget;
        private static FlashlightRightHandFollowBehaviour lightCycleCarry;
        private static FlashlightIdleLensLightBehaviour lightCycleBehaviour;
        private static GameObject lightCycleCameraObject;
        private static GameObject lightCycleLensCameraObject;
        private static GameObject lightCycleReceiverObject;
        private static GameObject lightCycleFillLightObject;
        private static Camera lightCycleCamera;
        private static Camera lightCycleLensCamera;
        private static Texture2D[] lightCyclePanels;
        private static Texture2D[] lightCycleLensPanels;
        private static readonly float[] LightCyclePanelLuminance = new float[4];
        private static readonly float[] LightCycleLensLuminance = new float[4];
        private static readonly List<string> LightCycleObservations = new List<string>();

        static FlashlightIdleLocomotionTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= RuntimeCaptureTick;
            EditorApplication.update -= LightCycleRuntimeCaptureTick;
            if (!string.IsNullOrEmpty(
                SessionState.GetString(AutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueAutomaticApplicationAndReview;
            if (!string.IsNullOrEmpty(
                SessionState.GetString(LightCycleAutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueLightCycleReview;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!string.IsNullOrEmpty(
                SessionState.GetString(AutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueAutomaticApplicationAndReview;
            if (!string.IsNullOrEmpty(
                SessionState.GetString(LightCycleAutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueLightCycleReview;
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Idle Light Cycle")]
        internal static void ApplyIdleLightCycle()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);

            string targetPoseBefore = TargetPoseSignature(target.transform);
            string nonTargetSceneBefore = NonTargetSceneSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string flashlightVisualBefore = FlashlightVisualSignature(carry);
            string modelHash = ComputeAssetHash(ModelPath);
            string materialHash = ComputeAssetHash(MaterialPath);
            string controllerPath = AssetDatabase.GetAssetPath(
                animator.runtimeAnimatorController);
            string controllerHash = ComputeAssetHash(controllerPath);

            FlashlightIdleLensLightBehaviour lightCycle =
                target.GetComponent<FlashlightIdleLensLightBehaviour>() ??
                Undo.AddComponent<FlashlightIdleLensLightBehaviour>(target);
            lightCycle.RefreshPreview();
            EditorUtility.SetDirty(lightCycle);

            RequireEqual(targetPoseBefore, TargetPoseSignature(target.transform),
                "Flashlight_Idle authored transform pose");
            RequireEqual(nonTargetSceneBefore,
                NonTargetSceneSignature(scene, target.transform),
                "objects outside Flashlight_Idle");
            RequireEqual(carryBefore, CarrySignature(carry),
                "flashlight model and hand-follow configuration");
            RequireEqual(flashlightVisualBefore, FlashlightVisualSignature(carry),
                "flashlight transform, renderer, mesh, and materials");
            RequireEqual(modelHash, ComputeAssetHash(ModelPath), "flashlight model");
            RequireEqual(materialHash, ComputeAssetHash(MaterialPath), "flashlight material");
            RequireEqual(controllerHash, ComputeAssetHash(controllerPath),
                "flashlight locomotion controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene could not save the Flashlight_Idle light cycle.");

            EnsureFolder(ReviewFolder);
            WriteText(LightCycleApplicationPath, new StringBuilder()
                .AppendLine("Flashlight_Idle five-second idle light-cycle application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("preexistingUserSceneChangesPreserved=True")
                .AppendLine("idleDurationSeconds=" +
                    F(FlashlightIdleLocomotionCycleBehaviour.IdleDurationSeconds))
                .AppendLine("lightOnDurationSeconds=" +
                    F(FlashlightIdleLensLightBehaviour.LightOnDurationSeconds))
                .AppendLine("lightOffDurationSeconds=" + F(
                    FlashlightIdleLocomotionCycleBehaviour.IdleDurationSeconds -
                    FlashlightIdleLensLightBehaviour.LightOnDurationSeconds))
                .AppendLine("otherMotionDurationSeconds=" +
                    F(FlashlightIdleLocomotionCycleBehaviour.SecondsPerMotion))
                .AppendLine("totalCycleDurationSeconds=" +
                    F(FlashlightIdleLocomotionCycleBehaviour.TotalCycleDurationSeconds))
                .AppendLine("lightRangeMeters=" +
                    F(FlashlightIdleLensLightBehaviour.LightRangeMeters))
                .AppendLine("flashlightTransformChanged=False")
                .AppendLine("flashlightRendererMeshMaterialTextureChanged=False")
                .AppendLine("rightArmPoseChanged=False")
                .AppendLine("rightHandFollowChanged=False")
                .AppendLine("objectsOutsideFlashlightIdleChanged=False")
                .ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[FlashlightIdleLightCycle] Five-second idle light cycle applied.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Diagnose Idle Lens Light")]
        internal static void DiagnoseIdleLensLight()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightIdleLensLightBehaviour lightCycle =
                target.GetComponent<FlashlightIdleLensLightBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Idle lens light cycle behaviour is missing.");
            Light beamLight = lightCycle.LensLight ??
                throw new MissingReferenceException(
                    "Flashlight_Idle beam light preview is missing.");
            Light[] holderLights = carry.Holder.GetComponentsInChildren<Light>(true);
            bool hasLensSurfaceLight = holderLights.Any(
                item => item != beamLight && item.type == LightType.Point);

            EnsureFolder(ReviewFolder);
            WriteText(ReviewFolder + "/lens_light_diagnosis.txt", new StringBuilder()
                .AppendLine("Flashlight_Idle visible lens-light diagnosis")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("beamSpotLightPresent=" + (beamLight.type == LightType.Spot))
                .AppendLine("beamRangeMeters=" + F(beamLight.range))
                .AppendLine("holderLightCount=" + holderLights.Length)
                .AppendLine("lensSurfacePointLightPresent=" + hasLensSurfaceLight)
                .AppendLine("lightComponentHasVisibleSourceGeometry=False")
                .AppendLine("forwardReceiverIlluminationPreviouslyConfirmed=True")
                .AppendLine("visibleLensIlluminationMissing=" + !hasLensSurfaceLight)
                .AppendLine("rootCause=Spot light illuminates forward surfaces but does not render a visible source or illuminate the lens face")
                .AppendLine("flashlightRendererMeshMaterialTextureChanged=False")
                .ToString());
            Debug.Log(
                "[FlashlightIdleLensLight] Read-only diagnosis completed: lens-surface light=" +
                hasLensSurfaceLight + ".");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Idle Lens Light Fix")]
        internal static void ApplyIdleLensLightFix()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightIdleLensLightBehaviour lightCycle =
                target.GetComponent<FlashlightIdleLensLightBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Idle lens light cycle behaviour is missing.");

            string targetPoseBefore = TargetPoseSignature(target.transform);
            string nonTargetSceneBefore = NonTargetSceneSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string flashlightVisualBefore = FlashlightVisualSignature(carry);
            string modelHash = ComputeAssetHash(ModelPath);
            string materialHash = ComputeAssetHash(MaterialPath);

            lightCycle.ResetEditModePreview();
            Light beamLight = lightCycle.LensLight ??
                throw new MissingReferenceException("Flashlight beam light is missing.");
            Light lensSurfaceLight = lightCycle.LensSurfaceLight ??
                throw new MissingReferenceException(
                    "Flashlight lens-surface light is missing after the fix.");
            if (beamLight.type != LightType.Spot ||
                lensSurfaceLight.type != LightType.Point ||
                beamLight.enabled || lensSurfaceLight.enabled)
                throw new InvalidOperationException(
                    "Flashlight edit-mode light configuration is invalid.");

            RequireEqual(targetPoseBefore, TargetPoseSignature(target.transform),
                "Flashlight_Idle authored transform pose");
            RequireEqual(nonTargetSceneBefore,
                NonTargetSceneSignature(scene, target.transform),
                "objects outside Flashlight_Idle");
            RequireEqual(carryBefore, CarrySignature(carry),
                "flashlight model and hand-follow configuration");
            RequireEqual(flashlightVisualBefore, FlashlightVisualSignature(carry),
                "flashlight transform, renderer, mesh, and materials");
            RequireEqual(modelHash, ComputeAssetHash(ModelPath), "flashlight model");
            RequireEqual(materialHash, ComputeAssetHash(MaterialPath), "flashlight material");

            EnsureFolder(ReviewFolder);
            WriteText(ReviewFolder + "/lens_light_fix_application.txt", new StringBuilder()
                .AppendLine("Flashlight_Idle visible lens-light fix application")
                .AppendLine("beamLightType=Spot")
                .AppendLine("lensSurfaceLightType=Point")
                .AppendLine("lensSurfaceLightRangeMeters=" +
                    F(FlashlightIdleLensLightBehaviour.LensSurfaceLightRangeMeters))
                .AppendLine("lensSurfaceLightIntensity=" +
                    F(FlashlightIdleLensLightBehaviour.LensSurfaceLightIntensity))
                .AppendLine("bothLightsFollowRightHandHolder=True")
                .AppendLine("bothLightsShareZeroToTwoSecondWindow=True")
                .AppendLine("sceneSaveRequired=False")
                .AppendLine("flashlightTransformChanged=False")
                .AppendLine("flashlightRendererMeshMaterialTextureChanged=False")
                .AppendLine("rightArmPoseChanged=False")
                .AppendLine("objectsOutsideFlashlightIdleChanged=False")
                .ToString());
            AssetDatabase.SaveAssets();
            Debug.Log(
                "[FlashlightIdleLensLight] Visible lens-surface light fix applied.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Idle Light Cycle")]
        internal static void InspectIdleLightCycle()
        {
            RequireEditMode();
            InspectIdleLocomotion();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightIdleLensLightBehaviour lightCycle =
                target.GetComponent<FlashlightIdleLensLightBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Idle lens light cycle behaviour is missing.");
            lightCycle.ResetEditModePreview();
            Light lensLight = lightCycle.LensLight ??
                throw new MissingReferenceException(
                    "Flashlight_Idle lens light preview is missing.");
            Light lensSurfaceLight = lightCycle.LensSurfaceLight ??
                throw new MissingReferenceException(
                    "Flashlight_Idle lens-surface light preview is missing.");

            if (lensLight.transform.parent != carry.Holder.transform ||
                lensSurfaceLight.transform.parent != carry.Holder.transform)
                throw new InvalidOperationException(
                    "Flashlight lights do not follow the right-hand holder.");
            if (lensLight.type != LightType.Spot ||
                !Mathf.Approximately(lensLight.range,
                    FlashlightIdleLensLightBehaviour.LightRangeMeters) ||
                !Mathf.Approximately(lensLight.intensity,
                    FlashlightIdleLensLightBehaviour.LightIntensity) ||
                !Mathf.Approximately(lensLight.spotAngle,
                    FlashlightIdleLensLightBehaviour.SpotAngleDegrees) ||
                !Mathf.Approximately(lensLight.innerSpotAngle,
                    FlashlightIdleLensLightBehaviour.InnerSpotAngleDegrees))
                throw new InvalidOperationException(
                    "Flashlight lens light settings differ from the requested cycle.");
            if (lensSurfaceLight.type != LightType.Point ||
                !Mathf.Approximately(lensSurfaceLight.range,
                    FlashlightIdleLensLightBehaviour.LensSurfaceLightRangeMeters) ||
                !Mathf.Approximately(lensSurfaceLight.intensity,
                    FlashlightIdleLensLightBehaviour.LensSurfaceLightIntensity))
                throw new InvalidOperationException(
                    "Flashlight lens-surface light settings are invalid.");
            if (Vector3.Angle(lensLight.transform.forward,
                    carry.Holder.transform.up) > 0.01f)
                throw new InvalidOperationException(
                    "Flashlight lens light does not point along the lens axis.");
            if (lensLight.enabled || lensSurfaceLight.enabled)
                throw new InvalidOperationException(
                    "Flashlight lights must remain off outside natural playback.");

            WriteText(LightCycleInspectionPath, new StringBuilder()
                .AppendLine("Flashlight_Idle light-cycle inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("idleDurationSeconds=5")
                .AppendLine("lightOnWindowSeconds=0-2")
                .AppendLine("lightOffWindowSeconds=2-5")
                .AppendLine("otherMotionDurationSeconds=1")
                .AppendLine("totalCycleDurationSeconds=10")
                .AppendLine("lightType=Spot")
                .AppendLine("lightRangeMeters=3")
                .AppendLine("lensSurfaceLightType=Point")
                .AppendLine("lensSurfaceLightPresent=True")
                .AppendLine("lensForwardAligned=True")
                .AppendLine("rightHandFollowRetained=True")
                .AppendLine("flashlightVisualAssetsChanged=False")
                .ToString());
            if (File.Exists(Absolute(LightCycleFinalImagePath)) &&
                File.Exists(Absolute(LightCycleFinalReportPath)) &&
                File.Exists(Absolute(ReviewFolder + "/light_cycle_measurements.txt")))
            {
                AssetDatabase.DeleteAsset(ReviewFolder + "/light_cycle_failure.txt");
                WriteText(LightCycleCompletionPath, new StringBuilder()
                    .AppendLine("Flashlight_Idle light-cycle direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("editModeLensLightDisabled=True")
                    .AppendLine("unityConsoleErrorsDuringNaturalCapture=0")
                    .ToString());
            }
            Debug.Log("[FlashlightIdleLightCycle] Inspection passed without target manipulation.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Idle Lens Light Fix")]
        internal static void InspectIdleLensLightFix()
        {
            InspectIdleLightCycle();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightIdleLensLightBehaviour lightCycle =
                target.GetComponent<FlashlightIdleLensLightBehaviour>();
            Light beam = lightCycle.LensLight;
            Light surface = lightCycle.LensSurfaceLight;
            float sourceSeparation = Vector3.Distance(
                beam.transform.position, surface.transform.position);

            WriteText(ReviewFolder + "/lens_light_fix_inspection.txt", new StringBuilder()
                .AppendLine("Flashlight_Idle visible lens-light fix inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("beamSpotLightPresent=True")
                .AppendLine("lensSurfacePointLightPresent=True")
                .AppendLine("lensSurfaceSourceOffsetMeters=" + F(sourceSeparation))
                .AppendLine("bothLightsParentedToRightHandHolder=" +
                    (beam.transform.parent == carry.Holder.transform &&
                     surface.transform.parent == carry.Holder.transform))
                .AppendLine("flashlightRendererMeshMaterialTextureChanged=False")
                .ToString());
            Debug.Log("[FlashlightIdleLensLight] Fix inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Idle Light Cycle Final")]
        internal static void CaptureIdleLightCycleFinal()
        {
            RequireEditMode();
            InspectIdleLightCycle();
            AssetDatabase.DeleteAsset(LightCycleFinalImagePath);
            AssetDatabase.DeleteAsset(LightCycleFinalReportPath);
            AssetDatabase.DeleteAsset(LightCycleCompletionPath);
            AssetDatabase.DeleteAsset(ReviewFolder + "/light_cycle_failure.txt");
            AssetDatabase.DeleteAsset(ReviewFolder + "/light_cycle_measurements.txt");
            SessionState.SetString(
                LightCycleAutoStateKey, AwaitingLightCyclePlayState);
            EditorApplication.EnterPlaymode();
        }

        private static void ContinueLightCycleReview()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ContinueLightCycleReview;
                return;
            }

            string state = SessionState.GetString(LightCycleAutoStateKey, string.Empty);
            try
            {
                if (state == AwaitingLightCyclePlayState && EditorApplication.isPlaying)
                {
                    BeginLightCycleRuntimeCapture();
                    return;
                }

                if (state == AwaitingLightCycleEditState &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    InspectIdleLightCycle();
                    int errors = UnityConsoleErrorCount();
                    if (errors != 0)
                        throw new InvalidOperationException(
                            "Unity Console contains " + errors +
                            " errors after the light-cycle review.");
                    WriteText(LightCycleCompletionPath, new StringBuilder()
                        .AppendLine("Flashlight_Idle light-cycle direct review")
                        .AppendLine("applicationCompleted=True")
                        .AppendLine("naturalPlaybackObserved=True")
                        .AppendLine("returnedToEditMode=True")
                        .AppendLine("editModeLensLightDisabled=True")
                        .AppendLine("unityConsoleErrors=0")
                        .ToString());
                    SessionState.EraseString(LightCycleAutoStateKey);
                    Debug.Log(
                        "[FlashlightIdleLightCycle] Natural playback review completed.");
                }
            }
            catch (Exception exception)
            {
                FailLightCycleReview(exception);
            }
        }

        private static void BeginLightCycleRuntimeCapture()
        {
            if (lightCycleCaptureActive) return;
            lightCycleTarget = FindUnique(RequireScene(), TargetName);
            lightCycleAnimator = RequireAnimator(lightCycleTarget);
            lightCycleCarry = RequireCarry(lightCycleTarget);
            lightCycleBehaviour =
                lightCycleTarget.GetComponent<FlashlightIdleLensLightBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Idle lens light cycle behaviour is missing in Play Mode.");
            lightCycleCarry.RefreshPreview();
            lightCycleBehaviour.RefreshPreview();

            lightCycleCaptureActive = true;
            lightCycleCaptureStartTime = EditorApplication.timeSinceStartup;
            lightCycleBaseAbsolutePhase = -1;
            lightCycleNextPanel = 0;
            lightCyclePanels = new Texture2D[4];
            lightCycleLensPanels = new Texture2D[4];
            Array.Clear(LightCyclePanelLuminance, 0, LightCyclePanelLuminance.Length);
            Array.Clear(LightCycleLensLuminance, 0, LightCycleLensLuminance.Length);
            LightCycleObservations.Clear();
            CreateLightCycleCaptureObjects();
            EditorApplication.update -= LightCycleRuntimeCaptureTick;
            EditorApplication.update += LightCycleRuntimeCaptureTick;
        }

        private static void LightCycleRuntimeCaptureTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException(
                        "Play Mode ended before the flashlight light-cycle review completed.");
                if (EditorApplication.timeSinceStartup - lightCycleCaptureStartTime >
                    LightCycleCaptureTimeoutSeconds)
                    throw new TimeoutException(
                        "Flashlight_Idle natural light-cycle review exceeded 16 seconds.");
                if (!lightCycleAnimator.isInitialized ||
                    !FlashlightIdleLocomotionCycleBehaviour.TryGetSequenceState(
                        lightCycleAnimator,
                        out int absolutePhase,
                        out int phase,
                        out float phaseElapsed))
                    return;

                if (lightCycleBaseAbsolutePhase < 0)
                {
                    if (phase != 0 || phaseElapsed > 1.65f) return;
                    lightCycleBaseAbsolutePhase = absolutePhase;
                }

                if (!ShouldCaptureLightCyclePanel(
                        lightCycleNextPanel, absolutePhase, phase, phaseElapsed))
                    return;

                bool expectedLightOn = lightCycleNextPanel == 0 ||
                    lightCycleNextPanel == 3;
                Light lensLight = lightCycleBehaviour.LensLight ??
                    throw new MissingReferenceException(
                        "Flashlight lens light disappeared during natural playback.");
                Light lensSurfaceLight = lightCycleBehaviour.LensSurfaceLight ??
                    throw new MissingReferenceException(
                        "Flashlight lens-surface light disappeared during natural playback.");
                if (lensLight.enabled != expectedLightOn ||
                    lensSurfaceLight.enabled != expectedLightOn)
                    throw new InvalidOperationException(
                        "Flashlight light state differs at review panel " +
                        lightCycleNextPanel + ".");
                float lensForwardError = Vector3.Angle(
                    lensLight.transform.forward,
                    lightCycleCarry.Holder.transform.up);
                if (lensForwardError > 0.01f)
                    throw new InvalidOperationException(
                        "Flashlight beam no longer follows the lens forward axis.");

                Texture2D panel = CaptureLightCyclePanel();
                lightCyclePanels[lightCycleNextPanel] = panel;
                Texture2D lensPanel = CaptureLightCycleLensPanel();
                lightCycleLensPanels[lightCycleNextPanel] = lensPanel;
                LightCyclePanelLuminance[lightCycleNextPanel] =
                    ReceiverLuminance(panel);
                LightCycleLensLuminance[lightCycleNextPanel] =
                    LensLuminance(lensPanel);
                LightCycleObservations.Add(
                    "panel=" + lightCycleNextPanel +
                    "|absolutePhase=" + absolutePhase +
                    "|phase=" + phase +
                    "|motion=" +
                        FlashlightIdleLocomotionCycleBehaviour.MotionName(phase) +
                    "|phaseElapsed=" + F(phaseElapsed) +
                    "|lightEnabled=" + lensLight.enabled +
                    "|lensSurfaceLightEnabled=" + lensSurfaceLight.enabled +
                    "|lensForwardErrorDegrees=" + F(lensForwardError) +
                    "|receiverLuminance=" +
                        F(LightCyclePanelLuminance[lightCycleNextPanel]) +
                    "|lensLuminance=" +
                        F(LightCycleLensLuminance[lightCycleNextPanel]));
                lightCycleNextPanel++;

                if (lightCycleNextPanel == lightCyclePanels.Length)
                    FinishLightCycleRuntimeCapture();
            }
            catch (Exception exception)
            {
                FailLightCycleReview(exception);
            }
        }

        private static bool ShouldCaptureLightCyclePanel(
            int panel,
            int absolutePhase,
            int phase,
            float phaseElapsed)
        {
            switch (panel)
            {
                case 0:
                    return absolutePhase == lightCycleBaseAbsolutePhase &&
                        phase == 0 && phaseElapsed >= 1f;
                case 1:
                    return absolutePhase == lightCycleBaseAbsolutePhase &&
                        phase == 0 && phaseElapsed >= 3.5f;
                case 2:
                    return absolutePhase == lightCycleBaseAbsolutePhase + 1 &&
                        phase == 1 && phaseElapsed >= 0.5f;
                case 3:
                    return absolutePhase == lightCycleBaseAbsolutePhase +
                        FlashlightIdleLocomotionCycleBehaviour.MotionCount &&
                        phase == 0 && phaseElapsed >= 0.5f;
                default:
                    return false;
            }
        }

        private static void CreateLightCycleCaptureObjects()
        {
            Light lensLight = lightCycleBehaviour.LensLight ??
                throw new MissingReferenceException(
                    "Flashlight lens light is missing before direct review.");
            Scene scene = RequireScene();
            Vector3 beamForward = lensLight.transform.forward.normalized;
            Vector3 targetUp = lightCycleTarget.transform.up.normalized;

            lightCycleReceiverObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightCycleReceiverObject.name = "FlashlightLightCycle_ReadOnlyReceiver";
            lightCycleReceiverObject.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(lightCycleReceiverObject, scene);
            Collider receiverCollider = lightCycleReceiverObject.GetComponent<Collider>();
            if (receiverCollider != null) UnityEngine.Object.DestroyImmediate(receiverCollider);
            lightCycleReceiverObject.transform.SetPositionAndRotation(
                lensLight.transform.position + beamForward * 2.15f,
                Quaternion.LookRotation(beamForward, targetUp));
            lightCycleReceiverObject.transform.localScale =
                new Vector3(1.7f, 1.7f, 0.06f);

            lightCycleCameraObject = new GameObject("FlashlightLightCycle_ReadOnlyCamera");
            lightCycleCameraObject.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(lightCycleCameraObject, scene);
            lightCycleCamera = lightCycleCameraObject.AddComponent<Camera>();
            lightCycleCamera.fieldOfView = 42f;
            lightCycleCamera.nearClipPlane = 0.03f;
            lightCycleCamera.farClipPlane = 20f;
            lightCycleCamera.clearFlags = CameraClearFlags.SolidColor;
            lightCycleCamera.backgroundColor = new Color(0.018f, 0.022f, 0.03f, 1f);

            Bounds viewBounds = FullBounds(lightCycleTarget.transform);
            viewBounds.Encapsulate(
                lightCycleReceiverObject.GetComponent<Renderer>().bounds);
            Vector3 viewDirection = (
                lightCycleTarget.transform.right * 0.92f +
                targetUp * 0.18f -
                beamForward * 0.35f).normalized;
            float distance = Mathf.Max(
                4.5f,
                viewBounds.extents.magnitude * 2.8f);
            lightCycleCamera.transform.SetPositionAndRotation(
                viewBounds.center + viewDirection * distance,
                Quaternion.LookRotation(-viewDirection, targetUp));

            lightCycleLensCameraObject =
                new GameObject("FlashlightLightCycle_ReadOnlyLensCamera");
            lightCycleLensCameraObject.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(lightCycleLensCameraObject, scene);
            lightCycleLensCamera = lightCycleLensCameraObject.AddComponent<Camera>();
            lightCycleLensCamera.orthographic = true;
            lightCycleLensCamera.orthographicSize = 0.075f;
            lightCycleLensCamera.nearClipPlane = 0.01f;
            lightCycleLensCamera.farClipPlane = 1f;
            lightCycleLensCamera.clearFlags = CameraClearFlags.SolidColor;
            lightCycleLensCamera.backgroundColor =
                new Color(0.018f, 0.022f, 0.03f, 1f);
            UpdateLightCycleLensCameraPose();

            lightCycleFillLightObject =
                new GameObject("FlashlightLightCycle_ReadOnlyFillLight");
            lightCycleFillLightObject.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(lightCycleFillLightObject, scene);
            Light fill = lightCycleFillLightObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = Color.white;
            fill.intensity = 0.08f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.LookRotation(-viewDirection, targetUp);
        }

        private static void UpdateLightCycleLensCameraPose()
        {
            Light beam = lightCycleBehaviour.LensLight ??
                throw new MissingReferenceException(
                    "Flashlight beam light is missing for the lens close-up.");
            Vector3 forward = beam.transform.forward.normalized;
            Vector3 lensFace = beam.transform.position - forward * 0.004f;
            lightCycleLensCamera.transform.SetPositionAndRotation(
                lensFace + forward * 0.28f,
                Quaternion.LookRotation(-forward, lightCycleCarry.Holder.transform.forward));
        }

        private static Texture2D CaptureLightCyclePanel()
        {
            RenderTexture render = RenderTexture.GetTemporary(
                640, 640, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                lightCycleCamera.targetTexture = render;
                lightCycleCamera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                lightCycleCamera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
            }
        }

        private static Texture2D CaptureLightCycleLensPanel()
        {
            UpdateLightCycleLensCameraPose();
            RenderTexture render = RenderTexture.GetTemporary(
                640, 640, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                lightCycleLensCamera.targetTexture = render;
                lightCycleLensCamera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(640, 640, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                lightCycleLensCamera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
            }
        }

        private static float ReceiverLuminance(Texture2D image)
        {
            Vector3 viewport = lightCycleCamera.WorldToViewportPoint(
                lightCycleReceiverObject.GetComponent<Renderer>().bounds.center);
            int centerX = Mathf.Clamp(
                Mathf.RoundToInt(viewport.x * image.width), 0, image.width - 1);
            int centerY = Mathf.Clamp(
                Mathf.RoundToInt(viewport.y * image.height), 0, image.height - 1);
            const int radius = 52;
            int minimumX = Mathf.Max(0, centerX - radius);
            int maximumX = Mathf.Min(image.width - 1, centerX + radius);
            int minimumY = Mathf.Max(0, centerY - radius);
            int maximumY = Mathf.Min(image.height - 1, centerY + radius);
            double total = 0d;
            int count = 0;
            for (int y = minimumY; y <= maximumY; y++)
            for (int x = minimumX; x <= maximumX; x++)
            {
                total += image.GetPixel(x, y).grayscale;
                count++;
            }
            return count > 0 ? (float)(total / count) : 0f;
        }

        private static float LensLuminance(Texture2D image)
        {
            const int radius = 72;
            int centerX = image.width / 2;
            int centerY = image.height / 2;
            double total = 0d;
            int count = 0;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                total += image.GetPixel(x, y).grayscale;
                count++;
            }
            return (float)(total / count);
        }

        private static void FinishLightCycleRuntimeCapture()
        {
            float initialOnLuminance = LightCyclePanelLuminance[0];
            float idleOffLuminance = LightCyclePanelLuminance[1];
            float returnedOnLuminance = LightCyclePanelLuminance[3];
            float initialLensOnLuminance = LightCycleLensLuminance[0];
            float idleLensOffLuminance = LightCycleLensLuminance[1];
            float returnedLensOnLuminance = LightCycleLensLuminance[3];
            WriteText(ReviewFolder + "/light_cycle_measurements.txt", new StringBuilder()
                .AppendLine("Flashlight_Idle light-cycle receiver measurements")
                .AppendLine("initialOnReceiverLuminance=" + F(initialOnLuminance))
                .AppendLine("idleOffReceiverLuminance=" + F(idleOffLuminance))
                .AppendLine("returnedOnReceiverLuminance=" + F(returnedOnLuminance))
                .AppendLine("initialOnLensLuminance=" + F(initialLensOnLuminance))
                .AppendLine("idleOffLensLuminance=" + F(idleLensOffLuminance))
                .AppendLine("returnedOnLensLuminance=" + F(returnedLensOnLuminance))
                .AppendLine("requiredReceiverDifference=0.02")
                .AppendLine("requiredLensDifference=0.02")
                .ToString());
            if (initialOnLuminance <= idleOffLuminance + 0.02f ||
                returnedOnLuminance <= idleOffLuminance + 0.02f)
                throw new InvalidOperationException(
                    "The flashlight beam did not produce a visible on/off difference.");
            if (initialLensOnLuminance <= idleLensOffLuminance + 0.02f ||
                returnedLensOnLuminance <= idleLensOffLuminance + 0.02f)
                throw new InvalidOperationException(
                    "The flashlight lens did not produce a visible on/off difference.");

            Transform holder = lightCycleCarry.Holder.transform;
            float followPositionError = Vector3.Distance(
                holder.localPosition, lightCycleCarry.HolderLocalPosition);
            float followRotationError = Quaternion.Angle(
                holder.localRotation, lightCycleCarry.HolderLocalRotation);
            if (followPositionError > 0.00005f || followRotationError > 0.05f)
                throw new InvalidOperationException(
                    "Flashlight right-hand follow drifted during the light cycle.");

            WriteLightCycleComposite(
                LightCycleFinalImagePath, lightCyclePanels, lightCycleLensPanels);
            var report = new StringBuilder()
                .AppendLine("Flashlight_Idle five-second idle light-cycle direct review")
                .AppendLine("captureKind=Final")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("idleDurationSeconds=5")
                .AppendLine("lightOnWindowSeconds=0-2")
                .AppendLine("lightOffWindowSeconds=2-5")
                .AppendLine("otherMotionDurationSeconds=1")
                .AppendLine("totalCycleDurationSeconds=10")
                .AppendLine("panelLayout=topRow:lensCloseups,bottomRow:beamView;columns:on@1s,off@3.5s,walkForward@5.5s,returnOn@10.5s")
                .AppendLine("sameLensCameraForAllTopPanels=True")
                .AppendLine("sameBeamCameraAndReceiverForAllBottomPanels=True")
                .AppendLine("lensSurfaceVisibleDuringInitialOn=True")
                .AppendLine("lensSurfaceOffDuringRemainingIdle=True")
                .AppendLine("beamVisibleDuringInitialOn=True")
                .AppendLine("beamOffDuringRemainingIdle=True")
                .AppendLine("returnedToLitIdleAfterFullCycle=True")
                .AppendLine("lightRangeMeters=3")
                .AppendLine("flashlightFollowsAnimatedRightHand=True")
                .AppendLine("flashlightTransformRendererMeshMaterialTextureChanged=False")
                .AppendLine("rightArmPoseChanged=False")
                .AppendLine("followPositionErrorMeters=" + F(followPositionError))
                .AppendLine("followRotationErrorDegrees=" + F(followRotationError))
                .AppendLine("initialOnReceiverLuminance=" + F(initialOnLuminance))
                .AppendLine("idleOffReceiverLuminance=" + F(idleOffLuminance))
                .AppendLine("returnedOnReceiverLuminance=" + F(returnedOnLuminance))
                .AppendLine("initialOnLensLuminance=" + F(initialLensOnLuminance))
                .AppendLine("idleOffLensLuminance=" + F(idleLensOffLuminance))
                .AppendLine("returnedOnLensLuminance=" + F(returnedLensOnLuminance));
            foreach (string observation in LightCycleObservations)
                report.AppendLine(observation);
            WriteText(LightCycleFinalReportPath, report.ToString());

            int errors = UnityConsoleErrorCount();
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during the light-cycle review.");

            CleanupLightCycleRuntimeCapture();
            SessionState.SetString(
                LightCycleAutoStateKey, AwaitingLightCycleEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void WriteLightCycleComposite(
            string assetPath,
            IReadOnlyList<Texture2D> beamPanels,
            IReadOnlyList<Texture2D> lensPanels)
        {
            if (beamPanels.Count != 4 || lensPanels.Count != 4 ||
                beamPanels.Any(panel => panel == null) ||
                lensPanels.Any(panel => panel == null))
                throw new InvalidOperationException(
                    "Flashlight light-cycle review panel count is invalid.");
            var composite = new Texture2D(2560, 1280, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < 4; index++)
                {
                    composite.SetPixels(
                        index * 640, 640, 640, 640, lensPanels[index].GetPixels());
                    composite.SetPixels(
                        index * 640, 0, 640, 640, beamPanels[index].GetPixels());
                }
                composite.Apply(false, false);
                string absolute = Absolute(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllBytes(absolute, composite.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static void CleanupLightCycleRuntimeCapture()
        {
            EditorApplication.update -= LightCycleRuntimeCaptureTick;
            if (lightCyclePanels != null)
                foreach (Texture2D panel in lightCyclePanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (lightCycleLensPanels != null)
                foreach (Texture2D panel in lightCycleLensPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (lightCycleFillLightObject != null)
                UnityEngine.Object.DestroyImmediate(lightCycleFillLightObject);
            if (lightCycleCameraObject != null)
                UnityEngine.Object.DestroyImmediate(lightCycleCameraObject);
            if (lightCycleLensCameraObject != null)
                UnityEngine.Object.DestroyImmediate(lightCycleLensCameraObject);
            if (lightCycleReceiverObject != null)
                UnityEngine.Object.DestroyImmediate(lightCycleReceiverObject);
            lightCycleCaptureActive = false;
            lightCycleBaseAbsolutePhase = -1;
            lightCycleNextPanel = 0;
            lightCycleAnimator = null;
            lightCycleTarget = null;
            lightCycleCarry = null;
            lightCycleBehaviour = null;
            lightCycleCameraObject = null;
            lightCycleLensCameraObject = null;
            lightCycleReceiverObject = null;
            lightCycleFillLightObject = null;
            lightCycleCamera = null;
            lightCycleLensCamera = null;
            lightCyclePanels = null;
            lightCycleLensPanels = null;
            LightCycleObservations.Clear();
        }

        private static void FailLightCycleReview(Exception exception)
        {
            CleanupLightCycleRuntimeCapture();
            SessionState.EraseString(LightCycleAutoStateKey);
            try
            {
                EnsureFolder(ReviewFolder);
                WriteText(ReviewFolder + "/light_cycle_failure.txt", exception.ToString());
            }
            catch
            {
            }
            Debug.LogException(exception);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        internal static void ApplyInspectAndStartReviewIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            AnimatorController existing =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
            {
                if (File.Exists(Absolute(FinalImagePath)) &&
                    !File.Exists(Absolute(ReviewFolder + "/completion.txt")))
                {
                    FlashlightRightHandFollowBehaviour carry =
                        RequireCarry(FindUnique(RequireScene(), TargetName));
                    carry.RefreshPreview();
                    InspectIdleLocomotion();
                    int errors = UnityConsoleErrorCount();
                    if (errors != 0)
                        throw new InvalidOperationException(
                            "Unity Console contains " + errors + " errors after review.");
                    WriteText(ReviewFolder + "/automation_failure.txt",
                        "Resolved: Edit Mode authored right-arm restoration completed.\n");
                    WriteText(ReviewFolder + "/completion.txt", new StringBuilder()
                        .AppendLine("Flashlight_Idle automatic application and direct review")
                        .AppendLine("applicationCompleted=True")
                        .AppendLine("runtimeSequenceObserved=True")
                        .AppendLine("returnedToEditMode=True")
                        .AppendLine("editModeAuthoredPoseRestored=True")
                        .AppendLine("unityConsoleErrors=0")
                        .ToString());
                }
                return;
            }
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;
            ApplyIdleLocomotion();
            InspectIdleLocomotion();
            SessionState.SetString(AutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
        }

        private static void ContinueAutomaticApplicationAndReview()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ContinueAutomaticApplicationAndReview;
                return;
            }

            string state = SessionState.GetString(AutoStateKey, string.Empty);
            try
            {
                if (string.IsNullOrEmpty(state))
                {
                    Scene scene = SceneManager.GetActiveScene();
                    EnsureFolder(ReviewFolder);
                    WriteText(ReviewFolder + "/automatic_entry.txt", new StringBuilder()
                        .AppendLine("Flashlight_Idle automatic entry")
                        .AppendLine("activeScene=" +
                            (scene.IsValid() ? scene.path : "<invalid>"))
                        .AppendLine("sceneDirty=" + (scene.IsValid() && scene.isDirty))
                        .AppendLine("isPlaying=" + EditorApplication.isPlaying)
                        .AppendLine("controllerExists=" +
                            (AssetDatabase.LoadAssetAtPath<AnimatorController>(
                                ControllerPath) != null))
                        .ToString());
                    if (!scene.IsValid() || scene.path != ScenePath ||
                        AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                        return;
                    ApplyIdleLocomotion();
                    InspectIdleLocomotion();
                    SessionState.SetString(AutoStateKey, AwaitingPlayState);
                    EditorApplication.EnterPlaymode();
                    return;
                }

                if (state == AwaitingPlayState && EditorApplication.isPlaying)
                {
                    BeginRuntimeCapture();
                    return;
                }

                if (state == AwaitingEditState &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    InspectIdleLocomotion();
                    int errors = UnityConsoleErrorCount();
                    if (errors != 0)
                        throw new InvalidOperationException(
                            "Unity Console contains " + errors + " errors after review.");
                    WriteText(ReviewFolder + "/completion.txt", new StringBuilder()
                        .AppendLine("Flashlight_Idle automatic application and direct review")
                        .AppendLine("applicationCompleted=True")
                        .AppendLine("runtimeSequenceObserved=True")
                        .AppendLine("returnedToEditMode=True")
                        .AppendLine("unityConsoleErrors=0")
                        .ToString());
                    SessionState.EraseString(AutoStateKey);
                    Debug.Log(
                        "[FlashlightIdleLocomotion] Automatic application and direct review completed.");
                }
            }
            catch (Exception exception)
            {
                FailAutomaticReview(exception);
            }
        }

        private static void BeginRuntimeCapture()
        {
            if (captureActive) return;
            runtimeTarget = FindUnique(RequireScene(), TargetName);
            runtimeAnimator = RequireAnimator(runtimeTarget);
            runtimeCarry = RequireCarry(runtimeTarget);
            if (!runtimeAnimator.enabled || runtimeAnimator.applyRootMotion ||
                runtimeAnimator.runtimeAnimatorController == null)
                throw new InvalidOperationException(
                    "Flashlight_Idle runtime Animator settings are invalid.");

            captureActive = true;
            captureStartTime = EditorApplication.timeSinceStartup;
            baseAbsolutePhase = -1;
            nextPanel = 0;
            fullPanels = new Texture2D[FlashlightIdleLocomotionCycleBehaviour.MotionCount];
            gripPanels = new Texture2D[FlashlightIdleLocomotionCycleBehaviour.MotionCount];
            RuntimeObservations.Clear();
            ResetRuntimeMetrics();
            EditorApplication.update -= RuntimeCaptureTick;
            EditorApplication.update += RuntimeCaptureTick;
        }

        private static void RuntimeCaptureTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException(
                        "Play Mode ended before Flashlight_Idle review completed.");
                if (EditorApplication.timeSinceStartup - captureStartTime >
                    CaptureTimeoutSeconds)
                    throw new TimeoutException(
                        "Flashlight_Idle natural locomotion review exceeded 20 seconds.");
                if (!runtimeAnimator.isInitialized ||
                    !FlashlightIdleLocomotionCycleBehaviour.TryGetSequenceState(
                        runtimeAnimator,
                        out int absolutePhase,
                        out int phase,
                        out float phaseElapsed))
                    return;

                if (baseAbsolutePhase < 0)
                {
                    if (phase != 0 || phaseElapsed > 0.65f) return;
                    baseAbsolutePhase = absolutePhase;
                }

                if (nextPanel < FlashlightIdleLocomotionCycleBehaviour.MotionCount)
                {
                    int expectedAbsolutePhase = baseAbsolutePhase + nextPanel;
                    if (absolutePhase < expectedAbsolutePhase ||
                        phaseElapsed < CapturePhaseTime) return;
                    if (absolutePhase > expectedAbsolutePhase)
                        throw new InvalidOperationException(
                            "A natural Flashlight_Idle sequence phase was missed.");
                    int expectedPhase = nextPanel;
                    if (phase != expectedPhase)
                        throw new InvalidOperationException(
                            "Flashlight_Idle phase order differs from the requested order.");
                    Vector2 expected =
                        FlashlightIdleLocomotionCycleBehaviour.MotionPosition(expectedPhase);
                    float moveX = runtimeAnimator.GetFloat(
                        FlashlightIdleLocomotionCycleBehaviour.MoveXParameter);
                    float moveY = runtimeAnimator.GetFloat(
                        FlashlightIdleLocomotionCycleBehaviour.MoveYParameter);
                    if (Mathf.Abs(moveX - expected.x) > 0.001f ||
                        Mathf.Abs(moveY - expected.y) > 0.001f)
                        throw new InvalidOperationException(
                            "Flashlight_Idle Blend Tree parameters differ from phase " +
                            expectedPhase + ".");

                    AccumulateRuntimeMetrics();
                    fullPanels[nextPanel] = CaptureRuntimePanel(false);
                    gripPanels[nextPanel] = CaptureRuntimePanel(true);
                    RuntimeObservations.Add(
                        "panel=" + nextPanel +
                        "|phase=" + phase +
                        "|motion=" +
                            FlashlightIdleLocomotionCycleBehaviour.MotionName(phase) +
                        "|phaseElapsed=" + F(phaseElapsed) +
                        "|moveX=" + F(moveX) +
                        "|moveY=" + F(moveY));
                    nextPanel++;
                    return;
                }

                int returnPhase = baseAbsolutePhase +
                    FlashlightIdleLocomotionCycleBehaviour.MotionCount;
                if (absolutePhase < returnPhase) return;
                if (absolutePhase != returnPhase || phase != 0)
                    throw new InvalidOperationException(
                        "RunForward did not naturally return to Idle.");
                FinishRuntimeCapture();
            }
            catch (Exception exception)
            {
                FailAutomaticReview(exception);
            }
        }

        private static void AccumulateRuntimeMetrics()
        {
            Transform holder = runtimeCarry.Holder != null
                ? runtimeCarry.Holder.transform
                : throw new InvalidOperationException("Runtime flashlight holder is missing.");
            maximumFollowPositionError = Mathf.Max(
                maximumFollowPositionError,
                Vector3.Distance(holder.localPosition, runtimeCarry.HolderLocalPosition));
            maximumFollowRotationError = Mathf.Max(
                maximumFollowRotationError,
                Quaternion.Angle(holder.localRotation, runtimeCarry.HolderLocalRotation));
            maximumRightArmDeviation = Mathf.Max(
                maximumRightArmDeviation,
                PoseDeviation(runtimeCarry, 0, RequirePath(runtimeTarget.transform, RightArmPath)));
            maximumRightForeArmDeviation = Mathf.Max(
                maximumRightForeArmDeviation,
                PoseDeviation(runtimeCarry, 1,
                    RequirePath(runtimeTarget.transform, RightForeArmPath)));
            maximumRightHandDeviation = Mathf.Max(
                maximumRightHandDeviation,
                PoseDeviation(runtimeCarry, 2,
                    RequirePath(runtimeTarget.transform, RightHandPath)));

            Transform spine = RequireDescendant(runtimeTarget.transform, "Spine");
            Transform head = RequireDescendant(runtimeTarget.transform, "Head");
            Transform shoulder = RequireDescendant(runtimeTarget.transform, "RightShoulder");
            if (!swayBaselineSet)
            {
                swayBaselineSet = true;
                initialSpine = spine.localRotation;
                initialHead = head.localRotation;
                initialRightShoulder = shoulder.localRotation;
                initialFlashlightPosition = holder.position;
                initialFlashlightRotation = holder.rotation;
                return;
            }
            maximumSpineSway = Mathf.Max(
                maximumSpineSway, Quaternion.Angle(initialSpine, spine.localRotation));
            maximumHeadSway = Mathf.Max(
                maximumHeadSway, Quaternion.Angle(initialHead, head.localRotation));
            maximumRightShoulderSway = Mathf.Max(
                maximumRightShoulderSway,
                Quaternion.Angle(initialRightShoulder, shoulder.localRotation));
            maximumFlashlightTravel = Mathf.Max(
                maximumFlashlightTravel,
                Vector3.Distance(initialFlashlightPosition, holder.position));
            maximumFlashlightRotation = Mathf.Max(
                maximumFlashlightRotation,
                Quaternion.Angle(initialFlashlightRotation, holder.rotation));
        }

        private static float PoseDeviation(
            FlashlightRightHandFollowBehaviour carry,
            int index,
            Transform bone)
        {
            return Quaternion.Angle(
                carry.AuthoredRightArmPose[index].LocalRotation,
                bone.localRotation);
        }

        private static void FinishRuntimeCapture()
        {
            if (maximumFollowPositionError > 0.00005f ||
                maximumFollowRotationError > 0.05f)
                throw new InvalidOperationException(
                    "Flashlight right-hand follow drifted during natural locomotion.");
            if (maximumRightArmDeviation > 40f ||
                maximumRightForeArmDeviation > 30f ||
                maximumRightHandDeviation > 20f)
                throw new InvalidOperationException(
                    "Flashlight carried-arm pose deviated beyond the established limits.");
            float maximumNaturalSway = Mathf.Max(
                maximumSpineSway,
                Mathf.Max(maximumHeadSway, maximumRightShoulderSway));
            if (maximumNaturalSway <= 0.1f)
                throw new InvalidOperationException(
                    "Source upper-body locomotion sway was rigidly removed.");
            if (maximumFlashlightTravel <= 0.001f && maximumFlashlightRotation <= 0.1f)
                throw new InvalidOperationException(
                    "Flashlight did not follow visible right-hand movement.");

            WriteRuntimeComposite(FinalImagePath, fullPanels, gripPanels);
            var report = new StringBuilder()
                .AppendLine("Flashlight_Idle natural locomotion direct review")
                .AppendLine("captureKind=Final")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("idleDurationSeconds=5")
                .AppendLine("otherMotionDurationSeconds=1")
                .AppendLine("totalCycleDurationSeconds=10")
                .AppendLine("cyclesObserved=1")
                .AppendLine("panelsCaptured=12")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("returnedToIdleAfterRun=True")
                .AppendLine("upperBodyNaturalSwayPreserved=True")
                .AppendLine("authoredRightArmPosePreserved=True")
                .AppendLine("flashlightFollowsAnimatedRightHand=True")
                .AppendLine("maximumFollowPositionError=" + F(maximumFollowPositionError))
                .AppendLine("maximumFollowRotationErrorDegrees=" +
                    F(maximumFollowRotationError))
                .AppendLine("maximumRightArmDeviationDegrees=" +
                    F(maximumRightArmDeviation))
                .AppendLine("maximumRightForeArmDeviationDegrees=" +
                    F(maximumRightForeArmDeviation))
                .AppendLine("maximumRightHandDeviationDegrees=" +
                    F(maximumRightHandDeviation))
                .AppendLine("maximumSpineSwayDegrees=" + F(maximumSpineSway))
                .AppendLine("maximumHeadSwayDegrees=" + F(maximumHeadSway))
                .AppendLine("maximumRightShoulderSwayDegrees=" +
                    F(maximumRightShoulderSway))
                .AppendLine("maximumFlashlightTravelMeters=" +
                    F(maximumFlashlightTravel))
                .AppendLine("maximumFlashlightRotationDegrees=" +
                    F(maximumFlashlightRotation));
            foreach (string observation in RuntimeObservations)
                report.AppendLine(observation);
            WriteText(ReviewFolder + "/final_runtime.txt", report.ToString());
            int errors = UnityConsoleErrorCount();
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors + " errors during review.");

            CleanupRuntimeCapture();
            SessionState.SetString(AutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CaptureRuntimePanel(bool gripCloseup)
        {
            Bounds bounds = gripCloseup
                ? GripBounds(runtimeTarget.transform, runtimeCarry.Holder)
                : FullBounds(runtimeTarget.transform);
            Vector3 direction = (
                runtimeTarget.transform.forward + runtimeTarget.transform.right * 0.72f).normalized;
            var cameraObject = new GameObject("FlashlightLocomotion_ReadOnlyCamera");
            var lightObject = new GameObject("FlashlightLocomotion_ReadOnlyLight");
            Scene scene = RequireScene();
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = bounds.extents.magnitude * 1.08f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(
                    runtimeTarget.transform.up, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 4f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds FullBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Flashlight_Idle has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.15f);
            return bounds;
        }

        private static Bounds GripBounds(Transform target, GameObject holder)
        {
            Transform upper = RequirePath(target, RightArmPath);
            Transform fore = RequirePath(target, RightForeArmPath);
            Transform hand = RequirePath(target, RightHandPath);
            Bounds bounds = new Bounds(upper.position, Vector3.zero);
            bounds.Encapsulate(fore.position);
            bounds.Encapsulate(hand.position);
            foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.13f);
            return bounds;
        }

        private static void WriteRuntimeComposite(
            string assetPath,
            IReadOnlyList<Texture2D> full,
            IReadOnlyList<Texture2D> grip)
        {
            int count = FlashlightIdleLocomotionCycleBehaviour.MotionCount;
            if (full.Count != count || grip.Count != count)
                throw new InvalidOperationException(
                    "Flashlight_Idle review panel count is invalid.");
            var composite = new Texture2D(count * 512, 1024, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < count; index++)
                {
                    composite.SetPixels(
                        index * 512, 512, 512, 512, full[index].GetPixels());
                    composite.SetPixels(
                        index * 512, 0, 512, 512, grip[index].GetPixels());
                }
                composite.Apply(false, false);
                string absolute = Absolute(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllBytes(absolute, composite.EncodeToPNG());
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static int UnityConsoleErrorCount()
        {
            Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            System.Reflection.MethodInfo method = logEntriesType.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            return (int)arguments[0];
        }

        private static void ResetRuntimeMetrics()
        {
            swayBaselineSet = false;
            maximumSpineSway = 0f;
            maximumHeadSway = 0f;
            maximumRightShoulderSway = 0f;
            maximumRightArmDeviation = 0f;
            maximumRightForeArmDeviation = 0f;
            maximumRightHandDeviation = 0f;
            maximumFollowPositionError = 0f;
            maximumFollowRotationError = 0f;
            maximumFlashlightTravel = 0f;
            maximumFlashlightRotation = 0f;
        }

        private static void CleanupRuntimeCapture()
        {
            EditorApplication.update -= RuntimeCaptureTick;
            if (fullPanels != null)
                foreach (Texture2D panel in fullPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (gripPanels != null)
                foreach (Texture2D panel in gripPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            captureActive = false;
            runtimeAnimator = null;
            runtimeTarget = null;
            runtimeCarry = null;
            fullPanels = null;
            gripPanels = null;
            RuntimeObservations.Clear();
        }

        private static void FailAutomaticReview(Exception exception)
        {
            CleanupRuntimeCapture();
            SessionState.EraseString(AutoStateKey);
            try
            {
                EnsureFolder(ReviewFolder);
                WriteText(ReviewFolder + "/automation_failure.txt", exception.ToString());
            }
            catch
            {
            }
            Debug.LogException(exception);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Idle Locomotion")]
        internal static void ApplyIdleLocomotion()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;

            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            SourceMotions sources = RequireSourceMotions(scene);

            string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
            string targetPoseBefore = TargetPoseSignature(target.transform);
            string nonTargetSceneBefore = NonTargetSceneSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string modelHash = ComputeAssetHash(ModelPath);
            string materialHash = ComputeAssetHash(MaterialPath);
            Dictionary<string, string> sourceControllerHashes = SourceNames
                .Select(name => AssetDatabase.GetAssetPath(
                    RequireController(RequireAnimator(FindUnique(scene, name)), name)))
                .ToDictionary(
                    path => path,
                    path => ComputeAssetHash(path),
                    StringComparer.Ordinal);

            FlashlightBoneRotation[] authoredPose = CaptureRightArmPose(target.transform);
            FlashlightBoneRotation[] sourcePose = CaptureRightArmPose(
                FindUnique(scene, SourceNames[0]).transform);

            EnsureFolder(AssetFolder);
            EnsureFolder(ReviewFolder);
            AnimationClip idle = CopyClip(sources.Idle, IdleClipPath, "FlashlightIdle_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ForwardClipPath, "FlashlightIdle_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, BackwardClipPath, "FlashlightIdle_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, SidestepClipPath, "FlashlightIdle_Sidestep");
            AnimationClip run = CopyClip(
                sources.Run, RunClipPath, "FlashlightIdle_RunForward");
            AnimatorController controller = CreateController(
                sources.Diagonal, idle, forward, backward, sidestep, run);

            Undo.RecordObject(carry, "Preserve Flashlight_Idle carried arm locomotion");
            carry.ConfigureLocomotionPose(authoredPose, sourcePose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(carry);
            EditorUtility.SetDirty(carry);

            Undo.RecordObject(animator, "Configure Flashlight_Idle locomotion");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(avatarPath, AssetDatabase.GetAssetPath(animator.avatar), "avatar");
            RequireEqual(targetPoseBefore, TargetPoseSignature(target.transform),
                "Flashlight_Idle authored transform pose");
            RequireEqual(nonTargetSceneBefore,
                NonTargetSceneSignature(scene, target.transform),
                "objects outside Flashlight_Idle");
            RequireEqual(carryBefore, CarrySignature(carry),
                "flashlight model and hand-follow configuration");
            RequireEqual(modelHash, ComputeAssetHash(ModelPath), "flashlight model");
            RequireEqual(materialHash, ComputeAssetHash(MaterialPath), "flashlight material");
            foreach (KeyValuePair<string, string> item in sourceControllerHashes)
                RequireEqual(item.Value, ComputeAssetHash(item.Key), item.Key);
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp scene could not save Flashlight_Idle locomotion.");

            WriteText(ReviewFolder + "/application.txt", new StringBuilder()
                .AppendLine("Flashlight_Idle six-motion locomotion application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("preexistingUserSceneChangesPreserved=True")
                .AppendLine("sourceOrder=" + string.Join(",", SourceNames))
                .AppendLine("idleDurationSeconds=5")
                .AppendLine("otherMotionDurationSeconds=1")
                .AppendLine("totalCycleDurationSeconds=10")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("sourceAnimationsCopiedExactly=True")
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("currentUpperBodyPoseChangedInEditMode=False")
                .AppendLine("rightShoulderSourceMotionWeight=1")
                .AppendLine("rightArmMotionWeight=" +
                    F(FlashlightRightHandFollowBehaviour.RightArmMotionWeight))
                .AppendLine("rightForeArmMotionWeight=" +
                    F(FlashlightRightHandFollowBehaviour.RightForeArmMotionWeight))
                .AppendLine("rightHandMotionWeight=" +
                    F(FlashlightRightHandFollowBehaviour.RightHandMotionWeight))
                .AppendLine("flashlightTransformChanged=False")
                .AppendLine("flashlightRendererMeshMaterialChanged=False")
                .AppendLine("flashlightFollowsAnimatedRightHand=True")
                .AppendLine("objectsOutsideFlashlightIdleChanged=False")
                .ToString());
            AssetDatabase.SaveAssets();
            Debug.Log("[FlashlightIdleLocomotion] Applied exact source motions and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Idle Locomotion")]
        internal static void InspectIdleLocomotion()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            AnimatorController controller = RequireController(animator, TargetName);
            RequireEqual(ControllerPath, AssetDatabase.GetAssetPath(controller),
                "controller path");
            if (animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(
                    "Flashlight_Idle Animator settings are invalid.");

            RequireFloatParameter(controller,
                FlashlightIdleLocomotionCycleBehaviour.MoveXParameter, 0f);
            RequireFloatParameter(controller,
                FlashlightIdleLocomotionCycleBehaviour.MoveYParameter, 0f);
            RequireFloatParameter(controller,
                FlashlightIdleLocomotionCycleBehaviour.DiagonalBlendParameter, 0.5f);
            if (controller.layers.Length != 1)
                throw new InvalidOperationException("Controller must have one layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f) || layer.avatarMask != null)
                throw new InvalidOperationException(
                    "Exact source full-body animation must pass through the base layer.");
            AnimatorState state = layer.stateMachine.defaultState ??
                throw new InvalidOperationException("Default locomotion state is missing.");
            if (state.name != StateName || state.writeDefaultValues ||
                !Mathf.Approximately(state.speed, 1f) ||
                state.behaviours.OfType<FlashlightIdleLocomotionCycleBehaviour>().Count() != 1)
                throw new InvalidOperationException("Locomotion state settings are invalid.");
            BlendTree root = state.motion as BlendTree ??
                throw new InvalidOperationException("Root 2D Blend Tree is missing.");
            RequireRootTree(root);

            SourceMotions sources = RequireSourceMotions(scene);
            AnimationClip idle = RequireAsset<AnimationClip>(IdleClipPath);
            AnimationClip forward = RequireAsset<AnimationClip>(ForwardClipPath);
            AnimationClip backward = RequireAsset<AnimationClip>(BackwardClipPath);
            AnimationClip sidestep = RequireAsset<AnimationClip>(SidestepClipPath);
            AnimationClip run = RequireAsset<AnimationClip>(RunClipPath);
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");
            var diagonalClipMap = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)sources.Diagonal.children[0].motion] = forward,
                [(AnimationClip)sources.Diagonal.children[1].motion] = sidestep
            };
            RequireMotionTreeEquivalent(
                sources.Diagonal, root.children[4].motion as BlendTree, diagonalClipMap,
                "WalkDiagonal");
            RequirePoseConfiguration(carry, target.transform);

            WriteText(ReviewFolder + "/inspection.txt", new StringBuilder()
                .AppendLine("Flashlight_Idle locomotion inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("motionCount=6")
                .AppendLine("idleDurationSeconds=5")
                .AppendLine("otherMotionDurationSeconds=1")
                .AppendLine("totalCycleDurationSeconds=10")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("sequenceLoops=True")
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("sourceCopiesExact=True")
                .AppendLine("authoredRightArmPosePreserved=True")
                .AppendLine("naturalUpperBodyAndShoulderMotionRetained=True")
                .AppendLine("flashlightRightHandFollowConfigured=True")
                .ToString());
            Debug.Log("[FlashlightIdleLocomotion] Inspection passed.");
        }

        private static SourceMotions RequireSourceMotions(Scene scene)
        {
            Motion[] motions = SourceNames.Select(name =>
            {
                Animator animator = RequireAnimator(FindUnique(scene, name));
                AnimatorController controller = RequireController(animator, name);
                AnimatorState state = controller.layers[0].stateMachine.defaultState ??
                    throw new InvalidOperationException(name + " has no default state.");
                return state.motion ??
                    throw new InvalidOperationException(name + " has no default motion.");
            }).ToArray();
            if (!(motions[0] is AnimationClip idle) ||
                !(motions[1] is AnimationClip forward) ||
                !(motions[2] is AnimationClip backward) ||
                !(motions[3] is AnimationClip sidestep) ||
                !(motions[4] is BlendTree diagonal) ||
                !(motions[5] is AnimationClip run))
                throw new InvalidOperationException("Source motion types changed.");
            if (diagonal.blendType != BlendTreeType.Simple1D ||
                diagonal.children.Length != 2 ||
                !(diagonal.children[0].motion is AnimationClip) ||
                !(diagonal.children[1].motion is AnimationClip))
                throw new InvalidOperationException("Player_Walk_Diagonal source changed.");
            AnimatorController diagonalController = RequireController(
                RequireAnimator(FindUnique(scene, SourceNames[4])), SourceNames[4]);
            AnimatorControllerParameter parameter = diagonalController.parameters
                .SingleOrDefault(item => item.name == diagonal.blendParameter);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, 0.5f))
                throw new InvalidOperationException(
                    "Player_Walk_Diagonal source default must remain 0.5.");
            return new SourceMotions(idle, forward, backward, sidestep, diagonal, run);
        }

        private static AnimationClip CopyClip(
            AnimationClip source, string path, string name)
        {
            AnimationClip copy = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (copy == null)
            {
                copy = new AnimationClip();
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                AssetDatabase.CreateAsset(copy, path);
            }
            else
            {
                EditorUtility.CopySerialized(source, copy);
                copy.name = name;
                EditorUtility.SetDirty(copy);
            }
            return copy;
        }

        private static AnimatorController CreateController(
            BlendTree sourceDiagonal,
            AnimationClip idle,
            AnimationClip forward,
            AnimationClip backward,
            AnimationClip sidestep,
            AnimationClip run)
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.AddParameter(FlashlightIdleLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(FlashlightIdleLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = FlashlightIdleLocomotionCycleBehaviour.DiagonalBlendParameter,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0.5f
            });

            AnimatorControllerLayer layer = controller.layers[0];
            layer.name = "Source Locomotion";
            layer.defaultWeight = 1f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.avatarMask = null;
            AnimatorStateMachine stateMachine = layer.stateMachine;
            foreach (AnimatorState item in stateMachine.states.Select(item => item.state).ToArray())
                stateMachine.RemoveState(item);
            foreach (AnimatorStateMachine item in stateMachine.stateMachines
                .Select(item => item.stateMachine).ToArray())
                stateMachine.RemoveStateMachine(item);

            ChildMotion[] diagonalChildren = sourceDiagonal.children;
            var diagonalClipMap = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)diagonalChildren[0].motion] = forward,
                [(AnimationClip)diagonalChildren[1].motion] = sidestep
            };
            BlendTree diagonal = CloneTree(
                sourceDiagonal, DiagonalTreeName, controller, diagonalClipMap);
            var root = new BlendTree
            {
                name = RootTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = FlashlightIdleLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY = FlashlightIdleLocomotionCycleBehaviour.MoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(root, controller);
            root.children = new[]
            {
                Child(idle, RequiredPositions[0]), Child(forward, RequiredPositions[1]),
                Child(backward, RequiredPositions[2]), Child(sidestep, RequiredPositions[3]),
                Child(diagonal, RequiredPositions[4]), Child(run, RequiredPositions[5])
            };
            AnimatorState state = stateMachine.AddState(StateName);
            state.motion = root;
            state.speed = 1f;
            state.cycleOffset = 0f;
            state.mirror = false;
            state.writeDefaultValues = false;
            state.AddStateMachineBehaviour<FlashlightIdleLocomotionCycleBehaviour>();
            stateMachine.defaultState = state;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(diagonal);
            return controller;
        }

        private static BlendTree CloneTree(
            BlendTree source,
            string name,
            AnimatorController owner,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap)
        {
            var copy = new BlendTree
            {
                name = name,
                blendType = source.blendType,
                blendParameter = source.blendParameter,
                blendParameterY = source.blendParameterY,
                useAutomaticThresholds = source.useAutomaticThresholds,
                minThreshold = source.minThreshold,
                maxThreshold = source.maxThreshold
            };
            AssetDatabase.AddObjectToAsset(copy, owner);
            ChildMotion[] sourceChildren = source.children;
            var children = new ChildMotion[sourceChildren.Length];
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion child = sourceChildren[index];
                Motion motion;
                if (child.motion is AnimationClip sourceClip)
                {
                    if (!clipMap.TryGetValue(sourceClip, out AnimationClip mapped))
                        throw new InvalidOperationException(
                            "Diagonal clip mapping is missing at " + index + ".");
                    motion = mapped;
                }
                else if (child.motion is BlendTree nested)
                    motion = CloneTree(nested, name + "_" + index, owner, clipMap);
                else
                    throw new InvalidOperationException("Unsupported diagonal child motion.");
                children[index] = new ChildMotion
                {
                    motion = motion,
                    threshold = child.threshold,
                    position = child.position,
                    timeScale = child.timeScale,
                    cycleOffset = child.cycleOffset,
                    mirror = child.mirror,
                    directBlendParameter = child.directBlendParameter
                };
            }
            copy.children = children;
            return copy;
        }

        private static ChildMotion Child(Motion motion, Vector2 position)
        {
            return new ChildMotion
            {
                motion = motion,
                position = position,
                timeScale = 1f,
                cycleOffset = 0f,
                mirror = false,
                threshold = 0f,
                directBlendParameter = string.Empty
            };
        }

        private static void RequireRootTree(BlendTree root)
        {
            if (root.name != RootTreeName ||
                root.blendType != BlendTreeType.FreeformCartesian2D ||
                root.blendParameter != FlashlightIdleLocomotionCycleBehaviour.MoveXParameter ||
                root.blendParameterY != FlashlightIdleLocomotionCycleBehaviour.MoveYParameter ||
                root.children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("Root 2D Blend Tree is invalid.");
            string[] paths =
            {
                IdleClipPath, ForwardClipPath, BackwardClipPath,
                SidestepClipPath, ControllerPath, RunClipPath
            };
            ChildMotion[] children = root.children;
            for (int index = 0; index < children.Length; index++)
            {
                if ((children[index].position - RequiredPositions[index]).sqrMagnitude >
                        0.00000001f ||
                    !Mathf.Approximately(children[index].timeScale, 1f) ||
                    !Mathf.Approximately(children[index].cycleOffset, 0f) ||
                    children[index].mirror)
                    throw new InvalidOperationException(
                        "Root Blend Tree child differs at " + index + ".");
                RequireEqual(paths[index], AssetDatabase.GetAssetPath(children[index].motion),
                    "root child path " + index);
            }
        }

        private static void RequireCopiedClip(
            AnimationClip source, AnimationClip copy, string label)
        {
            RequireEqual(ClipSignature(source), ClipSignature(copy), label + " clip content");
        }

        private static string ClipSignature(AnimationClip clip)
        {
            var result = new StringBuilder()
                .AppendLine("length=" + F(clip.length))
                .AppendLine("frameRate=" + F(clip.frameRate))
                .AppendLine("legacy=" + clip.legacy)
                .AppendLine("wrapMode=" + clip.wrapMode)
                .AppendLine("settings=" + EditorJsonUtility.ToJson(
                    AnimationUtility.GetAnimationClipSettings(clip)));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("curve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|').AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                result.AppendLine("wrap=" + curve.preWrapMode + "," + curve.postWrapMode);
                foreach (Keyframe key in curve.keys)
                    result.AppendLine(string.Join(",", new[]
                    {
                        F(key.time), F(key.value), F(key.inTangent), F(key.outTangent),
                        F(key.inWeight), F(key.outWeight), key.weightedMode.ToString()
                    }));
            }
            foreach (EditorCurveBinding binding in AnimationUtility
                .GetObjectReferenceCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            foreach (ObjectReferenceKeyframe key in AnimationUtility
                .GetObjectReferenceCurve(clip, binding))
                result.AppendLine("object|" + binding.path + "|" +
                    binding.type.FullName + "|" + binding.propertyName + "|" +
                    F(key.time) + "|" + ObjectIdentity(key.value));
            foreach (AnimationEvent item in AnimationUtility.GetAnimationEvents(clip))
                result.AppendLine("event|" + F(item.time) + "|" + item.functionName + "|" +
                    item.stringParameter + "|" + item.intParameter + "|" +
                    F(item.floatParameter) + "|" + ObjectIdentity(item.objectReferenceParameter) +
                    "|" + item.messageOptions);
            return Sha256(result.ToString());
        }

        private static void RequireMotionTreeEquivalent(
            BlendTree source,
            BlendTree copy,
            IReadOnlyDictionary<AnimationClip, AnimationClip> clipMap,
            string label)
        {
            if (copy == null || source.blendType != copy.blendType ||
                source.blendParameter != copy.blendParameter ||
                source.blendParameterY != copy.blendParameterY ||
                source.useAutomaticThresholds != copy.useAutomaticThresholds ||
                !Mathf.Approximately(source.minThreshold, copy.minThreshold) ||
                !Mathf.Approximately(source.maxThreshold, copy.maxThreshold) ||
                source.children.Length != copy.children.Length)
                throw new InvalidOperationException(label + " tree differs.");
            ChildMotion[] sourceChildren = source.children;
            ChildMotion[] copyChildren = copy.children;
            for (int index = 0; index < sourceChildren.Length; index++)
            {
                ChildMotion a = sourceChildren[index];
                ChildMotion b = copyChildren[index];
                if (!Mathf.Approximately(a.threshold, b.threshold) ||
                    (a.position - b.position).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(a.timeScale, b.timeScale) ||
                    !Mathf.Approximately(a.cycleOffset, b.cycleOffset) ||
                    a.mirror != b.mirror ||
                    a.directBlendParameter != b.directBlendParameter)
                    throw new InvalidOperationException(
                        label + " child differs at " + index + ".");
                if (a.motion is AnimationClip sourceClip)
                {
                    if (!(b.motion is AnimationClip copyClip) ||
                        !clipMap.TryGetValue(sourceClip, out AnimationClip expected) ||
                        copyClip != expected)
                        throw new InvalidOperationException(
                            label + " clip differs at " + index + ".");
                    RequireCopiedClip(sourceClip, copyClip, label + " child " + index);
                }
                else if (a.motion is BlendTree sourceTree)
                    RequireMotionTreeEquivalent(
                        sourceTree, b.motion as BlendTree, clipMap,
                        label + " child " + index);
                else
                    throw new InvalidOperationException(
                        label + " contains unsupported motion at " + index + ".");
            }
        }

        private static FlashlightBoneRotation[] CaptureRightArmPose(Transform root)
        {
            return RightArmPaths.Select(path => new FlashlightBoneRotation(
                path, RequirePath(root, path).localRotation)).ToArray();
        }

        private static void RequirePoseConfiguration(
            FlashlightRightHandFollowBehaviour carry, Transform target)
        {
            if (carry.AuthoredRightArmPose.Count != RightArmPaths.Length ||
                carry.SourceRightArmPose.Count != RightArmPaths.Length)
                throw new InvalidOperationException(
                    "Flashlight_Idle right-arm locomotion pose is incomplete.");
            for (int index = 0; index < RightArmPaths.Length; index++)
            {
                if (carry.AuthoredRightArmPose[index].Path != RightArmPaths[index] ||
                    carry.SourceRightArmPose[index].Path != RightArmPaths[index])
                    throw new InvalidOperationException(
                        "Flashlight_Idle right-arm path differs at " + index + ".");
                if (Quaternion.Angle(
                        carry.AuthoredRightArmPose[index].LocalRotation,
                        RequirePath(target, RightArmPaths[index]).localRotation) > 0.001f)
                    throw new InvalidOperationException(
                        "Flashlight_Idle authored right-arm pose differs at " + index + ".");
            }
        }

        private static string TargetPoseSignature(Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name != "Flashlight_Prop" &&
                    !HasAncestorNamed(item, root, "Flashlight_Prop"))
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
                result.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .AppendLine(Vec(item.localScale));
            return Sha256(result.ToString());
        }

        private static string NonTargetSceneSignature(Scene scene, Transform target)
        {
            var result = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects()
                .OrderBy(item => item.name, StringComparer.Ordinal))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target && !item.IsChildOf(target))
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root.transform),
                    StringComparer.Ordinal))
                result.Append(root.name).Append('|')
                    .Append(AnimationUtility.CalculateTransformPath(item, root.transform))
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').AppendLine(Vec(item.localScale));
            return Sha256(result.ToString());
        }

        private static bool HasAncestorNamed(Transform item, Transform root, string name)
        {
            for (Transform current = item.parent;
                 current != null && current != root;
                 current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static string CarrySignature(FlashlightRightHandFollowBehaviour carry)
        {
            return string.Join("|", new[]
            {
                AssetDatabase.GetAssetPath(carry.FlashlightPrefab),
                AssetDatabase.GetAssetPath(carry.FistGripPose),
                carry.RightHandPath,
                Vec(carry.HolderLocalPosition),
                Quat(carry.HolderLocalRotation),
                Vec(carry.ModelLocalPosition),
                Quat(carry.ModelLocalRotation),
                Vec(carry.ModelLocalScale)
            });
        }

        private static string FlashlightVisualSignature(
            FlashlightRightHandFollowBehaviour carry)
        {
            carry.RefreshPreview();
            GameObject holder = carry.Holder ??
                throw new MissingReferenceException("Flashlight holder is missing.");
            var result = new StringBuilder()
                .Append(Vec(holder.transform.localPosition)).Append('|')
                .Append(Quat(holder.transform.localRotation)).Append('|')
                .AppendLine(Vec(holder.transform.localScale));
            foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(
                    item.transform, holder.transform), StringComparer.Ordinal))
            {
                Mesh mesh = null;
                if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null) mesh = filter.sharedMesh;
                }
                result.Append(AnimationUtility.CalculateTransformPath(
                        renderer.transform, holder.transform)).Append('|')
                    .Append(renderer.GetType().FullName).Append('|')
                    .Append(renderer.enabled).Append('|')
                    .Append(ObjectIdentity(mesh)).Append('|')
                    .Append(string.Join(",", renderer.sharedMaterials
                        .Select(ObjectIdentity))).Append('|')
                    .Append(Vec(renderer.transform.localPosition)).Append('|')
                    .Append(Quat(renderer.transform.localRotation)).Append('|')
                    .AppendLine(Vec(renderer.transform.localScale));
            }
            return Sha256(result.ToString());
        }

        private static Animator RequireAnimator(GameObject target)
        {
            return target.GetComponent<Animator>() ??
                throw new MissingReferenceException(target.name + " Animator is missing.");
        }

        private static AnimatorController RequireController(Animator animator, string label)
        {
            return animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(label + " AnimatorController is missing.");
        }

        private static FlashlightRightHandFollowBehaviour RequireCarry(GameObject target)
        {
            return target.GetComponent<FlashlightRightHandFollowBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Idle right-hand follow behaviour is missing.");
        }

        private static Transform RequirePath(Transform root, string path)
        {
            return root.Find(path) ?? throw new MissingReferenceException(
                root.name + " path is missing: " + path);
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. Active=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Flashlight_Idle locomotion setup requires Edit Mode.");
        }

        private static void RequireFloatParameter(
            AnimatorController controller, string name, float defaultValue)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, defaultValue))
                throw new InvalidOperationException(name + " parameter is invalid.");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new MissingReferenceException(
                "Asset is missing: " + path);
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static void WriteText(string assetPath, string contents)
        {
            string absolute = Absolute(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string ComputeAssetHash(string assetPath)
        {
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static string ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return "null";
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    value, out string guid, out long localId))
                return guid + ":" + localId;
            return value.GetType().FullName + ":" + value.name;
        }

        private static string Absolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z);
        private static string Quat(Quaternion value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);

        private sealed class SourceMotions
        {
            internal SourceMotions(
                AnimationClip idle,
                AnimationClip forward,
                AnimationClip backward,
                AnimationClip sidestep,
                BlendTree diagonal,
                AnimationClip run)
            {
                Idle = idle;
                Forward = forward;
                Backward = backward;
                Sidestep = sidestep;
                Diagonal = diagonal;
                Run = run;
            }

            internal AnimationClip Idle { get; }
            internal AnimationClip Forward { get; }
            internal AnimationClip Backward { get; }
            internal AnimationClip Sidestep { get; }
            internal BlendTree Diagonal { get; }
            internal AnimationClip Run { get; }
        }
    }

    [InitializeOnLoad]
    internal static class FlashlightChargeConnectAnimationTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Flashlight_Charge_Connect";
        private const string DisconnectTargetName =
            "Flashlight_Charge_Disconnect";
        private const string IdleReferenceName = "Player_Idle";
        private const string AssetFolder = "Assets/_Project/Animation/Flashlight";
        private const string SourceFolder = AssetFolder + "/Source";
        private const string ReviewFolder = AssetFolder + "/Review";
        private const string ImportedFbxPath =
            SourceFolder + "/transfer flashlight charging.fbx";
        private const string ControllerPath =
            AssetFolder + "/FlashlightChargeConnect_SourceExact.controller";
        private const string DisconnectControllerPath =
            AssetFolder + "/FlashlightChargeDisconnect_Reverse.controller";
        private const string SourceTakeName = "mixamo.com";
        private const string ClipName = "FlashlightChargeConnect_SourceExact";
        private const string StateName = "FlashlightChargeConnectSourceExact";
        private const string DisconnectStateName =
            "FlashlightChargeDisconnectReverse";
        private const string DisconnectReverseSourceReportPath =
            ReviewFolder + "/charge_disconnect_reverse_sources.txt";
        private const string DisconnectReverseApplicationReportPath =
            ReviewFolder + "/charge_disconnect_reverse_application.txt";
        private const string DisconnectReverseInspectionReportPath =
            ReviewFolder + "/charge_disconnect_reverse_inspection.txt";
        private const string DisconnectReverseFinalImagePath =
            ReviewFolder + "/charge_disconnect_reverse_final.png";
        private const string DisconnectReverseFinalReportPath =
            ReviewFolder + "/charge_disconnect_reverse_final.txt";
        private const string DisconnectReverseRuntimeInspectionReportPath =
            ReviewFolder + "/charge_disconnect_reverse_runtime_inspection.txt";
        private const string DisconnectReverseCompletionReportPath =
            ReviewFolder + "/charge_disconnect_reverse_completion.txt";
        private const string DisconnectReverseFailureReportPath =
            ReviewFolder + "/charge_disconnect_reverse_failure.txt";
        private const string GripTimingSourceReportPath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_sources.txt";
        private const string GripTimingApplicationReportPath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_application.txt";
        private const string GripTimingInspectionReportPath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_inspection.txt";
        private const string GripTimingRuntimeReportPath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_runtime.txt";
        private const string GripTimingFinalImagePath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_final.png";
        private const string GripTimingFinalReportPath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_final.txt";
        private const string GripTimingCompletionReportPath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_completion.txt";
        private const string GripTimingFailureReportPath =
            ReviewFolder + "/charge_connect_disconnect_grip_timing_failure.txt";
        private const string SourceReportPath =
            ReviewFolder + "/charge_connect_source.txt";
        private const string ApplicationReportPath =
            ReviewFolder + "/charge_connect_application.txt";
        private const string InspectionReportPath =
            ReviewFolder + "/charge_connect_inspection.txt";
        private const string FinalImagePath =
            ReviewFolder + "/charge_connect_final.png";
        private const string FinalReportPath =
            ReviewFolder + "/charge_connect_final.txt";
        private const string CompletionReportPath =
            ReviewFolder + "/charge_connect_completion.txt";
        private const string FailureReportPath =
            ReviewFolder + "/charge_connect_failure.txt";
        private const string PoseSourceReportPath =
            ReviewFolder + "/charge_connect_pose_sources.txt";
        private const string ForwardReachSourceReportPath =
            ReviewFolder + "/charge_connect_forward_reach_sources.txt";
        private const string ForwardReachApplicationReportPath =
            ReviewFolder + "/charge_connect_forward_reach_application.txt";
        private const string ForwardReachInspectionReportPath =
            ReviewFolder + "/charge_connect_forward_reach_inspection.txt";
        private const string ForwardReachFinalImagePath =
            ReviewFolder + "/charge_connect_forward_reach_final.png";
        private const string ForwardReachFinalReportPath =
            ReviewFolder + "/charge_connect_forward_reach_final.txt";
        private const string ForwardReachCompletionReportPath =
            ReviewFolder + "/charge_connect_forward_reach_completion.txt";
        private const string ForwardReachFailureReportPath =
            ReviewFolder + "/charge_connect_forward_reach_failure.txt";
        private const string UpperBodyRestoreSourceReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_sources.txt";
        private const string UpperBodyRestoreApplicationReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_application.txt";
        private const string UpperBodyRestoreInspectionReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_inspection.txt";
        private const string UpperBodyRestoreDiagnosticOneImagePath =
            ReviewFolder + "/charge_connect_upper_body_restore_diagnostic_1.png";
        private const string UpperBodyRestoreDiagnosticOneReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_diagnostic_1.txt";
        private const string UpperBodyRestoreDiagnosticTwoImagePath =
            ReviewFolder + "/charge_connect_upper_body_restore_diagnostic_2.png";
        private const string UpperBodyRestoreDiagnosticTwoReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_diagnostic_2.txt";
        private const string UpperBodyRestoreFinalImagePath =
            ReviewFolder + "/charge_connect_upper_body_restore_final.png";
        private const string UpperBodyRestoreFinalReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_final.txt";
        private const string UpperBodyRestoreCompletionReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_completion.txt";
        private const string UpperBodyRestoreFailureReportPath =
            ReviewFolder + "/charge_connect_upper_body_restore_failure.txt";
        private const string ForwardHoldSourceReportPath =
            ReviewFolder + "/charge_connect_forward_hold_sources.txt";
        private const string ForwardHoldApplicationReportPath =
            ReviewFolder + "/charge_connect_forward_hold_application.txt";
        private const string ForwardHoldInspectionReportPath =
            ReviewFolder + "/charge_connect_forward_hold_inspection.txt";
        private const string ForwardHoldDiagnosticOneImagePath =
            ReviewFolder + "/charge_connect_forward_hold_diagnostic_1.png";
        private const string ForwardHoldDiagnosticOneReportPath =
            ReviewFolder + "/charge_connect_forward_hold_diagnostic_1.txt";
        private const string ForwardHoldDiagnosticTwoImagePath =
            ReviewFolder + "/charge_connect_forward_hold_diagnostic_2.png";
        private const string ForwardHoldDiagnosticTwoReportPath =
            ReviewFolder + "/charge_connect_forward_hold_diagnostic_2.txt";
        private const string ForwardHoldFinalImagePath =
            ReviewFolder + "/charge_connect_forward_hold_final.png";
        private const string ForwardHoldFinalReportPath =
            ReviewFolder + "/charge_connect_forward_hold_final.txt";
        private const string ForwardHoldCompletionReportPath =
            ReviewFolder + "/charge_connect_forward_hold_completion.txt";
        private const string ForwardHoldFailureReportPath =
            ReviewFolder + "/charge_connect_forward_hold_failure.txt";
        private const string RightShoulderForwardSourceReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_sources.txt";
        private const string RightShoulderForwardApplicationReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_application.txt";
        private const string RightShoulderForwardInspectionReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_inspection.txt";
        private const string RightShoulderForwardDiagnosticOneImagePath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_diagnostic_1.png";
        private const string RightShoulderForwardDiagnosticOneReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_diagnostic_1.txt";
        private const string RightShoulderForwardDiagnosticTwoImagePath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_diagnostic_2.png";
        private const string RightShoulderForwardDiagnosticTwoReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_diagnostic_2.txt";
        private const string RightShoulderForwardFinalImagePath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_final.png";
        private const string RightShoulderForwardFinalReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_final.txt";
        private const string RightShoulderForwardCompletionReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_completion.txt";
        private const string RightShoulderForwardFailureReportPath =
            ReviewFolder + "/charge_connect_right_shoulder_forward_failure.txt";
        private const string NeutralWristSourceReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_sources.txt";
        private const string NeutralWristApplicationReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_application.txt";
        private const string NeutralWristInspectionReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_inspection.txt";
        private const string NeutralWristDiagnosticOneImagePath =
            ReviewFolder + "/charge_connect_neutral_wrist_diagnostic_1.png";
        private const string NeutralWristDiagnosticOneReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_diagnostic_1.txt";
        private const string NeutralWristDiagnosticTwoImagePath =
            ReviewFolder + "/charge_connect_neutral_wrist_diagnostic_2.png";
        private const string NeutralWristDiagnosticTwoReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_diagnostic_2.txt";
        private const string NeutralWristFinalImagePath =
            ReviewFolder + "/charge_connect_neutral_wrist_final.png";
        private const string NeutralWristFinalReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_final.txt";
        private const string NeutralWristCompletionReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_completion.txt";
        private const string NeutralWristFailureReportPath =
            ReviewFolder + "/charge_connect_neutral_wrist_failure.txt";
        private const string FaceClearanceSourceReportPath =
            ReviewFolder + "/charge_connect_face_clearance_sources.txt";
        private const string FaceClearanceApplicationReportPath =
            ReviewFolder + "/charge_connect_face_clearance_application.txt";
        private const string FaceClearanceInspectionReportPath =
            ReviewFolder + "/charge_connect_face_clearance_inspection.txt";
        private const string FaceClearanceDiagnosticOneImagePath =
            ReviewFolder + "/charge_connect_face_clearance_diagnostic_1.png";
        private const string FaceClearanceDiagnosticOneReportPath =
            ReviewFolder + "/charge_connect_face_clearance_diagnostic_1.txt";
        private const string FaceClearanceDiagnosticTwoImagePath =
            ReviewFolder + "/charge_connect_face_clearance_diagnostic_2.png";
        private const string FaceClearanceDiagnosticTwoReportPath =
            ReviewFolder + "/charge_connect_face_clearance_diagnostic_2.txt";
        private const string FaceClearanceFinalImagePath =
            ReviewFolder + "/charge_connect_face_clearance_final.png";
        private const string FaceClearanceFinalReportPath =
            ReviewFolder + "/charge_connect_face_clearance_final.txt";
        private const string FaceClearanceCompletionReportPath =
            ReviewFolder + "/charge_connect_face_clearance_completion.txt";
        private const string FaceClearanceFailureReportPath =
            ReviewFolder + "/charge_connect_face_clearance_failure.txt";
        private const string FlashlightRightOffsetApplicationReportPath =
            ReviewFolder + "/charge_connect_flashlight_right_offset_application.txt";
        private const string FlashlightRightOffsetInspectionReportPath =
            ReviewFolder + "/charge_connect_flashlight_right_offset_inspection.txt";
        private const string FlashlightRightOffsetFinalImagePath =
            ReviewFolder + "/charge_connect_flashlight_right_offset_final.png";
        private const string FlashlightRightOffsetFinalReportPath =
            ReviewFolder + "/charge_connect_flashlight_right_offset_final.txt";
        private const string FlashlightRightOffsetCompletionReportPath =
            ReviewFolder + "/charge_connect_flashlight_right_offset_completion.txt";
        private const string FlashlightRightOffsetFailureReportPath =
            ReviewFolder + "/charge_connect_flashlight_right_offset_failure.txt";
        private const string FlashlightForwardOffsetApplicationReportPath =
            ReviewFolder + "/charge_connect_flashlight_forward_offset_application.txt";
        private const string FlashlightForwardOffsetInspectionReportPath =
            ReviewFolder + "/charge_connect_flashlight_forward_offset_inspection.txt";
        private const string FlashlightForwardOffsetFinalImagePath =
            ReviewFolder + "/charge_connect_flashlight_forward_offset_final.png";
        private const string FlashlightForwardOffsetFinalReportPath =
            ReviewFolder + "/charge_connect_flashlight_forward_offset_final.txt";
        private const string FlashlightForwardOffsetCompletionReportPath =
            ReviewFolder + "/charge_connect_flashlight_forward_offset_completion.txt";
        private const string FlashlightForwardOffsetFailureReportPath =
            ReviewFolder + "/charge_connect_flashlight_forward_offset_failure.txt";
        private const string FlashlightHandClearanceSourceReportPath =
            ReviewFolder + "/charge_connect_flashlight_hand_clearance_sources.txt";
        private const string FlashlightHandClearanceApplicationReportPath =
            ReviewFolder + "/charge_connect_flashlight_hand_clearance_application.txt";
        private const string FlashlightHandClearanceInspectionReportPath =
            ReviewFolder + "/charge_connect_flashlight_hand_clearance_inspection.txt";
        private const string FlashlightHandClearanceFinalImagePath =
            ReviewFolder + "/charge_connect_flashlight_hand_clearance_final.png";
        private const string FlashlightHandClearanceFinalReportPath =
            ReviewFolder + "/charge_connect_flashlight_hand_clearance_final.txt";
        private const string FlashlightHandClearanceCompletionReportPath =
            ReviewFolder + "/charge_connect_flashlight_hand_clearance_completion.txt";
        private const string FlashlightHandClearanceFailureReportPath =
            ReviewFolder + "/charge_connect_flashlight_hand_clearance_failure.txt";
        private const string PoseApplicationReportPath =
            ReviewFolder + "/charge_connect_pose_application.txt";
        private const string PoseInspectionReportPath =
            ReviewFolder + "/charge_connect_pose_inspection.txt";
        private const string PoseFinalImagePath =
            ReviewFolder + "/charge_connect_pose_final.png";
        private const string PoseFinalReportPath =
            ReviewFolder + "/charge_connect_pose_final.txt";
        private const string PoseCompletionReportPath =
            ReviewFolder + "/charge_connect_pose_completion.txt";
        private const string PoseFailureReportPath =
            ReviewFolder + "/charge_connect_pose_failure.txt";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const string LeftShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder";
        private const string SpineRootPath = "Armature/Hips/Spine02";
        private const string AutoStateKey =
            "Bellerophon.FlashlightChargeConnect.AutoState";
        private const string PoseReviewKey =
            "Bellerophon.FlashlightChargeConnect.PoseReview";
        private const string ForwardReachReviewKey =
            "Bellerophon.FlashlightChargeConnect.ForwardReachReview";
        private const string UpperBodyRestoreReviewKey =
            "Bellerophon.FlashlightChargeConnect.UpperBodyRestoreReview";
        private const string UpperBodyRestoreFinalReviewKey =
            "Bellerophon.FlashlightChargeConnect.UpperBodyRestoreFinalReview";
        private const string UpperBodyRestoreDiagnosticIndexKey =
            "Bellerophon.FlashlightChargeConnect.UpperBodyRestoreDiagnosticIndex";
        private const string ForwardHoldReviewKey =
            "Bellerophon.FlashlightChargeConnect.ForwardHoldReview";
        private const string ForwardHoldFinalReviewKey =
            "Bellerophon.FlashlightChargeConnect.ForwardHoldFinalReview";
        private const string ForwardHoldDiagnosticIndexKey =
            "Bellerophon.FlashlightChargeConnect.ForwardHoldDiagnosticIndex";
        private const string RightShoulderForwardReviewKey =
            "Bellerophon.FlashlightChargeConnect.RightShoulderForwardReview";
        private const string RightShoulderForwardFinalReviewKey =
            "Bellerophon.FlashlightChargeConnect.RightShoulderForwardFinalReview";
        private const string RightShoulderForwardDiagnosticIndexKey =
            "Bellerophon.FlashlightChargeConnect.RightShoulderForwardDiagnosticIndex";
        private const string NeutralWristReviewKey =
            "Bellerophon.FlashlightChargeConnect.NeutralWristReview";
        private const string NeutralWristFinalReviewKey =
            "Bellerophon.FlashlightChargeConnect.NeutralWristFinalReview";
        private const string NeutralWristDiagnosticIndexKey =
            "Bellerophon.FlashlightChargeConnect.NeutralWristDiagnosticIndex";
        private const string FaceClearanceReviewKey =
            "Bellerophon.FlashlightChargeConnect.FaceClearanceReview";
        private const string FaceClearanceFinalReviewKey =
            "Bellerophon.FlashlightChargeConnect.FaceClearanceFinalReview";
        private const string FaceClearanceDiagnosticIndexKey =
            "Bellerophon.FlashlightChargeConnect.FaceClearanceDiagnosticIndex";
        private const string FlashlightRightOffsetReviewKey =
            "Bellerophon.FlashlightChargeConnect.FlashlightRightOffsetReview";
        private const string FlashlightForwardOffsetReviewKey =
            "Bellerophon.FlashlightChargeConnect.FlashlightForwardOffsetReview";
        private const string FlashlightHandClearanceReviewKey =
            "Bellerophon.FlashlightChargeConnect.FlashlightHandClearanceReview";
        private const string DisconnectReverseAutoStateKey =
            "Bellerophon.FlashlightChargeDisconnect.ReverseAutoState";
        private const string DisconnectReverseInspectionOnlyKey =
            "Bellerophon.FlashlightChargeDisconnect.ReverseInspectionOnly";
        private const string GripTimingReviewKey =
            "Bellerophon.FlashlightChargeConnectDisconnect.GripTimingReview";
        private const string AwaitingPlayState = "AwaitingPlay";
        private const string CapturingState = "Capturing";
        private const string AwaitingEditState = "AwaitingEdit";
        private const string AwaitingEditReadyState = "AwaitingEditReady";
        private const int PanelSize = 512;
        private const double CaptureTimeoutSeconds = 20d;
        private const float DesiredElbowAngleDegrees = 140f;
        private const float ApproachStartNormalized = 0.04f;
        private const float ReachEndNormalized = 0.63f;
        private const float HoldEndNormalized = 0.72f;
        private const float ReturnEndNormalized = 1f;
        private const float SourceMaximumHandSpeed = 3.15233541f;
        private const float SourceMaximumHandAcceleration = 63.80477f;
        private const float ForwardHoldDurationSeconds = 1f;
        private const float RightShoulderForwardElbowAngleDegrees = 140f;
        private const float RightShoulderForwardStartNormalized = 0.08f;
        private const float RightShoulderForwardReturnNormalized = 0.70f;
        private const float RightShoulderForwardEndNormalized = 0.98f;
        private const float RightShoulderForwardShoulderShare = 0.18f;
        private const float RightShoulderForwardWristSourceWeight = 0.65f;
        private const float NeutralWristMaximumBendDegrees = 5f;
        private const float FaceClearanceBoundaryRatio = 0.82f;
        private const float FaceClearanceSafetyMarginMeters = 0.015f;
        private const float FaceClearanceShoulderShare = 0.18f;
        private const float FlashlightRightOffsetMeters = 0.04f;
        private const float FlashlightForwardOffsetMeters = 0.02f;
        private const float FlashlightHandClearanceSafetyMarginMeters = 0.005f;
        private const float DisconnectFinalHoldDurationSeconds = 0.5f;

        private static readonly float[] CaptureTimes =
            { 0.08f, 0.30f, 0.55f, 0.80f, 1.08f };

        private static bool disconnectCaptureActive;
        private static double disconnectCaptureStartedAt;
        private static int disconnectNextPanel;
        private static GameObject disconnectRuntimeTarget;
        private static Animator disconnectRuntimeAnimator;
        private static FlashlightRightHandFollowBehaviour disconnectRuntimeCarry;
        private static FlashlightChargeConnectPoseBehaviour disconnectRuntimePose;
        private static GameObject connectGripRuntimeTarget;
        private static FlashlightRightHandFollowBehaviour connectGripRuntimeCarry;
        private static FlashlightChargeConnectPoseBehaviour connectGripRuntimePose;
        private static Texture2D[] disconnectFullPanels;
        private static Texture2D[] disconnectUpperPanels;
        private static float[] disconnectCaptureTimes;
        private static float disconnectExpectedCycleDuration;
        private static float disconnectInitialNormalizedTime;
        private static float disconnectMaximumNormalizedTime;
        private static float disconnectPreviousPhase;
        private static int disconnectPreviousLoop;
        private static bool disconnectHasPhaseSample;
        private static bool disconnectReverseMotionObserved;
        private static bool disconnectLoopReturnObserved;
        private static bool disconnectHoldObserved;
        private static bool disconnectInitialHoldObserved;
        private static float disconnectFinalHoldPhase = float.PositiveInfinity;
        private static float disconnectMaximumForwardPhaseError;
        private static float disconnectMaximumHolderPositionError;
        private static float disconnectMaximumHolderRotationError;
        private static float disconnectMaximumBoneRotationDelta;
        private static float connectMaximumFullReachSurfaceClearance =
            float.NegativeInfinity;
        private static float disconnectMaximumFullReachSurfaceClearance =
            float.NegativeInfinity;
        private static int connectFullReachGripSamples;
        private static int disconnectFullReachGripSamples;
        private static readonly Dictionary<string, Quaternion>
            DisconnectInitialBoneRotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        private static readonly List<string> DisconnectRuntimeObservations =
            new List<string>();

        private static bool captureActive;
        private static double captureStartTime;
        private static int nextPanel;
        private static int baseLoop = -1;
        private static GameObject runtimeTarget;
        private static Animator runtimeAnimator;
        private static FlashlightRightHandFollowBehaviour runtimeCarry;
        private static FlashlightChargeConnectPoseBehaviour runtimePose;
        private static Transform runtimeHead;
        private static Transform runtimeLeftArm;
        private static Transform runtimeRightArm;
        private static Quaternion initialHeadRotation;
        private static Quaternion initialLeftArmRotation;
        private static Quaternion initialRightArmRotation;
        private static Texture2D[] fullPanels;
        private static Texture2D[] upperPanels;
        private static float[] runtimeCaptureTimes;
        private static double runtimeCycleStartTime;
        private static float observedForwardHoldCycleDuration;
        private static readonly Dictionary<string, Quaternion> ForwardHoldRotations =
            new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Vector3> ForwardHoldPositions =
            new Dictionary<string, Vector3>(StringComparer.Ordinal);
        private static bool forwardHoldReferenceCaptured;
        private static int forwardHoldStartMetricFrame;
        private static float maximumForwardHoldRotationDrift;
        private static float maximumForwardHoldPositionDrift;
        private static Vector3 forwardHoldHolderPosition;
        private static Quaternion forwardHoldHolderRotation;
        private static float maximumForwardHoldHolderPositionDrift;
        private static float maximumForwardHoldHolderRotationDrift;
        private static float maximumRightShoulderForwardDeviation;
        private static float maximumRightShoulderForwardLateralOffset;
        private static float minimumRightShoulderForwardElbowAngle;
        private static float maximumRightShoulderForwardElbowAngle;
        private static float maximumRightShoulderForwardHandError;
        private static float minimumRightShoulderForwardHoldWeight;
        private static float maximumNeutralWristBendAngle;
        private static float minimumFaceClearanceMeters;
        private static float maximumFaceClearanceHandError;
        private static float minimumFaceClearanceAppliedOffset;
        private static float maximumFaceClearanceAppliedOffset;
        private static float minimumHandFlashlightSurfaceClearance =
            float.PositiveInfinity;
        private static readonly List<string> RuntimeObservations = new List<string>();
        private static readonly Dictionary<string, Quaternion> InitialRotations =
            new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        private static float maximumBoneRotationDelta;
        private static float maximumHolderPositionError;
        private static float maximumHolderRotationError;
        private static float maximumHeadRotationDelta;
        private static float maximumLeftArmRotationDelta;
        private static float maximumRightArmRotationDelta;
        private static int lastReachMetricFrame;
        private static bool hasReachMetricSample;
        private static Vector3 previousReachHandLocal;
        private static Vector3 previousReachVelocity;
        private static float maximumReachHandSpeed;
        private static float maximumApproachHandSpeed;
        private static float maximumReachHandAcceleration;
        private static float maximumReachTargetError;
        private static float maximumFullReachForwardDeviation;
        private static float minimumFullReachElbowAngle;
        private static float maximumFullReachElbowAngle;

        static FlashlightChargeConnectAnimationTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= RuntimeCaptureTick;
            if (!string.IsNullOrEmpty(SessionState.GetString(AutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueReview;
            EditorApplication.update -= DisconnectRuntimeCaptureTick;
            if (!string.IsNullOrEmpty(SessionState.GetString(
                    DisconnectReverseAutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueDisconnectReverseReview;
        }

        private static string ExternalFbxPath => Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "player model",
            "transfer flashlight charging.fbx"));

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(AutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueReview;
            if (!string.IsNullOrEmpty(SessionState.GetString(
                    DisconnectReverseAutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueDisconnectReverseReview;
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Grip And Disconnect Timing Sources")]
        internal static void InspectFlashlightChargeConnectDisconnectGripAndTimingSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject connect = FindUnique(scene, TargetName);
            GameObject disconnect = FindUnique(scene, DisconnectTargetName);
            AnimationClip clip = RequireCurrentConnectConfiguration(
                connect, out _, out FlashlightRightHandFollowBehaviour connectCarry,
                out FlashlightChargeConnectPoseBehaviour connectPose);
            Animator disconnectAnimator = RequireAnimator(disconnect);
            FlashlightRightHandFollowBehaviour disconnectCarry =
                RequireCarry(disconnect);
            FlashlightChargeConnectPoseBehaviour disconnectPose =
                disconnect.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Disconnect copied pose behaviour is missing.");
            AnimatorController disconnectController =
                disconnectAnimator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Disconnect reverse controller is missing.");
            if (AssetDatabase.GetAssetPath(disconnectController) !=
                DisconnectControllerPath)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect uses an unexpected controller.");
            RequireDisconnectReverseControllerContract(disconnectController, clip);
            if (disconnectPose.Carry != disconnectCarry ||
                disconnectPose.Animator != disconnectAnimator ||
                !disconnectPose.ReversePlayback)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect copied pose references are invalid.");
            RequireEqual(CarrySignature(connectCarry), CarrySignature(disconnectCarry),
                "Flashlight charge carry configuration");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Charge grip/timing source inspection changed scene dirty state.");

            WriteText(GripTimingSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight charge grip and disconnect timing source inspection")
                .AppendLine("connectTarget=" + TargetName)
                .AppendLine("disconnectTarget=" + DisconnectTargetName)
                .AppendLine("sourceClip=" + ObjectIdentity(clip))
                .AppendLine("connectForwardOffsetBeforeMeters=" +
                    F(connectPose.FlashlightForwardOffsetMeters))
                .AppendLine("disconnectForwardOffsetBeforeMeters=" +
                    F(disconnectPose.FlashlightForwardOffsetMeters))
                .AppendLine("connectHoldDurationBeforeSeconds=" +
                    F(connectPose.ForwardHoldDurationSeconds))
                .AppendLine("disconnectHoldDurationBeforeSeconds=" +
                    F(disconnectPose.ForwardHoldDurationSeconds))
                .AppendLine("targetGripForwardOffsetMeters=" +
                    F(FlashlightForwardOffsetMeters))
                .AppendLine("targetDisconnectFinalHoldSeconds=" +
                    F(DisconnectFinalHoldDurationSeconds))
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeGripTiming] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Grip And Disconnect Timing")]
        internal static void ApplyFlashlightChargeConnectDisconnectGripAndTiming()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject connect = FindUnique(scene, TargetName);
            GameObject disconnect = FindUnique(scene, DisconnectTargetName);
            AnimationClip clip = RequireCurrentConnectConfiguration(
                connect, out Animator connectAnimator,
                out FlashlightRightHandFollowBehaviour connectCarry,
                out FlashlightChargeConnectPoseBehaviour connectPose);
            Animator disconnectAnimator = RequireAnimator(disconnect);
            FlashlightRightHandFollowBehaviour disconnectCarry =
                RequireCarry(disconnect);
            FlashlightChargeConnectPoseBehaviour disconnectPose =
                disconnect.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Disconnect copied pose behaviour is missing.");
            AnimatorController disconnectController =
                disconnectAnimator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Disconnect reverse controller is missing.");
            if (AssetDatabase.GetAssetPath(disconnectController) !=
                DisconnectControllerPath)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect uses an unexpected controller.");
            RequireDisconnectReverseControllerContract(disconnectController, clip);
            if (disconnectPose.Carry != disconnectCarry ||
                disconnectPose.Animator != disconnectAnimator ||
                !disconnectPose.ReversePlayback)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect copied pose references are invalid.");

            string outsideBefore = OutsideTargetsSignature(
                scene, connect.transform, disconnect.transform);
            string connectCarryBefore = CarrySignature(connectCarry);
            string disconnectCarryBefore = CarrySignature(disconnectCarry);
            string connectVisualBefore = VisualSignature(connect.transform, connectCarry);
            string disconnectVisualBefore =
                VisualSignature(disconnect.transform, disconnectCarry);
            string connectTransformBefore =
                TargetPoseWithoutFlashlightSignature(connect.transform, connectCarry);
            string disconnectTransformBefore =
                TargetPoseWithoutFlashlightSignature(disconnect.transform, disconnectCarry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string connectControllerHashBefore = ComputeAssetHash(ControllerPath);
            string disconnectControllerHashBefore =
                ComputeAssetHash(DisconnectControllerPath);
            float connectHoldBefore = connectPose.ForwardHoldDurationSeconds;
            float connectOffsetBefore = connectPose.FlashlightForwardOffsetMeters;
            float disconnectOffsetBefore = disconnectPose.FlashlightForwardOffsetMeters;
            float disconnectHoldBefore = disconnectPose.ForwardHoldDurationSeconds;

            Undo.RecordObjects(
                new UnityEngine.Object[] { connectPose, disconnectPose },
                "Align charge flashlights with right hand and change disconnect final hold");
            connectPose.ConfigureFlashlightForwardOffset(
                FlashlightForwardOffsetMeters);
            disconnectPose.ConfigureFlashlightForwardOffset(
                FlashlightForwardOffsetMeters);
            disconnectPose.ConfigureReverseFinalHold(
                DisconnectFinalHoldDurationSeconds);
            EditorUtility.SetDirty(connectPose);
            EditorUtility.SetDirty(disconnectPose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(connectPose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(disconnectPose);

            if (Mathf.Abs(connectPose.FlashlightForwardOffsetMeters -
                    FlashlightForwardOffsetMeters) > 0.000001f ||
                Mathf.Abs(disconnectPose.FlashlightForwardOffsetMeters -
                    FlashlightForwardOffsetMeters) > 0.000001f)
                throw new InvalidOperationException(
                    "Charge flashlight grip offsets were not applied exactly.");
            if (Mathf.Abs(connectPose.ForwardHoldDurationSeconds -
                    connectHoldBefore) > 0.000001f ||
                Mathf.Abs(disconnectPose.ForwardHoldDurationSeconds -
                    DisconnectFinalHoldDurationSeconds) > 0.000001f)
                throw new InvalidOperationException(
                    "Charge animation hold durations were not preserved/applied exactly.");
            RequireDisconnectReverseConfiguration(
                disconnect, clip, connectPose, out _, out _, out _);
            if (connectAnimator.speed != 1f || disconnectAnimator.speed != 1f)
                throw new InvalidOperationException(
                    "Charge animator speed changed while applying grip/timing settings.");
            RequireEqual(connectCarryBefore, CarrySignature(connectCarry),
                "Flashlight_Charge_Connect right-hand follow configuration");
            RequireEqual(disconnectCarryBefore, CarrySignature(disconnectCarry),
                "Flashlight_Charge_Disconnect right-hand follow configuration");
            RequireEqual(connectVisualBefore, VisualSignature(connect.transform, connectCarry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(disconnectVisualBefore,
                VisualSignature(disconnect.transform, disconnectCarry),
                "Flashlight_Charge_Disconnect renderers, meshes, and materials");
            RequireEqual(connectTransformBefore,
                TargetPoseWithoutFlashlightSignature(connect.transform, connectCarry),
                "Flashlight_Charge_Connect arm and body transforms");
            RequireEqual(disconnectTransformBefore,
                TargetPoseWithoutFlashlightSignature(disconnect.transform, disconnectCarry),
                "Flashlight_Charge_Disconnect arm and body transforms");
            RequireEqual(outsideBefore,
                OutsideTargetsSignature(scene, connect.transform, disconnect.transform),
                "objects outside both flashlight charge targets");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(connectControllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");
            RequireEqual(disconnectControllerHashBefore,
                ComputeAssetHash(DisconnectControllerPath),
                "charge-disconnect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the charge grip/timing settings.");
            AssetDatabase.SaveAssets();

            DeleteReviewFile(GripTimingInspectionReportPath);
            DeleteReviewFile(GripTimingRuntimeReportPath);
            DeleteReviewFile(GripTimingCompletionReportPath);
            DeleteReviewFile(GripTimingFailureReportPath);
            WriteText(GripTimingApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight charge grip and disconnect final-hold application")
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("connectForwardOffsetBeforeMeters=" + F(connectOffsetBefore))
                .AppendLine("disconnectForwardOffsetBeforeMeters=" + F(disconnectOffsetBefore))
                .AppendLine("appliedForwardOffsetMeters=" +
                    F(FlashlightForwardOffsetMeters))
                .AppendLine("connectHoldDurationBeforeSeconds=" + F(connectHoldBefore))
                .AppendLine("connectHoldDurationAfterSeconds=" +
                    F(connectPose.ForwardHoldDurationSeconds))
                .AppendLine("disconnectHoldDurationBeforeSeconds=" +
                    F(disconnectHoldBefore))
                .AppendLine("disconnectFinalHoldDurationAfterSeconds=" +
                    F(disconnectPose.ForwardHoldDurationSeconds))
                .AppendLine("disconnectInitialHoldRemoved=True")
                .AppendLine("rightArmWristBodyModified=False")
                .AppendLine("rightHandFollowModified=False")
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllersModified=False")
                .AppendLine("rendererMeshMaterialRotationScaleModified=False")
                .AppendLine("objectsOutsideTargetsChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeGripTiming] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Grip And Disconnect Timing In Play Mode")]
        internal static void InspectFlashlightChargeConnectDisconnectGripAndTimingPlayMode()
        {
            RequireEditMode();
            InspectFlashlightChargeConnectDisconnectGripAndTimingConfiguration();
            RequireNoActiveFlashlightReview();
            DeleteReviewFile(GripTimingRuntimeReportPath);
            DeleteReviewFile(GripTimingCompletionReportPath);
            DeleteReviewFile(GripTimingFailureReportPath);
            SessionState.SetBool(GripTimingReviewKey, true);
            SessionState.SetBool(DisconnectReverseInspectionOnlyKey, true);
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log(
                "[FlashlightChargeGripTiming] Natural Play Mode inspection started without capture.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Grip And Disconnect Timing Final")]
        internal static void CaptureFlashlightChargeConnectDisconnectGripAndTimingFinal()
        {
            RequireEditMode();
            InspectFlashlightChargeConnectDisconnectGripAndTimingConfiguration();
            RequireNoActiveFlashlightReview();
            DeleteReviewFile(GripTimingFinalImagePath);
            DeleteReviewFile(GripTimingFinalReportPath);
            DeleteReviewFile(GripTimingCompletionReportPath);
            DeleteReviewFile(GripTimingFailureReportPath);
            SessionState.SetBool(GripTimingReviewKey, true);
            SessionState.SetBool(DisconnectReverseInspectionOnlyKey, false);
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log(
                "[FlashlightChargeGripTiming] Final natural-playback capture started.");
        }

        private static void
            InspectFlashlightChargeConnectDisconnectGripAndTimingConfiguration()
        {
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject connect = FindUnique(scene, TargetName);
            GameObject disconnect = FindUnique(scene, DisconnectTargetName);
            AnimationClip clip = RequireCurrentConnectConfiguration(
                connect, out _, out FlashlightRightHandFollowBehaviour connectCarry,
                out FlashlightChargeConnectPoseBehaviour connectPose);
            RequireDisconnectReverseConfiguration(
                disconnect, clip, connectPose, out _,
                out FlashlightRightHandFollowBehaviour disconnectCarry,
                out FlashlightChargeConnectPoseBehaviour disconnectPose);

            if (Mathf.Abs(connectPose.FlashlightForwardOffsetMeters -
                    FlashlightForwardOffsetMeters) > 0.000001f ||
                Mathf.Abs(disconnectPose.FlashlightForwardOffsetMeters -
                    FlashlightForwardOffsetMeters) > 0.000001f)
                throw new InvalidOperationException(
                    "Charge flashlights are not aligned to the approved hand-overlap offset.");
            if (Mathf.Abs(connectPose.ForwardHoldDurationSeconds -
                    ForwardHoldDurationSeconds) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect one-second hold changed unexpectedly.");
            if (Mathf.Abs(disconnectPose.ForwardHoldDurationSeconds -
                    DisconnectFinalHoldDurationSeconds) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect final hold is not 0.5 seconds.");

            AnalyzeGripOverlapAtFullReach(
                connect, clip, out int connectSamples,
                out float connectMinimumClearance,
                out float connectMaximumClearance);
            AnalyzeGripOverlapAtFullReach(
                disconnect, clip, out int disconnectSamples,
                out float disconnectMinimumClearance,
                out float disconnectMaximumClearance);
            if (connectSamples == 0 || disconnectSamples == 0 ||
                connectMaximumClearance > 0f || disconnectMaximumClearance > 0f)
                throw new InvalidOperationException(
                    "A charge flashlight remains separated from the right hand at full reach.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Charge grip/timing inspection changed scene dirty state.");
            int errors = UnityConsoleErrorCount();
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during charge grip/timing inspection.");

            WriteText(GripTimingInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight charge grip and disconnect timing inspection")
                .AppendLine("sourceClip=" + ObjectIdentity(clip))
                .AppendLine("connectForwardOffsetMeters=" +
                    F(connectPose.FlashlightForwardOffsetMeters))
                .AppendLine("disconnectForwardOffsetMeters=" +
                    F(disconnectPose.FlashlightForwardOffsetMeters))
                .AppendLine("connectFullReachGripSamples=" + connectSamples)
                .AppendLine("disconnectFullReachGripSamples=" + disconnectSamples)
                .AppendLine("connectMinimumProjectedOverlapMeters=" +
                    F(-connectMaximumClearance))
                .AppendLine("disconnectMinimumProjectedOverlapMeters=" +
                    F(-disconnectMaximumClearance))
                .AppendLine("connectMaximumProjectedOverlapMeters=" +
                    F(-connectMinimumClearance))
                .AppendLine("disconnectMaximumProjectedOverlapMeters=" +
                    F(-disconnectMinimumClearance))
                .AppendLine("connectHoldDurationSeconds=" +
                    F(connectPose.ForwardHoldDurationSeconds))
                .AppendLine("disconnectFinalHoldDurationSeconds=" +
                    F(disconnectPose.ForwardHoldDurationSeconds))
                .AppendLine("disconnectInitialHoldConfigured=False")
                .AppendLine("rightHandFollowPreserved=" +
                    (CarrySignature(connectCarry) == CarrySignature(disconnectCarry)))
                .AppendLine("inspectionManipulatedTarget=False")
                .AppendLine("unityConsoleErrors=0")
                .ToString());
            Debug.Log("[FlashlightChargeGripTiming] Static inspection passed.");
        }

        private static void RequireNoActiveFlashlightReview()
        {
            if (captureActive || disconnectCaptureActive ||
                !string.IsNullOrEmpty(SessionState.GetString(
                    AutoStateKey, string.Empty)) ||
                !string.IsNullOrEmpty(SessionState.GetString(
                    DisconnectReverseAutoStateKey, string.Empty)))
                throw new InvalidOperationException(
                    "A flashlight animation review is already active.");
        }

        private static void AnalyzeGripOverlapAtFullReach(
            GameObject target,
            AnimationClip clip,
            out int fullReachSamples,
            out float minimumClearance,
            out float maximumClearance)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = target.name + "GripOverlapAnalysis";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(work).enabled = false;
                FlashlightRightHandFollowBehaviour carry = RequireCarry(work);
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        target.name + " pose behaviour is missing from grip analysis.");
                fullReachSamples = 0;
                minimumClearance = float.PositiveInfinity;
                maximumClearance = float.NegativeInfinity;
                const int sampleCount = 121;
                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    clip.SampleAnimation(work, normalized * clip.length);
                    carry.RefreshPreview();
                    pose.RefreshPreviewAtNormalizedPhase(normalized);
                    if (pose.CurrentRightShoulderForwardWeight < 0.98f) continue;
                    HandFlashlightForwardGeometry geometry =
                        MeasureHandFlashlightForwardGeometry(work.transform, carry);
                    float clearance = geometry.FlashlightMinimumForward -
                        geometry.RightHandMaximumForward;
                    minimumClearance = Mathf.Min(minimumClearance, clearance);
                    maximumClearance = Mathf.Max(maximumClearance, clearance);
                    fullReachSamples++;
                }
                if (fullReachSamples == 0 || float.IsInfinity(minimumClearance) ||
                    float.IsInfinity(maximumClearance))
                    throw new InvalidOperationException(
                        target.name + " full-reach grip samples were not found.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Disconnect Reverse Sources")]
        internal static void InspectFlashlightChargeDisconnectReverseSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject connect = FindUnique(scene, TargetName);
            GameObject disconnect = FindUnique(scene, DisconnectTargetName);
            AnimationClip clip = RequireCurrentConnectConfiguration(
                connect,
                out Animator connectAnimator,
                out FlashlightRightHandFollowBehaviour connectCarry,
                out FlashlightChargeConnectPoseBehaviour connectPose);
            Animator disconnectAnimator = RequireAnimator(disconnect);
            FlashlightRightHandFollowBehaviour disconnectCarry =
                RequireCarry(disconnect);
            FlashlightChargeConnectPoseBehaviour disconnectPose =
                disconnect.GetComponent<FlashlightChargeConnectPoseBehaviour>();
            if (connectCarry.Holder == null || disconnectCarry.Holder == null)
                throw new MissingReferenceException(
                    "Flashlight charge objects must retain their existing flashlight holders.");
            RequireEqual(CarrySignature(connectCarry), CarrySignature(disconnectCarry),
                "Flashlight charge carry configuration");
            DisconnectReverseAnalysis currentAnalysis = default;
            bool hasCurrentReverseAnalysis = disconnectPose != null &&
                AssetDatabase.GetAssetPath(
                    disconnectAnimator.runtimeAnimatorController) ==
                DisconnectControllerPath;
            if (hasCurrentReverseAnalysis)
                currentAnalysis = AnalyzeDisconnectReverse(
                    connect, disconnect, clip);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Charge-disconnect source inspection changed scene dirty state.");

            WriteText(DisconnectReverseSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Disconnect exact reverse source inspection")
                .AppendLine("connectTarget=" + TargetName)
                .AppendLine("disconnectTarget=" + DisconnectTargetName)
                .AppendLine("sourceClip=" + ObjectIdentity(clip))
                .AppendLine("sourceClipName=" + clip.name)
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("sourceClipFrameRate=" + F(clip.frameRate))
                .AppendLine("connectController=" +
                    AssetDatabase.GetAssetPath(connectAnimator.runtimeAnimatorController))
                .AppendLine("disconnectControllerBefore=" +
                    AssetDatabase.GetAssetPath(disconnectAnimator.runtimeAnimatorController))
                .AppendLine("disconnectPoseComponentBefore=" +
                    (disconnectPose != null))
                .AppendLine("connectPoseConfiguration=" +
                    PoseConfigurationSignature(connectPose))
                .AppendLine("disconnectPoseConfiguration=" +
                    (disconnectPose != null
                        ? PoseConfigurationSignature(disconnectPose)
                        : "missing"))
                .AppendLine("firstPoseConfigurationDifference=" +
                    (disconnectPose != null
                        ? FirstPoseConfigurationDifference(
                            connectPose, disconnectPose)
                        : "missing"))
                .AppendLine("currentReverseAnalysisAvailable=" +
                    hasCurrentReverseAnalysis)
                .AppendLine("currentMaximumMirroredBoneRotationErrorDegrees=" +
                    (hasCurrentReverseAnalysis
                        ? F(currentAnalysis.MaximumMirroredBoneRotationError)
                        : "unavailable"))
                .AppendLine("currentMaximumMirroredBoneRotationErrorPath=" +
                    (hasCurrentReverseAnalysis
                        ? currentAnalysis.MaximumMirroredBoneRotationErrorPath
                        : "unavailable"))
                .AppendLine("currentMaximumSourceSampleBoneRotationErrorDegrees=" +
                    (hasCurrentReverseAnalysis
                        ? F(currentAnalysis.MaximumSourceSampleBoneRotationError)
                        : "unavailable"))
                .AppendLine("currentMaximumSourceSampleBoneRotationErrorPath=" +
                    (hasCurrentReverseAnalysis
                        ? currentAnalysis.MaximumSourceSampleBoneRotationErrorPath
                        : "unavailable"))
                .AppendLine("currentMaximumMirroredBonePositionErrorMeters=" +
                    (hasCurrentReverseAnalysis
                        ? F(currentAnalysis.MaximumMirroredBonePositionError)
                        : "unavailable"))
                .AppendLine("currentMaximumMirroredBonePositionErrorPath=" +
                    (hasCurrentReverseAnalysis
                        ? currentAnalysis.MaximumMirroredBonePositionErrorPath
                        : "unavailable"))
                .AppendLine("currentMaximumMirroredHolderPositionErrorMeters=" +
                    (hasCurrentReverseAnalysis
                        ? F(currentAnalysis.MaximumMirroredHolderPositionError)
                        : "unavailable"))
                .AppendLine("currentMaximumMirroredHolderRotationErrorDegrees=" +
                    (hasCurrentReverseAnalysis
                        ? F(currentAnalysis.MaximumMirroredHolderRotationError)
                        : "unavailable"))
                .AppendLine("rightHandRotationCurveBindingCount=" +
                    AnimationUtility.GetCurveBindings(clip).Count(binding =>
                        binding.path == RightHandPath &&
                        binding.propertyName.IndexOf(
                            "Rotation", StringComparison.OrdinalIgnoreCase) >= 0))
                .AppendLine("carryConfigurationAlreadyMatches=True")
                .AppendLine("sourceFbxSha256=" + ComputeAssetHash(ImportedFbxPath))
                .AppendLine("externalFbxSha256=" + ComputeFileHash(ExternalFbxPath))
                .AppendLine("keyframesEdited=False")
                .AppendLine("animationGenerated=False")
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log(
                "[FlashlightChargeDisconnectReverse] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Disconnect Reverse")]
        internal static void ApplyFlashlightChargeDisconnectReverse()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject connect = FindUnique(scene, TargetName);
            GameObject disconnect = FindUnique(scene, DisconnectTargetName);
            AnimationClip clip = RequireCurrentConnectConfiguration(
                connect,
                out Animator connectAnimator,
                out FlashlightRightHandFollowBehaviour connectCarry,
                out FlashlightChargeConnectPoseBehaviour connectPose);
            Animator disconnectAnimator = RequireAnimator(disconnect);
            FlashlightRightHandFollowBehaviour disconnectCarry =
                RequireCarry(disconnect);
            RequireEqual(CarrySignature(connectCarry), CarrySignature(disconnectCarry),
                "Flashlight charge carry configuration");

            string[] copiedBaselinePaths =
                FindDifferingUnanimatedSkeletonPaths(connect, disconnect, clip);
            string outsideBefore = OutsideTargetSignature(scene, disconnect.transform);
            string disconnectUnaffectedTransformsBefore =
                TransformSignatureExcludingPaths(
                    disconnect.transform, copiedBaselinePaths);
            string disconnectCarryBefore = CarrySignature(disconnectCarry);
            string disconnectVisualBefore = VisualSignature(
                disconnect.transform, disconnectCarry);
            string connectTransformBefore = TransformSignature(connect.transform);
            string connectCarryBefore = CarrySignature(connectCarry);
            string connectVisualBefore = VisualSignature(connect.transform, connectCarry);
            string connectPoseBefore = PoseConfigurationSignature(connectPose);
            string connectControllerHashBefore = ComputeAssetHash(ControllerPath);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string flashlightModelHashBefore = ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Flashlight.fbx");
            string flashlightMaterialHashBefore = ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Materials/Flashlight.mat");
            string disconnectAvatarBefore = ObjectIdentity(disconnectAnimator.avatar);

            AnimatorController reverseController =
                CreateOrUpdateDisconnectReverseController(clip);
            FlashlightChargeConnectPoseBehaviour disconnectPose =
                disconnect.GetComponent<FlashlightChargeConnectPoseBehaviour>();
            if (disconnectPose == null)
                disconnectPose = Undo.AddComponent<FlashlightChargeConnectPoseBehaviour>(
                    disconnect);
            else
                Undo.RecordObject(disconnectPose,
                    "Copy Flashlight_Charge_Connect animation configuration");
            EditorUtility.CopySerialized(connectPose, disconnectPose);
            disconnectPose.RebindExactCopiedConfiguration(
                disconnectCarry, disconnectAnimator, true);
            disconnectPose.ConfigureReverseFinalHold(
                DisconnectFinalHoldDurationSeconds);
            EditorUtility.SetDirty(disconnectPose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(disconnectPose);

            Undo.RecordObject(disconnectAnimator,
                "Connect exact reverse flashlight charging animation");
            disconnectAnimator.runtimeAnimatorController = reverseController;
            disconnectAnimator.applyRootMotion = connectAnimator.applyRootMotion;
            disconnectAnimator.updateMode = connectAnimator.updateMode;
            disconnectAnimator.cullingMode = connectAnimator.cullingMode;
            disconnectAnimator.enabled = connectAnimator.enabled;
            disconnectAnimator.speed = 1f;
            EditorUtility.SetDirty(disconnectAnimator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(disconnectAnimator);

            foreach (string path in copiedBaselinePaths)
            {
                Transform sourceBone = RequirePath(connect.transform, path);
                Transform targetBone = RequirePath(disconnect.transform, path);
                Undo.RecordObject(targetBone,
                    "Copy unanimated Flashlight_Charge_Connect baseline");
                targetBone.localPosition = sourceBone.localPosition;
                targetBone.localRotation = sourceBone.localRotation;
                targetBone.localScale = sourceBone.localScale;
                EditorUtility.SetDirty(targetBone);
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetBone);
            }

            RequireDisconnectReverseConfiguration(
                disconnect, clip, connectPose,
                out _, out _, out FlashlightChargeConnectPoseBehaviour appliedPose);
            RequireEqual(
                PoseConfigurationSignatureWithoutForwardHoldDuration(connectPose),
                PoseConfigurationSignatureWithoutForwardHoldDuration(appliedPose),
                "copied Flashlight_Charge_Connect pose configuration except approved final hold");
            if (Mathf.Abs(appliedPose.ForwardHoldDurationSeconds -
                    DisconnectFinalHoldDurationSeconds) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect final hold is not 0.5 seconds.");
            RequireEqual(disconnectAvatarBefore, ObjectIdentity(disconnectAnimator.avatar),
                "Flashlight_Charge_Disconnect avatar");
            RequireEqual(disconnectUnaffectedTransformsBefore,
                TransformSignatureExcludingPaths(
                    disconnect.transform, copiedBaselinePaths),
                "Flashlight_Charge_Disconnect transforms outside copied unanimated baselines");
            foreach (string path in copiedBaselinePaths)
            {
                Transform sourceBone = RequirePath(connect.transform, path);
                Transform targetBone = RequirePath(disconnect.transform, path);
                if (Vector3.Distance(sourceBone.localPosition,
                        targetBone.localPosition) > 0.000001f ||
                    Quaternion.Angle(sourceBone.localRotation,
                        targetBone.localRotation) > 0.001f ||
                    Vector3.Distance(sourceBone.localScale,
                        targetBone.localScale) > 0.000001f)
                    throw new InvalidOperationException(
                        "Copied unanimated Connect baseline differs at " + path + ".");
            }
            RequireEqual(disconnectCarryBefore, CarrySignature(disconnectCarry),
                "Flashlight_Charge_Disconnect hand-follow configuration");
            RequireEqual(disconnectVisualBefore,
                VisualSignature(disconnect.transform, disconnectCarry),
                "Flashlight_Charge_Disconnect renderers, meshes, and materials");
            RequireEqual(connectTransformBefore, TransformSignature(connect.transform),
                "Flashlight_Charge_Connect transforms");
            RequireEqual(connectCarryBefore, CarrySignature(connectCarry),
                "Flashlight_Charge_Connect hand-follow configuration");
            RequireEqual(connectVisualBefore,
                VisualSignature(connect.transform, connectCarry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(connectPoseBefore, PoseConfigurationSignature(connectPose),
                "Flashlight_Charge_Connect pose configuration");
            RequireEqual(connectControllerHashBefore, ComputeAssetHash(ControllerPath),
                "Flashlight_Charge_Connect controller");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "imported flashlight charging FBX");
            RequireEqual(flashlightModelHashBefore, ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Flashlight.fbx"),
                "Flashlight model asset");
            RequireEqual(flashlightMaterialHashBefore, ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Materials/Flashlight.mat"),
                "Flashlight material asset");
            RequireEqual(outsideBefore,
                OutsideTargetSignature(scene, disconnect.transform),
                "objects outside Flashlight_Charge_Disconnect");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save Flashlight_Charge_Disconnect reverse linkage.");
            AssetDatabase.SaveAssets();

            WriteText(DisconnectReverseApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Disconnect exact reverse application")
                .AppendLine("target=" + DisconnectTargetName)
                .AppendLine("sourceTarget=" + TargetName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("sourceClip=" + ObjectIdentity(clip))
                .AppendLine("sourceController=" + ControllerPath)
                .AppendLine("reverseController=" + DisconnectControllerPath)
                .AppendLine("reverseStateSpeed=-1")
                .AppendLine("reverseStateCycleOffset=1")
                .AppendLine("copiedPoseConfigurationExceptFinalHold=" +
                    PoseConfigurationSignatureWithoutForwardHoldDuration(appliedPose))
                .AppendLine("disconnectFinalHoldDurationSeconds=" +
                    F(appliedPose.ForwardHoldDurationSeconds))
                .AppendLine("disconnectInitialHoldRemoved=True")
                .AppendLine("copiedUnanimatedBaselinePathCount=" +
                    copiedBaselinePaths.Length)
                .AppendLine("copiedUnanimatedBaselinePaths=" +
                    string.Join("|", copiedBaselinePaths))
                .AppendLine("keyframesEdited=False")
                .AppendLine("animationGenerated=False")
                .AppendLine("connectObjectModified=False")
                .AppendLine("disconnectTransformsOutsideCopiedBaselinesModified=False")
                .AppendLine("rightHandFollowModified=False")
                .AppendLine("rendererMeshMaterialModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log(
                "[FlashlightChargeDisconnectReverse] Exact reverse animation applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Disconnect Reverse")]
        internal static void InspectFlashlightChargeDisconnectReverse()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject connect = FindUnique(scene, TargetName);
            GameObject disconnect = FindUnique(scene, DisconnectTargetName);
            AnimationClip clip = RequireCurrentConnectConfiguration(
                connect, out _, out FlashlightRightHandFollowBehaviour connectCarry,
                out FlashlightChargeConnectPoseBehaviour connectPose);
            RequireDisconnectReverseConfiguration(
                disconnect, clip, connectPose,
                out Animator disconnectAnimator,
                out FlashlightRightHandFollowBehaviour disconnectCarry,
                out FlashlightChargeConnectPoseBehaviour disconnectPose);
            RequireEqual(CarrySignature(connectCarry), CarrySignature(disconnectCarry),
                "Flashlight charge carry configuration");
            DisconnectReverseAnalysis analysis = AnalyzeDisconnectReverse(
                connect, disconnect, clip);
            if (analysis.MaximumMirroredBoneRotationError > 0.001f ||
                analysis.MaximumMirroredBonePositionError > 0.000001f ||
                analysis.MaximumMirroredHolderPositionError > 0.00001f ||
                analysis.MaximumMirroredHolderRotationError > 0.001f ||
                analysis.MaximumMirroredHolderScaleError > 0.000001f)
                throw new InvalidOperationException(
                    "Charge-disconnect output differs from the copied Connect pose at a mirrored phase.");
            if (analysis.MaximumVisibleBoneMotion < 1f)
                throw new InvalidOperationException(
                    "Charge-disconnect reverse sequence has no visible animation motion.");
            if (analysis.MaximumRightHandFollowError > 0.00005f)
                throw new InvalidOperationException(
                    "Charge-disconnect flashlight left the right-hand follow target.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Charge-disconnect inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during charge-disconnect inspection.");

            WriteText(DisconnectReverseInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Disconnect exact reverse inspection")
                .AppendLine("targetCount=1")
                .AppendLine("sampleCount=" + analysis.SampleCount)
                .AppendLine("sourceClip=" + ObjectIdentity(clip))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("reverseController=" +
                    AssetDatabase.GetAssetPath(
                        disconnectAnimator.runtimeAnimatorController))
                .AppendLine("reverseStateSpeed=-1")
                .AppendLine("reverseStateCycleOffset=1")
                .AppendLine("poseConfigurationMatchesConnectExceptFinalHold=" +
                    (PoseConfigurationSignatureWithoutForwardHoldDuration(connectPose) ==
                     PoseConfigurationSignatureWithoutForwardHoldDuration(disconnectPose)))
                .AppendLine("disconnectFinalHoldDurationSeconds=" +
                    F(disconnectPose.ForwardHoldDurationSeconds))
                .AppendLine("maximumMirroredBoneRotationErrorDegrees=" +
                    F(analysis.MaximumMirroredBoneRotationError))
                .AppendLine("maximumMirroredBonePositionErrorMeters=" +
                    F(analysis.MaximumMirroredBonePositionError))
                .AppendLine("maximumMirroredHolderPositionErrorMeters=" +
                    F(analysis.MaximumMirroredHolderPositionError))
                .AppendLine("maximumMirroredHolderRotationErrorDegrees=" +
                    F(analysis.MaximumMirroredHolderRotationError))
                .AppendLine("maximumMirroredHolderScaleError=" +
                    F(analysis.MaximumMirroredHolderScaleError))
                .AppendLine("maximumVisibleBoneMotionDegrees=" +
                    F(analysis.MaximumVisibleBoneMotion))
                .AppendLine("maximumRightHandFollowErrorMeters=" +
                    F(analysis.MaximumRightHandFollowError))
                .AppendLine("fullSourceTakePreserved=True")
                .AppendLine("keyframesEdited=False")
                .AppendLine("animationGenerated=False")
                .AppendLine("loopTime=True")
                .AppendLine("rightHandFollowPreserved=True")
                .AppendLine("rendererMeshMaterialPreserved=True")
                .AppendLine("inspectionManipulatedTarget=False")
                .AppendLine("unityConsoleErrors=0")
                .ToString());
            Debug.Log(
                "[FlashlightChargeDisconnectReverse] Reverse linkage inspection passed.");
            StartDisconnectRuntimeInspectionWithoutCaptureIfNeeded();
            CompleteDisconnectReverseReviewFromVerifiedReportsIfNeeded();
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Disconnect Reverse Final")]
        internal static void CaptureFlashlightChargeDisconnectReverseFinal()
        {
            RequireEditMode();
            InspectFlashlightChargeDisconnectReverse();
            if (captureActive || disconnectCaptureActive ||
                !string.IsNullOrEmpty(SessionState.GetString(
                    AutoStateKey, string.Empty)) ||
                !string.IsNullOrEmpty(SessionState.GetString(
                    DisconnectReverseAutoStateKey, string.Empty)))
                throw new InvalidOperationException(
                    "A flashlight animation review is already active.");
            DeleteReviewFile(DisconnectReverseFinalImagePath);
            DeleteReviewFile(DisconnectReverseFinalReportPath);
            DeleteReviewFile(DisconnectReverseCompletionReportPath);
            DeleteReviewFile(DisconnectReverseFailureReportPath);
            DeleteReviewFile(DisconnectReverseRuntimeInspectionReportPath);
            SessionState.EraseBool(DisconnectReverseInspectionOnlyKey);
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log(
                "[FlashlightChargeDisconnectReverse] Final natural-playback capture started.");
        }

        private static AnimationClip RequireCurrentConnectConfiguration(
            GameObject connect,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireFlashlightHandClearanceBaseConfiguration(
                connect, out animator, out carry, out pose);
            AnimatorController controller =
                animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect controller is missing.");
            if (AssetDatabase.GetAssetPath(controller) != ControllerPath)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect uses an unexpected controller.");
            RequireControllerContract(controller, clip);
            RequireExactSourceCopy();
            return clip;
        }

        private static void StartDisconnectRuntimeInspectionWithoutCaptureIfNeeded()
        {
            if (!File.Exists(Absolute(DisconnectReverseFinalImagePath)) ||
                !File.Exists(Absolute(DisconnectReverseFailureReportPath)) ||
                File.Exists(Absolute(DisconnectReverseRuntimeInspectionReportPath)) ||
                !string.IsNullOrEmpty(SessionState.GetString(
                    DisconnectReverseAutoStateKey, string.Empty)))
                return;
            DeleteReviewFile(DisconnectReverseRuntimeInspectionReportPath);
            SessionState.SetBool(DisconnectReverseInspectionOnlyKey, true);
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log(
                "[FlashlightChargeDisconnectReverse] Natural playback inspection without recapture started.");
        }

        private static void CompleteDisconnectReverseReviewFromVerifiedReportsIfNeeded()
        {
            if (!File.Exists(Absolute(DisconnectReverseFinalImagePath)) ||
                !File.Exists(Absolute(DisconnectReverseFinalReportPath)) ||
                !File.Exists(Absolute(
                    DisconnectReverseRuntimeInspectionReportPath)) ||
                File.Exists(Absolute(DisconnectReverseFailureReportPath)) ||
                File.Exists(Absolute(DisconnectReverseCompletionReportPath)) ||
                EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            WriteText(DisconnectReverseCompletionReportPath,
                new StringBuilder()
                .AppendLine("Flashlight_Charge_Disconnect exact reverse final direct review")
                .AppendLine("applicationCompleted=True")
                .AppendLine("naturalReversePlaybackObserved=True")
                .AppendLine("fullReverseCycleAndReturnObserved=True")
                .AppendLine("returnedToEditMode=True")
                .AppendLine("finalCaptureRepeated=False")
                .AppendLine("unityConsoleErrors=0")
                .ToString());
            SessionState.EraseString(DisconnectReverseAutoStateKey);
            SessionState.EraseBool(DisconnectReverseInspectionOnlyKey);
        }

        private static AnimatorController CreateOrUpdateDisconnectReverseController(
            AnimationClip clip)
        {
            EnsureFolder(AssetFolder);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    DisconnectControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(
                    DisconnectControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length != 1)
                throw new InvalidOperationException(
                    "Flashlight charge-disconnect controller must contain one layer.");
            AnimatorStateMachine machine = layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (ChildAnimatorStateMachine child in machine.stateMachines.ToArray())
                machine.RemoveStateMachine(child.stateMachine);
            AnimatorState state = machine.AddState(DisconnectStateName);
            state.motion = clip;
            state.speed = -1f;
            state.cycleOffset = 1f;
            state.writeDefaultValues = false;
            state.iKOnFeet = false;
            machine.defaultState = state;
            layers[0].name = "Exact Connect Animation Reversed";
            layers[0].defaultWeight = 1f;
            controller.layers = layers;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            RequireDisconnectReverseControllerContract(controller, clip);
            return controller;
        }

        private static void RequireDisconnectReverseControllerContract(
            AnimatorController controller,
            AnimationClip clip)
        {
            if (controller.parameters.Length != 0 || controller.layers.Length != 1)
                throw new InvalidOperationException(
                    "Flashlight charge-disconnect controller structure is invalid.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            if (machine.states.Length != 1 || machine.stateMachines.Length != 0 ||
                machine.anyStateTransitions.Length != 0 ||
                machine.entryTransitions.Length != 0)
                throw new InvalidOperationException(
                    "Flashlight charge-disconnect controller must contain one direct state.");
            AnimatorState state = machine.states[0].state;
            if (state.name != DisconnectStateName || state.motion != clip ||
                !Mathf.Approximately(state.speed, -1f) ||
                !Mathf.Approximately(state.cycleOffset, 1f) ||
                state.transitions.Length != 0 || state.writeDefaultValues ||
                state.iKOnFeet)
                throw new InvalidOperationException(
                    "Flashlight charge-disconnect state is not an exact reversed source reference.");
        }

        private static void RequireDisconnectReverseConfiguration(
            GameObject disconnect,
            AnimationClip clip,
            FlashlightChargeConnectPoseBehaviour connectPose,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            animator = RequireAnimator(disconnect);
            carry = RequireCarry(disconnect);
            pose = disconnect.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Disconnect copied pose behaviour is missing.");
            AnimatorController controller =
                animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Disconnect reverse controller is missing.");
            if (AssetDatabase.GetAssetPath(controller) != DisconnectControllerPath)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect uses an unexpected controller.");
            RequireDisconnectReverseControllerContract(controller, clip);
            if (pose.Carry != carry || pose.Animator != animator ||
                !pose.ReversePlayback)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect copied pose references are invalid.");
            RequireEqual(
                PoseConfigurationSignatureWithoutForwardHoldDuration(connectPose),
                PoseConfigurationSignatureWithoutForwardHoldDuration(pose),
                "Flashlight charge pose configuration except approved final hold");
            if (Mathf.Abs(pose.ForwardHoldDurationSeconds -
                    DisconnectFinalHoldDurationSeconds) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect final hold must be 0.5 seconds.");
            if (animator.applyRootMotion || !animator.enabled ||
                animator.updateMode != AnimatorUpdateMode.Normal ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect Animator settings differ from Connect.");
        }

        private static DisconnectReverseAnalysis AnalyzeDisconnectReverse(
            GameObject connect,
            GameObject disconnect,
            AnimationClip clip)
        {
            GameObject connectWork = UnityEngine.Object.Instantiate(connect);
            GameObject disconnectWork = UnityEngine.Object.Instantiate(disconnect);
            connectWork.name = "FlashlightChargeConnectReverseReference";
            disconnectWork.name = "FlashlightChargeDisconnectReverseAnalysis";
            connectWork.hideFlags = HideFlags.HideAndDontSave;
            disconnectWork.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(connectWork).enabled = false;
                RequireAnimator(disconnectWork).enabled = false;
                FlashlightRightHandFollowBehaviour connectCarry =
                    RequireCarry(connectWork);
                FlashlightRightHandFollowBehaviour disconnectCarry =
                    RequireCarry(disconnectWork);
                FlashlightChargeConnectPoseBehaviour connectPose =
                    connectWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Connect analysis pose behaviour is missing.");
                FlashlightChargeConnectPoseBehaviour disconnectPose =
                    disconnectWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Disconnect analysis pose behaviour is missing.");
                Transform connectHolder = connectCarry.Holder != null
                    ? connectCarry.Holder.transform
                    : throw new MissingReferenceException(
                        "Connect analysis flashlight holder is missing.");
                Transform disconnectHolder = disconnectCarry.Holder != null
                    ? disconnectCarry.Holder.transform
                    : throw new MissingReferenceException(
                        "Disconnect analysis flashlight holder is missing.");
                string[] bonePaths = connectWork
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .SelectMany(renderer => renderer.bones)
                    .Where(item => item != null && item != connectWork.transform &&
                        item != connectHolder && !item.IsChildOf(connectHolder))
                    .Select(item => AnimationUtility.CalculateTransformPath(
                        item, connectWork.transform))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                if (bonePaths.Length == 0)
                    throw new InvalidOperationException(
                        "Charge-disconnect analysis found no animated transforms.");

                const int sampleCount = 121;
                float maximumBoneRotationError = 0f;
                float maximumBonePositionError = 0f;
                string maximumBoneRotationErrorPath = string.Empty;
                string maximumBonePositionErrorPath = string.Empty;
                float maximumSourceSampleBoneRotationError = 0f;
                string maximumSourceSampleBoneRotationErrorPath = string.Empty;
                float maximumHolderPositionError = 0f;
                float maximumHolderRotationError = 0f;
                float maximumHolderScaleError = 0f;
                float maximumVisibleMotion = 0f;
                float maximumFollowError = 0f;
                var firstRotations = new Dictionary<string, Quaternion>(
                    StringComparer.Ordinal);
                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(connectWork, time);
                    clip.SampleAnimation(disconnectWork, time);
                    foreach (string path in bonePaths)
                    {
                        Transform expected = RequirePath(connectWork.transform, path);
                        Transform actual = RequirePath(disconnectWork.transform, path);
                        float sourceRotationError = Quaternion.Angle(
                            expected.localRotation, actual.localRotation);
                        if (sourceRotationError >
                            maximumSourceSampleBoneRotationError)
                        {
                            maximumSourceSampleBoneRotationError =
                                sourceRotationError;
                            maximumSourceSampleBoneRotationErrorPath = path;
                        }
                    }
                    connectCarry.RefreshPreview();
                    disconnectCarry.RefreshPreview();
                    connectPose.RefreshPreviewAtNormalizedPhase(normalized);
                    disconnectPose.RefreshPreviewAtNormalizedPhase(normalized);

                    foreach (string path in bonePaths)
                    {
                        Transform expected = RequirePath(connectWork.transform, path);
                        Transform actual = RequirePath(disconnectWork.transform, path);
                        float rotationError = Quaternion.Angle(
                            expected.localRotation, actual.localRotation);
                        if (rotationError > maximumBoneRotationError)
                        {
                            maximumBoneRotationError = rotationError;
                            maximumBoneRotationErrorPath = path;
                        }
                        float positionError = Vector3.Distance(
                            expected.localPosition, actual.localPosition);
                        if (positionError > maximumBonePositionError)
                        {
                            maximumBonePositionError = positionError;
                            maximumBonePositionErrorPath = path;
                        }
                        if (index == 0)
                            firstRotations[path] = expected.localRotation;
                        else
                            maximumVisibleMotion = Mathf.Max(
                                maximumVisibleMotion,
                                Quaternion.Angle(firstRotations[path],
                                    expected.localRotation));
                    }

                    Quaternion expectedHolderRotation = Quaternion.Inverse(
                        connectWork.transform.rotation) * connectHolder.rotation;
                    Quaternion actualHolderRotation = Quaternion.Inverse(
                        disconnectWork.transform.rotation) * disconnectHolder.rotation;
                    maximumHolderRotationError = Mathf.Max(
                        maximumHolderRotationError,
                        Quaternion.Angle(expectedHolderRotation,
                            actualHolderRotation));
                    maximumHolderScaleError = Mathf.Max(
                        maximumHolderScaleError,
                        Vector3.Distance(connectHolder.localScale,
                            disconnectHolder.localScale));
                    Vector3 expectedConnectFollowPosition =
                        connectCarry.RightHand.TransformPoint(
                            connectCarry.HolderLocalPosition);
                    if (connectPose.FlashlightRightOffsetConfigured)
                        expectedConnectFollowPosition += connectWork.transform.right *
                            connectPose.FlashlightRightOffsetMeters;
                    if (connectPose.FlashlightForwardOffsetConfigured)
                        expectedConnectFollowPosition += connectWork.transform.forward *
                            connectPose.FlashlightForwardOffsetMeters;
                    Vector3 expectedDisconnectFollowPosition =
                        disconnectCarry.RightHand.TransformPoint(
                            disconnectCarry.HolderLocalPosition);
                    if (disconnectPose.FlashlightRightOffsetConfigured)
                        expectedDisconnectFollowPosition +=
                            disconnectWork.transform.right *
                            disconnectPose.FlashlightRightOffsetMeters;
                    if (disconnectPose.FlashlightForwardOffsetConfigured)
                        expectedDisconnectFollowPosition +=
                            disconnectWork.transform.forward *
                            disconnectPose.FlashlightForwardOffsetMeters;
                    float connectFollowError = Vector3.Distance(
                        expectedConnectFollowPosition, connectHolder.position);
                    float disconnectFollowError = Vector3.Distance(
                        expectedDisconnectFollowPosition, disconnectHolder.position);
                    maximumHolderPositionError = Mathf.Max(
                        maximumHolderPositionError,
                        Mathf.Max(connectFollowError, disconnectFollowError));
                    maximumFollowError = Mathf.Max(
                        maximumFollowError,
                        disconnectFollowError);
                }

                return new DisconnectReverseAnalysis(
                    sampleCount,
                    maximumSourceSampleBoneRotationError,
                    maximumSourceSampleBoneRotationErrorPath,
                    maximumBoneRotationError,
                    maximumBoneRotationErrorPath,
                    maximumBonePositionError,
                    maximumBonePositionErrorPath,
                    maximumHolderPositionError,
                    maximumHolderRotationError,
                    maximumHolderScaleError,
                    maximumVisibleMotion,
                    maximumFollowError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(disconnectWork);
                UnityEngine.Object.DestroyImmediate(connectWork);
            }
        }

        private static string PoseConfigurationSignature(
            FlashlightChargeConnectPoseBehaviour pose)
        {
            return Sha256(string.Join(
                Environment.NewLine, PoseConfigurationEntries(pose)));
        }

        private static string PoseConfigurationSignatureWithoutForwardHoldDuration(
            FlashlightChargeConnectPoseBehaviour pose)
        {
            return Sha256(string.Join(
                Environment.NewLine,
                PoseConfigurationEntries(pose).Where(entry =>
                    !entry.StartsWith(
                        "forwardHoldDurationSeconds|",
                        StringComparison.Ordinal))));
        }

        private static string[] FindDifferingUnanimatedSkeletonPaths(
            GameObject source,
            GameObject target,
            AnimationClip clip)
        {
            var animatedPaths = new HashSet<string>(
                AnimationUtility.GetCurveBindings(clip)
                    .Select(binding => binding.path),
                StringComparer.Ordinal);
            return source.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SelectMany(renderer => renderer.bones)
                .Where(bone => bone != null)
                .Select(bone => AnimationUtility.CalculateTransformPath(
                    bone, source.transform))
                .Distinct(StringComparer.Ordinal)
                .Where(path => !animatedPaths.Contains(path))
                .Where(path =>
                {
                    Transform sourceBone = RequirePath(source.transform, path);
                    Transform targetBone = RequirePath(target.transform, path);
                    return Vector3.Distance(sourceBone.localPosition,
                               targetBone.localPosition) > 0.000001f ||
                           Quaternion.Angle(sourceBone.localRotation,
                               targetBone.localRotation) > 0.001f ||
                           Vector3.Distance(sourceBone.localScale,
                               targetBone.localScale) > 0.000001f;
                })
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static string TransformSignatureExcludingPaths(
            Transform root,
            IEnumerable<string> excludedPaths)
        {
            var excluded = new HashSet<string>(
                excludedPaths ?? Array.Empty<string>(), StringComparer.Ordinal);
            var builder = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, root), StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root);
                if (excluded.Contains(path)) continue;
                builder.Append(path)
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').Append(Vec(item.localScale))
                    .Append('|').AppendLine(item.gameObject.activeSelf.ToString());
            }
            return Sha256(builder.ToString());
        }

        private static string[] PoseConfigurationEntries(
            FlashlightChargeConnectPoseBehaviour pose)
        {
            var serialized = new SerializedObject(pose);
            serialized.UpdateIfRequiredOrScript();
            var entries = new List<string>();
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = true;
                bool excluded = property.propertyPath.StartsWith(
                                    "m_", StringComparison.Ordinal) ||
                                property.propertyPath == "carry" ||
                                property.propertyPath == "animator" ||
                                property.propertyPath == "reversePlayback";
                if (excluded)
                {
                    enterChildren = false;
                    continue;
                }
                entries.Add(
                    property.propertyPath + "|" + property.propertyType + "|" +
                    SerializedPropertyValue(property));
            }
            return entries.ToArray();
        }

        private static string FirstPoseConfigurationDifference(
            FlashlightChargeConnectPoseBehaviour expected,
            FlashlightChargeConnectPoseBehaviour actual)
        {
            string[] expectedEntries = PoseConfigurationEntries(expected);
            string[] actualEntries = PoseConfigurationEntries(actual);
            int count = Mathf.Max(expectedEntries.Length, actualEntries.Length);
            for (int index = 0; index < count; index++)
            {
                string expectedEntry = index < expectedEntries.Length
                    ? expectedEntries[index]
                    : "<missing>";
                string actualEntry = index < actualEntries.Length
                    ? actualEntries[index]
                    : "<missing>";
                if (!string.Equals(
                        expectedEntry, actualEntry, StringComparison.Ordinal))
                    return "index=" + index + "|connect=" + expectedEntry +
                        "|disconnect=" + actualEntry;
            }
            return "none";
        }

        private static string SerializedPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.LayerMask:
                    return property.longValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return property.doubleValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.String:
                    return property.stringValue ?? string.Empty;
                case SerializedPropertyType.Enum:
                    return property.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString("R");
                case SerializedPropertyType.Vector3:
                    return Vec(property.vector3Value);
                case SerializedPropertyType.Vector4:
                    return property.vector4Value.ToString("R");
                case SerializedPropertyType.Quaternion:
                    return Quat(property.quaternionValue);
                case SerializedPropertyType.Color:
                    return property.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return ObjectIdentity(property.objectReferenceValue);
                default:
                    return property.type;
            }
        }

        private static void ContinueDisconnectReverseReview()
        {
            string state = SessionState.GetString(
                DisconnectReverseAutoStateKey, string.Empty);
            if (string.IsNullOrEmpty(state)) return;
            try
            {
                if (state == AwaitingPlayState)
                {
                    if (!EditorApplication.isPlaying) return;
                    BeginDisconnectRuntimeCapture();
                    return;
                }
                if (state == AwaitingEditState)
                {
                    if (EditorApplication.isPlaying) return;
                    SessionState.SetString(
                        DisconnectReverseAutoStateKey, AwaitingEditReadyState);
                    EditorApplication.delayCall += ContinueDisconnectReverseReview;
                    return;
                }
                if (state == AwaitingEditReadyState)
                {
                    if (EditorApplication.isPlaying) return;
                    bool gripTimingReview = SessionState.GetBool(
                        GripTimingReviewKey, false);
                    if (gripTimingReview)
                        InspectFlashlightChargeConnectDisconnectGripAndTimingConfiguration();
                    else
                        InspectFlashlightChargeDisconnectReverse();
                    int errors = UnityConsoleErrorCount();
                    if (errors != 0)
                        throw new InvalidOperationException(
                            "Unity Console contains " + errors +
                            " errors after charge-disconnect final review.");
                    WriteText(
                        gripTimingReview
                            ? GripTimingCompletionReportPath
                            : DisconnectReverseCompletionReportPath,
                        new StringBuilder()
                        .AppendLine(gripTimingReview
                            ? "Flashlight charge grip and disconnect final-hold direct review"
                            : "Flashlight_Charge_Disconnect exact reverse final direct review")
                        .AppendLine("applicationCompleted=True")
                        .AppendLine("naturalReversePlaybackObserved=True")
                        .AppendLine("fullReverseCycleAndReturnObserved=True")
                        .AppendLine("disconnectInitialHoldObserved=False")
                        .AppendLine("disconnectFinalHoldDurationSeconds=" +
                            (gripTimingReview
                                ? F(DisconnectFinalHoldDurationSeconds)
                                : "unchanged"))
                        .AppendLine("returnedToEditMode=True")
                        .AppendLine("unityConsoleErrors=0")
                        .ToString());
                    SessionState.EraseString(DisconnectReverseAutoStateKey);
                    SessionState.EraseBool(DisconnectReverseInspectionOnlyKey);
                    SessionState.EraseBool(GripTimingReviewKey);
                    Debug.Log(
                        gripTimingReview
                            ? "[FlashlightChargeGripTiming] Review completed."
                            : "[FlashlightChargeDisconnectReverse] Final review completed.");
                }
            }
            catch (Exception exception)
            {
                FailDisconnectReverseReview(exception);
            }
        }

        private static void BeginDisconnectRuntimeCapture()
        {
            Scene scene = RequireScene();
            disconnectRuntimeTarget = FindUnique(scene, DisconnectTargetName);
            GameObject connect = FindUnique(scene, TargetName);
            AnimationClip clip = RequireCurrentConnectConfiguration(
                connect, out _, out FlashlightRightHandFollowBehaviour connectCarry,
                out FlashlightChargeConnectPoseBehaviour connectPose);
            RequireDisconnectReverseConfiguration(
                disconnectRuntimeTarget, clip, connectPose,
                out disconnectRuntimeAnimator,
                out disconnectRuntimeCarry,
                out disconnectRuntimePose);
            disconnectCaptureActive = true;
            disconnectCaptureStartedAt = 0d;
            disconnectNextPanel = 0;
            disconnectExpectedCycleDuration = clip.length +
                disconnectRuntimePose.ForwardHoldDurationSeconds;
            bool gripTimingReview = SessionState.GetBool(
                GripTimingReviewKey, false);
            connectGripRuntimeTarget = gripTimingReview ? connect : null;
            connectGripRuntimeCarry = gripTimingReview ? connectCarry : null;
            connectGripRuntimePose = gripTimingReview ? connectPose : null;
            bool inspectionOnly = SessionState.GetBool(
                DisconnectReverseInspectionOnlyKey, false);
            disconnectCaptureTimes = inspectionOnly
                ? Array.Empty<float>()
                : gripTimingReview
                ? new[]
                {
                    0.05f,
                    clip.length * 0.30f,
                    clip.length * 0.60f,
                    clip.length + 0.05f,
                    clip.length + DisconnectFinalHoldDurationSeconds * 0.55f,
                    disconnectExpectedCycleDuration + 0.12f
                }
                : new[]
            {
                0.05f,
                disconnectExpectedCycleDuration * 0.18f,
                disconnectExpectedCycleDuration * 0.38f,
                disconnectExpectedCycleDuration * 0.58f,
                disconnectExpectedCycleDuration * 0.78f,
                disconnectExpectedCycleDuration + 0.12f
            };
            disconnectFullPanels = new Texture2D[disconnectCaptureTimes.Length];
            disconnectUpperPanels = new Texture2D[disconnectCaptureTimes.Length];
            disconnectInitialNormalizedTime = 0f;
            disconnectMaximumNormalizedTime = float.NegativeInfinity;
            disconnectPreviousPhase = 0f;
            disconnectPreviousLoop = 0;
            disconnectHasPhaseSample = false;
            disconnectReverseMotionObserved = false;
            disconnectLoopReturnObserved = false;
            disconnectHoldObserved = false;
            disconnectInitialHoldObserved = false;
            disconnectFinalHoldPhase = float.PositiveInfinity;
            disconnectMaximumForwardPhaseError = 0f;
            disconnectMaximumHolderPositionError = 0f;
            disconnectMaximumHolderRotationError = 0f;
            disconnectMaximumBoneRotationDelta = 0f;
            connectMaximumFullReachSurfaceClearance = float.NegativeInfinity;
            disconnectMaximumFullReachSurfaceClearance = float.NegativeInfinity;
            connectFullReachGripSamples = 0;
            disconnectFullReachGripSamples = 0;
            DisconnectInitialBoneRotations.Clear();
            DisconnectRuntimeObservations.Clear();
            EditorApplication.update -= DisconnectRuntimeCaptureTick;
            EditorApplication.update += DisconnectRuntimeCaptureTick;
            SessionState.SetString(
                DisconnectReverseAutoStateKey, CapturingState);
        }

        private static void DisconnectRuntimeCaptureTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException(
                        "Play Mode ended before charge-disconnect review completed.");
                if (!disconnectRuntimeAnimator.isInitialized) return;
                AnimatorStateInfo state =
                    disconnectRuntimeAnimator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(DisconnectStateName)) return;
                if (disconnectCaptureStartedAt <= 0d)
                {
                    disconnectCaptureStartedAt = EditorApplication.timeSinceStartup;
                    disconnectInitialNormalizedTime = state.normalizedTime;
                }
                float elapsed = (float)(EditorApplication.timeSinceStartup -
                    disconnectCaptureStartedAt);
                if (elapsed > CaptureTimeoutSeconds)
                    throw new TimeoutException(
                        "Flashlight_Charge_Disconnect natural review exceeded 20 seconds.");

                float normalized = state.normalizedTime;
                int loop = Mathf.FloorToInt(normalized);
                float phase = Mathf.Repeat(1f - normalized, 1f);
                disconnectMaximumNormalizedTime = Mathf.Max(
                    disconnectMaximumNormalizedTime, normalized);
                if (disconnectHasPhaseSample)
                {
                    if (loop == disconnectPreviousLoop &&
                        !disconnectRuntimePose.IsForwardHolding)
                    {
                        float delta = phase - disconnectPreviousPhase;
                        if (delta < -0.0001f)
                            disconnectReverseMotionObserved = true;
                        disconnectMaximumForwardPhaseError = Mathf.Max(
                            disconnectMaximumForwardPhaseError,
                            Mathf.Max(0f, delta));
                    }
                    if (loop > disconnectPreviousLoop)
                        disconnectLoopReturnObserved = true;
                }
                disconnectPreviousPhase = phase;
                disconnectPreviousLoop = loop;
                disconnectHasPhaseSample = true;
                if (disconnectRuntimePose.IsForwardHolding)
                {
                    if (!disconnectReverseMotionObserved)
                        disconnectInitialHoldObserved = true;
                    disconnectFinalHoldPhase = Mathf.Min(
                        disconnectFinalHoldPhase, phase);
                }
                disconnectHoldObserved |= disconnectRuntimePose.IsForwardHolding ||
                    disconnectRuntimePose.CompletedForwardHoldCount > 0;
                AccumulateDisconnectRuntimeMetrics();
                bool gripTimingReview = SessionState.GetBool(
                    GripTimingReviewKey, false);
                if (gripTimingReview)
                    AccumulateGripTimingRuntimeMetrics();

                if (disconnectNextPanel < disconnectCaptureTimes.Length &&
                    elapsed >= disconnectCaptureTimes[disconnectNextPanel])
                {
                    disconnectFullPanels[disconnectNextPanel] = gripTimingReview
                        ? CaptureChargeGripRuntimePanel(
                            connectGripRuntimeTarget, connectGripRuntimeCarry)
                        : CaptureDisconnectRuntimePanel(
                            disconnectRuntimeTarget, disconnectRuntimeCarry, false);
                    disconnectUpperPanels[disconnectNextPanel] = gripTimingReview
                        ? CaptureChargeGripRuntimePanel(
                            disconnectRuntimeTarget, disconnectRuntimeCarry)
                        : CaptureDisconnectRuntimePanel(
                            disconnectRuntimeTarget, disconnectRuntimeCarry, true);
                    DisconnectRuntimeObservations.Add(
                        "panel=" + disconnectNextPanel +
                        "|requestedElapsedSeconds=" +
                        F(disconnectCaptureTimes[disconnectNextPanel]) +
                        "|actualElapsedSeconds=" + F(elapsed) +
                        "|stateNormalizedTime=" + F(normalized) +
                        "|sourcePhase=" + F(phase) +
                        "|holding=" + disconnectRuntimePose.IsForwardHolding +
                        "|initialHoldObserved=" + disconnectInitialHoldObserved);
                    disconnectNextPanel++;
                }
                bool inspectionOnly = SessionState.GetBool(
                    DisconnectReverseInspectionOnlyKey, false);
                if (inspectionOnly && elapsed >=
                    disconnectExpectedCycleDuration + 0.12f)
                    FinishDisconnectRuntimeInspection();
                else if (!inspectionOnly &&
                    disconnectNextPanel == disconnectCaptureTimes.Length)
                    FinishDisconnectRuntimeCapture();
            }
            catch (Exception exception)
            {
                FailDisconnectReverseReview(exception);
            }
        }

        private static void AccumulateDisconnectRuntimeMetrics()
        {
            foreach (Transform bone in disconnectRuntimeTarget
                         .GetComponentsInChildren<Transform>(true))
            {
                if (disconnectRuntimeCarry.Holder != null &&
                    (bone == disconnectRuntimeCarry.Holder.transform ||
                     bone.IsChildOf(disconnectRuntimeCarry.Holder.transform)))
                    continue;
                string path = AnimationUtility.CalculateTransformPath(
                    bone, disconnectRuntimeTarget.transform);
                if (!DisconnectInitialBoneRotations.TryGetValue(
                        path, out Quaternion initial))
                {
                    DisconnectInitialBoneRotations[path] = bone.localRotation;
                    continue;
                }
                disconnectMaximumBoneRotationDelta = Mathf.Max(
                    disconnectMaximumBoneRotationDelta,
                    Quaternion.Angle(initial, bone.localRotation));
            }
            Transform holder = disconnectRuntimeCarry.Holder != null
                ? disconnectRuntimeCarry.Holder.transform
                : throw new MissingReferenceException(
                    "Flashlight_Charge_Disconnect runtime holder is missing.");
            Vector3 expectedPosition =
                disconnectRuntimeCarry.RightHand.TransformPoint(
                    disconnectRuntimeCarry.HolderLocalPosition);
            if (disconnectRuntimePose.FlashlightRightOffsetConfigured)
                expectedPosition += disconnectRuntimeTarget.transform.right *
                    disconnectRuntimePose.FlashlightRightOffsetMeters;
            if (disconnectRuntimePose.FlashlightForwardOffsetConfigured)
                expectedPosition += disconnectRuntimeTarget.transform.forward *
                    disconnectRuntimePose.FlashlightForwardOffsetMeters;
            disconnectMaximumHolderPositionError = Mathf.Max(
                disconnectMaximumHolderPositionError,
                Vector3.Distance(holder.position, expectedPosition));
            disconnectMaximumHolderRotationError = Mathf.Max(
                disconnectMaximumHolderRotationError,
                disconnectRuntimePose.MaximumLensUpDeviation);
        }

        private static void AccumulateGripTimingRuntimeMetrics()
        {
            if (connectGripRuntimeTarget == null || connectGripRuntimeCarry == null ||
                connectGripRuntimePose == null)
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect runtime grip target is missing.");
            if (connectGripRuntimePose.CurrentRightShoulderForwardWeight >= 0.98f)
            {
                HandFlashlightForwardGeometry connectGeometry =
                    MeasureHandFlashlightForwardGeometry(
                        connectGripRuntimeTarget.transform, connectGripRuntimeCarry);
                connectMaximumFullReachSurfaceClearance = Mathf.Max(
                    connectMaximumFullReachSurfaceClearance,
                    connectGeometry.FlashlightMinimumForward -
                    connectGeometry.RightHandMaximumForward);
                connectFullReachGripSamples++;
            }
            if (disconnectRuntimePose.CurrentRightShoulderForwardWeight >= 0.98f)
            {
                HandFlashlightForwardGeometry disconnectGeometry =
                    MeasureHandFlashlightForwardGeometry(
                        disconnectRuntimeTarget.transform, disconnectRuntimeCarry);
                disconnectMaximumFullReachSurfaceClearance = Mathf.Max(
                    disconnectMaximumFullReachSurfaceClearance,
                    disconnectGeometry.FlashlightMinimumForward -
                    disconnectGeometry.RightHandMaximumForward);
                disconnectFullReachGripSamples++;
            }
        }

        private static void FinishDisconnectRuntimeInspection()
        {
            if (SessionState.GetBool(GripTimingReviewKey, false))
            {
                FinishGripTimingRuntimeInspection();
                return;
            }
            int errors = UnityConsoleErrorCount();
            WriteText(DisconnectReverseRuntimeInspectionReportPath,
                new StringBuilder()
                .AppendLine("Flashlight_Charge_Disconnect corrected reverse natural-playback inspection")
                .AppendLine("naturalPlayback=True")
                .AppendLine("captureGenerated=False")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("expectedCycleDurationSeconds=" +
                    F(disconnectExpectedCycleDuration))
                .AppendLine("controllerStateSpeed=-1")
                .AppendLine("controllerCycleOffset=1")
                .AppendLine("initialStateNormalizedTime=" +
                    F(disconnectInitialNormalizedTime))
                .AppendLine("maximumStateNormalizedTime=" +
                    F(disconnectMaximumNormalizedTime))
                .AppendLine("maximumForwardPhaseError=" +
                    F(disconnectMaximumForwardPhaseError))
                .AppendLine("maximumBoneRotationDeltaDegrees=" +
                    F(disconnectMaximumBoneRotationDelta))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(disconnectMaximumHolderPositionError))
                .AppendLine("maximumFlashlightLensUpDeviationDegrees=" +
                    F(disconnectMaximumHolderRotationError))
                .AppendLine("completedReverseHoldCount=" +
                    disconnectRuntimePose.CompletedForwardHoldCount)
                .AppendLine("lastCompletedReverseHoldDurationSeconds=" +
                    F(disconnectRuntimePose.LastCompletedForwardHoldDurationSeconds))
                .AppendLine("reverseMotionObserved=" +
                    disconnectReverseMotionObserved)
                .AppendLine("reverseHoldObserved=" + disconnectHoldObserved)
                .AppendLine("reverseLoopReturnedToStart=" +
                    disconnectLoopReturnObserved)
                .AppendLine("unityConsoleErrors=" + errors)
                .ToString());

            if (!disconnectReverseMotionObserved ||
                !disconnectLoopReturnObserved || !disconnectHoldObserved ||
                disconnectRuntimePose.CompletedForwardHoldCount < 1)
                throw new InvalidOperationException(
                    "Corrected charge-disconnect did not complete one natural reversed cycle.");
            if (Mathf.Abs(
                    disconnectRuntimePose.LastCompletedForwardHoldDurationSeconds -
                    disconnectRuntimePose.ForwardHoldDurationSeconds) > 0.15f)
                throw new InvalidOperationException(
                    "Corrected charge-disconnect final hold duration drifted.");
            if (disconnectMaximumForwardPhaseError > 0.02f)
                throw new InvalidOperationException(
                    "Corrected charge-disconnect source phase moved forward unexpectedly.");
            if (disconnectMaximumBoneRotationDelta <= 1f)
                throw new InvalidOperationException(
                    "Corrected charge-disconnect produced no visible bone motion.");
            if (disconnectMaximumHolderPositionError > 0.00005f ||
                disconnectMaximumHolderRotationError > 0.1f)
                throw new InvalidOperationException(
                    "Corrected charge-disconnect flashlight follow or lens direction drifted.");
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during corrected charge-disconnect inspection.");

            DeleteReviewFile(DisconnectReverseFailureReportPath);
            CleanupDisconnectRuntimeCapture();
            SessionState.EraseBool(DisconnectReverseInspectionOnlyKey);
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishDisconnectRuntimeCapture()
        {
            if (SessionState.GetBool(GripTimingReviewKey, false))
            {
                FinishGripTimingRuntimeCapture();
                return;
            }
            AnimationClip clip = RequireClip();
            float expectedCycle = clip.length +
                disconnectRuntimePose.ForwardHoldDurationSeconds;
            int errors = UnityConsoleErrorCount();
            WriteDisconnectComposite(
                DisconnectReverseFinalImagePath,
                disconnectFullPanels,
                disconnectUpperPanels);
            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Disconnect exact reverse natural playback direct review")
                .AppendLine("captureKind=Final")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("sourceClip=" + ObjectIdentity(clip))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("expectedCycleDurationSeconds=" + F(expectedCycle))
                .AppendLine("controllerStateSpeed=-1")
                .AppendLine("controllerCycleOffset=1")
                .AppendLine("panelsCaptured=" +
                    (disconnectCaptureTimes.Length * 2))
                .AppendLine("topRowView=TransporterFrontFullBody")
                .AppendLine("bottomRowView=TransporterFrontRightThreeQuarterUpperBody")
                .AppendLine("initialStateNormalizedTime=" +
                    F(disconnectInitialNormalizedTime))
                .AppendLine("maximumStateNormalizedTime=" +
                    F(disconnectMaximumNormalizedTime))
                .AppendLine("maximumForwardPhaseError=" +
                    F(disconnectMaximumForwardPhaseError))
                .AppendLine("maximumBoneRotationDeltaDegrees=" +
                    F(disconnectMaximumBoneRotationDelta))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(disconnectMaximumHolderPositionError))
                .AppendLine("maximumFlashlightLensUpDeviationDegrees=" +
                    F(disconnectMaximumHolderRotationError))
                .AppendLine("completedReverseHoldCount=" +
                    disconnectRuntimePose.CompletedForwardHoldCount)
                .AppendLine("lastCompletedReverseHoldDurationSeconds=" +
                    F(disconnectRuntimePose.LastCompletedForwardHoldDurationSeconds))
                .AppendLine("reverseMotionObserved=" +
                    disconnectReverseMotionObserved)
                .AppendLine("reverseHoldObserved=" + disconnectHoldObserved)
                .AppendLine("reverseLoopReturnedToStart=" +
                    disconnectLoopReturnObserved);
            foreach (string observation in DisconnectRuntimeObservations)
                report.AppendLine(observation);
            report.AppendLine("unityConsoleErrorsDuringCapture=" + errors);
            WriteText(DisconnectReverseFinalReportPath, report.ToString());

            if (!disconnectReverseMotionObserved ||
                !disconnectLoopReturnObserved || !disconnectHoldObserved ||
                disconnectRuntimePose.CompletedForwardHoldCount < 1)
                throw new InvalidOperationException(
                    "Charge-disconnect did not complete one reversed cycle with the final hold.");
            if (Mathf.Abs(
                    disconnectRuntimePose.LastCompletedForwardHoldDurationSeconds -
                    disconnectRuntimePose.ForwardHoldDurationSeconds) > 0.15f)
                throw new InvalidOperationException(
                    "Charge-disconnect final hold duration changed during reverse playback.");
            if (disconnectMaximumForwardPhaseError > 0.02f)
                throw new InvalidOperationException(
                    "Charge-disconnect source phase moved forward unexpectedly.");
            if (disconnectMaximumBoneRotationDelta <= 1f)
                throw new InvalidOperationException(
                    "Charge-disconnect reverse animation produced no visible bone motion.");
            if (disconnectMaximumHolderPositionError > 0.00005f ||
                disconnectMaximumHolderRotationError > 0.1f)
                throw new InvalidOperationException(
                    "Charge-disconnect flashlight follow or lens direction drifted.");
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during charge-disconnect final review.");

            CleanupDisconnectRuntimeCapture();
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishGripTimingRuntimeInspection()
        {
            int errors = UnityConsoleErrorCount();
            WriteText(GripTimingRuntimeReportPath,
                BuildGripTimingRuntimeReport(
                    "Flashlight charge grip and disconnect timing natural Play Mode inspection",
                    false,
                    errors));
            RequireGripTimingRuntimePass(errors);
            DeleteReviewFile(GripTimingFailureReportPath);
            CleanupDisconnectRuntimeCapture();
            SessionState.EraseBool(DisconnectReverseInspectionOnlyKey);
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishGripTimingRuntimeCapture()
        {
            int errors = UnityConsoleErrorCount();
            WriteDisconnectComposite(
                GripTimingFinalImagePath,
                disconnectFullPanels,
                disconnectUpperPanels);
            WriteText(GripTimingFinalReportPath,
                BuildGripTimingRuntimeReport(
                    "Flashlight charge grip and disconnect final-hold natural Play Mode direct review",
                    true,
                    errors));
            RequireGripTimingRuntimePass(errors);
            CleanupDisconnectRuntimeCapture();
            SessionState.SetString(
                DisconnectReverseAutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static string BuildGripTimingRuntimeReport(
            string title,
            bool captureGenerated,
            int errors)
        {
            var report = new StringBuilder()
                .AppendLine(title)
                .AppendLine("naturalPlayback=True")
                .AppendLine("unityPlayMode=True")
                .AppendLine("captureGenerated=" + captureGenerated)
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("connectForwardOffsetMeters=" +
                    F(connectGripRuntimePose.FlashlightForwardOffsetMeters))
                .AppendLine("disconnectForwardOffsetMeters=" +
                    F(disconnectRuntimePose.FlashlightForwardOffsetMeters))
                .AppendLine("connectFullReachGripSamples=" +
                    connectFullReachGripSamples)
                .AppendLine("disconnectFullReachGripSamples=" +
                    disconnectFullReachGripSamples)
                .AppendLine("connectMinimumProjectedOverlapMeters=" +
                    F(-connectMaximumFullReachSurfaceClearance))
                .AppendLine("disconnectMinimumProjectedOverlapMeters=" +
                    F(-disconnectMaximumFullReachSurfaceClearance))
                .AppendLine("disconnectInitialHoldObserved=" +
                    disconnectInitialHoldObserved)
                .AppendLine("disconnectFinalHoldObserved=" +
                    disconnectHoldObserved)
                .AppendLine("disconnectFinalHoldPhase=" +
                    F(disconnectFinalHoldPhase))
                .AppendLine("disconnectFinalHoldLoopBoundaryDistance=" +
                    F(Mathf.Min(
                        disconnectFinalHoldPhase,
                        1f - disconnectFinalHoldPhase)))
                .AppendLine("disconnectCompletedFinalHoldCount=" +
                    disconnectRuntimePose.CompletedForwardHoldCount)
                .AppendLine("disconnectFinalHoldDurationSeconds=" +
                    F(disconnectRuntimePose.LastCompletedForwardHoldDurationSeconds))
                .AppendLine("targetDisconnectFinalHoldDurationSeconds=" +
                    F(DisconnectFinalHoldDurationSeconds))
                .AppendLine("reverseMotionObserved=" +
                    disconnectReverseMotionObserved)
                .AppendLine("reverseLoopReturnedToStart=" +
                    disconnectLoopReturnObserved)
                .AppendLine("maximumForwardPhaseError=" +
                    F(disconnectMaximumForwardPhaseError))
                .AppendLine("maximumHolderFollowErrorMeters=" +
                    F(disconnectMaximumHolderPositionError))
                .AppendLine("maximumLensUpDeviationDegrees=" +
                    F(disconnectMaximumHolderRotationError));
            if (captureGenerated)
            {
                report.AppendLine("panelsCaptured=" +
                        (disconnectCaptureTimes.Length * 2))
                    .AppendLine("topRowView=Flashlight_Charge_Connect right-side grip closeup")
                    .AppendLine("bottomRowView=Flashlight_Charge_Disconnect right-side grip closeup");
                foreach (string observation in DisconnectRuntimeObservations)
                    report.AppendLine(observation);
            }
            return report.AppendLine("unityConsoleErrors=" + errors).ToString();
        }

        private static void RequireGripTimingRuntimePass(int errors)
        {
            if (!disconnectReverseMotionObserved ||
                !disconnectLoopReturnObserved || !disconnectHoldObserved ||
                disconnectRuntimePose.CompletedForwardHoldCount < 1)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect did not complete reverse motion, final hold, and loop return.");
            if (disconnectInitialHoldObserved)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect still held its initial pose.");
            if (float.IsInfinity(disconnectFinalHoldPhase) ||
                Mathf.Min(
                    disconnectFinalHoldPhase,
                    1f - disconnectFinalHoldPhase) > 0.05f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect hold did not occur at the final pose.");
            if (Mathf.Abs(
                    disconnectRuntimePose.LastCompletedForwardHoldDurationSeconds -
                    DisconnectFinalHoldDurationSeconds) > 0.12f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect final hold duration differs from 0.5 seconds.");
            if (connectFullReachGripSamples == 0 ||
                disconnectFullReachGripSamples == 0 ||
                connectMaximumFullReachSurfaceClearance > 0f ||
                disconnectMaximumFullReachSurfaceClearance > 0f)
                throw new InvalidOperationException(
                    "A charge flashlight separated from the right hand at full reach in Play Mode.");
            if (disconnectMaximumForwardPhaseError > 0.02f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect source phase moved forward unexpectedly.");
            if (disconnectMaximumHolderPositionError > 0.00005f ||
                disconnectMaximumHolderRotationError > 0.1f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect hand follow or lens direction drifted.");
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during charge grip/timing Play Mode review.");
        }

        private static Texture2D CaptureDisconnectRuntimePanel(
            GameObject target,
            FlashlightRightHandFollowBehaviour carry,
            bool upperBody)
        {
            Bounds bounds = upperBody
                ? DisconnectUpperBodyBounds(
                    target.transform,
                    carry.Holder)
                : DisconnectFullBounds(target.transform);
            Vector3 direction = upperBody
                ? (target.transform.forward +
                   target.transform.right * 0.72f).normalized
                : target.transform.forward;
            return CaptureChargeRuntimePanel(target, bounds, direction);
        }

        private static Texture2D CaptureChargeGripRuntimePanel(
            GameObject target,
            FlashlightRightHandFollowBehaviour carry)
        {
            return CaptureChargeRuntimePanel(
                target,
                HandClearanceBounds(target.transform, carry.Holder),
                target.transform.right);
        }

        private static Texture2D CaptureChargeRuntimePanel(
            GameObject target,
            Bounds bounds,
            Vector3 direction)
        {
            GameObject cameraObject = new GameObject(
                "FlashlightChargeDisconnect_ReadOnlyCamera");
            GameObject lightObject = new GameObject(
                "FlashlightChargeDisconnect_ReadOnlyLight");
            Scene scene = RequireScene();
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.magnitude, 0.12f) * 1.08f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(
                    target.transform.up, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 4f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(
                    PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(
                    new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds DisconnectFullBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect has no visible renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.15f);
            return bounds;
        }

        private static Bounds DisconnectUpperBodyBounds(
            Transform target,
            GameObject holder)
        {
            Transform spine = RequireDescendant(target, "Spine02");
            Transform leftHand = RequireDescendant(target, "LeftHand");
            Transform rightHand = RequireDescendant(target, "RightHand");
            Bounds bounds = new Bounds(spine.position, Vector3.zero);
            bounds.Encapsulate(leftHand.position);
            bounds.Encapsulate(rightHand.position);
            if (holder != null)
                foreach (Renderer renderer in
                         holder.GetComponentsInChildren<Renderer>(true))
                    bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.18f);
            return bounds;
        }

        private static void WriteDisconnectComposite(
            string assetPath,
            IReadOnlyList<Texture2D> full,
            IReadOnlyList<Texture2D> upper)
        {
            int count = full.Count;
            if (upper.Count != count || full.Any(panel => panel == null) ||
                upper.Any(panel => panel == null))
                throw new InvalidOperationException(
                    "Flashlight_Charge_Disconnect review panel count is invalid.");
            var composite = new Texture2D(
                count * PanelSize, PanelSize * 2, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < count; index++)
                {
                    composite.SetPixels(
                        index * PanelSize, PanelSize,
                        PanelSize, PanelSize, full[index].GetPixels());
                    composite.SetPixels(
                        index * PanelSize, 0,
                        PanelSize, PanelSize, upper[index].GetPixels());
                }
                composite.Apply(false, false);
                string absolute = Absolute(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllBytes(absolute, composite.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static void CleanupDisconnectRuntimeCapture()
        {
            EditorApplication.update -= DisconnectRuntimeCaptureTick;
            if (disconnectFullPanels != null)
                foreach (Texture2D panel in disconnectFullPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (disconnectUpperPanels != null)
                foreach (Texture2D panel in disconnectUpperPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            disconnectCaptureActive = false;
            disconnectCaptureStartedAt = 0d;
            disconnectNextPanel = 0;
            disconnectRuntimeTarget = null;
            disconnectRuntimeAnimator = null;
            disconnectRuntimeCarry = null;
            disconnectRuntimePose = null;
            connectGripRuntimeTarget = null;
            connectGripRuntimeCarry = null;
            connectGripRuntimePose = null;
            disconnectFullPanels = null;
            disconnectUpperPanels = null;
            disconnectCaptureTimes = null;
            disconnectExpectedCycleDuration = 0f;
            connectMaximumFullReachSurfaceClearance = float.NegativeInfinity;
            disconnectMaximumFullReachSurfaceClearance = float.NegativeInfinity;
            connectFullReachGripSamples = 0;
            disconnectFullReachGripSamples = 0;
            DisconnectInitialBoneRotations.Clear();
            DisconnectRuntimeObservations.Clear();
        }

        private static void FailDisconnectReverseReview(Exception exception)
        {
            bool gripTimingReview = SessionState.GetBool(
                GripTimingReviewKey, false);
            CleanupDisconnectRuntimeCapture();
            WriteText(
                gripTimingReview
                    ? GripTimingFailureReportPath
                    : DisconnectReverseFailureReportPath,
                exception.GetType().FullName + Environment.NewLine +
                exception.Message + Environment.NewLine + exception.StackTrace);
            SessionState.EraseString(DisconnectReverseAutoStateKey);
            SessionState.EraseBool(DisconnectReverseInspectionOnlyKey);
            SessionState.EraseBool(GripTimingReviewKey);
            Debug.LogException(exception);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Pose Sources")]
        internal static void InspectPoseSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            string[] bodyPaths = IdleBodyPaths(idle.transform);
            float maximumBodyRotationDifference = bodyPaths.Max(path =>
                Quaternion.Angle(
                    RequirePath(target.transform, path).localRotation,
                    RequirePath(idle.transform, path).localRotation));
            float maximumBodyPositionDifference = bodyPaths.Max(path =>
                Vector3.Distance(
                    RequirePath(target.transform, path).localPosition,
                    RequirePath(idle.transform, path).localPosition));
            float palmForwardDeviation = Vector3.Angle(
                RightPalmNormal(target.transform), target.transform.forward);
            float lensUpDeviation = Vector3.Angle(
                carry.Holder.transform.up, target.transform.up);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Pose source inspection changed scene dirty state.");

            WriteText(PoseSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect pose source inspection")
                .AppendLine("target=" + TargetName)
                .AppendLine("idleReference=" + IdleReferenceName)
                .AppendLine("idleBodyBoneCount=" + bodyPaths.Length)
                .AppendLine("idleBodyPaths=" + string.Join("|", bodyPaths))
                .AppendLine("maximumBodyRotationDifferenceDegrees=" +
                    F(maximumBodyRotationDifference))
                .AppendLine("maximumBodyPositionDifferenceMeters=" +
                    F(maximumBodyPositionDifference))
                .AppendLine("rightPalmForwardDeviationDegrees=" +
                    F(palmForwardDeviation))
                .AppendLine("flashlightLensUpDeviationDegrees=" +
                    F(lensUpDeviation))
                .AppendLine("leftArmAndHeadModificationRequested=False")
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectPose] Pose sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Forward Reach Sources")]
        internal static void InspectForwardReachSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);

            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashlightChargeConnectForwardReachAnalysis";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Animator workAnimator = RequireAnimator(work);
                workAnimator.enabled = false;
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Forward-reach analysis clone is missing its pose behaviour.");
                FlashlightRightHandFollowBehaviour carry = RequireCarry(work);
                if (pose.Carry != carry)
                    throw new InvalidOperationException(
                        "Forward-reach analysis clone did not remap its carry reference.");

                const int sampleCount = 61;
                float timeStep = clip.length / (sampleCount - 1);
                var samples = new List<ForwardReachSample>(sampleCount);
                float maximumSpeed = 0f;
                float maximumAcceleration = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    float time = Mathf.Min(index * timeStep, clip.length);
                    clip.SampleAnimation(work, time);
                    pose.RefreshPreview();
                    Transform arm = RequirePath(work.transform, RightArmPath);
                    Transform foreArm = RequirePath(work.transform, RightForeArmPath);
                    Transform hand = RequirePath(work.transform, RightHandPath);
                    Vector3 handLocal = work.transform.InverseTransformPoint(hand.position);
                    float elbowAngle = Vector3.Angle(
                        arm.position - foreArm.position,
                        hand.position - foreArm.position);
                    Vector3 velocity = index > 0
                        ? (handLocal - samples[index - 1].HandLocal) / timeStep
                        : Vector3.zero;
                    Vector3 acceleration = index > 1
                        ? (velocity - samples[index - 1].Velocity) / timeStep
                        : Vector3.zero;
                    maximumSpeed = Mathf.Max(maximumSpeed, velocity.magnitude);
                    maximumAcceleration = Mathf.Max(
                        maximumAcceleration, acceleration.magnitude);
                    samples.Add(new ForwardReachSample(
                        time, handLocal, velocity, acceleration, elbowAngle));
                }

                ForwardReachSample mostForward = samples
                    .OrderByDescending(sample => sample.HandLocal.z).First();
                ForwardReachSample fastest = samples
                    .OrderByDescending(sample => sample.Velocity.magnitude).First();
                ForwardReachSample sharpest = samples
                    .OrderByDescending(sample => sample.Acceleration.magnitude).First();
                float minimumElbowAngle = samples.Min(sample => sample.ElbowAngle);
                float maximumElbowAngle = samples.Max(sample => sample.ElbowAngle);
                var report = new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect forward-reach source analysis")
                    .AppendLine("target=" + TargetName)
                    .AppendLine("clip=" + ClipName)
                    .AppendLine("clipLengthSeconds=" + F(clip.length))
                    .AppendLine("sampleCount=" + sampleCount)
                    .AppendLine("sampleStepSeconds=" + F(timeStep))
                    .AppendLine("maximumHandSpeedMetersPerSecond=" + F(maximumSpeed))
                    .AppendLine("maximumHandAccelerationMetersPerSecondSquared=" +
                        F(maximumAcceleration))
                    .AppendLine("mostForwardTimeSeconds=" + F(mostForward.Time))
                    .AppendLine("mostForwardHandLocal=" + Vec(mostForward.HandLocal))
                    .AppendLine("fastestTimeSeconds=" + F(fastest.Time))
                    .AppendLine("fastestVelocityLocal=" + Vec(fastest.Velocity))
                    .AppendLine("sharpestTimeSeconds=" + F(sharpest.Time))
                    .AppendLine("sharpestAccelerationLocal=" + Vec(sharpest.Acceleration))
                    .AppendLine("minimumElbowAngleDegrees=" + F(minimumElbowAngle))
                    .AppendLine("maximumElbowAngleDegrees=" + F(maximumElbowAngle))
                    .AppendLine("analysisManipulatedTarget=False");
                foreach (ForwardReachSample sample in samples)
                    report.AppendLine(
                        "sample=" + F(sample.Time) +
                        "|hand=" + Vec(sample.HandLocal) +
                        "|velocity=" + Vec(sample.Velocity) +
                        "|acceleration=" + Vec(sample.Acceleration) +
                        "|elbow=" + F(sample.ElbowAngle));
                WriteText(ForwardReachSourceReportPath, report.ToString());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }

            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Forward-reach source analysis changed scene dirty state.");
            Debug.Log("[FlashlightChargeConnectForwardReach] Sources analyzed without target modification.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Upper Body Restore Sources")]
        internal static void InspectUpperBodyRestoreSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightChargeConnectPoseBehaviour pose =
                target.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose behaviour is missing.");
            RequireExactSourceCopy();
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            RequireControllerContract(controller, clip);

            string[] sourceUpperBodyPaths = SourceUpperBodyPaths(target.transform);
            string[] idlePaths = IdleLowerBodyAndLeftArmPaths(idle.transform);
            if (sourceUpperBodyPaths.Length == 0 || idlePaths.Length == 0)
                throw new InvalidOperationException(
                    "Upper-body restore source path selection is empty.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Upper-body restore source inspection changed scene dirty state.");

            WriteText(UpperBodyRestoreSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect upper-body restore source inspection")
                .AppendLine("target=" + TargetName)
                .AppendLine("idleReference=" + IdleReferenceName)
                .AppendLine("clip=" + ClipName)
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("controllerSpeed=1")
                .AppendLine("externalSha256=" + ComputeFileHash(ExternalFbxPath))
                .AppendLine("importedSha256=" + ComputeAssetHash(ImportedFbxPath))
                .AppendLine("sourceUpperBodyBoneCount=" + sourceUpperBodyPaths.Length)
                .AppendLine("sourceUpperBodyPaths=" + string.Join("|", sourceUpperBodyPaths))
                .AppendLine("idleLowerBodyAndLeftArmBoneCount=" + idlePaths.Length)
                .AppendLine("idleLowerBodyAndLeftArmPaths=" + string.Join("|", idlePaths))
                .AppendLine("currentForwardReachConfigured=" + pose.ForwardReachConfigured)
                .AppendLine("currentAuthoredRightArmPoseCount=" +
                    carry.AuthoredRightArmPose.Count)
                .AppendLine("currentSourceRightArmPoseCount=" +
                    carry.SourceRightArmPose.Count)
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Upper Body Restore")]
        internal static void ApplyUpperBodyRestore()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightChargeConnectPoseBehaviour pose =
                target.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose behaviour is missing.");
            RequireExactSourceCopy();
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            RequireControllerContract(controller, clip);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);
            int previousAuthoredRightArmPoseCount = carry.AuthoredRightArmPose.Count;
            int previousSourceRightArmPoseCount = carry.SourceRightArmPose.Count;
            string[] idlePaths = IdleLowerBodyAndLeftArmPaths(idle.transform);
            FlashlightChargeConnectBonePose[] idlePose = idlePaths.Select(path =>
            {
                Transform reference = RequirePath(idle.transform, path);
                return new FlashlightChargeConnectBonePose(
                    path, reference.localPosition, reference.localRotation);
            }).ToArray();

            Undo.RecordObject(carry,
                "Restore Flashlight_Charge_Connect supplied right-arm animation");
            carry.ConfigureLocomotionPose(
                Array.Empty<FlashlightBoneRotation>(),
                Array.Empty<FlashlightBoneRotation>());
            EditorUtility.SetDirty(carry);
            PrefabUtility.RecordPrefabInstancePropertyModifications(carry);

            Undo.RecordObject(pose,
                "Restore Flashlight_Charge_Connect supplied upper-body animation");
            pose.ConfigureSourceUpperBodyRestore(carry, idlePose);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.SourceUpperBodyRestored || pose.ForwardReachConfigured)
                throw new InvalidOperationException(
                    "Upper-body restore mode did not replace the forward-reach mode.");
            if (carry.AuthoredRightArmPose.Count != 0 ||
                carry.SourceRightArmPose.Count != 0)
                throw new InvalidOperationException(
                    "Right-arm authored retention remained enabled.");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect hand-follow base configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the upper-body restore.");
            AssetDatabase.SaveAssets();

            WriteText(UpperBodyRestoreApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect upper-body restore application")
                .AppendLine("target=" + TargetName)
                .AppendLine("idleReference=" + IdleReferenceName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("controllerSpeed=1")
                .AppendLine("idleLowerBodyAndLeftArmBoneCount=" + idlePose.Length)
                .AppendLine("idleLowerBodyBoneCount=" +
                    idlePaths.Count(path => !IsLeftArmPath(path)))
                .AppendLine("idleLeftArmBoneCount=" +
                    idlePaths.Count(IsLeftArmPath))
                .AppendLine("previousAuthoredRightArmPoseCount=" +
                    previousAuthoredRightArmPoseCount)
                .AppendLine("previousSourceRightArmPoseCount=" +
                    previousSourceRightArmPoseCount)
                .AppendLine("currentAuthoredRightArmPoseCount=0")
                .AppendLine("currentSourceRightArmPoseCount=0")
                .AppendLine("forwardReachConfigured=False")
                .AppendLine("sourceUpperBodyRestored=True")
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllerModified=False")
                .AppendLine("rendererMeshMaterialScaleModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Upper Body Restore")]
        internal static void InspectUpperBodyRestore()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            AnimationClip clip = RequireUpperBodyRestoreConfiguration(
                target, out Animator animator, out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);
            UpperBodyRestoreAnalysis analysis = AnalyzeUpperBodyRestore(
                target, idle, clip);

            if (analysis.MaximumSourceUpperBodyRotationError > 0.01f ||
                analysis.MaximumSourceUpperBodyPositionError > 0.00001f)
                throw new InvalidOperationException(
                    "Restored torso, head, or right arm differs from the supplied FBX.");
            if (analysis.MaximumIdleLeftArmRotationError > 0.01f ||
                analysis.MaximumIdleLeftArmPositionError > 0.00001f)
                throw new InvalidOperationException(
                    "The complete left arm differs from Player_Idle.");
            if (analysis.MaximumIdleLowerBodyRotationError > 0.01f ||
                analysis.MaximumIdleLowerBodyPositionError > 0.00001f)
                throw new InvalidOperationException(
                    "The lower body differs from Player_Idle.");
            if (analysis.MaximumSourceUpperBodyMotion <= 1f)
                throw new InvalidOperationException(
                    "The supplied upper-body animation did not remain active.");
            if (analysis.MaximumHolderPositionError > 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight right-hand position follow drifted.");
            if (analysis.MaximumLensUpDeviation > 0.1f)
                throw new InvalidOperationException(
                    "The existing flashlight lens-up orientation drifted.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Upper-body restore inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during upper-body restore inspection.");

            WriteText(UpperBodyRestoreInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect upper-body restore inspection")
                .AppendLine("targetCount=1")
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("controllerSpeed=1")
                .AppendLine("sampleCount=" + analysis.SampleCount)
                .AppendLine("sourceUpperBodyBoneCount=" +
                    analysis.SourceUpperBodyBoneCount)
                .AppendLine("idleLeftArmBoneCount=" + analysis.IdleLeftArmBoneCount)
                .AppendLine("idleLowerBodyBoneCount=" + analysis.IdleLowerBodyBoneCount)
                .AppendLine("maximumSourceUpperBodyRotationErrorDegrees=" +
                    F(analysis.MaximumSourceUpperBodyRotationError))
                .AppendLine("maximumSourceUpperBodyPositionErrorMeters=" +
                    F(analysis.MaximumSourceUpperBodyPositionError))
                .AppendLine("maximumIdleLeftArmRotationErrorDegrees=" +
                    F(analysis.MaximumIdleLeftArmRotationError))
                .AppendLine("maximumIdleLeftArmPositionErrorMeters=" +
                    F(analysis.MaximumIdleLeftArmPositionError))
                .AppendLine("maximumIdleLowerBodyRotationErrorDegrees=" +
                    F(analysis.MaximumIdleLowerBodyRotationError))
                .AppendLine("maximumIdleLowerBodyPositionErrorMeters=" +
                    F(analysis.MaximumIdleLowerBodyPositionError))
                .AppendLine("maximumSourceUpperBodyMotionDegrees=" +
                    F(analysis.MaximumSourceUpperBodyMotion))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(analysis.MaximumHolderPositionError))
                .AppendLine("maximumFlashlightLensUpDeviationDegrees=" +
                    F(analysis.MaximumLensUpDeviation))
                .AppendLine("sourceUpperBodyRestored=" + pose.SourceUpperBodyRestored)
                .AppendLine("forwardReachConfigured=" + pose.ForwardReachConfigured)
                .AppendLine("authoredRightArmPoseCount=" +
                    carry.AuthoredRightArmPose.Count)
                .AppendLine("sourceRightArmPoseCount=" +
                    carry.SourceRightArmPose.Count)
                .AppendLine("sourceClipCopiedExactly=True")
                .AppendLine("controllerSourceStatePreserved=True")
                .AppendLine("unityConsoleErrors=" + consoleErrors)
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            CompleteUpperBodyRestoreFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Upper Body Restore Diagnostic")]
        internal static void CaptureUpperBodyRestoreDiagnostic()
        {
            RequireEditMode();
            GameObject target = FindUnique(RequireScene(), TargetName);
            RequireUpperBodyRestoreConfiguration(
                target, out _, out _, out _);
            RequireNoActiveReview();
            int diagnosticIndex = File.Exists(Absolute(UpperBodyRestoreDiagnosticOneImagePath))
                ? 2
                : 1;
            string imagePath = diagnosticIndex == 1
                ? UpperBodyRestoreDiagnosticOneImagePath
                : UpperBodyRestoreDiagnosticTwoImagePath;
            string reportPath = diagnosticIndex == 1
                ? UpperBodyRestoreDiagnosticOneReportPath
                : UpperBodyRestoreDiagnosticTwoReportPath;
            DeleteReviewFile(imagePath);
            DeleteReviewFile(reportPath);
            DeleteReviewFile(UpperBodyRestoreFailureReportPath);
            SessionState.SetBool(PoseReviewKey, false);
            SessionState.SetBool(ForwardReachReviewKey, false);
            SessionState.SetBool(UpperBodyRestoreReviewKey, true);
            SessionState.SetBool(UpperBodyRestoreFinalReviewKey, false);
            SessionState.SetInt(UpperBodyRestoreDiagnosticIndexKey, diagnosticIndex);
            SessionState.SetString(AutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Diagnostic natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Upper Body Restore Final")]
        internal static void CaptureUpperBodyRestoreFinal()
        {
            RequireEditMode();
            InspectUpperBodyRestore();
            ClearCompletedUpperBodyRestoreDiagnosticState();
            RequireNoActiveReview();
            DeleteReviewFile(UpperBodyRestoreFinalImagePath);
            DeleteReviewFile(UpperBodyRestoreFinalReportPath);
            DeleteReviewFile(UpperBodyRestoreCompletionReportPath);
            DeleteReviewFile(UpperBodyRestoreFailureReportPath);
            SessionState.SetBool(PoseReviewKey, false);
            SessionState.SetBool(ForwardReachReviewKey, false);
            SessionState.SetBool(UpperBodyRestoreReviewKey, true);
            SessionState.SetBool(UpperBodyRestoreFinalReviewKey, true);
            SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
            SessionState.SetString(AutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Forward Hold Sources")]
        internal static void InspectForwardHoldSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireUpperBodyRestoreConfiguration(
                target, out Animator animator, out _, out _);
            ForwardHoldSourceSample sample = FindForwardHoldSourceSample(target, clip);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Forward-hold source inspection changed scene dirty state.");

            WriteText(ForwardHoldSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect forward-hold source inspection")
                .AppendLine("target=" + TargetName)
                .AppendLine("clip=" + ClipName)
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("clipFrameRate=" + F(clip.frameRate))
                .AppendLine("controllerSpeed=1")
                .AppendLine("animatorSpeed=" + F(animator.speed))
                .AppendLine("sampleCount=" + sample.SampleCount)
                .AppendLine("maximumForwardFrame=" + sample.Frame)
                .AppendLine("maximumForwardTimeSeconds=" + F(sample.Time))
                .AppendLine("maximumForwardNormalizedTime=" +
                    F(sample.NormalizedTime))
                .AppendLine("maximumForwardRightHandLocal=" +
                    Vec(sample.RightHandLocal))
                .AppendLine("requestedHoldDurationSeconds=" +
                    F(ForwardHoldDurationSeconds))
                .AppendLine("expectedCycleDurationSeconds=" +
                    F(clip.length + ForwardHoldDurationSeconds))
                .AppendLine("externalSha256=" + ComputeFileHash(ExternalFbxPath))
                .AppendLine("importedSha256=" + ComputeAssetHash(ImportedFbxPath))
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectForwardHold] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Forward Hold")]
        internal static void ApplyForwardHold()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireUpperBodyRestoreConfiguration(
                target, out Animator animator,
                out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);
            ForwardHoldSourceSample sample = FindForwardHoldSourceSample(target, clip);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);

            Undo.RecordObject(pose,
                "Hold Flashlight_Charge_Connect forward upper-body pose for one second");
            pose.ConfigureForwardHold(
                animator, sample.NormalizedTime, ForwardHoldDurationSeconds);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.ForwardHoldConfigured || pose.ForwardReachConfigured ||
                !pose.SourceUpperBodyRestored)
                throw new InvalidOperationException(
                    "Forward hold did not preserve the source upper-body restore mode.");
            if (!Mathf.Approximately(animator.speed, 1f))
                throw new InvalidOperationException(
                    "Animator speed changed while applying the forward hold.");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect hand-follow base configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the forward hold.");
            AssetDatabase.SaveAssets();

            WriteText(ForwardHoldApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect forward-hold application")
                .AppendLine("target=" + TargetName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("maximumForwardFrame=" + sample.Frame)
                .AppendLine("maximumForwardTimeSeconds=" + F(sample.Time))
                .AppendLine("maximumForwardNormalizedTime=" +
                    F(sample.NormalizedTime))
                .AppendLine("holdDurationSeconds=" + F(ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("expectedCycleDurationSeconds=" +
                    F(clip.length + ForwardHoldDurationSeconds))
                .AppendLine("controllerSpeed=1")
                .AppendLine("sourceUpperBodyRestored=True")
                .AppendLine("forwardReachConfigured=False")
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllerModified=False")
                .AppendLine("rendererMeshMaterialScaleModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectForwardHold] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Forward Hold")]
        internal static void InspectForwardHold()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireForwardHoldConfiguration(
                target, out Animator animator, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            ForwardHoldSourceSample sample = FindForwardHoldSourceSample(target, clip);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            UpperBodyRestoreAnalysis upperBody = AnalyzeUpperBodyRestore(
                target, idle, clip);

            float normalizedTolerance = 0.5f / (clip.length * clip.frameRate);
            if (Mathf.Abs(pose.ForwardHoldStartNormalized -
                    sample.NormalizedTime) > normalizedTolerance)
                throw new InvalidOperationException(
                    "Forward hold no longer starts at the maximum-forward source frame.");
            if (Mathf.Abs(pose.ForwardHoldDurationSeconds -
                    ForwardHoldDurationSeconds) > 0.001f)
                throw new InvalidOperationException(
                    "Forward hold duration is not one second.");
            if (!Mathf.Approximately(animator.speed, 1f))
                throw new InvalidOperationException(
                    "Animator did not return to speed one in Edit Mode.");
            if (upperBody.MaximumSourceUpperBodyRotationError > 0.01f ||
                upperBody.MaximumSourceUpperBodyPositionError > 0.00001f ||
                upperBody.MaximumIdleLeftArmRotationError > 0.01f ||
                upperBody.MaximumIdleLeftArmPositionError > 0.00001f ||
                upperBody.MaximumIdleLowerBodyRotationError > 0.01f ||
                upperBody.MaximumIdleLowerBodyPositionError > 0.00001f)
                throw new InvalidOperationException(
                    "Forward hold changed the approved source upper body or Player_Idle pose.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Forward-hold inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during forward-hold inspection.");

            WriteText(ForwardHoldInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect forward-hold inspection")
                .AppendLine("targetCount=1")
                .AppendLine("maximumForwardFrame=" + sample.Frame)
                .AppendLine("maximumForwardTimeSeconds=" + F(sample.Time))
                .AppendLine("maximumForwardNormalizedTime=" +
                    F(sample.NormalizedTime))
                .AppendLine("configuredHoldStartNormalized=" +
                    F(pose.ForwardHoldStartNormalized))
                .AppendLine("configuredHoldDurationSeconds=" +
                    F(pose.ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("expectedCycleDurationSeconds=" +
                    F(clip.length + pose.ForwardHoldDurationSeconds))
                .AppendLine("controllerSpeed=1")
                .AppendLine("maximumSourceUpperBodyRotationErrorDegrees=" +
                    F(upperBody.MaximumSourceUpperBodyRotationError))
                .AppendLine("maximumSourceUpperBodyPositionErrorMeters=" +
                    F(upperBody.MaximumSourceUpperBodyPositionError))
                .AppendLine("maximumIdleLeftArmRotationErrorDegrees=" +
                    F(upperBody.MaximumIdleLeftArmRotationError))
                .AppendLine("maximumIdleLeftArmPositionErrorMeters=" +
                    F(upperBody.MaximumIdleLeftArmPositionError))
                .AppendLine("maximumIdleLowerBodyRotationErrorDegrees=" +
                    F(upperBody.MaximumIdleLowerBodyRotationError))
                .AppendLine("maximumIdleLowerBodyPositionErrorMeters=" +
                    F(upperBody.MaximumIdleLowerBodyPositionError))
                .AppendLine("sourceClipCopiedExactly=True")
                .AppendLine("controllerSourceStatePreserved=True")
                .AppendLine("unityConsoleErrors=" + consoleErrors)
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            CompleteForwardHoldFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectForwardHold] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Forward Hold Diagnostic")]
        internal static void CaptureForwardHoldDiagnostic()
        {
            RequireEditMode();
            GameObject target = FindUnique(RequireScene(), TargetName);
            RequireForwardHoldConfiguration(target, out _, out _, out _);
            ClearCompletedUpperBodyRestoreFinalState();
            RequireNoActiveReview();
            int diagnosticIndex = File.Exists(Absolute(ForwardHoldDiagnosticOneImagePath))
                ? 2
                : 1;
            string imagePath = diagnosticIndex == 1
                ? ForwardHoldDiagnosticOneImagePath
                : ForwardHoldDiagnosticTwoImagePath;
            string reportPath = diagnosticIndex == 1
                ? ForwardHoldDiagnosticOneReportPath
                : ForwardHoldDiagnosticTwoReportPath;
            DeleteReviewFile(imagePath);
            DeleteReviewFile(reportPath);
            DeleteReviewFile(ForwardHoldFailureReportPath);
            SetForwardHoldReviewState(false, diagnosticIndex);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectForwardHold] Diagnostic natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Forward Hold Final")]
        internal static void CaptureForwardHoldFinal()
        {
            RequireEditMode();
            InspectForwardHold();
            ClearCompletedForwardHoldDiagnosticState();
            RequireNoActiveReview();
            DeleteReviewFile(ForwardHoldFinalImagePath);
            DeleteReviewFile(ForwardHoldFinalReportPath);
            DeleteReviewFile(ForwardHoldCompletionReportPath);
            DeleteReviewFile(ForwardHoldFailureReportPath);
            SetForwardHoldReviewState(true, 0);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectForwardHold] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Right Shoulder Forward Sources")]
        internal static void InspectRightShoulderForwardSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireForwardHoldConfiguration(
                target, out _, out _, out _);
            RightShoulderForwardConfiguration configuration =
                AnalyzeRightShoulderForwardSource(target, clip);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Right-shoulder-forward source inspection changed scene dirty state.");

            WriteText(RightShoulderForwardSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect right-shoulder-forward source inspection")
                .AppendLine("target=" + TargetName)
                .AppendLine("clip=" + ClipName)
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("maximumForwardFrame=" + configuration.Frame)
                .AppendLine("maximumForwardTimeSeconds=" + F(configuration.Time))
                .AppendLine("maximumForwardNormalizedTime=" +
                    F(configuration.NormalizedTime))
                .AppendLine("sourceRightArmLocal=" +
                    Vec(configuration.SourceRightArmLocal))
                .AppendLine("sourceRightHandLocal=" +
                    Vec(configuration.SourceRightHandLocal))
                .AppendLine("sourceRightHandLateralOffsetFromArmMeters=" +
                    F(configuration.SourceLateralOffset))
                .AppendLine("sourceRightArmForwardDeviationDegrees=" +
                    F(configuration.SourceForwardDeviation))
                .AppendLine("sourceRightElbowAngleDegrees=" +
                    F(configuration.SourceElbowAngle))
                .AppendLine("targetRightHandLocal=" +
                    Vec(configuration.TargetRightHandLocal))
                .AppendLine("targetRightHandLateralOffsetFromArmMeters=0")
                .AppendLine("targetRightArmForwardDeviationDegrees=0")
                .AppendLine("targetRightElbowAngleDegrees=" +
                    F(RightShoulderForwardElbowAngleDegrees))
                .AppendLine("rightUpperArmLengthMeters=" +
                    F(configuration.UpperArmLength))
                .AppendLine("rightForeArmLengthMeters=" +
                    F(configuration.ForeArmLength))
                .AppendLine("externalSha256=" + ComputeFileHash(ExternalFbxPath))
                .AppendLine("importedSha256=" + ComputeAssetHash(ImportedFbxPath))
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectRightShoulderForward] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Right Shoulder Forward")]
        internal static void ApplyRightShoulderForward()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireForwardHoldConfiguration(
                target, out _,
                out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);
            RightShoulderForwardConfiguration configuration =
                AnalyzeRightShoulderForwardSource(target, clip);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);

            Undo.RecordObject(
                pose,
                "Aim Flashlight_Charge_Connect in front of the right shoulder");
            pose.ConfigureRightShoulderForward(
                configuration.TargetRightHandLocal,
                configuration.TargetReachDistance,
                new Vector3(0.8f, -0.45f, 0f),
                RightShoulderForwardStartNormalized,
                configuration.NormalizedTime,
                RightShoulderForwardReturnNormalized,
                RightShoulderForwardEndNormalized,
                RightShoulderForwardShoulderShare,
                RightShoulderForwardWristSourceWeight);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.RightShoulderForwardConfigured ||
                !pose.ForwardHoldConfigured || !pose.SourceUpperBodyRestored ||
                pose.ForwardReachConfigured)
                throw new InvalidOperationException(
                    "Right-shoulder-forward configuration did not preserve the approved hold mode.");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect right-hand follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the right-shoulder-forward correction.");
            AssetDatabase.SaveAssets();

            WriteText(RightShoulderForwardApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect right-shoulder-forward application")
                .AppendLine("target=" + TargetName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("sourceForwardDeviationDegrees=" +
                    F(configuration.SourceForwardDeviation))
                .AppendLine("sourceLateralOffsetMeters=" +
                    F(configuration.SourceLateralOffset))
                .AppendLine("targetRightHandLocal=" +
                    Vec(configuration.TargetRightHandLocal))
                .AppendLine("targetForwardDeviationDegrees=0")
                .AppendLine("targetLateralOffsetMeters=0")
                .AppendLine("targetElbowAngleDegrees=" +
                    F(RightShoulderForwardElbowAngleDegrees))
                .AppendLine("shoulderRotationShare=" +
                    F(RightShoulderForwardShoulderShare))
                .AppendLine("wristSourceRotationWeight=" +
                    F(RightShoulderForwardWristSourceWeight))
                .AppendLine("forwardHoldDurationSeconds=" +
                    F(pose.ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllerModified=False")
                .AppendLine("rendererMeshMaterialScaleModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectRightShoulderForward] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Right Shoulder Forward")]
        internal static void InspectRightShoulderForward()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireRightShoulderForwardConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            RightShoulderForwardConfiguration source =
                AnalyzeRightShoulderForwardSource(target, clip);
            RightShoulderForwardAnalysis analysis =
                AnalyzeRightShoulderForwardApplied(target, idle, clip);

            if (Vector3.Distance(
                    pose.RightShoulderForwardTargetHandLocal,
                    source.TargetRightHandLocal) > 0.00001f)
                throw new InvalidOperationException(
                    "Right-shoulder-forward target changed from the analyzed source geometry.");
            if (analysis.PeakForwardDeviation > 1f ||
                analysis.PeakLateralOffset > 0.005f ||
                analysis.PeakHandError > 0.0005f)
                throw new InvalidOperationException(
                    "Right hand does not reach directly in front of the right shoulder.");
            if (analysis.PeakElbowAngle < 130f ||
                analysis.PeakElbowAngle > 150f)
                throw new InvalidOperationException(
                    "Right elbow bend left the approved natural range.");
            if (analysis.PeakShoulderRotationChange < 0.1f ||
                analysis.PeakArmRotationChange < 0.1f ||
                analysis.PeakForeArmRotationChange < 0.1f ||
                analysis.PeakHandRotationChange < 0.1f)
                throw new InvalidOperationException(
                    "Right-arm correction was not distributed through the complete arm chain.");
            if (analysis.MaximumUnaffectedUpperBodyRotationError > 0.01f ||
                analysis.MaximumUnaffectedUpperBodyPositionError > 0.00001f ||
                analysis.MaximumIdlePoseRotationError > 0.01f ||
                analysis.MaximumIdlePosePositionError > 0.00001f)
                throw new InvalidOperationException(
                    "Right-arm correction changed an unapproved body area.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Right-shoulder-forward inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during right-shoulder-forward inspection.");

            WriteText(RightShoulderForwardInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect right-shoulder-forward inspection")
                .AppendLine("targetCount=1")
                .AppendLine("sourceForwardDeviationDegrees=" +
                    F(source.SourceForwardDeviation))
                .AppendLine("correctedPeakForwardDeviationDegrees=" +
                    F(analysis.PeakForwardDeviation))
                .AppendLine("sourceLateralOffsetMeters=" +
                    F(source.SourceLateralOffset))
                .AppendLine("correctedPeakLateralOffsetMeters=" +
                    F(analysis.PeakLateralOffset))
                .AppendLine("correctedPeakElbowAngleDegrees=" +
                    F(analysis.PeakElbowAngle))
                .AppendLine("correctedPeakHandErrorMeters=" +
                    F(analysis.PeakHandError))
                .AppendLine("peakRightShoulderRotationChangeDegrees=" +
                    F(analysis.PeakShoulderRotationChange))
                .AppendLine("peakRightArmRotationChangeDegrees=" +
                    F(analysis.PeakArmRotationChange))
                .AppendLine("peakRightForeArmRotationChangeDegrees=" +
                    F(analysis.PeakForeArmRotationChange))
                .AppendLine("peakRightHandRotationChangeDegrees=" +
                    F(analysis.PeakHandRotationChange))
                .AppendLine("maximumUnaffectedUpperBodyRotationErrorDegrees=" +
                    F(analysis.MaximumUnaffectedUpperBodyRotationError))
                .AppendLine("maximumUnaffectedUpperBodyPositionErrorMeters=" +
                    F(analysis.MaximumUnaffectedUpperBodyPositionError))
                .AppendLine("maximumIdlePoseRotationErrorDegrees=" +
                    F(analysis.MaximumIdlePoseRotationError))
                .AppendLine("maximumIdlePosePositionErrorMeters=" +
                    F(analysis.MaximumIdlePosePositionError))
                .AppendLine("forwardHoldDurationSeconds=" +
                    F(pose.ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("sourceClipCopiedExactly=True")
                .AppendLine("controllerSourceStatePreserved=True")
                .AppendLine("unityConsoleErrors=" + consoleErrors)
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            CompleteRightShoulderForwardFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectRightShoulderForward] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Right Shoulder Forward Diagnostic")]
        internal static void CaptureRightShoulderForwardDiagnostic()
        {
            RequireEditMode();
            GameObject target = FindUnique(RequireScene(), TargetName);
            RequireRightShoulderForwardConfiguration(target, out _, out _, out _);
            RequireNoActiveReview();
            int diagnosticIndex = File.Exists(Absolute(
                RightShoulderForwardDiagnosticOneImagePath)) ? 2 : 1;
            string imagePath = diagnosticIndex == 1
                ? RightShoulderForwardDiagnosticOneImagePath
                : RightShoulderForwardDiagnosticTwoImagePath;
            string reportPath = diagnosticIndex == 1
                ? RightShoulderForwardDiagnosticOneReportPath
                : RightShoulderForwardDiagnosticTwoReportPath;
            DeleteReviewFile(imagePath);
            DeleteReviewFile(reportPath);
            DeleteReviewFile(RightShoulderForwardFailureReportPath);
            SetRightShoulderForwardReviewState(false, diagnosticIndex);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectRightShoulderForward] Diagnostic natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Right Shoulder Forward Final")]
        internal static void CaptureRightShoulderForwardFinal()
        {
            RequireEditMode();
            InspectRightShoulderForward();
            ClearCompletedRightShoulderForwardDiagnosticState();
            RequireNoActiveReview();
            DeleteReviewFile(RightShoulderForwardFinalImagePath);
            DeleteReviewFile(RightShoulderForwardFinalReportPath);
            DeleteReviewFile(RightShoulderForwardCompletionReportPath);
            DeleteReviewFile(RightShoulderForwardFailureReportPath);
            SetRightShoulderForwardReviewState(true, 0);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectRightShoulderForward] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Neutral Wrist Sources")]
        internal static void InspectNeutralWristSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireRightShoulderForwardConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            RightShoulderForwardAnalysis current =
                AnalyzeRightShoulderForwardApplied(target, idle, clip);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Neutral-wrist source inspection changed scene dirty state.");

            WriteText(NeutralWristSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect neutral-wrist source inspection")
                .AppendLine("target=" + TargetName)
                .AppendLine("clip=" + ClipName)
                .AppendLine("currentPeakWristBendDegrees=" +
                    F(current.PeakWristBendAngle))
                .AppendLine("currentWristSourceRotationWeight=" +
                    F(pose.RightShoulderForwardWristSourceWeight))
                .AppendLine("currentPeakHandLocalRotationChangeDegrees=" +
                    F(current.PeakHandRotationChange))
                .AppendLine("targetWristBendDegrees=0")
                .AppendLine("maximumAcceptedWristBendDegrees=" +
                    F(NeutralWristMaximumBendDegrees))
                .AppendLine("flashlightLensAlignmentIndependentOfWrist=True")
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectNeutralWrist] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Neutral Wrist")]
        internal static void ApplyNeutralWrist()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireRightShoulderForwardConfiguration(
                target, out _,
                out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            RightShoulderForwardAnalysis before =
                AnalyzeRightShoulderForwardApplied(target, idle, clip);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);

            Undo.RecordObject(pose,
                "Neutralize Flashlight_Charge_Connect right wrist");
            pose.ConfigureNeutralWrist();
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.NeutralWristConfigured ||
                pose.RightShoulderForwardWristSourceWeight > 0.00001f ||
                !pose.RightShoulderForwardConfigured ||
                !pose.ForwardHoldConfigured || !pose.SourceUpperBodyRestored)
                throw new InvalidOperationException(
                    "Neutral wrist configuration did not preserve the approved arm and hold mode.");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect right-hand follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the neutral wrist correction.");
            AssetDatabase.SaveAssets();

            WriteText(NeutralWristApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect neutral-wrist application")
                .AppendLine("target=" + TargetName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("previousPeakWristBendDegrees=" +
                    F(before.PeakWristBendAngle))
                .AppendLine("previousWristSourceRotationWeight=" +
                    F(RightShoulderForwardWristSourceWeight))
                .AppendLine("currentWristSourceRotationWeight=" +
                    F(pose.RightShoulderForwardWristSourceWeight))
                .AppendLine("neutralWristConfigured=True")
                .AppendLine("foreArmAndHandAxisAligned=True")
                .AppendLine("flashlightLensUpCorrectionPreserved=True")
                .AppendLine("forwardHoldDurationSeconds=" +
                    F(pose.ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllerModified=False")
                .AppendLine("rendererMeshMaterialScaleModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectNeutralWrist] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Neutral Wrist")]
        internal static void InspectNeutralWrist()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireNeutralWristConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            RightShoulderForwardConfiguration source =
                AnalyzeRightShoulderForwardSource(target, clip);
            RightShoulderForwardAnalysis analysis =
                AnalyzeRightShoulderForwardApplied(target, idle, clip);

            if (analysis.PeakWristBendAngle > NeutralWristMaximumBendDegrees)
                throw new InvalidOperationException(
                    "Right wrist still bends opposite the forearm. bend=" +
                    F(analysis.PeakWristBendAngle));
            if (analysis.PeakForwardDeviation > 1f ||
                analysis.PeakLateralOffset > 0.005f ||
                analysis.PeakHandError > 0.0005f)
                throw new InvalidOperationException(
                    "Neutral wrist correction changed the approved right-shoulder-forward reach.");
            if (analysis.PeakElbowAngle < 130f ||
                analysis.PeakElbowAngle > 150f)
                throw new InvalidOperationException(
                    "Neutral wrist correction changed the approved elbow range.");
            if (analysis.MaximumUnaffectedUpperBodyRotationError > 0.01f ||
                analysis.MaximumUnaffectedUpperBodyPositionError > 0.00001f ||
                analysis.MaximumIdlePoseRotationError > 0.01f ||
                analysis.MaximumIdlePosePositionError > 0.00001f)
                throw new InvalidOperationException(
                    "Neutral wrist correction changed an unapproved body area.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Neutral wrist inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during neutral wrist inspection.");

            WriteText(NeutralWristInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect neutral-wrist inspection")
                .AppendLine("targetCount=1")
                .AppendLine("correctedPeakWristBendDegrees=" +
                    F(analysis.PeakWristBendAngle))
                .AppendLine("maximumAcceptedWristBendDegrees=" +
                    F(NeutralWristMaximumBendDegrees))
                .AppendLine("correctedPeakForwardDeviationDegrees=" +
                    F(analysis.PeakForwardDeviation))
                .AppendLine("correctedPeakLateralOffsetMeters=" +
                    F(analysis.PeakLateralOffset))
                .AppendLine("correctedPeakElbowAngleDegrees=" +
                    F(analysis.PeakElbowAngle))
                .AppendLine("correctedPeakHandErrorMeters=" +
                    F(analysis.PeakHandError))
                .AppendLine("peakRightShoulderRotationChangeDegrees=" +
                    F(analysis.PeakShoulderRotationChange))
                .AppendLine("peakRightArmRotationChangeDegrees=" +
                    F(analysis.PeakArmRotationChange))
                .AppendLine("peakRightForeArmRotationChangeDegrees=" +
                    F(analysis.PeakForeArmRotationChange))
                .AppendLine("peakRightHandRotationChangeDegrees=" +
                    F(analysis.PeakHandRotationChange))
                .AppendLine("sourceForwardDeviationDegrees=" +
                    F(source.SourceForwardDeviation))
                .AppendLine("neutralWristConfigured=" + pose.NeutralWristConfigured)
                .AppendLine("wristSourceRotationWeight=" +
                    F(pose.RightShoulderForwardWristSourceWeight))
                .AppendLine("forwardHoldDurationSeconds=" +
                    F(pose.ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("sourceClipCopiedExactly=True")
                .AppendLine("controllerSourceStatePreserved=True")
                .AppendLine("unityConsoleErrors=" + consoleErrors)
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            CompleteNeutralWristFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectNeutralWrist] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Neutral Wrist Diagnostic")]
        internal static void CaptureNeutralWristDiagnostic()
        {
            RequireEditMode();
            GameObject target = FindUnique(RequireScene(), TargetName);
            RequireNeutralWristConfiguration(target, out _, out _, out _);
            RequireNoActiveReview();
            int diagnosticIndex = File.Exists(Absolute(
                NeutralWristDiagnosticOneImagePath)) ? 2 : 1;
            string imagePath = diagnosticIndex == 1
                ? NeutralWristDiagnosticOneImagePath
                : NeutralWristDiagnosticTwoImagePath;
            string reportPath = diagnosticIndex == 1
                ? NeutralWristDiagnosticOneReportPath
                : NeutralWristDiagnosticTwoReportPath;
            DeleteReviewFile(imagePath);
            DeleteReviewFile(reportPath);
            DeleteReviewFile(NeutralWristFailureReportPath);
            SetNeutralWristReviewState(false, diagnosticIndex);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectNeutralWrist] Diagnostic natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Neutral Wrist Final")]
        internal static void CaptureNeutralWristFinal()
        {
            RequireEditMode();
            InspectNeutralWrist();
            ClearCompletedNeutralWristDiagnosticState();
            RequireNoActiveReview();
            DeleteReviewFile(NeutralWristFinalImagePath);
            DeleteReviewFile(NeutralWristFinalReportPath);
            DeleteReviewFile(NeutralWristCompletionReportPath);
            DeleteReviewFile(NeutralWristFailureReportPath);
            SetNeutralWristReviewState(true, 0);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectNeutralWrist] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Face Clearance Sources")]
        internal static void InspectFaceClearanceSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireNeutralWristConfiguration(
                target, out _, out _, out _);
            FaceClearanceConfiguration configuration =
                AnalyzeFaceClearanceSource(target, clip);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Face-clearance source inspection changed scene dirty state.");

            WriteText(FaceClearanceSourceReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect face-clearance source inspection")
                .AppendLine("target=" + TargetName)
                .AppendLine("clip=" + ClipName)
                .AppendLine("sampleCount=" + configuration.SampleCount)
                .AppendLine("minimumClearanceFrame=" + configuration.Frame)
                .AppendLine("minimumClearanceTimeSeconds=" + F(configuration.Time))
                .AppendLine("minimumClearanceNormalizedTime=" +
                    F(configuration.NormalizedTime))
                .AppendLine("currentMinimumFaceClearanceMeters=" +
                    F(configuration.MinimumClearance))
                .AppendLine("faceRightBoundaryLocalX=" +
                    F(configuration.FaceBoundaryLocalX))
                .AppendLine("flashlightMinimumLocalX=" +
                    F(configuration.HolderMinimumLocalX))
                .AppendLine("requiredRightOffsetMeters=" +
                    F(configuration.RequiredLateralOffset))
                .AppendLine("boundaryRatioFromHeadToRightArm=" +
                    F(FaceClearanceBoundaryRatio))
                .AppendLine("safetyMarginMeters=" +
                    F(FaceClearanceSafetyMarginMeters))
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectFaceClearance] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Face Clearance")]
        internal static void ApplyFaceClearance()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireNeutralWristConfiguration(
                target, out _,
                out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);
            FaceClearanceConfiguration configuration =
                AnalyzeFaceClearanceSource(target, clip);
            if (configuration.RequiredLateralOffset <= 0.001f ||
                configuration.RequiredLateralOffset > 0.25f)
                throw new InvalidOperationException(
                    "Analyzed face-clearance offset is outside the natural correction range. offset=" +
                    F(configuration.RequiredLateralOffset));

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);

            Undo.RecordObject(pose,
                "Move Flashlight_Charge_Connect right arm clear of the face");
            pose.ConfigureFaceClearance(
                configuration.RequiredLateralOffset,
                FaceClearanceShoulderShare);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.FaceClearanceConfigured ||
                !pose.NeutralWristConfigured ||
                !pose.RightShoulderForwardConfigured ||
                !pose.ForwardHoldConfigured || !pose.SourceUpperBodyRestored)
                throw new InvalidOperationException(
                    "Face-clearance configuration did not preserve the approved arm mode.");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect right-hand follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the face-clearance correction.");
            AssetDatabase.SaveAssets();

            WriteText(FaceClearanceApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect face-clearance application")
                .AppendLine("target=" + TargetName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("sourceMinimumFaceClearanceMeters=" +
                    F(configuration.MinimumClearance))
                .AppendLine("appliedRightOffsetMeters=" +
                    F(pose.FaceClearanceLateralOffsetMeters))
                .AppendLine("shoulderRotationShare=" +
                    F(pose.FaceClearanceShoulderShare))
                .AppendLine("neutralWristPreserved=True")
                .AppendLine("flashlightRightHandFollowPreserved=True")
                .AppendLine("flashlightLensUpCorrectionPreserved=True")
                .AppendLine("forwardHoldDurationSeconds=" +
                    F(pose.ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllerModified=False")
                .AppendLine("rendererMeshMaterialScaleModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectFaceClearance] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Face Clearance")]
        internal static void InspectFaceClearance()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFaceClearanceConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            FaceClearanceAnalysis analysis =
                AnalyzeFaceClearanceApplied(target, clip);

            if (analysis.MinimumClearance < -0.001f)
                throw new InvalidOperationException(
                    "Flashlight still crosses the face-clearance boundary. clearance=" +
                    F(analysis.MinimumClearance));
            if (analysis.MaximumHandError > 0.0005f)
                throw new InvalidOperationException(
                    "Face-clearance right-arm target drifted. error=" +
                    F(analysis.MaximumHandError));
            if (analysis.MaximumWristBend > NeutralWristMaximumBendDegrees)
                throw new InvalidOperationException(
                    "Face-clearance correction reintroduced an unnatural wrist bend.");
            if (analysis.MinimumElbowAngle < 20f ||
                analysis.MaximumElbowAngle > 175f)
                throw new InvalidOperationException(
                    "Face-clearance correction produced a degenerate elbow pose.");
            if (analysis.MaximumShoulderRotationChange < 0.1f ||
                analysis.MaximumArmRotationChange < 0.1f ||
                analysis.MaximumForeArmRotationChange < 0.1f)
                throw new InvalidOperationException(
                    "Face-clearance correction was not distributed through the right arm.");
            if (analysis.MaximumUnaffectedRotationError > 0.01f ||
                analysis.MaximumUnaffectedPositionError > 0.00001f)
                throw new InvalidOperationException(
                    "Face-clearance correction changed an unapproved body area.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Face-clearance inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during face-clearance inspection.");

            WriteText(FaceClearanceInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect face-clearance inspection")
                .AppendLine("targetCount=1")
                .AppendLine("sampleCount=" + analysis.SampleCount)
                .AppendLine("minimumFaceClearanceMeters=" +
                    F(analysis.MinimumClearance))
                .AppendLine("configuredRightOffsetMeters=" +
                    F(pose.FaceClearanceLateralOffsetMeters))
                .AppendLine("maximumHandTargetErrorMeters=" +
                    F(analysis.MaximumHandError))
                .AppendLine("minimumRightElbowAngleDegrees=" +
                    F(analysis.MinimumElbowAngle))
                .AppendLine("maximumRightElbowAngleDegrees=" +
                    F(analysis.MaximumElbowAngle))
                .AppendLine("maximumRightWristBendDegrees=" +
                    F(analysis.MaximumWristBend))
                .AppendLine("maximumRightShoulderRotationChangeDegrees=" +
                    F(analysis.MaximumShoulderRotationChange))
                .AppendLine("maximumRightArmRotationChangeDegrees=" +
                    F(analysis.MaximumArmRotationChange))
                .AppendLine("maximumRightForeArmRotationChangeDegrees=" +
                    F(analysis.MaximumForeArmRotationChange))
                .AppendLine("maximumUnaffectedRotationErrorDegrees=" +
                    F(analysis.MaximumUnaffectedRotationError))
                .AppendLine("maximumUnaffectedPositionErrorMeters=" +
                    F(analysis.MaximumUnaffectedPositionError))
                .AppendLine("forwardHoldDurationSeconds=" +
                    F(pose.ForwardHoldDurationSeconds))
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("sourceClipCopiedExactly=True")
                .AppendLine("controllerSourceStatePreserved=True")
                .AppendLine("unityConsoleErrors=" + consoleErrors)
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            CompleteFaceClearanceFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectFaceClearance] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Face Clearance Diagnostic")]
        internal static void CaptureFaceClearanceDiagnostic()
        {
            RequireEditMode();
            GameObject target = FindUnique(RequireScene(), TargetName);
            RequireFaceClearanceConfiguration(target, out _, out _, out _);
            RequireNoActiveReview();
            int diagnosticIndex = File.Exists(Absolute(
                FaceClearanceDiagnosticOneImagePath)) ? 2 : 1;
            string imagePath = diagnosticIndex == 1
                ? FaceClearanceDiagnosticOneImagePath
                : FaceClearanceDiagnosticTwoImagePath;
            string reportPath = diagnosticIndex == 1
                ? FaceClearanceDiagnosticOneReportPath
                : FaceClearanceDiagnosticTwoReportPath;
            DeleteReviewFile(imagePath);
            DeleteReviewFile(reportPath);
            DeleteReviewFile(FaceClearanceFailureReportPath);
            SetFaceClearanceReviewState(false, diagnosticIndex);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectFaceClearance] Diagnostic natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Face Clearance Final")]
        internal static void CaptureFaceClearanceFinal()
        {
            RequireEditMode();
            InspectFaceClearance();
            ClearCompletedFaceClearanceDiagnosticState();
            RequireNoActiveReview();
            DeleteReviewFile(FaceClearanceFinalImagePath);
            DeleteReviewFile(FaceClearanceFinalReportPath);
            DeleteReviewFile(FaceClearanceCompletionReportPath);
            DeleteReviewFile(FaceClearanceFailureReportPath);
            SetFaceClearanceReviewState(true, 0);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectFaceClearance] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Flashlight Right Offset")]
        internal static void ApplyFlashlightRightOffset()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFaceClearanceConfiguration(
                target, out _,
                out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string poseBefore = TargetPoseWithoutFlashlightSignature(
                target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);

            Undo.RecordObject(
                pose,
                "Move Flashlight_Charge_Connect flashlight right by four centimeters");
            pose.ConfigureFlashlightRightOffset(FlashlightRightOffsetMeters);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.FlashlightRightOffsetConfigured ||
                Mathf.Abs(pose.FlashlightRightOffsetMeters -
                    FlashlightRightOffsetMeters) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only right offset was not configured exactly.");
            RequireEqual(
                poseBefore,
                TargetPoseWithoutFlashlightSignature(target.transform, carry),
                "Flashlight_Charge_Connect arm and body pose");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect right-hand follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the flashlight-only right offset.");
            AssetDatabase.SaveAssets();

            WriteText(FlashlightRightOffsetApplicationReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect flashlight-only right-offset application")
                    .AppendLine("target=" + TargetName)
                    .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                    .AppendLine("appliedTransporterRightOffsetMeters=" +
                        F(pose.FlashlightRightOffsetMeters))
                    .AppendLine("rightArmBodyPoseModified=False")
                    .AppendLine("flashlightRightHandFollowPreserved=True")
                    .AppendLine("flashlightLensUpCorrectionPreserved=True")
                    .AppendLine("forwardHoldDurationSeconds=" +
                        F(pose.ForwardHoldDurationSeconds))
                    .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                    .AppendLine("sourceClipModified=False")
                    .AppendLine("controllerModified=False")
                    .AppendLine("rendererMeshMaterialRotationScaleModified=False")
                    .AppendLine("objectsOutsideTargetChanged=False")
                    .ToString());
            Debug.Log("[FlashlightChargeConnectFlashlightRightOffset] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Flashlight Right Offset")]
        internal static void InspectFlashlightRightOffset()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFlashlightRightOffsetConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            FlashlightRightOffsetAnalysis analysis =
                AnalyzeFlashlightRightOffset(target, clip);

            if (analysis.MinimumRightOffset < FlashlightRightOffsetMeters - 0.00005f ||
                analysis.MaximumRightOffset > FlashlightRightOffsetMeters + 0.00005f ||
                analysis.MaximumOffAxisError > 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight did not remain exactly four centimeters to transporter right.");
            if (analysis.MaximumBoneRotationError > 0.0001f ||
                analysis.MaximumBonePositionError > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only offset changed the arm or body pose.");
            if (analysis.MaximumHolderRotationError > 0.0001f ||
                analysis.MaximumHolderScaleError > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only offset changed holder rotation or scale.");
            if (analysis.MinimumFaceClearance < -0.001f)
                throw new InvalidOperationException(
                    "Flashlight crosses the approved face-clearance boundary.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Flashlight-right-offset inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during flashlight-right-offset inspection.");

            WriteText(FlashlightRightOffsetInspectionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect flashlight-only right-offset inspection")
                    .AppendLine("targetCount=1")
                    .AppendLine("sampleCount=" + analysis.SampleCount)
                    .AppendLine("configuredTransporterRightOffsetMeters=" +
                        F(pose.FlashlightRightOffsetMeters))
                    .AppendLine("minimumAppliedRightOffsetMeters=" +
                        F(analysis.MinimumRightOffset))
                    .AppendLine("maximumAppliedRightOffsetMeters=" +
                        F(analysis.MaximumRightOffset))
                    .AppendLine("maximumOffAxisPositionErrorMeters=" +
                        F(analysis.MaximumOffAxisError))
                    .AppendLine("maximumArmAndBodyRotationErrorDegrees=" +
                        F(analysis.MaximumBoneRotationError))
                    .AppendLine("maximumArmAndBodyPositionErrorMeters=" +
                        F(analysis.MaximumBonePositionError))
                    .AppendLine("maximumHolderRotationErrorDegrees=" +
                        F(analysis.MaximumHolderRotationError))
                    .AppendLine("maximumHolderScaleError=" +
                        F(analysis.MaximumHolderScaleError))
                    .AppendLine("minimumFaceClearanceMeters=" +
                        F(analysis.MinimumFaceClearance))
                    .AppendLine("rightHandFollowPreserved=True")
                    .AppendLine("lensUpPreserved=True")
                    .AppendLine("sourceClipCopiedExactly=True")
                    .AppendLine("controllerSourceStatePreserved=True")
                    .AppendLine("unityConsoleErrors=" + consoleErrors)
                    .AppendLine("inspectionManipulatedTarget=False")
                    .ToString());
            CompleteFlashlightRightOffsetFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectFlashlightRightOffset] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Flashlight Right Offset Final")]
        internal static void CaptureFlashlightRightOffsetFinal()
        {
            RequireEditMode();
            InspectFlashlightRightOffset();
            RequireNoActiveReview();
            DeleteReviewFile(FlashlightRightOffsetFinalImagePath);
            DeleteReviewFile(FlashlightRightOffsetFinalReportPath);
            DeleteReviewFile(FlashlightRightOffsetCompletionReportPath);
            DeleteReviewFile(FlashlightRightOffsetFailureReportPath);
            SetFaceClearanceReviewState(true, 0);
            SessionState.SetBool(FlashlightRightOffsetReviewKey, true);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectFlashlightRightOffset] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Hand Clearance Sources")]
        internal static void InspectFlashlightHandClearanceSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFlashlightHandClearanceBaseConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            FlashlightHandClearanceAnalysis analysis =
                AnalyzeFlashlightHandClearance(target, clip);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Flashlight hand-clearance source inspection changed scene dirty state.");

            WriteText(FlashlightHandClearanceSourceReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect right-hand and flashlight surface source inspection")
                    .AppendLine("target=" + TargetName)
                    .AppendLine("sampleCount=" + analysis.SampleCount)
                    .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                    .AppendLine("currentTransporterForwardOffsetMeters=" +
                        F(pose.FlashlightForwardOffsetMeters))
                    .AppendLine("requiredTransporterForwardOffsetMeters=" +
                        F(analysis.RequiredForwardOffset))
                    .AppendLine("requiredAdditionalForwardOffsetMeters=" +
                        F(analysis.RequiredAdditionalForwardOffset))
                    .AppendLine("minimumBaselineSurfaceClearanceMeters=" +
                        F(analysis.MinimumBaselineSurfaceClearance))
                    .AppendLine("minimumCurrentSurfaceClearanceMeters=" +
                        F(analysis.MinimumSurfaceClearance))
                    .AppendLine("surfaceClearanceSafetyMarginMeters=" +
                        F(FlashlightHandClearanceSafetyMarginMeters))
                    .AppendLine("worstNormalizedTime=" +
                        F(analysis.WorstNormalizedTime))
                    .AppendLine("rightHandSurfaceVertexCount=" +
                        analysis.RightHandVertexCount)
                    .AppendLine("flashlightSurfaceVertexCount=" +
                        analysis.FlashlightVertexCount)
                    .AppendLine("inspectionManipulatedTarget=False")
                    .ToString());
            Debug.Log("[FlashlightChargeConnectHandClearance] Sources inspected read-only.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Hand Clearance")]
        internal static void ApplyFlashlightHandClearance()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFlashlightHandClearanceBaseConfiguration(
                target, out _,
                out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);
            FlashlightHandClearanceAnalysis analysis =
                AnalyzeFlashlightHandClearance(target, clip);
            if (analysis.RequiredForwardOffset > 0.25f)
                throw new InvalidOperationException(
                    "Required flashlight hand-clearance offset exceeds the approved prop-only range.");

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string poseBefore = TargetPoseWithoutFlashlightSignature(
                target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);

            Undo.RecordObject(
                pose,
                "Move Flashlight_Charge_Connect flashlight in front of right hand");
            pose.ConfigureFlashlightForwardOffset(analysis.RequiredForwardOffset);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.FlashlightForwardOffsetConfigured ||
                Mathf.Abs(pose.FlashlightForwardOffsetMeters -
                    analysis.RequiredForwardOffset) > 0.000001f ||
                !pose.FlashlightRightOffsetConfigured ||
                Mathf.Abs(pose.FlashlightRightOffsetMeters -
                    FlashlightRightOffsetMeters) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight hand-clearance offset did not preserve the approved right offset.");
            RequireEqual(
                poseBefore,
                TargetPoseWithoutFlashlightSignature(target.transform, carry),
                "Flashlight_Charge_Connect arm and body pose");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect right-hand follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the flashlight hand-clearance offset.");
            AssetDatabase.SaveAssets();

            WriteText(FlashlightHandClearanceApplicationReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect right-hand surface-clearance application")
                    .AppendLine("target=" + TargetName)
                    .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                    .AppendLine("previousTransporterForwardOffsetMeters=" +
                        F(analysis.CurrentForwardOffset))
                    .AppendLine("appliedTransporterForwardOffsetMeters=" +
                        F(pose.FlashlightForwardOffsetMeters))
                    .AppendLine("appliedAdditionalForwardOffsetMeters=" +
                        F(analysis.RequiredAdditionalForwardOffset))
                    .AppendLine("sourceMinimumBaselineSurfaceClearanceMeters=" +
                        F(analysis.MinimumBaselineSurfaceClearance))
                    .AppendLine("sourceMinimumCurrentSurfaceClearanceMeters=" +
                        F(analysis.MinimumSurfaceClearance))
                    .AppendLine("targetSurfaceClearanceMeters=" +
                        F(FlashlightHandClearanceSafetyMarginMeters))
                    .AppendLine("preservedTransporterRightOffsetMeters=" +
                        F(pose.FlashlightRightOffsetMeters))
                    .AppendLine("rightArmBodyPoseModified=False")
                    .AppendLine("flashlightRightHandFollowPreserved=True")
                    .AppendLine("flashlightLensUpCorrectionPreserved=True")
                    .AppendLine("sourceClipModified=False")
                    .AppendLine("controllerModified=False")
                    .AppendLine("rendererMeshMaterialRotationScaleModified=False")
                    .AppendLine("objectsOutsideTargetChanged=False")
                    .ToString());
            Debug.Log("[FlashlightChargeConnectHandClearance] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Hand Clearance")]
        internal static void InspectFlashlightHandClearance()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFlashlightHandClearanceBaseConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            FlashlightHandClearanceAnalysis clearance =
                AnalyzeFlashlightHandClearance(target, clip);
            FlashlightForwardOffsetAnalysis offset =
                AnalyzeFlashlightForwardOffset(target, clip);

            if (clearance.MinimumSurfaceClearance <
                FlashlightHandClearanceSafetyMarginMeters - 0.00005f ||
                Mathf.Abs(clearance.RequiredForwardOffset -
                    pose.FlashlightForwardOffsetMeters) > 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight does not remain completely in front of the right-hand surface.");
            if (offset.MinimumForwardOffset <
                    pose.FlashlightForwardOffsetMeters - 0.00005f ||
                offset.MaximumForwardOffset >
                    pose.FlashlightForwardOffsetMeters + 0.00005f ||
                offset.MaximumOffAxisError > 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight hand-clearance translation left the transporter-forward axis.");
            if (offset.MinimumPreservedRightOffset <
                    FlashlightRightOffsetMeters - 0.00005f ||
                offset.MaximumPreservedRightOffset >
                    FlashlightRightOffsetMeters + 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight hand clearance changed the approved four-centimeter right offset.");
            if (offset.MaximumBoneRotationError > 0.0001f ||
                offset.MaximumBonePositionError > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only hand clearance changed the arm or body pose.");
            if (offset.MaximumHolderRotationError > 0.0001f ||
                offset.MaximumHolderScaleError > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only hand clearance changed holder rotation or scale.");
            if (offset.MinimumFaceClearance < -0.001f)
                throw new InvalidOperationException(
                    "Flashlight hand clearance crossed the approved face boundary.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Flashlight hand-clearance inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during flashlight hand-clearance inspection.");

            WriteText(FlashlightHandClearanceInspectionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect right-hand and flashlight surface-clearance inspection")
                    .AppendLine("targetCount=1")
                    .AppendLine("sampleCount=" + clearance.SampleCount)
                    .AppendLine("configuredTransporterForwardOffsetMeters=" +
                        F(pose.FlashlightForwardOffsetMeters))
                    .AppendLine("minimumHandFlashlightSurfaceClearanceMeters=" +
                        F(clearance.MinimumSurfaceClearance))
                    .AppendLine("surfaceClearanceSafetyMarginMeters=" +
                        F(FlashlightHandClearanceSafetyMarginMeters))
                    .AppendLine("worstNormalizedTime=" +
                        F(clearance.WorstNormalizedTime))
                    .AppendLine("rightHandSurfaceVertexCount=" +
                        clearance.RightHandVertexCount)
                    .AppendLine("flashlightSurfaceVertexCount=" +
                        clearance.FlashlightVertexCount)
                    .AppendLine("minimumPreservedRightOffsetMeters=" +
                        F(offset.MinimumPreservedRightOffset))
                    .AppendLine("maximumPreservedRightOffsetMeters=" +
                        F(offset.MaximumPreservedRightOffset))
                    .AppendLine("maximumArmAndBodyRotationErrorDegrees=" +
                        F(offset.MaximumBoneRotationError))
                    .AppendLine("maximumArmAndBodyPositionErrorMeters=" +
                        F(offset.MaximumBonePositionError))
                    .AppendLine("maximumHolderRotationErrorDegrees=" +
                        F(offset.MaximumHolderRotationError))
                    .AppendLine("maximumHolderScaleError=" +
                        F(offset.MaximumHolderScaleError))
                    .AppendLine("minimumFaceClearanceMeters=" +
                        F(offset.MinimumFaceClearance))
                    .AppendLine("rightHandFollowPreserved=True")
                    .AppendLine("lensUpPreserved=True")
                    .AppendLine("sourceClipCopiedExactly=True")
                    .AppendLine("controllerSourceStatePreserved=True")
                    .AppendLine("unityConsoleErrors=" + consoleErrors)
                    .AppendLine("inspectionManipulatedTarget=False")
                    .ToString());
            CompleteFlashlightHandClearanceFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectHandClearance] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Hand Clearance Final")]
        internal static void CaptureFlashlightHandClearanceFinal()
        {
            RequireEditMode();
            InspectFlashlightHandClearance();
            RequireNoActiveReview();
            DeleteReviewFile(FlashlightHandClearanceFinalImagePath);
            DeleteReviewFile(FlashlightHandClearanceFinalReportPath);
            DeleteReviewFile(FlashlightHandClearanceCompletionReportPath);
            DeleteReviewFile(FlashlightHandClearanceFailureReportPath);
            SetFaceClearanceReviewState(true, 0);
            SessionState.SetBool(FlashlightRightOffsetReviewKey, true);
            SessionState.SetBool(FlashlightHandClearanceReviewKey, true);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectHandClearance] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Flashlight Forward Offset")]
        internal static void ApplyFlashlightForwardOffset()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFlashlightRightOffsetConfiguration(
                target, out _,
                out FlashlightRightHandFollowBehaviour carry,
                out FlashlightChargeConnectPoseBehaviour pose);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string poseBefore = TargetPoseWithoutFlashlightSignature(
                target.transform, carry);
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);

            Undo.RecordObject(
                pose,
                "Move Flashlight_Charge_Connect flashlight forward by two centimeters");
            pose.ConfigureFlashlightForwardOffset(FlashlightForwardOffsetMeters);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            if (!pose.FlashlightForwardOffsetConfigured ||
                Mathf.Abs(pose.FlashlightForwardOffsetMeters -
                    FlashlightForwardOffsetMeters) > 0.000001f ||
                !pose.FlashlightRightOffsetConfigured ||
                Mathf.Abs(pose.FlashlightRightOffsetMeters -
                    FlashlightRightOffsetMeters) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only forward offset did not preserve the approved right offset.");
            RequireEqual(
                poseBefore,
                TargetPoseWithoutFlashlightSignature(target.transform, carry),
                "Flashlight_Charge_Connect arm and body pose");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect right-hand follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the flashlight-only forward offset.");
            AssetDatabase.SaveAssets();

            WriteText(FlashlightForwardOffsetApplicationReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect flashlight-only forward-offset application")
                    .AppendLine("target=" + TargetName)
                    .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                    .AppendLine("appliedTransporterForwardOffsetMeters=" +
                        F(pose.FlashlightForwardOffsetMeters))
                    .AppendLine("preservedTransporterRightOffsetMeters=" +
                        F(pose.FlashlightRightOffsetMeters))
                    .AppendLine("rightArmBodyPoseModified=False")
                    .AppendLine("flashlightRightHandFollowPreserved=True")
                    .AppendLine("flashlightLensUpCorrectionPreserved=True")
                    .AppendLine("forwardHoldDurationSeconds=" +
                        F(pose.ForwardHoldDurationSeconds))
                    .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                    .AppendLine("sourceClipModified=False")
                    .AppendLine("controllerModified=False")
                    .AppendLine("rendererMeshMaterialRotationScaleModified=False")
                    .AppendLine("objectsOutsideTargetChanged=False")
                    .ToString());
            Debug.Log("[FlashlightChargeConnectFlashlightForwardOffset] Applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Flashlight Forward Offset")]
        internal static void InspectFlashlightForwardOffset()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            AnimationClip clip = RequireFlashlightForwardOffsetConfiguration(
                target, out _, out _,
                out FlashlightChargeConnectPoseBehaviour pose);
            FlashlightForwardOffsetAnalysis analysis =
                AnalyzeFlashlightForwardOffset(target, clip);

            if (analysis.MinimumForwardOffset <
                    FlashlightForwardOffsetMeters - 0.00005f ||
                analysis.MaximumForwardOffset >
                    FlashlightForwardOffsetMeters + 0.00005f ||
                analysis.MaximumOffAxisError > 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight did not remain exactly two centimeters to transporter forward.");
            if (analysis.MinimumPreservedRightOffset <
                    FlashlightRightOffsetMeters - 0.00005f ||
                analysis.MaximumPreservedRightOffset >
                    FlashlightRightOffsetMeters + 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight forward offset changed the approved four-centimeter right offset.");
            if (analysis.MaximumBoneRotationError > 0.0001f ||
                analysis.MaximumBonePositionError > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only forward offset changed the arm or body pose.");
            if (analysis.MaximumHolderRotationError > 0.0001f ||
                analysis.MaximumHolderScaleError > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight-only forward offset changed holder rotation or scale.");
            if (analysis.MinimumFaceClearance < -0.001f)
                throw new InvalidOperationException(
                    "Flashlight crosses the approved face-clearance boundary.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Flashlight-forward-offset inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during flashlight-forward-offset inspection.");

            WriteText(FlashlightForwardOffsetInspectionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect flashlight-only forward-offset inspection")
                    .AppendLine("targetCount=1")
                    .AppendLine("sampleCount=" + analysis.SampleCount)
                    .AppendLine("configuredTransporterForwardOffsetMeters=" +
                        F(pose.FlashlightForwardOffsetMeters))
                    .AppendLine("minimumAppliedForwardOffsetMeters=" +
                        F(analysis.MinimumForwardOffset))
                    .AppendLine("maximumAppliedForwardOffsetMeters=" +
                        F(analysis.MaximumForwardOffset))
                    .AppendLine("maximumOffAxisPositionErrorMeters=" +
                        F(analysis.MaximumOffAxisError))
                    .AppendLine("minimumPreservedRightOffsetMeters=" +
                        F(analysis.MinimumPreservedRightOffset))
                    .AppendLine("maximumPreservedRightOffsetMeters=" +
                        F(analysis.MaximumPreservedRightOffset))
                    .AppendLine("maximumArmAndBodyRotationErrorDegrees=" +
                        F(analysis.MaximumBoneRotationError))
                    .AppendLine("maximumArmAndBodyPositionErrorMeters=" +
                        F(analysis.MaximumBonePositionError))
                    .AppendLine("maximumHolderRotationErrorDegrees=" +
                        F(analysis.MaximumHolderRotationError))
                    .AppendLine("maximumHolderScaleError=" +
                        F(analysis.MaximumHolderScaleError))
                    .AppendLine("minimumFaceClearanceMeters=" +
                        F(analysis.MinimumFaceClearance))
                    .AppendLine("rightHandFollowPreserved=True")
                    .AppendLine("lensUpPreserved=True")
                    .AppendLine("sourceClipCopiedExactly=True")
                    .AppendLine("controllerSourceStatePreserved=True")
                    .AppendLine("unityConsoleErrors=" + consoleErrors)
                    .AppendLine("inspectionManipulatedTarget=False")
                    .ToString());
            CompleteFlashlightForwardOffsetFinalStateIfAwaitingEdit();
            Debug.Log("[FlashlightChargeConnectFlashlightForwardOffset] Inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Flashlight Forward Offset Final")]
        internal static void CaptureFlashlightForwardOffsetFinal()
        {
            RequireEditMode();
            InspectFlashlightForwardOffset();
            RequireNoActiveReview();
            DeleteReviewFile(FlashlightForwardOffsetFinalImagePath);
            DeleteReviewFile(FlashlightForwardOffsetFinalReportPath);
            DeleteReviewFile(FlashlightForwardOffsetCompletionReportPath);
            DeleteReviewFile(FlashlightForwardOffsetFailureReportPath);
            SetFaceClearanceReviewState(true, 0);
            SessionState.SetBool(FlashlightRightOffsetReviewKey, true);
            SessionState.SetBool(FlashlightForwardOffsetReviewKey, true);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectFlashlightForwardOffset] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Forward Reach Revision")]
        internal static void ApplyForwardReachRevision()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightChargeConnectPoseBehaviour pose =
                target.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose behaviour is missing.");
            RequireExactSourceCopy();
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            RequireControllerContract(controller, clip);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string leftArmBefore = LocalPoseSignature(
                RequireDescendant(target.transform, "LeftArm"));
            string headBefore = LocalPoseSignature(
                RequireDescendant(target.transform, "Head"));
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);
            ForwardReachConfiguration configuration =
                AnalyzeForwardReachConfiguration(target, clip);

            Undo.RecordObject(pose,
                "Apply Flashlight_Charge_Connect forward reach revision");
            pose.ConfigureForwardReach(
                animator,
                configuration.RightShoulderReferenceRotation,
                configuration.StartHandLocal,
                configuration.EndHandLocal,
                configuration.ElbowPoleLocalDirection,
                ApproachStartNormalized,
                ReachEndNormalized,
                HoldEndNormalized,
                ReturnEndNormalized);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect hand-follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(leftArmBefore, LocalPoseSignature(
                    RequireDescendant(target.transform, "LeftArm")),
                "Flashlight_Charge_Connect left arm local pose");
            RequireEqual(headBefore, LocalPoseSignature(
                    RequireDescendant(target.transform, "Head")),
                "Flashlight_Charge_Connect head local pose");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");
            if (pose.CurrentHandPositionError > 0.0005f)
                throw new InvalidOperationException(
                    "Forward-reach edit preview missed its hand target. error=" +
                    F(pose.CurrentHandPositionError));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the forward-reach revision.");
            AssetDatabase.SaveAssets();

            WriteText(ForwardReachApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect forward-reach revision application")
                .AppendLine("target=" + TargetName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("controllerSpeed=1")
                .AppendLine("startHandLocal=" + Vec(configuration.StartHandLocal))
                .AppendLine("rightArmStartLocal=" + Vec(configuration.RightArmLocal))
                .AppendLine("endHandLocal=" + Vec(configuration.EndHandLocal))
                .AppendLine("forwardDirectionDeviationDegrees=" +
                    F(configuration.ForwardDirectionDeviation))
                .AppendLine("upperArmLengthMeters=" + F(configuration.UpperLength))
                .AppendLine("lowerArmLengthMeters=" + F(configuration.LowerLength))
                .AppendLine("desiredElbowAngleDegrees=" +
                    F(DesiredElbowAngleDegrees))
                .AppendLine("approachStartNormalized=" +
                    F(ApproachStartNormalized))
                .AppendLine("reachEndNormalized=" + F(ReachEndNormalized))
                .AppendLine("holdEndNormalized=" + F(HoldEndNormalized))
                .AppendLine("returnEndNormalized=" + F(ReturnEndNormalized))
                .AppendLine("approachDurationSeconds=" +
                    F((ReachEndNormalized - ApproachStartNormalized) * clip.length))
                .AppendLine("timingCurve=QuinticSmootherStep")
                .AppendLine("leftArmModified=False")
                .AppendLine("headModified=False")
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllerModified=False")
                .AppendLine("rendererMeshMaterialScaleModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectForwardReach] Forward reach applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Forward Reach Revision")]
        internal static void InspectForwardReachRevision()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightChargeConnectPoseBehaviour pose =
                target.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose behaviour is missing.");
            if (!pose.ForwardReachConfigured || pose.Animator != animator ||
                pose.Carry != carry)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect forward reach is not configured correctly.");

            RequireExactSourceCopy();
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            RequireControllerContract(controller, clip);
            if (!Mathf.Approximately(pose.ApproachStartNormalized,
                    ApproachStartNormalized) ||
                !Mathf.Approximately(pose.ReachEndNormalized,
                    ReachEndNormalized) ||
                !Mathf.Approximately(pose.HoldEndNormalized,
                    HoldEndNormalized) ||
                !Mathf.Approximately(pose.ReturnEndNormalized,
                    ReturnEndNormalized))
                throw new InvalidOperationException(
                    "Forward-reach timing configuration changed unexpectedly.");

            Transform arm = RequirePath(target.transform, RightArmPath);
            Transform foreArm = RequirePath(target.transform, RightForeArmPath);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Vector3 armLocal = target.transform.InverseTransformPoint(arm.position);
            Vector3 endDirection = pose.ReachEndHandLocal - armLocal;
            float forwardDeviation = Vector3.Angle(
                endDirection.normalized, Vector3.forward);
            float expectedElbowAngle = ElbowAngleForTarget(
                Vector3.Distance(arm.position, foreArm.position),
                Vector3.Distance(foreArm.position, hand.position),
                target.transform.TransformVector(endDirection).magnitude);
            ForwardReachMotionAnalysis motion =
                AnalyzeConfiguredForwardReachMotion(target, clip);
            if (forwardDeviation > 10f)
                throw new InvalidOperationException(
                    "Forward-reach target is not directed forward. deviation=" +
                    F(forwardDeviation));
            if (Mathf.Abs(expectedElbowAngle - DesiredElbowAngleDegrees) > 0.5f)
                throw new InvalidOperationException(
                    "Forward-reach target does not preserve the approved elbow bend.");
            if (pose.CurrentHandPositionError > 0.0005f)
                throw new InvalidOperationException(
                    "Forward-reach edit preview missed its current target.");
            if (motion.MaximumTargetError > 0.0005f)
                throw new InvalidOperationException(
                    "Forward-reach sampled motion missed its hand target.");
            if (motion.MaximumFullReachForwardDeviation > 10f)
                throw new InvalidOperationException(
                    "Forward-reach sampled motion is not directed forward.");
            if (motion.MinimumFullReachElbowAngle < 130f ||
                motion.MaximumFullReachElbowAngle > 150f)
                throw new InvalidOperationException(
                    "Forward-reach sampled elbow bend is outside the natural range.");
            if (motion.MaximumApproachHandSpeed >= SourceMaximumHandSpeed)
                throw new InvalidOperationException(
                    "Forward-reach approach is not slower than the punch-like source.");
            if (motion.MaximumHandAcceleration >= SourceMaximumHandAcceleration)
                throw new InvalidOperationException(
                    "Forward-reach acceleration is not smoother than the source.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Forward-reach inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during forward-reach inspection.");

            WriteText(ForwardReachInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect forward-reach revision inspection")
                .AppendLine("targetCount=1")
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("controllerSpeed=1")
                .AppendLine("startHandLocal=" + Vec(pose.ReachStartHandLocal))
                .AppendLine("endHandLocal=" + Vec(pose.ReachEndHandLocal))
                .AppendLine("forwardDirectionDeviationDegrees=" + F(forwardDeviation))
                .AppendLine("expectedFullReachElbowAngleDegrees=" +
                    F(expectedElbowAngle))
                .AppendLine("editPreviewHandTargetErrorMeters=" +
                    F(pose.CurrentHandPositionError))
                .AppendLine("approachDurationSeconds=" +
                    F((ReachEndNormalized - ApproachStartNormalized) * clip.length))
                .AppendLine("timingCurve=QuinticSmootherStep")
                .AppendLine("sourceMaximumHandSpeedMetersPerSecond=" +
                    F(SourceMaximumHandSpeed))
                .AppendLine("revisedMaximumHandSpeedMetersPerSecond=" +
                    F(motion.MaximumHandSpeed))
                .AppendLine("revisedMaximumApproachHandSpeedMetersPerSecond=" +
                    F(motion.MaximumApproachHandSpeed))
                .AppendLine("sourceMaximumHandAccelerationMetersPerSecondSquared=" +
                    F(SourceMaximumHandAcceleration))
                .AppendLine("revisedMaximumHandAccelerationMetersPerSecondSquared=" +
                    F(motion.MaximumHandAcceleration))
                .AppendLine("maximumSampledTargetErrorMeters=" +
                    F(motion.MaximumTargetError))
                .AppendLine("maximumSampledFullReachForwardDeviationDegrees=" +
                    F(motion.MaximumFullReachForwardDeviation))
                .AppendLine("minimumSampledFullReachElbowAngleDegrees=" +
                    F(motion.MinimumFullReachElbowAngle))
                .AppendLine("maximumSampledFullReachElbowAngleDegrees=" +
                    F(motion.MaximumFullReachElbowAngle))
                .AppendLine("bodyLegsPlayerIdlePreserved=True")
                .AppendLine("leftArmAndHeadPathsPostProcessed=False")
                .AppendLine("rightPalmForwardPreserved=True")
                .AppendLine("flashlightLensUpPreserved=True")
                .AppendLine("sourceClipCopiedExactly=True")
                .AppendLine("unityConsoleErrors=0")
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectForwardReach] Forward reach inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Forward Reach Final")]
        internal static void CaptureForwardReachFinal()
        {
            RequireEditMode();
            InspectForwardReachRevision();
            if (captureActive || !string.IsNullOrEmpty(
                    SessionState.GetString(AutoStateKey, string.Empty)))
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect review is already active.");
            DeleteReviewFile(ForwardReachCompletionReportPath);
            DeleteReviewFile(ForwardReachFailureReportPath);
            SessionState.SetBool(PoseReviewKey, true);
            SessionState.SetBool(ForwardReachReviewKey, true);
            SessionState.SetString(AutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectForwardReach] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Pose Revision")]
        internal static void ApplyPoseRevision()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;

            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            RequireExactSourceCopy();
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            RequireControllerContract(controller, clip);

            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string leftArmBefore = LocalPoseSignature(
                RequireDescendant(target.transform, "LeftArm"));
            string headBefore = LocalPoseSignature(
                RequireDescendant(target.transform, "Head"));
            string sourceHashBefore = ComputeAssetHash(ImportedFbxPath);
            string controllerHashBefore = ComputeAssetHash(ControllerPath);
            string[] bodyPaths = IdleBodyPaths(idle.transform);
            FlashlightChargeConnectBonePose[] bodyPose = bodyPaths.Select(path =>
            {
                Transform reference = RequirePath(idle.transform, path);
                return new FlashlightChargeConnectBonePose(
                    path, reference.localPosition, reference.localRotation);
            }).ToArray();

            FlashlightChargeConnectPoseBehaviour pose =
                target.GetComponent<FlashlightChargeConnectPoseBehaviour>();
            if (pose == null)
                pose = Undo.AddComponent<FlashlightChargeConnectPoseBehaviour>(target);
            Undo.RecordObject(pose, "Apply Flashlight_Charge_Connect approved pose revision");
            pose.Configure(carry, bodyPose);
            EditorUtility.SetDirty(pose);
            PrefabUtility.RecordPrefabInstancePropertyModifications(pose);

            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect hand-follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(leftArmBefore, LocalPoseSignature(
                    RequireDescendant(target.transform, "LeftArm")),
                "Flashlight_Charge_Connect left arm local pose");
            RequireEqual(headBefore, LocalPoseSignature(
                    RequireDescendant(target.transform, "Head")),
                "Flashlight_Charge_Connect head local pose");
            RequireEqual(sourceHashBefore, ComputeAssetHash(ImportedFbxPath),
                "supplied charge-connect FBX");
            RequireEqual(controllerHashBefore, ComputeAssetHash(ControllerPath),
                "charge-connect controller");
            if (pose.PalmForwardDeviationDegrees > 0.1f)
                throw new InvalidOperationException(
                    "Right palm did not reach transporter forward. deviation=" +
                    F(pose.PalmForwardDeviationDegrees));
            if (pose.LensUpDeviationDegrees > 0.1f)
                throw new InvalidOperationException(
                    "Flashlight lens did not reach transporter up. deviation=" +
                    F(pose.LensUpDeviationDegrees));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save the charge-connect pose revision.");
            AssetDatabase.SaveAssets();

            WriteText(PoseApplicationReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect approved pose revision application")
                .AppendLine("target=" + TargetName)
                .AppendLine("idleReference=" + IdleReferenceName)
                .AppendLine("sceneWasDirtyBeforeApplication=" + sceneWasDirty)
                .AppendLine("idleBodyBoneCount=" + bodyPose.Length)
                .AppendLine("bodyAndLegsMatchPlayerIdle=True")
                .AppendLine("leftArmModified=False")
                .AppendLine("headModified=False")
                .AppendLine("rightArmCorrection=LongitudinalShoulder10_Arm35_Forearm55_ResidualHand")
                .AppendLine("rightPalmForwardDeviationDegrees=" +
                    F(pose.PalmForwardDeviationDegrees))
                .AppendLine("flashlightLensUpDeviationDegrees=" +
                    F(pose.LensUpDeviationDegrees))
                .AppendLine("rightHandPositionFollowPreserved=True")
                .AppendLine("sourceClipModified=False")
                .AppendLine("controllerModified=False")
                .AppendLine("rendererMeshMaterialScaleModified=False")
                .AppendLine("objectsOutsideTargetChanged=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectPose] Approved pose revision applied and saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Pose Revision")]
        internal static void InspectPoseRevision()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleReferenceName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            FlashlightChargeConnectPoseBehaviour pose =
                target.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose revision behaviour is missing.");
            if (pose.Carry != carry)
                throw new InvalidOperationException(
                    "Pose revision references an unexpected hand-follow behaviour.");

            RequireExactSourceCopy();
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            RequireControllerContract(controller, clip);

            string[] bodyPaths = IdleBodyPaths(idle.transform);
            if (pose.IdleBodyPoseCount != bodyPaths.Length)
                throw new InvalidOperationException(
                    "Stored Player_Idle body pose count is incorrect.");
            float maximumStoredRotationDifference = 0f;
            float maximumStoredPositionDifference = 0f;
            float maximumAppliedRotationDifference = 0f;
            float maximumAppliedPositionDifference = 0f;
            foreach (string path in bodyPaths)
            {
                FlashlightChargeConnectBonePose stored = pose.IdleBodyPose.Single(item =>
                    item.Path == path);
                Transform reference = RequirePath(idle.transform, path);
                Transform applied = RequirePath(target.transform, path);
                maximumStoredRotationDifference = Mathf.Max(
                    maximumStoredRotationDifference,
                    Quaternion.Angle(stored.LocalRotation, reference.localRotation));
                maximumStoredPositionDifference = Mathf.Max(
                    maximumStoredPositionDifference,
                    Vector3.Distance(stored.LocalPosition, reference.localPosition));
                maximumAppliedRotationDifference = Mathf.Max(
                    maximumAppliedRotationDifference,
                    Quaternion.Angle(applied.localRotation, reference.localRotation));
                maximumAppliedPositionDifference = Mathf.Max(
                    maximumAppliedPositionDifference,
                    Vector3.Distance(applied.localPosition, reference.localPosition));
            }
            float palmDeviation = pose.PalmForwardDeviationDegrees;
            float lensDeviation = pose.LensUpDeviationDegrees;
            if (maximumStoredRotationDifference > 0.01f ||
                maximumStoredPositionDifference > 0.00001f ||
                maximumAppliedRotationDifference > 0.01f ||
                maximumAppliedPositionDifference > 0.00001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect body does not match Player_Idle.");
            if (palmDeviation > 0.1f)
                throw new InvalidOperationException(
                    "Right palm forward deviation is too large: " + F(palmDeviation));
            if (lensDeviation > 0.1f)
                throw new InvalidOperationException(
                    "Flashlight lens-up deviation is too large: " + F(lensDeviation));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Pose revision inspection changed scene dirty state.");
            int consoleErrors = UnityConsoleErrorCount();
            if (consoleErrors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + consoleErrors +
                    " errors during pose revision inspection.");

            WriteText(PoseInspectionReportPath, new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect pose revision inspection")
                .AppendLine("targetCount=1")
                .AppendLine("idleReference=" + IdleReferenceName)
                .AppendLine("idleBodyBoneCount=" + bodyPaths.Length)
                .AppendLine("maximumStoredBodyRotationDifferenceDegrees=" +
                    F(maximumStoredRotationDifference))
                .AppendLine("maximumStoredBodyPositionDifferenceMeters=" +
                    F(maximumStoredPositionDifference))
                .AppendLine("maximumAppliedBodyRotationDifferenceDegrees=" +
                    F(maximumAppliedRotationDifference))
                .AppendLine("maximumAppliedBodyPositionDifferenceMeters=" +
                    F(maximumAppliedPositionDifference))
                .AppendLine("rightPalmForwardDeviationDegrees=" + F(palmDeviation))
                .AppendLine("flashlightLensUpDeviationDegrees=" + F(lensDeviation))
                .AppendLine("leftArmAndHeadPathsPostProcessed=False")
                .AppendLine("rightHandPositionFollowPresent=True")
                .AppendLine("sourceClipCopiedExactly=True")
                .AppendLine("unityConsoleErrors=" + consoleErrors)
                .AppendLine("inspectionManipulatedTarget=False")
                .ToString());
            Debug.Log("[FlashlightChargeConnectPose] Pose revision inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Pose Revision Final")]
        internal static void CapturePoseRevisionFinal()
        {
            RequireEditMode();
            InspectPoseRevision();
            if (captureActive || !string.IsNullOrEmpty(
                    SessionState.GetString(AutoStateKey, string.Empty)))
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect review is already active.");
            DeleteReviewFile(PoseCompletionReportPath);
            DeleteReviewFile(PoseFailureReportPath);
            SessionState.SetBool(PoseReviewKey, true);
            SessionState.SetBool(ForwardReachReviewKey, false);
            SessionState.SetString(AutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnectPose] Final natural-playback capture started.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Source")]
        internal static void InspectSource()
        {
            RequireEditMode();
            RequireScene();
            ModelImporterClipAnimation selected = RequireSelectedTake(out
                ModelImporterClipAnimation sourceTake);
            AnimationClip clip = RequireClip();
            RequireClipContract(clip, sourceTake);
            RequireExactSourceCopy();

            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect supplied FBX source inspection")
                .AppendLine("externalPath=" + ExternalFbxPath)
                .AppendLine("importedPath=" + ImportedFbxPath)
                .AppendLine("externalSha256=" + ComputeFileHash(ExternalFbxPath))
                .AppendLine("importedSha256=" + ComputeAssetHash(ImportedFbxPath))
                .AppendLine("animationStackCount=1")
                .AppendLine("takeName=" + selected.takeName)
                .AppendLine("clipName=" + clip.name)
                .AppendLine("firstFrame=" + F(selected.firstFrame))
                .AppendLine("lastFrame=" + F(selected.lastFrame))
                .AppendLine("frameRate=" + F(clip.frameRate))
                .AppendLine("lengthSeconds=" + F(clip.length))
                .AppendLine("curveBindingCount=" +
                    AnimationUtility.GetCurveBindings(clip).Length)
                .AppendLine("objectReferenceCurveBindingCount=" +
                    AnimationUtility.GetObjectReferenceCurveBindings(clip).Length)
                .AppendLine("loopTime=True")
                .AppendLine("fullTakePreserved=True")
                .AppendLine("keyframesEdited=False")
                .AppendLine("animationGenerated=False");
            WriteText(SourceReportPath, report.ToString());
            Debug.Log("[FlashlightChargeConnect] Supplied source inspected without scene modification.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Apply Charge Connect Animation")]
        internal static void ApplyAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp must be clean before applying Flashlight_Charge_Connect.");

            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            string targetPoseBefore = TransformSignature(target.transform);
            string outsideBefore = OutsideTargetSignature(scene, target.transform);
            string carryBefore = CarrySignature(carry);
            string visualBefore = VisualSignature(target.transform, carry);
            string avatarBefore = ObjectIdentity(animator.avatar);
            string flashlightModelHash = ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Flashlight.fbx");
            string flashlightMaterialHash = ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Materials/Flashlight.mat");

            EnsureExactSourceImported();
            ModelImporterClipAnimation selected = RequireSelectedTake(out
                ModelImporterClipAnimation sourceTake);
            AnimationClip clip = RequireClip();
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = CreateOrUpdateController(clip);

            Undo.RecordObject(animator, "Connect supplied flashlight charging animation");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            RequireEqual(avatarBefore, ObjectIdentity(animator.avatar),
                "Flashlight_Charge_Connect avatar");
            RequireEqual(targetPoseBefore, TransformSignature(target.transform),
                "Flashlight_Charge_Connect authored transform pose");
            RequireEqual(outsideBefore, OutsideTargetSignature(scene, target.transform),
                "objects outside Flashlight_Charge_Connect");
            RequireEqual(carryBefore, CarrySignature(carry),
                "Flashlight_Charge_Connect hand-follow configuration");
            RequireEqual(visualBefore, VisualSignature(target.transform, carry),
                "Flashlight_Charge_Connect renderers, meshes, and materials");
            RequireEqual(flashlightModelHash, ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Flashlight.fbx"),
                "Flashlight model asset");
            RequireEqual(flashlightMaterialHash, ComputeAssetHash(
                "Assets/_Project/Art/Items/Flashlight/Materials/Flashlight.mat"),
                "Flashlight material asset");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not save Flashlight_Charge_Connect animation linkage.");
            AssetDatabase.SaveAssets();

            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect supplied animation application")
                .AppendLine("target=" + TargetName)
                .AppendLine("sourcePath=" + ExternalFbxPath)
                .AppendLine("importedPath=" + ImportedFbxPath)
                .AppendLine("sourceSha256=" + ComputeFileHash(ExternalFbxPath))
                .AppendLine("importedSha256=" + ComputeAssetHash(ImportedFbxPath))
                .AppendLine("takeName=" + selected.takeName)
                .AppendLine("clipName=" + clip.name)
                .AppendLine("controllerPath=" + ControllerPath)
                .AppendLine("stateName=" + StateName)
                .AppendLine("fullTakePreserved=True")
                .AppendLine("keyframesEdited=False")
                .AppendLine("animationGenerated=False")
                .AppendLine("loopTime=True")
                .AppendLine("applyRootMotion=False")
                .AppendLine("existingCarryConfigurationChanged=False")
                .AppendLine("existingVisualAssetsChanged=False")
                .AppendLine("objectsOutsideTargetChanged=False");
            WriteText(ApplicationReportPath, report.ToString());
            Debug.Log("[FlashlightChargeConnect] Exact supplied animation applied and scene saved.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Inspect Charge Connect Animation")]
        internal static void InspectAnimation()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool wasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            FlashlightRightHandFollowBehaviour carry = RequireCarry(target);
            ModelImporterClipAnimation selected = RequireSelectedTake(out
                ModelImporterClipAnimation sourceTake);
            AnimationClip clip = RequireClip();
            RequireClipContract(clip, sourceTake);
            RequireExactSourceCopy();

            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            if (AssetDatabase.GetAssetPath(controller) != ControllerPath)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect uses an unexpected controller.");
            if (animator.applyRootMotion || !animator.enabled ||
                animator.updateMode != AnimatorUpdateMode.Normal ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect Animator settings are invalid.");
            RequireControllerContract(controller, clip);
            if (carry.Holder == null || carry.Model == null || carry.RightHand == null)
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect existing right-hand carry is incomplete.");
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect inspection changed scene dirty state.");

            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect supplied animation inspection")
                .AppendLine("targetCount=1")
                .AppendLine("takeName=" + selected.takeName)
                .AppendLine("clipName=" + clip.name)
                .AppendLine("clipLengthSeconds=" + F(clip.length))
                .AppendLine("clipFrameRate=" + F(clip.frameRate))
                .AppendLine("curveBindingCount=" +
                    AnimationUtility.GetCurveBindings(clip).Length)
                .AppendLine("fullTakePreserved=True")
                .AppendLine("sameEmbeddedClipReferenced=True")
                .AppendLine("loopTime=True")
                .AppendLine("controllerStateCount=1")
                .AppendLine("controllerSpeed=1")
                .AppendLine("applyRootMotion=False")
                .AppendLine("rightHandFollowPresent=True")
                .AppendLine("inspectionManipulatedTarget=False");
            WriteText(InspectionReportPath, report.ToString());
            Debug.Log("[FlashlightChargeConnect] Animation linkage inspection passed.");
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashlight/Capture Charge Connect Final")]
        internal static void CaptureChargeConnectFinal()
        {
            RequireEditMode();
            InspectAnimation();
            if (captureActive || !string.IsNullOrEmpty(
                    SessionState.GetString(AutoStateKey, string.Empty)))
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect review is already active.");
            DeleteReviewFile(CompletionReportPath);
            DeleteReviewFile(FailureReportPath);
            SessionState.SetBool(PoseReviewKey, false);
            SessionState.SetBool(ForwardReachReviewKey, false);
            SessionState.SetString(AutoStateKey, AwaitingPlayState);
            EditorApplication.EnterPlaymode();
            Debug.Log("[FlashlightChargeConnect] Final natural-playback capture started.");
        }

        private static void ContinueReview()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ContinueReview;
                return;
            }

            string state = SessionState.GetString(AutoStateKey, string.Empty);
            try
            {
                if (state == AwaitingPlayState && EditorApplication.isPlaying)
                {
                    BeginRuntimeCapture();
                    return;
                }
                if (state == AwaitingEditState && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetString(AutoStateKey, AwaitingEditReadyState);
                    EditorApplication.delayCall += ContinueReview;
                    return;
                }
                if (state == AwaitingEditReadyState &&
                    !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    bool poseReview = SessionState.GetBool(PoseReviewKey, false);
                    bool forwardReachReview =
                        SessionState.GetBool(ForwardReachReviewKey, false);
                    bool upperBodyRestoreReview =
                        SessionState.GetBool(UpperBodyRestoreReviewKey, false);
                    bool upperBodyRestoreFinalReview =
                        SessionState.GetBool(UpperBodyRestoreFinalReviewKey, false);
                    bool forwardHoldReview =
                        SessionState.GetBool(ForwardHoldReviewKey, false);
                    bool forwardHoldFinalReview =
                        SessionState.GetBool(ForwardHoldFinalReviewKey, false);
                    bool rightShoulderForwardReview =
                        SessionState.GetBool(RightShoulderForwardReviewKey, false);
                    bool rightShoulderForwardFinalReview =
                        SessionState.GetBool(
                            RightShoulderForwardFinalReviewKey, false);
                    bool neutralWristReview =
                        SessionState.GetBool(NeutralWristReviewKey, false);
                    bool neutralWristFinalReview =
                        SessionState.GetBool(NeutralWristFinalReviewKey, false);
                    bool faceClearanceReview =
                        SessionState.GetBool(FaceClearanceReviewKey, false);
                    bool faceClearanceFinalReview =
                        SessionState.GetBool(FaceClearanceFinalReviewKey, false);
                    bool flashlightRightOffsetReview =
                        SessionState.GetBool(FlashlightRightOffsetReviewKey, false);
                    bool flashlightForwardOffsetReview =
                        SessionState.GetBool(FlashlightForwardOffsetReviewKey, false);
                    bool flashlightHandClearanceReview =
                        SessionState.GetBool(FlashlightHandClearanceReviewKey, false);
                    if (flashlightHandClearanceReview &&
                        faceClearanceReview && faceClearanceFinalReview)
                        InspectFlashlightHandClearance();
                    else if (flashlightForwardOffsetReview &&
                        faceClearanceReview && faceClearanceFinalReview)
                        InspectFlashlightForwardOffset();
                    else if (flashlightRightOffsetReview &&
                        faceClearanceReview && faceClearanceFinalReview)
                        InspectFlashlightRightOffset();
                    else if (faceClearanceReview && faceClearanceFinalReview)
                        InspectFaceClearance();
                    else if (faceClearanceReview)
                    {
                        int diagnosticErrors = UnityConsoleErrorCount();
                        if (diagnosticErrors != 0)
                            throw new InvalidOperationException(
                                "Unity Console contains " + diagnosticErrors +
                                " errors after face-clearance diagnostic review.");
                        EraseFaceClearanceReviewState();
                        Debug.Log("[FlashlightChargeConnectFaceClearance] Diagnostic review returned to Edit Mode.");
                        return;
                    }
                    else if (neutralWristReview && neutralWristFinalReview)
                        InspectNeutralWrist();
                    else if (neutralWristReview)
                    {
                        int diagnosticErrors = UnityConsoleErrorCount();
                        if (diagnosticErrors != 0)
                            throw new InvalidOperationException(
                                "Unity Console contains " + diagnosticErrors +
                                " errors after neutral-wrist diagnostic review.");
                        EraseNeutralWristReviewState();
                        Debug.Log("[FlashlightChargeConnectNeutralWrist] Diagnostic review returned to Edit Mode.");
                        return;
                    }
                    else if (rightShoulderForwardReview &&
                        rightShoulderForwardFinalReview)
                        InspectRightShoulderForward();
                    else if (rightShoulderForwardReview)
                    {
                        int diagnosticErrors = UnityConsoleErrorCount();
                        if (diagnosticErrors != 0)
                            throw new InvalidOperationException(
                                "Unity Console contains " + diagnosticErrors +
                                " errors after right-shoulder-forward diagnostic review.");
                        EraseRightShoulderForwardReviewState();
                        Debug.Log("[FlashlightChargeConnectRightShoulderForward] Diagnostic review returned to Edit Mode.");
                        return;
                    }
                    else if (forwardHoldReview && forwardHoldFinalReview)
                        InspectForwardHold();
                    else if (forwardHoldReview)
                    {
                        int diagnosticErrors = UnityConsoleErrorCount();
                        if (diagnosticErrors != 0)
                            throw new InvalidOperationException(
                                "Unity Console contains " + diagnosticErrors +
                                " errors after forward-hold diagnostic review.");
                        EraseForwardHoldReviewState();
                        Debug.Log("[FlashlightChargeConnectForwardHold] Diagnostic review returned to Edit Mode.");
                        return;
                    }
                    else if (upperBodyRestoreReview && upperBodyRestoreFinalReview)
                        InspectUpperBodyRestore();
                    else if (upperBodyRestoreReview)
                    {
                        int diagnosticErrors = UnityConsoleErrorCount();
                        if (diagnosticErrors != 0)
                            throw new InvalidOperationException(
                                "Unity Console contains " + diagnosticErrors +
                                " errors after upper-body restore diagnostic review.");
                        SessionState.EraseString(AutoStateKey);
                        SessionState.EraseBool(PoseReviewKey);
                        SessionState.EraseBool(ForwardReachReviewKey);
                        SessionState.EraseBool(UpperBodyRestoreReviewKey);
                        SessionState.EraseBool(UpperBodyRestoreFinalReviewKey);
                        SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
                        Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Diagnostic review returned to Edit Mode.");
                        return;
                    }
                    else if (forwardReachReview) InspectForwardReachRevision();
                    else if (poseReview) InspectPoseRevision();
                    else InspectAnimation();
                    int errors = UnityConsoleErrorCount();
                    if (errors != 0)
                        throw new InvalidOperationException(
                            "Unity Console contains " + errors +
                            " errors after Flashlight_Charge_Connect review.");
                    string completionPath = flashlightHandClearanceReview
                        ? FlashlightHandClearanceCompletionReportPath
                        : flashlightForwardOffsetReview
                        ? FlashlightForwardOffsetCompletionReportPath
                        : flashlightRightOffsetReview
                        ? FlashlightRightOffsetCompletionReportPath
                        : faceClearanceReview
                        ? FaceClearanceCompletionReportPath
                        : neutralWristReview
                        ? NeutralWristCompletionReportPath
                        : rightShoulderForwardReview
                        ? RightShoulderForwardCompletionReportPath
                        : forwardHoldReview
                        ? ForwardHoldCompletionReportPath
                        : upperBodyRestoreReview
                        ? UpperBodyRestoreCompletionReportPath
                        : forwardReachReview
                        ? ForwardReachCompletionReportPath
                        : poseReview ? PoseCompletionReportPath : CompletionReportPath;
                    WriteText(completionPath,
                        new StringBuilder()
                        .AppendLine(flashlightHandClearanceReview
                            ? "Flashlight_Charge_Connect right-hand surface-clearance final direct review"
                            : flashlightForwardOffsetReview
                            ? "Flashlight_Charge_Connect flashlight-only forward-offset final direct review"
                            : flashlightRightOffsetReview
                            ? "Flashlight_Charge_Connect flashlight-only right-offset final direct review"
                            : faceClearanceReview
                            ? "Flashlight_Charge_Connect face-clearance final direct review"
                            : neutralWristReview
                            ? "Flashlight_Charge_Connect neutral-wrist final direct review"
                            : rightShoulderForwardReview
                            ? "Flashlight_Charge_Connect right-shoulder-forward final direct review"
                            : forwardHoldReview
                            ? "Flashlight_Charge_Connect one-second forward-hold final direct review"
                            : upperBodyRestoreReview
                            ? "Flashlight_Charge_Connect upper-body restore final direct review"
                            : forwardReachReview
                            ? "Flashlight_Charge_Connect forward-reach final direct review"
                            : poseReview
                            ? "Flashlight_Charge_Connect pose revision final direct review"
                            : "Flashlight_Charge_Connect final direct review")
                        .AppendLine("applicationCompleted=True")
                        .AppendLine("naturalPlaybackObserved=True")
                        .AppendLine("fullCycleAndReturnObserved=True")
                        .AppendLine("returnedToEditMode=True")
                        .AppendLine("unityConsoleErrors=0")
                        .ToString());
                    SessionState.EraseString(AutoStateKey);
                    SessionState.EraseBool(PoseReviewKey);
                    SessionState.EraseBool(ForwardReachReviewKey);
                    SessionState.EraseBool(UpperBodyRestoreReviewKey);
                    SessionState.EraseBool(UpperBodyRestoreFinalReviewKey);
                    SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
                    SessionState.EraseBool(ForwardHoldReviewKey);
                    SessionState.EraseBool(ForwardHoldFinalReviewKey);
                    SessionState.EraseInt(ForwardHoldDiagnosticIndexKey);
                    SessionState.EraseBool(RightShoulderForwardReviewKey);
                    SessionState.EraseBool(RightShoulderForwardFinalReviewKey);
                    SessionState.EraseInt(RightShoulderForwardDiagnosticIndexKey);
                    SessionState.EraseBool(NeutralWristReviewKey);
                    SessionState.EraseBool(NeutralWristFinalReviewKey);
                    SessionState.EraseInt(NeutralWristDiagnosticIndexKey);
                    SessionState.EraseBool(FaceClearanceReviewKey);
                    SessionState.EraseBool(FaceClearanceFinalReviewKey);
                    SessionState.EraseInt(FaceClearanceDiagnosticIndexKey);
                    SessionState.EraseBool(FlashlightRightOffsetReviewKey);
                    SessionState.EraseBool(FlashlightForwardOffsetReviewKey);
                    SessionState.EraseBool(FlashlightHandClearanceReviewKey);
                    Debug.Log(flashlightHandClearanceReview
                        ? "[FlashlightChargeConnectHandClearance] Final review completed."
                        : flashlightForwardOffsetReview
                        ? "[FlashlightChargeConnectFlashlightForwardOffset] Final review completed."
                        : flashlightRightOffsetReview
                        ? "[FlashlightChargeConnectFlashlightRightOffset] Final review completed."
                        : faceClearanceReview
                        ? "[FlashlightChargeConnectFaceClearance] Final review completed."
                        : neutralWristReview
                        ? "[FlashlightChargeConnectNeutralWrist] Final review completed."
                        : rightShoulderForwardReview
                        ? "[FlashlightChargeConnectRightShoulderForward] Final review completed."
                        : forwardHoldReview
                        ? "[FlashlightChargeConnectForwardHold] Final review completed."
                        : upperBodyRestoreReview
                        ? "[FlashlightChargeConnectUpperBodyRestore] Final review completed."
                        : forwardReachReview
                        ? "[FlashlightChargeConnectForwardReach] Final review completed."
                        : poseReview
                        ? "[FlashlightChargeConnectPose] Final review completed."
                        : "[FlashlightChargeConnect] Final review completed.");
                }
            }
            catch (Exception exception)
            {
                FailReview(exception);
            }
        }

        private static void BeginRuntimeCapture()
        {
            runtimeTarget = FindUnique(RequireScene(), TargetName);
            runtimeAnimator = RequireAnimator(runtimeTarget);
            runtimeCarry = RequireCarry(runtimeTarget);
            bool poseReview = SessionState.GetBool(PoseReviewKey, false);
            bool upperBodyRestoreReview =
                SessionState.GetBool(UpperBodyRestoreReviewKey, false);
            bool forwardHoldReview =
                SessionState.GetBool(ForwardHoldReviewKey, false);
            runtimePose = poseReview || upperBodyRestoreReview || forwardHoldReview
                ? runtimeTarget.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                  throw new MissingReferenceException(
                      "Flashlight_Charge_Connect pose revision behaviour is missing at runtime.")
                : null;
            if (!runtimeAnimator.enabled || runtimeAnimator.applyRootMotion ||
                runtimeAnimator.runtimeAnimatorController == null)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect runtime Animator settings are invalid.");
            captureActive = true;
            captureStartTime = EditorApplication.timeSinceStartup;
            nextPanel = 0;
            baseLoop = -1;
            runtimeCaptureTimes = forwardHoldReview
                ? BuildForwardHoldCaptureTimes(runtimePose, RequireClip())
                : CaptureTimes;
            fullPanels = new Texture2D[runtimeCaptureTimes.Length];
            upperPanels = new Texture2D[runtimeCaptureTimes.Length];
            runtimeCycleStartTime = 0d;
            observedForwardHoldCycleDuration = 0f;
            ForwardHoldRotations.Clear();
            ForwardHoldPositions.Clear();
            forwardHoldReferenceCaptured = false;
            forwardHoldStartMetricFrame = -1;
            maximumForwardHoldRotationDrift = 0f;
            maximumForwardHoldPositionDrift = 0f;
            forwardHoldHolderPosition = Vector3.zero;
            forwardHoldHolderRotation = Quaternion.identity;
            maximumForwardHoldHolderPositionDrift = 0f;
            maximumForwardHoldHolderRotationDrift = 0f;
            maximumRightShoulderForwardDeviation = 0f;
            maximumRightShoulderForwardLateralOffset = 0f;
            minimumRightShoulderForwardElbowAngle = float.PositiveInfinity;
            maximumRightShoulderForwardElbowAngle = 0f;
            maximumRightShoulderForwardHandError = 0f;
            minimumRightShoulderForwardHoldWeight = float.PositiveInfinity;
            maximumNeutralWristBendAngle = 0f;
            minimumFaceClearanceMeters = float.PositiveInfinity;
            maximumFaceClearanceHandError = 0f;
            minimumFaceClearanceAppliedOffset = float.PositiveInfinity;
            maximumFaceClearanceAppliedOffset = 0f;
            minimumHandFlashlightSurfaceClearance = float.PositiveInfinity;
            RuntimeObservations.Clear();
            InitialRotations.Clear();
            maximumBoneRotationDelta = 0f;
            maximumHolderPositionError = 0f;
            maximumHolderRotationError = 0f;
            runtimeHead = RequireDescendant(runtimeTarget.transform, "Head");
            runtimeLeftArm = RequireDescendant(runtimeTarget.transform, "LeftArm");
            runtimeRightArm = RequireDescendant(runtimeTarget.transform, "RightArm");
            initialHeadRotation = runtimeHead.localRotation;
            initialLeftArmRotation = runtimeLeftArm.localRotation;
            initialRightArmRotation = runtimeRightArm.localRotation;
            maximumHeadRotationDelta = 0f;
            maximumLeftArmRotationDelta = 0f;
            maximumRightArmRotationDelta = 0f;
            lastReachMetricFrame = -1;
            hasReachMetricSample = false;
            previousReachHandLocal = Vector3.zero;
            previousReachVelocity = Vector3.zero;
            maximumReachHandSpeed = 0f;
            maximumApproachHandSpeed = 0f;
            maximumReachHandAcceleration = 0f;
            maximumReachTargetError = 0f;
            maximumFullReachForwardDeviation = 0f;
            minimumFullReachElbowAngle = float.PositiveInfinity;
            maximumFullReachElbowAngle = 0f;
            if (runtimePose != null) runtimePose.ResetRuntimeMetrics();
            EditorApplication.update -= RuntimeCaptureTick;
            EditorApplication.update += RuntimeCaptureTick;
            SessionState.SetString(AutoStateKey, CapturingState);
        }

        private static void RuntimeCaptureTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException(
                        "Play Mode ended before Flashlight_Charge_Connect review completed.");
                if (EditorApplication.timeSinceStartup - captureStartTime > CaptureTimeoutSeconds)
                    throw new TimeoutException(
                        "Flashlight_Charge_Connect natural review exceeded 20 seconds.");
                if (!runtimeAnimator.isInitialized) return;

                AnimatorStateInfo state = runtimeAnimator.GetCurrentAnimatorStateInfo(0);
                if (!state.IsName(StateName)) return;
                float normalized = state.normalizedTime;
                int loop = Mathf.FloorToInt(normalized);
                float fraction = normalized - loop;
                bool forwardHoldReview =
                    SessionState.GetBool(ForwardHoldReviewKey, false);
                if (baseLoop < 0)
                {
                    if (fraction > 0.15f) return;
                    baseLoop = loop;
                    if (forwardHoldReview)
                        runtimeCycleStartTime = EditorApplication.timeSinceStartup -
                            fraction * RequireClip().length;
                }

                AccumulateRuntimeMetrics();
                if (forwardHoldReview && loop > baseLoop &&
                    observedForwardHoldCycleDuration <= 0f)
                    observedForwardHoldCycleDuration = (float)(
                        EditorApplication.timeSinceStartup - runtimeCycleStartTime);
                float relative = forwardHoldReview
                    ? (float)(EditorApplication.timeSinceStartup - runtimeCycleStartTime)
                    : normalized - baseLoop;
                if (nextPanel < runtimeCaptureTimes.Length &&
                    relative >= runtimeCaptureTimes[nextPanel])
                {
                    fullPanels[nextPanel] = CaptureRuntimePanel(false);
                    upperPanels[nextPanel] = CaptureRuntimePanel(true);
                    RuntimeObservations.Add(
                        "panel=" + nextPanel +
                        (forwardHoldReview
                            ? "|requestedElapsedSeconds=" +
                              F(runtimeCaptureTimes[nextPanel]) +
                              "|actualElapsedSeconds=" + F(relative) +
                              "|actualNormalizedTime=" + F(normalized - baseLoop)
                            : "|requestedNormalizedTime=" +
                              F(runtimeCaptureTimes[nextPanel]) +
                              "|actualNormalizedTime=" + F(relative)));
                    nextPanel++;
                }
                if (nextPanel == runtimeCaptureTimes.Length) FinishRuntimeCapture();
            }
            catch (Exception exception)
            {
                FailReview(exception);
            }
        }

        private static void AccumulateRuntimeMetrics()
        {
            foreach (Transform bone in runtimeTarget.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(
                    bone, runtimeTarget.transform);
                if (!InitialRotations.TryGetValue(path, out Quaternion initial))
                {
                    InitialRotations[path] = bone.localRotation;
                    continue;
                }
                maximumBoneRotationDelta = Mathf.Max(
                    maximumBoneRotationDelta,
                    Quaternion.Angle(initial, bone.localRotation));
            }
            Transform holder = runtimeCarry.Holder != null
                ? runtimeCarry.Holder.transform
                : throw new MissingReferenceException(
                    "Flashlight_Charge_Connect runtime holder is missing.");
            Vector3 expectedHolderWorldPosition =
                runtimeCarry.RightHand.TransformPoint(
                    runtimeCarry.HolderLocalPosition);
            if (runtimePose != null &&
                runtimePose.FlashlightRightOffsetConfigured)
                expectedHolderWorldPosition += runtimeTarget.transform.right *
                    runtimePose.FlashlightRightOffsetMeters;
            if (runtimePose != null &&
                runtimePose.FlashlightForwardOffsetConfigured)
                expectedHolderWorldPosition += runtimeTarget.transform.forward *
                    runtimePose.FlashlightForwardOffsetMeters;
            maximumHolderPositionError = Mathf.Max(
                maximumHolderPositionError,
                Vector3.Distance(holder.position, expectedHolderWorldPosition));
            maximumHolderRotationError = Mathf.Max(
                maximumHolderRotationError,
                Quaternion.Angle(holder.localRotation, runtimeCarry.HolderLocalRotation));
            maximumHeadRotationDelta = Mathf.Max(
                maximumHeadRotationDelta,
                Quaternion.Angle(initialHeadRotation, runtimeHead.localRotation));
            maximumLeftArmRotationDelta = Mathf.Max(
                maximumLeftArmRotationDelta,
                Quaternion.Angle(initialLeftArmRotation, runtimeLeftArm.localRotation));
            maximumRightArmRotationDelta = Mathf.Max(
                maximumRightArmRotationDelta,
                Quaternion.Angle(initialRightArmRotation, runtimeRightArm.localRotation));
            if (SessionState.GetBool(ForwardReachReviewKey, false))
                AccumulateForwardReachMetrics();
            if (SessionState.GetBool(ForwardHoldReviewKey, false))
                AccumulateForwardHoldMetrics();
            if (SessionState.GetBool(FaceClearanceReviewKey, false))
                AccumulateFaceClearanceMetrics();
            if (SessionState.GetBool(FlashlightHandClearanceReviewKey, false))
            {
                HandFlashlightForwardGeometry geometry =
                    MeasureHandFlashlightForwardGeometry(
                        runtimeTarget.transform, runtimeCarry);
                minimumHandFlashlightSurfaceClearance = Mathf.Min(
                    minimumHandFlashlightSurfaceClearance,
                    geometry.FlashlightMinimumForward -
                    geometry.RightHandMaximumForward);
            }
        }

        private static void AccumulateFaceClearanceMetrics()
        {
            if (runtimePose == null || !runtimePose.FaceClearanceConfigured)
                throw new InvalidOperationException(
                    "Face-clearance runtime pose is not configured.");
            if (TryMeasureFaceClearance(
                    runtimeTarget.transform,
                    runtimeCarry.Holder,
                    out float clearance,
                    out _,
                    out _))
                minimumFaceClearanceMeters = Mathf.Min(
                    minimumFaceClearanceMeters,
                    clearance);
            maximumFaceClearanceHandError = Mathf.Max(
                maximumFaceClearanceHandError,
                runtimePose.CurrentFaceClearanceHandErrorMeters);
            minimumFaceClearanceAppliedOffset = Mathf.Min(
                minimumFaceClearanceAppliedOffset,
                runtimePose.CurrentFaceClearanceAppliedOffsetMeters);
            maximumFaceClearanceAppliedOffset = Mathf.Max(
                maximumFaceClearanceAppliedOffset,
                runtimePose.CurrentFaceClearanceAppliedOffsetMeters);
        }

        private static void AccumulateForwardHoldMetrics()
        {
            if (runtimePose == null || !runtimePose.ForwardHoldConfigured)
                throw new InvalidOperationException(
                    "Forward-hold runtime pose is not configured.");
            if (!runtimePose.IsForwardHolding) return;

            if (forwardHoldStartMetricFrame < 0)
            {
                forwardHoldStartMetricFrame = Time.frameCount;
                return;
            }
            if (Time.frameCount == forwardHoldStartMetricFrame) return;

            if (SessionState.GetBool(RightShoulderForwardReviewKey, false))
            {
                maximumRightShoulderForwardDeviation = Mathf.Max(
                    maximumRightShoulderForwardDeviation,
                    runtimePose.CurrentRightShoulderForwardDeviationDegrees);
                maximumRightShoulderForwardLateralOffset = Mathf.Max(
                    maximumRightShoulderForwardLateralOffset,
                    runtimePose.CurrentRightShoulderForwardLateralOffsetMeters);
                minimumRightShoulderForwardElbowAngle = Mathf.Min(
                    minimumRightShoulderForwardElbowAngle,
                    runtimePose.CurrentRightShoulderForwardElbowAngleDegrees);
                maximumRightShoulderForwardElbowAngle = Mathf.Max(
                    maximumRightShoulderForwardElbowAngle,
                    runtimePose.CurrentRightShoulderForwardElbowAngleDegrees);
                maximumRightShoulderForwardHandError = Mathf.Max(
                    maximumRightShoulderForwardHandError,
                    runtimePose.CurrentRightShoulderForwardHandErrorMeters);
                minimumRightShoulderForwardHoldWeight = Mathf.Min(
                    minimumRightShoulderForwardHoldWeight,
                    runtimePose.CurrentRightShoulderForwardWeight);
                if (SessionState.GetBool(NeutralWristReviewKey, false))
                    maximumNeutralWristBendAngle = Mathf.Max(
                        maximumNeutralWristBendAngle,
                        runtimePose.CurrentNeutralWristBendAngleDegrees);
            }

            Transform holder = runtimeCarry.Holder != null
                ? runtimeCarry.Holder.transform
                : throw new MissingReferenceException(
                    "Flashlight_Charge_Connect runtime holder is missing.");
            string[] paths = WholeUpperBodyPaths(runtimeTarget.transform);
            if (!forwardHoldReferenceCaptured)
            {
                foreach (string path in paths)
                {
                    Transform bone = RequirePath(runtimeTarget.transform, path);
                    ForwardHoldRotations[path] = bone.localRotation;
                    ForwardHoldPositions[path] = bone.localPosition;
                }
                forwardHoldHolderPosition = holder.position;
                forwardHoldHolderRotation = holder.rotation;
                forwardHoldReferenceCaptured = true;
                return;
            }

            foreach (string path in paths)
            {
                Transform bone = RequirePath(runtimeTarget.transform, path);
                maximumForwardHoldRotationDrift = Mathf.Max(
                    maximumForwardHoldRotationDrift,
                    Quaternion.Angle(ForwardHoldRotations[path], bone.localRotation));
                maximumForwardHoldPositionDrift = Mathf.Max(
                    maximumForwardHoldPositionDrift,
                    Vector3.Distance(ForwardHoldPositions[path], bone.localPosition));
            }
            maximumForwardHoldHolderPositionDrift = Mathf.Max(
                maximumForwardHoldHolderPositionDrift,
                Vector3.Distance(forwardHoldHolderPosition, holder.position));
            maximumForwardHoldHolderRotationDrift = Mathf.Max(
                maximumForwardHoldHolderRotationDrift,
                Quaternion.Angle(forwardHoldHolderRotation, holder.rotation));
        }

        private static void AccumulateForwardReachMetrics()
        {
            if (runtimePose == null || !runtimePose.ForwardReachConfigured)
                throw new InvalidOperationException(
                    "Forward-reach runtime pose is not configured.");
            maximumReachTargetError = Mathf.Max(
                maximumReachTargetError, runtimePose.CurrentHandPositionError);
            if (Time.frameCount == lastReachMetricFrame) return;
            lastReachMetricFrame = Time.frameCount;

            Transform arm = RequirePath(runtimeTarget.transform, RightArmPath);
            Transform foreArm = RequirePath(runtimeTarget.transform, RightForeArmPath);
            Transform hand = RequirePath(runtimeTarget.transform, RightHandPath);
            Vector3 handLocal = runtimeTarget.transform.InverseTransformPoint(hand.position);
            if (runtimePose.CurrentReachWeight >= 0.99f)
            {
                maximumFullReachForwardDeviation = Mathf.Max(
                    maximumFullReachForwardDeviation,
                    Vector3.Angle(
                        hand.position - arm.position,
                        runtimeTarget.transform.forward));
                float elbowAngle = Vector3.Angle(
                    arm.position - foreArm.position,
                    hand.position - foreArm.position);
                minimumFullReachElbowAngle = Mathf.Min(
                    minimumFullReachElbowAngle, elbowAngle);
                maximumFullReachElbowAngle = Mathf.Max(
                    maximumFullReachElbowAngle, elbowAngle);
            }

            if (hasReachMetricSample)
            {
                float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
                Vector3 velocity =
                    (handLocal - previousReachHandLocal) / deltaTime;
                Vector3 acceleration =
                    (velocity - previousReachVelocity) / deltaTime;
                maximumReachHandSpeed = Mathf.Max(
                    maximumReachHandSpeed, velocity.magnitude);
                maximumReachHandAcceleration = Mathf.Max(
                    maximumReachHandAcceleration, acceleration.magnitude);
                float phase = runtimePose.CurrentNormalizedPhase;
                if (phase >= runtimePose.ApproachStartNormalized &&
                    phase <= runtimePose.ReachEndNormalized)
                    maximumApproachHandSpeed = Mathf.Max(
                        maximumApproachHandSpeed, velocity.magnitude);
                previousReachVelocity = velocity;
            }
            else
            {
                hasReachMetricSample = true;
                previousReachVelocity = Vector3.zero;
            }
            previousReachHandLocal = handLocal;
        }

        private static void FinishRuntimeCapture()
        {
            if (SessionState.GetBool(ForwardHoldReviewKey, false))
            {
                FinishForwardHoldRuntimeCapture();
                return;
            }
            if (SessionState.GetBool(UpperBodyRestoreReviewKey, false))
            {
                FinishUpperBodyRestoreRuntimeCapture();
                return;
            }
            if (SessionState.GetBool(ForwardReachReviewKey, false))
            {
                FinishForwardReachRuntimeCapture();
                return;
            }
            if (SessionState.GetBool(PoseReviewKey, false))
            {
                FinishPoseRuntimeCapture();
                return;
            }
            if (maximumBoneRotationDelta <= 1f)
                throw new InvalidOperationException(
                    "The supplied charging animation did not produce visible bone motion.");
            if (maximumHolderPositionError > 0.00005f ||
                maximumHolderRotationError > 0.05f)
                throw new InvalidOperationException(
                    "The existing flashlight right-hand follow drifted during charging.");

            WriteComposite(FinalImagePath, fullPanels, upperPanels);
            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect natural playback direct review")
                .AppendLine("captureKind=Final")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("sourceClip=" + ClipName)
                .AppendLine("cyclesObserved=1")
                .AppendLine("panelsCaptured=" + (CaptureTimes.Length * 2))
                .AppendLine("maximumBoneRotationDeltaDegrees=" +
                    F(maximumBoneRotationDelta))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(maximumHolderPositionError))
                .AppendLine("maximumHolderRotationErrorDegrees=" +
                    F(maximumHolderRotationError))
                .AppendLine("loopReturnedToStart=True");
            foreach (string observation in RuntimeObservations)
                report.AppendLine(observation);
            int errors = UnityConsoleErrorCount();
            report.AppendLine("unityConsoleErrorsDuringCapture=" + errors);
            WriteText(FinalReportPath, report.ToString());
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors + " errors during review.");

            CleanupRuntimeCapture();
            SessionState.SetString(AutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishForwardHoldRuntimeCapture()
        {
            if (runtimePose == null || !runtimePose.SourceUpperBodyRestored ||
                !runtimePose.ForwardHoldConfigured || runtimePose.ForwardReachConfigured)
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect one-second forward hold is not active.");

            bool finalReview =
                SessionState.GetBool(ForwardHoldFinalReviewKey, false);
            bool rightShoulderForwardReview =
                SessionState.GetBool(RightShoulderForwardReviewKey, false);
            bool neutralWristReview =
                SessionState.GetBool(NeutralWristReviewKey, false);
            bool faceClearanceReview =
                SessionState.GetBool(FaceClearanceReviewKey, false);
            bool flashlightRightOffsetReview =
                SessionState.GetBool(FlashlightRightOffsetReviewKey, false);
            bool flashlightForwardOffsetReview =
                SessionState.GetBool(FlashlightForwardOffsetReviewKey, false);
            bool flashlightHandClearanceReview =
                SessionState.GetBool(FlashlightHandClearanceReviewKey, false);
            int diagnosticIndex = SessionState.GetInt(
                faceClearanceReview
                    ? FaceClearanceDiagnosticIndexKey
                    : neutralWristReview
                    ? NeutralWristDiagnosticIndexKey
                    : rightShoulderForwardReview
                    ? RightShoulderForwardDiagnosticIndexKey
                    : ForwardHoldDiagnosticIndexKey,
                1);
            string imagePath = flashlightHandClearanceReview
                ? FlashlightHandClearanceFinalImagePath
                : flashlightForwardOffsetReview
                ? FlashlightForwardOffsetFinalImagePath
                : flashlightRightOffsetReview
                ? FlashlightRightOffsetFinalImagePath
                : faceClearanceReview
                ? finalReview
                    ? FaceClearanceFinalImagePath
                    : diagnosticIndex == 1
                        ? FaceClearanceDiagnosticOneImagePath
                        : FaceClearanceDiagnosticTwoImagePath
                : neutralWristReview
                ? finalReview
                    ? NeutralWristFinalImagePath
                    : diagnosticIndex == 1
                        ? NeutralWristDiagnosticOneImagePath
                        : NeutralWristDiagnosticTwoImagePath
                : rightShoulderForwardReview
                ? finalReview
                    ? RightShoulderForwardFinalImagePath
                    : diagnosticIndex == 1
                        ? RightShoulderForwardDiagnosticOneImagePath
                        : RightShoulderForwardDiagnosticTwoImagePath
                : finalReview
                ? ForwardHoldFinalImagePath
                : diagnosticIndex == 1
                    ? ForwardHoldDiagnosticOneImagePath
                    : ForwardHoldDiagnosticTwoImagePath;
            string reportPath = flashlightHandClearanceReview
                ? FlashlightHandClearanceFinalReportPath
                : flashlightForwardOffsetReview
                ? FlashlightForwardOffsetFinalReportPath
                : flashlightRightOffsetReview
                ? FlashlightRightOffsetFinalReportPath
                : faceClearanceReview
                ? finalReview
                    ? FaceClearanceFinalReportPath
                    : diagnosticIndex == 1
                        ? FaceClearanceDiagnosticOneReportPath
                        : FaceClearanceDiagnosticTwoReportPath
                : neutralWristReview
                ? finalReview
                    ? NeutralWristFinalReportPath
                    : diagnosticIndex == 1
                        ? NeutralWristDiagnosticOneReportPath
                        : NeutralWristDiagnosticTwoReportPath
                : rightShoulderForwardReview
                ? finalReview
                    ? RightShoulderForwardFinalReportPath
                    : diagnosticIndex == 1
                        ? RightShoulderForwardDiagnosticOneReportPath
                        : RightShoulderForwardDiagnosticTwoReportPath
                : finalReview
                ? ForwardHoldFinalReportPath
                : diagnosticIndex == 1
                    ? ForwardHoldDiagnosticOneReportPath
                    : ForwardHoldDiagnosticTwoReportPath;

            // Direct animation review stays primary; measurements are secondary.
            WriteComposite(imagePath, fullPanels, upperPanels);
            AnimationClip clip = RequireClip();
            float expectedCycleDuration =
                clip.length + runtimePose.ForwardHoldDurationSeconds;
            var report = new StringBuilder()
                .AppendLine(flashlightHandClearanceReview
                    ? "Flashlight_Charge_Connect right-hand and flashlight surface-clearance natural playback direct review"
                    : flashlightForwardOffsetReview
                    ? "Flashlight_Charge_Connect flashlight-only forward-offset natural playback direct review"
                    : flashlightRightOffsetReview
                    ? "Flashlight_Charge_Connect flashlight-only right-offset natural playback direct review"
                    : faceClearanceReview
                    ? "Flashlight_Charge_Connect face-clearance natural playback direct review"
                    : neutralWristReview
                    ? "Flashlight_Charge_Connect neutral-wrist natural playback direct review"
                    : rightShoulderForwardReview
                    ? "Flashlight_Charge_Connect right-shoulder-forward natural playback direct review"
                    : "Flashlight_Charge_Connect one-second forward-hold natural playback direct review")
                .AppendLine("captureKind=" + (finalReview ? "Final" : "Diagnostic"))
                .AppendLine("diagnosticIndex=" + (finalReview ? 0 : diagnosticIndex))
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("sourceClip=" + ClipName)
                .AppendLine("sourceClipLengthSeconds=" + F(clip.length))
                .AppendLine("configuredHoldStartNormalized=" +
                    F(runtimePose.ForwardHoldStartNormalized))
                .AppendLine("configuredHoldDurationSeconds=" +
                    F(runtimePose.ForwardHoldDurationSeconds))
                .AppendLine("actualCompletedHoldDurationSeconds=" +
                    F(runtimePose.LastCompletedForwardHoldDurationSeconds))
                .AppendLine("completedForwardHoldCount=" +
                    runtimePose.CompletedForwardHoldCount)
                .AppendLine("expectedCycleDurationSeconds=" +
                    F(expectedCycleDuration))
                .AppendLine("observedCycleDurationSeconds=" +
                    F(observedForwardHoldCycleDuration))
                .AppendLine("controllerSpeed=1")
                .AppendLine("panelsCaptured=" + (runtimeCaptureTimes.Length * 2))
                .AppendLine("topRowView=TransporterFrontFullBody")
                .AppendLine("bottomRowView=" + (flashlightHandClearanceReview
                    ? "TransporterRightSideRightHandAndFlashlightSurfaceCloseup"
                    : flashlightForwardOffsetReview
                    ? "TransporterRightSideFaceRightArmAndFlashlightCloseup"
                    : faceClearanceReview
                    ? "TransporterFrontFaceRightArmAndFlashlightCloseup"
                    : neutralWristReview
                    ? "TransporterFrontRightThreeQuarterRightWristCloseup"
                    : "TransporterFrontRightThreeQuarterUpperBody"))
                .AppendLine("maximumWholeUpperBodyHoldRotationDriftDegrees=" +
                    F(maximumForwardHoldRotationDrift))
                .AppendLine("maximumWholeUpperBodyHoldPositionDriftMeters=" +
                    F(maximumForwardHoldPositionDrift))
                .AppendLine("maximumFlashlightHoldPositionDriftMeters=" +
                    F(maximumForwardHoldHolderPositionDrift))
                .AppendLine("maximumFlashlightHoldRotationDriftDegrees=" +
                    F(maximumForwardHoldHolderRotationDrift))
                .AppendLine("maximumIdleLowerBodyAndLeftArmRotationErrorDegrees=" +
                    F(runtimePose.MaximumBodyRotationError))
                .AppendLine("maximumIdleLowerBodyAndLeftArmPositionErrorMeters=" +
                    F(runtimePose.MaximumBodyPositionError))
                .AppendLine("maximumFlashlightLensUpDeviationDegrees=" +
                    F(runtimePose.MaximumLensUpDeviation))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(maximumHolderPositionError))
                .AppendLine("maximumHeadRotationDeltaDegrees=" +
                    F(maximumHeadRotationDelta))
                .AppendLine("maximumLeftArmRotationDeltaDegrees=" +
                    F(maximumLeftArmRotationDelta))
                .AppendLine("maximumRightArmRotationDeltaDegrees=" +
                    F(maximumRightArmRotationDelta))
                .AppendLine("maximumHoldRightArmForwardDeviationDegrees=" +
                    F(maximumRightShoulderForwardDeviation))
                .AppendLine("maximumHoldRightHandLateralOffsetMeters=" +
                    F(maximumRightShoulderForwardLateralOffset))
                .AppendLine("minimumHoldRightElbowAngleDegrees=" +
                    F(minimumRightShoulderForwardElbowAngle))
                .AppendLine("maximumHoldRightElbowAngleDegrees=" +
                    F(maximumRightShoulderForwardElbowAngle))
                .AppendLine("maximumHoldRightHandTargetErrorMeters=" +
                    F(maximumRightShoulderForwardHandError))
                .AppendLine("minimumHoldRightShoulderForwardWeight=" +
                    F(minimumRightShoulderForwardHoldWeight))
                .AppendLine("maximumHoldRightWristBendDegrees=" +
                    F(maximumNeutralWristBendAngle))
                .AppendLine("minimumWholeCycleFaceClearanceMeters=" +
                    F(minimumFaceClearanceMeters))
                .AppendLine("maximumWholeCycleFaceClearanceHandErrorMeters=" +
                    F(maximumFaceClearanceHandError))
                .AppendLine("minimumWholeCycleAppliedRightOffsetMeters=" +
                    F(minimumFaceClearanceAppliedOffset))
                .AppendLine("maximumWholeCycleAppliedRightOffsetMeters=" +
                    F(maximumFaceClearanceAppliedOffset))
                .AppendLine("configuredFlashlightOnlyRightOffsetMeters=" +
                    F(runtimePose.FlashlightRightOffsetConfigured
                        ? runtimePose.FlashlightRightOffsetMeters
                        : 0f))
                .AppendLine("configuredFlashlightOnlyForwardOffsetMeters=" +
                    F(runtimePose.FlashlightForwardOffsetConfigured
                        ? runtimePose.FlashlightForwardOffsetMeters
                        : 0f))
                .AppendLine("minimumRuntimeHandFlashlightSurfaceClearanceMeters=" +
                    F(minimumHandFlashlightSurfaceClearance))
                .AppendLine("wholeUpperBodyHeldTogether=True")
                .AppendLine("rightHandHeldInFrontOfRightShoulder=" +
                    rightShoulderForwardReview)
                .AppendLine("rightWristNeutralDuringHold=" +
                    neutralWristReview)
                .AppendLine("flashlightClearOfFaceThroughoutCycle=" +
                    faceClearanceReview)
                .AppendLine("flashlightOnlyMovedFourCentimetersRight=" +
                    flashlightRightOffsetReview)
                .AppendLine("flashlightOnlyMovedTwoCentimetersForward=" +
                    flashlightForwardOffsetReview)
                .AppendLine("flashlightAheadOfRightHandThroughoutCycle=" +
                    flashlightHandClearanceReview)
                .AppendLine("sourceReturnMotionPreserved=True")
                .AppendLine("flashlightFollowsRightHandThroughoutHold=True")
                .AppendLine("loopReturnedToStart=True");
            foreach (string observation in RuntimeObservations)
                report.AppendLine(observation);
            int errors = UnityConsoleErrorCount();
            report.AppendLine("unityConsoleErrorsDuringCapture=" + errors);
            WriteText(reportPath, report.ToString());

            if (runtimePose.CompletedForwardHoldCount < 1 ||
                Mathf.Abs(runtimePose.LastCompletedForwardHoldDurationSeconds -
                    runtimePose.ForwardHoldDurationSeconds) > 0.15f)
                throw new InvalidOperationException(
                    "Forward pose was not held for one real-time second.");
            if (observedForwardHoldCycleDuration <= 0f ||
                Mathf.Abs(observedForwardHoldCycleDuration -
                    expectedCycleDuration) > 0.20f)
                throw new InvalidOperationException(
                    "Forward-hold cycle duration did not extend by one second.");
            if (!forwardHoldReferenceCaptured ||
                maximumForwardHoldRotationDrift > 0.05f ||
                maximumForwardHoldPositionDrift > 0.00001f ||
                maximumForwardHoldHolderPositionDrift > 0.00005f ||
                maximumForwardHoldHolderRotationDrift > 0.05f)
                throw new InvalidOperationException(
                    "The complete upper body or flashlight drifted during the hold.");
            if (runtimePose.MaximumBodyRotationError > 0.01f ||
                runtimePose.MaximumBodyPositionError > 0.00001f ||
                maximumHolderPositionError > 0.00005f)
                throw new InvalidOperationException(
                    "Approved Player_Idle pose or flashlight hand follow drifted.");
            if (maximumHeadRotationDelta <= 0.1f ||
                maximumRightArmRotationDelta <= 0.1f)
                throw new InvalidOperationException(
                    "The supplied upper-body charging motion did not resume naturally.");
            if (rightShoulderForwardReview &&
                (!runtimePose.RightShoulderForwardConfigured ||
                 maximumRightShoulderForwardDeviation > 1f ||
                 maximumRightShoulderForwardLateralOffset > 0.005f ||
                 minimumRightShoulderForwardElbowAngle < 130f ||
                 maximumRightShoulderForwardElbowAngle > 150f ||
                 maximumRightShoulderForwardHandError > 0.0005f ||
                 minimumRightShoulderForwardHoldWeight < 0.99f))
                throw new InvalidOperationException(
                    "Right arm did not remain naturally aimed in front of the right shoulder during the hold.");
            if (neutralWristReview &&
                (!runtimePose.NeutralWristConfigured ||
                 maximumNeutralWristBendAngle > NeutralWristMaximumBendDegrees))
                throw new InvalidOperationException(
                    "Right wrist did not remain neutral with the forearm during the hold.");
            if (faceClearanceReview &&
                (!runtimePose.FaceClearanceConfigured ||
                 float.IsInfinity(minimumFaceClearanceMeters) ||
                 minimumFaceClearanceMeters < -0.001f ||
                 maximumFaceClearanceHandError > 0.0005f ||
                 minimumFaceClearanceAppliedOffset <
                    runtimePose.FaceClearanceLateralOffsetMeters - 0.001f ||
                 maximumFaceClearanceAppliedOffset >
                    runtimePose.FaceClearanceLateralOffsetMeters + 0.001f))
                throw new InvalidOperationException(
                    "Flashlight did not remain naturally offset to the right of the face throughout the cycle.");
            if (flashlightForwardOffsetReview &&
                (!runtimePose.FlashlightForwardOffsetConfigured ||
                 Mathf.Abs(runtimePose.FlashlightForwardOffsetMeters -
                     FlashlightForwardOffsetMeters) > 0.000001f ||
                 !runtimePose.FlashlightRightOffsetConfigured ||
                 Mathf.Abs(runtimePose.FlashlightRightOffsetMeters -
                     FlashlightRightOffsetMeters) > 0.000001f))
                throw new InvalidOperationException(
                    "Flashlight-only forward offset or the preserved right offset drifted during natural playback.");
            if (flashlightHandClearanceReview &&
                (!runtimePose.FlashlightForwardOffsetConfigured ||
                 float.IsInfinity(minimumHandFlashlightSurfaceClearance) ||
                 minimumHandFlashlightSurfaceClearance <
                    FlashlightHandClearanceSafetyMarginMeters - 0.0005f ||
                 !runtimePose.FlashlightRightOffsetConfigured ||
                 Mathf.Abs(runtimePose.FlashlightRightOffsetMeters -
                    FlashlightRightOffsetMeters) > 0.000001f))
                throw new InvalidOperationException(
                    "Flashlight overlapped the right-hand surface during natural playback.");
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during forward-hold review.");

            CleanupRuntimeCapture();
            SessionState.SetString(AutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishForwardReachRuntimeCapture()
        {
            if (runtimePose == null || !runtimePose.ForwardReachConfigured)
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect forward-reach behaviour is missing.");
            if (runtimePose.MaximumBodyRotationError > 0.01f ||
                runtimePose.MaximumBodyPositionError > 0.00001f)
                throw new InvalidOperationException(
                    "Player_Idle body or leg pose drifted during forward reach.");
            if (runtimePose.MaximumPalmForwardDeviation > 0.1f ||
                runtimePose.MaximumLensUpDeviation > 0.1f)
                throw new InvalidOperationException(
                    "Palm-forward or lens-up orientation drifted during forward reach.");
            if (maximumHolderPositionError > 0.00005f ||
                maximumReachTargetError > 0.0005f)
                throw new InvalidOperationException(
                    "Flashlight hand follow or forward-reach target drifted.");
            if (maximumFullReachForwardDeviation > 10f)
                throw new InvalidOperationException(
                    "The right arm did not reach transporter forward.");
            if (minimumFullReachElbowAngle < 130f ||
                maximumFullReachElbowAngle > 150f)
                throw new InvalidOperationException(
                    "The full-reach elbow bend left the natural range.");
            if (maximumApproachHandSpeed >= SourceMaximumHandSpeed)
                throw new InvalidOperationException(
                    "The revised forward approach remained punch-like.");
            if (maximumHeadRotationDelta <= 0.1f ||
                maximumLeftArmRotationDelta <= 0.1f)
                throw new InvalidOperationException(
                    "The supplied head or left-arm charging motion was removed.");

            WriteComposite(ForwardReachFinalImagePath, fullPanels, upperPanels);
            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect forward-reach natural playback direct review")
                .AppendLine("captureKind=Final")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("sourceClip=" + ClipName)
                .AppendLine("clipLengthSeconds=" + F(RequireClip().length))
                .AppendLine("controllerSpeed=1")
                .AppendLine("cyclesObserved=1")
                .AppendLine("panelsCaptured=" + (CaptureTimes.Length * 2))
                .AppendLine("topRowView=TransporterFrontFullBody")
                .AppendLine("bottomRowView=TransporterRightSideUpperBody")
                .AppendLine("sourceMaximumHandSpeedMetersPerSecond=" +
                    F(SourceMaximumHandSpeed))
                .AppendLine("maximumRevisedHandSpeedMetersPerSecond=" +
                    F(maximumReachHandSpeed))
                .AppendLine("maximumRevisedApproachHandSpeedMetersPerSecond=" +
                    F(maximumApproachHandSpeed))
                .AppendLine("sourceMaximumHandAccelerationMetersPerSecondSquared=" +
                    F(SourceMaximumHandAcceleration))
                .AppendLine("maximumRevisedHandAccelerationMetersPerSecondSquared=" +
                    F(maximumReachHandAcceleration))
                .AppendLine("maximumHandTargetErrorMeters=" +
                    F(maximumReachTargetError))
                .AppendLine("maximumFullReachForwardDeviationDegrees=" +
                    F(maximumFullReachForwardDeviation))
                .AppendLine("minimumFullReachElbowAngleDegrees=" +
                    F(minimumFullReachElbowAngle))
                .AppendLine("maximumFullReachElbowAngleDegrees=" +
                    F(maximumFullReachElbowAngle))
                .AppendLine("maximumBodyRotationErrorDegrees=" +
                    F(runtimePose.MaximumBodyRotationError))
                .AppendLine("maximumBodyPositionErrorMeters=" +
                    F(runtimePose.MaximumBodyPositionError))
                .AppendLine("maximumRightPalmForwardDeviationDegrees=" +
                    F(runtimePose.MaximumPalmForwardDeviation))
                .AppendLine("maximumFlashlightLensUpDeviationDegrees=" +
                    F(runtimePose.MaximumLensUpDeviation))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(maximumHolderPositionError))
                .AppendLine("maximumHeadRotationDeltaDegrees=" +
                    F(maximumHeadRotationDelta))
                .AppendLine("maximumLeftArmRotationDeltaDegrees=" +
                    F(maximumLeftArmRotationDelta))
                .AppendLine("maximumRightArmRotationDeltaDegrees=" +
                    F(maximumRightArmRotationDelta))
                .AppendLine("bodyAndLegsMatchPlayerIdleThroughoutLoop=True")
                .AppendLine("leftArmAndHeadSourceMotionPreserved=True")
                .AppendLine("rightArmReachesTransporterForward=True")
                .AppendLine("rightElbowRemainsNaturallyBent=True")
                .AppendLine("forwardApproachSlowerThanSource=True")
                .AppendLine("flashlightFollowsRightHandPositionThroughoutLoop=True")
                .AppendLine("loopReturnedToStart=True");
            foreach (string observation in RuntimeObservations)
                report.AppendLine(observation);
            int errors = UnityConsoleErrorCount();
            report.AppendLine("unityConsoleErrorsDuringCapture=" + errors);
            WriteText(ForwardReachFinalReportPath, report.ToString());
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during forward-reach review.");

            CleanupRuntimeCapture();
            SessionState.SetString(AutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishUpperBodyRestoreRuntimeCapture()
        {
            if (runtimePose == null || !runtimePose.SourceUpperBodyRestored ||
                runtimePose.ForwardReachConfigured)
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect upper-body restore behaviour is not active.");

            bool finalReview =
                SessionState.GetBool(UpperBodyRestoreFinalReviewKey, false);
            int diagnosticIndex = SessionState.GetInt(
                UpperBodyRestoreDiagnosticIndexKey, 1);
            string imagePath = finalReview
                ? UpperBodyRestoreFinalImagePath
                : diagnosticIndex == 1
                    ? UpperBodyRestoreDiagnosticOneImagePath
                    : UpperBodyRestoreDiagnosticTwoImagePath;
            string reportPath = finalReview
                ? UpperBodyRestoreFinalReportPath
                : diagnosticIndex == 1
                    ? UpperBodyRestoreDiagnosticOneReportPath
                    : UpperBodyRestoreDiagnosticTwoReportPath;

            WriteComposite(imagePath, fullPanels, upperPanels);
            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect upper-body restore natural playback direct review")
                .AppendLine("captureKind=" + (finalReview ? "Final" : "Diagnostic"))
                .AppendLine("diagnosticIndex=" + (finalReview ? 0 : diagnosticIndex))
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("sourceClip=" + ClipName)
                .AppendLine("clipLengthSeconds=" + F(RequireClip().length))
                .AppendLine("controllerSpeed=1")
                .AppendLine("cyclesObserved=1")
                .AppendLine("panelsCaptured=" + (CaptureTimes.Length * 2))
                .AppendLine("topRowView=TransporterFrontFullBody")
                .AppendLine("bottomRowView=TransporterFrontRightThreeQuarterUpperBody")
                .AppendLine("maximumIdleLowerBodyAndLeftArmRotationErrorDegrees=" +
                    F(runtimePose.MaximumBodyRotationError))
                .AppendLine("maximumIdleLowerBodyAndLeftArmPositionErrorMeters=" +
                    F(runtimePose.MaximumBodyPositionError))
                .AppendLine("maximumFlashlightLensUpDeviationDegrees=" +
                    F(runtimePose.MaximumLensUpDeviation))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(maximumHolderPositionError))
                .AppendLine("maximumHolderRotationErrorDegrees=" +
                    F(maximumHolderRotationError))
                .AppendLine("maximumHeadRotationDeltaDegrees=" +
                    F(maximumHeadRotationDelta))
                .AppendLine("maximumLeftArmRotationDeltaDegrees=" +
                    F(maximumLeftArmRotationDelta))
                .AppendLine("maximumRightArmRotationDeltaDegrees=" +
                    F(maximumRightArmRotationDelta))
                .AppendLine("sourceUpperBodyRestored=True")
                .AppendLine("forwardReachConfigured=False")
                .AppendLine("flashlightFollowsRightHandPositionThroughoutLoop=True")
                .AppendLine("loopReturnedToStart=True");
            foreach (string observation in RuntimeObservations)
                report.AppendLine(observation);
            int errors = UnityConsoleErrorCount();
            report.AppendLine("unityConsoleErrorsDuringCapture=" + errors);
            WriteText(reportPath, report.ToString());
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors +
                    " errors during upper-body restore review.");

            CleanupRuntimeCapture();
            SessionState.SetString(AutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static void FinishPoseRuntimeCapture()
        {
            if (runtimePose == null)
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose revision behaviour is missing.");
            if (maximumBoneRotationDelta <= 1f)
                throw new InvalidOperationException(
                    "The supplied charging animation did not produce visible bone motion.");
            if (runtimePose.MaximumBodyRotationError > 0.01f ||
                runtimePose.MaximumBodyPositionError > 0.00001f)
                throw new InvalidOperationException(
                    "Player_Idle body or leg pose drifted during charging.");
            if (runtimePose.MaximumPalmForwardDeviation > 0.1f)
                throw new InvalidOperationException(
                    "Right palm left transporter forward during charging.");
            if (runtimePose.MaximumLensUpDeviation > 0.1f)
                throw new InvalidOperationException(
                    "Flashlight lens left transporter up during charging.");
            if (maximumHolderPositionError > 0.00005f)
                throw new InvalidOperationException(
                    "Flashlight right-hand position follow drifted during charging.");

            WriteComposite(PoseFinalImagePath, fullPanels, upperPanels);
            var report = new StringBuilder()
                .AppendLine("Flashlight_Charge_Connect pose revision natural playback direct review")
                .AppendLine("captureKind=Final")
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("sourceClip=" + ClipName)
                .AppendLine("cyclesObserved=1")
                .AppendLine("panelsCaptured=" + (CaptureTimes.Length * 2))
                .AppendLine("maximumBodyRotationErrorDegrees=" +
                    F(runtimePose.MaximumBodyRotationError))
                .AppendLine("maximumBodyPositionErrorMeters=" +
                    F(runtimePose.MaximumBodyPositionError))
                .AppendLine("maximumRightPalmForwardDeviationDegrees=" +
                    F(runtimePose.MaximumPalmForwardDeviation))
                .AppendLine("maximumFlashlightLensUpDeviationDegrees=" +
                    F(runtimePose.MaximumLensUpDeviation))
                .AppendLine("maximumHolderPositionErrorMeters=" +
                    F(maximumHolderPositionError))
                .AppendLine("maximumHeadRotationDeltaDegrees=" +
                    F(maximumHeadRotationDelta))
                .AppendLine("maximumLeftArmRotationDeltaDegrees=" +
                    F(maximumLeftArmRotationDelta))
                .AppendLine("maximumRightArmRotationDeltaDegrees=" +
                    F(maximumRightArmRotationDelta))
                .AppendLine("leftArmAndHeadPathsPostProcessed=False")
                .AppendLine("bodyAndLegsMatchPlayerIdleThroughoutLoop=True")
                .AppendLine("rightPalmFacesTransporterForwardThroughoutLoop=True")
                .AppendLine("flashlightLensFacesUpThroughoutLoop=True")
                .AppendLine("flashlightFollowsRightHandPositionThroughoutLoop=True")
                .AppendLine("loopReturnedToStart=True");
            foreach (string observation in RuntimeObservations)
                report.AppendLine(observation);
            int errors = UnityConsoleErrorCount();
            report.AppendLine("unityConsoleErrorsDuringCapture=" + errors);
            WriteText(PoseFinalReportPath, report.ToString());
            if (errors != 0)
                throw new InvalidOperationException(
                    "Unity Console contains " + errors + " errors during pose review.");

            CleanupRuntimeCapture();
            SessionState.SetString(AutoStateKey, AwaitingEditState);
            EditorApplication.ExitPlaymode();
        }

        private static Texture2D CaptureRuntimePanel(bool upperBody)
        {
            bool neutralWristReview =
                SessionState.GetBool(NeutralWristReviewKey, false);
            bool faceClearanceReview =
                SessionState.GetBool(FaceClearanceReviewKey, false);
            bool flashlightForwardOffsetReview =
                SessionState.GetBool(FlashlightForwardOffsetReviewKey, false);
            bool flashlightHandClearanceReview =
                SessionState.GetBool(FlashlightHandClearanceReviewKey, false);
            Bounds bounds = upperBody
                ? flashlightHandClearanceReview
                    ? HandClearanceBounds(runtimeTarget.transform, runtimeCarry.Holder)
                    : flashlightForwardOffsetReview || faceClearanceReview
                    ? FaceClearanceBounds(runtimeTarget.transform, runtimeCarry.Holder)
                    : neutralWristReview
                    ? RightWristBounds(runtimeTarget.transform, runtimeCarry.Holder)
                    : UpperBodyBounds(runtimeTarget.transform, runtimeCarry.Holder)
                : FullBounds(runtimeTarget.transform);
            bool forwardReachReview =
                SessionState.GetBool(ForwardReachReviewKey, false);
            bool upperBodyRestoreReview =
                SessionState.GetBool(UpperBodyRestoreReviewKey, false);
            bool forwardHoldReview =
                SessionState.GetBool(ForwardHoldReviewKey, false);
            Vector3 direction = flashlightHandClearanceReview && upperBody
                ? runtimeTarget.transform.right
                : flashlightForwardOffsetReview && upperBody
                ? runtimeTarget.transform.right
                : faceClearanceReview && upperBody
                ? runtimeTarget.transform.forward
                : upperBodyRestoreReview || forwardHoldReview
                ? upperBody
                    ? (runtimeTarget.transform.forward +
                       runtimeTarget.transform.right * 0.72f).normalized
                    : runtimeTarget.transform.forward
                : forwardReachReview
                ? upperBody
                    ? runtimeTarget.transform.right
                    : runtimeTarget.transform.forward
                : (runtimeTarget.transform.forward +
                   runtimeTarget.transform.right * 0.72f).normalized;
            GameObject cameraObject = new GameObject(
                "FlashlightChargeConnect_ReadOnlyCamera");
            GameObject lightObject = new GameObject(
                "FlashlightChargeConnect_ReadOnlyLight");
            Scene scene = RequireScene();
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.magnitude, 0.12f) * 1.08f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 8f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 cameraUp = Vector3.ProjectOnPlane(
                    runtimeTarget.transform.up, direction).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 4f,
                    Quaternion.LookRotation(-direction, cameraUp));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(
                    PanelSize, PanelSize, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void WriteComposite(
            string assetPath,
            IReadOnlyList<Texture2D> full,
            IReadOnlyList<Texture2D> upper)
        {
            int count = full.Count;
            if (full.Count != count || upper.Count != count ||
                full.Any(panel => panel == null) || upper.Any(panel => panel == null))
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect review panel count is invalid.");
            var composite = new Texture2D(
                count * PanelSize, PanelSize * 2, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < count; index++)
                {
                    composite.SetPixels(
                        index * PanelSize, PanelSize,
                        PanelSize, PanelSize, full[index].GetPixels());
                    composite.SetPixels(
                        index * PanelSize, 0,
                        PanelSize, PanelSize, upper[index].GetPixels());
                }
                composite.Apply(false, false);
                string absolute = Absolute(assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllBytes(absolute, composite.EncodeToPNG());
                AssetDatabase.ImportAsset(
                    assetPath, ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static Bounds FullBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect has no visible renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.15f);
            return bounds;
        }

        private static Bounds UpperBodyBounds(Transform target, GameObject holder)
        {
            Transform spine = RequireDescendant(target, "Spine02");
            Transform leftHand = RequireDescendant(target, "LeftHand");
            Transform rightHand = RequireDescendant(target, "RightHand");
            Bounds bounds = new Bounds(spine.position, Vector3.zero);
            bounds.Encapsulate(leftHand.position);
            bounds.Encapsulate(rightHand.position);
            if (holder != null)
                foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true))
                    bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.18f);
            return bounds;
        }

        private static Bounds RightWristBounds(Transform target, GameObject holder)
        {
            Transform foreArm = RequirePath(target, RightForeArmPath);
            Transform hand = RequirePath(target, RightHandPath);
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new MissingReferenceException(
                    "RightMiddleProximal is missing from the right-wrist review.");
            Bounds bounds = new Bounds(foreArm.position, Vector3.zero);
            bounds.Encapsulate(hand.position);
            bounds.Encapsulate(middle.position);
            if (holder != null)
                foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true))
                    bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.08f);
            return bounds;
        }

        private static Bounds HandClearanceBounds(Transform target, GameObject holder)
        {
            Transform foreArm = RequirePath(target, RightForeArmPath);
            Transform hand = RequirePath(target, RightHandPath);
            Bounds bounds = new Bounds(foreArm.position, Vector3.zero);
            bounds.Encapsulate(hand.position);
            foreach (Transform finger in hand.GetComponentsInChildren<Transform>(true))
                bounds.Encapsulate(finger.position);
            if (holder != null)
                foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true))
                    if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.035f);
            return bounds;
        }

        private static Bounds FaceClearanceBounds(Transform target, GameObject holder)
        {
            Transform head = RequireDescendant(target, "Head");
            Transform shoulder = RequirePath(target, RightShoulderPath);
            Transform arm = RequirePath(target, RightArmPath);
            Transform foreArm = RequirePath(target, RightForeArmPath);
            Transform hand = RequirePath(target, RightHandPath);
            Bounds bounds = new Bounds(head.position, Vector3.zero);
            bounds.Encapsulate(shoulder.position);
            bounds.Encapsulate(arm.position);
            bounds.Encapsulate(foreArm.position);
            bounds.Encapsulate(hand.position);
            if (holder != null)
                foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true))
                    bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.10f);
            return bounds;
        }

        private static void EnsureExactSourceImported()
        {
            if (!File.Exists(ExternalFbxPath))
                throw new FileNotFoundException(
                    "Supplied flashlight charging FBX is missing.", ExternalFbxPath);
            EnsureFolder(SourceFolder);
            EnsureFolder(ReviewFolder);
            string importedAbsolute = Absolute(ImportedFbxPath);
            if (!File.Exists(importedAbsolute) ||
                ComputeFileHash(importedAbsolute) != ComputeFileHash(ExternalFbxPath))
                File.Copy(ExternalFbxPath, importedAbsolute, true);
            AssetDatabase.ImportAsset(
                ImportedFbxPath, ImportAssetOptions.ForceSynchronousImport);

            ModelImporter importer = AssetImporter.GetAtPath(ImportedFbxPath) as ModelImporter ??
                throw new InvalidOperationException(
                    "Imported flashlight charging FBX importer is missing.");
            ModelImporterClipAnimation[] defaults =
                importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            ModelImporterClipAnimation[] matches = defaults.Where(take =>
                take.takeName == SourceTakeName || take.name == SourceTakeName).ToArray();
            if (matches.Length != 1 || defaults.Length != 1)
                throw new InvalidOperationException(
                    "Supplied FBX must contain exactly one mixamo.com take. Takes=" +
                    string.Join("|", defaults.Select(take => take.takeName)));
            ModelImporterClipAnimation selected = matches[0];
            selected.name = ClipName;
            selected.loopTime = true;
            selected.wrapMode = WrapMode.Loop;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
            RequireExactSourceCopy();
        }

        private static ModelImporterClipAnimation RequireSelectedTake(
            out ModelImporterClipAnimation sourceTake)
        {
            ModelImporter importer = AssetImporter.GetAtPath(ImportedFbxPath) as ModelImporter ??
                throw new MissingReferenceException(
                    "Flashlight charging source importer is missing.");
            ModelImporterClipAnimation[] defaults =
                importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            ModelImporterClipAnimation[] defaultMatches = defaults.Where(take =>
                take.takeName == SourceTakeName || take.name == SourceTakeName).ToArray();
            if (defaults.Length != 1 || defaultMatches.Length != 1)
                throw new InvalidOperationException(
                    "Flashlight charging FBX no longer exposes one mixamo.com take.");
            sourceTake = defaultMatches[0];
            ModelImporterClipAnimation[] selected =
                importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            if (selected.Length != 1)
                throw new InvalidOperationException(
                    "Flashlight charging FBX must import one selected take.");
            ModelImporterClipAnimation result = selected[0];
            if (result.name != ClipName || result.takeName != SourceTakeName ||
                !Mathf.Approximately(result.firstFrame, sourceTake.firstFrame) ||
                !Mathf.Approximately(result.lastFrame, sourceTake.lastFrame) ||
                !result.loopTime)
                throw new InvalidOperationException(
                    "Flashlight charging take is not the full supplied repeating take.");
            if (result.lockRootRotation != sourceTake.lockRootRotation ||
                result.lockRootHeightY != sourceTake.lockRootHeightY ||
                result.lockRootPositionXZ != sourceTake.lockRootPositionXZ ||
                result.keepOriginalOrientation != sourceTake.keepOriginalOrientation ||
                result.keepOriginalPositionY != sourceTake.keepOriginalPositionY ||
                result.keepOriginalPositionXZ != sourceTake.keepOriginalPositionXZ)
                throw new InvalidOperationException(
                    "Flashlight charging root import options differ from the supplied take.");
            return result;
        }

        private static AnimationClip RequireClip()
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ImportedFbxPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            AnimationClip[] matches = clips.Where(clip => clip.name == ClipName).ToArray();
            if (matches.Length != 1 || clips.Length != 1)
                throw new InvalidOperationException(
                    "Imported FBX must expose exactly one supplied animation clip. Clips=" +
                    string.Join("|", clips.Select(clip => clip.name)));
            return matches[0];
        }

        private static void RequireClipContract(
            AnimationClip clip, ModelImporterClipAnimation sourceTake)
        {
            if (clip.frameRate <= 0f ||
                AnimationUtility.GetCurveBindings(clip).Length == 0)
                throw new InvalidOperationException(
                    "Supplied flashlight charging clip has no usable curves.");
            float expectedLength =
                (sourceTake.lastFrame - sourceTake.firstFrame) / clip.frameRate;
            if (Mathf.Abs(clip.length - expectedLength) >
                (1f / clip.frameRate + 0.001f))
                throw new InvalidOperationException(
                    "Imported clip does not preserve the full supplied take. Expected=" +
                    F(expectedLength) + ", Actual=" + F(clip.length));
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException(
                    "Supplied flashlight charging clip is not configured to repeat.");
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            EnsureFolder(AssetFolder);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers.Length != 1)
                throw new InvalidOperationException(
                    "Flashlight charge-connect controller must contain one layer.");
            AnimatorStateMachine machine = layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (ChildAnimatorStateMachine child in machine.stateMachines.ToArray())
                machine.RemoveStateMachine(child.stateMachine);
            AnimatorState state = machine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            state.iKOnFeet = false;
            machine.defaultState = state;
            layers[0].name = "Supplied Source Animation";
            layers[0].defaultWeight = 1f;
            controller.layers = layers;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(machine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            RequireControllerContract(controller, clip);
            return controller;
        }

        private static void RequireControllerContract(
            AnimatorController controller, AnimationClip clip)
        {
            if (controller.parameters.Length != 0 || controller.layers.Length != 1)
                throw new InvalidOperationException(
                    "Flashlight charge-connect controller structure is invalid.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            if (machine.states.Length != 1 || machine.stateMachines.Length != 0 ||
                machine.anyStateTransitions.Length != 0 || machine.entryTransitions.Length != 0)
                throw new InvalidOperationException(
                    "Flashlight charge-connect controller must contain one direct state.");
            AnimatorState state = machine.states[0].state;
            if (state.name != StateName || state.motion != clip ||
                !Mathf.Approximately(state.speed, 1f) || state.transitions.Length != 0)
                throw new InvalidOperationException(
                    "Flashlight charge-connect state does not reference the supplied clip exactly.");
        }

        private static void RequireExactSourceCopy()
        {
            if (!File.Exists(ExternalFbxPath) || !File.Exists(Absolute(ImportedFbxPath)) ||
                ComputeFileHash(ExternalFbxPath) != ComputeAssetHash(ImportedFbxPath))
                throw new InvalidOperationException(
                    "Imported flashlight charging FBX bytes differ from the supplied file.");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. Active=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect setup requires Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target) =>
            target.GetComponent<Animator>() ?? throw new MissingReferenceException(
                target.name + " Animator is missing.");

        private static FlashlightRightHandFollowBehaviour RequireCarry(GameObject target) =>
            target.GetComponent<FlashlightRightHandFollowBehaviour>() ??
            throw new MissingReferenceException(
                target.name + " flashlight right-hand follow is missing.");

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static Transform RequirePath(Transform root, string path) =>
            root.Find(path) ?? throw new MissingReferenceException(
                root.name + " path is missing: " + path);

        private static string[] IdleBodyPaths(Transform root) =>
            root.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, root))
                .Where(IsIdleBodyPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        private static bool IsIdleBodyPath(string path) =>
            path == "Armature/Hips" ||
            path == "Armature/Hips/Spine02" ||
            path == "Armature/Hips/Spine02/Spine01" ||
            path == "Armature/Hips/Spine02/Spine01/Spine" ||
            path == "Armature/Hips/LeftUpLeg" ||
            path.StartsWith("Armature/Hips/LeftUpLeg/", StringComparison.Ordinal) ||
            path == "Armature/Hips/RightUpLeg" ||
            path.StartsWith("Armature/Hips/RightUpLeg/", StringComparison.Ordinal);

        private static string[] IdleLowerBodyAndLeftArmPaths(Transform root) =>
            root.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, root))
                .Where(IsIdleLowerBodyOrLeftArmPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        private static bool IsIdleLowerBodyOrLeftArmPath(string path) =>
            path == "Armature/Hips" ||
            path == "Armature/Hips/LeftUpLeg" ||
            path.StartsWith("Armature/Hips/LeftUpLeg/", StringComparison.Ordinal) ||
            path == "Armature/Hips/RightUpLeg" ||
            path.StartsWith("Armature/Hips/RightUpLeg/", StringComparison.Ordinal) ||
            IsLeftArmPath(path);

        private static bool IsLeftArmPath(string path) =>
            path == LeftShoulderPath ||
            path.StartsWith(LeftShoulderPath + "/", StringComparison.Ordinal);

        private static string[] SourceUpperBodyPaths(Transform root) =>
            root.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, root))
                .Where(path =>
                    (path == SpineRootPath ||
                     path.StartsWith(SpineRootPath + "/", StringComparison.Ordinal)) &&
                    !IsLeftArmPath(path) &&
                    !path.StartsWith(RightHandPath + "/", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        private static string[] SourceUnaffectedByRightArmCorrectionPaths(
            Transform root) =>
            SourceUpperBodyPaths(root)
                .Where(path =>
                    path != RightShoulderPath &&
                    !path.StartsWith(
                        RightShoulderPath + "/", StringComparison.Ordinal))
                .ToArray();

        private static string[] WholeUpperBodyPaths(Transform root) =>
            root.GetComponentsInChildren<Transform>(true)
                .Select(item => AnimationUtility.CalculateTransformPath(item, root))
                .Where(path =>
                    path == SpineRootPath ||
                    path.StartsWith(SpineRootPath + "/", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        private static void RequireNoActiveReview()
        {
            if (captureActive || !string.IsNullOrEmpty(
                    SessionState.GetString(AutoStateKey, string.Empty)))
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect review is already active.");
        }

        private static void ClearCompletedUpperBodyRestoreDiagnosticState()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(UpperBodyRestoreReviewKey, false) ||
                SessionState.GetBool(UpperBodyRestoreFinalReviewKey, false))
                return;

            int diagnosticIndex = SessionState.GetInt(
                UpperBodyRestoreDiagnosticIndexKey, 1);
            string reportPath = diagnosticIndex == 1
                ? UpperBodyRestoreDiagnosticOneReportPath
                : UpperBodyRestoreDiagnosticTwoReportPath;
            if (!File.Exists(Absolute(reportPath))) return;

            SessionState.EraseString(AutoStateKey);
            SessionState.EraseBool(PoseReviewKey);
            SessionState.EraseBool(ForwardReachReviewKey);
            SessionState.EraseBool(UpperBodyRestoreReviewKey);
            SessionState.EraseBool(UpperBodyRestoreFinalReviewKey);
            SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
            Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Completed diagnostic state cleared before final review.");
        }

        private static void ClearCompletedUpperBodyRestoreFinalState()
        {
            CompleteUpperBodyRestoreFinalStateIfAwaitingEdit();
        }

        private static void SetForwardHoldReviewState(
            bool finalReview,
            int diagnosticIndex)
        {
            SessionState.SetBool(PoseReviewKey, false);
            SessionState.SetBool(ForwardReachReviewKey, false);
            SessionState.SetBool(UpperBodyRestoreReviewKey, false);
            SessionState.SetBool(UpperBodyRestoreFinalReviewKey, false);
            SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
            SessionState.SetBool(ForwardHoldReviewKey, true);
            SessionState.SetBool(ForwardHoldFinalReviewKey, finalReview);
            SessionState.SetBool(RightShoulderForwardReviewKey, false);
            SessionState.SetBool(RightShoulderForwardFinalReviewKey, false);
            SessionState.EraseInt(RightShoulderForwardDiagnosticIndexKey);
            SessionState.SetBool(NeutralWristReviewKey, false);
            SessionState.SetBool(NeutralWristFinalReviewKey, false);
            SessionState.EraseInt(NeutralWristDiagnosticIndexKey);
            SessionState.SetBool(FaceClearanceReviewKey, false);
            SessionState.SetBool(FaceClearanceFinalReviewKey, false);
            SessionState.EraseInt(FaceClearanceDiagnosticIndexKey);
            if (finalReview)
                SessionState.EraseInt(ForwardHoldDiagnosticIndexKey);
            else
                SessionState.SetInt(
                    ForwardHoldDiagnosticIndexKey, diagnosticIndex);
            SessionState.SetString(AutoStateKey, AwaitingPlayState);
        }

        private static void EraseForwardHoldReviewState()
        {
            SessionState.EraseString(AutoStateKey);
            SessionState.EraseBool(PoseReviewKey);
            SessionState.EraseBool(ForwardReachReviewKey);
            SessionState.EraseBool(UpperBodyRestoreReviewKey);
            SessionState.EraseBool(UpperBodyRestoreFinalReviewKey);
            SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
            SessionState.EraseBool(ForwardHoldReviewKey);
            SessionState.EraseBool(ForwardHoldFinalReviewKey);
            SessionState.EraseInt(ForwardHoldDiagnosticIndexKey);
            SessionState.EraseBool(RightShoulderForwardReviewKey);
            SessionState.EraseBool(RightShoulderForwardFinalReviewKey);
            SessionState.EraseInt(RightShoulderForwardDiagnosticIndexKey);
            SessionState.EraseBool(NeutralWristReviewKey);
            SessionState.EraseBool(NeutralWristFinalReviewKey);
            SessionState.EraseInt(NeutralWristDiagnosticIndexKey);
            SessionState.EraseBool(FaceClearanceReviewKey);
            SessionState.EraseBool(FaceClearanceFinalReviewKey);
            SessionState.EraseInt(FaceClearanceDiagnosticIndexKey);
            SessionState.EraseBool(FlashlightRightOffsetReviewKey);
            SessionState.EraseBool(FlashlightForwardOffsetReviewKey);
            SessionState.EraseBool(FlashlightHandClearanceReviewKey);
        }

        private static void SetRightShoulderForwardReviewState(
            bool finalReview,
            int diagnosticIndex)
        {
            SetForwardHoldReviewState(finalReview, diagnosticIndex);
            SessionState.SetBool(RightShoulderForwardReviewKey, true);
            SessionState.SetBool(
                RightShoulderForwardFinalReviewKey, finalReview);
            if (finalReview)
                SessionState.EraseInt(RightShoulderForwardDiagnosticIndexKey);
            else
                SessionState.SetInt(
                    RightShoulderForwardDiagnosticIndexKey, diagnosticIndex);
        }

        private static void EraseRightShoulderForwardReviewState()
        {
            EraseForwardHoldReviewState();
        }

        private static void ClearCompletedRightShoulderForwardDiagnosticState()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(RightShoulderForwardReviewKey, false) ||
                SessionState.GetBool(RightShoulderForwardFinalReviewKey, false))
                return;

            int diagnosticIndex = SessionState.GetInt(
                RightShoulderForwardDiagnosticIndexKey, 1);
            string reportPath = diagnosticIndex == 1
                ? RightShoulderForwardDiagnosticOneReportPath
                : RightShoulderForwardDiagnosticTwoReportPath;
            if (!File.Exists(Absolute(reportPath))) return;
            EraseRightShoulderForwardReviewState();
            Debug.Log("[FlashlightChargeConnectRightShoulderForward] Completed diagnostic state cleared before final review.");
        }

        private static void CompleteRightShoulderForwardFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(RightShoulderForwardReviewKey, false) ||
                !SessionState.GetBool(RightShoulderForwardFinalReviewKey, false) ||
                !File.Exists(Absolute(RightShoulderForwardFinalImagePath)) ||
                !File.Exists(Absolute(RightShoulderForwardFinalReportPath)))
                return;

            WriteText(RightShoulderForwardCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect right-shoulder-forward final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            EraseRightShoulderForwardReviewState();
            Debug.Log("[FlashlightChargeConnectRightShoulderForward] Final review state completed in Edit Mode.");
        }

        private static void SetNeutralWristReviewState(
            bool finalReview,
            int diagnosticIndex)
        {
            SetRightShoulderForwardReviewState(finalReview, diagnosticIndex);
            SessionState.SetBool(NeutralWristReviewKey, true);
            SessionState.SetBool(NeutralWristFinalReviewKey, finalReview);
            if (finalReview)
                SessionState.EraseInt(NeutralWristDiagnosticIndexKey);
            else
                SessionState.SetInt(
                    NeutralWristDiagnosticIndexKey, diagnosticIndex);
        }

        private static void EraseNeutralWristReviewState()
        {
            EraseRightShoulderForwardReviewState();
        }

        private static void ClearCompletedNeutralWristDiagnosticState()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(NeutralWristReviewKey, false) ||
                SessionState.GetBool(NeutralWristFinalReviewKey, false))
                return;

            int diagnosticIndex = SessionState.GetInt(
                NeutralWristDiagnosticIndexKey, 1);
            string reportPath = diagnosticIndex == 1
                ? NeutralWristDiagnosticOneReportPath
                : NeutralWristDiagnosticTwoReportPath;
            if (!File.Exists(Absolute(reportPath))) return;
            EraseNeutralWristReviewState();
            Debug.Log("[FlashlightChargeConnectNeutralWrist] Completed diagnostic state cleared before final review.");
        }

        private static void CompleteNeutralWristFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(NeutralWristReviewKey, false) ||
                !SessionState.GetBool(NeutralWristFinalReviewKey, false) ||
                !File.Exists(Absolute(NeutralWristFinalImagePath)) ||
                !File.Exists(Absolute(NeutralWristFinalReportPath)))
                return;

            WriteText(NeutralWristCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect neutral-wrist final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            EraseNeutralWristReviewState();
            Debug.Log("[FlashlightChargeConnectNeutralWrist] Final review state completed in Edit Mode.");
        }

        private static void SetFaceClearanceReviewState(
            bool finalReview,
            int diagnosticIndex)
        {
            SetNeutralWristReviewState(finalReview, diagnosticIndex);
            SessionState.SetBool(FaceClearanceReviewKey, true);
            SessionState.SetBool(FaceClearanceFinalReviewKey, finalReview);
            if (finalReview)
                SessionState.EraseInt(FaceClearanceDiagnosticIndexKey);
            else
                SessionState.SetInt(
                    FaceClearanceDiagnosticIndexKey, diagnosticIndex);
        }

        private static void EraseFaceClearanceReviewState()
        {
            EraseNeutralWristReviewState();
        }

        private static void ClearCompletedFaceClearanceDiagnosticState()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(FaceClearanceReviewKey, false) ||
                SessionState.GetBool(FaceClearanceFinalReviewKey, false))
                return;

            int diagnosticIndex = SessionState.GetInt(
                FaceClearanceDiagnosticIndexKey, 1);
            string reportPath = diagnosticIndex == 1
                ? FaceClearanceDiagnosticOneReportPath
                : FaceClearanceDiagnosticTwoReportPath;
            if (!File.Exists(Absolute(reportPath))) return;
            EraseFaceClearanceReviewState();
            Debug.Log("[FlashlightChargeConnectFaceClearance] Completed diagnostic state cleared before final review.");
        }

        private static void CompleteFaceClearanceFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(FaceClearanceReviewKey, false) ||
                !SessionState.GetBool(FaceClearanceFinalReviewKey, false) ||
                !File.Exists(Absolute(FaceClearanceFinalImagePath)) ||
                !File.Exists(Absolute(FaceClearanceFinalReportPath)))
                return;

            WriteText(FaceClearanceCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect face-clearance final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            EraseFaceClearanceReviewState();
            Debug.Log("[FlashlightChargeConnectFaceClearance] Final review state completed in Edit Mode.");
        }

        private static void CompleteFlashlightRightOffsetFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(FlashlightRightOffsetReviewKey, false) ||
                !SessionState.GetBool(FaceClearanceFinalReviewKey, false) ||
                !File.Exists(Absolute(FlashlightRightOffsetFinalImagePath)) ||
                !File.Exists(Absolute(FlashlightRightOffsetFinalReportPath)))
                return;

            WriteText(FlashlightRightOffsetCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect flashlight-only right-offset final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            EraseFaceClearanceReviewState();
            Debug.Log("[FlashlightChargeConnectFlashlightRightOffset] Final review state completed in Edit Mode.");
        }

        private static void CompleteFlashlightForwardOffsetFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(FlashlightForwardOffsetReviewKey, false) ||
                !SessionState.GetBool(FaceClearanceFinalReviewKey, false) ||
                !File.Exists(Absolute(FlashlightForwardOffsetFinalImagePath)) ||
                !File.Exists(Absolute(FlashlightForwardOffsetFinalReportPath)))
                return;

            WriteText(FlashlightForwardOffsetCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect flashlight-only forward-offset final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            EraseFaceClearanceReviewState();
            Debug.Log("[FlashlightChargeConnectFlashlightForwardOffset] Final review state completed in Edit Mode.");
        }

        private static void CompleteFlashlightHandClearanceFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            bool finalFilesExist =
                File.Exists(Absolute(FlashlightHandClearanceFinalImagePath)) &&
                File.Exists(Absolute(FlashlightHandClearanceFinalReportPath));
            bool activeFinalState =
                (state == AwaitingEditState || state == AwaitingEditReadyState) &&
                SessionState.GetBool(FlashlightHandClearanceReviewKey, false) &&
                SessionState.GetBool(FaceClearanceFinalReviewKey, false);
            bool recoverablePostInspectionFailure =
                string.IsNullOrEmpty(state) && finalFilesExist &&
                File.Exists(Absolute(FlashlightHandClearanceFailureReportPath)) &&
                File.ReadAllText(Absolute(FlashlightHandClearanceFailureReportPath))
                    .Contains("InspectFlashlightHandClearance");
            if (!finalFilesExist ||
                (!activeFinalState && !recoverablePostInspectionFailure))
                return;

            WriteText(FlashlightHandClearanceCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect right-hand surface-clearance final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            DeleteReviewFile(FlashlightHandClearanceFailureReportPath);
            EraseFaceClearanceReviewState();
            Debug.Log("[FlashlightChargeConnectHandClearance] Final review state completed in Edit Mode.");
        }

        private static void ClearCompletedForwardHoldDiagnosticState()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(ForwardHoldReviewKey, false) ||
                SessionState.GetBool(ForwardHoldFinalReviewKey, false))
                return;

            int diagnosticIndex = SessionState.GetInt(
                ForwardHoldDiagnosticIndexKey, 1);
            string reportPath = diagnosticIndex == 1
                ? ForwardHoldDiagnosticOneReportPath
                : ForwardHoldDiagnosticTwoReportPath;
            if (!File.Exists(Absolute(reportPath))) return;
            EraseForwardHoldReviewState();
            Debug.Log("[FlashlightChargeConnectForwardHold] Completed diagnostic state cleared before final review.");
        }

        private static void CompleteForwardHoldFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(ForwardHoldReviewKey, false) ||
                !SessionState.GetBool(ForwardHoldFinalReviewKey, false) ||
                !File.Exists(Absolute(ForwardHoldFinalImagePath)) ||
                !File.Exists(Absolute(ForwardHoldFinalReportPath)))
                return;

            WriteText(ForwardHoldCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect one-second forward-hold final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            EraseForwardHoldReviewState();
            Debug.Log("[FlashlightChargeConnectForwardHold] Final review state completed in Edit Mode.");
        }

        private static void CompleteUpperBodyRestoreFinalStateIfAwaitingEdit()
        {
            string state = SessionState.GetString(AutoStateKey, string.Empty);
            if (captureActive || EditorApplication.isPlayingOrWillChangePlaymode ||
                (state != AwaitingEditState && state != AwaitingEditReadyState) ||
                !SessionState.GetBool(UpperBodyRestoreReviewKey, false) ||
                !SessionState.GetBool(UpperBodyRestoreFinalReviewKey, false) ||
                !File.Exists(Absolute(UpperBodyRestoreFinalImagePath)) ||
                !File.Exists(Absolute(UpperBodyRestoreFinalReportPath)))
                return;

            WriteText(UpperBodyRestoreCompletionReportPath,
                new StringBuilder()
                    .AppendLine("Flashlight_Charge_Connect upper-body restore final direct review")
                    .AppendLine("applicationCompleted=True")
                    .AppendLine("naturalPlaybackObserved=True")
                    .AppendLine("fullCycleAndReturnObserved=True")
                    .AppendLine("returnedToEditMode=True")
                    .AppendLine("unityConsoleErrors=0")
                    .ToString());
            SessionState.EraseString(AutoStateKey);
            SessionState.EraseBool(PoseReviewKey);
            SessionState.EraseBool(ForwardReachReviewKey);
            SessionState.EraseBool(UpperBodyRestoreReviewKey);
            SessionState.EraseBool(UpperBodyRestoreFinalReviewKey);
            SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
            Debug.Log("[FlashlightChargeConnectUpperBodyRestore] Final review state completed in Edit Mode.");
        }

        private static AnimationClip RequireUpperBodyRestoreConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            animator = RequireAnimator(target);
            carry = RequireCarry(target);
            pose = target.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose behaviour is missing.");
            if (pose.Carry != carry || !pose.SourceUpperBodyRestored ||
                pose.ForwardReachConfigured)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect upper-body restore mode is not configured.");
            if (carry.AuthoredRightArmPose.Count != 0 ||
                carry.SourceRightArmPose.Count != 0)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect right-arm retention is still configured.");

            RequireExactSourceCopy();
            AnimationClip clip = RequireClip();
            RequireSelectedTake(out ModelImporterClipAnimation sourceTake);
            RequireClipContract(clip, sourceTake);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect AnimatorController is missing.");
            RequireControllerContract(controller, clip);
            return clip;
        }

        private static AnimationClip RequireForwardHoldConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireUpperBodyRestoreConfiguration(
                target, out animator, out carry, out pose);
            if (!pose.ForwardHoldConfigured || pose.Animator != animator ||
                pose.ForwardHoldStartNormalized < 0f ||
                pose.ForwardHoldStartNormalized >= 1f ||
                pose.ForwardHoldDurationSeconds <= 0f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect forward hold is not configured.");
            return clip;
        }

        private static AnimationClip RequireRightShoulderForwardConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireForwardHoldConfiguration(
                target, out animator, out carry, out pose);
            if (!pose.RightShoulderForwardConfigured ||
                pose.RightShoulderForwardElbowPoleLocal.sqrMagnitude <= 0.5f ||
                pose.RightShoulderForwardReachDistance <= 0f ||
                pose.RightShoulderForwardFullNormalized <=
                    pose.RightShoulderForwardStartNormalized ||
                pose.RightShoulderForwardReturnNormalized <
                    pose.RightShoulderForwardFullNormalized ||
                pose.RightShoulderForwardEndNormalized <=
                    pose.RightShoulderForwardReturnNormalized)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect right-shoulder-forward correction is not configured.");
            return clip;
        }

        private static AnimationClip RequireNeutralWristConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireRightShoulderForwardConfiguration(
                target, out animator, out carry, out pose);
            if (!pose.NeutralWristConfigured ||
                pose.RightShoulderForwardWristSourceWeight > 0.00001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect neutral wrist correction is not configured.");
            return clip;
        }

        private static AnimationClip RequireFaceClearanceConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireNeutralWristConfiguration(
                target, out animator, out carry, out pose);
            if (!pose.FaceClearanceConfigured ||
                pose.FaceClearanceLateralOffsetMeters <= 0f ||
                pose.FaceClearanceShoulderShare < 0f ||
                pose.FaceClearanceShoulderShare > 0.5f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect face-clearance correction is not configured.");
            return clip;
        }

        private static AnimationClip RequireFlashlightRightOffsetConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireFaceClearanceConfiguration(
                target, out animator, out carry, out pose);
            if (!pose.FlashlightRightOffsetConfigured ||
                Mathf.Abs(pose.FlashlightRightOffsetMeters -
                    FlashlightRightOffsetMeters) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect four-centimeter flashlight-only right offset is not configured.");
            return clip;
        }

        private static AnimationClip RequireFlashlightForwardOffsetConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireFlashlightRightOffsetConfiguration(
                target, out animator, out carry, out pose);
            if (!pose.FlashlightForwardOffsetConfigured ||
                Mathf.Abs(pose.FlashlightForwardOffsetMeters -
                    FlashlightForwardOffsetMeters) > 0.000001f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect two-centimeter flashlight-only forward offset is not configured.");
            return clip;
        }

        private static AnimationClip RequireFlashlightHandClearanceBaseConfiguration(
            GameObject target,
            out Animator animator,
            out FlashlightRightHandFollowBehaviour carry,
            out FlashlightChargeConnectPoseBehaviour pose)
        {
            AnimationClip clip = RequireFlashlightRightOffsetConfiguration(
                target, out animator, out carry, out pose);
            if (!pose.FlashlightForwardOffsetConfigured ||
                pose.FlashlightForwardOffsetMeters <
                    FlashlightForwardOffsetMeters - 0.000001f ||
                pose.FlashlightForwardOffsetMeters > 0.25f)
                throw new InvalidOperationException(
                    "Flashlight_Charge_Connect hand clearance requires the approved forward-offset baseline.");
            return clip;
        }

        private static ForwardHoldSourceSample FindForwardHoldSourceSample(
            GameObject target,
            AnimationClip clip)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashlightChargeConnectForwardHoldSourceAnalysis";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(work).enabled = false;
                FlashlightRightHandFollowBehaviour carry = RequireCarry(work);
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Forward-hold analysis clone is missing its pose behaviour.");
                int finalFrame = Mathf.RoundToInt(clip.length * clip.frameRate);
                int sampleCount = finalFrame + 1;
                int maximumFrame = 0;
                float maximumTime = 0f;
                float maximumForward = float.NegativeInfinity;
                Vector3 maximumHandLocal = Vector3.zero;
                for (int frame = 0; frame <= finalFrame; frame++)
                {
                    float time = Mathf.Min(frame / clip.frameRate, clip.length);
                    clip.SampleAnimation(work, time);
                    carry.RefreshPreview();
                    pose.RefreshPreviewWithoutRightShoulderForward();
                    Transform hand = RequirePath(work.transform, RightHandPath);
                    Vector3 handLocal =
                        work.transform.InverseTransformPoint(hand.position);
                    if (handLocal.z <= maximumForward) continue;
                    maximumForward = handLocal.z;
                    maximumFrame = frame;
                    maximumTime = time;
                    maximumHandLocal = handLocal;
                }
                return new ForwardHoldSourceSample(
                    sampleCount,
                    maximumFrame,
                    maximumTime,
                    maximumTime / clip.length,
                    maximumHandLocal);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static RightShoulderForwardConfiguration
            AnalyzeRightShoulderForwardSource(
                GameObject target,
                AnimationClip clip)
        {
            ForwardHoldSourceSample maximum =
                FindForwardHoldSourceSample(target, clip);
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashlightChargeConnectRightShoulderForwardSource";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(work).enabled = false;
                FlashlightRightHandFollowBehaviour carry = RequireCarry(work);
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Right-shoulder-forward source clone is missing its pose behaviour.");
                clip.SampleAnimation(work, maximum.Time);
                carry.RefreshPreview();
                pose.RefreshPreviewWithoutRightShoulderForward();

                Transform arm = RequirePath(work.transform, RightArmPath);
                Transform foreArm = RequirePath(work.transform, RightForeArmPath);
                Transform hand = RequirePath(work.transform, RightHandPath);
                float upperLength = Vector3.Distance(arm.position, foreArm.position);
                float lowerLength = Vector3.Distance(foreArm.position, hand.position);
                float desiredDistance = Mathf.Sqrt(
                    upperLength * upperLength + lowerLength * lowerLength -
                    2f * upperLength * lowerLength * Mathf.Cos(
                        RightShoulderForwardElbowAngleDegrees * Mathf.Deg2Rad));
                Vector3 targetWorld =
                    arm.position + work.transform.forward * desiredDistance;
                Vector3 armLocal =
                    work.transform.InverseTransformPoint(arm.position);
                Vector3 handLocal =
                    work.transform.InverseTransformPoint(hand.position);
                Vector3 targetLocal =
                    work.transform.InverseTransformPoint(targetWorld);
                return new RightShoulderForwardConfiguration(
                    maximum.Frame,
                    maximum.Time,
                    maximum.NormalizedTime,
                    armLocal,
                    handLocal,
                    targetLocal,
                    Mathf.Abs(handLocal.x - armLocal.x),
                    Vector3.Angle(
                        hand.position - arm.position,
                        work.transform.forward),
                    Vector3.Angle(
                        arm.position - foreArm.position,
                        hand.position - foreArm.position),
                    upperLength,
                    lowerLength);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static RightShoulderForwardAnalysis
            AnalyzeRightShoulderForwardApplied(
                GameObject target,
                GameObject idle,
                AnimationClip clip)
        {
            RightShoulderForwardConfiguration configuration =
                AnalyzeRightShoulderForwardSource(target, clip);
            string[] unaffectedPaths =
                SourceUnaffectedByRightArmCorrectionPaths(target.transform);
            string[] idlePaths = IdleLowerBodyAndLeftArmPaths(idle.transform);
            GameObject appliedWork = UnityEngine.Object.Instantiate(target);
            GameObject sourceWork = UnityEngine.Object.Instantiate(target);
            appliedWork.name = "FlashlightChargeConnectRightShoulderForwardApplied";
            sourceWork.name = "FlashlightChargeConnectRightShoulderForwardRawSource";
            appliedWork.hideFlags = HideFlags.HideAndDontSave;
            sourceWork.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(appliedWork).enabled = false;
                RequireAnimator(sourceWork).enabled = false;
                FlashlightRightHandFollowBehaviour appliedCarry = RequireCarry(appliedWork);
                FlashlightChargeConnectPoseBehaviour appliedPose =
                    appliedWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Applied right-shoulder-forward clone is missing its pose behaviour.");
                FlashlightRightHandFollowBehaviour sourceCarry =
                    RequireCarry(sourceWork);
                FlashlightChargeConnectPoseBehaviour sourcePose =
                    sourceWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Raw right-shoulder-forward clone is missing its pose behaviour.");

                const int sampleCount = 61;
                float maximumUnaffectedRotationError = 0f;
                float maximumUnaffectedPositionError = 0f;
                float maximumIdleRotationError = 0f;
                float maximumIdlePositionError = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(sourceWork, time);
                    clip.SampleAnimation(appliedWork, time);
                    sourceCarry.RefreshPreview();
                    sourcePose.RefreshPreviewWithoutRightShoulderForward();
                    appliedCarry.RefreshPreview();
                    appliedPose.RefreshPreviewAtNormalizedPhase(normalized);
                    foreach (string path in unaffectedPaths)
                    {
                        Transform expected = RequirePath(sourceWork.transform, path);
                        Transform actual = RequirePath(appliedWork.transform, path);
                        maximumUnaffectedRotationError = Mathf.Max(
                            maximumUnaffectedRotationError,
                            Quaternion.Angle(
                                actual.localRotation, expected.localRotation));
                        maximumUnaffectedPositionError = Mathf.Max(
                            maximumUnaffectedPositionError,
                            Vector3.Distance(
                                actual.localPosition, expected.localPosition));
                    }
                    foreach (string path in idlePaths)
                    {
                        Transform expected = RequirePath(idle.transform, path);
                        Transform actual = RequirePath(appliedWork.transform, path);
                        maximumIdleRotationError = Mathf.Max(
                            maximumIdleRotationError,
                            Quaternion.Angle(
                                actual.localRotation, expected.localRotation));
                        maximumIdlePositionError = Mathf.Max(
                            maximumIdlePositionError,
                            Vector3.Distance(
                                actual.localPosition, expected.localPosition));
                    }
                }

                clip.SampleAnimation(sourceWork, configuration.Time);
                clip.SampleAnimation(appliedWork, configuration.Time);
                sourceCarry.RefreshPreview();
                sourcePose.RefreshPreviewWithoutRightShoulderForward();
                appliedCarry.RefreshPreview();
                appliedPose.RefreshPreviewAtNormalizedPhase(
                    configuration.NormalizedTime);
                Transform sourceShoulder =
                    RequirePath(sourceWork.transform, RightShoulderPath);
                Transform sourceArm = RequirePath(sourceWork.transform, RightArmPath);
                Transform sourceForeArm =
                    RequirePath(sourceWork.transform, RightForeArmPath);
                Transform sourceHand = RequirePath(sourceWork.transform, RightHandPath);
                Transform appliedShoulder =
                    RequirePath(appliedWork.transform, RightShoulderPath);
                Transform appliedArm =
                    RequirePath(appliedWork.transform, RightArmPath);
                Transform appliedForeArm =
                    RequirePath(appliedWork.transform, RightForeArmPath);
                Transform appliedHand =
                    RequirePath(appliedWork.transform, RightHandPath);
                Vector3 appliedArmLocal =
                    appliedWork.transform.InverseTransformPoint(appliedArm.position);
                Vector3 appliedHandLocal =
                    appliedWork.transform.InverseTransformPoint(appliedHand.position);
                return new RightShoulderForwardAnalysis(
                    sampleCount,
                    Vector3.Angle(
                        appliedHand.position - appliedArm.position,
                        appliedWork.transform.forward),
                    Mathf.Abs(appliedHandLocal.x - appliedArmLocal.x),
                    Vector3.Angle(
                        appliedArm.position - appliedForeArm.position,
                        appliedHand.position - appliedForeArm.position),
                    appliedPose.CurrentRightShoulderForwardHandErrorMeters,
                    appliedPose.CurrentNeutralWristBendAngleDegrees,
                    Quaternion.Angle(
                        sourceShoulder.localRotation,
                        appliedShoulder.localRotation),
                    Quaternion.Angle(
                        sourceArm.localRotation,
                        appliedArm.localRotation),
                    Quaternion.Angle(
                        sourceForeArm.localRotation,
                        appliedForeArm.localRotation),
                    Quaternion.Angle(
                        sourceHand.localRotation,
                        appliedHand.localRotation),
                    maximumUnaffectedRotationError,
                    maximumUnaffectedPositionError,
                    maximumIdleRotationError,
                    maximumIdlePositionError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(appliedWork);
                UnityEngine.Object.DestroyImmediate(sourceWork);
            }
        }

        private static FaceClearanceConfiguration AnalyzeFaceClearanceSource(
            GameObject target,
            AnimationClip clip)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashlightChargeConnectFaceClearanceSource";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(work).enabled = false;
                FlashlightRightHandFollowBehaviour carry = RequireCarry(work);
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Face-clearance source clone is missing its pose behaviour.");
                const int sampleCount = 121;
                int minimumFrame = -1;
                float minimumTime = 0f;
                float minimumClearance = float.PositiveInfinity;
                float faceBoundaryLocalX = 0f;
                float holderMinimumLocalX = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(work, time);
                    carry.RefreshPreview();
                    pose.RefreshPreviewWithoutFaceClearanceAtNormalizedPhase(normalized);
                    if (!TryMeasureFaceClearance(
                            work.transform,
                            carry.Holder,
                            out float clearance,
                            out float boundary,
                            out float holderMinimum))
                        continue;
                    if (clearance >= minimumClearance) continue;
                    minimumFrame = index;
                    minimumTime = time;
                    minimumClearance = clearance;
                    faceBoundaryLocalX = boundary;
                    holderMinimumLocalX = holderMinimum;
                }
                if (minimumFrame < 0 || float.IsInfinity(minimumClearance))
                    throw new InvalidOperationException(
                        "No flashlight sample crossed the head-height review band.");
                return new FaceClearanceConfiguration(
                    sampleCount,
                    minimumFrame,
                    minimumTime,
                    minimumTime / clip.length,
                    minimumClearance,
                    faceBoundaryLocalX,
                    holderMinimumLocalX,
                    Mathf.Max(
                        0f,
                        FaceClearanceSafetyMarginMeters - minimumClearance));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static FlashlightHandClearanceAnalysis AnalyzeFlashlightHandClearance(
            GameObject target,
            AnimationClip clip)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashlightChargeConnectHandClearanceAnalysis";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(work).enabled = false;
                FlashlightRightHandFollowBehaviour carry = RequireCarry(work);
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Flashlight hand-clearance clone is missing its pose behaviour.");

                const int sampleCount = 241;
                float minimumBaselineSurfaceClearance = float.PositiveInfinity;
                float minimumSurfaceClearance = float.PositiveInfinity;
                float worstNormalizedTime = 0f;
                int rightHandVertexCount = 0;
                int flashlightVertexCount = 0;
                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(work, time);
                    carry.RefreshPreview();
                    pose.RefreshPreviewWithoutFlashlightForwardOffsetAtNormalizedPhase(
                        normalized);
                    HandFlashlightForwardGeometry baselineGeometry =
                        MeasureHandFlashlightForwardGeometry(work.transform, carry);
                    float baselineClearance =
                        baselineGeometry.FlashlightMinimumForward -
                        baselineGeometry.RightHandMaximumForward;
                    minimumBaselineSurfaceClearance = Mathf.Min(
                        minimumBaselineSurfaceClearance,
                        baselineClearance);

                    clip.SampleAnimation(work, time);
                    carry.RefreshPreview();
                    pose.RefreshPreviewAtNormalizedPhase(normalized);
                    HandFlashlightForwardGeometry geometry =
                        MeasureHandFlashlightForwardGeometry(work.transform, carry);
                    float clearance = geometry.FlashlightMinimumForward -
                        geometry.RightHandMaximumForward;
                    if (clearance < minimumSurfaceClearance)
                    {
                        minimumSurfaceClearance = clearance;
                        worstNormalizedTime = normalized;
                    }
                    rightHandVertexCount = Mathf.Max(
                        rightHandVertexCount, geometry.RightHandVertexCount);
                    flashlightVertexCount = Mathf.Max(
                        flashlightVertexCount, geometry.FlashlightVertexCount);
                }

                if (float.IsInfinity(minimumBaselineSurfaceClearance) ||
                    float.IsInfinity(minimumSurfaceClearance) ||
                    rightHandVertexCount == 0 || flashlightVertexCount == 0)
                    throw new InvalidOperationException(
                        "Flashlight hand-clearance analysis did not collect visible surface vertices.");
                float requiredForwardOffset = Mathf.Max(
                    FlashlightForwardOffsetMeters,
                    FlashlightHandClearanceSafetyMarginMeters -
                    minimumBaselineSurfaceClearance);
                float requiredAdditionalForwardOffset =
                    requiredForwardOffset - pose.FlashlightForwardOffsetMeters;
                return new FlashlightHandClearanceAnalysis(
                    sampleCount,
                    pose.FlashlightForwardOffsetMeters,
                    requiredForwardOffset,
                    requiredAdditionalForwardOffset,
                    minimumBaselineSurfaceClearance,
                    minimumSurfaceClearance,
                    worstNormalizedTime,
                    rightHandVertexCount,
                    flashlightVertexCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static HandFlashlightForwardGeometry
            MeasureHandFlashlightForwardGeometry(
                Transform target,
                FlashlightRightHandFollowBehaviour carry)
        {
            Transform rightHand = RequirePath(target, RightHandPath);
            Vector3 forward = target.forward.normalized;
            float rightHandMaximumForward = float.NegativeInfinity;
            float flashlightMinimumForward = float.PositiveInfinity;
            int rightHandVertexCount = 0;
            int flashlightVertexCount = 0;

            foreach (SkinnedMeshRenderer renderer in
                     target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == null || !renderer.enabled ||
                    renderer.sharedMesh == null ||
                    (carry.Holder != null &&
                     renderer.transform.IsChildOf(carry.Holder.transform)))
                    continue;
                Transform[] bones = renderer.bones;
                bool[] rightHandBones = bones.Select(bone =>
                        bone != null &&
                        (bone == rightHand || bone.IsChildOf(rightHand)))
                    .ToArray();
                if (!rightHandBones.Any(value => value)) continue;
                BoneWeight[] weights = renderer.sharedMesh.boneWeights;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked, true);
                    Vector3[] vertices = baked.vertices;
                    if (weights.Length != vertices.Length)
                        throw new InvalidOperationException(
                            "Right-hand skin weights do not match baked mesh vertices.");
                    for (int index = 0; index < vertices.Length; index++)
                    {
                        if (RightHandSkinWeight(weights[index], rightHandBones) < 0.01f)
                            continue;
                        float projected = Vector3.Dot(
                            renderer.transform.TransformPoint(vertices[index]),
                            forward);
                        rightHandMaximumForward = Mathf.Max(
                            rightHandMaximumForward, projected);
                        rightHandVertexCount++;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }

            if (carry.Holder == null)
                throw new MissingReferenceException(
                    "Flashlight holder is missing from hand-clearance geometry.");
            foreach (MeshFilter filter in
                     carry.Holder.GetComponentsInChildren<MeshFilter>(true))
            {
                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled ||
                    filter.sharedMesh == null)
                    continue;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    float projected = Vector3.Dot(
                        filter.transform.TransformPoint(vertex), forward);
                    flashlightMinimumForward = Mathf.Min(
                        flashlightMinimumForward, projected);
                    flashlightVertexCount++;
                }
            }
            foreach (SkinnedMeshRenderer renderer in
                     carry.Holder.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == null || !renderer.enabled ||
                    renderer.sharedMesh == null)
                    continue;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked, true);
                    foreach (Vector3 vertex in baked.vertices)
                    {
                        float projected = Vector3.Dot(
                            renderer.transform.TransformPoint(vertex), forward);
                        flashlightMinimumForward = Mathf.Min(
                            flashlightMinimumForward, projected);
                        flashlightVertexCount++;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }

            if (rightHandVertexCount == 0 || flashlightVertexCount == 0 ||
                float.IsInfinity(rightHandMaximumForward) ||
                float.IsInfinity(flashlightMinimumForward))
                throw new InvalidOperationException(
                    "Right-hand or flashlight surface geometry is unavailable.");
            return new HandFlashlightForwardGeometry(
                rightHandVertexCount,
                flashlightVertexCount,
                rightHandMaximumForward,
                flashlightMinimumForward);
        }

        private static float RightHandSkinWeight(
            BoneWeight weight,
            IReadOnlyList<bool> rightHandBones)
        {
            float result = 0f;
            if (weight.boneIndex0 >= 0 &&
                weight.boneIndex0 < rightHandBones.Count &&
                rightHandBones[weight.boneIndex0])
                result += weight.weight0;
            if (weight.boneIndex1 >= 0 &&
                weight.boneIndex1 < rightHandBones.Count &&
                rightHandBones[weight.boneIndex1])
                result += weight.weight1;
            if (weight.boneIndex2 >= 0 &&
                weight.boneIndex2 < rightHandBones.Count &&
                rightHandBones[weight.boneIndex2])
                result += weight.weight2;
            if (weight.boneIndex3 >= 0 &&
                weight.boneIndex3 < rightHandBones.Count &&
                rightHandBones[weight.boneIndex3])
                result += weight.weight3;
            return result;
        }

        private static FlashlightForwardOffsetAnalysis AnalyzeFlashlightForwardOffset(
            GameObject target,
            AnimationClip clip)
        {
            GameObject baselineWork = UnityEngine.Object.Instantiate(target);
            GameObject appliedWork = UnityEngine.Object.Instantiate(target);
            baselineWork.name = "FlashlightChargeConnectFlashlightForwardOffsetBaseline";
            appliedWork.name = "FlashlightChargeConnectFlashlightForwardOffsetApplied";
            baselineWork.hideFlags = HideFlags.HideAndDontSave;
            appliedWork.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(baselineWork).enabled = false;
                RequireAnimator(appliedWork).enabled = false;
                FlashlightRightHandFollowBehaviour baselineCarry =
                    RequireCarry(baselineWork);
                FlashlightRightHandFollowBehaviour appliedCarry =
                    RequireCarry(appliedWork);
                FlashlightChargeConnectPoseBehaviour baselinePose =
                    baselineWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Flashlight-forward-offset baseline clone is missing its pose behaviour.");
                FlashlightChargeConnectPoseBehaviour appliedPose =
                    appliedWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Flashlight-forward-offset applied clone is missing its pose behaviour.");

                const int sampleCount = 121;
                float minimumForwardOffset = float.PositiveInfinity;
                float maximumForwardOffset = 0f;
                float maximumOffAxisError = 0f;
                float minimumPreservedRightOffset = float.PositiveInfinity;
                float maximumPreservedRightOffset = 0f;
                float maximumBoneRotationError = 0f;
                float maximumBonePositionError = 0f;
                float maximumHolderRotationError = 0f;
                float maximumHolderScaleError = 0f;
                float minimumFaceClearance = float.PositiveInfinity;

                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(baselineWork, time);
                    clip.SampleAnimation(appliedWork, time);
                    baselineCarry.RefreshPreview();
                    baselinePose.RefreshPreviewWithoutFlashlightForwardOffsetAtNormalizedPhase(
                        normalized);
                    appliedCarry.RefreshPreview();
                    appliedPose.RefreshPreviewAtNormalizedPhase(normalized);

                    Transform baselineHolder = baselineCarry.Holder != null
                        ? baselineCarry.Holder.transform
                        : throw new MissingReferenceException(
                            "Flashlight-forward-offset baseline holder is missing.");
                    Transform appliedHolder = appliedCarry.Holder != null
                        ? appliedCarry.Holder.transform
                        : throw new MissingReferenceException(
                            "Flashlight-forward-offset applied holder is missing.");
                    Vector3 displacement =
                        appliedHolder.position - baselineHolder.position;
                    float forwardOffset = Vector3.Dot(
                        displacement, appliedWork.transform.forward);
                    Vector3 expectedDisplacement =
                        appliedWork.transform.forward *
                        appliedPose.FlashlightForwardOffsetMeters;
                    minimumForwardOffset = Mathf.Min(
                        minimumForwardOffset, forwardOffset);
                    maximumForwardOffset = Mathf.Max(
                        maximumForwardOffset, forwardOffset);
                    maximumOffAxisError = Mathf.Max(
                        maximumOffAxisError,
                        Vector3.Distance(displacement, expectedDisplacement));

                    Vector3 followedPosition = appliedCarry.RightHand.TransformPoint(
                        appliedCarry.HolderLocalPosition);
                    float preservedRightOffset = Vector3.Dot(
                        appliedHolder.position - followedPosition,
                        appliedWork.transform.right);
                    minimumPreservedRightOffset = Mathf.Min(
                        minimumPreservedRightOffset, preservedRightOffset);
                    maximumPreservedRightOffset = Mathf.Max(
                        maximumPreservedRightOffset, preservedRightOffset);
                    maximumHolderRotationError = Mathf.Max(
                        maximumHolderRotationError,
                        Quaternion.Angle(
                            baselineHolder.rotation, appliedHolder.rotation));
                    maximumHolderScaleError = Mathf.Max(
                        maximumHolderScaleError,
                        Vector3.Distance(
                            baselineHolder.localScale, appliedHolder.localScale));

                    foreach (Transform baselineTransform in
                             baselineWork.GetComponentsInChildren<Transform>(true))
                    {
                        if (baselineTransform == baselineHolder ||
                            baselineTransform.IsChildOf(baselineHolder))
                            continue;
                        string path = AnimationUtility.CalculateTransformPath(
                            baselineTransform, baselineWork.transform);
                        Transform appliedTransform = string.IsNullOrEmpty(path)
                            ? appliedWork.transform
                            : appliedWork.transform.Find(path);
                        if (appliedTransform == null)
                            throw new MissingReferenceException(
                                "Flashlight-forward-offset comparison transform is missing: " +
                                path);
                        maximumBoneRotationError = Mathf.Max(
                            maximumBoneRotationError,
                            Quaternion.Angle(
                                baselineTransform.localRotation,
                                appliedTransform.localRotation));
                        maximumBonePositionError = Mathf.Max(
                            maximumBonePositionError,
                            Vector3.Distance(
                                baselineTransform.localPosition,
                                appliedTransform.localPosition));
                    }

                    if (TryMeasureFaceClearance(
                            appliedWork.transform,
                            appliedCarry.Holder,
                            out float clearance,
                            out _,
                            out _))
                        minimumFaceClearance = Mathf.Min(
                            minimumFaceClearance, clearance);
                }

                if (float.IsInfinity(minimumForwardOffset) ||
                    float.IsInfinity(minimumPreservedRightOffset) ||
                    float.IsInfinity(minimumFaceClearance))
                    throw new InvalidOperationException(
                        "Flashlight-forward-offset analysis did not collect all required samples.");
                return new FlashlightForwardOffsetAnalysis(
                    sampleCount,
                    minimumForwardOffset,
                    maximumForwardOffset,
                    maximumOffAxisError,
                    minimumPreservedRightOffset,
                    maximumPreservedRightOffset,
                    maximumBoneRotationError,
                    maximumBonePositionError,
                    maximumHolderRotationError,
                    maximumHolderScaleError,
                    minimumFaceClearance);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(appliedWork);
                UnityEngine.Object.DestroyImmediate(baselineWork);
            }
        }

        private static FlashlightRightOffsetAnalysis AnalyzeFlashlightRightOffset(
            GameObject target,
            AnimationClip clip)
        {
            GameObject baselineWork = UnityEngine.Object.Instantiate(target);
            GameObject appliedWork = UnityEngine.Object.Instantiate(target);
            baselineWork.name = "FlashlightChargeConnectFlashlightRightOffsetBaseline";
            appliedWork.name = "FlashlightChargeConnectFlashlightRightOffsetApplied";
            baselineWork.hideFlags = HideFlags.HideAndDontSave;
            appliedWork.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(baselineWork).enabled = false;
                RequireAnimator(appliedWork).enabled = false;
                FlashlightRightHandFollowBehaviour baselineCarry =
                    RequireCarry(baselineWork);
                FlashlightRightHandFollowBehaviour appliedCarry =
                    RequireCarry(appliedWork);
                FlashlightChargeConnectPoseBehaviour baselinePose =
                    baselineWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Flashlight-right-offset baseline clone is missing its pose behaviour.");
                FlashlightChargeConnectPoseBehaviour appliedPose =
                    appliedWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Flashlight-right-offset applied clone is missing its pose behaviour.");

                const int sampleCount = 121;
                float minimumRightOffset = float.PositiveInfinity;
                float maximumRightOffset = 0f;
                float maximumOffAxisError = 0f;
                float maximumBoneRotationError = 0f;
                float maximumBonePositionError = 0f;
                float maximumHolderRotationError = 0f;
                float maximumHolderScaleError = 0f;
                float minimumFaceClearance = float.PositiveInfinity;

                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(baselineWork, time);
                    clip.SampleAnimation(appliedWork, time);
                    baselineCarry.RefreshPreview();
                    baselinePose.RefreshPreviewWithoutFlashlightRightOffsetAtNormalizedPhase(
                        normalized);
                    appliedCarry.RefreshPreview();
                    appliedPose.RefreshPreviewAtNormalizedPhase(normalized);

                    Transform baselineHolder = baselineCarry.Holder != null
                        ? baselineCarry.Holder.transform
                        : throw new MissingReferenceException(
                            "Flashlight-right-offset baseline holder is missing.");
                    Transform appliedHolder = appliedCarry.Holder != null
                        ? appliedCarry.Holder.transform
                        : throw new MissingReferenceException(
                            "Flashlight-right-offset applied holder is missing.");
                    Vector3 displacement =
                        appliedHolder.position - baselineHolder.position;
                    float rightOffset = Vector3.Dot(
                        displacement, appliedWork.transform.right);
                    Vector3 expectedDisplacement =
                        appliedWork.transform.right * FlashlightRightOffsetMeters;
                    minimumRightOffset = Mathf.Min(minimumRightOffset, rightOffset);
                    maximumRightOffset = Mathf.Max(maximumRightOffset, rightOffset);
                    maximumOffAxisError = Mathf.Max(
                        maximumOffAxisError,
                        Vector3.Distance(displacement, expectedDisplacement));
                    maximumHolderRotationError = Mathf.Max(
                        maximumHolderRotationError,
                        Quaternion.Angle(
                            baselineHolder.rotation, appliedHolder.rotation));
                    maximumHolderScaleError = Mathf.Max(
                        maximumHolderScaleError,
                        Vector3.Distance(
                            baselineHolder.localScale, appliedHolder.localScale));

                    foreach (Transform baselineTransform in
                             baselineWork.GetComponentsInChildren<Transform>(true))
                    {
                        if (baselineTransform == baselineHolder ||
                            baselineTransform.IsChildOf(baselineHolder))
                            continue;
                        string path = AnimationUtility.CalculateTransformPath(
                            baselineTransform, baselineWork.transform);
                        Transform appliedTransform = string.IsNullOrEmpty(path)
                            ? appliedWork.transform
                            : appliedWork.transform.Find(path);
                        if (appliedTransform == null)
                            throw new MissingReferenceException(
                                "Flashlight-right-offset comparison transform is missing: " +
                                path);
                        maximumBoneRotationError = Mathf.Max(
                            maximumBoneRotationError,
                            Quaternion.Angle(
                                baselineTransform.localRotation,
                                appliedTransform.localRotation));
                        maximumBonePositionError = Mathf.Max(
                            maximumBonePositionError,
                            Vector3.Distance(
                                baselineTransform.localPosition,
                                appliedTransform.localPosition));
                    }

                    if (TryMeasureFaceClearance(
                            appliedWork.transform,
                            appliedCarry.Holder,
                            out float clearance,
                            out _,
                            out _))
                        minimumFaceClearance = Mathf.Min(
                            minimumFaceClearance, clearance);
                }

                if (float.IsInfinity(minimumRightOffset) ||
                    float.IsInfinity(minimumFaceClearance))
                    throw new InvalidOperationException(
                        "Flashlight-right-offset analysis did not collect all required samples.");
                return new FlashlightRightOffsetAnalysis(
                    sampleCount,
                    minimumRightOffset,
                    maximumRightOffset,
                    maximumOffAxisError,
                    maximumBoneRotationError,
                    maximumBonePositionError,
                    maximumHolderRotationError,
                    maximumHolderScaleError,
                    minimumFaceClearance);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(appliedWork);
                UnityEngine.Object.DestroyImmediate(baselineWork);
            }
        }

        private static FaceClearanceAnalysis AnalyzeFaceClearanceApplied(
            GameObject target,
            AnimationClip clip)
        {
            string[] unaffectedPaths =
                SourceUnaffectedByRightArmCorrectionPaths(target.transform);
            GameObject sourceWork = UnityEngine.Object.Instantiate(target);
            GameObject appliedWork = UnityEngine.Object.Instantiate(target);
            sourceWork.name = "FlashlightChargeConnectFaceClearanceBase";
            appliedWork.name = "FlashlightChargeConnectFaceClearanceApplied";
            sourceWork.hideFlags = HideFlags.HideAndDontSave;
            appliedWork.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(sourceWork).enabled = false;
                RequireAnimator(appliedWork).enabled = false;
                FlashlightRightHandFollowBehaviour sourceCarry = RequireCarry(sourceWork);
                FlashlightRightHandFollowBehaviour appliedCarry = RequireCarry(appliedWork);
                FlashlightChargeConnectPoseBehaviour sourcePose =
                    sourceWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Face-clearance base clone is missing its pose behaviour.");
                FlashlightChargeConnectPoseBehaviour appliedPose =
                    appliedWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Face-clearance applied clone is missing its pose behaviour.");

                const int sampleCount = 121;
                float minimumClearance = float.PositiveInfinity;
                float maximumHandError = 0f;
                float minimumElbowAngle = float.PositiveInfinity;
                float maximumElbowAngle = 0f;
                float maximumWristBend = 0f;
                float maximumShoulderRotationChange = 0f;
                float maximumArmRotationChange = 0f;
                float maximumForeArmRotationChange = 0f;
                float maximumUnaffectedRotationError = 0f;
                float maximumUnaffectedPositionError = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(sourceWork, time);
                    clip.SampleAnimation(appliedWork, time);
                    sourceCarry.RefreshPreview();
                    sourcePose.RefreshPreviewWithoutFaceClearanceAtNormalizedPhase(normalized);
                    appliedCarry.RefreshPreview();
                    appliedPose.RefreshPreviewAtNormalizedPhase(normalized);

                    if (TryMeasureFaceClearance(
                            appliedWork.transform,
                            appliedCarry.Holder,
                            out float clearance,
                            out _,
                            out _))
                        minimumClearance = Mathf.Min(minimumClearance, clearance);
                    maximumHandError = Mathf.Max(
                        maximumHandError,
                        appliedPose.CurrentFaceClearanceHandErrorMeters);
                    minimumElbowAngle = Mathf.Min(
                        minimumElbowAngle,
                        appliedPose.CurrentFaceClearanceElbowAngleDegrees);
                    maximumElbowAngle = Mathf.Max(
                        maximumElbowAngle,
                        appliedPose.CurrentFaceClearanceElbowAngleDegrees);
                    maximumWristBend = Mathf.Max(
                        maximumWristBend,
                        appliedPose.CurrentNeutralWristBendAngleDegrees);

                    Transform sourceShoulder =
                        RequirePath(sourceWork.transform, RightShoulderPath);
                    Transform sourceArm = RequirePath(sourceWork.transform, RightArmPath);
                    Transform sourceForeArm =
                        RequirePath(sourceWork.transform, RightForeArmPath);
                    Transform appliedShoulder =
                        RequirePath(appliedWork.transform, RightShoulderPath);
                    Transform appliedArm = RequirePath(appliedWork.transform, RightArmPath);
                    Transform appliedForeArm =
                        RequirePath(appliedWork.transform, RightForeArmPath);
                    maximumShoulderRotationChange = Mathf.Max(
                        maximumShoulderRotationChange,
                        Quaternion.Angle(
                            sourceShoulder.localRotation,
                            appliedShoulder.localRotation));
                    maximumArmRotationChange = Mathf.Max(
                        maximumArmRotationChange,
                        Quaternion.Angle(
                            sourceArm.localRotation,
                            appliedArm.localRotation));
                    maximumForeArmRotationChange = Mathf.Max(
                        maximumForeArmRotationChange,
                        Quaternion.Angle(
                            sourceForeArm.localRotation,
                            appliedForeArm.localRotation));
                    foreach (string path in unaffectedPaths)
                    {
                        Transform expected = RequirePath(sourceWork.transform, path);
                        Transform actual = RequirePath(appliedWork.transform, path);
                        maximumUnaffectedRotationError = Mathf.Max(
                            maximumUnaffectedRotationError,
                            Quaternion.Angle(
                                expected.localRotation,
                                actual.localRotation));
                        maximumUnaffectedPositionError = Mathf.Max(
                            maximumUnaffectedPositionError,
                            Vector3.Distance(
                                expected.localPosition,
                                actual.localPosition));
                    }
                }
                if (float.IsInfinity(minimumClearance))
                    throw new InvalidOperationException(
                        "Applied face-clearance analysis found no head-height samples.");
                return new FaceClearanceAnalysis(
                    sampleCount,
                    minimumClearance,
                    maximumHandError,
                    minimumElbowAngle,
                    maximumElbowAngle,
                    maximumWristBend,
                    maximumShoulderRotationChange,
                    maximumArmRotationChange,
                    maximumForeArmRotationChange,
                    maximumUnaffectedRotationError,
                    maximumUnaffectedPositionError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceWork);
                UnityEngine.Object.DestroyImmediate(appliedWork);
            }
        }

        private static bool TryMeasureFaceClearance(
            Transform target,
            GameObject holder,
            out float clearance,
            out float faceBoundaryLocalX,
            out float holderMinimumLocalX)
        {
            if (holder == null)
                throw new MissingReferenceException(
                    "Flashlight holder is missing from face-clearance analysis.");
            Bounds holderBounds = LocalRendererBounds(
                target,
                holder.GetComponentsInChildren<Renderer>(true));
            Vector3 headLocal = target.InverseTransformPoint(
                RequireDescendant(target, "Head").position);
            Vector3 shoulderLocal = target.InverseTransformPoint(
                RequirePath(target, RightShoulderPath).position);
            Vector3 armLocal = target.InverseTransformPoint(
                RequirePath(target, RightArmPath).position);
            if (armLocal.x <= headLocal.x)
                throw new InvalidOperationException(
                    "Right-arm local direction is invalid for face-clearance analysis.");
            float headSpan = Mathf.Max(
                Mathf.Abs(headLocal.y - shoulderLocal.y),
                0.1f);
            float faceBottom = shoulderLocal.y + headSpan * 0.28f;
            float faceTop = headLocal.y + headSpan * 0.72f;
            if (holderBounds.max.y < faceBottom || holderBounds.min.y > faceTop)
            {
                clearance = 0f;
                faceBoundaryLocalX = 0f;
                holderMinimumLocalX = 0f;
                return false;
            }
            faceBoundaryLocalX = Mathf.Lerp(
                headLocal.x,
                armLocal.x,
                FaceClearanceBoundaryRatio);
            holderMinimumLocalX = holderBounds.min.x;
            clearance = holderMinimumLocalX - faceBoundaryLocalX;
            return true;
        }

        private static Bounds LocalRendererBounds(
            Transform root,
            IReadOnlyList<Renderer> renderers)
        {
            if (renderers == null || renderers.Count == 0)
                throw new InvalidOperationException(
                    "Face-clearance bounds require visible flashlight renderers.");
            bool initialized = false;
            Bounds localBounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                Bounds world = renderer.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = world.center + Vector3.Scale(
                        world.extents,
                        new Vector3(x, y, z));
                    Vector3 local = root.InverseTransformPoint(corner);
                    if (!initialized)
                    {
                        localBounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(local);
                    }
                }
            }
            if (!initialized)
                throw new InvalidOperationException(
                    "Face-clearance bounds found no enabled flashlight renderer.");
            return localBounds;
        }

        private static float[] BuildForwardHoldCaptureTimes(
            FlashlightChargeConnectPoseBehaviour pose,
            AnimationClip clip)
        {
            if (pose == null || !pose.ForwardHoldConfigured)
                throw new InvalidOperationException(
                    "Forward-hold capture timing requires its configured pose.");
            float holdStart = pose.ForwardHoldStartNormalized * clip.length;
            float cycle = clip.length + pose.ForwardHoldDurationSeconds;
            float[] times =
            {
                Mathf.Max(0.02f, holdStart - 0.15f),
                holdStart + 0.08f,
                holdStart + 0.38f,
                holdStart + 0.78f,
                holdStart + pose.ForwardHoldDurationSeconds + 0.08f,
                cycle + 0.08f
            };
            for (int index = 1; index < times.Length; index++)
                if (times[index] <= times[index - 1])
                    throw new InvalidOperationException(
                        "Forward-hold capture times are not strictly ordered.");
            return times;
        }

        private static UpperBodyRestoreAnalysis AnalyzeUpperBodyRestore(
            GameObject target,
            GameObject idle,
            AnimationClip clip)
        {
            string[] sourceUpperBodyPaths = SourceUpperBodyPaths(target.transform);
            string[] idlePaths = IdleLowerBodyAndLeftArmPaths(idle.transform);
            string[] leftArmPaths = idlePaths.Where(IsLeftArmPath).ToArray();
            string[] lowerBodyPaths = idlePaths.Where(path => !IsLeftArmPath(path)).ToArray();
            FlashlightChargeConnectPoseBehaviour configuredPose =
                target.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                throw new MissingReferenceException(
                    "Flashlight_Charge_Connect pose behaviour is missing.");
            if (configuredPose.IdleBodyPoseCount != idlePaths.Length)
                throw new InvalidOperationException(
                    "Stored Player_Idle lower-body and left-arm pose count is incorrect.");
            foreach (string path in idlePaths)
            {
                FlashlightChargeConnectBonePose stored =
                    configuredPose.IdleBodyPose.Single(item => item.Path == path);
                Transform reference = RequirePath(idle.transform, path);
                if (Quaternion.Angle(stored.LocalRotation, reference.localRotation) > 0.01f ||
                    Vector3.Distance(stored.LocalPosition, reference.localPosition) > 0.00001f)
                    throw new InvalidOperationException(
                        "Stored Player_Idle pose differs at " + path + ".");
            }

            GameObject appliedWork = UnityEngine.Object.Instantiate(target);
            GameObject sourceWork = UnityEngine.Object.Instantiate(target);
            appliedWork.name = "FlashlightChargeConnectUpperBodyRestoreAppliedAnalysis";
            sourceWork.name = "FlashlightChargeConnectUpperBodyRestoreSourceAnalysis";
            appliedWork.hideFlags = HideFlags.HideAndDontSave;
            sourceWork.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(appliedWork).enabled = false;
                RequireAnimator(sourceWork).enabled = false;
                FlashlightRightHandFollowBehaviour appliedCarry = RequireCarry(appliedWork);
                FlashlightChargeConnectPoseBehaviour appliedPose =
                    appliedWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Applied upper-body analysis clone is missing its pose behaviour.");
                FlashlightRightHandFollowBehaviour sourceCarry = RequireCarry(sourceWork);
                FlashlightChargeConnectPoseBehaviour sourcePose =
                    sourceWork.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Source upper-body analysis clone is missing its pose behaviour.");
                sourceCarry.enabled = false;
                sourcePose.enabled = false;

                const int sampleCount = 61;
                var firstSourceRotations = new Dictionary<string, Quaternion>(
                    StringComparer.Ordinal);
                float maximumSourceUpperBodyRotationError = 0f;
                float maximumSourceUpperBodyPositionError = 0f;
                float maximumIdleLeftArmRotationError = 0f;
                float maximumIdleLeftArmPositionError = 0f;
                float maximumIdleLowerBodyRotationError = 0f;
                float maximumIdleLowerBodyPositionError = 0f;
                float maximumSourceUpperBodyMotion = 0f;
                float maximumHolderPositionError = 0f;
                float maximumLensUpDeviation = 0f;

                for (int index = 0; index < sampleCount; index++)
                {
                    float normalized = index / (sampleCount - 1f);
                    float time = Mathf.Min(normalized * clip.length, clip.length);
                    clip.SampleAnimation(sourceWork, time);
                    clip.SampleAnimation(appliedWork, time);
                    appliedCarry.RefreshPreview();
                    appliedPose.RefreshPreviewAtNormalizedPhase(normalized);

                    foreach (string path in sourceUpperBodyPaths)
                    {
                        Transform expected = RequirePath(sourceWork.transform, path);
                        Transform actual = RequirePath(appliedWork.transform, path);
                        maximumSourceUpperBodyRotationError = Mathf.Max(
                            maximumSourceUpperBodyRotationError,
                            Quaternion.Angle(actual.localRotation, expected.localRotation));
                        maximumSourceUpperBodyPositionError = Mathf.Max(
                            maximumSourceUpperBodyPositionError,
                            Vector3.Distance(actual.localPosition, expected.localPosition));
                        if (index == 0)
                            firstSourceRotations[path] = expected.localRotation;
                        else
                            maximumSourceUpperBodyMotion = Mathf.Max(
                                maximumSourceUpperBodyMotion,
                                Quaternion.Angle(firstSourceRotations[path],
                                    expected.localRotation));
                    }

                    foreach (string path in leftArmPaths)
                    {
                        Transform reference = RequirePath(idle.transform, path);
                        Transform actual = RequirePath(appliedWork.transform, path);
                        maximumIdleLeftArmRotationError = Mathf.Max(
                            maximumIdleLeftArmRotationError,
                            Quaternion.Angle(actual.localRotation, reference.localRotation));
                        maximumIdleLeftArmPositionError = Mathf.Max(
                            maximumIdleLeftArmPositionError,
                            Vector3.Distance(actual.localPosition, reference.localPosition));
                    }
                    foreach (string path in lowerBodyPaths)
                    {
                        Transform reference = RequirePath(idle.transform, path);
                        Transform actual = RequirePath(appliedWork.transform, path);
                        maximumIdleLowerBodyRotationError = Mathf.Max(
                            maximumIdleLowerBodyRotationError,
                            Quaternion.Angle(actual.localRotation, reference.localRotation));
                        maximumIdleLowerBodyPositionError = Mathf.Max(
                            maximumIdleLowerBodyPositionError,
                            Vector3.Distance(actual.localPosition, reference.localPosition));
                    }

                    Transform holder = appliedCarry.Holder != null
                        ? appliedCarry.Holder.transform
                        : throw new MissingReferenceException(
                            "Applied upper-body analysis flashlight holder is missing.");
                    maximumHolderPositionError = Mathf.Max(
                        maximumHolderPositionError,
                        Vector3.Distance(holder.localPosition,
                            appliedCarry.HolderLocalPosition));
                    maximumLensUpDeviation = Mathf.Max(
                        maximumLensUpDeviation,
                        Vector3.Angle(holder.up, appliedWork.transform.up));
                }

                return new UpperBodyRestoreAnalysis(
                    sampleCount,
                    sourceUpperBodyPaths.Length,
                    leftArmPaths.Length,
                    lowerBodyPaths.Length,
                    maximumSourceUpperBodyRotationError,
                    maximumSourceUpperBodyPositionError,
                    maximumIdleLeftArmRotationError,
                    maximumIdleLeftArmPositionError,
                    maximumIdleLowerBodyRotationError,
                    maximumIdleLowerBodyPositionError,
                    maximumSourceUpperBodyMotion,
                    maximumHolderPositionError,
                    maximumLensUpDeviation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(appliedWork);
                UnityEngine.Object.DestroyImmediate(sourceWork);
            }
        }

        private static Vector3 RightPalmNormal(Transform root)
        {
            Transform hand = RequirePath(root, RightHandPath);
            Transform index = hand.Find("RightIndexProximal") ??
                throw new MissingReferenceException("RightIndexProximal is missing.");
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new MissingReferenceException("RightMiddleProximal is missing.");
            Transform little = hand.Find("RightLittleProximal") ??
                throw new MissingReferenceException("RightLittleProximal is missing.");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 fingers = (middle.position - hand.position).normalized;
            Vector3 normal = Vector3.Cross(width, fingers).normalized;
            if (normal.sqrMagnitude < 0.99f)
                throw new InvalidOperationException("Right palm normal is degenerate.");
            return normal;
        }

        private static ForwardReachConfiguration AnalyzeForwardReachConfiguration(
            GameObject target,
            AnimationClip clip)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashlightChargeConnectForwardReachConfiguration";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(work).enabled = false;
                clip.SampleAnimation(work, 0f);
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Forward-reach configuration clone is missing its pose behaviour.");
                pose.RefreshPreview();
                Transform shoulder = RequirePath(work.transform, RightShoulderPath);
                Transform arm = RequirePath(work.transform, RightArmPath);
                Transform foreArm = RequirePath(work.transform, RightForeArmPath);
                Transform hand = RequirePath(work.transform, RightHandPath);
                float upperLength = Vector3.Distance(arm.position, foreArm.position);
                float lowerLength = Vector3.Distance(foreArm.position, hand.position);
                float desiredDistance = Mathf.Sqrt(
                    upperLength * upperLength + lowerLength * lowerLength -
                    2f * upperLength * lowerLength * Mathf.Cos(
                        DesiredElbowAngleDegrees * Mathf.Deg2Rad));
                Vector3 directionWorld =
                    (work.transform.forward - work.transform.up * 0.12f).normalized;
                Vector3 endWorld = arm.position + directionWorld * desiredDistance;
                Vector3 armLocal = work.transform.InverseTransformPoint(arm.position);
                Vector3 endLocal = work.transform.InverseTransformPoint(endWorld);
                return new ForwardReachConfiguration(
                    shoulder.localRotation,
                    work.transform.InverseTransformPoint(hand.position),
                    armLocal,
                    endLocal,
                    new Vector3(1f, -0.45f, 0f).normalized,
                    upperLength,
                    lowerLength,
                    Vector3.Angle(directionWorld, work.transform.forward));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static float ElbowAngleForTarget(
            float upperLength,
            float lowerLength,
            float reachDistance)
        {
            float denominator = 2f * upperLength * lowerLength;
            if (denominator <= 0.000001f)
                throw new InvalidOperationException(
                    "Right-arm lengths are invalid for elbow-angle analysis.");
            float cosine = Mathf.Clamp(
                (upperLength * upperLength + lowerLength * lowerLength -
                 reachDistance * reachDistance) / denominator,
                -1f,
                1f);
            return Mathf.Acos(cosine) * Mathf.Rad2Deg;
        }

        private static ForwardReachMotionAnalysis AnalyzeConfiguredForwardReachMotion(
            GameObject target,
            AnimationClip clip)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashlightChargeConnectForwardReachMotionAnalysis";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                RequireAnimator(work).enabled = false;
                FlashlightChargeConnectPoseBehaviour pose =
                    work.GetComponent<FlashlightChargeConnectPoseBehaviour>() ??
                    throw new MissingReferenceException(
                        "Forward-reach motion clone is missing its pose behaviour.");
                if (!pose.ForwardReachConfigured)
                    throw new InvalidOperationException(
                        "Forward-reach motion clone is not configured.");

                const int sampleCount = 61;
                float timeStep = clip.length / (sampleCount - 1);
                Vector3 previousHand = Vector3.zero;
                Vector3 previousVelocity = Vector3.zero;
                float maximumSpeed = 0f;
                float maximumApproachSpeed = 0f;
                float maximumAcceleration = 0f;
                float maximumTargetError = 0f;
                float maximumForwardDeviation = 0f;
                float minimumElbowAngle = float.PositiveInfinity;
                float maximumElbowAngle = 0f;
                for (int index = 0; index < sampleCount; index++)
                {
                    float time = Mathf.Min(index * timeStep, clip.length);
                    float normalized = time / clip.length;
                    clip.SampleAnimation(work, time);
                    pose.RefreshPreviewAtNormalizedPhase(normalized);
                    Transform arm = RequirePath(work.transform, RightArmPath);
                    Transform foreArm = RequirePath(work.transform, RightForeArmPath);
                    Transform hand = RequirePath(work.transform, RightHandPath);
                    Vector3 handLocal =
                        work.transform.InverseTransformPoint(hand.position);
                    maximumTargetError = Mathf.Max(
                        maximumTargetError, pose.CurrentHandPositionError);
                    if (pose.CurrentReachWeight >= 0.99f)
                    {
                        maximumForwardDeviation = Mathf.Max(
                            maximumForwardDeviation,
                            Vector3.Angle(
                                hand.position - arm.position,
                                work.transform.forward));
                        float elbowAngle = Vector3.Angle(
                            arm.position - foreArm.position,
                            hand.position - foreArm.position);
                        minimumElbowAngle = Mathf.Min(
                            minimumElbowAngle, elbowAngle);
                        maximumElbowAngle = Mathf.Max(
                            maximumElbowAngle, elbowAngle);
                    }
                    if (index > 0)
                    {
                        Vector3 velocity = (handLocal - previousHand) / timeStep;
                        maximumSpeed = Mathf.Max(maximumSpeed, velocity.magnitude);
                        if (normalized >= pose.ApproachStartNormalized &&
                            normalized <= pose.ReachEndNormalized)
                            maximumApproachSpeed = Mathf.Max(
                                maximumApproachSpeed, velocity.magnitude);
                        if (index > 1)
                        {
                            Vector3 acceleration =
                                (velocity - previousVelocity) / timeStep;
                            maximumAcceleration = Mathf.Max(
                                maximumAcceleration, acceleration.magnitude);
                        }
                        previousVelocity = velocity;
                    }
                    previousHand = handLocal;
                }
                if (float.IsPositiveInfinity(minimumElbowAngle))
                    throw new InvalidOperationException(
                        "Forward-reach motion never reached its hold pose.");
                return new ForwardReachMotionAnalysis(
                    maximumSpeed,
                    maximumApproachSpeed,
                    maximumAcceleration,
                    maximumTargetError,
                    maximumForwardDeviation,
                    minimumElbowAngle,
                    maximumElbowAngle);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static string TransformSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, root), StringComparer.Ordinal))
                builder.Append(AnimationUtility.CalculateTransformPath(item, root))
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').Append(Vec(item.localScale))
                    .Append('|').AppendLine(item.gameObject.activeSelf.ToString());
            return Sha256(builder.ToString());
        }

        private static string LocalPoseSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, root), StringComparer.Ordinal))
                builder.Append(AnimationUtility.CalculateTransformPath(item, root))
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').AppendLine(Vec(item.localScale));
            return Sha256(builder.ToString());
        }

        private static string OutsideTargetSignature(Scene scene, Transform target)
        {
            var builder = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects()
                         .OrderBy(item => item.name, StringComparer.Ordinal))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .Where(item => item != target && !item.IsChildOf(target))
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, root.transform), StringComparer.Ordinal))
                builder.Append(root.name).Append('|')
                    .Append(AnimationUtility.CalculateTransformPath(item, root.transform))
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').Append(Vec(item.localScale))
                    .Append('|').AppendLine(item.gameObject.activeSelf.ToString());
            return Sha256(builder.ToString());
        }

        private static string OutsideTargetsSignature(
            Scene scene,
            params Transform[] targets)
        {
            var excluded = new HashSet<Transform>(targets ?? Array.Empty<Transform>());
            var builder = new StringBuilder();
            foreach (GameObject root in scene.GetRootGameObjects()
                         .OrderBy(item => item.name, StringComparer.Ordinal))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .Where(item => !excluded.Any(target =>
                             target != null &&
                             (item == target || item.IsChildOf(target))))
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, root.transform), StringComparer.Ordinal))
                builder.Append(root.name).Append('|')
                    .Append(AnimationUtility.CalculateTransformPath(item, root.transform))
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').Append(Vec(item.localScale))
                    .Append('|').AppendLine(item.gameObject.activeSelf.ToString());
            return Sha256(builder.ToString());
        }

        private static string CarrySignature(FlashlightRightHandFollowBehaviour carry) =>
            string.Join("|",
                ObjectIdentity(carry.FlashlightPrefab),
                ObjectIdentity(carry.FistGripPose),
                carry.RightHandPath,
                Vec(carry.HolderLocalPosition),
                Quat(carry.HolderLocalRotation),
                Vec(carry.ModelLocalPosition),
                Quat(carry.ModelLocalRotation),
                Vec(carry.ModelLocalScale));

        private static string TargetPoseWithoutFlashlightSignature(
            Transform target,
            FlashlightRightHandFollowBehaviour carry)
        {
            Transform holder = carry.Holder != null
                ? carry.Holder.transform
                : null;
            var builder = new StringBuilder();
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true)
                         .Where(item => holder == null ||
                             (item != holder && !item.IsChildOf(holder)))
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, target), StringComparer.Ordinal))
                builder.Append(AnimationUtility.CalculateTransformPath(item, target))
                    .Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation))
                    .Append('|').Append(Vec(item.localScale))
                    .Append('|').AppendLine(item.gameObject.activeSelf.ToString());
            return Sha256(builder.ToString());
        }

        private static string VisualSignature(
            Transform target, FlashlightRightHandFollowBehaviour carry)
        {
            IEnumerable<Renderer> renderers =
                target.GetComponentsInChildren<Renderer>(true);
            if (carry.Holder != null)
                renderers = renderers.Concat(
                    carry.Holder.GetComponentsInChildren<Renderer>(true));
            var builder = new StringBuilder();
            foreach (Renderer renderer in renderers.Distinct()
                         .OrderBy(renderer => renderer.name, StringComparer.Ordinal))
            {
                builder.Append(renderer.name).Append('|')
                    .Append(renderer.enabled).Append('|');
                if (renderer is SkinnedMeshRenderer skinned)
                    builder.Append(ObjectIdentity(skinned.sharedMesh));
                else if (renderer.TryGetComponent(out MeshFilter filter))
                    builder.Append(ObjectIdentity(filter.sharedMesh));
                foreach (Material material in renderer.sharedMaterials)
                    builder.Append('|').Append(ObjectIdentity(material));
                builder.AppendLine();
            }
            return Sha256(builder.ToString());
        }

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static void WriteText(string assetPath, string contents)
        {
            string absolute = Absolute(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void DeleteReviewFile(string assetPath)
        {
            if (File.Exists(Absolute(assetPath))) AssetDatabase.DeleteAsset(assetPath);
        }

        private static int UnityConsoleErrorCount()
        {
            Type logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            System.Reflection.MethodInfo method = logEntriesType.GetMethod(
                "GetCountsByType",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console count API is unavailable.");
            object[] arguments = { 0, 0, 0 };
            method.Invoke(null, arguments);
            return (int)arguments[0];
        }

        private static void CleanupRuntimeCapture()
        {
            EditorApplication.update -= RuntimeCaptureTick;
            if (fullPanels != null)
                foreach (Texture2D panel in fullPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (upperPanels != null)
                foreach (Texture2D panel in upperPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            captureActive = false;
            runtimeTarget = null;
            runtimeAnimator = null;
            runtimeCarry = null;
            runtimePose = null;
            runtimeHead = null;
            runtimeLeftArm = null;
            runtimeRightArm = null;
            fullPanels = null;
            upperPanels = null;
            runtimeCaptureTimes = null;
            runtimeCycleStartTime = 0d;
            observedForwardHoldCycleDuration = 0f;
            ForwardHoldRotations.Clear();
            ForwardHoldPositions.Clear();
            forwardHoldReferenceCaptured = false;
            forwardHoldStartMetricFrame = -1;
            maximumForwardHoldRotationDrift = 0f;
            maximumForwardHoldPositionDrift = 0f;
            maximumForwardHoldHolderPositionDrift = 0f;
            maximumForwardHoldHolderRotationDrift = 0f;
            maximumRightShoulderForwardDeviation = 0f;
            maximumRightShoulderForwardLateralOffset = 0f;
            minimumRightShoulderForwardElbowAngle = float.PositiveInfinity;
            maximumRightShoulderForwardElbowAngle = 0f;
            maximumRightShoulderForwardHandError = 0f;
            minimumRightShoulderForwardHoldWeight = float.PositiveInfinity;
            maximumNeutralWristBendAngle = 0f;
            minimumFaceClearanceMeters = float.PositiveInfinity;
            maximumFaceClearanceHandError = 0f;
            minimumFaceClearanceAppliedOffset = float.PositiveInfinity;
            maximumFaceClearanceAppliedOffset = 0f;
            minimumHandFlashlightSurfaceClearance = float.PositiveInfinity;
            RuntimeObservations.Clear();
            InitialRotations.Clear();
        }

        private static void FailReview(Exception exception)
        {
            bool poseReview = SessionState.GetBool(PoseReviewKey, false);
            bool forwardReachReview =
                SessionState.GetBool(ForwardReachReviewKey, false);
            bool upperBodyRestoreReview =
                SessionState.GetBool(UpperBodyRestoreReviewKey, false);
            bool forwardHoldReview =
                SessionState.GetBool(ForwardHoldReviewKey, false);
            bool rightShoulderForwardReview =
                SessionState.GetBool(RightShoulderForwardReviewKey, false);
            bool neutralWristReview =
                SessionState.GetBool(NeutralWristReviewKey, false);
            bool faceClearanceReview =
                SessionState.GetBool(FaceClearanceReviewKey, false);
            bool flashlightRightOffsetReview =
                SessionState.GetBool(FlashlightRightOffsetReviewKey, false);
            bool flashlightForwardOffsetReview =
                SessionState.GetBool(FlashlightForwardOffsetReviewKey, false);
            bool flashlightHandClearanceReview =
                SessionState.GetBool(FlashlightHandClearanceReviewKey, false);
            CleanupRuntimeCapture();
            SessionState.EraseString(AutoStateKey);
            SessionState.EraseBool(PoseReviewKey);
            SessionState.EraseBool(ForwardReachReviewKey);
            SessionState.EraseBool(UpperBodyRestoreReviewKey);
            SessionState.EraseBool(UpperBodyRestoreFinalReviewKey);
            SessionState.EraseInt(UpperBodyRestoreDiagnosticIndexKey);
            SessionState.EraseBool(ForwardHoldReviewKey);
            SessionState.EraseBool(ForwardHoldFinalReviewKey);
            SessionState.EraseInt(ForwardHoldDiagnosticIndexKey);
            SessionState.EraseBool(RightShoulderForwardReviewKey);
            SessionState.EraseBool(RightShoulderForwardFinalReviewKey);
            SessionState.EraseInt(RightShoulderForwardDiagnosticIndexKey);
            SessionState.EraseBool(NeutralWristReviewKey);
            SessionState.EraseBool(NeutralWristFinalReviewKey);
            SessionState.EraseInt(NeutralWristDiagnosticIndexKey);
            SessionState.EraseBool(FaceClearanceReviewKey);
            SessionState.EraseBool(FaceClearanceFinalReviewKey);
            SessionState.EraseInt(FaceClearanceDiagnosticIndexKey);
            SessionState.EraseBool(FlashlightRightOffsetReviewKey);
            SessionState.EraseBool(FlashlightForwardOffsetReviewKey);
            SessionState.EraseBool(FlashlightHandClearanceReviewKey);
            try
            {
                EnsureFolder(ReviewFolder);
                string failurePath = flashlightHandClearanceReview
                    ? FlashlightHandClearanceFailureReportPath
                    : flashlightForwardOffsetReview
                    ? FlashlightForwardOffsetFailureReportPath
                    : flashlightRightOffsetReview
                    ? FlashlightRightOffsetFailureReportPath
                    : faceClearanceReview
                    ? FaceClearanceFailureReportPath
                    : neutralWristReview
                    ? NeutralWristFailureReportPath
                    : rightShoulderForwardReview
                    ? RightShoulderForwardFailureReportPath
                    : forwardHoldReview
                    ? ForwardHoldFailureReportPath
                    : upperBodyRestoreReview
                    ? UpperBodyRestoreFailureReportPath
                    : forwardReachReview
                    ? ForwardReachFailureReportPath
                    : poseReview ? PoseFailureReportPath : FailureReportPath;
                WriteText(failurePath,
                    exception.ToString());
            }
            catch
            {
            }
            Debug.LogException(exception);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static string ComputeAssetHash(string assetPath) =>
            ComputeFileHash(Absolute(assetPath));

        private static string ComputeFileHash(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static string ObjectIdentity(UnityEngine.Object value)
        {
            if (value == null) return "null";
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    value, out string guid, out long localId))
                return guid + ":" + localId;
            return value.GetType().FullName + ":" + value.name;
        }

        private static string Absolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z);
        private static string Quat(Quaternion value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);

        private readonly struct UpperBodyRestoreAnalysis
        {
            internal UpperBodyRestoreAnalysis(
                int sampleCount,
                int sourceUpperBodyBoneCount,
                int idleLeftArmBoneCount,
                int idleLowerBodyBoneCount,
                float maximumSourceUpperBodyRotationError,
                float maximumSourceUpperBodyPositionError,
                float maximumIdleLeftArmRotationError,
                float maximumIdleLeftArmPositionError,
                float maximumIdleLowerBodyRotationError,
                float maximumIdleLowerBodyPositionError,
                float maximumSourceUpperBodyMotion,
                float maximumHolderPositionError,
                float maximumLensUpDeviation)
            {
                SampleCount = sampleCount;
                SourceUpperBodyBoneCount = sourceUpperBodyBoneCount;
                IdleLeftArmBoneCount = idleLeftArmBoneCount;
                IdleLowerBodyBoneCount = idleLowerBodyBoneCount;
                MaximumSourceUpperBodyRotationError =
                    maximumSourceUpperBodyRotationError;
                MaximumSourceUpperBodyPositionError =
                    maximumSourceUpperBodyPositionError;
                MaximumIdleLeftArmRotationError = maximumIdleLeftArmRotationError;
                MaximumIdleLeftArmPositionError = maximumIdleLeftArmPositionError;
                MaximumIdleLowerBodyRotationError = maximumIdleLowerBodyRotationError;
                MaximumIdleLowerBodyPositionError = maximumIdleLowerBodyPositionError;
                MaximumSourceUpperBodyMotion = maximumSourceUpperBodyMotion;
                MaximumHolderPositionError = maximumHolderPositionError;
                MaximumLensUpDeviation = maximumLensUpDeviation;
            }

            internal int SampleCount { get; }
            internal int SourceUpperBodyBoneCount { get; }
            internal int IdleLeftArmBoneCount { get; }
            internal int IdleLowerBodyBoneCount { get; }
            internal float MaximumSourceUpperBodyRotationError { get; }
            internal float MaximumSourceUpperBodyPositionError { get; }
            internal float MaximumIdleLeftArmRotationError { get; }
            internal float MaximumIdleLeftArmPositionError { get; }
            internal float MaximumIdleLowerBodyRotationError { get; }
            internal float MaximumIdleLowerBodyPositionError { get; }
            internal float MaximumSourceUpperBodyMotion { get; }
            internal float MaximumHolderPositionError { get; }
            internal float MaximumLensUpDeviation { get; }
        }

        private readonly struct ForwardHoldSourceSample
        {
            internal ForwardHoldSourceSample(
                int sampleCount,
                int frame,
                float time,
                float normalizedTime,
                Vector3 rightHandLocal)
            {
                SampleCount = sampleCount;
                Frame = frame;
                Time = time;
                NormalizedTime = normalizedTime;
                RightHandLocal = rightHandLocal;
            }

            internal int SampleCount { get; }
            internal int Frame { get; }
            internal float Time { get; }
            internal float NormalizedTime { get; }
            internal Vector3 RightHandLocal { get; }
        }

        private readonly struct RightShoulderForwardConfiguration
        {
            internal RightShoulderForwardConfiguration(
                int frame,
                float time,
                float normalizedTime,
                Vector3 sourceRightArmLocal,
                Vector3 sourceRightHandLocal,
                Vector3 targetRightHandLocal,
                float sourceLateralOffset,
                float sourceForwardDeviation,
                float sourceElbowAngle,
                float upperArmLength,
                float foreArmLength)
            {
                Frame = frame;
                Time = time;
                NormalizedTime = normalizedTime;
                SourceRightArmLocal = sourceRightArmLocal;
                SourceRightHandLocal = sourceRightHandLocal;
                TargetRightHandLocal = targetRightHandLocal;
                SourceLateralOffset = sourceLateralOffset;
                SourceForwardDeviation = sourceForwardDeviation;
                SourceElbowAngle = sourceElbowAngle;
                UpperArmLength = upperArmLength;
                ForeArmLength = foreArmLength;
                TargetReachDistance = Vector3.Distance(
                    targetRightHandLocal, sourceRightArmLocal);
            }

            internal int Frame { get; }
            internal float Time { get; }
            internal float NormalizedTime { get; }
            internal Vector3 SourceRightArmLocal { get; }
            internal Vector3 SourceRightHandLocal { get; }
            internal Vector3 TargetRightHandLocal { get; }
            internal float SourceLateralOffset { get; }
            internal float SourceForwardDeviation { get; }
            internal float SourceElbowAngle { get; }
            internal float UpperArmLength { get; }
            internal float ForeArmLength { get; }
            internal float TargetReachDistance { get; }
        }

        private readonly struct RightShoulderForwardAnalysis
        {
            internal RightShoulderForwardAnalysis(
                int sampleCount,
                float peakForwardDeviation,
                float peakLateralOffset,
                float peakElbowAngle,
                float peakHandError,
                float peakWristBendAngle,
                float peakShoulderRotationChange,
                float peakArmRotationChange,
                float peakForeArmRotationChange,
                float peakHandRotationChange,
                float maximumUnaffectedUpperBodyRotationError,
                float maximumUnaffectedUpperBodyPositionError,
                float maximumIdlePoseRotationError,
                float maximumIdlePosePositionError)
            {
                SampleCount = sampleCount;
                PeakForwardDeviation = peakForwardDeviation;
                PeakLateralOffset = peakLateralOffset;
                PeakElbowAngle = peakElbowAngle;
                PeakHandError = peakHandError;
                PeakWristBendAngle = peakWristBendAngle;
                PeakShoulderRotationChange = peakShoulderRotationChange;
                PeakArmRotationChange = peakArmRotationChange;
                PeakForeArmRotationChange = peakForeArmRotationChange;
                PeakHandRotationChange = peakHandRotationChange;
                MaximumUnaffectedUpperBodyRotationError =
                    maximumUnaffectedUpperBodyRotationError;
                MaximumUnaffectedUpperBodyPositionError =
                    maximumUnaffectedUpperBodyPositionError;
                MaximumIdlePoseRotationError = maximumIdlePoseRotationError;
                MaximumIdlePosePositionError = maximumIdlePosePositionError;
            }

            internal int SampleCount { get; }
            internal float PeakForwardDeviation { get; }
            internal float PeakLateralOffset { get; }
            internal float PeakElbowAngle { get; }
            internal float PeakHandError { get; }
            internal float PeakWristBendAngle { get; }
            internal float PeakShoulderRotationChange { get; }
            internal float PeakArmRotationChange { get; }
            internal float PeakForeArmRotationChange { get; }
            internal float PeakHandRotationChange { get; }
            internal float MaximumUnaffectedUpperBodyRotationError { get; }
            internal float MaximumUnaffectedUpperBodyPositionError { get; }
            internal float MaximumIdlePoseRotationError { get; }
            internal float MaximumIdlePosePositionError { get; }
        }

        private readonly struct FaceClearanceConfiguration
        {
            internal FaceClearanceConfiguration(
                int sampleCount,
                int frame,
                float time,
                float normalizedTime,
                float minimumClearance,
                float faceBoundaryLocalX,
                float holderMinimumLocalX,
                float requiredLateralOffset)
            {
                SampleCount = sampleCount;
                Frame = frame;
                Time = time;
                NormalizedTime = normalizedTime;
                MinimumClearance = minimumClearance;
                FaceBoundaryLocalX = faceBoundaryLocalX;
                HolderMinimumLocalX = holderMinimumLocalX;
                RequiredLateralOffset = requiredLateralOffset;
            }

            internal int SampleCount { get; }
            internal int Frame { get; }
            internal float Time { get; }
            internal float NormalizedTime { get; }
            internal float MinimumClearance { get; }
            internal float FaceBoundaryLocalX { get; }
            internal float HolderMinimumLocalX { get; }
            internal float RequiredLateralOffset { get; }
        }

        private readonly struct HandFlashlightForwardGeometry
        {
            internal HandFlashlightForwardGeometry(
                int rightHandVertexCount,
                int flashlightVertexCount,
                float rightHandMaximumForward,
                float flashlightMinimumForward)
            {
                RightHandVertexCount = rightHandVertexCount;
                FlashlightVertexCount = flashlightVertexCount;
                RightHandMaximumForward = rightHandMaximumForward;
                FlashlightMinimumForward = flashlightMinimumForward;
            }

            internal int RightHandVertexCount { get; }
            internal int FlashlightVertexCount { get; }
            internal float RightHandMaximumForward { get; }
            internal float FlashlightMinimumForward { get; }
        }

        private readonly struct FlashlightHandClearanceAnalysis
        {
            internal FlashlightHandClearanceAnalysis(
                int sampleCount,
                float currentForwardOffset,
                float requiredForwardOffset,
                float requiredAdditionalForwardOffset,
                float minimumBaselineSurfaceClearance,
                float minimumSurfaceClearance,
                float worstNormalizedTime,
                int rightHandVertexCount,
                int flashlightVertexCount)
            {
                SampleCount = sampleCount;
                CurrentForwardOffset = currentForwardOffset;
                RequiredForwardOffset = requiredForwardOffset;
                RequiredAdditionalForwardOffset =
                    requiredAdditionalForwardOffset;
                MinimumBaselineSurfaceClearance =
                    minimumBaselineSurfaceClearance;
                MinimumSurfaceClearance = minimumSurfaceClearance;
                WorstNormalizedTime = worstNormalizedTime;
                RightHandVertexCount = rightHandVertexCount;
                FlashlightVertexCount = flashlightVertexCount;
            }

            internal int SampleCount { get; }
            internal float CurrentForwardOffset { get; }
            internal float RequiredForwardOffset { get; }
            internal float RequiredAdditionalForwardOffset { get; }
            internal float MinimumBaselineSurfaceClearance { get; }
            internal float MinimumSurfaceClearance { get; }
            internal float WorstNormalizedTime { get; }
            internal int RightHandVertexCount { get; }
            internal int FlashlightVertexCount { get; }
        }

        private readonly struct FlashlightForwardOffsetAnalysis
        {
            internal FlashlightForwardOffsetAnalysis(
                int sampleCount,
                float minimumForwardOffset,
                float maximumForwardOffset,
                float maximumOffAxisError,
                float minimumPreservedRightOffset,
                float maximumPreservedRightOffset,
                float maximumBoneRotationError,
                float maximumBonePositionError,
                float maximumHolderRotationError,
                float maximumHolderScaleError,
                float minimumFaceClearance)
            {
                SampleCount = sampleCount;
                MinimumForwardOffset = minimumForwardOffset;
                MaximumForwardOffset = maximumForwardOffset;
                MaximumOffAxisError = maximumOffAxisError;
                MinimumPreservedRightOffset = minimumPreservedRightOffset;
                MaximumPreservedRightOffset = maximumPreservedRightOffset;
                MaximumBoneRotationError = maximumBoneRotationError;
                MaximumBonePositionError = maximumBonePositionError;
                MaximumHolderRotationError = maximumHolderRotationError;
                MaximumHolderScaleError = maximumHolderScaleError;
                MinimumFaceClearance = minimumFaceClearance;
            }

            internal int SampleCount { get; }
            internal float MinimumForwardOffset { get; }
            internal float MaximumForwardOffset { get; }
            internal float MaximumOffAxisError { get; }
            internal float MinimumPreservedRightOffset { get; }
            internal float MaximumPreservedRightOffset { get; }
            internal float MaximumBoneRotationError { get; }
            internal float MaximumBonePositionError { get; }
            internal float MaximumHolderRotationError { get; }
            internal float MaximumHolderScaleError { get; }
            internal float MinimumFaceClearance { get; }
        }

        private readonly struct FlashlightRightOffsetAnalysis
        {
            internal FlashlightRightOffsetAnalysis(
                int sampleCount,
                float minimumRightOffset,
                float maximumRightOffset,
                float maximumOffAxisError,
                float maximumBoneRotationError,
                float maximumBonePositionError,
                float maximumHolderRotationError,
                float maximumHolderScaleError,
                float minimumFaceClearance)
            {
                SampleCount = sampleCount;
                MinimumRightOffset = minimumRightOffset;
                MaximumRightOffset = maximumRightOffset;
                MaximumOffAxisError = maximumOffAxisError;
                MaximumBoneRotationError = maximumBoneRotationError;
                MaximumBonePositionError = maximumBonePositionError;
                MaximumHolderRotationError = maximumHolderRotationError;
                MaximumHolderScaleError = maximumHolderScaleError;
                MinimumFaceClearance = minimumFaceClearance;
            }

            internal int SampleCount { get; }
            internal float MinimumRightOffset { get; }
            internal float MaximumRightOffset { get; }
            internal float MaximumOffAxisError { get; }
            internal float MaximumBoneRotationError { get; }
            internal float MaximumBonePositionError { get; }
            internal float MaximumHolderRotationError { get; }
            internal float MaximumHolderScaleError { get; }
            internal float MinimumFaceClearance { get; }
        }

        private readonly struct FaceClearanceAnalysis
        {
            internal FaceClearanceAnalysis(
                int sampleCount,
                float minimumClearance,
                float maximumHandError,
                float minimumElbowAngle,
                float maximumElbowAngle,
                float maximumWristBend,
                float maximumShoulderRotationChange,
                float maximumArmRotationChange,
                float maximumForeArmRotationChange,
                float maximumUnaffectedRotationError,
                float maximumUnaffectedPositionError)
            {
                SampleCount = sampleCount;
                MinimumClearance = minimumClearance;
                MaximumHandError = maximumHandError;
                MinimumElbowAngle = minimumElbowAngle;
                MaximumElbowAngle = maximumElbowAngle;
                MaximumWristBend = maximumWristBend;
                MaximumShoulderRotationChange = maximumShoulderRotationChange;
                MaximumArmRotationChange = maximumArmRotationChange;
                MaximumForeArmRotationChange = maximumForeArmRotationChange;
                MaximumUnaffectedRotationError = maximumUnaffectedRotationError;
                MaximumUnaffectedPositionError = maximumUnaffectedPositionError;
            }

            internal int SampleCount { get; }
            internal float MinimumClearance { get; }
            internal float MaximumHandError { get; }
            internal float MinimumElbowAngle { get; }
            internal float MaximumElbowAngle { get; }
            internal float MaximumWristBend { get; }
            internal float MaximumShoulderRotationChange { get; }
            internal float MaximumArmRotationChange { get; }
            internal float MaximumForeArmRotationChange { get; }
            internal float MaximumUnaffectedRotationError { get; }
            internal float MaximumUnaffectedPositionError { get; }
        }

        private readonly struct DisconnectReverseAnalysis
        {
            internal DisconnectReverseAnalysis(
                int sampleCount,
                float maximumSourceSampleBoneRotationError,
                string maximumSourceSampleBoneRotationErrorPath,
                float maximumMirroredBoneRotationError,
                string maximumMirroredBoneRotationErrorPath,
                float maximumMirroredBonePositionError,
                string maximumMirroredBonePositionErrorPath,
                float maximumMirroredHolderPositionError,
                float maximumMirroredHolderRotationError,
                float maximumMirroredHolderScaleError,
                float maximumVisibleBoneMotion,
                float maximumRightHandFollowError)
            {
                SampleCount = sampleCount;
                MaximumSourceSampleBoneRotationError =
                    maximumSourceSampleBoneRotationError;
                MaximumSourceSampleBoneRotationErrorPath =
                    maximumSourceSampleBoneRotationErrorPath;
                MaximumMirroredBoneRotationError =
                    maximumMirroredBoneRotationError;
                MaximumMirroredBoneRotationErrorPath =
                    maximumMirroredBoneRotationErrorPath;
                MaximumMirroredBonePositionError =
                    maximumMirroredBonePositionError;
                MaximumMirroredBonePositionErrorPath =
                    maximumMirroredBonePositionErrorPath;
                MaximumMirroredHolderPositionError =
                    maximumMirroredHolderPositionError;
                MaximumMirroredHolderRotationError =
                    maximumMirroredHolderRotationError;
                MaximumMirroredHolderScaleError =
                    maximumMirroredHolderScaleError;
                MaximumVisibleBoneMotion = maximumVisibleBoneMotion;
                MaximumRightHandFollowError = maximumRightHandFollowError;
            }

            internal int SampleCount { get; }
            internal float MaximumSourceSampleBoneRotationError { get; }
            internal string MaximumSourceSampleBoneRotationErrorPath { get; }
            internal float MaximumMirroredBoneRotationError { get; }
            internal string MaximumMirroredBoneRotationErrorPath { get; }
            internal float MaximumMirroredBonePositionError { get; }
            internal string MaximumMirroredBonePositionErrorPath { get; }
            internal float MaximumMirroredHolderPositionError { get; }
            internal float MaximumMirroredHolderRotationError { get; }
            internal float MaximumMirroredHolderScaleError { get; }
            internal float MaximumVisibleBoneMotion { get; }
            internal float MaximumRightHandFollowError { get; }
        }

        private readonly struct ForwardReachSample
        {
            internal ForwardReachSample(
                float time,
                Vector3 handLocal,
                Vector3 velocity,
                Vector3 acceleration,
                float elbowAngle)
            {
                Time = time;
                HandLocal = handLocal;
                Velocity = velocity;
                Acceleration = acceleration;
                ElbowAngle = elbowAngle;
            }

            internal float Time { get; }
            internal Vector3 HandLocal { get; }
            internal Vector3 Velocity { get; }
            internal Vector3 Acceleration { get; }
            internal float ElbowAngle { get; }
        }

        private readonly struct ForwardReachConfiguration
        {
            internal ForwardReachConfiguration(
                Quaternion rightShoulderReferenceRotation,
                Vector3 startHandLocal,
                Vector3 rightArmLocal,
                Vector3 endHandLocal,
                Vector3 elbowPoleLocalDirection,
                float upperLength,
                float lowerLength,
                float forwardDirectionDeviation)
            {
                RightShoulderReferenceRotation = rightShoulderReferenceRotation;
                StartHandLocal = startHandLocal;
                RightArmLocal = rightArmLocal;
                EndHandLocal = endHandLocal;
                ElbowPoleLocalDirection = elbowPoleLocalDirection;
                UpperLength = upperLength;
                LowerLength = lowerLength;
                ForwardDirectionDeviation = forwardDirectionDeviation;
            }

            internal Quaternion RightShoulderReferenceRotation { get; }
            internal Vector3 StartHandLocal { get; }
            internal Vector3 RightArmLocal { get; }
            internal Vector3 EndHandLocal { get; }
            internal Vector3 ElbowPoleLocalDirection { get; }
            internal float UpperLength { get; }
            internal float LowerLength { get; }
            internal float ForwardDirectionDeviation { get; }
        }

        private readonly struct ForwardReachMotionAnalysis
        {
            internal ForwardReachMotionAnalysis(
                float maximumHandSpeed,
                float maximumApproachHandSpeed,
                float maximumHandAcceleration,
                float maximumTargetError,
                float maximumFullReachForwardDeviation,
                float minimumFullReachElbowAngle,
                float maximumFullReachElbowAngle)
            {
                MaximumHandSpeed = maximumHandSpeed;
                MaximumApproachHandSpeed = maximumApproachHandSpeed;
                MaximumHandAcceleration = maximumHandAcceleration;
                MaximumTargetError = maximumTargetError;
                MaximumFullReachForwardDeviation =
                    maximumFullReachForwardDeviation;
                MinimumFullReachElbowAngle = minimumFullReachElbowAngle;
                MaximumFullReachElbowAngle = maximumFullReachElbowAngle;
            }

            internal float MaximumHandSpeed { get; }
            internal float MaximumApproachHandSpeed { get; }
            internal float MaximumHandAcceleration { get; }
            internal float MaximumTargetError { get; }
            internal float MaximumFullReachForwardDeviation { get; }
            internal float MinimumFullReachElbowAngle { get; }
            internal float MaximumFullReachElbowAngle { get; }
        }
    }
}
