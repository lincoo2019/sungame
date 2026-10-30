using System.Collections.Generic;
using Godot;

[Tool]
public partial class TowerDefensePlantDoomShroomGar : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplodeStart += ExplodeStart;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplodeStart -= ExplodeStart;
		}
	}

	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}

	public void ExplodeStart()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent == null || explodeComponent.IsReleased || !GodotObject.IsInstanceValid(instance) || TowerDefenseManager.Instance == null)
		{
			return;
		}
		Vector2 size = TowerDefenseManager.Instance.GetMapGridSize() * 2f * _explodeComponent.explodeRange;
		Rect2 checkRect = AabbShapeUtil.RectFromCenter(GetLogicalGlobalPosition(), size);
		List<TowerDefenseCharacter> charactersIntersectingRectListExcludingCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(checkRect, base.camp);
		Dictionary<int, TowerDefenseCharacter> dictionary = new Dictionary<int, TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item in charactersIntersectingRectListExcludingCamp)
		{
			if (!GodotObject.IsInstanceValid(item) || item.die || item.nearDie || item.camp == base.camp || !(item is TowerDefenseZombie) || !GodotObject.IsInstanceValid(item.instance) || item.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				continue;
			}
			int y = item.gridPos.Y;
			if (!dictionary.TryGetValue(y, out var value))
			{
				dictionary[y] = item;
				continue;
			}
			float x = item.GetLogicalGlobalPosition().X;
			float x2 = value.GetLogicalGlobalPosition().X;
			if (instance.hypnoses ? (x > x2) : (x < x2))
			{
				dictionary[y] = item;
			}
		}
		int num = 0;
		foreach (KeyValuePair<int, TowerDefenseCharacter> item2 in dictionary)
		{
			item2.Deconstruct(out var _, out var value2);
			TowerDefenseCharacter towerDefenseCharacter = value2;
			if (num >= 5)
			{
				break;
			}
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				continue;
			}
			Vector2 logicalGlobalPosition = towerDefenseCharacter.GetLogicalGlobalPosition();
			towerDefenseCharacter.skipDestroySet = true;
			towerDefenseCharacter.Destroy();
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGargantuarRedEyes");
			TowerDefenseZombie zombie = (TowerDefenseZombie)packetConfig.Create(logicalGlobalPosition, towerDefenseCharacter.gridPos);
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
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				int nextSyncId = TowerDefenseManager.Instance.currentControl.GetNextSyncId();
				TowerDefenseManager.Instance.currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGargantuarRedEyes", towerDefenseCharacter.gridPos.X, towerDefenseCharacter.gridPos.Y, nextSyncId, 1.0, 1.0, !instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y);
			}
			num++;
		}
	}
}
