using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ShieldClearanceTools
    {
        private const string OutputFolder = "docs/validation/shield_state_motion_set_2026-09-07";
        private const string ShieldObjectName = "Shield_RightForeArm";
        private const float ExpectedDownwardDisplacementMeters = 0.05f;
        private const float ExpectedAdditionalForwardOffsetMeters = 0.01f;
        private const float ExpectedHandleOverlapMeters = 0.01f;
        private static readonly string[] TargetNames =
        {
            "Shield_Draw",
            "Shield_Idle",
            "Shield_Stow",
            "Shield_Raise",
            "Shield_Block_Idle",
            "Shield_Lower",
            "Shield_Block_Impact",
            "Shield_Break_Reaction"
        };

        public static void Inspect()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("StartShieldAnimationReviewLoop must be active before Shield clearance inspection.");

            Directory.CreateDirectory(Absolute(OutputFolder));
            Scene scene = SceneManager.GetActiveScene();
            var report = new StringBuilder("Read-only live Shield handle and forearm clearance inspection.\n");
            bool corrected = TargetNames.Select(name => FindUnique(scene, name))
                .All(target => target.GetComponent<ShieldRightArmClearance>() != null);
            float maximumOverlapError = 0f;
            float maximumRightOffset = float.NegativeInfinity;
            float maximumNonDrawRightOffset = float.NegativeInfinity;
            float maximumDownwardError = 0f;
            float maximumForwardOffsetError = 0f;
            float maximumLoweringRotationError = 0f;
            float minimumHeadForwardAlignment = 1f;
            float minimumHeadUpAlignment = 1f;
            foreach (string targetName in TargetNames)
            {
                GameObject target = FindUnique(scene, targetName);
                Transform forearm = RequireNamedTransform(target.transform, "RightForeArm");
                Transform hand = RequireNamedTransform(target.transform, "RightHand");
                Transform shield = forearm.Cast<Transform>().Single(item => item.name == ShieldObjectName);
                ShieldAnimationTools.GetShieldHandleBasis(shield, out Vector3 handleAnchor,
                    out Bounds handleBounds, out Vector3 faceAxis, out _);
                Vector3 outward = shield.TransformDirection(faceAxis).normalized;
                ProjectionInterval handle = ProjectBounds(shield, handleBounds, outward);
                ProjectionInterval arm = ProjectForearmSurface(target, forearm, outward);
                float overlap = IntervalOverlap(handle, arm);
                float boneFront = Mathf.Max(Vector3.Dot(forearm.position, outward), Vector3.Dot(hand.position, outward));
                float skinBeyondBoneFront = arm.Maximum - boneFront;
                float requiredOutwardShift = arm.Maximum - 0.02f - handle.Minimum;
                Transform spine = RequireNamedTransform(target.transform, "Spine");
                Vector3 forearmCenter = Vector3.Lerp(forearm.position, hand.position, 0.5f);
                float rightOffset = Vector3.Dot(forearmCenter - spine.position, target.transform.right);
                var armClearance = target.GetComponent<ShieldRightArmClearance>() ??
                    throw new InvalidOperationException(targetName + " has no ShieldRightArmClearance component.");
                var follower = shield.GetComponent<ShieldForearmFollower>() ??
                    throw new InvalidOperationException(targetName + " has no ShieldForearmFollower component.");
                var headForward = target.GetComponent<ShieldHeadForward>() ??
                    throw new InvalidOperationException(targetName + " has no ShieldHeadForward component.");
                float downwardDisplacement = armClearance.LastAppliedDownwardDisplacementMeters;
                float additionalForwardOffset = follower.AdditionalForwardOffsetMeters;
                maximumOverlapError = Mathf.Max(maximumOverlapError,
                    Mathf.Abs(overlap - ExpectedHandleOverlapMeters));
                maximumRightOffset = Mathf.Max(maximumRightOffset, rightOffset);
                if (targetName != "Shield_Draw")
                    maximumNonDrawRightOffset = Mathf.Max(maximumNonDrawRightOffset, rightOffset);
                maximumDownwardError = Mathf.Max(maximumDownwardError,
                    Mathf.Abs(downwardDisplacement - ExpectedDownwardDisplacementMeters));
                maximumForwardOffsetError = Mathf.Max(maximumForwardOffsetError,
                    Mathf.Abs(additionalForwardOffset - ExpectedAdditionalForwardOffsetMeters));
                maximumLoweringRotationError = Mathf.Max(maximumLoweringRotationError,
                    armClearance.LastLoweringRotationErrorDegrees);
                minimumHeadForwardAlignment = Mathf.Min(minimumHeadForwardAlignment,
                    headForward.LastForwardAlignment);
                minimumHeadUpAlignment = Mathf.Min(minimumHeadUpAlignment,
                    headForward.LastUpAlignment);
                report.AppendLine(targetName +
                    " handleForearmCenterDistanceMeters=" +
                    Vector3.Distance(shield.TransformPoint(handleAnchor), forearmCenter)
                        .ToString("F8", CultureInfo.InvariantCulture) +
                    " handleDepthMeters=" + handle.Size.ToString("F8", CultureInfo.InvariantCulture) +
                    " forearmDepthMeters=" + arm.Size.ToString("F8", CultureInfo.InvariantCulture) +
                    " projectedOverlapMeters=" + overlap.ToString("F8", CultureInfo.InvariantCulture) +
                    " skinBeyondBoneFrontMeters=" + skinBeyondBoneFront.ToString("F8", CultureInfo.InvariantCulture) +
                    " requiredOutwardShiftFor2cmMeters=" + requiredOutwardShift.ToString("F8", CultureInfo.InvariantCulture) +
                    " forearmCenterRightOfSpineMeters=" +
                    rightOffset.ToString("F8", CultureInfo.InvariantCulture) +
                    " handRightOfSpineMeters=" +
                    Vector3.Dot(hand.position - spine.position, target.transform.right).ToString("F8", CultureInfo.InvariantCulture) +
                    " appliedDownwardDisplacementMeters=" +
                    downwardDisplacement.ToString("F8", CultureInfo.InvariantCulture) +
                    " loweringRotationErrorDegrees=" +
                    armClearance.LastLoweringRotationErrorDegrees.ToString("F8", CultureInfo.InvariantCulture) +
                    " headForwardAlignment=" +
                    headForward.LastForwardAlignment.ToString("F8", CultureInfo.InvariantCulture) +
                    " headUpAlignment=" +
                    headForward.LastUpAlignment.ToString("F8", CultureInfo.InvariantCulture) +
                    " additionalForwardOffsetMeters=" +
                    additionalForwardOffset.ToString("F8", CultureInfo.InvariantCulture) +
                    " forearmVertexCount=" + arm.PointCount);
            }
            report.AppendLine("maximumHandleOverlapErrorFrom1cmMeters=" + maximumOverlapError.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("maximumForearmCenterRightOfSpineMeters=" + maximumRightOffset.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("maximumNonDrawForearmCenterRightOfSpineMeters=" + maximumNonDrawRightOffset.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("maximumDownwardDisplacementErrorFrom5cmMeters=" + maximumDownwardError.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("maximumAdditionalForwardOffsetErrorFrom1cmMeters=" + maximumForwardOffsetError.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("maximumLoweringRotationErrorDegrees=" + maximumLoweringRotationError.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("minimumHeadForwardAlignment=" + minimumHeadForwardAlignment.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("minimumHeadUpAlignment=" + minimumHeadUpAlignment.ToString("F8", CultureInfo.InvariantCulture));
            report.AppendLine("Inspection baked temporary meshes for measurement and did not change any target transform, pose, mesh, rig, skin weight or animation curve.");
            string outputName = corrected ? "runtime_clearance.txt" : "baseline_clearance.txt";
            File.WriteAllText(Absolute(OutputFolder + "/" + outputName), report.ToString(), Encoding.UTF8);
            if (corrected && maximumOverlapError > 0.005f)
                throw new InvalidOperationException("Shield handle overlap differs from the expected post-shift contact by more than 5 mm. Error=" + maximumOverlapError);
            if (corrected && maximumNonDrawRightOffset > 0.222f)
                throw new InvalidOperationException("Right forearm is still farther from the torso than the configured limit. Offset=" + maximumRightOffset);
            if (corrected && maximumDownwardError > 0.002f)
                throw new InvalidOperationException("Right forearm downward displacement differs from 5 cm by more than 2 mm. Error=" + maximumDownwardError);
            if (corrected && maximumForwardOffsetError > 0.0001f)
                throw new InvalidOperationException("Shield additional forward offset differs from 1 cm. Error=" + maximumForwardOffsetError);
            if (corrected && maximumLoweringRotationError > 0.001f)
                throw new InvalidOperationException("Rigid arm lowering changed a joint rotation. Error=" + maximumLoweringRotationError);
            if (corrected && (minimumHeadForwardAlignment < 0.9999f || minimumHeadUpAlignment < 0.9999f))
                throw new InvalidOperationException("Head is not aligned to transporter forward/up.");
            Debug.Log("Read-only Shield handle/forearm clearance inspection complete.");
        }

        private static ProjectionInterval ProjectForearmSurface(GameObject target, Transform forearm, Vector3 axis)
        {
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            int pointCount = 0;
            foreach (SkinnedMeshRenderer renderer in target.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh shared = renderer.sharedMesh;
                if (shared == null) continue;
                Transform[] bones = renderer.bones;
                int forearmBoneIndex = Array.FindIndex(bones, bone => bone == forearm);
                if (forearmBoneIndex < 0) continue;
                BoneWeight[] weights = shared.boneWeights;
                if (weights.Length != shared.vertexCount) continue;
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    Vector3[] vertices = baked.vertices;
                    for (int index = 0; index < vertices.Length; index++)
                    {
                        if (Influence(weights[index], forearmBoneIndex) < 0.15f) continue;
                        float projection = Vector3.Dot(renderer.transform.TransformPoint(vertices[index]), axis);
                        minimum = Mathf.Min(minimum, projection);
                        maximum = Mathf.Max(maximum, projection);
                        pointCount++;
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            if (pointCount == 0)
                throw new InvalidOperationException(target.name + " has no baked vertices influenced by RightForeArm.");
            return new ProjectionInterval(minimum, maximum, pointCount);
        }

        private static float Influence(BoneWeight weight, int boneIndex)
        {
            float result = 0f;
            if (weight.boneIndex0 == boneIndex) result += weight.weight0;
            if (weight.boneIndex1 == boneIndex) result += weight.weight1;
            if (weight.boneIndex2 == boneIndex) result += weight.weight2;
            if (weight.boneIndex3 == boneIndex) result += weight.weight3;
            return result;
        }

        private static ProjectionInterval ProjectBounds(Transform owner, Bounds bounds, Vector3 axis)
        {
            float minimum = float.PositiveInfinity;
            float maximum = float.NegativeInfinity;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
            {
                Vector3 local = new Vector3(x == 0 ? min.x : max.x, y == 0 ? min.y : max.y, z == 0 ? min.z : max.z);
                float projection = Vector3.Dot(owner.TransformPoint(local), axis);
                minimum = Mathf.Min(minimum, projection);
                maximum = Mathf.Max(maximum, projection);
            }
            return new ProjectionInterval(minimum, maximum, 8);
        }

        private static float IntervalOverlap(ProjectionInterval left, ProjectionInterval right) =>
            Mathf.Max(0f, Mathf.Min(left.Maximum, right.Maximum) - Mathf.Max(left.Minimum, right.Minimum));

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == name)
                .Select(item => item.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Expected one " + name + " in active scene, found " + matches.Length + ".");
            return matches[0];
        }

        private static Transform RequireNamedTransform(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " must contain one " + name + ". Found=" + matches.Length);
            return matches[0];
        }

        private static string Absolute(string relativePath) =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));

        private readonly struct ProjectionInterval
        {
            internal readonly float Minimum;
            internal readonly float Maximum;
            internal readonly int PointCount;
            internal float Size => Maximum - Minimum;

            internal ProjectionInterval(float minimum, float maximum, int pointCount)
            {
                Minimum = minimum;
                Maximum = maximum;
                PointCount = pointCount;
            }
        }
    }
}
