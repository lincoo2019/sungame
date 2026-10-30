using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureBrainConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packetName = "ItemBrain";

	[Export(PropertyHint.Range, "-200,200,1")]
	public double horizontalOffset = 10.0;

	[Export(PropertyHint.None, "")]
	public bool characterFilter = true;

	public void Init(Dictionary data)
	{
		if (data == null)
		{
			data = new Dictionary();
		}
		packetName = data.GetValueOrDefault("PacketName", "ItemBrain").AsString();
		if (string.IsNullOrWhiteSpace(packetName))
		{
			packetName = "ItemBrain";
		}
		double num = data.GetValueOrDefault("HorizontalOffset", 10.0).AsDouble();
		horizontalOffset = (double.IsFinite(num) ? Math.Clamp(num, -200.0, 200.0) : 10.0);
		characterFilter = data.GetValueOrDefault("CharacterFilter", true).AsBool();
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["PacketName"] = packetName,
			["HorizontalOffset"] = horizontalOffset,
			["CharacterFilter"] = characterFilter
		};
	}
}
