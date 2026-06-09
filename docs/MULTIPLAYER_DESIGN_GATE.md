# Multiplayer Design Gate

Date: 2026-06-09

This document is the step 22 result for `docs/DETAILED_IMPLEMENTATION_PLAN.md`.
It defines the online multiplayer design before any Steam lobby, real network package,
or max-5 online synchronization implementation starts.

## Status

- Design gate execution: complete.
- Online implementation: blocked until the user explicitly approves this design for implementation.
- User-confirmed release policy: network-related implementation starts only after the non-network game is otherwise complete.
- Step 22 completion does not mean release readiness.
- Steamworks SDK linkage: not started.
- Real network package integration: not started.
- Max-5 online synchronization code: not started.

## Source Alignment

- `docs/DETAILED_IMPLEMENTATION_PLAN.md` step 22 requires a design gate for authority model, sync scope, Steam integration scope, message mapping, and network test strategy.
- `docs/MVP_IMPLEMENTATION_ORDER.md` limits the existing phase 17 scope to network-independent interfaces and local 2-player simulation.
- `docs/HARNESS.md` confirms the current coop validation path uses local authority without Steam lobby or real online transport.
- `docs/GAME_DESIGN_SOURCE.txt` does not define online protocol details, so this document settles the missing multiplayer policies without changing source-valued gameplay balance.

## Non-Goals

- Do not add Steamworks SDK references in runtime gameplay code.
- Do not add a real network package in this step.
- Do not implement lobby creation, invite handling, matchmaking, relay, NAT traversal, or packet transport in this step.
- Do not change rewards, repair costs, hazard probabilities, enemy stats, item prices, or contract values.
- Do not change current single-player behavior.
- Do not treat this design gate as a release gate or release approval.

## Decision Summary

- Online MVP uses a maximum of 5 participants.
- The host is the only authority for canonical gameplay state.
- Clients send input and interaction intents; the host validates them and publishes accepted results.
- Clients may predict local camera, cursor, reticle, and pose presentation, but not settlement, damage, hazards, intruders, wallet, inventory, contract, or ship state.
- Steam lobby is discovery and invitation infrastructure only. The Steam lobby never stores canonical gameplay state.
- Late join is allowed only while the session is in planet/pre-departure states. Active transport late join is blocked.
- Reconnect is allowed for the same platform identity while the host session is still alive.
- Host migration is not part of the initial online MVP.
- Planet-side state-changing UI is host-only for the initial online MVP.

## Existing Foundation To Preserve

- `CoopSessionLimits.FutureOnlineMaxPlayers` is the online capacity target.
- `ICoopSessionAuthority` is the gameplay authority boundary.
- `LocalCoopSessionAuthority` is the local validation model for authority behavior.
- `CoopSessionSnapshot` is the current full-state snapshot shape for coop replication.
- `IPlatformMultiplayerServices` is the platform multiplayer boundary.
- `NullPlatformMultiplayerServices` remains valid and must keep tests runnable without Steam.

## Authority Model

### Roles

- Host: lobby owner, campaign save owner, and only canonical gameplay simulation authority.
- Participant: connected non-host player that can submit intents and receive snapshots/deltas.
- Reconnecting participant: previously accepted participant returning with the same platform identity.

### Host-Owned Canonical State

- `GameSessionState`
- `ShipState`
- `CargoState`
- `WalletState`
- `TransportRunState`
- `TransportHazardState`
- `SeedIntruderState` and later intruder states
- External target state for hazards, alien lifeforms, Cargo Freedom League, and space pirates
- Contract board state, accepted contracts, special contract progress, reputation, unlocks
- Player equipment inventory, ship equipment inventory, purchases, sales, and upgrades
- Settlement result, debt grace, total loss, repair claims, game-over state

### Client-Owned Soft State

- Local camera and cursor presentation
- Local input buffering
- Local UI selection highlight
- Local audio/visual feedback that does not mutate gameplay state
- Last submitted player pose before host acknowledgement

### Validation Rules

- Every gameplay mutation starts as a client request or host-local command.
- The host rejects requests from participants that are not joined, do not own the relevant device, are in the wrong session phase, or are blocked by exclusive ownership.
- RNG decisions are made on the host. Clients receive the resulting event/state, not the right to independently roll.
- Every accepted state mutation receives a monotonically increasing session sequence.
- Clients ignore stale deltas with older sequence values.
- Discrete commands use request ids so duplicate packets can be acknowledged without applying twice.

## Interaction Ownership

