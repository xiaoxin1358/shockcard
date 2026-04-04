using Godot;
using System.Collections.Generic;

public partial class RoundSettlementController : Node
{
	[Export] public BuffCardCatalog BuffCatalog;
	[Export] public int ChoiceCardCount = 3;
	[Export] public bool AutoRestartWhenNoChoices = true;

	private readonly List<BuffCardData> _allCards = new();
	private readonly List<BuffCardData> _activeCards = new();
	private readonly List<BuffCardData> _currentChoices = new();
	private readonly HashSet<string> _ownedCardIds = new();
	private readonly RandomNumberGenerator _rng = new();

	private HUD _hud;
	private GameManager _gameManager;
	private GameplayEventBus _eventBus;
	private CardEffectRuntime _effectRuntime;
	private bool _isChoosingCard;

	private static readonly string[] DefaultCardPaths =
	{
		"res://data/cards/overload_propulsion.tres",
		"res://data/cards/energy_battery.tres",
		"res://data/cards/energy_recovery.tres",
		"res://data/cards/chain_shockwave.tres",
		"res://data/cards/low_friction_surface.tres"
	};

	private const string DefaultCatalogPath = "res://data/cards/buff_catalog.tres";

	public override void _EnterTree()
	{
		AddToGroup("round_settlement");
	}

	public override void _Ready()
	{
		_rng.Randomize();
		ResolveRefs();
		EnsureEventBus();
		EnsureEffectRuntime();
		LoadCardPool();
		ConnectHudSignals();

		_effectRuntime?.SetActiveCards(_activeCards);
	}

	public override void _ExitTree()
	{
		DisconnectHudSignals();
	}

	private void ResolveRefs()
	{
		if (_hud == null || !IsInstanceValid(_hud))
		{
			_hud = GetTree().GetFirstNodeInGroup("hud_controller") as HUD;
		}

		if (_gameManager == null || !IsInstanceValid(_gameManager))
		{
			_gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		}
	}

	private void EnsureEventBus()
	{
		_eventBus = GetTree().GetFirstNodeInGroup("gameplay_event_bus") as GameplayEventBus;
		if (_eventBus != null)
		{
			return;
		}

		_eventBus = new GameplayEventBus
		{
			Name = "GameplayEventBus"
		};

		AddChild(_eventBus);
	}

	private void EnsureEffectRuntime()
	{
		_effectRuntime = GetNodeOrNull<CardEffectRuntime>("CardEffectRuntime");
		if (_effectRuntime != null)
		{
			return;
		}

		_effectRuntime = new CardEffectRuntime
		{
			Name = "CardEffectRuntime"
		};

		AddChild(_effectRuntime);
	}

	private void ConnectHudSignals()
	{
		ResolveRefs();
		if (_hud == null)
		{
			return;
		}

		Callable onContinueRequested = Callable.From(OnContinueRequested);
		if (!_hud.IsConnected(HUD.SignalName.ContinueRequested, onContinueRequested))
		{
			_hud.Connect(HUD.SignalName.ContinueRequested, onContinueRequested);
		}

		Callable onBuffChoiceSelected = Callable.From<int>(OnBuffChoiceSelected);
		if (!_hud.IsConnected(HUD.SignalName.BuffChoiceSelected, onBuffChoiceSelected))
		{
			_hud.Connect(HUD.SignalName.BuffChoiceSelected, onBuffChoiceSelected);
		}
	}

	private void DisconnectHudSignals()
	{
		if (_hud == null || !IsInstanceValid(_hud))
		{
			return;
		}

		Callable onContinueRequested = Callable.From(OnContinueRequested);
		if (_hud.IsConnected(HUD.SignalName.ContinueRequested, onContinueRequested))
		{
			_hud.Disconnect(HUD.SignalName.ContinueRequested, onContinueRequested);
		}

		Callable onBuffChoiceSelected = Callable.From<int>(OnBuffChoiceSelected);
		if (_hud.IsConnected(HUD.SignalName.BuffChoiceSelected, onBuffChoiceSelected))
		{
			_hud.Disconnect(HUD.SignalName.BuffChoiceSelected, onBuffChoiceSelected);
		}
	}

