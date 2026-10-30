using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseItemSpikeball : TowerDefenseItem, INetworkSpawnStateReceiver
{
	private AttackComponent attackComponent;

	private int _pendingCarrierSyncId = -1;

	[Export(PropertyHint.None, "")]
	public double attack = 20.0;

	private double _fireInterval = 1.5;

	public TowerDefenseZombie carryCharacter;

	private static bool HasGameplayAuthority
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return MultiPlayerManager.IsHost;
			}
			return true;
		}
	}

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady())
			{
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased)
				{
					attackComponent.attackInterval = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent.OnAttack += ComponentAttack;
			attackComponent.SetCheckAreaRectangleWidth(0, TowerDefenseManager.Instance.GetMapGridSize().X * 1.2f);
			if (GodotObject.IsInstanceValid(carryCharacter))
			{
				attackComponent.SetCheckHitBoxSource(carryCharacter);
			}
			else
			{
				ResolvePendingCarrier();
			}
		}
	}

	public override void _ExitTree()
	{
		DetachCarryCharacter(clearFlag: true);
		base._ExitTree();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.OnAttack -= ComponentAttack;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) || !TowerDefenseManager.CurrentControl.isGameRunning || !inGame || die || nearDie)
		{
			return;
		}
		ResolvePendingCarrier();
		if (GodotObject.IsInstanceValid(targetZombie) && Carry(targetZombie as TowerDefenseZombie) == null)
		{
			Destroy();
		}
		else
		{
			if (!GodotObject.IsInstanceValid(carryCharacter))
			{
				return;
			}
			if (carryCharacter.nearDie || carryCharacter.die)
			{
				Destroy();
				return;
			}
			UpdateCarryPresentation();
			if (HasGameplayAuthority)
			{
				ProcessCarryCarCollision();
			}
		}
	}

	public void ComponentAttack()
	{
		AudioManager.Instance.AudioPlay("ProjectileThrow");
		if (HasGameplayAuthority)
		{
			attackComponent.AttackAllFlag(attack, 2);
		}
	}

	public TowerDefenseCharacter Carry(TowerDefenseZombie character)
	{
		targetZombie = null;
		if (character != null)
		{
			if (character.isRise)
			{
				return null;
			}
			if (character.hasSpikeball)
			{
				return null;
			}
			if ((character.instance.maskFlags & 1) == 0)
			{
				return null;
			}
			if (character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				return null;
			}
			if (character.camp != base.camp)
			{
				return null;
			}
			if (!character.targetRegistrationComponent.canCarry)
			{
				return null;
			}
		}
		AttachCarryCharacter(character);
		return carryCharacter;
	}

	private void AttachCarryCharacter(TowerDefenseZombie character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			if (GodotObject.IsInstanceValid(carryCharacter) && carryCharacter != character)
			{
				DetachCarryCharacter(clearFlag: true);
			}
			carryCharacter = character;
			carryCharacter.OnDestroy -= CarryCharacterDestroyed;
			carryCharacter.OnDestroy += CarryCharacterDestroyed;
			carryCharacter.hasSpikeball = true;
			UpdateCarryPresentation();
			attackComponent?.SetCheckHitBoxSource(carryCharacter);
		}
	}

	private void UpdateCarryPresentation()
	{
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			ulong num = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			if (num == 18446744073709551615uL)
			{
				num = Engine.GetPhysicsFrames();
			}
			base.groundHeight = carryCharacter.groundHeight;
			base.z = 10.0;
			SetGlobalPositionForPhysicsFrame(carryCharacter.GetGlobalPositionForPhysicsFrame(num), num);
			base.gridPos = carryCharacter.gridPos;
		}
	}

	private void ResolvePendingCarrier()
	{
		if (_pendingCarrierSyncId >= 0)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(_pendingCarrierSyncId, out var value) && value is TowerDefenseZombie character && GodotObject.IsInstanceValid(character))
			{
				_pendingCarrierSyncId = -1;
				targetZombie = null;
				AttachCarryCharacter(character);
			}
		}
	}

	private void QueueNetworkCarrier(Dictionary data)
	{
		int num = data.GetValueOrDefault("carrierSyncId", -1).AsInt32();
		if (num < 0)
		{
			_pendingCarrierSyncId = -1;
			DetachCarryCharacter(clearFlag: true);
		}
		else if (GodotObject.IsInstanceValid(carryCharacter) && carryCharacter.syncId == num)
		{
			_pendingCarrierSyncId = -1;
		}
		else
		{
			_pendingCarrierSyncId = num;
			ResolvePendingCarrier();
		}
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		if (data != null)
		{
			QueueNetworkCarrier(data);
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		int num = (GodotObject.IsInstanceValid(carryCharacter) ? carryCharacter.syncId : _pendingCarrierSyncId);
		return new Dictionary { ["carrierSyncId"] = num };
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		if (data != null)
		{
			QueueNetworkCarrier(data);
		}
	}

	private void DetachCarryCharacter(bool clearFlag)
	{
		if (!GodotObject.IsInstanceValid(carryCharacter))
		{
			carryCharacter = null;
			return;
		}
		carryCharacter.OnDestroy -= CarryCharacterDestroyed;
		if (clearFlag && !carryCharacter.die && !carryCharacter.nearDie)
		{
			carryCharacter.hasSpikeball = false;
		}
		carryCharacter = null;
	}

	private void CarryCharacterDestroyed(TowerDefenseCharacter _character)
	{
		Destroy();
	}

	private void ProcessCarryCarCollision()
	{
		if (GodotObject.IsInstanceValid(carryCharacter) && carryCharacter.IsHitBoxEnabled)
		{
			List<TowerDefenseCharacter> charactersIntersectingRectList = TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectList(carryCharacter.WorldHitRect, carryCharacter.gridPos.Y, includeAllLineCheck: true);
			for (int i = 0; i < charactersIntersectingRectList.Count && !TryDestroyCarZombie(charactersIntersectingRectList[i]); i++)
			{
			}
		}
	}

	private bool TryDestroyCarZombie(TowerDefenseCharacter character)
	{
		if (!(character is TowerDefenseZombie towerDefenseZombie))
		{
			return false;
		}
		if (towerDefenseZombie.nearDie || towerDefenseZombie.die)
		{
			return false;
		}
		if (towerDefenseZombie.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR)
		{
			return false;
		}
		towerDefenseZombie.Die();
		Destroy();
		return true;
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "attack", attack },
			{ "fireInterval", fireInterval }
		};
		if (GodotObject.IsInstanceValid(carryCharacter))
		{
			dictionary["carryCharacterNodeName"] = carryCharacter.Name;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attack = data.GetValueOrDefault("attack", 20.0).AsDouble();
		fireInterval = data.GetValueOrDefault("fireInterval", fireInterval).AsDouble();
		string text = data.GetValueOrDefault("carryCharacterNodeName", "").AsString();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (GodotObject.IsInstanceValid(node2D))
		{
			TowerDefenseZombie nodeOrNull = node2D.GetNodeOrNull<TowerDefenseZombie>(new NodePath(text));
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				_pendingCarrierSyncId = -1;
				AttachCarryCharacter(nodeOrNull);
			}
		}
	}
}
