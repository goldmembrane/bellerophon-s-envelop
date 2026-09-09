using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ShieldBreakArtSampleTools
    {
        private const string ShieldMeshPath =
            "Assets/_Project/Art/Items/Shield/Meshes/Shield_TextFree_Window.asset";
        private const string ShieldBodyMaterialPath =
            "Assets/_Project/Art/Items/Shield/Materials/Shield_TextFree.mat";
        private const string ShieldWindowMaterialPath =
            "Assets/_Project/Art/Items/Shield/Materials/Shield_Window_Transparent.mat";
        private const string BreakAnimationPath =
            "Assets/_Project/Art/Player/Animations/Shield/Shield_Break_Reaction_Mixamo.fbx";
        private const string BreakClipName = "Shield_Break_Reaction_Mixamo";
        private const string OutputFolder = "artSample/shield_break_2026-09-08";
        private const string FrameFolder = OutputFolder + "/frames";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Shield_Break_Reaction";
        private const string ShieldObjectName = "Shield_RightForeArm";
        private const string RuntimeFragmentFolder =
            "Assets/_Project/Art/Items/Shield/BreakFragments";
        private const string RuntimeValidationFolder =
            "docs/validation/shield_break_disappear_after_1s_2026-09-08";
        private const string RuntimeFragmentRootName = "ShieldBreakDetachedFragments";
        private const float ApprovedStartDelaySeconds = 0.05f;
        private const float ApprovedCycleDurationSeconds = 2.866667f;
        private const float ApprovedTravelMultiplier = 1.9f;
        private const float ApprovedGravityBaseMultiplier = 0.18f;
        private const float ApprovedGravityProgressMultiplier = 0.62f;
        private const float ApprovedLinearSpeedMultiplier = 10f;
        private const float ApprovedFragmentVisibleDurationSeconds = 1f;
        private const string ApprovedShieldMeshSha =
            "B9289C31B9673E8A4D6EC195D847D06165DFC1834076E30F91537EB825929AFE";
        private const string ApprovedBodyMaterialSha =
            "BA4B8B624AAD262EDB23B5462F467961C8DDC1191B520CF84AEFADA1645CDE0F";
        private const string ApprovedWindowMaterialSha =
            "38D25DD80976EDFB116F0131D755D1328964A717E28A6B976900C2D96127850B";
        private const string ApprovedBreakAnimationSha =
            "7CE9FDBC3FE3B042E6E8CB5E0637FB3FFDD8E5D77A1CEDF0E270BA6E41F46B7D";
        private const string ApprovedSourceImageSha =
            "000BDC8F40073BD509420C36F27ACDB798B45969079047D20264368762466663";
        private const string ApprovedPreviewSha =
            "098D348DE37BA7B824B84B70B6729079243118C6CC747D5F569356CB962AE072";
        private const string ApprovedComparisonSha =
            "B95E37DF1B3408C1F3F15912ACC71FF63A42B56E5C599D05D230076FA4F47512";
        private const string ApprovedManifestSha =
            "CAD9FC5C788ED959489A24680DEEFB2BEF051EA16DCC4A49912F73231FAAEA75";
        private const int MaximumPieceCount = 20;
        private const int PieceCount = 17;
        private const int FramesPerSecond = 30;
        private const int RenderWidth = 720;
        private const int RenderHeight = 900;
        private const int RuntimeCaptureWidth = 300;
        private const int RuntimeCaptureHeight = 380;
        private const int RandomSeed = 20260908;
        private const int FragmentShapeSeed = 20260913;

        private static bool inspectingRuntime;
        private static double runtimeInspectionStartedAt;
        private static int runtimeInspectionStartDetachCount;
        private static int runtimeInspectionStartResetCount;
        private static int runtimeInspectionStartHideCount;
        private static int runtimeInspectionLastRecordedDetachCount;
        private static int runtimeInspectionLastRecordedHideCount;
        private static bool observedIntactBeforeDelay;
        private static bool observedDetachedAfterDelay;
        private static bool observedDetachedWorldAnchor;
        private static bool observedFractureEnd;
        private static bool observedLoopReset;
        private static bool observedFragmentsVisibleBeforeHide;
        private static bool observedFragmentsHiddenAfterOneSecond;
        private static bool observedFragmentsHiddenUntilReset;
        private static float maximumLivePoseError;
        private static float maximumDetachedAnchorPositionError;
        private static float maximumDetachedAnchorRotationError;
        private static float maximumRuntimeScaleError;
        private static float minimumDetachCycleTime = float.PositiveInfinity;
        private static float maximumDetachCycleTime = float.NegativeInfinity;
        private static float maximumPreDetachCycleTime = float.NegativeInfinity;
        private static bool allDetachesCrossedThresholdOnFirstFrame;
        private static float minimumHideCycleTime = float.PositiveInfinity;
        private static float maximumHideCycleTime = float.NegativeInfinity;
        private static float maximumPreHideCycleTime = float.NegativeInfinity;
        private static bool allHidesCrossedThresholdOnFirstFrame;
        private static bool observedEndTravel;
        private static bool capturedEndTravelStart;
        private static Vector3 endTravelStartPosition;
        private static float observedEndTravelDistance;

        private static bool capturingRuntime;
        private static int captureCycleCount;
        private static int capturePhase;
        private static string runtimeCaptureOutputPath;
        private static string runtimeCaptureFrameFolder;
        private static readonly float[] CapturePhaseTimes =
            { 0.02f, 0.08f, 0.35f, 0.80f, 1.02f, 1.10f, 2.75f };

        public static void ApplyApprovedRuntime()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Approved Shield break runtime must be applied in Edit Mode.");
            VerifyApprovedSourceIntegrity();
            Scene scene = RequireScene();
            string[] unchangedRootSignatures = OtherRootSignatures(scene);
            GameObject target = FindUnique(scene, TargetName);
            ShieldStateMotion stateMotion = target.GetComponent<ShieldStateMotion>() ??
                throw new InvalidOperationException(TargetName + " is missing ShieldStateMotion.");
            if (stateMotion.MotionKind != ShieldStateMotionKind.BreakReaction)
                throw new InvalidOperationException(TargetName + " is not configured as BreakReaction.");
            if (Mathf.Abs(stateMotion.CycleDurationSeconds - ApprovedCycleDurationSeconds) > 0.00001f)
                throw new InvalidOperationException("Shield_Break_Reaction cycle duration differs from the approved sample.");

            Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
            Transform[] shieldMatches = forearm.Cast<Transform>()
                .Where(item => item.name == ShieldObjectName).ToArray();
            if (shieldMatches.Length != 1)
                throw new InvalidOperationException(TargetName + " requires one right-forearm Shield. Found=" +
                    shieldMatches.Length);
            Transform shield = shieldMatches[0];
            MeshFilter[] shieldFilters = shield.GetComponentsInChildren<MeshFilter>(true)
                .Where(item => item.sharedMesh != null).ToArray();
            if (shieldFilters.Length != 1)
                throw new InvalidOperationException("Approved Shield runtime expects one source MeshFilter. Found=" +
                    shieldFilters.Length);
            Mesh source = AssetDatabase.LoadAssetAtPath<Mesh>(ShieldMeshPath) ??
                throw new InvalidOperationException("Approved Shield mesh is missing.");
            if (shieldFilters[0].sharedMesh != source)
                throw new InvalidOperationException("Scene Shield does not reference the approved text-free window mesh.");
            Renderer[] intactRenderers = shield.GetComponentsInChildren<Renderer>(true);
            if (intactRenderers.Length == 0)
                throw new InvalidOperationException("Scene Shield has no intact renderer.");

            Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShieldBodyMaterialPath) ??
                throw new InvalidOperationException("Approved Shield body material is missing.");
            Material windowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShieldWindowMaterialPath) ??
                throw new InvalidOperationException("Approved Shield window material is missing.");
            Material[] intactMaterials = intactRenderers.SelectMany(item => item.sharedMaterials).ToArray();
            if (!intactMaterials.Contains(bodyMaterial) || !intactMaterials.Contains(windowMaterial))
                throw new InvalidOperationException("Scene Shield does not use both approved body and window materials.");

            MeshSourceData sourceData = ReadMesh(source);
            FragmentMeshData[] fragments = BuildIrregularFragments(sourceData, source.bounds, out _);
            if (fragments.Length != PieceCount || PieceCount > MaximumPieceCount)
                throw new InvalidOperationException("Approved Shield fragment count is invalid.");
            ApprovedMotionData motion = BuildApprovedMotionData();
            Vector3[] centers = fragments.Select(item => item.Bounds.center).ToArray();

            EnsureAssetFolder(RuntimeFragmentFolder);
            var runtimeMeshes = new Mesh[PieceCount];
            for (int index = 0; index < PieceCount; index++)
            {
                string assetPath = RuntimeFragmentFolder + "/ShieldBreakFragment_" +
                    (index + 1).ToString("D2") + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null &&
                    !AssetDatabase.DeleteAsset(assetPath))
                    throw new InvalidOperationException("Existing approved fragment could not be replaced: " + assetPath);
                Mesh mesh = BuildFragmentMesh(fragments[index], index, source.indexFormat);
                mesh.hideFlags = HideFlags.None;
                AssetDatabase.CreateAsset(mesh, assetPath);
                runtimeMeshes[index] = mesh;
            }

            Transform[] oldRoots = target.GetComponentsInChildren<Transform>(true)
                .Where(item => item != target.transform && item.name == RuntimeFragmentRootName).ToArray();
            foreach (Transform oldRoot in oldRoots)
                UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);

            var rootObject = new GameObject(RuntimeFragmentRootName);
            Transform fragmentRoot = rootObject.transform;
            fragmentRoot.SetParent(target.transform, false);
            rootObject.SetActive(false);
            var pivots = new Transform[PieceCount];
            for (int index = 0; index < PieceCount; index++)
            {
                var pivotObject = new GameObject("ShieldFragment_" + (index + 1).ToString("D2"));
                Transform pivot = pivotObject.transform;
                pivot.SetParent(fragmentRoot, false);
                pivot.localPosition = centers[index];
                var geometryObject = new GameObject("ApprovedShieldGeometry");
                Transform geometry = geometryObject.transform;
                geometry.SetParent(pivot, false);
                geometry.localPosition = -centers[index];
                geometryObject.AddComponent<MeshFilter>().sharedMesh = runtimeMeshes[index];
                geometryObject.AddComponent<MeshRenderer>().sharedMaterials =
                    new[] { bodyMaterial, windowMaterial };
                pivots[index] = pivot;
            }

            ShieldBreakReactionFracture runtime = target.GetComponent<ShieldBreakReactionFracture>();
            if (runtime == null) runtime = target.AddComponent<ShieldBreakReactionFracture>();
            float extent = Mathf.Max(source.bounds.size.x, source.bounds.size.z);
            runtime.Configure(stateMotion, shield, intactRenderers, target.transform, fragmentRoot, pivots,
                centers, motion.Directions, motion.RotationAxes, motion.RotationDegrees,
                motion.DistanceScales, ApprovedStartDelaySeconds, ApprovedCycleDurationSeconds, extent);
            EditorUtility.SetDirty(runtime);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(rootObject);
            AssetDatabase.SaveAssets();

            if (!unchangedRootSignatures.SequenceEqual(OtherRootSignatures(scene), StringComparer.Ordinal))
                throw new InvalidOperationException("A CargoRunMvp scene root outside Shield_Break_Reaction changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after Shield fracture application.");
            VerifyApprovedSourceIntegrity();

            Directory.CreateDirectory(Absolute(RuntimeValidationFolder));
            var report = new StringBuilder();
            report.AppendLine("Approved Shield fragment design applied with the user-approved runtime speed override.");
            report.AppendLine("target=" + TargetName);
            report.AppendLine("fragmentCount=" + PieceCount + " approvedMaximum=" + MaximumPieceCount);
            report.AppendLine("startDelaySeconds=" + ApprovedStartDelaySeconds.ToString("F6", CultureInfo.InvariantCulture));
            report.AppendLine("fractureDurationSeconds=" +
                (ApprovedCycleDurationSeconds - ApprovedStartDelaySeconds).ToString("F6", CultureInfo.InvariantCulture));
            report.AppendLine("cycleDurationSeconds=" + ApprovedCycleDurationSeconds.ToString("F6", CultureInfo.InvariantCulture));
            report.AppendLine("shapeSeed=" + FragmentShapeSeed + " motionSeed=" + RandomSeed);
            report.AppendLine("linearSpeedMultiplier=" +
                ApprovedLinearSpeedMultiplier.ToString("F3", CultureInfo.InvariantCulture) +
                " translationInterpolation=linear continuesThroughAnimationEnd=True");
            report.AppendLine("fragmentVisibleDurationSeconds=" +
                ApprovedFragmentVisibleDurationSeconds.ToString("F3", CultureInfo.InvariantCulture) +
                " hideCycleTimeSeconds=" +
                (ApprovedStartDelaySeconds + ApprovedFragmentVisibleDurationSeconds)
                    .ToString("F3", CultureInfo.InvariantCulture) +
                " remainsHiddenUntilLoopReset=True");
            report.AppendLine("fragmentRootDetachesToWorld=True loopRestoresIntactShield=True");
            report.AppendLine("approvedMeshAndMaterialsPreserved=True otherSceneRootsUnchanged=True sceneSaved=True");
            File.WriteAllText(Absolute(RuntimeValidationFolder + "/applied.txt"), report.ToString(), Encoding.UTF8);
            Debug.Log("Approved Shield break runtime applied exactly: fragments=" + PieceCount +
                " startDelay=" + ApprovedStartDelaySeconds.ToString("F3", CultureInfo.InvariantCulture));
        }

        public static void Generate()
        {
            Mesh source = AssetDatabase.LoadAssetAtPath<Mesh>(ShieldMeshPath) ??
                throw new InvalidOperationException("Approved Shield mesh is missing: " + ShieldMeshPath);
            Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShieldBodyMaterialPath) ??
                throw new InvalidOperationException("Approved Shield body material is missing.");
            Material windowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShieldWindowMaterialPath) ??
                throw new InvalidOperationException("Approved Shield window material is missing.");
            AnimationClip breakClip = AssetDatabase.LoadAllAssetsAtPath(BreakAnimationPath)
                .OfType<AnimationClip>()
                .SingleOrDefault(clip => clip.name == BreakClipName) ??
                throw new InvalidOperationException("Imported Shield break clip is missing.");
            if (source.subMeshCount != 2)
                throw new InvalidOperationException("Approved Shield sample requires exactly body and window submeshes.");

            string absoluteOutput = Absolute(OutputFolder);
            string absoluteFrames = Absolute(FrameFolder);
            Directory.CreateDirectory(absoluteOutput);
            Directory.CreateDirectory(absoluteFrames);
            foreach (string oldFrame in Directory.GetFiles(absoluteFrames, "frame_*.png"))
                File.Delete(oldFrame);

            MeshSourceData data = ReadMesh(source);
            if (PieceCount > MaximumPieceCount)
                throw new InvalidOperationException("Shield fragment count exceeds the approved maximum of 20.");
            FragmentMeshData[] fragmentData = BuildIrregularFragments(data, source.bounds,
                out Vector2[] fragmentSeeds);
            float[] projectedAreas = fragmentData.Select(fragment => fragment.ProjectedArea()).ToArray();
            float totalProjectedArea = projectedAreas.Sum();
            float minimumProjectedArea = projectedAreas.Min();
            float maximumProjectedArea = projectedAreas.Max();
            float meanProjectedArea = totalProjectedArea / PieceCount;
            float areaDeviation = Mathf.Sqrt(projectedAreas.Sum(area =>
                (area - meanProjectedArea) * (area - meanProjectedArea)) / PieceCount);
            float areaVariation = meanProjectedArea <= 0f ? 0f : areaDeviation / meanProjectedArea;
            if (minimumProjectedArea <= totalProjectedArea * 0.01f ||
                maximumProjectedArea / minimumProjectedArea < 1.8f || areaVariation < 0.20f)
                throw new InvalidOperationException("Random Shield fragments are still too uniform or contain a sliver.");
            Vector3[] centers = fragmentData.Select(fragment => fragment.Bounds.center).ToArray();
            var fragments = new List<GameObject>();
            var fragmentMeshes = new List<Mesh>();
            ApprovedMotionData approvedMotion = BuildApprovedMotionData();
            Vector3[] directions = approvedMotion.Directions;
            Vector3[] rotationAxes = approvedMotion.RotationAxes;
            float[] rotationDegrees = approvedMotion.RotationDegrees;
            float[] distanceScales = approvedMotion.DistanceScales;
            Vector3 transporterRight = Vector3.right;
            Vector3 shieldExteriorForward = Vector3.down;
            Vector3 vertical = Vector3.forward;

            GameObject sampleRoot = new GameObject("ShieldBreakArtSample_Root")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject cameraObject = null;
            GameObject keyLightObject = null;
            GameObject fillLightObject = null;
            RenderTexture render = null;
            Texture2D pixels = null;
            Color previousAmbient = RenderSettings.ambientLight;
            AmbientMode previousAmbientMode = RenderSettings.ambientMode;
            try
            {
                for (int piece = 0; piece < PieceCount; piece++)
                {
                    Mesh fragmentMesh = BuildFragmentMesh(fragmentData[piece], piece, source.indexFormat);
                    fragmentMeshes.Add(fragmentMesh);
                    GameObject pivot = new GameObject("ShieldFragment_" + (piece + 1).ToString("D2"));
                    pivot.hideFlags = HideFlags.HideAndDontSave;
                    pivot.transform.SetParent(sampleRoot.transform, false);
                    pivot.transform.localPosition = centers[piece];
                    GameObject geometry = new GameObject("ApprovedShieldGeometry");
                    geometry.hideFlags = HideFlags.HideAndDontSave;
                    geometry.transform.SetParent(pivot.transform, false);
                    geometry.transform.localPosition = -centers[piece];
                    geometry.AddComponent<MeshFilter>().sharedMesh = fragmentMesh;
                    geometry.AddComponent<MeshRenderer>().sharedMaterials =
                        new[] { bodyMaterial, windowMaterial };
                    fragments.Add(pivot);
                }

                Bounds bounds = source.bounds;
                float extent = Mathf.Max(bounds.size.x, bounds.size.z);
                cameraObject = new GameObject("ShieldBreakArtSample_Camera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.cameraType = CameraType.Game;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.035f, 0.05f, 1f);
                camera.orthographic = true;
                camera.aspect = RenderWidth / (float)RenderHeight;
                camera.orthographicSize = Mathf.Max(bounds.size.z * 1.55f, bounds.size.x * 2.15f);
                camera.nearClipPlane = Mathf.Max(0.00001f, extent * 0.01f);
                camera.farClipPlane = Mathf.Max(0.1f, extent * 20f);
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.transform.position = bounds.center + shieldExteriorForward * Mathf.Max(extent * 5f, 0.05f);
                camera.transform.LookAt(bounds.center + transporterRight * extent * 0.34f - vertical * extent * 0.12f,
                    vertical);

                keyLightObject = CreateDirectionalLight("ShieldBreakArtSample_Key",
                    new Color(0.88f, 0.95f, 1f), 2.1f,
                    Quaternion.LookRotation(-shieldExteriorForward - vertical * 0.45f + transporterRight * 0.2f));
                fillLightObject = CreateDirectionalLight("ShieldBreakArtSample_Fill",
                    new Color(0.30f, 0.58f, 0.88f), 0.85f,
                    Quaternion.LookRotation(shieldExteriorForward - transporterRight * 0.7f));
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.20f, 0.24f, 0.30f);
                render = RenderTexture.GetTemporary(RenderWidth, RenderHeight, 24, RenderTextureFormat.ARGB32);
                pixels = new Texture2D(RenderWidth, RenderHeight, TextureFormat.RGB24, false);
                camera.targetTexture = render;
                camera.Render();
                camera.Render();
                int frameCount = Mathf.CeilToInt(breakClip.length * FramesPerSecond) + 1;
                for (int frame = 0; frame < frameCount; frame++)
                {
                    float time = frame == frameCount - 1 ? breakClip.length : frame / (float)FramesPerSecond;
                    float progress = Mathf.Clamp01(time / breakClip.length);
                    float eased = progress * progress * (3f - 2f * progress);
                    camera.orthographicSize = Mathf.Lerp(bounds.size.z * 0.72f,
                        Mathf.Max(bounds.size.z * 1.55f, bounds.size.x * 2.15f), eased);
                    for (int piece = 0; piece < PieceCount; piece++)
                    {
                        float travel = extent * 1.9f * distanceScales[piece] * eased;
                        Vector3 gravity = -vertical * extent * (0.18f + 0.62f * progress) * progress;
                        fragments[piece].transform.localPosition = centers[piece] + directions[piece] * travel + gravity;
                        fragments[piece].transform.localRotation = Quaternion.AngleAxis(
                            rotationDegrees[piece] * eased, rotationAxes[piece]);
                    }
                    RenderFrame(camera, render, pixels,
                        Path.Combine(absoluteFrames, "frame_" + frame.ToString("D4") + ".png"));
                }

                var manifest = new StringBuilder();
                manifest.AppendLine("Shield break art sample generated only from the currently approved Shield mesh and materials.");
                manifest.AppendLine("mesh=" + ShieldMeshPath + " sha256=" + Sha256(Absolute(ShieldMeshPath)));
                manifest.AppendLine("bodyMaterial=" + ShieldBodyMaterialPath + " sha256=" + Sha256(Absolute(ShieldBodyMaterialPath)));
                manifest.AppendLine("windowMaterial=" + ShieldWindowMaterialPath + " sha256=" + Sha256(Absolute(ShieldWindowMaterialPath)));
                manifest.AppendLine("breakAnimation=" + BreakAnimationPath + " sha256=" + Sha256(Absolute(BreakAnimationPath)));
                manifest.AppendLine("durationSeconds=" + breakClip.length.ToString("F6", CultureInfo.InvariantCulture));
                manifest.AppendLine("framesPerSecond=" + FramesPerSecond + " frameCount=" + frameCount);
                manifest.AppendLine("pieceCount=" + PieceCount + " approvedMaximumPieceCount=" + MaximumPieceCount +
                    " motionRandomSeed=" + RandomSeed + " fragmentShapeSeed=" + FragmentShapeSeed);
                manifest.AppendLine("fragmentShape=random-voronoi projectedAreaVariation=" +
                    areaVariation.ToString("F6", CultureInfo.InvariantCulture) +
                    " minimumAreaFraction=" + (minimumProjectedArea / totalProjectedArea).ToString("F6", CultureInfo.InvariantCulture) +
                    " maximumToMinimumAreaRatio=" + (maximumProjectedArea / minimumProjectedArea).ToString("F6", CultureInfo.InvariantCulture));
                manifest.AppendLine("overallDirection=transporter-right-rear");
                manifest.AppendLine("bodyAndWindowBothPartitioned=True runtimeIntegrated=False imageGenerationUsed=False");
                for (int piece = 0; piece < PieceCount; piece++)
                    manifest.AppendLine("piece" + (piece + 1).ToString("D2") +
                        " seed=" + Format(fragmentSeeds[piece]) +
                        " projectedArea=" + projectedAreas[piece].ToString("F8", CultureInfo.InvariantCulture) +
                        " vertexCount=" + fragmentData[piece].Vertices.Count +
                        " triangleCount=" + fragmentData[piece].TriangleCount +
                        " direction=" + Format(directions[piece]) +
                        " rotationAxis=" + Format(rotationAxes[piece]) +
                        " rotationDegrees=" + rotationDegrees[piece].ToString("F3", CultureInfo.InvariantCulture));
                File.WriteAllText(Path.Combine(absoluteOutput, "sample_manifest.txt"), manifest.ToString(), Encoding.UTF8);
                Debug.Log("Shield break art sample frame pass complete. frames=" + frameCount +
                    " duration=" + breakClip.length.ToString("F6", CultureInfo.InvariantCulture));
            }
            finally
            {
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientLight = previousAmbient;
                if (cameraObject != null)
                {
                    Camera camera = cameraObject.GetComponent<Camera>();
                    if (camera != null) camera.targetTexture = null;
                }
                if (render != null) RenderTexture.ReleaseTemporary(render);
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (keyLightObject != null) UnityEngine.Object.DestroyImmediate(keyLightObject);
                if (fillLightObject != null) UnityEngine.Object.DestroyImmediate(fillLightObject);
                if (sampleRoot != null) UnityEngine.Object.DestroyImmediate(sampleRoot);
                foreach (Mesh mesh in fragmentMeshes)
                    if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        public static void InspectApprovedRuntime()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("StartShieldAnimationReviewLoop must be active before runtime inspection.");
            VerifyApprovedSourceIntegrity();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            ShieldBreakReactionFracture runtime = target.GetComponent<ShieldBreakReactionFracture>() ??
                throw new InvalidOperationException("Approved Shield fracture runtime component is missing.");
            VerifyRuntimeConfiguration(target, runtime);
            if (inspectingRuntime) EditorApplication.update -= InspectRuntimeTick;
            runtimeInspectionStartedAt = EditorApplication.timeSinceStartup;
            runtimeInspectionStartDetachCount = runtime.DetachmentCount;
            runtimeInspectionStartResetCount = runtime.ResetCount;
            runtimeInspectionStartHideCount = runtime.HideCount;
            runtimeInspectionLastRecordedDetachCount = runtime.DetachmentCount;
            runtimeInspectionLastRecordedHideCount = runtime.HideCount;
            observedIntactBeforeDelay = false;
            observedDetachedAfterDelay = false;
            observedDetachedWorldAnchor = false;
            observedFractureEnd = false;
            observedLoopReset = false;
            observedFragmentsVisibleBeforeHide = false;
            observedFragmentsHiddenAfterOneSecond = false;
            observedFragmentsHiddenUntilReset = false;
            maximumLivePoseError = 0f;
            maximumDetachedAnchorPositionError = 0f;
            maximumDetachedAnchorRotationError = 0f;
            maximumRuntimeScaleError = 0f;
            minimumDetachCycleTime = float.PositiveInfinity;
            maximumDetachCycleTime = float.NegativeInfinity;
            maximumPreDetachCycleTime = float.NegativeInfinity;
            allDetachesCrossedThresholdOnFirstFrame = true;
            minimumHideCycleTime = float.PositiveInfinity;
            maximumHideCycleTime = float.NegativeInfinity;
            maximumPreHideCycleTime = float.NegativeInfinity;
            allHidesCrossedThresholdOnFirstFrame = true;
            observedEndTravel = false;
            capturedEndTravelStart = false;
            endTravelStartPosition = Vector3.zero;
            observedEndTravelDistance = 0f;
            Directory.CreateDirectory(Absolute(RuntimeValidationFolder));
            string incompletePath = Absolute(RuntimeValidationFolder + "/runtime_metrics_incomplete.txt");
            if (File.Exists(incompletePath)) File.Delete(incompletePath);
            inspectingRuntime = true;
            EditorApplication.update += InspectRuntimeTick;
            Debug.Log("Approved Shield break runtime observation started without target manipulation.");
        }

        public static void CaptureApprovedRuntime(string outputPath)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("StartShieldAnimationReviewLoop must be active before final capture.");
            string metricsPath = Absolute(RuntimeValidationFolder + "/runtime_metrics.txt");
            if (!File.Exists(metricsPath) ||
                File.ReadAllText(metricsPath, Encoding.UTF8).IndexOf("overallPassed=True", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Approved runtime metrics must pass before the one final capture.");
            if (capturingRuntime)
                throw new InvalidOperationException("Approved Shield break final capture is already running.");
            runtimeCaptureOutputPath = Path.IsPathRooted(outputPath) ? outputPath : Absolute(outputPath);
            if (File.Exists(runtimeCaptureOutputPath))
                throw new InvalidOperationException("The approved one-time final capture already exists: " +
                    runtimeCaptureOutputPath);
            string outputDirectory = Path.GetDirectoryName(runtimeCaptureOutputPath);
            if (string.IsNullOrEmpty(outputDirectory))
                throw new InvalidOperationException("Final capture output directory is invalid.");
            Directory.CreateDirectory(outputDirectory);
            runtimeCaptureFrameFolder = Path.Combine(outputDirectory, "capture_frames");
            Directory.CreateDirectory(runtimeCaptureFrameFolder);
            if (Directory.GetFiles(runtimeCaptureFrameFolder, "runtime_*.png").Length > 0)
                throw new InvalidOperationException("Runtime capture frame folder is not empty; refusing a repeated capture.");

            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            ShieldBreakReactionFracture runtime = target.GetComponent<ShieldBreakReactionFracture>() ??
                throw new InvalidOperationException("Approved Shield fracture runtime component is missing.");
            VerifyRuntimeConfiguration(target, runtime);
            ShieldStateMotion motion = target.GetComponent<ShieldStateMotion>() ??
                throw new InvalidOperationException("Shield_Break_Reaction state motion is missing.");
            captureCycleCount = motion.CompletedCycleCount;
            capturePhase = -1;
            capturingRuntime = true;
            EditorApplication.update += CaptureRuntimeTick;
            Debug.Log("Approved Shield break one-time final capture is waiting for the next unmodified loop.");
        }

        private static void InspectRuntimeTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Play Mode ended during approved Shield runtime observation.");
                Scene scene = RequireScene();
                GameObject target = FindUnique(scene, TargetName);
                ShieldBreakReactionFracture runtime = target.GetComponent<ShieldBreakReactionFracture>() ??
                    throw new InvalidOperationException("Approved Shield fracture runtime component disappeared.");
                float cycleTime = runtime.CurrentCycleTimeSeconds;
                if (cycleTime <= ApprovedStartDelaySeconds * 0.75f &&
                    runtime.IntactShieldVisible && !runtime.DetachedRootVisible && !runtime.IsDetached)
                    observedIntactBeforeDelay = true;
                if (cycleTime >= ApprovedStartDelaySeconds && runtime.IsDetached)
                {
                    float hideCycleTime = ApprovedStartDelaySeconds + ApprovedFragmentVisibleDurationSeconds;
                    if (cycleTime < hideCycleTime)
                    {
                        observedDetachedAfterDelay |= !runtime.IntactShieldVisible &&
                            runtime.DetachedRootVisible && !runtime.FragmentsHidden;
                        observedFragmentsVisibleBeforeHide |= cycleTime >= hideCycleTime - 0.15f &&
                            runtime.DetachedRootVisible && !runtime.FragmentsHidden;
                    }
                    else
                    {
                        observedFragmentsHiddenAfterOneSecond |= !runtime.IntactShieldVisible &&
                            !runtime.DetachedRootVisible && runtime.FragmentsHidden;
                        observedFragmentsHiddenUntilReset |= cycleTime >=
                            ApprovedCycleDurationSeconds - 0.15f && !runtime.DetachedRootVisible &&
                            runtime.FragmentsHidden;
                    }
                    observedDetachedWorldAnchor |= runtime.DetachedRootHasNoParent;
                    maximumLivePoseError = Mathf.Max(maximumLivePoseError,
                        runtime.CalculateMaximumApprovedPoseError());
                    maximumDetachedAnchorPositionError = Mathf.Max(maximumDetachedAnchorPositionError,
                        runtime.DetachedAnchorPositionError);
                    maximumDetachedAnchorRotationError = Mathf.Max(maximumDetachedAnchorRotationError,
                        runtime.DetachedAnchorRotationErrorDegrees);
                    maximumRuntimeScaleError = Mathf.Max(maximumRuntimeScaleError,
                        runtime.LastMaximumFragmentScaleError);
                    if (runtime.DetachmentCount > runtimeInspectionLastRecordedDetachCount)
                    {
                        runtimeInspectionLastRecordedDetachCount = runtime.DetachmentCount;
                        minimumDetachCycleTime = Mathf.Min(minimumDetachCycleTime,
                            runtime.LastDetachCycleTimeSeconds);
                        maximumDetachCycleTime = Mathf.Max(maximumDetachCycleTime,
                            runtime.LastDetachCycleTimeSeconds);
                        maximumPreDetachCycleTime = Mathf.Max(maximumPreDetachCycleTime,
                            runtime.LastPreDetachCycleTimeSeconds);
                        allDetachesCrossedThresholdOnFirstFrame &=
                            runtime.LastPreDetachCycleTimeSeconds < ApprovedStartDelaySeconds &&
                            runtime.LastDetachCycleTimeSeconds >= ApprovedStartDelaySeconds;
                    }
                    if (runtime.HideCount > runtimeInspectionLastRecordedHideCount)
                    {
                        runtimeInspectionLastRecordedHideCount = runtime.HideCount;
                        minimumHideCycleTime = Mathf.Min(minimumHideCycleTime,
                            runtime.LastHideCycleTimeSeconds);
                        maximumHideCycleTime = Mathf.Max(maximumHideCycleTime,
                            runtime.LastHideCycleTimeSeconds);
                        maximumPreHideCycleTime = Mathf.Max(maximumPreHideCycleTime,
                            runtime.LastPreHideCycleTimeSeconds);
                        allHidesCrossedThresholdOnFirstFrame &=
                            runtime.LastPreHideCycleTimeSeconds < hideCycleTime &&
                            runtime.LastHideCycleTimeSeconds >= hideCycleTime;
                    }
                    float progress = runtime.CurrentFractureProgress;
                    if (!capturedEndTravelStart && progress >= 0.82f && progress <= 0.90f)
                    {
                        endTravelStartPosition = runtime.FragmentPivots[0].localPosition;
                        capturedEndTravelStart = true;
                    }
                    if (capturedEndTravelStart && progress >= 0.98f)
                    {
                        observedEndTravelDistance = Mathf.Max(observedEndTravelDistance,
                            Vector3.Distance(endTravelStartPosition,
                                runtime.FragmentPivots[0].localPosition));
                        observedEndTravel |= observedEndTravelDistance > 0.00001f;
                        observedFractureEnd = true;
                    }
                }
                observedLoopReset |= runtime.ResetCount > runtimeInspectionStartResetCount &&
                    cycleTime < ApprovedStartDelaySeconds && runtime.IntactShieldVisible &&
                    !runtime.DetachedRootVisible && !runtime.IsDetached;

                if (EditorApplication.timeSinceStartup - runtimeInspectionStartedAt < 6.5d) return;
                EditorApplication.update -= InspectRuntimeTick;
                inspectingRuntime = false;
                int detachments = runtime.DetachmentCount - runtimeInspectionStartDetachCount;
                int resets = runtime.ResetCount - runtimeInspectionStartResetCount;
                int hides = runtime.HideCount - runtimeInspectionStartHideCount;
                bool detachTimingPassed = detachments >= 2 &&
                    allDetachesCrossedThresholdOnFirstFrame;
                bool motionPassed = maximumLivePoseError <= 0.0001f &&
                    maximumDetachedAnchorPositionError <= 0.00001f &&
                    maximumDetachedAnchorRotationError <= 0.001f &&
                    Mathf.Abs(runtime.LinearSpeedMultiplier - ApprovedLinearSpeedMultiplier) <= 0.000001f &&
                    runtime.LastMinimumFragmentSpeedLocalUnitsPerSecond > 0f && observedEndTravel;
                bool scalePassed = maximumRuntimeScaleError <= 0.00001f;
                bool loopPassed = detachments >= 2 && resets >= 2 && observedLoopReset;
                bool hideTimingPassed = hides >= 2 && allHidesCrossedThresholdOnFirstFrame &&
                    observedFragmentsVisibleBeforeHide && observedFragmentsHiddenAfterOneSecond &&
                    observedFragmentsHiddenUntilReset;
                bool overallPassed = observedIntactBeforeDelay && observedDetachedAfterDelay &&
                    observedDetachedWorldAnchor && observedFractureEnd && detachTimingPassed &&
                    motionPassed && scalePassed && hideTimingPassed && loopPassed;
                var report = new StringBuilder();
                report.AppendLine("Read-only live Shield_Break_Reaction fracture observation.");
                report.AppendLine("fragmentCount=" + runtime.FragmentCount + " approvedMaximum=" + MaximumPieceCount);
                report.AppendLine("startDelaySeconds=" + runtime.StartDelaySeconds.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("fractureDurationSeconds=" + runtime.FractureDurationSeconds.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("cycleDurationSeconds=" + runtime.CycleDurationSeconds.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("detachmentsObserved=" + detachments + " hidesObserved=" + hides +
                    " resetsObserved=" + resets);
                report.AppendLine("detachCycleTimeRangeSeconds=" +
                    minimumDetachCycleTime.ToString("F6", CultureInfo.InvariantCulture) + ".." +
                    maximumDetachCycleTime.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("maximumPreDetachCycleTimeSeconds=" +
                    maximumPreDetachCycleTime.ToString("F6", CultureInfo.InvariantCulture) +
                    " firstFrameAfterThresholdPassed=" + allDetachesCrossedThresholdOnFirstFrame);
                report.AppendLine("fragmentVisibleDurationSeconds=" +
                    runtime.FragmentVisibleDurationSeconds.ToString("F6", CultureInfo.InvariantCulture) +
                    " hideCycleTimeRangeSeconds=" +
                    minimumHideCycleTime.ToString("F6", CultureInfo.InvariantCulture) + ".." +
                    maximumHideCycleTime.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("maximumPreHideCycleTimeSeconds=" +
                    maximumPreHideCycleTime.ToString("F6", CultureInfo.InvariantCulture) +
                    " firstFrameAfterHideThresholdPassed=" + allHidesCrossedThresholdOnFirstFrame);
                report.AppendLine("fragmentsVisibleBeforeHideObserved=" + observedFragmentsVisibleBeforeHide +
                    " fragmentsHiddenAfterOneSecondObserved=" + observedFragmentsHiddenAfterOneSecond +
                    " fragmentsHiddenUntilResetObserved=" + observedFragmentsHiddenUntilReset);
                report.AppendLine("intactBeforeDelayObserved=" + observedIntactBeforeDelay);
                report.AppendLine("detachedAfterDelayObserved=" + observedDetachedAfterDelay);
                report.AppendLine("detachedWorldAnchorObserved=" + observedDetachedWorldAnchor);
                report.AppendLine("fractureEndObserved=" + observedFractureEnd);
                report.AppendLine("loopResetToIntactObserved=" + observedLoopReset);
                report.AppendLine("linearSpeedMultiplier=" +
                    runtime.LinearSpeedMultiplier.ToString("F6", CultureInfo.InvariantCulture));
                report.AppendLine("minimumFragmentLinearSpeedLocalUnitsPerSecond=" +
                    runtime.LastMinimumFragmentSpeedLocalUnitsPerSecond.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("maximumFragmentLinearSpeedLocalUnitsPerSecond=" +
                    runtime.LastMaximumFragmentSpeedLocalUnitsPerSecond.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("endSegmentTravelObserved=" + observedEndTravel +
                    " firstFragmentTravelFrom82To98Percent=" +
                    observedEndTravelDistance.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("maximumApprovedPoseError=" +
                    maximumLivePoseError.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("maximumDetachedAnchorPositionErrorMeters=" +
                    maximumDetachedAnchorPositionError.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("maximumDetachedAnchorRotationErrorDegrees=" +
                    maximumDetachedAnchorRotationError.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("maximumFragmentScaleError=" +
                    maximumRuntimeScaleError.ToString("F8", CultureInfo.InvariantCulture));
                report.AppendLine("exactConfigurationPassed=True detachTimingPassed=" + detachTimingPassed +
                    " linearMotionPassed=" + motionPassed + " scalePreservationPassed=" + scalePassed +
                    " hideTimingPassed=" + hideTimingPassed + " loopPassed=" + loopPassed);
                report.AppendLine("Observation did not sample or change any target transform, renderer, mesh, material or animation.");
                report.AppendLine("overallPassed=" + overallPassed);
                File.WriteAllText(Absolute(RuntimeValidationFolder + "/runtime_metrics.txt"),
                    report.ToString(), Encoding.UTF8);
                if (!overallPassed)
                    throw new InvalidOperationException("Approved Shield break runtime observation failed. See runtime_metrics.txt.");
                Debug.Log("Approved Shield break runtime observation passed.");
            }
            catch (Exception exception)
            {
                EditorApplication.update -= InspectRuntimeTick;
                inspectingRuntime = false;
                File.WriteAllText(Absolute(RuntimeValidationFolder + "/runtime_metrics_incomplete.txt"),
                    exception.ToString(), Encoding.UTF8);
                Debug.LogException(exception);
            }
        }

        private static void CaptureRuntimeTick()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                    throw new InvalidOperationException("Play Mode ended during the approved Shield final capture.");
                Scene scene = RequireScene();
                GameObject target = FindUnique(scene, TargetName);
                ShieldBreakReactionFracture runtime = target.GetComponent<ShieldBreakReactionFracture>() ??
                    throw new InvalidOperationException("Approved Shield fracture runtime component disappeared.");
                ShieldStateMotion motion = target.GetComponent<ShieldStateMotion>() ??
                    throw new InvalidOperationException("Shield_Break_Reaction state motion disappeared.");
                if (capturePhase < 0)
                {
                    if (motion.CompletedCycleCount <= captureCycleCount ||
                        runtime.CurrentCycleTimeSeconds > ApprovedStartDelaySeconds * 0.75f) return;
                    captureCycleCount = motion.CompletedCycleCount;
                    capturePhase = 0;
                }
                if (motion.CompletedCycleCount != captureCycleCount)
                    throw new InvalidOperationException("Final capture missed an approved phase before the loop ended.");
                if (runtime.CurrentCycleTimeSeconds < CapturePhaseTimes[capturePhase]) return;

                string phaseLabel = capturePhase.ToString("D2") + "_" +
                    runtime.CurrentCycleTimeSeconds.ToString("F3", CultureInfo.InvariantCulture).Replace('.', '_');
                RenderRuntimeView(target, runtime, target.transform.forward,
                    Path.Combine(runtimeCaptureFrameFolder, "runtime_front_" + phaseLabel + ".png"));
                RenderRuntimeView(target, runtime, target.transform.right,
                    Path.Combine(runtimeCaptureFrameFolder, "runtime_side_" + phaseLabel + ".png"));
                capturePhase++;
                if (capturePhase < CapturePhaseTimes.Length) return;

                EditorApplication.update -= CaptureRuntimeTick;
                capturingRuntime = false;
                string[] front = Directory.GetFiles(runtimeCaptureFrameFolder, "runtime_front_*.png")
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                string[] side = Directory.GetFiles(runtimeCaptureFrameFolder, "runtime_side_*.png")
                    .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                if (front.Length != CapturePhaseTimes.Length || side.Length != CapturePhaseTimes.Length)
                    throw new InvalidOperationException("Approved Shield final capture phase count is incomplete.");
                ComposeRuntimeCapture(front.Concat(side).ToArray(), CapturePhaseTimes.Length,
                    runtimeCaptureOutputPath);
                var manifest = new StringBuilder();
                manifest.AppendLine("Unmodified live Shield_Break_Reaction playback.");
                manifest.AppendLine("Top row: transporter front. Bottom row: transporter right side.");
                manifest.AppendLine("Requested cycle times: " + string.Join(", ", CapturePhaseTimes.Select(time =>
                    time.ToString("F2", CultureInfo.InvariantCulture))));
                manifest.AppendLine("The first tile is intact before 0.05 seconds; 0.08 through 1.02 seconds show the approved fragments at 10.0x linear speed.");
                manifest.AppendLine("All fragments hide at 1.05 seconds and stay hidden in the 1.10 and 2.75 second tiles until loop reset.");
                manifest.AppendLine("No target pose, transform, renderer state, mesh, rig, material or animation curve was changed for capture.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(runtimeCaptureOutputPath),
                    "capture_manifest.txt"), manifest.ToString(), Encoding.UTF8);
                Debug.Log("Approved Shield break one-time final capture complete: " + runtimeCaptureOutputPath);
            }
            catch (Exception exception)
            {
                EditorApplication.update -= CaptureRuntimeTick;
                capturingRuntime = false;
                string directory = string.IsNullOrEmpty(runtimeCaptureOutputPath)
                    ? Absolute(RuntimeValidationFolder)
                    : Path.GetDirectoryName(runtimeCaptureOutputPath);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "capture_incomplete.txt"),
                    exception.ToString(), Encoding.UTF8);
                Debug.LogException(exception);
            }
        }

        private static void VerifyRuntimeConfiguration(GameObject target,
            ShieldBreakReactionFracture runtime)
        {
            if (runtime.FragmentCount != PieceCount || PieceCount > MaximumPieceCount)
                throw new InvalidOperationException("Runtime fragment count differs from the approved 17-piece sample.");
            if (Mathf.Abs(runtime.StartDelaySeconds - ApprovedStartDelaySeconds) > 0.000001f ||
                Mathf.Abs(runtime.FractureDurationSeconds -
                    (ApprovedCycleDurationSeconds - ApprovedStartDelaySeconds)) > 0.00001f ||
                Mathf.Abs(runtime.CycleDurationSeconds - ApprovedCycleDurationSeconds) > 0.00001f)
                throw new InvalidOperationException("Runtime fracture timing differs from the approved timing.");
            Mesh source = AssetDatabase.LoadAssetAtPath<Mesh>(ShieldMeshPath) ??
                throw new InvalidOperationException("Approved Shield source mesh is missing.");
            Material body = AssetDatabase.LoadAssetAtPath<Material>(ShieldBodyMaterialPath) ??
                throw new InvalidOperationException("Approved Shield body material is missing.");
            Material window = AssetDatabase.LoadAssetAtPath<Material>(ShieldWindowMaterialPath) ??
                throw new InvalidOperationException("Approved Shield window material is missing.");
            FragmentMeshData[] expectedFragments = BuildIrregularFragments(ReadMesh(source), source.bounds, out _);
            Vector3[] expectedCenters = expectedFragments.Select(item => item.Bounds.center).ToArray();
            ApprovedMotionData expectedMotion = BuildApprovedMotionData();
            SerializedObject serialized = new SerializedObject(runtime);
            RequireVector3Array(serialized.FindProperty("fragmentCenters"), expectedCenters,
                "fragment centers");
            RequireVector3Array(serialized.FindProperty("directions"), expectedMotion.Directions,
                "fragment directions");
            RequireVector3Array(serialized.FindProperty("rotationAxes"), expectedMotion.RotationAxes,
                "fragment rotation axes");
            RequireFloatArray(serialized.FindProperty("rotationDegrees"), expectedMotion.RotationDegrees,
                "fragment rotation degrees");
            RequireFloatArray(serialized.FindProperty("distanceScales"), expectedMotion.DistanceScales,
                "fragment distance scales");
            RequireFloatValue(serialized.FindProperty("travelMultiplier"), ApprovedTravelMultiplier,
                "travel multiplier");
            RequireFloatValue(serialized.FindProperty("gravityBaseMultiplier"), ApprovedGravityBaseMultiplier,
                "gravity base multiplier");
            RequireFloatValue(serialized.FindProperty("gravityProgressMultiplier"),
                ApprovedGravityProgressMultiplier, "gravity progress multiplier");
            RequireFloatValue(serialized.FindProperty("linearSpeedMultiplier"),
                ApprovedLinearSpeedMultiplier, "linear speed multiplier");
            RequireFloatValue(serialized.FindProperty("fragmentVisibleDurationSeconds"),
                ApprovedFragmentVisibleDurationSeconds, "fragment visible duration");

            Transform[] pivots = runtime.FragmentPivots;
            if (pivots.Length != PieceCount)
                throw new InvalidOperationException("Runtime fragment pivot array is incomplete.");
            for (int index = 0; index < PieceCount; index++)
            {
                Transform pivot = pivots[index];
                if (pivot == null || pivot.name != "ShieldFragment_" + (index + 1).ToString("D2"))
                    throw new InvalidOperationException("Runtime fragment pivot order differs at " + index + ".");
                MeshFilter[] filters = pivot.GetComponentsInChildren<MeshFilter>(true)
                    .Where(item => item.sharedMesh != null).ToArray();
                MeshRenderer[] renderers = pivot.GetComponentsInChildren<MeshRenderer>(true);
                if (filters.Length != 1 || renderers.Length != 1)
                    throw new InvalidOperationException("Runtime fragment geometry hierarchy differs at " + index + ".");
                string expectedPath = RuntimeFragmentFolder + "/ShieldBreakFragment_" +
                    (index + 1).ToString("D2") + ".asset";
                if (AssetDatabase.GetAssetPath(filters[0].sharedMesh) != expectedPath)
                    throw new InvalidOperationException("Runtime fragment mesh asset path differs at " + index + ".");
                VerifyFragmentMesh(filters[0].sharedMesh, expectedFragments[index], index);
                Material[] materials = renderers[0].sharedMaterials;
                if (materials.Length != 2 || materials[0] != body || materials[1] != window)
                    throw new InvalidOperationException("Runtime fragment materials differ from the approved pair at " + index + ".");
            }

            Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
            Transform shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
            MeshFilter sourceFilter = shield.GetComponentsInChildren<MeshFilter>(true)
                .Single(item => item.sharedMesh != null);
            if (sourceFilter.sharedMesh != source)
                throw new InvalidOperationException("The intact runtime Shield no longer uses the approved source mesh.");
            VerifyApprovedSourceIntegrity();
        }

        private static void RequireVector3Array(SerializedProperty property, Vector3[] expected, string label)
        {
            if (property == null || !property.isArray || property.arraySize != expected.Length)
                throw new InvalidOperationException("Approved " + label + " array length differs.");
            for (int index = 0; index < expected.Length; index++)
                if (Vector3.Distance(property.GetArrayElementAtIndex(index).vector3Value, expected[index]) > 0.0000001f)
                    throw new InvalidOperationException("Approved " + label + " differ at " + index + ".");
        }

        private static void RequireFloatArray(SerializedProperty property, float[] expected, string label)
        {
            if (property == null || !property.isArray || property.arraySize != expected.Length)
                throw new InvalidOperationException("Approved " + label + " array length differs.");
            for (int index = 0; index < expected.Length; index++)
                if (Mathf.Abs(property.GetArrayElementAtIndex(index).floatValue - expected[index]) > 0.0000001f)
                    throw new InvalidOperationException("Approved " + label + " differ at " + index + ".");
        }

        private static void RequireFloatValue(SerializedProperty property, float expected, string label)
        {
            if (property == null || Mathf.Abs(property.floatValue - expected) > 0.0000001f)
                throw new InvalidOperationException("Approved " + label + " differs.");
        }

        private static void VerifyFragmentMesh(Mesh actual, FragmentMeshData expected, int index)
        {
            Vector3[] vertices = actual.vertices;
            Vector3[] normals = actual.normals;
            Vector4[] tangents = actual.tangents;
            Vector2[] uv = actual.uv;
            if (vertices.Length != expected.Vertices.Count || normals.Length != expected.Normals.Count ||
                tangents.Length != expected.Tangents.Count || uv.Length != expected.Uv.Count ||
                actual.subMeshCount != expected.Triangles.Length)
                throw new InvalidOperationException("Approved fragment mesh channel count differs at " + index + ".");
            for (int vertex = 0; vertex < vertices.Length; vertex++)
            {
                if (Vector3.Distance(vertices[vertex], expected.Vertices[vertex]) > 0.0000001f ||
                    Vector3.Distance(normals[vertex], expected.Normals[vertex]) > 0.0000001f ||
                    Vector4.Distance(tangents[vertex], expected.Tangents[vertex]) > 0.0000001f ||
                    Vector2.Distance(uv[vertex], expected.Uv[vertex]) > 0.0000001f)
                    throw new InvalidOperationException("Approved fragment mesh vertex data differs at piece " +
                        index + " vertex " + vertex + ".");
            }
            for (int submesh = 0; submesh < actual.subMeshCount; submesh++)
                if (!actual.GetTriangles(submesh).SequenceEqual(expected.Triangles[submesh]))
                    throw new InvalidOperationException("Approved fragment triangles differ at piece " +
                        index + " submesh " + submesh + ".");
        }

        private static ApprovedMotionData BuildApprovedMotionData()
        {
            var result = new ApprovedMotionData(PieceCount);
            var random = new System.Random(RandomSeed);
            Vector3 transporterRight = Vector3.right;
            Vector3 shieldExteriorForward = Vector3.down;
            Vector3 transporterRear = -shieldExteriorForward;
            Vector3 overallDirection = (transporterRight * 0.78f + transporterRear * 0.62f).normalized;
            for (int piece = 0; piece < PieceCount; piece++)
            {
                Vector3 randomVector = new Vector3(RandomRange(random, -0.48f, 0.48f),
                    RandomRange(random, -0.30f, 0.42f), RandomRange(random, -0.62f, 0.72f));
                Vector3 direction = (overallDirection * 1.35f + randomVector).normalized;
                if (Vector3.Dot(direction, overallDirection) < 0.55f)
                    direction = (direction + overallDirection).normalized;
                result.Directions[piece] = direction;
                result.RotationAxes[piece] = new Vector3(RandomRange(random, -1f, 1f),
                    RandomRange(random, -1f, 1f), RandomRange(random, -1f, 1f)).normalized;
                if (result.RotationAxes[piece].sqrMagnitude < 0.000001f)
                    result.RotationAxes[piece] = Vector3.up;
                result.RotationDegrees[piece] = RandomRange(random, 85f, 245f) *
                    (random.NextDouble() < 0.5d ? -1f : 1f);
                result.DistanceScales[piece] = RandomRange(random, 0.72f, 1.26f);
            }
            return result;
        }

        private static void VerifyApprovedSourceIntegrity()
        {
            RequireSha(ShieldMeshPath, ApprovedShieldMeshSha);
            RequireSha(ShieldBodyMaterialPath, ApprovedBodyMaterialSha);
            RequireSha(ShieldWindowMaterialPath, ApprovedWindowMaterialSha);
            RequireSha(BreakAnimationPath, ApprovedBreakAnimationSha);
            RequireSha(OutputFolder + "/source_approved_65.png", ApprovedSourceImageSha);
            RequireSha(OutputFolder + "/shield_break_preview.mp4", ApprovedPreviewSha);
            RequireSha(OutputFolder + "/comparison.png", ApprovedComparisonSha);
            RequireSha(OutputFolder + "/sample_manifest.txt", ApprovedManifestSha);
        }

        private static void RequireSha(string path, string expected)
        {
            string absolutePath = Absolute(path);
            if (!File.Exists(absolutePath))
                throw new InvalidOperationException("Approved Shield source is missing: " + path);
            string actual = Sha256(absolutePath);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("Approved Shield source hash changed: " + path +
                    " expected=" + expected + " actual=" + actual);
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must already be active. Active=" + scene.path);
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string objectName)
        {
            GameObject[] matches = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(item => item.scene == scene && item.name == objectName).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one scene object named " + objectName +
                    ". Found=" + matches.Length);
            return matches[0];
        }

        private static Transform RequireNamedTransform(Transform root, string objectName)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == objectName).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + objectName +
                    ". Found=" + matches.Length);
            return matches[0];
        }

        private static string[] OtherRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects().Where(item => item.name != TargetName)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(HierarchySignature).ToArray();
        }

        private static string HierarchySignature(GameObject root)
        {
            var builder = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(item, root.transform),
                             StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root.transform);
                builder.Append(path).Append('|').Append(item.gameObject.activeSelf).Append('|');
                foreach (Component component in item.GetComponents<Component>())
                    if (component != null) builder.Append(component.GetType().FullName).Append(',');
                builder.Append('\n');
            }
            using (var hash = SHA256.Create())
                return root.name + "|" + BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(builder.ToString()))).Replace("-", string.Empty);
        }

        private static void EnsureAssetFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void RenderRuntimeView(GameObject target, ShieldBreakReactionFracture runtime,
            Vector3 viewDirection, string outputPath)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy).ToList();
            Transform detached = runtime.DetachedFragmentRoot;
            if (detached != null && !detached.IsChildOf(target.transform))
                renderers.AddRange(detached.GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.enabled && item.gameObject.activeInHierarchy));
            renderers = renderers.Distinct().ToList();
            if (renderers.Count == 0)
                throw new InvalidOperationException("Shield_Break_Reaction has no visible renderers for capture.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Count; index++) bounds.Encapsulate(renderers[index].bounds);
            Vector3 center = bounds.center;
            float aspect = RuntimeCaptureWidth / (float)RuntimeCaptureHeight;
            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            float orthographicSize = Mathf.Max(bounds.size.y * 0.58f,
                horizontalSize / (2f * aspect) * 1.12f, 1.15f);
            float distance = Mathf.Max(5f, bounds.size.magnitude * 2f);
            var cameraObject = new GameObject("ApprovedShieldBreakRuntimeCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera camera = cameraObject.AddComponent<Camera>();
            RenderTexture render = RenderTexture.GetTemporary(RuntimeCaptureWidth, RuntimeCaptureHeight,
                24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(RuntimeCaptureWidth, RuntimeCaptureHeight, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.cameraType = CameraType.Game;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.035f, 0.05f, 1f);
                camera.orthographic = true;
                camera.aspect = aspect;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance * 4f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.transform.position = center + viewDirection.normalized * distance;
                camera.transform.LookAt(center, target.transform.up);
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, RuntimeCaptureWidth, RuntimeCaptureHeight), 0, 0);
                pixels.Apply(false, false);
                File.WriteAllBytes(outputPath, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void ComposeRuntimeCapture(string[] paths, int columns, string outputPath)
        {
            Texture2D[] textures = paths.Select(path =>
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!texture.LoadImage(File.ReadAllBytes(path)))
                    throw new InvalidOperationException("Runtime capture tile could not be loaded: " + path);
                return texture;
            }).ToArray();
            int rows = Mathf.CeilToInt(textures.Length / (float)columns);
            var sheet = new Texture2D(columns * RuntimeCaptureWidth,
                rows * RuntimeCaptureHeight, TextureFormat.RGB24, false);
            sheet.SetPixels(Enumerable.Repeat(new Color(0.025f, 0.035f, 0.05f, 1f),
                sheet.width * sheet.height).ToArray());
            for (int index = 0; index < textures.Length; index++)
            {
                int column = index % columns;
                int row = index / columns;
                sheet.SetPixels(column * RuntimeCaptureWidth, (rows - row - 1) * RuntimeCaptureHeight,
                    RuntimeCaptureWidth, RuntimeCaptureHeight, textures[index].GetPixels());
            }
            sheet.Apply(false, false);
            File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            foreach (Texture2D texture in textures) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        private static MeshSourceData ReadMesh(Mesh source)
        {
            using (Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(source))
            {
                Mesh.MeshData meshData = dataArray[0];
                var vertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
                meshData.GetVertices(vertices);
                var result = new MeshSourceData { Vertices = vertices.ToArray() };
                vertices.Dispose();
                if (meshData.HasVertexAttribute(VertexAttribute.Normal))
                {
                    var normals = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
                    meshData.GetNormals(normals);
                    result.Normals = normals.ToArray();
                    normals.Dispose();
                }
                if (meshData.HasVertexAttribute(VertexAttribute.Tangent))
                {
                    var tangents = new NativeArray<Vector4>(meshData.vertexCount, Allocator.Temp);
                    meshData.GetTangents(tangents);
                    result.Tangents = tangents.ToArray();
                    tangents.Dispose();
                }
                if (meshData.HasVertexAttribute(VertexAttribute.TexCoord0))
                {
                    var uv = new NativeArray<Vector2>(meshData.vertexCount, Allocator.Temp);
                    meshData.GetUVs(0, uv);
                    result.Uv = uv.ToArray();
                    uv.Dispose();
                }
                result.SubmeshIndices = new int[source.subMeshCount][];
                for (int submesh = 0; submesh < source.subMeshCount; submesh++)
                {
                    int count = meshData.GetSubMesh(submesh).indexCount;
                    var indices = new NativeArray<int>(count, Allocator.Temp);
                    meshData.GetIndices(indices, submesh, true);
                    result.SubmeshIndices[submesh] = indices.ToArray();
                    indices.Dispose();
                }
                return result;
            }
        }

        private static FragmentMeshData[] BuildIrregularFragments(MeshSourceData data, Bounds bounds,
            out Vector2[] seeds)
        {
            seeds = BuildRandomFragmentSeeds(bounds);
            var fragments = Enumerable.Range(0, PieceCount)
                .Select(_ => new FragmentMeshData(data.SubmeshIndices.Length)).ToArray();
            for (int piece = 0; piece < PieceCount; piece++)
            {
                FragmentMeshData fragment = fragments[piece];
                for (int submesh = 0; submesh < data.SubmeshIndices.Length; submesh++)
                {
                    int[] indices = data.SubmeshIndices[submesh];
                    for (int index = 0; index < indices.Length; index += 3)
                    {
                        var polygon = new List<SampleVertex>(3)
                        {
                            Vertex(data, indices[index]),
                            Vertex(data, indices[index + 1]),
                            Vertex(data, indices[index + 2])
                        };
                        for (int other = 0; other < PieceCount && polygon.Count >= 3; other++)
                        {
                            if (other == piece) continue;
                            Vector2 normal = seeds[other] - seeds[piece];
                            float boundary = (seeds[other].sqrMagnitude - seeds[piece].sqrMagnitude) * 0.5f;
                            polygon = ClipToVoronoiHalfPlane(polygon, normal, boundary);
                        }
                        if (polygon.Count < 3) continue;
                        int first = fragment.AddVertex(polygon[0]);
                        for (int vertex = 1; vertex < polygon.Count - 1; vertex++)
                        {
                            fragment.Triangles[submesh].Add(first);
                            fragment.Triangles[submesh].Add(fragment.AddVertex(polygon[vertex]));
                            fragment.Triangles[submesh].Add(fragment.AddVertex(polygon[vertex + 1]));
                        }
                    }
                }
                fragment.CalculateBounds();
                if (fragment.Vertices.Count == 0)
                    throw new InvalidOperationException("Clipped Shield partition produced an empty fragment.");
            }
            return fragments;
        }

        private static Vector2[] BuildRandomFragmentSeeds(Bounds bounds)
        {
            var random = new System.Random(FragmentShapeSeed);
            var result = new List<Vector2>(PieceCount);
            float insetX = bounds.size.x * 0.035f;
            float insetZ = bounds.size.z * 0.035f;
            float minimumSeparation = Mathf.Min(bounds.size.x, bounds.size.z) * 0.10f;
            for (int attempt = 0; result.Count < PieceCount && attempt < 12000; attempt++)
            {
                Vector2 candidate = new Vector2(
                    RandomRange(random, bounds.min.x + insetX, bounds.max.x - insetX),
                    RandomRange(random, bounds.min.z + insetZ, bounds.max.z - insetZ));
                if (result.All(existing => Vector2.Distance(existing, candidate) >= minimumSeparation))
                    result.Add(candidate);
                if (attempt > 4000 && result.Count < PieceCount)
                    minimumSeparation *= 0.9995f;
            }
            if (result.Count != PieceCount)
                throw new InvalidOperationException("Could not place the requested random Shield fracture seeds.");
            return result.ToArray();
        }

        private static SampleVertex Vertex(MeshSourceData data, int index) => new SampleVertex
        {
            Position = data.Vertices[index],
            Normal = data.Normals == null ? Vector3.zero : data.Normals[index],
            Tangent = data.Tangents == null ? Vector4.zero : data.Tangents[index],
            Uv = data.Uv == null ? Vector2.zero : data.Uv[index]
        };

        private static List<SampleVertex> ClipToVoronoiHalfPlane(List<SampleVertex> input,
            Vector2 normal, float boundary)
        {
            if (input.Count == 0) return input;
            var output = new List<SampleVertex>(input.Count + 2);
            SampleVertex previous = input[input.Count - 1];
            float previousValue = PlaneValue(previous.Position, normal);
            bool previousInside = previousValue <= boundary + 0.0000001f;
            foreach (SampleVertex current in input)
            {
                float currentValue = PlaneValue(current.Position, normal);
                bool currentInside = currentValue <= boundary + 0.0000001f;
                if (currentInside != previousInside)
                {
                    float denominator = currentValue - previousValue;
                    float amount = Mathf.Abs(denominator) < 0.0000001f ? 0f :
                        Mathf.Clamp01((boundary - previousValue) / denominator);
                    output.Add(SampleVertex.Lerp(previous, current, amount));
                }
                if (currentInside) output.Add(current);
                previous = current;
                previousValue = currentValue;
                previousInside = currentInside;
            }
            return output;
        }

        private static float PlaneValue(Vector3 value, Vector2 normal) =>
            value.x * normal.x + value.z * normal.y;

        private static string Format(Vector2 value) => string.Format(CultureInfo.InvariantCulture,
            "({0:F5},{1:F5})", value.x, value.y);

        private static Mesh BuildFragmentMesh(FragmentMeshData data, int piece, IndexFormat indexFormat)
        {
            var mesh = new Mesh
            {
                name = "ShieldBreakFragment_" + (piece + 1).ToString("D2"),
                indexFormat = data.Vertices.Count > 65535 ? IndexFormat.UInt32 : indexFormat,
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetTangents(data.Tangents);
            mesh.SetUVs(0, data.Uv);
            mesh.subMeshCount = data.Triangles.Length;
            for (int submesh = 0; submesh < data.Triangles.Length; submesh++)
                mesh.SetTriangles(data.Triangles[submesh], submesh, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject CreateDirectionalLight(string name, Color color, float intensity,
            Quaternion rotation)
        {
            var result = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            Light light = result.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            result.transform.rotation = rotation;
            return result;
        }

        private static void RenderFrame(Camera camera, RenderTexture render, Texture2D pixels, string path)
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                camera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, RenderWidth, RenderHeight), 0, 0);
                pixels.Apply(false, false);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        private static float RandomRange(System.Random random, float minimum, float maximum) =>
            minimum + (float)random.NextDouble() * (maximum - minimum);

        private static string Sha256(string path)
        {
            using (var hash = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Format(Vector3 value) => string.Format(CultureInfo.InvariantCulture,
            "({0:F5},{1:F5},{2:F5})", value.x, value.y, value.z);

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));

        private sealed class MeshSourceData
        {
            internal Vector3[] Vertices;
            internal Vector3[] Normals;
            internal Vector4[] Tangents;
            internal Vector2[] Uv;
            internal int[][] SubmeshIndices;
        }

        private sealed class ApprovedMotionData
        {
            internal readonly Vector3[] Directions;
            internal readonly Vector3[] RotationAxes;
            internal readonly float[] RotationDegrees;
            internal readonly float[] DistanceScales;

            internal ApprovedMotionData(int count)
            {
                Directions = new Vector3[count];
                RotationAxes = new Vector3[count];
                RotationDegrees = new float[count];
                DistanceScales = new float[count];
            }
        }

        private sealed class FragmentMeshData
        {
            internal readonly List<Vector3> Vertices = new List<Vector3>();
            internal readonly List<Vector3> Normals = new List<Vector3>();
            internal readonly List<Vector4> Tangents = new List<Vector4>();
            internal readonly List<Vector2> Uv = new List<Vector2>();
            internal readonly List<int>[] Triangles;
            internal Bounds Bounds;
            internal int TriangleCount => Triangles.Sum(indices => indices.Count / 3);

            internal FragmentMeshData(int submeshCount)
            {
                Triangles = Enumerable.Range(0, submeshCount).Select(_ => new List<int>()).ToArray();
            }

            internal int AddVertex(SampleVertex vertex)
            {
                int index = Vertices.Count;
                Vertices.Add(vertex.Position);
                Normals.Add(vertex.Normal);
                Tangents.Add(vertex.Tangent);
                Uv.Add(vertex.Uv);
                return index;
            }

            internal void CalculateBounds()
            {
                if (Vertices.Count == 0) return;
                Bounds = new Bounds(Vertices[0], Vector3.zero);
                for (int index = 1; index < Vertices.Count; index++) Bounds.Encapsulate(Vertices[index]);
            }

            internal float ProjectedArea()
            {
                float result = 0f;
                foreach (List<int> indices in Triangles)
                for (int index = 0; index < indices.Count; index += 3)
                {
                    Vector3 a = Vertices[indices[index]];
                    Vector3 b = Vertices[indices[index + 1]];
                    Vector3 c = Vertices[indices[index + 2]];
                    Vector2 ab = new Vector2(b.x - a.x, b.z - a.z);
                    Vector2 ac = new Vector2(c.x - a.x, c.z - a.z);
                    result += Mathf.Abs(ab.x * ac.y - ab.y * ac.x) * 0.5f;
                }
                return result;
            }
        }

        private struct SampleVertex
        {
            internal Vector3 Position;
            internal Vector3 Normal;
            internal Vector4 Tangent;
            internal Vector2 Uv;

            internal static SampleVertex Lerp(SampleVertex left, SampleVertex right, float amount)
            {
                Vector3 normal = Vector3.Lerp(left.Normal, right.Normal, amount);
                if (normal.sqrMagnitude > 0.000001f) normal.Normalize();
                Vector4 tangent = Vector4.Lerp(left.Tangent, right.Tangent, amount);
                Vector3 tangentDirection = new Vector3(tangent.x, tangent.y, tangent.z);
                if (tangentDirection.sqrMagnitude > 0.000001f) tangentDirection.Normalize();
                tangent = new Vector4(tangentDirection.x, tangentDirection.y, tangentDirection.z,
                    Mathf.Abs(tangent.w) < 0.5f ? left.Tangent.w : Mathf.Sign(tangent.w));
                return new SampleVertex
                {
                    Position = Vector3.Lerp(left.Position, right.Position, amount),
                    Normal = normal,
                    Tangent = tangent,
                    Uv = Vector2.Lerp(left.Uv, right.Uv, amount)
                };
            }
        }
    }
}
