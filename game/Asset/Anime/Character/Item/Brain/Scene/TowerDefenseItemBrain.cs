using Godot;

[Tool]
public partial class TowerDefenseItemBrain : TowerDefenseItem
{
	public delegate void BrainDestroyEventHandler(TowerDefenseItemBrain brain);

	public bool over;

	public event BrainDestroyEventHandler OnBrainDestroy;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.SetAlive(alive: false);
			}
			AddToGroup("Brain", persistent: true);
			targetRegistrationComponent.attackGridColumnAliasOffset = 1;
			targetRegistrationComponent.canProjectileCheck = false;
		}
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		this.OnBrainDestroy?.Invoke(this);
		if ((Global.IsEditor && Global.Instance.enterLevelMode == "DiyLevel") || Global.Instance.enterLevelMode == "LoadLevel" || Global.Instance.enterLevelMode == "OnlineLevel")
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		double num = GD.Randf();
		if (num <= 0.02)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_DIAMOND, logicalGlobalPosition, 70.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
			{
				towerDefenseGroundItemBase.gridPos = base.gridPos;
			}
		}
		else if (num < 0.2)
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase2 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, logicalGlobalPosition, 70.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase2))
			{
				towerDefenseGroundItemBase2.gridPos = base.gridPos;
			}
		}
		else
		{
			TowerDefenseGroundItemBase towerDefenseGroundItemBase3 = TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, logicalGlobalPosition, 70.0, new Vector2((float)GD.RandRange(-100.0, 100.0), -400f), 980.0);
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase3))
			{
				towerDefenseGroundItemBase3.gridPos = base.gridPos;
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}
}
