using System;

namespace Bellerophon.Core.Session
{
    public enum SeedIntruderKind
    {
        None,
        Parvum,
        Fuga,
        LongaArma,
        Tergo,
        Urzere,
        Societas,
        Monstrum,
        Mimesis
    }

    public enum SeedIntruderBehaviorKind
    {
        None,
        MetalFeeder,
        BacklineImpaler,
        SeedSupportEmitter,
        PackMetalFeeder,
        GiantMetalFeeder,
        PlayerMimic
    }

    public enum SeedIntruderSpecialEffectKind
    {
        None,
        BiologicalMovementSlow,
        TergoPostureBreak,
        SeedEntityEmpowerment,
        RoomWideMovementSlow,
        MimesisInjectionInterruptStop
    }

    public readonly struct SeedIntruderProfile
    {
        public SeedIntruderProfile(
            SeedIntruderKind kind,
            IntruderDefinition intruderDefinition,
            SeedIntruderBehaviorKind behaviorKind,
            bool prefersMetal,
            bool canFeedOnMetalCargo,
            bool canCreateDestroyedPartPlaceholder,
            bool canBeExternallyRepelled,
            bool playerTargetLocked,
            bool followsOtherSeedEntities,
            bool anchorsAtTargetRoom,
            int biologicalDamage,
            int shieldedBiologicalDamage,
            int nonBiologicalDamage,
            int shipFacilityDamage,
            float metalCargoDamagePercentPerAttack,
            int movementSlowPercent,
            float movementSlowDurationSeconds,
            PlayerPostureState playerPostureOnHit,
            int tergoOpeningDamage,
            int tergoPierceDamage,
            int tergoPinnedDrillDamage,
            float tergoPinnedDrillDelaySeconds,
            int seedAttackDamageBonusPercent,
            float seedMovementSpeedBonus,
            int seedHealthRegenPerSecond,
            int nonSeedVisionPenalty,
            int roomWideMovementSlowPercent,
            float roomWideMovementSlowDurationSeconds,
            bool hasMimesisVoiceMimicryPlaceholder,
            bool stopsWhenInjectionInterrupted)
        {
            if (kind == SeedIntruderKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Seed intruder profile requires a concrete kind.");
            }

            if (behaviorKind == SeedIntruderBehaviorKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(behaviorKind), "Seed intruder profile requires a behavior kind.");
            }

            if (intruderDefinition.Faction != IntruderFaction.SeedEntity)
            {
                throw new ArgumentOutOfRangeException(nameof(intruderDefinition), "Seed intruder profile requires a seed entity definition.");
            }

            Kind = kind;
            IntruderDefinition = intruderDefinition;
            BehaviorKind = behaviorKind;
            PrefersMetal = prefersMetal;
            CanFeedOnMetalCargo = canFeedOnMetalCargo;
            CanCreateDestroyedPartPlaceholder = canCreateDestroyedPartPlaceholder;
            CanBeExternallyRepelled = canBeExternallyRepelled;
            PlayerTargetLocked = playerTargetLocked;
            FollowsOtherSeedEntities = followsOtherSeedEntities;
            AnchorsAtTargetRoom = anchorsAtTargetRoom;
            BiologicalDamage = Math.Max(0, biologicalDamage);
            ShieldedBiologicalDamage = Math.Max(0, shieldedBiologicalDamage);
            NonBiologicalDamage = Math.Max(0, nonBiologicalDamage);
            ShipFacilityDamage = Math.Max(0, shipFacilityDamage);
            MetalCargoDamagePercentPerAttack = Math.Max(0f, metalCargoDamagePercentPerAttack);
            MovementSlowPercent = ClampPercent(movementSlowPercent);
            MovementSlowDurationSeconds = Math.Max(0f, movementSlowDurationSeconds);
            PlayerPostureOnHit = playerPostureOnHit;
            TergoOpeningDamage = Math.Max(0, tergoOpeningDamage);
            TergoPierceDamage = Math.Max(0, tergoPierceDamage);
            TergoPinnedDrillDamage = Math.Max(0, tergoPinnedDrillDamage);
            TergoPinnedDrillDelaySeconds = Math.Max(0f, tergoPinnedDrillDelaySeconds);
            SeedAttackDamageBonusPercent = Math.Max(0, seedAttackDamageBonusPercent);
            SeedMovementSpeedBonus = Math.Max(0f, seedMovementSpeedBonus);
            SeedHealthRegenPerSecond = Math.Max(0, seedHealthRegenPerSecond);
            NonSeedVisionPenalty = Math.Max(0, nonSeedVisionPenalty);
            RoomWideMovementSlowPercent = ClampPercent(roomWideMovementSlowPercent);
            RoomWideMovementSlowDurationSeconds = Math.Max(0f, roomWideMovementSlowDurationSeconds);
            HasMimesisVoiceMimicryPlaceholder = hasMimesisVoiceMimicryPlaceholder;
            StopsWhenInjectionInterrupted = stopsWhenInjectionInterrupted;
        }

        public SeedIntruderKind Kind { get; }

        public IntruderDefinition IntruderDefinition { get; }

        public SeedIntruderBehaviorKind BehaviorKind { get; }

        public bool PrefersMetal { get; }

        public bool CanFeedOnMetalCargo { get; }

        public bool CanCreateDestroyedPartPlaceholder { get; }

        public bool CanBeExternallyRepelled { get; }

        public bool PlayerTargetLocked { get; }

        public bool FollowsOtherSeedEntities { get; }

        public bool AnchorsAtTargetRoom { get; }

        public int BiologicalDamage { get; }

        public int ShieldedBiologicalDamage { get; }

        public int NonBiologicalDamage { get; }

        public int ShipFacilityDamage { get; }

        public float MetalCargoDamagePercentPerAttack { get; }

        public int MovementSlowPercent { get; }

        public float MovementSlowDurationSeconds { get; }

        public PlayerPostureState PlayerPostureOnHit { get; }

        public int TergoOpeningDamage { get; }

        public int TergoPierceDamage { get; }

        public int TergoPinnedDrillDamage { get; }

        public float TergoPinnedDrillDelaySeconds { get; }

        public int SeedAttackDamageBonusPercent { get; }

        public float SeedMovementSpeedBonus { get; }

        public int SeedHealthRegenPerSecond { get; }

        public int NonSeedVisionPenalty { get; }

        public int RoomWideMovementSlowPercent { get; }

