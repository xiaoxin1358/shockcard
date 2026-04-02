using Godot;

public partial class PlayerDebugVisual : Node2D
{
	[Export] public float Radius = 20.0f;
	[Export] public Color FillColor = new Color(0.24f, 0.74f, 0.96f, 1.0f);
	[Export] public Color StrokeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	[Export] public float StrokeWidth = 2.0f;

	public override void _Draw()
	{
		DrawCircle(Vector2.Zero, Radius, FillColor);
		DrawArc(Vector2.Zero, Radius, 0.0f, Mathf.Tau, 48, StrokeColor, StrokeWidth);
	}
}