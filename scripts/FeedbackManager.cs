using Godot;

public partial class FeedbackManager : Node
{
	[Export] public NodePath CameraPath = "Runtime/Player/Camera2D";
	[Export] public float MaxShakePixels = 22.0f;
	[Export] public float ShakeRecoverSpeed = 6.0f;
	[Export] public string BossHitExplosionVideoPath = "res://sorces/boom.ogv";
	[Export] public NodePath BossHitExplosionPlayerPath = "BossHitExplosionVideo";
	[Export] public string BossHitExplosionShaderPath = "res://shaders/green_screen_key.gdshader";
	[Export(PropertyHint.Range, "-40,6,0.5")] public float BossHitExplosionVolumeDb = -10.0f;
	[Export] public Color BossHitKeyColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
	[Export] public float BossHitKeyThreshold = 0.35f;
	[Export] public float BossHitKeySoftness = 0.08f;
	[Export] public bool EnableBossHitDebugHotkey = true;
	[Export] public Key BossHitDebugPlayKey = Key.F2;
	[Export] public bool EnableBossHitCameraZoomOut = true;
	[Export(PropertyHint.Range, "0.2,1.0,0.01")] public float BossHitCameraZoomOut = 0.8f;
	[Export(PropertyHint.Range, "0.05,2.0,0.01")] public float BossHitCameraZoomOutDuration = 0.85f;

	private Camera2D _camera;
	private Vector2 _baseOffset = Vector2.Zero;
	private float _shakeStrength;
	private readonly RandomNumberGenerator _rng = new();
	private VideoStream _bossHitExplosionStream;
	private Shader _bossHitKeyShader;
	private VideoStreamPlayer _bossHitExplosionPlayer;
	private Callable _bossHitExplosionFinishedCallable;
	private bool _wasDebugPlayKeyDown;

	private bool _isHitStopping;
	private ulong _hitStopEndMs;

