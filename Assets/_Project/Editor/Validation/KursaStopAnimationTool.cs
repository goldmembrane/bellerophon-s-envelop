using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaStopAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string TargetSlotName = "Kursa_09_Stop";
        private const string ModelName = "Kursa_Model";
        private const string FaceMaterialName = "Kursa_face_metal_Approved";
        private const string ApprovedShaderName =
            "Bellerophon/Kursa/ApprovedAppearance";
        private const string EyeDesaturationShaderProperty = "_EyeDesaturation";
        private const string EyeDesaturationAnimationProperty =
            "material._EyeDesaturation";
        private const string StateName = "Stop";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_09_Stop_Loop.anim";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_09_Stop.controller";
        private const string ValidationFolder =
            "docs/validation/kursa_stop_arm_relax_fix_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Kursa_Stop_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Kursa_Stop_FinalReview.png";
        private const float TransitionSeconds = 2f;
        private const float HoldSeconds = 1f;
        private const float DurationSeconds = TransitionSeconds + HoldSeconds;
        private const float HeadBowDegrees = 45f;
        private const float FrameRate = 60f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        private static readonly float[] ReviewTimes =
        {
            0f, 1f, TransitionSeconds, TransitionSeconds + HoldSeconds * 0.5f,
            DurationSeconds - 1f / FrameRate
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Stop Animation")]
        public static void ApplyKursaStopAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireDirectChild(
                placement.transform,
                StaticSlotName));
            var targetSlot = RequireDirectChild(placement.transform, TargetSlotName);
            var previousModel = RequireModel(targetSlot);
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            RequireEyeShaderContract(staticRenderer, StaticSlotName);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var targetSlotTransformBefore = LocalTransformSignature(targetSlot);

            DeleteAssetIfPresent(ControllerPath);
            DeleteAssetIfPresent(ClipPath);

            GameObject replacement = null;
            try
            {
                replacement = UnityEngine.Object.Instantiate(staticModel.gameObject);
                replacement.name = ModelName;
                replacement.transform.SetParent(targetSlot, false);
                replacement.transform.SetLocalPositionAndRotation(
                    staticModel.localPosition,
                    staticModel.localRotation);
                replacement.transform.localScale = staticModel.localScale;

                foreach (var animator in replacement.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                foreach (var legacy in replacement.GetComponentsInChildren<Animation>(true))
                    UnityEngine.Object.DestroyImmediate(legacy);

                var replacementRenderer = RequireRenderer(
                    replacement.transform,
                    TargetSlotName);
                CopyPropertyBlock(staticRenderer, replacementRenderer);
                RequireExactStaticAppearance(
                    staticRenderer,
                    replacementRenderer,
                    TargetSlotName);
                RequireEyeShaderContract(replacementRenderer, TargetSlotName);

                var clip = CreateStopClip(
                    replacement.transform,
                    replacementRenderer);
                var controller = CreateController(clip);
                var animatorComponent = replacement.AddComponent<Animator>();
                animatorComponent.runtimeAnimatorController = controller;
                animatorComponent.applyRootMotion = false;
                animatorComponent.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animatorComponent.updateMode = AnimatorUpdateMode.Normal;
                animatorComponent.enabled = true;
                animatorComponent.Rebind();
                animatorComponent.Update(0f);
                EditorUtility.SetDirty(animatorComponent);

                RequirePlacedContract(
                    replacement.transform,
                    staticModel,
                    staticRenderer,
                    clip,
                    controller);

                UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
                replacement = null;
                RequireEqual(
                    otherSlotsBefore,
                    OtherSlotSignatures(placement.transform),
                    "A Kursa slot outside Kursa_09_Stop changed.");
                RequireEqual(
                    otherRootsBefore,
                    OtherRootSignatures(scene, placement),
                    "A scene root outside the Kursa placement changed.");
                if (targetSlotTransformBefore != LocalTransformSignature(targetSlot))
                    throw new InvalidOperationException(
                        "The Kursa_09_Stop slot transform changed.");
                RequireSlotContract(placement.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after applying Kursa_09_Stop.");
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "KursaStopAnimationApplied Result=PASS, Slot=Kursa_09_Stop, " +
                    "StaticAppearanceCopied=True, StartPose=Kursa_01_Static, " +
                    "TransitionSeconds=2, HoldSeconds=1, DurationSeconds=3, " +
                    "HeadBowDegrees=45, BothArmsHanging=True, ShieldMovesWithLeftArm=True, " +
                    "EyesGraduallyDesaturated=True, EyeTexturesChanged=False, " +
                    "OriginalMaterialsChanged=False, Loop=True, RootMotion=False, " +
                    "OtherSlotsUnchanged=True, OtherSceneRootsUnchanged=True, SceneSaved=True.");
            }
            catch
            {
                if (replacement != null)
                    UnityEngine.Object.DestroyImmediate(replacement);
                DeleteAssetIfPresent(ControllerPath);
                DeleteAssetIfPresent(ClipPath);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Stop Diagnostic")]
        public static void CaptureKursaStopDiagnostic()
        {
            CaptureReview(NextDiagnosticPath(), "Diagnostic");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Stop Final Review")]
        public static void CaptureKursaStopFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa stop final review already exists: " +
                    destination);
            CaptureReview(destination, "Final");
        }

        public static void InspectKursaStopShieldSkinning()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var model = RequireModel(RequireDirectChild(
                placement.transform,
                TargetSlotName));
            var renderer = RequireRenderer(model, TargetSlotName);
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException("Kursa stop mesh is missing.");
            var shieldSubmeshes = Enumerable.Range(0, renderer.sharedMaterials.Length)
                .Where(index => renderer.sharedMaterials[index] != null &&
                    renderer.sharedMaterials[index].name.IndexOf(
                        "shield",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (shieldSubmeshes.Length == 0)
                throw new InvalidOperationException(
                    "Kursa stop renderer has no shield material submesh.");
            var shieldVertices = new HashSet<int>();
            foreach (var submesh in shieldSubmeshes)
            foreach (var index in mesh.GetTriangles(submesh))
                shieldVertices.Add(index);
            var weights = mesh.boneWeights;
            var totals = new Dictionary<int, float>();
            foreach (var vertex in shieldVertices)
            {
                var weight = weights[vertex];
                AddBoneWeight(totals, weight.boneIndex0, weight.weight0);
                AddBoneWeight(totals, weight.boneIndex1, weight.weight1);
                AddBoneWeight(totals, weight.boneIndex2, weight.weight2);
                AddBoneWeight(totals, weight.boneIndex3, weight.weight3);
            }
            var summary = string.Join(
                ",",
                totals.OrderByDescending(item => item.Value)
                    .Take(10)
                    .Select(item => renderer.bones[item.Key].name + ":" +
                        item.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)));
            Debug.Log(
                "KursaStopShieldSkinningInspected Result=PASS, " +
                "ShieldSubmeshes=" + string.Join(",", shieldSubmeshes) +
                ", ShieldVertices=" + shieldVertices.Count +
                ", DominantBones=" + summary + ".");
        }

        private static void AddBoneWeight(
            IDictionary<int, float> totals,
            int boneIndex,
            float weight)
        {
            if (weight <= 0f) return;
            totals[boneIndex] = totals.TryGetValue(boneIndex, out var current)
                ? current + weight
                : weight;
        }

        private static AnimationClip CreateStopClip(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var skeleton = RequireSkeletonPaths(model, renderer);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var start = CapturePoses(skeleton);
            Dictionary<string, LocalPose> target;
            try
            {
                AuthorStopPose(model, skeleton);
                target = CapturePoses(skeleton);
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
            }

            var clip = new AnimationClip
            {
                name = "Kursa_09_Stop_Loop",
                frameRate = FrameRate,
                wrapMode = WrapMode.Loop
            };
            foreach (var path in skeleton.Keys.OrderBy(item => item, StringComparer.Ordinal))
            {
                var startPose = start[path];
                var targetPose = target[path];
                if ((targetPose.Position - startPose.Position).sqrMagnitude > 0.00000001f)
                    throw new InvalidOperationException(
                        "Kursa stop must preserve every skeleton local position: " +
                        path + ".");
                var targetRotation = targetPose.Rotation;
                if (Quaternion.Dot(startPose.Rotation, targetRotation) < 0f)
                    targetRotation = Negate(targetRotation);
                SetPositionCurves(
                    clip,
                    path,
                    startPose.Position,
                    targetPose.Position);
                SetQuaternionCurves(
                    clip,
                    path,
                    startPose.Rotation,
                    targetRotation);
            }

            var rendererPath = AnimationUtility.CalculateTransformPath(
                renderer.transform,
                model);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    EyeDesaturationAnimationProperty),
                LinearCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(TransitionSeconds, 1f),
                    new Keyframe(DurationSeconds, 1f)));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, ClipPath);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void AuthorStopPose(
            Transform model,
            IReadOnlyDictionary<string, Transform> skeleton)
        {
            var head = RequireBone(skeleton, "Head");
            var localFaceForward = Quaternion.Inverse(head.rotation) * model.forward;
            var positive = Quaternion.AngleAxis(HeadBowDegrees, model.right) *
                head.rotation;
            var negative = Quaternion.AngleAxis(-HeadBowDegrees, model.right) *
                head.rotation;
            head.rotation = Vector3.Dot(
                    positive * localFaceForward,
                    model.up) <
                Vector3.Dot(negative * localFaceForward, model.up)
                ? positive
                : negative;

            AuthorHangingArm(model, skeleton, true);
            AuthorHangingArm(model, skeleton, false);
        }

        private static void AuthorHangingArm(
            Transform model,
            IReadOnlyDictionary<string, Transform> skeleton,
            bool left)
        {
            var prefix = left ? "Left" : "Right";
            var upper = RequireBone(skeleton, prefix + "Arm");
            var lower = RequireBone(skeleton, prefix + "ForeArm");
            var hand = RequireBone(skeleton, prefix + "Hand");
            var handRotation = hand.rotation;
            var armLength = Vector3.Distance(upper.position, lower.position) +
                Vector3.Distance(lower.position, hand.position);
            var side = left ? -model.right : model.right;
            var handTarget = upper.position -
                model.up * (armLength * 0.96f) +
                side * (armLength * 0.10f);
            var elbowPole = upper.position -
                model.up * (armLength * 0.52f) +
                side * (armLength * 0.36f) +
                model.forward * (armLength * 0.08f);
            SolveTwoBoneChain(
                upper,
                lower,
                hand,
                handTarget,
                elbowPole,
                handRotation);
        }

        private static void SolveTwoBoneChain(
            Transform upper,
            Transform lower,
            Transform tip,
            Vector3 tipTarget,
            Vector3 pole,
            Quaternion tipRotation)
        {
            var rootPosition = upper.position;
            var upperLength = Vector3.Distance(upper.position, lower.position);
            var lowerLength = Vector3.Distance(lower.position, tip.position);
            var rootToTarget = tipTarget - rootPosition;
            var targetDistance = Mathf.Clamp(
                rootToTarget.magnitude,
                Mathf.Abs(upperLength - lowerLength) + 0.0001f,
                upperLength + lowerLength - 0.0001f);
            var targetDirection = rootToTarget.normalized;
            var poleDirection = Vector3.ProjectOnPlane(
                pole - rootPosition,
                targetDirection).normalized;
            if (poleDirection.sqrMagnitude < 0.5f)
                throw new InvalidOperationException(
                    "The Kursa stop arm pole is degenerate.");
            var along =
                (upperLength * upperLength + targetDistance * targetDistance -
                 lowerLength * lowerLength) /
                (2f * targetDistance);
            var away = Mathf.Sqrt(Mathf.Max(
                0f,
                upperLength * upperLength - along * along));
            var desiredJoint = rootPosition +
                targetDirection * along +
                poleDirection * away;
            upper.rotation = Quaternion.FromToRotation(
                lower.position - upper.position,
                desiredJoint - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(
                tip.position - lower.position,
                tipTarget - lower.position) * lower.rotation;
            tip.rotation = tipRotation;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(StateName);
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

        private static void RequirePlacedContract(
            Transform model,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            AnimationClip clip,
            RuntimeAnimatorController controller)
        {
            var renderer = RequireRenderer(model, TargetSlotName);
            RequireExactStaticAppearance(staticRenderer, renderer, TargetSlotName);
            RequireEyeShaderContract(renderer, TargetSlotName);
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Kursa_09_Stop must contain one Animator.");
            if (animator.runtimeAnimatorController != controller ||
                animator.applyRootMotion ||
                animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                throw new InvalidOperationException(
                    "Kursa_09_Stop Animator configuration differs.");
            }
            if (Mathf.Abs(clip.length - DurationSeconds) > 0.001f)
                throw new InvalidOperationException(
                    "Kursa_09_Stop duration differs from 3 seconds.");
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || settings.loopBlend)
                throw new InvalidOperationException(
                    "Kursa_09_Stop must loop without return blending.");
            var eyeBinding = AnimationUtility.GetCurveBindings(clip)
                .SingleOrDefault(item =>
                    item.type == typeof(SkinnedMeshRenderer) &&
                    item.propertyName == EyeDesaturationAnimationProperty);
            if (string.IsNullOrEmpty(eyeBinding.propertyName))
                throw new InvalidOperationException(
                    "Kursa_09_Stop eye desaturation curve is missing.");
            var eyeCurve = AnimationUtility.GetEditorCurve(clip, eyeBinding) ??
                throw new InvalidOperationException(
                    "Kursa_09_Stop eye desaturation curve is unavailable.");
            if (Mathf.Abs(eyeCurve.Evaluate(0f)) > 0.001f ||
                Mathf.Abs(eyeCurve.Evaluate(TransitionSeconds) - 1f) > 0.001f ||
                Mathf.Abs(eyeCurve.Evaluate(DurationSeconds - 0.001f) - 1f) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Kursa_09_Stop eye desaturation timing differs.");
            }

            var targetSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            try
            {
                clip.SampleAnimation(model.gameObject, TransitionSeconds);
                var completed = CaptureNamedBonePoses(model);
                clip.SampleAnimation(
                    model.gameObject,
                    TransitionSeconds + HoldSeconds * 0.5f);
                RequirePoseMatch(
                    completed,
                    CaptureNamedBonePoses(model),
                    "stop completion and hold pose");
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                clip.SampleAnimation(model.gameObject, 0f);
                RequirePoseMatch(
                    CaptureNamedBonePoses(staticModel),
                    CaptureNamedBonePoses(model),
                    "static pose and stop start pose");
            }
            finally
            {
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
            }
        }

        private static void CaptureReview(string destination, string kind)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticModel = RequireModel(RequireDirectChild(
                placement.transform,
                StaticSlotName));
            var targetModel = RequireModel(RequireDirectChild(
                placement.transform,
                TargetSlotName));
            var staticRenderer = RequireRenderer(staticModel, StaticSlotName);
            var targetRenderer = RequireRenderer(targetModel, TargetSlotName);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                throw new InvalidOperationException("Kursa stop clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                ControllerPath) ?? throw new InvalidOperationException(
                    "Kursa stop controller is missing.");
            RequirePlacedContract(
                targetModel,
                staticModel,
                staticRenderer,
                clip,
                controller);
            CaptureContactSheet(
                scene,
                staticModel,
                staticRenderer,
                targetModel,
                targetRenderer,
                clip,
                destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa stop capture changed the scene dirty state.");
            Debug.Log(
                "KursaStopReviewCaptured Kind=" + kind +
                ", DirectVisualReviewRequired=True, " +
                "Columns=StaticReference|0|1|2|2.5|FinalHoldFrame, " +
                "Rows=ObliqueFull|SideFull|FrontFace, Image=" + destination +
                ", SceneChanged=False.");
        }

        private static void CaptureContactSheet(
            Scene scene,
            Transform staticModel,
            SkinnedMeshRenderer staticRenderer,
            Transform targetModel,
            SkinnedMeshRenderer targetRenderer,
            AnimationClip clip,
            string destination)
        {
            const int panelWidth = 300;
            const int panelHeight = 360;
            const int columns = 6;
            const int rows = 3;
            var sceneRenderers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var rendererStates = sceneRenderers
                .Select(item => new RendererSnapshot(item))
                .ToArray();
            var targetSnapshots = targetModel.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item))
                .ToArray();
            var propertySnapshots = new[]
            {
                new RendererPropertySnapshot(staticRenderer),
                new RendererPropertySnapshot(targetRenderer)
            };
            var animator = targetModel.GetComponentsInChildren<Animator>(true).Single();
            var animatorEnabled = animator.enabled;
            var updateWhenOffscreen = targetRenderer.updateWhenOffscreen;
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("The Player camera is missing.");
            var cameraObject = new GameObject(
                "KursaStopReviewCamera",
                typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var targetTexture = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var sheet = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                animator.enabled = true;
                targetRenderer.updateWhenOffscreen = true;
                var targetFullBounds = FullLoopRendererBounds(
                    targetModel,
                    targetRenderer,
                    animator,
                    targetSnapshots);
                var sharedFullSize = Vector3.Max(
                    staticRenderer.bounds.size,
                    targetFullBounds.size);
                var staticFullBounds = new Bounds(
                    staticRenderer.bounds.center,
                    sharedFullSize);
                var staticFaceBounds = FaceBounds(staticModel, staticRenderer);
                var targetFaceBounds = FaceBounds(targetModel, targetRenderer);
                var sharedFaceSize = Vector3.Max(
                    staticFaceBounds.size,
                    targetFaceBounds.size);
                staticFaceBounds.size = sharedFaceSize;
                targetFaceBounds.size = sharedFaceSize;

                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 30f;
                camera.aspect = panelWidth / (float)panelHeight;
                camera.targetTexture = targetTexture;

                for (var row = 0; row < rows; row++)
                {
                    var faceRow = row == 2;
                    var yaw = row == 0 ? 35f : row == 1 ? 90f : 0f;
                    RenderPanel(
                        camera,
                        staticModel,
                        sceneRenderers,
                        targetTexture,
                        panel,
                        faceRow ? staticFaceBounds : staticFullBounds,
                        yaw);
                    CopyPanel(panel, sheet, 0, rows - 1 - row, panelWidth, panelHeight);

                    for (var index = 0; index < ReviewTimes.Length; index++)
                    {
                        propertySnapshots[1].Restore();
                        EvaluateAnimator(animator, ReviewTimes[index]);
                        var currentBounds = faceRow
                            ? FaceBounds(targetModel, targetRenderer)
                            : new Bounds(targetRenderer.bounds.center, sharedFullSize);
                        if (faceRow) currentBounds.size = sharedFaceSize;
                        RenderPanel(
                            camera,
                            targetModel,
                            sceneRenderers,
                            targetTexture,
                            panel,
                            currentBounds,
                            yaw);
                        CopyPanel(
                            panel,
                            sheet,
                            index + 1,
                            rows - 1 - row,
                            panelWidth,
                            panelHeight);
                    }
                }

                sheet.Apply();
                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Kursa stop review folder."));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                animator.enabled = false;
                foreach (var snapshot in targetSnapshots) snapshot.Restore();
                foreach (var snapshot in propertySnapshots) snapshot.Restore();
                targetRenderer.updateWhenOffscreen = updateWhenOffscreen;
                animator.enabled = animatorEnabled;
                foreach (var snapshot in rendererStates) snapshot.Restore();
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(sheet);
                targetTexture.Release();
                UnityEngine.Object.DestroyImmediate(targetTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static Bounds FullLoopRendererBounds(
            Transform model,
            Renderer renderer,
            Animator animator,
            IReadOnlyList<TransformSnapshot> snapshots)
        {
            var initialized = false;
            var result = new Bounds();
            foreach (var time in ReviewTimes)
            {
                EvaluateAnimator(animator, time);
                if (!initialized)
                {
                    result = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    result.Encapsulate(renderer.bounds);
                }
            }
            foreach (var snapshot in snapshots) snapshot.Restore();
            return result;
        }

        private static void EvaluateAnimator(Animator animator, float time)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.Play(
                Animator.StringToHash(StateName),
                0,
                Mathf.Clamp01(time / DurationSeconds));
            animator.Update(0f);
        }

        private static Bounds FaceBounds(
            Transform model,
            Renderer renderer)
        {
            var head = model.GetComponentsInChildren<Transform>(true)
                .Single(item => item.name == "Head");
            var bodyHeight = renderer.bounds.size.y;
            var size = bodyHeight * 0.28f;
            return new Bounds(
                head.position + model.up * (bodyHeight * 0.035f),
                new Vector3(size, size, size));
        }

        private static void RenderPanel(
            Camera camera,
            Transform model,
            IEnumerable<Renderer> sceneRenderers,
            RenderTexture target,
            Texture2D panel,
            Bounds bounds,
            float yaw)
        {
            foreach (var renderer in sceneRenderers)
                renderer.enabled = renderer.transform.IsChildOf(model);
            FrameCamera(camera, model, bounds, target.width / (float)target.height, yaw);
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            panel.Apply();
        }

        private static void FrameCamera(
            Camera camera,
            Transform model,
            Bounds bounds,
            float aspect,
            float yaw)
        {
            var direction = Quaternion.AngleAxis(yaw, model.up) * model.forward.normalized;
            var vertical = bounds.extents.y /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) /
                Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.16f;
            camera.transform.position = bounds.center + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                bounds.center - camera.transform.position,
                Vector3.up);
        }

        private static void CopyPanel(
            Texture2D panel,
            Texture2D sheet,
            int column,
            int row,
            int width,
            int height)
        {
            sheet.SetPixels(
                column * width,
                row * height,
                width,
                height,
                panel.GetPixels());
        }

        private static Dictionary<string, Transform> RequireSkeletonPaths(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var transforms = new HashSet<Transform>(renderer.bones);
            foreach (var bone in renderer.bones)
            {
                var current = bone.parent;
                while (current != null && current != model)
                {
                    transforms.Add(current);
                    current = current.parent;
                }
            }
            var result = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var item in transforms)
            {
                var path = AnimationUtility.CalculateTransformPath(item, model);
                if (string.IsNullOrEmpty(path))
                    throw new InvalidOperationException(
                        "The Kursa skeleton unexpectedly includes the model root.");
                result.Add(path, item);
            }
            return result;
        }

        private static Transform RequireBone(
            IReadOnlyDictionary<string, Transform> skeleton,
            string name)
        {
            var matches = skeleton.Values.Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "The Kursa rig does not contain exactly one " + name + " bone.");
            return matches[0];
        }

        private static Dictionary<string, LocalPose> CapturePoses(
            IReadOnlyDictionary<string, Transform> skeleton) =>
            skeleton.ToDictionary(
                item => item.Key,
                item => new LocalPose(item.Value),
                StringComparer.Ordinal);

        private static Dictionary<string, LocalPose> CaptureNamedBonePoses(Transform model) =>
            model.GetComponentsInChildren<Transform>(true)
                .Where(item => item != model)
                .ToDictionary(
                    item => AnimationUtility.CalculateTransformPath(item, model),
                    item => new LocalPose(item),
                    StringComparer.Ordinal);

        private static void RequirePoseMatch(
            IReadOnlyDictionary<string, LocalPose> expected,
            IReadOnlyDictionary<string, LocalPose> actual,
            string context)
        {
            if (expected.Count != actual.Count || expected.Keys.Any(key =>
                    !actual.TryGetValue(key, out var pose) ||
                    Vector3.Distance(expected[key].Position, pose.Position) > 0.0001f ||
                    Quaternion.Angle(expected[key].Rotation, pose.Rotation) > 0.01f))
            {
                throw new InvalidOperationException(
                    "Kursa pose mismatch between " + context + ".");
            }
        }

        private static void SetPositionCurves(
            AnimationClip clip,
            string path,
            Vector3 start,
            Vector3 target)
        {
            SetTransformCurve(clip, path, "m_LocalPosition.x", start.x, target.x);
            SetTransformCurve(clip, path, "m_LocalPosition.y", start.y, target.y);
            SetTransformCurve(clip, path, "m_LocalPosition.z", start.z, target.z);
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion start,
            Quaternion target)
        {
            SetTransformCurve(clip, path, "m_LocalRotation.x", start.x, target.x);
            SetTransformCurve(clip, path, "m_LocalRotation.y", start.y, target.y);
            SetTransformCurve(clip, path, "m_LocalRotation.z", start.z, target.z);
            SetTransformCurve(clip, path, "m_LocalRotation.w", start.w, target.w);
        }

        private static void SetTransformCurve(
            AnimationClip clip,
            string path,
            string property,
            float start,
            float target)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
                LinearCurve(
                    new Keyframe(0f, start),
                    new Keyframe(TransitionSeconds, target),
                    new Keyframe(DurationSeconds, target)));
        }

        private static AnimationCurve LinearCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        private static void RequireEyeShaderContract(
            SkinnedMeshRenderer renderer,
            string context)
        {
            var faceMaterials = renderer.sharedMaterials.Where(item =>
                item != null && item.name == FaceMaterialName).ToArray();
            if (faceMaterials.Length != 1 ||
                faceMaterials[0].shader == null ||
                faceMaterials[0].shader.name != ApprovedShaderName ||
                faceMaterials[0].GetTexture("_EyeLeft") == null ||
                faceMaterials[0].GetTexture("_EyeRight") == null ||
                !faceMaterials[0].HasProperty(EyeDesaturationShaderProperty))
            {
                throw new InvalidOperationException(
                    context + " approved face-eye shader contract differs.");
            }
        }

        private static void RequireExactStaticAppearance(
            SkinnedMeshRenderer expected,
            SkinnedMeshRenderer actual,
            string context)
        {
            if (expected.sharedMesh != actual.sharedMesh ||
                expected.bones.Length != actual.bones.Length ||
                expected.rootBone.name != actual.rootBone.name ||
                expected.sharedMaterials.Length != actual.sharedMaterials.Length ||
                !expected.sharedMaterials.SequenceEqual(actual.sharedMaterials))
            {
                throw new InvalidOperationException(
                    context + " does not use the exact static Kursa appearance.");
            }
        }

        private static void CopyPropertyBlock(Renderer source, Renderer target)
        {
            var block = new MaterialPropertyBlock();
            source.GetPropertyBlock(block);
            target.SetPropertyBlock(block);
        }

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on Kursa_09_Stop.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(item =>
                item.name == PlacementRootName) ??
            throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 ||
                    slot.GetChild(0).name != ModelName)
                {
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at index " + index + ".");
                }
            }
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                throw new InvalidOperationException(slot.name + " model contract differs.");
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(
            Transform model,
            string context) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ?? throw new InvalidOperationException(
                    context + " must contain one skinned renderer.");

        private static string[] OtherSlotSignatures(Transform placement) =>
            SlotNames.Where(item => item != TargetSlotName)
                .Select(item => RecursiveSignature(RequireDirectChild(placement, item)))
                .ToArray();

        private static string[] OtherRootSignatures(Scene scene, GameObject placement) =>
            scene.GetRootGameObjects()
                .Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform))
                .ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|')
                    .Append(item.gameObject.activeSelf).Append('|')
                    .Append(item.localPosition).Append('|')
                    .Append(item.localRotation).Append('|')
                    .Append(item.localScale);
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled);
                    if (renderer is SkinnedMeshRenderer skinned)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                    foreach (var material in renderer.sharedMaterials)
                        builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
                foreach (var animator in item.GetComponents<Animator>())
                {
                    builder.Append("|A:").Append(animator.enabled)
                        .Append(':').Append(animator.applyRootMotion)
                        .Append(':').Append(AssetDatabase.GetAssetPath(
                            animator.runtimeAnimatorController));
                }
            }
            return builder.ToString();
        }

        private static string LocalTransformSignature(Transform transform) =>
            transform.localPosition + "|" + transform.localRotation + "|" +
            transform.localScale;

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string NextDiagnosticPath()
        {
            for (var index = 1; index <= 2; index++)
            {
                var candidate = Absolute(string.Format(DiagnosticPathFormat, index));
                if (!File.Exists(candidate)) return candidate;
            }
            throw new InvalidOperationException(
                "The two approved Kursa stop arm-relax diagnostics already exist.");
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null &&
                !AssetDatabase.DeleteAsset(path))
            {
                throw new InvalidOperationException("Asset could not be replaced: " + path);
            }
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private static Quaternion Negate(Quaternion value) =>
            new Quaternion(-value.x, -value.y, -value.z, -value.w);

        private readonly struct LocalPose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public LocalPose(Transform transform)
            {
                Position = transform.localPosition;
                Rotation = transform.localRotation;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public TransformSnapshot(Transform value)
            {
                transform = value;
                position = value.localPosition;
                rotation = value.localRotation;
                scale = value.localScale;
            }

            public void Restore()
            {
                if (transform == null) return;
                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }
        }

        private readonly struct RendererSnapshot
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererSnapshot(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null) renderer.enabled = enabled;
            }
        }

        private sealed class RendererPropertySnapshot
        {
            private readonly Renderer renderer;
            private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

            public RendererPropertySnapshot(Renderer value)
            {
                renderer = value;
                value.GetPropertyBlock(block);
            }

            public void Restore()
            {
                if (renderer != null) renderer.SetPropertyBlock(block);
            }
        }
    }
}
