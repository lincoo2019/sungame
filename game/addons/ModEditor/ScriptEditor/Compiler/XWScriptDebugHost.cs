using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PVZHE.ModEditor.ScriptEditor;

public partial class XWScriptDebugHost : Node
{
	private const string HostNodeName = "ModEditorScriptDebugHost";

	private readonly object _dispatchLock = new object();

	private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();

	private readonly List<Task> _ownedCleanupTasks = new List<Task>();

	private int _mainThreadId;

	private SynchronizationContext _mainThreadContext;

	private int _wakeRequestPending;

	public int MainThreadId => _mainThreadId;

	public int OwnedCleanupCount => _ownedCleanupTasks.Count;

	public int PreviewRootCount
	{
		get
		{
			int num = 0;
			foreach (Node child in GetChildren())
			{
				if (child.Name.ToString().StartsWith("ScriptDebugPreview_", StringComparison.Ordinal))
				{
					num++;
				}
			}
			return num;
		}
	}

	public override void _Ready()
	{
		_mainThreadId = System.Environment.CurrentManagedThreadId;
		_mainThreadContext = SynchronizationContext.Current;
		base.ProcessMode = ProcessModeEnum.Always;
		SetProcess(enable: false);
	}

	public override void _Process(double delta)
	{
		DrainMainThreadQueue();
		for (int num = _ownedCleanupTasks.Count - 1; num >= 0; num--)
		{
			Task task = _ownedCleanupTasks[num];
			if (task.IsCompleted)
			{
				if (task.IsFaulted)
				{
					GD.PushError("Script debug cleanup failed: " + task.Exception?.GetBaseException().Message);
				}
				_ownedCleanupTasks.RemoveAt(num);
			}
		}
		if (!HasPendingWork())
		{
			SetProcess(enable: false);
		}
	}

