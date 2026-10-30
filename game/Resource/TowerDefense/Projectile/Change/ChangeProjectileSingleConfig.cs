using Godot;

[GlobalClass]
public partial class ChangeProjectileSingleConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData projectileData;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileConfig projectileConfig;

	[Export(PropertyHint.None, "")]
	public string changeAudio;

	public string projectileName
	{
		get
		{
			if (GodotObject.IsInstanceValid(projectileData))
			{
				return projectileData.projectileName.ToString();
			}
			return "";
		}
	}
}
