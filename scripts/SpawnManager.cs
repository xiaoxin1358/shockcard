using Godot;
using System.Collections.Generic;

public partial class SpawnManager : Node
{
	[Export] public PackedScene EnemyScene;
	[Export] public PackedScene ChaserEnemyScene;
	[Export] public PackedScene ChaserArcherScene;
	[Export] public PackedScene ChaserLancerScene;
	[Export] public PackedScene ChaserPawnScene;
	[Export] public PackedScene ObstacleScene;

	[Export] public NodePath EnemiesContainerPath = "Runtime/Enemies";
	[Export] public NodePath ObstaclesContainerPath = "Runtime/Obstacles";
	[Export] public NodePath DropsContainerPath = "Runtime/Drops";
	[Export] public NodePath EnemySpawnPointsPath = "World/SpawnPoints/EnemySpawns";
	[Export] public NodePath ObstacleSpawnPointsPath = "World/SpawnPoints/ObstacleSpawns";
	[Export] public NodePath PlayerPath = "Runtime/Player";
	[Export] public NodePath PlayerSpawnPath = "World/SpawnPoints/PlayerSpawn";

	[Export] public int EnemySpawnCount = 2;
	[Export] public int ObstacleSpawnCount = 2;
	[Export] public bool RandomObstacleType = true;
	[Export] public bool EnablePeriodicEnemySpawn = true;
	[Export] public float EnemySpawnIntervalSec = 2.0f;
	[Export] public bool EnablePeriodicChaserSpawn = true;
	[Export] public float ChaserSpawnIntervalSec = 3.0f;
	[Export] public int InitialChaserSpawnCount = 1;
	[Export] public int MaxChaserEnemyCount = 3;
	[Export] public Vector2 RandomSpawnHalfExtents = new Vector2(500.0f, 500.0f);
	[Export] public float RandomSpawnEdgePadding = 72.0f;

	private double _enemySpawnTimer;
	private double _chaserSpawnTimer;

	public override void _EnterTree()
	{
		AddToGroup("spawn_manager");
	}

	public override void _Ready()
	{
		EnsureChaserScenesLoaded();
		ClearEnemies();
		SpawnRuntimeCombatants();
	}

	public override void _Process(double delta)
	{
		if (!EnablePeriodicEnemySpawn)
		{
			_enemySpawnTimer = 0.0;
		}
		else
		{
			float interval = Mathf.Max(0.2f, EnemySpawnIntervalSec);
			_enemySpawnTimer += delta;

			while (_enemySpawnTimer >= interval)
			{
				_enemySpawnTimer -= interval;
				SpawnSingleEnemyRandom();
			}
		}

		if (!EnablePeriodicChaserSpawn)
		{
			_chaserSpawnTimer = 0.0;
			return;
		}

		float chaserInterval = Mathf.Max(0.2f, ChaserSpawnIntervalSec);
		_chaserSpawnTimer += delta;

		while (_chaserSpawnTimer >= chaserInterval)
		{
			_chaserSpawnTimer -= chaserInterval;
			SpawnSingleChaserEnemyRandom();
		}
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
				CreateFallbackMarker(new Vector2(-150, 140)),
				CreateFallbackMarker(new Vector2(170, -120))
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
		int initialEnemyCount = Mathf.Max(0, EnemySpawnCount);
		for (int i = 0; i < initialEnemyCount; i++)
		{
			SpawnSingleEnemyRandom();
		}

		int initialChaserCount = Mathf.Clamp(InitialChaserSpawnCount, 0, Mathf.Max(0, MaxChaserEnemyCount));
		for (int i = 0; i < initialChaserCount; i++)
		{
			SpawnSingleChaserEnemyRandom();
		}

		SpawnObstacles();
	}

	public void SpawnSingleEnemyRandom()
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

		Node enemyNode = EnemyScene.Instantiate();
		if (enemyNode is Node2D enemy2D)
		{
			float halfX = Mathf.Max(24.0f, RandomSpawnHalfExtents.X - RandomSpawnEdgePadding);
			float halfY = Mathf.Max(24.0f, RandomSpawnHalfExtents.Y - RandomSpawnEdgePadding);
			enemy2D.Position = new Vector2(
				(float)GD.RandRange(-halfX, halfX),
				(float)GD.RandRange(-halfY, halfY)
			);
		}

		enemiesContainer.AddChild(enemyNode);
	}

	public void SpawnSingleChaserEnemyRandom()
	{
		PackedScene selectedChaserScene = PickRandomChaserScene();
		if (selectedChaserScene == null)
		{
			return;
		}

		Node enemiesContainer = ResolveNode(EnemiesContainerPath);
		if (enemiesContainer == null)
		{
			return;
		}

		if (CountChaserEnemies(enemiesContainer) >= Mathf.Max(0, MaxChaserEnemyCount))
		{
			return;
		}

		Node chaserNode = selectedChaserScene.Instantiate();

		if (chaserNode is Node2D chaser2D)
		{
			float halfX = Mathf.Max(24.0f, RandomSpawnHalfExtents.X - RandomSpawnEdgePadding);
			float halfY = Mathf.Max(24.0f, RandomSpawnHalfExtents.Y - RandomSpawnEdgePadding);
			chaser2D.Position = new Vector2(
				(float)GD.RandRange(-halfX, halfX),
				(float)GD.RandRange(-halfY, halfY)
			);
		}

		enemiesContainer.AddChild(chaserNode);
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
		_enemySpawnTimer = 0.0;
		_chaserSpawnTimer = 0.0;

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

	private void EnsureChaserScenesLoaded()
	{
		if (ChaserArcherScene == null)
		{
			ChaserArcherScene = GD.Load<PackedScene>("res://scenes/ChaserArcher.tscn");
		}

		if (ChaserLancerScene == null)
		{
			ChaserLancerScene = GD.Load<PackedScene>("res://scenes/ChaserLancer.tscn");
		}

		if (ChaserPawnScene == null)
		{
			ChaserPawnScene = GD.Load<PackedScene>("res://scenes/ChaserPawn.tscn");
		}
	}

	private PackedScene PickRandomChaserScene()
	{
		EnsureChaserScenesLoaded();

		var candidates = new List<PackedScene>(3);
		if (ChaserArcherScene != null)
		{
			candidates.Add(ChaserArcherScene);
		}

		if (ChaserLancerScene != null)
		{
			candidates.Add(ChaserLancerScene);
		}

		if (ChaserPawnScene != null)
		{
			candidates.Add(ChaserPawnScene);
		}

		if (candidates.Count == 0)
		{
			return ChaserEnemyScene;
		}

		int index = (int)(GD.Randi() % (uint)candidates.Count);
		return candidates[index];
	}

	private int CountChaserEnemies(Node enemiesContainer)
	{
		int count = 0;
		foreach (Node child in enemiesContainer.GetChildren())
		{
			if (child is ChaserEnemy)
			{
				count += 1;
			}
		}

		return count;
	}
}
