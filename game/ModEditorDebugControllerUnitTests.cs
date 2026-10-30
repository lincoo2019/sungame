using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PVZHE.ModEditor.Debugging;

internal static class ModEditorDebugControllerUnitTests
{
	public static async Task<int> Main()
	{
		_ = 4;
		try
		{
			await CSharpBreakpointAndSnapshotAsync();
			await PauseAndStepCommandsAsync();
			await BlueprintBreakpointAndForcedPauseAsync();
			await ConcurrentCheckpointsSharePauseGateAsync();
			await StopAndCancellationAsync();
			Console.WriteLine("MOD_EDITOR_DEBUG_CONTROLLER_UNIT_RESULT:PASS");
			return 0;
		}
		catch (Exception value)
		{
			Console.Error.WriteLine("MOD_EDITOR_DEBUG_CONTROLLER_UNIT_RESULT:FAIL");
			Console.Error.WriteLine(value);
			return 1;
		}
	}

	private static async Task CSharpBreakpointAndSnapshotAsync()
	{
		XWModDebugController controller = new XWModDebugController();
		controller.AddBreakpoint(XWModDebugBreakpoint.ForCSharp("Scripts\\Demo.cs", 12));
		Dictionary<string, XWModDebugVariable> variables = new Dictionary<string, XWModDebugVariable> { ["sun"] = new XWModDebugVariable("sun", "int", "50") };
		XWModDebugStackFrame[] callStack = new XWModDebugStackFrame[1]
		{
			new XWModDebugStackFrame("main", "主入口", "Scripts/Demo.cs", 12, "", variables)
		};
		int callerThread = Environment.CurrentManagedThreadId;
		TaskCompletionSource<(XWModDebugSnapshot Snapshot, int ThreadId)> eventArrived = new TaskCompletionSource<(XWModDebugSnapshot, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
		controller.SnapshotChanged += delegate(object? _, XWModDebugSnapshot snapshot)
		{
			if (snapshot.State == XWModDebugState.Paused)
			{
				eventArrived.TrySetResult((snapshot, Environment.CurrentManagedThreadId));
			}
		};
		Task<XWModDebugCheckpointResult> wait = controller.CheckpointAsync(XWModDebugCheckpoint.ForCSharp("Scripts/Demo.cs", 12, "更新阳光", -1, callStack, variables));
		Assert(!wait.IsCompleted, "命中 C# 断点后不应继续执行。");
		(XWModDebugSnapshot, int) obj = await eventArrived.Task.WaitAsync(TimeSpan.FromSeconds(2L));
		var (xWModDebugSnapshot, _) = obj;
		Assert(obj.Item2 != callerThread, "SnapshotChanged 不应在调用检查点的主线程同步执行。");
		Assert(xWModDebugSnapshot.PauseReason == XWModDebugPauseReason.Breakpoint, "C# 断点暂停原因错误。");
		Assert(xWModDebugSnapshot.CallStack.Count == 1, "调用栈没有进入快照。");
		Assert(xWModDebugSnapshot.Variables["sun"].DisplayValue == "50", "变量快照内容错误。");
		variables["sun"] = new XWModDebugVariable("sun", "int", "999");
		Assert(xWModDebugSnapshot.Variables["sun"].DisplayValue == "50", "变量快照没有与运行时集合隔离。");
		Assert(controller.Continue(), "暂停后无法继续。");
		Assert(await wait == XWModDebugCheckpointResult.Continue, "继续命令没有释放检查点。");
	}

	private static async Task PauseAndStepCommandsAsync()
	{
		XWModDebugController controller = new XWModDebugController();
		Assert(controller.RequestPause(), "运行状态无法请求暂停。");
		Task<XWModDebugCheckpointResult> task = controller.CheckpointAsync(CSharpCheckpoint(20, 2));
		Assert(!task.IsCompleted, "暂停请求没有在下一检查点生效。");
		Assert(controller.StepOver(), "无法执行单步跳过。");
		Assert(await task == XWModDebugCheckpointResult.Continue, "单步跳过没有释放当前检查点。");
		Assert(await controller.CheckpointAsync(CSharpCheckpoint(21, 3)) == XWModDebugCheckpointResult.Continue, "单步跳过错误地停在更深调用层。");
		Task<XWModDebugCheckpointResult> task2 = controller.CheckpointAsync(CSharpCheckpoint(22, 2));
		Assert(!task2.IsCompleted, "单步跳过没有停在原调用层。");
		Assert(controller.StepOut(), "无法执行单步跳出。");
		await task2;
		Assert(await controller.CheckpointAsync(CSharpCheckpoint(23, 2)) == XWModDebugCheckpointResult.Continue, "单步跳出错误地停在当前调用层。");
		Task<XWModDebugCheckpointResult> task3 = controller.CheckpointAsync(CSharpCheckpoint(24, 1));
		Assert(!task3.IsCompleted, "单步跳出没有停在上层调用帧。");
		Assert(controller.StepInto(), "无法执行单步进入。");
		await task3;
		Task<XWModDebugCheckpointResult> task4 = controller.CheckpointAsync(CSharpCheckpoint(25, 4));
		Assert(!task4.IsCompleted, "单步进入没有停在下一检查点。");
		Assert(controller.Continue(), "单步进入暂停后无法继续。");
		await task4;
	}

	private static async Task BlueprintBreakpointAndForcedPauseAsync()
	{
		XWModDebugController controller = new XWModDebugController();
		controller.AddBreakpoint(XWModDebugBreakpoint.ForBlueprint("Blueprints/Test.tres", "42"));
		Task<XWModDebugCheckpointResult> task = controller.CheckpointAsync(XWModDebugCheckpoint.ForBlueprint("Blueprints/Test.tres", "42", "生成僵尸", "__XWBPGraphNode_SpawnZombie"));
		Assert(!task.IsCompleted, "蓝图节点断点没有暂停。");
		Assert(controller.Snapshot.CurrentCheckpoint.BlueprintNodeTypeId == "__XWBPGraphNode_SpawnZombie", "蓝图节点类型没有进入快照。");
		Assert(controller.Continue(), "蓝图断点无法继续。");
		await task;
		Task<XWModDebugCheckpointResult> task2 = controller.CheckpointAsync(XWModDebugCheckpoint.ForBlueprint("Blueprints/Test.tres", "99", "断点", "__XWBPGraphNode_Breakpoint", null, null, forcePause: true));
		Assert(!task2.IsCompleted, "蓝图断点节点没有强制暂停。");
		Assert(controller.Snapshot.PauseReason == XWModDebugPauseReason.ForcedBreakpoint, "强制蓝图断点的暂停原因错误。");
		controller.Continue();
		await task2;
	}

	private static async Task StopAndCancellationAsync()
	{
		XWModDebugController controller = new XWModDebugController();
		controller.AddBreakpoint(XWModDebugBreakpoint.ForCSharp("Scripts/Stop.cs", 1));
		Task<XWModDebugCheckpointResult> task = controller.CheckpointAsync(XWModDebugCheckpoint.ForCSharp("Scripts/Stop.cs", 1));
		Assert(controller.Stop(), "无法停止暂停中的调试会话。");
		Assert(await task == XWModDebugCheckpointResult.Stopped, "停止没有释放等待检查点。");
		Assert(await controller.CheckpointAsync(XWModDebugCheckpoint.ForCSharp("Scripts/Stop.cs", 1)) == XWModDebugCheckpointResult.Stopped, "停止后的检查点没有立即返回停止。");
		Assert(controller.Start(), "停止后无法开始新会话。");
		using CancellationTokenSource cancellation = new CancellationTokenSource();
		Task<XWModDebugCheckpointResult> task2 = controller.CheckpointAsync(XWModDebugCheckpoint.ForCSharp("Scripts/Stop.cs", 1), cancellation.Token);
		cancellation.Cancel();
		await AssertCanceledAsync(task2);
		Assert(controller.State == XWModDebugState.Paused, "单个执行线程取消等待不应擅自改变整个调试会话的暂停状态。");
		Assert(controller.Stop(), "取消执行线程后无法停止调试会话。");
	}

	private static async Task ConcurrentCheckpointsSharePauseGateAsync()
	{
		XWModDebugController xWModDebugController = new XWModDebugController();
		xWModDebugController.AddBreakpoint(XWModDebugBreakpoint.ForCSharp("Scripts/Workers.cs", 7));
		Task<XWModDebugCheckpointResult> task = xWModDebugController.CheckpointAsync(XWModDebugCheckpoint.ForCSharp("Scripts/Workers.cs", 7, "工作线程一"));
		Task<XWModDebugCheckpointResult> second = xWModDebugController.CheckpointAsync(XWModDebugCheckpoint.ForCSharp("Scripts/Workers.cs", 100, "工作线程二"));
		Assert(!task.IsCompleted && !second.IsCompleted, "并发执行线程没有共享会话级暂停闸门。");
		Assert(xWModDebugController.Snapshot.CurrentCheckpoint.Line == 7, "后进入的并发检查点覆盖了首个断点位置。");
		Assert(xWModDebugController.Continue(), "并发暂停后无法继续。");
		Assert(await task == XWModDebugCheckpointResult.Continue, "首个执行线程没有恢复。");
		Assert(await second == XWModDebugCheckpointResult.Continue, "并发执行线程没有一同恢复。");
	}

	private static XWModDebugCheckpoint CSharpCheckpoint(int line, int frameDepth)
	{
		return XWModDebugCheckpoint.ForCSharp("Scripts/Step.cs", line, $"第 {line} 行", frameDepth);
	}

	private static async Task AssertCanceledAsync(Task task)
	{
		try
		{
			await task;
			throw new InvalidOperationException("取消的检查点没有抛出 OperationCanceledException。");
		}
		catch (OperationCanceledException)
		{
		}
	}

	private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (!condition())
		{
			if (stopwatch.Elapsed > timeout)
			{
				throw new TimeoutException("等待调试器状态超时。");
			}
			await Task.Delay(10);
		}
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
