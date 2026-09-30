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
