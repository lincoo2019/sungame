using System;
using Godot;
using Godot.Collections;

public partial class PuzzleShaderComponentSyncHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly PuzzleShaderComponent[] _components = new PuzzleShaderComponent[1000];

	private readonly PuzzleShaderSyncProbeOwner[] _owners = new PuzzleShaderSyncProbeOwner[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private PuzzleShaderComponentDefinition _definition;

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
			GD.Print($"PUZZLE_SHADER_COMPONENT_SYNC_HOT_PATH_RESULT passed={flag2} functionalPassed={flag} instances={1000} treeOwners={num} activeInstances={num2} warmupSamples={240} measuredSamples={1200} meanMs={optimizationBatchResult.MeanMilliseconds:F6} p50Ms={optimizationBatchResult.P50Milliseconds:F6} p95Ms={optimizationBatchResult.P95Milliseconds:F6} p99Ms={optimizationBatchResult.P99Milliseconds:F6} maxMs={optimizationBatchResult.MaximumMilliseconds:F6} allocatedBytes={optimizationBatchResult.AllocatedBytes} gen0={optimizationBatchResult.Gen0Collections} gen1={optimizationBatchResult.Gen1Collections} gen2={optimizationBatchResult.Gen2Collections}");
			GetTree().Quit((!flag2) ? 2 : 0);
		}
		catch (Exception ex)
		{
			GD.PrintErr("PUZZLE_SHADER_COMPONENT_SYNC_HOT_PATH_EXCEPTION " + ex);
			GetTree().Quit(2);
		}
		finally
		{
			ReleaseWorkload();
		}
	}

	private void CreateWorkload()
	{
		_definition = new PuzzleShaderComponentDefinition
		{
			ComponentTypeId = "PuzzleShaderComponent",
			DefinitionId = "puzzle_shader.sync.hotpath.runtime",
			InstanceId = "puzzle_shader.sync.hotpath.runtime",
			WireIndex = 0,
			enablePuzzleShader = false,
			freezeInIzmIdle = false
		};
		for (int i = 0; i < 1000; i++)
		{
			PuzzleShaderSyncProbeOwner puzzleShaderSyncProbeOwner = CreateOwner();
			AddChild(puzzleShaderSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
			ComponentManager componentManager = new ComponentManager();
			PuzzleShaderComponent puzzleShaderComponent = new PuzzleShaderComponent();
			puzzleShaderComponent.Bind(componentManager, puzzleShaderSyncProbeOwner, _definition);
			puzzleShaderComponent.Activate();
			_owners[i] = puzzleShaderSyncProbeOwner;
			_managers[i] = componentManager;
			_components[i] = puzzleShaderComponent;
		}
	}

	private bool RunFunctionalContract()
	{
		PuzzleShaderComponent puzzleShaderComponent = _components[0];
		Dictionary dictionary = puzzleShaderComponent.SyncSerialize();
		bool flag = HasState(dictionary, idleFrozen: false, shaderApplied: false, alive: true);
		dictionary["_alive"] = true;
		Dictionary dictionary2 = puzzleShaderComponent.SyncSerialize();
		bool flag2 = dictionary == dictionary2 && !dictionary2.ContainsKey("_alive") && HasState(dictionary2, idleFrozen: false, shaderApplied: false, alive: true);
		dictionary2["foreign"] = 99;
		Dictionary dictionary3 = puzzleShaderComponent.SyncSerialize();
		bool flag3 = dictionary == dictionary3 && !dictionary3.ContainsKey("foreign") && HasState(dictionary3, idleFrozen: false, shaderApplied: false, alive: true);
		puzzleShaderComponent.SyncDeserialize(new Dictionary
		{
			["idleFrozen"] = false,
			["alive"] = false
		});
		bool flag4 = HasState(puzzleShaderComponent.SyncSerialize(), idleFrozen: false, shaderApplied: false, alive: false);
		Dictionary payload = puzzleShaderComponent.ExportComponentSave();
		puzzleShaderComponent.ImportComponentSave(new Dictionary
		{
			["idleFrozen"] = false,
			["alive"] = true
		}, null);
		bool flag5 = HasState(payload, idleFrozen: false, shaderApplied: false, alive: false) && HasState(puzzleShaderComponent.SyncSerialize(), idleFrozen: false, shaderApplied: false, alive: true);
		bool flag6 = RunLifecycleContract();
		return flag && flag2 && flag3 && flag4 && flag5 && flag6;
	}

	private bool RunLifecycleContract()
	{
		PuzzleShaderSyncProbeOwner puzzleShaderSyncProbeOwner = CreateOwner();
		AddChild(puzzleShaderSyncProbeOwner, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager = new ComponentManager();
		PuzzleShaderComponent puzzleShaderComponent = new PuzzleShaderComponent();
		puzzleShaderComponent.Bind(manager, puzzleShaderSyncProbeOwner, _definition);
		puzzleShaderComponent.Activate();
		puzzleShaderComponent.SetAlive(alive: false);
		puzzleShaderComponent.Detach(ComponentDetachReason.TemporaryTreeExit);
		bool num = puzzleShaderComponent.Lifecycle == ComponentRuntimeLifecycle.Detached && HasState(puzzleShaderComponent.SyncSerialize(), idleFrozen: false, shaderApplied: false, alive: false);
		PuzzleShaderSyncProbeOwner puzzleShaderSyncProbeOwner2 = CreateOwner();
		AddChild(puzzleShaderSyncProbeOwner2, forceReadableName: false, InternalMode.Disabled);
		ComponentManager manager2 = new ComponentManager();
		puzzleShaderComponent.Bind(manager2, puzzleShaderSyncProbeOwner2, _definition);
		puzzleShaderComponent.Activate();
		bool flag = puzzleShaderComponent.IsAttached && !puzzleShaderComponent.IsReleased && HasState(puzzleShaderComponent.SyncSerialize(), idleFrozen: false, shaderApplied: false, alive: false);
		puzzleShaderComponent.Release();
		bool flag2 = puzzleShaderComponent.IsReleased && HasState(puzzleShaderComponent.SyncSerialize(), idleFrozen: false, shaderApplied: false, alive: false);
		puzzleShaderSyncProbeOwner.Free();
		puzzleShaderSyncProbeOwner2.Free();
		return num && flag && flag2;
	}

	private bool RestoreBenchmarkFixture()
	{
		for (int i = 0; i < 1000; i++)
		{
			PuzzleShaderComponent puzzleShaderComponent = _components[i];
			puzzleShaderComponent.SetAlive(alive: true);
			if (puzzleShaderComponent.Lifecycle != ComponentRuntimeLifecycle.Active || !HasState(puzzleShaderComponent.SyncSerialize(), idleFrozen: false, shaderApplied: false, alive: true))
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

	private static PuzzleShaderSyncProbeOwner CreateOwner()
	{
		PuzzleShaderSyncProbeOwner puzzleShaderSyncProbeOwner = new PuzzleShaderSyncProbeOwner
		{
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			instance = new TowerDefenseCharacterInstance(),
			ProcessMode = ProcessModeEnum.Disabled
		};
		puzzleShaderSyncProbeOwner.instance.character = puzzleShaderSyncProbeOwner;
		return puzzleShaderSyncProbeOwner;
	}

	private static bool HasState(Dictionary payload, bool idleFrozen, bool shaderApplied, bool alive)
	{
		if (payload.Count == 3 && payload.TryGetValue("idleFrozen", out var value) && value.VariantType == Variant.Type.Bool && value.AsBool() == idleFrozen && payload.TryGetValue("shaderApplied", out var value2) && value2.VariantType == Variant.Type.Bool && value2.AsBool() == shaderApplied && payload.TryGetValue("alive", out var value3) && value3.VariantType == Variant.Type.Bool)
		{
			return value3.AsBool() == alive;
		}
		return false;
	}

	private int CountTreeOwners()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			PuzzleShaderSyncProbeOwner obj = _owners[i];
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
			PuzzleShaderComponent obj = _components[i];
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
