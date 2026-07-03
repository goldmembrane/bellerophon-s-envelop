using System;

namespace Bellerophon.Core.Session
{
    public enum SpecialContractKind
    {
        None,
        PresenceDetectorUnlock,
        LightBladeUnlock,
        ElectricMineUnlock,
        CorridorPurifierUnlock
    }

    public enum SpecialContractEnemyKind
    {
        None,
        Resistance,
        Revolution,
        Istante,
        Ata,
        Monstrum,
        Dolore
    }

    public enum SpecialContractObjectiveItemKind
    {
        None,
        ResistanceChip,
        RevolutionChip,
        IstantePowerCore,
        AtaControlModule
    }

    public readonly struct SpecialEquipmentUnlockState
    {
        public SpecialEquipmentUnlockState(
            bool presenceDetectorUnlocked,
            bool lightBladeUnlocked,
            bool electricMineUnlocked,
            bool corridorPurifierUnlocked)
        {
            PresenceDetectorUnlocked = presenceDetectorUnlocked;
            LightBladeUnlocked = lightBladeUnlocked;
            ElectricMineUnlocked = electricMineUnlocked;
            CorridorPurifierUnlocked = corridorPurifierUnlocked;
        }

        public bool PresenceDetectorUnlocked { get; }

        public bool LightBladeUnlocked { get; }

        public bool ElectricMineUnlocked { get; }

        public bool CorridorPurifierUnlocked { get; }

        public static SpecialEquipmentUnlockState None => new SpecialEquipmentUnlockState(false, false, false, false);

        public bool IsUnlocked(EquipmentItemKind itemKind)
        {
            switch (itemKind)
            {
                case EquipmentItemKind.PresenceDetector:
                    return PresenceDetectorUnlocked;
                case EquipmentItemKind.LightBlade:
                    return LightBladeUnlocked;
                case EquipmentItemKind.ElectricMine:
                    return ElectricMineUnlocked;
                case EquipmentItemKind.CorridorPurifier:
                    return CorridorPurifierUnlocked;
                default:
                    return false;
            }
        }

        public SpecialEquipmentUnlockState WithUnlocked(EquipmentItemKind itemKind)
        {
            switch (itemKind)
            {
                case EquipmentItemKind.PresenceDetector:
                    return new SpecialEquipmentUnlockState(true, LightBladeUnlocked, ElectricMineUnlocked, CorridorPurifierUnlocked);
                case EquipmentItemKind.LightBlade:
                    return new SpecialEquipmentUnlockState(PresenceDetectorUnlocked, true, ElectricMineUnlocked, CorridorPurifierUnlocked);
                case EquipmentItemKind.ElectricMine:
                    return new SpecialEquipmentUnlockState(PresenceDetectorUnlocked, LightBladeUnlocked, true, CorridorPurifierUnlocked);
                case EquipmentItemKind.CorridorPurifier:
                    return new SpecialEquipmentUnlockState(PresenceDetectorUnlocked, LightBladeUnlocked, ElectricMineUnlocked, true);
                default:
                    return this;
            }
        }
    }

