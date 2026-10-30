using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantLampShroom : TowerDefensePlant
{
	private const float PacketSpawnOffsetX = 52f;

	private const float PacketLaunchSpeedX = 65f;

	private const float PacketLaunchSpeedY = -300f;

	private ExplodeComponent _explodeComponent;

	public Array<Array> spawnPacketList = new Array<Array>
	{
		new Array { "PlantSunFlagbean", "PlantIceSunShroom" },
		new Array { "PlantJalaTorch", "PlantCherryPea" },
		new Array { "PlantHypnoBean", "ZombieNormalDoomShroomBlackHelmet" },
		new Array { "PlantKabbageTail", "PlantTabooBean" },
		new Array { "PlantGarlicChomper", "PlantGarlicChomper" }
	};

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
			AudioManager.Instance.AudioPlay("Plantern");
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

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
		}
	}

	public void Explode()
	{
		AudioManager.Instance.AudioPlay("Diamond");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Array array = spawnPacketList.PickRandom();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(array[0].AsString());
		if (instance.hypnoses)
		{
			packetConfig.overrideHypnoses = true;
		}
		SpawnPacket(packetConfig, logicalGlobalPosition - new Vector2(52f, 0f), 15.0, isFall: false, useCost: false, useRandf: false, new Vector2(-65f, -300f));
		TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig(array[1].AsString());
		if (instance.hypnoses)
		{
			packetConfig2.overrideHypnoses = true;
		}
		SpawnPacket(packetConfig2, logicalGlobalPosition + new Vector2(52f, 0f), 15.0, isFall: false, useCost: false, useRandf: false, new Vector2(65f, -300f));
	}
}
