using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TutorialConditionCheckCharaterNum : TutorialConditionConfig
{
	[Export(PropertyHint.None, "")]
	public string characterName = "";

	[Export(PropertyHint.Enum, ">,>=,==,<,<=")]
	public string method = ">";

	[Export(PropertyHint.None, "")]
	public int num = 1;

	public override void Init(Dictionary data)
	{
		base.Init(data);
		characterName = data.GetValueOrDefault("CharacterName", "").AsString();
		method = data.GetValueOrDefault("Method", ">").AsString();
		num = data.GetValueOrDefault("Num", 1).AsInt32();
	}

	public override bool Step()
	{
		Array characterFromName = TowerDefenseManager.Instance.GetCharacterFromName(characterName);
		return method switch
		{
			">" => characterFromName.Count > num, 
			">=" => characterFromName.Count >= num, 
			"==" => characterFromName.Count == num, 
			"<" => characterFromName.Count < num, 
			"<=" => characterFromName.Count <= num, 
			_ => false, 
		};
	}
}
