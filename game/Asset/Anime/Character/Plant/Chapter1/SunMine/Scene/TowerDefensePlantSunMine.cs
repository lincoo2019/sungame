using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunMine : TowerDefensePlant
{
	public PotatoComponent potatoComponent;

	private ExplodeComponent _explodeComponent;

	private double _readyTime = 15.0;

	[Export(PropertyHint.None, "")]
	public double readyTime
	{
		get
		{
			return _readyTime;
		}
		set
		{
			_readyTime = value;
			if (!IsNodeReady())
			{
				return;
			}
			PotatoComponent obj = potatoComponent;
			if (obj != null && !obj.IsReleased)
			{
				PotatoComponent obj2 = potatoComponent;
				if (obj2 != null && !obj2.IsReleased)
				{
					potatoComponent.readyTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			potatoComponent.readyTime = (float)readyTime;
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.reload = false;
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
	}

	public void ReadyRise()
	{
		potatoComponent.ReadyRise();
	}

	public void Explode()
	{
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		if (instance.hypnoses)
		{
			BrainSunCreate(logicalGlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
		}
		else
		{
			SunCreate(logicalGlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "readyTime", readyTime } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		readyTime = data.GetValueOrDefault("readyTime", 15.0).AsDouble();
	}
}
