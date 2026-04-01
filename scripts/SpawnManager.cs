using Godot;
using System.Collections.Generic;

public partial class SpawnManager : Node
{
	[Export] public PackedScene EnemyScene;
	[Export] public PackedScene ObstacleScene;

	[Export] public NodePath EnemiesContainerPath = "Runtime/Enemies";
	[Export] public NodePath ObstaclesContainerPath = "Runtime/Obstacles";
	[Export] public NodePath DropsContainerPath = "Runtime/Drops";
	[Export] public NodePath EnemySpawnPointsPath = "World/SpawnPoints/EnemySpawns";
	[Export] public NodePath ObstacleSpawnPointsPath = "World/SpawnPoints/ObstacleSpawns";
	[Export] public NodePath PlayerPath = "Runtime/PlayerInstance";
	[Export] public NodePath PlayerSpawnPath = "World/SpawnPoints/PlayerSpawn";

	[Export] public int EnemySpawnCount = 2;
	[Export] public int ObstacleSpawnCount = 2;
	[Export] public bool RandomObstacleType = true;

	public override void _EnterTree()
	{
		AddToGroup("spawn_manager");
	}

	public void ClearEnemies()
	{
		Node enemiesContainer = ResolveNode(EnemiesContainerPath);
		if (enemiesContainer == null)
		{
			return;
		}

		foreach (Node child in enemiesContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	public void ClearObstacles()
	{
		Node obstaclesContainer = ResolveNode(ObstaclesContainerPath);
		if (obstaclesContainer == null)
		{
			return;
		}

		foreach (Node child in obstaclesContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	public void ClearRuntimeCombatants()
	{
		ClearEnemies();
		ClearObstacles();
		ClearDrops();
	}

	public void ClearDrops()
	{
		Node dropsContainer = ResolveNode(DropsContainerPath);
		if (dropsContainer == null)
		{
			return;
		}

		foreach (Node child in dropsContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	public void SpawnEnemies()
	{
		if (EnemyScene == null)
		{
			return;
		}

		Node enemiesContainer = ResolveNode(EnemiesContainerPath);
		if (enemiesContainer == null)
		{
			return;
		}

		List<Marker2D> markers = GetMarkerChildren(EnemySpawnPointsPath);
		if (markers.Count == 0)
		{
			return;
		}

		for (int i = 0; i < EnemySpawnCount; i++)
		{
			Marker2D marker = markers[i % markers.Count];
			Node enemyNode = EnemyScene.Instantiate();
			if (enemyNode is Node2D enemy2D)
			{
				enemy2D.Position = marker.Position;
			}
			enemiesContainer.AddChild(enemyNode);
		}
	}

	public void SpawnObstacles()
	{
		if (ObstacleScene == null)
		{
			return;
		}

		Node obstaclesContainer = ResolveNode(ObstaclesContainerPath);
		if (obstaclesContainer == null)
		{
			return;
		}

		List<Marker2D> markers = GetMarkerChildren(ObstacleSpawnPointsPath);
		if (markers.Count == 0)
		{
			markers = new List<Marker2D>
			{
				CreateFallbackMarker(new Vector2(500, 390)),
				CreateFallbackMarker(new Vector2(760, 390))
			};
		}

		for (int i = 0; i < ObstacleSpawnCount; i++)
		{
			Marker2D marker = markers[i % markers.Count];
			Node obstacleNode = ObstacleScene.Instantiate();
			if (obstacleNode is Node2D obstacle2D)
			{
				obstacle2D.Position = marker.Position;
			}

			if (obstacleNode is ObstacleController obstacleController)
			{
				if (RandomObstacleType)
				{
					bool useStone = GD.Randf() >= 0.5f;
					obstacleController.ActiveKind = useStone
						? ObstacleController.ObstacleKind.Stone
						: ObstacleController.ObstacleKind.Wood;
				}
				else
				{
					obstacleController.ActiveKind = ObstacleController.ObstacleKind.Wood;
				}
			}

			obstaclesContainer.AddChild(obstacleNode);
		}
	}

	public void SpawnRuntimeCombatants()
	{
		SpawnEnemies();
		SpawnObstacles();
	}

	public void ResetPlayerToSpawn()
	{
		Node playerNode = ResolveNode(PlayerPath);
		Marker2D playerSpawn = ResolveNode(PlayerSpawnPath) as Marker2D;
		if (playerNode is Node2D player2D && playerSpawn != null)
		{
			player2D.Position = playerSpawn.Position;
		}
	}

	public void KeepPlayerPosition()
	{
		// Intentionally empty: keeps current player position.
	}

	public void ResetRuntime(bool keepPlayerPosition)
	{
		if (keepPlayerPosition)
		{
			KeepPlayerPosition();
		}
		else
		{
			ResetPlayerToSpawn();
		}

		ClearRuntimeCombatants();
		SpawnRuntimeCombatants();
	}

	private Node ResolveNode(NodePath path)
	{
		Node currentScene = GetTree().CurrentScene;
		if (currentScene != null)
		{
			Node fromScene = currentScene.GetNodeOrNull(path);
			if (fromScene != null)
			{
				return fromScene;
			}
		}

		return GetNodeOrNull(path);
	}

	private List<Marker2D> GetMarkerChildren(NodePath containerPath)
	{
		var result = new List<Marker2D>();
		Node container = ResolveNode(containerPath);
		if (container == null)
		{
			return result;
		}

		foreach (Node child in container.GetChildren())
		{
			if (child is Marker2D marker)
			{
				result.Add(marker);
			}
		}

		return result;
	}

	private Marker2D CreateFallbackMarker(Vector2 position)
	{
		var marker = new Marker2D();
		marker.Position = position;
		return marker;
	}
}
