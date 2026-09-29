using Godot;

public enum TargetZone
{
	TopLeft,
	TopCenter,
	TopRight,
	BottomLeft,
	BottomCenter,
	BottomRight,
}

public enum Difficulty
{
	Easy,
	Medium,
	Hard,
}

public enum ShotOutcome
{
	Goal,
	Saved,
	Missed,
}

public enum MatchPhase
{
	Aiming,
	BallInFlight,
	ShowingResult,
	GameOver,
}

public readonly record struct Shot(TargetZone Zone, float Power)
{
	public float TravelSeconds => Mathf.Lerp(1.0f, 0.3f, Power);
	public bool IsOverTheBar => Power > 0.9f;
}

public readonly record struct KeeperDive(TargetZone Zone, float ReactionSeconds);
