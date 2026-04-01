public partial class WoodBoard : ObstacleBase
{
	protected override bool UseStoneFeedback => false;

	public override void _Ready()
	{
		if (BreakSpeedThreshold <= 0.0f)
		{
			BreakSpeedThreshold = 140.0f;
		}

		base._Ready();
	}
}
