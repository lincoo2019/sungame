using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantWinterMelonShroom : TowerDefensePlant
{
	private FireComponent fireComponent;

	private AttackComponent attackComponent;

	private GpuParticles2D fireParticles;

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
			if (IsNodeReady() && attackComponent != null)
			{
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased)
				{
					attackComponent.attackInterval = (float)value;
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
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			fireParticles = GetNode<GpuParticles2D>("%FireParticles");
			attackComponent.OnAttack += Attack;
			attackComponent.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 6.5f);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.OnAttack -= Attack;
		}
	}

	public void Attack()
	{
		fireParticles.Restart();
		AudioManager.Instance.AudioPlay("Fume");
		attackComponent.AttackEventExecute();
		BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
		{
			gridYOverride = base.gridPos.Y,
			flipXOverride = (base.Scale.X < 0f)
		};
		fireComponent.CreateProjectileByData(0, new Vector2(600f, 0f), fireComponent.fireCheckList[0].projectile.GetProjetile(), -1, base.camp, Vector2.Zero, overrides);
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { { "fireInterval", fireInterval } };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireInterval = data.GetValueOrDefault("fireInterval", 2.0).AsDouble();
	}
}
