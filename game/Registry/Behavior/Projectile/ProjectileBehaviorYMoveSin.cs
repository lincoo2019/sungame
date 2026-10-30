using Godot;

[GlobalClass]
public partial class ProjectileBehaviorYMoveSin : ProjectileBehaviorDefinition
{
	private sealed class SineKernel : ProjectileBehaviorKernel
	{
		private readonly double[] _timers;

		private readonly double _strength;

		private readonly double _speed;

		public SineKernel(int capacity, double strength, double speed)
		{
			_timers = new double[capacity];
			_strength = strength;
			_speed = speed;
		}

		public override void OnSpawn(int index, ref BulletData bullet)
		{
			_timers[index] = 0.0;
		}

		public override void Process(int index, ref BulletData bullet, double delta)
		{
			double num = _timers[index] + delta;
			_timers[index] = num;
			bullet.pos.Y = bullet.savePos.Y + (float)Mathf.Sin(num * _speed) * (float)_strength;
		}

		public override void OnDespawn(int index, ref BulletData bullet)
		{
			_timers[index] = 0.0;
		}
	}

	[Export(PropertyHint.None, "")]
	public double Strength = 30.0;

	[Export(PropertyHint.None, "")]
	public double Speed = 10.0;

	public ProjectileBehaviorYMoveSin()
	{
	}

	public ProjectileBehaviorYMoveSin(double _strength, double _speed)
	{
		Strength = _strength;
		Speed = _speed;
	}

	public override ProjectileBehaviorKernel CreateBulletFieldKernel(int capacity)
	{
		return new SineKernel(capacity, Strength, Speed);
	}
}
