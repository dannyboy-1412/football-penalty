# Penalty loop

Aim a zone, time the power meter, shoot, and resolve the kick against the keeper.

## Sub-features

- `aim-zone` selects one of six goal zones
- `power-meter` oscillates, red above 0.9
- `shoot` run-up then ball flight
- `keeper-dive` Easy / Medium / Hard reaction
- `score-lives` +10 on goal, -1 life on save or miss

## How to get to it (user POV)

- Start Game from the menu
- Click a zone or use arrow keys, then Shoot or Space

## Driving it with VerifyDriver

Preconditions:

- Current scene is `GameManager` in `Aiming`
- Launch with `-- --verify` for the miss loop, or `-- --demo-shot` for one kick

- **Miss loop.** Aim `TopLeft`, wait until power > 0.93, press Space. Repeat three times. Console has `ROUND ... outcome=Missed` with lives 2, then 1, then 0.
- **One kick.** `--demo-shot` aims `TopRight` at power 0.5. Watch the keeper dive and the ball fly.
- **Proof.** Snapshots `pre-miss-1.png` through `pre-miss-3.png` show a red meter and TopLeft highlighted. Do not treat a `BallInFlight` frame as a result.

## Gotchas

Shot input is ignored outside `Aiming`. Power above 0.9 is always `Missed`, keeper or not.
