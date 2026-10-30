using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseMushroomMinis : TowerDefenseGravestone
{
	private bool _celebrating;

	private bool _plantingReaction;

	private bool _zombiePresent;

	private BattleEventBus _events;

	public override bool IsPermanentObstacle => true;

	public override void _Ready()
	{
		base.rise = false;
		useIdleAnimeReset = false;
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && base.HasValidRuntimeConfiguration)
		{
			ApplyProtection();
			_events = BattleEventBus.Instance;
			if (GodotObject.IsInstanceValid(_events))
			{
				_events.OnGameVictory += Celebrate;
				_events.OnGameStarted += ResetReaction;
			}
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			_celebrating = currentControl != null && currentControl.levelControl?.awardCreate == true;
			RefreshReaction();
		}
	}

	private void ApplyProtection()
	{
		instance.invincible = true;
		instance.invincibleHurt = true;
		instance.invincibleSmash = true;
		instance.canBeCollection = false;
		instance.canCollection = false;
		instance.maskFlags = 0;
		if (targetRegistrationComponent != null)
		{
			targetRegistrationComponent.canProjectileCheck = false;
			targetRegistrationComponent.canCarry = false;
			targetRegistrationComponent.NotifyTargetStateChanged();
		}
		HitBoxDestroy();
	}

	public override void _ExitTree()
	{
		if (GodotObject.IsInstanceValid(_events))
		{
			_events.OnGameVictory -= Celebrate;
			_events.OnGameStarted -= ResetReaction;
		}
		_events = null;
		base._ExitTree();
	}

	public override bool CanSleep()
	{
		return false;
	}

	public override void IdleEntered()
	{
		RefreshReaction();
	}

	public override void IdleProcessing(double delta)
	{
		RefreshReaction();
	}

	public override void OnPlantingBlocked()
	{
		if (!_celebrating && !_plantingReaction && !isDestroy && GodotObject.IsInstanceValid(instance))
		{
			RefreshReaction();
			if (!_zombiePresent)
			{
				_plantingReaction = true;
				RefreshReaction();
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "anim_shooting" && _plantingReaction)
		{
			_plantingReaction = false;
			RefreshReaction();
		}
	}

	private bool HasZombieInCell()
	{
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = TowerDefenseManager.Instance?.characterRegistry;
		if (!GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry))
		{
			return false;
		}
		foreach (TowerDefenseCharacter charactersForGridWindow in towerDefenseBattleCharacterRegistry.GetCharactersForGridWindowList(base.gridPos.X, base.gridPos.Y, 0, 0))
		{
			if (charactersForGridWindow is TowerDefenseZombie { isDestroy: false, die: false, nearDie: false, isGround: not false } towerDefenseZombie && towerDefenseZombie.gridPos == base.gridPos && GodotObject.IsInstanceValid(towerDefenseZombie.instance) && towerDefenseZombie.instance.hitpoints > 0.0)
			{
				return true;
			}
		}
		return false;
	}

	private void RefreshReaction()
	{
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(base.sprite) && !isDestroy)
		{
			_zombiePresent = !_celebrating && HasZombieInCell();
			if (_zombiePresent || _celebrating)
			{
				_plantingReaction = false;
			}
			string text = (_celebrating ? "anim_cheer" : ((_zombiePresent || _plantingReaction) ? "anim_shooting" : "anim_idle"));
			bool flag = !_plantingReaction;
			if (base.sprite.clip != text || base.sprite.loop != flag)
			{
				base.sprite.SetAnimation(text, flag);
			}
			base.sprite.timeScale = timeScale;
		}
	}

	private void Celebrate()
	{
		_celebrating = true;
		RefreshReaction();
	}

	private void ResetReaction()
	{
		_celebrating = false;
		_plantingReaction = false;
		RefreshReaction();
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		ApplyProtection();
		_plantingReaction = false;
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		_celebrating = currentControl != null && currentControl.levelControl?.awardCreate == true;
		RefreshReaction();
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		_plantingReaction = false;
		RefreshReaction();
	}

	public override void ActivateGameplayProcessing()
	{
		base.ActivateGameplayProcessing();
		ApplyProtection();
		RefreshReaction();
	}

	public override void Destroy(bool freeInstance = true)
	{
		if (Global.IsEditor && SceneManager.CurrentScene == "LevelEditorStage")
		{
			base.Destroy(freeInstance);
		}
	}

	public override void ClearFromMap()
	{
		base.Destroy();
	}

	public override void ShovelDestroy()
	{
		Destroy();
	}

	public override void DestroyWithVisualDelay(double delaySeconds)
	{
	}

	public override void AshDestroy()
	{
	}

	public override void SmashDestroy()
	{
	}

	public override void BlowBack(double num, double time = -1.0)
	{
	}
}
