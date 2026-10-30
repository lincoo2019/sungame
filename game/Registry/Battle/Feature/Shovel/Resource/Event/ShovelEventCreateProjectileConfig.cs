using Godot;

[GlobalClass]
public partial class ShovelEventCreateProjectileConfig : ShovelEventConfig
{
	[Export(PropertyHint.None, "")]
	public int everyNum = 10;

	[Export(PropertyHint.None, "")]
	public string projecileName;

	[Export(PropertyHint.None, "")]
	public double projectilebaseDamage = 20.0;

	public override void Execute(TowerDefenseCharacter character)
	{
		Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
		double groundHeight = character.GetGroundHeight(logicalGlobalPosition.Y);
		for (int i = 0; i < (int)Mathf.Floor(character.cost / (double)everyNum); i++)
		{
			double num = GD.RandRange(-10.0, 40.0);
			TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(new StringName(projecileName));
			towerDefenseProjectileCreateData.baseDamage = projectilebaseDamage;
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = character.gridPos.Y
			};
			FireComponent.CreateProjectilePositionByData(character, null, groundHeight + num - 20.0, logicalGlobalPosition + new Vector2(GD.RandRange(-10, 10), -20f), new Vector2(300f, 0f), towerDefenseProjectileCreateData, -1, character.camp, default(Vector2), overrides);
		}
	}
}
