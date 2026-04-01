using Godot;

public partial class EnemyController : CharacterBody2D
{
	[Export] public bool EnablePatrol = false;
	[Export] public float PatrolSpeed = 70.0f;
	[Export] public float PatrolHalfDistance = 48.0f;
	[Export] public PackedScene CardDropScene;

	private Area2D _hurtbox;
	private Marker2D _dropAnchor;
	private Node _dropsRoot;
	private CardManager _cardManager;
	private FeedbackManager _feedback;
	private Vector2 _spawnPosition;
	private int _patrolDir = 1;
	private bool _isDead;

	public override void _EnterTree()
	{
		AddToGroup("enemy");
	}

	public override void _Ready()
	{
		_hurtbox = GetNodeOrNull<Area2D>("Hurtbox");
		_dropAnchor = GetNodeOrNull<Marker2D>("DropAnchor");
		_spawnPosition = GlobalPosition;

		if (_hurtbox != null)
		{
			_hurtbox.BodyEntered += OnHurtboxBodyEntered;
		}

		_dropsRoot = GetTree().CurrentScene?.GetNodeOrNull<Node>("Runtime/Drops");
		_cardManager = GetTree().GetFirstNodeInGroup("card_manager") as CardManager;
		_feedback = GetTree().GetFirstNodeInGroup("feedback_manager") as FeedbackManager;

		if (CardDropScene == null)
		{
			CardDropScene = GD.Load<PackedScene>("res://scenes/CardDrop.tscn");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead)
		{
			return;
		}

		if (!EnablePatrol)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float dt = (float)delta;
		float leftBound = _spawnPosition.X - PatrolHalfDistance;
		float rightBound = _spawnPosition.X + PatrolHalfDistance;

		Velocity = new Vector2(_patrolDir * PatrolSpeed, 0.0f);
		MoveAndSlide();

		if (GlobalPosition.X <= leftBound)
		{
			_patrolDir = 1;
		}
		else if (GlobalPosition.X >= rightBound)
		{
			_patrolDir = -1;
		}
	}

	private void OnHurtboxBodyEntered(Node2D body)
	{
		if (_isDead)
		{
			return;
		}

		if (!body.IsInGroup("player"))
		{
			return;
		}

		float speed = body is CharacterBody2D cb ? cb.Velocity.Length() : 0.0f;
		_feedback?.AddEnemyHitFeedback(speed);

		DieAndDropCard();
	}

	private void DieAndDropCard()
	{
		_isDead = true;
		CardDrop drop = SpawnCardDrop();
		_cardManager?.NotifyEnemyKilled(drop);
		QueueFree();
	}

	private CardDrop SpawnCardDrop()
	{
		if (CardDropScene == null)
		{
			return null;
		}

		CardDrop drop = CardDropScene.Instantiate<CardDrop>();
		Vector2 dropPos = _dropAnchor != null ? _dropAnchor.GlobalPosition : GlobalPosition;
		drop.GlobalPosition = dropPos;

		if (_dropsRoot != null)
		{
			_dropsRoot.AddChild(drop);
		}
		else
		{
			GetTree().CurrentScene?.AddChild(drop);
		}

		return drop;
	}
}
