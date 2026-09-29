public sealed class ScoreManager
{
	public const int StartingLives = 3;
	public const int GoalPoints = 10;
	public const int StreakBonus = 5;

	public int Score { get; private set; }
	public int Lives { get; private set; } = StartingLives;
	public int Goals { get; private set; }
	public int Saves { get; private set; }
	public int Misses { get; private set; }
	public int Shots { get; private set; }
	public int Streak { get; private set; }

	public bool IsGameOver => Lives <= 0;

	public void Apply(GameResult result)
	{
		Shots++;
		switch (result.Outcome)
		{
			case ShotOutcome.Goal:
				Goals++;
				Streak++;
				Score += GoalPoints;
				if (Streak >= 2)
					Score += StreakBonus;
				break;
			case ShotOutcome.Saved:
				Saves++;
				Streak = 0;
				Lives--;
				break;
			case ShotOutcome.Missed:
				Misses++;
				Streak = 0;
				Lives--;
				break;
		}
	}

	public void Reset()
	{
		Score = 0;
		Lives = StartingLives;
		Goals = 0;
		Saves = 0;
		Misses = 0;
		Shots = 0;
		Streak = 0;
	}
}
