using Godot;

public partial class ObstacleController : Node2D
{
	public enum ObstacleKind
	{
		Wood = 0,
		Stone = 1
	}

	[Signal]
	public delegate void ObstacleBrokenEventHandler(Node2D obstacleNode, float impactSpeed, Node2D source);

	[Export] public ObstacleKind ActiveKind = ObstacleKind.Wood;

	private WoodBoard _woodBoard;
	private StoneBoard _stoneBoard;

	public override void _EnterTree()
	{
		AddToGroup("obstacle");
	}

	public override void _Ready()
	{
		_woodBoard = GetNodeOrNull<WoodBoard>("Wood");
		_stoneBoard = GetNodeOrNull<StoneBoard>("Stone");

		if (_woodBoard != null)
		{
			_woodBoard.Broken += OnChildObstacleBroken;
		}

		if (_stoneBoard != null)
		{
			_stoneBoard.Broken += OnChildObstacleBroken;
		}

		ApplyActiveKind();
	}

	private void ApplyActiveKind()
	{
		bool useWood = ActiveKind == ObstacleKind.Wood;
		SetObstacleEnabled(_woodBoard, useWood);
		SetObstacleEnabled(_stoneBoard, !useWood);
	}

	private void SetObstacleEnabled(StaticBody2D node, bool enabled)
	{
		if (node == null)
		{
			return;
		}

		node.Visible = enabled;
		node.SetProcess(enabled);
		node.SetPhysicsProcess(enabled);

		CollisionShape2D collision = node.GetNodeOrNull<CollisionShape2D>("Collision");
		if (collision != null)
		{
			collision.Disabled = !enabled;
		}

		Area2D hitArea = node.GetNodeOrNull<Area2D>("HitArea");
		if (hitArea != null)
		{
			hitArea.Monitoring = enabled;
		}
	}

	private void OnChildObstacleBroken(ObstacleBase obstacle, float impactSpeed, Node2D source)
	{
		EmitSignal(SignalName.ObstacleBroken, obstacle, impactSpeed, source);
		QueueFree();
	}
}
