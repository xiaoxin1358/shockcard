using Godot;
using System.Collections.Generic;

public partial class CardDisplayUI : VBoxContainer
{
	[Export] public NodePath SettlementPanelPath = "../../SettlementPanel";
	[Export] public NodePath SettlementTextPath = "../../SettlementPanel/SettlementText";
	[Export] public Texture2D SpadeAtlas;
	[Export] public Texture2D HeartAtlas;
	[Export] public Texture2D ClubAtlas;
	[Export] public Texture2D DiamondAtlas;
	[Export] public int AtlasColumns = 5;
	[Export] public int AtlasRows = 3;
	[Export] public int LastRowCardCount = 3;

	private Label _deckCount;
	private Control _settlementPanel;
	private Label _settlementText;
	private FeedbackManager _feedback;
	private readonly List<Control> _slotRoots = new();
	private readonly List<Label> _slotLabels = new();
	private readonly List<TextureRect> _slotTextures = new();
	private readonly Dictionary<int, AtlasTexture> _cardTextureCache = new();

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
		if (_slotRoots.Count == 0)
		{
			SetDeckCount(currentCount, maxCount);
			return;
		}

		int safeIndex = Mathf.Clamp(slotIndex, 0, _slotRoots.Count - 1);
		SetSlotCardVisual(safeIndex, data);

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

	public void ResetDisplay(int maxSlots)
	{
		if (_settlementPanel != null)
		{
			_settlementPanel.Visible = false;
			_settlementPanel.Modulate = new Color(1, 1, 1, 1);
		}

		RefreshCardSlots(System.Array.Empty<CardData>(), maxSlots);
	}

	private void CacheCardSlots()
	{
		_slotRoots.Clear();
		_slotLabels.Clear();
		_slotTextures.Clear();

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
			if (label != null)
			{
				label.Text = "--";
				label.Visible = false;
			}

			TextureRect cardTexture = slotRoot.GetNodeOrNull<TextureRect>("CardTexture");
			if (cardTexture == null)
			{
				cardTexture = new TextureRect();
				cardTexture.Name = "CardTexture";
				cardTexture.MouseFilter = MouseFilterEnum.Ignore;
				cardTexture.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
				cardTexture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
				cardTexture.SetAnchorsPreset(Control.LayoutPreset.FullRect);
				cardTexture.OffsetLeft = 2.0f;
				cardTexture.OffsetTop = 2.0f;
				cardTexture.OffsetRight = -2.0f;
				cardTexture.OffsetBottom = -2.0f;
				slotRoot.AddChild(cardTexture);
			}

			_slotRoots.Add(slotRoot);
			_slotLabels.Add(label);
			_slotTextures.Add(cardTexture);
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
		for (int i = 0; i < _slotRoots.Count; i++)
		{
			Control slot = _slotRoots[i];
			slot.Modulate = new Color(1, 1, 1, 1);
			slot.Scale = Vector2.One;

			if (i < cards.Count)
			{
				SetSlotCardVisual(i, cards[i]);
			}
			else
			{
				ClearSlotCardVisual(i);
			}
		}

		SetDeckCount(cards.Count, maxSlots);
	}

	private void SetSlotCardVisual(int slotIndex, CardData data)
	{
		if (slotIndex < 0 || slotIndex >= _slotRoots.Count)
		{
			return;
		}

		if (slotIndex < _slotLabels.Count && _slotLabels[slotIndex] != null)
		{
			_slotLabels[slotIndex].Text = data.DisplayText;
			_slotLabels[slotIndex].Visible = false;
		}

		if (slotIndex >= _slotTextures.Count || _slotTextures[slotIndex] == null)
		{
			return;
		}

		_slotTextures[slotIndex].Texture = GetCardTexture(data.Suit, data.Rank);
	}

	private void ClearSlotCardVisual(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= _slotRoots.Count)
		{
			return;
		}

		if (slotIndex < _slotLabels.Count && _slotLabels[slotIndex] != null)
		{
			_slotLabels[slotIndex].Text = "--";
			_slotLabels[slotIndex].Visible = false;
		}

		if (slotIndex < _slotTextures.Count && _slotTextures[slotIndex] != null)
		{
			_slotTextures[slotIndex].Texture = null;
		}
	}

	private AtlasTexture GetCardTexture(CardSuit suit, CardRank rank)
	{
		int rankIndex = (int)rank;
		if (rankIndex < 0)
		{
			return null;
		}

		int cacheKey = (((int)suit) << 8) | rankIndex;
		if (_cardTextureCache.TryGetValue(cacheKey, out AtlasTexture cached))
		{
			return cached;
		}

		Texture2D atlasTexture = GetSuitAtlas(suit);
		if (atlasTexture == null)
		{
			return null;
		}

		int columns = Mathf.Max(1, AtlasColumns);
		int rows = Mathf.Max(1, AtlasRows);
		int row = rankIndex / columns;
		int col = rankIndex % columns;

		if (row >= rows)
		{
			return null;
		}

		if (row == rows - 1 && LastRowCardCount > 0 && col >= LastRowCardCount)
		{
			return null;
		}

		float cardWidth = (float)atlasTexture.GetWidth() / columns;
		float cardHeight = (float)atlasTexture.GetHeight() / rows;
		if (cardWidth <= 0.0f || cardHeight <= 0.0f)
		{
			return null;
		}

		var atlas = new AtlasTexture();
		atlas.Atlas = atlasTexture;
		atlas.Region = new Rect2(col * cardWidth, row * cardHeight, cardWidth, cardHeight);
		_cardTextureCache[cacheKey] = atlas;
		return atlas;
	}

	private Texture2D GetSuitAtlas(CardSuit suit)
	{
		return suit switch
		{
			CardSuit.Spade => SpadeAtlas,
			CardSuit.Heart => HeartAtlas,
			CardSuit.Club => ClubAtlas,
			CardSuit.Diamond => DiamondAtlas,
			_ => null
		};
	}

	private void SetDeckCount(int current, int max)
	{
		if (_deckCount != null)
		{
			_deckCount.Text = current + "/" + max;
		}
	}
}
