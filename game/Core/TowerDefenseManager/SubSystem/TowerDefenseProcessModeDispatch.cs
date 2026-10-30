using System.Collections.Generic;
using Godot;

public static class TowerDefenseProcessModeDispatch
{
	private static ulong _componentBattlefieldBoundsCacheFrame = 18446744073709551615uL;

	private static bool _hasComponentBattlefieldBoundsForCurrentPhysicsFrame;

	private static float _componentBattlefieldLeftForCurrentPhysicsFrame;

	private static float _componentBattlefieldRightForCurrentPhysicsFrame;

	private static int _characterPhysicsBatchDispatchDepth;

	private static ulong _mapFeatureCacheFrame = 18446744073709551615uL;

	private static bool _hasMapFeatureForCurrentPhysicsFrame;

	private static ulong _specialRulesPreventSleepCacheFrame = 18446744073709551615uL;

	private static bool _specialRulesPreventSleepForCurrentPhysicsFrame;

	private static readonly Dictionary<Node, Node.ProcessModeEnum> ParentModeCache = new Dictionary<Node, Node.ProcessModeEnum>();

	private static Node _lastInheritedDispatchParent;

	private static Node.ProcessModeEnum _lastInheritedDispatchMode;

	private static bool _hasLastInheritedDispatchMode;

	private static ulong _cacheFrame = 18446744073709551615uL;

	private static bool _isIZMModeForCurrentPhysicsFrame;

	public static bool IsCharacterPhysicsBatchDispatchActive => _characterPhysicsBatchDispatchDepth > 0;

	public static ulong CurrentPhysicsFrame => _cacheFrame;

	public static bool IsIZMModeForCurrentPhysicsFrame => _isIZMModeForCurrentPhysicsFrame;

	public static bool HasMapFeatureForCurrentPhysicsFrame => ResolveHasMapFeatureForCurrentPhysicsFrame();

	public static bool SpecialRulesPreventSleepForCurrentPhysicsFrame => ResolveSpecialRulesPreventSleepForCurrentPhysicsFrame();

