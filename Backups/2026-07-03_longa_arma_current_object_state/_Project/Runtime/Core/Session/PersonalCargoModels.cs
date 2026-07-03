using System;

namespace Bellerophon.Core.Session
{
    public readonly struct PersonalCargoItemState
    {
        public PersonalCargoItemState(
            string id,
            string displayName,
            CargoGrade grade,
            int sizeUnits,
            int baseSaleValue,
            PlanetTrait originTrait,
            float durabilityPercent)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Personal cargo id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Personal cargo display name is required.", nameof(displayName));
            }

            if (sizeUnits <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeUnits), "Personal cargo size must be positive.");
            }

            if (baseSaleValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseSaleValue), "Personal cargo sale value cannot be negative.");
            }

            Id = id;
            DisplayName = displayName;
            Grade = grade;
            SizeUnits = sizeUnits;
            BaseSaleValue = baseSaleValue;
            OriginTrait = originTrait;
            DurabilityPercent = Clamp01(durabilityPercent);
        }

        public string Id { get; }

        public string DisplayName { get; }

        public CargoGrade Grade { get; }

        public int SizeUnits { get; }

        public int BaseSaleValue { get; }

        public PlanetTrait OriginTrait { get; }

        public float DurabilityPercent { get; }

        public float LossPercent => 1f - DurabilityPercent;

        public PersonalCargoItemState WithDurabilityPercent(float durabilityPercent)
        {
            return new PersonalCargoItemState(
                Id,
                DisplayName,
                Grade,
                SizeUnits,
                BaseSaleValue,
                OriginTrait,
                durabilityPercent);
        }

        public PersonalCargoItemState WithDamagePercent(float damagePercent)
        {
            if (damagePercent < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damagePercent), "Personal cargo damage cannot be negative.");
            }

            return WithDurabilityPercent(DurabilityPercent - damagePercent);
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

    public sealed class PersonalCargoHoldState
    {
        private static readonly PersonalCargoItemState[] EmptyItems = new PersonalCargoItemState[0];
        private readonly PersonalCargoItemState[] items;

        public PersonalCargoHoldState(PersonalCargoItemState[] cargoItems)
        {
            items = cargoItems == null || cargoItems.Length == 0
                ? EmptyItems
                : (PersonalCargoItemState[])cargoItems.Clone();
        }

        public static PersonalCargoHoldState Empty => new PersonalCargoHoldState(EmptyItems);

        public PersonalCargoItemState[] Items => items == null || items.Length == 0
            ? EmptyItems
            : (PersonalCargoItemState[])items.Clone();

        public int Count => items == null ? 0 : items.Length;

        public bool HasCargo => Count > 0;

        public int UsedSizeUnits
        {
            get
            {
                var total = 0;
                var current = items ?? EmptyItems;
                for (var i = 0; i < current.Length; i++)
                {
                    total += current[i].SizeUnits;
                }

                return total;
            }
        }

        public PersonalCargoItemState GetCargo(int index)
        {
            var current = items ?? EmptyItems;
            if (index < 0 || index >= current.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return current[index];
        }

        public PersonalCargoHoldState WithCargoAdded(PersonalCargoItemState cargo)
        {
            var current = items ?? EmptyItems;
            var next = new PersonalCargoItemState[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = cargo;
            return new PersonalCargoHoldState(next);
        }

        public PersonalCargoHoldState WithoutCargoAt(int index)
        {
            var current = items ?? EmptyItems;
            if (index < 0 || index >= current.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            if (current.Length == 1)
            {
                return Empty;
            }

            var next = new PersonalCargoItemState[current.Length - 1];
            var writeIndex = 0;
            for (var i = 0; i < current.Length; i++)
            {
                if (i == index)
                {
                    continue;
                }

                next[writeIndex] = current[i];
                writeIndex++;
            }

            return new PersonalCargoHoldState(next);
        }

        public PersonalCargoHoldState WithDamagePercent(float damagePercent)
        {
            if (damagePercent < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damagePercent), "Personal cargo damage cannot be negative.");
            }

            if (damagePercent <= 0f || Count == 0)
            {
                return this;
            }

            var current = items ?? EmptyItems;
            var next = new PersonalCargoItemState[current.Length];
            for (var i = 0; i < current.Length; i++)
            {
                next[i] = current[i].WithDamagePercent(damagePercent);
            }

            return new PersonalCargoHoldState(next);
        }
    }

    public readonly struct PersonalCargoSaleQuote
    {
        public PersonalCargoSaleQuote(
            int salePrice,
            int traitModifierPercent,
            PlanetTrait destinationTrait,
            float durabilityMultiplier)
        {
            SalePrice = salePrice < 0 ? 0 : salePrice;
            TraitModifierPercent = traitModifierPercent;
            DestinationTrait = destinationTrait;
            DurabilityMultiplier = durabilityMultiplier < 0f ? 0f : durabilityMultiplier;
        }

        public int SalePrice { get; }

        public int TraitModifierPercent { get; }

        public PlanetTrait DestinationTrait { get; }

        public float DurabilityMultiplier { get; }
    }

    public readonly struct PersonalCargoCollectionResult
    {
        public PersonalCargoCollectionResult(
            bool collected,
            GameSessionState state,
            PersonalCargoItemState cargo,
            string summary)
        {
            Collected = collected;
            State = state;
            Cargo = cargo;
            Summary = summary ?? string.Empty;
        }

        public bool Collected { get; }

        public GameSessionState State { get; }

        public PersonalCargoItemState Cargo { get; }

        public string Summary { get; }
    }

    public readonly struct PersonalCargoSaleResult
    {
        public PersonalCargoSaleResult(
            bool sold,
            GameSessionState state,
            PersonalCargoItemState cargo,
            PersonalCargoSaleQuote quote,
            string summary)
        {
            Sold = sold;
            State = state;
            Cargo = cargo;
            Quote = quote;
            Summary = summary ?? string.Empty;
        }

        public bool Sold { get; }

        public GameSessionState State { get; }

        public PersonalCargoItemState Cargo { get; }

        public PersonalCargoSaleQuote Quote { get; }

        public string Summary { get; }
    }

    public static class PersonalCargoRules
    {
        public const int FullCargoHoldCapacityUnits = 300;
        public const int CommonCargoSizeUnits = 50;
        public const int RareCargoSizeUnits = 100;
        public const int PremiumCargoSizeUnits = 200;
        public const int CommonBaseSaleValue = 100;
        public const int RareBaseSaleValue = 250;
        public const int PremiumBaseSaleValue = 600;
        public const int SameTraitSalePenaltyPercent = -50;

        public static int CalculateCapacityUnits(ShipState ship)
        {
            return RoundToInt(FullCargoHoldCapacityUnits * ShipStateRules.CalculateCargoHoldScore(ship));
        }

        public static int CalculateAvailableUnits(ShipState ship, PersonalCargoHoldState hold)
        {
            var used = hold == null ? 0 : hold.UsedSizeUnits;
            return Math.Max(0, CalculateCapacityUnits(ship) - used);
        }

        public static bool CanAddCargo(ShipState ship, PersonalCargoHoldState hold, PersonalCargoItemState cargo)
        {
            return ShipStateRules.CanTransportPersonalCargo(ship) &&
                   CalculateAvailableUnits(ship, hold) >= cargo.SizeUnits;
        }

        public static PersonalCargoItemState CreateCollectedCargo(PlanetTrait originTrait, int collectionSeed)
        {
            var grade = RollGrade(collectionSeed);
            return new PersonalCargoItemState(
                CreateCargoId(originTrait, collectionSeed),
                FormatTraitName(originTrait) + " " + FormatGradeName(grade) + " Cargo",
                grade,
                GetSizeUnits(grade),
                GetBaseSaleValue(grade),
                originTrait,
                1f);
        }

        public static CargoGrade RollGrade(int seed)
        {
            var normalizedSeed = seed == int.MinValue ? 0 : Math.Abs(seed);
            var roll = normalizedSeed % 100;
            if (roll < 60)
            {
                return CargoGrade.Common;
            }

            return roll < 90 ? CargoGrade.Rare : CargoGrade.Premium;
        }

        public static int GetSizeUnits(CargoGrade grade)
        {
            switch (grade)
            {
                case CargoGrade.Common:
                    return CommonCargoSizeUnits;
                case CargoGrade.Rare:
                    return RareCargoSizeUnits;
                case CargoGrade.Premium:
                    return PremiumCargoSizeUnits;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
            }
        }

        public static int GetBaseSaleValue(CargoGrade grade)
        {
            switch (grade)
            {
                case CargoGrade.Common:
                    return CommonBaseSaleValue;
                case CargoGrade.Rare:
                    return RareBaseSaleValue;
                case CargoGrade.Premium:
                    return PremiumBaseSaleValue;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
            }
        }

        public static PersonalCargoSaleQuote CalculateSaleQuote(
            PersonalCargoItemState cargo,
            PlanetTrait destinationTrait)
        {
            var traitModifierPercent = GetTraitModifierPercent(cargo.OriginTrait, destinationTrait);
            var traitMultiplier = Math.Max(0f, 1f + (traitModifierPercent / 100f));
            var salePrice = RoundToInt(cargo.BaseSaleValue * traitMultiplier * cargo.DurabilityPercent);
            return new PersonalCargoSaleQuote(
                salePrice,
                traitModifierPercent,
                destinationTrait,
                cargo.DurabilityPercent);
        }

        public static int GetTraitModifierPercent(PlanetTrait originTrait, PlanetTrait destinationTrait)
        {
            if (originTrait == destinationTrait)
            {
                return SameTraitSalePenaltyPercent;
            }

            switch (originTrait)
            {
                case PlanetTrait.WaterRich:
                    return GetWaterRichModifier(destinationTrait);
                case PlanetTrait.OrganicRich:
                    return GetOrganicRichModifier(destinationTrait);
                case PlanetTrait.CommonMineralRich:
                    return GetCommonMineralModifier(destinationTrait);
                case PlanetTrait.RareMineralRich:
                    return GetRareMineralModifier(destinationTrait);
                case PlanetTrait.WoodRich:
                    return GetWoodRichModifier(destinationTrait);
                case PlanetTrait.VolcanicActive:
                    return GetVolcanicModifier(destinationTrait);
                default:
                    throw new ArgumentOutOfRangeException(nameof(originTrait), originTrait, null);
            }
        }

        public static string FormatTraitName(PlanetTrait trait)
        {
            switch (trait)
            {
                case PlanetTrait.WaterRich:
                    return "Water Rich";
                case PlanetTrait.CommonMineralRich:
                    return "Common Mineral";
                case PlanetTrait.OrganicRich:
                    return "Organic";
                case PlanetTrait.RareMineralRich:
                    return "Rare Mineral";
                case PlanetTrait.WoodRich:
                    return "Wood Rich";
                case PlanetTrait.VolcanicActive:
                    return "Volcanic";
                default:
                    throw new ArgumentOutOfRangeException(nameof(trait), trait, null);
            }
        }

        private static int GetWaterRichModifier(PlanetTrait destinationTrait)
        {
            switch (destinationTrait)
            {
                case PlanetTrait.OrganicRich:
                    return 50;
                case PlanetTrait.CommonMineralRich:
                    return 70;
                case PlanetTrait.RareMineralRich:
                    return 80;
                case PlanetTrait.WoodRich:
                    return 25;
                case PlanetTrait.VolcanicActive:
                    return 100;
                default:
                    return SameTraitSalePenaltyPercent;
            }
        }

        private static int GetOrganicRichModifier(PlanetTrait destinationTrait)
        {
            switch (destinationTrait)
            {
                case PlanetTrait.WaterRich:
                    return 25;
                case PlanetTrait.CommonMineralRich:
                    return 80;
                case PlanetTrait.RareMineralRich:
                    return 100;
                case PlanetTrait.WoodRich:
                    return 10;
                case PlanetTrait.VolcanicActive:
                    return 50;
                default:
                    return SameTraitSalePenaltyPercent;
            }
        }

        private static int GetCommonMineralModifier(PlanetTrait destinationTrait)
        {
            switch (destinationTrait)
            {
                case PlanetTrait.WaterRich:
                    return 50;
                case PlanetTrait.RareMineralRich:
                    return 15;
                case PlanetTrait.WoodRich:
                    return 80;
                case PlanetTrait.VolcanicActive:
                    return 100;
                case PlanetTrait.OrganicRich:
                    return 60;
                default:
                    return SameTraitSalePenaltyPercent;
            }
        }

        private static int GetRareMineralModifier(PlanetTrait destinationTrait)
        {
            switch (destinationTrait)
            {
                case PlanetTrait.WaterRich:
                    return 60;
                case PlanetTrait.CommonMineralRich:
                    return 40;
                case PlanetTrait.WoodRich:
                    return 60;
                case PlanetTrait.VolcanicActive:
                    return 80;
                case PlanetTrait.OrganicRich:
                    return 100;
                default:
                    return SameTraitSalePenaltyPercent;
            }
        }

        private static int GetWoodRichModifier(PlanetTrait destinationTrait)
        {
            switch (destinationTrait)
            {
                case PlanetTrait.WaterRich:
                    return 30;
                case PlanetTrait.CommonMineralRich:
                    return 100;
                case PlanetTrait.RareMineralRich:
                    return 60;
                case PlanetTrait.VolcanicActive:
                    return 5;
                case PlanetTrait.OrganicRich:
                    return 80;
                default:
                    return SameTraitSalePenaltyPercent;
            }
        }

        private static int GetVolcanicModifier(PlanetTrait destinationTrait)
        {
            switch (destinationTrait)
            {
                case PlanetTrait.WaterRich:
                    return 50;
                case PlanetTrait.CommonMineralRich:
                    return 80;
                case PlanetTrait.RareMineralRich:
                    return 100;
                case PlanetTrait.OrganicRich:
                    return 30;
                case PlanetTrait.WoodRich:
                    return 10;
                default:
                    return SameTraitSalePenaltyPercent;
            }
        }

        private static string CreateCargoId(PlanetTrait originTrait, int seed)
        {
            return "personal-" + originTrait.ToString().ToLowerInvariant() + "-" + Math.Abs(seed == int.MinValue ? 0 : seed);
        }

        private static string FormatGradeName(CargoGrade grade)
        {
            switch (grade)
            {
                case CargoGrade.Common:
                    return "Common";
                case CargoGrade.Rare:
                    return "Rare";
                case CargoGrade.Premium:
                    return "Premium";
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
            }
        }

        private static int RoundToInt(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
