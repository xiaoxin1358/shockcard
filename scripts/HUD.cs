using Godot;
using System.Collections.Generic;

public partial class HUD : Control
{
	private EnergyBarUI _energyUi;
	private CardDisplayUI _cardDisplayUi;
	private BossHpBarUI _bossHpBarUi;
	private PhaseHintUI _phaseHintUi;
	private Control _resultPanel;
	private Label _resultText;

	public override void _EnterTree()
	{
		AddToGroup("hud_controller");
	}

	public override void _Ready()
	{
		_energyUi = GetNodeOrNull<EnergyBarUI>("TopBar/EnergyPanel");
		_cardDisplayUi = GetNodeOrNull<CardDisplayUI>("TopBar/DeckPanel");
		_bossHpBarUi = GetNodeOrNull<BossHpBarUI>("TopBar/BossBarPanel");
		_phaseHintUi = GetNodeOrNull<PhaseHintUI>("PhaseBanner");
		_resultPanel = GetNodeOrNull<Control>("ResultPanel");
		_resultText = GetNodeOrNull<Label>("ResultPanel/ResultText");

		EnergyManager energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
		_energyUi?.BindEnergyManager(energyManager);

		ClearGameResult();
	}

	public void BindBoss(BossController boss)
	{
		_bossHpBarUi?.BindBoss(boss);
		_phaseHintUi?.BindBoss(boss);
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
}
