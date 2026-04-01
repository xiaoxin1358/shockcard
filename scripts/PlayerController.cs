using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export] public float MaxDragDistance = 220.0f;
	[Export] public float MinEffectiveDrag = 10.0f;
	[Export] public float ImpulsePerPixel = 9.0f;
	[Export] public float MaxSpeed = 1200.0f;
	[Export] public float SlideDamping = 520.0f;
	[Export] public float CollisionRetain = 0.72f;
	[Export] public float StopSpeedThreshold = 18.0f;
	[Export] public float ImpactScale = 0.03f;
	[Export] public float EnergyCostPerImpulse = 0.02f;
	[Export] public float MinEnergyCost = 2.0f;
	[Export] public string DragAction = "player_drag";

	public float LastImpactStrength { get; private set; }

	private enum MoveState
	{
		Idle,
		Dragging,
		Sliding
	}

	private MoveState _state = MoveState.Idle;
	private Line2D _dragGuide;
	private EnergyManager _energyManager;
	private Vector2 _dragStartGlobal;
	private Vector2 _cachedDragVector;

	public override void _Ready()
	{
		AddToGroup("player");

		_dragGuide = GetNodeOrNull<Line2D>("DragGuide");
		if (_dragGuide != null)
		{
			_dragGuide.Visible = false;
			if (_dragGuide.Points.Length < 2)
			{
				_dragGuide.ClearPoints();
				_dragGuide.AddPoint(Vector2.Zero);
				_dragGuide.AddPoint(Vector2.Zero);
			}
		}

		_energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		bool dragPressed = IsDragPressed();
		bool dragJustPressed = IsDragJustPressed();
		bool dragJustReleased = IsDragJustReleased();

		if (dragJustPressed)
		{
			_state = MoveState.Dragging;
			_dragStartGlobal = GetGlobalMousePosition();
			_cachedDragVector = Vector2.Zero;
			SetGuideVisible(true);
		}

		if (_state == MoveState.Dragging && dragPressed)
		{
			Vector2 rawDrag = _dragStartGlobal - GetGlobalMousePosition();
			_cachedDragVector = rawDrag.LimitLength(MaxDragDistance);
			UpdateGuide(_cachedDragVector);
		}

		if (_state == MoveState.Dragging && dragJustReleased)
		{
			ApplyDragImpulse(_cachedDragVector);
			_cachedDragVector = Vector2.Zero;
			SetGuideVisible(false);
		}

		Vector2 preMoveVelocity = Velocity;

		if (_state != MoveState.Dragging)
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, SlideDamping * dt);
		}

		MoveAndSlide();
		ResolveSlideCollisions(preMoveVelocity);

		if (Velocity.Length() < StopSpeedThreshold && _state != MoveState.Dragging)
		{
			Velocity = Vector2.Zero;
			if (_state == MoveState.Sliding)
			{
				_state = MoveState.Idle;
			}
		}
	}

	private void ApplyDragImpulse(Vector2 dragVector)
	{
		if (dragVector.Length() < MinEffectiveDrag)
		{
			if (Velocity.Length() > StopSpeedThreshold)
			{
				_state = MoveState.Sliding;
			}
			else
			{
				_state = MoveState.Idle;
			}
			return;
		}

		Vector2 impulse = dragVector * ImpulsePerPixel;
		float energyCost = Mathf.Max(MinEnergyCost, impulse.Length() * EnergyCostPerImpulse);

		if (_energyManager != null && !_energyManager.TryConsume(energyCost))
		{
			if (Velocity.Length() > StopSpeedThreshold)
			{
				_state = MoveState.Sliding;
			}
			else
			{
				_state = MoveState.Idle;
			}
			return;
		}

		Velocity += impulse;
		Velocity = Velocity.LimitLength(MaxSpeed);
		_state = MoveState.Sliding;
	}

	private void ResolveSlideCollisions(Vector2 preMoveVelocity)
	{
		LastImpactStrength = 0.0f;

		int collisionCount = GetSlideCollisionCount();
		if (collisionCount <= 0)
		{
			return;
		}

		float impact = preMoveVelocity.Length() * ImpactScale;
		if (impact > LastImpactStrength)
		{
			LastImpactStrength = impact;
		}

		Vector2 adjustedVelocity = Velocity;

		for (int i = 0; i < collisionCount; i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			Vector2 normal = collision.GetNormal();

			Vector2 tangentComponent = adjustedVelocity.Slide(normal);
			if (tangentComponent.LengthSquared() > 1.0f)
			{
				adjustedVelocity = tangentComponent * CollisionRetain;
			}
			else
			{
				adjustedVelocity = adjustedVelocity.Bounce(normal) * CollisionRetain;
			}
		}

		Velocity = adjustedVelocity.LimitLength(MaxSpeed);
	}

	private void UpdateGuide(Vector2 dragVector)
	{
		if (_dragGuide == null)
		{
			return;
		}

		Vector2 localEnd = ToLocal(GlobalPosition + dragVector);
		_dragGuide.SetPointPosition(0, Vector2.Zero);
		_dragGuide.SetPointPosition(1, localEnd);
	}

	private void SetGuideVisible(bool visible)
	{
		if (_dragGuide != null)
		{
			_dragGuide.Visible = visible;
		}
	}

	private bool IsDragPressed()
	{
		if (InputMap.HasAction(DragAction))
		{
			return Input.IsActionPressed(DragAction);
		}

		return Input.IsMouseButtonPressed(MouseButton.Left);
	}

	private bool IsDragJustPressed()
	{
		if (InputMap.HasAction(DragAction))
		{
			return Input.IsActionJustPressed(DragAction);
		}

		return Input.IsMouseButtonPressed(MouseButton.Left) && _state != MoveState.Dragging;
	}

	private bool IsDragJustReleased()
	{
		if (InputMap.HasAction(DragAction))
		{
			return Input.IsActionJustReleased(DragAction);
		}

		return !Input.IsMouseButtonPressed(MouseButton.Left) && _state == MoveState.Dragging;
	}
}
