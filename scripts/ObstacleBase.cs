using Godot;

public abstract partial class ObstacleBase : StaticBody2D
{
	[Signal]
	public delegate void BrokenEventHandler(ObstacleBase obstacle, float impactSpeed, Node2D source);

	[Signal]
	public delegate void HitButNotBrokenEventHandler(ObstacleBase obstacle, float impactSpeed, Node2D source);

	[Export] public float BreakSpeedThreshold = 200.0f;
	[Export] public float FeedbackDuration = 0.16f;
	[Export] public NodePath HitAreaPath = "HitArea";
	[Export] public NodePath VisualPath = "Sprite";

	private bool _isBroken;
	private Area2D _hitArea;
	private CanvasItem _visual;
	private FeedbackManager _feedback;

	protected virtual bool UseStoneFeedback => false;

	public override void _Ready()
	{
		_hitArea = GetNodeOrNull<Area2D>(HitAreaPath);
		_visual = GetNodeOrNull<CanvasItem>(VisualPath);
		_feedback = GetTree().GetFirstNodeInGroup("feedback_manager") as FeedbackManager;

		if (_hitArea != null)
		{
			_hitArea.BodyEntered += OnHitAreaBodyEntered;
		}
	}

	public bool TryBreakFromImpact(float impactSpeed, Node2D source)
	{
		if (_isBroken)
		{
			return false;
		}

		if (impactSpeed < BreakSpeedThreshold)
		{
			EmitSignal(SignalName.HitButNotBroken, this, impactSpeed, source);
			_feedback?.AddEnemyHitFeedback(impactSpeed * 0.45f);
			PlayBlockedFeedback();
			return false;
		}

		_isBroken = true;
		EmitSignal(SignalName.Broken, this, impactSpeed, source);
		_feedback?.AddObstacleBreakFeedback(impactSpeed, UseStoneFeedback);
		PlayBreakFeedbackAndDestroy();
		return true;
	}

	protected virtual float GetImpactSpeedFromBody(Node2D body)
	{
		if (body is CharacterBody2D character)
		{
			return character.Velocity.Length();
		}

		return 0.0f;
	}

	private void OnHitAreaBodyEntered(Node2D body)
	{
		if (_isBroken || body == null)
		{
			return;
		}

		if (!body.IsInGroup("player"))
		{
			return;
		}

		float impactSpeed = GetImpactSpeedFromBody(body);
		TryBreakFromImpact(impactSpeed, body);
	}

	private void PlayBlockedFeedback()
	{
		if (_visual == null)
		{
			return;
		}

		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(_visual, "modulate", new Color(1.0f, 0.85f, 0.85f, 1.0f), 0.06f);
		tween.TweenProperty(_visual, "modulate", Colors.White, 0.10f);
	}

	private void PlayBreakFeedbackAndDestroy()
	{
		if (_visual == null)
		{
			QueueFree();
			return;
		}

		Tween tween = CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(_visual, "modulate:a", 0.0f, FeedbackDuration);
		tween.Parallel().TweenProperty(_visual, "scale", new Vector2(0.75f, 0.75f), FeedbackDuration);
		tween.Finished += QueueFree;
	}
}