    public readonly struct SpecialContractProgressState
    {
        public SpecialContractProgressState(
            SpecialContractKind activeContractKind,
            SpecialEquipmentUnlockState equipmentUnlocks,
            int organicRichPlanetVisits,
            int volcanicActivePlanetVisits,
            int commonMineralRichPlanetVisits,
            int rareMineralRichPlanetVisits,
            int veryHardContractCompletions,
            int resistanceNeutralizedCount,
            int revolutionNeutralizedCount,
            int istanteNeutralizedCount,
            int ataNeutralizedCount,
            int monstrumNeutralizedCount,
            int doloreNeutralizedCount,
            int resistanceChipCount,
            int revolutionChipCount,
            int istantePowerCoreCount,
            int ataControlModuleCount,
            bool corridorPurifierInstalled,
            int corridorPurifierChargeCount)
        {
            ActiveContractKind = activeContractKind;
            EquipmentUnlocks = equipmentUnlocks;
            OrganicRichPlanetVisits = Math.Max(0, organicRichPlanetVisits);
            VolcanicActivePlanetVisits = Math.Max(0, volcanicActivePlanetVisits);
            CommonMineralRichPlanetVisits = Math.Max(0, commonMineralRichPlanetVisits);
            RareMineralRichPlanetVisits = Math.Max(0, rareMineralRichPlanetVisits);
            VeryHardContractCompletions = Math.Max(0, veryHardContractCompletions);
            ResistanceNeutralizedCount = Math.Max(0, resistanceNeutralizedCount);
            RevolutionNeutralizedCount = Math.Max(0, revolutionNeutralizedCount);
            IstanteNeutralizedCount = Math.Max(0, istanteNeutralizedCount);
            AtaNeutralizedCount = Math.Max(0, ataNeutralizedCount);
            MonstrumNeutralizedCount = Math.Max(0, monstrumNeutralizedCount);
            DoloreNeutralizedCount = Math.Max(0, doloreNeutralizedCount);
            ResistanceChipCount = Math.Max(0, resistanceChipCount);
            RevolutionChipCount = Math.Max(0, revolutionChipCount);
            IstantePowerCoreCount = Math.Max(0, istantePowerCoreCount);
            AtaControlModuleCount = Math.Max(0, ataControlModuleCount);
            CorridorPurifierInstalled = corridorPurifierInstalled;
            CorridorPurifierChargeCount = Math.Max(0, corridorPurifierChargeCount);
        }

        public SpecialContractKind ActiveContractKind { get; }

        public SpecialEquipmentUnlockState EquipmentUnlocks { get; }

        public int OrganicRichPlanetVisits { get; }

        public int VolcanicActivePlanetVisits { get; }

        public int CommonMineralRichPlanetVisits { get; }

        public int RareMineralRichPlanetVisits { get; }

        public int VeryHardContractCompletions { get; }

        public int ResistanceNeutralizedCount { get; }

        public int RevolutionNeutralizedCount { get; }

        public int IstanteNeutralizedCount { get; }

        public int AtaNeutralizedCount { get; }

        public int MonstrumNeutralizedCount { get; }

        public int DoloreNeutralizedCount { get; }

        public int ResistanceChipCount { get; }

        public int RevolutionChipCount { get; }

        public int IstantePowerCoreCount { get; }

        public int AtaControlModuleCount { get; }

        public bool CorridorPurifierInstalled { get; }

        public int CorridorPurifierChargeCount { get; }

        public bool HasActiveContract => ActiveContractKind != SpecialContractKind.None;

        public static SpecialContractProgressState Empty => new SpecialContractProgressState(
            SpecialContractKind.None,
            SpecialEquipmentUnlockState.None,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            0);

        public SpecialContractProgressState WithActiveContract(SpecialContractKind kind)
        {
            return new SpecialContractProgressState(
                kind,
                EquipmentUnlocks,
                OrganicRichPlanetVisits,
                VolcanicActivePlanetVisits,
                CommonMineralRichPlanetVisits,
                RareMineralRichPlanetVisits,
                VeryHardContractCompletions,
                ResistanceNeutralizedCount,
                RevolutionNeutralizedCount,
                IstanteNeutralizedCount,
                AtaNeutralizedCount,
                MonstrumNeutralizedCount,
                DoloreNeutralizedCount,
                0,
                0,
                0,
                0,
                CorridorPurifierInstalled,
                CorridorPurifierChargeCount);
        }

        public SpecialContractProgressState WithEquipmentUnlocks(SpecialEquipmentUnlockState unlocks)
        {
            return new SpecialContractProgressState(
                ActiveContractKind,
                unlocks,
                OrganicRichPlanetVisits,
                VolcanicActivePlanetVisits,
                CommonMineralRichPlanetVisits,
                RareMineralRichPlanetVisits,
                VeryHardContractCompletions,
                ResistanceNeutralizedCount,
                RevolutionNeutralizedCount,
                IstanteNeutralizedCount,
                AtaNeutralizedCount,
                MonstrumNeutralizedCount,
                DoloreNeutralizedCount,
                ResistanceChipCount,
                RevolutionChipCount,
                IstantePowerCoreCount,
                AtaControlModuleCount,
                CorridorPurifierInstalled,
                CorridorPurifierChargeCount);
        }

        public SpecialContractProgressState WithProgressCounts(
            int organicRichPlanetVisits,
            int volcanicActivePlanetVisits,
            int commonMineralRichPlanetVisits,
            int rareMineralRichPlanetVisits,
            int veryHardContractCompletions,
            int resistanceNeutralizedCount,
            int revolutionNeutralizedCount,
            int istanteNeutralizedCount,
            int ataNeutralizedCount,
            int monstrumNeutralizedCount,
            int doloreNeutralizedCount)
        {
            return new SpecialContractProgressState(
                ActiveContractKind,
                EquipmentUnlocks,
                organicRichPlanetVisits,
                volcanicActivePlanetVisits,
                commonMineralRichPlanetVisits,
                rareMineralRichPlanetVisits,
                veryHardContractCompletions,
                resistanceNeutralizedCount,
                revolutionNeutralizedCount,
                istanteNeutralizedCount,
                ataNeutralizedCount,
                monstrumNeutralizedCount,
                doloreNeutralizedCount,
                ResistanceChipCount,
                RevolutionChipCount,
                IstantePowerCoreCount,
                AtaControlModuleCount,
                CorridorPurifierInstalled,
                CorridorPurifierChargeCount);
        }

        public SpecialContractProgressState WithObjectiveItems(
            int resistanceChipCount,
            int revolutionChipCount,
            int istantePowerCoreCount,
            int ataControlModuleCount)
        {
            return new SpecialContractProgressState(
                ActiveContractKind,
                EquipmentUnlocks,
                OrganicRichPlanetVisits,
                VolcanicActivePlanetVisits,
                CommonMineralRichPlanetVisits,
                RareMineralRichPlanetVisits,
                VeryHardContractCompletions,
                ResistanceNeutralizedCount,
                RevolutionNeutralizedCount,
                IstanteNeutralizedCount,
                AtaNeutralizedCount,
                MonstrumNeutralizedCount,
                DoloreNeutralizedCount,
                resistanceChipCount,
                revolutionChipCount,
                istantePowerCoreCount,
                ataControlModuleCount,
                CorridorPurifierInstalled,
                CorridorPurifierChargeCount);
        }

        public SpecialContractProgressState WithCorridorPurifier(bool installed, int chargeCount)
        {
            return new SpecialContractProgressState(
                ActiveContractKind,
                EquipmentUnlocks,
                OrganicRichPlanetVisits,
                VolcanicActivePlanetVisits,
                CommonMineralRichPlanetVisits,
                RareMineralRichPlanetVisits,
                VeryHardContractCompletions,
                ResistanceNeutralizedCount,
                RevolutionNeutralizedCount,
                IstanteNeutralizedCount,
                AtaNeutralizedCount,
                MonstrumNeutralizedCount,
                DoloreNeutralizedCount,
                ResistanceChipCount,
                RevolutionChipCount,
                IstantePowerCoreCount,
                AtaControlModuleCount,
                installed,
                chargeCount);
        }
    }

