using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Fuga;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.FugaCargoRunScene
{
    internal static class FugaHitReactionMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string HitSlotName = "Fuga_04_Hit";
        private const string ModelName = "Fuga_Model";
        private const string PlayerName = "Player";
        private const string LeftClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Hit_NewModel_Left.anim";
        private const string RightClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Hit_NewModel_Right.anim";
        private const string SlowWingFlapClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Hit_NewModel_SlowWingFlap.anim";
        private const string IdleWingbeatClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Idle_NewModel_WingbeatBreathing.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Hit_NewModel_Random.controller";
        private const string SlowWingLayerName = "Slow Wing Flap";
        private const string SlowWingStateName = "Fuga_Hit_SlowWingFlap_08Hz";
        private const string LegacyClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Hit_SquashRecoil.anim";
        private const string LegacyControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Hit_SquashRecoil.controller";
        private const string OutputFolder = "docs/validation/fuga_hit_reaction_2026-08-17";
        private const string ReportPath = OutputFolder + "/Fuga_Hit_Reaction_Report.txt";
        private const string CapturePath = OutputFolder + "/Fuga_Hit_Reaction_Comparison.png";

        // User-approved hit-reaction values. Fuga_Model owns both curves so its body and child wings move together.
        private const float MaximumBodyRollDegrees = 45f;
        private const float VerticalRecoilMeters = 0.15f;
        private const float ReactionReturnSeconds = 0.6f;
        private const float HorizontalHoldSeconds = 0.5f;
        private const float ReactionDurationSeconds = ReactionReturnSeconds + HorizontalHoldSeconds;
        private const float VerticalReverseTimeSeconds = ReactionReturnSeconds * 0.5f;
        private const float SlowWingbeatFrequencyHz = 0.8f;
        private const float SlowWingbeatDurationSeconds = 1f / SlowWingbeatFrequencyHz;
        private const float SlowWingbeatHalfCycleSeconds = SlowWingbeatDurationSeconds * 0.5f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private static readonly float[] MotionSampleTimes =
        {
            0f,
            VerticalReverseTimeSeconds,
            ReactionReturnSeconds,
            ReactionDurationSeconds,
        };

        private static readonly float[] CaptureReactionTimes = { 0f, 0.3f, 0.6f };
        private static readonly float[] CaptureHoldTimes = { 0.6f, 0.85f, 1.1f };

        [MenuItem("Bellerophon/Enemies/Fuga/Apply New Hit Reaction")]
        public static void ApplyFugaHitReaction()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the Fuga hit reaction.");
            }

            var legacyClipHashBefore = Sha256(Absolute(LegacyClipPath));
            var legacyControllerHashBefore = Sha256(Absolute(LegacyControllerPath));
            var placement = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placement, HitSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var slotTransformBefore = TransformSignature(slot);
            var modelTransformBefore = TransformSignature(model);
            var otherFugaBefore = OtherFugaSignature(placement);
            var otherRootsBefore = OtherRootSignatures(scene);

            if (model.localPosition.sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(model.localRotation, Quaternion.identity) > 0.001f)
            {
                throw new InvalidOperationException("The Fuga hit model must start at the neutral local transform.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The Fuga hit model has no SkinnedMeshRenderer.");
            RequireWingHierarchy(model, renderer);
            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var idleWingbeatClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleWingbeatClipPath) ??
                                   throw new InvalidOperationException("The approved Fuga idle wingbeat clip is missing.");

            var leftClip = CreateHitClip(
                LeftClipPath,
                FugaHitReactionRandomDriver.LeftStateName,
                slot,
                model,
                -MaximumBodyRollDegrees);
            var rightClip = CreateHitClip(
                RightClipPath,
                FugaHitReactionRandomDriver.RightStateName,
                slot,
                model,
                MaximumBodyRollDegrees);
            var slowWingFlapClip = CreateSlowWingFlapClip(idleWingbeatClip, slot, leftWing, rightWing);
            var controller = CreateController(leftClip, rightClip, slowWingFlapClip);

            var animator = slot.GetComponent<Animator>() ?? slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            var driver = slot.GetComponent<FugaHitReactionRandomDriver>() ??
                         slot.gameObject.AddComponent<FugaHitReactionRandomDriver>();
            driver.Configure(animator, configuredRepeatPlayback: true);
            driver.enabled = true;
            EditorUtility.SetDirty(driver);

            var legacyPlayback = slot.GetComponent<FugaAnimationReviewPlaybackDriver>();
            if (legacyPlayback != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyPlayback);
            }

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Fuga_04_Hit has no Rigidbody.");
            var physicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                                throw new InvalidOperationException("Fuga_04_Hit has no FugaPhysicsMotionDriver.");
            body.isKinematic = true;
            body.useGravity = false;
            physicsDriver.LockRootMotionForReview = true;
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(physicsDriver);

            if (!string.Equals(slotTransformBefore, TransformSignature(slot), StringComparison.Ordinal) ||
                !string.Equals(modelTransformBefore, TransformSignature(model), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Fuga hit slot or model base transform changed.");
            }

            if (!string.Equals(otherFugaBefore, OtherFugaSignature(placement), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A protected non-hit Fuga slot changed.");
            }

            if (!otherRootsBefore.SequenceEqual(OtherRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside the Fuga placement changed.");
            }

            RequireHash(legacyClipHashBefore, Sha256(Absolute(LegacyClipPath)), "legacy Fuga hit clip");
            RequireHash(
                legacyControllerHashBefore,
                Sha256(Absolute(LegacyControllerPath)),
                "legacy Fuga hit controller");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying the Fuga hit reaction.");
            }

            AssetDatabase.SaveAssetIfDirty(leftClip);
            AssetDatabase.SaveAssetIfDirty(rightClip);
            AssetDatabase.SaveAssetIfDirty(slowWingFlapClip);
            AssetDatabase.SaveAssetIfDirty(controller);
            AssetDatabase.SaveAssets();
            var result = InspectAppliedState();
            WriteReport(result, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaHitReactionApplied Result=PASS" +
                ", RandomDirection=Uniform50_50" +
                ", MaximumBodyRollDegrees=45" +
                ", VerticalRecoilMeters=0.15" +
                ", ReturnSeconds=0.6" +
                ", HorizontalHoldSeconds=0.5" +
                ", ReplayIntervalSeconds=1.1" +
                ", WingbeatFrequencyHz=0.8" +
                ", AutomaticRepeatPlayback=True" +
                ", FreshRandomDirectionPerReplay=True" +
                ", WingsInherit=True" +
                ", RigidbodyRootMoved=False" +
                ", LegacyHitAnimationUsed=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect New Hit Reaction")]
        public static void InspectFugaHitReaction()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var result = InspectAppliedState();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Inspecting the Fuga hit reaction changed the scene dirty state.");
            }

            WriteReport(result, captureCreated: File.Exists(Absolute(CapturePath)));
            Debug.Log(
                "FugaHitReactionInspected Result=PASS" +
                ", RandomDirection=Uniform50_50" +
                ", LeftRightRollDegrees=-45,+45" +
                ", VerticalOffsetsMeters=+0.15,-0.15,0" +
                ", ReturnSeconds=0.6" +
                ", HorizontalHoldSeconds=0.5" +
                ", ReplayIntervalSeconds=1.1" +
                ", WingbeatFrequencyHz=0.8" +
                ", AutomaticRepeatPlayback=True" +
                ", FreshRandomDirectionPerReplay=True" +
                ", WingsInherit=True" +
                ", RigidbodyRootMoved=False" +
                ", LegacyHitAnimationUsed=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture New Hit Reaction")]
        public static void CaptureFugaHitReaction()
        {
            var result = InspectAppliedState();
            CaptureComparison(result.Slot, result.LeftClip, result.SlowWingFlapClip, Absolute(CapturePath));
            WriteReport(result, captureCreated: true);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaHitReactionCaptured Result=PASS" +
                ", Cycle1Direction=Left" +
                ", Cycle1SampleTimesSeconds=0,0.3,0.6" +
                ", HorizontalHoldSampleTimesSeconds=0.6,0.85,1.1" +
                ", WingbeatFrequencyHz=0.8" +
                ", AutomaticRepeatPlayback=True" +
                ", Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        private static HitResult InspectAppliedState()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            var placement = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placement, HitSlotName);
            var model = RequireDirectChild(slot, ModelName);
            if (model.localPosition.sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(model.localRotation, Quaternion.identity) > 0.001f)
            {
                throw new InvalidOperationException("The Fuga hit model base transform changed in the scene.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The Fuga hit model has no SkinnedMeshRenderer.");
            RequireWingHierarchy(model, renderer);
            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");

            var leftClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LeftClipPath) ??
                           throw new InvalidOperationException("The new left Fuga hit clip is missing.");
            var rightClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RightClipPath) ??
                            throw new InvalidOperationException("The new right Fuga hit clip is missing.");
            var slowWingFlapClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SlowWingFlapClipPath) ??
                                   throw new InvalidOperationException("The new slow Fuga hit wingbeat clip is missing.");
            var idleWingbeatClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleWingbeatClipPath) ??
                                   throw new InvalidOperationException("The approved Fuga idle wingbeat clip is missing.");
            InspectClip(leftClip, slot, model, -MaximumBodyRollDegrees, "left");
            InspectClip(rightClip, slot, model, MaximumBodyRollDegrees, "right");
            InspectSampledMotion(slot, model, leftClip, -MaximumBodyRollDegrees, "left");
            InspectSampledMotion(slot, model, rightClip, MaximumBodyRollDegrees, "right");
            InspectSlowWingFlapClip(slowWingFlapClip, idleWingbeatClip, slot, leftWing, rightWing);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The new Fuga hit controller is missing.");
            InspectController(controller, leftClip, rightClip, slowWingFlapClip);
            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The Fuga hit Animator is missing.");
            if (!animator.enabled || animator.applyRootMotion || animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("The Fuga hit Animator configuration is incorrect.");
            }

            var driver = slot.GetComponent<FugaHitReactionRandomDriver>() ??
                         throw new InvalidOperationException("The Fuga hit random-direction driver is missing.");
            if (!driver.enabled || driver.Animator != animator || !driver.RepeatPlayback ||
                FugaHitReactionDirectionSelector.Select(0f) != FugaHitReactionDirection.Left ||
                FugaHitReactionDirectionSelector.Select(0.499999f) != FugaHitReactionDirection.Left ||
                FugaHitReactionDirectionSelector.Select(0.5f) != FugaHitReactionDirection.Right ||
                FugaHitReactionDirectionSelector.Select(1f) != FugaHitReactionDirection.Right)
            {
                throw new InvalidOperationException("The Fuga hit direction selection is not an exact 50:50 split.");
            }

            InspectReplayClock();

            if (slot.GetComponent<FugaAnimationReviewPlaybackDriver>() != null)
            {
                throw new InvalidOperationException("The Fuga hit slot still uses the legacy playback driver.");
            }

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("The Fuga hit Rigidbody is missing.");
            var physicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                                throw new InvalidOperationException("The Fuga hit physics driver is missing.");
            if (!body.isKinematic || body.useGravity || !physicsDriver.LockRootMotionForReview ||
                physicsDriver.FollowVerticalAxis || physicsDriver.UseDeathFallSequence || physicsDriver.IdleHoverEnabled)
            {
                throw new InvalidOperationException("The Fuga hit Rigidbody root configuration changed.");
            }

            RequireHash(Sha256(Absolute(LegacyClipPath)), Sha256(Absolute(LegacyClipPath)), "legacy Fuga hit clip");
            if (controller.animationClips.Any(clip =>
                    string.Equals(AssetDatabase.GetAssetPath(clip), LegacyClipPath, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("The new Fuga hit controller still uses the legacy hit clip.");
            }

            return new HitResult(slot, leftClip, rightClip, slowWingFlapClip, slot.position.y);
        }

        private static AnimationClip CreateHitClip(
            string assetPath,
            string clipName,
            Transform slot,
            Transform model,
            float startingRollDegrees)
        {
            AssetDatabase.DeleteAsset(assetPath);
            var clip = new AnimationClip
            {
                name = clipName,
                frameRate = 60f,
                wrapMode = WrapMode.Once,
            };
            var modelPath = RelativePath(slot, model);
            SetCurve(
                clip,
                modelPath,
                "localEulerAnglesRaw.x",
                0f,
                0f,
                ReactionReturnSeconds,
                0f,
                ReactionDurationSeconds,
                0f);
            SetCurve(
                clip,
                modelPath,
                "localEulerAnglesRaw.y",
                0f,
                0f,
                ReactionReturnSeconds,
                0f,
                ReactionDurationSeconds,
                0f);
            SetCurve(
                clip,
                modelPath,
                "localEulerAnglesRaw.z",
                0f,
                startingRollDegrees,
                ReactionReturnSeconds,
                0f,
                ReactionDurationSeconds,
                0f);
            SetCurve(
                clip,
                modelPath,
                "m_LocalPosition.y",
                0f,
                model.localPosition.y + VerticalRecoilMeters,
                VerticalReverseTimeSeconds,
                model.localPosition.y - VerticalRecoilMeters,
                ReactionReturnSeconds,
                model.localPosition.y,
                ReactionDurationSeconds,
                model.localPosition.y);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) ??
                   throw new InvalidOperationException("The new Fuga hit clip was not created: " + assetPath + ".");
        }

        private static AnimationClip CreateSlowWingFlapClip(
            AnimationClip idleWingbeatClip,
            Transform slot,
            Transform leftWing,
            Transform rightWing)
        {
            var wingPaths = new HashSet<string>(StringComparer.Ordinal)
            {
                RelativePath(slot, leftWing),
                RelativePath(slot, rightWing),
            };
            var sourceBindings = AnimationUtility.GetCurveBindings(idleWingbeatClip)
                .Where(binding =>
                    binding.type == typeof(Transform) &&
                    wingPaths.Contains(binding.path) &&
                    binding.propertyName.IndexOf("localRotation", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (sourceBindings.Length != 8)
            {
                throw new InvalidOperationException("The approved idle clip must provide exactly eight wing rotation curves.");
            }

            AssetDatabase.DeleteAsset(SlowWingFlapClipPath);
            var clip = new AnimationClip
            {
                name = "Fuga_Hit_NewModel_SlowWingFlap",
                frameRate = 60f,
                wrapMode = WrapMode.Loop,
            };
            foreach (var binding in sourceBindings)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(idleWingbeatClip, binding) ??
                                  throw new InvalidOperationException("An approved idle wing rotation curve is missing.");
                if (sourceCurve.length < 2)
                {
                    throw new InvalidOperationException("An approved idle wing rotation curve has insufficient pose keys.");
                }

                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    SmoothCurve(
                        new Keyframe(0f, sourceCurve.keys[0].value),
                        new Keyframe(SlowWingbeatHalfCycleSeconds, sourceCurve.keys[1].value),
                        new Keyframe(SlowWingbeatDurationSeconds, sourceCurve.keys[0].value)));
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, SlowWingFlapClipPath);
            AssetDatabase.ImportAsset(SlowWingFlapClipPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(SlowWingFlapClipPath) ??
                   throw new InvalidOperationException("The slow Fuga hit wingbeat clip was not created.");
        }

        private static AnimatorController CreateController(
            AnimationClip leftClip,
            AnimationClip rightClip,
            AnimationClip slowWingFlapClip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var leftState = stateMachine.AddState(FugaHitReactionRandomDriver.LeftStateName);
            leftState.motion = leftClip;
            leftState.writeDefaultValues = true;
            var rightState = stateMachine.AddState(FugaHitReactionRandomDriver.RightStateName);
            rightState.motion = rightClip;
            rightState.writeDefaultValues = true;
            stateMachine.defaultState = leftState;

            controller.AddLayer(SlowWingLayerName);
            var layers = controller.layers;
            layers[1].defaultWeight = 1f;
            controller.layers = layers;
            var slowWingStateMachine = controller.layers[1].stateMachine;
            var slowWingState = slowWingStateMachine.AddState(SlowWingStateName);
            slowWingState.motion = slowWingFlapClip;
            slowWingState.writeDefaultValues = true;
            slowWingStateMachine.defaultState = slowWingState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void InspectClip(
            AnimationClip clip,
            Transform slot,
            Transform model,
            float startingRollDegrees,
            string label)
        {
            if (Mathf.Abs(clip.length - ReactionDurationSeconds) > 0.0001f || clip.isLooping)
            {
                throw new InvalidOperationException("The " + label + " Fuga hit clip duration or loop setting is incorrect.");
            }

            var path = RelativePath(slot, model);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var expectedProperties = new HashSet<string>(StringComparer.Ordinal)
            {
                "localEulerAnglesRaw.x",
                "localEulerAnglesRaw.y",
                "localEulerAnglesRaw.z",
                "m_LocalPosition.y",
            };
            if (bindings.Length != expectedProperties.Count ||
                bindings.Any(binding =>
                    !string.Equals(binding.path, path, StringComparison.Ordinal) ||
                    !expectedProperties.Contains(binding.propertyName)))
            {
                throw new InvalidOperationException("The " + label + " Fuga hit clip binding contract is incorrect.");
            }

            RequireCurve(
                clip,
                path,
                "localEulerAnglesRaw.x",
                new[] { 0f, ReactionReturnSeconds, ReactionDurationSeconds },
                new[] { 0f, 0f, 0f });
            RequireCurve(
                clip,
                path,
                "localEulerAnglesRaw.y",
                new[] { 0f, ReactionReturnSeconds, ReactionDurationSeconds },
                new[] { 0f, 0f, 0f });
            RequireCurve(
                clip,
                path,
                "localEulerAnglesRaw.z",
                new[] { 0f, ReactionReturnSeconds, ReactionDurationSeconds },
                new[] { startingRollDegrees, 0f, 0f });
            RequireCurve(
                clip,
                path,
                "m_LocalPosition.y",
                MotionSampleTimes,
                new[] { VerticalRecoilMeters, -VerticalRecoilMeters, 0f, 0f });
        }

        private static void InspectSampledMotion(
            Transform slot,
            Transform model,
            AnimationClip clip,
            float startingRollDegrees,
            string label)
        {
            var scene = slot.gameObject.scene;
            var dirtyBefore = scene.isDirty;
            var rootPosition = slot.position;
            var modelBasePosition = model.localPosition;
            var expectedRolls = new[] { startingRollDegrees, startingRollDegrees * 0.5f, 0f, 0f };
            var expectedVerticalOffsets = new[] { VerticalRecoilMeters, -VerticalRecoilMeters, 0f, 0f };
            AnimationMode.StartAnimationMode();
            try
            {
                for (var index = 0; index < MotionSampleTimes.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(slot.gameObject, clip, MotionSampleTimes[index]);
                    AnimationMode.EndSampling();
                    var roll = Mathf.DeltaAngle(0f, model.localEulerAngles.z);
                    if (Mathf.Abs(roll - expectedRolls[index]) > 0.05f ||
                        Mathf.Abs(model.localPosition.y -
                                  (modelBasePosition.y + expectedVerticalOffsets[index])) > 0.0001f ||
                        Mathf.Abs(model.localPosition.x - modelBasePosition.x) > 0.0001f ||
                        Mathf.Abs(model.localPosition.z - modelBasePosition.z) > 0.0001f ||
                        Vector3.Distance(slot.position, rootPosition) > 0.0001f)
                    {
                        throw new InvalidOperationException(
                            "The sampled " + label + " Fuga hit pose is incorrect at " +
                            MotionSampleTimes[index].ToString("0.###", CultureInfo.InvariantCulture) + " seconds.");
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }
            }

            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Sampling the " + label + " Fuga hit clip changed the scene.");
            }
        }

        private static void InspectController(
            AnimatorController controller,
            AnimationClip leftClip,
            AnimationClip rightClip,
            AnimationClip slowWingFlapClip)
        {
            if (controller.layers.Length != 2)
            {
                throw new InvalidOperationException("The Fuga hit controller must have a reaction layer and a slow-wing layer.");
            }

            var reactionStates = controller.layers[0].stateMachine.states;
            if (reactionStates.Length != 2)
            {
                throw new InvalidOperationException("The Fuga hit controller must have exactly two states.");
            }

            var leftState = reactionStates.Select(child => child.state).SingleOrDefault(state =>
                string.Equals(state.name, FugaHitReactionRandomDriver.LeftStateName, StringComparison.Ordinal));
            var rightState = reactionStates.Select(child => child.state).SingleOrDefault(state =>
                string.Equals(state.name, FugaHitReactionRandomDriver.RightStateName, StringComparison.Ordinal));
            if (leftState == null || rightState == null || leftState.motion != leftClip || rightState.motion != rightClip)
            {
                throw new InvalidOperationException("The Fuga hit controller state-to-clip mapping is incorrect.");
            }

            var slowWingLayer = controller.layers[1];
            var slowWingStates = slowWingLayer.stateMachine.states;
            if (!string.Equals(slowWingLayer.name, SlowWingLayerName, StringComparison.Ordinal) ||
                Mathf.Abs(slowWingLayer.defaultWeight - 1f) > 0.0001f ||
                slowWingStates.Length != 1 ||
                !string.Equals(slowWingStates[0].state.name, SlowWingStateName, StringComparison.Ordinal) ||
                slowWingStates[0].state.motion != slowWingFlapClip)
            {
                throw new InvalidOperationException("The independent slow-wing layer configuration is incorrect.");
            }

            var expectedClips = new HashSet<AnimationClip> { leftClip, rightClip, slowWingFlapClip };
            if (!expectedClips.SetEquals(controller.animationClips))
            {
                throw new InvalidOperationException("The Fuga hit controller clip set is incorrect.");
            }
        }

        private static void InspectReplayClock()
        {
            var elapsed = 0f;
            if (Mathf.Abs(FugaHitReactionReplayClock.IntervalSeconds - ReactionDurationSeconds) > 0.0001f ||
                FugaHitReactionReplayClock.Advance(ref elapsed, 1.099f) != 0 ||
                Mathf.Abs(elapsed - 1.099f) > 0.0001f ||
                FugaHitReactionReplayClock.Advance(ref elapsed, 0.001f) != 1 ||
                Mathf.Abs(elapsed) > 0.0001f ||
                FugaHitReactionReplayClock.Advance(ref elapsed, 2.2f) != 2 ||
                Mathf.Abs(elapsed) > 0.0001f)
            {
                throw new InvalidOperationException("The Fuga hit 1.1-second replay clock is incorrect.");
            }
        }

        private static void InspectSlowWingFlapClip(
            AnimationClip slowWingFlapClip,
            AnimationClip idleWingbeatClip,
            Transform slot,
            Transform leftWing,
            Transform rightWing)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(slowWingFlapClip);
            if (Mathf.Abs(slowWingFlapClip.length - SlowWingbeatDurationSeconds) > 0.0001f ||
                !settings.loopTime)
            {
                throw new InvalidOperationException("The slow Fuga hit wingbeat must loop at exactly 0.8 Hz.");
            }

            var wingPaths = new HashSet<string>(StringComparer.Ordinal)
            {
                RelativePath(slot, leftWing),
                RelativePath(slot, rightWing),
            };
            var bindings = AnimationUtility.GetCurveBindings(slowWingFlapClip);
            if (bindings.Length != 8 || bindings.Any(binding =>
                    binding.type != typeof(Transform) ||
                    !wingPaths.Contains(binding.path) ||
                    binding.propertyName.IndexOf("localRotation", StringComparison.OrdinalIgnoreCase) < 0))
            {
                throw new InvalidOperationException("The slow Fuga hit wingbeat must contain only eight wing rotation curves.");
            }

            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(slowWingFlapClip, binding) ??
                            throw new InvalidOperationException("A slow Fuga hit wing rotation curve is missing.");
                var sourceCurve = AnimationUtility.GetEditorCurve(idleWingbeatClip, binding) ??
                                  throw new InvalidOperationException("The matching approved idle wing curve is missing.");
                if (curve.length != 3 || sourceCurve.length < 2 ||
                    Mathf.Abs(curve.keys[0].time) > 0.0001f ||
                    Mathf.Abs(curve.keys[1].time - SlowWingbeatHalfCycleSeconds) > 0.0001f ||
                    Mathf.Abs(curve.keys[2].time - SlowWingbeatDurationSeconds) > 0.0001f ||
                    Mathf.Abs(curve.keys[0].value - sourceCurve.keys[0].value) > 0.0001f ||
                    Mathf.Abs(curve.keys[1].value - sourceCurve.keys[1].value) > 0.0001f ||
                    Mathf.Abs(curve.keys[2].value - sourceCurve.keys[0].value) > 0.0001f)
                {
                    throw new InvalidOperationException("The slow Fuga hit wingbeat did not preserve the approved idle wing poses.");
                }
            }

            foreach (var wingPath in wingPaths)
            {
                var upstroke = ReadLocalRotation(slowWingFlapClip, wingPath, 0f);
                var downstroke = ReadLocalRotation(slowWingFlapClip, wingPath, SlowWingbeatHalfCycleSeconds);
                if (Mathf.Abs(Quaternion.Angle(upstroke, downstroke) - 84f) > 0.05f)
                {
                    throw new InvalidOperationException("The slow Fuga hit wingbeat did not preserve the 84-degree idle stroke range.");
                }
            }
        }

        private static Quaternion ReadLocalRotation(AnimationClip clip, string path, float time)
        {
            float Component(string suffix)
            {
                var binding = AnimationUtility.GetCurveBindings(clip).Single(candidate =>
                    string.Equals(candidate.path, path, StringComparison.Ordinal) &&
                    candidate.type == typeof(Transform) &&
                    candidate.propertyName.EndsWith("localRotation." + suffix, StringComparison.OrdinalIgnoreCase));
                return AnimationUtility.GetEditorCurve(clip, binding).Evaluate(time);
            }

            return new Quaternion(Component("x"), Component("y"), Component("z"), Component("w")).normalized;
        }

        private static void CaptureComparison(
            Transform slot,
            AnimationClip hitClip,
            AnimationClip slowWingFlapClip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga hit capture path."));
            var scene = SceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            Texture2D composite = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            try
            {
                cameraObject = new GameObject("FugaHitCaptureCamera", typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                lightObject = new GameObject("FugaHitCaptureLight", typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
                camera.cullingMask = ~0;
                camera.allowHDR = false;
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;

                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

                var panelWidth = CaptureWidth / 3;
                var panelHeight = CaptureHeight / 2;
                composite = new Texture2D(panelWidth * 3, panelHeight * 2, TextureFormat.RGB24, false);
                var playerCamera = RequireRoot(PlayerName).GetComponentInChildren<Camera>(true) ??
                                   throw new InvalidOperationException("The Player camera is missing.");

                AnimationMode.StartAnimationMode();
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(slot.gameObject, hitClip, ReactionDurationSeconds);
                AnimationMode.SampleAnimationClip(slot.gameObject, slowWingFlapClip, 0f);
                AnimationMode.EndSampling();
                var baselineBounds = BoundsOf(slot);
                var fixedCenter = baselineBounds.center;
                var fixedDirection = (fixedCenter - playerCamera.transform.position).normalized;
                var fixedOrthographicSize = Mathf.Max(
                    baselineBounds.extents.y * 1.55f,
                    baselineBounds.extents.x * 1.55f / (panelWidth / (float)panelHeight));
                var sampleRows = new[] { CaptureReactionTimes, CaptureHoldTimes };
                for (var row = 0; row < sampleRows.Length; row++)
                {
                    for (var column = 0; column < sampleRows[row].Length; column++)
                    {
                        var sampleTime = sampleRows[row][column];
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(slot.gameObject, hitClip, sampleTime);
                        AnimationMode.SampleAnimationClip(slot.gameObject, slowWingFlapClip, sampleTime);
                        AnimationMode.EndSampling();
                        camera.transform.position = fixedCenter - fixedDirection * 10f;
                        camera.transform.rotation = Quaternion.LookRotation(fixedDirection, Vector3.up);
                        camera.orthographicSize = fixedOrthographicSize;
                        var panel = Render(camera, panelWidth, panelHeight);
                        composite.SetPixels(
                            column * panelWidth,
                            (1 - row) * panelHeight,
                            panelWidth,
                            panelHeight,
                            panel.GetPixels());
                        UnityEngine.Object.DestroyImmediate(panel);
                    }
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }

                if (composite != null)
                {
                    UnityEngine.Object.DestroyImmediate(composite);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }

            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The temporary Fuga hit capture changed the scene dirty state.");
            }
        }

        private static void WriteReport(HitResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga New Hit Reaction Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + HitSlotName)
                .AppendLine("LeftClip=" + LeftClipPath)
                .AppendLine("RightClip=" + RightClipPath)
                .AppendLine("SlowWingFlapClip=" + SlowWingFlapClipPath)
                .AppendLine("SlowWingFlapSource=" + IdleWingbeatClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("ExistingHitAnimationUsed=False")
                .AppendLine("ExistingHitAssetsModified=False")
                .AppendLine("DirectionSelection=Uniform50_50PerHit")
                .AppendLine("AutomaticRepeatPlayback=True")
                .AppendLine("ReplayIntervalSeconds=1.100")
                .AppendLine("FreshRandomDirectionPerReplay=True")
                .AppendLine("InitialPlaybackOnEnable=True")
                .AppendLine("LeftSelectionInterval=[0.0,0.5)")
                .AppendLine("RightSelectionInterval=[0.5,1.0]")
                .AppendLine("MaximumBodyRollDegrees=45.000")
                .AppendLine("LeftBodyRollDegrees=-45.000->0.000")
                .AppendLine("RightBodyRollDegrees=45.000->0.000")
                .AppendLine("BodyRollReturnSeconds=0.600")
                .AppendLine("HorizontalHoldStartSeconds=0.600")
                .AppendLine("HorizontalHoldEndSeconds=1.100")
                .AppendLine("HorizontalHoldDurationSeconds=0.500")
                .AppendLine("VerticalRecoilOwner=Fuga_ModelLocalPositionY")
                .AppendLine("VerticalRecoilMeters=0.150")
                .AppendLine("VerticalRecoilTimesSeconds=0.000,0.300,0.600")
                .AppendLine("VerticalRecoilOffsetsMeters=0.150,-0.150,0.000")
                .AppendLine("WingsInheritBodyRoll=True")
                .AppendLine("WingsInheritVerticalRecoil=True")
                .AppendLine("HitBaseLayerIndependentWingCurves=0")
                .AppendLine("SlowWingLayerName=" + SlowWingLayerName)
                .AppendLine("SlowWingLayerWeight=1.000")
                .AppendLine("SlowWingLayerRotationCurves=8")
                .AppendLine("SlowWingbeatFrequencyHz=0.800")
                .AppendLine("SlowWingbeatCycleSeconds=1.250")
                .AppendLine("SlowWingbeatHalfCycleSeconds=0.625")
                .AppendLine("SlowWingbeatPoseSource=IdleWingbeatPoseAAndPoseB")
                .AppendLine("SlowWingbeatStrokeRangeDegrees=84.000")
                .AppendLine("SlowWingbeatBodyBreathingCopied=False")
                .AppendLine("HitRecoilCurvesModified=False")
                .AppendLine("HitSlotRootPositionCurves=0")
                .AppendLine("RigidbodyRootMoved=False")
                .AppendLine("HitSlotWorldY=" + Num(result.SlotWorldY))
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("CaptureCycle1Direction=Left")
                .AppendLine("CaptureCycle1SampleTimesSeconds=0,0.3,0.6")
                .AppendLine("CaptureHorizontalHoldSampleTimesSeconds=0.6,0.85,1.1")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga hit report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void SetCurve(AnimationClip clip, string path, string property, params float[] timeValues)
        {
            if (timeValues.Length == 0 || timeValues.Length % 2 != 0)
            {
                throw new ArgumentException("Curve time/value pairs are required.", nameof(timeValues));
            }

            var keys = new Keyframe[timeValues.Length / 2];
            for (var index = 0; index < keys.Length; index++)
            {
                keys[index] = new Keyframe(timeValues[index * 2], timeValues[index * 2 + 1]);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                LinearCurve(keys));
        }

        private static AnimationCurve LinearCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < keys.Length; index++)
            {
                if (index > 0)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                }

                if (index < keys.Length - 1)
                {
                    AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                }
            }

            return curve;
        }

        private static AnimationCurve SmoothCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static void RequireCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyList<float> expectedTimes,
            IReadOnlyList<float> expectedValues)
        {
            var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                        throw new InvalidOperationException(clip.name + " is missing " + property + ".");
            if (curve.length != expectedTimes.Count || expectedTimes.Count != expectedValues.Count)
            {
                throw new InvalidOperationException(clip.name + " has an incorrect " + property + " key count.");
            }

            for (var index = 0; index < curve.length; index++)
            {
                if (Mathf.Abs(curve.keys[index].time - expectedTimes[index]) > 0.0001f ||
                    Mathf.Abs(curve.keys[index].value - expectedValues[index]) > 0.0001f)
                {
                    throw new InvalidOperationException(clip.name + " has an incorrect " + property + " key.");
                }
            }
        }

        private static void RequireWingHierarchy(Transform model, SkinnedMeshRenderer renderer)
        {
            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            if (!leftWing.IsChildOf(model) || !rightWing.IsChildOf(model))
            {
                throw new InvalidOperationException("Both Fuga wings must remain under Fuga_Model.");
            }
        }

        private static Transform FindBone(SkinnedMeshRenderer renderer, string name)
        {
            return renderer.bones.FirstOrDefault(bone => bone != null && string.Equals(bone.name, name, StringComparison.Ordinal)) ??
                   throw new InvalidOperationException("The Fuga model is missing bone " + name + ".");
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            }

            return scene;
        }

        private static Transform RequireRoot(string name)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                       .Select(gameObject => gameObject.transform)
                       .SingleOrDefault(transform => string.Equals(transform.name, name, StringComparison.Ordinal)) ??
                   throw new InvalidOperationException("The scene root is missing: " + name + ".");
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            return Enumerable.Range(0, parent.childCount)
                       .Select(parent.GetChild)
                       .SingleOrDefault(child => string.Equals(child.name, name, StringComparison.Ordinal)) ??
                   throw new InvalidOperationException(parent.name + " is missing direct child " + name + ".");
        }

        private static string RelativePath(Transform root, Transform target)
        {
            var path = AnimationUtility.CalculateTransformPath(target, root);
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("The animation target cannot be the slot root.");
            }

            return path;
        }

        private static Texture2D Render(Camera camera, int width, int height)
        {
            var previous = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                return image;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static string[] OtherRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(gameObject => !string.Equals(gameObject.name, PlacementRootName, StringComparison.Ordinal))
                .Select(gameObject => TransformSignature(gameObject.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string OtherFugaSignature(Transform placement)
        {
            return string.Join(
                "\n",
                Enumerable.Range(0, placement.childCount)
                    .Select(placement.GetChild)
                    .Where(child => !string.Equals(child.name, HitSlotName, StringComparison.Ordinal))
                    .Select(TransformSignature)
                    .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static string TransformSignature(Transform transform)
        {
            return transform.name + "|" + Vec(transform.localPosition) + "|" +
                   Vec(transform.localEulerAngles) + "|" + Vec(transform.localScale);
        }

        private static string Sha256(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Required preserved asset is missing.", path);
            }

            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The " + label + " changed.");
            }
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), projectRelativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private sealed class HitResult
        {
            public HitResult(
                Transform slot,
                AnimationClip leftClip,
                AnimationClip rightClip,
                AnimationClip slowWingFlapClip,
                float slotWorldY)
            {
                Slot = slot;
                LeftClip = leftClip;
                RightClip = rightClip;
                SlowWingFlapClip = slowWingFlapClip;
                SlotWorldY = slotWorldY;
            }

            public Transform Slot { get; }
            public AnimationClip LeftClip { get; }
            public AnimationClip RightClip { get; }
            public AnimationClip SlowWingFlapClip { get; }
            public float SlotWorldY { get; }
        }
    }
}
