using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Bellerophon.Editor.SmorzandoCargoRunScene
{
    internal static class SmorzandoPersonWalkApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string ReferenceSlotName = "Smorzando_Person_01";
        private const string IdleSlotName = "Smorzando_Person_02";
        private const string WalkSlotName = "Smorzando_Person_03";
        private const string PersonModelName = "Smorzando_Person_Model";
        private const string WalkingSourceRelativePath = "enemies model/smorzando walking.fbx";
        private const string WalkingModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person_Walking.fbx";
        private const string StaticModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person.fbx";
        private const string WalkControllerAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Walk.controller";
        private const string ValidationRelativeFolder =
            "docs/validation/smorzando_person_walk_2026-07-18";
        private const string InspectionReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonWalkSourceInspection.txt";
        private const string ApplyReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonWalkApply.txt";
        private const string CaptureRelativeFolder =
            ValidationRelativeFolder + "/automated_visual_capture";
        private const int CaptureLayer = 31;
        private const int CycleFramesPerSecond = 10;

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Person Walking Source")]
        public static void InspectSmorzandoPersonWalkingSource()
        {
            var walkingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WalkingModelAssetPath) ??
                throw new InvalidOperationException("Smorzando walking FBX has not been imported.");
            var staticAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelAssetPath) ??
                throw new InvalidOperationException("Smorzando static person FBX is missing.");
            var importer = AssetImporter.GetAtPath(WalkingModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Smorzando walking FBX importer is missing.");
            var walkingRenderers = walkingAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var staticRenderers = staticAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var clips = LoadWalkClips();
            var report = new StringBuilder();
            report.AppendLine("Asset=" + WalkingModelAssetPath);
            report.AppendLine("AnimationType=" + importer.animationType);
            report.AppendLine("ImportAnimation=" + importer.importAnimation);
            report.AppendLine("WalkingRendererCount=" + walkingRenderers.Length);
            report.AppendLine("StaticRendererCount=" + staticRenderers.Length);
            report.AppendLine("ClipCount=" + clips.Length);
            AppendRendererReport(report, "Walking", walkingRenderers);
            AppendRendererReport(report, "Static", staticRenderers);
            for (var clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                var clip = clips[clipIndex];
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var rootMotionBindings = bindings
                    .Where(binding => string.IsNullOrEmpty(binding.path) ||
                        binding.propertyName.IndexOf("RootT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        binding.propertyName.IndexOf("MotionT", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(binding => binding.path + ":" + binding.propertyName)
                    .Distinct()
                    .ToArray();
                report.AppendLine(
                    $"Clip[{clipIndex}]={clip.name},Length={clip.length:0.######},FrameRate={clip.frameRate:0.######}," +
                    $"Loop={clip.isLooping},Curves={bindings.Length},RootMotionBindings={rootMotionBindings.Length}");
                foreach (var binding in rootMotionBindings)
                {
                    report.AppendLine("RootMotionBinding=" + binding);
                }
            }

            WriteTextReport(InspectionReportRelativePath, report.ToString());
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoPersonWalkingSourceInspected Renderers={walkingRenderers.Length}, " +
                $"Clips={clips.Length}, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Person Walk")]
        public static void ApplySmorzandoPersonWalk()
        {
            ConfigureWalkClipLoop();
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceModel = root.transform.Find(ReferenceSlotName + "/" + PersonModelName) ??
                throw new InvalidOperationException("Smorzando static person reference model is missing.");
            var idleModel = root.transform.Find(IdleSlotName + "/" + PersonModelName) ??
                throw new InvalidOperationException("Smorzando idle person model is missing.");
            var walkSlot = root.transform.Find(WalkSlotName) ??
                throw new InvalidOperationException("Smorzando person walk slot is missing.");
            var staleReplacement = walkSlot.Find(PersonModelName + "_Replacement");
            if (staleReplacement != null)
            {
                UnityEngine.Object.DestroyImmediate(staleReplacement.gameObject);
            }
            var existingModel = walkSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Existing Smorzando walk-slot model is missing.");
            var referenceRenderer = referenceModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Smorzando static reference has no SkinnedMeshRenderer.");
            var walkingAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WalkingModelAssetPath) ??
                throw new InvalidOperationException("Smorzando walking FBX has not been imported.");
            var walkingAssetRenderer = walkingAsset.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Smorzando walking FBX has no SkinnedMeshRenderer.");
            AssertMatchingGeometry(referenceRenderer, walkingAssetRenderer);
            var clip = RequireWalkClip();
            if (!clip.isLooping)
            {
                throw new InvalidOperationException("Smorzando walking clip is not configured to loop.");
            }

            var controller = CreateOrUpdateWalkController(clip);
            var preservedTransforms = root.GetComponentsInChildren<Transform>(true)
                .Where(target => target != existingModel && !target.IsChildOf(existingModel))
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var existingLocalPosition = existingModel.localPosition;
            var existingLocalRotation = existingModel.localRotation;
            var existingLocalScale = existingModel.localScale;
            var replacement = PrefabUtility.InstantiatePrefab(walkingAsset, scene) as GameObject ??
                throw new InvalidOperationException("Smorzando walking FBX could not be instantiated.");
            replacement.name = PersonModelName + "_Replacement";
            replacement.transform.SetParent(walkSlot, false);
            replacement.transform.localPosition = existingLocalPosition;
            replacement.transform.localRotation = existingLocalRotation;
            replacement.transform.localScale = existingLocalScale;
            var replacementRenderer = replacement.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Smorzando walking replacement has no SkinnedMeshRenderer.");
            replacementRenderer.sharedMaterials = referenceRenderer.sharedMaterials;
            replacementRenderer.updateWhenOffscreen = true;
            var animator = replacement.GetComponent<Animator>();
            if (animator == null)
            {
                animator = replacement.AddComponent<Animator>();
            }
            if (animator == null)
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw new MissingComponentException("Smorzando walking replacement Animator could not be created.");
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            AssertMatchingGeometry(referenceRenderer, replacementRenderer);
            AssertMaterialsSynchronized(referenceRenderer, replacementRenderer);
            UnityEngine.Object.DestroyImmediate(existingModel.gameObject);
            replacement.name = PersonModelName;
            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after person walk apply.");
            }

            foreach (var snapshot in preservedTransforms)
            {
                snapshot.AssertUnchanged();
            }

            var appliedModel = walkSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Applied Smorzando walking model is missing after save.");
            var appliedAnimator = appliedModel.GetComponent<Animator>() ??
                throw new InvalidOperationException("Applied Smorzando walking model has no Animator.");
            if (appliedAnimator.runtimeAnimatorController != controller || appliedAnimator.applyRootMotion)
            {
                throw new InvalidOperationException("Smorzando walking Animator configuration was not preserved.");
            }

            var idleAnimator = idleModel.GetComponent<Animator>();
            if (idleAnimator != null && idleAnimator.runtimeAnimatorController == controller)
            {
                throw new InvalidOperationException("Smorzando idle person was incorrectly assigned the walk controller.");
            }

            WriteTextReport(
                ApplyReportRelativePath,
                string.Join(
                    Environment.NewLine,
                    "Target=Approved Smorzando Enemy Placement/Smorzando_Person_03/Smorzando_Person_Model",
                    "StaticReference=Smorzando_Person_01",
                    "IdlePreserved=Smorzando_Person_02",
                    "SourceAsset=" + WalkingModelAssetPath,
                    "SourceSha256=" + ComputeSha256(ProjectAbsolutePath(WalkingSourceRelativePath)),
                    "ImportedSha256=" + ComputeSha256(ProjectAbsolutePath(WalkingModelAssetPath)),
                    "StaticSha256=" + ComputeSha256(ProjectAbsolutePath(StaticModelAssetPath)),
                    "Renderer=char1",
                    "VertexCount=" + replacementRenderer.sharedMesh.vertexCount,
                    "SubMeshCount=" + replacementRenderer.sharedMesh.subMeshCount,
                    "BoneCount=" + replacementRenderer.bones.Length,
                    "Clip=" + clip.name,
                    "ClipLengthSeconds=" + clip.length.ToString("0.######"),
                    "ClipFrameRate=" + clip.frameRate.ToString("0.######"),
                    "ClipLoop=True",
                    "ApplyRootMotion=False",
                    "Material=" + referenceRenderer.sharedMaterial.name,
                    "MaterialReferenceMatched=True",
                    "GeometryMatchedStatic=True",
                    "OtherTransformsChanged=False",
                    "SelectionCleared=True") + Environment.NewLine);
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoPersonWalkApplied Target={WalkSlotName}, Clip={clip.name}, " +
                $"Duration={clip.length:0.###}s, MaterialMatched=True, OtherTransformsChanged=False, " +
                "SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Person Walk Frames")]
        public static void CaptureSmorzandoPersonWalkFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceSlot = root.transform.Find(ReferenceSlotName) ??
                throw new InvalidOperationException("Smorzando static person reference slot is missing.");
            var walkSlot = root.transform.Find(WalkSlotName) ??
                throw new InvalidOperationException("Smorzando person walk slot is missing.");
            var clip = RequireWalkClip();
            var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * CycleFramesPerSecond));
            var captureFolder = ProjectAbsolutePath(CaptureRelativeFolder);
            var frontFolder = Path.Combine(captureFolder, "front_cycle_frames");
            var obliqueFolder = Path.Combine(captureFolder, "oblique_cycle_frames");
            Directory.CreateDirectory(frontFolder);
            Directory.CreateDirectory(obliqueFolder);

            var cameraObject = new GameObject("Smorzando_PersonWalk_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_PersonWalk_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject referenceClone = null;
            GameObject walkClone = null;
            GameObject floor = null;
            Material floorMaterial = null;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.014f, 0.012f, 1f);
                camera.orthographic = true;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 3.4f;
                light.color = new Color(1f, 0.82f, 0.68f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

                referenceClone = UnityEngine.Object.Instantiate(referenceSlot.gameObject);
                referenceClone.name = "Smorzando_PersonWalk_StaticReferenceClone";
                walkClone = UnityEngine.Object.Instantiate(walkSlot.gameObject);
                walkClone.name = "Smorzando_PersonWalk_MotionClone";
                referenceClone.transform.position = Vector3.zero;
                walkClone.transform.position = Vector3.zero;
                SetCaptureOnly(referenceClone);
                SetCaptureOnly(walkClone);
                DisableHelperComponents(referenceClone);
                DisableHelperComponents(walkClone);
                var walkModel = walkClone.transform.Find(PersonModelName) ??
                    throw new InvalidOperationException("Smorzando walking capture model is missing.");
                var walkAnimator = walkModel.GetComponent<Animator>();
                if (walkAnimator != null)
                {
                    walkAnimator.enabled = false;
                }

                var modelBasePosition = walkModel.localPosition;
                var modelBaseRotation = walkModel.localRotation;
                var modelBaseScale = walkModel.localScale;
                clip.SampleAnimation(walkModel.gameObject, 0f);
                var referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                var walkBounds = CalculateVisibleBounds(walkClone.transform);
                foreach (var sampleTime in new[] { clip.length * 0.25f, clip.length * 0.5f, clip.length * 0.75f })
                {
                    clip.SampleAnimation(walkModel.gameObject, sampleTime);
                    walkBounds.Encapsulate(CalculateVisibleBounds(walkClone.transform));
                }

                var halfSpacing = (referenceBounds.extents.x + walkBounds.extents.x + 0.55f) * 0.5f;
                referenceClone.transform.position += Vector3.right *
                    (-halfSpacing - referenceBounds.center.x);
                walkClone.transform.position += Vector3.right *
                    (halfSpacing - walkBounds.center.x);
                referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                walkBounds = CalculateVisibleBounds(walkClone.transform);
                var pairBounds = referenceBounds;
                pairBounds.Encapsulate(walkBounds);
                floor = CreateCaptureFloor(pairBounds, out floorMaterial);

                referenceClone.SetActive(false);
                var walkTarget = walkBounds.center;
                var walkOrthoSize = Mathf.Max(walkBounds.extents.y + 0.26f, walkBounds.extents.x + 0.26f);
                var frontPosition = walkTarget + Vector3.back * 35f;
                var obliqueDirection = (Vector3.back + Vector3.right * 0.48f).normalized;
                var obliquePosition = walkTarget + obliqueDirection * 35f;
                for (var frame = 0; frame < frameCount; frame++)
                {
                    var time = frame * clip.length / frameCount;
                    clip.SampleAnimation(walkModel.gameObject, time);
                    CapturePng(
                        camera,
                        frontPosition,
                        walkTarget,
                        Vector3.up,
                        walkOrthoSize,
                        640,
                        640,
                        Path.Combine(frontFolder, $"Smorzando_PersonWalk_Front_{frame:000}.png"));
                    CapturePng(
                        camera,
                        obliquePosition,
                        walkTarget,
                        Vector3.up,
                        walkOrthoSize,
                        640,
                        640,
                        Path.Combine(obliqueFolder, $"Smorzando_PersonWalk_Oblique_{frame:000}.png"));
                }

                referenceClone.SetActive(true);
                var pairTarget = pairBounds.center + Vector3.up * 0.02f;
                var pairOrthoSize = Mathf.Max(
                    pairBounds.extents.y + 0.28f,
                    pairBounds.extents.x / (16f / 9f) + 0.28f);
                clip.SampleAnimation(walkModel.gameObject, 0f);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    Vector3.up,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonWalk_StaticVsWalk_T000.png"));
                clip.SampleAnimation(walkModel.gameObject, clip.length * 0.25f);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    Vector3.up,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonWalk_StaticVsWalk_T103.png"));

                var keyFrames = new[]
                {
                    0,
                    Mathf.RoundToInt((frameCount - 1) * 0.25f),
                    Mathf.RoundToInt((frameCount - 1) * 0.5f),
                    Mathf.RoundToInt((frameCount - 1) * 0.75f),
                    frameCount - 1
                };
                CreateKeyframeSheet(frontFolder, obliqueFolder, captureFolder, keyFrames);
                var videoPath = Path.Combine(captureFolder, "Smorzando_PersonWalk_Loop.mp4");
                EncodeLoopVideo(frontFolder, videoPath);
                AssertTransformUnchanged(walkModel, modelBasePosition, modelBaseRotation, modelBaseScale);
                File.WriteAllLines(
                    Path.Combine(captureFolder, "Smorzando_PersonWalk_CaptureManifest.txt"),
                    new[]
                    {
                        "Clip=" + clip.name,
                        "CycleDurationSeconds=" + clip.length.ToString("0.######"),
                        "CycleFrameCount=" + frameCount,
                        "CycleFramesPerSecond=" + CycleFramesPerSecond,
                        "KeyFrames=" + string.Join("|", keyFrames.Select(frame => frame.ToString("000"))),
                        "TargetSlot=Smorzando_Person_03",
                        "StaticReferenceSlot=Smorzando_Person_01",
                        "MaterialReferenceMatched=True",
                        "GeometryMatchedStatic=True",
                        "ApplyRootMotion=False",
                        "ModelRootTransformAnimated=False",
                        "Views=FrontCycle|ObliqueCycle|StaticVsWalk|KeyframeSheet|LoopVideo",
                        "VideoEncoded=True",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                Selection.activeObject = null;
                Debug.Log(
                    $"SmorzandoPersonWalkFramesCaptured Folder={captureFolder}, Frames={frameCount}, " +
                    "Views=Front|Oblique|StaticVsWalk|LoopVideo, MaterialMatched=True, VideoEncoded=True, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(referenceClone);
                UnityEngine.Object.DestroyImmediate(walkClone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando person walk capture changed the scene dirty state.");
                }
            }
        }

        private static void ConfigureWalkClipLoop()
        {
            var importer = AssetImporter.GetAtPath(WalkingModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Smorzando walking FBX importer is missing.");
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException("Smorzando walking FBX must contain exactly one default clip.");
            }

            clips[0].name = "Smorzando_Person_Walk";
            clips[0].loopTime = true;
            clips[0].loopPose = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController CreateOrUpdateWalkController(AnimationClip clip)
        {
            EnsureAssetFolder(WalkControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(WalkControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(WalkControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "Walk") ?? stateMachine.AddState("Walk");
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip RequireWalkClip()
        {
            var clips = LoadWalkClips();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException("Smorzando walking FBX must contain exactly one animation clip.");
            }

            return clips[0];
        }

        private static AnimationClip[] LoadWalkClips()
        {
            return AssetDatabase.LoadAllAssetsAtPath(WalkingModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static void AssertMatchingGeometry(
            SkinnedMeshRenderer referenceRenderer,
            SkinnedMeshRenderer walkingRenderer)
        {
            var referenceMesh = referenceRenderer.sharedMesh;
            var walkingMesh = walkingRenderer.sharedMesh;
            if (referenceMesh == null || walkingMesh == null ||
                referenceMesh.vertexCount != walkingMesh.vertexCount ||
                referenceMesh.subMeshCount != walkingMesh.subMeshCount ||
                referenceRenderer.bones.Length != walkingRenderer.bones.Length ||
                referenceMesh.bounds.center != walkingMesh.bounds.center ||
                referenceMesh.bounds.size != walkingMesh.bounds.size)
            {
                throw new InvalidOperationException(
                    "Smorzando walking model geometry does not match the static person model.");
            }
        }

        private static void AssertMaterialsSynchronized(
            SkinnedMeshRenderer referenceRenderer,
            SkinnedMeshRenderer walkingRenderer)
        {
            var referenceMaterials = referenceRenderer.sharedMaterials;
            var walkingMaterials = walkingRenderer.sharedMaterials;
            if (referenceMaterials.Length != walkingMaterials.Length)
            {
                throw new InvalidOperationException("Smorzando walking material slot count does not match static reference.");
            }

            for (var index = 0; index < referenceMaterials.Length; index++)
            {
                if (referenceMaterials[index] != walkingMaterials[index])
                {
                    throw new InvalidOperationException(
                        "Smorzando walking material is not the same asset as the static reference material.");
                }
            }
        }

        private static void AppendRendererReport(
            StringBuilder report,
            string prefix,
            IReadOnlyList<SkinnedMeshRenderer> renderers)
        {
            for (var index = 0; index < renderers.Count; index++)
            {
                var renderer = renderers[index];
                var mesh = renderer.sharedMesh;
                report.AppendLine(
                    $"{prefix}Renderer[{index}]={renderer.name},Mesh={mesh?.name},Vertices={mesh?.vertexCount ?? 0}," +
                    $"SubMeshes={mesh?.subMeshCount ?? 0},Bounds={FormatBounds(mesh?.bounds ?? default)}," +
                    $"Bones={renderer.bones.Length},Materials={string.Join("|", renderer.sharedMaterials.Select(MaterialName))}");
            }
        }

        private static void EncodeLoopVideo(string frameFolder, string videoPath)
        {
            var inputPattern = Path.Combine(frameFolder, "Smorzando_PersonWalk_Front_%03d.png");
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments =
                    $"-y -loglevel error -framerate {CycleFramesPerSecond} -i \"{inputPattern}\" " +
                    $"-c:v libx264 -pix_fmt yuv420p -movflags +faststart \"{videoPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(startInfo) ??
                throw new InvalidOperationException("ffmpeg could not be started for person walk video.");
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(60000) || process.ExitCode != 0)
            {
                throw new InvalidOperationException("Person walk video encoding failed: " + error);
            }
        }

        private static void CreateKeyframeSheet(
            string frontFolder,
            string obliqueFolder,
            string captureFolder,
            IReadOnlyList<int> keyFrames)
        {
            const int cellSize = 640;
            var sheet = new Texture2D(cellSize * keyFrames.Count, cellSize * 2, TextureFormat.RGBA32, false);
            try
            {
                sheet.SetPixels(Enumerable.Repeat(
                    new Color(0.018f, 0.014f, 0.012f, 1f),
                    sheet.width * sheet.height).ToArray());
                for (var index = 0; index < keyFrames.Count; index++)
                {
                    CopyPngToSheet(
                        Path.Combine(frontFolder, $"Smorzando_PersonWalk_Front_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        cellSize);
                    CopyPngToSheet(
                        Path.Combine(obliqueFolder, $"Smorzando_PersonWalk_Oblique_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        0);
                }

                sheet.Apply();
                File.WriteAllBytes(
                    Path.Combine(captureFolder, "Smorzando_PersonWalk_KeyframeSheet.png"),
                    sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void CopyPngToSheet(string path, Texture2D sheet, int x, int y)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(File.ReadAllBytes(path)) || texture.width != 640 || texture.height != 640)
                {
                    throw new InvalidDataException("Unexpected person walk capture size: " + path);
                }

                sheet.SetPixels(x, y, texture.width, texture.height, texture.GetPixels());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static GameObject CreateCaptureFloor(Bounds bounds, out Material material)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Smorzando_PersonWalk_CaptureFloor";
            floor.hideFlags = HideFlags.HideAndDontSave;
            floor.layer = CaptureLayer;
            floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
            floor.transform.localScale = new Vector3(
                Mathf.Max(bounds.size.x + 2f, 5f),
                0.05f,
                Mathf.Max(bounds.size.z + 2f, 5f));
            var collider = floor.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
                color = new Color(0.11f, 0.085f, 0.07f, 1f)
            };
            floor.GetComponent<MeshRenderer>().sharedMaterial = material;
            return floor;
        }

        private static void CapturePng(
            Camera camera,
            Vector3 cameraPosition,
            Vector3 target,
            Vector3 up,
            float orthographicSize,
            int width,
            int height,
            string path)
        {
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, up);
            camera.orthographicSize = orthographicSize;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ProjectAbsolutePath(CaptureRelativeFolder));
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Bounds CalculateVisibleBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Smorzando person walk capture has no visible renderers.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void SetCaptureOnly(GameObject root)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = CaptureLayer;
                target.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static void DisableHelperComponents(GameObject root)
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                camera.enabled = false;
            }

            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }
        }

        private static void AssertTransformUnchanged(
            Transform target,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            if (target.localPosition != localPosition || target.localRotation != localRotation ||
                target.localScale != localScale)
            {
                throw new InvalidOperationException("Smorzando walk clip animated the model root Transform.");
            }
        }

        private static Scene RequireOpenCargoRunScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            return scene;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name) ??
                throw new InvalidOperationException(name + " root is missing from CargoRunMvp.");
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            var current = "Assets";
            foreach (var part in folder.Split('/').Skip(1))
            {
                var next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, part);
                }
                current = next;
            }
        }

        private static void WriteTextReport(string relativePath, string contents)
        {
            var path = ProjectAbsolutePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ProjectAbsolutePath("docs/validation"));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string MaterialName(Material material)
        {
            return material != null ? material.name : "None";
        }

        private static string FormatBounds(Bounds bounds)
        {
            return $"Center({bounds.center.x:0.######},{bounds.center.y:0.######},{bounds.center.z:0.######})" +
                $"Size({bounds.size.x:0.######},{bounds.size.y:0.######},{bounds.size.z:0.######})";
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
            }

            public void AssertUnchanged()
            {
                if (target == null || target.localPosition != localPosition ||
                    target.localRotation != localRotation || target.localScale != localScale)
                {
                    throw new InvalidOperationException("Smorzando person walk apply changed a preserved Transform.");
                }
            }
        }
    }
}
