using System;
using Godot;
using Godot.Collections;

public partial class ShaderEffectComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly ShaderEffectComponent[] _components = new ShaderEffectComponent[1000];

	private readonly ShaderEffectSyncProbeOwner[] _owners = new ShaderEffectSyncProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private ShaderEffectComponentDefinition _definition;

	public override void _Ready()
	{
		Run();
	}

	private void Run()
	{
		bool flag = false;
		OptimizationBatchResult optimizationBatchResult = default(OptimizationBatchResult);
		try
		{
			CreateWorkload();
			flag = RunFunctionalContract() && RestoreBenchmarkFixture();
			PrimePayloadCapacity();
			OptimizationBatchSampler.PrepareForWarmup();
			optimizationBatchResult = Benchmark();
			int num = CountTreeOwners();
			int num2 = CountActiveInstances();
			bool flag2 = flag && num == 1000 && num2 == 1000 && optimizationBatchResult.AllocatedBytes == 0L && optimizationBatchResult.Gen0Collections == 0 && optimizationBatchResult.Gen1Collections == 0 && optimizationBatchResult.Gen2Collections == 0 && optimizationBatchResult.P99Milliseconds < 0.2;
			GD.Print($"SHADER_EFFECT_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("SHADER_EFFECT_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new ShaderEffectComponentDefinition
		{
			ComponentTypeId = "ShaderEffectComponent",
			DefinitionId = "shader_effect.sync.hotpath.runtime",
			InstanceId = "shader_effect.sync.hotpath.runtime",
			WireIndex = 0
		};
		for (int i = 0; i < 1000; i++)
		{
			ShaderEffectSyncProbeOwner shaderEffectSyncProbeOwner = CreateOwner();
			AddChild(shaderEffectSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			ShaderEffectComponent shaderEffectComponent = new ShaderEffectComponent();
			shaderEffectComponent.Bind(componentManager, shaderEffectSyncProbeOwner, _definition);
			shaderEffectComponent.Activate();
			_owners[i] = shaderEffectSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = shaderEffectComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		ShaderEffectComponent shaderEffectComponent = _components[0];
		Dictionary dictionary = shaderEffectComponent.SyncSerialize();
		bool flag = HasFlags(dictionary, 0);
		dictionary["_alive"] = true;
		Dictionary dictionary2 = shaderEffectComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasFlags(dictionary2, 0);
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = shaderEffectComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasFlags(dictionary3, 0);
		int num = ShaderEffectComponent.ShaderEffectFlags["hologram"];
		shaderEffectComponent.SyncDeserialize(new Dictionary { ["effectFlags"] = num });
		bool flag4 = HasFlags(shaderEffectComponent.SyncSerialize(), num) && _owners[0].spriteGroup.Modulate != Colors.White;
		Dictionary payload = shaderEffectComponent.ExportComponentSave();
		shaderEffectComponent.ImportComponentSave(new Dictionary { ["effectFlags"] = 0 }, null);
		bool flag5 = HasFlags(payload, num) && HasFlags(shaderEffectComponent.SyncSerialize(), 0) && _owners[0].spriteGroup.Modulate == Colors.White;
		bool flag6 = RunLifecycleContract();
		return flag && flag2 && flag3 && flag4 && flag5 && flag6;
	}

	private bool RunLifecycleContract()
	{
		ShaderEffectSyncProbeOwner shaderEffectSyncProbeOwner = CreateOwner();
		AddChild(shaderEffectSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		ShaderEffectComponent shaderEffectComponent = new ShaderEffectComponent();
		shaderEffectComponent.Bind(manager, shaderEffectSyncProbeOwner, _definition);
		shaderEffectComponent.Activate();
		shaderEffectComponent.SyncDeserialize(new Dictionary { ["effectFlags"] = ShaderEffectComponent.ShaderEffectFlags["ash"] });
		shaderEffectComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool num = shaderEffectComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasFlags(shaderEffectComponent.SyncSerialize(), ShaderEffectComponent.ShaderEffectFlags["ash"]);
		ShaderEffectSyncProbeOwner shaderEffectSyncProbeOwner2 = CreateOwner();
		AddChild(shaderEffectSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		shaderEffectComponent.Bind(manager2, shaderEffectSyncProbeOwner2, _definition);
		shaderEffectComponent.Activate();
		bool flag = shaderEffectComponent.IsAttached && !shaderEffectComponent.IsReleased && HasFlags(shaderEffectComponent.SyncSerialize(), ShaderEffectComponent.ShaderEffectFlags["ash"]);
		shaderEffectComponent.Release();
		bool flag2 = shaderEffectComponent.IsReleased && HasFlags(shaderEffectComponent.SyncSerialize(), 0);
		shaderEffectSyncProbeOwner.Free();
		shaderEffectSyncProbeOwner2.Free();
		return num && flag && flag2;
	}

	private bool RestoreBenchmarkFixture()
	{
		for (int i = 0; i < 1000; i++)
		{
			ShaderEffectComponent shaderEffectComponent = _components[i];
			shaderEffectComponent.SyncDeserialize(new Dictionary { ["effectFlags"] = 0 });
			shaderEffectComponent.SetAlive(alive: true);
			if (shaderEffectComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasFlags(shaderEffectComponent.SyncSerialize(), 0))
			{
				return false;
			}
		}
		return true;
	}

	private void PrimePayloadCapacity()
	{
		for (int i = 0; i < 240; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				_components[j].SyncSerialize();
			}
		}
	}

	private OptimizationBatchResult Benchmark()
	{
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int i = 0; i < 1200; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			for (int j = 0; j < 1000; j++)
			{
				_components[j].SyncSerialize();
			}
			optimizationBatchSampler.EndSample(startTicks);
		}
		return optimizationBatchSampler.Complete();
	}

	private static ShaderEffectSyncProbeOwner CreateOwner()
	{
		ShaderEffectSyncProbeOwner shaderEffectSyncProbeOwner = new ShaderEffectSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		shaderEffectSyncProbeOwner.instance.character = shaderEffectSyncProbeOwner;
		shaderEffectSyncProbeOwner.spriteGroup = new Node2D();
		shaderEffectSyncProbeOwner.AddChild(shaderEffectSyncProbeOwner.spriteGroup, forceReadableName: false, InternalMode.Disabled);
		return shaderEffectSyncProbeOwner;
	}

	private static bool HasFlags(Dictionary payload, int flags)
	{
		if (payload.Count == 1 && payload.TryGetValue("effectFlags", out var value) && value.VariantType == Variant.Type.Int)
		{
			return value.AsInt32() == flags;
		}
		return false;
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			ShaderEffectSyncProbeOwner obj = _owners[i];
			if (obj != null && obj.IsInsideTree())
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
			ShaderEffectComponent obj = _components[i];
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
			_owners[i]?.Free();
			_components[i] = null;
			_owners[i] = null;
			_managers[i] = null;
		}
	}
}
