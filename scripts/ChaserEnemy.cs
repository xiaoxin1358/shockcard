// Chaser chase failure notes (postmortem):
// 1) PlayerPath can be null if not assigned in Inspector; calling IsEmpty on null caused NullReferenceException.
// 2) Player node path changed from Runtime/PlayerInstance to Runtime/Player; stale paths made lookup fail.
// 3) Once exception happened in _PhysicsProcess, chase force stopped applying, so the chaser looked "not moving".
// Current fix: null-safe PlayerPath check + default path + fallback lookup by scene path/group.
using Godot;

public partial class ChaserEnemy : RigidBody2D
{
	[Export] public NodePath PlayerPath = "Runtime/Player";
	[Export] public float ChaseForce = 900.0f;
	[Export] public float MaxChaseSpeed = 320.0f;
	[Export] public float DestroyByPlayerSpeed = 760.0f;
	[Export] public float LowSpeedEnergyPenalty = 20.0f;
	[Export] public bool FaceVelocityDirection = true;
	[Export] public int BoxSize = 40;
	[Export] public Color BodyColor = new Color(0.95f, 0.28f, 0.2f, 1.0f);
	[Export] public Color OutlineColor = new Color(1.0f, 0.85f, 0.85f, 1.0f);

	private Node2D _player;
	private EnergyManager _energyManager;

	public override void _EnterTree()
	{
		AddToGroup("enemy");
	}

	public override void _Ready()
	{
		GravityScale = 0.0f;
		CanSleep = false;
		Sleeping = false;
		ContactMonitor = true;
		MaxContactsReported = Mathf.Max(MaxContactsReported, 8);
		BodyEntered += OnBodyEntered;

		_energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
		ResolvePlayerRef();
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		Sleeping = false;
		ResolvePlayerRef();
		if (_player == null)
		{
			return;
		}

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		if (toPlayer.LengthSquared() > 1.0f)
		{
			Vector2 forceDir = toPlayer.Normalized();
			ApplyCentralForce(forceDir * Mathf.Max(1.0f, ChaseForce));
			LinearVelocity = LinearVelocity.LimitLength(Mathf.Max(1.0f, MaxChaseSpeed));
			if (FaceVelocityDirection && LinearVelocity.LengthSquared() > 1.0f)
			{
				Rotation = LinearVelocity.Angle();
			}
		}
	}

	public override void _Draw()
	{
		float half = BoxSize * 0.5f;
		Rect2 rect = new Rect2(-half, -half, BoxSize, BoxSize);
		DrawRect(rect, BodyColor);
		DrawRect(rect, OutlineColor, false, 2.0f);
	}

	private void ResolvePlayerRef()
	{
		if (_player != null && IsInstanceValid(_player))
		{
			return;
		}

		if (PlayerPath != null && !PlayerPath.IsEmpty)
		{
			_player = GetNodeOrNull<Node2D>(PlayerPath);
			if (_player != null)
			{
				return;
			}
		}

		Node sceneRoot = GetTree().CurrentScene;
		_player = sceneRoot?.GetNodeOrNull<Node2D>("Runtime/Player");
		if (_player != null)
		{
			return;
		}

		_player = GetTree().GetFirstNodeInGroup("player") as Node2D;
	}

	private void OnBodyEntered(Node body)
	{
		if (body is not Node2D playerNode || !body.IsInGroup("player"))
		{
			return;
		}

		float playerSpeed = GetPlayerVelocity(playerNode).Length();
		if (playerSpeed >= DestroyByPlayerSpeed)
		{
			QueueFree();
			return;
		}

		// Low-speed contact now consumes energy instead of HP.
		if (_energyManager != null)
		{
			_energyManager.TryConsume(Mathf.Max(0.0f, LowSpeedEnergyPenalty));
		}
	}

	private Vector2 GetPlayerVelocity(Node2D playerNode)
	{
		if (playerNode is CharacterBody2D characterBody)
		{
			return characterBody.Velocity;
		}

		if (playerNode is RigidBody2D rigidBody)
		{
			return rigidBody.LinearVelocity;
		}

		return Vector2.Zero;
	}
}
