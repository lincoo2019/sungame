using Godot;
using Godot.Collections;

[GlobalClass]
public partial class UnlockConditionLevelFinishConfig : UnlockConditionBaseConfig
{
	[Export(PropertyHint.None, "")]
	public string levelSaveKey = "";

	public override bool Check()
	{
		if (levelSaveKey == "Unlock")
		{
			return true;
		}
		return GameSaveManager.Instance.GetLevelValue(levelSaveKey).GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
			.GetValueOrDefault("Finish", 0.0)
			.AsDouble() > 0.0;
	}
}
