using System;
using Godot;

public sealed class MowerHitComponent : CharacterComponentRuntime
{
	public PackedScene mowerHitScene;

	public PackedScene mowerPuffScene;

	public float startupVelocity = 100f;

	public float runVelocity = 200f;

	public float accelerationDelay = 0.25f;

	public double attackAnimationBlend = 0.1;

	public double hugeTargetDamage = 100000.0;

	public double terminalTargetDamage = 1000000000.0;

	public int maxEffectCount = 100;

	public bool createPuffEffect = true;

	public bool ignoreGravestones = true;

	public bool ignoreCraters = true;

	public bool ignoreBowlingPlants = true;

	public bool ignoreSoccerBalls = true;

	public bool ignoreHypnotizedZombies = true;

	public bool ignoreBossZombies = true;

	public bool allowJumpingPolestriker = true;

	public TowerDefenseMower parent;

	private SceneTreeTimer _accelerationTimer;

	private Action _accelerationHandler;

	private bool _runAccelerationStarted;

	private bool _configured;

	private bool IsRemoteClient
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

	private MowerHitComponentDefinition Definition => base.ComponentDefinition as MowerHitComponentDefinition;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseMower;
		if (!_configured)
		{
			MowerHitComponentDefinition definition = Definition;
			mowerHitScene = definition?.mowerHitScene;
			mowerPuffScene = definition?.mowerPuffScene;
			startupVelocity = definition?.startupVelocity ?? 100f;
			runVelocity = definition?.runVelocity ?? 200f;
			accelerationDelay = definition?.accelerationDelay ?? 0.25f;
			attackAnimationBlend = definition?.attackAnimationBlend ?? 0.1;
			hugeTargetDamage = definition?.hugeTargetDamage ?? 100000.0;
			terminalTargetDamage = definition?.terminalTargetDamage ?? 1000000000.0;
			maxEffectCount = definition?.maxEffectCount ?? 100;
			createPuffEffect = definition?.createPuffEffect ?? true;
			ignoreGravestones = definition?.ignoreGravestones ?? true;
			ignoreCraters = definition?.ignoreCraters ?? true;
			ignoreBowlingPlants = definition?.ignoreBowlingPlants ?? true;
			ignoreSoccerBalls = definition?.ignoreSoccerBalls ?? true;
			ignoreHypnotizedZombies = definition?.ignoreHypnotizedZombies ?? true;
			ignoreBossZombies = definition?.ignoreBossZombies ?? true;
			allowJumpingPolestriker = definition?.allowJumpingPolestriker ?? true;
			_configured = true;
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DetachAccelerationTimer();
		parent = null;
	}

