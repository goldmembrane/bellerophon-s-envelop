using System;

namespace Bellerophon.Core.Session
{
    public enum AlienLifeformKind
    {
        None,
        Cantabile,
        ConSpirito,
        Accelerando,
        Grave,
        Smorzando,
        Ostinato,
        Dolore
    }

    public enum AlienLifeformBehaviorKind
    {
        None,
        SoundHunter,
        RandomCharger,
        AcceleratingHunter,
        StatusAmplifier,
        AbsorbingLiquid,
        FrenzyRestCycle,
        FrameExecutioner
    }

    public enum AlienLifeformBehaviorPhase
    {
        None,
        Tracking,
        Charging,
        Resting,
        Liquid,
        Zombie,
        Roaring,
        Frenzy,
        Recovering
    }

    public enum AlienLifeformSpecialEffectKind
    {
        None,
        SonicStop,
        EngineResonance,
        Charge,
        Acceleration,
        StatusAmplification,
        AbsorptionStop,
        SmorzandoZombieTransform,
        Frenzy,
        RestRecovery,
        ExecutionPull
    }

    public readonly struct AlienLifeformProfile
    {
        public AlienLifeformProfile(
            AlienLifeformKind kind,
            IntruderDefinition intruderDefinition,
            AlienLifeformBehaviorKind behaviorKind,
            int directDamage,
            bool canTargetLoudestSound = false,
            float sonicStopRadiusMeters = 0f,
            float sonicStopDurationSeconds = 0f,
            bool canResonateWithEngineRoom = false,
            int engineRoomResonanceDamage = 0,
            int chargeDamage = 0,
            float chargeDistanceMeters = 0f,
            float chargeDurationSeconds = 0f,
            float restDurationSeconds = 0f,
            bool hasNoPriorityTarget = false,
            float accelerationStartSpeed = 0f,
            float accelerationMaxSpeed = 0f,
            float accelerationStartAttackDelaySeconds = 0f,
            float accelerationMinimumAttackDelaySeconds = 0f,
            float accelerationAttackMovementSpeed = 0f,
            float accelerationResetSightLossSeconds = 0f,
            float statusDamageMultiplier = 1f,
            float statusDurationMultiplier = 1f,
            float attackWindupSeconds = 0f,
            int liquidDamagePerSecond = 0,
            int liquidDamageReductionPercent = 0,
            int absorptionStacksPerSecond = 0,
            int movementSlowPercentPerStack = 0,
            int absorptionStopStackThreshold = 0,
            float absorptionStopDurationSeconds = 0f,
            int absorptionStopDamage = 0,
            int zombieHealth = 0,
            float zombieSpeed = 0f,
            float zombieChargeSpeed = 0f,
            float zombieChargeDelaySeconds = 0f,
            int zombieSelfDestructDamage = 0,
            int zombieShieldDamageMultiplier = 1,
            float zombieSelfDestructRadiusMeters = 0f,
            float frenzyHealthThresholdPercent = 0f,
            float roarDurationSeconds = 0f,
            int roarDamageReductionPercent = 0,
            float frenzyDurationSeconds = 0f,
            float frenzyMovementSpeed = 0f,
            int frenzyDamage = 0,
            float frenzyAttackDelaySeconds = 0f,
            float recoveryRestDurationSeconds = 0f,
            int recoveryDamageTakenBonusPercent = 0,
            float recoveryHealPercent = 0f,
            float executionHealthThresholdPercent = 0f)
        {
            if (kind == AlienLifeformKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Alien lifeform profile requires a concrete kind.");
            }

            if (behaviorKind == AlienLifeformBehaviorKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(behaviorKind), "Alien lifeform profile requires a behavior kind.");
            }

            if (intruderDefinition.Faction != IntruderFaction.AlienLifeform)
            {
                throw new ArgumentOutOfRangeException(nameof(intruderDefinition), "Alien lifeform profile requires an alien lifeform definition.");
            }

            Kind = kind;
            IntruderDefinition = intruderDefinition;
            BehaviorKind = behaviorKind;
            DirectDamage = Math.Max(0, directDamage);
            CanTargetLoudestSound = canTargetLoudestSound;
            SonicStopRadiusMeters = Math.Max(0f, sonicStopRadiusMeters);
            SonicStopDurationSeconds = Math.Max(0f, sonicStopDurationSeconds);
            CanResonateWithEngineRoom = canResonateWithEngineRoom;
            EngineRoomResonanceDamage = Math.Max(0, engineRoomResonanceDamage);
            ChargeDamage = Math.Max(0, chargeDamage);
            ChargeDistanceMeters = Math.Max(0f, chargeDistanceMeters);
            ChargeDurationSeconds = Math.Max(0f, chargeDurationSeconds);
            RestDurationSeconds = Math.Max(0f, restDurationSeconds);
            HasNoPriorityTarget = hasNoPriorityTarget;
            AccelerationStartSpeed = Math.Max(0f, accelerationStartSpeed);
            AccelerationMaxSpeed = Math.Max(0f, accelerationMaxSpeed);
            AccelerationStartAttackDelaySeconds = Math.Max(0f, accelerationStartAttackDelaySeconds);
            AccelerationMinimumAttackDelaySeconds = Math.Max(0f, accelerationMinimumAttackDelaySeconds);
            AccelerationAttackMovementSpeed = Math.Max(0f, accelerationAttackMovementSpeed);
            AccelerationResetSightLossSeconds = Math.Max(0f, accelerationResetSightLossSeconds);
            StatusDamageMultiplier = statusDamageMultiplier <= 0f ? 1f : statusDamageMultiplier;
            StatusDurationMultiplier = statusDurationMultiplier <= 0f ? 1f : statusDurationMultiplier;
            AttackWindupSeconds = Math.Max(0f, attackWindupSeconds);
            LiquidDamagePerSecond = Math.Max(0, liquidDamagePerSecond);
            LiquidDamageReductionPercent = ClampPercent(liquidDamageReductionPercent);
            AbsorptionStacksPerSecond = Math.Max(0, absorptionStacksPerSecond);
            MovementSlowPercentPerStack = ClampPercent(movementSlowPercentPerStack);
            AbsorptionStopStackThreshold = Math.Max(0, absorptionStopStackThreshold);
            AbsorptionStopDurationSeconds = Math.Max(0f, absorptionStopDurationSeconds);
            AbsorptionStopDamage = Math.Max(0, absorptionStopDamage);
            ZombieHealth = Math.Max(0, zombieHealth);
            ZombieSpeed = Math.Max(0f, zombieSpeed);
            ZombieChargeSpeed = Math.Max(0f, zombieChargeSpeed);
            ZombieChargeDelaySeconds = Math.Max(0f, zombieChargeDelaySeconds);
            ZombieSelfDestructDamage = Math.Max(0, zombieSelfDestructDamage);
            ZombieShieldDamageMultiplier = Math.Max(1, zombieShieldDamageMultiplier);
            ZombieSelfDestructRadiusMeters = Math.Max(0f, zombieSelfDestructRadiusMeters);
            FrenzyHealthThresholdPercent = Clamp01(frenzyHealthThresholdPercent);
            RoarDurationSeconds = Math.Max(0f, roarDurationSeconds);
            RoarDamageReductionPercent = ClampPercent(roarDamageReductionPercent);
            FrenzyDurationSeconds = Math.Max(0f, frenzyDurationSeconds);
            FrenzyMovementSpeed = Math.Max(0f, frenzyMovementSpeed);
            FrenzyDamage = Math.Max(0, frenzyDamage);
            FrenzyAttackDelaySeconds = Math.Max(0f, frenzyAttackDelaySeconds);
            RecoveryRestDurationSeconds = Math.Max(0f, recoveryRestDurationSeconds);
            RecoveryDamageTakenBonusPercent = Math.Max(0, recoveryDamageTakenBonusPercent);
            RecoveryHealPercent = Clamp01(recoveryHealPercent);
            ExecutionHealthThresholdPercent = Clamp01(executionHealthThresholdPercent);
        }

