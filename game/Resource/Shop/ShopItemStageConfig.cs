using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ShopItemStageConfig : Resource
{
	[ExportGroup("Base", "")]
	[Export(PropertyHint.Enum, "Feature,TowerDefensePacket")]
	public string saveType = "Feature";

	[Export(PropertyHint.None, "")]
	public string saveKey = "";

	[Export(PropertyHint.None, "")]
	public Texture2D texture;

	[Export(PropertyHint.None, "")]
	public string describe;

	[Export(PropertyHint.None, "")]
	public string npcTalk;

	[ExportGroup("Init", "")]
	[Export(PropertyHint.Enum, "Coin")]
	public string type = "Coin";

	[Export(PropertyHint.None, "")]
	public int cost;

	[Export(PropertyHint.None, "")]
	public int openMinNum;

	[Export(PropertyHint.None, "")]
	public int openMaxNum = 1;

	[Export(PropertyHint.None, "")]
	public int addNum = 1;

	public void Init(Dictionary data)
	{
		saveType = data.GetValueOrDefault("SaveType", "Feature").AsString();
		saveKey = data.GetValueOrDefault("SaveKey", "").AsString();
		string text = data.GetValueOrDefault("Texture", "").AsString();
		if (text != "")
		{
			texture = GD.Load<Texture2D>(text);
		}
		describe = data.GetValueOrDefault("Describe", "").AsString();
		npcTalk = data.GetValueOrDefault("NpcTalk", "").AsString();
		cost = data.GetValueOrDefault("Cost", 0).AsInt32();
		openMinNum = data.GetValueOrDefault("OpenMinNum", 0).AsInt32();
		openMaxNum = data.GetValueOrDefault("OpenMaxNum", 1).AsInt32();
		addNum = data.GetValueOrDefault("AddNum", 1).AsInt32();
	}
}
