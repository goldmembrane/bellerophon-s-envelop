using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Bellerophon.Editor.SmorzandoCargoRunScene
{
    internal static class SmorzandoPersonHitApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string HitSlotName = "Smorzando_Person_05";
        private const string PersonModelName = "Smorzando_Person_Model";
        private const string HitClipAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Hit.anim";
        private const string HitControllerAssetPath =
            "Assets/_Project/Art/Enemies/Smorzando/Animations/Smorzando_Person_Hit.controller";
        private const string ValidationRelativeFolder =
            "docs/validation/smorzando_person_hit_2026-07-18";
        private const string InspectionReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonHitTargetInspection.txt";
        private const string ApplyReportRelativePath =
            ValidationRelativeFolder + "/Smorzando_PersonHitApply.txt";
        private const string CaptureRelativeFolder =
            ValidationRelativeFolder + "/automated_visual_capture";
        private const float CycleDurationSeconds = 0.75f;
        private const float MaximumRecoilTimeSeconds = 0.14f;
        private const int CycleFramesPerSecond = 20;
        private const int CycleFrameCount = 15;
        private const int CaptureLayer = 31;

        private static readonly float[] PoseTimes = { 0f, 0.05f, 0.14f, 0.24f, 0.42f, 0.58f, 0.75f };
        private static readonly float[] PoseWeights = { 0f, 0.35f, 1f, 0.72f, 0.22f, 0f, 0f };

        [MenuItem("Bellerophon/Enemies/Smorzando/Inspect Person Hit Target")]
        public static void InspectSmorzandoPersonHitTarget()
        {
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var hitSlot = root.transform.Find(HitSlotName) ??
                throw new InvalidOperationException("Fifth Smorzando person slot is missing.");
            var hitModel = hitSlot.Find(PersonModelName) ??
                throw new InvalidOperationException("Fifth Smorzando person model is missing.");
            var renderer = hitModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Fifth Smorzando person has no SkinnedMeshRenderer.");
            var animator = hitModel.GetComponent<Animator>();
            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Target=" + SmorzandoRootName + "/" + HitSlotName + "/" + PersonModelName);
            report.AppendLine("SlotLocalPosition=" + FormatVector(hitSlot.localPosition));
            report.AppendLine("SlotLocalEuler=" + FormatVector(hitSlot.localEulerAngles));
            report.AppendLine("SlotLocalScale=" + FormatVector(hitSlot.localScale));
            report.AppendLine("ModelLocalPosition=" + FormatVector(hitModel.localPosition));
            report.AppendLine("ModelLocalEuler=" + FormatVector(hitModel.localEulerAngles));
            report.AppendLine("ModelLocalScale=" + FormatVector(hitModel.localScale));
            report.AppendLine("AnimatorPresent=" + (animator != null));
            report.AppendLine("AnimatorController=" +
                (animator != null && animator.runtimeAnimatorController != null
                    ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : "None"));
            report.AppendLine("ApplyRootMotion=" + (animator != null && animator.applyRootMotion));
            report.AppendLine("Renderer=" + renderer.name);
            report.AppendLine("Mesh=" + (renderer.sharedMesh != null ? renderer.sharedMesh.name : "None"));
            report.AppendLine("VertexCount=" +
                (renderer.sharedMesh != null ? renderer.sharedMesh.vertexCount : 0));
            report.AppendLine("BoneCount=" + renderer.bones.Length);
            report.AppendLine("RootBone=" +
                (renderer.rootBone != null ? RelativePath(hitModel, renderer.rootBone) : "None"));
            report.AppendLine("Materials=" +
                string.Join("|", renderer.sharedMaterials.Select(MaterialName)));
            report.AppendLine("TransformCount=" + hitModel.GetComponentsInChildren<Transform>(true).Length);
            foreach (var target in hitModel.GetComponentsInChildren<Transform>(true))
            {
                report.AppendLine(
                    "Transform=" + RelativePath(hitModel, target) +
                    ",LocalPosition=" + FormatVector(target.localPosition) +
                    ",LocalEuler=" + FormatVector(target.localEulerAngles) +
                    ",LocalScale=" + FormatVector(target.localScale));
            }

            WriteTextReport(InspectionReportRelativePath, report.ToString());
            Selection.activeObject = null;
            Debug.Log(
                "SmorzandoPersonHitTargetInspected Target=Smorzando_Person_05, " +
                "Transforms=" + hitModel.GetComponentsInChildren<Transform>(true).Length +
                ", Bones=" + renderer.bones.Length + ", SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Apply Person Hit")]
        public static void ApplySmorzandoPersonHit()
        {
            var scene = RequireOpenCargoRunScene();
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceModel = RequirePersonModel(root.transform, "Smorzando_Person_01");
            var idleModel = RequirePersonModel(root.transform, "Smorzando_Person_02");
            var walkModel = RequirePersonModel(root.transform, "Smorzando_Person_03");
            var runModel = RequirePersonModel(root.transform, "Smorzando_Person_04");
            var hitModel = RequirePersonModel(root.transform, HitSlotName);
            var referenceRenderer = referenceModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Static Smorzando reference has no SkinnedMeshRenderer.");
            var hitRenderer = hitModel.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("Fifth Smorzando person has no SkinnedMeshRenderer.");
            AssertMatchingGeometry(referenceRenderer, hitRenderer);
            AssertMaterialsSynchronized(referenceRenderer, hitRenderer);

            var preservedTransforms = root.GetComponentsInChildren<Transform>(true)
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var idleController = GetController(idleModel);
            var walkController = GetController(walkModel);
            var runController = GetController(runModel);
            var clip = CreateOrUpdateHitClip(hitModel);
            var controller = CreateOrUpdateHitController(clip);
            var animator = hitModel.GetComponent<Animator>();
            if (animator == null)
            {
                animator = hitModel.gameObject.AddComponent<Animator>();
            }
            if (animator == null)
            {
                throw new MissingComponentException("Smorzando hit Animator could not be created.");
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after person hit apply.");
            }

            foreach (var snapshot in preservedTransforms)
            {
                snapshot.AssertUnchanged();
            }
            if (GetController(idleModel) != idleController || GetController(walkModel) != walkController ||
                GetController(runModel) != runController)
            {
                throw new InvalidOperationException(
                    "Smorzando idle, walk, or run Animator controller changed unexpectedly.");
            }
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Smorzando hit Animator configuration was not preserved.");
            }

            var poseResult = InspectGeneratedHitPose(hitModel, clip);
            if (poseResult.MaximumSpineAngleDegrees < 12f)
            {
                throw new InvalidOperationException("Smorzando hit recoil is not large enough to read as a hit.");
            }
            if (!poseResult.ReturnedToStanding || !poseResult.ModelRootStayedFixed)
            {
                throw new InvalidOperationException(
                    "Smorzando hit motion did not return to standing with a fixed model root.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            WriteTextReport(
                ApplyReportRelativePath,
                string.Join(
                    Environment.NewLine,
                    "Target=Approved Smorzando Enemy Placement/Smorzando_Person_05/Smorzando_Person_Model",
                    "ReferenceStatic=Smorzando_Person_01",
                    "IdlePreserved=Smorzando_Person_02",
                    "WalkPreserved=Smorzando_Person_03",
                    "RunPreserved=Smorzando_Person_04",
                    "Clip=" + HitClipAssetPath,
                    "Controller=" + HitControllerAssetPath,
                    "CycleDurationSeconds=0.75",
                    "MaximumRecoilTimeSeconds=0.14",
                    "MaximumSpineAngleDegrees=" + poseResult.MaximumSpineAngleDegrees.ToString("0.######"),
                    "CurveBindingCount=" + bindings.Length,
                    "AnimatedTransformCount=" + bindings.Select(binding => binding.path).Distinct().Count(),
                    "ModelRootCurveCount=" + bindings.Count(binding => string.IsNullOrEmpty(binding.path)),
                    "LoopTime=True",
                    "LoopBlend=True",
                    "ApplyRootMotion=False",
                    "ReturnedToStanding=" + poseResult.ReturnedToStanding,
                    "ModelRootStayedFixed=" + poseResult.ModelRootStayedFixed,
                    "MaterialReferenceMatched=True",
                    "GeometryMatchedStatic=True",
                    "IdleControllerPreserved=True",
                    "WalkControllerPreserved=True",
                    "RunControllerPreserved=True",
                    "OtherTransformsChanged=False",
                    "SelectionCleared=True") + Environment.NewLine);
            Selection.activeObject = null;
            Debug.Log(
                "SmorzandoPersonHitApplied Target=Smorzando_Person_05, Cycle=0.75s, " +
                "MaximumSpineAngle=" + poseResult.MaximumSpineAngleDegrees.ToString("0.###") +
                ", ReturnedToStanding=True, ApplyRootMotion=False, OtherTransformsChanged=False, " +
                "SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Smorzando/Capture Person Hit Frames")]
        public static void CaptureSmorzandoPersonHitFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequireRoot(scene, SmorzandoRootName);
            var referenceSlot = root.transform.Find("Smorzando_Person_01") ??
                throw new InvalidOperationException("Static Smorzando reference slot is missing.");
            var hitSlot = root.transform.Find(HitSlotName) ??
                throw new InvalidOperationException("Smorzando hit slot is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipAssetPath) ??
                throw new InvalidOperationException("Smorzando hit clip is missing.");
            var captureFolder = ProjectAbsolutePath(CaptureRelativeFolder);
            var frontFolder = Path.Combine(captureFolder, "front_cycle_frames");
            var obliqueFolder = Path.Combine(captureFolder, "oblique_cycle_frames");
            Directory.CreateDirectory(frontFolder);
            Directory.CreateDirectory(obliqueFolder);

            var cameraObject = new GameObject("Smorzando_PersonHit_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Smorzando_PersonHit_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject referenceClone = null;
            GameObject hitClone = null;
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
                referenceClone.name = "Smorzando_PersonHit_StaticReferenceClone";
                hitClone = UnityEngine.Object.Instantiate(hitSlot.gameObject);
                hitClone.name = "Smorzando_PersonHit_MotionClone";
                referenceClone.transform.position = Vector3.zero;
                hitClone.transform.position = Vector3.zero;
                SetCaptureOnly(referenceClone);
                SetCaptureOnly(hitClone);
                DisableHelperComponents(referenceClone);
                DisableHelperComponents(hitClone);
                var hitModel = hitClone.transform.Find(PersonModelName) ??
                    throw new InvalidOperationException("Smorzando hit capture model is missing.");
                var hitAnimator = hitModel.GetComponent<Animator>();
                if (hitAnimator != null)
                {
                    hitAnimator.enabled = false;
                }

                var modelBasePosition = hitModel.localPosition;
                var modelBaseRotation = hitModel.localRotation;
                var modelBaseScale = hitModel.localScale;
                clip.SampleAnimation(hitModel.gameObject, 0f);
                var standingRotations = HitPoseSpecs().ToDictionary(
                    pose => pose.Path,
                    pose => (hitModel.Find(pose.Path) ??
                        throw new InvalidOperationException("Hit capture rig target is missing: " + pose.Path)).localRotation);
                var referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                var hitBounds = CalculateVisibleBounds(hitClone.transform);
                foreach (var sampleTime in new[] { 0.05f, 0.14f, 0.24f, 0.42f, 0.58f })
                {
                    clip.SampleAnimation(hitModel.gameObject, sampleTime);
                    hitBounds.Encapsulate(CalculateVisibleBounds(hitClone.transform));
                }

                var halfSpacing = (referenceBounds.extents.x + hitBounds.extents.x + 0.55f) * 0.5f;
                referenceClone.transform.position += Vector3.right * (-halfSpacing - referenceBounds.center.x);
                hitClone.transform.position += Vector3.right * (halfSpacing - hitBounds.center.x);
                referenceBounds = CalculateVisibleBounds(referenceClone.transform);
                hitBounds = CalculateVisibleBounds(hitClone.transform);
                var pairBounds = referenceBounds;
                pairBounds.Encapsulate(hitBounds);
                floor = CreateCaptureFloor(pairBounds, out floorMaterial);

                referenceClone.SetActive(false);
                var hitTarget = hitBounds.center;
                var hitOrthoSize = Mathf.Max(hitBounds.extents.y + 0.26f, hitBounds.extents.x + 0.26f);
                var frontPosition = hitTarget + Vector3.back * 35f;
                var obliqueDirection = (Vector3.back + Vector3.right * 0.48f).normalized;
                var obliquePosition = hitTarget + obliqueDirection * 35f;
                for (var frame = 0; frame < CycleFrameCount; frame++)
                {
                    var time = frame * CycleDurationSeconds / CycleFrameCount;
                    clip.SampleAnimation(hitModel.gameObject, time);
                    CapturePng(
                        camera,
                        frontPosition,
                        hitTarget,
                        hitOrthoSize,
                        640,
                        640,
                        Path.Combine(frontFolder, $"Smorzando_PersonHit_Front_{frame:000}.png"));
                    CapturePng(
                        camera,
                        obliquePosition,
                        hitTarget,
                        hitOrthoSize,
                        640,
                        640,
                        Path.Combine(obliqueFolder, $"Smorzando_PersonHit_Oblique_{frame:000}.png"));
                }

                referenceClone.SetActive(true);
                var pairTarget = pairBounds.center + Vector3.up * 0.02f;
                var pairOrthoSize = Mathf.Max(
                    pairBounds.extents.y + 0.28f,
                    pairBounds.extents.x / (16f / 9f) + 0.28f);
                clip.SampleAnimation(hitModel.gameObject, 0f);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonHit_StaticVsHit_T000.png"));
                clip.SampleAnimation(hitModel.gameObject, MaximumRecoilTimeSeconds);
                CapturePng(
                    camera,
                    pairTarget + Vector3.back * 40f,
                    pairTarget,
                    pairOrthoSize,
                    1280,
                    720,
                    Path.Combine(captureFolder, "Smorzando_PersonHit_StaticVsHit_T014.png"));

                var keyFrames = new[] { 0, 1, 3, 5, 8, 12, 14 };
                CreateKeyframeSheet(frontFolder, obliqueFolder, captureFolder, keyFrames);
                var videoPath = Path.Combine(captureFolder, "Smorzando_PersonHit_Loop.mp4");
                EncodeLoopVideo(frontFolder, videoPath);

                clip.SampleAnimation(hitModel.gameObject, CycleDurationSeconds);
                var returnedToStanding = HitPoseSpecs().All(pose =>
                {
                    var target = hitModel.Find(pose.Path);
                    return target != null &&
                        Quaternion.Angle(standingRotations[pose.Path], target.localRotation) <= 0.1f;
                });
                var rootStayedFixed = hitModel.localPosition == modelBasePosition &&
                    hitModel.localRotation == modelBaseRotation && hitModel.localScale == modelBaseScale;
                if (!returnedToStanding || !rootStayedFixed)
                {
                    throw new InvalidOperationException(
                        "Smorzando hit capture did not return to standing with a fixed model root.");
                }

                File.WriteAllLines(
                    Path.Combine(captureFolder, "Smorzando_PersonHit_CaptureManifest.txt"),
                    new[]
                    {
                        "Clip=Smorzando_Person_Hit",
                        "CycleDurationSeconds=0.75",
                        "CycleFrameCount=15",
                        "CycleFramesPerSecond=20",
                        "MaximumRecoilTimeSeconds=0.14",
                        "KeyFrames=" + string.Join("|", keyFrames.Select(frame => frame.ToString("000"))),
                        "TargetSlot=Smorzando_Person_05",
                        "StaticReferenceSlot=Smorzando_Person_01",
                        "MaterialReferenceMatched=True",
                        "GeometryMatchedStatic=True",
                        "ReturnedToStanding=True",
                        "ApplyRootMotion=False",
                        "ModelRootTransformAnimated=False",
                        "Views=FrontCycle|ObliqueCycle|StaticVsHit|KeyframeSheet|LoopVideo",
                        "VideoEncoded=True",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                Selection.activeObject = null;
                Debug.Log(
                    "SmorzandoPersonHitFramesCaptured Folder=" + captureFolder +
                    ", Frames=15, Views=Front|Oblique|StaticVsHit|LoopVideo, " +
                    "ReturnedToStanding=True, MaterialMatched=True, VideoEncoded=True, " +
                    "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(referenceClone);
                UnityEngine.Object.DestroyImmediate(hitClone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Smorzando person hit capture changed the scene dirty state.");
                }
            }
        }

        private static AnimationClip CreateOrUpdateHitClip(Transform hitModel)
        {
            EnsureAssetFolder(HitClipAssetPath);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipAssetPath);
            if (clip == null)
            {
                clip = new AnimationClip
                {
                    name = "Smorzando_Person_Hit",
                    frameRate = 60f,
                    wrapMode = WrapMode.Loop
                };
                AssetDatabase.CreateAsset(clip, HitClipAssetPath);
            }

            clip.ClearCurves();
            clip.name = "Smorzando_Person_Hit";
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            foreach (var pose in HitPoseSpecs())
            {
                var target = hitModel.Find(pose.Path) ??
                    throw new InvalidOperationException("Smorzando hit rig target is missing: " + pose.Path);
                AddWorldRotationRecoilCurves(clip, hitModel, target, pose.WorldEulerAtMaximum);
            }

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            AssetDatabase.ImportAsset(HitClipAssetPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(HitClipAssetPath) ??
                throw new InvalidOperationException("Smorzando hit clip could not be reloaded.");
        }

        private static AnimatorController CreateOrUpdateHitController(AnimationClip clip)
        {
            EnsureAssetFolder(HitControllerAssetPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(HitControllerAssetPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(HitControllerAssetPath);
            }
            if (controller.layers.Length == 0)
            {
                AssetDatabase.DeleteAsset(HitControllerAssetPath);
                controller = AnimatorController.CreateAnimatorControllerAtPath(HitControllerAssetPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                .FirstOrDefault(candidate => candidate != null && candidate.name == "Hit") ??
                stateMachine.AddState("Hit");
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

        private static IEnumerable<HitPoseSpec> HitPoseSpecs()
        {
            yield return new HitPoseSpec("Armature", new Vector3(3f, 0f, 0f));
            yield return new HitPoseSpec("Armature/Hips", new Vector3(5f, 0f, 0f));
            yield return new HitPoseSpec("Armature/Hips/LeftUpLeg", new Vector3(-3f, 0f, -1.5f));
            yield return new HitPoseSpec("Armature/Hips/LeftUpLeg/LeftLeg", new Vector3(4f, 0f, 0f));
            yield return new HitPoseSpec("Armature/Hips/RightUpLeg", new Vector3(-3f, 0f, 1.5f));
            yield return new HitPoseSpec("Armature/Hips/RightUpLeg/RightLeg", new Vector3(4f, 0f, 0f));
            yield return new HitPoseSpec("Armature/Hips/Spine02", new Vector3(8f, 0f, 0f));
            yield return new HitPoseSpec("Armature/Hips/Spine02/Spine01", new Vector3(7f, 0f, 0f));
            yield return new HitPoseSpec("Armature/Hips/Spine02/Spine01/Spine", new Vector3(9f, 0f, 0f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/neck", new Vector3(-5f, 0f, 0f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/neck/Head", new Vector3(-4f, 0f, 0f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder", new Vector3(4f, 0f, -7f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm", new Vector3(8f, 0f, -3f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm",
                new Vector3(-5f, 0f, 0f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder", new Vector3(4f, 0f, 7f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm", new Vector3(8f, 0f, 3f));
            yield return new HitPoseSpec(
                "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm",
                new Vector3(-5f, 0f, 0f));
        }

        private static void AddWorldRotationRecoilCurves(
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
                var worldDelta = Quaternion.Euler(worldEulerAtMaximum * PoseWeights[index]);
                rotations[index] = Quaternion.Inverse(parentWorldRotation) *
                    (worldDelta * baselineWorldRotation);
                if (index > 0 && Quaternion.Dot(rotations[index - 1], rotations[index]) < 0f)
                {
                    rotations[index] = new Quaternion(
                        -rotations[index].x,
                        -rotations[index].y,
                        -rotations[index].z,
                        -rotations[index].w);
                }
            }

            SetQuaternionCurve(clip, path, "m_LocalRotation.x", rotations.Select(value => value.x).ToArray());
            SetQuaternionCurve(clip, path, "m_LocalRotation.y", rotations.Select(value => value.y).ToArray());
            SetQuaternionCurve(clip, path, "m_LocalRotation.z", rotations.Select(value => value.z).ToArray());
            SetQuaternionCurve(clip, path, "m_LocalRotation.w", rotations.Select(value => value.w).ToArray());
        }

        private static void SetQuaternionCurve(
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

        private static HitPoseInspection InspectGeneratedHitPose(Transform hitModel, AnimationClip clip)
        {
            var clone = UnityEngine.Object.Instantiate(hitModel.gameObject);
            clone.name = "Smorzando_PersonHit_InternalPoseInspection";
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                foreach (var target in clone.GetComponentsInChildren<Transform>(true))
                {
                    target.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                var animator = clone.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }
                var modelTransform = clone.transform;
                var modelPosition = modelTransform.localPosition;
                var modelRotation = modelTransform.localRotation;
                var modelScale = modelTransform.localScale;
                clip.SampleAnimation(clone, 0f);
                var standingRotations = HitPoseSpecs().ToDictionary(
                    pose => pose.Path,
                    pose => (clone.transform.Find(pose.Path) ??
                        throw new InvalidOperationException("Hit preview target is missing: " + pose.Path)).localRotation);
                var spinePath = "Armature/Hips/Spine02/Spine01/Spine";
                var spine = clone.transform.Find(spinePath) ??
                    throw new InvalidOperationException("Hit preview spine is missing.");
                var standingSpineRotation = spine.rotation;
                clip.SampleAnimation(clone, MaximumRecoilTimeSeconds);
                var maximumSpineAngle = Quaternion.Angle(standingSpineRotation, spine.rotation);
                clip.SampleAnimation(clone, CycleDurationSeconds);
                var returnedToStanding = HitPoseSpecs().All(pose =>
                {
                    var target = clone.transform.Find(pose.Path);
                    return target != null &&
                        Quaternion.Angle(standingRotations[pose.Path], target.localRotation) <= 0.1f;
                });
                var rootStayedFixed = modelTransform.localPosition == modelPosition &&
                    modelTransform.localRotation == modelRotation && modelTransform.localScale == modelScale;
                return new HitPoseInspection(maximumSpineAngle, returnedToStanding, rootStayedFixed);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
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
            SkinnedMeshRenderer hitRenderer)
        {
            var referenceMesh = referenceRenderer.sharedMesh;
            var hitMesh = hitRenderer.sharedMesh;
            if (referenceMesh == null || hitMesh == null ||
                referenceMesh.vertexCount != hitMesh.vertexCount ||
                referenceMesh.subMeshCount != hitMesh.subMeshCount ||
                referenceRenderer.bones.Length != hitRenderer.bones.Length ||
                referenceMesh.bounds.center != hitMesh.bounds.center ||
                referenceMesh.bounds.size != hitMesh.bounds.size)
            {
                throw new InvalidOperationException(
                    "Smorzando hit model geometry does not match the static person model.");
            }
        }

        private static void AssertMaterialsSynchronized(
            SkinnedMeshRenderer referenceRenderer,
            SkinnedMeshRenderer hitRenderer)
        {
            var referenceMaterials = referenceRenderer.sharedMaterials;
            var hitMaterials = hitRenderer.sharedMaterials;
            if (referenceMaterials.Length != hitMaterials.Length)
            {
                throw new InvalidOperationException("Smorzando hit material slot count does not match.");
            }
            for (var index = 0; index < referenceMaterials.Length; index++)
            {
                if (referenceMaterials[index] != hitMaterials[index])
                {
                    throw new InvalidOperationException(
                        "Smorzando hit material is not the static reference material asset.");
                }
            }
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

        private static void EncodeLoopVideo(string frameFolder, string videoPath)
        {
            var inputPattern = Path.Combine(frameFolder, "Smorzando_PersonHit_Front_%03d.png");
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
                throw new InvalidOperationException("ffmpeg could not be started for person hit video.");
            var error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(60000) || process.ExitCode != 0)
            {
                throw new InvalidOperationException("Person hit video encoding failed: " + error);
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
                            $"Smorzando_PersonHit_Front_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        cellSize);
                    CopyPngToSheet(
                        Path.Combine(obliqueFolder,
                            $"Smorzando_PersonHit_Oblique_{keyFrames[index]:000}.png"),
                        sheet,
                        index * cellSize,
                        0);
                }
                sheet.Apply();
                File.WriteAllBytes(
                    Path.Combine(captureFolder, "Smorzando_PersonHit_KeyframeSheet.png"),
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
                    throw new InvalidDataException("Unexpected person hit capture size: " + path);
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
            floor.name = "Smorzando_PersonHit_CaptureFloor";
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

        private static Bounds CalculateVisibleBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Smorzando person hit capture has no visible renderers.");
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

        private readonly struct HitPoseSpec
        {
            public HitPoseSpec(string path, Vector3 worldEulerAtMaximum)
            {
                Path = path;
                WorldEulerAtMaximum = worldEulerAtMaximum;
            }

            public string Path { get; }
            public Vector3 WorldEulerAtMaximum { get; }
        }

        private readonly struct HitPoseInspection
        {
            public HitPoseInspection(
                float maximumSpineAngleDegrees,
                bool returnedToStanding,
                bool modelRootStayedFixed)
            {
                MaximumSpineAngleDegrees = maximumSpineAngleDegrees;
                ReturnedToStanding = returnedToStanding;
                ModelRootStayedFixed = modelRootStayedFixed;
            }

            public float MaximumSpineAngleDegrees { get; }
            public bool ReturnedToStanding { get; }
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
                    throw new InvalidOperationException("Smorzando person hit apply changed a preserved Transform.");
                }
            }
        }
    }
}
