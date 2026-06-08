using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bellerophon.Platform;
using UnityEngine;

namespace Bellerophon.Core.Session
{
    [Serializable]
    public sealed class SaveGameDocument
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public GameStartFlowSaveData startFlow;
        public GameSettingsSaveData settings;
        public PlanetVisitRecordSaveData[] visitedPlanets;
    }

    [Serializable]
    public sealed class GameStartFlowSaveData
    {
        public int phase;
        public GameSessionSaveData session;
        public TransportContractSaveData[] availableContracts;
        public float associationContractProgress01;
        public bool associationContractStopped;
        public bool hasCompletedTutorialBefore;
        public bool tutorialSkipped;
    }

    [Serializable]
    public sealed class GameSessionSaveData
    {
        public int phase;
        public ShipSaveData ship;
        public WalletSaveData wallet;
        public SettlementResultSaveData settlement;
        public bool isAssociationMember;
        public PlanetStartSaveData currentPlanet;
        public StartingLoadoutSaveData startingLoadout;
        public PlayerEquipmentSaveData equipment;
        public ReputationSaveData reputation;
        public PersonalCargoHoldSaveData personalCargoHold;
        public ShipUpgradeSaveData shipUpgrades;
        public int currentPlanetTrait;
        public int completedTransportCount;
        public int towingIncidentCount;
        public bool hasActiveTransportContract;
        public TransportContractSaveData activeTransportContract;
        public bool hasActiveCargo;
        public CargoSaveData activeCargo;
        public TransportContractSaveData[] pendingTransportContracts;
        public TransportContractSaveData[] activeTransportContracts;
        public TransportHazardUnlockSaveData transportHazardUnlocks;
        public SpecialContractProgressSaveData specialContracts;
    }

    [Serializable]
    public sealed class GameSettingsSaveData
    {
        public int resolutionWidth;
        public int resolutionHeight;
        public bool fullscreen;
        public float masterVolume;
        public float musicVolume;
        public float effectsVolume;
        public float mouseSensitivity;
        public bool highContrastUi;
        public bool reduceCameraShake;
    }

    [Serializable]
    public sealed class PlanetVisitRecordSaveData
    {
        public int trait;
        public int visitCount;
        public bool isCurrent;
    }

    [Serializable]
    public sealed class ShipSaveData
    {
        public int runState;
        public ShipRoomSaveData[] rooms;
    }

    [Serializable]
    public sealed class ShipRoomSaveData
    {
        public int roomId;
        public int currentDurability;
        public int maxDurability;
        public bool isFunctionOffline;
        public bool isBlackout;
        public bool isSealed;
    }

    [Serializable]
    public sealed class WalletSaveData
    {
        public int credits;
        public bool allowsDebt;
        public bool hasUnpaidDebtGrace;
    }

    [Serializable]
    public sealed class SettlementResultSaveData
    {
        public int grossRevenue;
        public int expenses;
        public int netChange;
        public int finalBalance;
        public bool isTransportFailed;
        public bool requiresTowing;
        public bool isGameOver;
        public float cargoHoldScore;
        public float personalCargoSaleMultiplier;
        public int debtStatus;
        public int pendingRepairCost;
        public SettlementLineItemSaveData[] lineItems;
    }

    [Serializable]
    public sealed class SettlementLineItemSaveData
    {
        public string label;
        public int amount;
        public bool isRevenue;
        public bool affectsBalance;
    }

    [Serializable]
    public sealed class PlanetStartSaveData
    {
        public string displayName;
        public bool hasAssociationLogoSign;
    }

    [Serializable]
    public sealed class StartingLoadoutSaveData
    {
        public bool hasDefaultCargoShip;
        public bool hasBasicProtectiveSuit;
        public int stickCount;
    }

    [Serializable]
    public sealed class PlayerEquipmentSaveData
    {
        public bool hasBasicProtectiveSuit;
        public EquipmentSlotSaveData[] handSlots;
        public EquipmentSlotSaveData[] supplySlots;
        public int activeHandSlotIndex;
        public float useCooldownSeconds;
        public int activeMode;
        public string lastActionSummary;
        public int unlockedHandSlotCount;
        public int unlockedSupplySlotCount;
        public int activeProtectiveItemKind;
        public int activeDamageReductionPercent;
        public float strengthEnhancerRemainingSeconds;
        public int strengthDamageBonusPercent;
        public float flashlightRemainingSeconds;
        public float electricBatonChargeCooldownSeconds;
        public float miniFlamethrowerContinuousHitSeconds;
        public float miniFlamethrowerHitGapSeconds;
    }

    [Serializable]
    public sealed class EquipmentSlotSaveData
    {
        public int itemKind;
        public int count;
        public int durabilityPercent;
        public int purchasePriceCredits;
    }

    [Serializable]
    public sealed class ReputationSaveData
    {
        public int fameScore;
        public int associationFameScore;
        public bool hasUsedRevivalContract;
    }

    [Serializable]
    public sealed class PersonalCargoHoldSaveData
    {
        public PersonalCargoItemSaveData[] items;
    }

    [Serializable]
    public sealed class PersonalCargoItemSaveData
    {
        public string id;
        public string displayName;
        public int grade;
        public int sizeUnits;
        public int baseSaleValue;
        public int originTrait;
        public float durabilityPercent;
    }

    [Serializable]
    public sealed class ShipUpgradeSaveData
    {
        public int durabilityPurchasedTier;
        public int durabilityEquippedTier;
        public int weaponSystemsPurchasedTier;
        public int weaponSystemsEquippedTier;
        public int autoPilotPurchasedTier;
        public int autoPilotEquippedTier;
        public int supplySlotsPurchasedTier;
        public int supplySlotsEquippedTier;
        public int internalControlPurchasedTier;
        public int internalControlEquippedTier;
        public ShipAppearanceSaveData appearance;
    }

    [Serializable]
    public sealed class ShipAppearanceSaveData
    {
        public string hullPaintSlotId;
        public string emblemSlotId;
        public string nameplateSlotId;
    }

    [Serializable]
    public sealed class TransportContractSaveData
    {
        public string id;
        public string displayName;
        public string transportTargetName;
        public int contractType;
        public int difficulty;
        public int durationSeconds;
        public int rewardCredits;
        public CargoSaveData cargo;
        public bool isTutorial;
        public int requiredCargoHoldScore;
        public bool isRevivalContract;
        public int originTrait;
        public int destinationTrait;
    }

    [Serializable]
    public sealed class CargoSaveData
    {
        public int grade;
        public int sizeUnits;
        public int baseValue;
        public float durabilityPercent;
        public bool isPersonalCargo;
    }

    [Serializable]
    public sealed class TransportHazardUnlockSaveData
    {
        public bool cargoFreedomLeagueUnlocked;
        public bool spacePirateUnlocked;
        public bool alienLifeUnlocked;
        public bool concealedBlackHoleUnlocked;
    }

    [Serializable]
    public sealed class SpecialContractProgressSaveData
    {
        public int activeContractKind;
        public SpecialEquipmentUnlockSaveData equipmentUnlocks;
        public int organicRichPlanetVisits;
        public int volcanicActivePlanetVisits;
        public int commonMineralRichPlanetVisits;
        public int rareMineralRichPlanetVisits;
        public int veryHardContractCompletions;
        public int resistanceNeutralizedCount;
        public int revolutionNeutralizedCount;
        public int istanteNeutralizedCount;
        public int ataNeutralizedCount;
        public int monstrumNeutralizedCount;
        public int doloreNeutralizedCount;
        public int resistanceChipCount;
        public int revolutionChipCount;
        public int istantePowerCoreCount;
        public int ataControlModuleCount;
        public bool corridorPurifierInstalled;
        public int corridorPurifierChargeCount;
    }

    [Serializable]
    public sealed class SpecialEquipmentUnlockSaveData
    {
        public bool presenceDetectorUnlocked;
        public bool lightBladeUnlocked;
        public bool electricMineUnlocked;
        public bool corridorPurifierUnlocked;
    }

    public static class SaveGameSerializer
    {
        public static string ToJson(SaveGameDocument document, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(SaveGameMigration.Migrate(document), prettyPrint);
        }

        public static SaveGameDocument FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Save game JSON is required.", nameof(json));
            }

            return SaveGameMigration.Migrate(JsonUtility.FromJson<SaveGameDocument>(json));
        }
    }

    public static class SaveGameMigration
    {
        public static SaveGameDocument Migrate(SaveGameDocument document)
        {
            if (document == null)
            {
                document = SaveGameMapper.CreateDocument(
                    NewGameStartFlowState.CreateNewGame(),
                    GameSettingsState.Default);
            }

            if (document.version > SaveGameDocument.CurrentVersion)
            {
                throw new InvalidOperationException(
                    "Save file version " + document.version +
                    " is newer than supported version " + SaveGameDocument.CurrentVersion + ".");
            }

            if (document.startFlow == null)
            {
                document.startFlow = SaveGameMapper.CreateStartFlowSaveData(NewGameStartFlowState.CreateNewGame());
            }

            if (document.startFlow.session == null)
            {
                document.startFlow.session = SaveGameMapper.CreateSessionSaveData(
                    GameSessionState.StartSession(new WalletState(0, false)));
            }

            if (document.settings == null)
            {
                document.settings = SaveGameMapper.CreateSettingsSaveData(GameSettingsState.Default);
            }

            if (document.visitedPlanets == null)
            {
                document.visitedPlanets = SaveGameMapper.CreateVisitedPlanetSaveData(
                    SaveGameMapper.ToSession(document.startFlow.session));
            }

            document.version = SaveGameDocument.CurrentVersion;
            return document;
        }
    }

    public static class SaveGameMapper
    {
        private static readonly ShipRoomId[] ShipRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom
        };

        public static SaveGameDocument CreateDocument(
            NewGameStartFlowState flow,
            GameSettingsState settings)
        {
            if (flow == null)
            {
                throw new ArgumentNullException(nameof(flow));
            }

            var hasCompletedTutorial = HasCompletedTutorial(flow);
            var normalizedFlow = flow.HasCompletedTutorialBefore == hasCompletedTutorial
                ? flow
                : flow.WithTutorialCompletedBefore(hasCompletedTutorial);

            return new SaveGameDocument
            {
                version = SaveGameDocument.CurrentVersion,
                startFlow = CreateStartFlowSaveData(normalizedFlow),
                settings = CreateSettingsSaveData(settings),
                visitedPlanets = CreateVisitedPlanetSaveData(normalizedFlow.Session)
            };
        }

        public static GameStartFlowSaveData CreateStartFlowSaveData(NewGameStartFlowState flow)
        {
            if (flow == null)
            {
                throw new ArgumentNullException(nameof(flow));
            }

            var contracts = new TransportContractSaveData[flow.AvailableContractCount];
            for (var i = 0; i < contracts.Length; i++)
            {
                contracts[i] = CreateContractSaveData(flow.GetAvailableContract(i));
            }

            return new GameStartFlowSaveData
            {
                phase = (int)flow.Phase,
                session = CreateSessionSaveData(flow.Session),
                availableContracts = contracts,
                associationContractProgress01 = flow.AssociationContractScroll.Progress01,
                associationContractStopped = flow.AssociationContractScroll.IsStopped,
                hasCompletedTutorialBefore = HasCompletedTutorial(flow),
                tutorialSkipped = flow.TutorialSkipped
            };
        }

        public static GameSessionSaveData CreateSessionSaveData(GameSessionState session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return new GameSessionSaveData
            {
                phase = (int)session.Phase,
                ship = CreateShipSaveData(session.Ship),
                wallet = CreateWalletSaveData(session.Wallet),
                settlement = CreateSettlementSaveData(session.SettlementResult),
                isAssociationMember = session.IsAssociationMember,
                currentPlanet = CreatePlanetStartSaveData(session.CurrentPlanet),
                startingLoadout = CreateStartingLoadoutSaveData(session.StartingLoadout),
                equipment = CreateEquipmentSaveData(session.Equipment),
                reputation = CreateReputationSaveData(session.Reputation),
                personalCargoHold = CreatePersonalCargoHoldSaveData(session.PersonalCargoHold),
                shipUpgrades = CreateShipUpgradeSaveData(session.ShipUpgrades),
                currentPlanetTrait = (int)session.CurrentPlanetTrait,
                completedTransportCount = session.CompletedTransportCount,
                towingIncidentCount = session.TowingIncidentCount,
                hasActiveTransportContract = session.ActiveTransportContract.HasValue,
                activeTransportContract = session.ActiveTransportContract.HasValue
                    ? CreateContractSaveData(session.ActiveTransportContract.Value)
                    : null,
                hasActiveCargo = session.ActiveCargo.HasValue,
                activeCargo = session.ActiveCargo.HasValue ? CreateCargoSaveData(session.ActiveCargo.Value) : null,
                pendingTransportContracts = CreateContractSaveData(session.PendingTransportContracts),
                activeTransportContracts = CreateContractSaveData(session.ActiveTransportContracts),
                transportHazardUnlocks = CreateHazardUnlockSaveData(session.TransportHazardUnlocks),
                specialContracts = CreateSpecialContractSaveData(session.SpecialContracts)
            };
        }

        public static GameSettingsSaveData CreateSettingsSaveData(GameSettingsState settings)
        {
            return new GameSettingsSaveData
            {
                resolutionWidth = settings.ResolutionWidth,
                resolutionHeight = settings.ResolutionHeight,
                fullscreen = settings.Fullscreen,
                masterVolume = settings.MasterVolume,
                musicVolume = settings.MusicVolume,
                effectsVolume = settings.EffectsVolume,
                mouseSensitivity = settings.MouseSensitivity,
                highContrastUi = settings.HighContrastUi,
                reduceCameraShake = settings.ReduceCameraShake
            };
        }

        public static PlanetVisitRecordSaveData[] CreateVisitedPlanetSaveData(GameSessionState session)
        {
            if (session == null)
            {
                return new PlanetVisitRecordSaveData[0];
            }

            var records = new List<PlanetVisitRecordSaveData>();
            AddVisitRecord(records, PlanetTrait.OrganicRich, session.SpecialContracts.OrganicRichPlanetVisits, session.CurrentPlanetTrait);
            AddVisitRecord(records, PlanetTrait.VolcanicActive, session.SpecialContracts.VolcanicActivePlanetVisits, session.CurrentPlanetTrait);
            AddVisitRecord(records, PlanetTrait.CommonMineralRich, session.SpecialContracts.CommonMineralRichPlanetVisits, session.CurrentPlanetTrait);
            AddVisitRecord(records, PlanetTrait.RareMineralRich, session.SpecialContracts.RareMineralRichPlanetVisits, session.CurrentPlanetTrait);

            var containsCurrent = false;
            for (var i = 0; i < records.Count; i++)
            {
                if ((PlanetTrait)records[i].trait == session.CurrentPlanetTrait)
                {
                    containsCurrent = true;
                    break;
                }
            }

            if (!containsCurrent)
            {
                records.Add(new PlanetVisitRecordSaveData
                {
                    trait = (int)session.CurrentPlanetTrait,
                    visitCount = session.CompletedTransportCount > 0 ? 1 : 0,
                    isCurrent = true
                });
            }

            return records.ToArray();
        }

        public static NewGameStartFlowState ToStartFlow(SaveGameDocument document)
        {
            var migrated = SaveGameMigration.Migrate(document);
            return ToStartFlow(migrated.startFlow);
        }

        public static NewGameStartFlowState CreateNewGameFromProfile(SaveGameDocument document)
        {
            var migrated = SaveGameMigration.Migrate(document);
            return NewGameStartFlowState.CreateNewGame(HasCompletedTutorial(migrated.startFlow));
        }

        public static NewGameStartFlowState ToStartFlow(GameStartFlowSaveData data)
        {
            if (data == null)
            {
                return NewGameStartFlowState.CreateNewGame();
            }

            return NewGameStartFlowState.Restore(
                (NewGameStartFlowPhase)data.phase,
                ToSession(data.session),
                ToContracts(data.availableContracts),
                new AssociationContractScrollState(
                    data.associationContractProgress01,
                    data.associationContractStopped),
                HasCompletedTutorial(data),
                data.tutorialSkipped);
        }

        public static GameSessionState ToSession(GameSessionSaveData data)
        {
            if (data == null)
            {
                return GameSessionState.StartSession(new WalletState(0, false));
            }

            TransportContractDefinition? activeContract = null;
            if (data.hasActiveTransportContract && data.activeTransportContract != null)
            {
                activeContract = ToContract(data.activeTransportContract);
            }

            CargoState? activeCargo = null;
            if (data.hasActiveCargo && data.activeCargo != null)
            {
                activeCargo = ToCargo(data.activeCargo);
            }

            return GameSessionState.Restore(
                (GameSessionPhase)data.phase,
                ToShip(data.ship),
                ToWallet(data.wallet),
                ToSettlement(data.settlement),
                data.isAssociationMember,
                ToPlanetStart(data.currentPlanet),
                ToStartingLoadout(data.startingLoadout),
                ToEquipment(data.equipment),
                ToReputation(data.reputation),
                ToPersonalCargoHold(data.personalCargoHold),
                ToShipUpgrade(data.shipUpgrades),
                (PlanetTrait)data.currentPlanetTrait,
                data.completedTransportCount,
                data.towingIncidentCount,
                activeContract,
                activeCargo,
                ToContracts(data.pendingTransportContracts),
                ToContracts(data.activeTransportContracts),
                ToHazardUnlock(data.transportHazardUnlocks),
                ToSpecialContracts(data.specialContracts));
        }

        public static GameSettingsState ToSettings(SaveGameDocument document)
        {
            return ToSettings(SaveGameMigration.Migrate(document).settings);
        }

        public static GameSettingsState ToSettings(GameSettingsSaveData data)
        {
            if (data == null)
            {
                return GameSettingsState.Default;
            }

            return new GameSettingsState(
                data.resolutionWidth,
                data.resolutionHeight,
                data.fullscreen,
                data.masterVolume,
                data.musicVolume,
                data.effectsVolume,
                data.mouseSensitivity,
                data.highContrastUi,
                data.reduceCameraShake);
        }

        private static bool HasCompletedTutorial(NewGameStartFlowState flow)
        {
            return flow.HasCompletedTutorialBefore ||
                   flow.TutorialSkipped ||
                   flow.Session.CompletedTransportCount > 0;
        }

        private static bool HasCompletedTutorial(GameStartFlowSaveData flow)
        {
            if (flow == null)
            {
                return false;
            }

            return flow.hasCompletedTutorialBefore ||
                   flow.tutorialSkipped ||
                   (flow.session != null && flow.session.completedTransportCount > 0);
        }

        private static void AddVisitRecord(
            List<PlanetVisitRecordSaveData> records,
            PlanetTrait trait,
            int count,
            PlanetTrait currentTrait)
        {
            if (count <= 0 && trait != currentTrait)
            {
                return;
            }

            records.Add(new PlanetVisitRecordSaveData
            {
                trait = (int)trait,
                visitCount = Math.Max(0, count),
                isCurrent = trait == currentTrait
            });
        }

        private static ShipSaveData CreateShipSaveData(ShipState ship)
        {
            var rooms = new ShipRoomSaveData[ShipRoomOrder.Length];
            for (var i = 0; i < ShipRoomOrder.Length; i++)
            {
                var room = ship.GetRoom(ShipRoomOrder[i]);
                rooms[i] = new ShipRoomSaveData
                {
                    roomId = (int)ShipRoomOrder[i],
                    currentDurability = room.CurrentDurability,
                    maxDurability = room.MaxDurability,
                    isFunctionOffline = room.IsFunctionOffline,
                    isBlackout = room.IsBlackout,
                    isSealed = room.IsSealed
                };
            }

            return new ShipSaveData
            {
                runState = (int)ship.RunState,
                rooms = rooms
            };
        }

        private static WalletSaveData CreateWalletSaveData(WalletState wallet)
        {
            return new WalletSaveData
            {
                credits = wallet.Credits,
                allowsDebt = wallet.AllowsDebt,
                hasUnpaidDebtGrace = wallet.HasUnpaidDebtGrace
            };
        }

        private static SettlementResultSaveData CreateSettlementSaveData(SettlementResult settlement)
        {
            var lines = settlement.LineItems ?? new SettlementLineItem[0];
            var lineData = new SettlementLineItemSaveData[lines.Length];
            for (var i = 0; i < lines.Length; i++)
            {
                lineData[i] = new SettlementLineItemSaveData
                {
                    label = lines[i].Label,
                    amount = lines[i].Amount,
                    isRevenue = lines[i].IsRevenue,
                    affectsBalance = lines[i].AffectsBalance
                };
            }

            return new SettlementResultSaveData
            {
                grossRevenue = settlement.GrossRevenue,
                expenses = settlement.Expenses,
                netChange = settlement.NetChange,
                finalBalance = settlement.FinalBalance,
                isTransportFailed = settlement.IsTransportFailed,
                requiresTowing = settlement.RequiresTowing,
                isGameOver = settlement.IsGameOver,
                cargoHoldScore = settlement.CargoHoldScore,
                personalCargoSaleMultiplier = settlement.PersonalCargoSaleMultiplier,
                debtStatus = (int)settlement.DebtStatus,
                pendingRepairCost = settlement.PendingRepairCost,
                lineItems = lineData
            };
        }

        private static PlanetStartSaveData CreatePlanetStartSaveData(PlanetStartState planet)
        {
            return new PlanetStartSaveData
            {
                displayName = planet.DisplayName,
                hasAssociationLogoSign = planet.HasAssociationLogoSign
            };
        }

        private static StartingLoadoutSaveData CreateStartingLoadoutSaveData(StartingLoadoutState loadout)
        {
            return new StartingLoadoutSaveData
            {
                hasDefaultCargoShip = loadout.HasDefaultCargoShip,
                hasBasicProtectiveSuit = loadout.HasBasicProtectiveSuit,
                stickCount = loadout.StickCount
            };
        }

        private static PlayerEquipmentSaveData CreateEquipmentSaveData(PlayerEquipmentState equipment)
        {
            return new PlayerEquipmentSaveData
            {
                hasBasicProtectiveSuit = equipment.HasBasicProtectiveSuit,
                handSlots = CreateSlotSaveData(equipment.HandSlots),
                supplySlots = CreateSlotSaveData(equipment.SupplySlots),
                activeHandSlotIndex = equipment.ActiveHandSlotIndex,
                useCooldownSeconds = equipment.UseCooldownSeconds,
                activeMode = (int)equipment.ActiveMode,
                lastActionSummary = equipment.LastActionSummary,
                unlockedHandSlotCount = equipment.UnlockedHandSlotCount,
                unlockedSupplySlotCount = equipment.UnlockedSupplySlotCount,
                activeProtectiveItemKind = (int)equipment.ActiveProtectiveItemKind,
                activeDamageReductionPercent = equipment.ActiveDamageReductionPercent,
                strengthEnhancerRemainingSeconds = equipment.StrengthEnhancerRemainingSeconds,
                strengthDamageBonusPercent = equipment.StrengthDamageBonusPercent,
                flashlightRemainingSeconds = equipment.FlashlightRemainingSeconds,
                electricBatonChargeCooldownSeconds = equipment.ElectricBatonChargeCooldownSeconds,
                miniFlamethrowerContinuousHitSeconds = equipment.MiniFlamethrowerContinuousHitSeconds,
                miniFlamethrowerHitGapSeconds = equipment.MiniFlamethrowerHitGapSeconds
            };
        }

        private static EquipmentSlotSaveData[] CreateSlotSaveData(EquipmentSlotState[] slots)
        {
            if (slots == null || slots.Length == 0)
            {
                return new EquipmentSlotSaveData[0];
            }

            var data = new EquipmentSlotSaveData[slots.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                data[i] = new EquipmentSlotSaveData
                {
                    itemKind = (int)slots[i].ItemKind,
                    count = slots[i].Count,
                    durabilityPercent = slots[i].DurabilityPercent,
                    purchasePriceCredits = slots[i].PurchasePriceCredits
                };
            }

            return data;
        }

        private static ReputationSaveData CreateReputationSaveData(ReputationState reputation)
        {
            return new ReputationSaveData
            {
                fameScore = reputation.FameScore,
                associationFameScore = reputation.AssociationFameScore,
                hasUsedRevivalContract = reputation.HasUsedRevivalContract
            };
        }

        private static PersonalCargoHoldSaveData CreatePersonalCargoHoldSaveData(PersonalCargoHoldState hold)
        {
            var items = hold == null ? new PersonalCargoItemState[0] : hold.Items;
            var data = new PersonalCargoItemSaveData[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                data[i] = new PersonalCargoItemSaveData
                {
                    id = items[i].Id,
                    displayName = items[i].DisplayName,
                    grade = (int)items[i].Grade,
                    sizeUnits = items[i].SizeUnits,
                    baseSaleValue = items[i].BaseSaleValue,
                    originTrait = (int)items[i].OriginTrait,
                    durabilityPercent = items[i].DurabilityPercent
                };
            }

            return new PersonalCargoHoldSaveData { items = data };
        }

        private static ShipUpgradeSaveData CreateShipUpgradeSaveData(ShipUpgradeState upgrades)
        {
            return new ShipUpgradeSaveData
            {
                durabilityPurchasedTier = upgrades.DurabilityPurchasedTier,
                durabilityEquippedTier = upgrades.DurabilityEquippedTier,
                weaponSystemsPurchasedTier = upgrades.WeaponSystemsPurchasedTier,
                weaponSystemsEquippedTier = upgrades.WeaponSystemsEquippedTier,
                autoPilotPurchasedTier = upgrades.AutoPilotPurchasedTier,
                autoPilotEquippedTier = upgrades.AutoPilotEquippedTier,
                supplySlotsPurchasedTier = upgrades.SupplySlotsPurchasedTier,
                supplySlotsEquippedTier = upgrades.SupplySlotsEquippedTier,
                internalControlPurchasedTier = upgrades.InternalControlPurchasedTier,
                internalControlEquippedTier = upgrades.InternalControlEquippedTier,
                appearance = new ShipAppearanceSaveData
                {
                    hullPaintSlotId = upgrades.Appearance.HullPaintSlotId,
                    emblemSlotId = upgrades.Appearance.EmblemSlotId,
                    nameplateSlotId = upgrades.Appearance.NameplateSlotId
                }
            };
        }

        private static TransportContractSaveData[] CreateContractSaveData(TransportContractDefinition[] contracts)
        {
            if (contracts == null || contracts.Length == 0)
            {
                return new TransportContractSaveData[0];
            }

            var data = new TransportContractSaveData[contracts.Length];
            for (var i = 0; i < contracts.Length; i++)
            {
                data[i] = CreateContractSaveData(contracts[i]);
            }

            return data;
        }

        private static TransportContractSaveData CreateContractSaveData(TransportContractDefinition contract)
        {
            return new TransportContractSaveData
            {
                id = contract.Id,
                displayName = contract.DisplayName,
                transportTargetName = contract.TransportTargetName,
                contractType = (int)contract.ContractType,
                difficulty = (int)contract.Difficulty,
                durationSeconds = contract.DurationSeconds,
                rewardCredits = contract.RewardCredits,
                cargo = CreateCargoSaveData(contract.Cargo),
                isTutorial = contract.IsTutorial,
                requiredCargoHoldScore = contract.RequiredCargoHoldScore,
                isRevivalContract = contract.IsRevivalContract,
                originTrait = (int)contract.OriginTrait,
                destinationTrait = (int)contract.DestinationTrait
            };
        }

        private static CargoSaveData CreateCargoSaveData(CargoState cargo)
        {
            return new CargoSaveData
            {
                grade = (int)cargo.Grade,
                sizeUnits = cargo.SizeUnits,
                baseValue = cargo.BaseValue,
                durabilityPercent = cargo.DurabilityPercent,
                isPersonalCargo = cargo.IsPersonalCargo
            };
        }

        private static TransportHazardUnlockSaveData CreateHazardUnlockSaveData(TransportHazardUnlockState unlocks)
        {
            return new TransportHazardUnlockSaveData
            {
                cargoFreedomLeagueUnlocked = unlocks.CargoFreedomLeagueUnlocked,
                spacePirateUnlocked = unlocks.SpacePirateUnlocked,
                alienLifeUnlocked = unlocks.AlienLifeUnlocked,
                concealedBlackHoleUnlocked = unlocks.ConcealedBlackHoleUnlocked
            };
        }

        private static SpecialContractProgressSaveData CreateSpecialContractSaveData(SpecialContractProgressState state)
        {
            return new SpecialContractProgressSaveData
            {
                activeContractKind = (int)state.ActiveContractKind,
                equipmentUnlocks = new SpecialEquipmentUnlockSaveData
                {
                    presenceDetectorUnlocked = state.EquipmentUnlocks.PresenceDetectorUnlocked,
                    lightBladeUnlocked = state.EquipmentUnlocks.LightBladeUnlocked,
                    electricMineUnlocked = state.EquipmentUnlocks.ElectricMineUnlocked,
                    corridorPurifierUnlocked = state.EquipmentUnlocks.CorridorPurifierUnlocked
                },
                organicRichPlanetVisits = state.OrganicRichPlanetVisits,
                volcanicActivePlanetVisits = state.VolcanicActivePlanetVisits,
                commonMineralRichPlanetVisits = state.CommonMineralRichPlanetVisits,
                rareMineralRichPlanetVisits = state.RareMineralRichPlanetVisits,
                veryHardContractCompletions = state.VeryHardContractCompletions,
                resistanceNeutralizedCount = state.ResistanceNeutralizedCount,
                revolutionNeutralizedCount = state.RevolutionNeutralizedCount,
                istanteNeutralizedCount = state.IstanteNeutralizedCount,
                ataNeutralizedCount = state.AtaNeutralizedCount,
                monstrumNeutralizedCount = state.MonstrumNeutralizedCount,
                doloreNeutralizedCount = state.DoloreNeutralizedCount,
                resistanceChipCount = state.ResistanceChipCount,
                revolutionChipCount = state.RevolutionChipCount,
                istantePowerCoreCount = state.IstantePowerCoreCount,
                ataControlModuleCount = state.AtaControlModuleCount,
                corridorPurifierInstalled = state.CorridorPurifierInstalled,
                corridorPurifierChargeCount = state.CorridorPurifierChargeCount
            };
        }

        private static ShipState ToShip(ShipSaveData data)
        {
            var ship = ShipState.CreateDefault();
            if (data != null && data.rooms != null)
            {
                for (var i = 0; i < data.rooms.Length; i++)
                {
                    var room = data.rooms[i];
                    if (room == null)
                    {
                        continue;
                    }

                    ship = ship.WithRoom(
                        (ShipRoomId)room.roomId,
                        new ShipRoomState(
                            room.currentDurability,
                            room.maxDurability <= 0 ? 100 : room.maxDurability,
                            room.isFunctionOffline,
                            room.isBlackout,
                            room.isSealed));
                }
            }

            return ship.WithRunState(data == null ? ShipRunState.Docked : (ShipRunState)data.runState);
        }

        private static WalletState ToWallet(WalletSaveData data)
        {
            return data == null
                ? new WalletState(0, false)
                : new WalletState(data.credits, data.allowsDebt, data.hasUnpaidDebtGrace);
        }

        private static SettlementResult ToSettlement(SettlementResultSaveData data)
        {
            if (data == null)
            {
                return default;
            }

            var lines = data.lineItems == null
                ? new SettlementLineItem[0]
                : new SettlementLineItem[data.lineItems.Length];
            for (var i = 0; i < lines.Length; i++)
            {
                var line = data.lineItems[i];
                lines[i] = line == null
                    ? new SettlementLineItem("Unknown", 0, false)
                    : new SettlementLineItem(
                        string.IsNullOrWhiteSpace(line.label) ? "Unknown" : line.label,
                        line.amount,
                        line.isRevenue,
                        line.affectsBalance);
            }

            return new SettlementResult(
                data.grossRevenue,
                data.expenses,
                data.netChange,
                data.finalBalance,
                data.isTransportFailed,
                data.requiresTowing,
                data.isGameOver,
                data.cargoHoldScore,
                data.personalCargoSaleMultiplier,
                (SettlementDebtStatus)data.debtStatus,
                lines,
                data.pendingRepairCost);
        }

        private static PlanetStartState ToPlanetStart(PlanetStartSaveData data)
        {
            return data == null
                ? PlanetStartState.None
                : new PlanetStartState(data.displayName, data.hasAssociationLogoSign);
        }

        private static StartingLoadoutState ToStartingLoadout(StartingLoadoutSaveData data)
        {
            return data == null
                ? StartingLoadoutState.Empty
                : new StartingLoadoutState(
                    data.hasDefaultCargoShip,
                    data.hasBasicProtectiveSuit,
                    data.stickCount);
        }

        private static PlayerEquipmentState ToEquipment(PlayerEquipmentSaveData data)
        {
            if (data == null)
            {
                return PlayerEquipmentState.Empty;
            }

            return new PlayerEquipmentState(
                data.hasBasicProtectiveSuit,
                ToSlots(data.handSlots),
                ToSlots(data.supplySlots),
                data.activeHandSlotIndex,
                data.useCooldownSeconds,
                (EquipmentUseMode)data.activeMode,
                data.lastActionSummary,
                data.unlockedHandSlotCount <= 0 ? PlayerEquipmentState.DefaultHandSlotCount : data.unlockedHandSlotCount,
                data.unlockedSupplySlotCount <= 0 ? PlayerEquipmentState.DefaultSupplySlotCount : data.unlockedSupplySlotCount,
                (EquipmentItemKind)data.activeProtectiveItemKind,
                data.activeDamageReductionPercent,
                data.strengthEnhancerRemainingSeconds,
                data.strengthDamageBonusPercent,
                data.flashlightRemainingSeconds,
                data.electricBatonChargeCooldownSeconds,
                data.miniFlamethrowerContinuousHitSeconds,
                data.miniFlamethrowerHitGapSeconds);
        }

        private static EquipmentSlotState[] ToSlots(EquipmentSlotSaveData[] data)
        {
            if (data == null || data.Length == 0)
            {
                return new EquipmentSlotState[0];
            }

            var slots = new EquipmentSlotState[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                slots[i] = data[i] == null
                    ? EquipmentSlotState.Empty
                    : new EquipmentSlotState(
                        (EquipmentItemKind)data[i].itemKind,
                        data[i].count,
                        data[i].durabilityPercent,
                        data[i].purchasePriceCredits);
            }

            return slots;
        }

        private static ReputationState ToReputation(ReputationSaveData data)
        {
            return data == null
                ? ReputationState.Default
                : new ReputationState(
                    data.fameScore,
                    data.associationFameScore,
                    data.hasUsedRevivalContract);
        }

        private static PersonalCargoHoldState ToPersonalCargoHold(PersonalCargoHoldSaveData data)
        {
            if (data == null || data.items == null || data.items.Length == 0)
            {
                return PersonalCargoHoldState.Empty;
            }

            var items = new PersonalCargoItemState[data.items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                var item = data.items[i];
                items[i] = item == null
                    ? new PersonalCargoItemState(
                        "unknown-" + i,
                        "Unknown Cargo",
                        CargoGrade.Common,
                        1,
                        0,
                        PlanetTrait.CommonMineralRich,
                        1f)
                    : new PersonalCargoItemState(
                        item.id,
                        item.displayName,
                        (CargoGrade)item.grade,
                        item.sizeUnits,
                        item.baseSaleValue,
                        (PlanetTrait)item.originTrait,
                        item.durabilityPercent);
            }

            return new PersonalCargoHoldState(items);
        }

        private static ShipUpgradeState ToShipUpgrade(ShipUpgradeSaveData data)
        {
            if (data == null)
            {
                return ShipUpgradeState.Empty;
            }

            var appearance = data.appearance == null
                ? ShipAppearanceCustomizationState.Default
                : new ShipAppearanceCustomizationState(
                    data.appearance.hullPaintSlotId,
                    data.appearance.emblemSlotId,
                    data.appearance.nameplateSlotId);
            return new ShipUpgradeState(
                data.durabilityPurchasedTier,
                data.durabilityEquippedTier,
                data.weaponSystemsPurchasedTier,
                data.weaponSystemsEquippedTier,
                data.autoPilotPurchasedTier,
                data.autoPilotEquippedTier,
                data.supplySlotsPurchasedTier,
                data.supplySlotsEquippedTier,
                data.internalControlPurchasedTier,
                data.internalControlEquippedTier,
                appearance);
        }

        private static TransportContractDefinition[] ToContracts(TransportContractSaveData[] data)
        {
            if (data == null || data.Length == 0)
            {
                return new TransportContractDefinition[0];
            }

            var contracts = new TransportContractDefinition[data.Length];
            for (var i = 0; i < data.Length; i++)
            {
                contracts[i] = data[i] == null ? TransportContractDefinition.CreateTutorial() : ToContract(data[i]);
            }

            return contracts;
        }

        private static TransportContractDefinition ToContract(TransportContractSaveData data)
        {
            return new TransportContractDefinition(
                data.id,
                data.displayName,
                data.transportTargetName,
                (ContractType)data.contractType,
                (ContractDifficulty)data.difficulty,
                data.durationSeconds,
                data.rewardCredits,
                ToCargo(data.cargo),
                data.isTutorial,
                data.requiredCargoHoldScore,
                data.isRevivalContract,
                (PlanetTrait)data.originTrait,
                (PlanetTrait)data.destinationTrait);
        }

        private static CargoState ToCargo(CargoSaveData data)
        {
            return data == null
                ? new CargoState(CargoGrade.Common, 1, 0, 1f, false)
                : new CargoState(
                    (CargoGrade)data.grade,
                    data.sizeUnits <= 0 ? 1 : data.sizeUnits,
                    data.baseValue,
                    data.durabilityPercent,
                    data.isPersonalCargo);
        }

        private static TransportHazardUnlockState ToHazardUnlock(TransportHazardUnlockSaveData data)
        {
            return data == null
                ? TransportHazardUnlockState.None
                : new TransportHazardUnlockState(
                    data.cargoFreedomLeagueUnlocked,
                    data.spacePirateUnlocked,
                    data.alienLifeUnlocked,
                    data.concealedBlackHoleUnlocked);
        }

        private static SpecialContractProgressState ToSpecialContracts(SpecialContractProgressSaveData data)
        {
            if (data == null)
            {
                return SpecialContractProgressState.Empty;
            }

            var unlocks = data.equipmentUnlocks == null
                ? SpecialEquipmentUnlockState.None
                : new SpecialEquipmentUnlockState(
                    data.equipmentUnlocks.presenceDetectorUnlocked,
                    data.equipmentUnlocks.lightBladeUnlocked,
                    data.equipmentUnlocks.electricMineUnlocked,
                    data.equipmentUnlocks.corridorPurifierUnlocked);
            return new SpecialContractProgressState(
                (SpecialContractKind)data.activeContractKind,
                unlocks,
                data.organicRichPlanetVisits,
                data.volcanicActivePlanetVisits,
                data.commonMineralRichPlanetVisits,
                data.rareMineralRichPlanetVisits,
                data.veryHardContractCompletions,
                data.resistanceNeutralizedCount,
                data.revolutionNeutralizedCount,
                data.istanteNeutralizedCount,
                data.ataNeutralizedCount,
                data.monstrumNeutralizedCount,
                data.doloreNeutralizedCount,
                data.resistanceChipCount,
                data.revolutionChipCount,
                data.istantePowerCoreCount,
                data.ataControlModuleCount,
                data.corridorPurifierInstalled,
                data.corridorPurifierChargeCount);
        }
    }

    public interface ISaveGameRepository
    {
        bool Exists(string slotId);

        void Save(string slotId, SaveGameDocument document);

        bool TryLoad(string slotId, out SaveGameDocument document);

        void Delete(string slotId);
    }

    public sealed class SaveGameService
    {
        public const string DefaultSlotId = "default";

        private readonly ISaveGameRepository repository;

        public SaveGameService(ISaveGameRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public void Save(string slotId, NewGameStartFlowState flow, GameSettingsState settings)
        {
            repository.Save(slotId, SaveGameMapper.CreateDocument(flow, settings));
        }

        public bool TryLoad(string slotId, out NewGameStartFlowState flow, out GameSettingsState settings)
        {
            SaveGameDocument document;
            if (!repository.TryLoad(slotId, out document))
            {
                flow = null;
                settings = GameSettingsState.Default;
                return false;
            }

            flow = SaveGameMapper.ToStartFlow(document);
            settings = SaveGameMapper.ToSettings(document);
            return true;
        }

        public bool TryCreateNewGameFromProfile(string slotId, out NewGameStartFlowState flow)
        {
            SaveGameDocument document;
            if (!repository.TryLoad(slotId, out document))
            {
                flow = null;
                return false;
            }

            flow = SaveGameMapper.CreateNewGameFromProfile(document);
            return true;
        }
    }

    public sealed class InMemorySaveGameRepository : ISaveGameRepository
    {
        private readonly Dictionary<string, string> saves = new Dictionary<string, string>();

        public bool Exists(string slotId)
        {
            return saves.ContainsKey(NormalizeSlotId(slotId));
        }

        public void Save(string slotId, SaveGameDocument document)
        {
            saves[NormalizeSlotId(slotId)] = SaveGameSerializer.ToJson(document);
        }

        public bool TryLoad(string slotId, out SaveGameDocument document)
        {
            string json;
            if (!saves.TryGetValue(NormalizeSlotId(slotId), out json))
            {
                document = null;
                return false;
            }

            document = SaveGameSerializer.FromJson(json);
            return true;
        }

        public void Delete(string slotId)
        {
            saves.Remove(NormalizeSlotId(slotId));
        }

        private static string NormalizeSlotId(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                return SaveGameService.DefaultSlotId;
            }

            return slotId.Trim();
        }
    }

    public sealed class FileSaveGameRepository : ISaveGameRepository
    {
        private const string FileExtension = ".json";
        private readonly string rootDirectory;

        public FileSaveGameRepository(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Save root directory is required.", nameof(rootDirectory));
            }

            this.rootDirectory = rootDirectory;
        }

        public static FileSaveGameRepository CreateDefault()
        {
            return new FileSaveGameRepository(Path.Combine(Application.persistentDataPath, "Saves"));
        }

        public bool Exists(string slotId)
        {
            return File.Exists(GetPath(slotId));
        }

        public void Save(string slotId, SaveGameDocument document)
        {
            Directory.CreateDirectory(rootDirectory);
            File.WriteAllText(GetPath(slotId), SaveGameSerializer.ToJson(document), Encoding.UTF8);
        }

        public bool TryLoad(string slotId, out SaveGameDocument document)
        {
            var path = GetPath(slotId);
            if (!File.Exists(path))
            {
                document = null;
                return false;
            }

            document = SaveGameSerializer.FromJson(File.ReadAllText(path, Encoding.UTF8));
            return true;
        }

        public void Delete(string slotId)
        {
            var path = GetPath(slotId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string GetPath(string slotId)
        {
            return Path.Combine(rootDirectory, SanitizeSlotId(slotId) + FileExtension);
        }

        private static string SanitizeSlotId(string slotId)
        {
            var value = string.IsNullOrWhiteSpace(slotId)
                ? SaveGameService.DefaultSlotId
                : slotId.Trim();
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                builder.Append(Array.IndexOf(invalid, value[i]) >= 0 ? '_' : value[i]);
            }

            return builder.ToString();
        }
    }

    public sealed class PlatformCloudSaveGameRepository : ISaveGameRepository
    {
        private const string FilePrefix = "saves/";
        private const string FileExtension = ".json";
        private readonly IPlatformCloudSaveServices cloud;

        public PlatformCloudSaveGameRepository(IPlatformCloudSaveServices cloud)
        {
            this.cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
        }

        public bool Exists(string slotId)
        {
            return cloud.Exists(GetPath(slotId));
        }

        public void Save(string slotId, SaveGameDocument document)
        {
            cloud.WriteFile(
                GetPath(slotId),
                Encoding.UTF8.GetBytes(SaveGameSerializer.ToJson(document)));
        }

        public bool TryLoad(string slotId, out SaveGameDocument document)
        {
            byte[] bytes;
            if (!cloud.TryReadFile(GetPath(slotId), out bytes))
            {
                document = null;
                return false;
            }

            document = SaveGameSerializer.FromJson(Encoding.UTF8.GetString(bytes));
            return true;
        }

        public void Delete(string slotId)
        {
            cloud.DeleteFile(GetPath(slotId));
        }

        private static string GetPath(string slotId)
        {
            return FilePrefix +
                   (string.IsNullOrWhiteSpace(slotId) ? SaveGameService.DefaultSlotId : slotId.Trim()) +
                   FileExtension;
        }
    }
}
