using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelCatalogConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string catalogKey = "Adventure";

	[Export(PropertyHint.None, "")]
	public Array<LevelChapterConfig> chapterList = new Array<LevelChapterConfig>();

	public Dictionary ToRuntimeDictionary()
	{
		Array array = new Array();
		foreach (LevelChapterConfig chapter in chapterList)
		{
			if (GodotObject.IsInstanceValid(chapter))
			{
				array.Add(chapter.ToRuntimeDictionary());
			}
		}
		return new Dictionary { ["Chapter"] = array };
	}
}
