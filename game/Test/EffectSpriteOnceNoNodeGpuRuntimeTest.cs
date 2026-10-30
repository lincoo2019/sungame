using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public partial class EffectSpriteOnceNoNodeGpuRuntimeTest : Node
{
	private readonly record struct PerformanceStats(double Mean, double P99, double Max);

	private const int InstanceCount = 1000;

	private const int WarmupSamples = 240;

	private const int MeasuredSamples = 1200;

	private const int ExpirySamples = 240;

	private const double HotPathBudgetMilliseconds = 0.2;

	private const long AllocatedBudgetBytes = 0L;

	private const double SteadyDelta = 1E-06;

	private const double ExpiryDelta = 10.0;

	private const string FireDataPath = "res://Asset/Anime/Effect/Fire/Base/Fire.tres";

	private static readonly string[] ProductionScenePaths = new string[7] { "res://Prefab/Particles/Splats/FireSplats/FireSplats.tscn", "res://Prefab/Particles/Splats/IceFireSplats/IceFireSplats.tscn", "res://Prefab/Particles/Splats/WhiteFireSplats/WhiteFireSplats.tscn", "res://Prefab/Particles/Splats/MegaFireSplats/MegaFireSplats.tscn", "res://Prefab/Particles/Splats/PeaBombSplats/PeaBombSplats.tscn", "res://Prefab/Particles/Splats/FirePeaBombSplats/FirePeaBombSplats.tscn", "res://Prefab/Particles/Splats/FireZombiePeaSplats/FireZombiePeaSplats.tscn" };

	private static readonly string[] ProductionClips = new string[7] { "Done", "Done", "Done", "Done", "Idle", "Idle", "Done" };

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private async void Run()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		AdobeAnimateRenderBackend previousBackend = Global.Instance.adobeAnimateRenderBackend;
		Node2D mount = new Node2D
		{
			Name = "EffectSpriteOnceNoNodeGpuMount"
		};
		TowerDefenseControlNew control = new TowerDefenseControlNew
		{
			characterNode = mount
		};
		int failures = 0;
		try
		{
			_ = 1;
			try
			{
				AddChild(mount, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
				string method = RenderingServer.GetCurrentRenderingMethod().ToString();
				string driver = RenderingServer.GetCurrentRenderingDriverName().ToString();
				bool rendererPassed = string.Equals(method, "mobile", StringComparison.OrdinalIgnoreCase) && driver.IndexOf("vulkan", StringComparison.OrdinalIgnoreCase) >= 0;
				Require(rendererPassed, "Vulkan Mobile renderer", ref failures);
				PackedScene[] scenes = new PackedScene[ProductionScenePaths.Length];
				for (int i = 0; i < scenes.Length; i++)
				{
					scenes[i] = ResourceLoader.Load<PackedScene>(ProductionScenePaths[i], null, ResourceLoader.CacheMode.Reuse);
					Require(GodotObject.IsInstanceValid(scenes[i]), $"production scene {i}", ref failures);
				}
				TowerDefenseEffectSpriteOnceBatcher batcher = TowerDefenseEffectSpriteOnceBatcher.GetOrCreate();
				Require(GodotObject.IsInstanceValid(batcher), "shared batcher", ref failures);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				TowerDefenseEffectSpriteOnceGpuHandle[] array = new TowerDefenseEffectSpriteOnceGpuHandle[1000];
				double[] array2 = new double[1200];
				double[] array3 = new double[1200];
				double[] samples = new double[1200];
				double[] samples2 = new double[240];
				Require(WarmupRegistrationAndRemoval(scenes, array) && batcher.ActiveNodeFreeCount == 0, "allocation-free warmup", ref failures);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
				int nodeCount = GetTree().GetNodeCount();
				long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
				bool flag = BenchmarkRegistrationAndRemoval(scenes, array, array2, array3);
				long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
				PerformanceStats registrationStats = CalculateStats(array2);
				PerformanceStats removalStats = CalculateStats(array3);
				bool flag2 = RegisterBatch(scenes, array);
				int treeNodeDelta = GetTree().GetNodeCount() - nodeCount;
				int validHandleCount = 0;
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j].IsValid)
					{
						validHandleCount++;
					}
				}
				batcher._Process(0.0);
				int dispatchedInstanceCount = batcher.LastDrawSubmissionCount;
				BenchmarkSteadyPlayback(batcher, samples);
				PerformanceStats steadyStats = CalculateStats(samples);
				RemoveBatch(array);
				batcher._Process(0.0);
				bool flag3 = BenchmarkExpiry(batcher, scenes, array, samples2);
				PerformanceStats expiryStats = CalculateStats(samples2);
				bool performancePassed = registrationStats.P99 < 0.2 && removalStats.P99 < 0.2 && steadyStats.P99 < 0.2 && expiryStats.P99 < 0.2 && allocatedBytes <= 0;
				bool bulkPassed = flag && flag2 && flag3 && validHandleCount == 1000 && dispatchedInstanceCount == 1000 && treeNodeDelta == 0;
				Require(bulkPassed, "1000 no-node registrations", ref failures);
				Require(performancePassed, "hot-path budgets", ref failures);
				List<string> list = new List<string>(2);
				TowerDefenseEffectSpriteOnceGpuHandle handle;
				bool flag4 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out handle, new Vector2I(2, 3), "Flame|Done", Transform2D.Identity, list.Add);
				batcher._Process(1.0);
				batcher._Process(0.001);
				bool clipCallbacks = flag4 && list.Count == 2 && list[0] == "Flame" && list[1] == "Done" && !handle.IsValid;
				Require(clipCallbacks, "Flame|Done completion callbacks", ref failures);
				TowerDefenseEffectSpriteOnceGpuHandle handle2;
				bool flag5 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out handle2, default(Vector2I), "Done");
				bool flag6 = handle2.Remove();
				TowerDefenseEffectSpriteOnceGpuHandle handle3;
				bool flag7 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out handle3, default(Vector2I), "Done");
				bool generationSafe = flag5 && flag6 && flag7 && !handle2.IsValid && handle3.IsValid;
				handle3.Remove();
				Require(generationSafe, "generation-safe handle", ref failures);
				List<string> reentrantCallbacks = new List<string>(2);
				TowerDefenseEffectSpriteOnceGpuHandle siblingHandle = default(TowerDefenseEffectSpriteOnceGpuHandle);
				TowerDefenseEffectSpriteOnceGpuHandle reentrantHandle = default(TowerDefenseEffectSpriteOnceGpuHandle);
				bool reentrantSpawned = false;
				TowerDefenseEffectSpriteOnceGpuHandle handle4;
				bool num = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out handle4, default(Vector2I), "Done", null, delegate
				{
					reentrantCallbacks.Add("first");
					siblingHandle.Remove();
					reentrantSpawned = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out reentrantHandle, default(Vector2I), "Done", null, delegate
					{
						reentrantCallbacks.Add("replacement");
					});
				});
				bool flag8 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out siblingHandle, default(Vector2I), "Done", null, delegate
				{
					reentrantCallbacks.Add("sibling");
				});
				batcher._Process(1.0);
				bool flag9 = num && flag8 && reentrantSpawned && reentrantCallbacks.Count == 1 && reentrantCallbacks[0] == "first" && !handle4.IsValid && !siblingHandle.IsValid && reentrantHandle.IsValid;
				batcher._Process(1.0);
				bool reentrantSafe = flag9 && reentrantCallbacks.Count == 2 && reentrantCallbacks[1] == "replacement" && !reentrantHandle.IsValid;
				Require(reentrantSafe, "same-deadline callback reentry", ref failures);
				TowerDefenseEffectSpriteOnceGpuHandle handle5;
				bool flag10 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out handle5, default(Vector2I), "Done");
				scenes[0].EmitChanged();
				batcher._Process(0.0);
				bool sceneFailClosed = flag10 && !handle5.IsValid;
				Require(sceneFailClosed, "scene/script revision fail-closed", ref failures);
				PackedScene packedScene = new PackedScene();
				Node2D node2D = new Node2D();
				Error error = packedScene.Pack(node2D);
				node2D.Free();
				TowerDefenseEffectSpriteOnceGpuHandle handle6;
				bool flag11 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(packedScene, out handle6, default(Vector2I), "Done");
				bool scriptFailClosed = error == Error.Ok && !flag11;
				Require(scriptFailClosed, "script identity fail-closed", ref failures);
				AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Effect/Fire/Base/Fire.tres", null, ResourceLoader.CacheMode.Reuse);
				TowerDefenseEffectSpriteOnceGpuHandle handle7;
				bool flag12 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out handle7, default(Vector2I), "Done");
				double frameRate = adobeAnimateData.frameRate++;
				adobeAnimateData.EmitChanged();
				batcher._Process(0.0);
				bool dataFailClosed = flag12 && !handle7.IsValid;
				adobeAnimateData.frameRate = frameRate;
				adobeAnimateData.EmitChanged();
				batcher._Process(0.0);
				Require(dataFailClosed, "data revision fail-closed", ref failures);
				TowerDefenseEffectSpriteOnceGpuHandle handle8;
				bool flag13 = TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[0], out handle8, default(Vector2I), "Done");
				Node2D node2D2 = new Node2D
				{
					Name = "ReplacementOwnerMount"
				};
				AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
				TowerDefenseControlNew towerDefenseControlNew = (manager.currentControl = new TowerDefenseControlNew
				{
					characterNode = node2D2
				});
				batcher._Process(0.0);
				bool ownerFailClosed = flag13 && !handle8.IsValid;
				manager.currentControl = control;
				node2D2.QueueFree();
				towerDefenseControlNew.Free();
				Require(ownerFailClosed, "owner replacement fail-closed", ref failures);
				AdobeAnimateSprite fallbackSprite = scenes[0].Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
				TowerDefenseEffectSpriteOnce fallbackEffect = manager.CreateEffectSpriteSceneOnce(fallbackSprite, new Vector2I(1, 1), "Done");
				int fallbackNodesBefore = GetTree().GetNodeCount();
				mount.AddChild(fallbackEffect, forceReadableName: false, InternalMode.Disabled);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				int num2 = GetTree().GetNodeCount() - fallbackNodesBefore;
				bool flag14 = GodotObject.IsInstanceValid(fallbackEffect) && GodotObject.IsInstanceValid(fallbackSprite) && fallbackEffect.GetParent() == mount && fallbackSprite.GetParent() == fallbackEffect && num2 >= 2;
				Require(flag14, "real Node fallback", ref failures);
				fallbackEffect.QueueFree();
				bool value = rendererPassed && bulkPassed && clipCallbacks && generationSafe && reentrantSafe && sceneFailClosed && scriptFailClosed && dataFailClosed && ownerFailClosed && flag14;
				GD.Print("EFFECT_SPRITE_ONCE_NO_NODE_GPU " + $"renderer={driver}_{method} instances={1000} " + $"warmupSamples={240} " + $"measuredSamples={1200} " + $"registrationMeanMs={registrationStats.Mean:F6} " + $"registrationP99Ms={registrationStats.P99:F6} " + $"registrationMaxMs={registrationStats.Max:F6} " + $"removalMeanMs={removalStats.Mean:F6} " + $"removalP99Ms={removalStats.P99:F6} " + $"removalMaxMs={removalStats.Max:F6} " + $"steadyMeanMs={steadyStats.Mean:F6} " + $"steadyP99Ms={steadyStats.P99:F6} " + $"steadyMaxMs={steadyStats.Max:F6} " + $"expiryMeanMs={expiryStats.Mean:F6} " + $"expiryP99Ms={expiryStats.P99:F6} " + $"expiryMaxMs={expiryStats.Max:F6} " + $"budgetMs={0.2:F1} " + $"allocatedBytes={allocatedBytes} " + $"allocatedBudgetBytes={0L} " + $"treeNodeDelta={treeNodeDelta} " + $"active={validHandleCount} " + $"dispatched={dispatchedInstanceCount} " + $"clipCallbacks={clipCallbacks} " + $"generationSafe={generationSafe} " + $"reentrantSafe={reentrantSafe} " + $"sceneFailClosed={sceneFailClosed} " + $"scriptFailClosed={scriptFailClosed} " + $"dataFailClosed={dataFailClosed} " + $"ownerFailClosed={ownerFailClosed} " + $"nodeFallback={flag14} " + $"performancePassed={performancePassed} " + $"functionalPassed={value}");
			}
			catch (Exception ex)
			{
				failures++;
				GD.PushError(ex.ToString());
			}
		}
		finally
		{
			Global.Instance.adobeAnimateRenderBackend = previousBackend;
			manager.currentControl = previousControl;
			if (GodotObject.IsInstanceValid(mount))
			{
				mount.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.Free();
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetTree().Quit((failures != 0) ? 2 : 0);
		}
	}

	private static bool RegisterBatch(PackedScene[] scenes, TowerDefenseEffectSpriteOnceGpuHandle[] handles)
	{
		bool flag = true;
		for (int i = 0; i < handles.Length; i++)
		{
			int num = i % scenes.Length;
			flag &= TowerDefenseManager.TryCreateEffectSpriteOnceGpu(scenes[num], out handles[i], new Vector2I(i / 7 % 11, 1 + i % 7), ProductionClips[num]);
		}
		return flag;
	}

	private static bool WarmupRegistrationAndRemoval(PackedScene[] scenes, TowerDefenseEffectSpriteOnceGpuHandle[] handles)
	{
		bool flag = true;
		for (int i = 0; i < 240; i++)
		{
			flag &= RegisterBatch(scenes, handles);
			flag &= RemoveBatch(handles);
		}
		return flag;
	}

	private static bool BenchmarkRegistrationAndRemoval(PackedScene[] scenes, TowerDefenseEffectSpriteOnceGpuHandle[] handles, double[] registrationSamples, double[] removalSamples)
	{
		bool flag = true;
		for (int i = 0; i < 1200; i++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			flag &= RegisterBatch(scenes, handles);
			registrationSamples[i] = ElapsedMilliseconds(timestamp);
			timestamp = Stopwatch.GetTimestamp();
			flag &= RemoveBatch(handles);
			removalSamples[i] = ElapsedMilliseconds(timestamp);
		}
		return flag;
	}

	private static void BenchmarkSteadyPlayback(TowerDefenseEffectSpriteOnceBatcher batcher, double[] samples)
	{
		for (int i = 0; i < 240; i++)
		{
			batcher._Process(1E-06);
		}
		for (int j = 0; j < samples.Length; j++)
		{
			long timestamp = Stopwatch.GetTimestamp();
			batcher._Process(1E-06);
			samples[j] = ElapsedMilliseconds(timestamp);
		}
	}

	private static bool BenchmarkExpiry(TowerDefenseEffectSpriteOnceBatcher batcher, PackedScene[] scenes, TowerDefenseEffectSpriteOnceGpuHandle[] handles, double[] samples)
	{
		bool flag = true;
		for (int i = 0; i < samples.Length; i++)
		{
			flag &= RegisterBatch(scenes, handles);
			long timestamp = Stopwatch.GetTimestamp();
			batcher._Process(10.0);
			samples[i] = ElapsedMilliseconds(timestamp);
			flag &= batcher.ActiveNodeFreeCount == 0;
		}
		return flag;
	}

	private static bool RemoveBatch(TowerDefenseEffectSpriteOnceGpuHandle[] handles)
	{
		bool flag = true;
		for (int i = 0; i < handles.Length; i++)
		{
			flag &= handles[i].Remove();
		}
		return flag;
	}

	private static double ElapsedMilliseconds(long started)
	{
		return (double)(Stopwatch.GetTimestamp() - started) * 1000.0 / (double)Stopwatch.Frequency;
	}

	private static PerformanceStats CalculateStats(double[] samples)
	{
		Array.Sort(samples);
		double num = 0.0;
		for (int i = 0; i < samples.Length; i++)
		{
			num += samples[i];
		}
		return new PerformanceStats(num / (double)samples.Length, Percentile(samples, 0.99), samples[^1]);
	}

	private static double Percentile(double[] sortedSamples, double fraction)
	{
		int num = Math.Clamp((int)Math.Ceiling((double)sortedSamples.Length * fraction) - 1, 0, sortedSamples.Length - 1);
		return sortedSamples[num];
	}

	private static void Require(bool condition, string name, ref int failures)
	{
		if (!condition)
		{
			failures++;
			GD.PushError("EffectSpriteOnce no-node GPU check failed: " + name);
		}
	}
}
