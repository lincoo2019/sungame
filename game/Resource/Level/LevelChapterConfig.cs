using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelChapterConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string chapterName = "新章节";

	[Export(PropertyHint.None, "")]
	public string openKey = "";

	[Export(PropertyHint.None, "")]
	public Texture2D unlockImage;

	[Export(PropertyHint.None, "")]
	public Texture2D lockImage;

	[Export(PropertyHint.None, "")]
	public Texture2D background;

	[Export(PropertyHint.None, "")]
	public Texture2D building;

	[Export(PropertyHint.Range, "0,20,1")]
	public int preOpen;

	[Export(PropertyHint.None, "")]
	public bool locked;

	[Export(PropertyHint.None, "")]
	public Array<LevelChooseConfig> levelList = new Array<LevelChooseConfig>();

	public Dictionary ToRuntimeDictionary()
	{
		Array array = new Array();
		foreach (LevelChooseConfig level in levelList)
		{
			if (GodotObject.IsInstanceValid(level))
			{
				array.Add(level.ToRuntimeDictionary());
			}
		}
		return new Dictionary
		{
			["Name"] = chapterName ?? "",
			["OpenKey"] = openKey ?? "",
			["UnlockImage"] = LevelChooseConfig.ResourceReference(unlockImage),
			["LockImage"] = LevelChooseConfig.ResourceReference(lockImage),
			["Background"] = LevelChooseConfig.ResourceReference(background),
			["Building"] = LevelChooseConfig.ResourceReference(building),
			["PreOpen"] = preOpen,
			["Lock"] = locked,
			["Level"] = array
		};
	}
}
