using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantStopAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ispant Enemy Placement";
        private const string StaticSlotName = "Ispant_01_Static";
        private const string StopSlotName = "Ispant_10_Stop";
        private const string StaticModelName = "Ispant_Model";
        private const string StopModelName = "Ispant_Stop_Model";
        private const string BodyRendererName = "Ispant_Armed_Body";
        private const string CrescentRendererName = "Ispant_Crescent_Ornament";
        private const string EyeRendererName = "Ispant_Reference_Eye_Slits";
        private const string EyeDesaturationProperty = "material._EyeDesaturation";
        private const string StateName = "Ispant_Stop";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_10_Stop.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_10_Stop.controller";
        private const string ValidationFolder =
            "docs/validation/ispant_stop_animation_2026-08-11";
        private const string DiagnosticPath =
            ValidationFolder + "/Ispant_10_CrescentAttachment_Diagnostic.png";
        private const string FinalPath =
            ValidationFolder + "/Ispant_10_CrescentAttachment_Final.png";
        private const int ExpectedSlots = 12;
        private const float TransitionSeconds = 2f;
        private const float HoldSeconds = 1f;
        private const float DurationSeconds = TransitionSeconds + HoldSeconds;
        private const float HeadBowDegrees = 45f;
        private const float FrameRate = 60f;
        private static readonly float[] ReviewTimes =
        {
            0f, 0.25f, 0.5f, 0.75f, 1f, 1.25f,
            1.5f, 1.75f, 2f, 2.5f, DurationSeconds - 1f / FrameRate
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Slot 10 Stop Animation")]
        public static void ApplyIspant10StopAnimation()
        {
            var scene = RequireScene(requireClean: true);
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var stopSlot = RequireSlot(placement.transform, StopSlotName, 9);
            if (stopSlot.childCount != 1)
                throw new InvalidOperationException("Ispant_10_Stop must contain exactly one model.");

            var previous = stopSlot.GetChild(0);
            var stopSlotSnapshot = new TransformSnapshot(stopSlot);
            var otherSlotsBefore = OtherSlotSignatures(placement.transform, stopSlot);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var replacement = UnityEngine.Object.Instantiate(staticModel.gameObject);
            replacement.name = StopModelName;
            replacement.transform.SetParent(stopSlot, false);
            replacement.transform.SetLocalPositionAndRotation(
                previous.localPosition, previous.localRotation);
            replacement.transform.localScale = previous.localScale;

            try
            {
                foreach (var animator in replacement.GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
                foreach (var legacy in replacement.GetComponentsInChildren<Animation>(true))
                    UnityEngine.Object.DestroyImmediate(legacy);

                var body = RequireRenderer<SkinnedMeshRenderer>(
                    replacement.transform, BodyRendererName);
                var crescent = RequireRenderer<SkinnedMeshRenderer>(
                    replacement.transform, CrescentRendererName);
                var eyes = RequireRenderer<Renderer>(
                    replacement.transform, EyeRendererName);
                RequireEyeShader(eyes);
                var clip = CreateOrUpdateStopClip(replacement.transform, body, eyes);
                MakeCrescentRigid(replacement.transform, crescent);
                var controller = CreateOrUpdateController(clip);
                var animatorComponent = replacement.AddComponent<Animator>();
                animatorComponent.runtimeAnimatorController = controller;
                animatorComponent.applyRootMotion = false;
                animatorComponent.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animatorComponent.updateMode = AnimatorUpdateMode.Normal;
                animatorComponent.enabled = true;
                animatorComponent.Rebind();
                animatorComponent.Update(0f);
                EditorUtility.SetDirty(animatorComponent);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            if (stopSlot.childCount != 1 || stopSlot.GetChild(0) != replacement.transform)
                throw new InvalidOperationException("Ispant_10_Stop replacement contract differs.");
            if (!stopSlotSnapshot.Matches(stopSlot))
                throw new InvalidOperationException("Ispant_10_Stop slot transform changed.");
            RequireEqual(
                otherSlotsBefore,
                OtherSlotSignatures(placement.transform, stopSlot),
                "An Ispant slot outside slot 10 changed.");
            RequireEqual(
                otherRootsBefore,
                OtherRootSignatures(scene, placement),
                "A scene root outside the Ispant placement changed.");

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(stopSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("CargoRunMvp could not be saved for Ispant_10_Stop.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = stopSlot.gameObject;
            Debug.Log(
                "Ispant10StopAnimationApplied" +
                ", Target=" + PlacementRootName + "/" + StopSlotName +
                ", RotationCurvesOnly=True, MeshAssetsChanged=False" +
                ", CrescentSharedMeshPreserved=True, CrescentRigidHeadMount=True" +
                ", CrescentStaticVisualPositionRestored=True" +
                ", EyesGraduallyDesaturated=True, Loop=True" +
                ", OtherSlotsChanged=False, OtherSceneRootsChanged=False, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 10 Stop Diagnostic")]
        public static void CaptureIspant10StopDiagnostic()
        {
            var destination = Absolute(DiagnosticPath);
            if (File.Exists(destination))
                File.Delete(destination);
            CaptureReview(destination);
            Debug.Log("Ispant10StopDiagnosticCaptured, Image=" + DiagnosticPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Slot 10 Stop Final")]
        public static void CaptureIspant10StopFinal()
        {
            var destination = Absolute(FinalPath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time slot-10 final image already exists.");
            CaptureReview(destination);
            Debug.Log("Ispant10StopFinalCaptured, Image=" + FinalPath + ".");
        }

        private static AnimationClip CreateOrUpdateStopClip(
            Transform model,
            SkinnedMeshRenderer body,
            Renderer eyes)
        {
            var requiredBoneNames = new[]
            {
                "Head", "LeftArm", "LeftForeArm", "LeftHand",
                "RightArm", "RightForeArm", "RightHand"
            };
            var descendants = model.GetComponentsInChildren<Transform>(true);
            var bones = requiredBoneNames.ToDictionary(
                name => name,
                name => descendants.SingleOrDefault(item => item.name == name) ??
                        throw new InvalidOperationException(
                            "The Ispant hierarchy is missing control bone " + name + "."),
                StringComparer.Ordinal);
            var animatedBones = new[]
            {
                RequireBone(bones, "Head"),
                RequireBone(bones, "LeftArm"),
                RequireBone(bones, "LeftForeArm"),
                RequireBone(bones, "LeftHand"),
                RequireBone(bones, "RightArm"),
                RequireBone(bones, "RightForeArm"),
                RequireBone(bones, "RightHand")
            };
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var start = animatedBones.ToDictionary(item => item, item => item.localRotation);
            Dictionary<Transform, Quaternion> target;
            try
            {
                AuthorStopPose(model, bones);
                target = animatedBones.ToDictionary(item => item, item => item.localRotation);
            }
            finally
            {
                foreach (var snapshot in snapshots)
                    snapshot.Restore();
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Ispant_10_Stop" };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);

            clip.name = "Ispant_10_Stop";
            clip.frameRate = FrameRate;
            clip.wrapMode = WrapMode.Loop;
            foreach (var bone in animatedBones)
            {
                var startRotation = start[bone];
                var targetRotation = target[bone];
                if (Quaternion.Dot(startRotation, targetRotation) < 0f)
                    targetRotation = Negate(targetRotation);
                SetQuaternionCurves(
                    clip,
                    AnimationUtility.CalculateTransformPath(bone, model),
                    startRotation,
                    targetRotation);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    AnimationUtility.CalculateTransformPath(eyes.transform, model),
                    eyes.GetType(),
                    EyeDesaturationProperty),
                LinearCurve(0f, 0f, 1f));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalOrientation = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void AuthorStopPose(
            Transform model,
            IReadOnlyDictionary<string, Transform> bones)
        {
            var head = RequireBone(bones, "Head");
            var localFaceForward = Quaternion.Inverse(head.rotation) * model.forward;
            var positive = Quaternion.AngleAxis(HeadBowDegrees, model.right) * head.rotation;
            var negative = Quaternion.AngleAxis(-HeadBowDegrees, model.right) * head.rotation;
            head.rotation = Vector3.Dot(positive * localFaceForward, model.up) <
                            Vector3.Dot(negative * localFaceForward, model.up)
                ? positive
                : negative;
            AuthorHangingArm(model, bones, true);
            AuthorHangingArm(model, bones, false);
        }

        private static void MakeCrescentRigid(
            Transform model,
            SkinnedMeshRenderer source)
        {
            var head = model.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == "Head") ??
                       throw new InvalidOperationException(
                           "The Ispant hierarchy is missing the Head bone for the crescent mount.");
            var sharedMesh = source.sharedMesh ??
                             throw new InvalidOperationException(
                                 "The Ispant crescent shared mesh is missing.");
            var sharedMaterials = source.sharedMaterials;
            var shadowCastingMode = source.shadowCastingMode;
            var receiveShadows = source.receiveShadows;
            var lightProbeUsage = source.lightProbeUsage;
            var reflectionProbeUsage = source.reflectionProbeUsage;
            var renderingLayerMask = source.renderingLayerMask;
            var motionVectorGenerationMode = source.motionVectorGenerationMode;
            var allowOcclusionWhenDynamic = source.allowOcclusionWhenDynamic;
            var crescentObject = source.gameObject;
            var staticVisualCenter = source.bounds.center;

            UnityEngine.Object.DestroyImmediate(source);
            crescentObject.AddComponent<MeshFilter>().sharedMesh = sharedMesh;
            var target = crescentObject.AddComponent<MeshRenderer>();
            target.sharedMaterials = sharedMaterials;
            target.shadowCastingMode = shadowCastingMode;
            target.receiveShadows = receiveShadows;
            target.lightProbeUsage = lightProbeUsage;
            target.reflectionProbeUsage = reflectionProbeUsage;
            target.renderingLayerMask = renderingLayerMask;
            target.motionVectorGenerationMode = motionVectorGenerationMode;
            target.allowOcclusionWhenDynamic = allowOcclusionWhenDynamic;
            crescentObject.transform.position += staticVisualCenter - target.bounds.center;
            crescentObject.transform.SetParent(head, true);
            crescentObject.transform.position += staticVisualCenter - target.bounds.center;
            EditorUtility.SetDirty(crescentObject);
            EditorUtility.SetDirty(target);
        }

        private static void AuthorHangingArm(
            Transform model,
            IReadOnlyDictionary<string, Transform> bones,
            bool left)
        {
            var prefix = left ? "Left" : "Right";
            var upper = RequireBone(bones, prefix + "Arm");
            var lower = RequireBone(bones, prefix + "ForeArm");
            var hand = RequireBone(bones, prefix + "Hand");
            var handRotation = hand.rotation;
            var armLength = Vector3.Distance(upper.position, lower.position) +
                            Vector3.Distance(lower.position, hand.position);
            var side = left ? -model.right : model.right;
            var handTarget = upper.position - model.up * (armLength * 0.96f) +
                             side * (armLength * 0.10f);
            var elbowPole = upper.position - model.up * (armLength * 0.52f) +
                            side * (armLength * 0.36f) +
                            model.forward * (armLength * 0.08f);
            SolveTwoBoneChain(upper, lower, hand, handTarget, elbowPole, handRotation);
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
            var poleDirection = Vector3.ProjectOnPlane(pole - rootPosition, targetDirection).normalized;
            if (poleDirection.sqrMagnitude < 0.5f)
                throw new InvalidOperationException("The Ispant stop arm pole is degenerate.");
            var along = (upperLength * upperLength + targetDistance * targetDistance -
                         lowerLength * lowerLength) / (2f * targetDistance);
            var away = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            var desiredJoint = rootPosition + targetDirection * along + poleDirection * away;
            upper.rotation = Quaternion.FromToRotation(
                lower.position - upper.position,
                desiredJoint - upper.position) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(
                tip.position - lower.position,
                tipTarget - lower.position) * lower.rotation;
            tip.rotation = tipRotation;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion start,
            Quaternion target)
        {
            var properties = new[]
            {
                "m_LocalRotation.x", "m_LocalRotation.y",
                "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (var component = 0; component < properties.Length; component++)
            {
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[component]),
                    LinearCurve(start[component], target[component], target[component]));
            }
        }

        private static AnimationCurve LinearCurve(float start, float transition, float hold)
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, start),
                new Keyframe(TransitionSeconds, transition),
                new Keyframe(DurationSeconds, hold))
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var childState in stateMachine.states.ToArray())
                stateMachine.RemoveState(childState.state);
            var state = stateMachine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void CaptureReview(string destination)
        {
            var scene = RequireScene(requireClean: true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var staticModel = RequireDirectChild(
                RequireSlot(placement.transform, StaticSlotName, 0), StaticModelName);
            var model = RequireDirectChild(
                RequireSlot(placement.transform, StopSlotName, 9), StopModelName);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The Ispant stop clip is missing.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid capture folder."));

            var modelSnapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var staticRenderers = staticModel.GetComponentsInChildren<Renderer>(true);
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            var rendererSnapshots = staticRenderers.Concat(modelRenderers)
                .Distinct().Select(item => new RendererSnapshot(item)).ToArray();
            var layerSnapshots = staticRenderers.Concat(modelRenderers)
                .Select(item => item.gameObject).Distinct()
                .Select(item => new LayerSnapshot(item)).ToArray();
            var cameraObject = new GameObject("Ispant10StopReviewCamera", typeof(Camera))
                { hideFlags = HideFlags.HideAndDontSave };
            var keyObject = new GameObject("Ispant10StopReviewKey", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            var fillObject = new GameObject("Ispant10StopReviewFill", typeof(Light))
                { hideFlags = HideFlags.HideAndDontSave };
            const int renderLayer = 30;
            const int panelWidth = 480;
            const int bodyHeight = 600;
            const int faceHeight = 260;
            var panelCount = ReviewTimes.Length + 1;
            var strip = new Texture2D(
                panelWidth * panelCount, bodyHeight + faceHeight, TextureFormat.RGB24, false);
            var bodyTarget = new RenderTexture(
                panelWidth, bodyHeight, 24, RenderTextureFormat.ARGB32);
            var faceTarget = new RenderTexture(
                panelWidth, faceHeight, 24, RenderTextureFormat.ARGB32);
            var bodyPanel = new Texture2D(panelWidth, bodyHeight, TextureFormat.RGB24, false);
            var facePanel = new Texture2D(panelWidth, faceHeight, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var layer in layerSnapshots)
                    layer.GameObject.layer = renderLayer;
                foreach (var renderer in staticRenderers.Concat(modelRenderers))
                    renderer.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = 1 << renderLayer;
                camera.fieldOfView = 34f;
                var key = keyObject.GetComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 1.2f;
                key.color = new Color(1f, 0.95f, 0.88f);
                key.cullingMask = 1 << renderLayer;
                keyObject.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
                var fill = fillObject.GetComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 0.7f;
                fill.color = new Color(0.65f, 0.78f, 1f);
                fill.cullingMask = 1 << renderLayer;
                fillObject.transform.rotation = Quaternion.Euler(20f, 145f, 0f);

                var staticBody = RequireRenderer<SkinnedMeshRenderer>(staticModel, BodyRendererName);
                var staticCrescent = RequireRenderer<Renderer>(staticModel, CrescentRendererName);
                _ = RequireRenderer<SkinnedMeshRenderer>(model, BodyRendererName);
                _ = RequireRenderer<MeshRenderer>(model, CrescentRendererName);
                var referenceHeight = staticBody.bounds.size.y;
                var targetReferenceCenter = model.parent.TransformPoint(
                    staticModel.parent.InverseTransformPoint(staticBody.bounds.center));
                var targetReferenceCrescentCenter = model.parent.TransformPoint(
                    staticModel.parent.InverseTransformPoint(staticCrescent.bounds.center));
                foreach (var renderer in staticRenderers)
                    renderer.enabled = true;
                RenderReviewPanel(
                    camera, strip, bodyTarget, bodyPanel, faceTarget, facePanel,
                    0, staticBody.bounds.center, staticCrescent.bounds.center, referenceHeight,
                    panelWidth, bodyHeight, faceHeight);
                foreach (var renderer in staticRenderers)
                    renderer.enabled = false;
                foreach (var renderer in modelRenderers)
                    renderer.enabled = true;
                for (var index = 0; index < ReviewTimes.Length; index++)
                {
                    SampleClip(model.gameObject, clip, ReviewTimes[index]);
                    RenderReviewPanel(
                        camera, strip, bodyTarget, bodyPanel, faceTarget, facePanel,
                        index + 1, targetReferenceCenter, targetReferenceCrescentCenter,
                        referenceHeight,
                        panelWidth, bodyHeight, faceHeight);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var snapshot in rendererSnapshots) snapshot.Restore();
                foreach (var snapshot in layerSnapshots) snapshot.Restore();
                foreach (var snapshot in modelSnapshots) snapshot.Restore();
                StopSampling();
                UnityEngine.Object.DestroyImmediate(bodyPanel);
                UnityEngine.Object.DestroyImmediate(facePanel);
                UnityEngine.Object.DestroyImmediate(strip);
                bodyTarget.Release();
                faceTarget.Release();
                UnityEngine.Object.DestroyImmediate(bodyTarget);
                UnityEngine.Object.DestroyImmediate(faceTarget);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Ispant stop capture changed the scene dirty state.");
        }

        private static void RenderReviewPanel(
            Camera camera,
            Texture2D strip,
            RenderTexture bodyTarget,
            Texture2D bodyPanel,
            RenderTexture faceTarget,
            Texture2D facePanel,
            int panelIndex,
            Vector3 bodyCenter,
            Vector3 faceCenter,
            float referenceHeight,
            int width,
            int bodyHeight,
            int faceHeight)
        {
            FrameCamera(camera, bodyCenter, referenceHeight, width / (float)bodyHeight);
            RenderToTexture(camera, bodyTarget, bodyPanel, width, bodyHeight);
            strip.SetPixels32(
                panelIndex * width, faceHeight, width, bodyHeight, bodyPanel.GetPixels32());
            FrameCamera(camera, faceCenter, referenceHeight * 0.22f, width / (float)faceHeight);
            RenderToTexture(camera, faceTarget, facePanel, width, faceHeight);
            strip.SetPixels32(
                panelIndex * width, 0, width, faceHeight, facePanel.GetPixels32());
        }

        private static void RenderToTexture(
            Camera camera,
            RenderTexture target,
            Texture2D panel,
            int width,
            int height)
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            panel.Apply();
        }

        private static void FrameCamera(
            Camera camera,
            Vector3 center,
            float height,
            float aspect)
        {
            camera.aspect = aspect;
            var distance = (height * 0.5f) /
                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            camera.transform.position = center + Vector3.back * distance * 1.25f;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position, Vector3.up);
        }

        private static void SampleClip(GameObject model, AnimationClip clip, float time)
        {
            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, clip, time);
            AnimationMode.EndSampling();
        }

        private static void StopSampling()
        {
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        private static void RequireEyeShader(Renderer eyes)
        {
            if (eyes.sharedMaterials.Length != 1 || eyes.sharedMaterial == null ||
                eyes.sharedMaterial.shader == null ||
                eyes.sharedMaterial.shader.name != "Bellerophon/Ispant/ApprovedAppearance" ||
                !eyes.sharedMaterial.HasProperty("_EyeDesaturation"))
                throw new InvalidOperationException("The approved Ispant eye shader contract differs.");
        }

        private static Transform RequireBone(
            IReadOnlyDictionary<string, Transform> bones,
            string name)
        {
            if (!bones.TryGetValue(name, out var bone))
                throw new InvalidOperationException("The Ispant rig is missing bone " + name + ".");
            return bone;
        }

        private static Quaternion Negate(Quaternion value) =>
            new Quaternion(-value.x, -value.y, -value.z, -value.w);

        private static Scene RequireScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active for slot-10 work.");
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene)
        {
            var roots = scene.GetRootGameObjects()
                .Where(item => item.name == PlacementRootName).ToArray();
            if (roots.Length != 1 || roots[0].transform.childCount != ExpectedSlots)
                throw new InvalidOperationException("The approved Ispant placement contract differs.");
            return roots[0];
        }

        private static Transform RequireSlot(Transform placement, string name, int index)
        {
            if (index < 0 || index >= placement.childCount || placement.GetChild(index).name != name)
                throw new InvalidOperationException("The required Ispant slot differs: " + name + ".");
            return placement.GetChild(index);
        }

        private static Transform RequireDirectChild(Transform parent, string name) =>
            parent.Cast<Transform>().SingleOrDefault(item => item.name == name) ??
            throw new InvalidOperationException(
                "Required direct child is missing: " + parent.name + "/" + name + ".");

        private static T RequireRenderer<T>(Transform root, string name) where T : Renderer =>
            root.GetComponentsInChildren<T>(true).SingleOrDefault(item => item.name == name) ??
            throw new InvalidOperationException("Required Ispant renderer is missing: " + name + ".");

        private static string[] OtherSlotSignatures(Transform placement, Transform target) =>
            placement.Cast<Transform>().Where(item => item != target)
                .Select(HierarchySignature).ToArray();

        private static string[] OtherRootSignatures(Scene scene, GameObject placement) =>
            scene.GetRootGameObjects().Where(item => item != placement)
                .Select(item => HierarchySignature(item.transform)).ToArray();

        private static string HierarchySignature(Transform root) =>
            string.Join(";", root.GetComponentsInChildren<Transform>(true).Select(item =>
                AnimationUtility.CalculateTransformPath(item, root) + "|" +
                item.gameObject.activeSelf + "|" + item.childCount));

        private static void RequireEqual(string[] expected, string[] actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string path) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));

        private sealed class TransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public TransformSnapshot(Transform transform)
            {
                this.transform = transform;
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                localScale = transform.localScale;
            }

            public bool Matches(Transform other) =>
                other == transform && other.localPosition == localPosition &&
                other.localRotation == localRotation && other.localScale == localScale;

            public void Restore()
            {
                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
                transform.localScale = localScale;
            }
        }

        private sealed class RendererSnapshot
        {
            private readonly Renderer renderer;
            private readonly bool enabled;
            public RendererSnapshot(Renderer renderer)
            {
                this.renderer = renderer;
                enabled = renderer.enabled;
            }
            public void Restore() => renderer.enabled = enabled;
        }

        private sealed class LayerSnapshot
        {
            public readonly GameObject GameObject;
            private readonly int layer;
            public LayerSnapshot(GameObject gameObject)
            {
                GameObject = gameObject;
                layer = gameObject.layer;
            }
            public void Restore() => GameObject.layer = layer;
        }
    }
}