        public float RoomWideMovementSlowDurationSeconds { get; }

        public bool HasMimesisVoiceMimicryPlaceholder { get; }

        public bool StopsWhenInjectionInterrupted { get; }

        public SeedIntruderSpecialEffectKind PrimarySpecialEffectKind
        {
            get
            {
                switch (BehaviorKind)
                {
                    case SeedIntruderBehaviorKind.BacklineImpaler:
                        return SeedIntruderSpecialEffectKind.TergoPostureBreak;
                    case SeedIntruderBehaviorKind.SeedSupportEmitter:
                        return SeedIntruderSpecialEffectKind.SeedEntityEmpowerment;
                    case SeedIntruderBehaviorKind.GiantMetalFeeder:
                        return SeedIntruderSpecialEffectKind.RoomWideMovementSlow;
                    case SeedIntruderBehaviorKind.PlayerMimic:
                        return SeedIntruderSpecialEffectKind.MimesisInjectionInterruptStop;
                    case SeedIntruderBehaviorKind.MetalFeeder:
                    case SeedIntruderBehaviorKind.PackMetalFeeder:
                        return MovementSlowPercent > 0
                            ? SeedIntruderSpecialEffectKind.BiologicalMovementSlow
                            : SeedIntruderSpecialEffectKind.None;
                    default:
                        return SeedIntruderSpecialEffectKind.None;
                }
            }
        }

        public bool CanAttackPlayer => BiologicalDamage > 0 || TergoPierceDamage > 0;

        public bool CanDamageShip => ShipFacilityDamage > 0;

