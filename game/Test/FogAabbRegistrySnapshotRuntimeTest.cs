using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class FogAabbRegistrySnapshotRuntimeTest : Node
{
	private sealed class CountingQuery : IAabbAreaQuery2D
	{
		public bool Enabled { get; set; } = true;

		public uint CollisionLayer { get; set; } = 8u;

		public uint CollisionMask { get; set; } = 1u;

		public bool Monitoring { get; set; } = true;

		public Node RegistryOwner { get; init; }

		public Rect2 WorldRect { get; set; }

		public int WorldRectReads { get; private set; }

		public bool TryGetWorldRect(out Rect2 rect)
		{
			WorldRectReads++;
			rect = WorldRect;
			return Enabled;
		}
	}

	private const int LightCount = 40;

	private const int OtherRootLightCount = 12;

	private const int FogTileCount = 120;

	private readonly List<CountingQuery> _queries = new List<CountingQuery>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			await RunSnapshotScenario();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[FogAabbRegistrySnapshotRuntimeTest] Unexpected exception: {value}");
		}
		finally
		{
			for (int i = 0; i < _queries.Count; i++)
			{
				AabbAreaLayerRegistry.Unregister(_queries[i]);
			}
		}
		bool flag = _failures == 0;
		GD.Print($"FOG_AABB_REGISTRY_SNAPSHOT_RESULT passed={flag} checks={_checks} failures={_failures} lights={40} fogTiles={120}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunSnapshotScenario()
	{
		CheckSnapshotDoesNotRetainQueries();
		Node characterRoot = new Node
		{
			Name = "CharacterRoot"
		};
		Node otherRoot = new Node
		{
			Name = "OtherCharacterRoot"
		};
		AddChild(characterRoot, forceReadableName: false, InternalMode.Disabled);
		AddChild(otherRoot, forceReadableName: false, InternalMode.Disabled);
		for (int i = 0; i < 40; i++)
		{
			Node node = new Node
			{
				Name = $"Light{i}"
			};
			characterRoot.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			CountingQuery countingQuery = new CountingQuery
			{
				RegistryOwner = node,
				WorldRect = new Rect2((float)i * 10f, 0f, 8f, 8f)
			};
			_queries.Add(countingQuery);
			AabbAreaLayerRegistry.Register(countingQuery);
		}
		for (int j = 0; j < 12; j++)
		{
			Node node2 = new Node
			{
				Name = $"OtherLight{j}"
			};
			otherRoot.AddChild(node2, forceReadableName: false, InternalMode.Disabled);
			CountingQuery countingQuery2 = new CountingQuery
			{
				RegistryOwner = node2,
				WorldRect = new Rect2((float)j * 12f, 40f, 8f, 8f)
			};
			_queries.Add(countingQuery2);
			AabbAreaLayerRegistry.Register(countingQuery2);
		}
		Node node3 = new Node
		{
			Name = "DisabledBranch",
			ProcessMode = ProcessModeEnum.Disabled
		};
		Node node4 = new Node
		{
			Name = "InheritedDisabledLight"
		};
		characterRoot.AddChild(node3, forceReadableName: false, InternalMode.Disabled);
		node3.AddChild(node4, forceReadableName: false, InternalMode.Disabled);
		CountingQuery inheritedDisabledQuery = new CountingQuery
		{
			RegistryOwner = node4,
			WorldRect = new Rect2(0f, 80f, 8f, 8f)
		};
		_queries.Add(inheritedDisabledQuery);
		AabbAreaLayerRegistry.Register(inheritedDisabledQuery);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> first = null;
		for (int k = 0; k < 120; k++)
		{
			IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
			if (first == null)
			{
				first = worldRectSnapshotsForCollisionLayer;
			}
			Check(first == worldRectSnapshotsForCollisionLayer, "Every Fog tile in one physics frame must reuse the same immutable snapshot list.");
			Check(worldRectSnapshotsForCollisionLayer.Count == 40, "The shared snapshot must include every enabled light area under the character root.");
		}
		int num = 0;
		for (int l = 0; l < 40; l++)
		{
			num += _queries[l].WorldRectReads;
		}
		Check(num == 40, $"{120} Fog tiles must compute each of {40} light rectangles once per physics frame, got {num} reads.");
		Check(inheritedDisabledQuery.WorldRectReads == 0, "A light inheriting Disabled from an ancestor must be rejected by effective Godot processing state before reading its rectangle.");
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer2 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(otherRoot, 8u);
		Check(worldRectSnapshotsForCollisionLayer2.Count == 12, "A second root must receive its own frame snapshot.");
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer3 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(first == worldRectSnapshotsForCollisionLayer3, "A/B/A root queries in one frame must reuse A instead of evicting it from a single tuple slot.");
		int num2 = 0;
		for (int m = 0; m < 40; m++)
		{
			num2 += _queries[m].WorldRectReads;
		}
		Check(num2 == 40, "An interleaved root query must not cause the first root's rectangles to be recomputed in the same frame.");
		CountingQuery changed = _queries[0];
		Rect2 worldRect = first[39].WorldRect;
		changed.WorldRect = new Rect2(900f, 900f, 8f, 8f);
		changed.CollisionLayer = 0u;
		changed.Enabled = false;
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer4 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(first == worldRectSnapshotsForCollisionLayer4 && worldRectSnapshotsForCollisionLayer4.Count == 40, "Membership changes after the first query must not partially mutate the current physics-frame snapshot.");
		Check(worldRectSnapshotsForCollisionLayer4[39].WorldRect == worldRect, "World rectangles in a physics-frame snapshot must remain coherent after a same-frame transform/state change.");
		Check(changed.WorldRectReads == 1, "A same-frame state change must not trigger a partial snapshot rebuild.");
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer5 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(first != worldRectSnapshotsForCollisionLayer5 && worldRectSnapshotsForCollisionLayer5.Count == 39, "The next physics frame must rebuild the snapshot and observe changed membership.");
		Check(changed.WorldRectReads == 1, "A disabled query must be rejected before its rectangle is recomputed on the next frame.");
		Node node5 = new Node
		{
			Name = "LateLight"
		};
		characterRoot.AddChild(node5, forceReadableName: false, InternalMode.Disabled);
		CountingQuery lateQuery = new CountingQuery
		{
			RegistryOwner = node5,
			WorldRect = new Rect2(1000f, 0f, 8f, 8f)
		};
		_queries.Add(lateQuery);
		AabbAreaLayerRegistry.Register(lateQuery);
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer6 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(worldRectSnapshotsForCollisionLayer5 == worldRectSnapshotsForCollisionLayer6 && worldRectSnapshotsForCollisionLayer6.Count == 39, "Register must not partially rebuild a snapshot already observed by other Fog tiles in the same physics frame.");
		Check(lateQuery.WorldRectReads == 0, "A same-frame registration must wait for the next coherent frame snapshot before reading its rectangle.");
		List<IAabbAreaQuery2D> queriesForCollisionLayer = AabbAreaLayerRegistry.GetQueriesForCollisionLayer(characterRoot, 8u);
		Check(queriesForCollisionLayer.Contains(lateQuery), "The legacy query API must continue to observe registration changes immediately.");
		CountingQuery countingQuery3 = _queries[1];
		AabbAreaLayerRegistry.Unregister(countingQuery3);
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer7 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(worldRectSnapshotsForCollisionLayer5 == worldRectSnapshotsForCollisionLayer7 && worldRectSnapshotsForCollisionLayer7.Count == 39, "Unregister must not partially mutate a snapshot already observed in the same physics frame.");
		List<IAabbAreaQuery2D> queriesForCollisionLayer2 = AabbAreaLayerRegistry.GetQueriesForCollisionLayer(characterRoot, 8u);
		Check(!queriesForCollisionLayer2.Contains(countingQuery3), "The legacy query API must continue to observe unregistration changes immediately.");
		int lateReadsBeforeLifecycleSnapshot = lateQuery.WorldRectReads;
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer8 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(worldRectSnapshotsForCollisionLayer8.Count == 39, "The next frame must atomically include the registered light and exclude the unregistered light.");
		Check(lateQuery.WorldRectReads == lateReadsBeforeLifecycleSnapshot + 1, "The newly registered light rectangle must be captured once when the next frame snapshot is built.");
		AabbAreaLayerRegistry.Unregister(lateQuery);
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer9 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(worldRectSnapshotsForCollisionLayer8 == worldRectSnapshotsForCollisionLayer9, "Removing a light after snapshot publication must preserve the published frame snapshot without retaining the query object.");
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		IReadOnlyList<AabbAreaLayerRegistry.WorldRectSnapshot> worldRectSnapshotsForCollisionLayer10 = AabbAreaLayerRegistry.GetWorldRectSnapshotsForCollisionLayer(characterRoot, 8u);
		Check(worldRectSnapshotsForCollisionLayer10.Count == 38, "The frame after removal must omit the removed light.");
	}

	private void CheckSnapshotDoesNotRetainQueries()
	{
		FieldInfo[] fields = typeof(AabbAreaLayerRegistry.WorldRectSnapshot).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		for (int i = 0; i < fields.Length; i++)
		{
			Check(!typeof(IAabbAreaQuery2D).IsAssignableFrom(fields[i].FieldType), "A cached world-rectangle snapshot must not strongly retain an IAabbAreaQuery2D across scene transitions.");
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[FogAabbRegistrySnapshotRuntimeTest] " + message);
		}
	}
}
