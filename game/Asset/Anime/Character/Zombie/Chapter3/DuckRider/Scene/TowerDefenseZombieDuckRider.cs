using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieDuckRider : TowerDefenseZombie
{
	public bool useCone;

	public bool useBucket;

	public bool over;

	public bool mowerKill;

	public TowerDefenseCharacter carryCharacter;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !base.IsProgressRestoreInFlight)
		{
			float num = GD.Randf();
			if ((double)num < 0.1)
			{
				useBucket = true;
				base.sprite.SetFliters(new Array { "anim_bucket" }, open: true);
			}
			else if ((double)num < 0.3)
			{
				useCone = true;
				base.sprite.SetFliters(new Array { "anim_cone" }, open: true);
			}
		}
	}

	public override void _ExitTree()
	{
		if (!Engine.IsEditorHint())
		{
			ReleaseCarriedCharacter();
		}
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl.isGameRunning && inGame && !over && !die && !nearDie)
		{
			if (GodotObject.IsInstanceValid(carryCharacter))
			{
				ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
				Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
				Vector2 globalPositionForPhysicsFrame2 = carryCharacter.GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
				globalPositionForPhysicsFrame2.X = globalPositionForPhysicsFrame.X + (float)(instance.hypnoses ? (-30) : 30);
				carryCharacter.SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame2, currentPhysicsFrame);
				carryCharacter.groundHeight = base.z + 40.0;
				carryCharacter.z = base.z + 40.0;
			}
			else
			{
				ProcessCarryCandidate();
			}
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * base.walkSpeedScale;
		if (!attackComponent.CanAttack())
		{
			return;
		}
		if (GodotObject.IsInstanceValid(attackComponent.target.cell) && attackComponent.target.cell.HasSpike())
		{
			attackComponent.target = attackComponent.target.cell.GetSpike();
		}
		if ((attackComponent.target.instance.physiqueTypeFlags & 0x10) == 0)
		{
			attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)base.config).smashAttack);
			return;
		}
		if (attackComponent.target.instance.spikeHurt != -1.0)
		{
			TowerDefenseCharacter target = attackComponent.target;
			double spikeHurt = attackComponent.target.instance.spikeHurt;
			target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
		}
		Die();
	}

	public override void DieEntered()
	{
		base.DieEntered();
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		HitBoxDestroy();
		base.sprite.SetFliters(new Array
		{
			"anim_bucket", "anim_cone", "anim_hair", "Zombie_outerarm_lower", "Zombie_outerarm_upper", "Zombie_outerarm_hand", "anim_head2", "Zombie_tie", "Zombie_body", "Zombie_outerleg_lower",
			"Zombie_outerleg_foot", "Zombie_outerleg_upper", "Zombie_innerleg_foot", "Zombie_innerleg_lower", "Zombie_innerleg_upper", "anim_head1", "Zombie_neck", "anim_innerarm1", "anim_innerarm2", "anim_innerarm3"
		}, open: false);
		ReleaseCarriedCharacter();
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		if (mowerKill)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			return;
		}
		string packetName = "ZombieNormal";
		if (useCone)
		{
			packetName = "ZombieNormalCone";
		}
		if (useBucket)
		{
			packetName = "ZombieNormalBucket";
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter zombie = packetConfig.Create(logicalGlobalPosition, base.gridPos, 40.0);
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", zombie);
		double hitpointScale = instance.hitpointScale;
		Vector2 scale = transformPoint.Scale;
		zombie.CallDeferred("SetHitpointAndScale", hitpointScale, scale);
		if (instance.hypnoses)
		{
			Callable.From(delegate
			{
				zombie.Hypnoses();
			}).CallDeferred();
		}
		zombie.CallDeferred("WalkReady");
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(packetName, base.gridPos.X, base.gridPos.Y, nextSyncId, hitpointScale, scale.X, instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true, 40.0);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private void ReleaseCarriedCharacter()
	{
		TowerDefenseZombieCarryRelation.Release(this, ref carryCharacter, base.groundHeight);
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary();
		TowerDefenseZombieCarryRelation.Export(dictionary, carryCharacter);
		dictionary["useCone"] = useCone;
		dictionary["useBucket"] = useBucket;
		return dictionary;
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		useCone = data.GetValueOrDefault("useCone", false).AsBool();
		useBucket = data.GetValueOrDefault("useBucket", false).AsBool();
		base.sprite.SetFliters(new Array { "anim_bucket" }, useBucket);
		base.sprite.SetFliters(new Array { "anim_cone" }, useCone);
		TowerDefenseZombie towerDefenseZombie = TowerDefenseZombieCarryRelation.Resolve(data);
		if (!GodotObject.IsInstanceValid(towerDefenseZombie) || towerDefenseZombie == this || !TowerDefenseZombieCarryRelation.Attach(this, ref carryCharacter, towerDefenseZombie, base.z + 40.0))
		{
			TowerDefenseZombieCarryRelation.Release(this, ref carryCharacter, base.groundHeight);
		}
	}

	private void ProcessCarryCandidate()
	{
		if (TryGetActiveWorldHitRect(out var rect))
		{
			List<TowerDefenseCharacter> charactersIntersectingRectListForCamp = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListForCamp(rect, base.camp);
			for (int i = 0; i < charactersIntersectingRectListForCamp.Count && (!(charactersIntersectingRectListForCamp[i] is TowerDefenseZombie zombie) || !TryCarryZombie(zombie)); i++)
			{
			}
		}
	}

	private bool TryCarryZombie(TowerDefenseZombie zombie)
	{
		if (over)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning)
		{
			return false;
		}
		if (!inGame)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return false;
		}
		if (zombie == this)
		{
			return false;
		}
		if (zombie.isRise)
		{
			return false;
		}
		if (zombie.camp != base.camp)
		{
			return false;
		}
		if (!zombie.CanCollision(instance.maskFlags))
		{
			return false;
		}
		if (zombie.instance.zombiePhysique > TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL)
		{
			return false;
		}
		if (!zombie.targetRegistrationComponent.canCarry)
		{
			return false;
		}
		return TowerDefenseZombieCarryRelation.Attach(this, ref carryCharacter, zombie, base.z + 40.0);
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			ReleaseCarriedCharacter();
		}
	}
}
