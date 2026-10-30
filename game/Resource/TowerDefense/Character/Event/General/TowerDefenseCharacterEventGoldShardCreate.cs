using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventGoldShardCreate : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public bool dieCreate;

	[Export(PropertyHint.None, "")]
	public int num;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, dieCreate, num);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, dieCreate, num);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, dieCreate, num);
	}

	public override void Init(Dictionary valueDictionary)
	{
		dieCreate = valueDictionary.GetValueOrDefault("DieCreate", Variant.From<bool>(false)).AsBool();
		num = valueDictionary.GetValueOrDefault("Num", Variant.From<int>(0)).AsInt32();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "GoldShardCreate",
			["Value"] = new Dictionary
			{
				["DieCreate"] = dieCreate,
				["Num"] = num
			}
		};
	}

	public static void Run(TowerDefenseCharacter target, bool dieCreate = false, int num = 0)
	{
		if (!dieCreate || target.IsDie())
		{
			for (int i = 0; i < num; i++)
			{
				target.GoldShardCreate(target.GetLogicalGlobalPosition(), new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
		}
	}
}
