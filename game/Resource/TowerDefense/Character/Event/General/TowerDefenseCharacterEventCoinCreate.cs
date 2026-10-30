using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventCoinCreate : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double num = 10.0;

	[Export(PropertyHint.None, "")]
	public bool dieCreate;

	[Export(PropertyHint.None, "")]
	public bool collect;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, num, dieCreate, collect);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, num * delta, dieCreate, collect);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, num, dieCreate, collect);
	}

	public override void Init(Dictionary valueDictionary)
	{
		num = valueDictionary.GetValueOrDefault("Num", Variant.From<double>(50.0)).AsDouble();
		dieCreate = valueDictionary.GetValueOrDefault("DieCreate", Variant.From<bool>(false)).AsBool();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "CoinCreate",
			["Value"] = new Dictionary
			{
				["Num"] = num,
				["DieCreate"] = dieCreate
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, double num = 10.0, bool dieCreate = false, bool collect = false)
	{
		if (!dieCreate || target.IsDie())
		{
			target.CoinCreate(target.GetLogicalGlobalPosition(), (int)num, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0, collect);
		}
	}
}
