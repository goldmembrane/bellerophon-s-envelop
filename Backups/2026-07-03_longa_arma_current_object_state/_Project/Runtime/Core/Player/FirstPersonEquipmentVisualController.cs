using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEngine;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonEquipmentVisualController : MonoBehaviour
    {
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private GameObject stickVisual;
        [SerializeField] private GameObject musketVisual;
        [SerializeField] private GameObject protectiveSuitReadout;

        public GameObject StickVisual => stickVisual;

        public GameObject MusketVisual => musketVisual;

        public GameObject ProtectiveSuitReadout => protectiveSuitReadout;

        public void Configure(
            ShipDeviceInteractionState deviceState,
            GameObject stick,
            GameObject musket,
            GameObject suitReadout)
        {
            shipDeviceState = deviceState;
            stickVisual = stick;
            musketVisual = musket;
            protectiveSuitReadout = suitReadout;
            RefreshVisuals();
        }

        private void Awake()
        {
            if (shipDeviceState == null)
            {
                shipDeviceState = Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            }

            RefreshVisuals();
        }

        private void OnEnable()
        {
            RefreshVisuals();
        }

        private void LateUpdate()
        {
            RefreshVisuals();
        }

        public void RefreshForValidation()
        {
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (shipDeviceState == null)
            {
                SetActive(stickVisual, false);
                SetActive(musketVisual, false);
                SetActive(protectiveSuitReadout, false);
                return;
            }

            var equipment = shipDeviceState.CurrentEquipmentState;
            var activeItem = equipment.ActiveHandSlot.ItemKind;
            SetActive(stickVisual, activeItem == EquipmentItemKind.Stick);
            SetActive(musketVisual, activeItem == EquipmentItemKind.Musket);
            SetActive(
                protectiveSuitReadout,
                equipment.ActiveProtectiveItemKind != EquipmentItemKind.None);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
