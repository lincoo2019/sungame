using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantFireSnowPea : TowerDefensePlant
{
	private FireComponent fireComponent;

	private MousePressComponent _mousePressComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireNum = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_mousePressComponent = componentManager.GetRuntime<MousePressComponent>();
			_mousePressComponent.OnPressed += Flip;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (_mousePressComponent != null)
		{
			_mousePressComponent.OnPressed -= Flip;
		}
	}

	public void Flip(Vector2 pos)
	{
		base.Scale = new Vector2(0f - base.Scale.X, base.Scale.Y);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
