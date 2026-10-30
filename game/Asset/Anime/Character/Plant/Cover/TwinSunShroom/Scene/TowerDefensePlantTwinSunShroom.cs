using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantTwinSunShroom : TowerDefensePlant
{
	private ProduceComponent _produceComponent;

	private GrowUpComponent _growUpComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

	private double _growUpTime = 60.0;

	[Export(PropertyHint.None, "")]
	public double produceInterval
	{
		get
		{
			return _produceInterval;
		}
		set
		{
			_produceInterval = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int sunNum
	{
		get
		{
			return _sunNum;
		}
		set
		{
			_sunNum = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.num = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double growUpTime
	{
		get
		{
			return _growUpTime;
		}
		set
		{
			_growUpTime = value;
			if (IsNodeReady() && _growUpComponent != null)
			{
				GrowUpComponent growUpComponent = _growUpComponent;
				if (growUpComponent != null && !growUpComponent.IsReleased)
				{
					_growUpComponent.growUpTime[0] = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			_growUpComponent = componentManager.GetRuntime<GrowUpComponent>();
			if (_growUpComponent != null)
			{
				_growUpComponent.OnGrow += GowUp;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GrowUpComponent growUpComponent = _growUpComponent;
		if (growUpComponent != null && !growUpComponent.IsReleased)
		{
			_growUpComponent.OnGrow -= GowUp;
		}
	}

	public void GowUp(int reach)
	{
		if (reach == 0)
		{
			sunNum = 50;
			ProduceComponent produceComponent = _produceComponent;
			if (produceComponent != null && !produceComponent.IsReleased)
			{
				_produceComponent.num = sunNum;
			}
		}
	}

	public override void Cover(TowerDefenseCharacter character)
	{
		base.Cover(character);
		if (character is TowerDefensePlantSunShroom towerDefensePlantSunShroom)
		{
			transformPoint.Scale = towerDefensePlantSunShroom.transformPoint.Scale;
			shadowComponent.saveShadowScale = towerDefensePlantSunShroom.shadowComponent.saveShadowScale;
			shadowComponent.saveTransformPointScale = towerDefensePlantSunShroom.shadowComponent.saveTransformPointScale;
			ProduceComponent runtime = towerDefensePlantSunShroom.componentManager.GetRuntime<ProduceComponent>();
			GrowUpComponent runtime2 = towerDefensePlantSunShroom.componentManager.GetRuntime<GrowUpComponent>();
			sunNum = ((runtime != null && !runtime.IsReleased) ? runtime.num : towerDefensePlantSunShroom.sunNum);
			ProduceComponent produceComponent = _produceComponent;
			if (produceComponent != null && !produceComponent.IsReleased)
			{
				_produceComponent.num = sunNum;
			}
			GrowUpComponent growUpComponent = _growUpComponent;
			if (growUpComponent != null && !growUpComponent.IsReleased && runtime2 != null && !runtime2.IsReleased)
			{
				_growUpComponent.growUpReach = runtime2.growUpReach;
				_growUpComponent.timer = runtime2.timer;
			}
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		ProduceComponent produceComponent = _produceComponent;
		if (produceComponent != null && !produceComponent.IsReleased)
		{
			_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["produceInterval"] = produceInterval,
			["sunNum"] = sunNum,
			["growUpTime"] = growUpTime
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		sunNum = (data.ContainsKey("sunNum") ? data["sunNum"].AsInt32() : 25);
		growUpTime = (data.ContainsKey("growUpTime") ? data["growUpTime"].AsDouble() : 60.0);
	}
}
