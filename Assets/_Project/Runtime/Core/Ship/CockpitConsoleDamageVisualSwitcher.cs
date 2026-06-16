using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public sealed class CockpitConsoleDamageVisualSwitcher : MonoBehaviour
    {
        [SerializeField]
        private ShipDeviceInteractionState interactionState;

        [SerializeField]
        private GameObject normalConsoleRoot;

        [SerializeField]
        private GameObject destroyedConsoleRoot;

        public ShipDeviceInteractionState InteractionState => interactionState;

        public GameObject NormalConsoleRoot => normalConsoleRoot;

        public GameObject DestroyedConsoleRoot => destroyedConsoleRoot;

        public bool IsDestroyedVisualActive { get; private set; }

        public void Configure(
            ShipDeviceInteractionState nextInteractionState,
            GameObject nextNormalConsoleRoot,
            GameObject nextDestroyedConsoleRoot)
        {
            interactionState = nextInteractionState;
            normalConsoleRoot = nextNormalConsoleRoot;
            destroyedConsoleRoot = nextDestroyedConsoleRoot;
            Refresh();
        }

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (normalConsoleRoot == null || destroyedConsoleRoot == null)
            {
                return;
            }

            var shouldShowDestroyed = interactionState != null &&
                interactionState.CurrentShipState.GetRoom(ShipRoomId.Cockpit).CurrentDurability <= 0;

            if (normalConsoleRoot.activeSelf == shouldShowDestroyed)
            {
                normalConsoleRoot.SetActive(!shouldShowDestroyed);
            }

            if (destroyedConsoleRoot.activeSelf != shouldShowDestroyed)
            {
                destroyedConsoleRoot.SetActive(shouldShowDestroyed);
            }

            IsDestroyedVisualActive = shouldShowDestroyed;
        }
    }
}
