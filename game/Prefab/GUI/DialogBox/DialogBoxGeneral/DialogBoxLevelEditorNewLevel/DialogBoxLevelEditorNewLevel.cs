using Godot;

public partial class DialogBoxLevelEditorNewLevel : DialogPopup
{
	public delegate void CreateLevelEventHandler(TowerDefenseLevelConfig level);

	public event CreateLevelEventHandler OnCreateLevel;

	public override void _Ready()
	{
		base._Ready();
		GetNode<BaseButton>("%NormalButton").Pressed += NormalButtonPressed;
		GetNode<BaseButton>("%ConveyorButton").Pressed += ConveyorButtonPressed;
		GetNode<BaseButton>("%CancleButton").Pressed += CancleButtonPressed;
	}

	public void NormalButtonPressed()
	{
		TowerDefenseLevelConfig level = new TowerDefenseLevelConfig();
		this.OnCreateLevel?.Invoke(level);
		CloseDialog();
	}

	public void ConveyorButtonPressed()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig();
		towerDefenseLevelConfig.ConveyorPreset();
		this.OnCreateLevel?.Invoke(towerDefenseLevelConfig);
		CloseDialog();
	}

	public void CancleButtonPressed()
	{
		CloseDialog();
	}
}
