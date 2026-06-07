using System;

namespace Bellerophon.Core.Session
{
    public enum EquipmentItemKind
    {
        None,
        Stick,
        Musket,
        BasicProtectiveSuit,
        SuppressionShield,
        ProtectiveSuit,
        InsulatedSuit,
        FireproofSuit,
        HeadProtector,
        InjuryReliever,
        BandageSet,
        AuxiliaryBattery,
        ShieldChargeBattery,
        ShieldConverter,
        MoveSpeedEnhancer,
        StrengthEnhancer,
        ProtectiveEnhancer,
        FocusEnhancer,
        ShieldSurgeInducer,
        VacuumCleaner,
        PortableSpeaker,
        HologramSpray,
        Flashbang,
        Flashlight,
        TemporaryOpenerSet,
        PhysicalProtectiveSuit,
        NanomachineTreatment,
        RapidShieldBuffer,
        RepairDevice,
        MarkerSpray,
        PresenceDetector,
        Shotgun,
        MiniFlamethrower,
        ElectricBaton,
        Dagger
    }

    public enum EquipmentItemCategory
    {
        None,
        Weapon,
        ProtectiveGear,
        Treatment,
        Enhancement,
        Utility
    }

    public enum EquipmentStorageTarget
    {
        HandFirst,
        HandOnly,
        SupplyOnly
    }

    public enum EquipmentUseMode
    {
        None,
        Primary,
        Throwing,
        PrecisionAim
    }

    public enum EquipmentUseOutcome
    {
        None,
        NoItem,
        CooldownBlocked,
        MeleeHit,
        MeleeMiss,
        ThrowSkeleton,
        RangedHit,
        RangedMiss,
        ReloadSkeleton,
        Dropped,
        ProtectiveEquipped,
        TreatmentApplied,
        EnhancementApplied,
        UtilityActivated,
        ActionBlocked
    }

    public enum EquipmentShopSection
    {
        Buy,
        Sell
    }

