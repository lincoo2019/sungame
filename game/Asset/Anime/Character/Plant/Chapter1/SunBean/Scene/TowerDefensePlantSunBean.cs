using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunBean : TowerDefensePlant
{
	private ProduceComponent produceComponent;

	private GrowUpComponent growUpComponent;

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
			if (IsNodeReady() && produceComponent != null)
			{
				ProduceComponent obj = produceComponent;
				if (obj != null && !obj.IsReleased)
				{
					produceComponent.produceInterval = (float)value;
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
			if (IsNodeReady() && produceComponent != null)
			{
				ProduceComponent obj = produceComponent;
				if (obj != null && !obj.IsReleased)
				{
					produceComponent.num = value;
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
			if (IsNodeReady() && growUpComponent != null)
			{
				GrowUpComponent obj = growUpComponent;
				if (obj != null && !obj.IsReleased)
				{
					growUpComponent.growUpTime[0] = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			produceComponent = componentManager.GetRuntime<ProduceComponent>();
			growUpComponent = componentManager.GetRuntime<GrowUpComponent>();
			if (growUpComponent != null)
			{
				growUpComponent.OnGrow += GowUp;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (!Engine.IsEditorHint())
		{
			GrowUpComponent obj = growUpComponent;
			if (obj != null && !obj.IsReleased)
			{
				growUpComponent.OnGrow -= GowUp;
			}
		}
	}

	public void GowUp(int reach)
	{
		if (reach == 0)
		{
			sunNum = 25;
			produceComponent.num = sunNum;
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "produceInterval", produceInterval },
			{ "sunNum", sunNum },
			{ "growUpTime", growUpTime }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = data.GetValueOrDefault("produceInterval", 25.0).AsDouble();
		sunNum = data.GetValueOrDefault("sunNum", 25).AsInt32();
		growUpTime = data.GetValueOrDefault("growUpTime", 60.0).AsDouble();
	}
}
