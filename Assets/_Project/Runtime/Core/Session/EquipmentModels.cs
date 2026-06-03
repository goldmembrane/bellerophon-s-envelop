using System;

namespace Bellerophon.Core.Session
{
    public enum EquipmentItemKind
    {
        None,
        Stick,
        Musket,
        BasicProtectiveSuit
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
        Dropped
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
            float precisionAimMoveMultiplier)
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
    }

    public readonly struct EquipmentSlotState
    {
        public EquipmentSlotState(EquipmentItemKind itemKind, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Equipment count cannot be negative.");
            }

            ItemKind = count == 0 ? EquipmentItemKind.None : itemKind;
            Count = itemKind == EquipmentItemKind.None ? 0 : count;
        }

        public EquipmentItemKind ItemKind { get; }

        public int Count { get; }

        public bool IsEmpty => ItemKind == EquipmentItemKind.None || Count <= 0;

        public static EquipmentSlotState Empty => new EquipmentSlotState(EquipmentItemKind.None, 0);

        public static EquipmentSlotState One(EquipmentItemKind itemKind)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                return Empty;
            }

            return new EquipmentSlotState(itemKind, 1);
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
            FunctionalInPhase15 = functionalInPhase15;
        }

        public EquipmentShopSection Section { get; }

        public EquipmentItemCategory Category { get; }

        public EquipmentItemKind ItemKind { get; }

        public string DisplayName { get; }

        public int PriceCredits { get; }

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
            string summary)
        {
            State = state;
            Outcome = outcome;
            ItemKind = itemKind;
            Mode = mode;
            Damage = Math.Max(0, damage);
            Summary = summary ?? string.Empty;
        }

        public PlayerEquipmentState State { get; }

        public EquipmentUseOutcome Outcome { get; }

        public EquipmentItemKind ItemKind { get; }

        public EquipmentUseMode Mode { get; }

        public int Damage { get; }

        public string Summary { get; }

        public bool AppliesIntruderDamage =>
            Outcome == EquipmentUseOutcome.MeleeHit ||
            Outcome == EquipmentUseOutcome.RangedHit;
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

    public readonly struct PlayerEquipmentState
    {
        public const int DefaultHandSlotCount = 2;
        public const int DefaultSupplySlotCount = 3;

        private static readonly EquipmentSlotState[] EmptyHandSlots =
        {
            EquipmentSlotState.Empty,
            EquipmentSlotState.Empty
        };

        private static readonly EquipmentSlotState[] EmptySupplySlots =
        {
            EquipmentSlotState.Empty,
            EquipmentSlotState.Empty,
            EquipmentSlotState.Empty
        };

        private readonly EquipmentSlotState[] handSlots;
        private readonly EquipmentSlotState[] supplySlots;

        public PlayerEquipmentState(
            bool hasBasicProtectiveSuit,
            EquipmentSlotState[] handSlots,
            EquipmentSlotState[] supplySlots,
            int activeHandSlotIndex,
            float useCooldownSeconds,
            EquipmentUseMode activeMode,
            string lastActionSummary)
        {
            if (activeHandSlotIndex < 0 || activeHandSlotIndex >= DefaultHandSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(activeHandSlotIndex), activeHandSlotIndex, null);
            }

            HasBasicProtectiveSuit = hasBasicProtectiveSuit;
            this.handSlots = NormalizeSlots(handSlots, DefaultHandSlotCount);
            this.supplySlots = NormalizeSlots(supplySlots, DefaultSupplySlotCount);
            ActiveHandSlotIndex = activeHandSlotIndex;
            UseCooldownSeconds = Math.Max(0f, useCooldownSeconds);
            ActiveMode = activeMode;
            LastActionSummary = lastActionSummary ?? string.Empty;
        }

        public bool HasBasicProtectiveSuit { get; }

        public int ActiveHandSlotIndex { get; }

        public float UseCooldownSeconds { get; }

        public EquipmentUseMode ActiveMode { get; }

        public string LastActionSummary { get; }

        public EquipmentSlotState ActiveHandSlot => GetHandSlot(ActiveHandSlotIndex);

        public bool HasAnyItem(EquipmentItemKind itemKind)
        {
            if (itemKind == EquipmentItemKind.None)
            {
                return false;
            }

            for (var i = 0; i < DefaultHandSlotCount; i++)
            {
                if (GetHandSlot(i).ItemKind == itemKind)
                {
                    return true;
                }
            }

            for (var i = 0; i < DefaultSupplySlotCount; i++)
            {
                if (GetSupplySlot(i).ItemKind == itemKind)
                {
                    return true;
                }
            }

            return false;
        }

        public EquipmentSlotState GetHandSlot(int index)
        {
            if (index < 0 || index >= DefaultHandSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return handSlots == null ? EmptyHandSlots[index] : handSlots[index];
        }

        public EquipmentSlotState GetSupplySlot(int index)
        {
            if (index < 0 || index >= DefaultSupplySlotCount)
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
                LastActionSummary);
        }

        public PlayerEquipmentState WithHandSlot(int index, EquipmentSlotState slot)
        {
            var nextSlots = HandSlots;
            nextSlots[index] = slot;
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                nextSlots,
                SupplySlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary);
        }

        public PlayerEquipmentState WithSupplySlot(int index, EquipmentSlotState slot)
        {
            var nextSlots = SupplySlots;
            nextSlots[index] = slot;
            return new PlayerEquipmentState(
                HasBasicProtectiveSuit,
                HandSlots,
                nextSlots,
                ActiveHandSlotIndex,
                UseCooldownSeconds,
                ActiveMode,
                LastActionSummary);
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
                LastActionSummary);
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
                LastActionSummary);
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
                summary);
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
                summary);
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
                    EquipmentSlotState.Empty
                },
                EmptySupplySlots,
                0,
                0f,
                EquipmentUseMode.Primary,
                "Basic protective suit equipped; stick issued.");
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

        private static readonly EquipmentShopCatalogEntry[] Phase15BuyCatalog =
        {
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Buy,
                EquipmentItemCategory.Weapon,
                EquipmentItemKind.Stick,
                "Stick",
                StickPriceCredits,
                true),
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Buy,
                EquipmentItemCategory.Weapon,
                EquipmentItemKind.Musket,
                "Musket",
                MusketPriceCredits,
                true),
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Buy,
                EquipmentItemCategory.ProtectiveGear,
                EquipmentItemKind.BasicProtectiveSuit,
                "Basic Protective Suit",
                0,
                false),
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Buy,
                EquipmentItemCategory.Treatment,
                EquipmentItemKind.None,
                "Treatment items",
                0,
                false),
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Buy,
                EquipmentItemCategory.Enhancement,
                EquipmentItemKind.None,
                "Enhancement items",
                0,
                false),
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Buy,
                EquipmentItemCategory.Utility,
                EquipmentItemKind.None,
                "Utility items",
                0,
                false)
        };

        private static readonly EquipmentShopCatalogEntry[] Phase15SellCatalog =
        {
            new EquipmentShopCatalogEntry(
                EquipmentShopSection.Sell,
                EquipmentItemCategory.Utility,
                EquipmentItemKind.None,
                "Personal cargo sale slot",
                0,
                false)
        };

        public static EquipmentItemDefinition GetDefinition(EquipmentItemKind itemKind)
        {
            switch (itemKind)
            {
                case EquipmentItemKind.Stick:
                    return new EquipmentItemDefinition(
                        EquipmentItemKind.Stick,
                        "Stick",
                        EquipmentItemCategory.Weapon,
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
                    return new EquipmentItemDefinition(
                        EquipmentItemKind.Musket,
                        "Musket",
                        EquipmentItemCategory.Weapon,
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
                        1f);
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

        public static PlayerEquipmentState Tick(PlayerEquipmentState state, float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (deltaSeconds <= 0f || state.UseCooldownSeconds <= 0f)
            {
                return state;
            }

            return state.WithCooldown(Math.Max(0f, state.UseCooldownSeconds - deltaSeconds));
        }

        public static EquipmentUseResult UseActiveEquipment(
            PlayerEquipmentState state,
            bool alternateMode,
            bool hasIntruderTarget)
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
                    return UseStick(state, alternateMode, hasIntruderTarget);
                case EquipmentItemKind.Musket:
                    return UseMusket(state, alternateMode, hasIntruderTarget);
                default:
                    var unsupportedState = state.WithModeAndSummary(EquipmentUseMode.None, "Equipped item cannot be used as a weapon.");
                    return new EquipmentUseResult(
                        unsupportedState,
                        EquipmentUseOutcome.NoItem,
                        slot.ItemKind,
                        EquipmentUseMode.None,
                        0,
                        unsupportedState.LastActionSummary);
            }
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

            var reloadState = state.WithModeAndSummary(EquipmentUseMode.Primary, "Musket reload input received; magazine size and reload time are pending confirmation.");
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
                    "This shop entry is data-only in Phase 15.");
            }

            var definition = GetDefinition(itemKind);
            if (itemKind == EquipmentItemKind.BasicProtectiveSuit)
            {
                return new EquipmentPurchaseResult(
                    state.WithBasicProtectiveSuit(true).WithModeAndSummary(state.ActiveMode, "Basic protective suit state is available."),
                    false,
                    0,
                    itemKind,
                    "Basic protective suit is already part of the starting state.");
            }

            if (state.HasAnyItem(itemKind))
            {
                return new EquipmentPurchaseResult(
                    state,
                    false,
                    0,
                    itemKind,
                    definition.DisplayName + " is already held or stored.");
            }

            for (var i = 0; i < PlayerEquipmentState.DefaultHandSlotCount; i++)
            {
                if (!state.GetHandSlot(i).IsEmpty)
                {
                    continue;
                }

                var handState = state
                    .WithHandSlot(i, EquipmentSlotState.One(itemKind))
                    .WithActiveHandSlot(i)
                    .WithModeAndSummary(EquipmentUseMode.Primary, "Purchased and equipped " + definition.DisplayName + ".");
                return new EquipmentPurchaseResult(
                    handState,
                    true,
                    definition.PriceCredits,
                    itemKind,
                    handState.LastActionSummary);
            }

            for (var i = 0; i < PlayerEquipmentState.DefaultSupplySlotCount; i++)
            {
                if (!state.GetSupplySlot(i).IsEmpty)
                {
                    continue;
                }

                var supplyState = state
                    .WithSupplySlot(i, EquipmentSlotState.One(itemKind))
                    .WithModeAndSummary(state.ActiveMode, "Purchased and stored " + definition.DisplayName + ".");
                return new EquipmentPurchaseResult(
                    supplyState,
                    true,
                    definition.PriceCredits,
                    itemKind,
                    supplyState.LastActionSummary);
            }

            return new EquipmentPurchaseResult(
                state,
                false,
                0,
                itemKind,
                "No hand or supply slot is available.");
        }

        public static string FormatItemName(EquipmentItemKind itemKind)
        {
            return itemKind == EquipmentItemKind.None
                ? "Empty"
                : GetDefinition(itemKind).DisplayName;
        }

        private static EquipmentUseResult UseStick(
            PlayerEquipmentState state,
            bool alternateMode,
            bool hasIntruderTarget)
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
            var summary = hasIntruderTarget
                ? "Stick hit active intruder for 30 damage."
                : "Stick swing found no active intruder target.";
            var nextState = state.WithCooldownModeAndSummary(
                StickUseDelaySeconds,
                EquipmentUseMode.Primary,
                summary);
            return new EquipmentUseResult(
                nextState,
                outcome,
                EquipmentItemKind.Stick,
                EquipmentUseMode.Primary,
                hasIntruderTarget ? StickDamage : 0,
                summary);
        }

        private static EquipmentUseResult UseMusket(
            PlayerEquipmentState state,
            bool alternateMode,
            bool hasIntruderTarget)
        {
            var mode = alternateMode ? EquipmentUseMode.PrecisionAim : EquipmentUseMode.Primary;
            var outcome = hasIntruderTarget ? EquipmentUseOutcome.RangedHit : EquipmentUseOutcome.RangedMiss;
            var summary = hasIntruderTarget
                ? "Musket fired at active intruder for 50 damage."
                : "Musket fired with no active intruder target.";
            var nextState = state.WithCooldownModeAndSummary(
                MusketUseDelaySeconds,
                mode,
                summary);
            return new EquipmentUseResult(
                nextState,
                outcome,
                EquipmentItemKind.Musket,
                mode,
                hasIntruderTarget ? MusketDamage : 0,
                summary);
        }

        private static EquipmentShopCatalogEntry[] CloneCatalog(EquipmentShopCatalogEntry[] catalog)
        {
            var clone = new EquipmentShopCatalogEntry[catalog.Length];
            Array.Copy(catalog, clone, catalog.Length);
            return clone;
        }
    }
}
