using System;

namespace Bellerophon.Core.Session
{
    public enum CargoGrade
    {
        Common,
        Rare,
        Premium
    }

    public enum ContractType
    {
        Association,
        Private,
        Special
    }

    public enum ContractDifficulty
    {
        Intro,
        VeryEasy,
        Easy,
        Normal,
        Hard,
        VeryHard,
        Master
    }

    public readonly struct CargoState
    {
        public CargoState(
            CargoGrade grade,
            int sizeUnits,
            int baseValue,
            float durabilityPercent,
            bool isPersonalCargo)
        {
            if (sizeUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeUnits), "Cargo size must be positive.");
            }

            if (baseValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseValue), "Cargo value cannot be negative.");
            }

            Grade = grade;
            SizeUnits = sizeUnits;
            BaseValue = baseValue;
            DurabilityPercent = Clamp01(durabilityPercent);
            IsPersonalCargo = isPersonalCargo;
        }

        public CargoGrade Grade { get; }

        public int SizeUnits { get; }

        public int BaseValue { get; }

        public float DurabilityPercent { get; }

        public float LossPercent => 1f - DurabilityPercent;

        public bool IsPersonalCargo { get; }

        public CargoState WithDurabilityPercent(float durabilityPercent)
        {
            return new CargoState(Grade, SizeUnits, BaseValue, durabilityPercent, IsPersonalCargo);
        }

        public CargoState WithDamagePercent(float damagePercent)
        {
            if (damagePercent < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damagePercent), "Cargo damage cannot be negative.");
            }

            return WithDurabilityPercent(DurabilityPercent - damagePercent);
        }

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
