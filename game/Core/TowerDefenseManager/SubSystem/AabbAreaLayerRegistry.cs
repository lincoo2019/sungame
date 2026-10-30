using System.Collections.Generic;
using Godot;

public static class AabbAreaLayerRegistry
{
	public readonly struct WorldRectSnapshot
	{
		public Rect2 WorldRect { get; }

		public WorldRectSnapshot(Rect2 worldRect)
		{
			WorldRect = worldRect;
		}
	}

	private static readonly List<IAabbAreaQuery2D> RegisteredQueries = new List<IAabbAreaQuery2D>();

	private static readonly List<IAabbAreaQuery2D> QueryResults = new List<IAabbAreaQuery2D>();

	private static readonly List<AabbArea2D> LegacyAreaResults = new List<AabbArea2D>();

	private static readonly Dictionary<(ulong RootId, uint CollisionLayerMask), IReadOnlyList<WorldRectSnapshot>> WorldRectSnapshotsByKey = new Dictionary<(ulong, uint), IReadOnlyList<WorldRectSnapshot>>();

	private static ulong _snapshotFrame = 18446744073709551615uL;

	public static List<AabbArea2D> GetAreasForCollisionLayer(Node root, uint collisionLayerMask)
	{
		LegacyAreaResults.Clear();
		List<IAabbAreaQuery2D> queriesForCollisionLayer = GetQueriesForCollisionLayer(root, collisionLayerMask);
		for (int i = 0; i < queriesForCollisionLayer.Count; i++)
		{
			if (queriesForCollisionLayer[i] is AabbArea2D item)
			{
				LegacyAreaResults.Add(item);
			}
		}
		return LegacyAreaResults;
	}

	private static bool TryGetRegistryOwner(IAabbAreaQuery2D query, out Node owner)
	{
		owner = null;
		if (query == null)
		{
			return false;
		}
		if (query is GodotObject instance && !GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		owner = query.RegistryOwner;
		return GodotObject.IsInstanceValid(owner);
	}

	public static List<IAabbAreaQuery2D> GetQueriesForCollisionLayer(Node root, uint collisionLayerMask)
	{
		QueryResults.Clear();
		if (!GodotObject.IsInstanceValid(root) || collisionLayerMask == 0)
		{
			return QueryResults;
		}
		FillQueriesForCollisionLayer(root, collisionLayerMask, QueryResults);
		return QueryResults;
	}

	private static void FillQueriesForCollisionLayer(Node root, uint collisionLayerMask, List<IAabbAreaQuery2D> result)
	{
		for (int num = RegisteredQueries.Count - 1; num >= 0; num--)
		{
			IAabbAreaQuery2D aabbAreaQuery2D = RegisteredQueries[num];
			Rect2 worldRect;
			if (!TryGetRegistryOwner(aabbAreaQuery2D, out var owner))
			{
				RegisteredQueries.RemoveAt(num);
			}
			else if (CanUseAreaQuery(aabbAreaQuery2D, owner, root, collisionLayerMask, out worldRect))
			{
				result.Add(aabbAreaQuery2D);
			}
		}
	}

	private static bool CanUseAreaQuery(IAabbAreaQuery2D query, Node owner, Node root, uint collisionLayerMask, out Rect2 worldRect)
	{
		worldRect = default(Rect2);
		if (!query.Enabled || !owner.CanProcess())
		{
			return false;
		}
		if ((query.CollisionLayer & collisionLayerMask) == 0)
		{
			return false;
		}
		if (owner != root && !root.IsAncestorOf(owner))
		{
			return false;
		}
		return query.TryGetWorldRect(out worldRect);
	}

	public static void Register(IAabbAreaQuery2D query)
	{
		if (query != null && !RegisteredQueries.Contains(query))
		{
			RegisteredQueries.Add(query);
		}
	}

	public static void Unregister(IAabbAreaQuery2D query)
	{
		if (query != null)
		{
			RegisteredQueries.Remove(query);
		}
	}

	private static List<WorldRectSnapshot> BuildWorldRectSnapshots(Node root, uint collisionLayerMask)
	{
		List<WorldRectSnapshot> result = new List<WorldRectSnapshot>();
		if (!GodotObject.IsInstanceValid(root) || collisionLayerMask == 0)
		{
			return result;
		}
		FillWorldRectSnapshots(root, collisionLayerMask, result);
		return result;
	}

	private static void FillWorldRectSnapshots(Node root, uint collisionLayerMask, List<WorldRectSnapshot> result)
	{
		for (int num = RegisteredQueries.Count - 1; num >= 0; num--)
		{
			IAabbAreaQuery2D query = RegisteredQueries[num];
			Rect2 worldRect;
			if (!TryGetRegistryOwner(query, out var owner))
			{
				RegisteredQueries.RemoveAt(num);
			}
			else if (CanUseAreaQuery(query, owner, root, collisionLayerMask, out worldRect))
			{
				result.Add(new WorldRectSnapshot(worldRect));
			}
		}
	}

	private static void UpdateWorldRectSnapshotFrame()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (physicsFrames != _snapshotFrame)
		{
			WorldRectSnapshotsByKey.Clear();
			_snapshotFrame = physicsFrames;
		}
	}

	private static (ulong RootId, uint CollisionLayerMask) CreateWorldRectSnapshotKey(Node root, uint collisionLayerMask)
	{
		return (RootId: GodotObject.IsInstanceValid(root) ? root.GetInstanceId() : 0, CollisionLayerMask: collisionLayerMask);
	}

	public static IReadOnlyList<WorldRectSnapshot> GetWorldRectSnapshotsForCollisionLayer(Node root, uint collisionLayerMask)
	{
		UpdateWorldRectSnapshotFrame();
		(ulong, uint) key = CreateWorldRectSnapshotKey(root, collisionLayerMask);
		if (WorldRectSnapshotsByKey.TryGetValue(key, out var value))
		{
			return value;
		}
		List<WorldRectSnapshot> list = BuildWorldRectSnapshots(root, collisionLayerMask);
		WorldRectSnapshotsByKey[key] = list;
		return list;
	}
}
