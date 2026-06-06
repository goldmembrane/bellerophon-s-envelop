using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class ShipUpgradeRulesTests
    {
        [TestCase(ShipUpgradeCategory.Durability, 1, 1000)]
        [TestCase(ShipUpgradeCategory.Durability, 2, 2000)]
        [TestCase(ShipUpgradeCategory.Durability, 3, 4000)]
        [TestCase(ShipUpgradeCategory.WeaponSystems, 1, 1500)]
        [TestCase(ShipUpgradeCategory.WeaponSystems, 2, 2500)]
        [TestCase(ShipUpgradeCategory.WeaponSystems, 3, 4500)]
        [TestCase(ShipUpgradeCategory.AutoPilot, 1, 3000)]
        [TestCase(ShipUpgradeCategory.AutoPilot, 2, 4800)]
        [TestCase(ShipUpgradeCategory.AutoPilot, 3, 6500)]
        [TestCase(ShipUpgradeCategory.SupplySlots, 1, 1000)]
        [TestCase(ShipUpgradeCategory.SupplySlots, 2, 2500)]
        [TestCase(ShipUpgradeCategory.SupplySlots, 3, 5000)]
        [TestCase(ShipUpgradeCategory.InternalControl, 1, 2500)]
        [TestCase(ShipUpgradeCategory.InternalControl, 2, 5000)]
        [TestCase(ShipUpgradeCategory.InternalControl, 3, 10000)]
        public void PurchaseCosts_FollowUpdatedDesignSource(
            ShipUpgradeCategory category,
            int tier,
            int expectedCost)
        {
            Assert.That(ShipUpgradeRules.GetPurchaseCost(category, tier), Is.EqualTo(expectedCost));
        }

        [Test]
        public void PurchaseAndEquipRules_KeepPurchasedAndEquippedTiersSeparate()
        {
            var purchased = ShipUpgradeRules.PurchaseNextTier(
                ShipUpgradeState.Empty,
                ShipUpgradeCategory.SupplySlots);
            var equipped = ShipUpgradeRules.EquipHighestPurchasedTier(
                purchased,
                ShipUpgradeCategory.SupplySlots);

            Assert.That(purchased.GetPurchasedTier(ShipUpgradeCategory.SupplySlots), Is.EqualTo(1));
            Assert.That(purchased.GetEquippedTier(ShipUpgradeCategory.SupplySlots), Is.Zero);
            Assert.That(ShipUpgradeRules.GetNextPurchaseCost(purchased, ShipUpgradeCategory.SupplySlots), Is.EqualTo(2500));
            Assert.That(equipped.GetEquippedTier(ShipUpgradeCategory.SupplySlots), Is.EqualTo(1));
            Assert.That(ShipUpgradeRules.GetEffectValue(ShipUpgradeCategory.SupplySlots, 1), Is.EqualTo(5));
        }

        [Test]
        public void DurabilityPurchase_AutoEquipsPurchasedTier()
        {
            var purchased = ShipUpgradeRules.PurchaseNextTier(
                ShipUpgradeState.Empty,
                ShipUpgradeCategory.Durability);

            Assert.That(purchased.GetPurchasedTier(ShipUpgradeCategory.Durability), Is.EqualTo(1));
            Assert.That(purchased.GetEquippedTier(ShipUpgradeCategory.Durability), Is.EqualTo(1));
            Assert.That(ShipUpgradeRules.CanEquipPurchasedTier(purchased, ShipUpgradeCategory.Durability), Is.False);
        }
    }
}
