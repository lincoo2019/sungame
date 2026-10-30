using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventBase : Resource
{
	public virtual void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
	}

	public virtual void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
	}

	public virtual void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
	}

	public virtual void ExecuteGroundProject(TowerDefenseProjectile projectile, Vector2 position, Vector2I gridPos)
	{
	}

	public virtual void Init(Dictionary valueDictionary)
	{
	}

	public virtual Dictionary Export()
	{
		return new Dictionary();
	}
}
