using Godot;
using System.Collections.Generic;

public partial class CardDisplayUI : VBoxContainer
{
	[Export] public NodePath SettlementPanelPath = "../../SettlementPanel";
	[Export] public NodePath SettlementTextPath = "../../SettlementPanel/SettlementText";

	private Label _deckCount;
	private Control _settlementPanel;
	private Label _settlementText;
	private FeedbackManager _feedback;
	private readonly List<Control> _slotRoots = new();
	private readonly List<Label> _slotLabels = new();

	public override void _Ready()
	{
		_deckCount = GetNodeOrNull<Label>("DeckCount");
		_settlementPanel = GetNodeOrNull<Control>(SettlementPanelPath);
		_settlementText = GetNodeOrNull<Label>(SettlementTextPath);
		_feedback = GetTree().GetFirstNodeInGroup("feedback_manager") as FeedbackManager;

		CacheCardSlots();
		SetDeckCount(0, _slotRoots.Count > 0 ? _slotRoots.Count : 5);

		if (_settlementPanel != null)
		{
			_settlementPanel.Visible = false;
		}
	}

	public Vector2 GetSlotGlobalAnchor(int slotIndex)
	{
		if (_slotRoots.Count == 0)
		{
			return GlobalPosition;
		}

		int safeIndex = Mathf.Clamp(slotIndex, 0, _slotRoots.Count - 1);
		Control slot = _slotRoots[safeIndex];
		return slot.GlobalPosition + (slot.Size * 0.5f);
	}

	public void OnCardCollected(int slotIndex, CardData data, int currentCount, int maxCount)
	{
		if (_slotLabels.Count == 0)
		{
			SetDeckCount(currentCount, maxCount);
			return;
		}

		int safeIndex = Mathf.Clamp(slotIndex, 0, _slotLabels.Count - 1);
		Label label = _slotLabels[safeIndex];
		label.Text = data.DisplayText;

		Control slot = _slotRoots[safeIndex];
		slot.Scale = new Vector2(0.75f, 0.75f);
		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(slot, "scale", Vector2.One, 0.18f);

		SetDeckCount(currentCount, maxCount);
	}

	public void OnHandSettled(PokerHandResult result, IReadOnlyList<CardData> keptCards, int maxSlots)
	{
		_feedback?.AddSettlementFeedback(result.DamageMultiplier);
		ShowSettlementHint(result);
		ApplySettlementVisuals(result);

		var refreshTimer = GetTree().CreateTimer(0.28f);
		refreshTimer.Timeout += () => { RefreshCardSlots(keptCards, maxSlots); };
	}

	private void CacheCardSlots()
	{
		_slotRoots.Clear();
		_slotLabels.Clear();

		Node slotsContainer = GetNodeOrNull("CardSlots");
		if (slotsContainer == null)
		{
			return;
		}

		for (int i = 0; i < slotsContainer.GetChildCount(); i++)
		{
			Control slotRoot = slotsContainer.GetChild(i) as Control;
			if (slotRoot == null)
			{
				continue;
			}

			Label label = slotRoot.GetNodeOrNull<Label>("CardLabel");
			if (label == null)
			{
				continue;
			}

			label.Text = "--";
			_slotRoots.Add(slotRoot);
			_slotLabels.Add(label);
		}
	}

	private void ShowSettlementHint(PokerHandResult result)
	{
		if (_settlementPanel == null || _settlementText == null)
		{
			return;
		}

		_settlementPanel.Visible = true;
		_settlementPanel.Modulate = new Color(1, 1, 1, 1);
		_settlementText.Text = result.HandName + " x" + result.DamageMultiplier.ToString("0.00");

		var hideTimer = GetTree().CreateTimer(1.2f);
		hideTimer.Timeout += () =>
		{
			if (_settlementPanel == null)
			{
				return;
			}

			Tween tween = CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.TweenProperty(_settlementPanel, "modulate:a", 0.0f, 0.22f);
			tween.Finished += () =>
			{
				if (_settlementPanel != null)
				{
					_settlementPanel.Visible = false;
				}
			};
		};
	}

	private void ApplySettlementVisuals(PokerHandResult result)
	{
		var winningSet = new HashSet<int>(result.WinningCardIndices);

		for (int i = 0; i < _slotRoots.Count; i++)
		{
			Control slot = _slotRoots[i];
			if (winningSet.Contains(i))
			{
				slot.Modulate = new Color(1, 1, 1, 1);
				slot.Scale = new Vector2(0.9f, 0.9f);

				Tween highlightTween = CreateTween();
				highlightTween.SetEase(Tween.EaseType.Out);
				highlightTween.SetTrans(Tween.TransitionType.Back);
				highlightTween.TweenProperty(slot, "scale", Vector2.One, 0.16f);
			}
			else
			{
				Tween fadeTween = CreateTween();
				fadeTween.SetEase(Tween.EaseType.Out);
				fadeTween.SetTrans(Tween.TransitionType.Cubic);
				fadeTween.TweenProperty(slot, "modulate:a", 0.25f, 0.22f);
			}
		}
	}

	private void RefreshCardSlots(IReadOnlyList<CardData> cards, int maxSlots)
	{
		for (int i = 0; i < _slotLabels.Count; i++)
		{
			Control slot = _slotRoots[i];
			slot.Modulate = new Color(1, 1, 1, 1);
			slot.Scale = Vector2.One;

			if (i < cards.Count)
			{
				_slotLabels[i].Text = cards[i].DisplayText;
			}
			else
			{
				_slotLabels[i].Text = "--";
			}
		}

		SetDeckCount(cards.Count, maxSlots);
	}

	private void SetDeckCount(int current, int max)
	{
		if (_deckCount != null)
		{
			_deckCount.Text = current + "/" + max;
		}
	}
}
