using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPotSale : TowerDefensePlant
{
	private ProduceComponent _produceComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

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
				_produceComponent.produceInterval = (float)_produceInterval;
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
				_produceComponent.num = _sunNum;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			if (GodotObject.IsInstanceValid(packet) && packet.GetCost() == 0)
			{
				sunNum = 40;
			}
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "produceInterval", produceInterval },
			{ "sunNum", sunNum }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (double)data.GetValueOrDefault("produceInterval", 25.0);
		sunNum = (int)data.GetValueOrDefault("sunNum", 25);
	}
}
