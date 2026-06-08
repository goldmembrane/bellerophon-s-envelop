using System;

namespace Bellerophon.Core.Session
{
    public readonly struct GameSettingsState
    {
        public const int MinResolutionWidth = 640;
        public const int MinResolutionHeight = 360;
        public const float MinMouseSensitivity = 0.001f;

        public GameSettingsState(
            int resolutionWidth,
            int resolutionHeight,
            bool fullscreen,
            float masterVolume,
            float musicVolume,
            float effectsVolume,
            float mouseSensitivity,
            bool highContrastUi,
            bool reduceCameraShake)
        {
            ResolutionWidth = Math.Max(MinResolutionWidth, resolutionWidth);
            ResolutionHeight = Math.Max(MinResolutionHeight, resolutionHeight);
            Fullscreen = fullscreen;
            MasterVolume = Clamp01(masterVolume);
            MusicVolume = Clamp01(musicVolume);
            EffectsVolume = Clamp01(effectsVolume);
            MouseSensitivity = Math.Max(MinMouseSensitivity, mouseSensitivity);
            HighContrastUi = highContrastUi;
            ReduceCameraShake = reduceCameraShake;
        }

        public int ResolutionWidth { get; }

        public int ResolutionHeight { get; }

        public bool Fullscreen { get; }

        public float MasterVolume { get; }

        public float MusicVolume { get; }

        public float EffectsVolume { get; }

        public float MouseSensitivity { get; }

        public bool HighContrastUi { get; }

        public bool ReduceCameraShake { get; }

        public static GameSettingsState Default => new GameSettingsState(
            1920,
            1080,
            true,
            1f,
            1f,
            1f,
            0.12f,
            false,
            false);

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
