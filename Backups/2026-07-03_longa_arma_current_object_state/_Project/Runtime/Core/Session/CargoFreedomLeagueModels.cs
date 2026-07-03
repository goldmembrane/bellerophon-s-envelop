using System;

namespace Bellerophon.Core.Session
{
    public enum CargoFreedomLeagueKind
    {
        None,
        Negatif,
        Rebellion,
        Resistance,
        Revolution
    }

    public enum CargoFreedomLeagueBehaviorKind
    {
        None,
        CargoRaider,
        CargoShieldProjector,
        WeaponLooter,
        BombSaboteur
    }

    public enum CargoFreedomLeagueActionKind
    {
        None,
        CargoDamagedAndStored,
        RetargetedToAttacker,
        CargoShieldInstalled,
        ShieldedAreaAttack,
        WeaponLooted,
        BombInstalling,
        BombArmed,
        BombDetonated,
        RoomAttacked
    }

    public enum CargoFreedomLeagueDropKind
    {
        None,
        StoredCargo,
        StolenWeapon
    }

    public readonly struct CargoFreedomLeagueBoardingCraftProfile
    {
        public CargoFreedomLeagueBoardingCraftProfile(
            CargoFreedomLeagueKind kind,
            int health,
            float widthMeters,
            float lengthMeters,
            float heightMeters,
            int groupCount,
            int unitsPerGroup,
            string frontalMarkDescription)
        {
            if (kind == CargoFreedomLeagueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Boarding craft profile requires a concrete kind.");
            }

            if (health <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(health), "Boarding craft health must be positive.");
            }

            if (widthMeters <= 0f || lengthMeters <= 0f || heightMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(widthMeters), "Boarding craft dimensions must be positive.");
            }

