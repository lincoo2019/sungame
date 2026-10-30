using Godot;
using Godot.Collections;

[GlobalClass]
public partial class GameConfigSaveConfigCSharp : Resource
{
	[Export(PropertyHint.None, "")]
	public Dictionary saveDictionary = new Dictionary();

	public void _Init()
	{
		saveDictionary = new Dictionary();
	}

	public void Init()
	{
		Dictionary dictionary = GameSaveManager.CONFIG_INIT.Data.AsGodotDictionary();
		foreach (Variant key in dictionary.Keys)
		{
			saveDictionary[key] = dictionary[key];
		}
	}
}
