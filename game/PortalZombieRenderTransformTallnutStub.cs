using Godot;

public partial class PortalZombieRenderTransformTallnutStub : TowerDefensePlantPorTallnut
{
	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Disabled;
		instance = new TowerDefenseCharacterInstance
		{
			canBeCollection = false,
			hypnoses = false
		};
		base.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		open = true;
		AddToGroup("PorTallnut");
	}
}
