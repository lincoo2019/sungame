using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPeaVase : TowerDefensePlant
{
	private static PackedScene _VASE_PLANT_CHUNKS;

	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "Pea";

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> EventList = new Array<TowerDefenseCharacterEventBase>();

	private static PackedScene VASE_PLANT_CHUNKS => _VASE_PLANT_CHUNKS ?? (_VASE_PLANT_CHUNKS = GD.Load<PackedScene>("uid://drx6wjsp5hkv7"));

	[Export(PropertyHint.None, "")]
	public double FireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			IsNodeReady();
		}
	}

	[Export(PropertyHint.None, "")]
	public int FireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady())
			{
				_fireComponent.fireNum = _fireNum;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string ProjectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady())
			{
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (damagePointName == "Damage1" || damagePointName == "Damage2")
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			AudioManager.Instance.AudioPlay("VaseBreaking");
			TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(VASE_PLANT_CHUNKS, base.gridPos);
			towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
			TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(0.5f, 1.5f), EventList, new Array<TowerDefenseCharacter>(), base.camp, -1);
		}
		if (!(damagePointName == "Damage1"))
		{
			if (damagePointName == "Damage2")
			{
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = "PeaVase";
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.baseDamage = 40.0;
				FireNum = 2;
			}
		}
		else
		{
			((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = "PeaVase";
			((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.baseDamage = 40.0;
		}
	}

	public override void DestroySet()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		AudioManager.Instance.AudioPlay("VaseBreaking");
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(VASE_PLANT_CHUNKS, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition(transformPoint) - new Vector2(0f, 30f);
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantPeaShooterSingle");
		if (instance.hypnoses)
		{
			packetConfig.overrideHypnoses = true;
		}
		TowerDefenseManager.Instance.SpawnPacket(packetConfig, logicalGlobalPosition + new Vector2(0f, 0f - (float)base.groundHeight), 15.0, isFall: false);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", FireNum },
			{ "projectileName", ProjectileName },
			{ "fireInterval", FireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		FireNum = data.GetValueOrDefault("fireNum", Variant.From<int>(1)).AsInt32();
		ProjectileName = data.GetValueOrDefault("projectileName", Variant.From<string>("Pea")).AsString();
		FireInterval = data.GetValueOrDefault("fireInterval", Variant.From<double>(1.5)).AsDouble();
	}
}
