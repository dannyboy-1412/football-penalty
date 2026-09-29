using Godot;

public partial class MediumGoalkeeper : Goalkeeper
{
	public override float ReactionSeconds => 0.30f;

	protected override TargetZone ChooseDiveZone(Shot shot, RandomNumberGenerator rng)
		=> rng.Randf() < 0.4f ? shot.Zone : (TargetZone)rng.RandiRange(0, 5);
}
