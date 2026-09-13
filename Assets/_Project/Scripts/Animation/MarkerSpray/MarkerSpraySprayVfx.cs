using Bellerophon.PlayerAnimation;
using UnityEngine;

namespace Bellerophon.Vfx
{
    [ExecuteAlways]
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class MarkerSpraySprayVfx : MonoBehaviour
    {
        public const int CurrentConfigurationVersion = 1;

        [SerializeField] private GameObject effectPrefab;
        [SerializeField] private Vector3 nozzleLocalPosition;
        [SerializeField] private Quaternion nozzleLocalRotation = Quaternion.identity;
        [SerializeField] private int configurationVersion;

        private MarkerSprayRightHandFollowBehaviour carry;
        private Transform boundSprayModel;
        private GameObject effectInstance;
        private ParticleSystem[] layers;
        private bool emissionRequested = true;

        public GameObject EffectPrefab => effectPrefab;
        public GameObject EffectInstance => effectInstance;
        public Transform BoundSprayModel => boundSprayModel;
        public Vector3 NozzleLocalPosition => nozzleLocalPosition;
        public Quaternion NozzleLocalRotation => nozzleLocalRotation;
        public int ConfigurationVersion => configurationVersion;
        public bool EmissionRequested => emissionRequested;
        public int LayerCount => layers != null ? layers.Length : 0;

        public Vector3 NozzleWorldPosition => boundSprayModel != null
            ? boundSprayModel.TransformPoint(nozzleLocalPosition)
            : transform.position;

        public Quaternion NozzleWorldRotation => boundSprayModel != null
            ? boundSprayModel.rotation * nozzleLocalRotation
            : transform.rotation;

        public void Configure(
            GameObject configuredEffectPrefab,
            Vector3 configuredNozzleLocalPosition,
            Quaternion configuredNozzleLocalRotation)
        {
            effectPrefab = configuredEffectPrefab;
            nozzleLocalPosition = configuredNozzleLocalPosition;
            nozzleLocalRotation = configuredNozzleLocalRotation;
            configurationVersion = CurrentConfigurationVersion;
            carry = null;
            RebindEffect();
        }

        public void BeginEmission()
        {
            emissionRequested = true;
            RefreshBinding();
            PlayLayers();
        }

        public void EndEmission()
        {
            emissionRequested = false;
            StopLayers(false);
        }

        public void RefreshPreview(float simulationSeconds = 0.9f)
        {
            RefreshBinding();
            if (Application.isPlaying || layers == null)
            {
                return;
            }

            foreach (ParticleSystem layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                layer.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                layer.Simulate(
                    Mathf.Max(0f, simulationSeconds),
                    true,
                    true,
                    true);
                layer.Pause(true);
            }
        }

        private void OnEnable()
        {
            emissionRequested = true;
            RefreshBinding();
            if (Application.isPlaying)
            {
                PlayLayers();
            }
            else
            {
                RefreshPreview();
            }
        }

        private void LateUpdate()
        {
            RefreshBinding();
            if (Application.isPlaying && emissionRequested)
            {
                PlayLayers();
            }
        }

        private void OnAnimatorMove()
        {
            RefreshBinding();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                DetachFadingTail();
            }
            else
            {
                DestroyEffectImmediate();
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                DestroyEffectImmediate();
            }
        }

        private void RefreshBinding()
        {
            if (effectPrefab == null)
            {
                return;
            }

            if (carry == null)
            {
                carry = GetComponent<MarkerSprayRightHandFollowBehaviour>();
            }

            if (carry == null)
            {
                return;
            }

            if (carry.SprayModel == null && !Application.isPlaying)
            {
                carry.RefreshPreview();
            }

            Transform currentModel = carry.SprayModel;
            if (currentModel == null)
            {
                return;
            }

            if (boundSprayModel != currentModel || effectInstance == null)
            {
                RebindEffect();
            }

            if (effectInstance == null)
            {
                return;
            }

            effectInstance.transform.SetPositionAndRotation(
                currentModel.TransformPoint(nozzleLocalPosition),
                currentModel.rotation * nozzleLocalRotation);
            effectInstance.transform.localScale = Vector3.one;
        }

        private void RebindEffect()
        {
            DestroyEffectImmediate();
            if (effectPrefab == null)
            {
                return;
            }

            if (carry == null)
            {
                carry = GetComponent<MarkerSprayRightHandFollowBehaviour>();
            }

            if (carry == null)
            {
                return;
            }

            if (carry.SprayModel == null && !Application.isPlaying)
            {
                carry.RefreshPreview();
            }

            boundSprayModel = carry.SprayModel;
            if (boundSprayModel == null)
            {
                return;
            }

            Transform parent = carry.SprayHolder != null
                ? carry.SprayHolder.transform
                : transform;
            effectInstance = Instantiate(effectPrefab, parent, false);
            effectInstance.name = "MarkerSpraySpray_VFX";
            if (!Application.isPlaying)
            {
                SetHideFlagsRecursively(
                    effectInstance,
                    HideFlags.DontSaveInEditor);
            }

            layers = effectInstance.GetComponentsInChildren<ParticleSystem>(true);
            effectInstance.transform.SetPositionAndRotation(
                boundSprayModel.TransformPoint(nozzleLocalPosition),
                boundSprayModel.rotation * nozzleLocalRotation);
            effectInstance.transform.localScale = Vector3.one;
        }

        private void PlayLayers()
        {
            if (layers == null)
            {
                return;
            }

            foreach (ParticleSystem layer in layers)
            {
                if (layer != null && !layer.isEmitting)
                {
                    layer.Play(true);
                }
            }
        }

        private void StopLayers(bool clear)
        {
            if (layers == null)
            {
                return;
            }

            ParticleSystemStopBehavior behavior = clear
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;
            foreach (ParticleSystem layer in layers)
            {
                if (layer != null)
                {
                    layer.Stop(true, behavior);
                }
            }
        }

        private void DetachFadingTail()
        {
            if (effectInstance == null)
            {
                return;
            }

            StopLayers(false);
            effectInstance.transform.SetParent(null, true);
            MarkerSpraySprayVfxTail tail =
                effectInstance.AddComponent<MarkerSpraySprayVfxTail>();
            tail.Initialize(layers, 1.2f);
            effectInstance = null;
            layers = null;
            boundSprayModel = null;
        }

        private void DestroyEffectImmediate()
        {
            if (effectInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(effectInstance);
                }
                else
                {
                    DestroyImmediate(effectInstance);
                }
            }

            effectInstance = null;
            layers = null;
            boundSprayModel = null;
        }

        private static void SetHideFlagsRecursively(
            GameObject root,
            HideFlags flags)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                item.gameObject.hideFlags = flags;
            }
        }
    }

    [DisallowMultipleComponent]
    internal sealed class MarkerSpraySprayVfxTail : MonoBehaviour
    {
        private ParticleSystem[] layers;
        private float maximumLifetimeSeconds;

        internal void Initialize(
            ParticleSystem[] configuredLayers,
            float configuredMaximumLifetimeSeconds)
        {
            layers = configuredLayers;
            maximumLifetimeSeconds = configuredMaximumLifetimeSeconds;
        }

        private void Update()
        {
            maximumLifetimeSeconds -= Time.deltaTime;
            if (maximumLifetimeSeconds > 0f && AnyLayerAlive())
            {
                return;
            }

            Destroy(gameObject);
        }

        private bool AnyLayerAlive()
        {
            if (layers == null)
            {
                return false;
            }

            foreach (ParticleSystem layer in layers)
            {
                if (layer != null && layer.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
