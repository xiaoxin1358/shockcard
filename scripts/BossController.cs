using Godot;

public partial class BossController : CharacterBody2D
{
	[Signal]
	public delegate void HpChangedEventHandler(float currentHp, float maxHp);

	[Signal]
	public delegate void PhaseChangedEventHandler(int phaseIndex, float thresholdPercent);

	[Signal]
	public delegate void DefeatedEventHandler();

	[Export] public float MaxHp = 1200.0f;
	[Export] public float StartHp = 1200.0f;

	public float CurrentHp { get; private set; }
	public int CurrentPhase { get; private set; }

	private readonly bool[] _phaseTriggered = new bool[3];
	private FeedbackManager _feedback;

	public override void _EnterTree()
	{
		AddToGroup("boss");
	}

	public override void _Ready()
	{
		_feedback = GetTree().GetFirstNodeInGroup("feedback_manager") as FeedbackManager;
		ResetBoss();
	}

	public void ResetBoss()
	{
		float startHp = StartHp > 0.0f ? StartHp : MaxHp;
		CurrentHp = Mathf.Clamp(startHp, 0.0f, MaxHp);
		CurrentPhase = 0;

		for (int i = 0; i < _phaseTriggered.Length; i++)
		{
			_phaseTriggered[i] = false;
		}

		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	public void TakeDamage(float amount)
	{
		if (amount <= 0.0f || CurrentHp <= 0.0f)
		{
			return;
		}

		CurrentHp = Mathf.Clamp(CurrentHp - amount, 0.0f, MaxHp);
		_feedback?.AddBossHitFeedback(amount, MaxHp);
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
		CheckPhaseTransitions();

		if (CurrentHp <= 0.0f)
		{
			EmitSignal(SignalName.Defeated);
		}
	}

	private void CheckPhaseTransitions()
	{
		if (MaxHp <= 0.0f)
		{
			return;
		}

		float hpPercent = (CurrentHp / MaxHp) * 100.0f;

		TryTriggerPhase(0, hpPercent, 75.0f);
		TryTriggerPhase(1, hpPercent, 50.0f);
		TryTriggerPhase(2, hpPercent, 25.0f);
	}

	private void TryTriggerPhase(int idx, float hpPercent, float threshold)
	{
		if (_phaseTriggered[idx])
		{
			return;
		}

		if (hpPercent <= threshold)
		{
			_phaseTriggered[idx] = true;
			CurrentPhase = idx + 1;
			EmitSignal(SignalName.PhaseChanged, idx, threshold);
		}
	}
}
