using Godot;

public partial class GameplayEventBus : Node
{
    [Signal]
    public delegate void EnemyKilledEventHandler(Node enemyNode, Vector2 enemyGlobalPosition, float killerSpeed);

    [Signal]
    public delegate void PlayerShootEventHandler(Vector2 impulse, float energyCost, Vector2 finalVelocity);

    [Signal]
    public delegate void CollisionEventHandler(Node collider, Vector2 normal, float impactSpeed, Vector2 preMoveVelocity);

    public override void _EnterTree()
    {
        AddToGroup("gameplay_event_bus");
    }

    public void PublishEnemyKilled(Node enemyNode, Vector2 enemyGlobalPosition, float killerSpeed)
    {
        EmitSignal(SignalName.EnemyKilled, enemyNode, enemyGlobalPosition, killerSpeed);
    }

    public void PublishPlayerShoot(Vector2 impulse, float energyCost, Vector2 finalVelocity)
    {
        EmitSignal(SignalName.PlayerShoot, impulse, energyCost, finalVelocity);
    }

    public void PublishCollision(Node collider, Vector2 normal, float impactSpeed, Vector2 preMoveVelocity)
    {
        EmitSignal(SignalName.Collision, collider, normal, impactSpeed, preMoveVelocity);
    }
}
