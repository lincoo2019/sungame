using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventGarlic : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public bool fliterCar = true;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float probability = 1f;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, fliterCar, probability);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, fliterCar, probability);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, fliterCar, probability);
	}

	public override void Init(Dictionary valueDictionary)
	{
		fliterCar = valueDictionary.GetValueOrDefault("FliterCar", Variant.From<bool>(false)).AsBool();
		probability = valueDictionary.GetValueOrDefault("Probability", Variant.From<float>(1f)).AsSingle();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "Garlic",
			["Value"] = new Dictionary
			{
				["FliterCar"] = fliterCar,
				["Probability"] = probability
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, bool fliterCar = true, float probability = 1f)
	{
		if ((!(probability < 1f) || !(GD.Randf() > probability)) && (!fliterCar || target.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR))
		{
			target.Garlic();
		}
	}
}
