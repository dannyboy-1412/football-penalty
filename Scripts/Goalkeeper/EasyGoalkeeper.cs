using Godot;

public partial class EasyGoalkeeper : Goalkeeper
{
	public override float ReactionSeconds => 0.45f;

	protected override TargetZone ChooseDiveZone(Shot shot, RandomNumberGenerator rng)
		=> (TargetZone)rng.RandiRange(0, 5);
}
