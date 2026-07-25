using UnityEngine;

namespace Bellerophon.Enemies.Negatif
{
    [DisallowMultipleComponent]
    public sealed class NegatifEyeEmissionPulse : MonoBehaviour
    {
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [SerializeField] private Renderer[] eyeRenderers = new Renderer[0];
        [SerializeField, Range(0f, 12f)] private float emissionStrength = 9f;

        private MaterialPropertyBlock propertyBlock;

        public float EmissionStrength => emissionStrength;
        public Renderer[] EyeRenderers => eyeRenderers;

        public void Configure(Renderer negativeEye, Renderer positiveEye, float maximumStrength)
        {
            eyeRenderers = new[] { negativeEye, positiveEye };
            emissionStrength = maximumStrength;
            ApplyCurrentEmission();
        }

        public void ApplyCurrentEmission()
        {
            ApplyEmission(emissionStrength);
        }

        public void ApplyPreviewEmission(float strength)
        {
            ApplyEmission(strength);
        }

        private void Awake()
        {
            ApplyCurrentEmission();
        }

        private void OnEnable()
        {
            ApplyCurrentEmission();
        }

        private void LateUpdate()
        {
            ApplyCurrentEmission();
        }

        private void ApplyEmission(float strength)
        {
            propertyBlock ??= new MaterialPropertyBlock();
            var clampedStrength = Mathf.Clamp(strength, 0f, 12f);
            foreach (var eyeRenderer in eyeRenderers)
            {
                if (eyeRenderer == null)
                {
                    continue;
                }

                eyeRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(EmissionStrengthId, clampedStrength);
                eyeRenderer.SetPropertyBlock(propertyBlock);
                propertyBlock.Clear();
            }
        }
    }
}
