using System;
using System.Collections.Generic;

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

    public enum IntruderMobilityKind
    {
        Walking,
        Flying,
        Stationary
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

    public enum IntruderRelationKind
    {
        None,
        Hostile,
        Competitive,
        Allied,
        Bonded,
        Commanded
    }

    public enum IntruderRelationMarkerKind
    {
        None,
        RedCircle,
        GrayCircle,
        SkyBlueCircle,
        GreenCircle
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

    public readonly struct IntruderRelationProfile
    {
        public IntruderRelationProfile(
            IntruderRelationKind relationKind,
            IntruderRelationMarkerKind markerKind,
            bool canDirectlyAttack,
            bool friendlyFireDamagesHealth,
            bool friendlyFireAppliesStatusEffects)
        {
            if (relationKind == IntruderRelationKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(relationKind), "Intruder relation requires a concrete relation kind.");
            }

            RelationKind = relationKind;
            MarkerKind = markerKind;
            CanDirectlyAttack = canDirectlyAttack;
            FriendlyFireDamagesHealth = friendlyFireDamagesHealth;
            FriendlyFireAppliesStatusEffects = friendlyFireAppliesStatusEffects;
        }

        public IntruderRelationKind RelationKind { get; }

        public IntruderRelationMarkerKind MarkerKind { get; }

        public bool CanDirectlyAttack { get; }

        public bool FriendlyFireDamagesHealth { get; }

        public bool FriendlyFireAppliesStatusEffects { get; }
    }

    public readonly struct IntruderEnvironmentAssessment
    {
        public IntruderEnvironmentAssessment(
            int closedCorridorPercent,
            int closedCorridorCount,
            int blackoutRoomCount,
            int availableCctvCount,
            bool intruderDetectionOnline,
            bool intruderSuppressionOnline,
            float statMultiplier,
            float effectiveMovementSpeed,
            int effectiveRoomDamage)
        {
            ClosedCorridorPercent = Math.Max(0, closedCorridorPercent);
            ClosedCorridorCount = Math.Max(0, closedCorridorCount);
            BlackoutRoomCount = Math.Max(0, blackoutRoomCount);
            AvailableCctvCount = Math.Max(0, availableCctvCount);
            IntruderDetectionOnline = intruderDetectionOnline;
            IntruderSuppressionOnline = intruderSuppressionOnline;
            StatMultiplier = statMultiplier < 0f ? 0f : statMultiplier;
            EffectiveMovementSpeed = effectiveMovementSpeed < 0f ? 0f : effectiveMovementSpeed;
            EffectiveRoomDamage = Math.Max(0, effectiveRoomDamage);
        }

        public int ClosedCorridorPercent { get; }

        public int ClosedCorridorCount { get; }

        public int BlackoutRoomCount { get; }

        public int AvailableCctvCount { get; }

        public bool IntruderDetectionOnline { get; }

        public bool IntruderSuppressionOnline { get; }

        public float StatMultiplier { get; }

        public float EffectiveMovementSpeed { get; }

        public int EffectiveRoomDamage { get; }
    }

    public readonly struct IntruderRouteAssessment
    {
        public IntruderRouteAssessment(
            ShipRoomId currentRoom,
            ShipRoomId targetRoom,
            ShipRoomId nextRoom,
            bool hasPath,
            bool isAtTarget,
            bool blockedBySealedRoom,
            int closedCorridorPercent,
            int closedCorridorCount,
            int remainingStepCount)
        {
            CurrentRoom = currentRoom;
            TargetRoom = targetRoom;
            NextRoom = nextRoom;
            HasPath = hasPath;
            IsAtTarget = isAtTarget;
            BlockedBySealedRoom = blockedBySealedRoom;
            ClosedCorridorPercent = Math.Max(0, closedCorridorPercent);
            ClosedCorridorCount = Math.Max(0, closedCorridorCount);
            RemainingStepCount = Math.Max(0, remainingStepCount);
        }

        public ShipRoomId CurrentRoom { get; }

        public ShipRoomId TargetRoom { get; }

        public ShipRoomId NextRoom { get; }

        public bool HasPath { get; }

        public bool IsAtTarget { get; }

        public bool BlockedBySealedRoom { get; }

        public int ClosedCorridorPercent { get; }

        public int ClosedCorridorCount { get; }

        public int RemainingStepCount { get; }

        public bool CanAdvance => HasPath && !IsAtTarget && !BlockedBySealedRoom;
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
            IntruderTargetPriority[] targetPriorities,
            IntruderMobilityKind mobilityKind = IntruderMobilityKind.Walking,
            bool issuesFactionCommands = false)
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
            MobilityKind = mobilityKind;
            IssuesFactionCommands = issuesFactionCommands;
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

        public IntruderMobilityKind MobilityKind { get; }

        public bool IssuesFactionCommands { get; }

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
        private static readonly CombatStatusEffectState[] EmptyStatusEffects = new CombatStatusEffectState[0];
        private readonly CombatStatusEffectState[] statusEffects;

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
            IntruderMobilityKind mobilityKind,
            bool issuesFactionCommands,
            IntrusionPhase phase,
            IntruderResolution resolution,
            CombatStatusEffectState[] statusEffects = null)
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
            MobilityKind = mobilityKind;
            IssuesFactionCommands = issuesFactionCommands;
            Phase = phase;
            Resolution = resolution;
            this.statusEffects = CombatStatusEffectRules.CloneEffects(statusEffects);
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

        public IntruderMobilityKind MobilityKind { get; }

        public bool IssuesFactionCommands { get; }

        public IntrusionPhase Phase { get; }

        public IntruderResolution Resolution { get; }

        public CombatStatusEffectState[] StatusEffects =>
            statusEffects == null ? EmptyStatusEffects : CombatStatusEffectRules.CloneEffects(statusEffects);

        public bool IsActive => Phase == IntrusionPhase.Active && CurrentHealth > 0;

        public bool IsResolved => Phase == IntrusionPhase.Resolved;

        public bool HasStatusEffect(CombatStatusEffectKind kind)
        {
            return CombatStatusEffectRules.HasEffect(statusEffects, kind);
        }

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
            IntruderMobilityKind.Walking,
            false,
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
                definition.MobilityKind,
                definition.IssuesFactionCommands,
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
                MobilityKind,
                IssuesFactionCommands,
                Phase,
                Resolution,
                statusEffects);
        }

        public IntruderEntityState MoveToRoom(ShipRoomId roomId)
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
                roomId,
                TargetRoom,
                TargetType,
                MobilityKind,
                IssuesFactionCommands,
                Phase,
                Resolution,
                statusEffects);
        }

        public IntruderEntityState WithTarget(
            IntruderTargetType targetType,
            ShipRoomId targetRoom,
            IntruderObjectiveType objective)
        {
            if (!IsActive)
            {
                return this;
            }

            if (targetType == IntruderTargetType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Intruder target cannot be none.");
            }

            if (objective == IntruderObjectiveType.None)
            {
                throw new ArgumentOutOfRangeException(nameof(objective), "Intruder objective cannot be none.");
            }

            return new IntruderEntityState(
                InstanceId,
                DefinitionId,
                Faction,
                objective,
                CurrentHealth,
                MaxHealth,
                CurrentRoom,
                targetRoom,
                targetType,
                MobilityKind,
                IssuesFactionCommands,
                Phase,
                Resolution,
                statusEffects);
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
                    MobilityKind,
                    IssuesFactionCommands,
                    Phase,
                    Resolution,
                    statusEffects);
            }

            return Resolve(IntruderResolution.Neutralized);
        }

        public IntruderEntityState WithRecoveredHealth(int healthAmount)
        {
            if (healthAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(healthAmount), "Intruder recovery amount cannot be negative.");
            }

            if (!IsActive || healthAmount == 0)
            {
                return this;
            }

            return new IntruderEntityState(
                InstanceId,
                DefinitionId,
                Faction,
                Objective,
                CurrentHealth + healthAmount,
                MaxHealth,
                CurrentRoom,
                TargetRoom,
                TargetType,
                MobilityKind,
                IssuesFactionCommands,
                Phase,
                Resolution,
                statusEffects);
        }

        public IntruderEntityState WithStatusEffects(CombatStatusEffectState[] effects)
        {
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
                MobilityKind,
                IssuesFactionCommands,
                Phase,
                Resolution,
                effects);
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
                MobilityKind,
                IssuesFactionCommands,
                IntrusionPhase.Resolved,
                resolution,
                statusEffects);
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

        private static readonly ShipRoomCorridorLink[] CorridorLinks =
        {
            new ShipRoomCorridorLink(ShipRoomId.CargoHold, ShipRoomId.Cockpit),
            new ShipRoomCorridorLink(ShipRoomId.CargoHold, ShipRoomId.EngineRoom),
            new ShipRoomCorridorLink(ShipRoomId.CargoHold, ShipRoomId.ControlRoom),
            new ShipRoomCorridorLink(ShipRoomId.CargoHold, ShipRoomId.Armory),
            new ShipRoomCorridorLink(ShipRoomId.CargoHold, ShipRoomId.SupplyRoom),
            new ShipRoomCorridorLink(ShipRoomId.SupplyRoom, ShipRoomId.Armory),
            new ShipRoomCorridorLink(ShipRoomId.Cockpit, ShipRoomId.EngineRoom),
            new ShipRoomCorridorLink(ShipRoomId.Cockpit, ShipRoomId.ControlRoom),
            new ShipRoomCorridorLink(ShipRoomId.EngineRoom, ShipRoomId.ControlRoom),
            new ShipRoomCorridorLink(ShipRoomId.ControlRoom, ShipRoomId.Armory)
        };

        public static IntrusionAttemptState CreateAttempt(
            string attemptId,
            IntruderDefinition definition,
            int seed,
            ShipRoomId playerRoom)
        {
            return CreateAttempt(attemptId, definition, seed, playerRoom, null);
        }

        public static IntrusionAttemptState CreateAttempt(
            string attemptId,
            IntruderDefinition definition,
            int seed,
            ShipRoomId playerRoom,
            ShipState ship)
        {
            var entryRoom = SelectEntryRoom(seed);
            var target = SelectTarget(definition, seed, playerRoom, ship);
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

        public static IntruderEntityState ApplyStatusEffect(
            IntruderEntityState intruder,
            CombatStatusEffectApplication application)
        {
            if (!intruder.IsActive || !application.HasEffect)
            {
                return intruder;
            }

            return intruder.WithStatusEffects(
                CombatStatusEffectRules.ApplyEffect(intruder.StatusEffects, application));
        }

        public static IntruderEntityState TickStatusEffects(IntruderEntityState intruder, float deltaSeconds)
        {
            if (!intruder.IsActive || deltaSeconds <= 0f)
            {
                return intruder;
            }

            var ticked = CombatStatusEffectRules.TickEffects(intruder.StatusEffects, deltaSeconds);
            var next = intruder.WithStatusEffects(ticked.Effects);
            return ticked.HealthDamage > 0 ? next.WithDamage(ticked.HealthDamage) : next;
        }

        public static IntruderTargetSelection SelectTarget(
            IntruderDefinition definition,
            int seed,
            ShipRoomId playerRoom)
        {
            return SelectTarget(definition, seed, playerRoom, null);
        }

        public static IntruderTargetSelection SelectTarget(
            IntruderDefinition definition,
            int seed,
            ShipRoomId playerRoom,
            ShipState ship)
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

                if (!CanUseTargetPriority(priorities[i], ship))
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

        public static IntruderEnvironmentAssessment AssessEnvironment(
            IntruderDefinition definition,
            ShipState ship)
        {
            return AssessEnvironment(definition, ship, DefaultRoomDamage);
        }

        public static IntruderEnvironmentAssessment AssessEnvironment(
            IntruderDefinition definition,
            ShipState ship,
            int baseRoomDamage)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (baseRoomDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRoomDamage), "Room damage cannot be negative.");
            }

            var suppressionOnline = ShipStateRules.IsIntruderSuppressionOnline(ship);
            var statMultiplier = suppressionOnline ? 1f : ShipStateRules.ControlRoomDestroyedIntruderStatMultiplier;
            var movementSpeed = definition.MobilityKind == IntruderMobilityKind.Stationary
                ? 0f
                : definition.MovementSpeed * statMultiplier;
            return new IntruderEnvironmentAssessment(
                ShipStateRules.CalculateControlRoomClosedCorridorPercent(ship),
                CalculateClosedCorridorCount(ship),
                ShipStateRules.CalculateEngineBlackoutRoomCount(ship),
                ShipStateRules.CalculateControlRoomAvailableCctvCount(ship),
                ShipStateRules.IsIntruderDetectionOnline(ship),
                suppressionOnline,
                statMultiplier,
                movementSpeed,
                ShipStateRules.CalculateInternalIntruderRoomDamage(baseRoomDamage, ship));
        }

        public static IntruderRelationProfile DetermineRelation(
            IntruderFaction sourceFaction,
            IntruderFaction targetFaction)
        {
            if (sourceFaction == IntruderFaction.None || targetFaction == IntruderFaction.None)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceFaction), "Intruder relation requires concrete factions.");
            }

            if (sourceFaction == targetFaction)
            {
                switch (sourceFaction)
                {
                    case IntruderFaction.SeedEntity:
                        return CreateRelationProfile(IntruderRelationKind.Competitive);
                    case IntruderFaction.AlienLifeform:
                        return CreateRelationProfile(IntruderRelationKind.Allied);
                    case IntruderFaction.CargoFreedomLeague:
                    case IntruderFaction.SpacePirate:
                        return CreateRelationProfile(IntruderRelationKind.Bonded);
                }
            }

            if (sourceFaction == IntruderFaction.AlienLifeform || targetFaction == IntruderFaction.AlienLifeform)
            {
                return sourceFaction == IntruderFaction.SeedEntity || targetFaction == IntruderFaction.SeedEntity
                    ? CreateRelationProfile(IntruderRelationKind.Hostile)
                    : CreateRelationProfile(IntruderRelationKind.Competitive);
            }

            return CreateRelationProfile(IntruderRelationKind.Hostile);
        }

        public static IntruderRelationProfile DetermineRelation(
            IntruderDefinition source,
            IntruderDefinition target)
        {
            if (source.IssuesFactionCommands && source.Faction == target.Faction)
            {
                return CreateRelationProfile(IntruderRelationKind.Commanded);
            }

            return DetermineRelation(source.Faction, target.Faction);
        }

        public static IntruderRelationProfile DetermineRelation(
            IntruderEntityState source,
            IntruderEntityState target)
        {
            if (source.IssuesFactionCommands && source.Faction == target.Faction)
            {
                return CreateRelationProfile(IntruderRelationKind.Commanded);
            }

            return DetermineRelation(source.Faction, target.Faction);
        }

        public static IntruderRouteAssessment AssessRoute(
            IntruderEntityState intruder,
            ShipState ship)
        {
            if (!intruder.IsActive)
            {
                return new IntruderRouteAssessment(
                    intruder.CurrentRoom,
                    intruder.TargetRoom,
                    intruder.CurrentRoom,
                    false,
                    false,
                    false,
                    ship == null ? 0 : ShipStateRules.CalculateControlRoomClosedCorridorPercent(ship),
                    ship == null ? 0 : CalculateClosedCorridorCount(ship),
                    0);
            }

            return AssessRoute(intruder.CurrentRoom, intruder.TargetRoom, intruder.MobilityKind, ship);
        }

        public static IntruderRouteAssessment AssessRoute(
            ShipRoomId currentRoom,
            ShipRoomId targetRoom,
            IntruderMobilityKind mobilityKind,
            ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var closedPercent = ShipStateRules.CalculateControlRoomClosedCorridorPercent(ship);
            var closedCount = mobilityKind == IntruderMobilityKind.Flying ? 0 : CalculateClosedCorridorCount(ship);
            if (currentRoom == targetRoom)
            {
                return new IntruderRouteAssessment(
                    currentRoom,
                    targetRoom,
                    currentRoom,
                    true,
                    true,
                    IsRoomSealed(ship, currentRoom),
                    closedPercent,
                    closedCount,
                    0);
            }

            if (mobilityKind == IntruderMobilityKind.Stationary ||
                IsRoomSealed(ship, currentRoom) ||
                IsRoomSealed(ship, targetRoom))
            {
                return new IntruderRouteAssessment(
                    currentRoom,
                    targetRoom,
                    currentRoom,
                    false,
                    false,
                    true,
                    closedPercent,
                    closedCount,
                    0);
            }

            if (!TryFindPath(currentRoom, targetRoom, mobilityKind, ship, closedCount, out var nextRoom, out var remainingSteps))
            {
                return new IntruderRouteAssessment(
                    currentRoom,
                    targetRoom,
                    currentRoom,
                    false,
                    false,
                    false,
                    closedPercent,
                    closedCount,
                    0);
            }

            return new IntruderRouteAssessment(
                currentRoom,
                targetRoom,
                nextRoom,
                true,
                false,
                false,
                closedPercent,
                closedCount,
                remainingSteps);
        }

        public static IntruderEntityState AdvanceOneRoomTowardTarget(
            IntruderEntityState intruder,
            ShipState ship)
        {
            var route = AssessRoute(intruder, ship);
            return route.CanAdvance ? intruder.MoveToRoom(route.NextRoom) : intruder;
        }

        public static IntruderEntityState MoveToReachableTargetRoom(
            IntruderEntityState intruder,
            ShipState ship)
        {
            var route = AssessRoute(intruder, ship);
            return route.CanAdvance || route.IsAtTarget ? intruder.MoveToRoom(route.TargetRoom) : intruder;
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

        public static int CalculateClosedCorridorCount(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return CalculateClosedCorridorCount(ShipStateRules.CalculateControlRoomClosedCorridorPercent(ship));
        }

        public static int CalculateClosedCorridorCount(int closedCorridorPercent)
        {
            var clampedPercent = Math.Max(0, Math.Min(100, closedCorridorPercent));
            return Math.Min(
                CorridorLinks.Length,
                (int)Math.Round(CorridorLinks.Length * (clampedPercent / 100f), MidpointRounding.AwayFromZero));
        }

        private static bool CanUseTargetPriority(IntruderTargetPriority priority, ShipState ship)
        {
            if (ship == null)
            {
                return true;
            }

            switch (priority.TargetType)
            {
                case IntruderTargetType.Cargo:
                    return !IsRoomSealed(ship, ShipRoomId.CargoHold);
                case IntruderTargetType.Room:
                case IntruderTargetType.Ship:
                    return !IsRoomSealed(ship, priority.RoomId);
                case IntruderTargetType.Player:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(priority), priority.TargetType, "Unsupported intruder target type.");
            }
        }

        public static IntruderRelationProfile CreateRelationProfile(IntruderRelationKind relationKind)
        {
            switch (relationKind)
            {
                case IntruderRelationKind.Hostile:
                    return new IntruderRelationProfile(
                        relationKind,
                        IntruderRelationMarkerKind.RedCircle,
                        canDirectlyAttack: true,
                        friendlyFireDamagesHealth: true,
                        friendlyFireAppliesStatusEffects: true);
                case IntruderRelationKind.Competitive:
                    return new IntruderRelationProfile(
                        relationKind,
                        IntruderRelationMarkerKind.GrayCircle,
                        canDirectlyAttack: false,
                        friendlyFireDamagesHealth: true,
                        friendlyFireAppliesStatusEffects: true);
                case IntruderRelationKind.Allied:
                    return new IntruderRelationProfile(
                        relationKind,
                        IntruderRelationMarkerKind.SkyBlueCircle,
                        canDirectlyAttack: false,
                        friendlyFireDamagesHealth: false,
                        friendlyFireAppliesStatusEffects: true);
                case IntruderRelationKind.Bonded:
                    return new IntruderRelationProfile(
                        relationKind,
                        IntruderRelationMarkerKind.GreenCircle,
                        canDirectlyAttack: false,
                        friendlyFireDamagesHealth: false,
                        friendlyFireAppliesStatusEffects: false);
                case IntruderRelationKind.Commanded:
                    return new IntruderRelationProfile(
                        relationKind,
                        IntruderRelationMarkerKind.None,
                        canDirectlyAttack: false,
                        friendlyFireDamagesHealth: false,
                        friendlyFireAppliesStatusEffects: false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(relationKind), relationKind, "Unsupported intruder relation.");
            }
        }

        private static bool TryFindPath(
            ShipRoomId currentRoom,
            ShipRoomId targetRoom,
            IntruderMobilityKind mobilityKind,
            ShipState ship,
            int closedCorridorCount,
            out ShipRoomId nextRoom,
            out int remainingSteps)
        {
            nextRoom = currentRoom;
            remainingSteps = 0;
            var visited = new HashSet<ShipRoomId>();
            var previous = new Dictionary<ShipRoomId, ShipRoomId>();
            var queue = new Queue<ShipRoomId>();
            visited.Add(currentRoom);
            queue.Enqueue(currentRoom);

            while (queue.Count > 0)
            {
                var room = queue.Dequeue();
                for (var i = 0; i < CorridorLinks.Length; i++)
                {
                    if (!CorridorLinks[i].Connects(room, out var neighbor))
                    {
                        continue;
                    }

                    if (!CanTraverseCorridor(i, mobilityKind, closedCorridorCount) ||
                        IsRoomSealed(ship, neighbor) ||
                        visited.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    previous[neighbor] = room;
                    if (neighbor == targetRoom)
                    {
                        return ResolvePath(currentRoom, targetRoom, previous, out nextRoom, out remainingSteps);
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private static bool ResolvePath(
            ShipRoomId currentRoom,
            ShipRoomId targetRoom,
            Dictionary<ShipRoomId, ShipRoomId> previous,
            out ShipRoomId nextRoom,
            out int remainingSteps)
        {
            nextRoom = targetRoom;
            remainingSteps = 0;
            var walker = targetRoom;
            while (previous.TryGetValue(walker, out var parent))
            {
                remainingSteps++;
                if (parent == currentRoom)
                {
                    nextRoom = walker;
                    return true;
                }

                walker = parent;
            }

            nextRoom = currentRoom;
            remainingSteps = 0;
            return false;
        }

        private static bool CanTraverseCorridor(
            int corridorIndex,
            IntruderMobilityKind mobilityKind,
            int closedCorridorCount)
        {
            return mobilityKind == IntruderMobilityKind.Flying || corridorIndex >= closedCorridorCount;
        }

        private static bool IsRoomSealed(ShipState ship, ShipRoomId roomId)
        {
            return ship.GetRoom(roomId).IsSealed;
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

        private readonly struct ShipRoomCorridorLink
        {
            public ShipRoomCorridorLink(ShipRoomId from, ShipRoomId to)
            {
                From = from;
                To = to;
            }

            public ShipRoomId From { get; }

            public ShipRoomId To { get; }

            public bool Connects(ShipRoomId roomId, out ShipRoomId neighbor)
            {
                if (From == roomId)
                {
                    neighbor = To;
                    return true;
                }

                if (To == roomId)
                {
                    neighbor = From;
                    return true;
                }

                neighbor = roomId;
                return false;
            }
        }
    }
}
