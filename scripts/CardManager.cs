using Godot;
using System.Collections.Generic;

public partial class CardManager : Node
{
    [Signal]
    public delegate void HandSettledEventHandler(string handName, float damageMultiplier);

    [Export] public int MaxCollectedCards = 5;

    public int TotalCardsFromEnemies { get; private set; }
    public IReadOnlyList<CardData> CollectedCards => _collectedCards;
    public bool HasSettlementResult { get; private set; }
    public PokerHandResult LastSettlementResult { get; private set; }

    private readonly List<CardData> _collectedCards = new();
    private readonly RandomNumberGenerator _rng = new();

    private EnergyManager _energyManager;
    private HUD _hud;

    public override void _EnterTree()
    {
        AddToGroup("card_manager");
    }

    public override void _Ready()
    {
        _energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
        ResolveHud();
    }

    public void NotifyEnemyKilled(CardDrop drop, CardData? predefinedData = null)
    {
        TotalCardsFromEnemies += 1;
        _energyManager?.RestoreOnEnemyKill();

        if (drop == null)
        {
            return;
        }

        if (_collectedCards.Count >= MaxCollectedCards)
        {
            drop.QueueFree();
            return;
        }

        ResolveHud();
        int targetSlot = _collectedCards.Count;
        Vector2 targetGlobalPos = _hud != null ? _hud.GetSlotGlobalAnchor(targetSlot) : drop.GlobalPosition;
        CardData data = predefinedData ?? CreateRandomCardData();

        drop.BeginAutoCollect(this, data, targetSlot, targetGlobalPos);
    }

    public bool TryCollectCard(CardData data, int requestedSlot)
    {
        if (_collectedCards.Count >= MaxCollectedCards)
        {
            return false;
        }

        int slot = _collectedCards.Count;
        if (requestedSlot >= 0 && requestedSlot < MaxCollectedCards)
        {
            slot = requestedSlot;
        }

        _collectedCards.Add(data);
        ResolveHud();
        _hud?.OnCardCollected(slot, data, _collectedCards.Count, MaxCollectedCards);

        if (_collectedCards.Count >= MaxCollectedCards)
        {
            AutoSettleCurrentHand();
        }

        return true;
    }

    public float GetLastDamageMultiplier()
    {
        return HasSettlementResult ? LastSettlementResult.DamageMultiplier : 1.0f;
    }

    private void AutoSettleCurrentHand()
    {
        if (_collectedCards.Count != MaxCollectedCards)
        {
            return;
        }

        PokerHandResult result = PokerHandEvaluator.Evaluate(_collectedCards);
        LastSettlementResult = result;
        HasSettlementResult = true;

        GD.Print("[Settlement] " + result.HandName + " x" + result.DamageMultiplier.ToString("0.00"));
        EmitSignal(SignalName.HandSettled, result.HandName, result.DamageMultiplier);

        var keptCards = new List<CardData>();
        for (int i = 0; i < result.WinningCardIndices.Length; i++)
        {
            int index = result.WinningCardIndices[i];
            if (index >= 0 && index < _collectedCards.Count)
            {
                keptCards.Add(_collectedCards[index]);
            }
        }

        ResolveHud();
        _hud?.OnHandSettled(result, keptCards, MaxCollectedCards);

        _collectedCards.Clear();
        _collectedCards.AddRange(keptCards);
    }

    private CardData CreateRandomCardData()
    {
        CardSuit suit = (CardSuit)_rng.RandiRange(0, 3);
        CardRank rank = (CardRank)_rng.RandiRange(0, 12);
        return new CardData(suit, rank);
    }

    private void ResolveHud()
    {
        if (_hud == null)
        {
            _hud = GetTree().GetFirstNodeInGroup("hud_controller") as HUD;
        }
    }
}
