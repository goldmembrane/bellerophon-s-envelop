using System;
using Bellerophon.Core.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase13IntruderFrameworkEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase13IntruderFrameworkBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 13 intruder framework validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase13IntruderFrameworkBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase13IntruderFrameworkBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase13IntruderFrameworkBootstrap.Phase13RootName);
            if (root == null)
            {
                throw new InvalidOperationException("Phase 13 intruder framework root is missing.");
            }

            var summary = BuildValidationSummary();
            Debug.Log("Phase 13 intruder framework validation passed.");
            Debug.Log("Phase 13 intruder framework validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var cargoImpact = ApplyImpact(CreateDefinition(IntruderObjectiveType.AttackCargo, null));
            if (cargoImpact.AffectedTargetType != IntruderTargetType.Cargo ||
                cargoImpact.Cargo.DurabilityPercent >= 1f ||
                ShipStateRules.CalculateRepairCost(cargoImpact.Ship) != 0)
            {
                throw new InvalidOperationException("Phase 13 cargo attack objective must damage cargo without ship repair cost.");
            }

            var roomImpact = ApplyImpact(CreateDefinition(
                IntruderObjectiveType.OccupyRoom,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Room, ShipRoomId.ControlRoom, 0)
                }));
            if (!roomImpact.RoomOccupied ||
                !roomImpact.Ship.GetRoom(ShipRoomId.ControlRoom).IsFunctionOffline)
            {
                throw new InvalidOperationException("Phase 13 room occupation objective must mark the target room offline.");
            }

            var playerImpact = ApplyImpact(CreateDefinition(IntruderObjectiveType.AttackPlayer, null));
            if (!playerImpact.ThreatensPlayer ||
                playerImpact.AffectedTargetType != IntruderTargetType.Player)
            {
                throw new InvalidOperationException("Phase 13 player attack objective must produce a player threat flag.");
            }

            var shipImpact = ApplyImpact(CreateDefinition(
                IntruderObjectiveType.DestroyShip,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.EngineRoom, 0)
                }));
            if (!shipImpact.ThreatensShip ||
                shipImpact.RoomDamageApplied <= 0 ||
                ShipStateRules.CalculateRepairCost(shipImpact.Ship) <= 0)
            {
                throw new InvalidOperationException("Phase 13 ship destruction objective must damage a target ship room.");
            }

            var routeDefinition = CreateDefinition(
                IntruderObjectiveType.DestroyShip,
                new[]
                {
                    new IntruderTargetPriority(IntruderTargetType.Ship, ShipRoomId.Armory, 0)
                });
            var routeAttempt = IntruderRules.CreateAttempt(
                "phase13-route",
                routeDefinition,
                0,
                ShipRoomId.Cockpit);
            var routeIntruder = IntruderRules.CreateBoardedIntruder(
                IntruderRules.ResolveAttempt(routeAttempt, false),
                routeDefinition);
            var criticalControl = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(25, 100));
            var closedRoute = IntruderRules.AssessRoute(routeIntruder, criticalControl);
            if (closedRoute.HasPath ||
                closedRoute.ClosedCorridorPercent != 90 ||
                closedRoute.ClosedCorridorCount != 9)
            {
                throw new InvalidOperationException("Phase 13 route validation must reflect critical control-room corridor closure.");
            }

            var destroyedControl = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100));
            var environment = IntruderRules.AssessEnvironment(routeDefinition, destroyedControl, 3);
            if (environment.IntruderSuppressionOnline ||
                environment.StatMultiplier != ShipStateRules.ControlRoomDestroyedIntruderStatMultiplier ||
                environment.EffectiveRoomDamage != 9)
            {
                throw new InvalidOperationException("Phase 13 environment validation must reflect disabled internal intruder suppression.");
            }

            var bond = IntruderRules.DetermineRelation(
                IntruderFaction.CargoFreedomLeague,
                IntruderFaction.CargoFreedomLeague);
            var alienPirate = IntruderRules.DetermineRelation(
                IntruderFaction.AlienLifeform,
                IntruderFaction.SpacePirate);
            var seedAlien = IntruderRules.DetermineRelation(
                IntruderFaction.SeedEntity,
                IntruderFaction.AlienLifeform);
            if (bond.RelationKind != IntruderRelationKind.Bonded ||
                bond.MarkerKind != IntruderRelationMarkerKind.GreenCircle ||
                bond.FriendlyFireDamagesHealth ||
                bond.FriendlyFireAppliesStatusEffects ||
                alienPirate.RelationKind != IntruderRelationKind.Competitive ||
                seedAlien.RelationKind != IntruderRelationKind.Hostile)
            {
                throw new InvalidOperationException("Phase 13 faction relation validation must match the source design.");
            }

            var attempt = IntruderRules.CreateAttempt(
                "phase13-summary",
                CreateDefinition(IntruderObjectiveType.AttackCargo, null),
                42,
                ShipRoomId.Cockpit);

            return $"Attempt={attempt.Phase}; Entry={attempt.EntryRoom}; Cargo={cargoImpact.Cargo.DurabilityPercent:0.00}; RoomOffline={roomImpact.Ship.GetRoom(ShipRoomId.ControlRoom).IsFunctionOffline}; PlayerThreat={playerImpact.ThreatensPlayer}; ShipDamage={shipImpact.RoomDamageApplied}; ClosedCorridors={closedRoute.ClosedCorridorCount}; Suppression={environment.IntruderSuppressionOnline}; Relation={bond.RelationKind}/{alienPirate.RelationKind}/{seedAlien.RelationKind}";
        }

        private static IntruderImpactResult ApplyImpact(IntruderDefinition definition)
        {
            var attempt = IntruderRules.CreateAttempt(
                "phase13-" + definition.PrimaryObjective,
                definition,
                42,
                ShipRoomId.Armory);
            var boarded = IntruderRules.ResolveAttempt(attempt, false);
            var intruder = IntruderRules.CreateBoardedIntruder(boarded, definition).MoveToTargetRoom();
            return IntruderRules.ApplyObjectivePressure(
                intruder,
                ShipState.CreateDefault(),
                new CargoState(CargoGrade.Common, 50, 100, 1f, false),
                20,
                0.15f);
        }

        private static IntruderDefinition CreateDefinition(
            IntruderObjectiveType objective,
            IntruderTargetPriority[] priorities)
        {
            return new IntruderDefinition(
                "phase13-" + objective,
                "Phase 13 " + objective,
                IntruderFaction.SeedEntity,
                objective,
                maxHealth: 80,
                movementSpeed: 1.5f,
                attackRange: 2f,
                attackDelaySeconds: 1.25f,
                targetPriorities: priorities);
        }
    }
}
