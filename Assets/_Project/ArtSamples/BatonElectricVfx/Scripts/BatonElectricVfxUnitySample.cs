using UnityEngine;

namespace Bellerophon.ArtSamples
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BatonElectricVfxUnitySample : MonoBehaviour
    {
        public enum EffectMode
        {
            ChargeReady,
            Discharge
        }

        [SerializeField] private EffectMode mode;
        [SerializeField] private LineRenderer[] haloLayers;
        [SerializeField] private LineRenderer[] deepLayers;
        [SerializeField] private LineRenderer[] blueLayers;
        [SerializeField] private LineRenderer[] glowLayers;
        [SerializeField] private LineRenderer[] coreLayers;
        [SerializeField] private float dischargeRangeMeters = 5f;

        private bool manualPreview;
        private float manualPreviewTime;
        private float runtimeTime;

        public EffectMode Mode => mode;

        public int GroupCount => Mathf.Min(
            LayerCount(haloLayers),
            Mathf.Min(
                LayerCount(deepLayers),
                Mathf.Min(
                    LayerCount(blueLayers),
                    Mathf.Min(
                        LayerCount(glowLayers),
                        LayerCount(coreLayers)))));

        public float DischargeRangeMeters => dischargeRangeMeters;

        private static int LayerCount(LineRenderer[] layers)
        {
            return layers != null ? layers.Length : 0;
        }

        private void OnEnable()
        {
            if (manualPreview)
            {
                Evaluate(manualPreviewTime);
                return;
            }

            runtimeTime = 0f;
            Evaluate(runtimeTime);
        }

        private void Update()
        {
            if (manualPreview)
            {
                return;
            }

            runtimeTime += Application.isPlaying
                ? Time.deltaTime
                : 1f / 30f;
            Evaluate(runtimeTime);
        }

        private void OnValidate()
        {
            dischargeRangeMeters = Mathf.Clamp(dischargeRangeMeters, 3f, 5f);
        }

        public void SetPreviewTime(float previewTime)
        {
            manualPreview = true;
            manualPreviewTime = Mathf.Max(0f, previewTime);
            Evaluate(manualPreviewTime);
        }

        public void ResumeAnimatedPreview()
        {
            manualPreview = false;
            runtimeTime = 0f;
            Evaluate(runtimeTime);
        }

        public void Evaluate(float previewTime)
        {
            if (mode == EffectMode.ChargeReady)
            {
                EvaluateCharge(previewTime);
            }
            else
            {
                EvaluateDischarge(previewTime);
            }
        }

        private void EvaluateCharge(float previewTime)
        {
            int groups = GroupCount;
            const int pointCount = 7;
            int continuityBeat = Mathf.FloorToInt(previewTime * 5.4f);
            int continuityGroup = groups > 0
                ? Mathf.Min(
                    groups - 1,
                    Mathf.FloorToInt(
                        Hash01(ComposeSeed(83, 17, 23, continuityBeat)) *
                        groups))
                : -1;
            for (int group = 0; group < groups; group++)
            {
                SetGroupPointCount(group, pointCount);
                float groupSeed = Hash01(group * 911 + 37);
                float baseY = Mathf.Lerp(
                    0.018f,
                    0.282f,
                    Hash01(group * 353 + 101));
                float span = Mathf.Lerp(
                    0.035f,
                    0.105f,
                    Hash01(group * 647 + 211));
                float direction = Hash01(group * 431 + 17) > 0.5f
                    ? 1f
                    : -1f;
                float baseAngle = groupSeed * Mathf.PI * 2f +
                    previewTime * direction * Mathf.Lerp(
                        2.4f,
                        5.8f,
                        Hash01(group * 283 + 73));
                float bend = Mathf.Lerp(
                    0.42f,
                    1.55f,
                    Hash01(group * 179 + 59)) * Mathf.PI;
                float visibility = EvaluateChargeVisibility(group, previewTime);
                if (group == continuityGroup)
                {
                    visibility = Mathf.Max(visibility, 0.72f);
                }

                SetGroupAlpha(group, visibility);

                for (int point = 0; point < pointCount; point++)
                {
                    float t = point / (float)(pointCount - 1);
                    float centered = t - 0.5f;
                    float angleNoise = AnimatedHashSigned(
                        group,
                        point,
                        1,
                        previewTime * 8f);
                    float heightNoise = AnimatedHashSigned(
                        group,
                        point,
                        2,
                        previewTime * 7f);
                    float radiusNoise = AnimatedHashSigned(
                        group,
                        point,
                        3,
                        previewTime * 9f);
                    float angle = baseAngle +
                        direction * centered * bend +
                        angleNoise * 0.48f;
                    float radius = 0.043f + radiusNoise * 0.010f;
                    float y = Mathf.Clamp(
                        baseY + centered * span + heightNoise * 0.014f,
                        0f,
                        0.30f);
                    SetGroupPoint(
                        group,
                        point,
                        new Vector3(
                            Mathf.Cos(angle) * radius,
                            y,
                            Mathf.Sin(angle) * radius));
                }
            }
        }

        private void EvaluateDischarge(float previewTime)
        {
            int groups = GroupCount;
            if (groups == 0)
            {
                return;
            }

            const int mainPointCount = 13;
            SetGroupPointCount(0, mainPointCount);
            SetGroupAlpha(
                0,
                0.88f + 0.12f * Mathf.Abs(Mathf.Sin(previewTime * 19f)));
            for (int point = 0; point < mainPointCount; point++)
            {
                float t = point / (float)(mainPointCount - 1);
                SetGroupPoint(
                    0,
                    point,
                    EvaluateMainPosition(t, previewTime));
            }

            int pathBranchCount = Mathf.Min(3, groups - 1);
            for (int branch = 0; branch < pathBranchCount; branch++)
            {
                int group = branch + 1;
                const int branchPointCount = 11;
                SetGroupPointCount(group, branchPointCount);
                SetGroupAlpha(
                    group,
                    0.70f + 0.24f * Mathf.Abs(
                        Mathf.Sin(previewTime * 23f + group * 2.7f)));
                for (int point = 0; point < branchPointCount; point++)
                {
                    float t = point / (float)(branchPointCount - 1);
                    float jitter = AnimatedHashSigned(
                        group,
                        point,
                        5,
                        previewTime * 9f);
                    SetGroupPoint(
                        group,
                        point,
                        EvaluateConvergingPathPosition(
                            branch,
                            point,
                            t,
                            jitter,
                            previewTime));
                }
            }

            for (int group = 4; group < groups; group++)
            {
                const int collectorPointCount = 4;
                SetGroupPointCount(group, collectorPointCount);
                int collectorIndex = group - 4;
                float angle = Mathf.Lerp(
                    -2.35f,
                    2.35f,
                    collectorIndex / 3f) +
                    AnimatedHashSigned(
                        group,
                        0,
                        7,
                        previewTime * 8f) * 0.16f;
                float length = 0.11f + collectorIndex * 0.012f;
                SetGroupAlpha(
                    group,
                    0.58f + 0.38f * Mathf.Abs(
                        Mathf.Sin(previewTime * 31f + group)));
                for (int point = 0; point < collectorPointCount; point++)
                {
                    float t = point / (float)(collectorPointCount - 1);
                    float crooked = AnimatedHashSigned(
                        group,
                        point,
                        8,
                        previewTime * 10f) * 0.018f * t;
                    SetGroupPoint(
                        group,
                        point,
                        new Vector3(
                            Mathf.Cos(angle) * length * t,
                            Mathf.Sin(angle) * length * t + crooked,
                            AnimatedHashSigned(
                                group,
                                point,
                                10,
                                previewTime * 9f) * 0.020f * t));
                }
            }
        }

        private Vector3 EvaluateMainPosition(float t, float previewTime)
        {
            if (t <= 0f)
            {
                return Vector3.zero;
            }

            if (t >= 1f)
            {
                return new Vector3(dischargeRangeMeters, 0f, 0f);
            }

            int segment = Mathf.RoundToInt(t * 12f);
            float envelope = Mathf.Sin(Mathf.PI * t);
            float amplitude = Mathf.Lerp(
                0.055f,
                0.165f,
                Hash01(segment * 733 + 41));
            float staticBend = Hash01(segment * 1297 + 307) * 2f - 1f;
            float animatedY = AnimatedHashSigned(
                29,
                segment,
                4,
                previewTime * 5.7f);
            float animatedZ = AnimatedHashSigned(
                31,
                segment,
                6,
                previewTime * 6.9f);
            float xJitter = AnimatedHashSigned(
                37,
                segment,
                12,
                previewTime * 4.8f) * 0.022f * envelope;
            float warpedT = Mathf.Clamp(t + xJitter, 0.001f, 0.999f);
            float y = (staticBend * amplitude + animatedY * 0.075f) *
                envelope;
            float z = (
                (Hash01(segment * 1877 + 509) * 2f - 1f) * 0.055f +
                animatedZ * 0.080f) * envelope;
            return new Vector3(dischargeRangeMeters * warpedT, y, z);
        }

        private static float EvaluateChargeVisibility(
            int group,
            float previewTime)
        {
            float interval = Mathf.Lerp(
                0.16f,
                0.54f,
                Hash01(group * 619 + 113));
            float phase = Hash01(group * 977 + 271) * 4.2f;
            float cycleTime = (previewTime + phase) / interval;
            int cycle = Mathf.FloorToInt(cycleTime);
            float localTime = cycleTime - cycle;
            float duty = Mathf.Lerp(
                0.24f,
                0.72f,
                Hash01(ComposeSeed(group, 0, 13, cycle)));
            float fade = Mathf.Min(0.12f, duty * 0.34f);
            float appear = Mathf.SmoothStep(0f, 1f, localTime / fade);
            float disappear = 1f - Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(duty - fade, duty, localTime));
            float gate = localTime < duty
                ? appear * disappear
                : 0f;
            float strength = Mathf.Lerp(
                0.70f,
                1f,
                Hash01(ComposeSeed(group, 0, 17, cycle)));
            float microFlicker = Mathf.Lerp(
                0.76f,
                1f,
                AnimatedHash01(group, 0, 19, previewTime * 17f));
            return gate * strength * microFlicker;
        }

        private Vector3 EvaluateConvergingPathPosition(
            int branch,
            int point,
            float t,
            float jitter,
            float previewTime)
        {
            if (t <= 0f)
            {
                return Vector3.zero;
            }

            if (t >= 1f)
            {
                return new Vector3(dischargeRangeMeters, 0f, 0f);
            }

            float envelope = Mathf.Sin(Mathf.PI * t);
            float staticY = Hash01(
                ComposeSeed(branch + 53, point, 25, 0)) * 2f - 1f;
            float staticZ = Hash01(
                ComposeSeed(branch + 59, point, 27, 0)) * 2f - 1f;
            float amplitudeY = Mathf.Lerp(
                0.075f,
                0.285f,
                Hash01(ComposeSeed(branch + 61, point, 29, 0)));
            float animatedY = AnimatedHashSigned(
                branch + 67,
                point,
                31,
                previewTime * (5.1f + branch * 0.83f));
            float animatedZ = AnimatedHashSigned(
                branch + 71,
                point,
                33,
                previewTime * (6.3f + branch * 0.71f));
            float xJitter = AnimatedHashSigned(
                branch + 73,
                point,
                35,
                previewTime * (4.4f + branch * 0.57f)) *
                0.028f * envelope;
            float warpedT = Mathf.Clamp(t + xJitter, 0.001f, 0.999f);
            float y = (
                staticY * amplitudeY +
                animatedY * 0.105f +
                jitter * 0.050f) * envelope;
            float z = (
                staticZ * 0.110f +
                animatedZ * 0.095f) * envelope;
            return new Vector3(dischargeRangeMeters * warpedT, y, z);
        }

        private static float AnimatedHash01(
            int group,
            int point,
            int channel,
            float scaledTime)
        {
            int frame = Mathf.FloorToInt(scaledTime);
            float blend = Mathf.SmoothStep(
                0f,
                1f,
                scaledTime - frame);
            float from = Hash01(ComposeSeed(group, point, channel, frame));
            float to = Hash01(ComposeSeed(group, point, channel, frame + 1));
            return Mathf.Lerp(from, to, blend);
        }

        private static float AnimatedHashSigned(
            int group,
            int point,
            int channel,
            float scaledTime)
        {
            return AnimatedHash01(group, point, channel, scaledTime) * 2f - 1f;
        }

        private static int ComposeSeed(
            int group,
            int point,
            int channel,
            int frame)
        {
            unchecked
            {
                return group * 73856093 ^
                    point * 19349663 ^
                    channel * 83492791 ^
                    frame * 26544357;
            }
        }

        private static float Hash01(int seed)
        {
            unchecked
            {
                uint value = (uint)seed;
                value ^= value >> 16;
                value *= 0x7feb352d;
                value ^= value >> 15;
                value *= 0x846ca68b;
                value ^= value >> 16;
                return (value & 0x00ffffff) / 16777215f;
            }
        }

        private void SetGroupPointCount(int group, int pointCount)
        {
            haloLayers[group].positionCount = pointCount;
            deepLayers[group].positionCount = pointCount;
            blueLayers[group].positionCount = pointCount;
            glowLayers[group].positionCount = pointCount;
            coreLayers[group].positionCount = pointCount;
        }

        private void SetGroupPoint(int group, int point, Vector3 position)
        {
            haloLayers[group].SetPosition(point, position);
            deepLayers[group].SetPosition(point, position);
            blueLayers[group].SetPosition(point, position);
            glowLayers[group].SetPosition(point, position);
            coreLayers[group].SetPosition(point, position);
        }

        private void SetGroupAlpha(int group, float alpha)
        {
            SetRendererAlpha(haloLayers[group], alpha * 0.55f);
            SetRendererAlpha(deepLayers[group], alpha * 0.86f);
            SetRendererAlpha(blueLayers[group], alpha * 0.90f);
            SetRendererAlpha(glowLayers[group], alpha * 0.96f);
            SetRendererAlpha(
                coreLayers[group],
                alpha <= 0.001f ? 0f : Mathf.Clamp01(alpha + 0.10f));
        }

        private static void SetRendererAlpha(LineRenderer renderer, float alpha)
        {
            Color start = renderer.startColor;
            Color end = renderer.endColor;
            start.a = alpha;
            end.a = alpha * 0.76f;
            renderer.startColor = start;
            renderer.endColor = end;
        }
    }
}
