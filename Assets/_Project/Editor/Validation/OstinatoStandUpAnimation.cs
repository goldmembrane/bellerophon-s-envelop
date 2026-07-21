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
    internal static class OstinatoStandUpAnimation
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string PlacementRootName = "Approved Ostinato Enemy Placement";
        internal const string StaticSlotName = "Ostinato_01_Static_Review";
        internal const string OriginalSlotName = "Ostinato_08_Static_Review";
        internal const string StandUpSlotName = "Ostinato_08_Stand_Up";
        internal const string ModelName = "Ostinato_Model";
        internal const string StateName = "Ostinato_08_Stand_Up";
        // Slot 08 uses the supplied stand-up FBX as a looping review object.
        internal const string ValidationFolder =
            "docs/validation/ostinato_stand_up_fbx_replacement_2026-07-21";
        private const string FbxPath =
            "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_08_Stand_Up.fbx";
        private const string SourceRelativePath = "enemies model/ostinato stand up.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_08_Stand_Up.controller";
        private const string ApprovedModelPath =
            "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato_ApprovedUnity.fbx";
        private const string ClipName = "Ostinato_08_Stand_Up";
        private const string SelectedTakeName = "mixamo.com";
        private const int ExpectedSlotIndex = 7;
        private const string TargetReportPath = ValidationFolder + "/Ostinato_StandUpFbxReplacementTarget.txt";
        private const string ApplyReportPath = ValidationFolder + "/Ostinato_StandUpFbxReplacementApply.txt";
        private const string InspectionReportPath = ValidationFolder + "/Ostinato_StandUpFbxReplacementInspection.txt";

        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_SoftTissue.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_HookBlade.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_CompoundEye.mat",
        };

        public static void InspectOstinatoStandUpFbxReplacementTarget()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato stand up FBX importer is missing.");
            var defaults = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var selected = RequireSingleSelectedDefaultTake(defaults);
            var root = RequirePlacementRoot(scene);
            var slot = FindStandUpSlot(root);
            RequireSlotContract(slot);
            var model = RequireModel(slot);
            var sourceHash = ComputeSha256(ProjectAbsolutePath(SourceRelativePath));
            var importedHash = ComputeSha256(ProjectAbsolutePath(FbxPath));
            if (sourceHash != importedHash)
            {
                throw new InvalidOperationException("The project Ostinato stand up FBX differs from the supplied source.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Target=" + PlacementRootName + "/" + slot.name + "/" + model.name);
            report.AppendLine("TargetSiblingIndex=" + slot.GetSiblingIndex());
            report.AppendLine("CurrentPlaybackObjectId=" + GlobalObjectId.GetGlobalObjectIdSlow(model.gameObject));
            report.AppendLine("SourceFbx=" + SourceRelativePath);
            report.AppendLine("ImportedFbx=" + FbxPath);
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
            }
            report.AppendLine("SelectedTake=" + selected.takeName);
            report.AppendLine("SelectedTakeFirstFrame=" + Format(selected.firstFrame));
            report.AppendLine("SelectedTakeLastFrame=" + Format(selected.lastFrame));
            report.AppendLine("SceneChanged=False");
            WriteText(TargetReportPath, report.ToString());
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("StandUp FBX target inspection changed the scene dirty state.");
            }
            Debug.Log("OstinatoStandUpFbxReplacementTargetInspected Take=mixamo.com, Frames=" +
                      Format(selected.firstFrame) + ".." + Format(selected.lastFrame) +
                      ", SourceCopyHashesMatch=True, SceneChanged=False");
        }

        public static void ApplyOstinatoStandUpFbxReplacement()
        {
            var scene = RequireScene();
            var root = RequirePlacementRoot(scene);
            var slot = FindStandUpSlot(root);
            RequireSlotContract(slot);
            var slotSnapshot = new TransformSnapshot(slot);
            var otherSlotsBefore = CaptureOtherSlotSignatures(root, slot);
            var previousModel = RequireModel(slot);
            var previousObjectId = GlobalObjectId.GetGlobalObjectIdSlow(previousModel.gameObject).ToString();
            var modelPosition = previousModel.localPosition;
            var modelRotation = previousModel.localRotation;
            var modelScale = previousModel.localScale;
            var sourceHashBefore = ComputeSha256(ProjectAbsolutePath(SourceRelativePath));
            var importedHashBefore = ComputeSha256(ProjectAbsolutePath(FbxPath));
            if (sourceHashBefore != importedHashBefore)
            {
                throw new InvalidOperationException("The project Ostinato stand up FBX differs from the supplied source.");
            }

            ConfigureImporter();
            var overrideTake = RequireSelectedTakeContract(out var defaultTake);
            var clip = RequireClip();
            RequireClipContract(clip, defaultTake);
            var curveFingerprintBefore = BuildClipCurveFingerprint(clip);
            var controller = CreateOrUpdateController(clip);
            var standUpAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath) ??
                throw new InvalidOperationException("The imported Ostinato stand up FBX model is missing.");
            var staticSlot = root.Find(StaticSlotName) ??
                throw new InvalidOperationException("The approved static Ostinato slot is missing.");
            var approvedRenderer = RequireRenderer(staticSlot, "approved static slot");
            if (AssetDatabase.GetAssetPath(approvedRenderer.sharedMesh) != ApprovedModelPath)
            {
                throw new InvalidOperationException("The static Ostinato slot does not use the approved model mesh.");
            }
            var approvedMaterials = ApprovedMaterialPaths.Select(path =>
                AssetDatabase.LoadAssetAtPath<Material>(path) ??
                throw new InvalidOperationException("Approved Ostinato material is missing: " + path)).ToArray();

            var replacement = PrefabUtility.InstantiatePrefab(standUpAsset, scene) as GameObject ??
                throw new InvalidOperationException("The supplied Ostinato stand up FBX could not be instantiated.");
            replacement.name = ModelName;
            replacement.transform.SetParent(slot, false);
            replacement.transform.localPosition = modelPosition;
            replacement.transform.localRotation = modelRotation;
            replacement.transform.localScale = modelScale;
            var replacementRenderer = RequireRenderer(replacement.transform, "stand up replacement");
            SynchronizeApprovedAppearance(replacement, replacementRenderer, approvedRenderer, approvedMaterials);
            DisableOtherRenderers(replacement, replacementRenderer);
            RequireBindingsResolve(replacement.transform, clip);
            var animator = replacement.GetComponent<Animator>() ?? replacement.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.speed = 1f;
            animator.enabled = true;
            replacementRenderer.updateWhenOffscreen = true;

            UnityEngine.Object.DestroyImmediate(previousModel.gameObject);
            slot.name = StandUpSlotName;
            slotSnapshot.AssertUnchanged(slot);
            if (slot.childCount != 1 || slot.GetChild(0) != replacement.transform)
            {
                throw new InvalidOperationException("The old slot 08 object was not replaced exactly once.");
            }
            var replacementObjectId = GlobalObjectId.GetGlobalObjectIdSlow(replacement).ToString();
            if (replacementObjectId == previousObjectId)
            {
                throw new InvalidOperationException("The slot 08 playback object was not replaced.");
            }
            RequireOtherSlotsUnchanged(root, otherSlotsBefore);
            RequireSynchronizedAppearance(replacement, replacementRenderer, approvedRenderer);
            RequireControllerContract(animator, clip);
            var prefabSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(replacement);
            if (prefabSourcePath != FbxPath)
            {
                throw new InvalidOperationException("The replacement object is not an instance of the supplied stand up FBX. Actual=" +
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
                throw new InvalidOperationException("CargoRunMvp could not be saved after stand up FBX replacement.");
            }
            AssetDatabase.SaveAssets();

            var sourceHashAfter = ComputeSha256(ProjectAbsolutePath(SourceRelativePath));
            var importedHashAfter = ComputeSha256(ProjectAbsolutePath(FbxPath));
            var curveFingerprintAfter = BuildClipCurveFingerprint(RequireClip());
            if (sourceHashAfter != sourceHashBefore || importedHashAfter != importedHashBefore ||
                sourceHashAfter != importedHashAfter || curveFingerprintAfter != curveFingerprintBefore)
            {
                throw new InvalidOperationException("The supplied stand up FBX or its imported curves changed during replacement.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("Target=" + PlacementRootName + "/" + StandUpSlotName + "/" + ModelName);
            report.AppendLine("PreviousPlaybackObjectId=" + previousObjectId);
            report.AppendLine("ReplacementPlaybackObjectId=" + replacementObjectId);
            report.AppendLine("PlaybackObjectReplaced=True");
            report.AppendLine("PlaybackPrefabSource=" + prefabSourcePath);
            report.AppendLine("SelectedTake=" + overrideTake.takeName);
            report.AppendLine("DefaultTakeFirstFrame=" + Format(defaultTake.firstFrame));
            report.AppendLine("DefaultTakeLastFrame=" + Format(defaultTake.lastFrame));
            report.AppendLine("OverrideTakeFirstFrame=" + Format(overrideTake.firstFrame));
            report.AppendLine("OverrideTakeLastFrame=" + Format(overrideTake.lastFrame));
            report.AppendLine("FullSelectedTakePreserved=True");
            report.AppendLine("LoopTime=" + overrideTake.loopTime);
            report.AppendLine("PlaybackSpeed=1");
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("AnimationCurveFingerprintBefore=" + curveFingerprintBefore);
            report.AppendLine("AnimationCurveFingerprintAfter=" + curveFingerprintAfter);
            report.AppendLine("AnimationCurvesModified=False");
            report.AppendLine("ApprovedMesh=" + AssetDatabase.GetAssetPath(replacementRenderer.sharedMesh));
            report.AppendLine("ApprovedMaterials=" + string.Join("|", replacementRenderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
            report.AppendLine("AppearanceSynchronizedFrom=" + StaticSlotName);
            report.AppendLine("SourceSha256Before=" + sourceHashBefore);
            report.AppendLine("SourceSha256After=" + sourceHashAfter);
            report.AppendLine("ImportedSha256Before=" + importedHashBefore);
            report.AppendLine("ImportedSha256After=" + importedHashAfter);
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            report.AppendLine("SceneSaved=True");
            WriteText(ApplyReportPath, report.ToString());
            Selection.activeGameObject = replacement;
            Debug.Log("OstinatoStandUpFbxReplacementApplied Take=mixamo.com, Frames=" +
                      Format(overrideTake.firstFrame) + ".." + Format(overrideTake.lastFrame) +
                      ", Speed=1, ApplyRootMotion=False, ApprovedAppearance=True, OtherSlotsUnchanged=True");
        }

        public static void InspectOstinatoStandUpFbxReplacement()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var slot = root.Find(StandUpSlotName) ??
                throw new InvalidOperationException("The Ostinato stand up slot is missing.");
            RequireSlotContract(slot);
            var model = RequireModel(slot);
            var prefabSourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject);
            if (prefabSourcePath != FbxPath)
            {
                throw new InvalidOperationException("The slot 08 object is not sourced from the supplied stand up FBX.");
            }
            var staticSlot = root.Find(StaticSlotName) ??
                throw new InvalidOperationException("The approved static Ostinato slot is missing.");
            var approvedRenderer = RequireRenderer(staticSlot, "approved static slot");
            var renderer = RequireRenderer(model, "stand up replacement");
            RequireSynchronizedAppearance(model.gameObject, renderer, approvedRenderer);
            var clip = RequireClip();
            var selectedTake = RequireSelectedTakeContract(out var defaultTake);
            RequireClipContract(clip, defaultTake);
            RequireBindingsResolve(model, clip);
            var animator = model.GetComponent<Animator>() ??
                throw new InvalidOperationException("The Ostinato stand up replacement Animator is missing.");
            RequireControllerContract(animator, clip);
            var sourceHash = ComputeSha256(ProjectAbsolutePath(SourceRelativePath));
            var importedHash = ComputeSha256(ProjectAbsolutePath(FbxPath));
            if (sourceHash != importedHash)
            {
                throw new InvalidOperationException("The supplied and imported Ostinato stand up FBX hashes differ.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("Target=" + PlacementRootName + "/" + StandUpSlotName + "/" + ModelName);
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
            report.AppendLine("AppearanceSynchronizedFrom=" + StaticSlotName);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("ImportedSha256=" + importedHash);
            report.AppendLine("SourceCopyHashesMatch=True");
            report.AppendLine("SceneChanged=False");
            WriteText(InspectionReportPath, report.ToString());
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException("StandUp FBX replacement inspection changed the scene dirty state.");
            }
            Debug.Log("OstinatoStandUpFbxReplacementInspected Result=PASS, Take=mixamo.com, FullTake=True, " +
                      "Speed=1, ApplyRootMotion=False, ApprovedAppearance=True, SceneChanged=False");
        }

        public static void CaptureOstinatoStandUpFbxReplacement()
        {
            InspectOstinatoStandUpFbxReplacement();
            OstinatoStandUpRuntimeCapture.Begin();
        }

        private static void ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato stand up FBX importer is missing.");
            var defaults = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            var selected = RequireSingleSelectedDefaultTake(defaults);
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

        private static ModelImporterClipAnimation RequireSingleSelectedDefaultTake(
            IEnumerable<ModelImporterClipAnimation> defaults)
        {
            var matches = defaults.Where(take => take.takeName == SelectedTakeName || take.name == SelectedTakeName).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("The supplied stand up FBX must expose exactly one mixamo.com take. Matches=" +
                                                    matches.Length);
            }
            return matches[0];
        }

        private static ModelImporterClipAnimation RequireSelectedTakeContract(
            out ModelImporterClipAnimation defaultTake)
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato stand up FBX importer is missing.");
            defaultTake = RequireSingleSelectedDefaultTake(
                importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>());
            var overrides = importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
            if (overrides.Length != 1)
            {
                throw new InvalidOperationException("The stand up FBX must import exactly one selected take. Count=" + overrides.Length);
            }
            var selected = overrides[0];
            if (selected.name != ClipName || selected.takeName != SelectedTakeName ||
                !Mathf.Approximately(selected.firstFrame, defaultTake.firstFrame) ||
                !Mathf.Approximately(selected.lastFrame, defaultTake.lastFrame) ||
                !selected.loopTime || selected.loopPose)
            {
                throw new InvalidOperationException("The mixamo.com take is not configured as an untrimmed repeating override.");
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

        private static AnimationClip RequireClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)).ToArray();
            var matches = clips.Where(clip => clip.name == ClipName).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("The stand up FBX must expose exactly one selected clip. Matches=" +
                                                    matches.Length + ", All=" + string.Join("|", clips.Select(clip => clip.name)));
            }
            return matches[0];
        }

        private static void RequireClipContract(AnimationClip clip, ModelImporterClipAnimation defaultTake)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0 || clip.frameRate <= 0f)
            {
                throw new InvalidOperationException("The selected mixamo.com stand up clip has no usable animation curves.");
            }
            var expectedLength = (defaultTake.lastFrame - defaultTake.firstFrame) / clip.frameRate;
            if (Mathf.Abs(clip.length - expectedLength) > (1f / clip.frameRate + 0.001f))
            {
                throw new InvalidOperationException("The imported stand up clip does not preserve the full selected take. ExpectedLength=" +
                                                    Format(expectedLength) + ", Actual=" + Format(clip.length));
            }
            if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("The selected mixamo.com stand up clip is not configured to loop.");
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
                throw new InvalidOperationException("Ostinato stand up controller must contain one layer.");
            }
            var machine = layers[0].stateMachine;
            foreach (var child in machine.states.ToArray()) machine.RemoveState(child.state);
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
                throw new InvalidOperationException("Ostinato stand up Animator Controller is missing.");
            if (AssetDatabase.GetAssetPath(controller) != ControllerPath || animator.applyRootMotion ||
                !Mathf.Approximately(animator.speed, 1f) || !animator.enabled)
            {
                throw new InvalidOperationException("Ostinato stand up Animator settings changed.");
            }
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states).ToArray();
            if (states.Length != 1 || states[0].state.name != StateName || states[0].state.motion != clip ||
                !Mathf.Approximately(states[0].state.speed, 1f))
            {
                throw new InvalidOperationException("Ostinato stand up controller state contract changed.");
            }
        }

        private static void RequireBindingsResolve(Transform model, AnimationClip clip)
        {
            var unresolved = AnimationUtility.GetCurveBindings(clip)
                .Where(binding => !string.IsNullOrEmpty(binding.path) && model.Find(binding.path) == null)
                .Select(binding => binding.path + ":" + binding.propertyName).ToArray();
            if (unresolved.Length > 0)
            {
                throw new InvalidOperationException("The stand up clip has unresolved bindings: " + string.Join("|", unresolved));
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
                    throw new InvalidOperationException("The stand up FBX rig cannot map approved bone: " + approvedBone.name);
                }
                return matches[0];
            }).ToArray();
            var rootMatches = replacementTransforms.Where(candidate =>
                candidate.name == approvedRenderer.rootBone.name).ToArray();
            if (rootMatches.Length != 1)
            {
                throw new InvalidOperationException("The stand up FBX rig cannot map the approved root bone.");
            }
            replacementRenderer.sharedMesh = approvedRenderer.sharedMesh;
            replacementRenderer.bones = mappedBones;
            replacementRenderer.rootBone = rootMatches[0];
            replacementRenderer.sharedMaterials = approvedMaterials;
        }

        private static void DisableOtherRenderers(GameObject replacement, Renderer approvedRenderer)
        {
            foreach (var renderer in replacement.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != approvedRenderer) renderer.enabled = false;
            }
        }

        private static void RequireSynchronizedAppearance(
            GameObject replacement,
            SkinnedMeshRenderer renderer,
            SkinnedMeshRenderer approvedRenderer)
        {
            if (renderer.sharedMesh != approvedRenderer.sharedMesh)
            {
                throw new InvalidOperationException("The stand up replacement does not use the approved static Ostinato mesh.");
            }
            if (!renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).SequenceEqual(ApprovedMaterialPaths))
            {
                throw new InvalidOperationException("The stand up replacement does not use the four approved materials.");
            }
            var approvedBoneNames = approvedRenderer.bones.Select(bone => bone.name).ToArray();
            var replacementBoneNames = renderer.bones.Select(bone => bone == null ? string.Empty : bone.name).ToArray();
            if (!approvedBoneNames.SequenceEqual(replacementBoneNames) || renderer.rootBone == null ||
                approvedRenderer.rootBone == null || renderer.rootBone.name != approvedRenderer.rootBone.name)
            {
                throw new InvalidOperationException("The approved appearance is not mapped to the stand up FBX rig.");
            }
            if (replacement.GetComponentsInChildren<Renderer>(true).Any(candidate =>
                    candidate != renderer && candidate.enabled))
            {
                throw new InvalidOperationException("The stand up FBX has an unsynchronized visible renderer.");
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

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("CargoRunMvp must be active in Edit Mode.");
            }
            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            return scene.GetRootGameObjects().Single(target => target.name == PlacementRootName).transform;
        }

        private static Transform FindStandUpSlot(Transform root)
        {
            var original = root.Find(OriginalSlotName);
            var applied = root.Find(StandUpSlotName);
            if (original != null && applied != null)
            {
                throw new InvalidOperationException("Both static and stand up slot 08 exist.");
            }
            return applied ?? original ?? throw new InvalidOperationException("Ostinato slot 08 is missing.");
        }

        private static void RequireSlotContract(Transform slot)
        {
            if (slot.GetSiblingIndex() != ExpectedSlotIndex || slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato stand up replacement target must be slot 08 with one child.");
            }
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
            {
                throw new InvalidOperationException("Ostinato slot 08 must contain one Ostinato_Model child.");
            }
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(Transform target, string label)
        {
            var renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1 || renderers[0].sharedMesh == null)
            {
                throw new InvalidOperationException(label + " must contain one valid SkinnedMeshRenderer.");
            }
            return renderers[0];
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
            return "(" + Format(value.x) + "," + Format(value.y) + "," + Format(value.z) + ")";
        }

        private static string Format(Quaternion value)
        {
            return "(" + Format(value.x) + "," + Format(value.y) + "," +
                   Format(value.z) + "," + Format(value.w) + ")";
        }

        private sealed class TransformSnapshot
        {
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private readonly int siblingIndex;

            public TransformSnapshot(Transform target)
            {
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
                siblingIndex = target.GetSiblingIndex();
            }

            public void AssertUnchanged(Transform target)
            {
                if (target.localPosition != position || target.localRotation != rotation ||
                    target.localScale != scale || target.GetSiblingIndex() != siblingIndex)
                {
                    throw new InvalidOperationException("The slot 08 Transform changed during replacement.");
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class OstinatoStandUpRuntimeCapture
    {
        private const string SessionKey = "Bellerophon.OstinatoStandUpRuntimeCapture.State";
        private const int WaitingForPlayMode = 1;
        private const int Capturing = 2;
        private const int WaitingForEditMode = 3;
        private const int ReviewLayer = 29;
        private const int ImageSize = 320;
        private const int SheetColumns = 5;
        private const int CaptureFrameCount = 15;
        private const string RuntimeFramesPath =
            OstinatoStandUpAnimation.ValidationFolder + "/runtime_frames";
        private const string RuntimeImagePath =
            OstinatoStandUpAnimation.ValidationFolder + "/Ostinato_StandUp_RuntimeContactSheet.png";
        private const string RuntimeReportPath =
            OstinatoStandUpAnimation.ValidationFolder + "/Ostinato_StandUp_RuntimePlayback.txt";
        private const string CompletionPath =
            OstinatoStandUpAnimation.ValidationFolder + "/Ostinato_StandUp_RuntimePlayback.completed";
        private const string FailurePath =
            OstinatoStandUpAnimation.ValidationFolder + "/Ostinato_StandUp_RuntimePlayback.failed.txt";

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

        static OstinatoStandUpRuntimeCapture()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Unity must be in Edit Mode before stand up capture begins.");
            }
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != OstinatoStandUpAnimation.ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be active before stand up capture.");
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
                        throw new InvalidOperationException("Unity left Play Mode before stand up capture completed.");
                    }
                    CaptureWhenDue();
                    return;
                }
                if (state == WaitingForEditMode && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.EraseInt(SessionKey);
                    OstinatoStandUpAnimation.WriteText(
                        CompletionPath,
                        "Ostinato stand up capture completed in Unity Editor Play Mode.");
                    Debug.Log("OstinatoStandUpCaptured Frames=" + CaptureFrameCount +
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
                target.name == OstinatoStandUpAnimation.PlacementRootName).transform;
            var slot = root.Find(OstinatoStandUpAnimation.StandUpSlotName) ??
                throw new InvalidOperationException("Ostinato stand up slot is missing in Play Mode.");
            var model = slot.childCount == 1 ? slot.GetChild(0) : null;
            if (model == null || model.name != OstinatoStandUpAnimation.ModelName)
            {
                throw new InvalidOperationException("Ostinato stand up model is invalid in Play Mode.");
            }
            animator = model.GetComponent<Animator>();
            renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
            if (animator == null || animator.runtimeAnimatorController == null || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Ostinato stand up runtime Animator is invalid.");
            }

            layeredObjects = model.GetComponentsInChildren<Transform>(true)
                .Select(target => target.gameObject).ToArray();
            originalLayers = layeredObjects.Select(target => target.layer).ToArray();
            foreach (var target in layeredObjects) target.layer = ReviewLayer;
            cameraObject = new GameObject("Ostinato_StandUp_ReviewCamera", typeof(Camera));
            keyObject = new GameObject("Ostinato_StandUp_KeyLight", typeof(Light));
            fillObject = new GameObject("Ostinato_StandUp_FillLight", typeof(Light));
            reviewCamera = cameraObject.GetComponent<Camera>();
            ConfigureCameraAndLights();
            modelStartPosition = model.position;
            modelStartRotation = model.rotation;
            animator.Play(OstinatoStandUpAnimation.StateName, 0, 0f);
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
            if (EditorApplication.timeSinceStartup - captureStartTime > 15d)
            {
                throw new TimeoutException("Ostinato stand up Animator capture timed out.");
            }
            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(OstinatoStandUpAnimation.StateName))
            {
                throw new InvalidOperationException("Ostinato stand up Animator left its state.");
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
            var frameDirectory = OstinatoStandUpAnimation.ProjectAbsolutePath(RuntimeFramesPath);
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
            var imagePath = OstinatoStandUpAnimation.ProjectAbsolutePath(RuntimeImagePath);
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ??
                                      throw new InvalidOperationException("StandUp capture folder is invalid."));
            File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);

            var report = new StringBuilder();
            report.AppendLine("Target=" + OstinatoStandUpAnimation.PlacementRootName + "/" +
                              OstinatoStandUpAnimation.StandUpSlotName);
            report.AppendLine("PlaybackMode=Unity Editor Play Mode scene Animator");
            report.AppendLine("AnimatorState=" + OstinatoStandUpAnimation.StateName);
            report.AppendLine("CapturedFrames=" + CapturedImages.Count);
            report.AppendLine("ViewsPerFrame=Front|ThreeQuarter");
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            report.AppendLine("MaximumObservedRootDistance=" +
                              maximumRootDistance.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("MaximumObservedRootRotation=" +
                              maximumRootRotation.ToString("0.######", CultureInfo.InvariantCulture));
            report.AppendLine("FinalImage=" + RuntimeImagePath);
            report.AppendLine("FrameDirectory=" + RuntimeFramesPath);
            OstinatoStandUpAnimation.WriteText(RuntimeReportPath, report.ToString());
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
            OstinatoStandUpAnimation.WriteText(FailurePath, exception.ToString());
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
            var path = OstinatoStandUpAnimation.ProjectAbsolutePath(relativePath);
            if (File.Exists(path)) File.Delete(path);
        }

        private static void TryDeleteDirectory(string relativePath)
        {
            var path = OstinatoStandUpAnimation.ProjectAbsolutePath(relativePath);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }
}

