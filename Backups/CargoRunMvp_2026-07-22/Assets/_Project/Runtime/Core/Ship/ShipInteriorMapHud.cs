using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.UI;

namespace Bellerophon.Core.Ship
{
    public sealed class ShipInteriorMapHud : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private ShipDeviceInteractionState shipDeviceState;
        [SerializeField] private RectTransform mapRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text currentRoomText;
        [SerializeField] private RectTransform currentRoomMarker;
        [SerializeField] private Image cockpitImage;
        [SerializeField] private Image cargoHoldImage;
        [SerializeField] private Image armoryImage;
        [SerializeField] private Image supplyRoomImage;
        [SerializeField] private Image engineRoomImage;
        [SerializeField] private Image controlRoomImage;

        public Transform PlayerTransform => playerTransform;

        public RectTransform MapRoot => mapRoot;

        public Text TitleText => titleText;

        public Text CurrentRoomText => currentRoomText;

        public RectTransform CurrentRoomMarker => currentRoomMarker;

        public ShipRoomId CurrentRoom { get; private set; }

        public void Configure(
            Transform player,
            ShipDeviceInteractionState deviceState,
            RectTransform root,
            Text title,
            Text currentRoom,
            RectTransform marker,
            Image cockpit,
            Image cargoHold,
            Image armory,
            Image supplyRoom,
            Image engineRoom,
            Image controlRoom)
        {
            playerTransform = player;
            shipDeviceState = deviceState;
            mapRoot = root;
            titleText = title;
            currentRoomText = currentRoom;
            currentRoomMarker = marker;
            cockpitImage = cockpit;
            cargoHoldImage = cargoHold;
            armoryImage = armory;
            supplyRoomImage = supplyRoom;
            engineRoomImage = engineRoom;
            controlRoomImage = controlRoom;
            Refresh();
        }

        private void Awake()
        {
            if (playerTransform == null)
            {
                var player = Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
                playerTransform = player == null ? null : player.transform;
            }

            if (shipDeviceState == null)
            {
                shipDeviceState = Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            }
        }

        private void Update()
        {
            Refresh();
        }

        public void RefreshForValidation()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (mapRoot != null)
            {
                mapRoot.localScale = new Vector3(
                    ShipInteriorMapRules.ShipInteriorMapScale,
                    ShipInteriorMapRules.ShipInteriorMapScale,
                    1f);
            }

            CurrentRoom = ShipInteriorMapRules.FindCurrentRoom(
                playerTransform == null ? Vector3.zero : playerTransform.position);
            if (titleText != null)
            {
                titleText.text = "Ship Map";
            }

            if (currentRoomText != null)
            {
                currentRoomText.text = "Current: " + ShipInteriorMapRules.FormatRoomName(CurrentRoom);
            }

            RefreshRoomImages();
            RefreshMarker();
        }

        private void RefreshRoomImages()
        {
            SetRoomColor(ShipRoomId.Cockpit, cockpitImage);
            SetRoomColor(ShipRoomId.CargoHold, cargoHoldImage);
            SetRoomColor(ShipRoomId.Armory, armoryImage);
            SetRoomColor(ShipRoomId.SupplyRoom, supplyRoomImage);
            SetRoomColor(ShipRoomId.EngineRoom, engineRoomImage);
            SetRoomColor(ShipRoomId.ControlRoom, controlRoomImage);
        }

        private void RefreshMarker()
        {
            if (currentRoomMarker == null)
            {
                return;
            }

            var room = ShipInteriorMapRules.GetRoom(CurrentRoom);
            currentRoomMarker.anchoredPosition = room.MapPosition;
            currentRoomMarker.sizeDelta = room.MapSize * ShipInteriorMapRules.ShipInteriorMapScale;
            currentRoomMarker.gameObject.SetActive(true);
        }

        private void SetRoomColor(ShipRoomId roomId, Image image)
        {
            if (image == null)
            {
                return;
            }

            var isCurrent = roomId == CurrentRoom;
            var durability = shipDeviceState == null
                ? 1f
                : shipDeviceState.CurrentShipState.GetRoom(roomId).DurabilityPercent;
            image.color = GetRoomColor(durability, isCurrent);
        }

        private static Color GetRoomColor(float durability, bool isCurrent)
        {
            if (isCurrent)
            {
                return new Color(0.17f, 0.72f, 0.58f, 0.92f);
            }

            if (durability <= 0f)
            {
                return new Color(0.02f, 0.02f, 0.02f, 0.86f);
            }

            if (durability <= 0.25f)
            {
                return new Color(0.52f, 0.08f, 0.06f, 0.82f);
            }

            if (durability <= 0.5f)
            {
                return new Color(0.53f, 0.23f, 0.12f, 0.82f);
            }

            if (durability <= 0.75f)
            {
                return new Color(0.48f, 0.42f, 0.16f, 0.82f);
            }

            return new Color(0.18f, 0.25f, 0.24f, 0.82f);
        }
    }
}
