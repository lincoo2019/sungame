using Godot;

[GlobalClass]
public partial class TowerDefenseLevelBaseConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string name = "";

	[Export(PropertyHint.None, "")]
	public string description = "关卡描述";

	[Export(PropertyHint.None, "")]
	public string levelName = "关卡名";

	[Export(PropertyHint.None, "")]
	public int levelNumber = 1;

	[Export(PropertyHint.None, "")]
	public string nextLevel = "";

	[Export(PropertyHint.None, "")]
	public GeneralEnum.HOMEWORLD homeWorld;

	public virtual void Init()
	{
	}
}