    public readonly struct SpecialContractDefinition
    {
        public SpecialContractDefinition(
            SpecialContractKind kind,
            string displayName,
            int requiredFame,
            int bonusCredits,
            EquipmentItemKind unlockItemKind,
            EquipmentItemKind grantedItemKind)
        {
            if (kind == SpecialContractKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Special contract definition requires a concrete kind.");
            }

            Kind = kind;
            DisplayName = displayName ?? string.Empty;
            RequiredFame = Math.Max(0, requiredFame);
            BonusCredits = Math.Max(0, bonusCredits);
            UnlockItemKind = unlockItemKind;
            GrantedItemKind = grantedItemKind;
        }

        public SpecialContractKind Kind { get; }

        public string DisplayName { get; }

        public int RequiredFame { get; }

        public int BonusCredits { get; }

        public EquipmentItemKind UnlockItemKind { get; }

        public EquipmentItemKind GrantedItemKind { get; }
    }

    public readonly struct SpecialContractAcceptanceResult
    {
        public SpecialContractAcceptanceResult(
            SpecialContractProgressState state,
            SpecialContractKind contractKind,
            bool accepted,
            string summary)
        {
            State = state;
            ContractKind = contractKind;
            Accepted = accepted;
            Summary = summary ?? string.Empty;
        }

        public SpecialContractProgressState State { get; }

        public SpecialContractKind ContractKind { get; }

        public bool Accepted { get; }

        public string Summary { get; }
    }

    public readonly struct SpecialContractEnemyRecordResult
    {
        public SpecialContractEnemyRecordResult(
            SpecialContractProgressState state,
            SpecialContractEnemyKind enemyKind,
            bool killCounted,
            bool objectiveItemDropped,
            SpecialContractObjectiveItemKind objectiveItemKind)
        {
            State = state;
            EnemyKind = enemyKind;
            KillCounted = killCounted;
            ObjectiveItemDropped = objectiveItemDropped;
            ObjectiveItemKind = objectiveItemKind;
        }

        public SpecialContractProgressState State { get; }

        public SpecialContractEnemyKind EnemyKind { get; }

        public bool KillCounted { get; }

        public bool ObjectiveItemDropped { get; }

        public SpecialContractObjectiveItemKind ObjectiveItemKind { get; }
    }

    public readonly struct SpecialContractRouteModifier
    {
        public SpecialContractRouteModifier(
            bool forcesAllIntrusionHazards,
            int intrusionOccurrenceMultiplier,
            int fixedDurationSeconds)
        {
            ForcesAllIntrusionHazards = forcesAllIntrusionHazards;
            IntrusionOccurrenceMultiplier = Math.Max(1, intrusionOccurrenceMultiplier);
            FixedDurationSeconds = Math.Max(0, fixedDurationSeconds);
        }

        public bool ForcesAllIntrusionHazards { get; }

        public int IntrusionOccurrenceMultiplier { get; }

        public int FixedDurationSeconds { get; }

        public static SpecialContractRouteModifier None => new SpecialContractRouteModifier(false, 1, 0);
    }

