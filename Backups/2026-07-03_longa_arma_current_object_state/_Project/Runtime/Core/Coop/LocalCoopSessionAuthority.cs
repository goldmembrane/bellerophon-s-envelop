using System;
using System.Collections.Generic;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;

namespace Bellerophon.Core.Coop
{
    public interface ICoopSessionAuthority
    {
        int ParticipantCount { get; }

        CoopJoinResult Join(CoopParticipantId participantId);

        bool UpdatePlayerPose(CoopPlayerPoseState pose);

        CoopInteractionResult SubmitInteraction(CoopInteractionRequest request);

        CoopSessionSnapshot CreateSnapshot(CoopParticipantId perspectiveParticipantId);
    }

    public sealed class LocalCoopSessionAuthority : ICoopSessionAuthority
    {
        private static readonly ShipDeviceType[] SyncDeviceOrder =
        {
            ShipDeviceType.CockpitHelm,
            ShipDeviceType.EngineRoomPowerScreen,
            ShipDeviceType.ControlRoomMainScreen,
            ShipDeviceType.ArmoryTurretHandle,
            ShipDeviceType.SupplyRoomStorageCabinet,
            ShipDeviceType.CargoHoldCargoStatus
        };

        private static readonly ShipCctvTarget[] CctvOrder =
        {
            ShipCctvTarget.Cockpit,
            ShipCctvTarget.CargoHold,
            ShipCctvTarget.EngineRoom,
            ShipCctvTarget.Armory
        };

        private readonly int maxParticipants;
        private readonly List<CoopParticipantId> participantOrder = new List<CoopParticipantId>();
        private readonly Dictionary<string, CoopPlayerPoseState> playerPoses =
            new Dictionary<string, CoopPlayerPoseState>(StringComparer.Ordinal);
        private readonly Dictionary<string, CoopPlayerInteractionState> playerInteractions =
            new Dictionary<string, CoopPlayerInteractionState>(StringComparer.Ordinal);
        private readonly Dictionary<ShipDeviceType, CoopParticipantId> deviceClaims =
            new Dictionary<ShipDeviceType, CoopParticipantId>();

        private GameSessionState session;
        private ShipState shipState;
        private CargoState cargoState;
        private bool hasTransportRun;
        private TransportRunState transportRunState;
        private TransportHazardState transportHazardState;
        private TransportHazardResult lastTransportHazardResult;
        private SeedIntruderState seedIntruderState;
        private ShipCctvTarget currentCctvTarget;
        private bool engineOverclockActive;
        private bool engineOverclockUsedThisRun;
        private int engineOverclockActivationCount;

