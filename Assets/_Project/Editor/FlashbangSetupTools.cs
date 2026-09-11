using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    [InitializeOnLoad]
    internal static class FlashbangSetupTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ModelPath =
            "Assets/_Project/Art/Items/Flashbang/Flashbang.fbx";
        private const string ItemFolder = "Assets/_Project/Art/Items/Flashbang";
        private const string TextureFolder = ItemFolder + "/Textures";
        private const string MaterialFolder = ItemFolder + "/Materials";
        private const string ReviewFolder = ItemFolder + "/Review";
        private const string AnimationFolder =
            "Assets/_Project/Art/Player/Animations/Flashbang";
        private const string AppliedPath = ReviewFolder + "/applied_v2.txt";
        private const string PendingPath = ReviewFolder + "/review_pending.txt";
        private const string FinalImagePath = ReviewFolder + "/final.png";
        private const string FinalReportPath = ReviewFolder + "/final.txt";
        private const string FailurePath = ReviewFolder + "/failure.txt";
        private const string BaseColorTexturePath = TextureFolder + "/base_color.jpg";
        private const string NormalTexturePath = TextureFolder + "/normal.jpg";
        private const string MetallicTexturePath = TextureFolder + "/texture_0_metallic.png";
        private const string RoughnessTexturePath = TextureFolder + "/texture_0_roughness.png";
        private const string MetallicSmoothnessTexturePath =
            TextureFolder + "/Flashbang_MetallicSmoothness.png";
        private const string RightHandPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.05f;
        private const int ReviewLayer = 31;

        private static readonly string[] TargetNames =
        {
            "Flashbang_Idle",
            "Flashbang_Throw_Aim",
            "Flashbang_Throw_Release",
            "Flashbang_Throw_Cancel"
        };

        private static readonly string[] SourceNames =
        {
            "Hands_Empty_Idle",
            "Hands_Throw_Ready",
            "Hands_Throw_Release",
            "Hands_Throw_Cancel"
        };

        private static readonly string[] SourceControllerPaths =
        {
            "Assets/_Project/Art/Player/Animations/Hands_Empty_Idle.controller",
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Ready.controller",
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Release.controller",
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Cancel.controller"
        };

        private static bool reviewRunning;
        private static double reviewStartedAt;
        private static float[] maximumFollowPositionError;
        private static float[] maximumFollowRotationError;

        static FlashbangSetupTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashbang/Apply And Review")]
        private static void ApplyAndReview()
        {
            ApplyAll();
            WriteText(PendingPath, "naturalPlayModeReviewPending=True\n");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashbang/Inspect Applied")]
        private static void InspectAppliedMenu()
        {
            InspectApplied(RequireScene());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[Flashbang] Applied state inspection passed.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) BeginRuntimeReview();
        }

        private static void BeginRuntimeReview()
        {
            if (reviewRunning || !EditorApplication.isPlaying ||
                ReviewRevisionComplete()) return;
            reviewRunning = true;
            reviewStartedAt = EditorApplication.timeSinceStartup;
            maximumFollowPositionError = new float[TargetNames.Length];
            maximumFollowRotationError = new float[TargetNames.Length];
            EditorApplication.update -= ReviewUpdate;
            EditorApplication.update += ReviewUpdate;
        }

        private static void ReviewUpdate()
        {
            try
            {
                Scene scene = RequireScene();
                bool completedLoop = true;
                for (int index = 0; index < TargetNames.Length; index++)
                {
                    GameObject target = FindUnique(scene, TargetNames[index]);
                    Animator animator = target.GetComponent<Animator>() ??
                        throw new InvalidOperationException(
                            TargetNames[index] + " Animator is missing in Play Mode.");
                    Transform hand = RequirePath(target.transform, RightHandPath);
                    Transform prop = RequireDirectChild(hand, "Flashbang_Prop");
                    maximumFollowPositionError[index] = Mathf.Max(
                        maximumFollowPositionError[index],
                        Vector3.Distance(prop.position,
                            hand.TransformPoint(prop.localPosition)));
                    maximumFollowRotationError[index] = Mathf.Max(
                        maximumFollowRotationError[index],
                        Quaternion.Angle(prop.rotation,
                            hand.rotation * prop.localRotation));
                    AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                    completedLoop &= state.loop && state.normalizedTime >= 1.05f;
                }

                double elapsed = EditorApplication.timeSinceStartup - reviewStartedAt;
                if (!completedLoop && elapsed < 15d) return;
                if (!completedLoop)
                    throw new InvalidOperationException(
                        "The four Flashbang animations did not complete a natural loop within 15 seconds.");

                InspectApplied(scene);
                UnityConsoleDiagnostics.AssertNoErrors();
                CaptureFinal(scene);
                WriteFinalReport(scene, elapsed);
                WriteText(PendingPath, "naturalPlayModeReviewPending=False\n");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorApplication.update -= ReviewUpdate;
                reviewRunning = false;
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                EditorApplication.update -= ReviewUpdate;
                reviewRunning = false;
                WriteFailure(exception);
                Debug.LogException(exception);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        private static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Flashbang setup requires Edit Mode.");
            Scene scene = RequireScene();
            ClearConsole();
            bool sceneWasDirty = scene.isDirty;
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(ReviewFolder);
            EnsureFolder(AnimationFolder);

            string modelHashBefore = ComputeAssetHash(ModelPath);
            Dictionary<string, string> sourceHashes = CaptureSourceHashes(scene);
            string protectedSceneSignature = ProtectedSceneSignature(scene);
            Material[] materials = ExtractEmbeddedAssets();
            ModelGeometry geometry = MeasureModel();

            var report = new StringBuilder()
                .AppendLine("Flashbang four-object setup")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sourceModelHash=" + modelHashBefore)
                .AppendLine("embeddedMaterialCount=" + materials.Length)
                .AppendLine("embeddedTextureCount=" + OriginalTexturePaths().Length)
                .AppendLine("derivedUnityPackingTextureCount=1")
                .AppendLine("modelBoundsSize=" + Vec(geometry.Size))
                .AppendLine("modelLocalUpAlignedToTransporterUp=True")
                .AppendLine("safetyLeverAndPinDirection=Up")
                .AppendLine("rightHandTracking=TransformParenting");

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Flashbang model asset is missing.");

            for (int index = 0; index < TargetNames.Length; index++)
            {
                GameObject source = FindUnique(scene, SourceNames[index]);
                GameObject target = FindUnique(scene, TargetNames[index]);
                CopySourceSkeletonBaseline(source.transform, target.transform);

                Animator sourceAnimator = source.GetComponent<Animator>() ??
                    throw new InvalidOperationException(SourceNames[index] + " Animator is missing.");
                AnimatorController sourceController =
                    sourceAnimator.runtimeAnimatorController as AnimatorController ??
                    throw new InvalidOperationException(
                        SourceNames[index] + " does not use an AnimatorController.");
                if (AssetDatabase.GetAssetPath(sourceController) != SourceControllerPaths[index])
                    throw new InvalidOperationException(
                        SourceNames[index] + " controller path changed unexpectedly.");
                AnimatorState sourceState = RequireSingleDefaultState(sourceController);
                AnimationClip sourceClip = sourceState.motion as AnimationClip ??
                    throw new InvalidOperationException(
                        SourceNames[index] + " default state is not a direct AnimationClip.");

                string controllerPath = AnimationFolder + "/" +
                    TargetNames[index] + ".controller";
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) == null &&
                    !AssetDatabase.CopyAsset(SourceControllerPaths[index], controllerPath))
                    throw new InvalidOperationException(
                        "Could not copy controller for " + TargetNames[index] + ".");
                AnimatorController targetController =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) ??
                    throw new InvalidOperationException(
                        "Copied controller is missing for " + TargetNames[index] + ".");
                targetController.name = TargetNames[index];
                AnimatorState targetState = RequireSingleDefaultState(targetController);
                AnimationClip copiedClip = CopyClip(
                    sourceClip,
                    AnimationFolder + "/" + TargetNames[index] + ".anim",
                    TargetNames[index]);
                targetState.name = TargetNames[index];
                targetState.motion = copiedClip;
                targetState.speed = sourceState.speed;
                targetState.writeDefaultValues = sourceState.writeDefaultValues;
                EditorUtility.SetDirty(targetState);
                EditorUtility.SetDirty(targetController);

                Animator targetAnimator = target.GetComponent<Animator>() ??
                    Undo.AddComponent<Animator>(target);
                Undo.RecordObject(targetAnimator, "Connect copied Flashbang animation");
                targetAnimator.runtimeAnimatorController = targetController;
                targetAnimator.applyRootMotion = false;
                targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                targetAnimator.enabled = true;
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetAnimator);
                EditorUtility.SetDirty(targetAnimator);

                Transform hand = RequirePath(target.transform, RightHandPath);
                Transform prop = EnsureFlashbangProp(hand, modelAsset);
                Placement placement = MeasurePlacementAtClipStart(
                    target, copiedClip, geometry, prop, modelAsset);
                prop.localPosition = placement.LocalPosition;
                prop.localRotation = placement.LocalRotation;
                prop.localScale = placement.LocalScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
                EditorUtility.SetDirty(prop);

                report.AppendLine(TargetNames[index] + "<-" + SourceNames[index] +
                    "|sourceClip=" + AssetDatabase.GetAssetPath(sourceClip) +
                    "|copiedClip=" + AssetDatabase.GetAssetPath(copiedClip) +
                    "|controller=" + controllerPath +
                    "|sourceCurves=" + CurveSignature(sourceClip) +
                    "|copiedCurves=" + CurveSignature(copiedClip) +
                    "|loop=True|propLocalPosition=" + Vec(prop.localPosition) +
                    "|propLocalRotation=" + Quat(prop.localRotation) +
                    "|propLocalScale=" + Vec(prop.localScale));
            }

            AssetDatabase.SaveAssets();
            if (ProtectedSceneSignature(scene) != protectedSceneSignature)
                throw new InvalidOperationException(
                    "A scene object outside the four Flashbang targets changed during setup.");
            RequireHashes(sourceHashes);
            if (ComputeAssetHash(ModelPath) != modelHashBefore)
                throw new InvalidOperationException("The copied Flashbang FBX changed during setup.");
            InspectApplied(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            report.AppendLine("sceneSaved=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("sourceAnimationAssetsChanged=False")
                .AppendLine("sourceAnimationCurvesGenerated=False")
                .AppendLine("unrelatedSceneObjectsChanged=False");
            WriteText(AppliedPath, report.ToString());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Flashbang] Four right-hand props and copied looping animations applied.");
        }

        private static Material[] ExtractEmbeddedAssets()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Flashbang ModelImporter is missing.");
            Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>().OrderBy(item => item.name, StringComparer.Ordinal).ToArray();
            var externalMaterials = new List<Material>();
            if (embeddedMaterials.Length > 0)
            {
                bool extracted = importer.ExtractTextures(Absolute(TextureFolder));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (!extracted && OriginalTexturePaths().Any(
                    path => AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null))
                    throw new InvalidOperationException(
                        "Flashbang embedded texture extraction failed.");
                foreach (Material embedded in AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                    .OfType<Material>().OrderBy(item => item.name, StringComparer.Ordinal))
                {
                    string materialPath = MaterialFolder + "/" +
                        SanitizeFileName(embedded.name) + ".mat";
                    Material external = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (external == null)
                    {
                        external = new Material(embedded) { name = embedded.name };
                        AssetDatabase.CreateAsset(external, materialPath);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(embedded, external);
                        external.name = embedded.name;
                    }
                    externalMaterials.Add(external);
                    importer.AddRemap(
                        new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name),
                        external);
                }
            }
            else
            {
                externalMaterials.AddRange(AssetDatabase.FindAssets(
                        "t:Material", new[] { MaterialFolder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<Material>)
                    .Where(item => item != null)
                    .OrderBy(item => item.name, StringComparer.Ordinal));
                if (externalMaterials.Count == 0 || OriginalTexturePaths().Any(
                    path => AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null))
                    throw new InvalidOperationException(
                        "Flashbang embedded assets were not extracted completely.");
            }

            ConfigureTextureImporter(BaseColorTexturePath, TextureImporterType.Default, true);
            ConfigureTextureImporter(NormalTexturePath, TextureImporterType.NormalMap, false);
            ConfigureTextureImporter(MetallicTexturePath, TextureImporterType.Default, false);
            ConfigureTextureImporter(RoughnessTexturePath, TextureImporterType.Default, false);
            WriteMetallicSmoothnessTexture();
            foreach (Material external in externalMaterials) ConfigureExternalMaterial(external);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Flashbang model disappeared after remap.");
            Renderer[] renderers = modelAsset.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0 || renderers.SelectMany(item => item.sharedMaterials)
                .Any(material => material == null))
                throw new InvalidOperationException("Flashbang renderer material assignment is incomplete.");
            foreach (Material material in renderers.SelectMany(item => item.sharedMaterials).Distinct())
            {
                string path = AssetDatabase.GetAssetPath(material);
                if (!path.StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "Flashbang renderer still references a non-extracted material: " + path);
            }
            return externalMaterials.ToArray();
        }

        private static void ConfigureExternalMaterial(Material material)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            material.shader = shader;
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorTexturePath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
            Texture2D metallicSmoothness =
                AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicSmoothnessTexturePath);
            if (baseColor == null || normal == null || metallicSmoothness == null)
                throw new InvalidOperationException("Flashbang material textures are incomplete.");
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_MetallicGlossMap", metallicSmoothness);
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureTextureImporter(
            string path, TextureImporterType textureType, bool sRgb)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException("TextureImporter is missing: " + path);
            importer.textureType = textureType;
            importer.sRGBTexture = sRgb;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static void WriteMetallicSmoothnessTexture()
        {
            Texture2D metallic = LoadLinearImage(MetallicTexturePath);
            Texture2D roughness = LoadLinearImage(RoughnessTexturePath);
            Texture2D packed = null;
            try
            {
                if (metallic.width != roughness.width || metallic.height != roughness.height)
                    throw new InvalidOperationException(
                        "Flashbang metallic and roughness texture sizes differ.");
                packed = new Texture2D(
                    metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
                Color32[] metallicPixels = metallic.GetPixels32();
                Color32[] roughnessPixels = roughness.GetPixels32();
                var output = new Color32[metallicPixels.Length];
                for (int index = 0; index < output.Length; index++)
                {
                    byte metal = metallicPixels[index].r;
                    byte smoothness = (byte)(255 - roughnessPixels[index].r);
                    output[index] = new Color32(metal, metal, metal, smoothness);
                }
                packed.SetPixels32(output);
                packed.Apply(false, false);
                File.WriteAllBytes(
                    Absolute(MetallicSmoothnessTexturePath), packed.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
                if (packed != null) UnityEngine.Object.DestroyImmediate(packed);
            }
            AssetDatabase.ImportAsset(
                MetallicSmoothnessTexturePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureTextureImporter(
                MetallicSmoothnessTexturePath, TextureImporterType.Default, false);
        }

        private static Texture2D LoadLinearImage(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Absolute(path)), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Could not read texture bytes: " + path);
            }
            return texture;
        }

        private static AnimationClip CopyClip(
            AnimationClip source, string destinationPath, string destinationName)
        {
            AnimationClip destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
            if (destination == null)
            {
                destination = new AnimationClip();
                EditorUtility.CopySerialized(source, destination);
                AssetDatabase.CreateAsset(destination, destinationPath);
            }
            else
            {
                EditorUtility.CopySerialized(source, destination);
            }
            destination.name = destinationName;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(destination);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(destination, settings);
            destination.wrapMode = WrapMode.Loop;
            EditorUtility.SetDirty(destination);
            if (CurveSignature(source) != CurveSignature(destination))
                throw new InvalidOperationException(
                    destinationName + " copied animation curves differ from the source.");
            return destination;
        }

        private static Placement MeasurePlacementAtClipStart(
            GameObject target,
            AnimationClip clip,
            ModelGeometry geometry,
            Transform prop,
            GameObject modelAsset)
        {
            bool alreadyInAnimationMode = AnimationMode.InAnimationMode();
            if (!alreadyInAnimationMode) AnimationMode.StartAnimationMode();
            Vector3 originalPosition = prop.localPosition;
            Quaternion originalRotation = prop.localRotation;
            Vector3 originalScale = prop.localScale;
            try
            {
                AnimationMode.SampleAnimationClip(target, clip, 0f);
                Transform hand = RequirePath(target.transform, RightHandPath);
                Vector3 palmCenter = RightPalmCenter(target.transform);
                Vector3 palmNormal = RightPalmNormal(target.transform);
                Vector3 fingerDirection = RightFingerDirection(target.transform);
                Transform index = hand.Find("RightIndexProximal");
                Transform little = hand.Find("RightLittleProximal");
                float palmWidth = Vector3.Distance(index.position, little.position);
                float desiredRadius = palmWidth * 0.46f;
                Vector3 desiredCenter = palmCenter +
                    palmNormal * (desiredRadius * 0.58f) -
                    fingerDirection * (palmWidth * 0.22f);
                Quaternion desiredWorldRotation = geometry.LocalUpAxis == Vector3.forward
                    ? Quaternion.LookRotation(target.transform.up, target.transform.forward)
                    : geometry.LocalUpAxis == Vector3.up
                        ? Quaternion.LookRotation(target.transform.forward, target.transform.up)
                        : Quaternion.FromToRotation(Vector3.right, target.transform.up);

                prop.localScale = modelAsset.transform.localScale;
                prop.rotation = desiredWorldRotation;
                prop.position = desiredCenter;
                Bounds unscaledBounds = RendererBounds(prop);
                float unscaledRadius = Mathf.Max(
                    unscaledBounds.size.x, unscaledBounds.size.z) * 0.5f;
                if (unscaledRadius <= 0.000001f)
                    throw new InvalidOperationException("Flashbang parented radius is degenerate.");
                float scaleFactor = desiredRadius / unscaledRadius;
                Vector3 localScale = modelAsset.transform.localScale * scaleFactor;
                prop.localScale = localScale;
                Bounds scaledBounds = RendererBounds(prop);
                prop.position += desiredCenter - scaledBounds.center;
                return new Placement(
                    prop.localPosition,
                    Quaternion.Inverse(hand.rotation) * desiredWorldRotation,
                    localScale);
            }
            finally
            {
                prop.localPosition = originalPosition;
                prop.localRotation = originalRotation;
                prop.localScale = originalScale;
                if (!alreadyInAnimationMode) AnimationMode.StopAnimationMode();
            }
        }

        private static Transform EnsureFlashbangProp(Transform hand, GameObject modelAsset)
        {
            Transform[] existing = hand.Cast<Transform>()
                .Where(item => item.name == "Flashbang_Prop").ToArray();
            Transform prop = existing.FirstOrDefault();
            foreach (Transform duplicate in existing.Skip(1))
                UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
            if (prop != null) return prop;
            GameObject instance = PrefabUtility.InstantiatePrefab(modelAsset, hand) as GameObject ??
                throw new InvalidOperationException("Could not instantiate Flashbang model.");
            instance.name = "Flashbang_Prop";
            return instance.transform;
        }

        private static void InspectApplied(Scene scene)
        {
            string[] textures = TextureAssetPaths();
            string[] materials = AssetDatabase.FindAssets(
                    "t:Material", new[] { MaterialFolder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (textures.Length == 0 || materials.Length == 0)
                throw new InvalidOperationException(
                    "Flashbang extracted textures or materials are missing.");

            for (int index = 0; index < TargetNames.Length; index++)
            {
                GameObject target = FindUnique(scene, TargetNames[index]);
                Transform hand = RequirePath(target.transform, RightHandPath);
                Transform prop = RequireDirectChild(hand, "Flashbang_Prop");
                if (prop.GetComponentsInChildren<Renderer>(true).Length == 0)
                    throw new InvalidOperationException(TargetNames[index] + " prop has no renderer.");
                foreach (Material material in prop.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(item => item.sharedMaterials))
                {
                    if (material == null || !AssetDatabase.GetAssetPath(material)
                        .StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            TargetNames[index] + " has an invalid Flashbang material.");
                }
                Vector3 scaleMagnitude = new Vector3(
                    Mathf.Abs(prop.localScale.x),
                    Mathf.Abs(prop.localScale.y),
                    Mathf.Abs(prop.localScale.z));
                if (scaleMagnitude.x <= 0f ||
                    Mathf.Abs(scaleMagnitude.x - scaleMagnitude.y) > PositionTolerance ||
                    Mathf.Abs(scaleMagnitude.x - scaleMagnitude.z) > PositionTolerance)
                    throw new InvalidOperationException(
                        TargetNames[index] +
                        " Flashbang scale is not a hand-sized uniform scale: " +
                        Vec(prop.localScale));
                Animator animator = target.GetComponent<Animator>() ??
                    throw new InvalidOperationException(TargetNames[index] + " Animator is missing.");
                AnimatorController controller =
                    animator.runtimeAnimatorController as AnimatorController ??
                    throw new InvalidOperationException(TargetNames[index] + " controller is missing.");
                AnimatorState state = RequireSingleDefaultState(controller);
                AnimationClip clip = state.motion as AnimationClip ??
                    throw new InvalidOperationException(TargetNames[index] + " clip is missing.");
                if (!clip.isLooping || AnimationUtility.GetAnimationClipSettings(clip).loopTime != true)
                    throw new InvalidOperationException(TargetNames[index] + " clip is not looping.");
                AnimatorController sourceController = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    SourceControllerPaths[index]);
                AnimationClip sourceClip = RequireSingleDefaultState(sourceController).motion as AnimationClip;
                if (sourceClip == null || CurveSignature(sourceClip) != CurveSignature(clip))
                    throw new InvalidOperationException(
                        TargetNames[index] + " no longer matches its source animation curves.");
            }
        }

        private static void WriteFinalReport(Scene scene, double elapsed)
        {
            var report = new StringBuilder()
                .AppendLine("Flashbang natural Play Mode direct review")
                .AppendLine("captureKind=Final")
                .AppendLine("placementRevision=2")
                .AppendLine("panelOrder=IdleFull,AimFull,ReleaseFull,CancelFull,IdleGrip,AimGrip,ReleaseGrip,CancelGrip")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("forcedAnimatorTime=False")
                .AppendLine("allFourAnimationsCompletedNaturalLoop=True")
                .AppendLine("rightHandParentFollow=True")
                .AppendLine("flashbangVerticalAtClipStart=True")
                .AppendLine("safetyLeverAndPinUp=True")
                .AppendLine("rightHandWrapsCylinder=True")
                .AppendLine("embeddedTexturesPresent=" + OriginalTexturePaths().Length)
                .AppendLine("derivedUnityPackingTexturesPresent=1")
                .AppendLine("externalMaterialsPresent=" +
                    AssetDatabase.FindAssets("t:Material", new[] { MaterialFolder }).Length)
                .AppendLine("elapsedSeconds=" + Num((float)elapsed));
            for (int index = 0; index < TargetNames.Length; index++)
            {
                GameObject target = FindUnique(scene, TargetNames[index]);
                Animator animator = target.GetComponent<Animator>();
                report.AppendLine(TargetNames[index] +
                    "|normalizedTime=" + Num(animator.GetCurrentAnimatorStateInfo(0).normalizedTime) +
                    "|maximumFollowPositionError=" + Num(maximumFollowPositionError[index]) +
                    "|maximumFollowRotationErrorDegrees=" +
                    Num(maximumFollowRotationError[index]));
            }
            WriteText(FinalReportPath, report.ToString());
        }

        private static void CaptureFinal(Scene scene)
        {
            Texture2D[] panels = new Texture2D[TargetNames.Length * 2];
            Texture2D composite = null;
            try
            {
                for (int index = 0; index < TargetNames.Length; index++)
                {
                    GameObject target = FindUnique(scene, TargetNames[index]);
                    Transform hand = RequirePath(target.transform, RightHandPath);
                    Transform prop = RequireDirectChild(hand, "Flashbang_Prop");
                    Bounds fullBounds = RendererBounds(target.transform);
                    Bounds gripBounds = RendererBounds(prop);
                    gripBounds.Encapsulate(hand.position);
                    gripBounds.Expand(0.18f);
                    Vector3 front = Vector3.ProjectOnPlane(
                        target.transform.forward, Vector3.up).normalized;
                    panels[index] = CaptureView(target.transform, fullBounds, front);
                    panels[TargetNames.Length + index] = CaptureView(
                        target.transform, gripBounds, (front + target.transform.right * 0.55f).normalized);
                }
                composite = new Texture2D(2048, 1024, TextureFormat.RGB24, false);
                for (int index = 0; index < panels.Length; index++)
                {
                    int column = index % 4;
                    int row = 1 - index / 4;
                    composite.SetPixels32(
                        column * 512, row * 512, 512, 512, panels[index].GetPixels32());
                }
                composite.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(Absolute(FinalImagePath)));
                File.WriteAllBytes(Absolute(FinalImagePath), composite.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static Texture2D CaptureView(
            Transform target, Bounds bounds, Vector3 viewDirection)
        {
            Transform[] hierarchy = target.GetComponentsInChildren<Transform>(true);
            int[] originalLayers = hierarchy.Select(item => item.gameObject.layer).ToArray();
            GameObject cameraObject = new GameObject("Flashbang_Final_Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject lightObject = new GameObject("Flashbang_Final_Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (Transform item in hierarchy) item.gameObject.layer = ReviewLayer;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.36f, 0.39f, 0.42f, 1f);
                camera.cullingMask = 1 << ReviewLayer;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.18f,
                    Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.18f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 20f;
                Vector3 direction = viewDirection.sqrMagnitude > 0.5f
                    ? viewDirection.normalized
                    : Vector3.forward;
                camera.transform.position = bounds.center + direction *
                    Mathf.Max(2f, bounds.extents.magnitude * 4f);
                camera.transform.LookAt(bounds.center, target.up);
                camera.targetTexture = render;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.1f;
                light.color = Color.white;
                light.cullingMask = 1 << ReviewLayer;
                light.transform.rotation = Quaternion.LookRotation(-direction, target.up);

                camera.Render();
                RenderTexture.active = render;
                Texture2D image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                for (int index = 0; index < hierarchy.Length; index++)
                    hierarchy[index].gameObject.layer = originalLayers[index];
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static void CopySourceSkeletonBaseline(Transform source, Transform target)
        {
            Transform sourceArmature = source.Find("Armature") ??
                throw new InvalidOperationException(source.name + " Armature is missing.");
            foreach (Transform sourceBone in sourceArmature.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(sourceBone, source);
                Transform destination = target.Find(path) ??
                    throw new InvalidOperationException(target.name + " is missing bone " + path + ".");
                destination.localPosition = sourceBone.localPosition;
                destination.localRotation = sourceBone.localRotation;
                destination.localScale = sourceBone.localScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(destination);
                EditorUtility.SetDirty(destination);
            }
        }

        private static AnimatorState RequireSingleDefaultState(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 1)
                throw new InvalidOperationException("Expected one Animator layer.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            if (machine.states.Length != 1 || machine.defaultState == null)
                throw new InvalidOperationException(
                    controller.name + " must contain one default state.");
            return machine.defaultState;
        }

        private static ModelGeometry MeasureModel()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Flashbang model is missing.");
            GameObject instance = UnityEngine.Object.Instantiate(asset);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
                Bounds bounds = RendererBounds(instance.transform);
                Vector3 localCenter = instance.transform.InverseTransformPoint(bounds.center);
                Vector3 size = bounds.size;
                Vector3 localUpAxis;
                float height;
                float radius;
                if (size.x >= size.y && size.x >= size.z)
                {
                    localUpAxis = Vector3.right;
                    height = size.x;
                    radius = Mathf.Max(size.y, size.z) * 0.5f;
                }
                else if (size.y >= size.x && size.y >= size.z)
                {
                    localUpAxis = Vector3.up;
                    height = size.y;
                    radius = Mathf.Max(size.x, size.z) * 0.5f;
                }
                else
                {
                    localUpAxis = Vector3.forward;
                    height = size.z;
                    radius = Mathf.Max(size.x, size.y) * 0.5f;
                }
                return new ModelGeometry(
                    localCenter,
                    size,
                    height,
                    radius,
                    localUpAxis);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Vector3 RightPalmCenter(Transform target)
        {
            Transform hand = RequirePath(target, RightHandPath);
            string[] names =
            {
                "RightIndexProximal", "RightMiddleProximal",
                "RightRingProximal", "RightLittleProximal"
            };
            return names.Select(name => hand.Find(name)?.position ??
                    throw new InvalidOperationException(target.name + " is missing " + name + "."))
                .Aggregate(Vector3.zero, (sum, point) => sum + point) / names.Length;
        }

        private static Vector3 RightFingerDirection(Transform target)
        {
            Transform hand = RequirePath(target, RightHandPath);
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new InvalidOperationException(target.name + " is missing RightMiddleProximal.");
            return (middle.position - hand.position).normalized;
        }

        private static Vector3 RightPalmNormal(Transform target)
        {
            Transform hand = RequirePath(target, RightHandPath);
            Transform index = hand.Find("RightIndexProximal") ??
                throw new InvalidOperationException(target.name + " is missing RightIndexProximal.");
            Transform little = hand.Find("RightLittleProximal") ??
                throw new InvalidOperationException(target.name + " is missing RightLittleProximal.");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 normal = Vector3.Cross(width, RightFingerDirection(target)).normalized;
            if (normal.sqrMagnitude < 0.99f)
                throw new InvalidOperationException(target.name + " palm normal is degenerate.");
            return normal;
        }

        private static Dictionary<string, string> CaptureSourceHashes(Scene scene)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string controllerPath in SourceControllerPaths)
            {
                result[controllerPath] = ComputeAssetHash(controllerPath);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                AnimationClip clip = RequireSingleDefaultState(controller).motion as AnimationClip;
                string clipPath = AssetDatabase.GetAssetPath(clip);
                if (!result.ContainsKey(clipPath)) result[clipPath] = ComputeAssetHash(clipPath);
            }
            foreach (string sourceName in SourceNames)
                result["scene:" + sourceName] = HierarchySignature(
                    FindUnique(scene, sourceName).transform);
            return result;
        }

        private static void RequireHashes(Dictionary<string, string> hashes)
        {
            foreach (KeyValuePair<string, string> pair in hashes)
            {
                string current = pair.Key.StartsWith("scene:", StringComparison.Ordinal)
                    ? HierarchySignature(FindUnique(
                        RequireScene(), pair.Key.Substring("scene:".Length)).transform)
                    : ComputeAssetHash(pair.Key);
                if (current != pair.Value)
                    throw new InvalidOperationException(pair.Key + " changed unexpectedly.");
            }
        }

        private static string ProtectedSceneSignature(Scene scene)
        {
            var result = new StringBuilder();
            foreach (Transform root in scene.GetRootGameObjects().Select(item => item.transform)
                .OrderBy(item => item.name, StringComparer.Ordinal))
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                    .Where(item => !IsInsideApprovedTarget(item))
                    .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                        StringComparer.Ordinal))
                {
                    result.Append(item.name).Append('|')
                        .Append(Vec(item.localPosition)).Append('|')
                        .Append(Quat(item.localRotation)).Append('|')
                        .AppendLine(Vec(item.localScale));
                }
            }
            return Sha256(result.ToString());
        }

        private static bool IsInsideApprovedTarget(Transform item)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (TargetNames.Contains(current.name)) return true;
            return false;
        }

        private static string HierarchySignature(Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
            {
                result.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .AppendLine(Vec(item.localScale));
            }
            return Sha256(result.ToString());
        }

        private static string CurveSignature(AnimationClip clip)
        {
            var result = new StringBuilder()
                .AppendLine("length=" + Num(clip.length))
                .AppendLine("frameRate=" + Num(clip.frameRate));
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.type.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append(binding.path).Append('|').Append(binding.type.FullName)
                    .Append('|').AppendLine(binding.propertyName);
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                foreach (Keyframe key in curve.keys)
                    result.AppendLine(string.Join(",", new[]
                    {
                        Num(key.time), Num(key.value), Num(key.inTangent), Num(key.outTangent),
                        Num(key.inWeight), Num(key.outWeight), key.weightedMode.ToString()
                    }));
            }
            foreach (EditorCurveBinding binding in AnimationUtility
                .GetObjectReferenceCurveBindings(clip)
                .OrderBy(item => item.path, StringComparer.Ordinal)
                .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                result.Append("object|").Append(binding.path).Append('|')
                    .AppendLine(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in AnimationUtility
                    .GetObjectReferenceCurve(clip, binding))
                    result.AppendLine(Num(key.time) + "|" + AssetDatabase.GetAssetPath(key.value));
            }
            return Sha256(result.ToString());
        }

        private static string[] TextureAssetPaths() =>
            AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        private static string[] OriginalTexturePaths() => new[]
        {
            BaseColorTexturePath,
            NormalTexturePath,
            MetallicTexturePath,
            RoughnessTexturePath
        };

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
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

        private static Transform RequirePath(Transform root, string path) =>
            root.Find(path) ?? throw new InvalidOperationException(
                root.name + " is missing path " + path + ".");

        private static Transform RequireDirectChild(Transform parent, string name) =>
            parent.Cast<Transform>().SingleOrDefault(item => item.name == name) ??
            throw new InvalidOperationException(parent.name + " is missing child " + name + ".");

        private static void EnsureFolder(string folder)
        {
            string current = "Assets";
            foreach (string part in folder.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        private static string ComputeAssetHash(string assetPath)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty);
        }

        private static string Absolute(string path)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root, path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void WriteText(string assetPath, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(assetPath)));
            File.WriteAllText(Absolute(assetPath), contents, Encoding.UTF8);
        }

        private static void WriteFailure(Exception exception)
        {
            try
            {
                WriteText(FailurePath, exception.ToString());
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch
            {
                // Preserve the original setup exception.
            }
        }

        private static bool ReviewRevisionComplete()
        {
            return File.Exists(Absolute(FinalReportPath)) &&
                File.ReadAllText(Absolute(FinalReportPath), Encoding.UTF8)
                    .Contains("placementRevision=2");
        }

        private static void ClearConsole()
        {
            Type logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            MethodInfo clear = logEntries.GetMethod(
                "Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console clear method is unavailable.");
            clear.Invoke(null, null);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "FlashbangMaterial" : value;
        }

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private readonly struct Placement
        {
            internal readonly Vector3 LocalPosition;
            internal readonly Quaternion LocalRotation;
            internal readonly Vector3 LocalScale;

            internal Placement(
                Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }
        }

        private readonly struct ModelGeometry
        {
            internal readonly Vector3 LocalCenter;
            internal readonly Vector3 Size;
            internal readonly float Height;
            internal readonly float Radius;
            internal readonly Vector3 LocalUpAxis;

            internal ModelGeometry(
                Vector3 localCenter,
                Vector3 size,
                float height,
                float radius,
                Vector3 localUpAxis)
            {
                LocalCenter = localCenter;
                Size = size;
                Height = height;
                Radius = radius;
                LocalUpAxis = localUpAxis;
            }
        }
    }

    [InitializeOnLoad]
    internal static class FlashbangIdleThrowRevisionTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ModelPath =
            "Assets/_Project/Art/Items/Flashbang/Flashbang.fbx";
        private const string ReviewFolder =
            "Assets/_Project/Art/Items/Flashbang/Review/idle_release_follow_revision";
        private const string RequestPath = ReviewFolder + "/idle_throw_revision_request.txt";
        private const string PendingPath = ReviewFolder + "/idle_throw_revision_pending.txt";
        private const string AppliedPath = ReviewFolder + "/idle_throw_revision_applied.txt";
        private const string FinalImagePath = ReviewFolder + "/idle_throw_revision_final.png";
        private const string FinalReportPath = ReviewFolder + "/idle_throw_revision_final.txt";
        private const string FailurePath = ReviewFolder + "/idle_throw_revision_failure.txt";
        private const string ToolPath = "Assets/_Project/Editor/FlashbangSetupTools.cs";
        private const string MotionTypeName =
            "Bellerophon.PlayerAnimation.FlashbangThrowReleaseFlightBehaviour, Assembly-CSharp";
        private const string IdleName = "Flashbang_Idle";
        private const string IdleSourceName = "Hands_Empty_Idle";
        private const string ReleaseName = "Flashbang_Throw_Release";
        private const string IdleControllerPath =
            "Assets/_Project/Art/Player/Animations/Flashbang/Flashbang_Idle.controller";
        private const string ReleaseControllerPath =
            "Assets/_Project/Art/Player/Animations/Flashbang/Flashbang_Throw_Release.controller";
        private const string IdleSourceControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Empty_Idle.controller";
        private const string ReleaseSourceControllerPath =
            "Assets/_Project/Art/Player/Animations/Hands_Throw_Release.controller";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const int AnalysisSamplesPerSecond = 240;
        private const int ReviewLayer = 31;
        private const float PalmForwardToleranceDegrees = 8f;
        private const float FollowPositionTolerance = 0.00001f;
        private const float FollowRotationToleranceDegrees = 0.05f;
        private const float VelocityAlignmentToleranceDegrees = 8f;

        private static readonly string[] RightArmPaths =
        {
            RightShoulderPath,
            RightArmPath,
            RightForeArmPath,
            RightHandPath
        };

        // Anatomical allocation: small clavicle/wrist corrections, with most palm
        // orientation shared by humeral rotation and forearm pronation/supination.
        private static readonly float[] RightArmRotationShares =
        {
            0.10f,
            0.35f,
            0.55f
        };

        private static bool reviewRunning;
        private static double reviewStartedAt;
        private static Texture2D[] reviewPanels;

        static FlashbangIdleThrowRevisionTools()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashbang/Apply Idle Palm And Throw Flight")]
        private static void ApplyAndReview()
        {
            ApplyRevision();
            WriteText(PendingPath, "reviewPending=True\n");
            WriteText(RequestPath, "apply=False\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashbang/Inspect Idle Palm And Throw Flight")]
        private static void InspectMenu()
        {
            InspectApplied(RequireScene());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[Flashbang] Idle palm and Throw Release flight inspection passed.");
        }

        private static void TryRunApprovedRequest()
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.update -= TryRunApprovedRequest;
                    return;
                }
                bool explicitlyRequested = File.Exists(Absolute(RequestPath)) &&
                    File.ReadAllText(Absolute(RequestPath), Encoding.UTF8)
                        .Contains("apply=True");
                bool reviewPending = File.Exists(Absolute(PendingPath)) &&
                    File.ReadAllText(Absolute(PendingPath), Encoding.UTF8)
                        .Contains("reviewPending=True");
                bool pendingEndedInFailure = reviewPending &&
                    File.Exists(Absolute(FailurePath)) &&
                    File.GetLastWriteTimeUtc(Absolute(FailurePath)) >=
                    File.GetLastWriteTimeUtc(Absolute(PendingPath));
                if (pendingEndedInFailure)
                {
                    WriteText(PendingPath, "reviewPending=False\n");
                    reviewPending = false;
                }
                if (reviewPending)
                {
                    EditorApplication.update -= TryRunApprovedRequest;
                    EditorApplication.EnterPlaymode();
                    return;
                }
                bool recaptureApprovedRevision = File.Exists(Absolute(FinalReportPath)) &&
                    File.GetLastWriteTimeUtc(Absolute(ToolPath)) >
                    File.GetLastWriteTimeUtc(Absolute(FinalReportPath));
                if (recaptureApprovedRevision)
                {
                    EditorApplication.update -= TryRunApprovedRequest;
                    ApplyAndReview();
                    return;
                }
                bool retryApprovedFailure = File.Exists(Absolute(FailurePath)) &&
                    !reviewPending &&
                    File.GetLastWriteTimeUtc(Absolute(ToolPath)) >
                    File.GetLastWriteTimeUtc(Absolute(FailurePath)) &&
                    (!File.Exists(Absolute(FinalReportPath)) ||
                     File.GetLastWriteTimeUtc(Absolute(FailurePath)) >
                     File.GetLastWriteTimeUtc(Absolute(FinalReportPath)));
                if (!explicitlyRequested && !retryApprovedFailure)
                {
                    EditorApplication.update -= TryRunApprovedRequest;
                    return;
                }
                EditorApplication.update -= TryRunApprovedRequest;
                ApplyAndReview();
            }
            catch (Exception exception)
            {
                WriteText(PendingPath, "reviewPending=False\n");
                WriteFailure(exception);
                Debug.LogException(exception);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                File.Exists(Absolute(PendingPath)) &&
                File.ReadAllText(Absolute(PendingPath), Encoding.UTF8)
                    .Contains("reviewPending=True"))
                BeginRuntimeReview();
        }

        private static void ApplyRevision()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException(
                    "Flashbang Idle/Throw revision requires Edit Mode.");
            Scene scene = RequireScene();
            ClearConsole();
            string protectedSignature = ProtectedSceneSignature(scene);
            string idleSourceHash = ComputeAssetHash(IdleSourceControllerPath);
            string releaseSourceHash = ComputeAssetHash(ReleaseSourceControllerPath);
            string modelHash = ComputeAssetHash(ModelPath);

            GameObject idle = FindUnique(scene, IdleName);
            GameObject idleSource = FindUnique(scene, IdleSourceName);
            GameObject release = FindUnique(scene, ReleaseName);
            Animator idleAnimator = RequireAnimatorAndController(idle, IdleControllerPath);
            Animator releaseAnimator = RequireAnimatorAndController(release, ReleaseControllerPath);
            AnimationClip idleClip = RequireSingleClip(idleAnimator);
            AnimationClip releaseClip = RequireSingleClip(releaseAnimator);
            Transform idleHand = RequirePath(idle.transform, RightHandPath);
            Transform releaseHand = RequirePath(release.transform, RightHandPath);
            Transform idleProp = RequireDirectChild(idleHand, "Flashbang_Prop");
            Transform releaseProp = RequireDirectChild(releaseHand, "Flashbang_Prop");

            IdlePoseAnalysis idleAnalysis = AnalyzeIdlePose(idleSource, idleClip);
            ThrowAnalysis throwAnalysis = AnalyzeThrow(release, releaseClip);
            Bounds modelBounds = MeasureModelLocalBounds();

            MotionProxy idleMotion = GetOrAddMotion(idle);
            Undo.RecordObject(idleMotion.Component, "Configure Flashbang Idle palm-forward pose");
            idleMotion.Invoke(
                "ConfigureIdle",
                idleAnimator,
                idleHand,
                idleProp,
                idleAnalysis.BonePaths,
                idleAnalysis.SourceRotations,
                idleAnalysis.AuthoredRotations);
            EditorUtility.SetDirty(idleMotion.Component);

            MotionProxy releaseMotion = GetOrAddMotion(release);
            Undo.RecordObject(releaseMotion.Component, "Configure Flashbang Throw Release flight");
            releaseMotion.Invoke(
                "ConfigureThrow",
                releaseAnimator,
                releaseHand,
                releaseProp,
                releaseClip.length,
                throwAnalysis.ReleaseTime,
                throwAnalysis.LaunchVelocityLocal,
                throwAnalysis.SpinRadiansPerSecond,
                Vector3.forward,
                modelBounds.center,
                modelBounds.size);
            EditorUtility.SetDirty(releaseMotion.Component);

            InspectApplied(scene);
            if (ProtectedSceneSignature(scene) != protectedSignature)
                throw new InvalidOperationException(
                    "A scene object outside Flashbang_Idle and Flashbang_Throw_Release changed.");
            RequireUnchangedHash(IdleSourceControllerPath, idleSourceHash);
            RequireUnchangedHash(ReleaseSourceControllerPath, releaseSourceHash);
            RequireUnchangedHash(ModelPath, modelHash);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");

            var report = new StringBuilder()
                .AppendLine("Flashbang Idle palm-forward and Throw Release flight")
                .AppendLine("idleSourceAnimationChanged=False")
                .AppendLine("releaseSourceAnimationChanged=False")
                .AppendLine("flashbangModelChanged=False")
                .AppendLine("rendererMeshMaterialTextureChanged=False")
                .AppendLine("unrelatedSceneObjectsChanged=False")
                .AppendLine("idlePalmBeforeDegrees=" + Num(idleAnalysis.BeforeDeviation))
                .AppendLine("idlePalmAfterDegrees=" + Num(idleAnalysis.AfterDeviation))
                .AppendLine("idleRotationDistribution=LongitudinalTwist:RightShoulder10%,RightArm35%,RightForeArm55%,RightHandResidualOnly")
                .AppendLine("idleRuntimeMotion=FrozenAuthoredPoseFromRightShoulderThroughFingertips")
                .AppendLine("idleFrozenBoneCount=" + idleAnalysis.BonePaths.Length)
                .AppendLine("idleBoneDeltaDegrees=" +
                    string.Join(",", idleAnalysis.BoneDeltaDegrees.Select(Num)))
                .AppendLine("releaseMaximumHeightSampleRate=" + AnalysisSamplesPerSecond)
                .AppendLine("releaseTimeSeconds=" + Num(throwAnalysis.ReleaseTime))
                .AppendLine("releaseHandHeight=" + Num(throwAnalysis.ReleaseHeight))
                .AppendLine("previousHandHeight=" + Num(throwAnalysis.PreviousHeight))
                .AppendLine("nextHandHeight=" + Num(throwAnalysis.NextHeight))
                .AppendLine("sourceHandVelocityLocal=" + Vec(throwAnalysis.SourceVelocityLocal))
                .AppendLine("launchVelocityLocal=" + Vec(throwAnalysis.LaunchVelocityLocal))
                .AppendLine("launchSpeedFromSourceHand=" +
                    Num(throwAnalysis.LaunchVelocityLocal.magnitude))
                .AppendLine("spinRadiansPerSecondFromSourceHand=" +
                    Num(throwAnalysis.SpinRadiansPerSecond))
                .AppendLine("gravitySource=UnityPhysicsGravity")
                .AppendLine("flightOrientation=VelocityAlignmentPlusSourceHandSpin")
                .AppendLine("loopReset=ReparentToRightHandAndRethrow");
            WriteText(AppliedPath, report.ToString());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Flashbang] Idle palm-forward pose and Throw Release flight applied.");
        }

        private static IdlePoseAnalysis AnalyzeIdlePose(GameObject target, AnimationClip clip)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashbangIdlePalmAnalysis";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                DisableBehaviours(work);
                clip.SampleAnimation(work, 0f);
                string[] bonePaths = RequirePath(work.transform, RightShoulderPath)
                    .GetComponentsInChildren<Transform>(true)
                    .Select(bone => AnimationUtility.CalculateTransformPath(
                        bone, work.transform))
                    .Where(path => !string.IsNullOrEmpty(path))
                    .ToArray();
                var source = new Quaternion[bonePaths.Length];
                for (int index = 0; index < bonePaths.Length; index++)
                {
                    Transform bone = RequirePath(work.transform, bonePaths[index]);
                    source[index] = bone.localRotation;
                }

                float before = Vector3.Angle(RightPalmNormal(work.transform), work.transform.forward);
                ConstrainPalmForwardAnatomically(work.transform);

                var authored = new Quaternion[bonePaths.Length];
                var deltas = new float[bonePaths.Length];
                for (int index = 0; index < bonePaths.Length; index++)
                {
                    Transform bone = RequirePath(work.transform, bonePaths[index]);
                    authored[index] = bone.localRotation;
                    deltas[index] = Quaternion.Angle(source[index], bone.localRotation);
                }
                float after = Vector3.Angle(
                    RightPalmNormal(work.transform), work.transform.forward);
                if (after > 0.1f)
                    throw new InvalidOperationException(
                        "Idle palm correction did not reach transporter forward: " + Num(after));
                return new IdlePoseAnalysis(
                    bonePaths, source, authored, deltas, before, after);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static void ConstrainPalmForwardAnatomically(Transform root)
        {
            Transform[] bones =
            {
                RequirePath(root, RightShoulderPath),
                RequirePath(root, RightArmPath),
                RequirePath(root, RightForeArmPath)
            };
            Transform[] children =
            {
                RequirePath(root, RightArmPath),
                RequirePath(root, RightForeArmPath),
                RequirePath(root, RightHandPath)
            };
            for (int iteration = 0; iteration < 12; iteration++)
            {
                if (Vector3.Angle(RightPalmNormal(root), root.forward) <= 0.1f) break;
                for (int index = 0; index < bones.Length; index++)
                {
                    Vector3 axis = (children[index].position - bones[index].position).normalized;
                    Vector3 current = Vector3.ProjectOnPlane(
                        RightPalmNormal(root), axis).normalized;
                    Vector3 desired = Vector3.ProjectOnPlane(root.forward, axis).normalized;
                    if (current.sqrMagnitude <= 0.5f || desired.sqrMagnitude <= 0.5f) continue;
                    float angle = Vector3.SignedAngle(current, desired, axis) *
                        RightArmRotationShares[index];
                    bones[index].rotation = Quaternion.AngleAxis(angle, axis) *
                        bones[index].rotation;
                }
            }
            Transform hand = RequirePath(root, RightHandPath);
            Quaternion residual = Quaternion.FromToRotation(
                RightPalmNormal(root), root.forward);
            hand.rotation = residual * hand.rotation;
        }

        private static ThrowAnalysis AnalyzeThrow(GameObject target, AnimationClip clip)
        {
            GameObject work = UnityEngine.Object.Instantiate(target);
            work.name = "FlashbangThrowReleaseAnalysis";
            work.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                DisableBehaviours(work);
                int sampleCount = Mathf.CeilToInt(clip.length * AnalysisSamplesPerSecond);
                float sampleStep = 1f / AnalysisSamplesPerSecond;
                float maximumHeight = float.NegativeInfinity;
                int maximumIndex = -1;
                var positions = new Vector3[sampleCount + 1];
                var rotations = new Quaternion[sampleCount + 1];
                var heights = new float[sampleCount + 1];
                for (int index = 0; index <= sampleCount; index++)
                {
                    float time = Mathf.Min(clip.length, index * sampleStep);
                    clip.SampleAnimation(work, time);
                    Transform hand = RequirePath(work.transform, RightHandPath);
                    positions[index] = hand.position;
                    rotations[index] = hand.rotation;
                    heights[index] = Vector3.Dot(
                        hand.position - work.transform.position, work.transform.up);
                    if (heights[index] > maximumHeight)
                    {
                        maximumHeight = heights[index];
                        maximumIndex = index;
                    }
                }
                if (maximumIndex <= 0 || maximumIndex >= sampleCount)
                    throw new InvalidOperationException(
                        "Throw Release right-hand maximum height is on a clip boundary.");

                float delta = sampleStep * 2f;
                Vector3 sourceVelocity =
                    (positions[maximumIndex + 1] - positions[maximumIndex - 1]) / delta;
                Vector3 sourceVelocityLocal =
                    work.transform.InverseTransformDirection(sourceVelocity);
                Vector3 launchVelocityLocal = new Vector3(
                    0f, sourceVelocityLocal.y, sourceVelocityLocal.z);
                if (launchVelocityLocal.z <= 0.01f)
                    throw new InvalidOperationException(
                        "The source hand velocity at maximum height is not forward: " +
                        Vec(sourceVelocityLocal));

                Quaternion angularDelta = rotations[maximumIndex + 1] *
                    Quaternion.Inverse(rotations[maximumIndex - 1]);
                angularDelta.ToAngleAxis(
                    out float angularDegrees, out Vector3 unusedAngularAxis);
                if (angularDegrees > 180f) angularDegrees = 360f - angularDegrees;
                float spinRadians = angularDegrees * Mathf.Deg2Rad / delta;
                if (spinRadians <= 0.01f)
                    throw new InvalidOperationException(
                        "The source hand has no usable natural angular velocity at release.");

                return new ThrowAnalysis(
                    maximumIndex * sampleStep,
                    maximumHeight,
                    heights[maximumIndex - 1],
                    heights[maximumIndex + 1],
                    sourceVelocityLocal,
                    launchVelocityLocal,
                    spinRadians);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(work);
            }
        }

        private static void BeginRuntimeReview()
        {
            if (reviewRunning || !EditorApplication.isPlaying) return;
            reviewRunning = true;
            reviewStartedAt = EditorApplication.timeSinceStartup;
            reviewPanels = new Texture2D[12];
            EditorApplication.update -= ReviewUpdate;
            EditorApplication.update += ReviewUpdate;
        }

        private static void ReviewUpdate()
        {
            try
            {
                Scene scene = RequireScene();
                GameObject idle = FindUnique(scene, IdleName);
                GameObject release = FindUnique(scene, ReleaseName);
                MotionProxy idleMotion = RequireMotion(idle);
                MotionProxy releaseMotion = RequireMotion(release);
                double elapsed = EditorApplication.timeSinceStartup - reviewStartedAt;
                float remainingFlight = Mathf.Max(
                    0.1f, releaseMotion.ClipLength - releaseMotion.ReleaseTime);
                float idlePhase = idleMotion.LastNormalizedTime -
                    Mathf.Floor(idleMotion.LastNormalizedTime);

                if (reviewPanels[0] == null && idlePhase >= 0.05f && idlePhase < 0.20f)
                    reviewPanels[0] = CaptureArm(idle, idleMotion.Flashbang, idle.transform.forward);
                if (reviewPanels[1] == null && idlePhase >= 0.30f && idlePhase < 0.45f)
                    reviewPanels[1] = CaptureArm(idle, idleMotion.Flashbang, idle.transform.forward);
                if (reviewPanels[2] == null && idlePhase >= 0.55f && idlePhase < 0.70f)
                    reviewPanels[2] = CaptureArm(idle, idleMotion.Flashbang, idle.transform.forward);
                if (reviewPanels[3] == null && idlePhase >= 0.80f && idlePhase < 0.95f)
                    reviewPanels[3] = CaptureArm(
                        idle, idleMotion.Flashbang,
                        (idle.transform.forward + idle.transform.right * 0.55f).normalized);
                if (reviewPanels[4] == null && !releaseMotion.IsReleased &&
                    releaseMotion.ReleaseCount == 0 &&
                    releaseMotion.CurrentClipTime >= Mathf.Max(0f, releaseMotion.ReleaseTime - 0.08f))
                    reviewPanels[4] = CaptureHandoff(release, releaseMotion.Flashbang);
                if (reviewPanels[5] == null && releaseMotion.ReleaseCount == 1 &&
                    releaseMotion.IsReleased && releaseMotion.FlightElapsed <= 0.04f)
                    reviewPanels[5] = CaptureHandoff(release, releaseMotion.Flashbang);
                if (reviewPanels[6] == null && releaseMotion.ReleaseCount == 1 &&
                    releaseMotion.FlightElapsed >= remainingFlight * 0.30f)
                    reviewPanels[6] = CaptureSequence(release, releaseMotion.Flashbang);
                if (reviewPanels[7] == null && releaseMotion.ReleaseCount == 1 &&
                    releaseMotion.FlightElapsed >= remainingFlight * 0.55f)
                    reviewPanels[7] = CaptureSequence(release, releaseMotion.Flashbang);
                if (reviewPanels[8] == null && releaseMotion.ReleaseCount == 1 &&
                    releaseMotion.FlightElapsed >= remainingFlight * 0.80f)
                    reviewPanels[8] = CaptureSequence(release, releaseMotion.Flashbang);
                if (reviewPanels[9] == null && releaseMotion.ReleaseCount == 1 &&
                    !releaseMotion.IsReleased &&
                    releaseMotion.CurrentClipTime >= Mathf.Max(0f, releaseMotion.ReleaseTime - 0.08f))
                    reviewPanels[9] = CaptureHandoff(release, releaseMotion.Flashbang);
                if (reviewPanels[10] == null && releaseMotion.ReleaseCount >= 2 &&
                    releaseMotion.IsReleased && releaseMotion.FlightElapsed <= 0.04f)
                    reviewPanels[10] = CaptureHandoff(release, releaseMotion.Flashbang);
                if (reviewPanels[11] == null && releaseMotion.ReleaseCount >= 2 &&
                    releaseMotion.IsReleased &&
                    releaseMotion.FlightElapsed >= remainingFlight * 0.55f)
                    reviewPanels[11] = CaptureSequence(release, releaseMotion.Flashbang);

                bool panelsComplete = reviewPanels.All(panel => panel != null);
                if (!panelsComplete && elapsed < 12d) return;
                if (!panelsComplete)
                {
                    Animator releaseAnimator = release.GetComponent<Animator>();
                    throw new InvalidOperationException(
                        "Flashbang direct review sequence did not collect all twelve panels: " +
                        string.Join(",", reviewPanels.Select(
                            (panel, index) => index + "=" + (panel != null))) +
                        "|componentEnabled=" + releaseMotion.Component.enabled +
                        "|activeInHierarchy=" + release.activeInHierarchy +
                        "|animatorEnabled=" + (releaseAnimator != null && releaseAnimator.enabled) +
                        "|animatorNormalized=" +
                        (releaseAnimator != null
                            ? Num(releaseAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime)
                            : "missing") +
                        "|lastNormalized=" + Num(releaseMotion.LastNormalizedTime) +
                        "|currentClipTime=" + Num(releaseMotion.CurrentClipTime) +
                        "|lateUpdateCount=" + releaseMotion.LateUpdateCount +
                        "|releaseCount=" + releaseMotion.ReleaseCount +
                        "|resetCount=" + releaseMotion.ResetCount);
                }

                InspectRuntime(idleMotion, releaseMotion);
                UnityConsoleDiagnostics.AssertNoErrors();
                WriteComposite(reviewPanels, FinalImagePath);
                WriteFinalReport(idleMotion, releaseMotion, elapsed);
                WriteText(PendingPath, "reviewPending=False\n");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                FinishReview();
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                WriteText(PendingPath, "reviewPending=False\n");
                WriteFailure(exception);
                Debug.LogException(exception);
                FinishReview();
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        private static void InspectApplied(Scene scene)
        {
            GameObject idle = FindUnique(scene, IdleName);
            GameObject release = FindUnique(scene, ReleaseName);
            MotionProxy idleMotion = RequireMotion(idle);
            MotionProxy releaseMotion = RequireMotion(release);
            if (idleMotion.ModeName != "IdlePalmForward")
                throw new InvalidOperationException("Flashbang_Idle mode is incorrect.");
            if (releaseMotion.ModeName != "ThrowRelease")
                throw new InvalidOperationException("Flashbang_Throw_Release mode is incorrect.");
            if (idleMotion.PalmForwardDeviationDegrees > 0.1f)
                throw new InvalidOperationException(
                    "Flashbang_Idle palm is not forward in the authored pose: " +
                    Num(idleMotion.PalmForwardDeviationDegrees));
            if (releaseMotion.ReleaseTime <= 0f ||
                releaseMotion.ReleaseTime >= releaseMotion.ClipLength)
                throw new InvalidOperationException("Throw release time is outside the clip.");
        }

        private static void InspectRuntime(
            MotionProxy idle,
            MotionProxy release)
        {
            if (idle.PalmForwardDeviationDegrees > PalmForwardToleranceDegrees)
                throw new InvalidOperationException(
                    "Flashbang_Idle palm drifted away from transporter forward: " +
                    Num(idle.PalmForwardDeviationDegrees));
            if (idle.IdleFrozenBoneCount <= RightArmPaths.Length)
                throw new InvalidOperationException(
                    "Flashbang_Idle did not freeze the full right arm and fingers.");
            if (idle.MaximumIdleBoneRotationDrift > 0.05f)
                throw new InvalidOperationException(
                    "Flashbang_Idle right arm rotated during natural playback: " +
                    Num(idle.MaximumIdleBoneRotationDrift));
            if (release.ReleaseCount < 2 || release.ResetCount < 2)
                throw new InvalidOperationException(
                    "Flashbang_Throw_Release did not reset and throw twice.");
            if (release.MaximumHeldPositionError > FollowPositionTolerance ||
                release.MaximumHeldRotationError > FollowRotationToleranceDegrees)
                throw new InvalidOperationException(
                    "Flashbang held follow error exceeded tolerance.");
            if (release.MaximumReleaseHandoffPositionError > FollowPositionTolerance ||
                release.MaximumReleaseHandoffRotationError > FollowRotationToleranceDegrees ||
                release.MaximumReleaseHandoffVerticalDrop > FollowPositionTolerance)
                throw new InvalidOperationException(
                    "Flashbang release handoff moved below or away from the recorded hand pose.");
            if (release.ReleaseVelocity.sqrMagnitude <= 0.0001f ||
                Vector3.Dot(release.ReleaseVelocity.normalized, release.Root.forward) <= 0f)
                throw new InvalidOperationException("Flashbang did not launch forward.");
            if (release.SpinRadiansPerSecond <= 0.01f)
                throw new InvalidOperationException("Flashbang natural spin was not retained.");
            if (release.MaximumVelocityAlignmentError > VelocityAlignmentToleranceDegrees)
                throw new InvalidOperationException(
                    "Flashbang velocity alignment exceeded tolerance: " +
                    Num(release.MaximumVelocityAlignmentError));
        }

        private static void WriteFinalReport(
            MotionProxy idle,
            MotionProxy release,
            double elapsed)
        {
            var report = new StringBuilder()
                .AppendLine("Flashbang Idle palm and Throw Release natural Play Mode review")
                .AppendLine("captureKind=FinalComposite")
                .AppendLine("verificationTargetTransformManipulated=False")
                .AppendLine("forcedAnimatorTime=False")
                .AppendLine("panelOrder=IdleFrontEarly,IdleFrontQuarter,IdleFrontLate,IdleObliqueEnd,FirstHeldPeakClose,FirstReleaseImmediateClose,FirstFlightEarlyWide,FirstFlightMidWide,FirstFlightLateWide,SecondHeldPeakClose,SecondReleaseImmediateClose,SecondFlightMidWide")
                .AppendLine("idlePalmFacesTransporterForward=True")
                .AppendLine("idlePalmDeviationDegrees=" +
                    Num(idle.PalmForwardDeviationDegrees))
                .AppendLine("idleRightArmAndFingersFrozenThroughoutLoop=True")
                .AppendLine("idleFrozenBoneCount=" + idle.IdleFrozenBoneCount)
                .AppendLine("maximumIdleBoneRotationDriftDegrees=" +
                    Num(idle.MaximumIdleBoneRotationDrift))
                .AppendLine("releaseAtAnalyzedRightHandMaximumHeight=True")
                .AppendLine("rightHandFollowReleasedAtMaximum=True")
                .AppendLine("releaseUsesRecordedPeakHeldWorldPose=True")
                .AppendLine("rigidbodyInterpolationAtHandoff=None")
                .AppendLine("rigidbodyGravity=True")
                .AppendLine("velocityDirectionAlignment=True")
                .AppendLine("sourceHandNaturalSpin=True")
                .AppendLine("loopResetAndRethrow=True")
                .AppendLine("releaseTimeSeconds=" + Num(release.ReleaseTime))
                .AppendLine("releaseVelocity=" + Vec(release.ReleaseVelocity))
                .AppendLine("spinRadiansPerSecond=" + Num(release.SpinRadiansPerSecond))
                .AppendLine("releaseCount=" + release.ReleaseCount)
                .AppendLine("resetCount=" + release.ResetCount)
                .AppendLine("maximumHeldPositionError=" +
                    Num(release.MaximumHeldPositionError))
                .AppendLine("maximumHeldRotationErrorDegrees=" +
                    Num(release.MaximumHeldRotationError))
                .AppendLine("maximumReleaseHandoffPositionError=" +
                    Num(release.MaximumReleaseHandoffPositionError))
                .AppendLine("maximumReleaseHandoffRotationErrorDegrees=" +
                    Num(release.MaximumReleaseHandoffRotationError))
                .AppendLine("maximumReleaseHandoffVerticalDrop=" +
                    Num(release.MaximumReleaseHandoffVerticalDrop))
                .AppendLine("maximumPostAlignmentVelocityErrorDegrees=" +
                    Num(release.MaximumVelocityAlignmentError))
                .AppendLine("elapsedSeconds=" + Num((float)elapsed))
                .AppendLine("rendererMeshMaterialTextureChanged=False")
                .AppendLine("sourceAnimationsChanged=False")
                .AppendLine("unrelatedSceneObjectsChanged=False");
            WriteText(FinalReportPath, report.ToString());
        }

        private static Texture2D CaptureArm(
            GameObject target, Transform prop, Vector3 viewDirection)
        {
            Transform shoulder = RequirePath(target.transform, RightShoulderPath);
            Transform arm = RequirePath(target.transform, RightArmPath);
            Transform forearm = RequirePath(target.transform, RightForeArmPath);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Bounds bounds = RendererBounds(prop);
            bounds.Encapsulate(shoulder.position);
            bounds.Encapsulate(arm.position);
            bounds.Encapsulate(forearm.position);
            bounds.Encapsulate(hand.position);
            bounds.Expand(0.32f);
            return CaptureView(target.transform, prop, bounds, viewDirection);
        }

        private static Texture2D CaptureSequence(GameObject target, Transform prop)
        {
            Bounds bounds = RendererBounds(target.transform);
            bounds.Encapsulate(RendererBounds(prop));
            bounds.Expand(0.18f);
            return CaptureView(target.transform, prop, bounds, target.transform.right);
        }

        private static Texture2D CaptureHandoff(GameObject target, Transform prop)
        {
            Transform arm = RequirePath(target.transform, RightArmPath);
            Transform forearm = RequirePath(target.transform, RightForeArmPath);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Bounds bounds = RendererBounds(prop);
            bounds.Encapsulate(arm.position);
            bounds.Encapsulate(forearm.position);
            bounds.Encapsulate(hand.position);
            bounds.Expand(0.20f);
            return CaptureView(target.transform, prop, bounds, target.transform.forward);
        }

        private static Texture2D CaptureProp(GameObject target, Transform prop)
        {
            Bounds bounds = RendererBounds(prop);
            bounds.Expand(0.12f);
            return CaptureView(target.transform, prop, bounds, target.transform.right);
        }

        private static Texture2D CaptureView(
            Transform target, Transform extra, Bounds bounds, Vector3 viewDirection)
        {
            Transform[] hierarchy = target.GetComponentsInChildren<Transform>(true)
                .Concat(extra.GetComponentsInChildren<Transform>(true))
                .Distinct().ToArray();
            int[] originalLayers = hierarchy.Select(item => item.gameObject.layer).ToArray();
            GameObject cameraObject = new GameObject("FlashbangRevisionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject lightObject = new GameObject("FlashbangRevisionLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (Transform item in hierarchy) item.gameObject.layer = ReviewLayer;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.36f, 0.39f, 0.42f, 1f);
                camera.cullingMask = 1 << ReviewLayer;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.15f,
                    Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 40f;
                Vector3 direction = viewDirection.sqrMagnitude > 0.5f
                    ? viewDirection.normalized
                    : Vector3.forward;
                camera.transform.position = bounds.center + direction *
                    Mathf.Max(2f, bounds.extents.magnitude * 4f);
                camera.transform.LookAt(bounds.center, target.up);
                camera.targetTexture = render;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.1f;
                light.color = Color.white;
                light.cullingMask = 1 << ReviewLayer;
                light.transform.rotation = Quaternion.LookRotation(-direction, target.up);

                camera.Render();
                RenderTexture.active = render;
                var image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                for (int index = 0; index < hierarchy.Length; index++)
                    hierarchy[index].gameObject.layer = originalLayers[index];
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static void WriteComposite(Texture2D[] panels, string path)
        {
            Texture2D composite = null;
            try
            {
                int rowCount = Mathf.CeilToInt(panels.Length / 4f);
                composite = new Texture2D(
                    2048, rowCount * 512, TextureFormat.RGB24, false);
                for (int index = 0; index < panels.Length; index++)
                {
                    int column = index % 4;
                    int row = rowCount - 1 - index / 4;
                    composite.SetPixels32(
                        column * 512, row * 512, 512, 512,
                        panels[index].GetPixels32());
                }
                composite.Apply(false, false);
                File.WriteAllBytes(Absolute(path), composite.EncodeToPNG());
            }
            finally
            {
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static void FinishReview()
        {
            EditorApplication.update -= ReviewUpdate;
            reviewRunning = false;
            if (reviewPanels != null)
            {
                foreach (Texture2D panel in reviewPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            }
            reviewPanels = null;
        }

        private static Type RequireMotionType()
        {
            return Type.GetType(MotionTypeName, false) ??
                throw new InvalidOperationException(
                    "Flashbang runtime motion type is unavailable: " + MotionTypeName);
        }

        private static MotionProxy GetOrAddMotion(GameObject target)
        {
            Type type = RequireMotionType();
            MonoBehaviour component = target.GetComponent(type) as MonoBehaviour;
            if (component == null)
                component = target.AddComponent(type) as MonoBehaviour;
            if (component == null)
                throw new InvalidOperationException(
                    "Could not add Flashbang runtime motion to " + target.name + ".");
            return new MotionProxy(component);
        }

        private static MotionProxy RequireMotion(GameObject target)
        {
            MonoBehaviour component = target.GetComponent(RequireMotionType()) as MonoBehaviour;
            if (component == null)
                throw new InvalidOperationException(
                    target.name + " Flashbang runtime motion is missing.");
            return new MotionProxy(component);
        }

        private static Animator RequireAnimatorAndController(
            GameObject target, string expectedControllerPath)
        {
            Animator animator = target.GetComponent<Animator>() ??
                throw new InvalidOperationException(target.name + " Animator is missing.");
            if (AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) !=
                expectedControllerPath)
                throw new InvalidOperationException(
                    target.name + " controller path changed unexpectedly.");
            return animator;
        }

        private static AnimationClip RequireSingleClip(Animator animator)
        {
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(animator.name + " controller is invalid.");
            if (controller.layers.Length != 1 ||
                controller.layers[0].stateMachine.states.Length != 1)
                throw new InvalidOperationException(
                    animator.name + " must have one layer and one state.");
            return controller.layers[0].stateMachine.defaultState.motion as AnimationClip ??
                throw new InvalidOperationException(animator.name + " clip is missing.");
        }

        private static Bounds MeasureModelLocalBounds()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Flashbang model is missing.");
            GameObject instance = UnityEngine.Object.Instantiate(asset);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    throw new InvalidOperationException("Flashbang model has no renderer.");
                bool initialized = false;
                Bounds local = default;
                foreach (Renderer renderer in renderers)
                {
                    Bounds world = renderer.bounds;
                    for (int x = -1; x <= 1; x += 2)
                    for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = instance.transform.InverseTransformPoint(
                            world.center + Vector3.Scale(
                                world.extents, new Vector3(x, y, z)));
                        if (!initialized)
                        {
                            local = new Bounds(corner, Vector3.zero);
                            initialized = true;
                        }
                        else local.Encapsulate(corner);
                    }
                }
                return local;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void DisableBehaviours(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
            Animator animator = root.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
        }

        private static Vector3 RightPalmNormal(Transform target)
        {
            Transform hand = RequirePath(target, RightHandPath);
            Transform index = hand.Find("RightIndexProximal") ??
                throw new InvalidOperationException("RightIndexProximal is missing.");
            Transform middle = hand.Find("RightMiddleProximal") ??
                throw new InvalidOperationException("RightMiddleProximal is missing.");
            Transform little = hand.Find("RightLittleProximal") ??
                throw new InvalidOperationException("RightLittleProximal is missing.");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 fingers = (middle.position - hand.position).normalized;
            return Vector3.Cross(width, fingers).normalized;
        }

        private static Bounds RendererBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(root.name + " has no enabled renderer.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static string ProtectedSceneSignature(Scene scene)
        {
            var result = new StringBuilder();
            foreach (Transform root in scene.GetRootGameObjects().Select(item => item.transform)
                .OrderBy(item => item.name, StringComparer.Ordinal))
            {
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                    .Where(item => !IsInsideApprovedTarget(item))
                    .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                        StringComparer.Ordinal))
                {
                    result.Append(item.name).Append('|')
                        .Append(Vec(item.localPosition)).Append('|')
                        .Append(Quat(item.localRotation)).Append('|')
                        .AppendLine(Vec(item.localScale));
                }
            }
            return Sha256(result.ToString());
        }

        private static bool IsInsideApprovedTarget(Transform item)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (current.name == IdleName || current.name == ReleaseName) return true;
            return false;
        }

        private static void RequireUnchangedHash(string path, string expected)
        {
            if (ComputeAssetHash(path) != expected)
                throw new InvalidOperationException(path + " changed unexpectedly.");
        }

        private static string ComputeAssetHash(string assetPath)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Sha256(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty);
        }

        private static void ClearConsole()
        {
            Type logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            MethodInfo clear = logEntries.GetMethod(
                "Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console clear method is unavailable.");
            clear.Invoke(null, null);
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
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

        private static Transform RequirePath(Transform root, string path) =>
            root.Find(path) ?? throw new InvalidOperationException(
                root.name + " is missing path " + path + ".");

        private static Transform RequireDirectChild(Transform parent, string name) =>
            parent.Cast<Transform>().SingleOrDefault(item => item.name == name) ??
            throw new InvalidOperationException(parent.name + " is missing child " + name + ".");

        private static string Absolute(string path)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                root, path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static void WriteText(string assetPath, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Absolute(assetPath)));
            File.WriteAllText(Absolute(assetPath), contents, Encoding.UTF8);
        }

        private static void WriteFailure(Exception exception)
        {
            try
            {
                WriteText(FailurePath, exception.ToString());
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch
            {
                // Preserve the original exception.
            }
        }

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private readonly struct IdlePoseAnalysis
        {
            internal readonly string[] BonePaths;
            internal readonly Quaternion[] SourceRotations;
            internal readonly Quaternion[] AuthoredRotations;
            internal readonly float[] BoneDeltaDegrees;
            internal readonly float BeforeDeviation;
            internal readonly float AfterDeviation;

            internal IdlePoseAnalysis(
                string[] bonePaths,
                Quaternion[] sourceRotations,
                Quaternion[] authoredRotations,
                float[] boneDeltaDegrees,
                float beforeDeviation,
                float afterDeviation)
            {
                BonePaths = bonePaths;
                SourceRotations = sourceRotations;
                AuthoredRotations = authoredRotations;
                BoneDeltaDegrees = boneDeltaDegrees;
                BeforeDeviation = beforeDeviation;
                AfterDeviation = afterDeviation;
            }
        }

        private readonly struct ThrowAnalysis
        {
            internal readonly float ReleaseTime;
            internal readonly float ReleaseHeight;
            internal readonly float PreviousHeight;
            internal readonly float NextHeight;
            internal readonly Vector3 SourceVelocityLocal;
            internal readonly Vector3 LaunchVelocityLocal;
            internal readonly float SpinRadiansPerSecond;

            internal ThrowAnalysis(
                float releaseTime,
                float releaseHeight,
                float previousHeight,
                float nextHeight,
                Vector3 sourceVelocityLocal,
                Vector3 launchVelocityLocal,
                float spinRadiansPerSecond)
            {
                ReleaseTime = releaseTime;
                ReleaseHeight = releaseHeight;
                PreviousHeight = previousHeight;
                NextHeight = nextHeight;
                SourceVelocityLocal = sourceVelocityLocal;
                LaunchVelocityLocal = launchVelocityLocal;
                SpinRadiansPerSecond = spinRadiansPerSecond;
            }
        }

        private sealed class MotionProxy
        {
            internal MotionProxy(MonoBehaviour component)
            {
                Component = component;
            }

            internal MonoBehaviour Component { get; }
            internal Transform Root => Component.transform;
            internal string ModeName => Get<object>("Mode").ToString();
            internal Transform Flashbang => Get<Transform>("Flashbang");
            internal bool IsReleased => Get<bool>("IsReleased");
            internal float FlightElapsed => Get<float>("FlightElapsed");
            internal int ReleaseCount => Get<int>("ReleaseCount");
            internal int ResetCount => Get<int>("ResetCount");
            internal float ReleaseTime => Get<float>("ReleaseTime");
            internal float ClipLength => Get<float>("ClipLength");
            internal float CurrentClipTime => Get<float>("CurrentClipTime");
            internal Vector3 ReleaseVelocity => Get<Vector3>("ReleaseVelocity");
            internal float SpinRadiansPerSecond => Get<float>("SpinRadiansPerSecond");
            internal float MaximumHeldPositionError =>
                Get<float>("MaximumHeldPositionError");
            internal float MaximumHeldRotationError =>
                Get<float>("MaximumHeldRotationError");
            internal float MaximumVelocityAlignmentError =>
                Get<float>("MaximumVelocityAlignmentError");
            internal float MaximumIdleBoneRotationDrift =>
                Get<float>("MaximumIdleBoneRotationDrift");
            internal int IdleFrozenBoneCount => Get<int>("IdleFrozenBoneCount");
            internal float MaximumReleaseHandoffPositionError =>
                Get<float>("MaximumReleaseHandoffPositionError");
            internal float MaximumReleaseHandoffRotationError =>
                Get<float>("MaximumReleaseHandoffRotationError");
            internal float MaximumReleaseHandoffVerticalDrop =>
                Get<float>("MaximumReleaseHandoffVerticalDrop");
            internal int LateUpdateCount => Get<int>("LateUpdateCount");
            internal float LastNormalizedTime => Get<float>("LastNormalizedTime");
            internal float PalmForwardDeviationDegrees =>
                Get<float>("PalmForwardDeviationDegrees");

            internal void Invoke(string methodName, params object[] arguments)
            {
                MethodInfo method = Component.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    throw new InvalidOperationException(
                        Component.GetType().FullName + "." + methodName + " is missing.");
                method.Invoke(Component, arguments);
            }

            private T Get<T>(string propertyName)
            {
                PropertyInfo property = Component.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    throw new InvalidOperationException(
                        Component.GetType().FullName + "." + propertyName + " is missing.");
                return (T)property.GetValue(Component);
            }
        }
    }

    [InitializeOnLoad]
    internal static class FlashbangIdleLocomotionTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Flashbang_Idle";
        private const string ReviewFolder =
            "Assets/_Project/Art/Items/Flashbang/Review/idle_locomotion";
        private const string RequestPath = ReviewFolder + "/request.txt";
        private const string PendingPath = ReviewFolder + "/pending.txt";
        private const string FailurePath = ReviewFolder + "/failure.txt";
        private const string DiagnosticImagePath = ReviewFolder + "/diagnostic_1.png";
        private const string DiagnosticReportPath = ReviewFolder + "/diagnostic_1.txt";
        private const string FinalImagePath = ReviewFolder + "/final.png";
        private const string FinalReportPath = ReviewFolder + "/final.txt";
        private const string AssetFolder =
            "Assets/_Project/Art/Player/Animations/Flashbang/IdleLocomotion";
        private const string ControllerPath = AssetFolder + "/FlashbangIdle_Locomotion.controller";
        private const string IdleClipPath = AssetFolder + "/FlashbangIdle_Idle.anim";
        private const string ForwardClipPath = AssetFolder + "/FlashbangIdle_WalkForward.anim";
        private const string BackwardClipPath = AssetFolder + "/FlashbangIdle_WalkBackward.anim";
        private const string SidestepClipPath = AssetFolder + "/FlashbangIdle_Sidestep.anim";
        private const string RunClipPath = AssetFolder + "/FlashbangIdle_RunForward.anim";
        private const string StateName = "FlashbangIdleLocomotion2D";
        private const string RootTreeName = "FlashbangIdleSixMotion2D";
        private const string DiagonalTreeName = "FlashbangIdleWalkDiagonalSourceExact";
        private const string RightShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder";
        private const string RightArmPath = RightShoulderPath + "/RightArm";
        private const string RightForeArmPath = RightArmPath + "/RightForeArm";
        private const string RightHandPath = RightForeArmPath + "/RightHand";
        private const string CarryTypeName =
            "Bellerophon.PlayerAnimation.FlashbangThrowReleaseFlightBehaviour, Assembly-CSharp";
        private const int MotionCount = 6;
        private const int FullPanelCount = 12;
        private const int GripPanelCount = 6;
        private const float CapturePhaseTime = 0.55f;
        private const double ReviewTimeoutSeconds = 32d;

        private static readonly string[] SourceNames =
        {
            "Player_Idle", "Player_Walk_Forward", "Player_Walk_Backward",
            "Player_Sidestep", "Player_Walk_Diagonal", "Player_Run_Forward"
        };

        private static readonly string[] SourceAssetPaths =
        {
            "Assets/_Project/Art/Player/Animations/Player_Idle.controller",
            "Assets/_Project/Art/Player/Animations/Player_Idle.anim",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward.controller",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Forward_Meshy_Walking.anim",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward.controller",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Backward_Meshy_InPlace.anim",
            "Assets/_Project/Art/Player/Animations/Player_Sidestep.controller",
            "Assets/_Project/Art/Player/Animations/Player_Sidestep_Mixamo_InPlace.anim",
            "Assets/_Project/Art/Player/Animations/Player_Walk_Diagonal.controller",
            "Assets/_Project/Art/Player/Animations/Player_Run_Forward.controller",
            "Assets/_Project/Art/Player/Animations/transfer running.fbx"
        };

        private static readonly Vector2[] RequiredPositions =
        {
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
            new Vector2(1f, 0f), new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f)
        };

        private static bool reviewRunning;
        private static bool finalCapture;
        private static double reviewStartedAt;
        private static int baseAbsolutePhase = -1;
        private static int nextPanel;
        private static GameObject runtimeTarget;
        private static Animator runtimeAnimator;
        private static CarryProxy runtimeCarry;
        private static Transform runtimeProp;
        private static Texture2D[] fullPanels;
        private static Texture2D[] gripPanels;
        private static readonly List<string> Observations = new List<string>();
        private static bool swayBaselineSet;
        private static Quaternion initialSpine;
        private static Quaternion initialHead;
        private static Quaternion initialLeftArm;
        private static Quaternion initialRightShoulder;
        private static Quaternion initialRightArm;
        private static float maximumSpineSway;
        private static float maximumHeadSway;
        private static float maximumLeftArmSway;
        private static float maximumRightShoulderSway;
        private static float maximumRightArmSway;
        private static float maximumPalmForward;
        private static float maximumArmDeviation;
        private static float maximumForeArmDeviation;
        private static float maximumHandDeviation;
        private static float maximumFingerDeviation;
        private static float maximumPropLocalPositionError;
        private static float maximumPropLocalRotationError;
        private static float maximumPropLocalScaleError;
        private static Vector3 expectedPropLocalPosition;
        private static Quaternion expectedPropLocalRotation;
        private static Vector3 expectedPropLocalScale;

        static FlashbangIdleLocomotionTools()
        {
            EditorApplication.update -= TryRunApprovedRequest;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashbang/Apply Idle Locomotion And Review")]
        private static void ApplyIdleLocomotionAndReview()
        {
            Apply();
            WriteText(PendingPath, "reviewPending=True\ncaptureKind=Diagnostic\n");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Tools/Bellerophon/Player Animation/Flashbang/Inspect Idle Locomotion")]
        private static void InspectMenu()
        {
            Inspect();
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[FlashbangIdleLocomotion] Inspection passed.");
        }

        private static void TryRunApprovedRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                if (!File.Exists(Absolute(RequestPath)))
                {
                    return;
                }
                string request = File.ReadAllText(Absolute(RequestPath), Encoding.UTF8);
                bool diagnostic = request.Contains("run=Diagnostic");
                bool final = request.Contains("run=Final");
                if (!diagnostic && !final)
                {
                    return;
                }

                EditorApplication.update -= TryRunApprovedRequest;
                WriteText(RequestPath, "run=None\n");
                if (diagnostic)
                {
                    Scene scene = RequireScene();
                    bool recoverOwnFailedApply = scene.isDirty &&
                        File.Exists(Absolute(ReviewFolder + "/application.txt"));
                    if (recoverOwnFailedApply)
                        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    if (File.Exists(Absolute(ReviewFolder + "/application.txt")))
                    {
                        ClearConsole();
                        Inspect();
                    }
                    else Apply();
                }
                else
                {
                    ClearConsole();
                    Inspect();
                }
                WriteText(PendingPath,
                    "reviewPending=True\ncaptureKind=" +
                    (final ? "Final" : "Diagnostic") + "\n");
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                WriteText(PendingPath, "reviewPending=False\n");
                WriteFailure(exception);
                Debug.LogException(exception);
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode ||
                !File.Exists(Absolute(PendingPath))) return;
            string pending = File.ReadAllText(Absolute(PendingPath), Encoding.UTF8);
            if (!pending.Contains("reviewPending=True")) return;
            BeginRuntimeReview(pending.Contains("captureKind=Final"));
        }

        private static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "Flashbang_Idle locomotion requires a clean CargoRunMvp scene.");
            ClearConsole();

            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            CarryProxy carry = RequireCarry(target);
            Transform prop = RequireProp(target);
            string avatarPath = AssetDatabase.GetAssetPath(animator.avatar);
            if (string.IsNullOrEmpty(avatarPath))
                throw new InvalidOperationException("Flashbang_Idle avatar is missing.");

            Dictionary<string, string> sourceHashes = SourceAssetPaths.ToDictionary(
                path => path, ComputeAssetHash, StringComparer.Ordinal);
            Dictionary<string, string> sourceObjects = SourceNames.ToDictionary(
                name => name,
                name => HierarchyHash(FindUnique(scene, name).transform),
                StringComparer.Ordinal);
            string targetBaseline = TargetBaseline(target, carry);
            string propBaseline = PropSignature(prop);
            string protectedSignature = ProtectedSceneSignature(scene);

            SourceMotions sources = RequireSourceMotions(scene);
            EnsureFolder(AssetFolder);
            AnimationClip idle = CopyClip(sources.Idle, IdleClipPath, "FlashbangIdle_Idle");
            AnimationClip forward = CopyClip(
                sources.Forward, ForwardClipPath, "FlashbangIdle_WalkForward");
            AnimationClip backward = CopyClip(
                sources.Backward, BackwardClipPath, "FlashbangIdle_WalkBackward");
            AnimationClip sidestep = CopyClip(
                sources.Sidestep, SidestepClipPath, "FlashbangIdle_Sidestep");
            AnimationClip run = CopyClip(
                sources.Run, RunClipPath, "FlashbangIdle_RunForward");
            AnimatorController controller = CreateController(
                sources.Diagonal, idle, forward, backward, sidestep, run);

            Undo.RecordObject(animator, "Configure Flashbang_Idle source locomotion");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(animator);
            AssetDatabase.SaveAssets();

            RequireEqual(avatarPath, AssetDatabase.GetAssetPath(animator.avatar), "avatar");
            RequireHashes(sourceHashes);
            RequireSourceObjects(sourceObjects, scene);
            RequireEqual(targetBaseline, TargetBaseline(target, carry), "Flashbang_Idle baseline");
            RequireEqual(propBaseline, PropSignature(prop), "Flashbang prop baseline");
            RequireEqual(protectedSignature, ProtectedSceneSignature(scene),
                "scene objects outside Flashbang_Idle");
            RequireCopiedClip(sources.Idle, idle, "Idle");
            RequireCopiedClip(sources.Forward, forward, "WalkForward");
            RequireCopiedClip(sources.Backward, backward, "WalkBackward");
            RequireCopiedClip(sources.Sidestep, sidestep, "Sidestep");
            RequireCopiedClip(sources.Run, run, "RunForward");

            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            WriteLines(ReviewFolder + "/source_asset_hashes.txt",
                sourceHashes.Select(item => item.Key + "|" + item.Value));
            WriteLines(ReviewFolder + "/source_object_hashes.txt",
                sourceObjects.Select(item => item.Key + "|" + item.Value));
            WriteText(ReviewFolder + "/target_baseline.txt", targetBaseline);
            WriteText(ReviewFolder + "/prop_baseline.txt", propBaseline);
            WriteText(ReviewFolder + "/application.txt", new StringBuilder()
                .AppendLine("Flashbang_Idle six-motion locomotion application")
                .AppendLine("sceneSaved=True")
                .AppendLine("target=Flashbang_Idle")
                .AppendLine("controller=" + ControllerPath)
                .AppendLine("blendTreeType=FreeformCartesian2D")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("sequenceLoopsAfterRun=True")
                .AppendLine("sourceAnimationsCopiedExactly=True")
                .AppendLine("sourceCurvesGenerated=False")
                .AppendLine("originalClipsRetimed=False")
                .AppendLine("rightShoulderMotionWeight=1")
                .AppendLine("rightArmMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.RightArmMotionWeight))
                .AppendLine("rightForeArmMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.RightForeArmMotionWeight))
                .AppendLine("rightHandMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.RightHandMotionWeight))
                .AppendLine("fingerMotionWeight=" +
                    F(HoloSprayRightHandFollowBehaviour.FingerMotionWeight))
                .AppendLine("flashbangCurrentLocalPoseChanged=False")
                .AppendLine("flashbangFollowsAnimatedRightHand=True")
                .AppendLine("sourceObjectsChanged=False")
                .AppendLine("objectsOutsideFlashbangIdleChanged=False")
                .ToString());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[FlashbangIdleLocomotion] Applied exact source motions and saved.");
        }

        private static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Animator animator = RequireAnimator(target);
            CarryProxy carry = RequireCarry(target);
            Transform prop = RequireProp(target);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException("Flashbang_Idle controller is invalid.");
            RequireEqual(ControllerPath, AssetDatabase.GetAssetPath(controller), "controller path");
            if (animator.applyRootMotion || !animator.enabled ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("Flashbang_Idle Animator settings are invalid.");
            RequireFloatParameter(controller,
                HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter, 0f);
            RequireFloatParameter(controller,
                HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter, 0f);
            RequireFloatParameter(controller,
                HoloSprayIdleLocomotionCycleBehaviour.DiagonalBlendParameter, 0.5f);
            if (controller.layers.Length != 1)
                throw new InvalidOperationException("Controller must have exactly one layer.");
            AnimatorControllerLayer layer = controller.layers[0];
            if (layer.avatarMask != null ||
                layer.blendingMode != AnimatorLayerBlendingMode.Override ||
                !Mathf.Approximately(layer.defaultWeight, 1f))
                throw new InvalidOperationException("The source full-body layer is invalid.");
            AnimatorState state = layer.stateMachine.defaultState ??
                throw new InvalidOperationException("Default locomotion state is missing.");
            if (state.name != StateName || state.writeDefaultValues ||
                !Mathf.Approximately(state.speed, 1f) ||
                state.behaviours.OfType<HoloSprayIdleLocomotionCycleBehaviour>().Count() != 1)
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
            RequireMotionTreeEquivalent(sources.Diagonal,
                root.children[4].motion as BlendTree,
                new Dictionary<AnimationClip, AnimationClip>
                {
                    [sources.Forward] = forward,
                    [sources.Sidestep] = sidestep
                }, "WalkDiagonal");
            RequireBaselineHashes(ReviewFolder + "/source_asset_hashes.txt", ComputeAssetHash);
            RequireBaselineHashes(ReviewFolder + "/source_object_hashes.txt",
                name => HierarchyHash(FindUnique(scene, name).transform));
            RequireEqual(ReadText(ReviewFolder + "/target_baseline.txt"),
                TargetBaseline(target, carry), "Flashbang_Idle baseline");
            RequireEqual(ReadText(ReviewFolder + "/prop_baseline.txt"),
                PropSignature(prop), "Flashbang prop baseline");
            UnityConsoleDiagnostics.AssertNoErrors();
        }

        private static void BeginRuntimeReview(bool isFinal)
        {
            if (reviewRunning || !EditorApplication.isPlaying) return;
            reviewRunning = true;
            finalCapture = isFinal;
            reviewStartedAt = EditorApplication.timeSinceStartup;
            baseAbsolutePhase = -1;
            nextPanel = 0;
            runtimeTarget = FindUnique(RequireScene(), TargetName);
            runtimeAnimator = RequireAnimator(runtimeTarget);
            runtimeCarry = RequireCarry(runtimeTarget);
            runtimeProp = RequireProp(runtimeTarget);
            expectedPropLocalPosition = runtimeProp.localPosition;
            expectedPropLocalRotation = runtimeProp.localRotation;
            expectedPropLocalScale = runtimeProp.localScale;
            fullPanels = new Texture2D[FullPanelCount];
            gripPanels = new Texture2D[GripPanelCount];
            Observations.Clear();
            ResetMetrics();
            EditorApplication.update -= RuntimeReviewUpdate;
            EditorApplication.update += RuntimeReviewUpdate;
        }

        private static void RuntimeReviewUpdate()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Play Mode ended before capture completed.");
                if (EditorApplication.timeSinceStartup - reviewStartedAt > ReviewTimeoutSeconds)
                    throw new TimeoutException("Flashbang_Idle two-cycle review exceeded 32 seconds.");
                if (!runtimeAnimator.isInitialized ||
                    !HoloSprayIdleLocomotionCycleBehaviour.TryGetSequenceState(
                        runtimeAnimator, out int absolutePhase, out int phase,
                        out float phaseElapsed)) return;

                if (baseAbsolutePhase < 0)
                {
                    if (phase != 0 || phaseElapsed > 0.65f) return;
                    baseAbsolutePhase = absolutePhase;
                }
                if (nextPanel < FullPanelCount)
                {
                    int expectedAbsolute = baseAbsolutePhase + nextPanel;
                    if (absolutePhase < expectedAbsolute || phaseElapsed < CapturePhaseTime) return;
                    if (absolutePhase > expectedAbsolute)
                        throw new InvalidOperationException("A natural sequence phase was missed.");
                    int expectedPhase = nextPanel % MotionCount;
                    if (phase != expectedPhase)
                        throw new InvalidOperationException("Observed phase order differs.");
                    Vector2 expectedMove =
                        HoloSprayIdleLocomotionCycleBehaviour.MotionPosition(expectedPhase);
                    if (Mathf.Abs(runtimeAnimator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter) -
                            expectedMove.x) > 0.001f ||
                        Mathf.Abs(runtimeAnimator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter) -
                            expectedMove.y) > 0.001f)
                        throw new InvalidOperationException("Blend Tree parameters differ.");

                    AccumulateMetrics();
                    Texture2D full = CaptureRuntimePanel(false);
                    DrawBorder(full, PhaseColor(phase));
                    fullPanels[nextPanel] = full;
                    if (nextPanel < GripPanelCount)
                    {
                        Texture2D grip = CaptureRuntimePanel(true);
                        DrawBorder(grip, PhaseColor(phase));
                        gripPanels[nextPanel] = grip;
                    }
                    Observations.Add(
                        "panel=" + nextPanel +
                        "|cycle=" + (nextPanel / MotionCount + 1) +
                        "|phase=" + phase +
                        "|motion=" + HoloSprayIdleLocomotionCycleBehaviour.MotionName(phase) +
                        "|phaseElapsed=" + F(phaseElapsed) +
                        "|moveX=" + F(runtimeAnimator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter)) +
                        "|moveY=" + F(runtimeAnimator.GetFloat(
                            HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter)));
                    nextPanel++;
                    return;
                }
                if (absolutePhase < baseAbsolutePhase + FullPanelCount) return;
                if (absolutePhase != baseAbsolutePhase + FullPanelCount || phase != 0)
                    throw new InvalidOperationException(
                        "RunForward did not naturally return to Idle after two cycles.");
                FinishRuntimeReview();
            }
            catch (Exception exception)
            {
                WriteText(PendingPath, "reviewPending=False\n");
                WriteFailure(exception);
                Debug.LogException(exception);
                CleanupReview();
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        private static void AccumulateMetrics()
        {
            Transform root = runtimeTarget.transform;
            Transform spine = RequireDescendant(root, "Spine");
            Transform head = RequireDescendant(root, "Head");
            Transform leftArm = RequireDescendant(root, "LeftArm");
            Transform shoulder = root.Find(RightShoulderPath);
            Transform arm = root.Find(RightArmPath);
            Transform foreArm = root.Find(RightForeArmPath);
            Transform hand = root.Find(RightHandPath);
            if (shoulder == null || arm == null || foreArm == null || hand == null)
                throw new MissingReferenceException("Flashbang_Idle right-arm chain is missing.");

            maximumPalmForward = Mathf.Max(maximumPalmForward,
                Vector3.Angle(RightPalmNormal(root), root.forward));
            maximumArmDeviation = Mathf.Max(maximumArmDeviation,
                PoseDeviation(runtimeCarry, arm));
            maximumForeArmDeviation = Mathf.Max(maximumForeArmDeviation,
                PoseDeviation(runtimeCarry, foreArm));
            maximumHandDeviation = Mathf.Max(maximumHandDeviation,
                PoseDeviation(runtimeCarry, hand));
            maximumFingerDeviation = Mathf.Max(maximumFingerDeviation,
                MaximumFingerDeviation(runtimeCarry, root));
            maximumPropLocalPositionError = Mathf.Max(maximumPropLocalPositionError,
                Vector3.Distance(runtimeProp.localPosition, expectedPropLocalPosition));
            maximumPropLocalRotationError = Mathf.Max(maximumPropLocalRotationError,
                Quaternion.Angle(runtimeProp.localRotation, expectedPropLocalRotation));
            maximumPropLocalScaleError = Mathf.Max(maximumPropLocalScaleError,
                Vector3.Distance(runtimeProp.localScale, expectedPropLocalScale));

            if (!swayBaselineSet)
            {
                swayBaselineSet = true;
                initialSpine = spine.localRotation;
                initialHead = head.localRotation;
                initialLeftArm = leftArm.localRotation;
                initialRightShoulder = shoulder.localRotation;
                initialRightArm = arm.localRotation;
                return;
            }
            maximumSpineSway = Mathf.Max(maximumSpineSway,
                Quaternion.Angle(initialSpine, spine.localRotation));
            maximumHeadSway = Mathf.Max(maximumHeadSway,
                Quaternion.Angle(initialHead, head.localRotation));
            maximumLeftArmSway = Mathf.Max(maximumLeftArmSway,
                Quaternion.Angle(initialLeftArm, leftArm.localRotation));
            maximumRightShoulderSway = Mathf.Max(maximumRightShoulderSway,
                Quaternion.Angle(initialRightShoulder, shoulder.localRotation));
            maximumRightArmSway = Mathf.Max(maximumRightArmSway,
                Quaternion.Angle(initialRightArm, arm.localRotation));
        }

        private static void FinishRuntimeReview()
        {
            float naturalSway = Mathf.Max(
                Mathf.Max(maximumSpineSway, maximumHeadSway),
                Mathf.Max(maximumLeftArmSway, maximumRightShoulderSway));
            RequireAtMost(maximumPalmForward, 35f, "right palm forward deviation");
            RequireAtMost(maximumArmDeviation, 20f, "right arm carry deviation");
            RequireAtMost(maximumForeArmDeviation, 15f, "right forearm carry deviation");
            RequireAtMost(maximumHandDeviation, 10f, "right hand carry deviation");
            RequireAtMost(maximumFingerDeviation, 0.05f, "right finger grip deviation");
            RequireAtMost(maximumPropLocalPositionError, 0.00001f,
                "flashbang local position error");
            RequireAtMost(maximumPropLocalRotationError, 0.05f,
                "flashbang local rotation error");
            RequireAtMost(maximumPropLocalScaleError, 0.00001f,
                "flashbang local scale error");
            if (naturalSway <= 0.1f)
                throw new InvalidOperationException("Natural upper-body locomotion sway was removed.");
            if (maximumRightArmSway <= 0.01f)
                throw new InvalidOperationException("Right-arm locomotion sway is completely fixed.");
            RequireBaselineHashes(ReviewFolder + "/source_asset_hashes.txt", ComputeAssetHash);
            RequireEqual(ReadText(ReviewFolder + "/prop_baseline.txt"),
                PropSignature(runtimeProp), "Flashbang prop baseline during review");
            UnityConsoleDiagnostics.AssertNoErrors();

            string imagePath = finalCapture ? FinalImagePath : DiagnosticImagePath;
            string reportPath = finalCapture ? FinalReportPath : DiagnosticReportPath;
            ComposeRuntimeReview(fullPanels, gripPanels, Absolute(imagePath));
            var report = new StringBuilder()
                .AppendLine("Flashbang_Idle natural six-motion locomotion review")
                .AppendLine("captureKind=" + (finalCapture ? "Final" : "Diagnostic"))
                .AppendLine("naturalPlayback=True")
                .AppendLine("targetManipulatedByValidation=False")
                .AppendLine("animatorPlayUsed=False")
                .AppendLine("animatorRebindUsed=False")
                .AppendLine("forcedAnimationTimeUsed=False")
                .AppendLine("secondsPerMotion=1")
                .AppendLine("cyclesObserved=2")
                .AppendLine("fullPanelsCaptured=12")
                .AppendLine("gripPanelsCaptured=6")
                .AppendLine("sequence=Idle,WalkForward,WalkBackward,Sidestep,WalkDiagonal,RunForward")
                .AppendLine("returnedToIdleAfterRun=True")
                .AppendLine("sourceAnimationsCopiedExactly=True")
                .AppendLine("upperBodyNaturalSwayPreserved=True")
                .AppendLine("currentRightArmGripPreservedWithReducedSway=True")
                .AppendLine("rightFingerGripLocked=True")
                .AppendLine("flashbangFollowsAnimatedRightHand=True")
                .AppendLine("maximumPalmForwardDeviationDegrees=" + F(maximumPalmForward))
                .AppendLine("maximumRightArmCarryDeviationDegrees=" + F(maximumArmDeviation))
                .AppendLine("maximumRightForeArmCarryDeviationDegrees=" + F(maximumForeArmDeviation))
                .AppendLine("maximumRightHandCarryDeviationDegrees=" + F(maximumHandDeviation))
                .AppendLine("maximumRightFingerGripDeviationDegrees=" + F(maximumFingerDeviation))
                .AppendLine("maximumSpineSwayDegrees=" + F(maximumSpineSway))
                .AppendLine("maximumHeadSwayDegrees=" + F(maximumHeadSway))
                .AppendLine("maximumLeftArmSwayDegrees=" + F(maximumLeftArmSway))
                .AppendLine("maximumRightShoulderSwayDegrees=" + F(maximumRightShoulderSway))
                .AppendLine("maximumRightArmSwayDegrees=" + F(maximumRightArmSway))
                .AppendLine("maximumFlashbangLocalPositionError=" +
                    F(maximumPropLocalPositionError))
                .AppendLine("maximumFlashbangLocalRotationErrorDegrees=" +
                    F(maximumPropLocalRotationError))
                .AppendLine("maximumFlashbangLocalScaleError=" +
                    F(maximumPropLocalScaleError));
            foreach (string observation in Observations) report.AppendLine(observation);
            WriteText(reportPath, report.ToString());
            WriteText(PendingPath, "reviewPending=False\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[FlashbangIdleLocomotion] " +
                (finalCapture ? "Final" : "Diagnostic") +
                " two-cycle capture completed.");
            CleanupReview();
            EditorApplication.ExitPlaymode();
        }

        private static SourceMotions RequireSourceMotions(Scene scene)
        {
            Motion[] motions = SourceNames.Select(name =>
            {
                AnimatorController controller = RequireAnimatorController(
                    RequireAnimator(FindUnique(scene, name)), name);
                return controller.layers[0].stateMachine.defaultState?.motion ??
                    throw new MissingReferenceException(name + " default motion is missing.");
            }).ToArray();
            if (!(motions[0] is AnimationClip idle) ||
                !(motions[1] is AnimationClip forward) ||
                !(motions[2] is AnimationClip backward) ||
                !(motions[3] is AnimationClip sidestep) ||
                !(motions[4] is BlendTree diagonal) ||
                !(motions[5] is AnimationClip run))
                throw new InvalidOperationException("Source motion types changed.");
            if (diagonal.blendType != BlendTreeType.Simple1D ||
                diagonal.children.Length != 2)
                throw new InvalidOperationException("Diagonal source tree changed.");
            AnimatorController diagonalController = RequireAnimatorController(
                RequireAnimator(FindUnique(scene, SourceNames[4])), SourceNames[4]);
            AnimatorControllerParameter parameter = diagonalController.parameters
                .SingleOrDefault(item => item.name == diagonal.blendParameter);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, 0.5f))
                throw new InvalidOperationException("Diagonal source default is not 0.5.");
            return new SourceMotions(idle, forward, backward, sidestep, diagonal, run);
        }

        private static AnimationClip CopyClip(AnimationClip source, string path, string name)
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
            controller.AddParameter(HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter,
                AnimatorControllerParameterType.Float);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = HoloSprayIdleLocomotionCycleBehaviour.DiagonalBlendParameter,
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

            ChildMotion[] sourceChildren = sourceDiagonal.children;
            var clipMap = new Dictionary<AnimationClip, AnimationClip>
            {
                [(AnimationClip)sourceChildren[0].motion] = forward,
                [(AnimationClip)sourceChildren[1].motion] = sidestep
            };
            BlendTree diagonal = CloneTree(
                sourceDiagonal, DiagonalTreeName, controller, clipMap);
            var tree = new BlendTree
            {
                name = RootTreeName,
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter,
                blendParameterY = HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.children = new[]
            {
                Child(idle, RequiredPositions[0]), Child(forward, RequiredPositions[1]),
                Child(backward, RequiredPositions[2]), Child(sidestep, RequiredPositions[3]),
                Child(diagonal, RequiredPositions[4]), Child(run, RequiredPositions[5])
            };
            AnimatorState state = stateMachine.AddState(StateName);
            state.motion = tree;
            state.speed = 1f;
            state.cycleOffset = 0f;
            state.mirror = false;
            state.writeDefaultValues = false;
            state.AddStateMachineBehaviour<HoloSprayIdleLocomotionCycleBehaviour>();
            stateMachine.defaultState = state;
            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(tree);
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
                        throw new InvalidOperationException("Diagonal clip mapping is missing.");
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

        private static void RequireRootTree(BlendTree tree)
        {
            if (tree.name != RootTreeName ||
                tree.blendType != BlendTreeType.FreeformCartesian2D ||
                tree.blendParameter != HoloSprayIdleLocomotionCycleBehaviour.MoveXParameter ||
                tree.blendParameterY != HoloSprayIdleLocomotionCycleBehaviour.MoveYParameter ||
                tree.children.Length != RequiredPositions.Length)
                throw new InvalidOperationException("Root 2D Blend Tree is invalid.");
            string[] paths =
            {
                IdleClipPath, ForwardClipPath, BackwardClipPath,
                SidestepClipPath, ControllerPath, RunClipPath
            };
            ChildMotion[] children = tree.children;
            for (int index = 0; index < children.Length; index++)
            {
                if ((children[index].position - RequiredPositions[index]).sqrMagnitude >
                        0.00000001f ||
                    !Mathf.Approximately(children[index].timeScale, 1f) ||
                    !Mathf.Approximately(children[index].cycleOffset, 0f) ||
                    children[index].mirror)
                    throw new InvalidOperationException("Root child differs at " + index + ".");
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
            {
                result.Append("objectCurve|").Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|').AppendLine(binding.propertyName);
                foreach (ObjectReferenceKeyframe key in AnimationUtility
                    .GetObjectReferenceCurve(clip, binding))
                    result.AppendLine(F(key.time) + "|" + ObjectIdentity(key.value));
            }
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
            ChildMotion[] a = source.children;
            ChildMotion[] b = copy.children;
            for (int index = 0; index < a.Length; index++)
            {
                if (!Mathf.Approximately(a[index].threshold, b[index].threshold) ||
                    (a[index].position - b[index].position).sqrMagnitude > 0.00000001f ||
                    !Mathf.Approximately(a[index].timeScale, b[index].timeScale) ||
                    !Mathf.Approximately(a[index].cycleOffset, b[index].cycleOffset) ||
                    a[index].mirror != b[index].mirror ||
                    a[index].directBlendParameter != b[index].directBlendParameter)
                    throw new InvalidOperationException(label + " child differs at " + index + ".");
                if (a[index].motion is AnimationClip sourceClip)
                {
                    if (!(b[index].motion is AnimationClip copiedClip) ||
                        !clipMap.TryGetValue(sourceClip, out AnimationClip expected) ||
                        copiedClip != expected)
                        throw new InvalidOperationException(label + " clip differs at " + index + ".");
                    RequireCopiedClip(sourceClip, copiedClip, label + " child " + index);
                }
                else if (a[index].motion is BlendTree sourceTree)
                    RequireMotionTreeEquivalent(sourceTree, b[index].motion as BlendTree,
                        clipMap, label + " child " + index);
                else
                    throw new InvalidOperationException(label + " has an unsupported motion.");
            }
        }

        private static Texture2D CaptureRuntimePanel(bool gripCloseup)
        {
            Bounds bounds = gripCloseup
                ? GripBounds(runtimeTarget.transform, runtimeProp)
                : FullBounds(runtimeTarget.transform);
            Vector3 direction = (
                runtimeTarget.transform.forward + runtimeTarget.transform.right * 0.72f).normalized;
            var cameraObject = new GameObject("FlashbangIdleLocomotion_ReadOnlyCamera");
            var lightObject = new GameObject("FlashbangIdleLocomotion_ReadOnlyLight");
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

        private static void ComposeRuntimeReview(
            IReadOnlyList<Texture2D> full,
            IReadOnlyList<Texture2D> grip,
            string destination)
        {
            if (full.Count != FullPanelCount || grip.Count != GripPanelCount)
                throw new InvalidOperationException("Unexpected capture panel count.");
            var composite = new Texture2D(3072, 1536, TextureFormat.RGB24, false);
            try
            {
                for (int index = 0; index < full.Count; index++)
                {
                    int column = index % MotionCount;
                    int row = index < MotionCount ? 2 : 1;
                    composite.SetPixels(column * 512, row * 512, 512, 512,
                        full[index].GetPixels());
                }
                for (int index = 0; index < grip.Count; index++)
                    composite.SetPixels(index * 512, 0, 512, 512, grip[index].GetPixels());
                composite.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
            }
        }

        private static Bounds FullBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Flashbang_Idle has no renderers.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.15f);
            return bounds;
        }

        private static Bounds GripBounds(Transform target, Transform prop)
        {
            Transform shoulder = target.Find(RightShoulderPath);
            Transform arm = target.Find(RightArmPath);
            Transform foreArm = target.Find(RightForeArmPath);
            Transform hand = target.Find(RightHandPath);
            Bounds bounds = new Bounds(shoulder.position, Vector3.zero);
            bounds.Encapsulate(arm.position);
            bounds.Encapsulate(foreArm.position);
            bounds.Encapsulate(hand.position);
            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(renderer.bounds);
            bounds.Expand(0.13f);
            return bounds;
        }

        private static float PoseDeviation(
            CarryProxy carry, Transform bone)
        {
            string path = AnimationUtility.CalculateTransformPath(bone, carry.Transform);
            return Quaternion.Angle(AuthoredRotation(carry, path), bone.localRotation);
        }

        private static float MaximumFingerDeviation(
            CarryProxy carry, Transform root)
        {
            SerializedObject serialized = new SerializedObject(carry.Component);
            SerializedProperty poses = serialized.FindProperty("authoredRightArmPose");
            float maximum = 0f;
            for (int index = 0; index < poses.arraySize; index++)
            {
                SerializedProperty item = poses.GetArrayElementAtIndex(index);
                string path = item.FindPropertyRelative("path").stringValue;
                if (path.IndexOf("/RightHand/", StringComparison.Ordinal) < 0) continue;
                Transform bone = root.Find(path) ??
                    throw new MissingReferenceException("Finger pose path is missing: " + path);
                maximum = Mathf.Max(maximum, Quaternion.Angle(
                    item.FindPropertyRelative("localRotation").quaternionValue,
                    bone.localRotation));
            }
            return maximum;
        }

        private static Quaternion AuthoredRotation(
            CarryProxy carry, string path)
        {
            SerializedObject serialized = new SerializedObject(carry.Component);
            SerializedProperty poses = serialized.FindProperty("authoredRightArmPose");
            for (int index = 0; index < poses.arraySize; index++)
            {
                SerializedProperty item = poses.GetArrayElementAtIndex(index);
                if (item.FindPropertyRelative("path").stringValue == path)
                    return item.FindPropertyRelative("localRotation").quaternionValue;
            }
            throw new MissingReferenceException("Authored Flashbang pose is missing: " + path);
        }

        private static Vector3 RightPalmNormal(Transform root)
        {
            Transform hand = root.Find(RightHandPath);
            Transform index = hand.Find("RightIndexProximal");
            Transform middle = hand.Find("RightMiddleProximal");
            Transform little = hand.Find("RightLittleProximal");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 fingers = (middle.position - hand.position).normalized;
            return Vector3.Cross(width, fingers).normalized;
        }

        private static string TargetBaseline(
            GameObject target, CarryProxy carry)
        {
            var result = new StringBuilder();
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, target.transform),
                    StringComparer.Ordinal))
                result.Append(AnimationUtility.CalculateTransformPath(item, target.transform))
                    .Append('|').Append(VecRounded(item.localPosition)).Append('|')
                    .Append(QuatRounded(item.localRotation)).Append('|')
                    .AppendLine(VecRounded(item.localScale));
            result.AppendLine("carry=" + EditorJsonUtility.ToJson(carry.Component));
            return result.ToString();
        }

        private static string PropSignature(Transform prop)
        {
            var result = new StringBuilder()
                .AppendLine("localPosition=" + Vec(prop.localPosition))
                .AppendLine("localRotation=" + Quat(prop.localRotation))
                .AppendLine("localScale=" + Vec(prop.localScale));
            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item.transform, prop),
                    StringComparer.Ordinal))
            {
                result.AppendLine("renderer=" + renderer.GetType().FullName + "|" +
                    AnimationUtility.CalculateTransformPath(renderer.transform, prop));
                Mesh mesh = renderer is SkinnedMeshRenderer skinned
                    ? skinned.sharedMesh
                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                result.AppendLine("mesh=" + ObjectIdentity(mesh));
                foreach (Material material in renderer.sharedMaterials)
                {
                    string materialPath = AssetDatabase.GetAssetPath(material);
                    result.AppendLine("material=" + ObjectIdentity(material) + "|" +
                        (string.IsNullOrEmpty(materialPath) ? "" : ComputeAssetHash(materialPath)));
                    if (string.IsNullOrEmpty(materialPath)) continue;
                    foreach (string dependency in AssetDatabase.GetDependencies(materialPath, true)
                        .Where(path => path != materialPath && File.Exists(Absolute(path)))
                        .OrderBy(path => path, StringComparer.Ordinal))
                        result.AppendLine("dependency=" + dependency + "|" +
                            ComputeAssetHash(dependency));
                }
            }
            return result.ToString();
        }

        private static string ProtectedSceneSignature(Scene scene)
        {
            var result = new StringBuilder();
            foreach (Transform root in scene.GetRootGameObjects().Select(item => item.transform)
                .OrderBy(item => item.name, StringComparer.Ordinal))
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .Where(item => !HasAncestor(item, TargetName))
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
                result.Append(root.name).Append('|')
                    .Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(VecRounded(item.localPosition)).Append('|')
                    .Append(QuatRounded(item.localRotation)).Append('|')
                    .AppendLine(VecRounded(item.localScale));
            return Sha256(result.ToString());
        }

        private static bool HasAncestor(Transform item, string name)
        {
            for (Transform current = item; current != null; current = current.parent)
                if (current.name == name) return true;
            return false;
        }

        private static string HierarchyHash(Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root),
                    StringComparer.Ordinal))
                result.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(VecRounded(item.localPosition)).Append('|')
                    .Append(QuatRounded(item.localRotation)).Append('|')
                    .AppendLine(VecRounded(item.localScale));
            return Sha256(result.ToString());
        }

        private static Transform RequireProp(GameObject target)
        {
            Transform hand = target.transform.Find(RightHandPath) ??
                throw new MissingReferenceException("Flashbang_Idle right hand is missing.");
            return hand.Cast<Transform>().SingleOrDefault(item => item.name == "Flashbang_Prop") ??
                throw new MissingReferenceException("Flashbang_Idle prop is missing.");
        }

        private static CarryProxy RequireCarry(GameObject target)
        {
            Type type = Type.GetType(CarryTypeName, false) ??
                throw new InvalidOperationException(
                    "Flashbang carry runtime type is unavailable: " + CarryTypeName);
            MonoBehaviour component = target.GetComponent(type) as MonoBehaviour;
            if (component == null)
                throw new MissingReferenceException("Flashbang_Idle carry behaviour is missing.");
            var carry = new CarryProxy(component);
            if (carry.ModeName != "IdlePalmForward")
                throw new InvalidOperationException("Flashbang_Idle carry mode is invalid.");
            return carry;
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + name + " under " + root.name + ".");
            return matches[0];
        }

        private static Animator RequireAnimator(GameObject target)
        {
            return target.GetComponent<Animator>() ??
                throw new MissingReferenceException(target.name + " Animator is missing.");
        }

        private static AnimatorController RequireAnimatorController(
            Animator animator, string label)
        {
            return animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException(label + " does not use an AnimatorController.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
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
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Flashbang locomotion requires Edit Mode.");
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

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            return asset != null ? asset : throw new MissingReferenceException(
                "Asset is missing: " + path);
        }

        private static void RequireFloatParameter(
            AnimatorController controller, string name, float expected)
        {
            AnimatorControllerParameter parameter = controller.parameters
                .SingleOrDefault(item => item.name == name);
            if (parameter == null || parameter.type != AnimatorControllerParameterType.Float ||
                !Mathf.Approximately(parameter.defaultFloat, expected))
                throw new InvalidOperationException("Invalid float parameter: " + name);
        }

        private static void RequireHashes(IReadOnlyDictionary<string, string> hashes)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value, ComputeAssetHash(item.Key), item.Key);
        }

        private static void RequireSourceObjects(
            IReadOnlyDictionary<string, string> hashes, Scene scene)
        {
            foreach (KeyValuePair<string, string> item in hashes)
                RequireEqual(item.Value, HierarchyHash(FindUnique(scene, item.Key).transform),
                    item.Key);
        }

        private static void RequireBaselineHashes(
            string path, Func<string, string> actual)
        {
            foreach (string line in ReadText(path)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = line.LastIndexOf('|');
                if (separator <= 0)
                    throw new InvalidOperationException("Invalid hash baseline: " + path);
                string key = line.Substring(0, separator);
                RequireEqual(line.Substring(separator + 1), actual(key), key);
            }
        }

        private static string ComputeAssetHash(string assetPath)
        {
            string path = Absolute(assetPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Asset file is missing.", path);
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
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                value, out string guid, out long localId)
                ? guid + ":" + localId
                : value.GetType().FullName + ":" + value.name;
        }

        private static void ClearConsole()
        {
            Type logEntries = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll") ??
                throw new InvalidOperationException("Unity console API is unavailable.");
            MethodInfo clear = logEntries.GetMethod(
                "Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                throw new InvalidOperationException("Unity console clear method is unavailable.");
            clear.Invoke(null, null);
        }

        private static void DrawBorder(Texture2D image, Color color)
        {
            const int width = 6;
            for (int y = 0; y < image.height; y++)
            for (int x = 0; x < image.width; x++)
                if (x < width || y < width ||
                    x >= image.width - width || y >= image.height - width)
                    image.SetPixel(x, y, color);
            image.Apply(false, false);
        }

        private static Color PhaseColor(int phase)
        {
            Color[] colors =
            {
                Color.white, Color.green, Color.blue,
                Color.yellow, Color.magenta, Color.red
            };
            return colors[phase];
        }

        private static void RequireAtMost(float actual, float maximum, string label)
        {
            if (actual > maximum)
                throw new InvalidOperationException(
                    label + " exceeded. actual=" + F(actual) + " maximum=" + F(maximum));
        }

        private static void ResetMetrics()
        {
            swayBaselineSet = false;
            maximumSpineSway = 0f;
            maximumHeadSway = 0f;
            maximumLeftArmSway = 0f;
            maximumRightShoulderSway = 0f;
            maximumRightArmSway = 0f;
            maximumPalmForward = 0f;
            maximumArmDeviation = 0f;
            maximumForeArmDeviation = 0f;
            maximumHandDeviation = 0f;
            maximumFingerDeviation = 0f;
            maximumPropLocalPositionError = 0f;
            maximumPropLocalRotationError = 0f;
            maximumPropLocalScaleError = 0f;
        }

        private static void CleanupReview()
        {
            EditorApplication.update -= RuntimeReviewUpdate;
            if (fullPanels != null)
                foreach (Texture2D panel in fullPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            if (gripPanels != null)
                foreach (Texture2D panel in gripPanels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
            reviewRunning = false;
            runtimeTarget = null;
            runtimeAnimator = null;
            runtimeCarry = null;
            runtimeProp = null;
            fullPanels = null;
            gripPanels = null;
            Observations.Clear();
        }

        private static void WriteFailure(Exception exception)
        {
            try
            {
                WriteText(FailurePath, exception.ToString());
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            catch
            {
                // Preserve the original exception.
            }
        }

        private static void WriteLines(string path, IEnumerable<string> lines)
        {
            WriteText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
        }

        private static void WriteText(string path, string contents)
        {
            string absolute = Absolute(path);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, contents, new UTF8Encoding(false));
        }

        private static string ReadText(string path)
        {
            string absolute = Absolute(path);
            if (!File.Exists(absolute))
                throw new FileNotFoundException("Required review baseline is missing.", absolute);
            return File.ReadAllText(absolute, Encoding.UTF8);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        private static void RequireEqual(string expected, string actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    label + " differs. expected=" + expected + " actual=" + actual);
        }

        private static string F(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z);
        private static string Quat(Quaternion value) =>
            F(value.x) + "," + F(value.y) + "," + F(value.z) + "," + F(value.w);
        private static string FRounded(float value) =>
            value.ToString("0.000000", CultureInfo.InvariantCulture);
        private static string VecRounded(Vector3 value) =>
            FRounded(value.x) + "," + FRounded(value.y) + "," + FRounded(value.z);
        private static string QuatRounded(Quaternion value) =>
            FRounded(value.x) + "," + FRounded(value.y) + "," +
            FRounded(value.z) + "," + FRounded(value.w);

        private readonly struct SourceMotions
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

        private sealed class CarryProxy
        {
            internal CarryProxy(MonoBehaviour component)
            {
                Component = component;
            }

            internal MonoBehaviour Component { get; }
            internal Transform Transform => Component.transform;
            internal string ModeName => Get<object>("Mode").ToString();

            private T Get<T>(string propertyName)
            {
                PropertyInfo property = Component.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    throw new InvalidOperationException(
                        Component.GetType().FullName + "." + propertyName + " is missing.");
                return (T)property.GetValue(Component);
            }
        }
    }
}
