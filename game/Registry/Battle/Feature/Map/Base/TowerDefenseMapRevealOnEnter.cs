using Godot;

[GlobalClass]
public partial class TowerDefenseMapRevealOnEnter : TowerDefenseMap
{
	private static readonly string[] ConventionalRevealNodes = new string[5] { "FrontlawnFloor", "FrontlawnDoor", "BackyardFloor", "BackyardDoor", "UndeadVillageDoor" };

	public TowerDefenseMapRevealOnEnter()
	{
		ConfigureEnterRoomRevealNodes(ConventionalRevealNodes);
	}
}