        private static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }

    public readonly struct SeedIntruderState
    {
        private SeedIntruderState(
            SeedIntruderKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder,
            float elapsedSeconds,
            float attackAccumulatorSeconds,
            int appliedAttackCount,
            int totalRoomDamageApplied,
            float totalCargoDamagePercentApplied)
        {
            Kind = kind;
            Definition = definition;
            Attempt = attempt;
            Intruder = intruder;
            ElapsedSeconds = Math.Max(0f, elapsedSeconds);
            AttackAccumulatorSeconds = Math.Max(0f, attackAccumulatorSeconds);
            AppliedAttackCount = Math.Max(0, appliedAttackCount);
            TotalRoomDamageApplied = Math.Max(0, totalRoomDamageApplied);
            TotalCargoDamagePercentApplied = totalCargoDamagePercentApplied < 0f ? 0f : totalCargoDamagePercentApplied;
        }

        public SeedIntruderKind Kind { get; }

        public IntruderDefinition Definition { get; }

        public IntrusionAttemptState Attempt { get; }

        public IntruderEntityState Intruder { get; }

        public float ElapsedSeconds { get; }

        public float AttackAccumulatorSeconds { get; }

        public int AppliedAttackCount { get; }

        public int TotalRoomDamageApplied { get; }

        public float TotalCargoDamagePercentApplied { get; }

        public bool IsActive => Kind != SeedIntruderKind.None && Intruder.IsActive;

        public bool IsResolved => Kind != SeedIntruderKind.None && Intruder.IsResolved;

        public ShipRoomId TargetRoom => Intruder.TargetRoom;

        public static SeedIntruderState None => new SeedIntruderState(
            SeedIntruderKind.None,
            default,
            IntrusionAttemptState.None,
            IntruderEntityState.None,
            0f,
            0f,
            0,
            0,
            0f);

        public static SeedIntruderState Start(
            SeedIntruderKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder)
        {
            if (kind == SeedIntruderKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Seed intruder requires a concrete kind.");
            }

            if (!intruder.IsActive)
            {
                throw new ArgumentException("Seed intruder entity must be active.", nameof(intruder));
            }

            return new SeedIntruderState(
                kind,
                definition,
                attempt,
                intruder,
                0f,
                0f,
                0,
                0,
                0f);
        }

        public SeedIntruderState WithProgress(
            IntruderEntityState intruder,
            float elapsedSeconds,
            float attackAccumulatorSeconds,
            int appliedAttackCount,
            int totalRoomDamageApplied,
            float totalCargoDamagePercentApplied)
        {
            return new SeedIntruderState(
                Kind,
                Definition,
                Attempt,
                intruder,
                elapsedSeconds,
                attackAccumulatorSeconds,
                appliedAttackCount,
                totalRoomDamageApplied,
                totalCargoDamagePercentApplied);
        }

        public SeedIntruderState WithIntruder(IntruderEntityState intruder)
        {
            return new SeedIntruderState(
                Kind,
                Definition,
                Attempt,
                intruder,
                ElapsedSeconds,
                AttackAccumulatorSeconds,
                AppliedAttackCount,
                TotalRoomDamageApplied,
                TotalCargoDamagePercentApplied);
        }
    }

    public readonly struct SeedIntruderTickResult
    {
        public SeedIntruderTickResult(
            SeedIntruderState state,
            ShipState ship,
            CargoState cargo,
            int attackCount,
            int roomDamageApplied,
            float cargoDamagePercentApplied,
            SeedIntruderSpecialEffectKind specialEffectKind = SeedIntruderSpecialEffectKind.None,
            int playerDamageApplied = 0,
            int movementSlowPercentApplied = 0,
            float movementSlowDurationSecondsApplied = 0f,
            PlayerPostureState playerPostureApplied = PlayerPostureState.Standing,
            int seedAttackDamageBonusPercentApplied = 0,
            float seedMovementSpeedBonusApplied = 0f,
            int seedHealthRegenPerSecondApplied = 0,
            int nonSeedVisionPenaltyApplied = 0,
            int roomWideMovementSlowPercentApplied = 0,
            float roomWideMovementSlowDurationSecondsApplied = 0f,
            bool hasMimesisVoiceMimicryPlaceholder = false)
        {
            State = state;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            Cargo = cargo;
            AttackCount = Math.Max(0, attackCount);
            RoomDamageApplied = Math.Max(0, roomDamageApplied);
            CargoDamagePercentApplied = cargoDamagePercentApplied < 0f ? 0f : cargoDamagePercentApplied;
            SpecialEffectKind = specialEffectKind;
            PlayerDamageApplied = Math.Max(0, playerDamageApplied);
            MovementSlowPercentApplied = ClampPercent(movementSlowPercentApplied);
            MovementSlowDurationSecondsApplied = Math.Max(0f, movementSlowDurationSecondsApplied);
            PlayerPostureApplied = playerPostureApplied;
            SeedAttackDamageBonusPercentApplied = Math.Max(0, seedAttackDamageBonusPercentApplied);
            SeedMovementSpeedBonusApplied = Math.Max(0f, seedMovementSpeedBonusApplied);
            SeedHealthRegenPerSecondApplied = Math.Max(0, seedHealthRegenPerSecondApplied);
            NonSeedVisionPenaltyApplied = Math.Max(0, nonSeedVisionPenaltyApplied);
            RoomWideMovementSlowPercentApplied = ClampPercent(roomWideMovementSlowPercentApplied);
            RoomWideMovementSlowDurationSecondsApplied = Math.Max(0f, roomWideMovementSlowDurationSecondsApplied);
            HasMimesisVoiceMimicryPlaceholder = hasMimesisVoiceMimicryPlaceholder;
        }

        public SeedIntruderState State { get; }

        public ShipState Ship { get; }

        public CargoState Cargo { get; }

        public int AttackCount { get; }

        public int RoomDamageApplied { get; }

        public float CargoDamagePercentApplied { get; }

        public SeedIntruderSpecialEffectKind SpecialEffectKind { get; }

        public int PlayerDamageApplied { get; }

        public int MovementSlowPercentApplied { get; }

        public float MovementSlowDurationSecondsApplied { get; }

        public PlayerPostureState PlayerPostureApplied { get; }

        public int SeedAttackDamageBonusPercentApplied { get; }

        public float SeedMovementSpeedBonusApplied { get; }

        public int SeedHealthRegenPerSecondApplied { get; }

        public int NonSeedVisionPenaltyApplied { get; }

        public int RoomWideMovementSlowPercentApplied { get; }

        public float RoomWideMovementSlowDurationSecondsApplied { get; }

        public bool HasMimesisVoiceMimicryPlaceholder { get; }

        public bool AppliedDamage => RoomDamageApplied > 0 || CargoDamagePercentApplied > 0f || PlayerDamageApplied > 0;

        public bool AppliedSpecialEffect => SpecialEffectKind != SeedIntruderSpecialEffectKind.None ||
                                            HasMimesisVoiceMimicryPlaceholder;

        private static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }

    public static class SeedIntruderRules
    {
        public const float OccurrenceCheckIntervalSeconds = 2f;
        public const int OccurrencePercent = 15;
        public const string ParvumDefinitionId = "seed-parvum";
        public const string FugaDefinitionId = "seed-fuga";
        public const string LongaArmaDefinitionId = "seed-longa-arma";
        public const string TergoDefinitionId = "seed-tergo";
        public const string UrzereDefinitionId = "seed-urzere";
        public const string SocietasDefinitionId = "seed-societas";
        public const string MonstrumDefinitionId = "seed-monstrum";
        public const string MimesisDefinitionId = "seed-mimesis";

        public const int ParvumHealth = 55;
        public const float ParvumMovementSpeed = 2.5f;
        public const float ParvumAttackRange = 1f;
        public const float ParvumAttackDelaySeconds = 0.5f;
        public const int ParvumBiologicalDamage = 6;
        public const int ParvumShieldedBiologicalDamage = 1;
        public const int ParvumShipFacilityDamage = 3;
        public const int ParvumMovementSlowPercent = 30;
        public const float ParvumMovementSlowDurationSeconds = 0.8f;

        public const int FugaHealth = 65;
        public const float FugaMovementSpeed = 3.5f;
        public const float FugaAttackRange = 1f;
        public const float FugaAttackDelaySeconds = 1f;
        public const int FugaDamage = 10;

        public const int LongaArmaHealth = 110;
        public const float LongaArmaMovementSpeed = 3f;
        public const float LongaArmaAttackRange = 4f;
        public const float LongaArmaAttackDelaySeconds = 2.5f;
        public const int LongaArmaDamage = 30;

        public const int TergoHealth = 85;
        public const float TergoMovementSpeed = 2f;
        public const float TergoRushMovementSpeed = 15f;
        public const float TergoAttackRange = 2f;
        public const float TergoAttackDelaySeconds = 0f;
        public const int TergoOpeningDamage = 105;
        public const int TergoPierceDamage = 100;
        public const int TergoPinnedDrillDamage = 20;
        public const float TergoPinnedDrillDelaySeconds = 2f;
        public const float TergoInterruptedCowerSeconds = 5f;

        public const int UrzereHealth = 125;
        public const float UrzereMovementSpeed = 1f;
        public const int UrzereSeedAttackBonusPercent = 25;
        public const float UrzereSeedMovementSpeedBonus = 1f;
        public const int UrzereSeedHealthRegenPerSecond = 5;
        public const int UrzereNonSeedVisionPenalty = 1;

        public const int SocietasHealth = 95;
        public const float SocietasMovementSpeed = 4f;
        public const float SocietasAttackRange = 1f;
        public const float SocietasAttackDelaySeconds = 1f;
        public const int SocietasBiologicalDamage = 12;
        public const int SocietasShieldedBiologicalDamage = 2;
        public const int SocietasNonBiologicalDamage = 6;
        public const int SocietasMovementSlowPercent = 50;
        public const float SocietasMovementSlowDurationSeconds = 1f;

        public const int MonstrumHealth = 265;
        public const float MonstrumMovementSpeed = 1.5f;
        public const float MonstrumAttackRange = 1f;
        public const float MonstrumUserAggroRange = 5f;
        public const float MonstrumAttackDelaySeconds = 3f;
        public const int MonstrumDamage = 45;
        public const int MonstrumRoomWideMovementSlowPercent = 80;
        public const float MonstrumRoomWideMovementSlowDurationSeconds = 1f;

        public const int MimesisHealth = 55;
        public const float MimesisMovementSpeed = 2.5f;
        public const float MimesisAttackRange = 1f;
        public const float MimesisAttackDelaySeconds = 0.5f;
        public const int MimesisDamage = 15;
        public const float MimesisInterruptedStopDurationSeconds = 1f;

        private static readonly SeedIntruderKind[] SourceKindOrder =
        {
            SeedIntruderKind.Parvum,
            SeedIntruderKind.Fuga,
            SeedIntruderKind.LongaArma,
            SeedIntruderKind.Tergo,
            SeedIntruderKind.Urzere,
            SeedIntruderKind.Societas,
            SeedIntruderKind.Monstrum,
            SeedIntruderKind.Mimesis
        };

        private static readonly ShipRoomId[] SeedTargetRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom
        };

        public static SeedIntruderKind[] SourceSeedIntruderKinds => (SeedIntruderKind[])SourceKindOrder.Clone();

        public static bool CanCheckSeedIntruder(GameSessionState session)
        {
            return session != null &&
                   session.Phase == GameSessionPhase.Transporting &&
                   session.CompletedTransportCount > 0 &&
                   session.ActiveTransportContract.HasValue &&
                   !session.ActiveTransportContract.Value.IsTutorial;
        }

        public static bool ShouldStartSeedIntruder(GameSessionState session, int checkIndex)
        {
            return ShouldStartSeedIntruder(session, checkIndex, session?.Ship);
        }

        public static bool ShouldStartSeedIntruder(GameSessionState session, int checkIndex, ShipState ship)
        {
            if (!CanCheckSeedIntruder(session) || checkIndex <= 0)
            {
                return false;
            }

            var occurrencePercent = ShipStateRules.CalculateSeedIntruderOccurrencePercent(
                OccurrencePercent,
                ship ?? session.Ship);
            return RollSeedIntruderPercent(CreateSeedIntruderSeed(session, checkIndex)) < occurrencePercent;
        }

        public static SeedIntruderState CreateSeedIntrusion(GameSessionState session, int checkIndex)
        {
            if (session == null || !session.ActiveTransportContract.HasValue)
            {
                throw new ArgumentException("An active transport contract is required for a seed intruder.", nameof(session));
            }

            if (checkIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(checkIndex), "Seed intruder check index must be positive.");
            }

            var seed = CreateSeedIntruderSeed(session, checkIndex);
            var kind = SelectSeedIntruderKind(seed);
            var cargoMaterial = ResolveCargoMaterial(session);
            return CreateSeedIntrusionForSeed(
                kind,
                seed,
                ShipRoomId.Cockpit,
                "seed-" + FormatSeedIntruderKind(kind).ToLowerInvariant() + "-" +
                session.ActiveTransportContract.Value.Id + "-" + checkIndex,
                cargoMaterial);
        }

        public static SeedIntruderState CreateParvumIntrusion(GameSessionState session, int checkIndex)
        {
            if (session == null || !session.ActiveTransportContract.HasValue)
            {
                throw new ArgumentException("An active transport contract is required for a seed intruder.", nameof(session));
            }

            if (checkIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(checkIndex), "Seed intruder check index must be positive.");
            }

            return CreateParvumIntrusionForSeed(
                CreateSeedIntruderSeed(session, checkIndex),
                ShipRoomId.Cockpit,
                "seed-parvum-" + session.ActiveTransportContract.Value.Id + "-" + checkIndex);
        }

        public static SeedIntruderState CreateParvumIntrusionForSeed(
            int seed,
            ShipRoomId playerRoom,
            string attemptId = "seed-parvum-validation")
        {
            return CreateSeedIntrusionForSeed(
                SeedIntruderKind.Parvum,
                seed,
                playerRoom,
                attemptId,
                CargoMaterial.Unspecified);
        }

        public static SeedIntruderState CreateSeedIntrusionForSeed(
            SeedIntruderKind kind,
            int seed,
            ShipRoomId playerRoom,
            string attemptId = "seed-intruder-validation",
            CargoMaterial cargoMaterial = CargoMaterial.Unspecified)
        {
            if (kind == SeedIntruderKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Seed intrusion requires a concrete kind.");
            }

            var targetRoom = SelectSeedTargetRoom(seed);
            var profile = CreateSeedIntruderProfile(kind, targetRoom, cargoMaterial);
            var attempt = IntruderRules.CreateAttempt(attemptId, profile.IntruderDefinition, seed, playerRoom);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, profile.IntruderDefinition);
            return SeedIntruderState.Start(kind, profile.IntruderDefinition, boarded, intruder);
        }

        public static IntruderDefinition CreateParvumDefinition(ShipRoomId targetRoom)
        {
            return CreateSeedIntruderProfile(
                SeedIntruderKind.Parvum,
                targetRoom,
                CargoMaterial.Unspecified).IntruderDefinition;
        }

        public static SeedIntruderProfile[] CreateAllSourceSeedProfiles()
        {
            var profiles = new SeedIntruderProfile[SourceKindOrder.Length];
            for (var i = 0; i < SourceKindOrder.Length; i++)
            {
                profiles[i] = GetProfile(SourceKindOrder[i]);
            }

            return profiles;
        }

        public static SeedIntruderProfile GetProfile(SeedIntruderKind kind)
        {
            return CreateSeedIntruderProfile(kind, ShipRoomId.Cockpit, CargoMaterial.Unspecified);
        }

        public static SeedIntruderProfile CreateSeedIntruderProfile(
            SeedIntruderKind kind,
            ShipRoomId targetRoom,
            CargoMaterial cargoMaterial = CargoMaterial.Unspecified)
        {
            switch (kind)
            {
                case SeedIntruderKind.Parvum:
                    return CreateMetalFeederProfile(
                        kind,
                        ParvumDefinitionId,
                        "Parvum",
                        ParvumHealth,
                        ParvumMovementSpeed,
                        ParvumAttackRange,
                        ParvumAttackDelaySeconds,
                        IntruderMobilityKind.Walking,
                        targetRoom,
                        cargoMaterial,
                        SeedIntruderBehaviorKind.MetalFeeder,
                        ParvumBiologicalDamage,
                        ParvumShieldedBiologicalDamage,
                        ParvumShipFacilityDamage,
                        ParvumShipFacilityDamage,
                        ParvumMovementSlowPercent,
                        ParvumMovementSlowDurationSeconds,
                        followsOtherSeedEntities: false);
                case SeedIntruderKind.Fuga:
                    return CreateMetalFeederProfile(
                        kind,
                        FugaDefinitionId,
                        "Fuga",
                        FugaHealth,
                        FugaMovementSpeed,
                        FugaAttackRange,
                        FugaAttackDelaySeconds,
                        IntruderMobilityKind.Flying,
                        targetRoom,
                        cargoMaterial,
                        SeedIntruderBehaviorKind.MetalFeeder,
                        FugaDamage,
                        0,
                        FugaDamage,
                        FugaDamage,
                        0,
                        0f,
                        followsOtherSeedEntities: false);
                case SeedIntruderKind.LongaArma:
                    return CreateMetalFeederProfile(
                        kind,
                        LongaArmaDefinitionId,
                        "Longa Arma",
                        LongaArmaHealth,
                        LongaArmaMovementSpeed,
                        LongaArmaAttackRange,
                        LongaArmaAttackDelaySeconds,
                        IntruderMobilityKind.Walking,
                        targetRoom,
                        cargoMaterial,
                        SeedIntruderBehaviorKind.MetalFeeder,
                        LongaArmaDamage,
                        0,
                        LongaArmaDamage,
                        LongaArmaDamage,
                        0,
                        0f,
                        followsOtherSeedEntities: false);
                case SeedIntruderKind.Tergo:
                    return new SeedIntruderProfile(
                        kind,
                        CreateDefinition(
                            TergoDefinitionId,
                            "Tergo",
                            IntruderObjectiveType.AttackPlayer,
                            TergoHealth,
                            TergoMovementSpeed,
                            TergoAttackRange,
                            TergoAttackDelaySeconds,
                            CreatePlayerTargetPriorities(),
                            IntruderMobilityKind.Walking),
                        SeedIntruderBehaviorKind.BacklineImpaler,
                        prefersMetal: false,
                        canFeedOnMetalCargo: false,
                        canCreateDestroyedPartPlaceholder: false,
                        canBeExternallyRepelled: false,
                        playerTargetLocked: true,
                        followsOtherSeedEntities: false,
                        anchorsAtTargetRoom: false,
                        biologicalDamage: TergoPierceDamage,
                        shieldedBiologicalDamage: 0,
                        nonBiologicalDamage: 0,
                        shipFacilityDamage: 0,
                        metalCargoDamagePercentPerAttack: 0f,
                        movementSlowPercent: 0,
                        movementSlowDurationSeconds: 0f,
                        playerPostureOnHit: PlayerPostureState.KnockedDownByTergo,
                        tergoOpeningDamage: TergoOpeningDamage,
                        tergoPierceDamage: TergoPierceDamage,
                        tergoPinnedDrillDamage: TergoPinnedDrillDamage,
                        tergoPinnedDrillDelaySeconds: TergoPinnedDrillDelaySeconds,
                        seedAttackDamageBonusPercent: 0,
                        seedMovementSpeedBonus: 0f,
                        seedHealthRegenPerSecond: 0,
                        nonSeedVisionPenalty: 0,
                        roomWideMovementSlowPercent: 0,
                        roomWideMovementSlowDurationSeconds: 0f,
                        hasMimesisVoiceMimicryPlaceholder: false,
                        stopsWhenInjectionInterrupted: false);
                case SeedIntruderKind.Urzere:
                    return new SeedIntruderProfile(
                        kind,
                        CreateDefinition(
                            UrzereDefinitionId,
                            "Urzere",
                            IntruderObjectiveType.OccupyRoom,
                            UrzereHealth,
                            UrzereMovementSpeed,
                            0f,
                            0f,
                            CreateRoomTargetPriorities(targetRoom),
                            IntruderMobilityKind.Walking),
                        SeedIntruderBehaviorKind.SeedSupportEmitter,
                        prefersMetal: false,
                        canFeedOnMetalCargo: false,
                        canCreateDestroyedPartPlaceholder: false,
                        canBeExternallyRepelled: false,
                        playerTargetLocked: false,
                        followsOtherSeedEntities: false,
                        anchorsAtTargetRoom: true,
                        biologicalDamage: 0,
                        shieldedBiologicalDamage: 0,
                        nonBiologicalDamage: 0,
                        shipFacilityDamage: 0,
                        metalCargoDamagePercentPerAttack: 0f,
                        movementSlowPercent: 0,
                        movementSlowDurationSeconds: 0f,
                        playerPostureOnHit: PlayerPostureState.Standing,
                        tergoOpeningDamage: 0,
                        tergoPierceDamage: 0,
                        tergoPinnedDrillDamage: 0,
                        tergoPinnedDrillDelaySeconds: 0f,
                        seedAttackDamageBonusPercent: UrzereSeedAttackBonusPercent,
                        seedMovementSpeedBonus: UrzereSeedMovementSpeedBonus,
                        seedHealthRegenPerSecond: UrzereSeedHealthRegenPerSecond,
                        nonSeedVisionPenalty: UrzereNonSeedVisionPenalty,
                        roomWideMovementSlowPercent: 0,
                        roomWideMovementSlowDurationSeconds: 0f,
                        hasMimesisVoiceMimicryPlaceholder: false,
                        stopsWhenInjectionInterrupted: false);
                case SeedIntruderKind.Societas:
                    return CreateMetalFeederProfile(
                        kind,
                        SocietasDefinitionId,
                        "Societas",
                        SocietasHealth,
                        SocietasMovementSpeed,
                        SocietasAttackRange,
                        SocietasAttackDelaySeconds,
                        IntruderMobilityKind.Walking,
                        targetRoom,
                        cargoMaterial,
                        SeedIntruderBehaviorKind.PackMetalFeeder,
                        SocietasBiologicalDamage,
                        SocietasShieldedBiologicalDamage,
                        SocietasNonBiologicalDamage,
                        SocietasNonBiologicalDamage,
                        SocietasMovementSlowPercent,
                        SocietasMovementSlowDurationSeconds,
                        followsOtherSeedEntities: true);
                case SeedIntruderKind.Monstrum:
                    return CreateMetalFeederProfile(
                        kind,
                        MonstrumDefinitionId,
                        "Monstrum",
                        MonstrumHealth,
                        MonstrumMovementSpeed,
                        MonstrumAttackRange,
                        MonstrumAttackDelaySeconds,
                        IntruderMobilityKind.Walking,
                        targetRoom,
                        cargoMaterial,
                        SeedIntruderBehaviorKind.GiantMetalFeeder,
                        MonstrumDamage,
                        0,
                        MonstrumDamage,
                        MonstrumDamage,
                        0,
                        0f,
                        followsOtherSeedEntities: false);
                case SeedIntruderKind.Mimesis:
                    return new SeedIntruderProfile(
                        kind,
                        CreateDefinition(
                            MimesisDefinitionId,
                            "Mimesis",
                            IntruderObjectiveType.AttackPlayer,
                            MimesisHealth,
                            MimesisMovementSpeed,
                            MimesisAttackRange,
                            MimesisAttackDelaySeconds,
                            CreatePlayerTargetPriorities(),
                            IntruderMobilityKind.Walking),
                        SeedIntruderBehaviorKind.PlayerMimic,
                        prefersMetal: false,
                        canFeedOnMetalCargo: false,
                        canCreateDestroyedPartPlaceholder: false,
                        canBeExternallyRepelled: false,
                        playerTargetLocked: true,
                        followsOtherSeedEntities: false,
                        anchorsAtTargetRoom: false,
                        biologicalDamage: MimesisDamage,
                        shieldedBiologicalDamage: 0,
                        nonBiologicalDamage: 0,
                        shipFacilityDamage: 0,
                        metalCargoDamagePercentPerAttack: 0f,
                        movementSlowPercent: 0,
                        movementSlowDurationSeconds: 0f,
                        playerPostureOnHit: PlayerPostureState.Standing,
                        tergoOpeningDamage: 0,
                        tergoPierceDamage: 0,
                        tergoPinnedDrillDamage: 0,
                        tergoPinnedDrillDelaySeconds: 0f,
                        seedAttackDamageBonusPercent: 0,
                        seedMovementSpeedBonus: 0f,
                        seedHealthRegenPerSecond: 0,
                        nonSeedVisionPenalty: 0,
                        roomWideMovementSlowPercent: 0,
                        roomWideMovementSlowDurationSeconds: 0f,
                        hasMimesisVoiceMimicryPlaceholder: true,
                        stopsWhenInjectionInterrupted: true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported seed intruder kind.");
            }
        }

        public static SeedIntruderTickResult TickParvum(
            SeedIntruderState state,
            ShipState ship,
            CargoState cargo,
            float deltaSeconds)
        {
            return TickParvum(state, ship, cargo, deltaSeconds, ParvumShipFacilityDamage);
        }

        public static SeedIntruderTickResult TickParvum(
            SeedIntruderState state,
            ShipState ship,
            CargoState cargo,
            float deltaSeconds,
            int roomDamagePerAttack)
        {
            if (state.IsActive && state.Kind != SeedIntruderKind.Parvum)
            {
                throw new ArgumentOutOfRangeException(nameof(state), state.Kind, "TickParvum requires a Parvum seed intruder.");
            }

            return TickSeedIntruder(
                state,
                ship,
                cargo,
                deltaSeconds,
                roomDamagePerAttack,
                CargoMaterial.Unspecified);
        }

        public static SeedIntruderTickResult TickSeedIntruder(
            SeedIntruderState state,
            ShipState ship,
            CargoState cargo,
            float deltaSeconds,
            int roomDamagePerAttack = -1,
            CargoMaterial cargoMaterial = CargoMaterial.Unspecified)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (roomDamagePerAttack < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(roomDamagePerAttack), "Room damage cannot be below -1.");
            }

            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!state.IsActive || deltaSeconds <= 0f)
            {
                return new SeedIntruderTickResult(state, ship, cargo, 0, 0, 0f);
            }

            var profile = GetProfile(state.Kind);
            var tickedIntruder = IntruderRules.TickStatusEffects(state.Intruder, deltaSeconds);
            if (!tickedIntruder.IsActive)
            {
                return new SeedIntruderTickResult(
                    state.WithIntruder(tickedIntruder),
                    ship,
                    cargo,
                    0,
                    0,
                    0f);
            }

            var movementBlocked = CombatStatusEffectRules.BlocksMovement(tickedIntruder.StatusEffects);
            var actionBlocked = CombatStatusEffectRules.BlocksActions(tickedIntruder.StatusEffects);
            var intruder = movementBlocked || tickedIntruder.CurrentRoom == tickedIntruder.TargetRoom
                ? tickedIntruder
                : IntruderRules.MoveToReachableTargetRoom(tickedIntruder, ship);
            var elapsed = state.ElapsedSeconds + deltaSeconds;
            var attackAccumulator = actionBlocked
                ? state.AttackAccumulatorSeconds
                : state.AttackAccumulatorSeconds + deltaSeconds;
            var attackCount = 0;
            var roomDamage = 0;
            var playerDamage = 0;
            var cargoDamage = 0f;
            var nextShip = ship;
            var nextCargo = cargo;
            var specialEffectKind = SeedIntruderSpecialEffectKind.None;
            var movementSlowPercent = 0;
            var movementSlowDuration = 0f;
            var playerPosture = PlayerPostureState.Standing;
            var seedAttackBonus = 0;
            var seedMovementBonus = 0f;
            var seedRegen = 0;
            var nonSeedVisionPenalty = 0;
            var roomWideSlowPercent = 0;
            var roomWideSlowDuration = 0f;
            var anchored = profile.AnchorsAtTargetRoom && intruder.CurrentRoom == intruder.TargetRoom;

            if (!actionBlocked && anchored)
            {
                specialEffectKind = SeedIntruderSpecialEffectKind.SeedEntityEmpowerment;
                seedAttackBonus = profile.SeedAttackDamageBonusPercent;
                seedMovementBonus = profile.SeedMovementSpeedBonus;
                seedRegen = profile.SeedHealthRegenPerSecond;
                nonSeedVisionPenalty = profile.NonSeedVisionPenalty;
            }

            if (!actionBlocked && CanApplyAttack(profile, intruder, cargoMaterial))
            {
                if (profile.IntruderDefinition.AttackDelaySeconds <= 0f)
                {
                    attackCount = 1;
                    ApplySeedAttack(
                        profile,
                        intruder,
                        cargoMaterial,
                        roomDamagePerAttack,
                        ref nextShip,
                        ref nextCargo,
                        ref roomDamage,
                        ref cargoDamage,
                        ref playerDamage,
                        ref specialEffectKind,
                        ref movementSlowPercent,
                        ref movementSlowDuration,
                        ref playerPosture,
                        ref roomWideSlowPercent,
                        ref roomWideSlowDuration);
                    attackAccumulator = 0f;
                }
                else
                {
                    while (attackAccumulator + 0.0001f >= profile.IntruderDefinition.AttackDelaySeconds)
                    {
                        attackAccumulator -= profile.IntruderDefinition.AttackDelaySeconds;
                        attackCount++;
                        ApplySeedAttack(
                            profile,
                            intruder,
                            cargoMaterial,
                            roomDamagePerAttack,
                            ref nextShip,
                            ref nextCargo,
                            ref roomDamage,
                            ref cargoDamage,
                            ref playerDamage,
                            ref specialEffectKind,
                            ref movementSlowPercent,
                            ref movementSlowDuration,
                            ref playerPosture,
                            ref roomWideSlowPercent,
                            ref roomWideSlowDuration);
                    }
                }
            }

            var nextState = state.WithProgress(
                intruder,
                elapsed,
                attackAccumulator,
                state.AppliedAttackCount + attackCount,
                state.TotalRoomDamageApplied + roomDamage,
                state.TotalCargoDamagePercentApplied + cargoDamage);

            return new SeedIntruderTickResult(
                nextState,
                nextShip,
                nextCargo,
                attackCount,
                roomDamage,
                cargoDamage,
                specialEffectKind,
                playerDamage,
                movementSlowPercent,
                movementSlowDuration,
                playerPosture,
                seedAttackBonus,
                seedMovementBonus,
                seedRegen,
                nonSeedVisionPenalty,
                roomWideSlowPercent,
                roomWideSlowDuration,
                profile.HasMimesisVoiceMimicryPlaceholder);
        }

        public static SeedIntruderState ApplyStatusEffect(
            SeedIntruderState state,
            CombatStatusEffectApplication application)
        {
            if (!state.IsActive || !application.HasEffect)
            {
                return state;
            }

            return state.WithIntruder(IntruderRules.ApplyStatusEffect(state.Intruder, application));
        }

        public static SeedIntruderState ApplyDamage(SeedIntruderState state, int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Seed intruder damage cannot be negative.");
            }

            if (!state.IsActive || damage == 0)
            {
                return state;
            }

            return state.WithIntruder(state.Intruder.WithDamage(damage));
        }

        public static SeedIntruderState RetargetMetalFeederToPlayer(
            SeedIntruderState state,
            ShipRoomId playerRoom)
        {
            if (!state.IsActive)
            {
                return state;
            }

            var profile = GetProfile(state.Kind);
            if (!profile.PrefersMetal)
            {
                return state;
            }

            return state.WithIntruder(state.Intruder.WithTarget(
                IntruderTargetType.Player,
                playerRoom,
                IntruderObjectiveType.AttackPlayer));
        }

        public static SeedIntruderState ResolveActiveIntruder(SeedIntruderState state, IntruderResolution resolution)
        {
            if (!state.IsActive)
            {
                return state;
            }

            return state.WithIntruder(state.Intruder.Resolve(resolution));
        }

        public static ShipRoomId SelectParvumTargetRoom(int seed)
        {
            return SelectSeedTargetRoom(seed);
        }

        public static ShipRoomId SelectSeedTargetRoom(int seed)
        {
            return SeedTargetRoomOrder[PositiveModulo(seed, SeedTargetRoomOrder.Length)];
        }

        public static SeedIntruderKind SelectSeedIntruderKind(int seed)
        {
            return SourceKindOrder[PositiveModulo(seed, SourceKindOrder.Length)];
        }

        public static bool IsMetalCargo(CargoMaterial material)
        {
            return material == CargoMaterial.CommonMetal ||
                   material == CargoMaterial.RareMetal;
        }

        public static IntruderRelationProfile DetermineSeedRelation(
            SeedIntruderKind sourceKind,
            IntruderFaction targetFaction)
        {
            if (sourceKind == SeedIntruderKind.None || targetFaction == IntruderFaction.None)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceKind), "Seed relation requires concrete source and target.");
            }

            if (sourceKind == SeedIntruderKind.Mimesis && targetFaction != IntruderFaction.SeedEntity)
            {
                return IntruderRules.CreateRelationProfile(IntruderRelationKind.Competitive);
            }

            return IntruderRules.DetermineRelation(IntruderFaction.SeedEntity, targetFaction);
        }

        public static int CreateSeedIntruderSeed(GameSessionState session, int checkIndex)
        {
            if (session == null || !session.ActiveTransportContract.HasValue || checkIndex <= 0)
            {
                return 0;
            }

            var transportNumber = session.CompletedTransportCount + 1;
            return CreateStablePositiveHash(session.ActiveTransportContract.Value.Id, transportNumber, checkIndex);
        }

        public static int RollSeedIntruderPercent(int seed)
        {
            return PositiveModulo(seed, 100);
        }

        public static string FormatSeedIntruderKind(SeedIntruderKind kind)
        {
            switch (kind)
            {
                case SeedIntruderKind.Parvum:
                    return "Parvum";
                case SeedIntruderKind.Fuga:
                    return "Fuga";
                case SeedIntruderKind.LongaArma:
                    return "Longa Arma";
                case SeedIntruderKind.Tergo:
                    return "Tergo";
                case SeedIntruderKind.Urzere:
                    return "Urzere";
                case SeedIntruderKind.Societas:
                    return "Societas";
                case SeedIntruderKind.Monstrum:
                    return "Monstrum";
                case SeedIntruderKind.Mimesis:
                    return "Mimesis";
                default:
                    return "None";
            }
        }

        private static SeedIntruderProfile CreateMetalFeederProfile(
            SeedIntruderKind kind,
            string definitionId,
            string displayName,
            int health,
            float movementSpeed,
            float attackRange,
            float attackDelaySeconds,
            IntruderMobilityKind mobilityKind,
            ShipRoomId targetRoom,
            CargoMaterial cargoMaterial,
            SeedIntruderBehaviorKind behaviorKind,
            int biologicalDamage,
            int shieldedBiologicalDamage,
            int nonBiologicalDamage,
            int shipFacilityDamage,
            int movementSlowPercent,
            float movementSlowDurationSeconds,
            bool followsOtherSeedEntities)
        {
            var targetPriorities = CreateMetalTargetPriorities(targetRoom, cargoMaterial);
            var primaryObjective = targetPriorities[0].TargetType == IntruderTargetType.Cargo
                ? IntruderObjectiveType.AttackCargo
                : IntruderObjectiveType.DestroyShip;
            var roomSlowPercent = behaviorKind == SeedIntruderBehaviorKind.GiantMetalFeeder
                ? MonstrumRoomWideMovementSlowPercent
                : 0;
            var roomSlowDuration = behaviorKind == SeedIntruderBehaviorKind.GiantMetalFeeder
                ? MonstrumRoomWideMovementSlowDurationSeconds
                : 0f;

            return new SeedIntruderProfile(
                kind,
                CreateDefinition(
                    definitionId,
                    displayName,
                    primaryObjective,
                    health,
                    movementSpeed,
                    attackRange,
                    attackDelaySeconds,
                    targetPriorities,
                    mobilityKind),
                behaviorKind,
                prefersMetal: true,
                canFeedOnMetalCargo: true,
                canCreateDestroyedPartPlaceholder: true,
                canBeExternallyRepelled: false,
                playerTargetLocked: false,
                followsOtherSeedEntities: followsOtherSeedEntities,
                anchorsAtTargetRoom: false,
                biologicalDamage: biologicalDamage,
                shieldedBiologicalDamage: shieldedBiologicalDamage,
                nonBiologicalDamage: nonBiologicalDamage,
                shipFacilityDamage: shipFacilityDamage,
                metalCargoDamagePercentPerAttack: IntruderRules.DefaultCargoDamagePercent,
                movementSlowPercent: movementSlowPercent,
                movementSlowDurationSeconds: movementSlowDurationSeconds,
                playerPostureOnHit: PlayerPostureState.Standing,
                tergoOpeningDamage: 0,
                tergoPierceDamage: 0,
                tergoPinnedDrillDamage: 0,
                tergoPinnedDrillDelaySeconds: 0f,
                seedAttackDamageBonusPercent: 0,
                seedMovementSpeedBonus: 0f,
                seedHealthRegenPerSecond: 0,
                nonSeedVisionPenalty: 0,
                roomWideMovementSlowPercent: roomSlowPercent,
                roomWideMovementSlowDurationSeconds: roomSlowDuration,
                hasMimesisVoiceMimicryPlaceholder: false,
                stopsWhenInjectionInterrupted: false);
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
                IntruderFaction.SeedEntity,
                objective,
                maxHealth,
                movementSpeed,
                attackRange,
                attackDelaySeconds,
                targetPriorities,
                mobilityKind);
        }

        private static IntruderTargetPriority[] CreateMetalTargetPriorities(
            ShipRoomId targetRoom,
            CargoMaterial cargoMaterial)
        {
            if (IsMetalCargo(cargoMaterial))
            {
                return new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Cargo, ShipRoomId.CargoHold, 0),
                    new IntruderTargetPriority(IntruderTargetType.Ship, targetRoom, 1)
                };
            }

            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Ship, targetRoom, 0)
            };
        }

        private static IntruderTargetPriority[] CreatePlayerTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Player, ShipRoomId.Cockpit, 0)
            };
        }

        private static IntruderTargetPriority[] CreateRoomTargetPriorities(ShipRoomId targetRoom)
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Room, targetRoom, 0)
            };
        }

        private static bool CanApplyAttack(
            SeedIntruderProfile profile,
            IntruderEntityState intruder,
            CargoMaterial cargoMaterial)
        {
            switch (intruder.TargetType)
            {
                case IntruderTargetType.Cargo:
                    return profile.CanFeedOnMetalCargo &&
                           profile.MetalCargoDamagePercentPerAttack > 0f &&
                           IsMetalCargo(cargoMaterial);
                case IntruderTargetType.Ship:
                case IntruderTargetType.Room:
                    return profile.CanDamageShip;
                case IntruderTargetType.Player:
                    return profile.CanAttackPlayer;
                default:
                    return false;
            }
        }

        private static void ApplySeedAttack(
            SeedIntruderProfile profile,
            IntruderEntityState intruder,
            CargoMaterial cargoMaterial,
            int roomDamagePerAttack,
            ref ShipState ship,
            ref CargoState cargo,
            ref int roomDamage,
            ref float cargoDamage,
            ref int playerDamage,
            ref SeedIntruderSpecialEffectKind specialEffectKind,
            ref int movementSlowPercent,
            ref float movementSlowDuration,
            ref PlayerPostureState playerPosture,
            ref int roomWideSlowPercent,
            ref float roomWideSlowDuration)
        {
            switch (intruder.TargetType)
            {
                case IntruderTargetType.Cargo:
                    if (profile.CanFeedOnMetalCargo && IsMetalCargo(cargoMaterial))
                    {
                        cargo = cargo.WithDamagePercent(profile.MetalCargoDamagePercentPerAttack);
                        cargoDamage += profile.MetalCargoDamagePercentPerAttack;
                    }

                    break;
                case IntruderTargetType.Ship:
                case IntruderTargetType.Room:
                    var damage = roomDamagePerAttack >= 0
                        ? roomDamagePerAttack
                        : profile.ShipFacilityDamage;
                    if (damage > 0)
                    {
                        var room = ship.GetRoom(intruder.CurrentRoom);
                        ship = ship.WithRoom(intruder.CurrentRoom, room.WithDamage(damage));
                        roomDamage += damage;
                    }

                    if (profile.PrimarySpecialEffectKind == SeedIntruderSpecialEffectKind.RoomWideMovementSlow)
                    {
                        specialEffectKind = SeedIntruderSpecialEffectKind.RoomWideMovementSlow;
                        roomWideSlowPercent = profile.RoomWideMovementSlowPercent;
                        roomWideSlowDuration = profile.RoomWideMovementSlowDurationSeconds;
                    }

                    break;
                case IntruderTargetType.Player:
                    var playerHit = profile.TergoPierceDamage > 0
                        ? profile.TergoPierceDamage
                        : profile.BiologicalDamage;
                    playerDamage += playerHit;
                    specialEffectKind = profile.PrimarySpecialEffectKind;
                    movementSlowPercent = profile.MovementSlowPercent;
                    movementSlowDuration = profile.MovementSlowDurationSeconds;
                    playerPosture = profile.PlayerPostureOnHit;
                    break;
            }
        }

        private static CargoMaterial ResolveCargoMaterial(GameSessionState session)
        {
            if (session == null || !session.ActiveTransportContract.HasValue)
            {
                return CargoMaterial.Unspecified;
            }

            try
            {
                var catalog = DetailedContractCatalogRules.CreateDefaultStepTwoCatalog();
                var contract = DetailedContractCatalogRules.FindContract(
                    catalog,
                    session.ActiveTransportContract.Value.Id);
                return DetailedContractCatalogRules.FindCargo(catalog, contract.CargoId).Material;
            }
            catch (InvalidOperationException)
            {
                return CargoMaterial.Unspecified;
            }
        }

        private static int CreateStablePositiveHash(string text, int transportNumber, int checkIndex)
        {
            unchecked
            {
                var hash = 2166136261u;
                for (var i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }

                hash ^= (uint)transportNumber;
                hash *= 16777619u;
                hash ^= (uint)checkIndex;
                hash *= 16777619u;
                return (int)(hash & 0x7fffffffu);
            }
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
