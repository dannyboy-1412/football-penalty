using System;
using Godot;

public partial class UIManager : CanvasLayer
{
	public event Action RestartRequested;
	public event Action MainMenuRequested;

	private Label _score;
	private Label _lives;
	private Label _banner;
	private Control _gameOver;
	private Label _finalScore;
	private Label _stats;

	public override void _Ready()
	{
		_score = GetNode<Label>("ScoreLabel");
		_lives = GetNode<Label>("LivesLabel");
		_banner = GetNode<Label>("Banner");
		_gameOver = GetNode<Control>("GameOver");
		_finalScore = _gameOver.GetNode<Label>("Panel/FinalScore");
		_stats = _gameOver.GetNode<Label>("Panel/Stats");
		_gameOver.GetNode<Button>("Panel/Restart").Pressed += () => RestartRequested?.Invoke();
		_gameOver.GetNode<Button>("Panel/MainMenu").Pressed += () => MainMenuRequested?.Invoke();
		HideResult();
		HideGameOver();
	}

	public void RefreshHud(ScoreManager score)
	{
		_score.Text = $"Score  {score.Score}";
		_lives.Text = $"Lives  {score.Lives}";
	}

	public void ShowResult(GameResult result, ScoreManager score)
	{
		_banner.Text = BannerText(result, score);
		_banner.Modulate = result.Outcome switch
		{
			ShotOutcome.Goal => new Color(1f, 0.86f, 0.2f),
			ShotOutcome.Saved => new Color(0.45f, 0.75f, 1f),
			_ => new Color(0.85f, 0.85f, 0.85f),
		};
		_banner.Visible = true;
		RefreshHud(score);
	}

	public void HideResult() => _banner.Visible = false;

	public void ShowGameOver(ScoreManager score)
	{
		HideResult();
		_finalScore.Text = $"Score  {score.Score}";
		_stats.Text = $"{score.Goals} goals   {score.Saves} saves   {score.Misses} misses   {score.Shots} shots";
		_gameOver.Visible = true;
		RefreshHud(score);
	}

	public void HideGameOver() => _gameOver.Visible = false;

	private static string BannerText(GameResult result, ScoreManager score)
	{
		if (result.Outcome == ShotOutcome.Goal && score.Streak >= 2)
			return $"GOAL!  STREAK x{score.Streak}  +{ScoreManager.GoalPoints + ScoreManager.StreakBonus}";
		return result.Outcome switch
		{
			ShotOutcome.Goal => "GOAL!",
			ShotOutcome.Saved => "SAVED!",
			_ => "MISSED!",
		};
	}
}
