using System;

namespace Bellerophon.Core.Session
{
    public enum TransportHazardType
    {
        None,
        AsteroidField
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
            if (durationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Asteroid field duration must be positive.");
            }

            return new TransportHazardState(
                TransportHazardType.AsteroidField,
                seed,
                durationSeconds,
                0f,
                0f);
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
            ShipRoomHazardDamage[] damages)
        {
            HazardType = hazardType;
            Resolution = resolution;
            roomDamages = damages ?? EmptyDamage;
        }

        public TransportHazardType HazardType { get; }

        public TransportHazardResolution Resolution { get; }

        public ShipRoomHazardDamage[] RoomDamages => roomDamages ?? EmptyDamage;

        public bool HasResult => Resolution != TransportHazardResolution.None;

        public static TransportHazardResult None => new TransportHazardResult(
            TransportHazardType.None,
            TransportHazardResolution.None,
            EmptyDamage);
    }

    public static class TransportHazardRules
    {
        public const int AsteroidFieldOccurrencePercent = 100;
        public const int MinimumAsteroidFieldDurationSeconds = 10;
        public const int AsteroidFieldDurationVarianceSeconds = 5;
        public const float AsteroidFieldAvoidanceRatioForClear = 0.5f;
        public const int AsteroidFieldDirectHitRoomCount = 2;
        public const int AsteroidFieldDirectHitDamage = 25;
        public const int AsteroidFieldGlancingHitDamage = 10;

        private static readonly ShipRoomId[] DamageRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom
        };

        public static bool ShouldStartAsteroidField(GameSessionState session)
        {
            if (session == null ||
                session.Phase != GameSessionPhase.Transporting ||
                session.CompletedTransportCount <= 0 ||
                !session.ActiveTransportContract.HasValue ||
                session.ActiveTransportContract.Value.IsTutorial)
            {
                return false;
            }

            var seed = CreateAsteroidSeed(session);
            return RollPercent(seed) < AsteroidFieldOccurrencePercent;
        }

        public static TransportHazardState CreateAsteroidField(GameSessionState session)
        {
            if (session == null || !session.ActiveTransportContract.HasValue)
            {
                throw new ArgumentException("An active transport contract is required for an asteroid hazard.", nameof(session));
            }

            var seed = CreateAsteroidSeed(session);
            var durationSeconds = MinimumAsteroidFieldDurationSeconds +
                                  PositiveModulo(seed, AsteroidFieldDurationVarianceSeconds);
            return TransportHazardState.StartAsteroidField(seed, durationSeconds);
        }

        public static TransportHazardResult ResolveAsteroidField(TransportHazardState hazard)
        {
            return ResolveAsteroidField(hazard, false);
        }

        public static TransportHazardResult ResolveAsteroidField(TransportHazardState hazard, bool neutralizedByTurret)
        {
            if (hazard.HazardType == TransportHazardType.None)
            {
                return TransportHazardResult.None;
            }

            if (hazard.HazardType != TransportHazardType.AsteroidField)
            {
                throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Unsupported transport hazard type.");
            }

            if (neutralizedByTurret)
            {
                return new TransportHazardResult(
                    TransportHazardType.AsteroidField,
                    TransportHazardResolution.Neutralized,
                    new ShipRoomHazardDamage[0]);
            }

            if (hazard.ManualAvoidanceRatio >= AsteroidFieldAvoidanceRatioForClear)
            {
                return new TransportHazardResult(
                    TransportHazardType.AsteroidField,
                    TransportHazardResolution.Avoided,
                    new ShipRoomHazardDamage[0]);
            }

            if (hazard.ManualAvoidanceSeconds > 0f)
            {
                return new TransportHazardResult(
                    TransportHazardType.AsteroidField,
                    TransportHazardResolution.GlancingHit,
                    CreateRoomDamage(hazard.Seed, 1, AsteroidFieldGlancingHitDamage));
            }

            return new TransportHazardResult(
                TransportHazardType.AsteroidField,
                TransportHazardResolution.DirectHit,
                CreateRoomDamage(hazard.Seed, AsteroidFieldDirectHitRoomCount, AsteroidFieldDirectHitDamage));
        }

        public static ExternalTargetState CreateExternalTarget(TransportHazardState hazard)
        {
            if (hazard.HazardType == TransportHazardType.None)
            {
                return ExternalTargetState.None;
            }

            if (hazard.HazardType != TransportHazardType.AsteroidField)
            {
                throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Unsupported transport hazard type.");
            }

            return new ExternalTargetState(
                "asteroid-field-" + hazard.Seed,
                ExternalTargetType.Asteroid,
                ManualTurretState.DefaultAsteroidTargetHealth,
                ManualTurretState.DefaultAsteroidTargetHealth,
                CreateTargetCoordinate(hazard.Seed, 17, 0.58f),
                CreateTargetCoordinate(hazard.Seed, 31, 0.42f),
                ManualTurretState.DefaultAsteroidHitRadius);
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
            if (session == null || !session.ActiveTransportContract.HasValue)
            {
                return 0;
            }

            var transportNumber = session.CompletedTransportCount + 1;
            return CreateStablePositiveHash(session.ActiveTransportContract.Value.Id, transportNumber);
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

        private static int CreateStablePositiveHash(string text, int transportNumber)
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
