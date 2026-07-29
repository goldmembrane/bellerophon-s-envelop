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
    internal static class RevolutionTurnModelReplacementTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourceFbxPath =
            "D:/Bellerophon2/Bellerophon/enemies model/révolution turn.fbx";
        private const string ImportedFbxPath =
            "Assets/_Project/Art/Enemies/Revolution/Models/RevolutionTurn.fbx";
        private const string ApprovedAppearanceFbxPath =
            "Assets/_Project/Art/Enemies/Revolution/ApprovedAppearance/Models/Revolution_ApprovedAppearance.fbx";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Revolution/Controllers/Revolution_07_Turn.controller";
        private const string ValidationFolder =
            "docs/validation/revolution_turn_2026-07-29";
        private const string InspectionPath =
            ValidationFolder + "/Revolution_07_Turn_Inspection.txt";
        private const string CapturePath =
            ValidationFolder + "/Revolution_07_Turn_VisualReview.png";
        private const string SourceCurveInspectionPath =
            ValidationFolder + "/Revolution_07_Turn_SourceCurves.txt";
        private const string Turn360ClipPath =
            "Assets/_Project/Art/Enemies/Revolution/Animations/Revolution_07_Turn_360_3s_StaticArms.anim";
        private const string Turn360InspectionPath =
            ValidationFolder + "/Revolution_07_Turn_360_3s_StaticArms_Inspection.txt";
        private const string SourceSha256 =
            "74745CC9DF32B29B47BF101092F605B72AD5E9B0389DF7B372C8CAFD320E246D";
        private const string PlacementRootName =
            "Approved Revolution Enemy Placement";
        private const string StaticSlotName = "Revolution_01";
        private const string TurnSlotName = "Revolution_07";
        private const string ReplacementModelName =
            "Revolution_Turn_Model";
        private const string MixamoTakeMarker = "mixamo.com";
        private const string ImportedClipName =
            "Revolution_07_Turn_Mixamo";
        private const string StateName =
            "Revolution_07_Turn_Mixamo";
        private const string Turn360ClipName =
            "Revolution_07_Turn_360_3s_StaticArms";
        private const string HipsPath = "Armature/Hips";
        private const float Turn360Duration = 3f;
        private const float HalfTurnDuration = 1.5f;
        private const int Turn360FrameRate = 60;
        private const int Turn360FrameCount = 180;
        private const int HalfTurnFrameCount = 90;
        private const int ExpectedTriangleCount = 3945;
        private const int ExpectedBoneCount = 24;
        private const int ExpectedApprovedMaterialCount = 8;
        private const int ReviewLayer = 30;
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
            "Bellerophon/Enemies/Revolution/Apply Turn Model Replacement")]
        public static void ApplyRevolutionTurn()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException(
                    "Revolution turn replacement requires Edit Mode. Play Mode exit was requested; run the command again after Unity returns to Edit Mode.");
            }

            RequireHash(SourceFbxPath, SourceSha256);
            CopySourceFbx();
            ConfigureImporter();
            RequireHash(ImportedFbxPath, SourceSha256);

            var importedPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ImportedFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Revolution turn FBX is missing.",
                    ImportedFbxPath);
            var importedClip = RequireImportedMixamoClip();
            var importedAvatar =
                AssetDatabase.LoadAllAssetsAtPath(ImportedFbxPath)
                    .OfType<Avatar>()
                    .SingleOrDefault() ??
                throw new InvalidOperationException(
                    "Revolution turn FBX did not produce exactly one Generic Avatar.");
            var importedRenderer =
                RequireMainRenderer(
                    importedPrefab.transform,
                    "imported Revolution turn FBX");
            RequireAuthoredGeometry(importedRenderer);
            var controller = CreateOrUpdateController(importedClip);

            var scene = RequireCurrentScene();
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot =
                RequireDirectChild(placementRoot, StaticSlotName);
            var turnSlot =
                RequireDirectChild(placementRoot, TurnSlotName);
            if (turnSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_07 must contain exactly one model before replacement.");
            }

            var staticModel = staticSlot.GetChild(0);
            var previousModel = turnSlot.GetChild(0);
            var staticMainRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01 static approved model");
            RequireApprovedStaticAppearance(staticMainRenderer);
            RequireMatchingBoneNames(
                staticMainRenderer,
                importedRenderer,
                "Revolution turn rig");

            var slotPositionBefore = turnSlot.localPosition;
            var slotRotationBefore = turnSlot.localRotation;
            var slotScaleBefore = turnSlot.localScale;
            var otherSlotsBefore =
                CaptureOtherSlotSignatures(placementRoot, turnSlot);
            var previousLocalPosition = previousModel.localPosition;
            var previousLocalRotation = previousModel.localRotation;
            var previousLocalScale = previousModel.localScale;

            var replacement =
                PrefabUtility.InstantiatePrefab(importedPrefab, scene) as
                    GameObject ??
                throw new InvalidOperationException(
                    "Revolution turn FBX could not be instantiated.");
            replacement.name = ReplacementModelName;
            replacement.transform.SetParent(turnSlot, false);
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
                        "Revolution_07 turn model");
                RequireMatchingBoneNames(
                    importedRenderer,
                    replacementMainRenderer,
                    "instantiated Revolution turn rig");
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
                        "The replacement model did not preserve the previous Revolution_07 local transform.");
                }
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(
                previousModel.gameObject);
            if (turnSlot.childCount != 1 ||
                turnSlot.GetChild(0) != replacement.transform)
            {
                throw new InvalidOperationException(
                    "Revolution_07 replacement did not leave exactly one turn model.");
            }

            RequireSlotTransformUnchanged(
                turnSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                turnSlot,
                otherSlotsBefore);
            RequirePrefabSource(replacement.transform);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            Directory.CreateDirectory(Absolute(ValidationFolder));
            CaptureVisualReview(
                staticSlot,
                turnSlot,
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
                turnSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                turnSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                replacement.transform);

            EditorUtility.SetDirty(replacement);
            EditorUtility.SetDirty(turnSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Revolution_07 turn replacement.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ImportedFbxPath, SourceSha256);
            Selection.activeGameObject = turnSlot.gameObject;
            Debug.Log(
                "RevolutionTurnApplied" +
                ", Slot=" + TurnSlotName +
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
            "Bellerophon/Enemies/Revolution/Inspect Turn Source Curves")]
        public static void InspectRevolutionTurnSourceCurves()
        {
            var importedPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ImportedFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Revolution turn FBX is missing.",
                    ImportedFbxPath);
            var importedClip = RequireImportedMixamoClip();
            var bindings =
                AnimationUtility.GetCurveBindings(importedClip)
                    .OrderBy(binding => binding.path)
                    .ThenBy(binding => binding.propertyName)
                    .ToArray();
            var report = new StringBuilder();
            report.AppendLine("Revolution_07 Turn Source Curve Inspection");
            report.AppendLine("SourceClip=" + importedClip.name);
            report.AppendLine(
                "Length=" +
                importedClip.length.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "FrameRate=" +
                importedClip.frameRate.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine("BindingCount=" + bindings.Length);
            report.AppendLine();
            report.AppendLine("[CurveBindings]");
            foreach (var binding in bindings)
            {
                var curve =
                    AnimationUtility.GetEditorCurve(
                        importedClip,
                        binding);
                report.AppendLine(
                    "Path=" + binding.path +
                    " | Property=" + binding.propertyName +
                    " | Type=" + binding.type.FullName +
                    " | Keys=" + curve.length +
                    " | First=" +
                    FormatKey(curve.keys.First()) +
                    " | Last=" +
                    FormatKey(curve.keys.Last()));
            }

            var instance =
                UnityEngine.Object.Instantiate(importedPrefab);
            instance.hideFlags =
                HideFlags.HideAndDontSave;
            try
            {
                var animator =
                    instance.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }

                var transforms =
                    instance.GetComponentsInChildren<Transform>(true)
                        .OrderBy(transform =>
                            AnimationUtility.CalculateTransformPath(
                                transform,
                                instance.transform))
                        .ToArray();
                var startRotations =
                    new Dictionary<Transform, Quaternion>();
                importedClip.SampleAnimation(instance, 0f);
                foreach (var transform in transforms)
                {
                    startRotations[transform] =
                        transform.localRotation;
                }

                var sampleTimes =
                    new[]
                    {
                        0f,
                        importedClip.length * 0.25f,
                        importedClip.length * 0.5f,
                        importedClip.length * 0.75f,
                        importedClip.length
                    };
                report.AppendLine();
                report.AppendLine("[WorldHeadingSamples]");
                foreach (var sampleTime in sampleTimes)
                {
                    importedClip.SampleAnimation(
                        instance,
                        sampleTime);
                    report.AppendLine(
                        "Time=" +
                        sampleTime.ToString(
                            "0.#########",
                            CultureInfo.InvariantCulture) +
                        " | RootForward=" +
                        FormatVector(instance.transform.forward));
                    foreach (var transform in transforms.Where(
                                 candidate =>
                                     candidate.name.IndexOf(
                                         "Hips",
                                         StringComparison.OrdinalIgnoreCase) >=
                                     0))
                    {
                        report.AppendLine(
                            "  " +
                            AnimationUtility.CalculateTransformPath(
                                transform,
                                instance.transform) +
                            " Forward=" +
                            FormatVector(transform.forward) +
                            " LocalEuler=" +
                            FormatVector(
                                transform.localEulerAngles));
                    }
                }

                importedClip.SampleAnimation(
                    instance,
                    importedClip.length);
                report.AppendLine();
                report.AppendLine("[LocalRotationStartToEnd]");
                foreach (var transform in transforms)
                {
                    var angle =
                        Quaternion.Angle(
                            startRotations[transform],
                            transform.localRotation);
                    if (angle < 0.001f)
                    {
                        continue;
                    }

                    report.AppendLine(
                        "Path=" +
                        AnimationUtility.CalculateTransformPath(
                            transform,
                            instance.transform) +
                        " | Angle=" +
                        angle.ToString(
                            "0.#########",
                            CultureInfo.InvariantCulture) +
                        " | StartEuler=" +
                        FormatVector(
                            startRotations[transform].eulerAngles) +
                        " | EndEuler=" +
                        FormatVector(
                            transform.localEulerAngles));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Directory.CreateDirectory(
                Absolute(ValidationFolder));
            File.WriteAllText(
                Absolute(SourceCurveInspectionPath),
                report.ToString(),
                new UTF8Encoding(false));
            Debug.Log(
                "RevolutionTurnSourceCurvesInspected" +
                ", Report=" + SourceCurveInspectionPath +
                ", BindingCount=" + bindings.Length + ".");
        }

        private static string FormatKey(Keyframe key)
        {
            return key.time.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   ":" +
                   key.value.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture);
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" +
                   value.x.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   "," +
                   value.y.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   "," +
                   value.z.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   ")";
        }

        [MenuItem(
            "Bellerophon/Enemies/Revolution/Apply 360 Turn 3s Static Arms")]
        public static void ApplyRevolutionTurn360StaticArms()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.ExitPlaymode();
                }

                throw new InvalidOperationException(
                    "Revolution 360-degree turn correction requires Edit Mode. Play Mode exit was requested; run the command again after Unity returns to Edit Mode.");
            }

            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ImportedFbxPath, SourceSha256);
            var sourceClip = RequireImportedMixamoClip();
            var scene = RequireCurrentScene();
            var placementRoot = RequirePlacementRoot(scene);
            var staticSlot =
                RequireDirectChild(placementRoot, StaticSlotName);
            var turnSlot =
                RequireDirectChild(placementRoot, TurnSlotName);
            if (staticSlot.childCount != 1 ||
                turnSlot.childCount != 1)
            {
                throw new InvalidOperationException(
                    "Revolution_01 and Revolution_07 must each contain exactly one model.");
            }

            var staticModel = staticSlot.GetChild(0);
            var turnModel = turnSlot.GetChild(0);
            var staticRenderer =
                RequireMainRenderer(
                    staticModel,
                    "Revolution_01 static approved model");
            RequireApprovedStaticAppearance(staticRenderer);
            RequirePrefabSource(turnModel);
            RequireAppearanceSynchronized(
                staticModel,
                turnModel);

            var slotPositionBefore = turnSlot.localPosition;
            var slotRotationBefore = turnSlot.localRotation;
            var slotScaleBefore = turnSlot.localScale;
            var otherSlotsBefore =
                CaptureOtherSlotSignatures(
                    placementRoot,
                    turnSlot);
            var correctedClip =
                CreateOrUpdateTurn360Clip(
                    sourceClip,
                    staticModel,
                    turnModel);
            var controller =
                CreateOrUpdateController(correctedClip);
            var animator =
                turnModel.GetComponent<Animator>() ??
                throw new InvalidOperationException(
                    "Revolution_07 Animator is missing.");
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
            var metrics =
                RequireTurn360ClipContract(
                    sourceClip,
                    correctedClip,
                    staticModel,
                    turnModel);
            RequireSlotTransformUnchanged(
                turnSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                turnSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                turnModel);

            Directory.CreateDirectory(
                Absolute(ValidationFolder));
            CaptureVisualReview(
                staticSlot,
                turnSlot,
                turnModel.gameObject,
                correctedClip);
            RevertSampledEulerHintOverrides(turnModel);
            WriteTurn360Inspection(
                sourceClip,
                correctedClip,
                controller,
                staticModel,
                turnModel,
                metrics);

            RequireTurn360ClipContract(
                sourceClip,
                correctedClip,
                staticModel,
                turnModel);
            RequireSlotTransformUnchanged(
                turnSlot,
                slotPositionBefore,
                slotRotationBefore,
                slotScaleBefore);
            RequireOtherSlotsUnchanged(
                placementRoot,
                turnSlot,
                otherSlotsBefore);
            RequireAppearanceSynchronized(
                staticModel,
                turnModel);

            EditorUtility.SetDirty(turnModel.gameObject);
            EditorUtility.SetDirty(turnSlot.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Revolution_07 360-degree turn correction.");
            }

            AssetDatabase.SaveAssets();
            RequireHash(SourceFbxPath, SourceSha256);
            RequireHash(ImportedFbxPath, SourceSha256);
            Selection.activeGameObject = turnSlot.gameObject;
            Debug.Log(
                "RevolutionTurn360StaticArmsApplied" +
                ", Slot=" + TurnSlotName +
                ", Clip=" + correctedClip.name +
                ", Duration=" +
                correctedClip.length.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", HalfTurnDirection=" +
                metrics.HalfTurnDegrees.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                ", StaticArmBones=" +
                string.Join(",", StaticArmBoneNames) +
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

        private static AnimationClip CreateOrUpdateTurn360Clip(
            AnimationClip sourceClip,
            Transform staticModel,
            Transform turnModel)
        {
            var armPaths =
                RequireMatchingStaticArmPaths(
                    staticModel,
                    turnModel);
            var editableArmPaths =
                new HashSet<string>(
                    armPaths.Values,
                    StringComparer.Ordinal);
            var importedPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    ImportedFbxPath) ??
                throw new FileNotFoundException(
                    "Imported Revolution turn FBX is missing.",
                    ImportedFbxPath);
            var sampleInstance =
                UnityEngine.Object.Instantiate(
                    importedPrefab);
            sampleInstance.hideFlags =
                HideFlags.HideAndDontSave;
            var candidate = new AnimationClip
            {
                name = Turn360ClipName,
                frameRate = Turn360FrameRate,
                wrapMode = WrapMode.Loop
            };

            try
            {
                var sampleAnimator =
                    sampleInstance.GetComponent<Animator>();
                if (sampleAnimator != null)
                {
                    sampleAnimator.enabled = false;
                }

                var hips =
                    RequireUniqueDescendant(
                        sampleInstance.transform,
                        "Hips");
                var calculatedHipsPath =
                    AnimationUtility.CalculateTransformPath(
                        hips,
                        sampleInstance.transform);
                if (!string.Equals(
                        calculatedHipsPath,
                        HipsPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "The supplied turn FBX Hips path differs. Actual=" +
                        calculatedHipsPath + ".");
                }

                RequireSourceRootCurveContract(sourceClip);
                sourceClip.SampleAnimation(
                    sampleInstance,
                    0f);
                var startForward =
                    HorizontalDirection(hips.forward);
                var startPosition = hips.position;
                sourceClip.SampleAnimation(
                    sampleInstance,
                    sourceClip.length);
                var endForward =
                    HorizontalDirection(hips.forward);
                var endPosition = hips.position;
                var sourceHalfTurn =
                    Vector3.SignedAngle(
                        startForward,
                        endForward,
                        Vector3.up);
                if (Mathf.Abs(sourceHalfTurn) < 150f ||
                    Mathf.Abs(sourceHalfTurn) >= 179.9f)
                {
                    throw new InvalidOperationException(
                        "The supplied Mixamo turn source is not an unambiguous approximately-180-degree turn. SignedHeading=" +
                        sourceHalfTurn.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) + ".");
                }

                var targetHalfTurn =
                    Mathf.Sign(sourceHalfTurn) * 180f;
                var sourceBindings =
                    AnimationUtility.GetCurveBindings(
                        sourceClip);
                foreach (var binding in sourceBindings)
                {
                    if (IsHipsTransformBinding(binding) ||
                        IsEditableArmRotation(
                            binding,
                            editableArmPaths))
                    {
                        continue;
                    }

                    var sourceCurve =
                        AnimationUtility.GetEditorCurve(
                            sourceClip,
                            binding) ??
                        throw new InvalidOperationException(
                            "A Revolution turn source curve is missing: " +
                            binding.path + "/" +
                            binding.propertyName + ".");
                    AnimationUtility.SetEditorCurve(
                        candidate,
                        binding,
                        BuildRepeatedSourceCurve(
                            sourceCurve,
                            sourceClip.length));
                }

                var rootPositionSamples =
                    new Vector3[Turn360FrameCount + 1];
                var rootRotationSamples =
                    new Quaternion[Turn360FrameCount + 1];
                var pivot =
                    new Vector3(
                        (startPosition.x + endPosition.x) *
                        0.5f,
                        0f,
                        (startPosition.z + endPosition.z) *
                        0.5f);
                Quaternion? previousRotation = null;
                for (var frame = 0;
                     frame <= Turn360FrameCount;
                     frame++)
                {
                    var cycle = frame / HalfTurnFrameCount;
                    var withinCycleFrame =
                        frame % HalfTurnFrameCount;
                    var normalizedSourceTime =
                        withinCycleFrame /
                        (float)HalfTurnFrameCount;
                    sourceClip.SampleAnimation(
                        sampleInstance,
                        normalizedSourceTime *
                        sourceClip.length);

                    var sourceHeading =
                        Vector3.SignedAngle(
                            startForward,
                            HorizontalDirection(hips.forward),
                            Vector3.up);
                    var normalizedHeading =
                        targetHalfTurn *
                        normalizedSourceTime;
                    var correctedWorldRotation =
                        Quaternion.AngleAxis(
                            normalizedHeading - sourceHeading,
                            Vector3.up) *
                        hips.rotation;
                    var finalWorldRotation =
                        Quaternion.AngleAxis(
                            cycle * targetHalfTurn,
                            Vector3.up) *
                        correctedWorldRotation;
                    var finalLocalRotation =
                        Quaternion.Inverse(
                            hips.parent.rotation) *
                        finalWorldRotation;
                    if (previousRotation.HasValue &&
                        Quaternion.Dot(
                            previousRotation.Value,
                            finalLocalRotation) < 0f)
                    {
                        finalLocalRotation =
                            new Quaternion(
                                -finalLocalRotation.x,
                                -finalLocalRotation.y,
                                -finalLocalRotation.z,
                                -finalLocalRotation.w);
                    }

                    previousRotation = finalLocalRotation;
                    rootRotationSamples[frame] =
                        finalLocalRotation;

                    var sourceWorldPosition = hips.position;
                    var horizontalOffset =
                        new Vector3(
                            sourceWorldPosition.x - pivot.x,
                            0f,
                            sourceWorldPosition.z - pivot.z);
                    var rotatedHorizontalOffset =
                        Quaternion.AngleAxis(
                            cycle * targetHalfTurn,
                            Vector3.up) *
                        horizontalOffset;
                    var finalWorldPosition =
                        new Vector3(
                            pivot.x + rotatedHorizontalOffset.x,
                            sourceWorldPosition.y,
                            pivot.z + rotatedHorizontalOffset.z);
                    rootPositionSamples[frame] =
                        hips.parent.InverseTransformPoint(
                            finalWorldPosition);
                }

                SetVector3Curves(
                    candidate,
                    HipsPath,
                    "m_LocalPosition",
                    rootPositionSamples);
                SetQuaternionCurves(
                    candidate,
                    HipsPath,
                    rootRotationSamples);
                foreach (var boneName in
                         StaticArmBoneNames)
                {
                    var staticBone =
                        RequireUniqueDescendant(
                            staticModel,
                            boneName);
                    SetConstantQuaternionCurves(
                        candidate,
                        armPaths[boneName],
                        staticBone.localRotation,
                        Turn360Duration);
                }

                var settings =
                    AnimationUtility.GetAnimationClipSettings(
                        candidate);
                settings.loopTime = true;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(
                    candidate,
                    settings);
                candidate.EnsureQuaternionContinuity();

                var savedClip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        Turn360ClipPath);
                if (savedClip == null)
                {
                    AssetDatabase.CreateAsset(
                        candidate,
                        Turn360ClipPath);
                    candidate = null;
                }
                else
                {
                    EditorUtility.CopySerialized(
                        candidate,
                        savedClip);
                    savedClip.name = Turn360ClipName;
                    EditorUtility.SetDirty(savedClip);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    sampleInstance);
                if (candidate != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        candidate);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                Turn360ClipPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            return
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    Turn360ClipPath) ??
                throw new InvalidOperationException(
                    "Revolution_07 360-degree turn clip was not saved.");
        }

        private static void RequireSourceRootCurveContract(
            AnimationClip sourceClip)
        {
            var hipsBindings =
                AnimationUtility.GetCurveBindings(sourceClip)
                    .Where(binding =>
                        binding.path == HipsPath)
                    .ToArray();
            var expected =
                new HashSet<string>(
                    new[]
                    {
                        "m_LocalPosition.x",
                        "m_LocalPosition.y",
                        "m_LocalPosition.z",
                        "m_LocalRotation.x",
                        "m_LocalRotation.y",
                        "m_LocalRotation.z",
                        "m_LocalRotation.w"
                    },
                    StringComparer.Ordinal);
            if (hipsBindings.Length != expected.Count ||
                hipsBindings.Any(binding =>
                    binding.type != typeof(Transform) ||
                    !expected.Contains(
                        binding.propertyName)))
            {
                throw new InvalidOperationException(
                    "The supplied Mixamo turn Hips curve mapping differs from the inspected quaternion/position contract.");
            }
        }

        private static bool IsHipsTransformBinding(
            EditorCurveBinding binding)
        {
            return binding.path == HipsPath &&
                   binding.type == typeof(Transform) &&
                   (binding.propertyName.StartsWith(
                        "m_LocalPosition.",
                        StringComparison.Ordinal) ||
                    binding.propertyName.StartsWith(
                        "m_LocalRotation.",
                        StringComparison.Ordinal));
        }

        private static AnimationCurve BuildRepeatedSourceCurve(
            AnimationCurve source,
            float sourceLength)
        {
            var values =
                new float[Turn360FrameCount + 1];
            for (var frame = 0;
                 frame <= Turn360FrameCount;
                 frame++)
            {
                var withinCycleFrame =
                    frame % HalfTurnFrameCount;
                var normalizedSourceTime =
                    withinCycleFrame /
                    (float)HalfTurnFrameCount;
                values[frame] =
                    source.Evaluate(
                        normalizedSourceTime *
                        sourceLength);
            }

            return BuildLinearCurve(values);
        }

        private static void SetVector3Curves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            IReadOnlyList<Vector3> values)
        {
            SetSampledCurve(
                clip,
                path,
                propertyPrefix + ".x",
                values.Select(value => value.x).ToArray());
            SetSampledCurve(
                clip,
                path,
                propertyPrefix + ".y",
                values.Select(value => value.y).ToArray());
            SetSampledCurve(
                clip,
                path,
                propertyPrefix + ".z",
                values.Select(value => value.z).ToArray());
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<Quaternion> values)
        {
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.x",
                values.Select(value => value.x).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.y",
                values.Select(value => value.y).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.z",
                values.Select(value => value.z).ToArray());
            SetSampledCurve(
                clip,
                path,
                "m_LocalRotation.w",
                values.Select(value => value.w).ToArray());
        }

        private static void SetSampledCurve(
            AnimationClip clip,
            string path,
            string property,
            IReadOnlyList<float> values)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    property),
                BuildLinearCurve(values));
        }

        private static AnimationCurve BuildLinearCurve(
            IReadOnlyList<float> values)
        {
            var keys = new Keyframe[values.Count];
            for (var index = 0;
                 index < values.Count;
                 index++)
            {
                keys[index] =
                    new Keyframe(
                        index /
                        (float)Turn360FrameRate,
                        values[index]);
            }

            var curve = new AnimationCurve(keys)
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
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

            return curve;
        }

        private static void SetConstantQuaternionCurves(
            AnimationClip clip,
            string path,
            Quaternion rotation,
            float endTime)
        {
            SetConstantCurve(
                clip,
                path,
                "m_LocalRotation.x",
                rotation.x,
                endTime);
            SetConstantCurve(
                clip,
                path,
                "m_LocalRotation.y",
                rotation.y,
                endTime);
            SetConstantCurve(
                clip,
                path,
                "m_LocalRotation.z",
                rotation.z,
                endTime);
            SetConstantCurve(
                clip,
                path,
                "m_LocalRotation.w",
                rotation.w,
                endTime);
        }

        private static void SetConstantCurve(
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

        private static SortedDictionary<string, string>
            RequireMatchingStaticArmPaths(
                Transform staticModel,
                Transform turnModel)
        {
            var result =
                new SortedDictionary<string, string>(
                    StringComparer.Ordinal);
            foreach (var boneName in
                     StaticArmBoneNames)
            {
                var staticPath =
                    AnimationUtility.CalculateTransformPath(
                        RequireUniqueDescendant(
                            staticModel,
                            boneName),
                        staticModel);
                var turnPath =
                    AnimationUtility.CalculateTransformPath(
                        RequireUniqueDescendant(
                            turnModel,
                            boneName),
                        turnModel);
                if (!string.Equals(
                        staticPath,
                        turnPath,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        boneName +
                        " path differs between Revolution_01 and Revolution_07. Static=" +
                        staticPath + ", Turn=" +
                        turnPath + ".");
                }

                result.Add(boneName, turnPath);
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
                    root.name +
                    " must contain exactly one " +
                    name + " bone. Found=" +
                    matches.Length + ".");
            }

            return matches[0];
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

        private static Vector3 HorizontalDirection(
            Vector3 direction)
        {
            var horizontal =
                Vector3.ProjectOnPlane(
                    direction,
                    Vector3.up);
            if (horizontal.sqrMagnitude < 0.000001f)
            {
                throw new InvalidOperationException(
                    "A sampled Revolution turn heading is vertical and cannot define a turn direction.");
            }

            return horizontal.normalized;
        }

        private static Turn360Metrics RequireTurn360ClipContract(
            AnimationClip sourceClip,
            AnimationClip correctedClip,
            Transform staticModel,
            Transform turnModel)
        {
            if (correctedClip.name != Turn360ClipName ||
                Mathf.Abs(
                    correctedClip.length -
                    Turn360Duration) > 0.000001f ||
                Mathf.Abs(
                    correctedClip.frameRate -
                    Turn360FrameRate) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Revolution_07 corrected turn clip duration or frame rate differs from the 3-second/60-fps contract.");
            }

            var settings =
                AnimationUtility.GetAnimationClipSettings(
                    correctedClip);
            if (!settings.loopTime ||
                !correctedClip.isLooping ||
                correctedClip.wrapMode != WrapMode.Loop)
            {
                throw new InvalidOperationException(
                    "Revolution_07 corrected turn clip is not configured to loop.");
            }

            var armPaths =
                RequireMatchingStaticArmPaths(
                    staticModel,
                    turnModel);
            var editableArmPaths =
                new HashSet<string>(
                    armPaths.Values,
                    StringComparer.Ordinal);
            foreach (var boneName in
                     StaticArmBoneNames)
            {
                var expected =
                    RequireUniqueDescendant(
                            staticModel,
                            boneName)
                        .localRotation;
                RequireConstantRotationComponent(
                    correctedClip,
                    armPaths[boneName],
                    "m_LocalRotation.x",
                    expected.x);
                RequireConstantRotationComponent(
                    correctedClip,
                    armPaths[boneName],
                    "m_LocalRotation.y",
                    expected.y);
                RequireConstantRotationComponent(
                    correctedClip,
                    armPaths[boneName],
                    "m_LocalRotation.z",
                    expected.z);
                RequireConstantRotationComponent(
                    correctedClip,
                    armPaths[boneName],
                    "m_LocalRotation.w",
                    expected.w);
            }

            var maximumPreservedCurveError = 0f;
            foreach (var binding in
                     AnimationUtility.GetCurveBindings(
                         sourceClip))
            {
                if (IsHipsTransformBinding(binding) ||
                    IsEditableArmRotation(
                        binding,
                        editableArmPaths))
                {
                    continue;
                }

                var sourceCurve =
                    AnimationUtility.GetEditorCurve(
                        sourceClip,
                        binding) ??
                    throw new InvalidOperationException(
                        "A protected source curve is missing.");
                var correctedCurve =
                    AnimationUtility.GetEditorCurve(
                        correctedClip,
                        binding) ??
                    throw new InvalidOperationException(
                        "A protected corrected curve is missing: " +
                        binding.path + "/" +
                        binding.propertyName + ".");
                for (var frame = 0;
                     frame <= Turn360FrameCount;
                     frame++)
                {
                    var withinCycleFrame =
                        frame % HalfTurnFrameCount;
                    var expected =
                        sourceCurve.Evaluate(
                            withinCycleFrame /
                            (float)HalfTurnFrameCount *
                            sourceClip.length);
                    var actual =
                        correctedCurve.Evaluate(
                            frame /
                            (float)Turn360FrameRate);
                    maximumPreservedCurveError =
                        Mathf.Max(
                            maximumPreservedCurveError,
                            Mathf.Abs(expected - actual));
                }
            }

            if (maximumPreservedCurveError > 0.00001f)
            {
                throw new InvalidOperationException(
                    "Revolution_07 corrected turn changed a protected Mixamo stepping/body curve. MaximumCurveError=" +
                    maximumPreservedCurveError.ToString(
                        "0.#########",
                        CultureInfo.InvariantCulture) + ".");
            }

            var sampleInstance =
                UnityEngine.Object.Instantiate(
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        ImportedFbxPath));
            sampleInstance.hideFlags =
                HideFlags.HideAndDontSave;
            try
            {
                var animator =
                    sampleInstance.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }

                var hips =
                    RequireUniqueDescendant(
                        sampleInstance.transform,
                        "Hips");
                correctedClip.SampleAnimation(
                    sampleInstance,
                    0f);
                var startWorldRotation = hips.rotation;
                sourceClip.SampleAnimation(
                    sampleInstance,
                    0f);
                var sourceStart =
                    HorizontalDirection(hips.forward);
                sourceClip.SampleAnimation(
                    sampleInstance,
                    sourceClip.length);
                var sourceHalfTurn =
                    Vector3.SignedAngle(
                        sourceStart,
                        HorizontalDirection(hips.forward),
                        Vector3.up);
                var targetHalfTurn =
                    Mathf.Sign(sourceHalfTurn) * 180f;
                var sampleTimes =
                    new[]
                    {
                        0f,
                        0.75f,
                        1.5f,
                        2.25f,
                        3f
                    };
                var expectedDegrees =
                    new[]
                    {
                        0f,
                        targetHalfTurn * 0.5f,
                        targetHalfTurn,
                        targetHalfTurn * 1.5f,
                        targetHalfTurn * 2f
                    };
                var maximumHeadingError = 0f;
                for (var index = 0;
                     index < sampleTimes.Length;
                     index++)
                {
                    correctedClip.SampleAnimation(
                        sampleInstance,
                        sampleTimes[index]);
                    var expectedForward =
                        Quaternion.AngleAxis(
                            expectedDegrees[index],
                            Vector3.up) *
                        sourceStart;
                    maximumHeadingError =
                        Mathf.Max(
                            maximumHeadingError,
                            Vector3.Angle(
                                expectedForward,
                                HorizontalDirection(
                                    hips.forward)));
                }

                if (maximumHeadingError > 0.01f)
                {
                    throw new InvalidOperationException(
                        "Revolution_07 corrected turn heading differs from 0/90/180/270/360-degree samples. MaximumHeadingError=" +
                        maximumHeadingError.ToString(
                            "0.######",
                            CultureInfo.InvariantCulture) + ".");
                }

                correctedClip.SampleAnimation(
                    sampleInstance,
                    0f);
                var startPosition = hips.position;
                correctedClip.SampleAnimation(
                    sampleInstance,
                    Turn360Duration);
                var loopPositionError =
                    Vector3.Distance(
                        startPosition,
                        hips.position);
                var loopRotationError =
                    Quaternion.Angle(
                        startWorldRotation,
                        hips.rotation);
                if (loopPositionError > 0.0001f ||
                    loopRotationError > 0.01f)
                {
                    throw new InvalidOperationException(
                        "Revolution_07 corrected turn does not close at 3 seconds. PositionError=" +
                        loopPositionError.ToString(
                            "0.#########",
                            CultureInfo.InvariantCulture) +
                        ", RotationError=" +
                        loopRotationError.ToString(
                            "0.#########",
                            CultureInfo.InvariantCulture) + ".");
                }

                return new Turn360Metrics(
                    sourceHalfTurn,
                    targetHalfTurn,
                    maximumPreservedCurveError,
                    maximumHeadingError,
                    loopPositionError,
                    loopRotationError);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    sampleInstance);
            }
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
                    "Revolution_07 corrected arm curve is missing: " +
                    path + "/" + property + ".");
            if (curve.length != 2 ||
                Mathf.Abs(curve.Evaluate(0f) - expected) >
                    0.000001f ||
                Mathf.Abs(
                    curve.Evaluate(HalfTurnDuration) -
                    expected) > 0.000001f ||
                Mathf.Abs(
                    curve.Evaluate(Turn360Duration) -
                    expected) > 0.000001f)
            {
                throw new InvalidOperationException(
                    "Revolution_07 corrected arm curve does not remain at the Revolution_01 static angle: " +
                    path + "/" + property + ".");
            }
        }

        private static void WriteTurn360Inspection(
            AnimationClip sourceClip,
            AnimationClip correctedClip,
            AnimatorController controller,
            Transform staticModel,
            Transform turnModel,
            Turn360Metrics metrics)
        {
            var report = new StringBuilder();
            report.AppendLine(
                "Revolution_07 360 Degree / 3 Second Turn Inspection");
            report.AppendLine(
                "SourceClip=" + sourceClip.name);
            report.AppendLine(
                "CorrectedClip=" + Turn360ClipPath);
            report.AppendLine(
                "Controller=" + ControllerPath);
            report.AppendLine(
                "ControllerState=" + correctedClip.name);
            report.AppendLine("DurationSeconds=3");
            report.AppendLine("FrameRate=60");
            report.AppendLine(
                "SourceHalfTurnDegrees=" +
                metrics.SourceHalfTurnDegrees.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "NormalizedHalfTurnDegrees=" +
                metrics.HalfTurnDegrees.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "HeadingSamplesSeconds=0|0.75|1.5|2.25|3");
            report.AppendLine(
                "HeadingSamplesDegrees=0|" +
                (metrics.HalfTurnDegrees * 0.5f).ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                "|" +
                metrics.HalfTurnDegrees.ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                "|" +
                (metrics.HalfTurnDegrees * 1.5f).ToString(
                    "0.######",
                    CultureInfo.InvariantCulture) +
                "|" +
                (metrics.HalfTurnDegrees * 2f).ToString(
                    "0.######",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumPreservedMixamoCurveError=" +
                metrics.MaximumPreservedCurveError.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "MaximumHeadingErrorDegrees=" +
                metrics.MaximumRotationError.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "LoopPositionError=" +
                metrics.LoopPositionError.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "LoopRotationErrorDegrees=" +
                metrics.LoopRotationError.ToString(
                    "0.#########",
                    CultureInfo.InvariantCulture));
            report.AppendLine(
                "RootMotion=False");
            report.AppendLine(
                "Loop=True");
            report.AppendLine(
                "TurnConstruction=Two continuous normalized copies of the supplied Mixamo turn");
            report.AppendLine(
                "SecondHalfRootPosition=180-degree mirrored source trajectory around the inspected midpoint");
            report.AppendLine(
                "StaticArmReference=Revolution_01");
            report.AppendLine(
                "StaticArmBones=" +
                string.Join("|", StaticArmBoneNames));
            foreach (var boneName in
                     StaticArmBoneNames)
            {
                var staticBone =
                    RequireUniqueDescendant(
                        staticModel,
                        boneName);
                var turnBone =
                    RequireUniqueDescendant(
                        turnModel,
                        boneName);
                report.AppendLine(
                    "StaticArmRotation[" + boneName +
                    "]=" +
                    FormatQuaternion(
                        staticBone.localRotation));
                report.AppendLine(
                    "TurnArmPath[" + boneName +
                    "]=" +
                    AnimationUtility.CalculateTransformPath(
                        turnBone,
                        turnModel));
            }

            report.AppendLine(
                "VisualReview=" + CapturePath);
            report.AppendLine(
                "VisualColumnsSeconds=0|0.75|1.5|2.25|3");
            report.AppendLine(
                "StaticAndTurnMeshSame=True");
            report.AppendLine(
                "StaticAndTurnMaterialsSame=True");
            report.AppendLine(
                "OtherRevolutionSlotsChanged=False");
            report.AppendLine(
                "SourceFbxChanged=False");
            File.WriteAllText(
                Absolute(Turn360InspectionPath),
                report.ToString(),
                new UTF8Encoding(false));
        }

        private static string FormatQuaternion(
            Quaternion value)
        {
            return "(" +
                   value.x.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   "," +
                   value.y.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   "," +
                   value.z.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   "," +
                   value.w.ToString(
                       "0.#########",
                       CultureInfo.InvariantCulture) +
                   ")";
        }

        private readonly struct Turn360Metrics
        {
            public Turn360Metrics(
                float sourceHalfTurnDegrees,
                float halfTurnDegrees,
                float maximumPreservedCurveError,
                float maximumRotationError,
                float loopPositionError,
                float loopRotationError)
            {
                SourceHalfTurnDegrees =
                    sourceHalfTurnDegrees;
                HalfTurnDegrees = halfTurnDegrees;
                MaximumPreservedCurveError =
                    maximumPreservedCurveError;
                MaximumRotationError =
                    maximumRotationError;
                LoopPositionError =
                    loopPositionError;
                LoopRotationError =
                    loopRotationError;
            }

            public float SourceHalfTurnDegrees { get; }
            public float HalfTurnDegrees { get; }
            public float MaximumPreservedCurveError { get; }
            public float MaximumRotationError { get; }
            public float LoopPositionError { get; }
            public float LoopRotationError { get; }
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
                    "Revolution turn ModelImporter is missing.");
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
                    "Revolution turn ModelImporter was lost after its initial import.");
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
                    "The supplied Revolution turn FBX must expose exactly one Mixamo take. Candidates=" +
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
                    "Revolution turn FBX did not import exactly one selected Mixamo clip. Imported=" +
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
                    "The imported Revolution turn renderer has no mesh.");
            if (mesh.vertexCount <= 0 ||
                TriangleCount(mesh) != ExpectedTriangleCount ||
                renderer.bones.Length != ExpectedBoneCount)
            {
                throw new InvalidOperationException(
                    "The supplied Revolution turn geometry or rig differs from the current Revolution model. Vertices=" +
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
                    "The Revolution turn renderer hierarchy does not match Revolution_01. Static=" +
                    string.Join("|", staticRenderers.Keys) +
                    ", Turn=" +
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
                            "Turn renderer has no MeshFilter at " +
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
                    "Static and turn renderer paths differ after appearance synchronization.");
            }

            foreach (var pair in staticRenderers)
            {
                var source = pair.Value;
                var target = targetRenderers[pair.Key];
                if (!source.sharedMaterials.SequenceEqual(
                        target.sharedMaterials))
                {
                    throw new InvalidOperationException(
                        "Revolution_07 materials differ from Revolution_01 at " +
                        pair.Key + ".");
                }

                if (source is SkinnedMeshRenderer sourceSkinned &&
                    target is SkinnedMeshRenderer targetSkinned)
                {
                    if (sourceSkinned.sharedMesh !=
                        targetSkinned.sharedMesh)
                    {
                        throw new InvalidOperationException(
                            "Revolution_07 skinned mesh differs from Revolution_01 at " +
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
                            "Revolution_07 static mesh differs from Revolution_01 at " +
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
                    "Revolution_07 Animator contract differs.");
            }

            var clips =
                animator.runtimeAnimatorController.animationClips;
            if (clips.Length != 1 ||
                clips[0] != clip)
            {
                throw new InvalidOperationException(
                    "Revolution_07 controller must reference only the selected Mixamo turn clip.");
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
                    "Revolution_07 is not a direct instance of the supplied turn FBX. Source=" +
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
                    "A Revolution slot outside Revolution_07 changed.");
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
                    "Revolution_07 slot transform changed.");
            }
        }

        private static void CaptureVisualReview(
            Transform staticSlot,
            Transform turnSlot,
            GameObject turnModel,
            AnimationClip clip)
        {
            var staticStates =
                CaptureLayerStates(staticSlot);
            var turnStates =
                CaptureLayerStates(turnSlot);
            var cameraObject =
                new GameObject(
                    "Revolution_Turn_ReviewCamera",
                    typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var keyLightObject =
                new GameObject(
                    "Revolution_Turn_ReviewKey",
                    typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var fillLightObject =
                new GameObject(
                    "Revolution_Turn_ReviewFill",
                    typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            var panels = new List<Texture2D>();
            var animationModeStarted = false;

            try
            {
                SetLayerRecursively(staticSlot, ReviewLayer);
                SetLayerRecursively(turnSlot, ReviewLayer);
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
                    new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };
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

                AnimationMode.StartAnimationMode();
                animationModeStarted = true;
                foreach (var normalizedTime in normalizedTimes)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(
                        turnModel,
                        clip,
                        clip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    panels.Add(
                        RenderPanel(camera, turnSlot, 0f));
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
                        turnModel,
                        clip,
                        clip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    panels.Add(
                        RenderPanel(
                            camera,
                            turnSlot,
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
                RestoreLayerStates(turnStates);
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

            var bounds = BakedWorldBounds(mainRenderer);

            var viewDirection =
                (slot.forward +
                 slot.right * oblique).normalized;
            var radius =
                Mathf.Max(
                    bounds.extents.magnitude,
                    bounds.extents.y);
            var distance =
                radius /
                Mathf.Tan(
                    camera.fieldOfView *
                    0.5f *
                    Mathf.Deg2Rad) *
                1.2f;
            camera.transform.position =
                bounds.center + viewDirection * distance;
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

        private static Bounds BakedWorldBounds(
            SkinnedMeshRenderer renderer)
        {
            var bakedMesh = new Mesh
            {
                name = "Revolution_Turn_Review_BakedMesh"
            };
            try
            {
                renderer.BakeMesh(bakedMesh);
                var local = bakedMesh.bounds;
                var transform = renderer.transform;
                var worldCenter =
                    transform.TransformPoint(local.center);
                var worldExtentsX =
                    transform.TransformVector(
                        new Vector3(local.extents.x, 0f, 0f));
                var worldExtentsY =
                    transform.TransformVector(
                        new Vector3(0f, local.extents.y, 0f));
                var worldExtentsZ =
                    transform.TransformVector(
                        new Vector3(0f, 0f, local.extents.z));
                var worldExtents =
                    new Vector3(
                        Mathf.Abs(worldExtentsX.x) +
                        Mathf.Abs(worldExtentsY.x) +
                        Mathf.Abs(worldExtentsZ.x),
                        Mathf.Abs(worldExtentsX.y) +
                        Mathf.Abs(worldExtentsY.y) +
                        Mathf.Abs(worldExtentsZ.y),
                        Mathf.Abs(worldExtentsX.z) +
                        Mathf.Abs(worldExtentsY.z) +
                        Mathf.Abs(worldExtentsZ.z));
                return new Bounds(
                    worldCenter,
                    worldExtents * 2f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
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
                    "Unexpected Revolution turn review panel count.");
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
                    "Revolution_07");
            var builder = new StringBuilder();
            builder.AppendLine(
                "Revolution 07 Turn Inspection");
            builder.AppendLine(
                "Source=" + SourceFbxPath);
            builder.AppendLine(
                "ImportedSource=" + ImportedFbxPath);
            builder.AppendLine(
                "SourceSha256=" + SourceSha256);
            builder.AppendLine(
                "Slot=" + TurnSlotName);
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
                "State=" + StateName);
            builder.AppendLine(
                "StaticMesh=" +
                AssetDatabase.GetAssetPath(
                    staticRenderer.sharedMesh));
            builder.AppendLine(
                "TurnMesh=" +
                AssetDatabase.GetAssetPath(
                    replacementRenderer.sharedMesh));
            builder.AppendLine(
                "StaticAndTurnMeshSame=" +
                (staticRenderer.sharedMesh ==
                 replacementRenderer.sharedMesh));
            builder.AppendLine(
                "ApprovedMaterials=" +
                string.Join(
                    "|",
                    staticRenderer.sharedMaterials.Select(
                        AssetDatabase.GetAssetPath)));
            builder.AppendLine(
                "StaticAndTurnMaterialsSame=" +
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