    public readonly struct EquipmentItemDefinition
    {
        public EquipmentItemDefinition(
            EquipmentItemKind itemKind,
            string displayName,
            EquipmentItemCategory category,
            int damage,
            float minRange,
            float maxRange,
            float useDelaySeconds,
            int priceCredits,
            bool requiresTwoHands,
            bool hasThrowMode,
            bool hasPrecisionAimMode,
            bool hasReloadInputSkeleton,
            bool hasConfirmedMagazineSpec,
            float precisionAimMoveMultiplier,
            EquipmentStorageTarget storageTarget = EquipmentStorageTarget.HandFirst,
            int maxStackCount = 1,
            int maxDurabilityPercent = 100,
            bool isUniquePerShip = true,
            EquipmentAvailability availability = EquipmentAvailability.CommonShop)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(itemKind), "Equipment definition requires an item kind.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Equipment display name is required.", nameof(displayName));
            }

            if (category == EquipmentItemCategory.None)
            {
                throw new ArgumentOutOfRangeException(nameof(category), "Equipment definition requires a category.");
            }

            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Equipment damage cannot be negative.");
            }

            if (minRange < 0f || maxRange < minRange)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRange), "Equipment range must be non-negative and ordered.");
            }

            if (useDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(useDelaySeconds), "Equipment use delay cannot be negative.");
            }

            if (priceCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priceCredits), "Equipment price cannot be negative.");
            }

            if (maxStackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackCount), "Equipment max stack count must be positive.");
            }

            if (maxDurabilityPercent <= 0 || maxDurabilityPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDurabilityPercent), "Equipment max durability must be between 1 and 100.");
            }

            ItemKind = itemKind;
            DisplayName = displayName;
            Category = category;
            Damage = damage;
            MinRange = minRange;
            MaxRange = maxRange;
            UseDelaySeconds = useDelaySeconds;
            PriceCredits = priceCredits;
            RequiresTwoHands = requiresTwoHands;
            HasThrowMode = hasThrowMode;
            HasPrecisionAimMode = hasPrecisionAimMode;
            HasReloadInputSkeleton = hasReloadInputSkeleton;
            HasConfirmedMagazineSpec = hasConfirmedMagazineSpec;
            PrecisionAimMoveMultiplier = precisionAimMoveMultiplier <= 0f ? 1f : precisionAimMoveMultiplier;
            StorageTarget = storageTarget;
            MaxStackCount = maxStackCount;
            MaxDurabilityPercent = maxDurabilityPercent;
            IsUniquePerShip = isUniquePerShip;
            Availability = availability;
        }

        public EquipmentItemKind ItemKind { get; }

        public string DisplayName { get; }

        public EquipmentItemCategory Category { get; }

        public int Damage { get; }

        public float MinRange { get; }

        public float MaxRange { get; }

        public float UseDelaySeconds { get; }

        public int PriceCredits { get; }

        public bool RequiresTwoHands { get; }

        public bool HasThrowMode { get; }

        public bool HasPrecisionAimMode { get; }

        public bool HasReloadInputSkeleton { get; }

        public bool HasConfirmedMagazineSpec { get; }

        public float PrecisionAimMoveMultiplier { get; }

        public EquipmentStorageTarget StorageTarget { get; }

        public int MaxStackCount { get; }

        public int MaxDurabilityPercent { get; }

        public bool IsUniquePerShip { get; }

        public EquipmentAvailability Availability { get; }
    }

    public readonly struct EquipmentSlotState
    {
        public EquipmentSlotState(EquipmentItemKind itemKind, int count)
            : this(itemKind, count, itemKind == EquipmentItemKind.None || count <= 0 ? 0 : 100, 0)
        {
        }

        public EquipmentSlotState(
            EquipmentItemKind itemKind,
            int count,
            int durabilityPercent,
            int purchasePriceCredits)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Equipment count cannot be negative.");
            }

            if (durabilityPercent < 0 || durabilityPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(durabilityPercent), "Equipment durability must be between 0 and 100.");
            }

            if (purchasePriceCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(purchasePriceCredits), "Equipment purchase price cannot be negative.");
            }

            var isEmpty = itemKind == EquipmentItemKind.None || count == 0;
            ItemKind = isEmpty ? EquipmentItemKind.None : itemKind;
            Count = isEmpty ? 0 : count;
            DurabilityPercent = isEmpty ? 0 : durabilityPercent;
            PurchasePriceCredits = isEmpty ? 0 : purchasePriceCredits;
        }

        public EquipmentItemKind ItemKind { get; }

        public int Count { get; }

        public int DurabilityPercent { get; }

        public int PurchasePriceCredits { get; }

        public bool IsEmpty => ItemKind == EquipmentItemKind.None || Count <= 0;

        public bool WasPurchased => !IsEmpty && PurchasePriceCredits > 0;

        public static EquipmentSlotState Empty => new EquipmentSlotState(EquipmentItemKind.None, 0);

        public static EquipmentSlotState One(EquipmentItemKind itemKind)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                return Empty;
            }

            return new EquipmentSlotState(itemKind, 1);
        }

        public static EquipmentSlotState Purchased(EquipmentItemKind itemKind, int purchasePriceCredits)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                return Empty;
            }

            return new EquipmentSlotState(itemKind, 1, 100, purchasePriceCredits);
        }

        public EquipmentSlotState WithCount(int count)
        {
            return count <= 0
                ? Empty
                : new EquipmentSlotState(ItemKind, count, DurabilityPercent, PurchasePriceCredits);
        }

        public EquipmentSlotState WithDurabilityPercent(int durabilityPercent)
        {
            return IsEmpty
                ? Empty
                : new EquipmentSlotState(ItemKind, Count, durabilityPercent, PurchasePriceCredits);
        }

        public EquipmentSlotState WithPurchasePriceCredits(int purchasePriceCredits)
        {
            return IsEmpty
                ? Empty
                : new EquipmentSlotState(ItemKind, Count, DurabilityPercent, purchasePriceCredits);
        }
    }

    public readonly struct EquipmentShopCatalogEntry
    {
        public EquipmentShopCatalogEntry(
            EquipmentShopSection section,
            EquipmentItemCategory category,
            EquipmentItemKind itemKind,
            string displayName,
            int priceCredits,
            EquipmentAvailability availability,
            bool functionalInPhase15)
        {
            if (category == EquipmentItemCategory.None)
            {
                throw new ArgumentOutOfRangeException(nameof(category), "Shop entry requires a category.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Shop entry display name is required.", nameof(displayName));
            }

            if (priceCredits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(priceCredits), "Shop entry price cannot be negative.");
            }

            Section = section;
            Category = category;
            ItemKind = itemKind;
            DisplayName = displayName;
            PriceCredits = priceCredits;
            Availability = availability;
            FunctionalInPhase15 = functionalInPhase15;
        }

        public EquipmentShopSection Section { get; }

        public EquipmentItemCategory Category { get; }

        public EquipmentItemKind ItemKind { get; }

        public string DisplayName { get; }

        public int PriceCredits { get; }

        public EquipmentAvailability Availability { get; }

        public bool FunctionalInPhase15 { get; }
    }

    public readonly struct EquipmentUseResult
    {
        public EquipmentUseResult(
            PlayerEquipmentState state,
            EquipmentUseOutcome outcome,
            EquipmentItemKind itemKind,
            EquipmentUseMode mode,
            int damage,
            string summary,
            int healthDelta = 0,
            int shieldDelta = 0,
            bool consumedItem = false,
            int damageReductionPercent = 0,
            int damageBonusPercent = 0,
            float effectDurationSeconds = 0f,
            CombatStatusEffectApplication statusEffectToApply = default,
            CombatStatusEffectKind statusEffectToClear = CombatStatusEffectKind.None,
            CombatStatusEffectApplication delayedStatusEffectToApply = default,
            float delayedStatusEffectDelaySeconds = 0f)
        {
            State = state;
            Outcome = outcome;
            ItemKind = itemKind;
            Mode = mode;
            Damage = Math.Max(0, damage);
            Summary = summary ?? string.Empty;
            HealthDelta = Math.Max(0, healthDelta);
            ShieldDelta = Math.Max(0, shieldDelta);
            ConsumedItem = consumedItem;
            DamageReductionPercent = ClampPercent(damageReductionPercent);
            DamageBonusPercent = Math.Max(0, damageBonusPercent);
            EffectDurationSeconds = Math.Max(0f, effectDurationSeconds);
            StatusEffectToApply = statusEffectToApply;
            StatusEffectToClear = statusEffectToClear;
            DelayedStatusEffectToApply = delayedStatusEffectToApply;
            DelayedStatusEffectDelaySeconds = Math.Max(0f, delayedStatusEffectDelaySeconds);
        }

        public PlayerEquipmentState State { get; }

        public EquipmentUseOutcome Outcome { get; }

        public EquipmentItemKind ItemKind { get; }

        public EquipmentUseMode Mode { get; }

        public int Damage { get; }

        public string Summary { get; }

        public int HealthDelta { get; }

        public int ShieldDelta { get; }

        public bool ConsumedItem { get; }

        public int DamageReductionPercent { get; }

        public int DamageBonusPercent { get; }

        public float EffectDurationSeconds { get; }

        public CombatStatusEffectApplication StatusEffectToApply { get; }

        public CombatStatusEffectKind StatusEffectToClear { get; }

        public CombatStatusEffectApplication DelayedStatusEffectToApply { get; }

        public float DelayedStatusEffectDelaySeconds { get; }

        public bool AppliesIntruderDamage =>
            Outcome == EquipmentUseOutcome.MeleeHit ||
            Outcome == EquipmentUseOutcome.RangedHit;

        private static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }

    public readonly struct EquipmentPurchaseResult
    {
        public EquipmentPurchaseResult(
            PlayerEquipmentState state,
            bool purchased,
            int spentCredits,
            EquipmentItemKind itemKind,
            string summary)
        {
            State = state;
            Purchased = purchased;
            SpentCredits = Math.Max(0, spentCredits);
            ItemKind = itemKind;
            Summary = summary ?? string.Empty;
        }

        public PlayerEquipmentState State { get; }

        public bool Purchased { get; }

        public int SpentCredits { get; }

        public EquipmentItemKind ItemKind { get; }

        public string Summary { get; }
    }

    public readonly struct EquipmentDisposalResult
    {
        public EquipmentDisposalResult(
            PlayerEquipmentState state,
            bool disposed,
            int receivedCredits,
            EquipmentItemKind itemKind,
            string summary)
        {
            State = state;
            Disposed = disposed;
            ReceivedCredits = Math.Max(0, receivedCredits);
            ItemKind = itemKind;
            Summary = summary ?? string.Empty;
        }

        public PlayerEquipmentState State { get; }

        public bool Disposed { get; }

        public int ReceivedCredits { get; }

        public EquipmentItemKind ItemKind { get; }

        public string Summary { get; }
    }

    public readonly struct EquipmentDisposalSessionResult
    {
        public EquipmentDisposalSessionResult(
            bool disposed,
            GameSessionState state,
            EquipmentItemKind itemKind,
            int receivedCredits,
            string summary)
        {
            Disposed = disposed;
            State = state;
            ItemKind = itemKind;
            ReceivedCredits = Math.Max(0, receivedCredits);
            Summary = summary ?? string.Empty;
        }

        public bool Disposed { get; }

        public GameSessionState State { get; }

        public EquipmentItemKind ItemKind { get; }

        public int ReceivedCredits { get; }

        public string Summary { get; }
    }

    public readonly struct PlayerEquipmentState
    {
        public const int DefaultHandSlotCount = 3;
        public const int UpgradedHandSlotCount = 4;
        public const int MaxHandSlotCount = 4;
        public const int DefaultSupplySlotCount = 3;
        public const int MaxSupplySlotCount = 25;

        private static readonly EquipmentSlotState[] EmptyHandSlots = CreateEmptySlots(MaxHandSlotCount);
        private static readonly EquipmentSlotState[] EmptySupplySlots = CreateEmptySlots(MaxSupplySlotCount);

        private readonly EquipmentSlotState[] handSlots;
        private readonly EquipmentSlotState[] supplySlots;

        // Tracks source-defined timed weapon state that must survive equipment state copies.
        public PlayerEquipmentState(
            bool hasBasicProtectiveSuit,
            EquipmentSlotState[] handSlots,
            EquipmentSlotState[] supplySlots,
            int activeHandSlotIndex,
            float useCooldownSeconds,
            EquipmentUseMode activeMode,
            string lastActionSummary,
            int unlockedHandSlotCount = DefaultHandSlotCount,
            int unlockedSupplySlotCount = DefaultSupplySlotCount,
            EquipmentItemKind activeProtectiveItemKind = EquipmentItemKind.None,
            int activeDamageReductionPercent = 0,
            float strengthEnhancerRemainingSeconds = 0f,
            int strengthDamageBonusPercent = 0,
            float flashlightRemainingSeconds = 0f,
            float electricBatonChargeCooldownSeconds = 0f,
            float miniFlamethrowerContinuousHitSeconds = 0f,
            float miniFlamethrowerHitGapSeconds = 0f)
        {
            var normalizedHandCount = RequireHandSlotCount(unlockedHandSlotCount, nameof(unlockedHandSlotCount));
            var normalizedSupplyCount = RequireSupplySlotCount(unlockedSupplySlotCount, nameof(unlockedSupplySlotCount));
            if (activeHandSlotIndex < 0 || activeHandSlotIndex >= normalizedHandCount)
            {
                throw new ArgumentOutOfRangeException(nameof(activeHandSlotIndex), activeHandSlotIndex, null);
            }

            HasBasicProtectiveSuit = hasBasicProtectiveSuit;
            this.handSlots = NormalizeSlots(handSlots, MaxHandSlotCount);
            this.supplySlots = NormalizeSlots(supplySlots, MaxSupplySlotCount);
            ActiveHandSlotIndex = activeHandSlotIndex;
            UseCooldownSeconds = Math.Max(0f, useCooldownSeconds);
            ActiveMode = activeMode;
            LastActionSummary = lastActionSummary ?? string.Empty;
            UnlockedHandSlotCount = normalizedHandCount;
            UnlockedSupplySlotCount = normalizedSupplyCount;
            ActiveProtectiveItemKind = activeProtectiveItemKind;
            ActiveDamageReductionPercent = ClampPercent(activeDamageReductionPercent);
            StrengthEnhancerRemainingSeconds = Math.Max(0f, strengthEnhancerRemainingSeconds);
            StrengthDamageBonusPercent = Math.Max(0, strengthDamageBonusPercent);
            FlashlightRemainingSeconds = Math.Max(0f, flashlightRemainingSeconds);
            ElectricBatonChargeCooldownSeconds = Math.Max(0f, electricBatonChargeCooldownSeconds);
            MiniFlamethrowerContinuousHitSeconds = Math.Max(0f, miniFlamethrowerContinuousHitSeconds);
            MiniFlamethrowerHitGapSeconds = Math.Max(0f, miniFlamethrowerHitGapSeconds);
        }

        public bool HasBasicProtectiveSuit { get; }

        public int UnlockedHandSlotCount { get; }

        public int UnlockedSupplySlotCount { get; }

        public int ActiveHandSlotIndex { get; }

        public float UseCooldownSeconds { get; }

        public EquipmentUseMode ActiveMode { get; }

        public string LastActionSummary { get; }

        public EquipmentItemKind ActiveProtectiveItemKind { get; }

        public int ActiveDamageReductionPercent { get; }

        public float StrengthEnhancerRemainingSeconds { get; }

        public int StrengthDamageBonusPercent { get; }

        public float FlashlightRemainingSeconds { get; }

        public float ElectricBatonChargeCooldownSeconds { get; }

        public float MiniFlamethrowerContinuousHitSeconds { get; }

        public float MiniFlamethrowerHitGapSeconds { get; }

        public bool HasActiveStrengthEnhancer => StrengthEnhancerRemainingSeconds > 0.0001f && StrengthDamageBonusPercent > 0;

        public bool HasActiveFlashlight => FlashlightRemainingSeconds > 0.0001f;

        public bool IsElectricBatonCharged => ElectricBatonChargeCooldownSeconds <= 0.0001f;

        public EquipmentSlotState ActiveHandSlot => GetHandSlot(ActiveHandSlotIndex);

        public bool HasAnyItem(EquipmentItemKind itemKind)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                return false;
            }

            for (var i = 0; i < UnlockedHandSlotCount; i++)
            {
                if (GetHandSlot(i).ItemKind == itemKind)
                {
                    return true;
                }
            }

            for (var i = 0; i < UnlockedSupplySlotCount; i++)
            {
                if (GetSupplySlot(i).ItemKind == itemKind)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasPurchasedItem()
        {
            for (var i = 0; i < UnlockedHandSlotCount; i++)
            {
                if (GetHandSlot(i).WasPurchased)
                {
                    return true;
                }
            }

            for (var i = 0; i < UnlockedSupplySlotCount; i++)
            {
                if (GetSupplySlot(i).WasPurchased)
                {
                    return true;
                }
            }

            return false;
        }

        public EquipmentSlotState GetHandSlot(int index)
        {
            if (index < 0 || index >= UnlockedHandSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return handSlots == null ? EmptyHandSlots[index] : handSlots[index];
        }

        public EquipmentSlotState GetSupplySlot(int index)
        {
            if (index < 0 || index >= UnlockedSupplySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return supplySlots == null ? EmptySupplySlots[index] : supplySlots[index];
        }

        public EquipmentSlotState[] HandSlots => CloneSlots(handSlots ?? EmptyHandSlots);

        public EquipmentSlotState[] SupplySlots => CloneSlots(supplySlots ?? EmptySupplySlots);

        public PlayerEquipmentState WithBasicProtectiveSuit(bool hasSuit)
        {
            return new PlayerEquipmentState(
                hasSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithHandSlot(int index, EquipmentSlotState slot)
        {
            if (index < 0 || index >= UnlockedHandSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            var nextSlots = HandSlots;
            nextSlots[index] = slot;
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                nextSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithSupplySlot(int index, EquipmentSlotState slot)
        {
            if (index < 0 || index >= UnlockedSupplySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            var nextSlots = SupplySlots;
            nextSlots[index] = slot;
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                nextSlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithActiveHandSlot(int activeSlotIndex)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                activeSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithUnlockedHandSlotCount(int unlockedHandSlotCount)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                Math.Min(ActiveHandSlotIndex, unlockedHandSlotCount - 1),
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary,
                unlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithPouchUpgrade(bool enabled)
        {
            return WithUnlockedHandSlotCount(enabled ? UpgradedHandSlotCount : DefaultHandSlotCount);
        }

        public PlayerEquipmentState WithUnlockedSupplySlotCount(int unlockedSupplySlotCount)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary,
                UnlockedHandSlotCount,
                unlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithCooldown(float cooldownSeconds)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                cooldownSeconds,
                ActiveMode,
                LastActionSummary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithModeAndSummary(EquipmentUseMode mode, string summary)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                mode,
                summary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithCooldownModeAndSummary(
            float cooldownSeconds,
            EquipmentUseMode mode,
            string summary)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                cooldownSeconds,
                mode,
                summary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithProtection(EquipmentItemKind itemKind, int damageReductionPercent, string summary)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                summary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                itemKind,
                damageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithStrengthEnhancer(float remainingSeconds, int damageBonusPercent, string summary)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                summary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                remainingSeconds,
                damageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithFlashlight(float remainingSeconds, string summary)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                summary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                remainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithTimedEffects(
            float strengthEnhancerRemainingSeconds,
            float flashlightRemainingSeconds,
            float electricBatonChargeCooldownSeconds,
            float miniFlamethrowerContinuousHitSeconds,
            float miniFlamethrowerHitGapSeconds)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                strengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                flashlightRemainingSeconds,
                electricBatonChargeCooldownSeconds,
                miniFlamethrowerContinuousHitSeconds,
                miniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithElectricBatonChargeCooldown(float cooldownSeconds, string summary)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                summary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                cooldownSeconds,
                MiniFlamethrowerContinuousHitSeconds,
                MiniFlamethrowerHitGapSeconds);
        }

        public PlayerEquipmentState WithMiniFlamethrowerHitWindow(
            float continuousHitSeconds,
            float hitGapSeconds,
            string summary)
        {
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                summary,
                UnlockedHandSlotCount,
                UnlockedSupplySlotCount,
                ActiveProtectiveItemKind,
                ActiveDamageReductionPercent,
                StrengthEnhancerRemainingSeconds,
                StrengthDamageBonusPercent,
                FlashlightRemainingSeconds,
                ElectricBatonChargeCooldownSeconds,
                continuousHitSeconds,
                hitGapSeconds);
        }

        public static PlayerEquipmentState Empty => new PlayerEquipmentState(
            false,
            EmptyHandSlots,
            EmptySupplySlots,
            0,
            0f,
            EquipmentUseMode.None,
            string.Empty);

        public static PlayerEquipmentState CreateDefaultAssociationIssue()
        {
            return new PlayerEquipmentState(
                true,
                new[]
                {
                    EquipmentSlotState.One(EquipmentItemKind.Stick),
                    EquipmentSlotState.Empty,
                    EquipmentSlotState.Empty,
                    EquipmentSlotState.Empty
                },
                EmptySupplySlots,
                0,
                0f,
                EquipmentUseMode.Primary,
                "Basic protective suit equipped; stick issued.");
        }

        private static int RequireHandSlotCount(int slotCount, string parameterName)
        {
            if (slotCount < 1 || slotCount > MaxHandSlotCount)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Hand slot count must be between 1 and " + MaxHandSlotCount + ".");
            }

            return slotCount;
        }

        private static int RequireSupplySlotCount(int slotCount, string parameterName)
        {
            if (slotCount < 0 || slotCount > MaxSupplySlotCount)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Supply slot count must be between 0 and " + MaxSupplySlotCount + ".");
            }

            return slotCount;
        }

        private static EquipmentSlotState[] NormalizeSlots(EquipmentSlotState[] slots, int requiredCount)
        {
            var normalized = new EquipmentSlotState[requiredCount];
            for (var i = 0; i < requiredCount; i++)
            {
                normalized[i] = slots != null && i < slots.Length
                    ? slots[i]
                    : EquipmentSlotState.Empty;
            }

            return normalized;
        }

        private static EquipmentSlotState[] CloneSlots(EquipmentSlotState[] slots)
        {
            var clone = new EquipmentSlotState[slots.Length];
            Array.Copy(slots, clone, slots.Length);
            return clone;
        }

        private static EquipmentSlotState[] CreateEmptySlots(int slotCount)
        {
            var slots = new EquipmentSlotState[slotCount];
            for (var i = 0; i < slotCount; i++)
            {
                slots[i] = EquipmentSlotState.Empty;
            }

            return slots;
        }

        private static int ClampPercent(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }

    public static class EquipmentRules
    {
        public const int StickDamage = 30;
        public const float StickMinRange = 2f;
        public const float StickMaxRange = 3f;
        public const float StickUseDelaySeconds = 2.5f;
        public const float StickThrowMinRange = 4f;
        public const float StickThrowMaxRange = 5f;
        public const int StickPriceCredits = 200;

        public const int MusketDamage = 50;
        public const float MusketMinRange = 5f;
        public const float MusketMaxRange = 7f;
        public const float MusketUseDelaySeconds = 3.5f;
        public const float MusketPrecisionAimMoveMultiplier = 0.8f;
        public const int MusketPriceCredits = 450;

        public const int ShotgunDamage = 70;
        public const float ShotgunMinRange = 1.5f;
        public const float ShotgunMaxRange = 4f;
        public const float ShotgunUseDelaySeconds = 3f;
        public const int ShotgunPriceCredits = 600;

        public const int MiniFlamethrowerDamage = 4;
        public const float MiniFlamethrowerMinRange = 1f;
        public const float MiniFlamethrowerMaxRange = 3f;
        public const float MiniFlamethrowerUseDelaySeconds = 0.5f;
        public const float MiniFlamethrowerSustainedHitGraceSeconds = 0.1f;
        public const int MiniFlamethrowerPriceCredits = 800;

        public const int ElectricBatonDamage = 25;
        public const float ElectricBatonMinRange = 1f;
        public const float ElectricBatonMaxRange = 1.5f;
        public const float ElectricBatonUseDelaySeconds = 2.5f;
        public const int ElectricBatonPriceCredits = 500;

        public const int DaggerDamage = 15;
        public const float DaggerMinRange = 1f;
        public const float DaggerMaxRange = 1f;
        public const float DaggerUseDelaySeconds = 2f;
        public const int DaggerPriceCredits = 150;

        public const int FlashlightPriceCredits = 25;
        public const int InjuryRelieverPriceCredits = 125;
        public const int InjuryRelieverHealAmount = 25;
        public const int BandageSetHealAmount = 10;
        public const int AuxiliaryBatteryShieldAmount = 10;
        public const int ShieldChargeBatteryShieldAmount = 25;
        public const int NanomachineTreatmentHealAmount = 100;
        public const int RapidShieldBufferShieldAmount = 50;
        public const int ProtectiveSuitReductionPercent = 20;
        public const int InsulatedSuitReductionPercent = 20;
        public const int FireproofSuitReductionPercent = 20;
        public const int HeadProtectorReductionPercent = 20;
        public const int PhysicalProtectiveSuitReductionPercent = 30;
        public const int SuppressionShieldReductionPercent = 30;
        public const int ProtectiveEnhancerReductionPercent = 20;
        public const float MoveSpeedEnhancerDurationSeconds = 10f;
        public const int StrengthEnhancerDamageBonusPercent = 40;
        public const float StrengthEnhancerDurationSeconds = 60f;
        public const float ProtectiveEnhancerDurationSeconds = 60f;
        public const float FocusEnhancerDurationSeconds = 60f;
        public const float FlashlightDurationSeconds = 45f;

        private static readonly EquipmentItemCategory[] StorageTabOrder =
        {
            EquipmentItemCategory.None,
            EquipmentItemCategory.Weapon,
            EquipmentItemCategory.ProtectiveGear,
            EquipmentItemCategory.Treatment,
            EquipmentItemCategory.Enhancement,
            EquipmentItemCategory.Utility
        };

        private static readonly EquipmentShopCatalogEntry[] Phase15BuyCatalog =
        {
            BuyEntry(EquipmentItemKind.Stick, true),
            BuyEntry(EquipmentItemKind.Musket, true),
            BuyEntry(EquipmentItemKind.Shotgun, true),
            BuyEntry(EquipmentItemKind.MiniFlamethrower, true),
            BuyEntry(EquipmentItemKind.ElectricBaton, true),
            BuyEntry(EquipmentItemKind.Dagger, true),
            BuyEntry(EquipmentItemKind.SuppressionShield, true),
            BuyEntry(EquipmentItemKind.ProtectiveSuit, true),
            BuyEntry(EquipmentItemKind.InsulatedSuit, true),
            BuyEntry(EquipmentItemKind.FireproofSuit, true),
            BuyEntry(EquipmentItemKind.HeadProtector, true),
            BuyEntry(EquipmentItemKind.InjuryReliever, true),
            BuyEntry(EquipmentItemKind.BandageSet, true),
            BuyEntry(EquipmentItemKind.AuxiliaryBattery, true),
            BuyEntry(EquipmentItemKind.ShieldChargeBattery, true),
            BuyEntry(EquipmentItemKind.ShieldConverter, true),
            BuyEntry(EquipmentItemKind.MoveSpeedEnhancer, true),
            BuyEntry(EquipmentItemKind.StrengthEnhancer, true),
            BuyEntry(EquipmentItemKind.ProtectiveEnhancer, true),
            BuyEntry(EquipmentItemKind.FocusEnhancer, true),
            BuyEntry(EquipmentItemKind.ShieldSurgeInducer, true),
            BuyEntry(EquipmentItemKind.VacuumCleaner, true),
            BuyEntry(EquipmentItemKind.PortableSpeaker, true),
            BuyEntry(EquipmentItemKind.HologramSpray, true),
            BuyEntry(EquipmentItemKind.Flashbang, true),
            BuyEntry(EquipmentItemKind.Flashlight, true),
            BuyEntry(EquipmentItemKind.TemporaryOpenerSet, true),
            BuyEntry(EquipmentItemKind.PhysicalProtectiveSuit, true),
            BuyEntry(EquipmentItemKind.NanomachineTreatment, true),
            BuyEntry(EquipmentItemKind.RapidShieldBuffer, true),
            BuyEntry(EquipmentItemKind.RepairDevice, true),
            BuyEntry(EquipmentItemKind.MarkerSpray, true),
            BuyEntry(EquipmentItemKind.PresenceDetector, false)
        };

        private static readonly EquipmentShopCatalogEntry[] Phase15SellCatalog =
        {
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Sell,
                EquipmentItemCategory.Utility,
                EquipmentItemKind.None,
                "Purchased item disposal",
                0,
                EquipmentAvailability.CommonShop,
                true),
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Sell,
                EquipmentItemCategory.Utility,
                EquipmentItemKind.None,
                "Personal cargo sale slot",
                0,
                EquipmentAvailability.CommonShop,
                true)
        };

        public static EquipmentItemDefinition GetDefinition(EquipmentItemKind itemKind)
        {
            switch (itemKind)
            {
                case EquipmentItemKind.Stick:
                    return Weapon(
                        EquipmentItemKind.Stick,
                        "Stick",
                        StickDamage,
                        StickMinRange,
                        StickMaxRange,
                        StickUseDelaySeconds,
                        StickPriceCredits,
                        false,
                        true,
                        false,
                        false,
                        false,
                        1f);
                case EquipmentItemKind.Musket:
                    return Weapon(
                        EquipmentItemKind.Musket,
                        "Musket",
                        MusketDamage,
                        MusketMinRange,
                        MusketMaxRange,
                        MusketUseDelaySeconds,
                        MusketPriceCredits,
                        true,
                        false,
                        true,
                        true,
                        false,
                        MusketPrecisionAimMoveMultiplier);
                case EquipmentItemKind.Shotgun:
                    return Weapon(
                        EquipmentItemKind.Shotgun,
                        "Shotgun",
                        ShotgunDamage,
                        ShotgunMinRange,
                        ShotgunMaxRange,
                        ShotgunUseDelaySeconds,
                        ShotgunPriceCredits,
                        true,
                        false,
                        false,
                        true,
                        true,
                        1f);
                case EquipmentItemKind.MiniFlamethrower:
                    return Weapon(
                        EquipmentItemKind.MiniFlamethrower,
                        "Mini Flamethrower",
                        MiniFlamethrowerDamage,
                        MiniFlamethrowerMinRange,
                        MiniFlamethrowerMaxRange,
                        MiniFlamethrowerUseDelaySeconds,
                        MiniFlamethrowerPriceCredits,
                        true,
                        false,
                        false,
                        false,
                        true,
                        1f);
                case EquipmentItemKind.ElectricBaton:
                    return Weapon(
                        EquipmentItemKind.ElectricBaton,
                        "Electric Baton",
                        ElectricBatonDamage,
                        ElectricBatonMinRange,
                        ElectricBatonMaxRange,
                        ElectricBatonUseDelaySeconds,
                        ElectricBatonPriceCredits,
                        false,
                        false,
                        false,
                        false,
                        false,
                        1f);
                case EquipmentItemKind.Dagger:
                    return Weapon(
                        EquipmentItemKind.Dagger,
                        "Dagger",
                        DaggerDamage,
                        DaggerMinRange,
                        DaggerMaxRange,
                        DaggerUseDelaySeconds,
                        DaggerPriceCredits,
                        false,
                        true,
                        false,
                        false,
                        false,
                        1f);
                case EquipmentItemKind.BasicProtectiveSuit:
                    return new EquipmentItemDefinition(
                        EquipmentItemKind.BasicProtectiveSuit,
                        "Basic Protective Suit",
                        EquipmentItemCategory.ProtectiveGear,
                        0,
                        0f,
                        0f,
                        0f,
                        0,
                        false,
                        false,
                        false,
                        false,
                        false,
                        1f,
                        EquipmentStorageTarget.SupplyOnly,
                        1,
                        100,
                        true,
                        EquipmentAvailability.StartingLoadout);
                case EquipmentItemKind.SuppressionShield:
                    return SupplyItem(itemKind, "Suppression Shield", EquipmentItemCategory.ProtectiveGear, 600, true);
                case EquipmentItemKind.ProtectiveSuit:
                    return SupplyItem(itemKind, "Protective Suit", EquipmentItemCategory.ProtectiveGear, 400, true);
                case EquipmentItemKind.InsulatedSuit:
                    return SupplyItem(itemKind, "Insulated Suit", EquipmentItemCategory.ProtectiveGear, 700, true);
                case EquipmentItemKind.FireproofSuit:
                    return SupplyItem(itemKind, "Fireproof Suit", EquipmentItemCategory.ProtectiveGear, 700, true);
                case EquipmentItemKind.HeadProtector:
                    return SupplyItem(itemKind, "Head Protector", EquipmentItemCategory.ProtectiveGear, 850, true);
                case EquipmentItemKind.InjuryReliever:
                    return SupplyItem(itemKind, "Injury Reliever", EquipmentItemCategory.Treatment, InjuryRelieverPriceCredits, false, 5);
                case EquipmentItemKind.BandageSet:
                    return SupplyItem(itemKind, "Hemostatic Bandage Set", EquipmentItemCategory.Treatment, 75, false, 5);
                case EquipmentItemKind.AuxiliaryBattery:
                    return SupplyItem(itemKind, "Auxiliary Battery", EquipmentItemCategory.Treatment, 180, false, 4);
                case EquipmentItemKind.ShieldChargeBattery:
                    return SupplyItem(itemKind, "Shield Charge Battery", EquipmentItemCategory.Treatment, 250, false, 4);
                case EquipmentItemKind.ShieldConverter:
                    return SupplyItem(itemKind, "Shield Converter", EquipmentItemCategory.Treatment, 175, false, 4);
                case EquipmentItemKind.MoveSpeedEnhancer:
                    return SupplyItem(itemKind, "Move Speed Enhancer", EquipmentItemCategory.Enhancement, 65, false, 5);
                case EquipmentItemKind.StrengthEnhancer:
                    return SupplyItem(itemKind, "Strength Enhancer", EquipmentItemCategory.Enhancement, 100, false, 5);
                case EquipmentItemKind.ProtectiveEnhancer:
                    return SupplyItem(itemKind, "Protective Enhancer", EquipmentItemCategory.Enhancement, 160, false, 5);
                case EquipmentItemKind.FocusEnhancer:
                    return SupplyItem(itemKind, "Focus Enhancer", EquipmentItemCategory.Enhancement, 180, false, 5);
                case EquipmentItemKind.ShieldSurgeInducer:
                    return SupplyItem(itemKind, "Shield Surge Inducer", EquipmentItemCategory.Enhancement, 150, false, 5);
                case EquipmentItemKind.VacuumCleaner:
                    return Utility(itemKind, "Vacuum Cleaner", 80, false);
                case EquipmentItemKind.PortableSpeaker:
                    return Utility(itemKind, "Portable Speaker", 200, false);
                case EquipmentItemKind.HologramSpray:
                    return Utility(itemKind, "Hologram Spray", 250, false);
                case EquipmentItemKind.Flashbang:
                    return Utility(itemKind, "Flashbang", 100, false, 3);
                case EquipmentItemKind.Flashlight:
                    return Utility(itemKind, "Flashlight", FlashlightPriceCredits, false);
                case EquipmentItemKind.TemporaryOpenerSet:
                    return Utility(itemKind, "Temporary Opener Set", 180, false, 2);
                case EquipmentItemKind.PhysicalProtectiveSuit:
                    return SupplyItem(
                        itemKind,
                        "Physical Protective Suit",
                        EquipmentItemCategory.ProtectiveGear,
                        1250,
                        true,
                        1,
                        EquipmentAvailability.FameRestrictedShop);
                case EquipmentItemKind.NanomachineTreatment:
                    return SupplyItem(
                        itemKind,
                        "Nanomachine Treatment",
                        EquipmentItemCategory.Treatment,
                        300,
                        false,
                        3,
                        EquipmentAvailability.FameRestrictedShop);
                case EquipmentItemKind.RapidShieldBuffer:
                    return SupplyItem(
                        itemKind,
                        "Rapid Shield Buffer",
                        EquipmentItemCategory.Enhancement,
                        350,
                        false,
                        4,
                        EquipmentAvailability.FameRestrictedShop);
                case EquipmentItemKind.RepairDevice:
                    return SupplyItem(
                        itemKind,
                        "Repair Device",
                        EquipmentItemCategory.Utility,
                        900,
                        true,
                        1,
                        EquipmentAvailability.FameRestrictedShop);
                case EquipmentItemKind.MarkerSpray:
                    return Utility(
                        itemKind,
                        "Marker Spray",
                        500,
                        true,
                        1,
                        EquipmentAvailability.FameRestrictedShop);
                case EquipmentItemKind.PresenceDetector:
                    return SupplyItem(
                        itemKind,
                        "Presence Detector",
                        EquipmentItemCategory.Utility,
                        1000,
                        true,
                        1,
                        EquipmentAvailability.SpecialUnlock);
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemKind), itemKind, null);
            }
        }

        public static EquipmentShopCatalogEntry[] CreatePhase15BuyCatalog()
        {
            return CloneCatalog(Phase15BuyCatalog);
        }

        public static EquipmentShopCatalogEntry[] CreatePhase15SellCatalog()
        {
            return CloneCatalog(Phase15SellCatalog);
        }

        public static EquipmentItemCategory[] GetStorageTabOrder()
        {
            return (EquipmentItemCategory[])StorageTabOrder.Clone();
        }

        public static EquipmentShopCatalogEntry[] FilterCatalogByCategory(
            EquipmentShopCatalogEntry[] catalog,
            EquipmentItemCategory category)
        {
            if (catalog == null || catalog.Length == 0)
            {
                return new EquipmentShopCatalogEntry[0];
            }

            if (category == EquipmentItemCategory.None)
            {
                return CloneCatalog(catalog);
            }

            var count = 0;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (catalog[i].Category == category)
                {
                    count++;
                }
            }

            var filtered = new EquipmentShopCatalogEntry[count];
            var index = 0;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (catalog[i].Category == category)
                {
                    filtered[index] = catalog[i];
                    index++;
                }
            }

            return filtered;
        }

        public static EquipmentShopCatalogEntry[] FilterCatalogByAvailability(
            EquipmentShopCatalogEntry[] catalog,
            EquipmentAvailability availability)
        {
            if (catalog == null || catalog.Length == 0)
            {
                return new EquipmentShopCatalogEntry[0];
            }

            var count = 0;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (catalog[i].Availability == availability)
                {
                    count++;
                }
            }

            var filtered = new EquipmentShopCatalogEntry[count];
            var index = 0;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (catalog[i].Availability == availability)
                {
                    filtered[index] = catalog[i];
                    index++;
                }
            }

            return filtered;
        }

        public static PlayerEquipmentState Tick(PlayerEquipmentState state, float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (deltaSeconds <= 0f)
            {
                return state;
            }

            var next = state.UseCooldownSeconds <= 0f
                ? state
                : state.WithCooldown(Math.Max(0f, state.UseCooldownSeconds - deltaSeconds));
            var electricBatonCooldown = next.ElectricBatonChargeCooldownSeconds;
            if (HasHandItem(next, EquipmentItemKind.ElectricBaton))
            {
                electricBatonCooldown = Math.Max(0f, electricBatonCooldown - deltaSeconds);
            }

            var miniFlamethrowerContinuousHitSeconds = next.MiniFlamethrowerContinuousHitSeconds;
            var miniFlamethrowerHitGapSeconds = next.MiniFlamethrowerHitGapSeconds;
            if (miniFlamethrowerContinuousHitSeconds > 0.0001f)
            {
                miniFlamethrowerHitGapSeconds += deltaSeconds;
                if (miniFlamethrowerHitGapSeconds >
                    MiniFlamethrowerUseDelaySeconds + MiniFlamethrowerSustainedHitGraceSeconds)
                {
                    miniFlamethrowerContinuousHitSeconds = 0f;
                    miniFlamethrowerHitGapSeconds = 0f;
                }
            }

            return next.WithTimedEffects(
                Math.Max(0f, next.StrengthEnhancerRemainingSeconds - deltaSeconds),
                Math.Max(0f, next.FlashlightRemainingSeconds - deltaSeconds),
                electricBatonCooldown,
                miniFlamethrowerContinuousHitSeconds,
                miniFlamethrowerHitGapSeconds);
        }

        public static EquipmentUseResult UseActiveEquipment(
            PlayerEquipmentState state,
            bool alternateMode,
            bool hasIntruderTarget)
        {
            return UseActiveEquipment(state, alternateMode, hasIntruderTarget, null);
        }

        public static EquipmentUseResult UseActiveEquipment(
            PlayerEquipmentState state,
            bool alternateMode,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects)
        {
            var slot = state.ActiveHandSlot;
            if (slot.IsEmpty)
            {
                var noItemState = state.WithModeAndSummary(EquipmentUseMode.None, "No item is equipped in the active hand slot.");
                return new EquipmentUseResult(
                    noItemState,
                    EquipmentUseOutcome.NoItem,
                    EquipmentItemKind.None,
                    EquipmentUseMode.None,
                    0,
                    noItemState.LastActionSummary);
            }

            if (CombatStatusEffectRules.BlocksActions(playerStatusEffects))
            {
                var blockedState = state.WithModeAndSummary(
                    state.ActiveMode,
                    "Player action is blocked by " + CombatStatusEffectRules.BuildHudSummary(playerStatusEffects) + ".");
                return new EquipmentUseResult(
                    blockedState,
                    EquipmentUseOutcome.ActionBlocked,
                    slot.ItemKind,
                    state.ActiveMode,
                    0,
                    blockedState.LastActionSummary);
            }

            if (state.UseCooldownSeconds > 0.0001f)
            {
                var blockedState = state.WithModeAndSummary(state.ActiveMode, "Weapon is cooling down.");
                return new EquipmentUseResult(
                    blockedState,
                    EquipmentUseOutcome.CooldownBlocked,
                    slot.ItemKind,
                    state.ActiveMode,
                    0,
                    blockedState.LastActionSummary);
            }

            switch (slot.ItemKind)
            {
                case EquipmentItemKind.Stick:
                    return UseStick(state, alternateMode, hasIntruderTarget, playerStatusEffects);
                case EquipmentItemKind.Musket:
                    return UseMusket(state, alternateMode, hasIntruderTarget, playerStatusEffects);
                case EquipmentItemKind.Shotgun:
                    return UseRangedWeapon(state, EquipmentItemKind.Shotgun, hasIntruderTarget, playerStatusEffects);
                case EquipmentItemKind.MiniFlamethrower:
                    return UseMiniFlamethrower(state, hasIntruderTarget, playerStatusEffects);
                case EquipmentItemKind.ElectricBaton:
                    return UseElectricBaton(state, hasIntruderTarget, playerStatusEffects);
                case EquipmentItemKind.Dagger:
                    return alternateMode
                        ? UseThrownDagger(state, hasIntruderTarget, playerStatusEffects)
                        : UseMeleeWeapon(state, EquipmentItemKind.Dagger, hasIntruderTarget, playerStatusEffects);
                case EquipmentItemKind.Flashbang:
                    return UseFlashbang(state, hasIntruderTarget);
                case EquipmentItemKind.Flashlight:
                    return UseFlashlight(state);
                default:
                    var unsupportedState = state.WithModeAndSummary(EquipmentUseMode.None, "Equipped item cannot be used as a weapon yet.");
                    return new EquipmentUseResult(
                        unsupportedState,
                        EquipmentUseOutcome.NoItem,
                        slot.ItemKind,
                        EquipmentUseMode.None,
                        0,
                        unsupportedState.LastActionSummary);
            }
        }

        public static EquipmentUseResult UseSupplyItem(PlayerEquipmentState state, int supplySlotIndex)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            if (slot.IsEmpty)
            {
                var noItemState = state.WithModeAndSummary(state.ActiveMode, "No item is stored in the selected supply slot.");
                return new EquipmentUseResult(
                    noItemState,
                    EquipmentUseOutcome.NoItem,
                    EquipmentItemKind.None,
                    state.ActiveMode,
                    0,
                    noItemState.LastActionSummary);
            }

            switch (slot.ItemKind)
            {
                case EquipmentItemKind.SuppressionShield:
                    return UseProtectiveSupplyItem(state, supplySlotIndex, SuppressionShieldReductionPercent);
                case EquipmentItemKind.ProtectiveSuit:
                    return UseProtectiveSupplyItem(state, supplySlotIndex, ProtectiveSuitReductionPercent);
                case EquipmentItemKind.InsulatedSuit:
                    return UseProtectiveSupplyItem(state, supplySlotIndex, InsulatedSuitReductionPercent);
                case EquipmentItemKind.FireproofSuit:
                    return UseProtectiveSupplyItem(state, supplySlotIndex, FireproofSuitReductionPercent);
                case EquipmentItemKind.HeadProtector:
                    return UseProtectiveSupplyItem(state, supplySlotIndex, HeadProtectorReductionPercent);
                case EquipmentItemKind.PhysicalProtectiveSuit:
                    return UseProtectiveSupplyItem(state, supplySlotIndex, PhysicalProtectiveSuitReductionPercent);
                case EquipmentItemKind.InjuryReliever:
                    return UseTreatmentSupplyItem(state, supplySlotIndex, InjuryRelieverHealAmount, 0);
                case EquipmentItemKind.BandageSet:
                    return UseTreatmentSupplyItem(
                        state,
                        supplySlotIndex,
                        BandageSetHealAmount,
                        0,
                        CombatStatusEffectKind.Bleeding);
                case EquipmentItemKind.AuxiliaryBattery:
                    return UseTreatmentSupplyItem(state, supplySlotIndex, 0, AuxiliaryBatteryShieldAmount);
                case EquipmentItemKind.ShieldChargeBattery:
                    return UseTreatmentSupplyItem(state, supplySlotIndex, 0, ShieldChargeBatteryShieldAmount);
                case EquipmentItemKind.NanomachineTreatment:
                    return UseTreatmentSupplyItem(state, supplySlotIndex, NanomachineTreatmentHealAmount, 0);
                case EquipmentItemKind.RapidShieldBuffer:
                    return UseTreatmentSupplyItem(state, supplySlotIndex, 0, RapidShieldBufferShieldAmount);
                case EquipmentItemKind.MoveSpeedEnhancer:
                    return UseMoveSpeedEnhancer(state, supplySlotIndex);
                case EquipmentItemKind.StrengthEnhancer:
                    return UseStrengthEnhancer(state, supplySlotIndex);
                case EquipmentItemKind.ProtectiveEnhancer:
                    return UseProtectiveEnhancer(state, supplySlotIndex);
                case EquipmentItemKind.FocusEnhancer:
                    return UseFocusEnhancer(state, supplySlotIndex);
                default:
                    var definition = GetDefinition(slot.ItemKind);
                    var unsupportedState = state.WithModeAndSummary(
                        state.ActiveMode,
                        definition.DisplayName + " has no active use in Step 8 yet.");
                    return new EquipmentUseResult(
                        unsupportedState,
                        EquipmentUseOutcome.NoItem,
                        slot.ItemKind,
                        state.ActiveMode,
                        0,
                        unsupportedState.LastActionSummary);
            }
        }

        public static int CalculateDamageAfterProtection(int rawDamage, PlayerEquipmentState equipment)
        {
            if (rawDamage <= 0)
            {
                return 0;
            }

            return Math.Max(0, rawDamage - rawDamage * CalculateDamageReductionPercent(equipment) / 100);
        }

        public static int CalculateDamageReductionPercent(PlayerEquipmentState equipment)
        {
            var reduction = equipment.HasBasicProtectiveSuit ? 10 : 0;
            reduction += equipment.ActiveDamageReductionPercent;
            return Math.Min(80, reduction);
        }

        public static EquipmentUseResult ReloadActiveEquipment(PlayerEquipmentState state)
        {
            var slot = state.ActiveHandSlot;
            if (slot.IsEmpty)
            {
                var noItemState = state.WithModeAndSummary(EquipmentUseMode.None, "No item is equipped for reload.");
                return new EquipmentUseResult(
                    noItemState,
                    EquipmentUseOutcome.NoItem,
                    EquipmentItemKind.None,
                    EquipmentUseMode.None,
                    0,
                    noItemState.LastActionSummary);
            }

            var definition = GetDefinition(slot.ItemKind);
            if (!definition.HasReloadInputSkeleton)
            {
                var notReloadableState = state.WithModeAndSummary(state.ActiveMode, definition.DisplayName + " has no reload action.");
                return new EquipmentUseResult(
                    notReloadableState,
                    EquipmentUseOutcome.NoItem,
                    slot.ItemKind,
                    state.ActiveMode,
                    0,
                    notReloadableState.LastActionSummary);
            }

            var reloadState = state.WithModeAndSummary(
                EquipmentUseMode.Primary,
                definition.DisplayName + " reload input received; magazine size and reload time are pending confirmation.");
            return new EquipmentUseResult(
                reloadState,
                EquipmentUseOutcome.ReloadSkeleton,
                slot.ItemKind,
                EquipmentUseMode.Primary,
                0,
                reloadState.LastActionSummary);
        }

        public static EquipmentUseResult DropActiveHandItem(PlayerEquipmentState state)
        {
            var slot = state.ActiveHandSlot;
            if (slot.IsEmpty)
            {
                var noItemState = state.WithModeAndSummary(EquipmentUseMode.None, "No item is equipped to drop.");
                return new EquipmentUseResult(
                    noItemState,
                    EquipmentUseOutcome.NoItem,
                    EquipmentItemKind.None,
                    EquipmentUseMode.None,
                    0,
                    noItemState.LastActionSummary);
            }

            var definition = GetDefinition(slot.ItemKind);
            var nextState = state
                .WithHandSlot(state.ActiveHandSlotIndex, EquipmentSlotState.Empty)
                .WithCooldownModeAndSummary(0f, EquipmentUseMode.None, "Dropped " + definition.DisplayName + ".");
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.Dropped,
                slot.ItemKind,
                EquipmentUseMode.None,
                0,
                nextState.LastActionSummary);
        }

        public static EquipmentPurchaseResult PurchaseItem(
            PlayerEquipmentState state,
            EquipmentItemKind itemKind)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                return new EquipmentPurchaseResult(
                    state,
                    false,
                    0,
                    itemKind,
                    "This shop entry is informational.");
            }

            var definition = GetDefinition(itemKind);
            if (definition.Availability == EquipmentAvailability.StartingLoadout)
            {
                return new EquipmentPurchaseResult(
                    state.WithBasicProtectiveSuit(true).WithModeAndSummary(state.ActiveMode, definition.DisplayName + " is part of the starting state."),
                    false,
                    0,
                    itemKind,
                    definition.DisplayName + " is already part of the starting state.");
            }

            if (definition.Availability == EquipmentAvailability.SpecialUnlock)
            {
                return new EquipmentPurchaseResult(
                    state,
                    false,
                    0,
                    itemKind,
                    definition.DisplayName + " requires a special contract unlock before purchase.");
            }

            if (definition.IsUniquePerShip && state.HasAnyItem(itemKind))
            {
                return new EquipmentPurchaseResult(
                    state,
                    false,
                    0,
                    itemKind,
                    definition.DisplayName + " is already held or stored.");
            }

            PlayerEquipmentState stackState;
            EquipmentPurchaseResult result;
            switch (definition.StorageTarget)
            {
                case EquipmentStorageTarget.HandOnly:
                    if (TryStackHandItem(state, definition, out stackState))
                    {
                        return PurchaseSucceeded(stackState, definition, "Purchased and stacked " + definition.DisplayName + ".");
                    }

                    return TryStoreInHand(state, definition, out result)
                        ? result
                        : PurchaseFailed(state, itemKind, "No hand slot is available.");
                case EquipmentStorageTarget.SupplyOnly:
                    if (TryStackSupplyItem(state, definition, out stackState))
                    {
                        return PurchaseSucceeded(stackState, definition, "Purchased and stacked " + definition.DisplayName + ".");
                    }

                    return TryStoreInSupply(state, definition, out result)
                        ? result
                        : PurchaseFailed(state, itemKind, "No supply slot is available.");
                default:
                    if (TryStackHandItem(state, definition, out stackState))
                    {
                        return PurchaseSucceeded(stackState, definition, "Purchased and stacked " + definition.DisplayName + ".");
                    }

                    if (TryStoreInHand(state, definition, out result))
                    {
                        return result;
                    }

                    if (TryStackSupplyItem(state, definition, out stackState))
                    {
                        return PurchaseSucceeded(stackState, definition, "Purchased and stacked " + definition.DisplayName + ".");
                    }

                    return TryStoreInSupply(state, definition, out result)
                        ? result
                        : PurchaseFailed(state, itemKind, "No hand or supply slot is available.");
            }
        }

        public static EquipmentDisposalResult DisposeFirstPurchasedItem(PlayerEquipmentState state)
        {
            for (var i = 0; i < state.UnlockedHandSlotCount; i++)
            {
                var slot = state.GetHandSlot(i);
                if (!slot.WasPurchased)
                {
                    continue;
                }

                return DisposePurchasedHandItem(state, i);
            }

            for (var i = 0; i < state.UnlockedSupplySlotCount; i++)
            {
                var slot = state.GetSupplySlot(i);
                if (!slot.WasPurchased)
                {
                    continue;
                }

                return DisposePurchasedSupplyItem(state, i);
            }

            return new EquipmentDisposalResult(
                state,
                false,
                0,
                EquipmentItemKind.None,
                "No purchased item is available for disposal.");
        }

        public static EquipmentDisposalResult DisposePurchasedHandItem(PlayerEquipmentState state, int handSlotIndex)
        {
            var slot = state.GetHandSlot(handSlotIndex);
            if (!slot.WasPurchased)
            {
                return new EquipmentDisposalResult(
                    state,
                    false,
                    0,
                    slot.ItemKind,
                    "Selected hand slot has no purchased item for disposal.");
            }

            var credits = CalculateDisposalCredits(slot);
            var nextSlot = RemoveOne(slot);
            var nextMode = handSlotIndex == state.ActiveHandSlotIndex && nextSlot.IsEmpty
                ? EquipmentUseMode.None
                : state.ActiveMode;
            var nextState = state
                .WithHandSlot(handSlotIndex, nextSlot)
                .WithCooldownModeAndSummary(0f, nextMode, "Disposed " + FormatItemName(slot.ItemKind) + " for " + FormatMoney(credits) + ".");
            return new EquipmentDisposalResult(
                nextState,
                true,
                credits,
                slot.ItemKind,
                nextState.LastActionSummary);
        }

        public static EquipmentDisposalResult DisposePurchasedSupplyItem(PlayerEquipmentState state, int supplySlotIndex)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            if (!slot.WasPurchased)
            {
                return new EquipmentDisposalResult(
                    state,
                    false,
                    0,
                    slot.ItemKind,
                    "Selected supply slot has no purchased item for disposal.");
            }

            var credits = CalculateDisposalCredits(slot);
            var nextState = state
                .WithSupplySlot(supplySlotIndex, RemoveOne(slot))
                .WithModeAndSummary(state.ActiveMode, "Disposed " + FormatItemName(slot.ItemKind) + " for " + FormatMoney(credits) + ".");
            return new EquipmentDisposalResult(
                nextState,
                true,
                credits,
                slot.ItemKind,
                nextState.LastActionSummary);
        }

        public static int CalculateDisposalCredits(EquipmentSlotState slot)
        {
            if (!slot.WasPurchased)
            {
                return 0;
            }

            return Math.Max(1, slot.PurchasePriceCredits / 100);
        }

        public static EquipmentSlotState ApplyDurabilityDamage(EquipmentSlotState slot, int damagePercent)
        {
            if (damagePercent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damagePercent), "Equipment durability damage cannot be negative.");
            }

            if (slot.IsEmpty || damagePercent == 0)
            {
                return slot;
            }

            var nextDurability = Math.Max(0, slot.DurabilityPercent - damagePercent);
            return nextDurability <= 0
                ? EquipmentSlotState.Empty
                : slot.WithDurabilityPercent(nextDurability);
        }

        public static string FormatItemName(EquipmentItemKind itemKind)
        {
            return itemKind == EquipmentItemKind.None
                ? "Empty"
                : GetDefinition(itemKind).DisplayName;
        }

        public static string FormatCategoryTabName(EquipmentItemCategory category)
        {
            switch (category)
            {
                case EquipmentItemCategory.None:
                    return "All";
                case EquipmentItemCategory.Weapon:
                    return "Weapon";
                case EquipmentItemCategory.ProtectiveGear:
                    return "Protective";
                case EquipmentItemCategory.Treatment:
                    return "Treatment";
                case EquipmentItemCategory.Enhancement:
                    return "Enhancement";
                case EquipmentItemCategory.Utility:
                    return "Utility";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }

        public static string FormatAvailabilityName(EquipmentAvailability availability)
        {
            switch (availability)
            {
                case EquipmentAvailability.CommonShop:
                    return "Common";
                case EquipmentAvailability.FameRestrictedShop:
                    return "Fame";
                case EquipmentAvailability.SpecialUnlock:
                    return "Special";
                case EquipmentAvailability.StartingLoadout:
                    return "Start";
                default:
                    throw new ArgumentOutOfRangeException(nameof(availability), availability, null);
            }
        }

        private static EquipmentUseResult UseStick(
            PlayerEquipmentState state,
            bool alternateMode,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects)
        {
            if (alternateMode)
            {
                var throwState = state.WithModeAndSummary(
                    EquipmentUseMode.Throwing,
                    "Stick throw mode input received; thrown physics and recovery are pending later implementation.");
                return new EquipmentUseResult(
                    throwState,
                    EquipmentUseOutcome.ThrowSkeleton,
                    EquipmentItemKind.Stick,
                    EquipmentUseMode.Throwing,
                    0,
                    throwState.LastActionSummary);
            }

            var outcome = hasIntruderTarget ? EquipmentUseOutcome.MeleeHit : EquipmentUseOutcome.MeleeMiss;
            var damage = CalculateWeaponDamage(state, EquipmentItemKind.Stick, StickDamage);
            var summary = hasIntruderTarget
                ? "Stick hit active intruder for " + damage + " damage."
                : "Stick swing found no active intruder target.";
            var cooldown = CombatStatusEffectRules.CalculateWeaponDelay(playerStatusEffects, false, StickUseDelaySeconds);
            var nextState = ApplyActiveHandUseDurability(state, 1)
                .WithCooldownModeAndSummary(cooldown, EquipmentUseMode.Primary, summary);
            return new EquipmentUseResult(
                nextState,
                outcome,
                EquipmentItemKind.Stick,
                EquipmentUseMode.Primary,
                hasIntruderTarget ? damage : 0,
                summary);
        }

        private static EquipmentUseResult UseMusket(
            PlayerEquipmentState state,
            bool alternateMode,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects)
        {
            var mode = alternateMode ? EquipmentUseMode.PrecisionAim : EquipmentUseMode.Primary;
            var outcome = hasIntruderTarget ? EquipmentUseOutcome.RangedHit : EquipmentUseOutcome.RangedMiss;
            var damage = CalculateWeaponDamage(state, EquipmentItemKind.Musket, MusketDamage);
            var summary = hasIntruderTarget
                ? "Musket fired at active intruder for " + damage + " damage."
                : "Musket fired with no active intruder target.";
            var cooldown = CombatStatusEffectRules.CalculateWeaponDelay(playerStatusEffects, true, MusketUseDelaySeconds);
            var nextState = ApplyActiveHandUseDurability(state, 1)
                .WithCooldownModeAndSummary(cooldown, mode, summary);
            return new EquipmentUseResult(
                nextState,
                outcome,
                EquipmentItemKind.Musket,
                mode,
                hasIntruderTarget ? damage : 0,
                summary);
        }

        private static EquipmentUseResult UseMeleeWeapon(
            PlayerEquipmentState state,
            EquipmentItemKind itemKind,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects,
            CombatStatusEffectApplication statusEffectToApply = default,
            int damageOverride = -1)
        {
            var definition = GetDefinition(itemKind);
            var outcome = hasIntruderTarget ? EquipmentUseOutcome.MeleeHit : EquipmentUseOutcome.MeleeMiss;
            var damage = damageOverride >= 0 ? damageOverride : CalculateWeaponDamage(state, itemKind, definition.Damage);
            var summary = hasIntruderTarget
                ? definition.DisplayName + " hit active intruder for " + damage + " damage."
                : definition.DisplayName + " attack found no active intruder target.";
            if (hasIntruderTarget && statusEffectToApply.HasEffect)
            {
                summary += " " + CombatStatusEffectRules.FormatEffectName(statusEffectToApply.Kind) + " applied.";
            }

            var cooldown = CombatStatusEffectRules.CalculateWeaponDelay(
                playerStatusEffects,
                false,
                definition.UseDelaySeconds);
            var nextState = ApplyActiveHandUseDurability(state, 1)
                .WithCooldownModeAndSummary(cooldown, EquipmentUseMode.Primary, summary);
            return new EquipmentUseResult(
                nextState,
                outcome,
                itemKind,
                EquipmentUseMode.Primary,
                hasIntruderTarget ? damage : 0,
                summary,
                statusEffectToApply: hasIntruderTarget ? statusEffectToApply : default);
        }

        private static EquipmentUseResult UseRangedWeapon(
            PlayerEquipmentState state,
            EquipmentItemKind itemKind,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects,
            CombatStatusEffectApplication statusEffectToApply = default)
        {
            var definition = GetDefinition(itemKind);
            var outcome = hasIntruderTarget ? EquipmentUseOutcome.RangedHit : EquipmentUseOutcome.RangedMiss;
            var damage = CalculateWeaponDamage(state, itemKind, definition.Damage);
            var summary = hasIntruderTarget
                ? definition.DisplayName + " hit active intruder for " + damage + " damage."
                : definition.DisplayName + " fired with no active intruder target.";
            if (hasIntruderTarget && statusEffectToApply.HasEffect)
            {
                summary += " " + CombatStatusEffectRules.FormatEffectName(statusEffectToApply.Kind) + " applied.";
            }

            var cooldown = CombatStatusEffectRules.CalculateWeaponDelay(
                playerStatusEffects,
                true,
                definition.UseDelaySeconds);
            var nextState = ApplyActiveHandUseDurability(state, 1)
                .WithCooldownModeAndSummary(cooldown, EquipmentUseMode.Primary, summary);
            return new EquipmentUseResult(
                nextState,
                outcome,
                itemKind,
                EquipmentUseMode.Primary,
                hasIntruderTarget ? damage : 0,
                summary,
                statusEffectToApply: hasIntruderTarget ? statusEffectToApply : default);
        }

        private static EquipmentUseResult UseThrownDagger(
            PlayerEquipmentState state,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects = null)
        {
            var damage = CalculateWeaponDamage(state, EquipmentItemKind.Dagger, DaggerDamage);
            var outcome = hasIntruderTarget ? EquipmentUseOutcome.RangedHit : EquipmentUseOutcome.RangedMiss;
            var summary = hasIntruderTarget
                ? "Dagger throw hit active intruder for " + damage + " damage."
                : "Dagger throw found no active intruder target.";
            var cooldown = CombatStatusEffectRules.CalculateWeaponDelay(playerStatusEffects, true, DaggerUseDelaySeconds);
            var nextState = ApplyActiveHandUseDurability(state, 3)
                .WithCooldownModeAndSummary(cooldown, EquipmentUseMode.Throwing, summary);
            return new EquipmentUseResult(
                nextState,
                outcome,
                EquipmentItemKind.Dagger,
                EquipmentUseMode.Throwing,
                hasIntruderTarget ? damage : 0,
                summary);
        }

        private static EquipmentUseResult UseMiniFlamethrower(
            PlayerEquipmentState state,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects)
        {
            var continuousHitSeconds = 0f;
            var statusEffect = default(CombatStatusEffectApplication);
            if (hasIntruderTarget)
            {
                continuousHitSeconds =
                    state.MiniFlamethrowerHitGapSeconds <=
                    MiniFlamethrowerUseDelaySeconds + MiniFlamethrowerSustainedHitGraceSeconds
                        ? state.MiniFlamethrowerContinuousHitSeconds + MiniFlamethrowerUseDelaySeconds
                        : MiniFlamethrowerUseDelaySeconds;
                if (continuousHitSeconds + 0.0001f >= CombatStatusEffectRules.MiniFlamethrowerBurnTriggerSeconds)
                {
                    statusEffect = CombatStatusEffectRules.CreateBurn(
                        CombatStatusEffectRules.BurnDefaultDurationSeconds,
                        CombatStatusEffectRules.BurnDefaultTickDamage);
                }
            }

            var result = UseRangedWeapon(
                state,
                EquipmentItemKind.MiniFlamethrower,
                hasIntruderTarget,
                playerStatusEffects,
                statusEffect);
            var nextState = result.State.WithMiniFlamethrowerHitWindow(
                hasIntruderTarget ? continuousHitSeconds : 0f,
                0f,
                result.Summary);
            return new EquipmentUseResult(
                nextState,
                result.Outcome,
                result.ItemKind,
                result.Mode,
                result.Damage,
                result.Summary,
                effectDurationSeconds: result.StatusEffectToApply.DurationSeconds,
                statusEffectToApply: result.StatusEffectToApply);
        }

        private static EquipmentUseResult UseElectricBaton(
            PlayerEquipmentState state,
            bool hasIntruderTarget,
            CombatStatusEffectState[] playerStatusEffects)
        {
            var charged = state.IsElectricBatonCharged;
            var consumedCharge = hasIntruderTarget && charged;
            var baseDamage = CalculateWeaponDamage(state, EquipmentItemKind.ElectricBaton, ElectricBatonDamage);
            var damage = consumedCharge
                ? baseDamage + CombatStatusEffectRules.ElectricBatonChargedDamageBonus
                : baseDamage;
            var statusEffect = consumedCharge
                ? CombatStatusEffectRules.CreateStopped(
                    CombatStatusEffectRules.ElectricBatonChargedStoppedDurationSeconds)
                : default;
            var result = UseMeleeWeapon(
                state,
                EquipmentItemKind.ElectricBaton,
                hasIntruderTarget,
                playerStatusEffects,
                statusEffect,
                damage);
            var nextCooldown = consumedCharge
                ? CombatStatusEffectRules.ElectricBatonChargeCooldownSeconds
                : state.ElectricBatonChargeCooldownSeconds;
            var nextState = result.State.WithElectricBatonChargeCooldown(nextCooldown, result.Summary);
            return new EquipmentUseResult(
                nextState,
                result.Outcome,
                result.ItemKind,
                result.Mode,
                result.Damage,
                result.Summary,
                effectDurationSeconds: result.StatusEffectToApply.DurationSeconds,
                statusEffectToApply: result.StatusEffectToApply);
        }

        private static EquipmentUseResult UseFlashbang(PlayerEquipmentState state, bool hasIntruderTarget)
        {
            var status = hasIntruderTarget
                ? CombatStatusEffectRules.CreateConfusion(
                    CombatStatusEffectRules.FlashbangConfusionDurationSeconds)
                : default;
            var summary = hasIntruderTarget
                ? "Flashbang detonated; " + CombatStatusEffectRules.FormatEffectName(CombatStatusEffectKind.Confusion) + " applied."
                : "Flashbang detonated with no active intruder target.";
            var nextState = ApplyActiveHandUseDurability(state, 100)
                .WithCooldownModeAndSummary(0.5f, EquipmentUseMode.Throwing, summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.UtilityActivated,
                EquipmentItemKind.Flashbang,
                EquipmentUseMode.Throwing,
                hasIntruderTarget ? 5 : 0,
                summary,
                consumedItem: true,
                effectDurationSeconds: status.DurationSeconds,
                statusEffectToApply: status);
        }

        private static EquipmentUseResult UseFlashlight(PlayerEquipmentState state)
        {
            var summary = "Flashlight beam activated for " + MathfCeilToInt(FlashlightDurationSeconds) + " seconds.";
            var nextState = ApplyActiveHandUseDurability(state, 1)
                .WithCooldownModeAndSummary(0.5f, EquipmentUseMode.Primary, summary)
                .WithFlashlight(FlashlightDurationSeconds, summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.UtilityActivated,
                EquipmentItemKind.Flashlight,
                EquipmentUseMode.Primary,
                0,
                summary,
                effectDurationSeconds: FlashlightDurationSeconds);
        }

        private static EquipmentUseResult UseProtectiveSupplyItem(
            PlayerEquipmentState state,
            int supplySlotIndex,
            int damageReductionPercent)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            var definition = GetDefinition(slot.ItemKind);
            var summary = definition.DisplayName + " equipped; incoming damage reduction +" + damageReductionPercent + "%.";
            var nextState = ApplySupplySlotDurabilityDamage(state, supplySlotIndex, 5)
                .WithProtection(slot.ItemKind, damageReductionPercent, summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.ProtectiveEquipped,
                slot.ItemKind,
                state.ActiveMode,
                0,
                summary,
                damageReductionPercent: damageReductionPercent);
        }

        private static EquipmentUseResult UseTreatmentSupplyItem(
            PlayerEquipmentState state,
            int supplySlotIndex,
            int healthDelta,
            int shieldDelta,
            CombatStatusEffectKind statusEffectToClear = CombatStatusEffectKind.None)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            var definition = GetDefinition(slot.ItemKind);
            var summary = definition.DisplayName + " applied.";
            if (healthDelta > 0)
            {
                summary += " Health +" + healthDelta + ".";
            }

            if (shieldDelta > 0)
            {
                summary += " Shield +" + shieldDelta + ".";
            }

            if (statusEffectToClear != CombatStatusEffectKind.None)
            {
                summary += " Clears " + CombatStatusEffectRules.FormatEffectName(statusEffectToClear) + ".";
            }

            var nextState = state
                .WithSupplySlot(supplySlotIndex, RemoveOne(slot))
                .WithModeAndSummary(state.ActiveMode, summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.TreatmentApplied,
                slot.ItemKind,
                state.ActiveMode,
                0,
                summary,
                healthDelta,
                shieldDelta,
                true,
                statusEffectToClear: statusEffectToClear);
        }

        private static EquipmentUseResult UseMoveSpeedEnhancer(PlayerEquipmentState state, int supplySlotIndex)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            var summary = "Move Speed Enhancer applied for " +
                          MathfCeilToInt(MoveSpeedEnhancerDurationSeconds) +
                          " seconds; " +
                          CombatStatusEffectRules.FormatEffectName(CombatStatusEffectKind.Exhaustion) +
                          " follows.";
            var nextState = state
                .WithSupplySlot(supplySlotIndex, RemoveOne(slot))
                .WithModeAndSummary(state.ActiveMode, summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.EnhancementApplied,
                EquipmentItemKind.MoveSpeedEnhancer,
                state.ActiveMode,
                0,
                summary,
                consumedItem: true,
                effectDurationSeconds: MoveSpeedEnhancerDurationSeconds,
                delayedStatusEffectToApply: CombatStatusEffectRules.CreateExhaustion(
                    CombatStatusEffectRules.ExhaustionDefaultDurationSeconds),
                delayedStatusEffectDelaySeconds: MoveSpeedEnhancerDurationSeconds);
        }

        private static EquipmentUseResult UseStrengthEnhancer(PlayerEquipmentState state, int supplySlotIndex)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            var summary = "Strength Enhancer applied; weapon damage +" +
                          StrengthEnhancerDamageBonusPercent +
                          "% for " +
                          MathfCeilToInt(StrengthEnhancerDurationSeconds) +
                          " seconds.";
            var nextState = state
                .WithSupplySlot(supplySlotIndex, RemoveOne(slot))
                .WithStrengthEnhancer(
                    StrengthEnhancerDurationSeconds,
                    StrengthEnhancerDamageBonusPercent,
                    summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.EnhancementApplied,
                EquipmentItemKind.StrengthEnhancer,
                state.ActiveMode,
                0,
                summary,
                consumedItem: true,
                damageBonusPercent: StrengthEnhancerDamageBonusPercent,
                effectDurationSeconds: StrengthEnhancerDurationSeconds,
                delayedStatusEffectToApply: CombatStatusEffectRules.CreateFatigue(
                    CombatStatusEffectRules.FatigueDefaultDurationSeconds),
                delayedStatusEffectDelaySeconds: StrengthEnhancerDurationSeconds);
        }

        private static EquipmentUseResult UseProtectiveEnhancer(PlayerEquipmentState state, int supplySlotIndex)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            var summary = "Protective Enhancer applied; incoming damage reduction +" +
                          ProtectiveEnhancerReductionPercent +
                          "% for " +
                          MathfCeilToInt(ProtectiveEnhancerDurationSeconds) +
                          " seconds.";
            var nextState = state
                .WithSupplySlot(supplySlotIndex, RemoveOne(slot))
                .WithProtection(EquipmentItemKind.ProtectiveEnhancer, ProtectiveEnhancerReductionPercent, summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.EnhancementApplied,
                EquipmentItemKind.ProtectiveEnhancer,
                state.ActiveMode,
                0,
                summary,
                consumedItem: true,
                damageReductionPercent: ProtectiveEnhancerReductionPercent,
                effectDurationSeconds: ProtectiveEnhancerDurationSeconds);
        }

        private static EquipmentUseResult UseFocusEnhancer(PlayerEquipmentState state, int supplySlotIndex)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            var summary = "Focus Enhancer applied for " +
                          MathfCeilToInt(FocusEnhancerDurationSeconds) +
                          " seconds; " +
                          CombatStatusEffectRules.FormatEffectName(CombatStatusEffectKind.Dizziness) +
                          " follows.";
            var nextState = state
                .WithSupplySlot(supplySlotIndex, RemoveOne(slot))
                .WithModeAndSummary(state.ActiveMode, summary);
            return new EquipmentUseResult(
                nextState,
                EquipmentUseOutcome.EnhancementApplied,
                EquipmentItemKind.FocusEnhancer,
                state.ActiveMode,
                0,
                summary,
                consumedItem: true,
                effectDurationSeconds: FocusEnhancerDurationSeconds,
                delayedStatusEffectToApply: CombatStatusEffectRules.CreateDizziness(
                    CombatStatusEffectRules.DizzinessDefaultDurationSeconds),
                delayedStatusEffectDelaySeconds: FocusEnhancerDurationSeconds);
        }

        private static int CalculateWeaponDamage(
            PlayerEquipmentState state,
            EquipmentItemKind itemKind,
            int baseDamage)
        {
            if (!state.HasActiveStrengthEnhancer || !IsStrengthEnhancerDamageTarget(itemKind))
            {
                return Math.Max(0, baseDamage);
            }

            if (itemKind == EquipmentItemKind.Dagger)
            {
                return Math.Max(0, baseDamage + 10);
            }

            return Math.Max(0, baseDamage + baseDamage * state.StrengthDamageBonusPercent / 100);
        }

        private static bool IsStrengthEnhancerDamageTarget(EquipmentItemKind itemKind)
        {
            switch (itemKind)
            {
                case EquipmentItemKind.Stick:
                case EquipmentItemKind.ElectricBaton:
                case EquipmentItemKind.Dagger:
                    return true;
                default:
                    return false;
            }
        }

        private static bool HasHandItem(PlayerEquipmentState state, EquipmentItemKind itemKind)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                return false;
            }

            for (var i = 0; i < state.UnlockedHandSlotCount; i++)
            {
                if (state.GetHandSlot(i).ItemKind == itemKind)
                {
                    return true;
                }
            }

            return false;
        }

        private static PlayerEquipmentState ApplyActiveHandUseDurability(PlayerEquipmentState state, int damagePercent)
        {
            var slot = state.ActiveHandSlot;
            return state.WithHandSlot(
                state.ActiveHandSlotIndex,
                ApplyDurabilityDamage(slot, Math.Max(0, damagePercent)));
        }

        private static PlayerEquipmentState ApplySupplySlotDurabilityDamage(
            PlayerEquipmentState state,
            int supplySlotIndex,
            int damagePercent)
        {
            var slot = state.GetSupplySlot(supplySlotIndex);
            return state.WithSupplySlot(
                supplySlotIndex,
                ApplyDurabilityDamage(slot, Math.Max(0, damagePercent)));
        }

        private static int MathfCeilToInt(float value)
        {
            return (int)Math.Ceiling(value);
        }

        private static bool TryStackHandItem(
            PlayerEquipmentState state,
            EquipmentItemDefinition definition,
            out PlayerEquipmentState nextState)
        {
            nextState = state;
            for (var i = 0; i < state.UnlockedHandSlotCount; i++)
            {
                var slot = state.GetHandSlot(i);
                if (slot.ItemKind != definition.ItemKind || slot.Count >= definition.MaxStackCount)
                {
                    continue;
                }

                nextState = state
                    .WithHandSlot(i, slot.WithCount(slot.Count + 1))
                    .WithActiveHandSlot(i)
                    .WithModeAndSummary(EquipmentUseMode.Primary, "Purchased and stacked " + definition.DisplayName + ".");
                return true;
            }

            return false;
        }

        private static bool TryStackSupplyItem(
            PlayerEquipmentState state,
            EquipmentItemDefinition definition,
            out PlayerEquipmentState nextState)
        {
            nextState = state;
            for (var i = 0; i < state.UnlockedSupplySlotCount; i++)
            {
                var slot = state.GetSupplySlot(i);
                if (slot.ItemKind != definition.ItemKind || slot.Count >= definition.MaxStackCount)
                {
                    continue;
                }

                nextState = state
                    .WithSupplySlot(i, slot.WithCount(slot.Count + 1))
                    .WithModeAndSummary(state.ActiveMode, "Purchased and stacked " + definition.DisplayName + ".");
                return true;
            }

            return false;
        }

        private static bool TryStoreInHand(
            PlayerEquipmentState state,
            EquipmentItemDefinition definition,
            out EquipmentPurchaseResult result)
        {
            for (var i = 0; i < state.UnlockedHandSlotCount; i++)
            {
                if (!state.GetHandSlot(i).IsEmpty)
                {
                    continue;
                }

                var handState = state
                    .WithHandSlot(i, EquipmentSlotState.Purchased(definition.ItemKind, definition.PriceCredits))
                    .WithActiveHandSlot(i)
                    .WithModeAndSummary(EquipmentUseMode.Primary, "Purchased and equipped " + definition.DisplayName + ".");
                result = new EquipmentPurchaseResult(
                    handState,
                    true,
                    definition.PriceCredits,
                    definition.ItemKind,
                    handState.LastActionSummary);
                return true;
            }

            result = default;
            return false;
        }

        private static bool TryStoreInSupply(
            PlayerEquipmentState state,
            EquipmentItemDefinition definition,
            out EquipmentPurchaseResult result)
        {
            for (var i = 0; i < state.UnlockedSupplySlotCount; i++)
            {
                if (!state.GetSupplySlot(i).IsEmpty)
                {
                    continue;
                }

                var supplyState = state
                    .WithSupplySlot(i, EquipmentSlotState.Purchased(definition.ItemKind, definition.PriceCredits))
                    .WithModeAndSummary(state.ActiveMode, "Purchased and stored " + definition.DisplayName + ".");
                result = new EquipmentPurchaseResult(
                    supplyState,
                    true,
                    definition.PriceCredits,
                    definition.ItemKind,
                    supplyState.LastActionSummary);
                return true;
            }

            result = default;
            return false;
        }

        private static EquipmentPurchaseResult PurchaseSucceeded(
            PlayerEquipmentState state,
            EquipmentItemDefinition definition,
            string summary)
        {
            return new EquipmentPurchaseResult(
                state.WithModeAndSummary(state.ActiveMode, summary),
                true,
                definition.PriceCredits,
                definition.ItemKind,
                summary);
        }

        private static EquipmentPurchaseResult PurchaseFailed(
            PlayerEquipmentState state,
            EquipmentItemKind itemKind,
            string summary)
        {
            return new EquipmentPurchaseResult(state, false, 0, itemKind, summary);
        }

        private static EquipmentSlotState RemoveOne(EquipmentSlotState slot)
        {
            return slot.Count <= 1 ? EquipmentSlotState.Empty : slot.WithCount(slot.Count - 1);
        }

        private static EquipmentItemDefinition Weapon(
            EquipmentItemKind itemKind,
            string displayName,
            int damage,
            float minRange,
            float maxRange,
            float useDelaySeconds,
            int priceCredits,
            bool requiresTwoHands,
            bool hasThrowMode,
            bool hasPrecisionAimMode,
            bool hasReloadInputSkeleton,
            bool hasConfirmedMagazineSpec,
            float precisionAimMoveMultiplier)
        {
            return new EquipmentItemDefinition(
                itemKind,
                displayName,
                EquipmentItemCategory.Weapon,
                damage,
                minRange,
                maxRange,
                useDelaySeconds,
                priceCredits,
                requiresTwoHands,
                hasThrowMode,
                hasPrecisionAimMode,
                hasReloadInputSkeleton,
                hasConfirmedMagazineSpec,
                precisionAimMoveMultiplier,
                EquipmentStorageTarget.HandFirst,
                1,
                100,
                true,
                EquipmentAvailability.CommonShop);
        }

        private static EquipmentItemDefinition SupplyItem(
            EquipmentItemKind itemKind,
            string displayName,
            EquipmentItemCategory category,
            int priceCredits,
            bool isUniquePerShip,
            int maxStackCount = 1,
            EquipmentAvailability availability = EquipmentAvailability.CommonShop)
        {
            return new EquipmentItemDefinition(
                itemKind,
                displayName,
                category,
                0,
                0f,
                0f,
                0f,
                priceCredits,
                false,
                false,
                false,
                false,
                false,
                1f,
                EquipmentStorageTarget.SupplyOnly,
                maxStackCount,
                100,
                isUniquePerShip,
                availability);
        }

        private static EquipmentItemDefinition Utility(
            EquipmentItemKind itemKind,
            string displayName,
            int priceCredits,
            bool isUniquePerShip,
            int maxStackCount = 1,
            EquipmentAvailability availability = EquipmentAvailability.CommonShop)
        {
            return new EquipmentItemDefinition(
                itemKind,
                displayName,
                EquipmentItemCategory.Utility,
                0,
                0f,
                0f,
                0f,
                priceCredits,
                false,
                itemKind == EquipmentItemKind.Flashbang,
                false,
                false,
                false,
                1f,
                EquipmentStorageTarget.HandFirst,
                maxStackCount,
                100,
                isUniquePerShip,
                availability);
        }

        private static EquipmentShopCatalogEntry BuyEntry(EquipmentItemKind itemKind, bool functionalInPhase15)
        {
            var definition = GetDefinition(itemKind);
            return new EquipmentShopCatalogEntry(
                EquipmentShopSection.Buy,
                definition.Category,
                itemKind,
                definition.DisplayName,
                definition.PriceCredits,
                definition.Availability,
                functionalInPhase15);
        }

        private static EquipmentShopCatalogEntry[] CloneCatalog(EquipmentShopCatalogEntry[] catalog)
        {
            var clone = new EquipmentShopCatalogEntry[catalog.Length];
            Array.Copy(catalog, clone, catalog.Length);
            return clone;
        }

        private static string FormatMoney(int value)
        {
            return value < 0 ? "-$" + -value : "$" + value;
        }
    }
}
