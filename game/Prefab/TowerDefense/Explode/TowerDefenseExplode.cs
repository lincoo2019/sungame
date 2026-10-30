using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseExplode : Node2D
{
	private enum ExplosionKind
	{
		Projectile,
		Area,
		Line,
		Column
	}

	private sealed class ExplosionWorkItem
	{
		public ExplosionKind Kind;

		public ulong EarliestPhysicsFrame;

		public Vector2 Position;

		public Vector2 Size;

		public int LineOrColumn;

		public TowerDefenseEnum.CHARACTER_CAMP Camp;

		public int CollisionFlags;

		public bool CanHitShieldedTargets;

		public bool SuppressDeathrattles;

		public TowerDefenseProjectileConfig ProjectileConfig;

		public ProjectileHitInfo ProjectileHitInfo;

		public TowerDefenseCharacterEventBase[] ProjectileEvents;

		public readonly List<TowerDefenseCharacterEventBase> Events = new List<TowerDefenseCharacterEventBase>();

		public TowerDefenseCharacter ExcludedCharacter;

		public HashSet<TowerDefenseCharacter> ExcludeSet;

		public List<TowerDefenseCharacter> Characters;

		public int CharacterIndex;

		public void Reset()
		{
			Kind = ExplosionKind.Projectile;
			EarliestPhysicsFrame = 0uL;
			Position = default(Vector2);
			Size = default(Vector2);
			LineOrColumn = 0;
			Camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
			CollisionFlags = 0;
			CanHitShieldedTargets = false;
			SuppressDeathrattles = false;
			ProjectileConfig = null;
			ProjectileHitInfo = default(ProjectileHitInfo);
			ProjectileEvents = null;
			Events.Clear();
			ExcludedCharacter = null;
			ExcludeSet = null;
			Characters = null;
			CharacterIndex = 0;
		}
	}

	private const int MaxCharacterSnapshotPoolSize = 32;

	private const int MaxExcludeSetPoolSize = 32;

	private const int MaxWorkItemPoolSize = 2048;

	private const string DispatcherNodeName = "ExplosionDispatchQueue";

	private static readonly HashSet<TowerDefenseCharacter> _emptyExclude = new HashSet<TowerDefenseCharacter>();

	private static readonly Stack<List<TowerDefenseCharacter>> _characterSnapshotPool = new Stack<List<TowerDefenseCharacter>>();

	private static readonly Stack<HashSet<TowerDefenseCharacter>> _excludeSetPool = new Stack<HashSet<TowerDefenseCharacter>>();

	private static readonly Stack<ExplosionWorkItem> _workItemPool = new Stack<ExplosionWorkItem>();

	private static TowerDefenseExplode _dispatcher;

	private static Node2D _dispatcherRoot;

	private readonly Queue<ExplosionWorkItem> _pending = new Queue<ExplosionWorkItem>();

	private bool _dispatching;

	private static List<TowerDefenseCharacter> RentCharacterSnapshot()
	{
		if (_characterSnapshotPool.Count == 0)
		{
			return new List<TowerDefenseCharacter>();
		}
		return _characterSnapshotPool.Pop();
	}

	private static void ReturnCharacterSnapshot(List<TowerDefenseCharacter> snapshot)
	{
		if (snapshot != null)
		{
			snapshot.Clear();
			if (_characterSnapshotPool.Count < 32)
			{
				_characterSnapshotPool.Push(snapshot);
			}
		}
	}

	private static HashSet<TowerDefenseCharacter> RentExcludeSet(Array<TowerDefenseCharacter> exclude)
	{
		if (exclude == null)
		{
			return _emptyExclude;
		}
		int count = exclude.Count;
		if (count == 0)
		{
			return _emptyExclude;
		}
		HashSet<TowerDefenseCharacter> hashSet = ((_excludeSetPool.Count == 0) ? new HashSet<TowerDefenseCharacter>() : _excludeSetPool.Pop());
		for (int i = 0; i < count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = exclude[i];
			if (towerDefenseCharacter != null)
			{
				hashSet.Add(towerDefenseCharacter);
			}
		}
		return hashSet;
	}

	private static void CaptureExclusions(ExplosionWorkItem item, Array<TowerDefenseCharacter> exclude)
	{
		if (exclude == null || exclude.Count == 0)
		{
			item.ExcludeSet = _emptyExclude;
		}
		else if (exclude.Count == 1)
		{
			item.ExcludedCharacter = exclude[0];
			item.ExcludeSet = _emptyExclude;
		}
		else
		{
			item.ExcludeSet = RentExcludeSet(exclude);
		}
	}

	private static void ReturnExcludeSet(HashSet<TowerDefenseCharacter> set)
	{
		if (set != null && set != _emptyExclude)
		{
			set.Clear();
			if (_excludeSetPool.Count < 32)
			{
				_excludeSetPool.Push(set);
			}
		}
	}

	private static ExplosionWorkItem RentWorkItem()
	{
		if (_workItemPool.Count != 0)
		{
			return _workItemPool.Pop();
		}
		return new ExplosionWorkItem();
	}

	private static void ReturnWorkItem(ExplosionWorkItem item)
	{
		if (item != null)
		{
			ReturnExcludeSet(item.ExcludeSet);
			ReturnCharacterSnapshot(item.Characters);
			item.Reset();
			if (_workItemPool.Count < 2048)
			{
				_workItemPool.Push(item);
			}
		}
	}

	private static void CopyEvents(Array<TowerDefenseCharacterEventBase> source, List<TowerDefenseCharacterEventBase> destination)
	{
		if (source != null)
		{
			int count = source.Count;
			for (int i = 0; i < count; i++)
			{
				destination.Add(source[i]);
			}
		}
	}

	private static TowerDefenseExplode GetOrCreateDispatcher()
	{
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return null;
		}
		if (_dispatcher != null && !_dispatcher.IsQueuedForDeletion() && _dispatcherRoot == characterNode)
		{
			return _dispatcher;
		}
		TowerDefenseExplode nodeOrNull = characterNode.GetNodeOrNull<TowerDefenseExplode>("ExplosionDispatchQueue");
		if (GodotObject.IsInstanceValid(nodeOrNull) && !nodeOrNull.IsQueuedForDeletion())
		{
			_dispatcher = nodeOrNull;
			_dispatcherRoot = characterNode;
			return nodeOrNull;
		}
		_dispatcher = new TowerDefenseExplode
		{
			Name = "ExplosionDispatchQueue"
		};
		characterNode.AddChild(_dispatcher, forceReadableName: false, InternalMode.Disabled);
		_dispatcherRoot = characterNode;
		return _dispatcher;
	}

	private static void Enqueue(ExplosionWorkItem item)
	{
		TowerDefenseExplode orCreateDispatcher = GetOrCreateDispatcher();
		if (orCreateDispatcher == null)
		{
			ReturnWorkItem(item);
			return;
		}
		item.EarliestPhysicsFrame = Engine.GetPhysicsFrames() + 1;
		orCreateDispatcher._pending.Enqueue(item);
		orCreateDispatcher.SetPhysicsProcess(enable: true);
	}

	public static TowerDefenseExplode CreateProjectileExplode(Vector2 pos, TowerDefenseProjectileConfig projectileConfig, Array<TowerDefenseCharacter> exclude, TowerDefenseEnum.CHARACTER_CAMP camp, bool canHitShieldedTargets = false)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid(projectileConfig))
		{
			return null;
		}
		QueueProjectileExplode(pos, projectileConfig, exclude, null, camp, canHitShieldedTargets, projectileConfig.baseDamage, projectileConfig.damageFlags, projectileConfig.fireMethodFlags, projectileConfig.collisionFlags);
		return null;
	}

	public static TowerDefenseExplode CreateProjectileExplode(Vector2 pos, TowerDefenseProjectileConfig projectileConfig, Array<TowerDefenseCharacter> exclude, TowerDefenseEnum.CHARACTER_CAMP camp, bool canHitShieldedTargets, double runtimeDamage, int runtimeDamageFlags, int runtimeFireMethodFlags, int runtimeCollisionFlags)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid(projectileConfig))
		{
			return null;
		}
		QueueProjectileExplode(pos, projectileConfig, exclude, null, camp, canHitShieldedTargets, runtimeDamage, runtimeDamageFlags, runtimeFireMethodFlags, runtimeCollisionFlags);
		return null;
	}

	internal static TowerDefenseExplode CreateProjectileExplodeSingleExclude(Vector2 pos, TowerDefenseProjectileConfig projectileConfig, TowerDefenseCharacter excludedCharacter, TowerDefenseEnum.CHARACTER_CAMP camp, bool canHitShieldedTargets = false)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid(projectileConfig))
		{
			return null;
		}
		QueueProjectileExplode(pos, projectileConfig, null, excludedCharacter, camp, canHitShieldedTargets, projectileConfig.baseDamage, projectileConfig.damageFlags, projectileConfig.fireMethodFlags, projectileConfig.collisionFlags);
		return null;
	}

	internal static TowerDefenseExplode CreateProjectileExplodeSingleExclude(Vector2 pos, TowerDefenseProjectileConfig projectileConfig, TowerDefenseCharacter excludedCharacter, TowerDefenseEnum.CHARACTER_CAMP camp, bool canHitShieldedTargets, double runtimeDamage, int runtimeDamageFlags, int runtimeFireMethodFlags, int runtimeCollisionFlags)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid(projectileConfig))
		{
			return null;
		}
		QueueProjectileExplode(pos, projectileConfig, null, excludedCharacter, camp, canHitShieldedTargets, runtimeDamage, runtimeDamageFlags, runtimeFireMethodFlags, runtimeCollisionFlags);
		return null;
	}

	private static void QueueProjectileExplode(Vector2 pos, TowerDefenseProjectileConfig projectileConfig, Array<TowerDefenseCharacter> exclude, TowerDefenseCharacter excludedCharacter, TowerDefenseEnum.CHARACTER_CAMP camp, bool canHitShieldedTargets, double runtimeDamage, int runtimeDamageFlags, int runtimeFireMethodFlags, int runtimeCollisionFlags)
	{
		ExplosionWorkItem explosionWorkItem = RentWorkItem();
		explosionWorkItem.Kind = ExplosionKind.Projectile;
		explosionWorkItem.Position = pos;
		explosionWorkItem.Camp = camp;
		explosionWorkItem.CanHitShieldedTargets = canHitShieldedTargets;
		explosionWorkItem.CollisionFlags = runtimeCollisionFlags;
		explosionWorkItem.ProjectileConfig = projectileConfig;
		explosionWorkItem.ProjectileHitInfo = new ProjectileHitInfo
		{
			damage = runtimeDamage,
			damageFlags = runtimeDamageFlags,
			useRuntimeOverrides = true,
			fireMethodFlags = runtimeFireMethodFlags,
			position = pos,
			projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL,
			config = projectileConfig,
			camp = camp,
			collisionFlags = runtimeCollisionFlags
		};
		explosionWorkItem.SuppressDeathrattles = (runtimeDamageFlags & 0x20) != 0;
		explosionWorkItem.ProjectileEvents = projectileConfig.HitCharacterEvents;
		explosionWorkItem.ExcludedCharacter = excludedCharacter;
		if (excludedCharacter == null)
		{
			CaptureExclusions(explosionWorkItem, exclude);
		}
		Enqueue(explosionWorkItem);
	}

	public static TowerDefenseExplode CreateExplode(Vector2 pos, Vector2 size, Array<TowerDefenseCharacterEventBase> eventList, Array<TowerDefenseCharacter> exclude, TowerDefenseEnum.CHARACTER_CAMP camp, int collisionFlags, bool suppressDeathrattles = false)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return null;
		}
		if (eventList == null || eventList.Count == 0)
		{
			return null;
		}
		ExplosionWorkItem explosionWorkItem = RentWorkItem();
		explosionWorkItem.Kind = ExplosionKind.Area;
		explosionWorkItem.Position = pos;
		explosionWorkItem.Size = size;
		explosionWorkItem.Camp = camp;
		explosionWorkItem.CollisionFlags = collisionFlags;
		explosionWorkItem.SuppressDeathrattles = suppressDeathrattles;
		CaptureExclusions(explosionWorkItem, exclude);
		CopyEvents(eventList, explosionWorkItem.Events);
		Enqueue(explosionWorkItem);
		return null;
	}

	public static TowerDefenseExplode CreateExplodeLine(int line, Array<TowerDefenseCharacterEventBase> eventList, Array<TowerDefenseCharacter> exclude, TowerDefenseEnum.CHARACTER_CAMP camp, int collisionFlags, bool suppressDeathrattles = false)
	{
		return QueueLineColumnExplosion(ExplosionKind.Line, line, eventList, exclude, camp, collisionFlags, suppressDeathrattles);
	}

	public static TowerDefenseExplode CreateExplodeColumn(int column, Array<TowerDefenseCharacterEventBase> eventList, Array<TowerDefenseCharacter> exclude, TowerDefenseEnum.CHARACTER_CAMP camp, int collisionFlags, bool suppressDeathrattles = false)
	{
		return QueueLineColumnExplosion(ExplosionKind.Column, column, eventList, exclude, camp, collisionFlags, suppressDeathrattles);
	}

	private static TowerDefenseExplode QueueLineColumnExplosion(ExplosionKind kind, int lineOrColumn, Array<TowerDefenseCharacterEventBase> eventList, Array<TowerDefenseCharacter> exclude, TowerDefenseEnum.CHARACTER_CAMP camp, int collisionFlags, bool suppressDeathrattles = false)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return null;
		}
		if (eventList == null || eventList.Count == 0)
		{
			return null;
		}
		ExplosionWorkItem explosionWorkItem = RentWorkItem();
		explosionWorkItem.Kind = kind;
		explosionWorkItem.LineOrColumn = lineOrColumn;
		explosionWorkItem.Camp = camp;
		explosionWorkItem.CollisionFlags = collisionFlags;
		explosionWorkItem.SuppressDeathrattles = suppressDeathrattles;
		CaptureExclusions(explosionWorkItem, exclude);
		CopyEvents(eventList, explosionWorkItem.Events);
		Enqueue(explosionWorkItem);
		return null;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_dispatching)
		{
			return;
		}
		_dispatching = true;
		int items = 0;
		int explosionCount = 0;
		try
		{
			items = DispatchReadyExplosions(respectEarliestPhysicsFrame: true, out explosionCount);
		}
		finally
		{
			_dispatching = false;
			TowerDefensePerfProfiler.SampleHotPath("explode.dispatch.workUnits", items);
			TowerDefensePerfProfiler.SampleHotPath("explode.dispatch.explosions", explosionCount);
			if (_pending.Count == 0)
			{
				SetPhysicsProcess(enable: false);
			}
		}
	}

	private int DispatchReadyExplosions(bool respectEarliestPhysicsFrame, out int explosionCount)
	{
		ulong num = (respectEarliestPhysicsFrame ? Engine.GetPhysicsFrames() : 18446744073709551615uL);
		int num2 = 0;
		explosionCount = 0;
		while (_pending.Count > 0)
		{
			ExplosionWorkItem explosionWorkItem = _pending.Peek();
			if (explosionWorkItem.EarliestPhysicsFrame > num)
			{
				break;
			}
			_pending.Dequeue();
			explosionCount++;
			try
			{
				PrepareCharacters(explosionWorkItem);
				num2++;
				while (explosionWorkItem.CharacterIndex < explosionWorkItem.Characters.Count)
				{
					ExecuteNextCharacter(explosionWorkItem);
					num2++;
				}
			}
			catch (Exception value)
			{
				GD.PushError($"TowerDefenseExplode atomic dispatch failed: {value}");
			}
			finally
			{
				ReturnWorkItem(explosionWorkItem);
			}
		}
		return num2;
	}

	internal static int GetPendingWorkItemCountForTest()
	{
		if (!GodotObject.IsInstanceValid(_dispatcher))
		{
			return 0;
		}
		return _dispatcher._pending.Count;
	}

	internal static int DrainPendingWorkForTest()
	{
		if (!GodotObject.IsInstanceValid(_dispatcher) || _dispatcher._dispatching)
		{
			return 0;
		}
		TowerDefenseExplode dispatcher = _dispatcher;
		dispatcher._dispatching = true;
		try
		{
			int explosionCount;
			return dispatcher.DispatchReadyExplosions(respectEarliestPhysicsFrame: false, out explosionCount);
		}
		finally
		{
			dispatcher._dispatching = false;
			if (dispatcher._pending.Count == 0)
			{
				dispatcher.SetPhysicsProcess(enable: false);
			}
		}
	}

	private static void PrepareCharacters(ExplosionWorkItem item)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance.characterRegistry))
		{
			item.Characters = RentCharacterSnapshot();
			return;
		}
		item.Characters = RentCharacterSnapshot();
		switch (item.Kind)
		{
		case ExplosionKind.Projectile:
			if (item.ProjectileConfig != null)
			{
				Vector2 size2 = instance.GetMapGridSize() * 2f * item.ProjectileConfig.rangeSize;
				instance.characterRegistry.FillCharactersIntersectingRectListExcludingCamp(AabbShapeUtil.RectFromCenter(item.Position, size2), item.Camp, item.Characters);
			}
			break;
		case ExplosionKind.Area:
		{
			Vector2 size = instance.GetMapGridSize() * 2f * item.Size;
			instance.characterRegistry.FillCharactersIntersectingRectListExcludingCamp(AabbShapeUtil.RectFromCenter(item.Position, size), item.Camp, item.Characters);
			break;
		}
		case ExplosionKind.Line:
			item.Characters.AddRange(instance.GetCharacterLineList(item.LineOrColumn, fliterGraveStone: false));
			break;
		case ExplosionKind.Column:
			item.Characters.AddRange(instance.GetCharacterColumnList(item.LineOrColumn, fliterGraveStone: false));
			break;
		}
	}

	private static void ExecuteNextCharacter(ExplosionWorkItem item)
	{
		TowerDefenseCharacter towerDefenseCharacter = item.Characters[item.CharacterIndex++];
		if (!PassesBaseFilter(towerDefenseCharacter, item.Camp, item.ExcludedCharacter, item.ExcludeSet, out var instance) || !towerDefenseCharacter.CanReceiveExplosionHit())
		{
			return;
		}
		bool allowItemLadder = item.Kind == ExplosionKind.Line || item.Kind == ExplosionKind.Column;
		if (!PassesCollisionFilter(towerDefenseCharacter, instance, item.CollisionFlags, allowItemLadder))
		{
			return;
		}
		bool suppressDeathrattles = towerDefenseCharacter.suppressDeathrattles;
		towerDefenseCharacter.suppressDeathrattles |= item.SuppressDeathrattles;
		try
		{
			ExecuteCharacterEvents(item, towerDefenseCharacter, instance);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !towerDefenseCharacter.isDestroy && !instance.die && !instance.nearDie)
			{
				towerDefenseCharacter.suppressDeathrattles = suppressDeathrattles;
			}
		}
	}

	private static void ExecuteCharacterEvents(ExplosionWorkItem item, TowerDefenseCharacter character, TowerDefenseCharacterInstance instance)
	{
		if (item.Kind == ExplosionKind.Projectile)
		{
			bool flag = (item.ProjectileHitInfo.fireMethodFlags & 2) != 0;
			if (!instance.HasShieldArmor || flag || item.CanHitShieldedTargets)
			{
				TowerDefenseCharacterEventBase[] projectileEvents = item.ProjectileEvents;
				for (int i = 0; i < projectileEvents.Length; i++)
				{
					projectileEvents[i].Execute(character.GetLogicalGlobalPosition(), character);
				}
				character.ProjectileHurt(in item.ProjectileHitInfo, item.ProjectileConfig, playSplatAudio: true, Vector2.Zero, isRange: true);
			}
		}
		else
		{
			Vector2 pos = ((item.Kind == ExplosionKind.Area) ? item.Position : (character.GetLogicalGlobalPosition() + new Vector2((float)GD.RandRange(-25.0, 25.0), 0f)));
			for (int j = 0; j < item.Events.Count; j++)
			{
				item.Events[j].Execute(pos, character);
			}
		}
	}

	public override void _ExitTree()
	{
		while (_pending.Count > 0)
		{
			ReturnWorkItem(_pending.Dequeue());
		}
		if (_dispatcher == this)
		{
			_dispatcher = null;
			_dispatcherRoot = null;
		}
	}

	private static bool PassesBaseFilter(TowerDefenseCharacter character, TowerDefenseEnum.CHARACTER_CAMP camp, TowerDefenseCharacter excludedCharacter, HashSet<TowerDefenseCharacter> exclude, out TowerDefenseCharacterInstance instance)
	{
		instance = null;
		if (character == null)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(character))
		{
			return false;
		}
		if (character.die || character.nearDie)
		{
			return false;
		}
		if (character.camp == camp)
		{
			return false;
		}
		if (character == excludedCharacter)
		{
			return false;
		}
		if (exclude != null && exclude.Contains(character))
		{
			return false;
		}
		TowerDefenseCharacterInstance instance2 = character.instance;
		if (!GodotObject.IsInstanceValid(instance2))
		{
			return false;
		}
		if (!CanExplosionCollect(character, instance2))
		{
			return false;
		}
		instance = instance2;
		return true;
	}

	private static bool CanExplosionCollect(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance)
	{
		if (instance.canBeCollection)
		{
			return true;
		}
		return character is TowerDefenseZombieGhost;
	}

	private static bool PassesCollisionFilter(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, int collisionFlags, bool allowItemLadder)
	{
		if (allowItemLadder && character.config.name == "ItemLadder")
		{
			return true;
		}
		if ((collisionFlags & instance.maskFlags) == 0 && collisionFlags != -1)
		{
			return false;
		}
		return true;
	}
}
