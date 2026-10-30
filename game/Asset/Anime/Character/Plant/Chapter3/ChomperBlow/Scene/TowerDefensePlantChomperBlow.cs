using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantChomperBlow : TowerDefensePlant
{
	private ChomperComponent chomperComponent;

	private BloverComponent bloverComponent;

	private AttackComponent attackComponent;

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
			if (IsNodeReady() && chomperComponent != null && !chomperComponent.IsReleased)
			{
				chomperComponent.chewTime = (float)value;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			bloverComponent = componentManager.GetRuntime<BloverComponent>();
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 1.75f);
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "blow")
		{
			bloverComponent.ExecuteLine(base.gridPos.Y);
			BattleEventBus.Instance.EmitBlowLineEffectEmit(base.gridPos.Y);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "chewTime", chewTime } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		chewTime = data.GetValueOrDefault("chewTime", 30.0).AsDouble();
	}
}