	protected override void OnReleased()
	{
		DetachAccelerationTimer();
		parent = null;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			DetachAccelerationTimer();
		}
	}

	public void HitCheck(AabbArea2D area)
	{
		if (GodotObject.IsInstanceValid(area) && area.GetParent() is TowerDefenseCharacter character)
		{
			HitCheckCharacter(character);
		}
	}

	public void HitCheckCharacter(TowerDefenseCharacter character)
	{
		if (IsRemoteClient || !CanProcessHit(character) || (ignoreGravestones && character is TowerDefenseGravestone) || (ignoreCraters && character is TowerDefenseCrater) || (ignoreBowlingPlants && character is TowerDefensePlantBowlingBase) || (ignoreSoccerBalls && character is TowerDefenseItemSoccerBall) || (character is TowerDefenseZombie towerDefenseZombie && ((!parent.run && towerDefenseZombie.Scale.X < 0f) || (ignoreHypnotizedZombies && towerDefenseZombie.instance.hypnoses) || (ignoreBossZombies && towerDefenseZombie.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS))) || !CanHitCharacter(character))
		{
			return;
		}
		bool flag = character.IsTargetableFromLine(parent.gridPos.Y);
		if (parent.CheckDifferentCamp(character.camp) && flag)
		{
			if (!parent.run)
			{
				parent.Run();
				parent.run = true;
			}
			Hit(character);
		}
	}

	private bool CanProcessHit(TowerDefenseCharacter character)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && parent.CanStartRun() && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(character.instance) && !character.die && !character.nearDie && !character.instance.die)
		{
			return !character.instance.nearDie;
		}
		return false;
	}

	private bool CanHitCharacter(TowerDefenseCharacter character)
	{
		if (parent.CanCollision(character.instance.maskFlags))
		{
			return true;
		}
		if (allowJumpingPolestriker && character is TowerDefenseZombiePolestriker towerDefenseZombiePolestriker)
		{
			return towerDefenseZombiePolestriker.isJump;
		}
		return false;
	}

	public void Hit(TowerDefenseCharacter character)
	{
		if (IsRemoteClient || !CanProcessHit(character))
		{
			return;
		}
		ExecuteMowerConfig(character);
		TryCreatePuff();
		if (IsHugeNonBossTarget(character))
		{
			if (character is TowerDefenseZombieDuckRider towerDefenseZombieDuckRider)
			{
				towerDefenseZombieDuckRider.mowerKill = true;
			}
			character.White(0.5, 0.0, 0.2);
			character.instance.DealHurt(hugeTargetDamage, playSplatAudio: false);
			FinishHit(character);
		}
		else if ((character.instance.physiqueTypeFlags & 0x20) != 0)
		{
			MarkTerminal(character);
			ApplyTerminalMowerDamage(character);
			FinishHit(character);
		}
		else
		{
			MarkTerminal(character);
			TryCreateMowerHit(character);
			ApplyTerminalMowerDamage(character);
			FinishHit(character);
		}
	}

	private void ApplyTerminalMowerDamage(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character?.instance))
		{
			character.instance.DealHurt(terminalTargetDamage, playSplatAudio: true, Vector2.Zero, createDamagePart: false);
		}
	}

	private void ExecuteMowerConfig(TowerDefenseCharacter character)
	{
		if (character.HasHitBox && parent.config is TowerDefenseMowerConfig towerDefenseMowerConfig && GodotObject.IsInstanceValid(towerDefenseMowerConfig.mowerConfig))
		{
			towerDefenseMowerConfig.mowerConfig.Execute(character);
		}
		if (character.HasHitBox)
		{
			character.HitBoxDestroy();
		}
	}

	private bool IsHugeNonBossTarget(TowerDefenseCharacter character)
	{
		if (character.config is TowerDefenseZombieConfig { physique: >=TowerDefenseEnum.ZOMBIE_PHYSIQUE.HUGE })
		{
			return character.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		}
		return false;
	}

	private static void MarkTerminal(TowerDefenseCharacter character)
	{
		character.die = true;
		character.nearDie = true;
	}

	private void TryCreatePuff()
	{
		if (createPuffEffect && CanCreateEffect() && GodotObject.IsInstanceValid(mowerPuffScene) && GodotObject.IsInstanceValid(parent?.transformPoint) && GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(mowerPuffScene, parent.gridPos);
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseEffectSpriteOnce.GlobalPosition = parent.GetLogicalGlobalPosition(parent.transformPoint);
			}
		}
	}

	private bool CanCreateEffect()
	{
		if (maxEffectCount < 0)
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.GetEffectCount() < maxEffectCount;
		}
		return false;
	}

	private bool TryCreateMowerHit(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(mowerHitScene) || !GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
		{
			return false;
		}
		MowerHit mowerHit = mowerHitScene.Instantiate(PackedScene.GenEditState.Disabled) as MowerHit;
		if (!GodotObject.IsInstanceValid(mowerHit))
		{
			return false;
		}
		TowerDefenseGroundItemBase.characterNode.AddChild(mowerHit, forceReadableName: false, Node.InternalMode.Disabled);
		mowerHit.GlobalPosition = (GodotObject.IsInstanceValid(parent.sprite) ? parent.GetLogicalGlobalPosition(parent.sprite) : parent.GetLogicalGlobalPosition());
		if (mowerHit.Init(character))
		{
			return true;
		}
		mowerHit.QueueFree();
		return false;
	}

	private void FinishHit(TowerDefenseCharacter character)
	{
		SendMowerKillSync(character);
		StartMowerRun();
	}

	private void StartMowerRun()
	{
		if (_runAccelerationStarted || !GodotObject.IsInstanceValid(parent) || !parent.CanStartRun())
		{
			return;
		}
		CharacterMoveComponent moveComponent = parent.moveComponent;
		if (moveComponent == null || moveComponent.IsReleased)
		{
			return;
		}
		_runAccelerationStarted = true;
		parent.moveComponent.SetVelocity(Vector2.Right * startupVelocity);
		if (accelerationDelay <= 0f)
		{
			CompleteMowerAcceleration();
			return;
		}
		SceneTree tree = parent.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			CompleteMowerAcceleration();
			return;
		}
		_accelerationTimer = tree.CreateTimer(accelerationDelay, processAlways: false);
		_accelerationHandler = CompleteMowerAcceleration;
		_accelerationTimer.Timeout += _accelerationHandler;
	}

	private void CompleteMowerAcceleration()
	{
		DetachAccelerationTimer();
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent) || !parent.CanStartRun())
		{
			return;
		}
		CharacterMoveComponent moveComponent = parent.moveComponent;
		if (moveComponent == null || moveComponent.IsReleased)
		{
			return;
		}
		parent.moveComponent.SetVelocity(Vector2.Right * runVelocity);
		if (!GodotObject.IsInstanceValid(parent.sprite))
		{
			return;
		}
		if (!parent.inWater && !string.IsNullOrEmpty(parent.attackAnimeClips))
		{
			parent.sprite.SetAnimation(parent.attackAnimeClips, loop: false);
			if (!string.IsNullOrEmpty(parent.runAnimeClips))
			{
				parent.sprite.AddAnimation(parent.runAnimeClips, 0.0, loop: true, attackAnimationBlend);
			}
		}
		else if (parent.inWater && !string.IsNullOrEmpty(parent.attackWaterAnimeClips))
		{
			parent.sprite.SetAnimation(parent.attackWaterAnimeClips, loop: false);
			if (!string.IsNullOrEmpty(parent.runWaterAnimeClips))
			{
				parent.sprite.AddAnimation(parent.runWaterAnimeClips, 0.0, loop: true, attackAnimationBlend);
			}
		}
	}

	private void DetachAccelerationTimer()
	{
		if (GodotObject.IsInstanceValid(_accelerationTimer) && _accelerationHandler != null)
		{
			_accelerationTimer.Timeout -= _accelerationHandler;
		}
		_accelerationTimer = null;
		_accelerationHandler = null;
	}

	private void SendMowerKillSync(TowerDefenseCharacter character)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(character) && character.syncId >= 0 && GodotObject.IsInstanceValid(MultiPlayerManager.Instance))
		{
			MultiPlayerManager.Instance.SendCharacterDestroy(character.syncId);
		}
	}
}
