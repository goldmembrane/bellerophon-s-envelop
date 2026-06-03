using System;

namespace Bellerophon.Core.Session
{
    public enum IntruderFaction
    {
        None,
        SeedEntity,
        AlienLifeform,
        CargoFreedomLeague,
        SpacePirate
    }

    public enum IntruderObjectiveType
    {
        None,
        AttackCargo,
        OccupyRoom,
        AttackPlayer,
        DestroyShip
    }

    public enum IntrusionPhase
    {
        None,
        Attempting,
        Boarded,
        Active,
        Resolved
    }

    public enum IntruderTargetType
    {
        None,
        Cargo,
        Room,
        Player,
        Ship
    }

    public enum IntruderResolution
    {
        None,
        Repelled,
        Neutralized,
        ObjectiveApplied
    }

    public readonly struct IntruderTargetPriority
    {
        public IntruderTargetPriority(IntruderTargetType targetType, ShipRoomId roomId, int priority)
        {
            if (targetType == IntruderTargetType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Intruder target priority requires a target type.");
            }

            if (priority < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priority), "Intruder target priority cannot be negative.");
            }

            TargetType = targetType;
            RoomId = roomId;
            Priority = priority;
        }

        public IntruderTargetType TargetType { get; }

        public ShipRoomId RoomId { get; }

        public int Priority { get; }
    }

    public readonly struct IntruderTargetSelection
    {
        public IntruderTargetSelection(IntruderTargetType targetType, ShipRoomId roomId)
        {
            if (targetType == IntruderTargetType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Intruder target selection requires a target type.");
            }

            TargetType = targetType;
            RoomId = roomId;
        }

        public IntruderTargetType TargetType { get; }

        public ShipRoomId RoomId { get; }
    }

    public readonly struct IntruderDefinition
    {
        private static readonly IntruderTargetPriority[] EmptyPriorities = new IntruderTargetPriority[0];
        private readonly IntruderTargetPriority[] targetPriorities;

        public IntruderDefinition(
            string definitionId,
            string displayName,
            IntruderFaction faction,
            IntruderObjectiveType primaryObjective,
            int maxHealth,
            float movementSpeed,
            float attackRange,
            float attackDelaySeconds,
            IntruderTargetPriority[] targetPriorities)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new ArgumentException("Intruder definition id is required.", nameof(definitionId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Intruder display name is required.", nameof(displayName));
            }

            if (faction == IntruderFaction.None)
            {
                throw new ArgumentOutOfRangeException(nameof(faction), "Intruder definition requires a faction.");
            }

            if (primaryObjective == IntruderObjectiveType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(primaryObjective), "Intruder definition requires an objective.");
            }

            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Intruder max health must be positive.");
            }

            if (movementSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed), "Intruder movement speed cannot be negative.");
            }

            if (attackRange < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackRange), "Intruder attack range cannot be negative.");
            }

            if (attackDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(attackDelaySeconds), "Intruder attack delay cannot be negative.");
            }

            DefinitionId = definitionId;
            DisplayName = displayName;
            Faction = faction;
            PrimaryObjective = primaryObjective;
            MaxHealth = maxHealth;
            MovementSpeed = movementSpeed;
            AttackRange = attackRange;
            AttackDelaySeconds = attackDelaySeconds;
            this.targetPriorities = targetPriorities == null || targetPriorities.Length == 0
                ? EmptyPriorities
                : (IntruderTargetPriority[])targetPriorities.Clone();
        }

        public string DefinitionId { get; }

        public string DisplayName { get; }

        public IntruderFaction Faction { get; }

        public IntruderObjectiveType PrimaryObjective { get; }

        public int MaxHealth { get; }

        public float MovementSpeed { get; }

        public float AttackRange { get; }

        public float AttackDelaySeconds { get; }

        public IntruderTargetPriority[] TargetPriorities => targetPriorities == null
            ? EmptyPriorities
            : (IntruderTargetPriority[])targetPriorities.Clone();
    }

    public readonly struct IntrusionAttemptState
    {
        private IntrusionAttemptState(
            string attemptId,
            string definitionId,
            IntruderFaction faction,
            IntruderObjectiveType objective,
            int seed,
            ShipRoomId entryRoom,
            ShipRoomId targetRoom,
            IntruderTargetType targetType,
            IntrusionPhase phase,
            IntruderResolution resolution)
        {
            AttemptId = attemptId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            Faction = faction;
            Objective = objective;
            Seed = seed;
            EntryRoom = entryRoom;
            TargetRoom = targetRoom;
            TargetType = targetType;
            Phase = phase;
            Resolution = resolution;
        }

        public string AttemptId { get; }

        public string DefinitionId { get; }

        public IntruderFaction Faction { get; }

        public IntruderObjectiveType Objective { get; }

        public int Seed { get; }

        public ShipRoomId EntryRoom { get; }

        public ShipRoomId TargetRoom { get; }

        public IntruderTargetType TargetType { get; }

        public IntrusionPhase Phase { get; }

        public IntruderResolution Resolution { get; }

        public bool IsAttempting => Phase == IntrusionPhase.Attempting;

        public bool HasBoarded => Phase == IntrusionPhase.Boarded;

        public bool IsResolved => Phase == IntrusionPhase.Resolved;

        public static IntrusionAttemptState None => new IntrusionAttemptState(
            string.Empty,
            string.Empty,
            IntruderFaction.None,
            IntruderObjectiveType.None,
            0,
            ShipRoomId.Cockpit,
            ShipRoomId.Cockpit,
            IntruderTargetType.None,
            IntrusionPhase.None,
            IntruderResolution.None);

        public static IntrusionAttemptState Start(
            string attemptId,
            IntruderDefinition definition,
            int seed,
            ShipRoomId entryRoom,
            IntruderTargetSelection target)
        {
            if (string.IsNullOrWhiteSpace(attemptId))
            {
                throw new ArgumentException("Intrusion attempt id is required.", nameof(attemptId));
            }

            return new IntrusionAttemptState(
                attemptId,
                definition.DefinitionId,
                definition.Faction,
                definition.PrimaryObjective,
                seed,
                entryRoom,
                target.RoomId,
                target.TargetType,
                IntrusionPhase.Attempting,
                IntruderResolution.None);
        }

        public IntrusionAttemptState MarkBoarded()
        {
            RequirePhase(IntrusionPhase.Attempting);
            return new IntrusionAttemptState(
                AttemptId,
                DefinitionId,
                Faction,
                Objective,
                Seed,
                EntryRoom,
                TargetRoom,
                TargetType,
                IntrusionPhase.Boarded,
                IntruderResolution.None);
        }

        public IntrusionAttemptState MarkRepelled()
        {
            RequirePhase(IntrusionPhase.Attempting);
            return new IntrusionAttemptState(
                AttemptId,
                DefinitionId,
                Faction,
                Objective,
                Seed,
                EntryRoom,
                TargetRoom,
                TargetType,
                IntrusionPhase.Resolved,
                IntruderResolution.Repelled);
        }

        private void RequirePhase(IntrusionPhase expectedPhase)
        {
            if (Phase != expectedPhase)
            {
                throw new InvalidOperationException($"Expected intrusion phase {expectedPhase}, but current phase is {Phase}.");
            }
        }
    }

    public readonly struct IntruderEntityState
    {
        private IntruderEntityState(
            string instanceId,
            string definitionId,
            IntruderFaction faction,
            IntruderObjectiveType objective,
            int currentHealth,
            int maxHealth,
            ShipRoomId currentRoom,
            ShipRoomId targetRoom,
            IntruderTargetType targetType,
            IntrusionPhase phase,
            IntruderResolution resolution)
        {
            InstanceId = instanceId ?? string.Empty;
            DefinitionId = definitionId ?? string.Empty;
            Faction = faction;
            Objective = objective;
            CurrentHealth = Clamp(currentHealth, 0, maxHealth);
            MaxHealth = Math.Max(0, maxHealth);
            CurrentRoom = currentRoom;
            TargetRoom = targetRoom;
            TargetType = targetType;
            Phase = phase;
            Resolution = resolution;
        }

        public string InstanceId { get; }

        public string DefinitionId { get; }

        public IntruderFaction Faction { get; }

        public IntruderObjectiveType Objective { get; }

        public int CurrentHealth { get; }

        public int MaxHealth { get; }

        public ShipRoomId CurrentRoom { get; }

        public ShipRoomId TargetRoom { get; }

        public IntruderTargetType TargetType { get; }

        public IntrusionPhase Phase { get; }

        public IntruderResolution Resolution { get; }

        public bool IsActive => Phase == IntrusionPhase.Active && CurrentHealth > 0;

        public bool IsResolved => Phase == IntrusionPhase.Resolved;

        public static IntruderEntityState None => new IntruderEntityState(
            string.Empty,
            string.Empty,
            IntruderFaction.None,
            IntruderObjectiveType.None,
            0,
            0,
            ShipRoomId.Cockpit,
            ShipRoomId.Cockpit,
            IntruderTargetType.None,
            IntrusionPhase.None,
            IntruderResolution.None);

        public static IntruderEntityState Board(
            IntrusionAttemptState attempt,
            IntruderDefinition definition)
        {
            if (!attempt.HasBoarded)
            {
                throw new InvalidOperationException("Intruder entity can only be created from a boarded intrusion attempt.");
            }

            if (attempt.DefinitionId != definition.DefinitionId)
            {
                throw new ArgumentException("Intrusion attempt and intruder definition do not match.", nameof(definition));
            }

            return new IntruderEntityState(
                attempt.AttemptId + "-intruder",
                definition.DefinitionId,
                definition.Faction,
                definition.PrimaryObjective,
                definition.MaxHealth,
                definition.MaxHealth,
                attempt.EntryRoom,
                attempt.TargetRoom,
                attempt.TargetType,
                IntrusionPhase.Active,
                IntruderResolution.None);
        }

        public IntruderEntityState MoveToTargetRoom()
        {
            if (!IsActive)
            {
                return this;
            }

            return new IntruderEntityState(
                InstanceId,
                DefinitionId,
                Faction,
                Objective,
                CurrentHealth,
                MaxHealth,
                TargetRoom,
                TargetRoom,
                TargetType,
                Phase,
                Resolution);
        }

        public IntruderEntityState WithDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Intruder damage cannot be negative.");
            }

            if (!IsActive || damage == 0)
            {
                return this;
            }

            var nextHealth = CurrentHealth - damage;
            if (nextHealth > 0)
            {
                return new IntruderEntityState(
                    InstanceId,
                    DefinitionId,
                    Faction,
                    Objective,
                    nextHealth,
                    MaxHealth,
                    CurrentRoom,
                    TargetRoom,
                    TargetType,
                    Phase,
                    Resolution);
            }

            return Resolve(IntruderResolution.Neutralized);
        }

        public IntruderEntityState Resolve(IntruderResolution resolution)
        {
            if (resolution == IntruderResolution.None)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution), "Resolved intruders require a resolution.");
            }

            return new IntruderEntityState(
                InstanceId,
                DefinitionId,
                Faction,
                Objective,
                CurrentHealth,
                MaxHealth,
                CurrentRoom,
                TargetRoom,
                TargetType,
                IntrusionPhase.Resolved,
                resolution);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }

    public readonly struct IntruderImpactResult
    {
        public IntruderImpactResult(
            IntruderEntityState intruder,
            ShipState ship,
            CargoState cargo,
            IntruderTargetType affectedTargetType,
            ShipRoomId affectedRoom,
            int roomDamageApplied,
            float cargoDamagePercentApplied,
            bool threatensPlayer,
            bool roomOccupied,
            bool threatensShip)
        {
            Intruder = intruder;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            Cargo = cargo;
            AffectedTargetType = affectedTargetType;
            AffectedRoom = affectedRoom;
            RoomDamageApplied = Math.Max(0, roomDamageApplied);
            CargoDamagePercentApplied = cargoDamagePercentApplied < 0f ? 0f : cargoDamagePercentApplied;
            ThreatensPlayer = threatensPlayer;
            RoomOccupied = roomOccupied;
            ThreatensShip = threatensShip;
        }

        public IntruderEntityState Intruder { get; }

        public ShipState Ship { get; }

        public CargoState Cargo { get; }

        public IntruderTargetType AffectedTargetType { get; }

        public ShipRoomId AffectedRoom { get; }

        public int RoomDamageApplied { get; }

        public float CargoDamagePercentApplied { get; }

        public bool ThreatensPlayer { get; }

        public bool RoomOccupied { get; }

        public bool ThreatensShip { get; }
    }

    public static class IntruderRules
    {
        public const int DefaultRoomDamage = 10;
        public const float DefaultCargoDamagePercent = 0.05f;

        private static readonly ShipRoomId[] BoardableRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom
        };

        public static IntrusionAttemptState CreateAttempt(
            string attemptId,
            IntruderDefinition definition,
            int seed,
            ShipRoomId playerRoom)
        {
            var entryRoom = SelectEntryRoom(seed);
            var target = SelectTarget(definition, seed, playerRoom);
            return IntrusionAttemptState.Start(attemptId, definition, seed, entryRoom, target);
        }

        public static IntrusionAttemptState ResolveAttempt(
            IntrusionAttemptState attempt,
            bool intrusionRepelled)
        {
            if (!attempt.IsAttempting)
            {
                throw new InvalidOperationException("Only active intrusion attempts can be resolved.");
            }

            return intrusionRepelled
                ? attempt.MarkRepelled()
                : attempt.MarkBoarded();
        }

        public static IntruderEntityState CreateBoardedIntruder(
            IntrusionAttemptState attempt,
            IntruderDefinition definition)
        {
            return IntruderEntityState.Board(attempt, definition);
        }

        public static IntruderTargetSelection SelectTarget(
            IntruderDefinition definition,
            int seed,
            ShipRoomId playerRoom)
        {
            var priorities = definition.TargetPriorities;
            if (priorities.Length == 0)
            {
                return CreateFallbackTarget(definition.PrimaryObjective, seed, playerRoom);
            }

            var bestIndex = -1;
            var bestPriority = int.MaxValue;
            for (var i = 0; i < priorities.Length; i++)
            {
                if (priorities[i].Priority >= bestPriority)
                {
                    continue;
                }

                bestIndex = i;
                bestPriority = priorities[i].Priority;
            }

            if (bestIndex < 0)
            {
                return CreateFallbackTarget(definition.PrimaryObjective, seed, playerRoom);
            }

            var selected = priorities[bestIndex];
            switch (selected.TargetType)
            {
                case IntruderTargetType.Cargo:
                    return new IntruderTargetSelection(IntruderTargetType.Cargo, ShipRoomId.CargoHold);
                case IntruderTargetType.Player:
                    return new IntruderTargetSelection(IntruderTargetType.Player, playerRoom);
                case IntruderTargetType.Ship:
                    return new IntruderTargetSelection(IntruderTargetType.Ship, selected.RoomId);
                case IntruderTargetType.Room:
                    return new IntruderTargetSelection(IntruderTargetType.Room, selected.RoomId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(definition), selected.TargetType, "Unsupported intruder target type.");
            }
        }

        public static IntruderImpactResult ApplyObjectivePressure(
            IntruderEntityState intruder,
            ShipState ship,
            CargoState cargo)
        {
            return ApplyObjectivePressure(
                intruder,
                ship,
                cargo,
                DefaultRoomDamage,
                DefaultCargoDamagePercent);
        }

        public static IntruderImpactResult ApplyObjectivePressure(
            IntruderEntityState intruder,
            ShipState ship,
            CargoState cargo,
            int roomDamage,
            float cargoDamagePercent)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (roomDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roomDamage), "Intruder room damage cannot be negative.");
            }

            if (cargoDamagePercent < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cargoDamagePercent), "Intruder cargo damage cannot be negative.");
            }

            if (!intruder.IsActive)
            {
                return new IntruderImpactResult(
                    intruder,
                    ship,
                    cargo,
                    IntruderTargetType.None,
                    ShipRoomId.Cockpit,
                    0,
                    0f,
                    false,
                    false,
                    false);
            }

            switch (intruder.Objective)
            {
                case IntruderObjectiveType.AttackCargo:
                    return new IntruderImpactResult(
                        intruder.Resolve(IntruderResolution.ObjectiveApplied),
                        ship,
                        cargo.WithDamagePercent(cargoDamagePercent),
                        IntruderTargetType.Cargo,
                        ShipRoomId.CargoHold,
                        0,
                        cargoDamagePercent,
                        false,
                        false,
                        false);
                case IntruderObjectiveType.OccupyRoom:
                    var occupiedRoom = ship.GetRoom(intruder.TargetRoom).WithFunctionOffline(true);
                    return new IntruderImpactResult(
                        intruder.Resolve(IntruderResolution.ObjectiveApplied),
                        ship.WithRoom(intruder.TargetRoom, occupiedRoom),
                        cargo,
                        IntruderTargetType.Room,
                        intruder.TargetRoom,
                        0,
                        0f,
                        false,
                        true,
                        false);
                case IntruderObjectiveType.AttackPlayer:
                    return new IntruderImpactResult(
                        intruder.Resolve(IntruderResolution.ObjectiveApplied),
                        ship,
                        cargo,
                        IntruderTargetType.Player,
                        intruder.TargetRoom,
                        0,
                        0f,
                        true,
                        false,
                        false);
                case IntruderObjectiveType.DestroyShip:
                    var damagedRoom = ship.GetRoom(intruder.TargetRoom).WithDamage(roomDamage);
                    return new IntruderImpactResult(
                        intruder.Resolve(IntruderResolution.ObjectiveApplied),
                        ship.WithRoom(intruder.TargetRoom, damagedRoom),
                        cargo,
                        IntruderTargetType.Ship,
                        intruder.TargetRoom,
                        roomDamage,
                        0f,
                        false,
                        false,
                        true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(intruder), intruder.Objective, "Unsupported intruder objective.");
            }
        }

        public static ShipRoomId SelectEntryRoom(int seed)
        {
            return BoardableRoomOrder[PositiveModulo(seed, BoardableRoomOrder.Length)];
        }

        private static IntruderTargetSelection CreateFallbackTarget(
            IntruderObjectiveType objective,
            int seed,
            ShipRoomId playerRoom)
        {
            switch (objective)
            {
                case IntruderObjectiveType.AttackCargo:
                    return new IntruderTargetSelection(IntruderTargetType.Cargo, ShipRoomId.CargoHold);
                case IntruderObjectiveType.OccupyRoom:
                    return new IntruderTargetSelection(IntruderTargetType.Room, SelectEntryRoom(seed + 17));
                case IntruderObjectiveType.AttackPlayer:
                    return new IntruderTargetSelection(IntruderTargetType.Player, playerRoom);
                case IntruderObjectiveType.DestroyShip:
                    return new IntruderTargetSelection(IntruderTargetType.Ship, ShipRoomId.EngineRoom);
                default:
                    throw new ArgumentOutOfRangeException(nameof(objective), objective, "Unsupported intruder objective.");
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
