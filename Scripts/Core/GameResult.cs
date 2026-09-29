public sealed record GameResult(Shot Shot, KeeperDive Dive, ShotOutcome Outcome)
{
	public const float DiveSeconds = 0.25f;

	public static GameResult Resolve(Shot shot, KeeperDive dive)
	{
		if (shot.IsOverTheBar)
			return new GameResult(shot, dive, ShotOutcome.Missed);

		bool sameZone = dive.Zone == shot.Zone;
		bool arrivedInTime = dive.ReactionSeconds + DiveSeconds <= shot.TravelSeconds;
		ShotOutcome outcome = sameZone && arrivedInTime ? ShotOutcome.Saved : ShotOutcome.Goal;
		return new GameResult(shot, dive, outcome);
	}
}
