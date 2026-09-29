# Main menu

The first screen. Pick a keeper difficulty and start a match.

## Sub-features

- `menu-title` shows Football Penalty Challenge and Start Game
- `menu-difficulty` toggles Easy / Medium / Hard, Medium on by default
- `menu-start` loads the pitch with the chosen difficulty

## How to get to it (user POV)

- Launch the Godot project
- Click Main Menu from game over

## Driving it with VerifyDriver

Preconditions:

- `project.godot` main scene is `res://Scenes/MainMenu/MainMenu.tscn`
- Launch with `-- --verify`

- **Hard.** Click the node named `Hard`. Snapshot `verify-output/menu-hard.png`. Hard is the pressed toggle.
- **Start.** Click the node named `Start`. Scene becomes `GameManager`. Snapshot `verify-output/game-start.png`. Score 0, Lives 3.

## Gotchas

Lookup uses node name `Start`, not the caption "Start Game". Click Hard before Start or the match uses Medium.
