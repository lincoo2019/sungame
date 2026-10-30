using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventYBCreate : TowerDefenseCharacterEventBase
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
		num = valueDictionary.GetValueOrDefault("Num", 50.0).AsDouble();
		dieCreate = valueDictionary.GetValueOrDefault("DieCreate", false).AsBool();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "YBCreate",
			["Value"] = new Dictionary
			{
				["Num"] = num,
				["DieCreate"] = dieCreate
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, double _num = 10.0, bool _dieCreate = false, bool _collect = false)
	{
		if (!_dieCreate || target.IsDie())
		{
			target.YBCreate(target.GetLogicalGlobalPosition(), (int)_num, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0, _collect);
		}
	}
}
