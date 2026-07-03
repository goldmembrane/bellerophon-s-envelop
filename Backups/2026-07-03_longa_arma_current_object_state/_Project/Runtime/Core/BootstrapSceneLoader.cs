using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Core
{
    public sealed class BootstrapSceneLoader : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "CargoRunMvp";

        public string NextSceneName => nextSceneName;

        private void Start()
        {
            if (!string.IsNullOrWhiteSpace(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
