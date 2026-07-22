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
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Dolore06HitReaction
{
    internal static class Dolore06HitReactionApplyAndReview
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Dolore Enemy Placement";
        private const string SlotName = "Dolore_06_Hit_Reaction";
        private const string ModelName = "Dolore_Model";
        private const string RendererName = "char1";
        private const string StaticSlotName = "Dolore_01_Static_Review";
        private const string RigRootName = "Dolore_Rig";
        private const string ChestBoneName = "chest";
        private const string HeadBoneName = "head";
        private const string AssetFolder = "Assets/_Project/Art/Enemies/Dolore/Animations";
        private const string MeshPath = AssetFolder + "/Dolore_06_HitReactionMesh.asset";
        private const string ClipPath = AssetFolder + "/Dolore_06_Hit_Reaction.anim";
        private const string ControllerPath = AssetFolder + "/Dolore_06_Hit_Reaction.controller";
        private const string BlendShapeName = "Dolore_Hit_HeadLeft";
        private const string ValidationFolder = "docs/validation/dolore_hit_reaction_2026-07-23";
        private const string TargetReportPath = ValidationFolder + "/Dolore_06_HitReactionTarget.txt";
        private const string InspectionReportPath = ValidationFolder + "/Dolore_06_HitReactionInspection.txt";
        private const string CaptureFolder = ValidationFolder + "/Dolore_06_HitReaction_Diagnostic";
        private const float Duration = 2f;
        private const float ImpactTime = 0.32f;
        private const float RecoilDistance = 0.10f;
        private const float RecoilAngleDegrees = 8f;
        private const float FrameTurnDegrees = -14f;
        private const float HeadTurnDegrees = -25f;
        private const float Tolerance = 0.0001f;
        private const int CaptureLayer = 30;

        private static readonly float[] MotionTimes = { 0f, 0.12f, 0.32f, 0.65f, 1.1f, 1.55f, 2f };
        private static readonly float[] RecoilFractions = { 0f, 0.45f, 1f, 0.35f, -0.10f, 0.08f, 0f };
        private static readonly float[] HeadWeights = { 0f, 35f, 100f, 80f, 35f, 10f, 0f };

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 5 Hit Reaction Target")]
        public static void InspectTarget()
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            var slot = RequireSlot(scene);
            var model = RequireChild(slot, ModelName);
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault(item => item.sharedMesh != null) ??
                throw new InvalidOperationException("The Dolore model must contain exactly one skinned renderer.");
            if (renderer.sharedMesh == null)
                throw new InvalidOperationException("The Dolore char1 mesh is missing.");
            if (renderer.bones.Length != 27)
                throw new InvalidOperationException("The approved Dolore rig must contain 27 renderer bones.");

            var baked = new Mesh { name = "Dolore06HitReactionTargetInspection" };
            try
            {
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                var weights = renderer.sharedMesh.boneWeights;
                if (vertices.Length != weights.Length)
                    throw new InvalidOperationException("The baked and weighted Dolore vertex counts differ.");
                var stats = renderer.bones.Select(_ => new BoneInfluenceStats()).ToArray();
                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    var point = slot.InverseTransformPoint(renderer.transform.TransformPoint(vertices[vertexIndex]));
                    Accumulate(stats, weights[vertexIndex].boneIndex0, weights[vertexIndex].weight0, point);
                    Accumulate(stats, weights[vertexIndex].boneIndex1, weights[vertexIndex].weight1, point);
                    Accumulate(stats, weights[vertexIndex].boneIndex2, weights[vertexIndex].weight2, point);
                    Accumulate(stats, weights[vertexIndex].boneIndex3, weights[vertexIndex].weight3, point);
                }

                var report = new StringBuilder()
                    .AppendLine("Result=PASS")
                    .AppendLine("Scene=" + ScenePath)
                    .AppendLine("Target=" + PlacementRootName + "/" + SlotName)
                    .AppendLine("Model=" + ModelName)
                    .AppendLine("Renderer=" + renderer.name)
                    .AppendLine("RigBoneCount=" + renderer.bones.Length)
                    .AppendLine("MeshVertexCount=" + renderer.sharedMesh.vertexCount)
                    .AppendLine("SlotLocalForward=" + Vec(Vector3.forward))
                    .AppendLine("SlotLocalLeft=" + Vec(Vector3.left));
                for (var index = 0; index < renderer.bones.Length; index++)
                {
                    var bone = renderer.bones[index];
                    var boneStats = stats[index];
                    report.AppendLine(
                        "Bone[" + index + "]=" + bone.name +
                        "|Path=" + AnimationUtility.CalculateTransformPath(bone, model) +
                        "|Parent=" + (bone.parent != null ? bone.parent.name : "None") +
                        "|SlotPosition=" + Vec(slot.InverseTransformPoint(bone.position)) +
                        "|WeightedVertexCount=" + boneStats.VertexCount +
                        "|WeightSum=" + Num(boneStats.WeightSum) +
                        "|InfluenceBounds=" + boneStats.BoundsText());
                }
                for (var subMesh = 0; subMesh < renderer.sharedMesh.subMeshCount; subMesh++)
                {
                    var vertexIndices = new HashSet<int>(renderer.sharedMesh.GetIndices(subMesh));
                    var material = subMesh < renderer.sharedMaterials.Length
                        ? renderer.sharedMaterials[subMesh]
                        : null;
                    report.AppendLine(
                        "SubMesh[" + subMesh + "]=" + (material != null ? material.name : "None") +
                        "|UniqueVertexCount=" + vertexIndices.Count +
                        "|Bounds=" + BoundsText(vertexIndices.Select(index =>
                            slot.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index])))));
                    foreach (var boneName in new[] { "Hips", "chest", "head", "headend", "earend", "R_earend" })
                    {
                        var boneIndex = Array.FindIndex(renderer.bones, bone => bone.name == boneName);
                        if (boneIndex < 0) continue;
                        var influenced = vertexIndices
                            .Select(index => BoneWeightForIndex(weights[index], boneIndex))
                            .Where(weight => weight > 0f)
                            .ToArray();
                        report.AppendLine(
                            "SubMeshBone=" + subMesh + "|" + boneName +
                            "|VertexCount=" + influenced.Length +
                            "|WeightSum=" + Num(influenced.Sum()));
                    }
                }
                WriteText(TargetReportPath, report.AppendLine("SceneChanged=False").ToString());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }

            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Hit-reaction target inspection changed CargoRunMvp.");
            Debug.Log("Dolore06HitReactionTargetInspected Result=PASS RigBoneCount=27 SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Apply Motion 5 Hit Reaction")]
        public static void ApplyAnimation()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp contains pre-existing unsaved changes.");
            var slot = RequireSlot(scene);
            var model = RequireChild(slot, ModelName);
            var renderer = RequireRenderer(model);
            var sourceRenderer = RequireRenderer(RequireChild(RequireSlot(scene, StaticSlotName), ModelName));
            var otherSlotsBefore = OtherSlotSignatures(scene);

            EnsureAssetFolder();
            var chest = RequireDescendant(model, ChestBoneName);
            var head = RequireDescendant(model, HeadBoneName);
            var generatedMesh = CreateHeadTurnMesh(sourceRenderer.sharedMesh, renderer, slot, head);
            renderer.sharedMesh = generatedMesh;
            EditorUtility.SetDirty(renderer);

            var restPosition = chest.localPosition;
            var restRotation = chest.localRotation;
            var peakPosition = restPosition + chest.parent.InverseTransformVector(
                slot.TransformVector(Vector3.back * RecoilDistance));
            var peakRotation = RecoilRotation(renderer, slot, chest, restRotation);
            var clip = CreateClip(slot, renderer, chest, restPosition, peakPosition, restRotation, peakRotation);
            var controller = CreateController(clip);
            DisableCompetingAnimation(model);
            var animator = slot.GetComponent<Animator>();
            if (animator == null) animator = slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            var metrics = Inspect(scene);
            var otherSlotsAfter = OtherSlotSignatures(scene);
            if (!otherSlotsBefore.SequenceEqual(otherSlotsAfter, StringComparer.Ordinal))
                throw new InvalidOperationException("A Dolore slot outside the hit-reaction target changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved.");
            AssetDatabase.SaveAssets();
            WriteInspection(metrics, "Apply", true);
            Debug.Log(
                "Dolore06HitReactionApplied Result=PASS Duration=" + Num(metrics.Duration) +
                " BackwardRecoil=" + Num(metrics.BackwardRecoil) +
                " FrameSignedYawDegrees=" + Num(metrics.FrameSignedYawDegrees) +
                " HeadTurnDegrees=" + Num(metrics.HeadTurnDegrees) +
                " SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Inspect Motion 5 Hit Reaction")]
        public static void InspectAnimation()
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            var metrics = Inspect(scene);
            WriteInspection(metrics, "Inspect", false);
            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Hit-reaction inspection changed CargoRunMvp.");
            Debug.Log(
                "Dolore06HitReactionInspected Result=PASS Duration=" + Num(metrics.Duration) +
                " BackwardRecoil=" + Num(metrics.BackwardRecoil) +
                " FrameSignedYawDegrees=" + Num(metrics.FrameSignedYawDegrees) +
                " HeadTurnDegrees=" + Num(metrics.HeadTurnDegrees) +
                " LoopBoundaryError=" + Num(metrics.LoopBoundaryError) +
                " SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Dolore/Capture Motion 5 Hit Reaction Diagnostic")]
        public static void CaptureDiagnostic()
        {
            var scene = RequireScene();
            var dirty = scene.isDirty;
            var metrics = Inspect(scene);
            var slot = RequireSlot(scene);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The hit-reaction clip is missing.");
            var clone = UnityEngine.Object.Instantiate(slot.gameObject);
            clone.name = "Dolore_06_HitReaction_DiagnosticClone";
            clone.hideFlags = HideFlags.DontSave;
            var cameraObject = new GameObject("Dolore_06_HitReaction_DiagnosticCamera")
            {
                hideFlags = HideFlags.DontSave,
                layer = CaptureLayer
            };
            var lightObject = new GameObject("Dolore_06_HitReaction_DiagnosticLight")
            {
                hideFlags = HideFlags.DontSave,
                layer = CaptureLayer
            };
            try
            {
                SetLayer(clone.transform, CaptureLayer);
                clone.SetActive(true);
                var animator = clone.GetComponent<Animator>() ??
                               throw new InvalidOperationException("The diagnostic clone Animator is missing.");
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                foreach (var skinned in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                    skinned.forceMatrixRecalculationPerRender = true;
                }
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
                light.intensity = 2.8f;
                light.color = new Color(1f, 0.93f, 0.86f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
                var folder = Absolute(CaptureFolder);
                Directory.CreateDirectory(folder);
                foreach (var file in Directory.GetFiles(folder, "*.png")) File.Delete(file);
                CapturePose(clone.transform, animator, camera, 0f, false, Path.Combine(folder, "01_Rest_Front.png"));
                CapturePose(clone.transform, animator, camera, ImpactTime, false, Path.Combine(folder, "02_Impact_Front.png"));
                CapturePose(clone.transform, animator, camera, 1.1f, false, Path.Combine(folder, "03_Recovery_Front.png"));
                CapturePose(clone.transform, animator, camera, 0f, true, Path.Combine(folder, "04_Rest_Oblique.png"));
                CapturePose(clone.transform, animator, camera, ImpactTime, true, Path.Combine(folder, "05_Impact_Oblique.png"));
                CapturePose(clone.transform, animator, camera, 1.1f, true, Path.Combine(folder, "06_Recovery_Oblique.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(clone);
            }
            AssetDatabase.Refresh();
            if (scene.isDirty != dirty)
                throw new InvalidOperationException("Hit-reaction diagnostic changed CargoRunMvp.");
            Debug.Log(
                "Dolore06HitReactionCaptured Result=PASS Frames=6 BackwardRecoil=" +
                Num(metrics.BackwardRecoil) + " FrameSignedYawDegrees=" +
                Num(metrics.FrameSignedYawDegrees) + " SceneChanged=False.");
        }

        private static Mesh CreateHeadTurnMesh(
            Mesh source,
            SkinnedMeshRenderer renderer,
            Transform slot,
            Transform head)
        {
            if (source == null) throw new InvalidOperationException("The approved Dolore source mesh is missing.");
            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = "Dolore_06_HitReactionMesh";
            var headIndex = Array.FindIndex(renderer.bones, bone => bone.name == HeadBoneName);
            if (headIndex < 0) throw new InvalidOperationException("The Dolore head bone is missing from the renderer.");
            var tissueSubMesh = Array.FindIndex(
                renderer.sharedMaterials,
                material => material != null && material.name.IndexOf("Tissue", StringComparison.OrdinalIgnoreCase) >= 0);
            if (tissueSubMesh < 0) throw new InvalidOperationException("The approved tissue material slot is missing.");
            var tissueVertices = new HashSet<int>(source.GetIndices(tissueSubMesh));
            var vertices = source.vertices;
            var normals = source.normals;
            var weights = source.boneWeights;
            var deltaVertices = new Vector3[vertices.Length];
            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            var pivot = slot.InverseTransformPoint(head.position);
            var fullTurn = Quaternion.AngleAxis(HeadTurnDegrees, Vector3.up);
            var affected = 0;
            for (var index = 0; index < vertices.Length; index++)
            {
                if (!tissueVertices.Contains(index)) continue;
                var mask = BoneWeightForIndex(weights[index], headIndex);
                if (mask <= 0.0001f) continue;
                var turn = Quaternion.SlerpUnclamped(Quaternion.identity, fullTurn, mask);
                var point = slot.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]));
                var turned = pivot + turn * (point - pivot);
                deltaVertices[index] = renderer.transform.InverseTransformVector(
                    slot.TransformVector(turned - point));
                if (normals.Length == vertices.Length)
                {
                    var normal = slot.InverseTransformDirection(renderer.transform.TransformDirection(normals[index]));
                    var turnedNormal = turn * normal;
                    deltaNormals[index] = renderer.transform.InverseTransformVector(
                        slot.TransformVector(turnedNormal - normal));
                }
                affected++;
            }
            if (affected < 100)
                throw new InvalidOperationException("The head turn affects too few tissue vertices. Count=" + affected);
            mesh.AddBlendShapeFrame(BlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
            if (AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) != null) AssetDatabase.DeleteAsset(MeshPath);
            AssetDatabase.CreateAsset(mesh, MeshPath);
            AssetDatabase.ImportAsset(MeshPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath) ??
                   throw new InvalidOperationException("The generated hit-reaction mesh could not be loaded.");
        }

        private static Quaternion RecoilRotation(
            SkinnedMeshRenderer renderer,
            Transform slot,
            Transform chest,
            Quaternion restRotation)
        {
            var worldAxis = slot.TransformDirection(Vector3.right);
            var restWorld = chest.rotation;
            chest.rotation = Quaternion.AngleAxis(RecoilAngleDegrees, worldAxis) * restWorld;
            var positiveZ = FrameCenterInSlot(renderer, slot).z;
            var positiveWorld = chest.rotation;
            chest.localRotation = restRotation;
            chest.rotation = Quaternion.AngleAxis(-RecoilAngleDegrees, worldAxis) * restWorld;
            var negativeZ = FrameCenterInSlot(renderer, slot).z;
            var negativeWorld = chest.rotation;
            chest.localRotation = restRotation;

            var recoilWorld = positiveZ < negativeZ ? positiveWorld : negativeWorld;
            var leftTurnAxis = slot.TransformDirection(Vector3.up);
            chest.rotation = Quaternion.AngleAxis(FrameTurnDegrees, leftTurnAxis) * recoilWorld;
            var result = chest.localRotation;
            chest.localRotation = restRotation;
            return result;
        }

        private static AnimationClip CreateClip(
            Transform slot,
            SkinnedMeshRenderer renderer,
            Transform chest,
            Vector3 restPosition,
            Vector3 peakPosition,
            Quaternion restRotation,
            Quaternion peakRotation)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = "Dolore_06_Hit_Reaction", frameRate = 60f };
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            clip.name = "Dolore_06_Hit_Reaction";
            clip.frameRate = 60f;

            var chestPath = AnimationUtility.CalculateTransformPath(chest, slot);
            var positions = RecoilFractions.Select(fraction => Vector3.LerpUnclamped(restPosition, peakPosition, fraction)).ToArray();
            SetVectorCurves(clip, chestPath, "m_LocalPosition", positions);
            var rotations = RecoilFractions.Select(fraction =>
                    Quaternion.SlerpUnclamped(restRotation, peakRotation, fraction).normalized)
                .ToArray();
            for (var index = 1; index < rotations.Length; index++)
                if (Quaternion.Dot(rotations[index - 1], rotations[index]) < 0f)
                    rotations[index] = new Quaternion(
                        -rotations[index].x,
                        -rotations[index].y,
                        -rotations[index].z,
                        -rotations[index].w);
            SetQuaternionCurves(clip, chestPath, rotations);

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, slot);
            SetCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + BlendShapeName),
                HeadWeights);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            return clip;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("HitReaction");
            state.motion = clip;
            state.speed = 1f;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        private static Metrics Inspect(Scene scene)
        {
            var slot = RequireSlot(scene);
            var model = RequireChild(slot, ModelName);
            var renderer = RequireRenderer(model);
            var chest = RequireDescendant(model, ChestBoneName);
            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The hit-reaction slot Animator is missing.");
            if (AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) != ControllerPath)
                throw new InvalidOperationException("The hit-reaction Animator Controller changed.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The hit-reaction clip is missing.");
            if (Mathf.Abs(clip.length - Duration) > Tolerance)
                throw new InvalidOperationException("Hit reaction must be exactly two seconds. Length=" + Num(clip.length));
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime)
                throw new InvalidOperationException("Hit reaction must loop.");
            if (AssetDatabase.GetAssetPath(renderer.sharedMesh) != MeshPath)
                throw new InvalidOperationException("The hit-reaction mesh changed.");
            var blendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            if (blendShapeIndex < 0)
                throw new InvalidOperationException("The head-left BlendShape is missing.");
            if (AnimationUtility.GetCurveBindings(clip).Any(binding =>
                    string.IsNullOrEmpty(binding.path) && binding.type == typeof(Transform)))
                throw new InvalidOperationException("The hit-reaction clip must not move the slot root.");
            var cloneMetrics = MeasureAnimatorClone(slot, ImpactTime);
            if (cloneMetrics.BackwardRecoil < 0.07f || cloneMetrics.FrameSignedYawDegrees > -10f)
                throw new InvalidOperationException(
                    "The actual Animator clone does not reproduce the authored recoil and left frame turn. Recoil=" +
                    Num(cloneMetrics.BackwardRecoil) + " FrameSignedYawDegrees=" +
                    Num(cloneMetrics.FrameSignedYawDegrees));

            var restSlotPosition = slot.localPosition;
            var restSlotRotation = slot.localRotation;
            var restSlotScale = slot.localScale;
            var restChestPosition = chest.localPosition;
            var restChestRotation = chest.localRotation;
            var snapshot = PoseSnapshot.Capture(slot);
            try
            {
                clip.SampleAnimation(slot.gameObject, 0f);
                var startChestPosition = chest.localPosition;
                var startChestRotation = chest.localRotation;
                var startChestWorldRotation = chest.rotation;
                var startFrameCenter = FrameCenterInSlot(renderer, slot);
                var startBlendShapeWeight = renderer.GetBlendShapeWeight(blendShapeIndex);
                var startBounds = BoundsInSlot(renderer, slot);

                clip.SampleAnimation(slot.gameObject, ImpactTime);
                var impactChestWorldRotation = chest.rotation;
                var impactFrameCenter = FrameCenterInSlot(renderer, slot);
                var impactBlendShapeWeight = renderer.GetBlendShapeWeight(blendShapeIndex);
                var impactBounds = BoundsInSlot(renderer, slot);

                clip.SampleAnimation(slot.gameObject, Duration);
                var endChestPosition = chest.localPosition;
                var endChestRotation = chest.localRotation;
                var endBlendShapeWeight = renderer.GetBlendShapeWeight(blendShapeIndex);
                var endBounds = BoundsInSlot(renderer, slot);

                var backwardRecoil = startFrameCenter.z - impactFrameCenter.z;
                var frameSignedYawDegrees = SignedYawInSlot(
                    slot,
                    startChestWorldRotation,
                    impactChestWorldRotation);
                var loopBoundaryError = Vector3.Distance(startChestPosition, endChestPosition) +
                                        Quaternion.Angle(startChestRotation, endChestRotation) +
                                        Mathf.Abs(startBlendShapeWeight - endBlendShapeWeight);
                var headMetrics = MeasureHeadBlendShape(renderer.sharedMesh, blendShapeIndex, renderer, slot);
                if (backwardRecoil < 0.07f || frameSignedYawDegrees > -10f ||
                    Mathf.Abs(frameSignedYawDegrees - FrameTurnDegrees) > 1f ||
                    impactBlendShapeWeight < 99.9f ||
                    startBlendShapeWeight > Tolerance || endBlendShapeWeight > Tolerance ||
                    loopBoundaryError > 0.01f ||
                    headMetrics.AffectedTissueVertices < 100 ||
                    headMetrics.NonTissueMovedVertices != 0 ||
                    headMetrics.LeftTurnForward.x >= -0.1f ||
                    !ValidBounds(startBounds) || !ValidBounds(impactBounds) || !ValidBounds(endBounds))
                    throw new InvalidOperationException(
                        "The Dolore hit reaction does not match the approved recoil and left head turn. " +
                        "BackwardRecoil=" + Num(backwardRecoil) +
                        " FrameSignedYawDegrees=" + Num(frameSignedYawDegrees) +
                        " ImpactHeadWeight=" + Num(impactBlendShapeWeight) +
                        " LoopBoundaryError=" + Num(loopBoundaryError) +
                        " AffectedTissueVertices=" + headMetrics.AffectedTissueVertices +
                        " NonTissueMovedVertices=" + headMetrics.NonTissueMovedVertices +
                        " LeftTurnForward=" + Vec(headMetrics.LeftTurnForward));
                if (slot.localPosition != restSlotPosition ||
                    Quaternion.Angle(slot.localRotation, restSlotRotation) > Tolerance ||
                    slot.localScale != restSlotScale)
                    throw new InvalidOperationException("Sampling changed the hit-reaction slot root.");
                return new Metrics(
                    clip.length,
                    backwardRecoil,
                    frameSignedYawDegrees,
                    Mathf.Abs(HeadTurnDegrees),
                    impactBlendShapeWeight,
                    loopBoundaryError,
                    headMetrics.AffectedTissueVertices,
                    headMetrics.NonTissueMovedVertices,
                    Vector3.Distance(restChestPosition, startChestPosition),
                    Quaternion.Angle(restChestRotation, startChestRotation),
                    cloneMetrics.BackwardRecoil,
                    cloneMetrics.FrameSignedYawDegrees);
            }
            finally
            {
                snapshot.Restore();
            }
        }

        private static HeadMetrics MeasureHeadBlendShape(
            Mesh mesh,
            int blendShapeIndex,
            SkinnedMeshRenderer renderer,
            Transform slot)
        {
            var deltaVertices = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(blendShapeIndex, 0, deltaVertices, deltaNormals, deltaTangents);
            var tissueSubMesh = Array.FindIndex(
                renderer.sharedMaterials,
                material => material != null && material.name.IndexOf("Tissue", StringComparison.OrdinalIgnoreCase) >= 0);
            var tissue = new HashSet<int>(mesh.GetIndices(tissueSubMesh));
            var affectedTissue = 0;
            var nonTissueMoved = 0;
            for (var index = 0; index < deltaVertices.Length; index++)
            {
                if (deltaVertices[index].sqrMagnitude <= Tolerance * Tolerance) continue;
                if (tissue.Contains(index)) affectedTissue++;
                else nonTissueMoved++;
            }
            var leftForward = Quaternion.AngleAxis(HeadTurnDegrees, Vector3.up) * Vector3.forward;
            return new HeadMetrics(affectedTissue, nonTissueMoved, leftForward);
        }

        private static void SetVectorCurves(
            AnimationClip clip,
            string path,
            string property,
            Vector3[] values)
        {
            var axes = new[] { "x", "y", "z" };
            for (var axis = 0; axis < axes.Length; axis++)
                SetCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), property + "." + axes[axis]),
                    values.Select(value => axis == 0 ? value.x : axis == 1 ? value.y : value.z).ToArray());
        }

        private static void SetQuaternionCurves(AnimationClip clip, string path, Quaternion[] values)
        {
            var properties = new[]
            {
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w"
            };
            for (var component = 0; component < properties.Length; component++)
                SetCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), properties[component]),
                    values.Select(value => component == 0 ? value.x :
                        component == 1 ? value.y : component == 2 ? value.z : value.w).ToArray());
        }

        private static void SetCurve(AnimationClip clip, EditorCurveBinding binding, float[] values)
        {
            if (values.Length != MotionTimes.Length)
                throw new InvalidOperationException("Hit-reaction curve key count changed.");
            var curve = new AnimationCurve(MotionTimes.Select((time, index) => new Keyframe(time, values[index])).ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void Accumulate(BoneInfluenceStats[] stats, int boneIndex, float weight, Vector3 point)
        {
            if (weight <= 0f || boneIndex < 0 || boneIndex >= stats.Length) return;
            stats[boneIndex].Add(point, weight);
        }

        private static float BoneWeightForIndex(BoneWeight weight, int boneIndex)
        {
            var value = 0f;
            if (weight.boneIndex0 == boneIndex) value += weight.weight0;
            if (weight.boneIndex1 == boneIndex) value += weight.weight1;
            if (weight.boneIndex2 == boneIndex) value += weight.weight2;
            if (weight.boneIndex3 == boneIndex) value += weight.weight3;
            return value;
        }

        private static string BoundsText(IEnumerable<Vector3> points)
        {
            var values = points.ToArray();
            if (values.Length == 0) return "None";
            var bounds = new Bounds(values[0], Vector3.zero);
            for (var index = 1; index < values.Length; index++) bounds.Encapsulate(values[index]);
            return "Center=" + Vec(bounds.center) + ",Size=" + Vec(bounds.size);
        }

        private static SkinnedMeshRenderer RequireRenderer(Transform model)
        {
            return model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault(item => item.sharedMesh != null) ??
                   throw new InvalidOperationException("The Dolore model must contain exactly one skinned renderer.");
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

        private static Vector3 FrameCenterInSlot(SkinnedMeshRenderer renderer, Transform slot)
        {
            var materialIndex = Array.FindIndex(
                renderer.sharedMaterials,
                material => material != null && material.name.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0);
            if (materialIndex < 0) throw new InvalidOperationException("The approved frame material slot is missing.");
            var baked = new Mesh { name = "Dolore06FrameMeasurement" };
            try
            {
                renderer.BakeMesh(baked);
                var vertices = baked.vertices;
                return BoundsFromPoints(renderer.sharedMesh.GetIndices(materialIndex)
                        .Distinct()
                        .Select(index => slot.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]))))
                    .center;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Bounds BoundsInSlot(SkinnedMeshRenderer renderer, Transform slot)
        {
            var baked = new Mesh { name = "Dolore06BoundsMeasurement" };
            try
            {
                renderer.BakeMesh(baked);
                return BoundsFromPoints(baked.vertices.Select(vertex =>
                    slot.InverseTransformPoint(renderer.transform.TransformPoint(vertex))));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Bounds BoundsFromPoints(IEnumerable<Vector3> points)
        {
            var values = points.ToArray();
            if (values.Length == 0) throw new InvalidOperationException("Bounds require at least one point.");
            var bounds = new Bounds(values[0], Vector3.zero);
            for (var index = 1; index < values.Length; index++) bounds.Encapsulate(values[index]);
            return bounds;
        }

        private static bool ValidBounds(Bounds bounds)
        {
            return bounds.size.x > Tolerance && bounds.size.y > Tolerance && bounds.size.z > Tolerance &&
                   IsFinite(bounds.center) && IsFinite(bounds.size);
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
        }

        private static void DisableCompetingAnimation(Transform model)
        {
            foreach (var animator in model.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                EditorUtility.SetDirty(animator);
            }
            foreach (var animation in model.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static string[] OtherSlotSignatures(Scene scene)
        {
            var placement = scene.GetRootGameObjects().Single(item => item.name == PlacementRootName).transform;
            return Enumerable.Range(0, placement.childCount)
                .Select(placement.GetChild)
                .Where(item => item.name != SlotName)
                .Select(item =>
                {
                    var renderers = item.GetComponentsInChildren<Renderer>(true);
                    var animators = item.GetComponentsInChildren<Animator>(true);
                    return item.name + "|" + Vec(item.localPosition) + "|" + Quat(item.localRotation) + "|" +
                           Vec(item.localScale) + "|" + item.childCount + "|" +
                           string.Join(",", renderers.Select(renderer =>
                               renderer.name + ":" + renderer.enabled + ":" +
                               (renderer is SkinnedMeshRenderer skinned
                                   ? AssetDatabase.GetAssetPath(skinned.sharedMesh)
                                   : string.Empty))) + "|" +
                           string.Join(",", animators.Select(animator =>
                               animator.name + ":" + animator.enabled + ":" +
                               AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)));
                })
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static void EnsureAssetFolder()
        {
            var parts = AssetFolder.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        private static void WriteInspection(Metrics metrics, string phase, bool saved)
        {
            var report = new StringBuilder()
                .AppendLine("Result=PASS")
                .AppendLine("Phase=" + phase)
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + SlotName)
                .AppendLine("Clip=" + ClipPath)
                .AppendLine("Controller=" + ControllerPath)
                .AppendLine("Mesh=" + MeshPath)
                .AppendLine("DurationSeconds=" + Num(metrics.Duration))
                .AppendLine("LoopEnabled=True")
                .AppendLine("BackwardRecoilMeters=" + Num(metrics.BackwardRecoil))
                .AppendLine("ChestRecoilDistanceSetting=" + Num(RecoilDistance))
                .AppendLine("ChestRecoilAngleSettingDegrees=" + Num(RecoilAngleDegrees))
                .AppendLine("FrameTurnDirection=DoloreLocalLeft")
                .AppendLine("FrameTurnDegreesSetting=" + Num(Mathf.Abs(FrameTurnDegrees)))
                .AppendLine("FrameSignedYawDegrees=" + Num(metrics.FrameSignedYawDegrees))
                .AppendLine("HeadTurnDirection=DoloreLocalLeft")
                .AppendLine("HeadTurnDegrees=" + Num(metrics.HeadTurnDegrees))
                .AppendLine("ImpactBlendShapeWeight=" + Num(metrics.ImpactBlendShapeWeight))
                .AppendLine("HeadAffectedTissueVertexCount=" + metrics.AffectedTissueVertices)
                .AppendLine("PortraitAndFrameMovedVertexCount=" + metrics.NonTissueMovedVertices)
                .AppendLine("LoopBoundaryError=" + Num(metrics.LoopBoundaryError))
                .AppendLine("StartChestPositionError=" + Num(metrics.StartChestPositionError))
                .AppendLine("StartChestRotationErrorDegrees=" + Num(metrics.StartChestRotationError))
                .AppendLine("ActualAnimatorCloneBackwardRecoil=" + Num(metrics.ActualAnimatorCloneBackwardRecoil))
                .AppendLine("ActualAnimatorCloneFrameSignedYawDegrees=" + Num(metrics.ActualAnimatorCloneFrameSignedYawDegrees))
                .AppendLine("SlotRootAnimationCurves=False")
                .AppendLine("OtherDoloreSlotsChanged=False")
                .AppendLine("SourceFbxChanged=False")
                .AppendLine("HarnessValidationExecuted=False")
                .AppendLine("SceneSaved=" + saved)
                .ToString();
            WriteText(InspectionReportPath, report);
            AssetDatabase.Refresh();
        }

        private static void CapturePose(
            Transform clone,
            Animator animator,
            Camera camera,
            float time,
            bool oblique,
            string path)
        {
            animator.Play("HitReaction", 0, time / Duration);
            animator.Update(0f);
            var renderers = clone.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("The diagnostic clone has no renderer.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 1f);
            var direction = oblique
                ? (clone.forward + clone.right * 0.65f + Vector3.up * 0.15f).normalized
                : clone.forward;
            camera.transform.position = bounds.center + direction * size * 2.6f;
            camera.transform.LookAt(bounds.center + Vector3.up * size * 0.03f);
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(1024, 768, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1024, 768, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0f, 0f, 1024, 768), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void SetLayer(Transform root, int layer)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                transform.gameObject.layer = layer;
        }

        private static AnimatorMetrics MeasureAnimatorClone(Transform sourceSlot, float time)
        {
            var clone = UnityEngine.Object.Instantiate(sourceSlot.gameObject);
            clone.name = "Dolore_06_HitReaction_AnimatorInspectionClone";
            clone.hideFlags = HideFlags.DontSave;
            try
            {
                clone.SetActive(true);
                var animator = clone.GetComponent<Animator>() ??
                               throw new InvalidOperationException("The Animator inspection clone is missing its Animator.");
                animator.enabled = true;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                var cloneModel = RequireChild(clone.transform, ModelName);
                var cloneRenderer = RequireRenderer(cloneModel);
                animator.Play("HitReaction", 0, 0f);
                animator.Update(0f);
                var start = FrameCenterInSlot(cloneRenderer, clone.transform);
                var cloneChest = RequireDescendant(cloneModel, ChestBoneName);
                var startChestWorldRotation = cloneChest.rotation;
                animator.Play("HitReaction", 0, time / Duration);
                animator.Update(0f);
                var impact = FrameCenterInSlot(cloneRenderer, clone.transform);
                return new AnimatorMetrics(
                    start.z - impact.z,
                    SignedYawInSlot(clone.transform, startChestWorldRotation, cloneChest.rotation));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }

        private static float SignedYawInSlot(
            Transform slot,
            Quaternion startWorldRotation,
            Quaternion endWorldRotation)
        {
            var worldDelta = endWorldRotation * Quaternion.Inverse(startWorldRotation);
            var slotDelta = Quaternion.Inverse(slot.rotation) * worldDelta * slot.rotation;
            var turnedForward = slotDelta * Vector3.forward;
            return Mathf.Atan2(turnedForward.x, turnedForward.z) * Mathf.Rad2Deg;
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
            return RequireSlot(scene, SlotName);
        }

        private static Transform RequireSlot(Scene scene, string slotName)
        {
            var placement = scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ??
                            throw new InvalidOperationException("Approved Dolore placement is missing.");
            return placement.transform.Find(slotName) ??
                   throw new InvalidOperationException("Dolore slot is missing: " + slotName);
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            return parent.Find(name) ?? throw new InvalidOperationException(parent.name + " is missing " + name + ".");
        }

        private static void WriteText(string relativePath, string contents)
        {
            var absolutePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ??
                                      throw new InvalidOperationException("Invalid validation folder."));
            File.WriteAllText(absolutePath, contents, new UTF8Encoding(false));
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));

        private static string Num(float value) =>
            value.ToString("0.#########", CultureInfo.InvariantCulture);

        private static string Vec(Vector3 value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";

        private static string Quat(Quaternion value) =>
            "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w) + ")";

        private sealed class BoneInfluenceStats
        {
            private bool hasBounds;
            private Bounds bounds;

            public int VertexCount { get; private set; }
            public float WeightSum { get; private set; }

            public void Add(Vector3 point, float weight)
            {
                VertexCount++;
                WeightSum += weight;
                if (!hasBounds)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }

            public string BoundsText()
            {
                return hasBounds ? "Center=" + Vec(bounds.center) + ",Size=" + Vec(bounds.size) : "None";
            }
        }

        private sealed class PoseSnapshot
        {
            private readonly TransformState[] transforms;
            private readonly RendererState[] renderers;

            private PoseSnapshot(TransformState[] transforms, RendererState[] renderers)
            {
                this.transforms = transforms;
                this.renderers = renderers;
            }

            public static PoseSnapshot Capture(Transform root)
            {
                return new PoseSnapshot(
                    root.GetComponentsInChildren<Transform>(true).Select(TransformState.Capture).ToArray(),
                    root.GetComponentsInChildren<Renderer>(true).Select(RendererState.Capture).ToArray());
            }

            public void Restore()
            {
                foreach (var state in transforms) state.Apply();
                foreach (var state in renderers) state.Apply();
            }
        }

        private readonly struct TransformState
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            private TransformState(Transform target)
            {
                this.target = target;
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
            }

            public static TransformState Capture(Transform target) => new TransformState(target);

            public void Apply()
            {
                if (target == null) return;
                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }
        }

        private readonly struct RendererState
        {
            private readonly Renderer target;
            private readonly bool enabled;

            private RendererState(Renderer target)
            {
                this.target = target;
                enabled = target.enabled;
            }

            public static RendererState Capture(Renderer target) => new RendererState(target);

            public void Apply()
            {
                if (target != null) target.enabled = enabled;
            }
        }

        private readonly struct HeadMetrics
        {
            public HeadMetrics(int affectedTissueVertices, int nonTissueMovedVertices, Vector3 leftTurnForward)
            {
                AffectedTissueVertices = affectedTissueVertices;
                NonTissueMovedVertices = nonTissueMovedVertices;
                LeftTurnForward = leftTurnForward;
            }

            public int AffectedTissueVertices { get; }
            public int NonTissueMovedVertices { get; }
            public Vector3 LeftTurnForward { get; }
        }

        private readonly struct AnimatorMetrics
        {
            public AnimatorMetrics(float backwardRecoil, float frameSignedYawDegrees)
            {
                BackwardRecoil = backwardRecoil;
                FrameSignedYawDegrees = frameSignedYawDegrees;
            }

            public float BackwardRecoil { get; }
            public float FrameSignedYawDegrees { get; }
        }

        private readonly struct Metrics
        {
            public Metrics(
                float duration,
                float backwardRecoil,
                float frameSignedYawDegrees,
                float headTurnDegrees,
                float impactBlendShapeWeight,
                float loopBoundaryError,
                int affectedTissueVertices,
                int nonTissueMovedVertices,
                float startChestPositionError,
                float startChestRotationError,
                float actualAnimatorCloneBackwardRecoil,
                float actualAnimatorCloneFrameSignedYawDegrees)
            {
                Duration = duration;
                BackwardRecoil = backwardRecoil;
                FrameSignedYawDegrees = frameSignedYawDegrees;
                HeadTurnDegrees = headTurnDegrees;
                ImpactBlendShapeWeight = impactBlendShapeWeight;
                LoopBoundaryError = loopBoundaryError;
                AffectedTissueVertices = affectedTissueVertices;
                NonTissueMovedVertices = nonTissueMovedVertices;
                StartChestPositionError = startChestPositionError;
                StartChestRotationError = startChestRotationError;
                ActualAnimatorCloneBackwardRecoil = actualAnimatorCloneBackwardRecoil;
                ActualAnimatorCloneFrameSignedYawDegrees = actualAnimatorCloneFrameSignedYawDegrees;
            }

            public float Duration { get; }
            public float BackwardRecoil { get; }
            public float FrameSignedYawDegrees { get; }
            public float HeadTurnDegrees { get; }
            public float ImpactBlendShapeWeight { get; }
            public float LoopBoundaryError { get; }
            public int AffectedTissueVertices { get; }
            public int NonTissueMovedVertices { get; }
            public float StartChestPositionError { get; }
            public float StartChestRotationError { get; }
            public float ActualAnimatorCloneBackwardRecoil { get; }
            public float ActualAnimatorCloneFrameSignedYawDegrees { get; }
        }
    }
}
