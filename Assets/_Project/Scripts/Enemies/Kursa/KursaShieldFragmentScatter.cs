using System;
using UnityEngine;

namespace Bellerophon.Enemies.Kursa
{
    public static class KursaShieldFragmentScatterMath
    {
        public static void Evaluate(
            float elapsedSeconds,
            float scatterSeconds,
            Vector3[] sourceVertices,
            Vector3[] sourceNormals,
            Vector4[] sourceTangents,
            Vector3[] startCenters,
            Vector3[] endCenters,
            Quaternion[] endRotations,
            Vector3[] outputVertices,
            Vector3[] outputNormals,
            Vector4[] outputTangents)
        {
            if (scatterSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(scatterSeconds));
            if (startCenters == null || endCenters == null || endRotations == null ||
                startCenters.Length != endCenters.Length ||
                startCenters.Length != endRotations.Length)
            {
                throw new ArgumentException("Kursa shield fragment motion data is incomplete.");
            }

            var expectedVertexCount = startCenters.Length * 3;
            if (sourceVertices == null || outputVertices == null ||
                sourceVertices.Length != expectedVertexCount ||
                outputVertices.Length != expectedVertexCount)
            {
                throw new ArgumentException("Kursa shield fragment vertex data is incomplete.");
            }
            if (sourceNormals == null || outputNormals == null ||
                sourceNormals.Length != expectedVertexCount ||
                outputNormals.Length != expectedVertexCount)
            {
                throw new ArgumentException("Kursa shield fragment normal data is incomplete.");
            }
            if (sourceTangents == null || outputTangents == null ||
                sourceTangents.Length != expectedVertexCount ||
                outputTangents.Length != expectedVertexCount)
            {
                throw new ArgumentException("Kursa shield fragment tangent data is incomplete.");
            }

            var progress = Mathf.Clamp01(elapsedSeconds / scatterSeconds);
            for (var fragmentIndex = 0;
                fragmentIndex < startCenters.Length;
                fragmentIndex++)
            {
                var startCenter = startCenters[fragmentIndex];
                var center = Vector3.LerpUnclamped(
                    startCenter,
                    endCenters[fragmentIndex],
                    progress);
                var rotation = Quaternion.LerpUnclamped(
                    Quaternion.identity,
                    endRotations[fragmentIndex],
                    progress);
                var vertexStart = fragmentIndex * 3;
                for (var offset = 0; offset < 3; offset++)
                {
                    var vertexIndex = vertexStart + offset;
                    outputVertices[vertexIndex] = center + rotation *
                        (sourceVertices[vertexIndex] - startCenter);
                    outputNormals[vertexIndex] =
                        (rotation * sourceNormals[vertexIndex]).normalized;
                    var sourceTangent = sourceTangents[vertexIndex];
                    var tangentDirection = rotation * new Vector3(
                        sourceTangent.x,
                        sourceTangent.y,
                        sourceTangent.z);
                    outputTangents[vertexIndex] = new Vector4(
                        tangentDirection.x,
                        tangentDirection.y,
                        tangentDirection.z,
                        sourceTangent.w);
                }
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class KursaShieldFragmentScatter : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        // These values bind the approved one-second effect to the Mixamo loop.
        [SerializeField] private float scatterSeconds = 1f;
        [SerializeField] private float clipSeconds;
        // One center and rotation per three-vertex shield triangle replaces per-object curves.
        [SerializeField] private Vector3[] startCenters = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] endCenters = Array.Empty<Vector3>();
        [SerializeField] private Quaternion[] endRotations = Array.Empty<Quaternion>();

        private Mesh runtimeMesh;
        private Vector3[] sourceVertices;
        private Vector3[] sourceNormals;
        private Vector4[] sourceTangents;
        private Vector3[] outputVertices;
        private Vector3[] outputNormals;
        private Vector4[] outputTangents;

        public Animator Animator => animator;
        public MeshFilter MeshFilter => meshFilter;
        public MeshRenderer MeshRenderer => meshRenderer;
        public float ScatterSeconds => scatterSeconds;
        public float ClipSeconds => clipSeconds;
        public int FragmentCount => startCenters?.Length ?? 0;

        public void Configure(
            Animator configuredAnimator,
            MeshFilter configuredMeshFilter,
            MeshRenderer configuredMeshRenderer,
            float configuredScatterSeconds,
            float configuredClipSeconds,
            Vector3[] configuredStartCenters,
            Vector3[] configuredEndCenters,
            Quaternion[] configuredEndRotations)
        {
            animator = configuredAnimator;
            meshFilter = configuredMeshFilter;
            meshRenderer = configuredMeshRenderer;
            scatterSeconds = configuredScatterSeconds;
            clipSeconds = configuredClipSeconds;
            startCenters = configuredStartCenters ?? Array.Empty<Vector3>();
            endCenters = configuredEndCenters ?? Array.Empty<Vector3>();
            endRotations = configuredEndRotations ?? Array.Empty<Quaternion>();
            ClearCachedGeometry();
            RequireValidConfiguration();
        }

        public void EvaluateAtSeconds(float elapsedSeconds)
        {
            RequireValidConfiguration();
            EnsureGeometryCache();
            KursaShieldFragmentScatterMath.Evaluate(
                elapsedSeconds,
                scatterSeconds,
                sourceVertices,
                sourceNormals,
                sourceTangents,
                startCenters,
                endCenters,
                endRotations,
                outputVertices,
                outputNormals,
                outputTangents);
            var mesh = meshFilter.sharedMesh;
            mesh.vertices = outputVertices;
            mesh.normals = outputNormals;
            mesh.tangents = outputTangents;
            mesh.RecalculateBounds();
            meshRenderer.enabled = elapsedSeconds < scatterSeconds;
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            RequireValidConfiguration();
            runtimeMesh = Instantiate(meshFilter.sharedMesh);
            runtimeMesh.name = meshFilter.sharedMesh.name + "_Runtime";
            runtimeMesh.MarkDynamic();
            meshFilter.sharedMesh = runtimeMesh;
            ClearCachedGeometry();
            EvaluateAtSeconds(0f);
        }

        private void OnEnable()
        {
            if (Application.isPlaying && runtimeMesh != null)
                EvaluateAtSeconds(0f);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || animator == null || runtimeMesh == null)
                return;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            var normalizedLoopTime = state.normalizedTime -
                Mathf.Floor(state.normalizedTime);
            EvaluateAtSeconds(normalizedLoopTime * clipSeconds);
        }

