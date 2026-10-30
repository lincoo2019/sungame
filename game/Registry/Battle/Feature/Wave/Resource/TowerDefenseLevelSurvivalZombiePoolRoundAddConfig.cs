using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelSurvivalZombiePoolRoundAddConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public int round;

	[Export(PropertyHint.None, "")]
	public Array zombieList = new Array();

	public void Init(Dictionary data)
	{
		round = data.GetValueOrDefault("Round", 0).AsInt32();
		zombieList = (data.ContainsKey("Zombie") ? ((Array)data["Zombie"]) : new Array());
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["Round"] = round,
			["Zombie"] = zombieList
		};
	}
}
