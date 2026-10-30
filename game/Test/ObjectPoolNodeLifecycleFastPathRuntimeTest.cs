using System;
using System.Threading.Tasks;
using Godot;

public sealed partial class ObjectPoolNodeLifecycleFastPathRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private readonly Node[] _probeNodes = new Node[1000];

	private bool _baselineMode;

	private Node _probeParent;

	private Node _productionParent;

	private Node _fallbackParent;

	private int _checks;

	private int _failures;

	private bool _typedProductionContract;

	private bool _coinLifecyclePassed;

	private bool _sunLifecyclePassed;

	private bool _damageLifecyclePassed;

	private bool _customFallbackPassed;

	private bool _scriptSubclassFallbackPassed;

	private bool _callbackOrderPassed;

	private bool _deferredBatchPassed;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		OS.LowProcessorUsageMode = false;
		_baselineMode = string.Equals(System.Environment.GetEnvironmentVariable("PVZHE_OBJECT_POOL_LIFECYCLE_BASELINE"), "1", StringComparison.Ordinal);
		_probeParent = new Node
		{
			Name = "LifecycleProbeNodes"
		};
		_productionParent = new Node2D
		{
			Name = "ProductionPoolParent"
		};
		_fallbackParent = new Node
		{
			Name = "CustomFallbackParent"
		};
		AddChild(_probeParent, forceReadableName: false, InternalMode.Disabled);
		AddChild(_productionParent, forceReadableName: false, InternalMode.Disabled);
		AddChild(_fallbackParent, forceReadableName: false, InternalMode.Disabled);
		Callable.From(Run).CallDeferred();
	}

	private async void Run()
	{
		ObjectManager manager = null;
		try
		{
			PrepareProbeNodes();
			MeasureRefreshDispatch();
			MeasureRecycleDispatch();
			if (!_baselineMode)
			{
				manager = ObjectManager.Instance;
				Check(GodotObject.IsInstanceValid(manager), "ObjectManager autoload must be available.");
				if (GodotObject.IsInstanceValid(manager))
				{
					manager.SetPhysicsProcess(enable: false);
					await ValidateProductionLifecycle();
					await ValidateCustomFallback();
					await ValidateScriptSubclassFallback();
				}
			}
		}
		catch (Exception ex)
		{
			_failures++;
			GD.PushError(ex.ToString());
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.SetPhysicsProcess(enable: true);
			}
			for (int i = 0; i < _probeNodes.Length; i++)
			{
				if (GodotObject.IsInstanceValid(_probeNodes[i]))
				{
					_probeNodes[i].QueueFree();
				}
			}
			bool flag = _failures == 0;
			GD.Print($"OBJECT_POOL_NODE_LIFECYCLE_RESULT passed={flag} baselineMode={_baselineMode} checks={_checks} failures={_failures} typedProduction={_typedProductionContract} coinLifecycle={_coinLifecyclePassed} sunLifecycle={_sunLifecyclePassed} damageLifecycle={_damageLifecyclePassed} customFallback={_customFallbackPassed} scriptSubclassFallback={_scriptSubclassFallbackPassed} callbackOrder={_callbackOrderPassed} deferredBatch={_deferredBatchPassed}");
			GetTree().Quit((!flag) ? 2 : 0);
		}
	}

	private void PrepareProbeNodes()
	{
		for (int i = 0; i < 1000; i++)
		{
			Node node = (_baselineMode ? ((Node)new ObjectPoolDynamicLifecycleProbeNode()) : ((Node)new ObjectPoolTypedLifecycleProbeNode()));
			_probeParent.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_probeNodes[i] = node;
		}
		Check(_probeParent.GetChildCount() == 1000, "Exactly 1000 real Godot Node probes must be active.");
	}

	private void MeasureRefreshDispatch()
	{
		ResetRefreshCounter();
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			DispatchRefreshBatch();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		ResetRefreshCounter();
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			DispatchRefreshBatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		bool functionalPassed = ReadRefreshCounter() == 1200000;
		PrintPerformanceResult("object-pool-node-refresh-static-lifecycle", "refresh-from-pool", in result, functionalPassed);
	}

	private void MeasureRecycleDispatch()
	{
		ResetRecycleCounter();
		OptimizationBatchSampler.PrepareForWarmup();
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			DispatchRecycleBatch();
			OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		ResetRecycleCounter();
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			DispatchRecycleBatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		bool functionalPassed = ReadRecycleCounter() == 1200000;
		PrintPerformanceResult("object-pool-node-recycle-static-lifecycle", "recycle-to-pool", in result, functionalPassed);
	}

	private void DispatchRefreshBatch()
	{
		for (int i = 0; i < 1000; i++)
		{
			PoolConfig.DispatchPopLifecycle(_probeNodes[i], PoolConfig.RefreshCallbackName);
		}
	}

	private void DispatchRecycleBatch()
	{
		for (int i = 0; i < 1000; i++)
		{
			PoolConfig.DispatchPushLifecycle(_probeNodes[i], PoolConfig.RecycleCallbackName);
		}
	}

	private void PrintPerformanceResult(string task, string phase, in OptimizationBatchResult result, bool functionalPassed)
	{
		OptimizationResultIdentity identity = new OptimizationResultIdentity(task, OptimizationWorkloadKind.BareComponent, "builtin.pool.node", "res://Test/ObjectPoolNodeLifecycleFastPathRuntimeTest.tscn", "IObjectPoolLifecycle", "standard-refresh-recycle", _baselineMode ? "dynamic-fallback" : "typed-static", "none", "none", phase, OptimizationScheduleKind.BackToBack, "headless-mobile", 60, 240);
		bool flag = OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed, 1000, 240, 1000, 1000, in identity);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, functionalPassed, flag, 1000, 240, 1000, 1000));
		Check(functionalPassed, task + " did not invoke exactly 1000 callbacks per sample.");
		if (!_baselineMode)
		{
			Check(flag, task + " must remain below 0.2 ms P99 with zero steady allocation and GC.");
		}
	}

	private async Task ValidateProductionLifecycle()
	{
		_typedProductionContract = typeof(IObjectPoolLifecycle).IsAssignableFrom(typeof(TowerDefenseCoinBase)) && typeof(IObjectPoolLifecycle).IsAssignableFrom(typeof(TowerDefenseSunBase)) && typeof(IObjectPoolLifecycle).IsAssignableFrom(typeof(DamagePartDrop));
		Check(_typedProductionContract, "Coin, Sun and DamagePartDrop must use the typed lifecycle.");
		await ValidateCoinLifecycle();
		await ValidateSunLifecycle();
		await ValidateDamagePartLifecycle();
	}

	private async Task ValidateCoinLifecycle()
	{
		PoolConfig pool = GetPool(ObjectManagerConfig.OBJECT.COIN_SILVER);
		await ResetPool(pool);
		TowerDefenseCoinBase coin = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.COIN_SILVER, _productionParent) as TowerDefenseCoinBase;
		Check(GodotObject.IsInstanceValid(coin), "Real Coin pool pop failed.");
		if (GodotObject.IsInstanceValid(coin))
		{
			ulong instanceId = coin.GetInstanceId();
			bool firstRefresh = coin.GetParent() == _productionParent && coin.IsInGroup("Coin") && !coin.isCollect && !coin.die && !coin.disabledInput && !coin.over && coin.spriteNode.Modulate.A == 1f;
			coin.isCollect = true;
			coin.die = true;
			coin.disabledInput = true;
			coin.over = true;
			coin.autoCollect = true;
			coin.spriteNode.Modulate = new Color(coin.spriteNode.Modulate, 0.2f);
			coin.moveComponent.SetVelocity(new Vector2(10f, -20f));
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_SILVER, coin);
			bool immediateRecycle = coin.GetParent() == _productionParent && !coin.IsInGroup("Coin") && !coin.autoCollect && !coin.moveComponent.HasActiveMovement && !coin.IsPhysicsProcessing();
			await FlushDeferredWork();
			bool flag = coin.GetParent() == null && pool.stack.Count == 1;
			TowerDefenseCoinBase towerDefenseCoinBase = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.COIN_SILVER, _productionParent) as TowerDefenseCoinBase;
			bool flag2 = towerDefenseCoinBase == coin && towerDefenseCoinBase.GetInstanceId() == instanceId && towerDefenseCoinBase.GetParent() == _productionParent && towerDefenseCoinBase.IsInGroup("Coin") && !towerDefenseCoinBase.isCollect && !towerDefenseCoinBase.die && !towerDefenseCoinBase.disabledInput && !towerDefenseCoinBase.over && towerDefenseCoinBase.spriteNode.Modulate.A == 1f && pool.stack.Count == 0;
			_coinLifecyclePassed = firstRefresh && immediateRecycle && flag && flag2;
			Check(_coinLifecyclePassed, "Real Coin reset/recycle, parent or pool-count semantics drifted.");
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.COIN_SILVER, towerDefenseCoinBase);
			await FlushDeferredWork();
			await ResetPool(pool);
		}
	}

	private async Task ValidateSunLifecycle()
	{
		PoolConfig pool = GetPool(ObjectManagerConfig.OBJECT.SUN);
		await ResetPool(pool);
		TowerDefenseSunBase sun = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.SUN, _productionParent) as TowerDefenseSunBase;
		Check(GodotObject.IsInstanceValid(sun), "Real Sun pool pop failed.");
		if (GodotObject.IsInstanceValid(sun))
		{
			ulong instanceId = sun.GetInstanceId();
			StringName groupName = new StringName(sun.GetGroupName());
			bool firstRefresh = sun.GetParent() == _productionParent && sun.IsInGroup(groupName) && sun.IsInGroup("SunDropItem") && sun.sunNum == 25 && !sun.isCollect && !sun.die && !sun.over && sun.sprite.Modulate.A == 1f;
			sun.sunNum = 90L;
			sun.isCollect = true;
			sun.die = true;
			sun.over = true;
			sun.autoCollect = true;
			sun.sprite.Modulate = new Color(sun.sprite.Modulate, 0.2f);
			sun.moveComponent.SetVelocity(new Vector2(12f, -30f));
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SUN, sun);
			bool immediateRecycle = sun.GetParent() == _productionParent && !sun.IsInGroup(groupName) && !sun.IsInGroup("SunDropItem") && !sun.autoCollect && !sun.moveComponent.HasActiveMovement && !sun.IsPhysicsProcessing();
			await FlushDeferredWork();
			bool flag = sun.GetParent() == null && pool.stack.Count == 1;
			TowerDefenseSunBase towerDefenseSunBase = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.SUN, _productionParent) as TowerDefenseSunBase;
			bool flag2 = towerDefenseSunBase == sun && towerDefenseSunBase.GetInstanceId() == instanceId && towerDefenseSunBase.GetParent() == _productionParent && towerDefenseSunBase.IsInGroup(groupName) && towerDefenseSunBase.IsInGroup("SunDropItem") && towerDefenseSunBase.sunNum == 25 && !towerDefenseSunBase.isCollect && !towerDefenseSunBase.die && !towerDefenseSunBase.over && towerDefenseSunBase.sprite.Modulate.A == 1f && pool.stack.Count == 0;
			_sunLifecyclePassed = firstRefresh && immediateRecycle && flag && flag2;
			Check(_sunLifecyclePassed, "Real Sun reset/recycle, parent or pool-count semantics drifted.");
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.SUN, towerDefenseSunBase);
			await FlushDeferredWork();
			await ResetPool(pool);
		}
	}

	private async Task ValidateDamagePartLifecycle()
	{
		PoolConfig pool = GetPool(ObjectManagerConfig.OBJECT.damagePart);
		await ResetPool(pool);
		DamagePartDrop damage = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.damagePart, _productionParent) as DamagePartDrop;
		Check(GodotObject.IsInstanceValid(damage), "Real DamagePartDrop pool pop failed.");
		if (GodotObject.IsInstanceValid(damage))
		{
			ulong instanceId = damage.GetInstanceId();
			MoveComponent node = damage.GetNode<MoveComponent>("%MoveComponent");
			bool firstRefresh = damage.GetParent() == _productionParent && damage.IsInGroup("Effect") && !damage.over && damage.height == 100.0 && damage.jumpSpeed == 300.0 && damage.jumpTime == 3 && damage.Modulate.A == 1f;
			damage.height = 7.0;
			damage.jumpSpeed = 8.0;
			damage.jumpTime = 1;
			damage.over = false;
			damage.Modulate = new Color(damage.Modulate, 0.2f);
			node.SetVelocity(new Vector2(15f, -25f));
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.damagePart, damage);
			bool immediateRecycle = damage.GetParent() == _productionParent && !damage.IsInGroup("Effect") && damage.over && !node.HasActiveMovement && !damage.IsPhysicsProcessing();
			await FlushDeferredWork();
			bool flag = damage.GetParent() == null && pool.stack.Count == 1;
			DamagePartDrop damagePartDrop = ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.damagePart, _productionParent) as DamagePartDrop;
			bool flag2 = damagePartDrop == damage && damagePartDrop.GetInstanceId() == instanceId && damagePartDrop.GetParent() == _productionParent && damagePartDrop.IsInGroup("Effect") && !damagePartDrop.over && damagePartDrop.height == 100.0 && damagePartDrop.jumpSpeed == 300.0 && damagePartDrop.jumpTime == 3 && damagePartDrop.Modulate.A == 1f && pool.stack.Count == 0;
			_damageLifecyclePassed = firstRefresh && immediateRecycle && flag && flag2;
			Check(_damageLifecyclePassed, "Real DamagePartDrop reset/recycle, parent or pool-count semantics drifted.");
			ObjectManager.PoolPush(ObjectManagerConfig.OBJECT.damagePart, damagePartDrop);
			await FlushDeferredWork();
			await ResetPool(pool);
		}
	}

	private async Task ValidateCustomFallback()
	{
		PoolConfig pool = new PoolConfig
		{
			maxNum = 4,
			popCallable = "ModRefresh",
			pushCallable = "ModRecycle"
		};
		ObjectPoolCustomLifecycleProbeNode first = new ObjectPoolCustomLifecycleProbeNode();
		ObjectPoolCustomLifecycleProbeNode second = new ObjectPoolCustomLifecycleProbeNode();
		_fallbackParent.AddChild(first, forceReadableName: false, InternalMode.Disabled);
		_fallbackParent.AddChild(second, forceReadableName: false, InternalMode.Disabled);
		bool methodsVisible = first.HasMethod("ModRefresh") && first.HasMethod("ModRecycle");
		pool.Push(first);
		pool.Push(second);
		bool immediate = first.CustomRecycleCalls == 1 && second.CustomRecycleCalls == 1 && first.TypedRecycleCalls == 0 && second.TypedRecycleCalls == 0 && first.RecycleParent == _fallbackParent && second.RecycleParent == _fallbackParent && first.GetParent() == _fallbackParent && second.GetParent() == _fallbackParent;
		await FlushDeferredWork();
		bool flag = first.GetParent() == null && second.GetParent() == null && pool.stack.Count == 2;
		Node node = pool.Pop(_fallbackParent);
		Node node2 = pool.Pop(_fallbackParent);
		bool flag2 = first.CustomRefreshCalls == 1 && second.CustomRefreshCalls == 1 && first.TypedRefreshCalls == 0 && second.TypedRefreshCalls == 0 && first.RefreshParent == _fallbackParent && second.RefreshParent == _fallbackParent && node != node2 && pool.stack.Count == 0;
		_customFallbackPassed = methodsVisible && immediate && flag && flag2;
		_callbackOrderPassed = immediate && flag2;
		_deferredBatchPassed = flag;
		Check(_customFallbackPassed, "Custom/Mod lifecycle did not remain on dynamic HasMethod/Call fallback.");
		Check(_callbackOrderPassed, "Recycle must run before removal and Refresh after reparenting.");
		Check(_deferredBatchPassed, "One deferred batch must detach every queued Node.");
		pool.Push(node);
		pool.Push(node2);
		await FlushDeferredWork();
		pool.Clear();
		await FlushDeferredWork();
	}

	private async Task ValidateScriptSubclassFallback()
	{
		ObjectPoolCoinCSharpModLifecycleProbeNode csharpMod = new ObjectPoolCoinCSharpModLifecycleProbeNode();
		_fallbackParent.AddChild(csharpMod, forceReadableName: false, InternalMode.Disabled);
		IObjectPoolLifecycle objectPoolLifecycle = csharpMod;
		PoolConfig csharpPool = new PoolConfig
		{
			maxNum = 1,
			popCallable = "Refresh",
			pushCallable = "Recycle"
		};
		csharpMod.autoCollect = true;
		csharpMod.isCollect = true;
		csharpPool.Push(csharpMod);
		bool csharpRecycled = !objectPoolLifecycle.SupportsDirectPoolLifecycleDispatch && csharpMod.ModRecycleCalls == 1 && csharpMod.ModRecycleParent == _fallbackParent && csharpMod.autoCollect && csharpMod.GetParent() == _fallbackParent;
		await FlushDeferredWork();
		bool csharpDeferred = csharpMod.GetParent() == null && csharpPool.stack.Count == 1;
		Node node = csharpPool.Pop(_fallbackParent);
		bool csharpRefreshed = node == csharpMod && csharpMod.ModRefreshCalls == 1 && csharpMod.ModRefreshParent == _fallbackParent && csharpMod.isCollect && csharpMod.GetParent() == _fallbackParent && csharpPool.stack.Count == 0;
		GDScript gDScript = GD.Load<GDScript>("res://Test/ObjectPoolGDScriptLifecycleProbe.gd");
		Check(GodotObject.IsInstanceValid(gDScript), "The GDScript lifecycle fixture must load.");
		if (GodotObject.IsInstanceValid(gDScript))
		{
			Node gdscriptMod = gDScript.New().AsGodotObject() as Node;
			Check(GodotObject.IsInstanceValid(gdscriptMod), "The GDScript lifecycle fixture must instantiate.");
			if (GodotObject.IsInstanceValid(gdscriptMod))
			{
				_fallbackParent.AddChild(gdscriptMod, forceReadableName: false, InternalMode.Disabled);
				PoolConfig gdscriptPool = new PoolConfig
				{
					maxNum = 1,
					popCallable = "Refresh",
					pushCallable = "Recycle"
				};
				gdscriptPool.Push(gdscriptMod);
				bool gdscriptRecycled = !(gdscriptMod is IObjectPoolLifecycle) && gdscriptMod.Get("mod_recycle_calls").AsInt32() == 1 && gdscriptMod.Get("mod_recycle_parent").AsGodotObject() == _fallbackParent && gdscriptMod.GetParent() == _fallbackParent;
				await FlushDeferredWork();
				bool flag = gdscriptMod.GetParent() == null && gdscriptPool.stack.Count == 1;
				bool flag2 = gdscriptPool.Pop(_fallbackParent) == gdscriptMod && gdscriptMod.Get("mod_refresh_calls").AsInt32() == 1 && gdscriptMod.Get("mod_refresh_parent").AsGodotObject() == _fallbackParent && gdscriptMod.GetParent() == _fallbackParent && gdscriptPool.stack.Count == 0;
				_scriptSubclassFallbackPassed = csharpRecycled && csharpDeferred && csharpRefreshed && gdscriptRecycled && flag && flag2;
				Check(_scriptSubclassFallbackPassed, "GDScript Nodes and unknown C# Mod subclasses must keep standard Refresh/Recycle on dynamic HasMethod/Call.");
				csharpPool.Push(csharpMod);
				gdscriptPool.Push(gdscriptMod);
				await FlushDeferredWork();
				csharpPool.Clear();
				gdscriptPool.Clear();
				await FlushDeferredWork();
			}
		}
	}

	private PoolConfig GetPool(ObjectManagerConfig.OBJECT id)
	{
		ObjectManager instance = ObjectManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || id < ObjectManagerConfig.OBJECT.NOONE || (int)id >= instance.poolList.Count)
		{
			return null;
		}
		return instance.poolList[(int)id];
	}

	private async Task ResetPool(PoolConfig pool)
	{
		Check(GodotObject.IsInstanceValid(pool), "Required production PoolConfig is unavailable.");
		if (GodotObject.IsInstanceValid(pool))
		{
			pool.Clear();
			await FlushDeferredWork();
			Check(pool.stack.Count == 0, "Pool reset did not clear its stack.");
		}
	}

	private async Task FlushDeferredWork()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private void ResetRefreshCounter()
	{
		if (_baselineMode)
		{
			ObjectPoolDynamicLifecycleProbeNode.ResetRefreshCalls();
		}
		else
		{
			ObjectPoolTypedLifecycleProbeNode.ResetRefreshCalls();
		}
	}

	private void ResetRecycleCounter()
	{
		if (_baselineMode)
		{
			ObjectPoolDynamicLifecycleProbeNode.ResetRecycleCalls();
		}
		else
		{
			ObjectPoolTypedLifecycleProbeNode.ResetRecycleCalls();
		}
	}

	private long ReadRefreshCounter()
	{
		if (!_baselineMode)
		{
			return ObjectPoolTypedLifecycleProbeNode.RefreshCalls;
		}
		return ObjectPoolDynamicLifecycleProbeNode.RefreshCalls;
	}

	private long ReadRecycleCounter()
	{
		if (!_baselineMode)
		{
			return ObjectPoolTypedLifecycleProbeNode.RecycleCalls;
		}
		return ObjectPoolDynamicLifecycleProbeNode.RecycleCalls;
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError(message);
		}
	}
}
