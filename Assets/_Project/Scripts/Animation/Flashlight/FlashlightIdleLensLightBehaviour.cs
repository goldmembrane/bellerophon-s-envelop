using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [DefaultExecutionOrder(1100)]
    public sealed class FlashlightIdleLensLightBehaviour : MonoBehaviour
    {
        public const float LightOnDurationSeconds = 2f;
        public const float LightRangeMeters = 3f;
        public const float LightIntensity = 120f;
        public const float SpotAngleDegrees = 38f;
        public const float InnerSpotAngleDegrees = 24f;
        public const string LensLightObjectName = "Flashlight_Lens_Light";
        public const float LensSurfaceLightRangeMeters = 0.18f;
        public const float LensSurfaceLightIntensity = 35f;
        public const string LensSurfaceLightObjectName =
            "Flashlight_Lens_Surface_Light";

        private FlashlightRightHandFollowBehaviour carry;
        private Animator animator;
        private GameObject lensLightObject;
        private Light lensLight;
        private GameObject lensSurfaceLightObject;
        private Light lensSurfaceLight;

        public Light LensLight => lensLight;
        public Light LensSurfaceLight => lensSurfaceLight;
        public bool IsIlluminating => lensLight != null && lensLight.enabled &&
            lensSurfaceLight != null && lensSurfaceLight.enabled;

        public void RefreshPreview()
        {
            ResolveDependencies();
            EnsureLensLight();
            ApplyLensPose();
            ApplyLightState();
        }

        public void ResetEditModePreview()
        {
            if (Application.isPlaying) return;
            DestroyLensLight();
            RefreshPreview();
        }

        private void OnEnable()
        {
            RefreshPreview();
        }

        private void OnDisable()
        {
            DestroyLensLight();
        }

        private void OnDestroy()
        {
            DestroyLensLight();
        }

        private void LateUpdate()
        {
            RefreshPreview();
        }

        private void ResolveDependencies()
        {
            if (carry == null)
                carry = GetComponent<FlashlightRightHandFollowBehaviour>();
            if (carry == null)
                throw new MissingReferenceException(
                    name + " flashlight right-hand follow behaviour is missing.");
            if (animator == null) animator = GetComponent<Animator>();
        }

        private void EnsureLensLight()
        {
            carry.RefreshPreview();
            GameObject holder = carry.Holder;
            if (holder == null)
                throw new MissingReferenceException(name + " flashlight holder is missing.");
            if (lensLightObject != null && lensLight != null &&
                lensSurfaceLightObject != null && lensSurfaceLight != null &&
                lensLightObject.transform.parent == holder.transform &&
                lensSurfaceLightObject.transform.parent == holder.transform)
                return;

            DestroyLensLight();
            lensLightObject = new GameObject(LensLightObjectName);
            lensLightObject.transform.SetParent(holder.transform, false);
            lensLightObject.hideFlags = Application.isPlaying
                ? HideFlags.DontSave
                : HideFlags.DontSaveInEditor;
            lensLight = lensLightObject.AddComponent<Light>();
            lensLight.type = LightType.Spot;
            lensLight.color = Color.white;
            lensLight.intensity = LightIntensity;
            lensLight.range = LightRangeMeters;
            lensLight.spotAngle = SpotAngleDegrees;
            lensLight.innerSpotAngle = InnerSpotAngleDegrees;
            lensLight.shadows = LightShadows.Soft;
            lensLight.shadowStrength = 0.8f;
            lensLight.renderMode = LightRenderMode.ForcePixel;

            lensSurfaceLightObject = new GameObject(LensSurfaceLightObjectName);
            lensSurfaceLightObject.transform.SetParent(holder.transform, false);
            lensSurfaceLightObject.hideFlags = Application.isPlaying
                ? HideFlags.DontSave
                : HideFlags.DontSaveInEditor;
            lensSurfaceLight = lensSurfaceLightObject.AddComponent<Light>();
            lensSurfaceLight.type = LightType.Point;
            lensSurfaceLight.color = Color.white;
            lensSurfaceLight.intensity = LensSurfaceLightIntensity;
            lensSurfaceLight.range = LensSurfaceLightRangeMeters;
            lensSurfaceLight.shadows = LightShadows.None;
            lensSurfaceLight.renderMode = LightRenderMode.ForcePixel;
        }

        private void ApplyLensPose()
        {
            Transform holder = carry.Holder.transform;
            Transform model = carry.Model;
            if (model == null)
                throw new MissingReferenceException(name + " flashlight model is missing.");

            Vector3 direction = holder.up.normalized;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new MissingReferenceException(name + " flashlight renderer is missing.");

            float farthestProjection = float.NegativeInfinity;
            Vector3 lensCenter = holder.position;
            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                Vector3 extents = bounds.extents;
                float directionalExtent =
                    Mathf.Abs(direction.x) * extents.x +
                    Mathf.Abs(direction.y) * extents.y +
                    Mathf.Abs(direction.z) * extents.z;
                Vector3 candidate = bounds.center + direction * directionalExtent;
                float projection = Vector3.Dot(candidate, direction);
                if (projection <= farthestProjection) continue;
                farthestProjection = projection;
                lensCenter = candidate;
            }

            lensLightObject.transform.SetPositionAndRotation(
                lensCenter + direction * 0.004f,
                Quaternion.LookRotation(direction, holder.forward));
            lensSurfaceLightObject.transform.SetPositionAndRotation(
                lensCenter + direction * 0.008f,
                Quaternion.LookRotation(direction, holder.forward));
        }

        private void ApplyLightState()
        {
            bool shouldIlluminate = Application.isPlaying &&
                FlashlightIdleLocomotionCycleBehaviour.TryGetSequenceState(
                    animator,
                    out _,
                    out int phase,
                    out float phaseElapsed) &&
                phase == 0 &&
                phaseElapsed < LightOnDurationSeconds;
            lensLight.enabled = shouldIlluminate;
            lensSurfaceLight.enabled = shouldIlluminate;
        }

        private void DestroyLensLight()
        {
            if (lensLightObject == null) return;
            if (Application.isPlaying) Destroy(lensLightObject);
            else DestroyImmediate(lensLightObject);
            if (lensSurfaceLightObject != null)
            {
                if (Application.isPlaying) Destroy(lensSurfaceLightObject);
                else DestroyImmediate(lensSurfaceLightObject);
            }
            lensLightObject = null;
            lensLight = null;
            lensSurfaceLightObject = null;
            lensSurfaceLight = null;
        }
    }
}
