using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaIdleAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string IdleSlotName = "Ata_02_Idle";
        private const string ModelName = "Ata_Model";
        private const string SourceClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_02_Idle.anim";
        private const string AnimationFolder =
            "Assets/_Project/Art/Enemies/Ata/Animations";
        private const string ClipPath = AnimationFolder + "/Ata_02_Idle.anim";
        private const string ControllerPath = AnimationFolder + "/Ata_02_Idle.controller";
        private const string IdleRigName = "Ata_Idle_FootLock_Rig";
        private const string DiagnosticPath =
            "docs/validation/ata_idle_2026-08-11/Ata_02_Idle_Diagnostic.png";
        private const string FinalPath =
            "docs/validation/ata_idle_2026-08-11/Ata_02_Idle_Final.png";
        private const string ReportPath =
            "docs/validation/ata_idle_2026-08-11/Ata_02_Idle_Report.txt";
        // Matches the approved Ispant idle cycle: two seconds and 15 mm of subtle travel.
        private const float Duration = 2f;
        private const float ExpectedVerticalTravel = 0.015f;
        private const float PositionTolerance = 0.0002f;
        private static readonly float[] ReviewTimes = { 0f, 0.5f, 1f, 1.5f, 2f };

        [MenuItem("Bellerophon/Enemies/Ata/Apply Idle Animation")]
        public static void ApplyAtaIdleAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var idleSlot = RequireDirectChild(placement.transform, IdleSlotName);
            var model = RequireDirectChild(idleSlot, ModelName);
            var modelBefore = new TransformSnapshot(model);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, idleSlot);

            EnsureAnimationFolder();
            var clip = CreateClip(model);
            var controller = CreateController(clip);
            ConfigureAnimator(model, controller);
            ConfigureFootLockRig(model);
            NormalizeVisibleVerticalTravel(model, clip);

            if (!modelBefore.Matches())
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle model position, rotation, or scale changed while applying idle animation.");
            }

            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, idleSlot),
                "An Ata slot outside Ata_02_Idle changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ata placement changed.");
            RequireAppliedState(model, clip, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after applying Ata idle animation.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "AtaIdleAnimationApplied Result=PASS" +
                ", Slot=" + IdleSlotName +
                ", Duration=" + Num(Duration) +
                ", Reference=Ispant_02_Idle" +
                ", TargetVerticalTravel=" + Num(ExpectedVerticalTravel) +
                ", RootMotion=False" +
                ", FootLockIK=True" +
                ", OtherAtaSlotsUnchanged=True" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Idle Diagnostic")]
        public static void CaptureAtaIdleAnimationDiagnostic()
        {
            CaptureReview(DiagnosticPath, "AtaIdleAnimationDiagnosticCaptured");
        }

        [MenuItem("Bellerophon/Enemies/Ata/Capture Idle Final")]
        public static void CaptureAtaIdleAnimationFinal()
        {
            CaptureReview(FinalPath, "AtaIdleAnimationFinalCaptured");
        }

        private static AnimationClip CreateClip(Transform model)
        {
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceClipPath) ??
                         throw new InvalidOperationException(
                             "The approved Ispant idle reference clip is missing.");
            var hips = RequireUniqueDescendant(model, "Hips");
            var targetPath = AnimationUtility.CalculateTransformPath(hips, model);
            DeleteAssetIfPresent(ClipPath);
            var clip = new AnimationClip
            {
                name = "Ata_02_Idle",
                frameRate = source.frameRate
            };

            var copied = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                if (!binding.path.EndsWith("/Hips", StringComparison.Ordinal) ||
                    !binding.propertyName.StartsWith(
                        "m_LocalPosition.",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ??
                                  throw new InvalidOperationException(
                                      "The Ispant idle Hips curve is unavailable.");
                var curve = RebasePositionCurve(
                    sourceCurve,
                    hips,
                    binding.propertyName);
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        targetPath,
                        typeof(Transform),
                        binding.propertyName),
                    curve);
                copied++;
            }

            if (copied != 3)
            {
                throw new InvalidOperationException(
                    "Ata idle must copy exactly the three Ispant Hips position curves.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            if (Mathf.Abs(clip.length - Duration) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Ata idle duration differs from the Ispant reference.");
            }

            return clip;
        }

        private static AnimationCurve RebasePositionCurve(
            AnimationCurve source,
            Transform target,
            string propertyName)
        {
            if (source.length == 0)
            {
                throw new InvalidOperationException(
                    "Ata idle source position curve is empty: " + propertyName + ".");
            }

            var targetBaseline = propertyName.EndsWith(".x", StringComparison.Ordinal)
                ? target.localPosition.x
                : propertyName.EndsWith(".y", StringComparison.Ordinal)
                    ? target.localPosition.y
                    : target.localPosition.z;
            var sourceBaseline = source.keys[0].value;
            var keys = source.keys;
            for (var index = 0; index < keys.Length; index++)
            {
                keys[index].value =
                    targetBaseline + keys[index].value - sourceBaseline;
            }

            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            DeleteAssetIfPresent(ControllerPath);
            var controller =
                AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("AtaIdle");
            state.motion = clip;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureAnimator(
            Transform model,
            AnimatorController controller)
        {
            var animators = model.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle contains multiple Animators.");
            }

            var animator = animators.Length == 0
                ? model.gameObject.AddComponent<Animator>()
                : animators[0];
            if (animator.transform != model)
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle Animator must be on Ata_Model.");
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
        }

        private static void ConfigureFootLockRig(Transform model)
        {
            foreach (var existing in Enumerable.Range(0, model.childCount)
                         .Select(model.GetChild)
                         .Where(child => child.name == IdleRigName)
                         .ToArray())
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            var rigObject = new GameObject(IdleRigName);
            rigObject.transform.SetParent(model, false);
            var rig = rigObject.AddComponent(
                RequireRiggingType("UnityEngine.Animations.Rigging.Rig"));
            SetFloat(rig, "m_Weight", 1f);
            CreateFootConstraint(rigObject.transform, model, "Left");
            CreateFootConstraint(rigObject.transform, model, "Right");

            var builderType =
                RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            var builders = model.GetComponents(builderType);
            if (builders.Length > 1)
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle contains multiple RigBuilders.");
            }

            var builder = builders.Length == 0
                ? model.gameObject.AddComponent(builderType)
                : builders[0];
            ((Behaviour)builder).enabled = true;
            var serialized = new SerializedObject(builder);
            var layers = serialized.FindProperty("m_RigLayers") ??
                         throw new InvalidOperationException(
                             "Animation Rigging layer property is unavailable.");
            layers.arraySize = 1;
            var layer = layers.GetArrayElementAtIndex(0);
            layer.FindPropertyRelative("m_Rig").objectReferenceValue = rig;
            layer.FindPropertyRelative("m_Active").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rigObject);
            EditorUtility.SetDirty(rig);
            EditorUtility.SetDirty(builder);
        }

        private static void CreateFootConstraint(
            Transform rigRoot,
            Transform model,
            string side)
        {
            var upper = RequireUniqueDescendant(model, side + "UpLeg");
            var lower = RequireUniqueDescendant(model, side + "Leg");
            var foot = RequireUniqueDescendant(model, side + "Foot");
            var target = new GameObject(side + "FootTarget").transform;
            target.SetParent(rigRoot, false);
            target.position = foot.position;
            target.rotation = foot.rotation;
            var hint = new GameObject(side + "KneeHint").transform;
            hint.SetParent(rigRoot, false);
            var line = foot.position - upper.position;
            var projection = line.sqrMagnitude > 0.000001f
                ? upper.position + Vector3.Project(lower.position - upper.position, line)
                : upper.position;
            var bendDirection = lower.position - projection;
            if (bendDirection.sqrMagnitude < 0.000001f)
            {
                bendDirection = model.forward;
            }

            hint.position = lower.position + bendDirection.normalized *
                Mathf.Max(line.magnitude * 0.35f, 0.1f);
            hint.rotation = lower.rotation;

            var constraintObject = new GameObject(side + "FootTwoBoneIK");
            constraintObject.transform.SetParent(rigRoot, false);
            var constraint = constraintObject.AddComponent(
                RequireRiggingType(
                    "UnityEngine.Animations.Rigging.TwoBoneIKConstraint"));
            var serialized = new SerializedObject(constraint);
            SetObject(serialized, "m_Data.m_Root", upper);
            SetObject(serialized, "m_Data.m_Mid", lower);
            SetObject(serialized, "m_Data.m_Tip", foot);
            SetObject(serialized, "m_Data.m_Target", target);
            SetObject(serialized, "m_Data.m_Hint", hint);
            SetFloat(serialized, "m_Data.m_TargetPositionWeight", 1f);
            SetFloat(serialized, "m_Data.m_TargetRotationWeight", 1f);
            SetFloat(serialized, "m_Data.m_HintWeight", 1f);
            SetBool(serialized, "m_Data.m_MaintainTargetPositionOffset", false);
            SetBool(serialized, "m_Data.m_MaintainTargetRotationOffset", false);
            SetFloat(serialized, "m_Weight", 1f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(constraint);
        }

        private static void RequireAppliedState(
            Transform model,
            AnimationClip clip,
            AnimatorController controller)
        {
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                           throw new InvalidOperationException(
                               "Ata_02_Idle Animator is missing.");
            if (animator.transform != model || !animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle Animator configuration differs.");
            }

            if (AnimationUtility.GetCurveBindings(clip).Any(binding =>
                    !binding.propertyName.StartsWith(
                        "m_LocalPosition.",
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Ata idle contains a curve outside the approved Hips position motion.");
            }

            RequireIdleRig(model);
        }

        private static void NormalizeVisibleVerticalTravel(
            Transform model,
            AnimationClip clip)
        {
            var initialTravel = MeasureVisibleVerticalTravel(model, clip);
            if (initialTravel <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "Ata idle reference mapping has no visible vertical travel.");
            }

            ScalePositionCurveDeltas(clip, ExpectedVerticalTravel / initialTravel);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            var finalTravel = MeasureVisibleVerticalTravel(model, clip);
            if (Mathf.Abs(finalTravel - ExpectedVerticalTravel) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Ata idle vertical travel normalization failed. Actual=" +
                    Num(finalTravel) + ".");
            }
        }

        private static void ScalePositionCurveDeltas(
            AnimationClip clip,
            float ratio)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.propertyName.StartsWith(
                        "m_LocalPosition.",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var baseline = curve.keys[0].value;
                var keys = curve.keys;
                for (var index = 0; index < keys.Length; index++)
                {
                    keys[index].value =
                        baseline + (keys[index].value - baseline) * ratio;
                    keys[index].inTangent *= ratio;
                    keys[index].outTangent *= ratio;
                }

                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }

        private static float MeasureVisibleVerticalTravel(
            Transform model,
            AnimationClip clip)
        {
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var rigBuilder = RequireIdleRig(model);
            var head = RequireUniqueDescendant(model, "Head");
            RigEvaluationSession evaluation = null;
            var minimum = float.PositiveInfinity;
            var maximum = float.NegativeInfinity;
            try
            {
                animator.enabled = false;
                evaluation = new RigEvaluationSession(animator, rigBuilder, clip);
                foreach (var time in ReviewTimes)
                {
                    evaluation.Sample(time);
                    minimum = Mathf.Min(minimum, head.position.y);
                    maximum = Mathf.Max(maximum, head.position.y);
                }

                return maximum - minimum;
            }
            finally
            {
                evaluation?.Dispose();
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
            }
        }

        private static void CaptureReview(string relativePath, string logPrefix)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var model = RequireDirectChild(
                RequireDirectChild(placement.transform, IdleSlotName),
                ModelName);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("Ata idle clip is missing.");
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                throw new InvalidOperationException("Ata idle controller is missing.");
            RequireAppliedState(model, clip, controller);
            var destination = Absolute(relativePath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ata idle capture already exists: " + relativePath);
            }

            var metrics = CaptureStrip(model, clip, destination);
            WriteReport(metrics);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ata idle capture changed the scene dirty state.");
            }

            Debug.Log(
                logPrefix + " Result=PASS" +
                ", Times=0,0.5,1,1.5,2" +
                ", VisibleVerticalTravel=" + Num(metrics.VisibleVerticalTravel) +
                ", MaximumFootTravel=" + Num(metrics.MaximumFootTravel) +
                ", Image=" + relativePath +
                ", SceneChanged=False.");
        }

        private static CaptureMetrics CaptureStrip(
            Transform model,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid Ata idle capture folder."));
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var animator = model.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var rigBuilder = RequireIdleRig(model);
            var leftFoot = RequireUniqueDescendant(model, "LeftFoot");
            var rightFoot = RequireUniqueDescendant(model, "RightFoot");
            var head = RequireUniqueDescendant(model, "Head");
            var otherRenderers = model.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => !renderer.transform.IsChildOf(model))
                .Select(renderer => new RendererSnapshot(renderer))
                .ToArray();
            var sourceCamera = GameObject.Find("Player")?
                                   .GetComponentInChildren<Camera>(true) ??
                               throw new InvalidOperationException(
                                   "Player camera is missing.");
            var cameraObject = new GameObject(
                "AtaIdleReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            const int width = 384;
            const int height = 640;
            var strip = new Texture2D(
                width * ReviewTimes.Length,
                height,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            RigEvaluationSession evaluation = null;
            var initialLeftFoot = leftFoot.position;
            var initialRightFoot = rightFoot.position;
            var minimumHead = float.PositiveInfinity;
            var maximumHead = float.NegativeInfinity;
            var maximumFootTravel = 0f;
            try
            {
                foreach (var snapshot in otherRenderers)
                {
                    snapshot.Renderer.enabled = false;
                }

                animator.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                evaluation = new RigEvaluationSession(animator, rigBuilder, clip);
                evaluation.Sample(0f);
                FrameCamera(camera, model, width / (float)height);
                for (var index = 0; index < ReviewTimes.Length; index++)
                {
                    evaluation.Sample(ReviewTimes[index]);
                    minimumHead = Mathf.Min(minimumHead, head.position.y);
                    maximumHead = Mathf.Max(maximumHead, head.position.y);
                    maximumFootTravel = Mathf.Max(
                        maximumFootTravel,
                        Vector3.Distance(leftFoot.position, initialLeftFoot),
                        Vector3.Distance(rightFoot.position, initialRightFoot));
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                    {
                        throw new InvalidOperationException(
                            "Ata idle review contains Unity magenta shader fallback.");
                    }

                    strip.SetPixels32(index * width, 0, width, height, pixels);
                }

                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
                return new CaptureMetrics(
                    maximumHead - minimumHead,
                    maximumFootTravel);
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                evaluation?.Dispose();
                foreach (var renderer in otherRenderers)
                {
                    renderer.Restore();
                }

                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(Camera camera, Transform model, float aspect)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(false);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Ata idle model has no renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            var direction = GameObject.Find("Player")?
                                .GetComponentInChildren<Camera>(true)?
                                .transform.position - bounds.center ?? Vector3.back;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.back;
            }

            direction.Normalize();
            camera.aspect = aspect;
            var vertical = bounds.extents.y /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                             Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.18f;
            camera.transform.position =
                bounds.center + direction * distance + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static Component RequireIdleRig(Transform model)
        {
            var builderType =
                RequireRiggingType("UnityEngine.Animations.Rigging.RigBuilder");
            var constraintType = RequireRiggingType(
                "UnityEngine.Animations.Rigging.TwoBoneIKConstraint");
            var builder = model.GetComponents(builderType).SingleOrDefault() ??
                          throw new InvalidOperationException(
                              "Ata_02_Idle RigBuilder is missing.");
            var serialized = new SerializedObject(builder);
            var layers = serialized.FindProperty("m_RigLayers");
            if (!((Behaviour)builder).enabled || layers == null || layers.arraySize != 1)
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle RigBuilder configuration differs.");
            }

            var layer = layers.GetArrayElementAtIndex(0);
            var rig = layer.FindPropertyRelative("m_Rig").objectReferenceValue as Component;
            if (!layer.FindPropertyRelative("m_Active").boolValue ||
                rig == null || rig.name != IdleRigName)
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle Rig layer configuration differs.");
            }

            if (rig.GetComponentsInChildren(constraintType, true).Length != 2)
            {
                throw new InvalidOperationException(
                    "Ata_02_Idle must contain two foot IK constraints.");
            }

            return builder;
        }

        private static void WriteReport(CaptureMetrics metrics)
        {
            var absolute = Absolute(ReportPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException("Invalid Ata idle report folder."));
            File.WriteAllLines(
                absolute,
                new[]
                {
                    "Target=Approved Ata Enemy Placement/Ata_02_Idle",
                    "Reference=Ispant_02_Idle",
                    "DurationSeconds=" + Num(Duration),
                    "TargetVerticalTravel=" + Num(ExpectedVerticalTravel),
                    "VisibleVerticalTravel=" + Num(metrics.VisibleVerticalTravel),
                    "MaximumFootTravel=" + Num(metrics.MaximumFootTravel),
                    "RootMotion=False",
                    "FootLockIK=True",
                    "PositionCurvesOnly=True",
                    "OtherAtaSlotsChanged=False",
                    "PlayerOrCameraChanged=False"
                },
                Encoding.UTF8);
        }

        private static void EnsureAnimationFolder()
        {
            const string ataFolder = "Assets/_Project/Art/Enemies/Ata";
            if (!AssetDatabase.IsValidFolder(AnimationFolder) &&
                string.IsNullOrEmpty(AssetDatabase.CreateFolder(ataFolder, "Animations")))
            {
                throw new InvalidOperationException(
                    "Ata animation folder could not be created.");
            }
        }

        private static Scene RequireScene(bool requireClean)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Ata idle animation work requires Edit Mode.");
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "The current active scene must be CargoRunMvp.");
            }

            if (requireClean && scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes.");
            }

            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(root => root.name == PlacementRootName) ??
                   throw new InvalidOperationException(
                       "Approved Ata placement is missing.");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(child => child.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            }

            return matches[0];
        }

        private static Transform RequireUniqueDescendant(Transform root, string name)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Required Ata rig transform differs: " + name + ".");
            }

            return matches[0];
        }

        private static string[] OtherSlotSignatures(
            Transform placement,
            Transform idleSlot)
        {
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(slot => slot != idleSlot)
                .Select(RecursiveSignature)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(item.gameObject.activeSelf).Append('|')
                    .Append(Vec(item.localPosition)).Append('|')
                    .Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).Append('|')
                    .Append(string.Join(",", item.GetComponents<Component>()
                        .Where(component => component != null)
                        .Select(component => component.GetType().FullName)
                        .OrderBy(name => name, StringComparer.Ordinal)))
                    .AppendLine();
            }

            return builder.ToString();
        }

        private static string[] OtherRootSignatures(
            Scene scene,
            GameObject placement)
        {
            return scene.GetRootGameObjects()
                .Where(root => root != placement)
                .Select(root =>
                    root.name + "|" + root.activeSelf + "|" +
                    Vec(root.transform.localPosition) + "|" +
                    Quat(root.transform.localRotation) + "|" +
                    Vec(root.transform.localScale) + "|" +
                    root.transform.childCount.ToString(CultureInfo.InvariantCulture))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static Type RequireRiggingType(string fullName)
        {
            return Type.GetType(fullName + ", Unity.Animation.Rigging") ??
                   throw new InvalidOperationException(
                       "Animation Rigging type is unavailable: " + fullName + ".");
        }

        private static object InvokeRigBuilder(
            Component builder,
            string methodName,
            params object[] arguments)
        {
            var method = builder.GetType().GetMethods()
                .SingleOrDefault(candidate =>
                    candidate.Name == methodName &&
                    candidate.GetParameters().Length == arguments.Length) ??
                throw new InvalidOperationException(
                    "Animation Rigging method is unavailable: " + methodName + ".");
            return method.Invoke(builder, arguments);
        }

        private static void SetObject(
            SerializedObject serialized,
            string path,
            UnityEngine.Object value)
        {
            var property = serialized.FindProperty(path) ??
                           throw new InvalidOperationException(
                               "Animation Rigging object property is unavailable: " +
                               path + ".");
            property.objectReferenceValue = value;
        }

        private static void SetFloat(Component component, string path, float value)
        {
            var serialized = new SerializedObject(component);
            SetFloat(serialized, path, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(
            SerializedObject serialized,
            string path,
            float value)
        {
            var property = serialized.FindProperty(path) ??
                           throw new InvalidOperationException(
                               "Animation Rigging float property is unavailable: " +
                               path + ".");
            property.floatValue = value;
        }

        private static void SetBool(
            SerializedObject serialized,
            string path,
            bool value)
        {
            var property = serialized.FindProperty(path) ??
                           throw new InvalidOperationException(
                               "Animation Rigging bool property is unavailable: " +
                               path + ".");
            property.boolValue = value;
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(
                    "Existing Ata idle asset could not be replaced: " + path + ".");
            }
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + ")";
        }

        private static string Quat(Quaternion value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + "," + Num(value.w) + ")";
        }

        private sealed class RigEvaluationSession : IDisposable
        {
            private readonly Animator animator;
            private readonly Component builder;
            private readonly bool animatorEnabled;
            private readonly bool builderEnabled;
            private PlayableGraph graph;
            private AnimationClipPlayable clipPlayable;

            public RigEvaluationSession(
                Animator animator,
                Component builder,
                AnimationClip clip)
            {
                this.animator = animator;
                this.builder = builder;
                animatorEnabled = animator.enabled;
                builderEnabled = ((Behaviour)builder).enabled;
                animator.enabled = true;
                ((Behaviour)builder).enabled = false;
                animator.Rebind();
                graph = PlayableGraph.Create("AtaIdleRigEvaluation");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                clipPlayable = AnimationClipPlayable.Create(graph, clip);
                clipPlayable.SetApplyFootIK(false);
                var output = AnimationPlayableOutput.Create(
                    graph,
                    "AtaIdleClip",
                    animator);
                output.SetSourcePlayable(clipPlayable);
                if (!(bool)InvokeRigBuilder(builder, "Build", graph))
                {
                    throw new InvalidOperationException(
                        "Ata idle RigBuilder graph could not be built.");
                }

                graph.Play();
            }

            public void Sample(float time)
            {
                clipPlayable.SetTime(time);
                InvokeRigBuilder(builder, "SyncLayers");
                graph.Evaluate(0f);
            }

            public void Dispose()
            {
                if (graph.IsValid())
                {
                    graph.Destroy();
                }

                InvokeRigBuilder(builder, "Clear");
                ((Behaviour)builder).enabled = builderEnabled;
                animator.enabled = animatorEnabled;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            public void Restore()
            {
                if (transform == null)
                {
                    return;
                }

                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }

            public bool Matches()
            {
                return transform != null &&
                       Vector3.Distance(position, transform.localPosition) <=
                       PositionTolerance &&
                       Quaternion.Angle(rotation, transform.localRotation) <= 0.01f &&
                       Vector3.Distance(scale, transform.localScale) <=
                       PositionTolerance;
            }
        }

        private readonly struct RendererSnapshot
        {
            public readonly Renderer Renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer renderer)
            {
                Renderer = renderer;
                enabled = renderer.enabled;
            }

            public void Restore()
            {
                if (Renderer != null)
                {
                    Renderer.enabled = enabled;
                }
            }
        }

        private readonly struct CaptureMetrics
        {
            public readonly float VisibleVerticalTravel;
            public readonly float MaximumFootTravel;

            public CaptureMetrics(
                float visibleVerticalTravel,
                float maximumFootTravel)
            {
                VisibleVerticalTravel = visibleVerticalTravel;
                MaximumFootTravel = maximumFootTravel;
            }
        }
    }
}
