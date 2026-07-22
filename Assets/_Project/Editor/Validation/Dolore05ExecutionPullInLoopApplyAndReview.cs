using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore05ExecutionPullInLoop
{
    internal static class Dolore05ExecutionPullInLoopApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string SlotName = "Dolore_05_Execution_Pull_In";
        private const string ModelName = "Dolore_Model";
        private const string AttachmentName = "Dolore_Attack_Attachment";
        private const string FrameMaterialName = "Dolore_Oxidized_Brass_Frame";
        private const int ExpectedTentacleBoneCount = 13;
        private const string RingBoneName = "Bone_010";
        private const string FirstMovingBoneName = "Bone_009";
        private const string TipBoneName = "Bone_001";
        // Bone_008 stays behind the torso; the following rig joints form one continuous penetration line.
        private const string PenetrationEntryBoneName = "Bone_008";
        private static readonly string[] PenetrationIntermediateBoneNames =
            { "Bone_007", "Bone_006", "Bone_005" };
        private const string StandingName = "Dolore_05_Execution_Target_Transfer";
        private const string LyingName = "Dolore_05_Execution_Target_Transfer_Lying";
        private const string PortalOccluderName = "Dolore_05_Execution_Portal_Occluder";
        private const float TargetFrameFitRatio = 0.80f;
        private const float PierceDuration = 0.58f;
        private const float PullDuration = 2f;
        private const float PullVisibleUntil = 1.94f;
        private const float PullStartXRotation = 60f;
        // The target keeps its outer pull rotation; only the internal upper-body rig rises by this amount.
        private const float UpperBodyRiseDegrees = 40f;
        // Four small alternating struggle cycles run across the approved two-second pull.
        private const float StruggleCycleSeconds = 0.5f;
        private const float ArmSwingDegrees = 5f;
        private const float LegSwingDegrees = 8f;
        private static readonly string[] UpperBodyBoneNames = { "Spine02", "Spine01", "Spine" };
        private static readonly float[] UpperBodyBoneRiseDegrees = { 16f, 14f, 10f };
        // The skinned terminal chain provides about 0.117 local units after alignment; reserve 0.005 at both faces.
        private const float LyingTorsoMaximumThickness = 0.10f;
        // Keep the pointed end visibly beyond the local +Y chest surface after passing through the torso.
        private const float LyingTorsoExitMargin = 0.005f;
        private const float VisibilityMargin = 0.001f;
        private const float PortalBehindMargin = 0.01f;
        private const float Tolerance = 0.0001f;
        private const int CaptureLayer = 30;
        private const int PulledTargetRenderQueue = 2000;

        private const string SourceStanding = "enemies model/transfer.fbx";
        private const string SourceLying = "enemies model/transfer lying.fbx";
        private const string AssetFolder = "Assets/_Project/Art/Generated/Enemies/Dolore/ExecutionTarget";
        private const string StandingAsset = AssetFolder + "/transfer.fbx";
        private const string LyingAsset = AssetFolder + "/transfer lying.fbx";
        private const string MaterialFolder = AssetFolder + "/Materials";
        private const string PortalClipShaderPath =
            MaterialFolder + "/DoloreExecutionPortalClip.shader";
        private const string PortalClipPlaneProperty = "_PortalClipPlane";
        private const string PulledTargetMaterialPrefix = MaterialFolder + "/Dolore_05_Pulled_Target_";
        private const string AnimationFolder =
            "Assets/_Project/Art/Generated/Enemies/Dolore/AttackAttachment/Animations";
        private const string SourceIntro = AnimationFolder + "/Dolore_05_ExecutionPullIn_Intro.anim";
        private const string SourcePierce = AnimationFolder + "/Dolore_05_ExecutionPullIn_PierceHold.anim";
        private const string SlotIntro = AnimationFolder + "/Dolore_05_ExecutionPullIn_IntroSlot.anim";
        private const string SlotPierce = AnimationFolder + "/Dolore_05_ExecutionPullIn_PierceHoldSlot.anim";
        private const string SlotPull = AnimationFolder + "/Dolore_05_ExecutionPullIn_PullInSlot.anim";
        private const string ControllerPath = AnimationFolder + "/Dolore_05_ExecutionPullIn.controller";
        private const string ReviewFolder = AssetFolder + "/Review";
        private const string InspectionPath = ReviewFolder + "/Dolore_05_ExecutionPullIn_Loop_Inspection.txt";
        private const string CaptureFolder = ReviewFolder + "/Dolore_05_ExecutionPullIn_Loop_Diagnostic";

        private static readonly string ProjectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                                                     throw new InvalidOperationException("Project root is unavailable.");

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Motion 4 Execution Pull-In Loop")]
        public static void ApplyLoop()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp contains unsaved changes.");

            EnsureFolder(AssetFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(AnimationFolder);
            EnsureFolder(ReviewFolder);
            CopyExact(SourceStanding, StandingAsset);
            CopyExact(SourceLying, LyingAsset);

            var slot = RequireSlot(scene);
            var model = RequireChild(slot, ModelName);
            var attachment = RequireChild(model, AttachmentName);
            var sourceIntro = RequireAsset<AnimationClip>(SourceIntro);
            var sourcePierce = RequireAsset<AnimationClip>(SourcePierce);
            var pierce = SamplePierce(slot, attachment, sourcePierce);
            var frameBounds = FrameBounds(slot, model, attachment);

            var standing = RequireChild(slot, StandingName);
            var standingPrefab = RequireAsset<GameObject>(StandingAsset);
            if (AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(standing.gameObject)) !=
                StandingAsset)
                throw new InvalidOperationException("The standing target is not the approved transfer.fbx instance.");
            var standingUnitBounds = BoundsAtUnitScale(standing, slot);

            var oldLying = slot.Find(LyingName);
            if (oldLying != null) UnityEngine.Object.DestroyImmediate(oldLying.gameObject);
            var lyingPrefab = RequireAsset<GameObject>(LyingAsset);
            var lyingObject = PrefabUtility.InstantiatePrefab(lyingPrefab, slot) as GameObject ??
                              throw new InvalidOperationException("transfer lying.fbx could not be instantiated.");
            lyingObject.name = LyingName;
            var lying = lyingObject.transform;
            lying.localPosition = Vector3.zero;
            lying.localRotation = Quaternion.identity;
            lying.localScale = Vector3.one;
            var lyingUnitBounds = BoundsAtUnitScale(lying, slot);

            // One uniform scale keeps both approved FBX poses within 80% of the frame width and height.
            var uniformScale = Mathf.Min(
                frameBounds.size.x * TargetFrameFitRatio /
                Mathf.Max(standingUnitBounds.size.x, lyingUnitBounds.size.x),
                frameBounds.size.y * TargetFrameFitRatio /
                Mathf.Max(standingUnitBounds.size.y, lyingUnitBounds.size.y));
            if (!(uniformScale > 0f) || float.IsNaN(uniformScale))
                throw new InvalidOperationException("The approved uniform transporter scale could not be calculated.");
            var pointedBounds = BoundsFromPoints(pierce.PointedVertices);
            var pointedIndex = Enumerable.Range(0, pierce.PointedVertices.Length)
                .OrderBy(index => (pierce.PointedVertices[index] - pointedBounds.center).sqrMagnitude)
                .First();
            var pointedAnchor = pierce.PointedVertices[pointedIndex];
            standing.localPosition = Vector3.zero;
            standing.localRotation = Quaternion.identity;
            standing.localScale = Vector3.one * uniformScale;
            AlignProjectedBodySurface(standing, slot, pointedAnchor, 0.60f);
            var standingPlacementZ = MaximumVisiblePlacement(standing, slot, pierce);
            standing.localPosition = new Vector3(
                standing.localPosition.x,
                standing.localPosition.y,
                standingPlacementZ);
            DisableChildAnimators(standing);

            lying.localPosition = Vector3.zero;
            lying.localRotation = Quaternion.Euler(PullStartXRotation, 0f, 0f);
            lying.localScale = Vector3.one * uniformScale;
            var penetrationDirection = lying.TransformDirection(Vector3.up);
            var terminalLengthInSlot = TerminalPenetrationLengthInSlot(
                slot,
                attachment,
                sourcePierce,
                pointedIndex,
                penetrationDirection);
            // Place the torso against the rear plane of the tapered pointed section, not its center vertex.
            // This keeps the whole sharp point outside while the unchanged rig line pierces below it.
            var pointedBaseAnchor = PointedBaseAnchorInSlot(
                slot,
                attachment,
                sourcePierce,
                pointedIndex,
                penetrationDirection,
                pointedAnchor);
            var lyingPoseSnapshot = PoseSnapshot.Capture(lying);
            Vector3 lyingImpactPosition;
            try
            {
                ApplyTransporterStrugglePose(lying, 0f);
                var chestLengthFraction = ChestLengthFraction(lying);
                var torsoPenetration = TorsoPenetrationInLying(
                    lying,
                    slot,
                    terminalLengthInSlot,
                    chestLengthFraction);
                var torsoExitInSlot = slot.InverseTransformPoint(
                    lying.TransformPoint(torsoPenetration.Front + Vector3.up * LyingTorsoExitMargin));
                lying.localPosition += pointedBaseAnchor - torsoExitInSlot;
                lyingImpactPosition = lying.localPosition;
            }
            finally
            {
                lyingPoseSnapshot.Restore();
            }
            lying.localPosition = lyingImpactPosition;
            DisableChildAnimators(lying);

            // The impact placement is authored in the approved 60-degree pull-start pose.
            // Keep the scene/rest pose upright; the PullIn clip owns the visible rotation.
            lying.localRotation = Quaternion.identity;
            var lyingStartBounds = BoundsIn(lying, slot);
            var ringAnchor = pierce.RingAnchor;
            var lyingEndPosition = lying.localPosition + ringAnchor - lyingStartBounds.center;
            lyingEndPosition.z = lying.localPosition.z +
                                 (ringAnchor.z - PortalBehindMargin - lyingStartBounds.max.z);
            AssignPulledTargetMaterials(lying, slot, ringAnchor);
            RemovePortalOccluder(slot);

            var intro = RebaseClip(sourceIntro, SlotIntro, "Dolore_05_ExecutionPullIn_IntroSlot", slot, attachment);
            var pierceClip = RebaseClip(sourcePierce, SlotPierce, "Dolore_05_ExecutionPullIn_PierceHoldSlot", slot, attachment);
            var pull = CreatePullClip(
                sourceIntro,
                sourcePierce,
                slot,
                attachment,
                standing,
                lying,
                lyingEndPosition,
                ringAnchor.z);
            AddTargetCurves(intro, slot, standing, lying, standing.localPosition, lying.localPosition, true, false);
            AddTargetCurves(pierceClip, slot, standing, lying, standing.localPosition, lying.localPosition, true, false);
            SetClipLoop(intro, false);
            SetClipLoop(pierceClip, false);
            SetClipLoop(pull, false);

            var controller = CreateController(intro, pierceClip, pull);
            var attachmentAnimator = attachment.GetComponent<Animator>();
            if (attachmentAnimator != null) UnityEngine.Object.DestroyImmediate(attachmentAnimator);
            var animator = slot.GetComponent<Animator>();
            if (animator == null) animator = slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            SetRenderers(standing, true);
            SetRenderers(lying, false);
            EditorUtility.SetDirty(standing);
            EditorUtility.SetDirty(lying);

            var metrics = Inspect(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            WriteInspection(metrics, "Apply", true);
            Debug.Log(
                "Dolore05ExecutionPullInLoopApplied Result=PASS UniformScale=" + Num(metrics.UniformScale) +
                " StandingFrameRatio=" + Num(Mathf.Max(metrics.StandingWidthRatio, metrics.StandingHeightRatio)) +
                " LyingFrameRatio=" + Num(Mathf.Max(metrics.LyingWidthRatio, metrics.LyingHeightRatio)) +
                " PullSeconds=" + Num(metrics.PullLength) +
                " PierceToPullTipError=" + Num(metrics.PierceToPullTipError) +
                " PointedTipFollowError=" + Num(metrics.PointedTipFollowMaximumError) +
                " StandingSourceHash=" + metrics.StandingSourceHash +
                " LyingSourceHash=" + metrics.LyingSourceHash + " SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 4 Execution Pull-In Loop")]
        public static void InspectLoop()
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            var metrics = Inspect(scene);
            WriteInspection(metrics, "Inspect", false);
            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Execution pull-in inspection changed CargoRunMvp.");
            Debug.Log(
                "Dolore05ExecutionPullInLoopInspected Result=PASS UniformScale=" + Num(metrics.UniformScale) +
                " StandingFrameRatio=" + Num(Mathf.Max(metrics.StandingWidthRatio, metrics.StandingHeightRatio)) +
                " LyingFrameRatio=" + Num(Mathf.Max(metrics.LyingWidthRatio, metrics.LyingHeightRatio)) +
                " PullSeconds=" + Num(metrics.PullLength) +
                " PierceToPullTipError=" + Num(metrics.PierceToPullTipError) +
                " PointedTipFollowError=" + Num(metrics.PointedTipFollowMaximumError) +
                " PullStartLyingPenetrated=" + metrics.PullStartLyingPenetrated +
                " PullEndHidden=" + metrics.PullEndHidden + " SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Motion 4 Execution Pull-In Loop Diagnostic")]
        public static void CaptureDiagnostic()
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            Inspect(scene);
            var slot = RequireSlot(scene);
            var placement = slot.parent;
            var active = new Dictionary<GameObject, bool>();
            for (var index = 0; index < placement.childCount; index++)
            {
                var child = placement.GetChild(index).gameObject;
                if (child == slot.gameObject) continue;
                active[child] = child.activeSelf;
                child.SetActive(false);
            }
            var layers = SetLayer(slot, CaptureLayer);
            var rendererStates = slot.GetComponentsInChildren<Renderer>(true)
                .Select(RendererState.Capture)
                .ToArray();
            var cameraObject = new GameObject("Dolore Execution Pull-In Diagnostic Camera")
            {
                hideFlags = HideFlags.DontSave
            };
            var lightObject = new GameObject("Dolore Execution Pull-In Diagnostic Light")
            {
                hideFlags = HideFlags.DontSave
            };
            try
            {
                var animator = slot.GetComponent<Animator>() ??
                               throw new InvalidOperationException("The slot Animator is missing.");
                animator.Rebind();
                animator.Update(0f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.035f, 0.045f, 1f);
                camera.fieldOfView = 32f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 2.6f;
                light.color = new Color(1f, 0.93f, 0.86f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                EnsureFolder(CaptureFolder);
                var folder = Absolute(CaptureFolder);
                Directory.CreateDirectory(folder);
                foreach (var file in Directory.GetFiles(folder, "*.png")) File.Delete(file);

                CaptureAnimatorState(animator, camera, slot, "Intro", 0.01f, folder, "01_IntroStanding.png");
                CaptureAnimatorState(animator, camera, slot, "Intro", 0.25f, folder, "02_RingGenerated.png");
                CaptureAnimatorState(animator, camera, slot, "PierceHold", 1f, folder, "03_PierceStanding.png");
                CaptureAnimatorState(animator, camera, slot, "PullIn", 0.01f, folder, "04_LyingSwapPenetrated.png");
                CaptureAnimatorState(animator, camera, slot, "PullIn", 0.50f, folder, "05_PullMid.png");
                CaptureAnimatorState(animator, camera, slot, "PullIn", 0.99f, folder, "06_PullEnd.png");
                animator.Rebind();
                animator.Play("Intro", 0, 0f);
                animator.Update(0f);
            }
            finally
            {
                foreach (var state in rendererStates) state.Apply();
                RestoreLayers(layers);
                foreach (var pair in active) pair.Key.SetActive(pair.Value);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
            AssetDatabase.Refresh();
            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Execution pull-in diagnostic changed CargoRunMvp.");
            Debug.Log(
                "Dolore05ExecutionPullInLoopCaptured Result=PASS ActualAnimatorStates=6 CaptureFolder=" +
                CaptureFolder + " SceneChanged=False.");
        }

        private static Metrics Inspect(Scene scene)
        {
            var slot = RequireSlot(scene);
            var model = RequireChild(slot, ModelName);
            var attachment = RequireChild(model, AttachmentName);
            var standing = RequireChild(slot, StandingName);
            var lying = RequireChild(slot, LyingName);
            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The execution Animator must be on the slot root.");
            if (attachment.GetComponent<Animator>() != null)
                throw new InvalidOperationException("The attachment Animator must not compete with the slot Animator.");
            if (AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != ControllerPath)
                throw new InvalidOperationException("The execution controller changed.");
            if (standing.GetComponentsInChildren<Collider>(true).Length != 0 ||
                lying.GetComponentsInChildren<Collider>(true).Length != 0)
                throw new InvalidOperationException("Execution preview targets must not contain Colliders.");
            if (slot.Find(PortalOccluderName) != null)
                throw new InvalidOperationException("The obsolete depth occluder must not remain in the execution slot.");

            var intro = RequireAsset<AnimationClip>(SlotIntro);
            var pierce = RequireAsset<AnimationClip>(SlotPierce);
            var pull = RequireAsset<AnimationClip>(SlotPull);
            if (Mathf.Abs(pull.length - PullDuration) > Tolerance)
                throw new InvalidOperationException("Pull-in must be exactly two seconds. Length=" + Num(pull.length));
            if (Mathf.Abs(pierce.length - PierceDuration) > Tolerance)
                throw new InvalidOperationException("PierceHold duration changed.");
            ValidateController(animator.runtimeAnimatorController as AnimatorController);

            var frameBounds = FrameBounds(slot, model, attachment);
            var standingBounds = BoundsIn(standing, slot);
            var lyingBounds = BoundsIn(lying, slot);
            var uniform = standing.localScale.x;
            if (Mathf.Abs(standing.localScale.y - uniform) > Tolerance ||
                Mathf.Abs(standing.localScale.z - uniform) > Tolerance ||
                Mathf.Abs(lying.localScale.x - uniform) > Tolerance ||
                Mathf.Abs(lying.localScale.y - uniform) > Tolerance ||
                Mathf.Abs(lying.localScale.z - uniform) > Tolerance)
                throw new InvalidOperationException("Standing and lying transporter scales must remain uniform.");
            var standingWidthRatio = standingBounds.size.x / frameBounds.size.x;
            var standingHeightRatio = standingBounds.size.y / frameBounds.size.y;
            var lyingWidthRatio = lyingBounds.size.x / frameBounds.size.x;
            var lyingHeightRatio = lyingBounds.size.y / frameBounds.size.y;
            var greatestFrameRatio = Mathf.Max(
                Mathf.Max(standingWidthRatio, standingHeightRatio),
                Mathf.Max(lyingWidthRatio, lyingHeightRatio));
            if (greatestFrameRatio > TargetFrameFitRatio + 0.002f ||
                Mathf.Abs(greatestFrameRatio - TargetFrameFitRatio) > 0.002f)
                throw new InvalidOperationException(
                    "Standing and lying transporters must fit within 80% of the frame width and height. " +
                    "GreatestRatio=" + Num(greatestFrameRatio));
            var lyingPath = AnimationUtility.CalculateTransformPath(lying, slot);
            var scaleVariation = PullScaleVariation(pull, lyingPath, uniform);
            if (scaleVariation > Tolerance)
                throw new InvalidOperationException(
                    "The lying transporter scale changes during pull-in. Variation=" + Num(scaleVariation));

            var approvedRing = SamplePierce(slot, attachment, RequireAsset<AnimationClip>(SourcePierce)).RingAnchor;
            var expectedClipPlane = PortalClipPlane(slot, approvedRing);
            var pulledMaterials = lying.GetComponentsInChildren<Renderer>(true)
                .SelectMany(item => item.sharedMaterials)
                .ToArray();
            if (pulledMaterials.Length == 0 || pulledMaterials.Any(item => item == null ||
                    !AssetDatabase.GetAssetPath(item).StartsWith(PulledTargetMaterialPrefix, StringComparison.Ordinal) ||
                    AssetDatabase.GetAssetPath(item.shader) != PortalClipShaderPath ||
                    item.renderQueue != PulledTargetRenderQueue ||
                    !item.HasProperty(PortalClipPlaneProperty) ||
                    Vector4.Distance(item.GetVector(PortalClipPlaneProperty), expectedClipPlane) > Tolerance))
                throw new InvalidOperationException("The pulled target portal clipping configuration changed.");
            var portalPlaneZ = approvedRing.z;

            var standingHash = ExactHash(SourceStanding, StandingAsset);
            var lyingHash = ExactHash(SourceLying, LyingAsset);
            var pointedIndex = SamplePointedIndex(slot, pierce, PierceDuration);
            var pierceTip = SamplePointedVertex(slot, pierce, PierceDuration, pointedIndex);
            var pullTip = SamplePointedVertex(slot, pull, 0f, pointedIndex);
            var tipError = Vector3.Distance(pierceTip, pullTip);
            if (tipError > 0.002f)
                throw new InvalidOperationException(
                    "The pointed tentacle pose jumps when the lying model is swapped. " +
                    "Error=" + Num(tipError) +
                    " PiercePoint=" + Vec(pierceTip) +
                    " PullPoint=" + Vec(pullTip));
            var tipFollow = MeasurePointedTipFollow(slot, pull, lying, portalPlaneZ, pointedIndex);
            if (tipFollow.MaximumError > 0.003f || !tipFollow.AllSamplesRemainImpaled)
                throw new InvalidOperationException(
                    "The pointed tentacle end does not stay attached to the pulled transporter body. " +
                    "MaximumError=" + Num(tipFollow.MaximumError) +
                    " AllSamplesRemainImpaled=" + tipFollow.AllSamplesRemainImpaled +
                    " FirstFailedTime=" + Num(tipFollow.FirstFailedTime) +
                    " CrossingFound=" + tipFollow.FirstCrossingFound +
                    " PointLocalY=" + Num(tipFollow.FirstPointZ) +
                    " TorsoBackLocalY=" + Num(tipFollow.FirstEntryZ) +
                    " TorsoFrontLocalY=" + Num(tipFollow.FirstExitZ) +
                    " EntryBoneLocalY=" + Num(tipFollow.FirstChainMinimumZ));
            var pointedExposure = MeasurePointedBaseExposure(
                slot,
                attachment,
                pull,
                lying,
                portalPlaneZ,
                pointedIndex);
            if (!pointedExposure.AllVisibleSamplesExposePointedBase ||
                pointedExposure.VisibleSampleCount == 0 ||
                pointedExposure.MinimumFrontClearance < LyingTorsoExitMargin - Tolerance)
                throw new InvalidOperationException(
                    "The transporter intersects the tapered pointed section instead of the shaft below it. " +
                    "MinimumPointedBaseFrontClearance=" + Num(pointedExposure.MinimumFrontClearance) +
                    " VisibleSampleCount=" + pointedExposure.VisibleSampleCount +
                    " AllVisibleSamplesExposePointedBase=" +
                    pointedExposure.AllVisibleSamplesExposePointedBase);
            var pullRotation = MeasurePullRotation(slot, pull, lying, portalPlaneZ);
            if (Mathf.Abs(pullRotation.StartX - PullStartXRotation) > 0.1f ||
                Mathf.Abs(pullRotation.EntryX) > 0.1f ||
                pullRotation.PostEntryMaximumAbsoluteX > 0.1f)
                throw new InvalidOperationException(
                    "The pulled transporter must rotate from X +60 degrees to X 0 before entering the portal. " +
                    "StartX=" + Num(pullRotation.StartX) +
                    " EntryTime=" + Num(pullRotation.EntryTime) +
                    " EntryX=" + Num(pullRotation.EntryX) +
                    " PostEntryMaximumAbsoluteX=" + Num(pullRotation.PostEntryMaximumAbsoluteX));
            var tentaclePenetration = MeasureTentaclePenetration(
                slot,
                attachment,
                pull,
                lying,
                pointedIndex,
                portalPlaneZ);
            if (tentaclePenetration.MaximumDirectionError > 2f ||
                !tentaclePenetration.AllEntrySamplesBehindBodyBackSurface)
                throw new InvalidOperationException(
                    "The tentacle penetration chain does not follow the transporter's local back-to-chest axis. " +
                    "BodyLocalNormalMaximumErrorDegrees=" + Num(tentaclePenetration.MaximumDirectionError) +
                    " MaximumDirectionErrorTime=" + Num(tentaclePenetration.MaximumDirectionErrorTime) +
                    " MaximumDirectionErrorSegment=" + tentaclePenetration.MaximumDirectionErrorSegment +
                    " CrossingSampleCount=" + tentaclePenetration.CrossingSampleCount +
                    " AllEntrySamplesBehindBodyBackSurface=" +
                    tentaclePenetration.AllEntrySamplesBehindBodyBackSurface +
                    " FirstFailedTime=" + Num(tentaclePenetration.FirstFailedTime) +
                    " FirstCrossingFound=" + tentaclePenetration.FirstCrossingFound +
                    " FirstEntryBoneLocalY=" + Num(tentaclePenetration.FirstEntryBoneZ) +
                    " FirstPointLocalY=" + Num(tentaclePenetration.FirstPointZ) +
                    " FirstTorsoBackLocalY=" + Num(tentaclePenetration.FirstBodyEntryZ) +
                    " FirstTorsoFrontLocalY=" + Num(tentaclePenetration.FirstExitZ));
            var struggle = MeasureTransporterStruggle(slot, attachment, pull, lying, pointedIndex);
            if (Mathf.Abs(struggle.UpperBodyRiseDegrees - UpperBodyRiseDegrees) > 0.5f ||
                struggle.MaximumArmStraightnessErrorDegrees > 0.5f ||
                struggle.LeftArmPeakDegrees < ArmSwingDegrees - 0.5f ||
                struggle.RightArmPeakDegrees > -ArmSwingDegrees + 0.5f ||
                struggle.LeftLegPeakDegrees < 6.5f ||
                struggle.RightLegPeakDegrees > -6.5f ||
                struggle.LateArmSwingMagnitudeDegrees < ArmSwingDegrees - 0.5f ||
                struggle.RepeatPoseMaximumErrorDegrees > 0.1f ||
                struggle.LoopBonePoseMaximumErrorDegrees > 0.1f ||
                struggle.ChestLengthFractionError > 0.15f)
                throw new InvalidOperationException(
                    "The pulled transporter struggle pose no longer matches the approved chest-pierced motion. " +
                    "UpperBodyRiseDegrees=" + Num(struggle.UpperBodyRiseDegrees) +
                    " MaximumArmStraightnessErrorDegrees=" +
                    Num(struggle.MaximumArmStraightnessErrorDegrees) +
                    " LeftArmPeakDegrees=" + Num(struggle.LeftArmPeakDegrees) +
                    " RightArmPeakDegrees=" + Num(struggle.RightArmPeakDegrees) +
                    " LeftLegPeakDegrees=" + Num(struggle.LeftLegPeakDegrees) +
                    " RightLegPeakDegrees=" + Num(struggle.RightLegPeakDegrees) +
                    " LateArmSwingMagnitudeDegrees=" + Num(struggle.LateArmSwingMagnitudeDegrees) +
                    " RepeatPoseMaximumErrorDegrees=" + Num(struggle.RepeatPoseMaximumErrorDegrees) +
                    " LoopBonePoseMaximumErrorDegrees=" + Num(struggle.LoopBonePoseMaximumErrorDegrees) +
                    " ChestLengthFractionError=" + Num(struggle.ChestLengthFractionError));

            var pullStart = SampleVisibilityAndBounds(slot, pull, 0f, lying);
            var pullEnd = SampleVisibilityAndBounds(slot, pull, PullDuration, lying);
            var pullStartPenetrated = PullStartPenetrates(pullStart.Bounds, pullTip);
            if (!pullStart.StandingHidden || !pullStart.LyingVisible || !pullStartPenetrated)
                throw new InvalidOperationException("The immediate lying swap no longer preserves the visible penetration.");
            if (pullStart.Bounds.max.z <= portalPlaneZ + PortalBehindMargin)
                throw new InvalidOperationException(
                    "The lying transporter has no visible front-side portion at pull-in start.");
            if (!pullEnd.StandingHidden || pullEnd.LyingVisible ||
                pullEnd.Bounds.max.z > portalPlaneZ - PortalBehindMargin + Tolerance)
                throw new InvalidOperationException(
                    "The full-size transporter does not pass completely behind the portal before hiding.");
            var partialOcclusionTime = FindPartialOcclusionTime(slot, pull, lying, portalPlaneZ);
            if (partialOcclusionTime < 0f)
                throw new InvalidOperationException(
                    "No pull-in sample contains both visible front-side and clipped back-side body portions.");

            var introStart = SampleVisibilityAndBounds(slot, intro, 0f, standing);
            if (!introStart.StandingVisible || !introStart.LyingHidden)
                throw new InvalidOperationException("The loop does not restore the standing transporter at Intro.");

            return new Metrics(
                uniform,
                standingWidthRatio,
                standingHeightRatio,
                lyingWidthRatio,
                lyingHeightRatio,
                frameBounds,
                standingBounds,
                lyingBounds,
                standing.localPosition,
                lying.localPosition,
                pull.length,
                tipError,
                pullStartPenetrated,
                !pullEnd.LyingVisible,
                portalPlaneZ,
                scaleVariation,
                pullStart.Bounds.min.z,
                pullEnd.Bounds.max.z,
                partialOcclusionTime,
                expectedClipPlane,
                pulledMaterials.Min(item => item.renderQueue),
                tipFollow.MaximumError,
                tipFollow.SampleCount,
                tipFollow.VisibleImpaledSampleCount,
                tipFollow.AllSamplesRemainImpaled,
                pointedExposure.MinimumFrontClearance,
                pointedExposure.VisibleSampleCount,
                pointedExposure.AllVisibleSamplesExposePointedBase,
                pullRotation.StartX,
                pullRotation.EntryTime,
                pullRotation.EntryX,
                pullRotation.PostEntryMaximumAbsoluteX,
                tentaclePenetration.MaximumDirectionError,
                tentaclePenetration.SampleCount,
                tentaclePenetration.CrossingSampleCount,
                tentaclePenetration.AllEntrySamplesBehindBodyBackSurface,
                struggle,
                standingHash,
                lyingHash);
        }

        private static AnimationClip RebaseClip(
            AnimationClip source,
            string destination,
            string name,
            Transform slot,
            Transform attachment)
        {
            var clip = LoadOrCreateClip(destination, name);
            ClearClip(clip);
            var prefix = AnimationUtility.CalculateTransformPath(attachment, slot);
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var rebased = binding;
                rebased.path = string.IsNullOrEmpty(binding.path) ? prefix : prefix + "/" + binding.path;
                AnimationUtility.SetEditorCurve(clip, rebased, AnimationUtility.GetEditorCurve(source, binding));
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var rebased = binding;
                rebased.path = string.IsNullOrEmpty(binding.path) ? prefix : prefix + "/" + binding.path;
                AnimationUtility.SetObjectReferenceCurve(
                    clip,
                    rebased,
                    AnimationUtility.GetObjectReferenceCurve(source, binding));
            }
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimationClip CreatePullClip(
            AnimationClip intro,
            AnimationClip pierce,
            Transform slot,
            Transform attachment,
            Transform standing,
            Transform lying,
            Vector3 lyingEndPosition,
            float portalPlaneZ)
        {
            var clip = LoadOrCreateClip(SlotPull, "Dolore_05_ExecutionPullIn_PullInSlot");
            ClearClip(clip);
            var prefix = AnimationUtility.CalculateTransformPath(attachment, slot);
            var introCurves = AnimationUtility.GetCurveBindings(intro)
                .ToDictionary(BindingKey, binding => new CurveSource(binding, AnimationUtility.GetEditorCurve(intro, binding)));
            var pierceCurves = AnimationUtility.GetCurveBindings(pierce)
                .ToDictionary(BindingKey, binding => new CurveSource(binding, AnimationUtility.GetEditorCurve(pierce, binding)));
            foreach (var key in introCurves.Keys.Union(pierceCurves.Keys).OrderBy(item => item, StringComparer.Ordinal))
            {
                var startSource = pierceCurves.TryGetValue(key, out var p) ? p : introCurves[key];
                var endSource = introCurves.TryGetValue(key, out var i) ? i : startSource;
                var start = startSource.Curve.Evaluate(pierce.length);
                var end = endSource.Curve.Evaluate(0f);
                if (startSource.Binding.propertyName.IndexOf("Euler", StringComparison.OrdinalIgnoreCase) >= 0)
                    end = NearestAngle(start, end);
                var rebased = startSource.Binding;
                rebased.path = string.IsNullOrEmpty(rebased.path) ? prefix : prefix + "/" + rebased.path;
                AnimationCurve curve;
                if (rebased.propertyName == "m_Enabled")
                    curve = ConstantThen(rebased, start, end);
                else
                    curve = AnimationCurve.EaseInOut(0f, start, PullDuration, end);
                AnimationUtility.SetEditorCurve(clip, rebased, curve);
            }
            AddTargetCurves(clip, slot, standing, lying, standing.localPosition, lying.localPosition, false, true);
            SetVectorCurve(clip, slot, lying, "m_LocalPosition", lying.localPosition, lyingEndPosition);
            SetConstantVectorCurve(clip, slot, lying, "m_LocalScale", lying.localScale);
            var uprightTime = PullDuration * 0.6f;
            for (var iteration = 0; iteration < 4; iteration++)
            {
                SetPullTargetRotationCurves(clip, slot, lying, uprightTime);
                var measuredEntryTime = FindPortalEntryTime(slot, clip, lying, portalPlaneZ);
                if (measuredEntryTime < 0f)
                    throw new InvalidOperationException("The pulled transporter never reaches the portal plane.");
                uprightTime = measuredEntryTime;
            }
            SetPullTargetRotationCurves(clip, slot, lying, uprightTime);
            AddTransporterStruggleCurves(clip, slot, lying);
            AddPointedTipFollowCurves(clip, slot, attachment, lying);
            foreach (var renderer in lying.GetComponentsInChildren<Renderer>(true))
                SetVisibilityCurve(clip, slot, renderer, true, false, PullVisibleUntil);
            foreach (var renderer in standing.GetComponentsInChildren<Renderer>(true))
                SetVisibilityCurve(clip, slot, renderer, false, false, 0f);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AddTransporterStruggleCurves(
            AnimationClip clip,
            Transform slot,
            Transform lying)
        {
            var controlled = UpperBodyBoneNames
                .Concat(new[]
                {
                    "LeftArm", "LeftForeArm", "RightArm", "RightForeArm",
                    "LeftUpLeg", "LeftLeg", "RightUpLeg", "RightLeg"
                })
                .Select(name => RequireDescendant(lying, name))
                .Distinct()
                .ToArray();
            var rest = lying.GetComponentsInChildren<Transform>(true)
                .Where(item => item != lying)
                .Select(TransformState.Capture)
                .ToArray();
            var times = Enumerable.Range(0, 17).Select(index => index * 0.125f).ToArray();
            var rotations = controlled.ToDictionary(
                bone => bone,
                _ => new Quaternion[times.Length]);
            try
            {
                for (var index = 0; index < times.Length; index++)
                {
                    var time = times[index];
                    clip.SampleAnimation(slot.gameObject, time);
                    foreach (var state in rest) state.Apply();
                    var phase = Mathf.Sin(time / StruggleCycleSeconds * Mathf.PI * 2f);
                    ApplyTransporterStrugglePose(lying, phase);
                    foreach (var bone in controlled)
                    {
                        var rotation = bone.localRotation.normalized;
                        var track = rotations[bone];
                        if (index > 0 && Quaternion.Dot(track[index - 1], rotation) < 0f)
                            rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                        track[index] = rotation;
                    }
                }

                var properties = new[]
                {
                    "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
                };
                foreach (var bone in controlled)
                {
                    var path = AnimationUtility.CalculateTransformPath(bone, slot);
                    var track = rotations[bone];
                    for (var component = 0; component < properties.Length; component++)
                    {
                        var keys = times.Select((time, index) => new Keyframe(
                                time,
                                component == 0 ? track[index].x :
                                component == 1 ? track[index].y :
                                component == 2 ? track[index].z : track[index].w))
                            .ToArray();
                        var curve = new AnimationCurve(keys);
                        for (var key = 0; key < curve.length; key++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                        }
                        AnimationUtility.SetEditorCurve(
                            clip,
                            EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[component]),
                            curve);
                    }
                }
            }
            finally
            {
                foreach (var state in rest) state.Apply();
            }
        }

        private static void ApplyTransporterStrugglePose(Transform lying, float phase)
        {
            var bodyRight = lying.TransformDirection(Vector3.right).normalized;
            for (var index = 0; index < UpperBodyBoneNames.Length; index++)
            {
                var spine = RequireDescendant(lying, UpperBodyBoneNames[index]);
                spine.rotation = Quaternion.AngleAxis(UpperBodyBoneRiseDegrees[index], bodyRight) *
                                 spine.rotation;
            }

            var gazeDirection = Quaternion.AngleAxis(UpperBodyRiseDegrees, bodyRight) *
                                lying.TransformDirection(Vector3.up);
            AlignLimbSegment(
                RequireDescendant(lying, "LeftArm"),
                RequireDescendant(lying, "LeftForeArm"),
                Quaternion.AngleAxis(ArmSwingDegrees * phase, bodyRight) * gazeDirection);
            AlignLimbSegment(
                RequireDescendant(lying, "LeftForeArm"),
                RequireDescendant(lying, "LeftHand"),
                Quaternion.AngleAxis(ArmSwingDegrees * phase, bodyRight) * gazeDirection);
            AlignLimbSegment(
                RequireDescendant(lying, "RightArm"),
                RequireDescendant(lying, "RightForeArm"),
                Quaternion.AngleAxis(-ArmSwingDegrees * phase, bodyRight) * gazeDirection);
            AlignLimbSegment(
                RequireDescendant(lying, "RightForeArm"),
                RequireDescendant(lying, "RightHand"),
                Quaternion.AngleAxis(-ArmSwingDegrees * phase, bodyRight) * gazeDirection);

            ApplyLegSwing(lying, "LeftUpLeg", "LeftLeg", "LeftFoot", LegSwingDegrees * phase, bodyRight);
            ApplyLegSwing(lying, "RightUpLeg", "RightLeg", "RightFoot", -LegSwingDegrees * phase, bodyRight);
        }

        private static void ApplyLegSwing(
            Transform lying,
            string upperName,
            string lowerName,
            string footName,
            float angle,
            Vector3 bodyRight)
        {
            var upper = RequireDescendant(lying, upperName);
            var lower = RequireDescendant(lying, lowerName);
            var foot = RequireDescendant(lying, footName);
            var upperDirection = lower.position - upper.position;
            AlignLimbSegment(
                upper,
                lower,
                Quaternion.AngleAxis(angle, bodyRight) * upperDirection);
            var lowerDirection = foot.position - lower.position;
            AlignLimbSegment(
                lower,
                foot,
                Quaternion.AngleAxis(-angle * 0.5f, bodyRight) * lowerDirection);
        }

        private static void AlignLimbSegment(Transform bone, Transform child, Vector3 desiredDirection)
        {
            var segment = child.position - bone.position;
            if (segment.sqrMagnitude <= Tolerance * Tolerance ||
                desiredDirection.sqrMagnitude <= Tolerance * Tolerance)
                throw new InvalidOperationException(bone.name + " limb segment cannot be aligned.");
            bone.rotation = Quaternion.FromToRotation(segment.normalized, desiredDirection.normalized) * bone.rotation;
        }

        private static void SetPullTargetRotationCurves(
            AnimationClip clip,
            Transform slot,
            Transform lying,
            float uprightTime)
        {
            var path = AnimationUtility.CalculateTransformPath(lying, slot);
            var start = Quaternion.Euler(PullStartXRotation, 0f, 0f);
            var end = Quaternion.identity;
            var properties = new[]
            {
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
            };
            var starts = new[] { start.x, start.y, start.z, start.w };
            var ends = new[] { end.x, end.y, end.z, end.w };
            uprightTime = Mathf.Clamp(uprightTime, 0.001f, PullDuration);
            for (var axis = 0; axis < properties.Length; axis++)
            {
                var curve = new AnimationCurve(
                    new Keyframe(0f, starts[axis], 0f, 0f),
                    new Keyframe(uprightTime, ends[axis], 0f, 0f),
                    new Keyframe(PullDuration, ends[axis], 0f, 0f));
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[axis]),
                    curve);
            }
        }

        private static AnimationCurve ConstantThen(EditorCurveBinding binding, float start, float end)
        {
            if (Mathf.Approximately(start, end)) return AnimationCurve.Constant(0f, PullDuration, start);
            return new AnimationCurve(
                new Keyframe(0f, start),
                new Keyframe(PullVisibleUntil, start),
                new Keyframe(PullDuration, end));
        }

        private static void AddTargetCurves(
            AnimationClip clip,
            Transform slot,
            Transform standing,
            Transform lying,
            Vector3 standingPosition,
            Vector3 lyingPosition,
            bool standingVisible,
            bool lyingVisible)
        {
            SetConstantVectorCurve(clip, slot, standing, "m_LocalPosition", standingPosition);
            SetConstantVectorCurve(clip, slot, standing, "m_LocalScale", standing.localScale);
            SetConstantVectorCurve(clip, slot, lying, "m_LocalPosition", lyingPosition);
            SetConstantVectorCurve(clip, slot, lying, "m_LocalScale", lying.localScale);
            foreach (var renderer in standing.GetComponentsInChildren<Renderer>(true))
                SetVisibilityCurve(clip, slot, renderer, standingVisible, standingVisible, 0f);
            foreach (var renderer in lying.GetComponentsInChildren<Renderer>(true))
                SetVisibilityCurve(clip, slot, renderer, lyingVisible, lyingVisible, 0f);
        }

        private static void SetVisibilityCurve(
            AnimationClip clip,
            Transform slot,
            Renderer renderer,
            bool start,
            bool end,
            float switchTime)
        {
            var binding = EditorCurveBinding.FloatCurve(
                AnimationUtility.CalculateTransformPath(renderer.transform, slot),
                renderer.GetType(),
                "m_Enabled");
            var startValue = start ? 1f : 0f;
            var endValue = end ? 1f : 0f;
            AnimationCurve curve;
            if (start == end)
                curve = AnimationCurve.Constant(0f, Mathf.Max(clip.length, 0.001f), startValue);
            else
                curve = new AnimationCurve(
                    new Keyframe(0f, startValue),
                    new Keyframe(switchTime, startValue),
                    new Keyframe(PullDuration, endValue));
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void SetConstantVectorCurve(
            AnimationClip clip,
            Transform slot,
            Transform target,
            string property,
            Vector3 value)
        {
            var path = AnimationUtility.CalculateTransformPath(target, slot);
            var values = new[] { value.x, value.y, value.z };
            var axes = new[] { "x", "y", "z" };
            var endTime = Mathf.Max(clip.length, 0.001f);
            for (var index = 0; index < 3; index++)
            {
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property + "." + axes[index]);
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    AnimationCurve.Constant(0f, endTime, values[index]));
            }
        }

        private static void SetVectorCurve(
            AnimationClip clip,
            Transform slot,
            Transform target,
            string property,
            Vector3 start,
            Vector3 end)
        {
            var path = AnimationUtility.CalculateTransformPath(target, slot);
            var valuesStart = new[] { start.x, start.y, start.z };
            var valuesEnd = new[] { end.x, end.y, end.z };
            var axes = new[] { "x", "y", "z" };
            for (var index = 0; index < 3; index++)
            {
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property + "." + axes[index]);
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    AnimationCurve.EaseInOut(0f, valuesStart[index], PullDuration, valuesEnd[index]));
            }
        }

        private static void AddPointedTipFollowCurves(
            AnimationClip clip,
            Transform slot,
            Transform attachment,
            Transform lying)
        {
            var renderer = RequireTentacleRenderer(attachment);
            var movingBone = renderer.bones.Single(item => item.name == FirstMovingBoneName);
            var movingParent = movingBone.parent ??
                         throw new InvalidOperationException(FirstMovingBoneName + " must have a parent bone.");
            var tipBone = renderer.bones.Single(item => item.name == TipBoneName);
            var entryBone = renderer.bones.Single(item => item.name == PenetrationEntryBoneName);
            var pointedDriver = PointedDriver(renderer, tipBone);
            var penetrationChain = PenetrationChain(renderer, pointedDriver);
            var controlledBones = penetrationChain.Distinct().ToArray();
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                clip.SampleAnimation(slot.gameObject, 0f);
                var initialPoints = PointedVertices(renderer, tipBone, slot);
                var initialBounds = BoundsFromPoints(initialPoints);
                var pointedIndex = Enumerable.Range(0, initialPoints.Length)
                    .OrderBy(index => (initialPoints[index] - initialBounds.center).sqrMagnitude)
                    .First();
                var initialPoint = initialPoints[pointedIndex];
                var impalementPointInLying = lying.InverseTransformPoint(slot.TransformPoint(initialPoint));
                var times = Enumerable.Range(0, 20).Select(index => index * 0.1f)
                    .Concat(new[] { PullVisibleUntil, PullDuration })
                    .Distinct()
                    .OrderBy(item => item)
                    .ToArray();
                var positions = new Vector3[times.Length];
                var rotations = controlledBones.ToDictionary(
                    bone => bone,
                    _ => new Quaternion[times.Length]);
                for (var index = 0; index < times.Length; index++)
                {
                    var time = times[index];
                    clip.SampleAnimation(slot.gameObject, time);
                    var position = movingBone.localPosition;
                    if (time <= PullVisibleUntil + Tolerance)
                    {
                        // The lying model's local +Y is its back-to-chest normal. Following this axis keeps
                        // the penetration line inside the same torso cross-section while X rotates 60 -> 0.
                        var desiredDirection = lying.TransformDirection(Vector3.up);
                        for (var segmentIndex = 0; segmentIndex + 1 < penetrationChain.Length; segmentIndex++)
                        {
                            var segmentBone = penetrationChain[segmentIndex];
                            var nextBone = penetrationChain[segmentIndex + 1];
                            if (segmentBone == nextBone) continue;
                            var entrySegment = nextBone.position - segmentBone.position;
                            if (entrySegment.sqrMagnitude <= Tolerance * Tolerance)
                                throw new InvalidOperationException(
                                    segmentBone.name + " penetration segment cannot be measured.");
                            segmentBone.rotation = Quaternion.FromToRotation(
                                                       entrySegment.normalized,
                                                       desiredDirection.normalized) *
                                                   segmentBone.rotation;
                        }

                        // The dominant skinned-mesh driver rotates the pointed mesh independently.
                        for (var iteration = 0; iteration < 4; iteration++)
                        {
                            var currentPoints = PointedVertices(renderer, tipBone, slot);
                            var pointedSegment = slot.TransformPoint(currentPoints[pointedIndex]) -
                                                 pointedDriver.position;
                            if (pointedSegment.sqrMagnitude <= Tolerance * Tolerance)
                                throw new InvalidOperationException(
                                    pointedDriver.name + " pointed mesh direction cannot be measured.");
                            pointedDriver.rotation = Quaternion.FromToRotation(
                                                        pointedSegment.normalized,
                                                        desiredDirection.normalized) *
                                                    pointedDriver.rotation;
                        }
                    }
                    foreach (var bone in controlledBones)
                    {
                        var rotation = bone.localRotation.normalized;
                        var track = rotations[bone];
                        if (index > 0 && Quaternion.Dot(track[index - 1], rotation) < 0f)
                            rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
                        bone.localRotation = rotation;
                        track[index] = rotation;
                    }
                    if (time <= PullVisibleUntil + Tolerance)
                    {
                        var desiredPoint = slot.InverseTransformPoint(lying.TransformPoint(impalementPointInLying));
                        var currentPoints = PointedVertices(renderer, tipBone, slot);
                        var deltaInSlot = desiredPoint - currentPoints[pointedIndex];
                        position += movingParent.InverseTransformVector(slot.TransformVector(deltaInSlot));
                        movingBone.localPosition = position;
                    }
                    positions[index] = position;
                }

                var path = AnimationUtility.CalculateTransformPath(movingBone, slot);
                var axes = new[] { "x", "y", "z" };
                for (var axis = 0; axis < 3; axis++)
                {
                    var keys = times.Select((time, index) =>
                        new Keyframe(time, axis == 0 ? positions[index].x : axis == 1 ? positions[index].y : positions[index].z))
                        .ToArray();
                    var curve = new AnimationCurve(keys);
                    for (var key = 0; key < curve.length; key++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                        AnimationUtility.SetKeyRightTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                    }
                    AnimationUtility.SetEditorCurve(
                        clip,
                        EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axes[axis]),
                        curve);
                }
                var rotationProperties = new[]
                {
                    "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
                };
                foreach (var bone in controlledBones)
                {
                    var bonePath = AnimationUtility.CalculateTransformPath(bone, slot);
                    var track = rotations[bone];
                    for (var component = 0; component < rotationProperties.Length; component++)
                    {
                        var keys = times.Select((time, index) => new Keyframe(
                                time,
                                component == 0 ? track[index].x :
                                component == 1 ? track[index].y :
                                component == 2 ? track[index].z : track[index].w))
                            .ToArray();
                        var curve = new AnimationCurve(keys);
                        for (var key = 0; key < curve.length; key++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(
                                curve,
                                key,
                                AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(
                                curve,
                                key,
                                AnimationUtility.TangentMode.Linear);
                        }
                        AnimationUtility.SetEditorCurve(
                            clip,
                            EditorCurveBinding.FloatCurve(
                                bonePath,
                                typeof(Transform),
                                rotationProperties[component]),
                            curve);
                    }
                }

                // Re-sample the completed position + rotation curves, then correct the actual skinned
                // pointed vertex. This keeps the same body-local impalement point after quaternion sampling.
                for (var correction = 0; correction < 2; correction++)
                {
                    for (var index = 0; index < times.Length; index++)
                    {
                        var time = times[index];
                        clip.SampleAnimation(slot.gameObject, time);
                        var position = movingBone.localPosition;
                        if (time <= PullVisibleUntil + Tolerance)
                        {
                            var currentPoints = PointedVertices(renderer, tipBone, slot);
                            var desiredPoint = slot.InverseTransformPoint(
                                lying.TransformPoint(impalementPointInLying));
                            var deltaInSlot = desiredPoint - currentPoints[pointedIndex];
                            position += movingParent.InverseTransformVector(slot.TransformVector(deltaInSlot));
                        }
                        positions[index] = position;
                    }

                    for (var axis = 0; axis < 3; axis++)
                    {
                        var keys = times.Select((time, index) => new Keyframe(
                                time,
                                axis == 0 ? positions[index].x :
                                axis == 1 ? positions[index].y : positions[index].z))
                            .ToArray();
                        var curve = new AnimationCurve(keys);
                        for (var key = 0; key < curve.length; key++)
                        {
                            AnimationUtility.SetKeyLeftTangentMode(
                                curve,
                                key,
                                AnimationUtility.TangentMode.Linear);
                            AnimationUtility.SetKeyRightTangentMode(
                                curve,
                                key,
                                AnimationUtility.TangentMode.Linear);
                        }
                        AnimationUtility.SetEditorCurve(
                            clip,
                            EditorCurveBinding.FloatCurve(
                                path,
                                typeof(Transform),
                                "m_LocalPosition." + axes[axis]),
                            curve);
                    }
                }
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static AnimatorController CreateController(
            AnimationClip intro,
            AnimationClip pierce,
            AnimationClip pull)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states.ToArray()) machine.RemoveState(state.state);
            foreach (var child in machine.stateMachines.ToArray()) machine.RemoveStateMachine(child.stateMachine);
            var introState = machine.AddState("Intro", new Vector3(180f, 30f));
            var pierceState = machine.AddState("PierceHold", new Vector3(390f, 30f));
            var pullState = machine.AddState("PullIn", new Vector3(600f, 30f));
            introState.motion = intro;
            pierceState.motion = pierce;
            pullState.motion = pull;
            machine.defaultState = introState;
            AddExitTransition(introState, pierceState);
            AddExitTransition(pierceState, pullState);
            AddExitTransition(pullState, introState);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddExitTransition(AnimatorState source, AnimatorState destination)
        {
            var transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.offset = 0f;
            transition.canTransitionToSelf = false;
        }

        private static void ValidateController(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length != 1)
                throw new InvalidOperationException("Execution controller must have one layer.");
            var states = controller.layers[0].stateMachine.states.Select(item => item.state).ToArray();
            var names = states.Select(item => item.name).OrderBy(item => item).ToArray();
            if (!names.SequenceEqual(new[] { "Intro", "PierceHold", "PullIn" }))
                throw new InvalidOperationException("Execution controller states changed.");
            foreach (var state in states)
            {
                if (state.transitions.Length != 1 || !state.transitions[0].hasExitTime ||
                    Mathf.Abs(state.transitions[0].exitTime - 1f) > Tolerance ||
                    Mathf.Abs(state.transitions[0].duration) > Tolerance)
                    throw new InvalidOperationException(state.name + " must have one zero-duration exit transition.");
            }
            var byName = states.ToDictionary(item => item.name);
            if (byName["Intro"].transitions[0].destinationState != byName["PierceHold"] ||
                byName["PierceHold"].transitions[0].destinationState != byName["PullIn"] ||
                byName["PullIn"].transitions[0].destinationState != byName["Intro"])
                throw new InvalidOperationException("Execution controller loop order changed.");
        }

        private static PierceSample SamplePierce(
            Transform slot,
            Transform attachment,
            AnimationClip pierce)
        {
            var renderer = RequireTentacleRenderer(attachment);
            var states = renderer.bones.Select(BoneState.Capture).ToArray();
            var enabled = renderer.enabled;
            try
            {
                pierce.SampleAnimation(attachment.gameObject, PierceDuration);
                var tip = renderer.bones.Single(item => item.name == TipBoneName);
                var ring = renderer.bones.Single(item => item.name == RingBoneName);
                var vertices = PointedVertices(renderer, tip, slot);
                return new PierceSample(
                    slot.InverseTransformPoint(tip.position),
                    slot.InverseTransformPoint(ring.position),
                    renderer.bones.Select(item => slot.InverseTransformPoint(item.position).z).Min(),
                    vertices);
            }
            finally
            {
                for (var index = 0; index < states.Length; index++) states[index].Apply(renderer.bones[index]);
                renderer.enabled = enabled;
            }
        }

        private static Vector3[] PointedVertices(
            SkinnedMeshRenderer renderer,
            Transform tip,
            Transform reference)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Tentacle mesh is missing.");
            var weights = mesh.boneWeights;
            var vertices = mesh.vertices;
            var bindPoses = mesh.bindposes;
            var matrices = renderer.bones.Select((bone, index) => bone.localToWorldMatrix * bindPoses[index]).ToArray();
            var dominant = weights.Select(DominantBone).ToArray();
            var driverIndex = Array.IndexOf(renderer.bones, PointedDriver(renderer, tip));
            if (driverIndex < 0) throw new InvalidOperationException("Tentacle tip driver is unavailable.");
            var result = new List<Vector3>();
            for (var index = 0; index < vertices.Length; index++)
            {
                if (dominant[index] != driverIndex) continue;
                var weight = weights[index];
                var world = Weighted(matrices, vertices[index], weight.boneIndex0, weight.weight0) +
                            Weighted(matrices, vertices[index], weight.boneIndex1, weight.weight1) +
                            Weighted(matrices, vertices[index], weight.boneIndex2, weight.weight2) +
                            Weighted(matrices, vertices[index], weight.boneIndex3, weight.weight3);
                result.Add(reference.InverseTransformPoint(world));
            }
            if (result.Count == 0) throw new InvalidOperationException("Tentacle pointed section is empty.");
            return result.ToArray();
        }

        private static Transform PointedDriver(SkinnedMeshRenderer renderer, Transform tip)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Tentacle mesh is missing.");
            var dominant = mesh.boneWeights.Select(DominantBone).ToArray();
            var driver = tip;
            while (driver != null)
            {
                var candidate = Array.IndexOf(renderer.bones, driver);
                if (candidate >= 0 && dominant.Contains(candidate))
                    return driver;
                driver = driver.parent;
            }
            throw new InvalidOperationException("Tentacle tip driver is unavailable.");
        }

        private static Transform[] PenetrationChain(
            SkinnedMeshRenderer renderer,
            Transform pointedDriver)
        {
            return new[] { PenetrationEntryBoneName }
                .Concat(PenetrationIntermediateBoneNames)
                .Select(name => renderer.bones.Single(item => item.name == name))
                .Concat(new[] { pointedDriver })
                .Distinct()
                .ToArray();
        }

        private static Vector3 Weighted(Matrix4x4[] matrices, Vector3 vertex, int bone, float weight)
        {
            return weight <= 0f ? Vector3.zero : matrices[bone].MultiplyPoint3x4(vertex) * weight;
        }

        private static int DominantBone(BoneWeight weight)
        {
            var index = weight.boneIndex0;
            var greatest = weight.weight0;
            if (weight.weight1 > greatest) { index = weight.boneIndex1; greatest = weight.weight1; }
            if (weight.weight2 > greatest) { index = weight.boneIndex2; greatest = weight.weight2; }
            if (weight.weight3 > greatest) index = weight.boneIndex3;
            return index;
        }

        private static float MaximumVisiblePlacement(Transform target, Transform reference, PierceSample pierce)
        {
            var triangles = ProjectedTriangles(target, reference);
            var maximum = float.PositiveInfinity;
            var overlap = 0;
            foreach (var vertex in pierce.PointedVertices)
            {
                if (!TryFrontZ(triangles, vertex.x, vertex.y, out var front)) continue;
                maximum = Mathf.Min(maximum, vertex.z - VisibilityMargin - front);
                overlap++;
            }
            if (overlap == 0 || float.IsInfinity(maximum))
                throw new InvalidOperationException(target.name + " does not overlap the tentacle pointed section.");
            if (maximum <= 0f)
                throw new InvalidOperationException(target.name + " cannot remain in the approved positive local Z range.");
            target.localPosition = new Vector3(target.localPosition.x, target.localPosition.y, maximum);
            var placedTriangles = ProjectedTriangles(target, reference);
            var greatestOverlappedFront = float.NegativeInfinity;
            foreach (var vertex in pierce.PointedVertices)
                if (TryFrontZ(placedTriangles, vertex.x, vertex.y, out var front))
                    greatestOverlappedFront = Mathf.Max(greatestOverlappedFront, front);
            if (float.IsNegativeInfinity(greatestOverlappedFront) ||
                pierce.ChainMinimumZ >= greatestOverlappedFront - Tolerance)
                throw new InvalidOperationException(
                    target.name + " does not place the tentacle chain behind its overlapped front surface.");
            target.localPosition = new Vector3(target.localPosition.x, target.localPosition.y, 0f);
            return maximum;
        }

        private static void AlignProjectedBodySurface(
            Transform target,
            Transform reference,
            Vector3 pointedAnchor,
            float preferredHeightFraction)
        {
            var bounds = BoundsIn(target, reference);
            var preferred = new Vector2(
                bounds.center.x,
                Mathf.Lerp(bounds.min.y, bounds.max.y, preferredHeightFraction));
            var width = Mathf.Max(bounds.size.x, Tolerance);
            var height = Mathf.Max(bounds.size.y, Tolerance);
            var triangle = ProjectedTriangles(target, reference)
                .Where(item => item.ProjectedArea > 0.0000001f)
                .OrderBy(item =>
                {
                    var center = item.Center;
                    var x = (center.x - preferred.x) / width;
                    var y = (center.y - preferred.y) / height;
                    return x * x + y * y;
                })
                .ThenByDescending(item => item.ProjectedArea)
                .FirstOrDefault();
            if (triangle.ProjectedArea <= 0.0000001f)
                throw new InvalidOperationException(target.name + " has no projected body surface for penetration.");
            var surface = triangle.Center;
            target.localPosition += new Vector3(
                pointedAnchor.x - surface.x,
                pointedAnchor.y - surface.y,
                0f);
        }

        private static List<Triangle> ProjectedTriangles(Transform root, Transform reference)
        {
            var result = new List<Triangle>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    var mesh = new Mesh();
                    try { skinned.BakeMesh(mesh); AddTriangles(mesh, skinned.transform, reference, result); }
                    finally { UnityEngine.Object.DestroyImmediate(mesh); }
                }
                else if (renderer is MeshRenderer meshRenderer)
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>();
                    if (filter != null) AddTriangles(filter.sharedMesh, filter.transform, reference, result);
                }
            }
            if (result.Count == 0) throw new InvalidOperationException(root.name + " has no mesh triangles.");
            return result;
        }

        private static void AddTriangles(Mesh mesh, Transform meshTransform, Transform reference, ICollection<Triangle> target)
        {
            if (mesh == null) return;
            var source = mesh.vertices;
            var vertices = source.Select(item => reference.InverseTransformPoint(meshTransform.TransformPoint(item))).ToArray();
            var indices = mesh.triangles;
            for (var index = 0; index + 2 < indices.Length; index += 3)
                target.Add(new Triangle(vertices[indices[index]], vertices[indices[index + 1]], vertices[indices[index + 2]]));
        }

        private static bool TryFrontZ(IEnumerable<Triangle> triangles, float x, float y, out float z)
        {
            z = float.NegativeInfinity;
            var found = false;
            foreach (var triangle in triangles)
            {
                if (!triangle.TryZ(x, y, out var candidate)) continue;
                z = Mathf.Max(z, candidate);
                found = true;
            }
            return found;
        }

        private static Crossing MeshCrossing(Transform target, Transform reference, Vector3 tip)
        {
            var values = new List<float>();
            foreach (var triangle in ProjectedTriangles(target, reference))
                if (triangle.TryZ(tip.x, tip.y, out var z)) values.Add(z);
            if (values.Count < 2) return new Crossing(false, 0f, 0f);
            values.Sort();
            return new Crossing(true, values.First(), values.Last());
        }

        private static LocalYCrossing MeshCrossingAlongLocalY(Transform target, Vector3 pointInTarget)
        {
            var values = new List<float>();
            foreach (var triangle in ProjectedTriangles(target, target))
                if (triangle.TryY(pointInTarget.x, pointInTarget.z, out var y)) values.Add(y);
            if (values.Count < 2) return new LocalYCrossing(false, 0f, 0f);
            values.Sort();
            return new LocalYCrossing(true, values.First(), values.Last());
        }

        private static float ChestLengthFraction(Transform lying)
        {
            var bounds = BoundsIn(lying, lying);
            if (bounds.size.z <= Tolerance)
                throw new InvalidOperationException(lying.name + " has no measurable chest-length axis.");
            var chest = RequireDescendant(lying, "Spine");
            var chestInLying = lying.InverseTransformPoint(chest.position);
            return Mathf.Clamp01(Mathf.InverseLerp(bounds.min.z, bounds.max.z, chestInLying.z));
        }

        private static TorsoPenetration TorsoPenetrationInLying(
            Transform lying,
            Transform slot,
            float terminalLengthInSlot,
            float preferredLengthFraction)
        {
            var bounds = BoundsIn(lying, lying);
            var candidates = new List<Tuple<float, TorsoPenetration>>();
            for (var zStep = -4; zStep <= 6; zStep++)
            {
                var zFraction = Mathf.Clamp01(preferredLengthFraction + zStep * 0.025f);
                for (var xStep = -5; xStep <= 5; xStep++)
                {
                    var xFraction = 0.5f + xStep * 0.05f;
                    var point = new Vector3(
                        Mathf.Lerp(bounds.min.x, bounds.max.x, xFraction),
                        bounds.center.y,
                        Mathf.Lerp(bounds.min.z, bounds.max.z, zFraction));
                    var crossing = MeshCrossingAlongLocalY(lying, point);
                    if (!crossing.HasCrossing) continue;
                    if (crossing.FrontY - crossing.BackY > LyingTorsoMaximumThickness + Tolerance)
                        continue;
                    var requiredLocalLength = crossing.FrontY - crossing.BackY +
                                              LyingTorsoExitMargin * 2f;
                    var requiredLengthInSlot = slot.InverseTransformVector(
                        lying.TransformVector(Vector3.up * requiredLocalLength)).magnitude;
                    if (requiredLengthInSlot > terminalLengthInSlot - Tolerance) continue;
                    var score = Mathf.Abs(zFraction - preferredLengthFraction) * 2f +
                                Mathf.Abs(xFraction - 0.5f);
                    candidates.Add(new Tuple<float, TorsoPenetration>(
                        score,
                        new TorsoPenetration(
                            new Vector3(point.x, crossing.BackY, point.z),
                            new Vector3(point.x, crossing.FrontY, point.z))));
                }
            }
            if (candidates.Count == 0)
                throw new InvalidOperationException(
                    lying.name + " has no torso cross-section thin enough for the unmodified terminal rig length. " +
                    "TerminalLengthInSlot=" + Num(terminalLengthInSlot));
            return candidates.OrderBy(item => item.Item1).First().Item2;
        }

        private static float TerminalPenetrationLengthInSlot(
            Transform slot,
            Transform attachment,
            AnimationClip pierce,
            int pointedIndex,
            Vector3 desiredDirection)
        {
            var renderer = RequireTentacleRenderer(attachment);
            var states = renderer.bones.Select(BoneState.Capture).ToArray();
            try
            {
                pierce.SampleAnimation(attachment.gameObject, PierceDuration);
                var tipBone = renderer.bones.Single(item => item.name == TipBoneName);
                var entryBone = renderer.bones.Single(item => item.name == PenetrationEntryBoneName);
                var pointedDriver = PointedDriver(renderer, tipBone);
                var penetrationChain = PenetrationChain(renderer, pointedDriver);
                for (var segmentIndex = 0; segmentIndex + 1 < penetrationChain.Length; segmentIndex++)
                {
                    var segmentBone = penetrationChain[segmentIndex];
                    var nextBone = penetrationChain[segmentIndex + 1];
                    if (segmentBone == nextBone) continue;
                    var entrySegment = nextBone.position - segmentBone.position;
                    segmentBone.rotation = Quaternion.FromToRotation(
                                               entrySegment.normalized,
                                               desiredDirection.normalized) *
                                           segmentBone.rotation;
                }
                for (var iteration = 0; iteration < 4; iteration++)
                {
                    var alignedPoints = PointedVertices(renderer, tipBone, slot);
                    var pointedSegment = slot.TransformPoint(alignedPoints[pointedIndex]) -
                                         pointedDriver.position;
                    pointedDriver.rotation = Quaternion.FromToRotation(
                                                 pointedSegment.normalized,
                                                 desiredDirection.normalized) *
                                             pointedDriver.rotation;
                }
                var points = PointedVertices(renderer, tipBone, slot);
                if (pointedIndex < 0 || pointedIndex >= points.Length)
                    throw new InvalidOperationException("The approved pointed vertex index is unavailable.");
                var entry = slot.InverseTransformPoint(entryBone.position);
                var directionInSlot = slot.InverseTransformDirection(desiredDirection).normalized;
                return Mathf.Abs(Vector3.Dot(points[pointedIndex] - entry, directionInSlot));
            }
            finally
            {
                for (var index = 0; index < states.Length; index++) states[index].Apply(renderer.bones[index]);
            }
        }

        private static Vector3 PointedBaseAnchorInSlot(
            Transform slot,
            Transform attachment,
            AnimationClip pierce,
            int pointedIndex,
            Vector3 desiredDirection,
            Vector3 pointedAnchor)
        {
            var renderer = RequireTentacleRenderer(attachment);
            var states = renderer.bones.Select(BoneState.Capture).ToArray();
            try
            {
                pierce.SampleAnimation(attachment.gameObject, PierceDuration);
                var tipBone = renderer.bones.Single(item => item.name == TipBoneName);
                var pointedDriver = PointedDriver(renderer, tipBone);
                var penetrationChain = PenetrationChain(renderer, pointedDriver);
                for (var segmentIndex = 0; segmentIndex + 1 < penetrationChain.Length; segmentIndex++)
                {
                    var segmentBone = penetrationChain[segmentIndex];
                    var nextBone = penetrationChain[segmentIndex + 1];
                    var segment = nextBone.position - segmentBone.position;
                    segmentBone.rotation = Quaternion.FromToRotation(
                                               segment.normalized,
                                               desiredDirection.normalized) *
                                           segmentBone.rotation;
                }
                for (var iteration = 0; iteration < 4; iteration++)
                {
                    var alignedPoints = PointedVertices(renderer, tipBone, slot);
                    var pointedSegment = slot.TransformPoint(alignedPoints[pointedIndex]) -
                                         pointedDriver.position;
                    pointedDriver.rotation = Quaternion.FromToRotation(
                                                 pointedSegment.normalized,
                                                 desiredDirection.normalized) *
                                             pointedDriver.rotation;
                }

                var points = PointedVertices(renderer, tipBone, slot);
                if (pointedIndex < 0 || pointedIndex >= points.Length)
                    throw new InvalidOperationException("The approved pointed vertex index is unavailable.");
                var directionInSlot = slot.InverseTransformDirection(desiredDirection).normalized;
                var selectedProjection = Vector3.Dot(points[pointedIndex], directionInSlot);
                var baseProjection = points.Min(point => Vector3.Dot(point, directionInSlot));
                if (selectedProjection <= baseProjection + Tolerance)
                    throw new InvalidOperationException("The pointed section has no measurable base below its center.");
                return pointedAnchor + directionInSlot * (baseProjection - selectedProjection);
            }
            finally
            {
                for (var index = 0; index < states.Length; index++) states[index].Apply(renderer.bones[index]);
            }
        }

        private static bool PullStartPenetrates(Bounds bounds, Vector3 tip)
        {
            return tip.x >= bounds.min.x - Tolerance && tip.x <= bounds.max.x + Tolerance &&
                   tip.y >= bounds.min.y - Tolerance && tip.y <= bounds.max.y + Tolerance &&
                   tip.z >= bounds.min.z - Tolerance;
        }

        private static int SamplePointedIndex(Transform slot, AnimationClip clip, float time)
        {
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                clip.SampleAnimation(slot.gameObject, time);
                var renderer = RequireTentacleRenderer(RequireChild(RequireChild(slot, ModelName), AttachmentName));
                var points = PointedVertices(
                    renderer,
                    renderer.bones.Single(item => item.name == TipBoneName),
                    slot);
                var bounds = BoundsFromPoints(points);
                return Enumerable.Range(0, points.Length)
                    .OrderBy(index => (points[index] - bounds.center).sqrMagnitude)
                    .First();
            }
            finally { snapshot.Restore(); }
        }

        private static Vector3 SamplePointedVertex(
            Transform slot,
            AnimationClip clip,
            float time,
            int pointedIndex)
        {
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                clip.SampleAnimation(slot.gameObject, time);
                var renderer = RequireTentacleRenderer(RequireChild(RequireChild(slot, ModelName), AttachmentName));
                var points = PointedVertices(
                    renderer,
                    renderer.bones.Single(item => item.name == TipBoneName),
                    slot);
                if (pointedIndex < 0 || pointedIndex >= points.Length)
                    throw new InvalidOperationException("The approved pointed vertex index is unavailable.");
                return points[pointedIndex];
            }
            finally { snapshot.Restore(); }
        }

        private static TipFollowMetrics MeasurePointedTipFollow(
            Transform slot,
            AnimationClip clip,
            Transform lying,
            float portalPlaneZ,
            int pointedIndex)
        {
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                var renderer = RequireTentacleRenderer(RequireChild(RequireChild(slot, ModelName), AttachmentName));
                var tip = renderer.bones.Single(item => item.name == TipBoneName);
                var entryBone = renderer.bones.Single(item => item.name == PenetrationEntryBoneName);
                clip.SampleAnimation(slot.gameObject, 0f);
                var initialPoints = PointedVertices(renderer, tip, slot);
                if (pointedIndex < 0 || pointedIndex >= initialPoints.Length)
                    throw new InvalidOperationException("The approved pointed vertex index is unavailable.");
                var initialPoint = initialPoints[pointedIndex];
                var impalementPointInLying = lying.InverseTransformPoint(slot.TransformPoint(initialPoint));
                var maximumError = 0f;
                var allSamplesRemainImpaled = true;
                var visibleImpaledSampleCount = 0;
                var firstFailedTime = -1f;
                var firstCrossingFound = true;
                var firstPointZ = 0f;
                var firstEntryZ = 0f;
                var firstExitZ = 0f;
                var firstChainMinimumZ = 0f;
                const int sampleCount = 20;
                for (var sample = 0; sample < sampleCount; sample++)
                {
                    var time = sample * 0.1f;
                    clip.SampleAnimation(slot.gameObject, time);
                    var currentPoint = PointedVertices(renderer, tip, slot)[pointedIndex];
                    var expectedPoint = slot.InverseTransformPoint(lying.TransformPoint(impalementPointInLying));
                    maximumError = Mathf.Max(maximumError, Vector3.Distance(currentPoint, expectedPoint));
                    var pointInLying = lying.InverseTransformPoint(slot.TransformPoint(currentPoint));
                    var entryBoneInLying = lying.InverseTransformPoint(entryBone.position);
                    var crossing = MeshCrossingAlongLocalY(lying, pointInLying);
                    // Once the back of the body crosses the portal, the entry surface is intentionally clipped.
                    // Keep tracking the pointed vertex, but judge visible through-body penetration only beforehand.
                    var bodyStillVisible = BoundsIn(lying, slot).min.z > portalPlaneZ + Tolerance;
                    if (!bodyStillVisible) continue;
                    visibleImpaledSampleCount++;
                    var remainsImpaled = crossing.HasCrossing &&
                                         pointInLying.y > crossing.FrontY + Tolerance &&
                                         entryBoneInLying.y < crossing.BackY - Tolerance;
                    if (!remainsImpaled && firstFailedTime < 0f)
                    {
                        firstFailedTime = time;
                        firstCrossingFound = crossing.HasCrossing;
                        firstPointZ = pointInLying.y;
                        firstEntryZ = crossing.BackY;
                        firstExitZ = crossing.FrontY;
                        firstChainMinimumZ = entryBoneInLying.y;
                    }
                    allSamplesRemainImpaled &= remainsImpaled;
                }
                return new TipFollowMetrics(
                    maximumError,
                    sampleCount,
                    allSamplesRemainImpaled && visibleImpaledSampleCount > 0,
                    visibleImpaledSampleCount,
                    firstFailedTime,
                    firstCrossingFound,
                    firstPointZ,
                    firstEntryZ,
                    firstExitZ,
                    firstChainMinimumZ);
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static PointedExposureMetrics MeasurePointedBaseExposure(
            Transform slot,
            Transform attachment,
            AnimationClip pull,
            Transform lying,
            float portalPlaneZ,
            int pointedIndex)
        {
            var renderer = RequireTentacleRenderer(attachment);
            var tipBone = renderer.bones.Single(item => item.name == TipBoneName);
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                var minimumFrontClearance = float.PositiveInfinity;
                var visibleSampleCount = 0;
                var allVisibleSamplesExposePointedBase = true;
                const int sampleCount = 20;
                for (var sample = 0; sample < sampleCount; sample++)
                {
                    var time = sample * 0.1f;
                    pull.SampleAnimation(slot.gameObject, time);
                    if (BoundsIn(lying, slot).min.z <= portalPlaneZ + Tolerance) continue;
                    var points = PointedVertices(renderer, tipBone, slot);
                    if (pointedIndex < 0 || pointedIndex >= points.Length)
                        throw new InvalidOperationException("The approved pointed vertex index is unavailable.");
                    var pointsInLying = points
                        .Select(point => lying.InverseTransformPoint(slot.TransformPoint(point)))
                        .ToArray();
                    var selectedPoint = pointsInLying[pointedIndex];
                    var baseY = pointsInLying.Min(point => point.y);
                    var pointedBase = new Vector3(selectedPoint.x, baseY, selectedPoint.z);
                    var crossing = MeshCrossingAlongLocalY(lying, pointedBase);
                    if (!crossing.HasCrossing)
                    {
                        allVisibleSamplesExposePointedBase = false;
                        continue;
                    }
                    visibleSampleCount++;
                    var clearance = pointedBase.y - crossing.FrontY;
                    minimumFrontClearance = Mathf.Min(minimumFrontClearance, clearance);
                    allVisibleSamplesExposePointedBase &= clearance >= LyingTorsoExitMargin - Tolerance;
                }
                if (float.IsPositiveInfinity(minimumFrontClearance)) minimumFrontClearance = -1f;
                return new PointedExposureMetrics(
                    minimumFrontClearance,
                    visibleSampleCount,
                    allVisibleSamplesExposePointedBase && visibleSampleCount > 0);
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static VisibilitySample SampleVisibilityAndBounds(
            Transform slot,
            AnimationClip clip,
            float time,
            Transform measured)
        {
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                clip.SampleAnimation(slot.gameObject, time);
                var standing = RequireChild(slot, StandingName);
                var lying = RequireChild(slot, LyingName);
                var standingVisible = standing.GetComponentsInChildren<Renderer>(true).Any(item => item.enabled);
                var lyingVisible = lying.GetComponentsInChildren<Renderer>(true).Any(item => item.enabled);
                return new VisibilitySample(
                    standingVisible,
                    !standingVisible,
                    lyingVisible,
                    !lyingVisible,
                    BoundsIn(measured, slot));
            }
            finally { snapshot.Restore(); }
        }

        private static float PullScaleVariation(AnimationClip clip, string lyingPath, float expectedScale)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip)
                .Where(item => item.path == lyingPath &&
                               item.type == typeof(Transform) &&
                               item.propertyName.StartsWith("m_LocalScale.", StringComparison.Ordinal))
                .ToDictionary(item => item.propertyName, item => AnimationUtility.GetEditorCurve(clip, item));
            var properties = new[] { "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
            if (properties.Any(property => !bindings.ContainsKey(property)))
                throw new InvalidOperationException("The pull-in clip is missing a lying transporter scale curve.");
            var greatest = 0f;
            for (var sample = 0; sample <= 8; sample++)
            {
                var time = PullDuration * sample / 8f;
                foreach (var property in properties)
                    greatest = Mathf.Max(greatest, Mathf.Abs(bindings[property].Evaluate(time) - expectedScale));
            }
            return greatest;
        }

        private static float FindPartialOcclusionTime(
            Transform slot,
            AnimationClip pull,
            Transform lying,
            float portalPlaneZ)
        {
            for (var sample = 1; sample < 20; sample++)
            {
                var time = PullDuration * sample / 20f;
                var state = SampleVisibilityAndBounds(slot, pull, time, lying);
                if (state.LyingVisible &&
                    state.Bounds.min.z < portalPlaneZ - Tolerance &&
                    state.Bounds.max.z > portalPlaneZ + Tolerance)
                    return time;
            }
            return -1f;
        }

        private static float FindPortalEntryTime(
            Transform slot,
            AnimationClip pull,
            Transform lying,
            float portalPlaneZ)
        {
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                pull.SampleAnimation(slot.gameObject, 0f);
                if (BoundsIn(lying, slot).min.z <= portalPlaneZ + Tolerance) return 0f;
                const int coarseSamples = 200;
                var previousTime = 0f;
                for (var sample = 1; sample <= coarseSamples; sample++)
                {
                    var time = PullDuration * sample / coarseSamples;
                    pull.SampleAnimation(slot.gameObject, time);
                    if (BoundsIn(lying, slot).min.z > portalPlaneZ + Tolerance)
                    {
                        previousTime = time;
                        continue;
                    }

                    var low = previousTime;
                    var high = time;
                    for (var iteration = 0; iteration < 14; iteration++)
                    {
                        var middle = (low + high) * 0.5f;
                        pull.SampleAnimation(slot.gameObject, middle);
                        if (BoundsIn(lying, slot).min.z <= portalPlaneZ + Tolerance)
                            high = middle;
                        else
                            low = middle;
                    }
                    return high;
                }
                return -1f;
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static PullRotationMetrics MeasurePullRotation(
            Transform slot,
            AnimationClip pull,
            Transform lying,
            float portalPlaneZ)
        {
            var entryTime = FindPortalEntryTime(slot, pull, lying, portalPlaneZ);
            if (entryTime < 0f)
                throw new InvalidOperationException("The pulled transporter never reaches the portal plane.");
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                pull.SampleAnimation(slot.gameObject, 0f);
                var startX = Mathf.DeltaAngle(0f, lying.localEulerAngles.x);
                pull.SampleAnimation(slot.gameObject, entryTime);
                var entryX = Mathf.DeltaAngle(0f, lying.localEulerAngles.x);
                var postEntryMaximumAbsoluteX = Mathf.Abs(entryX);
                const int postEntrySamples = 20;
                for (var sample = 1; sample <= postEntrySamples; sample++)
                {
                    var time = Mathf.Lerp(entryTime, PullDuration, sample / (float)postEntrySamples);
                    pull.SampleAnimation(slot.gameObject, time);
                    postEntryMaximumAbsoluteX = Mathf.Max(
                        postEntryMaximumAbsoluteX,
                        Mathf.Abs(Mathf.DeltaAngle(0f, lying.localEulerAngles.x)));
                }
                return new PullRotationMetrics(startX, entryTime, entryX, postEntryMaximumAbsoluteX);
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static TransporterStruggleMetrics MeasureTransporterStruggle(
            Transform slot,
            Transform attachment,
            AnimationClip pull,
            Transform lying,
            int pointedIndex)
        {
            var spineBones = UpperBodyBoneNames.Select(name => RequireDescendant(lying, name)).ToArray();
            var leftArm = RequireDescendant(lying, "LeftArm");
            var leftForeArm = RequireDescendant(lying, "LeftForeArm");
            var leftHand = RequireDescendant(lying, "LeftHand");
            var rightArm = RequireDescendant(lying, "RightArm");
            var rightForeArm = RequireDescendant(lying, "RightForeArm");
            var rightHand = RequireDescendant(lying, "RightHand");
            var leftUpLeg = RequireDescendant(lying, "LeftUpLeg");
            var leftLeg = RequireDescendant(lying, "LeftLeg");
            var rightUpLeg = RequireDescendant(lying, "RightUpLeg");
            var rightLeg = RequireDescendant(lying, "RightLeg");
            var controlled = spineBones.Concat(new[]
                {
                    leftArm, leftForeArm, rightArm, rightForeArm,
                    leftUpLeg, leftLeg, rightUpLeg, rightLeg
                })
                .Distinct()
                .ToArray();
            var restRotations = controlled.ToDictionary(item => item, item => item.localRotation);
            var leftLegRestDirection = SegmentDirectionInLying(lying, leftUpLeg, leftLeg);
            var rightLegRestDirection = SegmentDirectionInLying(lying, rightUpLeg, rightLeg);
            var renderer = RequireTentacleRenderer(attachment);
            var tipBone = renderer.bones.Single(item => item.name == TipBoneName);
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                pull.SampleAnimation(slot.gameObject, 0f);
                var upperBodyRise = spineBones.Sum(
                    spine => Quaternion.Angle(restRotations[spine], spine.localRotation));
                var lyingBounds = BoundsIn(lying, lying);
                var chest = RequireDescendant(lying, "Spine");
                var chestInLying = lying.InverseTransformPoint(chest.position);
                var points = PointedVertices(renderer, tipBone, slot);
                if (pointedIndex < 0 || pointedIndex >= points.Length)
                    throw new InvalidOperationException("The approved pointed vertex index is unavailable.");
                var pointInLying = lying.InverseTransformPoint(slot.TransformPoint(points[pointedIndex]));
                var chestFraction = Mathf.InverseLerp(lyingBounds.min.z, lyingBounds.max.z, chestInLying.z);
                var pointFraction = Mathf.InverseLerp(lyingBounds.min.z, lyingBounds.max.z, pointInLying.z);
                var chestLengthFractionError = Mathf.Abs(chestFraction - pointFraction);

                var maximumArmStraightnessError = 0f;
                for (var sample = 0; sample <= 15; sample++)
                {
                    pull.SampleAnimation(slot.gameObject, sample * 0.125f);
                    maximumArmStraightnessError = Mathf.Max(
                        maximumArmStraightnessError,
                        Vector3.Angle(leftForeArm.position - leftArm.position, leftHand.position - leftForeArm.position),
                        Vector3.Angle(rightForeArm.position - rightArm.position, rightHand.position - rightForeArm.position));
                }

                pull.SampleAnimation(slot.gameObject, 0.125f);
                var bodyRight = lying.TransformDirection(Vector3.right).normalized;
                var gaze = Quaternion.AngleAxis(UpperBodyRiseDegrees, bodyRight) *
                           lying.TransformDirection(Vector3.up);
                var leftArmPeak = Vector3.SignedAngle(
                    gaze,
                    leftForeArm.position - leftArm.position,
                    bodyRight);
                var rightArmPeak = Vector3.SignedAngle(
                    gaze,
                    rightForeArm.position - rightArm.position,
                    bodyRight);
                var leftLegPeak = Vector3.SignedAngle(
                    leftLegRestDirection,
                    SegmentDirectionInLying(lying, leftUpLeg, leftLeg),
                    Vector3.right);
                var rightLegPeak = Vector3.SignedAngle(
                    rightLegRestDirection,
                    SegmentDirectionInLying(lying, rightUpLeg, rightLeg),
                    Vector3.right);
                var repeatedPose = controlled.ToDictionary(item => item, item => item.localRotation);

                pull.SampleAnimation(slot.gameObject, 1.625f);
                var repeatPoseMaximumError = controlled.Max(
                    item => Quaternion.Angle(repeatedPose[item], item.localRotation));

                pull.SampleAnimation(slot.gameObject, 1.875f);
                bodyRight = lying.TransformDirection(Vector3.right).normalized;
                gaze = Quaternion.AngleAxis(UpperBodyRiseDegrees, bodyRight) *
                       lying.TransformDirection(Vector3.up);
                var lateArmSwingMagnitude = Mathf.Abs(Vector3.SignedAngle(
                    gaze,
                    leftForeArm.position - leftArm.position,
                    bodyRight));

                pull.SampleAnimation(slot.gameObject, 0f);
                var loopStartPose = controlled.ToDictionary(item => item, item => item.localRotation);
                pull.SampleAnimation(slot.gameObject, PullDuration);
                var loopBonePoseMaximumError = controlled.Max(
                    item => Quaternion.Angle(loopStartPose[item], item.localRotation));

                return new TransporterStruggleMetrics(
                    upperBodyRise,
                    maximumArmStraightnessError,
                    leftArmPeak,
                    rightArmPeak,
                    leftLegPeak,
                    rightLegPeak,
                    lateArmSwingMagnitude,
                    repeatPoseMaximumError,
                    loopBonePoseMaximumError,
                    chestLengthFractionError);
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static Vector3 SegmentDirectionInLying(Transform lying, Transform start, Transform end)
        {
            return lying.InverseTransformDirection(end.position - start.position).normalized;
        }

        private static TentaclePenetrationMetrics MeasureTentaclePenetration(
            Transform slot,
            Transform attachment,
            AnimationClip pull,
            Transform lying,
            int pointedIndex,
            float portalPlaneZ)
        {
            var renderer = RequireTentacleRenderer(attachment);
            var entryBone = renderer.bones.Single(item => item.name == PenetrationEntryBoneName);
            var tipBone = renderer.bones.Single(item => item.name == TipBoneName);
            var pointedDriver = PointedDriver(renderer, tipBone);
            var penetrationChain = PenetrationChain(renderer, pointedDriver);
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                var maximumDirectionError = 0f;
                var maximumDirectionErrorTime = -1f;
                var maximumDirectionErrorSegment = string.Empty;
                var allEntrySamplesBehindBodyBackSurface = true;
                var crossingSampleCount = 0;
                var firstFailedTime = -1f;
                var firstCrossingFound = true;
                var firstEntryBoneZ = 0f;
                var firstPointZ = 0f;
                var firstBodyEntryZ = 0f;
                var firstExitZ = 0f;
                const int sampleCount = 20;
                for (var sample = 0; sample < sampleCount; sample++)
                {
                    var time = sample * 0.1f;
                    pull.SampleAnimation(slot.gameObject, time);
                    if (BoundsIn(lying, slot).min.z <= portalPlaneZ + Tolerance) continue;
                    var points = PointedVertices(renderer, tipBone, slot);
                    if (pointedIndex < 0 || pointedIndex >= points.Length)
                        throw new InvalidOperationException("The approved pointed vertex index is unavailable.");
                    var pointInSlot = points[pointedIndex];
                    var pointInLying = lying.InverseTransformPoint(slot.TransformPoint(pointInSlot));
                    var entryBoneInLying = lying.InverseTransformPoint(entryBone.position);
                    var pointedDriverInLying = lying.InverseTransformPoint(pointedDriver.position);
                    var penetrationSegments = Enumerable.Range(0, penetrationChain.Length - 1)
                        .Select(index => new KeyValuePair<string, Vector3>(
                            penetrationChain[index].name + "->" + penetrationChain[index + 1].name,
                            lying.InverseTransformPoint(penetrationChain[index + 1].position) -
                            lying.InverseTransformPoint(penetrationChain[index].position)))
                        .ToArray();
                    foreach (var penetrationSegment in penetrationSegments)
                    {
                        var segment = penetrationSegment.Value;
                        var directionError = segment.sqrMagnitude <= Tolerance * Tolerance
                            ? 180f
                            : Vector3.Angle(segment, Vector3.up);
                        if (directionError > maximumDirectionError)
                        {
                            maximumDirectionError = directionError;
                            maximumDirectionErrorTime = time;
                            maximumDirectionErrorSegment = penetrationSegment.Key;
                        }
                    }
                    var pointedSegment = pointInLying - pointedDriverInLying;
                    var pointedDirectionError = pointedSegment.sqrMagnitude <= Tolerance * Tolerance
                        ? 180f
                        : Vector3.Angle(pointedSegment, Vector3.up);
                    if (pointedDirectionError > maximumDirectionError)
                    {
                        maximumDirectionError = pointedDirectionError;
                        maximumDirectionErrorTime = time;
                        maximumDirectionErrorSegment = pointedDriver.name + "->PointedVertex";
                    }

                    var crossing = MeshCrossingAlongLocalY(lying, pointInLying);
                    if (!crossing.HasCrossing) continue;
                    crossingSampleCount++;
                    var entryBehindBodyBackSurface = pointInLying.y > crossing.FrontY + Tolerance &&
                                                     entryBoneInLying.y < crossing.BackY - Tolerance;
                    if (!entryBehindBodyBackSurface && firstFailedTime < 0f)
                    {
                        firstFailedTime = time;
                        firstCrossingFound = crossing.HasCrossing;
                        firstEntryBoneZ = entryBoneInLying.y;
                        firstPointZ = pointInLying.y;
                        firstBodyEntryZ = crossing.BackY;
                        firstExitZ = crossing.FrontY;
                    }
                    allEntrySamplesBehindBodyBackSurface &= entryBehindBodyBackSurface;
                }
                return new TentaclePenetrationMetrics(
                    maximumDirectionError,
                    maximumDirectionErrorTime,
                    maximumDirectionErrorSegment,
                    sampleCount,
                    crossingSampleCount,
                    allEntrySamplesBehindBodyBackSurface && crossingSampleCount > 0,
                    firstFailedTime,
                    firstCrossingFound,
                    firstEntryBoneZ,
                    firstPointZ,
                    firstBodyEntryZ,
                    firstExitZ);
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static Bounds FrameBounds(Transform slot, Transform model, Transform attachment)
        {
            var matches = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(item => !item.transform.IsChildOf(attachment))
                .SelectMany(renderer => renderer.sharedMaterials.Select((material, index) =>
                    new { Renderer = renderer, Material = material, Submesh = index }))
                .Where(item => item.Material != null &&
                               string.Equals(item.Material.name, FrameMaterialName, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "The Dolore model must contain exactly one " + FrameMaterialName + " submesh.");
            return SubmeshBoundsIn(matches[0].Renderer, matches[0].Submesh, slot);
        }

        private static Bounds SubmeshBoundsIn(
            SkinnedMeshRenderer renderer,
            int submeshIndex,
            Transform reference)
        {
            if (renderer.sharedMesh == null || submeshIndex < 0 || submeshIndex >= renderer.sharedMesh.subMeshCount)
                throw new InvalidOperationException(renderer.name + " has an invalid frame submesh index.");
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked);
                var indices = baked.GetIndices(submeshIndex).Distinct().ToArray();
                if (indices.Length == 0)
                    throw new InvalidOperationException("The Dolore frame submesh has no vertices.");
                var vertices = baked.vertices;
                Func<int, Vector3> point = index =>
                    reference.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]));
                var bounds = new Bounds(point(indices[0]), Vector3.zero);
                for (var index = 1; index < indices.Length; index++) bounds.Encapsulate(point(indices[index]));
                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Bounds BoundsAtUnitScale(Transform target, Transform reference)
        {
            var position = target.localPosition;
            var rotation = target.localRotation;
            var scale = target.localScale;
            try
            {
                target.localPosition = Vector3.zero;
                target.localRotation = Quaternion.identity;
                target.localScale = Vector3.one;
                return BoundsIn(target, reference);
            }
            finally
            {
                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }
        }

        private static Bounds BoundsIn(Transform root, Transform reference)
        {
            return BoundsFromRenderers(root.GetComponentsInChildren<Renderer>(true), reference, root.name);
        }

        private static Bounds BoundsFromRenderers(IEnumerable<Renderer> renderers, Transform reference, string label)
        {
            var has = false;
            var result = new Bounds();
            foreach (var renderer in renderers)
            foreach (var corner in Corners(renderer.bounds))
            {
                var local = reference.InverseTransformPoint(corner);
                if (!has) { result = new Bounds(local, Vector3.zero); has = true; }
                else result.Encapsulate(local);
            }
            if (!has) throw new InvalidOperationException(label + " has no renderer bounds.");
            return result;
        }

        private static Bounds BoundsFromPoints(IReadOnlyList<Vector3> points)
        {
            if (points.Count == 0) throw new InvalidOperationException("Point bounds require at least one point.");
            var result = new Bounds(points[0], Vector3.zero);
            for (var index = 1; index < points.Count; index++) result.Encapsulate(points[index]);
            return result;
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            for (var z = 0; z < 2; z++)
                yield return new Vector3(
                    x == 0 ? bounds.min.x : bounds.max.x,
                    y == 0 ? bounds.min.y : bounds.max.y,
                    z == 0 ? bounds.min.z : bounds.max.z);
        }

        private static void CaptureAnimatorState(
            Animator animator,
            Camera camera,
            Transform slot,
            string state,
            float normalizedTime,
            string folder,
            string file)
        {
            animator.Play(state, 0, normalizedTime);
            animator.Update(0f);
            var renderers = slot.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            var bounds = BoundsFromRenderers(renderers, slot, state);
            var worldCenter = slot.TransformPoint(bounds.center);
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 1f);
            var outward = slot.forward;
            camera.transform.position = worldCenter + outward * size * 2.5f + Vector3.up * size * 0.12f;
            camera.transform.LookAt(worldCenter);
            var texture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                RenderTexture.active = texture;
                camera.Render();
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(folder, file), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                texture.Release();
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static Dictionary<Transform, int> SetLayer(Transform root, int layer)
        {
            var result = new Dictionary<Transform, int>();
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                result[transform] = transform.gameObject.layer;
                transform.gameObject.layer = layer;
            }
            return result;
        }

        private static void RestoreLayers(IReadOnlyDictionary<Transform, int> layers)
        {
            foreach (var pair in layers) if (pair.Key != null) pair.Key.gameObject.layer = pair.Value;
        }

        private static void SetRenderers(Transform root, bool enabled)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = enabled;
        }

        private static void DisableChildAnimators(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        }

        private static void AssignPulledTargetMaterials(Transform lying, Transform slot, Vector3 ringAnchor)
        {
            var shader = RequireAsset<Shader>(PortalClipShaderPath);
            var clipPlane = PortalClipPlane(slot, ringAnchor);
            var renderers = lying.GetComponentsInChildren<Renderer>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(item.transform, lying), StringComparer.Ordinal)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The lying transporter has no renderer for portal occlusion.");
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                var sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials.Length == 0 || sourceMaterials.Any(item => item == null))
                    throw new InvalidOperationException(renderer.name + " has an invalid source material.");
                var pulledMaterials = new Material[sourceMaterials.Length];
                for (var materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
                {
                    var source = sourceMaterials[materialIndex];
                    var assetPath = PulledTargetMaterialPrefix + rendererIndex.ToString("00") + "_" +
                                    materialIndex.ToString("00") + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (material == null)
                    {
                        material = new Material(source);
                        AssetDatabase.CreateAsset(material, assetPath);
                    }
                    else
                    {
                        EditorUtility.CopySerialized(source, material);
                    }
                    material.name = "Dolore_05_Pulled_Target_" + rendererIndex.ToString("00") + "_" +
                                    materialIndex.ToString("00");
                    material.shader = shader;
                    material.SetVector(PortalClipPlaneProperty, clipPlane);
                    material.renderQueue = PulledTargetRenderQueue;
                    EditorUtility.SetDirty(material);
                    pulledMaterials[materialIndex] = material;
                }
                renderer.sharedMaterials = pulledMaterials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static Vector4 PortalClipPlane(Transform slot, Vector3 ringAnchor)
        {
            var normal = slot.TransformDirection(Vector3.forward).normalized;
            var point = slot.TransformPoint(ringAnchor);
            return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
        }

        private static void RemovePortalOccluder(Transform slot)
        {
            var existing = slot.Find(PortalOccluderName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        private static void WriteInspection(Metrics metrics, string phase, bool saved)
        {
            var text = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Phase=" + phase)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + SlotName)
                .AppendLine("StandingSource=" + SourceStanding)
                .AppendLine("StandingAsset=" + StandingAsset)
                .AppendLine("StandingSHA256=" + metrics.StandingSourceHash)
                .AppendLine("LyingSource=" + SourceLying)
                .AppendLine("LyingAsset=" + LyingAsset)
                .AppendLine("LyingSHA256=" + metrics.LyingSourceHash)
                .AppendLine("UniformScale=" + Num(metrics.UniformScale))
                .AppendLine("OriginalProportionMaintained=True")
                .AppendLine("FrameBounds=" + BoundsText(metrics.FrameBounds))
                .AppendLine("StandingBounds=" + BoundsText(metrics.StandingBounds))
                .AppendLine("LyingBounds=" + BoundsText(metrics.LyingBounds))
                .AppendLine("StandingWidthRatio=" + Num(metrics.StandingWidthRatio))
                .AppendLine("StandingHeightRatio=" + Num(metrics.StandingHeightRatio))
                .AppendLine("LyingWidthRatio=" + Num(metrics.LyingWidthRatio))
                .AppendLine("LyingHeightRatio=" + Num(metrics.LyingHeightRatio))
                .AppendLine("FrameFitTargetRatio=0.8")
                .AppendLine("BothPosesFitWithinFrame=True")
                .AppendLine("StandingPosition=" + Vec(metrics.StandingPosition))
                .AppendLine("LyingImpactPosition=" + Vec(metrics.LyingPosition))
                .AppendLine("Controller=" + ControllerPath)
                .AppendLine("StateOrder=Intro->PierceHold->PullIn->Intro")
                .AppendLine("PullClip=" + SlotPull)
                .AppendLine("PullSeconds=" + Num(metrics.PullLength))
                .AppendLine("ImmediateLyingSwap=True")
                .AppendLine("PierceToPullTipError=" + Num(metrics.PierceToPullTipError))
                .AppendLine("PointedTipFollowMaximumError=" + Num(metrics.PointedTipFollowMaximumError))
                .AppendLine("PointedTipFollowSampleCount=" + metrics.PointedTipFollowSampleCount)
                .AppendLine("PointedTipVisibleImpaledSampleCount=" + metrics.PointedTipVisibleImpaledSampleCount)
                .AppendLine("PointedTipRemainsImpaledAtAllSamples=" + metrics.PointedTipRemainsImpaledAtAllSamples)
                .AppendLine("PointedBaseMinimumFrontClearance=" +
                            Num(metrics.PointedBaseMinimumFrontClearance))
                .AppendLine("PointedBaseVisibleSampleCount=" + metrics.PointedBaseVisibleSampleCount)
                .AppendLine("PointedBaseExposedAtAllVisibleSamples=" +
                            metrics.PointedBaseExposedAtAllVisibleSamples)
                .AppendLine("PullStartXRotation=" + Num(metrics.PullStartXRotation))
                .AppendLine("PortalEntryTime=" + Num(metrics.PortalEntryTime))
                .AppendLine("PortalEntryXRotation=" + Num(metrics.PortalEntryXRotation))
                .AppendLine("PostEntryMaximumAbsoluteXRotation=" + Num(metrics.PostEntryMaximumAbsoluteXRotation))
                .AppendLine("TentacleBodyLocalNormalMaximumErrorDegrees=" +
                            Num(metrics.TentaclePenetrationChainMaximumError))
                .AppendLine("TentaclePenetrationSampleCount=" + metrics.TentaclePenetrationSampleCount)
                .AppendLine("TentaclePenetrationCrossingSampleCount=" +
                            metrics.TentaclePenetrationCrossingSampleCount)
                .AppendLine("TentacleEntryBoneRemainsBehindTorsoBackSurface=" +
                            metrics.TentacleEntryBoneRemainsBehindBodyBackSurface)
                .AppendLine("ChestPiercingUsesSpineBone=True")
                .AppendLine("ChestLengthFractionError=" + Num(metrics.Struggle.ChestLengthFractionError))
                .AppendLine("UpperBodyRiseDegrees=" + Num(metrics.Struggle.UpperBodyRiseDegrees))
                .AppendLine("PelvisRotationUnchangedByUpperBodyRise=True")
                .AppendLine("MaximumArmStraightnessErrorDegrees=" +
                            Num(metrics.Struggle.MaximumArmStraightnessErrorDegrees))
                .AppendLine("LeftArmPeakDegrees=" + Num(metrics.Struggle.LeftArmPeakDegrees))
                .AppendLine("RightArmPeakDegrees=" + Num(metrics.Struggle.RightArmPeakDegrees))
                .AppendLine("LeftLegPeakDegrees=" + Num(metrics.Struggle.LeftLegPeakDegrees))
                .AppendLine("RightLegPeakDegrees=" + Num(metrics.Struggle.RightLegPeakDegrees))
                .AppendLine("LateArmSwingMagnitudeDegrees=" +
                            Num(metrics.Struggle.LateArmSwingMagnitudeDegrees))
                .AppendLine("RepeatPoseMaximumErrorDegrees=" +
                            Num(metrics.Struggle.RepeatPoseMaximumErrorDegrees))
                .AppendLine("LoopBonePoseMaximumErrorDegrees=" +
                            Num(metrics.Struggle.LoopBonePoseMaximumErrorDegrees))
                .AppendLine("StruggleRepeatsUntilPortalEntry=True")
                .AppendLine("PortalPlaneZ=" + Num(metrics.PortalPlaneZ))
                .AppendLine("PullScaleVariation=" + Num(metrics.PullScaleVariation))
                .AppendLine("PullScaleConstant=True")
                .AppendLine("PullStartMinimumZ=" + Num(metrics.PullStartMinimumZ))
                .AppendLine("PullEndMaximumZ=" + Num(metrics.PullEndMaximumZ))
                .AppendLine("PartialOcclusionSampleTime=" + Num(metrics.PartialOcclusionSampleTime))
                .AppendLine("CrossedPortionHiddenImmediately=True")
                .AppendLine("PortalClipShader=" + PortalClipShaderPath)
                .AppendLine("PortalClipPlaneWorld=" + Vec4(metrics.PortalClipPlaneWorld))
                .AppendLine("PixelClipIndependentOfRenderOrder=True")
                .AppendLine("LegacyDepthOccluderPresent=False")
                .AppendLine("PulledTargetRenderQueue=" + metrics.PulledTargetRenderQueue)
                .AppendLine("PullStartLyingPenetrated=" + metrics.PullStartLyingPenetrated)
                .AppendLine("PullEndHidden=" + metrics.PullEndHidden)
                .AppendLine("LoopRestoresStanding=True")
                .AppendLine("LoopRestartsAtRingGeneration=True")
                .AppendLine("ColliderCount=0")
                .AppendLine("SourceFbxBytesChanged=False")
                .AppendLine("HarnessValidationExecuted=False")
                .AppendLine("SceneSaved=" + saved)
                .ToString();
            File.WriteAllText(Absolute(InspectionPath), text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(InspectionPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static string ExactHash(string source, string asset)
        {
            var sourceHash = Hash(Absolute(source));
            var assetHash = Hash(Absolute(asset));
            if (sourceHash != assetHash)
                throw new InvalidOperationException(asset + " bytes differ from the approved source.");
            return sourceHash;
        }

        private static void CopyExact(string source, string asset)
        {
            var sourcePath = Absolute(source);
            var assetPath = Absolute(asset);
            if (!File.Exists(sourcePath)) throw new InvalidOperationException("Approved source is missing: " + source);
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? AssetFolder);
            if (!File.Exists(assetPath) || Hash(sourcePath) != Hash(assetPath)) File.Copy(sourcePath, assetPath, true);
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ExactHash(source, asset);
        }

        private static string Hash(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            return scene;
        }

        private static Transform RequireSlot(Scene scene)
        {
            var placement = scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                            throw new InvalidOperationException("Approved Dolore placement is missing.");
            var slot = placement.transform.Find(SlotName) ??
                       throw new InvalidOperationException("Dolore execution slot is missing.");
            return slot;
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            return parent.Find(name) ?? throw new InvalidOperationException(parent.name + " is missing " + name + ".");
        }

        private static Transform RequireDescendant(Transform parent, string name)
        {
            var matches = parent.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    parent.name + " must contain exactly one " + name + " bone. Count=" + matches.Length);
            return matches[0];
        }

        private static SkinnedMeshRenderer RequireTentacleRenderer(Transform attachment)
        {
            return attachment.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault(item => item.sharedMesh != null &&
                                                item.bones.Length == ExpectedTentacleBoneCount &&
                                                item.bones.Any(bone => bone.name == TipBoneName) &&
                                                item.bones.Any(bone => bone.name == RingBoneName)) ??
                   throw new InvalidOperationException("The approved 13-bone tentacle renderer is missing.");
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                   throw new InvalidOperationException("Required asset is missing: " + path);
        }

        private static AnimationClip LoadOrCreateClip(string path, string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) { clip.name = name; return clip; }
            clip = new AnimationClip { name = name, frameRate = 60f };
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void ClearClip(AnimationClip clip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            clip.frameRate = 60f;
        }

        private static void SetClipLoop(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static string BindingKey(EditorCurveBinding binding)
        {
            return binding.path + "|" + binding.type.AssemblyQualifiedName + "|" + binding.propertyName;
        }

        private static float NearestAngle(float start, float end)
        {
            return start + Mathf.DeltaAngle(start, end);
        }

        private static string Absolute(string relative)
        {
            return Path.GetFullPath(Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value) => value.ToString("0.#########", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) => "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        private static string Vec4(Vector4 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";
        private static string BoundsText(Bounds value) => "Center=" + Vec(value.center) + " Size=" + Vec(value.size);

        private readonly struct CurveSource
        {
            public CurveSource(EditorCurveBinding binding, AnimationCurve curve) { Binding = binding; Curve = curve; }
            public EditorCurveBinding Binding { get; }
            public AnimationCurve Curve { get; }
        }

        private readonly struct PierceSample
        {
            public PierceSample(Vector3 tip, Vector3 ringAnchor, float chainMinimumZ, Vector3[] pointedVertices)
            { Tip = tip; RingAnchor = ringAnchor; ChainMinimumZ = chainMinimumZ; PointedVertices = pointedVertices; }
            public Vector3 Tip { get; }
            public Vector3 RingAnchor { get; }
            public float ChainMinimumZ { get; }
            public Vector3[] PointedVertices { get; }
        }

        private readonly struct Crossing
        {
            public Crossing(bool hasCrossing, float entryZ, float exitZ)
            { HasCrossing = hasCrossing; EntryZ = entryZ; ExitZ = exitZ; }
            public bool HasCrossing { get; }
            public float EntryZ { get; }
            public float ExitZ { get; }
        }

        private readonly struct LocalYCrossing
        {
            public LocalYCrossing(bool hasCrossing, float backY, float frontY)
            { HasCrossing = hasCrossing; BackY = backY; FrontY = frontY; }
            public bool HasCrossing { get; }
            public float BackY { get; }
            public float FrontY { get; }
        }

        private readonly struct TorsoPenetration
        {
            public TorsoPenetration(Vector3 back, Vector3 front)
            { Back = back; Front = front; }
            public Vector3 Back { get; }
            public Vector3 Front { get; }
        }

        private readonly struct VisibilitySample
        {
            public VisibilitySample(bool standingVisible, bool standingHidden, bool lyingVisible, bool lyingHidden, Bounds bounds)
            { StandingVisible = standingVisible; StandingHidden = standingHidden; LyingVisible = lyingVisible; LyingHidden = lyingHidden; Bounds = bounds; }
            public bool StandingVisible { get; }
            public bool StandingHidden { get; }
            public bool LyingVisible { get; }
            public bool LyingHidden { get; }
            public Bounds Bounds { get; }
        }

        private readonly struct TipFollowMetrics
        {
            public TipFollowMetrics(
                float maximumError,
                int sampleCount,
                bool allSamplesRemainImpaled,
                int visibleImpaledSampleCount,
                float firstFailedTime,
                bool firstCrossingFound,
                float firstPointZ,
                float firstEntryZ,
                float firstExitZ,
                float firstChainMinimumZ)
            {
                MaximumError = maximumError;
                SampleCount = sampleCount;
                AllSamplesRemainImpaled = allSamplesRemainImpaled;
                VisibleImpaledSampleCount = visibleImpaledSampleCount;
                FirstFailedTime = firstFailedTime;
                FirstCrossingFound = firstCrossingFound;
                FirstPointZ = firstPointZ;
                FirstEntryZ = firstEntryZ;
                FirstExitZ = firstExitZ;
                FirstChainMinimumZ = firstChainMinimumZ;
            }
            public float MaximumError { get; }
            public int SampleCount { get; }
            public bool AllSamplesRemainImpaled { get; }
            public int VisibleImpaledSampleCount { get; }
            public float FirstFailedTime { get; }
            public bool FirstCrossingFound { get; }
            public float FirstPointZ { get; }
            public float FirstEntryZ { get; }
            public float FirstExitZ { get; }
            public float FirstChainMinimumZ { get; }
        }

        private readonly struct PointedExposureMetrics
        {
            public PointedExposureMetrics(
                float minimumFrontClearance,
                int visibleSampleCount,
                bool allVisibleSamplesExposePointedBase)
            {
                MinimumFrontClearance = minimumFrontClearance;
                VisibleSampleCount = visibleSampleCount;
                AllVisibleSamplesExposePointedBase = allVisibleSamplesExposePointedBase;
            }
            public float MinimumFrontClearance { get; }
            public int VisibleSampleCount { get; }
            public bool AllVisibleSamplesExposePointedBase { get; }
        }

        private readonly struct TransporterStruggleMetrics
        {
            public TransporterStruggleMetrics(
                float upperBodyRiseDegrees,
                float maximumArmStraightnessErrorDegrees,
                float leftArmPeakDegrees,
                float rightArmPeakDegrees,
                float leftLegPeakDegrees,
                float rightLegPeakDegrees,
                float lateArmSwingMagnitudeDegrees,
                float repeatPoseMaximumErrorDegrees,
                float loopBonePoseMaximumErrorDegrees,
                float chestLengthFractionError)
            {
                UpperBodyRiseDegrees = upperBodyRiseDegrees;
                MaximumArmStraightnessErrorDegrees = maximumArmStraightnessErrorDegrees;
                LeftArmPeakDegrees = leftArmPeakDegrees;
                RightArmPeakDegrees = rightArmPeakDegrees;
                LeftLegPeakDegrees = leftLegPeakDegrees;
                RightLegPeakDegrees = rightLegPeakDegrees;
                LateArmSwingMagnitudeDegrees = lateArmSwingMagnitudeDegrees;
                RepeatPoseMaximumErrorDegrees = repeatPoseMaximumErrorDegrees;
                LoopBonePoseMaximumErrorDegrees = loopBonePoseMaximumErrorDegrees;
                ChestLengthFractionError = chestLengthFractionError;
            }
            public float UpperBodyRiseDegrees { get; }
            public float MaximumArmStraightnessErrorDegrees { get; }
            public float LeftArmPeakDegrees { get; }
            public float RightArmPeakDegrees { get; }
            public float LeftLegPeakDegrees { get; }
            public float RightLegPeakDegrees { get; }
            public float LateArmSwingMagnitudeDegrees { get; }
            public float RepeatPoseMaximumErrorDegrees { get; }
            public float LoopBonePoseMaximumErrorDegrees { get; }
            public float ChestLengthFractionError { get; }
        }

        private readonly struct PullRotationMetrics
        {
            public PullRotationMetrics(
                float startX,
                float entryTime,
                float entryX,
                float postEntryMaximumAbsoluteX)
            {
                StartX = startX;
                EntryTime = entryTime;
                EntryX = entryX;
                PostEntryMaximumAbsoluteX = postEntryMaximumAbsoluteX;
            }
            public float StartX { get; }
            public float EntryTime { get; }
            public float EntryX { get; }
            public float PostEntryMaximumAbsoluteX { get; }
        }

        private readonly struct TentaclePenetrationMetrics
        {
            public TentaclePenetrationMetrics(
                float maximumDirectionError,
                float maximumDirectionErrorTime,
                string maximumDirectionErrorSegment,
                int sampleCount,
                int crossingSampleCount,
                bool allEntrySamplesBehindBodyBackSurface,
                float firstFailedTime,
                bool firstCrossingFound,
                float firstEntryBoneZ,
                float firstPointZ,
                float firstBodyEntryZ,
                float firstExitZ)
            {
                MaximumDirectionError = maximumDirectionError;
                MaximumDirectionErrorTime = maximumDirectionErrorTime;
                MaximumDirectionErrorSegment = maximumDirectionErrorSegment;
                SampleCount = sampleCount;
                CrossingSampleCount = crossingSampleCount;
                AllEntrySamplesBehindBodyBackSurface = allEntrySamplesBehindBodyBackSurface;
                FirstFailedTime = firstFailedTime;
                FirstCrossingFound = firstCrossingFound;
                FirstEntryBoneZ = firstEntryBoneZ;
                FirstPointZ = firstPointZ;
                FirstBodyEntryZ = firstBodyEntryZ;
                FirstExitZ = firstExitZ;
            }
            public float MaximumDirectionError { get; }
            public float MaximumDirectionErrorTime { get; }
            public string MaximumDirectionErrorSegment { get; }
            public int SampleCount { get; }
            public int CrossingSampleCount { get; }
            public bool AllEntrySamplesBehindBodyBackSurface { get; }
            public float FirstFailedTime { get; }
            public bool FirstCrossingFound { get; }
            public float FirstEntryBoneZ { get; }
            public float FirstPointZ { get; }
            public float FirstBodyEntryZ { get; }
            public float FirstExitZ { get; }
        }

        private readonly struct Triangle
        {
            private readonly Vector3 a;
            private readonly Vector3 b;
            private readonly Vector3 c;
            public Triangle(Vector3 a, Vector3 b, Vector3 c) { this.a = a; this.b = b; this.c = c; }
            public Vector3 Center => (a + b + c) / 3f;
            public float ProjectedArea => Mathf.Abs(
                (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5f;
            public bool TryZ(float x, float y, out float z)
            {
                var denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
                if (Mathf.Abs(denominator) < 0.0000001f) { z = 0f; return false; }
                var u = ((b.y - c.y) * (x - c.x) + (c.x - b.x) * (y - c.y)) / denominator;
                var v = ((c.y - a.y) * (x - c.x) + (a.x - c.x) * (y - c.y)) / denominator;
                var w = 1f - u - v;
                if (u < -Tolerance || v < -Tolerance || w < -Tolerance) { z = 0f; return false; }
                z = u * a.z + v * b.z + w * c.z;
                return true;
            }

            public bool TryY(float x, float z, out float y)
            {
                var denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(denominator) < 0.0000001f) { y = 0f; return false; }
                var u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / denominator;
                var v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / denominator;
                var w = 1f - u - v;
                if (u < -Tolerance || v < -Tolerance || w < -Tolerance) { y = 0f; return false; }
                y = u * a.y + v * b.y + w * c.y;
                return true;
            }
        }

        private sealed class PoseSnapshot
        {
            private readonly TransformState[] transforms;
            private readonly RendererState[] renderers;
            private PoseSnapshot(TransformState[] transforms, RendererState[] renderers)
            { this.transforms = transforms; this.renderers = renderers; }
            public static PoseSnapshot Capture(Transform root) => new PoseSnapshot(
                root.GetComponentsInChildren<Transform>(true).Select(TransformState.Capture).ToArray(),
                root.GetComponentsInChildren<Renderer>(true).Select(RendererState.Capture).ToArray());
            public void Restore()
            {
                foreach (var state in transforms) state.Apply();
                foreach (var state in renderers) state.Apply();
            }
        }

        private readonly struct BoneState
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private BoneState(Transform transform) { position = transform.localPosition; rotation = transform.localRotation; scale = transform.localScale; }
            public static BoneState Capture(Transform transform) => new BoneState(transform);
            public void Apply(Transform transform) { transform.localPosition = position; transform.localRotation = rotation; transform.localScale = scale; }
        }

        private readonly struct TransformState
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private TransformState(Transform target) { this.target = target; position = target.localPosition; rotation = target.localRotation; scale = target.localScale; }
            public static TransformState Capture(Transform target) => new TransformState(target);
            public void Apply() { if (target != null) { target.localPosition = position; target.localRotation = rotation; target.localScale = scale; } }
        }

        private readonly struct RendererState
        {
            private readonly Renderer target;
            private readonly bool enabled;
            private readonly Material[] materials;
            private RendererState(Renderer target)
            {
                this.target = target;
                enabled = target.enabled;
                materials = target.sharedMaterials.ToArray();
            }
            public static RendererState Capture(Renderer target) => new RendererState(target);
            public void Apply()
            {
                if (target == null) return;
                target.sharedMaterials = materials;
                target.enabled = enabled;
            }
        }

        private readonly struct Metrics
        {
            public Metrics(float uniformScale, float standingWidthRatio, float standingHeightRatio,
                float lyingWidthRatio, float lyingHeightRatio, Bounds frameBounds, Bounds standingBounds,
                Bounds lyingBounds,
                Vector3 standingPosition, Vector3 lyingPosition, float pullLength, float pierceToPullTipError,
                bool pullStartLyingPenetrated, bool pullEndHidden, float portalPlaneZ, float pullScaleVariation,
                float pullStartMinimumZ, float pullEndMaximumZ, float partialOcclusionSampleTime,
                Vector4 portalClipPlaneWorld, int pulledTargetRenderQueue,
                float pointedTipFollowMaximumError, int pointedTipFollowSampleCount,
                int pointedTipVisibleImpaledSampleCount,
                bool pointedTipRemainsImpaledAtAllSamples,
                float pointedBaseMinimumFrontClearance,
                int pointedBaseVisibleSampleCount,
                bool pointedBaseExposedAtAllVisibleSamples,
                float pullStartXRotation, float portalEntryTime,
                float portalEntryXRotation, float postEntryMaximumAbsoluteXRotation,
                float tentaclePenetrationChainMaximumError, int tentaclePenetrationSampleCount,
                int tentaclePenetrationCrossingSampleCount,
                bool tentacleEntryBoneRemainsBehindBodyBackSurface,
                TransporterStruggleMetrics struggle,
                string standingSourceHash, string lyingSourceHash)
            {
                UniformScale = uniformScale; StandingWidthRatio = standingWidthRatio;
                StandingHeightRatio = standingHeightRatio; LyingWidthRatio = lyingWidthRatio;
                LyingHeightRatio = lyingHeightRatio; FrameBounds = frameBounds;
                StandingBounds = standingBounds; LyingBounds = lyingBounds;
                StandingPosition = standingPosition; LyingPosition = lyingPosition;
                PullLength = pullLength; PierceToPullTipError = pierceToPullTipError;
                PullStartLyingPenetrated = pullStartLyingPenetrated; PullEndHidden = pullEndHidden;
                PortalPlaneZ = portalPlaneZ; PullScaleVariation = pullScaleVariation;
                PullStartMinimumZ = pullStartMinimumZ; PullEndMaximumZ = pullEndMaximumZ;
                PartialOcclusionSampleTime = partialOcclusionSampleTime;
                PortalClipPlaneWorld = portalClipPlaneWorld;
                PulledTargetRenderQueue = pulledTargetRenderQueue;
                PointedTipFollowMaximumError = pointedTipFollowMaximumError;
                PointedTipFollowSampleCount = pointedTipFollowSampleCount;
                PointedTipVisibleImpaledSampleCount = pointedTipVisibleImpaledSampleCount;
                PointedTipRemainsImpaledAtAllSamples = pointedTipRemainsImpaledAtAllSamples;
                PointedBaseMinimumFrontClearance = pointedBaseMinimumFrontClearance;
                PointedBaseVisibleSampleCount = pointedBaseVisibleSampleCount;
                PointedBaseExposedAtAllVisibleSamples = pointedBaseExposedAtAllVisibleSamples;
                PullStartXRotation = pullStartXRotation;
                PortalEntryTime = portalEntryTime;
                PortalEntryXRotation = portalEntryXRotation;
                PostEntryMaximumAbsoluteXRotation = postEntryMaximumAbsoluteXRotation;
                TentaclePenetrationChainMaximumError = tentaclePenetrationChainMaximumError;
                TentaclePenetrationSampleCount = tentaclePenetrationSampleCount;
                TentaclePenetrationCrossingSampleCount = tentaclePenetrationCrossingSampleCount;
                TentacleEntryBoneRemainsBehindBodyBackSurface =
                    tentacleEntryBoneRemainsBehindBodyBackSurface;
                Struggle = struggle;
                StandingSourceHash = standingSourceHash; LyingSourceHash = lyingSourceHash;
            }
            public float UniformScale { get; }
            public float StandingWidthRatio { get; }
            public float StandingHeightRatio { get; }
            public float LyingWidthRatio { get; }
            public float LyingHeightRatio { get; }
            public Bounds FrameBounds { get; }
            public Bounds StandingBounds { get; }
            public Bounds LyingBounds { get; }
            public Vector3 StandingPosition { get; }
            public Vector3 LyingPosition { get; }
            public float PullLength { get; }
            public float PierceToPullTipError { get; }
            public bool PullStartLyingPenetrated { get; }
            public bool PullEndHidden { get; }
            public float PortalPlaneZ { get; }
            public float PullScaleVariation { get; }
            public float PullStartMinimumZ { get; }
            public float PullEndMaximumZ { get; }
            public float PartialOcclusionSampleTime { get; }
            public Vector4 PortalClipPlaneWorld { get; }
            public int PulledTargetRenderQueue { get; }
            public float PointedTipFollowMaximumError { get; }
            public int PointedTipFollowSampleCount { get; }
            public int PointedTipVisibleImpaledSampleCount { get; }
            public bool PointedTipRemainsImpaledAtAllSamples { get; }
            public float PointedBaseMinimumFrontClearance { get; }
            public int PointedBaseVisibleSampleCount { get; }
            public bool PointedBaseExposedAtAllVisibleSamples { get; }
            public float PullStartXRotation { get; }
            public float PortalEntryTime { get; }
            public float PortalEntryXRotation { get; }
            public float PostEntryMaximumAbsoluteXRotation { get; }
            public float TentaclePenetrationChainMaximumError { get; }
            public int TentaclePenetrationSampleCount { get; }
            public int TentaclePenetrationCrossingSampleCount { get; }
            public bool TentacleEntryBoneRemainsBehindBodyBackSurface { get; }
            public TransporterStruggleMetrics Struggle { get; }
            public string StandingSourceHash { get; }
            public string LyingSourceHash { get; }
        }
    }
}
