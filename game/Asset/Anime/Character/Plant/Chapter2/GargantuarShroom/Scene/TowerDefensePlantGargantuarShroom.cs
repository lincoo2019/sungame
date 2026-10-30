using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantGargantuarShroom : TowerDefensePlant
{
	public bool over;

	public override void AttackDeal(TowerDefenseCharacter character, string type, double num)
	{
		base.AttackDeal(character, type, num);
		if (instance.sleep || over || !GodotObject.IsInstanceValid(character))
		{
			return;
		}
		if ((character.instance.unUseBuffFlags & 8) != 0)
		{
			SkipInvincibleHurt(num);
		}
		else
		{
			if (!(type == "Eat"))
			{
				return;
			}
			over = true;
			if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
			{
				character.skipDestroySet = true;
				character.Destroy();
				Destroy();
				return;
			}
			Vector2 logicalGlobalPosition = character.GetLogicalGlobalPosition();
			character.skipDestroySet = true;
			character.Destroy();
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGargantuarRedEyes");
			TowerDefenseZombie zombie = (TowerDefenseZombie)packetConfig.Create(logicalGlobalPosition, character.gridPos);
			TowerDefenseManager.GetCharacterNode().AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
			if (!instance.hypnoses)
			{
				zombie.Hypnoses();
			}
			zombie.SetMainStateMachineDispatchEnabled(enabled: false);
			Tween tween = zombie.CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Back);
			tween.TweenProperty(zombie.transformPoint, "scale", Vector2.One, 0.5).From(Vector2.One * 0.5f);
			tween.Finished += delegate
			{
				if (GodotObject.IsInstanceValid(zombie))
				{
					zombie.Walk();
					zombie.SetMainStateMachineDispatchEnabled(enabled: true);
				}
			};
			if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
			{
				int nextSyncId = TowerDefenseManager.Instance.currentControl.GetNextSyncId();
				TowerDefenseManager.Instance.currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGargantuarRedEyes", character.gridPos.X, character.gridPos.Y, nextSyncId, 1.0, 1.0, !instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y);
			}
			Destroy();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "over", over } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
