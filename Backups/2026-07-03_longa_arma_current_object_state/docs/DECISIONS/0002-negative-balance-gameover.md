# 0002 Negative Balance Game Over Rule

Date: 2026-06-03

## Decision

The game does not end immediately when settlement costs exceed the player's current credits.

If settlement makes the wallet negative, the player keeps the negative balance and may continue to the next transport run. After the next run's settlement, if the wallet is still negative, the game transitions to a game over sequence.

The final game over sequence must disable player control and show a full-screen cutscene with the cargo ship visible and a pod being discarded from the ship, then present the game over screen.

## Rationale

This user-confirmed rule overrides the original design note that the game ends immediately when the player cannot pay compensation. It gives one transport run of debt pressure before the final game over.

## Implementation Notes

- Settlement logic should distinguish first negative settlement from final negative settlement.
- The first negative settlement must allow the next transport flow.
- The final negative settlement must expose a deterministic state for tests before the cutscene/game over UI plays.
- Cargo direct pickup, carrying, and delivery interactions remain forbidden by concept.

## Implementation Status

- Implemented in phase 9 through `SettlementDebtStatus`, `WalletState.HasUnpaidDebtGrace`, and `GameSessionPhase.GameOver`.
- `TransportSettlementController` opens arrival settlement UI automatically when the transport run completes.
- Final debt game over disables player input and shows a full-screen cargo ship and ejected pod cutscene.
- Validated by EditMode debt tests and `.\scripts\Run-Phase9SettlementGameOverSmoke.ps1`.
