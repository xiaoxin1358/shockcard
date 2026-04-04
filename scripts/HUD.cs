using Godot;
using System.Collections.Generic;

public partial class HUD : Control
{
	[Signal]
	public delegate void ContinueRequestedEventHandler();

	[Signal]
	public delegate void BuffChoiceSelectedEventHandler(int choiceIndex);

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
	private PanelContainer _buffChoicePanel;
	private Label _buffChoiceTitle;
	private VBoxContainer _buffChoiceList;
	private readonly List<Button> _buffChoiceButtons = new();
	private readonly List<BuffCardData> _shownChoices = new();

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

		EnsureBuffChoiceUi();
		HideBuffChoices();

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

		HideBuffChoices();
	}

	public void ShowBuffChoices(IReadOnlyList<BuffCardData> choices)
	{
		EnsureBuffChoiceUi();
		if (_buffChoicePanel == null || choices == null || choices.Count <= 0)
		{
			return;
		}

		_shownChoices.Clear();
		for (int i = 0; i < choices.Count; i++)
		{
			if (choices[i] != null)
			{
				_shownChoices.Add(choices[i]);
			}
		}

		if (_shownChoices.Count <= 0)
		{
			return;
		}

		if (_buffChoiceTitle != null)
		{
			_buffChoiceTitle.Text = "Choose One Buff Card";
		}

		for (int i = 0; i < _buffChoiceButtons.Count; i++)
		{
			Button button = _buffChoiceButtons[i];
			if (button == null)
			{
				continue;
			}

			if (i < _shownChoices.Count)
			{
				BuffCardData card = _shownChoices[i];
				button.Visible = true;
				button.Disabled = false;
				button.Text = card.DisplayName + "\n" + card.Description;
			}
			else
			{
				button.Visible = false;
				button.Disabled = true;
				button.Text = string.Empty;
			}
		}

		_buffChoicePanel.Visible = true;
		if (_resultPanel != null)
		{
			_resultPanel.Visible = false;
		}
	}

	public void HideBuffChoices()
	{
		_shownChoices.Clear();
		if (_buffChoicePanel != null)
		{
			_buffChoicePanel.Visible = false;
		}
	}

	private void EnsureBuffChoiceUi()
	{
		if (_buffChoicePanel != null)
		{
			return;
		}

		_buffChoicePanel = new PanelContainer();
		_buffChoicePanel.Name = "BuffChoicePanel";
		_buffChoicePanel.SetAnchorsPreset(LayoutPreset.Center);
		_buffChoicePanel.OffsetLeft = -260.0f;
		_buffChoicePanel.OffsetTop = -170.0f;
		_buffChoicePanel.OffsetRight = 260.0f;
		_buffChoicePanel.OffsetBottom = 170.0f;
		_buffChoicePanel.Visible = false;
		AddChild(_buffChoicePanel);

		var content = new VBoxContainer();
		content.Name = "Content";
		content.SetAnchorsPreset(LayoutPreset.FullRect);
		content.OffsetLeft = 12.0f;
		content.OffsetTop = 12.0f;
		content.OffsetRight = -12.0f;
		content.OffsetBottom = -12.0f;
		content.AddThemeConstantOverride("separation", 10);
		_buffChoicePanel.AddChild(content);

		_buffChoiceTitle = new Label();
		_buffChoiceTitle.Name = "Title";
		_buffChoiceTitle.Text = "Choose One Buff Card";
		_buffChoiceTitle.HorizontalAlignment = HorizontalAlignment.Center;
		content.AddChild(_buffChoiceTitle);

		_buffChoiceList = new VBoxContainer();
		_buffChoiceList.Name = "ChoiceList";
		_buffChoiceList.AddThemeConstantOverride("separation", 8);
		content.AddChild(_buffChoiceList);

		_buffChoiceButtons.Clear();
		for (int i = 0; i < 3; i++)
		{
			var button = new Button();
			button.CustomMinimumSize = new Vector2(0, 56);
			button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			button.Text = "Choice";
			int capturedIndex = i;
			button.Pressed += () => OnBuffChoiceButtonPressed(capturedIndex);
			_buffChoiceList.AddChild(button);
			_buffChoiceButtons.Add(button);
		}
	}

	private void OnBuffChoiceButtonPressed(int index)
	{
		if (index < 0 || index >= _shownChoices.Count)
		{
			return;
		}

		EmitSignal(SignalName.BuffChoiceSelected, index);
	}

	private void OnContinuePressed()
	{
		EmitSignal(SignalName.ContinueRequested);
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