	public static Node ResolveBatchParent(Node source)
	{
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl))
		{
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			if (GodotObject.IsInstanceValid(characterNode))
			{
				return characterNode;
			}
		}
		if (!GodotObject.IsInstanceValid(source) || !source.IsInsideTree())
		{
			return null;
		}
		SceneTree tree = source.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(tree.CurrentScene))
		{
			return tree.CurrentScene;
		}
		if (!GodotObject.IsInstanceValid(tree.Root))
		{
			return null;
		}
		return tree.Root;
	}

	public static bool TryGetComponentBattlefieldBoundsForCurrentPhysicsFrame(out float left, out float right)
	{
		EnsureComponentBattlefieldBoundsForCurrentPhysicsFrame();
		left = _componentBattlefieldLeftForCurrentPhysicsFrame;
		right = _componentBattlefieldRightForCurrentPhysicsFrame;
		return _hasComponentBattlefieldBoundsForCurrentPhysicsFrame;
	}

	private static void EnsureComponentBattlefieldBoundsForCurrentPhysicsFrame()
	{
		if (_componentBattlefieldBoundsCacheFrame != _cacheFrame)
		{
			_componentBattlefieldBoundsCacheFrame = _cacheFrame;
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (_hasComponentBattlefieldBoundsForCurrentPhysicsFrame = GodotObject.IsInstanceValid(instance))
			{
				_componentBattlefieldLeftForCurrentPhysicsFrame = (float)instance.GetMapGroundLeft();
				_componentBattlefieldRightForCurrentPhysicsFrame = (float)instance.GetMapGroundRight();
			}
			else
			{
				_componentBattlefieldLeftForCurrentPhysicsFrame = 0f;
				_componentBattlefieldRightForCurrentPhysicsFrame = 0f;
			}
		}
	}

	public static void EnterCharacterPhysicsBatchDispatch()
	{
		_characterPhysicsBatchDispatchDepth++;
	}

	public static void ExitCharacterPhysicsBatchDispatch()
	{
		if (_characterPhysicsBatchDispatchDepth > 0)
		{
			_characterPhysicsBatchDispatchDepth--;
		}
	}

	public static bool ShouldDispatch(Node node, bool treePaused)
	{
		Node.ProcessModeEnum processModeEnum = node.ProcessMode;
		if (processModeEnum == Node.ProcessModeEnum.Inherit)
		{
			processModeEnum = ResolveInheritedProcessMode(node);
		}
		return ShouldDispatchMode(processModeEnum, treePaused);
	}

	public static bool ShouldDispatchInherited(Node parent, bool treePaused)
	{
		Node.ProcessModeEnum processModeEnum;
		if (_hasLastInheritedDispatchMode && parent == _lastInheritedDispatchParent)
		{
			processModeEnum = _lastInheritedDispatchMode;
		}
		else
		{
			processModeEnum = ResolveEffectiveProcessModeCached(parent);
			_lastInheritedDispatchParent = parent;
			_lastInheritedDispatchMode = processModeEnum;
			_hasLastInheritedDispatchMode = true;
		}
		return ShouldDispatchMode(processModeEnum, treePaused);
	}

	private static bool ShouldDispatchMode(Node.ProcessModeEnum mode, bool treePaused)
	{
		if (mode == Node.ProcessModeEnum.Disabled)
		{
			return false;
		}
		if (!treePaused)
		{
			return mode != Node.ProcessModeEnum.WhenPaused;
		}
		if (mode != Node.ProcessModeEnum.Always)
		{
			return mode == Node.ProcessModeEnum.WhenPaused;
		}
		return true;
	}

	public static ulong GetPhysicsFrameForCurrentDispatch()
	{
		if (_characterPhysicsBatchDispatchDepth > 0 && _cacheFrame != 18446744073709551615uL)
		{
			return _cacheFrame;
		}
		return Engine.GetPhysicsFrames();
	}

	public static ulong GetPhysicsFrameForCachedGameplayQuery()
	{
		if (_cacheFrame == 18446744073709551615uL)
		{
			return Engine.GetPhysicsFrames();
		}
		return _cacheFrame;
	}

	public static void BeginDispatchPass()
	{
		ParentModeCache.Clear();
		_lastInheritedDispatchParent = null;
		_hasLastInheritedDispatchMode = false;
	}

	public static void BeginFrame()
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (_cacheFrame != physicsFrames)
		{
			_cacheFrame = physicsFrames;
			ParentModeCache.Clear();
			_lastInheritedDispatchParent = null;
			_hasLastInheritedDispatchMode = false;
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			_isIZMModeForCurrentPhysicsFrame = GodotObject.IsInstanceValid(instance) && instance.IsIZMMode();
		}
	}

	private static Node.ProcessModeEnum ResolveInheritedProcessMode(Node node)
	{
		return ResolveEffectiveProcessModeCached(node.GetParent());
	}

	private static Node.ProcessModeEnum ResolveEffectiveProcessModeCached(Node parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return Node.ProcessModeEnum.Pausable;
		}
		if (ParentModeCache.TryGetValue(parent, out var value))
		{
			return value;
		}
		Node.ProcessModeEnum processModeEnum = ResolveEffectiveProcessMode(parent);
		ParentModeCache[parent] = processModeEnum;
		return processModeEnum;
	}

	private static Node.ProcessModeEnum ResolveEffectiveProcessMode(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			Node.ProcessModeEnum processMode = node2.ProcessMode;
			if (processMode != Node.ProcessModeEnum.Inherit)
			{
				return processMode;
			}
			node2 = node2.GetParent();
		}
		return Node.ProcessModeEnum.Pausable;
	}

	private static bool ResolveHasMapFeatureForCurrentPhysicsFrame()
	{
		if (_mapFeatureCacheFrame == _cacheFrame)
		{
			return _hasMapFeatureForCurrentPhysicsFrame;
		}
		_mapFeatureCacheFrame = _cacheFrame;
		_hasMapFeatureForCurrentPhysicsFrame = GodotObject.IsInstanceValid(TowerDefenseManager.GetMapFeature());
		return _hasMapFeatureForCurrentPhysicsFrame;
	}

	private static bool ResolveSpecialRulesPreventSleepForCurrentPhysicsFrame()
	{
		if (_cacheFrame == 18446744073709551615uL)
		{
			return TowerDefenseManager.MapSpecialRulesPreventSleep();
		}
		if (_specialRulesPreventSleepCacheFrame == _cacheFrame)
		{
			return _specialRulesPreventSleepForCurrentPhysicsFrame;
		}
		_specialRulesPreventSleepCacheFrame = _cacheFrame;
		_specialRulesPreventSleepForCurrentPhysicsFrame = TowerDefenseManager.MapSpecialRulesPreventSleep();
		return _specialRulesPreventSleepForCurrentPhysicsFrame;
	}
}
