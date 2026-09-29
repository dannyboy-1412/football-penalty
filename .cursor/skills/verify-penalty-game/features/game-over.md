# Game over

The match ends at 0 lives. Restart or return to the menu.

## Sub-features

- `overlay` shows GAME OVER, final score, and shot stats
- `restart` resets score and lives, same difficulty
- `main-menu` loads `MainMenu.tscn`

## How to get to it (user POV)

- Lose the third life
- Wait through the last result banner

## Driving it with VerifyDriver

Preconditions:

- Three scripted misses have run
- `GameManager.Phase` is `GameOver`

- **Overlay.** Snapshot `verify-output/game-over.png`. HUD lives are 0. Stats list 3 misses.
- **Restart.** Click node `Restart`. Wait until `Aiming`, lives 3, score 0. Snapshot `verify-output/restart.png`. Overlay gone.

## Gotchas

The viewport texture lags one or two frames after `ShowGameOver` / `HideGameOver`. Wait those frames before snapshotting.
