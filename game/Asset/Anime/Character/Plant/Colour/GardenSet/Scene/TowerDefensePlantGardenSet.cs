using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantGardenSet : TowerDefensePlant
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
		AudioManager.Instance.AudioPlay("Diamond");
		Array<TowerDefensePacketConfig> packetConfigCostLowerWithTypeList = TowerDefenseManager.GetPacketConfigCostLowerWithTypeList(999999, new Array<TowerDefenseEnum.PACKET_TYPE> { TowerDefenseEnum.PACKET_TYPE.COLOUR });
		Array<TowerDefensePacketConfig> array = new Array<TowerDefensePacketConfig>();
		foreach (TowerDefensePacketConfig item in packetConfigCostLowerWithTypeList)
		{
			if (item.characterConfig.name != base.config.name)
			{
				array.Add(item);
			}
		}
		if (array.Count != 0)
		{
			TowerDefensePacketConfig towerDefensePacketConfig = array.PickRandom();
			if (instance.hypnoses)
			{
				towerDefensePacketConfig.overrideHypnoses = true;
			}
			SpawnPacket(towerDefensePacketConfig, GetLogicalGlobalPosition(), 15.0, isFall: false);
		}
	}
}
