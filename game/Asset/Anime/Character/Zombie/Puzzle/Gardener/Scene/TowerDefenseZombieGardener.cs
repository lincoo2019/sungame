using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieGardener : TowerDefenseZombie
{
	private AttackComponent _attackComponent2;

	private Node2D _potNode;

	private Node2D _plantNode;

	private string _pendingBarrowPlantNodeName = "";

	public bool canBarrow = true;

	public bool hasBarrow = true;

	public TowerDefenseCharacter barrowPlant;

	public Transform2D barrowPlantTranform;

	public bool over;

	public bool isPlant;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			_potNode = GetNode<Node2D>("%PotNode");
			_plantNode = GetNode<Node2D>("%PlantNode");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (GodotObject.IsInstanceValid(barrowPlant))
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			Vector2 logicalGlobalPosition2 = GetLogicalGlobalPosition(_potNode);
			Vector2 logicalGlobalPosition3 = barrowPlant.GetLogicalGlobalPosition();
			barrowPlant.gridPos = new Vector2I(base.gridPos.X, barrowPlant.gridPos.Y);
			barrowPlant.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition2.X, logicalGlobalPosition3.Y));
			barrowPlant.groundHeight = (logicalGlobalPosition2.Y - logicalGlobalPosition.Y) / transformPoint.GlobalScale.Y;
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void Hypnoses(double time = -1.0, bool canFliter = true, TowerDefenseCharacterBuffHypnoses hypnosesConfig = null)
	{
		if (!hasBarrow)
		{
			base.Hypnoses(time, canFliter, hypnosesConfig);
		}
	}

	public override void WalkProcessing(double delta)
	{
		if (hasBarrow && _attackComponent2.CanAttack() && (_attackComponent2.target.instance.physiqueTypeFlags & 0x10) != 0)
		{
			if (GodotObject.IsInstanceValid(_attackComponent2.target.cell) && _attackComponent2.target.cell.HasSpike())
			{
				_attackComponent2.target = _attackComponent2.target.cell.GetSpike();
			}
			if (_attackComponent2.target.instance.spikeHurt != -1.0)
			{
				TowerDefenseCharacter target = _attackComponent2.target;
				double spikeHurt = _attackComponent2.target.instance.spikeHurt;
				target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
				instance.ArmorDelete("Barrow");
				return;
			}
		}
		if (hasBarrow && canBarrow && !GodotObject.IsInstanceValid(barrowPlant) && _attackComponent2.CanAttack() && (_attackComponent2.target.instance.physiqueTypeFlags & 0x10) == 0)
		{
			foreach (TowerDefenseCharacter target2 in _attackComponent2.GetTargetList())
			{
				if (!(target2 is TowerDefensePlant { inGame: not false, componentAlive: not false } towerDefensePlant))
				{
					continue;
				}
				if (towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.POT) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.LILYPAD) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.SOIL) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.BRICK) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.SURROUND))
				{
					if (GodotObject.IsInstanceValid(towerDefensePlant.cell) && towerDefensePlant.cell.characterSlotDictionary.TryGetValue(towerDefensePlant, out var value) && GodotObject.IsInstanceValid(value))
					{
						continue;
					}
					Barrow(towerDefensePlant);
				}
				TurnTowardBarrowExit();
				break;
			}
		}
		base.WalkProcessing(delta);
		if (!GodotObject.IsInstanceValid(barrowPlant))
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
		}
	}

	public override void AttackProcessing(double delta)
	{
		if (hasBarrow && _attackComponent2.CanAttack() && (_attackComponent2.target.instance.physiqueTypeFlags & 0x10) != 0 && _attackComponent2.target.instance.spikeHurt != -1.0)
		{
			TowerDefenseCharacter target = _attackComponent2.target;
			double spikeHurt = _attackComponent2.target.instance.spikeHurt;
			target.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
			instance.ArmorDelete("Barrow");
			return;
		}
		if (hasBarrow && canBarrow && !GodotObject.IsInstanceValid(barrowPlant) && _attackComponent2.CanAttack() && (_attackComponent2.target.instance.physiqueTypeFlags & 0x10) == 0)
		{
			foreach (TowerDefenseCharacter target2 in _attackComponent2.GetTargetList())
			{
				if (!(target2 is TowerDefensePlant { inGame: not false, componentAlive: not false } towerDefensePlant))
				{
					continue;
				}
				if (towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.POT) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.LILYPAD) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.SOIL) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.BRICK) || towerDefensePlant.config.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.SURROUND))
				{
					if (GodotObject.IsInstanceValid(towerDefensePlant.cell) && towerDefensePlant.cell.characterSlotDictionary.TryGetValue(towerDefensePlant, out var value) && GodotObject.IsInstanceValid(value))
					{
						continue;
					}
					Barrow(towerDefensePlant);
				}
				TurnTowardBarrowExit();
				break;
			}
		}
		if (hasBarrow)
		{
			if (!attackComponent.CanAttack())
			{
				Walk();
			}
			base.sprite.timeScale = timeScale * 2.0;
		}
		else
		{
			base.AttackProcessing(delta);
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Barrow")
		{
			base.useAttackDps = true;
			hasBarrow = false;
			canBarrow = false;
			base.walkAnimeClip = "Walk";
			base.swimAnimeClip = "Walk";
			base.attackAnimeClip = "Eat";
			attackComponent.checkVase = false;
			attackComponent.attackType = "Eat";
			attackComponent.target = null;
			_attackComponent2.target = null;
			_attackComponent2.SetAlive(alive: false);
			Walk();
			instance.collisionFlags = 1;
			if (!isPlant)
			{
				isPlant = true;
				Plant();
			}
		}
	}

	public override void DestroySet()
	{
		if (!over)
		{
			over = true;
			Plant();
		}
	}

	public void Barrow(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || !character.inGame || !character.componentAlive)
		{
			return;
		}
		canBarrow = false;
		barrowPlant = character;
		TowerDefenseCellInstance towerDefenseCellInstance = barrowPlant.cell;
		if (GodotObject.IsInstanceValid(towerDefenseCellInstance))
		{
			TowerDefenseCharacter target = towerDefenseCellInstance.GetTarget(instance.maskFlags, base.camp);
			if (GodotObject.IsInstanceValid(target))
			{
				barrowPlant = target;
			}
			if (GodotObject.IsInstanceValid(barrowPlant))
			{
				towerDefenseCellInstance.RemoveCharacter(barrowPlant);
			}
		}
		barrowPlantTranform = barrowPlant.Transform;
		barrowPlant.SetHitBoxSuppressed(HitBoxSuppressionReason.Carried, suppressed: true);
		barrowPlant.gridPos = new Vector2I(-1, barrowPlant.gridPos.Y);
		barrowPlant.inGame = false;
		barrowPlant.componentAlive = false;
		attackComponent.target = null;
		_attackComponent2.target = null;
		barrowPlant.Reparent(_plantNode);
		barrowPlant.shadowSprite.Visible = false;
		barrowPlant.Scale = new Vector2(barrowPlant.Scale.X, (float)Mathf.Sign(barrowPlant.Scale.Y) * barrowPlant.Scale.Y);
		barrowPlant.Rotation = 0f;
		barrowPlant.SetLogicalGlobalPosition(GetLogicalGlobalPosition(_plantNode));
	}

	internal void TurnTowardBarrowExit()
	{
		base.Scale = new Vector2(0f - base.Scale.X, base.Scale.Y);
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.RefreshDirectionCache();
		}
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.NotifyAncestorTransformChangedForRender(changedInPhysicsFrame: true);
		}
	}

	public async void Plant()
	{
		if (!GodotObject.IsInstanceValid(barrowPlant))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(cell))
		{
			barrowPlant.componentAlive = true;
			barrowPlant.die = true;
			barrowPlant.Destroy();
			return;
		}
		if (!cell.CanPacketPlant(barrowPlant.packet))
		{
			barrowPlant.componentAlive = true;
			barrowPlant.die = true;
			barrowPlant.Destroy();
			return;
		}
		cell.CharacterPlant(barrowPlant.packet, barrowPlant);
		if (barrowPlant.HasHitBox)
		{
			barrowPlant.SetHitBoxSuppressed(HitBoxSuppressionReason.Carried, suppressed: false);
		}
		barrowPlant.gridPos = base.gridPos;
		barrowPlant.Reparent(TowerDefenseGroundItemBase.characterNode);
		barrowPlant.Rotation = barrowPlantTranform.Rotation;
		barrowPlant.Scale = barrowPlantTranform.Scale;
		barrowPlant.shadowSprite.Visible = !barrowPlant.invisible;
		if (barrowPlant.Scale.Y < 0f)
		{
			barrowPlant.Scale = new Vector2(barrowPlant.Scale.X, 0f - barrowPlant.Scale.Y);
			barrowPlant.RotationDegrees -= 180f;
		}
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(base.gridPos);
		barrowPlant.SetLogicalGlobalPosition(mapCellPlantPos);
		barrowPlant.shadowComponent.saveShadowPosition = new Vector2(mapCellPlantPos.X, barrowPlant.shadowComponent.saveShadowPosition.Y);
		barrowPlant.shadowSprite.GlobalPosition = new Vector2(mapCellPlantPos.X, barrowPlant.shadowSprite.GlobalPosition.Y);
		if (GodotObject.IsInstanceValid(barrowPlant))
		{
			barrowPlant.inGame = true;
			barrowPlant.componentAlive = true;
			barrowPlant.groundHeight = 0.0;
			barrowPlant.shadowSprite.Visible = !barrowPlant.invisible;
			barrowPlant = null;
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "attack")
		{
			_attackComponent2.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "canBarrow", canBarrow },
			{ "hasBarrow", hasBarrow },
			{ "over", over },
			{ "isPlant", isPlant }
		};
		if (GodotObject.IsInstanceValid(barrowPlant))
		{
			dictionary["barrowPlantNodeName"] = barrowPlant.Name.ToString();
			dictionary["barrowPlantTransform"] = barrowPlantTranform;
		}
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		canBarrow = data.GetValueOrDefault("canBarrow", true).AsBool();
		hasBarrow = data.GetValueOrDefault("hasBarrow", true).AsBool();
		over = data.GetValueOrDefault("over", false).AsBool();
		isPlant = data.GetValueOrDefault("isPlant", false).AsBool();
		_pendingBarrowPlantNodeName = data.GetValueOrDefault("barrowPlantNodeName", "").AsString();
		if (data.ContainsKey("barrowPlantTransform"))
		{
			barrowPlantTranform = data["barrowPlantTransform"].AsTransform2D();
		}
	}

	public override void FinalizeProgressRestore()
	{
		base.FinalizeProgressRestore();
		if (!string.IsNullOrEmpty(_pendingBarrowPlantNodeName) && GodotObject.IsInstanceValid(_plantNode))
		{
			TowerDefenseCharacter nodeOrNull = _plantNode.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(_pendingBarrowPlantNodeName));
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				barrowPlant = nodeOrNull;
			}
			_pendingBarrowPlantNodeName = "";
		}
	}
}