	public override void _EnterTree()
	{
		AddToGroup("feedback_manager");
		_bossHitExplosionFinishedCallable = Callable.From((System.Action)OnBossHitExplosionFinished);
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_bossHitExplosionPlayer))
		{
			EnsureBossHitExplosionFinishedDisconnected(_bossHitExplosionPlayer);
		}
	}

	public override void _Ready()
	{
		Node root = GetTree().CurrentScene;
		_camera = root?.GetNodeOrNull<Camera2D>(CameraPath);
		_bossHitExplosionStream = TryLoadBossHitExplosionStream();
		_bossHitKeyShader = TryLoadKeyShader();
		if (_camera != null)
		{
			_baseOffset = _camera.Offset;
		}
	}

	public override void _Process(double delta)
	{
		HandleDebugPlayHotkey();
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
		TryTriggerBossHitCameraZoomOut();
		PlayBossHitExplosionVideo();
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

	private void TryTriggerBossHitCameraZoomOut()
	{
		if (!EnableBossHitCameraZoomOut)
		{
			return;
		}

		PlayerController player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		if (player == null)
		{
			return;
		}

		player.TriggerBossHitCameraZoomOut(BossHitCameraZoomOut, BossHitCameraZoomOutDuration);
	}

	private VideoStream TryLoadBossHitExplosionStream()
	{
		if (string.IsNullOrEmpty(BossHitExplosionVideoPath))
		{
			return null;
		}

		if (!ResourceLoader.Exists(BossHitExplosionVideoPath))
		{
			GD.PushWarning("[FeedbackManager] BossHitExplosionVideoPath not found: " + BossHitExplosionVideoPath);
			return null;
		}

		if (!IsSupportedVideoExtension(BossHitExplosionVideoPath))
		{
			GD.PushWarning("[FeedbackManager] Video format is not supported here. Please convert to .webm or .ogv: " + BossHitExplosionVideoPath);
			return null;
		}

		VideoStream stream = ResourceLoader.Load<VideoStream>(BossHitExplosionVideoPath);
		if (stream == null)
		{
			GD.PushWarning("[FeedbackManager] Failed to load video stream: " + BossHitExplosionVideoPath);
		}

		return stream;
	}

	private Shader TryLoadKeyShader()
	{
		if (string.IsNullOrEmpty(BossHitExplosionShaderPath))
		{
			return null;
		}

		if (!ResourceLoader.Exists(BossHitExplosionShaderPath))
		{
			GD.PushWarning("[FeedbackManager] Shader not found: " + BossHitExplosionShaderPath);
			return null;
		}

		return GD.Load<Shader>(BossHitExplosionShaderPath);
	}

	private bool IsSupportedVideoExtension(string path)
	{
		string lower = path.ToLowerInvariant();
		return lower.EndsWith(".webm") || lower.EndsWith(".ogv");
	}

	private void HandleDebugPlayHotkey()
	{
		if (!EnableBossHitDebugHotkey)
		{
			_wasDebugPlayKeyDown = false;
			return;
		}

		bool isKeyDown = Input.IsKeyPressed(BossHitDebugPlayKey);
		if (isKeyDown && !_wasDebugPlayKeyDown)
		{
			TryTriggerBossHitCameraZoomOut();
			PlayBossHitExplosionVideo();
		}

		_wasDebugPlayKeyDown = isKeyDown;
	}

	private void PlayBossHitExplosionVideo()
	{
		Node bossNode = GetTree().GetFirstNodeInGroup("boss");
		if (bossNode == null)
		{
			GD.PushWarning("[FeedbackManager] Boss node not found in group: boss");
			return;
		}

		VideoStreamPlayer player = bossNode.GetNodeOrNull<VideoStreamPlayer>(BossHitExplosionPlayerPath);
		if (player == null)
		{
			GD.PushWarning("[FeedbackManager] BossHitExplosionVideo node not found under boss: " + BossHitExplosionPlayerPath);
			return;
		}

		if (player.Stream == null && _bossHitExplosionStream != null)
		{
			player.Stream = _bossHitExplosionStream;
		}

		player.Loop = false;
		player.VolumeDb = Mathf.Clamp(BossHitExplosionVolumeDb, -80.0f, 24.0f);

		if (_bossHitKeyShader != null)
		{
			var material = new ShaderMaterial();
			material.Shader = _bossHitKeyShader;
			material.SetShaderParameter("key_color", BossHitKeyColor);
			material.SetShaderParameter("threshold", BossHitKeyThreshold);
			material.SetShaderParameter("softness", BossHitKeySoftness);
			player.Material = material;
		}

		if (_bossHitExplosionPlayer != player && IsInstanceValid(_bossHitExplosionPlayer))
		{
			EnsureBossHitExplosionFinishedDisconnected(_bossHitExplosionPlayer);
		}

		_bossHitExplosionPlayer = player;
		EnsureBossHitExplosionFinishedDisconnected(_bossHitExplosionPlayer);
		EnsureBossHitExplosionFinishedConnected(_bossHitExplosionPlayer);

		player.Stop();
		player.Visible = true;
		player.Play();
	}

	private void OnBossHitExplosionFinished()
	{
		if (!IsInstanceValid(_bossHitExplosionPlayer))
		{
			return;
		}

		_bossHitExplosionPlayer.Stop();
		_bossHitExplosionPlayer.Visible = false;
	}

	private void EnsureBossHitExplosionFinishedConnected(VideoStreamPlayer player)
	{
		if (player == null)
		{
			return;
		}

		if (!player.IsConnected(VideoStreamPlayer.SignalName.Finished, _bossHitExplosionFinishedCallable))
		{
			player.Connect(VideoStreamPlayer.SignalName.Finished, _bossHitExplosionFinishedCallable);
		}
	}

	private void EnsureBossHitExplosionFinishedDisconnected(VideoStreamPlayer player)
	{
		if (player == null)
		{
			return;
		}

		if (player.IsConnected(VideoStreamPlayer.SignalName.Finished, _bossHitExplosionFinishedCallable))
		{
			player.Disconnect(VideoStreamPlayer.SignalName.Finished, _bossHitExplosionFinishedCallable);
		}
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
