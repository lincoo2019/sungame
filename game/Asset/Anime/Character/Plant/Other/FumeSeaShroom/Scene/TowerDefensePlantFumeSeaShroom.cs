using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantFumeSeaShroom : TowerDefensePlant
{
	private AttackComponent _attackComponent;

	private GpuParticles2D _fireParticles;

	private double _fireInterval = 2.0;

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
				_attackComponent.attackInterval = _fireInterval;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_attackComponent.OnAttack += Attack;
			_fireParticles = GetNode<GpuParticles2D>("%FireParticles");
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
		return new Dictionary { { "fireInterval", fireInterval } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 2.0);
	}
}
