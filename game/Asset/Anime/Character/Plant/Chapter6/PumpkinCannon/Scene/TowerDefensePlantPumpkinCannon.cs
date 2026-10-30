using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPumpkinCannon : TowerDefensePlant
{
	public CannonComponent cannonComponent;

	private double _restTime = 30.0;

	[Export(PropertyHint.None, "")]
	public double restTime
	{
		get
		{
			return _restTime;
		}
		set
		{
			_restTime = value;
			if (IsNodeReady() && cannonComponent != null)
			{
				CannonComponent obj = cannonComponent;
				if (obj != null && !obj.IsReleased)
				{
					cannonComponent.restTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			cannonComponent = componentManager.GetRuntime<CannonComponent>();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "restTime", restTime } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		restTime = data.GetValueOrDefault("restTime", 30).AsDouble();
	}
}
