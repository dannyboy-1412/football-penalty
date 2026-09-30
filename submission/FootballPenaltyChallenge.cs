// SUBMISSION COPY. Generated from Scripts/. Do not edit.
// Work stays in the separate files. Regenerate: powershell -File tools/concat-submission.ps1

// ===== Scripts/Ball/Ball.cs =====
using System;
using Godot;

public partial class Ball : Node2D
{
	public event Action Arrived;

	private Vector2 _spot;
	private Tween _flight;
	private Sprite2D _face;

	public override void _Ready()
	{
		_spot = Position;
		_face = GetNode<Sprite2D>("Face");
	}

	public void ResetToSpot()
	{
		_flight?.Kill();
		Position = _spot;
		Scale = Vector2.One;
		_face.Rotation = 0f;
		Visible = true;
	}

	public void Fly(Vector2 target, float seconds, bool caught)
	{
		_flight?.Kill();
		_flight = CreateTween();
		_flight.SetParallel();
		_flight.TweenProperty(this, "position", target, seconds)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
		Vector2 endScale = caught ? new Vector2(0.4f, 0.4f) : new Vector2(0.55f, 0.55f);
		_flight.TweenProperty(this, "scale", endScale, seconds);
		if (!caught)
			_flight.TweenProperty(_face, "rotation", Mathf.Tau * 2f, seconds);
		_flight.SetParallel(false);
		_flight.TweenCallback(Callable.From(() => Arrived?.Invoke()));
	}
}

// ===== Scripts/Core/GameManager.cs =====
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

// ===== Scripts/Core/GameResult.cs =====
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

// ===== Scripts/Core/PenaltyTypes.cs =====
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

// ===== Scripts/Core/ScoreManager.cs =====
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

// ===== Scripts/Core/Sfx.cs =====
using Godot;

public static class Sfx
{
	public static AudioStreamWav Kick => Tone(180, 0.09f, 0.7f);
	public static AudioStreamWav Goal => Chord(0.28f, 0.55f, 523, 659, 784);
	public static AudioStreamWav Save => Tone(90, 0.16f, 0.65f);
	public static AudioStreamWav Miss => Tone(220, 0.18f, 0.4f, fall: true);
	public static AudioStreamWav Crowd => Noise(1.4f, 0.12f);

	private static AudioStreamWav Tone(float hz, float seconds, float volume, bool fall = false)
	{
		int rate = 22050;
		int n = Mathf.Max(2, (int)(rate * seconds));
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = i / (float)n;
			float env = t < 0.1f ? t / 0.1f : 1f - (t - 0.1f) / 0.9f;
			float freq = fall ? hz * (1f - 0.5f * t) : hz;
			short sample = (short)(Mathf.Sin(t * seconds * freq * Mathf.Tau) * env * volume * short.MaxValue);
			data[i * 2] = (byte)(sample & 0xff);
			data[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
		}

		return Stream(data, rate);
	}

	private static AudioStreamWav Chord(float seconds, float volume, params float[] hz)
	{
		int rate = 22050;
		int n = Mathf.Max(2, (int)(rate * seconds));
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = i / (float)n;
			float env = t < 0.08f ? t / 0.08f : 1f - (t - 0.08f) / 0.92f;
			float mix = 0f;
			foreach (float f in hz)
				mix += Mathf.Sin(t * seconds * f * Mathf.Tau);
			short sample = (short)(mix / hz.Length * env * volume * short.MaxValue);
			data[i * 2] = (byte)(sample & 0xff);
			data[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
		}

		return Stream(data, rate);
	}

	private static AudioStreamWav Noise(float seconds, float volume)
	{
		int rate = 22050;
		int n = Mathf.Max(2, (int)(rate * seconds));
		var data = new byte[n * 2];
		uint seed = 1234567;
		for (int i = 0; i < n; i++)
		{
			seed ^= seed << 13;
			seed ^= seed >> 17;
			seed ^= seed << 5;
			float nse = (seed / (float)uint.MaxValue) * 2f - 1f;
			short sample = (short)(nse * volume * short.MaxValue);
			data[i * 2] = (byte)(sample & 0xff);
			data[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
		}

		return Stream(data, rate, loop: true);
	}

	private static AudioStreamWav Stream(byte[] data, int rate, bool loop = false)
	{
		var stream = new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = rate,
			Stereo = false,
			Data = data,
		};
		if (loop)
		{
			stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
			stream.LoopEnd = data.Length / 2;
		}

		return stream;
	}
}

// ===== Scripts/Goalkeeper/EasyGoalkeeper.cs =====
using Godot;

public partial class EasyGoalkeeper : Goalkeeper
{
	public override float ReactionSeconds => 0.45f;

	protected override TargetZone ChooseDiveZone(Shot shot, RandomNumberGenerator rng)
		=> (TargetZone)rng.RandiRange(0, 5);
}

// ===== Scripts/Goalkeeper/Goalkeeper.cs =====
using System;
using Godot;

