using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class MowerConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Texture2D texture;

	[Export(PropertyHint.None, "")]
	public PackedScene sprite;

	[Export(PropertyHint.None, "")]
	public string saveKey;

	[Export(PropertyHint.None, "")]
	public Array<UnlockConditionBaseConfig> unlockCheckList;

	[Export(PropertyHint.None, "")]
	public string name;

	[Export(PropertyHint.None, "")]
	public string describe;

	[Export(PropertyHint.None, "")]
	public string handbookDescribe;

	[Export(PropertyHint.None, "")]
	public string handbookStory;

	[Export(PropertyHint.None, "")]
	public Array<MowerEventConfig> eventList;

	public void Execute(TowerDefenseCharacter character)
	{
		foreach (MowerEventConfig @event in eventList)
		{
			@event.Execute(character);
		}
	}

	public bool Unlock()
	{
		if (CommandManager.Instance.debugPacketOpenAll)
		{
			return true;
		}
		if (GameSaveManager.Instance.GetFeatureValue(saveKey) <= 0)
		{
			if (unlockCheckList == null || unlockCheckList.Count <= 0)
			{
				return false;
			}
			foreach (UnlockConditionBaseConfig unlockCheck in unlockCheckList)
			{
				if (!unlockCheck.Check())
				{
					return false;
				}
			}
			GameSaveManager.Instance.SetFeatureValue(saveKey, true);
			GameSaveManager.Instance.Save();
		}
		return true;
	}
}
