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
    internal static class FugaMoveMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string MoveSlotName = "Fuga_02_Move";
        private const string IdleSlotName = "Fuga_01_Idle";
        private const string ModelName = "Fuga_Model";
        private const string PlayerName = "Player";
        private const string MoveHoverTargetName = "Fuga_02_Move_HoverTarget";
        private const string SourceModelPath = "D:/Bellerophon2/Bellerophon/enemies model/fuga.glb";
        private const string ImportedModelPath = "Assets/_Project/Art/Enemies/Fuga/Models/fuga.glb";
        private const string ExpectedModelSha256 =
            "009430EB298B83C6EA48CD2AF7B9BE3DF075EA512DAF6978BBE41D5C917AF3AB";
        private const string ExpectedImportedRigSha256 =
            "4DA5AE82DE38E84804188549A6E24F923D77BC04EF072B98D245F34C2B0A9C3B";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Move_NewModel_StationaryFlight.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Move_NewModel_StationaryFlight.controller";
        private const string IdleControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Idle_NewModel_WingbeatBreathing.controller";
        private const string IdleMeshPath =
            "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Idle_BreathingMesh.asset";
        private const string OutputFolder = "docs/validation/fuga_move_motion_2026-08-16";
        private const string ReportPath = OutputFolder + "/Fuga_Move_Motion_Report.txt";
        private const string CapturePath = OutputFolder + "/Fuga_Move_Motion_Comparison.png";
        private const string Tilt15ReportPath = OutputFolder + "/Fuga_Move_Tilt_15_Report.txt";
        private const string Tilt15CapturePath = OutputFolder + "/Fuga_Move_Tilt_15_Comparison.png";
        private const float LoopDuration = 2f;
        private const float WingbeatFrequency = 1.5f;
        private const int WingbeatsPerLoop = 3;
        private const float UpstrokeAngle = 44f;
        private const float DownstrokeAngle = -40f;
        private const float ForwardTiltAngle = 15f;
        private const float HoverAmplitude = 0.015f;
        private const float HoverFrequency = 1.5f;
        private const float HoverFollowGain = 24f;
        private const float HoverSpeedLimit = 0.8f;
        private const float PlayerDistance = 5f;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;

        private static readonly string[] SlotNames =
        {
            "Fuga_00_Static",
            "Fuga_01_Idle",
            "Fuga_02_Move",
            "Fuga_03_Attack",
            "Fuga_04_Hit",
            "Fuga_05_Death",
            "Fuga_06_Consume",
        };

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Move Motion")]
        public static void ApplyFugaMoveMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the Fuga move motion.");
            }

            RequireModelHashes();
            var placementRoot = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placementRoot, MoveSlotName);
            var player = RequireRoot(PlayerName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The move Fuga model has no SkinnedMeshRenderer.");
            var protectedRootsBefore = OtherRootSignatures(scene);
            var placementTransformBefore = TransformSignature(placementRoot);
            var otherFugaBefore = OtherFugaSignature(placementRoot);
            var moveProtectedBefore = MoveProtectedSignature(slot, model, renderer);
            var playerPreservedBefore = PlayerPreservedSignature(player);

            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var clip = CreateMoveClip(slot, model, leftWing, rightWing);
            var controller = CreateController(clip);

            var animator = slot.GetComponent<Animator>() ?? slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Fuga_02_Move has no Rigidbody.");
            if (slot.GetComponent<Collider>() == null)
            {
                throw new InvalidOperationException("Fuga_02_Move has no Collider.");
            }

            var driver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Fuga_02_Move has no FugaPhysicsMotionDriver.");
            var target = driver.MotionPathTarget ??
                         throw new InvalidOperationException("Fuga_02_Move has no approved Motion Path target.");
            target.SetParent(placementRoot, false);
            target.name = MoveHoverTargetName;
            target.localPosition = slot.localPosition;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
            EditorUtility.SetDirty(target);

            body.isKinematic = false;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            EditorUtility.SetDirty(body);

            driver.enabled = true;
            driver.Configure(body, target, false, true, false, false);
            driver.ConfigureIdleHover(HoverAmplitude, HoverFrequency, HoverFollowGain, HoverSpeedLimit);
            EditorUtility.SetDirty(driver);

            ConfigurePlayerAtDistance(placementRoot, player);

            if (!string.Equals(placementTransformBefore, TransformSignature(placementRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Fuga placement root transform changed.");
            }

            if (!string.Equals(otherFugaBefore, OtherFugaSignature(placementRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A protected non-move Fuga slot changed.");
            }

            if (!string.Equals(moveProtectedBefore, MoveProtectedSignature(slot, model, renderer), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A protected move-slot transform, model, mesh, collider, or component changed.");
            }

            if (!string.Equals(playerPreservedBefore, PlayerPreservedSignature(player), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A Player property outside the approved root position and rotation changed.");
            }

            var protectedRootsAfter = OtherRootSignatures(scene);
            if (!protectedRootsBefore.SequenceEqual(protectedRootsAfter, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Fuga and Player changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying Fuga move motion.");
            }

            AssetDatabase.SaveAssets();
            RequireModelHashes();
            var result = InspectAppliedState();
            WriteReport(result, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaMoveMotionApplied Result=PASS" +
                ", WingbeatHz=1.5" +
                ", WingbeatsPerLoop=3" +
                ", ForwardTiltDegrees=" + Num(ForwardTiltAngle) +
                ", HoverHz=1.5" +
                ", HoverAmplitudeMeters=0.015" +
                ", PlayerDistanceMeters=5" +
                ", ForwardTranslation=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Move Tilt 15")]
        public static void ApplyFugaMoveTilt15()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the 15-degree Fuga move tilt.");
            }

            RequireModelHashes();
            var placementRoot = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placementRoot, MoveSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The Fuga move clip is missing.");
            var modelPath = RelativePath(slot, model);
            var wingCurvesBefore = WingCurveSignature(clip, modelPath);
            var controllerHashBefore = Sha256(Absolute(ControllerPath));
            var sceneHashBefore = Sha256(Absolute(ScenePath));

            var tilted = model.localRotation * Quaternion.AngleAxis(ForwardTiltAngle, Vector3.right);
            AddConstantRotationCurves(clip, modelPath, tilted);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);

            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                   throw new InvalidOperationException("The updated Fuga move clip is missing.");
            if (!string.Equals(wingCurvesBefore, WingCurveSignature(clip, modelPath), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A Fuga move wing flap curve changed while applying the body tilt.");
            }

            RequireHash(controllerHashBefore, Sha256(Absolute(ControllerPath)), "Fuga move controller preservation");
            RequireHash(sceneHashBefore, Sha256(Absolute(ScenePath)), "CargoRunMvp scene preservation");
            if (scene.isDirty)
            {
                throw new InvalidOperationException("Applying the 15-degree Fuga move tilt changed the scene dirty state.");
            }

            var result = InspectAppliedState();
            WriteTilt15Report(result, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaMoveTilt15Applied Result=PASS" +
                ", ForwardTiltDegrees=15" +
                ", WingsInheritModelTilt=True" +
                ", WingLocalFlapCurvesChanged=False" +
                ", ControllerChanged=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Move Motion")]
        public static void InspectFugaMoveMotion()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            RequireModelHashes();
            var result = InspectAppliedState();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Fuga move-motion inspection changed the scene dirty state.");
            }

            WriteReport(result, File.Exists(Absolute(CapturePath)));
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaMoveMotionInspected Result=PASS" +
                ", WingbeatHz=1.5" +
                ", ForwardTiltDegrees=" + Num(ForwardTiltAngle) +
                ", HoverHz=1.5" +
                ", PlayerDistanceMeters=" + Num(result.PlayerHorizontalDistance) +
                ", OtherFugaSlotsChanged=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Move Tilt 15")]
        public static void InspectFugaMoveTilt15()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var sceneHashBefore = Sha256(Absolute(ScenePath));
            var controllerHashBefore = Sha256(Absolute(ControllerPath));
            RequireModelHashes();
            var result = InspectAppliedState();
            RequireHash(controllerHashBefore, Sha256(Absolute(ControllerPath)), "Fuga move controller inspection");
            RequireHash(sceneHashBefore, Sha256(Absolute(ScenePath)), "CargoRunMvp scene inspection");
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The 15-degree Fuga move-tilt inspection changed the scene dirty state.");
            }

            WriteTilt15Report(result, File.Exists(Absolute(Tilt15CapturePath)));
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaMoveTilt15Inspected Result=PASS" +
                ", ForwardTiltDegrees=15" +
                ", WingsInheritModelTilt=True" +
                ", WingbeatHz=1.5" +
                ", HoverHz=1.5" +
                ", PlayerDistanceMeters=" + Num(result.PlayerHorizontalDistance) +
                ", ControllerChanged=False" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Move Motion")]
        public static void CaptureFugaMoveMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final move-motion capture.");
            }

            var result = InspectAppliedState();
            CaptureComparison(result.Slot, result.Clip, Absolute(CapturePath));
            WriteReport(result, captureCreated: true);
            AssetDatabase.Refresh();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final Fuga move-motion capture changed the scene.");
            }

            Debug.Log(
                "FugaMoveMotionCaptured Result=PASS" +
                ", SampleTimesSeconds=0,0.333333,0.666667,1" +
                ", Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Move Tilt 15")]
        public static void CaptureFugaMoveTilt15()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final 15-degree move-tilt capture.");
            }

            var sceneHashBefore = Sha256(Absolute(ScenePath));
            var controllerHashBefore = Sha256(Absolute(ControllerPath));
            var result = InspectAppliedState();
            CaptureComparison(result.Slot, result.Clip, Absolute(Tilt15CapturePath));
            RequireHash(controllerHashBefore, Sha256(Absolute(ControllerPath)), "Fuga move controller capture");
            RequireHash(sceneHashBefore, Sha256(Absolute(ScenePath)), "CargoRunMvp scene capture");
            WriteTilt15Report(result, captureCreated: true);
            AssetDatabase.Refresh();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final 15-degree Fuga move-tilt capture changed the scene.");
            }

            Debug.Log(
                "FugaMoveTilt15Captured Result=PASS" +
                ", SampleTimesSeconds=0,0.333333,0.666667,1" +
                ", Image=" + Tilt15CapturePath +
                ", SceneChanged=False.");
        }

        private static MoveResult InspectAppliedState()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            var placementRoot = RequireRoot(PlacementRootName);
            var slots = SlotNames.Select(name => RequireDirectChild(placementRoot, name)).ToArray();
            for (var index = 0; index < slots.Length; index++)
            {
                if (slots[index].GetSiblingIndex() != index ||
                    (index > 0 && slots[index - 1].localPosition.x >= slots[index].localPosition.x))
                {
                    throw new InvalidOperationException("The approved Fuga state order changed.");
                }
            }

            var slot = slots[2];
            var model = RequireDirectChild(slot, ModelName);
            if (model.localPosition.sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(model.localRotation, Quaternion.identity) > 0.001f)
            {
                throw new InvalidOperationException("The move model base transform changed in the scene.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The move Fuga model has no SkinnedMeshRenderer.");
            if (!string.Equals(
                    AssetDatabase.GetAssetPath(renderer.sharedMesh),
                    ImportedModelPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The move Fuga mesh assignment changed.");
            }

            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The move Fuga Animator is missing.");
            if (!animator.enabled || animator.applyRootMotion ||
                !string.Equals(
                    AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
                    ControllerPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The move Fuga Animator configuration is incorrect.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The Fuga move clip is missing.");
            InspectClipContract(clip, model);

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("The move Fuga Rigidbody is missing.");
            var driver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("The move Fuga physics driver is missing.");
            var target = driver.MotionPathTarget ??
                         throw new InvalidOperationException("The move Fuga hover target is missing.");
            if (body.isKinematic || body.useGravity || body.constraints != RigidbodyConstraints.FreezeRotation ||
                driver.Body != body || driver.LockRootMotionForReview || !driver.FollowVerticalAxis ||
                driver.UseDeathFallSequence || !driver.IdleHoverEnabled ||
                Mathf.Abs(driver.IdleHoverAmplitude - HoverAmplitude) > 0.0001f ||
                Mathf.Abs(driver.IdleHoverFrequency - HoverFrequency) > 0.0001f ||
                target.parent != placementRoot || target.name != MoveHoverTargetName ||
                Mathf.Abs(driver.IdleHoverBaseLocalPosition.x - slot.localPosition.x) > 0.000001f ||
                Mathf.Abs(driver.IdleHoverBaseLocalPosition.y - slot.localPosition.y) > 0.000001f ||
                Mathf.Abs(driver.IdleHoverBaseLocalPosition.z - slot.localPosition.z) > 0.000001f ||
                Mathf.Abs(target.localPosition.x - slot.localPosition.x) > 0.000001f ||
                Mathf.Abs(target.localPosition.z - slot.localPosition.z) > 0.000001f)
            {
                throw new InvalidOperationException("The move Fuga stationary-flight Rigidbody configuration is incorrect.");
            }

            var idleSlot = slots[1];
            var idleAnimator = idleSlot.GetComponent<Animator>() ??
                               throw new InvalidOperationException("The idle Fuga Animator is missing.");
            var idleRenderer = RequireDirectChild(idleSlot, ModelName)
                .GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                throw new InvalidOperationException("The idle Fuga renderer is missing.");
            var idleDriver = idleSlot.GetComponent<FugaPhysicsMotionDriver>() ??
                             throw new InvalidOperationException("The idle Fuga physics driver is missing.");
            if (!string.Equals(
                    AssetDatabase.GetAssetPath(idleAnimator.runtimeAnimatorController),
                    IdleControllerPath,
                    StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.GetAssetPath(idleRenderer.sharedMesh), IdleMeshPath, StringComparison.Ordinal) ||
                Mathf.Abs(idleDriver.IdleHoverFrequency - 1f) > 0.0001f ||
                Mathf.Abs(idleDriver.IdleHoverAmplitude - 0.015f) > 0.0001f)
            {
                throw new InvalidOperationException("The existing Fuga idle motion changed.");
            }

            for (var index = 0; index < slots.Length; index++)
            {
                if (index == 1 || index == 2)
                {
                    continue;
                }

                var otherAnimator = slots[index].GetComponent<Animator>();
                if (otherAnimator != null && otherAnimator.runtimeAnimatorController != null)
                {
                    throw new InvalidOperationException(slots[index].name + " received an unexpected controller.");
                }
            }

            var player = RequireRoot(PlayerName);
            var focus = LineupCenter(slots);
            var playerDelta = player.position - focus;
            playerDelta.y = 0f;
            var playerDistance = playerDelta.magnitude;
            if (Mathf.Abs(playerDistance - PlayerDistance) > 0.001f)
            {
                throw new InvalidOperationException("The Player is not exactly five meters from the Fuga lineup center.");
            }

            var camera = player.GetComponentInChildren<Camera>(true) ??
                         throw new InvalidOperationException("The Player camera is missing.");
            var toFocus = focus - camera.transform.position;
            toFocus.y = 0f;
            var cameraForward = camera.transform.forward;
            cameraForward.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f || cameraForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(toFocus.normalized, cameraForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException("The Player camera does not face the Fuga lineup.");
            }

            return new MoveResult(slot, clip, playerDistance, target.localPosition.y, focus, player.position);
        }

        private static AnimationClip CreateMoveClip(
            Transform slot,
            Transform model,
            Transform leftWing,
            Transform rightWing)
        {
            AssetDatabase.DeleteAsset(ClipPath);
            var clip = new AnimationClip
            {
                name = "Fuga_Move_NewModel_StationaryFlight",
                frameRate = 60f,
                wrapMode = WrapMode.Loop
            };

            AddWingRotationCurves(clip, RelativePath(slot, leftWing), leftWing.localRotation);
            AddWingRotationCurves(clip, RelativePath(slot, rightWing), rightWing.localRotation);
            var tilted = model.localRotation * Quaternion.AngleAxis(ForwardTiltAngle, Vector3.right);
            AddConstantRotationCurves(clip, RelativePath(slot, model), tilted);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, ClipPath);
            AssetDatabase.ImportAsset(ClipPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                   throw new InvalidOperationException("The Fuga move clip was not created.");
        }

        private static void AddWingRotationCurves(
            AnimationClip clip,
            string path,
            Quaternion bindRotation)
        {
            var keyCount = WingbeatsPerLoop * 2 + 1;
            var x = new Keyframe[keyCount];
            var y = new Keyframe[keyCount];
            var z = new Keyframe[keyCount];
            var w = new Keyframe[keyCount];
            var previous = Quaternion.identity;
            for (var index = 0; index < keyCount; index++)
            {
                var time = index * (0.5f / WingbeatFrequency);
                var angle = index % 2 == 0 ? UpstrokeAngle : DownstrokeAngle;
                var value = bindRotation * Quaternion.AngleAxis(angle, Vector3.right);
                if (index > 0 && Quaternion.Dot(previous, value) < 0f)
                {
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                }

                previous = value;
                x[index] = new Keyframe(time, value.x);
                y[index] = new Keyframe(time, value.y);
                z[index] = new Keyframe(time, value.z);
                w[index] = new Keyframe(time, value.w);
            }

            SetRotationCurves(clip, path, x, y, z, w);
        }

        private static void AddConstantRotationCurves(AnimationClip clip, string path, Quaternion value)
        {
            SetRotationCurves(
                clip,
                path,
                new Keyframe(0f, value.x), new Keyframe(LoopDuration, value.x),
                new Keyframe(0f, value.y), new Keyframe(LoopDuration, value.y),
                new Keyframe(0f, value.z), new Keyframe(LoopDuration, value.z),
                new Keyframe(0f, value.w), new Keyframe(LoopDuration, value.w));
        }

        private static void SetRotationCurves(
            AnimationClip clip,
            string path,
            Keyframe[] x,
            Keyframe[] y,
            Keyframe[] z,
            Keyframe[] w)
        {
            clip.SetCurve(path, typeof(Transform), "localRotation.x", SmoothCurve(x));
            clip.SetCurve(path, typeof(Transform), "localRotation.y", SmoothCurve(y));
            clip.SetCurve(path, typeof(Transform), "localRotation.z", SmoothCurve(z));
            clip.SetCurve(path, typeof(Transform), "localRotation.w", SmoothCurve(w));
        }

        private static void SetRotationCurves(
            AnimationClip clip,
            string path,
            Keyframe x0,
            Keyframe x1,
            Keyframe y0,
            Keyframe y1,
            Keyframe z0,
            Keyframe z1,
            Keyframe w0,
            Keyframe w1)
        {
            SetRotationCurves(
                clip,
                path,
                new[] { x0, x1 },
                new[] { y0, y1 },
                new[] { z0, z1 },
                new[] { w0, w1 });
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

        private static AnimatorController CreateController(AnimationClip clip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("Fuga_Move_NewModel_StationaryFlight");
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void InspectClipContract(AnimationClip clip, Transform model)
        {
            if (Mathf.Abs(clip.length - LoopDuration) > 0.0001f ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("The Fuga move clip is not an exact looping two-second clip.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 12 || bindings.Any(binding => string.IsNullOrEmpty(binding.path)) ||
                bindings.Any(binding =>
                    binding.propertyName.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                throw new InvalidOperationException("The Fuga move clip contains unexpected or root-position curves.");
            }

            var modelPath = RelativePath(model.parent, model);
            var bodyBindings = bindings.Where(binding => binding.path == modelPath).ToArray();
            var wingBindings = bindings.Where(binding => binding.path != modelPath).ToArray();
            if (bodyBindings.Length != 4 || wingBindings.Length != 8)
            {
                throw new InvalidOperationException("The Fuga move clip body/wing curve ownership is incorrect.");
            }

            if (wingBindings.Any(binding =>
                    !binding.path.StartsWith(modelPath + "/", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("A Fuga move wing curve is not below Fuga_Model and cannot inherit its tilt.");
            }

            foreach (var binding in wingBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException("A Fuga move wing curve is missing.");
                if (curve.length != 7)
                {
                    throw new InvalidOperationException("A Fuga move wing curve does not contain seven keys.");
                }

                for (var index = 0; index < curve.length; index++)
                {
                    var expectedTime = index * (0.5f / WingbeatFrequency);
                    if (Mathf.Abs(curve.keys[index].time - expectedTime) > 0.0001f)
                    {
                        throw new InvalidOperationException("A Fuga move wing curve is not exactly 1.5Hz.");
                    }
                }
            }

            var bodyCurves = bodyBindings.ToDictionary(
                binding => binding.propertyName,
                binding => AnimationUtility.GetEditorCurve(clip, binding),
                StringComparer.Ordinal);
            if (bodyCurves.Values.Any(curve => curve == null || curve.length != 2 ||
                                              Mathf.Abs(curve.keys[0].time) > 0.0001f ||
                                              Mathf.Abs(curve.keys[1].time - LoopDuration) > 0.0001f))
            {
                throw new InvalidOperationException("The Fuga move body tilt curves are not constant for two seconds.");
            }

            var actualTilt = QuaternionFromCurves(bodyCurves, 0);
            var expectedTilt = model.localRotation * Quaternion.AngleAxis(ForwardTiltAngle, Vector3.right);
            if (Quaternion.Angle(actualTilt, expectedTilt) > 0.05f)
            {
                throw new InvalidOperationException(
                    "The Fuga move body is not tilted forward by " + Num(ForwardTiltAngle) + " degrees.");
            }
        }

        private static string WingCurveSignature(AnimationClip clip, string modelPath)
        {
            var builder = new StringBuilder();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => binding.path != modelPath)
                         .OrderBy(binding => binding.path, StringComparer.Ordinal)
                         .ThenBy(binding => binding.propertyName, StringComparer.Ordinal))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException("A Fuga move wing curve is missing.");
                builder.Append(binding.path).Append('|').Append(binding.propertyName).Append('|');
                foreach (var key in curve.keys)
                {
                    builder.Append(Num(key.time)).Append(',')
                        .Append(Num(key.value)).Append(',')
                        .Append(Num(key.inTangent)).Append(',')
                        .Append(Num(key.outTangent)).Append(';');
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static Quaternion QuaternionFromCurves(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            int keyIndex)
        {
            return new Quaternion(
                CurveComponent(curves, 'x', keyIndex),
                CurveComponent(curves, 'y', keyIndex),
                CurveComponent(curves, 'z', keyIndex),
                CurveComponent(curves, 'w', keyIndex));
        }

        private static float CurveComponent(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            char component,
            int keyIndex)
        {
            var matches = curves
                .Where(pair => pair.Key.EndsWith("." + component, StringComparison.Ordinal))
                .Select(pair => pair.Value)
                .ToArray();
            if (matches.Length != 1 || matches[0] == null || matches[0].length <= keyIndex)
            {
                throw new InvalidOperationException("A Fuga move quaternion curve component is missing: " + component + ".");
            }

            return matches[0].keys[keyIndex].value;
        }

        private static void ConfigurePlayerAtDistance(Transform placementRoot, Transform player)
        {
            var slots = SlotNames.Select(name => RequireDirectChild(placementRoot, name)).ToArray();
            var focus = LineupCenter(slots);
            var direction = player.position - focus;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("The existing Player-to-Fuga direction is unusable.");
            }

            direction.Normalize();
            var position = focus + direction * PlayerDistance;
            position.y = player.position.y;
            var lookDirection = focus - position;
            lookDirection.y = 0f;
            player.SetPositionAndRotation(
                position,
                Quaternion.LookRotation(lookDirection.normalized, Vector3.up));
            EditorUtility.SetDirty(player);
        }

        private static Vector3 LineupCenter(IEnumerable<Transform> slots)
        {
            var array = slots.ToArray();
            if (array.Length == 0)
            {
                throw new InvalidOperationException("The Fuga lineup is empty.");
            }

            var sum = Vector3.zero;
            foreach (var slot in array)
            {
                sum += slot.position;
            }

            return sum / array.Length;
        }

        private static string MoveProtectedSignature(
            Transform slot,
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var collider = slot.GetComponent<BoxCollider>() ??
                           throw new InvalidOperationException("The move Fuga BoxCollider is missing.");
            return TransformSignature(slot) + "|" + slot.GetSiblingIndex() + "|" +
                   TransformSignature(model) + "|" + AssetDatabase.GetAssetPath(renderer.sharedMesh) + "|" +
                   Vec(collider.center) + "|" + Vec(collider.size) + "|" +
                   string.Join(",", slot.GetComponents<Component>()
                       .Where(component => component != null)
                       .Select(component => component.GetType().FullName)
                       .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static string OtherFugaSignature(Transform placementRoot)
        {
            var builder = new StringBuilder();
            foreach (Transform child in placementRoot)
            {
                if (child.name == MoveSlotName || child.name == MoveHoverTargetName)
                {
                    continue;
                }

                AppendHierarchySignature(builder, child);
                if (!child.name.StartsWith("Fuga_", StringComparison.Ordinal) ||
                    child.name.EndsWith("HoverTarget", StringComparison.Ordinal))
                {
                    continue;
                }

                var animator = child.GetComponent<Animator>();
                var driver = child.GetComponent<FugaPhysicsMotionDriver>();
                builder.Append("Animator|").Append(child.name).Append('|')
                    .Append(animator != null && animator.enabled).Append('|')
                    .Append(animator != null && animator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                        : string.Empty).AppendLine();
                builder.Append("Driver|").Append(child.name).Append('|')
                    .Append(driver != null && driver.IdleHoverEnabled).Append('|')
                    .Append(driver != null ? Num(driver.IdleHoverAmplitude) : string.Empty).Append('|')
                    .Append(driver != null ? Num(driver.IdleHoverFrequency) : string.Empty).AppendLine();
                foreach (var renderer in child.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    builder.Append("Mesh|").Append(HierarchyPath(renderer.transform)).Append('|')
                        .Append(mesh != null ? AssetDatabase.GetAssetPath(mesh) : string.Empty).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string PlayerPreservedSignature(Transform player)
        {
            var builder = new StringBuilder()
                .Append(Vec(player.localScale)).Append('|')
                .Append(player.GetSiblingIndex()).Append('|')
                .Append(string.Join(",", player.GetComponents<Component>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().FullName)
                    .OrderBy(value => value, StringComparer.Ordinal)))
                .AppendLine();
            foreach (var child in player.GetComponentsInChildren<Transform>(true).Where(child => child != player))
            {
                builder.Append(HierarchyPath(child)).Append('|')
                    .Append(Vec(child.localPosition)).Append('|')
                    .Append(Vec(child.localEulerAngles)).Append('|')
                    .Append(Vec(child.localScale)).Append('|')
                    .Append(child.GetSiblingIndex()).Append('|')
                    .Append(child.gameObject.activeSelf).AppendLine();
            }

            return builder.ToString();
        }

        private static string[] OtherRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName && root.name != PlayerName)
                .Select(root => HierarchySignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            AppendHierarchySignature(builder, root);
            return builder.ToString();
        }

        private static void AppendHierarchySignature(StringBuilder builder, Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(HierarchyPath(transform)).Append('|')
                    .Append(transform.GetSiblingIndex()).Append('|')
                    .Append(Vec(transform.localPosition)).Append('|')
                    .Append(Vec(transform.localEulerAngles)).Append('|')
                    .Append(Vec(transform.localScale)).Append('|')
                    .Append(transform.gameObject.activeSelf).AppendLine();
            }
        }

        private static string TransformSignature(Transform transform)
        {
            return transform.name + "|" + Vec(transform.localPosition) + "|" +
                   Vec(transform.localEulerAngles) + "|" + Vec(transform.localScale);
        }

        private static void CaptureComparison(Transform slot, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga move capture path."));
            var scene = SceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            Texture2D composite = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            try
            {
                cameraObject = new GameObject("FugaMoveCaptureCamera", typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lightObject = new GameObject("FugaMoveCaptureLight", typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
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

                var panelWidth = CaptureWidth / 2;
                var panelHeight = CaptureHeight / 2;
                composite = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                var times = new[] { 0f, 1f / 3f, 2f / 3f, 1f };
                var playerCamera = RequireRoot(PlayerName).GetComponentInChildren<Camera>(true) ??
                                   throw new InvalidOperationException("The Player camera is missing.");
                var baseBounds = BoundsOf(slot);
                var direction = (baseBounds.center - playerCamera.transform.position).normalized;
                AnimationMode.StartAnimationMode();
                for (var index = 0; index < times.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(slot.gameObject, clip, times[index]);
                    AnimationMode.EndSampling();
                    var bounds = BoundsOf(slot);
                    camera.transform.position = bounds.center - direction * 10f;
                    camera.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    camera.orthographicSize = Mathf.Max(
                        bounds.extents.y * 1.3f,
                        bounds.extents.x * 1.3f / (panelWidth / (float)panelHeight));
                    var panel = Render(camera, panelWidth, panelHeight);
                    var x = index % 2 * panelWidth;
                    var y = (1 - index / 2) * panelHeight;
                    composite.SetPixels(x, y, panelWidth, panelHeight, panel.GetPixels());
                    UnityEngine.Object.DestroyImmediate(panel);
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
                throw new InvalidOperationException("The temporary Fuga move capture changed the scene dirty state.");
            }
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

        private static void WriteReport(MoveResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Move Motion Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + MoveSlotName)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("ExistingMoveAnimationUsed=False")
                .AppendLine("LoopDurationSeconds=2.000")
                .AppendLine("WingbeatFrequencyHz=1.500")
                .AppendLine("WingbeatsPerLoop=3")
                .AppendLine("HalfStrokeIntervalSeconds=0.333333")
                .AppendLine("WingCurveKeyCountPerQuaternionComponent=7")
                .AppendLine("UpstrokeAngleDegrees=44.000")
                .AppendLine("DownstrokeAngleDegrees=-40.000")
                .AppendLine("TotalShoulderStrokeDegrees=84.000")
                .AppendLine("ForwardTiltDegrees=" + ForwardTiltAngle.ToString("0.000", CultureInfo.InvariantCulture))
                .AppendLine("ForwardTiltOwner=Fuga_ModelLocalRotationCurves")
                .AppendLine("SlotRootAnimationCurves=0")
                .AppendLine("HoverAmplitudeMeters=0.015")
                .AppendLine("HoverFrequencyHz=1.500")
                .AppendLine("HoverFrequencyMatchesWingbeat=True")
                .AppendLine("HoverRootMovement=RigidbodyVelocityInFixedUpdate")
                .AppendLine("HoverTarget=" + MoveHoverTargetName)
                .AppendLine("HoverTargetCurrentLocalY=" + Num(result.HoverTargetLocalY))
                .AppendLine("ForwardTranslation=False")
                .AppendLine("PlayerHorizontalDistanceMeters=" + Num(result.PlayerHorizontalDistance))
                .AppendLine("LineupCenter=" + Vec(result.LineupCenter))
                .AppendLine("PlayerPosition=" + Vec(result.PlayerPosition))
                .AppendLine("PlayerFacesLineup=True")
                .AppendLine("IdleMotionChanged=False")
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("PlacementOrderChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("CaptureSampleTimesSeconds=0,0.333333,0.666667,1")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga move report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void WriteTilt15Report(MoveResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Move Tilt 15 Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + MoveSlotName)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("ForwardTiltDegrees=15.000")
                .AppendLine("ForwardTiltOwner=Fuga_ModelLocalRotationCurves")
                .AppendLine("WingsInheritModelTilt=True")
                .AppendLine("WingTiltOwner=Fuga_ModelParentRotationCurves")
                .AppendLine("WingLocalFlapCurvesChanged=False")
                .AppendLine("WingbeatFrequencyHz=1.500")
                .AppendLine("WingbeatsPerLoop=3")
                .AppendLine("HalfStrokeIntervalSeconds=0.333333")
                .AppendLine("WingCurveKeyCountPerQuaternionComponent=7")
                .AppendLine("UpstrokeAngleDegrees=44.000")
                .AppendLine("DownstrokeAngleDegrees=-40.000")
                .AppendLine("TotalShoulderStrokeDegrees=84.000")
                .AppendLine("HoverAmplitudeMeters=0.015")
                .AppendLine("HoverFrequencyHz=1.500")
                .AppendLine("HoverFrequencyMatchesWingbeat=True")
                .AppendLine("ForwardTranslation=False")
                .AppendLine("PlayerHorizontalDistanceMeters=" + Num(result.PlayerHorizontalDistance))
                .AppendLine("PlayerFacesLineup=True")
                .AppendLine("IdleMotionChanged=False")
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("PlacementOrderChanged=False")
                .AppendLine("ControllerChanged=False")
                .AppendLine("SceneChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("CaptureSampleTimesSeconds=0,0.333333,0.666667,1")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(Tilt15ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga move-tilt report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static Transform FindBone(SkinnedMeshRenderer renderer, string name)
        {
            var matches = renderer.bones.Where(bone => bone != null && bone.name == name).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Expected exactly one Fuga bone named " + name + ".");
            }

            return matches[0];
        }

        private static Transform RequireRoot(string name)
        {
            var gameObject = GameObject.Find(name) ??
                             throw new InvalidOperationException(name + " is missing.");
            if (gameObject.transform.parent != null)
            {
                throw new InvalidOperationException(name + " is not a scene root.");
            }

            return gameObject.transform;
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = parent.Cast<Transform>().Where(child => child.name == name).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one direct child " + parent.name + "/" + name + ".");
            }

            return matches[0];
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be active. ActiveScene=" + scene.path + ".");
            }

            return scene;
        }

        private static string RelativePath(Transform root, Transform target)
        {
            var names = new Stack<string>();
            var cursor = target;
            while (cursor != null && cursor != root)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }

            if (cursor != root)
            {
                throw new InvalidOperationException(target.name + " is not below " + root.name + ".");
            }

            return string.Join("/", names);
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            var cursor = transform;
            while (cursor != null)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }

            return string.Join("/", names);
        }

        private static void RequireModelHashes()
        {
            RequireHash(ExpectedModelSha256, Sha256(SourceModelPath), "source Fuga GLB");
            RequireHash(ExpectedImportedRigSha256, Sha256(Absolute(ImportedModelPath)), "imported lip-rig Fuga GLB");
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    label + " SHA-256 mismatch. Expected=" + expected + ", Actual=" + actual + ".");
            }
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct MoveResult
        {
            public MoveResult(
                Transform slot,
                AnimationClip clip,
                float playerHorizontalDistance,
                float hoverTargetLocalY,
                Vector3 lineupCenter,
                Vector3 playerPosition)
            {
                Slot = slot;
                Clip = clip;
                PlayerHorizontalDistance = playerHorizontalDistance;
                HoverTargetLocalY = hoverTargetLocalY;
                LineupCenter = lineupCenter;
                PlayerPosition = playerPosition;
            }

            public Transform Slot { get; }
            public AnimationClip Clip { get; }
            public float PlayerHorizontalDistance { get; }
            public float HoverTargetLocalY { get; }
            public Vector3 LineupCenter { get; }
            public Vector3 PlayerPosition { get; }
        }
    }
}