public abstract partial class Goalkeeper : Node2D
{
	private enum KeeperPose
	{
		Ready,
		DiveLeft,
		DiveRight,
	}

	private Vector2 _center;
	private Tween _dive;
	private readonly RandomNumberGenerator _rng = new();
	private Texture2D _standArt;
	private Texture2D _diveArt;
	private Sprite2D _figure;
	private Sprite2D _shadow;

	public abstract float ReactionSeconds { get; }

	protected abstract TargetZone ChooseDiveZone(Shot shot, RandomNumberGenerator rng);

	public static Goalkeeper For(Difficulty difficulty) => difficulty switch
	{
		Difficulty.Easy => new EasyGoalkeeper(),
		Difficulty.Medium => new MediumGoalkeeper(),
		Difficulty.Hard => new HardGoalkeeper(),
		_ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
	};

	public override void _Ready()
	{
		_center = Position;
		BuildBody();
	}

	public void Seed(ulong seed) => _rng.Seed = seed;

	public KeeperDive ReadShot(Shot shot) => new KeeperDive(ChooseDiveZone(shot, _rng), ReactionSeconds);

	public void PlayDive(KeeperDive dive, Vector2 zoneGlobal)
	{
		_dive?.Kill();
		ResetPose();
		_dive = CreateTween();
		_dive.TweenInterval(dive.ReactionSeconds);
		Vector2 target = new Vector2(zoneGlobal.X, Mathf.Lerp(GlobalPosition.Y, zoneGlobal.Y, 0.55f));
		float lean = Mathf.Clamp((target.X - GlobalPosition.X) / 180f, -0.85f, 0.85f);
		KeeperPose pose = Mathf.Abs(lean) <= 0.12f
			? KeeperPose.Ready
			: lean < 0f ? KeeperPose.DiveLeft : KeeperPose.DiveRight;
		_dive.TweenCallback(Callable.From(() => Show(pose)));
		_dive.TweenProperty(this, "global_position", target, GameResult.DiveSeconds)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
	}

	public void ResetToCenter()
	{
		_dive?.Kill();
		Position = _center;
		ResetPose();
	}

	private void ResetPose()
	{
		Rotation = 0f;
		Scale = Vector2.One;
		Show(KeeperPose.Ready);
	}

	private void Show(KeeperPose pose)
	{
		bool diving = pose != KeeperPose.Ready;
		_figure.Texture = diving ? _diveArt : _standArt;
		_figure.FlipH = pose == KeeperPose.DiveLeft;
		_figure.Offset = diving ? new Vector2(0, -28) : new Vector2(0, -56);
		_shadow.Position = new Vector2(0, diving ? 2 : -2);
		_shadow.Scale = diving ? new Vector2(1.4f, 0.6f) : Vector2.One;
	}

	private void BuildBody()
	{
		_standArt = GD.Load<Texture2D>("res://Art/keeper_stand.svg");
		_diveArt = GD.Load<Texture2D>("res://Art/keeper_dive.svg");
		_shadow = new Sprite2D
		{
			Name = "Shadow",
			Texture = GD.Load<Texture2D>("res://Art/shadow.svg"),
			Centered = true,
		};
		_figure = new Sprite2D
		{
			Name = "Figure",
			Centered = true,
		};
		AddChild(_shadow);
		AddChild(_figure);
		Show(KeeperPose.Ready);
	}
}

// ===== Scripts/Goalkeeper/HardGoalkeeper.cs =====
using Godot;

public partial class HardGoalkeeper : Goalkeeper
{
	public override float ReactionSeconds => 0.15f;

	protected override TargetZone ChooseDiveZone(Shot shot, RandomNumberGenerator rng)
		=> rng.Randf() < 0.65f ? shot.Zone : (TargetZone)rng.RandiRange(0, 5);
}

// ===== Scripts/Goalkeeper/MediumGoalkeeper.cs =====
using Godot;

public partial class MediumGoalkeeper : Goalkeeper
{
	public override float ReactionSeconds => 0.30f;

	protected override TargetZone ChooseDiveZone(Shot shot, RandomNumberGenerator rng)
		=> rng.Randf() < 0.4f ? shot.Zone : (TargetZone)rng.RandiRange(0, 5);
}

// ===== Scripts/Player/Player.cs =====
using System;
using Godot;

public partial class Player : Node2D
{
	public event Action<Shot> ShotTaken;

	private ShotController _shot;
	private Vector2 _home;
	private Tween _runUp;

	public ShotController Shot => _shot;

	public override void _Ready()
	{
		_home = Position;
		_shot = GetNode<ShotController>("ShotController");
		_shot.ShotRequested += OnShotRequested;
	}

	public void ResetToHome()
	{
		_runUp?.Kill();
		Position = _home;
		_shot.ResetMeter();
	}

	private void OnShotRequested(Shot shot)
	{
		_runUp?.Kill();
		_runUp = CreateTween();
		_runUp.TweenProperty(this, "position", _home + new Vector2(0, -48), 0.22)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
		_runUp.TweenCallback(Callable.From(() => ShotTaken?.Invoke(shot)));
	}
}

