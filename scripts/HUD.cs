using Godot;
using System.Collections.Generic;

public partial class HUD : Control
{
	private EnergyBarUI _energyUi;
	private CardDisplayUI _cardDisplayUi;
	private Label _bossName;
	private TextureProgressBar _bossHpBar;
	private Label _bossHpText;
	private BossController _boundBoss;
	private PhaseHintUI _phaseHintUi;
	private Control _debugPanel;
	private Label _debugText;
	private Control _resultPanel;
	private Label _resultText;
	private Button _continueButton;
	private Button _quitButton;

	public override void _EnterTree()
	{
		AddToGroup("hud_controller");
	}

	public override void _Ready()
	{
		_energyUi = GetNodeOrNull<EnergyBarUI>("TopBar/EnergyPanel");
		_cardDisplayUi = GetNodeOrNull<CardDisplayUI>("TopBar/DeckPanel");
		_bossName = GetNodeOrNull<Label>("BossTop/BossBarPanel/BossName");
		_bossHpBar = GetNodeOrNull<TextureProgressBar>("BossTop/BossBarPanel/BossHpBar");
		_bossHpText = GetNodeOrNull<Label>("BossTop/BossBarPanel/BossHpText");
		_phaseHintUi = GetNodeOrNull<PhaseHintUI>("PhaseBanner");
		_debugPanel = GetNodeOrNull<Control>("DebugPanel");
		_debugText = GetNodeOrNull<Label>("DebugPanel/DebugText");
		_resultPanel = GetNodeOrNull<Control>("ResultPanel");
		_resultText = GetNodeOrNull<Label>("ResultPanel/ResultContent/ResultText");
		_continueButton = GetNodeOrNull<Button>("ResultPanel/ResultContent/ActionButtons/ContinueButton");
		_quitButton = GetNodeOrNull<Button>("ResultPanel/ResultContent/ActionButtons/QuitButton");

		if (_continueButton != null)
		{
			_continueButton.Pressed += OnContinuePressed;
		}

		if (_quitButton != null)
		{
			_quitButton.Pressed += OnQuitPressed;
		}

		EnergyManager energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
		_energyUi?.BindEnergyManager(energyManager);

		BossController boss = GetTree().GetFirstNodeInGroup("boss") as BossController;
		BindBoss(boss);

		SetDebugVisible(false);
		SetDebugText(string.Empty);

		ClearGameResult();
	}

	public void SetDebugVisible(bool visible)
	{
		if (_debugPanel != null)
		{
			_debugPanel.Visible = visible;
		}
	}

	public void SetDebugText(string text)
	{
		if (_debugText != null)
		{
			_debugText.Text = text;
		}
	}

	public void BindBoss(BossController boss)
	{
		if (_boundBoss != null && IsInstanceValid(_boundBoss))
		{
			Callable hpChangedCallable = Callable.From<float, float>(OnBossHpChanged);
			if (_boundBoss.IsConnected(BossController.SignalName.HpChanged, hpChangedCallable))
			{
				_boundBoss.Disconnect(BossController.SignalName.HpChanged, hpChangedCallable);
			}
		}

		_boundBoss = boss;
		if (_bossName != null)
		{
			_bossName.Text = "Boss HP";
		}

		if (_boundBoss == null)
		{
			if (_bossHpBar != null)
			{
				_bossHpBar.Value = 0.0f;
			}

			if (_bossHpText != null)
			{
				_bossHpText.Text = "0 / 0";
			}

			return;
		}

		Callable connectedCallable = Callable.From<float, float>(OnBossHpChanged);
		if (!_boundBoss.IsConnected(BossController.SignalName.HpChanged, connectedCallable))
		{
			_boundBoss.Connect(BossController.SignalName.HpChanged, connectedCallable);
		}

		OnBossHpChanged(_boundBoss.CurrentHp, _boundBoss.MaxHp);
		_phaseHintUi?.BindBoss(boss);
	}

	private void OnBossHpChanged(float currentHp, float maxHp)
	{
		if (_bossHpBar == null)
		{
			return;
		}

		_bossHpBar.MaxValue = maxHp;
		_bossHpBar.Value = currentHp;

		if (_bossHpText != null)
		{
			_bossHpText.Text = Mathf.CeilToInt(currentHp) + " / " + Mathf.CeilToInt(maxHp);
		}
	}

	public Vector2 GetSlotGlobalAnchor(int slotIndex)
	{
		if (_cardDisplayUi == null)
		{
			return GlobalPosition;
		}

		return _cardDisplayUi.GetSlotGlobalAnchor(slotIndex);
	}

	public void OnCardCollected(int slotIndex, CardData data, int currentCount, int maxCount)
	{
		_cardDisplayUi?.OnCardCollected(slotIndex, data, currentCount, maxCount);
	}

	public void OnHandSettled(PokerHandResult result, IReadOnlyList<CardData> keptCards, int maxSlots)
	{
		_cardDisplayUi?.OnHandSettled(result, keptCards, maxSlots);
	}

	public void OnCardsReset(int maxSlots)
	{
		_cardDisplayUi?.ResetDisplay(maxSlots);
	}

	public void ShowGameResult(string text)
	{
		if (_resultPanel == null || _resultText == null)
		{
			return;
		}

		_resultText.Text = text;
		_resultPanel.Visible = true;
		_resultPanel.Modulate = new Color(1, 1, 1, 1);
	}

	public void ClearGameResult()
	{
		if (_resultPanel != null)
		{
			_resultPanel.Visible = false;
		}
	}

	private void OnContinuePressed()
	{
		GameManager gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		gameManager?.RequestRestart();
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
