using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterPropertyChangeConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string propertyName = "";

	[Export(PropertyHint.None, "")]
	public Variant value;

	public void Init(Dictionary data)
	{
		propertyName = data.GetValueOrDefault("PropertyName", "").AsString();
		value = data.GetValueOrDefault("Value");
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["PropertyName"] = propertyName,
			["Value"] = value
		};
	}

	public void Execute(TowerDefenseCharacter character)
	{
		character.Set(propertyName, value);
	}
}
