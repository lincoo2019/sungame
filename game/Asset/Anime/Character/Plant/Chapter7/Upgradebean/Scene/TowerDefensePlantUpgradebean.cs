using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantUpgradebean : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void Explode()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		int num = (((double)GD.Randf() > 0.5) ? 25 : 50);
		List<TowerDefenseCharacter> characterList = cell.GetCharacterList();
		Array<TowerDefenseCharacter> array = new Array<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item in characterList)
		{
			if (item is TowerDefensePlant && item.config.name != base.config.name && item.camp == base.camp)
			{
				array.Add(item);
			}
		}
		List<TowerDefenseCharacter> list = array.OrderBy(delegate(TowerDefenseCharacter ch)
		{
			TowerDefenseCharacter value;
			bool flag = cell.characterSlotDictionary.TryGetValue(ch, out value) && GodotObject.IsInstanceValid(value);
			if (flag)
			{
				TowerDefenseEnum.PLANTGRIDTYPE plantGridOverrideType = value.config.plantGridOverrideType;
				bool flag2 = (uint)(plantGridOverrideType - 5) <= 1u;
				flag = flag2;
			}
			return (!flag) ? 1 : 0;
		}).ToList();
		if (list.Count == 0)
		{
			Array array2 = (Array?)TowerDefenseManager.GetPacketConfigCostLowerWithTypeList(num, new Array<TowerDefenseEnum.PACKET_TYPE>
			{
				TowerDefenseEnum.PACKET_TYPE.WHITE,
				TowerDefenseEnum.PACKET_TYPE.GOLD,
				TowerDefenseEnum.PACKET_TYPE.DIAMOND,
				TowerDefenseEnum.PACKET_TYPE.COLOUR,
				TowerDefenseEnum.PACKET_TYPE.STAR,
				TowerDefenseEnum.PACKET_TYPE.ORIGINAL
			});
			if (array2.Count > 0)
			{
				TowerDefensePacketConfig towerDefensePacketConfig = array2.PickRandom().As<TowerDefensePacketConfig>();
				if (instance.hypnoses)
				{
					towerDefensePacketConfig.overrideHypnoses = true;
				}
				SpawnPacket(towerDefensePacketConfig, logicalGlobalPosition, 15.0, isFall: false);
			}
			return;
		}
		foreach (TowerDefenseCharacter item2 in list)
		{
			if (!GodotObject.IsInstanceValid(item2))
			{
				continue;
			}
			item2.WakeUp();
			Array array2 = (Array?)TowerDefenseManager.GetPacketConfigCostLowerWithTypeList((int)(item2.cost + (double)num), new Array<TowerDefenseEnum.PACKET_TYPE>
			{
				TowerDefenseEnum.PACKET_TYPE.WHITE,
				TowerDefenseEnum.PACKET_TYPE.GOLD,
				TowerDefenseEnum.PACKET_TYPE.DIAMOND,
				TowerDefenseEnum.PACKET_TYPE.COLOUR,
				TowerDefenseEnum.PACKET_TYPE.STAR,
				TowerDefenseEnum.PACKET_TYPE.ORIGINAL
			});
			Array array3 = new Array();
			foreach (Variant item3 in array2)
			{
				if ((double)((TowerDefensePacketConfig)(GodotObject)item3).characterConfig.cost > item2.cost)
				{
					array3.Add(item3);
				}
			}
			if (array3.Count > 0)
			{
				item2.Destroy();
				TowerDefensePacketConfig towerDefensePacketConfig2 = array3.PickRandom().As<TowerDefensePacketConfig>();
				if (instance.hypnoses)
				{
					towerDefensePacketConfig2.overrideHypnoses = true;
				}
				SpawnPacket(towerDefensePacketConfig2, logicalGlobalPosition, 15.0, isFall: false);
			}
			else if (instance.hypnoses)
			{
				BrainSunCreate(logicalGlobalPosition, num);
			}
			else
			{
				SunCreate(logicalGlobalPosition, num);
			}
		}
	}
}
