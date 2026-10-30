using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantDoomCobCannon : TowerDefensePlant
{
	private CannonComponent _cannonComponent;

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
			if (IsNodeReady() && _cannonComponent != null)
			{
				CannonComponent cannonComponent = _cannonComponent;
				if (cannonComponent != null && !cannonComponent.IsReleased)
				{
					_cannonComponent.restTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_cannonComponent = componentManager.GetRuntime<CannonComponent>();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["restTime"] = restTime };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		restTime = (data.ContainsKey("restTime") ? data["restTime"].AsDouble() : 30.0);
	}
}
