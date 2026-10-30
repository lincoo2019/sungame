using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseReverseRake : TowerDefenseGravestone, INetworkSpawnStateReceiver
{
	private bool _triggered;

	private bool _hitCompleted;

	private static bool IsRemoteClient
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			RemoveFromGroup("Gravestone");
			TargetRegistrationComponent obj = targetRegistrationComponent;
			if (obj != null && !obj.IsReleased)
			{
				targetRegistrationComponent.canProjectileCheck = false;
			}
		}
	}

	public override void IdleEntered()
	{
		base.sprite.SetAnimation("Idle", loop: false);
		base.sprite.pause = !_triggered;
	}

	public override void ActivateGameplayProcessing()
	{
		base.ActivateGameplayProcessing();
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.pause = !_triggered;
		}
	}

	public static int TargetPriority(TowerDefenseCharacter target)
	{
		bool flag = !GodotObject.IsInstanceValid(target);
		if (!flag)
		{
			bool flag2 = ((target is TowerDefensePlant || target is TowerDefenseZombie) ? true : false);
			flag = !flag2;
		}
		if (flag || !target.inGame || target.editorPreviewMode || target.die || target.nearDie || target.isDestroy || target.IsQueuedForDeletion() || !GodotObject.IsInstanceValid(target.instance) || target.instance.hitpoints <= 0.0 || target.instance.invincible || target.instance.hologram || !target.instance.canBeCollection || !GodotObject.IsInstanceValid(target.config) || (target.instance.maskFlags & 2) != 0)
		{
			return 2147483647;
		}
		if (target is TowerDefenseZombie)
		{
			if ((target.instance.maskFlags & 1) == 0)
			{
				return 2147483647;
			}
			return 3;
		}
		if (target.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.SURROUND))
		{
			return 0;
		}
		if (target.config.plantGridOverrideType == TowerDefenseEnum.PLANTGRIDTYPE.POT)
		{
			return 2;
		}
		return 1;
	}

	private TowerDefenseCharacter FindVictim()
	{
		TowerDefenseCellInstance towerDefenseCellInstance = (GodotObject.IsInstanceValid(cell) ? cell : TowerDefenseManager.GetMapCell(base.gridPos));
		if (!GodotObject.IsInstanceValid(towerDefenseCellInstance))
		{
			return null;
		}
		TowerDefenseCharacter result = null;
		int num = 2147483647;
		foreach (TowerDefenseCharacter character in towerDefenseCellInstance.GetCharacterList())
		{
			int num2 = TargetPriority(character);
			if (num2 < num && character.camp != base.camp)
			{
				num = num2;
				result = character;
			}
		}
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (num > 0 && GodotObject.IsInstanceValid(towerDefenseManager?.characterRegistry))
		{
			foreach (TowerDefenseCharacter charactersForLine in towerDefenseManager.characterRegistry.GetCharactersForLineList(base.gridPos.Y))
			{
				int num3 = TargetPriority(charactersForLine);
				if (num3 < num && charactersForLine.camp != base.camp && !((charactersForLine.ShouldUpdateGridPos() ? towerDefenseManager.GetMapGridPos(charactersForLine.GetLogicalGlobalPosition()) : charactersForLine.gridPos) != base.gridPos))
				{
					num = num3;
					result = charactersForLine;
				}
			}
		}
		return result;
	}

	public override void IdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (!IsRemoteClient && !_triggered && inGame && !editorPreviewMode && !(timeScale <= 0.0) && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && GodotObject.IsInstanceValid(FindVictim()))
		{
			_triggered = true;
			base.sprite.SetAnimation("Idle", loop: false);
			base.sprite.pause = false;
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (!(clip != "Idle") && _triggered && !_hitCompleted && !IsRemoteClient && !die && !isDestroy)
		{
			_hitCompleted = true;
			TowerDefenseCharacter towerDefenseCharacter = FindVictim();
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.Hurt(800.0);
			}
			AudioManager.Instance.AudioPlay("Bonk");
			Destroy();
		}
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		if (data != null && data.ContainsKey("rake_camp"))
		{
			TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP = (TowerDefenseEnum.CHARACTER_CAMP)data["rake_camp"].AsInt32();
			TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP2 = ((!data.GetValueOrDefault("hypnoses", false).AsBool()) ? cHARACTER_CAMP : (cHARACTER_CAMP switch
			{
				TowerDefenseEnum.CHARACTER_CAMP.PLANT => TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, 
				TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE => TowerDefenseEnum.CHARACTER_CAMP.PLANT, 
				_ => cHARACTER_CAMP, 
			}));
			base.camp = cHARACTER_CAMP2;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["triggered"] = _triggered;
		dictionary["hitCompleted"] = _hitCompleted;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_triggered = data.GetValueOrDefault("triggered", false).AsBool();
		_hitCompleted = data.GetValueOrDefault("hitCompleted", false).AsBool();
		if (_hitCompleted && !IsRemoteClient)
		{
			CallDeferred("Destroy");
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return ExportVariantSave();
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		_triggered = data.GetValueOrDefault("triggered", _triggered).AsBool();
		_hitCompleted = data.GetValueOrDefault("hitCompleted", _hitCompleted).AsBool();
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.pause = !_triggered;
		}
	}
}
