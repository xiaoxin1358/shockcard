using Godot;

public partial class DragIndicatorUI : Line2D
{
	[Export] public float MaxReferenceLength = 220.0f;
	[Export] public float MinWidth = 2.0f;
	[Export] public float MaxWidth = 8.0f;
	[Export] public float MinAlpha = 0.35f;
	[Export] public float MaxAlpha = 1.0f;

	public override void _Process(double delta)
	{
		if (Points.Length < 2)
		{
			return;
		}

		float len = Points[0].DistanceTo(Points[1]);
		float t = Mathf.Clamp(len / Mathf.Max(1.0f, MaxReferenceLength), 0.0f, 1.0f);

		Width = Mathf.Lerp(MinWidth, MaxWidth, t);
		Color color = DefaultColor;
		color.A = Mathf.Lerp(MinAlpha, MaxAlpha, t);
		DefaultColor = color;
	}
}
