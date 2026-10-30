using Godot;
using Godot.Collections;
using Microsoft.Extensions.Logging;
using ZLogger;

[GlobalClass]
public partial class ObjectManager : Node2D
{
	private static readonly ILogger _logger = Log.CreateLogger<ObjectManager>();

	private const ulong PoolMaintenanceIntervalFrames = 60uL;

	private ulong _lastPoolMaintenanceFrame;

	public static ObjectManager Instance { get; private set; }

	public Array<PoolConfig> poolList { get; set; } = new Array<PoolConfig>();

	public override void _Ready()
	{
		Instance = this;
		ProjectileUpdateManager node = new ProjectileUpdateManager();
		AddChild(node, forceReadableName: false, InternalMode.Disabled);
		poolList = new Array<PoolConfig>();
		poolList.Resize(1000);
		for (int i = 0; i < 1000; i++)
		{
			poolList[i] = null;
		}
		PoolCreate("uid://bct1gvf1i8ohw", ObjectManagerConfig.OBJECT.damagePart, 64, "Refresh", "Recycle");
		PoolCreate("uid://dk3bkihnh1i0l", ObjectManagerConfig.OBJECT.SUN, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://d161xee5m0kkw", ObjectManagerConfig.OBJECT.SUN_BRAIN, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://da7lvlco511ds", ObjectManagerConfig.OBJECT.SUN_JALAPENO, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://boebeodp5s2g2", ObjectManagerConfig.OBJECT.SUN_QX, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://c4r7bn2wqxnat", ObjectManagerConfig.OBJECT.SUN_MAGIC, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://csynbfevdbiju", ObjectManagerConfig.OBJECT.COIN_SILVER, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://kbif4idtgolo", ObjectManagerConfig.OBJECT.COIN_GOLD, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://6b78y08u52f5", ObjectManagerConfig.OBJECT.COIN_DIAMOND, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://bnrh1k2cgopsn", ObjectManagerConfig.OBJECT.COIN_LUCKY_BAG, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://733w81lrellb", ObjectManagerConfig.OBJECT.COIN_TQ, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://c25ngfvpp0uo3", ObjectManagerConfig.OBJECT.COIN_YB1, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://1twddwaolt4r", ObjectManagerConfig.OBJECT.COIN_YB2, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://dsfn18qda4271", ObjectManagerConfig.OBJECT.COIN_GOLD_SHARD, 32, "Refresh", "Recycle", 8);
		PoolCreate("uid://ueeii5v2dl8q", ObjectManagerConfig.OBJECT.PARTICLES_SPLASH, 128, "Refresh", "Recycle", 24);
		PoolCreate("uid://lcop8me7nwde", ObjectManagerConfig.OBJECT.PARTICLES_RISE_DIRT, 64, "Refresh", "Recycle");
		PoolCreate("uid://b0wigigia32ny", ObjectManagerConfig.OBJECT.PARTICLES_ICE_TRAP, 64, "Refresh", "Recycle");
		PoolCreate("res://Script/Component/TowerDefense/Character/ShowHealthComponent/ShowHealthComponentView.tscn", ObjectManagerConfig.OBJECT.SHOW_HEALTH_VIEW, 256, "", "", 32).idleTrimFrames = 600uL;
	}

	public override void _ExitTree()
	{
		for (int i = 0; i < poolList.Count; i++)
		{
			PoolConfig poolConfig = poolList[i];
			if (GodotObject.IsInstanceValid(poolConfig))
			{
				poolConfig.ReleaseImmediately();
			}
		}
		poolList.Clear();
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public override void _PhysicsProcess(double _delta)
	{
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (physicsFrames < _lastPoolMaintenanceFrame)
		{
			_lastPoolMaintenanceFrame = physicsFrames;
		}
		if (physicsFrames - _lastPoolMaintenanceFrame >= 60)
		{
			_lastPoolMaintenanceFrame = physicsFrames;
			for (int i = 0; i < poolList.Count; i++)
			{
				poolList[i]?.Maintenance(physicsFrames);
			}
		}
	}

	public void Clear()
	{
		ProjectileUpdateManager.Instance?.Clear();
		BulletField.ClearRuntimeCaches();
		for (int i = 0; i < poolList.Count; i++)
		{
			PoolConfig poolConfig = poolList[i];
			if (GodotObject.IsInstanceValid(poolConfig))
			{
				poolConfig.Clear();
			}
		}
	}

	public PoolConfig PoolCreate(PackedScene scene, ObjectManagerConfig.OBJECT id, int maxNum, string popCallable = "", string pushCallable = "", int idleRetainNum = 16)
	{
		if (id < ObjectManagerConfig.OBJECT.NOONE || id >= ObjectManagerConfig.OBJECT.MAX)
		{
			ILogger logger = _logger;
			bool enabled;
			ZLoggerErrorInterpolatedStringHandler message = new ZLoggerErrorInterpolatedStringHandler(17, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Invalid pool ID: ");
				message.AppendFormatted((int)id, 0, null, "(int)id");
			}
			logger.ZLogError(ref message);
			return null;
		}
		if (poolList[(int)id] != null)
		{
			poolList[(int)id].Clear();
		}
		PoolConfig poolConfig = new PoolConfig();
		poolConfig.scene = scene;
		poolConfig.maxNum = maxNum;
		poolConfig.idleRetainNum = idleRetainNum;
		poolConfig.popCallable = popCallable;
		poolConfig.pushCallable = pushCallable;
		poolList[(int)id] = poolConfig;
		return poolConfig;
	}

	public PoolConfig PoolCreate(string scenePath, ObjectManagerConfig.OBJECT id, int maxNum, string popCallable = "", string pushCallable = "", int idleRetainNum = 16)
	{
		if (id < ObjectManagerConfig.OBJECT.NOONE || id >= ObjectManagerConfig.OBJECT.MAX)
		{
			ILogger logger = _logger;
			bool enabled;
			ZLoggerErrorInterpolatedStringHandler message = new ZLoggerErrorInterpolatedStringHandler(17, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Invalid pool ID: ");
				message.AppendFormatted((int)id, 0, null, "(int)id");
			}
			logger.ZLogError(ref message);
			return null;
		}
		if (poolList[(int)id] != null)
		{
			poolList[(int)id].Clear();
		}
		PoolConfig poolConfig = new PoolConfig();
		poolConfig.scenePath = scenePath;
		poolConfig.maxNum = maxNum;
		poolConfig.idleRetainNum = idleRetainNum;
		poolConfig.popCallable = popCallable;
		poolConfig.pushCallable = pushCallable;
		poolList[(int)id] = poolConfig;
		return poolConfig;
	}

	public static void PoolPush(ObjectManagerConfig.OBJECT id, Node node)
	{
		if (!GodotObject.IsInstanceValid(node) || TryPoolPush(id, node))
		{
			return;
		}
		ZLoggerErrorInterpolatedStringHandler message;
		bool enabled;
		if (id < ObjectManagerConfig.OBJECT.NOONE || id >= ObjectManagerConfig.OBJECT.MAX)
		{
			ILogger logger = _logger;
			ILogger logger2 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(17, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Invalid pool ID: ");
				message.AppendFormatted((int)id, 0, null, "(int)id");
			}
			logger2.ZLogError(ref message);
		}
		else
		{
			ILogger logger = _logger;
			ILogger logger3 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(29, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Pool not initialized for ID: ");
				message.AppendFormatted((int)id, 0, null, "(int)id");
			}
			logger3.ZLogError(ref message);
		}
	}

	public static bool TryPoolPush(ObjectManagerConfig.OBJECT id, Node node)
	{
		if (!GodotObject.IsInstanceValid(node) || !GodotObject.IsInstanceValid(Instance) || id < ObjectManagerConfig.OBJECT.NOONE || (int)id >= Instance.poolList.Count)
		{
			return false;
		}
		PoolConfig poolConfig = Instance.poolList[(int)id];
		if (!GodotObject.IsInstanceValid(poolConfig))
		{
			return false;
		}
		poolConfig.Push(node);
		return true;
	}

	public static Node PoolPop(ObjectManagerConfig.OBJECT id, Node parent)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return null;
		}
		ZLoggerErrorInterpolatedStringHandler message;
		bool enabled;
		if (id < ObjectManagerConfig.OBJECT.NOONE || id >= ObjectManagerConfig.OBJECT.MAX)
		{
			ILogger logger = _logger;
			ILogger logger2 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(17, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Invalid pool ID: ");
				message.AppendFormatted((int)id, 0, null, "(int)id");
			}
			logger2.ZLogError(ref message);
			return null;
		}
		if (Instance.poolList[(int)id] == null)
		{
			ILogger logger = _logger;
			ILogger logger3 = logger;
			message = new ZLoggerErrorInterpolatedStringHandler(29, 1, logger, out enabled);
			if (enabled)
			{
				message.AppendLiteral("Pool not initialized for ID: ");
				message.AppendFormatted((int)id, 0, null, "(int)id");
			}
			logger3.ZLogError(ref message);
			return null;
		}
		return Instance.poolList[(int)id].Pop(parent);
	}

	public static PackedScene GetPoolScene(ObjectManagerConfig.OBJECT id)
	{
		if (!GodotObject.IsInstanceValid(Instance) || id < ObjectManagerConfig.OBJECT.NOONE || (int)id >= Instance.poolList.Count)
		{
			return null;
		}
		PoolConfig poolConfig = Instance.poolList[(int)id];
		if (!GodotObject.IsInstanceValid(poolConfig))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(poolConfig.scene))
		{
			return poolConfig.scene;
		}
		if (!string.IsNullOrWhiteSpace(poolConfig.scenePath))
		{
			return GD.Load<PackedScene>(poolConfig.scenePath);
		}
		return null;
	}

	public ObjectManager()
	{
		StartupLoadDiagnostics.Mark("autoload.constructed/ObjectManager");
	}
}
