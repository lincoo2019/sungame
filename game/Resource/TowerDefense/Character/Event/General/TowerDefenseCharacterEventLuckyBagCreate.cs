using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventLuckyBagCreate : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public bool dieCreate;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target, dieCreate);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target, dieCreate);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target, dieCreate);
	}

	public override void Init(Dictionary valueDictionary)
	{
		dieCreate = valueDictionary.GetValueOrDefault("DieCreate", Variant.From<bool>(false)).AsBool();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "LuckyBagCreate",
			["Value"] = new Dictionary { ["DieCreate"] = dieCreate }
		};
	}

	public static void Run(TowerDefenseCharacter target, bool dieCreate = false)
	{
		if (!dieCreate || target.IsDie())
		{
			target.LuckyBagCreate(target.GetLogicalGlobalPosition(), new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
		}
	}
}
