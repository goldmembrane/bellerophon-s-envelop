using Bellerophon.Core.Coop;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using Bellerophon.Platform;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class CoopFoundationTests
    {
        [Test]
        public void LocalAuthority_TwoPlayersShareTransportStateAndRemotePose()
        {
            var authority = CreateTwoPlayerAuthority(out var helmPlayer, out var remotePlayer);
            var pose = new CoopPlayerPoseState(
                helmPlayer,
                1f,
                0f,
                18f,
                90f,
                12f,
                ShipRoomId.Cockpit);

            Assert.That(authority.UpdatePlayerPose(pose), Is.True);
            Assert.That(authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(helmPlayer, ShipDeviceType.CockpitHelm)).Accepted, Is.True);

            var startResult = authority.SubmitInteraction(
                CoopInteractionRequest.StartTransportRun(helmPlayer, 60));
            var remoteSnapshot = authority.CreateSnapshot(remotePlayer);

            Assert.That(startResult.Status, Is.EqualTo(CoopInteractionResultStatus.Accepted));
            Assert.That(remoteSnapshot.ParticipantCount, Is.EqualTo(CoopSessionLimits.LocalSimulationPlayerCount));
            Assert.That(remoteSnapshot.Session.Phase, Is.EqualTo(GameSessionPhase.Transporting));
            Assert.That(remoteSnapshot.HasTransportRun, Is.True);
            Assert.That(remoteSnapshot.TransportRun.EffectiveDurationSeconds, Is.EqualTo(60));
            Assert.That(remoteSnapshot.TryGetPlayerPose(helmPlayer, out var mirroredPose), Is.True);
            Assert.That(mirroredPose.CurrentRoom, Is.EqualTo(ShipRoomId.Cockpit));
            Assert.That(remoteSnapshot.TryGetPlayerInteraction(helmPlayer, out var interaction), Is.True);
            Assert.That(interaction.IsInteracting, Is.True);
            Assert.That(interaction.DeviceType, Is.EqualTo(ShipDeviceType.CockpitHelm));
        }

        [Test]
        public void LocalAuthority_DeviceOwnershipMakesInteractionsConsistent()
        {
            var authority = CreateTwoPlayerAuthority(out var controlPlayer, out var waitingPlayer);

            var claim = authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(controlPlayer, ShipDeviceType.ControlRoomMainScreen));
            var blocked = authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(waitingPlayer, ShipDeviceType.ControlRoomMainScreen));
            var cctv = authority.SubmitInteraction(CoopInteractionRequest.CycleCctv(controlPlayer, 1));
            authority.SubmitInteraction(
                CoopInteractionRequest.ReleaseDevice(controlPlayer, ShipDeviceType.ControlRoomMainScreen));
            var nextClaim = authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(waitingPlayer, ShipDeviceType.ControlRoomMainScreen));
            var snapshot = authority.CreateSnapshot(controlPlayer);

            Assert.That(claim.Status, Is.EqualTo(CoopInteractionResultStatus.Accepted));
            Assert.That(blocked.Status, Is.EqualTo(CoopInteractionResultStatus.RejectedDeviceBusy));
            Assert.That(cctv.Status, Is.EqualTo(CoopInteractionResultStatus.Accepted));
            Assert.That(nextClaim.Status, Is.EqualTo(CoopInteractionResultStatus.Accepted));
            Assert.That(snapshot.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.CargoHold));
            Assert.That(snapshot.TryGetDeviceClaim(
                ShipDeviceType.ControlRoomMainScreen,
                out var controlRoomClaim), Is.True);
            Assert.That(controlRoomClaim.OwnerParticipantId, Is.EqualTo(waitingPlayer));
        }

        [Test]
        public void LocalAuthority_AuthoritativeDamageAndSettlementReachAllSnapshots()
        {
            var authority = CreateTwoPlayerAuthority(out var helmPlayer, out var remotePlayer);
            authority.SubmitInteraction(
                CoopInteractionRequest.BeginDevice(helmPlayer, ShipDeviceType.CockpitHelm));
            authority.SubmitInteraction(CoopInteractionRequest.StartTransportRun(helmPlayer, 60));

            authority.ApplyAuthoritativeHazardResult(new TransportHazardResult(
                TransportHazardType.AsteroidField,
                TransportHazardResolution.DirectHit,
                new[]
                {
                    new ShipRoomHazardDamage(ShipRoomId.EngineRoom, 25)
                }));

            var damagedSnapshot = authority.CreateSnapshot(remotePlayer);
            authority.CompleteAuthoritativeTransport(CreateSettlementInput(damagedSnapshot));
            var completedSnapshot = authority.CreateSnapshot(helmPlayer);

            Assert.That(damagedSnapshot.Ship.GetRoom(ShipRoomId.EngineRoom).CurrentDurability, Is.EqualTo(75));
            Assert.That(damagedSnapshot.Session.Ship.GetRoom(ShipRoomId.EngineRoom).CurrentDurability, Is.EqualTo(75));
            Assert.That(completedSnapshot.Session.Phase, Is.EqualTo(GameSessionPhase.Completed));
            Assert.That(completedSnapshot.Session.SettlementResult.GrossRevenue, Is.EqualTo(100));
            Assert.That(completedSnapshot.Session.Wallet.Credits, Is.EqualTo(100));
        }

        [Test]
        public void NullPlatformServices_ExposeMultiplayerBoundaryWithoutOnlineTransport()
        {
            IPlatformServices platform = new NullPlatformServices();

            Assert.That(platform.Multiplayer.ProviderName, Is.EqualTo("Null"));
            Assert.That(platform.Multiplayer.IsAvailable, Is.True);
            Assert.That(platform.Multiplayer.MaxSupportedPlayers, Is.EqualTo(CoopSessionLimits.FutureOnlineMaxPlayers));
            Assert.That(platform.Multiplayer.SupportsOnlineTransport, Is.False);
        }

        private static LocalCoopSessionAuthority CreateTwoPlayerAuthority(
            out CoopParticipantId first,
            out CoopParticipantId second)
        {
            var authority = LocalCoopSessionAuthority.CreateLocalSimulation(
                GameSessionState.StartSession(new WalletState(0, false)));
            first = new CoopParticipantId("player-a");
            second = new CoopParticipantId("player-b");

            Assert.That(authority.Join(first).Status, Is.EqualTo(CoopJoinResultStatus.Joined));
            Assert.That(authority.Join(second).Status, Is.EqualTo(CoopJoinResultStatus.Joined));
            return authority;
        }

        private static SettlementInput CreateSettlementInput(CoopSessionSnapshot snapshot)
        {
            return new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Normal,
                snapshot.Cargo,
                snapshot.Ship,
                new CrewState(2, 0),
                snapshot.Session.Wallet,
                contractBasePay: 100);
        }
    }
}