| Surface | Initial Online MVP Owner | Policy |
| --- | --- | --- |
| Cockpit helm | Exclusive device owner | Owner can request transport start and send manual/auto flight intents. Host validates session phase and ship readiness. |
| Armory manual turret | Exclusive device owner | Owner sends aim/fire/reload intents. Host decides ammo, hit, damage, and target destruction. |
| Armory carried equipment | Per-participant presentation, host-owned inventory | Slot selection is local input, but inventory mutation and purchases are host authoritative. |
| Control room main screen | Exclusive device owner | Owner can cycle CCTV and later trigger control-room commands. Non-owners receive read-only state. |
| Engine room power screen | Exclusive device owner | Owner can request engine-room commands such as overclock. Host validates one-use/run constraints. |
| Supply room cabinet | Exclusive device owner | Owner can request supply checkout or storage actions. Host mutates shared supply/equipment state. |
| Cargo hold status | Read-only by default | Direct cargo pickup/carry/delivery remains forbidden by project design. |
| Maintenance UI | Host only | Host pays repair charges and applies repair results. Other participants can view replicated summary later. |
| Shop UI | Host only | Host buys/sells from the shared wallet/inventory. Other participants are read-only in MVP. |
| Contract UI | Host only | Host accepts contracts and starts special-contract progress. Other participants are read-only in MVP. |
| Cargo depot UI | Host only | Host changes cargo depot state if later cargo-depot mutations are added. |
| Settings/menu UI | Local user | Local display/audio/input settings are not synchronized gameplay state. |

Device ownership release rules:

- Voluntary exit releases the device immediately.
- Disconnect releases any active device claim so the ship remains playable.
- Reconnect does not automatically reclaim the device; the participant must claim it again.
- Host can force-release orphaned claims in future UI, but the initial MVP should release on disconnect before a manual force command is needed.

## Authoritative Synchronization Scope

The following state is host-authoritative only:

- Transport run phase, duration, progress, manual-flight result, and auto/manual mode availability.
- Hazard scheduling, hazard type, hazard seed, active target state, avoidance result, and damage result.
- Intruder spawn, route, target selection, attack ticks, damage, treatment, defeat, and cleanup.
- External target health, destruction, and boarding/intrusion transition.
- Ship room durability, offline state, repair estimate, and pending repair claim.
- Cargo loss, cargo value, cargo ownership, cargo material, and final cargo score.
- Crew/death/casualty counts.
- Wallet credits, debt grace, settlement result, reward payout, towing cost, repair cost, and game-over decision.
- Contract acceptance, completion, failure, special-contract progress, unlocks, reputation, and visited-planet records.
- Equipment inventory, active hand slot authority state, shop purchases/sales, supply use, and ship upgrades.
- Save checkpoints and cloud-save payloads.

The following can be client-predicted but must be corrected by host snapshots:

- Remote player pose interpolation.
- Manual turret reticle movement.
- Cockpit manual-flight cursor movement.
- Local UI hover/selection.
- Audio and visual feedback for submitted commands.

## Steam Lobby And Session Policy

### Lobby Purpose

Steam lobby is used for:

- Finding or joining a friend/invite session.
- Communicating joinability and metadata.
- Carrying connection bootstrap data for the eventual transport layer.

Steam lobby is not used for:

- Canonical gameplay state.
- Save payloads.
- Settlement results.
- Damage, hazard, intruder, or wallet decisions.

### Lobby Metadata

Required metadata when implementation begins:

- Build version
- Multiplayer protocol version
- Max players
- Current participant count
- Join state: `PlanetJoinable`, `ReadyJoinable`, `TransportLocked`, or `Closed`
- Host display name
- Save/campaign identifier hash, not raw save data

### Invite

- Invites can target a joinable lobby.
- Invite acceptance must still pass build/protocol/session-capacity checks.
- Invite acceptance during active transport is rejected unless it is a reconnect for an existing participant identity.

### Late Join

- Allowed while the host session is in planet/pre-departure states.
- Blocked during active transport.
- Blocked during settlement/game-over transitions.
- A late join receives a full `CoopSessionSnapshot` before any deltas.

### Reconnect

- Reconnect is allowed for the same platform identity if the host session is still alive.
- Reconnect during active transport is allowed only into the previous participant slot.
- Reconnect receives a full snapshot and then resumes deltas from the current host sequence.
- Device ownership is not restored automatically after reconnect.

### Disconnect

- Non-host disconnect releases active device ownership.
- The host continues the session if at least one participant remains or if the host is playing solo.
- If a non-host disconnects during an interaction, any in-flight command from that participant is ignored after disconnect is observed.

### Host Disconnect And Migration

- Initial online MVP has no host migration.
- If the host disconnects, non-host clients leave the active session with a host-lost reason.
- The host can later resume from the host-owned save if a valid save checkpoint exists.
- Host migration can be reconsidered only after save ownership, deterministic state transfer, and Steam lobby owner transfer risks are separately designed.

## Local Coop To Online Message Mapping

There is no direct client-to-client gameplay messaging in the initial online MVP.
All gameplay messages route through the host.

### Client To Host

