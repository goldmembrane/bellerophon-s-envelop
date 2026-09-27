using System;

namespace Bellerophon.Enemies.Parvum
{
    public enum ParvumBehaviour { Search, ApproachMetal, Consume, PursueAttacker, ApproachSpeaker, Bite, DestroySpeaker, Dead, RelocateRoom, AdvanceIntoRoom }

    // Separate from combat damage: sixty half-second bites remove exactly 500 durability.
    public static class ParvumGameplayRules
    {
        public const float MetalRange = 5f;
        public const float RoomEntryAdvanceDistance = 1f;
        public const float PursuitRange = 10f;
        public const float SpeakerRange = 20f;
        // Navigation and physical stepping share one limit; never enlarge scene floor geometry.
        public const float MaximumStepHeight = .32f;
        public const double FacilityDamagePerBite = 25d / 3d;
        public const int CargoBitesToConsume = 120; // 60 seconds of direct feeding, one bite every 0.5 seconds.
        public static int BiteDamage(bool shielded, bool biological) => shielded ? 1 : biological ? 6 : 3;
        public static float SlowDuration(bool shielded, bool biological) => shielded ? 0f : biological ? .8f : .4f;
    }

    // Per cargo, retained when feeding pauses. Capture initial remaining durability once,
    // rather than repeatedly taking a percentage of the ever-decreasing remainder.
    public sealed class ParvumCargoLedger
    {
        private double initialDurability;
        private double externalDamage;
        private float lastWrittenDurability;
        private int bites;
        public int Bites => bites;

        public float ConsumeBite(float currentDurability)
        {
            if (currentDurability <= 0) return 0;
            if (bites == 0)
                initialDurability = currentDurability;
            else
                externalDamage += lastWrittenDurability - currentDurability;
            bites++;
            double remaining = initialDurability * (1d - (double)bites / ParvumGameplayRules.CargoBitesToConsume) - externalDamage;
            lastWrittenDurability = (float)Math.Max(0d, Math.Min(1d, remaining));
            return lastWrittenDurability;
        }
    }

    // One ledger per physical part, not per attacker; interruptions do not reset accumulated feeding.
    public sealed class ParvumFacilityLedger
    {
        public double AccumulatedAttackSeconds { get; private set; }
        public bool HasDamagePart { get; private set; }
        private double damageRemainder;
        private double ongoingDamageTime;

        public int ConsumeBite()
        {
            damageRemainder += ParvumGameplayRules.FacilityDamagePerBite;
            int damage = (int)Math.Floor(damageRemainder + 1e-9d);
            damageRemainder -= damage;
            return damage;
        }

        public void AccumulateAttack(double seconds)
        {
            AccumulatedAttackSeconds += Math.Max(0, seconds);
            if (AccumulatedAttackSeconds > 5d + 1e-6d) HasDamagePart = true;
        }

        public int TickDamagePart(double seconds)
        {
            if (!HasDamagePart || seconds <= 0) return 0;
            ongoingDamageTime += seconds;
            int ticks = (int)Math.Floor((ongoingDamageTime + 1e-9d) / 3d);
            ongoingDamageTime -= ticks * 3d;
            return ticks * 10;
        }
    }

    public sealed class ParvumSlowWindow
    {
        public float Remaining { get; private set; }
        public void Clear() => Remaining=0;
        public bool TryApply(float duration)
        {
            if (Remaining > 0 || duration <= 0) return false;
            Remaining = duration;
            return true;
        }
        public void Tick(float delta) => Remaining = Math.Max(0, Remaining - Math.Max(0, delta));
    }
}
