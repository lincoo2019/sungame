using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterEventGarlicFireHit : TowerDefenseCharacterEventBase
{
	private const double PoisonDamage = 5.0;

	private bool _runtimeMagicDamage;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		ApplyPoisonDamage(target, _runtimeMagicDamage);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		if ((!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && GodotObject.IsInstanceValid(target))
		{
			Vector2 logicalGlobalPosition = target.GetLogicalGlobalPosition();
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			Vector2 mapCellPosCenter = instance.GetMapCellPosCenter(instance.GetMapGridPos(logicalGlobalPosition));
			Vector2 pos = new Vector2(logicalGlobalPosition.X, mapCellPosCenter.Y);
			bool flag = projectile != null && TowerDefenseCharacterInstance.IsMagicDamage(projectile.damageFlags);
			ApplyPoisonDamage(target, flag);
			TowerDefenseCharacterEventBase item = (flag ? new TowerDefenseCharacterEventGarlicFireHit
			{
				_runtimeMagicDamage = true
			} : this);
			TowerDefenseExplode.CreateExplode(eventList: new Array<TowerDefenseCharacterEventBase> { item }, exclude: new Array<TowerDefenseCharacter> { target }, pos: pos, size: projectile.config.rangeSize, camp: projectile.camp, collisionFlags: projectile.collisionFlags);
		}
	}

	private void ApplyPoisonDamage(TowerDefenseCharacter target, bool magicDamage)
	{
		if ((!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && GodotObject.IsInstanceValid(target) && GodotObject.IsInstanceValid(target.instance) && !target.instance.die && (target.instance.unUseBuffFlags & 0x40) == 0)
		{
			target.instance.DealHurt(GetPoisonDamage(target, magicDamage), playSplatAudio: false);
			ShowHealthComponent showHealthComponent = target.showHealthComponent;
			if (showHealthComponent != null && !showHealthComponent.IsReleased)
			{
				target.showHealthComponent.MarkDirty();
			}
		}
	}

	private static double GetPoisonDamage(TowerDefenseCharacter target, bool magicDamage)
	{
		if (!magicDamage || target.instance.IsMagicPhysique)
		{
			return 5.0;
		}
		return 7.5;
	}
}
