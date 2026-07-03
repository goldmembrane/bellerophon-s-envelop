using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bellerophon.Enemies.Fuga
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class FugaAnimationReviewPlaybackDriver : MonoBehaviour
    {
        [SerializeField] private AnimationClip clip;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playInEditMode = true;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private float startOffsetSeconds;

        private float elapsedSeconds;

#if UNITY_EDITOR
        private double lastEditorTime;
#endif

        public AnimationClip Clip => clip;
        public bool Loop => loop;
        public bool PlayInEditMode => playInEditMode;
        public float PlaybackSpeed => playbackSpeed;
        public float StartOffsetSeconds => startOffsetSeconds;

        public void Configure(
            AnimationClip configuredClip,
            bool configuredLoop,
            float configuredStartOffsetSeconds,
            float configuredPlaybackSpeed)
        {
            clip = configuredClip;
            loop = configuredLoop;
            startOffsetSeconds = Mathf.Max(0f, configuredStartOffsetSeconds);
            playbackSpeed = Mathf.Max(0.01f, configuredPlaybackSpeed);
            elapsedSeconds = startOffsetSeconds;
#if UNITY_EDITOR
            lastEditorTime = EditorApplication.timeSinceStartup;
#endif
            SampleCurrentPose();
        }

        private void OnEnable()
        {
            elapsedSeconds = Mathf.Max(0f, startOffsetSeconds);
#if UNITY_EDITOR
            lastEditorTime = EditorApplication.timeSinceStartup;
#endif
            SampleCurrentPose();
        }

        private void Update()
        {
            if (!Application.isPlaying && !playInEditMode)
            {
                return;
            }

            elapsedSeconds += CalculateDeltaSeconds() * playbackSpeed;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
#endif
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !playInEditMode)
            {
                return;
            }

            SampleCurrentPose();
        }

        private float CalculateDeltaSeconds()
        {
            if (Application.isPlaying)
            {
                return Time.deltaTime;
            }

#if UNITY_EDITOR
            var now = EditorApplication.timeSinceStartup;
            var delta = Mathf.Clamp((float)(now - lastEditorTime), 0f, 0.1f);
            lastEditorTime = now;
            return delta;
#else
            return 0f;
#endif
        }

        private void SampleCurrentPose()
        {
            if (clip == null)
            {
                return;
            }

            var duration = Mathf.Max(clip.length, 0.0001f);
            var sampleTime = loop ? Mathf.Repeat(elapsedSeconds, duration) : Mathf.Min(elapsedSeconds, duration);
            clip.SampleAnimation(gameObject, sampleTime);
        }
    }
}
