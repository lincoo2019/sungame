using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunPad : TowerDefensePlant
{
	private ProduceComponent produceComponent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> allEventList = new Array<TowerDefenseCharacterEventBase>();

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

	public int coldCheckInterval = 2;

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			produceComponent = componentManager.GetRuntime<ProduceComponent>();
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			base.BatchUpdate(delta);
			if (coldCheckInterval > 0)
			{
				coldCheckInterval--;
				return;
			}
			TowerDefenseExplode.CreateExplode(GetLogicalGlobalPosition(), new Vector2(0.25f, 0.25f), allEventList, null, TowerDefenseEnum.CHARACTER_CAMP.ALL, -1);
			coldCheckInterval = 2;
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
			{ "coldCheckInterval", coldCheckInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = data.GetValueOrDefault("produceInterval", 25.0).AsDouble();
		sunNum = data.GetValueOrDefault("sunNum", 25).AsInt32();
		coldCheckInterval = data.GetValueOrDefault("coldCheckInterval", 2).AsInt32();
	}
}
