using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ShovelConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Texture2D texture;

	[Export(PropertyHint.None, "")]
	public string saveKey;

	[Export(PropertyHint.None, "")]
	public Array<UnlockConditionBaseConfig> unlockCheckList = new Array<UnlockConditionBaseConfig>();

	[Export(PropertyHint.None, "")]
	public string name;

	[Export(PropertyHint.None, "")]
	public string describe;

	[Export(PropertyHint.None, "")]
	public string handbookDescribe;

	[Export(PropertyHint.None, "")]
	public string handbookStory;

	[Export(PropertyHint.None, "")]
	public Array<ShovelEventConfig> eventList = new Array<ShovelEventConfig>();

	[Export(PropertyHint.None, "")]
	public Array<string> shovelableNames = new Array<string>();

	public void Execute(TowerDefenseCharacter character)
	{
		foreach (ShovelEventConfig @event in eventList)
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
		GlobalFeatureManager instance = GlobalFeatureManager.Instance;
		if (instance == null || !instance.IsRegistered(saveKey))
		{
			return false;
		}
		if (!instance.IsUnlocked(saveKey))
		{
			if (unlockCheckList.Count <= 0)
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
			instance.Unlock(saveKey);
		}
		return true;
	}
}
