using UnityEngine;

namespace Bellerophon.ArtSamples
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BatonElectricVfxSampleSequence : MonoBehaviour
    {
        [SerializeField] private BatonElectricVfxUnitySample chargeReady;
        [SerializeField] private BatonElectricVfxUnitySample discharge;
        [SerializeField] private Camera sampleCamera;
        [SerializeField] private float cycleSeconds = 4f;
        [SerializeField] private float chargeSeconds = 2.4f;

        private void OnEnable()
        {
            Apply(Time.realtimeSinceStartup);
        }

        private void Update()
        {
            Apply(Time.realtimeSinceStartup);
        }

        private void Apply(float previewTime)
        {
            if (chargeReady == null || discharge == null ||
                sampleCamera == null)
            {
                return;
            }

            float phase = Mathf.Repeat(
                previewTime,
                Mathf.Max(0.1f, cycleSeconds));
            bool showCharge = phase < chargeSeconds;
            chargeReady.gameObject.SetActive(showCharge);
            discharge.gameObject.SetActive(!showCharge);

            if (showCharge)
            {
                chargeReady.ResumeAnimatedPreview();
                sampleCamera.orthographicSize = 0.33f;
                sampleCamera.transform.position = new Vector3(0f, 0.25f, -2f);
                sampleCamera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(0f, 0.25f, 0f) -
                    sampleCamera.transform.position,
                    Vector3.up);
            }
            else
            {
                discharge.ResumeAnimatedPreview();
                sampleCamera.orthographicSize = 1.55f;
                sampleCamera.transform.position = new Vector3(2.48f, 0.25f, -4f);
                sampleCamera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(2.48f, 0.25f, 0f) -
                    sampleCamera.transform.position,
                    Vector3.up);
            }
        }
    }
}
