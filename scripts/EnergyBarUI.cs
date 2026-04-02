using Godot;

public partial class EnergyBarUI : VBoxContainer
{
	private TextureProgressBar _energyBar;
	private Label _energyValue;
	private EnergyManager _boundEnergyManager;

	public override void _Ready()
	{
		_energyBar = GetNodeOrNull<TextureProgressBar>("EnergyBar");
		_energyValue = GetNodeOrNull<Label>("EnergyValue");
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

		if (_energyValue != null)
		{
			_energyValue.Text = Mathf.RoundToInt(currentEnergy) + " / " + Mathf.RoundToInt(maxEnergy);
		}
	}
}
