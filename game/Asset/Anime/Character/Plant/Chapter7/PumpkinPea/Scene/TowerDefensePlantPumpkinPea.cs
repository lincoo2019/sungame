using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPumpkinPea : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

	private string _projectileName = "Pea";

	[Export(PropertyHint.None, "")]
	public int level = 1;

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
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
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
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
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
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (!inGame && !editorMapPreviewMode)
			{
				((PumpkinPeaSprite)base.sprite).back.ZIndex = 0;
			}
		}
	}

	public override void Cover(TowerDefenseCharacter character)
	{
		if (character.config.name == "PlantPumpkinPea" && character is TowerDefensePlantPumpkinPea towerDefensePlantPumpkinPea)
		{
			level = Mathf.Clamp(towerDefensePlantPumpkinPea.level + 1, 1, 3);
			LevelSet(level);
		}
	}

	public void LevelSet(int lv)
	{
		switch (lv)
		{
		case 2:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_helmet", "Upgrade2_barrel" }, open: true);
			instance.hitpoints += 2000.0;
			instance.hitpointsSave += 2000.0;
			_fireComponent.fireEventName = "fire&fire2";
			break;
		case 3:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_eyebrow2", "Upgrade1_helmet", "Upgrade2_helmet", "Upgrade2_barrel", "Upgrade2_barrel2", "Upgrade2_face" }, open: true);
			instance.hitpoints += 4000.0;
			instance.hitpointsSave += 4000.0;
			_fireComponent.fireEventName = "fire&fire2&fire3";
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["level"] = level,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "Pea");
		level = ((!data.ContainsKey("level")) ? 1 : data["level"].AsInt32());
		if (level > 1)
		{
			LevelSetVisual(level);
		}
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
	}

	public void LevelSetVisual(int lv)
	{
		switch (lv)
		{
		case 2:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_helmet", "Upgrade2_barrel" }, open: true);
			_fireComponent.fireEventName = "fire&fire2";
			break;
		case 3:
			base.sprite.SetFliters(new Array { "Upgrade1_eyebrow", "Upgrade1_helmet", "Upgrade1_helmet2", "Upgrade2_helmet", "Upgrade2_barrel", "Upgrade2_barrel2", "Upgrade2_face" }, open: true);
			_fireComponent.fireEventName = "fire&fire2&fire3";
			break;
		}
	}
}
