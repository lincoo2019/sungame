using Godot;

[Tool]
public partial class TowerDefensePlantCardless : TowerDefensePlant
{
	[Export(PropertyHint.None, "")]
	public TowerDefensePacketChangeCost changeCost;

	public override async void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			instance.invincible = true;
			instance.canBeCollection = false;
			changeCost = changeCost.Duplicate() as TowerDefensePacketChangeCost;
			OnHypnosisStateChanged();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			while (GodotObject.IsInstanceValid(this) && IsInsideTree() && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && !TowerDefenseManager.Instance.IsGameRunning())
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			if (GodotObject.IsInstanceValid(this) && IsInsideTree())
			{
				EnsureChangeCostRegistered();
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!Engine.IsEditorHint())
		{
			TowerDefenseManager.Instance.ChangeCostRemove(changeCost);
		}
	}

	public override void PreSpawn()
	{
		base.PreSpawn();
		EnsureChangeCostRegistered();
	}

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
		instance.canBeCollection = true;
	}

	public override void SleepExited()
	{
		base.SleepExited();
		instance.invincible = true;
		instance.canBeCollection = false;
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		bool flag = TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode();
		changeCost.method = ((instance.hypnoses != flag) ? "Increase" : "Decrease");
		instance.invincible = true;
		instance.canBeCollection = false;
	}

	private void EnsureChangeCostRegistered()
	{
		if (GodotObject.IsInstanceValid(changeCost) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && !TowerDefenseManager.GetChangeCostList().Contains(changeCost))
		{
			TowerDefenseManager.Instance.ChangeCostAdd(changeCost);
		}
	}
}
