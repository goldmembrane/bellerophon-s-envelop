using System;

namespace Bellerophon.Core.Session
{
    public enum SpacePirateKind
    {
        None,
        Pahur,
        Kurus,
        Istante,
        Ata
    }

    public enum SpacePirateBehaviorKind
    {
        None,
        RocketTrooper,
        ShieldBearer,
        EliteSoldier,
        Commander
    }

    public enum SpacePirateActionKind
    {
        None,
        RocketAreaAttack,
        ShieldGuard,
        ShieldBash,
        MusketShot,
        DaggerSlash,
        CommandIssued,
        SabotagePreparing,
        SabotageApplied,
        SubordinatesStopped
    }

    public enum SpacePirateFormationKind
    {
        None,
        Protective,
        Breakthrough
    }

    public enum SpacePirateSabotageKind
    {
        None,
        EngineOutputReduction,
        ControlRoomHack,
        AutoPilotDisable,
        ArmoryTurretDisable,
        SupplyRoomBomb,
        CargoHoldBomb
    }

    public readonly struct SpacePirateBoardingCraftProfile
    {
        public SpacePirateBoardingCraftProfile(
            SpacePirateKind kind,
            int health,
            float widthMeters,
            float lengthMeters,
            float heightMeters,
            int payloadUnitCount,
            string frontalMarkDescription)
        {
            if (kind == SpacePirateKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Space pirate boarding craft requires a concrete kind.");
            }

            if (health <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(health), "Boarding craft health must be positive.");
            }

            if (widthMeters <= 0f || lengthMeters <= 0f || heightMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(widthMeters), "Boarding craft dimensions must be positive.");
            }

