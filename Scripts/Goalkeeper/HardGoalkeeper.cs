using Godot;

public partial class HardGoalkeeper : Goalkeeper
{
	public override float ReactionSeconds => 0.15f;

	protected override TargetZone ChooseDiveZone(Shot shot, RandomNumberGenerator rng)
		=> rng.Randf() < 0.65f ? shot.Zone : (TargetZone)rng.RandiRange(0, 5);
}
