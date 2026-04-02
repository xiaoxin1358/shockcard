// Chaser chase failure notes (postmortem):
// 1) PlayerPath can be null if not assigned in Inspector; calling IsEmpty on null caused NullReferenceException.
// 2) Player node path changed from Runtime/PlayerInstance to Runtime/Player; stale paths made lookup fail.
// 3) Once exception happened in _PhysicsProcess, chase force stopped applying, so the chaser looked "not moving".
// Current fix: null-safe PlayerPath check + default path + fallback lookup by scene path/group.
using Godot;
using System.Collections.Generic;

public partial class ChaserEnemy : RigidBody2D
{
	[Export] public NodePath PlayerPath = "Runtime/Player";
	[Export] public NodePath AnimatedSpritePath = "AnimatedSprite2D";
	[Export] public NodePath VariantArcherPath = "AnimatedSprite2D_Archer";
	[Export] public NodePath VariantLancerPath = "AnimatedSprite2D_Lancer";
	[Export] public NodePath VariantPawnPath = "AnimatedSprite2D_Pawn";
	[Export] public float ChaseForce = 900.0f;
	[Export] public float MaxChaseSpeed = 320.0f;
	[Export] public float DestroyByPlayerSpeed = 760.0f;
	[Export] public float LowSpeedEnergyPenalty = 20.0f;
	[Export] public float RunAnimationSpeedThreshold = 8.0f;
	[Export] public bool FaceVelocityDirection = true;
	[Export] public bool DrawDebugBody = false;
	[Export] public string IdleAnimationName = "idle";
	[Export] public string RunAnimationName = "run";
	[Export] public string AttackAnimationName = "attack";
	[Export] public string ExplosionTexturePath = "res://Tiny Swords/Tiny Swords (Free Pack)/Particle FX/Explosion_01.png";
	[Export] public string ExplosionAnimationName = "explode";
	[Export] public int ExplosionFrameWidth = 192;
	[Export] public int ExplosionFrameHeight = 192;
	[Export] public int ExplosionFrameCount = 8;
	[Export] public float ExplosionAnimationSpeed = 16.0f;
	[Export] public Vector2 ExplosionScale = new Vector2(0.8f, 0.8f);
	[Export] public string DeathSfxPath = "res://sorces/被砍.mp3";
	[Export] public float DeathSfxVolumeDb = -4.0f;
	[Export] public int BoxSize = 40;
	[Export] public Color BodyColor = new Color(0.95f, 0.28f, 0.2f, 1.0f);
	[Export] public Color OutlineColor = new Color(1.0f, 0.85f, 0.85f, 1.0f);

	private Node2D _player;
	private EnergyManager _energyManager;
	private AnimatedSprite2D _animatedSprite;
	private SpriteFrames _pendingVariantFrames;
	private int _pendingVariantIndex = -1;
	private readonly List<SpriteFrames> _variantFrames = new();
	private bool _isAttackAnimating;
	private string _activeAttackAnimationName = string.Empty;
	private SpriteFrames _explosionFrames;
	private bool _isDying;

	public override void _EnterTree()
	{
		AddToGroup("enemy");
	}

