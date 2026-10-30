using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantDisguiserBlover : TowerDefensePlant
{
	private static PackedScene _BLOW;

	public double hpNext;

	public double hpNextInterval;

	public bool over;

	private static PackedScene BLOW => _BLOW ?? (_BLOW = GD.Load<PackedScene>("uid://j7kll4h65yex"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			hpNextInterval = instance.hitpoints / 6.0;
			hpNext = instance.hitpoints - hpNextInterval;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			while (instance.hitpoints <= hpNext)
			{
				hpNext -= hpNextInterval;
				BlowMethod();
			}
		}
	}

	public override void DestroySet()
	{
		if (!isShovel && !over)
		{
			over = true;
			double num = 0.0;
			while (hpNext >= 0.0)
			{
				hpNext -= hpNextInterval;
				num += 1.0;
			}
			BlowMethod(num);
		}
	}

	public void BlowMethod(double blowScale = 1.0)
	{
		BattleEventBus.Instance.EmitBlowAllEffectEmit();
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(BLOW, base.gridPos, "Idle");
		towerDefenseEffectSpriteOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
		foreach (TowerDefenseCharacter item in TowerDefenseManager.Instance.GetCharacterTarget(this))
		{
			if (item is TowerDefenseZombie && item.instance.zombiePhysique >= TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE)
			{
				item.BlowBack(0.25 * blowScale, 0.25);
			}
			else if (item.instance.unUseBuffFlags != -1 && (item.instance.unUseBuffFlags & 0x200) == 0 && !(item is TowerDefensePlant) && !(item is TowerDefenseGravestone) && !(item is TowerDefenseItem) && (item.instance.collisionFlags & 0x10) == 0 && (item.instance.collisionFlags & 0x20) == 0)
			{
				if (item.instance.collisionFlags != 0)
				{
					item.BlowBack(0.5 * blowScale, 0.25);
				}
				if ((item.instance.collisionFlags & 2) != 0)
				{
					item.Blow();
				}
			}
		}
		AudioManager.Instance.AudioPlay("Blover");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "hpNext", hpNext },
			{ "hpNextInterval", hpNextInterval },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hpNext = data.GetValueOrDefault("hpNext", Variant.From<double>(0.0)).AsDouble();
		hpNextInterval = data.GetValueOrDefault("hpNextInterval", Variant.From<double>(0.0)).AsDouble();
		over = data.GetValueOrDefault("over", Variant.From<bool>(false)).AsBool();
	}
}