        public LocalCoopSessionAuthority(GameSessionState initialSession, int maxParticipants)
        {
            if (initialSession == null)
            {
                throw new ArgumentNullException(nameof(initialSession));
            }

            if (maxParticipants < CoopSessionLimits.LocalSimulationPlayerCount ||
                maxParticipants > CoopSessionLimits.FutureOnlineMaxPlayers)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxParticipants),
                    "Coop session participant capacity must stay within the planned 2 to 5 player range.");
            }

            this.maxParticipants = maxParticipants;
            session = initialSession;
            shipState = initialSession.Ship;
            cargoState = initialSession.ActiveCargo.HasValue
                ? initialSession.ActiveCargo.Value
                : new CargoState(CargoGrade.Common, 50, 100, 1f, false);
            transportHazardState = TransportHazardState.None;
            lastTransportHazardResult = TransportHazardResult.None;
            seedIntruderState = SeedIntruderState.None;
            currentCctvTarget = ShipCctvTarget.Cockpit;
        }

        public int ParticipantCount => participantOrder.Count;

        public static LocalCoopSessionAuthority CreateLocalSimulation(GameSessionState initialSession)
        {
            return new LocalCoopSessionAuthority(
                initialSession,
                CoopSessionLimits.FutureOnlineMaxPlayers);
        }

        public CoopJoinResult Join(CoopParticipantId participantId)
        {
            if (!participantId.IsValid)
            {
                return new CoopJoinResult(
                    CoopJoinResultStatus.InvalidParticipant,
                    "Participant id is invalid.");
            }

            if (playerPoses.ContainsKey(participantId.Value))
            {
                return new CoopJoinResult(
                    CoopJoinResultStatus.AlreadyJoined,
                    participantId.Value + " is already in the local coop session.");
            }

            if (participantOrder.Count >= maxParticipants)
            {
                return new CoopJoinResult(
                    CoopJoinResultStatus.SessionFull,
                    "Local coop session is full.");
            }

            participantOrder.Add(participantId);
            playerPoses[participantId.Value] = CoopPlayerPoseState.CreateDefault(participantId);
            playerInteractions[participantId.Value] = CoopPlayerInteractionState.None(participantId);
            return new CoopJoinResult(
                CoopJoinResultStatus.Joined,
                participantId.Value + " joined the local coop session.");
        }

        public bool UpdatePlayerPose(CoopPlayerPoseState pose)
        {
            if (!IsJoined(pose.ParticipantId))
            {
                return false;
            }

            playerPoses[pose.ParticipantId.Value] = pose;
            return true;
        }

        public CoopInteractionResult SubmitInteraction(CoopInteractionRequest request)
        {
            if (!request.ParticipantId.IsValid || !IsJoined(request.ParticipantId))
            {
                return Reject(CoopInteractionResultStatus.RejectedNotJoined, "Participant is not joined.");
            }

            switch (request.RequestType)
            {
                case CoopInteractionRequestType.BeginDeviceInteraction:
                    return BeginDeviceInteraction(request.ParticipantId, request.DeviceType);
                case CoopInteractionRequestType.ReleaseDeviceInteraction:
                    return ReleaseDeviceInteraction(request.ParticipantId, request.DeviceType);
                case CoopInteractionRequestType.CycleCctv:
                    return CycleCctv(request.ParticipantId, request.CctvDirection);
                case CoopInteractionRequestType.StartTransportRun:
                    return StartTransportRun(request.ParticipantId, request.TransportDurationSeconds);
                default:
                    return Reject(CoopInteractionResultStatus.RejectedInvalidRequest, "Unsupported coop request.");
            }
        }

        public CoopSessionSnapshot CreateSnapshot(CoopParticipantId perspectiveParticipantId)
        {
            if (perspectiveParticipantId.IsValid && !IsJoined(perspectiveParticipantId))
            {
                throw new InvalidOperationException("Cannot create a coop snapshot for a participant that is not joined.");
            }

            return new CoopSessionSnapshot(
                session,
                shipState,
                cargoState,
                hasTransportRun,
                transportRunState,
                transportHazardState,
                lastTransportHazardResult,
                seedIntruderState,
                currentCctvTarget,
                engineOverclockActive,
                engineOverclockUsedThisRun,
                engineOverclockActivationCount,
                BuildPoseArray(),
                BuildInteractionArray(),
                BuildDeviceClaimArray());
        }

        public void SetAuthoritativeCargoState(CargoState nextCargoState)
        {
            cargoState = nextCargoState;
        }

        public void SetAuthoritativeTransportHazard(TransportHazardState hazard)
        {
            transportHazardState = hazard;
        }

        public void SetAuthoritativeSeedIntruder(SeedIntruderState intruder)
        {
            seedIntruderState = intruder;
        }

        public void ApplyAuthoritativeHazardResult(TransportHazardResult result)
        {
            lastTransportHazardResult = result;
            shipState = TransportHazardRules.ApplyHazardResult(shipState, result);
            session = session.WithShipState(shipState);
            if (hasTransportRun)
            {
                transportRunState = transportRunState.WithShipState(shipState);
            }

            transportHazardState = TransportHazardState.None;
        }

        public void CompleteAuthoritativeTransport(SettlementInput settlementInput)
        {
            session = session.CompleteTransport(settlementInput.WithShip(shipState));
            shipState = session.Ship;
            hasTransportRun = false;
            transportHazardState = TransportHazardState.None;
            lastTransportHazardResult = TransportHazardResult.None;
            seedIntruderState = SeedIntruderState.None;
        }

        private CoopInteractionResult BeginDeviceInteraction(
            CoopParticipantId participantId,
            ShipDeviceType deviceType)
        {
            if (deviceClaims.TryGetValue(deviceType, out var owner) &&
                owner.IsValid &&
                owner != participantId)
            {
                return Reject(
                    CoopInteractionResultStatus.RejectedDeviceBusy,
                    deviceType + " is already controlled by " + owner.Value + ".");
            }

            deviceClaims[deviceType] = participantId;
            playerInteractions[participantId.Value] = new CoopPlayerInteractionState(
                participantId,
                true,
                deviceType,
                "Using " + deviceType + ".");

            return Accept(deviceType + " interaction claimed.");
        }

        private CoopInteractionResult ReleaseDeviceInteraction(
            CoopParticipantId participantId,
            ShipDeviceType deviceType)
        {
            if (OwnsDevice(participantId, deviceType))
            {
                deviceClaims.Remove(deviceType);
            }

            playerInteractions[participantId.Value] = CoopPlayerInteractionState.None(participantId);
            return Accept(deviceType + " interaction released.");
        }

        private CoopInteractionResult CycleCctv(CoopParticipantId participantId, int direction)
        {
            if (direction == 0)
            {
                return Reject(CoopInteractionResultStatus.RejectedInvalidRequest, "CCTV cycle direction cannot be zero.");
            }

            if (!OwnsDevice(participantId, ShipDeviceType.ControlRoomMainScreen))
            {
                return Reject(
                    CoopInteractionResultStatus.RejectedRequiresDeviceOwnership,
                    "Control room screen ownership is required to cycle CCTV.");
            }

            currentCctvTarget = GetNextCctvTarget(currentCctvTarget, direction);
            playerInteractions[participantId.Value] = new CoopPlayerInteractionState(
                participantId,
                true,
                ShipDeviceType.ControlRoomMainScreen,
                "CCTV target changed to " + currentCctvTarget + ".");
            return Accept("CCTV target changed to " + currentCctvTarget + ".");
        }

        private CoopInteractionResult StartTransportRun(CoopParticipantId participantId, int durationSeconds)
        {
            if (durationSeconds <= 0)
            {
                return Reject(
                    CoopInteractionResultStatus.RejectedInvalidRequest,
                    "Transport duration must be positive.");
            }

            if (!OwnsDevice(participantId, ShipDeviceType.CockpitHelm))
            {
                return Reject(
                    CoopInteractionResultStatus.RejectedRequiresDeviceOwnership,
                    "Cockpit helm ownership is required to start transport.");
            }

            if (session.Phase != GameSessionPhase.Ready &&
                session.Phase != GameSessionPhase.Completed)
            {
                return Reject(
                    CoopInteractionResultStatus.RejectedSessionState,
                    "Transport can only start from a ready or completed session.");
            }

            session = session.WithShipState(shipState).StartTransport();
            shipState = session.Ship;
            transportRunState = TransportRunState.Start(durationSeconds, shipState);
            hasTransportRun = true;
            transportHazardState = TransportHazardState.None;
            lastTransportHazardResult = TransportHazardResult.None;
            seedIntruderState = SeedIntruderState.None;
            return Accept("Authoritative transport run started.");
        }

        private bool OwnsDevice(CoopParticipantId participantId, ShipDeviceType deviceType)
        {
            return deviceClaims.TryGetValue(deviceType, out var owner) &&
                   owner.IsValid &&
                   owner == participantId;
        }

        private bool IsJoined(CoopParticipantId participantId)
        {
            return participantId.IsValid && playerPoses.ContainsKey(participantId.Value);
        }

        private CoopPlayerPoseState[] BuildPoseArray()
        {
            var poses = new CoopPlayerPoseState[participantOrder.Count];
            for (var i = 0; i < participantOrder.Count; i++)
            {
                poses[i] = playerPoses[participantOrder[i].Value];
            }

            return poses;
        }

        private CoopPlayerInteractionState[] BuildInteractionArray()
        {
            var interactions = new CoopPlayerInteractionState[participantOrder.Count];
            for (var i = 0; i < participantOrder.Count; i++)
            {
                interactions[i] = playerInteractions[participantOrder[i].Value];
            }

            return interactions;
        }

        private CoopDeviceClaimState[] BuildDeviceClaimArray()
        {
            var claims = new CoopDeviceClaimState[SyncDeviceOrder.Length];
            for (var i = 0; i < SyncDeviceOrder.Length; i++)
            {
                var deviceType = SyncDeviceOrder[i];
                claims[i] = deviceClaims.TryGetValue(deviceType, out var owner)
                    ? new CoopDeviceClaimState(deviceType, owner)
                    : CoopDeviceClaimState.Unclaimed(deviceType);
            }

            return claims;
        }

        private static ShipCctvTarget GetNextCctvTarget(ShipCctvTarget current, int direction)
        {
            var currentIndex = 0;
            for (var i = 0; i < CctvOrder.Length; i++)
            {
                if (CctvOrder[i] == current)
                {
                    currentIndex = i;
                    break;
                }
            }

            var nextIndex = currentIndex + (direction > 0 ? 1 : -1);
            if (nextIndex < 0)
            {
                nextIndex = CctvOrder.Length - 1;
            }
            else if (nextIndex >= CctvOrder.Length)
            {
                nextIndex = 0;
            }

            return CctvOrder[nextIndex];
        }

        private static CoopInteractionResult Accept(string summary)
        {
            return new CoopInteractionResult(CoopInteractionResultStatus.Accepted, summary);
        }

        private static CoopInteractionResult Reject(CoopInteractionResultStatus status, string summary)
        {
            return new CoopInteractionResult(status, summary);
        }
    }
}
