using System;
using System.IO;
using Bellerophon.Core.Session;
using Bellerophon.Platform;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class SaveGamePersistenceTests
    {
        [Test]
        public void SaveGameService_RoundTripsDetailedSessionAndSettings()
        {
            var repository = new InMemorySaveGameRepository();
            var service = new SaveGameService(repository);
            var source = CreateDetailedCompletedFlow();
            var settings = new GameSettingsState(
                2560,
                1440,
                false,
                0.8f,
                0.45f,
                0.7f,
                0.2f,
                true,
                true);

            service.Save("roundtrip", source, settings);

            NewGameStartFlowState loaded;
            GameSettingsState loadedSettings;
            Assert.That(service.TryLoad("roundtrip", out loaded, out loadedSettings), Is.True);
            Assert.That(loaded.HasCompletedTutorialBefore, Is.True);
            Assert.That(loaded.AvailableContractCount, Is.EqualTo(2));
            Assert.That(loaded.Session.Wallet.Credits, Is.EqualTo(source.Session.Wallet.Credits));
            Assert.That(loaded.Session.Ship.GetRoom(ShipRoomId.EngineRoom).CurrentDurability, Is.EqualTo(63));
            Assert.That(loaded.Session.Ship.GetRoom(ShipRoomId.ControlRoom).IsFunctionOffline, Is.True);
            Assert.That(loaded.Session.Equipment.GetHandSlot(1).ItemKind, Is.EqualTo(EquipmentItemKind.Musket));
            Assert.That(loaded.Session.Equipment.GetSupplySlot(0).ItemKind, Is.EqualTo(EquipmentItemKind.PresenceDetector));
            Assert.That(loaded.Session.PersonalCargoHold.GetCargo(0).OriginTrait, Is.EqualTo(PlanetTrait.RareMineralRich));
            Assert.That(loaded.Session.ShipUpgrades.SupplySlotsEquippedTier, Is.EqualTo(1));
            Assert.That(loaded.Session.Reputation.FameScore, Is.EqualTo(2400));
            Assert.That(loaded.Session.SpecialContracts.ActiveContractKind, Is.EqualTo(SpecialContractKind.ElectricMineUnlock));
            Assert.That(loaded.Session.SpecialContracts.EquipmentUnlocks.ElectricMineUnlocked, Is.False);
            Assert.That(loadedSettings.ResolutionWidth, Is.EqualTo(2560));
            Assert.That(loadedSettings.MouseSensitivity, Is.EqualTo(0.2f));
            Assert.That(loadedSettings.HighContrastUi, Is.True);
        }

        [Test]
        public void SavedProfile_StartsNewGameWithTutorialSkipAndGrantsElevenHundred()
        {
            var repository = new InMemorySaveGameRepository();
            var completed = CreateCompletedTutorialFlow();
            repository.Save("profile", SaveGameMapper.CreateDocument(completed, GameSettingsState.Default));

            SaveGameDocument document;
            Assert.That(repository.TryLoad("profile", out document), Is.True);

            var newGame = SaveGameMapper.CreateNewGameFromProfile(document)
                .MoveAssociationContractToBottom()
                .AcceptAssociationContract();

            Assert.That(newGame.HasCompletedTutorialBefore, Is.True);
            Assert.That(newGame.CanSkipTutorial, Is.True);

            var skipped = newGame.SkipTutorialForReturningPlayer();
            Assert.That(skipped.Session.Wallet.Credits, Is.EqualTo(1100));
            Assert.That(skipped.Session.Wallet.Credits, Is.EqualTo(NewGameStartFlowState.TutorialSkipRewardCredits));
            Assert.That(skipped.AvailableContractCount, Is.EqualTo(2));
        }

        [Test]
        public void SaveGameMigration_FillsVersionSettingsAndDefaultSession()
        {
            var legacy = new SaveGameDocument
            {
                version = 0,
                startFlow = new GameStartFlowSaveData
                {
                    phase = (int)NewGameStartFlowPhase.ContractPrompt,
                    hasCompletedTutorialBefore = true
                }
            };

            var migrated = SaveGameMigration.Migrate(legacy);
            var settings = SaveGameMapper.ToSettings(migrated);
            var profileNewGame = SaveGameMapper.CreateNewGameFromProfile(migrated);

            Assert.That(migrated.version, Is.EqualTo(SaveGameDocument.CurrentVersion));
            Assert.That(migrated.startFlow.session, Is.Not.Null);
            Assert.That(settings.ResolutionWidth, Is.EqualTo(1920));
            Assert.That(settings.MouseSensitivity, Is.EqualTo(0.12f));
            Assert.That(profileNewGame.HasCompletedTutorialBefore, Is.True);
        }

        [Test]
        public void FileRepository_SavesLoadsAndDeletesJsonSlot()
        {
            var directory = Path.Combine(Path.GetTempPath(), "BellerophonSaveTests", Guid.NewGuid().ToString("N"));
            var repository = new FileSaveGameRepository(directory);
            try
            {
                var document = SaveGameMapper.CreateDocument(CreateCompletedTutorialFlow(), GameSettingsState.Default);
                repository.Save("slot:one", document);

                SaveGameDocument loaded;
                Assert.That(repository.Exists("slot:one"), Is.True);
                Assert.That(repository.TryLoad("slot:one", out loaded), Is.True);
                Assert.That(SaveGameMapper.ToStartFlow(loaded).HasCompletedTutorialBefore, Is.True);

                repository.Delete("slot:one");
                Assert.That(repository.Exists("slot:one"), Is.False);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public void NullPlatformServices_ExposeAchievementCloudAndStatsBoundaries()
        {
            var platform = new NullPlatformServices();
            platform.UnlockAchievement("tutorial-complete");
            platform.Stats.SetIntStat("completed_transports", 3);

            int completedTransportCount;
            Assert.That(platform.Achievements.ProviderName, Is.EqualTo("Null"));
            Assert.That(platform.Achievements.IsAchievementUnlocked("tutorial-complete"), Is.True);
            Assert.That(platform.Cloud.ProviderName, Is.EqualTo("Null"));
            Assert.That(platform.Cloud.SupportsRemoteSync, Is.False);
            Assert.That(platform.Stats.TryGetIntStat("completed_transports", out completedTransportCount), Is.True);
            Assert.That(completedTransportCount, Is.EqualTo(3));

            var repository = new PlatformCloudSaveGameRepository(platform.Cloud);
            repository.Save("cloud-slot", SaveGameMapper.CreateDocument(CreateCompletedTutorialFlow(), GameSettingsState.Default));
            SaveGameDocument loaded;
            Assert.That(repository.TryLoad("cloud-slot", out loaded), Is.True);
            Assert.That(SaveGameMapper.CreateNewGameFromProfile(loaded).HasCompletedTutorialBefore, Is.True);
        }

        private static NewGameStartFlowState CreateDetailedCompletedFlow()
        {
            var flow = CreateCompletedTutorialFlow();
            var ship = flow.Session.Ship
                .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(63, 100))
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(88, 100, true));
            var equipment = new PlayerEquipmentState(
                true,
                new[]
                {
                    EquipmentSlotState.One(EquipmentItemKind.Stick),
                    EquipmentSlotState.Purchased(EquipmentItemKind.Musket, 450),
                    EquipmentSlotState.Empty,
                    EquipmentSlotState.Empty
                },
                new[]
                {
                    EquipmentSlotState.Purchased(EquipmentItemKind.PresenceDetector, 300),
                    EquipmentSlotState.One(EquipmentItemKind.BandageSet),
                    EquipmentSlotState.Empty
                },
                1,
                0.5f,
                EquipmentUseMode.PrecisionAim,
                "Saved equipment state.",
                PlayerEquipmentState.DefaultHandSlotCount,
                PlayerEquipmentState.DefaultSupplySlotCount,
                EquipmentItemKind.BasicProtectiveSuit,
                10,
                4f,
                25,
                3f,
                2f,
                1f,
                0.25f);
            var cargo = new PersonalCargoHoldState(new[]
            {
                new PersonalCargoItemState(
                    "rare-ore-001",
                    "Rare ore",
                    CargoGrade.Rare,
                    2,
                    320,
                    PlanetTrait.RareMineralRich,
                    0.75f)
            });
            var upgrades = new ShipUpgradeState(
                1,
                1,
                1,
                0,
                1,
                1,
                1,
                1,
                0,
                0,
                new ShipAppearanceCustomizationState("red-hull", "association-emblem", "bellerophon"));
            var special = new SpecialContractProgressState(
                SpecialContractKind.ElectricMineUnlock,
                new SpecialEquipmentUnlockState(true, true, false, false),
                2,
                1,
                3,
                1,
                2,
                2,
                1,
                0,
                0,
                0,
                0,
                2,
                1,
                0,
                0,
                false,
                0);

            return flow.WithSession(
                flow.Session
                    .WithShipState(ship)
                    .WithEquipment(equipment)
                    .WithPersonalCargoHold(cargo)
                    .WithShipUpgrades(upgrades)
                    .WithReputation(new ReputationState(2400, 900, true))
                    .WithSpecialContracts(special));
        }

        private static NewGameStartFlowState CreateCompletedTutorialFlow()
        {
            var tutorial = NewGameStartFlowState.CreateNewGame()
                .MoveAssociationContractToBottom()
                .AcceptAssociationContract()
                .AcceptTutorialContract();
            var contract = tutorial.Session.ActiveTransportContract.Value;
            var completed = tutorial.Session.CompleteTransport(new SettlementInput(
                contract.ContractType,
                contract.Difficulty,
                contract.Cargo,
                tutorial.Session.Ship,
                new CrewState(1, 0),
                tutorial.Session.Wallet,
                contractBasePay: contract.RewardCredits,
                repairSupportAmount: NewGameStartFlowState.TutorialSkipRepairSupportCredits));
            return tutorial
                .WithSession(completed)
                .PreparePostTransportContracts();
        }
    }
}