    public readonly struct SpecialContractSettlementResult
    {
        public SpecialContractSettlementResult(
            SpecialContractProgressState state,
            SpecialContractKind contractKind,
            bool completed,
            bool failed,
            int bonusCredits,
            EquipmentItemKind unlockedItemKind,
            EquipmentItemKind grantedItemKind,
            string summary)
        {
            State = state;
            ContractKind = contractKind;
            Completed = completed;
            Failed = failed;
            BonusCredits = Math.Max(0, bonusCredits);
            UnlockedItemKind = unlockedItemKind;
            GrantedItemKind = grantedItemKind;
            Summary = summary ?? string.Empty;
        }

        public SpecialContractProgressState State { get; }

        public SpecialContractKind ContractKind { get; }

        public bool Completed { get; }

        public bool Failed { get; }

        public int BonusCredits { get; }

        public EquipmentItemKind UnlockedItemKind { get; }

        public EquipmentItemKind GrantedItemKind { get; }

        public string Summary { get; }
    }

    public static class SpecialContractRules
    {
        public const int PresenceDetectorRequiredFame = 500;
        public const int PresenceDetectorRequiredOrganicVisits = 1;
        public const int PresenceDetectorRequiredResistanceChips = 2;
        public const int PresenceDetectorRequiredRevolutionChips = 1;
        public const int PresenceDetectorBonusCredits = 2000;

        public const int LightBladeRequiredFame = 1000;
        public const int LightBladeRequiredVolcanicVisits = 3;
        public const int LightBladeRequiredVeryHardCompletions = 2;
        public const int LightBladeRequiredIstantePowerCores = 1;
        public const int LightBladeRequiredAtaControlModules = 1;
        public const int LightBladeBonusCredits = 2500;

        public const int ElectricMineRequiredFame = 3000;
        public const int ElectricMineRequiredCommonMineralVisits = 1;
        public const int ElectricMineRequiredRareMineralVisits = 1;
        public const int ElectricMineRequiredRevolutionNeutralizations = 3;
        public const int ElectricMineSpecialCargoMinimumSizeUnits = PersonalCargoRules.FullCargoHoldCapacityUnits / 2;
        public const float ElectricMineSpecialCargoMinimumDurability = 0.55f;
        public const int ElectricMineBonusCredits = 3000;

        public const int CorridorPurifierRequiredFame = 5000;
        public const int CorridorPurifierRequiredEnemyNeutralizations = 1;
        public const int CorridorPurifierRouteDurationSeconds = 284;
        public const int CorridorPurifierIntrusionOccurrenceMultiplier = 3;
        public const int CorridorPurifierBonusCredits = 7500;
        public const int CorridorPurifierRewardChargeCount = 1;

        private static readonly SpecialContractDefinition[] Definitions =
        {
            new SpecialContractDefinition(
                SpecialContractKind.PresenceDetectorUnlock,
                "Presence Detector Unlock",
                PresenceDetectorRequiredFame,
                PresenceDetectorBonusCredits,
                EquipmentItemKind.PresenceDetector,
                EquipmentItemKind.None),
            new SpecialContractDefinition(
                SpecialContractKind.LightBladeUnlock,
                "Light Blade Unlock",
                LightBladeRequiredFame,
                LightBladeBonusCredits,
                EquipmentItemKind.LightBlade,
                EquipmentItemKind.LightBlade),
            new SpecialContractDefinition(
                SpecialContractKind.ElectricMineUnlock,
                "Electric Mine Unlock",
                ElectricMineRequiredFame,
                ElectricMineBonusCredits,
                EquipmentItemKind.ElectricMine,
                EquipmentItemKind.None),
            new SpecialContractDefinition(
                SpecialContractKind.CorridorPurifierUnlock,
                "Corridor Purifier Unlock",
                CorridorPurifierRequiredFame,
                CorridorPurifierBonusCredits,
                EquipmentItemKind.CorridorPurifier,
                EquipmentItemKind.CorridorPurifier)
        };

        public static SpecialContractDefinition[] CreateAllDefinitions()
        {
            var clone = new SpecialContractDefinition[Definitions.Length];
            Array.Copy(Definitions, clone, Definitions.Length);
            return clone;
        }