            if (groupCount <= 0 || unitsPerGroup <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(groupCount), "Boarding craft payload counts must be positive.");
            }

            Kind = kind;
            Health = health;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            HeightMeters = heightMeters;
            GroupCount = groupCount;
            UnitsPerGroup = unitsPerGroup;
            FrontalMarkDescription = frontalMarkDescription ?? string.Empty;
        }

        public CargoFreedomLeagueKind Kind { get; }

        public int Health { get; }

        public float WidthMeters { get; }

        public float LengthMeters { get; }

        public float HeightMeters { get; }

        public int GroupCount { get; }

        public int UnitsPerGroup { get; }

        public int TotalUnitCount => GroupCount * UnitsPerGroup;

        public string FrontalMarkDescription { get; }
    }

    public readonly struct CargoFreedomLeagueProfile
    {
        public CargoFreedomLeagueProfile(
            CargoFreedomLeagueKind kind,
            IntruderDefinition intruderDefinition,
            CargoFreedomLeagueBehaviorKind behaviorKind,
            CargoFreedomLeagueBoardingCraftProfile boardingCraft,
            int attackDamage,
            float cargoDamagePercentPerAttack = 0f,
            float storedCargoRecoveryPercent = 0f,
            bool retargetsToAttackerWhenDamaged = false,
            bool installsCargoShield = false,
            float attackModeTransitionSeconds = 0f,
            float sweepDurationSeconds = 0f,
            EquipmentItemKind lootedWeaponKind = EquipmentItemKind.None,
            int lootedWeaponDamage = 0,
            float lootedWeaponMinimumRange = 0f,
            float lootedWeaponMaximumRange = 0f,
            float lootedWeaponAttackDelaySeconds = 0f,
            float transformDurationSeconds = 0f,
            float bombInstallDurationSeconds = 0f,
            float bombDetonationDelaySeconds = 0f,
            float supplyRoomDamagePercentPerSecond = 0f,
            float otherRoomDamagePercentPerSecond = 0f,
            float maximumSustainedAttackSeconds = 0f,
            bool dropsSpecialMissionChip = false,
            bool hasConfirmedMovementSpeed = true,
            bool hasConfirmedAttackDelay = true)
        {
            if (kind == CargoFreedomLeagueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Cargo Freedom League profile requires a concrete kind.");
            }

            if (behaviorKind == CargoFreedomLeagueBehaviorKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(behaviorKind), "Cargo Freedom League profile requires a behavior kind.");
            }

            if (intruderDefinition.Faction != IntruderFaction.CargoFreedomLeague)
            {
                throw new ArgumentOutOfRangeException(nameof(intruderDefinition), "Cargo Freedom League profile requires a Cargo Freedom League definition.");
            }

            if (boardingCraft.Kind != kind)
            {
                throw new ArgumentException("Boarding craft kind must match the unit profile kind.", nameof(boardingCraft));
            }

            Kind = kind;
            IntruderDefinition = intruderDefinition;
            BehaviorKind = behaviorKind;
            BoardingCraft = boardingCraft;
            AttackDamage = Math.Max(0, attackDamage);
            CargoDamagePercentPerAttack = Clamp01(cargoDamagePercentPerAttack);
            StoredCargoRecoveryPercent = Clamp01(storedCargoRecoveryPercent);
            RetargetsToAttackerWhenDamaged = retargetsToAttackerWhenDamaged;
            InstallsCargoShield = installsCargoShield;
            AttackModeTransitionSeconds = Math.Max(0f, attackModeTransitionSeconds);
            SweepDurationSeconds = Math.Max(0f, sweepDurationSeconds);
            LootedWeaponKind = lootedWeaponKind;
            LootedWeaponDamage = Math.Max(0, lootedWeaponDamage);
            LootedWeaponMinimumRange = Math.Max(0f, lootedWeaponMinimumRange);
            LootedWeaponMaximumRange = Math.Max(LootedWeaponMinimumRange, lootedWeaponMaximumRange);
            LootedWeaponAttackDelaySeconds = Math.Max(0f, lootedWeaponAttackDelaySeconds);
            TransformDurationSeconds = Math.Max(0f, transformDurationSeconds);
            BombInstallDurationSeconds = Math.Max(0f, bombInstallDurationSeconds);
            BombDetonationDelaySeconds = Math.Max(0f, bombDetonationDelaySeconds);
            SupplyRoomDamagePercentPerSecond = Math.Max(0f, supplyRoomDamagePercentPerSecond);
            OtherRoomDamagePercentPerSecond = Math.Max(0f, otherRoomDamagePercentPerSecond);
            MaximumSustainedAttackSeconds = Math.Max(0f, maximumSustainedAttackSeconds);
            DropsSpecialMissionChip = dropsSpecialMissionChip;
            HasConfirmedMovementSpeed = hasConfirmedMovementSpeed;
            HasConfirmedAttackDelay = hasConfirmedAttackDelay;
        }

        public CargoFreedomLeagueKind Kind { get; }

        public IntruderDefinition IntruderDefinition { get; }

        public CargoFreedomLeagueBehaviorKind BehaviorKind { get; }

        public CargoFreedomLeagueBoardingCraftProfile BoardingCraft { get; }

        public int AttackDamage { get; }

        public float CargoDamagePercentPerAttack { get; }

        public float StoredCargoRecoveryPercent { get; }

        public bool RetargetsToAttackerWhenDamaged { get; }

        public bool InstallsCargoShield { get; }

        public float AttackModeTransitionSeconds { get; }

        public float SweepDurationSeconds { get; }

        public EquipmentItemKind LootedWeaponKind { get; }

        public int LootedWeaponDamage { get; }

        public float LootedWeaponMinimumRange { get; }

        public float LootedWeaponMaximumRange { get; }

        public float LootedWeaponAttackDelaySeconds { get; }

        public float TransformDurationSeconds { get; }

        public float BombInstallDurationSeconds { get; }

        public float BombDetonationDelaySeconds { get; }

        public float SupplyRoomDamagePercentPerSecond { get; }

        public float OtherRoomDamagePercentPerSecond { get; }

        public float MaximumSustainedAttackSeconds { get; }

        public bool DropsSpecialMissionChip { get; }

        public bool HasConfirmedMovementSpeed { get; }

        public bool HasConfirmedAttackDelay { get; }

        public bool CanLootWeapon => LootedWeaponKind != EquipmentItemKind.None;

        public bool CanPlantSupplyRoomBomb => BombInstallDurationSeconds > 0f && BombDetonationDelaySeconds > 0f;

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }

    public readonly struct CargoFreedomLeagueState
    {
        private CargoFreedomLeagueState(
            CargoFreedomLeagueKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder,
            float elapsedSeconds,
            int appliedActionCount,
            float storedCargoPercent,
            bool cargoShieldInstalled,
            EquipmentItemKind stolenEquipmentKind,
            float bombInstallationProgressSeconds,
            bool bombArmed,
            float bombDetonationElapsedSeconds,
            bool bombDetonated)
        {
            Kind = kind;
            Definition = definition;
            Attempt = attempt;
            Intruder = intruder;
            ElapsedSeconds = Math.Max(0f, elapsedSeconds);
            AppliedActionCount = Math.Max(0, appliedActionCount);
            StoredCargoPercent = Clamp01(storedCargoPercent);
            CargoShieldInstalled = cargoShieldInstalled;
            StolenEquipmentKind = stolenEquipmentKind;
            BombInstallationProgressSeconds = Math.Max(0f, bombInstallationProgressSeconds);
            BombArmed = bombArmed;
            BombDetonationElapsedSeconds = Math.Max(0f, bombDetonationElapsedSeconds);
            BombDetonated = bombDetonated;
        }

        public CargoFreedomLeagueKind Kind { get; }

        public IntruderDefinition Definition { get; }

        public IntrusionAttemptState Attempt { get; }

        public IntruderEntityState Intruder { get; }

        public float ElapsedSeconds { get; }

        public int AppliedActionCount { get; }

        public float StoredCargoPercent { get; }

        public bool CargoShieldInstalled { get; }

        public EquipmentItemKind StolenEquipmentKind { get; }

        public float BombInstallationProgressSeconds { get; }

        public bool BombArmed { get; }

        public float BombDetonationElapsedSeconds { get; }

        public bool BombDetonated { get; }

        public bool IsActive => Kind != CargoFreedomLeagueKind.None && Intruder.IsActive;

        public bool IsResolved => Kind != CargoFreedomLeagueKind.None && Intruder.IsResolved;

        public bool HasStolenEquipment => StolenEquipmentKind != EquipmentItemKind.None;

        public ShipRoomId TargetRoom => Intruder.TargetRoom;

        public static CargoFreedomLeagueState None => new CargoFreedomLeagueState(
            CargoFreedomLeagueKind.None,
            default,
            IntrusionAttemptState.None,
            IntruderEntityState.None,
            0f,
            0,
            0f,
            false,
            EquipmentItemKind.None,
            0f,
            false,
            0f,
            false);

        public static CargoFreedomLeagueState Start(
            CargoFreedomLeagueKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder)
        {
            if (kind == CargoFreedomLeagueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Cargo Freedom League state requires a concrete kind.");
            }

            if (!intruder.IsActive)
            {
                throw new ArgumentException("Cargo Freedom League intruder entity must be active.", nameof(intruder));
            }

            return new CargoFreedomLeagueState(
                kind,
                definition,
                attempt,
                intruder,
                0f,
                0,
                0f,
                false,
                EquipmentItemKind.None,
                0f,
                false,
                0f,
                false);
        }

        public CargoFreedomLeagueState WithIntruder(IntruderEntityState intruder)
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                intruder,
                ElapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithElapsed(float elapsedSeconds)
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                elapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithActionApplied()
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount + 1,
                StoredCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithStoredCargoPercent(float storedCargoPercent)
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount,
                storedCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithCargoShieldInstalled(bool cargoShieldInstalled)
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                cargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithStolenEquipment(EquipmentItemKind equipmentKind)
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                CargoShieldInstalled,
                equipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithBombInstallationProgress(float progressSeconds)
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                progressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithBombArmed()
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                true,
                BombDetonationElapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithBombDetonationElapsed(float elapsedSeconds)
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                elapsedSeconds,
                BombDetonated);
        }

        public CargoFreedomLeagueState WithBombDetonated()
        {
            return new CargoFreedomLeagueState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                AppliedActionCount,
                StoredCargoPercent,
                CargoShieldInstalled,
                StolenEquipmentKind,
                BombInstallationProgressSeconds,
                BombArmed,
                BombDetonationElapsedSeconds,
                true);
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

    public readonly struct CargoFreedomLeagueTickResult
    {
        public CargoFreedomLeagueTickResult(
            CargoFreedomLeagueState state,
            ShipState ship,
            CargoState cargo,
            CargoFreedomLeagueActionKind actionKind,
            int roomDamageApplied,
            float cargoDamagePercentApplied,
            EquipmentItemKind lootedEquipmentKind = EquipmentItemKind.None,
            bool cargoShieldInstalled = false,
            bool bombDetonated = false,
            bool retargeted = false)
        {
            State = state;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            Cargo = cargo;
            ActionKind = actionKind;
            RoomDamageApplied = Math.Max(0, roomDamageApplied);
            CargoDamagePercentApplied = cargoDamagePercentApplied < 0f ? 0f : cargoDamagePercentApplied;
            LootedEquipmentKind = lootedEquipmentKind;
            CargoShieldInstalled = cargoShieldInstalled;
            BombDetonated = bombDetonated;
            Retargeted = retargeted;
        }

        public CargoFreedomLeagueState State { get; }

        public ShipState Ship { get; }

        public CargoState Cargo { get; }

        public CargoFreedomLeagueActionKind ActionKind { get; }

        public int RoomDamageApplied { get; }

        public float CargoDamagePercentApplied { get; }

        public EquipmentItemKind LootedEquipmentKind { get; }

        public bool CargoShieldInstalled { get; }

        public bool BombDetonated { get; }

        public bool Retargeted { get; }

        public bool AppliedAction => ActionKind != CargoFreedomLeagueActionKind.None;
    }

    public readonly struct CargoFreedomLeagueDropResult
    {
        public CargoFreedomLeagueDropResult(
            CargoFreedomLeagueDropKind dropKind,
            float recoveredCargoPercent,
            EquipmentItemKind droppedEquipmentKind,
            bool resistanceChipDropped,
            bool revolutionChipDropped)
        {
            DropKind = dropKind;
            RecoveredCargoPercent = recoveredCargoPercent < 0f ? 0f : recoveredCargoPercent;
            DroppedEquipmentKind = droppedEquipmentKind;
            ResistanceChipDropped = resistanceChipDropped;
            RevolutionChipDropped = revolutionChipDropped;
        }

        public CargoFreedomLeagueDropKind DropKind { get; }

        public float RecoveredCargoPercent { get; }

        public EquipmentItemKind DroppedEquipmentKind { get; }

        public bool ResistanceChipDropped { get; }

        public bool RevolutionChipDropped { get; }

        public bool HasDrop =>
            DropKind != CargoFreedomLeagueDropKind.None ||
            ResistanceChipDropped ||
            RevolutionChipDropped;

        public static CargoFreedomLeagueDropResult None => new CargoFreedomLeagueDropResult(
            CargoFreedomLeagueDropKind.None,
            0f,
            EquipmentItemKind.None,
            false,
            false);
    }

    public static class CargoFreedomLeagueRules
    {
        public const string NegatifDefinitionId = "cargo-freedom-negatif";
        public const string RebellionDefinitionId = "cargo-freedom-rebellion";
        public const string ResistanceDefinitionId = "cargo-freedom-resistance";
        public const string RevolutionDefinitionId = "cargo-freedom-revolution";

        public const int NegatifHealth = 40;
        public const float NegatifMovementSpeed = 2.8f;
        public const int NegatifAttackDamage = 8;
        public const float NegatifAttackRange = 1f;
        public const float NegatifCargoDamagePercentPerAttack = 0.08f;
        public const float NegatifStoredCargoRecoveryPercent = 0.7f;
        public const float NegatifUnconfirmedAttackDelaySeconds = 0f;

        public const int RebellionHealth = 60;
        public const float RebellionMovementSpeed = 1.8f;
        public const int RebellionAttackDamage = 5;
        public const float RebellionAttackRange = 5f;
        public const float RebellionAttackDelaySeconds = 0.2f;
        public const float RebellionAttackModeTransitionSeconds = 2f;
        public const float RebellionSweepDurationSeconds = 0.8f;

        public const int ResistanceHealth = 100;
        public const float ResistanceMovementSpeed = 2.5f;
        public const int ResistanceAttackDamage = 10;
        public const float ResistanceAttackRange = 1.5f;
        public const float ResistanceAttackDelaySeconds = 1.5f;
        public const EquipmentItemKind ResistanceLootedWeaponKind = EquipmentItemKind.Musket;
        public const int ResistanceLootedWeaponDamage = 50;
        public const float ResistanceLootedWeaponMinimumRange = 5f;
        public const float ResistanceLootedWeaponMaximumRange = 7f;
        public const float ResistanceLootedWeaponAttackDelaySeconds = 3.5f;

        public const int RevolutionHealth = 200;
        public const float RevolutionUnconfirmedMovementSpeed = 0f;
        public const int RevolutionAttackDamage = 5;
        public const float RevolutionAttackRange = 3f;
        public const float RevolutionAttackDelaySeconds = 0.8f;
        public const float RevolutionTransformDurationSeconds = 1.5f;
        public const float RevolutionMaximumSustainedAttackSeconds = 8f;
        public const float RevolutionBombInstallDurationSeconds = 3f;
        public const float RevolutionBombDetonationDelaySeconds = 10f;
        public const float RevolutionBombThresholdPercent = 0.3f;
        public const float RevolutionSupplyRoomDamagePercentPerSecond = 0.02f;
        public const float RevolutionOtherRoomDamagePercentPerSecond = 0.01f;

        public const int NegatifBoardingCraftHealth = 300;
        public const int RebellionBoardingCraftHealth = 450;
        public const int ResistanceBoardingCraftHealth = 700;
        public const int RevolutionBoardingCraftHealth = 1000;

        private static readonly CargoFreedomLeagueKind[] SourceKindOrder =
        {
            CargoFreedomLeagueKind.Negatif,
            CargoFreedomLeagueKind.Rebellion,
            CargoFreedomLeagueKind.Resistance,
            CargoFreedomLeagueKind.Revolution
        };

        private static readonly ShipRoomId[] PostBombTargetOrder =
        {
            ShipRoomId.CargoHold,
            ShipRoomId.Armory,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Cockpit
        };

        public static CargoFreedomLeagueProfile[] CreateAllSourceCargoFreedomLeagueProfiles()
        {
            var profiles = new CargoFreedomLeagueProfile[SourceKindOrder.Length];
            for (var i = 0; i < SourceKindOrder.Length; i++)
            {
                profiles[i] = GetProfile(SourceKindOrder[i]);
            }

            return profiles;
        }

        public static CargoFreedomLeagueProfile GetProfile(CargoFreedomLeagueKind kind)
        {
            switch (kind)
            {
                case CargoFreedomLeagueKind.Negatif:
                    return new CargoFreedomLeagueProfile(
                        kind,
                        CreateDefinition(
                            NegatifDefinitionId,
                            "Negatif",
                            IntruderObjectiveType.AttackCargo,
                            NegatifHealth,
                            NegatifMovementSpeed,
                            NegatifAttackRange,
                            NegatifUnconfirmedAttackDelaySeconds,
                            CreateCargoTargetPriorities()),
                        CargoFreedomLeagueBehaviorKind.CargoRaider,
                        GetBoardingCraftProfile(kind),
                        NegatifAttackDamage,
                        cargoDamagePercentPerAttack: NegatifCargoDamagePercentPerAttack,
                        storedCargoRecoveryPercent: NegatifStoredCargoRecoveryPercent,
                        retargetsToAttackerWhenDamaged: true,
                        hasConfirmedAttackDelay: false);
                case CargoFreedomLeagueKind.Rebellion:
                    return new CargoFreedomLeagueProfile(
                        kind,
                        CreateDefinition(
                            RebellionDefinitionId,
                            "Rebellion",
                            IntruderObjectiveType.AttackCargo,
                            RebellionHealth,
                            RebellionMovementSpeed,
                            RebellionAttackRange,
                            RebellionAttackDelaySeconds,
                            CreateCargoTargetPriorities()),
                        CargoFreedomLeagueBehaviorKind.CargoShieldProjector,
                        GetBoardingCraftProfile(kind),
                        RebellionAttackDamage,
                        installsCargoShield: true,
                        attackModeTransitionSeconds: RebellionAttackModeTransitionSeconds,
                        sweepDurationSeconds: RebellionSweepDurationSeconds);
                case CargoFreedomLeagueKind.Resistance:
                    return new CargoFreedomLeagueProfile(
                        kind,
                        CreateDefinition(
                            ResistanceDefinitionId,
                            "Resistance",
                            IntruderObjectiveType.OccupyRoom,
                            ResistanceHealth,
                            ResistanceMovementSpeed,
                            ResistanceAttackRange,
                            ResistanceAttackDelaySeconds,
                            CreateSupplyRoomTargetPriorities()),
                        CargoFreedomLeagueBehaviorKind.WeaponLooter,
                        GetBoardingCraftProfile(kind),
                        ResistanceAttackDamage,
                        lootedWeaponKind: ResistanceLootedWeaponKind,
                        lootedWeaponDamage: ResistanceLootedWeaponDamage,
                        lootedWeaponMinimumRange: ResistanceLootedWeaponMinimumRange,
                        lootedWeaponMaximumRange: ResistanceLootedWeaponMaximumRange,
                        lootedWeaponAttackDelaySeconds: ResistanceLootedWeaponAttackDelaySeconds,
                        dropsSpecialMissionChip: true);
                case CargoFreedomLeagueKind.Revolution:
                    return new CargoFreedomLeagueProfile(
                        kind,
                        CreateDefinition(
                            RevolutionDefinitionId,
                            "Revolution",
                            IntruderObjectiveType.DestroyShip,
                            RevolutionHealth,
                            RevolutionUnconfirmedMovementSpeed,
                            RevolutionAttackRange,
                            RevolutionAttackDelaySeconds,
                            CreateSupplyRoomTargetPriorities()),
                        CargoFreedomLeagueBehaviorKind.BombSaboteur,
                        GetBoardingCraftProfile(kind),
                        RevolutionAttackDamage,
                        transformDurationSeconds: RevolutionTransformDurationSeconds,
                        bombInstallDurationSeconds: RevolutionBombInstallDurationSeconds,
                        bombDetonationDelaySeconds: RevolutionBombDetonationDelaySeconds,
                        supplyRoomDamagePercentPerSecond: RevolutionSupplyRoomDamagePercentPerSecond,
                        otherRoomDamagePercentPerSecond: RevolutionOtherRoomDamagePercentPerSecond,
                        maximumSustainedAttackSeconds: RevolutionMaximumSustainedAttackSeconds,
                        dropsSpecialMissionChip: true,
                        hasConfirmedMovementSpeed: false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported Cargo Freedom League kind.");
            }
        }

        public static CargoFreedomLeagueBoardingCraftProfile GetBoardingCraftProfile(
            CargoFreedomLeagueKind kind)
        {
            switch (kind)
            {
                case CargoFreedomLeagueKind.Negatif:
                    return new CargoFreedomLeagueBoardingCraftProfile(
                        kind,
                        NegatifBoardingCraftHealth,
                        5f,
                        10f,
                        3f,
                        3,
                        5,
                        "cargo mark with two crossed scratches");
                case CargoFreedomLeagueKind.Rebellion:
                    return new CargoFreedomLeagueBoardingCraftProfile(
                        kind,
                        RebellionBoardingCraftHealth,
                        6f,
                        10f,
                        4f,
                        2,
                        3,
                        "shielded cargo mark with two wing pairs");
                case CargoFreedomLeagueKind.Resistance:
                    return new CargoFreedomLeagueBoardingCraftProfile(
                        kind,
                        ResistanceBoardingCraftHealth,
                        6f,
                        8f,
                        7f,
                        2,
                        2,
                        "crossed guns with wings and cargo behind");
                case CargoFreedomLeagueKind.Revolution:
                    return new CargoFreedomLeagueBoardingCraftProfile(
                        kind,
                        RevolutionBoardingCraftHealth,
                        10f,
                        15f,
                        10f,
                        1,
                        1,
                        "mushroom cloud with cargo flying out");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported Cargo Freedom League craft kind.");
            }
        }

        public static CargoFreedomLeagueKind SelectKindForSeed(int seed)
        {
            var index = (seed & 0x7fffffff) % SourceKindOrder.Length;
            return SourceKindOrder[index];
        }

        public static CargoFreedomLeagueState CreateCargoFreedomLeagueIntrusionFromHazard(
            TransportHazardState hazard,
            int boardingIndex,
            ShipRoomId playerRoom = ShipRoomId.Cockpit)
        {
            if (hazard.HazardType != TransportHazardType.CargoFreedomLeagueRegion)
            {
                throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Cargo Freedom League intrusion requires a Cargo Freedom League region hazard.");
            }

            if (boardingIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boardingIndex), "Cargo Freedom League boarding index must be positive.");
            }

            var seed = CreateBoardingSeed(hazard.Seed, boardingIndex);
            var kind = SelectKindForSeed(seed);
            return CreateCargoFreedomLeagueIntrusionForSeed(
                kind,
                seed,
                playerRoom,
                "cargo-freedom-" + hazard.Seed + "-" + boardingIndex);
        }

        public static CargoFreedomLeagueState CreateCargoFreedomLeagueIntrusionForSeed(
            CargoFreedomLeagueKind kind,
            int seed,
            ShipRoomId playerRoom,
            string attemptId = "cargo-freedom-validation")
        {
            if (kind == CargoFreedomLeagueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Cargo Freedom League intrusion requires a concrete kind.");
            }

            var profile = GetProfile(kind);
            var attempt = IntruderRules.CreateAttempt(attemptId, profile.IntruderDefinition, seed, playerRoom);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, profile.IntruderDefinition);
            return CargoFreedomLeagueState.Start(kind, profile.IntruderDefinition, boarded, intruder);
        }

        public static ExternalTargetState CreateBoardingCraftExternalTarget(TransportHazardState hazard)
        {
            if (hazard.HazardType != TransportHazardType.CargoFreedomLeagueRegion)
            {
                throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Cargo Freedom League external target requires a Cargo Freedom League region hazard.");
            }

            var kind = SelectKindForSeed(hazard.Seed);
            var craft = GetBoardingCraftProfile(kind);
            return new ExternalTargetState(
                "cargo-freedom-" + FormatCargoFreedomLeagueKind(kind).ToLowerInvariant() + "-" + hazard.Seed,
                ExternalTargetType.CargoFreedomLeagueBoardingCraft,
                craft.Health,
                craft.Health,
                CreateTargetCoordinate(hazard.Seed, 83, 0.58f),
                CreateTargetCoordinate(hazard.Seed, 109, 0.42f),
                ManualTurretState.DefaultAsteroidHitRadius);
        }

        public static CargoFreedomLeagueTickResult TickCargoFreedomLeague(
            CargoFreedomLeagueState state,
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
                return new CargoFreedomLeagueTickResult(
                    state,
                    ship,
                    cargo,
                    CargoFreedomLeagueActionKind.None,
                    0,
                    0f);
            }

            var tickedIntruder = IntruderRules.TickStatusEffects(state.Intruder, deltaSeconds);
            if (!tickedIntruder.IsActive)
            {
                return new CargoFreedomLeagueTickResult(
                    state.WithIntruder(tickedIntruder),
                    ship,
                    cargo,
                    CargoFreedomLeagueActionKind.None,
                    0,
                    0f);
            }

            var movementBlocked = CombatStatusEffectRules.BlocksMovement(tickedIntruder.StatusEffects);
            var intruder = movementBlocked || tickedIntruder.CurrentRoom == tickedIntruder.TargetRoom
                ? tickedIntruder
                : IntruderRules.MoveToReachableTargetRoom(tickedIntruder, ship);
            var nextState = state
                .WithIntruder(intruder)
                .WithElapsed(state.ElapsedSeconds + deltaSeconds);

            if (intruder.CurrentRoom != intruder.TargetRoom)
            {
                return new CargoFreedomLeagueTickResult(
                    nextState,
                    ship,
                    cargo,
                    CargoFreedomLeagueActionKind.None,
                    0,
                    0f);
            }

            var profile = GetProfile(state.Kind);
            switch (profile.BehaviorKind)
            {
                case CargoFreedomLeagueBehaviorKind.CargoRaider:
                    return ApplyNegatifCargoRaid(nextState, profile, ship, cargo);
                case CargoFreedomLeagueBehaviorKind.CargoShieldProjector:
                    return ApplyRebellionShieldOrAttack(nextState, profile, ship, cargo);
                case CargoFreedomLeagueBehaviorKind.WeaponLooter:
                    return ApplyResistanceWeaponLoot(nextState, profile, ship, cargo);
                case CargoFreedomLeagueBehaviorKind.BombSaboteur:
                    return ApplyRevolutionSabotage(nextState, profile, ship, cargo, deltaSeconds);
                default:
                    return new CargoFreedomLeagueTickResult(
                        nextState,
                        ship,
                        cargo,
                        CargoFreedomLeagueActionKind.None,
                        0,
                        0f);
            }
        }

        public static CargoFreedomLeagueState ApplyDamage(CargoFreedomLeagueState state, int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Cargo Freedom League damage cannot be negative.");
            }

            if (!state.IsActive || damage == 0)
            {
                return state;
            }

            return state.WithIntruder(state.Intruder.WithDamage(damage));
        }

        public static CargoFreedomLeagueState RetargetToAttacker(
            CargoFreedomLeagueState state,
            ShipRoomId attackerRoom)
        {
            if (!state.IsActive)
            {
                return state;
            }

            var profile = GetProfile(state.Kind);
            if (!profile.RetargetsToAttackerWhenDamaged)
            {
                return state;
            }

            return state.WithIntruder(state.Intruder.WithTarget(
                IntruderTargetType.Player,
                attackerRoom,
                IntruderObjectiveType.AttackPlayer));
        }

        public static CargoFreedomLeagueDropResult ResolveDrops(
            CargoFreedomLeagueState state,
            bool specialMissionAccepted)
        {
            if (state.Kind == CargoFreedomLeagueKind.None || state.Intruder.IsActive)
            {
                return CargoFreedomLeagueDropResult.None;
            }

            var profile = GetProfile(state.Kind);
            var dropKind = CargoFreedomLeagueDropKind.None;
            var recoveredCargo = 0f;
            var equipment = EquipmentItemKind.None;
            if (state.Kind == CargoFreedomLeagueKind.Negatif && state.StoredCargoPercent > 0f)
            {
                dropKind = CargoFreedomLeagueDropKind.StoredCargo;
                recoveredCargo = state.StoredCargoPercent * profile.StoredCargoRecoveryPercent;
            }

            if (state.Kind == CargoFreedomLeagueKind.Resistance && state.HasStolenEquipment)
            {
                dropKind = CargoFreedomLeagueDropKind.StolenWeapon;
                equipment = state.StolenEquipmentKind;
            }

            var resistanceChip = specialMissionAccepted &&
                                 state.Kind == CargoFreedomLeagueKind.Resistance &&
                                 profile.DropsSpecialMissionChip;
            var revolutionChip = specialMissionAccepted &&
                                 state.Kind == CargoFreedomLeagueKind.Revolution &&
                                 profile.DropsSpecialMissionChip;
            return new CargoFreedomLeagueDropResult(
                dropKind,
                recoveredCargo,
                equipment,
                resistanceChip,
                revolutionChip);
        }

        public static string FormatCargoFreedomLeagueKind(CargoFreedomLeagueKind kind)
        {
            switch (kind)
            {
                case CargoFreedomLeagueKind.Negatif:
                    return "Negatif";
                case CargoFreedomLeagueKind.Rebellion:
                    return "Rebellion";
                case CargoFreedomLeagueKind.Resistance:
                    return "Resistance";
                case CargoFreedomLeagueKind.Revolution:
                    return "Revolution";
                default:
                    return "None";
            }
        }

        private static CargoFreedomLeagueTickResult ApplyNegatifCargoRaid(
            CargoFreedomLeagueState state,
            CargoFreedomLeagueProfile profile,
            ShipState ship,
            CargoState cargo)
        {
            var nextCargo = cargo.WithDamagePercent(profile.CargoDamagePercentPerAttack);
            var nextState = state
                .WithStoredCargoPercent(state.StoredCargoPercent + profile.CargoDamagePercentPerAttack)
                .WithActionApplied();
            return new CargoFreedomLeagueTickResult(
                nextState,
                ship,
                nextCargo,
                CargoFreedomLeagueActionKind.CargoDamagedAndStored,
                0,
                profile.CargoDamagePercentPerAttack);
        }

        private static CargoFreedomLeagueTickResult ApplyRebellionShieldOrAttack(
            CargoFreedomLeagueState state,
            CargoFreedomLeagueProfile profile,
            ShipState ship,
            CargoState cargo)
        {
            if (!state.CargoShieldInstalled)
            {
                var shieldedState = state
                    .WithCargoShieldInstalled(true)
                    .WithActionApplied();
                return new CargoFreedomLeagueTickResult(
                    shieldedState,
                    ship,
                    cargo,
                    CargoFreedomLeagueActionKind.CargoShieldInstalled,
                    0,
                    0f,
                    cargoShieldInstalled: true);
            }

            var room = ship.GetRoom(ShipRoomId.CargoHold);
            var nextShip = ship.WithRoom(ShipRoomId.CargoHold, room.WithDamage(profile.AttackDamage));
            return new CargoFreedomLeagueTickResult(
                state.WithActionApplied(),
                nextShip,
                cargo,
                CargoFreedomLeagueActionKind.ShieldedAreaAttack,
                profile.AttackDamage,
                0f,
                cargoShieldInstalled: true);
        }

        private static CargoFreedomLeagueTickResult ApplyResistanceWeaponLoot(
            CargoFreedomLeagueState state,
            CargoFreedomLeagueProfile profile,
            ShipState ship,
            CargoState cargo)
        {
            if (!state.HasStolenEquipment)
            {
                var lootedState = state
                    .WithStolenEquipment(profile.LootedWeaponKind)
                    .WithActionApplied();
                return new CargoFreedomLeagueTickResult(
                    lootedState,
                    ship,
                    cargo,
                    CargoFreedomLeagueActionKind.WeaponLooted,
                    0,
                    0f,
                    lootedEquipmentKind: profile.LootedWeaponKind);
            }

            var room = ship.GetRoom(state.Intruder.CurrentRoom);
            var nextShip = ship.WithRoom(state.Intruder.CurrentRoom, room.WithDamage(profile.AttackDamage));
            return new CargoFreedomLeagueTickResult(
                state.WithActionApplied(),
                nextShip,
                cargo,
                CargoFreedomLeagueActionKind.RoomAttacked,
                profile.AttackDamage,
                0f,
                lootedEquipmentKind: state.StolenEquipmentKind);
        }

        private static CargoFreedomLeagueTickResult ApplyRevolutionSabotage(
            CargoFreedomLeagueState state,
            CargoFreedomLeagueProfile profile,
            ShipState ship,
            CargoState cargo,
            float deltaSeconds)
        {
            if (state.BombArmed && !state.BombDetonated)
            {
                return TickRevolutionBomb(state, profile, ship, cargo, deltaSeconds);
            }

            if (state.Intruder.CurrentRoom == ShipRoomId.SupplyRoom)
            {
                var supplyRoom = ship.GetRoom(ShipRoomId.SupplyRoom);
                if (!state.BombDetonated && supplyRoom.DurabilityPercent <= RevolutionBombThresholdPercent)
                {
                    var progress = state.BombInstallationProgressSeconds + deltaSeconds;
                    var installingState = state
                        .WithBombInstallationProgress(progress)
                        .WithActionApplied();
                    if (progress >= profile.BombInstallDurationSeconds)
                    {
                        return new CargoFreedomLeagueTickResult(
                            installingState.WithBombArmed(),
                            ship,
                            cargo,
                            CargoFreedomLeagueActionKind.BombArmed,
                            0,
                            0f);
                    }

                    return new CargoFreedomLeagueTickResult(
                        installingState,
                        ship,
                        cargo,
                        CargoFreedomLeagueActionKind.BombInstalling,
                        0,
                        0f);
                }

                var damage = CalculateRoomDamageFromPercent(
                    supplyRoom,
                    profile.SupplyRoomDamagePercentPerSecond,
                    deltaSeconds);
                var damagedShip = ship.WithRoom(ShipRoomId.SupplyRoom, supplyRoom.WithDamage(damage));
                return new CargoFreedomLeagueTickResult(
                    state.WithActionApplied(),
                    damagedShip,
                    cargo,
                    CargoFreedomLeagueActionKind.RoomAttacked,
                    damage,
                    0f);
            }

            var targetRoom = ship.GetRoom(state.Intruder.CurrentRoom);
            var roomDamage = CalculateRoomDamageFromPercent(
                targetRoom,
                profile.OtherRoomDamagePercentPerSecond,
                deltaSeconds);
            return new CargoFreedomLeagueTickResult(
                state.WithActionApplied(),
                ship.WithRoom(state.Intruder.CurrentRoom, targetRoom.WithDamage(roomDamage)),
                cargo,
                CargoFreedomLeagueActionKind.RoomAttacked,
                roomDamage,
                0f);
        }

        private static CargoFreedomLeagueTickResult TickRevolutionBomb(
            CargoFreedomLeagueState state,
            CargoFreedomLeagueProfile profile,
            ShipState ship,
            CargoState cargo,
            float deltaSeconds)
        {
            var elapsed = state.BombDetonationElapsedSeconds + deltaSeconds;
            var timedState = state.WithBombDetonationElapsed(elapsed).WithActionApplied();
            if (elapsed < profile.BombDetonationDelaySeconds)
            {
                return new CargoFreedomLeagueTickResult(
                    timedState,
                    ship,
                    cargo,
                    CargoFreedomLeagueActionKind.BombArmed,
                    0,
                    0f);
            }

            var supplyRoom = ship.GetRoom(ShipRoomId.SupplyRoom);
            var damage = supplyRoom.CurrentDurability;
            var nextShip = ship.WithRoom(ShipRoomId.SupplyRoom, supplyRoom.WithDamage(damage));
            var retarget = SelectPostBombTargetRoom(state.Attempt.Seed);
            var nextIntruder = state.Intruder.WithTarget(
                IntruderTargetType.Room,
                retarget,
                IntruderObjectiveType.DestroyShip);
            var detonatedState = timedState
                .WithBombDetonated()
                .WithIntruder(nextIntruder);
            return new CargoFreedomLeagueTickResult(
                detonatedState,
                nextShip,
                cargo,
                CargoFreedomLeagueActionKind.BombDetonated,
                damage,
                0f,
                bombDetonated: true,
                retargeted: true);
        }

        private static IntruderDefinition CreateDefinition(
            string definitionId,
            string displayName,
            IntruderObjectiveType objective,
            int maxHealth,
            float movementSpeed,
            float attackRange,
            float attackDelaySeconds,
            IntruderTargetPriority[] targetPriorities)
        {
            return new IntruderDefinition(
                definitionId,
                displayName,
                IntruderFaction.CargoFreedomLeague,
                objective,
                maxHealth,
                movementSpeed,
                attackRange,
                attackDelaySeconds,
                targetPriorities,
                IntruderMobilityKind.Walking);
        }

        private static IntruderTargetPriority[] CreateCargoTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Cargo, ShipRoomId.CargoHold, 0),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.CargoHold, 1)
            };
        }

        private static IntruderTargetPriority[] CreateSupplyRoomTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.SupplyRoom, 0)
            };
        }

        private static ShipRoomId SelectPostBombTargetRoom(int seed)
        {
            var index = (seed & 0x7fffffff) % PostBombTargetOrder.Length;
            return PostBombTargetOrder[index];
        }

        private static int CalculateRoomDamageFromPercent(
            ShipRoomState room,
            float damagePercentPerSecond,
            float deltaSeconds)
        {
            if (damagePercentPerSecond <= 0f || deltaSeconds <= 0f)
            {
                return 0;
            }

            return RoundUpToInt(room.MaxDurability * damagePercentPerSecond * deltaSeconds);
        }

        private static int RoundUpToInt(float value)
        {
            return value <= 0f ? 0 : (int)Math.Ceiling(value);
        }

        private static int CreateBoardingSeed(int hazardSeed, int boardingIndex)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + hazardSeed;
                hash = hash * 31 + boardingIndex;
                return hash & 0x7fffffff;
            }
        }

        private static float CreateTargetCoordinate(int seed, int salt, float scale)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * 397 ^ salt;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                var normalized = ((hash & 0x7fffffff) % 2001) / 1000f - 1f;
                return Clamp(normalized * scale, -0.9f, 0.9f);
            }
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
}
