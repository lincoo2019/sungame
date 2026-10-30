using Godot;

public partial class FirePeaPortalExitZombiePortalStub : TowerDefenseZombiePortal
{
	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Disabled;
	}
}
