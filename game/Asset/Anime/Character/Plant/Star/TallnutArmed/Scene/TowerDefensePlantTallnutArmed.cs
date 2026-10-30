using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantTallnutArmed : TowerDefensePlant, INetworkSpawnStateReceiver
{
	private static PackedScene _TALLNUT_ARMED;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public int dieNum;

	private bool _stageHitpointsApplied;

	private static PackedScene TALLNUT_ARMED => _TALLNUT_ARMED ?? (_TALLNUT_ARMED = GD.Load<PackedScene>("uid://dw1omhgw0ymw6"));

	public override void IdleEntered()
	{
		base.IdleEntered();
		ApplyStageHitpointsOnce();
		switch (dieNum)
		{
		case 0:
			base.sprite.SetAnimation("IdleA");
			break;
		case 1:
			base.sprite.SetAnimation("CrakedA", loop: false);
			base.sprite.AddAnimation("IdleB", 0.0);
			break;
		case 2:
			base.sprite.SetAnimation("CrakedB", loop: false);
			base.sprite.AddAnimation("IdleC", 0.0);
			break;
		case 3:
			base.sprite.SetAnimation("CrakedC", loop: false);
			base.sprite.AddAnimation("IdleD", 0.0);
			break;
		}
	}

	private void ApplyStageHitpointsOnce()
	{
		if (!_stageHitpointsApplied)
		{
			_stageHitpointsApplied = true;
			switch (dieNum)
			{
			case 1:
				instance.hitpointsSave -= 2000.0;
				instance.hitpoints -= 2000.0;
				break;
			case 2:
				instance.hitpointsSave -= 4000.0;
				instance.hitpoints -= 4000.0;
				break;
			case 3:
				instance.hitpointsSave -= 6000.0;
				instance.hitpoints -= 6000.0;
				break;
			}
		}
	}

	public override void DestroySet()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		base.DestroySet();
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(1.25f, 0.25f), eventList, new Array<TowerDefenseCharacter>(), base.camp, -1);
		}
		if (dieNum == 3)
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(TALLNUT_ARMED, base.gridPos, "CrakedD");
			towerDefenseEffectSpriteOnce.GlobalPosition = logicalGlobalPosition + new Vector2(0f, (float)base.groundHeight);
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			if (!TowerDefenseManager.HasGameplayAuthority)
			{
				return;
			}
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantTallnutArmed");
			if (!GodotObject.IsInstanceValid(packetConfig))
			{
				return;
			}
			TowerDefensePlant towerDefensePlant = (TowerDefensePlant)(base.HasEconomyOwner ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos, playAudio: true, noLimit: true, skipPlacementCheck: true) : packetConfig.Plant(base.gridPos, playAudio: true, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true));
			if (GodotObject.IsInstanceValid(towerDefensePlant))
			{
				((TowerDefensePlantTallnutArmed)towerDefensePlant).dieNum = dieNum + 1;
				if (instance.hypnoses)
				{
					towerDefensePlant.Hypnoses();
				}
				Dictionary spawnState = new Dictionary { ["dieNum"] = dieNum + 1 };
				TowerDefenseManager.PublishSpawnedCharacter("PlantTallnutArmed", towerDefensePlant, useCreate: false, 0.0, walkAfterSpawn: false, "", spawnState);
			}
		}
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		int num = dieNum;
		ApplyNetworkStage(data);
		if (num != dieNum)
		{
			_stageHitpointsApplied = false;
		}
		if (IsNodeReady())
		{
			ApplyStageHitpointsOnce();
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary { ["dieNum"] = dieNum };
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		ApplyNetworkStage(data);
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return dieNum;
	}

	private void ApplyNetworkStage(Dictionary data)
	{
		if (data != null)
		{
			int networkStage = GetNetworkStage(data);
			bool num = networkStage != dieNum;
			dieNum = networkStage;
			if (num && IsNodeReady() && GodotObject.IsInstanceValid(base.sprite))
			{
				ApplySyncedStageVisual();
			}
		}
	}

	private int GetNetworkStage(Dictionary data)
	{
		if (data != null)
		{
			return Math.Clamp(data.GetValueOrDefault("dieNum", dieNum).AsInt32(), 0, 3);
		}
		return dieNum;
	}

	private void ApplySyncedStageVisual()
	{
		switch (dieNum)
		{
		case 0:
			base.sprite.SetAnimation("IdleA");
			break;
		case 1:
			base.sprite.SetAnimation("IdleB");
			break;
		case 2:
			base.sprite.SetAnimation("IdleC");
			break;
		case 3:
			base.sprite.SetAnimation("IdleD");
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "dieNum", dieNum } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		dieNum = Math.Clamp(data.GetValueOrDefault("dieNum", Variant.From<int>(0)).AsInt32(), 0, 3);
		_stageHitpointsApplied = true;
		if (IsNodeReady() && GodotObject.IsInstanceValid(base.sprite))
		{
			ApplySyncedStageVisual();
		}
	}
}
