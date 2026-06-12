using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public sealed class ShipInteriorAtmosphereController : MonoBehaviour
    {
        public const float TargetFogDensity = 0.044f;
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
            RenderSettings.ambientLight = new Color(0.006f, 0.007f, 0.007f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.006f, 0.007f, 0.008f, 1f);
            RenderSettings.fogDensity = TargetFogDensity;

            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = new Color(0.004f, 0.005f, 0.006f, 1f);
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
                    light.intensity = 0.065f;
                    light.color = new Color(0.38f, 0.43f, 0.40f, 1f);
                    continue;
                }

                light.intensity = Mathf.Min(light.intensity, 0.38f);
                light.color = new Color(0.46f, 0.52f, 0.46f, 1f);
            }
        }
    }
}
