using Godot;

[GlobalClass]
public partial class TowerDefenseMapRevealShaderTime : TowerDefenseMapShaderTime
{
	private static readonly string[] ConventionalRevealNodes = new string[5] { "FrontlawnFloor", "FrontlawnDoor", "BackyardFloor", "BackyardDoor", "UndeadVillageDoor" };

	public TowerDefenseMapRevealShaderTime()
	{
		ConfigureEnterRoomRevealNodes(ConventionalRevealNodes);
	}
}
