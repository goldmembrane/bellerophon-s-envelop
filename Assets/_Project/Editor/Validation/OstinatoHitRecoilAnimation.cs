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

namespace Bellerophon.Editor
{
    internal static class OstinatoHitRecoilAnimation
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string PlacementRootName = "Approved Ostinato Enemy Placement";
        internal const string StaticSlotName = "Ostinato_05_Static_Review";
        internal const string HitSlotName = "Ostinato_05_Hit_Recoil";
        internal const string ModelName = "Ostinato_Model";
        internal const string HitFbxPath =
            "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_05_Hit_Recoil.fbx";
        internal const string SourceHitRelativePath = "enemies model/ostinato hitted.fbx";
        internal const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_05_Hit_Recoil.controller";
        internal const string ClipName = "Ostinato_05_Hit_Recoil";
        internal const string StateName = "Ostinato_05_Hit_Recoil";
        internal const string ValidationFolder =
            "docs/validation/ostinato_hit_fbx_replacement_2026-07-21";
        internal const string ReplacementValidationFolder =
            "docs/validation/ostinato_hit_fbx_replacement_2026-07-21";
        private const string ReplacementTargetReportPath =
            ReplacementValidationFolder + "/Ostinato_HitFbxReplacementTarget.txt";
        private const string ReplacementApplyReportPath =
            ReplacementValidationFolder + "/Ostinato_HitFbxReplacementApply.txt";
        private const string ReplacementInspectionReportPath =
            ReplacementValidationFolder + "/Ostinato_HitFbxReplacementInspection.txt";
        private const string SelectedTakeName = "mixamo.com";
        private const string ApprovedStaticSlotName = "Ostinato_01_Static_Review";
        private const string ApprovedModelPath =
            "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_ApprovedUnity.fbx";
        private const int HitSlotIndex = 4;
        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_SoftTissue.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_HookBlade.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_CompoundEye.mat",
        };
        private const string TargetReportPath = ValidationFolder + "/Ostinato_HitRecoilTargetInspection.txt";
        private const string ApplyReportPath = ValidationFolder + "/Ostinato_HitRecoilApply.txt";
        private const string InspectionReportPath = ValidationFolder + "/Ostinato_HitRecoilInspection.txt";
        private const float ExpectedDuration = 0.70f;
        private const float ExpectedFrameRate = 60f;
        private const float PeakTime = 11f / 60f;
        private const float ReturnTime = 30f / 60f;
        private const float MinimumTorsoRotation = 20f;
        private const float MinimumHeadRotation = 45f;
        private const float MinimumBackwardHeadTravel = 0.08f;
        private const float MaximumReturnRotationError = 0.15f;
        private const float MaximumReturnPositionError = 0.001f;

        private static readonly string[] RequiredBoneNames =
        {
            "Hips", "Spine02", "Spine01", "Spine", "neck", "Head",
        };

        public static void InspectOstinatoHitRecoilTarget()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var slot = FindHitSlot(root);
            if (slot.GetSiblingIndex() != 4)
            {
                throw new InvalidOperationException("Ostinato hit recoil must use sibling slot 05.");
            }

            var model = RequireModel(slot);
            var renderer = RequireRenderer(model);
            var bones = RequireBones(model);
            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Target=" + PlacementRootName + "/" + slot.name + "/" + model.name);
            report.AppendLine("SiblingIndex=" + slot.GetSiblingIndex());
            report.AppendLine("SlotLocalPosition=" + Format(slot.localPosition));
            report.AppendLine("SlotLocalEuler=" + Format(slot.localEulerAngles));
            report.AppendLine("SlotLocalScale=" + Format(slot.localScale));
            report.AppendLine("ModelLocalPosition=" + Format(model.localPosition));
            report.AppendLine("ModelLocalEuler=" + Format(model.localEulerAngles));
            report.AppendLine("ModelLocalScale=" + Format(model.localScale));
            report.AppendLine("Renderer=" + renderer.name);
            report.AppendLine("Mesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh));
            report.AppendLine("VertexCount=" + renderer.sharedMesh.vertexCount);
            report.AppendLine("Materials=" + string.Join("|", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            report.AppendLine("BoneCount=" + renderer.bones.Length);
            report.AppendLine("RequiredBones=" + string.Join("|", bones.Keys));
            foreach (var target in model.GetComponentsInChildren<Transform>(true))
            {
                report.AppendLine("Transform=" + RelativePath(model, target));
            }
            report.AppendLine("AnimatorPresent=" + (model.GetComponent<Animator>() != null));
            report.AppendLine("HitFbxPresent=" + (AssetDatabase.LoadMainAssetAtPath(HitFbxPath) != null));
            report.AppendLine("SceneChanged=False");
            WriteText(TargetReportPath, report.ToString());
            Selection.activeObject = null;

            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Hit recoil target inspection changed the scene dirty state.");
            }
            Debug.Log("OstinatoHitRecoilTargetInspected Slot=05, Bones=" + bones.Count +
                      ", SceneChanged=False, SelectionCleared=True");
        }

        public static void InspectOstinatoHitFbxReplacementTarget()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var importer = AssetImporter.GetAtPath(HitFbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato hit FBX importer is missing.");
            var defaults = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            if (defaults.Length == 0)
            {
                throw new InvalidOperationException("The supplied Ostinato hit FBX exposes no Unity animation take.");
            }

            var root = RequirePlacementRoot(scene);
            var slot = FindHitSlot(root);
            var model = RequireModel(slot);
            var sourceHash = ComputeSha256(ProjectAbsolutePath(SourceHitRelativePath));
            var importedHash = ComputeSha256(ProjectAbsolutePath(HitFbxPath));
            if (sourceHash != importedHash)
            {
                throw new InvalidOperationException("The project Ostinato hit FBX differs from the supplied source.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Target=" + PlacementRootName + "/" + slot.name + "/" + model.name);
            report.AppendLine("TargetSiblingIndex=" + slot.GetSiblingIndex());
            report.AppendLine("CurrentPlaybackObjectId=" + GlobalObjectId.GetGlobalObjectIdSlow(model.gameObject));
            report.AppendLine("SourceFbx=" + SourceHitRelativePath);
            report.AppendLine("ImportedFbx=" + HitFbxPath);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("ImportedSha256=" + importedHash);
            report.AppendLine("SourceCopyHashesMatch=True");
            report.AppendLine("DefaultTakeCount=" + defaults.Length);
            for (var index = 0; index < defaults.Length; index++)
            {
                var take = defaults[index];
                report.AppendLine("DefaultTake" + index + "Name=" + take.name);
                report.AppendLine("DefaultTake" + index + "TakeName=" + take.takeName);
                report.AppendLine("DefaultTake" + index + "FirstFrame=" + Format(take.firstFrame));
                report.AppendLine("DefaultTake" + index + "LastFrame=" + Format(take.lastFrame));
                report.AppendLine("DefaultTake" + index + "LoopTime=" + take.loopTime);
            }
            report.AppendLine("CurrentOverrideTakeCount=" +
                              (importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>()).Length);
            report.AppendLine("SceneChanged=False");
            WriteText(ReplacementTargetReportPath, report.ToString());
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Hit FBX target inspection changed the scene dirty state.");
            }
            Debug.Log("OstinatoHitFbxReplacementTargetInspected DefaultTakeCount=" + defaults.Length +
                      ", SourceCopyHashesMatch=True, SceneChanged=False");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Supplied Hit FBX Replacement")]
        public static void ApplyOstinatoHitFbxReplacement()
        {
            var scene = RequireScene();
            var root = RequirePlacementRoot(scene);
            var slot = FindHitSlot(root);
            if (slot.GetSiblingIndex() != HitSlotIndex || slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato hit replacement target must be slot 05 with one child.");
            }

            var slotSnapshot = new TransformSnapshot(slot);
            var otherSlotsBefore = CaptureOtherSlotSignatures(root, slot);
            var previousModel = slot.GetChild(0);
            var previousObjectId = GlobalObjectId.GetGlobalObjectIdSlow(previousModel.gameObject).ToString();
            var modelPosition = previousModel.localPosition;
            var modelRotation = previousModel.localRotation;
            var modelScale = previousModel.localScale;
            var sourceHashBefore = ComputeSha256(ProjectAbsolutePath(SourceHitRelativePath));
            var importedHashBefore = ComputeSha256(ProjectAbsolutePath(HitFbxPath));
            if (sourceHashBefore != importedHashBefore)
            {
                throw new InvalidOperationException("The project Ostinato hit FBX differs from the supplied source.");
            }

            ConfigureSuppliedHitImporter();
            var selectedTake = RequireSelectedTakeContract(out var defaultTake);
            var clip = RequireHitClip();
            RequireSuppliedClipContract(clip, defaultTake);
            var curveFingerprintBefore = BuildClipCurveFingerprint(clip);
            var controller = CreateOrUpdateController(clip);
            var hitAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HitFbxPath) ??
                throw new InvalidOperationException("The imported Ostinato hit FBX model is missing.");
            var staticSlot = root.Find(ApprovedStaticSlotName) ??
                throw new InvalidOperationException("The approved static Ostinato slot is missing.");
            var approvedRenderer = RequireRenderer(staticSlot);
            if (AssetDatabase.GetAssetPath(approvedRenderer.sharedMesh) != ApprovedModelPath)
            {
                throw new InvalidOperationException("The static Ostinato slot does not use the approved model mesh.");
            }
            var approvedMaterials = ApprovedMaterialPaths.Select(path =>
                AssetDatabase.LoadAssetAtPath<Material>(path) ??
                throw new InvalidOperationException("Approved Ostinato material is missing: " + path)).ToArray();

            var replacement = PrefabUtility.InstantiatePrefab(hitAsset, scene) as GameObject ??
                throw new InvalidOperationException("The supplied Ostinato hit FBX could not be instantiated.");
            replacement.name = ModelName;
            replacement.transform.SetParent(slot, false);
            replacement.transform.localPosition = modelPosition;
            replacement.transform.localRotation = modelRotation;
            replacement.transform.localScale = modelScale;
            var replacementRenderer = RequireRenderer(replacement.transform);
            SynchronizeApprovedAppearance(replacement, replacementRenderer, approvedRenderer, approvedMaterials);
            DisableNonOstinatoRenderers(replacement, replacementRenderer);
            RequireBindingContract(replacement.transform, clip);
            var animator = replacement.GetComponent<Animator>() ?? replacement.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.speed = 1f;
            animator.enabled = true;
            replacementRenderer.updateWhenOffscreen = true;

            UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
            slot.name = HitSlotName;
            slotSnapshot.AssertUnchanged(slot);
            if (slot.childCount != 1 || slot.GetChild(0) != replacement.transform)
            {
                throw new InvalidOperationException("The old Ostinato hit object was not replaced exactly once.");
            }
            var replacementObjectId = GlobalObjectId.GetGlobalObjectIdSlow(replacement).ToString();
            if (replacementObjectId == previousObjectId)
            {
                throw new InvalidOperationException("The Ostinato hit playback object was not replaced.");
            }
            RequireOtherSlotsUnchanged(root, otherSlotsBefore);
            RequireSynchronizedAppearance(replacement, replacementRenderer, approvedRenderer);
            RequireControllerContract(animator, clip);
            var prefabSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(replacement);
            if (prefabSourcePath != HitFbxPath)
            {
                throw new InvalidOperationException("The replacement object is not an instance of the supplied hit FBX. Actual=" +
                                                    prefabSourcePath);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(replacementRenderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            EditorUtility.SetDirty(replacementRenderer);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(slot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after hit FBX replacement.");
            }
            AssetDatabase.SaveAssets();

            var sourceHashAfter = ComputeSha256(ProjectAbsolutePath(SourceHitRelativePath));
            var importedHashAfter = ComputeSha256(ProjectAbsolutePath(HitFbxPath));
            var curveFingerprintAfter = BuildClipCurveFingerprint(RequireHitClip());
            if (sourceHashAfter != sourceHashBefore || importedHashAfter != importedHashBefore ||
                sourceHashAfter != importedHashAfter || curveFingerprintAfter != curveFingerprintBefore)
            {
                throw new InvalidOperationException("The supplied Ostinato hit FBX or its imported curves changed during scene replacement.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + HitSlotName + "/" + ModelName);
            report.AppendLine("PreviousPlaybackObjectId=" + previousObjectId);
            report.AppendLine("ReplacementPlaybackObjectId=" + replacementObjectId);
            report.AppendLine("PlaybackObjectReplaced=True");
            report.AppendLine("PlaybackPrefabSource=" + prefabSourcePath);
            report.AppendLine("SelectedTake=" + selectedTake.takeName);
            report.AppendLine("DefaultTakeFirstFrame=" + Format(defaultTake.firstFrame));
            report.AppendLine("DefaultTakeLastFrame=" + Format(defaultTake.lastFrame));
            report.AppendLine("OverrideTakeFirstFrame=" + Format(selectedTake.firstFrame));
            report.AppendLine("OverrideTakeLastFrame=" + Format(selectedTake.lastFrame));
            report.AppendLine("FullSelectedTakePreserved=True");
            report.AppendLine("LoopTime=" + selectedTake.loopTime);
            report.AppendLine("PlaybackSpeed=1");
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("AnimationCurveFingerprintBefore=" + curveFingerprintBefore);
            report.AppendLine("AnimationCurveFingerprintAfter=" + curveFingerprintAfter);
            report.AppendLine("AnimationCurvesModified=False");
            report.AppendLine("ApprovedMesh=" + AssetDatabase.GetAssetPath(replacementRenderer.sharedMesh));
            report.AppendLine("ApprovedMaterials=" + string.Join("|", replacementRenderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            report.AppendLine("AppearanceSynchronizedFrom=" + ApprovedStaticSlotName);
            report.AppendLine("SourceSha256Before=" + sourceHashBefore);
            report.AppendLine("SourceSha256After=" + sourceHashAfter);
            report.AppendLine("ImportedSha256Before=" + importedHashBefore);
            report.AppendLine("ImportedSha256After=" + importedHashAfter);
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            report.AppendLine("SceneSaved=True");
            WriteText(ReplacementApplyReportPath, report.ToString());
            Selection.activeGameObject = replacement;
            Debug.Log("OstinatoHitFbxReplacementApplied Take=mixamo.com, Frames=" +
                      Format(selectedTake.firstFrame) + ".." + Format(selectedTake.lastFrame) +
                      ", Speed=1, ApplyRootMotion=False, ApprovedAppearance=True, OtherSlotsUnchanged=True");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Supplied Hit FBX Replacement")]
        public static void InspectOstinatoHitFbxReplacement()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var slot = root.Find(HitSlotName) ??
                throw new InvalidOperationException("The Ostinato hit slot is missing.");
            if (slot.GetSiblingIndex() != HitSlotIndex || slot.childCount != 1)
            {
                throw new InvalidOperationException("The Ostinato hit replacement is not in slot 05.");
            }
            var model = slot.GetChild(0);
            if (model.name != ModelName)
            {
                throw new InvalidOperationException("The Ostinato hit replacement model name changed.");
            }
            var prefabSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject);
            if (prefabSourcePath != HitFbxPath)
            {
                throw new InvalidOperationException("The slot 05 object is not sourced from the supplied hit FBX.");
            }
            var staticSlot = root.Find(ApprovedStaticSlotName) ??
                throw new InvalidOperationException("The approved static Ostinato slot is missing.");
            var approvedRenderer = RequireRenderer(staticSlot);
            var renderer = RequireRenderer(model);
            RequireSynchronizedAppearance(model.gameObject, renderer, approvedRenderer);
            var clip = RequireHitClip();
            var selectedTake = RequireSelectedTakeContract(out var defaultTake);
            RequireSuppliedClipContract(clip, defaultTake);
            RequireBindingContract(model, clip);
            var animator = model.GetComponent<Animator>() ??
                throw new InvalidOperationException("The Ostinato hit replacement Animator is missing.");
            RequireControllerContract(animator, clip);
            var sourceHash = ComputeSha256(ProjectAbsolutePath(SourceHitRelativePath));
            var importedHash = ComputeSha256(ProjectAbsolutePath(HitFbxPath));
            if (sourceHash != importedHash)
            {
                throw new InvalidOperationException("The supplied and imported Ostinato hit FBX hashes differ.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Target=" + PlacementRootName + "/" + HitSlotName + "/" + ModelName);
            report.AppendLine("PlaybackObjectId=" + GlobalObjectId.GetGlobalObjectIdSlow(model.gameObject));
            report.AppendLine("PlaybackPrefabSource=" + prefabSourcePath);
            report.AppendLine("SelectedTake=" + selectedTake.takeName);
            report.AppendLine("SelectedTakeFirstFrame=" + Format(selectedTake.firstFrame));
            report.AppendLine("SelectedTakeLastFrame=" + Format(selectedTake.lastFrame));
            report.AppendLine("DefaultTakeFirstFrame=" + Format(defaultTake.firstFrame));
            report.AppendLine("DefaultTakeLastFrame=" + Format(defaultTake.lastFrame));
            report.AppendLine("FullSelectedTakePreserved=True");
            report.AppendLine("Clip=" + clip.name);
            report.AppendLine("ClipLength=" + Format(clip.length));
            report.AppendLine("ClipFrameRate=" + Format(clip.frameRate));
            report.AppendLine("ClipCurveBindings=" + AnimationUtility.GetCurveBindings(clip).Length);
            report.AppendLine("ClipCurveFingerprint=" + BuildClipCurveFingerprint(clip));
            report.AppendLine("LoopTime=" + selectedTake.loopTime);
            report.AppendLine("PlaybackSpeed=" + Format(animator.speed));
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("ApprovedMesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh));
            report.AppendLine("ApprovedMaterials=" + string.Join("|", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            report.AppendLine("AppearanceSynchronizedFrom=" + ApprovedStaticSlotName);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("ImportedSha256=" + importedHash);
            report.AppendLine("SourceCopyHashesMatch=True");
            report.AppendLine("SceneChanged=False");
            WriteText(ReplacementInspectionReportPath, report.ToString());
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Hit FBX replacement inspection changed the scene dirty state.");
            }
            Debug.Log("OstinatoHitFbxReplacementInspected Result=PASS, Take=mixamo.com, FullTake=True, " +
                      "Speed=1, ApplyRootMotion=False, ApprovedAppearance=True, SceneChanged=False");
        }

        public static void CaptureOstinatoHitFbxReplacement()
        {
            InspectOstinatoHitFbxReplacement();
            OstinatoHitRecoilRuntimeCapture.Begin();
        }

        public static void ApplyOstinatoHitRecoilAnimation()
        {
            var scene = RequireScene();
            var root = RequirePlacementRoot(scene);
            var slot = FindHitSlot(root);
            if (slot.GetSiblingIndex() != 4)
            {
                throw new InvalidOperationException("Ostinato hit recoil target is not sibling slot 05.");
            }

            var slotSnapshot = new TransformSnapshot(slot);
            var otherSlotSignatures = root.Cast<Transform>()
                .Where(candidate => candidate != slot)
                .ToDictionary(candidate => candidate.name, BuildHierarchySignature);
            var model = RequireModel(slot);
            var renderer = RequireRenderer(model);
            var mesh = renderer.sharedMesh;
            var materials = renderer.sharedMaterials.ToArray();
            var geometrySignature = RendererSignature(renderer);
            RequireBones(model);

            ConfigureHitImporter();
            var clip = RequireHitClip();
            RequireClipContract(clip);
            var controller = CreateOrUpdateController(clip);

            slot.name = HitSlotName;
            var animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.speed = 1f;
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(slot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after Ostinato hit recoil apply.");
            }

            slotSnapshot.AssertUnchanged(slot);
            if (slot.GetSiblingIndex() != 4 || slot.name != HitSlotName)
            {
                throw new InvalidOperationException("Ostinato slot 05 identity changed unexpectedly.");
            }
            if (renderer.sharedMesh != mesh || !renderer.sharedMaterials.SequenceEqual(materials) ||
                RendererSignature(renderer) != geometrySignature)
            {
                throw new InvalidOperationException("Ostinato hit recoil changed the approved mesh or materials.");
            }
            foreach (var pair in otherSlotSignatures)
            {
                var candidate = root.Find(pair.Key) ??
                    throw new InvalidOperationException("Another Ostinato slot disappeared: " + pair.Key);
                if (BuildHierarchySignature(candidate) != pair.Value)
                {
                    throw new InvalidOperationException("Another Ostinato slot changed: " + pair.Key);
                }
            }

            var metrics = InspectMotion(model, clip);
            RequireMotionContract(metrics);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var report = BuildMotionReport(metrics, clip, animator, bindings);
            report.Insert(0, "Result=PASS" + Environment.NewLine);
            report.AppendLine("SourceBlend=Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_05_Hit_Recoil_Source.blend");
            report.AppendLine("SourceFbxSha256=" + ComputeSha256(ProjectAbsolutePath(HitFbxPath)));
            report.AppendLine("ApprovedMeshPreserved=True");
            report.AppendLine("ApprovedMaterialsPreserved=True");
            report.AppendLine("SlotTransformPreserved=True");
            report.AppendLine("OtherSlotsChanged=False");
            report.AppendLine("SceneSaved=True");
            WriteText(ApplyReportPath, report.ToString());
            Selection.activeObject = null;
            Debug.Log("OstinatoHitRecoilApplied Duration=" + Format(clip.length) +
                      ", TorsoRotation=" + Format(metrics.TorsoRotation) +
                      ", HeadRotation=" + Format(metrics.HeadRotation) +
                      ", HeadBackwardTravel=" + Format(metrics.HeadBackwardTravel) +
                      ", ReturnedToRest=True, OtherSlotsChanged=False");
        }

        public static void InspectOstinatoHitRecoilAnimation()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var slot = root.Find(HitSlotName) ??
                throw new InvalidOperationException("Ostinato hit recoil slot is not applied.");
            if (slot.GetSiblingIndex() != 4)
            {
                throw new InvalidOperationException("Ostinato hit recoil is not in slot 05.");
            }
            var model = RequireModel(slot);
            var renderer = RequireRenderer(model);
            var animator = model.GetComponent<Animator>() ??
                throw new InvalidOperationException("Ostinato hit recoil Animator is missing.");
            var clip = RequireHitClip();
            RequireClipContract(clip);
            RequireControllerContract(animator, clip);
            RequireBindingContract(model, clip);
            var metrics = InspectMotion(model, clip);
            RequireMotionContract(metrics);
            var report = BuildMotionReport(
                metrics,
                clip,
                animator,
                AnimationUtility.GetCurveBindings(clip));
            report.Insert(0, "Result=PASS" + Environment.NewLine);
            report.AppendLine("RendererSignature=" + RendererSignature(renderer));
            report.AppendLine("SourceFbxSha256=" + ComputeSha256(ProjectAbsolutePath(HitFbxPath)));
            report.AppendLine("SceneChanged=False");
            WriteText(InspectionReportPath, report.ToString());
            Selection.activeObject = null;
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("Hit recoil inspection changed the scene dirty state.");
            }
            Debug.Log("OstinatoHitRecoilInspected Result=PASS, HeadRotation=" +
                      Format(metrics.HeadRotation) + ", TorsoRotation=" +
                      Format(metrics.TorsoRotation) + ", ReturnedToRest=True, SceneChanged=False");
        }

        public static void CaptureOstinatoHitRecoilAnimation()
        {
            var scene = RequireScene();
            var root = RequirePlacementRoot(scene);
            var slot = root.Find(HitSlotName) ??
                throw new InvalidOperationException("Ostinato hit recoil slot is not applied.");
            var model = RequireModel(slot);
            var animator = model.GetComponent<Animator>() ??
                throw new InvalidOperationException("Ostinato hit recoil Animator is missing.");
            var clip = RequireHitClip();
            RequireClipContract(clip);
            RequireControllerContract(animator, clip);
            RequireMotionContract(InspectMotion(model, clip));
            OstinatoHitRecoilRuntimeCapture.Begin();
        }

        private static void ConfigureSuppliedHitImporter()
        {
            var importer = AssetImporter.GetAtPath(HitFbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato hit FBX importer is missing.");
            var defaults = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var matches = defaults.Where(take => take.takeName == SelectedTakeName || take.name == SelectedTakeName).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("The supplied hit FBX must expose exactly one mixamo.com take. Matches=" +
                                                    matches.Length);
            }
            var selected = matches[0];
            selected.name = ClipName;
            selected.loopTime = true;
            selected.loopPose = false;
            selected.wrapMode = WrapMode.Loop;
            importer.isReadable = true;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.resampleCurves = false;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
        }

        private static ModelImporterClipAnimation RequireSelectedTakeContract(
            out ModelImporterClipAnimation defaultTake)
        {
            var importer = AssetImporter.GetAtPath(HitFbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato hit FBX importer is missing.");
            var defaults = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var defaultMatches = defaults.Where(take =>
                take.takeName == SelectedTakeName || take.name == SelectedTakeName).ToArray();
            if (defaultMatches.Length != 1)
            {
                throw new InvalidOperationException("The supplied hit FBX no longer exposes one mixamo.com default take.");
            }
            defaultTake = defaultMatches[0];
            var overrides = importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            if (overrides.Length != 1)
            {
                throw new InvalidOperationException("The hit FBX must import exactly one selected take. Count=" + overrides.Length);
            }
            var selected = overrides[0];
            if (selected.name != ClipName || selected.takeName != SelectedTakeName ||
                !Mathf.Approximately(selected.firstFrame, defaultTake.firstFrame) ||
                !Mathf.Approximately(selected.lastFrame, defaultTake.lastFrame) ||
                !selected.loopTime || selected.loopPose)
            {
                throw new InvalidOperationException("The mixamo.com take is not configured as an untrimmed loop-only override.");
            }
            if (selected.lockRootRotation != defaultTake.lockRootRotation ||
                selected.lockRootHeightY != defaultTake.lockRootHeightY ||
                selected.lockRootPositionXZ != defaultTake.lockRootPositionXZ ||
                selected.keepOriginalOrientation != defaultTake.keepOriginalOrientation ||
                selected.keepOriginalPositionY != defaultTake.keepOriginalPositionY ||
                selected.keepOriginalPositionXZ != defaultTake.keepOriginalPositionXZ)
            {
                throw new InvalidOperationException("A root transform import option differs from the source mixamo.com take.");
            }
            return selected;
        }

        private static void RequireSuppliedClipContract(AnimationClip clip, ModelImporterClipAnimation defaultTake)
        {
            if (AnimationUtility.GetCurveBindings(clip).Length == 0)
            {
                throw new InvalidOperationException("The selected mixamo.com clip has no animation curves.");
            }
            var expectedLength = (defaultTake.lastFrame - defaultTake.firstFrame) / clip.frameRate;
            if (clip.frameRate <= 0f || Mathf.Abs(clip.length - expectedLength) > (1f / clip.frameRate + 0.001f))
            {
                throw new InvalidOperationException("The imported clip does not preserve the full selected take. ExpectedLength=" +
                                                    Format(expectedLength) + ", Actual=" + Format(clip.length));
            }
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("The selected mixamo.com clip is not configured to loop.");
            }
        }

        private static Dictionary<string, string> CaptureOtherSlotSignatures(Transform root, Transform excluded)
        {
            return root.Cast<Transform>().Where(candidate => candidate != excluded)
                .ToDictionary(candidate => candidate.name, BuildHierarchySignature);
        }

        private static void RequireOtherSlotsUnchanged(Transform root, IReadOnlyDictionary<string, string> before)
        {
            foreach (var pair in before)
            {
                var candidate = root.Find(pair.Key) ??
                    throw new InvalidOperationException("Another Ostinato slot disappeared: " + pair.Key);
                if (BuildHierarchySignature(candidate) != pair.Value)
                {
                    throw new InvalidOperationException("Another Ostinato slot changed: " + pair.Key);
                }
            }
        }

        private static void SynchronizeApprovedAppearance(
            GameObject replacement,
            SkinnedMeshRenderer replacementRenderer,
            SkinnedMeshRenderer approvedRenderer,
            Material[] approvedMaterials)
        {
            if (approvedRenderer.sharedMesh == null || approvedRenderer.rootBone == null || approvedRenderer.bones.Length == 0)
            {
                throw new InvalidOperationException("The approved static Ostinato skinning data is incomplete.");
            }
            var replacementTransforms = replacement.GetComponentsInChildren<Transform>(true);
            var mappedBones = approvedRenderer.bones.Select(approvedBone =>
            {
                var matches = replacementTransforms.Where(candidate => candidate.name == approvedBone.name).ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidOperationException("The supplied hit FBX rig cannot map approved bone: " + approvedBone.name);
                }
                return matches[0];
            }).ToArray();
            var rootMatches = replacementTransforms.Where(candidate =>
                candidate.name == approvedRenderer.rootBone.name).ToArray();
            if (rootMatches.Length != 1)
            {
                throw new InvalidOperationException("The supplied hit FBX rig cannot map the approved root bone.");
            }
            replacementRenderer.sharedMesh = approvedRenderer.sharedMesh;
            replacementRenderer.bones = mappedBones;
            replacementRenderer.rootBone = rootMatches[0];
            replacementRenderer.sharedMaterials = approvedMaterials;
        }

        private static void DisableNonOstinatoRenderers(GameObject replacement, Renderer approvedRenderer)
        {
            foreach (var renderer in replacement.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != approvedRenderer)
                {
                    renderer.enabled = false;
                }
            }
        }

        private static void RequireSynchronizedAppearance(
            GameObject replacement,
            SkinnedMeshRenderer renderer,
            SkinnedMeshRenderer approvedRenderer)
        {
            if (renderer.sharedMesh != approvedRenderer.sharedMesh)
            {
                throw new InvalidOperationException("The hit replacement does not use the approved static Ostinato mesh.");
            }
            if (!renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).SequenceEqual(ApprovedMaterialPaths))
            {
                throw new InvalidOperationException("The hit replacement does not use the four approved materials.");
            }
            var approvedBoneNames = approvedRenderer.bones.Select(bone => bone.name).ToArray();
            var replacementBoneNames = renderer.bones.Select(bone => bone == null ? string.Empty : bone.name).ToArray();
            if (!approvedBoneNames.SequenceEqual(replacementBoneNames) || renderer.rootBone == null ||
                approvedRenderer.rootBone == null || renderer.rootBone.name != approvedRenderer.rootBone.name)
            {
                throw new InvalidOperationException("The approved appearance is not mapped to the supplied hit FBX rig.");
            }
            var visibleExtraRenderers = replacement.GetComponentsInChildren<Renderer>(true)
                .Where(candidate => candidate != renderer && candidate.enabled).ToArray();
            if (visibleExtraRenderers.Length != 0)
            {
                throw new InvalidOperationException("The supplied hit FBX has an unsynchronized visible renderer.");
            }
        }

        private static string BuildClipCurveFingerprint(AnimationClip clip)
        {
            var builder = new StringBuilder();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .OrderBy(item => item.path, StringComparer.Ordinal)
                         .ThenBy(item => item.propertyName, StringComparer.Ordinal))
            {
                builder.Append(binding.path).Append('|').Append(binding.type.FullName).Append('|')
                    .Append(binding.propertyName).Append('|');
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null)
                {
                    foreach (var key in curve.keys)
                    {
                        builder.Append(key.time.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                            .Append(key.value.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                            .Append(key.inTangent.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                            .Append(key.outTangent.ToString("R", CultureInfo.InvariantCulture)).Append(';');
                    }
                }
                builder.AppendLine();
            }
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())))
                    .Replace("-", string.Empty);
            }
        }

        private static void ConfigureHitImporter()
        {
            var importer = AssetImporter.GetAtPath(HitFbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato hit recoil FBX importer is missing.");
            importer.isReadable = true;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.resampleCurves = false;
            var clips = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException("Ostinato hit FBX must expose one default take. Count=" + clips.Length);
            }
            var take = clips[0];
            take.name = ClipName;
            take.loopTime = true;
            take.loopPose = false;
            take.wrapMode = WrapMode.Loop;
            take.lockRootRotation = true;
            take.lockRootHeightY = true;
            take.lockRootPositionXZ = true;
            take.keepOriginalOrientation = true;
            take.keepOriginalPositionY = true;
            take.keepOriginalPositionXZ = true;
            importer.clipAnimations = new[] { take };
            importer.SaveAndReimport();
        }

        private static AnimationClip RequireHitClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(HitFbxPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var matches = clips.Where(clip => clip.name == ClipName).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Ostinato hit FBX must expose exactly one " + ClipName +
                                                    " clip. Matches=" + matches.Length +
                                                    ", All=" + string.Join("|", clips.Select(clip => clip.name)));
            }
            return matches[0];
        }

        private static void RequireClipContract(AnimationClip clip)
        {
            if (Mathf.Abs(clip.length - ExpectedDuration) > 0.02f)
            {
                throw new InvalidOperationException("Hit clip duration changed. Expected=0.70, Actual=" + Format(clip.length));
            }
            if (Mathf.Abs(clip.frameRate - ExpectedFrameRate) > 0.01f)
            {
                throw new InvalidOperationException("Hit clip frame rate changed. Actual=" + Format(clip.frameRate));
            }
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("Hit clip loop playback is disabled.");
            }
            if (AnimationUtility.GetCurveBindings(clip).Length == 0)
            {
                throw new InvalidOperationException("Hit clip has no animation curves.");
            }
        }

        private static AnimatorController CreateOrUpdateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }
            var layers = controller.layers;
            if (layers.Length != 1)
            {
                throw new InvalidOperationException("Ostinato hit controller must have one layer.");
            }
            var machine = layers[0].stateMachine;
            foreach (var child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }
            var state = machine.AddState(StateName);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            machine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void RequireControllerContract(Animator animator, AnimationClip clip)
        {
            var controller = animator.runtimeAnimatorController as AnimatorController ??
                throw new InvalidOperationException("Ostinato hit Animator Controller is missing.");
            if (AssetDatabase.GetAssetPath(controller) != ControllerPath || animator.applyRootMotion ||
                !Mathf.Approximately(animator.speed, 1f))
            {
                throw new InvalidOperationException("Ostinato hit Animator settings changed.");
            }
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states).ToArray();
            if (states.Length != 1 || states[0].state.name != StateName || states[0].state.motion != clip ||
                !Mathf.Approximately(states[0].state.speed, 1f))
            {
                throw new InvalidOperationException("Ostinato hit controller state contract changed.");
            }
        }

        private static void RequireBindingContract(Transform model, AnimationClip clip)
        {
            var unresolved = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => !string.IsNullOrEmpty(binding.path) && model.Find(binding.path) == null)
                .Select(binding => binding.path + ":" + binding.propertyName)
                .ToArray();
            if (unresolved.Length > 0)
            {
                throw new InvalidOperationException("Hit clip has unresolved bindings: " + string.Join("|", unresolved));
            }
        }

        private static MotionMetrics InspectMotion(Transform model, AnimationClip clip)
        {
            RequireBindingContract(model, clip);
            var transforms = model.GetComponentsInChildren<Transform>(true);
            var original = transforms.Select(target => new TransformSnapshot(target)).ToArray();
            var baseline = SamplePose(model, clip, 0f);
            var peak = SamplePose(model, clip, PeakTime);
            var returned = SamplePose(model, clip, ReturnTime);
            var ended = SamplePose(model, clip, clip.length);
            foreach (var snapshot in original)
            {
                snapshot.AssertUnchanged(snapshot.Target);
            }

            return new MotionMetrics(
                Quaternion.Angle(baseline.SpineRotation, peak.SpineRotation),
                Quaternion.Angle(baseline.HeadRotation, peak.HeadRotation),
                baseline.HeadPosition.z - peak.HeadPosition.z,
                baseline.SpinePosition.z - peak.SpinePosition.z,
                PoseSnapshot.MaximumRotationDelta(baseline, returned),
                PoseSnapshot.MaximumPositionDelta(baseline, returned),
                PoseSnapshot.MaximumRotationDelta(baseline, ended),
                PoseSnapshot.MaximumPositionDelta(baseline, ended),
                Vector3.Distance(baseline.ModelPosition, peak.ModelPosition),
                Quaternion.Angle(baseline.ModelRotation, peak.ModelRotation));
        }

        private static PoseSnapshot SamplePose(Transform model, AnimationClip clip, float time)
        {
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.SampleAnimationClip(model.gameObject, clip, Mathf.Clamp(time, 0f, clip.length));
                var bones = RequireBones(model);
                return new PoseSnapshot(model, bones);
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        private static void RequireMotionContract(MotionMetrics metrics)
        {
            if (metrics.TorsoRotation < MinimumTorsoRotation)
            {
                throw new InvalidOperationException("Torso hit recoil is not visible enough. Actual=" + Format(metrics.TorsoRotation));
            }
            if (metrics.HeadRotation < MinimumHeadRotation || metrics.HeadRotation <= metrics.TorsoRotation + 10f)
            {
                throw new InvalidOperationException("Head recoil is not distinctly larger than the torso. Head=" +
                                                    Format(metrics.HeadRotation) + ", Torso=" + Format(metrics.TorsoRotation));
            }
            if (metrics.HeadBackwardTravel < MinimumBackwardHeadTravel ||
                metrics.HeadBackwardTravel <= metrics.SpineBackwardTravel)
            {
                throw new InvalidOperationException("Head and torso do not flinch backward clearly. Head=" +
                                                    Format(metrics.HeadBackwardTravel) + ", Spine=" +
                                                    Format(metrics.SpineBackwardTravel));
            }
            if (metrics.ReturnRotationError > MaximumReturnRotationError ||
                metrics.EndRotationError > MaximumReturnRotationError ||
                metrics.ReturnPositionError > MaximumReturnPositionError ||
                metrics.EndPositionError > MaximumReturnPositionError)
            {
                throw new InvalidOperationException("Hit recoil did not return to the exact rest pose.");
            }
            if (metrics.ModelRootDistance > 0.0001f || metrics.ModelRootRotation > 0.01f)
            {
                throw new InvalidOperationException("Hit recoil moved the model root.");
            }
        }

        private static StringBuilder BuildMotionReport(
            MotionMetrics metrics,
            AnimationClip clip,
            Animator animator,
            EditorCurveBinding[] bindings)
        {
            var report = new StringBuilder();
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + HitSlotName + "/" + ModelName);
            report.AppendLine("SourceFbx=" + HitFbxPath);
            report.AppendLine("Clip=" + clip.name);
            report.AppendLine("Controller=" + ControllerPath);
            report.AppendLine("AnimatorState=" + StateName);
            report.AppendLine("DurationSeconds=" + Format(clip.length));
            report.AppendLine("FrameRate=" + Format(clip.frameRate));
            report.AppendLine("PeakTimeSeconds=" + Format(PeakTime));
            report.AppendLine("ReturnTimeSeconds=" + Format(ReturnTime));
            report.AppendLine("LoopTime=" + AnimationUtility.GetAnimationClipSettings(clip).loopTime);
            report.AppendLine("ApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("CurveBindingCount=" + bindings.Length);
            report.AppendLine("AnimatedTransformCount=" + bindings.Select(binding => binding.path).Distinct().Count());
            report.AppendLine("TorsoRotationDegrees=" + Format(metrics.TorsoRotation));
            report.AppendLine("HeadRotationDegrees=" + Format(metrics.HeadRotation));
            report.AppendLine("HeadRotationLargerThanTorso=" + (metrics.HeadRotation > metrics.TorsoRotation + 10f));
            report.AppendLine("HeadBackwardTravel=" + Format(metrics.HeadBackwardTravel));
            report.AppendLine("SpineBackwardTravel=" + Format(metrics.SpineBackwardTravel));
            report.AppendLine("ReturnRotationError=" + Format(metrics.ReturnRotationError));
            report.AppendLine("ReturnPositionError=" + Format(metrics.ReturnPositionError));
            report.AppendLine("EndRotationError=" + Format(metrics.EndRotationError));
            report.AppendLine("EndPositionError=" + Format(metrics.EndPositionError));
            report.AppendLine("ModelRootDistance=" + Format(metrics.ModelRootDistance));
            report.AppendLine("ModelRootRotation=" + Format(metrics.ModelRootRotation));
            report.AppendLine("ReturnedToRest=True");
            report.AppendLine("RootMotionLocked=True");
            return report;
        }

        private static Dictionary<string, Transform> RequireBones(Transform model)
        {
            var all = model.GetComponentsInChildren<Transform>(true);
            var result = new Dictionary<string, Transform>();
            foreach (var name in RequiredBoneNames)
            {
                var matches = all.Where(target => target.name == name).ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidOperationException("Required Ostinato bone is not unique: " + name);
                }
                result.Add(name, matches[0]);
            }
            return result;
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be the active scene.");
            }
            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects().Single(target => target.name == PlacementRootName).transform;
        }

        private static Transform FindHitSlot(Transform root)
        {
            var hit = root.Find(HitSlotName);
            var original = root.Find(StaticSlotName);
            if (hit != null && original != null)
            {
                throw new InvalidOperationException("Both static and hit slot 05 exist.");
            }
            return hit ?? original ?? throw new InvalidOperationException("Ostinato slot 05 is missing.");
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato slot 05 must contain exactly one model root.");
            }
            var model = slot.GetChild(0);
            if (model.name != ModelName)
            {
                throw new InvalidOperationException("Unexpected Ostinato slot 05 model name: " + model.name);
            }
            return model;
        }

        private static SkinnedMeshRenderer RequireRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1 || renderers[0].sharedMesh == null)
            {
                throw new InvalidOperationException("Ostinato slot 05 must contain one valid skinned renderer.");
            }
            return renderers[0];
        }

        private static string BuildHierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(RelativePath(root, target)).Append('|')
                    .Append(Format(target.localPosition)).Append('|')
                    .Append(Format(target.localRotation)).Append('|')
                    .Append(Format(target.localScale)).Append('|');
                var renderer = target.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null)
                {
                    builder.Append(AssetDatabase.GetAssetPath(renderer.sharedMesh)).Append('|')
                        .Append(string.Join(",", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
                }
                var animator = target.GetComponent<Animator>();
                if (animator != null)
                {
                    builder.Append(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)).Append('|')
                        .Append(animator.applyRootMotion);
                }
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private static string RendererSignature(SkinnedMeshRenderer renderer)
        {
            return AssetDatabase.GetAssetPath(renderer.sharedMesh) + ";" + renderer.sharedMesh.vertexCount + ";" +
                   string.Join("|", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath));
        }

        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root) return string.Empty;
            var names = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", names);
        }

        internal static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        internal static void WriteText(string relativePath, string text)
        {
            var absolute = ProjectAbsolutePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                                      throw new InvalidOperationException("Output directory is invalid."));
            File.WriteAllText(absolute, text, new UTF8Encoding(false));
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static string Format(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Format(Vector3 value)
        {
            return $"({Format(value.x)},{Format(value.y)},{Format(value.z)})";
        }

        private static string Format(Quaternion value)
        {
            return $"({Format(value.x)},{Format(value.y)},{Format(value.z)},{Format(value.w)})";
        }

        private readonly struct MotionMetrics
        {
            public MotionMetrics(
                float torsoRotation,
                float headRotation,
                float headBackwardTravel,
                float spineBackwardTravel,
                float returnRotationError,
                float returnPositionError,
                float endRotationError,
                float endPositionError,
                float modelRootDistance,
                float modelRootRotation)
            {
                TorsoRotation = torsoRotation;
                HeadRotation = headRotation;
                HeadBackwardTravel = headBackwardTravel;
                SpineBackwardTravel = spineBackwardTravel;
                ReturnRotationError = returnRotationError;
                ReturnPositionError = returnPositionError;
                EndRotationError = endRotationError;
                EndPositionError = endPositionError;
                ModelRootDistance = modelRootDistance;
                ModelRootRotation = modelRootRotation;
            }

            public float TorsoRotation { get; }
            public float HeadRotation { get; }
            public float HeadBackwardTravel { get; }
            public float SpineBackwardTravel { get; }
            public float ReturnRotationError { get; }
            public float ReturnPositionError { get; }
            public float EndRotationError { get; }
            public float EndPositionError { get; }
            public float ModelRootDistance { get; }
            public float ModelRootRotation { get; }
        }

        private sealed class PoseSnapshot
        {
            private readonly Dictionary<string, Vector3> positions;
            private readonly Dictionary<string, Quaternion> rotations;

            public PoseSnapshot(Transform model, IReadOnlyDictionary<string, Transform> bones)
            {
                ModelPosition = model.position;
                ModelRotation = model.rotation;
                positions = bones.ToDictionary(pair => pair.Key, pair => model.InverseTransformPoint(pair.Value.position));
                rotations = bones.ToDictionary(
                    pair => pair.Key,
                    pair => Quaternion.Inverse(model.rotation) * pair.Value.rotation);
                SpinePosition = positions["Spine"];
                HeadPosition = positions["Head"];
                SpineRotation = rotations["Spine"];
                HeadRotation = rotations["Head"];
            }

            public Vector3 ModelPosition { get; }
            public Quaternion ModelRotation { get; }
            public Vector3 SpinePosition { get; }
            public Vector3 HeadPosition { get; }
            public Quaternion SpineRotation { get; }
            public Quaternion HeadRotation { get; }

            public static float MaximumPositionDelta(PoseSnapshot left, PoseSnapshot right)
            {
                return left.positions.Keys.Max(key => Vector3.Distance(left.positions[key], right.positions[key]));
            }

            public static float MaximumRotationDelta(PoseSnapshot left, PoseSnapshot right)
            {
                return left.rotations.Keys.Max(key => Quaternion.Angle(left.rotations[key], right.rotations[key]));
            }
        }

        private sealed class TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private readonly int siblingIndex;
            public TransformSnapshot(Transform target)
            {
                Target = target;
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
                siblingIndex = target.GetSiblingIndex();
            }

            public Transform Target { get; }

            public void AssertUnchanged(Transform target)
            {
                if (target.localPosition != position || target.localRotation != rotation ||
                    target.localScale != scale || target.GetSiblingIndex() != siblingIndex)
                {
                    throw new InvalidOperationException("A preserved Ostinato Transform changed: " + target.name);
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class OstinatoHitRecoilRuntimeCapture
    {
        private const string SessionKey = "Bellerophon.OstinatoHitRecoilRuntimeCapture.State";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int ReviewLayer = 29;
        private const int ImageSize = 320;
        private const int SheetColumns = 5;
        private const int CaptureFrameCount = 15;
        private const string RuntimeFramesPath = OstinatoHitRecoilAnimation.ValidationFolder + "/runtime_frames";
        private const string RuntimeImagePath =
            OstinatoHitRecoilAnimation.ValidationFolder + "/Ostinato_HitRecoil_RuntimeContactSheet.png";
        private const string RuntimeReportPath =
            OstinatoHitRecoilAnimation.ValidationFolder + "/Ostinato_HitRecoil_RuntimePlayback.txt";
        private const string CompletionPath =
            OstinatoHitRecoilAnimation.ValidationFolder + "/Ostinato_HitRecoil_RuntimePlayback.completed";
        private const string FailurePath =
            OstinatoHitRecoilAnimation.ValidationFolder + "/Ostinato_HitRecoil_RuntimePlayback.failed.txt";

        private static readonly float[] TargetNormalizedTimes = Enumerable.Range(0, CaptureFrameCount)
            .Select(index => index / (float)(CaptureFrameCount - 1)).ToArray();
        private static readonly List<byte[]> CapturedImages = new List<byte[]>();
        private static Animator animator;
        private static SkinnedMeshRenderer renderer;
        private static Camera reviewCamera;
        private static GameObject cameraObject;
        private static GameObject keyObject;
        private static GameObject fillObject;
        private static GameObject[] layeredObjects;
        private static int[] originalLayers;
        private static Vector3 modelStartPosition;
        private static Quaternion modelStartRotation;
        private static double captureStartTime;
        private static float startNormalizedTime;
        private static int nextCaptureIndex;
        private static float maximumRootDistance;
        private static float maximumRootRotation;

        static OstinatoHitRecoilRuntimeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Unity must be in Edit Mode before hit recoil capture begins.");
            }
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != OstinatoHitRecoilAnimation.ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be active before hit recoil capture.");
            }
            TryDelete(CompletionPath);
            TryDelete(FailurePath);
            TryDeleteDirectory(RuntimeFramesPath);
            CapturedImages.Clear();
            SessionState.SetInt(SessionKey, WaitingForPlayMode);
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            var state = SessionState.GetInt(SessionKey, 0);
            if (state == 0) return;
            try
            {
                if (state == WaitingForPlayMode)
                {
                    if (EditorApplication.isPlaying) StartCapture();
                    return;
                }
                if (state == Capturing)
                {
                    if (!EditorApplication.isPlaying)
                    {
                        throw new InvalidOperationException("Unity left Play Mode before hit recoil capture completed.");
                    }
                    CaptureWhenDue();
                    return;
                }
                if (state == WaitingForEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.EraseInt(SessionKey);
                    OstinatoHitRecoilAnimation.WriteText(
                        CompletionPath,
                        "Ostinato hit recoil capture completed in Unity Editor Play Mode.");
                    Debug.Log("OstinatoHitRecoilCaptured Frames=" + CaptureFrameCount +
                              ", Views=Front|ThreeQuarter, Image=" + RuntimeImagePath);
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private static void StartCapture()
        {
            var scene = SceneManager.GetActiveScene();
            var root = scene.GetRootGameObjects().Single(target =>
                target.name == OstinatoHitRecoilAnimation.PlacementRootName).transform;
            var slot = root.Find(OstinatoHitRecoilAnimation.HitSlotName) ??
                throw new InvalidOperationException("Ostinato hit recoil slot is missing in Play Mode.");
            var model = slot.childCount == 1 ? slot.GetChild(0) : null;
            if (model == null || model.name != OstinatoHitRecoilAnimation.ModelName)
            {
                throw new InvalidOperationException("Ostinato hit recoil model is invalid in Play Mode.");
            }
            animator = model.GetComponent<Animator>();
            renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            if (animator == null || animator.runtimeAnimatorController == null || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Ostinato hit recoil runtime Animator is invalid.");
            }

            layeredObjects = model.GetComponentsInChildren<Transform>(true)
                .Select(target => target.gameObject).ToArray();
            originalLayers = layeredObjects.Select(target => target.layer).ToArray();
            foreach (var target in layeredObjects) target.layer = ReviewLayer;

            cameraObject = new GameObject("Ostinato_HitRecoil_ReviewCamera", typeof(Camera));
            keyObject = new GameObject("Ostinato_HitRecoil_KeyLight", typeof(Light));
            fillObject = new GameObject("Ostinato_HitRecoil_FillLight", typeof(Light));
            reviewCamera = cameraObject.GetComponent<Camera>();
            ConfigureCameraAndLights();
            modelStartPosition = model.position;
            modelStartRotation = model.rotation;
            animator.Play(OstinatoHitRecoilAnimation.StateName, 0, 0f);
            animator.Update(0f);
            startNormalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            captureStartTime = EditorApplication.timeSinceStartup;
            nextCaptureIndex = 0;
            maximumRootDistance = 0f;
            maximumRootRotation = 0f;
            CapturedImages.Clear();
            SessionState.SetInt(SessionKey, Capturing);
        }

        private static void CaptureWhenDue()
        {
            if (EditorApplication.timeSinceStartup - captureStartTime > 12d)
            {
                throw new TimeoutException("Ostinato hit recoil Animator capture timed out.");
            }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(OstinatoHitRecoilAnimation.StateName))
            {
                throw new InvalidOperationException("Ostinato hit recoil Animator left its state.");
            }
            var elapsed = state.normalizedTime - startNormalizedTime;
            if (elapsed + 0.002f < TargetNormalizedTimes[nextCaptureIndex]) return;
            maximumRootDistance = Mathf.Max(maximumRootDistance,
                Vector3.Distance(animator.transform.position, modelStartPosition));
            maximumRootRotation = Mathf.Max(maximumRootRotation,
                Quaternion.Angle(animator.transform.rotation, modelStartRotation));
            var texture = RenderFrame();
            CapturedImages.Add(texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            nextCaptureIndex++;
            if (nextCaptureIndex >= TargetNormalizedTimes.Length) Finish();
        }

        private static Texture2D RenderFrame()
        {
            var bounds = renderer.bounds;
            bounds.Expand(new Vector3(0.18f, 0.14f, 0.18f));
            var target = bounds.center + Vector3.up * bounds.extents.y * 0.03f;
            var halfFov = reviewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var distance = Mathf.Max(bounds.extents.y, bounds.extents.x) / Mathf.Tan(halfFov) +
                           bounds.extents.z + 0.18f;
            var front = RenderView(target, Vector3.back, distance);
            var oblique = RenderView(target, new Vector3(0.7f, 0f, -1f).normalized, distance);
            var combined = new Texture2D(ImageSize * 2, ImageSize, TextureFormat.RGBA32, false);
            combined.SetPixels(0, 0, ImageSize, ImageSize, front.GetPixels());
            combined.SetPixels(ImageSize, 0, ImageSize, ImageSize, oblique.GetPixels());
            combined.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(front);
            UnityEngine.Object.DestroyImmediate(oblique);
            return combined;
        }

        private static Texture2D RenderView(Vector3 target, Vector3 direction, float distance)
        {
            reviewCamera.transform.position = target + direction * distance;
            reviewCamera.transform.rotation = Quaternion.LookRotation(target - reviewCamera.transform.position, Vector3.up);
            var renderTexture = RenderTexture.GetTemporary(
                ImageSize, ImageSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                reviewCamera.targetTexture = renderTexture;
                reviewCamera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                reviewCamera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void Finish()
        {
            var frameWidth = ImageSize * 2;
            var rows = Mathf.CeilToInt(CapturedImages.Count / (float)SheetColumns);
            var sheet = new Texture2D(frameWidth * SheetColumns, ImageSize * rows, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(new Color32(9, 12, 14, 255), sheet.width * sheet.height).ToArray());
            var frameDirectory = OstinatoHitRecoilAnimation.ProjectAbsolutePath(RuntimeFramesPath);
            Directory.CreateDirectory(frameDirectory);
            for (var index = 0; index < CapturedImages.Count; index++)
            {
                File.WriteAllBytes(Path.Combine(frameDirectory, "frame_" + index.ToString("D3") + ".png"),
                    CapturedImages[index]);
                var frame = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                frame.LoadImage(CapturedImages[index], false);
                var column = index % SheetColumns;
                var row = rows - 1 - index / SheetColumns;
                sheet.SetPixels(column * frameWidth, row * ImageSize, frameWidth, ImageSize, frame.GetPixels());
                UnityEngine.Object.DestroyImmediate(frame);
            }
            sheet.Apply(false, false);
            var imagePath = OstinatoHitRecoilAnimation.ProjectAbsolutePath(RuntimeImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ??
                                      throw new InvalidOperationException("Hit recoil capture folder is invalid."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);

            var report = new StringBuilder();
            report.AppendLine("Target=" + OstinatoHitRecoilAnimation.PlacementRootName + "/" +
                              OstinatoHitRecoilAnimation.HitSlotName);
            report.AppendLine("PlaybackMode=Unity Editor Play Mode scene Animator");
            report.AppendLine("AnimatorState=" + OstinatoHitRecoilAnimation.StateName);
            report.AppendLine("CapturedFrames=" + CapturedImages.Count);
            report.AppendLine("ViewsPerFrame=Front|ThreeQuarter");
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("MaximumObservedRootDistance=" + maximumRootDistance.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MaximumObservedRootRotation=" + maximumRootRotation.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("FinalImage=" + RuntimeImagePath);
            report.AppendLine("FrameDirectory=" + RuntimeFramesPath);
            OstinatoHitRecoilAnimation.WriteText(RuntimeReportPath, report.ToString());
            Cleanup();
            SessionState.SetInt(SessionKey, WaitingForEditMode);
            EditorApplication.ExitPlaymode();
        }

        private static void ConfigureCameraAndLights()
        {
            reviewCamera.clearFlags = CameraClearFlags.SolidColor;
            reviewCamera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            reviewCamera.fieldOfView = 40f;
            reviewCamera.nearClipPlane = 0.05f;
            reviewCamera.farClipPlane = 100f;
            reviewCamera.cullingMask = 1 << ReviewLayer;
            reviewCamera.allowHDR = true;
            reviewCamera.allowMSAA = true;
            var key = keyObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.45f;
            key.color = new Color(1f, 0.89f, 0.72f);
            key.cullingMask = 1 << ReviewLayer;
            keyObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            var fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.78f;
            fill.color = new Color(0.46f, 0.66f, 1f);
            fill.cullingMask = 1 << ReviewLayer;
            fillObject.transform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void Cleanup()
        {
            if (layeredObjects != null && originalLayers != null)
            {
                for (var index = 0; index < Mathf.Min(layeredObjects.Length, originalLayers.Length); index++)
                {
                    if (layeredObjects[index] != null) layeredObjects[index].layer = originalLayers[index];
                }
            }
            Destroy(cameraObject);
            Destroy(keyObject);
            Destroy(fillObject);
            animator = null;
            renderer = null;
            reviewCamera = null;
            cameraObject = null;
            keyObject = null;
            fillObject = null;
            layeredObjects = null;
            originalLayers = null;
            CapturedImages.Clear();
        }

        private static void Fail(Exception exception)
        {
            Cleanup();
            OstinatoHitRecoilAnimation.WriteText(FailurePath, exception.ToString());
            SessionState.EraseInt(SessionKey);
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.ExitPlaymode();
            Debug.LogException(exception);
        }

        private static void Destroy(GameObject target)
        {
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
        }

        private static void TryDelete(string relativePath)
        {
            var path = OstinatoHitRecoilAnimation.ProjectAbsolutePath(relativePath);
            if (File.Exists(path)) File.Delete(path);
        }

        private static void TryDeleteDirectory(string relativePath)
        {
            var path = OstinatoHitRecoilAnimation.ProjectAbsolutePath(relativePath);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }
}
