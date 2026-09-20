using System;
using UnityEngine;

namespace Bellerophon.ArtSamples
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class StunTwistElectricArcUnitySample : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] cyanHalos = Array.Empty<LineRenderer>();
        [SerializeField] private LineRenderer[] cyanGlows = Array.Empty<LineRenderer>();
        [SerializeField] private LineRenderer[] whiteCores = Array.Empty<LineRenderer>();
        [SerializeField] private Transform torsoAnchor;
        [SerializeField] private Quaternion torsoOrientationOffset = Quaternion.identity;
        [SerializeField] private Vector3 torsoCenter = new Vector3(0f, 1.1f, 0f);
        [SerializeField] private float torsoHeight = 0.9f;
        [SerializeField] private float torsoRadiusX = 0.38f;
        [SerializeField] private float torsoRadiusZ = 0.27f;

        private bool fixedPreview;
        private float fixedPreviewTime;

        public void SetPreviewTime(float previewTime)
        {
            fixedPreview = true;
            fixedPreviewTime = previewTime;
            Evaluate(previewTime);
        }

        public void ResumeAnimatedPreview()
        {
            fixedPreview = false;
        }

        private void OnEnable()
        {
            ResolveTorsoAnchor();
            Evaluate(fixedPreview ? fixedPreviewTime : Time.realtimeSinceStartup);
        }

        private void Update()
        {
            ResolveTorsoAnchor();
            Evaluate(fixedPreview ? fixedPreviewTime : Time.realtimeSinceStartup);
        }

        private void ResolveTorsoAnchor()
        {
            if (torsoAnchor != null)
                return;
            Animator animator = GetComponentInParent<Animator>();
            if (animator == null)
                return;
            if (animator.isHuman)
            {
                torsoAnchor = animator.GetBoneTransform(HumanBodyBones.Chest) ??
                    animator.GetBoneTransform(HumanBodyBones.Spine);
            }
            if (torsoAnchor == null)
            {
                Transform[] candidates = animator.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in candidates)
                {
                    if (candidate.name != "Spine")
                        continue;
                    bool hasLeftShoulder = false;
                    bool hasRightShoulder = false;
                    foreach (Transform child in candidate)
                    {
                        hasLeftShoulder |= child.name == "LeftShoulder";
                        hasRightShoulder |= child.name == "RightShoulder";
                    }
                    if (hasLeftShoulder && hasRightShoulder)
                    {
                        torsoAnchor = candidate;
                        break;
                    }
                }
            }
            if (torsoAnchor != null)
            {
                torsoOrientationOffset =
                    Quaternion.Inverse(torsoAnchor.rotation) *
                    animator.transform.rotation;
            }
        }

        private void Evaluate(float previewTime)
        {
            ResolveTorsoAnchor();
            int groups = Mathf.Min(
                cyanHalos == null ? 0 : cyanHalos.Length,
                cyanGlows == null ? 0 : cyanGlows.Length,
                whiteCores == null ? 0 : whiteCores.Length);
            if (groups == 0)
                return;

            int forcedVisible = Mathf.Abs(Mathf.FloorToInt(previewTime * 4.3f)) % groups;
            for (int group = 0; group < groups; group++)
            {
                const int pointCount = 9;
                SetPointCount(group, pointCount);
                float visibility = EvaluateVisibility(group, previewTime);
                if (group == forcedVisible)
                    visibility = Mathf.Max(visibility, 0.86f);

                float orbitSpeed = Mathf.Lerp(0.58f, 1.08f, Hash01(group * 811 + 31));
                float direction = (group & 1) == 0 ? 1f : -1f;
                float baseAngle = group * Mathf.PI * 2f / groups +
                    previewTime * orbitSpeed * direction;
                int shapeFrame = Mathf.FloorToInt(previewTime * (5.2f + group * 0.17f));
                float band = Hash01(group * 419 + shapeFrame * 1171 + 79);
                float centerY = torsoCenter.y + Mathf.Lerp(
                    -torsoHeight * 0.44f,
                    torsoHeight * 0.44f,
                    band);
                float sweep = Mathf.Lerp(0.62f, 1.42f, Hash01(group * 613 + shapeFrame * 929 + 17));
                float rise = Mathf.Lerp(-0.14f, 0.14f, Hash01(group * 991 + shapeFrame * 443 + 53));

                for (int point = 0; point < pointCount; point++)
                {
                    float t = point / (float)(pointCount - 1);
                    float envelope = Mathf.Sin(Mathf.PI * t);
                    float jitterAngle = HashSigned(
                        group * 1009 + point * 313 + shapeFrame * 1877 + 101) *
                        0.15f * envelope;
                    float jitterRadius = HashSigned(
                        group * 1297 + point * 587 + shapeFrame * 733 + 211) *
                        0.075f * envelope;
                    float jitterY = HashSigned(
                        group * 1663 + point * 719 + shapeFrame * 421 + 307) *
                        0.045f * envelope;
                    float angle = baseAngle + (t - 0.5f) * sweep + jitterAngle;
                    float radiusScale = 1f + jitterRadius;
                    Vector3 position = new Vector3(
                        torsoCenter.x + Mathf.Cos(angle) * torsoRadiusX * radiusScale,
                        centerY + (t - 0.5f) * rise + jitterY,
                        torsoCenter.z + Mathf.Sin(angle) * torsoRadiusZ * radiusScale);
                    SetPoint(group, point, position);
                }

                SetAlpha(group, visibility);
            }
        }

        private static float EvaluateVisibility(int group, float previewTime)
        {
            float interval = Mathf.Lerp(0.22f, 0.62f, Hash01(group * 557 + 47));
            float shifted = previewTime / interval + Hash01(group * 907 + 131) * 6f;
            int cycle = Mathf.FloorToInt(shifted);
            float local = shifted - cycle;
            float duty = Mathf.Lerp(0.34f, 0.76f, Hash01(group * 677 + cycle * 1223 + 197));
            if (local >= duty)
                return 0f;
            float fade = Mathf.Min(0.12f, duty * 0.28f);
            float enter = Mathf.SmoothStep(0f, 1f, local / Mathf.Max(0.001f, fade));
            float exit = 1f - Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(duty - fade, duty, local));
            float micro = Mathf.Lerp(
                0.72f,
                1f,
                Hash01(group * 829 + Mathf.FloorToInt(previewTime * 23f) * 617 + 263));
            return enter * exit * micro;
        }

        private void SetPointCount(int group, int count)
        {
            cyanHalos[group].positionCount = count;
            cyanGlows[group].positionCount = count;
            whiteCores[group].positionCount = count;
        }

        private void SetPoint(int group, int point, Vector3 position)
        {
            Transform anchor = torsoAnchor != null ? torsoAnchor : transform;
            Quaternion orientation = anchor.rotation * torsoOrientationOffset;
            Vector3 worldPosition = anchor.position + orientation * position;
            cyanHalos[group].SetPosition(point, worldPosition);
            cyanGlows[group].SetPosition(point, worldPosition);
            whiteCores[group].SetPosition(point, worldPosition);
        }

        private void SetAlpha(int group, float alpha)
        {
            SetRendererAlpha(cyanHalos[group], alpha * 0.42f);
            SetRendererAlpha(cyanGlows[group], alpha * 0.86f);
            SetRendererAlpha(
                whiteCores[group],
                alpha <= 0.001f ? 0f : Mathf.Clamp01(alpha + 0.12f));
        }

        private static void SetRendererAlpha(LineRenderer renderer, float alpha)
        {
            Color start = renderer.startColor;
            Color end = renderer.endColor;
            start.a = alpha;
            end.a = alpha * 0.74f;
            renderer.startColor = start;
            renderer.endColor = end;
        }

        private static float HashSigned(int seed) => Hash01(seed) * 2f - 1f;

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
    }
}
