using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingEasterEgg : TowerDefensePlantBowlingBase
{
	[Export(PropertyHint.None, "")]
	public string packetBank = "WallnutBowling";

	private Vector2I _lastBowlingHitGridPos = new Vector2I(-1, -1);

	private static PackedScene _IMITATER_CLOUD;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			bowlingComponent = componentManager.GetRuntime<BowlingComponent>();
			BowlingComponent obj = bowlingComponent;
			if (obj != null && !obj.IsReleased)
			{
				bowlingComponent.OnBowling += Bowling;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.OnBowling -= Bowling;
		}
	}

	public void Bowling(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			_lastBowlingHitGridPos = character.gridPos;
		}
		moveComponent.velocity = Vector2.Zero;
		bowlingComponent.isRoll = false;
		base.sprite.timeScale = timeScale * 2.0;
		base.sprite.SetAnimation("Open", loop: false, 0.1);
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "Open"))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2I vector2I = ResolveOpenGridPos(logicalGlobalPosition);
		if (!TowerDefenseManager.Instance.CheckMapGridPosIn(vector2I))
		{
			Destroy();
			return;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(vector2I);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, vector2I);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(packetBank);
		if (GodotObject.IsInstanceValid(packetBankData))
		{
			Array array = new Array();
			array.AddRange(packetBankData.GetCategory("White"));
			array.AddRange(packetBankData.GetCategory("Original"));
			array.AddRange(packetBankData.GetCategory("Gold"));
			string text = (string)array.PickRandom();
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
			while (packetConfig.characterConfig is TowerDefensePlantConfig && array.Count > 1 && (!GodotObject.IsInstanceValid(mapCell) || !mapCell.CanPacketPlant(packetConfig)))
			{
				array.Remove(text);
				text = (string)array.PickRandom();
				packetConfig = TowerDefenseManager.GetPacketConfig(text);
			}
			if (packetConfig.characterConfig is TowerDefensePlantConfig)
			{
				Vector2I vector2I2 = vector2I;
				if (GodotObject.IsInstanceValid(mapCell) && !mapCell.CanPacketPlant(packetConfig))
				{
					vector2I2 = FindNearbyEmptyCell(vector2I, packetConfig);
				}
				if (TowerDefenseManager.Instance.CheckMapGridPosIn(vector2I2))
				{
					TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(vector2I2);
					if (GodotObject.IsInstanceValid(towerDefenseCharacter))
					{
						if (towerDefenseCharacter is TowerDefensePlantBowlingWallnutShooterBoom towerDefensePlantBowlingWallnutShooterBoom)
						{
							towerDefensePlantBowlingWallnutShooterBoom.ArmExplosionImmediately();
						}
						towerDefenseCharacter.CallDeferred("WakeUp");
						if (instance.hypnoses)
						{
							towerDefenseCharacter.Hypnoses();
						}
						if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
						{
							TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
							if (GodotObject.IsInstanceValid(currentControl))
							{
								int nextSyncId = currentControl.GetNextSyncId();
								currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
								MultiPlayerManager.Instance.SendSpawnCharacterAt(text, vector2I2.X, vector2I2.Y, nextSyncId, 1.0, 1.0, instance.hypnoses);
							}
						}
					}
				}
			}
		}
		Destroy();
	}

	private Vector2I ResolveOpenGridPos(Vector2 logicalPosition)
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return base.gridPos;
		}
		if (towerDefenseManager.CheckMapGridPosIn(base.gridPos))
		{
			return base.gridPos;
		}
		Vector2I mapGridPos = towerDefenseManager.GetMapGridPos(logicalPosition);
		if (towerDefenseManager.CheckMapGridPosIn(mapGridPos))
		{
			return mapGridPos;
		}
		if (towerDefenseManager.CheckMapGridPosIn(_lastBowlingHitGridPos))
		{
			return _lastBowlingHitGridPos;
		}
		Vector2 mapGridSize = towerDefenseManager.GetMapGridSize();
		float num = Mathf.Max(1f, mapGridSize.X * 0.001f);
		float num2 = Mathf.Max(1f, mapGridSize.Y * 0.001f);
		Vector2 pos = new Vector2(Mathf.Clamp(logicalPosition.X, (float)towerDefenseManager.GetMapGroundLeft(), (float)towerDefenseManager.GetMapGroundRight() - num), Mathf.Clamp(logicalPosition.Y, (float)towerDefenseManager.GetMapGroundUp(), (float)towerDefenseManager.GetMapGroundDown() - num2));
		Vector2I mapGridPos2 = towerDefenseManager.GetMapGridPos(pos);
		if (towerDefenseManager.CheckMapGridPosIn(mapGridPos2))
		{
			return mapGridPos2;
		}
		return _lastBowlingHitGridPos;
	}

	private Vector2I FindNearbyEmptyCell(Vector2I startPos, TowerDefensePacketConfig packetConfig)
	{
		Vector2I[] array = new Vector2I[8]
		{
			new Vector2I(-1, 0),
			new Vector2I(1, 0),
			new Vector2I(0, -1),
			new Vector2I(0, 1),
			new Vector2I(-1, -1),
			new Vector2I(1, -1),
			new Vector2I(-1, 1),
			new Vector2I(1, 1)
		};
		foreach (Vector2I vector2I in array)
		{
			Vector2I result = startPos + vector2I;
			if (TowerDefenseManager.Instance.CheckMapGridPosIn(result))
			{
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(result);
				if (GodotObject.IsInstanceValid(mapCell) && mapCell.CanPacketPlant(packetConfig))
				{
					return result;
				}
			}
		}
		return startPos;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "packetBank", packetBank } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		packetBank = (string)data.GetValueOrDefault("packetBank", "WallnutBowling");
	}
}
