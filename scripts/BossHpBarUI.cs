using Godot;

public partial class BossHpBarUI : VBoxContainer
{
	private Label _bossName;
	private TextureProgressBar _bossHpBar;
	private Label _bossHpText;
	private BossController _boundBoss;

	public override void _Ready()
	{
		_bossName = GetNodeOrNull<Label>("BossName");
		_bossHpBar = GetNodeOrNull<TextureProgressBar>("BossHpBar");
		_bossHpText = GetNodeOrNull<Label>("BossHpText");

		if (_bossName != null)
		{
			_bossName.Text = "Boss HP";
		}
	}

	public void BindBoss(BossController boss)
	{
		if (boss == null)
		{
			return;
		}

		if (_boundBoss != null && IsInstanceValid(_boundBoss))
		{
			Callable hpCb = Callable.From<float, float>(OnBossHpChanged);
			if (_boundBoss.IsConnected("HpChanged", hpCb))
			{
				_boundBoss.Disconnect("HpChanged", hpCb);
			}
		}

		_boundBoss = boss;
		_boundBoss.Connect("HpChanged", Callable.From<float, float>(OnBossHpChanged));
		OnBossHpChanged(_boundBoss.CurrentHp, _boundBoss.MaxHp);
	}

	private void OnBossHpChanged(float currentHp, float maxHp)
	{
		if (_bossHpBar == null)
		{
			return;
		}

		_bossHpBar.MaxValue = maxHp;
		_bossHpBar.Value = currentHp;

		if (_bossHpText != null)
		{
			_bossHpText.Text = Mathf.CeilToInt(currentHp) + " / " + Mathf.CeilToInt(maxHp);
		}
	}
}
