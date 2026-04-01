public partial class StoneBoard : ObstacleBase
{
	protected override bool UseStoneFeedback => true;

	public override void _Ready()
	{
		if (BreakSpeedThreshold <= 0.0f)
		{
			BreakSpeedThreshold = 380.0f;
		}

		base._Ready();
	}
}
