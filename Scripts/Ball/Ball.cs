using System;
using Godot;

public partial class Ball : Node2D
{
	public event Action Arrived;

	private Vector2 _spot;
	private Tween _flight;

	public override void _Ready()
	{
		_spot = Position;
		if (GetNodeOrNull<ColorRect>("Body") is { } body)
			body.Visible = false;
		var poly = new Polygon2D { Color = new Color(0.96f, 0.96f, 0.98f), Name = "Face" };
		var points = new Vector2[18];
		for (int i = 0; i < points.Length; i++)
		{
			float angle = i / (float)points.Length * Mathf.Tau;
			points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 14f;
		}
		poly.Polygon = points;
		AddChild(poly);
		AddChild(new Polygon2D
		{
			Color = new Color(0.12f, 0.12f, 0.14f),
			Polygon =
			[
				new Vector2(-3, -8),
				new Vector2(4, -6),
				new Vector2(5, 1),
				new Vector2(-1, 5),
				new Vector2(-6, -1),
			],
		});
	}

	public void ResetToSpot()
	{
		_flight?.Kill();
		Position = _spot;
		Scale = Vector2.One;
		Rotation = 0f;
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
			_flight.TweenProperty(this, "rotation", Mathf.Tau * 2f, seconds);
		_flight.SetParallel(false);
		_flight.TweenCallback(Callable.From(() => Arrived?.Invoke()));
	}
}
