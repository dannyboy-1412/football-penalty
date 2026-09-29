using System;
using Godot;

public abstract partial class Goalkeeper : Node2D
{
	private Vector2 _center;
	private Tween _dive;
	private readonly RandomNumberGenerator _rng = new();

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
		_dive.TweenCallback(Callable.From(() =>
		{
			Rotation = lean;
			Scale = new Vector2(1.25f, 0.72f);
		}));
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
	}

	private void BuildBody()
	{
		AddChild(new ColorRect
		{
			OffsetLeft = -22,
			OffsetTop = -36,
			OffsetRight = 22,
			OffsetBottom = 0,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.12f, 0.42f, 0.28f),
		});
		AddChild(new ColorRect
		{
			OffsetLeft = -22,
			OffsetTop = -78,
			OffsetRight = 22,
			OffsetBottom = -36,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.95f, 0.82f, 0.18f),
		});
		AddChild(new ColorRect
		{
			OffsetLeft = -14,
			OffsetTop = -102,
			OffsetRight = 14,
			OffsetBottom = -78,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.93f, 0.74f, 0.55f),
		});
		AddChild(new ColorRect
		{
			OffsetLeft = -34,
			OffsetTop = -70,
			OffsetRight = -22,
			OffsetBottom = -50,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.95f, 0.95f, 0.97f),
		});
		AddChild(new ColorRect
		{
			OffsetLeft = 22,
			OffsetTop = -70,
			OffsetRight = 34,
			OffsetBottom = -50,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Color = new Color(0.95f, 0.95f, 0.97f),
		});
	}
}