	private void LoadCardPool()
	{
		_allCards.Clear();

		if (BuffCatalog == null)
		{
			BuffCatalog = ResourceLoader.Load<BuffCardCatalog>(DefaultCatalogPath);
		}

		if (BuffCatalog != null)
		{
			for (int i = 0; i < BuffCatalog.Cards.Count; i++)
			{
				TryAddCard(BuffCatalog.Cards[i]);
			}
		}

		if (_allCards.Count > 0)
		{
			return;
		}

		for (int i = 0; i < DefaultCardPaths.Length; i++)
		{
			BuffCardData card = ResourceLoader.Load<BuffCardData>(DefaultCardPaths[i]);
			TryAddCard(card);
		}
	}

	private void TryAddCard(BuffCardData card)
	{
		if (card == null || string.IsNullOrWhiteSpace(card.CardId))
		{
			return;
		}

		for (int i = 0; i < _allCards.Count; i++)
		{
			if (_allCards[i].CardId == card.CardId)
			{
				return;
			}
		}

		_allCards.Add(card);
	}

	private void OnContinueRequested()
	{
		ResolveRefs();
		if (_gameManager == null || _hud == null || _isChoosingCard)
		{
			return;
		}

		if (_gameManager.CurrentState == GameResultState.Running)
		{
			_gameManager.RequestRestart();
			_effectRuntime?.ApplyRunStartBuffs();
			return;
		}

		OpenChoiceFlow();
	}

	private void OpenChoiceFlow()
	{
		_currentChoices.Clear();
		BuildChoices(_currentChoices, Mathf.Max(1, ChoiceCardCount));

		if (_currentChoices.Count <= 0)
		{
			if (AutoRestartWhenNoChoices)
			{
				_gameManager?.RequestRestart();
				_effectRuntime?.ApplyRunStartBuffs();
			}
			return;
		}

		_isChoosingCard = true;
		_hud?.ShowBuffChoices(_currentChoices);
	}

	private void BuildChoices(List<BuffCardData> outChoices, int count)
	{
		var unownedPool = new List<BuffCardData>();

		for (int i = 0; i < _allCards.Count; i++)
		{
			BuffCardData card = _allCards[i];
			if (card == null || string.IsNullOrWhiteSpace(card.CardId))
			{
				continue;
			}

			if (_ownedCardIds.Contains(card.CardId))
			{
				continue;
			}

			unownedPool.Add(card);
		}

		Shuffle(unownedPool);

		int takeCount = Mathf.Min(count, unownedPool.Count);
		for (int i = 0; i < takeCount; i++)
		{
			outChoices.Add(unownedPool[i]);
		}
	}

	private void Shuffle(List<BuffCardData> cards)
	{
		for (int i = cards.Count - 1; i > 0; i--)
		{
			int j = _rng.RandiRange(0, i);
			(cards[i], cards[j]) = (cards[j], cards[i]);
		}
	}

	private void OnBuffChoiceSelected(int index)
	{
		if (!_isChoosingCard)
		{
			return;
		}

		if (index < 0 || index >= _currentChoices.Count)
		{
			return;
		}

		BuffCardData chosenCard = _currentChoices[index];
		_isChoosingCard = false;
		_currentChoices.Clear();
		_hud?.HideBuffChoices();

		if (chosenCard != null && !string.IsNullOrWhiteSpace(chosenCard.CardId) && _ownedCardIds.Add(chosenCard.CardId))
		{
			_activeCards.Add(chosenCard);
			_effectRuntime?.SetActiveCards(_activeCards);
		}

		_gameManager?.RequestRestart();
		_effectRuntime?.ApplyRunStartBuffs();
	}
}
