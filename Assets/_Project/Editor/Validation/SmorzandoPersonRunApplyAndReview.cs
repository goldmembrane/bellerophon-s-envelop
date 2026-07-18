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
    internal static class SmorzandoPersonRunApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string ReferenceSlotName = "Smorzando_Person_01";
        private const string IdleSlotName = "Smorzando_Person_02";
        private const string WalkSlotName = "Smorzando_Person_03";
        private const string RunSlotName = "Smorzando_Person_04";
        private const string PersonModelName = "Smorzando_Person_Model";
        private const string RunningSourceRelativePath = "enemies model/smorzando running.fbx";
        private const string RunningModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person_Running.fbx";
        private const string StaticModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person.fbx";
        private const string RunControllerAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Run.controller";
        private const string ValidationRelativeFolder =
            "docs/validation/smorzando_person_run_2026-07-18";
        private const string InspectionReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonRunSourceInspection.txt";
        private const string ApplyReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonRunApply.txt";
        private const string CaptureRelativeFolder =
            ValidationRelativeFolder + "/automated_visual_capture";
        private const int CaptureLayer = 31;
        private const int CycleFramesPerSecond = 10;

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Person Running Source")]
        public static void InspectSmorzandoPersonRunningSource()
        {
            var runningAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RunningModelAssetPath) ??
                throw new InvalidOperationException("Smorzando running FBX has not been imported.");
            var staticAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StaticModelAssetPath) ??
                throw new InvalidOperationException("Smorzando static person FBX is missing.");
            var importer = AssetImporter.GetAtPath(RunningModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Smorzando running FBX importer is missing.");
            var runningRenderers = runningAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var staticRenderers = staticAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var clips = LoadRunClips();
            var report = new StringBuilder();
            report.AppendLine("Asset=" + RunningModelAssetPath);
            report.AppendLine("SourceSha256=" + ComputeSha256(ProjectAbsolutePath(RunningSourceRelativePath)));
            report.AppendLine("ImportedSha256=" + ComputeSha256(ProjectAbsolutePath(RunningModelAssetPath)));
            report.AppendLine("AnimationType=" + importer.animationType);
            report.AppendLine("ImportAnimation=" + importer.importAnimation);
            report.AppendLine("RunningRendererCount=" + runningRenderers.Length);
            report.AppendLine("StaticRendererCount=" + staticRenderers.Length);
            report.AppendLine("ClipCount=" + clips.Length);
            AppendRendererReport(report, "Running", runningRenderers);
            AppendRendererReport(report, "Static", staticRenderers);
            for (var clipIndex = 0; clipIndex < clips.Length; clipIndex++)
            {
                var clip = clips[clipIndex];
                var bindings = AnimationUtility.GetCurveBindings(clip);
                var rootMotionBindings = bindings
                    .Where(binding => string.IsNullOrEmpty(binding.path) ||
                        binding.propertyName.IndexOf("RootT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        binding.propertyName.IndexOf("MotionT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        binding.propertyName.IndexOf("RootQ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        binding.propertyName.IndexOf("MotionQ", StringComparison.OrdinalIgnoreCase) >= 0)
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
                $"SmorzandoPersonRunningSourceInspected Renderers={runningRenderers.Length}, " +
                $"Clips={clips.Length}, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Person Run")]
        public static void ApplySmorzandoPersonRun()
        {
            ConfigureRunClipLoopAndRootLocks();
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceModel = root.transform.Find(ReferenceSlotName + "/" + PersonModelName) ??
                throw new InvalidOperationException("Smorzando static person reference model is missing.");
            var idleModel = root.transform.Find(IdleSlotName + "/" + PersonModelName) ??
                throw new InvalidOperationException("Smorzando idle person model is missing.");
            var walkModel = root.transform.Find(WalkSlotName + "/" + PersonModelName) ??
                throw new InvalidOperationException("Smorzando walking person model is missing.");
            var runSlot = root.transform.Find(RunSlotName) ??
                throw new InvalidOperationException("Smorzando person run slot is missing.");
            var staleReplacement = runSlot.Find(PersonModelName + "_Replacement");
            if (staleReplacement != null)
            {
                UnityEngine.Object.DestroyImmediate(staleReplacement.gameObject);
            }

            var existingModel = runSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Existing Smorzando run-slot model is missing.");
            var referenceRenderers = referenceModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var runningAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RunningModelAssetPath) ??
                throw new InvalidOperationException("Smorzando running FBX has not been imported.");
            var runningAssetRenderers = runningAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            AssertMatchingGeometry(referenceRenderers, runningAssetRenderers);
            var clip = RequireRunClip();
            if (!clip.isLooping)
            {
                throw new InvalidOperationException("Smorzando running clip is not configured to loop.");
            }

            var controller = CreateOrUpdateRunController(clip);
            var idleController = GetController(idleModel);
            var walkController = GetController(walkModel);
            var preservedTransforms = root.GetComponentsInChildren<Transform>(true)
                .Where(target => target != existingModel && !target.IsChildOf(existingModel))
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var existingLocalPosition = existingModel.localPosition;
            var existingLocalRotation = existingModel.localRotation;
            var existingLocalScale = existingModel.localScale;
            var replacement = PrefabUtility.InstantiatePrefab(runningAsset, scene) as GameObject ??
                throw new InvalidOperationException("Smorzando running FBX could not be instantiated.");
            replacement.name = PersonModelName + "_Replacement";
            replacement.transform.SetParent(runSlot, false);
            replacement.transform.localPosition = existingLocalPosition;
            replacement.transform.localRotation = existingLocalRotation;
            replacement.transform.localScale = existingLocalScale;
            var replacementRenderers = replacement.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SynchronizeMaterials(referenceRenderers, replacementRenderers);
            foreach (var renderer in replacementRenderers)
            {
                renderer.updateWhenOffscreen = true;
            }

            var animator = replacement.GetComponent<Animator>();
            if (animator == null)
            {
                animator = replacement.AddComponent<Animator>();
            }
            if (animator == null)
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw new MissingComponentException("Smorzando running replacement Animator could not be created.");
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            AssertMatchingGeometry(referenceRenderers, replacementRenderers);
            AssertMaterialsSynchronized(referenceRenderers, replacementRenderers);
            UnityEngine.Object.DestroyImmediate(existingModel.gameObject);
            replacement.name = PersonModelName;
            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after person run apply.");
            }

            foreach (var snapshot in preservedTransforms)
            {
                snapshot.AssertUnchanged();
            }

            var appliedModel = runSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Applied Smorzando running model is missing after save.");
            var appliedAnimator = appliedModel.GetComponent<Animator>();
            if (appliedAnimator == null || appliedAnimator.runtimeAnimatorController != controller ||
                appliedAnimator.applyRootMotion)
            {
                throw new InvalidOperationException("Smorzando running Animator configuration was not preserved.");
            }
            if (GetController(idleModel) != idleController || GetController(walkModel) != walkController)
            {
                throw new InvalidOperationException("Smorzando idle or walk Animator controller changed unexpectedly.");
            }

            WriteTextReport(
                ApplyReportRelativePath,
                string.Join(
                    Environment.NewLine,
                    "Target=Approved Smorzando Enemy Placement/Smorzando_Person_04/Smorzando_Person_Model",
                    "StaticReference=Smorzando_Person_01",
                    "IdlePreserved=Smorzando_Person_02",
                    "WalkPreserved=Smorzando_Person_03",
                    "SourceAsset=" + RunningModelAssetPath,
                    "SourceSha256=" + ComputeSha256(ProjectAbsolutePath(RunningSourceRelativePath)),
                    "ImportedSha256=" + ComputeSha256(ProjectAbsolutePath(RunningModelAssetPath)),
                    "StaticSha256=" + ComputeSha256(ProjectAbsolutePath(StaticModelAssetPath)),
                    "RendererCount=" + replacementRenderers.Length,
                    "VertexCounts=" + string.Join("|", replacementRenderers.Select(RendererVertexCount)),
                    "BoneCounts=" + string.Join("|", replacementRenderers.Select(renderer => renderer.bones.Length)),
                    "Clip=" + clip.name,
                    "ClipLengthSeconds=" + clip.length.ToString("0.######"),
                    "ClipFrameRate=" + clip.frameRate.ToString("0.######"),
                    "ClipLoop=True",
                    "RootRotationLocked=True",
                    "RootHeightLocked=True",
                    "RootPositionXZLocked=True",
                    "ApplyRootMotion=False",
                    "MaterialReferenceMatched=True",
                    "GeometryMatchedStatic=True",
                    "IdleControllerPreserved=True",
                    "WalkControllerPreserved=True",
                    "OtherTransformsChanged=False",
                    "SelectionCleared=True") + Environment.NewLine);
            Selection.activeObject = null;
            Debug.Log(
                $"SmorzandoPersonRunApplied Target={RunSlotName}, Clip={clip.name}, " +
                $"Duration={clip.length:0.###}s, MaterialMatched=True, ApplyRootMotion=False, " +
                "OtherTransformsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Person Run Frames")]
        public static void CaptureSmorzandoPersonRunFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceSlot = root.transform.Find(ReferenceSlotName) ??
                throw new InvalidOperationException("Smorzando static person reference slot is missing.");
            var runSlot = root.transform.Find(RunSlotName) ??
                throw new InvalidOperationException("Smorzando person run slot is missing.");
            var clip = RequireRunClip();
            var frameCount = Mathf.Max(1, Mathf.RoundToInt(clip.length * CycleFramesPerSecond));
            var captureFolder = ProjectAbsolutePath(CaptureRelativeFolder);
            var frontFolder = Path.Combine(captureFolder, "front_cycle_frames");
            var obliqueFolder = Path.Combine(captureFolder, "oblique_cycle_frames");
            Directory.CreateDirectory(frontFolder);
            Directory.CreateDirectory(obliqueFolder);

            var cameraObject = new GameObject("Smorzando_PersonRun_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_PersonRun_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject referenceClone = null;
            GameObject runClone = null;
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
                referenceClone.name = "Smorzando_PersonRun_StaticReferenceClone";
                runClone = UnityEngine.Object.Instantiate(runSlot.gameObject);
                runClone.name = "Smorzando_PersonRun_MotionClone";
                referenceClone.transform.position = Vector3.zero;
                runClone.transform.position = Vector3.zero;
                SetCaptureOnly(referenceClone);
                SetCaptureOnly(runClone);
                DisableHelperComponents(referenceClone);
                DisableHelperComponents(runClone);
                var runModel = runClone.transform.Find(PersonModelName) ??
                    throw new InvalidOperationException("Smorzando running capture model is missing.");
                var runAnimator = runModel.GetComponent<Animator>();
                if (runAnimator != null)
                {
                    runAnimator.enabled = false;
                }

                var modelBasePosition = runModel.localPosition;
                var modelBaseRotation = runModel.localRotation;
                var modelBaseScale = runModel.localScale;
                SampleInPlace(clip, runModel, 0f, modelBasePosition, modelBaseRotation, modelBaseScale);
                var referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                var runBounds = CalculateVisibleBounds(runClone.transform);
                foreach (var sampleTime in new[] { clip.length * 0.25f, clip.length * 0.5f, clip.length * 0.75f })
                {
                    SampleInPlace(clip, runModel, sampleTime, modelBasePosition, modelBaseRotation, modelBaseScale);
                    runBounds.Encapsulate(CalculateVisibleBounds(runClone.transform));
                }

                var halfSpacing = (referenceBounds.extents.x + runBounds.extents.x + 0.55f) * 0.5f;
                referenceClone.transform.position += Vector3.right * (-halfSpacing - referenceBounds.center.x);
                runClone.transform.position += Vector3.right * (halfSpacing - runBounds.center.x);
                referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                runBounds = CalculateVisibleBounds(runClone.transform);
                var pairBounds = referenceBounds;
                pairBounds.Encapsulate(runBounds);
                floor = CreateCaptureFloor(pairBounds, out floorMaterial);

                referenceClone.SetActive(false);
                var runTarget = runBounds.center;
                var runOrthoSize = Mathf.Max(runBounds.extents.y + 0.26f, runBounds.extents.x + 0.26f);
                var frontPosition = runTarget + Vector3.back * 35f;
                var obliqueDirection = (Vector3.back + Vector3.right * 0.48f).normalized;
                var obliquePosition = runTarget + obliqueDirection * 35f;
                for (var frame = 0; frame < frameCount; frame++)
                {
                    var time = frame * clip.length / frameCount;
                    SampleInPlace(clip, runModel, time, modelBasePosition, modelBaseRotation, modelBaseScale);
                    CapturePng(camera, frontPosition, runTarget, runOrthoSize, 640, 640,
                        Path.Combine(frontFolder, $"Smorzando_PersonRun_Front_{frame:000}.png"));
                    CapturePng(camera, obliquePosition, runTarget, runOrthoSize, 640, 640,
                        Path.Combine(obliqueFolder, $"Smorzando_PersonRun_Oblique_{frame:000}.png"));
                }

                referenceClone.SetActive(true);
                var pairTarget = pairBounds.center + Vector3.up * 0.02f;
                var pairOrthoSize = Mathf.Max(pairBounds.extents.y + 0.28f,
                    pairBounds.extents.x / (16f / 9f) + 0.28f);
                SampleInPlace(clip, runModel, 0f, modelBasePosition, modelBaseRotation, modelBaseScale);
                CapturePng(camera, pairTarget + Vector3.back * 40f, pairTarget, pairOrthoSize, 1280, 720,
                    Path.Combine(captureFolder, "Smorzando_PersonRun_StaticVsRun_T000.png"));
                SampleInPlace(clip, runModel, clip.length * 0.25f,
                    modelBasePosition, modelBaseRotation, modelBaseScale);
                CapturePng(camera, pairTarget + Vector3.back * 40f, pairTarget, pairOrthoSize, 1280, 720,
                    Path.Combine(captureFolder, "Smorzando_PersonRun_StaticVsRun_T025.png"));

                var keyFrames = new[]
                {
                    0,
                    Mathf.RoundToInt((frameCount - 1) * 0.25f),
                    Mathf.RoundToInt((frameCount - 1) * 0.5f),
                    Mathf.RoundToInt((frameCount - 1) * 0.75f),
                    frameCount - 1
                };
                CreateKeyframeSheet(frontFolder, obliqueFolder, captureFolder, keyFrames);
                var videoPath = Path.Combine(captureFolder, "Smorzando_PersonRun_Loop.mp4");
                EncodeLoopVideo(frontFolder, videoPath);
                File.WriteAllLines(
                    Path.Combine(captureFolder, "Smorzando_PersonRun_CaptureManifest.txt"),
                    new[]
                    {
                        "Clip=" + clip.name,
                        "CycleDurationSeconds=" + clip.length.ToString("0.######"),
                        "CycleFrameCount=" + frameCount,
                        "CycleFramesPerSecond=" + CycleFramesPerSecond,
                        "KeyFrames=" + string.Join("|", keyFrames.Select(frame => frame.ToString("000"))),
                        "TargetSlot=Smorzando_Person_04",
                        "StaticReferenceSlot=Smorzando_Person_01",
                        "MaterialReferenceMatched=True",
                        "GeometryMatchedStatic=True",
                        "ApplyRootMotion=False",
                        "ModelRootTransformHeldInPlace=True",
                        "Views=FrontCycle|ObliqueCycle|StaticVsRun|KeyframeSheet|LoopVideo",
                        "VideoEncoded=True",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                Selection.activeObject = null;
                Debug.Log(
                    $"SmorzandoPersonRunFramesCaptured Folder={captureFolder}, Frames={frameCount}, " +
                    "Views=Front|Oblique|StaticVsRun|LoopVideo, MaterialMatched=True, VideoEncoded=True, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(referenceClone);
                UnityEngine.Object.DestroyImmediate(runClone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando person run capture changed the scene dirty state.");
                }
            }
        }

        private static void ConfigureRunClipLoopAndRootLocks()
        {
            var importer = AssetImporter.GetAtPath(RunningModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Smorzando running FBX importer is missing.");
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException("Smorzando running FBX must contain exactly one default clip.");
            }

            clips[0].name = "Smorzando_Person_Run";
            clips[0].loopTime = true;
            clips[0].loopPose = true;
            clips[0].lockRootRotation = true;
            clips[0].lockRootHeightY = true;
            clips[0].lockRootPositionXZ = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController CreateOrUpdateRunController(AnimationClip clip)
        {
            EnsureAssetFolder(RunControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(RunControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(RunControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "Run") ?? stateMachine.AddState("Run");
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static RuntimeAnimatorController GetController(Transform model)
        {
            var animator = model.GetComponent<Animator>();
            return animator != null ? animator.runtimeAnimatorController : null;
        }

        private static AnimationClip RequireRunClip()
        {
            var clips = LoadRunClips();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException("Smorzando running FBX must contain exactly one animation clip.");
            }
            return clips[0];
        }

        private static AnimationClip[] LoadRunClips()
        {
            return AssetDatabase.LoadAllAssetsAtPath(RunningModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static void AssertMatchingGeometry(
            IReadOnlyList<SkinnedMeshRenderer> referenceRenderers,
            IReadOnlyList<SkinnedMeshRenderer> runningRenderers)
        {
            if (referenceRenderers.Count == 0 || referenceRenderers.Count != runningRenderers.Count)
            {
                throw new InvalidOperationException(
                    "Smorzando running renderer count does not match the static person model.");
            }
            for (var index = 0; index < referenceRenderers.Count; index++)
            {
                var referenceRenderer = referenceRenderers[index];
                var runningRenderer = runningRenderers[index];
                var referenceMesh = referenceRenderer.sharedMesh;
                var runningMesh = runningRenderer.sharedMesh;
                if (referenceMesh == null || runningMesh == null ||
                    referenceMesh.vertexCount != runningMesh.vertexCount ||
                    referenceMesh.subMeshCount != runningMesh.subMeshCount ||
                    referenceRenderer.bones.Length != runningRenderer.bones.Length ||
                    referenceMesh.bounds.center != runningMesh.bounds.center ||
                    referenceMesh.bounds.size != runningMesh.bounds.size)
                {
                    throw new InvalidOperationException(
                        "Smorzando running model geometry does not match the static person model.");
                }
            }
        }

        private static void SynchronizeMaterials(
            IReadOnlyList<SkinnedMeshRenderer> referenceRenderers,
            IReadOnlyList<SkinnedMeshRenderer> runningRenderers)
        {
            if (referenceRenderers.Count != runningRenderers.Count)
            {
                throw new InvalidOperationException("Smorzando running material renderer count does not match.");
            }
            for (var index = 0; index < referenceRenderers.Count; index++)
            {
                runningRenderers[index].sharedMaterials = referenceRenderers[index].sharedMaterials;
            }
        }

        private static void AssertMaterialsSynchronized(
            IReadOnlyList<SkinnedMeshRenderer> referenceRenderers,
            IReadOnlyList<SkinnedMeshRenderer> runningRenderers)
        {
            for (var rendererIndex = 0; rendererIndex < referenceRenderers.Count; rendererIndex++)
            {
                var referenceMaterials = referenceRenderers[rendererIndex].sharedMaterials;
                var runningMaterials = runningRenderers[rendererIndex].sharedMaterials;
                if (referenceMaterials.Length != runningMaterials.Length)
                {
                    throw new InvalidOperationException("Smorzando running material slot count does not match.");
                }
                for (var materialIndex = 0; materialIndex < referenceMaterials.Length; materialIndex++)
                {
                    if (referenceMaterials[materialIndex] != runningMaterials[materialIndex])
                    {
                        throw new InvalidOperationException(
                            "Smorzando running material is not the static reference material asset.");
                    }
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

        private static int RendererVertexCount(SkinnedMeshRenderer renderer)
        {
            return renderer.sharedMesh != null ? renderer.sharedMesh.vertexCount : 0;
        }

        private static void SampleInPlace(
            AnimationClip clip,
            Transform model,
            float time,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            clip.SampleAnimation(model.gameObject, time);
            model.localPosition = position;
            model.localRotation = rotation;
            model.localScale = scale;
        }

        private static void EncodeLoopVideo(string frameFolder, string videoPath)
        {
            var inputPattern = Path.Combine(frameFolder, "Smorzando_PersonRun_Front_%03d.png");
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-y -loglevel error -framerate {CycleFramesPerSecond} -i \"{inputPattern}\" " +
                    $"-c:v libx264 -pix_fmt yuv420p -movflags +faststart \"{videoPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            using var process = Process.Start(startInfo) ??
                throw new InvalidOperationException("ffmpeg could not be started for person run video.");
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(60000) || process.ExitCode != 0)
            {
                throw new InvalidOperationException("Person run video encoding failed: " + error);
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
                sheet.SetPixels(Enumerable.Repeat(new Color(0.018f, 0.014f, 0.012f, 1f),
                    sheet.width * sheet.height).ToArray());
                for (var index = 0; index < keyFrames.Count; index++)
                {
                    CopyPngToSheet(Path.Combine(frontFolder,
                        $"Smorzando_PersonRun_Front_{keyFrames[index]:000}.png"), sheet, index * cellSize, cellSize);
                    CopyPngToSheet(Path.Combine(obliqueFolder,
                        $"Smorzando_PersonRun_Oblique_{keyFrames[index]:000}.png"), sheet, index * cellSize, 0);
                }
                sheet.Apply();
                File.WriteAllBytes(Path.Combine(captureFolder, "Smorzando_PersonRun_KeyframeSheet.png"),
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
                    throw new InvalidDataException("Unexpected person run capture size: " + path);
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
            floor.name = "Smorzando_PersonRun_CaptureFloor";
            floor.hideFlags = HideFlags.HideAndDontSave;
            floor.layer = CaptureLayer;
            floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
            floor.transform.localScale = new Vector3(Mathf.Max(bounds.size.x + 2f, 5f), 0.05f,
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
            float orthographicSize,
            int width,
            int height,
            string path)
        {
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, Vector3.up);
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
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Smorzando person run capture has no visible renderers.");
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
                    throw new InvalidOperationException("Smorzando person run apply changed a preserved Transform.");
                }
            }
        }
    }
}
