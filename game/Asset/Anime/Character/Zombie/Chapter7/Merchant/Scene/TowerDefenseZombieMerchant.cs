using Godot;

[Tool]
public partial class TowerDefenseZombieMerchant : TowerDefenseZombie
{
	[Export(PropertyHint.None, "")]
	public TowerDefensePacketChangeCost changeCost;

	public override async void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater");
			changeCost = changeCost.Duplicate() as TowerDefensePacketChangeCost;
			OnHypnosisStateChanged();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (TowerDefenseManager.Instance.IsGameRunning())
			{
				TowerDefenseManager.Instance.ChangeCostAdd(changeCost);
			}
		}
	}

	public override void PreSpawn()
	{
		base.PreSpawn();
		TowerDefenseManager.Instance.ChangeCostAdd(changeCost);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!Engine.IsEditorHint())
		{
			TowerDefenseManager.Instance.ChangeCostRemove(changeCost);
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		bool flag = TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode();
		changeCost.method = ((instance.hypnoses != flag) ? "Decrease" : "Increase");
	}
}
