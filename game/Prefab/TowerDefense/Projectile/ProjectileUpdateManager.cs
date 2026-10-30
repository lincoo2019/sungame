using Godot;

public partial class ProjectileUpdateManager : Node
{
	public static ProjectileUpdateManager Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		base.ProcessPriority = 2147483646;
	}

	public override void _PhysicsProcess(double delta)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		ulong physicsFrames = Engine.GetPhysicsFrames();
		BulletField bulletField = (GodotObject.IsInstanceValid(BulletField.Instance) ? BulletField.Instance : BulletField.EnsureMountedOnCharacterNode());
		int items = 0;
		if (GodotObject.IsInstanceValid(bulletField))
		{
			TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
			bulletField.ProcessQueuedRenderTemplateWarmups(1);
			bulletField.UpdateSimulation(delta, physicsFrames);
			items = bulletField.ActiveCount;
			TowerDefensePerfProfiler.EndSpikeProbe("projectile.updateManager", in probe, items);
		}
		TowerDefensePerfProfiler.End("projectile.updateManager", startTicks, items);
		TowerDefensePerfProfiler.DumpIfNeeded();
	}

	public override void _Process(double delta)
	{
		BulletField instance = BulletField.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.PublishRenderState(delta, Engine.GetPhysicsFrames());
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: not false, Echo: false } inputEventKey && inputEventKey.Keycode == Key.Key9)
		{
			bool flag = (TowerDefensePerfProfiler.DetailedHotPathMetrics = (TowerDefensePerfProfiler.Enabled = !TowerDefensePerfProfiler.Enabled));
			if (flag)
			{
				TowerDefensePerfProfiler.MaxMetricsPerDump = 48;
			}
			TowerDefensePerfProfiler.Reset();
			GD.Print($"[TDPerf] Enabled={flag} detailed={TowerDefensePerfProfiler.DetailedHotPathMetrics} dumpFrames={TowerDefensePerfProfiler.DumpIntervalFrames} maxMetrics={TowerDefensePerfProfiler.MaxMetricsPerDump}");
		}
	}

	public void Clear()
	{
		if (GodotObject.IsInstanceValid(BulletField.Instance))
		{
			BulletField.Instance.ClearActiveBullets();
		}
	}
}