	public override void _Ready()
	{
		GravityScale = 0.0f;
		LockRotation = true;
		Rotation = 0.0f;
		CanSleep = false;
		Sleeping = false;
		ContactMonitor = true;
		MaxContactsReported = Mathf.Max(MaxContactsReported, 8);
		BodyEntered += OnBodyEntered;

		_energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>(AnimatedSpritePath);
		_explosionFrames = BuildExplosionFrames();
		CacheVariantFrames();
		if (_animatedSprite != null)
		{
			_animatedSprite.AnimationFinished += OnAnimationFinished;
			if (_pendingVariantFrames != null)
			{
				ApplyPendingVariantFrames();
			}
			else if (_pendingVariantIndex >= 0)
			{
				ApplyVariantByIndex(_pendingVariantIndex);
			}

			PlayMovementAnimation();
		}

		ResolvePlayerRef();
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDying)
		{
			return;
		}

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
		}

		if (FaceVelocityDirection)
		{
			UpdateFacingByVelocity();
		}

		PlayMovementAnimation();
	}

	public override void _Draw()
	{
		if (!DrawDebugBody)
		{
			return;
		}

		float half = BoxSize * 0.5f;
		Rect2 rect = new Rect2(-half, -half, BoxSize, BoxSize);
		DrawRect(rect, BodyColor);
		DrawRect(rect, OutlineColor, false, 2.0f);
	}

	public void SetVariantFrames(SpriteFrames frames)
	{
		_pendingVariantIndex = -1;
		_pendingVariantFrames = frames;
		ApplyPendingVariantFrames();
	}

	public void SetVariantByIndex(int index)
	{
		_pendingVariantFrames = null;
		_pendingVariantIndex = index;
		ApplyVariantByIndex(index);
	}

	public void ApplyRandomVariant()
	{
		if (_variantFrames.Count == 0)
		{
			return;
		}

		int index = (int)(GD.Randi() % (uint)_variantFrames.Count);
		SetVariantByIndex(index);
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
		if (_isDying)
		{
			return;
		}

		if (body is not Node2D playerNode || !body.IsInGroup("player"))
		{
			return;
		}

		float playerSpeed = GetPlayerVelocity(playerNode).Length();
		if (playerSpeed >= DestroyByPlayerSpeed)
		{
			StartDeathSequence();
			return;
		}

		// Low-speed contact now consumes energy instead of HP.
		if (_energyManager != null)
		{
			_energyManager.TryConsume(Mathf.Max(0.0f, LowSpeedEnergyPenalty));
		}

		PlayAttackAnimation();
	}

	private void ApplyPendingVariantFrames()
	{
		if (_animatedSprite == null || _pendingVariantFrames == null)
		{
			return;
		}

		_animatedSprite.SpriteFrames = _pendingVariantFrames;
		_activeAttackAnimationName = string.Empty;
	}

	private void ApplyVariantByIndex(int index)
	{
		if (_variantFrames.Count == 0)
		{
			return;
		}

		int safeIndex = Mathf.Clamp(index, 0, _variantFrames.Count - 1);
		SetVariantFrames(_variantFrames[safeIndex]);
	}

	private void CacheVariantFrames()
	{
		_variantFrames.Clear();
		TryAddVariantFrames(VariantArcherPath);
		TryAddVariantFrames(VariantLancerPath);
		TryAddVariantFrames(VariantPawnPath);
	}

	private void TryAddVariantFrames(NodePath path)
	{
		AnimatedSprite2D sprite = GetNodeOrNull<AnimatedSprite2D>(path);
		if (sprite?.SpriteFrames == null)
		{
			return;
		}

		_variantFrames.Add(sprite.SpriteFrames);
	}

	private void PlayMovementAnimation()
	{
		if (_animatedSprite == null || _isAttackAnimating)
		{
			return;
		}

		if (_animatedSprite.SpriteFrames == null)
		{
			return;
		}

		bool isMoving = LinearVelocity.Length() >= Mathf.Max(0.0f, RunAnimationSpeedThreshold);
		if (isMoving && _animatedSprite.SpriteFrames.HasAnimation(RunAnimationName))
		{
			if (_animatedSprite.Animation != RunAnimationName || !_animatedSprite.IsPlaying())
			{
				_animatedSprite.Play(RunAnimationName);
			}
			return;
		}

		if (_animatedSprite.SpriteFrames.HasAnimation(IdleAnimationName))
		{
			if (_animatedSprite.Animation != IdleAnimationName || !_animatedSprite.IsPlaying())
			{
				_animatedSprite.Play(IdleAnimationName);
			}
		}
	}

	private void PlayAttackAnimation()
	{
		if (_animatedSprite == null || _isAttackAnimating || _animatedSprite.SpriteFrames == null)
		{
			return;
		}

		string attackAnimation = ResolveAttackAnimationName();
		if (string.IsNullOrEmpty(attackAnimation))
		{
			return;
		}

		_isAttackAnimating = true;
		_activeAttackAnimationName = attackAnimation;
		_animatedSprite.Play(attackAnimation);
	}

	private void StartDeathSequence()
	{
		if (_isDying)
		{
			return;
		}

		_isDying = true;
		PlayDeathSfx();
		SetDeferred("sleeping", true);
		SetDeferred("linear_velocity", Vector2.Zero);
		SetDeferred("freeze", true);
		SetDeferred("collision_layer", 0u);
		SetDeferred("collision_mask", 0u);
		if (_animatedSprite != null)
		{
			_animatedSprite.Visible = false;
		}

		if (_explosionFrames == null)
		{
			CallDeferred(Node.MethodName.QueueFree);
			return;
		}

		var explosion = new AnimatedSprite2D();
		explosion.SpriteFrames = _explosionFrames;
		explosion.Animation = ExplosionAnimationName;
		explosion.Scale = ExplosionScale;
		explosion.ZIndex = 200;
		explosion.Position = Vector2.Zero;
		explosion.AnimationFinished += OnDeathExplosionFinished;
		AddChild(explosion);
		explosion.Play(ExplosionAnimationName);
	}

	private void OnDeathExplosionFinished()
	{
		QueueFree();
	}

	private void PlayDeathSfx()
	{
		if (string.IsNullOrEmpty(DeathSfxPath))
		{
			return;
		}

		AudioStream stream = GD.Load<AudioStream>(DeathSfxPath);
		if (stream == null)
		{
			return;
		}

		Node parent = GetTree().CurrentScene ?? GetParent();
		if (parent == null)
		{
			return;
		}

		var player = new AudioStreamPlayer2D();
		player.Stream = stream;
		player.VolumeDb = DeathSfxVolumeDb;
		player.GlobalPosition = GlobalPosition;
		player.Finished += player.QueueFree;
		parent.AddChild(player);
		player.Play();
	}

	private void UpdateFacingByVelocity()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		const float flipThreshold = 1.0f;
		if (Mathf.Abs(LinearVelocity.X) <= flipThreshold)
		{
			return;
		}

		// Keep character upright and only mirror sprite horizontally.
		_animatedSprite.FlipH = LinearVelocity.X < 0.0f;
	}

	private string ResolveAttackAnimationName()
	{
		if (_animatedSprite?.SpriteFrames == null)
		{
			return string.Empty;
		}

		if (_animatedSprite.SpriteFrames.HasAnimation(AttackAnimationName))
		{
			return AttackAnimationName;
		}

		var fallbackNames = new List<string>
		{
			"attack_right",
			"attack_down_right",
			"attack_up_right",
			"attack_up",
			"attack_down"
		};

		for (int i = 0; i < fallbackNames.Count; i++)
		{
			string candidate = fallbackNames[i];
			if (_animatedSprite.SpriteFrames.HasAnimation(candidate))
			{
				return candidate;
			}
		}

		return string.Empty;
	}

	private SpriteFrames BuildExplosionFrames()
	{
		if (string.IsNullOrEmpty(ExplosionTexturePath))
		{
			return null;
		}

		Texture2D texture = GD.Load<Texture2D>(ExplosionTexturePath);
		if (texture == null)
		{
			return null;
		}

		int frameW = Mathf.Max(1, ExplosionFrameWidth);
		int frameH = Mathf.Max(1, ExplosionFrameHeight);
		int columns = Mathf.Max(1, texture.GetWidth() / frameW);
		int rows = Mathf.Max(1, texture.GetHeight() / frameH);
		int availableFrames = columns * rows;
		int frameCount = ExplosionFrameCount > 0
			? Mathf.Clamp(ExplosionFrameCount, 1, availableFrames)
			: availableFrames;

		var frames = new SpriteFrames();
		frames.AddAnimation(ExplosionAnimationName);
		frames.SetAnimationLoop(ExplosionAnimationName, false);
		frames.SetAnimationSpeed(ExplosionAnimationName, Mathf.Max(1.0f, ExplosionAnimationSpeed));

		for (int i = 0; i < frameCount; i++)
		{
			int col = i % columns;
			int row = i / columns;
			if (row >= rows)
			{
				break;
			}

			var atlas = new AtlasTexture();
			atlas.Atlas = texture;
			atlas.Region = new Rect2(col * frameW, row * frameH, frameW, frameH);
			frames.AddFrame(ExplosionAnimationName, atlas);
		}

		return frames;
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite == null)
		{
			return;
		}

		if (!_isAttackAnimating)
		{
			return;
		}

		if (_animatedSprite.Animation != _activeAttackAnimationName)
		{
			return;
		}

		_isAttackAnimating = false;
		_activeAttackAnimationName = string.Empty;
		PlayMovementAnimation();
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
