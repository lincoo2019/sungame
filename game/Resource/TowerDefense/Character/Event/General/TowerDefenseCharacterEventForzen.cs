using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventForzen : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double time = 8.0;

	[Export(PropertyHint.None, "")]
	public double iceSpeedDownTime = 15.0;

	[Export(PropertyHint.None, "")]
	public bool fliterShield;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, time, iceSpeedDownTime, fliterShield);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, time, iceSpeedDownTime, fliterShield);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, time, iceSpeedDownTime, fliterShield);
	}

	public override void Init(Dictionary valueDictionary)
	{
		time = valueDictionary.GetValueOrDefault("Time", Variant.From<double>(8.0)).AsDouble();
		iceSpeedDownTime = valueDictionary.GetValueOrDefault("IceSpeedDownTime", Variant.From<double>(15.0)).AsDouble();
		fliterShield = valueDictionary.GetValueOrDefault("FliterShield", Variant.From<bool>(false)).AsBool();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "Forzen",
			["Value"] = new Dictionary
			{
				["Time"] = time,
				["IceSpeedDownTime"] = iceSpeedDownTime,
				["FliterShield"] = fliterShield
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, double time = 8.0, double iceSpeedDownTime = 15.0, bool fliterShield = false)
	{
		if (!fliterShield || !target.HasShield())
		{
			TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen = new TowerDefenseCharacterBuffFrozen();
			towerDefenseCharacterBuffFrozen.time = time;
			towerDefenseCharacterBuffFrozen.iceSpeedDownTime = iceSpeedDownTime;
			target.buff.AddBuff(towerDefenseCharacterBuffFrozen);
		}
	}
}
