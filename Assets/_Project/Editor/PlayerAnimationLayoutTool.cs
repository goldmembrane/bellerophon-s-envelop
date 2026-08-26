using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class PlayerAnimationLayoutTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ModelPath = "Assets/_Project/Art/Player/player.fbx";
        private const string MaterialFolder = "Assets/_Project/Art/Player/Materials";
        private const string TextureFolder = "Assets/_Project/Art/Player/Textures";
        private const string AnimationFolder = "Assets/_Project/Art/Player/Animations";
        private const string IdleClipPath = AnimationFolder + "/Player_Idle.anim";
        private const string IdleControllerPath = AnimationFolder + "/Player_Idle.controller";
        private const string RequirementsPath = "docs/PLAYER_ANIMATION_REQUIREMENTS.html";
        private const string LayoutRootName = "PlayerAnimationLayout";
        private const string IdleKey = "Player_Idle";
        private const string PlayerRootName = "Player";
        private const string AtaRootName = "Approved Ata Enemy Placement";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string IspantRootName = "Approved Ispant Enemy Placement";
        private const float FacingYaw = 180f;
        private const float PositionTolerance = 0.001f;
        private const int CaptureWidth = 3840;
        private const int CaptureHeight = 2160;
        private const int OverviewWidth = 2400;
        private const float IdleDuration = 3f;
        private const float IdleChestTravel = 0.02f;
        private const float IdleShoulderTravel = 0.003f;
        private const float IdleKneeFlexionDegrees = 5f;
        private const float IdleKneeSyncToleranceDegrees = 0.15f;
        private const float IdleFootPositionTolerance = 0.0001f;
        private const float IdleFootRotationToleranceDegrees = 0.02f;
        private const float IdleFrameRate = 60f;
        private const float PlayerStartFrontDistance = 2f;
        private const int PlayerStartCaptureWidth = 1920;
        private const int PlayerStartCaptureHeight = 1080;
        private static readonly float[] IdleReviewTimes = { 0f, 0.75f, 1.5f, 2.25f };

        [MenuItem("Bellerophon/Player/Arrange Animation Layout")]
        public static void Arrange()
        {
            var scene = RequireScene();
            var tables = ReadProductionKeyTables();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException(
                                 "The imported player FBX is unavailable: " + ModelPath);

            var ata = RequireRoot(AtaRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var ispant = RequireRoot(IspantRootName).transform;
            var zSpacing = RequireZSpacing(longa, tergo);
            var xSpacing = RequireIspantXSpacing(ispant);
            var firstPosition = new Vector3(
                ata.position.x,
                ata.position.y,
                ata.position.z - 2f * zSpacing);

            var existing = GameObject.Find(LayoutRootName);
            if (existing != null)
            {
                if (existing.transform.parent != null)
                {
                    throw new InvalidOperationException(
                        LayoutRootName + " exists but is not a scene root.");
                }

                UnityEngine.Object.DestroyImmediate(existing);
            }

            var layoutRoot = new GameObject(LayoutRootName);
            SceneManager.MoveGameObjectToScene(layoutRoot, scene);
            layoutRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            layoutRoot.transform.localScale = Vector3.one;

            for (var tableIndex = 0; tableIndex < tables.Count; tableIndex++)
            {
                var table = tables[tableIndex];
                for (var keyIndex = 0; keyIndex < table.Keys.Count; keyIndex++)
                {
                    var instance = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                                   throw new InvalidOperationException(
                                       "The player FBX could not be instantiated for " +
                                       table.Keys[keyIndex] + ".");
                    instance.name = table.Keys[keyIndex];
                    instance.transform.SetParent(layoutRoot.transform, false);
                    instance.transform.localPosition = firstPosition + new Vector3(
                        keyIndex * xSpacing,
                        0f,
                        -tableIndex * zSpacing);
                    instance.transform.localRotation = Quaternion.Euler(0f, FacingYaw, 0f);
                    instance.transform.localScale = Vector3.one;
                    DisableAnimationPlayback(instance.transform);
                    EditorUtility.SetDirty(instance);
                }
            }

            EditorUtility.SetDirty(layoutRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            var metrics = InspectLayout(scene, tables, requireNoAnimationPlayback: true);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            }

            Debug.Log(
                "PlayerAnimationLayout arranged." +
                " Tables=" + metrics.TableCount.ToString(CultureInfo.InvariantCulture) +
                ", ProductionKeys=" + metrics.KeyCount.ToString(CultureInfo.InvariantCulture) +
                ", FirstPosition=" + Vec(metrics.FirstPosition) +
                ", IspantXSpacing=" + Num(metrics.XSpacing) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", FacingYaw=" + Num(FacingYaw) +
                ", AnimationApplied=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Player/Apply Standing Idle Animation")]
        public static void ApplyIdleAnimation()
        {
            var scene = RequireScene();
            var tables = ReadProductionKeyTables();
            InspectLayout(scene, tables);
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var idleInstance = RequireDirectChild(layoutRoot, IdleKey);
            var idleBefore = new TransformState(idleInstance);
            var otherAnimationStates = OtherAnimationStates(layoutRoot, idleInstance);

            EnsureAssetFolder(AnimationFolder);
            var clip = CreateIdleClip(idleInstance);
            var controller = CreateIdleController(clip);
            ConfigureIdleAnimator(idleInstance, controller);

            if (!idleBefore.Matches(idleInstance))
            {
                throw new InvalidOperationException(
                    "Player_Idle root transform changed while applying the idle animation.");
            }

            RequireEqual(
                otherAnimationStates,
                OtherAnimationStates(layoutRoot, idleInstance),
                "A player instance outside Player_Idle changed animation state.");
            var metrics = InspectIdleAnimation(tables, clip, controller);
            EditorUtility.SetDirty(idleInstance.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            }

            Debug.Log(
                "PlayerIdleAnimation applied." +
                " Duration=" + Num(clip.length) +
                ", ChestVerticalTravel=" + Num(metrics.ChestVerticalTravel) +
                ", LeftKneeFlexion=" + Num(metrics.LeftKneeFlexion) +
                ", RightKneeFlexion=" + Num(metrics.RightKneeFlexion) +
                ", MaximumFootError=" + Num(metrics.MaximumFootError) +
                ", MaximumFootRotationError=" + Num(metrics.MaximumFootRotationError) +
                ", LoopBoundaryError=" + Num(metrics.LoopBoundaryError) +
                ", AnimatedInstance=" + IdleKey +
                ", OtherInstancesUnchanged=True" +
                ", SceneSaved=True.");
        }

        public static void CaptureIdleAnimation(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(
                    "CapturePlayerIdleAnimation requires an output path.",
                    nameof(outputPath));
            }

            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var tables = ReadProductionKeyTables();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath) ??
                       throw new InvalidOperationException("Player_Idle clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(IdleControllerPath) ??
                             throw new InvalidOperationException(
                                 "Player_Idle controller is missing.");
            var metrics = InspectIdleAnimation(tables, clip, controller);
            var destination = Absolute(outputPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The capture path has no directory."));

            CaptureIdlePhaseStrip(clip, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player_Idle capture changed the scene dirty state.");
            }

            Debug.Log(
                "PlayerIdleAnimation captured." +
                " Output=" + destination +
                ", ReviewTimes=0,0.75,1.5,2.25" +
                ", Duration=" + Num(clip.length) +
                ", ChestVerticalTravel=" + Num(metrics.ChestVerticalTravel) +
                ", LeftKneeFlexion=" + Num(metrics.LeftKneeFlexion) +
                ", RightKneeFlexion=" + Num(metrics.RightKneeFlexion) +
                ", MaximumFootError=" + Num(metrics.MaximumFootError) +
                ", MaximumFootRotationError=" + Num(metrics.MaximumFootRotationError) +
                ", LoopBoundaryError=" + Num(metrics.LoopBoundaryError) +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Player/Position Start View At Standing Idle")]
        public static void ApplyPlayerStartView()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player start view must be applied in Edit Mode.");
            }

            var scene = RequireScene();
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var idleInstance = RequireDirectChild(layoutRoot, IdleKey);
            var player = RequireRoot(PlayerRootName).transform;
            var camera = RequirePlayerCamera(player);
            var bounds = BoundsOf(idleInstance
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled));
            var front = Vector3.ProjectOnPlane(idleInstance.forward, Vector3.up).normalized;
            if (front.sqrMagnitude < 0.99f)
            {
                throw new InvalidOperationException(
                    "Player_Idle has no usable horizontal front direction.");
            }

            var position = idleInstance.position + front * PlayerStartFrontDistance;
            position.y = idleInstance.position.y;
            var lookDirection = Vector3.ProjectOnPlane(bounds.center - position, Vector3.up);
            player.SetPositionAndRotation(
                position,
                Quaternion.LookRotation(lookDirection.normalized, Vector3.up));
            camera.transform.localRotation = Quaternion.identity;

            var metrics = InspectPlayerStartView(player, idleInstance, camera, bounds);
            EditorUtility.SetDirty(player.gameObject);
            EditorUtility.SetDirty(camera.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp scene save failed.");
            }

            Debug.Log(
                "PlayerStartView applied." +
                " PlayerPosition=" + Vec(player.position) +
                ", PlayerYaw=" + Num(player.eulerAngles.y) +
                ", IdlePosition=" + Vec(idleInstance.position) +
                ", FrontDistance=" + Num(metrics.FrontDistance) +
                ", FacingDot=" + Num(metrics.FacingDot) +
                ", UpperBodyViewport=" + Vec(metrics.UpperBodyViewport) +
                ", SceneSaved=True.");
        }

        public static void EnterPlayerStartViewPlayMode()
        {
            RequireScene();
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode();
            }
        }

        public static void ExitPlayerStartViewPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
            }
        }

        public static void CapturePlayerStartView(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(
                    "CapturePlayerStartView requires an output path.",
                    nameof(outputPath));
            }

            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Player start view must be captured in Play Mode.");
            }

            RequireScene();
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var idleInstance = RequireDirectChild(layoutRoot, IdleKey);
            var player = RequireRoot(PlayerRootName).transform;
            var camera = RequirePlayerCamera(player);
            var bounds = BoundsOf(idleInstance
                .GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled));
            var metrics = InspectPlayerStartView(player, idleInstance, camera, bounds);
            var destination = Absolute(outputPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The capture path has no directory."));

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(
                PlayerStartCaptureWidth,
                PlayerStartCaptureHeight,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                PlayerStartCaptureWidth,
                PlayerStartCaptureHeight,
                TextureFormat.RGB24,
                false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        PlayerStartCaptureWidth,
                        PlayerStartCaptureHeight),
                    0,
                    0);
                image.Apply(false, false);
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(image);
            }

            Debug.Log(
                "PlayerStartView captured." +
                " Output=" + destination +
                ", PlayerPosition=" + Vec(player.position) +
                ", FrontDistance=" + Num(metrics.FrontDistance) +
                ", FacingDot=" + Num(metrics.FacingDot) +
                ", UpperBodyViewport=" + Vec(metrics.UpperBodyViewport) +
                ", PlayMode=True" +
                ", TargetUnchanged=True.");
        }

        private static PlayerStartViewMetrics InspectPlayerStartView(
            Transform player,
            Transform idleInstance,
            Camera camera,
            Bounds bounds)
        {
            var idleToPlayer = player.position - idleInstance.position;
            var horizontalOffset = Vector3.ProjectOnPlane(idleToPlayer, Vector3.up);
            var front = Vector3.ProjectOnPlane(idleInstance.forward, Vector3.up).normalized;
            var frontDistance = Vector3.Dot(horizontalOffset, front);
            var lateralError = (horizontalOffset - front * frontDistance).magnitude;
            if (Mathf.Abs(frontDistance - PlayerStartFrontDistance) > PositionTolerance ||
                lateralError > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Player start is not on the Player_Idle front axis. Distance=" +
                    Num(frontDistance) + ", LateralError=" + Num(lateralError) + ".");
            }

            var horizontalLook = Vector3.ProjectOnPlane(
                bounds.center - camera.transform.position,
                Vector3.up).normalized;
            var facingDot = Vector3.Dot(player.forward, horizontalLook);
            if (facingDot < 0.999f)
            {
                throw new InvalidOperationException(
                    "Player start is not facing Player_Idle. Dot=" + Num(facingDot) + ".");
            }

            var upperBodyPoint = bounds.center + Vector3.up * bounds.extents.y * 0.35f;
            var upperBodyViewport = camera.WorldToViewportPoint(upperBodyPoint);
            if (upperBodyViewport.z <= camera.nearClipPlane ||
                upperBodyViewport.x < 0.15f || upperBodyViewport.x > 0.85f ||
                upperBodyViewport.y < 0.2f || upperBodyViewport.y > 0.8f)
            {
                throw new InvalidOperationException(
                    "Player_Idle upper body is not framed by the player camera. Viewport=" +
                    Vec(upperBodyViewport) + ".");
            }

            return new PlayerStartViewMetrics(
                frontDistance,
                facingDot,
                upperBodyViewport);
        }

        private static Camera RequirePlayerCamera(Transform player)
        {
            var cameras = player.GetComponentsInChildren<Camera>(true);
            if (cameras.Length != 1)
            {
                throw new InvalidOperationException(
                    "Player must contain exactly one camera. Count=" + cameras.Length + ".");
            }

            return cameras[0];
        }

        private static AnimationClip CreateIdleClip(Transform idleInstance)
        {
            var animator = RequireAnimator(idleInstance);
            var chest = RequireRigBone(
                animator,
                HumanBodyBones.Chest,
                "Chest",
                "UpperChest",
                "Chest",
                "Spine2",
                "Spine02",
                "Spine3",
                "Spine03");
            var leftShoulder = RequireRigBone(
                animator,
                HumanBodyBones.LeftShoulder,
                "LeftShoulder",
                "LeftShoulder",
                "ShoulderL",
                "LShoulder");
            var rightShoulder = RequireRigBone(
                animator,
                HumanBodyBones.RightShoulder,
                "RightShoulder",
                "RightShoulder",
                "ShoulderR",
                "RShoulder");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            var isNewClip = clip == null;
            if (isNewClip)
            {
                clip = new AnimationClip();
            }
            else
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                }
            }

            clip.name = IdleKey;
            clip.frameRate = IdleFrameRate;
            clip.legacy = false;
            clip.wrapMode = WrapMode.Loop;

            AddVerticalBreathingCurves(
                clip,
                animator.transform,
                chest,
                IdleChestTravel);
            AddVerticalBreathingCurves(
                clip,
                animator.transform,
                leftShoulder,
                IdleShoulderTravel);
            AddVerticalBreathingCurves(
                clip,
                animator.transform,
                rightShoulder,
                IdleShoulderTravel);
            AddKneeBendCurves(clip);
            clip.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (isNewClip)
            {
                AssetDatabase.CreateAsset(clip, IdleClipPath);
            }

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                IdleClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath) ??
                   throw new InvalidOperationException(
                       "Player_Idle was not reloaded after saving.");
            if (!File.Exists(Absolute(IdleClipPath)))
            {
                throw new InvalidOperationException(
                    "Player_Idle was not written to disk.");
            }

            if (Mathf.Abs(clip.length - IdleDuration) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Player_Idle clip duration differs from three seconds.");
            }

            return clip;
        }

        private static void AddVerticalBreathingCurves(
            AnimationClip clip,
            Transform animationRoot,
            Transform bone,
            float travel)
        {
            if (bone.parent == null)
            {
                throw new InvalidOperationException(
                    bone.name + " has no parent for a local breathing curve.");
            }

            var localDelta = bone.parent.InverseTransformVector(Vector3.up * travel);
            var path = AnimationUtility.CalculateTransformPath(bone, animationRoot);
            SetPositionCurve(
                clip,
                path,
                "m_LocalPosition.x",
                bone.localPosition.x,
                localDelta.x);
            SetPositionCurve(
                clip,
                path,
                "m_LocalPosition.y",
                bone.localPosition.y,
                localDelta.y);
            SetPositionCurve(
                clip,
                path,
                "m_LocalPosition.z",
                bone.localPosition.z,
                localDelta.z);
        }

        private static void SetPositionCurve(
            AnimationClip clip,
            string path,
            string property,
            float baseline,
            float delta)
        {
            const float quarterDerivative = Mathf.PI / IdleDuration;
            var curve = new AnimationCurve(
                new Keyframe(0f, baseline, 0f, 0f),
                new Keyframe(
                    IdleDuration * 0.25f,
                    baseline + delta * 0.5f,
                    delta * quarterDerivative,
                    delta * quarterDerivative),
                new Keyframe(
                    IdleDuration * 0.5f,
                    baseline + delta,
                    0f,
                    0f),
                new Keyframe(
                    IdleDuration * 0.75f,
                    baseline + delta * 0.5f,
                    -delta * quarterDerivative,
                    -delta * quarterDerivative),
                new Keyframe(IdleDuration, baseline, 0f, 0f))
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static void AddKneeBendCurves(AnimationClip clip)
        {
            var scene = RequireScene();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException(
                                 "The imported player FBX is unavailable.");
            var sample = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                         throw new InvalidOperationException(
                             "The player FBX could not be instantiated for knee baking.");
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                var rig = RequireIdleKneeRig(animator);
                var frameCount = Mathf.RoundToInt(IdleDuration * IdleFrameRate);
                var times = new float[frameCount + 1];
                var hipsPositions = new Vector3[frameCount + 1];
                var legPositions = new Dictionary<Transform, Vector3[]>
                {
                    { rig.Left.Upper, new Vector3[frameCount + 1] },
                    { rig.Right.Upper, new Vector3[frameCount + 1] },
                    { rig.Left.Foot, new Vector3[frameCount + 1] },
                    { rig.Right.Foot, new Vector3[frameCount + 1] }
                };
                var rotations = rig.AnimatedLegBones.ToDictionary(
                    bone => bone,
                    bone => new Quaternion[frameCount + 1]);

                for (var frame = 0; frame <= frameCount; frame++)
                {
                    rig.Restore();
                    var time = frame / IdleFrameRate;
                    var weight = 0.5f - 0.5f * Mathf.Cos(
                        2f * Mathf.PI * time / IdleDuration);
                    var desiredLeftFlexion = rig.Left.BaselineFlexion +
                                             IdleKneeFlexionDegrees * weight;
                    var desiredRightFlexion = rig.Right.BaselineFlexion +
                                              IdleKneeFlexionDegrees * weight;
                    var leftDrop = RequiredPelvisDrop(
                        rig.Left,
                        desiredLeftFlexion);
                    var rightDrop = RequiredPelvisDrop(
                        rig.Right,
                        desiredRightFlexion);
                    var pelvisDrop = 0.5f * (leftDrop + rightDrop);
                    rig.Hips.localPosition = rig.HipsBaseline.LocalPosition +
                                             rig.Hips.parent.InverseTransformVector(
                                                 Vector3.down * pelvisDrop);
                    ApplyUpperLegDrop(rig.Left, leftDrop - pelvisDrop);
                    ApplyUpperLegDrop(rig.Right, rightDrop - pelvisDrop);
                    SolvePlantedLeg(rig.Left);
                    SolvePlantedLeg(rig.Right);

                    var footPositionError = Mathf.Max(
                        rig.Left.FootPositionError(),
                        rig.Right.FootPositionError());
                    var footRotationError = Mathf.Max(
                        rig.Left.FootRotationErrorDegrees(),
                        rig.Right.FootRotationErrorDegrees());
                    if (footPositionError > IdleFootPositionTolerance ||
                        footRotationError > IdleFootRotationToleranceDegrees)
                    {
                        throw new InvalidOperationException(
                            "Knee bake moved a planted foot. Frame=" + frame +
                            ", PositionError=" + Num(footPositionError) +
                            ", RotationError=" + Num(footRotationError) + ".");
                    }

                    times[frame] = time;
                    hipsPositions[frame] = rig.Hips.localPosition;
                    legPositions[rig.Left.Upper][frame] = rig.Left.Upper.localPosition;
                    legPositions[rig.Right.Upper][frame] = rig.Right.Upper.localPosition;
                    legPositions[rig.Left.Foot][frame] = rig.Left.Foot.localPosition;
                    legPositions[rig.Right.Foot][frame] = rig.Right.Foot.localPosition;
                    foreach (var bone in rig.AnimatedLegBones)
                    {
                        var rotation = bone.localRotation;
                        if (frame > 0 && Quaternion.Dot(
                                rotations[bone][frame - 1],
                                rotation) < 0f)
                        {
                            rotation = Negate(rotation);
                        }

                        rotations[bone][frame] = rotation;
                    }
                }

                var hipsPath = AnimationUtility.CalculateTransformPath(
                    rig.Hips,
                    animator.transform);
                SetSampledPositionCurves(clip, hipsPath, times, hipsPositions);
                foreach (var pair in legPositions)
                {
                    var path = AnimationUtility.CalculateTransformPath(
                        pair.Key,
                        animator.transform);
                    SetSampledPositionCurves(clip, path, times, pair.Value);
                }

                foreach (var pair in rotations)
                {
                    var path = AnimationUtility.CalculateTransformPath(
                        pair.Key,
                        animator.transform);
                    SetSampledRotationCurves(clip, path, times, pair.Value);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        private static float RequiredPelvisDrop(
            IdleLegChain leg,
            float desiredFlexionDegrees)
        {
            var desiredFlexion = desiredFlexionDegrees * Mathf.Deg2Rad;
            var desiredDistance = Mathf.Sqrt(
                leg.UpperLength * leg.UpperLength +
                leg.LowerLength * leg.LowerLength +
                2f * leg.UpperLength * leg.LowerLength * Mathf.Cos(desiredFlexion));
            var offset = leg.UpperBaselineWorldPosition - leg.FootTargetPosition;
            var horizontalSquared = offset.x * offset.x + offset.z * offset.z;
            var verticalSquared = desiredDistance * desiredDistance - horizontalSquared;
            if (verticalSquared <= 0f || offset.y <= 0f)
            {
                throw new InvalidOperationException(
                    "The player leg cannot reach the planted-foot knee target.");
            }

            return offset.y - Mathf.Sqrt(verticalSquared);
        }

        private static void ApplyUpperLegDrop(
            IdleLegChain leg,
            float additionalDrop)
        {
            leg.Upper.localPosition = leg.UpperBaselineLocalPosition +
                                      leg.Upper.parent.InverseTransformVector(
                                          Vector3.down * additionalDrop);
        }

        private static void SolvePlantedLeg(IdleLegChain leg)
        {
            var root = leg.Upper.position;
            var targetOffset = leg.FootTargetPosition - root;
            var distance = targetOffset.magnitude;
            if (distance <= 0.0001f ||
                distance >= leg.UpperLength + leg.LowerLength)
            {
                throw new InvalidOperationException(
                    "The planted foot target is outside the two-bone IK range.");
            }

            var axis = targetOffset / distance;
            var bend = Vector3.ProjectOnPlane(leg.BendDirection, axis);
            if (bend.sqrMagnitude <= 0.000001f)
            {
                throw new InvalidOperationException(
                    "The player knee bend direction is unusable.");
            }

            bend.Normalize();
            var along = (
                leg.UpperLength * leg.UpperLength -
                leg.LowerLength * leg.LowerLength +
                distance * distance) / (2f * distance);
            var height = Mathf.Sqrt(Mathf.Max(
                0f,
                leg.UpperLength * leg.UpperLength - along * along));
            var kneeTarget = root + axis * along + bend * height;

            var currentUpperDirection = leg.Lower.position - root;
            var desiredUpperDirection = kneeTarget - root;
            leg.Upper.rotation = Quaternion.FromToRotation(
                                     currentUpperDirection,
                                     desiredUpperDirection) *
                                 leg.Upper.rotation;

            var currentLowerDirection = leg.Foot.position - leg.Lower.position;
            var desiredLowerDirection = leg.FootTargetPosition - leg.Lower.position;
            leg.Lower.rotation = Quaternion.FromToRotation(
                                     currentLowerDirection,
                                     desiredLowerDirection) *
                                 leg.Lower.rotation;

            for (var iteration = 0; iteration < 8; iteration++)
            {
                RotateBoneTowardTarget(
                    leg.Lower,
                    leg.Foot,
                    leg.FootTargetPosition);
                RotateBoneTowardTarget(
                    leg.Upper,
                    leg.Foot,
                    leg.FootTargetPosition);
                if (Vector3.Distance(
                        leg.Foot.position,
                        leg.FootTargetPosition) <= 0.00001f)
                {
                    break;
                }
            }

            leg.Foot.position = leg.FootTargetPosition;
            leg.Foot.rotation = leg.FootTargetRotation;
        }

        private static void RotateBoneTowardTarget(
            Transform bone,
            Transform end,
            Vector3 target)
        {
            var currentDirection = end.position - bone.position;
            var targetDirection = target - bone.position;
            if (currentDirection.sqrMagnitude <= 0.0000001f ||
                targetDirection.sqrMagnitude <= 0.0000001f)
            {
                return;
            }

            bone.rotation = Quaternion.FromToRotation(
                                currentDirection,
                                targetDirection) *
                            bone.rotation;
        }

        private static void SetSampledPositionCurves(
            AnimationClip clip,
            string path,
            float[] times,
            Vector3[] values)
        {
            SetSampledCurve(
                clip,
                path,
                "m_LocalPosition.x",
                times,
                values.Select(value => value.x).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalPosition.y",
                times,
                values.Select(value => value.y).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalPosition.z",
                times,
                values.Select(value => value.z).ToArray());
        }

        private static void SetSampledRotationCurves(
            AnimationClip clip,
            string path,
            float[] times,
            Quaternion[] values)
        {
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.x",
                times,
                values.Select(value => value.x).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.y",
                times,
                values.Select(value => value.y).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.z",
                times,
                values.Select(value => value.z).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.w",
                times,
                values.Select(value => value.w).ToArray());
        }

        private static void SetSampledCurve(
            AnimationClip clip,
            string path,
            string property,
            float[] times,
            float[] values)
        {
            var keys = new Keyframe[times.Length];
            for (var index = 0; index < times.Length; index++)
            {
                float tangent;
                if (index == 0)
                {
                    tangent = (values[1] - values[0]) / (times[1] - times[0]);
                }
                else if (index == times.Length - 1)
                {
                    tangent = (values[index] - values[index - 1]) /
                              (times[index] - times[index - 1]);
                }
                else
                {
                    tangent = (values[index + 1] - values[index - 1]) /
                              (times[index + 1] - times[index - 1]);
                }

                keys[index] = new Keyframe(
                    times[index],
                    values[index],
                    tangent,
                    tangent);
            }

            var curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.Loop,
                postWrapMode = WrapMode.Loop
            };
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                curve);
        }

        private static Quaternion Negate(Quaternion value)
        {
            return new Quaternion(-value.x, -value.y, -value.z, -value.w);
        }

        private static AnimatorController CreateIdleController(AnimationClip clip)
        {
            DeleteAssetIfPresent(
                IdleControllerPath,
                "Existing Player_Idle controller could not be replaced.");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                IdleControllerPath);
            var state = controller.layers[0].stateMachine.AddState("PlayerIdle");
            state.motion = clip;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void ConfigureIdleAnimator(
            Transform idleInstance,
            AnimatorController controller)
        {
            var animator = RequireAnimator(idleInstance);
            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
        }

        private static IdleMetrics InspectIdleAnimation(
            IReadOnlyList<TableSpec> tables,
            AnimationClip clip,
            AnimatorController controller)
        {
            InspectLayout(RequireScene(), tables);
            if (Mathf.Abs(clip.length - IdleDuration) > 0.0001f ||
                Mathf.Abs(clip.frameRate - IdleFrameRate) > 0.0001f)
            {
                throw new InvalidOperationException(
                    "Player_Idle duration or frame rate differs.");
            }

            var serializedClip = new SerializedObject(clip);
            var loop = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loop == null || !loop.boolValue)
            {
                throw new InvalidOperationException("Player_Idle clip is not looping.");
            }

            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var idleInstance = RequireDirectChild(layoutRoot, IdleKey);
            var animator = RequireAnimator(idleInstance);
            if (!animator.enabled ||
                animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Player_Idle Animator configuration differs.");
            }

            var chest = RequireRigBone(
                animator,
                HumanBodyBones.Chest,
                "Chest",
                "UpperChest",
                "Chest",
                "Spine2",
                "Spine02",
                "Spine3",
                "Spine03");
            var leftShoulder = RequireRigBone(
                animator,
                HumanBodyBones.LeftShoulder,
                "LeftShoulder",
                "LeftShoulder",
                "ShoulderL",
                "LShoulder");
            var rightShoulder = RequireRigBone(
                animator,
                HumanBodyBones.RightShoulder,
                "RightShoulder",
                "RightShoulder",
                "ShoulderR",
                "RShoulder");
            var kneeRig = RequireIdleKneeRig(animator);
            var positionPaths = new[]
                {
                    chest,
                    leftShoulder,
                    rightShoulder,
                    kneeRig.Hips,
                    kneeRig.Left.Upper,
                    kneeRig.Right.Upper,
                    kneeRig.Left.Foot,
                    kneeRig.Right.Foot
                }
                .Select(bone => AnimationUtility.CalculateTransformPath(
                    bone,
                    animator.transform))
                .ToHashSet(StringComparer.Ordinal);
            var rotationPaths = kneeRig.AnimatedLegBones
                .Select(bone => AnimationUtility.CalculateTransformPath(
                    bone,
                    animator.transform))
                .ToHashSet(StringComparer.Ordinal);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 48 || bindings.Any(binding =>
                    !(positionPaths.Contains(binding.path) &&
                      binding.propertyName.StartsWith(
                          "m_LocalPosition.",
                          StringComparison.Ordinal)) &&
                    !(rotationPaths.Contains(binding.path) &&
                      binding.propertyName.StartsWith(
                          "m_LocalRotation.",
                          StringComparison.Ordinal))))
            {
                throw new InvalidOperationException(
                    "Player_Idle curve bindings differ from breathing plus planted-knee motion.");
            }

            return EvaluateIdleMotion(clip);
        }

        private static IdleMetrics EvaluateIdleMotion(AnimationClip clip)
        {
            var scene = RequireScene();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException(
                                 "The imported player FBX is unavailable.");
            var sample = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                         throw new InvalidOperationException(
                             "The player FBX could not be instantiated for idle evaluation.");
            sample.hideFlags = HideFlags.HideAndDontSave;
            sample.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            sample.transform.localScale = Vector3.one;
            try
            {
                var animator = RequireAnimator(sample.transform);
                var chest = RequireRigBone(
                    animator,
                    HumanBodyBones.Chest,
                    "Chest",
                    "UpperChest",
                    "Chest",
                    "Spine2",
                    "Spine02",
                    "Spine3",
                    "Spine03");
                var leftShoulder = RequireRigBone(
                    animator,
                    HumanBodyBones.LeftShoulder,
                    "LeftShoulder",
                    "LeftShoulder",
                    "ShoulderL",
                    "LShoulder");
                var rightShoulder = RequireRigBone(
                    animator,
                    HumanBodyBones.RightShoulder,
                    "RightShoulder",
                    "RightShoulder",
                    "ShoulderR",
                    "RShoulder");
                var kneeRig = RequireIdleKneeRig(animator);
                clip.SampleAnimation(animator.gameObject, 0f);
                var baseChestLocalPosition = chest.localPosition;
                var leftFootBase = new Pose(kneeRig.Left.Foot);
                var rightFootBase = new Pose(kneeRig.Right.Foot);
                var baseLeftFlexion = kneeRig.Left.KneeFlexionDegrees();
                var baseRightFlexion = kneeRig.Right.KneeFlexionDegrees();
                var loopStart = new[]
                {
                    new Pose(chest),
                    new Pose(leftShoulder),
                    new Pose(rightShoulder),
                    new Pose(kneeRig.Hips)
                };
                var legLoopStart = kneeRig.AnimatedLegBones
                    .Select(bone => new Pose(bone))
                    .ToArray();
                var minimumChestY = 0f;
                var maximumChestY = 0f;
                var maximumFootError = 0f;
                var maximumFootRotationError = 0f;
                var maximumLeftFlexion = 0f;
                var maximumRightFlexion = 0f;
                var maximumFlexionDifference = 0f;
                var frameCount = Mathf.RoundToInt(IdleDuration * IdleFrameRate);
                for (var frame = 0; frame <= frameCount; frame++)
                {
                    var time = frame / IdleFrameRate;
                    clip.SampleAnimation(animator.gameObject, time);
                    var chestLocalVertical = Vector3.Dot(
                        chest.parent.TransformVector(
                            chest.localPosition - baseChestLocalPosition),
                        Vector3.up);
                    minimumChestY = Mathf.Min(minimumChestY, chestLocalVertical);
                    maximumChestY = Mathf.Max(maximumChestY, chestLocalVertical);
                    maximumFootError = Mathf.Max(
                        maximumFootError,
                        Vector3.Distance(
                            leftFootBase.Position,
                            kneeRig.Left.Foot.position),
                        Vector3.Distance(
                            rightFootBase.Position,
                            kneeRig.Right.Foot.position));
                    maximumFootRotationError = Mathf.Max(
                        maximumFootRotationError,
                        Quaternion.Angle(
                            leftFootBase.Rotation,
                            kneeRig.Left.Foot.rotation),
                        Quaternion.Angle(
                            rightFootBase.Rotation,
                            kneeRig.Right.Foot.rotation));
                    var leftFlexion = kneeRig.Left.KneeFlexionDegrees() - baseLeftFlexion;
                    var rightFlexion = kneeRig.Right.KneeFlexionDegrees() - baseRightFlexion;
                    maximumLeftFlexion = Mathf.Max(maximumLeftFlexion, leftFlexion);
                    maximumRightFlexion = Mathf.Max(maximumRightFlexion, rightFlexion);
                    maximumFlexionDifference = Mathf.Max(
                        maximumFlexionDifference,
                        Mathf.Abs(leftFlexion - rightFlexion));
                }

                clip.SampleAnimation(animator.gameObject, IdleDuration);
                var loopBones = new[]
                {
                    chest,
                    leftShoulder,
                    rightShoulder,
                    kneeRig.Hips
                };
                var loopBoundaryError = Mathf.Max(
                    Enumerable.Range(0, loopBones.Length)
                        .Max(index => loopStart[index].Error(loopBones[index])),
                    Enumerable.Range(0, kneeRig.AnimatedLegBones.Length)
                        .Max(index => legLoopStart[index].Error(
                            kneeRig.AnimatedLegBones[index])));
                var chestTravel = maximumChestY - minimumChestY;
                if (Mathf.Abs(chestTravel - IdleChestTravel) > 0.0005f)
                {
                    throw new InvalidOperationException(
                        "Player_Idle chest travel differs from two centimeters. Actual=" +
                        Num(chestTravel) + ".");
                }

                if (maximumFootError > IdleFootPositionTolerance ||
                    maximumFootRotationError > IdleFootRotationToleranceDegrees)
                {
                    throw new InvalidOperationException(
                        "Player_Idle feet moved. PositionError=" +
                        Num(maximumFootError) + ", RotationError=" +
                        Num(maximumFootRotationError) + ".");
                }

                if (Mathf.Abs(maximumLeftFlexion - IdleKneeFlexionDegrees) > 0.25f ||
                    Mathf.Abs(maximumRightFlexion - IdleKneeFlexionDegrees) > 0.25f)
                {
                    throw new InvalidOperationException(
                        "Player_Idle knee flexion differs from five degrees. Left=" +
                        Num(maximumLeftFlexion) + ", Right=" +
                        Num(maximumRightFlexion) + ".");
                }

                if (maximumFlexionDifference > IdleKneeSyncToleranceDegrees)
                {
                    throw new InvalidOperationException(
                        "Player_Idle knees are not synchronized. Difference=" +
                        Num(maximumFlexionDifference) + ".");
                }

                if (loopBoundaryError > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "Player_Idle loop boundary differs. Error=" +
                        Num(loopBoundaryError) + ".");
                }

                return new IdleMetrics(
                    chestTravel,
                    maximumLeftFlexion,
                    maximumRightFlexion,
                    maximumFootError,
                    maximumFootRotationError,
                    loopBoundaryError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sample);
            }
        }

        [MenuItem("Bellerophon/Player/Apply Embedded Materials")]
        public static void ApplyMaterials()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var tables = ReadProductionKeyTables();
            var layoutBefore = InspectLayout(scene, tables);
            var importMetrics = ExtractAndRemapEmbeddedMaterials();
            var layoutAfter = InspectLayout(scene, tables);
            RequireSameLayout(layoutBefore, layoutAfter);
            var materialMetrics = InspectPlayerMaterials(tables);

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player material import changed the scene dirty state.");
            }

            Debug.Log(
                "PlayerAnimationMaterials applied." +
                " EmbeddedMaterials=" +
                importMetrics.EmbeddedMaterialCount.ToString(CultureInfo.InvariantCulture) +
                ", ExtractedTextures=" +
                materialMetrics.TextureCount.ToString(CultureInfo.InvariantCulture) +
                ", ExternalMaterials=" +
                materialMetrics.MaterialCount.ToString(CultureInfo.InvariantCulture) +
                ", MaterialSlotsPerInstance=" +
                materialMetrics.MaterialSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", VerifiedInstances=" +
                materialMetrics.InstanceCount.ToString(CultureInfo.InvariantCulture) +
                ", LayoutUnchanged=True" +
                ", AnimationApplied=False" +
                ", SceneChanged=False.");
        }

        public static void CaptureMaterials(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(
                    "CapturePlayerAnimationMaterials requires an output path.",
                    nameof(outputPath));
            }

            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var tables = ReadProductionKeyTables();
            InspectLayout(scene, tables);
            var materialMetrics = InspectPlayerMaterials(tables);
            var destination = Absolute(outputPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The capture path has no directory."));

            CaptureMaterialComparison(destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player material capture changed the scene dirty state.");
            }

            Debug.Log(
                "PlayerAnimationMaterials captured." +
                " Output=" + destination +
                ", ExternalMaterials=" +
                materialMetrics.MaterialCount.ToString(CultureInfo.InvariantCulture) +
                ", ExtractedTextures=" +
                materialMetrics.TextureCount.ToString(CultureInfo.InvariantCulture) +
                ", MaterialSlotsPerInstance=" +
                materialMetrics.MaterialSlotCount.ToString(CultureInfo.InvariantCulture) +
                ", VerifiedInstances=" +
                materialMetrics.InstanceCount.ToString(CultureInfo.InvariantCulture) +
                ", SourceAndSceneCompared=True" +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        public static void Capture(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(
                    "CapturePlayerAnimationLayout requires an output path.",
                    nameof(outputPath));
            }

            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var tables = ReadProductionKeyTables();
            var metrics = InspectLayout(scene, tables);
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var destination = Absolute(outputPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("The capture path has no directory."));

            CaptureComposite(layoutRoot, tables, destination);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Player animation layout capture changed the scene dirty state.");
            }

            Debug.Log(
                "PlayerAnimationLayout captured." +
                " Output=" + destination +
                ", Tables=" + metrics.TableCount.ToString(CultureInfo.InvariantCulture) +
                ", ProductionKeys=" + metrics.KeyCount.ToString(CultureInfo.InvariantCulture) +
                ", FirstPosition=" + Vec(metrics.FirstPosition) +
                ", IspantXSpacing=" + Num(metrics.XSpacing) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        private static LayoutMetrics InspectLayout(
            Scene scene,
            IReadOnlyList<TableSpec> tables,
            bool requireNoAnimationPlayback = false)
        {
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var ata = RequireRoot(AtaRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var ispant = RequireRoot(IspantRootName).transform;
            var zSpacing = RequireZSpacing(longa, tergo);
            var xSpacing = RequireIspantXSpacing(ispant);
            var firstPosition = new Vector3(
                ata.position.x,
                ata.position.y,
                ata.position.z - 2f * zSpacing);
            var expectedCount = tables.Sum(table => table.Keys.Count);
            if (layoutRoot.childCount != expectedCount)
            {
                throw new InvalidOperationException(
                    "Player animation layout instance count differs. Expected=" +
                    expectedCount + ", Actual=" + layoutRoot.childCount + ".");
            }

            var childIndex = 0;
            for (var tableIndex = 0; tableIndex < tables.Count; tableIndex++)
            {
                var table = tables[tableIndex];
                for (var keyIndex = 0; keyIndex < table.Keys.Count; keyIndex++)
                {
                    var instance = layoutRoot.GetChild(childIndex++);
                    var expectedPosition = firstPosition + new Vector3(
                        keyIndex * xSpacing,
                        0f,
                        -tableIndex * zSpacing);
                    if (instance.name != table.Keys[keyIndex] ||
                        Vector3.Distance(instance.position, expectedPosition) > PositionTolerance ||
                        Quaternion.Angle(
                            instance.rotation,
                            Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                        Vector3.Distance(instance.localScale, Vector3.one) > PositionTolerance)
                    {
                        throw new InvalidOperationException(
                            "Player animation layout contract differs at " +
                            table.Title + " / " + table.Keys[keyIndex] + ".");
                    }

                    var source = PrefabUtility.GetCorrespondingObjectFromSource(
                        instance.gameObject);
                    if (source == null || AssetDatabase.GetAssetPath(source) != ModelPath)
                    {
                        throw new InvalidOperationException(
                            instance.name + " is not a direct instance of the player FBX.");
                    }

                    if (!instance.GetComponentsInChildren<Renderer>(true).Any())
                    {
                        throw new InvalidOperationException(
                            instance.name + " has no renderer.");
                    }

                    if ((requireNoAnimationPlayback &&
                         instance.GetComponentsInChildren<Animator>(true)
                             .Any(animator =>
                                 animator.enabled ||
                                 animator.runtimeAnimatorController != null)) ||
                        instance.GetComponentsInChildren<Animation>(true)
                            .Any(animation => animation.enabled))
                    {
                        throw new InvalidOperationException(
                            instance.name + " has animation playback enabled.");
                    }
                }
            }

            return new LayoutMetrics
            {
                TableCount = tables.Count,
                KeyCount = expectedCount,
                FirstPosition = firstPosition,
                XSpacing = xSpacing,
                ZSpacing = zSpacing
            };
        }

        private static MaterialImportMetrics ExtractAndRemapEmbeddedMaterials()
        {
            EnsureAssetFolder(MaterialFolder);
            EnsureAssetFolder(TextureFolder);
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                           throw new InvalidOperationException(
                               "The player FBX does not have a ModelImporter.");

            importer.ExtractTextures(TextureFolder);
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                       throw new InvalidOperationException(
                           "The player FBX importer was lost after texture extraction.");
            var embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Material>()
                .Where(material => AssetDatabase.GetAssetPath(material) == ModelPath)
                .OrderBy(material => material.name, StringComparer.Ordinal)
                .ToArray();
            if (embeddedMaterials.Length == 0)
            {
                throw new InvalidOperationException(
                    "The player FBX contains no embedded material to extract.");
            }

            for (var index = 0; index < embeddedMaterials.Length; index++)
            {
                var sourceMaterial = embeddedMaterials[index];
                var fileName = SanitizeFileName(sourceMaterial.name);
                if (embeddedMaterials.Count(material => material.name == sourceMaterial.name) > 1)
                {
                    fileName += "_" + index.ToString("D2", CultureInfo.InvariantCulture);
                }

                var materialPath = MaterialFolder + "/" + fileName + ".mat";
                var externalMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (externalMaterial == null)
                {
                    externalMaterial = new Material(sourceMaterial)
                    {
                        name = sourceMaterial.name
                    };
                    AssetDatabase.CreateAsset(externalMaterial, materialPath);
                }
                else
                {
                    EditorUtility.CopySerialized(sourceMaterial, externalMaterial);
                    externalMaterial.name = sourceMaterial.name;
                    EditorUtility.SetDirty(externalMaterial);
                }

                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(
                        typeof(Material),
                        sourceMaterial.name),
                    externalMaterial);
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.External;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            return new MaterialImportMetrics
            {
                EmbeddedMaterialCount = embeddedMaterials.Length
            };
        }

        private static PlayerMaterialMetrics InspectPlayerMaterials(
            IReadOnlyList<TableSpec> tables)
        {
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException(
                                 "The imported player FBX is unavailable.");
            var sourceRenderers = OrderedRenderers(modelAsset.transform);
            if (sourceRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "The imported player FBX has no renderer.");
            }

            var sourceSlots = MaterialSlotPaths(sourceRenderers);
            var materials = sourceRenderers
                .SelectMany(renderer => renderer.sharedMaterials)
                .Where(material => material != null)
                .Distinct()
                .ToArray();
            if (materials.Length == 0)
            {
                throw new InvalidOperationException(
                    "The player FBX has no assigned material.");
            }

            foreach (var material in materials)
            {
                var materialPath = AssetDatabase.GetAssetPath(material);
                if (!materialPath.StartsWith(MaterialFolder + "/", StringComparison.Ordinal) ||
                    material.shader == null ||
                    material.shader.name == "Hidden/InternalErrorShader")
                {
                    throw new InvalidOperationException(
                        "Player material is not a valid extracted material: " +
                        material.name + ".");
                }
            }

            var textures = materials
                .SelectMany(material => material.GetTexturePropertyNames()
                    .Select(material.GetTexture))
                .Where(texture => texture != null)
                .Distinct()
                .ToArray();
            if (textures.Length == 0)
            {
                throw new InvalidOperationException(
                    "The extracted player materials reference no texture.");
            }

            foreach (var texture in textures)
            {
                var texturePath = AssetDatabase.GetAssetPath(texture);
                if (!texturePath.StartsWith(TextureFolder + "/", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Player texture was not extracted locally: " + texture.name + ".");
                }
            }

            var layoutRoot = RequireRoot(LayoutRootName).transform;
            var expectedInstances = tables.Sum(table => table.Keys.Count);
            for (var index = 0; index < layoutRoot.childCount; index++)
            {
                var instanceSlots = MaterialSlotPaths(
                    OrderedRenderers(layoutRoot.GetChild(index)));
                if (!sourceSlots.SequenceEqual(instanceSlots, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        layoutRoot.GetChild(index).name +
                        " does not use the source FBX material slots.");
                }
            }

            return new PlayerMaterialMetrics
            {
                MaterialCount = materials.Length,
                TextureCount = textures.Length,
                MaterialSlotCount = sourceSlots.Length,
                InstanceCount = expectedInstances
            };
        }

        private static Renderer[] OrderedRenderers(Transform root)
        {
            return root.GetComponentsInChildren<Renderer>(true)
                .OrderBy(
                    renderer => AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        root),
                    StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] MaterialSlotPaths(IEnumerable<Renderer> renderers)
        {
            return renderers
                .SelectMany(renderer => renderer.sharedMaterials.Select(material =>
                    material == null ? "<null>" : AssetDatabase.GetAssetPath(material)))
                .ToArray();
        }

        private static void RequireSameLayout(
            LayoutMetrics before,
            LayoutMetrics after)
        {
            if (before.TableCount != after.TableCount ||
                before.KeyCount != after.KeyCount ||
                Vector3.Distance(before.FirstPosition, after.FirstPosition) > PositionTolerance ||
                Mathf.Abs(before.XSpacing - after.XSpacing) > PositionTolerance ||
                Mathf.Abs(before.ZSpacing - after.ZSpacing) > PositionTolerance)
            {
                throw new InvalidOperationException(
                    "Player layout changed while applying materials.");
            }
        }

        private static IReadOnlyList<TableSpec> ReadProductionKeyTables()
        {
            var htmlPath = Absolute(RequirementsPath);
            if (!File.Exists(htmlPath))
            {
                throw new FileNotFoundException(
                    "Player animation requirements are missing.",
                    htmlPath);
            }

            var html = File.ReadAllText(htmlPath, Encoding.UTF8);
            var sections = Regex.Matches(
                html,
                @"<h2[^>]*>(?<title>.*?)</h2>(?<body>.*?)(?=<h2[^>]*>|</main>|</body>)",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var result = new List<TableSpec>();
            var allKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match section in sections)
            {
                var tables = Regex.Matches(
                    section.Groups["body"].Value,
                    @"<table[^>]*>(?<table>.*?)</table>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                foreach (Match tableMatch in tables)
                {
                    var tableHtml = tableMatch.Groups["table"].Value;
                    var header = Regex.Match(
                        tableHtml,
                        @"<thead[^>]*>.*?<tr[^>]*>(?<row>.*?)</tr>.*?</thead>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (!header.Success)
                    {
                        continue;
                    }

                    var headers = ReadCells(header.Groups["row"].Value);
                    var keyColumn = headers.FindIndex(value => value == "제작 키");
                    if (keyColumn < 0)
                    {
                        continue;
                    }

                    var body = Regex.Match(
                        tableHtml,
                        @"<tbody[^>]*>(?<body>.*?)</tbody>",
                        RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (!body.Success)
                    {
                        throw new InvalidOperationException(
                            "A production-key table has no tbody.");
                    }

                    var keys = new List<string>();
                    foreach (Match row in Regex.Matches(
                                 body.Groups["body"].Value,
                                 @"<tr[^>]*>(?<row>.*?)</tr>",
                                 RegexOptions.Singleline | RegexOptions.IgnoreCase))
                    {
                        var cells = ReadCells(row.Groups["row"].Value);
                        if (keyColumn >= cells.Count ||
                            string.IsNullOrWhiteSpace(cells[keyColumn]))
                        {
                            continue;
                        }

                        var key = cells[keyColumn];
                        if (!allKeys.Add(key))
                        {
                            throw new InvalidOperationException(
                                "Duplicate production key in requirements: " + key);
                        }

                        keys.Add(key);
                    }

                    if (keys.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "A production-key table contains no production keys.");
                    }

                    result.Add(new TableSpec(
                        StripHtml(section.Groups["title"].Value),
                        keys));
                }
            }

            if (result.Count == 0)
            {
                throw new InvalidOperationException(
                    "No tables with a production-key column were found.");
            }

            return result;
        }

        private static List<string> ReadCells(string rowHtml)
        {
            return Regex.Matches(
                    rowHtml,
                    @"<t[dh][^>]*>(?<cell>.*?)</t[dh]>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase)
                .Cast<Match>()
                .Select(match => StripHtml(match.Groups["cell"].Value))
                .ToList();
        }

        private static string StripHtml(string value)
        {
            var withoutTags = Regex.Replace(value, @"<[^>]+>", string.Empty);
            return WebUtility.HtmlDecode(withoutTags)
                .Replace('\u00A0', ' ')
                .Trim();
        }

        private static void DisableAnimationPlayback(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.runtimeAnimatorController = null;
                animator.enabled = false;
                EditorUtility.SetDirty(animator);
            }

            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static void CaptureComposite(
            Transform layoutRoot,
            IReadOnlyList<TableSpec> tables,
            string destination)
        {
            var layoutRenderers = layoutRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (layoutRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Player animation layout has no visible renderers.");
            }

            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(renderer => new RendererState(renderer))
                .ToArray();
            var cameraObject = new GameObject(
                "PlayerAnimationLayout_CaptureCamera",
                typeof(Camera));
            var keyLightObject = new GameObject(
                "PlayerAnimationLayout_KeyLight",
                typeof(Light));
            var fillLightObject = new GameObject(
                "PlayerAnimationLayout_FillLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            keyLightObject.hideFlags = HideFlags.HideAndDontSave;
            fillLightObject.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                foreach (var renderer in allRenderers)
                {
                    renderer.enabled = renderer.transform.IsChildOf(layoutRoot);
                }

                ConfigureLight(
                    keyLightObject.GetComponent<Light>(),
                    Quaternion.Euler(38f, -30f, 0f),
                    1.35f);
                ConfigureLight(
                    fillLightObject.GetComponent<Light>(),
                    Quaternion.Euler(25f, 145f, 0f),
                    0.65f);

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
                camera.orthographic = true;
                camera.allowHDR = false;
                camera.allowMSAA = true;

                var overviewBounds = BoundsOf(layoutRenderers);
                var detailBounds = DetailBounds(layoutRoot, tables);
                var overview = RenderPanel(
                    camera,
                    OverviewWidth,
                    CaptureHeight,
                    overviewBounds,
                    new Vector3(-0.2f, 0.95f, -0.8f));
                var detail = RenderPanel(
                    camera,
                    CaptureWidth - OverviewWidth,
                    CaptureHeight,
                    detailBounds,
                    new Vector3(-0.15f, 0.38f, -1f));
                try
                {
                    var composite = new Texture2D(
                        CaptureWidth,
                        CaptureHeight,
                        TextureFormat.RGB24,
                        false);
                    composite.SetPixels(0, 0, OverviewWidth, CaptureHeight, overview.GetPixels());
                    composite.SetPixels(
                        OverviewWidth,
                        0,
                        CaptureWidth - OverviewWidth,
                        CaptureHeight,
                        detail.GetPixels());
                    for (var x = OverviewWidth - 3; x <= OverviewWidth + 3; x++)
                    {
                        for (var y = 0; y < CaptureHeight; y++)
                        {
                            composite.SetPixel(x, y, new Color(0.18f, 0.25f, 0.35f));
                        }
                    }

                    composite.Apply();
                    File.WriteAllBytes(destination, composite.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(composite);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(overview);
                    UnityEngine.Object.DestroyImmediate(detail);
                }
            }
            finally
            {
                foreach (var state in rendererStates)
                {
                    state.Restore();
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }
        }

        private static void CaptureIdlePhaseStrip(
            AnimationClip clip,
            string destination)
        {
            var scene = RequireScene();
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException(
                                 "The imported player FBX is unavailable.");
            const float spacing = 2.2f;
            var samples = new List<GameObject>();
            var guideObjects = new List<GameObject>();
            Material guideMaterial = null;
            GameObject cameraObject = null;
            GameObject keyLightObject = null;
            GameObject fillLightObject = null;
            RendererState[] rendererStates = null;
            try
            {
                for (var index = 0; index < IdleReviewTimes.Length; index++)
                {
                    var sample = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                                 throw new InvalidOperationException(
                                     "The player FBX could not be instantiated for idle capture.");
                    sample.name = "PlayerIdle_" +
                                  IdleReviewTimes[index].ToString("0.00", CultureInfo.InvariantCulture);
                    sample.hideFlags = HideFlags.HideAndDontSave;
                    sample.transform.SetPositionAndRotation(
                        new Vector3(
                            (index - (IdleReviewTimes.Length - 1) * 0.5f) * spacing,
                            0f,
                            0f),
                        Quaternion.Euler(0f, FacingYaw, 0f));
                    sample.transform.localScale = Vector3.one;
                    var animator = RequireAnimator(sample.transform);
                    animator.enabled = false;
                    animator.runtimeAnimatorController = null;
                    clip.SampleAnimation(animator.gameObject, IdleReviewTimes[index]);
                    samples.Add(sample);
                }

                var sampleRenderers = samples
                    .SelectMany(sample => sample.GetComponentsInChildren<Renderer>(true))
                    .Where(renderer => renderer.enabled)
                    .ToArray();
                var sampleBounds = BoundsOf(sampleRenderers);
                var guideShader = Shader.Find("Universal Render Pipeline/Unlit") ??
                                  Shader.Find("Unlit/Color") ??
                                  throw new InvalidOperationException(
                                      "No unlit shader is available for idle review guides.");
                guideMaterial = new Material(guideShader)
                {
                    name = "PlayerIdleReviewGuide",
                    color = new Color(0.15f, 0.85f, 1f, 1f),
                    hideFlags = HideFlags.HideAndDontSave
                };

                var groundLine = CreateIdleGuide(
                    "PlayerIdle_GroundGuide",
                    new Vector3(0f, sampleBounds.min.y - 0.012f, -0.15f),
                    new Vector3(
                        spacing * IdleReviewTimes.Length + 0.8f,
                        0.008f,
                        0.04f),
                    guideMaterial);
                guideObjects.Add(groundLine);
                foreach (var sample in samples)
                {
                    var animator = RequireAnimator(sample.transform);
                    var chest = RequireRigBone(
                        animator,
                        HumanBodyBones.Chest,
                        "Chest",
                        "UpperChest",
                        "Chest",
                        "Spine2",
                        "Spine02",
                        "Spine3",
                        "Spine03");
                    guideObjects.Add(CreateIdleGuide(
                        sample.name + "_ChestGuide",
                        chest.position + new Vector3(0.62f, 0f, -0.15f),
                        new Vector3(0.34f, 0.008f, 0.04f),
                        guideMaterial));
                }

                var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                rendererStates = allRenderers
                    .Select(renderer => new RendererState(renderer))
                    .ToArray();
                var visibleRenderers = sampleRenderers
                    .Concat(guideObjects.Select(item => item.GetComponent<Renderer>()))
                    .Where(renderer => renderer != null)
                    .ToHashSet();
                foreach (var renderer in allRenderers)
                {
                    renderer.enabled = visibleRenderers.Contains(renderer);
                }

                cameraObject = new GameObject(
                    "PlayerIdleReviewCamera",
                    typeof(Camera));
                keyLightObject = new GameObject(
                    "PlayerIdleReviewKeyLight",
                    typeof(Light));
                fillLightObject = new GameObject(
                    "PlayerIdleReviewFillLight",
                    typeof(Light));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                keyLightObject.hideFlags = HideFlags.HideAndDontSave;
                fillLightObject.hideFlags = HideFlags.HideAndDontSave;
                ConfigureLight(
                    keyLightObject.GetComponent<Light>(),
                    Quaternion.Euler(35f, -25f, 0f),
                    1.7f);
                ConfigureLight(
                    fillLightObject.GetComponent<Light>(),
                    Quaternion.Euler(20f, 150f, 0f),
                    0.85f);

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f, 1f);
                camera.orthographic = true;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                var reviewBounds = BoundsOf(visibleRenderers);
                RequireIdleCaptureBounds(reviewBounds);
                var review = RenderPanel(
                    camera,
                    CaptureWidth,
                    CaptureHeight,
                    reviewBounds,
                    new Vector3(0f, 0.06f, -1f));
                try
                {
                    RequireVisibleIdleCapture(review, camera.backgroundColor);
                    File.WriteAllBytes(destination, review.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(review);
                }
            }
            finally
            {
                if (rendererStates != null)
                {
                    foreach (var state in rendererStates)
                    {
                        state.Restore();
                    }
                }

                foreach (var guide in guideObjects)
                {
                    if (guide != null)
                    {
                        UnityEngine.Object.DestroyImmediate(guide);
                    }
                }

                foreach (var sample in samples)
                {
                    if (sample != null)
                    {
                        UnityEngine.Object.DestroyImmediate(sample);
                    }
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (keyLightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(keyLightObject);
                }

                if (fillLightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(fillLightObject);
                }

                if (guideMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(guideMaterial);
                }
            }
        }

        private static GameObject CreateIdleGuide(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var guide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guide.name = name;
            guide.hideFlags = HideFlags.HideAndDontSave;
            guide.transform.SetPositionAndRotation(position, Quaternion.identity);
            guide.transform.localScale = scale;
            var collider = guide.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            guide.GetComponent<Renderer>().sharedMaterial = material;
            return guide;
        }

        private static void CaptureMaterialComparison(string destination)
        {
            var scene = RequireScene();
            var layoutRoot = RequireRoot(LayoutRootName).transform;
            if (layoutRoot.childCount == 0)
            {
                throw new InvalidOperationException(
                    "Player animation layout contains no instance.");
            }

            var sceneInstance = layoutRoot.GetChild(0);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                             throw new InvalidOperationException(
                                 "The imported player FBX is unavailable.");
            var sourceInstance = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                                 throw new InvalidOperationException(
                                     "The player FBX could not be instantiated for comparison.");
            sourceInstance.name = "PlayerMaterial_SourceComparison";
            sourceInstance.hideFlags = HideFlags.HideAndDontSave;
            var xSpacing = RequireIspantXSpacing(
                RequireRoot(IspantRootName).transform);
            sourceInstance.transform.SetPositionAndRotation(
                sceneInstance.position - Vector3.right * xSpacing,
                sceneInstance.rotation);
            sourceInstance.transform.localScale = sceneInstance.lossyScale;

            var allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var rendererStates = allRenderers
                .Select(renderer => new RendererState(renderer))
                .ToArray();
            var visibleRenderers = sourceInstance.GetComponentsInChildren<Renderer>(true)
                .Concat(sceneInstance.GetComponentsInChildren<Renderer>(true))
                .Where(renderer => renderer.enabled)
                .ToHashSet();
            var cameraObject = new GameObject(
                "PlayerMaterialComparison_Camera",
                typeof(Camera));
            var keyLightObject = new GameObject(
                "PlayerMaterialComparison_KeyLight",
                typeof(Light));
            var fillLightObject = new GameObject(
                "PlayerMaterialComparison_FillLight",
                typeof(Light));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            keyLightObject.hideFlags = HideFlags.HideAndDontSave;
            fillLightObject.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                foreach (var renderer in allRenderers)
                {
                    renderer.enabled = visibleRenderers.Contains(renderer);
                }

                ConfigureLight(
                    keyLightObject.GetComponent<Light>(),
                    Quaternion.Euler(35f, -25f, 0f),
                    1.7f);
                ConfigureLight(
                    fillLightObject.GetComponent<Light>(),
                    Quaternion.Euler(20f, 150f, 0f),
                    0.85f);

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.085f, 1f);
                camera.orthographic = true;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                var comparison = RenderPanel(
                    camera,
                    CaptureWidth,
                    CaptureHeight,
                    BoundsOf(visibleRenderers),
                    new Vector3(0f, 0.12f, -1f));
                try
                {
                    File.WriteAllBytes(destination, comparison.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(comparison);
                }
            }
            finally
            {
                foreach (var state in rendererStates)
                {
                    state.Restore();
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
                UnityEngine.Object.DestroyImmediate(sourceInstance);
            }
        }

        private static Texture2D RenderPanel(
            Camera camera,
            int width,
            int height,
            Bounds bounds,
            Vector3 viewDirection)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 2
            };
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            try
            {
                var direction = viewDirection.normalized;
                var distance = Mathf.Max(10f, bounds.extents.magnitude * 3f);
                camera.aspect = width / (float)height;
                camera.transform.position = bounds.center + direction * distance;
                camera.transform.LookAt(bounds.center, Vector3.up);
                var horizontalExtent = ProjectedHalfExtent(
                    bounds.extents,
                    camera.transform.right);
                var verticalExtent = ProjectedHalfExtent(
                    bounds.extents,
                    camera.transform.up);
                camera.orthographicSize = Mathf.Max(
                    verticalExtent * 1.08f,
                    horizontalExtent / camera.aspect * 1.08f,
                    1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = distance + bounds.extents.magnitude * 4f + 10f;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                panel.Apply();
                return panel;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Bounds DetailBounds(
            Transform layoutRoot,
            IReadOnlyList<TableSpec> tables)
        {
            var selected = new List<Renderer>();
            var offset = 0;
            for (var tableIndex = 0; tableIndex < Mathf.Min(3, tables.Count); tableIndex++)
            {
                var count = Mathf.Min(4, tables[tableIndex].Keys.Count);
                for (var keyIndex = 0; keyIndex < count; keyIndex++)
                {
                    selected.AddRange(
                        layoutRoot.GetChild(offset + keyIndex)
                            .GetComponentsInChildren<Renderer>(true)
                            .Where(renderer => renderer.enabled));
                }

                offset += tables[tableIndex].Keys.Count;
            }

            return BoundsOf(selected);
        }

        private static Bounds BoundsOf(IEnumerable<Renderer> source)
        {
            var renderers = source.Where(renderer => renderer != null && renderer.enabled).ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("No renderers are available for framing.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void RequireIdleCaptureBounds(Bounds bounds)
        {
            if (!IsFinite(bounds.center) || !IsFinite(bounds.extents) ||
                bounds.size.x < 5f || bounds.size.x > 15f ||
                bounds.size.y < 1f || bounds.size.y > 4f ||
                bounds.size.z > 5f)
            {
                throw new InvalidOperationException(
                    "Player_Idle capture bounds are unusable. Center=" +
                    Vec(bounds.center) + ", Size=" + Vec(bounds.size) + ".");
            }
        }

        private static void RequireVisibleIdleCapture(
            Texture2D image,
            Color background)
        {
            var pixels = image.GetPixels32();
            var background32 = (Color32)background;
            var visible = 0;
            var sampled = 0;
            const int stride = 16;
            for (var index = 0; index < pixels.Length; index += stride)
            {
                sampled++;
                var pixel = pixels[index];
                var difference = Mathf.Abs(pixel.r - background32.r) +
                                 Mathf.Abs(pixel.g - background32.g) +
                                 Mathf.Abs(pixel.b - background32.b);
                if (difference >= 18)
                {
                    visible++;
                }
            }

            var visibleRatio = visible / (float)sampled;
            if (visibleRatio < 0.005f)
            {
                throw new InvalidOperationException(
                    "Player_Idle capture contains no visible model. Ratio=" +
                    Num(visibleRatio) + ".");
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private static float ProjectedHalfExtent(Vector3 extents, Vector3 axis)
        {
            return Mathf.Abs(axis.x) * extents.x +
                   Mathf.Abs(axis.y) * extents.y +
                   Mathf.Abs(axis.z) * extents.z;
        }

        private static void ConfigureLight(
            Light light,
            Quaternion rotation,
            float intensity)
        {
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            light.transform.rotation = rotation;
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var parts = assetPath.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new string(value
                .Select(character => invalid.Contains(character) ? '_' : character)
                .ToArray());
            return string.IsNullOrWhiteSpace(sanitized) ? "PlayerMaterial" : sanitized;
        }

        private static float RequireZSpacing(Transform longa, Transform tergo)
        {
            var spacing = Mathf.Abs(longa.position.z - tergo.position.z);
            if (spacing <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Longa Arma/Tergo Z spacing is unusable.");
            }

            return spacing;
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

        private static Animator RequireAnimator(Transform root)
        {
            var animators = root.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
            {
                throw new InvalidOperationException(
                    root.name + " contains multiple Animators.");
            }

            var animator = animators.Length == 1
                ? animators[0]
                : root.gameObject.AddComponent<Animator>();
            if (animator.avatar == null)
            {
                animator.avatar = FindPlayerAvatar();
            }

            return animator;
        }

        private static Avatar FindPlayerAvatar()
        {
            var avatars = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<Avatar>()
                .Where(avatar => avatar.isValid && avatar.isHuman)
                .ToArray();
            return avatars.Length == 1 ? avatars[0] : null;
        }

        private static Transform RequireRigBone(
            Animator animator,
            HumanBodyBones humanoidBone,
            string label,
            params string[] fallbackNames)
        {
            if (animator.avatar != null &&
                animator.avatar.isValid &&
                animator.avatar.isHuman)
            {
                var humanoidTransform = animator.GetBoneTransform(humanoidBone);
                if (humanoidTransform != null)
                {
                    return humanoidTransform;
                }
            }

            var transforms = animator.GetComponentsInChildren<Transform>(true);
            foreach (var fallbackName in fallbackNames)
            {
                var token = NormalizeBoneName(fallbackName);
                var matches = transforms
                    .Where(transform =>
                        NormalizeBoneName(transform.name).EndsWith(
                            token,
                            StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length == 1)
                {
                    return matches[0];
                }
            }

            throw new InvalidOperationException(
                "Player rig bone is missing or ambiguous: " + label + ".");
        }

        private static IdleKneeRig RequireIdleKneeRig(Animator animator)
        {
            var hips = RequireRigBone(
                animator,
                HumanBodyBones.Hips,
                "Hips",
                "Hips",
                "Pelvis");
            var leftUpper = RequireRigBone(
                animator,
                HumanBodyBones.LeftUpperLeg,
                "LeftUpperLeg",
                "LeftUpLeg",
                "LeftUpperLeg",
                "UpperLegL",
                "ThighL",
                "LThigh");
            var leftLower = RequireRigBone(
                animator,
                HumanBodyBones.LeftLowerLeg,
                "LeftLowerLeg",
                "LeftLeg",
                "LeftLowerLeg",
                "LowerLegL",
                "CalfL",
                "LCalf",
                "ShinL");
            var leftFoot = RequireRigBone(
                animator,
                HumanBodyBones.LeftFoot,
                "LeftFoot",
                "LeftFoot",
                "FootL",
                "LFoot",
                "LeftAnkle");
            var rightUpper = RequireRigBone(
                animator,
                HumanBodyBones.RightUpperLeg,
                "RightUpperLeg",
                "RightUpLeg",
                "RightUpperLeg",
                "UpperLegR",
                "ThighR",
                "RThigh");
            var rightLower = RequireRigBone(
                animator,
                HumanBodyBones.RightLowerLeg,
                "RightLowerLeg",
                "RightLeg",
                "RightLowerLeg",
                "LowerLegR",
                "CalfR",
                "RCalf",
                "ShinR");
            var rightFoot = RequireRigBone(
                animator,
                HumanBodyBones.RightFoot,
                "RightFoot",
                "RightFoot",
                "FootR",
                "RFoot",
                "RightAnkle");
            return new IdleKneeRig(
                hips,
                new IdleLegChain(leftUpper, leftLower, leftFoot, animator.transform.forward),
                new IdleLegChain(rightUpper, rightLower, rightFoot, animator.transform.forward));
        }

        private static string NormalizeBoneName(string value)
        {
            return new string(value
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray());
        }

        private static string[] OtherAnimationStates(
            Transform layoutRoot,
            Transform excluded)
        {
            return Enumerable.Range(0, layoutRoot.childCount)
                .Select(layoutRoot.GetChild)
                .Where(child => child != excluded)
                .Select(child =>
                {
                    var animators = child.GetComponentsInChildren<Animator>(true);
                    var legacy = child.GetComponentsInChildren<Animation>(true);
                    return child.name + "|" + string.Join(
                        ";",
                        animators.Select(animator =>
                            animator.enabled + "," +
                            AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) + "," +
                            animator.applyRootMotion)) + "|" +
                           string.Join(";", legacy.Select(animation => animation.enabled));
                })
                .ToArray();
        }

        private static void RequireEqual(
            string[] expected,
            string[] actual,
            string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void DeleteAssetIfPresent(string path, string message)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static float RequireIspantXSpacing(Transform ispant)
        {
            if (ispant.childCount < 2)
            {
                throw new InvalidOperationException(
                    "Ispant placement needs at least two slots for X spacing.");
            }

            var spacing = Mathf.Abs(
                ispant.GetChild(1).position.x - ispant.GetChild(0).position.x);
            if (spacing <= 0.1f)
            {
                throw new InvalidOperationException("Ispant X spacing is unusable.");
            }

            return spacing;
        }

        private static GameObject RequireRoot(string name)
        {
            var root = GameObject.Find(name) ??
                       throw new InvalidOperationException(name + " is missing.");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(name + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene. ActiveScene=" + scene.path + ".");
            }

            return scene;
        }

        private static string Absolute(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                              throw new InvalidOperationException(
                                  "Unity project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private sealed class TableSpec
        {
            public TableSpec(string title, List<string> keys)
            {
                Title = title;
                Keys = keys;
            }

            public string Title { get; }
            public List<string> Keys { get; }
        }

        private sealed class LayoutMetrics
        {
            public int TableCount { get; set; }
            public int KeyCount { get; set; }
            public Vector3 FirstPosition { get; set; }
            public float XSpacing { get; set; }
            public float ZSpacing { get; set; }
        }

        private sealed class MaterialImportMetrics
        {
            public int EmbeddedMaterialCount { get; set; }
        }

        private sealed class PlayerMaterialMetrics
        {
            public int MaterialCount { get; set; }
            public int TextureCount { get; set; }
            public int MaterialSlotCount { get; set; }
            public int InstanceCount { get; set; }
        }

        private readonly struct IdleMetrics
        {
            public IdleMetrics(
                float chestVerticalTravel,
                float leftKneeFlexion,
                float rightKneeFlexion,
                float maximumFootError,
                float maximumFootRotationError,
                float loopBoundaryError)
            {
                ChestVerticalTravel = chestVerticalTravel;
                LeftKneeFlexion = leftKneeFlexion;
                RightKneeFlexion = rightKneeFlexion;
                MaximumFootError = maximumFootError;
                MaximumFootRotationError = maximumFootRotationError;
                LoopBoundaryError = loopBoundaryError;
            }

            public float ChestVerticalTravel { get; }
            public float LeftKneeFlexion { get; }
            public float RightKneeFlexion { get; }
            public float MaximumFootError { get; }
            public float MaximumFootRotationError { get; }
            public float LoopBoundaryError { get; }
        }

        private sealed class IdleKneeRig
        {
            public IdleKneeRig(
                Transform hips,
                IdleLegChain left,
                IdleLegChain right)
            {
                Hips = hips;
                Left = left;
                Right = right;
                HipsBaseline = new LocalBoneState(hips);
                AnimatedLegBones = new[]
                {
                    left.Upper,
                    left.Lower,
                    left.Foot,
                    right.Upper,
                    right.Lower,
                    right.Foot
                };

                if (!left.Upper.IsChildOf(hips) || !right.Upper.IsChildOf(hips))
                {
                    throw new InvalidOperationException(
                        "Both upper legs must be descendants of Hips.");
                }
            }

            public Transform Hips { get; }
            public LocalBoneState HipsBaseline { get; }
            public IdleLegChain Left { get; }
            public IdleLegChain Right { get; }
            public Transform[] AnimatedLegBones { get; }

            public void Restore()
            {
                HipsBaseline.Restore(Hips);
                Left.Restore();
                Right.Restore();
            }
        }

        private sealed class IdleLegChain
        {
            private readonly LocalBoneState upperBaseline;
            private readonly LocalBoneState lowerBaseline;
            private readonly LocalBoneState footBaseline;

            public IdleLegChain(
                Transform upper,
                Transform lower,
                Transform foot,
                Vector3 fallbackBendDirection)
            {
                if (!lower.IsChildOf(upper) || !foot.IsChildOf(lower))
                {
                    throw new InvalidOperationException(
                        "The player leg bones do not form an upper/lower/foot hierarchy.");
                }

                Upper = upper;
                Lower = lower;
                Foot = foot;
                upperBaseline = new LocalBoneState(upper);
                lowerBaseline = new LocalBoneState(lower);
                footBaseline = new LocalBoneState(foot);
                UpperBaselineWorldPosition = upper.position;
                FootTargetPosition = foot.position;
                FootTargetRotation = foot.rotation;
                UpperLength = Vector3.Distance(upper.position, lower.position);
                LowerLength = Vector3.Distance(lower.position, foot.position);
                BaselineFlexion = KneeFlexionDegrees();

                var axis = (FootTargetPosition - UpperBaselineWorldPosition).normalized;
                var bend = Vector3.ProjectOnPlane(
                    lower.position - upper.position,
                    axis);
                if (bend.sqrMagnitude <= 0.000001f)
                {
                    bend = Vector3.ProjectOnPlane(fallbackBendDirection, axis);
                }

                if (bend.sqrMagnitude <= 0.000001f)
                {
                    throw new InvalidOperationException(
                        "The player leg has no usable forward knee bend direction.");
                }

                BendDirection = bend.normalized;
            }

            public Transform Upper { get; }
            public Transform Lower { get; }
            public Transform Foot { get; }
            public Vector3 UpperBaselineWorldPosition { get; }
            public Vector3 FootTargetPosition { get; }
            public Quaternion FootTargetRotation { get; }
            public Vector3 BendDirection { get; }
            public float UpperLength { get; }
            public float LowerLength { get; }
            public float BaselineFlexion { get; }
            public Vector3 UpperBaselineLocalPosition => upperBaseline.LocalPosition;

            public void Restore()
            {
                upperBaseline.Restore(Upper);
                lowerBaseline.Restore(Lower);
                footBaseline.Restore(Foot);
            }

            public float KneeFlexionDegrees()
            {
                return 180f - Vector3.Angle(
                    Upper.position - Lower.position,
                    Foot.position - Lower.position);
            }

            public float FootPositionError()
            {
                return Vector3.Distance(FootTargetPosition, Foot.position);
            }

            public float FootRotationErrorDegrees()
            {
                return Quaternion.Angle(FootTargetRotation, Foot.rotation);
            }
        }

        private readonly struct LocalBoneState
        {
            public LocalBoneState(Transform transform)
            {
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
            }

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }

            public void Restore(Transform transform)
            {
                transform.localPosition = LocalPosition;
                transform.localRotation = LocalRotation;
            }
        }

        private readonly struct PlayerStartViewMetrics
        {
            public PlayerStartViewMetrics(
                float frontDistance,
                float facingDot,
                Vector3 upperBodyViewport)
            {
                FrontDistance = frontDistance;
                FacingDot = facingDot;
                UpperBodyViewport = upperBodyViewport;
            }

            public float FrontDistance { get; }
            public float FacingDot { get; }
            public Vector3 UpperBodyViewport { get; }
        }

        private readonly struct Pose
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;

            public Pose(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
            }

            public float Error(Transform transform)
            {
                return Mathf.Max(
                    Vector3.Distance(position, transform.position),
                    Quaternion.Angle(rotation, transform.rotation) * Mathf.Deg2Rad);
            }

            public Vector3 Position => position;

            public Quaternion Rotation => rotation;
        }

        private readonly struct TransformState
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformState(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
                scale = transform.localScale;
            }

            public bool Matches(Transform transform)
            {
                return Vector3.Distance(position, transform.position) <= PositionTolerance &&
                       Quaternion.Angle(rotation, transform.rotation) <= 0.01f &&
                       Vector3.Distance(scale, transform.localScale) <= PositionTolerance;
            }
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer rendererValue)
            {
                renderer = rendererValue;
                enabled = rendererValue.enabled;
            }

            public void Restore()
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }
    }
}
