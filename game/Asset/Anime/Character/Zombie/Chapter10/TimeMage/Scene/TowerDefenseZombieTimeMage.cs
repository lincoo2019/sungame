using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieTimeMage : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileCreateData spellProjectile;

	[Export(PropertyHint.None, "")]
	public Marker2D spellMuzzle;

	[Export(PropertyHint.None, "")]
	public PackedScene reviveEffect;

	public bool entryStarted;

	public bool spellSpent;

	public bool shooting;

	public bool shotPending;

	public double spellTimer;

	private StateHandle _castStateHandle;

	private bool _stateSignalsConnected;

	private bool _preserveCastOnRestore;

	private bool _reborn;

	private bool _rewindPending;

	private bool _rewindSpent;

	private double _appearElapsed = 1.0;

	private bool _appearanceEffectPlayed;

	private StateHandle _appearState;

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_castStateHandle = base.StateMachine.GetStateById("zombie.mage.cast");
		_appearState = base.StateMachine.GetStateById("zombie.time_mage.appear");
		StateHandle castStateHandle = _castStateHandle;
		if (castStateHandle != null && castStateHandle.IsValid)
		{
			StateHandle appearState = _appearState;
			if (appearState != null && appearState.IsValid)
			{
				_castStateHandle.Entered += CastEntered;
				_castStateHandle.PhysicsProcessing += CastProcessing;
				_castStateHandle.Exited += CastExited;
				_appearState.Entered += AppearEntered;
				_appearState.PhysicsProcessing += AppearProcessing;
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
			if (_appearState != null)
			{
				_appearState.Entered -= AppearEntered;
				_appearState.PhysicsProcessing -= AppearProcessing;
			}
			_appearState = null;
			_stateSignalsConnected = false;
		}
	}

	public override void DamagePointReach(string name)
	{
		base.DamagePointReach(name);
		if (!(name == "Arm"))
		{
			if (!(name == "Head"))
			{
				return;
			}
			if (_reborn || suppressDeathrattles)
			{
				if (base.damagePartSlot.ContainsKey("Head"))
				{
					DamagePartCreate("Head", null, new Vector2(-60f, -250f), keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
				}
			}
			else if (GodotObject.IsInstanceValid(base.sprite))
			{
				string[] array = new string[4] { "anim_head1", "anim_head2", "anim_innerhat", "anim_outerhat" };
				foreach (string text in array)
				{
					base.sprite.SetFliter(text, open: true);
				}
			}
		}
		else if (base.damagePartSlot.ContainsKey("Arm"))
		{
			DamagePartCreate("Arm", null, new Vector2(60f, -200f), keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
		}
	}

	public override void _Ready()
	{
		base._Ready();
		ApplyRebornVisual();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			ConnectStateSignals();
		}
	}

	public override void Walk()
	{
		ActivateGameplayProcessing();
		StateHandle castStateHandle = _castStateHandle;
		if (castStateHandle != null && castStateHandle.IsActive && !die && !nearDie)
		{
			return;
		}
		if (_reborn && _appearElapsed < 1.0 && !die)
		{
			StateHandle appearState = _appearState;
			if (appearState != null && appearState.IsValid)
			{
				if (!_appearState.IsActive)
				{
					SendStateEvent("ToMageAppear");
				}
				return;
			}
		}
		entryStarted = true;
		base.Walk();
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

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		SpellProcessing(delta);
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		SpellProcessing(delta);
	}

	private void SpellProcessing(double delta)
	{
		if (!entryStarted)
		{
			return;
		}
		StateHandle castStateHandle = _castStateHandle;
		if ((castStateHandle == null || !castStateHandle.IsActive) && CanCast())
		{
			spellTimer += delta * timeScale;
			if (spellTimer >= 10.0)
			{
				SendStateEvent("ToMageCast");
			}
		}
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
		shooting = true;
		base.sprite.SetAnimation("Shooting", loop: false);
	}

	public void CastProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (CanCast() && !shotPending)
		{
			spellTimer += delta * timeScale;
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
				spellTimer = 0.0;
				Fire();
			}
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
			FireComponent.CreateProjectilePositionByData(this, null, height, pos, velocity, projectileData, collisionFlags, num, default(Vector2), overrides);
		}
	}

	public void AppearEntered()
	{
		groundMoveComponent?.SetAlive(false);
		startAttack = false;
		base.sprite.SetAnimation("Idle");
		ApplyRebornVisual();
		SpawnAppearanceEffect();
	}

	private void SpawnAppearanceEffect()
	{
		if (!_appearanceEffectPlayed && reviveEffect != null && !(_appearElapsed >= 1.0) && GetParent() is Node2D node2D)
		{
			_appearanceEffectPlayed = true;
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			if (!TowerDefenseManager.TryCreateEffectSceneOnceFast(reviveEffect, node2D, base.gridPos, logicalGlobalPosition, preferSprite: true))
			{
				TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(reviveEffect, base.gridPos, "Fire");
				node2D.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
				towerDefenseEffectSpriteOnce.GlobalPosition = logicalGlobalPosition;
			}
		}
	}

	public void AppearProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (!base.sprite.pause && inGame && !die && !nearDie)
		{
			_appearElapsed = Math.Min(1.0, _appearElapsed + delta * timeScale);
			ApplyRebornVisual();
			if (_appearElapsed >= 1.0 && TowerDefenseManager.HasGameplayAuthority)
			{
				entryStarted = true;
				base.Walk();
			}
		}
	}

	private void ApplyRebornVisual()
	{
		string text = (base.dieWaterAnimeClip = ((_reborn || suppressDeathrattles) ? "DeathDeal" : "Death"));
		base.dieAnimeClip = text;
		if (_reborn && GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.SetFliter("watch", open: false);
			if (GodotObject.IsInstanceValid(transformPoint))
			{
				transformPoint.Modulate = new Color(1f, 1f, 1f, (float)_appearElapsed);
			}
		}
	}

	public override void DieEntered()
	{
		ApplyRebornVisual();
		if (!_reborn && !_rewindSpent && !suppressDeathrattles && inGame && TowerDefenseManager.HasGameplayAuthority)
		{
			_rewindPending = true;
		}
		base.DieEntered();
	}

	public override void AnimeCompleted(string clip)
	{
		if (!(clip == "Shooting"))
		{
			if (clip == "Death" && _rewindPending && !_rewindSpent && TowerDefenseManager.HasGameplayAuthority)
			{
				CompleteRewind();
			}
		}
		else
		{
			StateHandle castStateHandle = _castStateHandle;
			if (castStateHandle != null && castStateHandle.IsActive && shooting && CanCast())
			{
				base.Walk();
			}
		}
		base.AnimeCompleted(clip);
	}

	private void CompleteRewind()
	{
		_rewindSpent = true;
		_rewindPending = false;
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(packet))
		{
			return;
		}
		string saveKey = packet.saveKey;
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(saveKey);
		if (!GodotObject.IsInstanceValid(towerDefenseManager) || !GodotObject.IsInstanceValid(node2D) || packetConfig == null || suppressDeathrattles)
		{
			return;
		}
		bool hypnotized = instance.hypnoses;
		double hitpointScale = (GodotObject.IsInstanceValid(instance) ? instance.hitpointScale : 1.0);
		Vector2 bodyScale = (GodotObject.IsInstanceValid(transformPoint) ? transformPoint.Scale : Vector2.One);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		logicalGlobalPosition.X = (float)(hypnotized ? towerDefenseManager.GetMapGroundLeft() : towerDefenseManager.GetMapGroundRight());
		Vector2I vector2I = new Vector2I(hypnotized ? 1 : towerDefenseManager.GetMapGridNum().X, base.gridPos.Y);
		TowerDefenseCharacter towerDefenseCharacter = (base.EconomyOwnerAccountId.IsValid ? packetConfig.Create(base.EconomyOwnerAccountId, logicalGlobalPosition, vector2I, base.groundHeight) : packetConfig.Create(logicalGlobalPosition, vector2I, base.groundHeight));
		TowerDefenseZombieTimeMage next = towerDefenseCharacter as TowerDefenseZombieTimeMage;
		if (next == null)
		{
			towerDefenseCharacter?.QueueFree();
			return;
		}
		next._reborn = true;
		next._rewindSpent = true;
		next._appearElapsed = 0.0;
		next.Ready += delegate
		{
			next.SetHitpointAndScale(hitpointScale, bodyScale);
			if (hypnotized)
			{
				next.Hypnoses();
			}
			else
			{
				next.camp = base.camp;
			}
			next.WalkReady();
			TowerDefenseBattleFeatureWave.Instance?.AddSpawnCharacter(next);
		};
		node2D.AddChild(next, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseManager.PublishSpawnedCharacter(saveKey, next, useCreate: true, 0.0, walkAfterSpawn: true, "", next.ExportNetworkSpawnState());
	}

	public override Dictionary ExportNetworkSpawnState()
	{
		Dictionary dictionary = base.ExportNetworkSpawnState();
		dictionary["timeMageReborn"] = _reborn;
		dictionary["timeMageAppearElapsed"] = _appearElapsed;
		return dictionary;
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		_reborn = data.GetValueOrDefault("timeMageReborn", false).AsBool();
		_appearElapsed = data.GetValueOrDefault("timeMageAppearElapsed", 1.0).AsDouble();
		_rewindSpent |= _reborn;
		ApplyRebornVisual();
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["mageEntryStarted"] = entryStarted;
		dictionary["mageSpellSpent"] = spellSpent;
		dictionary["mageShooting"] = shooting;
		dictionary["mageShotPending"] = shotPending;
		dictionary["mageSpellElapsed"] = spellTimer;
		dictionary.Merge(ExportNetworkSpawnState(), overwrite: true);
		dictionary["timeMageRewindPending"] = _rewindPending;
		dictionary["timeMageRewindSpent"] = _rewindSpent;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		entryStarted = data.GetValueOrDefault("mageEntryStarted", false).AsBool();
		spellSpent = data.GetValueOrDefault("mageSpellSpent", false).AsBool();
		shooting = data.GetValueOrDefault("mageShooting", false).AsBool();
		shotPending = data.GetValueOrDefault("mageShotPending", false).AsBool();
		spellTimer = data.GetValueOrDefault("mageSpellElapsed", 0.0).AsDouble();
		_preserveCastOnRestore = shooting || shotPending;
		ImportNetworkSpawnState(data);
		_rewindPending = data.GetValueOrDefault("timeMageRewindPending", false).AsBool();
		_rewindSpent = data.GetValueOrDefault("timeMageRewindSpent", _reborn).AsBool();
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		base._ExitTree();
	}
}
