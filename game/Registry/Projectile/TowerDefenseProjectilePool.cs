using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class TowerDefenseProjectilePool : RefCounted
{
	public const int MAX_POOL_SIZE_PER_SCENE = 1024;

	public const string META_SCENE_KEY = "_projectile_pool_scene";

	private const ulong IdleTrimFrames = 1800uL;

	private const ulong MaintenanceIntervalFrames = 60uL;

	private const int IdleRetainedPerScene = 8;

	private static readonly Dictionary<long, List<Node2D>> _pools = new Dictionary<long, List<Node2D>>();

	private static readonly Dictionary<long, ulong> _lastTouchedFrames = new Dictionary<long, ulong>();

	private static readonly List<long> _poolKeysToRemove = new List<long>();

	private static ulong _lastMaintenanceFrame;

	public static Node2D Pop(PackedScene scene, Node parent)
	{
		long instanceId = (long)scene.GetInstanceId();
		_lastTouchedFrames[instanceId] = Engine.GetPhysicsFrames();
		Node2D node2D;
		if (_pools.TryGetValue(instanceId, out var value) && value.Count > 0)
		{
			int index = value.Count - 1;
			node2D = value[index];
			value.RemoveAt(index);
			Node parent2 = node2D.GetParent();
			if (GodotObject.IsInstanceValid(parent2))
			{
				if (parent2 != parent)
				{
					node2D.Reparent(parent);
				}
			}
			else
			{
				parent.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
			}
		}
		else
		{
			node2D = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
			node2D.SetMeta("_projectile_pool_scene", instanceId);
			parent.AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
		}
		return node2D;
	}

	public static void Push(Node node)
	{
		if (GodotObject.IsInstanceValid(node) && node.HasMeta("_projectile_pool_scene"))
		{
			long key = node.GetMeta("_projectile_pool_scene").AsInt64();
			_lastTouchedFrames[key] = Engine.GetPhysicsFrames();
			Node parent = node.GetParent();
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.RemoveChild(node);
			}
			if (node is Node2D node2D)
			{
				node2D.Visible = true;
				node2D.Rotation = 0f;
				node2D.Scale = Vector2.One;
				node2D.Position = Vector2.Zero;
			}
			if (!_pools.TryGetValue(key, out var value))
			{
				value = new List<Node2D>();
				_pools[key] = value;
			}
			if (value.Count < 1024)
			{
				value.Add((Node2D)node);
			}
			else
			{
				node.QueueFree();
			}
		}
	}

	public static void Clear()
	{
		foreach (KeyValuePair<long, List<Node2D>> pool in _pools)
		{
			foreach (Node2D item in pool.Value)
			{
				if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
				{
					item.QueueFree();
				}
			}
		}
		_pools.Clear();
		_lastTouchedFrames.Clear();
		_poolKeysToRemove.Clear();
		_lastMaintenanceFrame = 0uL;
	}

	public static void Maintenance(ulong frame)
	{
		if (frame < _lastMaintenanceFrame)
		{
			_lastMaintenanceFrame = frame;
		}
		else
		{
			if (frame - _lastMaintenanceFrame < 60)
			{
				return;
			}
			_lastMaintenanceFrame = frame;
			_poolKeysToRemove.Clear();
			foreach (KeyValuePair<long, List<Node2D>> pool in _pools)
			{
				if (!_lastTouchedFrames.TryGetValue(pool.Key, out var value))
				{
					_lastTouchedFrames[pool.Key] = frame;
				}
				else
				{
					if (frame < value || frame - value < 1800)
					{
						continue;
					}
					List<Node2D> value2 = pool.Value;
					while (value2.Count > 8)
					{
						int index = value2.Count - 1;
						Node2D node2D = value2[index];
						value2.RemoveAt(index);
						if (GodotObject.IsInstanceValid(node2D) && !node2D.IsQueuedForDeletion())
						{
							node2D.QueueFree();
						}
					}
					if (value2.Count == 0)
					{
						_poolKeysToRemove.Add(pool.Key);
					}
				}
			}
			for (int i = 0; i < _poolKeysToRemove.Count; i++)
			{
				long key = _poolKeysToRemove[i];
				_pools.Remove(key);
				_lastTouchedFrames.Remove(key);
			}
			_poolKeysToRemove.Clear();
		}
	}

	public static int GetPoolSize(PackedScene scene)
	{
		long instanceId = (long)scene.GetInstanceId();
		if (!_pools.TryGetValue(instanceId, out var value))
		{
			return 0;
		}
		return value.Count;
	}

	public static int GetTotalPoolSize()
	{
		int num = 0;
		foreach (List<Node2D> value in _pools.Values)
		{
			num += value.Count;
		}
		return num;
	}

	public static void PreWarm(PackedScene scene, int count)
	{
		long instanceId = (long)scene.GetInstanceId();
		_lastTouchedFrames[instanceId] = Engine.GetPhysicsFrames();
		if (!_pools.TryGetValue(instanceId, out var value))
		{
			value = new List<Node2D>();
			_pools[instanceId] = value;
		}
		int num = Mathf.Max(0, count - value.Count);
		for (int i = 0; i < num; i++)
		{
			Node2D node2D = scene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
			node2D.SetMeta("_projectile_pool_scene", instanceId);
			value.Add(node2D);
		}
	}
}
