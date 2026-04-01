using Godot;

public partial class MapResetManager : Node
{
	[Signal]
	public delegate void BossPhaseResetRequestedEventHandler(int phaseIndex, float thresholdPercent);

	[Export] public bool KeepPlayerPositionOnPhaseReset = true;

	private SpawnManager _spawnManager;

	public override void _EnterTree()
	{
		AddToGroup("map_reset");
	}

	public override void _Ready()
	{
		_spawnManager = GetTree().GetFirstNodeInGroup("spawn_manager") as SpawnManager;
	}

	public void RequestResetByBossPhase(int phaseIndex, float thresholdPercent)
	{
		GD.Print("[MapReset] Phase " + phaseIndex + " @ " + thresholdPercent + "%");
		EmitSignal(SignalName.BossPhaseResetRequested, phaseIndex, thresholdPercent);
		_spawnManager?.ResetRuntime(KeepPlayerPositionOnPhaseReset);
	}

	public void RequestResetKeepPlayerPosition()
	{
		_spawnManager?.ResetRuntime(true);
	}

	public void RequestResetAndRespawnPlayer()
	{
		_spawnManager?.ResetRuntime(false);
	}
}
