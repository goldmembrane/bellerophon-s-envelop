using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public sealed class ShipInteriorAtmosphereController : MonoBehaviour
    {
        public const float TargetFogDensity = 0.035f;
        public const float TargetCameraFarClip = 42f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private Light[] controlledLights;

        public Camera TargetCamera => targetCamera;

        public Light[] ControlledLights => controlledLights == null ? new Light[0] : (Light[])controlledLights.Clone();

        public void Configure(Camera camera, Light[] lights)
        {
            targetCamera = camera;
            controlledLights = lights == null ? new Light[0] : (Light[])lights.Clone();
            ApplyAtmosphere();
        }

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (controlledLights == null || controlledLights.Length == 0)
            {
                controlledLights = Object.FindObjectsByType<Light>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            }
        }

        private void OnEnable()
        {
            ApplyAtmosphere();
        }

        public void ApplyAtmosphere()
        {
            RenderSettings.ambientLight = new Color(0.025f, 0.03f, 0.032f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.018f, 0.022f, 0.024f, 1f);
            RenderSettings.fogDensity = TargetFogDensity;

            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = new Color(0.012f, 0.015f, 0.017f, 1f);
                targetCamera.farClipPlane = TargetCameraFarClip;
            }

            if (controlledLights == null)
            {
                return;
            }

            for (var i = 0; i < controlledLights.Length; i++)
            {
                var light = controlledLights[i];
                if (light == null)
                {
                    continue;
                }

                if (light.type == LightType.Directional)
                {
                    light.intensity = 0.22f;
                    light.color = new Color(0.62f, 0.69f, 0.66f, 1f);
                    continue;
                }

                light.intensity = Mathf.Min(light.intensity, 0.8f);
                light.color = new Color(0.52f, 0.68f, 0.62f, 1f);
            }
        }
    }
}
