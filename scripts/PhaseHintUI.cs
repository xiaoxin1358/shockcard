using Godot;

public partial class PhaseHintUI : CenterContainer
{
	[Export] public float VisibleDuration = 1.0f;

	private Label _phaseText;
	private BossController _boundBoss;

	public override void _Ready()
	{
		_phaseText = GetNodeOrNull<Label>("PhaseText");
		Visible = false;
	}

	public void BindBoss(BossController boss)
	{
		if (boss == null)
		{
			return;
		}

		if (_boundBoss != null && IsInstanceValid(_boundBoss))
		{
			Callable phaseCb = Callable.From<int, float>(OnBossPhaseChanged);
			if (_boundBoss.IsConnected("PhaseChanged", phaseCb))
			{
				_boundBoss.Disconnect("PhaseChanged", phaseCb);
			}
		}

		_boundBoss = boss;
		_boundBoss.Connect("PhaseChanged", Callable.From<int, float>(OnBossPhaseChanged));
	}

	private void OnBossPhaseChanged(int phaseIndex, float thresholdPercent)
	{
		if (_phaseText == null)
		{
			return;
		}

		_phaseText.Text = "PHASE " + (phaseIndex + 1) + " (" + thresholdPercent + "%)";
		Visible = true;
		Modulate = new Color(1, 1, 1, 1);

		var timer = GetTree().CreateTimer(VisibleDuration);
		timer.Timeout += () =>
		{
			Tween tween = CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.TweenProperty(this, "modulate:a", 0.0f, 0.18f);
			tween.Finished += () => { Visible = false; };
		};
	}
}
