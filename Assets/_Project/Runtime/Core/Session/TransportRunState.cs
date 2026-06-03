using System;

namespace Bellerophon.Core.Session
{
    public enum ShipFlightMode
    {
        AutoPilot,
        ManualFlight
    }

    public readonly struct TransportRunState
    {
        public const float ManualFlightInputSpeed = 1.6f;

        private TransportRunState(
            int baseDurationSeconds,
            int effectiveDurationSeconds,
            float elapsedSeconds,
            ShipFlightMode flightMode,
            ShipState ship,
            float manualOffsetX,
            float manualOffsetY)
        {
            if (baseDurationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDurationSeconds), "Transport duration must be positive.");
            }

            BaseDurationSeconds = baseDurationSeconds;
            EffectiveDurationSeconds = effectiveDurationSeconds;
            ElapsedSeconds = Clamp(elapsedSeconds, 0f, effectiveDurationSeconds);
            FlightMode = flightMode;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            ManualOffsetX = Clamp(manualOffsetX, -1f, 1f);
            ManualOffsetY = Clamp(manualOffsetY, -1f, 1f);
        }

        public int BaseDurationSeconds { get; }

        public int EffectiveDurationSeconds { get; }

        public float ElapsedSeconds { get; }

        public ShipFlightMode FlightMode { get; }

        public ShipState Ship { get; }

        public float ManualOffsetX { get; }

        public float ManualOffsetY { get; }

        public float RemainingSeconds => Math.Max(0f, EffectiveDurationSeconds - ElapsedSeconds);

        public float ProgressPercent => EffectiveDurationSeconds <= 0
            ? 0f
            : Clamp(ElapsedSeconds / EffectiveDurationSeconds, 0f, 1f);

        public bool IsComplete => ElapsedSeconds >= EffectiveDurationSeconds;

        public bool IsAutoPilotAvailable => ShipStateRules.CanUseAutoPilot(Ship);

        public static TransportRunState Start(int baseDurationSeconds, ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var effectiveDuration = ShipStateRules.CalculateEffectiveTransportDurationSeconds(baseDurationSeconds, ship);
            var initialMode = ShipStateRules.CanUseAutoPilot(ship)
                ? ShipFlightMode.AutoPilot
                : ShipFlightMode.ManualFlight;

            return new TransportRunState(
                baseDurationSeconds,
                effectiveDuration,
                0f,
                initialMode,
                ship,
                0f,
                0f);
        }

        public TransportRunState Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            return new TransportRunState(
                BaseDurationSeconds,
                EffectiveDurationSeconds,
                ElapsedSeconds + deltaSeconds,
                FlightMode,
                Ship,
                ManualOffsetX,
                ManualOffsetY);
        }

        public TransportRunState EnterManualFlight()
        {
            return WithMode(ShipFlightMode.ManualFlight);
        }

        public TransportRunState ReturnToAutoPilot()
        {
            return IsAutoPilotAvailable
                ? WithMode(ShipFlightMode.AutoPilot)
                : WithMode(ShipFlightMode.ManualFlight);
        }

        public TransportRunState ApplyManualFlightInput(float horizontal, float vertical, float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (FlightMode != ShipFlightMode.ManualFlight || deltaSeconds <= 0f)
            {
                return this;
            }

            var nextX = ManualOffsetX + Clamp(horizontal, -1f, 1f) * ManualFlightInputSpeed * deltaSeconds;
            var nextY = ManualOffsetY + Clamp(vertical, -1f, 1f) * ManualFlightInputSpeed * deltaSeconds;
            return new TransportRunState(
                BaseDurationSeconds,
                EffectiveDurationSeconds,
                ElapsedSeconds,
                FlightMode,
                Ship,
                nextX,
                nextY);
        }

        public TransportRunState WithShipState(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var effectiveDuration = ShipStateRules.CalculateEffectiveTransportDurationSeconds(BaseDurationSeconds, ship);
            var nextMode = ShipStateRules.CanUseAutoPilot(ship)
                ? FlightMode
                : ShipFlightMode.ManualFlight;

            return new TransportRunState(
                BaseDurationSeconds,
                effectiveDuration,
                ElapsedSeconds,
                nextMode,
                ship,
                ManualOffsetX,
                ManualOffsetY);
        }

        private TransportRunState WithMode(ShipFlightMode mode)
        {
            return new TransportRunState(
                BaseDurationSeconds,
                EffectiveDurationSeconds,
                ElapsedSeconds,
                mode,
                Ship,
                ManualOffsetX,
                ManualOffsetY);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
