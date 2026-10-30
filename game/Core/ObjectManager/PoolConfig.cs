using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class PoolConfig : RefCounted
{
	internal static readonly StringName RefreshCallbackName = new StringName("Refresh");

	internal static readonly StringName RecycleCallbackName = new StringName("Recycle");

	[Export(PropertyHint.None, "")]
	public PackedScene scene;

	[Export(PropertyHint.None, "")]
	public string scenePath = "";

	[Export(PropertyHint.None, "")]
	public int maxNum = 100;

	[Export(PropertyHint.None, "")]
	public int idleRetainNum = 16;

	[Export(PropertyHint.None, "")]
	public ulong idleTrimFrames = 600uL;

	[Export(PropertyHint.None, "")]
	public string popCallable = "";

	[Export(PropertyHint.None, "")]
	public string pushCallable = "";

	public List<Node> stack = new List<Node>();

	private StringName _popCallableSN;

	private StringName _pushCallableSN;

	private readonly List<Node> _pendingPush = new List<Node>();

	private bool _deferredPushScheduled;

	private ulong _lastTouchedFrame;

	private StringName GetPopCallableSN()
	{
		if (_popCallableSN == null && popCallable != "")
		{
			_popCallableSN = new StringName(popCallable);
		}
		return _popCallableSN;
	}

	private StringName GetPushCallableSN()
	{
		if (_pushCallableSN == null && pushCallable != "")
		{
			_pushCallableSN = new StringName(pushCallable);
		}
		return _pushCallableSN;
	}

	public void Push(Node node)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(node.GetParent()))
		{
			node.QueueFree();
			return;
		}
		StringName pushCallableSN = GetPushCallableSN();
		DispatchPushLifecycle(node, pushCallableSN);
		_lastTouchedFrame = Engine.GetPhysicsFrames();
		_pendingPush.Add(node);
		if (!_deferredPushScheduled)
		{
			_deferredPushScheduled = true;
			CallDeferred("DeferredPushBatch");
		}
	}

	private void DeferredPushBatch()
	{
		for (int i = 0; i < _pendingPush.Count; i++)
		{
			Node node = _pendingPush[i];
			if (!GodotObject.IsInstanceValid(node))
			{
				continue;
			}
			Node parent = node.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(node);
				if (maxNum == -1 || stack.Count < maxNum)
				{
					stack.Add(node);
				}
				else
				{
					node.QueueFree();
				}
			}
		}
		_pendingPush.Clear();
		_deferredPushScheduled = false;
	}

	public Node Pop(Node parent)
	{
		_lastTouchedFrame = Engine.GetPhysicsFrames();
		Node node;
		if (stack.Count > 0)
		{
			int index = stack.Count - 1;
			node = stack[index];
			stack.RemoveAt(index);
		}
		else
		{
			PackedScene packedScene = GetScene();
			if (packedScene == null)
			{
				GD.PushError("Object pool scene is missing: " + scenePath);
				return null;
			}
			node = packedScene.Instantiate<Node>(PackedScene.GenEditState.Disabled);
		}
		Node parent2 = node.GetParent();
		if (parent2 != parent)
		{
			if (parent2 != null)
			{
				node.Reparent(parent);
			}
			else
			{
				parent.AddChild(node, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		StringName popCallableSN = GetPopCallableSN();
		DispatchPopLifecycle(node, popCallableSN);
		return node;
	}

	internal static void DispatchPopLifecycle(Node node, StringName callback)
	{
		if (!(callback == null) && GodotObject.IsInstanceValid(node))
		{
			if (callback == RefreshCallbackName && node is IObjectPoolLifecycle { SupportsDirectPoolLifecycleDispatch: not false } objectPoolLifecycle)
			{
				objectPoolLifecycle.RefreshFromPool();
			}
			else if (node.HasMethod(callback))
			{
				node.Call(callback);
			}
		}
	}

	internal static void DispatchPushLifecycle(Node node, StringName callback)
	{
		if (!(callback == null) && GodotObject.IsInstanceValid(node))
		{
			if (callback == RecycleCallbackName && node is IObjectPoolLifecycle { SupportsDirectPoolLifecycleDispatch: not false } objectPoolLifecycle)
			{
				objectPoolLifecycle.RecycleToPool();
			}
			else if (node.HasMethod(callback))
			{
				node.Call(callback);
			}
		}
	}

	public void Maintenance(ulong frame)
	{
		int num = Math.Max(0, idleRetainNum);
		if (maxNum >= 0)
		{
			num = Math.Min(num, maxNum);
		}
		if (stack.Count <= num || frame < _lastTouchedFrame || frame - _lastTouchedFrame < idleTrimFrames)
		{
			return;
		}
		while (stack.Count > num)
		{
			int index = stack.Count - 1;
			Node node = stack[index];
			stack.RemoveAt(index);
			if (GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion())
			{
				node.QueueFree();
			}
		}
	}

	private PackedScene GetScene()
	{
		if (scene != null)
		{
			return scene;
		}
		if (string.IsNullOrEmpty(scenePath))
		{
			return null;
		}
		string path = ProjectResourceUidCache.ResolveResourcePath(scenePath);
		scene = GD.Load<PackedScene>(path);
		return scene;
	}

	public void Clear()
	{
		foreach (Node item in stack)
		{
			if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
			{
				item.QueueFree();
			}
		}
		stack.Clear();
		_pendingPush.Clear();
		_deferredPushScheduled = false;
		_lastTouchedFrame = 0uL;
	}

	public void ReleaseImmediately()
	{
		foreach (Node item in stack)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.Free();
			}
		}
		foreach (Node item2 in _pendingPush)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				item2.Free();
			}
		}
		stack.Clear();
		_pendingPush.Clear();
		_deferredPushScheduled = false;
		_lastTouchedFrame = 0uL;
		scene = null;
	}
}
