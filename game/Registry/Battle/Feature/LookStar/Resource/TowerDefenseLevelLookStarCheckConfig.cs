using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelLookStarCheckConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	[Export(PropertyHint.None, "")]
	public Vector2I gridPos;

	public void Init(Dictionary data)
	{
		packetName = data.GetValueOrDefault("PacketName", "").AsString();
		Array array = data.GetValueOrDefault("GridPos", new Array { 0, 0 }).AsGodotArray();
		gridPos = ((array.Count >= 2) ? new Vector2I(array[0].AsInt32(), array[1].AsInt32()) : Vector2I.Zero);
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["PacketName"] = packetName,
			["GridPos"] = new Array { gridPos.X, gridPos.Y }
		};
	}
}
