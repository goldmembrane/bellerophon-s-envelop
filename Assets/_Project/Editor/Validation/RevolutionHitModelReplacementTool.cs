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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RevolutionCargoRunScene
{
    internal static class RevolutionHitModelReplacementTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceFbxPath =
            "D:/Bellerophon2/Bellerophon/enemies model/révolution hit.fbx";
        private const string ImportedFbxPath =
            "Assets/_Project/Art/Enemies/Revolution/Models/RevolutionHit.fbx";
        private const string ApprovedAppearanceFbxPath =
            "Assets/_Project/Art/Enemies/Revolution/ApprovedAppearance/Models/Revolution_ApprovedAppearance.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Revolution/Controllers/Revolution_06_Hit.controller";
        private const string StaticArmClipPath =
            "Assets/_Project/Art/Enemies/Revolution/Animations/Revolution_06_Hit_StaticArms.anim";
        private const string ValidationFolder =
            "docs/validation/revolution_hit_2026-07-29";
        private const string InspectionPath =
            ValidationFolder + "/Revolution_06_Hit_Inspection.txt";
        private const string CapturePath =
            ValidationFolder + "/Revolution_06_Hit_VisualReview.png";
        private const string SourceSha256 =
            "F8637B24BA6F08A94D5FAFB9A51A61BFDAD57C2666F40CDD3E893303CE429F8D";
        private const string PlacementRootName =
            "Approved Revolution Enemy Placement";
        private const string StaticSlotName = "Revolution_01";
        private const string HitSlotName = "Revolution_06";
        private const string ReplacementModelName =
            "Revolution_Hit_Model";
        private const string MixamoTakeMarker = "mixamo.com";
        private const string ImportedClipName =
            "Revolution_06_Hit_Mixamo";
        private const string StaticArmClipName =
            "Revolution_06_Hit_StaticArms";
        private const int ExpectedTriangleCount = 3945;
        private const int ExpectedBoneCount = 24;
        private const int ExpectedApprovedMaterialCount = 8;
        private const int ReviewLayer = 30;
        private const int ReviewHiddenLayer = 29;
        private const int PanelSize = 320;
        private static readonly string[] StaticArmBoneNames =
        {
            "LeftShoulder",
            "LeftArm",
            "LeftForeArm",
            "LeftHand",
            "RightShoulder",
            "RightArm",
            "RightForeArm",
            "RightHand"
        };

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply Hit Model Replacement")]
        public static void ApplyRevolutionHit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException(
                    "Revolution hit replacement requires Edit Mode. Play Mode exit was requested; run the command again after Unity returns to Edit Mode.");
            }

            RequireHash(SourceFbxPath, SourceSha256);
            CopySourceFbx();
            ConfigureImporter();
            RequireHash(ImportedFbxPath, SourceSha256);

            var importedPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ImportedFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Revolution hit FBX is missing.",
                    ImportedFbxPath);
            var importedClip = RequireImportedMixamoClip();
            var importedAvatar =
                AssetDatabase.LoadAllAssetsAtPath(ImportedFbxPath)
                    .OfType<Avatar>()
                    .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Revolution hit FBX did not produce exactly one Generic Avatar.");
            var importedRenderer =
                RequireMainRenderer(
                    importedPrefab.transform,
                    "imported Revolution hit FBX");
            RequireAuthoredGeometry(importedRenderer);
            var controller = CreateOrUpdateController(importedClip);

            var scene = RequireCurrentScene();
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot =
                RequireDirectChild(placementRoot, StaticSlotName);
            var hitSlot =
                RequireDirectChild(placementRoot, HitSlotName);
            if (hitSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_06 must contain exactly one model before replacement.");
            }

            var staticModel = staticSlot.GetChild(0);
            var previousModel = hitSlot.GetChild(0);
            var staticMainRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01 static approved model");
            RequireApprovedStaticAppearance(staticMainRenderer);
            RequireMatchingBoneNames(
                staticMainRenderer,
                importedRenderer,
                "Revolution hit rig");

            var slotPositionBefore = hitSlot.localPosition;
            var slotRotationBefore = hitSlot.localRotation;
            var slotScaleBefore = hitSlot.localScale;
            var otherSlotsBefore =
                CaptureOtherSlotSignatures(placementRoot, hitSlot);
            var previousLocalPosition = previousModel.localPosition;
            var previousLocalRotation = previousModel.localRotation;
            var previousLocalScale = previousModel.localScale;

            var replacement =
                PrefabUtility.InstantiatePrefab(importedPrefab, scene) as
                    GameObject ??
                throw new InvalidOperationException(
                    "Revolution hit FBX could not be instantiated.");
            replacement.name = ReplacementModelName;
            replacement.transform.SetParent(hitSlot, false);
            replacement.transform.SetLocalPositionAndRotation(
                previousLocalPosition,
                previousLocalRotation);
            replacement.transform.localScale = previousLocalScale;

            try
            {
                SynchronizeAppearance(
                    staticModel,
                    replacement.transform);
                var replacementMainRenderer =
                    RequireMainRenderer(
                        replacement.transform,
                        "Revolution_06 hit model");
                RequireMatchingBoneNames(
                    importedRenderer,
                    replacementMainRenderer,
                    "instantiated Revolution hit rig");
                RequireAppearanceSynchronized(
                    staticModel,
                    replacement.transform);

                var animator = replacement.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = replacement.AddComponent<Animator>();
                }

                animator.runtimeAnimatorController = controller;
                animator.avatar = importedAvatar;
                animator.applyRootMotion = false;
                animator.cullingMode =
                    AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    animator);
                RequireAnimator(
                    animator,
                    controller,
                    importedClip);

                if (replacement.transform.localPosition !=
                        previousLocalPosition ||
                    replacement.transform.localRotation !=
                        previousLocalRotation ||
                    replacement.transform.localScale !=
                        previousLocalScale)
                {
                    throw new InvalidOperationException(
                        "The replacement model did not preserve the previous Revolution_06 local transform.");
                }
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(
                previousModel.gameObject);
            if (hitSlot.childCount != 1 ||
                hitSlot.GetChild(0) != replacement.transform)
            {
                throw new InvalidOperationException(
                    "Revolution_06 replacement did not leave exactly one hit model.");
            }

            RequireSlotTransformUnchanged(
                hitSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                hitSlot,
                otherSlotsBefore);
            RequirePrefabSource(replacement.transform);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            Directory.CreateDirectory(Absolute(ValidationFolder));
            CaptureVisualReview(
                staticSlot,
                hitSlot,
                replacement,
                importedClip);
            RevertSampledEulerHintOverrides(
                replacement.transform);
            WriteInspection(
                staticModel,
                replacement.transform,
                importedClip,
                controller);

            RequireSlotTransformUnchanged(
                hitSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                hitSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(hitSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Revolution_06 hit replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ImportedFbxPath, SourceSha256);
            Selection.activeGameObject = hitSlot.gameObject;
            Debug.Log(
                "RevolutionHitAttackApplied" +
                ", Slot=" + HitSlotName +
                ", Source=" + ImportedFbxPath +
                ", Clip=" + importedClip.name +
                ", ClipLength=" +
                importedClip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", Loop=True" +
                ", RootMotion=False" +
                ", StaticAppearanceDirectReference=True" +
                ", NewAppearanceAssets=False" +
                ", OtherSlotsUnchanged=True" +
                ", Capture=" + CapturePath + ".");

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply Hit Static Arm Pose")]
        public static void ApplyRevolutionHitStaticArmPose()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException(
                    "Revolution hit arm-pose correction requires Edit Mode. Play Mode exit was requested; run the command again after Unity returns to Edit Mode.");
            }

            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ImportedFbxPath, SourceSha256);
            var sourceClip = RequireImportedMixamoClip();
            var scene = RequireCurrentScene();
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot =
                RequireDirectChild(placementRoot, StaticSlotName);
            var hitSlot =
                RequireDirectChild(placementRoot, HitSlotName);
            if (staticSlot.childCount != 1 ||
                hitSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_01 and Revolution_06 must each contain exactly one model.");
            }

            var staticModel = staticSlot.GetChild(0);
            var hitModel = hitSlot.GetChild(0);
            var staticRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01 static approved model");
            RequireApprovedStaticAppearance(staticRenderer);
            RequirePrefabSource(hitModel);
            RequireAppearanceSynchronized(
                staticModel,
                hitModel);

            var slotPositionBefore = hitSlot.localPosition;
            var slotRotationBefore = hitSlot.localRotation;
            var slotScaleBefore = hitSlot.localScale;
            var otherSlotsBefore =
                CaptureOtherSlotSignatures(placementRoot, hitSlot);
            var correctedClip =
                CreateOrUpdateStaticArmClip(
                    sourceClip,
                    staticModel,
                    hitModel);
            var controller =
                CreateOrUpdateController(correctedClip);
            var animator =
                hitModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Revolution_06 Animator is missing.");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode =
                AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                animator);
            RequireAnimator(
                animator,
                controller,
                correctedClip);
            RequireStaticArmClipContract(
                sourceClip,
                correctedClip,
                staticModel,
                hitModel);
            RequireSlotTransformUnchanged(
                hitSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                hitSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                hitModel);

            Directory.CreateDirectory(Absolute(ValidationFolder));
            CaptureVisualReview(
                staticSlot,
                hitSlot,
                hitModel.gameObject,
                correctedClip);
            RevertSampledEulerHintOverrides(hitModel);
            WriteInspection(
                staticModel,
                hitModel,
                correctedClip,
                controller);
            AppendStaticArmInspection(
                sourceClip,
                correctedClip,
                staticModel,
                hitModel);

            RequireStaticArmClipContract(
                sourceClip,
                correctedClip,
                staticModel,
                hitModel);
            RequireSlotTransformUnchanged(
                hitSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                hitSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                hitModel);

            EditorUtility.SetDirty(hitModel.gameObject);
            EditorUtility.SetDirty(hitSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Revolution_06 static-arm correction.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ImportedFbxPath, SourceSha256);
            Selection.activeGameObject = hitSlot.gameObject;
            Debug.Log(
                "RevolutionHitStaticArmPoseApplied" +
                ", Slot=" + HitSlotName +
                ", SourceClip=" + sourceClip.name +
                ", CorrectedClip=" + correctedClip.name +
                ", UpdatedBones=" +
                string.Join(",", StaticArmBoneNames) +
                ", ArmRotationOnly=True" +
                ", OtherCurvesChanged=False" +
                ", OtherSlotsUnchanged=True" +
                ", Capture=" + CapturePath + ".");

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.EnterPlaymode();
                }
            };
        }

        private static AnimationClip CreateOrUpdateStaticArmClip(
            AnimationClip sourceClip,
            Transform staticModel,
            Transform hitModel)
        {
            var armPaths =
                RequireMatchingStaticArmPaths(
                    staticModel,
                    hitModel);
            var candidate = new AnimationClip();
            try
            {
                EditorUtility.CopySerialized(
                    sourceClip,
                    candidate);
                candidate.name = StaticArmClipName;
                foreach (var binding in
                         AnimationUtility
                             .GetCurveBindings(candidate)
                             .Where(binding =>
                                 IsEditableArmRotation(
                                     binding,
                                     armPaths.Values))
                             .ToArray())
                {
                    AnimationUtility.SetEditorCurve(
                        candidate,
                        binding,
                        null);
                }

                foreach (var boneName in StaticArmBoneNames)
                {
                    var staticBone =
                        RequireUniqueDescendant(
                            staticModel,
                            boneName);
                    SetConstantQuaternionCurves(
                        candidate,
                        armPaths[boneName],
                        staticBone.localRotation,
                        sourceClip.length);
                }

                var settings =
                    AnimationUtility.GetAnimationClipSettings(
                        sourceClip);
                settings.loopTime = true;
                settings.loopBlend = true;
                AnimationUtility.SetAnimationClipSettings(
                    candidate,
                    settings);
                candidate.wrapMode = WrapMode.Loop;
                candidate.frameRate = sourceClip.frameRate;
                candidate.EnsureQuaternionContinuity();
                RequireStaticArmClipContract(
                    sourceClip,
                    candidate,
                    staticModel,
                    hitModel);

                var correctedClip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        StaticArmClipPath);
                if (correctedClip == null)
                {
                    AssetDatabase.CreateAsset(
                        candidate,
                        StaticArmClipPath);
                    candidate = null;
                }
                else
                {
                    EditorUtility.CopySerialized(
                        candidate,
                        correctedClip);
                    correctedClip.name = StaticArmClipName;
                    EditorUtility.SetDirty(correctedClip);
                }
            }
            finally
            {
                if (candidate != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        candidate);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                StaticArmClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            var saved =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    StaticArmClipPath) ??
                throw new InvalidOperationException(
                    "Revolution_06 corrected static-arm clip was not saved.");
            RequireStaticArmClipContract(
                sourceClip,
                saved,
                staticModel,
                hitModel);
            return saved;
        }

        private static SortedDictionary<string, string>
            RequireMatchingStaticArmPaths(
                Transform staticModel,
                Transform hitModel)
        {
            var result =
                new SortedDictionary<string, string>(
                    StringComparer.Ordinal);
            foreach (var boneName in StaticArmBoneNames)
            {
                var staticPath =
                    AnimationUtility.CalculateTransformPath(
                        RequireUniqueDescendant(
                            staticModel,
                            boneName),
                        staticModel);
                var hitPath =
                    AnimationUtility.CalculateTransformPath(
                        RequireUniqueDescendant(
                            hitModel,
                            boneName),
                        hitModel);
                if (!string.Equals(
                        staticPath,
                        hitPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        boneName +
                        " path differs between Revolution_01 and Revolution_06. Static=" +
                        staticPath + ", Hit=" + hitPath + ".");
                }

                result.Add(boneName, hitPath);
            }

            return result;
        }

        private static Transform RequireUniqueDescendant(
            Transform root,
            string name)
        {
            var matches =
                root.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name == name)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    root.name + " must contain exactly one " +
                    name + " bone. Found=" +
                    matches.Length + ".");
            }

            return matches[0];
        }

        private static void RequireStaticArmClipContract(
            AnimationClip sourceClip,
            AnimationClip correctedClip,
            Transform staticModel,
            Transform hitModel)
        {
            var armPaths =
                RequireMatchingStaticArmPaths(
                    staticModel,
                    hitModel);
            var editablePaths =
                new HashSet<string>(
                    armPaths.Values,
                    StringComparer.Ordinal);
            if (Mathf.Abs(
                    sourceClip.length -
                    correctedClip.length) > 0.000001f ||
                Mathf.Abs(
                    sourceClip.frameRate -
                    correctedClip.frameRate) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Revolution_06 static-arm correction changed clip length or frame rate.");
            }

            var sourceProtected =
                ProtectedCurveSignature(
                    sourceClip,
                    editablePaths);
            var correctedProtected =
                ProtectedCurveSignature(
                    correctedClip,
                    editablePaths);
            if (!string.Equals(
                    sourceProtected,
                    correctedProtected,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Revolution_06 static-arm correction changed a non-arm-rotation curve.");
            }

            if (!string.Equals(
                    ObjectReferenceCurveSignature(sourceClip),
                    ObjectReferenceCurveSignature(correctedClip),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    AnimationEventSignature(sourceClip),
                    AnimationEventSignature(correctedClip),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Revolution_06 static-arm correction changed object-reference curves or animation events.");
            }

            var correctedArmBindings =
                AnimationUtility.GetCurveBindings(correctedClip)
                    .Where(binding =>
                        IsEditableArmRotation(
                            binding,
                            editablePaths))
                    .ToArray();
            if (correctedArmBindings.Length !=
                    StaticArmBoneNames.Length * 4 ||
                correctedArmBindings.Any(binding =>
                    !binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "Revolution_06 corrected arm rotation bindings differ. Found=" +
                    correctedArmBindings.Length + ".");
            }

            foreach (var boneName in StaticArmBoneNames)
            {
                var expected =
                    RequireUniqueDescendant(
                            staticModel,
                            boneName)
                        .localRotation;
                var path = armPaths[boneName];
                RequireConstantRotationComponent(
                    correctedClip,
                    path,
                    "m_LocalRotation.x",
                    expected.x);
                RequireConstantRotationComponent(
                    correctedClip,
                    path,
                    "m_LocalRotation.y",
                    expected.y);
                RequireConstantRotationComponent(
                    correctedClip,
                    path,
                    "m_LocalRotation.z",
                    expected.z);
                RequireConstantRotationComponent(
                    correctedClip,
                    path,
                    "m_LocalRotation.w",
                    expected.w);
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(
                    correctedClip);
            if (!settings.loopTime ||
                !correctedClip.isLooping ||
                correctedClip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(
                    "Revolution_06 corrected static-arm clip is not configured to loop.");
            }
        }

        private static bool IsEditableArmRotation(
            EditorCurveBinding binding,
            IEnumerable<string> editablePaths)
        {
            if (!editablePaths.Contains(
                    binding.path,
                    StringComparer.Ordinal))
            {
                return false;
            }

            return
                binding.propertyName.StartsWith(
                    "m_LocalRotation.",
                    StringComparison.Ordinal) ||
                binding.propertyName.StartsWith(
                    "localEulerAnglesRaw.",
                    StringComparison.Ordinal) ||
                binding.propertyName.StartsWith(
                    "localEulerAnglesBaked.",
                    StringComparison.Ordinal) ||
                binding.propertyName.StartsWith(
                    "localEulerAngles.",
                    StringComparison.Ordinal);
        }

        private static string ProtectedCurveSignature(
            AnimationClip clip,
            ISet<string> editablePaths)
        {
            var builder = new StringBuilder();
            foreach (var binding in
                     AnimationUtility.GetCurveBindings(clip)
                         .Where(binding =>
                             !IsEditableArmRotation(
                                 binding,
                                 editablePaths))
                         .OrderBy(
                             binding => binding.path,
                             StringComparer.Ordinal)
                         .ThenBy(
                             binding => binding.propertyName,
                             StringComparer.Ordinal))
            {
                var curve =
                    AnimationUtility.GetEditorCurve(
                        clip,
                        binding) ??
                    throw new InvalidOperationException(
                        "A protected Revolution_06 animation curve is missing.");
                builder.Append(binding.path);
                builder.Append('|');
                builder.Append(binding.type.FullName);
                builder.Append('|');
                builder.Append(binding.propertyName);
                builder.Append('|');
                builder.Append((int)curve.preWrapMode);
                builder.Append('|');
                builder.Append((int)curve.postWrapMode);
                foreach (var key in curve.keys)
                {
                    builder.Append('|');
                    builder.Append(
                        key.time.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                    builder.Append(',');
                    builder.Append(
                        key.value.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                    builder.Append(',');
                    builder.Append(
                        key.inTangent.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                    builder.Append(',');
                    builder.Append(
                        key.outTangent.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                    builder.Append(',');
                    builder.Append(
                        key.inWeight.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                    builder.Append(',');
                    builder.Append(
                        key.outWeight.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                    builder.Append(',');
                    builder.Append((int)key.weightedMode);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string ObjectReferenceCurveSignature(
            AnimationClip clip)
        {
            var builder = new StringBuilder();
            foreach (var binding in
                     AnimationUtility
                         .GetObjectReferenceCurveBindings(clip)
                         .OrderBy(
                             binding => binding.path,
                             StringComparer.Ordinal)
                         .ThenBy(
                             binding => binding.propertyName,
                             StringComparer.Ordinal))
            {
                builder.Append(binding.path);
                builder.Append('|');
                builder.Append(binding.type.FullName);
                builder.Append('|');
                builder.Append(binding.propertyName);
                foreach (var key in
                         AnimationUtility.GetObjectReferenceCurve(
                             clip,
                             binding))
                {
                    builder.Append('|');
                    builder.Append(
                        key.time.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                    builder.Append(',');
                    builder.Append(
                        key.value != null
                            ? AssetDatabase.GetAssetPath(key.value) +
                              ":" + key.value.name
                            : "<null>");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string AnimationEventSignature(
            AnimationClip clip)
        {
            var builder = new StringBuilder();
            foreach (var item in
                     AnimationUtility.GetAnimationEvents(clip))
            {
                builder.Append(
                    item.time.ToString(
                        "R",
                        CultureInfo.InvariantCulture));
                builder.Append('|');
                builder.Append(item.functionName);
                builder.Append('|');
                builder.Append(item.stringParameter);
                builder.Append('|');
                builder.Append(item.floatParameter.ToString(
                    "R",
                    CultureInfo.InvariantCulture));
                builder.Append('|');
                builder.Append(item.intParameter);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void SetConstantQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion rotation,
            float endTime)
        {
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.x",
                rotation.x,
                endTime);
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.y",
                rotation.y,
                endTime);
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.z",
                rotation.z,
                endTime);
            SetLinearCurve(
                clip,
                path,
                "m_LocalRotation.w",
                rotation.w,
                endTime);
        }

        private static void SetLinearCurve(
            AnimationClip clip,
            string path,
            string property,
            float value,
            float endTime)
        {
            var curve =
                new AnimationCurve(
                    new Keyframe(0f, value),
                    new Keyframe(endTime, value));
            for (var index = 0;
                 index < curve.length;
                 index++)
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

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                curve);
        }

        private static void RequireConstantRotationComponent(
            AnimationClip clip,
            string path,
            string property,
            float expected)
        {
            var curve =
                AnimationUtility.GetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        path,
                        typeof(Transform),
                        property)) ??
                throw new InvalidOperationException(
                    "Revolution_06 corrected arm curve is missing: " +
                    path + "/" + property + ".");
            if (curve.length != 2 ||
                Mathf.Abs(curve.Evaluate(0f) - expected) >
                    0.000001f ||
                Mathf.Abs(
                    curve.Evaluate(clip.length * 0.5f) -
                    expected) > 0.000001f ||
                Mathf.Abs(
                    curve.Evaluate(clip.length) -
                    expected) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Revolution_06 corrected arm curve does not remain at the Revolution_01 static angle: " +
                    path + "/" + property + ".");
            }
        }

        private static void AppendStaticArmInspection(
            AnimationClip sourceClip,
            AnimationClip correctedClip,
            Transform staticModel,
            Transform hitModel)
        {
            var armPaths =
                RequireMatchingStaticArmPaths(
                    staticModel,
                    hitModel);
            var builder = new StringBuilder();
            builder.AppendLine(
                "SourceMixamoClip=" + sourceClip.name);
            builder.AppendLine(
                "CorrectedClipAsset=" + StaticArmClipPath);
            builder.AppendLine(
                "CorrectedClip=" + correctedClip.name);
            builder.AppendLine(
                "StaticArmReference=Revolution_01");
            builder.AppendLine(
                "StaticArmBones=" +
                string.Join("|", StaticArmBoneNames));
            foreach (var boneName in StaticArmBoneNames)
            {
                var rotation =
                    RequireUniqueDescendant(
                            staticModel,
                            boneName)
                        .localRotation;
                builder.AppendLine(
                    "StaticArmRotation[" + boneName + "]=" +
                    rotation.x.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    "," +
                    rotation.y.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    "," +
                    rotation.z.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    "," +
                    rotation.w.ToString(
                        "R",
                        CultureInfo.InvariantCulture) +
                    ", Path=" + armPaths[boneName]);
            }

            builder.AppendLine(
                "ArmRotationCurvesChanged=True");
            builder.AppendLine(
                "ArmPositionCurvesChanged=False");
            builder.AppendLine(
                "NonArmCurvesChanged=False");
            builder.AppendLine(
                "AnimationEventsChanged=False");
            File.AppendAllText(
                Absolute(InspectionPath),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static void CopySourceFbx()
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(Absolute(ImportedFbxPath)) ??
                throw new InvalidOperationException(
                    "Revolution model directory is invalid."));
            if (!File.Exists(Absolute(ImportedFbxPath)) ||
                !string.Equals(
                    Sha256(Absolute(ImportedFbxPath)),
                    SourceSha256,
                    StringComparison.Ordinal))
            {
                File.Copy(
                    SourceFbxPath,
                    Absolute(ImportedFbxPath),
                    true);
            }

            AssetDatabase.ImportAsset(
                ImportedFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImporter()
        {
            var importer =
                AssetImporter.GetAtPath(ImportedFbxPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "Revolution hit ModelImporter is missing.");
            importer.importAnimation = true;
            importer.animationType =
                ModelImporterAnimationType.Generic;
            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.None;
            importer.animationCompression =
                ModelImporterAnimationCompression.Off;
            importer.animationWrapMode = WrapMode.Loop;
            importer.SaveAndReimport();

            importer =
                AssetImporter.GetAtPath(ImportedFbxPath) as
                    ModelImporter ??
                throw new InvalidOperationException(
                    "Revolution hit ModelImporter was lost after its initial import.");
            var sourceClips = importer.defaultClipAnimations;
            var mixamoCandidates =
                sourceClips.Where(candidate =>
                        string.Equals(
                            candidate.name,
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            candidate.takeName,
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) ||
                        candidate.name.IndexOf(
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) >= 0 ||
                        candidate.takeName.IndexOf(
                            MixamoTakeMarker,
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            if (mixamoCandidates.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Revolution hit FBX must expose exactly one Mixamo take. Candidates=" +
                    string.Join(
                        "|",
                        mixamoCandidates.Select(candidate =>
                            candidate.name + "[" +
                            candidate.takeName + "]")) +
                    ", Available=" +
                    string.Join(
                        "|",
                        sourceClips.Select(candidate =>
                            candidate.name + "[" +
                            candidate.takeName + "]")));
            }

            var mixamoClip = mixamoCandidates[0];
            mixamoClip.name = ImportedClipName;
            mixamoClip.wrapMode = WrapMode.Loop;
            mixamoClip.loopTime = true;
            mixamoClip.loopPose = true;
            importer.clipAnimations = new[] { mixamoClip };
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(
                ImportedFbxPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static AnimationClip RequireImportedMixamoClip()
        {
            var clips =
                AssetDatabase.LoadAllAssetsAtPath(ImportedFbxPath)
                    .OfType<AnimationClip>()
                    .Where(candidate =>
                        !candidate.name.StartsWith(
                            "__preview__",
                            StringComparison.Ordinal))
                    .ToArray();
            if (clips.Length != 1 ||
                clips[0].name != ImportedClipName)
            {
                throw new InvalidOperationException(
                    "Revolution hit FBX did not import exactly one selected Mixamo clip. Imported=" +
                    string.Join(
                        "|",
                        clips.Select(candidate => candidate.name)));
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(clips[0]);
            if (!settings.loopTime ||
                !clips[0].isLooping ||
                clips[0].empty)
            {
                throw new InvalidOperationException(
                    "The selected Revolution Mixamo clip is empty or is not configured to loop.");
            }

            return clips[0];
        }

        private static AnimatorController CreateOrUpdateController(
            AnimationClip clip)
        {
            var controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    ControllerPath);
            if (controller == null)
            {
                controller =
                    AnimatorController
                        .CreateAnimatorControllerAtPath(
                            ControllerPath);
            }

            controller.parameters =
                Array.Empty<AnimatorControllerParameter>();
            var stateMachine =
                controller.layers[0].stateMachine;
            foreach (var child in stateMachine.states.ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (var child in
                     stateMachine.stateMachines.ToArray())
            {
                stateMachine.RemoveStateMachine(
                    child.stateMachine);
            }

            var state = stateMachine.AddState(clip.name);
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = true;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !scene.isLoaded ||
                !string.Equals(
                    scene.path,
                    ScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active scene.");
            }

            return scene;
        }

        private static Transform RequirePlacementRoot(
            Scene scene)
        {
            var matches =
                scene.GetRootGameObjects()
                    .Where(root =>
                        root.name == PlacementRootName)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "The Revolution placement root is missing or ambiguous.");
            }

            return matches[0].transform;
        }

        private static Transform RequireDirectChild(
            Transform parent,
            string name)
        {
            var matches =
                parent.Cast<Transform>()
                    .Where(child => child.name == name)
                    .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    name +
                    " is missing or ambiguous below " +
                    parent.name + ".");
            }

            return matches[0];
        }

        private static SkinnedMeshRenderer RequireMainRenderer(
            Transform root,
            string context)
        {
            var renderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(
                    true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    context +
                    " must contain exactly one SkinnedMeshRenderer. Found=" +
                    renderers.Length + ".");
            }

            return renderers[0];
        }

        private static void RequireAuthoredGeometry(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "The imported Revolution hit renderer has no mesh.");
            if (mesh.vertexCount <= 0 ||
                TriangleCount(mesh) != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "The supplied Revolution hit geometry or rig differs from the current Revolution model. Vertices=" +
                    mesh.vertexCount +
                    ", Triangles=" + TriangleCount(mesh) +
                    ", Bones=" + renderer.bones.Length + ".");
            }
        }

        private static void RequireApprovedStaticAppearance(
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Revolution_01 approved renderer has no mesh.");
            if (AssetDatabase.GetAssetPath(mesh) !=
                    ApprovedAppearanceFbxPath ||
                TriangleCount(mesh) != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount ||
                mesh.subMeshCount !=
                    ExpectedApprovedMaterialCount ||
                renderer.sharedMaterials.Length !=
                    ExpectedApprovedMaterialCount)
            {
                throw new InvalidOperationException(
                    "Revolution_01 does not expose the expected approved appearance contract. Vertices=" +
                    mesh.vertexCount +
                    ", Triangles=" + TriangleCount(mesh) +
                    ", Bones=" + renderer.bones.Length +
                    ", SubMeshes=" + mesh.subMeshCount +
                    ", Materials=" +
                    renderer.sharedMaterials.Length + ".");
            }
        }

        private static void SynchronizeAppearance(
            Transform staticModel,
            Transform targetModel)
        {
            var staticRenderers =
                RendererMap(staticModel);
            var targetRenderers =
                RendererMap(targetModel);
            if (!staticRenderers.Keys.SequenceEqual(
                    targetRenderers.Keys,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The Revolution hit renderer hierarchy does not match Revolution_01. Static=" +
                    string.Join("|", staticRenderers.Keys) +
                    ", Hit=" +
                    string.Join("|", targetRenderers.Keys));
            }

            foreach (var pair in staticRenderers)
            {
                var source = pair.Value;
                var target = targetRenderers[pair.Key];
                if (source.GetType() != target.GetType())
                {
                    throw new InvalidOperationException(
                        "Renderer type differs at " + pair.Key + ".");
                }

                target.sharedMaterials =
                    source.sharedMaterials.ToArray();
                target.enabled = source.enabled;
                target.shadowCastingMode =
                    source.shadowCastingMode;
                target.receiveShadows =
                    source.receiveShadows;

                if (source is SkinnedMeshRenderer sourceSkinned &&
                    target is SkinnedMeshRenderer targetSkinned)
                {
                    RequireMatchingBoneNames(
                        sourceSkinned,
                        targetSkinned,
                        pair.Key);
                    targetSkinned.sharedMesh =
                        sourceSkinned.sharedMesh;
                    targetSkinned.updateWhenOffscreen =
                        sourceSkinned.updateWhenOffscreen;
                }
                else
                {
                    var sourceFilter =
                        source.GetComponent<MeshFilter>() ??
                        throw new InvalidOperationException(
                            "Static renderer has no MeshFilter at " +
                            pair.Key + ".");
                    var targetFilter =
                        target.GetComponent<MeshFilter>() ??
                        throw new InvalidOperationException(
                            "Hit renderer has no MeshFilter at " +
                            pair.Key + ".");
                    targetFilter.sharedMesh =
                        sourceFilter.sharedMesh;
                    EditorUtility.SetDirty(targetFilter);
                    PrefabUtility
                        .RecordPrefabInstancePropertyModifications(
                            targetFilter);
                }

                EditorUtility.SetDirty(target);
                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        target);
            }
        }

        private static SortedDictionary<string, Renderer>
            RendererMap(Transform root)
        {
            var result =
                new SortedDictionary<string, Renderer>(
                    StringComparer.Ordinal);
            foreach (var renderer in
                     root.GetComponentsInChildren<Renderer>(true))
            {
                var path =
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        root);
                var key =
                    path + "|" + renderer.GetType().Name;
                if (result.ContainsKey(key))
                {
                    throw new InvalidOperationException(
                        "Duplicate renderer path: " + key);
                }

                result.Add(key, renderer);
            }

            return result;
        }

        private static void RequireAppearanceSynchronized(
            Transform staticModel,
            Transform targetModel)
        {
            var staticRenderers =
                RendererMap(staticModel);
            var targetRenderers =
                RendererMap(targetModel);
            if (!staticRenderers.Keys.SequenceEqual(
                    targetRenderers.Keys,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Static and hit renderer paths differ after appearance synchronization.");
            }

            foreach (var pair in staticRenderers)
            {
                var source = pair.Value;
                var target = targetRenderers[pair.Key];
                if (!source.sharedMaterials.SequenceEqual(
                        target.sharedMaterials))
                {
                    throw new InvalidOperationException(
                        "Revolution_06 materials differ from Revolution_01 at " +
                        pair.Key + ".");
                }

                if (source is SkinnedMeshRenderer sourceSkinned &&
                    target is SkinnedMeshRenderer targetSkinned)
                {
                    if (sourceSkinned.sharedMesh !=
                        targetSkinned.sharedMesh)
                    {
                        throw new InvalidOperationException(
                            "Revolution_06 skinned mesh differs from Revolution_01 at " +
                            pair.Key + ".");
                    }
                }
                else
                {
                    var sourceMesh =
                        source.GetComponent<MeshFilter>()
                            ?.sharedMesh;
                    var targetMesh =
                        target.GetComponent<MeshFilter>()
                            ?.sharedMesh;
                    if (sourceMesh != targetMesh)
                    {
                        throw new InvalidOperationException(
                            "Revolution_06 static mesh differs from Revolution_01 at " +
                            pair.Key + ".");
                    }
                }
            }
        }

        private static void RequireMatchingBoneNames(
            SkinnedMeshRenderer reference,
            SkinnedMeshRenderer target,
            string context)
        {
            var referenceNames =
                reference.bones
                    .Select(bone =>
                        bone != null ? bone.name : "<null>")
                    .ToArray();
            var targetNames =
                target.bones
                    .Select(bone =>
                        bone != null ? bone.name : "<null>")
                    .ToArray();
            if (!referenceNames.SequenceEqual(
                    targetNames,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    context +
                    " bone names or order differ from Revolution_01.");
            }
        }

        private static void RequireAnimator(
            Animator animator,
            RuntimeAnimatorController controller,
            AnimationClip clip)
        {
            if (animator.runtimeAnimatorController != controller ||
                animator.avatar == null ||
                animator.applyRootMotion ||
                !animator.enabled)
            {
                throw new InvalidOperationException(
                    "Revolution_06 Animator contract differs.");
            }

            var clips =
                animator.runtimeAnimatorController.animationClips;
            if (clips.Length != 1 ||
                clips[0] != clip)
            {
                throw new InvalidOperationException(
                    "Revolution_06 controller must reference only the selected Mixamo hit clip.");
            }
        }

        private static void RequirePrefabSource(
            Transform replacement)
        {
            var source =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    replacement.gameObject);
            var path =
                source != null
                    ? AssetDatabase.GetAssetPath(source)
                    : string.Empty;
            if (path != ImportedFbxPath)
            {
                throw new InvalidOperationException(
                    "Revolution_06 is not a direct instance of the supplied hit FBX. Source=" +
                    path);
            }
        }

        private static SortedDictionary<string, string>
            CaptureOtherSlotSignatures(
                Transform placementRoot,
                Transform excludedSlot)
        {
            return new SortedDictionary<string, string>(
                placementRoot.Cast<Transform>()
                    .Where(slot => slot != excludedSlot)
                    .ToDictionary(
                        slot => slot.name,
                        SlotSignature,
                        StringComparer.Ordinal),
                StringComparer.Ordinal);
        }

        private static void RequireOtherSlotsUnchanged(
            Transform placementRoot,
            Transform excludedSlot,
            IReadOnlyDictionary<string, string> before)
        {
            var after =
                CaptureOtherSlotSignatures(
                    placementRoot,
                    excludedSlot);
            if (before.Count != after.Count ||
                before.Any(pair =>
                    !after.TryGetValue(
                        pair.Key,
                        out var value) ||
                    value != pair.Value))
            {
                throw new InvalidOperationException(
                    "A Revolution slot outside Revolution_06 changed.");
            }
        }

        private static string SlotSignature(Transform slot)
        {
            var builder = new StringBuilder();
            foreach (var item in
                     slot.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(
                    AnimationUtility.CalculateTransformPath(
                        item,
                        slot));
                builder.Append('|');
                builder.Append(item.localPosition.ToString("R"));
                builder.Append('|');
                builder.Append(item.localRotation.ToString("R"));
                builder.Append('|');
                builder.Append(item.localScale.ToString("R"));
                builder.Append('|');
                builder.Append(item.gameObject.activeSelf);
                builder.AppendLine();
            }

            foreach (var renderer in
                     slot.GetComponentsInChildren<Renderer>(true))
            {
                builder.Append(
                    AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        slot));
                builder.Append('|');
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        renderer is SkinnedMeshRenderer skinned
                            ? skinned.sharedMesh
                            : renderer.GetComponent<MeshFilter>()
                                ?.sharedMesh));
                builder.Append('|');
                builder.Append(
                    string.Join(
                        ",",
                        renderer.sharedMaterials.Select(
                            AssetDatabase.GetAssetPath)));
                builder.AppendLine();
            }

            foreach (var animator in
                     slot.GetComponentsInChildren<Animator>(true))
            {
                builder.Append(
                    AnimationUtility.CalculateTransformPath(
                        animator.transform,
                        slot));
                builder.Append('|');
                builder.Append(
                    AssetDatabase.GetAssetPath(
                        animator.runtimeAnimatorController));
                builder.Append('|');
                builder.Append(animator.applyRootMotion);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void RequireSlotTransformUnchanged(
            Transform slot,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            if (slot.localPosition != position ||
                slot.localRotation != rotation ||
                slot.localScale != scale)
            {
                throw new InvalidOperationException(
                    "Revolution_06 slot transform changed.");
            }
        }

        private static void CaptureVisualReview(
            Transform staticSlot,
            Transform hitSlot,
            GameObject hitModel,
            AnimationClip clip)
        {
            var staticStates =
                CaptureLayerStates(staticSlot);
            var hitStates =
                CaptureLayerStates(hitSlot);
            var cameraObject =
                new GameObject(
                    "Revolution_Hit_ReviewCamera",
                    typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var keyLightObject =
                new GameObject(
                    "Revolution_Hit_ReviewKey",
                    typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var fillLightObject =
                new GameObject(
                    "Revolution_Hit_ReviewFill",
                    typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var panels = new List<Texture2D>();
            var animationModeStarted = false;

            try
            {
                SetLayerRecursively(staticSlot, ReviewLayer);
                SetLayerRecursively(
                    hitSlot,
                    ReviewHiddenLayer);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor =
                    new Color(0.09f, 0.105f, 0.13f, 1f);
                camera.fieldOfView = 30f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.allowHDR = true;
                camera.allowMSAA = true;
                camera.cullingMask = 1 << ReviewLayer;

                ConfigureReviewLight(
                    keyLightObject.GetComponent<Light>(),
                    Quaternion.Euler(35f, -35f, 0f),
                    3.5f);
                ConfigureReviewLight(
                    fillLightObject.GetComponent<Light>(),
                    Quaternion.Euler(20f, 145f, 0f),
                    1.8f);

                var normalizedTimes =
                    new[] { 0f, 0.25f, 0.5f, 0.75f, 0.99f };
                var staticFront =
                    RenderPanel(camera, staticSlot, 0f);
                var staticOblique =
                    RenderPanel(camera, staticSlot, 0.58f);
                for (var index = 0;
                     index < normalizedTimes.Length;
                     index++)
                {
                    panels.Add(CloneTexture(staticFront));
                }

                SetLayerRecursively(
                    staticSlot,
                    ReviewHiddenLayer);
                SetLayerRecursively(hitSlot, ReviewLayer);
                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                foreach (var normalizedTime in normalizedTimes)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        hitModel,
                        clip,
                        clip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    panels.Add(
                        RenderPanel(
                            camera,
                            hitSlot,
                            0f));
                }

                for (var index = 0;
                     index < normalizedTimes.Length;
                     index++)
                {
                    panels.Add(CloneTexture(staticOblique));
                }

                foreach (var normalizedTime in normalizedTimes)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        hitModel,
                        clip,
                        clip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    panels.Add(
                        RenderPanel(
                            camera,
                            hitSlot,
                            0.58f));
                }

                var sheet =
                    ComposeSheet(
                        panels,
                        normalizedTimes.Length,
                        4);
                try
                {
                    File.WriteAllBytes(
                        Absolute(CapturePath),
                        sheet.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sheet);
                }

                UnityEngine.Object.DestroyImmediate(staticFront);
                UnityEngine.Object.DestroyImmediate(staticOblique);
            }
            finally
            {
                if (animationModeStarted)
                {
                    AnimationMode.StopAnimationMode();
                }

                RestoreLayerStates(staticStates);
                RestoreLayerStates(hitStates);
                foreach (var panel in panels)
                {
                    if (panel != null)
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                    }
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }
        }

        private static void ConfigureReviewLight(
            Light light,
            Quaternion rotation,
            float intensity)
        {
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            light.transform.rotation = rotation;
            light.cullingMask = 1 << ReviewLayer;
        }

        private static Texture2D RenderPanel(
            Camera camera,
            Transform slot,
            float oblique)
        {
            var mainRenderer =
                RequireMainRenderer(
                    slot,
                    slot.name + " direct visual review");
            if (!mainRenderer.enabled)
            {
                throw new InvalidOperationException(
                    slot.name +
                    " main renderer is disabled for direct review.");
            }

            var viewDirection =
                (slot.forward +
                 slot.right * oblique).normalized;
            var bounds =
                SkeletonWorldBounds(mainRenderer);
            var reviewRadius =
                Mathf.Max(
                    bounds.extents.magnitude,
                    bounds.extents.y);
            var distance =
                reviewRadius /
                Mathf.Tan(
                    camera.fieldOfView *
                    0.5f *
                    Mathf.Deg2Rad) *
                1.2f;
            camera.transform.position =
                bounds.center +
                viewDirection * distance;
            camera.transform.rotation =
                Quaternion.LookRotation(
                    bounds.center -
                    camera.transform.position,
                    Vector3.up);

            var renderTexture =
                RenderTexture.GetTemporary(
                    PanelSize,
                    PanelSize,
                    24,
                    RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture =
                    new Texture2D(
                        PanelSize,
                        PanelSize,
                        TextureFormat.RGBA32,
                        false);
                texture.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        PanelSize,
                        PanelSize),
                    0,
                    0);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Bounds SkeletonWorldBounds(
            SkinnedMeshRenderer renderer)
        {
            var bones =
                renderer.bones
                    .Where(bone => bone != null)
                    .ToArray();
            if (bones.Length != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "Revolution direct review requires the complete 24-bone rig.");
            }

            var bounds =
                new Bounds(
                    bones[0].position,
                    Vector3.zero);
            foreach (var bone in bones.Skip(1))
            {
                bounds.Encapsulate(bone.position);
            }

            return bounds;
        }

        private static Texture2D CloneTexture(
            Texture2D source)
        {
            var clone =
                new Texture2D(
                    source.width,
                    source.height,
                    TextureFormat.RGBA32,
                    false);
            clone.SetPixels32(source.GetPixels32());
            clone.Apply(false, false);
            return clone;
        }

        private static Texture2D ComposeSheet(
            IReadOnlyList<Texture2D> panels,
            int columns,
            int rows)
        {
            if (panels.Count != columns * rows)
            {
                throw new InvalidOperationException(
                    "Unexpected Revolution hit review panel count.");
            }

            var sheet =
                new Texture2D(
                    columns * PanelSize,
                    rows * PanelSize,
                    TextureFormat.RGBA32,
                    false);
            var background =
                Enumerable.Repeat(
                        new Color32(7, 9, 12, 255),
                        sheet.width * sheet.height)
                    .ToArray();
            sheet.SetPixels32(background);
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0;
                     column < columns;
                     column++)
                {
                    var panel =
                        panels[row * columns + column];
                    sheet.SetPixels32(
                        column * PanelSize,
                        (rows - 1 - row) * PanelSize,
                        PanelSize,
                        PanelSize,
                        panel.GetPixels32());
                }
            }

            sheet.Apply(false, false);
            return sheet;
        }

        private static LayerState[] CaptureLayerStates(
            Transform root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(item =>
                    new LayerState(
                        item.gameObject,
                        item.gameObject.layer))
                .ToArray();
        }

        private static void SetLayerRecursively(
            Transform root,
            int layer)
        {
            foreach (var item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                item.gameObject.layer = layer;
            }
        }

        private static void RestoreLayerStates(
            IEnumerable<LayerState> states)
        {
            foreach (var state in states)
            {
                state.GameObject.layer = state.Layer;
            }
        }

        private static void RevertSampledEulerHintOverrides(
            Transform root)
        {
            foreach (var item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                var serialized = new SerializedObject(item);
                var eulerHint =
                    serialized.FindProperty(
                        "m_LocalEulerAnglesHint");
                if (eulerHint != null &&
                    eulerHint.prefabOverride)
                {
                    PrefabUtility.RevertPropertyOverride(
                        eulerHint,
                        InteractionMode.AutomatedAction);
                }
            }
        }

        private static void WriteInspection(
            Transform staticModel,
            Transform replacement,
            AnimationClip clip,
            AnimatorController controller)
        {
            var staticRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01");
            var replacementRenderer =
                RequireMainRenderer(
                    replacement,
                    "Revolution_06");
            var builder = new StringBuilder();
            builder.AppendLine(
                "Revolution 06 Hit Attack Inspection");
            builder.AppendLine(
                "Source=" + SourceFbxPath);
            builder.AppendLine(
                "ImportedSource=" + ImportedFbxPath);
            builder.AppendLine(
                "SourceSha256=" + SourceSha256);
            builder.AppendLine(
                "Slot=" + HitSlotName);
            builder.AppendLine(
                "PrefabSource=" + ImportedFbxPath);
            builder.AppendLine(
                "Clip=" + clip.name);
            builder.AppendLine(
                "ClipLength=" +
                clip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "ClipFrameRate=" +
                clip.frameRate.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            builder.AppendLine(
                "ClipCurveBindings=" +
                AnimationUtility.GetCurveBindings(clip).Length);
            builder.AppendLine("Loop=True");
            builder.AppendLine("RootMotion=False");
            builder.AppendLine(
                "Controller=" + ControllerPath);
            builder.AppendLine(
                "State=" + clip.name);
            builder.AppendLine(
                "StaticMesh=" +
                AssetDatabase.GetAssetPath(
                    staticRenderer.sharedMesh));
            builder.AppendLine(
                "HitMesh=" +
                AssetDatabase.GetAssetPath(
                    replacementRenderer.sharedMesh));
            builder.AppendLine(
                "StaticAndHitMeshSame=" +
                (staticRenderer.sharedMesh ==
                 replacementRenderer.sharedMesh));
            builder.AppendLine(
                "ApprovedMaterials=" +
                string.Join(
                    "|",
                    staticRenderer.sharedMaterials.Select(
                        AssetDatabase.GetAssetPath)));
            builder.AppendLine(
                "StaticAndHitMaterialsSame=" +
                staticRenderer.sharedMaterials.SequenceEqual(
                    replacementRenderer.sharedMaterials));
            builder.AppendLine(
                "UnityMeshVertices=" +
                replacementRenderer.sharedMesh.vertexCount);
            builder.AppendLine(
                "Triangles=" +
                TriangleCount(
                    replacementRenderer.sharedMesh));
            builder.AppendLine(
                "Bones=" +
                replacementRenderer.bones.Length);
            builder.AppendLine(
                "VisualReview=" + CapturePath);
            builder.AppendLine(
                "VisualRows=StaticFront|AnimatedFront|StaticOblique|AnimatedOblique");
            builder.AppendLine(
                "VisualColumns=0|0.25|0.5|0.75|0.99 normalized time");
            builder.AppendLine(
                "NewMeshCreated=False");
            builder.AppendLine(
                "NewMaterialCreated=False");
            builder.AppendLine(
                "NewTextureCreated=False");
            builder.AppendLine(
                "OtherRevolutionSlotsChanged=False");
            File.WriteAllText(
                Absolute(InspectionPath),
                builder.ToString(),
                new UTF8Encoding(false));
        }

        private static int TriangleCount(Mesh mesh)
        {
            var count = 0;
            for (var index = 0;
                 index < mesh.subMeshCount;
                 index++)
            {
                count +=
                    (int)mesh.GetIndexCount(index) / 3;
            }

            return count;
        }

        private static void RequireHash(
            string path,
            string expected)
        {
            var absolute =
                path.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal)
                    ? Absolute(path)
                    : path;
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Required Revolution file is missing.",
                    absolute);
            }

            var actual = Sha256(absolute);
            if (!string.Equals(
                    actual,
                    expected,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Revolution file hash differs. Path=" +
                    path + ", Actual=" + actual +
                    ", Expected=" + expected + ".");
            }
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(
                    hash.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Absolute(string path)
        {
            return Path.GetFullPath(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)
                        ?.FullName ??
                    throw new InvalidOperationException(
                        "Project root is unavailable."),
                    path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private readonly struct LayerState
        {
            public LayerState(
                GameObject gameObject,
                int layer)
            {
                GameObject = gameObject;
                Layer = layer;
            }

            public GameObject GameObject { get; }
            public int Layer { get; }
        }
    }
}
