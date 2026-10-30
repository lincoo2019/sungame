using System.Collections.Generic;
using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantSquashVase : TowerDefensePlant
{
	private static PackedScene _VASE_ZOMBIE_CHUNKS;

	private SquashComponent _squashComponent;

	private AttackComponent _attackComponent;

	private static PackedScene VASE_ZOMBIE_CHUNKS => _VASE_ZOMBIE_CHUNKS ?? (_VASE_ZOMBIE_CHUNKS = GD.Load<PackedScene>("uid://djs8ytienucdy"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_squashComponent = componentManager.GetRuntime<SquashComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			if (_squashComponent != null)
			{
				_squashComponent.OnHitAliveCharacters += HitAliveCharacters;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		SquashComponent squashComponent = _squashComponent;
		if (squashComponent != null && !squashComponent.IsReleased)
		{
			_squashComponent.OnHitAliveCharacters -= HitAliveCharacters;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
	}

	public void HitAliveCharacters(List<TowerDefenseCharacter> characterList)
	{
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>(characterList);
		TowerDefenseCharacter target = _squashComponent.target;
		if (IsCaptureCandidate(target) && !list.Contains(target))
		{
			list.Add(target);
		}
		List<TowerDefenseCharacter> list2 = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item in list)
		{
			if (IsCaptureCandidate(item) && (!(item is TowerDefenseZombie) || item.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS))
			{
				list2.Add(item);
			}
		}
		if (list2.Count <= 0)
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseCharacter.CreateCharacter("VaseSquashBlack", logicalGlobalPosition, TowerDefenseManager.Instance.GetMapGridPos(logicalGlobalPosition), base.groundHeight);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			((TowerDefenseVaseSquashBlack)towerDefenseCharacter).CharacterList = list2;
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("VaseSquashBlack", base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, hypnoses: false, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight);
			}
		}
		Destroy();
	}

	private static bool IsCaptureCandidate(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance) && !character.die && !character.nearDie && !character.isDestroy)
		{
			return character.instance.hitpoints > 0.0;
		}
		return false;
	}
}