// ===== Scripts/Player/ShotController.cs =====
using System;
using System.Collections.Generic;
using Godot;

public partial class ShotController : CanvasLayer
{
	public event Action<Shot> ShotRequested;

	private const float CycleSeconds = 0.8f;

	private ProgressBar _meter;
	private Button _shoot;
	private readonly Dictionary<TargetZone, Button> _zoneButtons = new();
	private readonly Dictionary<TargetZone, ColorRect> _highlights = new();
	private float _power;
	private int _direction = 1;

	public TargetZone SelectedZone { get; private set; } = TargetZone.BottomCenter;
	public float Power => _power;
	public bool InputEnabled { get; set; } = true;

	public override void _Ready()
	{
		_meter = GetNode<ProgressBar>("PowerMeter");
		_shoot = GetNode<Button>("ShootButton");
		_shoot.Pressed += TryShoot;
		_meter.MinValue = 0;
		_meter.MaxValue = 1;
	}

	public override void _Process(double delta)
	{
		if (!InputEnabled)
			return;

		_power += _direction * (float)delta / CycleSeconds;
		if (_power >= 1f)
		{
			_power = 1f;
			_direction = -1;
		}
		else if (_power <= 0f)
		{
			_power = 0f;
			_direction = 1;
		}

		_meter.Value = _power;
		_meter.Modulate = _power > 0.9f ? new Color(1f, 0.28f, 0.22f) : Colors.White;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!InputEnabled)
			return;

		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			switch (key.Keycode)
			{
				case Key.Space:
					TryShoot();
					GetViewport().SetInputAsHandled();
					break;
				case Key.Left:
					MoveAim(-1, 0);
					break;
				case Key.Right:
					MoveAim(1, 0);
					break;
				case Key.Up:
					MoveAim(0, -1);
					break;
				case Key.Down:
					MoveAim(0, 1);
					break;
			}
		}
	}

	public void BindZones(IReadOnlyDictionary<TargetZone, Marker2D> zones)
	{
		foreach (var (zone, marker) in zones)
		{
			if (marker.GetNodeOrNull<ColorRect>("Highlight") is { } highlight)
				_highlights[zone] = highlight;

			var button = new Button
			{
				Flat = true,
				Position = marker.GlobalPosition + new Vector2(-72, -44),
				Size = new Vector2(144, 88),
			};
			button.AddThemeColorOverride("font_color", Colors.Transparent);
			button.Pressed += () => SelectZone(zone);
			AddChild(button);
			_zoneButtons[zone] = button;
		}

		SelectZone(SelectedZone);
	}

	public void ResetMeter()
	{
		_power = 0f;
		_direction = 1;
		_meter.Value = 0;
		_meter.Modulate = Colors.White;
		InputEnabled = true;
	}

	public void AimAt(TargetZone zone) => SelectZone(zone);

	public void ShootNow() => TryShoot();

	private void MoveAim(int columnDelta, int rowDelta)
	{
		int index = (int)SelectedZone;
		int column = Mathf.Clamp(index % 3 + columnDelta, 0, 2);
		int row = Mathf.Clamp(index / 3 + rowDelta, 0, 1);
		SelectZone((TargetZone)(row * 3 + column));
	}

	private void SelectZone(TargetZone zone)
	{
		SelectedZone = zone;
		foreach (var (candidate, highlight) in _highlights)
			highlight.Color = candidate == zone ? new Color(1f, 0.92f, 0.2f, 0.38f) : new Color(1f, 1f, 1f, 0.08f);
	}

	private void TryShoot()
	{
		if (!InputEnabled)
			return;

		InputEnabled = false;
		ShotRequested?.Invoke(new Shot(SelectedZone, _power));
	}
}

// ===== Scripts/UI/MainMenu.cs =====
using System.Linq;
using Godot;

public partial class MainMenu : Control
{
	private Difficulty _difficulty = Difficulty.Medium;

	public override void _Ready()
	{
		GetNode<Button>("Center/Easy").Pressed += () => _difficulty = Difficulty.Easy;
		GetNode<Button>("Center/Medium").Pressed += () => _difficulty = Difficulty.Medium;
		GetNode<Button>("Center/Hard").Pressed += () => _difficulty = Difficulty.Hard;
		GetNode<Button>("Center/Start").Pressed += StartGame;
		GetNode<Button>("Center/Medium").ButtonPressed = true;
		VerifyDriver.Ensure();
		if (OS.GetCmdlineUserArgs().Contains("--demo-shot"))
			CallDeferred(nameof(StartGame));
	}

	private void StartGame()
	{
		var game = GD.Load<PackedScene>("res://Scenes/Game/Game.tscn").Instantiate<GameManager>();
		game.Difficulty = _difficulty;
		GetTree().ChangeSceneToNode(game);
	}
}

// ===== Scripts/UI/UIManager.cs =====
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
