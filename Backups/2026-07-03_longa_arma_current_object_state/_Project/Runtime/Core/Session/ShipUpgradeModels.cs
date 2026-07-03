using System;

namespace Bellerophon.Core.Session
{
    public enum ShipUpgradeCategory
    {
        Durability,
        WeaponSystems,
        AutoPilot,
        SupplySlots,
        InternalControl
    }

    public readonly struct ShipAppearanceCustomizationState
    {
        public ShipAppearanceCustomizationState(
            string hullPaintSlotId,
            string emblemSlotId,
            string nameplateSlotId)
        {
            HullPaintSlotId = NormalizeSlotId(hullPaintSlotId, "default-hull");
            EmblemSlotId = NormalizeSlotId(emblemSlotId, "default-emblem");
            NameplateSlotId = NormalizeSlotId(nameplateSlotId, "default-nameplate");
        }

        public string HullPaintSlotId { get; }

        public string EmblemSlotId { get; }

        public string NameplateSlotId { get; }

        public static ShipAppearanceCustomizationState Default =>
            new ShipAppearanceCustomizationState("default-hull", "default-emblem", "default-nameplate");

        private static string NormalizeSlotId(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }

    public readonly struct ShipUpgradeState
    {
        public ShipUpgradeState(
            int durabilityPurchasedTier,
            int durabilityEquippedTier,
            int weaponSystemsPurchasedTier,
            int weaponSystemsEquippedTier,
            int autoPilotPurchasedTier,
            int autoPilotEquippedTier,
            int supplySlotsPurchasedTier,
            int supplySlotsEquippedTier,
            int internalControlPurchasedTier,
            int internalControlEquippedTier,
            ShipAppearanceCustomizationState appearance)
        {
            DurabilityPurchasedTier = ShipUpgradeRules.RequireTier(durabilityPurchasedTier, nameof(durabilityPurchasedTier));
            DurabilityEquippedTier = ShipUpgradeRules.RequireEquippedTier(durabilityEquippedTier, DurabilityPurchasedTier, nameof(durabilityEquippedTier));
            WeaponSystemsPurchasedTier = ShipUpgradeRules.RequireTier(weaponSystemsPurchasedTier, nameof(weaponSystemsPurchasedTier));
            WeaponSystemsEquippedTier = ShipUpgradeRules.RequireEquippedTier(weaponSystemsEquippedTier, WeaponSystemsPurchasedTier, nameof(weaponSystemsEquippedTier));
            AutoPilotPurchasedTier = ShipUpgradeRules.RequireTier(autoPilotPurchasedTier, nameof(autoPilotPurchasedTier));
            AutoPilotEquippedTier = ShipUpgradeRules.RequireEquippedTier(autoPilotEquippedTier, AutoPilotPurchasedTier, nameof(autoPilotEquippedTier));
            SupplySlotsPurchasedTier = ShipUpgradeRules.RequireTier(supplySlotsPurchasedTier, nameof(supplySlotsPurchasedTier));
            SupplySlotsEquippedTier = ShipUpgradeRules.RequireEquippedTier(supplySlotsEquippedTier, SupplySlotsPurchasedTier, nameof(supplySlotsEquippedTier));
            InternalControlPurchasedTier = ShipUpgradeRules.RequireTier(internalControlPurchasedTier, nameof(internalControlPurchasedTier));
            InternalControlEquippedTier = ShipUpgradeRules.RequireEquippedTier(internalControlEquippedTier, InternalControlPurchasedTier, nameof(internalControlEquippedTier));
            Appearance = appearance.HullPaintSlotId == null
                ? ShipAppearanceCustomizationState.Default
                : appearance;
        }

        public int DurabilityPurchasedTier { get; }

        public int DurabilityEquippedTier { get; }

        public int WeaponSystemsPurchasedTier { get; }

        public int WeaponSystemsEquippedTier { get; }

        public int AutoPilotPurchasedTier { get; }

        public int AutoPilotEquippedTier { get; }

        public int SupplySlotsPurchasedTier { get; }

        public int SupplySlotsEquippedTier { get; }

        public int InternalControlPurchasedTier { get; }

        public int InternalControlEquippedTier { get; }

        public ShipAppearanceCustomizationState Appearance { get; }

        public static ShipUpgradeState Empty =>
            new ShipUpgradeState(
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
                ShipAppearanceCustomizationState.Default);

        public int GetPurchasedTier(ShipUpgradeCategory category)
        {
            switch (category)
            {
                case ShipUpgradeCategory.Durability:
                    return DurabilityPurchasedTier;
                case ShipUpgradeCategory.WeaponSystems:
                    return WeaponSystemsPurchasedTier;
                case ShipUpgradeCategory.AutoPilot:
                    return AutoPilotPurchasedTier;
                case ShipUpgradeCategory.SupplySlots:
                    return SupplySlotsPurchasedTier;
                case ShipUpgradeCategory.InternalControl:
                    return InternalControlPurchasedTier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public int GetEquippedTier(ShipUpgradeCategory category)
        {
            switch (category)
            {
                case ShipUpgradeCategory.Durability:
                    return DurabilityEquippedTier;
                case ShipUpgradeCategory.WeaponSystems:
                    return WeaponSystemsEquippedTier;
                case ShipUpgradeCategory.AutoPilot:
                    return AutoPilotEquippedTier;
                case ShipUpgradeCategory.SupplySlots:
                    return SupplySlotsEquippedTier;
                case ShipUpgradeCategory.InternalControl:
                    return InternalControlEquippedTier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public ShipUpgradeState WithPurchasedTier(ShipUpgradeCategory category, int purchasedTier)
        {
            var equippedTier = GetEquippedTier(category);
            if (equippedTier > purchasedTier)
            {
                equippedTier = purchasedTier;
            }

            return WithTiers(category, purchasedTier, equippedTier);
        }

        public ShipUpgradeState WithEquippedTier(ShipUpgradeCategory category, int equippedTier)
        {
            return WithTiers(category, GetPurchasedTier(category), equippedTier);
        }

        private ShipUpgradeState WithTiers(
            ShipUpgradeCategory category,
            int purchasedTier,
            int equippedTier)
        {
            switch (category)
            {
                case ShipUpgradeCategory.Durability:
                    return new ShipUpgradeState(
                        purchasedTier,
                        equippedTier,
                        WeaponSystemsPurchasedTier,
                        WeaponSystemsEquippedTier,
                        AutoPilotPurchasedTier,
                        AutoPilotEquippedTier,
                        SupplySlotsPurchasedTier,
                        SupplySlotsEquippedTier,
                        InternalControlPurchasedTier,
                        InternalControlEquippedTier,
                        Appearance);
                case ShipUpgradeCategory.WeaponSystems:
                    return new ShipUpgradeState(
                        DurabilityPurchasedTier,
                        DurabilityEquippedTier,
                        purchasedTier,
                        equippedTier,
                        AutoPilotPurchasedTier,
                        AutoPilotEquippedTier,
                        SupplySlotsPurchasedTier,
                        SupplySlotsEquippedTier,
                        InternalControlPurchasedTier,
                        InternalControlEquippedTier,
                        Appearance);
                case ShipUpgradeCategory.AutoPilot:
                    return new ShipUpgradeState(
                        DurabilityPurchasedTier,
                        DurabilityEquippedTier,
                        WeaponSystemsPurchasedTier,
                        WeaponSystemsEquippedTier,
                        purchasedTier,
                        equippedTier,
                        SupplySlotsPurchasedTier,
                        SupplySlotsEquippedTier,
                        InternalControlPurchasedTier,
                        InternalControlEquippedTier,
                        Appearance);
                case ShipUpgradeCategory.SupplySlots:
                    return new ShipUpgradeState(
                        DurabilityPurchasedTier,
                        DurabilityEquippedTier,
                        WeaponSystemsPurchasedTier,
                        WeaponSystemsEquippedTier,
                        AutoPilotPurchasedTier,
                        AutoPilotEquippedTier,
                        purchasedTier,
                        equippedTier,
                        InternalControlPurchasedTier,
                        InternalControlEquippedTier,
                        Appearance);
                case ShipUpgradeCategory.InternalControl:
                    return new ShipUpgradeState(
                        DurabilityPurchasedTier,
                        DurabilityEquippedTier,
                        WeaponSystemsPurchasedTier,
                        WeaponSystemsEquippedTier,
                        AutoPilotPurchasedTier,
                        AutoPilotEquippedTier,
                        SupplySlotsPurchasedTier,
                        SupplySlotsEquippedTier,
                        purchasedTier,
                        equippedTier,
                        Appearance);
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }

    public readonly struct ShipUpgradePurchaseResult
    {
        public ShipUpgradePurchaseResult(
            bool purchased,
            GameSessionState state,
            ShipUpgradeCategory category,
            int purchasedTier,
            int spentCredits,
            string summary)
        {
            Purchased = purchased;
            State = state;
            Category = category;
            PurchasedTier = purchasedTier;
            SpentCredits = spentCredits;
            Summary = summary ?? string.Empty;
        }

        public bool Purchased { get; }

        public GameSessionState State { get; }

        public ShipUpgradeCategory Category { get; }

        public int PurchasedTier { get; }

        public int SpentCredits { get; }

        public string Summary { get; }
    }

    public readonly struct ShipUpgradeEquipResult
    {
        public ShipUpgradeEquipResult(
            bool equipped,
            GameSessionState state,
            ShipUpgradeCategory category,
            int equippedTier,
            string summary)
        {
            Equipped = equipped;
            State = state;
            Category = category;
            EquippedTier = equippedTier;
            Summary = summary ?? string.Empty;
        }

        public bool Equipped { get; }

        public GameSessionState State { get; }

        public ShipUpgradeCategory Category { get; }

        public int EquippedTier { get; }

        public string Summary { get; }
    }

    public static class ShipUpgradeRules
    {
        public const int MaxTier = 3;

        private static readonly ShipUpgradeCategory[] CategoryOrder =
        {
            ShipUpgradeCategory.Durability,
            ShipUpgradeCategory.WeaponSystems,
            ShipUpgradeCategory.AutoPilot,
            ShipUpgradeCategory.SupplySlots,
            ShipUpgradeCategory.InternalControl
        };

        public static ShipUpgradeCategory[] GetCategoryOrder()
        {
            return (ShipUpgradeCategory[])CategoryOrder.Clone();
        }

        public static int GetPurchaseCost(ShipUpgradeCategory category, int tier)
        {
            if (tier <= 0 || tier > MaxTier)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), "Ship upgrade tier must be between 1 and 3.");
            }

            switch (category)
            {
                case ShipUpgradeCategory.Durability:
                    return tier == 1 ? 1000 : tier == 2 ? 2000 : 4000;
                case ShipUpgradeCategory.WeaponSystems:
                    return tier == 1 ? 1500 : tier == 2 ? 2500 : 4500;
                case ShipUpgradeCategory.AutoPilot:
                    return tier == 1 ? 3000 : tier == 2 ? 4800 : 6500;
                case ShipUpgradeCategory.SupplySlots:
                    return tier == 1 ? 1000 : tier == 2 ? 2500 : 5000;
                case ShipUpgradeCategory.InternalControl:
                    return tier == 1 ? 2500 : tier == 2 ? 5000 : 10000;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static int GetNextPurchaseCost(ShipUpgradeState state, ShipUpgradeCategory category)
        {
            var nextTier = state.GetPurchasedTier(category) + 1;
            return nextTier > MaxTier ? 0 : GetPurchaseCost(category, nextTier);
        }

        public static bool CanPurchaseNextTier(ShipUpgradeState state, ShipUpgradeCategory category, int credits)
        {
            var cost = GetNextPurchaseCost(state, category);
            return cost > 0 && credits >= cost;
        }

        public static bool CanEquipPurchasedTier(ShipUpgradeState state, ShipUpgradeCategory category)
        {
            if (category == ShipUpgradeCategory.Durability)
            {
                return false;
            }

            return state.GetPurchasedTier(category) > 0 &&
                   state.GetEquippedTier(category) < state.GetPurchasedTier(category);
        }

        public static ShipUpgradeState PurchaseNextTier(ShipUpgradeState state, ShipUpgradeCategory category)
        {
            var purchasedTier = state.GetPurchasedTier(category);
            if (purchasedTier >= MaxTier)
            {
                return state;
            }

            var purchased = state.WithPurchasedTier(category, purchasedTier + 1);
            return category == ShipUpgradeCategory.Durability
                ? purchased.WithEquippedTier(category, purchased.GetPurchasedTier(category))
                : purchased;
        }

        public static ShipUpgradeState EquipHighestPurchasedTier(ShipUpgradeState state, ShipUpgradeCategory category)
        {
            return state.GetPurchasedTier(category) <= 0
                ? state
                : state.WithEquippedTier(category, state.GetPurchasedTier(category));
        }

        public static int GetEffectValue(ShipUpgradeCategory category, int tier)
        {
            RequireTier(tier, nameof(tier));
            switch (category)
            {
                case ShipUpgradeCategory.Durability:
                    return tier == 0 ? 500 : tier == 1 ? 600 : tier == 2 ? 750 : 1000;
                case ShipUpgradeCategory.WeaponSystems:
                    return tier == 0 ? 50 : tier == 1 ? 60 : tier == 2 ? 75 : 100;
                case ShipUpgradeCategory.AutoPilot:
                    return tier;
                case ShipUpgradeCategory.SupplySlots:
                    return tier == 0 ? 3 : tier == 1 ? 5 : tier == 2 ? 10 : 25;
                case ShipUpgradeCategory.InternalControl:
                    return tier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static string FormatCategoryName(ShipUpgradeCategory category)
        {
            switch (category)
            {
                case ShipUpgradeCategory.Durability:
                    return "Durability";
                case ShipUpgradeCategory.WeaponSystems:
                    return "Weapon Systems";
                case ShipUpgradeCategory.AutoPilot:
                    return "Auto Pilot";
                case ShipUpgradeCategory.SupplySlots:
                    return "Supply Slots";
                case ShipUpgradeCategory.InternalControl:
                    return "Internal Control";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static string FormatEffectSummary(ShipUpgradeCategory category, int tier)
        {
            RequireTier(tier, nameof(tier));
            switch (category)
            {
                case ShipUpgradeCategory.Durability:
                    return "Room durability rating " + GetEffectValue(category, tier);
                case ShipUpgradeCategory.WeaponSystems:
                    return tier >= 2
                        ? "Turret magazine " + GetEffectValue(category, tier) + " and plasma placement"
                        : "Turret magazine " + GetEffectValue(category, tier);
                case ShipUpgradeCategory.AutoPilot:
                    return FormatAutoPilotEffect(tier);
                case ShipUpgradeCategory.SupplySlots:
                    return "Supply room slots " + GetEffectValue(category, tier);
                case ShipUpgradeCategory.InternalControl:
                    return FormatInternalControlEffect(tier);
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static int RequireTier(int tier, string parameterName)
        {
            if (tier < 0 || tier > MaxTier)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Ship upgrade tier must be between 0 and 3.");
            }

            return tier;
        }

        public static int RequireEquippedTier(int equippedTier, int purchasedTier, string parameterName)
        {
            RequireTier(equippedTier, parameterName);
            if (equippedTier > purchasedTier)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Equipped upgrade tier cannot exceed purchased tier.");
            }

            return equippedTier;
        }

        private static string FormatAutoPilotEffect(int tier)
        {
            switch (tier)
            {
                case 0:
                    return "Base auto pilot cases";
                case 1:
                    return "Adds asteroid field auto-avoid";
                case 2:
                    return "Adds giant lifeform avoidance and slight intrusion reduction";
                case 3:
                    return "Adds pirate encounter avoidance and stronger intrusion reduction";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        private static string FormatInternalControlEffect(int tier)
        {
            switch (tier)
            {
                case 0:
                    return "Base engine control mode";
                case 1:
                    return "Adds corridor CCTV";
                case 2:
                    return "Intruder weakening improves from 10% to 20% health reduction";
                case 3:
                    return "Adds auto cleaning support and room turret placement";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }
    }
}
