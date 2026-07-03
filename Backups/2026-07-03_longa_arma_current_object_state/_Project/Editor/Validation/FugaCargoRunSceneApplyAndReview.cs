using System;
using System.Collections.Generic;
using System.IO;
using Bellerophon.Enemies.Fuga;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.FugaCargoRunScene
{
    internal static class FugaCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string CorridorRootName = "Approved Ship Corridor Segments";
        private const string ParvumPlacementRootName = "Approved Parvum Enemy Placement";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string ReviewCameraName = "Model Cam";
        private const string PlayerRootName = "Player";

        private const string SampleRootRelativePath = "artSample/enemies/fuga";
        private const string SourceModelRelativePath = SampleRootRelativePath + "/exports/fuga_sample.fbx";
        private const string SourceTextureRootRelativePath = SampleRootRelativePath + "/textures";

        private const string FugaArtRoot = "Assets/_Project/Art/Enemies/Fuga";
        private const string UnityModelFolder = FugaArtRoot + "/Models";
        private const string UnityMaterialFolder = FugaArtRoot + "/Materials";
        private const string UnityTextureFolder = FugaArtRoot + "/Textures";
        private const string UnityAnimationFolder = FugaArtRoot + "/Animations";
        private const string UnityControllerFolder = FugaArtRoot + "/Controllers";
        private const string UnityModelAssetPath = UnityModelFolder + "/fuga_sample.fbx";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Enemies/Fuga";
        private const string PrefabPath = PrefabFolder + "/FugaApproved.prefab";
        private const string ReviewOutputRelativePath = "docs/validation/fuga_cargo_run_scene";

        private const string ModelChildName = "FugaApproved_Model";
        private const string MotionTargetRootName = "Fuga_Physics_Motion_Helper_Targets";
        private const string MotionTargetName = "MotionPath_Target_Rigidbody_Goal";

        private const float FugaTargetHeightMeters = 0.60f;
        private const float FugaTargetWidthMeters = 0.40f;
        private const float FugaTargetDepthMeters = 0.20f;
        private const float FugaMinimumSceneScale = 0.001f;
        private const float FugaMaximumSceneScale = 120f;
        private const float FugaRequestedUniformScale = 0.25f;
        private const float FugaFacingYawDegrees = 180f;
        private const float FugaPlacementSpacing = 2.45f;
        private const float FugaPlacementMinimumXClearance = 0.35f;
        private const float FugaParvumMinimumRootZGap = 0.30f;
        private const float FugaFlightHeight = 1.05f;
        private const float FugaReviewCameraMinimumFrontDistance = 5.25f;
        private const float FugaReviewCameraMaximumFrontDistance = 7.50f;
        private const float FugaReviewPlayerFrontDistance = 4.35f;
        private const float FugaWingRootAttachLocalX = 0.42f;
        private const float FugaWingRootAttachLocalY = 1.08f;
        private const float FugaWingRootAttachLocalZ = -0.18f;
        private const float ApprovedWingPanelThickness = 0.14f;
        private static readonly Vector3 ReviewDeathFallVelocity = new Vector3(0f, -1.18f, 0f);
        private const float ReviewDeathFallDuration = 0.68f;
        private const float ReviewDeathImpactSettleDuration = 0.02f;
        private const float ReviewDeathFinalHoldDuration = 1.30f;
        private static readonly Vector3 FugaModelFacingEuler = Vector3.zero;

        private static readonly PlacementSpec[] PlacementSpecs =
        {
            new PlacementSpec("Fuga_00_Static", null, 0f, false, "Static comparison pose"),
            new PlacementSpec("Fuga_01_Idle", "Fuga_Idle_SlowWingbeat", 0.42f, true, "Idle: vertical up/down wing flap beside the body"),
            new PlacementSpec("Fuga_02_Move", "Fuga_Move_FastWingbeat", 0.24f, true, "Move: faster vertical up/down wing flap beside the body"),
            new PlacementSpec("Fuga_03_Attack", "Fuga_Attack_WingtipStrike", 0.48f, true, "Attack: attached wings lift apart, then wingtips swat across the front"),
            new PlacementSpec("Fuga_04_Hit", "Fuga_Hit_SquashRecoil", 0.24f, true, "Hit: recoil, squash recovery, wing droop"),
            new PlacementSpec("Fuga_05_Death", "Fuga_Death_FallAndFold", 0f, true, "Death review: looping hover, sharp tilt, fast Rigidbody fall, hard stop, final still hold, reset"),
            new PlacementSpec("Fuga_06_Consume", "Fuga_Consume_BiteForward", 0.50f, true, "Consume: connected lower jaw opens, body leans forward, mouth closes")
        };

        private static readonly string[] TextureFileNames =
        {
            "fuga2_body_wart_bump.png",
            "fuga2_golden_eye_albedo.png",
            "fuga2_inner_brown_olive_feather_albedo.png",
            "fuga2_lower_shell_leaf_albedo.png",
            "fuga2_olive_feather_albedo.png",
            "fuga2_wet_green_bumpy_body_albedo.png"
        };

        private static readonly string[] VisualFrontFeatureNames =
        {
            "Fuga2_Broad_Front_Snout_Bulge_With_Wavy_Mouth",
            "Fuga2_Subtle_Dark_Wavy_Mouth_Recess",
            "Fuga2_Left_Golden_Vertical_Slit_Eye",
            "Fuga2_Right_Golden_Vertical_Slit_Eye"
        };

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Approved Sample To CargoRunMvp")]
        public static void ApplyApprovedSampleToCurrentCargoRunScene()
        {
            RequireApprovedSampleFiles();
            EnsureUnityFolders();
            CopyApprovedSampleAssets();
            ConfigureImportedAssets();

            var materialSet = EnsureMaterials();
            var prefab = EnsurePrefab(materialSet);
            var sceneScale = Vector3.one * FugaRequestedUniformScale;
            var clips = EnsureAnimationClips(prefab, sceneScale);
            var controllers = EnsureAnimatorControllers(clips);

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlaceFugaReviewObjects(prefab, clips, controllers, sceneScale);
            ConfigureInitialFugaReviewCamera(placementRoot.transform);
            ConfigureInitialFugaPlayerStart(placementRoot.transform);
            WriteReviewFiles(scene, placementRoot, sceneScale);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Approved Fuga sample applied to CargoRunMvp scene.");
        }

        public static void InspectAppliedSceneState()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var root = GameObject.Find(PlacementRootName);
            if (root == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing in {scene.path}.");
            }

            if (root.transform.childCount != PlacementSpecs.Length)
            {
                throw new InvalidOperationException(
                    $"{PlacementRootName} must contain {PlacementSpecs.Length} Fuga review objects, but found {root.transform.childCount}.");
            }

            foreach (var spec in PlacementSpecs)
            {
                var child = root.transform.Find(spec.ObjectName);
                if (child == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing under {PlacementRootName}.");
                }

                if (child.GetComponent<Rigidbody>() == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing Rigidbody.");
                }

                if (child.GetComponent<Collider>() == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing Collider.");
                }

                var driver = child.GetComponent<FugaPhysicsMotionDriver>();
                if (driver == null || driver.MotionPathTarget == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing Fuga physics motion target wiring.");
                }

                var animator = child.GetComponent<Animator>();
                if (spec.ClipName == null)
                {
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        throw new InvalidOperationException($"{spec.ObjectName} should remain a static comparison object.");
                    }
                }
                else if (animator == null || animator.runtimeAnimatorController == null)
                {
                    throw new InvalidOperationException($"{spec.ObjectName} is missing Animator controller for {spec.ClipName}.");
                }

                if (spec.ClipName != null)
                {
                    var reviewPlayback = child.GetComponent<FugaAnimationReviewPlaybackDriver>();
                    if (reviewPlayback == null || reviewPlayback.Clip == null)
                    {
                        throw new InvalidOperationException($"{spec.ObjectName} is missing Fuga review playback driver for {spec.ClipName}.");
                    }

                    if (!string.Equals(reviewPlayback.Clip.name, spec.ClipName, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"{spec.ObjectName} review playback clip must be {spec.ClipName}, but was {reviewPlayback.Clip.name}.");
                    }
                }
            }

            InspectRequestedScaleAndFacing(root.transform);
            InspectFugaXSeparation(root.transform);
            InspectFugaReviewCamera(root.transform);
            InspectFugaPlayerStart(root.transform);
            InspectParvumZSeparation(root.transform);
            InspectFugaAnimationContracts(root.transform);

            Debug.Log("Approved Fuga CargoRunMvp scene state inspected.");
        }

        private static void InspectStaticSize(Transform placementRoot)
        {
            var staticObject = placementRoot.Find("Fuga_00_Static");
            if (staticObject == null)
            {
                return;
            }

            var bounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            var size = bounds.size;
            var horizontalLong = Mathf.Max(size.x, size.z);
            var horizontalShort = Mathf.Min(size.x, size.z);
            const float tolerance = 0.035f;

            if (size.y > FugaTargetHeightMeters + tolerance ||
                horizontalLong > FugaTargetWidthMeters + tolerance ||
                horizontalShort > FugaTargetDepthMeters + tolerance)
            {
                throw new InvalidOperationException(
                    $"Fuga static bounds exceed design size. Measured={size}, targetHxWxD={FugaTargetHeightMeters}x{FugaTargetWidthMeters}x{FugaTargetDepthMeters}m.");
            }
        }

        private static void InspectRequestedScaleAndFacing(Transform placementRoot)
        {
            const float scaleTolerance = 0.001f;
            const float yawTolerance = 0.5f;

            foreach (var spec in PlacementSpecs)
            {
                var child = placementRoot.Find(spec.ObjectName);
                if (child == null)
                {
                    continue;
                }

                var scale = child.localScale;
                if (Mathf.Abs(scale.x - FugaRequestedUniformScale) > scaleTolerance ||
                    Mathf.Abs(scale.y - FugaRequestedUniformScale) > scaleTolerance ||
                    Mathf.Abs(scale.z - FugaRequestedUniformScale) > scaleTolerance)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} scale must be {FugaRequestedUniformScale:0.###}, but was {scale}.");
                }

                var yaw = child.eulerAngles.y;
                if (Mathf.Abs(Mathf.DeltaAngle(yaw, FugaFacingYawDegrees)) > yawTolerance)
                {
                    throw new InvalidOperationException(
                        $"{spec.ObjectName} yaw must be {FugaFacingYawDegrees:0.###}, but was {yaw:0.###}.");
                }
            }
        }

        private static void InspectFugaXSeparation(Transform placementRoot)
        {
            var boundsList = BuildFugaBoundsSortedByX(placementRoot);
            for (var i = 0; i < boundsList.Count - 1; i++)
            {
                var xGap = boundsList[i + 1].min.x - boundsList[i].max.x;
                if (xGap < FugaPlacementMinimumXClearance)
                {
                    throw new InvalidOperationException(
                        $"Fuga review objects overlap or are too close on X. Gap={xGap:0.###}m, required={FugaPlacementMinimumXClearance:0.###}m.");
                }
            }
        }

        private static float CalculateMinimumFugaXGap(Transform placementRoot)
        {
            var boundsList = BuildFugaBoundsSortedByX(placementRoot);
            if (boundsList.Count < 2)
            {
                return 0f;
            }

            var minimumGap = float.MaxValue;
            for (var i = 0; i < boundsList.Count - 1; i++)
            {
                minimumGap = Mathf.Min(minimumGap, boundsList[i + 1].min.x - boundsList[i].max.x);
            }

            return minimumGap;
        }

        private static List<Bounds> BuildFugaBoundsSortedByX(Transform placementRoot)
        {
            var boundsList = new List<Bounds>(PlacementSpecs.Length);
            foreach (var spec in PlacementSpecs)
            {
                var child = placementRoot.Find(spec.ObjectName);
                if (child == null)
                {
                    continue;
                }

                boundsList.Add(CalculateRendererBounds(child, new Bounds(child.position, Vector3.zero)));
            }

            boundsList.Sort((left, right) => left.center.x.CompareTo(right.center.x));
            return boundsList;
        }

        private static void InspectFugaReviewCamera(Transform placementRoot)
        {
            var focus = FindFugaCameraFocus(placementRoot);
            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException("Fuga review camera is missing.");
            }

            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center;
            var expectedFront = CalculateFugaVisualFrontDirection(focus);
            var cameraFromFocus = camera.transform.position - lookAt;
            cameraFromFocus.y = 0f;

            if (cameraFromFocus.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("Fuga review camera is too close to the focus point.");
            }

            var actualFront = cameraFromFocus.normalized;
            if (Vector3.Dot(expectedFront, actualFront) < 0.96f)
            {
                throw new InvalidOperationException(
                    $"Fuga review camera is not placed on the current visual front. Expected={expectedFront}, actual={actualFront}.");
            }

            var frontDistance = Vector3.Dot(camera.transform.position - lookAt, expectedFront);
            if (frontDistance < FugaReviewCameraMinimumFrontDistance - 0.05f)
            {
                throw new InvalidOperationException(
                    $"Fuga review camera is too close to the current visual front. Distance={frontDistance:0.###}m, required={FugaReviewCameraMinimumFrontDistance:0.###}m.");
            }

            var toFocus = (lookAt - camera.transform.position).normalized;
            if (Vector3.Dot(camera.transform.forward, toFocus) < 0.96f)
            {
                throw new InvalidOperationException("Fuga review camera is not looking at the Fuga focus point.");
            }
        }

        private static void InspectFugaPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Player start transform is missing.");
            }

            var focus = FindFugaCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center;
            var expectedFront = CalculateFugaVisualFrontDirection(focus);
            var playerFromFocus = player.position - lookAt;
            playerFromFocus.y = 0f;

            if (playerFromFocus.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException("Player start is too close to the Fuga focus point.");
            }

            var actualFront = playerFromFocus.normalized;
            if (Vector3.Dot(expectedFront, actualFront) < 0.96f)
            {
                throw new InvalidOperationException(
                    $"Player start is not placed on the current Fuga visual front. Expected={expectedFront}, actual={actualFront}.");
            }

            var frontDistance = Vector3.Dot(player.position - lookAt, expectedFront);
            if (frontDistance < FugaReviewPlayerFrontDistance - 0.05f)
            {
                throw new InvalidOperationException(
                    $"Player start is too close to Fuga front. Distance={frontDistance:0.###}m, required={FugaReviewPlayerFrontDistance:0.###}m.");
            }

            var toFocus = lookAt - player.position;
            toFocus.y = 0f;
            if (toFocus.sqrMagnitude < 0.001f || Vector3.Dot(player.forward, toFocus.normalized) < 0.96f)
            {
                throw new InvalidOperationException("Player start is not facing the Fuga front.");
            }
        }

        private static void InspectParvumZSeparation(Transform placementRoot)
        {
            var parvumRoot = GameObject.Find(ParvumPlacementRootName);
            if (parvumRoot == null)
            {
                return;
            }

            var zGap = CalculateParvumZGap(placementRoot);
            var desiredGap = CalculateDesiredFugaParvumZGap();
            const float overlapTolerance = 0.01f;
            const float placementTolerance = 0.05f;

            if (zGap < FugaParvumMinimumRootZGap - overlapTolerance)
            {
                throw new InvalidOperationException(
                    $"Fuga overlaps or is too close to Parvum on root Z. Gap={zGap:0.###}m, required={FugaParvumMinimumRootZGap:0.###}m.");
            }

            if (Mathf.Abs(zGap - desiredGap) > placementTolerance)
            {
                throw new InvalidOperationException(
                    $"Fuga is not using the corridor-Parvum root Z gap. Gap={zGap:0.###}m, desired={desiredGap:0.###}m.");
            }
        }

        private static void InspectWingRootAttachment(Transform root)
        {
            var leftWing = FindChildRecursive(root, "Fuga2_Left_Wing_Root_For_Pose");
            var rightWing = FindChildRecursive(root, "Fuga2_Right_Wing_Root_For_Pose");
            RequireWingRootAttachment(leftWing, "left");
            RequireWingRootAttachment(rightWing, "right");
        }

        private static void RequireWingRootAttachment(Transform wingRoot, string label)
        {
            if (wingRoot == null)
            {
                throw new InvalidOperationException($"Fuga {label} wing root is missing.");
            }

            var expected = BuildWingRootAttachedLocalPosition(wingRoot.localPosition.x);
            if (Vector3.Distance(wingRoot.localPosition, expected) > 0.015f)
            {
                throw new InvalidOperationException(
                    $"Fuga {label} wing root must stay attached to the body side. Actual={wingRoot.localPosition}, expected={expected}.");
            }
        }

        private static void InspectFugaAnimationContracts(Transform placementRoot)
        {
            var death = placementRoot.Find("Fuga_05_Death");
            if (death == null)
            {
                throw new InvalidOperationException("Fuga_05_Death is missing.");
            }

            var attack = placementRoot.Find("Fuga_03_Attack");
            if (attack == null)
            {
                throw new InvalidOperationException("Fuga_03_Attack is missing.");
            }

            InspectWingRootAttachment(attack);

            var deathDriver = death.GetComponent<FugaPhysicsMotionDriver>();
            var deathBody = death.GetComponent<Rigidbody>();
            if (deathDriver == null || deathBody == null)
            {
                throw new InvalidOperationException("Fuga_05_Death must keep Rigidbody and FugaPhysicsMotionDriver components.");
            }

            if (!deathDriver.UseDeathFallSequence || !deathDriver.FollowVerticalAxis || !deathDriver.LoopDeathFallForReview || deathDriver.LockRootMotionForReview)
            {
                throw new InvalidOperationException("Fuga_05_Death must use the looping Rigidbody death fall sequence with vertical following enabled.");
            }

            if (deathBody.isKinematic)
            {
                throw new InvalidOperationException("Fuga_05_Death Rigidbody must be non-kinematic so the death fall is physics-driven.");
            }

            var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAnimationFolder + "/Fuga_Idle_SlowWingbeat.anim");
            var moveClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAnimationFolder + "/Fuga_Move_FastWingbeat.anim");
            var attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAnimationFolder + "/Fuga_Attack_WingtipStrike.anim");
            var hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAnimationFolder + "/Fuga_Hit_SquashRecoil.anim");
            var deathClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAnimationFolder + "/Fuga_Death_FallAndFold.anim");
            var consumeClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnityAnimationFolder + "/Fuga_Consume_BiteForward.anim");
            RequireClipLoopSetting(idleClip, true, "Fuga_Idle_SlowWingbeat");
            RequireClipLoopSetting(moveClip, true, "Fuga_Move_FastWingbeat");
            RequireClipLoopSetting(attackClip, true, "Fuga_Attack_WingtipStrike");
            RequireClipLoopSetting(hitClip, true, "Fuga_Hit_SquashRecoil");
            RequireClipLoopSetting(deathClip, true, "Fuga_Death_FallAndFold");
            RequireClipLoopSetting(consumeClip, true, "Fuga_Consume_BiteForward");
            RequireWingVerticalFlapCurve(idleClip, "Fuga_Idle_SlowWingbeat");
            RequireWingVerticalFlapCurve(moveClip, "Fuga_Move_FastWingbeat");
            RequireCurvePeak(hitClip, ModelChildName, "localEulerAnglesRaw.z", 18f, "Fuga_Hit_SquashRecoil visible hit recoil");
            RequireCurvePeak(attackClip, ModelChildName, "localPosition.z", MetersToLocal(0.16f, FugaRequestedUniformScale), "Fuga_Attack_WingtipStrike forward body drive");
            RequireCurveSpan(attackClip, "Fuga2_Left_Wing_Root_For_Pose", "localEulerAnglesRaw.z", 110f, "Fuga_Attack_WingtipStrike left wingtip frontal swat arc");
            RequireCurveSpan(attackClip, "Fuga2_Right_Wing_Root_For_Pose", "localEulerAnglesRaw.z", 110f, "Fuga_Attack_WingtipStrike right wingtip frontal swat arc");
            RejectCurve(attackClip, "Fuga2_Left_Wing_Root_For_Pose", "localPosition.x", "Fuga_Attack_WingtipStrike must keep the left wing root attached to the body.");
            RejectCurve(attackClip, "Fuga2_Left_Wing_Root_For_Pose", "localPosition.y", "Fuga_Attack_WingtipStrike must keep the left wing root attached to the body.");
            RejectCurve(attackClip, "Fuga2_Left_Wing_Root_For_Pose", "localPosition.z", "Fuga_Attack_WingtipStrike must keep the left wing root attached to the body.");
            RejectCurve(attackClip, "Fuga2_Right_Wing_Root_For_Pose", "localPosition.x", "Fuga_Attack_WingtipStrike must keep the right wing root attached to the body.");
            RejectCurve(attackClip, "Fuga2_Right_Wing_Root_For_Pose", "localPosition.y", "Fuga_Attack_WingtipStrike must keep the right wing root attached to the body.");
            RejectCurve(attackClip, "Fuga2_Right_Wing_Root_For_Pose", "localPosition.z", "Fuga_Attack_WingtipStrike must keep the right wing root attached to the body.");
            RejectCurvePeakAbove(attackClip, "Fuga2_Left_Wing_Root_For_Pose", "localEulerAnglesRaw.y", 8f, "Fuga_Attack_WingtipStrike must not lift the left wing over the body with yaw.");
            RejectCurvePeakAbove(attackClip, "Fuga2_Right_Wing_Root_For_Pose", "localEulerAnglesRaw.y", 8f, "Fuga_Attack_WingtipStrike must not lift the right wing over the body with yaw.");
            RequireWingVerticalFlapCurve(deathClip, "Fuga_Death_FallAndFold");
            RequireCurve(deathClip, ModelChildName, "localEulerAnglesRaw.z", "Fuga_Death_FallAndFold body tilt");
            RequireCurvePeak(deathClip, ModelChildName, "localEulerAnglesRaw.z", 50f, "Fuga_Death_FallAndFold stronger final body tilt");
            RequireCurvePeak(consumeClip, "Fuga2_Continuous_Lower_Jaw_To_Chest_Join", "localEulerAnglesRaw.x", 62f, "Fuga_Consume_BiteForward wide lower jaw opening");
            RequireAnyCurvePeakOnPath(consumeClip, "Fuga2_Subtle_Dark_Wavy_Mouth_Recess", 2.0f, "Fuga_Consume_BiteForward visible mouth opening scale");
            RejectCurve(consumeClip, "Fuga2_Continuous_Lower_Jaw_To_Chest_Join", "localPosition.x", "Fuga_Consume_BiteForward must keep the lower jaw connected without localPosition detachment.");
            RejectCurve(consumeClip, "Fuga2_Continuous_Lower_Jaw_To_Chest_Join", "localPosition.y", "Fuga_Consume_BiteForward must keep the lower jaw connected without localPosition detachment.");
            RejectCurve(consumeClip, "Fuga2_Continuous_Lower_Jaw_To_Chest_Join", "localPosition.z", "Fuga_Consume_BiteForward must keep the lower jaw connected without localPosition detachment.");
            RejectCurve(deathClip, ModelChildName, "localPosition.y", "Fuga_Death_FallAndFold must not fake falling by moving the model child.");

            if (deathDriver.DeathFallVelocity.y > -1f ||
                deathDriver.DeathFallDuration > 0.75f ||
                deathDriver.DeathImpactSettleDuration > 0.05f ||
                deathDriver.DeathFinalHoldDuration < 1.00f)
            {
                throw new InvalidOperationException("Fuga_05_Death must use the fast fall, immediate stop, and final still hold death profile.");
            }
        }

        private static void RequireWingVerticalFlapCurve(AnimationClip clip, string label)
        {
            RequireCurve(clip, "Fuga2_Left_Wing_Root_For_Pose", "localEulerAnglesRaw.z", label + " left vertical wing flap");
            RequireCurve(clip, "Fuga2_Right_Wing_Root_For_Pose", "localEulerAnglesRaw.z", label + " right vertical wing flap");
        }

        private static void RequireCurve(AnimationClip clip, string pathContains, string propertyName, string label)
        {
            if (clip == null)
            {
                throw new InvalidOperationException($"{label} clip is missing.");
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path.Contains(pathContains, StringComparison.Ordinal) &&
                    IsCompatibleCurveProperty(binding.propertyName, propertyName))
                {
                    return;
                }
            }

            throw new InvalidOperationException($"{label} is missing required curve {propertyName} on path containing {pathContains}.");
        }

        private static void RequireClipLoopSetting(AnimationClip clip, bool expectedLoop, string label)
        {
            if (clip == null)
            {
                throw new InvalidOperationException($"{label} clip is missing.");
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime != expectedLoop)
            {
                throw new InvalidOperationException($"{label} loopTime must be {expectedLoop}.");
            }
        }

        private static void RequireCurvePeak(AnimationClip clip, string pathContains, string propertyName, float minimumAbsoluteValue, string label)
        {
            if (clip == null)
            {
                throw new InvalidOperationException($"{label} clip is missing.");
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.path.Contains(pathContains, StringComparison.Ordinal) ||
                    !IsCompatibleCurveProperty(binding.propertyName, propertyName))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                foreach (var key in curve.keys)
                {
                    if (Mathf.Abs(key.value) >= minimumAbsoluteValue)
                    {
                        return;
                    }
                }
            }

            throw new InvalidOperationException(
                $"{label} must reach at least {minimumAbsoluteValue:0.###} degrees/units on {propertyName}.");
        }

        private static void RequireCurveSpan(AnimationClip clip, string pathContains, string propertyName, float minimumSpan, string label)
        {
            if (clip == null)
            {
                throw new InvalidOperationException($"{label} clip is missing.");
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.path.Contains(pathContains, StringComparison.Ordinal) ||
                    !IsCompatibleCurveProperty(binding.propertyName, propertyName))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0)
                {
                    continue;
                }

                var minimum = float.PositiveInfinity;
                var maximum = float.NegativeInfinity;
                foreach (var key in curve.keys)
                {
                    minimum = Mathf.Min(minimum, key.value);
                    maximum = Mathf.Max(maximum, key.value);
                }

                if (maximum - minimum >= minimumSpan)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"{label} must span at least {minimumSpan:0.###} units on {propertyName}.");
        }

        private static void RequireAnyCurvePeakOnPath(AnimationClip clip, string pathContains, float minimumAbsoluteValue, string label)
        {
            if (clip == null)
            {
                throw new InvalidOperationException($"{label} clip is missing.");
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.path.Contains(pathContains, StringComparison.Ordinal))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                foreach (var key in curve.keys)
                {
                    if (Mathf.Abs(key.value) >= minimumAbsoluteValue)
                    {
                        return;
                    }
                }
            }

            throw new InvalidOperationException(
                $"{label} must have a curve on path containing {pathContains} that reaches at least {minimumAbsoluteValue:0.###}.");
        }

        private static bool IsCompatibleCurveProperty(string actualPropertyName, string expectedPropertyName)
        {
            if (string.Equals(actualPropertyName, expectedPropertyName, StringComparison.Ordinal))
            {
                return true;
            }

            return string.Equals(actualPropertyName, ToSerializedTransformProperty(expectedPropertyName), StringComparison.Ordinal);
        }

        private static string ToSerializedTransformProperty(string propertyName)
        {
            if (propertyName.StartsWith("localPosition.", StringComparison.Ordinal))
            {
                return "m_LocalPosition." + propertyName[propertyName.LastIndexOf('.') + 1];
            }

            if (propertyName.StartsWith("localScale.", StringComparison.Ordinal))
            {
                return "m_LocalScale." + propertyName[propertyName.LastIndexOf('.') + 1];
            }

            return propertyName;
        }

        private static void RejectCurve(AnimationClip clip, string pathContains, string propertyName, string reason)
        {
            if (clip == null)
            {
                throw new InvalidOperationException("Animation clip is missing while checking rejected curves.");
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path.Contains(pathContains, StringComparison.Ordinal) &&
                    IsCompatibleCurveProperty(binding.propertyName, propertyName))
                {
                    throw new InvalidOperationException(reason);
                }
            }
        }

        private static void RejectCurvePeakAbove(AnimationClip clip, string pathContains, string propertyName, float maximumAbsoluteValue, string reason)
        {
            if (clip == null)
            {
                throw new InvalidOperationException("Animation clip is missing while checking rejected curve peaks.");
            }

            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.path.Contains(pathContains, StringComparison.Ordinal) ||
                    !IsCompatibleCurveProperty(binding.propertyName, propertyName))
                {
                    continue;
                }

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null)
                {
                    continue;
                }

                foreach (var key in curve.keys)
                {
                    if (Mathf.Abs(key.value) > maximumAbsoluteValue)
                    {
                        throw new InvalidOperationException(reason);
                    }
                }
            }
        }

        private static void RequireApprovedSampleFiles()
        {
            var sourceModelPath = ProjectPath(SourceModelRelativePath);
            if (!File.Exists(sourceModelPath))
            {
                throw new FileNotFoundException("Approved Fuga FBX sample is missing.", sourceModelPath);
            }

            foreach (var textureFileName in TextureFileNames)
            {
                var texturePath = ProjectPath(Path.Combine(SourceTextureRootRelativePath, textureFileName));
                if (!File.Exists(texturePath))
                {
                    throw new FileNotFoundException($"Approved Fuga texture is missing: {textureFileName}", texturePath);
                }
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(FugaArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
            EnsureUnityFolder(UnityTextureFolder);
            EnsureUnityFolder(UnityAnimationFolder);
            EnsureUnityFolder(UnityControllerFolder);
            EnsureUnityFolder("Assets/_Project/Prefabs/Enemies");
            EnsureUnityFolder(PrefabFolder);
        }

        private static void CopyApprovedSampleAssets()
        {
            CopyFileToAsset(ProjectPath(SourceModelRelativePath), UnityModelAssetPath);

            foreach (var textureFileName in TextureFileNames)
            {
                CopyFileToAsset(
                    ProjectPath(Path.Combine(SourceTextureRootRelativePath, textureFileName)),
                    UnityTextureFolder + "/" + textureFileName);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImportedAssets()
        {
            var modelImporter = AssetImporter.GetAtPath(UnityModelAssetPath) as ModelImporter;
            if (modelImporter != null)
            {
                modelImporter.importCameras = false;
                modelImporter.importLights = false;
                modelImporter.importBlendShapes = true;
                modelImporter.importVisibility = false;
                modelImporter.animationType = ModelImporterAnimationType.Generic;
                modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
                modelImporter.globalScale = 1f;
                modelImporter.SaveAndReimport();
            }

            foreach (var textureFileName in TextureFileNames)
            {
                var assetPath = UnityTextureFolder + "/" + textureFileName;
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = textureFileName.Contains("bump", StringComparison.OrdinalIgnoreCase)
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;
                importer.sRGBTexture = !textureFileName.Contains("bump", StringComparison.OrdinalIgnoreCase);
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.SaveAndReimport();
            }
        }

        private static MaterialSet EnsureMaterials()
        {
            var bodyAlbedo = LoadTexture("fuga2_wet_green_bumpy_body_albedo.png");
            var bodyNormal = LoadTexture("fuga2_body_wart_bump.png");
            var wingAlbedo = LoadTexture("fuga2_olive_feather_albedo.png");
            var innerWingAlbedo = LoadTexture("fuga2_inner_brown_olive_feather_albedo.png");
            var eyeAlbedo = LoadTexture("fuga2_golden_eye_albedo.png");
            var lowerShellAlbedo = LoadTexture("fuga2_lower_shell_leaf_albedo.png");

            return new MaterialSet(
                EnsureMaterial("M_Fuga2_Wet_Green_Bumpy_Body", bodyAlbedo, bodyNormal, new Color(0.20f, 0.35f, 0.18f), 0.18f, 0f),
                EnsureMaterial("M_Fuga2_Broad_Olive_Wing", wingAlbedo, null, new Color(0.18f, 0.27f, 0.13f), 0.27f, 0f),
                EnsureMaterial("M_Fuga2_Inner_Brown_Olive_Wing", innerWingAlbedo, null, new Color(0.25f, 0.22f, 0.12f), 0.20f, 0f),
                EnsureMaterial("M_Fuga2_Golden_Eye", eyeAlbedo, null, new Color(0.95f, 0.73f, 0.18f), 0.48f, 0f),
                EnsureMaterial("M_Fuga2_Lower_Shell_Leaf", lowerShellAlbedo, bodyNormal, new Color(0.11f, 0.20f, 0.10f), 0.22f, 0f),
                EnsureMaterial("M_Fuga2_Dark_Recess", null, null, new Color(0.03f, 0.035f, 0.025f), 0.10f, 0f));
        }

        private static GameObject EnsurePrefab(MaterialSet materialSet)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (modelAsset == null)
            {
                throw new InvalidOperationException($"Could not load Fuga model asset at {UnityModelAssetPath}.");
            }

            var root = new GameObject("FugaApproved");
            try
            {
                var modelInstance = UnityEngine.Object.Instantiate(modelAsset);
                modelInstance.name = ModelChildName;
                modelInstance.transform.SetParent(root.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.Euler(FugaModelFacingEuler);
                modelInstance.transform.localScale = Vector3.one;
                NormalizeFugaWingRootAttachment(modelInstance.transform);

                AssignMaterials(root, materialSet);
                var motionTarget = EnsureMotionTargetHierarchy(root.transform);
                EnsurePrefabPhysics(root, motionTarget);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Could not create Fuga prefab at {PrefabPath}.");
            }

            return prefab;
        }

        private static void NormalizeFugaWingRootAttachment(Transform modelRoot)
        {
            ApplyWingRootAttachment(FindChildRecursive(modelRoot, "Fuga2_Left_Wing_Root_For_Pose"));
            ApplyWingRootAttachment(FindChildRecursive(modelRoot, "Fuga2_Right_Wing_Root_For_Pose"));
        }

        private static void ApplyWingRootAttachment(Transform wingRoot)
        {
            if (wingRoot == null)
            {
                return;
            }

            wingRoot.localPosition = BuildWingRootAttachedLocalPosition(wingRoot.localPosition.x);
        }

        private static Vector3 BuildWingRootAttachedLocalPosition(float currentX)
        {
            var sign = Mathf.Abs(currentX) > 0.001f ? Mathf.Sign(currentX) : 1f;
            return new Vector3(sign * FugaWingRootAttachLocalX, FugaWingRootAttachLocalY, FugaWingRootAttachLocalZ);
        }

        private static Dictionary<string, AnimationClip> EnsureAnimationClips(GameObject prefab, Vector3 sceneScale)
        {
            var paths = ResolveAnimationPaths(prefab);
            var clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

            foreach (var spec in PlacementSpecs)
            {
                if (spec.ClipName == null)
                {
                    continue;
                }

                var clip = spec.ClipName switch
                {
                    "Fuga_Idle_SlowWingbeat" => BuildIdleClip(paths, sceneScale),
                    "Fuga_Move_FastWingbeat" => BuildMoveClip(paths, sceneScale),
                    "Fuga_Attack_WingtipStrike" => BuildAttackClip(paths, sceneScale),
                    "Fuga_Hit_SquashRecoil" => BuildHitClip(paths, sceneScale),
                    "Fuga_Death_FallAndFold" => BuildDeathClip(paths, sceneScale),
                    "Fuga_Consume_BiteForward" => BuildConsumeClip(paths, sceneScale),
                    _ => throw new InvalidOperationException($"Unsupported Fuga animation clip: {spec.ClipName}.")
                };

                var clipPath = UnityAnimationFolder + "/" + spec.ClipName + ".anim";
                SaveClip(clip, clipPath, spec.Looping);
                var savedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (savedClip == null)
                {
                    throw new InvalidOperationException($"Could not save Fuga animation clip: {clipPath}.");
                }

                savedClip.wrapMode = spec.Looping ? WrapMode.Loop : WrapMode.ClampForever;
                ApplyClipLoopSettings(savedClip, spec.Looping);
                EditorUtility.SetDirty(savedClip);
                clips[spec.ClipName] = savedClip;
            }

            return clips;
        }

        private static Dictionary<string, RuntimeAnimatorController> EnsureAnimatorControllers(
            IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var controllers = new Dictionary<string, RuntimeAnimatorController>(StringComparer.Ordinal);
            foreach (var spec in PlacementSpecs)
            {
                if (spec.ClipName == null)
                {
                    continue;
                }

                var controllerPath = UnityControllerFolder + "/" + spec.ClipName + ".controller";
                if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath) != null)
                {
                    AssetDatabase.DeleteAsset(controllerPath);
                }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var state = controller.layers[0].stateMachine.AddState(spec.ClipName);
                state.motion = clips[spec.ClipName];
                state.writeDefaultValues = true;
                controller.layers[0].stateMachine.defaultState = state;
                AssetDatabase.ImportAsset(controllerPath, ImportAssetOptions.ForceUpdate);
                controllers[spec.ClipName] = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            }

            return controllers;
        }

        private static GameObject PlaceFugaReviewObjects(
            GameObject prefab,
            IReadOnlyDictionary<string, AnimationClip> clips,
            IReadOnlyDictionary<string, RuntimeAnimatorController> controllers,
            Vector3 sceneScale)
        {
            var existingRoot = GameObject.Find(PlacementRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var placementRoot = new GameObject(PlacementRootName);
            var positions = BuildPlacementPositions();
            placementRoot.transform.position = Average(positions);

            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                var spec = PlacementSpecs[i];
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate Fuga prefab into scene.");
                }

                instance.name = spec.ObjectName;
                instance.transform.SetParent(placementRoot.transform, true);
                instance.transform.position = positions[i];
                instance.transform.rotation = Quaternion.Euler(0f, FugaFacingYawDegrees, 0f);
                instance.transform.localScale = sceneScale;

                ConfigureInstancePhysics(instance, spec);
                ConfigureInstanceAnimator(instance, spec, clips, controllers);
                ConfigureReviewPlayback(instance, spec, clips);
                ApplyReviewPose(instance, spec, clips);
            }

            AdjustPlacementRootZToCorridorParvumGap(placementRoot.transform);
            return placementRoot;
        }

        private static Vector3 CalculateFugaSceneScale(GameObject prefab)
        {
            var preview = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (preview == null)
            {
                throw new InvalidOperationException("Could not instantiate Fuga prefab for scale calculation.");
            }

            try
            {
                preview.hideFlags = HideFlags.HideAndDontSave;
                preview.transform.position = new Vector3(10000f, 10000f, 10000f);
                preview.transform.rotation = Quaternion.identity;
                preview.transform.localScale = Vector3.one;

                var bounds = CalculateRendererBounds(preview.transform, new Bounds(preview.transform.position, Vector3.one));
                var size = bounds.size;
                return new Vector3(
                    ScaleForDimension(FugaTargetWidthMeters, size.x),
                    ScaleForDimension(FugaTargetHeightMeters, size.y),
                    ScaleForDimension(FugaTargetDepthMeters, size.z));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static float ScaleForDimension(float targetMeters, float measuredUnits)
        {
            var scale = measuredUnits > 0.0001f ? targetMeters / measuredUnits : FugaMaximumSceneScale;
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            {
                scale = FugaMinimumSceneScale;
            }

            return Mathf.Clamp(scale, FugaMinimumSceneScale, FugaMaximumSceneScale);
        }

        private static Vector3[] BuildPlacementPositions()
        {
            var corridorBounds = FindRendererBounds(CorridorRootName, new Bounds(Vector3.zero, new Vector3(16f, 3f, 12f)));
            var parvumRoot = GameObject.Find(ParvumPlacementRootName);
            var parvumBounds = parvumRoot != null
                ? CalculateRendererBounds(parvumRoot.transform, corridorBounds)
                : corridorBounds;

            var centerX = parvumRoot != null ? parvumBounds.center.x : corridorBounds.center.x;
            var z = TryCalculateFugaRootZFromCorridorParvumRootGap(out var targetZ)
                ? targetZ
                : corridorBounds.min.z - FugaParvumMinimumRootZGap;
            var y = corridorBounds.min.y + FugaFlightHeight;
            var startX = centerX - (FugaPlacementSpacing * (PlacementSpecs.Length - 1) * 0.5f);

            var positions = new Vector3[PlacementSpecs.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = new Vector3(startX + (FugaPlacementSpacing * i), y, z);
            }

            return positions;
        }

        private static void AdjustPlacementRootZToCorridorParvumGap(Transform placementRoot)
        {
            if (!TryCalculateFugaRootZFromCorridorParvumRootGap(out var targetZ))
            {
                return;
            }

            placementRoot.position = new Vector3(placementRoot.position.x, placementRoot.position.y, targetZ);
        }

        private static void ConfigureInitialFugaReviewCamera(Transform placementRoot)
        {
            var focus = FindFugaCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var camera = FindOrCreateReviewCamera();

            var frontDirection = CalculateFugaVisualFrontDirection(focus);
            var lookAt = bounds.center;
            var distance = Mathf.Clamp(
                bounds.extents.magnitude * 4.5f,
                FugaReviewCameraMinimumFrontDistance,
                FugaReviewCameraMaximumFrontDistance);
            var verticalOffset = Mathf.Clamp(bounds.extents.y * 0.45f, 0.18f, 0.55f);
            camera.transform.position = lookAt + (frontDirection * distance) + (Vector3.up * verticalOffset);
            camera.transform.LookAt(lookAt);
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 250f;
            camera.fieldOfView = 32f;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(lookAt, camera.transform.rotation, distance, false, true);
            }
        }

        private static void ConfigureInitialFugaPlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = FindFugaCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center;
            var frontDirection = CalculateFugaVisualFrontDirection(focus);
            var startPosition = CalculateFugaPlayerStartPosition(lookAt, frontDirection);

            player.position = startPosition;
            player.rotation = CalculateYawRotationToward(startPosition, lookAt);
        }

        private static Transform FindPlayerStartTransform()
        {
            var player = GameObject.Find(PlayerRootName);
            return player != null ? player.transform : null;
        }

        private static Vector3 CalculateFugaPlayerStartPosition(Vector3 lookAt, Vector3 frontDirection)
        {
            return new Vector3(
                lookAt.x + frontDirection.x * FugaReviewPlayerFrontDistance,
                0f,
                lookAt.z + frontDirection.z * FugaReviewPlayerFrontDistance);
        }

        private static Quaternion CalculateYawRotationToward(Vector3 position, Vector3 target)
        {
            var facing = target - position;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        private static Transform FindFugaCameraFocus(Transform placementRoot)
        {
            return placementRoot.Find("Fuga_03_Attack") ?? placementRoot.Find("Fuga_00_Static") ?? placementRoot.GetChild(0);
        }

        private static Vector3 CalculateFugaVisualFrontDirection(Transform focus)
        {
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var featurePositions = new List<Vector3>(VisualFrontFeatureNames.Length);
            foreach (var featureName in VisualFrontFeatureNames)
            {
                var feature = FindChildRecursive(focus, featureName);
                if (feature != null)
                {
                    featurePositions.Add(feature.position);
                }
            }

            if (featurePositions.Count > 0)
            {
                var featureDirection = Average(featurePositions) - bounds.center;
                featureDirection.y = 0f;
                if (featureDirection.sqrMagnitude > 0.001f)
                {
                    return featureDirection.normalized;
                }
            }

            var yawRotation = Quaternion.Euler(0f, focus.eulerAngles.y, 0f);
            var fallbackDirection = yawRotation * Vector3.back;
            return fallbackDirection.sqrMagnitude > 0.001f ? fallbackDirection.normalized : Vector3.back;
        }

        private static Camera FindOrCreateReviewCamera()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return camera;
            }

            var cameraObject = GameObject.Find(ReviewCameraName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject(ReviewCameraName);
            }

            camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.tag = "MainCamera";
            return camera;
        }

        private static Camera FindReviewCamera()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return camera;
            }

            var cameraObject = GameObject.Find(ReviewCameraName);
            return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
        }

        private static bool IsDeathSpec(PlacementSpec spec)
        {
            return string.Equals(spec.ObjectName, "Fuga_05_Death", StringComparison.Ordinal) ||
                   string.Equals(spec.ClipName, "Fuga_Death_FallAndFold", StringComparison.Ordinal);
        }

        private static void ConfigureInstancePhysics(GameObject instance, PlacementSpec spec)
        {
            var rigidbody = instance.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = instance.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = !IsDeathSpec(spec);
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var collider = instance.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = instance.AddComponent<BoxCollider>();
            }

            ConfigureColliderFromRenderers(instance.transform, collider);

            var target = FindChildRecursive(instance.transform, MotionTargetName);
            if (target == null)
            {
                target = EnsureMotionTargetHierarchy(instance.transform);
            }

            var driver = instance.GetComponent<FugaPhysicsMotionDriver>();
            if (driver == null)
            {
                driver = instance.AddComponent<FugaPhysicsMotionDriver>();
            }

            var deathFall = IsDeathSpec(spec);
            driver.Configure(
                rigidbody,
                target,
                reviewLocked: !deathFall,
                configuredFollowVerticalAxis: deathFall,
                configuredUseDeathFallSequence: deathFall,
                configuredLoopDeathFallForReview: deathFall);

            if (deathFall)
            {
                driver.ConfigureDeathFall(
                    ReviewDeathFallVelocity,
                    ReviewDeathFallDuration,
                    ReviewDeathImpactSettleDuration,
                    ReviewDeathFinalHoldDuration);
            }
        }

        private static void ConfigureInstanceAnimator(
            GameObject instance,
            PlacementSpec spec,
            IReadOnlyDictionary<string, AnimationClip> clips,
            IReadOnlyDictionary<string, RuntimeAnimatorController> controllers)
        {
            var animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;

            if (spec.ClipName == null)
            {
                animator.runtimeAnimatorController = null;
                return;
            }

            animator.runtimeAnimatorController = controllers[spec.ClipName];
        }

        private static void ConfigureReviewPlayback(
            GameObject instance,
            PlacementSpec spec,
            IReadOnlyDictionary<string, AnimationClip> clips)
        {
            var reviewPlayback = instance.GetComponent<FugaAnimationReviewPlaybackDriver>();
            if (spec.ClipName == null)
            {
                if (reviewPlayback != null)
                {
                    UnityEngine.Object.DestroyImmediate(reviewPlayback);
                }

                return;
            }

            if (!clips.TryGetValue(spec.ClipName, out var clip) || clip == null)
            {
                throw new InvalidOperationException($"{spec.ObjectName} is missing review playback clip {spec.ClipName}.");
            }

            if (reviewPlayback == null)
            {
                reviewPlayback = instance.AddComponent<FugaAnimationReviewPlaybackDriver>();
            }

            var startOffset = IsDeathSpec(spec) ? 0f : spec.ReviewSampleTime;
            reviewPlayback.Configure(clip, spec.Looping, startOffset, 1f);
        }

        private static void ApplyReviewPose(
            GameObject instance,
            PlacementSpec spec,
            IReadOnlyDictionary<string, AnimationClip> clips)
        {
            if (spec.ClipName == null)
            {
                return;
            }

            if (clips.TryGetValue(spec.ClipName, out var clip) && clip != null)
            {
                clip.SampleAnimation(instance, spec.ReviewSampleTime);
            }
        }

        private static void EnsurePrefabPhysics(GameObject root, Transform motionTarget)
        {
            var rigidbody = root.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = root.AddComponent<Rigidbody>();
            }

            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var collider = root.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = root.AddComponent<BoxCollider>();
            }

            ConfigureColliderFromRenderers(root.transform, collider);

            var driver = root.GetComponent<FugaPhysicsMotionDriver>();
            if (driver == null)
            {
                driver = root.AddComponent<FugaPhysicsMotionDriver>();
            }

            driver.Configure(rigidbody, motionTarget, true);
        }

        private static AnimationClip BuildIdleClip(AnimationPaths paths, Vector3 sceneScale)
        {
            var clip = CreateClip("Fuga_Idle_SlowWingbeat", 1.6f);
            AddBodyHover(clip, paths.Model, 1.6f, MetersToLocal(0.035f, sceneScale.y));
            AddWingBeat(clip, paths, 1.6f, 38f, 14f, 0f);
            AddMotionTargetHold(clip, paths.MotionTarget);
            return clip;
        }

        private static AnimationClip BuildMoveClip(AnimationPaths paths, Vector3 sceneScale)
        {
            var clip = CreateClip("Fuga_Move_FastWingbeat", 0.75f);
            AddBodyHover(clip, paths.Model, 0.75f, MetersToLocal(0.055f, sceneScale.y));
            AddWingBeat(clip, paths, 0.75f, 56f, 20f, 0f);
            AddMotionTargetForwardPulse(clip, paths.MotionTarget, 0.75f, MetersToLocal(0.22f, sceneScale.z));
            return clip;
        }

        private static AnimationClip BuildAttackClip(AnimationPaths paths, Vector3 sceneScale)
        {
            var clip = CreateClip("Fuga_Attack_WingtipStrike", 1.05f);
            SetVectorCurve(clip, paths.Model, "localPosition",
                0f, Vector3.zero,
                0.16f, MetersToLocal(new Vector3(0f, 0.012f, -0.05f), sceneScale),
                0.42f, MetersToLocal(new Vector3(0f, -0.014f, 0.22f), sceneScale),
                0.58f, MetersToLocal(new Vector3(0f, -0.018f, 0.18f), sceneScale),
                1.05f, Vector3.zero);
            SetEulerX(clip, paths.Model, 0f, 0f, 0.16f, -7f, 0.42f, 14f, 0.58f, 9f, 1.05f, 0f);
            SetEulerY(clip, paths.Model, 0f, 0f, 0.16f, -4f, 0.42f, 6f, 0.58f, 2f, 1.05f, 0f);
            SetEulerZ(clip, paths.Model, 0f, 0f, 0.16f, 0f, 0.42f, 3f, 0.58f, 1f, 1.05f, 0f);
            SetWingEuler(clip, paths, 0f, 24f, 0.12f, 58f, 0.28f, 66f, 0.44f, -54f, 0.54f, -38f, 0.72f, 8f, 1.05f, 24f);
            SetWingPitch(clip, paths, 0f, 0f, 0.12f, -16f, 0.28f, -22f, 0.44f, 40f, 0.54f, 28f, 0.72f, -6f, 1.05f, 0f);
            AddMotionTargetForwardPulse(clip, paths.MotionTarget, 1.05f, MetersToLocal(0.18f, sceneScale.z));
            return clip;
        }

        private static AnimationClip BuildHitClip(AnimationPaths paths, Vector3 sceneScale)
        {
            var clip = CreateClip("Fuga_Hit_SquashRecoil", 1.15f);
            SetVectorCurve(clip, paths.Model, "localPosition", 0f, Vector3.zero, 0.14f, MetersToLocal(new Vector3(-0.11f, -0.018f, -0.04f), sceneScale), 0.38f, MetersToLocal(new Vector3(-0.04f, 0.008f, -0.018f), sceneScale), 1.15f, Vector3.zero);
            SetVectorCurve(clip, paths.Model, "localScale", 0f, Vector3.one, 0.12f, new Vector3(1.18f, 0.72f, 1.10f), 0.36f, new Vector3(0.94f, 1.08f, 0.96f), 1.15f, Vector3.one);
            SetEulerZ(clip, paths.Model, 0f, 0f, 0.16f, 22f, 0.38f, -8f, 1.15f, 0f);
            SetWingEuler(clip, paths, 0f, 0f, 0.12f, -62f, 0.38f, -25f, 1.15f, 0f);
            AddMotionTargetHold(clip, paths.MotionTarget);
            return clip;
        }

        private static AnimationClip BuildDeathClip(AnimationPaths paths, Vector3 sceneScale)
        {
            var impactTime = ReviewDeathFallDuration;
            var settleTime = ReviewDeathFallDuration + ReviewDeathImpactSettleDuration;
            var duration = ReviewDeathFallDuration + ReviewDeathImpactSettleDuration + ReviewDeathFinalHoldDuration;
            var clip = CreateClip("Fuga_Death_FallAndFold", duration);
            SetVectorCurve(clip, paths.Model, "localScale",
                0f, Vector3.one,
                0.28f, new Vector3(1.02f, 0.95f, 1.02f),
                impactTime, new Vector3(1.08f, 0.82f, 1.06f),
                settleTime, new Vector3(1.08f, 0.82f, 1.06f),
                duration, new Vector3(1.08f, 0.82f, 1.06f));
            SetEulerX(clip, paths.Model, 0f, 0f, 0.16f, 6f, 0.42f, 24f, impactTime, 34f, settleTime, 34f, duration, 34f);
            SetEulerY(clip, paths.Model, 0f, 0f, 0.25f, -12f, 0.54f, -28f, impactTime, -34f, settleTime, -34f, duration, -34f);
            SetEulerZ(clip, paths.Model, 0f, 0f, 0.18f, 18f, 0.42f, 44f, impactTime, 66f, settleTime, 66f, duration, 66f);
            SetWingEuler(clip, paths, 0f, 0f, 0.18f, -28f, 0.42f, -64f, impactTime, -88f, settleTime, -88f, duration, -88f);
            SetWingPitch(clip, paths, 0f, 0f, 0.28f, 26f, 0.54f, 52f, impactTime, 62f, settleTime, 62f, duration, 62f);
            AddMotionTargetHold(clip, paths.MotionTarget);
            return clip;
        }

        private static AnimationClip BuildConsumeClip(AnimationPaths paths, Vector3 sceneScale)
        {
            var clip = CreateClip("Fuga_Consume_BiteForward", 1.35f);
            SetVectorCurve(clip, paths.Model, "localPosition",
                0f, Vector3.zero,
                0.18f, MetersToLocal(new Vector3(0f, 0.012f, 0.02f), sceneScale),
                0.38f, MetersToLocal(new Vector3(0f, 0.004f, 0.06f), sceneScale),
                0.58f, MetersToLocal(new Vector3(0f, -0.032f, 0.28f), sceneScale),
                0.78f, MetersToLocal(new Vector3(0f, -0.020f, 0.18f), sceneScale),
                1.35f, Vector3.zero);
            SetEulerX(clip, paths.Model, 0f, 0f, 0.18f, 4f, 0.38f, 8f, 0.58f, 28f, 0.78f, 12f, 1.35f, 0f);
            SetWingEuler(clip, paths, 0f, 0f, 0.22f, 12f, 0.38f, 6f, 0.58f, -10f, 0.78f, 4f, 1.35f, 0f);

            if (!string.IsNullOrEmpty(paths.LowerJaw))
            {
                SetEulerX(clip, paths.LowerJaw, 0f, 0f, 0.16f, 28f, 0.34f, 66f, 0.50f, 72f, 0.68f, 20f, 0.82f, 4f, 1.35f, 0f);
            }

            if (!string.IsNullOrEmpty(paths.MouthRecess))
            {
                SetVectorCurve(clip, paths.MouthRecess, "localScale",
                    0f, Vector3.one,
                    0.18f, new Vector3(1.18f, 1.75f, 1.10f),
                    0.38f, new Vector3(1.35f, 2.80f, 1.22f),
                    0.56f, new Vector3(1.15f, 1.35f, 1.08f),
                    0.72f, new Vector3(0.85f, 0.62f, 0.94f),
                    1.35f, Vector3.one);
            }

            if (!string.IsNullOrEmpty(paths.SnoutBulge))
            {
                SetVectorCurve(clip, paths.SnoutBulge, "localScale",
                    0f, Vector3.one,
                    0.38f, new Vector3(1.06f, 0.88f, 1.04f),
                    0.58f, new Vector3(1.08f, 0.84f, 1.05f),
                    1.35f, Vector3.one);
            }

            AddMotionTargetForwardPulse(clip, paths.MotionTarget, 1.35f, 0.42f);
            return clip;
        }

        private static AnimationClip CreateClip(string name, float duration)
        {
            var clip = new AnimationClip
            {
                name = name,
                frameRate = 30f,
                wrapMode = WrapMode.Loop
            };

            return clip;
        }

        private static void AddBodyHover(AnimationClip clip, string path, float duration, float amplitude)
        {
            SetCurve(clip, path, typeof(Transform), "localPosition.y",
                Key(0f, 0f), Key(duration * 0.25f, amplitude), Key(duration * 0.5f, 0f),
                Key(duration * 0.75f, -amplitude * 0.6f), Key(duration, 0f));
        }

        private static void AddWingBeat(AnimationClip clip, AnimationPaths paths, float duration, float zAmplitude, float xAmplitude, float baseZ)
        {
            if (string.IsNullOrEmpty(paths.LeftWing) || string.IsNullOrEmpty(paths.RightWing))
            {
                return;
            }

            SetCurve(clip, paths.LeftWing, typeof(Transform), "localEulerAnglesRaw.z",
                Key(0f, baseZ - zAmplitude), Key(duration * 0.25f, baseZ + zAmplitude), Key(duration * 0.5f, baseZ - zAmplitude),
                Key(duration * 0.75f, baseZ + zAmplitude), Key(duration, baseZ - zAmplitude));
            SetCurve(clip, paths.RightWing, typeof(Transform), "localEulerAnglesRaw.z",
                Key(0f, -baseZ + zAmplitude), Key(duration * 0.25f, -baseZ - zAmplitude), Key(duration * 0.5f, -baseZ + zAmplitude),
                Key(duration * 0.75f, -baseZ - zAmplitude), Key(duration, -baseZ + zAmplitude));

            SetCurve(clip, paths.LeftWing, typeof(Transform), "localEulerAnglesRaw.x",
                Key(0f, -xAmplitude), Key(duration * 0.25f, xAmplitude), Key(duration * 0.5f, -xAmplitude),
                Key(duration * 0.75f, xAmplitude), Key(duration, -xAmplitude));
            SetCurve(clip, paths.RightWing, typeof(Transform), "localEulerAnglesRaw.x",
                Key(0f, -xAmplitude), Key(duration * 0.25f, xAmplitude), Key(duration * 0.5f, -xAmplitude),
                Key(duration * 0.75f, xAmplitude), Key(duration, -xAmplitude));
        }

        private static void SetWingEuler(AnimationClip clip, AnimationPaths paths, params float[] timeValuePairs)
        {
            if (string.IsNullOrEmpty(paths.LeftWing) || string.IsNullOrEmpty(paths.RightWing))
            {
                return;
            }

            var leftKeys = new List<Keyframe>();
            var rightKeys = new List<Keyframe>();
            for (var i = 0; i + 1 < timeValuePairs.Length; i += 2)
            {
                leftKeys.Add(Key(timeValuePairs[i], timeValuePairs[i + 1]));
                rightKeys.Add(Key(timeValuePairs[i], -timeValuePairs[i + 1]));
            }

            SetCurve(clip, paths.LeftWing, typeof(Transform), "localEulerAnglesRaw.z", leftKeys.ToArray());
            SetCurve(clip, paths.RightWing, typeof(Transform), "localEulerAnglesRaw.z", rightKeys.ToArray());
        }

        private static void SetWingPitch(AnimationClip clip, AnimationPaths paths, params float[] timeValuePairs)
        {
            if (string.IsNullOrEmpty(paths.LeftWing) || string.IsNullOrEmpty(paths.RightWing))
            {
                return;
            }

            SetFloatCurve(clip, paths.LeftWing, "localEulerAnglesRaw.x", timeValuePairs);
            SetFloatCurve(clip, paths.RightWing, "localEulerAnglesRaw.x", timeValuePairs);
        }

        private static void SetWingYaw(AnimationClip clip, AnimationPaths paths, params float[] timeValuePairs)
        {
            if (string.IsNullOrEmpty(paths.LeftWing) || string.IsNullOrEmpty(paths.RightWing))
            {
                return;
            }

            var leftKeys = new List<Keyframe>();
            var rightKeys = new List<Keyframe>();
            for (var i = 0; i + 1 < timeValuePairs.Length; i += 2)
            {
                leftKeys.Add(Key(timeValuePairs[i], timeValuePairs[i + 1]));
                rightKeys.Add(Key(timeValuePairs[i], -timeValuePairs[i + 1]));
            }

            SetCurve(clip, paths.LeftWing, typeof(Transform), "localEulerAnglesRaw.y", leftKeys.ToArray());
            SetCurve(clip, paths.RightWing, typeof(Transform), "localEulerAnglesRaw.y", rightKeys.ToArray());
        }

        private static void SetWingPositionOffset(AnimationClip clip, AnimationPaths paths, params object[] timeOffsetPairs)
        {
            if (string.IsNullOrEmpty(paths.LeftWing) || string.IsNullOrEmpty(paths.RightWing))
            {
                return;
            }

            var leftPairs = new List<object>();
            var rightPairs = new List<object>();
            for (var i = 0; i + 1 < timeOffsetPairs.Length; i += 2)
            {
                var time = Convert.ToSingle(timeOffsetPairs[i]);
                var offset = (Vector3)timeOffsetPairs[i + 1];
                leftPairs.Add(time);
                leftPairs.Add(paths.LeftWingRestPosition + offset);
                rightPairs.Add(time);
                rightPairs.Add(paths.RightWingRestPosition + offset);
            }

            SetVectorCurve(clip, paths.LeftWing, "localPosition", leftPairs.ToArray());
            SetVectorCurve(clip, paths.RightWing, "localPosition", rightPairs.ToArray());
        }

        private static void SetWingPositionOffsetWithOutwardSpread(AnimationClip clip, AnimationPaths paths, params object[] timeOffsetPairs)
        {
            if (string.IsNullOrEmpty(paths.LeftWing) || string.IsNullOrEmpty(paths.RightWing))
            {
                return;
            }

            var leftOutwardSign = CalculateOutwardSign(paths.LeftWingRestPosition, 1f);
            var rightOutwardSign = CalculateOutwardSign(paths.RightWingRestPosition, -1f);
            var leftPairs = new List<object>();
            var rightPairs = new List<object>();
            for (var i = 0; i + 1 < timeOffsetPairs.Length; i += 2)
            {
                var time = Convert.ToSingle(timeOffsetPairs[i]);
                var offset = (Vector3)timeOffsetPairs[i + 1];
                var leftOffset = new Vector3(offset.x * leftOutwardSign, offset.y, offset.z);
                var rightOffset = new Vector3(offset.x * rightOutwardSign, offset.y, offset.z);
                leftPairs.Add(time);
                leftPairs.Add(paths.LeftWingRestPosition + leftOffset);
                rightPairs.Add(time);
                rightPairs.Add(paths.RightWingRestPosition + rightOffset);
            }

            SetVectorCurve(clip, paths.LeftWing, "localPosition", leftPairs.ToArray());
            SetVectorCurve(clip, paths.RightWing, "localPosition", rightPairs.ToArray());
        }

        private static float CalculateOutwardSign(Vector3 restPosition, float fallbackSign)
        {
            return Mathf.Abs(restPosition.x) > 0.001f ? Mathf.Sign(restPosition.x) : fallbackSign;
        }

        private static void AddMotionTargetHold(AnimationClip clip, string motionTargetPath)
        {
            if (string.IsNullOrEmpty(motionTargetPath))
            {
                return;
            }

            SetVectorCurve(clip, motionTargetPath, "localPosition", 0f, Vector3.zero, 1f, Vector3.zero);
        }

        private static void AddMotionTargetForwardPulse(AnimationClip clip, string motionTargetPath, float duration, float forwardDistance)
        {
            if (string.IsNullOrEmpty(motionTargetPath))
            {
                return;
            }

            SetVectorCurve(clip, motionTargetPath, "localPosition", 0f, Vector3.zero, duration * 0.45f, new Vector3(0f, 0f, forwardDistance), duration, Vector3.zero);
        }

        private static void SetVectorCurve(AnimationClip clip, string path, string propertyPrefix, params object[] timeValuePairs)
        {
            var xKeys = new List<Keyframe>();
            var yKeys = new List<Keyframe>();
            var zKeys = new List<Keyframe>();

            for (var i = 0; i + 1 < timeValuePairs.Length; i += 2)
            {
                var time = Convert.ToSingle(timeValuePairs[i]);
                var value = (Vector3)timeValuePairs[i + 1];
                xKeys.Add(Key(time, value.x));
                yKeys.Add(Key(time, value.y));
                zKeys.Add(Key(time, value.z));
            }

            SetCurve(clip, path, typeof(Transform), propertyPrefix + ".x", xKeys.ToArray());
            SetCurve(clip, path, typeof(Transform), propertyPrefix + ".y", yKeys.ToArray());
            SetCurve(clip, path, typeof(Transform), propertyPrefix + ".z", zKeys.ToArray());
        }

        private static float MetersToLocal(float meters, float axisScale)
        {
            return meters / Mathf.Max(axisScale, FugaMinimumSceneScale);
        }

        private static Vector3 MetersToLocal(Vector3 meters, Vector3 sceneScale)
        {
            return new Vector3(
                meters.x / Mathf.Max(sceneScale.x, FugaMinimumSceneScale),
                meters.y / Mathf.Max(sceneScale.y, FugaMinimumSceneScale),
                meters.z / Mathf.Max(sceneScale.z, FugaMinimumSceneScale));
        }

        private static void SetEulerX(AnimationClip clip, string path, params float[] timeValuePairs)
        {
            SetFloatCurve(clip, path, "localEulerAnglesRaw.x", timeValuePairs);
        }

        private static void SetEulerY(AnimationClip clip, string path, params float[] timeValuePairs)
        {
            SetFloatCurve(clip, path, "localEulerAnglesRaw.y", timeValuePairs);
        }

        private static void SetEulerZ(AnimationClip clip, string path, params float[] timeValuePairs)
        {
            SetFloatCurve(clip, path, "localEulerAnglesRaw.z", timeValuePairs);
        }

        private static void SetFloatCurve(AnimationClip clip, string path, string propertyName, params float[] timeValuePairs)
        {
            var keys = new List<Keyframe>();
            for (var i = 0; i + 1 < timeValuePairs.Length; i += 2)
            {
                keys.Add(Key(timeValuePairs[i], timeValuePairs[i + 1]));
            }

            SetCurve(clip, path, typeof(Transform), propertyName, keys.ToArray());
        }

        private static void SetCurve(AnimationClip clip, string path, Type type, string propertyName, params Keyframe[] keys)
        {
            clip.SetCurve(path, type, propertyName, new AnimationCurve(keys));
        }

        private static Keyframe Key(float time, float value)
        {
            return new Keyframe(time, value);
        }

        private static void SaveClip(AnimationClip clip, string path, bool loop)
        {
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(clip, path);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            ApplyClipLoopSettings(clip, loop);
            EditorUtility.SetDirty(clip);
        }

        private static void ApplyClipLoopSettings(AnimationClip clip, bool loop)
        {
            var serializedClip = new SerializedObject(clip);
            serializedClip.Update();
            var loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loopTime != null)
            {
                loopTime.boolValue = loop;
            }

            var loopBlend = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopBlend");
            if (loopBlend != null)
            {
                loopBlend.boolValue = loop;
            }

            serializedClip.ApplyModifiedProperties();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
        }

        private static AnimationPaths ResolveAnimationPaths(GameObject prefab)
        {
            var model = prefab.transform.Find(ModelChildName);
            var motionTarget = FindChildRecursive(prefab.transform, MotionTargetName);
            var leftWing = FindChildRecursive(prefab.transform, "Fuga2_Left_Wing_Root_For_Pose");
            var rightWing = FindChildRecursive(prefab.transform, "Fuga2_Right_Wing_Root_For_Pose");
            var lowerJaw = FindChildRecursive(prefab.transform, "Fuga2_Continuous_Lower_Jaw_To_Chest_Join");
            var mouthRecess = FindChildRecursive(prefab.transform, "Fuga2_Subtle_Dark_Wavy_Mouth_Recess");
            var snoutBulge = FindChildRecursive(prefab.transform, "Fuga2_Broad_Front_Snout_Bulge_With_Wavy_Mouth");

            return new AnimationPaths(
                model != null ? AnimationUtility.CalculateTransformPath(model, prefab.transform) : ModelChildName,
                motionTarget != null ? AnimationUtility.CalculateTransformPath(motionTarget, prefab.transform) : string.Empty,
                leftWing != null ? AnimationUtility.CalculateTransformPath(leftWing, prefab.transform) : string.Empty,
                rightWing != null ? AnimationUtility.CalculateTransformPath(rightWing, prefab.transform) : string.Empty,
                leftWing != null ? leftWing.localPosition : Vector3.zero,
                rightWing != null ? rightWing.localPosition : Vector3.zero,
                lowerJaw != null ? AnimationUtility.CalculateTransformPath(lowerJaw, prefab.transform) : string.Empty,
                mouthRecess != null ? AnimationUtility.CalculateTransformPath(mouthRecess, prefab.transform) : string.Empty,
                snoutBulge != null ? AnimationUtility.CalculateTransformPath(snoutBulge, prefab.transform) : string.Empty);
        }

        private static Transform EnsureMotionTargetHierarchy(Transform root)
        {
            var targetRoot = root.Find(MotionTargetRootName);
            if (targetRoot == null)
            {
                targetRoot = new GameObject(MotionTargetRootName).transform;
                targetRoot.SetParent(root, false);
            }

            var target = targetRoot.Find(MotionTargetName);
            if (target == null)
            {
                target = new GameObject(MotionTargetName).transform;
                target.SetParent(targetRoot, false);
            }

            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
            return target;
        }

        private static void AssignMaterials(GameObject root, MaterialSet materialSet)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var sourceName = (renderer.name + " " + (materials[i] != null ? materials[i].name : string.Empty)).ToLowerInvariant();
                    materials[i] = SelectMaterialForSource(sourceName, materialSet);
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static Material SelectMaterialForSource(string sourceName, MaterialSet materialSet)
        {
            if (sourceName.Contains("dark") ||
                sourceName.Contains("pupil") ||
                sourceName.Contains("mouth") ||
                sourceName.Contains("recess") ||
                sourceName.Contains("crack") ||
                sourceName.Contains("midrib") ||
                sourceName.Contains("barb") ||
                sourceName.Contains("serrated_edge"))
            {
                return materialSet.Dark;
            }

            if (sourceName.Contains("golden") || sourceName.Contains("vertical_slit_eye"))
            {
                return materialSet.Eye;
            }

            if (sourceName.Contains("lower_shell") || sourceName.Contains("shell_leaf"))
            {
                return materialSet.LowerShell;
            }

            if (sourceName.Contains("inner") ||
                sourceName.Contains("brown") ||
                sourceName.Contains("single_connected_broad_wing_base"))
            {
                return materialSet.InnerWing;
            }

            if (sourceName.Contains("wing") || sourceName.Contains("feather") || sourceName.Contains("olive"))
            {
                return materialSet.Wing;
            }

            return materialSet.Body;
        }

        private static void ConfigureColliderFromRenderers(Transform root, BoxCollider collider)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            collider.center = root.InverseTransformPoint(bounds.center);
            var lossyScale = root.lossyScale;
            collider.size = new Vector3(
                SafeDivide(bounds.size.x, Mathf.Abs(lossyScale.x)),
                SafeDivide(bounds.size.y, Mathf.Abs(lossyScale.y)),
                SafeDivide(bounds.size.z, Mathf.Abs(lossyScale.z)));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return divisor > 0.0001f ? value / divisor : value;
        }

        private static Material EnsureMaterial(
            string name,
            Texture2D albedo,
            Texture2D normal,
            Color fallbackColor,
            float smoothness,
            float metallic)
        {
            var path = UnityMaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            SetMaterialColor(material, fallbackColor);
            SetMaterialFloat(material, "_Smoothness", smoothness);
            SetMaterialFloat(material, "_Glossiness", smoothness);
            SetMaterialFloat(material, "_Metallic", metallic);
            SetMaterialTexture(material, "_BaseMap", albedo);
            SetMaterialTexture(material, "_MainTex", albedo);

            if (normal != null)
            {
                SetMaterialTexture(material, "_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SetMaterialFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void SetMaterialTexture(Material material, string property, Texture texture)
        {
            if (texture != null && material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static Texture2D LoadTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(UnityTextureFolder + "/" + fileName);
        }

        private static Bounds FindRendererBounds(string objectName, Bounds fallback)
        {
            var root = GameObject.Find(objectName);
            return root != null ? CalculateRendererBounds(root.transform, fallback) : fallback;
        }

        private static Bounds CalculateRendererBounds(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static float CalculateParvumZGap(Transform fugaPlacementRoot)
        {
            var parvumRoot = GameObject.Find(ParvumPlacementRootName);
            if (parvumRoot == null)
            {
                return -1f;
            }

            return Mathf.Abs(fugaPlacementRoot.position.z - parvumRoot.transform.position.z);
        }

        private static float CalculateCorridorParvumZGap()
        {
            return TryCalculateCorridorParvumRootZGap(out var rootGap) ? rootGap : -1f;
        }

        private static float CalculateDesiredFugaParvumZGap()
        {
            return Mathf.Max(CalculateCorridorParvumZGap(), FugaParvumMinimumRootZGap);
        }

        private static bool TryCalculateFugaRootZFromCorridorParvumRootGap(out float targetZ)
        {
            targetZ = 0f;
            var corridorRoot = GameObject.Find(CorridorRootName);
            var parvumRoot = GameObject.Find(ParvumPlacementRootName);
            if (corridorRoot == null || parvumRoot == null)
            {
                return false;
            }

            var corridorZ = corridorRoot.transform.position.z;
            var parvumZ = parvumRoot.transform.position.z;
            var directionFromCorridorToParvum = Mathf.Sign(parvumZ - corridorZ);
            if (Mathf.Abs(directionFromCorridorToParvum) < 0.001f)
            {
                directionFromCorridorToParvum = -1f;
            }

            var rootGap = Mathf.Max(Mathf.Abs(parvumZ - corridorZ), FugaParvumMinimumRootZGap);
            targetZ = parvumZ + (directionFromCorridorToParvum * rootGap);
            return true;
        }

        private static bool TryCalculateCorridorParvumRootZGap(out float rootGap)
        {
            rootGap = 0f;
            var corridorRoot = GameObject.Find(CorridorRootName);
            var parvumRoot = GameObject.Find(ParvumPlacementRootName);
            if (corridorRoot == null || parvumRoot == null)
            {
                return false;
            }

            rootGap = Mathf.Abs(parvumRoot.transform.position.z - corridorRoot.transform.position.z);
            return true;
        }

        private static Vector3 Average(IReadOnlyList<Vector3> values)
        {
            var sum = Vector3.zero;
            foreach (var value in values)
            {
                sum += value;
            }

            return values.Count > 0 ? sum / values.Count : Vector3.zero;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                var match = FindChildRecursive(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void WriteReviewFiles(Scene scene, GameObject placementRoot, Vector3 sceneScale)
        {
            var reviewPath = ProjectPath(ReviewOutputRelativePath);
            Directory.CreateDirectory(reviewPath);
            var staticObject = placementRoot.transform.Find("Fuga_00_Static");
            var staticSize = staticObject != null
                ? CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.zero)).size
                : Vector3.zero;
            var parvumZGap = CalculateParvumZGap(placementRoot.transform);
            var corridorParvumZGap = CalculateCorridorParvumZGap();
            var desiredFugaParvumZGap = Mathf.Max(corridorParvumZGap, FugaParvumMinimumRootZGap);
            var minimumXGap = CalculateMinimumFugaXGap(placementRoot.transform);
            var camera = FindReviewCamera();
            var cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
            var cameraFocus = FindFugaCameraFocus(placementRoot.transform);
            var cameraFocusBounds = CalculateRendererBounds(cameraFocus, new Bounds(cameraFocus.position, Vector3.zero));
            var cameraFrontDirection = CalculateFugaVisualFrontDirection(cameraFocus);
            var cameraFrontDistance = camera != null
                ? Vector3.Dot(camera.transform.position - cameraFocusBounds.center, cameraFrontDirection)
                : 0f;
            var playerStart = FindPlayerStartTransform();
            var playerStartPosition = playerStart != null ? playerStart.position : Vector3.zero;
            var playerStartFrontDistance = playerStart != null
                ? Vector3.Dot(playerStart.position - cameraFocusBounds.center, cameraFrontDirection)
                : 0f;
            var playerToFocus = playerStart != null ? cameraFocusBounds.center - playerStart.position : Vector3.zero;
            playerToFocus.y = 0f;
            var playerFacingDot = playerStart != null && playerToFocus.sqrMagnitude > 0.001f
                ? Vector3.Dot(playerStart.forward, playerToFocus.normalized)
                : 0f;

            var record = new ReviewRecord
            {
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                scenePath = scene.path,
                placementRoot = PlacementRootName,
                placedCount = placementRoot.transform.childCount,
                placementRule = "Fuga is placed below Approved Parvum Enemy Placement on negative Z, with the review camera on the current visual front of the rotated Fuga.",
                sceneScaleX = sceneScale.x,
                sceneScaleY = sceneScale.y,
                sceneScaleZ = sceneScale.z,
                requestedUniformScale = FugaRequestedUniformScale,
                facingYawDegrees = FugaFacingYawDegrees,
                placementXSpacing = FugaPlacementSpacing,
                minimumXClearance = FugaPlacementMinimumXClearance,
                measuredMinimumXGap = minimumXGap,
                cameraPositionX = cameraPosition.x,
                cameraPositionY = cameraPosition.y,
                cameraPositionZ = cameraPosition.z,
                cameraFrontDirectionX = cameraFrontDirection.x,
                cameraFrontDirectionY = cameraFrontDirection.y,
                cameraFrontDirectionZ = cameraFrontDirection.z,
                cameraFrontDistance = cameraFrontDistance,
                playerStartPositionX = playerStartPosition.x,
                playerStartPositionY = playerStartPosition.y,
                playerStartPositionZ = playerStartPosition.z,
                playerStartFrontDistance = playerStartFrontDistance,
                playerStartFacingDot = playerFacingDot,
                targetHeightMeters = FugaTargetHeightMeters,
                targetWidthMeters = FugaTargetWidthMeters,
                targetDepthMeters = FugaTargetDepthMeters,
                staticBoundsX = staticSize.x,
                staticBoundsY = staticSize.y,
                staticBoundsZ = staticSize.z,
                approvedWingPanelThickness = ApprovedWingPanelThickness,
                deathMotionRule = "Fuga_05_Death uses a looping review death sequence: sharp Animator tilt/wing fold plus faster FugaPhysicsMotionDriver Rigidbody.linearVelocity fall, immediate Rigidbody freeze, final still hold, and reset.",
                parvumZGap = parvumZGap,
                corridorParvumZGap = corridorParvumZGap,
                desiredFugaParvumZGap = desiredFugaParvumZGap,
                parvumZClearance = FugaParvumMinimumRootZGap,
                animationStates = BuildAnimationStateRecords()
            };

            File.WriteAllText(
                Path.Combine(reviewPath, "FUGA_UNITY_APPLICATION_REVIEW.json"),
                JsonUtility.ToJson(record, true));
            File.WriteAllText(
                Path.Combine(reviewPath, "README.md"),
                BuildReviewReadme(record));
        }

        private static AnimationStateRecord[] BuildAnimationStateRecords()
        {
            var records = new AnimationStateRecord[PlacementSpecs.Length];
            for (var i = 0; i < PlacementSpecs.Length; i++)
            {
                records[i] = new AnimationStateRecord
                {
                    objectName = PlacementSpecs[i].ObjectName,
                    clipName = PlacementSpecs[i].ClipName ?? "Static comparison",
                    description = PlacementSpecs[i].Description,
                    looping = PlacementSpecs[i].Looping
                };
            }

            return records;
        }

        private static string BuildReviewReadme(ReviewRecord record)
        {
            return
                "# Fuga Unity Application Review\n\n" +
                $"- Created at: {record.createdAt}\n" +
                $"- Scene: `{record.scenePath}`\n" +
                $"- Placement root: `{record.placementRoot}`\n" +
                $"- Placed count: {record.placedCount}\n" +
                $"- Placement rule: {record.placementRule}\n" +
                $"- Scene scale X/Y/Z: {record.sceneScaleX:0.######} / {record.sceneScaleY:0.######} / {record.sceneScaleZ:0.######}\n" +
                $"- Requested uniform scale: {record.requestedUniformScale:0.###}\n" +
                $"- Facing yaw: {record.facingYawDegrees:0.###} degrees\n" +
                $"- X placement spacing: {record.placementXSpacing:0.###}m, measured minimum X gap: {record.measuredMinimumXGap:0.###}m, required minimum X clearance: {record.minimumXClearance:0.###}m\n" +
                $"- Camera position X/Y/Z: {record.cameraPositionX:0.###} / {record.cameraPositionY:0.###} / {record.cameraPositionZ:0.###}\n" +
                $"- Fuga visual front direction X/Y/Z: {record.cameraFrontDirectionX:0.###} / {record.cameraFrontDirectionY:0.###} / {record.cameraFrontDirectionZ:0.###}\n" +
                $"- Camera front distance: {record.cameraFrontDistance:0.###}m\n" +
                $"- Player start position X/Y/Z: {record.playerStartPositionX:0.###} / {record.playerStartPositionY:0.###} / {record.playerStartPositionZ:0.###}\n" +
                $"- Player start front distance: {record.playerStartFrontDistance:0.###}m, facing dot: {record.playerStartFacingDot:0.###}\n" +
                $"- Design reference H/W/D: {record.targetHeightMeters:0.###}m / {record.targetWidthMeters:0.###}m / {record.targetDepthMeters:0.###}m\n" +
                $"- Static bounds X/Y/Z: {record.staticBoundsX:0.###}m / {record.staticBoundsY:0.###}m / {record.staticBoundsZ:0.###}m\n\n" +
                $"- Approved broad wing panel thickness: {record.approvedWingPanelThickness:0.###} sample units\n" +
                $"- Death motion rule: {record.deathMotionRule}\n\n" +
                $"- Corridor/Parvum root Z gap: {record.corridorParvumZGap:0.###}m\n" +
                $"- Desired Fuga/Parvum root Z gap: {record.desiredFugaParvumZGap:0.###}m\n" +
                $"- Actual Fuga/Parvum root Z gap: {record.parvumZGap:0.###}m, minimum root clearance: {record.parvumZClearance:0.###}m\n\n" +
                "## Animation States\n\n" +
                "- `Fuga_00_Static`: approved sample static comparison\n" +
                "- `Fuga_01_Idle`: review playback driver loops vertical up/down wing flap beside the body\n" +
                "- `Fuga_02_Move`: review playback driver loops faster vertical up/down wing flap with Motion Path target pulse\n" +
                "- `Fuga_03_Attack`: keeps both wing roots attached, lifts the wings apart, then swats the front with both wingtips\n" +
                "- `Fuga_04_Hit`: visibly recoils, squashes, wings droop, then recovers\n" +
                "- `Fuga_05_Death`: looping review death sequence: hover, sharp tilt, fast Rigidbody fall, hard stop, final still hold, reset\n" +
                "- `Fuga_06_Consume`: keeps the lower jaw connected, opens the mouth, leans forward, and closes\n";
        }

        private static void CopyFileToAsset(string sourceFullPath, string targetAssetPath)
        {
            var targetFullPath = ProjectPath(targetAssetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFullPath) ?? ProjectRoot);
            File.Copy(sourceFullPath, targetFullPath, true);
        }

        private static void EnsureUnityFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string ProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private readonly struct PlacementSpec
        {
            public PlacementSpec(string objectName, string clipName, float reviewSampleTime, bool looping, string description)
            {
                ObjectName = objectName;
                ClipName = clipName;
                ReviewSampleTime = reviewSampleTime;
                Looping = looping;
                Description = description;
            }

            public string ObjectName { get; }
            public string ClipName { get; }
            public float ReviewSampleTime { get; }
            public bool Looping { get; }
            public string Description { get; }
        }

        private readonly struct AnimationPaths
        {
            public AnimationPaths(
                string model,
                string motionTarget,
                string leftWing,
                string rightWing,
                Vector3 leftWingRestPosition,
                Vector3 rightWingRestPosition,
                string lowerJaw,
                string mouthRecess,
                string snoutBulge)
            {
                Model = model;
                MotionTarget = motionTarget;
                LeftWing = leftWing;
                RightWing = rightWing;
                LeftWingRestPosition = leftWingRestPosition;
                RightWingRestPosition = rightWingRestPosition;
                LowerJaw = lowerJaw;
                MouthRecess = mouthRecess;
                SnoutBulge = snoutBulge;
            }

            public string Model { get; }
            public string MotionTarget { get; }
            public string LeftWing { get; }
            public string RightWing { get; }
            public Vector3 LeftWingRestPosition { get; }
            public Vector3 RightWingRestPosition { get; }
            public string LowerJaw { get; }
            public string MouthRecess { get; }
            public string SnoutBulge { get; }
        }

        private readonly struct MaterialSet
        {
            public MaterialSet(
                Material body,
                Material wing,
                Material innerWing,
                Material eye,
                Material lowerShell,
                Material dark)
            {
                Body = body;
                Wing = wing;
                InnerWing = innerWing;
                Eye = eye;
                LowerShell = lowerShell;
                Dark = dark;
            }

            public Material Body { get; }
            public Material Wing { get; }
            public Material InnerWing { get; }
            public Material Eye { get; }
            public Material LowerShell { get; }
            public Material Dark { get; }
        }

        [Serializable]
        private sealed class ReviewRecord
        {
            public string createdAt;
            public string scenePath;
            public string placementRoot;
            public int placedCount;
            public string placementRule;
            public float sceneScaleX;
            public float sceneScaleY;
            public float sceneScaleZ;
            public float requestedUniformScale;
            public float facingYawDegrees;
            public float placementXSpacing;
            public float minimumXClearance;
            public float measuredMinimumXGap;
            public float cameraPositionX;
            public float cameraPositionY;
            public float cameraPositionZ;
            public float cameraFrontDirectionX;
            public float cameraFrontDirectionY;
            public float cameraFrontDirectionZ;
            public float cameraFrontDistance;
            public float playerStartPositionX;
            public float playerStartPositionY;
            public float playerStartPositionZ;
            public float playerStartFrontDistance;
            public float playerStartFacingDot;
            public float targetHeightMeters;
            public float targetWidthMeters;
            public float targetDepthMeters;
            public float staticBoundsX;
            public float staticBoundsY;
            public float staticBoundsZ;
            public float approvedWingPanelThickness;
            public string deathMotionRule;
            public float parvumZGap;
            public float corridorParvumZGap;
            public float desiredFugaParvumZGap;
            public float parvumZClearance;
            public AnimationStateRecord[] animationStates;
        }

        [Serializable]
        private sealed class AnimationStateRecord
        {
            public string objectName;
            public string clipName;
            public string description;
            public bool looping;
        }
    }
}
