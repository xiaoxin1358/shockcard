using Godot;

public partial class EnergyBarUI : VBoxContainer
{
	private TextureProgressBar _energyBar;
	private EnergyManager _boundEnergyManager;

	public override void _Ready()
	{
		_energyBar = GetNodeOrNull<TextureProgressBar>("EnergyBar");
	}

	public void BindEnergyManager(EnergyManager manager)
	{
		if (manager == null || _energyBar == null)
		{
			return;
		}

		_boundEnergyManager = manager;
		_boundEnergyManager.Connect("EnergyChanged", Callable.From<float, float>(OnEnergyChanged));
		OnEnergyChanged(_boundEnergyManager.CurrentEnergy, _boundEnergyManager.MaxEnergy);
	}

	private void OnEnergyChanged(float currentEnergy, float maxEnergy)
	{
		if (_energyBar == null)
		{
			return;
		}

		_energyBar.MaxValue = maxEnergy;
		_energyBar.Value = currentEnergy;
	}
}
