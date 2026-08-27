using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class PlayerSidestepPlayModeReview
    {
        private const string CapturedSessionKey = "Bellerophon.PlayerSidestepReview.Captured";
        private const string FramesDirectory = "Logs/PlayerSidestepPlayModeReviewFrames";
        private const string MetricsPath = "docs/validation/player_sidestep_mixamo_in_place_review_metrics.json";
        private const int ReviewLayer = 31;
        private const int FramesPerLoop = 30;
        private const int LoopCount = 2;

        [Serializable]
        private sealed class ReviewMetrics
        {
            public string target;
            public string state;
            public string sourceTake;
            public int framesCaptured;
            public int loopsCaptured;
            public float rootHorizontalDisplacementMax;
            public float hipsHorizontalDisplacementMax;
            public float leftFootHorizontalRange;
            public float rightFootHorizontalRange;
            public float leftHandHorizontalRange;
            public float rightHandHorizontalRange;
            public float leftForeArmMinimumLateralFromSpine;
            public float rightForeArmMinimumLateralFromSpine;
            public float leftHandMinimumLateralFromSpine;
            public float rightHandMinimumLateralFromSpine;
            public float loopPositionDifferenceMax;
            public float loopRotationDifferenceDegreesMax;
            public bool applyRootMotion;
            public bool passedNumericChecks;
            public string validationPriority;
        }

        private sealed class Pose
        {
            internal readonly Dictionary<string, Vector3> Positions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            internal readonly Dictionary<string, Quaternion> Rotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);
        }

        [MenuItem("Bellerophon/Player/Capture Sidestep Mixamo In Place Review")]
        internal static void CaptureReview()
        {
            if (!EditorApplication.isPlaying)
            {
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    throw new InvalidOperationException("The active scene has unsaved changes. Save or discard them before review.");
                }

                PlayerSidestepAnimationTool.OpenCargoRunScene();
                SessionState.SetBool(CapturedSessionKey, false);
                EditorApplication.EnterPlaymode();
                Debug.Log("[PlayerSidestep] Entering Play Mode for direct two-loop review.");
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
            Debug.Log("[PlayerSidestep] Exiting Play Mode after direct two-loop review.");
        }

        private static void CaptureTwoLoops()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject connectedTarget = PlayerSidestepAnimationTool.FindUniqueTarget(scene);
            Animator connectedAnimator = connectedTarget.GetComponent<Animator>();
            if (connectedAnimator == null || connectedAnimator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("Player_Sidestep does not have the approved Animator/controller connection.");
            }

            if (connectedAnimator.applyRootMotion)
            {
                throw new InvalidOperationException("Player_Sidestep Apply Root Motion must be disabled.");
            }

            GameObject reviewTarget = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D composite = null;
            try
            {
                reviewTarget = UnityEngine.Object.Instantiate(connectedTarget);
                reviewTarget.name = PlayerSidestepAnimationTool.TargetName + "_PlayModeReview";
                reviewTarget.transform.position = Vector3.zero;
                SetLayerRecursively(reviewTarget, ReviewLayer);

                Animator animator = reviewTarget.GetComponent<Animator>();
                if (animator == null)
                {
                    throw new InvalidOperationException("Review clone is missing the connected Animator.");
                }

                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.Rebind();
                animator.Update(0f);

                Transform hips = PlayerSidestepAnimationTool.FindHips(reviewTarget);
                Transform leftFoot = FindUniqueBone(reviewTarget, "LeftFoot");
                Transform rightFoot = FindUniqueBone(reviewTarget, "RightFoot");
                Transform spine = FindUniqueBone(reviewTarget, "Spine");
                Transform leftArm = FindUniqueBone(reviewTarget, "LeftArm");
                Transform rightArm = FindUniqueBone(reviewTarget, "RightArm");
                Transform leftForeArm = FindUniqueBone(reviewTarget, "LeftForeArm");
                Transform rightForeArm = FindUniqueBone(reviewTarget, "RightForeArm");
                Transform leftHand = FindUniqueBone(reviewTarget, "LeftHand");
                Transform rightHand = FindUniqueBone(reviewTarget, "RightHand");

                cameraObject = new GameObject("PlayerSidestepReviewCamera", typeof(Camera));
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.cullingMask = 1 << ReviewLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 1f);
                camera.allowHDR = false;
                camera.allowMSAA = true;
                camera.orthographic = true;

                lightObject = new GameObject("PlayerSidestepReviewLight", typeof(Light));
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
                int stateHash = Animator.StringToHash(PlayerSidestepAnimationTool.StateName);
                int totalFrames = FramesPerLoop * LoopCount + 1;

                Vector3 rootBaseline = Vector3.zero;
                Vector3 hipsBaseline = Vector3.zero;
                float rootHorizontalMax = 0f;
                float hipsHorizontalMax = 0f;
                List<Vector3> leftFootPositions = new List<Vector3>();
                List<Vector3> rightFootPositions = new List<Vector3>();
                List<Vector3> leftHandPositions = new List<Vector3>();
                List<Vector3> rightHandPositions = new List<Vector3>();
                List<float> leftForeArmLateralDistances = new List<float>();
                List<float> rightForeArmLateralDistances = new List<float>();
                List<float> leftHandLateralDistances = new List<float>();
                List<float> rightHandLateralDistances = new List<float>();
                Pose poseAtLoop0 = null;
                Pose poseAtLoop1 = null;
                Pose poseAtLoop2 = null;
                Vector3 lateral = reviewTarget.transform.right.normalized;

                for (int frame = 0; frame < totalFrames; frame++)
                {
                    float normalizedTime = frame / (float)FramesPerLoop;
                    animator.Play(stateHash, 0, normalizedTime);
                    animator.Update(0f);

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
                    float leftSideSign = Mathf.Sign(Vector3.Dot(leftArm.position - spine.position, lateral));
                    float rightSideSign = Mathf.Sign(Vector3.Dot(rightArm.position - spine.position, lateral));
                    leftForeArmLateralDistances.Add(
                        leftSideSign * Vector3.Dot(leftForeArm.position - spine.position, lateral));
                    rightForeArmLateralDistances.Add(
                        rightSideSign * Vector3.Dot(rightForeArm.position - spine.position, lateral));
                    leftHandLateralDistances.Add(
                        leftSideSign * Vector3.Dot(leftHand.position - spine.position, lateral));
                    rightHandLateralDistances.Add(
                        rightSideSign * Vector3.Dot(rightHand.position - spine.position, lateral));

                    Bounds bounds = PlayerSidestepAnimationTool.CalculateRendererBounds(reviewTarget);
                    RenderPanel(camera, renderTexture, composite, 0, reviewTarget.transform, bounds, 1.12f, bounds.center);

                    Vector3 armCenter = bounds.center;
                    armCenter.y = bounds.min.y + bounds.size.y * 0.61f;
                    RenderPanel(camera, renderTexture, composite, panelSize, reviewTarget.transform, bounds, 0.36f, armCenter);

                    composite.Apply(false, false);
                    string framePath = Path.Combine(absoluteFramesDirectory, $"frame_{frame:000}.png");
                    File.WriteAllBytes(framePath, composite.EncodeToPNG());
                }

                float loopPositionDifference = Math.Max(
                    PosePositionDifference(poseAtLoop0, poseAtLoop1),
                    PosePositionDifference(poseAtLoop0, poseAtLoop2));
                float loopRotationDifference = Math.Max(
                    PoseRotationDifference(poseAtLoop0, poseAtLoop1),
                    PoseRotationDifference(poseAtLoop0, poseAtLoop2));

                ReviewMetrics metrics = new ReviewMetrics
                {
                    target = PlayerSidestepAnimationTool.TargetName,
                    state = PlayerSidestepAnimationTool.StateName,
                    sourceTake = "mixamo.com",
                    framesCaptured = totalFrames,
                    loopsCaptured = LoopCount,
                    rootHorizontalDisplacementMax = rootHorizontalMax,
                    hipsHorizontalDisplacementMax = hipsHorizontalMax,
                    leftFootHorizontalRange = HorizontalRange(leftFootPositions),
                    rightFootHorizontalRange = HorizontalRange(rightFootPositions),
                    leftHandHorizontalRange = HorizontalRange(leftHandPositions),
                    rightHandHorizontalRange = HorizontalRange(rightHandPositions),
                    leftForeArmMinimumLateralFromSpine = leftForeArmLateralDistances.Min(),
                    rightForeArmMinimumLateralFromSpine = rightForeArmLateralDistances.Min(),
                    leftHandMinimumLateralFromSpine = leftHandLateralDistances.Min(),
                    rightHandMinimumLateralFromSpine = rightHandLateralDistances.Min(),
                    loopPositionDifferenceMax = loopPositionDifference,
                    loopRotationDifferenceDegreesMax = loopRotationDifference,
                    applyRootMotion = animator.applyRootMotion,
                    passedNumericChecks = rootHorizontalMax <= 0.00001f &&
                        hipsHorizontalMax <= 0.0001f &&
                        loopPositionDifference <= 0.0001f &&
                        loopRotationDifference <= 0.01f &&
                        !animator.applyRootMotion,
                    validationPriority = "1순위 직접 모델링·애니메이션 확인, 2순위 수치·스크립트 보조 검증"
                };

                string absoluteMetricsPath = Path.GetFullPath(MetricsPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteMetricsPath) ?? throw new InvalidOperationException());
                File.WriteAllText(absoluteMetricsPath, JsonUtility.ToJson(metrics, true), new UTF8Encoding(false));

                if (!metrics.passedNumericChecks)
                {
                    throw new InvalidOperationException(
                        $"Sidestep numeric review failed: root={rootHorizontalMax:R}, hips={hipsHorizontalMax:R}, " +
                        $"loopPosition={loopPositionDifference:R}, loopRotation={loopRotationDifference:R}.");
                }

                Debug.Log(
                    $"[PlayerSidestep] Captured {totalFrames} composite front/arm-close-up frames across two loops. " +
                    $"rootHorizontalMax={rootHorizontalMax:R}, hipsHorizontalMax={hipsHorizontalMax:R}, " +
                    $"loopPositionMax={loopPositionDifference:R}, loopRotationMax={loopRotationDifference:R}, " +
                    $"minimum forearm lateral clearance L/R={leftForeArmLateralDistances.Min():R}/" +
                    $"{rightForeArmLateralDistances.Min():R}.");
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
            composite.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), destinationX, 0, false);
        }

        private static Transform FindUniqueBone(GameObject root, string exactNameWithoutNamespace)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => string.Equals(StripNamespace(item.name), exactNameWithoutNamespace, StringComparison.OrdinalIgnoreCase))
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

        private static float HorizontalRange(IReadOnlyCollection<Vector3> positions)
        {
            if (positions.Count == 0)
            {
                return 0f;
            }

            float minX = positions.Min(value => value.x);
            float maxX = positions.Max(value => value.x);
            float minZ = positions.Min(value => value.z);
            float maxZ = positions.Max(value => value.z);
            return new Vector2(maxX - minX, maxZ - minZ).magnitude;
        }
    }
}
