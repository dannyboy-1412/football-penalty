using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameManager : Node2D
{
	public Difficulty Difficulty { get; set; } = Difficulty.Medium;

	private MatchPhase _phase;
	private int _roundId;
	private GameResult _pending;
	private readonly ScoreManager _score = new();
	private Player _player;
	private Ball _ball;
	private Goalkeeper _keeper;
	private UIManager _ui;
	private Dictionary<TargetZone, Marker2D> _zones;
	private Marker2D _overBar;
	private AudioStreamPlayer _sfx;

	public ScoreManager Score => _score;
	public MatchPhase Phase => _phase;
	public UIManager UI => _ui;
	public Player Kicker => _player;

	public override void _Ready()
	{
		_player = GetNode<Player>("Player");
		_ball = GetNode<Ball>("Ball");
		_ui = GetNode<UIManager>("UIManager");
		Node2D goal = GetNode<Node2D>("Goal");
		_zones = new Dictionary<TargetZone, Marker2D>();
		foreach (TargetZone zone in Enum.GetValues<TargetZone>())
			_zones[zone] = goal.GetNode<Marker2D>(zone.ToString());
		_overBar = goal.GetNode<Marker2D>("OverTheBar");

		_keeper = Goalkeeper.For(Difficulty);
		_keeper.Name = "Goalkeeper";
		GetNode<Marker2D>("KeeperSlot").AddChild(_keeper);

		_sfx = new AudioStreamPlayer { Name = "Sfx" };
		AddChild(_sfx);
		AddChild(new AudioStreamPlayer
		{
			Name = "Crowd",
			Stream = Sfx.Crowd,
			VolumeDb = -18,
			Autoplay = true,
		});

		_player.Shot.BindZones(_zones);
		_player.ShotTaken += OnShotTaken;
		_ball.Arrived += OnBallArrived;
		_ui.RestartRequested += Restart;
		_ui.MainMenuRequested += GoToMainMenu;

		VerifyDriver.Ensure();
		if (OS.GetCmdlineUserArgs().Contains("--demo-shot"))
			_ = RunDemoShot();

		BeginRound();
	}

	public void Restart()
	{
		_roundId++;
		_score.Reset();
		_ui.HideGameOver();
		BeginRound();
	}

	public void GoToMainMenu()
	{
		_roundId++;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu/MainMenu.tscn");
	}

	private void OnShotTaken(Shot shot)
	{
		if (_phase != MatchPhase.Aiming)
			return;

		_phase = MatchPhase.BallInFlight;
		KeeperDive dive = _keeper.ReadShot(shot);
		_pending = GameResult.Resolve(shot, dive);

		Vector2 zonePos = _zones[shot.Zone].GlobalPosition;
		Vector2 divePos = _zones[dive.Zone].GlobalPosition;
		_keeper.PlayDive(dive, divePos);

		Vector2 ballTarget = shot.IsOverTheBar
			? _overBar.GlobalPosition
			: _pending.Outcome == ShotOutcome.Saved ? divePos : zonePos;
		_ball.Fly(ballTarget, shot.TravelSeconds, _pending.Outcome == ShotOutcome.Saved);
		PlaySfx(Sfx.Kick);
	}

	private async void OnBallArrived()
	{
		_score.Apply(_pending);
		_ui.ShowResult(_pending, _score);
		_phase = MatchPhase.ShowingResult;
		GD.Print($"ROUND zone={_pending.Shot.Zone} power={_pending.Shot.Power:0.00} outcome={_pending.Outcome} score={_score.Score} lives={_score.Lives}");
		PlaySfx(_pending.Outcome switch
		{
			ShotOutcome.Goal => Sfx.Goal,
			ShotOutcome.Saved => Sfx.Save,
			_ => Sfx.Miss,
		});

		int id = _roundId;
		await ToSignal(GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree() || id != _roundId)
			return;

		if (_score.IsGameOver)
		{
			_phase = MatchPhase.GameOver;
			_player.Shot.InputEnabled = false;
			_ui.ShowGameOver(_score);
			return;
		}

		BeginRound();
	}

	private void BeginRound()
	{
		_phase = MatchPhase.Aiming;
		_ball.ResetToSpot();
		_player.ResetToHome();
		_keeper.ResetToCenter();
		_ui.HideResult();
		_ui.RefreshHud(_score);
	}

	private void PlaySfx(AudioStreamWav stream)
	{
		_sfx.Stream = stream;
		_sfx.Play();
	}

	private async System.Threading.Tasks.Task RunDemoShot()
	{
		_player.Shot.AimAt(TargetZone.TopRight);
		while (IsInsideTree() && _player.Shot.Power < 0.5f)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (IsInsideTree())
			_player.Shot.ShootNow();
	}
}
