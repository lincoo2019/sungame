using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantDisguiserGatling : TowerDefensePlant
{
	public double hpNext;

	public double hpNextInterval;

	public bool over;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			hpNextInterval = instance.hitpoints / 6.0;
			hpNext = instance.hitpoints - hpNextInterval;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint())
		{
			while (instance.hitpoints <= hpNext)
			{
				hpNext -= hpNextInterval;
				Fire();
			}
		}
	}

	public override void DestroySet()
	{
		if (!isShovel && !over)
		{
			over = true;
			int num = 0;
			while (hpNext >= 0.0)
			{
				hpNext -= hpNextInterval;
				num++;
			}
			Fire(num);
		}
	}

	public void Fire(int fireScale = 1)
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ItemGatlingTX");
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		for (int i = 0; i < fireScale; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = (base.HasEconomyOwner ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos, playAudio: false, noLimit: true) : packetConfig.Plant(base.gridPos, playAudio: false, noLimit: true));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				if (instance.hypnoses)
				{
					towerDefenseCharacter.Hypnoses();
				}
				Dictionary spawnState = new Dictionary { ["plant_play_audio"] = false };
				TowerDefenseManager.PublishSpawnedCharacter("ItemGatlingTX", towerDefenseCharacter, useCreate: false, 0.0, walkAfterSpawn: false, "", spawnState);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "hpNext", hpNext },
			{ "hpNextInterval", hpNextInterval },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		hpNext = data.GetValueOrDefault("hpNext", Variant.From<double>(0.0)).AsDouble();
		hpNextInterval = data.GetValueOrDefault("hpNextInterval", Variant.From<double>(0.0)).AsDouble();
		over = data.GetValueOrDefault("over", Variant.From<bool>(false)).AsBool();
	}
}
