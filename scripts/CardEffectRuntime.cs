using Godot;
using System.Collections.Generic;

public partial class CardEffectRuntime : Node
{
    private readonly List<BuffCardData> _activeCards = new();

    private GameplayEventBus _eventBus;
    private PlayerController _player;
    private EnergyManager _energyManager;

    private float _baseSlideDamping;
    private bool _hasCapturedBaseSlideDamping;

    public override void _EnterTree()
    {
        AddToGroup("card_effect_runtime");
    }

    public override void _Ready()
    {
        ResolveRefs();
        ConnectEvents();
    }

    public override void _ExitTree()
    {
        DisconnectEvents();
    }

    public void SetActiveCards(IReadOnlyList<BuffCardData> cards)
    {
        _activeCards.Clear();
        if (cards != null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    _activeCards.Add(cards[i]);
                }
            }
        }

        ResolveRefs();
        ApplyPersistentModifiers();
    }

    public void ApplyRunStartBuffs()
    {
        ResolveRefs();
        ApplyPersistentModifiers();

        if (_energyManager == null)
        {
            return;
        }

        float batteryBonus = SumValueA(BuffEffectType.EnergyBattery);
        if (batteryBonus > 0.0f)
        {
            _energyManager.Restore(batteryBonus);
        }
    }

    private void ResolveRefs()
    {
        if (_eventBus == null || !IsInstanceValid(_eventBus))
        {
            _eventBus = GetTree().GetFirstNodeInGroup("gameplay_event_bus") as GameplayEventBus;
        }

        if (_player == null || !IsInstanceValid(_player))
        {
            _player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
            if (_player != null && !_hasCapturedBaseSlideDamping)
            {
                _baseSlideDamping = _player.SlideDamping;
                _hasCapturedBaseSlideDamping = true;
            }
        }

        if (_energyManager == null || !IsInstanceValid(_energyManager))
        {
            _energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
        }
    }

    private void ConnectEvents()
    {
        ResolveRefs();
        if (_eventBus == null)
        {
            return;
        }

        Callable onEnemyKilled = Callable.From<Node, Vector2, float>(OnEnemyKilled);
        if (!_eventBus.IsConnected(GameplayEventBus.SignalName.EnemyKilled, onEnemyKilled))
        {
            _eventBus.Connect(GameplayEventBus.SignalName.EnemyKilled, onEnemyKilled);
        }

        Callable onPlayerShoot = Callable.From<Vector2, float, Vector2>(OnPlayerShoot);
        if (!_eventBus.IsConnected(GameplayEventBus.SignalName.PlayerShoot, onPlayerShoot))
        {
            _eventBus.Connect(GameplayEventBus.SignalName.PlayerShoot, onPlayerShoot);
        }

        Callable onCollision = Callable.From<Node, Vector2, float, Vector2>(OnCollision);
        if (!_eventBus.IsConnected(GameplayEventBus.SignalName.Collision, onCollision))
        {
            _eventBus.Connect(GameplayEventBus.SignalName.Collision, onCollision);
        }
    }

    private void DisconnectEvents()
    {
        if (_eventBus == null || !IsInstanceValid(_eventBus))
        {
            return;
        }

        Callable onEnemyKilled = Callable.From<Node, Vector2, float>(OnEnemyKilled);
        if (_eventBus.IsConnected(GameplayEventBus.SignalName.EnemyKilled, onEnemyKilled))
        {
            _eventBus.Disconnect(GameplayEventBus.SignalName.EnemyKilled, onEnemyKilled);
        }

        Callable onPlayerShoot = Callable.From<Vector2, float, Vector2>(OnPlayerShoot);
        if (_eventBus.IsConnected(GameplayEventBus.SignalName.PlayerShoot, onPlayerShoot))
        {
            _eventBus.Disconnect(GameplayEventBus.SignalName.PlayerShoot, onPlayerShoot);
        }

        Callable onCollision = Callable.From<Node, Vector2, float, Vector2>(OnCollision);
        if (_eventBus.IsConnected(GameplayEventBus.SignalName.Collision, onCollision))
        {
            _eventBus.Disconnect(GameplayEventBus.SignalName.Collision, onCollision);
        }
    }

    private void OnPlayerShoot(Vector2 impulse, float energyCost, Vector2 finalVelocity)
    {
        ResolveRefs();
        if (_player == null)
        {
            return;
        }

        float overloadBonusRatio = SumValueA(BuffEffectType.OverloadPropulsion);
        if (overloadBonusRatio <= 0.0f)
        {
            return;
        }

        _player.AddExternalImpulse(impulse * overloadBonusRatio);
    }

    private void OnEnemyKilled(Node enemyNode, Vector2 enemyGlobalPosition, float killerSpeed)
    {
        ResolveRefs();

        if (_energyManager != null)
        {
            float recoveryPerKill = SumValueA(BuffEffectType.EnergyRecoveryOnKill);
            if (recoveryPerKill > 0.0f)
            {
                _energyManager.Restore(recoveryPerKill);
            }
        }

        float shockwaveRadius = SumValueA(BuffEffectType.ChainShockwaveOnKill);
        if (shockwaveRadius <= 0.0f)
        {
            return;
        }

        float propagatedSpeed = SumValueB(BuffEffectType.ChainShockwaveOnKill);
        float killSpeed = propagatedSpeed > 0.0f ? propagatedSpeed : killerSpeed;

        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemy");
        for (int i = 0; i < enemies.Count; i++)
        {
            Node node = enemies[i];
            if (node == null || !IsInstanceValid(node) || node == enemyNode)
            {
                continue;
            }

            if (node is not Node2D enemy2D)
            {
                continue;
            }

            if (enemy2D.GlobalPosition.DistanceTo(enemyGlobalPosition) > shockwaveRadius)
            {
                continue;
            }

            if (node is EnemyController normalEnemy)
            {
                normalEnemy.DefeatByShockwave(killSpeed);
            }
            else if (node is ChaserEnemy chaserEnemy)
            {
                chaserEnemy.DefeatByShockwave(killSpeed);
            }
        }
    }

    private void OnCollision(Node collider, Vector2 normal, float impactSpeed, Vector2 preMoveVelocity)
    {
        // Reserved event hook for future collision-driven card effects.
    }

    private void ApplyPersistentModifiers()
    {
        if (_player == null || !_hasCapturedBaseSlideDamping)
        {
            return;
        }

        float dampingMultiplier = 1.0f;

        for (int i = 0; i < _activeCards.Count; i++)
        {
            BuffCardData card = _activeCards[i];
            if (card == null)
            {
                continue;
            }

            if (card.EffectType == BuffEffectType.LowFrictionSurface)
            {
                dampingMultiplier *= Mathf.Max(0.01f, card.ValueA);
            }
        }

        _player.SlideDamping = _baseSlideDamping * dampingMultiplier;
    }

    private float SumValueA(BuffEffectType effectType)
    {
        float sum = 0.0f;
        for (int i = 0; i < _activeCards.Count; i++)
        {
            BuffCardData card = _activeCards[i];
            if (card == null || card.EffectType != effectType)
            {
                continue;
            }

            sum += card.ValueA;
        }

        return sum;
    }

    private float SumValueB(BuffEffectType effectType)
    {
        float sum = 0.0f;
        for (int i = 0; i < _activeCards.Count; i++)
        {
            BuffCardData card = _activeCards[i];
            if (card == null || card.EffectType != effectType)
            {
                continue;
            }

            sum += card.ValueB;
        }

        return sum;
    }
}
