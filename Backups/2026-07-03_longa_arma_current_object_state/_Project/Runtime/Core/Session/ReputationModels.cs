using System;

namespace Bellerophon.Core.Session
{
    public readonly struct ReputationState
    {
        public ReputationState(
            int fameScore,
            int associationFameScore,
            bool hasUsedRevivalContract)
        {
            FameScore = fameScore;
            AssociationFameScore = associationFameScore;
            HasUsedRevivalContract = hasUsedRevivalContract;
        }

        public int FameScore { get; }

        public int AssociationFameScore { get; }

        public bool HasUsedRevivalContract { get; }

        public static ReputationState Default => new ReputationState(0, 0, false);

        public ReputationState WithScores(int fameScore, int associationFameScore)
        {
            return new ReputationState(fameScore, associationFameScore, HasUsedRevivalContract);
        }

        public ReputationState MarkRevivalContractUsed()
        {
            return new ReputationState(FameScore, AssociationFameScore, true);
        }
    }

    public readonly struct ReputationChangeResult
    {
        public ReputationChangeResult(
            int fameDelta,
            int associationFameDelta,
            bool resetsFameToZero,
            bool marksRevivalContractUsed)
        {
            FameDelta = fameDelta;
            AssociationFameDelta = associationFameDelta;
            ResetsFameToZero = resetsFameToZero;
            MarksRevivalContractUsed = marksRevivalContractUsed;
        }

        public int FameDelta { get; }

        public int AssociationFameDelta { get; }

        public bool ResetsFameToZero { get; }

        public bool MarksRevivalContractUsed { get; }
    }

    public static class ReputationRules
    {
        public static ReputationChangeResult CalculateContractResult(
            TransportContractDefinition contract,
            bool isAssociationMember,
            bool isTransportCompleted,
            int deadCrewCount,
            float cargoLossPercent)
        {
            var cleanSuccess = isTransportCompleted &&
                               deadCrewCount <= 0 &&
                               cargoLossPercent <= 0f;
            var baseDelta = GetDifficultyDelta(contract.Difficulty, cleanSuccess);
            var associationDelta = 0;
            if (contract.ContractType == ContractType.Association)
            {
                if (isAssociationMember)
                {
                    associationDelta = baseDelta;
                }
                else if (cleanSuccess)
                {
                    associationDelta = RoundInt(baseDelta * 0.5f);
                }
            }

            return new ReputationChangeResult(
                baseDelta,
                associationDelta,
                contract.IsRevivalContract && cleanSuccess,
                contract.IsRevivalContract);
        }

        public static ReputationState ApplyChange(
            ReputationState state,
            ReputationChangeResult change)
        {
            var fameScore = change.ResetsFameToZero
                ? 0
                : state.FameScore + change.FameDelta;
            var associationFameScore = state.AssociationFameScore + change.AssociationFameDelta;
            return new ReputationState(
                fameScore,
                associationFameScore,
                state.HasUsedRevivalContract || change.MarksRevivalContractUsed);
        }

        public static int GetDifficultyDelta(ContractDifficulty difficulty, bool success)
        {
            switch (difficulty)
            {
                case ContractDifficulty.Intro:
                    return success ? 10 : -15;
                case ContractDifficulty.VeryEasy:
                    return success ? 20 : -30;
                case ContractDifficulty.Easy:
                    return success ? 50 : -80;
                case ContractDifficulty.Normal:
                    return success ? 120 : -220;
                case ContractDifficulty.Hard:
                    return success ? 300 : -600;
                case ContractDifficulty.VeryHard:
                    return success ? 750 : -1500;
                case ContractDifficulty.Master:
                    return success ? 3000 : -2000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
            }
        }

        private static int RoundInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
