using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class DropItemRegistry : RefCounted
{
	public static bool IsInit = false;

	public static System.Collections.Generic.Dictionary<StringName, DropItemConfig> DropItemDictionary = new System.Collections.Generic.Dictionary<StringName, DropItemConfig>();

	public static System.Collections.Generic.Dictionary<int, DropItemConfig> DropItemByIdDictionary = new System.Collections.Generic.Dictionary<int, DropItemConfig>();

	public static System.Collections.Generic.Dictionary<int, DropItemConfig> DropItemByCoinObjectIdDictionary = new System.Collections.Generic.Dictionary<int, DropItemConfig>();

	public static SunDropItemHandler _SunHandler;

	public static CoinDropItemHandler _CoinHandler;

	public static LuckyBagDropItemHandler _LuckyBagHandler;

	public static JalapenoSunDropItemHandler _JalapenoSunHandler;

	public static BrainSunDropItemHandler _BrainSunHandler;

	public static QXSunDropItemHandler _QXSunHandler;

	public static MagicSunDropItemHandler _MagicSunHandler;

	public static GoldShardDropItemHandler _GoldShardHandler;

	public static void Init()
	{
		if (!IsInit)
		{
			IsInit = true;
			CreateHandlers();
			RegisterInit();
		}
	}

	public static void CreateHandlers()
	{
		_SunHandler = new SunDropItemHandler();
		_CoinHandler = new CoinDropItemHandler();
		_LuckyBagHandler = new LuckyBagDropItemHandler();
		_JalapenoSunHandler = new JalapenoSunDropItemHandler();
		_QXSunHandler = new QXSunDropItemHandler();
		_MagicSunHandler = new MagicSunDropItemHandler();
		_BrainSunHandler = new BrainSunDropItemHandler();
		_GoldShardHandler = new GoldShardDropItemHandler();
	}

	public static void RegisterInit()
	{
		Register(CreateConfig("Sun", ObjectManagerConfig.OBJECT.SUN, GD.Load<PackedScene>("uid://dk3bkihnh1i0l"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN, 25, "", "Sun", -1, _SunHandler));
		Register(CreateConfig("SunBrain", ObjectManagerConfig.OBJECT.SUN_BRAIN, GD.Load<PackedScene>("uid://d161xee5m0kkw"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN, 25, "", "Sun", -1, _BrainSunHandler));
		Register(CreateConfig("SunJalapeno", ObjectManagerConfig.OBJECT.SUN_JALAPENO, GD.Load<PackedScene>("uid://da7lvlco511ds"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN, 25, "", "Sun", -1, _JalapenoSunHandler));
		Register(CreateConfig("SunQX", ObjectManagerConfig.OBJECT.SUN_QX, GD.Load<PackedScene>("uid://boebeodp5s2g2"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN, 25, "", "Sun", -1, _QXSunHandler));
		Register(CreateConfig("SunMagic", ObjectManagerConfig.OBJECT.SUN_MAGIC, GD.Load<PackedScene>("uid://c4r7bn2wqxnat"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN, 25, "", "Sun", -1, _MagicSunHandler));
		Register(CreateConfig("CoinSilver", ObjectManagerConfig.OBJECT.COIN_SILVER, GD.Load<PackedScene>("uid://csynbfevdbiju"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN, 10, "CoinFall", "CoinPick", 0, _CoinHandler));
		Register(CreateConfig("CoinGold", ObjectManagerConfig.OBJECT.COIN_GOLD, GD.Load<PackedScene>("uid://kbif4idtgolo"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN, 50, "CoinFall", "CoinPick", 1, _CoinHandler));
		Register(CreateConfig("CoinDiamond", ObjectManagerConfig.OBJECT.COIN_DIAMOND, GD.Load<PackedScene>("uid://6b78y08u52f5"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN, 1000, "CoinFall", "Diamond", 2, _CoinHandler));
		Register(CreateConfig("CoinTQ", ObjectManagerConfig.OBJECT.COIN_TQ, GD.Load<PackedScene>("uid://733w81lrellb"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN, 10, "CoinFall", "CoinPick", 4, _CoinHandler));
		Register(CreateConfig("CoinYr1", ObjectManagerConfig.OBJECT.COIN_YB1, GD.Load<PackedScene>("uid://c25ngfvpp0uo3"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN, 50, "CoinFall", "CoinPick", 5, _CoinHandler));
		Register(CreateConfig("CoinYr2", ObjectManagerConfig.OBJECT.COIN_YB2, GD.Load<PackedScene>("uid://1twddwaolt4r"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN, 1000, "CoinFall", "CoinPick", 6, _CoinHandler));
		Register(CreateConfig("LuckyBag", ObjectManagerConfig.OBJECT.COIN_LUCKY_BAG, GD.Load<PackedScene>("uid://bnrh1k2cgopsn"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.SPECIAL, 0, "CoinFall", "CoinPick", 3, _LuckyBagHandler));
		Register(CreateConfig("GoldShard", ObjectManagerConfig.OBJECT.COIN_GOLD_SHARD, GD.Load<PackedScene>("uid://dsfn18qda4271"), 100, TowerDefenseEnum.DROP_ITEM_CATEGORY.SPECIAL, 0, "CoinFall", "CoinPick", 7, _GoldShardHandler));
	}

	public static DropItemConfig CreateConfig(StringName _name, ObjectManagerConfig.OBJECT _id, PackedScene _scene, int _poolMaxNum, TowerDefenseEnum.DROP_ITEM_CATEGORY _category, int _value, string _fallAudio = "CoinFall", string _pickAudio = "CoinPick", int _coinObjectId = -1, DropItemHandler _handler = null)
	{
		return new DropItemConfig
		{
			Name = _name,
			Id = _id,
			Scene = _scene,
			PoolMaxNum = _poolMaxNum,
			Category = _category,
			Value = _value,
			FallAudio = _fallAudio,
			PickAudio = _pickAudio,
			CoinObjectId = _coinObjectId,
			Handler = _handler
		};
	}

	public static void Register(DropItemConfig config)
	{
		DropItemDictionary[config.Name] = config;
		DropItemByIdDictionary[(int)config.Id] = config;
		if (config.CoinObjectId >= 0)
		{
			DropItemByCoinObjectIdDictionary[config.CoinObjectId] = config;
		}
	}

	public static DropItemConfig GetDropItem(StringName _name)
	{
		Init();
		if (!DropItemDictionary.TryGetValue(_name, out var value))
		{
			return null;
		}
		return value;
	}

	public static DropItemConfig GetById(ObjectManagerConfig.OBJECT id)
	{
		Init();
		if (!DropItemByIdDictionary.TryGetValue((int)id, out var value))
		{
			return null;
		}
		return value;
	}

	public static DropItemConfig GetByCoinObjectId(int coinObjectId)
	{
		Init();
		if (!DropItemByCoinObjectIdDictionary.TryGetValue(coinObjectId, out var value))
		{
			return null;
		}
		return value;
	}

	public static Array<DropItemConfig> GetByCategory(TowerDefenseEnum.DROP_ITEM_CATEGORY category)
	{
		Init();
		Array<DropItemConfig> array = new Array<DropItemConfig>();
		foreach (DropItemConfig value in DropItemDictionary.Values)
		{
			if (value.Category == category)
			{
				array.Add(value);
			}
		}
		return array;
	}

	public static ObjectManagerConfig.OBJECT GetPoolKeyByCoinObjectId(int coinObjectId)
	{
		return GetByCoinObjectId(coinObjectId)?.Id ?? ObjectManagerConfig.OBJECT.NOONE;
	}

	public static DropItemHandler GetHandler(StringName _name)
	{
		DropItemConfig dropItem = GetDropItem(_name);
		if (dropItem != null && dropItem.Handler != null)
		{
			return dropItem.Handler;
		}
		return null;
	}

	public static DropItemHandler GetHandlerById(ObjectManagerConfig.OBJECT id)
	{
		DropItemConfig byId = GetById(id);
		if (byId != null && byId.Handler != null)
		{
			return byId.Handler;
		}
		return null;
	}

	public static LuckyBagDropItemHandler GetLuckyBagHandler()
	{
		Init();
		return _LuckyBagHandler;
	}

	public static JalapenoSunDropItemHandler GetJalapenoSunHandler()
	{
		Init();
		return _JalapenoSunHandler;
	}

	public static QXSunDropItemHandler GetQXSunHandler()
	{
		Init();
		return _QXSunHandler;
	}

	public static MagicSunDropItemHandler GetMagicSunHandler()
	{
		Init();
		return _MagicSunHandler;
	}

	public static void Reset()
	{
		Init();
		foreach (DropItemConfig value in DropItemDictionary.Values)
		{
			if (value.Handler != null)
			{
				value.Handler.Reset();
			}
		}
	}

	public static Array<StringName> GetNames()
	{
		Init();
		Array<StringName> array = new Array<StringName>();
		foreach (StringName key in DropItemDictionary.Keys)
		{
			array.Add(key);
		}
		return array;
	}

	public static Array<int> GetIds()
	{
		Init();
		Array<int> array = new Array<int>();
		foreach (int key in DropItemByIdDictionary.Keys)
		{
			array.Add(key);
		}
		return array;
	}

	internal static DropItemConfig GetDropItemLegacyForTests(StringName name)
	{
		Init();
		if (DropItemDictionary.ContainsKey(name))
		{
			return DropItemDictionary[name];
		}
		return null;
	}

	internal static DropItemConfig GetByIdLegacyForTests(ObjectManagerConfig.OBJECT id)
	{
		Init();
		if (DropItemByIdDictionary.ContainsKey((int)id))
		{
			return DropItemByIdDictionary[(int)id];
		}
		return null;
	}

	internal static DropItemConfig GetByCoinObjectIdLegacyForTests(int coinObjectId)
	{
		Init();
		if (DropItemByCoinObjectIdDictionary.ContainsKey(coinObjectId))
		{
			return DropItemByCoinObjectIdDictionary[coinObjectId];
		}
		return null;
	}
}
