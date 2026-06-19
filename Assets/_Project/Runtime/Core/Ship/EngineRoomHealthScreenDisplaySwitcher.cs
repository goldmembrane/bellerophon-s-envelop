using Bellerophon.Core.Session;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bellerophon.Core.Ship
{
    [ExecuteAlways]
    public sealed class EngineRoomHealthScreenDisplaySwitcher : MonoBehaviour
    {
        private const string MainDisplayObjectName = "ER-09 B2_Eq41_E single display tile surface";
        private const string LeftAuxiliaryDisplayObjectName = "ER-09 left decorative auxiliary wall screen decorative B2_Eq41_E display tile";
        private const string RightAuxiliaryDisplayObjectName = "ER-09 right decorative auxiliary wall screen decorative B2_Eq41_E display tile";
        private const float OfflineDurabilityThreshold = 0.2f;

        [SerializeField] private ShipDeviceInteractionState interactionState;
        [SerializeField] private Renderer displayRenderer;
        [SerializeField] private Renderer leftAuxiliaryDisplayRenderer;
        [SerializeField] private Renderer rightAuxiliaryDisplayRenderer;

        private MaterialPropertyBlock propertyBlock;

        public ShipDeviceInteractionState InteractionState => interactionState;

        public Renderer DisplayRenderer => displayRenderer;

        public Renderer LeftAuxiliaryDisplayRenderer => leftAuxiliaryDisplayRenderer;

        public Renderer RightAuxiliaryDisplayRenderer => rightAuxiliaryDisplayRenderer;

        public bool IsOfflineVisualActive { get; private set; }

        public void Configure(ShipDeviceInteractionState nextInteractionState, Renderer nextDisplayRenderer)
        {
            interactionState = nextInteractionState;
            displayRenderer = nextDisplayRenderer;
            AutoConfigureIfNeeded();
            Refresh();
        }

        public void RefreshForValidation()
        {
            Refresh();
        }

        private void Awake()
        {
            AutoConfigureIfNeeded();
            Refresh();
        }

        private void OnEnable()
        {
            AutoConfigureIfNeeded();
            Refresh();
        }

        private void Update()
        {
            AutoConfigureIfNeeded();
            Refresh();
        }

        private void OnDestroy()
        {
            ClearOfflineDisplay(displayRenderer);
            ClearOfflineDisplay(leftAuxiliaryDisplayRenderer);
            ClearOfflineDisplay(rightAuxiliaryDisplayRenderer);
        }

        private void AutoConfigureIfNeeded()
        {
            if (interactionState == null)
            {
                interactionState = Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            }

            if (displayRenderer == null)
            {
                displayRenderer = FindDisplayRenderer(MainDisplayObjectName);
            }

            if (leftAuxiliaryDisplayRenderer == null)
            {
                leftAuxiliaryDisplayRenderer = FindDisplayRenderer(LeftAuxiliaryDisplayObjectName);
            }

            if (rightAuxiliaryDisplayRenderer == null)
            {
                rightAuxiliaryDisplayRenderer = FindDisplayRenderer(RightAuxiliaryDisplayObjectName);
            }

        }

        public void Refresh()
        {
            if (displayRenderer == null && leftAuxiliaryDisplayRenderer == null && rightAuxiliaryDisplayRenderer == null)
            {
                return;
            }

            var shouldShowOffline = ShouldShowOfflineDisplay();
            if (shouldShowOffline)
            {
                ApplyOfflineDisplay(displayRenderer, ref propertyBlock);
                ApplyOfflineDisplay(leftAuxiliaryDisplayRenderer, ref propertyBlock);
                ApplyOfflineDisplay(rightAuxiliaryDisplayRenderer, ref propertyBlock);
            }
            else
            {
                ClearOfflineDisplay(displayRenderer);
                ClearOfflineDisplay(leftAuxiliaryDisplayRenderer);
                ClearOfflineDisplay(rightAuxiliaryDisplayRenderer);
            }

            IsOfflineVisualActive = shouldShowOffline;
        }

        private bool ShouldShowOfflineDisplay()
        {
            if (interactionState == null)
            {
                return false;
            }

            var engineRoom = interactionState.CurrentShipState.GetRoom(ShipRoomId.EngineRoom);
            return engineRoom.DurabilityPercent <= OfflineDurabilityThreshold;
        }

        private static Renderer FindDisplayRenderer(string objectName)
        {
            var transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform != null && transform.name == objectName)
                {
                    return transform.GetComponent<Renderer>();
                }
            }

            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInLoadedScene()
        {
            var renderer = FindDisplayRenderer(MainDisplayObjectName);
            if (renderer == null || renderer.GetComponent<EngineRoomHealthScreenDisplaySwitcher>() != null)
            {
                return;
            }

            var switcher = renderer.gameObject.AddComponent<EngineRoomHealthScreenDisplaySwitcher>();
            switcher.Configure(
                Object.FindFirstObjectByType<ShipDeviceInteractionState>(),
                renderer);
        }

        private static void ClearOfflineDisplay(Renderer renderer)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }

        private static void ApplyOfflineDisplay(Renderer renderer, ref MaterialPropertyBlock block)
        {
            if (renderer == null)
            {
                return;
            }

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", Color.black);
            block.SetColor("_Color", Color.black);
            block.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(block);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void ClearEditorPreviewAfterRefresh()
        {
            EditorApplication.delayCall += ClearEditorPreview;
        }

        private static void ClearEditorPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ClearOfflineDisplay(FindDisplayRenderer(MainDisplayObjectName));
            ClearOfflineDisplay(FindDisplayRenderer(LeftAuxiliaryDisplayObjectName));
            ClearOfflineDisplay(FindDisplayRenderer(RightAuxiliaryDisplayObjectName));
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }
}