| Message | Current Model Mapping | Delivery |
| --- | --- | --- |
| `JoinSessionRequest` | `CoopParticipantId` plus platform identity | Reliable ordered |
| `LeaveSessionNotice` | Participant id | Reliable ordered |
| `PlayerPoseUpdate` | `CoopPlayerPoseState` | Unreliable sequenced |
| `BeginDeviceInteractionRequest` | `CoopInteractionRequest.BeginDevice` | Reliable ordered |
| `ReleaseDeviceInteractionRequest` | `CoopInteractionRequest.ReleaseDevice` | Reliable ordered |
| `CycleCctvRequest` | `CoopInteractionRequest.CycleCctv` | Reliable ordered |
| `StartTransportRunRequest` | `CoopInteractionRequest.StartTransportRun` | Reliable ordered |
| `CockpitFlightInput` | Future cockpit command payload | Unreliable sequenced, host simulated |
| `ManualTurretInput` | Future turret aim/fire/reload payload | Aim sequenced, fire/reload reliable |
| `EngineRoomCommandRequest` | Future engine-room command payload | Reliable ordered |
| `SupplyCommandRequest` | Future supply cabinet command payload | Reliable ordered |
| `PlanetUiCommandRequest` | Maintenance/shop/contract/cargo-depot command | Reliable ordered, host-only in MVP |

### Host To Client

| Message | Current Model Mapping | Delivery |
| --- | --- | --- |
| `JoinSessionResult` | `CoopJoinResult` | Reliable ordered |
| `InteractionResult` | `CoopInteractionResult` | Reliable ordered |
| `SessionSnapshot` | `CoopSessionSnapshot` | Reliable ordered |
| `SessionDelta` | Changed canonical state with host sequence | Reliable ordered |
| `DeviceClaimDelta` | `CoopDeviceClaimState` | Reliable ordered |
| `PlayerPoseBroadcast` | Remote `CoopPlayerPoseState` values | Unreliable sequenced |
| `TransportRunDelta` | `TransportRunState` changes | Reliable ordered |
| `HazardStateDelta` | `TransportHazardState` and external target state | Reliable ordered |
| `HazardResolvedEvent` | `TransportHazardResult` | Reliable ordered |
| `IntruderStateDelta` | Intruder state changes | Reliable ordered |
| `SettlementResultEvent` | Settlement and wallet result | Reliable ordered |
| `ParticipantRemovedEvent` | Participant id and reason | Reliable ordered |
| `HostClosedSessionEvent` | Host shutdown/lost reason | Reliable ordered |

## Save, Cloud, Achievement, And Stats Policy

- Host-owned campaign save is the only shared session save.
- Non-host clients save local settings only.
- Steam Cloud integration remains behind `IPlatformCloudSaveServices`.
- Steam achievements and stats remain behind `IPlatformAchievementServices` and `IPlatformStatsServices`.
- Clients may unlock local achievements from host-approved events, but must not mutate shared session state from achievement/stat callbacks.
- Save writes should happen at safe checkpoints: planet hub, completed settlement, and explicit host save points. Active transport mid-run save is not part of the initial online MVP.

## Network Test Strategy

### Required Before Online Package Integration

- Document review of this design.
- Existing local coop regression:
  - `.\scripts\Run-Phase17CoopFoundationSmoke.ps1`
  - `.\scripts\Run-HarnessValidation.ps1`
  - `.\scripts\Run-EditModeTests.ps1`
- Message DTO and serializer EditMode tests once DTOs are introduced.
- Fake in-memory transport tests for 2 to 5 participants before Steam transport.

### Multi-Instance Verification Scenarios

- 5 participants can join, and a 6th is rejected.
- Two participants competing for the same device produce one accepted claim and one rejected claim.
- Cockpit owner can start transport; non-owner start request is rejected.
- Control room owner can change CCTV; non-owner command is rejected.
- Manual turret owner can destroy an external target; all clients receive the same result.
- Hazard damage is applied once by the host and reaches every client snapshot.
- Intruder target, room damage, and defeat state are identical on every client.
- Host-only shop purchase changes wallet/inventory once and replicates to all clients.
- Non-host shop/contract/maintenance mutation is rejected in MVP.
- Non-host disconnect releases device ownership.
- Reconnect restores the participant snapshot but not device ownership.
- Late join succeeds in planet/pre-departure state and is rejected during active transport.
- Host disconnect closes the session; no host migration occurs.
- Build/protocol mismatch rejects join before gameplay state is exchanged.

## Risk Checklist

- Single source of truth remains the host authority.
- Steam lobby metadata never becomes canonical gameplay state.
- Every gameplay mutation has a request id or host sequence.
- Device ownership conflict is resolved by the host.
- Disconnect releases orphaned device claims.
- Reconnect does not duplicate participants or replay old commands.
- Late join cannot enter an active transport run as a new participant.
- Host disconnect behavior is explicit because host migration is deferred.
- Host save ownership is explicit.
- Client prediction is presentation-only.
- RNG is host-only.
- Version/protocol mismatch blocks joining.
- Steam callbacks stay behind platform interfaces.
- Null platform services continue to run tests without Steam.

## Approval Gate

After this document is reviewed, online implementation can start only if the user explicitly approves this design for implementation.
The first implementation step after approval should still begin with interfaces, DTOs, fake transport tests, and local multi-participant verification before any Steam-specific code.
