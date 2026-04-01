using Godot;

public partial class MapResetController : Node
{
	[Signal]
	public delegate void BossPhaseResetRequestedEventHandler(int phaseIndex, float thresholdPercent);

	public override void _EnterTree()
	{
		AddToGroup("map_reset");
	}

	public void RequestResetByBossPhase(int phaseIndex, float thresholdPercent)
	{
		GD.Print("[MapReset] Phase " + phaseIndex + " @ " + thresholdPercent + "%");
		EmitSignal(SignalName.BossPhaseResetRequested, phaseIndex, thresholdPercent);
	}
}
