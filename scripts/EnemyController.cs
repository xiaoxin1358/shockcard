using Godot;

public partial class EnemyController : CharacterBody2D
{
	[Export] public bool EnablePatrol = false;
	[Export] public float PatrolSpeed = 70.0f;
	[Export] public float PatrolHalfDistance = 48.0f;
	[Export] public PackedScene CardDropScene;

	private Area2D _hurtbox;
	private Marker2D _dropAnchor;
	private Label _cardText;
	private Node _dropsRoot;
	private CardManager _cardManager;
	private FeedbackManager _feedback;
	private readonly RandomNumberGenerator _rng = new();
	private CardData _debugCardData;
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
		_cardText = GetNodeOrNull<Label>("Visual/CardText");
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

		_debugCardData = CreateRandomCardData();
		if (_cardText != null)
		{
			_cardText.Text = ToSuitSymbol(_debugCardData.Suit) + ToRankShort(_debugCardData.Rank);
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
		_cardManager?.NotifyEnemyKilled(drop, _debugCardData);
		QueueFree();
	}

	private CardData CreateRandomCardData()
	{
		CardSuit suit = (CardSuit)_rng.RandiRange(0, 3);
		CardRank rank = (CardRank)_rng.RandiRange(0, 12);
		return new CardData(suit, rank);
	}

	private string ToSuitSymbol(CardSuit suit)
	{
		return suit switch
		{
			CardSuit.Spade => "♠",
			CardSuit.Heart => "♥",
			CardSuit.Club => "♣",
			CardSuit.Diamond => "♦",
			_ => "?"
		};
	}

	private string ToRankShort(CardRank rank)
	{
		return rank switch
		{
			CardRank.Ace => "A",
			CardRank.Two => "2",
			CardRank.Three => "3",
			CardRank.Four => "4",
			CardRank.Five => "5",
			CardRank.Six => "6",
			CardRank.Seven => "7",
			CardRank.Eight => "8",
			CardRank.Nine => "9",
			CardRank.Ten => "10",
			CardRank.Jack => "J",
			CardRank.Queen => "Q",
			CardRank.King => "K",
			_ => "?"
		};
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
			_dropsRoot.CallDeferred(Node.MethodName.AddChild, drop);
		}
		else
		{
			GetTree().CurrentScene?.CallDeferred(Node.MethodName.AddChild, drop);
		}

		return drop;
	}
}
