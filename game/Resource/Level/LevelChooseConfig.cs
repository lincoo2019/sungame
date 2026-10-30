using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelChooseConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string openKey = "";

	[Export(PropertyHint.None, "")]
	public string saveKey = "";

	[Export(PropertyHint.None, "")]
	public Texture2D unlockImage;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelBaseConfig normalLevel;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelBaseConfig difficultLevel;

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelBaseConfig ultimateLevel;

	public Dictionary ToRuntimeDictionary()
	{
		return new Dictionary
		{
			["OpenKey"] = openKey ?? "",
			["SaveKey"] = saveKey ?? "",
			["UnlockImage"] = ResourceReference(unlockImage),
			["Level"] = new Dictionary
			{
				["Normal"] = ResourceReference(normalLevel),
				["Difficult"] = ResourceReference(difficultLevel),
				["Ultimate"] = ResourceReference(ultimateLevel)
			}
		};
	}

	internal static string ResourceReference(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "";
		}
		return resource.ResourcePath ?? "";
	}
}
