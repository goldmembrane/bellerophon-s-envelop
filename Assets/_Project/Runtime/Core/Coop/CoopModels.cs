using System;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;

namespace Bellerophon.Core.Coop
{
    public static class CoopSessionLimits
    {
        public const int LocalSimulationPlayerCount = 2;
        public const int FutureOnlineMaxPlayers = 5;
    }

    public enum CoopJoinResultStatus
    {
        Joined,
        AlreadyJoined,
        SessionFull,
        InvalidParticipant
    }

    public enum CoopInteractionRequestType
    {
        None,
        BeginDeviceInteraction,
        ReleaseDeviceInteraction,
        CycleCctv,
        StartTransportRun
    }

    public enum CoopInteractionResultStatus
    {
        Accepted,
        RejectedNotJoined,
        RejectedDeviceBusy,
        RejectedRequiresDeviceOwnership,
        RejectedInvalidRequest,
        RejectedSessionState
    }

    public readonly struct CoopParticipantId : IEquatable<CoopParticipantId>
    {
        private readonly string value;

        public CoopParticipantId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Coop participant id is required.", nameof(value));
            }

            this.value = value.Trim();
        }

        public string Value => value ?? string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CoopParticipantId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CoopParticipantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(CoopParticipantId left, CoopParticipantId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CoopParticipantId left, CoopParticipantId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct CoopJoinResult
    {
        public CoopJoinResult(CoopJoinResultStatus status, string summary)
        {
            Status = status;
            Summary = summary ?? string.Empty;
        }

        public CoopJoinResultStatus Status { get; }

        public string Summary { get; }

        public bool Succeeded =>
            Status == CoopJoinResultStatus.Joined ||
            Status == CoopJoinResultStatus.AlreadyJoined;
    }

    public readonly struct CoopPlayerPoseState
    {
        public CoopPlayerPoseState(
            CoopParticipantId participantId,
            float positionX,
            float positionY,
            float positionZ,
            float yawDegrees,
            float pitchDegrees,
            ShipRoomId currentRoom)
        {
            if (!participantId.IsValid)
            {
                throw new ArgumentException("Coop player pose requires a valid participant id.", nameof(participantId));
            }

            ParticipantId = participantId;
            PositionX = positionX;
            PositionY = positionY;
            PositionZ = positionZ;
            YawDegrees = yawDegrees;
            PitchDegrees = Clamp(pitchDegrees, -89f, 89f);
            CurrentRoom = currentRoom;
        }

        public CoopParticipantId ParticipantId { get; }

        public float PositionX { get; }

        public float PositionY { get; }

        public float PositionZ { get; }

        public float YawDegrees { get; }

        public float PitchDegrees { get; }

        public ShipRoomId CurrentRoom { get; }

        public static CoopPlayerPoseState CreateDefault(CoopParticipantId participantId)
        {
            return new CoopPlayerPoseState(
                participantId,
                0f,
                0f,
                0f,
                0f,
                0f,
                ShipRoomId.CargoHold);
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

    public readonly struct CoopPlayerInteractionState
    {
        public CoopPlayerInteractionState(
            CoopParticipantId participantId,
            bool isInteracting,
            ShipDeviceType deviceType,
            string summary)
        {
            if (!participantId.IsValid)
            {
                throw new ArgumentException("Coop player interaction requires a valid participant id.", nameof(participantId));
            }

            ParticipantId = participantId;
            IsInteracting = isInteracting;
            DeviceType = deviceType;
            Summary = summary ?? string.Empty;
        }

        public CoopParticipantId ParticipantId { get; }

        public bool IsInteracting { get; }

        public ShipDeviceType DeviceType { get; }

        public string Summary { get; }

        public static CoopPlayerInteractionState None(CoopParticipantId participantId)
        {
            return new CoopPlayerInteractionState(
                participantId,
                false,
                ShipDeviceType.CargoHoldCargoStatus,
                string.Empty);
        }
    }

    public readonly struct CoopDeviceClaimState
    {
        public CoopDeviceClaimState(ShipDeviceType deviceType, CoopParticipantId ownerParticipantId)
        {
            DeviceType = deviceType;
            OwnerParticipantId = ownerParticipantId;
        }

        public ShipDeviceType DeviceType { get; }

        public CoopParticipantId OwnerParticipantId { get; }

        public bool IsClaimed => OwnerParticipantId.IsValid;

        public static CoopDeviceClaimState Unclaimed(ShipDeviceType deviceType)
        {
            return new CoopDeviceClaimState(deviceType, default);
        }
    }

    public readonly struct CoopInteractionRequest
    {
        private CoopInteractionRequest(
            CoopParticipantId participantId,
            CoopInteractionRequestType requestType,
            ShipDeviceType deviceType,
            int cctvDirection,
            int transportDurationSeconds)
        {
            ParticipantId = participantId;
            RequestType = requestType;
            DeviceType = deviceType;
            CctvDirection = cctvDirection;
            TransportDurationSeconds = transportDurationSeconds;
        }

        public CoopParticipantId ParticipantId { get; }

        public CoopInteractionRequestType RequestType { get; }

        public ShipDeviceType DeviceType { get; }

        public int CctvDirection { get; }

        public int TransportDurationSeconds { get; }

        public static CoopInteractionRequest BeginDevice(CoopParticipantId participantId, ShipDeviceType deviceType)
        {
            return new CoopInteractionRequest(
                participantId,
                CoopInteractionRequestType.BeginDeviceInteraction,
                deviceType,
                0,
                0);
        }

        public static CoopInteractionRequest ReleaseDevice(CoopParticipantId participantId, ShipDeviceType deviceType)
        {
            return new CoopInteractionRequest(
                participantId,
                CoopInteractionRequestType.ReleaseDeviceInteraction,
                deviceType,
                0,
                0);
        }

        public static CoopInteractionRequest CycleCctv(CoopParticipantId participantId, int direction)
        {
            return new CoopInteractionRequest(
                participantId,
                CoopInteractionRequestType.CycleCctv,
                ShipDeviceType.ControlRoomMainScreen,
                direction,
                0);
        }

        public static CoopInteractionRequest StartTransportRun(CoopParticipantId participantId, int durationSeconds)
        {
            return new CoopInteractionRequest(
                participantId,
                CoopInteractionRequestType.StartTransportRun,
                ShipDeviceType.CockpitHelm,
                0,
                durationSeconds);
        }
    }

    public readonly struct CoopInteractionResult
    {
        public CoopInteractionResult(CoopInteractionResultStatus status, string summary)
        {
            Status = status;
            Summary = summary ?? string.Empty;
        }

        public CoopInteractionResultStatus Status { get; }

        public string Summary { get; }

        public bool Accepted => Status == CoopInteractionResultStatus.Accepted;
    }

    public sealed class CoopSessionSnapshot
    {
        private static readonly CoopPlayerPoseState[] EmptyPoses = new CoopPlayerPoseState[0];
        private static readonly CoopPlayerInteractionState[] EmptyInteractions = new CoopPlayerInteractionState[0];
        private static readonly CoopDeviceClaimState[] EmptyDeviceClaims = new CoopDeviceClaimState[0];

        private readonly CoopPlayerPoseState[] playerPoses;
        private readonly CoopPlayerInteractionState[] playerInteractions;
        private readonly CoopDeviceClaimState[] deviceClaims;

        public CoopSessionSnapshot(
            GameSessionState session,
            ShipState ship,
            CargoState cargo,
            bool hasTransportRun,
            TransportRunState transportRun,
            TransportHazardState transportHazard,
            TransportHazardResult lastTransportHazardResult,
            SeedIntruderState seedIntruder,
            ShipCctvTarget currentCctvTarget,
            bool engineOverclockActive,
            bool engineOverclockUsedThisRun,
            int engineOverclockActivationCount,
            CoopPlayerPoseState[] playerPoses,
            CoopPlayerInteractionState[] playerInteractions,
            CoopDeviceClaimState[] deviceClaims)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            Cargo = cargo;
            HasTransportRun = hasTransportRun;
            TransportRun = transportRun;
            TransportHazard = transportHazard;
            LastTransportHazardResult = lastTransportHazardResult;
            SeedIntruder = seedIntruder;
            CurrentCctvTarget = currentCctvTarget;
            EngineOverclockActive = engineOverclockActive;
            EngineOverclockUsedThisRun = engineOverclockUsedThisRun;
            EngineOverclockActivationCount = Math.Max(0, engineOverclockActivationCount);
            this.playerPoses = playerPoses == null
                ? EmptyPoses
                : (CoopPlayerPoseState[])playerPoses.Clone();
            this.playerInteractions = playerInteractions == null
                ? EmptyInteractions
                : (CoopPlayerInteractionState[])playerInteractions.Clone();
            this.deviceClaims = deviceClaims == null
                ? EmptyDeviceClaims
                : (CoopDeviceClaimState[])deviceClaims.Clone();
        }

        public GameSessionState Session { get; }

        public ShipState Ship { get; }

        public CargoState Cargo { get; }

        public bool HasTransportRun { get; }

        public TransportRunState TransportRun { get; }

        public TransportHazardState TransportHazard { get; }

        public TransportHazardResult LastTransportHazardResult { get; }

        public SeedIntruderState SeedIntruder { get; }

        public ShipCctvTarget CurrentCctvTarget { get; }

        public bool EngineOverclockActive { get; }

        public bool EngineOverclockUsedThisRun { get; }

        public int EngineOverclockActivationCount { get; }

        public int ParticipantCount => playerPoses.Length;

        public CoopPlayerPoseState[] PlayerPoses => (CoopPlayerPoseState[])playerPoses.Clone();

        public CoopPlayerInteractionState[] PlayerInteractions =>
            (CoopPlayerInteractionState[])playerInteractions.Clone();

        public CoopDeviceClaimState[] DeviceClaims => (CoopDeviceClaimState[])deviceClaims.Clone();

        public bool TryGetPlayerPose(CoopParticipantId participantId, out CoopPlayerPoseState pose)
        {
            for (var i = 0; i < playerPoses.Length; i++)
            {
                if (playerPoses[i].ParticipantId == participantId)
                {
                    pose = playerPoses[i];
                    return true;
                }
            }

            pose = default;
            return false;
        }

        public bool TryGetPlayerInteraction(
            CoopParticipantId participantId,
            out CoopPlayerInteractionState interaction)
        {
            for (var i = 0; i < playerInteractions.Length; i++)
            {
                if (playerInteractions[i].ParticipantId == participantId)
                {
                    interaction = playerInteractions[i];
                    return true;
                }
            }

            interaction = default;
            return false;
        }

        public bool TryGetDeviceClaim(ShipDeviceType deviceType, out CoopDeviceClaimState claim)
        {
            for (var i = 0; i < deviceClaims.Length; i++)
            {
                if (deviceClaims[i].DeviceType == deviceType)
                {
                    claim = deviceClaims[i];
                    return true;
                }
            }

            claim = CoopDeviceClaimState.Unclaimed(deviceType);
            return false;
        }
    }
}
