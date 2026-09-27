using UnityEngine;
using UnityEngine.AI;

namespace Bellerophon.Enemies.Parvum
{
    public sealed class ParvumNavigation : MonoBehaviour
    {
        [SerializeField] private NavMeshData data;
        private NavMeshDataInstance instance;
        public void Configure(NavMeshData navigationData) => data=navigationData;
        private void OnEnable() { if(data) instance=NavMesh.AddNavMeshData(data); }
        private void OnDisable() { if(instance.valid) instance.Remove(); }
    }
}
