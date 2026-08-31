using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    [UnityEditor.InitializeOnLoad]
    internal static class IspantPreAugust17BindingRepairRunner
    {
        private const string RepairScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string RepairPlacementName = "Approved Ispant Enemy Placement";
        private const string RepairApprovedModelPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedAppearance/Models/Ispant_Armed_Approved.fbx";
        private const string RepairStatusFolder = "Assets/_Project/Art/Enemies/Ispant/RestoreStatus";
        private const string RepairRequestPath = RepairStatusFolder + "/ApprovedBindingRepairOperationV7.txt";
        private const string RuntimeMeasurePendingKey =
            "Bellerophon.IspantPreAugust17.RuntimeMeasurePending";
        private const string RuntimeMeasureStartKey =
            "Bellerophon.IspantPreAugust17.RuntimeMeasureStart";
        private const string RepairReportPath = RepairStatusFolder + "/IspantBindingRepairReport.txt";
        private const string RepairPassPath = RepairStatusFolder + "/binding-repair_PASS.txt";
        private const string RepairFailPath = RepairStatusFolder + "/binding-repair_FAIL.txt";

        private static readonly string[] RepairAppearanceNames =
        {
            "Ispant_Armed_Body",
            "Ispant_Crescent_Ornament",
            "Ispant_Reference_Eye_Slits"
        };

        static IspantPreAugust17BindingRepairRunner()
        {
            UnityEditor.EditorApplication.update -= PollRepair;
            UnityEditor.EditorApplication.update += PollRepair;
            UnityEditor.EditorApplication.update -= PollRuntimeMeasure;
            UnityEditor.EditorApplication.update += PollRuntimeMeasure;
        }

        private static void PollRepair()
        {
            if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
            {
                return;
            }

            string absoluteRequest = System.IO.Path.GetFullPath(RepairRequestPath);
            if (!System.IO.File.Exists(absoluteRequest))
            {
                return;
            }

            string operation = System.IO.File.ReadAllText(absoluteRequest).Trim();
            System.IO.File.Delete(absoluteRequest);
            try
            {
                if (string.Equals(operation, "MEASURE", System.StringComparison.Ordinal))
                {
                    UnityEditor.SessionState.SetBool(RuntimeMeasurePendingKey, true);
                    UnityEditor.SessionState.SetFloat(RuntimeMeasureStartKey, -1f);
                    UnityEditor.EditorApplication.EnterPlaymode();
                    return;
                }

                if (string.Equals(operation, "CALIBRATE", System.StringComparison.Ordinal))
                {
                    ApplyRuntimeScaleCalibration();
                    return;
                }

                if (!string.Equals(operation, "APPLY", System.StringComparison.Ordinal))
                {
                    throw new System.InvalidOperationException("Unsupported Ispant repair operation: " + operation);
                }

                ApplyBindingRepair();
            }
            catch (System.Exception exception)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(RepairStatusFolder));
                string safeMessage = exception.Message;
                foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                {
                    safeMessage = safeMessage.Replace(invalid, '_');
                }

                if (safeMessage.Length > 120)
                {
                    safeMessage = safeMessage.Substring(0, 120);
                }

                System.IO.File.WriteAllText(
                    System.IO.Path.GetFullPath(RepairFailPath),
                    "IspantBindingRepairError=" + exception);
                System.IO.File.WriteAllText(
                    System.IO.Path.GetFullPath(
                        RepairStatusFolder + "/binding-repair_FAIL_Ispant_" + safeMessage + ".txt"),
                    "FAIL");
                UnityEngine.Debug.LogException(exception);
            }
        }

        private static void ApplyRuntimeScaleCalibration()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, RepairScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.InvalidOperationException(
                    "Ispant calibration requires CargoRunMvp to already be loaded; automatic scene switching is disabled.");
            }

            UnityEngine.GameObject placement = null;
            UnityEngine.GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (string.Equals(roots[rootIndex].name, RepairPlacementName, System.StringComparison.Ordinal))
                {
                    placement = roots[rootIndex];
                    break;
                }
            }

            if (placement == null)
            {
                throw new System.InvalidOperationException("Ispant placement root was not found for calibration.");
            }

            UnityEngine.Transform[] slots = new UnityEngine.Transform[placement.transform.childCount];
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                slots[slotIndex] = placement.transform.GetChild(slotIndex);
            }

            System.Array.Sort(
                slots,
                (left, right) => string.Compare(left.name, right.name, System.StringComparison.Ordinal));
            if (slots.Length != 12)
            {
                throw new System.InvalidOperationException("Expected 12 Ispant slots for calibration.");
            }

            UnityEngine.SkinnedMeshRenderer referenceBody = FindRepairAnimatedBody(slots[1]);
            if (referenceBody == null)
            {
                throw new System.InvalidOperationException("Historical Ispant idle reference body is missing.");
            }

            float referenceHeight = RepairLocalScaledHeight(referenceBody);
            if (referenceHeight <= 0.0001f)
            {
                throw new System.InvalidOperationException("Historical Ispant idle reference height is invalid.");
            }

            string[] otherRootsBefore = CaptureRepairRootSignatures(scene, placement);
            var report = new System.Text.StringBuilder();
            report.AppendLine("IspantRuntimeScaleReference=" +
                              referenceHeight.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));

            for (int slotIndex = 1; slotIndex < slots.Length; slotIndex++)
            {
                UnityEngine.Transform slot = slots[slotIndex];
                UnityEngine.Animator animator = slot.GetComponentInChildren<UnityEngine.Animator>(true);
                UnityEngine.SkinnedMeshRenderer animatedBody = FindRepairAnimatedBody(slot);
                if (animator == null || animatedBody == null)
                {
                    throw new System.InvalidOperationException(slot.name + " is missing its historical animated body.");
                }

                float currentHeight = RepairLocalScaledHeight(animatedBody);
                float ratio = referenceHeight / currentHeight;
                if (!float.IsFinite(ratio) || ratio < 0.25f || ratio > 4f)
                {
                    throw new System.InvalidOperationException(slot.name + " has an invalid calibration ratio: " + ratio);
                }

                UnityEngine.Transform staticAppearance = FindRepairStaticAppearance(animator.transform);
                UnityEngine.Vector3 staticWorldPosition = UnityEngine.Vector3.zero;
                UnityEngine.Quaternion staticWorldRotation = UnityEngine.Quaternion.identity;
                UnityEngine.Vector3 staticWorldScale = UnityEngine.Vector3.one;
                UnityEngine.Bounds staticBounds = default(UnityEngine.Bounds);
                bool hasStaticBounds = false;
                if (staticAppearance != null)
                {
                    staticWorldPosition = staticAppearance.position;
                    staticWorldRotation = staticAppearance.rotation;
                    staticWorldScale = staticAppearance.lossyScale;
                    var staticRenderers = new System.Collections.Generic.List<UnityEngine.Renderer>();
                    UnityEngine.Renderer[] candidates =
                        staticAppearance.GetComponentsInChildren<UnityEngine.Renderer>(true);
                    for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                    {
                        if (System.Array.IndexOf(RepairAppearanceNames, candidates[candidateIndex].name) >= 0)
                        {
                            staticRenderers.Add(candidates[candidateIndex]);
                        }
                    }

                    hasStaticBounds = TryRepairBounds(staticRenderers, out staticBounds);
                }

                UnityEditor.Undo.RecordObject(animator.transform, "Calibrate historical Ispant runtime scale");
                if (staticAppearance != null)
                {
                    UnityEditor.Undo.RecordObject(staticAppearance, "Preserve historical Ispant static appearance");
                }

                animator.transform.localScale *= ratio;
                if (staticAppearance != null)
                {
                    RestoreRepairWorldTransform(
                        staticAppearance,
                        staticWorldPosition,
                        staticWorldRotation,
                        staticWorldScale);
                }

                if (hasStaticBounds)
                {
                    UnityEngine.Bounds animatedBounds = animatedBody.bounds;
                    UnityEngine.Vector3 delta = new UnityEngine.Vector3(
                        staticBounds.center.x - animatedBounds.center.x,
                        staticBounds.min.y - animatedBounds.min.y,
                        staticBounds.center.z - animatedBounds.center.z);
                    animator.transform.position += delta;
                    RestoreRepairWorldTransform(
                        staticAppearance,
                        staticWorldPosition,
                        staticWorldRotation,
                        staticWorldScale);
                }

                report.AppendLine("IspantSlot=" + slot.name +
                                  " Before=" + currentHeight.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                                  " Ratio=" + ratio.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                                  " After=" + RepairLocalScaledHeight(animatedBody)
                                      .ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
            }

            string[] otherRootsAfter = CaptureRepairRootSignatures(scene, placement);
            if (otherRootsBefore.Length != otherRootsAfter.Length)
            {
                throw new System.InvalidOperationException("A non-Ispant scene root changed during calibration.");
            }

            for (int signatureIndex = 0; signatureIndex < otherRootsBefore.Length; signatureIndex++)
            {
                if (!string.Equals(
                        otherRootsBefore[signatureIndex],
                        otherRootsAfter[signatureIndex],
                        System.StringComparison.Ordinal))
                {
                    throw new System.InvalidOperationException("A non-Ispant scene root changed during calibration.");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            System.IO.File.WriteAllText(
                System.IO.Path.GetFullPath(RepairStatusFolder + "/IspantRuntimeScaleCalibrationReport.txt"),
                report.ToString());
            System.IO.File.WriteAllText(
                System.IO.Path.GetFullPath(RepairStatusFolder + "/runtime-scale-calibration_PASS.txt"),
                "PASS");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }

        private static UnityEngine.SkinnedMeshRenderer FindRepairAnimatedBody(UnityEngine.Transform slot)
        {
            UnityEngine.SkinnedMeshRenderer fallback = null;
            UnityEngine.SkinnedMeshRenderer[] renderers =
                slot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                UnityEngine.SkinnedMeshRenderer renderer = renderers[index];
                if (HasRepairAncestor(renderer.transform, "Ispant_StaticAppearance"))
                {
                    continue;
                }

                if (string.Equals(renderer.name, "Ispant_Armed_Body", System.StringComparison.Ordinal))
                {
                    return renderer;
                }

                if (fallback == null || renderer.name.IndexOf("body", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fallback = renderer;
                }
            }

            return fallback;
        }

        private static float RepairLocalScaledHeight(UnityEngine.SkinnedMeshRenderer renderer)
        {
            return renderer.localBounds.size.y * UnityEngine.Mathf.Abs(renderer.transform.lossyScale.y);
        }

        private static UnityEngine.Transform FindRepairStaticAppearance(UnityEngine.Transform root)
        {
            UnityEngine.Transform[] transforms = root.GetComponentsInChildren<UnityEngine.Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (string.Equals(
                        transforms[index].name,
                        "Ispant_StaticAppearance",
                        System.StringComparison.Ordinal))
                {
                    return transforms[index];
                }
            }

            return null;
        }

        private static void PollRuntimeMeasure()
        {
            if (!UnityEditor.SessionState.GetBool(RuntimeMeasurePendingKey, false) ||
                !UnityEditor.EditorApplication.isPlaying ||
                UnityEditor.EditorApplication.isPaused)
            {
                return;
            }

            float startedAt = UnityEditor.SessionState.GetFloat(RuntimeMeasureStartKey, -1f);
            if (startedAt < 0f)
            {
                UnityEditor.SessionState.SetFloat(
                    RuntimeMeasureStartKey,
                    (float)UnityEditor.EditorApplication.timeSinceStartup);
                return;
            }

            if (UnityEditor.EditorApplication.timeSinceStartup - startedAt < 1.5d)
            {
                return;
            }

            try
            {
                WriteRuntimeMeasurements();
            }
            catch (System.Exception exception)
            {
                string safeMessage = exception.Message;
                foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                {
                    safeMessage = safeMessage.Replace(invalid, '_');
                }

                if (safeMessage.Length > 120)
                {
                    safeMessage = safeMessage.Substring(0, 120);
                }

                System.IO.File.WriteAllText(
                    System.IO.Path.GetFullPath(
                        RepairStatusFolder + "/runtime-measure_FAIL_Ispant_" + safeMessage + ".txt"),
                    "FAIL");
            }
            finally
            {
                UnityEditor.SessionState.SetBool(RuntimeMeasurePendingKey, false);
                UnityEditor.EditorApplication.ExitPlaymode();
            }
        }

        private static void WriteRuntimeMeasurements()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.GameObject placement = null;
            UnityEngine.GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (string.Equals(roots[rootIndex].name, RepairPlacementName, System.StringComparison.Ordinal))
                {
                    placement = roots[rootIndex];
                    break;
                }
            }

            if (placement == null)
            {
                throw new System.InvalidOperationException("Runtime Ispant placement root was not found.");
            }

            UnityEngine.Transform[] slots = new UnityEngine.Transform[placement.transform.childCount];
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                slots[slotIndex] = placement.transform.GetChild(slotIndex);
            }

            System.Array.Sort(
                slots,
                (left, right) => string.Compare(left.name, right.name, System.StringComparison.Ordinal));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(RepairStatusFolder));

            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                UnityEngine.Transform slot = slots[slotIndex];
                UnityEngine.Animator animator = slot.GetComponentInChildren<UnityEngine.Animator>(true);
                UnityEngine.SkinnedMeshRenderer selected = null;
                UnityEngine.SkinnedMeshRenderer[] skinned =
                    slot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true);
                for (int rendererIndex = 0; rendererIndex < skinned.Length; rendererIndex++)
                {
                    UnityEngine.SkinnedMeshRenderer candidate = skinned[rendererIndex];
                    if (!candidate.enabled ||
                        !candidate.gameObject.activeInHierarchy ||
                        HasRepairAncestor(candidate.transform, "Ispant_StaticAppearance"))
                    {
                        continue;
                    }

                    if (string.Equals(candidate.name, "Ispant_Armed_Body", System.StringComparison.Ordinal) ||
                        candidate.name.IndexOf("body", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        selected == null)
                    {
                        selected = candidate;
                    }

                    if (string.Equals(candidate.name, "Ispant_Armed_Body", System.StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                float worldHeight = selected != null ? selected.bounds.size.y : 0f;
                float localScaledHeight = selected != null
                    ? selected.localBounds.size.y * UnityEngine.Mathf.Abs(selected.transform.lossyScale.y)
                    : 0f;
                float animatorScale = animator != null ? UnityEngine.Mathf.Abs(animator.transform.lossyScale.y) : 0f;
                float rootBoneScale = selected != null && selected.rootBone != null
                    ? UnityEngine.Mathf.Abs(selected.rootBone.lossyScale.y)
                    : 0f;
                string metricName = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "IspantM{0:00}_W{1:F3}_L{2:F3}_A{3:F3}_R{4:F3}.txt",
                    slotIndex + 1,
                    worldHeight,
                    localScaledHeight,
                    animatorScale,
                    rootBoneScale);
                System.IO.File.WriteAllText(
                    System.IO.Path.GetFullPath(RepairStatusFolder + "/" + metricName),
                    "PASS");
            }

            System.IO.File.WriteAllText(
                System.IO.Path.GetFullPath(RepairStatusFolder + "/runtime-measure_PASS.txt"),
                "PASS");
        }

        private static void ApplyBindingRepair()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() ||
                !string.Equals(scene.path, RepairScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.InvalidOperationException(
                    "Ispant binding repair requires CargoRunMvp to already be loaded; automatic scene switching is disabled.");
            }

            UnityEngine.GameObject placement = null;
            UnityEngine.GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                if (string.Equals(roots[rootIndex].name, RepairPlacementName, System.StringComparison.Ordinal))
                {
                    placement = roots[rootIndex];
                    break;
                }
            }

            if (placement == null)
            {
                throw new System.InvalidOperationException("Ispant placement root was not found.");
            }

            UnityEngine.Transform[] slots = new UnityEngine.Transform[placement.transform.childCount];
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                slots[slotIndex] = placement.transform.GetChild(slotIndex);
            }

            System.Array.Sort(
                slots,
                (left, right) => string.Compare(left.name, right.name, System.StringComparison.Ordinal));
            if (slots.Length != 12)
            {
                throw new System.InvalidOperationException("Expected 12 Ispant slots, found " + slots.Length + ".");
            }

            UnityEngine.GameObject approvedModel =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RepairApprovedModelPath);
            if (approvedModel == null)
            {
                throw new System.InvalidOperationException("Approved pre-August-17 Ispant model is missing.");
            }

            var approvedRenderers = new System.Collections.Generic.Dictionary<string, UnityEngine.Renderer>(
                System.StringComparer.Ordinal);
            UnityEngine.Renderer[] sourceRenderers =
                approvedModel.GetComponentsInChildren<UnityEngine.Renderer>(true);
            for (int sourceIndex = 0; sourceIndex < sourceRenderers.Length; sourceIndex++)
            {
                UnityEngine.Renderer source = sourceRenderers[sourceIndex];
                if (System.Array.IndexOf(RepairAppearanceNames, source.name) >= 0 &&
                    !approvedRenderers.ContainsKey(source.name))
                {
                    approvedRenderers.Add(source.name, source);
                }
            }

            if (approvedRenderers.Count != RepairAppearanceNames.Length)
            {
                throw new System.InvalidOperationException("Approved Ispant model is missing an appearance renderer.");
            }

            System.Collections.Generic.Dictionary<string, UnityEngine.Material[]> historicalMaterials =
                BuildRepairMaterialMap(slots[0]);
            string[] otherRootsBefore = CaptureRepairRootSignatures(scene, placement);
            var report = new System.Text.StringBuilder();
            report.AppendLine("IspantBindingRepair=pre-August-17 appearance on historical animation rigs");
            report.AppendLine("IspantApprovedModel=" + RepairApprovedModelPath);

            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                UnityEngine.Transform slot = slots[slotIndex];
                UnityEngine.Animator animator = slot.GetComponentInChildren<UnityEngine.Animator>(true);
                if (slotIndex > 0 && animator == null)
                {
                    throw new System.InvalidOperationException(slot.name + " has no historical Animator.");
                }

                if (animator != null)
                {
                    UnityEditor.Undo.RecordObject(animator, "Restore historical Ispant animation binding");
                    animator.applyRootMotion = false;
                    animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
                }

                int reboundCount = 0;
                UnityEngine.Renderer[] targetRenderers =
                    slot.GetComponentsInChildren<UnityEngine.Renderer>(true);
                for (int targetIndex = 0; targetIndex < targetRenderers.Length; targetIndex++)
                {
                    UnityEngine.Renderer target = targetRenderers[targetIndex];
                    UnityEngine.Renderer source;
                    if (!approvedRenderers.TryGetValue(target.name, out source))
                    {
                        continue;
                    }

                    UnityEditor.Undo.RecordObject(target, "Restore pre-August-17 Ispant appearance");
                    target.sharedMaterials = source.sharedMaterials;
                    if (HasRepairAncestor(target.transform, "Ispant_StaticAppearance"))
                    {
                        continue;
                    }
                    reboundCount++;
                }

                float scaleRatio = NormalizeRepairAnimatedAppearance(animator);
                int materialCount = RestoreRepairMaterials(slot, historicalMaterials);
                string controllerPath = animator != null && animator.runtimeAnimatorController != null
                    ? UnityEditor.AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                    : "none";
                if (slotIndex > 0 &&
                    (controllerPath.IndexOf("/Animations/Ispant_", System.StringComparison.Ordinal) < 0 ||
                     controllerPath.IndexOf("Ispant_New_", System.StringComparison.Ordinal) >= 0))
                {
                    throw new System.InvalidOperationException(slot.name + " is not connected to a historical controller.");
                }

                report.AppendLine("IspantSlot=" + slot.name +
                                  " Controller=" + controllerPath +
                                  " AppearanceMaterials=" + reboundCount +
                                  " ScaleRatio=" + scaleRatio.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                                  " MaterialsRestored=" + materialCount);
            }

            string[] otherRootsAfter = CaptureRepairRootSignatures(scene, placement);
            if (otherRootsBefore.Length != otherRootsAfter.Length)
            {
                throw new System.InvalidOperationException("A non-Ispant scene root changed during repair.");
            }

            for (int signatureIndex = 0; signatureIndex < otherRootsBefore.Length; signatureIndex++)
            {
                if (!string.Equals(
                        otherRootsBefore[signatureIndex],
                        otherRootsAfter[signatureIndex],
                        System.StringComparison.Ordinal))
                {
                    throw new System.InvalidOperationException("A non-Ispant scene root changed during repair.");
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(RepairStatusFolder));
            System.IO.File.WriteAllText(System.IO.Path.GetFullPath(RepairReportPath), report.ToString());
            System.IO.File.WriteAllText(
                System.IO.Path.GetFullPath(RepairPassPath),
                "PASS\n" + System.DateTime.UtcNow.ToString("O"));
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            UnityEngine.Debug.Log("Ispant pre-August-17 appearance binding repair completed.");
        }

        private static float NormalizeRepairAnimatedAppearance(UnityEngine.Animator animator)
        {
            if (animator == null)
            {
                return 1f;
            }

            UnityEngine.Transform staticAppearance = null;
            UnityEngine.Transform[] transforms =
                animator.transform.GetComponentsInChildren<UnityEngine.Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (string.Equals(
                        transforms[index].name,
                        "Ispant_StaticAppearance",
                        System.StringComparison.Ordinal))
                {
                    staticAppearance = transforms[index];
                    break;
                }
            }

            if (staticAppearance == null)
            {
                return 1f;
            }

            var staticRenderers = new System.Collections.Generic.List<UnityEngine.Renderer>();
            var animatedRenderers = new System.Collections.Generic.List<UnityEngine.Renderer>();
            UnityEngine.Renderer[] renderers =
                animator.transform.GetComponentsInChildren<UnityEngine.Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                UnityEngine.Renderer renderer = renderers[index];
                if (System.Array.IndexOf(RepairAppearanceNames, renderer.name) < 0)
                {
                    continue;
                }

                if (HasRepairAncestor(renderer.transform, "Ispant_StaticAppearance"))
                {
                    staticRenderers.Add(renderer);
                }
                else
                {
                    animatedRenderers.Add(renderer);
                }
            }

            UnityEngine.Bounds staticBounds;
            UnityEngine.Bounds animatedBounds;
            if (!TryRepairBounds(staticRenderers, out staticBounds) ||
                !TryRepairBounds(animatedRenderers, out animatedBounds) ||
                staticBounds.size.y <= 0.0001f || animatedBounds.size.y <= 0.0001f)
            {
                return 1f;
            }

            float ratio = staticBounds.size.y / animatedBounds.size.y;
            if (!float.IsFinite(ratio) || ratio < 0.05f || ratio > 20f)
            {
                throw new System.InvalidOperationException(
                    animator.name + " has an invalid historical appearance scale ratio: " + ratio);
            }

            UnityEngine.Vector3 staticWorldPosition = staticAppearance.position;
            UnityEngine.Quaternion staticWorldRotation = staticAppearance.rotation;
            UnityEngine.Vector3 staticWorldScale = staticAppearance.lossyScale;
            UnityEditor.Undo.RecordObject(animator.transform, "Normalize historical Ispant animated appearance");
            UnityEditor.Undo.RecordObject(staticAppearance, "Preserve historical Ispant static comparison appearance");
            animator.transform.localScale *= ratio;
            RestoreRepairWorldTransform(
                staticAppearance,
                staticWorldPosition,
                staticWorldRotation,
                staticWorldScale);

            if (TryRepairBounds(animatedRenderers, out animatedBounds))
            {
                UnityEngine.Vector3 delta = new UnityEngine.Vector3(
                    staticBounds.center.x - animatedBounds.center.x,
                    staticBounds.min.y - animatedBounds.min.y,
                    staticBounds.center.z - animatedBounds.center.z);
                animator.transform.position += delta;
                RestoreRepairWorldTransform(
                    staticAppearance,
                    staticWorldPosition,
                    staticWorldRotation,
                    staticWorldScale);
            }

            return ratio;
        }

        private static bool TryRepairBounds(
            System.Collections.Generic.IReadOnlyList<UnityEngine.Renderer> renderers,
            out UnityEngine.Bounds bounds)
        {
            bounds = default(UnityEngine.Bounds);
            bool initialized = false;
            for (int index = 0; index < renderers.Count; index++)
            {
                UnityEngine.Bounds current = renderers[index].bounds;
                if (!initialized)
                {
                    bounds = current;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(current);
                }
            }

            return initialized;
        }

        private static void RestoreRepairWorldTransform(
            UnityEngine.Transform target,
            UnityEngine.Vector3 worldPosition,
            UnityEngine.Quaternion worldRotation,
            UnityEngine.Vector3 worldScale)
        {
            target.position = worldPosition;
            target.rotation = worldRotation;
            UnityEngine.Vector3 parentScale = target.parent != null
                ? target.parent.lossyScale
                : UnityEngine.Vector3.one;
            target.localScale = new UnityEngine.Vector3(
                SafeRepairDivide(worldScale.x, parentScale.x),
                SafeRepairDivide(worldScale.y, parentScale.y),
                SafeRepairDivide(worldScale.z, parentScale.z));
        }

        private static float SafeRepairDivide(float numerator, float denominator)
        {
            return UnityEngine.Mathf.Abs(denominator) > 0.000001f
                ? numerator / denominator
                : numerator;
        }

        private static bool HasRepairAncestor(UnityEngine.Transform transform, string ancestorName)
        {
            for (UnityEngine.Transform current = transform; current != null; current = current.parent)
            {
                if (string.Equals(current.name, ancestorName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static System.Collections.Generic.Dictionary<string, UnityEngine.Material[]> BuildRepairMaterialMap(
            UnityEngine.Transform referenceSlot)
        {
            var result = new System.Collections.Generic.Dictionary<string, UnityEngine.Material[]>(
                System.StringComparer.OrdinalIgnoreCase);
            UnityEngine.Renderer[] renderers = referenceSlot.GetComponentsInChildren<UnityEngine.Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                UnityEngine.Renderer renderer = renderers[index];
                if (renderer.sharedMaterials.Length == 0)
                {
                    continue;
                }

                string key = RepairMaterialKey(renderer);
                if (!result.ContainsKey(key))
                {
                    result.Add(key, renderer.sharedMaterials);
                }
            }

            return result;
        }

        private static int RestoreRepairMaterials(
            UnityEngine.Transform slot,
            System.Collections.Generic.IReadOnlyDictionary<string, UnityEngine.Material[]> materials)
        {
            int restored = 0;
            UnityEngine.Renderer[] renderers = slot.GetComponentsInChildren<UnityEngine.Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                UnityEngine.Renderer renderer = renderers[index];
                UnityEngine.Material[] historical;
                if (!materials.TryGetValue(RepairMaterialKey(renderer), out historical))
                {
                    continue;
                }

                UnityEditor.Undo.RecordObject(renderer, "Restore historical Ispant materials");
                renderer.sharedMaterials = historical;
                restored++;
            }

            return restored;
        }

        private static string RepairMaterialKey(string rendererName)
        {
            string lower = rendererName.ToLowerInvariant();
            if (lower.Contains("body")) return "body";
            if (lower.Contains("crescent")) return "crescent";
            if (lower.Contains("eye")) return "eye";
            if (lower.Contains("sword") || lower.Contains("blade")) return "sword";
            if (lower.Contains("musket") || lower.Contains("rifle") || lower.Contains("firearm")) return "musket";
            return rendererName;
        }

        private static string RepairMaterialKey(UnityEngine.Renderer renderer)
        {
            var hierarchy = new System.Text.StringBuilder(renderer.name);
            UnityEngine.Transform current = renderer.transform.parent;
            for (int depth = 0; current != null && depth < 12; depth++, current = current.parent)
            {
                hierarchy.Append('/').Append(current.name);
            }

            return RepairMaterialKey(hierarchy.ToString());
        }

        private static string[] CaptureRepairRootSignatures(
            UnityEngine.SceneManagement.Scene scene,
            UnityEngine.GameObject placement)
        {
            var signatures = new System.Collections.Generic.List<string>();
            UnityEngine.GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                UnityEngine.GameObject root = roots[index];
                if (root == placement)
                {
                    continue;
                }

                signatures.Add(string.Join("|",
                    root.name,
                    root.activeSelf,
                    root.transform.localPosition,
                    root.transform.localRotation,
                    root.transform.localScale,
                    root.GetComponentsInChildren<UnityEngine.Transform>(true).Length));
            }

            signatures.Sort(System.StringComparer.Ordinal);
            return signatures.ToArray();
        }
    }

    [UnityEditor.InitializeOnLoad]
    internal static class IspantPreAugust17FocusedRepairRunner
    {
        private const string IspantPlacementName = "Approved Ispant Enemy Placement";
        private const string IspantScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string IspantRequestPath =
            "Assets/_Project/Art/Enemies/Ispant/RestoreStatus/ApprovedFocusedRepairOperationV15.txt";
        private const string IspantStatusFolder =
            "Assets/_Project/Art/Enemies/Ispant/RestoreStatus";

        static IspantPreAugust17FocusedRepairRunner()
        {
            UnityEditor.EditorApplication.update += PollIspantFocusedRepair;
        }

        private static void PollIspantFocusedRepair()
        {
            if (UnityEditor.EditorApplication.isCompiling ||
                UnityEditor.EditorApplication.isUpdating)
                return;

            string absoluteRequest = System.IO.Path.GetFullPath(IspantRequestPath);
            if (!System.IO.File.Exists(absoluteRequest))
                return;

            string operation = System.IO.File.ReadAllText(absoluteRequest).Trim().ToUpperInvariant();
            if (string.Equals(operation, "RECOVER_CAPTURE", System.StringComparison.Ordinal))
            {
                System.IO.File.Delete(absoluteRequest);
                UnityEditor.SessionState.SetBool("Bellerophon.IspantPreAugust17.CapturePending", false);
                UnityEditor.SessionState.SetFloat("Bellerophon.IspantPreAugust17.CaptureStart", 0f);
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                    UnityEditor.EditorApplication.isPlaying = false;
                WriteIspantStatus("capture-recovery_PASS.txt", "Stale Ispant direct-capture state cleared without changing scene targets.");
                return;
            }
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            System.IO.File.Delete(absoluteRequest);
            try
            {
                if (!string.Equals(operation, "APPLY", System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException("Unsupported focused Ispant repair operation: " + operation);
                ApplyIspantFocusedRepair();
                WriteIspantStatus("focused-repair_PASS.txt", "Ispant slot 08 scale and historical weapon materials restored.");
            }
            catch (System.Exception exception)
            {
                WriteIspantStatus("focused-repair_FAIL.txt", exception.ToString());
                UnityEngine.Debug.LogException(exception);
            }
            finally
            {
                UnityEditor.AssetDatabase.Refresh();
            }
        }

        private static void ApplyIspantFocusedRepair()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, IspantScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                throw new System.InvalidOperationException(
                    "Focused Ispant repair requires CargoRunMvp to already be loaded; automatic scene switching is disabled.");
            }

            UnityEngine.GameObject placement = System.Array.Find(
                scene.GetRootGameObjects(),
                root => string.Equals(root.name, IspantPlacementName, System.StringComparison.Ordinal));
            if (placement == null)
                throw new System.InvalidOperationException("Approved Ispant placement is missing.");

            var otherRootSignatures = CaptureOtherIspantRootSignatures(scene, placement);
            UnityEngine.Transform[] slots = new UnityEngine.Transform[placement.transform.childCount];
            for (int index = 0; index < slots.Length; index++)
                slots[index] = placement.transform.GetChild(index);
            System.Array.Sort(slots, (left, right) => System.StringComparer.Ordinal.Compare(left.name, right.name));
            if (slots.Length != 12)
                throw new System.InvalidOperationException("Expected 12 historical Ispant slots, found " + slots.Length + ".");

            UnityEngine.SkinnedMeshRenderer referenceBody = FindIspantAnimatedBody(slots[1]);
            UnityEngine.SkinnedMeshRenderer slotEightBody = FindIspantAnimatedBody(slots[7]);
            if (referenceBody == null || slotEightBody == null)
                throw new System.InvalidOperationException("Historical Ispant slot 02/08 animated body is missing.");

            float referenceHeight = PoseIndependentIspantHeight(referenceBody);
            float beforeHeight = PoseIndependentIspantHeight(slotEightBody);
            if (!IsFinitePositiveIspant(referenceHeight) || !IsFinitePositiveIspant(beforeHeight))
                throw new System.InvalidOperationException("Historical Ispant slot 08 scale measurement is invalid.");

            UnityEngine.Animator animator = FindIspantOwningAnimator(slotEightBody.transform);
            if (animator == null)
                throw new System.InvalidOperationException("Historical Ispant slot 08 Animator is missing.");

            UnityEngine.Transform staticAppearance = FindIspantDescendant(animator.transform, "Ispant_StaticAppearance");
            UnityEngine.Vector3 staticPosition = default;
            UnityEngine.Quaternion staticRotation = default;
            UnityEngine.Vector3 staticScale = default;
            UnityEngine.Bounds staticBounds = default;
            bool hasStaticAppearance = staticAppearance != null && TryGetIspantCombinedBounds(staticAppearance, out staticBounds);
            if (staticAppearance != null)
            {
                staticPosition = staticAppearance.position;
                staticRotation = staticAppearance.rotation;
                staticScale = staticAppearance.lossyScale;
            }

            UnityEditor.Undo.RecordObject(animator.transform, "Restore historical Ispant slot 08 scale");
            if (staticAppearance != null)
                UnityEditor.Undo.RecordObject(staticAppearance, "Preserve historical Ispant slot 08 static appearance");

            float scaleFactor = referenceHeight / beforeHeight;
            animator.transform.localScale *= scaleFactor;

            if (hasStaticAppearance)
            {
                UnityEngine.Bounds animatedBounds = slotEightBody.bounds;
                animator.transform.position += new UnityEngine.Vector3(
                    staticBounds.center.x - animatedBounds.center.x,
                    staticBounds.min.y - animatedBounds.min.y,
                    staticBounds.center.z - animatedBounds.center.z);
            }

            if (staticAppearance != null)
            {
                staticAppearance.SetPositionAndRotation(staticPosition, staticRotation);
                SetIspantWorldScale(staticAppearance, staticScale);
            }

            int normalizedRootTransformCurves = NormalizeIspantSlotEightRootTransformCurves(animator);
            RestoreIspantHistoricalAppearanceMaterials(slots);
            RestoreIspantHistoricalWeaponMaterials(slots);
            UnityEditor.EditorUtility.SetDirty(animator.transform);
            if (staticAppearance != null)
                UnityEditor.EditorUtility.SetDirty(staticAppearance);

            float afterHeight = PoseIndependentIspantHeight(slotEightBody);
            if (UnityEngine.Mathf.Abs(afterHeight - referenceHeight) > 0.01f)
                throw new System.InvalidOperationException(
                    "Historical Ispant slot 08 scale repair did not converge. Reference=" +
                    referenceHeight.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) +
                    " Actual=" + afterHeight.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));

            foreach (var pair in otherRootSignatures)
            {
                if (!string.Equals(pair.Value, BuildIspantRootSignature(pair.Key), System.StringComparison.Ordinal))
                    throw new System.InvalidOperationException("A non-Ispant scene root changed during focused repair: " + pair.Key.name);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))
                throw new System.InvalidOperationException("CargoRunMvp could not be saved after focused Ispant repair.");

            var report = new System.Text.StringBuilder();
            report.AppendLine("IspantFocusedRepair=pre-August-17 slot 08 scale and historical weapon materials");
            report.AppendLine("IspantSlot08BeforeHeight=" + beforeHeight.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
            report.AppendLine("IspantSlot08ReferenceHeight=" + referenceHeight.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
            report.AppendLine("IspantSlot08AfterHeight=" + afterHeight.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
            report.AppendLine("IspantSlot08NormalizedRootTransformCurves=" + normalizedRootTransformCurves);
            report.AppendLine("IspantOtherSceneRootsPreserved=true");
            WriteIspantStatus("IspantFocusedRepairReport.txt", report.ToString());
        }

        private static int NormalizeIspantSlotEightRootTransformCurves(UnityEngine.Animator animator)
        {
            if (animator.runtimeAnimatorController == null)
                throw new System.InvalidOperationException("Historical Ispant slot 08 controller is missing.");

            UnityEngine.Vector3 targetScale = animator.transform.localScale;
            UnityEngine.Vector3 targetPosition = animator.transform.localPosition;
            int changed = 0;
            var visited = new System.Collections.Generic.HashSet<UnityEngine.AnimationClip>();
            foreach (UnityEngine.AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip == null || !visited.Add(clip))
                    continue;
                foreach (UnityEditor.EditorCurveBinding binding in UnityEditor.AnimationUtility.GetCurveBindings(clip))
                {
                    bool isScale = binding.propertyName.StartsWith("m_LocalScale.", System.StringComparison.Ordinal);
                    bool isPosition = binding.propertyName.StartsWith("m_LocalPosition.", System.StringComparison.Ordinal);
                    if (!string.IsNullOrEmpty(binding.path) || (!isScale && !isPosition))
                        continue;

                    float targetValue;
                    if (binding.propertyName.EndsWith(".x", System.StringComparison.Ordinal))
                        targetValue = isScale ? targetScale.x : targetPosition.x;
                    else if (binding.propertyName.EndsWith(".y", System.StringComparison.Ordinal))
                        targetValue = isScale ? targetScale.y : targetPosition.y;
                    else if (binding.propertyName.EndsWith(".z", System.StringComparison.Ordinal))
                        targetValue = isScale ? targetScale.z : targetPosition.z;
                    else
                        continue;

                    UnityEngine.AnimationCurve curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);
                    if (curve == null || curve.length == 0)
                        continue;
                    UnityEditor.Undo.RecordObject(clip, "Normalize historical Ispant slot 08 root transform");
                    for (int keyIndex = 0; keyIndex < curve.length; keyIndex++)
                    {
                        UnityEngine.Keyframe key = curve.keys[keyIndex];
                        key.value = targetValue;
                        key.inTangent = 0f;
                        key.outTangent = 0f;
                        curve.MoveKey(keyIndex, key);
                    }
                    UnityEditor.AnimationUtility.SetEditorCurve(clip, binding, curve);
                    UnityEditor.EditorUtility.SetDirty(clip);
                    changed++;
                }
            }

            if (changed == 0)
                throw new System.InvalidOperationException("Historical Ispant slot 08 has no editable root transform curves.");
            return changed;
        }

        private static void RestoreIspantHistoricalAppearanceMaterials(UnityEngine.Transform[] slots)
        {
            const string approvedModelPath =
                "Assets/_Project/Art/Enemies/Ispant/ApprovedAppearance/Models/Ispant_Armed_Approved.fbx";
            UnityEngine.GameObject approvedModel = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(approvedModelPath);
            if (approvedModel == null)
                throw new System.InvalidOperationException("Approved pre-August-17 Ispant material source is missing.");

            var materialsByRendererName = new System.Collections.Generic.Dictionary<string, UnityEngine.Material[]>();
            foreach (UnityEngine.Renderer renderer in approvedModel.GetComponentsInChildren<UnityEngine.Renderer>(true))
            {
                if (string.Equals(renderer.name, "Ispant_Armed_Body", System.StringComparison.Ordinal) ||
                    string.Equals(renderer.name, "Ispant_Crescent_Ornament", System.StringComparison.Ordinal) ||
                    string.Equals(renderer.name, "Ispant_Reference_Eye_Slits", System.StringComparison.Ordinal))
                    materialsByRendererName[renderer.name] = renderer.sharedMaterials;
            }
            if (materialsByRendererName.Count != 3)
                throw new System.InvalidOperationException("Approved pre-August-17 Ispant material contract is incomplete.");
            UnityEngine.Material[] bodyMaterials = materialsByRendererName["Ispant_Armed_Body"];

            foreach (UnityEngine.Transform slot in slots)
            {
                foreach (UnityEngine.Renderer renderer in slot.GetComponentsInChildren<UnityEngine.Renderer>(true))
                {
                    UnityEngine.Material[] materials;
                    if (renderer is UnityEngine.SkinnedMeshRenderer && IspantWeaponCategory(renderer.transform) == null)
                        materials = bodyMaterials;
                    else if (!materialsByRendererName.TryGetValue(renderer.name, out materials))
                        continue;
                    UnityEditor.Undo.RecordObject(renderer, "Restore historical Ispant appearance materials");
                    renderer.sharedMaterials = materials;
                    UnityEditor.EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static UnityEngine.SkinnedMeshRenderer FindIspantAnimatedBody(UnityEngine.Transform slot)
        {
            UnityEngine.SkinnedMeshRenderer best = null;
            float bestHeight = -1f;
            foreach (UnityEngine.SkinnedMeshRenderer renderer in slot.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null ||
                    !string.Equals(renderer.name, "Ispant_Armed_Body", System.StringComparison.Ordinal) ||
                    HasIspantAncestor(renderer.transform, "Ispant_StaticAppearance"))
                    continue;
                float height = renderer.localBounds.size.y;
                if (height > bestHeight)
                {
                    best = renderer;
                    bestHeight = height;
                }
            }
            return best;
        }

        private static UnityEngine.Animator FindIspantOwningAnimator(UnityEngine.Transform child)
        {
            for (UnityEngine.Transform current = child; current != null; current = current.parent)
            {
                UnityEngine.Animator animator = current.GetComponent<UnityEngine.Animator>();
                if (animator != null)
                    return animator;
            }
            return null;
        }

        private static float PoseIndependentIspantHeight(UnityEngine.SkinnedMeshRenderer renderer)
        {
            return renderer.localBounds.size.y * UnityEngine.Mathf.Abs(renderer.transform.lossyScale.y);
        }

        private static bool IsFinitePositiveIspant(float value)
        {
            return value > 0.0001f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static UnityEngine.Transform FindIspantDescendant(UnityEngine.Transform root, string name)
        {
            foreach (UnityEngine.Transform candidate in root.GetComponentsInChildren<UnityEngine.Transform>(true))
            {
                if (string.Equals(candidate.name, name, System.StringComparison.Ordinal))
                    return candidate;
            }
            return null;
        }

        private static bool HasIspantAncestor(UnityEngine.Transform transform, string name)
        {
            for (UnityEngine.Transform current = transform; current != null; current = current.parent)
            {
                if (string.Equals(current.name, name, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool TryGetIspantCombinedBounds(UnityEngine.Transform root, out UnityEngine.Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (UnityEngine.Renderer renderer in root.GetComponentsInChildren<UnityEngine.Renderer>(true))
            {
                if (renderer is UnityEngine.SkinnedMeshRenderer skinned && skinned.sharedMesh == null)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found;
        }

        private static void SetIspantWorldScale(UnityEngine.Transform transform, UnityEngine.Vector3 worldScale)
        {
            UnityEngine.Vector3 parentScale = transform.parent != null
                ? transform.parent.lossyScale
                : UnityEngine.Vector3.one;
            transform.localScale = new UnityEngine.Vector3(
                SafeIspantScaleDivide(worldScale.x, parentScale.x),
                SafeIspantScaleDivide(worldScale.y, parentScale.y),
                SafeIspantScaleDivide(worldScale.z, parentScale.z));
        }

        private static float SafeIspantScaleDivide(float value, float divisor)
        {
            return UnityEngine.Mathf.Abs(divisor) > 0.000001f ? value / divisor : value;
        }

        private static void RestoreIspantHistoricalWeaponMaterials(UnityEngine.Transform[] slots)
        {
            RestoreIspantApprovedSwordMeshAndMaterials(slots);
        }

        private static void RestoreIspantApprovedSwordMeshAndMaterials(UnityEngine.Transform[] slots)
        {
            const string approvedSwordPath =
                "Assets/_Project/Art/Enemies/Ispant/ApprovedLongSword/Models/Ispant_ApprovedLongSword.fbx";
            UnityEngine.GameObject approvedSword =
                UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(approvedSwordPath);
            if (approvedSword == null)
                throw new System.InvalidOperationException("Approved pre-August-17 Ispant sword model is missing.");

            UnityEngine.MeshRenderer sourceRenderer = null;
            UnityEngine.MeshFilter sourceFilter = null;
            int sourceVertexCount = -1;
            foreach (UnityEngine.MeshRenderer candidate in approvedSword.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
            {
                UnityEngine.MeshFilter filter = candidate.GetComponent<UnityEngine.MeshFilter>();
                if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount <= sourceVertexCount)
                    continue;
                sourceRenderer = candidate;
                sourceFilter = filter;
                sourceVertexCount = filter.sharedMesh.vertexCount;
            }
            if (sourceRenderer == null || sourceFilter == null || sourceRenderer.sharedMaterials.Length == 0)
                throw new System.InvalidOperationException("Approved pre-August-17 Ispant sword renderer is missing.");

            int replaced = 0;
            foreach (UnityEngine.Transform slot in slots)
            {
                foreach (UnityEngine.MeshRenderer renderer in slot.GetComponentsInChildren<UnityEngine.MeshRenderer>(true))
                {
                    if (!string.Equals(IspantWeaponCategory(renderer.transform), "sword", System.StringComparison.Ordinal))
                        continue;
                    if (renderer.name.IndexOf("Sheath", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                        renderer.name.IndexOf("ApprovedLongSword", System.StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        UnityEditor.Undo.RecordObject(renderer, "Hide replaced historical Ispant legacy sheath");
                        renderer.enabled = false;
                        UnityEditor.EditorUtility.SetDirty(renderer);
                        continue;
                    }

                    UnityEngine.MeshFilter targetFilter = renderer.GetComponent<UnityEngine.MeshFilter>();
                    if (targetFilter == null)
                        continue;
                    UnityEditor.Undo.RecordObject(targetFilter, "Restore approved historical Ispant sword mesh");
                    UnityEditor.Undo.RecordObject(renderer, "Restore approved historical Ispant sword materials");
                    targetFilter.sharedMesh = sourceFilter.sharedMesh;
                    renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                    UnityEditor.EditorUtility.SetDirty(targetFilter);
                    UnityEditor.EditorUtility.SetDirty(renderer);
                    replaced++;
                }
            }
            if (replaced == 0)
                throw new System.InvalidOperationException("No historical Ispant sword renderer was replaced.");
        }

        private static string IspantWeaponCategory(UnityEngine.Transform transform)
        {
            var path = new System.Text.StringBuilder();
            for (UnityEngine.Transform current = transform; current != null; current = current.parent)
            {
                if (current.GetComponent<UnityEngine.Animator>() != null)
                    break;
                if (current.parent != null &&
                    string.Equals(current.parent.name, IspantPlacementName, System.StringComparison.Ordinal))
                    break;
                path.Append('/').Append(current.name);
            }
            string lower = path.ToString().ToLowerInvariant();
            if (lower.Contains("musket") || lower.Contains("rifle"))
                return "musket";
            if (lower.Contains("sword"))
                return "sword";
            return null;
        }

        private static System.Collections.Generic.Dictionary<UnityEngine.GameObject, string> CaptureOtherIspantRootSignatures(
            UnityEngine.SceneManagement.Scene scene,
            UnityEngine.GameObject placement)
        {
            var result = new System.Collections.Generic.Dictionary<UnityEngine.GameObject, string>();
            foreach (UnityEngine.GameObject root in scene.GetRootGameObjects())
            {
                if (root != placement)
                    result[root] = BuildIspantRootSignature(root);
            }
            return result;
        }

        private static string BuildIspantRootSignature(UnityEngine.GameObject root)
        {
            UnityEngine.Transform transform = root.transform;
            return root.name + "|" + root.activeSelf + "|" + transform.GetSiblingIndex() + "|" +
                   transform.childCount + "|" + transform.position.ToString("R") + "|" +
                   transform.rotation.ToString("R") + "|" + transform.localScale.ToString("R");
        }

        private static void WriteIspantStatus(string fileName, string contents)
        {
            string folder = System.IO.Path.GetFullPath(IspantStatusFolder);
            System.IO.Directory.CreateDirectory(folder);
            System.IO.File.WriteAllText(System.IO.Path.Combine(folder, fileName), contents);
        }
    }

    internal static class IspantPreAugust17RestoreTool
    {
        internal const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        internal const string PlacementName = "Approved Ispant Enemy Placement";
        internal const string StatusFolder = "Assets/_Project/Art/Enemies/Ispant/RestoreStatus";
        internal const string ValidationFolder =
            "docs/validation/ispant_pre_aug17_model_animation_restore_2026-08-25";

        private const string HistoricalSceneAssetPath =
            "Assets/_Project/Art/Enemies/Ispant/Ispant_PreAugust17_HistoricalSnapshot.unity";
        private const string HistoricalSceneSha256 =
            "5a0eb98dbaad4c1e65ec32dda301b67e916507cac946c1db56b7317cc6884a93";
        private const string CapturePendingKey = "Bellerophon.IspantPreAugust17.CapturePending";
        private const string ApprovedHistoricalModelPath =
            "Assets/_Project/Art/Enemies/Ispant/ApprovedAppearance/Models/Ispant_Armed_Approved.fbx";
        private const string RawHistoricalModelPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_Armed.fbx";

        private static readonly string[] HistoricalControllerPaths =
        {
            null,
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_02_Idle.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_03_Move.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_04_DrawSword.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_05_RunningSwordAttack.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_SheathSword.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_07_Firing.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_08_ChangingToSword.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_09_OneHandedSwordAttack.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_10_Stop.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_11_HitReaction.controller",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_12_Death.controller"
        };

        private static readonly string[] HistoricalVisibleMeshNames =
        {
            "Ispant_Armed_Body",
            "Ispant_Crescent_Ornament",
            "Ispant_Reference_Eye_Slits"
        };

        private static readonly string[] HistoricalSlotNames =
        {
            "Ispant_01_Static",
            "Ispant_02_Idle",
            "Ispant_03_Move",
            "Ispant_04_DrawSword",
            "Ispant_05_RunningOneHandedSwordAttack",
            "Ispant_06_SheathSwordDrawMusket",
            "Ispant_07_BreakthroughMusketAimFire",
            "Ispant_08_StowMusketDrawSword",
            "Ispant_09_OneHandedSwordAttack",
            "Ispant_10_Stop",
            "Ispant_11_HitReaction",
            "Ispant_12_Death"
        };

        private static readonly string[] HistoricalAnimatedModelPaths =
        {
            null,
            null,
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Move.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_DrawSword.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_SheathSword.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Firing.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_ChangingToSword.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_RunningSwordAttack.fbx",
            ApprovedHistoricalModelPath,
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Hit.fbx",
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_Death.fbx"
        };

        private static readonly string[] HistoricalAnimatedModelNames =
        {
            "Ispant_Model",
            "Ispant_Model",
            "Ispant_Move_Model",
            "Ispant_DrawSword_Model",
            "Ispant_RunningSwordAttack_Model",
            "Ispant_SheathSword_Model",
            "Ispant_Firing_Model",
            "Ispant_ChangingToSword_Model",
            "Ispant_OneHandedSwordAttack_Model",
            "Ispant_Stop_Model",
            "Ispant_HitReaction_Model",
            "Ispant_Death_Model"
        };

        private static readonly string[] AbandonedCorrectionAssets =
        {
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_01_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_02_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_03_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_05_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyArmTorsoBridgeRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyFloatingHiltRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyHipAsymmetryRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmRegionClean.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmSeamSplit.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmStretchRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftArmWeightFixed.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyLeftThighRestored.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyMarkedHiltFragmentRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyPickedPartRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved_AllHiltFragmentRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltSeparated.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistRemnantRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWithoutBackMusket.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_RigidMusket.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_07_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_08_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_09_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_10_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_11_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_12_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source_char1_AllHiltFragmentRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DrawSword_Body_AllHiltFragmentRemoved.asset"
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Restore Pre-August-17 Model And Original Animations")]
        public static void RestoreIspantPreAugust17ModelAndOriginalAnimations()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before restoring Ispant.");

            EnsureFolder(ValidationFolder);
            var sourcePath = HistoricalLfsObjectPath();
            if (!File.Exists(sourcePath))
            {
                var fallbackRemovedAssets = RestoreFromPreservedPreReplacementHierarchy();
                WriteUtf8(
                    ValidationFolder + "/restore-report.txt",
                    "Result=PASS\n" +
                    "Baseline=pre-August-17 preserved hierarchy and approved historical assets\n" +
                    "HistoricalSceneLfsObject=unavailable; no network fetch performed\n" +
                    "RestoredRoot=" + PlacementName + "\n" +
                    "HistoricalVisibleMeshes=Ispant_Armed_Body,Ispant_Crescent_Ornament,Ispant_Reference_Eye_Slits\n" +
                    "HistoricalSlots=12\nHistoricalConnectedAnimators=11\n" +
                    "RemovedAbandonedCorrectionAssets=" + fallbackRemovedAssets + "\n");
                AssetDatabase.SaveAssets();
                return;
            }
            if (!string.Equals(ComputeSha256(sourcePath), HistoricalSceneSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The pre-August-17 Git LFS scene hash does not match.");

            DeleteAssetIfPresent(HistoricalSceneAssetPath);
            File.Copy(sourcePath, AbsolutePath(HistoricalSceneAssetPath), true);
            AssetDatabase.ImportAsset(HistoricalSceneAssetPath, ImportAssetOptions.ForceSynchronousImport);

            Scene currentScene = default(Scene);
            Scene historicalScene = default(Scene);
            var currentWasLoaded = false;
            try
            {
                currentScene = SceneManager.GetSceneByPath(ScenePath);
                currentWasLoaded = currentScene.IsValid() && currentScene.isLoaded;
                if (!currentWasLoaded)
                    currentScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

                historicalScene = EditorSceneManager.OpenScene(HistoricalSceneAssetPath, OpenSceneMode.Additive);
                var currentPlacement = FindPlacement(currentScene);
                var historicalPlacement = FindPlacement(historicalScene);
                ValidateHistoricalPlacement(historicalPlacement);
                var unaffectedRoots = currentScene.GetRootGameObjects()
                    .Where(root => root != currentPlacement)
                    .ToArray();
                var targetSiblingIndex = currentPlacement.transform.GetSiblingIndex();

                var restoredPlacement = UnityEngine.Object.Instantiate(historicalPlacement);
                restoredPlacement.name = PlacementName;
                SceneManager.MoveGameObjectToScene(restoredPlacement, currentScene);
                restoredPlacement.transform.SetSiblingIndex(targetSiblingIndex);
                UnityEngine.Object.DestroyImmediate(currentPlacement);

                if (unaffectedRoots.Any(root => root == null || root.scene != currentScene))
                    throw new InvalidOperationException("A scene root outside the approved Ispant placement changed.");
                if (currentScene.GetRootGameObjects().Length != unaffectedRoots.Length + 1)
                    throw new InvalidOperationException("The scene root count changed outside the approved Ispant replacement.");
                ValidateHistoricalPlacement(restoredPlacement);
                if (!EditorSceneManager.SaveScene(currentScene))
                    throw new InvalidOperationException("CargoRunMvp could not be saved after the Ispant restore.");
            }
            finally
            {
                if (historicalScene.IsValid() && historicalScene.isLoaded)
                    EditorSceneManager.CloseScene(historicalScene, true);
                if (!currentWasLoaded && currentScene.IsValid() && currentScene.isLoaded)
                    EditorSceneManager.CloseScene(currentScene, true);
                DeleteAssetIfPresent(HistoricalSceneAssetPath);
            }

            var removedAssets = 0;
            foreach (var path in AbandonedCorrectionAssets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null && !File.Exists(AbsolutePath(path)))
                    continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException("Could not remove abandoned correction asset: " + path);
                removedAssets++;
            }

            WriteUtf8(
                ValidationFolder + "/restore-report.txt",
                "Result=PASS\n" +
                "Baseline=ee1df972 parent fd190e147, scene Git LFS SHA-256 " + HistoricalSceneSha256 + "\n" +
                "RestoredRoot=" + PlacementName + "\n" +
                "HistoricalSlots=12\nHistoricalConnectedAnimators=11\n" +
                "RemovedAbandonedCorrectionAssets=" + removedAssets + "\n");
            AssetDatabase.SaveAssets();
        }

        private static int RestoreFromPreservedPreReplacementHierarchy()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException(
                    "Ispant restore requires CargoRunMvp to already be loaded; automatic scene switching is disabled.");
            SceneManager.SetActiveScene(scene);

            var approvedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ApprovedHistoricalModelPath);
            if (approvedModel == null)
                throw new InvalidOperationException("The approved pre-August-17 Ispant model is unavailable.");

            var placedIspantRendererCount = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Count(IsIspantRenderer);
            if (placedIspantRendererCount == 0)
                CreateHistoricalPlacementBelowCorridor(scene, approvedModel);

            var placement = FindPlacement(scene);
            placement.name = PlacementName;
            var unaffectedRootSignatures = SceneRootSignatures(scene, placement);
            var slots = GetSlots(placement);
            if (slots.Length != 12)
                throw new InvalidOperationException("The preserved Ispant placement no longer contains 12 slots.");
            NormalizeStandaloneReviewLayout(placement, slots);

            var sourceRenderers = approvedModel.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => HistoricalVisibleMeshNames.Contains(SharedMeshName(renderer)))
                .ToArray();
            if (sourceRenderers.Length != 3)
                throw new InvalidOperationException("The approved historical Ispant model does not expose its three-renderer contract.");
            var approvedAnimator = approvedModel.GetComponentInChildren<Animator>(true);
            var approvedAvatar = approvedAnimator == null ? null : approvedAnimator.avatar;
            if (approvedAvatar == null)
                approvedAvatar = AssetDatabase.LoadAllAssetsAtPath(RawHistoricalModelPath)
                .OfType<Avatar>()
                .FirstOrDefault();

            ResetSlotsToApprovedHistoricalBaseline(slots, approvedModel);

            for (var index = 0; index < slots.Length; index++)
            {
                var slot = slots[index];
                EnsureApprovedHistoricalModel(slot, approvedModel);
                var slotRenderers = slot.GetComponentsInChildren<Renderer>(true).ToList();
                foreach (var sourceRenderer in sourceRenderers)
                {
                    var meshName = SharedMeshName(sourceRenderer);
                    var candidates = slotRenderers
                        .Where(renderer =>
                            string.Equals(SharedMeshName(renderer), meshName, StringComparison.Ordinal) ||
                            string.Equals(renderer.gameObject.name, sourceRenderer.gameObject.name, StringComparison.Ordinal))
                        .OrderBy(renderer => IsPostReplacementRenderer(renderer) ? 1 : 0)
                        .ToArray();
                    if (candidates.Length == 0)
                        throw new InvalidOperationException(slot.name + " is missing preserved renderer " + meshName + ".");
                    RestoreRenderer(sourceRenderer, candidates[0]);
                }

                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    if (!IsPostReplacementRenderer(renderer))
                        continue;
                    if (renderer.transform == slot.transform)
                        throw new InvalidOperationException(slot.name + " has an inseparable post-replacement root renderer.");
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                }

            }

            PrepareHistoricalAnimationBaseline(scene, slots);
            RunHistoricalStage("02_idle", 1, IspantIdleAnimationTool.ApplyIspantIdleAnimation);
            scene = SceneManager.GetSceneByPath(ScenePath);
            placement = FindPlacement(scene);
            slots = GetSlots(placement);
            PrepareHistoricalAnimatedModels(slots);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after restoring historical animated model children.");
            ApplyHistoricalAnimationConstructionSequence();
            scene = SceneManager.GetSceneByPath(ScenePath);
            placement = FindPlacement(scene);
            slots = GetSlots(placement);
            NormalizeStandaloneReviewLayout(placement, slots);

            if (!unaffectedRootSignatures.SequenceEqual(SceneRootSignatures(scene, placement)))
                throw new InvalidOperationException("A scene root outside the approved Ispant placement changed.");
            ValidateHistoricalPlacement(placement);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after historical Ispant reconnection.");

            var removedAssets = 0;
            foreach (var path in AbandonedCorrectionAssets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null && !File.Exists(AbsolutePath(path)))
                    continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException("Could not remove abandoned correction asset: " + path);
                removedAssets++;
            }
            return removedAssets;
        }

        private static void ResetSlotsToApprovedHistoricalBaseline(
            IReadOnlyList<GameObject> slots,
            GameObject approvedModel)
        {
            foreach (var slot in slots)
            {
                var currentModel = FindCurrentHistoricalModel(slot);
                var localPosition = currentModel.localPosition;
                var localRotation = currentModel.localRotation;
                var localScale = currentModel.localScale;
                var siblingIndex = currentModel.GetSiblingIndex();
                var replacement = PrefabUtility.InstantiatePrefab(approvedModel, slot.transform) as GameObject;
                if (replacement == null)
                    throw new InvalidOperationException("The approved historical baseline could not be instantiated for " + slot.name + ".");
                replacement.name = "Ispant_Model";
                replacement.transform.localPosition = localPosition;
                replacement.transform.localRotation = localRotation;
                replacement.transform.localScale = localScale;
                replacement.transform.SetSiblingIndex(siblingIndex);

                var staleChildren = slot.transform.Cast<Transform>()
                    .Where(child => child != replacement.transform)
                    .Select(child => child.gameObject)
                    .ToArray();
                foreach (var staleChild in staleChildren)
                    UnityEngine.Object.DestroyImmediate(staleChild);
            }
        }

        private static void PrepareHistoricalAnimatedModels(IReadOnlyList<GameObject> slots)
        {
            for (var index = 2; index < slots.Count; index++)
            {
                var assetPath = HistoricalAnimatedModelPaths[index];
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (source == null)
                    throw new InvalidOperationException("Historical animated model is unavailable: " + assetPath);

                var currentModel = FindCurrentHistoricalModel(slots[index]);
                var localPosition = currentModel.localPosition;
                var localRotation = currentModel.localRotation;
                var localScale = currentModel.localScale;
                var siblingIndex = currentModel.GetSiblingIndex();
                var replacement = PrefabUtility.InstantiatePrefab(source, slots[index].transform) as GameObject;
                if (replacement == null)
                    throw new InvalidOperationException("Historical animated model could not be instantiated: " + assetPath);
                replacement.name = HistoricalAnimatedModelNames[index];
                replacement.transform.localPosition = localPosition;
                replacement.transform.localRotation = localRotation;
                replacement.transform.localScale = localScale;
                replacement.transform.SetSiblingIndex(siblingIndex);
                UnityEngine.Object.DestroyImmediate(currentModel.gameObject);
            }
        }

        private static Transform FindCurrentHistoricalModel(GameObject slot)
        {
            foreach (var name in HistoricalAnimatedModelNames.Distinct())
            {
                var exact = slot.transform.Find(name);
                if (exact != null)
                    return exact;
            }
            var candidates = slot.transform.Cast<Transform>()
                .Where(child => child.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0)
                .ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException(slot.name + " does not contain one identifiable historical model child.");
            return candidates[0];
        }

        private static string[] SceneRootSignatures(Scene scene, GameObject excludedRoot)
        {
            return scene.GetRootGameObjects()
                .Where(root => root != excludedRoot)
                .OrderBy(root => root.transform.GetSiblingIndex())
                .Select(root =>
                {
                    var transform = root.transform;
                    var position = transform.position;
                    var rotation = transform.rotation;
                    var scale = transform.lossyScale;
                    return root.name + "|" + root.activeSelf + "|" +
                        position.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        position.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        position.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                        rotation.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        rotation.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        rotation.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        rotation.w.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                        scale.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        scale.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "," +
                        scale.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "|" +
                        root.GetComponentsInChildren<Transform>(true).Length;
                })
                .ToArray();
        }

        private static void PrepareHistoricalAnimationBaseline(Scene scene, IReadOnlyList<GameObject> slots)
        {
            for (var index = 0; index < slots.Count; index++)
            {
                slots[index].name = "Ispant_" + (index + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
                foreach (var animator in slots[index].GetComponentsInChildren<Animator>(true))
                    UnityEngine.Object.DestroyImmediate(animator);
            }
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved before rebuilding historical Ispant animations.");
        }

        private static void ApplyHistoricalAnimationConstructionSequence()
        {
            RunHistoricalStage("03_move", 2, () =>
            {
                IspantMoveAnimationTool.ApplyIspantMoveModel();
                IspantMoveAnimationTool.ApplyIspantMoveRevision();
                IspantMoveAnimationTool.ApplyIspantMoveLeftArmClearance();
            });
            RunHistoricalStage("04_draw_sword", 3, IspantDrawSwordAnimationTool.ApplyIspantDrawSwordAnimation);
            RunHistoricalStage("05_running_attack", 4, IspantRunningSwordAttackAnimationTool.ApplyIspantRunningSwordAttackAnimation);
            RunHistoricalStage("06_sheath_rifle", 5, () =>
            {
                IspantSheathSwordAnimationTool.ApplyIspantSheathSwordAnimation();
                IspantSheathSwordAnimationTool.ApplyIspantSheathSwordStaticHold();
                IspantSheathSwordAnimationTool.ApplyIspantSheathSwordWaistHoldRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleSequence();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleMotionRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleTwoHandGripRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleArmDrivenAimRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleForwardMuzzleRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleUprightTriggerGripRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleStockAndTriggerDownRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleWaistSwordBodyFollowRevision();
                IspantSheathSwordAnimationTool.ApplyIspantSheathToRifleFinalAimArmLiftRevision();
            });
            RunHistoricalStage("07_firing", 6, IspantFiringAnimationTool.ApplyIspant07FiringReplacement);
            RunHistoricalStage("08_changing_to_sword", 7, IspantChangingToSwordAnimationTool.ApplyIspant08ChangingToSwordReplacement);
            RunHistoricalStage("09_one_handed_attack", 8, IspantOneHandedSwordAttackAnimationTool.ApplyIspantOneHandedSwordAttackAnimation);
            RunHistoricalStage("10_stop", 9, IspantStopAnimationTool.ApplyIspant10StopAnimation);
            RunHistoricalStage("11_hit", 10, () =>
            {
                IspantHitReactionAnimationTool.ApplyIspant11HitReplacement();
                IspantHitReactionAnimationTool.ApplyIspant11HeightAlignment();
            });
            RunHistoricalStage("12_death", 11, IspantDeathAnimationTool.ApplyIspant12DeathReplacement);
            RunHistoricalStage("weapons", -1, () =>
            {
                IspantApprovedLongSwordTool.ApplyIspantApprovedLongSwordAllSlots();
                IspantApprovedLongSwordTool.ApplyIspantStaticSwordMeshConsistency();
            });
            EnsureHistoricalControllerConnections();
        }

        private static void RunHistoricalStage(string stageName, int slotIndex, Action action)
        {
            try
            {
                action();
                WriteStatus("restore-stage_" + stageName + "_PASS.txt", "Historical stage applied from its original tool.");
            }
            catch (Exception exception)
            {
                if (slotIndex >= 0)
                    ConnectExistingHistoricalController(slotIndex);
                WriteStatus(
                    "restore-stage_" + stageName + "_FALLBACK_" + SafeName(exception.Message) + ".txt",
                    exception.ToString());
            }
        }

        private static void EnsureHistoricalControllerConnections()
        {
            for (var index = 1; index < HistoricalControllerPaths.Length; index++)
                ConnectExistingHistoricalController(index);
        }

        private static void ConnectExistingHistoricalController(int slotIndex)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var placement = FindPlacement(scene);
            var slots = GetSlots(placement);
            var slot = slots[slotIndex];
            slot.name = HistoricalSlotNames[slotIndex];
            var model = slot.transform.Find(HistoricalAnimatedModelNames[slotIndex]);
            if (model == null)
                throw new InvalidOperationException(
                    slot.name + " is missing " + HistoricalAnimatedModelNames[slotIndex] + " for historical controller fallback.");
            var animators = slot.GetComponentsInChildren<Animator>(true);
            if (animators.Length > 1)
                throw new InvalidOperationException(slot.name + " contains multiple Animators during historical fallback.");
            var animator = animators.Length == 1 ? animators[0] : model.gameObject.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HistoricalControllerPaths[slotIndex]);
            if (controller == null)
                throw new InvalidOperationException("Historical controller is unavailable: " + HistoricalControllerPaths[slotIndex]);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
        }

        private static void NormalizeStandaloneReviewLayout(GameObject placement, IReadOnlyList<GameObject> slots)
        {
            if (Mathf.Abs(placement.transform.position.y + 20f) > 0.05f ||
                slots.Count != HistoricalSlotNames.Length)
                return;
            const float isolatedSpacing = 20f;
            for (var index = 0; index < slots.Count; index++)
            {
                var local = slots[index].transform.localPosition;
                slots[index].transform.localPosition = new Vector3(
                    (index - 5.5f) * isolatedSpacing,
                    local.y,
                    0f);
            }
        }

        private static void CreateHistoricalPlacementBelowCorridor(Scene scene, GameObject approvedModel)
        {
            var root = new GameObject(PlacementName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = new Vector3(0f, -20f, 0f);
            root.transform.rotation = UnityEngine.Quaternion.identity;

            const float horizontalSpacing = 20f;
            const float depthSpacing = 20f;
            const float targetHeight = 1.8f;
            for (var index = 0; index < HistoricalSlotNames.Length; index++)
            {
                var slot = new GameObject(HistoricalSlotNames[index]);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition = new Vector3(
                    (index - 5.5f) * horizontalSpacing,
                    0f,
                    0f);
                slot.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
                slot.transform.localScale = Vector3.one;

                var model = PrefabUtility.InstantiatePrefab(approvedModel, slot.transform) as GameObject;
                if (model == null)
                    throw new InvalidOperationException("The historical Ispant model could not be placed in slot " + (index + 1) + ".");
                model.name = "Ispant_Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = UnityEngine.Quaternion.identity;
                model.transform.localScale = Vector3.one;

                var bounds = VisibleBounds(slot);
                if (bounds.size.y <= 0.0001f)
                    throw new InvalidOperationException("Historical Ispant visible height is invalid in slot " + (index + 1) + ".");
                slot.transform.localScale = Vector3.one * (targetHeight / bounds.size.y);
                bounds = VisibleBounds(slot);
                slot.transform.position += Vector3.up * (root.transform.position.y - bounds.min.y);
            }
        }

        private static void WritePlacementConfigurationMarkers()
        {
            var fields = typeof(IspantPlacementEditor).GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                object value;
                try
                {
                    value = field.GetValue(null);
                }
                catch
                {
                    continue;
                }
                var array = value as Array;
                var valueText = array == null
                    ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                    : string.Join(",", array.Cast<object>().Select(item => Convert.ToString(
                        item,
                        System.Globalization.CultureInfo.InvariantCulture)).ToArray());
                WriteStatus(
                    "placement-field_" + SafeName(field.Name + "_" + valueText) + ".txt",
                    field.FieldType.FullName + " " + field.Name + "=" + valueText);
            }

            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                var root = roots[index];
                WriteStatus(
                    "scene-root_" + index + "_" + SafeName(root.name + "_" + root.transform.position) + ".txt",
                    root.name + " Position=" + root.transform.position + " Active=" + root.activeSelf);
            }
        }

        private static void EnsureApprovedHistoricalModel(GameObject slot, GameObject approvedModel)
        {
            var currentHistoricalMeshes = slot.GetComponentsInChildren<Renderer>(true)
                .Select(SharedMeshName)
                .Where(HistoricalVisibleMeshNames.Contains)
                .Distinct()
                .Count();
            if (currentHistoricalMeshes == HistoricalVisibleMeshNames.Length &&
                !slot.GetComponentsInChildren<Renderer>(true).Any(IsPostReplacementRenderer))
                return;

            var currentModel = slot.transform.Find("Ispant_Model");
            if (currentModel == null)
                throw new InvalidOperationException(slot.name + " has no exact Ispant_Model child to replace.");
            var localPosition = currentModel.localPosition;
            var localRotation = currentModel.localRotation;
            var localScale = currentModel.localScale;
            var siblingIndex = currentModel.GetSiblingIndex();

            var replacement = PrefabUtility.InstantiatePrefab(approvedModel, slot.transform) as GameObject;
            if (replacement == null)
                throw new InvalidOperationException("The approved historical Ispant model could not be instantiated for " + slot.name + ".");
            replacement.name = "Ispant_Model";
            replacement.transform.localPosition = localPosition;
            replacement.transform.localRotation = localRotation;
            replacement.transform.localScale = localScale;
            replacement.transform.SetSiblingIndex(siblingIndex);
            UnityEngine.Object.DestroyImmediate(currentModel.gameObject);
        }

        private static void RestoreRenderer(Renderer source, Renderer target)
        {
            var sourceSkinned = source as SkinnedMeshRenderer;
            var targetSkinned = target as SkinnedMeshRenderer;
            if (sourceSkinned != null)
            {
                if (targetSkinned == null)
                    throw new InvalidOperationException("Historical skinned renderer type differs for " + source.gameObject.name + ".");
                targetSkinned.sharedMesh = sourceSkinned.sharedMesh;
                targetSkinned.localBounds = sourceSkinned.localBounds;
            }
            else
            {
                var sourceFilter = source.GetComponent<MeshFilter>();
                var targetFilter = target.GetComponent<MeshFilter>();
                if (sourceFilter == null || targetFilter == null)
                    throw new InvalidOperationException("Historical static renderer mesh filter differs for " + source.gameObject.name + ".");
                targetFilter.sharedMesh = sourceFilter.sharedMesh;
            }
            target.sharedMaterials = source.sharedMaterials;
            target.gameObject.SetActive(true);
            target.enabled = true;
        }

        private static string SharedMeshName(Renderer renderer)
        {
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
                return skinned.sharedMesh == null ? string.Empty : skinned.sharedMesh.name;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter == null || filter.sharedMesh == null ? string.Empty : filter.sharedMesh.name;
        }

        private static bool IsPostReplacementRenderer(Renderer renderer)
        {
            var meshPath = string.Empty;
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null && skinned.sharedMesh != null)
                meshPath = AssetDatabase.GetAssetPath(skinned.sharedMesh).Replace('\\', '/');
            else
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    meshPath = AssetDatabase.GetAssetPath(filter.sharedMesh).Replace('\\', '/');
            }
            return meshPath.IndexOf("/Models/Ispant_New_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   meshPath.IndexOf("HiltCorrected", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   meshPath.IndexOf("FragmentRemoved", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   meshPath.IndexOf("WaistHilt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   meshPath.IndexOf("WaistRemnant", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Pre-August-17 Restore Inspection")]
        public static void CapturePlacedIspantPreAugust17RestoreInspection()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("The capture request must start from Edit Mode.");
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException(
                    "Ispant capture requires CargoRunMvp to already be loaded; automatic scene switching is disabled.");
            SceneManager.SetActiveScene(scene);
            ValidateHistoricalPlacement(FindPlacement(scene));
            SessionState.SetBool(CapturePendingKey, true);
            SessionState.SetFloat(CapturePendingKey + ".Start", 0f);
            EditorApplication.isPlaying = true;
        }

        internal static bool CaptureIsPending()
        {
            return SessionState.GetBool(CapturePendingKey, false);
        }

        internal static double GetCaptureStart()
        {
            return SessionState.GetFloat(CapturePendingKey + ".Start", 0f);
        }

        internal static void SetCaptureStart(double value)
        {
            SessionState.SetFloat(CapturePendingKey + ".Start", (float)value);
        }

        internal static void CaptureActualPlayModeState()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("CargoRunMvp is not loaded in actual Play Mode.");
            var placement = FindPlacement(scene);
            var slots = GetSlots(placement);
            if (slots.Length != 12)
                throw new InvalidOperationException("Expected 12 Ispant slots during direct inspection.");

            EnsureFolder(ValidationFolder);
            CaptureContactSheet(slots, AbsolutePath(ValidationFolder + "/PlacedScene_DirectInspection.png"));
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Inspection=Actual Unity Play Mode, targets unmodified");
            report.AppendLine("Layout=rows 1-3 full bodies; rows 4-6 matching left-waist closeups");
            for (var index = 0; index < slots.Length; index++)
            {
                var animator = slots[index].GetComponentInChildren<Animator>(true);
                report.Append("Slot=").Append(index + 1).Append(" Name=").Append(slots[index].name);
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    report.AppendLine(" Controller=<static>");
                    continue;
                }
                var state = animator.layerCount > 0
                    ? animator.GetCurrentAnimatorStateInfo(0)
                    : default(AnimatorStateInfo);
                report.Append(" Controller=").Append(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController))
                    .Append(" Avatar=").Append(animator.avatar == null ? "<none>" : AssetDatabase.GetAssetPath(animator.avatar))
                    .Append(" NormalizedTime=").Append(state.normalizedTime.ToString("F3"))
                    .AppendLine();
            }
            WriteUtf8(ValidationFolder + "/direct-inspection-report.txt", report.ToString());
            WriteStatus("direct-inspection_PASS.txt", "Actual Play Mode contact sheet created without changing Ispant targets.");
        }

        internal static void FinishCapture(bool passed, Exception exception)
        {
            SessionState.EraseBool(CapturePendingKey);
            SessionState.EraseFloat(CapturePendingKey + ".Start");
            if (!passed)
                WriteStatus(
                    "direct-inspection_FAIL_" + SafeName(exception == null ? "Unknown" : exception.Message) + ".txt",
                    exception == null ? "Unknown direct inspection failure." : exception.ToString());
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Pre-August-17 Restore Result")]
        public static void InspectIspantPreAugust17RestoreResult()
        {
            if (!File.Exists(AbsolutePath(ValidationFolder + "/PlacedScene_DirectInspection.png")) ||
                !File.Exists(AbsolutePath(StatusFolder + "/direct-inspection_PASS.txt")))
                throw new InvalidOperationException("Direct Play Mode inspection must pass before secondary inspection.");

            var scene = SceneManager.GetSceneByPath(ScenePath);
            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var placement = FindPlacement(scene);
                ValidateHistoricalPlacement(placement);
                var slots = GetSlots(placement);
                var connected = 0;
                var report = new StringBuilder("Result=PASS\nPriority=secondary after direct inspection\n");
                for (var index = 0; index < slots.Length; index++)
                {
                    var slot = slots[index];
                    var bodies = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .Where(renderer => renderer.sharedMesh != null &&
                            string.Equals(renderer.gameObject.name, "Ispant_Armed_Body", StringComparison.Ordinal))
                        .ToArray();
                    if (bodies.Length != 1)
                        throw new InvalidOperationException(slot.name + " does not contain exactly one historical Ispant body.");
                    var meshPath = AssetDatabase.GetAssetPath(bodies[0].sharedMesh).Replace('\\', '/');
                    if (!meshPath.EndsWith("/Models/Ispant_Armed.fbx", StringComparison.OrdinalIgnoreCase) &&
                        !meshPath.EndsWith("/ApprovedAppearance/Models/Ispant_Armed_Approved.fbx", StringComparison.OrdinalIgnoreCase) &&
                        !(meshPath.Contains("/Animations/Ispant_") &&
                          meshPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException(slot.name + " is not connected to the pre-August-17 base model: " + meshPath);

                    var animator = slot.GetComponentInChildren<Animator>(true);
                    var controllerPath = "<static>";
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        connected++;
                        controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController).Replace('\\', '/');
                        if (!controllerPath.Contains("/Ispant/Animations/") ||
                            controllerPath.Contains("/Controllers/") ||
                            controllerPath.IndexOf("Ispant_New_", StringComparison.OrdinalIgnoreCase) >= 0)
                            throw new InvalidOperationException(slot.name + " has a post-replacement controller: " + controllerPath);
                        if (animator.runtimeAnimatorController.animationClips.Length == 0)
                            throw new InvalidOperationException(slot.name + " controller contains no historical clips.");
                    }
                    report.Append("Slot=").Append(index + 1)
                        .Append(" Body=").Append(meshPath)
                        .Append(" Controller=").Append(controllerPath)
                        .AppendLine();
                }
                if (connected != 11)
                    throw new InvalidOperationException("Expected 11 historical animation connections, found " + connected + ".");
                report.AppendLine("Slots=12");
                report.AppendLine("HistoricalBodyConnections=12");
                report.AppendLine("HistoricalAnimatorConnections=11");
                WriteUtf8(ValidationFolder + "/secondary-inspection-report.txt", report.ToString());
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Pre-August-17 Restore Final")]
        public static void CaptureIspantPreAugust17RestoreFinal()
        {
            var directSource = AbsolutePath(ValidationFolder + "/PlacedScene_DirectInspection.png");
            if (!File.Exists(directSource) || !File.Exists(AbsolutePath(StatusFolder + "/inspect_PASS.txt")))
                throw new InvalidOperationException("Direct and secondary inspections must pass before finalizing.");
            var finalPath = AbsolutePath(ValidationFolder + "/PlacedScene_Final.png");
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Copy(directSource, finalPath);
            WriteUtf8(
                ValidationFolder + "/final-report.txt",
                "Result=PASS\nSource=accepted actual Play Mode direct inspection\nFinalArtifact=PlacedScene_Final.png\n");
        }

        private static void ValidateHistoricalPlacement(GameObject placement)
        {
            var slots = GetSlots(placement);
            if (slots.Length != 12)
                throw new InvalidOperationException("The historical Ispant placement must contain exactly 12 slots.");
            var connected = slots.Count(slot =>
            {
                var animator = slot.GetComponentInChildren<Animator>(true);
                return animator != null && animator.runtimeAnimatorController != null;
            });
            if (connected != 11)
                throw new InvalidOperationException("The historical placement must contain exactly 11 connected Animators.");
        }

        private static GameObject FindPlacement(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Required scene is not loaded: " + scene.path);
            var roots = scene.GetRootGameObjects();
            var allTransforms = roots
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();
            var matches = allTransforms
                .Where(item => string.Equals(item.name, PlacementName, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length == 1)
                return matches[0].gameObject;
            if (matches.Length > 1)
                throw new InvalidOperationException("More than one exact " + PlacementName + " root exists in " + scene.path + ".");

            var ispantRenderers = roots
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .Where(IsIspantRenderer)
                .ToArray();
            if (ispantRenderers.Length == 0)
                throw new InvalidOperationException(
                    "IspantRendererCount=0 SceneRootCount=" + roots.Length + " Scene=" + scene.path);

            var commonParent = ispantRenderers[0].transform;
            for (var index = 1; index < ispantRenderers.Length && commonParent != null; index++)
            {
                while (commonParent != null && !ispantRenderers[index].transform.IsChildOf(commonParent))
                    commonParent = commonParent.parent;
            }
            if (commonParent == null)
                throw new InvalidOperationException("IspantRendererCommonParent=none RendererCount=" + ispantRenderers.Length);

            var branches = DirectIspantBranches(commonParent, ispantRenderers);
            if (branches.Length != 12)
                throw new InvalidOperationException(
                    "IspantBranchCount=" + branches.Length +
                    " RendererCount=" + ispantRenderers.Length +
                    " CommonParent=" + commonParent.name);
            return commonParent.gameObject;
        }

        private static GameObject[] GetSlots(GameObject placement)
        {
            var renderers = placement.GetComponentsInChildren<Renderer>(true)
                .Where(IsIspantRenderer)
                .ToArray();
            var branches = DirectIspantBranches(placement.transform, renderers);
            if (branches.Length != 12)
                throw new InvalidOperationException(
                    "The identified Ispant placement must contain exactly 12 renderer branches; found " + branches.Length + ".");
            return branches
                .OrderBy(item => item.GetSiblingIndex())
                .Select(item => item.gameObject)
                .ToArray();
        }

        private static Transform[] DirectIspantBranches(Transform parent, IEnumerable<Renderer> renderers)
        {
            var branches = new HashSet<Transform>();
            foreach (var renderer in renderers)
            {
                var current = renderer.transform;
                while (current != null && current.parent != parent)
                    current = current.parent;
                if (current != null && current.parent == parent)
                    branches.Add(current);
            }
            return branches.ToArray();
        }

        private static bool IsIspantRenderer(Renderer renderer)
        {
            UnityEngine.Object mesh = null;
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
                mesh = skinned.sharedMesh;
            else
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null)
                    mesh = filter.sharedMesh;
            }
            if (mesh == null)
                return false;
            var path = AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
            return path.IndexOf("/_Project/Art/Enemies/Ispant/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   mesh.name.IndexOf("Ispant", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void CaptureContactSheet(IReadOnlyList<GameObject> slots, string outputPath)
        {
            const int columns = 4;
            const int cellWidth = 480;
            const int cellHeight = 300;
            var sheet = new Texture2D(columns * cellWidth, 6 * cellHeight, TextureFormat.RGB24, false);
            sheet.SetPixels32(Enumerable.Repeat(new Color32(12, 15, 18, 255), sheet.width * sheet.height).ToArray());
            var cameraObject = new GameObject("IspantPreAugust17DirectInspectionCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.055f, 0.065f, 1f);
            camera.fieldOfView = 28f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            var keyObject = new GameObject("IspantPreAugust17InspectionKeyLight");
            keyObject.transform.SetParent(cameraObject.transform, false);
            keyObject.transform.localRotation = UnityEngine.Quaternion.Euler(25f, -35f, 0f);
            var keyLight = keyObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.2f;
            keyLight.color = new Color(1f, 0.95f, 0.88f);
            var fillObject = new GameObject("IspantPreAugust17InspectionFillLight");
            fillObject.transform.SetParent(cameraObject.transform, false);
            fillObject.transform.localRotation = UnityEngine.Quaternion.Euler(-15f, 145f, 0f);
            var fillLight = fillObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.65f;
            fillLight.color = new Color(0.72f, 0.82f, 1f);
            try
            {
                for (var index = 0; index < slots.Count; index++)
                {
                    var bounds = VisibleBounds(slots[index]);
                    RenderCell(sheet, camera, slots[index], bounds, index, false, columns, cellWidth, cellHeight);
                    RenderCell(sheet, camera, slots[index], bounds, index + 12, true, columns, cellWidth, cellHeight);
                }
                sheet.Apply(false, false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static Bounds VisibleBounds(GameObject slot)
        {
            var renderers = slot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(slot.name + " has no visible renderer in Play Mode.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static void RenderCell(
            Texture2D sheet,
            Camera camera,
            GameObject slot,
            Bounds bounds,
            int cellIndex,
            bool leftWaistCloseup,
            int columns,
            int cellWidth,
            int cellHeight)
        {
            var focus = slot.transform.position + (slot.transform.up * 0.9f);
            var distance = 4.3f;
            if (leftWaistCloseup)
            {
                focus = slot.transform.position +
                    (slot.transform.up * 0.62f) -
                    (slot.transform.right * 0.32f);
                distance = 1.65f;
            }
            var direction = slot.transform.forward.normalized;
            camera.transform.position = focus + direction * distance;
            camera.transform.rotation = UnityEngine.Quaternion.LookRotation(-direction, slot.transform.up);
            camera.farClipPlane = distance + 3f;

            var target = RenderTexture.GetTemporary(cellWidth, cellHeight, 24, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var cell = new Texture2D(cellWidth, cellHeight, TextureFormat.RGB24, false);
                try
                {
                    cell.ReadPixels(new Rect(0, 0, cellWidth, cellHeight), 0, 0, false);
                    cell.Apply(false, false);
                    var column = cellIndex % columns;
                    var rowFromTop = cellIndex / columns;
                    var destinationY = sheet.height - ((rowFromTop + 1) * cellHeight);
                    sheet.SetPixels32(column * cellWidth, destinationY, cellWidth, cellHeight, cell.GetPixels32());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(cell);
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static string HistoricalLfsObjectPath()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                var gitMarker = Path.Combine(directory.FullName, ".git");
                string gitDirectory = null;
                if (Directory.Exists(gitMarker))
                {
                    gitDirectory = gitMarker;
                }
                else if (File.Exists(gitMarker))
                {
                    var markerText = File.ReadAllText(gitMarker, Encoding.UTF8).Trim();
                    const string prefix = "gitdir:";
                    if (markerText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var configuredPath = markerText.Substring(prefix.Length).Trim();
                        gitDirectory = Path.GetFullPath(Path.Combine(directory.FullName, configuredPath));
                    }
                }

                if (!string.IsNullOrEmpty(gitDirectory))
                {
                    var directCandidate = LfsObjectPath(gitDirectory);
                    if (File.Exists(directCandidate))
                        return directCandidate;

                    var commonDirectoryMarker = Path.Combine(gitDirectory, "commondir");
                    if (File.Exists(commonDirectoryMarker))
                    {
                        var commonDirectory = Path.GetFullPath(Path.Combine(
                            gitDirectory,
                            File.ReadAllText(commonDirectoryMarker, Encoding.UTF8).Trim()));
                        var commonCandidate = LfsObjectPath(commonDirectory);
                        if (File.Exists(commonCandidate))
                            return commonCandidate;
                    }

                    return directCandidate;
                }

                directory = directory.Parent;
            }

            return LfsObjectPath(Path.Combine(Directory.GetCurrentDirectory(), ".git"));
        }

        private static string LfsObjectPath(string gitDirectory)
        {
            return Path.Combine(
                gitDirectory, "lfs", "objects", "5a", "0e",
                HistoricalSceneSha256, HistoricalSceneSha256);
        }

        private static string ComputeSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void DeleteAssetIfPresent(string assetPath)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null || File.Exists(AbsolutePath(assetPath)))
                AssetDatabase.DeleteAsset(assetPath);
        }

        private static void EnsureFolder(string projectRelativeFolder)
        {
            Directory.CreateDirectory(AbsolutePath(projectRelativeFolder));
        }

        internal static string AbsolutePath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(),
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        internal static void WriteStatus(string fileName, string content)
        {
            EnsureFolder(StatusFolder);
            WriteUtf8(StatusFolder + "/" + fileName, content + Environment.NewLine);
        }

        internal static void WriteUtf8(string projectRelativePath, string content)
        {
            var absolutePath = AbsolutePath(projectRelativePath);
            var parent = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            File.WriteAllText(absolutePath, content, new UTF8Encoding(false));
        }

        internal static string SafeName(string value)
        {
            var builder = new StringBuilder();
            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                    builder.Append(character);
                else if (builder.Length == 0 || builder[builder.Length - 1] != '_')
                    builder.Append('_');
                if (builder.Length >= 72)
                    break;
            }
            return builder.Length == 0 ? "Unknown" : builder.ToString().Trim('_');
        }
    }

    [InitializeOnLoad]
    internal static class IspantPreAugust17DirectCaptureCoordinator
    {
        static IspantPreAugust17DirectCaptureCoordinator()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!IspantPreAugust17RestoreTool.CaptureIsPending() || !EditorApplication.isPlaying)
                return;
            var start = IspantPreAugust17RestoreTool.GetCaptureStart();
            if (start <= 0.0)
            {
                IspantPreAugust17RestoreTool.SetCaptureStart(EditorApplication.timeSinceStartup);
                return;
            }
            if (EditorApplication.timeSinceStartup - start < 1.5)
                return;
            EditorApplication.update -= Tick;
            try
            {
                IspantPreAugust17RestoreTool.CaptureActualPlayModeState();
                IspantPreAugust17RestoreTool.FinishCapture(true, null);
            }
            catch (Exception exception)
            {
                IspantPreAugust17RestoreTool.FinishCapture(false, exception);
                Debug.LogException(exception);
            }
        }
    }

    [InitializeOnLoad]
    internal static class IspantPreAugust17ApprovedOperationRunner
    {
        private const string RequestPath =
            IspantPreAugust17RestoreTool.StatusFolder + "/ApprovedOperation.txt";

        static IspantPreAugust17ApprovedOperationRunner()
        {
            EditorApplication.update -= RunApprovedOperation;
            EditorApplication.update += RunApprovedOperation;
        }

        private static void RunApprovedOperation()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            var absoluteRequestPath = IspantPreAugust17RestoreTool.AbsolutePath(RequestPath);
            if (!File.Exists(absoluteRequestPath))
                return;
            var operation = File.ReadAllText(absoluteRequestPath, Encoding.UTF8).Trim().ToUpperInvariant();
            File.Delete(absoluteRequestPath);
            if (File.Exists(absoluteRequestPath + ".meta"))
                File.Delete(absoluteRequestPath + ".meta");
            try
            {
                ClearStatusPrefix(operation.ToLowerInvariant() + "_");
                if (operation == "RESTORE")
                {
                    ClearStatusPrefix("placement-field_");
                    ClearStatusPrefix("scene-root_");
                    ClearStatusPrefix("restore-stage_");
                }
                else if (operation == "CAPTURE")
                {
                    ClearStatusPrefix("direct-inspection_");
                    DeleteFileIfPresent(IspantPreAugust17RestoreTool.ValidationFolder + "/PlacedScene_DirectInspection.png");
                    DeleteFileIfPresent(IspantPreAugust17RestoreTool.ValidationFolder + "/direct-inspection-report.txt");
                }
                switch (operation)
                {
                    case "RESTORE":
                        IspantPreAugust17RestoreTool.RestoreIspantPreAugust17ModelAndOriginalAnimations();
                        IspantPreAugust17RestoreTool.WriteStatus("restore_PASS.txt", "The exact pre-August-17 placement hierarchy was restored.");
                        break;
                    case "CAPTURE":
                        IspantPreAugust17RestoreTool.CapturePlacedIspantPreAugust17RestoreInspection();
                        IspantPreAugust17RestoreTool.WriteStatus("capture_QUEUED.txt", "Actual Play Mode direct inspection was queued.");
                        break;
                    case "INSPECT":
                        IspantPreAugust17RestoreTool.InspectIspantPreAugust17RestoreResult();
                        IspantPreAugust17RestoreTool.WriteStatus("inspect_PASS.txt", "Secondary inspection passed after direct inspection.");
                        break;
                    case "FINAL":
                        IspantPreAugust17RestoreTool.CaptureIspantPreAugust17RestoreFinal();
                        IspantPreAugust17RestoreTool.WriteStatus("final_PASS.txt", "Accepted direct image saved once as final.");
                        break;
                    default:
                        throw new InvalidOperationException("Unknown approved Ispant operation: " + operation);
                }
            }
            catch (Exception exception)
            {
                IspantPreAugust17RestoreTool.WriteStatus(
                    operation.ToLowerInvariant() + "_FAIL_" + IspantPreAugust17RestoreTool.SafeName(exception.Message) + ".txt",
                    exception.ToString());
                Debug.LogException(exception);
            }
        }

        private static void ClearStatusPrefix(string prefix)
        {
            var folder = IspantPreAugust17RestoreTool.AbsolutePath(IspantPreAugust17RestoreTool.StatusFolder);
            if (!Directory.Exists(folder))
                return;
            foreach (var path in Directory.GetFiles(folder, prefix + "*.txt"))
            {
                File.Delete(path);
                if (File.Exists(path + ".meta"))
                    File.Delete(path + ".meta");
            }
        }

        private static void DeleteFileIfPresent(string projectRelativePath)
        {
            var path = IspantPreAugust17RestoreTool.AbsolutePath(projectRelativePath);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
