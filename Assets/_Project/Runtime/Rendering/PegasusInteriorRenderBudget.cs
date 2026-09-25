using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.Rendering
{
    /// <summary>One-time batching of explicitly selected, stationary sample corridor parts.</summary>
    [DisallowMultipleComponent]
    public sealed class PegasusInteriorRenderBudget : MonoBehaviour
    {
        // Explicit scene references exclude shutters, animated objects and unrelated rooms.
        [SerializeField] private MeshRenderer[] stationaryParts = System.Array.Empty<MeshRenderer>();
        private readonly HashSet<Mesh> ownedMeshes = new HashSet<Mesh>();

        private void Start()
        {
            var objects = new List<GameObject>(stationaryParts.Length);
            var filters = new List<MeshFilter>(stationaryParts.Length);
            var originalMeshes = new HashSet<Mesh>();
            foreach (var renderer in stationaryParts)
            {
                if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    renderer.isPartOfStaticBatch || !renderer.transform.IsChildOf(transform)) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable) continue;
                objects.Add(renderer.gameObject);
                filters.Add(filter);
                originalMeshes.Add(filter.sharedMesh);
            }
            if (objects.Count == 0) return;
            StaticBatchingUtility.Combine(objects.ToArray(), gameObject);
            foreach (var filter in filters)
                if (filter.sharedMesh && !originalMeshes.Contains(filter.sharedMesh))
                    ownedMeshes.Add(filter.sharedMesh);
        }

        private void OnDestroy()
        {
            // Only transient batching buffers are owned here; source mesh assets are untouched.
            foreach (var mesh in ownedMeshes)
                if (mesh) Destroy(mesh);
            ownedMeshes.Clear();
        }
    }
}
