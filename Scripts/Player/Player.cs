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