	public static XWScriptDebugHost GetOrCreate(Node requester)
	{
		SceneTree sceneTree = requester?.GetTree();
		if (sceneTree?.Root == null)
		{
			return null;
		}
		XWScriptDebugHost nodeOrNull = sceneTree.Root.GetNodeOrNull<XWScriptDebugHost>("ModEditorScriptDebugHost");
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			return nodeOrNull;
		}
		XWScriptDebugHost xWScriptDebugHost = new XWScriptDebugHost
		{
			Name = "ModEditorScriptDebugHost"
		};
		sceneTree.Root.AddChild(xWScriptDebugHost, forceReadableName: false, InternalMode.Disabled);
		return xWScriptDebugHost;
	}

	public Node CreatePreviewRoot()
	{
		EnsureMainThread();
		Node node = new Node
		{
			Name = $"ScriptDebugPreview_{Guid.NewGuid():N}",
			ProcessMode = ProcessModeEnum.Always
		};
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
		return node;
	}

	public Task DispatchAsync(Action action)
	{
		if (action == null)
		{
			return Task.CompletedTask;
		}
		if (System.Environment.CurrentManagedThreadId == _mainThreadId && IsInsideTree())
		{
			action();
			return Task.CompletedTask;
		}
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (_dispatchLock)
		{
			_mainThreadQueue.Enqueue(delegate
			{
				try
				{
					action();
					completion.TrySetResult(result: true);
				}
				catch (Exception exception)
				{
					completion.TrySetException(exception);
				}
			});
		}
		RequestProcessing();
		return completion.Task;
	}

	public Task<T> DispatchAsync<T>(Func<T> action)
	{
		if (action == null)
		{
			return Task.FromResult(default(T));
		}
		if (System.Environment.CurrentManagedThreadId == _mainThreadId && IsInsideTree())
		{
			return Task.FromResult(action());
		}
		TaskCompletionSource<T> completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (_dispatchLock)
		{
			_mainThreadQueue.Enqueue(delegate
			{
				try
				{
					completion.TrySetResult(action());
				}
				catch (Exception exception)
				{
					completion.TrySetException(exception);
				}
			});
		}
		RequestProcessing();
		return completion.Task;
	}

	public Task NextProcessFrameAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		CancellationTokenRegistration registration = default(CancellationTokenRegistration);
		DispatchAsync(delegate
		{
			if (cancellationToken.IsCancellationRequested)
			{
				completion.TrySetCanceled(cancellationToken);
			}
			else
			{
				SceneTree tree = GetTree();
				Action handler = null;
				handler = delegate
				{
					tree.ProcessFrame -= handler;
					registration.Dispose();
					completion.TrySetResult(result: true);
				};
				tree.ProcessFrame += handler;
				if (cancellationToken.CanBeCanceled)
				{
					registration = cancellationToken.Register(delegate
					{
						DispatchAsync(delegate
						{
							if (GodotObject.IsInstanceValid(tree))
							{
								tree.ProcessFrame -= handler;
							}
							completion.TrySetCanceled(cancellationToken);
						});
					});
				}
			}
		}).ContinueWith(delegate(Task task)
		{
			if (task.IsFaulted)
			{
				completion.TrySetException(task.Exception?.GetBaseException() ?? new InvalidOperationException("Main-thread dispatch failed."));
			}
			else if (task.IsCanceled)
			{
				completion.TrySetCanceled();
			}
		}, TaskScheduler.Default);
		return completion.Task;
	}

	public void OwnSessionShutdown(XWScriptDebugSession session, Node previewRoot)
	{
		EnsureMainThread();
		if (session == null)
		{
			FreePreviewRoot(previewRoot);
			return;
		}
		_ownedCleanupTasks.Add(StopUntilReleasedAsync(session, previewRoot));
		RequestProcessing();
	}

	internal Task ClearPreviewRootAsync(Node previewRoot)
	{
		return DispatchAsync(delegate
		{
			if (!GodotObject.IsInstanceValid(previewRoot))
			{
				return;
			}
			foreach (Node child in previewRoot.GetChildren())
			{
				previewRoot.RemoveChild(child);
				child.Free();
			}
		});
	}

	private async Task StopUntilReleasedAsync(XWScriptDebugSession session, Node previewRoot)
	{
		while (session.HasLiveSession)
		{
			try
			{
				if (await session.StopAsync(500) || !session.HasLiveSession)
				{
					break;
				}
				goto IL_00e1;
			}
			catch (Exception ex)
			{
				GD.PushError("Script debug shutdown retry: " + ex.GetBaseException().Message);
				goto IL_00e1;
			}
			IL_00e1:
			if (session.HasLiveSession)
			{
				await Task.Delay(50);
			}
		}
		await ClearPreviewRootAsync(previewRoot);
		await DispatchAsync(delegate
		{
			FreePreviewRoot(previewRoot);
		});
	}

	private void DrainMainThreadQueue()
	{
		while (true)
		{
			Action action;
			lock (_dispatchLock)
			{
				if (_mainThreadQueue.Count == 0)
				{
					break;
				}
				action = _mainThreadQueue.Dequeue();
			}
			action();
		}
	}

	private bool HasPendingWork()
	{
		lock (_dispatchLock)
		{
			return _mainThreadQueue.Count > 0 || _ownedCleanupTasks.Count > 0;
		}
	}

	private void RequestProcessing()
	{
		if (_mainThreadId != 0 && System.Environment.CurrentManagedThreadId == _mainThreadId && IsInsideTree())
		{
			SetProcess(enable: true);
			return;
		}
		SynchronizationContext mainThreadContext = _mainThreadContext;
		if (mainThreadContext == null || Interlocked.Exchange(ref _wakeRequestPending, 1) != 0)
		{
			return;
		}
		mainThreadContext.Post(delegate
		{
			Interlocked.Exchange(ref _wakeRequestPending, 0);
			if (GodotObject.IsInstanceValid(this) && IsInsideTree())
			{
				SetProcess(enable: true);
			}
		}, null);
	}

	private static void FreePreviewRoot(Node previewRoot)
	{
		if (GodotObject.IsInstanceValid(previewRoot))
		{
			previewRoot.GetParent()?.RemoveChild(previewRoot);
			previewRoot.Free();
		}
	}

	private void EnsureMainThread()
	{
		if (_mainThreadId != 0 && System.Environment.CurrentManagedThreadId != _mainThreadId)
		{
			throw new InvalidOperationException("Script debug host tree mutations must run on the Godot main thread.");
		}
	}
}
