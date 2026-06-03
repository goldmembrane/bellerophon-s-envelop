using System;

namespace Bellerophon.Core.Session
{
    public enum SeedIntruderKind
    {
        None,
        Parvum
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
            float cargoDamagePercentApplied)
        {
            State = state;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            Cargo = cargo;
            AttackCount = Math.Max(0, attackCount);
            RoomDamageApplied = Math.Max(0, roomDamageApplied);
            CargoDamagePercentApplied = cargoDamagePercentApplied < 0f ? 0f : cargoDamagePercentApplied;
        }

        public SeedIntruderState State { get; }

        public ShipState Ship { get; }

        public CargoState Cargo { get; }

        public int AttackCount { get; }

        public int RoomDamageApplied { get; }

        public float CargoDamagePercentApplied { get; }

        public bool AppliedDamage => RoomDamageApplied > 0 || CargoDamagePercentApplied > 0f;
    }

    public static class SeedIntruderRules
    {
        public const float OccurrenceCheckIntervalSeconds = 2f;
        public const int OccurrencePercent = 15;
        public const string ParvumDefinitionId = "seed-parvum";
        public const int ParvumHealth = 55;
        public const float ParvumMovementSpeed = 2.5f;
        public const float ParvumAttackRange = 1f;
        public const float ParvumAttackDelaySeconds = 0.5f;
        public const int ParvumBiologicalDamage = 6;
        public const int ParvumShipFacilityDamage = 3;

        private static readonly ShipRoomId[] ParvumTargetRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom
        };

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
            if (!CanCheckSeedIntruder(session) || checkIndex <= 0)
            {
                return false;
            }

            return RollSeedIntruderPercent(CreateSeedIntruderSeed(session, checkIndex)) < OccurrencePercent;
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
            var targetRoom = SelectParvumTargetRoom(seed);
            var definition = CreateParvumDefinition(targetRoom);
            var attempt = IntruderRules.CreateAttempt(attemptId, definition, seed, playerRoom);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, definition);
            return SeedIntruderState.Start(SeedIntruderKind.Parvum, definition, boarded, intruder);
        }

        public static IntruderDefinition CreateParvumDefinition(ShipRoomId targetRoom)
        {
            return new IntruderDefinition(
                ParvumDefinitionId,
                "Parvum",
                IntruderFaction.SeedEntity,
                IntruderObjectiveType.DestroyShip,
                ParvumHealth,
                ParvumMovementSpeed,
                ParvumAttackRange,
                ParvumAttackDelaySeconds,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, targetRoom, 0)
                });
        }

        public static SeedIntruderTickResult TickParvum(
            SeedIntruderState state,
            ShipState ship,
            CargoState cargo,
            float deltaSeconds)
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
                return new SeedIntruderTickResult(state, ship, cargo, 0, 0, 0f);
            }

            if (state.Kind != SeedIntruderKind.Parvum)
            {
                throw new ArgumentOutOfRangeException(nameof(state), state.Kind, "Unsupported seed intruder kind.");
            }

            var intruder = state.Intruder.CurrentRoom == state.Intruder.TargetRoom
                ? state.Intruder
                : state.Intruder.MoveToTargetRoom();
            var elapsed = state.ElapsedSeconds + deltaSeconds;
            var attackAccumulator = state.AttackAccumulatorSeconds + deltaSeconds;
            var attackCount = 0;
            var roomDamage = 0;
            var nextShip = ship;

            while (attackAccumulator + 0.0001f >= ParvumAttackDelaySeconds)
            {
                attackAccumulator -= ParvumAttackDelaySeconds;
                attackCount++;
                roomDamage += ParvumShipFacilityDamage;

                var room = nextShip.GetRoom(intruder.TargetRoom);
                nextShip = nextShip.WithRoom(intruder.TargetRoom, room.WithDamage(ParvumShipFacilityDamage));
            }

            var nextState = state.WithProgress(
                intruder,
                elapsed,
                attackAccumulator,
                state.AppliedAttackCount + attackCount,
                state.TotalRoomDamageApplied + roomDamage,
                state.TotalCargoDamagePercentApplied);

            return new SeedIntruderTickResult(nextState, nextShip, cargo, attackCount, roomDamage, 0f);
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
            return ParvumTargetRoomOrder[PositiveModulo(seed, ParvumTargetRoomOrder.Length)];
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
