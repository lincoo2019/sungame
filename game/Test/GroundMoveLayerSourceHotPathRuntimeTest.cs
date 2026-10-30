using System;
using Godot;

public partial class GroundMoveLayerSourceHotPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int PoseChangingInstanceCount = 100;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private const double PhysicsDelta = 1.0 / 60.0;

	private const int WalkFrameStart = 44;

	private const int WalkFrameCount = 47;

	private static readonly StringName GroundLayerName = new StringName("_ground");

	private readonly GroundMoveComponent[] _components = new GroundMoveComponent[1000];

	private readonly GroundMoveLayerSourceProbeZombie[] _owners = new GroundMoveLayerSourceProbeZombie[1000];

	private readonly AdobeAnimateSprite[] _sprites = new AdobeAnimateSprite[1000];

	private readonly AdobeAnimateSlot[] _slots = new AdobeAnimateSlot[1000];

	private readonly ComponentManager[] _managers = new ComponentManager[1000];

	private GroundMoveComponentDefinition _definition;

	private Node2D _ownerMount;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		bool flag = false;
		try
		{
			CreateWorkload();
			bool flag2 = ValidateLayerAndLegacyContracts();
			ConfigureRoute(useDirectLayer: false);
			OptimizationBatchResult optimizationBatchResult = Measure();
			ConfigureRoute(useDirectLayer: true);
			OptimizationBatchResult optimizationBatchResult2 = Measure();
			int num = CountTreeOwners();
			int num2 = CountActiveComponents();
			bool flag3 = optimizationBatchResult.AllocatedBytes == 0L && optimizationBatchResult2.AllocatedBytes == 0;
			bool value = optimizationBatchResult2.MeanMilliseconds < optimizationBatchResult.MeanMilliseconds;
			bool flag4 = optimizationBatchResult2.MeanMilliseconds <= optimizationBatchResult.MeanMilliseconds * 1.02;
			bool flag5 = optimizationBatchResult2.P99Milliseconds < 0.2;
			flag = flag2 && num == 1000 && num2 == 1000 && flag3 && flag4 && flag5;
			GD.Print("GROUND_MOVE_LAYER_SOURCE_HOT_PATH_RESULT " + $"passed={flag} functional={flag2} improved={value} " + $"notRegressed={flag4} " + $"withinBudget={flag5} instances={1000} " + $"poseChangingInstances={100} " + $"treeOwners={num} " + $"activeComponents={num2} warmupSamples={240} " + $"measuredSamples={1200} " + $"legacyMeanMs={optimizationBatchResult.MeanMilliseconds:F6} " + $"legacyP95Ms={optimizationBatchResult.P95Milliseconds:F6} " + $"legacyP99Ms={optimizationBatchResult.P99Milliseconds:F6} " + $"legacyAllocatedBytes={optimizationBatchResult.AllocatedBytes} " + $"directMeanMs={optimizationBatchResult2.MeanMilliseconds:F6} " + $"directP95Ms={optimizationBatchResult2.P95Milliseconds:F6} " + $"directP99Ms={optimizationBatchResult2.P99Milliseconds:F6} " + $"directAllocatedBytes={optimizationBatchResult2.AllocatedBytes}");
		}
		catch (Exception ex)
		{
			GD.PrintErr("GROUND_MOVE_LAYER_SOURCE_HOT_PATH_EXCEPTION " + ex);
		}
		finally
		{
			ReleaseWorkload();
			GetTree().Quit((!flag) ? 2 : 0);
		}
	}

	private void CreateWorkload()
	{
		AdobeAnimateData adobeAnimateData = GD.Load<AdobeAnimateData>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/ZombieNormal.tres");
		if (adobeAnimateData == null)
		{
			throw new InvalidOperationException("Normal zombie animation data could not be loaded.");
		}
		_definition = new GroundMoveComponentDefinition
		{
			ComponentTypeId = "GroundMoveComponent",
			DefinitionId = "builtin.component.ground_move.layer_hot_path",
			InstanceId = "character.ground_move",
			WireIndex = 0,
			InitiallyAlive = true,
			groundLayerName = GroundLayerName,
			groundLayerOffset = Vector2.Zero,
			groundSlotPath = new NodePath("GroundSlot"),
			delay = 0f,
			moveYAxis = false,
			syncNetworkPosition = false
		};
		_ownerMount = new Node2D
		{
			Name = "GroundMoveLayerSourceOwners"
		};
		AddChild(_ownerMount, forceReadableName: false, InternalMode.Disabled);
		for (int i = 0; i < 1000; i++)
		{
			GroundMoveLayerSourceProbeZombie groundMoveLayerSourceProbeZombie = new GroundMoveLayerSourceProbeZombie
			{
				Name = $"GroundMoveOwner{i}",
				ProcessMode = ProcessModeEnum.Disabled
			};
			Node2D node2D = new Node2D
			{
				Name = "SpriteGroup",
				Scale = Vector2.Zero
			};
			Marker2D marker2D = new Marker2D
			{
				Name = "TransformPoint"
			};
			AdobeAnimateSprite adobeAnimateSprite = new AdobeAnimateSprite
			{
				Name = "ZombieNormal",
				flashAnimeData = adobeAnimateData,
				offset = new Vector2(-40f, -80f),
				trueFrameRate = 180.0,
				clip = "Walk1",
				frameIndex = 44,
				elapsedTimer = 0.0,
				pause = false
			};
			AdobeAnimateSlot adobeAnimateSlot = new AdobeAnimateSlot
			{
				Name = "GroundSlot",
				followSlotId = 1,
				drawLayerId = 1,
				updateAllFrame = false
			};
			_ownerMount.AddChild(groundMoveLayerSourceProbeZombie, forceReadableName: false, InternalMode.Disabled);
			groundMoveLayerSourceProbeZombie.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			node2D.AddChild(marker2D, forceReadableName: false, InternalMode.Disabled);
			marker2D.AddChild(adobeAnimateSprite, forceReadableName: false, InternalMode.Disabled);
			adobeAnimateSprite.AddChild(adobeAnimateSlot, forceReadableName: false, InternalMode.Disabled);
			groundMoveLayerSourceProbeZombie.spriteGroup = node2D;
			groundMoveLayerSourceProbeZombie.transformPoint = marker2D;
			groundMoveLayerSourceProbeZombie.sprite = adobeAnimateSprite;
			ComponentManager componentManager = new ComponentManager();
			GroundMoveComponent groundMoveComponent = new GroundMoveComponent();
			groundMoveComponent.Bind(componentManager, groundMoveLayerSourceProbeZombie, _definition);
			groundMoveComponent.Activate();
			groundMoveComponent.SetAlive(true);
			groundMoveComponent.delay = 0f;
			groundMoveLayerSourceProbeZombie.groundMoveComponent = groundMoveComponent;
			_owners[i] = groundMoveLayerSourceProbeZombie;
			_sprites[i] = adobeAnimateSprite;
			_slots[i] = adobeAnimateSlot;
			_managers[i] = componentManager;
			_components[i] = groundMoveComponent;
		}
	}

	private bool ValidateLayerAndLegacyContracts()
	{
		AdobeAnimateSprite adobeAnimateSprite = _sprites[0];
		AdobeAnimateSlot adobeAnimateSlot = _slots[0];
		GroundMoveComponent groundMoveComponent = _components[0];
		if (!adobeAnimateSprite.TryResolveLayerIdForRender(GroundLayerName, out var layerId) || layerId != 0 || !groundMoveComponent.UsesGroundLayerSource || groundMoveComponent.GroundLayerId != layerId)
		{
			return false;
		}
		for (int i = 0; i < 47; i++)
		{
			adobeAnimateSprite.frameIndex = 44 + i;
			adobeAnimateSprite.elapsedTimer = 0.0;
			if (!adobeAnimateSprite.TryGetManagedLayerPositionForRender(layerId, Vector2.Zero, out var position) || !adobeAnimateSprite.TryGetManagedSlotPositionForRender(adobeAnimateSlot, out var position2) || !position.IsEqualApprox(position2))
			{
				return false;
			}
		}
		groundMoveComponent.groundLayerName = null;
		bool num = groundMoveComponent.ResolveMovementSource() && !groundMoveComponent.UsesGroundLayerSource && groundMoveComponent.groundNode == adobeAnimateSlot;
		groundMoveComponent.groundLayerName = "__missing_ground_layer__";
		bool flag = groundMoveComponent.ResolveMovementSource() && !groundMoveComponent.UsesGroundLayerSource && groundMoveComponent.groundNode == adobeAnimateSlot;
		groundMoveComponent.groundLayerName = GroundLayerName;
		bool flag2 = groundMoveComponent.ResolveMovementSource() && groundMoveComponent.UsesGroundLayerSource && groundMoveComponent.GroundLayerId == 0;
		return num && flag && flag2;
	}

	private void ConfigureRoute(bool useDirectLayer)
	{
		for (int i = 0; i < 1000; i++)
		{
			GroundMoveComponent obj = _components[i];
			obj.groundLayerName = (useDirectLayer ? GroundLayerName : null);
			if (!obj.ResolveMovementSource())
			{
				throw new InvalidOperationException($"GroundMove source {i} failed to resolve.");
			}
			obj.delay = 0f;
			obj.RefreshDirectionCache();
		}
	}

	private OptimizationBatchResult Measure()
	{
		for (int i = 0; i < 240; i++)
		{
			Dispatch(i);
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			Dispatch(240 + j);
			optimizationBatchSampler.EndSample(startTicks);
		}
		return optimizationBatchSampler.Complete();
	}

	private void Dispatch(int sample)
	{
		for (int i = 0; i < 100; i++)
		{
			_sprites[i].frameIndex = 44 + (sample + i) % 47;
			_sprites[i].elapsedTimer = 0.0;
		}
		ulong physicsFrame = (ulong)Math.Max(0, sample);
		for (int j = 0; j < 1000; j++)
		{
			_components[j].PhysicsProcess(1.0 / 60.0, physicsFrame);
		}
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

	private int CountActiveComponents()
	{
		int num = 0;
		for (int i = 0; i < 1000; i++)
		{
			GroundMoveComponent obj = _components[i];
			if (obj != null && obj.Alive && _components[i].Lifecycle == ComponentRuntimeLifecycle.Active)
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
				_owners[i].groundMoveComponent = null;
				_owners[i].Free();
			}
			_components[i] = null;
			_owners[i] = null;
			_sprites[i] = null;
			_slots[i] = null;
			_managers[i] = null;
		}
		_definition?.Dispose();
		if (GodotObject.IsInstanceValid(_ownerMount))
		{
			_ownerMount.Free();
		}
		_definition = null;
		_ownerMount = null;
	}
}
