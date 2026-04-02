using Godot;

public partial class EnergyManager : Node
{
    [Signal]
    public delegate void EnergyChangedEventHandler(float currentEnergy, float maxEnergy);

    [Export] public float MaxEnergy = 500.0f;
    [Export] public float StartEnergy = 500.0f;
    [Export] public float EnemyKillRestoreAmount = 50.0f;
    [Export] public float SettlementRestoreAmount = 35.0f;

    public float CurrentEnergy { get; private set; }

    public override void _EnterTree()
    {
        AddToGroup("energy_manager");
    }

    public override void _Ready()
    {
        CurrentEnergy = Mathf.Clamp(StartEnergy, 0.0f, MaxEnergy);
        EmitEnergyChanged();
    }

    public bool TryConsume(float amount)
    {
        float clampedAmount = Mathf.Max(0.0f, amount);
        if (CurrentEnergy < clampedAmount)
        {
            return false;
        }

        CurrentEnergy = Mathf.Clamp(CurrentEnergy - clampedAmount, 0.0f, MaxEnergy);
        EmitEnergyChanged();
        return true;
    }

    public void Restore(float amount)
    {
        float clampedAmount = Mathf.Max(0.0f, amount);
        if (clampedAmount <= 0.0f)
        {
            return;
        }

        CurrentEnergy = Mathf.Clamp(CurrentEnergy + clampedAmount, 0.0f, MaxEnergy);
        EmitEnergyChanged();
    }

    public void RestoreOnEnemyKill()
    {
        Restore(EnemyKillRestoreAmount);
    }

    public void RestoreOnSettlement()
    {
        Restore(SettlementRestoreAmount);
    }

    public void SetEnergy(float value)
    {
        CurrentEnergy = Mathf.Clamp(value, 0.0f, MaxEnergy);
        EmitEnergyChanged();
    }

    private void EmitEnergyChanged()
    {
        EmitSignal(SignalName.EnergyChanged, CurrentEnergy, MaxEnergy);
    }
}
