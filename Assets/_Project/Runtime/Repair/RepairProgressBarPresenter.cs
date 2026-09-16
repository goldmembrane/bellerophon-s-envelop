using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellerophon.Repair
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RepairProgressBarPresenter : MonoBehaviour
    {
        public const float DesignDurationSeconds = 5f;
        public const float WidthMeters = 1.20f;
        public const float HeightMeters = 0.19f;
        public const float HeadClearanceMeters = 0.04f;

        private static readonly Color32 BackplateColor =
            new Color32(0x02, 0x09, 0x0d, 0xff);
        private static readonly Color32 OuterLineColor =
            new Color32(0x4c, 0xc8, 0xe6, 0xff);
        private static readonly Color32 InnerLineColor =
            new Color32(0x38, 0xbc, 0xdc, 0xff);
        private static readonly Color32 TrackColor =
            new Color32(0x04, 0x18, 0x21, 0xff);
        private static readonly Color32 FillColor =
            new Color32(0x43, 0xd7, 0xf3, 0xff);
        private static readonly Color32 FillHighlightColor =
            new Color32(0x9b, 0xf1, 0xff, 0xff);
        private static readonly Color32 ProgressMarkerColor =
            new Color32(0xef, 0xfd, 0xff, 0xff);

        [SerializeField] private Transform headAnchor;
        [SerializeField] private float headSurfaceOffsetMeters;
        [SerializeField] private float durationSeconds = DesignDurationSeconds;
        [SerializeField] private bool restartOnCompletion = true;
        [SerializeField, Range(0f, 1f)] private float editModePreviewProgress = 0.5f;
        [SerializeField, HideInInspector] private float elapsedSeconds;

        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private Mesh runtimeMesh;
        private Material runtimeMaterial;

        public Transform HeadAnchor => headAnchor;
        public float DurationSeconds => Mathf.Max(0.0001f, durationSeconds);
        public bool RestartOnCompletion => restartOnCompletion;
        public float EditModePreviewProgress => editModePreviewProgress;
        public float NormalizedProgress => Application.isPlaying
            ? Mathf.Clamp01(elapsedSeconds / DurationSeconds)
            : editModePreviewProgress;

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
            if (!Application.isPlaying)
            {
                return;
            }

            elapsedSeconds = restartOnCompletion
                ? Mathf.Repeat(elapsedSeconds + Time.deltaTime, DurationSeconds)
                : Mathf.Min(DurationSeconds, elapsedSeconds + Time.deltaTime);
            RebuildMesh();
        }

        private void LateUpdate()
        {
            ApplyPose(Camera.main);
        }

        private void OnValidate()
        {
            durationSeconds = Mathf.Max(0.0001f, durationSeconds);
            headSurfaceOffsetMeters = Mathf.Max(0f, headSurfaceOffsetMeters);
            editModePreviewProgress = Mathf.Clamp01(editModePreviewProgress);
            elapsedSeconds = Mathf.Clamp(elapsedSeconds, 0f, DurationSeconds);
            EnsureResources();
            ApplyPose(Camera.main);
            RebuildMesh();
        }

        private void OnDestroy()
        {
            DestroyRuntimeObject(runtimeMesh);
            DestroyRuntimeObject(runtimeMaterial);
        }

        public void Configure(
            Transform targetHeadAnchor,
            float visibleHeadTopWorldY,
            float authoritativeDurationSeconds,
            bool shouldRestartOnCompletion)
        {
            headAnchor = targetHeadAnchor;
            headSurfaceOffsetMeters = Mathf.Clamp(
                visibleHeadTopWorldY - targetHeadAnchor.position.y,
                0.18f,
                0.28f);
            durationSeconds = Mathf.Max(0.0001f, authoritativeDurationSeconds);
            restartOnCompletion = shouldRestartOnCompletion;
            editModePreviewProgress = 0.5f;
            elapsedSeconds = 0f;
            EnsureResources();
            ApplyPose(Camera.main);
            RebuildMesh();
        }

        public void SetProgress(float progress01)
        {
            elapsedSeconds = Mathf.Clamp01(progress01) * DurationSeconds;
            restartOnCompletion = false;
            RebuildMesh();
        }

        public void ResumeDesignLoop()
        {
            restartOnCompletion = true;
            elapsedSeconds = 0f;
            RebuildMesh();
        }

        public void RefreshForCamera(Camera targetCamera)
        {
            EnsureResources();
            ApplyPose(targetCamera);
            RebuildMesh();
        }

        private void ApplyPose(Camera targetCamera)
        {
            if (headAnchor == null)
            {
                return;
            }

            transform.position = headAnchor.position + Vector3.up *
                (headSurfaceOffsetMeters + HeadClearanceMeters + HeightMeters * 0.5f);
            Vector3 compensation = CompensateParentScale(transform.parent);
            transform.localScale = new Vector3(
                -compensation.x,
                compensation.y,
                compensation.z);
            if (targetCamera == null)
            {
                return;
            }

            Vector3 direction = targetCamera.transform.position - transform.position;
            if (direction.sqrMagnitude > 0.000001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private void EnsureResources()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (runtimeMesh == null)
            {
                runtimeMesh = new Mesh
                {
                    name = "Repair Progress Bar Runtime Mesh",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (runtimeMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    return;
                }

                runtimeMaterial = new Material(shader)
                {
                    name = "Repair Progress Bar Runtime Material",
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

            AddCenteredQuad(WidthMeters, HeightMeters, 0f, BackplateColor);
            AddCenteredQuad(
                WidthMeters - 0.012f,
                HeightMeters - 0.012f,
                0.0001f,
                OuterLineColor);
            AddCenteredQuad(
                WidthMeters - 0.025f,
                HeightMeters - 0.025f,
                0.0002f,
                BackplateColor);
            AddCenteredQuad(
                WidthMeters - 0.055f,
                HeightMeters - 0.042f,
                0.0003f,
                InnerLineColor);

            float trackWidth = WidthMeters - 0.072f;
            float trackHeight = HeightMeters - 0.058f;
            AddCenteredQuad(trackWidth, trackHeight, 0.0004f, TrackColor);

            float progress = NormalizedProgress;
            float fillWidth = trackWidth * progress;
            if (fillWidth > 0.0001f)
            {
                float left = -trackWidth * 0.5f;
                float right = left + fillWidth;
                AddQuad(
                    left,
                    right,
                    -trackHeight * 0.5f,
                    trackHeight * 0.5f,
                    0.0005f,
                    FillColor);

                float highlightWidth = Mathf.Min(0.018f, fillWidth);
                AddQuad(
                    right - highlightWidth,
                    right,
                    -trackHeight * 0.5f,
                    trackHeight * 0.5f,
                    0.0006f,
                    FillHighlightColor);

                float markerWidth = Mathf.Min(0.008f, fillWidth);
                AddQuad(
                    right - markerWidth * 0.5f,
                    right + markerWidth * 0.5f,
                    -trackHeight * 0.72f,
                    trackHeight * 0.72f,
                    0.0007f,
                    ProgressMarkerColor);
            }

            runtimeMesh.Clear();
            runtimeMesh.SetVertices(vertices);
            runtimeMesh.SetColors(colors);
            runtimeMesh.SetUVs(0, uvs);
            runtimeMesh.SetTriangles(triangles, 0, true);
            runtimeMesh.RecalculateBounds();
        }

        private void AddCenteredQuad(float width, float height, float z, Color32 color)
        {
            AddQuad(
                -width * 0.5f,
                width * 0.5f,
                -height * 0.5f,
                height * 0.5f,
                z,
                color);
        }

        private void AddQuad(
            float minimumX,
            float maximumX,
            float minimumY,
            float maximumY,
            float z,
            Color32 color)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(minimumX, minimumY, z));
            vertices.Add(new Vector3(minimumX, maximumY, z));
            vertices.Add(new Vector3(maximumX, maximumY, z));
            vertices.Add(new Vector3(maximumX, minimumY, z));
            for (int index = 0; index < 4; index++)
            {
                colors.Add(color);
                uvs.Add(Vector2.zero);
            }

            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        private static Vector3 CompensateParentScale(Transform parent)
        {
            if (parent == null)
            {
                return Vector3.one;
            }

            Vector3 scale = parent.lossyScale;
            return new Vector3(
                SafeReciprocal(scale.x),
                SafeReciprocal(scale.y),
                SafeReciprocal(scale.z));
        }

        private static float SafeReciprocal(float value)
        {
            return Mathf.Abs(value) <= 0.000001f ? 1f : 1f / Mathf.Abs(value);
        }

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
