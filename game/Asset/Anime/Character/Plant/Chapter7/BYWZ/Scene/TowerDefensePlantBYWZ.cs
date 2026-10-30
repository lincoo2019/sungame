using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBYWZ : TowerDefensePlant
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
		TowerDefensePacketConfig towerDefensePacketConfig = TowerDefenseManager.GetPacketConfigCostLowerWithTypeList(100, new Array<TowerDefenseEnum.PACKET_TYPE>
		{
			TowerDefenseEnum.PACKET_TYPE.WHITE,
			TowerDefenseEnum.PACKET_TYPE.ORIGINAL
		}).PickRandom();
		if (instance.hypnoses)
		{
			towerDefensePacketConfig.overrideHypnoses = true;
		}
		SpawnPacket(towerDefensePacketConfig, GetLogicalGlobalPosition(), 15.0, isFall: false);
	}
}
