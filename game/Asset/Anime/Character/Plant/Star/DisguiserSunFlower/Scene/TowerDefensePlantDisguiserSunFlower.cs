using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantDisguiserSunFlower : TowerDefensePlant
{
	private ProduceComponent _produceComponent;

	private int _sunNum = 25;

	private string _produceType = "Sun";

	[Export(PropertyHint.None, "")]
	public int SunNum
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

	[Export(PropertyHint.None, "")]
	public string ProduceType
	{
		get
		{
			return _produceType;
		}
		set
		{
			_produceType = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				_produceComponent.produceType = _produceType;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "sunNum", SunNum } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		SunNum = data.GetValueOrDefault("sunNum", Variant.From<int>(25)).AsInt32();
	}
}
