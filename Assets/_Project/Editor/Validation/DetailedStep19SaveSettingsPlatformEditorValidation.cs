using System;
using Bellerophon.Core.Session;
using Bellerophon.Platform;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep19SaveSettingsPlatformEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 19 save settings platform editor validation passed.");
            Debug.Log("Detailed step 19 save settings platform validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var repository = new InMemorySaveGameRepository();
            var service = new SaveGameService(repository);
            var completedFlow = CreateCompletedTutorialFlow();
            var settings = new GameSettingsState(
                1600,
                900,
                false,
                0.6f,
                0.5f,
                0.7f,
                0.18f,
                true,
                false);

            service.Save("step19", completedFlow, settings);

            NewGameStartFlowState loaded;
            GameSettingsState loadedSettings;
            if (!service.TryLoad("step19", out loaded, out loadedSettings))
            {
                throw new InvalidOperationException("Saved game must load from the in-memory repository.");
            }

            var profileNewGame = SaveGameMapper.CreateNewGameFromProfile(
                    SaveGameMigration.Migrate(SaveGameMapper.CreateDocument(loaded, loadedSettings)))
                .MoveAssociationContractToBottom()
                .AcceptAssociationContract();
            var skipped = profileNewGame.SkipTutorialForReturningPlayer();
            if (!loaded.HasCompletedTutorialBefore ||
                loadedSettings.ResolutionWidth != 1600 ||
                !profileNewGame.CanSkipTutorial ||
                skipped.Session.Wallet.Credits != NewGameStartFlowState.TutorialSkipRewardCredits)
            {
                throw new InvalidOperationException("Save/load must restore tutorial skip profile and settings.");
            }

            var legacy = SaveGameMigration.Migrate(new SaveGameDocument
            {
                version = 0,
                startFlow = new GameStartFlowSaveData
                {
                    hasCompletedTutorialBefore = true
                }
            });
            if (legacy.version != SaveGameDocument.CurrentVersion ||
                SaveGameMapper.CreateNewGameFromProfile(legacy).HasCompletedTutorialBefore == false)
            {
                throw new InvalidOperationException("Legacy save migration must normalize version and profile data.");
            }

            var platform = new NullPlatformServices();
            platform.UnlockAchievement("tutorial-complete");
            platform.Stats.SetIntStat("completed_transports", loaded.Session.CompletedTransportCount);
            var cloudRepository = new PlatformCloudSaveGameRepository(platform.Cloud);
            cloudRepository.Save("step19-cloud", SaveGameMapper.CreateDocument(loaded, loadedSettings));
            SaveGameDocument cloudLoaded;
            int completedTransportCount;
            if (!platform.Achievements.IsAchievementUnlocked("tutorial-complete") ||
                !platform.Stats.TryGetIntStat("completed_transports", out completedTransportCount) ||
                completedTransportCount <= 0 ||
                !cloudRepository.TryLoad("step19-cloud", out cloudLoaded) ||
                platform.Cloud.SupportsRemoteSync)
            {
                throw new InvalidOperationException("Platform boundaries must expose achievement, cloud, and stat interfaces without Steam SDK sync.");
            }

            return "Version=" + SaveGameDocument.CurrentVersion +
                   "; TutorialProfile=" + loaded.HasCompletedTutorialBefore +
                   "; SkipCredits=" + skipped.Session.Wallet.Credits +
                   "; Settings=" + loadedSettings.ResolutionWidth + "x" + loadedSettings.ResolutionHeight +
                   "; CloudRemoteSync=" + platform.Cloud.SupportsRemoteSync +
                   "; StatCompleted=" + completedTransportCount;
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
