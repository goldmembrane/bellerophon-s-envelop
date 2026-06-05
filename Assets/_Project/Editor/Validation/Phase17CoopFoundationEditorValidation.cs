using System;
using Bellerophon.Core.Coop;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using Bellerophon.Platform;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class Phase17CoopFoundationEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Phase 17 coop foundation editor validation passed.");
            Debug.Log("Phase 17 coop foundation validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            IPlatformServices platform = new NullPlatformServices();
            if (platform.Multiplayer.SupportsOnlineTransport ||
                platform.Multiplayer.MaxSupportedPlayers != CoopSessionLimits.FutureOnlineMaxPlayers)
            {
                throw new InvalidOperationException("Phase 17 must keep online transport behind the platform boundary.");
            }

            var authority = LocalCoopSessionAuthority.CreateLocalSimulation(
                GameSessionState.StartSession(new WalletState(0, false)));
            var first = new CoopParticipantId("phase17-local-a");
            var second = new CoopParticipantId("phase17-local-b");
            if (!authority.Join(first).Succeeded || !authority.Join(second).Succeeded)
            {
                throw new InvalidOperationException("Phase 17 local coop simulation must allow two participants.");
            }

            authority.UpdatePlayerPose(new CoopPlayerPoseState(
                first,
                0f,
                0f,
                18f,
                45f,
                8f,
                ShipRoomId.Cockpit));

            var firstControlClaim = authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(first, ShipDeviceType.ControlRoomMainScreen));
            var blockedSecondClaim = authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(second, ShipDeviceType.ControlRoomMainScreen));
            var cctv = authority.SubmitInteraction(CoopInteractionRequest.CycleCctv(first, 1));
            authority.SubmitInteraction(
                CoopInteractionRequest.ReleaseDevice(first, ShipDeviceType.ControlRoomMainScreen));
            var secondControlClaim = authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(second, ShipDeviceType.ControlRoomMainScreen));
            if (!firstControlClaim.Accepted ||
                blockedSecondClaim.Status != CoopInteractionResultStatus.RejectedDeviceBusy ||
                !cctv.Accepted ||
                !secondControlClaim.Accepted)
            {
                throw new InvalidOperationException("Phase 17 device ownership must serialize shared interactions.");
            }

            authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(first, ShipDeviceType.CockpitHelm));
            var startRun = authority.SubmitInteraction(CoopInteractionRequest.StartTransportRun(first, 60));
            if (!startRun.Accepted)
            {
                throw new InvalidOperationException("Phase 17 authority must accept owned cockpit transport start requests.");
            }

            authority.ApplyAuthoritativeHazardResult(new TransportHazardResult(
                TransportHazardType.AsteroidField,
                TransportHazardResolution.DirectHit,
                new[]
                {
                    new ShipRoomHazardDamage(ShipRoomId.EngineRoom, 25)
                }));

            var snapshot = authority.CreateSnapshot(second);
            if (snapshot.ParticipantCount != CoopSessionLimits.LocalSimulationPlayerCount ||
                snapshot.Session.Phase != GameSessionPhase.Transporting ||
                !snapshot.HasTransportRun ||
                snapshot.CurrentCctvTarget != ShipCctvTarget.CargoHold ||
                snapshot.Ship.GetRoom(ShipRoomId.EngineRoom).CurrentDurability != 75 ||
                !snapshot.TryGetPlayerPose(first, out var firstPose) ||
                firstPose.CurrentRoom != ShipRoomId.Cockpit)
            {
                throw new InvalidOperationException("Phase 17 shared snapshot does not mirror coop session state.");
            }

            return "Players=2; Phase=Transporting; RemotePose=Cockpit; Cctv=CargoHold; EngineRoom=75; OnlineTransport=Deferred";
        }
    }
}
