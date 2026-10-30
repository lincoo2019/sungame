using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventPurify : TowerDefenseCharacterEventBase
{
	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(target);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(target);
	}

	public override void Init(Dictionary valueDictionary)
	{
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "Purify",
			["Value"] = new Dictionary()
		};
	}

	public static void Run(TowerDefenseCharacter target)
	{
		TowerDefenseCellInstance cell = target.cell;
		if (GodotObject.IsInstanceValid(cell) && !cell.TryBeginPurify())
		{
			target.Destroy();
			return;
		}
		target.Purify();
		if (GodotObject.IsInstanceValid(cell) && !target.isDestroy)
		{
			cell.CancelPurify();
		}
	}
}
