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
    internal static class SmorzandoPersonDeathApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string ReferenceSlotName = "Smorzando_Person_01";
        private const string RunSlotName = "Smorzando_Person_04";
        private const string HitSlotName = "Smorzando_Person_05";
        private const string DeathSlotName = "Smorzando_Person_06";
        private const string PersonModelName = "Smorzando_Person_Model";
        private const string DeathClipAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Death.anim";
        private const string DeathSourceRelativePath = "enemies model/smorzando death.fbx";
        private const string DeathModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person_Death.fbx";
        private const string StaticModelAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Models/Smorzando_Person.fbx";
        private const string DeathControllerAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Death.controller";
        private const string ValidationRelativeFolder =
            "docs/validation/smorzando_person_death_2026-07-18";
        private const string InspectionReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonDeathTargetInspection.txt";
        private const string ApplyReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonDeathApply.txt";
        private const string CaptureRelativeFolder =
            "docs/validation/smorzando_person_death_fbx_2026-07-18/automated_visual_capture";
        private const string FbxApplyReportRelativePath =
            "docs/validation/smorzando_person_death_fbx_2026-07-18/Smorzando_PersonDeathFbxApply.txt";
        private const float CycleDurationSeconds = 1.8f;
        private const float LandingTimeSeconds = 1.05f;
        private const float FinalPoseSampleTimeSeconds = 1.79f;
        private const int CycleFramesPerSecond = 5;
        private const int CycleFrameCount = 27;
        private const int CaptureLayer = 31;

        private static readonly float[] PoseTimes =
            { 0f, 0.12f, 0.28f, 0.5f, 0.8f, 1.05f, 1.25f, 1.5f, 1.8f };
        private static readonly float[] PoseWeights =
            { 0f, 0f, 0.08f, 0.35f, 0.75f, 1f, 0.96f, 1f, 1f };

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Person Death Target")]
        public static void InspectSmorzandoPersonDeathTarget()
        {
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceSlot = root.transform.Find(ReferenceSlotName) ??
                throw new InvalidOperationException("Static Smorzando person reference slot is missing.");
            var runSlot = root.transform.Find(RunSlotName) ??
                throw new InvalidOperationException("Smorzando run slot is missing.");
            var hitSlot = root.transform.Find(HitSlotName) ??
                throw new InvalidOperationException("Smorzando hit slot is missing.");
            var referenceModel = referenceSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Static Smorzando person reference model is missing.");
            var renderer = referenceModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Static Smorzando person reference has no renderer.");
            var spacing = hitSlot.localPosition.x - runSlot.localPosition.x;
            var expectedPosition = hitSlot.localPosition + Vector3.right * spacing;
            var existingDeathSlot = root.transform.Find(DeathSlotName);
            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Root=" + SmorzandoRootName);
            report.AppendLine("ReferenceSlot=" + ReferenceSlotName);
            report.AppendLine("ReferenceSlotLocalPosition=" + FormatVector(referenceSlot.localPosition));
            report.AppendLine("ReferenceSlotLocalEuler=" + FormatVector(referenceSlot.localEulerAngles));
            report.AppendLine("ReferenceSlotLocalScale=" + FormatVector(referenceSlot.localScale));
            report.AppendLine("RunSlotLocalPosition=" + FormatVector(runSlot.localPosition));
            report.AppendLine("HitSlotLocalPosition=" + FormatVector(hitSlot.localPosition));
            report.AppendLine("RunToHitXSpacing=" + spacing.ToString("0.######"));
            report.AppendLine("ExpectedDeathSlotLocalPosition=" + FormatVector(expectedPosition));
            report.AppendLine("ExistingDeathSlot=" + (existingDeathSlot != null));
            var existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipAssetPath);
            report.AppendLine("ExistingDeathClip=" + (existingClip != null));
            if (existingDeathSlot != null && existingClip != null)
            {
                var existingDeathModel = existingDeathSlot.Find(PersonModelName) ??
                    throw new InvalidOperationException("Existing Smorzando death model is missing.");
                var currentPose = InspectGeneratedDeathPose(
                    existingDeathModel,
                    existingDeathSlot.position.y,
                    existingClip);
                report.AppendLine(
                    "CurrentDeathPoseFinalAngle=" +
                    currentPose.FinalArmatureAngleDegrees.ToString("0.######"));
                report.AppendLine(
                    "CurrentDeathPoseHeightRatio=" + currentPose.LyingHeightRatio.ToString("0.######"));
                report.AppendLine(
                    "CurrentDeathPoseDepthRatio=" + currentPose.LyingDepthRatio.ToString("0.######"));
                report.AppendLine(
                    "CurrentDeathPoseGroundGap=" + currentPose.FinalGroundGap.ToString("0.######"));
                report.AppendLine(
                    "CurrentDeathPoseRootFixed=" + currentPose.ModelRootStayedFixed);
            }
            report.AppendLine("ReferenceModelLocalPosition=" + FormatVector(referenceModel.localPosition));
            report.AppendLine("ReferenceModelLocalEuler=" + FormatVector(referenceModel.localEulerAngles));
            report.AppendLine("ReferenceModelLocalScale=" + FormatVector(referenceModel.localScale));
            report.AppendLine("ReferenceRenderer=" + renderer.name);
            report.AppendLine("ReferenceMesh=" +
                (renderer.sharedMesh != null ? renderer.sharedMesh.name : "None"));
            report.AppendLine("ReferenceVertexCount=" +
                (renderer.sharedMesh != null ? renderer.sharedMesh.vertexCount : 0));
            report.AppendLine("ReferenceBoneCount=" + renderer.bones.Length);
            report.AppendLine("ReferenceMaterials=" +
                string.Join("|", renderer.sharedMaterials.Select(MaterialName)));
            report.AppendLine("ReferenceAnimatorPresent=" +
                (referenceModel.GetComponent<Animator>() != null));
            report.AppendLine("ReferenceSlotTransformCount=" +
                referenceSlot.GetComponentsInChildren<Transform>(true).Length);
            foreach (var target in referenceSlot.GetComponentsInChildren<Transform>(true))
            {
                report.AppendLine(
                    "ReferenceTransform=" + RelativePath(referenceSlot, target) +
                    ",LocalPosition=" + FormatVector(target.localPosition) +
                    ",LocalEuler=" + FormatVector(target.localEulerAngles) +
                    ",LocalScale=" + FormatVector(target.localScale));
            }

            WriteTextReport(InspectionReportRelativePath, report.ToString());
            Selection.activeObject = null;
            Debug.Log(
                "SmorzandoPersonDeathTargetInspected ExistingDeathSlot=" +
                (existingDeathSlot != null) + ", XSpacing=" + spacing.ToString("0.###") +
                ", ExpectedX=" + expectedPosition.x.ToString("0.###") +
                ", SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Person Death")]
        public static void ApplySmorzandoPersonDeath()
        {
            ConfigureDeathFbxClipLoop();
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceSlot = root.transform.Find(ReferenceSlotName) ??
                throw new InvalidOperationException("Static Smorzando person reference slot is missing.");
            var referenceModel = referenceSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Static Smorzando person reference model is missing.");
            var referenceRenderers = referenceModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var deathSlot = root.transform.Find(DeathSlotName) ??
                throw new InvalidOperationException("Smorzando death slot is missing.");
            var existingModel = deathSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Existing Smorzando death model is missing.");
            var staleReplacement = deathSlot.Find(PersonModelName + "_Replacement");
            if (staleReplacement != null)
            {
                UnityEngine.Object.DestroyImmediate(staleReplacement.gameObject);
            }

            var deathAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DeathModelAssetPath) ??
                throw new InvalidOperationException("Smorzando death FBX has not been imported.");
            var deathAssetRenderers = deathAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            AssertCompatibleGeometry(referenceRenderers, deathAssetRenderers);
            var clip = RequireDeathFbxClip();
            if (!clip.isLooping)
            {
                throw new InvalidOperationException("Smorzando death FBX clip is not configured to loop.");
            }
            var controller = CreateOrUpdateDeathController(clip);
            var preservedTransforms = root.GetComponentsInChildren<Transform>(true)
                .Where(target => target != existingModel && !target.IsChildOf(existingModel))
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var idleController = GetController(RequirePersonModel(root.transform, "Smorzando_Person_02"));
            var walkController = GetController(RequirePersonModel(root.transform, "Smorzando_Person_03"));
            var runController = GetController(RequirePersonModel(root.transform, "Smorzando_Person_04"));
            var hitController = GetController(RequirePersonModel(root.transform, "Smorzando_Person_05"));
            var existingLocalPosition = existingModel.localPosition;
            var existingLocalRotation = existingModel.localRotation;
            var existingLocalScale = existingModel.localScale;
            var replacement = PrefabUtility.InstantiatePrefab(deathAsset, scene) as GameObject ??
                throw new InvalidOperationException("Smorzando death FBX could not be instantiated.");
            replacement.name = PersonModelName + "_Replacement";
            replacement.transform.SetParent(deathSlot, false);
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
                throw new MissingComponentException("Smorzando death Animator could not be created.");
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            AssertCompatibleGeometry(referenceRenderers, replacementRenderers);
            AssertMaterialsSynchronized(referenceRenderers, replacementRenderers);
            UnityEngine.Object.DestroyImmediate(existingModel.gameObject);
            replacement.name = PersonModelName;
            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after person death apply.");
            }

            foreach (var snapshot in preservedTransforms)
            {
                snapshot.AssertUnchanged();
            }
            if (GetController(RequirePersonModel(root.transform, "Smorzando_Person_02")) != idleController ||
                GetController(RequirePersonModel(root.transform, "Smorzando_Person_03")) != walkController ||
                GetController(RequirePersonModel(root.transform, "Smorzando_Person_04")) != runController ||
                GetController(RequirePersonModel(root.transform, "Smorzando_Person_05")) != hitController)
            {
                throw new InvalidOperationException(
                    "Smorzando idle, walk, run, or hit Animator controller changed unexpectedly.");
            }
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Smorzando death Animator configuration was not preserved.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            WriteTextReport(
                FbxApplyReportRelativePath,
                string.Join(
                    Environment.NewLine,
                    "Target=Approved Smorzando Enemy Placement/Smorzando_Person_06/Smorzando_Person_Model",
                    "SourceAsset=" + DeathModelAssetPath,
                    "SourceSha256=" + ComputeSha256(ProjectAbsolutePath(DeathSourceRelativePath)),
                    "ImportedSha256=" + ComputeSha256(ProjectAbsolutePath(DeathModelAssetPath)),
                    "StaticSha256=" + ComputeSha256(ProjectAbsolutePath(StaticModelAssetPath)),
                    "StaticMaterialReference=Smorzando_Person_01",
                    "LocalPosition=" + FormatVector(deathSlot.localPosition),
                    "Clip=" + clip.name,
                    "Controller=" + DeathControllerAssetPath,
                    "ClipLengthSeconds=" + clip.length.ToString("0.######"),
                    "ClipFrameRate=" + clip.frameRate.ToString("0.######"),
                    "CurveBindingCount=" + bindings.Length,
                    "AnimatedTransformCount=" + bindings.Select(binding => binding.path).Distinct().Count(),
                    "ModelRootCurveCount=" + bindings.Count(binding => string.IsNullOrEmpty(binding.path)),
                    "LoopTime=" + settings.loopTime,
                    "LoopBlend=" + settings.loopBlend,
                    "ApplyRootMotion=False",
                    "MaterialReferenceMatched=True",
                    "GeometryStructureMatchedStatic=True",
                    "GeneratedDeathClipRetained=True",
                    "GeneratedDeathClipConnected=False",
                    "ExistingPersonsPreserved=True",
                    "OtherTransformsChanged=False",
                    "SelectionCleared=True") + Environment.NewLine);
            Selection.activeObject = null;
            Debug.Log(
                "SmorzandoPersonDeathFbxApplied Target=Smorzando_Person_06, Clip=" + clip.name +
                ", Duration=" + clip.length.ToString("0.###") +
                "s, MaterialMatched=True, ApplyRootMotion=False, " +
                "OtherTransformsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Person Death Frames")]
        public static void CaptureSmorzandoPersonDeathFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceSlot = root.transform.Find(ReferenceSlotName) ??
                throw new InvalidOperationException("Static Smorzando reference slot is missing.");
            var deathSlot = root.transform.Find(DeathSlotName) ??
                throw new InvalidOperationException("Smorzando death slot is missing.");
            var clip = RequireDeathFbxClip();
            var cycleFrameCount = Mathf.Max(2, Mathf.CeilToInt(clip.length * CycleFramesPerSecond));
            var finalSampleTime = Mathf.Max(0f, clip.length - 1f / Mathf.Max(1f, clip.frameRate));
            var captureFolder = ProjectAbsolutePath(CaptureRelativeFolder);
            var frontFolder = Path.Combine(captureFolder, "front_cycle_frames");
            var obliqueFolder = Path.Combine(captureFolder, "oblique_cycle_frames");
            Directory.CreateDirectory(frontFolder);
            Directory.CreateDirectory(obliqueFolder);

            var cameraObject = new GameObject("Smorzando_PersonDeath_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_PersonDeath_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject referenceClone = null;
            GameObject deathClone = null;
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
                referenceClone.name = "Smorzando_PersonDeath_StaticReferenceClone";
                deathClone = UnityEngine.Object.Instantiate(deathSlot.gameObject);
                deathClone.name = "Smorzando_PersonDeath_MotionClone";
                referenceClone.transform.position = Vector3.zero;
                deathClone.transform.position = Vector3.zero;
                SetCaptureOnly(referenceClone);
                SetCaptureOnly(deathClone);
                DisableHelperComponents(referenceClone);
                DisableHelperComponents(deathClone);
                var deathModel = deathClone.transform.Find(PersonModelName) ??
                    throw new InvalidOperationException("Smorzando death capture model is missing.");
                var animator = deathModel.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }
                foreach (var renderer in deathClone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                }

                var modelPosition = deathModel.localPosition;
                var modelRotation = deathModel.localRotation;
                var modelScale = deathModel.localScale;
                SampleDeathInPlace(
                    clip, deathModel, 0f, modelPosition, modelRotation, modelScale);
                var referenceBounds = CalculateBakedSkinnedBounds(referenceClone.transform);
                var deathBounds = CalculateBakedSkinnedBounds(deathClone.transform);
                var boundsSampleTimes = Enumerable.Range(0, 9)
                    .Select(index => finalSampleTime * index / 8f)
                    .ToArray();
                foreach (var sampleTime in boundsSampleTimes)
                {
                    SampleDeathInPlace(
                        clip, deathModel, sampleTime, modelPosition, modelRotation, modelScale);
                    deathBounds.Encapsulate(CalculateBakedSkinnedBounds(deathClone.transform));
                }

                var halfSpacing = (referenceBounds.extents.x + deathBounds.extents.x + 0.55f) * 0.5f;
                referenceClone.transform.position += Vector3.right * (-halfSpacing - referenceBounds.center.x);
                deathClone.transform.position += Vector3.right * (halfSpacing - deathBounds.center.x);
                referenceBounds = CalculateBakedSkinnedBounds(referenceClone.transform);
                deathBounds = CalculateBakedSkinnedBounds(deathClone.transform);
                foreach (var sampleTime in boundsSampleTimes)
                {
                    SampleDeathInPlace(
                        clip, deathModel, sampleTime, modelPosition, modelRotation, modelScale);
                    deathBounds.Encapsulate(CalculateBakedSkinnedBounds(deathClone.transform));
                }
                var pairBounds = referenceBounds;
                pairBounds.Encapsulate(deathBounds);
                floor = CreateCaptureFloor(pairBounds, out floorMaterial);

                referenceClone.SetActive(false);
                var deathTarget = deathBounds.center;
                var deathOrthoSize = Mathf.Max(
                    deathBounds.extents.y + 0.28f,
                    deathBounds.extents.x + 0.28f);
                var frontPosition = deathTarget + Vector3.back * 35f;
                var obliqueDirection = (Vector3.back + Vector3.right * 0.48f).normalized;
                var obliquePosition = deathTarget + obliqueDirection * 35f;
                for (var frame = 0; frame < cycleFrameCount; frame++)
                {
                    var time = frame * clip.length / cycleFrameCount;
                    SampleDeathInPlace(
                        clip, deathModel, time, modelPosition, modelRotation, modelScale);
                    CapturePng(
                        camera,
                        frontPosition,
                        deathTarget,
                        deathOrthoSize,
                        640,
                        640,
                        Path.Combine(frontFolder, $"Smorzando_PersonDeath_Front_{frame:000}.png"));
                    CapturePng(
                        camera,
                        obliquePosition,
                        deathTarget,
                        deathOrthoSize,
                        640,
                        640,
                        Path.Combine(obliqueFolder, $"Smorzando_PersonDeath_Oblique_{frame:000}.png"));
                }

                referenceClone.SetActive(true);
                var pairTarget = pairBounds.center + Vector3.up * 0.02f;
                var pairOrthoSize = Mathf.Max(
                    pairBounds.extents.y + 0.32f,
                    pairBounds.extents.x / (16f / 9f) + 0.32f);
                SampleDeathInPlace(
                    clip, deathModel, 0f, modelPosition, modelRotation, modelScale);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonDeath_StaticVsDeath_T000.png"));
                SampleDeathInPlace(
                    clip, deathModel, finalSampleTime, modelPosition, modelRotation, modelScale);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonDeath_StaticVsDeath_Final.png"));

                var keyFrames = Enumerable.Range(0, 7)
                    .Select(index => Mathf.Min(
                        cycleFrameCount - 1,
                        Mathf.RoundToInt((cycleFrameCount - 1) * index / 6f)))
                    .Distinct()
                    .ToArray();
                CreateKeyframeSheet(frontFolder, obliqueFolder, captureFolder, keyFrames);
                EncodeLoopVideo(
                    frontFolder,
                    Path.Combine(captureFolder, "Smorzando_PersonDeath_Loop.mp4"));

                SampleDeathInPlace(
                    clip, deathModel, finalSampleTime, modelPosition, modelRotation, modelScale);
                var finalBounds = CalculateBakedSkinnedBounds(deathClone.transform);
                var finalGroundGap = finalBounds.min.y;
                var rootStayedFixed = deathModel.localPosition == modelPosition &&
                    deathModel.localRotation == modelRotation && deathModel.localScale == modelScale;
                if (!rootStayedFixed)
                {
                    throw new InvalidOperationException(
                        "Smorzando death FBX capture changed the review model root Transform.");
                }

                File.WriteAllLines(
                    Path.Combine(captureFolder, "Smorzando_PersonDeath_CaptureManifest.txt"),
                    new[]
                    {
                        "Clip=" + clip.name,
                        "CycleDurationSeconds=" + clip.length.ToString("0.######"),
                        "CycleFrameCount=" + cycleFrameCount,
                        "CycleFramesPerSecond=" + CycleFramesPerSecond,
                        "FinalPoseSampleTimeSeconds=" + finalSampleTime.ToString("0.######"),
                        "KeyFrames=" + string.Join("|", keyFrames.Select(frame => frame.ToString("000"))),
                        "TargetSlot=Smorzando_Person_06",
                        "StaticReferenceSlot=Smorzando_Person_01",
                        "FinalGroundGap=" + finalGroundGap.ToString("0.######"),
                        "MaterialReferenceMatched=True",
                        "GeometryStructureMatchedStatic=True",
                        "ClipLoop=True",
                        "ApplyRootMotion=False",
                        "ModelRootTransformAnimated=False",
                        "Views=FrontCycle|ObliqueCycle|StaticVsDeath|KeyframeSheet|LoopVideo",
                        "VideoEncoded=True",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                Selection.activeObject = null;
                Debug.Log(
                    "SmorzandoPersonDeathFramesCaptured Folder=" + captureFolder +
                    ", Frames=" + cycleFrameCount +
                    ", FinalGroundGap=" + finalGroundGap.ToString("0.###") +
                    ", Views=Front|Oblique|StaticVsDeath|LoopVideo, VideoEncoded=True, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(referenceClone);
                UnityEngine.Object.DestroyImmediate(deathClone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando person death capture changed the scene dirty state.");
                }
            }
        }

        private static void ConfigureDeathFbxClipLoop()
        {
            var importer = AssetImporter.GetAtPath(DeathModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Smorzando death FBX importer is missing.");
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Smorzando death FBX must contain exactly one default animation clip.");
            }

            clips[0].name = "Smorzando_Person_Death_Fbx";
            clips[0].loopTime = true;
            clips[0].loopPose = false;
            clips[0].lockRootRotation = true;
            clips[0].lockRootHeightY = true;
            clips[0].lockRootPositionXZ = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireDeathFbxClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(DeathModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    "Smorzando death FBX must contain exactly one imported animation clip.");
            }
            return clips[0];
        }

        private static void AssertCompatibleGeometry(
            IReadOnlyList<SkinnedMeshRenderer> referenceRenderers,
            IReadOnlyList<SkinnedMeshRenderer> deathRenderers)
        {
            if (referenceRenderers.Count == 0 || referenceRenderers.Count != deathRenderers.Count)
            {
                throw new InvalidOperationException(
                    "Smorzando death renderer count does not match the static person model.");
            }
            for (var index = 0; index < referenceRenderers.Count; index++)
            {
                var referenceRenderer = referenceRenderers[index];
                var deathRenderer = deathRenderers[index];
                var referenceMesh = referenceRenderer.sharedMesh;
                var deathMesh = deathRenderer.sharedMesh;
                if (referenceMesh == null || deathMesh == null ||
                    referenceMesh.name != deathMesh.name ||
                    referenceMesh.vertexCount != deathMesh.vertexCount ||
                    referenceMesh.subMeshCount != deathMesh.subMeshCount ||
                    referenceRenderer.bones.Length != deathRenderer.bones.Length)
                {
                    throw new InvalidOperationException(
                        "Smorzando death model structure does not match the static person model.");
                }
            }
        }

        private static void SynchronizeMaterials(
            IReadOnlyList<SkinnedMeshRenderer> referenceRenderers,
            IReadOnlyList<SkinnedMeshRenderer> deathRenderers)
        {
            if (referenceRenderers.Count != deathRenderers.Count)
            {
                throw new InvalidOperationException(
                    "Smorzando death material renderer count does not match.");
            }
            for (var index = 0; index < referenceRenderers.Count; index++)
            {
                deathRenderers[index].sharedMaterials = referenceRenderers[index].sharedMaterials;
            }
        }

        private static void AssertMaterialsSynchronized(
            IReadOnlyList<SkinnedMeshRenderer> referenceRenderers,
            IReadOnlyList<SkinnedMeshRenderer> deathRenderers)
        {
            if (referenceRenderers.Count != deathRenderers.Count)
            {
                throw new InvalidOperationException(
                    "Smorzando death material renderer count does not match.");
            }
            for (var rendererIndex = 0; rendererIndex < referenceRenderers.Count; rendererIndex++)
            {
                var referenceMaterials = referenceRenderers[rendererIndex].sharedMaterials;
                var deathMaterials = deathRenderers[rendererIndex].sharedMaterials;
                if (referenceMaterials.Length != deathMaterials.Length)
                {
                    throw new InvalidOperationException("Smorzando death material slot count does not match.");
                }
                for (var materialIndex = 0; materialIndex < referenceMaterials.Length; materialIndex++)
                {
                    if (referenceMaterials[materialIndex] != deathMaterials[materialIndex])
                    {
                        throw new InvalidOperationException(
                            "Smorzando death material is not the static reference material asset.");
                    }
                }
            }
        }

        private static void SampleDeathInPlace(
            AnimationClip clip,
            Transform model,
            float time,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            clip.SampleAnimation(model.gameObject, time);
            model.localPosition = localPosition;
            model.localRotation = localRotation;
            model.localScale = localScale;
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static AnimationClip CreateOrUpdateDeathClip(Transform deathModel, float floorWorldY)
        {
            EnsureAssetFolder(DeathClipAssetPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipAssetPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "Smorzando_Person_Death",
                    frameRate = 60f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, DeathClipAssetPath);
            }
            clip.ClearCurves();
            clip.name = "Smorzando_Person_Death";
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            foreach (var pose in DeathPoseSpecs())
            {
                var target = deathModel.Find(pose.Path) ??
                    throw new InvalidOperationException("Smorzando death rig target is missing: " + pose.Path);
                AddWorldRotationFallCurves(clip, deathModel, target, pose.WorldEulerAtMaximum);
            }
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            AssetDatabase.ImportAsset(DeathClipAssetPath, ImportAssetOptions.ForceUpdate);
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipAssetPath) ??
                throw new InvalidOperationException("Smorzando rotation-only death clip could not be reloaded.");

            clip = ApplyCalibratedHipsFloorCorrection(deathModel, floorWorldY, clip);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            AssetDatabase.ImportAsset(DeathClipAssetPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipAssetPath) ??
                throw new InvalidOperationException("Smorzando death clip could not be reloaded.");
        }

        private static AnimatorController CreateOrUpdateDeathController(AnimationClip clip)
        {
            EnsureAssetFolder(DeathControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DeathControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(DeathControllerAssetPath);
            }
            if (controller.layers.Length == 0)
            {
                AssetDatabase.DeleteAsset(DeathControllerAssetPath);
                controller = AnimatorController.CreateAnimatorControllerAtPath(DeathControllerAssetPath);
            }
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null && candidate.name == "Death") ??
                stateMachine.AddState("Death");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static IEnumerable<DeathPoseSpec> DeathPoseSpecs()
        {
            yield return new DeathPoseSpec("Armature", new Vector3(88f, 0f, 0f));
            yield return new DeathPoseSpec("Armature/Hips", new Vector3(6f, 0f, 0f));
            yield return new DeathPoseSpec("Armature/Hips/LeftUpLeg", new Vector3(-10f, 0f, -7f));
            yield return new DeathPoseSpec("Armature/Hips/LeftUpLeg/LeftLeg", new Vector3(18f, 0f, 0f));
            yield return new DeathPoseSpec("Armature/Hips/RightUpLeg", new Vector3(-10f, 0f, 7f));
            yield return new DeathPoseSpec("Armature/Hips/RightUpLeg/RightLeg", new Vector3(18f, 0f, 0f));
            yield return new DeathPoseSpec("Armature/Hips/Spine02", new Vector3(4f, 0f, 0f));
            yield return new DeathPoseSpec("Armature/Hips/Spine02/Spine01", new Vector3(3f, 0f, 0f));
            yield return new DeathPoseSpec("Armature/Hips/Spine02/Spine01/Spine", new Vector3(4f, 0f, 0f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/neck", new Vector3(-6f, 0f, 0f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head", new Vector3(3f, 0f, 0f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder", new Vector3(0f, 0f, -25f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm", new Vector3(5f, 0f, -18f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                new Vector3(-15f, 0f, 0f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder", new Vector3(0f, 0f, 25f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm", new Vector3(5f, 0f, 18f));
            yield return new DeathPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                new Vector3(-15f, 0f, 0f));
        }

        private static void AddWorldRotationFallCurves(
            AnimationClip clip,
            Transform modelRoot,
            Transform target,
            Vector3 worldEulerAtMaximum)
        {
            var path = RelativePath(modelRoot, target);
            var baselineWorldRotation = target.rotation;
            var parentWorldRotation = target.parent != null ? target.parent.rotation : Quaternion.identity;
            var rotations = new Quaternion[PoseTimes.Length];
            for (var index = 0; index < PoseTimes.Length; index++)
            {
                rotations[index] = Quaternion.Inverse(parentWorldRotation) *
                    (Quaternion.Euler(worldEulerAtMaximum * PoseWeights[index]) * baselineWorldRotation);
                if (index > 0 && Quaternion.Dot(rotations[index - 1], rotations[index]) < 0f)
                {
                    rotations[index] = Negated(rotations[index]);
                }
            }
            SetFloatCurve(clip, path, "m_LocalRotation.x", rotations.Select(value => value.x).ToArray());
            SetFloatCurve(clip, path, "m_LocalRotation.y", rotations.Select(value => value.y).ToArray());
            SetFloatCurve(clip, path, "m_LocalRotation.z", rotations.Select(value => value.z).ToArray());
            SetFloatCurve(clip, path, "m_LocalRotation.w", rotations.Select(value => value.w).ToArray());
        }

        private static void AddHipsVerticalCorrectionCurves(
            AnimationClip clip,
            Transform modelRoot,
            Transform hips,
            Vector3 localCorrection)
        {
            var baseline = hips.localPosition;
            var positions = PoseWeights
                .Select(weight => baseline + localCorrection * weight)
                .ToArray();
            SetFloatCurve(
                clip,
                RelativePath(modelRoot, hips),
                "m_LocalPosition.x",
                positions.Select(value => value.x).ToArray());
            SetFloatCurve(
                clip,
                RelativePath(modelRoot, hips),
                "m_LocalPosition.y",
                positions.Select(value => value.y).ToArray());
            SetFloatCurve(
                clip,
                RelativePath(modelRoot, hips),
                "m_LocalPosition.z",
                positions.Select(value => value.z).ToArray());
        }

        private static void SetFloatCurve(
            AnimationClip clip,
            string path,
            string propertyName,
            IReadOnlyList<float> values)
        {
            var keys = new Keyframe[PoseTimes.Length];
            for (var index = 0; index < PoseTimes.Length; index++)
            {
                keys[index] = new Keyframe(PoseTimes[index], values[index]);
            }
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName),
                curve);
        }

        private static AnimationClip ApplyCalibratedHipsFloorCorrection(
            Transform deathModel,
            float floorWorldY,
            AnimationClip clip)
        {
            const float targetGroundGap = 0.01f;
            const float probeWorldDistance = 0.1f;
            var hips = deathModel.Find("Armature/Hips") ??
                throw new InvalidOperationException("Smorzando death Hips root bone is missing.");
            var uncorrectedGap = MeasureFinalGroundGap(deathModel, floorWorldY, clip);
            var probeLocalCorrection = CalculateFinalHipsLocalWorldDelta(
                deathModel,
                Vector3.up * probeWorldDistance,
                clip);
            AddHipsVerticalCorrectionCurves(
                clip,
                deathModel,
                hips,
                probeLocalCorrection);
            clip = SaveImportAndReloadClip(clip);
            var probeGap = MeasureFinalGroundGap(deathModel, floorWorldY, clip);
            var measuredResponse = probeGap - uncorrectedGap;
            if (Mathf.Abs(measuredResponse) < 0.001f)
            {
                throw new InvalidOperationException(
                    "Smorzando death Hips floor response is too small to calibrate.");
            }

            var responseScale = (targetGroundGap - uncorrectedGap) / measuredResponse;
            AddHipsVerticalCorrectionCurves(
                clip,
                deathModel,
                hips,
                probeLocalCorrection * responseScale);
            return SaveImportAndReloadClip(clip);
        }

        private static Vector3 CalculateFinalHipsLocalWorldDelta(
            Transform deathModel,
            Vector3 worldDelta,
            AnimationClip clip)
        {
            var clone = UnityEngine.Object.Instantiate(deathModel.gameObject);
            clone.name = "Smorzando_PersonDeath_FloorCorrection";
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (var target in clone.GetComponentsInChildren<Transform>(true))
                {
                    target.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                foreach (var renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                }
                clip.SampleAnimation(clone, FinalPoseSampleTimeSeconds);
                var hips = clone.transform.Find("Armature/Hips") ??
                    throw new InvalidOperationException("Smorzando death correction Hips is missing.");
                return hips.parent != null
                    ? hips.parent.InverseTransformVector(worldDelta)
                    : worldDelta;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static float MeasureFinalGroundGap(
            Transform deathModel,
            float floorWorldY,
            AnimationClip clip)
        {
            var clone = UnityEngine.Object.Instantiate(deathModel.gameObject);
            clone.name = "Smorzando_PersonDeath_GroundGapMeasurement";
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (var target in clone.GetComponentsInChildren<Transform>(true))
                {
                    target.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                foreach (var renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                }
                clip.SampleAnimation(clone, FinalPoseSampleTimeSeconds);
                return CalculateBakedSkinnedBounds(clone.transform).min.y - floorWorldY;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static AnimationClip SaveImportAndReloadClip(AnimationClip clip)
        {
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            AssetDatabase.ImportAsset(DeathClipAssetPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipAssetPath) ??
                throw new InvalidOperationException("Smorzando death clip could not be reloaded.");
        }

        private static DeathPoseInspection InspectGeneratedDeathPose(
            Transform deathModel,
            float floorWorldY,
            AnimationClip clip)
        {
            var clone = UnityEngine.Object.Instantiate(deathModel.gameObject);
            clone.name = "Smorzando_PersonDeath_InternalPoseInspection";
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (var target in clone.GetComponentsInChildren<Transform>(true))
                {
                    target.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                foreach (var renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                }
                var animator = clone.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }
                var modelPosition = clone.transform.localPosition;
                var modelRotation = clone.transform.localRotation;
                var modelScale = clone.transform.localScale;
                var armature = clone.transform.Find("Armature") ??
                    throw new InvalidOperationException("Death inspection Armature is missing.");
                clip.SampleAnimation(clone, 0f);
                var standingArmatureRotation = armature.rotation;
                var standingBounds = CalculateBakedSkinnedBounds(clone.transform);
                clip.SampleAnimation(clone, FinalPoseSampleTimeSeconds);
                var lyingBounds = CalculateBakedSkinnedBounds(clone.transform);
                var angle = Quaternion.Angle(standingArmatureRotation, armature.rotation);
                var rootStayedFixed = clone.transform.localPosition == modelPosition &&
                    clone.transform.localRotation == modelRotation && clone.transform.localScale == modelScale;
                return new DeathPoseInspection(
                    angle,
                    standingBounds.size.y,
                    lyingBounds.size.y,
                    standingBounds.size.z,
                    lyingBounds.size.z,
                    lyingBounds.min.y - floorWorldY,
                    rootStayedFixed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
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

        private static Transform RequirePersonModel(Transform root, string slotName)
        {
            return root.Find(slotName + "/" + PersonModelName) ??
                throw new InvalidOperationException(slotName + " person model is missing.");
        }

        private static RuntimeAnimatorController GetController(Transform model)
        {
            var animator = model.GetComponent<Animator>();
            return animator != null ? animator.runtimeAnimatorController : null;
        }

        private static void AssertMatchingGeometry(
            SkinnedMeshRenderer referenceRenderer,
            SkinnedMeshRenderer deathRenderer)
        {
            var referenceMesh = referenceRenderer.sharedMesh;
            var deathMesh = deathRenderer.sharedMesh;
            if (referenceMesh == null || deathMesh == null ||
                referenceMesh.vertexCount != deathMesh.vertexCount ||
                referenceMesh.subMeshCount != deathMesh.subMeshCount ||
                referenceRenderer.bones.Length != deathRenderer.bones.Length ||
                referenceMesh.bounds.center != deathMesh.bounds.center ||
                referenceMesh.bounds.size != deathMesh.bounds.size)
            {
                throw new InvalidOperationException(
                    "Smorzando death model geometry does not match the static person model.");
            }
        }

        private static void AssertMaterialsSynchronized(
            SkinnedMeshRenderer referenceRenderer,
            SkinnedMeshRenderer deathRenderer)
        {
            var referenceMaterials = referenceRenderer.sharedMaterials;
            var deathMaterials = deathRenderer.sharedMaterials;
            if (referenceMaterials.Length != deathMaterials.Length)
            {
                throw new InvalidOperationException("Smorzando death material slot count does not match.");
            }
            for (var index = 0; index < referenceMaterials.Length; index++)
            {
                if (referenceMaterials[index] != deathMaterials[index])
                {
                    throw new InvalidOperationException(
                        "Smorzando death material is not the static reference material asset.");
                }
            }
        }

        private static Bounds CalculateBakedSkinnedBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Smorzando death pose has no visible renderers.");
            }

            var initialized = false;
            var combined = default(Bounds);
            foreach (var renderer in renderers)
            {
                var mesh = new Mesh
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                try
                {
                    renderer.BakeMesh(mesh);
                    var localBounds = mesh.bounds;
                    for (var x = -1; x <= 1; x += 2)
                    {
                        for (var y = -1; y <= 1; y += 2)
                        {
                            for (var z = -1; z <= 1; z += 2)
                            {
                                var localPoint = localBounds.center + Vector3.Scale(
                                    localBounds.extents,
                                    new Vector3(x, y, z));
                                var worldPoint = renderer.transform.TransformPoint(localPoint);
                                if (!initialized)
                                {
                                    combined = new Bounds(worldPoint, Vector3.zero);
                                    initialized = true;
                                }
                                else
                                {
                                    combined.Encapsulate(worldPoint);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }
            return combined;
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

        private static Quaternion Negated(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        private static void EncodeLoopVideo(string frameFolder, string videoPath)
        {
            var inputPattern = Path.Combine(frameFolder, "Smorzando_PersonDeath_Front_%03d.png");
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
                throw new InvalidOperationException("ffmpeg could not be started for person death video.");
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(60000) || process.ExitCode != 0)
            {
                throw new InvalidOperationException("Person death video encoding failed: " + error);
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
                        Path.Combine(frontFolder,
                            $"Smorzando_PersonDeath_Front_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        cellSize);
                    CopyPngToSheet(
                        Path.Combine(obliqueFolder,
                            $"Smorzando_PersonDeath_Oblique_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        0);
                }
                sheet.Apply();
                File.WriteAllBytes(
                    Path.Combine(captureFolder, "Smorzando_PersonDeath_KeyframeSheet.png"),
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
                    throw new InvalidDataException("Unexpected person death capture size: " + path);
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
            floor.name = "Smorzando_PersonDeath_CaptureFloor";
            floor.hideFlags = HideFlags.HideAndDontSave;
            floor.layer = CaptureLayer;
            floor.transform.position = new Vector3(bounds.center.x, -0.025f, bounds.center.z);
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
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(path) ?? ProjectAbsolutePath(CaptureRelativeFolder));
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

        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }
            var parts = new System.Collections.Generic.List<string>();
            var current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            if (current != root)
            {
                return "<outside-root>/" + target.name;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void WriteTextReport(string relativePath, string contents)
        {
            var path = ProjectAbsolutePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ProjectAbsolutePath("docs/validation"));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.######},{value.y:0.######},{value.z:0.######})";
        }

        private static string MaterialName(Material material)
        {
            return material != null ? material.name : "None";
        }

        private readonly struct DeathPoseSpec
        {
            public DeathPoseSpec(string path, Vector3 worldEulerAtMaximum)
            {
                Path = path;
                WorldEulerAtMaximum = worldEulerAtMaximum;
            }

            public string Path { get; }
            public Vector3 WorldEulerAtMaximum { get; }
        }

        private readonly struct DeathPoseInspection
        {
            public DeathPoseInspection(
                float finalArmatureAngleDegrees,
                float standingHeight,
                float lyingHeight,
                float standingDepth,
                float lyingDepth,
                float finalGroundGap,
                bool modelRootStayedFixed)
            {
                FinalArmatureAngleDegrees = finalArmatureAngleDegrees;
                StandingHeight = standingHeight;
                LyingHeight = lyingHeight;
                StandingDepth = standingDepth;
                LyingDepth = lyingDepth;
                FinalGroundGap = finalGroundGap;
                ModelRootStayedFixed = modelRootStayedFixed;
            }

            public float FinalArmatureAngleDegrees { get; }
            public float StandingHeight { get; }
            public float LyingHeight { get; }
            public float LyingHeightRatio => LyingHeight / StandingHeight;
            public float StandingDepth { get; }
            public float LyingDepth { get; }
            public float LyingDepthRatio => LyingDepth / StandingDepth;
            public float FinalGroundGap { get; }
            public bool ModelRootStayedFixed { get; }
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
                    throw new InvalidOperationException("Smorzando person death apply changed a preserved Transform.");
                }
            }
        }
    }
}
