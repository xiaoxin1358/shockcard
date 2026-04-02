using Godot;
using System.Collections.Generic;
using System.Text;

public partial class DebugManager : Node
{
	[Signal]
	public delegate void DebugModeChangedEventHandler(bool enabled);

	public static DebugManager Instance { get; private set; }

	[Export] public bool StartEnabled = false;
	[Export] public bool ShowCollisionShapes = true;
	[Export] public string ToggleAction = "debug_toggle";
	[Export] public Key FallbackToggleKey = Key.F1;

	public bool IsDebugEnabled { get; private set; }

	private HUD _hud;
	private PlayerController _player;
	private EnergyManager _energyManager;
	private GameManager _gameManager;
	private DebugCollisionOverlay _collisionOverlay;
	private readonly List<CollisionShape2D> _shapeBuffer = new();

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public override void _Ready()
	{
		SetProcess(true);
		SetProcessInput(true);
		EnsureOverlayParent();
		SetDebugEnabled(StartEnabled);
	}

	public override void _Input(InputEvent @event)
	{
		if (IsToggleEvent(@event))
		{
			ToggleDebug();
		}
	}

	public override void _Process(double delta)
	{
		CacheSceneRefs();

		if (_hud == null)
		{
			return;
		}

		if (!IsDebugEnabled)
		{
			_hud.SetDebugVisible(false);
			UpdateOverlayVisible();
			return;
		}

		_hud.SetDebugVisible(true);
		_hud.SetDebugText(BuildDebugText());
		UpdateCollisionShapes();
	}

	public void ToggleDebug()
	{
		SetDebugEnabled(!IsDebugEnabled);
	}

	public void SetDebugEnabled(bool enabled)
	{
		if (IsDebugEnabled == enabled)
		{
			UpdateOverlayVisible();
			return;
		}

		IsDebugEnabled = enabled;

		if (_hud != null)
		{
			_hud.SetDebugVisible(enabled);
			if (!enabled)
			{
				_hud.SetDebugText(string.Empty);
			}
		}

		UpdateOverlayVisible();
		EmitSignal(SignalName.DebugModeChanged, enabled);
	}

	private bool IsToggleEvent(InputEvent @event)
	{
		if (InputMap.HasAction(ToggleAction) && @event.IsActionPressed(ToggleAction))
		{
			return true;
		}

		if (!InputMap.HasAction(ToggleAction) && @event is InputEventKey keyEvent)
		{
			if (keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == FallbackToggleKey)
			{
				return true;
			}
		}

		return false;
	}

	private void CacheSceneRefs()
	{
		if (_hud == null || !IsInstanceValid(_hud))
		{
			_hud = GetTree().GetFirstNodeInGroup("hud_controller") as HUD;
		}

		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		}

		if (_energyManager == null || !IsInstanceValid(_energyManager))
		{
			_energyManager = GetTree().GetFirstNodeInGroup("energy_manager") as EnergyManager;
		}

		if (_gameManager == null || !IsInstanceValid(_gameManager))
		{
			_gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		}

