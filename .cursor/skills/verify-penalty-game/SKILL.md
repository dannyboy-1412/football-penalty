---
name: verify-penalty-game
description: Drive Football Penalty Challenge in Godot 4.7.2 .NET. Launch the Windows game, send menu and shot input, and prove the penalty loop from screenshots and ROUND logs. Use when checking Start, aim/power/shoot, goalkeeper, score/lives, game over, or Restart.
---

# Verify Football Penalty Challenge

Surface: Godot 4.7.2 desktop window (1152x648). Not a browser. Not `--headless`. Viewport screenshots need a real window.

Godot: `C:\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`
Dotnet: `C:\Program Files\dotnet\dotnet.exe`

## Launch

Build, then run with the user arg that attaches `VerifyDriver`:

```
& "C:\Program Files\dotnet\dotnet.exe" build FootballPenaltyChallenge.sln
& "C:\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --path . -- --verify
```

Or `.\.cursor\skills\verify-penalty-game\scripts\verify.ps1`.

Ready: console prints `VERIFY snapshot res://verify-output/menu-hard.png`. The driver then clicks Hard, Start, misses three over-the-bar shots, opens game over, clicks Restart, and quits.

Teardown: the driver calls `GetTree().Quit(0)` on pass or `Quit(1)` on fail. Do not kill by process name. If a run hangs, stop the PID you started.

`--demo-shot` is a shorter launch that fires one TopRight shot at half power and leaves the window open until you quit.

## Doctor

Read-only. Fail if any check fails.

1. `Test-Path C:\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`
2. `Test-Path "C:\Program Files\dotnet\dotnet.exe"`
3. `project.godot` has `run/main_scene` pointing at `res://Scenes/MainMenu/MainMenu.tscn`
4. `& "C:\Program Files\dotnet\dotnet.exe" build FootballPenaltyChallenge.sln --no-restore` exits 0

## Drive

`VerifyDriver` lives on the scene tree root only when `--verify` is in the user args (`OS.GetCmdlineUserArgs()`). It sends `InputEventMouseButton` / `InputEventKey` through `Input.ParseInputEvent`, and falls back to `Button.Pressed` / `ShotController.ShootNow()` if the phase does not change.

Buttons it looks up by node name:

- `Hard`, `Start` on the main menu
- `Restart`, `MainMenu` on game over

Shots: aim `TopLeft`, wait until power is above 0.93, press Space. Power above 0.9 goes over the bar, so those three shots are always misses.

Do not poke `ScoreManager` internals to fake a result. The pass/fail checks read `GameManager.Score` and `GameManager.Phase` after the real round.

## Evidence

Proof lives in `verify-output/` (ignored by git and by Godot via `.gdignore`). Cleanup must not delete this folder.

| File | What it proves |
|---|---|
| `menu-hard.png` | Main menu, Hard selected |
| `game-start.png` | Score 0, Lives 3, keeper on the line |
| `pre-miss-1.png` `pre-miss-2.png` `pre-miss-3.png` | Red power meter, aimed TopLeft |
| `game-over.png` | GAME OVER, final score, stats |
| `restart.png` | Overlay gone, Lives 3, Score 0 |

Console must contain:

```
ROUND zone=TopLeft power=0.95 outcome=Missed score=0 lives=2
ROUND zone=TopLeft power=0.95 outcome=Missed score=0 lives=1
ROUND zone=TopLeft power=0.95 outcome=Missed score=0 lives=0
VERIFY passed
```

Exit code 0. A run that only compiles has not been verified.

## Cleanup

Leave `verify-output/*.png` in place. If you started Godot and it did not quit, kill that PID only.

## Helpers

`.\.cursor\skills\verify-penalty-game\scripts\verify.ps1`

Runs doctor, build, `--verify`, then asserts the snapshot files exist and the log contains `VERIFY passed`.
