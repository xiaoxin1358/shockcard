using Godot;

public partial class FeedbackManager : Node
{
	[Export] public NodePath CameraPath = "Runtime/Player/Camera2D";
	[Export] public float MaxShakePixels = 22.0f;
	[Export] public float ShakeRecoverSpeed = 6.0f;

	private Camera2D _camera;
	private Vector2 _baseOffset = Vector2.Zero;
	private float _shakeStrength;
	private readonly RandomNumberGenerator _rng = new();

	private bool _isHitStopping;
	private ulong _hitStopEndMs;

	public override void _EnterTree()
	{
		AddToGroup("feedback_manager");
	}

	public override void _Ready()
	{
		Node root = GetTree().CurrentScene;
		_camera = root?.GetNodeOrNull<Camera2D>(CameraPath);
		if (_camera != null)
		{
			_baseOffset = _camera.Offset;
		}
	}

	public override void _Process(double delta)
	{
		UpdateHitStop();
		UpdateShake((float)delta);
	}

	public void AddEnemyHitFeedback(float speed)
	{
		float t = Mathf.Clamp(speed / 900.0f, 0.15f, 1.0f);
		RequestShake(t * 0.9f);
		RequestHitStop(Mathf.Lerp(0.035f, 0.08f, t), Mathf.Lerp(0.35f, 0.18f, t));
	}

	public void AddObstacleBreakFeedback(float speed, bool isStone)
	{
		float t = Mathf.Clamp(speed / 900.0f, 0.2f, 1.0f);
		float materialBoost = isStone ? 1.2f : 0.85f;
		RequestShake(Mathf.Clamp(t * materialBoost, 0.15f, 1.0f));

		float baseStop = isStone ? 0.085f : 0.055f;
		float baseScale = isStone ? 0.18f : 0.28f;
		RequestHitStop(Mathf.Lerp(baseStop * 0.6f, baseStop, t), Mathf.Lerp(baseScale, baseScale * 0.7f, t));
	}

	public void AddBossHitFeedback(float damage, float bossMaxHp)
	{
		float t = bossMaxHp <= 0.0f ? 0.6f : Mathf.Clamp(damage / (bossMaxHp * 0.12f), 0.35f, 1.0f);
		RequestShake(Mathf.Clamp(t * 1.3f, 0.2f, 1.0f));
		RequestHitStop(Mathf.Lerp(0.06f, 0.11f, t), Mathf.Lerp(0.22f, 0.12f, t));
	}

	public void AddSettlementFeedback(float multiplier)
	{
		float t = Mathf.Clamp((multiplier - 1.0f) / 3.5f, 0.2f, 1.0f);
		RequestShake(Mathf.Lerp(0.25f, 0.9f, t));
		RequestHitStop(Mathf.Lerp(0.055f, 0.1f, t), Mathf.Lerp(0.30f, 0.15f, t));
	}

	private void RequestShake(float normalized)
	{
		_shakeStrength = Mathf.Max(_shakeStrength, Mathf.Clamp(normalized, 0.0f, 1.0f));
	}

	private void RequestHitStop(float durationSec, float slowedTimeScale)
	{
		ulong endMs = Time.GetTicksMsec() + (ulong)Mathf.RoundToInt(durationSec * 1000.0f);
		if (!_isHitStopping || endMs > _hitStopEndMs)
		{
			_hitStopEndMs = endMs;
		}

		Engine.TimeScale = Mathf.Min(Engine.TimeScale, Mathf.Clamp(slowedTimeScale, 0.05f, 1.0f));
		_isHitStopping = true;
	}

	private void UpdateHitStop()
	{
		if (!_isHitStopping)
		{
			return;
		}

		if (Time.GetTicksMsec() >= _hitStopEndMs)
		{
			Engine.TimeScale = 1.0f;
			_isHitStopping = false;
		}
	}

	private void UpdateShake(float dt)
	{
		if (_camera == null)
		{
			return;
		}

		_shakeStrength = Mathf.Max(0.0f, _shakeStrength - ShakeRecoverSpeed * dt);

		if (_shakeStrength <= 0.001f)
		{
			_camera.Offset = _baseOffset;
			return;
		}

		float amp = MaxShakePixels * _shakeStrength;
		Vector2 jitter = new Vector2(
			_rng.RandfRange(-amp, amp),
			_rng.RandfRange(-amp, amp)
		);

		_camera.Offset = _baseOffset + jitter;
	}
}