        public AlienLifeformKind Kind { get; }

        public IntruderDefinition IntruderDefinition { get; }

        public AlienLifeformBehaviorKind BehaviorKind { get; }

        public bool CanBeExternallyRepelled => true;

        public int ExternalIntrusionObjectHealth => AlienLifeformRules.ExternalIntrusionObjectHealth;

        public int DirectDamage { get; }

        public bool CanTargetLoudestSound { get; }

        public float SonicStopRadiusMeters { get; }

        public float SonicStopDurationSeconds { get; }

        public bool CanResonateWithEngineRoom { get; }

        public int EngineRoomResonanceDamage { get; }

        public int ChargeDamage { get; }

        public float ChargeDistanceMeters { get; }

        public float ChargeDurationSeconds { get; }

        public float RestDurationSeconds { get; }

        public bool HasNoPriorityTarget { get; }

        public float AccelerationStartSpeed { get; }

        public float AccelerationMaxSpeed { get; }

        public float AccelerationStartAttackDelaySeconds { get; }

        public float AccelerationMinimumAttackDelaySeconds { get; }

        public float AccelerationAttackMovementSpeed { get; }

        public float AccelerationResetSightLossSeconds { get; }

        public float StatusDamageMultiplier { get; }

        public float StatusDurationMultiplier { get; }

        public float AttackWindupSeconds { get; }

        public int LiquidDamagePerSecond { get; }

        public int LiquidDamageReductionPercent { get; }

        public int AbsorptionStacksPerSecond { get; }

        public int MovementSlowPercentPerStack { get; }

        public int AbsorptionStopStackThreshold { get; }

        public float AbsorptionStopDurationSeconds { get; }

        public int AbsorptionStopDamage { get; }

        public int ZombieHealth { get; }

        public float ZombieSpeed { get; }

        public float ZombieChargeSpeed { get; }

        public float ZombieChargeDelaySeconds { get; }

        public int ZombieSelfDestructDamage { get; }

        public int ZombieShieldDamageMultiplier { get; }

        public float ZombieSelfDestructRadiusMeters { get; }

        public float FrenzyHealthThresholdPercent { get; }

        public float RoarDurationSeconds { get; }

        public int RoarDamageReductionPercent { get; }

        public float FrenzyDurationSeconds { get; }

        public float FrenzyMovementSpeed { get; }

        public int FrenzyDamage { get; }

        public float FrenzyAttackDelaySeconds { get; }

        public float RecoveryRestDurationSeconds { get; }

        public int RecoveryDamageTakenBonusPercent { get; }

        public float RecoveryHealPercent { get; }

        public float ExecutionHealthThresholdPercent { get; }

        public AlienLifeformSpecialEffectKind PrimarySpecialEffectKind
        {
            get
            {
                switch (BehaviorKind)
                {
                    case AlienLifeformBehaviorKind.SoundHunter:
                        return AlienLifeformSpecialEffectKind.SonicStop;
                    case AlienLifeformBehaviorKind.RandomCharger:
                        return AlienLifeformSpecialEffectKind.Charge;
                    case AlienLifeformBehaviorKind.AcceleratingHunter:
                        return AlienLifeformSpecialEffectKind.Acceleration;
                    case AlienLifeformBehaviorKind.StatusAmplifier:
                        return AlienLifeformSpecialEffectKind.StatusAmplification;
                    case AlienLifeformBehaviorKind.AbsorbingLiquid:
                        return AlienLifeformSpecialEffectKind.AbsorptionStop;
                    case AlienLifeformBehaviorKind.FrenzyRestCycle:
                        return AlienLifeformSpecialEffectKind.Frenzy;
                    case AlienLifeformBehaviorKind.FrameExecutioner:
                        return AlienLifeformSpecialEffectKind.ExecutionPull;
                    default:
                        return AlienLifeformSpecialEffectKind.None;
                }
            }
        }

        private static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
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

