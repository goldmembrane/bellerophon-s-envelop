using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class VacuumCleanerRightHandFollowTools
    {
        internal const string OutputFolder =
            "docs/validation/vacuum_use_right_hand_follow_2026-09-11";
        internal const string DiagnosticImagePath = OutputFolder + "/diagnostic.png";
        internal const string FinalImagePath = OutputFolder + "/final.png";

        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Vacuum_Use";
        private const string IdleName = "Vacuum_Idle";
        private const string RightHandPath =
            "Armature/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand";
        private const string CleanerPath = "VacuumCleaner_Prop";
        private const string CleanerModelName = "VacuumCleaner_Model";
        private const string ControllerPath =
            "Assets/_Project/Art/Player/Animations/VacuumUse/VacuumUse_Locomotion.controller";
        private const float PositionTolerance = 0.000001f;
        private const float RotationTolerance = 0.0001f;

        internal static string DiagnosticAbsolutePath => Absolute(DiagnosticImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        internal static void InspectSource()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Transform cleaner = RequirePath(target.transform, CleanerPath);
            Transform model = RequireDescendant(cleaner, CleanerModelName);
            Vector3 positionOffset = hand.InverseTransformPoint(cleaner.position);
            Quaternion rotationOffset = Quaternion.Inverse(hand.rotation) * cleaner.rotation;
            var report = new StringBuilder()
                .AppendLine("Vacuum_Use right-hand follower source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("target=" + TargetName)
                .AppendLine("rightHandPath=" + RightHandPath)
                .AppendLine("cleanerPath=" + CleanerPath)
                .AppendLine("rightHandWorldPosition=" + Vec(hand.position))
                .AppendLine("rightHandWorldRotation=" + Quat(hand.rotation))
                .AppendLine("cleanerWorldPosition=" + Vec(cleaner.position))
                .AppendLine("cleanerWorldRotation=" + Quat(cleaner.rotation))
                .AppendLine("cleanerLocalPosition=" + Vec(cleaner.localPosition))
                .AppendLine("cleanerLocalRotation=" + Quat(cleaner.localRotation))
                .AppendLine("cleanerLocalScale=" + Vec(cleaner.localScale))
                .AppendLine("modelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("modelLocalScale=" + Vec(model.localScale))
                .AppendLine("positionOffsetInHandSpace=" + Vec(positionOffset))
                .AppendLine("rotationOffsetFromHand=" + Quat(rotationOffset))
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[VacuumRightHandFollow] Source inspected read-only. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleName);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Transform cleaner = RequirePath(target.transform, CleanerPath);
            Transform model = RequireDescendant(cleaner, CleanerModelName);
            string targetSignature = ProtectedHierarchySignature(target.transform);
            string idleSignature = ProtectedHierarchySignature(idle.transform);
            string controllerHash = ComputeAssetHash(ControllerPath);
            PoseState cleanerBefore = new PoseState(cleaner);
            PoseState modelBefore = new PoseState(model);

            VacuumCleanerRightHandFollowBehaviour follower =
                target.GetComponent<VacuumCleanerRightHandFollowBehaviour>() ??
                Undo.AddComponent<VacuumCleanerRightHandFollowBehaviour>(target);
            Undo.RecordObject(follower, "Configure Vacuum cleaner right-hand follow");
            follower.Configure(RightHandPath, CleanerPath, hand, cleaner);
            EditorUtility.SetDirty(follower);
            EditorSceneManager.MarkSceneDirty(scene);

            cleanerBefore.RequireExact(cleaner, "cleaner");
            modelBefore.RequireExact(model, "cleaner model");
            RequireStringEqual(targetSignature, ProtectedHierarchySignature(target.transform),
                "Vacuum_Use protected hierarchy");
            RequireStringEqual(idleSignature, ProtectedHierarchySignature(idle.transform),
                "Vacuum_Idle protected hierarchy");
            RequireStringEqual(controllerHash, ComputeAssetHash(ControllerPath),
                "Vacuum locomotion controller hash");

            MonoScript script = MonoScript.FromMonoBehaviour(follower);
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    script, out string scriptGuid, out long scriptLocalId))
                throw new InvalidOperationException("Follower script identity is unavailable.");
            GlobalObjectId componentId = GlobalObjectId.GetGlobalObjectIdSlow(follower);
            GlobalObjectId targetId = GlobalObjectId.GetGlobalObjectIdSlow(target);
            WriteText("baseline.txt",
                "targetSignature=" + ComputeStringHash(targetSignature) + Environment.NewLine +
                "idleSignature=" + ComputeStringHash(idleSignature) + Environment.NewLine +
                "controllerHash=" + controllerHash + Environment.NewLine);
            var report = new StringBuilder()
                .AppendLine("Vacuum_Use right-hand follower application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("scenePatchRequired=True")
                .AppendLine("currentWorldPosePreserved=True")
                .AppendLine("currentLocalScalePreserved=True")
                .AppendLine("vacuumIdleChanged=False")
                .AppendLine("upperBodyPoseChanged=False")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("locomotionControllerChanged=False")
                .AppendLine("targetGameObjectLocalId=" + targetId.targetObjectId)
                .AppendLine("componentLocalId=" + componentId.targetObjectId)
                .AppendLine("scriptGuid=" + scriptGuid)
                .AppendLine("scriptLocalId=" + scriptLocalId)
                .AppendLine("rightHandPath=" + follower.RightHandPath)
                .AppendLine("vacuumCleanerPath=" + follower.VacuumCleanerPath)
                .AppendLine("positionOffsetInHandSpace=" +
                    Vec(follower.PositionOffsetInHandSpace))
                .AppendLine("rotationOffsetFromHand=" +
                    Quat(follower.RotationOffsetFromHand))
                .AppendLine("preservedLocalScale=" + Vec(follower.PreservedLocalScale))
                .AppendLine("cleanerWorldPosition=" + Vec(cleaner.position))
                .AppendLine("cleanerWorldRotation=" + Quat(cleaner.rotation))
                .AppendLine("cleanerLocalPosition=" + Vec(cleaner.localPosition))
                .AppendLine("cleanerLocalRotation=" + Quat(cleaner.localRotation))
                .AppendLine("cleanerLocalScale=" + Vec(cleaner.localScale));
            WriteText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[VacuumRightHandFollow] Applied without changing the authored pose. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject idle = FindUnique(scene, IdleName);
            Transform cleaner = RequirePath(target.transform, CleanerPath);
            VacuumCleanerRightHandFollowBehaviour follower =
                target.GetComponent<VacuumCleanerRightHandFollowBehaviour>() ??
                throw new InvalidOperationException("Vacuum right-hand follower is missing.");
            if (follower.RightHandPath != RightHandPath ||
                follower.VacuumCleanerPath != CleanerPath)
                throw new InvalidOperationException("Vacuum right-hand follower paths changed.");
            if (Vector3.Distance(cleaner.position, follower.ExpectedWorldPosition()) >
                    PositionTolerance ||
                Quaternion.Angle(cleaner.rotation, follower.ExpectedWorldRotation()) >
                    RotationTolerance ||
                Vector3.Distance(cleaner.localScale, follower.PreservedLocalScale) >
                    PositionTolerance)
                throw new InvalidOperationException(
                    "Vacuum authored pose no longer matches the stored hand offset.");

            string[] baselineLines = File.ReadAllLines(
                Absolute(OutputFolder + "/baseline.txt"), Encoding.UTF8);
            string targetHash = BaselineValue(baselineLines, "targetSignature");
            string idleHash = BaselineValue(baselineLines, "idleSignature");
            string controllerHash = BaselineValue(baselineLines, "controllerHash");
            RequireStringEqual(targetHash,
                ComputeStringHash(ProtectedHierarchySignature(target.transform)),
                "Vacuum_Use protected hierarchy");
            RequireStringEqual(idleHash,
                ComputeStringHash(ProtectedHierarchySignature(idle.transform)),
                "Vacuum_Idle protected hierarchy");
            RequireStringEqual(controllerHash, ComputeAssetHash(ControllerPath),
                "Vacuum locomotion controller hash");

            var report = new StringBuilder()
                .AppendLine("Vacuum_Use right-hand follower inspection")
                .AppendLine("componentConfigured=True")
                .AppendLine("authoredWorldPosePreserved=True")
                .AppendLine("authoredLocalScalePreserved=True")
                .AppendLine("storedHandOffsetMatches=True")
                .AppendLine("vacuumIdleChanged=False")
                .AppendLine("upperBodyPoseChanged=False")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("locomotionControllerChanged=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[VacuumRightHandFollow] Inspection passed. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static RuntimeSample MeasureRuntimeSample()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Runtime follower measurement requires Play Mode.");
            GameObject target = FindUnique(RequireScene(), TargetName);
            Transform hand = RequirePath(target.transform, RightHandPath);
            Transform cleaner = RequirePath(target.transform, CleanerPath);
            VacuumCleanerRightHandFollowBehaviour follower =
                target.GetComponent<VacuumCleanerRightHandFollowBehaviour>() ??
                throw new InvalidOperationException("Runtime follower is missing.");
            return new RuntimeSample(
                hand.position,
                hand.rotation,
                cleaner.position,
                cleaner.rotation,
                Vector3.Distance(cleaner.position, follower.ExpectedWorldPosition()),
                Quaternion.Angle(cleaner.rotation, follower.ExpectedWorldRotation()),
                Vector3.Distance(cleaner.localScale, follower.PreservedLocalScale));
        }

        internal static Texture2D CaptureRuntimePanel()
        {
            return VacuumUseLocomotionTools.CaptureRuntimePanel();
        }

        internal static void ComposeRuntimeReview(
            System.Collections.Generic.IReadOnlyList<Texture2D> panels,
            string destination)
        {
            VacuumUseLocomotionTools.ComposeRuntimeReview(panels, destination);
        }

        internal static void WriteRuntimeReport(string fileName, string contents)
        {
            WriteText(fileName, contents);
        }

        private static string BaselineValue(string[] lines, string key)
        {
            string prefix = key + "=";
            string line = lines.SingleOrDefault(item => item.StartsWith(
                prefix, StringComparison.Ordinal));
            if (line == null)
                throw new InvalidOperationException("Follower baseline is missing " + key + ".");
            return line.Substring(prefix.Length);
        }

        private static string ProtectedHierarchySignature(Transform root)
        {
            var result = new StringBuilder();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(value => TransformPath(value, root), StringComparer.Ordinal))
            {
                string path = TransformPath(item, root);
                result.Append(path).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(item.GetSiblingIndex()).Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).AppendLine();
                foreach (MeshFilter filter in item.GetComponents<MeshFilter>())
                    result.Append("MF|").Append(path).Append('|')
                        .Append(AssetIdentity(filter.sharedMesh)).AppendLine();
                foreach (SkinnedMeshRenderer renderer in
                         item.GetComponents<SkinnedMeshRenderer>())
                    result.Append("SMR|").Append(path).Append('|').Append(renderer.enabled)
                        .Append('|').Append(AssetIdentity(renderer.sharedMesh)).Append('|')
                        .Append(string.Join(",", renderer.sharedMaterials.Select(AssetIdentity)))
                        .AppendLine();
                foreach (MeshRenderer renderer in item.GetComponents<MeshRenderer>())
                    result.Append("MR|").Append(path).Append('|').Append(renderer.enabled)
                        .Append('|')
                        .Append(string.Join(",", renderer.sharedMaterials.Select(AssetIdentity)))
                        .AppendLine();
            }
            return result.ToString();
        }

        private static string AssetIdentity(UnityEngine.Object asset)
        {
            return asset == null ? "null" : AssetDatabase.GetAssetPath(asset) + "#" + asset.name;
        }

        private static string ComputeAssetHash(string assetPath)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ComputeStringHash(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty);
        }

        private static void RequireStringEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Follower operation requires Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name).Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one " + name + "; found " +
                    matches.Length + ".");
            return matches[0];
        }

        private static Transform RequirePath(Transform root, string path)
        {
            return root.Find(path) ??
                throw new InvalidOperationException(root.name + " path is missing: " + path + ".");
        }

        private static Transform RequireDescendant(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + name + ".");
            return matches[0];
        }

        private static string TransformPath(Transform item, Transform root)
        {
            return AnimationUtility.CalculateTransformPath(item, root);
        }

        private static void WriteText(string name, string contents)
        {
            string directory = Absolute(OutputFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), contents, Encoding.UTF8);
        }

        private static string Absolute(string path)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(root,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);

        private readonly struct PoseState
        {
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly Vector3 worldPosition;
            private readonly Quaternion worldRotation;

            internal PoseState(Transform value)
            {
                localPosition = value.localPosition;
                localRotation = value.localRotation;
                localScale = value.localScale;
                worldPosition = value.position;
                worldRotation = value.rotation;
            }

            internal void RequireExact(Transform value, string label)
            {
                if (Vector3.Distance(localPosition, value.localPosition) > PositionTolerance ||
                    Quaternion.Angle(localRotation, value.localRotation) > RotationTolerance ||
                    Vector3.Distance(localScale, value.localScale) > PositionTolerance ||
                    Vector3.Distance(worldPosition, value.position) > PositionTolerance ||
                    Quaternion.Angle(worldRotation, value.rotation) > RotationTolerance)
                    throw new InvalidOperationException(label + " pose changed during configuration.");
            }
        }

        internal readonly struct RuntimeSample
        {
            internal RuntimeSample(
                Vector3 handPosition,
                Quaternion handRotation,
                Vector3 cleanerPosition,
                Quaternion cleanerRotation,
                float positionError,
                float rotationError,
                float scaleError)
            {
                HandPosition = handPosition;
                HandRotation = handRotation;
                CleanerPosition = cleanerPosition;
                CleanerRotation = cleanerRotation;
                PositionError = positionError;
                RotationError = rotationError;
                ScaleError = scaleError;
            }

            internal Vector3 HandPosition { get; }
            internal Quaternion HandRotation { get; }
            internal Vector3 CleanerPosition { get; }
            internal Quaternion CleanerRotation { get; }
            internal float PositionError { get; }
            internal float RotationError { get; }
            internal float ScaleError { get; }
        }
    }
}
