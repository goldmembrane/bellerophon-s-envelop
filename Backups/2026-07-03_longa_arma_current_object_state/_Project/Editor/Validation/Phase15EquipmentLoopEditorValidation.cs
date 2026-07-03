using System;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase15EquipmentLoopEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase15EquipmentLoopBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 15 equipment loop validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase15EquipmentLoopBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase15EquipmentLoopBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase15EquipmentLoopBootstrap.Phase15RootName);
            var equipmentController = UnityEngine.Object.FindFirstObjectByType<PlayerEquipmentController>();
            var shopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            var planetController = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            if (root == null ||
                equipmentController == null ||
                shopController == null ||
                planetController == null ||
                deviceState == null ||
                maintenanceController == null)
            {
                throw new InvalidOperationException("Phase 15 equipment loop scene wiring is incomplete.");
            }

            if (equipmentController.EquipmentHudText == null ||
                equipmentController.PrecisionAimReticleText == null ||
                shopController.ShopRoot == null ||
                shopController.BodyText == null ||
                shopController.StatusText == null)
            {
                throw new InvalidOperationException("Phase 15 equipment HUD or shop text references are missing.");
            }

            if (shopController.BuyTabButton == null ||
                shopController.SellTabButton == null ||
                shopController.BuyStickButton == null ||
                shopController.BuyMusketButton == null ||
                shopController.BuyShotgunButton == null ||
                shopController.BuyFlashlightButton == null ||
                shopController.BuyInjuryRelieverButton == null ||
                shopController.BuyProtectiveSuitButton == null ||
                shopController.BuyStrengthEnhancerButton == null ||
                shopController.SellSelectedItemButton == null ||
                shopController.SellItemButtons.Length == 0 ||
                shopController.CloseButton == null ||
                maintenanceController.ShopButton == null)
            {
                throw new InvalidOperationException("Phase 15 shop buttons are missing.");
            }

            var background = shopController.ShopRoot.GetComponent<Image>();
            if (background == null || background.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 15 shop panel background must be fully opaque.");
            }

            var shopRootRect = shopController.ShopRoot.GetComponent<RectTransform>();
            if (shopRootRect == null ||
                shopRootRect.anchorMin != Vector2.zero ||
                shopRootRect.anchorMax != Vector2.one ||
                shopRootRect.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 15 shop root must cover the full screen so ship interior UI does not show behind it.");
            }

            if (shopController.IsShopVisible)
            {
                throw new InvalidOperationException("Phase 15 shop must start hidden.");
            }

            if (planetController.ShopController != shopController ||
                shopController.PlanetStayController != planetController ||
                shopController.MaintenanceController != maintenanceController)
            {
                throw new InvalidOperationException("Phase 15 shop must be linked to both the planet hub and maintenance screen.");
            }

            AssertNonBlockingText(equipmentController.EquipmentHudText, "equipment HUD");
            AssertNonBlockingText(equipmentController.PrecisionAimReticleText, "precision reticle");
            AssertNonBlockingText(shopController.BodyText, "shop body");
            AssertNonBlockingText(shopController.StatusText, "shop status");
            if (RectTransformsOverlap(
                    shopController.StatusText.GetComponent<RectTransform>(),
                    shopController.CloseButton.GetComponent<RectTransform>()))
            {
                throw new InvalidOperationException("Phase 15 shop status text must not overlap the close button.");
            }

            AssertButtonDoesNotOverlap(shopController.CloseButton, maintenanceController.ShopButton, "maintenance shop");
            AssertButtonDoesNotOverlap(shopController.CloseButton, maintenanceController.PersonalCargoButton, "maintenance cargo");
            AssertButtonDoesNotOverlap(shopController.CloseButton, maintenanceController.UpgradesButton, "maintenance upgrades");

            var summary = BuildValidationSummary();
            Debug.Log("Phase 15 equipment loop validation passed.");
            Debug.Log("Phase 15 equipment loop validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue();
            var stick = EquipmentRules.GetDefinition(EquipmentItemKind.Stick);
            var musket = EquipmentRules.GetDefinition(EquipmentItemKind.Musket);
            var shotgun = EquipmentRules.GetDefinition(EquipmentItemKind.Shotgun);
            var miniFlamethrower = EquipmentRules.GetDefinition(EquipmentItemKind.MiniFlamethrower);
            var electricBaton = EquipmentRules.GetDefinition(EquipmentItemKind.ElectricBaton);
            var dagger = EquipmentRules.GetDefinition(EquipmentItemKind.Dagger);
            if (!equipment.HasBasicProtectiveSuit ||
                equipment.GetHandSlot(0).ItemKind != EquipmentItemKind.Stick ||
                equipment.UnlockedHandSlotCount != PlayerEquipmentState.DefaultHandSlotCount ||
                equipment.UnlockedSupplySlotCount != PlayerEquipmentState.DefaultSupplySlotCount ||
                equipment.GetSupplySlot(0).IsEmpty == false ||
                stick.Damage != EquipmentRules.StickDamage ||
                stick.UseDelaySeconds != EquipmentRules.StickUseDelaySeconds ||
                !stick.HasThrowMode ||
                musket.Damage != EquipmentRules.MusketDamage ||
                musket.UseDelaySeconds != EquipmentRules.MusketUseDelaySeconds ||
                !musket.HasPrecisionAimMode ||
                !musket.HasReloadInputSkeleton ||
                musket.HasConfirmedMagazineSpec ||
                shotgun.Damage != EquipmentRules.ShotgunDamage ||
                shotgun.MinRange != EquipmentRules.ShotgunMinRange ||
                shotgun.MaxRange != EquipmentRules.ShotgunMaxRange ||
                !shotgun.HasReloadInputSkeleton ||
                !shotgun.HasConfirmedMagazineSpec ||
                miniFlamethrower.Damage != EquipmentRules.MiniFlamethrowerDamage ||
                electricBaton.Damage != EquipmentRules.ElectricBatonDamage ||
                dagger.Damage != EquipmentRules.DaggerDamage ||
                !dagger.HasThrowMode)
            {
                throw new InvalidOperationException("Phase 15 equipment definitions do not match confirmed scope.");
            }

            var purchaseSession = CreateCompletedTutorialSession().PurchaseEquipment(EquipmentItemKind.Musket);
            if (purchaseSession.Wallet.Credits != 650 ||
                purchaseSession.Equipment.GetHandSlot(1).ItemKind != EquipmentItemKind.Musket ||
                purchaseSession.Equipment.ActiveHandSlotIndex != 1)
            {
                throw new InvalidOperationException("Phase 15 musket shop purchase must deduct credits and equip the musket skeleton.");
            }

            var flashlightSession = purchaseSession.PurchaseEquipment(EquipmentItemKind.Flashlight);
            var aidSession = flashlightSession.PurchaseEquipment(EquipmentItemKind.InjuryReliever);
            var disposal = aidSession.DisposePurchasedSupplyEquipment(0);
            if (flashlightSession.Equipment.GetHandSlot(2).ItemKind != EquipmentItemKind.Flashlight ||
                aidSession.Equipment.GetSupplySlot(0).ItemKind != EquipmentItemKind.InjuryReliever ||
                !disposal.Disposed ||
                disposal.ItemKind != EquipmentItemKind.InjuryReliever ||
                disposal.ReceivedCredits != 1 ||
                disposal.State.Equipment.GetHandSlot(1).ItemKind != EquipmentItemKind.Musket ||
                disposal.State.Equipment.GetSupplySlot(0).IsEmpty == false)
            {
                throw new InvalidOperationException("Step 7 equipment purchases must use the third hand slot, supply storage, and selected 1% disposal.");
            }

            var step8Session = aidSession
                .PurchaseEquipment(EquipmentItemKind.ProtectiveSuit)
                .PurchaseEquipment(EquipmentItemKind.StrengthEnhancer);
            if (step8Session.Wallet.Credits != 0 ||
                step8Session.Equipment.GetSupplySlot(1).ItemKind != EquipmentItemKind.ProtectiveSuit ||
                step8Session.Equipment.GetSupplySlot(2).ItemKind != EquipmentItemKind.StrengthEnhancer)
            {
                throw new InvalidOperationException("Step 8 representative protective and enhancement purchases must fit the base supply storage.");
            }

            var protective = EquipmentRules.UseSupplyItem(step8Session.Equipment, 1);
            var treatment = EquipmentRules.UseSupplyItem(protective.State, 0);
            var strength = EquipmentRules.UseSupplyItem(treatment.State, 2);
            var strengthHit = EquipmentRules.UseActiveEquipment(strength.State.WithActiveHandSlot(0), false, true);
            if (protective.Outcome != EquipmentUseOutcome.ProtectiveEquipped ||
                EquipmentRules.CalculateDamageAfterProtection(50, protective.State) != 35 ||
                treatment.Outcome != EquipmentUseOutcome.TreatmentApplied ||
                treatment.HealthDelta != EquipmentRules.InjuryRelieverHealAmount ||
                !treatment.ConsumedItem ||
                strength.Outcome != EquipmentUseOutcome.EnhancementApplied ||
                !strength.State.HasActiveStrengthEnhancer ||
                strengthHit.Damage != 42)
            {
                throw new InvalidOperationException("Step 8 supply item effects must apply protective, treatment, and enhancement state.");
            }

            var lockedPurchase = EquipmentRules.PurchaseItem(equipment, EquipmentItemKind.PresenceDetector);
            if (lockedPurchase.Purchased)
            {
                throw new InvalidOperationException("Step 8 special utility shells must remain locked until their unlock conditions exist.");
            }

            var buyCatalog = EquipmentRules.CreatePhase15BuyCatalog();
            var sellCatalog = EquipmentRules.CreatePhase15SellCatalog();
            if (buyCatalog.Length < 20 ||
                sellCatalog.Length < 2 ||
                EquipmentRules.FilterCatalogByAvailability(buyCatalog, EquipmentAvailability.CommonShop).Length < 20 ||
                EquipmentRules.FilterCatalogByAvailability(buyCatalog, EquipmentAvailability.FameRestrictedShop).Length < 1 ||
                EquipmentRules.FilterCatalogByAvailability(buyCatalog, EquipmentAvailability.SpecialUnlock).Length < 1)
            {
                throw new InvalidOperationException("Step 7 shop catalog must include common, fame-limited, special, and sell sections.");
            }

            return $"HandSlots={PlayerEquipmentState.DefaultHandSlotCount}/{PlayerEquipmentState.UpgradedHandSlotCount}; SupplySlots={PlayerEquipmentState.DefaultSupplySlotCount}; Stick={stick.Damage}/{stick.UseDelaySeconds:0.0}s; Musket={musket.Damage}/{musket.UseDelaySeconds:0.0}s; Shotgun={shotgun.Damage}; Step8Protection={protective.DamageReductionPercent}; Step8StrengthHit={strengthHit.Damage}; Disposal={disposal.ReceivedCredits}";
        }

        private static GameSessionState CreateCompletedTutorialSession()
        {
            var tutorialContract = TransportContractDefinition.CreateTutorial();
            var tutorialSession = GameSessionState.StartAssociationSession().StartTransport(tutorialContract);
            return tutorialSession.CompleteTransport(new SettlementInput(
                tutorialContract.ContractType,
                tutorialContract.Difficulty,
                tutorialContract.Cargo,
                tutorialSession.Ship,
                new CrewState(1, 0),
                tutorialSession.Wallet,
                contractBasePay: tutorialContract.RewardCredits,
                repairSupportAmount: 100));
        }

        private static void AssertNonBlockingText(Text text, string label)
        {
            if (text == null)
            {
                throw new InvalidOperationException("Missing Phase 15 " + label + " text.");
            }

            if (text.raycastTarget)
            {
                throw new InvalidOperationException("Phase 15 " + label + " text must not block UI raycasts.");
            }
        }

        private static bool RectTransformsOverlap(RectTransform first, RectTransform second)
        {
            if (first == null || second == null || first.parent != second.parent)
            {
                return false;
            }

            var firstRect = GetLocalRect(first);
            var secondRect = GetLocalRect(second);
            return firstRect.xMin < secondRect.xMax &&
                   firstRect.xMax > secondRect.xMin &&
                   firstRect.yMin < secondRect.yMax &&
                   firstRect.yMax > secondRect.yMin;
        }

        private static Rect GetLocalRect(RectTransform rectTransform)
        {
            var rect = rectTransform.rect;
            var pivotOffset = new Vector2(rect.width * rectTransform.pivot.x, rect.height * rectTransform.pivot.y);
            var origin = rectTransform.anchoredPosition - pivotOffset;
            return new Rect(origin.x, origin.y, rect.width, rect.height);
        }

        private static void AssertButtonDoesNotOverlap(Button first, Button second, string label)
        {
            if (first == null || second == null)
            {
                throw new InvalidOperationException("Phase 15 close button overlap validation is missing " + label + " button.");
            }

            var firstRect = GetWorldRect(first.GetComponent<RectTransform>());
            var secondRect = GetWorldRect(second.GetComponent<RectTransform>());
            if (firstRect.xMin < secondRect.xMax &&
                firstRect.xMax > secondRect.xMin &&
                firstRect.yMin < secondRect.yMax &&
                firstRect.yMax > secondRect.yMin)
            {
                throw new InvalidOperationException("Phase 15 shop close button must not overlap the " + label + " button.");
            }
        }

        private static Rect GetWorldRect(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                return Rect.zero;
            }

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
