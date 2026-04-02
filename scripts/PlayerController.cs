using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Signal]
	public delegate void HpChangedEventHandler(float currentHp, float maxHp);

	[Export] public float MaxDragDistance = 220.0f;
	[Export] public float MinEffectiveDrag = 10.0f;
	[Export] public float ImpulsePerPixel = 9.0f;
	[Export] public float MaxSpeed = 1200.0f;
	[Export] public float SlideDamping = 520.0f;
	[Export] public float CollisionRetain = 0.72f;
	[Export] public float WallCollisionRetain = 0.95f;
	[Export] public float WallBounceMinSpeed = 80.0f;
	[Export] public float StopSpeedThreshold = 18.0f;
	[Export] public float ImpactScale = 0.03f;
	[Export] public float EnergyCostPerImpulse = 0.02f;
	[Export] public float MinEnergyCost = 2.0f;
	[Export] public float MaxHp = 100.0f;
	[Export] public float StartHp = 100.0f;
	[Export] public float DamageCooldownSec = 0.25f;
	[Export] public string DragAction = "player_drag";
	[Export] public bool EnableCameraControl = true;
	[Export] public bool EnableDynamicZoom = true;
	[Export] public bool ApplyDefaultZoomOnReady = true;
	[Export] public float ZoomAtHighSpeed = 0.6f;
	[Export] public float ZoomAtIdle = 0.8f;
	[Export] public float DefaultZoom = 0.7f;
	[Export] public float SpeedForHighZoom = 1200.0f;
	[Export] public float ZoomLerpSpeed = 6.0f;
	[Export] public float FollowSmoothingSpeed = 12.0f;
	[Export] public Vector2 MapHalfExtents = new Vector2(500.0f, 500.0f);
	[Export] public NodePath AnimatedSpritePath = "AnimatedSprite2D";
	[Export] public float RunAnimSpeedThreshold = 40.0f;
	[Export] public string IdleAnimationName = "idle";
	[Export] public string RunAnimationName = "run";
	[Export] public string AttackAnimationNameA = "attack1";
	[Export] public string AttackAnimationNameB = "attack2";

	public float LastImpactStrength { get; private set; }
	public string CurrentStateName => _state.ToString();
	public float CurrentHp { get; private set; }

	private enum MoveState
	{
		Idle,
		Dragging,
		Sliding
	}

	private MoveState _state = MoveState.Idle;
	private Line2D _dragGuide;
	private EnergyManager _energyManager;
	private Camera2D _camera;
	private GameManager _gameManager;
	private AnimatedSprite2D _animatedSprite;
	private Vector2 _dragStartGlobal;
	private Vector2 _cachedDragVector;
	private float _damageCooldownLeft;
	private bool _isAttackAnimating;
	private readonly RandomNumberGenerator _animRng = new();

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
		_gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>(AnimatedSpritePath);
		if (_animatedSprite != null)
		{
			_animatedSprite.AnimationFinished += OnAnimationFinished;
			PlayIdleAnimation();
		}

		ResetHp();
		SetupCamera();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (_damageCooldownLeft > 0.0f)
		{
			_damageCooldownLeft = Mathf.Max(0.0f, _damageCooldownLeft - dt);
		}

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

		UpdateMovementAnimation();

		UpdateCamera(dt);
	}

	public void ResetHp()
	{
		CurrentHp = Mathf.Clamp(StartHp, 0.0f, MaxHp);
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	public void TakeDamage(float amount)
	{
		if (amount <= 0.0f || CurrentHp <= 0.0f || _damageCooldownLeft > 0.0f)
		{
			return;
		}

		CurrentHp = Mathf.Clamp(CurrentHp - amount, 0.0f, MaxHp);
		_damageCooldownLeft = Mathf.Max(0.0f, DamageCooldownSec);
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);

		if (CurrentHp <= 0.0f)
		{
			if (_gameManager == null)
			{
				_gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
			}

			_gameManager?.TryTriggerLose("Player Down");
		}
	}

	private void SetupCamera()
	{
		_camera = GetNodeOrNull<Camera2D>("Camera2D");
		if (_camera == null || !EnableCameraControl)
		{
			return;
		}

		_camera.MakeCurrent();
		_camera.PositionSmoothingEnabled = true;
		_camera.PositionSmoothingSpeed = FollowSmoothingSpeed;
		_camera.LimitLeft = -Mathf.RoundToInt(MapHalfExtents.X);
		_camera.LimitTop = -Mathf.RoundToInt(MapHalfExtents.Y);
		_camera.LimitRight = Mathf.RoundToInt(MapHalfExtents.X);
		_camera.LimitBottom = Mathf.RoundToInt(MapHalfExtents.Y);

		if (ApplyDefaultZoomOnReady)
		{
			float z = Mathf.Clamp(DefaultZoom, ZoomAtHighSpeed, ZoomAtIdle);
			_camera.Zoom = new Vector2(z, z);
		}
	}

	private void UpdateCamera(float dt)
	{
		if (!EnableCameraControl || _camera == null)
		{
			return;
		}

		_camera.PositionSmoothingSpeed = FollowSmoothingSpeed;

		if (!EnableDynamicZoom)
		{
			return;
		}

		float t = Mathf.Clamp(Velocity.Length() / Mathf.Max(1.0f, SpeedForHighZoom), 0.0f, 1.0f);
		float targetZoom = Mathf.Lerp(ZoomAtIdle, ZoomAtHighSpeed, t);
		float currentZoom = _camera.Zoom.X;
		float nextZoom = Mathf.Lerp(currentZoom, targetZoom, dt * ZoomLerpSpeed);
		_camera.Zoom = new Vector2(nextZoom, nextZoom);
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
		Vector2 collisionVelocity = preMoveVelocity;

		for (int i = 0; i < collisionCount; i++)
		{
			KinematicCollision2D collision = GetSlideCollision(i);
			Node collider = collision.GetCollider() as Node;
			bool isWallCollider = IsWallCollider(collider);
			if (collider != null && collider.IsInGroup("enemy"))
			{
				PlayAttackAnimation();
			}

			Vector2 normal = collision.GetNormal();
			if (isWallCollider)
			{
				Vector2 bounceSource = collisionVelocity.LengthSquared() > 1.0f ? collisionVelocity : adjustedVelocity;
				adjustedVelocity = bounceSource.Bounce(normal) * Mathf.Clamp(WallCollisionRetain, 0.0f, 1.2f);

				// Guarantee an outward component so the body does not keep scraping along the wall.
				float outward = adjustedVelocity.Dot(normal);
				if (outward <= 0.0f)
				{
					adjustedVelocity += normal * Mathf.Max(40.0f, WallBounceMinSpeed * 0.5f);
				}

				if (adjustedVelocity.Length() < WallBounceMinSpeed)
				{
					if (adjustedVelocity.LengthSquared() > 0.001f)
					{
						adjustedVelocity = adjustedVelocity.Normalized() * WallBounceMinSpeed;
					}
					else
					{
						adjustedVelocity = normal * WallBounceMinSpeed;
					}
				}

				collisionVelocity = adjustedVelocity;
				continue;
			}

			Vector2 tangentComponent = adjustedVelocity.Slide(normal);
			if (tangentComponent.LengthSquared() > 1.0f)
			{
				adjustedVelocity = tangentComponent * CollisionRetain;
			}
			else
			{
				adjustedVelocity = adjustedVelocity.Bounce(normal) * CollisionRetain;
			}

			collisionVelocity = adjustedVelocity;
		}

		Velocity = adjustedVelocity.LimitLength(MaxSpeed);
	}

	private static bool IsWallCollider(Node collider)
	{
		if (collider == null)
		{
			return false;
		}

		if (collider is Walls || collider.IsInGroup("wall") || collider.Name == "Walls")
		{
			return true;
		}

		Node parent = collider.GetParent();
		return parent is Walls || (parent != null && (parent.IsInGroup("wall") || parent.Name == "Walls"));
	}

	private void UpdateMovementAnimation()
	{
		if (_animatedSprite == null || _isAttackAnimating)
		{
			return;
		}

		if (Velocity.Length() >= RunAnimSpeedThreshold)
		{
			if (_animatedSprite.Animation != RunAnimationName)
			{
				_animatedSprite.Play(RunAnimationName);
			}
		}
		else
		{
			if (_animatedSprite.Animation != IdleAnimationName)
			{
				_animatedSprite.Play(IdleAnimationName);
			}
		}
	}

	private void PlayIdleAnimation()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		if (_animatedSprite.Animation != IdleAnimationName)
		{
			_animatedSprite.Play(IdleAnimationName);
		}
	}

	private void PlayAttackAnimation()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		if (!_animatedSprite.SpriteFrames.HasAnimation(AttackAnimationNameA) && !_animatedSprite.SpriteFrames.HasAnimation(AttackAnimationNameB))
		{
			return;
		}

		if (_isAttackAnimating)
		{
			return;
		}

		bool hasAttackA = _animatedSprite.SpriteFrames.HasAnimation(AttackAnimationNameA);
		bool hasAttackB = _animatedSprite.SpriteFrames.HasAnimation(AttackAnimationNameB);
		string attackName;

		if (hasAttackA && hasAttackB)
		{
			attackName = _animRng.Randf() < 0.5f ? AttackAnimationNameA : AttackAnimationNameB;
		}
		else
		{
			attackName = hasAttackA ? AttackAnimationNameA : AttackAnimationNameB;
		}

		_isAttackAnimating = true;
		_animatedSprite.Play(attackName);
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		if (_animatedSprite.Animation == AttackAnimationNameA || _animatedSprite.Animation == AttackAnimationNameB)
		{
			_isAttackAnimating = false;
			UpdateMovementAnimation();
		}
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
