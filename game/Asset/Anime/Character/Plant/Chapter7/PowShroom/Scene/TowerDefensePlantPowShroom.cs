using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPowShroom : TowerDefensePlant
{
	private CannonComponent _cannonComponent;

	private AttackComponent _attackComponent;

	private GpuParticles2D _fireParticles;

	private double _fireInterval = 2.0;

	private double _restTime = 30.0;

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
			if (IsNodeReady() && _attackComponent != null)
			{
				AttackComponent attackComponent = _attackComponent;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					_attackComponent.attackInterval = (float)value;
				}
			}
		}
	}

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
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_fireParticles = GetNode<GpuParticles2D>("%FireParticles");
			_attackComponent.OnAttack += Attack;
			_attackComponent.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 6.5f);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack -= Attack;
		}
	}

	public void Attack()
	{
		_fireParticles.Restart();
		AudioManager.Instance.AudioPlay("Fume");
		_attackComponent.AttackEventExecute();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["restTime"] = restTime,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		restTime = (data.ContainsKey("restTime") ? data["restTime"].AsDouble() : 30.0);
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
	}
}
