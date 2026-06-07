using System;

namespace Bellerophon.Core.Session
{
    public enum TransportHazardType
    {
        None,
        AsteroidFieldSmall,
        AsteroidField = AsteroidFieldSmall,
        AsteroidFieldLarge,
        CargoFreedomLeagueRegion,
        SpacePirateRegion,
        AlienLifeRegion,
        ConcealedBlackHole
    }

    public enum TransportHazardResolution
    {
        None,
        Neutralized,
        Avoided,
        GlancingHit,
        DirectHit
    }

    public readonly struct ShipRoomHazardDamage
    {
        public ShipRoomHazardDamage(ShipRoomId roomId, int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Hazard damage cannot be negative.");
            }

            RoomId = roomId;
            Damage = damage;
        }

        public ShipRoomId RoomId { get; }

        public int Damage { get; }
    }

    public readonly struct TransportHazardUnlockState
    {
        public TransportHazardUnlockState(
            bool cargoFreedomLeagueUnlocked,
            bool spacePirateUnlocked,
            bool alienLifeUnlocked,
            bool concealedBlackHoleUnlocked)
        {
            CargoFreedomLeagueUnlocked = cargoFreedomLeagueUnlocked;
            SpacePirateUnlocked = spacePirateUnlocked;
            AlienLifeUnlocked = alienLifeUnlocked;
            ConcealedBlackHoleUnlocked = concealedBlackHoleUnlocked;
        }

        public bool CargoFreedomLeagueUnlocked { get; }

        public bool SpacePirateUnlocked { get; }

        public bool AlienLifeUnlocked { get; }

        public bool ConcealedBlackHoleUnlocked { get; }

        public static TransportHazardUnlockState None => new TransportHazardUnlockState(false, false, false, false);

        public TransportHazardUnlockState WithFameScore(int fameScore)
        {
            return new TransportHazardUnlockState(
                CargoFreedomLeagueUnlocked || fameScore >= TransportHazardRules.CargoFreedomLeagueFameThreshold,
                SpacePirateUnlocked || fameScore >= TransportHazardRules.SpacePirateFameThreshold,
                AlienLifeUnlocked || fameScore >= TransportHazardRules.AlienLifeFameThreshold,
                ConcealedBlackHoleUnlocked || fameScore >= TransportHazardRules.ConcealedBlackHoleFameThreshold);
        }

        public bool IsUnlocked(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                case TransportHazardType.AsteroidFieldLarge:
                    return true;
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return CargoFreedomLeagueUnlocked;
                case TransportHazardType.SpacePirateRegion:
                    return SpacePirateUnlocked;
                case TransportHazardType.AlienLifeRegion:
                    return AlienLifeUnlocked;
                case TransportHazardType.ConcealedBlackHole:
                    return ConcealedBlackHoleUnlocked;
                default:
                    return false;
            }
        }
    }

    public readonly struct TransportHazardState
    {
        private TransportHazardState(
            TransportHazardType hazardType,
            int seed,
            int durationSeconds,
            float elapsedSeconds,
            float manualAvoidanceSeconds)
        {
            if (durationSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Hazard duration cannot be negative.");
            }

            HazardType = hazardType;
            Seed = seed;
            DurationSeconds = durationSeconds;
            ElapsedSeconds = Clamp(elapsedSeconds, 0f, durationSeconds);
            ManualAvoidanceSeconds = Clamp(manualAvoidanceSeconds, 0f, DurationSeconds);
        }

        public TransportHazardType HazardType { get; }

        public int Seed { get; }

        public int DurationSeconds { get; }

        public float ElapsedSeconds { get; }

        public float ManualAvoidanceSeconds { get; }

        public bool HasActiveHazard => HazardType != TransportHazardType.None;

        public bool IsComplete => HasActiveHazard && ElapsedSeconds >= DurationSeconds;

        public float RemainingSeconds => Math.Max(0f, DurationSeconds - ElapsedSeconds);

        public float ManualAvoidanceRatio => DurationSeconds <= 0
            ? 0f
            : Clamp(ManualAvoidanceSeconds / DurationSeconds, 0f, 1f);

        public static TransportHazardState None => new TransportHazardState(
            TransportHazardType.None,
            0,
            0,
            0f,
            0f);

        public static TransportHazardState StartAsteroidField(int seed, int durationSeconds)
        {
            return StartAsteroidFieldSmall(seed, durationSeconds);
        }

        public static TransportHazardState StartAsteroidFieldSmall(int seed, int durationSeconds)
        {
            return Start(TransportHazardType.AsteroidFieldSmall, seed, durationSeconds);
        }

        public static TransportHazardState StartAsteroidFieldLarge(int seed, int durationSeconds)
        {
            return Start(TransportHazardType.AsteroidFieldLarge, seed, durationSeconds);
        }

        public static TransportHazardState Start(TransportHazardType hazardType, int seed, int durationSeconds)
        {
            if (hazardType == TransportHazardType.None)
            {
                return None;
            }

            if (hazardType == TransportHazardType.ConcealedBlackHole)
            {
                throw new InvalidOperationException("Concealed black hole is deferred to later hazard expansion.");
            }

            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Transport hazard duration must be positive.");
            }

            return new TransportHazardState(hazardType, seed, durationSeconds, 0f, 0f);
        }

        public TransportHazardState Tick(float deltaSeconds, bool manualAvoiding)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!HasActiveHazard || deltaSeconds <= 0f)
            {
                return this;
            }

            var nextElapsed = Clamp(ElapsedSeconds + deltaSeconds, 0f, DurationSeconds);
            var advancedSeconds = nextElapsed - ElapsedSeconds;
            var nextManualSeconds = manualAvoiding
                ? Clamp(ManualAvoidanceSeconds + advancedSeconds, 0f, DurationSeconds)
                : ManualAvoidanceSeconds;
            return new TransportHazardState(
                HazardType,
                Seed,
                DurationSeconds,
                nextElapsed,
                nextManualSeconds);
        }

        public TransportHazardState ApplyDurationReduction(int reductionSeconds)
        {
            if (reductionSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reductionSeconds), "Duration reduction cannot be negative.");
            }

            if (!HasActiveHazard || reductionSeconds == 0)
            {
                return this;
            }

            var nextElapsed = Clamp(ElapsedSeconds + reductionSeconds, 0f, DurationSeconds);
            var nextManualSeconds = Clamp(ManualAvoidanceSeconds + reductionSeconds, 0f, DurationSeconds);
            return new TransportHazardState(
                HazardType,
                Seed,
                DurationSeconds,
                nextElapsed,
                nextManualSeconds);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }

    public readonly struct TransportHazardResult
    {
        private static readonly ShipRoomHazardDamage[] EmptyDamage = new ShipRoomHazardDamage[0];
        private readonly ShipRoomHazardDamage[] roomDamages;

        public TransportHazardResult(
            TransportHazardType hazardType,
            TransportHazardResolution resolution,
            ShipRoomHazardDamage[] damages,
            int boardingEventCount = 0,
            int bombardmentHitCount = 0)
        {
            if (boardingEventCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boardingEventCount), "Boarding event count cannot be negative.");
            }

            if (bombardmentHitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bombardmentHitCount), "Bombardment hit count cannot be negative.");
            }

            HazardType = hazardType;
            Resolution = resolution;
            roomDamages = damages ?? EmptyDamage;
            BoardingEventCount = boardingEventCount;
            BombardmentHitCount = bombardmentHitCount;
        }

        public TransportHazardType HazardType { get; }

        public TransportHazardResolution Resolution { get; }

        public ShipRoomHazardDamage[] RoomDamages => roomDamages ?? EmptyDamage;

        public int BoardingEventCount { get; }

        public int BombardmentHitCount { get; }

        public bool HasResult => Resolution != TransportHazardResolution.None;

        public static TransportHazardResult None => new TransportHazardResult(
            TransportHazardType.None,
            TransportHazardResolution.None,
            EmptyDamage);
    }

    public static class TransportHazardRules
    {
        public const int AsteroidFieldOccurrencePercent = 30;
        public const int AsteroidFieldOccurrenceCheckIntervalSeconds = 5;
        public const int MinimumAsteroidFieldDurationSeconds = 10;
        public const int MaximumAsteroidFieldDurationSeconds = 120;
        public const int AsteroidFieldDurationVarianceSeconds = MaximumAsteroidFieldDurationSeconds - MinimumAsteroidFieldDurationSeconds + 1;
        public const int AsteroidFieldAutoDamageCheckIntervalSeconds = 5;
        public const int AsteroidFieldAutoDamagePercent = 70;
        public const float AsteroidFieldAvoidanceRatioForClear = 0.5f;
        public const int AsteroidFieldDirectHitRoomCount = 1;
        public const int AsteroidFieldSmallDamage = 10;
        public const int AsteroidFieldLargeDamage = 20;
        public const int AsteroidFieldDirectHitDamage = AsteroidFieldSmallDamage;
        public const int AsteroidFieldGlancingHitDamage = AsteroidFieldSmallDamage;
        public const int AsteroidFieldSmallTargetHealth = 400;
        public const int AsteroidFieldLargeTargetHealth = 1000;
        public const int AsteroidFieldManualBoosterReductionSeconds = 10;

        public const int CargoFreedomLeagueFameThreshold = 1800;
        public const int CargoFreedomLeagueOccurrencePercent = 15;
        public const int CargoFreedomLeagueOccurrenceCheckIntervalSeconds = 10;
        public const int MinimumCargoFreedomLeagueDurationSeconds = 5;
        public const int MaximumCargoFreedomLeagueDurationSeconds = 35;
        public const int CargoFreedomLeagueBoardingCheckIntervalSeconds = 2;
        public const int CargoFreedomLeagueBoardingPercent = 70;
        public const int CargoFreedomLeagueManualBoosterReductionSeconds = 10;

        public const int SpacePirateFameThreshold = 3000;
        public const int SpacePirateOccurrencePercent = 5;
        public const int SpacePirateOccurrenceCheckIntervalSeconds = 10;
        public const int MinimumSpacePirateDurationSeconds = 60;
        public const int MaximumSpacePirateDurationSeconds = 300;
        public const int SpacePirateBoardingCheckIntervalSeconds = 5;
        public const int SpacePirateBoardingPercent = 90;
        public const int SpacePirateBombardmentPercent = 30;
        public const int SpacePirateBombardmentDamage = 20;
        public const int SpacePirateManualBoosterReductionSeconds = 45;

        public const int AlienLifeFameThreshold = 900;
        public const int AlienLifeOccurrencePercent = 10;
        public const int AlienLifeOccurrenceCheckIntervalSeconds = 5;
        public const int MinimumAlienLifeDurationSeconds = 30;
        public const int MaximumAlienLifeDurationSeconds = 300;
        public const int AlienLifeBoardingCheckIntervalSeconds = 3;
        public const int AlienLifeBoardingPercent = 80;
        public const int AlienLifeManualBoosterReductionSeconds = 10;

        public const int ConcealedBlackHoleFameThreshold = 4500;
        public const int ConcealedBlackHoleOccurrencePercent = 1;
        public const int ConcealedBlackHoleOccurrenceCheckIntervalSeconds = 10;

        private static readonly ShipRoomId[] DamageRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom
        };

        public static bool CanCheckTransportHazard(GameSessionState session)
        {
            return session != null &&
                   session.Phase == GameSessionPhase.Transporting &&
                   session.CompletedTransportCount > 0 &&
                   session.ActiveTransportContract.HasValue &&
                   !session.ActiveTransportContract.Value.IsTutorial;
        }

        public static bool ShouldStartAsteroidField(GameSessionState session)
        {
            return ShouldStartAsteroidField(session, 1);
        }

        public static bool ShouldStartAsteroidField(GameSessionState session, int checkIndex)
        {
            if (!CanCheckTransportHazard(session))
            {
                return false;
            }

            return RollPercent(CreateHazardSeed(session, TransportHazardType.AsteroidFieldSmall, checkIndex)) <
                   AsteroidFieldOccurrencePercent;
        }

        public static TransportHazardState CreateAsteroidField(GameSessionState session)
        {
            return CreateAsteroidField(session, 1);
        }

        public static TransportHazardState CreateAsteroidField(GameSessionState session, int checkIndex)
        {
            if (!CanCheckTransportHazard(session))
            {
                throw new ArgumentException("A post-tutorial active transport contract is required for an asteroid hazard.", nameof(session));
            }

            var seed = CreateHazardSeed(session, TransportHazardType.AsteroidFieldSmall, checkIndex);
            var type = PositiveModulo(seed, 2) == 0
                ? TransportHazardType.AsteroidFieldSmall
                : TransportHazardType.AsteroidFieldLarge;
            return TransportHazardState.Start(
                type,
                seed,
                CreateDuration(seed, MinimumAsteroidFieldDurationSeconds, MaximumAsteroidFieldDurationSeconds));
        }

        public static bool ShouldStartHazard(
            GameSessionState session,
            TransportHazardType hazardType,
            int checkIndex)
        {
            if (!CanCheckTransportHazard(session) ||
                hazardType == TransportHazardType.None ||
                hazardType == TransportHazardType.ConcealedBlackHole ||
                !session.TransportHazardUnlocks.IsUnlocked(hazardType))
            {
                return false;
            }

            if (hazardType == TransportHazardType.AsteroidFieldLarge)
            {
                hazardType = TransportHazardType.AsteroidFieldSmall;
            }

            return RollPercent(CreateHazardSeed(session, hazardType, checkIndex)) <
                   GetOccurrencePercent(hazardType);
        }

        public static TransportHazardState CreateHazard(
            GameSessionState session,
            TransportHazardType hazardType,
            int checkIndex)
        {
            if (!CanCheckTransportHazard(session))
            {
                throw new ArgumentException("A post-tutorial active transport contract is required for a transport hazard.", nameof(session));
            }

            if (hazardType == TransportHazardType.AsteroidFieldLarge)
            {
                hazardType = TransportHazardType.AsteroidFieldSmall;
            }

            if (hazardType == TransportHazardType.AsteroidFieldSmall)
            {
                return CreateAsteroidField(session, checkIndex);
            }

            if (hazardType == TransportHazardType.ConcealedBlackHole)
            {
                throw new InvalidOperationException("Concealed black hole is deferred to later hazard expansion.");
            }

            if (!session.TransportHazardUnlocks.IsUnlocked(hazardType))
            {
                throw new InvalidOperationException(FormatHazardType(hazardType) + " is not unlocked in this session.");
            }

            var seed = CreateHazardSeed(session, hazardType, checkIndex);
            return TransportHazardState.Start(
                hazardType,
                seed,
                CreateDuration(seed, GetMinimumDurationSeconds(hazardType), GetMaximumDurationSeconds(hazardType)));
        }

        public static TransportHazardResult ResolveAsteroidField(TransportHazardState hazard)
        {
            return ResolveTransportHazard(hazard, false);
        }

        public static TransportHazardResult ResolveAsteroidField(TransportHazardState hazard, bool neutralizedByTurret)
        {
            return ResolveTransportHazard(hazard, neutralizedByTurret);
        }

        public static TransportHazardResult ResolveTransportHazard(TransportHazardState hazard)
        {
            return ResolveTransportHazard(hazard, false);
        }

        public static TransportHazardResult ResolveTransportHazard(
            TransportHazardState hazard,
            bool neutralizedByTurret)
        {
            if (hazard.HazardType == TransportHazardType.None)
            {
                return TransportHazardResult.None;
            }

            if (neutralizedByTurret)
            {
                return new TransportHazardResult(
                    hazard.HazardType,
                    TransportHazardResolution.Neutralized,
                    new ShipRoomHazardDamage[0]);
            }

            if (hazard.ManualAvoidanceRatio >= AsteroidFieldAvoidanceRatioForClear)
            {
                return new TransportHazardResult(
                    hazard.HazardType,
                    TransportHazardResolution.Avoided,
                    new ShipRoomHazardDamage[0]);
            }

            switch (hazard.HazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                case TransportHazardType.AsteroidFieldLarge:
                    return ResolveAsteroidHazard(hazard);
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return ResolveBoardingHazard(
                        hazard,
                        CargoFreedomLeagueBoardingCheckIntervalSeconds,
                        CargoFreedomLeagueBoardingPercent);
                case TransportHazardType.SpacePirateRegion:
                    return ResolvePirateHazard(hazard);
                case TransportHazardType.AlienLifeRegion:
                    return ResolveBoardingHazard(
                        hazard,
                        AlienLifeBoardingCheckIntervalSeconds,
                        AlienLifeBoardingPercent);
                case TransportHazardType.ConcealedBlackHole:
                    throw new InvalidOperationException("Concealed black hole is deferred to later hazard expansion.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Unsupported transport hazard type.");
            }
        }

        public static int GetManualFlightBoosterReductionSeconds(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                case TransportHazardType.AsteroidFieldLarge:
                    return AsteroidFieldManualBoosterReductionSeconds;
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return CargoFreedomLeagueManualBoosterReductionSeconds;
                case TransportHazardType.SpacePirateRegion:
                    return SpacePirateManualBoosterReductionSeconds;
                case TransportHazardType.AlienLifeRegion:
                    return AlienLifeManualBoosterReductionSeconds;
                default:
                    return 0;
            }
        }

        public static TransportHazardState ApplyManualFlightBooster(TransportHazardState hazard)
        {
            return hazard.ApplyDurationReduction(
                GetManualFlightBoosterReductionSeconds(hazard.HazardType));
        }

        public static ExternalTargetState CreateExternalTarget(TransportHazardState hazard)
        {
            switch (hazard.HazardType)
            {
                case TransportHazardType.None:
                    return ExternalTargetState.None;
                case TransportHazardType.AsteroidFieldSmall:
                    return CreateAsteroidExternalTarget(hazard, AsteroidFieldSmallTargetHealth);
                case TransportHazardType.AsteroidFieldLarge:
                    return CreateAsteroidExternalTarget(hazard, AsteroidFieldLargeTargetHealth);
                default:
                    return ExternalTargetState.None;
            }
        }

        public static ShipState ApplyHazardResult(ShipState ship, TransportHazardResult result)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var nextShip = ship;
            var damages = result.RoomDamages;
            for (var i = 0; i < damages.Length; i++)
            {
                var damage = damages[i];
                var room = nextShip.GetRoom(damage.RoomId);
                nextShip = nextShip.WithRoom(damage.RoomId, room.WithDamage(damage.Damage));
            }

            return nextShip;
        }

        public static int CreateAsteroidSeed(GameSessionState session)
        {
            return CreateHazardSeed(session, TransportHazardType.AsteroidFieldSmall, 1);
        }

        public static int CreateHazardSeed(
            GameSessionState session,
            TransportHazardType hazardType,
            int checkIndex)
        {
            if (session == null || !session.ActiveTransportContract.HasValue)
            {
                return 0;
            }

            if (checkIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(checkIndex), "Hazard check index must be positive.");
            }

            var transportNumber = session.CompletedTransportCount + 1;
            return CreateStablePositiveHash(
                session.ActiveTransportContract.Value.Id,
                transportNumber,
                (int)hazardType,
                checkIndex);
        }

        public static string FormatHazardType(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                    return "Asteroid Field Small";
                case TransportHazardType.AsteroidFieldLarge:
                    return "Asteroid Field Large";
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return "Cargo Freedom League Region";
                case TransportHazardType.SpacePirateRegion:
                    return "Space Pirate Region";
                case TransportHazardType.AlienLifeRegion:
                    return "Alien Life Region";
                case TransportHazardType.ConcealedBlackHole:
                    return "Concealed Black Hole";
                default:
                    return "None";
            }
        }

        public static int GetOccurrenceCheckIntervalSeconds(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                case TransportHazardType.AsteroidFieldLarge:
                    return AsteroidFieldOccurrenceCheckIntervalSeconds;
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return CargoFreedomLeagueOccurrenceCheckIntervalSeconds;
                case TransportHazardType.SpacePirateRegion:
                    return SpacePirateOccurrenceCheckIntervalSeconds;
                case TransportHazardType.AlienLifeRegion:
                    return AlienLifeOccurrenceCheckIntervalSeconds;
                case TransportHazardType.ConcealedBlackHole:
                    return ConcealedBlackHoleOccurrenceCheckIntervalSeconds;
                default:
                    return 0;
            }
        }

        public static int GetFameThreshold(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return CargoFreedomLeagueFameThreshold;
                case TransportHazardType.SpacePirateRegion:
                    return SpacePirateFameThreshold;
                case TransportHazardType.AlienLifeRegion:
                    return AlienLifeFameThreshold;
                case TransportHazardType.ConcealedBlackHole:
                    return ConcealedBlackHoleFameThreshold;
                default:
                    return 0;
            }
        }

        private static TransportHazardResult ResolveAsteroidHazard(TransportHazardState hazard)
        {
            var damage = hazard.HazardType == TransportHazardType.AsteroidFieldLarge
                ? AsteroidFieldLargeDamage
                : AsteroidFieldSmallDamage;

            if (hazard.ManualAvoidanceSeconds > 0f)
            {
                return new TransportHazardResult(
                    hazard.HazardType,
                    TransportHazardResolution.GlancingHit,
                    CreateRoomDamage(hazard.Seed, 1, damage));
            }

            var damages = CreateRepeatedRoomDamage(
                hazard.Seed,
                GetCheckCount(hazard.DurationSeconds, AsteroidFieldAutoDamageCheckIntervalSeconds),
                AsteroidFieldAutoDamagePercent,
                damage);
            return new TransportHazardResult(
                hazard.HazardType,
                damages.Length > 0 ? TransportHazardResolution.DirectHit : TransportHazardResolution.Avoided,
                damages);
        }

        private static TransportHazardResult ResolveBoardingHazard(
            TransportHazardState hazard,
            int intervalSeconds,
            int percent)
        {
            var eventCount = CountSuccessfulChecks(
                hazard.Seed,
                GetCheckCount(hazard.DurationSeconds, intervalSeconds),
                percent);
            return new TransportHazardResult(
                hazard.HazardType,
                eventCount > 0 ? TransportHazardResolution.DirectHit : TransportHazardResolution.Avoided,
                new ShipRoomHazardDamage[0],
                eventCount);
        }

        private static TransportHazardResult ResolvePirateHazard(TransportHazardState hazard)
        {
            var checkCount = GetCheckCount(hazard.DurationSeconds, SpacePirateBoardingCheckIntervalSeconds);
            var boardingCount = CountSuccessfulChecks(hazard.Seed, checkCount, SpacePirateBoardingPercent);
            var bombardmentCount = CountSuccessfulChecks(hazard.Seed ^ 0x3a5f19, checkCount, SpacePirateBombardmentPercent);
            var damages = CreateRepeatedRoomDamage(
                hazard.Seed ^ 0x6d2b79,
                bombardmentCount,
                100,
                SpacePirateBombardmentDamage);

            return new TransportHazardResult(
                hazard.HazardType,
                boardingCount > 0 || bombardmentCount > 0
                    ? TransportHazardResolution.DirectHit
                    : TransportHazardResolution.Avoided,
                damages,
                boardingCount,
                bombardmentCount);
        }

        private static ExternalTargetState CreateAsteroidExternalTarget(TransportHazardState hazard, int maxHealth)
        {
            return new ExternalTargetState(
                "asteroid-field-" + hazard.Seed,
                ExternalTargetType.Asteroid,
                maxHealth,
                maxHealth,
                CreateTargetCoordinate(hazard.Seed, 17, 0.58f),
                CreateTargetCoordinate(hazard.Seed, 31, 0.42f),
                ManualTurretState.DefaultAsteroidHitRadius);
        }

        private static int GetOccurrencePercent(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                case TransportHazardType.AsteroidFieldLarge:
                    return AsteroidFieldOccurrencePercent;
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return CargoFreedomLeagueOccurrencePercent;
                case TransportHazardType.SpacePirateRegion:
                    return SpacePirateOccurrencePercent;
                case TransportHazardType.AlienLifeRegion:
                    return AlienLifeOccurrencePercent;
                case TransportHazardType.ConcealedBlackHole:
                    return ConcealedBlackHoleOccurrencePercent;
                default:
                    return 0;
            }
        }

        private static int GetMinimumDurationSeconds(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                case TransportHazardType.AsteroidFieldLarge:
                    return MinimumAsteroidFieldDurationSeconds;
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return MinimumCargoFreedomLeagueDurationSeconds;
                case TransportHazardType.SpacePirateRegion:
                    return MinimumSpacePirateDurationSeconds;
                case TransportHazardType.AlienLifeRegion:
                    return MinimumAlienLifeDurationSeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hazardType), hazardType, null);
            }
        }

        private static int GetMaximumDurationSeconds(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidFieldSmall:
                case TransportHazardType.AsteroidFieldLarge:
                    return MaximumAsteroidFieldDurationSeconds;
                case TransportHazardType.CargoFreedomLeagueRegion:
                    return MaximumCargoFreedomLeagueDurationSeconds;
                case TransportHazardType.SpacePirateRegion:
                    return MaximumSpacePirateDurationSeconds;
                case TransportHazardType.AlienLifeRegion:
                    return MaximumAlienLifeDurationSeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hazardType), hazardType, null);
            }
        }

        private static int CreateDuration(int seed, int minSeconds, int maxSeconds)
        {
            if (maxSeconds < minSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSeconds), "Maximum duration must be greater than or equal to minimum duration.");
            }

            return minSeconds + PositiveModulo(seed, maxSeconds - minSeconds + 1);
        }

        private static int GetCheckCount(int durationSeconds, int intervalSeconds)
        {
            if (durationSeconds <= 0 || intervalSeconds <= 0)
            {
                return 0;
            }

            return Math.Max(1, durationSeconds / intervalSeconds);
        }

        private static int CountSuccessfulChecks(int seed, int checkCount, int percent)
        {
            if (checkCount <= 0 || percent <= 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < checkCount; i++)
            {
                if (RollPercent(seed + i * 977) < percent)
                {
                    count++;
                }
            }

            return count;
        }

        private static ShipRoomHazardDamage[] CreateRepeatedRoomDamage(
            int seed,
            int checkCount,
            int percent,
            int damage)
        {
            if (checkCount <= 0 || percent <= 0 || damage <= 0)
            {
                return new ShipRoomHazardDamage[0];
            }

            var successfulChecks = CountSuccessfulChecks(seed, checkCount, percent);
            var damages = new ShipRoomHazardDamage[successfulChecks];
            var damageIndex = 0;
            for (var i = 0; i < checkCount; i++)
            {
                var checkSeed = seed + i * 977;
                if (RollPercent(checkSeed) >= percent)
                {
                    continue;
                }

                damages[damageIndex] = CreateRoomDamage(checkSeed, 1, damage)[0];
                damageIndex++;
            }

            return damages;
        }

        private static ShipRoomHazardDamage[] CreateRoomDamage(int seed, int roomCount, int damage)
        {
            if (roomCount <= 0)
            {
                return new ShipRoomHazardDamage[0];
            }

            var count = Math.Min(roomCount, DamageRoomOrder.Length);
            var rooms = (ShipRoomId[])DamageRoomOrder.Clone();
            var random = new Random(seed);
            var damages = new ShipRoomHazardDamage[count];
            for (var i = 0; i < count; i++)
            {
                var selectedIndex = random.Next(i, rooms.Length);
                var selected = rooms[selectedIndex];
                rooms[selectedIndex] = rooms[i];
                rooms[i] = selected;
                damages[i] = new ShipRoomHazardDamage(selected, damage);
            }

            return damages;
        }

        private static int CreateStablePositiveHash(
            string text,
            int transportNumber,
            int hazardType,
            int checkIndex)
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
                hash ^= (uint)hazardType;
                hash *= 16777619u;
                hash ^= (uint)checkIndex;
                hash *= 16777619u;
                return (int)(hash & 0x7fffffffu);
            }
        }

        private static int RollPercent(int seed)
        {
            return PositiveModulo(seed, 100);
        }

        private static float CreateTargetCoordinate(int seed, int salt, float range)
        {
            var value = PositiveModulo(seed ^ (salt * 1103515245), 1000) / 999f;
            return (value * 2f - 1f) * range;
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
