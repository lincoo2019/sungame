using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIcenut : TowerDefensePlant
{
	private FireComponent fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

	private string _projectileName = "SnowPea";

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	private static PackedScene _SNOW_FLAKES;

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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireNum = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && this.fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	private static PackedScene SNOW_FLAKES => _SNOW_FLAKES ?? (_SNOW_FLAKES = GD.Load<PackedScene>("uid://b1ba7ajcvcgj8"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		}
	}

	public override async void DestroySet()
	{
		ViewManager.Instance.FullScreenColorBlink(new Color(0.117647f, 0.564706f, 1f, 0.5f), 0.1);
		AudioManager.Instance.AudioPlay("Frozen");
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseExplode.CreateExplode(logicalGlobalPosition, new Vector2(1.5f, 1.5f), eventList, new Array<TowerDefenseCharacter>(), base.camp, -1);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(SNOW_FLAKES, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "SnowPea").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
