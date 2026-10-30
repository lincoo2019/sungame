using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseProjectileEffectPeaCobCannonExplode : TowerDefenseProjectileEffectCherryPea
{
	public override string NetworkEffectId => "pea_cob_cannon_burst";

	public override void _Ready()
	{
		if (PrepareNetworkEffect())
		{
			TowerDefenseExplode.CreateExplode(base.GlobalPosition, new Vector2(1.5f, 1.5f), base.Eventlist, new Array<TowerDefenseCharacter>(), camp, -1);
			AttackCreateAsync();
		}
	}
}
