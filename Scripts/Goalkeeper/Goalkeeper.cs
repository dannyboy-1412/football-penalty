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
