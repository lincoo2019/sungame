using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantChomper : TowerDefensePlant
{
	private ChomperComponent _chomperComponent;

	private AttackComponent _attackComponent;

	private double _chewTime = 30.0;

	[Export(PropertyHint.None, "")]
	public double chewTime
	{
		get
		{
			return _chewTime;
		}
		set
		{
			_chewTime = value;
			if (IsNodeReady() && _chomperComponent != null && !_chomperComponent.IsReleased)
			{
				_chomperComponent.chewTime = (float)value;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_attackComponent.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 1.75f);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["chewTime"] = chewTime };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		chewTime = (data.ContainsKey("chewTime") ? data["chewTime"].AsDouble() : 30.0);
	}
}
