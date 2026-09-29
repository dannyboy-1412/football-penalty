using System.Linq;
using Godot;

public partial class VerifyDriver : Node
{
	public const string OutputDir = "res://verify-output";

	public static void Ensure()
	{
		if (!OS.GetCmdlineUserArgs().Contains("--verify"))
			return;
		if (Engine.GetMainLoop() is not SceneTree tree)
			return;
		if (tree.Root.GetNodeOrNull("VerifyDriver") != null)
			return;
		tree.Root.CallDeferred(Node.MethodName.AddChild, new VerifyDriver { Name = "VerifyDriver" });
	}

	public override void _Ready()
	{
		DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(OutputDir));
		_ = Run();
	}

	private async System.Threading.Tasks.Task Run()
	{
		await WaitFrames(2);
		if (GetTree().CurrentScene is MainMenu)
		{
			Click(FindButton("Hard"));
			await WaitFrames(2);
			Snapshot("menu-hard");
			Click(FindButton("Start"));
			await WaitUntil(() => GetTree().CurrentScene is GameManager);
			await WaitFrames(4);
		}

		if (GetTree().CurrentScene is not GameManager game)
		{
			GD.PrintErr("VERIFY failed: game scene missing");
			GetTree().Quit(1);
			return;
		}

		Snapshot("game-start");
		if (game.Score.Lives != 3 || game.Score.Score != 0)
		{
			GD.PrintErr($"VERIFY failed: start state score={game.Score.Score} lives={game.Score.Lives}");
			GetTree().Quit(1);
			return;
		}

		for (int i = 0; i < 3; i++)
		{
			await WaitUntil(() => game.Phase == MatchPhase.Aiming);
			game.Kicker.Shot.AimAt(TargetZone.TopLeft);
			while (game.Kicker.Shot.Power < 0.93f)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			Snapshot($"pre-miss-{i + 1}");
			SendKey(Key.Space);
			await WaitFrames(2);
			if (game.Phase == MatchPhase.Aiming)
				game.Kicker.Shot.ShootNow();
			await WaitUntil(() => game.Phase is MatchPhase.ShowingResult or MatchPhase.GameOver);
		}

		await WaitUntil(() => game.Phase == MatchPhase.GameOver);
		await WaitFrames(3);
		Snapshot("game-over");
		if (game.Score.Lives != 0)
		{
			GD.PrintErr($"VERIFY failed: expected 0 lives, got {game.Score.Lives}");
			GetTree().Quit(1);
			return;
		}

		Click(FindButton("Restart"));
		await WaitUntil(() => game.Phase == MatchPhase.Aiming && game.Score.Lives == 3 && game.Score.Score == 0);
		await WaitFrames(4);
		Snapshot("restart");
		GD.Print("VERIFY passed");
		GetTree().Quit(0);
	}

	private async System.Threading.Tasks.Task WaitUntil(System.Func<bool> ready, int maxFrames = 1800)
	{
		int frames = 0;
		while (!ready())
		{
			if (!IsInsideTree() || frames++ > maxFrames)
				throw new System.TimeoutException("VERIFY wait timed out");
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async System.Threading.Tasks.Task WaitFrames(int n)
	{
		for (int i = 0; i < n; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void Snapshot(string slug)
	{
		Image image = GetViewport().GetTexture().GetImage();
		string path = $"{OutputDir}/{slug}.png";
		image.SavePng(path);
		GD.Print($"VERIFY snapshot {path}");
	}

	private Button FindButton(string name)
	{
		return FindButton(GetTree().CurrentScene, name)
			?? throw new System.InvalidOperationException($"VERIFY missing button {name}");
	}

	private static Button FindButton(Node node, string name)
	{
		if (node is Button button && button.Name == name)
			return button;
		foreach (Node child in node.GetChildren())
		{
			Button found = FindButton(child, name);
			if (found != null)
				return found;
		}

		return null;
	}

	private static void Click(Button button)
	{
		Vector2 point = button.GetGlobalRect().GetCenter();
		var press = new InputEventMouseButton
		{
			ButtonIndex = MouseButton.Left,
			Pressed = true,
			Position = point,
			GlobalPosition = point,
		};
		Input.ParseInputEvent(press);
		var release = (InputEventMouseButton)press.Duplicate();
		release.Pressed = false;
		Input.ParseInputEvent(release);
		button.EmitSignal(Button.SignalName.Pressed);
	}

	private static void SendKey(Key key)
	{
		var press = new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Pressed = true,
		};
		Input.ParseInputEvent(press);
		var release = (InputEventKey)press.Duplicate();
		release.Pressed = false;
		Input.ParseInputEvent(release);
	}
}
