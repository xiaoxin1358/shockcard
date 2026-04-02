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
	[Export] public float LoseEnergyThreshold = 10.0f;

	public GameResultState CurrentState { get; private set; } = GameResultState.Running;

	private EnergyManager _energyManager;
	private BossController _boss;
	private HUD _hud;
	private MapResetManager _mapReset;
	private SpawnManager _spawnManager;
	private PlayerController _player;
	private CardManager _cardManager;
	private bool _isRestarting;

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
		_spawnManager = GetTree().GetFirstNodeInGroup("spawn_manager") as SpawnManager;
		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		_cardManager = GetTree().GetFirstNodeInGroup("card_manager") as CardManager;

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
		_hud?.ShowGameResult("YOU LOSE!");
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
		_hud?.ShowGameResult("YOU WIN!");
	}

	public void RequestRestart()
	{
		if (_isRestarting)
		{
			return;
		}

		_isRestarting = true;

		ResetBattleLoop();
		_boss?.ResetBoss();

		if (_energyManager != null)
		{
			_energyManager.SetEnergy(_energyManager.StartEnergy);
		}

		if (_player != null)
		{
			_player.ResetHp();
			_player.Velocity = Vector2.Zero;
		}

		_spawnManager?.ResetRuntime(false);
		_cardManager?.ResetCards();

		_isRestarting = false;
	}

	private void OnEnergyChanged(float currentEnergy, float maxEnergy)
	{
		if (!LoseWhenEnergyEmpty || CurrentState != GameResultState.Running)
		{
			return;
		}

		if (currentEnergy > LoseEnergyThreshold)
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
