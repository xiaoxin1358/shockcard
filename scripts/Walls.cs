using Godot;

public partial class Walls : StaticBody2D
{
	[Export] public Vector2 MapSize = new Vector2(1000.0f, 1000.0f);
	[Export] public float WallBounce = 0.9f;
	[Export] public float WallFriction = 0.05f;
	[Export] public Color BorderColor = new Color(0.55f, 0.55f, 0.55f, 0.95f);
	[Export] public float BorderWidth = 3.0f;

	public override void _Ready()
	{
		EnsurePhysicsMaterial();
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 half = MapSize * 0.5f;
		Vector2 topLeft = new Vector2(-half.X, -half.Y);
		Vector2 topRight = new Vector2(half.X, -half.Y);
		Vector2 bottomRight = new Vector2(half.X, half.Y);
		Vector2 bottomLeft = new Vector2(-half.X, half.Y);

		DrawLine(topLeft, topRight, BorderColor, BorderWidth);
		DrawLine(topRight, bottomRight, BorderColor, BorderWidth);
		DrawLine(bottomRight, bottomLeft, BorderColor, BorderWidth);
		DrawLine(bottomLeft, topLeft, BorderColor, BorderWidth);
	}

	private void EnsurePhysicsMaterial()
	{
		PhysicsMaterial mat = PhysicsMaterialOverride;
		if (mat == null)
		{
			mat = new PhysicsMaterial();
		}

		mat.Bounce = Mathf.Max(0.8f, WallBounce);
		mat.Friction = Mathf.Max(0.0f, WallFriction);
		PhysicsMaterialOverride = mat;
	}
}
