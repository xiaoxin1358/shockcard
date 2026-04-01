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

	public override void _EnterTree()
	{
		AddToGroup("card_drop");
	}

	public override void _Ready()
	{
		_cardLabel = GetNodeOrNull<Label>("CardLabel");
		UpdateLabel();
	}

	public void BeginAutoCollect(CardManager cardManager, CardData data, int targetSlot, Vector2 targetGlobalPosition)
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