            if (payloadUnitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadUnitCount), "Boarding craft payload count must be positive.");
            }

            Kind = kind;
            Health = health;
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            HeightMeters = heightMeters;
            PayloadUnitCount = payloadUnitCount;
            FrontalMarkDescription = frontalMarkDescription ?? string.Empty;
        }

        public SpacePirateKind Kind { get; }

        public int Health { get; }

        public float WidthMeters { get; }

        public float LengthMeters { get; }

        public float HeightMeters { get; }

        public int PayloadUnitCount { get; }

        public string FrontalMarkDescription { get; }
    }

    public readonly struct SpacePirateProfile
    {
        public SpacePirateProfile(
            SpacePirateKind kind,
            IntruderDefinition intruderDefinition,
            SpacePirateBehaviorKind behaviorKind,
            SpacePirateBoardingCraftProfile boardingCraft,
            int primaryDamage,
            float primaryMinimumRange,
            float primaryMaximumRange,
            float primaryDelaySeconds,
            int secondaryDamage = 0,
            float secondaryMinimumRange = 0f,
            float secondaryMaximumRange = 0f,
            float secondaryDelaySeconds = 0f,
            int shieldDurability = 0,
            float shieldBashRadiusMeters = 0f,
            float shieldBashWindupSeconds = 0f,
            float defensiveStanceDelaySeconds = 0f,
            float maximumFireDurationSeconds = 0f,
            float reloadWaitSeconds = 0f,
            float commandRadiusMeters = 0f,
            float commandedMovementSpeed = 0f,
            float sabotageCastSeconds = 0f,
            float sabotageRecoverySeconds = 0f,
            float bombInstallSeconds = 0f,
            bool issuesFactionCommands = false)
        {
            if (kind == SpacePirateKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Space pirate profile requires a concrete kind.");
            }

            if (behaviorKind == SpacePirateBehaviorKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(behaviorKind), "Space pirate profile requires a behavior kind.");
            }

            if (intruderDefinition.Faction != IntruderFaction.SpacePirate)
            {
                throw new ArgumentOutOfRangeException(nameof(intruderDefinition), "Space pirate profile requires a space pirate definition.");
            }

            if (boardingCraft.Kind != kind)
            {
                throw new ArgumentException("Boarding craft kind must match pirate kind.", nameof(boardingCraft));
            }

            Kind = kind;
            IntruderDefinition = intruderDefinition;
            BehaviorKind = behaviorKind;
            BoardingCraft = boardingCraft;
            PrimaryDamage = Math.Max(0, primaryDamage);
            PrimaryMinimumRange = Math.Max(0f, primaryMinimumRange);
            PrimaryMaximumRange = Math.Max(PrimaryMinimumRange, primaryMaximumRange);
            PrimaryDelaySeconds = Math.Max(0f, primaryDelaySeconds);
            SecondaryDamage = Math.Max(0, secondaryDamage);
            SecondaryMinimumRange = Math.Max(0f, secondaryMinimumRange);
            SecondaryMaximumRange = Math.Max(SecondaryMinimumRange, secondaryMaximumRange);
            SecondaryDelaySeconds = Math.Max(0f, secondaryDelaySeconds);
            ShieldDurability = Math.Max(0, shieldDurability);
            ShieldBashRadiusMeters = Math.Max(0f, shieldBashRadiusMeters);
            ShieldBashWindupSeconds = Math.Max(0f, shieldBashWindupSeconds);
            DefensiveStanceDelaySeconds = Math.Max(0f, defensiveStanceDelaySeconds);
            MaximumFireDurationSeconds = Math.Max(0f, maximumFireDurationSeconds);
            ReloadWaitSeconds = Math.Max(0f, reloadWaitSeconds);
            CommandRadiusMeters = Math.Max(0f, commandRadiusMeters);
            CommandedMovementSpeed = Math.Max(0f, commandedMovementSpeed);
            SabotageCastSeconds = Math.Max(0f, sabotageCastSeconds);
            SabotageRecoverySeconds = Math.Max(0f, sabotageRecoverySeconds);
            BombInstallSeconds = Math.Max(0f, bombInstallSeconds);
            IssuesFactionCommands = issuesFactionCommands;
        }

        public SpacePirateKind Kind { get; }

        public IntruderDefinition IntruderDefinition { get; }

        public SpacePirateBehaviorKind BehaviorKind { get; }

        public SpacePirateBoardingCraftProfile BoardingCraft { get; }

        public int PrimaryDamage { get; }

        public float PrimaryMinimumRange { get; }

        public float PrimaryMaximumRange { get; }

        public float PrimaryDelaySeconds { get; }

        public int SecondaryDamage { get; }

        public float SecondaryMinimumRange { get; }

        public float SecondaryMaximumRange { get; }

        public float SecondaryDelaySeconds { get; }

        public int ShieldDurability { get; }

        public float ShieldBashRadiusMeters { get; }

        public float ShieldBashWindupSeconds { get; }

        public float DefensiveStanceDelaySeconds { get; }

        public float MaximumFireDurationSeconds { get; }

        public float ReloadWaitSeconds { get; }

        public float CommandRadiusMeters { get; }

        public float CommandedMovementSpeed { get; }

        public float SabotageCastSeconds { get; }

        public float SabotageRecoverySeconds { get; }

        public float BombInstallSeconds { get; }

        public bool IssuesFactionCommands { get; }
    }

    public readonly struct SpacePirateState
    {
        private SpacePirateState(
            SpacePirateKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder,
            float elapsedSeconds,
            SpacePirateFormationKind formationKind,
            SpacePirateSabotageKind sabotageKind,
            float sabotageProgressSeconds,
            bool sabotageApplied,
            bool commanderAlive,
            bool subordinateStopped)
        {
            Kind = kind;
            Definition = definition;
            Attempt = attempt;
            Intruder = intruder;
            ElapsedSeconds = Math.Max(0f, elapsedSeconds);
            FormationKind = formationKind;
            SabotageKind = sabotageKind;
            SabotageProgressSeconds = Math.Max(0f, sabotageProgressSeconds);
            SabotageApplied = sabotageApplied;
            CommanderAlive = commanderAlive;
            SubordinateStopped = subordinateStopped;
        }

        public SpacePirateKind Kind { get; }

        public IntruderDefinition Definition { get; }

        public IntrusionAttemptState Attempt { get; }

        public IntruderEntityState Intruder { get; }

        public float ElapsedSeconds { get; }

        public SpacePirateFormationKind FormationKind { get; }

        public SpacePirateSabotageKind SabotageKind { get; }

        public float SabotageProgressSeconds { get; }

        public bool SabotageApplied { get; }

        public bool CommanderAlive { get; }

        public bool SubordinateStopped { get; }

        public bool IsActive => Kind != SpacePirateKind.None && Intruder.IsActive;

        public bool IsResolved => Kind != SpacePirateKind.None && Intruder.IsResolved;

        public static SpacePirateState None => new SpacePirateState(
            SpacePirateKind.None,
            default,
            IntrusionAttemptState.None,
            IntruderEntityState.None,
            0f,
            SpacePirateFormationKind.None,
            SpacePirateSabotageKind.None,
            0f,
            false,
            false,
            false);

        public static SpacePirateState Start(
            SpacePirateKind kind,
            IntruderDefinition definition,
            IntrusionAttemptState attempt,
            IntruderEntityState intruder)
        {
            if (kind == SpacePirateKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Space pirate state requires a concrete kind.");
            }

            if (!intruder.IsActive)
            {
                throw new ArgumentException("Space pirate intruder entity must be active.", nameof(intruder));
            }

            return new SpacePirateState(
                kind,
                definition,
                attempt,
                intruder,
                0f,
                SpacePirateFormationKind.None,
                SpacePirateSabotageKind.None,
                0f,
                false,
                true,
                false);
        }

        public SpacePirateState WithIntruder(IntruderEntityState intruder)
        {
            return new SpacePirateState(
                Kind,
                Definition,
                Attempt,
                intruder,
                ElapsedSeconds,
                FormationKind,
                SabotageKind,
                SabotageProgressSeconds,
                SabotageApplied,
                CommanderAlive,
                SubordinateStopped);
        }

        public SpacePirateState WithElapsed(float elapsedSeconds)
        {
            return new SpacePirateState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                elapsedSeconds,
                FormationKind,
                SabotageKind,
                SabotageProgressSeconds,
                SabotageApplied,
                CommanderAlive,
                SubordinateStopped);
        }

        public SpacePirateState WithFormation(SpacePirateFormationKind formationKind)
        {
            return new SpacePirateState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                formationKind,
                SabotageKind,
                SabotageProgressSeconds,
                SabotageApplied,
                CommanderAlive,
                SubordinateStopped);
        }

        public SpacePirateState WithSabotage(
            SpacePirateSabotageKind sabotageKind,
            float progressSeconds,
            bool sabotageApplied)
        {
            return new SpacePirateState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                FormationKind,
                sabotageKind,
                progressSeconds,
                sabotageApplied,
                CommanderAlive,
                SubordinateStopped);
        }

        public SpacePirateState WithCommanderAlive(bool commanderAlive)
        {
            return new SpacePirateState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                FormationKind,
                SabotageKind,
                SabotageProgressSeconds,
                SabotageApplied,
                commanderAlive,
                SubordinateStopped);
        }

        public SpacePirateState WithSubordinateStopped(bool stopped)
        {
            return new SpacePirateState(
                Kind,
                Definition,
                Attempt,
                Intruder,
                ElapsedSeconds,
                FormationKind,
                SabotageKind,
                SabotageProgressSeconds,
                SabotageApplied,
                CommanderAlive,
                stopped);
        }
    }

    public readonly struct SpacePirateTickResult
    {
        public SpacePirateTickResult(
            SpacePirateState state,
            ShipState ship,
            SpacePirateActionKind actionKind,
            int roomDamageApplied,
            int playerDamageApplied,
            int shieldDamageApplied = 0,
            bool defensiveStanceActive = false,
            bool commandIssued = false,
            bool subordinatesStopped = false)
        {
            State = state;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            ActionKind = actionKind;
            RoomDamageApplied = Math.Max(0, roomDamageApplied);
            PlayerDamageApplied = Math.Max(0, playerDamageApplied);
            ShieldDamageApplied = Math.Max(0, shieldDamageApplied);
            DefensiveStanceActive = defensiveStanceActive;
            CommandIssued = commandIssued;
            SubordinatesStopped = subordinatesStopped;
        }

        public SpacePirateState State { get; }

        public ShipState Ship { get; }

        public SpacePirateActionKind ActionKind { get; }

        public int RoomDamageApplied { get; }

        public int PlayerDamageApplied { get; }

        public int ShieldDamageApplied { get; }

        public bool DefensiveStanceActive { get; }

        public bool CommandIssued { get; }

        public bool SubordinatesStopped { get; }
    }

    public readonly struct SpacePirateCommandResult
    {
        public SpacePirateCommandResult(
            SpacePirateFormationKind formationKind,
            float commandRadiusMeters,
            float subordinateMovementSpeed,
            bool placesIstanteNearAta,
            bool placesKurusInFront,
            bool placesPahurBehindShield)
        {
            FormationKind = formationKind;
            CommandRadiusMeters = Math.Max(0f, commandRadiusMeters);
            SubordinateMovementSpeed = Math.Max(0f, subordinateMovementSpeed);
            PlacesIstanteNearAta = placesIstanteNearAta;
            PlacesKurusInFront = placesKurusInFront;
            PlacesPahurBehindShield = placesPahurBehindShield;
        }

        public SpacePirateFormationKind FormationKind { get; }

        public float CommandRadiusMeters { get; }

        public float SubordinateMovementSpeed { get; }

        public bool PlacesIstanteNearAta { get; }

        public bool PlacesKurusInFront { get; }

        public bool PlacesPahurBehindShield { get; }
    }

    public readonly struct SpacePirateSabotageResult
    {
        public SpacePirateSabotageResult(
            SpacePirateState state,
            ShipState ship,
            SpacePirateSabotageKind sabotageKind,
            bool sabotageApplied,
            int roomDamageApplied,
            bool boosterDisabled,
            bool autoPilotDisabled,
            bool turretDisabled,
            int blackoutRoomCount,
            int closedCorridorCount,
            float recoveryInteractionSeconds)
        {
            State = state;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            SabotageKind = sabotageKind;
            SabotageApplied = sabotageApplied;
            RoomDamageApplied = Math.Max(0, roomDamageApplied);
            BoosterDisabled = boosterDisabled;
            AutoPilotDisabled = autoPilotDisabled;
            TurretDisabled = turretDisabled;
            BlackoutRoomCount = Math.Max(0, blackoutRoomCount);
            ClosedCorridorCount = Math.Max(0, closedCorridorCount);
            RecoveryInteractionSeconds = Math.Max(0f, recoveryInteractionSeconds);
        }

        public SpacePirateState State { get; }

        public ShipState Ship { get; }

        public SpacePirateSabotageKind SabotageKind { get; }

        public bool SabotageApplied { get; }

        public int RoomDamageApplied { get; }

        public bool BoosterDisabled { get; }

        public bool AutoPilotDisabled { get; }

        public bool TurretDisabled { get; }

        public int BlackoutRoomCount { get; }

        public int ClosedCorridorCount { get; }

        public float RecoveryInteractionSeconds { get; }
    }

    public static class SpacePirateRules
    {
        public const string PahurDefinitionId = "space-pirate-pahur";
        public const string KurusDefinitionId = "space-pirate-kurus";
        public const string IstanteDefinitionId = "space-pirate-istante";
        public const string AtaDefinitionId = "space-pirate-ata";

        public const int PahurHealth = 150;
        public const float PahurMovementSpeed = 2f;
        public const int PahurRocketDamagePerHalfSecond = 5;
        public const float PahurRocketDamageIntervalSeconds = 0.5f;
        public const float PahurRocketMinimumRange = 1f;
        public const float PahurRocketMaximumRange = 2f;
        public const float PahurMaximumFireDurationSeconds = 10f;
        public const float PahurReloadWaitSeconds = 2.5f;

        public const int KurusHealth = 130;
        public const float KurusMovementSpeed = 1.5f;
        public const int KurusShieldDurability = 100;
        public const int KurusShieldBashDamage = 10;
        public const float KurusShieldBashRadiusMeters = 5f;
        public const float KurusShieldBashWindupSeconds = 2f;
        public const float KurusShieldBashDelaySeconds = 1.5f;
        public const float KurusDefensiveStanceDelaySeconds = 5f;

        public const int IstanteHealth = 200;
        public const int IstanteMusketDamage = 60;
        public const float IstanteMusketMinimumRange = 3f;
        public const float IstanteMusketMaximumRange = 5f;
        public const float IstanteMusketAttackDelaySeconds = 3.5f;
        public const int IstanteDaggerDamage = 40;
        public const float IstanteDaggerMinimumRange = 1f;
        public const float IstanteDaggerMaximumRange = 2.5f;
        public const float IstanteDaggerAttackDelaySeconds = 1.5f;

        public const int AtaHealth = 120;
        public const int AtaPistolDamage = 8;
        public const float AtaPistolMinimumRange = 2f;
        public const float AtaPistolMaximumRange = 3f;
        public const float AtaPistolAttackDelaySeconds = 1.5f;
        public const float AtaCommandRadiusMeters = 3f;
        public const float AtaCommandedMovementSpeed = 1.5f;
        public const float AtaSabotageCastSeconds = 35f;
        public const float AtaArmorySabotageCastSeconds = 30f;
        public const float AtaSabotageRecoverySeconds = 10f;
        public const float AtaBombInstallSeconds = 25f;
        public const int AtaControlHackClosedCorridorCount = 7;
        public const int AtaEngineBlackoutRoomCount = 5;

        public const int PahurBoardingCraftHealth = 350;
        public const int KurusBoardingCraftHealth = 600;
        public const int IstanteBoardingCraftHealth = 900;
        public const int AtaBoardingCraftHealth = 1200;

        private static readonly SpacePirateKind[] SourceKindOrder =
        {
            SpacePirateKind.Pahur,
            SpacePirateKind.Kurus,
            SpacePirateKind.Istante,
            SpacePirateKind.Ata
        };

        public static SpacePirateProfile[] CreateAllSourceSpacePirateProfiles()
        {
            var profiles = new SpacePirateProfile[SourceKindOrder.Length];
            for (var i = 0; i < SourceKindOrder.Length; i++)
            {
                profiles[i] = GetProfile(SourceKindOrder[i]);
            }

            return profiles;
        }

        public static SpacePirateProfile GetProfile(SpacePirateKind kind)
        {
            switch (kind)
            {
                case SpacePirateKind.Pahur:
                    return new SpacePirateProfile(
                        kind,
                        CreateDefinition(
                            PahurDefinitionId,
                            "Pahur",
                            IntruderObjectiveType.DestroyShip,
                            PahurHealth,
                            PahurMovementSpeed,
                            PahurRocketMaximumRange,
                            PahurRocketDamageIntervalSeconds,
                            CreateAreaAttackTargetPriorities()),
                        SpacePirateBehaviorKind.RocketTrooper,
                        GetBoardingCraftProfile(kind),
                        PahurRocketDamagePerHalfSecond,
                        PahurRocketMinimumRange,
                        PahurRocketMaximumRange,
                        PahurRocketDamageIntervalSeconds,
                        maximumFireDurationSeconds: PahurMaximumFireDurationSeconds,
                        reloadWaitSeconds: PahurReloadWaitSeconds);
                case SpacePirateKind.Kurus:
                    return new SpacePirateProfile(
                        kind,
                        CreateDefinition(
                            KurusDefinitionId,
                            "Kurus",
                            IntruderObjectiveType.DestroyShip,
                            KurusHealth,
                            KurusMovementSpeed,
                            KurusShieldBashRadiusMeters,
                            KurusShieldBashDelaySeconds,
                            CreateAreaAttackTargetPriorities()),
                        SpacePirateBehaviorKind.ShieldBearer,
                        GetBoardingCraftProfile(kind),
                        KurusShieldBashDamage,
                        0f,
                        KurusShieldBashRadiusMeters,
                        KurusShieldBashDelaySeconds,
                        shieldDurability: KurusShieldDurability,
                        shieldBashRadiusMeters: KurusShieldBashRadiusMeters,
                        shieldBashWindupSeconds: KurusShieldBashWindupSeconds,
                        defensiveStanceDelaySeconds: KurusDefensiveStanceDelaySeconds);
                case SpacePirateKind.Istante:
                    return new SpacePirateProfile(
                        kind,
                        CreateDefinition(
                            IstanteDefinitionId,
                            "Istante",
                            IntruderObjectiveType.AttackPlayer,
                            IstanteHealth,
                            AtaCommandedMovementSpeed,
                            IstanteMusketMaximumRange,
                            IstanteMusketAttackDelaySeconds,
                            CreatePlayerTargetPriorities()),
                        SpacePirateBehaviorKind.EliteSoldier,
                        GetBoardingCraftProfile(kind),
                        IstanteMusketDamage,
                        IstanteMusketMinimumRange,
                        IstanteMusketMaximumRange,
                        IstanteMusketAttackDelaySeconds,
                        secondaryDamage: IstanteDaggerDamage,
                        secondaryMinimumRange: IstanteDaggerMinimumRange,
                        secondaryMaximumRange: IstanteDaggerMaximumRange,
                        secondaryDelaySeconds: IstanteDaggerAttackDelaySeconds);
                case SpacePirateKind.Ata:
                    return new SpacePirateProfile(
                        kind,
                        CreateDefinition(
                            AtaDefinitionId,
                            "Ata",
                            IntruderObjectiveType.OccupyRoom,
                            AtaHealth,
                            AtaCommandedMovementSpeed,
                            AtaPistolMaximumRange,
                            AtaPistolAttackDelaySeconds,
                            CreateSabotageTargetPriorities(),
                            true),
                        SpacePirateBehaviorKind.Commander,
                        GetBoardingCraftProfile(kind),
                        AtaPistolDamage,
                        AtaPistolMinimumRange,
                        AtaPistolMaximumRange,
                        AtaPistolAttackDelaySeconds,
                        commandRadiusMeters: AtaCommandRadiusMeters,
                        commandedMovementSpeed: AtaCommandedMovementSpeed,
                        sabotageCastSeconds: AtaSabotageCastSeconds,
                        sabotageRecoverySeconds: AtaSabotageRecoverySeconds,
                        bombInstallSeconds: AtaBombInstallSeconds,
                        issuesFactionCommands: true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported space pirate kind.");
            }
        }

        public static SpacePirateBoardingCraftProfile GetBoardingCraftProfile(SpacePirateKind kind)
        {
            switch (kind)
            {
                case SpacePirateKind.Pahur:
                    return new SpacePirateBoardingCraftProfile(
                        kind,
                        PahurBoardingCraftHealth,
                        4f,
                        8f,
                        4f,
                        4,
                        "Pahur mark");
                case SpacePirateKind.Kurus:
                    return new SpacePirateBoardingCraftProfile(
                        kind,
                        KurusBoardingCraftHealth,
                        5f,
                        10f,
                        7f,
                        4,
                        "shield mark");
                case SpacePirateKind.Istante:
                    return new SpacePirateBoardingCraftProfile(
                        kind,
                        IstanteBoardingCraftHealth,
                        5f,
                        10f,
                        10f,
                        1,
                        "elite mark");
                case SpacePirateKind.Ata:
                    return new SpacePirateBoardingCraftProfile(
                        kind,
                        AtaBoardingCraftHealth,
                        7f,
                        12f,
                        15f,
                        1,
                        "commander mark");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported space pirate craft kind.");
            }
        }

        public static SpacePirateKind SelectKindForSeed(int seed)
        {
            var index = (seed & 0x7fffffff) % SourceKindOrder.Length;
            return SourceKindOrder[index];
        }

        public static SpacePirateState CreateSpacePirateIntrusionFromHazard(
            TransportHazardState hazard,
            int boardingIndex,
            ShipRoomId playerRoom = ShipRoomId.Cockpit)
        {
            if (hazard.HazardType != TransportHazardType.SpacePirateRegion)
            {
                throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Space pirate intrusion requires a space pirate region hazard.");
            }

            if (boardingIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boardingIndex), "Space pirate boarding index must be positive.");
            }

            var seed = CreateBoardingSeed(hazard.Seed, boardingIndex);
            return CreateSpacePirateIntrusionForSeed(
                SelectKindForSeed(seed),
                seed,
                playerRoom,
                "space-pirate-" + hazard.Seed + "-" + boardingIndex);
        }

        public static SpacePirateState CreateSpacePirateIntrusionForSeed(
            SpacePirateKind kind,
            int seed,
            ShipRoomId playerRoom,
            string attemptId = "space-pirate-validation")
        {
            if (kind == SpacePirateKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Space pirate intrusion requires a concrete kind.");
            }

            var profile = GetProfile(kind);
            var attempt = IntruderRules.CreateAttempt(attemptId, profile.IntruderDefinition, seed, playerRoom);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, profile.IntruderDefinition);
            return SpacePirateState.Start(kind, profile.IntruderDefinition, boarded, intruder);
        }

        public static ExternalTargetState CreateBoardingCraftExternalTarget(TransportHazardState hazard)
        {
            if (hazard.HazardType != TransportHazardType.SpacePirateRegion)
            {
                throw new ArgumentOutOfRangeException(nameof(hazard), hazard.HazardType, "Space pirate external target requires a space pirate region hazard.");
            }

            var kind = SelectKindForSeed(hazard.Seed);
            var craft = GetBoardingCraftProfile(kind);
            return new ExternalTargetState(
                "space-pirate-" + FormatSpacePirateKind(kind).ToLowerInvariant() + "-" + hazard.Seed,
                ExternalTargetType.SpacePirateBoardingCraft,
                craft.Health,
                craft.Health,
                CreateTargetCoordinate(hazard.Seed, 131, 0.58f),
                CreateTargetCoordinate(hazard.Seed, 167, 0.42f),
                ManualTurretState.DefaultAsteroidHitRadius);
        }

        public static SpacePirateTickResult TickSpacePirate(
            SpacePirateState state,
            ShipState ship,
            float deltaSeconds,
            bool encounteredTarget = false,
            bool closeTarget = false,
            SpacePirateFormationKind formationKind = SpacePirateFormationKind.None,
            bool commanderAlive = true)
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
                return new SpacePirateTickResult(state, ship, SpacePirateActionKind.None, 0, 0);
            }

            var profile = GetProfile(state.Kind);
            if (state.Kind != SpacePirateKind.Ata && !commanderAlive)
            {
                var stopped = state.WithCommanderAlive(false).WithSubordinateStopped(true);
                return new SpacePirateTickResult(
                    stopped,
                    ship,
                    SpacePirateActionKind.SubordinatesStopped,
                    0,
                    0,
                    subordinatesStopped: true);
            }

            var tickedIntruder = IntruderRules.TickStatusEffects(state.Intruder, deltaSeconds);
            if (!tickedIntruder.IsActive)
            {
                return new SpacePirateTickResult(
                    state.WithIntruder(tickedIntruder),
                    ship,
                    SpacePirateActionKind.None,
                    0,
                    0);
            }

            var nextState = state
                .WithIntruder(tickedIntruder)
                .WithElapsed(state.ElapsedSeconds + deltaSeconds)
                .WithCommanderAlive(commanderAlive);
            if (formationKind != SpacePirateFormationKind.None)
            {
                nextState = nextState.WithFormation(formationKind);
            }

            switch (profile.BehaviorKind)
            {
                case SpacePirateBehaviorKind.RocketTrooper:
                    return ApplyPahurRocketAttack(nextState, profile, ship, deltaSeconds);
                case SpacePirateBehaviorKind.ShieldBearer:
                    return ApplyKurusAction(nextState, profile, ship, encounteredTarget);
                case SpacePirateBehaviorKind.EliteSoldier:
                    return ApplyIstanteAttack(nextState, profile, ship, closeTarget);
                case SpacePirateBehaviorKind.Commander:
                    return new SpacePirateTickResult(
                        nextState,
                        ship,
                        SpacePirateActionKind.CommandIssued,
                        0,
                        0,
                        commandIssued: true);
                default:
                    return new SpacePirateTickResult(nextState, ship, SpacePirateActionKind.None, 0, 0);
            }
        }

        public static SpacePirateCommandResult IssueAtaCommand(SpacePirateFormationKind formationKind)
        {
            if (formationKind == SpacePirateFormationKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(formationKind), "Ata command requires a concrete formation.");
            }

            return new SpacePirateCommandResult(
                formationKind,
                AtaCommandRadiusMeters,
                AtaCommandedMovementSpeed,
                placesIstanteNearAta: formationKind == SpacePirateFormationKind.Protective,
                placesKurusInFront: true,
                placesPahurBehindShield: true);
        }

        public static SpacePirateSabotageResult TickAtaSabotage(
            SpacePirateState ata,
            ShipState ship,
            SpacePirateSabotageKind sabotageKind,
            float deltaSeconds)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (ata.Kind != SpacePirateKind.Ata)
            {
                throw new ArgumentOutOfRangeException(nameof(ata), ata.Kind, "Only Ata can apply pirate sabotage.");
            }

            if (sabotageKind == SpacePirateSabotageKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(sabotageKind), "Ata sabotage requires a concrete kind.");
            }

            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!ata.IsActive || deltaSeconds <= 0f)
            {
                return new SpacePirateSabotageResult(
                    ata,
                    ship,
                    sabotageKind,
                    false,
                    0,
                    false,
                    false,
                    false,
                    0,
                    0,
                    AtaSabotageRecoverySeconds);
            }

            var requiredSeconds = GetSabotageCastSeconds(sabotageKind);
            var progress = ata.SabotageKind == sabotageKind
                ? ata.SabotageProgressSeconds + deltaSeconds
                : deltaSeconds;
            var progressed = ata.WithSabotage(sabotageKind, progress, false);
            if (progress + 0.0001f < requiredSeconds)
            {
                return new SpacePirateSabotageResult(
                    progressed,
                    ship,
                    sabotageKind,
                    false,
                    0,
                    false,
                    false,
                    false,
                    0,
                    0,
                    AtaSabotageRecoverySeconds);
            }

            return ApplyAtaSabotage(progressed, ship, sabotageKind);
        }

        public static SpacePirateState ApplyDamage(SpacePirateState state, int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Space pirate damage cannot be negative.");
            }

            if (!state.IsActive || damage == 0)
            {
                return state;
            }

            return state.WithIntruder(state.Intruder.WithDamage(damage));
        }

        public static string FormatSpacePirateKind(SpacePirateKind kind)
        {
            switch (kind)
            {
                case SpacePirateKind.Pahur:
                    return "Pahur";
                case SpacePirateKind.Kurus:
                    return "Kurus";
                case SpacePirateKind.Istante:
                    return "Istante";
                case SpacePirateKind.Ata:
                    return "Ata";
                default:
                    return "None";
            }
        }

        private static SpacePirateTickResult ApplyPahurRocketAttack(
            SpacePirateState state,
            SpacePirateProfile profile,
            ShipState ship,
            float deltaSeconds)
        {
            var damage = CalculatePahurRocketDamage(deltaSeconds);
            var room = ship.GetRoom(state.Intruder.TargetRoom);
            return new SpacePirateTickResult(
                state,
                ship.WithRoom(state.Intruder.TargetRoom, room.WithDamage(damage)),
                SpacePirateActionKind.RocketAreaAttack,
                damage,
                0);
        }

        private static SpacePirateTickResult ApplyKurusAction(
            SpacePirateState state,
            SpacePirateProfile profile,
            ShipState ship,
            bool encounteredTarget)
        {
            if (!encounteredTarget)
            {
                return new SpacePirateTickResult(
                    state,
                    ship,
                    SpacePirateActionKind.ShieldGuard,
                    0,
                    0,
                    defensiveStanceActive: true);
            }

            return new SpacePirateTickResult(
                state,
                ship,
                SpacePirateActionKind.ShieldBash,
                0,
                profile.PrimaryDamage,
                shieldDamageApplied: profile.PrimaryDamage);
        }

        private static SpacePirateTickResult ApplyIstanteAttack(
            SpacePirateState state,
            SpacePirateProfile profile,
            ShipState ship,
            bool closeTarget)
        {
            if (closeTarget)
            {
                return new SpacePirateTickResult(
                    state,
                    ship,
                    SpacePirateActionKind.DaggerSlash,
                    0,
                    profile.SecondaryDamage);
            }

            return new SpacePirateTickResult(
                state,
                ship,
                SpacePirateActionKind.MusketShot,
                0,
                profile.PrimaryDamage);
        }

        private static SpacePirateSabotageResult ApplyAtaSabotage(
            SpacePirateState state,
            ShipState ship,
            SpacePirateSabotageKind sabotageKind)
        {
            var nextState = state.WithSabotage(sabotageKind, GetSabotageCastSeconds(sabotageKind), true);
            switch (sabotageKind)
            {
                case SpacePirateSabotageKind.EngineOutputReduction:
                    return new SpacePirateSabotageResult(
                        nextState,
                        ship,
                        sabotageKind,
                        true,
                        0,
                        boosterDisabled: true,
                        autoPilotDisabled: false,
                        turretDisabled: false,
                        blackoutRoomCount: AtaEngineBlackoutRoomCount,
                        closedCorridorCount: 0,
                        recoveryInteractionSeconds: AtaSabotageRecoverySeconds);
                case SpacePirateSabotageKind.ControlRoomHack:
                    return new SpacePirateSabotageResult(
                        nextState,
                        ship.WithRoom(
                            ShipRoomId.ControlRoom,
                            ship.GetRoom(ShipRoomId.ControlRoom).WithFunctionOffline(true)),
                        sabotageKind,
                        true,
                        0,
                        boosterDisabled: false,
                        autoPilotDisabled: false,
                        turretDisabled: false,
                        blackoutRoomCount: 0,
                        closedCorridorCount: AtaControlHackClosedCorridorCount,
                        recoveryInteractionSeconds: AtaSabotageRecoverySeconds);
                case SpacePirateSabotageKind.AutoPilotDisable:
                    return new SpacePirateSabotageResult(
                        nextState,
                        ship.WithRoom(
                            ShipRoomId.Cockpit,
                            ship.GetRoom(ShipRoomId.Cockpit).WithFunctionOffline(true)),
                        sabotageKind,
                        true,
                        0,
                        boosterDisabled: false,
                        autoPilotDisabled: true,
                        turretDisabled: false,
                        blackoutRoomCount: 0,
                        closedCorridorCount: 0,
                        recoveryInteractionSeconds: AtaSabotageRecoverySeconds);
                case SpacePirateSabotageKind.ArmoryTurretDisable:
                    return new SpacePirateSabotageResult(
                        nextState,
                        ship.WithRoom(
                            ShipRoomId.Armory,
                            ship.GetRoom(ShipRoomId.Armory).WithFunctionOffline(true)),
                        sabotageKind,
                        true,
                        0,
                        boosterDisabled: false,
                        autoPilotDisabled: false,
                        turretDisabled: true,
                        blackoutRoomCount: 0,
                        closedCorridorCount: 0,
                        recoveryInteractionSeconds: AtaSabotageRecoverySeconds);
                case SpacePirateSabotageKind.SupplyRoomBomb:
                    return ApplyAtaBomb(nextState, ship, ShipRoomId.SupplyRoom, sabotageKind);
                case SpacePirateSabotageKind.CargoHoldBomb:
                    return ApplyAtaBomb(nextState, ship, ShipRoomId.CargoHold, sabotageKind);
                default:
                    throw new ArgumentOutOfRangeException(nameof(sabotageKind), sabotageKind, "Unsupported Ata sabotage kind.");
            }
        }

        private static SpacePirateSabotageResult ApplyAtaBomb(
            SpacePirateState state,
            ShipState ship,
            ShipRoomId roomId,
            SpacePirateSabotageKind sabotageKind)
        {
            var room = ship.GetRoom(roomId);
            var damage = room.CurrentDurability;
            return new SpacePirateSabotageResult(
                state,
                ship.WithRoom(roomId, room.WithDamage(damage)),
                sabotageKind,
                true,
                damage,
                boosterDisabled: false,
                autoPilotDisabled: false,
                turretDisabled: false,
                blackoutRoomCount: 0,
                closedCorridorCount: 0,
                recoveryInteractionSeconds: AtaSabotageRecoverySeconds);
        }

        private static int CalculatePahurRocketDamage(float deltaSeconds)
        {
            var clampedSeconds = Math.Min(deltaSeconds, PahurMaximumFireDurationSeconds);
            var tickCount = (int)Math.Floor((clampedSeconds + 0.0001f) / PahurRocketDamageIntervalSeconds);
            return Math.Max(1, tickCount) * PahurRocketDamagePerHalfSecond;
        }

        private static float GetSabotageCastSeconds(SpacePirateSabotageKind sabotageKind)
        {
            switch (sabotageKind)
            {
                case SpacePirateSabotageKind.SupplyRoomBomb:
                case SpacePirateSabotageKind.CargoHoldBomb:
                    return AtaBombInstallSeconds;
                case SpacePirateSabotageKind.EngineOutputReduction:
                case SpacePirateSabotageKind.ControlRoomHack:
                case SpacePirateSabotageKind.AutoPilotDisable:
                    return AtaSabotageCastSeconds;
                case SpacePirateSabotageKind.ArmoryTurretDisable:
                    return AtaArmorySabotageCastSeconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sabotageKind), sabotageKind, "Unsupported Ata sabotage kind.");
            }
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
            bool issuesFactionCommands = false)
        {
            return new IntruderDefinition(
                definitionId,
                displayName,
                IntruderFaction.SpacePirate,
                objective,
                maxHealth,
                movementSpeed,
                attackRange,
                attackDelaySeconds,
                targetPriorities,
                IntruderMobilityKind.Walking,
                issuesFactionCommands);
        }

        private static IntruderTargetPriority[] CreateAreaAttackTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.CargoHold, 0),
                new IntruderTargetPriority(IntruderTargetType.Player, ShipRoomId.Cockpit, 1)
            };
        }

        private static IntruderTargetPriority[] CreatePlayerTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Player, ShipRoomId.Cockpit, 0),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.CargoHold, 1)
            };
        }

        private static IntruderTargetPriority[] CreateSabotageTargetPriorities()
        {
            return new[]
            {
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.EngineRoom, 0),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.ControlRoom, 1),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.Cockpit, 2),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.Armory, 3),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.SupplyRoom, 4),
                new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.CargoHold, 5)
            };
        }

        private static int CreateBoardingSeed(int hazardSeed, int boardingIndex)
        {
            unchecked
            {
                var hash = 23;
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
