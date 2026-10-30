using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;

public partial class DamagePartComponentHotPathRuntimeTest : Node
{
	private readonly struct SegmentResult(bool passed, double p99, long allocatedBytes, int gen0, int dispatched)
	{
		public readonly bool Passed = passed;

		public readonly double P99 = p99;

		public readonly long AllocatedBytes = allocatedBytes;

		public readonly int Gen0 = gen0;

		public readonly int Dispatched = dispatched;
	}

	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private static readonly StringName EmptyPartName = new StringName();

	private readonly DamagePartComponent[] _components = new DamagePartComponent[1000];

	private readonly DamagePartHotPathProbeOwner[] _owners = new DamagePartHotPathProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private readonly double[] _samples = new double[1200];

	public override void _Ready()
	{
		bool flag = false;
		try
		{
			CreateWorkload();
			if (!RunFunctionalContract())
			{
				throw new InvalidOperationException("DamagePart sync/lifecycle contract failed.");
			}
			SegmentResult segmentResult = RunSegment(0);
			SegmentResult segmentResult2 = RunSegment(1);
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			flag = segmentResult.Passed && segmentResult2.Passed && num == 1000 && num2 == 1000;
			GD.Print($"DAMAGE_PART_COMPONENT_HOT_PATH_RESULT passed={flag} instances={1000} warmupSamples={240} measuredSamples={1200} treeOwners={num} activeInstances={num2} syncIdlePassed={segmentResult.Passed} syncIdleP99Ms={segmentResult.P99:F6} syncIdleAllocatedBytes={segmentResult.AllocatedBytes} syncIdleGen0={segmentResult.Gen0} syncIdleDispatchedInstances={segmentResult.Dispatched} guardedEntryPassed={segmentResult2.Passed} guardedEntryP99Ms={segmentResult2.P99:F6} guardedEntryAllocatedBytes={segmentResult2.AllocatedBytes} guardedEntryGen0={segmentResult2.Gen0} guardedEntryDispatchedInstances={segmentResult2.Dispatched}");
		}
		catch (Exception ex)
		{
			GD.PrintErr("DAMAGE_PART_COMPONENT_HOT_PATH_EXCEPTION " + ex);
		}
		finally
		{
			ReleaseWorkload();
		}
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void CreateWorkload()
	{
		DamagePartComponentDefinition definition = new DamagePartComponentDefinition
		{
			ComponentTypeId = "DamagePartComponent",
			DefinitionId = "damage-part.hotpath.runtime",
			InstanceId = "damage-part.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			DamagePartHotPathProbeOwner damagePartHotPathProbeOwner = new DamagePartHotPathProbeOwner
			{
				Name = "DamagePartHotPathOwner" + i
			};
			AddChild(damagePartHotPathProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			DamagePartComponent damagePartComponent = new DamagePartComponent();
			damagePartComponent.Bind(componentManager, damagePartHotPathProbeOwner, definition);
			damagePartComponent.Activate();
			_owners[i] = damagePartHotPathProbeOwner;
			_managers[i] = componentManager;
			_components[i] = damagePartComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		DamagePartComponent damagePartComponent = _components[0];
		Dictionary data = new Dictionary
		{
			["part_velocity_x"] = 12f,
			["part_velocity_y"] = -34f
		};
		damagePartComponent.SyncDeserialize(data);
		Dictionary dictionary = damagePartComponent.SyncSerialize();
		Dictionary dictionary2 = damagePartComponent.SyncSerialize();
		bool num = dictionary == dictionary2 && dictionary2.GetValueOrDefault("part_velocity_x", 0f).AsSingle() == 12f && dictionary2.GetValueOrDefault("part_velocity_y", 0f).AsSingle() == -34f;
		damagePartComponent._syncPartVelocity = Vector2.Zero;
		damagePartComponent.SetAlive(alive: false);
		damagePartComponent.DamagePartCreate(EmptyPartName, null, Vector2.One, keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
		bool flag = !damagePartComponent.Alive;
		damagePartComponent.SetAlive(alive: true);
		damagePartComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		damagePartComponent.Bind(_managers[0], _owners[0], damagePartComponent.ComponentDefinition);
		damagePartComponent.Activate();
		bool flag2 = damagePartComponent.Lifecycle == ComponentRuntimeLifecycle.Active && damagePartComponent.Owner == _owners[0];
		return num && flag && flag2;
	}

	private SegmentResult RunSegment(int mode)
	{
		for (int i = 0; i < 1000; i++)
		{
			_components[i]._syncPartVelocity = Vector2.Zero;
		}
		for (int j = 0; j < 240; j++)
		{
			for (int k = 0; k < 1000; k++)
			{
				Dispatch(k, mode);
			}
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		int num2 = 0;
		for (int l = 0; l < 1200; l++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			for (int m = 0; m < 1000; m++)
			{
				Dispatch(m, mode);
				num2++;
			}
			_samples[l] = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		}
		long num3 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		int num4 = GC.CollectionCount(0) - num;
		System.Array.Sort(_samples);
		double num5 = Percentile(0.99);
		return new SegmentResult(num2 == 1200000 && num3 == 0L && num4 == 0 && num5 < 0.2, num5, num3, num4, num2);
	}

	private void Dispatch(int index, int mode)
	{
		if (mode == 0)
		{
			_components[index].SyncSerialize();
		}
		else
		{
			_components[index].DamagePartCreate(EmptyPartName, null, Vector2.One, keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
		}
	}

	private double Percentile(double fraction)
	{
		int num = Math.Clamp((int)Math.Ceiling(fraction * (double)_samples.Length) - 1, 0, _samples.Length - 1);
		return _samples[num];
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			if (GodotObject.IsInstanceValid(_owners[i]) && _owners[i].IsInsideTree())
			{
				num++;
			}
		}
		return num;
	}

	private int CountActiveInstances()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			DamagePartComponent obj = _components[i];
			if (obj != null && obj.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				num++;
			}
		}
		return num;
	}

	private void ReleaseWorkload()
	{
		for (int i = 0; i < 1000; i++)
		{
			_components[i]?.Release();
			if (GodotObject.IsInstanceValid(_owners[i]))
			{
				_owners[i].Free();
			}
			_components[i] = null;
			_owners[i] = null;
			_managers[i] = null;
		}
	}
}
