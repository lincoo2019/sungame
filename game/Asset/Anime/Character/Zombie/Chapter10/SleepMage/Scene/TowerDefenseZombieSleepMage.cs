using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieSleepMage : TowerDefenseZombie
{
	private const double EntryCastDelay = 5.0;

	private const double SleepDuration = 10.0;

	private const double AwakenHealHitpoints = 1000.0;

	private const double AwakenSpeedScale = 2.0;

	private const string HeadMediaName = "Zombie_SleepMage_head.png";

	private const string SleepHeadTexture = "uid://4xy3e7owb7hi";

	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData spellProjectile;

	[Export(PropertyHint.None, "")]
	public Marker2D spellMuzzle;

	public bool spellSpent;

	public bool shooting;

	public bool shotPending;

	public double chantTimer;

	public double entryTimer;

	public bool enteredField;

	public bool sleepTriggered;

	public bool sleeping;

	public double sleepTimer;

	public bool awakenApplied;

	private bool _sleepHeadApplied;

	private StateHandle _castStateHandle;

	private bool _stateSignalsConnected;

	private bool _preserveCastOnRestore;

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_castStateHandle = base.StateMachine.GetStateById("zombie.mage.cast");
			StateHandle castStateHandle = _castStateHandle;
			if (castStateHandle != null && castStateHandle.IsValid)
			{
				_castStateHandle.Entered += CastEntered;
				_castStateHandle.PhysicsProcessing += CastProcessing;
				_castStateHandle.Exited += CastExited;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_castStateHandle != null)
			{
				_castStateHandle.Entered -= CastEntered;
				_castStateHandle.PhysicsProcessing -= CastProcessing;
				_castStateHandle.Exited -= CastExited;
			}
			_castStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			ConnectStateSignals();
			if (sleeping)
			{
				ShowSleepIndicator();
				ApplySleepHeadVisual();
			}
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		base._ExitTree();
	}

	private bool CanCast()
	{
		if (Engine.IsEditorHint() || editorPreviewMode || !inGame || die || nearDie || isDestroy)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(base.sprite) || base.sprite.pause)
		{
			return false;
		}
		return TowerDefenseManager.HasGameplayAuthority;
	}

	public override void Walk()
	{
		ActivateGameplayProcessing();
		StateHandle castStateHandle = _castStateHandle;
		if ((castStateHandle == null || !castStateHandle.IsActive || die || nearDie) && !sleeping)
		{
			base.Walk();
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (sleeping)
		{
			ProcessSleep(delta);
			return;
		}
		TryStartCast(delta);
		base.WalkProcessing(delta);
	}

	private void TryStartCast(double delta)
	{
		if (spellSpent || sleepTriggered || sleeping)
		{
			return;
		}
		StateHandle castStateHandle = _castStateHandle;
		if (castStateHandle != null && castStateHandle.IsValid && !_castStateHandle.IsActive && CanCast() && HasEnteredField())
		{
			entryTimer += delta;
			if (!(entryTimer < 5.0))
			{
				SendStateEvent("ToMageCast");
			}
		}
	}

	private bool HasEnteredField()
	{
		if (enteredField)
		{
			return true;
		}
		if (!inGame || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return false;
		}
		if (!base.IsInsideComponentBattlefield)
		{
			return false;
		}
		enteredField = true;
		return true;
	}

	private bool TryFindSpellTarget(out TowerDefenseCharacter target)
	{
		target = null;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return false;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature) || mapFeature.rect.Size.X <= 0f)
		{
			return false;
		}
		List<TowerDefenseCharacter> characterTargetFromRectList = TowerDefenseManager.Instance.GetCharacterTargetFromRectList(this, mapFeature.rect, checkLine: true);
		TowerDefenseCharacter towerDefenseCharacter = null;
		for (int i = 0; i < characterTargetFromRectList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = characterTargetFromRectList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter2) && !towerDefenseCharacter2.die && !towerDefenseCharacter2.isDestroy)
			{
				if (towerDefenseCharacter2 is TowerDefensePlant)
				{
					target = towerDefenseCharacter2;
					return true;
				}
				if (towerDefenseCharacter == null)
				{
					towerDefenseCharacter = towerDefenseCharacter2;
				}
			}
		}
		target = towerDefenseCharacter;
		return GodotObject.IsInstanceValid(target);
	}

	public void CastEntered()
	{
		groundMoveComponent?.SetAlive(false);
		startAttack = false;
		if (_preserveCastOnRestore)
		{
			_preserveCastOnRestore = false;
			return;
		}
		shotPending = true;
		shooting = false;
		chantTimer = 0.0;
		base.sprite.SetAnimation("Up", loop: false);
	}

	public void CastProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (sleeping)
		{
			ProcessSleep(delta);
		}
		else if (CanCast() && !shooting)
		{
			chantTimer += delta * timeScale;
			if (chantTimer >= 5.0)
			{
				shooting = true;
				base.sprite.SetAnimation("Shooting", loop: false);
			}
		}
	}

	public void CastExited()
	{
		shotPending = false;
		shooting = false;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire")
		{
			StateHandle castStateHandle = _castStateHandle;
			if (castStateHandle != null && castStateHandle.IsActive && shooting && shotPending && CanCast())
			{
				shotPending = false;
				spellSpent = true;
				Fire();
			}
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		StateHandle castStateHandle = _castStateHandle;
		if (castStateHandle == null || !castStateHandle.IsActive)
		{
			return;
		}
		if (!(clip == "Up"))
		{
			if (clip == "Shooting" && shooting && !die && !nearDie && !isDestroy)
			{
				EnterSleep();
			}
		}
		else if (!shooting)
		{
			base.sprite.SetAnimation("Magic");
		}
	}

	public void Fire()
	{
		if (spellProjectile != null && TowerDefenseManager.HasGameplayAuthority)
		{
			if (GodotObject.IsInstanceValid(spellMuzzle) && spellMuzzle.GetParent() is AdobeAnimateSlot adobeAnimateSlot)
			{
				adobeAnimateSlot.Update();
			}
			Vector2 pos = (GodotObject.IsInstanceValid(spellMuzzle) ? GetLogicalGlobalPosition(spellMuzzle) : (GetLogicalGlobalPosition() + new Vector2(0f, -25f)));
			bool value = base.Scale.X * transformPoint.Scale.X * base.sprite.Scale.X >= 0f;
			TryFindSpellTarget(out var target);
			TowerDefenseCharacter target2 = target;
			double height = base.groundHeight;
			Vector2 velocity = new Vector2(-240f, 0f);
			TowerDefenseProjectileCreateData projectileData = spellProjectile;
			int collisionFlags = instance.collisionFlags;
			TowerDefenseEnum.CHARACTER_CAMP num = base.camp;
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = value,
				deprioritizeDisabledTargets = true
			};
			FireComponent.CreateProjectilePositionByData(this, target2, height, pos, velocity, projectileData, collisionFlags, num, default(Vector2), overrides);
		}
	}

	private void EnterSleep()
	{
		if (!sleepTriggered)
		{
			sleepTriggered = true;
			sleeping = true;
			sleepTimer = 0.0;
			shooting = false;
			shotPending = false;
			startAttack = false;
			groundMoveComponent?.SetAlive(false);
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAnimation(string.IsNullOrEmpty(sleepAnimeClip) ? idleAnimeClip : sleepAnimeClip);
			}
			ApplySleepHeadVisual();
			ShowSleepIndicator();
		}
	}

	private void ApplySleepHeadVisual()
	{
		if (!_sleepHeadApplied && GodotObject.IsInstanceValid(base.sprite))
		{
			_sleepHeadApplied = true;
			base.sprite.SetAtlasReplace("Zombie_SleepMage_head.png", "uid://4xy3e7owb7hi");
		}
	}

	private void RestoreHeadVisual()
	{
		if (_sleepHeadApplied)
		{
			_sleepHeadApplied = false;
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAtlasReplace("Zombie_SleepMage_head.png", string.Empty);
			}
		}
	}

	private void ProcessSleep(double delta)
	{
		if (sleeping && TowerDefenseManager.HasGameplayAuthority)
		{
			sleepTimer += delta;
			if (!(sleepTimer < 10.0))
			{
				WakeUpFromSleep();
			}
		}
	}

	protected override void OnWakeUp()
	{
		base.OnWakeUp();
		if (sleeping)
		{
			WakeUpFromSleep();
		}
	}

	private void WakeUpFromSleep()
	{
		if (sleeping)
		{
			sleeping = false;
			sleepTimer = 0.0;
			RestoreHeadVisual();
			HideSleepIndicator();
			ApplyAwakenBlessing();
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.SetAnimation(base.walkAnimeClip);
			}
			base.Walk();
		}
	}

	private void ApplyAwakenBlessing()
	{
		if (!awakenApplied)
		{
			awakenApplied = true;
			if (GodotObject.IsInstanceValid(instance) && !instance.die && !instance.nearDie && TowerDefenseManager.HasGameplayAuthority)
			{
				Health(1000.0);
				timeScaleInit *= 2.0;
			}
		}
	}

	private void ShowSleepIndicator()
	{
		SleepComponent sleepRuntime = GetSleepRuntime();
		if (sleepRuntime != null)
		{
			sleepRuntime.ShowScriptedSleepIndicator();
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.QueueRedraw();
			}
		}
	}

	private void HideSleepIndicator()
	{
		GetSleepRuntime()?.HideScriptedSleepIndicator();
	}

	public override void DieEntered()
	{
		RestoreHeadVisual();
		HideSleepIndicator();
		base.DieEntered();
	}

	public override void DamagePointReach(string name)
	{
		base.DamagePointReach(name);
		if (!(name == "Arm"))
		{
			if (name == "Head" && base.damagePartSlot.ContainsKey("Head"))
			{
				DamagePartCreate("Head", null, new Vector2(-60f, -250f), keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
			}
		}
		else if (base.damagePartSlot.ContainsKey("Arm"))
		{
			DamagePartCreate("Arm", null, new Vector2(60f, -200f), keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["mageSpellSpent"] = spellSpent;
		dictionary["mageShooting"] = shooting;
		dictionary["mageShotPending"] = shotPending;
		dictionary["mageChantElapsed"] = chantTimer;
		dictionary["mageEntryElapsed"] = entryTimer;
		dictionary["mageEnteredField"] = enteredField;
		dictionary["mageSleepTriggered"] = sleepTriggered;
		dictionary["mageSleeping"] = sleeping;
		dictionary["mageSleepElapsed"] = sleepTimer;
		dictionary["mageAwakenApplied"] = awakenApplied;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		spellSpent = data.GetValueOrDefault("mageSpellSpent", false).AsBool();
		shooting = data.GetValueOrDefault("mageShooting", false).AsBool();
		shotPending = data.GetValueOrDefault("mageShotPending", false).AsBool();
		chantTimer = data.GetValueOrDefault("mageChantElapsed", 0.0).AsDouble();
		entryTimer = data.GetValueOrDefault("mageEntryElapsed", 0.0).AsDouble();
		enteredField = data.GetValueOrDefault("mageEnteredField", false).AsBool();
		sleepTriggered = data.GetValueOrDefault("mageSleepTriggered", false).AsBool();
		sleeping = data.GetValueOrDefault("mageSleeping", false).AsBool();
		sleepTimer = data.GetValueOrDefault("mageSleepElapsed", 0.0).AsDouble();
		awakenApplied = data.GetValueOrDefault("mageAwakenApplied", false).AsBool();
		_preserveCastOnRestore = shooting || shotPending;
		if (sleeping)
		{
			ApplySleepHeadVisual();
			ShowSleepIndicator();
		}
		else
		{
			RestoreHeadVisual();
			HideSleepIndicator();
		}
	}
}