        public static SpecialContractDefinition GetDefinition(SpecialContractKind kind)
        {
            for (var i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].Kind == kind)
                {
                    return Definitions[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported special contract kind.");
        }

        public static SpecialContractProgressState RecordPlanetVisit(
            SpecialContractProgressState state,
            PlanetTrait trait)
        {
            switch (trait)
            {
                case PlanetTrait.OrganicRich:
                    return state.WithProgressCounts(
                        state.OrganicRichPlanetVisits + 1,
                        state.VolcanicActivePlanetVisits,
                        state.CommonMineralRichPlanetVisits,
                        state.RareMineralRichPlanetVisits,
                        state.VeryHardContractCompletions,
                        state.ResistanceNeutralizedCount,
                        state.RevolutionNeutralizedCount,
                        state.IstanteNeutralizedCount,
                        state.AtaNeutralizedCount,
                        state.MonstrumNeutralizedCount,
                        state.DoloreNeutralizedCount);
                case PlanetTrait.VolcanicActive:
                    return state.WithProgressCounts(
                        state.OrganicRichPlanetVisits,
                        state.VolcanicActivePlanetVisits + 1,
                        state.CommonMineralRichPlanetVisits,
                        state.RareMineralRichPlanetVisits,
                        state.VeryHardContractCompletions,
                        state.ResistanceNeutralizedCount,
                        state.RevolutionNeutralizedCount,
                        state.IstanteNeutralizedCount,
                        state.AtaNeutralizedCount,
                        state.MonstrumNeutralizedCount,
                        state.DoloreNeutralizedCount);
                case PlanetTrait.CommonMineralRich:
                    return state.WithProgressCounts(
                        state.OrganicRichPlanetVisits,
                        state.VolcanicActivePlanetVisits,
                        state.CommonMineralRichPlanetVisits + 1,
                        state.RareMineralRichPlanetVisits,
                        state.VeryHardContractCompletions,
                        state.ResistanceNeutralizedCount,
                        state.RevolutionNeutralizedCount,
                        state.IstanteNeutralizedCount,
                        state.AtaNeutralizedCount,
                        state.MonstrumNeutralizedCount,
                        state.DoloreNeutralizedCount);
                case PlanetTrait.RareMineralRich:
                    return state.WithProgressCounts(
                        state.OrganicRichPlanetVisits,
                        state.VolcanicActivePlanetVisits,
                        state.CommonMineralRichPlanetVisits,
                        state.RareMineralRichPlanetVisits + 1,
                        state.VeryHardContractCompletions,
                        state.ResistanceNeutralizedCount,
                        state.RevolutionNeutralizedCount,
                        state.IstanteNeutralizedCount,
                        state.AtaNeutralizedCount,
                        state.MonstrumNeutralizedCount,
                        state.DoloreNeutralizedCount);
                default:
                    return state;
            }
        }

        public static SpecialContractProgressState RecordContractCompletion(
            SpecialContractProgressState state,
            ContractDifficulty difficulty)
        {
            if (difficulty != ContractDifficulty.VeryHard)
            {
                return state;
            }

            return state.WithProgressCounts(
                state.OrganicRichPlanetVisits,
                state.VolcanicActivePlanetVisits,
                state.CommonMineralRichPlanetVisits,
                state.RareMineralRichPlanetVisits,
                state.VeryHardContractCompletions + 1,
                state.ResistanceNeutralizedCount,
                state.RevolutionNeutralizedCount,
                state.IstanteNeutralizedCount,
                state.AtaNeutralizedCount,
                state.MonstrumNeutralizedCount,
                state.DoloreNeutralizedCount);
        }

        public static SpecialContractProgressState RecordTransportArrivalProgress(
            SpecialContractProgressState state,
            PlanetTrait destinationTrait,
            TransportContractDefinition? contract,
            bool completedTransport)
        {
            if (!completedTransport)
            {
                return state;
            }

            var next = RecordPlanetVisit(state, destinationTrait);
            return contract.HasValue
                ? RecordContractCompletion(next, contract.Value.Difficulty)
                : next;
        }

        public static bool CanOfferContract(
            SpecialContractProgressState state,
            ReputationState reputation,
            PlanetTrait currentPlanetTrait,
            SpecialContractKind kind)
        {
            if (state.HasActiveContract || IsRewardUnlocked(state, kind))
            {
                return false;
            }

            switch (kind)
            {
                case SpecialContractKind.PresenceDetectorUnlock:
                    return reputation.FameScore >= PresenceDetectorRequiredFame &&
                           currentPlanetTrait == PlanetTrait.OrganicRich &&
                           state.OrganicRichPlanetVisits >= PresenceDetectorRequiredOrganicVisits;
                case SpecialContractKind.LightBladeUnlock:
                    return reputation.FameScore >= LightBladeRequiredFame &&
                           currentPlanetTrait == PlanetTrait.RareMineralRich &&
                           state.VolcanicActivePlanetVisits >= LightBladeRequiredVolcanicVisits &&
                           state.VeryHardContractCompletions >= LightBladeRequiredVeryHardCompletions;
                case SpecialContractKind.ElectricMineUnlock:
                    return reputation.FameScore >= ElectricMineRequiredFame &&
                           (currentPlanetTrait == PlanetTrait.CommonMineralRich ||
                            currentPlanetTrait == PlanetTrait.RareMineralRich) &&
                           state.CommonMineralRichPlanetVisits >= ElectricMineRequiredCommonMineralVisits &&
                           state.RareMineralRichPlanetVisits >= ElectricMineRequiredRareMineralVisits &&
                           state.RevolutionNeutralizedCount >= ElectricMineRequiredRevolutionNeutralizations;
                case SpecialContractKind.CorridorPurifierUnlock:
                    return reputation.FameScore >= CorridorPurifierRequiredFame &&
                           state.MonstrumNeutralizedCount >= CorridorPurifierRequiredEnemyNeutralizations &&
                           state.DoloreNeutralizedCount >= CorridorPurifierRequiredEnemyNeutralizations &&
                           state.RevolutionNeutralizedCount >= CorridorPurifierRequiredEnemyNeutralizations &&
                           state.AtaNeutralizedCount >= CorridorPurifierRequiredEnemyNeutralizations;
                default:
                    return false;
            }
        }

        public static SpecialContractAcceptanceResult AcceptContract(
            SpecialContractProgressState state,
            ReputationState reputation,
            PlanetTrait currentPlanetTrait,
            SpecialContractKind kind)
        {
            if (kind == SpecialContractKind.None)
            {
                return new SpecialContractAcceptanceResult(state, kind, false, "No special contract was selected.");
            }

            if (!CanOfferContract(state, reputation, currentPlanetTrait, kind))
            {
                return new SpecialContractAcceptanceResult(
                    state,
                    kind,
                    false,
                    FormatContractName(kind) + " requirements are not met.");
            }

            var next = state.WithActiveContract(kind);
            return new SpecialContractAcceptanceResult(
                next,
                kind,
                true,
                FormatContractName(kind) + " accepted.");
        }

        public static SpecialContractEnemyRecordResult RecordEnemyNeutralized(
            SpecialContractProgressState state,
            SpecialContractEnemyKind enemyKind)
        {
            if (enemyKind == SpecialContractEnemyKind.None)
            {
                return new SpecialContractEnemyRecordResult(
                    state,
                    enemyKind,
                    false,
                    false,
                    SpecialContractObjectiveItemKind.None);
            }

            var next = IncrementEnemyCount(state, enemyKind);
            var item = GetActiveObjectiveDrop(next.ActiveContractKind, enemyKind);
            if (item != SpecialContractObjectiveItemKind.None)
            {
                next = AddObjectiveItem(next, item);
            }

            return new SpecialContractEnemyRecordResult(
                next,
                enemyKind,
                true,
                item != SpecialContractObjectiveItemKind.None,
                item);
        }

        public static bool ShouldRequestCargoFreedomLeagueSpecialChipDrop(
            SpecialContractProgressState state,
            CargoFreedomLeagueKind kind)
        {
            return state.ActiveContractKind == SpecialContractKind.PresenceDetectorUnlock &&
                   (kind == CargoFreedomLeagueKind.Resistance ||
                    kind == CargoFreedomLeagueKind.Revolution);
        }

        public static SpecialContractProgressState RecordCargoFreedomLeagueDrop(
            SpecialContractProgressState state,
            CargoFreedomLeagueDropResult drop)
        {
            var next = state;
            if (drop.ResistanceChipDropped)
            {
                next = AddObjectiveItem(next, SpecialContractObjectiveItemKind.ResistanceChip);
            }

            if (drop.RevolutionChipDropped)
            {
                next = AddObjectiveItem(next, SpecialContractObjectiveItemKind.RevolutionChip);
            }

            return next;
        }

        public static bool ShouldForceSpecialCargoPriority(SpecialContractProgressState state)
        {
            return state.ActiveContractKind == SpecialContractKind.ElectricMineUnlock;
        }

        public static SpecialContractRouteModifier CreateRouteModifier(SpecialContractProgressState state)
        {
            if (state.ActiveContractKind != SpecialContractKind.CorridorPurifierUnlock)
            {
                return SpecialContractRouteModifier.None;
            }

            return new SpecialContractRouteModifier(
                true,
                CorridorPurifierIntrusionOccurrenceMultiplier,
                CorridorPurifierRouteDurationSeconds);
        }

        public static SpecialContractSettlementResult ResolveTransportArrival(
            SpecialContractProgressState state,
            CargoState cargo,
            bool transportCompleted)
        {
            if (!state.HasActiveContract)
            {
                return new SpecialContractSettlementResult(
                    state,
                    SpecialContractKind.None,
                    false,
                    false,
                    0,
                    EquipmentItemKind.None,
                    EquipmentItemKind.None,
                    string.Empty);
            }

            var active = state.ActiveContractKind;
            if (!transportCompleted)
            {
                return CreateFailureResult(state, active);
            }

            if (!IsActiveContractReadyForCompletion(state, cargo))
            {
                if (active == SpecialContractKind.ElectricMineUnlock &&
                    cargo.SizeUnits >= ElectricMineSpecialCargoMinimumSizeUnits &&
                    cargo.DurabilityPercent < ElectricMineSpecialCargoMinimumDurability)
                {
                    return CreateFailureResult(state, active);
                }

                return new SpecialContractSettlementResult(
                    state,
                    active,
                    false,
                    false,
                    0,
                    EquipmentItemKind.None,
                    EquipmentItemKind.None,
                    FormatContractName(active) + " remains in progress.");
            }

            var definition = GetDefinition(active);
            var nextUnlocks = state.EquipmentUnlocks.WithUnlocked(definition.UnlockItemKind);
            var next = state
                .WithEquipmentUnlocks(nextUnlocks)
                .WithActiveContract(SpecialContractKind.None);
            if (active == SpecialContractKind.CorridorPurifierUnlock)
            {
                next = next.WithCorridorPurifier(true, state.CorridorPurifierChargeCount + CorridorPurifierRewardChargeCount);
            }

            return new SpecialContractSettlementResult(
                next,
                active,
                true,
                false,
                definition.BonusCredits,
                definition.UnlockItemKind,
                definition.GrantedItemKind,
                FormatContractName(active) + " completed.");
        }

        public static SpecialContractProgressState ResetProgressAfterFailure(SpecialContractProgressState state)
        {
            return new SpecialContractProgressState(
                SpecialContractKind.None,
                state.EquipmentUnlocks,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                state.CorridorPurifierInstalled,
                state.CorridorPurifierChargeCount);
        }

        public static TransportContractDefinition CreateElectricMineTransportContract()
        {
            return new TransportContractDefinition(
                "special-electric-mine-cargo-001",
                "Special Electric Mine Cargo",
                "Half-Hold Sensitive Device",
                ContractType.Special,
                ContractDifficulty.Master,
                120,
                0,
                new CargoState(
                    CargoGrade.Premium,
                    ElectricMineSpecialCargoMinimumSizeUnits,
                    0,
                    1f,
                    false),
                false,
                requiredCargoHoldScore: 50,
                originTrait: PlanetTrait.CommonMineralRich,
                destinationTrait: PlanetTrait.RareMineralRich);
        }

        public static TransportContractDefinition CreateCorridorPurifierTransportContract()
        {
            return new TransportContractDefinition(
                "special-corridor-purifier-route-001",
                "Special Corridor Purifier Route",
                "Purifier Calibration Cargo",
                ContractType.Special,
                ContractDifficulty.Master,
                CorridorPurifierRouteDurationSeconds,
                0,
                new CargoState(
                    CargoGrade.Premium,
                    PersonalCargoRules.PremiumCargoSizeUnits,
                    0,
                    1f,
                    false),
                false,
                requiredCargoHoldScore: 80,
                originTrait: PlanetTrait.RareMineralRich,
                destinationTrait: PlanetTrait.OrganicRich);
        }

        public static string FormatContractName(SpecialContractKind kind)
        {
            switch (kind)
            {
                case SpecialContractKind.PresenceDetectorUnlock:
                    return "Presence Detector Unlock";
                case SpecialContractKind.LightBladeUnlock:
                    return "Light Blade Unlock";
                case SpecialContractKind.ElectricMineUnlock:
                    return "Electric Mine Unlock";
                case SpecialContractKind.CorridorPurifierUnlock:
                    return "Corridor Purifier Unlock";
                default:
                    return "None";
            }
        }

        private static bool IsRewardUnlocked(
            SpecialContractProgressState state,
            SpecialContractKind kind)
        {
            switch (kind)
            {
                case SpecialContractKind.PresenceDetectorUnlock:
                    return state.EquipmentUnlocks.PresenceDetectorUnlocked;
                case SpecialContractKind.LightBladeUnlock:
                    return state.EquipmentUnlocks.LightBladeUnlocked;
                case SpecialContractKind.ElectricMineUnlock:
                    return state.EquipmentUnlocks.ElectricMineUnlocked;
                case SpecialContractKind.CorridorPurifierUnlock:
                    return state.EquipmentUnlocks.CorridorPurifierUnlocked;
                default:
                    return false;
            }
        }

        private static SpecialContractProgressState IncrementEnemyCount(
            SpecialContractProgressState state,
            SpecialContractEnemyKind enemyKind)
        {
            return state.WithProgressCounts(
                state.OrganicRichPlanetVisits,
                state.VolcanicActivePlanetVisits,
                state.CommonMineralRichPlanetVisits,
                state.RareMineralRichPlanetVisits,
                state.VeryHardContractCompletions,
                state.ResistanceNeutralizedCount + (enemyKind == SpecialContractEnemyKind.Resistance ? 1 : 0),
                state.RevolutionNeutralizedCount + (enemyKind == SpecialContractEnemyKind.Revolution ? 1 : 0),
                state.IstanteNeutralizedCount + (enemyKind == SpecialContractEnemyKind.Istante ? 1 : 0),
                state.AtaNeutralizedCount + (enemyKind == SpecialContractEnemyKind.Ata ? 1 : 0),
                state.MonstrumNeutralizedCount + (enemyKind == SpecialContractEnemyKind.Monstrum ? 1 : 0),
                state.DoloreNeutralizedCount + (enemyKind == SpecialContractEnemyKind.Dolore ? 1 : 0));
        }

        private static SpecialContractObjectiveItemKind GetActiveObjectiveDrop(
            SpecialContractKind activeContract,
            SpecialContractEnemyKind enemyKind)
        {
            switch (activeContract)
            {
                case SpecialContractKind.PresenceDetectorUnlock:
                    if (enemyKind == SpecialContractEnemyKind.Resistance)
                    {
                        return SpecialContractObjectiveItemKind.ResistanceChip;
                    }

                    return enemyKind == SpecialContractEnemyKind.Revolution
                        ? SpecialContractObjectiveItemKind.RevolutionChip
                        : SpecialContractObjectiveItemKind.None;
                case SpecialContractKind.LightBladeUnlock:
                    if (enemyKind == SpecialContractEnemyKind.Istante)
                    {
                        return SpecialContractObjectiveItemKind.IstantePowerCore;
                    }

                    return enemyKind == SpecialContractEnemyKind.Ata
                        ? SpecialContractObjectiveItemKind.AtaControlModule
                        : SpecialContractObjectiveItemKind.None;
                default:
                    return SpecialContractObjectiveItemKind.None;
            }
        }

        private static SpecialContractProgressState AddObjectiveItem(
            SpecialContractProgressState state,
            SpecialContractObjectiveItemKind itemKind)
        {
            switch (itemKind)
            {
                case SpecialContractObjectiveItemKind.ResistanceChip:
                    return state.WithObjectiveItems(
                        state.ResistanceChipCount + 1,
                        state.RevolutionChipCount,
                        state.IstantePowerCoreCount,
                        state.AtaControlModuleCount);
                case SpecialContractObjectiveItemKind.RevolutionChip:
                    return state.WithObjectiveItems(
                        state.ResistanceChipCount,
                        state.RevolutionChipCount + 1,
                        state.IstantePowerCoreCount,
                        state.AtaControlModuleCount);
                case SpecialContractObjectiveItemKind.IstantePowerCore:
                    return state.WithObjectiveItems(
                        state.ResistanceChipCount,
                        state.RevolutionChipCount,
                        state.IstantePowerCoreCount + 1,
                        state.AtaControlModuleCount);
                case SpecialContractObjectiveItemKind.AtaControlModule:
                    return state.WithObjectiveItems(
                        state.ResistanceChipCount,
                        state.RevolutionChipCount,
                        state.IstantePowerCoreCount,
                        state.AtaControlModuleCount + 1);
                default:
                    return state;
            }
        }

        private static bool IsActiveContractReadyForCompletion(
            SpecialContractProgressState state,
            CargoState cargo)
        {
            switch (state.ActiveContractKind)
            {
                case SpecialContractKind.PresenceDetectorUnlock:
                    return state.ResistanceChipCount >= PresenceDetectorRequiredResistanceChips &&
                           state.RevolutionChipCount >= PresenceDetectorRequiredRevolutionChips;
                case SpecialContractKind.LightBladeUnlock:
                    return state.IstantePowerCoreCount >= LightBladeRequiredIstantePowerCores &&
                           state.AtaControlModuleCount >= LightBladeRequiredAtaControlModules;
                case SpecialContractKind.ElectricMineUnlock:
                    return cargo.SizeUnits >= ElectricMineSpecialCargoMinimumSizeUnits &&
                           cargo.DurabilityPercent >= ElectricMineSpecialCargoMinimumDurability;
                case SpecialContractKind.CorridorPurifierUnlock:
                    return true;
                default:
                    return false;
            }
        }

        private static SpecialContractSettlementResult CreateFailureResult(
            SpecialContractProgressState state,
            SpecialContractKind active)
        {
            return new SpecialContractSettlementResult(
                ResetProgressAfterFailure(state),
                active,
                false,
                true,
                0,
                EquipmentItemKind.None,
                EquipmentItemKind.None,
                FormatContractName(active) + " failed; non-fame progress was reset.");
        }
    }
}
