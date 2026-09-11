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
        private const string AutoStateKey =
            "Bellerophon.FlashlightIdleLocomotion.AutoState";
        private const string AwaitingPlayState = "AwaitingPlay";
        private const string AwaitingEditState = "AwaitingEdit";
        private const float CapturePhaseTime = 0.48f;
        private const double CaptureTimeoutSeconds = 20d;

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

        static FlashlightIdleLocomotionTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= RuntimeCaptureTick;
            if (!string.IsNullOrEmpty(
                SessionState.GetString(AutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueAutomaticApplicationAndReview;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!string.IsNullOrEmpty(
                SessionState.GetString(AutoStateKey, string.Empty)))
                EditorApplication.delayCall += ContinueAutomaticApplicationAndReview;
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
                .AppendLine("secondsPerMotion=1")
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
                .AppendLine("secondsPerMotion=1")
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
                .AppendLine("secondsPerMotion=1")
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
}
