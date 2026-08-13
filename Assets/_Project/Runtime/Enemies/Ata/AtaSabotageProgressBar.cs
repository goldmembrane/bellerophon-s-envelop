using System.Collections.Generic;
using System.Linq;
using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellerophon.Enemies.Ata
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class AtaSabotageProgressBar : MonoBehaviour
    {
        // Approved art-sample dimensions and placement. Each placed bar receives its
        // authoritative gameplay duration; legacy sabotage instances fall back to 35 seconds.
        public const float WidthMeters = 0.62f;
        public const float HeightMeters = 0.055f;
        public const float HeadOffsetMeters = 0.18f;
        public static float CastDurationSeconds => SpacePirateRules.AtaSabotageCastSeconds;

        private const float InnerWidthMeters = 0.5365f;
        private const float InnerHeightMeters = 0.0259f;
        private const float InnerCornerRadiusMeters = 0.0065f;
        private const float StripePeriodMeters = 0.0291f;
        private const float StripeWidthMeters = 0.0146f;

        private static readonly Color32 OutlineColor = new(0x65, 0x71, 0x80, 0xff);
        private static readonly Color32 FrameColor = new(0x07, 0x0a, 0x0e, 0xff);
        private static readonly Color32 TrackColor = new(0x14, 0x1a, 0x21, 0xff);
        private static readonly Color32 FillStartColor = new(0x8f, 0x20, 0x1f, 0xff);
        private static readonly Color32 FillMiddleColor = new(0xd8, 0x45, 0x35, 0xff);
        private static readonly Color32 FillEndColor = new(0xff, 0x8a, 0x4c, 0xff);
        private static readonly Color32 HighlightColor = new(0xff, 0xd2, 0xa7, 0xe6);
        private static readonly Color32 RightBracketColor = new(0x7b, 0x87, 0x95, 0xff);
        private static readonly Color32 StripeColor = new(0xff, 0xff, 0xff, 0x10);
        private static readonly Color32 GlowColor = new(0xe6, 0x4c, 0x3d, 0x24);

        [SerializeField] private Transform headAnchor;
        [SerializeField, HideInInspector] private float headSurfaceOffsetMeters;
        [SerializeField, HideInInspector] private float castDurationSeconds;
        [SerializeField, HideInInspector] private bool restartOnCompletion;
        [SerializeField, HideInInspector] private float elapsedSeconds;

        private readonly List<Vector3> vertices = new();
        private readonly List<Color32> colors = new();
        private readonly List<int> triangles = new();
        private readonly List<Vector2> uvs = new();
        private Mesh runtimeMesh;
        private Material runtimeMaterial;

        public float DurationSeconds =>
            castDurationSeconds > 0f
                ? castDurationSeconds
                : CastDurationSeconds;
        public bool RestartOnCompletion => restartOnCompletion;
        public float NormalizedProgress =>
            DurationSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(elapsedSeconds / DurationSeconds);
        public float BarCenterOffsetMeters =>
            headSurfaceOffsetMeters + HeadOffsetMeters + HeightMeters * 0.5f;

        private void Awake()
        {
            EnsureResources();
            RebuildMesh();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                elapsedSeconds = 0f;
            }

            EnsureResources();
            ApplyPose(Camera.main);
            RebuildMesh();
        }

        private void Update()
        {
            if (!Application.isPlaying || DurationSeconds <= 0f)
            {
                return;
            }

            if (restartOnCompletion)
            {
                elapsedSeconds = Mathf.Repeat(
                    elapsedSeconds + Time.deltaTime,
                    DurationSeconds);
            }
            else
            {
                if (elapsedSeconds >= DurationSeconds)
                {
                    return;
                }

                elapsedSeconds = Mathf.Min(
                    DurationSeconds,
                    elapsedSeconds + Time.deltaTime);
            }

            RebuildMesh();
        }

        private void LateUpdate()
        {
            ApplyPose(Camera.main);
        }

        private void OnValidate()
        {
            elapsedSeconds = Mathf.Clamp(elapsedSeconds, 0f, DurationSeconds);
            headSurfaceOffsetMeters = Mathf.Max(0f, headSurfaceOffsetMeters);
            EnsureResources();
            ApplyPose(Camera.main);
            RebuildMesh();
        }

        private void OnDestroy()
        {
            DestroyRuntimeObject(runtimeMesh);
            DestroyRuntimeObject(runtimeMaterial);
        }

        public void Configure(Transform targetHeadAnchor, float visibleHeadTopWorldY)
        {
            Configure(targetHeadAnchor, visibleHeadTopWorldY, CastDurationSeconds);
        }

        public void Configure(
            Transform targetHeadAnchor,
            float visibleHeadTopWorldY,
            float authoritativeDurationSeconds)
        {
            Configure(
                targetHeadAnchor,
                visibleHeadTopWorldY,
                authoritativeDurationSeconds,
                false);
        }

        public void Configure(
            Transform targetHeadAnchor,
            float visibleHeadTopWorldY,
            float authoritativeDurationSeconds,
            bool shouldRestartOnCompletion)
        {
            headAnchor = targetHeadAnchor;
            headSurfaceOffsetMeters = Mathf.Max(
                0f,
                visibleHeadTopWorldY - targetHeadAnchor.position.y);
            castDurationSeconds = Mathf.Max(0f, authoritativeDurationSeconds);
            restartOnCompletion = shouldRestartOnCompletion;
            elapsedSeconds = 0f;
            EnsureResources();
            ApplyPose(Camera.main);
            RebuildMesh();
        }

        public void SetRestartOnCompletion(bool shouldRestartOnCompletion)
        {
            restartOnCompletion = shouldRestartOnCompletion;
            elapsedSeconds = 0f;
            EnsureResources();
            RebuildMesh();
        }

        public void SetProgressForReview(float progress01)
        {
            elapsedSeconds = Mathf.Clamp01(progress01) * DurationSeconds;
            EnsureResources();
            RebuildMesh();
        }

        public void RefreshForCamera(Camera targetCamera)
        {
            EnsureResources();
            ApplyPose(targetCamera);
            RebuildMesh();
        }

        private void EnsureResources()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();
            if (runtimeMesh == null)
            {
                runtimeMesh = new Mesh
                {
                    name = "Ata Sabotage Progress Bar Runtime Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (runtimeMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    return;
                }

                runtimeMaterial = new Material(shader)
                {
                    name = "Ata Sabotage Progress Bar Runtime Material",
                    hideFlags = HideFlags.HideAndDontSave,
                    mainTexture = Texture2D.whiteTexture
                };
                runtimeMaterial.SetInt("_Cull", (int)CullMode.Off);
            }

            meshFilter.sharedMesh = runtimeMesh;
            meshRenderer.sharedMaterial = runtimeMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.allowOcclusionWhenDynamic = false;
            meshRenderer.sortingOrder = 32000;
        }

        private void ApplyPose(Camera targetCamera)
        {
            if (headAnchor == null)
            {
                return;
            }

            transform.position = headAnchor.position + Vector3.up * BarCenterOffsetMeters;
            var compensatedScale = CompensateParentScale(transform.parent);
            transform.localScale = new Vector3(
                -compensatedScale.x,
                compensatedScale.y,
                compensatedScale.z);
            if (targetCamera != null)
            {
                var cameraDirection = targetCamera.transform.position - transform.position;
                if (cameraDirection.sqrMagnitude > 0.000001f)
                {
                    transform.rotation = Quaternion.LookRotation(cameraDirection, Vector3.up);
                }
            }
        }

        private void RebuildMesh()
        {
            if (runtimeMesh == null)
            {
                return;
            }

            vertices.Clear();
            colors.Clear();
            triangles.Clear();
            uvs.Clear();

            var halfWidth = WidthMeters * 0.5f;
            var halfHeight = HeightMeters * 0.5f;
            var shoulder = halfWidth * (270f / 298f);
            AddPolygon(new[]
            {
                new Vector2(-halfWidth, 0f),
                new Vector2(-shoulder, halfHeight),
                new Vector2(shoulder, halfHeight),
                new Vector2(halfWidth, 0f),
                new Vector2(shoulder, -halfHeight),
                new Vector2(-shoulder, -halfHeight)
            }, 0f, OutlineColor);

            var insetX = WidthMeters * (8f / 298f);
            var insetY = HeightMeters * (6f / 34f);
            AddPolygon(new[]
            {
                new Vector2(-halfWidth + insetX, 0f),
                new Vector2(-shoulder + insetX, halfHeight - insetY),
                new Vector2(shoulder - insetX, halfHeight - insetY),
                new Vector2(halfWidth - insetX, 0f),
                new Vector2(shoulder - insetX, -halfHeight + insetY),
                new Vector2(-shoulder + insetX, -halfHeight + insetY)
            }, 0.0001f, FrameColor);

            var innerHalfWidth = InnerWidthMeters * 0.5f;
            var innerHalfHeight = InnerHeightMeters * 0.5f;
            var progress = NormalizedProgress;
            var fillWidth = InnerWidthMeters * progress;
            var fillMinimum = -innerHalfWidth;
            var fillMaximum = fillMinimum + fillWidth;

            if (fillWidth > 0.0001f)
            {
                AddRoundedRect(
                    fillMinimum - 0.006f,
                    fillMaximum + 0.006f,
                    -innerHalfHeight - 0.004f,
                    innerHalfHeight + 0.004f,
                    InnerCornerRadiusMeters,
                    0.0002f,
                    GlowColor,
                    GlowColor,
                    false);
            }

            AddRoundedRect(
                -innerHalfWidth,
                innerHalfWidth,
                -innerHalfHeight,
                innerHalfHeight,
                InnerCornerRadiusMeters,
                0.0003f,
                TrackColor,
                TrackColor,
                false);

            if (fillWidth > 0.0001f)
            {
                AddRoundedRect(
                    fillMinimum,
                    fillMaximum,
                    -innerHalfHeight,
                    innerHalfHeight,
                    Mathf.Min(InnerCornerRadiusMeters, fillWidth * 0.25f),
                    0.0004f,
                    FillStartColor,
                    FillEndColor,
                    true);
                AddStripes(fillMinimum, fillMaximum, innerHalfHeight);

                var highlightWidth = Mathf.Min(WidthMeters * (5f / 298f), fillWidth);
                AddQuad(
                    new Vector2(fillMaximum - highlightWidth, -innerHalfHeight),
                    new Vector2(fillMaximum - highlightWidth, innerHalfHeight),
                    new Vector2(fillMaximum, innerHalfHeight),
                    new Vector2(fillMaximum, -innerHalfHeight),
                    0.0006f,
                    HighlightColor,
                    HighlightColor,
                    HighlightColor,
                    HighlightColor);
            }

            var bracketWidth = WidthMeters * (8f / 298f);
            var bracketHalfHeight = HeightMeters * (6f / 34f);
            AddPolygon(new[]
            {
                new Vector2(-halfWidth + bracketWidth * 0.75f, 0f),
                new Vector2(-halfWidth + bracketWidth * 1.75f, bracketHalfHeight),
                new Vector2(-halfWidth + bracketWidth * 1.75f, -bracketHalfHeight)
            }, 0.0007f, FillMiddleColor);
            AddPolygon(new[]
            {
                new Vector2(halfWidth - bracketWidth * 0.75f, 0f),
                new Vector2(halfWidth - bracketWidth * 1.75f, -bracketHalfHeight),
                new Vector2(halfWidth - bracketWidth * 1.75f, bracketHalfHeight)
            }, 0.0007f, RightBracketColor);

            runtimeMesh.Clear();
            runtimeMesh.SetVertices(vertices);
            runtimeMesh.SetColors(colors);
            runtimeMesh.SetUVs(0, uvs);
            runtimeMesh.SetTriangles(triangles, 0, true);
            runtimeMesh.RecalculateBounds();
        }

        private void AddStripes(float minimumX, float maximumX, float halfHeight)
        {
            for (var center = minimumX + StripePeriodMeters * 0.5f;
                 center < maximumX;
                 center += StripePeriodMeters)
            {
                var bottomMinimum = Mathf.Max(minimumX, center - StripeWidthMeters * 0.5f);
                var bottomMaximum = Mathf.Min(maximumX, center + StripeWidthMeters * 0.5f);
                var skew = StripeWidthMeters * 0.35f;
                var topMinimum = Mathf.Clamp(bottomMinimum - skew, minimumX, maximumX);
                var topMaximum = Mathf.Clamp(bottomMaximum - skew, minimumX, maximumX);
                if (bottomMaximum <= bottomMinimum || topMaximum <= topMinimum)
                {
                    continue;
                }

                AddQuad(
                    new Vector2(bottomMinimum, -halfHeight),
                    new Vector2(topMinimum, halfHeight),
                    new Vector2(topMaximum, halfHeight),
                    new Vector2(bottomMaximum, -halfHeight),
                    0.0005f,
                    StripeColor,
                    StripeColor,
                    StripeColor,
                    StripeColor);
            }
        }

        private void AddRoundedRect(
            float minimumX,
            float maximumX,
            float minimumY,
            float maximumY,
            float radius,
            float z,
            Color32 leftColor,
            Color32 rightColor,
            bool useApprovedGradient)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(
                (maximumX - minimumX) * 0.5f,
                (maximumY - minimumY) * 0.5f));
            var points = new List<Vector2>(20);
            AddCorner(points, minimumX + radius, maximumY - radius, radius, 180f, 90f);
            AddCorner(points, maximumX - radius, maximumY - radius, radius, 90f, 0f);
            AddCorner(points, maximumX - radius, minimumY + radius, radius, 0f, -90f);
            AddCorner(points, minimumX + radius, minimumY + radius, radius, -90f, -180f);
            AddPolygon(points, z, leftColor, rightColor, useApprovedGradient);
        }

        private static void AddCorner(
            ICollection<Vector2> points,
            float centerX,
            float centerY,
            float radius,
            float startDegrees,
            float endDegrees)
        {
            const int segments = 4;
            for (var index = 0; index <= segments; index++)
            {
                var angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)segments) *
                            Mathf.Deg2Rad;
                points.Add(new Vector2(
                    centerX + Mathf.Cos(angle) * radius,
                    centerY + Mathf.Sin(angle) * radius));
            }
        }

        private void AddPolygon(IReadOnlyList<Vector2> points, float z, Color32 color) =>
            AddPolygon(points, z, color, color, false);

        private void AddPolygon(
            IReadOnlyList<Vector2> points,
            float z,
            Color32 leftColor,
            Color32 rightColor,
            bool useApprovedGradient)
        {
            if (points.Count < 3)
            {
                return;
            }

            var start = vertices.Count;
            var minimumX = points.Min(point => point.x);
            var maximumX = points.Max(point => point.x);
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index];
                vertices.Add(new Vector3(point.x, point.y, z));
                var ratio = maximumX <= minimumX
                    ? 0f
                    : Mathf.InverseLerp(minimumX, maximumX, point.x);
                colors.Add(useApprovedGradient
                    ? EvaluateApprovedFill(ratio)
                    : Color32.Lerp(leftColor, rightColor, ratio));
                uvs.Add(Vector2.zero);
            }

            for (var index = 1; index < points.Count - 1; index++)
            {
                triangles.Add(start);
                triangles.Add(start + index);
                triangles.Add(start + index + 1);
            }
        }

        private void AddQuad(
            Vector2 bottomLeft,
            Vector2 topLeft,
            Vector2 topRight,
            Vector2 bottomRight,
            float z,
            Color32 bottomLeftColor,
            Color32 topLeftColor,
            Color32 topRightColor,
            Color32 bottomRightColor)
        {
            var start = vertices.Count;
            vertices.Add(new Vector3(bottomLeft.x, bottomLeft.y, z));
            vertices.Add(new Vector3(topLeft.x, topLeft.y, z));
            vertices.Add(new Vector3(topRight.x, topRight.y, z));
            vertices.Add(new Vector3(bottomRight.x, bottomRight.y, z));
            colors.Add(bottomLeftColor);
            colors.Add(topLeftColor);
            colors.Add(topRightColor);
            colors.Add(bottomRightColor);
            uvs.Add(Vector2.zero);
            uvs.Add(Vector2.zero);
            uvs.Add(Vector2.zero);
            uvs.Add(Vector2.zero);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Color32 EvaluateApprovedFill(float ratio)
        {
            if (ratio <= 0.62f)
            {
                return Color32.Lerp(FillStartColor, FillMiddleColor, ratio / 0.62f);
            }

            return Color32.Lerp(
                FillMiddleColor,
                FillEndColor,
                (ratio - 0.62f) / 0.38f);
        }

        private static Vector3 CompensateParentScale(Transform parent)
        {
            if (parent == null)
            {
                return Vector3.one;
            }

            var scale = parent.lossyScale;
            return new Vector3(
                SafeReciprocal(scale.x),
                SafeReciprocal(scale.y),
                SafeReciprocal(scale.z));
        }

        private static float SafeReciprocal(float value) =>
            Mathf.Abs(value) <= 0.000001f ? 1f : 1f / Mathf.Abs(value);

        private static void DestroyRuntimeObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
