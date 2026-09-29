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
