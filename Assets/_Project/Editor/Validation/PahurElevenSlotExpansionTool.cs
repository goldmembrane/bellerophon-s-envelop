using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.PahurCargoRunScene
{
    internal static class PahurElevenSlotExpansionTool
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Pahur Enemy Placement";
        private const string ModelName = "Pahur_Model";
        private const string ModelPath =
            "Assets/_Project/Art/Enemies/Pahur/Models/Pahur.fbx";
        private const string ReportPath =
            "docs/validation/pahur_eleven_slot_expansion_2026-08-01/Pahur_11_Slot_Expansion_Validation.txt";
        private const float FacingYaw = 180f;
        private const float Tolerance = 0.001f;
        private const int ExpectedTriangles = 4330;
        private const int ExpectedBones = 24;

        private static readonly string[] LegacySlotNames =
        {
            "Pahur_01_Static_Review",
            "Pahur_02_Idle",
            "Pahur_03_Move",
            "Pahur_04_MiniFlamethrower",
            "Pahur_05_BreakthroughFlamethrower",
            "Pahur_06_GuardianFlamethrower",
            "Pahur_07_Stop",
            "Pahur_08_FormationTransition",
            "Pahur_09_Hit",
            "Pahur_10_Death"
        };

        private static readonly string[] ExpandedSlotNames =
        {
            "Pahur_01_Static_Review",
            "Pahur_02_Idle",
            "Pahur_03_Move",
            "Pahur_04_MiniFlamethrower",
            "Pahur_05_BreakthroughFlamethrower",
            "Pahur_06_GuardianFlamethrower",
            "Pahur_07_Stop",
            "Pahur_08_ToGuardianStance",
            "Pahur_09_FromGuardianStance",
            "Pahur_10_Hit",
            "Pahur_11_Death"
        };

        [MenuItem("Bellerophon/Enemies/Pahur/Apply Eleven Slot Expansion")]
        public static void ApplyPahurElevenSlotExpansion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp contains pre-existing unsaved changes.");
            }

            var root = RequirePlacementRoot();
            var spacing = RequireLegacyContract(root.transform);
            var firstSevenBefore = FirstSevenSignatures(root.transform);
            var protectedBefore = ProtectedRootSignatures(scene);
            var rootTransformBefore = TransformSignature(root.transform);

            var toGuardian = root.transform.GetChild(7);
            var hit = root.transform.GetChild(8);
            var death = root.transform.GetChild(9);
            var toGuardianPose = new SlotPose(toGuardian);
            var hitPose = new SlotPose(hit);
            var deathPose = new SlotPose(death);
            var sourceModel = RequireModel(toGuardian);
            GameObject newSlot = null;

            try
            {
                newSlot = CreateCopiedSlot(scene, sourceModel);
                newSlot.transform.SetParent(root.transform, false);
                newSlot.transform.SetSiblingIndex(8);

                toGuardian.name = ExpandedSlotNames[7];
                newSlot.name = ExpandedSlotNames[8];
                hit.name = ExpandedSlotNames[9];
                death.name = ExpandedSlotNames[10];

                for (var index = 7; index < ExpandedSlotNames.Length; index++)
                {
                    var slot = root.transform.GetChild(index);
                    slot.localPosition = new Vector3(index * spacing, 0f, 0f);
                    slot.localRotation = Quaternion.Euler(0f, FacingYaw, 0f);
                    slot.localScale = Vector3.one;
                    EditorUtility.SetDirty(slot);
                }

                var metrics = InspectExpandedState(root.transform);
                RequireSameSignatures(
                    firstSevenBefore,
                    FirstSevenSignatures(root.transform),
                    "A Pahur slot from 01 through 07 changed during expansion.");
                RequireSameSignatures(
                    protectedBefore,
                    ProtectedRootSignatures(scene),
                    "A scene root outside the Pahur placement changed during expansion.");
                if (rootTransformBefore != TransformSignature(root.transform))
                {
                    throw new InvalidOperationException(
                        "The Pahur placement root transform changed during expansion.");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after expanding the Pahur lineup.");
                }

                if (scene.isDirty)
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp remained dirty after the Pahur lineup save.");
                }

                Debug.Log(
                    "PahurElevenSlotExpansionApplied Result=PASS" +
                    ", Slots=" + ExpandedSlotNames.Length +
                    ", XSpacing=" + Num(metrics.XSpacing) +
                    ", NewSlot=" + ExpandedSlotNames[8] +
                    ", NewSlotModelPath=" + ModelPath +
                    ", NewSlotAnimationActive=False" +
                    ", FirstSevenSlotsUnchanged=True" +
                    ", OtherSceneRootsUnchanged=True" +
                    ", SceneSaved=True.");
            }
            catch
            {
                if (newSlot != null)
                {
                    UnityEngine.Object.DestroyImmediate(newSlot);
                }

                toGuardian.name = LegacySlotNames[7];
                hit.name = LegacySlotNames[8];
                death.name = LegacySlotNames[9];
                toGuardianPose.Restore(toGuardian);
                hitPose.Restore(hit);
                deathPose.Restore(death);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                throw;
            }
        }

        [MenuItem("Bellerophon/Enemies/Pahur/Validate Eleven Slot Expansion")]
        public static void ValidatePahurElevenSlotExpansion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before Pahur slot validation.");
            }

            var root = RequirePlacementRoot();
            var protectedBefore = ProtectedRootSignatures(scene);
            var metrics = InspectExpandedState(root.transform);
            WriteReport(metrics);

            RequireSameSignatures(
                protectedBefore,
                ProtectedRootSignatures(scene),
                "A scene root outside the Pahur placement changed during validation.");
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "Pahur slot validation changed the scene dirty state.");
            }

            Debug.Log(
                "PahurElevenSlotExpansionValidated Result=PASS" +
                ", Slots=" + ExpandedSlotNames.Length +
                ", XSpacing=" + Num(metrics.XSpacing) +
                ", NewSlotTriangles=" + metrics.TriangleCount +
                ", NewSlotBones=" + metrics.BoneCount +
                ", NewSlotAnimationActive=False" +
                ", ExactOrder=True" +
                ", SceneChanged=False" +
                ", Report=" + ReportPath + ".");
        }

        private static GameObject CreateCopiedSlot(
            Scene scene,
            Transform sourceModel)
        {
            var sourcePrefab =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    sourceModel.gameObject) as GameObject ??
                throw new InvalidOperationException(
                    "Pahur_08 must remain linked to the approved Pahur FBX.");
            if (AssetDatabase.GetAssetPath(sourcePrefab) != ModelPath)
            {
                throw new InvalidOperationException(
                    "Pahur_08 does not use the approved Pahur FBX.");
            }

            var slot = new GameObject(ExpandedSlotNames[8]);
            try
            {
                var model =
                    PrefabUtility.InstantiatePrefab(sourcePrefab, scene)
                    as GameObject ??
                    throw new InvalidOperationException(
                        "The approved Pahur FBX could not be copied.");
                model.name = ModelName;
                model.transform.SetParent(slot.transform, false);
                model.transform.SetLocalPositionAndRotation(
                    sourceModel.localPosition,
                    sourceModel.localRotation);
                model.transform.localScale = sourceModel.localScale;

                CopyRendererMaterials(sourceModel, model.transform);
                foreach (var animator in
                         model.GetComponentsInChildren<Animator>(true))
                {
                    animator.enabled = false;
                    animator.runtimeAnimatorController = null;
                    animator.applyRootMotion = false;
                    EditorUtility.SetDirty(animator);
                }

                foreach (var animation in
                         model.GetComponentsInChildren<Animation>(true))
                {
                    animation.enabled = false;
                    EditorUtility.SetDirty(animation);
                }

                EditorUtility.SetDirty(slot);
                EditorUtility.SetDirty(model);
                return slot;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(slot);
                throw;
            }
        }

        private static void CopyRendererMaterials(
            Transform sourceModel,
            Transform targetModel)
        {
            var sourceRenderers =
                sourceModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .ToDictionary(
                        item => AnimationUtility.CalculateTransformPath(
                            item.transform,
                            sourceModel),
                        StringComparer.Ordinal);
            var targetRenderers =
                targetModel.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .ToDictionary(
                        item => AnimationUtility.CalculateTransformPath(
                            item.transform,
                            targetModel),
                        StringComparer.Ordinal);
            if (!sourceRenderers.Keys.OrderBy(item => item, StringComparer.Ordinal)
                    .SequenceEqual(
                        targetRenderers.Keys.OrderBy(item => item, StringComparer.Ordinal),
                        StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "The copied Pahur renderer hierarchy differs from Pahur_08.");
            }

            foreach (var pair in sourceRenderers)
            {
                targetRenderers[pair.Key].sharedMaterials =
                    pair.Value.sharedMaterials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(
                    targetRenderers[pair.Key]);
                EditorUtility.SetDirty(targetRenderers[pair.Key]);
            }
        }

        private static ExpansionMetrics InspectExpandedState(Transform root)
        {
            var spacing = RequireExpandedContract(root);
            var sourceModel = RequireModel(root.GetChild(7));
            var copiedModel = RequireModel(root.GetChild(8));

            var sourcePrefab =
                PrefabUtility.GetCorrespondingObjectFromSource(
                    copiedModel.gameObject);
            if (sourcePrefab == null ||
                AssetDatabase.GetAssetPath(sourcePrefab) != ModelPath)
            {
                throw new InvalidOperationException(
                    "Pahur_09 is not linked to the approved Pahur FBX.");
            }

            if (TransformSignature(sourceModel) !=
                TransformSignature(copiedModel))
            {
                throw new InvalidOperationException(
                    "Pahur_09 model transform differs from Pahur_08.");
            }

            var sourceRenderers =
                sourceModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var copiedRenderers =
                copiedModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (sourceRenderers.Length != 1 || copiedRenderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Pahur_08 and Pahur_09 must each contain one skinned renderer.");
            }

            var sourceRenderer = sourceRenderers[0];
            var copiedRenderer = copiedRenderers[0];
            if (copiedRenderer.sharedMesh == null ||
                AssetDatabase.GetAssetPath(copiedRenderer.sharedMesh) != ModelPath)
            {
                throw new InvalidOperationException(
                    "Pahur_09 does not use the approved Pahur mesh.");
            }

            var triangleCount = copiedRenderer.sharedMesh.triangles.Length / 3;
            if (triangleCount != ExpectedTriangles ||
                copiedRenderer.bones.Length != ExpectedBones)
            {
                throw new InvalidOperationException(
                    "Pahur_09 mesh or rig contract changed. Triangles=" +
                    triangleCount + ", Bones=" + copiedRenderer.bones.Length + ".");
            }

            var sourceMaterials = sourceRenderer.sharedMaterials
                .Select(AssetDatabase.GetAssetPath)
                .ToArray();
            var copiedMaterials = copiedRenderer.sharedMaterials
                .Select(AssetDatabase.GetAssetPath)
                .ToArray();
            if (!sourceMaterials.SequenceEqual(
                    copiedMaterials,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pahur_09 materials differ from Pahur_08.");
            }

            if (copiedModel.GetComponentsInChildren<Animator>(true)
                    .Any(item => item.enabled ||
                                 item.runtimeAnimatorController != null) ||
                copiedModel.GetComponentsInChildren<Animation>(true)
                    .Any(item => item.enabled))
            {
                throw new InvalidOperationException(
                    "Pahur_09 must not have active animation.");
            }

            return new ExpansionMetrics(
                spacing,
                triangleCount,
                copiedRenderer.bones.Length,
                copiedMaterials.Length,
                Vec(root.GetChild(7).localPosition),
                Vec(root.GetChild(8).localPosition),
                Vec(root.GetChild(9).localPosition),
                Vec(root.GetChild(10).localPosition));
        }

        private static float RequireLegacyContract(Transform root)
        {
            return RequireSlotContract(root, LegacySlotNames);
        }

        private static float RequireExpandedContract(Transform root)
        {
            return RequireSlotContract(root, ExpandedSlotNames);
        }

        private static float RequireSlotContract(
            Transform root,
            IReadOnlyList<string> names)
        {
            if (root.childCount != names.Count)
            {
                throw new InvalidOperationException(
                    "The Pahur placement slot count differs. Expected=" +
                    names.Count + ", Actual=" + root.childCount + ".");
            }

            var spacing = root.GetChild(1).localPosition.x -
                          root.GetChild(0).localPosition.x;
            if (spacing <= Tolerance)
            {
                throw new InvalidOperationException(
                    "The Pahur slot spacing is invalid.");
            }

            for (var index = 0; index < names.Count; index++)
            {
                var slot = root.GetChild(index);
                if (slot.name != names[index] ||
                    slot.childCount != 1 ||
                    Vector3.Distance(
                        slot.localPosition,
                        new Vector3(index * spacing, 0f, 0f)) > Tolerance ||
                    Quaternion.Angle(
                        slot.localRotation,
                        Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                    Vector3.Distance(slot.localScale, Vector3.one) > Tolerance)
                {
                    throw new InvalidOperationException(
                        "The Pahur slot contract differs at index " + index + ".");
                }

                RequireModel(slot);
            }

            return spacing;
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
            {
                throw new InvalidOperationException(
                    slot.name + " must contain exactly one Pahur_Model.");
            }

            return slot.GetChild(0);
        }

        private static GameObject RequirePlacementRoot()
        {
            return GameObject.Find(PlacementRootName) ??
                   throw new InvalidOperationException(
                       "The Pahur placement root is missing.");
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the current active scene. ActiveScene=" +
                    scene.path + ".");
            }

            return scene;
        }

        private static string[] FirstSevenSignatures(Transform root)
        {
            return Enumerable.Range(0, 7)
                .Select(index => HierarchyAndAssetSignature(root.GetChild(index)))
                .ToArray();
        }

        private static string HierarchyAndAssetSignature(Transform slot)
        {
            var builder = new StringBuilder();
            foreach (var item in slot.GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             item => RelativePath(slot, item),
                             StringComparer.Ordinal))
            {
                builder.Append(RelativePath(slot, item));
                builder.Append('|');
                builder.Append(TransformSignature(item));
                builder.Append(';');
            }

            foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
            {
                builder.Append(renderer.enabled);
                builder.Append('|');
                builder.Append(AssetDatabase.GetAssetPath(
                    (renderer as SkinnedMeshRenderer)?.sharedMesh));
                builder.Append('|');
                builder.Append(string.Join(
                    ",",
                    renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath)));
                builder.Append(';');
            }

            foreach (var animator in slot.GetComponentsInChildren<Animator>(true))
            {
                builder.Append(animator.enabled);
                builder.Append('|');
                builder.Append(animator.applyRootMotion);
                builder.Append('|');
                builder.Append(AssetDatabase.GetAssetPath(
                    animator.runtimeAnimatorController));
                builder.Append(';');
            }

            return builder.ToString();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .Select(item => HierarchyAndAssetSignature(item.transform))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string RelativePath(Transform root, Transform item)
        {
            return item == root
                ? root.name
                : root.name + "/" +
                  AnimationUtility.CalculateTransformPath(item, root);
        }

        private static void RequireSameSignatures(
            IReadOnlyList<string> before,
            IReadOnlyList<string> after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void WriteReport(ExpansionMetrics metrics)
        {
            var absolutePath = Absolute(ReportPath);
            var directory = Path.GetDirectoryName(absolutePath) ??
                            throw new InvalidOperationException(
                                "The Pahur expansion report directory is unavailable.");
            Directory.CreateDirectory(directory);
            File.WriteAllLines(
                absolutePath,
                new[]
                {
                    "Pahur Eleven Slot Expansion Validation",
                    "Result=PASS",
                    "Scene=" + ScenePath,
                    "PlacementRoot=" + PlacementRootName,
                    "SlotCount=" + ExpandedSlotNames.Length,
                    "SlotOrder=" + string.Join(",", ExpandedSlotNames),
                    "XSpacing=" + Num(metrics.XSpacing),
                    "Slot08Position=" + metrics.Slot08Position,
                    "Slot09Position=" + metrics.Slot09Position,
                    "Slot10Position=" + metrics.Slot10Position,
                    "Slot11Position=" + metrics.Slot11Position,
                    "NewSlotModelPath=" + ModelPath,
                    "NewSlotTriangles=" + metrics.TriangleCount,
                    "NewSlotBones=" + metrics.BoneCount,
                    "NewSlotMaterialCount=" + metrics.MaterialCount,
                    "NewSlotMatchesSlot08Materials=True",
                    "NewSlotAnimationActive=False",
                    "FirstSevenSlotsPreservedByApply=True",
                    "OtherSceneRootsPreservedByApply=True",
                    "SceneSaved=True",
                    "ValidationSceneChanged=False"
                },
                new UTF8Encoding(false));
        }

        private static string Absolute(string relativePath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Unity project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string TransformSignature(Transform item)
        {
            return Vec(item.localPosition) + "|" +
                   Quat(item.localRotation) + "|" +
                   Vec(item.localScale);
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + "," + Num(value.w);
        }

        private readonly struct SlotPose
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;

            public SlotPose(Transform slot)
            {
                localPosition = slot.localPosition;
                localRotation = slot.localRotation;
                localScale = slot.localScale;
            }

            public void Restore(Transform slot)
            {
                slot.localPosition = localPosition;
                slot.localRotation = localRotation;
                slot.localScale = localScale;
            }
        }

        private readonly struct ExpansionMetrics
        {
            public ExpansionMetrics(
                float xSpacing,
                int triangleCount,
                int boneCount,
                int materialCount,
                string slot08Position,
                string slot09Position,
                string slot10Position,
                string slot11Position)
            {
                XSpacing = xSpacing;
                TriangleCount = triangleCount;
                BoneCount = boneCount;
                MaterialCount = materialCount;
                Slot08Position = slot08Position;
                Slot09Position = slot09Position;
                Slot10Position = slot10Position;
                Slot11Position = slot11Position;
            }

            public float XSpacing { get; }
            public int TriangleCount { get; }
            public int BoneCount { get; }
            public int MaterialCount { get; }
            public string Slot08Position { get; }
            public string Slot09Position { get; }
            public string Slot10Position { get; }
            public string Slot11Position { get; }
        }
    }
}
