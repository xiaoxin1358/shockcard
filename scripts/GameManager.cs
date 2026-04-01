using Godot;

public partial class GameManager : Node
{
	[Signal]
	public delegate void GameStateChangedEventHandler(GameResultState newState);

	[Signal]
	public delegate void GameWonEventHandler();

	[Signal]
	public delegate void GameLostEventHandler();

	[Export] public bool LoseWhenEnergyEmpty = true;

	public GameResultState CurrentState { get; private set; } = GameResultState.Running;

	private EnergyManager _energyManager;
	private BossController _boss;
	private HUD _hud;
	private MapResetManager _mapReset;

	public override void _EnterTree()
	{
		AddToGroup("game_manager");
	}

	public override void _Ready()
	{
		_energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
		_boss = GetTree().GetFirstNodeInGroup("boss") as BossController;
		_hud = GetTree().GetFirstNodeInGroup("hud_controller") as HUD;
		_mapReset = GetTree().GetFirstNodeInGroup("map_reset") as MapResetManager;

		if (_energyManager != null)
		{
			_energyManager.Connect("EnergyChanged", Callable.From<float, float>(OnEnergyChanged));
		}

		if (_boss != null)
		{
			_boss.Connect("Defeated", Callable.From(OnBossDefeated));
		}

		if (_mapReset != null)
		{
			_mapReset.Connect("BossPhaseResetRequested", Callable.From<int, float>(OnBossPhaseResetRequested));
		}

		StartBattleLoop();
	}

	public void StartBattleLoop()
	{
		CurrentState = GameResultState.Running;
		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
		_hud?.ClearGameResult();
	}

	public void ResetBattleLoop()
	{
		StartBattleLoop();
	}

	public void TryTriggerLose(string reason)
	{
		if (CurrentState != GameResultState.Running)
		{
			return;
		}

		CurrentState = GameResultState.Lost;
		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
		EmitSignal(SignalName.GameLost);
		_hud?.ShowGameResult("Defeat - " + reason);
	}

	public void TryTriggerWin()
	{
		if (CurrentState != GameResultState.Running)
		{
			return;
		}

		CurrentState = GameResultState.Won;
		EmitSignal(SignalName.GameStateChanged, (int)CurrentState);
		EmitSignal(SignalName.GameWon);
		_hud?.ShowGameResult("Victory - Boss Defeated");
	}

	private void OnEnergyChanged(float currentEnergy, float maxEnergy)
	{
		if (!LoseWhenEnergyEmpty || CurrentState != GameResultState.Running)
		{
			return;
		}

		if (currentEnergy > 0.0f)
		{
			return;
		}

		if (_boss != null && _boss.CurrentHp > 0.0f)
		{
			TryTriggerLose("Energy Depleted");
		}
	}

	private void OnBossDefeated()
	{
		TryTriggerWin();
	}

	private void OnBossPhaseResetRequested(int phaseIndex, float thresholdPercent)
	{
		if (CurrentState == GameResultState.Running)
		{
			GD.Print("[GameLoop] Continue after phase reset: " + phaseIndex);
		}
	}
}
