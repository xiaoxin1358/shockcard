using Godot;

[GlobalClass]
public partial class BuffCardCatalog : Resource
{
    [Export] public Godot.Collections.Array<BuffCardData> Cards = new();
}
