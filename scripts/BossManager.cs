using Godot;

public partial class BossManager : Node
{
	[Export] public float BaseSettlementDamage = 60.0f;

	private CardManager _cardManager;
	private BossController _boss;
	private MapResetManager _mapReset;
	private GameManager _gameManager;
	private HUD _hud;

	public override void _Ready()
	{
		_cardManager = GetTree().GetFirstNodeInGroup("card_manager") as CardManager;
		_boss = GetTree().GetFirstNodeInGroup("boss") as BossController;
		_mapReset = GetTree().GetFirstNodeInGroup("map_reset") as MapResetManager;
		_gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		_hud = GetTree().GetFirstNodeInGroup("hud_controller") as HUD;

		if (_cardManager != null)
		{
			_cardManager.Connect("HandSettled", Callable.From<string, float>(OnHandSettled));
		}

		if (_boss != null)
		{
			_boss.Connect("PhaseChanged", Callable.From<int, float>(OnBossPhaseChanged));
			_boss.Connect("Defeated", Callable.From(OnBossDefeated));
		}

		_hud?.BindBoss(_boss);
	}

	private void OnHandSettled(string handName, float multiplier)
	{
		if (_boss == null || !IsBattleRunning())
		{
			return;
		}

		float damage = BaseSettlementDamage * multiplier;
		_boss.TakeDamage(damage);
		GD.Print("[BossDamage] " + handName + " -> " + damage.ToString("0.0"));
	}

	private void OnBossPhaseChanged(int phaseIndex, float thresholdPercent)
	{
		if (!IsBattleRunning())
		{
			return;
		}

		_mapReset?.RequestResetByBossPhase(phaseIndex, thresholdPercent);
	}

	private void OnBossDefeated()
	{
		GD.Print("[Boss] Defeated");
	}

	private bool IsBattleRunning()
	{
		return _gameManager == null || _gameManager.CurrentState == GameResultState.Running;
	}
}
