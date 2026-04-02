using Godot;

public partial class CardDrop : Area2D
{
	[Export] public float FlyDuration = 0.28f;
	[Export] public float ScaleAtTarget = 0.55f;

	public CardData Data { get; private set; }

	private bool _isCollecting;
	private CardManager _cardManager;
	private int _targetSlot;
	private Vector2 _targetGlobal;
	private Label _cardLabel;
	private bool _hasPendingCollect;
	private CardManager _pendingCardManager;
	private CardData _pendingData;
	private int _pendingTargetSlot;
	private Vector2 _pendingTargetGlobal;

	public override void _EnterTree()
	{
		AddToGroup("card_drop");
	}

	public override void _Ready()
	{
		_cardLabel = GetNodeOrNull<Label>("CardLabel");
		UpdateLabel();

		if (_hasPendingCollect)
		{
			_hasPendingCollect = false;
			BeginCollectInternal(_pendingCardManager, _pendingData, _pendingTargetSlot, _pendingTargetGlobal);
		}
	}

	public void BeginAutoCollect(CardManager cardManager, CardData data, int targetSlot, Vector2 targetGlobalPosition)
	{
		if (_isCollecting)
		{
			return;
		}

		if (!IsInsideTree())
		{
			Data = data;
			_pendingCardManager = cardManager;
			_pendingData = data;
			_pendingTargetSlot = targetSlot;
			_pendingTargetGlobal = targetGlobalPosition;
			_hasPendingCollect = true;
			return;
		}

		BeginCollectInternal(cardManager, data, targetSlot, targetGlobalPosition);
	}

	private void BeginCollectInternal(CardManager cardManager, CardData data, int targetSlot, Vector2 targetGlobalPosition)
	{
		if (_isCollecting)
		{
			return;
		}

		_isCollecting = true;
		_cardManager = cardManager;
		Data = data;
		_targetSlot = targetSlot;
		_targetGlobal = targetGlobalPosition;
		UpdateLabel();

		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(this, "global_position", _targetGlobal, FlyDuration);
		tween.Parallel().TweenProperty(this, "scale", new Vector2(ScaleAtTarget, ScaleAtTarget), FlyDuration);
		tween.Finished += OnFlyFinished;
	}

	private void OnFlyFinished()
	{
		_cardManager?.TryCollectCard(Data, _targetSlot);
		QueueFree();
	}

	private void UpdateLabel()
	{
		if (_cardLabel != null)
		{
			_cardLabel.Text = Data.DisplayText;
		}
	}
}
