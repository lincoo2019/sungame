using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantThreeHybirdPeater : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	[Export(PropertyHint.None, "")]
	public double FireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				_fireComponent.fireInterval = (float)_fireInterval;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int FireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				_fireComponent.fireNum = _fireNum;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.BindCheckRayToGridRow(1, 1);
			_fireComponent.BindCheckRayToGridRow(2, -1);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", FireNum },
			{ "fireInterval", FireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		FireNum = data.GetValueOrDefault("fireNum", Variant.From<int>(1)).AsInt32();
		FireInterval = data.GetValueOrDefault("fireInterval", Variant.From<double>(1.5)).AsDouble();
	}
}
