using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMagnetnut : TowerDefensePlant
{
	private MagnetComponent magnetComponent;

	private double _breakDownTime = 15.0;

	[Export(PropertyHint.None, "")]
	public double breakDownTime
	{
		get
		{
			return _breakDownTime;
		}
		set
		{
			_breakDownTime = value;
			if (!IsNodeReady())
			{
				return;
			}
			MagnetComponent obj = magnetComponent;
			if (obj != null && !obj.IsReleased)
			{
				MagnetComponent obj2 = magnetComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					magnetComponent.breakDownTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			magnetComponent = componentManager.GetRuntime<MagnetComponent>();
			if (magnetComponent == null)
			{
				GD.PushError("Magnet-nut is missing its Magnet resource runtime.");
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "breakDownTime", breakDownTime } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		breakDownTime = data.GetValueOrDefault("breakDownTime", 15.0).AsDouble();
	}
}
