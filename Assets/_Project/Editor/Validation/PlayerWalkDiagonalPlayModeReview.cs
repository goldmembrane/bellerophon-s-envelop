using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PlayerWalkDiagonalPlayModeReview
    {
        private const string CapturedSessionKey = "Bellerophon.PlayerWalkDiagonalForwardBlendReview.Captured";
        private const string FramesDirectory = "Logs/PlayerWalkDiagonalForwardBlendReviewFrames";
        private const string MetricsPath =
            "docs/validation/player_walk_diagonal_forward_blend_review_metrics.json";
        private const int ReviewLayer = 31;
        private const int FramesPerLoop = 30;
        private const int LoopCount = 2;

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string target;
            public string state;
            public string blendTree;
            public string blendParameter;
            public float blendValue;
            public string forwardMotion;
            public string sidestepMotion;
            public int framesCaptured;
            public int loopsCaptured;
            public float rootHorizontalDisplacementMax;
            public float hipsHorizontalDisplacementMax;
            public float leftFootForwardRange;
            public float rightFootForwardRange;
            public float leftFootLateralRange;
            public float rightFootLateralRange;
            public float leftHandForwardRange;
            public float rightHandForwardRange;
            public float leftHandLateralRange;
            public float rightHandLateralRange;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class Pose
        {
            internal readonly Dictionary<string, Vector3> Positions =
                new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations =
                new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        }

        [MenuItem("Bellerophon/Player/Capture Walk Diagonal Forward Blend Review")]
        internal static void CaptureReview()
        {
            if (!EditorApplication.isPlaying)
            {
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    throw new InvalidOperationException(
                        "The active scene has unsaved changes. Save or discard them before diagonal review.");
                }

                PlayerWalkDiagonalBlendTreeTool.OpenCargoRunScene();
                SessionState.SetBool(CapturedSessionKey, false);
                EditorApplication.EnterPlaymode();
                Debug.Log("[PlayerWalkDiagonal] Entering Play Mode for direct two-loop 50:50 Blend Tree review.");
                return;
            }

            if (!SessionState.GetBool(CapturedSessionKey, false))
            {
                CaptureTwoLoops();
                SessionState.SetBool(CapturedSessionKey, true);
                return;
            }

            SessionState.EraseBool(CapturedSessionKey);
            EditorApplication.ExitPlaymode();
            Debug.Log("[PlayerWalkDiagonal] Exiting Play Mode after direct two-loop Blend Tree review.");
        }

        private static void CaptureTwoLoops()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject connectedTarget = PlayerWalkDiagonalBlendTreeTool.FindUniqueTarget(scene);
            Animator connectedAnimator = connectedTarget.GetComponent<Animator>();
            AnimatorController controller = connectedAnimator != null
                ? connectedAnimator.runtimeAnimatorController as AnimatorController
                : null;
            if (connectedAnimator == null || controller == null)
            {
                throw new InvalidOperationException(
                    "Player_Walk_Diagonal does not have the approved AnimatorController connection.");
            }

            AnimationClip forwardClip = PlayerWalkDiagonalBlendTreeTool.RequireCurrentForwardClip();
            AnimationClip sidestepClip = PlayerWalkDiagonalBlendTreeTool.RequireCurrentSidestepClip();
            PlayerWalkDiagonalBlendTreeTool.AssertAnimatorConfiguration(
                connectedAnimator,
                controller,
                forwardClip,
                sidestepClip);

            GameObject reviewTarget = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D composite = null;
            try
            {
                reviewTarget = UnityEngine.Object.Instantiate(connectedTarget);
                reviewTarget.name = PlayerWalkDiagonalBlendTreeTool.TargetName + "_PlayModeReview";
                reviewTarget.transform.position = Vector3.zero;
                SetLayerRecursively(reviewTarget, ReviewLayer);

                Animator animator = reviewTarget.GetComponent<Animator>();
                if (animator == null)
                {
                    throw new InvalidOperationException("Diagonal review clone is missing the connected Animator.");
                }

                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.Rebind();
                animator.Update(0f);
                float actualBlendValue = animator.GetFloat(PlayerWalkDiagonalBlendTreeTool.BlendParameter);
                if (!Mathf.Approximately(actualBlendValue, PlayerWalkDiagonalBlendTreeTool.BlendValue))
                {
                    throw new InvalidOperationException(
                        $"Connected diagonal Blend Tree default must be 0.5; actual {actualBlendValue:R}.");
                }

                Transform hips = PlayerWalkDiagonalBlendTreeTool.FindHips(reviewTarget);
                Transform leftFoot = FindUniqueBone(reviewTarget, "LeftFoot");
                Transform rightFoot = FindUniqueBone(reviewTarget, "RightFoot");
                Transform leftHand = FindUniqueBone(reviewTarget, "LeftHand");
                Transform rightHand = FindUniqueBone(reviewTarget, "RightHand");

                cameraObject = new GameObject("PlayerWalkDiagonalReviewCamera", typeof(Camera));
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.cullingMask = 1 << ReviewLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 1f);
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.orthographic = true;

                lightObject = new GameObject("PlayerWalkDiagonalReviewLight", typeof(Light));
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.LookRotation(
                    -reviewTarget.transform.forward - reviewTarget.transform.up * 0.65f,
                    reviewTarget.transform.up);

                const int panelSize = 800;
                renderTexture = new RenderTexture(panelSize, panelSize, 24, RenderTextureFormat.ARGB32);
                renderTexture.Create();
                camera.targetTexture = renderTexture;
                composite = new Texture2D(panelSize * 2, panelSize, TextureFormat.RGB24, false);

                string absoluteFramesDirectory = Path.GetFullPath(FramesDirectory);
                Directory.CreateDirectory(absoluteFramesDirectory);
                int stateHash = Animator.StringToHash(PlayerWalkDiagonalBlendTreeTool.StateName);
                int totalFrames = FramesPerLoop * LoopCount + 1;

                Vector3 rootBaseline = Vector3.zero;
                Vector3 hipsBaseline = Vector3.zero;
                float rootHorizontalMax = 0f;
                float hipsHorizontalMax = 0f;
                List<Vector3> leftFootPositions = new List<Vector3>();
                List<Vector3> rightFootPositions = new List<Vector3>();
                List<Vector3> leftHandPositions = new List<Vector3>();
                List<Vector3> rightHandPositions = new List<Vector3>();
                Pose poseAtLoop0 = null;
                Pose poseAtLoop1 = null;
                Pose poseAtLoop2 = null;
                Vector3 forward = reviewTarget.transform.forward.normalized;
                Vector3 lateral = reviewTarget.transform.right.normalized;

                for (int frame = 0; frame < totalFrames; frame++)
                {
                    float normalizedTime = frame / (float)FramesPerLoop;
                    animator.Play(stateHash, 0, normalizedTime);
                    animator.Update(0f);

                    if (!Mathf.Approximately(
                        animator.GetFloat(PlayerWalkDiagonalBlendTreeTool.BlendParameter),
                        PlayerWalkDiagonalBlendTreeTool.BlendValue))
                    {
                        throw new InvalidOperationException("Diagonal Blend Tree value changed during direct review.");
                    }

                    if (frame == 0)
                    {
                        rootBaseline = reviewTarget.transform.position;
                        hipsBaseline = hips.position;
                        poseAtLoop0 = CapturePose(reviewTarget);
                    }
                    else if (frame == FramesPerLoop)
                    {
                        poseAtLoop1 = CapturePose(reviewTarget);
                    }
                    else if (frame == FramesPerLoop * LoopCount)
                    {
                        poseAtLoop2 = CapturePose(reviewTarget);
                    }

                    rootHorizontalMax = Mathf.Max(
                        rootHorizontalMax,
                        HorizontalDistance(reviewTarget.transform.position, rootBaseline));
                    hipsHorizontalMax = Mathf.Max(hipsHorizontalMax, HorizontalDistance(hips.position, hipsBaseline));
                    leftFootPositions.Add(leftFoot.position);
                    rightFootPositions.Add(rightFoot.position);
                    leftHandPositions.Add(leftHand.position);
                    rightHandPositions.Add(rightHand.position);

                    Bounds bounds = PlayerWalkDiagonalBlendTreeTool.CalculateRendererBounds(reviewTarget);
                    RenderPanel(camera, renderTexture, composite, 0, reviewTarget.transform, bounds, 0.62f, bounds.center);

                    Vector3 legCenter = bounds.center;
                    legCenter.y = bounds.min.y + bounds.size.y * 0.30f;
                    RenderPanel(camera, renderTexture, composite, panelSize, reviewTarget.transform, bounds, 0.32f, legCenter);

                    composite.Apply(false, false);
                    string framePath = Path.Combine(absoluteFramesDirectory, $"frame_{frame:000}.png");
                    File.WriteAllBytes(framePath, composite.EncodeToPNG());
                }

                float loopPositionDifference = Math.Max(
                    PosePositionDifference(poseAtLoop1, poseAtLoop2),
                    0f);
                float loopRotationDifference = Math.Max(
                    PoseRotationDifference(poseAtLoop1, poseAtLoop2),
                    0f);

                ReviewMetrics metrics = new ReviewMetrics
                {
                    target = PlayerWalkDiagonalBlendTreeTool.TargetName,
                    state = PlayerWalkDiagonalBlendTreeTool.StateName,
                    blendTree = PlayerWalkDiagonalBlendTreeTool.BlendTreeName,
                    blendParameter = PlayerWalkDiagonalBlendTreeTool.BlendParameter,
                    blendValue = actualBlendValue,
                    forwardMotion = AssetDatabase.GetAssetPath(forwardClip),
                    sidestepMotion = AssetDatabase.GetAssetPath(sidestepClip),
                    framesCaptured = totalFrames,
                    loopsCaptured = LoopCount,
                    rootHorizontalDisplacementMax = rootHorizontalMax,
                    hipsHorizontalDisplacementMax = hipsHorizontalMax,
                    leftFootForwardRange = ProjectedRange(leftFootPositions, forward),
                    rightFootForwardRange = ProjectedRange(rightFootPositions, forward),
                    leftFootLateralRange = ProjectedRange(leftFootPositions, lateral),
                    rightFootLateralRange = ProjectedRange(rightFootPositions, lateral),
                    leftHandForwardRange = ProjectedRange(leftHandPositions, forward),
                    rightHandForwardRange = ProjectedRange(rightHandPositions, forward),
                    leftHandLateralRange = ProjectedRange(leftHandPositions, lateral),
                    rightHandLateralRange = ProjectedRange(rightHandPositions, lateral),
                    loopPositionDifferenceMax = loopPositionDifference,
                    loopRotationDifferenceDegreesMax = loopRotationDifference,
                    applyRootMotion = animator.applyRootMotion,
                    passedNumericChecks = rootHorizontalMax <= 0.00001f &&
                        loopPositionDifference <= 0.0001f &&
                        loopRotationDifference <= 0.01f &&
                        !animator.applyRootMotion,
                    validationPriority =
                        "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };

                string absoluteMetricsPath = Path.GetFullPath(MetricsPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteMetricsPath) ??
                    throw new InvalidOperationException("Diagonal metrics directory is unavailable."));
                File.WriteAllText(absoluteMetricsPath, JsonUtility.ToJson(metrics, true), new UTF8Encoding(false));

                if (!metrics.passedNumericChecks)
                {
                    throw new InvalidOperationException(
                        $"Diagonal numeric review failed: root={rootHorizontalMax:R}, hips={hipsHorizontalMax:R}, " +
                        $"loopPosition={loopPositionDifference:R}, loopRotation={loopRotationDifference:R}.");
                }

                Debug.Log(
                    $"[PlayerWalkDiagonal] Captured {totalFrames} front/lower-body frames across two loops at " +
                    $"{PlayerWalkDiagonalBlendTreeTool.BlendParameter}={actualBlendValue:R}. " +
                    $"rootHorizontalMax={rootHorizontalMax:R}, hipsHorizontalMax={hipsHorizontalMax:R}, " +
                    $"loopPositionMax={loopPositionDifference:R}, loopRotationMax={loopRotationDifference:R}, " +
                    $"foot forward ranges L/R={metrics.leftFootForwardRange:R}/{metrics.rightFootForwardRange:R}, " +
                    $"foot lateral ranges L/R={metrics.leftFootLateralRange:R}/{metrics.rightFootLateralRange:R}.");
            }
            finally
            {
                RenderTexture.active = null;
                if (cameraObject != null)
                {
                    Camera camera = cameraObject.GetComponent<Camera>();
                    if (camera != null)
                    {
                        camera.targetTexture = null;
                    }
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.Destroy(renderTexture);
                }

                if (composite != null)
                {
                    UnityEngine.Object.Destroy(composite);
                }

                if (reviewTarget != null)
                {
                    UnityEngine.Object.Destroy(reviewTarget);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.Destroy(cameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.Destroy(lightObject);
                }
            }
        }

        private static void RenderPanel(
            Camera camera,
            RenderTexture renderTexture,
            Texture2D composite,
            int destinationX,
            Transform target,
            Bounds bounds,
            float verticalSizeFactor,
            Vector3 lookCenter)
        {
            Vector3 up = target.up.normalized;
            Vector3 forward = target.forward.normalized;
            camera.transform.rotation = Quaternion.LookRotation(-forward, up);
            camera.transform.position = lookCenter + forward * Math.Max(5f, bounds.size.magnitude * 2f);
            camera.orthographicSize = Math.Max(0.1f, bounds.size.y * verticalSizeFactor);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = Math.Max(20f, bounds.size.magnitude * 5f);
            camera.Render();
            RenderTexture.active = renderTexture;
            composite.ReadPixels(
                new Rect(0, 0, renderTexture.width, renderTexture.height),
                destinationX,
                0,
                false);
        }

        private static Transform FindUniqueBone(GameObject root, string exactNameWithoutNamespace)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(
                    StripNamespace(item.name), exactNameWithoutNamespace, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {exactNameWithoutNamespace} under {root.name}; found {matches.Length}.");
            }

            return matches[0];
        }

        private static string StripNamespace(string value)
        {
            int separator = value.LastIndexOf(':');
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                item.gameObject.layer = layer;
            }
        }

        private static Pose CapturePose(GameObject root)
        {
            Pose pose = new Pose();
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(item, root.transform);
                pose.Positions[path] = item.localPosition;
                pose.Rotations[path] = item.localRotation;
            }

            return pose;
        }

        private static float PosePositionDifference(Pose a, Pose b)
        {
            if (a == null || b == null || !a.Positions.Keys.ToHashSet().SetEquals(b.Positions.Keys))
            {
                return float.PositiveInfinity;
            }

            return a.Positions.Keys.Max(key => Vector3.Distance(a.Positions[key], b.Positions[key]));
        }

        private static float PoseRotationDifference(Pose a, Pose b)
        {
            if (a == null || b == null || !a.Rotations.Keys.ToHashSet().SetEquals(b.Rotations.Keys))
            {
                return float.PositiveInfinity;
            }

            return a.Rotations.Keys.Max(key => Quaternion.Angle(a.Rotations[key], b.Rotations[key]));
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector2 delta = new Vector2(a.x - b.x, a.z - b.z);
            return delta.magnitude;
        }

        private static float ProjectedRange(IReadOnlyCollection<Vector3> positions, Vector3 axis)
        {
            if (positions.Count == 0)
            {
                return 0f;
            }

            float[] projected = positions.Select(value => Vector3.Dot(value, axis)).ToArray();
            return projected.Max() - projected.Min();
        }
    }
}
