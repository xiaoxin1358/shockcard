using Godot;

public enum BuffEffectType
{
    None = 0,
    OverloadPropulsion = 1,
    EnergyBattery = 2,
    EnergyRecoveryOnKill = 3,
    ChainShockwaveOnKill = 4,
    LowFrictionSurface = 5
}

[GlobalClass]
public partial class BuffCardData : Resource
{
    [Export] public string CardId = string.Empty;
    [Export] public string DisplayName = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description = string.Empty;
    [Export] public BuffEffectType EffectType = BuffEffectType.None;
    [Export] public float ValueA = 0.0f;
    [Export] public float ValueB = 0.0f;
}
