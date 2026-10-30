using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterEventCreateAddPacket : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";
}