        private void OnDestroy()
        {
            if (runtimeMesh != null)
                Destroy(runtimeMesh);
        }

        private void EnsureGeometryCache()
        {
            if (sourceVertices != null) return;
            var mesh = meshFilter.sharedMesh;
            sourceVertices = mesh.vertices;
            sourceNormals = mesh.normals;
            sourceTangents = mesh.tangents;
            outputVertices = new Vector3[sourceVertices.Length];
            outputNormals = new Vector3[sourceNormals.Length];
            outputTangents = new Vector4[sourceTangents.Length];
        }

        private void ClearCachedGeometry()
        {
            sourceVertices = null;
            sourceNormals = null;
            sourceTangents = null;
            outputVertices = null;
            outputNormals = null;
            outputTangents = null;
        }

        private void RequireValidConfiguration()
        {
            if (animator == null || meshFilter == null || meshRenderer == null ||
                meshFilter.sharedMesh == null)
            {
                throw new InvalidOperationException(
                    "Kursa shield fragment scatter references are incomplete.");
            }
            if (scatterSeconds <= 0f || clipSeconds <= scatterSeconds)
            {
                throw new InvalidOperationException(
                    "Kursa shield fragment timing is invalid.");
            }
            if (startCenters == null || endCenters == null || endRotations == null ||
                startCenters.Length == 0 ||
                startCenters.Length != endCenters.Length ||
                startCenters.Length != endRotations.Length ||
                meshFilter.sharedMesh.vertexCount != startCenters.Length * 3 ||
                meshFilter.sharedMesh.normals.Length != meshFilter.sharedMesh.vertexCount ||
                meshFilter.sharedMesh.tangents.Length != meshFilter.sharedMesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "Kursa shield fragment geometry data is invalid.");
            }
        }
    }
}
