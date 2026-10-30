using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPotVampire : TowerDefensePlant
{
	private static PackedScene _drainScene;

	private const double DrainInterval = 5.0;

	private const double DrainRatio = 0.1;

	private CharacterTimerComponent _timerComponent;

	private static PackedScene DrainScene => _drainScene ?? (_drainScene = GD.Load<PackedScene>("uid://je8no5cuugta"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 5.0);
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn")
		{
			TryDrain();
			_timerComponent.Run("Spawn", 5.0);
		}
	}

	public void TryDrain()
	{
		if (base.sprite.pause || instance.sleep || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = FindDrainTarget();
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			double num = towerDefenseCharacter.instance.hitpointsSave * 0.1;
			if (double.IsFinite(num) && !(num <= 0.0))
			{
				base.sprite.SetAnimation("Attack", loop: false);
				PlayDrainEffect(towerDefenseCharacter);
				towerDefenseCharacter.FlagHurt(num, 16);
				ShareHealthInCell(num);
			}
		}
	}

	private TowerDefenseCharacter FindDrainTarget()
	{
		Array campTarget = TowerDefenseManager.Instance.GetCampTarget(base.camp);
		double num = -1.0;
		TowerDefenseCharacter result = null;
		foreach (Variant item in campTarget)
		{
			TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)(GodotObject)item;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS && !(towerDefenseCharacter.instance.hitpoints <= num))
			{
				num = towerDefenseCharacter.instance.hitpoints;
				result = towerDefenseCharacter;
			}
		}
		return result;
	}

	private void PlayDrainEffect(TowerDefenseCharacter target)
	{
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(DrainScene, target.gridPos);
		if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
		{
			towerDefenseEffectSpriteOnce.GlobalPosition = target.GetLogicalGlobalPosition();
			TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void ShareHealthInCell(double drainHp)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter character in cell.GetCharacterList())
		{
			if (GodotObject.IsInstanceValid(character) && !character.die && !character.nearDie && !(character is TowerDefenseCrater) && !(character is TowerDefenseItem) && !(character is TowerDefenseGravestone) && !(character is TowerDefensePlantBowlingBase) && !CheckDifferentCamp(character.camp) && character.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				list.Add(character);
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		double num = drainHp / (double)list.Count;
		foreach (TowerDefenseCharacter item in list)
		{
			item.Health(num);
			if (item.instance.hitpoints >= item.instance.hitpointsSave)
			{
				item.instance.hitpoints = item.instance.hitpointsSave;
			}
			ShowHealthComponent obj = item.showHealthComponent;
			if (obj != null && !obj.IsReleased)
			{
				item.showHealthComponent.MarkDirty();
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Attack")
		{
			base.sprite.SetAnimation("Idle");
		}
	}
}