    public readonly struct AlienLifeformState
    {
        private AlienLifeformState(
            AlienLifeformKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder,
            AlienLifeformBehaviorPhase phase,
            float elapsedSeconds,
            float phaseElapsedSeconds,
            float attackAccumulatorSeconds,
            int appliedAttackCount,
            int totalRoomDamageApplied,
            int totalPlayerDamageApplied,
            int absorptionStack)
        {
            Kind = kind;
            Definition = definition;
            Attempt = attempt;
            Intruder = intruder;
            Phase = phase;
            ElapsedSeconds = Math.Max(0f, elapsedSeconds);
            PhaseElapsedSeconds = Math.Max(0f, phaseElapsedSeconds);
            AttackAccumulatorSeconds = Math.Max(0f, attackAccumulatorSeconds);
            AppliedAttackCount = Math.Max(0, appliedAttackCount);
            TotalRoomDamageApplied = Math.Max(0, totalRoomDamageApplied);
            TotalPlayerDamageApplied = Math.Max(0, totalPlayerDamageApplied);
            AbsorptionStack = Math.Max(0, absorptionStack);
        }

        public AlienLifeformKind Kind { get; }

        public IntruderDefinition Definition { get; }

        public IntrusionAttemptState Attempt { get; }

        public IntruderEntityState Intruder { get; }

        public AlienLifeformBehaviorPhase Phase { get; }

        public float ElapsedSeconds { get; }

        public float PhaseElapsedSeconds { get; }

        public float AttackAccumulatorSeconds { get; }

        public int AppliedAttackCount { get; }

        public int TotalRoomDamageApplied { get; }

        public int TotalPlayerDamageApplied { get; }

        public int AbsorptionStack { get; }

        public bool IsActive => Kind != AlienLifeformKind.None && Intruder.IsActive;

        public bool IsResolved => Kind != AlienLifeformKind.None && Intruder.IsResolved;

        public ShipRoomId TargetRoom => Intruder.TargetRoom;

        public static AlienLifeformState None => new AlienLifeformState(
            AlienLifeformKind.None,
            default,
            IntrusionAttemptState.None,
            IntruderEntityState.None,
            AlienLifeformBehaviorPhase.None,
            0f,
            0f,
            0f,
            0,
            0,
            0,
            0);

        public static AlienLifeformState Start(
            AlienLifeformKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder,
            AlienLifeformBehaviorPhase phase)
        {
            if (kind == AlienLifeformKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Alien lifeform state requires a concrete kind.");
            }

            if (!intruder.IsActive)
            {
                throw new ArgumentException("Alien lifeform entity must be active.", nameof(intruder));
            }

            if (phase == AlienLifeformBehaviorPhase.None)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), "Alien lifeform state requires a concrete behavior phase.");
            }

            return new AlienLifeformState(
                kind,
                definition,
                attempt,
                intruder,
                phase,
                0f,
                0f,
                0f,
                0,
                0,
                0,
                0);
        }

        public AlienLifeformState WithProgress(
            IntruderEntityState intruder,
            AlienLifeformBehaviorPhase phase,
            float elapsedSeconds,
            float phaseElapsedSeconds,
            float attackAccumulatorSeconds,
            int appliedAttackCount,
            int totalRoomDamageApplied,
            int totalPlayerDamageApplied,
            int absorptionStack)
        {
            return new AlienLifeformState(
                Kind,
                Definition,
                Attempt,
                intruder,
                phase,
                elapsedSeconds,
                phaseElapsedSeconds,
                attackAccumulatorSeconds,
                appliedAttackCount,
                totalRoomDamageApplied,
                totalPlayerDamageApplied,
                absorptionStack);
        }

        public AlienLifeformState WithIntruder(IntruderEntityState intruder)
        {
            return new AlienLifeformState(
                Kind,
                Definition,
                Attempt,
                intruder,
                Phase,
                ElapsedSeconds,
                PhaseElapsedSeconds,
                AttackAccumulatorSeconds,
                AppliedAttackCount,
                TotalRoomDamageApplied,
                TotalPlayerDamageApplied,
                AbsorptionStack);
        }
    }

    public readonly struct AlienLifeformTickResult
    {
        public AlienLifeformTickResult(
            AlienLifeformState state,
            ShipState ship,
            int attackCount,
            int roomDamageApplied,
            int playerDamageApplied,
            AlienLifeformSpecialEffectKind specialEffectKind = AlienLifeformSpecialEffectKind.None,
            CombatStatusEffectApplication statusEffectToApply = default,
            float effectiveMovementSpeed = 0f,
            float effectiveAttackDelaySeconds = 0f,
            int absorptionStacksApplied = 0,
            int movementSlowPercentApplied = 0,
            bool engineRoomResonanceApplied = false,
            bool transformedToZombie = false,
            bool executedTarget = false,
            bool resting = false,
            bool frenzyActive = false)
        {
            State = state;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            AttackCount = Math.Max(0, attackCount);
            RoomDamageApplied = Math.Max(0, roomDamageApplied);
            PlayerDamageApplied = Math.Max(0, playerDamageApplied);
            SpecialEffectKind = specialEffectKind;
            StatusEffectToApply = statusEffectToApply;
            EffectiveMovementSpeed = Math.Max(0f, effectiveMovementSpeed);
            EffectiveAttackDelaySeconds = Math.Max(0f, effectiveAttackDelaySeconds);
            AbsorptionStacksApplied = Math.Max(0, absorptionStacksApplied);
            MovementSlowPercentApplied = ClampPercent(movementSlowPercentApplied);
            EngineRoomResonanceApplied = engineRoomResonanceApplied;
            TransformedToZombie = transformedToZombie;
            ExecutedTarget = executedTarget;
            Resting = resting;
            FrenzyActive = frenzyActive;
        }

        public AlienLifeformState State { get; }

        public ShipState Ship { get; }

        public int AttackCount { get; }

        public int RoomDamageApplied { get; }

        public int PlayerDamageApplied { get; }

        public AlienLifeformSpecialEffectKind SpecialEffectKind { get; }

        public CombatStatusEffectApplication StatusEffectToApply { get; }

        public float EffectiveMovementSpeed { get; }

        public float EffectiveAttackDelaySeconds { get; }

        public int AbsorptionStacksApplied { get; }

        public int MovementSlowPercentApplied { get; }

        public bool EngineRoomResonanceApplied { get; }

        public bool TransformedToZombie { get; }

        public bool ExecutedTarget { get; }

        public bool Resting { get; }

        public bool FrenzyActive { get; }

        public bool AppliedDamage => RoomDamageApplied > 0 || PlayerDamageApplied > 0;

        public bool AppliedSpecialEffect => SpecialEffectKind != AlienLifeformSpecialEffectKind.None ||
                                            StatusEffectToApply.HasEffect ||
                                            EngineRoomResonanceApplied;

        private static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }

    public static class AlienLifeformRules
    {
        public const int ExternalIntrusionObjectHealth = 350;
        public const string CantabileDefinitionId = "alien-cantabile";
        public const string ConSpiritoDefinitionId = "alien-con-spirito";
        public const string AccelerandoDefinitionId = "alien-accelerando";
        public const string GraveDefinitionId = "alien-grave";
        public const string SmorzandoDefinitionId = "alien-smorzando";
        public const string OstinatoDefinitionId = "alien-ostinato";
        public const string DoloreDefinitionId = "alien-dolore";

        public const int CantabileHealth = 70;
        public const float CantabileMovementSpeed = 2f;
        public const float CantabileAttackRange = 1f;
        public const float CantabileAttackDelaySeconds = 1f;
        public const int CantabileDamage = 20;
        public const float CantabileSonicStopRadiusMeters = 3f;
        public const float CantabileSonicStopDurationSeconds = 1f;

        public const int ConSpiritoHealth = 80;
        public const float ConSpiritoMovementSpeed = 2f;
        public const int ConSpiritoChargeDamage = 40;
        public const float ConSpiritoChargeDistanceMeters = 10f;
        public const float ConSpiritoChargeDurationSeconds = 4f;
        public const float ConSpiritoRestDurationSeconds = 2f;

        public const int AccelerandoHealth = 100;
        public const float AccelerandoStartMovementSpeed = 1f;
        public const float AccelerandoMaximumMovementSpeed = 5f;
        public const int AccelerandoDamage = 10;
        public const float AccelerandoStartAttackDelaySeconds = 1.5f;
        public const float AccelerandoMinimumAttackDelaySeconds = 0.5f;
        public const float AccelerandoAttackRange = 2f;
        public const float AccelerandoAttackMovementSpeed = 2.5f;
        public const float AccelerandoSightResetSeconds = 5f;
        public const float AccelerandoRampDurationSeconds = 10f;

        public const int GraveHealth = 300;
        public const float GraveMovementSpeed = 1.5f;
        public const float GraveAttackRange = 4f;
        public const int GraveDamage = 35;
        public const float GraveAttackWindupSeconds = 3f;
        public const float GraveAttackDelaySeconds = 2f;
        public const float GraveStatusDamageMultiplier = 1.5f;
        public const float GraveStatusDurationMultiplier = 2f;

        public const int SmorzandoHealth = 160;
        public const int SmorzandoLiquidDamagePerSecond = 5;
        public const int SmorzandoLiquidDamageReductionPercent = 90;
        public const int SmorzandoAbsorptionStacksPerSecond = 1;
        public const int SmorzandoMoveSlowPercentPerStack = 10;
        public const int SmorzandoStopStackThreshold = 10;
        public const float SmorzandoStopDurationSeconds = 1f;
        public const int SmorzandoStopDamage = 10;
        public const int SmorzandoZombieHealth = 1;
        public const float SmorzandoZombieMovementSpeed = 1f;
        public const float SmorzandoZombieChargeDelaySeconds = 1f;
        public const float SmorzandoZombieChargeSpeed = 5f;
        public const int SmorzandoZombieSelfDestructDamage = 140;
        public const int SmorzandoZombieShieldDamageMultiplier = 2;
        public const float SmorzandoZombieSelfDestructRadiusMeters = 2f;

        public const int OstinatoHealth = 110;
        public const float OstinatoMovementSpeed = 2.5f;
        public const int OstinatoDamage = 20;
        public const float OstinatoAttackDelaySeconds = 2.5f;
        public const float OstinatoAttackRange = 3f;
        public const float OstinatoFrenzyHealthThresholdPercent = 0.5f;
        public const float OstinatoRoarDurationSeconds = 2f;
        public const int OstinatoRoarDamageReductionPercent = 70;
        public const float OstinatoFrenzyDurationSeconds = 10f;
        public const float OstinatoFrenzyMovementSpeed = 3.8f;
        public const int OstinatoFrenzyDamage = 25;
        public const float OstinatoFrenzyAttackDelaySeconds = 1.5f;
        public const float OstinatoRecoveryRestDurationSeconds = 5f;
        public const int OstinatoRecoveryDamageTakenBonusPercent = 10;
        public const float OstinatoRecoveryHealPercent = 0.4f;

        public const int DoloreHealth = 100;
        public const float DoloreMovementSpeed = 1.5f;
        public const float DoloreAttackRange = 1f;
        public const int DoloreDamage = 20;
        public const float DoloreAttackDelaySeconds = 3f;
        public const float DoloreExecutionHealthThresholdPercent = 0.4f;

        private static readonly AlienLifeformKind[] SourceKindOrder =
        {
            AlienLifeformKind.Cantabile,
            AlienLifeformKind.ConSpirito,
            AlienLifeformKind.Accelerando,
            AlienLifeformKind.Grave,
            AlienLifeformKind.Smorzando,
            AlienLifeformKind.Ostinato,
            AlienLifeformKind.Dolore
        };

        private static readonly ShipRoomId[] AlienTargetRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Armory,
            ShipRoomId.CargoHold,
            ShipRoomId.SupplyRoom
        };

        public static AlienLifeformKind[] SourceAlienLifeformKinds => (AlienLifeformKind[])SourceKindOrder.Clone();

        public static AlienLifeformProfile[] CreateAllSourceAlienProfiles()
        {
            var profiles = new AlienLifeformProfile[SourceKindOrder.Length];
            for (var i = 0; i < SourceKindOrder.Length; i++)
            {
                profiles[i] = GetProfile(SourceKindOrder[i]);
            }

            return profiles;
        }

        public static AlienLifeformProfile GetProfile(AlienLifeformKind kind)
        {
            return CreateAlienLifeformProfile(kind, ShipRoomId.Cockpit);
        }

        public static AlienLifeformProfile CreateAlienLifeformProfile(
            AlienLifeformKind kind,
            ShipRoomId targetRoom)
        {
            switch (kind)
            {
                case AlienLifeformKind.Cantabile:
                    return new AlienLifeformProfile(
                        kind,
                        CreateDefinition(
                            CantabileDefinitionId,
                            "Cantabile",
                            IntruderObjectiveType.AttackPlayer,
                            CantabileHealth,
                            CantabileMovementSpeed,
                            CantabileAttackRange,
                            CantabileAttackDelaySeconds,
                            CreateSoundHunterTargetPriorities(),
                            IntruderMobilityKind.Flying),
                        AlienLifeformBehaviorKind.SoundHunter,
                        CantabileDamage,
                        canTargetLoudestSound: true,
                        sonicStopRadiusMeters: CantabileSonicStopRadiusMeters,
                        sonicStopDurationSeconds: CantabileSonicStopDurationSeconds,
                        canResonateWithEngineRoom: true,
                        engineRoomResonanceDamage: CantabileDamage);
                case AlienLifeformKind.ConSpirito:
                    return new AlienLifeformProfile(
                        kind,
                        CreateDefinition(
                            ConSpiritoDefinitionId,
                            "Con Spirito",
                            IntruderObjectiveType.AttackPlayer,
                            ConSpiritoHealth,
                            ConSpiritoMovementSpeed,
                            ConSpiritoChargeDistanceMeters,
                            0f,
                            null,
                            IntruderMobilityKind.Walking),
                        AlienLifeformBehaviorKind.RandomCharger,
                        0,
                        chargeDamage: ConSpiritoChargeDamage,
                        chargeDistanceMeters: ConSpiritoChargeDistanceMeters,
                        chargeDurationSeconds: ConSpiritoChargeDurationSeconds,
                        restDurationSeconds: ConSpiritoRestDurationSeconds,
                        hasNoPriorityTarget: true);
                case AlienLifeformKind.Accelerando:
                    return new AlienLifeformProfile(
                        kind,
                        CreateDefinition(
                            AccelerandoDefinitionId,
                            "Accelerando",
                            IntruderObjectiveType.AttackPlayer,
                            AccelerandoHealth,
                            AccelerandoStartMovementSpeed,
                            AccelerandoAttackRange,
                            AccelerandoStartAttackDelaySeconds,
                            CreatePlayerTargetPriorities(),
                            IntruderMobilityKind.Walking),
                        AlienLifeformBehaviorKind.AcceleratingHunter,
                        AccelerandoDamage,
                        accelerationStartSpeed: AccelerandoStartMovementSpeed,
                        accelerationMaxSpeed: AccelerandoMaximumMovementSpeed,
                        accelerationStartAttackDelaySeconds: AccelerandoStartAttackDelaySeconds,
                        accelerationMinimumAttackDelaySeconds: AccelerandoMinimumAttackDelaySeconds,
                        accelerationAttackMovementSpeed: AccelerandoAttackMovementSpeed,
                        accelerationResetSightLossSeconds: AccelerandoSightResetSeconds);
                case AlienLifeformKind.Grave:
                    return new AlienLifeformProfile(
                        kind,
                        CreateDefinition(
                            GraveDefinitionId,
                            "Grave",
                            IntruderObjectiveType.DestroyShip,
                            GraveHealth,
                            GraveMovementSpeed,
                            GraveAttackRange,
                            GraveAttackDelaySeconds,
                            CreateShipTargetPriorities(targetRoom),
                            IntruderMobilityKind.Walking),
                        AlienLifeformBehaviorKind.StatusAmplifier,
                        GraveDamage,
                        statusDamageMultiplier: GraveStatusDamageMultiplier,
                        statusDurationMultiplier: GraveStatusDurationMultiplier,
                        attackWindupSeconds: GraveAttackWindupSeconds);
                case AlienLifeformKind.Smorzando:
                    return new AlienLifeformProfile(
                        kind,
                        CreateDefinition(
                            SmorzandoDefinitionId,
                            "Smorzando",
                            IntruderObjectiveType.AttackPlayer,
                            SmorzandoHealth,
                            0f,
                            2f,
                            1f,
                            CreatePlayerTargetPriorities(),
                            IntruderMobilityKind.Stationary),
                        AlienLifeformBehaviorKind.AbsorbingLiquid,
                        0,
                        liquidDamagePerSecond: SmorzandoLiquidDamagePerSecond,
                        liquidDamageReductionPercent: SmorzandoLiquidDamageReductionPercent,
                        absorptionStacksPerSecond: SmorzandoAbsorptionStacksPerSecond,
                        movementSlowPercentPerStack: SmorzandoMoveSlowPercentPerStack,
                        absorptionStopStackThreshold: SmorzandoStopStackThreshold,
                        absorptionStopDurationSeconds: SmorzandoStopDurationSeconds,
                        absorptionStopDamage: SmorzandoStopDamage,
                        zombieHealth: SmorzandoZombieHealth,
                        zombieSpeed: SmorzandoZombieMovementSpeed,
                        zombieChargeSpeed: SmorzandoZombieChargeSpeed,
                        zombieChargeDelaySeconds: SmorzandoZombieChargeDelaySeconds,
                        zombieSelfDestructDamage: SmorzandoZombieSelfDestructDamage,
                        zombieShieldDamageMultiplier: SmorzandoZombieShieldDamageMultiplier,
                        zombieSelfDestructRadiusMeters: SmorzandoZombieSelfDestructRadiusMeters);
                case AlienLifeformKind.Ostinato:
                    return new AlienLifeformProfile(
                        kind,
                        CreateDefinition(
                            OstinatoDefinitionId,
                            "Ostinato",
                            IntruderObjectiveType.AttackPlayer,
                            OstinatoHealth,
                            OstinatoMovementSpeed,
                            OstinatoAttackRange,
                            OstinatoAttackDelaySeconds,
                            CreatePlayerTargetPriorities(),
                            IntruderMobilityKind.Walking),
                        AlienLifeformBehaviorKind.FrenzyRestCycle,
                        OstinatoDamage,
                        frenzyHealthThresholdPercent: OstinatoFrenzyHealthThresholdPercent,
                        roarDurationSeconds: OstinatoRoarDurationSeconds,
                        roarDamageReductionPercent: OstinatoRoarDamageReductionPercent,
                        frenzyDurationSeconds: OstinatoFrenzyDurationSeconds,
                        frenzyMovementSpeed: OstinatoFrenzyMovementSpeed,
                        frenzyDamage: OstinatoFrenzyDamage,
                        frenzyAttackDelaySeconds: OstinatoFrenzyAttackDelaySeconds,
                        recoveryRestDurationSeconds: OstinatoRecoveryRestDurationSeconds,
                        recoveryDamageTakenBonusPercent: OstinatoRecoveryDamageTakenBonusPercent,
                        recoveryHealPercent: OstinatoRecoveryHealPercent);
                case AlienLifeformKind.Dolore:
                    return new AlienLifeformProfile(
                        kind,
                        CreateDefinition(
                            DoloreDefinitionId,
                            "Dolore",
                            IntruderObjectiveType.AttackPlayer,
                            DoloreHealth,
                            DoloreMovementSpeed,
                            DoloreAttackRange,
                            DoloreAttackDelaySeconds,
                            CreateDoloreTargetPriorities(),
                            IntruderMobilityKind.Walking),
                        AlienLifeformBehaviorKind.FrameExecutioner,
                        DoloreDamage,
                        executionHealthThresholdPercent: DoloreExecutionHealthThresholdPercent);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported alien lifeform kind.");
            }
        }

        public static AlienLifeformState CreateAlienLifeformIntrusionFromHazard(
            TransportHazardState hazard,
            int boardingIndex,
            ShipRoomId playerRoom = ShipRoomId.Cockpit)
        {
            if (hazard.HazardType != TransportHazardType.AlienLifeRegion)
            {
                throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Alien lifeform intrusion requires an alien life region hazard.");
            }

            if (boardingIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boardingIndex), "Alien lifeform boarding index must be positive.");
            }

            var seed = CreateBoardingSeed(hazard.Seed, boardingIndex);
            var kind = SelectAlienLifeformKind(seed);
            return CreateAlienLifeformIntrusionForSeed(
                kind,
                seed,
                playerRoom,
                "alien-life-" + hazard.Seed + "-" + boardingIndex);
        }

        public static AlienLifeformState CreateAlienLifeformIntrusionForSeed(
            AlienLifeformKind kind,
            int seed,
            ShipRoomId playerRoom,
            string attemptId = "alien-lifeform-validation")
        {
            if (kind == AlienLifeformKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Alien lifeform intrusion requires a concrete kind.");
            }

            var targetRoom = SelectAlienTargetRoom(seed);
            var profile = CreateAlienLifeformProfile(kind, targetRoom);
            var attempt = IntruderRules.CreateAttempt(attemptId, profile.IntruderDefinition, seed, playerRoom);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, profile.IntruderDefinition);
            return AlienLifeformState.Start(
                kind,
                profile.IntruderDefinition,
                boarded,
                intruder,
                GetDefaultBehaviorPhase(kind));
        }

        public static AlienLifeformTickResult TickAlienLifeform(
            AlienLifeformState state,
            ShipState ship,
            float deltaSeconds,
            ShipRoomId? loudestSoundRoom = null,
            IntruderFaction encounteredFaction = IntruderFaction.None,
            int targetCurrentHealth = -1,
            int targetMaxHealth = -1,
            bool lostForwardSight = false)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!state.IsActive || deltaSeconds <= 0f)
            {
                return new AlienLifeformTickResult(
                    state,
                    ship,
                    0,
                    0,
                    0,
                    effectiveMovementSpeed: state.IsActive ? CalculateEffectiveMovementSpeed(state, 0f, lostForwardSight) : 0f,
                    effectiveAttackDelaySeconds: state.IsActive ? CalculateEffectiveAttackDelay(state, 0f, lostForwardSight) : 0f);
            }

            var profile = GetProfile(state.Kind);
            var tickedIntruder = IntruderRules.TickStatusEffects(state.Intruder, deltaSeconds);
            if (!tickedIntruder.IsActive)
            {
                return new AlienLifeformTickResult(
                    state.WithIntruder(tickedIntruder),
                    ship,
                    0,
                    0,
                    0);
            }

            var intruder = tickedIntruder;
            if (state.Kind == AlienLifeformKind.Cantabile &&
                loudestSoundRoom.HasValue &&
                profile.CanTargetLoudestSound)
            {
                intruder = intruder.WithTarget(
                    IntruderTargetType.Player,
                    loudestSoundRoom.Value,
                    IntruderObjectiveType.AttackPlayer);
            }

            var movementBlocked = CombatStatusEffectRules.BlocksMovement(intruder.StatusEffects);
            var actionBlocked = CombatStatusEffectRules.BlocksActions(intruder.StatusEffects);
            var phase = state.Phase == AlienLifeformBehaviorPhase.None
                ? GetDefaultBehaviorPhase(state.Kind)
                : state.Phase;
            var phaseElapsed = state.PhaseElapsedSeconds + deltaSeconds;
            var elapsed = state.ElapsedSeconds + deltaSeconds;
            var attackAccumulator = actionBlocked ? state.AttackAccumulatorSeconds : state.AttackAccumulatorSeconds + deltaSeconds;
            var attackCount = 0;
            var roomDamage = 0;
            var playerDamage = 0;
            var absorptionStacksApplied = 0;
            var movementSlowPercent = 0;
            var engineRoomResonanceApplied = false;
            var transformedToZombie = false;
            var executedTarget = false;
            var specialEffectKind = AlienLifeformSpecialEffectKind.None;
            var statusEffect = default(CombatStatusEffectApplication);
            var nextShip = ship;

            if (!movementBlocked && !IsMovementBlockedByPhase(phase))
            {
                intruder = IntruderRules.MoveToReachableTargetRoom(intruder, ship);
            }

            if (state.Kind == AlienLifeformKind.ConSpirito)
            {
                ResolveConSpiritoPhase(
                    profile,
                    actionBlocked,
                    encounteredFaction,
                    ref phase,
                    ref phaseElapsed,
                    ref attackCount,
                    ref playerDamage,
                    ref specialEffectKind);
                attackAccumulator = 0f;
            }
            else if (state.Kind == AlienLifeformKind.Ostinato)
            {
                ResolveOstinatoPhase(profile, ref intruder, ref phase, ref phaseElapsed, ref specialEffectKind);
            }

            if (state.Kind == AlienLifeformKind.Smorzando && phase == AlienLifeformBehaviorPhase.Liquid)
            {
                TickSmorzandoLiquid(
                    profile,
                    state.AbsorptionStack,
                    ref attackAccumulator,
                    ref attackCount,
                    ref playerDamage,
                    ref absorptionStacksApplied,
                    ref movementSlowPercent,
                    ref specialEffectKind,
                    ref statusEffect);
            }
            else if (!actionBlocked && !IsActionBlockedByPhase(phase) && state.Kind != AlienLifeformKind.ConSpirito)
            {
                var attackDelay = CalculateEffectiveAttackDelay(state, elapsed, lostForwardSight, phase);
                if (attackDelay <= 0.0001f)
                {
                    attackCount = 1;
                    ApplyAlienAttack(
                        profile,
                        intruder,
                        phase,
                        targetCurrentHealth,
                        targetMaxHealth,
                        ref nextShip,
                        ref roomDamage,
                        ref playerDamage,
                        ref specialEffectKind,
                        ref statusEffect,
                        ref engineRoomResonanceApplied,
                        ref executedTarget);
                    attackAccumulator = 0f;
                }
                else
                {
                    while (attackAccumulator + 0.0001f >= attackDelay)
                    {
                        attackAccumulator -= attackDelay;
                        attackCount++;
                        ApplyAlienAttack(
                            profile,
                            intruder,
                            phase,
                            targetCurrentHealth,
                            targetMaxHealth,
                            ref nextShip,
                            ref roomDamage,
                            ref playerDamage,
                            ref specialEffectKind,
                            ref statusEffect,
                            ref engineRoomResonanceApplied,
                            ref executedTarget);
                    }
                }
            }

            var nextAbsorptionStack = state.AbsorptionStack + absorptionStacksApplied;
            if (state.Kind == AlienLifeformKind.Smorzando && statusEffect.HasEffect)
            {
                nextAbsorptionStack = 0;
            }

            if (state.Kind == AlienLifeformKind.Smorzando &&
                phase == AlienLifeformBehaviorPhase.Zombie &&
                intruder.CurrentHealth > profile.ZombieHealth)
            {
                intruder = intruder.WithDamage(intruder.CurrentHealth - profile.ZombieHealth);
                transformedToZombie = true;
                specialEffectKind = AlienLifeformSpecialEffectKind.SmorzandoZombieTransform;
            }

            var nextState = state.WithProgress(
                intruder,
                phase,
                lostForwardSight && state.Kind == AlienLifeformKind.Accelerando ? 0f : elapsed,
                phaseElapsed,
                attackAccumulator,
                state.AppliedAttackCount + attackCount,
                state.TotalRoomDamageApplied + roomDamage,
                state.TotalPlayerDamageApplied + playerDamage,
                nextAbsorptionStack);

            return new AlienLifeformTickResult(
                nextState,
                nextShip,
                attackCount,
                roomDamage,
                playerDamage,
                specialEffectKind,
                statusEffect,
                CalculateEffectiveMovementSpeed(nextState, elapsed, lostForwardSight, phase),
                CalculateEffectiveAttackDelay(nextState, elapsed, lostForwardSight, phase),
                absorptionStacksApplied,
                movementSlowPercent,
                engineRoomResonanceApplied,
                transformedToZombie,
                executedTarget,
                phase == AlienLifeformBehaviorPhase.Resting || phase == AlienLifeformBehaviorPhase.Recovering,
                phase == AlienLifeformBehaviorPhase.Frenzy);
        }

        public static AlienLifeformState ApplyDamage(
            AlienLifeformState state,
            int damage,
            bool isStatusDamage = false)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Alien lifeform damage cannot be negative.");
            }

            if (!state.IsActive || damage == 0)
            {
                return state;
            }

            var profile = GetProfile(state.Kind);
            var adjustedDamage = damage;
            if (state.Kind == AlienLifeformKind.Grave && isStatusDamage)
            {
                adjustedDamage = CalculateStatusDamageToGrave(damage);
            }
            else if (state.Kind == AlienLifeformKind.Smorzando &&
                     state.Phase == AlienLifeformBehaviorPhase.Liquid)
            {
                adjustedDamage = CalculateSmorzandoDamageAfterLiquidReduction(damage);
            }

            if (profile.Kind == AlienLifeformKind.Smorzando &&
                state.Phase == AlienLifeformBehaviorPhase.Zombie &&
                adjustedDamage > 0)
            {
                return state.WithIntruder(state.Intruder.WithDamage(adjustedDamage));
            }

            return state.WithIntruder(state.Intruder.WithDamage(adjustedDamage));
        }

        public static AlienLifeformState ApplyStatusEffect(
            AlienLifeformState state,
            CombatStatusEffectApplication application)
        {
            if (!state.IsActive || !application.HasEffect)
            {
                return state;
            }

            var adjusted = state.Kind == AlienLifeformKind.Grave
                ? AmplifyStatusEffectForGrave(application)
                : application;
            return state.WithIntruder(IntruderRules.ApplyStatusEffect(state.Intruder, adjusted));
        }

        public static AlienLifeformState TransformSmorzandoToZombie(AlienLifeformState state)
        {
            if (!state.IsActive || state.Kind != AlienLifeformKind.Smorzando)
            {
                return state;
            }

            var profile = GetProfile(AlienLifeformKind.Smorzando);
            var intruder = state.Intruder.CurrentHealth > profile.ZombieHealth
                ? state.Intruder.WithDamage(state.Intruder.CurrentHealth - profile.ZombieHealth)
                : state.Intruder;
            return state.WithProgress(
                intruder,
                AlienLifeformBehaviorPhase.Zombie,
                state.ElapsedSeconds,
                0f,
                0f,
                state.AppliedAttackCount,
                state.TotalRoomDamageApplied,
                state.TotalPlayerDamageApplied,
                state.AbsorptionStack);
        }

        public static bool ShouldConSpiritoChargeEncounteredFaction(IntruderFaction encounteredFaction)
        {
            return encounteredFaction != IntruderFaction.None &&
                   encounteredFaction != IntruderFaction.AlienLifeform;
        }

        public static float CalculateAccelerandoMovementSpeed(float encounterSeconds)
        {
            var ratio = Clamp01(encounterSeconds / AccelerandoRampDurationSeconds);
            return AccelerandoStartMovementSpeed +
                   (AccelerandoMaximumMovementSpeed - AccelerandoStartMovementSpeed) * ratio;
        }

        public static float CalculateAccelerandoAttackDelay(float encounterSeconds)
        {
            var ratio = Clamp01(encounterSeconds / AccelerandoRampDurationSeconds);
            return AccelerandoStartAttackDelaySeconds -
                   (AccelerandoStartAttackDelaySeconds - AccelerandoMinimumAttackDelaySeconds) * ratio;
        }

        public static CombatStatusEffectApplication CreateCantabileSonicStop()
        {
            return CombatStatusEffectRules.CreateStopped(CantabileSonicStopDurationSeconds);
        }

        public static CombatStatusEffectApplication AmplifyStatusEffectForGrave(
            CombatStatusEffectApplication application)
        {
            if (!application.HasEffect)
            {
                return application;
            }

            return new CombatStatusEffectApplication(
                application.Kind,
                application.DurationSeconds * GraveStatusDurationMultiplier,
                application.TickIntervalSeconds,
                application.TickDamage > 0
                    ? RoundUpToInt(application.TickDamage * GraveStatusDamageMultiplier)
                    : 0);
        }

        public static int CalculateStatusDamageToGrave(int damage)
        {
            return damage <= 0 ? 0 : RoundUpToInt(damage * GraveStatusDamageMultiplier);
        }

        public static int CalculateSmorzandoDamageAfterLiquidReduction(int rawDamage)
        {
            if (rawDamage <= 0)
            {
                return 0;
            }

            var damage = RoundUpToInt(rawDamage * ((100 - SmorzandoLiquidDamageReductionPercent) / 100f));
            return Math.Max(1, damage);
        }

        public static bool ShouldDoloreExecuteTarget(int targetCurrentHealth, int targetMaxHealth)
        {
            if (targetCurrentHealth <= 0 || targetMaxHealth <= 0)
            {
                return false;
            }

            return (float)targetCurrentHealth / targetMaxHealth <= DoloreExecutionHealthThresholdPercent + 0.0001f;
        }

        public static AlienLifeformKind SelectAlienLifeformKind(int seed)
        {
            return SourceKindOrder[PositiveModulo(seed, SourceKindOrder.Length)];
        }

        public static ShipRoomId SelectAlienTargetRoom(int seed)
        {
            return AlienTargetRoomOrder[PositiveModulo(seed, AlienTargetRoomOrder.Length)];
        }

        public static string FormatAlienLifeformKind(AlienLifeformKind kind)
        {
            switch (kind)
            {
                case AlienLifeformKind.Cantabile:
                    return "Cantabile";
                case AlienLifeformKind.ConSpirito:
                    return "Con Spirito";
                case AlienLifeformKind.Accelerando:
                    return "Accelerando";
                case AlienLifeformKind.Grave:
                    return "Grave";
                case AlienLifeformKind.Smorzando:
                    return "Smorzando";
                case AlienLifeformKind.Ostinato:
                    return "Ostinato";
                case AlienLifeformKind.Dolore:
                    return "Dolore";
                default:
                    return "None";
            }
        }

        private static void ResolveConSpiritoPhase(
            AlienLifeformProfile profile,
            bool actionBlocked,
            IntruderFaction encounteredFaction,
            ref AlienLifeformBehaviorPhase phase,
            ref float phaseElapsed,
            ref int attackCount,
            ref int playerDamage,
            ref AlienLifeformSpecialEffectKind specialEffectKind)
        {
            if (actionBlocked && phase == AlienLifeformBehaviorPhase.Charging)
            {
                phase = AlienLifeformBehaviorPhase.Resting;
                phaseElapsed = 0f;
                return;
            }

            if (phase == AlienLifeformBehaviorPhase.Tracking &&
                ShouldConSpiritoChargeEncounteredFaction(encounteredFaction))
            {
                phase = AlienLifeformBehaviorPhase.Charging;
                phaseElapsed = 0f;
                attackCount = 1;
                playerDamage += profile.ChargeDamage;
                specialEffectKind = AlienLifeformSpecialEffectKind.Charge;
                return;
            }

            if (phase == AlienLifeformBehaviorPhase.Charging &&
                phaseElapsed + 0.0001f >= profile.ChargeDurationSeconds)
            {
                phase = AlienLifeformBehaviorPhase.Resting;
                phaseElapsed = 0f;
                return;
            }

            if (phase == AlienLifeformBehaviorPhase.Resting &&
                phaseElapsed + 0.0001f >= profile.RestDurationSeconds)
            {
                phase = AlienLifeformBehaviorPhase.Tracking;
                phaseElapsed = 0f;
            }
        }

        private static void ResolveOstinatoPhase(
            AlienLifeformProfile profile,
            ref IntruderEntityState intruder,
            ref AlienLifeformBehaviorPhase phase,
            ref float phaseElapsed,
            ref AlienLifeformSpecialEffectKind specialEffectKind)
        {
            if (phase == AlienLifeformBehaviorPhase.Tracking &&
                intruder.CurrentHealth <= RoundUpToInt(intruder.MaxHealth * profile.FrenzyHealthThresholdPercent))
            {
                phase = AlienLifeformBehaviorPhase.Roaring;
                phaseElapsed = 0f;
                specialEffectKind = AlienLifeformSpecialEffectKind.Frenzy;
                return;
            }

            if (phase == AlienLifeformBehaviorPhase.Roaring &&
                phaseElapsed + 0.0001f >= profile.RoarDurationSeconds)
            {
                phase = AlienLifeformBehaviorPhase.Frenzy;
                phaseElapsed = 0f;
                specialEffectKind = AlienLifeformSpecialEffectKind.Frenzy;
                return;
            }

            if (phase == AlienLifeformBehaviorPhase.Frenzy &&
                phaseElapsed + 0.0001f >= profile.FrenzyDurationSeconds)
            {
                phase = AlienLifeformBehaviorPhase.Recovering;
                phaseElapsed = 0f;
                specialEffectKind = AlienLifeformSpecialEffectKind.RestRecovery;
                return;
            }

            if (phase == AlienLifeformBehaviorPhase.Recovering &&
                phaseElapsed + 0.0001f >= profile.RecoveryRestDurationSeconds)
            {
                intruder = intruder.WithRecoveredHealth(RoundUpToInt(intruder.MaxHealth * profile.RecoveryHealPercent));
                phase = AlienLifeformBehaviorPhase.Tracking;
                phaseElapsed = 0f;
                specialEffectKind = AlienLifeformSpecialEffectKind.RestRecovery;
            }
        }

        private static void TickSmorzandoLiquid(
            AlienLifeformProfile profile,
            int currentAbsorptionStack,
            ref float attackAccumulator,
            ref int attackCount,
            ref int playerDamage,
            ref int absorptionStacksApplied,
            ref int movementSlowPercent,
            ref AlienLifeformSpecialEffectKind specialEffectKind,
            ref CombatStatusEffectApplication statusEffect)
        {
            while (attackAccumulator + 0.0001f >= 1f)
            {
                attackAccumulator -= 1f;
                attackCount++;
                playerDamage += profile.LiquidDamagePerSecond;
                absorptionStacksApplied += profile.AbsorptionStacksPerSecond;
            }

            var stack = currentAbsorptionStack + absorptionStacksApplied;
            movementSlowPercent = Math.Min(100, stack * profile.MovementSlowPercentPerStack);
            if (profile.AbsorptionStopStackThreshold > 0 &&
                stack >= profile.AbsorptionStopStackThreshold)
            {
                playerDamage += profile.AbsorptionStopDamage;
                statusEffect = CombatStatusEffectRules.CreateStopped(profile.AbsorptionStopDurationSeconds);
                specialEffectKind = AlienLifeformSpecialEffectKind.AbsorptionStop;
            }
        }

        private static void ApplyAlienAttack(
            AlienLifeformProfile profile,
            IntruderEntityState intruder,
            AlienLifeformBehaviorPhase phase,
            int targetCurrentHealth,
            int targetMaxHealth,
            ref ShipState ship,
            ref int roomDamage,
            ref int playerDamage,
            ref AlienLifeformSpecialEffectKind specialEffectKind,
            ref CombatStatusEffectApplication statusEffect,
            ref bool engineRoomResonanceApplied,
            ref bool executedTarget)
        {
            if (profile.Kind == AlienLifeformKind.Dolore &&
                ShouldDoloreExecuteTarget(targetCurrentHealth, targetMaxHealth))
            {
                playerDamage += targetCurrentHealth;
                specialEffectKind = AlienLifeformSpecialEffectKind.ExecutionPull;
                executedTarget = true;
                return;
            }

            var damage = profile.Kind == AlienLifeformKind.Ostinato &&
                         phase == AlienLifeformBehaviorPhase.Frenzy &&
                         profile.FrenzyDamage > 0
                ? profile.FrenzyDamage
                : profile.DirectDamage;

            switch (intruder.TargetType)
            {
                case IntruderTargetType.Player:
                    playerDamage += damage;
                    if (profile.Kind == AlienLifeformKind.Cantabile)
                    {
                        statusEffect = CreateCantabileSonicStop();
                        specialEffectKind = AlienLifeformSpecialEffectKind.SonicStop;
                    }

                    break;
                case IntruderTargetType.Ship:
                case IntruderTargetType.Room:
                    if (damage > 0)
                    {
                        var room = ship.GetRoom(intruder.CurrentRoom);
                        ship = ship.WithRoom(intruder.CurrentRoom, room.WithDamage(damage));
                        roomDamage += damage;
                    }

                    break;
            }

            if (profile.Kind == AlienLifeformKind.Accelerando)
            {
                specialEffectKind = AlienLifeformSpecialEffectKind.Acceleration;
            }

            if (profile.CanResonateWithEngineRoom &&
                profile.EngineRoomResonanceDamage > 0 &&
                (intruder.CurrentRoom == ShipRoomId.EngineRoom ||
                 intruder.TargetRoom == ShipRoomId.EngineRoom))
            {
                var engineRoom = ship.GetRoom(ShipRoomId.EngineRoom);
                ship = ship.WithRoom(ShipRoomId.EngineRoom, engineRoom.WithDamage(profile.EngineRoomResonanceDamage));
                roomDamage += profile.EngineRoomResonanceDamage;
                engineRoomResonanceApplied = true;
            }
        }

        private static float CalculateEffectiveMovementSpeed(
            AlienLifeformState state,
            float elapsedSeconds,
            bool lostForwardSight,
            AlienLifeformBehaviorPhase? overridePhase = null)
        {
            if (!state.IsActive)
            {
                return 0f;
            }

            var profile = GetProfile(state.Kind);
            var phase = overridePhase ?? state.Phase;
            switch (state.Kind)
            {
                case AlienLifeformKind.ConSpirito:
                    return phase == AlienLifeformBehaviorPhase.Charging && profile.ChargeDurationSeconds > 0.0001f
                        ? profile.ChargeDistanceMeters / profile.ChargeDurationSeconds
                        : profile.IntruderDefinition.MovementSpeed;
                case AlienLifeformKind.Accelerando:
                    return lostForwardSight ? profile.AccelerationStartSpeed : CalculateAccelerandoMovementSpeed(elapsedSeconds);
                case AlienLifeformKind.Smorzando:
                    if (phase == AlienLifeformBehaviorPhase.Zombie)
                    {
                        return profile.ZombieSpeed;
                    }

                    return 0f;
                case AlienLifeformKind.Ostinato:
                    if (phase == AlienLifeformBehaviorPhase.Frenzy)
                    {
                        return profile.FrenzyMovementSpeed;
                    }

                    return phase == AlienLifeformBehaviorPhase.Roaring ||
                           phase == AlienLifeformBehaviorPhase.Recovering
                        ? 0f
                        : profile.IntruderDefinition.MovementSpeed;
                default:
                    return profile.IntruderDefinition.MovementSpeed;
            }
        }

        private static float CalculateEffectiveAttackDelay(
            AlienLifeformState state,
            float elapsedSeconds,
            bool lostForwardSight,
            AlienLifeformBehaviorPhase? overridePhase = null)
        {
            if (!state.IsActive)
            {
                return 0f;
            }

            var profile = GetProfile(state.Kind);
            var phase = overridePhase ?? state.Phase;
            switch (state.Kind)
            {
                case AlienLifeformKind.Accelerando:
                    return lostForwardSight
                        ? profile.AccelerationStartAttackDelaySeconds
                        : CalculateAccelerandoAttackDelay(elapsedSeconds);
                case AlienLifeformKind.Ostinato:
                    return phase == AlienLifeformBehaviorPhase.Frenzy
                        ? profile.FrenzyAttackDelaySeconds
                        : profile.IntruderDefinition.AttackDelaySeconds;
                case AlienLifeformKind.ConSpirito:
                    return 0f;
                default:
                    return profile.IntruderDefinition.AttackDelaySeconds;
            }
        }

        private static bool IsMovementBlockedByPhase(AlienLifeformBehaviorPhase phase)
        {
            return phase == AlienLifeformBehaviorPhase.Resting ||
                   phase == AlienLifeformBehaviorPhase.Liquid ||
                   phase == AlienLifeformBehaviorPhase.Roaring ||
                   phase == AlienLifeformBehaviorPhase.Recovering;
        }

        private static bool IsActionBlockedByPhase(AlienLifeformBehaviorPhase phase)
        {
            return phase == AlienLifeformBehaviorPhase.Resting ||
                   phase == AlienLifeformBehaviorPhase.Roaring ||
                   phase == AlienLifeformBehaviorPhase.Recovering;
        }

        private static AlienLifeformBehaviorPhase GetDefaultBehaviorPhase(AlienLifeformKind kind)
        {
            return kind == AlienLifeformKind.Smorzando
                ? AlienLifeformBehaviorPhase.Liquid
                : AlienLifeformBehaviorPhase.Tracking;
        }

        private static IntruderDefinition CreateDefinition(
            string definitionId,
            string displayName,
            IntruderObjectiveType objective,
            int maxHealth,
            float movementSpeed,
            float attackRange,
            float attackDelaySeconds,
            IntruderTargetPriority[] targetPriorities,
            IntruderMobilityKind mobilityKind)
        {
            return new IntruderDefinition(
                definitionId,
                displayName,
                IntruderFaction.AlienLifeform,
                objective,
                maxHealth,
                movementSpeed,
                attackRange,
                attackDelaySeconds,
                targetPriorities,
                mobilityKind);
        }

        private static IntruderTargetPriority[] CreateSoundHunterTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Player, ShipRoomId.Cockpit, 0),
                new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.EngineRoom, 1)
            };
        }

        private static IntruderTargetPriority[] CreatePlayerTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Player, ShipRoomId.Cockpit, 0)
            };
        }

        private static IntruderTargetPriority[] CreateShipTargetPriorities(ShipRoomId targetRoom)
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Ship, targetRoom, 0)
            };
        }

        private static IntruderTargetPriority[] CreateDoloreTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Player, ShipRoomId.Cockpit, 0),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.ControlRoom, 1)
            };
        }

        private static int CreateBoardingSeed(int hazardSeed, int boardingIndex)
        {
            unchecked
            {
                return (int)(((uint)hazardSeed ^ ((uint)boardingIndex * 16777619u)) & 0x7fffffffu);
            }
        }

        private static int RoundUpToInt(float value)
        {
            return value <= 0f ? 0 : (int)Math.Ceiling(value);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            if (divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(divisor), "Divisor must be positive.");
            }

            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
