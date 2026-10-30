using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventHypnoses : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double time = -1.0;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, time);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, time);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, time);
	}

	public override void Init(Dictionary valueDictionary)
	{
		time = valueDictionary.GetValueOrDefault("Time", Variant.From<double>(-1.0)).AsDouble();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "Hypnoses",
			["Value"] = new Dictionary { ["Time"] = time }
		};
	}

	public static void Run(TowerDefenseCharacter target, double time = -1.0)
	{
		target.Hypnoses(time);
	}
}