		EnsureOverlayParent();
	}

	private string BuildDebugText()
	{
		StringBuilder sb = new();
		sb.AppendLine("DEBUG MODE");

		if (_player != null)
		{
			float speed = _player.Velocity.Length();
			sb.AppendLine($"Speed: {speed:0.0}");
			sb.AppendLine($"PlayerState: {_player.CurrentStateName}");
		}
		else
		{
			sb.AppendLine("Speed: N/A");
			sb.AppendLine("PlayerState: N/A");
		}

		if (_energyManager != null)
		{
			sb.AppendLine($"Energy: {_energyManager.CurrentEnergy:0.0}/{_energyManager.MaxEnergy:0.0}");
		}
		else
		{
			sb.AppendLine("Energy: N/A");
		}

		if (_gameManager != null)
		{
			sb.AppendLine($"GameState: {_gameManager.CurrentState}");
		}
		else
		{
			sb.AppendLine("GameState: N/A");
		}

		sb.Append($"Collision: {(ShowCollisionShapes ? "On" : "Off")}");
		return sb.ToString();
	}

	private void EnsureOverlayParent()
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene == null)
		{
			return;
		}

		if (_collisionOverlay == null || !IsInstanceValid(_collisionOverlay))
		{
			_collisionOverlay = new DebugCollisionOverlay();
			_collisionOverlay.Name = "DebugCollisionOverlay";
		}

		if (_collisionOverlay.GetParent() != currentScene)
		{
			_collisionOverlay.GetParent()?.RemoveChild(_collisionOverlay);
			currentScene.AddChild(_collisionOverlay);
		}
	}

	private void UpdateCollisionShapes()
	{
		if (_collisionOverlay == null || !IsInstanceValid(_collisionOverlay))
		{
			return;
		}

		if (!IsDebugEnabled || !ShowCollisionShapes)
		{
			_collisionOverlay.SetShapes(_shapeBuffer);
			return;
		}

		_shapeBuffer.Clear();
		CollectShapesFromGroup("player");
		CollectShapesFromGroup("enemy");
		CollectShapesFromGroup("obstacle");
		_collisionOverlay.SetShapes(_shapeBuffer);
	}

	private void CollectShapesFromGroup(string groupName)
	{
		Godot.Collections.Array<Node> nodes = GetTree().GetNodesInGroup(groupName);
		for (int i = 0; i < nodes.Count; i++)
		{
			CollectCollisionShapesRecursive(nodes[i]);
		}
	}

	private void CollectCollisionShapesRecursive(Node node)
	{
		if (node is CollisionShape2D collision)
		{
			if (!collision.Disabled && collision.Shape != null && collision.IsVisibleInTree())
			{
				_shapeBuffer.Add(collision);
			}
		}

		Godot.Collections.Array<Node> children = node.GetChildren();
		for (int i = 0; i < children.Count; i++)
		{
			CollectCollisionShapesRecursive(children[i]);
		}
	}

	private void UpdateOverlayVisible()
	{
		if (_collisionOverlay == null || !IsInstanceValid(_collisionOverlay))
		{
			return;
		}

		bool shouldShow = IsDebugEnabled && ShowCollisionShapes;
		_collisionOverlay.Visible = shouldShow;
		if (!shouldShow)
		{
			_shapeBuffer.Clear();
			_collisionOverlay.SetShapes(_shapeBuffer);
		}
	}
}

public partial class DebugCollisionOverlay : Node2D
{
	private readonly List<CollisionShape2D> _shapes = new();
	private readonly Color _lineColor = new Color(0.15f, 1.0f, 0.3f, 0.95f);
	private const float LineWidth = 2.0f;

	public void SetShapes(List<CollisionShape2D> source)
	{
		_shapes.Clear();
		for (int i = 0; i < source.Count; i++)
		{
			if (IsInstanceValid(source[i]))
			{
				_shapes.Add(source[i]);
			}
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		for (int i = 0; i < _shapes.Count; i++)
		{
			CollisionShape2D collision = _shapes[i];
			if (!IsInstanceValid(collision) || collision.Shape == null)
			{
				continue;
			}

			if (collision.Shape is CircleShape2D circle)
			{
				DrawCircleShape(collision, circle);
				continue;
			}

			if (collision.Shape is RectangleShape2D rect)
			{
				DrawRectangleShape(collision, rect);
			}
		}
	}

	private void DrawCircleShape(CollisionShape2D collision, CircleShape2D circle)
	{
		Transform2D transform = collision.GlobalTransform;
		Vector2 center = ToLocal(transform.Origin);
		float scaleX = transform.X.Length();
		float scaleY = transform.Y.Length();
		float radius = circle.Radius * Mathf.Max(0.01f, (scaleX + scaleY) * 0.5f);
		DrawArc(center, radius, 0.0f, Mathf.Tau, 40, _lineColor, LineWidth);
	}

	private void DrawRectangleShape(CollisionShape2D collision, RectangleShape2D rect)
	{
		Vector2 half = rect.Size * 0.5f;
		Vector2[] corners =
		{
			new Vector2(-half.X, -half.Y),
			new Vector2(half.X, -half.Y),
			new Vector2(half.X, half.Y),
			new Vector2(-half.X, half.Y)
		};

		for (int i = 0; i < corners.Length; i++)
		{
			Vector2 a = ToLocal(collision.GlobalTransform * corners[i]);
			Vector2 b = ToLocal(collision.GlobalTransform * corners[(i + 1) % corners.Length]);
			DrawLine(a, b, _lineColor, LineWidth);
		}
	}
}
