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
